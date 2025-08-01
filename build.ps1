# Build and Package Blueshot Application
# This script builds the application and creates a standalone installer

param(
    [string]$Configuration = "Release",
    [switch]$CreateInstaller = $true,
    [switch]$SelfContained = $true
)

Write-Host "Building Blueshot Standalone Application..." -ForegroundColor Green

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "bin") { Remove-Item -Recurse -Force "bin" }
if (Test-Path "obj") { Remove-Item -Recurse -Force "obj" }
if (Test-Path "installer") { Remove-Item -Recurse -Force "installer" }

# Build the application
Write-Host "Building application..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Publish as self-contained if requested
if ($SelfContained) {
    Write-Host "Publishing self-contained application..." -ForegroundColor Yellow
    dotnet publish --configuration $Configuration --output "bin\$Configuration\net8.0-windows\publish" --self-contained true --runtime win-x64 --verbosity minimal
} else {
    Write-Host "Publishing framework-dependent application..." -ForegroundColor Yellow
    dotnet publish --configuration $Configuration --output "bin\$Configuration\net8.0-windows\publish" --self-contained false --verbosity minimal
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}

# Copy icon to publish directory
if (Test-Path "icon.ico") {
    Copy-Item "icon.ico" "bin\$Configuration\net8.0-windows\publish\icon.ico" -Force
    Write-Host "Icon copied to publish directory" -ForegroundColor Green
}

Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "Published files location: bin\$Configuration\net8.0-windows\publish" -ForegroundColor Cyan

# Calculate published app size
$publishDir = "bin\$Configuration\net8.0-windows\publish"
if (Test-Path $publishDir) {
    $totalSize = (Get-ChildItem $publishDir -Recurse | Measure-Object -Property Length -Sum).Sum
    $sizeInMB = [math]::Round($totalSize / 1MB, 2)
    Write-Host "Published application size: $sizeInMB MB" -ForegroundColor Cyan
}

# Create installer if requested
if ($CreateInstaller) {
    Write-Host "Creating installer..." -ForegroundColor Yellow
    
    # Check if Inno Setup is installed
    $innoSetupPath = ""
    $possiblePaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 5\ISCC.exe"
    )
    
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $innoSetupPath = $path
            break
        }
    }
    
    if ($innoSetupPath -eq "") {
        Write-Host "Inno Setup not found. Please install Inno Setup from https://jrsoftware.org/isinfo.php" -ForegroundColor Red
        Write-Host "After installing Inno Setup, you can create the installer by running:" -ForegroundColor Yellow
        Write-Host "ISCC.exe BlueshotInstaller.iss" -ForegroundColor Yellow
        
        # Create a simple ZIP package as alternative
        Write-Host "Creating ZIP package as alternative..." -ForegroundColor Yellow
        $zipPath = "installer\Blueshot-Portable-v1.0.0.zip"
        New-Item -ItemType Directory -Force -Path "installer" | Out-Null
        
        if (Get-Command "Compress-Archive" -ErrorAction SilentlyContinue) {
            Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force
            Write-Host "Portable ZIP package created: $zipPath" -ForegroundColor Green
        }
    } else {
        Write-Host "Found Inno Setup at: $innoSetupPath" -ForegroundColor Green
        & $innoSetupPath "BlueshotInstaller.iss"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Installer created successfully!" -ForegroundColor Green
            $installerPath = "installer\Blueshot-Setup-v1.0.0.exe"
            if (Test-Path $installerPath) {
                $installerSize = (Get-Item $installerPath).Length
                $installerSizeInMB = [math]::Round($installerSize / 1MB, 2)
                Write-Host "Installer location: $installerPath ($installerSizeInMB MB)" -ForegroundColor Cyan
            }
        } else {
            Write-Host "Installer creation failed!" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "=== STANDALONE INSTALLER READY ===" -ForegroundColor Green
Write-Host ""
if ($SelfContained) {
    Write-Host "✓ Self-contained deployment (no .NET runtime required)" -ForegroundColor Green
} else {
    Write-Host "⚠ Framework-dependent deployment (.NET 8 runtime required)" -ForegroundColor Yellow
}
Write-Host "✓ Global Print Screen hotkey with fallback (Ctrl+Shift+F12)" -ForegroundColor Green
Write-Host "✓ System tray integration" -ForegroundColor Green
Write-Host "✓ Professional installer with auto-startup option" -ForegroundColor Green
Write-Host "✓ Screenshot gallery with annotation tools" -ForegroundColor Green
Write-Host ""
Write-Host "Installation Methods:" -ForegroundColor Cyan
Write-Host "1. Run installer\Blueshot-Setup-v1.0.0.exe (Recommended)" -ForegroundColor White
Write-Host "2. Extract portable ZIP and run Blueshot.exe" -ForegroundColor White
Write-Host ""
Write-Host "The application will automatically:" -ForegroundColor Green
Write-Host "• Register screenshot hotkeys (Print Screen or Ctrl+Shift+F12)" -ForegroundColor White
Write-Host "• Add system tray icon for quick access" -ForegroundColor White
Write-Host "• Start with Windows if selected during installation" -ForegroundColor White
