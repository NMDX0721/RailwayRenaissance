---
description: Kill and restart the NMDX2_Web Electron game dev server
---

# restart-game

Kill any running Electron process for the NMDX2_Web game, wait briefly, then restart the dev server with `npm start`.

## Usage

Run this command after making code changes to the NMDX2_Web game to verify the changes visually.

## Procedure

1. Kill all `electron.exe` processes
2. Wait 1-2 seconds for process cleanup
3. Start the dev server with `npm start` in the NMDX2_Web directory

## Command

```powershell
taskkill /F /IM "electron.exe" 2>$null; Start-Sleep -Seconds 1; cd "D:\Unity Project\NMDX2_Web"; npm start
```

## Notes

- Timeout: 5000ms (game starts quickly)
- If electron.exe is not running, `taskkill` fails silently (2>$null suppresses error)
- The working directory is `D:\Unity Project\NMDX2_Web`
- Use this after every code edit to the web/Electron version of the game
