param(
    [string]$AppSource = 'artifacts\release-0.5.0\app',
    [string]$WixDll = 'artifacts\tools\wix5\tools\net6.0\any\wix.dll',
    [string]$WixUI = 'artifacts\tools\wix-ui5\wixext5\WixToolset.UI.wixext.dll'
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
function Resolve-TaskPath([string]$taskPath) {
    if ([IO.Path]::IsPathRooted($taskPath)) { return [IO.Path]::GetFullPath($taskPath) }
    return [IO.Path]::GetFullPath((Join-Path $taskRoot $taskPath))
}
$taskApp = Resolve-TaskPath $AppSource
$taskWix = Resolve-TaskPath $WixDll
$taskUI = Resolve-TaskPath $WixUI
if (-not (Test-Path -LiteralPath $taskUI)) { throw 'WiX UI 5.0.2 extension is required. Run installer/bootstrap-wix.ps1.' }
if (-not (Test-Path -LiteralPath $taskWix)) { throw 'WiX 5.0.2 CLI is required. See docs/INSTALLER.md.' }
foreach ($taskFile in @('MinamotoBoard.exe','MinamotoBoard.exe.config','MinamotoBoard.PluginApi.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $taskApp $taskFile))) { throw "Missing application file: $taskFile" }
}
$taskOutput = Join-Path $taskRoot 'artifacts\installer'
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
$taskMaintenance = Join-Path $taskOutput 'maintenance'
$taskMsbuild = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe'
& $taskMsbuild (Join-Path $PSScriptRoot 'MaintenanceBridge\MaintenanceBridge.csproj') /p:Configuration=Release "/p:OutputPath=$taskMaintenance\" /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'MSI maintenance bridge build failed.' }
& dotnet --roll-forward Major $taskWix build (Join-Path $PSScriptRoot 'MinamotoBoard.wxs') (Join-Path $PSScriptRoot 'InstallerUI.wxs') (Join-Path $PSScriptRoot 'StandardUI\VerifyReadyDlg.wxs') -ext $taskUI -culture zh-CN -arch x64 -d "AppSource=$taskApp" -d "DocSource=$taskRoot" -d "MaintenanceSource=$taskMaintenance" -intermediateFolder (Join-Path $taskOutput 'obj') -o (Join-Path $taskOutput 'MinamotoBoard.msi')
if ($LASTEXITCODE -ne 0) { throw "MSI build failed: $LASTEXITCODE" }
Get-Item -LiteralPath (Join-Path $taskOutput 'MinamotoBoard.msi') | Select-Object FullName, Length
