<#
.SYNOPSIS
    Генерация иконок кнопок ленты для команд PanelKomplekt.

.DESCRIPTION
    Рисует иконки 16x16 и 32x32 (PNG, прозрачный фон) и кладёт их в папки команд:
      PanelKomplekt\Commands\C100_About\C100_About_16.png / _32.png
      PanelKomplekt\Commands\C101_ShowElementId\C101_ShowElementId_16.png / _32.png
    Каждый размер рисуется отдельно (без масштабирования), чтобы линии оставались чёткими.
    Цвета задаются в начале скрипта. С параметром -PreviewPath дополнительно создаётся
    картинка предпросмотра на светлом и тёмном фоне ленты AutoCAD.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\Generate-Icons.ps1 -PreviewPath C:\temp\icons-preview.png
#>
param([string]$PreviewPath)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$Root     = Split-Path $PSScriptRoot -Parent
$Commands = Join-Path $Root 'PanelKomplekt\Commands'

# Палитра: цвета различимы и на светлой, и на тёмной ленте AutoCAD.
$Blue   = [Drawing.Color]::FromArgb(255, 47, 128, 237)   # #2F80ED — основной цвет плагина
$Orange = [Drawing.Color]::FromArgb(255, 242, 153, 74)   # #F2994A — акцент (надпись id)

