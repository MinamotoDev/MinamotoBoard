# MinamotoBoard

**English** | [日本語](README.ja.md) | [简体中文](README.zh-CN.md) | [正體中文](README.zh-TW.md)

A lightweight whiteboard app for large touchscreen PCs.

[Download the latest release](https://github.com/MinamotoDev/MinamotoBoard/releases/latest)

## Requirements

- 64-bit Windows 10 22H2 or later.
- .NET Framework 4.8 or later.

## Run

- `MinamotoBoard.msi`: Installer with plugin support.
- `MinamotoBoard-no-plugins.exe`: Portable EXE without plugin support.

After building, run `artifacts\release-0.6.0\app\MinamotoBoard.exe`. When copying this build to another PC, include `MinamotoBoard.exe.config` and `MinamotoBoard.PluginApi.dll` from the same folder.

The portable version is at `artifacts\release-0.6.0\MinamotoBoard-no-plugins.exe` and can be copied on its own.

## Usage

- `F11`: Toggle full screen. `Esc`: Exit full screen. `Ctrl+Tab`: Show or hide the toolbar.
- `Tab`: Move keyboard focus.
- Supports mouse, touch, and compatible pens.

For display issues on older graphics hardware, enable software rendering for this process:

```powershell
& .\artifacts\release-0.6.0\app\MinamotoBoard.exe --software-rendering
```

## Build

Generate both release assets:

```powershell
.\scripts\build-release.ps1
```

Output: `artifacts\release-0.6.0`. A regular local Release build is at `src\MinamotoBoard\bin\Release\MinamotoBoard.exe`.

[Installer build requirements](docs/INSTALLER.md) · [Plugin development](docs/PLUGINS.md)

## Runtime Check

Check the target PC's .NET Framework version and x64 architecture:

```powershell
.\scripts\check-runtime.ps1
```

A .NET Framework registry `Release` value of `528040` or higher indicates version 4.8 or later. [Microsoft documentation](https://learn.microsoft.com/en-us/dotnet/framework/install/how-to-determine-which-versions-are-installed)

## Source

[App](src/MinamotoBoard/) · [Plugin API](src/MinamotoBoard.PluginApi/) · [Sample plugin](samples/BoardTheme.Plugin/) · [Installer](installer/) · [Tests](tests/MinamotoBoard.Tests/)

## License

Copyright (C) 2026 MinamotoDev

Except for the files listed in [Third-party notices](THIRD-PARTY-NOTICES.md), the project code is licensed under **GNU General Public License v3.0 only** (`GPL-3.0-only`). You may redistribute and modify it under the terms of GPL version 3.

This program comes without any warranty, including the implied warranties of merchantability or fitness for a particular purpose. See [LICENSE](LICENSE) for the full terms.
