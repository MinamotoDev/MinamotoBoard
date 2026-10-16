using System;
using System.ComponentModel;
#if !NO_PLUGINS
using System.Threading.Tasks;
using MinamotoBoard.Plugins;
#endif
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MinamotoBoard.Ink;
using MinamotoBoard.Models;

namespace MinamotoBoard.UI
{
    public partial class MainWindow : Window
    {
        private InkTools tools;
        private bool fullscreen;
        private WindowState previousState;
        private string colorName = "白色";
        private readonly BoardDocument document;
        private double toolbarScale = 1;
#if !NO_PLUGINS
        private PluginManager plugins;
        private readonly bool safeMode;
        private bool pluginsStarted;
#endif

        public MainWindow() : this(false) { }

        public MainWindow(bool safeMode)
        {
#if !NO_PLUGINS
            this.safeMode = safeMode;
#endif
            InitializeComponent();
            document = new BoardDocument(Board.Strokes, Board.Background);
            UpdatePageControls();
            tools = new InkTools(Board);
            PenWidth.ItemsSource = InkTools.PenWidths;
            EraserWidth.ItemsSource = InkTools.EraserWidths;
            PenWidth.SelectedItem = 4.0;
            EraserWidth.SelectedItem = 32.0;
            WhiteButton.IsChecked = true;
            PenButton.IsChecked = true;
            SetFullscreen(true);
#if !NO_PLUGINS
            PluginsButton.Visibility = Visibility.Visible;
            ContentRendered += OnFirstRendered;
            Closed += delegate { if (plugins != null) plugins.Dispose(); };
#endif
        }

#if !NO_PLUGINS
        private async void OnFirstRendered(object sender, EventArgs e)
        {
            if (pluginsStarted) return;
            pluginsStarted = true;
            PluginsButton.IsEnabled = false;
            plugins = PluginManager.CreateDefault();
            await Task.Run(() => plugins.Discover());
            if (!IsLoaded) { plugins.Dispose(); return; }
            plugins.LoadOnUiThread(this, Board, ToolsPanel, PluginOverlay, safeMode);
            PluginsButton.IsEnabled = true;
            PluginsButton.ToolTip = safeMode ? "安全模式" : "管理插件";
        }
#endif

        private void OnPlugins(object sender, RoutedEventArgs e)
        {
#if !NO_PLUGINS
            if (plugins != null) new PluginsWindow(plugins, safeMode) { Owner = this }.ShowDialog();
#endif
        }

        private void OnPenSelected(object sender, RoutedEventArgs e)
        {
            if (tools == null) return;
            tools.UsePen();
            UpdateStatus();
        }

        private void OnEraserSelected(object sender, RoutedEventArgs e)
        {
            if (tools == null) return;
            tools.UseEraser();
            UpdateStatus();
        }

        private void OnColorSelected(object sender, RoutedEventArgs e)
        {
            if (tools == null) return;
            RadioButton button = (RadioButton)sender;
            tools.SetColor((Color)ColorConverter.ConvertFromString((string)button.Tag));
            colorName = (string)button.ToolTip;
            PenButton.IsChecked = true;
            UpdateStatus();
        }

