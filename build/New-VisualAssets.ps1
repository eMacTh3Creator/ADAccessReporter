#requires -version 5.1
[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$assetDir = Join-Path $repoRoot 'assets'
New-Item -ItemType Directory -Force -Path $assetDir | Out-Null

Add-Type -AssemblyName System.Drawing

function New-LogoBitmap {
    param([int]$Size)

    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::FromArgb(17, 24, 39))

    $scale = $Size / 512.0
    $blue = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(37, 99, 235))
    $green = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(20, 184, 166))
    $amber = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(245, 158, 11))
    $white = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(248, 250, 252))
    $muted = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(148, 163, 184), [float](10 * $scale))

    try {
        $graphics.FillRectangle($blue, [float](92 * $scale), [float](128 * $scale), [float](328 * $scale), [float](232 * $scale))
        $graphics.FillRectangle($green, [float](124 * $scale), [float](160 * $scale), [float](264 * $scale), [float](168 * $scale))

        foreach ($x in 164, 256, 348) {
            $graphics.FillEllipse($white, [float](($x - 28) * $scale), [float](190 * $scale), [float](56 * $scale), [float](56 * $scale))
            $graphics.FillRectangle($white, [float](($x - 36) * $scale), [float](254 * $scale), [float](72 * $scale), [float](50 * $scale))
        }

        $graphics.DrawLine($muted, [float](164 * $scale), [float](246 * $scale), [float](256 * $scale), [float](276 * $scale))
        $graphics.DrawLine($muted, [float](348 * $scale), [float](246 * $scale), [float](256 * $scale), [float](276 * $scale))
        $graphics.FillEllipse($amber, [float](356 * $scale), [float](94 * $scale), [float](76 * $scale), [float](76 * $scale))
        $graphics.FillRectangle($amber, [float](388 * $scale), [float](158 * $scale), [float](12 * $scale), [float](86 * $scale))
    }
    finally {
        $blue.Dispose()
        $green.Dispose()
        $amber.Dispose()
        $white.Dispose()
        $muted.Dispose()
        $graphics.Dispose()
    }

    return $bitmap
}

function Save-PngIconAsIco {
    param(
        [string]$PngPath,
        [string]$IcoPath
    )

    $pngBytes = [System.IO.File]::ReadAllBytes($PngPath)
    $stream = [System.IO.File]::Open($IcoPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
    $writer = [System.IO.BinaryWriter]::new($stream)
    try {
        $writer.Write([UInt16]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]1)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$pngBytes.Length)
        $writer.Write([UInt32]22)
        $writer.Write($pngBytes)
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }
}

$logoPath = Join-Path $assetDir 'ad-access-reporter.png'
$iconPngPath = Join-Path $assetDir 'ad-access-reporter-icon.png'
$icoPath = Join-Path $assetDir 'ad-access-reporter.ico'

$logo = New-LogoBitmap -Size 512
try {
    $logo.Save($logoPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $logo.Save($iconPngPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $logo.Dispose()
}

Save-PngIconAsIco -PngPath $iconPngPath -IcoPath $icoPath

$previewPath = Join-Path $assetDir 'app-preview.png'
$preview = [System.Drawing.Bitmap]::new(1400, 860)
$g = [System.Drawing.Graphics]::FromImage($preview)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$font = [System.Drawing.Font]::new('Segoe UI', 24, [System.Drawing.FontStyle]::Bold)
$smallFont = [System.Drawing.Font]::new('Segoe UI', 15)
$tinyFont = [System.Drawing.Font]::new('Segoe UI', 12)
$monoFont = [System.Drawing.Font]::new('Consolas', 12)
$dark = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(15, 23, 42))
$ink = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(30, 41, 59))
$mutedText = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(100, 116, 139))
$panel = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
$bg = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(226, 232, 240))
$blueBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(37, 99, 235))
$greenBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(20, 184, 166))
$amberBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(245, 158, 11))
$linePen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(203, 213, 225), 2)

try {
    $g.FillRectangle($bg, 0, 0, 1400, 860)
    $g.FillRectangle($dark, 0, 0, 1400, 88)
    $g.DrawString('AD Access Reporter', $font, [System.Drawing.Brushes]::White, 44, 24)
    $g.DrawString('AD Groups', $smallFont, [System.Drawing.Brushes]::White, 46, 118)
    $g.DrawString('Folder Rights', $smallFont, $mutedText, 186, 118)
    $g.FillRectangle($panel, 44, 158, 1312, 628)
    $g.DrawRectangle($linePen, 44, 158, 1312, 628)

    $g.DrawString('Group names', $tinyFont, $ink, 76, 190)
    $g.DrawRectangle($linePen, 76, 222, 470, 132)
    $g.DrawString("Domain Admins`r`nVPN Users`r`nFinance Share Access", $monoFont, $ink, 96, 244)

    $g.DrawString('Options', $tinyFont, $ink, 590, 190)
    $g.DrawString('Nested group members', $tinyFont, $mutedText, 590, 228)
    $g.DrawString('Current domain credentials', $tinyFont, $mutedText, 590, 262)
    $g.FillRectangle($blueBrush, 1020, 222, 250, 48)
    $g.DrawString('Load Groups', $smallFont, [System.Drawing.Brushes]::White, 1088, 232)
    $g.FillRectangle($greenBrush, 1020, 286, 250, 48)
    $g.DrawString('Export CSV', $smallFont, [System.Drawing.Brushes]::White, 1095, 296)

    $g.DrawLine($linePen, 76, 392, 1270, 392)
    $columns = @('Display Name', 'SAM Account', 'Domain Admins', 'VPN Users', 'Status')
    $x = 92
    foreach ($column in $columns) {
        $g.DrawString($column, $tinyFont, $ink, $x, 414)
        $x += 230
    }

    $rows = @(
        @('Avery Brooks', 'abrooks', 'Yes', 'Yes', 'Common to all'),
        @('Morgan Lee', 'mlee', 'Yes', '', 'Only in Domain Admins'),
        @('Riley Chen', 'rchen', '', 'Yes', 'Only in VPN Users'),
        @('Sam Patel', 'spatel', 'Yes', 'Yes', 'Common to all')
    )

    $y = 462
    foreach ($row in $rows) {
        $g.DrawLine($linePen, 76, $y - 14, 1270, $y - 14)
        $x = 92
        foreach ($value in $row) {
            $brush = if ($value -eq 'Common to all') { $greenBrush } elseif ($value -like 'Only*') { $amberBrush } else { $mutedText }
            $g.DrawString($value, $tinyFont, $brush, $x, $y)
            $x += 230
        }
        $y += 58
    }
}
finally {
    $font.Dispose()
    $smallFont.Dispose()
    $tinyFont.Dispose()
    $monoFont.Dispose()
    $dark.Dispose()
    $ink.Dispose()
    $mutedText.Dispose()
    $panel.Dispose()
    $bg.Dispose()
    $blueBrush.Dispose()
    $greenBrush.Dispose()
    $amberBrush.Dispose()
    $linePen.Dispose()
    $g.Dispose()
}

$preview.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()

Write-Host "Visual assets written to $assetDir"
