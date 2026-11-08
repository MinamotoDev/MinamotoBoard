using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace MinamotoBoard.Ink
{
    // One filter per contact; all sizes are in the canvas coordinate space (DIP).
    internal sealed class AdaptiveEraser
    {
        private long lastUpdate;
        private bool initialized;
        public Size Size { get; private set; }

        public AdaptiveEraser(double manualWidth)
        {
            Size = new Size(manualWidth, manualWidth);
        }

        public void Update(Rect bounds, double manualWidth)
        {
            long now = Stopwatch.GetTimestamp();
            double elapsed = lastUpdate == 0 ? 0 : (double)(now - lastUpdate) / Stopwatch.Frequency;
            lastUpdate = now;
            // Empty and placeholder contacts are common on devices without contact-area data.
            if (bounds.IsEmpty || !ValidDimension(bounds.Width) || !ValidDimension(bounds.Height))
            {
                Size = new Size(manualWidth, manualWidth);
                initialized = false;
                return;
            }
            double width = Math.Max(8, Math.Min(256, bounds.Width));
            double height = Math.Max(8, Math.Min(256, bounds.Height));
            if (initialized)
            {
                width = Smooth(Size.Width, width, elapsed);
                height = Smooth(Size.Height, height, elapsed);
            }
            Size = new Size(width, height);
            initialized = true;
        }

        private static bool ValidDimension(double value)
        { return ToolPreferences.Finite(value) && value > 1 && value <= 4096; }

        private static double Smooth(double previous, double target, double elapsed)
        {
            if (Math.Abs(target - previous) < Math.Max(1, previous * 0.03)) return previous;
            double timeConstant = target > previous ? 0.035 : 0.09;
            double weight = 1 - Math.Exp(-Math.Max(0, elapsed) / timeConstant);
            return previous + (target - previous) * weight;
        }

        public static Geometry Sweep(IList<Point> path, Size size, bool rectangular)
        {
            StylusPointCollection points = new StylusPointCollection();
            foreach (Point point in path) points.Add(new StylusPoint(point.X, point.Y));
            // The same elliptical nib and polyline are used by native ink hit testing.
            Geometry geometry = new Stroke(points, new DrawingAttributes {
                Width = size.Width, Height = size.Height, StylusTip = rectangular ? StylusTip.Rectangle : StylusTip.Ellipse,
                IgnorePressure = true, FitToCurve = false }).GetGeometry();
            if (geometry.CanFreeze) geometry.Freeze();
            return geometry;
        }
    }
}
