using System.Windows.Ink;
using System.Windows.Media;

namespace MinamotoBoard.Ink
{
    public enum BoardTool { Pen, Eraser, Ruler, SetSquare, Compass, Bucket }

    public sealed class DrawingToolOptions
    {
        public Color Color = Colors.White;
        public double Width = 4;
        public int Style;

        public DrawingAttributes Attributes()
        {
            return new DrawingAttributes { Color = Color, Width = Width, Height = Width,
                IgnorePressure = true, FitToCurve = false,
                StylusTip = Style == 1 ? StylusTip.Rectangle : StylusTip.Ellipse,
                IsHighlighter = Style == 2 };
        }
    }
}
