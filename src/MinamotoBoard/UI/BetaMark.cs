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
            return new Image { Source = image, Height = 22, Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        }

        private static DrawingImage CreateImage()
        {
            // Vector artwork from design/beta-badge-codex/beta-logo.svg, including its viewBox.
            Geometry capsule = new RectangleGeometry(new Rect(40, 40, 1120, 360), 180, 180);
            capsule.Freeze();
            GeometryGroup letters = new GeometryGroup();
            letters.Children.Add(Geometry.Parse("F0 M310 122 H391 C438 122 461 142 461 174 C461 194 451 208 435 216 C458 223 471 240 471 263 C471 298 444 316 397 316 H310 Q298 316 298 304 V136 Q298 122 310 122 Z M346 160 V198 H387 C405 198 414 192 414 179 C414 166 405 160 387 160 Z M346 234 V278 H393 C414 278 424 271 424 256 C424 241 413 234 393 234 Z"));
            letters.Children.Add(Geometry.Parse("F0 M640 252 H529 C532 274 546 285 567 285 C580 285 589 280 595 273 Q598 270 604 270 H632 Q643 270 639 280 C627 306 603 320 567 320 C516 320 484 289 484 243 C484 197 515 166 564 166 C611 166 643 197 643 242 V247 Q643 252 640 252 Z M529 224 H599 C595 207 584 198 565 198 C547 198 534 207 529 224 Z"));
            letters.Children.Add(Geometry.Parse("F1 M677 131 H699 Q711 131 711 143 V169 H737 Q746 169 746 178 V200 Q746 209 737 209 H711 V261 Q711 278 728 278 H737 Q746 278 746 287 V307 Q746 316 737 316 H717 C680 316 665 301 665 267 V209 H652 Q644 209 644 201 V177 Q644 169 652 169 H665 V143 Q665 131 677 131 Z"));
            letters.Children.Add(Geometry.Parse("F0 M868 181 V176 Q868 167 877 167 H904 Q914 167 914 177 V306 Q914 316 904 316 H877 Q868 316 868 307 V302 C858 314 843 320 824 320 C784 320 755 289 755 243 C755 197 784 166 824 166 C843 166 858 171 868 181 Z M801 243 C801 268 814 283 835 283 C856 283 869 268 869 243 C869 218 856 203 835 203 C814 203 801 218 801 243 Z"));
            letters.Transform = new TranslateTransform(6, 0);
            letters.Freeze();
            DrawingGroup drawing = new DrawingGroup();
            using (DrawingContext context = drawing.Open())
            {
                context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, 1200, 440));
                context.DrawGeometry(BaseBrush(), null, capsule);
                context.DrawGeometry(Bloom(0.12, 0.03, 0.65, 0.85, 0x00, 0xDE, 0xF2), null, capsule);
                context.DrawGeometry(Bloom(0.94, 0.24, 0.65, 0.8, 0xFF, 0x88, 0xCF), null, capsule);
                context.DrawGeometry(Bloom(0.22, 1, 0.55, 0.85, 0xC1, 0x39, 0xFF), null, capsule);
                context.DrawGeometry(Bloom(0.68, 1.08, 0.52, 0.9, 0xFF, 0x80, 0xDC), null, capsule);
                context.DrawGeometry(Brushes.White, null, letters);
            }
            drawing.Freeze();
            DrawingImage result = new DrawingImage(drawing);
            result.Freeze();
            return result;
        }

        private static Brush BaseBrush()
        {
            LinearGradientBrush brush = new LinearGradientBrush {
                StartPoint = new Point(0, 0.2), EndPoint = new Point(1, 0.8),
                ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation };
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x00, 0xC9, 0xEF), 0));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x06, 0x49, 0xFF), 0.23));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x68, 0x09, 0xFA), 0.48));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xEB, 0x00, 0xE8), 0.72));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0x00, 0x89), 1));
            brush.Freeze();
            return brush;
        }

        private static Brush Bloom(double x, double y, double radius, double opacity, byte red, byte green, byte blue)
        {
            RadialGradientBrush brush = new RadialGradientBrush {
                Center = new Point(x, y), GradientOrigin = new Point(x, y), RadiusX = radius, RadiusY = radius,
                Opacity = opacity, ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation };
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(red, green, blue), 0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, red, green, blue), 1));
            brush.Freeze();
            return brush;
        }
    }
}
