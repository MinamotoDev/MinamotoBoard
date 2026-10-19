# サードパーティに関する表示

[English](THIRD-PARTY-NOTICES.md) | **日本語** | [简体中文](THIRD-PARTY-NOTICES.zh-CN.md) | [正體中文](THIRD-PARTY-NOTICES.zh-TW.md)


MinamotoBoard 独自のコードには GPL-3.0-only が適用されます。以下のサードパーティ製の素材には、それぞれのライセンスと著作権表示が適用されます。

## WiX 標準インストーラー UI

- プロジェクト：WiX Toolset 5.0.2。
- Copyright (c) .NET Foundation and contributors.
- ライセンス：Microsoft Reciprocal License（MS-RL）。[全文](installer/StandardUI/LICENSE.TXT)を参照してください。
- [上流のソースコード](https://github.com/wixtoolset/wix/tree/v5.0.2/src/ext/UI/wixlib)。
- `installer/StandardUI/VerifyReadyDlg.wxs` は上流の `VerifyReadyDlg.wxs` に基づき、ダイアログ識別子、表示条件、修復パラメーター、アンインストール確認、関連製品の削除イベントを変更し、設定削除のチェックボックスを追加しています。
- `installer/StandardUI/ExitDialog.wxs` は上流の `ExitDialog.wxs` に基づき、ダイアログ識別子とボタン位置を変更し、独立した起動ボタンを追加しています。
- `installer/InstallerUI.wxs` の標準ウィザードの画面遷移にも MS-RL が適用されます。本プロジェクト向けにインストール、更新、アンインストールの画面遷移と確認条件を調整しています。
- 公式の `WixToolset.UI.wixext` 拡張機能に含まれる標準ダイアログ、画像、翻訳リソース、パス検証処理には、上流のライセンスが適用されます。
- インストーラーの言語コンポーネントに組み込まれた WiX 公式の 4 言語の UI リソースは、MS-RL に基づいて提供されます。

これらのインストーラー UI の素材は、ホワイトボードアプリとは別のファイル・コンポーネントです。アプリのコードには GPL-3.0-only が適用されます。

## バージョン情報ページの画像

- GitHub アイコンのパスは [Primer Octicons](https://github.com/primer/octicons/blob/main/icons/mark-github-16.svg) に由来し、プロジェクトリポジトリへのリンクに使用しています。関連するブランドの権利は GitHub に帰属します。[ブランドガイドライン](https://brand.github.com/foundations/logo)を参照してください。
- 貢献者の写真は、プロジェクトコードの GPL ライセンスの対象外です。
