# Third-party Notices

**English** | [日本語](THIRD-PARTY-NOTICES.ja.md) | [简体中文](THIRD-PARTY-NOTICES.zh-CN.md) | [正體中文](THIRD-PARTY-NOTICES.zh-TW.md)


MinamotoBoard's own code is licensed under GPL-3.0-only. The third-party materials below retain their respective licenses and copyright notices.

## WiX Standard Installer UI

- Project: WiX Toolset 5.0.2.
- Copyright (c) .NET Foundation and contributors.
- License: Microsoft Reciprocal License (MS-RL). See [full license](installer/StandardUI/LICENSE.TXT).
- [Upstream source](https://github.com/wixtoolset/wix/tree/v5.0.2/src/ext/UI/wixlib).
- `installer/StandardUI/VerifyReadyDlg.wxs` derives from the upstream `VerifyReadyDlg.wxs`. Modifications cover the dialog identifier, display conditions, repair parameters, uninstall confirmation, related-product removal events, and a settings-cleanup checkbox.
- `installer/StandardUI/ExitDialog.wxs` derives from the upstream `ExitDialog.wxs`. Modifications cover the dialog identifier and button positions, with a separate launch button.
- The standard wizard navigation in `installer/InstallerUI.wxs` is also provided under MS-RL, with installation, upgrade, and uninstall navigation and confirmation conditions adapted for this project.
- Standard dialogs, artwork, localized resources, and path validation from the official `WixToolset.UI.wixext` extension remain under their upstream licenses.
- The installer language component embeds the official WiX UI resources in four languages. These resources are provided under MS-RL.

These installer UI materials are separate files and components from the whiteboard application. The application code is licensed under GPL-3.0-only.

## About-page Images

- The GitHub icon path comes from [Primer Octicons](https://github.com/primer/octicons/blob/main/icons/mark-github-16.svg) and links to the project repository. GitHub retains the relevant brand rights; see [GitHub brand guidelines](https://brand.github.com/foundations/logo).
- Contributor photos are not covered by the GPL license for the project code.
