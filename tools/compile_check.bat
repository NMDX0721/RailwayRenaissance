@echo off
setlocal
set "UNITY_EXE=D:\Unity Hub\Unity 6.5\Editor\Unity.exe"
set "PROJ_PATH=D:\Unity Project\RailwayRenaissance"
set "LOG_PATH=%PROJ_PATH%\compile_check.log"

if exist "%LOG_PATH%" del /f /q "%LOG_PATH%"

echo [*] Starting Unity batchmode compilation check...
echo     Editor: %UNITY_EXE%
echo     Project: %PROJ_PATH%
echo     Log: %LOG_PATH%

"%UNITY_EXE%" -quit -batchmode -projectPath "%PROJ_PATH%" -logFile "%LOG_PATH%"
set EXIT_CODE=%ERRORLEVEL%

if exist "%LOG_PATH%" (
    echo.
    echo [*] Scanning compilation results in log:
    findstr /C:"Tundra build success" /C:"error CS" "%LOG_PATH%"
    echo.
    echo [*] Batchmode exit code: %EXIT_CODE%
) else (
    echo [!] ERROR: Log file was not created.
)

exit /b %EXIT_CODE%
