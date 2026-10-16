using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MinamotoBoard.Ink;

namespace MinamotoBoard.UI
{
    internal static class ToolbarIcons
    {
        public static FrameworkElement Tool(BoardTool tool)
        {
            switch (tool)
            {
                case BoardTool.Pen: return Glyph("\uE70F");
                case BoardTool.Eraser: return Glyph("\uE75C");
                case BoardTool.Ruler: return Vector("M3,8 L25,8 25,20 3,20 Z M7,8 V14 M11,8 V12 M15,8 V14 M19,8 V12 M23,8 V14");
                case BoardTool.SetSquare: return Vector("M3,3 L25,25 3,25 Z M8,14 L14,20 8,20 Z M3,9 H6 M3,15 H6 M3,21 H6");
                case BoardTool.Compass: return Vector("M14,2 V6 M11,9 A3,3 0 1 1 17,9 A3,3 0 1 1 11,9 M12,11 L5,26 M16,11 L23,26 M8,19 H20");
                default: return Vector("M7,3 L22,18 12,26 2,16 Z M5,13 L17,13 M5,1 L11,7 M24,15 C19,21 20,25 24,25 C28,25 29,21 24,15 Z");
            }
        }

        public static FrameworkElement Glyph(string glyph)
        {
            return new TextBlock { Text = glyph, FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        }

        private static FrameworkElement Vector(string path)
        {
            Geometry geometry = Geometry.Parse(path); geometry.Freeze();
            Pen pen = new Pen(new SolidColorBrush(Color.FromRgb(23, 63, 43)), 1.8);
            pen.StartLineCap = pen.EndLineCap = PenLineCap.Round; pen.LineJoin = PenLineJoin.Round; pen.Freeze();
            DrawingImage image = new DrawingImage(new GeometryDrawing(null, pen, geometry)); image.Freeze();
            return new Image { Source = image, Width = 28, Height = 28, Stretch = Stretch.Uniform };
        }

        public static string Name(BoardTool tool)
        {
            switch (tool)
            {
                case BoardTool.Pen: return "画笔";
                case BoardTool.Eraser: return "橡皮";
                case BoardTool.Ruler: return "直尺";
                case BoardTool.SetSquare: return "三角板";
                case BoardTool.Compass: return "圆规";
                default: return "油漆桶";
            }
        }
    }
}
