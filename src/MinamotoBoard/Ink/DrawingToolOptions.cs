using System.Windows.Ink;
using System.Windows.Media;

namespace MinamotoBoard.Ink
{
    public enum BoardTool { Pen, Eraser, Ruler, SetSquare, Compass, Bucket, Highlighter }

    public sealed class DrawingToolOptions
    {
        public Color Color = Colors.White;
        public double Width = 4;
        public int Style;
        public bool Highlighter;

        public DrawingAttributes Attributes()
        {
            return new DrawingAttributes { Color = Highlighter ? System.Windows.Media.Color.FromArgb(96, Color.R, Color.G, Color.B) : Color, Width = Width, Height = Highlighter ? Width * 2 : Width,
                IgnorePressure = true, FitToCurve = false,
                StylusTip = Highlighter ? StylusTip.Rectangle : StylusTip.Ellipse,
                IsHighlighter = false };
        }
    }
}
