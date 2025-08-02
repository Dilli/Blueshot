# Blueshot Professional Installer Builder
# Version: 0.1.0-beta

param(
    [switch]$SkipBuild,
    [switch]$TestInstaller,
    [string]$Configuration = "Release"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building Blueshot v0.1.0-beta Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Function to check if a command exists
function Test-Command($cmdname) {
    return [bool](Get-Command -Name $cmdname -ErrorAction SilentlyContinue)
}

# Check for required tools
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check for .NET SDK
if (-not (Test-Command "dotnet")) {
    Write-Host "ERROR: .NET SDK not found. Please install .NET 8.0 SDK." -ForegroundColor Red
    exit 1
}

# Check for Inno Setup
$InnoSetupPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
)

$InnoSetupPath = $null
foreach ($path in $InnoSetupPaths) {
    if (Test-Path $path) {
        $InnoSetupPath = $path
        break
    }
}

if (-not $InnoSetupPath) {
    Write-Host "ERROR: Inno Setup 6 not found!" -ForegroundColor Red
    Write-Host "Please install Inno Setup 6 from: https://jrsoftware.org/isdl.php" -ForegroundColor Red
    exit 1
}

Write-Host "✓ .NET SDK found" -ForegroundColor Green
Write-Host "✓ Inno Setup found at: $InnoSetupPath" -ForegroundColor Green
Write-Host ""

# Create installer directory
if (-not (Test-Path "installer")) {
    New-Item -ItemType Directory -Path "installer" | Out-Null
    Write-Host "Created installer directory" -ForegroundColor Yellow
}

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
Get-ChildItem "installer" -Filter "*.exe" | Remove-Item -Force -ErrorAction SilentlyContinue
Get-ChildItem "installer" -Filter "*.tmp" | Remove-Item -Force -ErrorAction SilentlyContinue

if (-not $SkipBuild) {
    # Build the release version
    Write-Host ""
    Write-Host "Building release version..." -ForegroundColor Yellow
    
    $buildArgs = @(
        "publish"
        "-c", $Configuration
        "--self-contained", "true"
        "-r", "win-x64"
        "-o", "./bin/Release/publish"
        "--verbosity", "minimal"
    )
    
    $buildProcess = Start-Process -FilePath "dotnet" -ArgumentList $buildArgs -Wait -PassThru -NoNewWindow
    
    if ($buildProcess.ExitCode -ne 0) {
        Write-Host "ERROR: Failed to build release version (Exit code: $($buildProcess.ExitCode))" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✓ Release build completed successfully" -ForegroundColor Green
} else {
    Write-Host "Skipping build step..." -ForegroundColor Yellow
}

# Verify required files exist
Write-Host ""
Write-Host "Verifying build artifacts..." -ForegroundColor Yellow

$requiredFiles = @(
    "bin/Release/publish/Blueshot.exe",
    "icon.ico",
    "README.md",
    "LICENSE"
)

$missingFiles = @()
foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Host "ERROR: Missing required files:" -ForegroundColor Red
    $missingFiles | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "✓ All required files found" -ForegroundColor Green

# Build the installer
Write-Host ""
Write-Host "Building installer with Inno Setup..." -ForegroundColor Yellow

try {
    $installerProcess = Start-Process -FilePath $InnoSetupPath -ArgumentList "BlueshotInstaller.iss" -Wait -PassThru -NoNewWindow
    
    if ($installerProcess.ExitCode -ne 0) {
        Write-Host "ERROR: Failed to build installer (Exit code: $($installerProcess.ExitCode))" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "ERROR: Exception during installer build: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Verify installer was created
$installerPath = "installer\Blueshot-Setup-v0.1.0-beta.exe"
if (-not (Test-Path $installerPath)) {
    Write-Host "ERROR: Installer file not found at $installerPath" -ForegroundColor Red
    exit 1
}

# Get installer information
$installerInfo = Get-Item $installerPath
$installerSizeMB = [math]::Round($installerInfo.Length / 1MB, 2)

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Installer built successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "📁 Output: $installerPath" -ForegroundColor Cyan
Write-Host "📊 File size: $installerSizeMB MB" -ForegroundColor Cyan
Write-Host "📅 Created: $($installerInfo.LastWriteTime)" -ForegroundColor Cyan
Write-Host ""

# Digital signature information (if available)
try {
    $signature = Get-AuthenticodeSignature $installerPath -ErrorAction SilentlyContinue
    if ($signature.Status -eq "Valid") {
        Write-Host "🔒 Digital signature: Valid" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Digital signature: Not signed" -ForegroundColor Yellow
        Write-Host "   Consider code signing for production releases" -ForegroundColor Yellow
    }
} 
catch {
    Write-Host "⚠️  Could not verify digital signature" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎉 The installer is ready for distribution!" -ForegroundColor Green
Write-Host ""

# Test installer option
if ($TestInstaller) {
    Write-Host "Testing installer..." -ForegroundColor Yellow
    Start-Process -FilePath $installerPath -Wait
} else {
    $response = Read-Host "Do you want to test the installer now? (y/n)"
    if ($response -eq "y" -or $response -eq "Y") {
        Write-Host "Running installer..." -ForegroundColor Yellow
        Start-Process -FilePath $installerPath
    }
}

Write-Host ""
Write-Host "Installer build process completed successfully!" -ForegroundColor Green
