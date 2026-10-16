using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MinamotoBoard.PluginApi;

namespace MinamotoBoard.Samples
{
    public sealed class BoardThemePlugin : IBoardPlugin
    {
        private IBoardHost host;
        private Brush originalBackground;
        private Brush appliedBackground;
        private bool dark;
        public string Id { get { return "MinamotoDev.BoardTheme"; } }
        public string Name { get { return "黑板切换示例"; } }
        public int ApiVersion { get { return 1; } }

        public void Initialize(IBoardHost host)
        {
            this.host = host;
            originalBackground = host.Canvas.Background;
            host.AddToolbarButton("切换黑板", delegate
            {
                dark = !dark;
                appliedBackground = dark ? Brushes.Black : originalBackground;
                host.Canvas.Background = appliedBackground;
            });
            host.AddOverlay(new TextBlock { Text = "BoardTheme Plugin", Foreground = Brushes.White,
                Background = Brushes.DarkSlateGray, Padding = new Thickness(10), Margin = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top,
                IsHitTestVisible = false });
        }

        public void Shutdown()
        {
            if (host != null && host.Canvas.Background == appliedBackground)
                host.Canvas.Background = originalBackground;
            host = null;
        }
    }
}
