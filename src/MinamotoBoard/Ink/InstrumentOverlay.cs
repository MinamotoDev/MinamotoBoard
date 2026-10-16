using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace MinamotoBoard.Ink
{
    // One lightweight visual. Only the physical instrument and its handles intercept input.
    public sealed class InstrumentOverlay : FrameworkElement
    {
        private Point center;
        private double angle;
        private double radius = 110;
        private double length = 360;
        private bool triangle30;
        private BoardTool tool;
        private bool active;
        private DrawingToolOptions options;
        private int operation; // 1 move, 2 rotate/compass opening, 3 draw.
        private int pointer; // mouse=1, pen=2, touch=3.
        private TouchDevice touch;
        private Point origin, previous, lineStart;
        private Point edgeA, edgeB;
        private double lastAngle, swept;
        private readonly List<Point> preview = new List<Point>();
        public event Action<StrokeCollection> InkCreated;
        public bool IsInteracting { get { return pointer != 0; } }
        public double Angle { get { return angle; } set { angle = value; InvalidateVisual(); } }
        public double Length { get { return length; } set { length = value; InvalidateVisual(); } }
        public double Radius { get { return radius; } set { radius = value; InvalidateVisual(); } }
        public bool Triangle30 { get { return triangle30; } set { triangle30 = value; InvalidateVisual(); } }

        public void Activate(BoardTool kind, DrawingToolOptions settings)
        {
            CancelInteraction(); tool = kind; options = settings;
            active = kind == BoardTool.Ruler || kind == BoardTool.SetSquare || kind == BoardTool.Compass;
            IsHitTestVisible = active;
            if (!active) { InvalidateVisual(); return; }
            if (center == new Point()) center = new Point(Math.Max(240, ActualWidth / 2), Math.Max(220, ActualHeight / 2));
            center.X = Math.Max(30, Math.Min(Math.Max(30, ActualWidth - 30), center.X));
            center.Y = Math.Max(30, Math.Min(Math.Max(30, ActualHeight - 100), center.Y));
            InvalidateVisual();
        }

        public void CancelInteraction()
        {
            pointer = 0; operation = 0; preview.Clear();
            if (IsMouseCaptured) ReleaseMouseCapture();
            if (IsStylusCaptured) ReleaseStylusCapture();
            if (touch != null) { TouchDevice old = touch; touch = null; old.Capture(null); }
            InvalidateVisual();
        }

        private Point Local(Point point)
        {
            double a = -angle * Math.PI / 180;
            Vector p = point - center;
            return new Point(p.X * Math.Cos(a) - p.Y * Math.Sin(a), p.X * Math.Sin(a) + p.Y * Math.Cos(a));
        }

        private Point World(Point point)
        {
            double a = angle * Math.PI / 180;
            return center + new Vector(point.X * Math.Cos(a) - point.Y * Math.Sin(a), point.X * Math.Sin(a) + point.Y * Math.Cos(a));
        }

        private Point[] Vertices()
        {
            if (tool == BoardTool.Ruler) return new[] { new Point(-length / 2, -25), new Point(length / 2, -25), new Point(length / 2, 25), new Point(-length / 2, 25) };
            return new[] { new Point(-130, 110), new Point(150, 110), new Point(-130, triangle30 ? -52 : -170) };
        }

        private Point Handle { get { return tool == BoardTool.Ruler ? new Point(length / 2 + 24, 0) : new Point(176, 110); } }

        private static Point Project(Point p, Point a, Point b)
        {
            Vector ab = b - a;
            double t = Vector.Multiply(p - a, ab) / ab.LengthSquared;
            return a + ab * Math.Max(0, Math.Min(1, t));
        }

        private Geometry Body()
        {
            Point[] vertices = Vertices();
            StreamGeometry shape = new StreamGeometry();
            using (StreamGeometryContext context = shape.Open())
            {
                context.BeginFigure(vertices[0], true, true);
                for (int i = 1; i < vertices.Length; i++) context.LineTo(vertices[i], true, false);
            }
            shape.Freeze(); return shape;
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters parameters)
        {
            if (!active) return null;
            Point p = Local(parameters.HitPoint);
            bool hit = tool == BoardTool.Compass
                ? p.ToVector().Length <= 16 || (p - new Point(radius, 0)).Length <= 16 || Math.Abs(p.ToVector().Length - radius) <= 14
                : Body().FillContains(p) || Body().StrokeContains(new Pen(Brushes.Black, 24), p) || (p - Handle).Length <= 16;
            return hit ? new PointHitTestResult(this, parameters.HitPoint) : null;
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (!active) return;
            // Establish full layout bounds, while HitTestCore lets all non-instrument input pass through.
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
            dc.PushTransform(new TranslateTransform(center.X, center.Y));
            dc.PushTransform(new RotateTransform(angle));
            Pen outline = new Pen(Brushes.White, 1.5);
            Brush face = new SolidColorBrush(Color.FromArgb(60, 220, 235, 240));
            if (tool == BoardTool.Compass)
            {
                Pen guide = new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), 1);
                guide.DashStyle = DashStyles.Dash;
                dc.DrawEllipse(null, guide, new Point(), radius, radius);
                dc.DrawLine(new Pen(Brushes.LightGray, 6), new Point(radius / 2, -55), new Point());
                dc.DrawLine(new Pen(Brushes.LightGray, 6), new Point(radius / 2, -55), new Point(radius, 0));
                dc.DrawEllipse(Brushes.White, outline, new Point(), 8, 8);
                dc.DrawEllipse(Brushes.DodgerBlue, outline, new Point(radius, 0), 10, 10);
            }
            else
            {
                dc.DrawGeometry(face, outline, Body());
                Point[] vertices = Vertices();
                for (int edge = 0; edge < (tool == BoardTool.Ruler ? 1 : 2); edge++)
                {
                    Point a = vertices[edge], b = vertices[(edge + 1) % vertices.Length];
                    Vector direction = b - a; double distance = direction.Length; direction.Normalize();
                    Vector normal = new Vector(-direction.Y, direction.X);
                    if (tool == BoardTool.SetSquare) normal = -normal;
                    for (int tick = 1; tick < distance / 10; tick++)
                    {
                        Point start = a + direction * (tick * 10);
                        dc.DrawLine(outline, start, start + normal * (tick % 5 == 0 ? 14 : 7));
                    }
                }
                dc.DrawLine(outline, tool == BoardTool.Ruler ? new Point(length / 2, 0) : vertices[1], Handle);
                dc.DrawEllipse(Brushes.DodgerBlue, outline, Handle, 10, 10);
            }
            dc.Pop(); dc.Pop();
            if (preview.Count > 1)
            {
                StreamGeometry line = new StreamGeometry();
                using (StreamGeometryContext context = line.Open())
                {
                    context.BeginFigure(preview[0], false, false);
                    for (int i = 1; i < preview.Count; i++) context.LineTo(preview[i], true, false);
                }
                Pen pen = new Pen(new SolidColorBrush(options.Color), options.Width);
                pen.StartLineCap = pen.EndLineCap = PenLineCap.Round;
                if (options.Style == 1) pen.DashStyle = DashStyles.Dash;
                dc.DrawGeometry(null, pen, line);
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (!active || pointer != 0) return;
            center.X = Math.Max(30, Math.Min(Math.Max(30, ActualWidth - 30), center.X));
            center.Y = Math.Max(30, Math.Min(Math.Max(30, ActualHeight - 100), center.Y));
            InvalidateVisual();
        }

        private bool Begin(Point p, int kind)
        {
            if (!active || pointer != 0) return false;
            pointer = kind; origin = p; previous = center; preview.Clear();
            Point local = Local(p);
            if (tool == BoardTool.Compass)
            {
                if (local.ToVector().Length <= 16) operation = 1;
                else if ((local - new Point(radius, 0)).Length <= 16) operation = 2;
                else
                {
                    operation = 3; swept = 0; lastAngle = Math.Atan2(p.Y - center.Y, p.X - center.X);
                    preview.Add(center + new Vector(Math.Cos(lastAngle) * radius, Math.Sin(lastAngle) * radius));
                }
            }
            else if ((local - Handle).Length <= 16) operation = 2;
            else
            {
                Point[] vertices = Vertices(); double best = double.MaxValue;
                for (int i = 0; i < vertices.Length; i++)
                {
                    Point a = vertices[i], b = vertices[(i + 1) % vertices.Length];
                    double distance = (Project(local, a, b) - local).Length;
                    if (distance < best) { best = distance; edgeA = a; edgeB = b; }
                }
                operation = best <= 12 ? 3 : 1;
                if (operation == 3) { lineStart = Project(local, edgeA, edgeB); preview.Add(World(lineStart)); preview.Add(World(lineStart)); }
            }
            InvalidateVisual(); return true;
        }

        private void Move(Point p)
        {
            if (pointer == 0) return;
            if (operation == 1)
            {
                center = previous + (p - origin);
                center.X = Math.Max(15, Math.Min(ActualWidth - 15, center.X));
                center.Y = Math.Max(15, Math.Min(ActualHeight - 15, center.Y));
            }
            else if (operation == 2)
            {
                double worldAngle = Math.Atan2(p.Y - center.Y, p.X - center.X) * 180 / Math.PI;
                if (tool == BoardTool.Compass) { radius = Math.Max(20, Math.Min(500, (p - center).Length)); angle = worldAngle; }
                else angle = worldAngle - Math.Atan2(Handle.Y, Handle.X) * 180 / Math.PI;
            }
            else if (tool == BoardTool.Compass)
            {
                if (preview.Count >= 4096) return;
                double current = Math.Atan2(p.Y - center.Y, p.X - center.X), delta = current - lastAngle;
                while (delta > Math.PI) delta -= Math.PI * 2;
                while (delta < -Math.PI) delta += Math.PI * 2;
                double allowed = Math.Max(-Math.PI * 2, Math.Min(Math.PI * 2, swept + delta));
                delta = allowed - swept;
                int count = Math.Max(1, (int)Math.Ceiling(Math.Abs(delta) * Math.Max(radius, 30) / 3));
                for (int i = 1; i <= count; i++)
                {
                    double a = lastAngle + delta * i / count;
                    preview.Add(center + new Vector(Math.Cos(a) * radius, Math.Sin(a) * radius));
                }
                swept = allowed; lastAngle = current;
            }
            else if (preview.Count > 1) preview[1] = World(Project(Local(p), edgeA, edgeB));
            InvalidateVisual();
        }

        private void Finish(Point p)
        {
            Move(p);
            if (operation == 3 && preview.Count > 1 && (tool == BoardTool.Compass ? Math.Abs(swept) * radius > 2 : (preview[1] - preview[0]).Length > 2))
            {
                StrokeCollection strokes = BuildStrokes(preview, options);
                if (InkCreated != null) InkCreated(strokes);
            }
            CancelInteraction();
        }

        private static StrokeCollection BuildStrokes(List<Point> points, DrawingToolOptions settings)
        {
            StrokeCollection result = new StrokeCollection();
            if (settings.Style != 1)
            {
                StylusPointCollection ink = new StylusPointCollection();
                foreach (Point p in points) ink.Add(new StylusPoint(p.X, p.Y));
                result.Add(new Stroke(ink, settings.Attributes())); return result;
            }
            // Real separated native strokes keep dashed geometry editable by InkCanvas's eraser.
            double dash = Math.Max(8, settings.Width * 4), remaining = dash;
            bool draw = true; StylusPointCollection run = new StylusPointCollection();
            for (int i = 1; i < points.Count; i++)
            {
                Point current = points[i - 1]; Vector segment = points[i] - current; double distance = segment.Length;
                if (distance < 0.01) continue;
                segment.Normalize();
                while (distance > 0.01)
                {
                    double step = Math.Min(distance, remaining); Point next = current + segment * step;
                    if (draw) { if (run.Count == 0) run.Add(new StylusPoint(current.X, current.Y)); run.Add(new StylusPoint(next.X, next.Y)); }
                    current = next; distance -= step; remaining -= step;
                    if (remaining < 0.01)
                    {
                        if (draw && run.Count > 1) result.Add(new Stroke(run, settings.Attributes()));
                        run = new StylusPointCollection(); draw = !draw; remaining = draw ? dash : dash / 2;
                    }
                }
            }
            if (run.Count > 1) result.Add(new Stroke(run, settings.Attributes()));
            return result;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        { if (e.StylusDevice == null && Begin(e.GetPosition(this), 1)) { CaptureMouse(); e.Handled = true; } }
        protected override void OnMouseMove(MouseEventArgs e)
        { if (pointer == 1) { Move(e.GetPosition(this)); e.Handled = true; } }
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        { if (pointer == 1) { Finish(e.GetPosition(this)); e.Handled = true; } }
        protected override void OnStylusDown(StylusDownEventArgs e)
        { if (e.StylusDevice.TabletDevice.Type == TabletDeviceType.Stylus && Begin(e.GetPosition(this), 2)) { CaptureStylus(); e.Handled = true; } }
        protected override void OnStylusMove(StylusEventArgs e)
        { if (pointer == 2) { Move(e.GetPosition(this)); e.Handled = true; } }
        protected override void OnStylusUp(StylusEventArgs e)
        { if (pointer == 2) { Finish(e.GetPosition(this)); e.Handled = true; } }
        protected override void OnTouchDown(TouchEventArgs e)
        { if (Begin(e.GetTouchPoint(this).Position, 3)) { touch = e.TouchDevice; touch.Capture(this); e.Handled = true; } }
        protected override void OnTouchMove(TouchEventArgs e)
        { if (pointer == 3 && touch == e.TouchDevice) { Move(e.GetTouchPoint(this).Position); e.Handled = true; } }
        protected override void OnTouchUp(TouchEventArgs e)
        { if (pointer == 3 && touch == e.TouchDevice) { Finish(e.GetTouchPoint(this).Position); e.Handled = true; } }
        protected override void OnLostMouseCapture(MouseEventArgs e) { if (pointer == 1) CancelInteraction(); base.OnLostMouseCapture(e); }
        protected override void OnLostStylusCapture(StylusEventArgs e) { if (pointer == 2) CancelInteraction(); base.OnLostStylusCapture(e); }
        protected override void OnLostTouchCapture(TouchEventArgs e) { if (pointer == 3 && touch == e.TouchDevice) CancelInteraction(); base.OnLostTouchCapture(e); }
    }

    internal static class PointVector
    {
        public static Vector ToVector(this Point point) { return new Vector(point.X, point.Y); }
    }
}
