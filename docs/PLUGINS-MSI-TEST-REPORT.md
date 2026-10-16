# Plugins / MSI 0.2.0 验证记录

日期：2026-10-16（Asia/Taipei）。环境与此前基础白板测试相同，Windows build 26300，x64；不是目标学校设备。

## 构建与自动化

命令：`./build.ps1 -Test -OutputDirectory artifacts/plugins-release`。
Release 构建成功，无编译警告/错误，**11 / 11 自动化场景通过**。

原有 6 项笔迹与窗口测试全部保留；新增 5 个包含真实反射加载的集成场景：

1. 从独立临时插件目录发现并加载示例 DLL，新增按钮与覆盖层；执行按钮更改背景；退出时清理 UI 并恢复背景。
2. 保存禁用设置后不执行插件；原子替换为启用设置后，在安全模式仍不执行插件。
3. 损坏的 DLL 与先添加 UI 再抛异常的插件均显示失败，正常插件继续加载；失败插件即使 Shutdown 也抛异常，托管 UI 仍清理。
4. 根目录与一级子目录中存在重复 Id，仅允许一个插件初始化。
5. 托管按钮回调抛异常后不会向 UI 线程传播，错误已回报且该按钮禁用。

临时测试插件 DLL 在 Framework 中会锁定到进程退出，测试不递归删除运行中程序集目录。
没有将示例或故障夹具复制到用户实际 Plugins 目录，没有改动原有试写窗口。

## MSI

- 使用项目本地官方 NuGet WiX 5.0.2 包；SHA256 校验固定。
- `installer/build-msi.ps1` 生成 `artifacts/installer/MinamotoBoard.msi`。
- `installer/verify-msi.ps1` 成功：名称、MinamotoDev 拼写、0.2.0、整机安装、x64 摘要、ProgramFiles64Folder/MinamotoDev/Board 目录树、恰好 5 个有效载荷文件、开始菜单快捷方式、无自定义操作。
- 运行时条件边界：`#528039` 拒绝，`#528040` 与 `#534296` 接受，空值拒绝。
- Windows build 条件边界：19044 拒绝，19045 与 26300 接受，空值拒绝。
- `msiexec /a ... /qn TARGETDIR=...` 将 CAB 解包到项目 artifacts 中，退出码 0。
- 提取的 EXE、配置、API 与构建输出逐一进行 SHA256 比较（见本次验证输出）。

行政解包不等于实际整机安装。未执行本机 Program Files 安装、升级、维修或卸载；未验证学校电脑 UAC 策略、离线启动、真实触屏或老旧硬件。
MSI 未签名。构建工具 WiX 5 为旧版本，本包未使用其自定义操作。

尝试 Winget WiX 7 时遇到需要额外接受维护费用条款，未接受。尝试卸载该构建工具返回 1603；此项与应用 MSI 构建无关，不影响已生成的离线安装包。
最终使用仓库内工具包进行构建，没有新增现代 .NET SDK。
