# MinamotoBoard

面向学校触屏电脑的轻量 WPF 绿板。当前本地版本为 **0.3.0**，新增加页、翻页、页码和清屏；保留 Plugins 和无插件独立版。

**0.3.0 尚未发布 GitHub**。[本地版本说明](docs/releases/0.3.0.md)

0.3.0 本地修复版：MSI 新增安装与卸载确认、覆盖安装选项，启动快捷方式独立管理；卸载使用 Windows 设置/控制面板入口，不额外安装卸载程序或创建卸载快捷方式。
工具栏保持单行并居中，大屏适度收紧，窄屏横向滚动；常用按钮点击区域保持至少 44 DIP。

0.2.0 合入已选定的 v5 铅笔图标，用于 EXE、窗口、开始菜单快捷方式和卸载列表。
本次按要求只构建打包，没有运行测试或执行安装。

GitHub Release **0.2.0** 提供 `MinamotoBoard.msi`（含插件支持的标准安装版）和 `MinamotoBoard-no-plugins.exe`（独立无插件版）。
无插件版编译时移除插件加载、管理代码及 API DLL 引用，不需要附加文件；仍需系统自带的 .NET Framework 4.8 或更高版本。
[发布说明](docs/releases/0.2.0.md)

新增入口：底部 **Plugins** 按钮。插件可新增工具、访问笔迹并修改 WPF 界面；[插件开发与使用说明](docs/PLUGINS.md)。
插件通常位于 `%USERPROFILE%\Documents\MinamotoDev\Board\Plugins`，使用 Windows 实际文档位置，支持文档重定向。
`--safe-mode` 启动时不执行插件。

MSI：`artifacts\installer\MinamotoBoard.msi`，默认安装到 `C:\Program Files\MinamotoDev\Board`。
安装需要管理员授权，运行白板仍使用普通用户。[安装与构建说明](docs/INSTALLER.md)

## 运行

当前构建的程序：`artifacts\release-0.3.0\app\MinamotoBoard.exe`。双击即可运行。
复制到另一台电脑时，请同时复制同目录的 `MinamotoBoard.exe.config` 与 `MinamotoBoard.PluginApi.dll`。

无插件独立版位于 `artifacts\release-0.3.0\MinamotoBoard-no-plugins.exe`，可只复制这一份 EXE。
生成两份 Release 资产可运行 `./scripts/build-release.ps1`，该脚本不运行测试。

常规本地构建的程序位于 `src\MinamotoBoard\bin\Release\MinamotoBoard.exe`。
初版试写窗口保留；新版本构建使用独立目录，避免覆盖运行中的 EXE。

- 目标：Windows 10 22H2 **x64**，.NET Framework **4.8 或更高版本**。
- 无需联网、账号、Microsoft Store、浏览器组件或管理员权限运行。
- 当前**没有保存功能**；退出或程序异常终止后所有页面的笔迹会丢失。任一页面存在笔迹时关闭会提示确认。
- 当前不是已通过学校实机验收的正式发行版。

## 使用

- 启动默认全屏，绿板 `#236B45`，白色圆头画笔，4 DIP。
- 底部选择画笔、橡皮、五种笔色；笔宽为 2 / 4 / 6 / 10 / 16 DIP。
- 橡皮为原生局部擦除，直径可选 16 / 32 / 64 DIP。
- “加页”在当前页后插入空白页；“上一页 / 下一页”切换页面，页码显示当前位置，最多 100 页。
- “清屏”先确认，再仅删除当前页笔迹，保留背景和其他页；目前不支持撤销清屏。
- 选颜色会切回画笔；切换工具保留已选颜色与宽度。
- 工具栏可以隐藏，底部的“显示工具栏”按钮可恢复。
- `F11` 切换全屏，`Esc` 退出全屏，`Ctrl+Tab` 隐藏/显示工具栏。
- 普通 `Tab` 保留给键盘焦点导航。
- 窗口缩小时超出边界的笔迹暂时不可见，放大后重新显示；不缩放或移动原有笔迹坐标。
- 鼠标、手指和兼容触控笔交由原生 `InkCanvas` 处理；没有叠加 Touch/Mouse 事件绘图逻辑。
- 未宣称支持可靠的手掌防误触；多指和设备特有行为需实机验证。

旧显卡有显示问题时可显式启动进程级软件渲染，不修改系统配置：

```powershell
& .\artifacts\release-0.3.0\app\MinamotoBoard.exe --software-rendering
```

## 技术选型

选择 **C# + .NET Framework 4.8 + WPF + 原生 InkCanvas**，无第三方运行依赖。

