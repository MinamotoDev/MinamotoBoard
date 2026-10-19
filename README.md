# MinamotoBoard

**English** | [日本語](README.ja.md) | [简体中文](README.zh-CN.md) | [正體中文](README.zh-TW.md)

A lightweight whiteboard app for large touchscreen PCs.

[Download the latest release](https://github.com/MinamotoDev/MinamotoBoard/releases/latest)

## Requirements

See the [latest release](https://github.com/MinamotoDev/MinamotoBoard/releases/latest) for system requirements and dependencies.

## Run

- `MinamotoBoard.msi`: Installer with plugin support.
- `MinamotoBoard-no-plugins.exe`: Portable EXE without plugin support.

Build outputs are under `artifacts\`. When copying the application folder to another PC, include `MinamotoBoard.exe.config` and `MinamotoBoard.PluginApi.dll` alongside `MinamotoBoard.exe`. The portable EXE can be copied on its own.

## Usage

- `F11`: Toggle full screen. `Esc`: Exit full screen. `Ctrl+Tab`: Show or hide the toolbar.
- `Tab`: Move keyboard focus.
- Supports mouse, touch, and compatible pens.

For display issues on older graphics hardware, run this command from the application folder:

```powershell
& .\MinamotoBoard.exe --software-rendering
```

## Build

Generate release files:

```powershell
.\scripts\build-release.ps1
```

Output: `artifacts\`.

[Installer build requirements](docs/INSTALLER.md) · [Plugin development](docs/PLUGINS.md)

## Runtime Check

Check the target PC's .NET Framework version and x64 architecture:

```powershell
.\scripts\check-runtime.ps1
```

## Source

[App](src/MinamotoBoard/) · [Plugin API](src/MinamotoBoard.PluginApi/) · [Sample plugin](samples/BoardTheme.Plugin/) · [Installer](installer/) · [Tests](tests/MinamotoBoard.Tests/)

## License

Copyright (C) 2026 MinamotoDev

Except for the files listed in [Third-party notices](THIRD-PARTY-NOTICES.md), the project code is licensed under **GNU General Public License v3.0 only** (`GPL-3.0-only`). You may redistribute and modify it under the terms of GPL version 3.

This program comes without any warranty, including the implied warranties of merchantability or fitness for a particular purpose. See [LICENSE](LICENSE) for the full terms.
