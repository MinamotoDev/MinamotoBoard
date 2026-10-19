# MinamotoBoard MSI

当前版本：**0.6.0**。

## 安装与卸载

默认安装目录为 `C:\Program Files\MinamotoDev\Board`，遵循系统 `ProgramFiles64Folder`。
目标环境为 Windows 10 22H2 x64 或更高版本、.NET Framework 4.8 或更高版本。
安装需要管理员权限，运行白板使用普通用户权限。

安装包使用 WiX 官方标准 Windows Installer 对话框。安装与卸载界面自动识别 Windows 显示语言，支持English、日本語、简体中文和正體中文，其他语言默认使用英语。
双击 MSI 后确认安装；检测到旧版本时直接显示更新确认页，同一安装包的维护模式提供修复和卸载。
完成页提供“启动”和“完成”按钮。开始菜单提供白板入口。
Windows 设置和控制面板的卸载入口启动安装目录中的 `Uninstall.exe`。

卸载确认页提供“删除配置和插件”复选框，默认关闭。
勾选后，交互式卸载成功时清理当前用户的：

- Windows 文档目录下的 `MinamotoDev\Board`，包含 Plugins。
- `%LOCALAPPDATA%\MinamotoDev\Board`，包含插件启用设置。

文档目录通过 Windows 已知文件夹解析，支持文档重定向。
清理在发起卸载的用户上下文中执行，并核对用户 SID；不枚举其他用户。
配置清理不会在升级、修复、取消或失败的卸载中执行。静默卸载默认不删除配置。
遇到链接、目录访问失败或文件占用时，清理会显示错误。

新 MSI 对不同产品代码的旧版本执行卸载时，内部维护桥接程序调用系统 msiexec。
只有 msiexec 需要提升权限；维护协调进程负责在原用户上下文中清理选中的配置。
卸载程序确认后复制到临时目录运行，通过 msiexec 移除应用；开始菜单不创建卸载入口。

升级使用固定 UpgradeCode，支持同版本覆盖；旧包移除位于可回滚事务内。
包语言不影响旧版本识别。安装包和 EXE 当前未签名。

## 构建

应用使用 .NET Framework 4.8 Developer Pack 和系统 Framework MSBuild。
安装包使用固定版本 WiX CLI 5.0.2 与官方 WixToolset.UI.wixext 5.0.2。
开发机需要可运行构建工具的现代 .NET Runtime。
多语言 MSI 构建还需要 Visual Studio x64 C++ 编译器；生成的语言选择组件仅依赖 Windows 系统 DLL。

```powershell
# 首次下载构建工具，校验固定 SHA256
.\installer\bootstrap-wix.ps1

# 构建应用、无插件版与 MSI
.\scripts\build-release.ps1

# 单独构建 MSI
.\installer\build-msi.ps1
```

分发文件位于 `artifacts\release-0.6.0`：

- `MinamotoBoard.msi`
- `MinamotoBoard-no-plugins.exe`

安装包包含应用、卸载程序、插件 API、使用说明、GPL-3.0 和第三方许可证。
WiX 标准确认页源码位于 `installer/StandardUI`，按 MS-RL 提供；修改范围见 `THIRD-PARTY-NOTICES.md`。

[WiX UI 文档](https://docs.firegiant.com/wix/tools/wixext/wixui/) · [Windows Installer 自定义操作安全](https://learn.microsoft.com/en-us/windows/win32/msi/custom-action-security)
