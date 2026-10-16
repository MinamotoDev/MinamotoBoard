# MinamotoBoard

面向大屏触屏电脑的轻量白板。

[下载最新版本](https://github.com/MinamotoDev/MinamotoBoard/releases/latest) · [版本说明](docs/releases/0.3.1.md)

## 运行

当前构建的程序：`artifacts\release-0.4.0\app\MinamotoBoard.exe`。双击即可运行。
复制到另一台电脑时，请同时复制同目录的 `MinamotoBoard.exe.config` 与 `MinamotoBoard.PluginApi.dll`。

无插件独立版位于 `artifacts\release-0.4.0\MinamotoBoard-no-plugins.exe`，可只复制这一份 EXE。
生成两份 Release 资产可运行 `./scripts/build-release.ps1`，该脚本不运行测试。

常规本地构建的程序位于 `src\MinamotoBoard\bin\Release\MinamotoBoard.exe`。

## 使用

- 启动默认全屏，绿板 `#236B45`，白色圆头画笔，4 DIP。
- 工具栏使用图标，提供画笔、橡皮、直尺、三角板、圆规和油漆桶。
- 在工具图标上向上滑动调出颜色和样式选项，粗细使用连续滑杆调整；鼠标右键也可打开选项。
- 拖动尺身移动，拖动蓝色手柄旋转，沿尺边绘制直线。三角板提供 45° 和 30° / 60° 样式。
- 圆规中心可移动，蓝色手柄调整开口与方向，沿虚线圆周拖动画弧。
- 油漆桶填充点击位置的封闭区域；开放区域不填色。
- 橡皮局部擦除墨迹，触及填色区域时移除整块填色。
- 加页、上一页和下一页管理页面，最多 100 页；悬停翻页图标可查看页码。
- “清屏”确认后删除当前页面的全部墨迹，无法撤销。
- 收起工具栏后，左下角圆形按钮显示当前工具图标，点击展开工具栏。
- `F11` 切换全屏，`Esc` 退出全屏，`Ctrl+Tab` 隐藏/显示工具栏。
- 普通 `Tab` 保留给键盘焦点导航。
- 窗口缩小时超出边界的笔迹暂时不可见，放大后重新显示；不缩放或移动原有笔迹坐标。
- 自由书写使用原生 `InkCanvas`；尺规绘制生成可擦除的原生墨迹。
- 未宣称支持可靠的手掌防误触；多指和设备特有行为需实机验证。

旧显卡有显示问题时可显式启动进程级软件渲染，不修改系统配置：

```powershell
& .\artifacts\release-0.4.0\app\MinamotoBoard.exe --software-rendering
```

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

## 许可证

Copyright (C) 2026 MinamotoDev

除下述第三方文件外，MinamotoBoard 项目代码采用 **GNU General Public License v3.0 only**（`GPL-3.0-only`）。可以依据 GNU 通用公共许可证第 3 版的条款重新分发和修改本项目。

本程序不提供任何担保，包括适销性或特定用途适用性的默示担保。完整条款见 [LICENSE](LICENSE)。
