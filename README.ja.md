# みなもとボード

[English](README.md) | **日本語** | [简体中文](README.zh-CN.md) | [正體中文](README.zh-TW.md)

大型タッチスクリーン搭載 PC 向けの軽量なホワイトボードアプリ。

[最新版をダウンロード](https://github.com/MinamotoDev/MinamotoBoard/releases/latest)

## 動作環境

- 64 ビット版 Windows 10 22H2 以降。
- .NET Framework 4.8 以降。

## 起動

- `MinamotoBoard.msi`：プラグイン対応のインストーラー。
- `MinamotoBoard-no-plugins.exe`：プラグイン機能を含まない、インストール不要の EXE。

ビルド後は `artifacts\release-0.6.0\app\MinamotoBoard.exe` を起動します。このビルドを別の PC にコピーする場合は、同じフォルダーにある `MinamotoBoard.exe.config` と `MinamotoBoard.PluginApi.dll` も一緒にコピーしてください。

ポータブル版は `artifacts\release-0.6.0\MinamotoBoard-no-plugins.exe` に出力されます。この EXE だけで実行できます。

## 使い方

- `F11`：全画面表示の切り替え。`Esc`：全画面表示を終了。`Ctrl+Tab`：ツールバーの表示・非表示。
- `Tab`：キーボードフォーカスを移動。
- マウス、タッチ操作、対応するペンで入力できます。

古いグラフィックス環境で表示に問題がある場合は、このプロセスのソフトウェアレンダリングを有効にします。

```powershell
& .\artifacts\release-0.6.0\app\MinamotoBoard.exe --software-rendering
```

## ビルド

配布用の 2 種類のファイルを生成します。

```powershell
.\scripts\build-release.ps1
```

出力先は `artifacts\release-0.6.0` です。通常のローカル Release ビルドは `src\MinamotoBoard\bin\Release\MinamotoBoard.exe` に出力されます。

[インストーラーのビルド環境](docs/INSTALLER.md) · [プラグイン開発](docs/PLUGINS.md)

## 実行環境の確認

対象 PC の .NET Framework のバージョンと x64 アーキテクチャを確認します。

```powershell
.\scripts\check-runtime.ps1
```

.NET Framework のレジストリ値 `Release` が `528040` 以上であれば、バージョン 4.8 以降です。[Microsoft のドキュメント](https://learn.microsoft.com/en-us/dotnet/framework/install/how-to-determine-which-versions-are-installed)

## ソースコード

[アプリ](src/MinamotoBoard/) · [プラグイン API](src/MinamotoBoard.PluginApi/) · [サンプルプラグイン](samples/BoardTheme.Plugin/) · [インストーラー](installer/) · [テスト](tests/MinamotoBoard.Tests/)

## ライセンス

Copyright (C) 2026 MinamotoDev

[サードパーティに関する表示](THIRD-PARTY-NOTICES.md)に記載されたファイルを除き、本プロジェクトのコードには **GNU General Public License v3.0 only**（`GPL-3.0-only`）が適用されます。GPL 第 3 版の条項に従って再配布・改変できます。

本プログラムは、商品性や特定の目的への適合性に関する黙示の保証を含め、いかなる保証もなく提供されます。全文は [LICENSE](LICENSE) を参照してください。
