# MinamotoBoard Installer

**English** | [日本語](README.ja.md) | [简体中文](README.zh-CN.md) | [正體中文](README.zh-TW.md)

## Requirements

See the [latest release](https://github.com/MinamotoDev/MinamotoBoard/releases/latest) for system requirements and dependencies.

Administrator approval is required for installation. Run the app as a standard user.

Installation and uninstallation use the Windows display language. Supported languages are English, Japanese, Simplified Chinese, and Traditional Chinese, with English as the fallback.

## Usage

Open MinamotoBoard from the Start menu.

- Add pages or move between them.
- Swipe up on a tool, or right-click it, to adjust color, style, and width.
- Add drawing instruments from the toolbar. Use the pen or highlighter along their edges; use the red button to delete an instrument.
- Drag rulers and set squares to move them. Drag the blue handle to resize and rotate them.
- Drag the compass center to move it. Use its blue handle to adjust the radius, and draw arcs along its circular guide.
- Fill affects enclosed areas. Clearing a page removes its ink permanently.
- `F11`: Toggle full screen. `Esc`: Exit full screen. `Ctrl+Tab`: Show or hide the toolbar.

## Plugins and Uninstallation

Manage plugins in **Settings → Plugins**. Changes take effect after restart. Use trusted plugins only.

- Plugin folder: `MinamotoDev\Board\Plugins` under Windows Documents.
- Plugin settings: `%LOCALAPPDATA%\MinamotoDev\Board`.
- Safe mode: `MinamotoBoard.exe --safe-mode`.
- Software rendering: `MinamotoBoard.exe --software-rendering`.

Uninstall from Windows Settings or Control Panel, or run `Uninstall.exe` in the installation folder. **Delete settings and plugins** is unchecked by default. Selecting it removes the current user's `MinamotoDev\Board` folders under Documents and local application data after a successful uninstall.

[Installer build requirements](../docs/INSTALLER.md) · [Plugin development](../docs/PLUGINS.md) · [License](../LICENSE) · [Third-party notices](../THIRD-PARTY-NOTICES.md)
