param([Parameter(Mandatory=$true)][string]$PlayerDirectory)
$ErrorActionPreference = 'Stop'
function Copy-DependencyTree([string]$Source, [string]$Destination) {
    & robocopy $Source $Destination /E /XJ /COPY:DAT /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Dependency copy failed ($LASTEXITCODE): $Source" }
    $global:LASTEXITCODE = 0
}
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$target = [IO.Path]::GetFullPath($PlayerDirectory)
$allowed = [IO.Path]::GetFullPath((Join-Path $project 'tools/compile')) + [IO.Path]::DirectorySeparatorChar
if (-not $target.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase)) { throw 'Review output must be under tools/compile.' }
$data = Join-Path $target 'RailwayMusicReview_Data'
if (-not (Test-Path -LiteralPath $data -PathType Container)) { throw 'Build the review player first.' }
$service = Join-Path $data 'StreamingAssets/MusicService'
if (Test-Path -LiteralPath $service) { throw 'Service destination already exists; use a fresh review output.' }
$node = (Get-Command node -CommandType Application | Select-Object -First 1).Source
$deps = Join-Path $PSScriptRoot 'node_modules'
if (-not (Test-Path -LiteralPath $deps -PathType Container)) { throw 'Run npm ci first.' }
New-Item -ItemType Directory -Path $service | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bridge.js'), (Join-Path $PSScriptRoot 'package.json'), (Join-Path $PSScriptRoot 'package-lock.json'), (Join-Path $PSScriptRoot 'README.md') -Destination $service
Copy-DependencyTree $deps (Join-Path $service 'node_modules')
Copy-Item -LiteralPath $node -Destination (Join-Path $service 'node.exe')
$nodeLicense = Join-Path (Split-Path $node) 'LICENSE'
if ([IO.File]::Exists($nodeLicense)) { Copy-Item -LiteralPath $nodeLicense -Destination (Join-Path $service 'NODE-LICENSE') }
@('Local test package only. Transitive license notices and source obligations require review before distribution.', ('Node: ' + (& $node --version)), ('Node SHA256: ' + (Get-FileHash -LiteralPath $node -Algorithm SHA256).Hash)) | Set-Content -LiteralPath (Join-Path $service 'REVIEW-NOTICE.txt') -Encoding utf8
$qqSource = Join-Path $project 'tools/music/qq-service'
$qqDependencies = Join-Path $qqSource 'node_modules'
if (Test-Path -LiteralPath $qqDependencies -PathType Container) {
    $qqTarget = Join-Path $data 'StreamingAssets/QQMusicService'
    if (Test-Path -LiteralPath $qqTarget) { throw 'QQ service destination already exists.' }
    New-Item -ItemType Directory -Path $qqTarget | Out-Null
    Copy-Item -LiteralPath (Join-Path $qqSource 'bridge.js'), (Join-Path $qqSource 'package.json'), (Join-Path $qqSource 'package-lock.json'), (Join-Path $qqSource 'README.md') -Destination $qqTarget
    Copy-DependencyTree $qqDependencies (Join-Path $qqTarget 'node_modules')
}
$kugouSource = Join-Path $project 'tools/music/kugou-service'
$kugouDependencies = Join-Path $kugouSource 'node_modules'
if (Test-Path -LiteralPath $kugouDependencies -PathType Container) {
    $kugouTarget = Join-Path $data 'StreamingAssets/KugouMusicService'
    if (Test-Path -LiteralPath $kugouTarget) { throw 'Kugou service destination already exists.' }
    New-Item -ItemType Directory -Path $kugouTarget | Out-Null
    Copy-Item -LiteralPath (Join-Path $kugouSource 'bridge.js'), (Join-Path $kugouSource 'package.json'), (Join-Path $kugouSource 'package-lock.json'), (Join-Path $kugouSource 'README.md') -Destination $kugouTarget
    Copy-DependencyTree $kugouDependencies (Join-Path $kugouTarget 'node_modules')
}
$bodianSource = Join-Path $project 'tools/music/bodian-service'
$bodianDependencies = Join-Path $bodianSource 'node_modules'
if (Test-Path -LiteralPath $bodianDependencies -PathType Container) {
    $node24 = Get-Command node -All -CommandType Application | Where-Object { [int]((& $_.Source --version).TrimStart('v').Split('.')[0]) -ge 24 } | Select-Object -First 1
    if ($node24) {
        $bodianTarget = Join-Path $data 'StreamingAssets/BodianMusicService'
        if (Test-Path -LiteralPath $bodianTarget) { throw 'Bodian service destination already exists.' }
        New-Item -ItemType Directory -Path $bodianTarget | Out-Null
        Copy-Item -LiteralPath (Join-Path $bodianSource 'bridge.js'), (Join-Path $bodianSource 'package.json'), (Join-Path $bodianSource 'package-lock.json'), (Join-Path $bodianSource 'README.md') -Destination $bodianTarget
        Copy-DependencyTree $bodianDependencies (Join-Path $bodianTarget 'node_modules')
        Copy-Item -LiteralPath $node24.Source -Destination (Join-Path $bodianTarget 'node.exe')
    } else { Write-Warning 'Bodian requires Node 24 or later; it was not included in this review package.' }
}
$subsonicSource = Join-Path $project 'tools/music/subsonic-service'
$subsonicTarget = Join-Path $data 'StreamingAssets/SubsonicMusicService'
if (Test-Path -LiteralPath $subsonicTarget) { throw 'Subsonic service destination already exists.' }
New-Item -ItemType Directory -Path $subsonicTarget | Out-Null
Copy-Item -LiteralPath (Join-Path $subsonicSource 'bridge.js'), (Join-Path $subsonicSource 'README.md') -Destination $subsonicTarget
$webdavSource = Join-Path $project 'tools/music/webdav-service'
$webdavDependencies = Join-Path $webdavSource 'node_modules'
if (Test-Path -LiteralPath $webdavDependencies -PathType Container) {
    $webdavTarget = Join-Path $data 'StreamingAssets/WebdavMusicService'
    if (Test-Path -LiteralPath $webdavTarget) { throw 'WebDAV service destination already exists.' }
    New-Item -ItemType Directory -Path $webdavTarget | Out-Null
    Copy-Item -LiteralPath (Join-Path $webdavSource 'bridge.js'), (Join-Path $webdavSource 'package.json'), (Join-Path $webdavSource 'package-lock.json'), (Join-Path $webdavSource 'README.md') -Destination $webdavTarget
    Copy-DependencyTree $webdavDependencies (Join-Path $webdavTarget 'node_modules')
}
$fixtures = Join-Path $target 'review-fixtures'
if (-not (Test-Path -LiteralPath $fixtures)) { New-Item -ItemType Directory -Path $fixtures | Out-Null }
Copy-Item -LiteralPath (Join-Path $project 'tools/music/review/private-services-fixture.js') -Destination $fixtures
Copy-Item -LiteralPath (Join-Path $project 'Assets/Resources/bgm/platform.mp3') -Destination (Join-Path $fixtures 'review.mp3')
Write-Output "Prepared private review service at $service"



$spotifySource = Join-Path $project 'tools/music/spotify-service'
$spotifyDependencies = Join-Path $spotifySource 'node_modules'
if (Test-Path -LiteralPath $spotifyDependencies -PathType Container) {
    $spotifyTarget = Join-Path $data 'StreamingAssets/SpotifyMusicService'
    if (Test-Path -LiteralPath $spotifyTarget) { throw 'Spotify service destination already exists.' }
    New-Item -ItemType Directory -Path $spotifyTarget | Out-Null
    Copy-Item -LiteralPath (Join-Path $spotifySource 'bridge.js'), (Join-Path $spotifySource 'package.json'), (Join-Path $spotifySource 'package-lock.json'), (Join-Path $spotifySource 'README.md'), (Join-Path $spotifySource 'COPYING'), (Join-Path $spotifySource 'UPSTREAM.json') -Destination $spotifyTarget
    Copy-DependencyTree (Join-Path $spotifySource 'vendor') (Join-Path $spotifyTarget 'vendor')
    Copy-DependencyTree $spotifyDependencies (Join-Path $spotifyTarget 'node_modules')
}
