@echo off
echo ==============================================
echo   Blueshot Icon Integration Setup
echo ==============================================
echo.

REM Create icons directory if it doesn't exist
if not exist "icons" (
    mkdir icons
    echo Created icons directory.
)

REM Copy icons from bin to make them available at runtime
if exist "bin\Debug\net8.0-windows" (
    if not exist "bin\Debug\net8.0-windows\icons" (
        mkdir "bin\Debug\net8.0-windows\icons"
        echo Created icons directory in bin folder.
    )
)

if exist "bin\Release\net8.0-windows" (
    if not exist "bin\Release\net8.0-windows\icons" (
        mkdir "bin\Release\net8.0-windows\icons"
        echo Created icons directory in bin folder.
    )
)

echo.
echo Setup complete!
echo.
echo Next steps:
echo 1. Design icons in Canva using the guide in icons\icon-design-guide.md
echo 2. Download PNG files with these exact names:
echo    - save.png, copy.png, zoom-in.png, zoom-out.png
echo    - actual-size.png, fit-window.png, select.png
echo    - highlight.png, rectangle.png, line.png, arrow.png
echo    - text.png, counter.png, crop.png
echo 3. Place the PNG files in the icons\ directory
echo 4. Build and run the application to see your custom icons!
echo.
echo Press any key to continue...
pause >nul