        private void OnPenWidthChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tools == null || PenWidth.SelectedItem == null) return;
            tools.SetPenWidth((double)PenWidth.SelectedItem);
            UpdateStatus();
        }

        private void OnEraserWidthChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tools == null || EraserWidth.SelectedItem == null) return;
            tools.SetEraserWidth((double)EraserWidth.SelectedItem);
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            ToolStatus.Text = Board.EditingMode == InkCanvasEditingMode.Ink
                ? string.Format("{0}画笔 · {1} DIP", colorName, Board.DefaultDrawingAttributes.Width)
                : string.Format("橡皮擦 · {0} DIP", Board.EraserShape.Width);
        }

        private bool IsWriting()
        {
            return Board.IsStylusCaptured || Board.IsMouseCaptured || Board.AreAnyTouchesCaptured;
        }

        private void CaptureCurrentPage()
        {
            // Plugins may replace Canvas.Strokes rather than modifying it in place.
            document.CurrentPage.Strokes = Board.Strokes;
            document.CurrentPage.Background = Board.Background;
        }

        private void ShowCurrentPage()
        {
            Board.Strokes = document.CurrentPage.Strokes;
            Board.Background = document.CurrentPage.Background;
            UpdatePageControls();
            Board.Focus();
        }

        private void UpdatePageControls()
        {
            PageStatus.Text = string.Format("{0} / {1}", document.CurrentIndex + 1, document.Count);
            PreviousPageButton.IsEnabled = document.CanMovePrevious;
            NextPageButton.IsEnabled = document.CanMoveNext;
            AddPageButton.IsEnabled = document.CanAddPage;
            AddPageButton.ToolTip = document.CanAddPage
                ? "新增空白页"
                : "已达到 100 页上限";
        }

        private void OnAddPage(object sender, RoutedEventArgs e)
        {
            if (IsWriting()) return;
            CaptureCurrentPage();
            if (document.AddPage()) ShowCurrentPage();
        }

        private void OnPreviousPage(object sender, RoutedEventArgs e)
        {
            if (IsWriting()) return;
            CaptureCurrentPage();
            if (document.MovePrevious()) ShowCurrentPage();
        }

        private void OnNextPage(object sender, RoutedEventArgs e)
        {
            if (IsWriting()) return;
            CaptureCurrentPage();
            if (document.MoveNext()) ShowCurrentPage();
        }

        private void OnClearPage(object sender, RoutedEventArgs e)
        {
            if (IsWriting() || Board.Strokes.Count == 0) return;
            if (MessageBox.Show(this,
                "清空当前页面的全部墨迹吗？您无法撤销此操作。",
                "清屏", MessageBoxButton.OKCancel, MessageBoxImage.Warning,
                MessageBoxResult.Cancel) != MessageBoxResult.OK) return;
            Board.Strokes.Clear();
            Board.Focus();
        }

        private void OnHideToolbar(object sender, RoutedEventArgs e) { SetToolbarVisible(false); }
        private void OnShowToolbar(object sender, RoutedEventArgs e) { SetToolbarVisible(true); }

        private void OnBoardSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ToolsPanel == null) return;
            if (Toolbar != null) Toolbar.MaxWidth = Math.Max(0, e.NewSize.Width - 32);
            // WPF already accounts for monitor DPI. On large logical viewports,
            // make the floating toolbar modestly smaller, never below 44 DIP
            // for built-in touch targets (50 DIP * minimum scale 0.9 = 45 DIP).
            double relativeSize = Math.Min(e.NewSize.Width / 1600.0, e.NewSize.Height / 900.0);
            double scale = Math.Round(relativeSize > 1 ? Math.Max(0.9, 1 / relativeSize) : 1, 2);
            if (Math.Abs(scale - toolbarScale) < 0.01) return;
            toolbarScale = scale;
            ToolsPanel.LayoutTransform = new ScaleTransform(scale, scale);
        }

        private void OnToolbarScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (ToolbarLeft == null || ToolbarRight == null || ToolbarScroll == null) return;
            bool overflow = ToolbarScroll.ScrollableWidth > 1;
            ToolbarLeft.Visibility = ToolbarRight.Visibility = overflow ? Visibility.Visible : Visibility.Collapsed;
            ToolbarLeft.IsEnabled = ToolbarScroll.HorizontalOffset > 1;
            ToolbarRight.IsEnabled = ToolbarScroll.HorizontalOffset + 1 < ToolbarScroll.ScrollableWidth;
        }

        private void OnToolbarLeft(object sender, RoutedEventArgs e)
        {
            ToolbarScroll.ScrollToHorizontalOffset(ToolbarScroll.HorizontalOffset - 320 * toolbarScale);
        }

        private void OnToolbarRight(object sender, RoutedEventArgs e)
        {
            ToolbarScroll.ScrollToHorizontalOffset(ToolbarScroll.HorizontalOffset + 320 * toolbarScale);
        }

        private void SetToolbarVisible(bool visible)
        {
            Toolbar.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            ShowToolbarButton.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
            Board.Focus();
        }

        private void OnToggleFullscreen(object sender, RoutedEventArgs e) { SetFullscreen(!fullscreen); }

        private void SetFullscreen(bool enabled)
        {
            if (enabled)
            {
                previousState = WindowState;
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;
                WindowState = previousState;
            }
            fullscreen = enabled;
            FullscreenButton.Content = enabled ? "退出全屏" : "进入全屏";
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11) { SetFullscreen(!fullscreen); e.Handled = true; }
            else if (e.Key == Key.Escape && fullscreen) { SetFullscreen(false); e.Handled = true; }
            else if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SetToolbarVisible(Toolbar.Visibility != Visibility.Visible);
                e.Handled = true;
            }
        }

        private void OnClose(object sender, RoutedEventArgs e) { Close(); }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            CaptureCurrentPage();
            if (!document.HasInk) return;
            e.Cancel = MessageBox.Show(this,
                "确定退出应用程序吗？未保存的墨迹将全部丢失。",
                "关闭", MessageBoxButton.OKCancel, MessageBoxImage.Warning,
                MessageBoxResult.Cancel) != MessageBoxResult.OK;
        }
    }
}
