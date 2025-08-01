# Complete Standalone Installer Build Script for Blueshot
# This script creates a fully self-contained installer that works on any Windows machine

param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    
    [switch]$SkipBuild,
    [switch]$CleanFirst
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "   BLUESHOT STANDALONE INSTALLER BUILDER" -ForegroundColor Cyan  
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Function to check if a command exists
function Test-Command($cmdname) {
    return [bool](Get-Command -Name $cmdname -ErrorAction SilentlyContinue)
}

# Function to get folder size
function Get-FolderSize($Path) {
    if (Test-Path $Path) {
        $totalSize = (Get-ChildItem $Path -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
        return [math]::Round($totalSize / 1MB, 2)
    }
    return 0
}

try {
    # Clean previous builds if requested
    if ($CleanFirst) {
        Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
        $foldersToClean = @("bin", "obj", "installer")
        foreach ($folder in $foldersToClean) {
            if (Test-Path $folder) {
                Remove-Item -Recurse -Force $folder
                Write-Host "   * Cleaned $folder" -ForegroundColor Gray
            }
        }
        Write-Host ""
    }

    # Check .NET SDK
    Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
    if (-not (Test-Command "dotnet")) {
        throw ".NET SDK not found. Please install .NET 8 SDK from https://dotnet.microsoft.com/download"
    }

    $dotnetVersion = dotnet --version
    Write-Host "   * Found .NET SDK: $dotnetVersion" -ForegroundColor Green
    Write-Host ""

    # Build application if not skipped
    if (-not $SkipBuild) {
        Write-Host "Building Blueshot application..." -ForegroundColor Yellow
        dotnet build --configuration $Configuration --verbosity minimal --nologo

        if ($LASTEXITCODE -ne 0) {
            throw "Build failed with exit code $LASTEXITCODE"
        }
        Write-Host "   * Build completed successfully" -ForegroundColor Green
        Write-Host ""
    }

    # Publish self-contained application
    Write-Host "Publishing self-contained application..." -ForegroundColor Yellow
    $publishPath = "bin\$Configuration\net8.0-windows\publish"
    
    $publishArgs = @(
        "publish"
        "--configuration", $Configuration
        "--runtime", "win-x64"
        "--self-contained", "true"
        "--output", $publishPath
        "--verbosity", "minimal"
        "--nologo"
        "/p:PublishSingleFile=false"
        "/p:PublishTrimmed=false"
        "/p:IncludeNativeLibrariesForSelfExtract=true"
    )
    
    & dotnet @publishArgs

    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed with exit code $LASTEXITCODE"
    }

    # Copy additional files
    if (Test-Path "icon.ico") {
        Copy-Item "icon.ico" "$publishPath\icon.ico" -Force
        Write-Host "   * Icon file copied" -ForegroundColor Gray
    }

    if (Test-Path "README.md") {
        Copy-Item "README.md" "$publishPath\README.md" -Force
        Write-Host "   * README file copied" -ForegroundColor Gray
    }

    $publishSize = Get-FolderSize $publishPath
    Write-Host "   * Self-contained application published ($publishSize MB)" -ForegroundColor Green
    Write-Host ""

    # Create installer directory
    New-Item -ItemType Directory -Force -Path "installer" | Out-Null

    # Create portable ZIP package
    Write-Host "Creating portable ZIP package..." -ForegroundColor Yellow
    $zipPath = "installer\Blueshot-Portable-v1.0.0.zip"
    
    if (Test-Command "Compress-Archive") {
        Compress-Archive -Path "$publishPath\*" -DestinationPath $zipPath -Force
        Write-Host "   * Portable ZIP created: $zipPath" -ForegroundColor Green
    }

    # Check for Inno Setup and create installer
    Write-Host "Creating Windows installer..." -ForegroundColor Yellow
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
        Write-Host "   * Inno Setup not found" -ForegroundColor Yellow
        Write-Host "   Download from: https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
        Write-Host "   After installation, you can run: ISCC.exe BlueshotInstaller.iss" -ForegroundColor Yellow
    } else {
        Write-Host "   Found Inno Setup: $([System.IO.Path]::GetFileName($innoSetupPath))" -ForegroundColor Gray
        
        & $innoSetupPath "BlueshotInstaller.iss"
        
        if ($LASTEXITCODE -eq 0) {
            $installerPath = "installer\Blueshot-Setup-v1.0.0.exe"
            if (Test-Path $installerPath) {
                $installerSize = [math]::Round((Get-Item $installerPath).Length / 1MB, 2)
                Write-Host "   * Installer created: $installerPath ($installerSize MB)" -ForegroundColor Green
            }
        } else {
            Write-Host "   * Installer creation failed" -ForegroundColor Red
        }
    }

    Write-Host ""
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host "          BUILD COMPLETED SUCCESSFULLY" -ForegroundColor Green
    Write-Host "===============================================" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "Build Summary:" -ForegroundColor Cyan
    Write-Host "   * Configuration: $Configuration" -ForegroundColor White
    Write-Host "   * Runtime: Self-contained win-x64" -ForegroundColor White
    Write-Host "   * Published size: $publishSize MB" -ForegroundColor White
    Write-Host "   * Dependencies: None (includes .NET runtime)" -ForegroundColor White
    Write-Host ""
    
    Write-Host "Output Files:" -ForegroundColor Cyan
    Write-Host "   * Application: $publishPath" -ForegroundColor White
    if (Test-Path $zipPath) {
        Write-Host "   * Portable ZIP: $zipPath" -ForegroundColor White
    }
    if (Test-Path "installer\Blueshot-Setup-v1.0.0.exe") {
        Write-Host "   * Windows Installer: installer\Blueshot-Setup-v1.0.0.exe" -ForegroundColor White
    }
    Write-Host ""
    
    Write-Host "Distribution Options:" -ForegroundColor Cyan
    Write-Host "   1. Run installer\Blueshot-Setup-v1.0.0.exe (Recommended)" -ForegroundColor White
    Write-Host "      - Professional Windows installer" -ForegroundColor Gray
    Write-Host "      - Auto-startup option" -ForegroundColor Gray
    Write-Host "      - Start menu integration" -ForegroundColor Gray
    Write-Host "      - Uninstaller included" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   2. Extract and run from ZIP" -ForegroundColor White
    Write-Host "      - Portable version" -ForegroundColor Gray
    Write-Host "      - No installation required" -ForegroundColor Gray
    Write-Host "      - Run Blueshot.exe directly" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "Features Included:" -ForegroundColor Cyan
    Write-Host "   * Global Print Screen hotkey (with Ctrl+Shift+F12 fallback)" -ForegroundColor White
    Write-Host "   * Professional annotation tools" -ForegroundColor White
    Write-Host "   * Screenshot gallery and management" -ForegroundColor White
    Write-Host "   * System tray integration" -ForegroundColor White
    Write-Host "   * Custom icon and branding" -ForegroundColor White
    Write-Host "   * No external dependencies" -ForegroundColor White
    Write-Host ""

} catch {
    Write-Host ""
    Write-Host "BUILD FAILED" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    exit 1
}

Write-Host "Ready for distribution! Share the installer with users." -ForegroundColor Green
Write-Host ""
