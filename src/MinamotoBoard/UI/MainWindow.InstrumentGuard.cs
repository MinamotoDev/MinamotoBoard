using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace MinamotoBoard.UI
{
    public partial class MainWindow
    {
        private readonly HashSet<int> guardedTouches = new HashSet<int>();
        private readonly HashSet<int> guardedStyli = new HashSet<int>();
        private bool guardedMouse;

        private bool BlockInstrumentWriting(object source)
        {
            DependencyObject element = source as DependencyObject;
            bool canvas = element == Board || (element != null && Board.IsAncestorOf(element));
            bool instrument = element is MinamotoBoard.Ink.InstrumentOverlay;
            return (canvas || instrument) && Instruments.IsManipulating;
        }

        private void InitializeInstrumentTouchGuard()
        {
            // Window-level preview runs before InkCanvas and tool-specific preview handlers.
            PreviewTouchDown += delegate(object sender, TouchEventArgs e)
            {
                guardedTouches.Remove(e.TouchDevice.Id);
                if (!BlockInstrumentWriting(e.OriginalSource)) return;
                guardedTouches.Add(e.TouchDevice.Id); e.Handled = true;
            };
            PreviewTouchMove += delegate(object sender, TouchEventArgs e)
            { if (guardedTouches.Contains(e.TouchDevice.Id)) e.Handled = true; };
            PreviewTouchUp += delegate(object sender, TouchEventArgs e)
            { if (guardedTouches.Remove(e.TouchDevice.Id)) e.Handled = true; };
            PreviewStylusDown += delegate(object sender, StylusDownEventArgs e)
            {
                guardedStyli.Remove(e.StylusDevice.Id);
                if (!BlockInstrumentWriting(e.OriginalSource)) return;
                guardedStyli.Add(e.StylusDevice.Id); e.Handled = true;
            };
            PreviewStylusMove += delegate(object sender, StylusEventArgs e)
            { if (guardedStyli.Contains(e.StylusDevice.Id)) e.Handled = true; };
            PreviewStylusUp += delegate(object sender, StylusEventArgs e)
            { if (guardedStyli.Remove(e.StylusDevice.Id)) e.Handled = true; };
            PreviewMouseDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (e.ChangedButton != MouseButton.Left) return;
                guardedMouse = BlockInstrumentWriting(e.OriginalSource);
                if (guardedMouse) e.Handled = true;
            };
            PreviewMouseMove += delegate(object sender, MouseEventArgs e)
            { if (guardedMouse && e.LeftButton == MouseButtonState.Pressed) e.Handled = true; };
            PreviewMouseUp += delegate(object sender, MouseButtonEventArgs e)
            {
                if (e.ChangedButton != MouseButton.Left || !guardedMouse) return;
                guardedMouse = false; e.Handled = true;
            };
            Deactivated += delegate
            {
                Instruments.CancelInteraction();
                guardedTouches.Clear(); guardedStyli.Clear(); guardedMouse = false;
            };
        }
    }
}
