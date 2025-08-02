@echo off
echo ========================================
echo Building Blueshot v0.1.0-beta Installer
echo ========================================
echo.

REM Check if Inno Setup is installed
set "INNO_SETUP_PATH=C:\Program Files (x86)\Inno Setup 6"
if not exist "%INNO_SETUP_PATH%\ISCC.exe" (
    echo ERROR: Inno Setup 6 not found at "%INNO_SETUP_PATH%"
    echo Please install Inno Setup 6 from: https://jrsoftware.org/isdl.php
    echo.
    pause
    exit /b 1
)

REM Create installer directory if it doesn't exist
if not exist "installer" mkdir installer

REM Clean previous builds
echo Cleaning previous builds...
if exist "installer\*.exe" del /q "installer\*.exe"
if exist "installer\*.tmp" del /q "installer\*.tmp"

REM Build the release first
echo.
echo Building release version...
dotnet publish -c Release --self-contained true -r win-x64 -o ./bin/Release/publish

if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to build release version
    pause
    exit /b 1
)

REM Build the installer
echo.
echo Building installer with Inno Setup...
"%INNO_SETUP_PATH%\ISCC.exe" "BlueshotInstaller.iss"

if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to build installer
    pause
    exit /b 1
)

echo.
echo ========================================
echo Installer built successfully!
echo ========================================
echo.
echo Output: installer\Blueshot-Setup-v0.1.0-beta.exe
echo.

REM Check if installer was created
if exist "installer\Blueshot-Setup-v0.1.0-beta.exe" (
    echo File size:
    for %%I in ("installer\Blueshot-Setup-v0.1.0-beta.exe") do echo %%~zI bytes
    echo.
    echo The installer is ready for distribution!
    echo.
    set /p run_installer="Do you want to test the installer now? (y/n): "
    if /i "%run_installer%"=="y" (
        echo Running installer...
        "installer\Blueshot-Setup-v0.1.0-beta.exe"
    )
) else (
    echo ERROR: Installer file not found!
    pause
    exit /b 1
)

echo.
pause
