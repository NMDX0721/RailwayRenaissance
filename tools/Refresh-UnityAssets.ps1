param([int]$EditorProcessId = 0)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class RailUnityMenuRefresh {
 [DllImport("user32.dll")] public static extern IntPtr GetMenu(IntPtr w);
 [DllImport("user32.dll")] public static extern int GetMenuItemCount(IntPtr m);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetMenuString(IntPtr m,uint i,StringBuilder s,int n,uint f);
 [DllImport("user32.dll")] public static extern IntPtr GetSubMenu(IntPtr m,int i);
 [DllImport("user32.dll")] public static extern uint GetMenuItemID(IntPtr m,int i);
 [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr w,uint m,IntPtr a,IntPtr b);
}
'@
$taskEditor = if ($EditorProcessId) { Get-Process -Id $EditorProcessId } else {
    Get-Process Unity | Where-Object { $_.MainWindowTitle -like 'RailwayRenaissance - *' } | Select-Object -First 1
}
if (!$taskEditor -or $taskEditor.MainWindowTitle -notlike 'RailwayRenaissance - *') { throw 'Project editor window not found' }
$taskWindow = $taskEditor.MainWindowHandle
function Find-RefreshCommand([IntPtr]$taskMenu) {
    for ($taskIndex=0; $taskIndex -lt [RailUnityMenuRefresh]::GetMenuItemCount($taskMenu); $taskIndex++) {
        $taskLabel = New-Object System.Text.StringBuilder 256
        [void][RailUnityMenuRefresh]::GetMenuString($taskMenu,[uint32]$taskIndex,$taskLabel,256,1024)
        if ($taskLabel.ToString() -match '^Refresh\b') { return [RailUnityMenuRefresh]::GetMenuItemID($taskMenu,$taskIndex) }
        $taskSub = [RailUnityMenuRefresh]::GetSubMenu($taskMenu,$taskIndex)
        if ($taskSub -ne [IntPtr]::Zero) { $taskFound = Find-RefreshCommand $taskSub; if ($null -ne $taskFound) { return $taskFound } }
    }
}
$taskCommand = Find-RefreshCommand ([RailUnityMenuRefresh]::GetMenu($taskWindow))
if ($null -eq $taskCommand) { throw 'Refresh menu unavailable' }
if (![RailUnityMenuRefresh]::PostMessage($taskWindow,273,[IntPtr]$taskCommand,[IntPtr]::Zero)) { throw 'Refresh dispatch failed' }
'Project editor Refresh dispatched'
