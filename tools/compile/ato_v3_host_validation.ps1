$ErrorActionPreference='Stop'
if (Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq 'D:\Unity Hub\Unity 6.5\Editor\Unity.exe' }) { throw 'Unity already running; avoid concurrent modification' }
$project=(Get-Location).Path
$manifest=Join-Path $project 'Packages/manifest.json';$lock=Join-Path $project 'Packages/packages-lock.json'
$manifestBytes=[IO.File]::ReadAllBytes($manifest);$lockBytes=[IO.File]::ReadAllBytes($lock)
$log=Join-Path $project 'tools/compile/ato_v3_host_20261029.log'
try {
 $p=Get-Content -Raw $manifest | ConvertFrom-Json -AsHashtable
 $p.dependencies['com.unity.burst']='file:../tools/compile/BurstOffline'
 [IO.File]::WriteAllText($manifest,($p | ConvertTo-Json -Depth 20),[Text.UTF8Encoding]::new($false))
 $args=@('-batchmode','-projectPath',('"'+$project+'"'),'-logFile',('"'+$log+'"'),'-executeMethod','NF5KAtoResearchReview.Batch')
 $process=Start-Process -FilePath 'D:\Unity Hub\Unity 6.5\Editor\Unity.exe' -ArgumentList $args -Wait -PassThru -WindowStyle Hidden
 "UNITY_EXIT_CODE=$($process.ExitCode)" | Tee-Object -FilePath 'tools/compile/ato_v3_host_exit.txt'
} finally {
 [IO.File]::WriteAllBytes($manifest,$manifestBytes)
 [IO.File]::WriteAllBytes($lock,$lockBytes)
}
if ($process.ExitCode -ne 0) { exit $process.ExitCode }

