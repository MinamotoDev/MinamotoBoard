param(
    [string]$AppSource = 'artifacts\release-0.2.1',
    [string]$WixDll = 'artifacts\tools\wix5\tools\net6.0\any\wix.dll'
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
function Resolve-TaskPath([string]$taskPath) {
    if ([IO.Path]::IsPathRooted($taskPath)) { return [IO.Path]::GetFullPath($taskPath) }
    return [IO.Path]::GetFullPath((Join-Path $taskRoot $taskPath))
}
$taskApp = Resolve-TaskPath $AppSource
$taskWix = Resolve-TaskPath $WixDll
if (-not (Test-Path -LiteralPath $taskWix)) { throw 'WiX 5.0.2 CLI is required. See docs/INSTALLER.md.' }
foreach ($taskFile in @('MinamotoBoard.exe','MinamotoBoard.exe.config','MinamotoBoard.PluginApi.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $taskApp $taskFile))) { throw "Missing application file: $taskFile" }
}
$taskOutput = Join-Path $taskRoot 'artifacts\installer'
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
& dotnet --roll-forward Major $taskWix build (Join-Path $PSScriptRoot 'MinamotoBoard.wxs') -arch x64 -d "AppSource=$taskApp" -d "DocSource=$taskRoot" -intermediateFolder (Join-Path $taskOutput 'obj') -o (Join-Path $taskOutput 'MinamotoBoard.msi')
if ($LASTEXITCODE -ne 0) { throw "MSI build failed: $LASTEXITCODE" }
Get-Item -LiteralPath (Join-Path $taskOutput 'MinamotoBoard.msi') | Select-Object FullName, Length
