param([string]$Root = 'Assets/Resources/Art/Explore/Tiles')
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force -Path $Root | Out-Null
$c = @{
 grass = [Drawing.Color]::FromArgb(255,91,120,70)
 grassLight = [Drawing.Color]::FromArgb(255,112,143,79)
 grassDark = [Drawing.Color]::FromArgb(255,64,92,54)
 dirt = [Drawing.Color]::FromArgb(255,157,125,85)
 dirtLight = [Drawing.Color]::FromArgb(255,184,151,101)
 dirtDark = [Drawing.Color]::FromArgb(255,119,91,65)
 stone = [Drawing.Color]::FromArgb(255,122,107,90)
 stoneLight = [Drawing.Color]::FromArgb(255,165,147,125)
 stoneDark = [Drawing.Color]::FromArgb(255,86,75,68)
 rail = [Drawing.Color]::FromArgb(255,38,43,50)
 railShine = [Drawing.Color]::FromArgb(255,131,127,121)
 sleeper = [Drawing.Color]::FromArgb(255,82,57,39)
 sleeperLight = [Drawing.Color]::FromArgb(255,122,83,50)
 yellow = [Drawing.Color]::FromArgb(255,212,168,67)
}
function Fill($g,$col,$x,$y,$w,$h) {
 $b=[Drawing.SolidBrush]::new($col); $g.FillRectangle($b,$x,$y,$w,$h); $b.Dispose()
}
function Tile($name,$draw) {
 $im=[Drawing.Bitmap]::new(32,32,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[Drawing.Graphics]::FromImage($im)
 $g.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::None
 & $draw $g
 $g.Dispose()
 $im.Save((Join-Path (Resolve-Path $Root) ($name+'.png')),[Drawing.Imaging.ImageFormat]::Png)
 $im.Dispose()
}
foreach($v in 1..3) {
 Tile ('tile_ground_grass_{0:d2}' -f $v) {
  param($g)
  Fill $g $c.grass 0 0 32 32
  foreach($i in 0..12) {
   $x=2+(($i*11+$v*7)%27); $y=2+(($i*17+$v*5)%27)
   Fill $g $c.grassDark $x $y 3 1
   Fill $g $c.grassLight ($x+1) ($y-1) 1 2
  }
 }
 Tile ('tile_ground_dirt_{0:d2}' -f $v) {
  param($g)
  Fill $g $c.dirt 0 0 32 32
  foreach($i in 0..13) {
   $x=2+(($i*13+$v*5)%27); $y=2+(($i*19+$v*7)%27)
   Fill $g $c.dirtDark $x $y 2 1
   if(($i%3)-eq 0){Fill $g $c.dirtLight ($x+1) ($y+2) 2 1}
  }
 }
}
Tile 'tile_ground_ballast_01' {
 param($g)
 Fill $g $c.stone 0 0 32 32
 foreach($i in 0..22) {
  $x=2+(($i*13)%26);$y=2+(($i*17)%26)
  Fill $g $c.stoneDark $x $y 3 2
  Fill $g $c.stoneLight ($x+1) $y 2 1
 }
}
Tile 'tile_track_horizontal_01' {
 param($g)
 Fill $g $c.stone 0 0 32 32
 foreach($x in @(5,21)) {
  Fill $g $c.sleeper $x 2 6 28
  Fill $g $c.sleeperLight ($x+1) 3 2 25
 }
 foreach($y in @(8,22)) {
  Fill $g $c.rail 0 $y 32 4
  Fill $g $c.railShine 0 $y 32 1
 }
}
Tile 'tile_platform_surface_01' {
 param($g)
 Fill $g $c.stoneLight 0 0 32 32
 foreach($y in @(0,16)) {Fill $g $c.stone 0 $y 32 1}
 Fill $g $c.stone 16 1 1 15; Fill $g $c.stone 8 17 1 15
 Fill $g $c.dirtLight 4 5 3 1; Fill $g $c.stoneDark 24 24 2 1
}
Tile 'tile_platform_edge_yellow_01' {
 param($g)
 Fill $g $c.stoneLight 0 0 32 32
 Fill $g $c.stone 0 0 32 1; Fill $g $c.stone 16 1 1 18
 Fill $g $c.yellow 0 20 32 3
 Fill $g $c.stoneDark 0 24 32 8
 Fill $g $c.stone 0 24 32 2
 Fill $g $c.stone 0 30 32 1
}
$files=@(Get-ChildItem $Root -Filter 'tile_*.png' | Sort-Object Name)
$sheet=[Drawing.Bitmap]::new(640,([int][Math]::Ceiling($files.Count/5))*120,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$sg=[Drawing.Graphics]::FromImage($sheet)
$sg.Clear([Drawing.Color]::FromArgb(255,232,216,192))
$sg.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$font=[Drawing.Font]::new('Arial',7)
$brush=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255,58,32,16))
for($i=0;$i -lt $files.Count;$i++) {
 $im=[Drawing.Bitmap]::new($files[$i].FullName)
 $x=($i%5)*128; $y=[int][Math]::Floor($i/5)*120
 $sg.DrawImage($im,[Drawing.Rectangle]::new($x,$y,96,96),[Drawing.Rectangle]::new(0,0,32,32),[Drawing.GraphicsUnit]::Pixel)
 $sg.DrawString($files[$i].BaseName,$font,$brush,$x,$y+98)
 $im.Dispose()
}
$sg.Dispose();$font.Dispose();$brush.Dispose()
$sheet.Save((Join-Path (Resolve-Path $Root) 'outdoor_base_contact_sheet.png'),[Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
