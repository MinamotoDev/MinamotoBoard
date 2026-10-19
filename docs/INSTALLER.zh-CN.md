# MinamotoBoard MSI

[English](INSTALLER.md) | [日本語](INSTALLER.ja.md) | **简体中文** | [正體中文](INSTALLER.zh-TW.md)

## 安装与卸载

默认安装目录为 `C:\Program Files\MinamotoDev\Board`，遵循系统 `ProgramFiles64Folder`。运行环境为 Windows 10 22H2 x64 或更高版本、.NET Framework 4.8 或更高版本。安装需要管理员授权，运行白板使用普通用户权限。

安装包使用 WiX 官方标准 Windows Installer 对话框。安装与卸载自动识别 Windows 显示语言，支持英语、日语、简体中文和繁体中文，其他语言默认使用英语。

打开 MSI 后确认安装。检测到旧版本时直接显示更新确认页；同一安装包的维护模式提供修复和卸载。完成页提供**启动**和**完成**按钮。开始菜单提供白板入口。

Windows 设置和控制面板会启动安装目录中的 `Uninstall.exe`。卸载确认页的**删除配置和插件**默认不勾选。勾选后，交互式卸载成功时清理当前用户的以下目录：

- Windows 文档目录下的 `MinamotoDev\Board`，包含插件。
- `%LOCALAPPDATA%\MinamotoDev\Board`，包含插件启用设置。

文档目录通过 Windows 已知文件夹解析，支持重定向。清理以发起卸载的用户身份执行，并核对 SID。升级、修复、取消或失败的卸载保留配置；静默卸载默认保留配置。遇到链接、目录访问失败或文件占用时，清理会显示错误。

卸载程序复制到临时目录，通过系统 `msiexec` 移除应用。只有 `msiexec` 需要提升权限；选中的配置以原用户身份清理。开始菜单不创建卸载入口。

升级使用固定 UpgradeCode，支持同版本覆盖。旧包移除位于可回滚事务内，包语言不影响旧版本识别。安装包和 EXE 当前未签名。

## 构建要求

- .NET Framework 4.8 Developer Pack 和 Framework MSBuild。
- WiX CLI 5.0.2 与官方 WixToolset.UI.wixext 5.0.2。
- 可运行构建工具的现代 .NET 运行时。
- Visual Studio x64 C++ 编译器，用于 MSI 语言选择组件；生成的组件仅依赖 Windows 系统 DLL。

## 构建

第一条命令下载固定版本工具并校验 SHA-256；第二条构建应用、免安装版与 MSI；第三条从已有应用输出单独构建 MSI。

```powershell
.\installer\bootstrap-wix.ps1
.\scripts\build-release.ps1
.\installer\build-msi.ps1
```

发布文件位于 `artifacts\` 下：

- `MinamotoBoard.msi`
- `MinamotoBoard-no-plugins.exe`

安装包包含应用、卸载程序、插件 API、说明文件、GPL-3.0 和第三方许可证。标准对话框源码位于 [installer/StandardUI](../installer/StandardUI/)，按 MS-RL 提供；详见[第三方声明](../THIRD-PARTY-NOTICES.zh-CN.md)。

[WiX UI 文档](https://docs.firegiant.com/wix/tools/wixext/wixui/) · [Windows Installer 自定义操作安全](https://learn.microsoft.com/en-us/windows/win32/msi/custom-action-security)
