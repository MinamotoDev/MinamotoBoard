using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace MinamotoBoard.Ink
{
    // Append vector sweeps as input arrives. The same drawing is retained after pen-up.
    public sealed class LiveHighlighterStroke : Stroke
    {
        private DrawingGroup drawing;
        private Geometry previousSweep;
        private Vector direction;
        private int rendered;
        private DrawingAttributes renderedAttributes;
        private bool appending;
        private Rect bounds = Rect.Empty;
        private readonly bool dashed;
        private bool dashPaint = true;
        private double dashRemaining;

        public LiveHighlighterStroke(StylusPointCollection points, DrawingAttributes attributes) : this(points, attributes, false) { }

        public LiveHighlighterStroke(StylusPointCollection points, DrawingAttributes attributes, bool dashed) : base(points, attributes)
        {
            this.dashed = dashed;
            StylusPointsChanged += delegate { if (!appending) { bounds = Rect.Empty; ResetDrawing(); } };
            StylusPointsReplaced += delegate { bounds = Rect.Empty; ResetDrawing(); };
            DrawingAttributesChanged += delegate { bounds = Rect.Empty; ResetDrawing(); };
            DrawingAttributesReplaced += delegate { bounds = Rect.Empty; ResetDrawing(); };
        }

        public void Append(Point point)
        {
            GetBounds();
            bounds.Union(NibBounds(point));
            appending = true;
            try { StylusPoints.Add(new StylusPoint(point.X, point.Y)); }
            finally { appending = false; }
        }
        private Rect NibBounds(Point point)
        { return new Rect(point.X - DrawingAttributes.Width / 2, point.Y - DrawingAttributes.Height / 2, DrawingAttributes.Width, DrawingAttributes.Height); }
        public override Rect GetBounds()
        {
            if (bounds.IsEmpty) foreach (StylusPoint point in StylusPoints) bounds.Union(NibBounds((Point)point));
            return bounds;
        }

        private void ResetDrawing() { drawing = null; previousSweep = null; direction = new Vector(); rendered = 0; dashPaint = true; dashRemaining = 0; }

        protected override void DrawCore(DrawingContext context, DrawingAttributes attributes)
        {
            if (drawing == null || rendered > StylusPoints.Count || renderedAttributes == null
                || renderedAttributes.Color != attributes.Color || renderedAttributes.Width != attributes.Width
                || renderedAttributes.Height != attributes.Height)
            {
                ResetDrawing(); drawing = new DrawingGroup(); renderedAttributes = attributes.Clone();
            }
            Brush brush = new SolidColorBrush(attributes.Color); brush.Freeze();
            while (rendered < StylusPoints.Count)
            {
                Point end = (Point)StylusPoints[rendered];
                Point start = rendered == 0 ? end : (Point)StylusPoints[rendered - 1];
                Vector step = end - start;
                double distance = step.Length;
                if (rendered == 0) PaintSweep(start, end, attributes, brush);
                else if (distance > 0.0001)
                {
                    if (!dashed) PaintSweep(start, end, attributes, brush);
                    else
                    {
                        double dashLength = Math.Max(8, attributes.Width * 4);
                        if (dashRemaining <= 0) dashRemaining = dashLength;
                        Vector unit = step / distance;
                        Point position = start;
                        while (distance > 0.0001)
                        {
                            double amount = Math.Min(distance, dashRemaining);
                            Point next = position + unit * amount;
                            if (dashPaint) PaintSweep(position, next, attributes, brush);
                            position = next; distance -= amount; dashRemaining -= amount;
                            if (dashRemaining < 0.0001)
                            {
                                dashPaint = !dashPaint; dashRemaining = dashPaint ? dashLength : dashLength / 2;
                                previousSweep = null; direction = new Vector();
                            }
                        }
                    }
                }
                rendered++;
            }
            context.DrawDrawing(drawing);
        }

        private void PaintSweep(Point start, Point end, DrawingAttributes attributes, Brush brush)
        {
            Vector step = end - start;
            Geometry sweep;
            if (attributes.StylusTip == StylusTip.Rectangle) sweep = Sweep(start, end, attributes.Width / 2, attributes.Height / 2);
            else sweep = new Stroke(new StylusPointCollection(new[] { new StylusPoint(start.X, start.Y), new StylusPoint(end.X, end.Y) }), attributes).GetGeometry();
            bool reversal = step.LengthSquared > 0 && direction.LengthSquared > 0 && Vector.Multiply(step, direction) < 0;
            // Opaque pen segments can overlap directly; subtracting their joins creates antialiasing seams.
            Geometry fresh = attributes.Color.A == 255 || previousSweep == null || reversal ? sweep
                : Geometry.Combine(sweep, previousSweep, GeometryCombineMode.Exclude, null, 0.05, ToleranceType.Absolute);
            fresh.Freeze(); drawing.Children.Add(new GeometryDrawing(brush, null, fresh));
            previousSweep = sweep;
            if (step.LengthSquared > 0) direction = step;
        }

        private static Geometry Sweep(Point a, Point b, double x, double y)
        {
            // Convex hull of the rectangular nib at both ends, including fast pointer moves.
            List<Point> points = new List<Point> { new Point(a.X-x,a.Y-y), new Point(a.X+x,a.Y-y),
                new Point(a.X+x,a.Y+y), new Point(a.X-x,a.Y+y), new Point(b.X-x,b.Y-y),
                new Point(b.X+x,b.Y-y), new Point(b.X+x,b.Y+y), new Point(b.X-x,b.Y+y) };
            points.Sort(delegate(Point p, Point q) { int result = p.X.CompareTo(q.X); return result != 0 ? result : p.Y.CompareTo(q.Y); });
            List<Point> hull = new List<Point>();
            foreach (Point p in points)
            {
                while (hull.Count >= 2 && Vector.CrossProduct(hull[hull.Count-1]-hull[hull.Count-2], p-hull[hull.Count-1]) <= 0) hull.RemoveAt(hull.Count-1);
                hull.Add(p);
            }
            int lower = hull.Count;
            for (int i = points.Count-2; i >= 0; i--)
            {
                Point p = points[i];
                while (hull.Count > lower && Vector.CrossProduct(hull[hull.Count-1]-hull[hull.Count-2], p-hull[hull.Count-1]) <= 0) hull.RemoveAt(hull.Count-1);
                hull.Add(p);
            }
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext path = geometry.Open())
            {
                path.BeginFigure(hull[0], true, true);
                for (int i = 1; i < hull.Count-1; i++) path.LineTo(hull[i], true, false);
            }
            geometry.Freeze(); return geometry;
        }

        public override Stroke Clone() { return new LiveHighlighterStroke(StylusPoints.Clone(), DrawingAttributes.Clone(), dashed); }
    }

    public sealed class HighlighterCanvas : InkCanvas
    {
        public bool LiveHighlighter { get; set; }
        public bool DashedInk { get; set; }
        public int InkStyle { get; set; }
        public bool EraserCursor { get; set; }
        public double EraserDiameter { get; set; }
        private bool touchInput, cursorIsEraser;
        private Stroke current;
        private int pointer;
        private TouchDevice touch;
        private Cursor highlighterCursor;
        private Color cursorColor;
        private double cursorWidth, cursorHeight, cursorDpiX, cursorDpiY;

        public HighlighterCanvas()
        {
            Loaded += delegate { UpdateHighlighterCursor(); };
            Unloaded += delegate { ReleaseHighlighterCursor(); };
        }

        public void UpdateHighlighterCursor()
        {
            if (touchInput)
            {
                UseCustomCursor = true; Cursor = Cursors.None; Mouse.UpdateCursor(); return;
            }
            if (!LiveHighlighter && !DashedInk && !EraserCursor) { ReleaseHighlighterCursor(); UseCustomCursor = false; ClearValue(CursorProperty); return; }
            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            DrawingAttributes attributes = DefaultDrawingAttributes;
            double width = EraserCursor ? EraserDiameter : attributes.Width;
            if (highlighterCursor == null || cursorIsEraser != EraserCursor || cursorColor != attributes.Color || cursorWidth != width
                || cursorHeight != attributes.Height || cursorDpiX != dpi.DpiScaleX || cursorDpiY != dpi.DpiScaleY)
            {
                Cursor replacement = EraserCursor ? HighlighterCursor.CreateEraser(EraserDiameter, dpi.DpiScaleX, dpi.DpiScaleY) : HighlighterCursor.Create(attributes, dpi.DpiScaleX, dpi.DpiScaleY);
                Cursor old = highlighterCursor;
                highlighterCursor = replacement; Cursor = replacement; UseCustomCursor = true;
                cursorIsEraser = EraserCursor; cursorColor = attributes.Color; cursorWidth = width; cursorHeight = attributes.Height;
                cursorDpiX = dpi.DpiScaleX; cursorDpiY = dpi.DpiScaleY;
                Mouse.UpdateCursor();
                if (old != null) old.Dispose();
            }
            else { Cursor = highlighterCursor; UseCustomCursor = true; Mouse.UpdateCursor(); }
        }

        private void ReleaseHighlighterCursor()
        {
            if (highlighterCursor == null) return;
            UseCustomCursor = false; ClearValue(CursorProperty); Mouse.UpdateCursor();
            highlighterCursor.Dispose(); highlighterCursor = null;
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        { base.OnDpiChanged(oldDpi, newDpi); UpdateHighlighterCursor(); }

        private bool Begin(Point point, int device)
        {
            if ((!LiveHighlighter && !DashedInk) || pointer != 0) return false;
            pointer = device;
            current = new LiveHighlighterStroke(new StylusPointCollection(new[] { new StylusPoint(point.X, point.Y) }), DefaultDrawingAttributes.Clone(), InkStyle == 1);
            Strokes.Add(current); return true;
        }
        private void Move(Point point)
        {
            if (current == null) return;
            Point last = (Point)current.StylusPoints[current.StylusPoints.Count-1];
            if ((point-last).LengthSquared > 0.0001) AppendToolPoint(current, point);
        }
        private void Finish()
        {
            Stroke finished = current;
            current = null; pointer = 0;
            if (IsMouseCaptured) ReleaseMouseCapture();
            if (IsStylusCaptured) ReleaseStylusCapture();
            if (touch != null) { TouchDevice old = touch; touch = null; old.Capture(null); }
            if (finished != null) OnStrokeCollected(new InkCanvasStrokeCollectedEventArgs(finished));
        }
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.StylusDevice == null) ShowPointerCursor();
            base.OnPreviewMouseLeftButtonDown(e);
            if (e.StylusDevice == null && Begin(e.GetPosition(this), 1)) { CaptureMouse(); e.Handled = true; }
        }
        protected override void OnPreviewMouseMove(MouseEventArgs e)
        { if (e.StylusDevice == null) ShowPointerCursor(); base.OnPreviewMouseMove(e); if (pointer == 1 && e.StylusDevice == null) { Move(e.GetPosition(this)); e.Handled = true; } }
        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        { base.OnPreviewMouseLeftButtonUp(e); if (pointer == 1 && e.StylusDevice == null) { Move(e.GetPosition(this)); Finish(); e.Handled = true; } }
        protected override void OnPreviewStylusDown(StylusDownEventArgs e)
        {
            if (e.StylusDevice.TabletDevice.Type == TabletDeviceType.Touch) HideTouchCursor(); else ShowPointerCursor();
            base.OnPreviewStylusDown(e);
            if (!e.Inverted && e.StylusDevice.TabletDevice.Type == TabletDeviceType.Stylus && Begin(e.GetPosition(this), 2)) { CaptureStylus(); e.Handled = true; }
        }
        protected override void OnPreviewStylusMove(StylusEventArgs e)
        {
            base.OnPreviewStylusMove(e);
            if (pointer != 2) return;
            foreach (StylusPoint point in e.GetStylusPoints(this)) Move((Point)point);
            e.Handled = true;
        }
        protected override void OnPreviewStylusUp(StylusEventArgs e)
        {
            base.OnPreviewStylusUp(e);
            if (pointer != 2) return;
            foreach (StylusPoint point in e.GetStylusPoints(this)) Move((Point)point);
            Finish(); e.Handled = true;
        }
        public void NotifyPointerInput(bool touch) { if (touch) HideTouchCursor(); else ShowPointerCursor(); }
        public static Stroke CreateToolStroke(Point point, DrawingToolOptions options, bool guided)
        {
            StylusPointCollection points = new StylusPointCollection(new[] { new StylusPoint(point.X, point.Y) });
            DrawingAttributes attributes = options.Attributes();
            // Guide coordinates already describe an exact line or arc; curve fitting can deform its ends.
            if (guided) attributes.FitToCurve = false;
            return options.Highlighter || options.Style == 1 ? new LiveHighlighterStroke(points, attributes, options.Style == 1) : new Stroke(points, attributes);
        }
        public static void AppendToolPoint(Stroke stroke, Point point)
        {
            Point last = (Point)stroke.StylusPoints[stroke.StylusPoints.Count - 1];
            if ((point - last).LengthSquared <= 0.0001) return;
            LiveHighlighterStroke custom = stroke as LiveHighlighterStroke;
            if (custom != null) custom.Append(point); else stroke.StylusPoints.Add(new StylusPoint(point.X, point.Y));
        }
        private void HideTouchCursor() { if (!touchInput) { touchInput = true; UpdateHighlighterCursor(); } }
        private void ShowPointerCursor() { if (touchInput && !AreAnyTouchesCaptured) { touchInput = false; UpdateHighlighterCursor(); } }
        protected override void OnPreviewStylusInAirMove(StylusEventArgs e)
        { if (e.StylusDevice.TabletDevice.Type == TabletDeviceType.Stylus) ShowPointerCursor(); base.OnPreviewStylusInAirMove(e); }
        protected override void OnPreviewTouchDown(TouchEventArgs e)
        { HideTouchCursor(); base.OnPreviewTouchDown(e); if (Begin(e.GetTouchPoint(this).Position, 3)) { touch = e.TouchDevice; touch.Capture(this); e.Handled = true; } }
        protected override void OnPreviewTouchMove(TouchEventArgs e)
        {
            base.OnPreviewTouchMove(e);
            if (pointer != 3 || touch != e.TouchDevice) return;
            foreach (TouchPoint point in e.GetIntermediateTouchPoints(this)) Move(point.Position);
            Move(e.GetTouchPoint(this).Position); e.Handled = true;
        }
        protected override void OnPreviewTouchUp(TouchEventArgs e)
        { base.OnPreviewTouchUp(e); if (pointer == 3 && touch == e.TouchDevice) { Move(e.GetTouchPoint(this).Position); Finish(); e.Handled = true; } }
        protected override void OnLostMouseCapture(MouseEventArgs e) { if (pointer == 1) Finish(); base.OnLostMouseCapture(e); }
        protected override void OnLostStylusCapture(StylusEventArgs e) { if (pointer == 2) Finish(); base.OnLostStylusCapture(e); }
        protected override void OnLostTouchCapture(TouchEventArgs e) { if (pointer == 3 && touch == e.TouchDevice) Finish(); base.OnLostTouchCapture(e); }
    }
}
