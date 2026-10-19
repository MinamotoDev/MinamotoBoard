# MinamotoBoard 安装程序

[English](README.md) | [日本語](README.ja.md) | **简体中文** | [正體中文](README.zh-TW.md)

## 运行环境

运行环境和依赖要求见[最新版本发布说明](https://github.com/MinamotoDev/MinamotoBoard/releases/latest)。

安装需要管理员授权，运行白板使用普通用户权限。

安装与卸载界面使用 Windows 显示语言，支持英语、日语、简体中文和繁体中文，其他语言默认使用英语。

## 使用

从开始菜单打开 MinamotoBoard。

- 支持加页和翻页。
- 上滑工具或鼠标右键可调整颜色、样式与粗细。
- 点击尺规图标新增尺规，使用画笔或荧光笔沿边绘制，点击红色按钮删除尺规。
- 拖动直尺或三角板的尺身移动，使用蓝色手柄缩放和旋转。
- 拖动圆规圆心移动，使用蓝色手柄调整半径，沿圆周引导线画弧。
- 油漆桶填充封闭区域。清屏会删除当前页全部墨迹，无法撤销。
- `F11`：切换全屏。`Esc`：退出全屏。`Ctrl+Tab`：显示或隐藏工具栏。

## 插件与卸载

在**设置 → 插件**中管理插件，重启后生效。仅使用可信插件。

- 插件目录：Windows 文档目录下的 `MinamotoDev\Board\Plugins`。
- 插件设置：`%LOCALAPPDATA%\MinamotoDev\Board`。
- 安全模式：`MinamotoBoard.exe --safe-mode`。
- 软件渲染：`MinamotoBoard.exe --software-rendering`。

在 Windows 设置或控制面板中卸载，或运行安装目录中的 `Uninstall.exe`。**删除配置和插件**默认不勾选。勾选后，卸载成功时删除当前用户文档目录和本地应用数据目录下的 `MinamotoDev\Board` 文件夹。

[安装包构建要求](../docs/INSTALLER.zh-CN.md) · [插件开发](../docs/PLUGINS.zh-CN.md) · [许可证](../LICENSE) · [第三方声明](../THIRD-PARTY-NOTICES.zh-CN.md)
