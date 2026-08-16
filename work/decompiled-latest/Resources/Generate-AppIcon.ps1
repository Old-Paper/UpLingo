param(
    [string]$OutputPath = (Join-Path $PSScriptRoot 'UpLingo.ico')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-RoundedRectanglePath {
    param(
        [System.Drawing.RectangleF]$Bounds,
        [single]$Radius
    )

    $diameter = $Radius * 2
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc($Bounds.Left, $Bounds.Top, $diameter, $diameter, 180, 90)
    $path.AddArc($Bounds.Right - $diameter, $Bounds.Top, $diameter, $diameter, 270, 90)
    $path.AddArc($Bounds.Right - $diameter, $Bounds.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($Bounds.Left, $Bounds.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconFrame {
    param([int]$Size)

    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.Clear([System.Drawing.Color]::Transparent)

        $margin = [single]($Size * 0.025)
        $bounds = [System.Drawing.RectangleF]::new($margin, $margin, $Size - 2 * $margin, $Size - 2 * $margin)
        $backgroundPath = New-RoundedRectanglePath -Bounds $bounds -Radius ([single]($Size * 0.22))
        $backgroundBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new($bounds, [System.Drawing.ColorTranslator]::FromHtml('#17243c'), [System.Drawing.ColorTranslator]::FromHtml('#0b1220'), 90)
        try {
            $graphics.FillPath($backgroundBrush, $backgroundPath)
        }
        finally {
            $backgroundBrush.Dispose()
            $backgroundPath.Dispose()
        }

        $uPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
        $uPath.StartFigure()
        $uPath.AddLine([single]($Size * 0.285), [single]($Size * 0.275), [single]($Size * 0.285), [single]($Size * 0.565))
        $uPath.AddBezier([single]($Size * 0.285), [single]($Size * 0.565), [single]($Size * 0.285), [single]($Size * 0.70), [single]($Size * 0.55), [single]($Size * 0.77), [single]($Size * 0.62), [single]($Size * 0.60))
        $uBrushBounds = [System.Drawing.RectangleF]::new(0, 0, $Size, $Size)
        $uBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new($uBrushBounds, [System.Drawing.ColorTranslator]::FromHtml('#19c3ff'), [System.Drawing.ColorTranslator]::FromHtml('#44e0bd'), 35)
        $uPen = [System.Drawing.Pen]::new($uBrush, [single]([Math]::Max(2, $Size * 0.115)))
        $uPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $uPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        try {
            $graphics.DrawPath($uPen, $uPath)
        }
        finally {
            $uPen.Dispose()
            $uBrush.Dispose()
            $uPath.Dispose()
        }

        $riseBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new($uBrushBounds, [System.Drawing.ColorTranslator]::FromHtml('#ff2457'), [System.Drawing.ColorTranslator]::FromHtml('#ff6b35'), 315)
        $risePen = [System.Drawing.Pen]::new($riseBrush, [single]([Math]::Max(2, $Size * 0.095)))
        $risePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $risePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $risePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        try {
            $graphics.DrawLine($risePen, [single]($Size * 0.54), [single]($Size * 0.64), [single]($Size * 0.77), [single]($Size * 0.36))
            $graphics.DrawLine($risePen, [single]($Size * 0.64), [single]($Size * 0.35), [single]($Size * 0.78), [single]($Size * 0.34))
            $graphics.DrawLine($risePen, [single]($Size * 0.78), [single]($Size * 0.34), [single]($Size * 0.765), [single]($Size * 0.49))
        }
        finally {
            $risePen.Dispose()
            $riseBrush.Dispose()
        }

        $stream = [System.IO.MemoryStream]::new()
        try {
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            return ,$stream.ToArray()
        }
        finally {
            $stream.Dispose()
        }
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$frames = @($sizes | ForEach-Object { New-IconFrame -Size $_ })
$directory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($directory)) {
    [System.IO.Directory]::CreateDirectory($directory) | Out-Null
}

$file = [System.IO.File]::Open($OutputPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
$writer = [System.IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$frames.Count)
    $offset = 6 + 16 * $frames.Count
    for ($index = 0; $index -lt $frames.Count; $index++) {
        $size = $sizes[$index]
        $dimension = if ($size -eq 256) { 0 } else { $size }
        $writer.Write([byte]$dimension)
        $writer.Write([byte]$dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$frames[$index].Length)
        $writer.Write([uint32]$offset)
        $offset += $frames[$index].Length
    }
    foreach ($frame in $frames) {
        $writer.Write($frame)
    }
}
finally {
    $writer.Dispose()
    $file.Dispose()
}

Write-Output $OutputPath
