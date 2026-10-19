# MinamotoBoard MSI

**English** | [日本語](INSTALLER.ja.md) | [简体中文](INSTALLER.zh-CN.md) | [正體中文](INSTALLER.zh-TW.md)

## Installation and Uninstallation

The default installation folder is `C:\Program Files\MinamotoDev\Board`, resolved through `ProgramFiles64Folder`. Requirements: x64 Windows 10 22H2 or later and .NET Framework 4.8 or later. Installation requires administrator approval; normal use runs with standard user permissions.

The installer uses the official WiX Windows Installer dialogs. Installation and uninstallation detect the Windows display language, supporting English, Japanese, Simplified Chinese, and Traditional Chinese, with English as the fallback.

Opening the MSI presents a confirmation step. An older installed version leads directly to the update confirmation page; maintenance mode for the same package offers repair and removal. The completion page has **Launch** and **Finish** buttons. The Start menu contains an application shortcut.

Windows Settings and Control Panel launch `Uninstall.exe` from the installation folder. The uninstall confirmation includes **Delete settings and plugins**, unchecked by default. Selecting it removes the current user's following folders after a successful interactive uninstall:

- `MinamotoDev\Board` under Windows Documents, including plugins.
- `%LOCALAPPDATA%\MinamotoDev\Board`, including plugin enable/disable settings.

Windows known folders support redirected Documents locations. Cleanup runs as the initiating user and verifies the user SID. Upgrades, repairs, canceled uninstalls, and failed uninstalls keep settings. Silent uninstallation keeps settings by default. Links, inaccessible folders, and locked files produce cleanup errors.

The uninstaller copies itself to a temporary location and coordinates removal through system `msiexec`. Only `msiexec` needs elevation; selected settings are cleaned up in the initiating user's context. There is no Start menu uninstall shortcut.

Upgrades use a fixed UpgradeCode, support same-version replacement, and remove the old package within a rollback-capable transaction. Package language does not affect upgrade detection. The MSI and EXE files are currently unsigned.

## Build Requirements

- .NET Framework 4.8 Developer Pack and Framework MSBuild.
- WiX CLI 5.0.2 and the official WixToolset.UI.wixext 5.0.2 extension.
- A modern .NET runtime for the build tools.
- Visual Studio's x64 C++ compiler for native MSI language selection. The resulting component depends only on Windows system DLLs.

## Build

The first command downloads the pinned build tools and verifies their SHA-256 hashes. The second builds the application, portable version, and MSI. The third builds only the MSI from existing application output.

```powershell
.\installer\bootstrap-wix.ps1
.\scripts\build-release.ps1
.\installer\build-msi.ps1
```

Output files are under `artifacts\`:

- `MinamotoBoard.msi`
- `MinamotoBoard-no-plugins.exe`

The installer includes the application, uninstaller, plugin API, documentation, GPL-3.0 license, and third-party licenses. Standard dialog source is in [installer/StandardUI](../installer/StandardUI/) under MS-RL; see [Third-party notices](../THIRD-PARTY-NOTICES.md).

[WiX UI documentation](https://docs.firegiant.com/wix/tools/wixext/wixui/) · [Windows Installer custom action security](https://learn.microsoft.com/en-us/windows/win32/msi/custom-action-security)
