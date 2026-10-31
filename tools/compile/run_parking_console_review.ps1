param(
    [ValidatePattern('^[A-Za-z0-9_.]+$')][string]$Method='NF5KParkingBrakePanelSetup.Batch',
    [ValidatePattern('^[A-Za-z0-9_-]+$')][string]$LogName='parking_console_v7'
)
$ErrorActionPreference='Stop'
$projectRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$packageRoot=Join-Path $projectRoot '.tmp.local-packages/com.unity.burst'
if((Get-Content -LiteralPath (Join-Path $packageRoot 'package.json') -Raw | ConvertFrom-Json).version -ne '1.8.30'){throw 'Expected local Burst 1.8.30'}
$manifestPath=Join-Path $projectRoot 'Packages/manifest.json'
$lockPath=Join-Path $projectRoot 'Packages/packages-lock.json'
$manifestBytes=[IO.File]::ReadAllBytes($manifestPath)
$lockBytes=[IO.File]::ReadAllBytes($lockPath)
function Restore-ExactFile([string]$path,[byte[]]$bytes){
    $temporary=$path+'.codex-restore.tmp'
    [IO.File]::WriteAllBytes($temporary,$bytes)
    for($attempt=0;$attempt -lt 12;$attempt++){
        try{[IO.File]::Move($temporary,$path,$true);return}catch{if($attempt -eq 11){throw};Start-Sleep -Milliseconds 250}
    }
}
try{
    $config=[Text.Encoding]::UTF8.GetString($manifestBytes) | ConvertFrom-Json -AsHashtable
    $config.dependencies['com.unity.burst']='file:../.tmp.local-packages/com.unity.burst'
    [IO.File]::WriteAllText($manifestPath,($config | ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false))
    $arguments='-batchmode -force-d3d11 --burst-disable-compilation -projectPath "{0}" -executeMethod {1} -logFile -' -f $projectRoot,$Method
    $process=Start-Process -FilePath 'D:/Unity Hub/Unity 6.5/Editor/Unity.exe' -ArgumentList $arguments -RedirectStandardOutput (Join-Path $PSScriptRoot ($LogName+'_stdout.log')) -RedirectStandardError (Join-Path $PSScriptRoot ($LogName+'_stderr.log')) -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    Write-Output ('UNITY_EXIT='+$process.ExitCode)
}finally{
    Restore-ExactFile $manifestPath $manifestBytes
    Restore-ExactFile $lockPath $lockBytes
    Write-Output 'ORIGINAL_PACKAGE_FILES_RESTORED'
}
