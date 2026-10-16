# Build-time tool only; never included in the application's runtime dependencies.
$ErrorActionPreference = 'Stop'
$taskTools = Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts\tools'
New-Item -ItemType Directory -Path $taskTools -Force | Out-Null
$taskArchive = Join-Path $taskTools 'wix.5.0.2.zip'
Invoke-WebRequest 'https://api.nuget.org/v3-flatcontainer/wix/5.0.2/wix.5.0.2.nupkg' -OutFile $taskArchive
$taskExpected = 'F30EF0C74E2A986126539C5780BE93AC24E8136EAF723B1937B26272703AE173'
if ((Get-FileHash -LiteralPath $taskArchive -Algorithm SHA256).Hash -ne $taskExpected) { throw 'WiX package SHA256 mismatch.' }
Expand-Archive -LiteralPath $taskArchive -DestinationPath (Join-Path $taskTools 'wix5') -Force
$taskUIArchive = Join-Path $taskTools 'wix-ui.5.0.2.zip'
Invoke-WebRequest 'https://api.nuget.org/v3-flatcontainer/wixtoolset.ui.wixext/5.0.2/wixtoolset.ui.wixext.5.0.2.nupkg' -OutFile $taskUIArchive
$taskUIExpected = '5EF2C707614B9F70B6BBADD2D4ABCB4124EFEE215E9B16BFBC80113079A604C7'
if ((Get-FileHash -LiteralPath $taskUIArchive -Algorithm SHA256).Hash -ne $taskUIExpected) { throw 'WiX UI package SHA256 mismatch.' }
Expand-Archive -LiteralPath $taskUIArchive -DestinationPath (Join-Path $taskTools 'wix-ui5') -Force
