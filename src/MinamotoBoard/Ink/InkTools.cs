using System;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;

namespace MinamotoBoard.Ink
{
    public sealed class InkTools
    {
        private readonly InkCanvas canvas;
        public static readonly double[] PenWidths = { 2, 4, 6, 10, 16 };
        public static readonly double[] EraserWidths = { 16, 32, 64 };

        public InkTools(InkCanvas canvas)
        {
            if (canvas == null) throw new ArgumentNullException("canvas");
            this.canvas = canvas;
            canvas.DefaultDrawingAttributes = new DrawingAttributes
            {
                Color = Colors.White, Width = 4, Height = 4,
                FitToCurve = false, IgnorePressure = true,
                StylusTip = StylusTip.Ellipse
            };
            SetEraserWidth(32);
            UsePen();
        }

        public void UsePen()
        {
            canvas.EditingMode = InkCanvasEditingMode.Ink;
            canvas.EditingModeInverted = InkCanvasEditingMode.EraseByPoint;
        }

        public void UseEraser()
        {
            canvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            canvas.EditingModeInverted = InkCanvasEditingMode.EraseByPoint;
        }

        public void SetColor(Color color) { canvas.DefaultDrawingAttributes.Color = color; }

        public void SetPenWidth(double width)
        {
            if (double.IsNaN(width) || double.IsInfinity(width) || width < 1 || width > 40) throw new ArgumentOutOfRangeException("width");
            canvas.DefaultDrawingAttributes.Width = width;
            canvas.DefaultDrawingAttributes.Height = width;
        }

        public void SetEraserWidth(double width)
        {
            if (double.IsNaN(width) || double.IsInfinity(width) || width < 8 || width > 128) throw new ArgumentOutOfRangeException("width");
            canvas.EraserShape = new EllipseStylusShape(width, width);
        }
    }
}
