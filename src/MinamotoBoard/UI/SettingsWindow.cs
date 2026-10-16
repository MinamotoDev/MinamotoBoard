using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MinamotoBoard.Plugins;

namespace MinamotoBoard.UI
{
    public sealed class SettingsWindow : Window
    {
        public SettingsWindow(PluginManager manager, bool safeMode)
        {
            Title = "设置"; Width = 720; Height = 540;
            MinWidth = 480; MinHeight = 360; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            DockPanel layout = new DockPanel { Margin = new Thickness(12) };
            TabControl sections = new TabControl { Margin = new Thickness(12) };
            sections.Items.Add(new TabItem { Header = "插件", Content = layout });
            Content = sections;
            TextBlock info = new TextBlock { Text = manager.PluginDirectory +
                "\n仅使用可信插件，重启后生效。" + (safeMode ? "\n安全模式" : ""),
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 12) };
            DockPanel.SetDock(info, Dock.Top); layout.Children.Add(info);
            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal };
            DockPanel.SetDock(buttons, Dock.Bottom); layout.Children.Add(buttons);
            Button open = new Button { Content = "打开插件目录", MinHeight = 48, Padding = new Thickness(12) };
            Button save = new Button { Content = "保存", MinHeight = 48, Padding = new Thickness(12), Margin = new Thickness(8, 0, 0, 0) };
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
                list.Children.Add(choice); choices.Add(record, choice);
            }
            if (choices.Count == 0) list.Children.Add(new TextBlock { Text = "暂无插件", TextWrapping = TextWrapping.Wrap });
            open.Click += delegate
            {
                try { Process.Start(new ProcessStartInfo(manager.PluginDirectory) { UseShellExecute = true }); }
                catch (Exception ex) { message.Text = "无法打开目录：" + ex.Message; }
            };
            save.Click += async delegate
            {
                string[] disabled = choices.Where(pair => pair.Value.IsChecked != true).Select(pair => pair.Key.RelativePath).ToArray();
                save.IsEnabled = false;
                try
                {
                    await Task.Run(() => manager.SaveDisabled(disabled));
                    foreach (var pair in choices) pair.Key.Enabled = !disabled.Contains(pair.Key.RelativePath);
                    message.Text = "已保存，重启后生效。";
                }
                catch (Exception ex) { message.Text = "保存设置失败：" + ex.Message; }
                finally { save.IsEnabled = true; }
            };
        }
    }
}
