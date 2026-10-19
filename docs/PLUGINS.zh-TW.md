# MinamotoBoard 外掛程式 · API 1

[English](PLUGINS.md) | [日本語](PLUGINS.ja.md) | [简体中文](PLUGINS.zh-CN.md) | **正體中文**


## 安裝與管理

從**設定 → 外掛程式 → 開啟外掛程式資料夾**存取。路徑由 `Environment.SpecialFolder.MyDocuments` 解析，支援文件移動與 OneDrive 重新導向。通常位於 `%USERPROFILE%\Documents\MinamotoDev\Board\Plugins`，每個 Windows 使用者各自獨立。

將 `*.Plugin.dll` 進入點放在根目錄或一層子目錄中：

```text
Plugins/
  BoardTheme.Plugin.dll
  OtherPlugin/
    Other.Plugin.dll
    OtherDependency.dll
```

白板首次顯示後於背景尋找檔案，再於 UI 執行緒初始化。略過子目錄中的交接點與符號連結。每個進入點 DLL 必須包含一個公開、非抽象、具無參數建構函式的 `IBoardPlugin` 實作。相鄰相依 DLL 由 .NET Framework LoadFrom 解析；API 組件使用主程式提供的版本，請勿隨附另一份副本。

勾選啟用項目後按**儲存**，下次啟動生效。新增、替換或刪除 DLL 前請關閉白板；目前的 AppDomain 無法單獨卸載某個組件。

啟用設定以暫存檔取代方式寫入 `%LOCALAPPDATA%\MinamotoDev\Board\disabled-plugins.txt`，依進入點 DLL 相對路徑比對。重新命名或移動已停用的 DLL 會成為新進入點；無法讀取設定時會停止搜尋。

外掛程式造成啟動停滯時，在應用程式目錄執行：

```powershell
.\MinamotoBoard.exe --safe-mode
```

安全模式可尋找檔案與管理設定，不載入或執行外掛程式碼。

## 開發

目標為 **.NET Framework 4.8、x64、WPF**，參考 `MinamotoBoard.PluginApi.dll`。API 組件版本為 `1.0.0.0`，`ApiVersion` 為 `1`。

[BoardTheme 範例](../samples/BoardTheme.Plugin/)建置後產生 `BoardTheme.Plugin.dll`，示範工具列按鈕與覆蓋層。將輸出複製至外掛程式目錄後重新啟動。

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
| `Canvas` | 原生 InkCanvas 的筆跡集合、背景與繪圖屬性。 |
| `MainWindow` | WPF 主視窗、資源、樣式與事件。 |
| `Toolbar` | 可加入 WPF 控制項的底部工具列 Panel。 |
| `Overlay` | 畫布上方、工具列下方的 Grid，用於自訂面板或標記。 |
| `AddToolbarButton(label, action)` | 加入具例外處理的按鈕，傳回 IDisposable 移除控制代碼。 |
| `AddOverlay(element)` | 加入覆蓋層，傳回 IDisposable 移除控制代碼。 |

Initialize、Shutdown 與按鈕回呼在 UI 執行緒執行，必須迅速傳回。耗時 I/O 與計算放到背景，UI 更新透過 Dispatcher 執行。觸控處理應限於外掛程式本身，避免每個影格掃描全部筆跡；裝飾性覆蓋層設定 `IsHitTestVisible=false`。

主程式會在初始化失敗與結束時清理透過輔助方法加入的控制項。直接修改 Panel 或訂閱事件的外掛程式，應在 Shutdown 中還原狀態並取消訂閱。優先使用 API，避免依賴工具列內部名稱或配置。

切換頁面會取代 `Canvas.Strokes`。操作時重新讀取該屬性；訂閱者應處理 `Canvas.StrokesReplaced` 並取消舊集合的訂閱。自訂覆蓋層不會隨頁面自動切換，也不會隨筆跡清除而刪除。

## 錯誤與權限

- 重複 Id、不相容的 API 版本、無效進入點與損壞的 DLL 會顯示載入失敗，繼續處理其他外掛程式。
- 初始化失敗時呼叫 Shutdown，並移除透過主程式 API 加入的控制項。
- `AddToolbarButton` 回呼發生例外時記錄失敗並停用按鈕。
- 外掛程式與主程式在相同處理程序、相同權限下執行，沒有沙箱、處理程序隔離或動態卸載；失控的外掛程式會影響整個白板。
- 請僅使用信任的外掛程式。外掛程式可存取筆跡、使用者檔案與網路，應自行處理直接掛接的 WPF 事件及背景執行緒中的錯誤。
- 解除安裝預設保留外掛程式資料；勾選**刪除設定與外掛程式**後，會於解除安裝成功時刪除。

[Assembly.LoadFrom](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assembly.loadfrom?view=netframework-4.8) · [AppDomain 卸載說明](https://learn.microsoft.com/en-us/dotnet/framework/app-domains/how-to-unload-an-application-domain)
