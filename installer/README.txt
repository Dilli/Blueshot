# Blueshot - Professional Screenshot Tool

**Version:** 1.0.0  
**Built:** August 2025  
**Platform:** Windows 10/11 (64-bit)

## 🎯 Quick Start

### Option 1: Portable Version (Recommended for Testing)
1. Extract `Blueshot-Portable-v1.0.0.zip` to any folder
2. Run `Blueshot.exe`
3. The app will appear in your system tray
4. Start taking screenshots!

### Option 2: Full Installation
1. Download and install Inno Setup: https://jrsoftware.org/isinfo.php
2. Run: `ISCC.exe BlueshotInstaller.iss` in the project directory
3. Run the generated installer: `installer\Blueshot-Setup-v1.0.0.exe`

## 🔥 Features

### Screenshot Capture
- **Global Hotkeys:** Press `Print Screen` (or `Ctrl+Shift+F12` if unavailable)
- **System Tray Access:** Right-click tray icon → Capture Region
- **Instant Capture:** Double-click system tray icon

### Professional Annotation Tools
- **Drawing Tools:** Rectangle, Line, Arrow, Highlight
- **Text Annotations:** Add custom text with font options
- **Counter Numbers:** Auto-incrementing numbered annotations
- **Color Selection:** Choose from any color for annotations
- **Thickness Control:** Adjustable line thickness (1-15px)

### Gallery & Management
- **Multi-Screenshot Gallery:** Keep multiple screenshots open
- **Thumbnail Navigation:** Quick preview and switching
- **Professional UI:** Clean, modern interface
- **Keyboard Shortcuts:** Full keyboard support

### Export & Sharing
- **Copy to Clipboard:** Instant sharing with annotations included
- **Save Options:** Quick save to desktop or custom location
- **Format Support:** PNG, JPG, BMP formats

### System Integration
- **System Tray:** Minimizes to tray, stays out of the way
- **Auto-startup:** Optional startup with Windows
- **Hotkey Conflicts:** Automatic fallback if Print Screen is taken
- **No Dependencies:** Self-contained, works on any Windows PC

## 🎮 Controls & Shortcuts

### Global Hotkeys
- `Print Screen` - Capture screenshot
- `Ctrl+Shift+F12` - Fallback capture hotkey

### In Preview Window
- `Ctrl+S` - Quick save to desktop
- `Ctrl+Shift+S` - Save as (choose location)
- `Ctrl+C` - Copy to clipboard with annotations
- `Ctrl++` - Zoom in
- `Ctrl+-` - Zoom out
- `Ctrl+0` - Actual size
- `Ctrl+F` - Fit to window
- `Esc` - Close preview window
- `Delete` - Remove selected annotation
- `Arrow Keys` - Navigate between screenshots

### Annotation Tools
- `S` - Select tool
- `H` - Highlight tool
- `R` - Rectangle tool
- `L` - Line tool
- `A` - Arrow tool
- `T` - Text tool
- `C` - Counter tool
- `X` - Crop tool

## 🔧 System Requirements

- **OS:** Windows 10 version 1809 or later (64-bit)
- **RAM:** 512 MB available
- **Disk:** 200 MB free space
- **Dependencies:** None (self-contained)

## 🚀 Installation Details

### Self-Contained Deployment
This application includes the .NET 8 runtime, so no additional software is required. The total package size is approximately 160 MB but provides complete independence from system dependencies.

### Hotkey Registration
The application automatically tries to register these hotkeys in order:
1. `Print Screen` (most common)
2. `Ctrl+Shift+F12` (fallback)

If both fail, you can still capture screenshots via the system tray icon.

### Common Hotkey Conflicts
If hotkey registration fails, it's usually because these applications are using Print Screen:
- OneDrive (Microsoft cloud storage)
- Microsoft Teams
- Other screenshot tools
- Gaming software (Discord, Steam, etc.)

The application will automatically use the fallback hotkey or inform you to use the system tray.

## 📁 File Structure

```
Blueshot/
├── Blueshot.exe           # Main application
├── icon.ico               # Application icon
├── README.md              # This documentation
├── [System Files]         # .NET runtime and dependencies
└── [Language Packs]       # Localization resources
```

## 🎛️ Usage Tips

1. **First Run:** The app will appear in your system tray (bottom-right corner)
2. **Taking Screenshots:** Use Print Screen or the tray icon
3. **Annotations:** Use the left toolbar for drawing tools
4. **Gallery:** Keep multiple screenshots open and switch between them
5. **Sharing:** Copy to clipboard includes all your annotations
6. **Persistence:** The app remembers your tool selections and settings

## 🛠️ Troubleshooting

### Hotkey Not Working
- Check if OneDrive or Teams is running (common conflicts)
- Try the fallback hotkey: `Ctrl+Shift+F12`
- Use the system tray icon as alternative

### App Not Visible
- Check system tray (bottom-right corner, may be hidden)
- Look for the Blueshot icon
- Right-click the icon to access options

### Performance Issues
- Close unused screenshot windows
- Restart the application if memory usage is high
- Ensure Windows is updated

## 📞 Support

For issues or questions:
- Check the system tray for status messages
- Review hotkey conflicts with other software
- Restart the application if needed

---

**Blueshot v1.0.0** - Professional Screenshot Tool with Annotation Capabilities  
*Self-contained Windows application - No additional software required*
