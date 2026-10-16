# MinamotoBoard MSI

## 安装

生成文件：`artifacts\installer\MinamotoBoard.msi`，版本 **0.3.0**，x64，单文件内嵌 CAB，无在线下载依赖。

当前 0.3.0 为本地预览，新增加页、翻页和清屏，尚未发布 GitHub。两份分发文件位于 `artifacts\release-0.3.0`。

本地修复版仍保持 0.3.0：使用 WiX 官方标准对话框库生成的 Windows Installer 原生向导，采用默认布局、图片与简体中文文案，删除自行设计的界面和额外说明。安装需点击确认；已安装时提供标准“修复”/“删除”选项，删除前仍需确认。修复会覆盖程序文件，新包覆盖旧包时使用标准更新确认页。
使用 Windows Installer 原生注册到设置和控制面板的已安装程序列表，并登记安装位置。
开始菜单只创建白板启动项，不创建卸载快捷方式；安装目录不放置独立卸载程序。

0.2.0 合入用户选定的 v5 铅笔图标：EXE、WPF 主窗口、开始菜单快捷方式和卸载列表。
本次仅完成 Release 构建和 MSI 打包，按用户要求未运行测试、只读校验脚本或安装程序。
此前开发过程的测试记录不作为此次正式发布文件的运行验证结果。

本地分发文件还提供 `MinamotoBoard-no-plugins.exe`，其插件代码和 API 引用在编译时被移除，同样包含加页和清屏。
通过 `scripts/build-release.ps1` 生成两份发布资产；本次未运行测试。

默认安装位置：

```text
C:\Program Files\MinamotoDev\Board
```

使用 Windows 的 `ProgramFiles64Folder`；若系统的 Program Files 不在 C:，会遵循该系统目录。
名称采用用户确认的 **MinamotoDev**。

双击 MSI 安装，也可在命令行显示 Windows Installer 的基本进度界面：

```powershell
msiexec.exe /i .\MinamotoBoard.msi
```

这是整机安装，写入 Program Files 和所有用户开始菜单，需要管理员授权。
安装后从开始菜单 **MinamotoBoard** 启动，EXE 为 asInvoker，普通运行不要求管理员。
插件目录由**运行白板的用户**的 Windows 文档位置解析；安装程序不预先在安装者的文档目录写入插件。

可传入绝对路径覆盖默认目录：

```powershell
msiexec.exe /i .\MinamotoBoard.msi INSTALLFOLDER="D:\Apps\MinamotoDev\Board"
```

安装包包含应用 EXE、配置、插件 API、`README.txt` 和 `PLUGINS.md` 两份说明。
**不包含测试程序或自动启用的示例插件**。
前置条件检查要求 x64、Windows build >= 19045、Framework Release >= 528040；不满足时提示并停止。
管理员可从 Windows 应用列表卸载，正常交互模式会先显示“是否卸载”确认；高级管理员显式指定静默参数时遵循 Windows Installer 的静默行为。
升级使用固定 UpgradeCode，允许同版本 0.3.0 的新包覆盖旧包，仍阻止更高版本向低版本降级。
旧版移除安排在安装事务初始化之后，以支持失败回滚。启动快捷方式改为独立组件管理的普通快捷方式，覆盖安装会重建入口。
卸载仅移除 MSI 管理的程序文件和快捷方式，不删除用户文档中的插件、启用配置或其他用户文件。
当前 MSI 未做代码签名；未在学校 Windows 10 目标设备上实装验收。

## 可复现构建

应用构建仍使用 .NET Framework 4.8 Developer Pack 与系统 Framework MSBuild。
MSI 构建额外使用 **WiX CLI 5.0.2**（构建工具，不是应用运行依赖）。
先尝试了 Winget 官方 WiX CLI 7，但其运行要求另行接受维护费用条款，没有接受该条款。
最终使用官方 NuGet 5.0.2 包，解压到仓库忽略目录；不增加现代 .NET SDK。
开发机已装现代 .NET 8/10 Runtime，`dotnet --roll-forward Major` 可运行此构建工具。
这里使用旧版工具的限制：未宣称该工具仍在维护期内。构建时使用同版本的官方 `WixToolset.UI.wixext` 扩展；运行时由 Windows Installer 显示原生控件，不安装额外 UI 运行库。直接使用官方标准对话框，不加入自定义许可协议页。
`installer/StandardUI/VerifyReadyDlg.wxs` 来自 WiX 5.0.2 官方源码，保留全部布局、控件和本地化文案；仅调整显示条件与维护事件以支持旧产品代码的覆盖/卸载。源文件保留版权声明，许可证位于同目录 `LICENSE.TXT`。
安装界面语言改为简体中文，升级检测忽略包语言，以继续识别此前语言标记为英语的版本。
当新 MSI 检测到另一个旧产品代码、用户选择“卸载”时，包内的维护桥接程序只调用 Windows Installer 移除同一 UpgradeCode 的产品；它不是独立卸载程序，不安装到程序目录，也不创建开始菜单入口。
该内部桥接程序需要管理员授权，仅在用户已确认卸载后使用。维护完成后可能留有一个临时 EXE，交由 Windows 临时文件清理处理。

```powershell
# 首次准备构建工具，需要联网；后续构建无需重复下载。
.\installer\bootstrap-wix.ps1

# 编译（不运行测试）
.\scripts\build-release.ps1

# 生成安装包
.\installer\build-msi.ps1
```

脚本对 WiX CLI 和 UI 扩展包分别校验固定 SHA256。升级工具时需人工核对官方来源并更新固定版本及哈希。
MSI 源码为 `installer/MinamotoBoard.wxs`，对话框在 `installer/InstallerUI.wxs`，内部桥接源码在 `installer/MaintenanceBridge`。没有服务、计划任务、遥测或安装后自动启动白板。
需要新版本时同步调整应用 `AssemblyInfo.cs` 和 WiX 的 `Package/@Version`，保留 UpgradeCode 与稳定组件 Guid。
只读脚本检查 MSI 数据库内容，不执行整机安装。

官方资料：[WiX 5.0.2 NuGet 包](https://www.nuget.org/packages/wix/5.0.2)、[WiX Scope](https://docs.firegiant.com/wix/schema/wxs/packagescopetype/)、[RegistrySearch](https://docs.firegiant.com/wix/schema/wxs/registrysearch/)。
