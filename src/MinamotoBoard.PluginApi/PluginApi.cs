using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

[assembly: AssemblyVersion("1.0.0.0")]

namespace MinamotoBoard.PluginApi
{
    public interface IBoardPlugin
    {
        string Id { get; }
        string Name { get; }
        int ApiVersion { get; }
        void Initialize(IBoardHost host);
        void Shutdown();
    }

    // All UI access and lifecycle calls occur on the WPF dispatcher thread.
    public interface IBoardHost
    {
        Window MainWindow { get; }
        InkCanvas Canvas { get; }
        Panel Toolbar { get; }
        Panel Overlay { get; }
        IDisposable AddToolbarButton(string label, Action action);
        IDisposable AddOverlay(UIElement element);
    }
}
