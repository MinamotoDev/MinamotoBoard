using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace MinamotoBoard.Ink
{
    public sealed class GeometryOverlay : FrameworkElement
    {
        private Point start;
        private int pointer;
        private TouchDevice touch;
        private List<GeometryShapes.ShapePath> paths;
        private Color color;
        private double width;
        private bool guides;
        public bool ShowGuides { get; set; }
        public bool StandardShape { get; set; }
        private bool standard;
        public GeometryShape Shape { get; set; }
        public DrawingToolOptions Options { get; set; }
        public bool IsDrawing { get { return pointer != 0; } }
        public event Action<StrokeCollection> InkCreated;
        public event Action DrawingStarted;
        public event Action DrawingFinished;

        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
            if (paths == null) return;
            foreach (GeometryShapes.ShapePath path in paths)
            {
                StreamGeometry geometry = new StreamGeometry();
                using (StreamGeometryContext context = geometry.Open())
                {
                    if (path.Points.Count == 0) continue;
                    context.BeginFigure(path.Points[0], false, false);
                    for (int i = 1; i < path.Points.Count; i++) context.LineTo(path.Points[i], true, false);
                }
                double pathWidth = path.Guide ? GeometryShapes.GuideWidth(width) : width;
                Pen pen = new Pen(new SolidColorBrush(color), pathWidth)
                { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, DashCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
                if (path.Dashed)
                    pen.DashStyle = new DashStyle(new[] { Math.Max(6, pathWidth * 2.5) / pathWidth, Math.Max(4, pathWidth * 1.7) / pathWidth }, 0);
                dc.DrawGeometry(null, pen, geometry);
            }
        }
        private bool Begin(Point point, int kind)
        {
            if (pointer != 0 || Options == null) return false;
            start = point; pointer = kind; color = Options.Color; width = Options.Width;
            guides = ShowGuides;
            standard = StandardShape;
            paths = null;
            if (DrawingStarted != null) DrawingStarted();
            return true;
        }
        private void Move(Point point)
        {
            if (pointer == 0) return;
            paths = GeometryShapes.Build(Shape, start, point, guides, standard); InvalidateVisual();
        }
        private void Finish(Point point)
        {
            Move(point);
            try
            {
                if ((point - start).Length >= 5 && paths != null)
                {
                    StrokeCollection strokes = GeometryShapes.Ink(paths, color, width);
                    if (strokes.Count > 0 && InkCreated != null) InkCreated(strokes);
                }
            }
            finally { CancelDrawing(); }
        }
        public void CancelDrawing()
        {
            bool drawing = pointer != 0;
            pointer = 0; paths = null;
            if (IsMouseCaptured) ReleaseMouseCapture();
            if (IsStylusCaptured) ReleaseStylusCapture();
            if (touch != null) { TouchDevice old = touch; touch = null; old.Capture(null); }
            InvalidateVisual();
            if (drawing && DrawingFinished != null) DrawingFinished();
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null) return;
            if (Begin(e.GetPosition(this), 1)) CaptureMouse();
            e.Handled = true;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        { if (pointer == 1) { Move(e.GetPosition(this)); e.Handled = true; } }
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        { if (pointer == 1) { Finish(e.GetPosition(this)); e.Handled = true; } }
        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            if (e.StylusDevice.TabletDevice.Type != TabletDeviceType.Stylus) return;
            if (Begin(e.GetPosition(this), 2)) CaptureStylus();
            e.Handled = true;
        }
        protected override void OnStylusMove(StylusEventArgs e)
        { if (pointer == 2) { Move(e.GetPosition(this)); e.Handled = true; } }
        protected override void OnStylusUp(StylusEventArgs e)
        { if (pointer == 2) { Finish(e.GetPosition(this)); e.Handled = true; } }
        protected override void OnTouchDown(TouchEventArgs e)
        {
            if (Begin(e.GetTouchPoint(this).Position, 3)) { touch = e.TouchDevice; touch.Capture(this); }
            e.Handled = true;
        }
        protected override void OnTouchMove(TouchEventArgs e)
        { if (pointer == 3 && touch == e.TouchDevice) Move(e.GetTouchPoint(this).Position); e.Handled = true; }
        protected override void OnTouchUp(TouchEventArgs e)
        { if (pointer == 3 && touch == e.TouchDevice) Finish(e.GetTouchPoint(this).Position); e.Handled = true; }
        protected override void OnLostMouseCapture(MouseEventArgs e)
        { if (pointer == 1) CancelDrawing(); base.OnLostMouseCapture(e); }
        protected override void OnLostStylusCapture(StylusEventArgs e)
        { if (pointer == 2) CancelDrawing(); base.OnLostStylusCapture(e); }
        protected override void OnLostTouchCapture(TouchEventArgs e)
        { if (touch == e.TouchDevice) CancelDrawing(); base.OnLostTouchCapture(e); }
    }
}
