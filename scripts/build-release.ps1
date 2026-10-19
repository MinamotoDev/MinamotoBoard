# Build both official release assets. No tests or application launches are performed.
param([string]$OutputDirectory = 'artifacts\release-0.6.0')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskMsbuild = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe'
$taskProject = Join-Path $taskRoot 'src\MinamotoBoard\MinamotoBoard.csproj'
$taskRelease = if ([IO.Path]::IsPathRooted($OutputDirectory)) { [IO.Path]::GetFullPath($OutputDirectory) } else { [IO.Path]::GetFullPath((Join-Path $taskRoot $OutputDirectory)) }
$taskApp = Join-Path $taskRelease 'app'
$taskStandalone = Join-Path $taskRelease 'no-plugins'
& $taskMsbuild $taskProject /p:Configuration=Release /p:Platform=x64 "/p:OutputPath=$taskApp\" /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'Application build failed.' }
& $taskMsbuild $taskProject /p:Configuration=Release /p:Platform=x64 /p:NoPlugins=true "/p:OutputPath=$taskStandalone\" /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'No-plugins build failed.' }
& (Join-Path $taskRoot 'installer\build-msi.ps1') -AppSource $taskApp
Copy-Item -LiteralPath (Join-Path $taskRoot 'artifacts\installer\MinamotoBoard.msi') -Destination (Join-Path $taskRelease 'MinamotoBoard.msi')
Copy-Item -LiteralPath (Join-Path $taskStandalone 'MinamotoBoard-no-plugins.exe') -Destination (Join-Path $taskRelease 'MinamotoBoard-no-plugins.exe')
Write-Output "Release assets: $taskRelease"
