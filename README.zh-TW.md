# MinamotoBoard

[English](README.md) | [日本語](README.ja.md) | [简体中文](README.zh-CN.md) | **正體中文**

適用於大型觸控螢幕電腦的輕量白板應用程式。

[下載最新版本](https://github.com/MinamotoDev/MinamotoBoard/releases/latest)

## 系統需求

系統需求與相依性請參閱[最新版本發行說明](https://github.com/MinamotoDev/MinamotoBoard/releases/latest)。

## 執行

- `MinamotoBoard.msi`：支援外掛程式的安裝版。
- `MinamotoBoard-no-plugins.exe`：不含外掛程式功能的免安裝 EXE。

建置產物位於 `artifacts\` 下。將應用程式目錄複製到另一台電腦時，請將 `MinamotoBoard.exe.config` 與 `MinamotoBoard.PluginApi.dll` 和 `MinamotoBoard.exe` 放在一起。免安裝版只需複製 EXE。

## 使用方式

- `F11`：切換全螢幕。`Esc`：結束全螢幕。`Ctrl+Tab`：顯示或隱藏工具列。
- `Tab`：移動鍵盤焦點。
- 支援滑鼠、觸控與相容的觸控筆輸入。

舊型顯示卡出現顯示問題時，可在應用程式目錄執行：

```powershell
& .\MinamotoBoard.exe --software-rendering
```

## 建置

產生發行檔案：

```powershell
.\scripts\build-release.ps1
```

輸出目錄：`artifacts\`。

[安裝套件建置需求](docs/INSTALLER.zh-TW.md) · [外掛程式開發](docs/PLUGINS.zh-TW.md)

## 執行階段檢查

檢查目標電腦的 .NET Framework 版本與 x64 架構：

```powershell
.\scripts\check-runtime.ps1
```

## 原始碼

[應用程式](src/MinamotoBoard/) · [外掛程式 API](src/MinamotoBoard.PluginApi/) · [範例外掛程式](samples/BoardTheme.Plugin/) · [安裝程式](installer/) · [測試](tests/MinamotoBoard.Tests/)

## 授權條款

Copyright (C) 2026 MinamotoDev

除[第三方聲明](THIRD-PARTY-NOTICES.zh-TW.md)所列檔案外，專案程式碼採用 **GNU General Public License v3.0 only**（`GPL-3.0-only`）。可依 GPL 第 3 版的條款重新散布與修改本專案。

本程式不提供任何擔保，包括適售性或特定用途適用性的默示擔保。完整條款請參閱 [LICENSE](LICENSE)。
