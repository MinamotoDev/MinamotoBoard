using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using MinamotoBoard.UI;

namespace MinamotoBoard
{
    public partial class App : Application
    {
        static App()
        {
            // Keep DPI behavior when distributing the no-plugins EXE on its own.
            AppContext.SetSwitch("Switch.System.Windows.DoNotScaleForDpiChanges", false);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Explicit, per-process fallback for older graphics drivers. No system changes.
            if (Array.Exists(e.Args, a => string.Equals(a, "--software-rendering", StringComparison.OrdinalIgnoreCase)))
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
#if NO_PLUGINS
            MainWindow = new MainWindow();
#else
            bool safeMode = Array.Exists(e.Args, a => string.Equals(a, "--safe-mode", StringComparison.OrdinalIgnoreCase));
            MainWindow = new MainWindow(safeMode);
#endif
            MainWindow.Show();
        }
    }
}
