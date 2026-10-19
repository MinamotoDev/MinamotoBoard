using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MinamotoBoard.Ink
{
    public sealed class RegionFillStroke : Stroke
    {
        public Geometry Region { get; private set; }
        public RegionFillStroke(Geometry region, Color color) : base(BoundsPoints(region.Bounds),
            new DrawingAttributes { Color = color, Width = 1, Height = 1, IgnorePressure = true })
        {
            Region = region; if (Region.CanFreeze) Region.Freeze();
        }
        private static StylusPointCollection BoundsPoints(Rect bounds)
        {
            return new StylusPointCollection(new[] { new StylusPoint(bounds.Left, bounds.Top),
                new StylusPoint(bounds.Right, bounds.Top), new StylusPoint(bounds.Right, bounds.Bottom),
                new StylusPoint(bounds.Left, bounds.Bottom) });
        }
        protected override void DrawCore(DrawingContext context, DrawingAttributes attributes)
        { context.DrawGeometry(new SolidColorBrush(attributes.Color), null, Region); }
        public override Stroke Clone() { return new RegionFillStroke(Region, DrawingAttributes.Color); }
    }

    public sealed class RegionFillMask
    {
        public int Width, Height;
        public double Scale;
        public byte[] Pixels;
        public Geometry Boundaries;
        public Rect Viewport;
        public double MinimumStrokeWidth;
    }

    public static class RegionFill
    {
        // Rasterize boundaries only, in a temporary bounded mask. Result remains a vector fill.
        public static RegionFillMask Capture(StrokeCollection strokes, double width, double height)
        {
            double scale = Math.Min(1, Math.Min(1400 / width, 1000 / height));
            int w = Math.Max(1, (int)Math.Ceiling(width * scale)), h = Math.Max(1, (int)Math.Ceiling(height * scale));
            DrawingVisual visual = new DrawingVisual();
            double minimumWidth = double.MaxValue;
            using (DrawingContext context = visual.RenderOpen())
            {
                context.PushTransform(new ScaleTransform(scale, scale));
                foreach (Stroke stroke in strokes)
                {
                    if (stroke is RegionFillStroke) continue;
                    DrawingAttributes attributes = stroke.DrawingAttributes.Clone();
                    minimumWidth = Math.Min(minimumWidth, Math.Min(attributes.Width, attributes.Height));
                    attributes.Color = Colors.Black; attributes.IsHighlighter = false;
                    stroke.Draw(context, attributes);
                }
                context.Pop();
            }
            RenderTargetBitmap bitmap = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            byte[] pixels = new byte[w * h * 4]; bitmap.CopyPixels(pixels, w * 4, 0);
            GeometryGroup boundaries = new GeometryGroup { FillRule = FillRule.Nonzero };
            CollectGeometry(visual.Drawing, new Matrix(1 / scale, 0, 0, 1 / scale, 0, 0), boundaries);
            boundaries.Freeze();
            return new RegionFillMask { Width = w, Height = h, Scale = scale, Pixels = pixels,
                Boundaries = boundaries, Viewport = new Rect(0, 0, width, height), MinimumStrokeWidth = minimumWidth };
        }

        private static void CollectGeometry(Drawing drawing, Matrix parent, GeometryGroup result)
        {
            DrawingGroup group = drawing as DrawingGroup;
            if (group != null)
            {
                Matrix transform = group.Transform == null ? Matrix.Identity : group.Transform.Value; transform.Append(parent);
                foreach (Drawing child in group.Children) CollectGeometry(child, transform, result);
                return;
            }
            GeometryDrawing ink = drawing as GeometryDrawing;
            if (ink == null || ink.Geometry == null) return;
            Geometry shape = ink.Brush != null ? ink.Geometry.Clone() : ink.Pen != null ? ink.Geometry.GetWidenedPathGeometry(ink.Pen) : null;
            if (shape == null) return;
            if (shape.IsFrozen) shape = shape.Clone();
            Matrix matrix = shape.Transform == null ? Matrix.Identity : shape.Transform.Value; matrix.Append(parent);
            shape.Transform = new MatrixTransform(matrix); shape.Freeze(); result.Children.Add(shape);
        }

        public static Geometry Find(RegionFillMask mask, Point point, CancellationToken token)
        {
            int width = mask.Width, height = mask.Height;
            int sx = (int)(point.X * mask.Scale), sy = (int)(point.Y * mask.Scale);
            if (sx < 0 || sy < 0 || sx >= width || sy >= height) return null;
            int start = sy * width + sx;
            if (mask.Pixels[start * 4 + 3] >= 24) return null;
            byte[] visited = new byte[width * height];
            int[] queue = new int[width * height]; int head = 0, tail = 0;
            queue[tail++] = start; visited[start] = 1;
            while (head < tail)
            {
                if ((head & 4095) == 0) token.ThrowIfCancellationRequested();
                int index = queue[head++], x = index % width, y = index / width;
                // An open region touching any viewport boundary is deliberately never filled.
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1) return null;
                Visit(index - 1, mask.Pixels, visited, queue, ref tail);
                Visit(index + 1, mask.Pixels, visited, queue, ref tail);
                Visit(index - width, mask.Pixels, visited, queue, ref tail);
                Visit(index + width, mask.Pixels, visited, queue, ref tail);
            }
            if (tail < 4) return null;
            Geometry vector = FindVectorRegion(mask, point, token);
            if (vector != null) return vector;
            StreamGeometry geometry = new StreamGeometry { FillRule = FillRule.Nonzero };
            double unit = 1 / mask.Scale;
            using (StreamGeometryContext context = geometry.Open())
            {
                int spans = 0;
                for (int y = 0; y < height; y++)
                {
                    token.ThrowIfCancellationRequested();
                    for (int x = 0; x < width; x++)
                    {
                        if (visited[y * width + x] == 0) continue;
                        int left = x; while (x + 1 < width && visited[y * width + x + 1] != 0) x++;
                        if (++spans > 50000) return null;
                        context.BeginFigure(new Point(left * unit, y * unit), true, true);
                        context.LineTo(new Point((x + 1) * unit, y * unit), true, false);
                        context.LineTo(new Point((x + 1) * unit, (y + 1) * unit), true, false);
                        context.LineTo(new Point(left * unit, (y + 1) * unit), true, false);
                    }
                }
            }
            geometry.Freeze(); return geometry;
        }

        private sealed class Contour
        {
            public PathFigure Figure;
            public PathGeometry Shape;
            public double Area;
        }

        public static List<Geometry> SplitComponents(Geometry geometry)
        {
            PathGeometry outline = geometry.GetOutlinedPathGeometry(0.05, ToleranceType.Absolute);
            List<Contour> contours = new List<Contour>();
            foreach (PathFigure figure in outline.Figures)
            {
                if (!figure.IsClosed || !figure.IsFilled) continue;
                PathGeometry shape = new PathGeometry(new[] { figure.Clone() }) { FillRule = FillRule.EvenOdd };
                double area = shape.GetArea();
                if (area > 0.01) contours.Add(new Contour { Figure = figure, Shape = shape, Area = area });
            }
            int[] parents = new int[contours.Count];
            for (int i = 0; i < contours.Count; i++)
            {
                parents[i] = -1;
                double parentArea = double.MaxValue;
                for (int j = 0; j < contours.Count; j++)
                {
                    if (i == j || contours[j].Area <= contours[i].Area || contours[j].Area >= parentArea
                        || !contours[j].Shape.Bounds.Contains(contours[i].Shape.Bounds)) continue;
                    if (contours[j].Shape.FillContainsWithDetail(contours[i].Shape) == IntersectionDetail.FullyContains)
                    { parents[i] = j; parentArea = contours[j].Area; }
                }
            }
            List<Geometry> parts = new List<Geometry>();
            for (int i = 0; i < contours.Count; i++)
            {
                int depth = 0;
                for (int parent = parents[i]; parent >= 0; parent = parents[parent]) depth++;
                if (depth % 2 != 0) continue;
                PathGeometry part = new PathGeometry { FillRule = FillRule.EvenOdd };
                part.Figures.Add(contours[i].Figure.Clone());
                // A hole stays with its surrounding component; islands inside holes are separate components.
                for (int j = 0; j < contours.Count; j++) if (parents[j] == i) part.Figures.Add(contours[j].Figure.Clone());
                part.Freeze(); parts.Add(part);
            }
            return parts;
        }

        private static Geometry FindVectorRegion(RegionFillMask mask, Point point, CancellationToken token)
        {
            if (mask.Boundaries == null || mask.Boundaries.IsEmpty()) return null;
            token.ThrowIfCancellationRequested();
            PathGeometry background = Geometry.Combine(new RectangleGeometry(mask.Viewport), mask.Boundaries,
                GeometryCombineMode.Exclude, null, 0.05, ToleranceType.Absolute);
            if (!background.FillContains(point)) return null;
            List<Contour> contours = new List<Contour>();
            Contour selected = null;
            foreach (PathFigure figure in background.Figures)
            {
                token.ThrowIfCancellationRequested();
                if (!figure.IsFilled || !figure.IsClosed) continue;
                PathFigure copy = figure.Clone();
                PathGeometry shape = new PathGeometry(new[] { copy }) { FillRule = FillRule.EvenOdd };
                Contour contour = new Contour { Figure = copy, Shape = shape, Area = shape.GetArea() };
                contours.Add(contour);
                if ((selected == null || contour.Area < selected.Area) && shape.FillContains(point)) selected = contour;
            }
            if (selected == null) return null;
            Rect bounds = selected.Shape.Bounds;
            if (bounds.Left <= 0 || bounds.Top <= 0 || bounds.Right >= mask.Viewport.Right || bounds.Bottom >= mask.Viewport.Bottom) return null;
            PathGeometry region = new PathGeometry { FillRule = FillRule.EvenOdd };
            region.Figures.Add(selected.Figure);
            // Keep interior ink and disconnected enclosed regions as holes, rather than painting through them.
            contours.Sort(delegate(Contour a, Contour b) { return b.Area.CompareTo(a.Area); });
            List<Contour> holes = new List<Contour>();
            foreach (Contour candidate in contours)
            {
                token.ThrowIfCancellationRequested();
                if (candidate == selected || candidate.Area >= selected.Area || !bounds.Contains(candidate.Shape.Bounds)
                    || selected.Shape.FillContainsWithDetail(candidate.Shape) != IntersectionDetail.FullyContains) continue;
                bool nested = false;
                foreach (Contour hole in holes)
                    if (hole.Shape.Bounds.Contains(candidate.Shape.Bounds)
                        && hole.Shape.FillContainsWithDetail(candidate.Shape) == IntersectionDetail.FullyContains) { nested = true; break; }
                if (!nested) { holes.Add(candidate); region.Figures.Add(candidate.Figure); }
            }
            // A small underlap beneath the outline hides antialiasing gaps without crossing the stroke.
            Pen bleed = new Pen(Brushes.Black, Math.Min(1, mask.MinimumStrokeWidth * 0.8)) { LineJoin = PenLineJoin.Round };
            Geometry overlap = region.GetWidenedPathGeometry(bleed, 0.05, ToleranceType.Absolute);
            Geometry result = Geometry.Combine(region, overlap, GeometryCombineMode.Union, null, 0.05, ToleranceType.Absolute);
            token.ThrowIfCancellationRequested(); result.Freeze(); return result;
        }

        private static void Visit(int index, byte[] pixels, byte[] visited, int[] queue, ref int tail)
        {
            if (visited[index] != 0 || pixels[index * 4 + 3] >= 24) return;
            visited[index] = 1; queue[tail++] = index;
        }
    }
}
