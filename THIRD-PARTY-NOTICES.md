# 第三方声明

MinamotoBoard 自有代码采用 GPL-3.0-only。以下第三方内容保留原许可证及版权声明。

## WiX 标准安装界面

- 项目：WiX Toolset 5.0.2。
- 版权所有：Copyright (c) .NET Foundation and contributors.
- 许可证：Microsoft Reciprocal License（MS-RL），全文见 [installer/StandardUI/LICENSE.TXT](installer/StandardUI/LICENSE.TXT)。
- 上游源码：https://github.com/wixtoolset/wix/tree/v5.0.2/src/ext/UI/wixlib
- `installer/StandardUI/VerifyReadyDlg.wxs` 源自上游 `VerifyReadyDlg.wxs`；修改了对话框标识、显示条件、修复参数、卸载确认文本及旧产品卸载事件，增加了配置清理复选框。
- `installer/InstallerUI.wxs` 中依据上游标准向导编排的导航代码同样按 MS-RL 提供；本项目调整了安装、升级和卸载的导航与确认条件。
- 构建使用官方 `WixToolset.UI.wixext` 扩展中的标准对话框、图片、本地化资源与路径验证操作；这些内容保留上游许可，不改授 GPL。

上述安装界面内容与白板应用为独立文件和组件；此处的例外不会改变白板应用代码的 GPL-3.0-only 授权。

## 关于页图片

- GitHub 图标来自 https://github.githubassets.com/images/modules/logos_page/GitHub-Mark.png ，用于链接项目仓库；相关品牌权利属于 GitHub，使用说明见 https://brand.github.com/foundations/logo 。
- 维护人员头像由项目维护方提供，分别用于“丹羽　空”和 MinamotoDev 的身份展示，不属于项目代码的 GPL 授权范围。
