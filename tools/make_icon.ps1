# Draws Tradewatch/app.ico (clock face + "open" dot) at the standard Windows icon sizes.
# Usage (from the repo root):  powershell -ExecutionPolicy Bypass -File tools/make_icon.ps1
Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot "..\Tradewatch\app.ico"
$sizes = 16, 24, 32, 48, 64, 128, 256
$bg = [System.Drawing.ColorTranslator]::FromHtml("#0A1E1C")
$rim = [System.Drawing.ColorTranslator]::FromHtml("#1A4A44")
$accent = [System.Drawing.ColorTranslator]::FromHtml("#0aff9d")

function Draw-Frame([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = "AntiAlias"
    $g.Clear([System.Drawing.Color]::Transparent)

    # Rounded-square tile
    $r = [Math]::Max(2, $s * 0.22)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $w = $s - 1
    $path.AddArc(0, 0, $r * 2, $r * 2, 180, 90)
    $path.AddArc($w - $r * 2, 0, $r * 2, $r * 2, 270, 90)
    $path.AddArc($w - $r * 2, $w - $r * 2, $r * 2, $r * 2, 0, 90)
    $path.AddArc(0, $w - $r * 2, $r * 2, $r * 2, 90, 90)
    $path.CloseFigure()
    $g.FillPath((New-Object System.Drawing.SolidBrush $bg), $path)
    if ($s -ge 32) { $g.DrawPath((New-Object System.Drawing.Pen $rim, ($s / 32)), $path) }

    # Clock face
    $c = $s / 2
    $cr = $s * 0.31
    $pen = New-Object System.Drawing.Pen $accent, ([Math]::Max(1.2, $s * 0.075))
    $pen.StartCap = "Round"; $pen.EndCap = "Round"
    $g.DrawEllipse($pen, $c - $cr, $c - $cr, $cr * 2, $cr * 2)
    # Hands at about 10:10 o'clock... simplified to 12 and 3 so they stay readable at 16px
    $g.DrawLine($pen, $c, $c, $c, $c - $cr * 0.62)
    $g.DrawLine($pen, $c, $c, $c + $cr * 0.5, $c)

    # "Open" dot, bottom-right (same idea as the green tray dot)
    if ($s -ge 24) {
        $d = $s * 0.26
        $x = $s - $d - $s * 0.06
        $g.FillEllipse((New-Object System.Drawing.SolidBrush $bg), $x - $s * 0.04, $x - $s * 0.04, $d + $s * 0.08, $d + $s * 0.08)
        $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::LimeGreen)), $x, $x, $d, $d)
    }
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    if ($s -ge 256) {
        # 256px frame as PNG (keeps the file small; every Windows since Vista reads it)
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    } else {
        # Smaller frames as classic 32-bit bitmaps, which every icon loader understands
        $bw = New-Object System.IO.BinaryWriter $ms
        $bw.Write([UInt32]40); $bw.Write([Int32]$s); $bw.Write([Int32]($s * 2))
        $bw.Write([UInt16]1); $bw.Write([UInt16]32); $bw.Write([UInt32]0)
        $bw.Write([UInt32]0); $bw.Write([Int32]0); $bw.Write([Int32]0); $bw.Write([UInt32]0); $bw.Write([UInt32]0)
        for ($y = $s - 1; $y -ge 0; $y--) {
            for ($x = 0; $x -lt $s; $x++) {
                $p = $bmp.GetPixel($x, $y)
                $bw.Write([byte]$p.B); $bw.Write([byte]$p.G); $bw.Write([byte]$p.R); $bw.Write([byte]$p.A)
            }
        }
        # AND mask: all zero (alpha channel does the masking), rows padded to 4 bytes
        $maskRow = [Math]::Ceiling($s / 32) * 4
        $bw.Write((New-Object byte[] ($maskRow * $s)))
        $bw.Flush()
    }
    $bmp.Dispose()
    return , $ms.ToArray()
}

# ICO container: header, one directory entry per size, then the PNG frames.
$frames = $sizes | ForEach-Object { , (Draw-Frame $_) }
$fs = [System.IO.File]::Create($out)
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $len = $frames[$i].Length
    $dim = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32); $bw.Write([UInt32]$len); $bw.Write([UInt32]$offset)
    $offset += $len
}
foreach ($f in $frames) { $bw.Write($f) }
$bw.Close()
"Wrote $out"
