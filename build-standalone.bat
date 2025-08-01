@echo off
echo.
echo ===============================================
echo    BUILDING BLUESHOT STANDALONE INSTALLER
echo ===============================================
echo.

REM Change to script directory
cd /d "%~dp0"

REM Run the PowerShell build script
powershell.exe -ExecutionPolicy Bypass -File "build-standalone.ps1"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed. Press any key to exit.
    pause >nul
    exit /b %ERRORLEVEL%
)

echo.
echo Build completed! Press any key to exit.
pause >nul
