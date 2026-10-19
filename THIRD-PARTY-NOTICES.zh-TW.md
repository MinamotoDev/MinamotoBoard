# 第三方聲明

[English](THIRD-PARTY-NOTICES.md) | [日本語](THIRD-PARTY-NOTICES.ja.md) | [简体中文](THIRD-PARTY-NOTICES.zh-CN.md) | **正體中文**


MinamotoBoard 自有程式碼採用 GPL-3.0-only。以下第三方內容適用各自的授權條款與著作權聲明。

## WiX 標準安裝介面

- 專案：WiX Toolset 5.0.2。
- Copyright (c) .NET Foundation and contributors.
- 授權條款：Microsoft Reciprocal License（MS-RL），全文請參閱 [installer/StandardUI/LICENSE.TXT](installer/StandardUI/LICENSE.TXT)。
- [上游原始碼](https://github.com/wixtoolset/wix/tree/v5.0.2/src/ext/UI/wixlib)。
- `installer/StandardUI/VerifyReadyDlg.wxs` 源自上游 `VerifyReadyDlg.wxs`；修改對話方塊識別碼、顯示條件、修復參數、解除安裝確認文字與舊版移除事件，並加入設定清理核取方塊。
- `installer/StandardUI/ExitDialog.wxs` 源自上游 `ExitDialog.wxs`；修改對話方塊識別碼與按鈕位置，並加入獨立的啟動按鈕。
- `installer/InstallerUI.wxs` 中依上游標準精靈編排的導覽程式碼同樣以 MS-RL 提供；本專案調整安裝、升級與解除安裝的導覽及確認條件。
- 官方 `WixToolset.UI.wixext` 擴充套件中的標準對話方塊、圖片、在地化資源與路徑驗證操作適用上游授權條款。
- 安裝套件語言元件內嵌 WiX 官方四種語言的標準介面資源，相關資源以 MS-RL 提供。

上述安裝介面內容與白板應用程式為獨立檔案及元件。白板應用程式碼採用 GPL-3.0-only 授權。

## 關於頁面的圖片

- GitHub 圖示的向量路徑來自 [Primer Octicons](https://github.com/primer/octicons/blob/main/icons/mark-github-16.svg)，用於連結專案儲存庫；相關品牌權利屬於 GitHub，使用方式請參閱 [GitHub 品牌指南](https://brand.github.com/foundations/logo)。
- 貢獻者照片不屬於專案程式碼的 GPL 授權範圍。
