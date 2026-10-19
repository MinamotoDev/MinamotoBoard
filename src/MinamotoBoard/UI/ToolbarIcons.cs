using MinamotoBoard.Localization;
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
                case BoardTool.Pen: return Vector("M5,27 L8,18 23,3 29,9 14,24 Z M8,18 L14,24 M5,27 L11,25 M20,6 L26,12");
                case BoardTool.Highlighter: return Vector("M7,19 L20,4 28,11 15,26 Z M7,19 L15,26 10,29 3,23 Z M3,29 H13 M17,8 L25,15");
                case BoardTool.Eraser: return Vector("M4,20 L18,5 29,15 16,28 12,28 Z M10,14 L22,24 M16,28 H29");
                case BoardTool.Ruler: return Vector("M3,10 H29 V24 H3 Z M7,10 V17 M11,10 V14 M15,10 V17 M19,10 V14 M23,10 V17 M27,10 V14");
                case BoardTool.SetSquare: return Vector("M5,4 L28,27 H5 Z M10,16 L16,22 H10 Z M5,11 H8 M5,18 H8 M5,24 H8");
                case BoardTool.SetSquare30: return Vector("M9,3 V29 H25 Z M13,18 V25 H17 Z M9,10 H12 M9,17 H12 M9,24 H12");
                case BoardTool.Compass: return Vector("M16,3 V7 M13,10 A3,3 0 1 1 19,10 A3,3 0 1 1 13,10 M14,13 L6,29 M18,13 L26,29 M10,21 H22");
                default: return Vector("M8,5 L23,20 13,29 3,19 Z M6,15 H18 M5,2 L12,9 M26,17 C21,24 22,28 26,28 C30,28 31,24 26,17 Z");
            }
        }

        public static FrameworkElement Glyph(string glyph)
        {
            switch (glyph)
            {
                case "\uE760": return Vector("M19,7 L10,16 19,25");
                case "\uE761": return Vector("M13,7 L22,16 13,25");
                case "\uE76B": return Vector("M19,7 L10,16 19,25");
                case "\uE76C": return Vector("M13,7 L22,16 13,25");
                case "\uE710": return Vector("M16,5 V27 M5,16 H27");
                case "\uE74D": return Vector("M6,9 H26 M12,9 V5 H20 V9 M9,9 V28 H23 V9 M14,13 V24 M18,13 V24");
                case "\uE70D": return Vector("M6,11 L16,21 26,11");
                case "\uE711": return Vector("M7,7 L25,25 M7,25 L25,7");
                case "\uE713": return Vector("M13,3 H19 L20,7 23,9 27,8 30,13 27,16 27,19 29,22 26,27 22,26 19,28 18,31 H12 L11,27 8,25 4,26 1,21 4,18 4,15 2,12 5,7 9,8 12,6 Z M11,17 A5,5 0 1 1 21,17 A5,5 0 1 1 11,17");
                default: return Vector("M8,8 H24 V24 H8 Z");
            }
        }

        private static FrameworkElement Vector(string path)
        {
            Geometry geometry = Geometry.Parse(path); geometry.Freeze();
            Pen pen = new Pen(new SolidColorBrush(Color.FromRgb(23, 63, 43)), 1.65);
            pen.StartLineCap = pen.EndLineCap = PenLineCap.Round; pen.LineJoin = PenLineJoin.Round; pen.Freeze();
            DrawingGroup drawing = new DrawingGroup();
            // Every icon shares the same viewport; path bounds never change its scale or alignment.
            drawing.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 32, 32))));
            drawing.Children.Add(new GeometryDrawing(null, pen, geometry)); drawing.Freeze();
            DrawingImage image = new DrawingImage(drawing); image.Freeze();
            return new Image { Source = image, Width = 28, Height = 28, Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        }

        public static string Name(BoardTool tool)
        {
            switch (tool)
            {
                case BoardTool.Pen: return Localizer.T("画笔");
                case BoardTool.Highlighter: return Localizer.T("荧光笔");
                case BoardTool.Eraser: return Localizer.T("橡皮");
                case BoardTool.Ruler: return Localizer.T("直尺");
                case BoardTool.SetSquare: return Localizer.T("45° 三角板");
                case BoardTool.SetSquare30: return Localizer.T("30° / 60° 三角板");
                case BoardTool.Compass: return Localizer.T("圆规");
                default: return Localizer.T("油漆桶");
            }
        }
    }
}
