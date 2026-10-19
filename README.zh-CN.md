# MinamotoBoard

[English](README.md) | [日本語](README.ja.md) | **简体中文** | [正體中文](README.zh-TW.md)

面向大屏触屏电脑的轻量白板程序。

[下载最新版本](https://github.com/MinamotoDev/MinamotoBoard/releases/latest)

## 运行环境

- 64 位 Windows 10 22H2 或更高版本。
- .NET Framework 4.8 或更高版本。

## 运行

- `MinamotoBoard.msi`：含插件功能的安装版。
- `MinamotoBoard-no-plugins.exe`：不含插件功能的免安装 EXE。

构建后运行 `artifacts\release-0.6.0\app\MinamotoBoard.exe`。复制此构建到另一台电脑时，请同时复制同目录的 `MinamotoBoard.exe.config` 与 `MinamotoBoard.PluginApi.dll`。

免安装版位于 `artifacts\release-0.6.0\MinamotoBoard-no-plugins.exe`，可单独复制这一份 EXE。

## 使用

- `F11`：切换全屏。`Esc`：退出全屏。`Ctrl+Tab`：显示或隐藏工具栏。
- `Tab`：移动键盘焦点。
- 支持鼠标、触屏和兼容触控笔输入。

旧显卡出现显示问题时，可为当前进程启用软件渲染：

```powershell
& .\artifacts\release-0.6.0\app\MinamotoBoard.exe --software-rendering
```

## 构建

生成两份发布文件：

```powershell
.\scripts\build-release.ps1
```

输出目录为 `artifacts\release-0.6.0`。常规本地 Release 构建位于 `src\MinamotoBoard\bin\Release\MinamotoBoard.exe`。

[安装包构建要求](docs/INSTALLER.md) · [插件开发](docs/PLUGINS.md)

## 运行时检查

检查目标电脑的 .NET Framework 版本和 x64 架构：

```powershell
.\scripts\check-runtime.ps1
```

.NET Framework 注册表中的 `Release` 值为 `528040` 或更大时，表示版本为 4.8 或更高。[微软检测方法](https://learn.microsoft.com/en-us/dotnet/framework/install/how-to-determine-which-versions-are-installed)

## 源码

[应用](src/MinamotoBoard/) · [插件 API](src/MinamotoBoard.PluginApi/) · [示例插件](samples/BoardTheme.Plugin/) · [安装程序](installer/) · [测试](tests/MinamotoBoard.Tests/)

## 许可证

Copyright (C) 2026 MinamotoDev

除[第三方声明](THIRD-PARTY-NOTICES.md)中列出的文件外，项目代码采用 **GNU General Public License v3.0 only**（`GPL-3.0-only`）。可以依据 GPL 第 3 版的条款重新分发和修改本项目。

本程序不提供任何担保，包括适销性或特定用途适用性的默示担保。完整条款见 [LICENSE](LICENSE)。
