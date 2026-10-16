using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using MinamotoBoard.UI;

namespace MinamotoBoard
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Explicit, per-process fallback for older graphics drivers. No system changes.
            if (Array.Exists(e.Args, a => string.Equals(a, "--software-rendering", StringComparison.OrdinalIgnoreCase)))
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            bool safeMode = Array.Exists(e.Args, a => string.Equals(a, "--safe-mode", StringComparison.OrdinalIgnoreCase));
            MainWindow = new MainWindow(safeMode);
            MainWindow.Show();
        }
    }
}
