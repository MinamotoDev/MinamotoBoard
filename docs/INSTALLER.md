# MinamotoBoard MSI

## 安装

生成文件：`artifacts\installer\MinamotoBoard.msi`，版本 **0.2.1**，x64，单文件内嵌 CAB，无在线下载依赖。

0.2.1 合入用户选定的 v5 铅笔图标：EXE、WPF 主窗口、开始菜单快捷方式和卸载列表。
本次仅完成 Release 构建和 MSI 打包，按用户要求未运行测试、只读校验脚本或安装程序。
此前 0.2.0 的测试记录不作为 0.2.1 的运行验证结果。

默认安装位置：

```text
C:\Program Files\MinamotoDev\Board
```

使用 Windows 的 `ProgramFiles64Folder`；若系统的 Program Files 不在 C:，会遵循该系统目录。
名称采用用户确认的 **MinamotoDev**。

双击 MSI 安装，也可在命令行显示 Windows Installer 的基本进度界面：

```powershell
msiexec.exe /i .\MinamotoBoard.msi /qb
```

这是整机安装，写入 Program Files 和所有用户开始菜单，需要管理员授权。
安装后从开始菜单 **MinamotoBoard** 启动，EXE 为 asInvoker，普通运行不要求管理员。
插件目录由**运行白板的用户**的 Windows 文档位置解析；安装程序不预先在安装者的文档目录写入插件。

可传入绝对路径覆盖默认目录：

```powershell
msiexec.exe /i .\MinamotoBoard.msi /qb INSTALLFOLDER="D:\Apps\MinamotoDev\Board"
```

安装包包含应用 EXE、配置、插件 API、`README.txt` 和 `PLUGINS.md` 两份说明。
**不包含测试程序或自动启用的示例插件**。
前置条件检查要求 x64、Windows build >= 19045、Framework Release >= 528040；不满足时提示并停止。
管理员可从 Windows 应用列表卸载；升级使用固定 UpgradeCode 的 MajorUpgrade，阻止降级。
卸载仅移除 MSI 管理的程序文件和快捷方式，不删除用户文档中的插件、启用配置或其他用户文件。
当前 MSI 未做代码签名；未在学校 Windows 10 目标设备上实装验收。

## 可复现构建

应用构建仍使用 .NET Framework 4.8 Developer Pack 与系统 Framework MSBuild。
MSI 构建额外使用 **WiX CLI 5.0.2**（构建工具，不是应用运行依赖）。
先尝试了 Winget 官方 WiX CLI 7，但其运行要求另行接受维护费用条款，没有接受该条款。
最终使用官方 NuGet 5.0.2 包，解压到仓库忽略目录；不增加现代 .NET SDK。
开发机已装现代 .NET 8/10 Runtime，`dotnet --roll-forward Major` 可运行此构建工具。
这里使用旧版工具的限制：未宣称该工具仍在维护期内；MSI 不引入第三方/原生自定义操作。

```powershell
# 首次准备构建工具，需要联网；后续构建无需重复下载。
.\installer\bootstrap-wix.ps1

# 编译（不运行测试）
.\build.ps1 -OutputDirectory artifacts\release-0.2.1

# 生成安装包
.\installer\build-msi.ps1
```

脚本对 WiX 包校验固定 SHA256。升级工具时需人工核对官方来源并更新固定版本及哈希。
MSI 源码为 `installer/MinamotoBoard.wxs`，没有服务、计划任务、遥测或安装后自动启动。
需要新版本时同步调整应用 `AssemblyInfo.cs` 和 WiX 的 `Package/@Version`，保留 UpgradeCode 与稳定组件 Guid。
只读脚本检查 MSI 数据库内容，不执行整机安装。

官方资料：[WiX 5.0.2 NuGet 包](https://www.nuget.org/packages/wix/5.0.2)、[WiX Scope](https://docs.firegiant.com/wix/schema/wxs/packagescopetype/)、[RegistrySearch](https://docs.firegiant.com/wix/schema/wxs/registrysearch/)。
