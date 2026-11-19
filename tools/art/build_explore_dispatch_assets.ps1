param([string]$Root = 'Assets/Resources/Art/Explore/Interiors')
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force -Path $Root | Out-Null
$pal = @{
    wood = [System.Drawing.Color]::FromArgb(255,107,68,35)
    woodLight = [System.Drawing.Color]::FromArgb(255,184,132,58)
    woodDark = [System.Drawing.Color]::FromArgb(255,58,32,16)
    woodGrain = [System.Drawing.Color]::FromArgb(255,130,85,43)
    stone = [System.Drawing.Color]::FromArgb(255,122,107,90)
    stoneLight = [System.Drawing.Color]::FromArgb(255,148,133,115)
    stoneDark = [System.Drawing.Color]::FromArgb(255,86,75,68)
    stoneSpeck = [System.Drawing.Color]::FromArgb(255,165,147,125)
}
function Fill($g,$color,$x,$y,$w,$h) {
    $brush = [System.Drawing.SolidBrush]::new($color)
    $g.FillRectangle($brush,$x,$y,$w,$h)
    $brush.Dispose()
}
function MakeTile($kind,$variant) {
    $bmp = [System.Drawing.Bitmap]::new(32,32,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
    if ($kind -eq 'wood') {
        Fill $g $pal.wood 0 0 32 32
        foreach ($y in @(0,8,16,24)) {
            Fill $g $pal.woodDark 0 $y 32 1
            Fill $g $pal.woodLight 0 ($y+1) 32 1
            $seam = (8 + $variant * 5 + [int]($y/8) * 7) % 24 + 4
            Fill $g $pal.woodDark $seam ($y+1) 1 7
            Fill $g $pal.woodGrain (($variant*7+$y+3)%26+2) ($y+4) 4 1
            Fill $g $pal.woodLight (($variant*11+$y+16)%27+1) ($y+6) 3 1
        }
    } else {
        Fill $g $pal.stone 0 0 32 32
        foreach ($y in @(0,16)) {
            Fill $g $pal.stoneDark 0 $y 32 1
            Fill $g $pal.stoneLight 0 ($y+1) 32 1
            $seam = if ($variant -eq 1) { 16 } else { 8 + $y }
            Fill $g $pal.stoneDark $seam ($y+1) 1 15
        }
        foreach ($i in 0..9) {
            $x = ($i*13+$variant*9+3)%30+1
            $y = ($i*19+$variant*5+5)%28+3
            Fill $g $pal.stoneSpeck $x $y 2 1
        }
    }
    $g.Dispose()
    $name = if ($kind -eq 'wood') { 'int_floor_wood_{0:d2}.png' -f $variant } else { 'int_floor_tile_{0:d2}.png' -f $variant }
    $bmp.Save((Join-Path (Resolve-Path $Root) $name),[System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}
foreach ($v in 1..3) { MakeTile wood $v }
foreach ($v in 1..2) { MakeTile tile $v }

function Interior($name,$w,$h,$draw) {
    $bmp=[System.Drawing.Bitmap]::new($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g=[System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::None
    & $draw $g
    $g.Dispose()
    $bmp.Save((Join-Path (Resolve-Path $Root) ($name+'.png')),[System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}
Interior 'int_wall_top' 32 32 {
    param($g)
    Fill $g $pal.woodDark 0 0 32 5; Fill $g $pal.wood 0 5 32 5
    Fill $g $pal.stone 0 10 32 22
    foreach($y in @(12,20,28)){Fill $g $pal.stoneDark 0 $y 32 1}
    Fill $g $pal.stoneLight 3 14 10 2; Fill $g $pal.stoneLight 17 23 8 2
}
Interior 'int_wall_front' 32 48 {
    param($g)
    Fill $g $pal.stone 0 0 32 48
    foreach($y in @(0,10,20,30,40)){Fill $g $pal.stoneDark 0 $y 32 1}
    foreach($y in @(5,25,45)){Fill $g $pal.stoneLight 8 $y 12 1}
    Fill $g $pal.woodDark 0 43 32 5; Fill $g $pal.wood 0 42 32 1
}
foreach($corner in @('nw','ne','sw','se')) {
    Interior ('int_wall_corner_'+$corner) 32 32 {
        param($g)
        Fill $g $pal.stone 0 0 32 32
        foreach($y in @(0,10,20,30)){Fill $g $pal.stoneDark 0 $y 32 1}
        if($corner.EndsWith('w')){Fill $g $pal.woodDark 0 0 4 32}else{Fill $g $pal.woodDark 28 0 4 32}
        if($corner.StartsWith('n')){Fill $g $pal.woodDark 0 0 32 4}else{Fill $g $pal.woodDark 0 28 32 4}
    }
}
foreach($state in @('open','closed')) {
    Interior ('int_door_'+$state) 32 64 {
        param($g)
        Fill $g $pal.woodDark 2 1 28 63
        Fill $g $pal.wood 5 4 22 58
        if($state -eq 'closed') {
            Fill $g $pal.stoneDark 7 7 18 52
            Fill $g $pal.woodDark 7 9 18 2; Fill $g $pal.woodDark 7 42 18 2
            Fill $g $pal.stoneLight 9 13 14 25
            Fill $g $pal.woodLight 21 32 2 2
        } else {
            Fill $g $pal.woodDark 8 5 16 55
            Fill $g $pal.stoneDark 10 8 12 52
            Fill $g $pal.wood 4 7 4 51
        }
    }
}
Interior 'int_window_01' 32 48 {
    param($g)
    Fill $g $pal.woodDark 3 4 26 36; Fill $g $pal.wood 5 6 22 32
    Fill $g ([System.Drawing.Color]::FromArgb(255,101,128,134)) 7 8 18 27
    Fill $g $pal.stoneLight 8 9 16 3; Fill $g $pal.woodDark 15 8 2 27
    Fill $g $pal.woodDark 7 21 18 2; Fill $g $pal.woodLight 1 39 30 3
}

$props = 'Assets/Resources/Art/Explore/Props'
New-Item -ItemType Directory -Force -Path $props | Out-Null
$ink=[System.Drawing.Color]::FromArgb(255,26,42,58)
$paper=[System.Drawing.Color]::FromArgb(255,232,216,192)
$amber=[System.Drawing.Color]::FromArgb(255,184,132,58)
$blue=[System.Drawing.Color]::FromArgb(255,45,72,97)
$red=[System.Drawing.Color]::FromArgb(255,139,58,42)
function Prop($name,$w,$h,$draw) {
    $bmp=[System.Drawing.Bitmap]::new($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g=[System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::None
    & $draw $g
    $g.Dispose()
    $bmp.Save((Join-Path (Resolve-Path $props) ($name+'.png')),[System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}
Prop 'prop_dispatch_desk' 64 32 {
    param($g)
    Fill $g $pal.woodDark 2 25 60 6; Fill $g $pal.wood 1 7 62 20
    Fill $g $amber 2 6 60 2; Fill $g $pal.woodGrain 4 10 56 1
    Fill $g $paper 15 10 36 13; Fill $g $ink 18 12 30 1
    foreach($y in @(15,18,21)){Fill $g $pal.stone 18 $y 30 1}
    foreach($x in @(25,33,41)){Fill $g $pal.stone $x 12 1 10}
    Fill $g $pal.woodDark 4 28 8 4; Fill $g $pal.woodDark 52 28 8 4
}
Prop 'prop_strategy_board' 32 64 {
    param($g)
    Fill $g $pal.woodDark 2 3 28 50; Fill $g $pal.wood 4 5 24 45
    Fill $g $paper 6 9 20 31; Fill $g $red 7 10 18 2
    foreach($y in @(15,20,25,30,35)){Fill $g $ink 8 $y 15 1}
    Fill $g $pal.woodDark 7 50 3 14; Fill $g $pal.woodDark 22 50 3 14
}
foreach($state in @('off','on')) {
    Prop ('prop_route_screen_'+$state) 64 32 {
        param($g)
        Fill $g $ink 1 2 62 27; Fill $g $pal.stoneDark 4 5 56 20
        if($state -eq 'on') {
            Fill $g $blue 5 6 54 18; Fill $g $amber 9 15 44 2
            foreach($x in @(11,25,39,51)){Fill $g $paper $x 12 3 7}
        }
        Fill $g $pal.stone 28 29 8 3
    }
}
Prop 'prop_radio' 32 32 {
    param($g)
    Fill $g $ink 3 9 26 19; Fill $g $pal.stone 5 11 22 14
    Fill $g $amber 8 14 11 3; Fill $g $ink 8 20 11 2
    Fill $g $red 22 14 3 3; Fill $g $ink 22 20 3 3
    Fill $g $ink 25 1 1 8
}
Prop 'prop_wall_clock' 32 32 {
    param($g)
    Fill $g $pal.woodDark 5 4 22 24; Fill $g $paper 7 6 18 20
    Fill $g $ink 15 9 2 9; Fill $g $ink 16 16 6 2
    Fill $g $amber 13 15 5 5
}
Prop 'prop_sofa' 64 32 {
    param($g)
    Fill $g $pal.woodDark 2 8 60 22; Fill $g $red 5 10 54 16
    Fill $g $pal.wood 5 21 54 5; Fill $g $amber 30 12 3 12
    Fill $g $pal.woodDark 5 27 7 5; Fill $g $pal.woodDark 52 27 7 5
}
Prop 'prop_teatable' 32 32 {
    param($g)
    Fill $g $pal.woodDark 4 23 24 6; Fill $g $pal.wood 2 8 28 17
    Fill $g $amber 3 8 26 2; Fill $g $paper 12 11 8 6
    Fill $g $blue 14 12 4 3; Fill $g $pal.woodDark 5 29 4 3; Fill $g $pal.woodDark 23 29 4 3
}

$files = @(Get-ChildItem $Root -Filter 'int_*.png') + @(Get-ChildItem $props -Filter 'prop_*.png')
$sheet = [System.Drawing.Bitmap]::new(640,([int][Math]::Ceiling($files.Count/4))*144,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$sg = [System.Drawing.Graphics]::FromImage($sheet)
$sg.Clear([System.Drawing.Color]::FromArgb(255,232,216,192))
$sg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$font = [System.Drawing.Font]::new('Arial',8)
$brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255,58,32,16))
for($i=0;$i -lt $files.Count;$i++) {
    $img=[System.Drawing.Bitmap]::new($files[$i].FullName)
    $x=($i%4)*160+8; $y=[int][Math]::Floor($i/4)*144+8
    $sg.DrawImage($img,[System.Drawing.Rectangle]::new($x,$y,$img.Width*2,$img.Height*2),[System.Drawing.Rectangle]::new(0,0,$img.Width,$img.Height),[System.Drawing.GraphicsUnit]::Pixel)
    $sg.DrawString($files[$i].BaseName,$font,$brush,$x,$y+114)
    $img.Dispose()
}
$sg.Dispose(); $font.Dispose(); $brush.Dispose()
$sheet.Save((Join-Path (Resolve-Path $Root) 'dispatch_room_contact_sheet.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
