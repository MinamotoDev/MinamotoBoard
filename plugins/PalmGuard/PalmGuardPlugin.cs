using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MinamotoBoard.PluginApi;

[assembly: AssemblyTitle("PalmGuard for MinamotoBoard")]
[assembly: AssemblyCompany("MinamotoDev")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace MinamotoBoard.Plugins.PalmGuard
{
    public sealed class PalmGuardPlugin : IBoardPlugin
    {
        private sealed class Contact
        {
            internal int Id;
            internal int Role; // 1 controls, 2 drawing surface, 3 shield, 4 shield editor.
            internal bool Allowed;
            internal StylusDevice Stylus;
            internal TouchDevice Touch;
        }

        private IBoardHost host;
        private GuardSettings settings;
        private ShieldVisual shield;
        private IDisposable overlayHandle, toolbarHandle;
        private Button toolbarButton;
        private DependencyPropertyDescriptor languageDescriptor;
        private readonly Dictionary<int, Contact> contacts = new Dictionary<int, Contact>();
        private readonly Dictionary<int, Contact> ending = new Dictionary<int, Contact>();
        private int? owner;
        private bool paused, editing, editingResize, editingMouse, shuttingDown;
        private int? editingId;
        private Point editingStart;
        private Rect editingOriginal;
        private Window settingsWindow;
        private TextBlock saveMessage;
        private Action refreshSettings;

        public string Id { get { return "MinamotoDev.PalmGuard"; } }
        public string Name { get { return "PalmGuard"; } }
        public int ApiVersion { get { return 1; } }

        public void Initialize(IBoardHost value)
        {
            if (value == null) throw new ArgumentNullException("value");
            if (host != null) throw new InvalidOperationException("PalmGuard is already initialized.");
            host = value; settings = GuardSettings.Load(); shuttingDown = false;
            shield = new ShieldVisual(settings);
            overlayHandle = host.AddOverlay(shield);
            int oldCount = host.Toolbar.Children.Count;
            toolbarHandle = host.AddToolbarButton(Text("Name"), ShowSettings);
            if (host.Toolbar.Children.Count > oldCount)
            {
                toolbarButton = host.Toolbar.Children[oldCount] as Button;
                if (toolbarButton != null) toolbarButton.Content = ShieldIcon();
            }
            InputManager.Current.PreProcessInput += OnInput;
            host.MainWindow.Deactivated += OnDeactivated;
            languageDescriptor = DependencyPropertyDescriptor.FromProperty(FrameworkElement.LanguageProperty, typeof(Window));
            languageDescriptor.AddValueChanged(host.MainWindow, OnLanguageChanged);
            RefreshLanguage();
        }

        private static FrameworkElement ShieldIcon()
        {
            Geometry geometry = Geometry.Parse("M16,3 L28,8 V17 C28,24 22,28 16,30 C10,28 4,24 4,17 V8 Z M10,16 L14,20 22,11");
            geometry.Freeze();
            Pen pen = new Pen(new SolidColorBrush(Color.FromRgb(23, 63, 43)), 1.65);
            pen.StartLineCap = pen.EndLineCap = PenLineCap.Round; pen.LineJoin = PenLineJoin.Round; pen.Freeze();
            DrawingGroup group = new DrawingGroup();
            group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 32, 32))));
            group.Children.Add(new GeometryDrawing(null, pen, geometry)); group.Freeze();
            return new Image { Source = new DrawingImage(group), Width = 28, Height = 28 };
        }

        private void OnLanguageChanged(object sender, EventArgs e) { RefreshLanguage(); }
        private void RefreshLanguage()
        {
            if (host == null) return;
            if (toolbarButton != null)
            {
                toolbarButton.ToolTip = Text("Name");
                toolbarButton.Opacity = settings.Enabled ? 1 : 0.45;
                System.Windows.Automation.AutomationProperties.SetName(toolbarButton, Text("Name"));
            }
            if (refreshSettings != null) refreshSettings();
        }

        // Mark routed preview packets handled, rather than canceling the input manager's raw
        // packets: WPF must still run its TouchUp/deactivation and mouse-promotion cleanup.
        private void OnInput(object sender, PreProcessInputEventArgs e)
        {
            if (host == null || shuttingDown || !settings.Enabled) return;
            InputEventArgs input = e.StagingItem.Input;
            StylusEventArgs stylus = input as StylusEventArgs;
            TouchEventArgs touch = input as TouchEventArgs;
            MouseEventArgs mouse = input as MouseEventArgs;
            int id, action;
            Point point;
            if (stylus != null)
            {
                if (stylus.StylusDevice.TabletDevice.Type != TabletDeviceType.Touch) return;
                id = stylus.StylusDevice.Id;
                action = input.RoutedEvent == Stylus.PreviewStylusDownEvent ? 1 :
                    input.RoutedEvent == Stylus.PreviewStylusMoveEvent ? 2 :
                    input.RoutedEvent == Stylus.PreviewStylusUpEvent || input.RoutedEvent == Stylus.PreviewStylusOutOfRangeEvent ? 3 : 0;
                point = stylus.GetPosition(shield);
            }
            else if (touch != null)
            {
                id = touch.TouchDevice.Id;
                action = input.RoutedEvent == UIElement.PreviewTouchDownEvent ? 1 :
                    input.RoutedEvent == UIElement.PreviewTouchMoveEvent ? 2 :
                    input.RoutedEvent == UIElement.PreviewTouchUpEvent ? 3 : 0;
                point = touch.GetTouchPoint(shield).Position;
            }
            else
            {
                if (mouse == null) return;
                if (mouse.StylusDevice != null)
                {
                    if (mouse.StylusDevice.TabletDevice.Type != TabletDeviceType.Touch) return;
                    Contact promoted;
                    bool allowed = contacts.TryGetValue(mouse.StylusDevice.Id, out promoted)
                        || ending.TryGetValue(mouse.StylusDevice.Id, out promoted);
                    if (allowed && !promoted.Allowed) input.Handled = true;
                    return;
                }
                HandleEditorMouse(mouse); return;
            }

            Contact contact;
            bool known = contacts.TryGetValue(id, out contact);
            DependencyObject target = input.OriginalSource as DependencyObject ?? input.Source as DependencyObject ?? input.Device.Target as DependencyObject;
            if (!known && (target == null || Window.GetWindow(target) != host.MainWindow)) return;

            if (action == 0)
            {
                if (known && !contact.Allowed) input.Handled = true;
                return;
            }
            if (action == 1 && !known)
            {
                ending.Remove(id);
                contact = new Contact { Id = id };
                if (IsControl(target)) { contact.Role = 1; contact.Allowed = true; }
                else if (editing && settings.ShieldEnabled && EditorHit(point))
                { contact.Role = 4; BeginEdit(id, point); }
                else if (settings.ShieldEnabled && shield.Area.Contains(point)) contact.Role = 3;
                else
                {
                    contact.Role = 2;
                    if (!owner.HasValue && !paused) { owner = id; contact.Allowed = true; }
                }
                contacts.Add(id, contact); known = true;
                if (contact.Role == 2 && !contact.Allowed && settings.PauseOnMultiple) PauseDrawing();
            }
            if (!known)
            {
                // WPF touch and stylus representations share the contact ID. Retain the up
                // decision until all synchronously promoted packets have left the staging area.
                if (ending.TryGetValue(id, out contact)) { if (!contact.Allowed) input.Handled = true; }
                return;
            }
            if (stylus != null) contact.Stylus = stylus.StylusDevice;
            if (touch != null) contact.Touch = touch.TouchDevice;

            if (contact.Role == 4 && action == 2 && editingId == id) MoveEditor(point);
            if (contact.Role == 2 && contact.Allowed && action == 2
                && settings.ShieldEnabled && shield.Area.Contains(point)) PauseDrawing();
            bool accepted = contact.Allowed;
            if (!accepted) input.Handled = true;
            if (action == 3)
            {
                if (editingId == id) { editingId = null; SaveSettings(); }
                contacts.Remove(id); ending[id] = contact;
                host.MainWindow.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(delegate
                {
                    Contact previous;
                    if (ending.TryGetValue(id, out previous) && ReferenceEquals(previous, contact)) ending.Remove(id);
                }));
                bool drawingContacts = false;
                foreach (Contact remaining in contacts.Values) if (remaining.Role == 2) { drawingContacts = true; break; }
                if (!drawingContacts) { owner = null; paused = false; }
            }
        }

        private bool IsControl(DependencyObject target)
        {
            for (DependencyObject current = target; current != null; )
            {
                if (ReferenceEquals(current, host.Toolbar)) return true;
                if (current is ButtonBase || current is TextBoxBase || current is Selector || current is Thumb
                    || current is ScrollBar || current is MenuItem || current is Hyperlink || current is PasswordBox) return true;
                if (ReferenceEquals(current, host.MainWindow)) break;
                current = current is Visual ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
            }
            return false;
        }

        private void PauseDrawing()
        {
            if (paused) return;
            paused = true;
            foreach (Contact contact in contacts.Values) if (contact.Role == 2) contact.Allowed = false;
            Contact active;
            if (owner.HasValue && contacts.TryGetValue(owner.Value, out active)) ReleaseContact(active);
        }
        private static void ReleaseContact(Contact contact)
        {
            if (contact.Touch != null && contact.Touch.Captured != null) contact.Touch.Capture(null);
            if (contact.Stylus != null && contact.Stylus.Captured != null) contact.Stylus.Capture(null);
        }
        private void ResetInput()
        {
            foreach (Contact contact in contacts.Values) if (contact.Role == 2) ReleaseContact(contact);
            contacts.Clear(); ending.Clear(); owner = null; paused = false; editingId = null; editingMouse = false;
        }
        private void OnDeactivated(object sender, EventArgs e) { ResetInput(); }

        private bool EditorHit(Point point)
        { Rect area = shield.Area; area.Inflate(16, 16); return area.Contains(point); }
        private void BeginEdit(int id, Point point)
        {
            if (editingId.HasValue || editingMouse) return;
            editingId = id; editingStart = point; editingOriginal = shield.Area;
            editingResize = (point - editingOriginal.BottomRight).Length <= 28;
        }
        private void MoveEditor(Point point)
        {
            Rect rect = editingOriginal;
            Vector delta = point - editingStart;
            if (editingResize)
            {
                rect.Width = Math.Max(48, Math.Min(shield.ActualWidth - rect.X, rect.Width + delta.X));
                rect.Height = Math.Max(48, Math.Min(shield.ActualHeight - rect.Y, rect.Height + delta.Y));
            }
            else
            {
                rect.X = Math.Max(0, Math.Min(shield.ActualWidth - rect.Width, rect.X + delta.X));
                rect.Y = Math.Max(0, Math.Min(shield.ActualHeight - rect.Height, rect.Y + delta.Y));
            }
            shield.Apply(rect);
        }
        private void HandleEditorMouse(MouseEventArgs e)
        {
            if (!editing || !settings.ShieldEnabled) return;
            DependencyObject target = e.OriginalSource as DependencyObject ?? e.Source as DependencyObject ?? e.Device.Target as DependencyObject;
            if (target == null || Window.GetWindow(target) != host.MainWindow || IsControl(target)) return;
            Point point = e.GetPosition(shield);
            MouseButtonEventArgs button = e as MouseButtonEventArgs;
            if (e.RoutedEvent == Mouse.PreviewMouseDownEvent && button != null
                && button.ChangedButton == MouseButton.Left && EditorHit(point) && !editingId.HasValue)
            {
                BeginEdit(int.MinValue, point); editingId = null; editingMouse = true; e.Handled = true;
            }
            else if (editingMouse && e.RoutedEvent == Mouse.PreviewMouseMoveEvent)
            {
                if (e.LeftButton == MouseButtonState.Pressed) MoveEditor(point);
                else { editingMouse = false; SaveSettings(); }
                e.Handled = true;
            }
            else if (editingMouse && e.RoutedEvent == Mouse.PreviewMouseUpEvent && button != null && button.ChangedButton == MouseButton.Left)
            { MoveEditor(point); editingMouse = false; SaveSettings(); e.Handled = true; }
        }

        private void SaveSettings()
        {
            try { settings.Save(); if (saveMessage != null) saveMessage.Text = ""; }
            catch (Exception ex) { Trace.WriteLine(ex); if (saveMessage != null) saveMessage.Text = Text("SaveFailed"); }
            RefreshLanguage();
            if (shield != null) shield.InvalidateVisual();
        }

        private void ShowSettings()
        {
            if (settingsWindow != null) { settingsWindow.Activate(); return; }
            Window window = new Window { Owner = host.MainWindow, Width = 380, MinWidth = 340,
                SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false, Icon = host.MainWindow.Icon };
            StackPanel body = new StackPanel { Margin = new Thickness(18) };
            CheckBox enabled = new CheckBox { IsChecked = settings.Enabled, Margin = new Thickness(0, 0, 0, 14) };
            TextBlock modeLabel = new TextBlock { Margin = new Thickness(0, 0, 0, 6) };
            ComboBox mode = new ComboBox { MinHeight = 36, VerticalContentAlignment = VerticalAlignment.Center };
            CheckBox useShield = new CheckBox { IsChecked = settings.ShieldEnabled, Margin = new Thickness(0, 16, 0, 12) };
            CheckBox editShield = new CheckBox { Margin = new Thickness(0, 0, 0, 14) };
            Button close = new Button { MinWidth = 86, MinHeight = 32, HorizontalAlignment = HorizontalAlignment.Right, Padding = new Thickness(10, 4, 10, 4) };
            saveMessage = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Firebrick, Margin = new Thickness(0, 0, 0, 8) };
            body.Children.Add(enabled); body.Children.Add(modeLabel); body.Children.Add(mode);
            body.Children.Add(useShield); body.Children.Add(editShield); body.Children.Add(saveMessage); body.Children.Add(close);
            bool refreshing = false;
            refreshSettings = delegate
            {
                refreshing = true;
                try
                {
                    window.Title = Text("Name"); window.Language = host.MainWindow.Language;
                    enabled.Content = Text("Enabled"); modeLabel.Text = Text("Mode");
                    mode.ItemsSource = new[] { Text("Single"), Text("Pause") };
                    mode.SelectedIndex = settings.PauseOnMultiple ? 1 : 0; mode.ToolTip = Text("PauseHint");
                    useShield.Content = Text("Shield"); editShield.Content = Text("Edit"); editShield.ToolTip = Text("EditHint");
                    mode.IsEnabled = useShield.IsEnabled = settings.Enabled;
                    editShield.IsEnabled = settings.Enabled && settings.ShieldEnabled;
                    close.Content = Text("Close");
                }
                finally { refreshing = false; }
            };
            RoutedEventHandler changed = delegate
            {
                if (refreshing) return;
                ResetInput(); settings.Enabled = enabled.IsChecked == true;
                settings.ShieldEnabled = useShield.IsChecked == true;
                editing = settings.Enabled && settings.ShieldEnabled && editShield.IsChecked == true;
                shield.Editing = editing; SaveSettings();
            };
            enabled.Checked += changed; enabled.Unchecked += changed; useShield.Checked += changed; useShield.Unchecked += changed;
            editShield.Checked += changed; editShield.Unchecked += changed;
            mode.SelectionChanged += delegate
            {
                if (refreshing || mode.SelectedIndex < 0) return;
                ResetInput(); settings.PauseOnMultiple = mode.SelectedIndex == 1; SaveSettings();
            };
            close.Click += delegate { window.Close(); };
            window.Closed += delegate
            {
                editing = shield.Editing = false; editingId = null; editingMouse = false;
                refreshSettings = null; settingsWindow = null; saveMessage = null; SaveSettings();
            };
            window.Content = body; settingsWindow = window; RefreshLanguage(); window.Show();
        }

        private string Text(string key)
        {
            string language = host == null ? CultureInfo.CurrentUICulture.Name : host.MainWindow.Language.IetfLanguageTag;
            int column = language.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? 1 :
                language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ?
                (language.IndexOf("TW", StringComparison.OrdinalIgnoreCase) >= 0 || language.IndexOf("HK", StringComparison.OrdinalIgnoreCase) >= 0 || language.IndexOf("Hant", StringComparison.OrdinalIgnoreCase) >= 0 ? 3 : 2) : 0;
            string[] values;
            return Translations.TryGetValue(key, out values) ? values[column] : key;
        }
        private static readonly Dictionary<string, string[]> Translations = new Dictionary<string, string[]>
        {
            { "Name", new[] { "PalmGuard", "誤タッチ防止", "防误触", "防誤觸" } },
            { "Enabled", new[] { "Enable protection", "誤タッチ防止を有効にする", "启用防误触", "啟用防誤觸" } },
            { "Mode", new[] { "Mode", "モード", "模式", "模式" } },
            { "Single", new[] { "Single contact", "最初のタッチのみ", "单点锁定", "單點鎖定" } },
            { "Pause", new[] { "Pause on multiple contacts", "複数のタッチで一時停止", "多点暂停", "多點暫停" } },
            { "PauseHint", new[] { "Keeps ink already drawn. Resume after all drawing contacts lift.", "描画済みの筆跡は保持し、すべての描画タッチが離れると再開します。", "保留已经写入的内容，所有画板触点离屏后恢复。", "保留已寫入的內容，所有畫板觸點離開螢幕後恢復。" } },
            { "Shield", new[] { "Palm exclusion area", "タッチを無視する領域", "手掌屏蔽区", "手掌屏蔽區" } },
            { "Edit", new[] { "Adjust exclusion area", "領域の位置・サイズを調整", "调整屏蔽区", "調整屏蔽區" } },
            { "EditHint", new[] { "Drag the area to move it; drag its blue corner to resize. Close this window to lock it.", "領域をドラッグして移動し、青い角でサイズを調整します。このウィンドウを閉じると固定します。", "拖动区域移动，拖动蓝色角点调整大小；关闭此窗口后锁定。", "拖曳區域可移動，拖曳藍色角點可調整大小；關閉此視窗後鎖定。" } },
            { "Close", new[] { "Close", "閉じる", "关闭", "關閉" } },
            { "SaveFailed", new[] { "Unable to save plugin settings.", "プラグイン設定を保存できません。", "无法保存插件设置。", "無法儲存外掛程式設定。" } }
        };

        public void Shutdown()
        {
            if (host == null) return;
            shuttingDown = true;
            InputManager.Current.PreProcessInput -= OnInput;
            host.MainWindow.Deactivated -= OnDeactivated;
            if (languageDescriptor != null) languageDescriptor.RemoveValueChanged(host.MainWindow, OnLanguageChanged);
            ResetInput();
            if (settingsWindow != null) settingsWindow.Close();
            SaveSettings();
            if (toolbarHandle != null) toolbarHandle.Dispose();
            if (overlayHandle != null) overlayHandle.Dispose();
            toolbarHandle = overlayHandle = null; toolbarButton = null; shield = null; host = null;
        }
    }
}
