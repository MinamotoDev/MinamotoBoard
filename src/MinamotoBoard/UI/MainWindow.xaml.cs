using MinamotoBoard.Localization;
using System;
using System.ComponentModel;
#if !NO_PLUGINS
using System.Threading.Tasks;
using MinamotoBoard.Plugins;
#endif
using System.Windows;
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
            preferences = ToolPreferences.Load();
            Localizer.Initialize(preferences.Language);
            InitializeComponent();
            document = new BoardDocument(Board.Strokes, Board.Background);
            tools = new InkTools(Board);
            BuildToolbar(); UpdatePageControls(); TrackInk();
            Instruments.InkCreated += delegate(System.Windows.Ink.StrokeCollection strokes) { Board.Strokes.Add(strokes); };
            Instruments.PointerInput += Board.NotifyPointerInput;
            Instruments.SetBinding(CursorProperty, new System.Windows.Data.Binding("Cursor") { Source = Board });
            SetFullscreen(true);
            RefreshLanguage();
            Localizer.LanguageChanged += RefreshLanguage;
            Closed += delegate { Localizer.LanguageChanged -= RefreshLanguage; SaveToolPreferences(); CancelToolWork(); Instruments.CancelInteraction(); };
#if !NO_PLUGINS
            settingsButton.IsEnabled = false;
            ContentRendered += OnFirstRendered;
            Closed += delegate { if (plugins != null) plugins.Dispose(); };
#endif
        }
#if !NO_PLUGINS
        private async void OnFirstRendered(object sender, EventArgs e)
        {
            if (pluginsStarted) return;
            pluginsStarted = true; plugins = PluginManager.CreateDefault();
            await Task.Run(() => plugins.Discover());
            if (!IsLoaded) { plugins.Dispose(); return; }
            plugins.LoadOnUiThread(this, Board, ToolsPanel, PluginOverlay, safeMode);
            settingsButton.IsEnabled = true;
        }
