# MinamotoBoard MSI

[English](INSTALLER.md) | [日本語](INSTALLER.ja.md) | [简体中文](INSTALLER.zh-CN.md) | **正體中文**

## 安裝與解除安裝

預設安裝目錄為 `C:\Program Files\MinamotoDev\Board`，依系統 `ProgramFiles64Folder` 決定。系統需求為 Windows 10 22H2 x64 或更新版本、.NET Framework 4.8 或更新版本。安裝需要系統管理員授權，執行白板使用一般使用者權限。

安裝套件使用 WiX 官方標準 Windows Installer 對話方塊。安裝與解除安裝會自動辨識 Windows 顯示語言，支援英語、日語、簡體中文與繁體中文，其他語言預設使用英語。

開啟 MSI 後會先確認安裝。偵測到舊版時直接顯示更新確認頁；同一套件的維護模式提供修復與解除安裝。完成頁提供**啟動**與**完成**按鈕。開始功能表提供白板捷徑。

Windows 設定與控制台會啟動安裝目錄中的 `Uninstall.exe`。解除安裝確認頁的**刪除設定與外掛程式**預設不勾選。勾選後，互動式解除安裝成功時會清理目前使用者的以下目錄：

- Windows 文件目錄下的 `MinamotoDev\Board`，包含外掛程式。
- `%LOCALAPPDATA%\MinamotoDev\Board`，包含外掛程式啟用設定。

文件目錄透過 Windows 已知資料夾解析，支援重新導向。清理以發起解除安裝的使用者身分執行，並核對 SID。升級、修復、取消或失敗時會保留設定；無訊息解除安裝預設保留設定。遇到連結、無法存取的目錄或使用中的檔案時，會顯示清理錯誤。

解除安裝程式會複製到暫存目錄，透過系統 `msiexec` 移除應用程式。只有 `msiexec` 需要提高權限；選取的設定會以原使用者身分清理。開始功能表不建立解除安裝捷徑。

升級使用固定的 UpgradeCode，支援相同版本覆寫。舊套件移除在可復原的交易中執行，套件語言不影響舊版辨識。安裝套件與 EXE 目前未簽署。

## 建置需求

- .NET Framework 4.8 Developer Pack 與 Framework MSBuild。
- WiX CLI 5.0.2 與官方 WixToolset.UI.wixext 5.0.2。
- 可執行建置工具的新版 .NET 執行階段。
- Visual Studio x64 C++ 編譯器，用於 MSI 語言選擇元件；產生的元件僅依賴 Windows 系統 DLL。

## 建置

第一行下載固定版本工具並驗證 SHA-256；第二行建置應用程式、免安裝版與 MSI；第三行從既有應用程式輸出單獨建置 MSI。

```powershell
.\installer\bootstrap-wix.ps1
.\scripts\build-release.ps1
.\installer\build-msi.ps1
```

發行檔案位於 `artifacts\` 下：

- `MinamotoBoard.msi`
- `MinamotoBoard-no-plugins.exe`

安裝套件包含應用程式、解除安裝程式、外掛程式 API、說明文件、GPL-3.0 與第三方授權條款。標準對話方塊原始碼位於 [installer/StandardUI](../installer/StandardUI/)，以 MS-RL 提供；詳見[第三方聲明](../THIRD-PARTY-NOTICES.zh-TW.md)。

[WiX UI 文件](https://docs.firegiant.com/wix/tools/wixext/wixui/) · [Windows Installer 自訂動作安全性](https://learn.microsoft.com/en-us/windows/win32/msi/custom-action-security)