| 比较项 | .NET Framework 4.8 | 现代 .NET + WPF |
| --- | --- | --- |
| 目标系统部署 | Windows 10 22H2 原版包含 4.8；复制应用文件即可，仍需现场检查运行时 | Framework-dependent 需要另装匹配的 Desktop Runtime |
| 离线方案 | 使用 Windows 已有运行时 | Self-contained 可携带运行时，但应用目录更大，运行时补丁需随应用维护 |
| HDD / 磁盘占用 | 无需附带运行时，本项目实际 EXE 15,872 字节，配置 281 字节 | 携带运行时增加部署体积；本次未构建对比包 |
| 启动 / 内存 | 以已有系统组件减少部署负担；不据此声称速度更快或内存必然更低 | 可用现代运行时优化；具体差异必须用相同应用实测 |
| 编译 / 维护 | 安装 4.8 Developer Pack 后可用 Windows Framework MSBuild；API 成熟 | SDK 工具链更现代，适合后续升级，但需要维护 SDK 和运行时版本 |
| 官方支持边界 | Framework 支持跟随宿主 Windows 的生命周期，不能延长操作系统支持 | 当前微软安装页的 Windows 10 支持范围有限制；Self-contained 不改变操作系统支持状态 |

在目标设备离线、普通用户、HDD、4GB 内存的约束下，优先选择系统自带的 4.8。
本次开发机已通过 Winget 安装 **4.8 Developer Pack**，未安装大型 IDE 或现代 .NET SDK。
开发机运行的是 Framework 4.8.1，使用 **4.8 目标引用程序集**编译，不将 4.8.1 当作最低运行要求。

截至 2026-10-16，普通 Windows 10 22H2 常规支持已于 2025-10-14 结束。
需要学校 IT 核实系统版本及其 ESU/组织支持资格；本项目不能宣称普通未延长支持的系统仍获微软常规支持。

官方依据：

- [Windows 内置 Framework 版本](https://learn.microsoft.com/en-us/dotnet/framework/install/on-windows-and-server)
- [Framework 生命周期](https://learn.microsoft.com/en-us/lifecycle/faq/dotnet-framework)
- [现代 .NET 的 Windows 支持与安装](https://learn.microsoft.com/en-us/dotnet/core/install/windows)
- [现代 .NET 的部署模式](https://learn.microsoft.com/en-us/dotnet/core/deploying/)
- [InkCanvas 原生能力](https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.inkcanvas?view=netframework-4.8)

## 运行时检查

在目标电脑运行只读检查，不安装或更改系统组件：

```powershell
.\scripts\check-runtime.ps1
```

或者直接查询注册表：

```powershell
(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full').Release
```

`Release >= 528040` 表示 Framework 4.8 或更高版本。
这不是触屏测试或操作系统支持状态检查。[微软检测方法](https://learn.microsoft.com/en-us/dotnet/framework/install/how-to-determine-which-versions-are-installed)

## 构建与测试

开发工具首选 Winget 安装 4.8 Developer Pack：

```powershell
winget install --id Microsoft.DotNet.Framework.DeveloperPack_4 --version 4.8 --exact --source winget
.\build.ps1 -Test
```

已有窗口正在运行时，请勿覆盖它占用的 EXE，可指定一个新的输出目录：

```powershell
.\build.ps1 -Test -OutputDirectory artifacts\verified-next
```

脚本不自动下载依赖、不改变 PowerShell 执行策略。也可在具备 .NET Framework 4.8 开发工具的 Visual Studio 中打开 `MinamotoBoard.sln`，选择 `Release | x64`。
测试程序为无第三方测试框架的 STA 控制台，任一断言失败返回非零退出码。
包含真实 DLL 发现与加载、示例 UI 行为、禁用设置、安全模式、损坏 DLL、重复 Id、初始化清理和按钮回调错误测试。

## 结构

```text
MinamotoBoard.sln
build.ps1
src/MinamotoBoard/
  App.xaml / App.xaml.cs        启动与显式软件渲染选项
  App.config / app.manifest     运行时、DPI、普通用户权限
  UI/MainWindow.xaml           原生 WPF 工具栏与画布
  UI/MainWindow.xaml.cs        工具选择、全屏与关闭确认
  Ink/InkTools.cs              原生笔迹工具配置
  Models/BoardDocument.cs      页面、笔迹集合和页数上限
  Plugins/                     插件发现、生命周期与 UI 扩展
src/MinamotoBoard.PluginApi/    独立 API 程序集
samples/BoardTheme.Plugin/      可编译的改 UI / 新工具示例
installer/                     MSI 源码、构建及只读校验
tests/MinamotoBoard.Tests/      自动化场景测试
scripts/check-runtime.ps1      只读先决条件检查
docs/TEST-REPORT.md             当前测试证据和待验项目
```

项目从空仓库创建。页面模型已经引入；Services、Persistence 将在有实际需求时再增加。
内置功能没有后台服务、网络请求、遥测、动画循环、同步书写磁盘 I/O 或背景图片。第三方插件行为由其实现决定。

## 下一阶段

后续优先实现增量撤销/重做，覆盖书写、擦除和清屏；再考虑页面删除、独立历史及笔迹内存管理。
保存、加载、PNG 导出与恢复属于第三阶段，本次未实现。
真实触屏、多 DPI、老旧电脑和大量笔迹的验证清单见测试报告。
