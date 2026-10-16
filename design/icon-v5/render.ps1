# Generate the proposed vector icon as transparent PNGs and a multi-size ICO.
# This only creates design assets; it does not build or change the application.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$taskSizes = @(16,24,32,48,64,128,256)
$taskImages = @()
foreach ($taskSize in $taskSizes) {
    $taskBitmap = New-Object Drawing.Bitmap(($taskSize * 4), ($taskSize * 4))
    $taskGraphics = [Drawing.Graphics]::FromImage($taskBitmap)
    $taskGraphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $taskGraphics.Clear([Drawing.Color]::Transparent)
    $taskGraphics.ScaleTransform(($taskSize / 16.0), ($taskSize / 16.0))
    $taskShape = New-Object Drawing.Drawing2D.GraphicsPath
    $taskRadius = 13.0
    $taskDiameter = 2 * $taskRadius
    $taskShape.AddArc(2,2,$taskDiameter,$taskDiameter,180,90)
    $taskShape.AddArc((62-$taskDiameter),2,$taskDiameter,$taskDiameter,270,90)
    $taskShape.AddArc((62-$taskDiameter),(62-$taskDiameter),$taskDiameter,$taskDiameter,0,90)
    $taskShape.AddArc(2,(62-$taskDiameter),$taskDiameter,$taskDiameter,90,90)
    $taskShape.CloseFigure()
    $taskFill = New-Object Drawing.SolidBrush([Drawing.ColorTranslator]::FromHtml('#236B45'))
    $taskGraphics.FillPath($taskFill,$taskShape)
    $taskState = $taskGraphics.Save()
    $taskGraphics.TranslateTransform(32,32)
    $taskGraphics.RotateTransform(45)
    $taskGraphics.TranslateTransform(-32,-32)
    $taskSilhouette = New-Object Drawing.Drawing2D.GraphicsPath
    $taskSilhouette.AddArc(28,10,4,4,180,90)
    $taskSilhouette.AddArc(32,10,4,4,270,90)
    $taskSilhouette.AddLine(36,12,36,44)
    $taskSilhouette.AddLine(36,44,32,55)
    $taskSilhouette.AddLine(32,55,28,44)
    $taskSilhouette.CloseFigure()
    $taskShadow = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(40,0,0,0))
    $taskGraphics.TranslateTransform(1.2,1.2)
    $taskGraphics.FillPath($taskShadow,$taskSilhouette)
    $taskGraphics.TranslateTransform(-1.2,-1.2)
    $taskEraser = New-Object Drawing.Drawing2D.GraphicsPath
    $taskEraser.AddArc(28,10,4,4,180,90)
    $taskEraser.AddArc(32,10,4,4,270,90)
    $taskEraser.AddLine(36,12,36,20)
    $taskEraser.AddLine(36,20,28,20)
    $taskEraser.CloseFigure()
    $taskPink = New-Object Drawing.Drawing2D.LinearGradientBrush((New-Object Drawing.PointF(28,10)),(New-Object Drawing.PointF(36,10)),[Drawing.ColorTranslator]::FromHtml('#F8B4BC'),[Drawing.ColorTranslator]::FromHtml('#D97F8E'))
    $taskYellow = New-Object Drawing.SolidBrush([Drawing.ColorTranslator]::FromHtml('#E4BA58'))
    $taskLight = New-Object Drawing.SolidBrush([Drawing.ColorTranslator]::FromHtml('#EAD08C'))
    $taskDark = New-Object Drawing.SolidBrush([Drawing.ColorTranslator]::FromHtml('#CDA03F'))
    $taskSilver = New-Object Drawing.Drawing2D.LinearGradientBrush((New-Object Drawing.PointF(28,17)),(New-Object Drawing.PointF(36,17)),[Drawing.ColorTranslator]::FromHtml('#FFFFFF'),[Drawing.ColorTranslator]::FromHtml('#AEBAB6'))
    $taskWood = New-Object Drawing.SolidBrush([Drawing.ColorTranslator]::FromHtml('#F2D3A4'))
    $taskGraphite = New-Object Drawing.SolidBrush([Drawing.ColorTranslator]::FromHtml('#24332B'))
    $taskGraphics.FillPath($taskPink,$taskEraser)
    $taskGraphics.FillRectangle($taskYellow,28,20,8,24)
    $taskGraphics.FillRectangle($taskLight,28,20,2,24)
    $taskGraphics.FillRectangle($taskDark,34,20,2,24)
    $taskGraphics.FillRectangle($taskSilver,28,17,8,4)
    $taskGraphics.FillPolygon($taskWood,[Drawing.PointF[]]@(
        (New-Object Drawing.PointF(28,44)), (New-Object Drawing.PointF(36,44)),
        (New-Object Drawing.PointF(32,55))))
    $taskWoodShade = New-Object Drawing.SolidBrush([Drawing.ColorTranslator]::FromHtml('#C9A477'))
    $taskGraphics.FillPolygon($taskWoodShade,[Drawing.PointF[]]@(
        (New-Object Drawing.PointF(34,44)), (New-Object Drawing.PointF(36,44)),
        (New-Object Drawing.PointF(32,55))))
    $taskGraphics.FillPolygon($taskGraphite,[Drawing.PointF[]]@(
        (New-Object Drawing.PointF(30.55,51)), (New-Object Drawing.PointF(33.45,51)),
        (New-Object Drawing.PointF(32,55))))
    $taskGraphics.Restore($taskState)
    $taskEraser.Dispose(); $taskPink.Dispose(); $taskYellow.Dispose()
    $taskSilver.Dispose(); $taskWood.Dispose(); $taskGraphite.Dispose()
    $taskSilhouette.Dispose(); $taskShadow.Dispose(); $taskLight.Dispose(); $taskDark.Dispose(); $taskWoodShade.Dispose()
    $taskSmall = New-Object Drawing.Bitmap($taskSize,$taskSize)
    $taskResize = [Drawing.Graphics]::FromImage($taskSmall)
    $taskResize.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
    $taskResize.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $taskResize.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $taskResize.DrawImage($taskBitmap,0,0,$taskSize,$taskSize)
    $taskStream = New-Object IO.MemoryStream
    $taskSmall.Save($taskStream,[Drawing.Imaging.ImageFormat]::Png)
    $taskImages += ,($taskStream.ToArray())
    if ($taskSize -eq 256) { $taskSmall.Save((Join-Path $PSScriptRoot 'MinamotoBoard-preview.png'),[Drawing.Imaging.ImageFormat]::Png) }
    $taskStream.Dispose(); $taskResize.Dispose(); $taskSmall.Dispose()
    $taskFill.Dispose(); $taskShape.Dispose(); $taskGraphics.Dispose(); $taskBitmap.Dispose()
}
$taskFile = [IO.File]::Create((Join-Path $PSScriptRoot 'MinamotoBoard.ico'))
$taskWriter = New-Object IO.BinaryWriter($taskFile)
try {
    $taskWriter.Write([uint16]0); $taskWriter.Write([uint16]1); $taskWriter.Write([uint16]$taskSizes.Length)
    $taskOffset = 6 + 16 * $taskSizes.Length
    for ($taskIndex=0; $taskIndex -lt $taskSizes.Length; $taskIndex++) {
        $taskDimension = if ($taskSizes[$taskIndex] -eq 256) { 0 } else { $taskSizes[$taskIndex] }
        $taskWriter.Write([byte]$taskDimension); $taskWriter.Write([byte]$taskDimension)
        $taskWriter.Write([byte]0); $taskWriter.Write([byte]0)
        $taskWriter.Write([uint16]1); $taskWriter.Write([uint16]32)
        $taskWriter.Write([uint32]$taskImages[$taskIndex].Length); $taskWriter.Write([uint32]$taskOffset)
        $taskOffset += $taskImages[$taskIndex].Length
    }
    foreach ($taskImage in $taskImages) { $taskWriter.Write([byte[]]$taskImage) }
} finally { $taskWriter.Dispose(); $taskFile.Dispose() }
