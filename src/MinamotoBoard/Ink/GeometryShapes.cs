using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MinamotoBoard.Ink
{
    public enum GeometryShape
    {
        Segment, Arrow, DoubleArrow, Circle, Ellipse, Triangle, RightTriangle, EquilateralTriangle,
        Square, Rectangle, Rhombus, Parallelogram, Trapezoid, RightTrapezoid,
        Pentagon, Hexagon, Octagon, Star, Semicircle, Sector, Arc,
        Cube, Cuboid, TriangularPrism, QuadrangularPrism, PentagonalPrism, HexagonalPrism,
        TriangularPyramid, QuadrangularPyramid, PentagonalPyramid, HexagonalPyramid,
        Cylinder, Cone, Sphere, Frustum
    }

    // Both preview and final ink use the same sampled paths. No editable shape objects remain.
    internal static class GeometryShapes
    {
        internal sealed class ShapePath
        {
            public readonly List<Point> Points = new List<Point>();
            public bool Dashed;
            public bool Guide;
            public string Label;
        }
        public static bool IsSolid(GeometryShape kind) { return kind >= GeometryShape.Cube; }
        public static string Name(GeometryShape kind)
        {
            string[] names = { "线段", "箭头", "双向箭头", "圆", "椭圆", "等腰三角形", "直角三角形", "等边三角形",
                "矩形", "矩形", "菱形", "平行四边形", "等腰梯形", "直角梯形",
                "正五边形", "正六边形", "正八边形", "五角星", "半圆", "扇形", "弧",
                "立方", "立方", "三棱柱", "四棱柱", "五棱柱", "六棱柱",
                "三棱锥", "四棱锥", "五棱锥", "六棱锥", "圆柱", "圆锥", "球", "圆台" };
            return names[(int)kind];
        }
        private static void Path(List<ShapePath> paths, bool dashed, params Point[] points)
        {
            ShapePath path = new ShapePath { Dashed = dashed };
            path.Points.AddRange(points); paths.Add(path);
        }
        private static void Polygon(List<ShapePath> paths, params Point[] points)
        {
            ShapePath path = new ShapePath(); path.Points.AddRange(points); path.Points.Add(points[0]); paths.Add(path);
        }
        private static void EllipseArc(List<ShapePath> paths, double x, double y, double rx, double ry,
            double start, double sweep, bool dashed)
        {
            ShapePath path = new ShapePath { Dashed = dashed };
            int count = Math.Max(12, (int)Math.Ceiling(Math.Abs(sweep) * 48 / Math.PI));
            for (int i = 0; i <= count; i++)
            {
                double a = start + sweep * i / count;
                path.Points.Add(new Point(x + rx * Math.Cos(a), y + ry * Math.Sin(a)));
            }
            paths.Add(path);
        }
        public static List<ShapePath> Build(GeometryShape kind, Point start, Point end, bool guides = false, bool standard = true)
        {
            List<ShapePath> paths = new List<ShapePath>();
            if (kind == GeometryShape.Segment || kind == GeometryShape.Arrow || kind == GeometryShape.DoubleArrow)
            {
                Path(paths, false, start, end);
                Vector direction = end - start;
                double distance = direction.Length;
                if (distance > 0.01 && kind != GeometryShape.Segment)
                {
                    direction.Normalize();
                    Vector back = direction * Math.Min(24, distance * 0.25);
                    Vector wing = new Vector(-back.Y, back.X) * 0.45;
                    Path(paths, false, end - back + wing, end, end - back - wing);
                    if (kind == GeometryShape.DoubleArrow) Path(paths, false, start + back + wing, start, start + back - wing);
                }
                if (guides)
                {
                    GeometryGuides.Segment(paths, start, end);
                    GeometryGuides.ExpandLabels(paths);
                }
                return paths;
            }
            if (kind == GeometryShape.Circle || kind == GeometryShape.Ellipse || kind == GeometryShape.Sphere)
                EllipseArc(paths, .5, .5, .5, .5, 0, Math.PI * 2, false);
            else if (kind == GeometryShape.Triangle || kind == GeometryShape.EquilateralTriangle)
                Polygon(paths, new Point(.5, 0), new Point(1, 1), new Point(0, 1));
            else if (kind == GeometryShape.RightTriangle)
                Polygon(paths, new Point(0, 0), new Point(0, 1), new Point(1, 1));
            else if (kind == GeometryShape.Square || kind == GeometryShape.Rectangle)
                Polygon(paths, new Point(0, 0), new Point(1, 0), new Point(1, 1), new Point(0, 1));
            else if (kind == GeometryShape.Rhombus)
                Polygon(paths, new Point(.5, 0), new Point(1, .5), new Point(.5, 1), new Point(0, .5));
            else if (kind == GeometryShape.Parallelogram)
                Polygon(paths, new Point(.25, 0), new Point(1, 0), new Point(.75, 1), new Point(0, 1));
            else if (kind == GeometryShape.Trapezoid || kind == GeometryShape.RightTrapezoid)
                Polygon(paths, new Point(kind == GeometryShape.RightTrapezoid ? 0 : .25, 0),
                    new Point(.75, 0), new Point(1, 1), new Point(0, 1));
            else if (kind >= GeometryShape.Pentagon && kind <= GeometryShape.Star)
            {
                int count = kind == GeometryShape.Pentagon ? 5 : kind == GeometryShape.Hexagon ? 6 : kind == GeometryShape.Octagon ? 8 : 10;
                Point[] points = new Point[count];
                for (int i = 0; i < count; i++)
                {
                    double radius = kind == GeometryShape.Star && i % 2 == 1 ? .2 : .5;
                    double angle = -Math.PI / 2 + Math.PI * 2 * i / count;
                    points[i] = new Point(.5 + radius * Math.Cos(angle), .5 + radius * Math.Sin(angle));
                }
                Polygon(paths, points);
            }
            else if (kind == GeometryShape.Semicircle || kind == GeometryShape.Arc)
            {
                EllipseArc(paths, .5, 1, .5, 1, Math.PI, Math.PI, false);
                if (kind == GeometryShape.Semicircle) Path(paths, false, new Point(0, 1), new Point(1, 1));
            }
            else if (kind == GeometryShape.Sector)
            {
                EllipseArc(paths, 0, 1, 1, 1, -Math.PI / 2, Math.PI / 2, false);
                Path(paths, false, new Point(0, 0), new Point(0, 1), new Point(1, 1));
            }
            else if (kind >= GeometryShape.Cube && kind <= GeometryShape.HexagonalPyramid)
                Mesh(paths, kind, guides, !standard && kind >= GeometryShape.TriangularPrism
                    ? Math.Abs(end.Y - start.Y) / Math.Max(1, Math.Abs(end.X - start.X)) : -1);
            if (kind == GeometryShape.Sphere)
            {
                EllipseArc(paths, .5, .5, .5, .17, 0, Math.PI, false);
                EllipseArc(paths, .5, .5, .5, .17, Math.PI, Math.PI, true);
                EllipseArc(paths, .5, .5, .18, .5, -Math.PI / 2, Math.PI, false);
                EllipseArc(paths, .5, .5, .18, .5, Math.PI / 2, Math.PI, true);
            }
            else if (kind == GeometryShape.Cylinder || kind == GeometryShape.Cone || kind == GeometryShape.Frustum)
            {
                double topRadius = kind == GeometryShape.Cylinder ? .5 : .27;
                if (kind != GeometryShape.Cone) EllipseArc(paths, .5, .14, topRadius, .14, 0, Math.PI * 2, false);
                EllipseArc(paths, .5, .86, .5, .14, 0, Math.PI, false);
                EllipseArc(paths, .5, .86, .5, .14, Math.PI, Math.PI, true);
                Point left = kind == GeometryShape.Cone ? new Point(.5, 0) : new Point(.5 - topRadius, .14);
                Point right = kind == GeometryShape.Cone ? left : new Point(.5 + topRadius, .14);
                Path(paths, false, left, new Point(0, .86));
                Path(paths, false, right, new Point(1, .86));
            }
            Rect bounds = Rect.Empty;
            foreach (ShapePath path in paths) if (!path.Guide) foreach (Point point in path.Points) bounds.Union(point);
            if (guides && !(kind >= GeometryShape.Cube && kind <= GeometryShape.HexagonalPyramid))
            {
                GeometryShape guideKind = !standard && kind == GeometryShape.Square ? GeometryShape.Rectangle :
                    !standard && kind == GeometryShape.Circle ? GeometryShape.Ellipse : kind;
                GeometryGuides.Add(paths, guideKind);
            }
            double width = Math.Abs(end.X - start.X), height = Math.Abs(end.Y - start.Y);
            // Never distort a regular base through non-uniform screen-space scaling.
            bool fixedRatio = standard || (kind >= GeometryShape.TriangularPrism && kind <= GeometryShape.HexagonalPyramid);
            // Equilateral triangle and semicircle have their own natural width/height.
            double ratio = kind == GeometryShape.EquilateralTriangle ? 2 / Math.Sqrt(3) :
                kind == GeometryShape.Semicircle || kind == GeometryShape.Arc || kind == GeometryShape.Ellipse ? 2 : bounds.Width / Math.Max(.0001, bounds.Height);
            if (fixedRatio)
            {
                double scale = Math.Min(width / Math.Max(.0001, bounds.Width), height / Math.Max(.0001, bounds.Height));
                if (kind == GeometryShape.EquilateralTriangle || kind == GeometryShape.Semicircle || kind == GeometryShape.Arc || kind == GeometryShape.Ellipse)
                { height = Math.Min(height, width / ratio); width = height * ratio; }
                else { width = bounds.Width * scale; height = bounds.Height * scale; }
            }
            double x = end.X >= start.X ? start.X : start.X - width;
            double y = end.Y >= start.Y ? start.Y : start.Y - height;
            foreach (ShapePath path in paths)
                for (int i = 0; i < path.Points.Count; i++)
                {
                    Point point = path.Points[i];
                    path.Points[i] = new Point(x + (point.X - bounds.X) * width / Math.Max(.0001, bounds.Width),
                        kind == GeometryShape.Arc && end.Y < start.Y
                            ? start.Y - (point.Y - bounds.Y) * height / Math.Max(.0001, bounds.Height)
                            : y + (point.Y - bounds.Y) * height / Math.Max(.0001, bounds.Height));
                }
            if (guides)
            {
                if (kind >= GeometryShape.Pentagon && kind <= GeometryShape.Star) GeometryGuides.Polygon(paths, kind == GeometryShape.Star);
                GeometryGuides.ExpandLabels(paths);
            }
            return paths;
        }

        private sealed class Edge { public int A, B; public bool Visible; }
        private static void Mesh(List<ShapePath> paths, GeometryShape kind, bool guides, double targetRatio)
        {
            List<Point3D> vertices = new List<Point3D>();
            List<int[]> faces = new List<int[]>();
            bool box = kind == GeometryShape.Cube || kind == GeometryShape.Cuboid || kind == GeometryShape.QuadrangularPrism;
            bool pyramid = kind >= GeometryShape.TriangularPyramid;
            // Keep an oblique textbook view, but reduce lateral skew and open up the base.
            bool regularBase = kind >= GeometryShape.TriangularPrism && kind <= GeometryShape.HexagonalPyramid;
            double shear = regularBase ? .28 : .45;
            double elevation = regularBase ? .37 : .3;
            int n = box ? 4 : kind == GeometryShape.TriangularPrism || kind == GeometryShape.TriangularPyramid ? 3 :
                kind == GeometryShape.PentagonalPrism || kind == GeometryShape.PentagonalPyramid ? 5 :
                kind == GeometryShape.HexagonalPrism || kind == GeometryShape.HexagonalPyramid ? 6 : 4;
            for (int i = 0; i < n; i++)
            {
                double angle = Math.PI * 2 * i / n - Math.PI / 2;
                double x = box ? (i == 0 || i == 3 ? -.5 : .5) : .5 * Math.Cos(angle);
                double z = box ? (i < 2 ? -.5 : .5) : .5 * Math.Sin(angle);
                vertices.Add(new Point3D(x, -.5, z));
            }
            double height = 1;
            if (targetRatio >= 0)
            {
                double left = double.MaxValue, right = double.MinValue, front = double.MaxValue, back = double.MinValue;
                foreach (Point3D point in vertices)
                {
                    left = Math.Min(left, point.X + shear * point.Z); right = Math.Max(right, point.X + shear * point.Z);
                    front = Math.Min(front, -elevation * point.Z); back = Math.Max(back, -elevation * point.Z);
                }
                // Change the actual 3D height, then apply one uniform scale to the projection.
                double depth = pyramid ? -front : back - front;
                height = Math.Max(.12, Math.Min(1000, targetRatio * (right - left) - depth));
            }
            for (int i = 0; i < n; i++) vertices[i] = new Point3D(vertices[i].X, -height / 2, vertices[i].Z);
            if (pyramid) vertices.Add(new Point3D(0, height / 2, 0));
            else for (int i = 0; i < n; i++) vertices.Add(new Point3D(vertices[i].X, height / 2, vertices[i].Z));
            int[] bottom = new int[n], top = new int[n];
            for (int i = 0; i < n; i++)
            {
                bottom[i] = i; top[i] = n + i;
                int next = (i + 1) % n;
                faces.Add(pyramid ? new[] { i, next, n } : new[] { i, next, next + n, i + n });
            }
            faces.Add(bottom); if (!pyramid) faces.Add(top);
            Point3D center = new Point3D();
            foreach (Point3D p in vertices) { center.X += p.X / vertices.Count; center.Y += p.Y / vertices.Count; center.Z += p.Z / vertices.Count; }
            Dictionary<string, Edge> edges = new Dictionary<string, Edge>();
            foreach (int[] face in faces)
            {
                Vector3D normal = Vector3D.CrossProduct(vertices[face[1]] - vertices[face[0]], vertices[face[2]] - vertices[face[0]]);
                Point3D middle = new Point3D();
                foreach (int index in face) { middle.X += vertices[index].X / face.Length; middle.Y += vertices[index].Y / face.Length; middle.Z += vertices[index].Z / face.Length; }
                if (Vector3D.DotProduct(normal, middle - center) < 0) normal = -normal;
                bool visible = Vector3D.DotProduct(normal, new Vector3D(shear, elevation, -1)) > .00001;
                for (int i = 0; i < face.Length; i++)
                {
                    int a = Math.Min(face[i], face[(i + 1) % face.Length]), b = Math.Max(face[i], face[(i + 1) % face.Length]);
                    string key = a + ":" + b; Edge edge;
                    if (!edges.TryGetValue(key, out edge)) { edge = new Edge { A = a, B = b }; edges.Add(key, edge); }
                    edge.Visible |= visible;
                }
            }
            foreach (Edge edge in edges.Values)
            {
                Point3D a = vertices[edge.A], b = vertices[edge.B];
                Path(paths, !edge.Visible, new Point(a.X + shear * a.Z, -a.Y - elevation * a.Z),
                    new Point(b.X + shear * b.Z, -b.Y - elevation * b.Z));
            }
            if (guides)
            {
                GeometryGuides.Height(paths, new Point(0, -height / 2), new Point(0, height / 2), new Vector(.1, 0));
                GeometryGuides.Mark(paths, new Point(0, height / 2), "O");
            }
        }

        public static StrokeCollection Ink(List<ShapePath> paths, Color color, double width)
        {
            StrokeCollection result = new StrokeCollection();
            DrawingAttributes attributes = new DrawingAttributes { Color = color, Width = width, Height = width,
                IgnorePressure = true, FitToCurve = false, StylusTip = StylusTip.Ellipse };
            foreach (ShapePath path in paths)
            {
                DrawingAttributes style = attributes;
                if (path.Guide) { style = attributes.Clone(); style.Width = style.Height = GuideWidth(width); }
                if (!path.Dashed) AddStroke(result, path.Points, style);
                else
                {
                    // Real gaps, not an unbroken invisible hit-test stroke. Pixel erasing stays native.
                    double dash = Math.Max(6, style.Width * 2.5), gap = Math.Max(4, style.Width * 1.7), phase = 0;
                    List<Point> run = new List<Point>();
                    for (int i = 1; i < path.Points.Count; i++)
                    {
                        Point a = path.Points[i - 1], b = path.Points[i]; Vector delta = b - a;
                        double length = delta.Length, at = 0;
                        if (length < .00001) continue;
                        while (at < length - .00001)
                        {
                            bool drawing = phase < dash;
                            double take = Math.Min(length - at, (drawing ? dash : dash + gap) - phase);
                            if (drawing)
                            {
                                if (run.Count == 0) run.Add(a + delta * (at / length));
                                run.Add(a + delta * ((at + take) / length));
                            }
                            at += take; phase += take;
                            if (phase >= dash - .00001 && drawing) { AddStroke(result, run, style); run.Clear(); }
                            if (phase >= dash + gap - .00001) phase = 0;
                        }
                    }
                    AddStroke(result, run, style);
                }
            }
            return result;
        }
        internal static double GuideWidth(double width) { return Math.Max(1, Math.Min(2, width * .6)); }
        private static void AddStroke(StrokeCollection strokes, IList<Point> points, DrawingAttributes attributes)
        {
            if (points.Count < 2) return;
            StylusPointCollection samples = new StylusPointCollection();
            foreach (Point point in points) samples.Add(new StylusPoint(point.X, point.Y));
            strokes.Add(new Stroke(samples, attributes.Clone()));
        }
    }
}
