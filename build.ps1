param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [switch]$Test,
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$taskMsbuild = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe'
$taskReferences = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8'
if (-not (Test-Path -LiteralPath $taskReferences)) {
    throw '.NET Framework 4.8 Developer Pack is required. See README.md.'
}
if (-not (Test-Path -LiteralPath $taskMsbuild)) { throw 'Framework MSBuild was not found.' }
$taskArguments = @((Join-Path $PSScriptRoot 'MinamotoBoard.sln'), "/p:Configuration=$Configuration", '/p:Platform=x64', '/nologo', '/verbosity:minimal')
if ($OutputDirectory) {
    $taskOutput = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot $OutputDirectory))
    $taskArguments += "/p:OutputPath=$taskOutput\"
}
& $taskMsbuild @taskArguments
if ($LASTEXITCODE -ne 0) { throw "Build failed: $LASTEXITCODE" }
if ($Test) {
    $taskTests = if ($OutputDirectory) { Join-Path $taskOutput 'MinamotoBoard.Tests.exe' } else { Join-Path $PSScriptRoot "tests\MinamotoBoard.Tests\bin\$Configuration\MinamotoBoard.Tests.exe" }
    & $taskTests
    if ($LASTEXITCODE -ne 0) { throw "Tests failed: $LASTEXITCODE" }
}
