param(
    [Parameter(Mandatory)][string]$IdleSheet,
    [Parameter(Mandatory)][string]$WalkSheet,
    [Parameter(Mandatory)][string]$Destination
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$directions = 'down', 'up', 'left', 'right'
$frames = [System.Collections.Generic.List[object]]::new()

function Read-Sheet($path, $rows, $columns, $state) {
    $sheet = [Drawing.Bitmap]::new((Resolve-Path -LiteralPath $path).Path)
    try {
        for ($row = 0; $row -lt $rows; $row++) {
            for ($column = 0; $column -lt $columns; $column++) {
                $x0 = [int][Math]::Floor($column * $sheet.Width / $columns)
                $y0 = [int][Math]::Floor($row * $sheet.Height / $rows)
                $x1 = [int][Math]::Floor(($column + 1) * $sheet.Width / $columns) - 1
                $y1 = [int][Math]::Floor(($row + 1) * $sheet.Height / $rows) - 1
                $left = $x1; $right = $x0; $top = $y1; $bottom = $y0
                for ($y = $y0; $y -le $y1; $y++) {
                    for ($x = $x0; $x -le $x1; $x++) {
                        if ($sheet.GetPixel($x, $y).A -lt 128) { continue }
                        $left = [Math]::Min($left, $x); $right = [Math]::Max($right, $x)
                        $top = [Math]::Min($top, $y); $bottom = [Math]::Max($bottom, $y)
                    }
                }
                if ($right -le $left -or $bottom -le $top) { throw "Empty frame $row,$column in $path" }
                $bounds = [Drawing.Rectangle]::FromLTRB($left, $top, $right + 1, $bottom + 1)
                $sprite = $sheet.Clone($bounds, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
                $direction = if ($state -eq 'idle') { $directions[$row * 2 + $column] } else { $directions[$row] }
                $index = if ($state -eq 'idle') { 0 } else { $column }
                $frames.Add([pscustomobject]@{ Name = "char_player_${state}_${direction}_${index}"; Image = $sprite; State = $state })
            }
        }
    } finally { $sheet.Dispose() }
}

Read-Sheet $IdleSheet 2 2 'idle'
Read-Sheet $WalkSheet 4 4 'walk'
$destinationPath = (New-Item -ItemType Directory -Path $Destination -Force).FullName
try {
    foreach ($state in 'idle', 'walk') {
        $group = @($frames | Where-Object State -eq $state)
        $height = ($group | ForEach-Object { $_.Image.Height } | Measure-Object -Maximum).Maximum
        $scale = 184.0 / $height
        foreach ($frame in $group) {
            $source = $frame.Image
            $headLeft = $source.Width - 1; $headRight = 0
            for ($y = 0; $y -lt [int]($source.Height * 0.17); $y++) {
                for ($x = 0; $x -lt $source.Width; $x++) {
                    if ($source.GetPixel($x, $y).A -lt 128) { continue }
                    $headLeft = [Math]::Min($headLeft, $x); $headRight = [Math]::Max($headRight, $x)
                }
            }
            $w = [int][Math]::Round($source.Width * $scale)
            $h = [int][Math]::Round($source.Height * $scale)
            $ox = [int][Math]::Round(64 - ($headLeft + $headRight + 1) * $scale / 2)
            $oy = 191 - $h
            if ($ox -lt 0 -or $ox + $w -gt 128 -or $oy -lt 0) { throw "Frame would clip: $($frame.Name)" }
            $output = Join-Path $destinationPath ($frame.Name + '.png')
            if (Test-Path -LiteralPath $output) { throw "Frame already exists: $output" }
            $canvas = [Drawing.Bitmap]::new(128, 192, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
            try {
                for ($y = 0; $y -lt $h; $y++) {
                    $sy = [int][Math]::Floor(($y + 0.5) * $source.Height / $h)
                    for ($x = 0; $x -lt $w; $x++) {
                        $sx = [int][Math]::Floor(($x + 0.5) * $source.Width / $w)
                        $pixel = $source.GetPixel($sx, $sy)
                        if ($pixel.A -ge 128) { $canvas.SetPixel($ox + $x, $oy + $y, [Drawing.Color]::FromArgb(255, $pixel.R, $pixel.G, $pixel.B)) }
                    }
                }
                $canvas.Save($output, [Drawing.Imaging.ImageFormat]::Png)
            } finally { $canvas.Dispose() }
        }
    }
} finally { foreach ($frame in $frames) { $frame.Image.Dispose() } }
