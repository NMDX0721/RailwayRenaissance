---
description: Check Unity Editor log for compilation errors and runtime issues
---

# check-unity-errors

Read the Unity Editor log and report any errors, warnings, or compilation issues.

## Usage

Use this after making code changes to Unity C# scripts to verify compilation succeeded and identify runtime errors.

## Procedure

1. Read the Unity Editor log file
2. Filter for errors, warnings, and compilation messages
3. Report findings

## Log location

- Primary: `C:\Users\Oe_Lee\AppData\Local\Unity\Editor\Editor.log`
- Previous: `C:\Users\Oe_Lee\AppData\Local\Unity\Editor\Editor-prev.log`

## Command

```powershell
$logPath = "C:\Users\Oe_Lee\AppData\Local\Unity\Editor\Editor.log"
if (Test-Path $logPath) {
    $content = Get-Content $logPath -Tail 200
    $errors = $content | Select-String -Pattern "error|Error|ERROR" -CaseSensitive:$false
    $warnings = $content | Select-String -Pattern "warning|Warning|WARNING" -CaseSensitive:$false
    Write-Output "=== ERRORS ==="
    $errors | Select-Object -First 20
    Write-Output "`n=== WARNINGS ==="
    $warnings | Select-Object -First 10
    Write-Output "`nTotal errors: $($errors.Count), warnings: $($warnings.Count)"
} else {
    Write-Output "Unity Editor log not found at: $logPath"
}
```

## Notes

- Check the log AFTER Unity has recompiled (wait for auto-refresh or press Ctrl+R in Unity)
- Focus on `error CS` patterns for C# compilation errors
- `warning CS0618` = obsolete API usage (common: `FindObjectOfType`)
- Project logs also at: `D:\Unity Project\RailwayRenaissance\Logs\`
