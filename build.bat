@echo off
echo Building Blueshot Application...

echo Cleaning previous builds...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
if exist installer rmdir /s /q installer

echo Building application...
dotnet build --configuration Release --verbosity minimal
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo Publishing application...
dotnet publish --configuration Release --output "bin\Release\net8.0-windows\publish" --self-contained false --verbosity minimal
if %ERRORLEVEL% neq 0 (
    echo Publish failed!
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo Published files location: bin\Release\net8.0-windows\publish
echo.
echo To create an installer:
echo 1. Install Inno Setup from https://jrsoftware.org/isinfo.php
echo 2. Run: ISCC.exe BlueshotInstaller.iss
echo.
echo Manual Installation:
echo 1. Copy the contents of 'bin\Release\net8.0-windows\publish' to your desired folder
echo 2. Create shortcuts as needed
echo 3. For auto-startup: Add shortcut to Windows startup folder (Win+R, shell:startup)
echo.
echo The application will register Print Screen hotkey when it starts.
pause
