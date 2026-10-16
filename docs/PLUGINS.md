# MinamotoBoard Plugins · API 1

## 安装与管理

在白板底部点击 **Plugins → 打开插件目录**。
真实路径由 Windows 的 `Environment.SpecialFolder.MyDocuments` 解析，默认通常是：

```text
%USERPROFILE%\Documents\MinamotoDev\Board\Plugins
```

若文档被迁移或由 OneDrive 重定向，会使用 Windows 返回的文档目录，不写入拼错的 `Ducuments`，也不依赖不存在的 `%USERPROFILES%`。
这是每个 Windows 用户独立的插件位置，与 Program Files 安装目录无关。

可将插件放在根目录，或放在一个直接子目录中：

```text
Plugins/
  BoardTheme.Plugin.dll
  OtherPlugin/
    Other.Plugin.dll
    OtherDependency.dll
```

启动后首次显示白板，再在后台发现文件，随后在 UI 线程初始化插件。
只扫描 `*.Plugin.dll`，根目录及一层子目录；跳过子目录中的目录联接/符号链接。
无需加载时轮询文件系统。每个入口 DLL 必须且只能包含一个公开、非抽象、有无参构造函数的 `IBoardPlugin` 实现。
旁边的依赖 DLL 由 .NET Framework LoadFrom 机制解析，插件 API DLL 使用宿主提供的版本，不要附带另一个 API 副本。

插件列表显示加载/失败/禁用状态。勾选或取消后点击“保存启用设置”，**下次启动生效**。
添加、升级、移除 DLL 前请关闭白板；Framework 不能从当前 AppDomain 单独卸载程序集。
启用设置保存于 `%LOCALAPPDATA%\MinamotoDev\Board\disabled-plugins.txt`，采用临时文件及替换写入。
禁用配置按入口文件的相对路径匹配，更名或移动被禁用的 DLL 会成为新入口。
如果配置无法读取，停止本次发现，不冒险加载原本已禁用的插件。

插件导致启动卡住时：

```powershell
& 'C:\Program Files\MinamotoDev\Board\MinamotoBoard.exe' --safe-mode
```

安全模式会发现文件并允许管理启用设置，但不加载程序集或执行插件代码。

## 开发

目标 **.NET Framework 4.8、x64、WPF**，引用 `MinamotoBoard.PluginApi.dll`。
接口程序集版本固定 `1.0.0.0`，当前 API 号为 `1`。
在主解决方案中有完整示例 `samples/BoardTheme.Plugin`，构建后得到 `BoardTheme.Plugin.dll`。
示例新增“切换黑板”按钮和右上角标记，可验证新增行为及改动 UI。
示例不自动安装或启用；将构建结果复制到插件目录后重启。

```csharp
using MinamotoBoard.PluginApi;
using System.Windows.Media;

public sealed class MyPlugin : IBoardPlugin
{
    public string Id { get { return "MyCompany.MyPlugin"; } }
    public string Name { get { return "我的插件"; } }
    public int ApiVersion { get { return 1; } }
    public void Initialize(IBoardHost host)
    {
        host.AddToolbarButton("黑板", () => host.Canvas.Background = Brushes.Black);
    }
    public void Shutdown() { }
}
```

宿主提供：

| API | 用途 |
| --- | --- |
| `Canvas` | 原生 InkCanvas，可读写 Strokes、背景、工具属性 |
| `MainWindow` | 主窗口，可访问 WPF 资源、样式和事件 |
| `Toolbar` | 底部工具栏 Panel，可添加原生 WPF 控件 |
| `Overlay` | 画布上方、工具栏下方的 Grid，用于自定义面板或标记 |
| `AddToolbarButton(label, action)` | 带错误捕获的按钮；返回可 Dispose 的移除句柄 |
| `AddOverlay(element)` | 添加覆盖层；返回可 Dispose 的移除句柄 |

Initialize、Shutdown 和按钮回调都在 UI 线程执行，必须快速返回。
耗时 I/O/计算自行使用后台任务，UI 更新回到 Dispatcher；不得重写触摸点事件或在每帧扫描笔迹。
覆盖层装饰应设置 `IsHitTestVisible=false`，交互区域尽量局限于面板本身。
宿主管理添加方法产生的控件，在初始化失败或退出时清理；直接操作 Panel 或订阅原生事件的插件必须自己在 Shutdown 中还原/取消订阅。
插件不应假设默认工具栏的控件名称或内部布局稳定，优先用 API。

## 故障与权限边界

- 重复 Id、错误 API 版本、无有效入口和损坏 DLL 均显示加载失败，继续处理其他插件。
- 初始化失败会调用 Shutdown，并移除通过托管 API 新增的控件。
- 通过 `AddToolbarButton` 添加的回调抛异常时，记录失败状态并禁用该按钮。
- 不提供热卸载、进程隔离或权限沙箱；这是与宿主同权限的本机代码。
- 直接挂接的原生 WPF 事件、线程异常、无限循环和内存破坏无法保证被宿主隔离。
- 仅安装可信插件；插件可以修改笔迹、访问用户文件及网络。
- 卸载 MSI 不删除文档目录插件和用户启用配置。

原理依据：[微软 Assembly.LoadFrom](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assembly.loadfrom?view=netframework-4.8)、[AppDomain 卸载说明](https://learn.microsoft.com/en-us/dotnet/framework/app-domains/how-to-unload-an-application-domain)。
