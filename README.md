# MinamotoBoard

面向大屏触屏电脑的轻量白板。

[下载最新版本](https://github.com/MinamotoDev/MinamotoBoard/releases/latest)

## 运行

当前构建的程序：`artifacts\release-0.3.1\app\MinamotoBoard.exe`。双击即可运行。
复制到另一台电脑时，请同时复制同目录的 `MinamotoBoard.exe.config` 与 `MinamotoBoard.PluginApi.dll`。

无插件独立版位于 `artifacts\release-0.3.1\MinamotoBoard-no-plugins.exe`，可只复制这一份 EXE。
生成两份 Release 资产可运行 `./scripts/build-release.ps1`，该脚本不运行测试。

常规本地构建的程序位于 `src\MinamotoBoard\bin\Release\MinamotoBoard.exe`。

## 使用

- `F11` 切换全屏，`Esc` 退出全屏，`Ctrl+Tab` 隐藏/显示工具栏。
- 普通 `Tab` 保留给键盘焦点导航。
- 鼠标、手指和兼容触控笔交由原生 `InkCanvas` 处理。

旧显卡有显示问题时可显式启动进程级软件渲染，不修改系统配置：

```powershell
& .\artifacts\release-0.3.1\app\MinamotoBoard.exe --software-rendering
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
