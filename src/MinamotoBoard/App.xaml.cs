using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using MinamotoBoard.UI;

namespace MinamotoBoard
{
    public partial class App : Application
    {
        private SingleInstance singleInstance;
        static App()
        {
            // Keep DPI behavior when distributing the no-plugins EXE on its own.
            AppContext.SetSwitch("Switch.System.Windows.DoNotScaleForDpiChanges", false);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string documentPath = Array.Find(e.Args, argument => !argument.StartsWith("--", StringComparison.Ordinal) &&
                argument.EndsWith(".mboard", StringComparison.OrdinalIgnoreCase));
            if (documentPath != null)
            {
                try { documentPath = System.IO.Path.GetFullPath(documentPath); }
                catch (ArgumentException) { documentPath = null; }
                catch (System.IO.IOException) { documentPath = null; }
                catch (NotSupportedException) { documentPath = null; }
            }
            singleInstance = new SingleInstance();
            if (!singleInstance.TryAcquire())
            {
                bool delivered = singleInstance.NotifyExisting(documentPath).GetAwaiter().GetResult();
                // Take over only if the previous instance exited during startup/notification.
                if (delivered || !singleInstance.TryAcquire()) { Shutdown(); return; }
            }
            // Explicit, per-process fallback for older graphics drivers. No system changes.
            if (Array.Exists(e.Args, a => string.Equals(a, "--software-rendering", StringComparison.OrdinalIgnoreCase)))
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
#if NO_PLUGINS
            MainWindow = new MainWindow();
#else
            bool safeMode = Array.Exists(e.Args, a => string.Equals(a, "--safe-mode", StringComparison.OrdinalIgnoreCase));
            MainWindow = new MainWindow(safeMode);
#endif
            MainWindow board = (MainWindow)MainWindow;
            singleInstance.Listen(delegate(string path)
            {
                if (!Dispatcher.HasShutdownStarted)
                    Dispatcher.BeginInvoke(new Action(delegate { board.HandleExternalLaunch(path); }));
            });
            if (documentPath != null)
            {
                MainWindow window = (MainWindow)MainWindow;
                window.Loaded += delegate { window.OpenDocument(documentPath, false); };
            }
            MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (singleInstance != null) singleInstance.Dispose();
            base.OnExit(e);
        }
    }
}
