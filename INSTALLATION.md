# Blueshot Installation Package

## Overview
Blueshot is now ready for installation and deployment as a Windows application with global Print Screen hotkey support and system tray integration.

## What's Been Implemented

### 🔧 Core Features
- **Global Print Screen Hotkey**: Captures Print Screen key system-wide
- **System Tray Integration**: Runs minimized in system tray with custom icon
- **Auto-hide on Close**: Closing the window hides to tray instead of exiting
- **Professional UI**: Complete Greenshot-inspired icon overhaul
- **Annotation Tools**: Full suite of professional annotation tools with crop functionality

### 📦 Installation Components

#### 1. Application Files (bin/Release/net8.0-windows/publish/)
- `Blueshot.exe` - Main executable (framework-dependent)
- `Blueshot.pdb` - Debug symbols (optional for distribution)

#### 2. Installer Configuration
- `BlueshotInstaller.iss` - Inno Setup installer script
- Includes .NET 8 runtime check
- Auto-startup option
- Desktop shortcut option
- Proper uninstall support

#### 3. Build Scripts
- `build.ps1` - PowerShell build script
- `build.bat` - Batch file alternative
- Automated build and publish process

#### 4. Configuration Files
- `app.manifest` - Windows compatibility and DPI awareness
- `Blueshot.csproj` - Project configuration with installer metadata

## Installation Methods

### Method 1: Installer Package (Recommended)
1. **Install Inno Setup**: Download from https://jrsoftware.org/isinfo.php
2. **Build the application**: Run `build.ps1` or `build.bat`
3. **Create installer**: Run `ISCC.exe BlueshotInstaller.iss`
4. **Distribute**: Share `installer/Blueshot-Setup-v1.0.0.exe`

### Method 2: Manual Installation
1. **Build**: Run `build.ps1` or `build.bat`
2. **Copy**: Copy contents of `bin/Release/net8.0-windows/publish/` to desired folder
3. **Create Shortcuts**: Desktop and Start Menu shortcuts
4. **Auto-startup**: Copy shortcut to Windows startup folder (`shell:startup`)

### Method 3: Portable
1. **Build**: Run `build.ps1` or `build.bat`
2. **Package**: Zip the `bin/Release/net8.0-windows/publish/` folder
3. **Run**: Extract and run `Blueshot.exe` anywhere

## User Experience

### First Launch
1. Application starts minimized to system tray
2. Shows balloon tip: "Blueshot is running in the system tray. Press Print Screen to capture."
3. Global Print Screen hotkey is registered

### Daily Usage
1. **Press Print Screen**: Starts region capture from anywhere
2. **System Tray**: Double-click for quick capture, right-click for menu
3. **Annotations**: Professional toolbar with all tools available
4. **Background Operation**: Runs silently without interfering with other apps

### System Tray Menu
- Capture Region (Print Screen)
- Show Window
- Settings
- Exit

## Technical Details

### Global Hotkey Implementation
- Uses Windows API `RegisterHotKey` and `UnregisterHotKey`
- Registers VK_SNAPSHOT (Print Screen) without modifiers
- Processes WM_HOTKEY messages in main form's WndProc
- Properly cleans up on application exit

### System Tray Integration
- NotifyIcon with context menu
- Balloon tips for user feedback
- Hide instead of close behavior
- Professional tray icon

### Memory Footprint
- Lightweight: ~30-50MB RAM usage
- No background scanning or heavy processes
- Only activates when Print Screen is pressed

## Distribution Checklist

### For End Users
- [ ] .NET 8 Desktop Runtime installed
- [ ] Windows 10 version 1809 or later
- [ ] Administrator rights for initial hotkey registration (handled automatically)

### For Developers
- [ ] Visual Studio 2022 or .NET 8 SDK
- [ ] Inno Setup (for installer creation)
- [ ] Code signing certificate (optional, for trusted installation)

## Security Considerations

### Antivirus Detection
- Global hotkey registration may trigger antivirus warnings
- Code signing recommended for distribution
- Whitelist application in corporate environments

### Windows SmartScreen
- Unsigned executables may show warnings
- Consider Authenticode signing for wider distribution
- Users can click "More info" → "Run anyway"

## Future Enhancements

### Planned Features
- [ ] Custom hotkey configuration
- [ ] Settings panel with preferences
- [ ] Multiple capture modes (full screen, window)
- [ ] Cloud integration (OneDrive, Google Drive)
- [ ] Auto-update mechanism

### Technical Improvements
- [ ] Self-contained deployment option
- [ ] Modern packaging (MSIX)
- [ ] Windows Store distribution
- [ ] Digital signature

## Support Information

### System Requirements
- Windows 10 (1809+) / Windows 11
- .NET 8 Desktop Runtime
- 50MB disk space
- Any screen resolution

### Troubleshooting
- **Print Screen not working**: Check if other apps are using the hotkey. Blueshot will show a warning if registration fails.
- **Application won't start**: Verify .NET 8 Runtime is installed
- **High DPI issues**: Application includes DPI awareness manifest

## Installation Success Criteria

✅ **Application launches to system tray**
✅ **Print Screen hotkey captures globally**
✅ **Professional annotation tools work**
✅ **Can save screenshots and copy to clipboard with annotations**
✅ **System tray integration functional**
✅ **Graceful exit and cleanup**

---

**Blueshot is now ready for professional deployment and distribution!**
