# MinamotoBoard 安裝程式

[English](README.md) | [日本語](README.ja.md) | [简体中文](README.zh-CN.md) | **正體中文**

## 系統需求

系統需求與相依性請參閱[最新版本發行說明](https://github.com/MinamotoDev/MinamotoBoard/releases/latest)。

安裝需要系統管理員授權，執行白板使用一般使用者權限。

安裝與解除安裝介面使用 Windows 顯示語言，支援英語、日語、簡體中文與繁體中文，其他語言預設使用英語。

## 使用方式

從開始功能表開啟 MinamotoBoard。

- 支援新增頁面與翻頁。
- 向上滑動工具或按滑鼠右鍵，可調整顏色、樣式與粗細。
- 點選尺規圖示新增尺規，使用畫筆或螢光筆沿邊繪製，點選紅色按鈕刪除尺規。
- 拖曳直尺或三角板本體可移動，使用藍色控制點縮放及旋轉。
- 拖曳圓規圓心可移動，使用藍色控制點調整半徑，沿圓周引導線畫弧。
- 油漆桶填滿封閉區域。清除頁面會刪除目前頁面全部筆跡，無法復原。
- `F11`：切換全螢幕。`Esc`：結束全螢幕。`Ctrl+Tab`：顯示或隱藏工具列。

## 外掛程式與解除安裝

在**設定 → 外掛程式**中管理外掛程式，重新啟動後生效。請僅使用信任的外掛程式。

- 外掛程式目錄：Windows 文件目錄下的 `MinamotoDev\Board\Plugins`。
- 外掛程式設定：`%LOCALAPPDATA%\MinamotoDev\Board`。
- 安全模式：`MinamotoBoard.exe --safe-mode`。
- 軟體轉譯：`MinamotoBoard.exe --software-rendering`。

可從 Windows 設定或控制台解除安裝，或執行安裝目錄中的 `Uninstall.exe`。**刪除設定與外掛程式**預設不勾選。勾選後，解除安裝成功時會刪除目前使用者文件目錄與本機應用程式資料目錄下的 `MinamotoDev\Board` 資料夾。

[安裝套件建置需求](../docs/INSTALLER.zh-TW.md) · [外掛程式開發](../docs/PLUGINS.zh-TW.md) · [授權條款](../LICENSE) · [第三方聲明](../THIRD-PARTY-NOTICES.zh-TW.md)
