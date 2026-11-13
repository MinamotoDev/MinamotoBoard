using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;

namespace MinamotoBoard.Ink
{
    // Snapshot once per drag. Page changes always acquire the current stroke collection.
    internal sealed class InstrumentSnap
    {
        internal static readonly Guid LineMarker = new Guid("a686db5b-6276-4fd0-ae30-661eac3a9982");
        private const double Distance = 14;
        private readonly List<Target> targets = new List<Target>();
        private sealed class Target
        {
            public Rect Bounds = Rect.Empty;
            public readonly List<Segment> Segments = new List<Segment>();
            public void Add(Point a, Point b)
            {
                Bounds.Union(a); Bounds.Union(b);
                Segments.Add(new Segment { A = a, B = b });
            }
        }
        private struct Segment { public Point A, B; }

        public InstrumentSnap(StrokeCollection strokes, bool allInk)
        {
            if (strokes == null) return;
            foreach (Stroke stroke in strokes)
            {
                if (!allInk && (!stroke.ContainsPropertyData(LineMarker) || !object.Equals(stroke.GetPropertyData(LineMarker), 1))) continue;
                Target target = new Target();
                RegionFillStroke fill = stroke as RegionFillStroke;
                if (fill != null)
                {
                    // Fill strokes have placeholder stylus points: use their real boundary.
                    PathGeometry path = fill.Region.GetFlattenedPathGeometry(1, ToleranceType.Absolute);
                    Matrix matrix = path.Transform.Value;
                    foreach (PathFigure figure in path.Figures)
                    {
                        Point start = matrix.Transform(figure.StartPoint), last = start;
                        foreach (PathSegment segment in figure.Segments)
                        {
                            PolyLineSegment poly = segment as PolyLineSegment;
                            LineSegment line = segment as LineSegment;
                            if (poly != null) foreach (Point point in poly.Points)
                            { Point next = matrix.Transform(point); target.Add(last, next); last = next; }
                            else if (line != null)
                            { Point next = matrix.Transform(line.Point); target.Add(last, next); last = next; }
                        }
                        if (figure.IsClosed) target.Add(last, start);
                    }
                }
                else
                {
                    // Collapse collinear samples, retaining reversals and curved ink segments.
                    Point start = (Point)stroke.StylusPoints[0], last = start;
                    for (int i = 1; i < stroke.StylusPoints.Count; i++)
                    {
                        Point next = (Point)stroke.StylusPoints[i];
                        if ((last - start).LengthSquared > 0.01 &&
                            ((Project(last, start, next) - last).Length > 0.5 || Vector.Multiply(last - start, next - last) < 0))
                        { target.Add(start, last); start = last; }
                        last = next;
                    }
                    target.Add(start, last);
                }
                if (target.Segments.Count > 0) targets.Add(target);
            }
        }

        private static Point Project(Point p, Point a, Point b)
        {
            Vector ab = b - a;
            if (ab.LengthSquared < 0.000001) return a;
            return a + ab * Math.Max(0, Math.Min(1, Vector.Multiply(p - a, ab) / ab.LengthSquared));
        }

        public Point SnapPoint(Point point)
        {
            Point result = point; double best = Distance;
            Rect area = new Rect(point, point); area.Inflate(Distance, Distance);
            foreach (Target target in targets)
            {
                if (!area.IntersectsWith(target.Bounds)) continue;
                foreach (Segment segment in target.Segments)
                {
                    Point near = Project(point, segment.A, segment.B);
                    double distance = (near - point).Length;
                    if (distance < best) { best = distance; result = near; }
                }
            }
            return result;
        }

        public void SnapEdges(Point[] vertices, ref Point center, ref double angle)
        {
            Rect area = Rect.Empty;
            foreach (Point point in vertices) area.Union(point);
            area.Inflate(Distance, Distance);
            double best = double.MaxValue, rotation = 0;
            Point pivot = new Point(), destination = new Point();
            foreach (Target target in targets)
            {
                if (!area.IntersectsWith(target.Bounds)) continue;
                foreach (Segment segment in target.Segments)
                {
                    Rect bounds = new Rect(segment.A, segment.B);
                    if (!area.IntersectsWith(bounds)) continue;
                    Vector direction = segment.B - segment.A;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        Point a = vertices[i], b = vertices[(i + 1) % vertices.Length];
                        Vector edge = b - a;
                        double delta = direction.LengthSquared < 0.01 ? 0 : Vector.AngleBetween(edge, direction);
                        while (delta > 90) delta -= 180;
                        while (delta < -90) delta += 180;
                        if (Math.Abs(delta) > 10) continue;
                        for (int j = 0; j < 4; j++)
                        {
                            Point from = j == 0 ? a : j == 1 ? b : Project(j == 2 ? segment.A : segment.B, a, b);
                            Point to = j < 2 ? Project(from, segment.A, segment.B) : j == 2 ? segment.A : segment.B;
                            double distance = (from - to).Length;
                            double score = distance + Math.Abs(delta) * 0.4;
                            if (distance > Distance || score >= best) continue;
                            best = score; rotation = delta; pivot = from; destination = to;
                        }
                    }
                }
            }
            if (best == double.MaxValue) return;
            double radians = rotation * Math.PI / 180;
            Vector offset = center - pivot;
            center = destination + new Vector(offset.X * Math.Cos(radians) - offset.Y * Math.Sin(radians),
                offset.X * Math.Sin(radians) + offset.Y * Math.Cos(radians));
            angle += rotation;
        }
    }
}