# Создание пустого прозрачного холста с включённым сглаживанием.
function New-Canvas([int]$Size) {
    $bmp = New-Object Drawing.Bitmap($Size, $Size, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.Clear([Drawing.Color]::Transparent)
    return @{ Bitmap = $bmp; Graphics = $g }
}

# Перо с круглыми концами.
function New-Pen([Drawing.Color]$Color, [single]$Width) {
    $pen = New-Object Drawing.Pen($Color, $Width)
    $pen.StartCap = [Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [Drawing.Drawing2D.LineCap]::Round
    return $pen
}

# C100 «О плагине»: буква i в круге.
function Draw-About([int]$Size) {
    $c = New-Canvas $Size; $g = $c.Graphics
    $brush = New-Object Drawing.SolidBrush($Blue)
    if ($Size -eq 32) {
        $g.DrawEllipse((New-Pen $Blue 2.5), 2.25, 2.25, 27.5, 27.5)
        $g.FillEllipse($brush, 14.0, 7.0, 4.0, 4.0)                     # точка
        $g.DrawLine((New-Pen $Blue 3.5), 16.0, 14.5, 16.0, 24.0)        # ножка
    }
    else {
        $g.DrawEllipse((New-Pen $Blue 1.5), 1.25, 1.25, 13.5, 13.5)
        $g.FillEllipse($brush, 7.0, 3.5, 2.0, 2.0)
        $g.DrawLine((New-Pen $Blue 2.0), 8.0, 7.5, 8.0, 12.0)
    }
    $g.Dispose(); return $c.Bitmap
}

# C101 «ID элемента»: лупа, за которой надпись id.
function Draw-ShowElementId([int]$Size) {
    $c = New-Canvas $Size; $g = $c.Graphics
    $textBrush = New-Object Drawing.SolidBrush($Orange)
    # Надпись центрируется внутри линзы: «id» видна сквозь стекло лупы.
    $format = New-Object Drawing.StringFormat
    $format.Alignment = [Drawing.StringAlignment]::Center
    $format.LineAlignment = [Drawing.StringAlignment]::Center
    # Текст рисуется как векторный контур: обычный DrawString на прозрачном фоне даёт тёмный ореол.
    $family = New-Object Drawing.FontFamily('Segoe UI')
    $bold = [int][Drawing.FontStyle]::Bold
    $path = New-Object Drawing.Drawing2D.GraphicsPath
    if ($Size -eq 32) {
        $path.AddString('id', $family, $bold, 11.0, (New-Object Drawing.RectangleF(2.0, 1.5, 22.0, 22.0)), $format)
        $g.FillPath($textBrush, $path)
        $g.DrawEllipse((New-Pen $Blue 2.5), 2.25, 2.25, 21.5, 21.5)      # линза
        $g.DrawLine((New-Pen $Blue 4.0), 21.5, 21.5, 29.0, 29.0)         # ручка
    }
    else {
        $path.AddString('id', $family, $bold, 7.0, (New-Object Drawing.RectangleF(0.5, 0.0, 12.0, 12.0)), $format)
        $g.FillPath($textBrush, $path)
        $g.DrawEllipse((New-Pen $Blue 1.3), 0.9, 0.9, 11.2, 11.2)
        $g.DrawLine((New-Pen $Blue 2.2), 11.0, 11.0, 14.6, 14.6)
    }
    $g.Dispose(); return $c.Bitmap
}

# C102 «Проверка панелей»: вопросительный знак.
function Draw-PanelCheck([int]$Size) {
    $c = New-Canvas $Size; $g = $c.Graphics
    $brush = New-Object Drawing.SolidBrush($Blue)
    $format = New-Object Drawing.StringFormat
    $format.Alignment = [Drawing.StringAlignment]::Center
    $format.LineAlignment = [Drawing.StringAlignment]::Center
    # Знак рисуется векторным контуром (без тёмного ореола на прозрачном фоне), как надпись id в C101.
    $family = New-Object Drawing.FontFamily('Segoe UI')
    $path = New-Object Drawing.Drawing2D.GraphicsPath
    if ($Size -eq 32) {
        $path.AddString('?', $family, [int][Drawing.FontStyle]::Bold, 32.0, (New-Object Drawing.RectangleF(0.0, 1.0, 32.0, 32.0)), $format)
    }
    else {
        $path.AddString('?', $family, [int][Drawing.FontStyle]::Bold, 17.0, (New-Object Drawing.RectangleF(0.0, 0.5, 16.0, 16.0)), $format)
    }
    $g.FillPath($brush, $path)
    $g.Dispose(); return $c.Bitmap
}

$icons = [ordered]@{
    'C100_About'         = ${function:Draw-About}
    'C101_ShowElementId' = ${function:Draw-ShowElementId}
    'C102_PanelCheck'    = ${function:Draw-PanelCheck}
}

$generated = @()
foreach ($name in $icons.Keys) {
    $dir = Join-Path $Commands $name
    if (-not (Test-Path $dir)) { throw "Не найдена папка команды: $dir" }
    foreach ($size in 16, 32) {
        $bmp = & $icons[$name] $size
        $path = Join-Path $dir "${name}_$size.png"
        $bmp.Save($path, [Drawing.Imaging.ImageFormat]::Png)
        $generated += @{ Name = $name; Size = $size; Bitmap = $bmp }
        Write-Host "Создана иконка: $path"
    }
}

# Предпросмотр: светлая и тёмная лента, размер 1:1 и увеличение x6.
if ($PreviewPath) {
    $zoom = 6; $pad = 16
    $colW = 32 * $zoom + $pad
    $width = $pad + ($icons.Count * 2) * $colW
    $height = 2 * (32 * $zoom + 32 + 2 * $pad)
    $preview = New-Object Drawing.Bitmap($width, $height)
    $pg = [Drawing.Graphics]::FromImage($preview)
    $pg.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $pg.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
    $themes = @(@{ Y = 0; Color = [Drawing.Color]::FromArgb(245, 245, 245) }, @{ Y = $height / 2; Color = [Drawing.Color]::FromArgb(59, 68, 83) })
    foreach ($theme in $themes) {
        $pg.FillRectangle((New-Object Drawing.SolidBrush($theme.Color)), 0, $theme.Y, $width, $height / 2)
        $x = $pad
        foreach ($icon in $generated) {
            $y = $theme.Y + $pad
            $pg.DrawImage($icon.Bitmap, $x, $y, $icon.Size, $icon.Size)                                   # 1:1
            $pg.DrawImage($icon.Bitmap, $x, $y + 32 + 8, $icon.Size * $zoom, $icon.Size * $zoom)          # увеличение
            $x += $colW
        }
    }
    $pg.Dispose()
    $preview.Save($PreviewPath, [Drawing.Imaging.ImageFormat]::Png)
    Write-Host "Предпросмотр: $PreviewPath"
}
