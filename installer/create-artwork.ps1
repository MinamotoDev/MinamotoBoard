param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$taskIconPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\MinamotoBoard\Assets\MinamotoBoard.ico'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
foreach ($taskAsset in @(@('BoardBanner.bmp',493,58,437,5,48), @('BoardDialog.bmp',493,312,32,32,96))) {
    $taskBitmap = New-Object Drawing.Bitmap([int]$taskAsset[1], [int]$taskAsset[2])
    $taskGraphics = [Drawing.Graphics]::FromImage($taskBitmap)
    $taskIcon = New-Object Drawing.Icon($taskIconPath, [int]$taskAsset[5], [int]$taskAsset[5])
    try {
        $taskGraphics.Clear([Drawing.Color]::White)
        $taskGraphics.DrawIcon($taskIcon, (New-Object Drawing.Rectangle([int]$taskAsset[3], [int]$taskAsset[4], [int]$taskAsset[5], [int]$taskAsset[5])))
        $taskBitmap.Save((Join-Path $OutputDirectory $taskAsset[0]), [Drawing.Imaging.ImageFormat]::Bmp)
    } finally { $taskIcon.Dispose(); $taskGraphics.Dispose(); $taskBitmap.Dispose() }
}
