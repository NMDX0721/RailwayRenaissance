---
description: Validate JavaScript syntax for game files or userscript
---

# validate-js

Run `node -c` to validate JavaScript syntax without executing the code.

## Usage

Use this after editing any `.js` file to catch syntax errors before testing.

## Parameters

- `$1` — file path to validate (required)

## Default targets

If no argument is provided, validate the Microsoft Rewards userscript:

```powershell
node -c "E:\NMDX\微软积分商城签到.user.js"
```

## Validate multiple NMDX2_Web files

```powershell
cd "D:\Unity Project\NMDX2_Web"; node -e "['main.js','src/js/main.js','src/js/station.js','src/js/auth.js'].forEach(f=>{try{new Function(fs.readFileSync(f,'utf8'));console.log('OK: '+f);}catch(e){console.log('ERR: '+f+' - '+e.message);}});"
```

## Notes

- `node -c` checks syntax only, does not execute
- Exit code 0 = valid, non-zero = syntax error
- Common files to validate:
  - `E:\NMDX\微软积分商城签到.user.js` — Microsoft Rewards userscript
  - `D:\Unity Project\NMDX2_Web\src\js\*.js` — game modules
  - `D:\Unity Project\NMDX2_Web\main.js` — Electron main process
