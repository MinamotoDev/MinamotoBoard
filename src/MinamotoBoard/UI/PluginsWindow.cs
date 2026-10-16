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
    public sealed class PluginsWindow : Window
    {
        public PluginsWindow(PluginManager manager, bool safeMode)
        {
            Title = "MinamotoBoard · Plugins"; Width = 720; Height = 540;
            MinWidth = 480; MinHeight = 360; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            DockPanel layout = new DockPanel { Margin = new Thickness(20) };
            Content = layout;
            TextBlock heading = new TextBlock { Text = "Plugins" + (safeMode ? " · 安全模式" : ""), FontSize = 24 };
            DockPanel.SetDock(heading, Dock.Top); layout.Children.Add(heading);
            TextBlock info = new TextBlock { Text = manager.PluginDirectory +
                "\n仅安装可信插件。插件具有与白板相同的用户权限，可修改界面和笔迹。\n勾选表示启用；添加、移除及启用设置在重启白板后生效。",
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 12) };
            DockPanel.SetDock(info, Dock.Top); layout.Children.Add(info);
            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal };
            DockPanel.SetDock(buttons, Dock.Bottom); layout.Children.Add(buttons);
            Button open = new Button { Content = "打开插件目录", MinHeight = 48, Padding = new Thickness(12) };
            Button save = new Button { Content = "保存启用设置", MinHeight = 48, Padding = new Thickness(12), Margin = new Thickness(8, 0, 0, 0) };
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
            if (choices.Count == 0) list.Children.Add(new TextBlock { Text = "暂无插件。将 *.Plugin.dll 放入插件目录后重启白板。", TextWrapping = TextWrapping.Wrap });
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
                    message.Text = "已保存，请自行关闭并重新打开白板。当前笔迹不会因修改设置而被清除。";
                }
                catch (Exception ex) { message.Text = "保存设置失败：" + ex.Message; }
                finally { save.IsEnabled = true; }
            };
        }
    }
}
