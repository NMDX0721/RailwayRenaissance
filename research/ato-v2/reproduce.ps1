# Run in PowerShell 7 from the repository root. A new local directory preserves the frozen raw bytes.
$ErrorActionPreference='Stop'
$atoReplayRoot=Join-Path (Get-Location) ('tools/compile/ato-v2-replay-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfff'))
Expand-Archive -LiteralPath 'research/ato-v2/runs/20261026T011946759Z/frozen_sources.zip' -DestinationPath $atoReplayRoot
Push-Location $atoReplayRoot
try {
    dotnet run -c Release --project research/ato-v2/AtoV2.csproj -- --test
    if($LASTEXITCODE -ne 0){throw 'Frozen replay failed'}
} finally {Pop-Location}
