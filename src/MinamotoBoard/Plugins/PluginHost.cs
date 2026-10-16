using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MinamotoBoard.PluginApi;

namespace MinamotoBoard.Plugins
{
    public sealed class PluginHost : IBoardHost, IDisposable
    {
        private readonly List<IDisposable> contributions = new List<IDisposable>();
        private readonly Action<Exception> onError;
        private bool disposed;
        public Window MainWindow { get; private set; }
        public InkCanvas Canvas { get; private set; }
        public Panel Toolbar { get; private set; }
        public Panel Overlay { get; private set; }

        public PluginHost(Window window, InkCanvas canvas, Panel toolbar, Panel overlay, Action<Exception> onError)
        {
            MainWindow = window; Canvas = canvas; Toolbar = toolbar; Overlay = overlay; this.onError = onError;
        }

        public IDisposable AddToolbarButton(string label, Action action)
        {
            MainWindow.Dispatcher.VerifyAccess();
            if (disposed) throw new ObjectDisposedException("PluginHost");
            if (action == null) throw new ArgumentNullException("action");
            Button button = new Button { Content = MinamotoBoard.UI.ToolbarIcons.Glyph("\uE8A5"),
                ToolTip = label, MinWidth = 50, MinHeight = 50, Margin = new Thickness(3) };
            System.Windows.Automation.AutomationProperties.SetName(button, label);
            RoutedEventHandler handler = delegate
            {
                try { action(); }
                catch (Exception ex) { button.IsEnabled = false; onError(ex); }
            };
            button.Click += handler;
            Toolbar.Children.Add(button);
            IDisposable handle = new Contribution(delegate { button.Click -= handler; Toolbar.Children.Remove(button); });
            contributions.Add(handle);
            return handle;
        }

        public IDisposable AddOverlay(UIElement element)
        {
            MainWindow.Dispatcher.VerifyAccess();
            if (disposed) throw new ObjectDisposedException("PluginHost");
            if (element == null) throw new ArgumentNullException("element");
            Overlay.Children.Add(element);
            IDisposable handle = new Contribution(delegate { Overlay.Children.Remove(element); });
            contributions.Add(handle);
            return handle;
        }

        public void Dispose()
        {
            if (disposed) return;
            MainWindow.Dispatcher.VerifyAccess();
            disposed = true;
            for (int i = contributions.Count - 1; i >= 0; i--) contributions[i].Dispose();
            contributions.Clear();
        }

        private sealed class Contribution : IDisposable
        {
            private Action remove;
            public Contribution(Action remove) { this.remove = remove; }
            public void Dispose() { if (remove != null) { Action action = remove; remove = null; action(); } }
        }
    }
}
