$ErrorActionPreference = 'Stop'
$taskRelease = (Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction SilentlyContinue).Release
if ($taskRelease -ge 528040) {
    Write-Output ".NET Framework 4.8 or later detected. Release=$taskRelease"
} else {
    throw '.NET Framework 4.8 or later is required. Ask IT to verify the Windows installation.'
}
if (-not [Environment]::Is64BitOperatingSystem) { throw 'MinamotoBoard requires x64 Windows.' }
Write-Output 'x64 OS detected. This checks prerequisites only, not Windows support status or touch compatibility.'
