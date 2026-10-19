# 第三方声明

[English](THIRD-PARTY-NOTICES.md) | [日本語](THIRD-PARTY-NOTICES.ja.md) | **简体中文** | [正體中文](THIRD-PARTY-NOTICES.zh-TW.md)


MinamotoBoard 自有代码采用 GPL-3.0-only。以下第三方内容保留原许可证及版权声明。

## WiX 标准安装界面

- 项目：WiX Toolset 5.0.2。
- 版权所有：Copyright (c) .NET Foundation and contributors.
- 许可证：Microsoft Reciprocal License（MS-RL），全文见 [installer/StandardUI/LICENSE.TXT](installer/StandardUI/LICENSE.TXT)。
- [上游源码](https://github.com/wixtoolset/wix/tree/v5.0.2/src/ext/UI/wixlib)。
- `installer/StandardUI/VerifyReadyDlg.wxs` 源自上游 `VerifyReadyDlg.wxs`；修改了对话框标识、显示条件、修复参数、卸载确认文本及旧产品卸载事件，增加了配置清理复选框。
- `installer/StandardUI/ExitDialog.wxs` 源自上游 `ExitDialog.wxs`；修改了对话框标识和按钮位置，增加了独立启动按钮。
- `installer/InstallerUI.wxs` 中依据上游标准向导编排的导航代码同样按 MS-RL 提供；本项目调整了安装、升级和卸载的导航与确认条件。
- 官方 `WixToolset.UI.wixext` 扩展中的标准对话框、图片、本地化资源与路径验证操作适用上游许可证。
- 安装包语言组件内嵌 WiX 官方四种语言的标准界面资源，相关资源按 MS-RL 提供。

上述安装界面内容与白板应用为独立文件和组件。白板应用代码采用 GPL-3.0-only 授权。

## 关于页图片

- GitHub 图标的矢量路径来自 [Primer Octicons](https://github.com/primer/octicons/blob/main/icons/mark-github-16.svg)，用于链接项目仓库；相关品牌权利属于 GitHub，使用说明见 [GitHub 品牌指南](https://brand.github.com/foundations/logo)。
- 贡献者照片不属于项目代码的 GPL 授权范围。
