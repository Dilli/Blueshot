# Blueshot - Professional Screenshot Tool

<div align="center">

![Blueshot Logo](icon.ico)

**A powerful, self-contained screenshot tool with professional annotation capabilities**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue.svg)](https://www.microsoft.com/windows/)
[![Downloads](https://img.shields.io/github/downloads/yourusername/blueshot/total.svg)](https://github.com/yourusername/blueshot/releases)

[Download Latest Release](https://github.com/yourusername/blueshot/releases) • [Documentation](#-features) • [Contributing](#-contributing) • [Support](#-support)

</div>

## 🎯 Overview

Blueshot is a modern, feature-rich screenshot tool designed for Windows users who need professional annotation capabilities. Built with .NET 8 and WinForms, it provides a clean, intuitive interface while maintaining powerful functionality.

### ✨ Key Highlights

- 🚀 **Self-contained** - No .NET installation required
- ⚡ **Global hotkeys** - Print Screen with intelligent fallback
- 🎨 **Professional annotations** - Complete drawing toolkit
- 🖼️ **Gallery system** - Manage multiple screenshots
- 🔧 **System integration** - System tray, auto-startup
- 📱 **Conflict resolution** - Handles OneDrive/Teams conflicts
- 💾 **Multiple formats** - PNG, JPG, BMP export

## 🔥 Features

### Screenshot Capture
- **Global Hotkeys:** `Print Screen` (primary) or `Ctrl+Shift+F12` (fallback)
- **System Tray Access:** Right-click tray icon → Capture Region
- **Instant Capture:** Double-click system tray icon
- **Region Selection:** Click and drag to select capture area

### Professional Annotation Tools
- **Drawing Tools:** Rectangle, Line, Arrow, Highlight
- **Text Annotations:** Custom text with font options
- **Counter Numbers:** Auto-incrementing numbered annotations
- **Color Picker:** Choose from any color
- **Thickness Control:** Adjustable line thickness (1-15px)
- **Selection Tool:** Move and resize annotations

### Gallery & Management
- **Multi-Screenshot Support:** Keep multiple screenshots open
- **Thumbnail Navigation:** Quick preview and switching
- **Professional UI:** Clean, modern interface design
- **Keyboard Shortcuts:** Full keyboard navigation support

### Export & Sharing
- **Copy to Clipboard:** Instant sharing with annotations included
- **Save Options:** Quick save to desktop or custom location
- **Format Support:** PNG, JPG, BMP formats
- **Quality Control:** Configurable export settings

### System Integration
- **System Tray:** Minimizes to tray, stays accessible
- **Auto-startup:** Optional startup with Windows
- **Hotkey Management:** Automatic conflict detection and fallback
- **Custom Branding:** Professional icon and UI design

## 📥 Download & Installation

### Option 1: Standalone Package (Recommended)
1. Download `Blueshot-Standalone-v1.0.0.zip` from [Releases](https://github.com/yourusername/blueshot/releases)
2. Extract to your desired folder
3. Run `Install.bat` for full installation with shortcuts
4. Or run `Blueshot.exe` directly for portable mode

### Option 2: Build from Source
```bash
git clone https://github.com/yourusername/blueshot.git
cd blueshot
dotnet build --configuration Release
dotnet run --project Blueshot.csproj
```

### Option 3: Create Installer
```bash
# Install Inno Setup from https://jrsoftware.org/isinfo.php
# Then run:
ISCC.exe BlueshotInstaller.iss
```

## 🎮 Usage

### Quick Start
1. Launch Blueshot (appears in system tray)
2. Press `Print Screen` to capture
3. Select the area you want to capture
4. Use annotation tools to mark up your screenshot
5. Save or copy to clipboard

### Keyboard Shortcuts

#### Global Hotkeys
- `Print Screen` - Capture screenshot
- `Ctrl+Shift+F12` - Fallback capture hotkey

#### In Preview Window
- `Ctrl+S` - Quick save to desktop
- `Ctrl+Shift+S` - Save as (choose location)
- `Ctrl+C` - Copy to clipboard with annotations
- `Ctrl++` / `Ctrl+-` - Zoom in/out
- `Ctrl+0` - Actual size
- `Ctrl+F` - Fit to window
- `Esc` - Close preview window
- `Delete` - Remove selected annotation
- `Arrow Keys` - Navigate between screenshots

#### Annotation Tools
- `S` - Select tool
- `H` - Highlight tool
- `R` - Rectangle tool
- `L` - Line tool
- `A` - Arrow tool
- `T` - Text tool
- `C` - Counter tool
- `X` - Crop tool

## 🛠️ Development

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- Windows 10/11 (for testing)

### Project Structure
```
Blueshot/
├── Blueshot.csproj           # Main project file
├── Program.cs                # Application entry point
├── MainForm.cs               # Main application window
├── PreviewForm.cs            # Screenshot preview and annotation
├── RegionSelectorOverlay.cs  # Screen capture overlay
├── ScreenCaptureManager.cs   # Screenshot capture logic
├── GlobalHotkey.cs           # Global hotkey management
├── SplashForm.cs             # Startup splash screen
├── icon.ico                  # Application icon
├── app.manifest              # Windows manifest
├── BlueshotInstaller.iss     # Inno Setup installer script
├── build-standalone.ps1      # Build automation script
└── installer/                # Distribution files
```

### Building

#### Debug Build
```bash
dotnet build --configuration Debug
```

#### Release Build
```bash
dotnet build --configuration Release
```

#### Self-Contained Build
```bash
.\build-standalone.ps1
```

### Contributing Guidelines

1. **Fork the repository**
2. **Create a feature branch:** `git checkout -b feature/amazing-feature`
3. **Commit your changes:** `git commit -m 'Add amazing feature'`
4. **Push to the branch:** `git push origin feature/amazing-feature`
5. **Open a Pull Request**

#### Code Style
- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public methods
- Include unit tests for new features

#### Testing
- Test on Windows 10 and 11
- Verify hotkey functionality with various applications
- Test annotation tools thoroughly
- Check memory usage with multiple screenshots

## 🔧 System Requirements

- **OS:** Windows 10 version 1809 or later (64-bit)
- **RAM:** 512 MB available memory
- **Disk:** 200 MB free space
- **Dependencies:** None (self-contained deployment)

## 🐛 Troubleshooting

### Common Issues

#### Hotkey Not Working
- **Cause:** OneDrive, Teams, or other apps using Print Screen
- **Solution:** App automatically uses `Ctrl+Shift+F12` fallback
- **Alternative:** Use system tray icon to capture

#### App Not Visible
- **Cause:** Running in system tray
- **Solution:** Look for Blueshot icon in system tray (bottom-right)
- **Tip:** Right-click tray icon for options

#### Performance Issues
- **Solution:** Close unused screenshot windows
- **Tip:** Restart application if memory usage is high
- **Check:** Ensure Windows is updated

### Getting Help
- Check [Issues](https://github.com/yourusername/blueshot/issues) for known problems
- Search [Discussions](https://github.com/yourusername/blueshot/discussions) for solutions
- Create a new issue with detailed description and screenshots

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [.NET 8](https://dotnet.microsoft.com/)
- UI framework: [Windows Forms](https://docs.microsoft.com/en-us/dotnet/desktop/winforms/)
- Installer created with [Inno Setup](https://jrsoftware.org/isinfo.php)
- Icons and design inspired by modern Windows applications

## 📊 Project Stats

![GitHub stars](https://img.shields.io/github/stars/yourusername/blueshot?style=social)
![GitHub forks](https://img.shields.io/github/forks/yourusername/blueshot?style=social)
![GitHub issues](https://img.shields.io/github/issues/yourusername/blueshot)
![GitHub pull requests](https://img.shields.io/github/issues-pr/yourusername/blueshot)

## 🚀 Roadmap

- [ ] Cross-platform support (Linux, macOS)
- [ ] Cloud storage integration
- [ ] Plugin system for extensions
- [ ] Animated GIF capture
- [ ] OCR text extraction
- [ ] Collaboration features

---

<div align="center">

**Made with ❤️ by the Blueshot Team**

[⭐ Star this project](https://github.com/yourusername/blueshot) if you find it useful!

</div>

## 🚀 Features

- **Global Hotkey Support**: Press **Print Screen** anywhere to start capturing
- **System Tray Integration**: Runs quietly in the background
- **Professional Annotation Tools**: 
  - Select and manipulate annotations
  - Highlight areas with transparency
  - Draw rectangles, lines, and arrows
  - Add text annotations
  - Numbered counter annotations
  - Crop functionality
- **Modern UI**: Professional Greenshot-inspired icons and interface
- **Auto-startup**: Optionally start with Windows
- **Multiple Export Options**: Save as PNG/JPEG, copy to clipboard
- **Undo/Redo**: Full annotation history support

## 📦 Installation

### Option 1: Using the Installer (Recommended)

1. Download `Blueshot-Setup-v1.0.0.exe` from the releases page
2. Run the installer as administrator
3. Follow the installation wizard
4. Choose whether to start with Windows
5. Blueshot will start automatically after installation

### Option 2: Manual Installation

1. **Build from Source**:
   ```cmd
   # Using PowerShell
   .\build.ps1
   
   # Using Batch file
   build.bat
   ```

2. **Copy Files**:
   - Copy contents of `bin\Release\net8.0-windows\publish\` to your desired folder
   - Create shortcuts as needed

3. **Auto-startup** (Optional):
   - Press `Win+R`, type `shell:startup`, press Enter
   - Copy Blueshot.exe shortcut to this folder

## 🎯 Usage

### Quick Start
1. **Launch**: Blueshot starts in the system tray
2. **Capture**: Press **Print Screen** anywhere to start region selection
3. **Annotate**: Use the toolbar to add annotations
4. **Save**: Use Ctrl+S to save or Ctrl+C to copy to clipboard

### Hotkeys
- **Print Screen**: Start region capture (global hotkey)
- **F1**: Start region capture (when main window is focused)
- **F2**: Open settings
- **Esc**: Hide main window
- **Ctrl+S**: Save screenshot
- **Ctrl+C**: Copy to clipboard
- **Ctrl+Z**: Undo last annotation

### Annotation Tools
- **Select Tool**: Move and resize existing annotations
- **Highlight**: Semi-transparent highlighting
- **Rectangle**: Draw rectangular outlines
- **Line**: Draw straight lines
- **Arrow**: Draw arrows with arrowheads
- **Text**: Add text annotations
- **Counter**: Add numbered markers
- **Crop**: Select area to crop (Press Enter to apply)

### System Tray
- **Double-click**: Start region capture
- **Right-click**: Access context menu
  - Capture Region
  - Show Window
  - Settings
  - Exit

## 🛠️ Building from Source

### Prerequisites
- .NET 8.0 SDK or later
- Windows 10/11
- Visual Studio 2022 or VS Code (optional)

### Build Commands
```cmd
# Restore dependencies
dotnet restore

# Build application
dotnet build --configuration Release

# Publish for distribution
dotnet publish --configuration Release --output publish --self-contained false
```

### Creating Installer
1. Install [Inno Setup](https://jrsoftware.org/isinfo.php)
2. Run: `ISCC.exe BlueshotInstaller.iss`
3. Installer will be created in `installer\` folder

## 🔧 System Requirements

- **OS**: Windows 10 version 1809 or later, Windows 11
- **Framework**: .NET 8.0 Runtime (Desktop)
- **Memory**: 100MB RAM minimum
- **Storage**: 50MB available space
- **Display**: Any resolution supported

## 📂 Project Structure

```
Blueshot/
├── BlueshotInstaller.iss    # Inno Setup installer script
├── GlobalHotkey.cs          # Global hotkey handling
├── MainForm.cs              # Main application window
├── PreviewForm.cs           # Screenshot preview and annotation
├── RegionSelectorOverlay.cs # Region selection overlay
├── ScreenCaptureManager.cs  # Screen capture functionality
├── SplashForm.cs            # Startup splash screen
├── app.manifest             # Windows compatibility manifest
├── build.ps1                # PowerShell build script
├── build.bat                # Batch build script
└── README.md                # This file
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by [Greenshot](https://getgreenshot.org/) for UI design principles
- Icons and design patterns follow modern Windows 11 guidelines
- Built with .NET 8 and Windows Forms

## 🐛 Known Issues

- Global hotkey may conflict with other applications using Print Screen
- Some antivirus software may flag the application due to global hotkey registration
- Windows Defender SmartScreen may show warnings for unsigned executables

## 📞 Support

- Create an issue on GitHub for bug reports
- Check existing issues before creating new ones
- Include system information and steps to reproduce

---

**Made with ❤️ for the Windows community**
