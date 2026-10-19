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
        private double length = 640;
        private double triangleScale = 1;
        private bool positioned;
        private readonly Dictionary<BoardTool, InstrumentState> states = new Dictionary<BoardTool, InstrumentState>();
        private bool triangle30;
        private BoardTool tool;
        private bool active;
        private DrawingToolOptions options;
        private BoardTool writingTool = BoardTool.Pen;
        private Stroke liveStroke;
        private int operation; // 1 move, 2 resize/rotate, 3 draw, 4 delete.
        private int pointer; // mouse=1, pen=2, touch=3.
        private TouchDevice touch;
        private Point origin, previous;
        private Point edgeA, edgeB;
        private double lastAngle, arcStart, swept;
        private readonly List<Point> preview = new List<Point>();
        public event Action<StrokeCollection> InkCreated;
        public event Action StateChanged;
        public event Action DeleteRequested;
        public event Action<bool> PointerInput;

        public void SetWritingTool(BoardTool kind, DrawingToolOptions settings)
        { writingTool = kind; options = settings; }
        public bool IsInteracting { get { return pointer != 0; } }
        public double Angle { get { return angle; } set { angle = value; InvalidateVisual(); } }
        public double Length { get { return length; } set { length = value; InvalidateVisual(); } }
        public double Radius { get { return radius; } set { radius = value; InvalidateVisual(); } }
        public bool Triangle30 { get { return triangle30; } set { triangle30 = value; InvalidateVisual(); } }

        public void Activate(BoardTool kind, DrawingToolOptions settings)
        {
            CancelInteraction();
            if (tool != kind) { SaveState(); tool = kind; LoadState(); }
            options = settings;
            active = InstrumentLayer.IsInstrument(kind);
            triangle30 = kind == BoardTool.SetSquare30;
            IsHitTestVisible = active;
            if (!active) { InvalidateVisual(); return; }
            if (!positioned && ActualWidth > 0 && ActualHeight > 0) { center = new Point(ActualWidth / 2, ActualHeight / 2); positioned = true; }
            if (ActualWidth > 0 && ActualHeight > 0) ClampCenter();
            InvalidateVisual();
        }

        private void SaveState()
        {
            if (!active) return;
            states[tool] = new InstrumentState { Tool = tool, Positioned = positioned, X = center.X, Y = center.Y,
                Angle = angle, Length = length, Scale = triangleScale, Radius = radius, Triangle30 = triangle30 };
        }
        private void LoadState()
        {
            InstrumentState state;
            if (!states.TryGetValue(tool, out state)) state = new InstrumentState { Tool = tool };
            positioned = state.Positioned; center = new Point(state.X, state.Y); angle = state.Angle;
            length = state.Length; triangleScale = state.Scale; radius = state.Radius; triangle30 = state.Triangle30;
        }
        public List<InstrumentState> ExportStates() { SaveState(); return new List<InstrumentState>(states.Values); }
        public void ImportStates(List<InstrumentState> saved)
        {
            if (saved == null) return;
            foreach (InstrumentState state in saved)
            {
                if (state == null || !ToolPreferences.Finite(state.X) || !ToolPreferences.Finite(state.Y) || !ToolPreferences.Finite(state.Angle)
                    || !ToolPreferences.Finite(state.Length) || !ToolPreferences.Finite(state.Scale) || !ToolPreferences.Finite(state.Radius)) continue;
                state.Length = Math.Max(640, state.Length); state.Scale = Math.Max(1, state.Scale); state.Radius = Math.Max(0, state.Radius);
                states[state.Tool] = state;
            }
        }

        public void CancelInteraction()
        {
            bool changed = pointer != 0;
            pointer = 0; operation = 0; preview.Clear(); liveStroke = null;
            if (IsMouseCaptured) ReleaseMouseCapture();
            if (IsStylusCaptured) ReleaseStylusCapture();
            if (touch != null) { TouchDevice old = touch; touch = null; old.Capture(null); }
            if (changed && StateChanged != null) StateChanged();
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
            return new[] { new Point(-130 * triangleScale, 110 * triangleScale), new Point(150 * triangleScale, 110 * triangleScale), new Point(-130 * triangleScale, (triangle30 ? -52 : -170) * triangleScale) };
        }

        private Point Handle { get { return tool == BoardTool.Ruler ? new Point(length / 2 + 24, 0) : new Point(176 * triangleScale, 110 * triangleScale); } }

        private Point CompassHandle { get { return new Point(Math.Max(32, radius), 0); } }
        private Point DeleteHandle { get { return tool == BoardTool.Compass ? new Point(radius / 2, -55) : tool == BoardTool.Ruler ? new Point(-length / 2 - 24, 0) : new Point(-156 * triangleScale, 110 * triangleScale); } }
        private void ClampCenter()
        {
            center.X = Math.Max(30, Math.Min(Math.Max(30, ActualWidth - 30), center.X));
            center.Y = Math.Max(30, Math.Min(Math.Max(30, ActualHeight - 100), center.Y));
        }

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
            // Painting and erasing must reach ink beneath the guides, including the compass center.
            if (writingTool == BoardTool.Bucket || writingTool == BoardTool.Eraser)
            {
                bool control = (p - DeleteHandle).Length <= 16 || (p - (tool == BoardTool.Compass ? CompassHandle : Handle)).Length <= 16;
                return control ? new PointHitTestResult(this, parameters.HitPoint) : null;
            }
            bool hit = tool == BoardTool.Compass
                ? (p - new Point(radius / 2, -55)).Length <= 16 || p.ToVector().Length <= 16 || (p - CompassHandle).Length <= 16 || Math.Abs(p.ToVector().Length - radius) <= 14
                : Body().FillContains(p) || Body().StrokeContains(new Pen(Brushes.Black, 24), p) || (p - Handle).Length <= 16;
            hit = hit || (p - DeleteHandle).Length <= 16;
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
                if (radius < 32) dc.DrawLine(outline, new Point(radius, 0), CompassHandle);
                dc.DrawEllipse(Brushes.DodgerBlue, outline, CompassHandle, 10, 10);
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
                    if (tool != BoardTool.Ruler) normal = -normal;
                    // Only visible ticks are visited, regardless of the instrument size.
                    Point[] corners = { Local(new Point(-20, -20)), Local(new Point(ActualWidth + 20, -20)), Local(new Point(-20, ActualHeight + 20)), Local(new Point(ActualWidth + 20, ActualHeight + 20)) };
                    double first = distance, last = 0;
                    foreach (Point corner in corners) { double d = Vector.Multiply(corner - a, direction); first = Math.Min(first, d); last = Math.Max(last, d); }
                    double beginTick = Math.Max(1, Math.Floor(first / 10));
                    double endTick = Math.Min(distance / 10, Math.Ceiling(last / 10));
                    for (double tick = beginTick; tick < endTick; tick++)
                    {
                        Point start = a + direction * (tick * 10);
                        dc.DrawLine(outline, start, start + normal * (tick % 5 == 0 ? 14 : 7));
                    }
                }
                dc.DrawLine(outline, tool == BoardTool.Ruler ? new Point(length / 2, 0) : vertices[1], Handle);
                dc.DrawEllipse(Brushes.DodgerBlue, outline, Handle, 10, 10);
            }
            Point delete = DeleteHandle;
            if (tool != BoardTool.Compass) dc.DrawLine(outline, tool == BoardTool.Ruler ? new Point(-length / 2, 0) : Vertices()[0], delete);
            dc.DrawEllipse(Brushes.IndianRed, outline, delete, 10, 10);
            dc.DrawLine(outline, delete + new Vector(-3.5, -3.5), delete + new Vector(3.5, 3.5));
            dc.DrawLine(outline, delete + new Vector(-3.5, 3.5), delete + new Vector(3.5, -3.5));
            dc.Pop(); dc.Pop();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (!active || pointer != 0) return;
            if (!positioned && ActualWidth > 0 && ActualHeight > 0)
            { center = new Point(ActualWidth / 2, ActualHeight / 2); positioned = true; }
            ClampCenter();
            if (StateChanged != null) StateChanged();
            InvalidateVisual();
        }

        private bool Begin(Point p, int kind)
        {
            if (!active || pointer != 0) return false;
            pointer = kind; origin = p; previous = center; preview.Clear();
            if (PointerInput != null) PointerInput(kind == 3);
            Point local = Local(p);
            if ((local - DeleteHandle).Length <= 16) { operation = 4; InvalidateVisual(); return true; }
            if (tool == BoardTool.Compass)
            {
                if ((local - CompassHandle).Length <= 16) operation = 2;
                else if (local.ToVector().Length <= 16) operation = 1;
                else
                {
                    operation = 3; swept = 0; lastAngle = Math.Atan2(p.Y - center.Y, p.X - center.X);
                    arcStart = lastAngle;
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
                if (operation == 3) preview.Add(World(Project(local, edgeA, edgeB)));
            }
            if (operation == 3 && writingTool != BoardTool.Pen && writingTool != BoardTool.Highlighter) { operation = 1; preview.Clear(); }
            if (operation == 3 && preview.Count > 0)
            {
                liveStroke = HighlighterCanvas.CreateToolStroke(preview[0], options, true);
                if (InkCreated != null) InkCreated(new StrokeCollection { liveStroke });
            }
            InvalidateVisual(); return true;
        }

        private void Move(Point p)
        {
            if (pointer == 0 || operation == 4) return;
            if (operation == 1)
            {
                center = previous + (p - origin);
                center.X = Math.Max(15, Math.Min(ActualWidth - 15, center.X));
                center.Y = Math.Max(15, Math.Min(ActualHeight - 15, center.Y));
            }
            else if (operation == 2)
            {
                double worldAngle = Math.Atan2(p.Y - center.Y, p.X - center.X) * 180 / Math.PI;
                double distance = (p - center).Length;
                if (tool == BoardTool.Compass) { radius = distance; angle = worldAngle; }
                else if (tool == BoardTool.Ruler) { length = Math.Max(640, (distance - 24) * 2); angle = worldAngle; }
                else { triangleScale = Math.Max(1, distance / Math.Sqrt(176 * 176 + 110 * 110)); angle = worldAngle - Math.Atan2(110, 176) * 180 / Math.PI; }
            }
            else if (tool == BoardTool.Compass)
            {
                double current = Math.Atan2(p.Y - center.Y, p.X - center.X), delta = current - lastAngle;
                while (delta > Math.PI) delta -= Math.PI * 2;
                while (delta < -Math.PI) delta += Math.PI * 2;
                double allowed = Math.Max(-Math.PI * 2, Math.Min(Math.PI * 2, swept + delta));
                delta = allowed - swept;
                lastAngle = current;
                if (Math.Abs(delta) < 0.0000001) return;
                int count = Math.Max(1, (int)Math.Min(128, Math.Ceiling(Math.Abs(delta) * Math.Max(radius, 30) / 3)));
                for (int i = 1; i <= count; i++)
                {
                    double a = arcStart + swept + delta * i / count;
                    AddInkPoint(center + new Vector(Math.Cos(a) * radius, Math.Sin(a) * radius));
                }
                swept = allowed;
            }
            else if (operation == 3) AddInkPoint(World(Project(Local(p), edgeA, edgeB)));
            InvalidateVisual();
        }

        private void AddInkPoint(Point point)
        {
            if (liveStroke == null) return;
            HighlighterCanvas.AppendToolPoint(liveStroke, point);
        }

        private void Finish(Point p)
        {
            Move(p);
            bool delete = operation == 4 && (Local(p) - DeleteHandle).Length <= 16;
            CancelInteraction();
            if (StateChanged != null) StateChanged();
            if (delete && DeleteRequested != null) DeleteRequested();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        { if (e.StylusDevice == null && Begin(e.GetPosition(this), 1)) { CaptureMouse(); e.Handled = true; } }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.StylusDevice == null && PointerInput != null) PointerInput(false);
            if (pointer == 1) { Move(e.GetPosition(this)); e.Handled = true; }
        }
        protected override void OnStylusInAirMove(StylusEventArgs e)
        { if (e.StylusDevice.TabletDevice.Type == TabletDeviceType.Stylus && PointerInput != null) PointerInput(false); base.OnStylusInAirMove(e); }
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        { if (pointer == 1) { Finish(e.GetPosition(this)); e.Handled = true; } }
        protected override void OnStylusDown(StylusDownEventArgs e)
        { if (e.StylusDevice.TabletDevice.Type == TabletDeviceType.Stylus && Begin(e.GetPosition(this), 2)) { CaptureStylus(); e.Handled = true; } }
        protected override void OnStylusMove(StylusEventArgs e)
        { if (pointer == 2) { foreach (StylusPoint point in e.GetStylusPoints(this)) Move((Point)point); e.Handled = true; } }
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
