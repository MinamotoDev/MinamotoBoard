using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MinamotoBoard.UI
{
    internal static class BetaMark
    {
        private static readonly DrawingImage image = CreateImage();

        public static Image Create()
        {
            return new Image { Source = image, Height = 30, Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        }

        private static DrawingImage CreateImage()
        {
            Geometry clip = Geometry.Parse("M145.94,37.5h178.32c57.16,0,103.49,41.41,103.49,92.5s-46.34,92.5-103.49,92.5h-178.32c-57.16,0-103.49-41.41-103.49-92.5s46.34-92.5,103.49-92.5Z");
            Geometry stroke = Geometry.Parse("M145.94,37.5h178.32c57.16,0,103.49,41.41,103.49,92.5h0c0,51.09-46.34,92.5-103.49,92.5h-178.32c-57.16,0-103.49-41.41-103.49-92.5h0c0-51.09,46.34-92.5,103.49-92.5Z");
            GeometryGroup letters = new GeometryGroup();
            letters.Children.Add(Geometry.Parse("M124.09,162.39v-64.79h23.14c7.05,0,12.64,1.37,16.76,4.11,4.12,2.74,6.18,6.31,6.18,10.71,0,3.68-1.17,6.87-3.51,9.58s-5.58,4.64-9.71,5.78v.18c5.02.51,9.03,2.16,12.03,4.95,3,2.79,4.5,6.41,4.5,10.87,0,5.54-2.46,10.03-7.37,13.46-4.92,3.43-11.12,5.15-18.61,5.15h-23.39ZM136.14,106.23v18.43h7.83c4.2,0,7.5-.88,9.89-2.64s3.59-4.25,3.59-7.48c0-5.54-4.17-8.31-12.51-8.31h-8.8ZM136.14,133.3v20.51h10.32c4.51,0,7.99-.93,10.45-2.78,2.46-1.85,3.69-4.41,3.69-7.66,0-6.72-5.24-10.08-15.72-10.08h-8.75Z"));
            letters.Children.Add(Geometry.Parse("M227.94,162.39h-40.94v-64.79h39.31v9.13h-27.21v18.34h25.07v9.08h-25.07v19.16h28.84v9.08Z"));
            letters.Children.Add(Geometry.Parse("M288.21,106.73h-20.95v55.66h-12.1v-55.66h-20.9v-9.13h53.96v9.13Z"));
            letters.Children.Add(Geometry.Parse("M351.22,162.39h-13.27l-6.56-16.49h-28.68l-6.31,16.49h-13.22l27.31-64.79h13.63l27.11,64.79ZM328.18,137.14l-10.12-25.84c-.31-.84-.63-2.2-.97-4.07h-.2c-.31,1.72-.64,3.07-1.02,4.07l-10.02,25.84h22.33Z"));
            clip.Freeze(); stroke.Freeze(); letters.Freeze();
            Rect bounds = new Rect(42.45, 37.5, 385.31, 185);
            DrawingGroup fills = new DrawingGroup { ClipGeometry = clip };
            using (DrawingContext context = fills.Open())
            {
                context.DrawRectangle(BaseBrush(), null, bounds);
                context.DrawRectangle(Bloom(125.72, 171.41, 192.65, 92.50, 145, 236, 72, 153), null, bounds);
                context.DrawRectangle(Bloom(372.31, 93.71, 177.24, 85.10, 140, 34, 211, 238), null, bounds);
                context.DrawRectangle(Bloom(79.48, 67.81, 146.42, 70.30, 99, 167, 139, 250), null, bounds);
                context.DrawRectangle(Sheen(), null, bounds);
            }
            fills.Freeze();
            Pen rim = new Pen(Frozen(Color.FromArgb(89, 255, 255, 255)), 2);
            rim.Freeze();
            DrawingGroup drawing = new DrawingGroup();
            using (DrawingContext context = drawing.Open())
            {
                context.DrawDrawing(fills);
                context.DrawGeometry(null, rim, stroke);
                context.DrawGeometry(Brushes.White, null, letters);
            }
            drawing.Freeze();
            DrawingImage result = new DrawingImage(drawing);
            result.Freeze();
            return result;
        }

        private static Brush BaseBrush()
        {
            LinearGradientBrush brush = new LinearGradientBrush { MappingMode = BrushMappingMode.Absolute,
                StartPoint = new Point(42.52, 130.92), EndPoint = new Point(428.60, 130.92) };
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x6D, 0x28, 0xD9), 0));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x4F, 0x46, 0xE5), 0.34));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x25, 0x63, 0xEB), 0.64));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x08, 0x91, 0xB2), 1));
            return Freeze(brush);
        }

        private static Brush Sheen()
        {
            LinearGradientBrush brush = new LinearGradientBrush { MappingMode = BrushMappingMode.Absolute,
                StartPoint = new Point(233.29, 38.2), EndPoint = new Point(233.29, 223.64) };
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(64, 255, 255, 255), 0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.42));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 1));
            return Freeze(brush);
        }

        private static Brush Bloom(double x, double y, double radiusX, double radiusY, byte alpha, byte red, byte green, byte blue)
        {
            RadialGradientBrush brush = new RadialGradientBrush { MappingMode = BrushMappingMode.Absolute,
                Center = new Point(x, y), GradientOrigin = new Point(x, y), RadiusX = radiusX, RadiusY = radiusY };
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(alpha, red, green, blue), 0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, red, green, blue), 1));
            return Freeze(brush);
        }

        private static Brush Freeze(Brush brush) { brush.Freeze(); return brush; }
        private static SolidColorBrush Frozen(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
