# みなもとボード MSI

[English](INSTALLER.md) | **日本語** | [简体中文](INSTALLER.zh-CN.md) | [正體中文](INSTALLER.zh-TW.md)

## インストールとアンインストール

既定のインストール先は `C:\Program Files\MinamotoDev\Board` で、`ProgramFiles64Folder` に従います。64 ビット版 Windows 10 22H2 以降と .NET Framework 4.8 以降が必要です。インストールには管理者の許可が必要ですが、通常の使用時は標準ユーザー権限で実行します。

公式の WiX Windows Installer ダイアログを使用します。インストールとアンインストールは Windows の表示言語に合わせ、英語、日本語、簡体字中国語、繁体字中国語に対応します。その他の言語では英語を使用します。

MSI を開くと確認画面が表示されます。旧バージョンがある場合は更新確認画面へ進み、同じパッケージのメンテナンスモードでは修復・削除を選べます。完了画面には**起動**と**完了**ボタンがあります。スタートメニューにはアプリのショートカットを作成します。

Windows の設定やコントロールパネルからは、インストール先の `Uninstall.exe` を起動します。確認画面の**設定とプラグインを削除**は既定でオフです。選択すると、対話形式のアンインストール成功後に現在のユーザーの以下のフォルダーを削除します。

- Windows のドキュメント内の `MinamotoDev\Board`。プラグインを含みます。
- `%LOCALAPPDATA%\MinamotoDev\Board`。プラグインの有効・無効設定を含みます。

ドキュメントは Windows の既知のフォルダーから取得し、リダイレクトに対応します。削除は操作を開始したユーザーの権限で実行し、SID を確認します。更新・修復・キャンセル・失敗の場合は設定を残します。サイレントアンインストールも既定で設定を残します。リンク、アクセスできないフォルダー、使用中のファイルがある場合は削除エラーを表示します。

アンインストーラーは自身を一時フォルダーへコピーし、システムの `msiexec` で削除を進めます。昇格が必要なのは `msiexec` のみで、設定は操作を開始したユーザーの権限で削除します。スタートメニューにアンインストール用ショートカットは作成しません。

固定の UpgradeCode を使用し、同じバージョンの上書きにも対応します。旧パッケージの削除はロールバック可能なトランザクション内で行います。パッケージの言語は旧版の検出に影響しません。MSI と EXE は現在未署名です。

## ビルド環境

- .NET Framework 4.8 Developer Pack と Framework MSBuild。
- WiX CLI 5.0.2 と公式 WixToolset.UI.wixext 5.0.2。
- ビルドツールを実行できる新しい .NET ランタイム。
- MSI の言語選択用に Visual Studio の x64 C++ コンパイラー。生成されるコンポーネントは Windows のシステム DLL のみに依存します。

## ビルド

1 行目は固定バージョンのツールをダウンロードし、SHA-256 を検証します。2 行目はアプリ、ポータブル版、MSI を生成します。3 行目は既存のアプリ出力から MSI のみを生成します。

```powershell
.\installer\bootstrap-wix.ps1
.\scripts\build-release.ps1
.\installer\build-msi.ps1
```

配布用ファイルは `artifacts\` 内に出力されます。

- `MinamotoBoard.msi`
- `MinamotoBoard-no-plugins.exe`

アプリ、アンインストーラー、プラグイン API、ドキュメント、GPL-3.0 とサードパーティのライセンスを含みます。標準ダイアログのソースは [installer/StandardUI](../installer/StandardUI/) にあり、MS-RL が適用されます。[サードパーティに関する表示](../THIRD-PARTY-NOTICES.ja.md)を参照してください。

[WiX UI ドキュメント](https://docs.firegiant.com/wix/tools/wixext/wixui/) · [Windows Installer のカスタムアクションのセキュリティ](https://learn.microsoft.com/en-us/windows/win32/msi/custom-action-security)
