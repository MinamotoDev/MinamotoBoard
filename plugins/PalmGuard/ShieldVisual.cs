using System;
using System.Windows;
using System.Windows.Media;

namespace MinamotoBoard.Plugins.PalmGuard
{
    internal sealed class ShieldVisual : FrameworkElement
    {
        internal readonly GuardSettings Settings;
        internal bool Editing;
        internal ShieldVisual(GuardSettings settings)
        { Settings = settings; IsHitTestVisible = false; ClipToBounds = true; }
        internal Rect Area
        {
            get { return new Rect(Settings.X * ActualWidth, Settings.Y * ActualHeight,
                Settings.Width * ActualWidth, Settings.Height * ActualHeight); }
        }
        internal void Apply(Rect rect)
        {
            if (ActualWidth <= 0 || ActualHeight <= 0) return;
            Settings.Width = rect.Width / ActualWidth; Settings.Height = rect.Height / ActualHeight;
            Settings.X = rect.X / ActualWidth; Settings.Y = rect.Y / ActualHeight;
            Settings.Normalize(); InvalidateVisual();
        }
        protected override void OnRender(DrawingContext drawing)
        {
            base.OnRender(drawing);
            if (!Settings.Enabled || !Settings.ShieldEnabled || ActualWidth <= 0 || ActualHeight <= 0) return;
            Rect rect = Area;
            Pen border = new Pen(Editing ? Brushes.DodgerBlue : new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), 1.5);
            border.DashStyle = DashStyles.Dash;
            drawing.DrawRectangle(new SolidColorBrush(Color.FromArgb(42, 0, 0, 0)), border, rect);
            if (Editing) drawing.DrawEllipse(Brushes.DodgerBlue, new Pen(Brushes.White, 1.5), rect.BottomRight, 10, 10);
        }
    }
}
