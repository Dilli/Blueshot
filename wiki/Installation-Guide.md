# Installation Guide

Get Blueshot up and running on your Windows system with these comprehensive installation options.

## 🚀 Recommended: Professional Installer

### Download and Install
1. **Download the installer:**
   - Go to [Releases](https://github.com/Dilli/Blueshot/releases/latest)
   - Download `Blueshot-Setup-v0.1.0-beta.exe` (47.98 MB)

2. **Run the installer:**
   - Right-click the downloaded file → "Run as administrator" (recommended)
   - Or double-click to run with current permissions

3. **Follow the setup wizard:**
   - **Welcome screen** - Click "Next"
   - **License Agreement** - Read and accept the MIT license
   - **Installation Directory** - Choose install location (default: `C:\Program Files\Blueshot`)
   - **Additional Tasks** - Select your preferences:
     - ✅ Create desktop shortcut
     - ✅ Start Blueshot automatically when Windows starts
     - ✅ Associate common image file types with Blueshot
   - **Ready to Install** - Review settings and click "Install"

4. **Complete installation:**
   - Wait for files to be copied
   - Click "Finish" to complete and launch Blueshot

### Installer Features
- ✅ **One-click installation** with professional setup wizard
- ✅ **Desktop shortcut creation** for easy access
- ✅ **Start menu integration** with program group
- ✅ **Automatic startup option** to start with Windows
- ✅ **File type associations** for image files (optional)
- ✅ **Complete uninstaller** for clean removal

## 📁 Alternative: Portable Version

### Download and Extract
1. **Download the portable package:**
   - Go to [Releases](https://github.com/Dilli/Blueshot/releases/latest)
   - Download `Blueshot-Standalone-v0.1.0-beta.zip` (66.77 MB)

2. **Extract the archive:**
   - Right-click the ZIP file → "Extract All..."
   - Choose your desired location (e.g., `C:\Tools\Blueshot`)
   - Click "Extract"

3. **Run Blueshot:**
   - Navigate to the extracted folder
   - Double-click `Blueshot.exe` to launch

### Portable Advantages
- ✅ **No installation required** - run from any location
- ✅ **USB/Network drive compatible** - run from portable storage
- ✅ **Settings stored locally** - in application folder
- ✅ **No registry entries** - completely self-contained
- ✅ **Easy cleanup** - just delete the folder

## 🛠️ System Requirements

### Minimum Requirements
- **Operating System:** Windows 10 version 1809 or later (64-bit)
- **Architecture:** x64 (64-bit)
- **Memory:** 512 MB available RAM
- **Storage:** 200 MB free disk space
- **Display:** Any resolution (optimized for 1920x1080 and higher)

### Recommended Requirements
- **Operating System:** Windows 11 (latest updates)
- **Memory:** 1 GB available RAM
- **Storage:** 500 MB free disk space
- **Display:** 1920x1080 or higher resolution

### Dependencies
- ✅ **Self-contained deployment** - No .NET runtime installation required
- ✅ **No external dependencies** - All required libraries included
- ✅ **No additional software** - Works out of the box

## 🔧 Post-Installation Setup

### First Launch
1. **System tray icon** - Look for the Blueshot icon in your system tray (bottom-right corner)
2. **Global hotkey test** - Press `Print Screen` to test screenshot capture
3. **Permissions** - Allow through Windows Defender SmartScreen if prompted

### Configuration Options
- **Right-click system tray icon** → "Settings" to configure:
  - Hotkey preferences
  - Save locations
  - Image formats
  - Startup behavior

### Hotkey Setup
- **Primary hotkey:** `Print Screen` (automatically configured)
- **Fallback hotkey:** `Ctrl+Shift+F12` (if Print Screen conflicts)
- **Manual capture:** Double-click system tray icon

## ⚡ Quick Start Verification

### Test Your Installation
1. **Launch test:**
   - Press `Print Screen` key
   - You should see a region selection overlay

2. **Capture test:**
   - Click and drag to select a screen area
   - Preview window should open with annotation tools

3. **Save test:**
   - Press `Ctrl+S` to quick-save to desktop
   - Check for saved screenshot file

4. **System tray test:**
   - Look for Blueshot icon in system tray
   - Right-click for context menu options

## 🛡️ Security Considerations

### Windows Defender SmartScreen
- **First run warning:** Normal for unsigned applications
- **Action:** Click "More info" → "Run anyway"
- **Alternative:** Right-click installer → "Properties" → "Unblock"

### Antivirus Software
- **Global hotkey registration** may trigger security warnings
- **Action:** Add Blueshot to antivirus whitelist/exceptions
- **Common locations to whitelist:**
  - Installation folder: `C:\Program Files\Blueshot\`
  - Executable: `Blueshot.exe`

### Firewall
- **No network access required** for basic functionality
- **Future cloud features** may require internet access
- **Current version:** Fully offline capable

## 🔄 Updating Blueshot

### Automatic Updates
- Currently: **Manual update process**
- **Check for updates:** Visit [releases page](https://github.com/Dilli/Blueshot/releases)
- **Future versions:** Automatic update notifications planned

### Manual Update Process
1. **Download new version** from releases page
2. **Professional installer:** Run new installer (will update existing installation)
3. **Portable version:** Extract to same folder (overwrite existing files)
4. **Settings preserved:** Your configuration will be maintained

## 🗑️ Uninstalling Blueshot

### Professional Installation
1. **Windows Settings method:**
   - Open Windows Settings (`Win + I`)
   - Go to "Apps" → "Apps & features"
   - Search for "Blueshot"
   - Click "Uninstall"

2. **Control Panel method:**
   - Open Control Panel → "Programs and Features"
   - Find "Blueshot" in the list
   - Click "Uninstall"

3. **Start Menu method:**
   - Right-click Start button → "Apps and Features"
   - Search for "Blueshot"
   - Click three dots → "Uninstall"

### Portable Installation
1. **Simple deletion:**
   - Close Blueshot application
   - Delete the Blueshot folder
   - Remove any shortcuts you created

2. **Clean removal:**
   - Stop Blueshot from system tray
   - Remove from Windows startup (if added manually)
   - Delete application folder

### Clean Uninstall Verification
- ✅ **No files remaining** in installation directory
- ✅ **No system tray icon** after restart
- ✅ **Print Screen** returns to default Windows behavior
- ✅ **No startup entries** in Task Manager

## 🆘 Installation Troubleshooting

### Common Issues

#### Installer Won't Run
- **Cause:** Windows SmartScreen or antivirus blocking
- **Solution:** Right-click → "Run as administrator" or add to antivirus exceptions

#### Print Screen Not Working
- **Cause:** Conflict with OneDrive, Teams, or other screenshot tools
- **Solution:** Blueshot automatically uses `Ctrl+Shift+F12` as fallback

#### Application Won't Start
- **Cause:** Missing dependencies or corrupted installation
- **Solution:** 
  1. Restart Windows
  2. Reinstall Blueshot
  3. Check Windows Event Viewer for error details

#### Permission Errors
- **Cause:** Insufficient privileges for installation location
- **Solution:** Run installer as administrator or choose user directory

### Getting Additional Help
- 📖 **Check [[Troubleshooting]]** page for more solutions
- 🐛 **[Report installation issues](https://github.com/Dilli/Blueshot/issues/new)** with detailed error messages
- 💬 **[Ask for help](https://github.com/Dilli/Blueshot/discussions)** in community discussions

---

## Next Steps

After successful installation:
- 📚 **[[Quick Start Tutorial]]** - Learn the basics in 5 minutes
- ⌨️ **[[Keyboard Shortcuts]]** - Master the hotkeys  
- 🎨 **[[Annotation Tools]]** - Explore all annotation features
- ⚙️ **[[Configuration]]** - Customize Blueshot to your preferences
