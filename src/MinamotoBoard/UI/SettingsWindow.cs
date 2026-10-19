using MinamotoBoard.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
#if !NO_PLUGINS
using MinamotoBoard.Plugins;
#endif

namespace MinamotoBoard.UI
{
    public sealed class SettingsWindow : Window
    {
        private readonly TabControl sections;
        private readonly List<Action> localizedLabels = new List<Action>();

        private void BindText(Action update) { localizedLabels.Add(update); update(); }
        private void RefreshLanguage()
        {
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(Localizer.CurrentLanguage);
            foreach (Action update in localizedLabels) update();
        }

        public SettingsWindow(Action<string> selectLanguage)
        {
            BindText(delegate { Title = Localizer.T("设置"); }); Width = 720; Height = 540;
            MinWidth = 480; MinHeight = 360; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;
            sections = new TabControl { Margin = new Thickness(12) };
            StackPanel display = new StackPanel { Margin = new Thickness(20) };
            TextBlock languageLabel = new TextBlock { Margin = new Thickness(0, 0, 0, 8) };
            BindText(delegate { languageLabel.Text = Localizer.T("语言"); });
            ComboBox languages = new ComboBox { ItemsSource = Localizer.Names, Width = 240,
                HorizontalAlignment = HorizontalAlignment.Left, MinHeight = 36,
                VerticalContentAlignment = VerticalAlignment.Center,
                SelectedIndex = Array.IndexOf(Localizer.Codes, Localizer.CurrentLanguage) };
            Style languageItem = new Style(typeof(ComboBoxItem));
            languageItem.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            languageItem.Setters.Add(new Setter(Control.MinHeightProperty, 32.0));
            languages.ItemContainerStyle = languageItem;
            BindText(delegate { System.Windows.Automation.AutomationProperties.SetName(languages, Localizer.T("语言")); });
            languages.SelectionChanged += delegate
            {
                if (languages.SelectedIndex < 0) return;
                selectLanguage(Localizer.Codes[languages.SelectedIndex]);
            };
            display.Children.Add(languageLabel); display.Children.Add(languages);
            TabItem interfaceTab = new TabItem { Content = display };
            BindText(delegate { interfaceTab.Header = Localizer.T("界面"); });
            sections.Items.Add(interfaceTab);
            TabItem aboutTab = new TabItem { Content = CreateAboutPage() };
            BindText(delegate { aboutTab.Header = Localizer.T("关于"); });
            sections.Items.Add(aboutTab);
            RefreshLanguage();
            Localizer.LanguageChanged += RefreshLanguage;
            Closed += delegate { Localizer.LanguageChanged -= RefreshLanguage; };
            Content = sections;
        }

#if !NO_PLUGINS
        public SettingsWindow(PluginManager manager, bool safeMode, Action<string> selectLanguage) : this(selectLanguage)
        {
            DockPanel layout = new DockPanel { Margin = new Thickness(12) };
            TabItem pluginTab = new TabItem { Content = layout };
            BindText(delegate { pluginTab.Header = Localizer.T("插件"); });
            sections.Items.Insert(1, pluginTab);
            sections.SelectedIndex = 0;
            TextBlock info = new TextBlock {
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 12) };
            BindText(delegate { info.Text = manager.PluginDirectory + "\n" + Localizer.T("仅使用可信插件，重启后生效。") + (safeMode ? "\n" + Localizer.T("安全模式") : ""); });
            DockPanel.SetDock(info, Dock.Top); layout.Children.Add(info);
            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal };
            DockPanel.SetDock(buttons, Dock.Bottom); layout.Children.Add(buttons);
            Button open = new Button { Content = "打开插件目录", MinHeight = 48, Padding = new Thickness(12) };
            Button save = new Button { Content = "保存", MinHeight = 48, Padding = new Thickness(12), Margin = new Thickness(8, 0, 0, 0) };
            BindText(delegate { open.Content = Localizer.T("打开插件目录"); save.Content = Localizer.T("保存"); });
            buttons.Children.Add(open); buttons.Children.Add(save);
            TextBlock message = new TextBlock { Text = manager.DiscoveryError ?? "", TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 8) };
            DockPanel.SetDock(message, Dock.Bottom); layout.Children.Add(message);
            StackPanel list = new StackPanel();
            layout.Children.Add(new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            Dictionary<PluginRecord, CheckBox> choices = new Dictionary<PluginRecord, CheckBox>();
            foreach (PluginRecord record in manager.Records)
            {
                CheckBox choice = new CheckBox { IsChecked = record.Enabled, MinHeight = 52,
                    Content = new TextBlock { Text = record.Name + "\n" + record.RelativePath + " · " + record.Status,
                        TextWrapping = TextWrapping.Wrap }, Margin = new Thickness(0, 4, 0, 4) };
                BindText(delegate { ((TextBlock)choice.Content).Text = record.Name + "\n" + record.RelativePath + " · " + record.Status; });
                list.Children.Add(choice); choices.Add(record, choice);
            }
            if (choices.Count == 0)
            {
                TextBlock empty = new TextBlock { TextWrapping = TextWrapping.Wrap };
                BindText(delegate { empty.Text = Localizer.T("暂无插件"); }); list.Children.Add(empty);
            }
            open.Click += delegate
            {
                try { Process.Start(new ProcessStartInfo(manager.PluginDirectory) { UseShellExecute = true }); }
                catch (Exception ex) { string detail = ex.Message; BindMessage(message, "无法打开目录：", detail); }
            };
            save.Click += async delegate
            {
                string[] disabled = choices.Where(pair => pair.Value.IsChecked != true).Select(pair => pair.Key.RelativePath).ToArray();
                save.IsEnabled = false;
                try
                {
                    await Task.Run(() => manager.SaveDisabled(disabled));
                    foreach (var pair in choices) pair.Key.Enabled = !disabled.Contains(pair.Key.RelativePath);
                    BindMessage(message, "已保存，重启后生效。", "");
                }
                catch (Exception ex) { BindMessage(message, "保存设置失败：", ex.Message); }
                finally { save.IsEnabled = true; }
            };
        }
