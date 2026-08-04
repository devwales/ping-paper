# Ping icon generator
# Draws the soft "bubble" orb at several sizes and packs them into ping.ico.
# Run:  powershell -ExecutionPolicy Bypass -File assets/make-icon.ps1

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$outDir = $PSScriptRoot
if (-not $outDir) { $outDir = Split-Path -Parent $MyInvocation.MyCommand.Path }

function New-Orb {
    param([int]$Size)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $inset = [Math]::Max(1, [int]($Size * 0.06))
    $d = $Size - 2 * $inset

    # Soft radial disc: near-white center easing into gentle teal.
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddEllipse($inset, $inset, $d, $d)
    $brush = New-Object System.Drawing.Drawing2D.PathGradientBrush($path)
    $brush.CenterColor = [System.Drawing.Color]::FromArgb(255, 244, 250, 248)
    $brush.SurroundColors = @([System.Drawing.Color]::FromArgb(255, 166, 212, 202))
    $brush.FocusScales = New-Object System.Drawing.PointF(0.35, 0.3)
    $g.FillEllipse($brush, $inset, $inset, $d, $d)

    # Quiet highlight, top-left.
    $hw = [int]($d * 0.30)
    $hh = [int]($d * 0.17)
    $hx = $inset + [int]($d * 0.18)
    $hy = $inset + [int]($d * 0.12)
    $highlight = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(140, 255, 255, 255))
    $state = $g.Save()
    $g.TranslateTransform($hx + $hw / 2, $hy + $hh / 2)
    $g.RotateTransform(-25)
    $g.FillEllipse($highlight, -$hw / 2, -$hh / 2, $hw, $hh)
    $g.Restore($state)

    $g.Dispose()
    $brush.Dispose()
    $highlight.Dispose()
    $path.Dispose()
    return $bmp
}

$sizes = 16, 32, 48, 64, 256
$pngFiles = @()

foreach ($s in $sizes) {
    $bmp = New-Orb -Size $s
    $file = Join-Path $outDir "ping-$s.png"
    $bmp.Save($file, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $pngFiles += $file
    Write-Host "wrote $file"
}

# Pack PNGs into a multi-size .ico (PNG-compressed entries, Vista+)
$icoPath = Join-Path $outDir "ping.ico"
$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter($fs)

$bw.Write([uint16]0)              # reserved
$bw.Write([uint16]1)              # type: icon
$bw.Write([uint16]$sizes.Count)   # image count

$offset = 6 + 16 * $sizes.Count
$blobs = @()
foreach ($f in $pngFiles) {
    $bytes = [System.IO.File]::ReadAllBytes($f)
    $blobs += , $bytes
    $size = [int](Split-Path -Leaf $f).Replace("ping-", "").Replace(".png", "")
    $dimByte = if ($size -ge 256) { 0 } else { $size }
    $bw.Write([byte]$dimByte)     # width
    $bw.Write([byte]$dimByte)     # height
    $bw.Write([byte]0)            # palette
    $bw.Write([byte]0)            # reserved
    $bw.Write([uint16]1)          # planes
    $bw.Write([uint16]32)         # bits per pixel
    $bw.Write([uint32]$bytes.Length)
    $bw.Write([uint32]$offset)
    $offset += $bytes.Length
}
foreach ($b in $blobs) { $bw.Write($b) }

$bw.Close()
$fs.Close()
Write-Host "wrote $icoPath"
