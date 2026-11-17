using System;
using System.Collections.Generic;
using System.Windows;

namespace MinamotoBoard.Ink
{
    // Mathematical construction lines and single-line lettering become ordinary erasable ink.
    internal static class GeometryGuides
    {
        private static void Line(List<GeometryShapes.ShapePath> paths, bool dashed, params Point[] points)
        {
            GeometryShapes.ShapePath path = new GeometryShapes.ShapePath { Guide = true, Dashed = dashed };
            path.Points.AddRange(points); paths.Add(path);
        }
        public static void Mark(List<GeometryShapes.ShapePath> paths, Point point, string text)
        {
            GeometryShapes.ShapePath path = new GeometryShapes.ShapePath { Guide = true, Label = text };
            path.Points.Add(point); paths.Add(path);
        }
        public static void Height(List<GeometryShapes.ShapePath> paths, Point top, Point foot, Vector baseDirection)
        {
            Line(paths, true, top, foot);
            Vector up = top - foot;
            if (up.Length > .00001 && baseDirection.Length > .00001)
            {
                double size = Math.Min(10, Math.Min(up.Length * .12, baseDirection.Length * .4));
                up.Normalize(); baseDirection.Normalize();
                Line(paths, false, foot + up * size, foot + (up + baseDirection) * size, foot + baseDirection * size);
            }
            Mark(paths, top + (foot - top) * .5, "h");
        }
        public static void Segment(List<GeometryShapes.ShapePath> paths, Point start, Point end)
        {
            Mark(paths, start, "A"); Mark(paths, end, "B");
            Mark(paths, start + (end - start) * .5, "l");
        }
        private static void Radius(List<GeometryShapes.ShapePath> paths, Point center, Point edge, string label)
        {
            Line(paths, true, center, edge);
            Mark(paths, center, "O"); Mark(paths, center + (edge - center) * .5, label);
        }
        public static void Add(List<GeometryShapes.ShapePath> paths, GeometryShape kind)
        {
            if (kind == GeometryShape.Triangle || kind == GeometryShape.EquilateralTriangle || kind == GeometryShape.RightTriangle)
            {
                Height(paths, new Point(kind == GeometryShape.RightTriangle ? 0 : .5, 0), new Point(kind == GeometryShape.RightTriangle ? 0 : .5, 1), new Vector(.3, 0));
                Mark(paths, new Point(.5, 1), "a");
            }
            else if (kind == GeometryShape.Trapezoid || kind == GeometryShape.RightTrapezoid || kind == GeometryShape.Parallelogram)
            {
                Height(paths, new Point(.4, 0), new Point(.4, 1), new Vector(.3, 0));
                Mark(paths, new Point(.5, 0), "a"); Mark(paths, new Point(.5, 1), "b");
            }
            else if (kind == GeometryShape.Square || kind == GeometryShape.Rectangle)
            {
                Mark(paths, new Point(.5, 1), "a"); Mark(paths, new Point(1, .5), kind == GeometryShape.Square ? "a" : "b");
                Line(paths, false, new Point(0, .92), new Point(.08, .92), new Point(.08, 1));
            }
            else if (kind == GeometryShape.Rhombus)
            {
                Line(paths, true, new Point(.5, 0), new Point(.5, 1));
                Line(paths, true, new Point(0, .5), new Point(1, .5));
                Mark(paths, new Point(.5, .25), "a"); Mark(paths, new Point(.25, .5), "b");
            }
            else if (kind == GeometryShape.Circle || kind == GeometryShape.Sphere)
                Radius(paths, new Point(.5, .5), new Point(1, .5), "r");
            else if (kind == GeometryShape.Ellipse)
            {
                Radius(paths, new Point(.5, .5), new Point(1, .5), "a");
                Line(paths, true, new Point(.5, .5), new Point(.5, 0)); Mark(paths, new Point(.5, .25), "b");
            }
            else if (kind == GeometryShape.Semicircle || kind == GeometryShape.Arc)
                Radius(paths, new Point(.5, 1), new Point(.5, 0), "r");
            else if (kind == GeometryShape.Sector)
            {
                Mark(paths, new Point(0, 1), "O"); Mark(paths, new Point(.5, 1), "r");
                Line(paths, false, new Point(0, .9), new Point(.1, .9), new Point(.1, 1));
            }
            else if (kind == GeometryShape.Cylinder || kind == GeometryShape.Cone || kind == GeometryShape.Frustum)
            {
                Height(paths, new Point(.5, kind == GeometryShape.Cone ? 0 : .14), new Point(.5, .86), new Vector(.3, 0));
                Radius(paths, new Point(.5, .86), new Point(1, .86), kind == GeometryShape.Frustum ? "R" : "r");
                if (kind == GeometryShape.Frustum)
                    Radius(paths, new Point(.5, .14), new Point(.77, .14), "r");
            }
        }
        public static void Polygon(List<GeometryShapes.ShapePath> paths, bool star)
        {
            List<Point> vertices = paths[0].Points;
            int count = vertices.Count - 1;
            Point center = new Point();
            for (int i = 0; i < count; i++)
            { center.X += vertices[i].X / count; center.Y += vertices[i].Y / count; }
            Radius(paths, center, vertices[0], "r");
            if (star) return;
            Point a = vertices[1], b = vertices[2];
            Vector edge = b - a;
            if (edge.LengthSquared < .00001) return;
            double t = Math.Max(0, Math.Min(1, Vector.Multiply(center - a, edge) / edge.LengthSquared));
            Height(paths, center, a + edge * t, edge * .3);
        }
        public static void ExpandLabels(List<GeometryShapes.ShapePath> paths)
        {
            List<GeometryShapes.ShapePath> labels = paths.FindAll(delegate(GeometryShapes.ShapePath path) { return path.Label != null; });
            foreach (GeometryShapes.ShapePath label in labels)
            {
                paths.Remove(label);
                string data;
                switch (label.Label)
                {
                    case "h": data = "0,0 0,1;0,.5 .35,.35 .7,.5 .7,1"; break;
                    case "r": data = "0,.35 0,1;0,.55 .35,.35 .7,.4"; break;
                    case "R": data = "0,1 0,0 .6,0 .75,.25 .6,.5 0,.5;.4,.5 .8,1"; break;
                    case "a": data = ".65,.4 .2,.4 0,.65 .2,1 .65,1 .65,.4 .65,1"; break;
                    case "b": data = "0,0 0,1 .5,1 .7,.7 .5,.4 0,.4"; break;
                    case "l": data = ".2,0 .2,1 .55,1"; break;
                    case "A": data = "0,1 .4,0 .8,1;.15,.65 .65,.65"; break;
                    case "B": data = "0,1 0,0 .5,0 .7,.25 .5,.5 0,.5;.5,.5 .75,.75 .5,1 0,1"; break;
                    default: data = ".2,0 .6,0 .8,.3 .8,.7 .6,1 .2,1 0,.7 0,.3 .2,0"; break;
                }
                Point anchor = label.Points[0] + new Vector(7, -17);
                foreach (string run in data.Split(';'))
                {
                    List<Point> points = new List<Point>();
                    foreach (string pair in run.Split(' '))
                    {
                        string[] xy = pair.Split(',');
                        double x = double.Parse(xy[0], System.Globalization.CultureInfo.InvariantCulture);
                        double y = double.Parse(xy[1], System.Globalization.CultureInfo.InvariantCulture);
                        points.Add(anchor + new Vector(x * 13, y * 15));
                    }
                    Line(paths, false, points.ToArray());
                }
            }
        }
    }
}
