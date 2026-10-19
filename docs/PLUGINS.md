# MinamotoBoard Plugins · API 1

**English** | [日本語](PLUGINS.ja.md) | [简体中文](PLUGINS.zh-CN.md) | [正體中文](PLUGINS.zh-TW.md)


## Install and Manage

Open **Settings → Plugins → Open Plugins Folder**. Windows resolves the location through `Environment.SpecialFolder.MyDocuments`, including redirected or OneDrive-managed Documents folders. The usual location is `%USERPROFILE%\Documents\MinamotoDev\Board\Plugins`. Each Windows user has a separate plugin folder.

Place `*.Plugin.dll` entry points in the root or one immediate subfolder:

```text
Plugins/
  BoardTheme.Plugin.dll
  OtherPlugin/
    Other.Plugin.dll
    OtherDependency.dll
```

After the board first appears, discovery runs in the background and initialization runs on the UI thread. Subfolder junctions and symbolic links are skipped. Each entry DLL must contain exactly one public, non-abstract `IBoardPlugin` implementation with a parameterless constructor. Dependencies beside the entry point use .NET Framework's LoadFrom resolution. Use the host's plugin API assembly rather than shipping another copy.

Select enabled plugins and click **Save**; changes take effect on the next launch. Close the board before adding, replacing, or removing DLLs. Assemblies cannot be individually unloaded from the current AppDomain.

Enable/disable settings use atomic replacement at `%LOCALAPPDATA%\MinamotoDev\Board\disabled-plugins.txt`. Entries match relative DLL paths, so renaming or moving a disabled plugin creates a new entry. If these settings cannot be read, discovery stops.

If a plugin blocks startup, run this command from the application folder:

```powershell
.\MinamotoBoard.exe --safe-mode
```

Safe mode discovers files and allows settings changes without loading or executing plugins.

## Development

Target **.NET Framework 4.8, x64, WPF** and reference `MinamotoBoard.PluginApi.dll`. The API assembly version is `1.0.0.0`; `ApiVersion` is `1`.

The [BoardTheme sample](../samples/BoardTheme.Plugin/) builds `BoardTheme.Plugin.dll` and demonstrates a toolbar button and an overlay. Copy the output into the plugin folder, then restart the app.

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

| API | Purpose |
| --- | --- |
| `Canvas` | Native InkCanvas: ink collection, background, and drawing attributes. |
| `MainWindow` | Main WPF window, resources, styles, and events. |
| `Toolbar` | Bottom toolbar Panel for WPF controls. |
| `Overlay` | Grid above the canvas and below the toolbar for custom panels or markers. |
| `AddToolbarButton(label, action)` | Adds a button with exception handling; returns an IDisposable removal handle. |
| `AddOverlay(element)` | Adds an overlay; returns an IDisposable removal handle. |

Initialize, Shutdown, and button callbacks run on the UI thread and must return promptly. Run expensive I/O or computation in background tasks and dispatch UI updates through the Dispatcher. Keep touch handling scoped to the plugin, and avoid scanning all ink every frame. Decorative overlays should use `IsHitTestVisible=false`.

The host cleans up controls created by its helper methods on initialization failure or shutdown. Plugins that modify Panels directly or subscribe to events must restore those changes and unsubscribe in Shutdown. Prefer the API over assumptions about toolbar control names or layout.

Page changes replace `Canvas.Strokes`. Read that property when performing an operation; subscribers should handle `Canvas.StrokesReplaced` and detach from the old collection. Custom overlays do not switch with pages or clear with ink.

## Errors and Permissions

- Duplicate IDs, incompatible API versions, invalid entry points, and damaged DLLs are reported as load failures; other plugins are processed.
- Initialization failure calls Shutdown and removes controls added through the host API.
- Exceptions from `AddToolbarButton` callbacks record a failure and disable the button.
- Plugins run in the host process with the same permissions. There is no sandbox, process isolation, or hot unloading; a stalled plugin can stall the board.
- Install trusted plugins only. Plugins can access ink, user files, and the network. Plugins must handle errors in their own WPF events and background threads.
- Uninstallation keeps plugin data by default. Selecting **Delete settings and plugins** removes it after successful uninstallation.

[Assembly.LoadFrom](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assembly.loadfrom?view=netframework-4.8) · [Unloading AppDomains](https://learn.microsoft.com/en-us/dotnet/framework/app-domains/how-to-unload-an-application-domain)