#endif
        private bool IsWriting()
        { return Board.IsStylusCaptured || Board.IsMouseCaptured || Board.AreAnyTouchesCaptured || Instruments.IsInteracting; }
        private void CaptureCurrentPage()
        { document.CurrentPage.Strokes = Board.Strokes; document.CurrentPage.Background = Board.Background; }
        private void ShowCurrentPage()
        {
            CancelToolWork(); Instruments.CancelInteraction(); ToolOptionsPopup.IsOpen = false;
            Board.Strokes = document.CurrentPage.Strokes; Board.Background = document.CurrentPage.Background;
            TrackInk(); UpdatePageControls(); Board.Focus();
        }
        private void UpdatePageControls()
        {
            if (previousPageButton == null) return;
            previousPageButton.IsEnabled = document.CanMovePrevious; nextPageButton.IsEnabled = document.CanMoveNext;
            addPageButton.IsEnabled = document.CanAddPage;
            string page = Localizer.Format("第 {0} / {1} 页", document.CurrentIndex + 1, document.Count);
            Label(previousPageButton, Localizer.T("上一页") + " · " + page);
            Label(nextPageButton, Localizer.T("下一页") + " · " + page);
            Label(addPageButton, document.CanAddPage ? Localizer.T("加页") + " · " + page : Localizer.T("已达到 100 页上限"));
        }
        private void OnAddPage(object sender, RoutedEventArgs e)
        { if (IsWriting()) return; CaptureCurrentPage(); if (document.AddPage()) ShowCurrentPage(); }
        private void OnPreviousPage(object sender, RoutedEventArgs e)
        { if (IsWriting()) return; CaptureCurrentPage(); if (document.MovePrevious()) ShowCurrentPage(); }
        private void OnNextPage(object sender, RoutedEventArgs e)
        { if (IsWriting()) return; CaptureCurrentPage(); if (document.MoveNext()) ShowCurrentPage(); }
        private void OnClearPage(object sender, RoutedEventArgs e)
        {
            if (IsWriting() || Board.Strokes.Count == 0) return;
            if (MessageBox.Show(this, Localizer.T("清空当前页面的全部墨迹吗？您无法撤销此操作。"), Localizer.T("清屏"), MessageBoxButton.OKCancel,
                MessageBoxImage.Warning, MessageBoxResult.Cancel) != MessageBoxResult.OK) return;
            CancelToolWork(); Board.Strokes.Clear(); Board.Focus();
        }
        private void OnHideToolbar(object sender, RoutedEventArgs e) { SetToolbarVisible(false); }
        private void OnShowToolbar(object sender, RoutedEventArgs e) { SetToolbarVisible(true); }
        private void OnBoardSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ToolsPanel == null) return;
            Toolbar.MaxWidth = Math.Max(0, e.NewSize.Width - 32);
            double relative = Math.Min(e.NewSize.Width / 1600, e.NewSize.Height / 900);
            double scale = Math.Round(relative > 1 ? Math.Max(0.9, 1 / relative) : 1, 2);
            if (Math.Abs(scale - toolbarScale) < 0.01) return;
            toolbarScale = scale; ToolsPanel.LayoutTransform = new ScaleTransform(scale, scale);
        }
        private void OnToolbarScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            if (ToolbarLeft == null || ToolbarRight == null || ToolbarScroll == null) return;
            ToolbarLeft.Visibility = ToolbarRight.Visibility = ToolbarScroll.ScrollableWidth > 1 ? Visibility.Visible : Visibility.Collapsed;
            ToolbarLeft.IsEnabled = ToolbarScroll.HorizontalOffset > 1;
            ToolbarRight.IsEnabled = ToolbarScroll.HorizontalOffset + 1 < ToolbarScroll.ScrollableWidth;
        }
        private void OnToolbarLeft(object sender, RoutedEventArgs e)
        { ToolbarScroll.ScrollToHorizontalOffset(ToolbarScroll.HorizontalOffset - 300 * toolbarScale); }
        private void OnToolbarRight(object sender, RoutedEventArgs e)
        { ToolbarScroll.ScrollToHorizontalOffset(ToolbarScroll.HorizontalOffset + 300 * toolbarScale); }
        private void SetToolbarVisible(bool visible)
        {
            ToolOptionsPopup.IsOpen = false;
            Toolbar.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            CollapsedToolbar.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
            Board.Focus();
        }
        private void OnToggleFullscreen(object sender, RoutedEventArgs e) { SetFullscreen(!fullscreen); }
        private void OnMinimize(object sender, RoutedEventArgs e) { ToolOptionsPopup.IsOpen = false; WindowState = WindowState.Minimized; }
        private void OnSettings(object sender, RoutedEventArgs e)
        {
            ToolOptionsPopup.IsOpen = false;
#if !NO_PLUGINS
            if (plugins != null) new SettingsWindow(plugins, safeMode, SelectLanguage) { Owner = this }.ShowDialog();
#else
            new SettingsWindow(SelectLanguage) { Owner = this }.ShowDialog();
#endif
        }
        private void SelectLanguage(string code)
        {
            Localizer.SetLanguage(code);
            SaveToolPreferences();
        }
        private static void Label(System.Windows.Controls.Button button, string text)
        {
            button.ToolTip = text;
            System.Windows.Automation.AutomationProperties.SetName(button, text);
        }
        private void RefreshLanguage()
        {
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(Localizer.CurrentLanguage);
            ToolOptionsPopup.IsOpen = false;
            foreach (var pair in buttonLabels) Label(pair.Key, Localizer.T(pair.Value));
            foreach (var pair in toolButtons)
                Label(pair.Value, InstrumentLayer.IsInstrument(pair.Key) ? Localizer.Format("新增{0}", ToolbarIcons.Name(pair.Key)) : ToolbarIcons.Name(pair.Key));
            Label(MinimizeButton, Localizer.T("最小化"));
            Label(FullscreenButton, Localizer.T(fullscreen ? "退出全屏" : "全屏"));
            Label(ToolbarLeft, Localizer.T("向左滚动")); Label(ToolbarRight, Localizer.T("向右滚动"));
            Label(ShowToolbarButton, Localizer.T("显示工具栏") + " · " + ToolbarIcons.Name(activeTool));
            UpdatePageControls();
        }
        private void SetFullscreen(bool enabled)
        {
            if (enabled)
            {
                previousState = WindowState; WindowState = WindowState.Normal; WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize; WindowState = WindowState.Maximized;
            }
            else
            { WindowState = WindowState.Normal; WindowStyle = WindowStyle.SingleBorderWindow; ResizeMode = ResizeMode.CanResize; WindowState = previousState; }
            fullscreen = enabled; FullscreenButton.Content = enabled ? "\uE73F" : "\uE740";
            string label = enabled ? Localizer.T("退出全屏") : Localizer.T("全屏"); FullscreenButton.ToolTip = label;
            System.Windows.Automation.AutomationProperties.SetName(FullscreenButton, label);
        }
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11) { SetFullscreen(!fullscreen); e.Handled = true; }
            else if (e.Key == Key.Escape && ToolOptionsPopup.IsOpen) { ToolOptionsPopup.IsOpen = false; e.Handled = true; }
            else if (e.Key == Key.Escape && fullscreen) { SetFullscreen(false); e.Handled = true; }
            else if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Control)
            { SetToolbarVisible(Toolbar.Visibility != Visibility.Visible); e.Handled = true; }
        }
        private void OnClose(object sender, RoutedEventArgs e) { Close(); }
        private void OnClosing(object sender, CancelEventArgs e)
        {
            CaptureCurrentPage();
            if (!document.HasInk) return;
            e.Cancel = MessageBox.Show(this, Localizer.T("确定退出应用程序吗？未保存的墨迹将全部丢失。"), Localizer.T("关闭"),
                MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) != MessageBoxResult.OK;
        }
    }
}