#endif

        private readonly Dictionary<TextBlock, Action> messageBindings = new Dictionary<TextBlock, Action>();
        private void BindMessage(TextBlock control, string key, string detail)
        {
            Action previous;
            if (messageBindings.TryGetValue(control, out previous)) localizedLabels.Remove(previous);
            Action update = delegate { control.Text = Localizer.T(key) + detail; };
            messageBindings[control] = update; BindText(update);
        }

        private UIElement CreateAboutPage()
        {
            StackPanel layout = new StackPanel { Margin = new Thickness(20) };
            layout.Children.Add(new TextBlock { Text = "MinamotoBoard " + typeof(SettingsWindow).Assembly.GetName().Version.ToString(3), FontSize = 22,
                FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 16) });
            const string repository = "https://github.com/MinamotoDev/MinamotoBoard";
            DockPanel project = new DockPanel { Margin = new Thickness(0, 0, 0, 24) };
            Button github = new Button { Content = GitHubMark(), Width = 56, Height = 56,
                Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand, ToolTip = repository };
            FrameworkElementFactory surface = new FrameworkElementFactory(typeof(Border));
            surface.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            surface.AppendChild(content);
            github.Template = new ControlTemplate(typeof(Button)) { VisualTree = surface };
            BindText(delegate { System.Windows.Automation.AutomationProperties.SetName(github, Localizer.T("访问 GitHub 项目")); });
            github.Click += delegate { OpenLink(repository); };
            DockPanel.SetDock(github, Dock.Left); project.Children.Add(github);
            TextBlock projectLink = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 0, 0) };
            projectLink.Inlines.Add(CreateLink("MinamotoDev/MinamotoBoard", repository));
            project.Children.Add(projectLink);
            layout.Children.Add(project);
            TextBlock contributors = new TextBlock { FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 12) };
            BindText(delegate { contributors.Text = Localizer.T("贡献者"); }); layout.Children.Add(contributors);
            layout.Children.Add(Maintainer("MinamotoDev", "MinamotoDev.png", "https://github.com/MinamotoDev"));
            layout.Children.Add(Maintainer("丹羽　空", "NiwaSora.jpg", null));
            return new ScrollViewer { Content = layout, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        }

        private UIElement Maintainer(string name, string avatar, string address)
        {
            DockPanel row = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };
            Image image = AssetImage(avatar, 56);
            image.Margin = new Thickness(0, 0, 14, 0);
            DockPanel.SetDock(image, Dock.Left); row.Children.Add(image);
            TextBlock label = new TextBlock { FontSize = 16, VerticalAlignment = VerticalAlignment.Center };
            if (address == null) label.Text = name;
            else label.Inlines.Add(CreateLink(name, address));
            row.Children.Add(label);
            return row;
        }

        private static Image AssetImage(string name, double size)
        {
            Image image = new Image { Width = size, Height = size,
                Source = new BitmapImage(new Uri("pack://application:,,,/Assets/About/" + name, UriKind.Absolute)) };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            return image;
        }

        private static UIElement GitHubMark()
        {
            // Official Primer Octicons mark-github-16 path; vector rendering has no bitmap background.
            System.Windows.Resources.StreamResourceInfo resource = Application.GetResourceStream(
                new Uri("pack://application:,,,/Assets/About/GitHub-Mark.geometry", UriKind.Absolute));
            using (StreamReader reader = new StreamReader(resource.Stream, Encoding.UTF8, true))
            {
                Geometry geometry = Geometry.Parse(reader.ReadToEnd());
                geometry.Freeze();
                return new System.Windows.Shapes.Path { Data = geometry, Fill = Brushes.Black,
                    Width = 44, Height = 44, Stretch = Stretch.Uniform };
            }
        }

        private Hyperlink CreateLink(string text, string address)
        {
            Hyperlink link = new Hyperlink(new Run(text)) { NavigateUri = new Uri(address) };
            link.RequestNavigate += delegate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
            {
                OpenLink(address); e.Handled = true;
            };
            return link;
        }

        private void OpenLink(string address)
        {
            try { Process.Start(new ProcessStartInfo(address) { UseShellExecute = true }); }
            catch (Exception) { MessageBox.Show(this, Localizer.T("无法打开链接。"), "MinamotoBoard", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}
