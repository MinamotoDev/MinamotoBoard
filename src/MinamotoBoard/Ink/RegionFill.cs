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
    }

    public static class RegionFill
    {
        // Rasterize boundaries only, in a temporary bounded mask. Result remains a vector fill.
        public static RegionFillMask Capture(StrokeCollection strokes, double width, double height)
        {
            double scale = Math.Min(1, Math.Min(1400 / width, 1000 / height));
            int w = Math.Max(1, (int)Math.Ceiling(width * scale)), h = Math.Max(1, (int)Math.Ceiling(height * scale));
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                context.PushTransform(new ScaleTransform(scale, scale));
                foreach (Stroke stroke in strokes)
                {
                    if (stroke is RegionFillStroke) continue;
                    DrawingAttributes attributes = stroke.DrawingAttributes.Clone();
                    attributes.Color = Colors.Black; attributes.IsHighlighter = false;
                    stroke.Draw(context, attributes);
                }
                context.Pop();
            }
            RenderTargetBitmap bitmap = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            byte[] pixels = new byte[w * h * 4]; bitmap.CopyPixels(pixels, w * 4, 0);
            return new RegionFillMask { Width = w, Height = h, Scale = scale, Pixels = pixels };
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

        private static void Visit(int index, byte[] pixels, byte[] visited, int[] queue, ref int tail)
        {
            if (visited[index] != 0 || pixels[index * 4 + 3] >= 24) return;
            visited[index] = 1; queue[tail++] = index;
        }
    }
}
