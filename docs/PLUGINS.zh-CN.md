# MinamotoBoard 插件 · API 1

[English](PLUGINS.md) | [日本語](PLUGINS.ja.md) | **简体中文** | [正體中文](PLUGINS.zh-TW.md)


## 安装与管理

通过**设置 → 插件 → 打开插件目录**访问。路径由 `Environment.SpecialFolder.MyDocuments` 解析，支持文档迁移和 OneDrive 重定向。通常位于 `%USERPROFILE%\Documents\MinamotoDev\Board\Plugins`，每个 Windows 用户独立。

将 `*.Plugin.dll` 入口放在根目录或一层子目录中：

```text
Plugins/
  BoardTheme.Plugin.dll
  OtherPlugin/
    Other.Plugin.dll
    OtherDependency.dll
```

白板首次显示后在后台发现文件，再在 UI 线程初始化。跳过子目录中的目录联接和符号链接。每个入口 DLL 必须包含一个公开、非抽象、具有无参构造函数的 `IBoardPlugin` 实现。相邻依赖 DLL 由 .NET Framework LoadFrom 解析；API 程序集使用宿主提供的版本，不要附带另一份副本。

勾选启用项并点击**保存**，下次启动生效。添加、替换或删除 DLL 前关闭白板；当前 AppDomain 无法单独卸载某个程序集。

启用设置以临时文件替换方式写入 `%LOCALAPPDATA%\MinamotoDev\Board\disabled-plugins.txt`，按入口 DLL 相对路径匹配。更名或移动已禁用 DLL 会成为新入口；配置无法读取时停止发现。

插件导致启动卡住时，在程序目录中运行：

```powershell
.\MinamotoBoard.exe --safe-mode
```

安全模式允许发现文件和管理设置，不加载或执行插件代码。

## 开发

目标为 **.NET Framework 4.8、x64、WPF**，引用 `MinamotoBoard.PluginApi.dll`。API 程序集版本为 `1.0.0.0`，`ApiVersion` 为 `1`。

[BoardTheme 示例](../samples/BoardTheme.Plugin/)构建后生成 `BoardTheme.Plugin.dll`，展示工具栏按钮与覆盖层。将输出复制到插件目录后重启。

```csharp
using MinamotoBoard.PluginApi;
using System.Windows.Media;

public sealed class MyPlugin : IBoardPlugin
{
    public string Id { get { return "MyCompany.MyPlugin"; } }
    public string Name { get { return "My Plugin"; } }
    public int ApiVersion { get { return 1; } }
    public void Initialize(IBoardHost host)
    {
        host.AddToolbarButton("Blackboard", () => host.Canvas.Background = Brushes.Black);
    }
    public void Shutdown() { }
}
```

| API | 用途 |
| --- | --- |
| `Canvas` | 原生 InkCanvas 的笔迹集合、背景和绘图属性。 |
| `MainWindow` | WPF 主窗口、资源、样式和事件。 |
| `Toolbar` | 可添加 WPF 控件的底部工具栏 Panel。 |
| `Overlay` | 画布上方、工具栏下方的 Grid，用于自定义面板或标记。 |
| `AddToolbarButton(label, action)` | 添加带异常捕获的按钮，返回 IDisposable 移除句柄。 |
| `AddOverlay(element)` | 添加覆盖层，返回 IDisposable 移除句柄。 |

Initialize、Shutdown 和按钮回调运行在 UI 线程，必须快速返回。耗时 I/O 和计算放到后台，UI 更新通过 Dispatcher 执行。触摸处理应局限于插件自身，避免每帧扫描全部笔迹；装饰性覆盖层设置 `IsHitTestVisible=false`。

宿主会在初始化失败和退出时清理通过辅助方法添加的控件。直接修改 Panel 或订阅事件的插件，应在 Shutdown 中恢复状态、取消订阅。优先使用 API，避免依赖工具栏内部名称或布局。

切换页面会替换 `Canvas.Strokes`。操作时重新读取该属性；订阅者应处理 `Canvas.StrokesReplaced` 并解除旧集合订阅。自定义覆盖层不随页面自动切换，也不随笔迹清屏删除。

## 错误与权限

- 重复 Id、不兼容的 API 版本、无效入口和损坏 DLL 会显示加载失败，继续处理其他插件。
- 初始化失败时调用 Shutdown，并移除通过宿主 API 添加的控件。
- `AddToolbarButton` 回调出现异常时记录失败并禁用按钮。
- 插件与宿主同进程、同权限运行，没有沙箱、进程隔离或热卸载；失控插件会影响整个白板。
- 仅使用可信插件。插件可以访问笔迹、用户文件和网络，自行处理直接挂接的 WPF 事件及后台线程中的错误。
- 卸载默认保留插件数据；勾选**删除配置和插件**后，在卸载成功时删除。

[Assembly.LoadFrom](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assembly.loadfrom?view=netframework-4.8) · [AppDomain 卸载说明](https://learn.microsoft.com/en-us/dotnet/framework/app-domains/how-to-unload-an-application-domain)
