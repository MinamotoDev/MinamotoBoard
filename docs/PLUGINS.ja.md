# みなもとボードのプラグイン · API 1

[English](PLUGINS.md) | **日本語** | [简体中文](PLUGINS.zh-CN.md) | [正體中文](PLUGINS.zh-TW.md)


## インストールと管理

**設定 → プラグイン → プラグインフォルダーを開く**から開きます。`Environment.SpecialFolder.MyDocuments` を使用するため、ドキュメントの移動や OneDrive のリダイレクトにも対応します。通常の場所は `%USERPROFILE%\Documents\MinamotoDev\Board\Plugins` です。Windows ユーザーごとに独立しています。

`*.Plugin.dll` のエントリーポイントをルートまたは直下のサブフォルダーに配置します。

```text
Plugins/
  BoardTheme.Plugin.dll
  OtherPlugin/
    Other.Plugin.dll
    OtherDependency.dll
```

ホワイトボードの初回表示後にバックグラウンドでファイルを検出し、UI スレッドで初期化します。サブフォルダーのジャンクションやシンボリックリンクは除外します。各 DLL には、引数なしコンストラクターを持つ公開・非抽象の `IBoardPlugin` 実装が 1 つ必要です。隣接する依存 DLL は .NET Framework の LoadFrom で解決します。API アセンブリはホストのものを使用し、別のコピーを同梱しないでください。

有効にするプラグインを選んで**保存**すると、次回起動時に反映されます。DLL の追加・更新・削除前にはアプリを終了してください。現在の AppDomain から個々のアセンブリをアンロードすることはできません。

有効・無効の設定は `%LOCALAPPDATA%\MinamotoDev\Board\disabled-plugins.txt` にアトミック置換で保存します。DLL の相対パスで照合するため、無効なプラグインの移動・改名は新しい項目として扱います。設定を読み取れない場合は検出を停止します。

プラグインが起動を妨げる場合は、アプリのフォルダーから次のコマンドを実行します。

```powershell
.\MinamotoBoard.exe --safe-mode
```

セーフモードではファイルの検出と設定変更のみを行い、プラグインを読み込んだり実行したりしません。

## 開発

**.NET Framework 4.8、x64、WPF** を対象にし、`MinamotoBoard.PluginApi.dll` を参照します。API アセンブリのバージョンは `1.0.0.0`、`ApiVersion` は `1` です。

[BoardTheme サンプル](../samples/BoardTheme.Plugin/)は `BoardTheme.Plugin.dll` を生成し、ツールバーボタンとオーバーレイの追加方法を示します。出力をプラグインフォルダーにコピーして再起動します。

```csharp
using MinamotoBoard.PluginApi;
using System.Windows.Media;

public sealed class MyPlugin : IBoardPlugin
{
    public string Id { get { return "MyCompany.MyPlugin"; } }
    public string Name { get { return "My Plugin"; } }
    public int ApiVersion { get { return 1; } }
    public void Initialize(IBoardHost host)
    {
        host.AddToolbarButton("Blackboard", () => host.Canvas.Background = Brushes.Black);
    }
    public void Shutdown() { }
}
```

| API | 用途 |
| --- | --- |
| `Canvas` | ネイティブ InkCanvas の筆跡コレクション、背景、描画属性。 |
| `MainWindow` | WPF メインウィンドウ、リソース、スタイル、イベント。 |
| `Toolbar` | WPF コントロールを追加できる下部ツールバーの Panel。 |
| `Overlay` | キャンバスより上、ツールバーより下の Grid。独自パネルやマーカー用。 |
| `AddToolbarButton(label, action)` | 例外処理付きのボタンを追加し、削除用の IDisposable を返す。 |
| `AddOverlay(element)` | オーバーレイを追加し、削除用の IDisposable を返す。 |

Initialize、Shutdown、ボタンのコールバックは UI スレッドで実行されるため、速やかに処理を返してください。重い I/O・計算はバックグラウンドで行い、UI 更新は Dispatcher に戻します。タッチ処理はプラグイン内に限定し、毎フレーム全筆跡を走査しないでください。装飾用オーバーレイには `IsHitTestVisible=false` を設定します。

ホストのヘルパーで追加したコントロールは初期化失敗時と終了時に削除されます。Panel の直接変更やイベント購読は、プラグインの Shutdown で復元・解除してください。ツールバー内部の名前や配置への依存より API を優先します。

ページを切り替えると `Canvas.Strokes` が置き換わります。操作時にこのプロパティを読み直し、購読する場合は `Canvas.StrokesReplaced` を処理して旧コレクションの購読を解除します。独自オーバーレイはページ切り替えや筆跡の消去の対象外です。

## エラーと権限

- ID の重複、非対応の API バージョン、無効なエントリーポイント、破損 DLL は読み込み失敗として表示し、他のプラグインの処理を続けます。
- 初期化に失敗した場合は Shutdown を呼び、ホスト API で追加されたコントロールを削除します。
- `AddToolbarButton` のコールバックで例外が発生すると、失敗を記録してそのボタンを無効にします。
- ホストと同じプロセス・権限で動作します。サンドボックス、プロセス分離、実行中のアンロードはなく、プラグインの停止がアプリ全体に影響します。
- 信頼できるプラグインのみ使用してください。筆跡、ユーザーファイル、ネットワークにアクセスできます。独自の WPF イベントやスレッドのエラーはプラグインで処理してください。
- アンインストール時は既定でデータを残します。**設定とプラグインを削除**を選ぶと、成功後に削除します。

[Assembly.LoadFrom](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assembly.loadfrom?view=netframework-4.8) · [AppDomain のアンロード](https://learn.microsoft.com/en-us/dotnet/framework/app-domains/how-to-unload-an-application-domain)
