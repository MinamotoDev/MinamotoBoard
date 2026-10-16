using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MinamotoBoard.PluginApi;
using MinamotoBoard.Plugins;

namespace MinamotoBoard.Tests
{
    internal static class PluginTests
    {
        private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }

        private sealed class Fixture : IDisposable
        {
            public readonly string Root = Path.Combine(Path.GetTempPath(), "MinamotoBoardTests-" + Guid.NewGuid().ToString("N"));
            public readonly Window Window = new Window();
            public readonly InkCanvas Canvas = new InkCanvas { Background = Brushes.Green };
            public readonly StackPanel Toolbar = new StackPanel();
            public readonly Grid Overlay = new Grid();
            public readonly PluginManager Manager;
            public Fixture()
            {
                Directory.CreateDirectory(Root);
                Manager = new PluginManager(Root, Path.Combine(Root, "settings", "disabled.txt"));
                File.Copy(typeof(Samples.BoardThemePlugin).Assembly.Location, Path.Combine(Root, "Theme.Plugin.dll"));
            }
            public void Load(bool safe) { Manager.Discover(); Manager.LoadOnUiThread(Window, Canvas, Toolbar, Overlay, safe); }
            public void Dispose()
            {
                Manager.Dispose(); Window.Close();
                // Assemblies remain locked until this test process exits. Never recursively delete test paths here.
            }
        }

        public static void LoadAndCleanup()
        {
            using (Fixture f = new Fixture())
            {
                f.Load(false);
                Check(f.Manager.Records.Single().Status == "已加载", "Sample did not load");
                Check(f.Toolbar.Children.Count == 1 && f.Overlay.Children.Count == 1, "UI was not extended");
                ((Button)f.Toolbar.Children[0]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(f.Canvas.Background == Brushes.Black, "Plugin feature did not run");
                f.Manager.Dispose();
                Check(f.Toolbar.Children.Count == 0 && f.Overlay.Children.Count == 0, "UI not cleaned up");
                Check(f.Canvas.Background == Brushes.Green, "Plugin state not restored");
            }
        }

        public static void DisabledAndSafeMode()
        {
            using (Fixture f = new Fixture())
            {
                f.Manager.SaveDisabled(new[] { "Theme.Plugin.dll" });
                f.Load(false);
                Check(f.Toolbar.Children.Count == 0 && !f.Manager.Records.Single().Enabled, "Disabled plugin ran");
                f.Manager.SaveDisabled(new string[0]);
                f.Load(true);
                Check(f.Manager.Records.Single().Enabled && f.Toolbar.Children.Count == 0, "Safe mode ran plugin");
            }
        }

        public static void FailureIsolation()
        {
            using (Fixture f = new Fixture())
            {
                File.Copy(Assembly.GetExecutingAssembly().Location, Path.Combine(f.Root, "Broken.Plugin.dll"));
                File.WriteAllText(Path.Combine(f.Root, "Invalid.Plugin.dll"), "not an assembly");
                f.Load(false);
                Check(f.Manager.Records.Count(r => r.Status.StartsWith("加载失败")) == 2, "Failures not reported");
                Check(f.Toolbar.Children.Count == 1 && f.Overlay.Children.Count == 1, "Rollback/healthy plugin failed");
            }
        }

        public static void DuplicateIds()
        {
            using (Fixture f = new Fixture())
            {
                string child = Path.Combine(f.Root, "Second"); Directory.CreateDirectory(child);
                File.Copy(typeof(Samples.BoardThemePlugin).Assembly.Location, Path.Combine(child, "Theme.Plugin.dll"));
                f.Load(false);
                Check(f.Manager.Records.Count(r => r.Status == "已加载") == 1 && f.Toolbar.Children.Count == 1,
                    "Duplicate plugin initialized");
            }
        }

        public static void CallbackFailure()
        {
            Window window = new Window(); StackPanel toolbar = new StackPanel(); bool reported = false;
            using (PluginHost host = new PluginHost(window, new InkCanvas(), toolbar, new Grid(), ex => reported = true))
            {
                host.AddToolbarButton("Fail", () => { throw new Exception("expected"); });
                Button button = (Button)toolbar.Children[0]; button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(reported && !button.IsEnabled, "Callback failure escaped or remained enabled");
            }
            window.Close();
        }
    }

    // A real reflection-loaded fixture: contributes UI, then fails initialization and shutdown.
    public sealed class BrokenPlugin : IBoardPlugin
    {
        public string Id { get { return "test.broken"; } }
        public string Name { get { return "Broken"; } }
        public int ApiVersion { get { return 1; } }
        public void Initialize(IBoardHost host) { host.AddToolbarButton("Leaked", () => { }); throw new Exception("expected"); }
        public void Shutdown() { throw new Exception("expected shutdown failure"); }
    }
}
