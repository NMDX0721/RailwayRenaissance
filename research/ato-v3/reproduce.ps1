param([ValidateSet('1','2')][string]$Version='2')
$ErrorActionPreference='Stop'
$run=if($Version -eq '1'){'20261028T120907846Z'}else{'20261028T121643698Z'}
$argument=if($Version -eq '1'){'--test'}else{'--test-v2'}
$atoReplayRoot=Join-Path (Get-Location) ('tools/compile/ato-v3-replay-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfff'))
Expand-Archive -LiteralPath "research/ato-v3/runs/$run/frozen_sources.zip" -DestinationPath $atoReplayRoot
Push-Location $atoReplayRoot
try {dotnet run -c Release --project research/ato-v3/AtoV3.csproj -- $argument;if($LASTEXITCODE -ne 0){throw 'Frozen replay failed'}} finally {Pop-Location}
