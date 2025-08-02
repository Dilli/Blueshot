# Blueshot v0.2.0-beta Release Notes

**Release Date**: August 2, 2025  
**Build Type**: Beta Release

## 🎉 What's New in v0.2.0-beta

### 🖼️ **Taskbar Icon Fix**
- **Fixed taskbar icon not displaying** when application is minimized
- Implemented `LoadApplicationIcon()` method with robust icon loading
- Added fallback icon support for better compatibility
- Enhanced `ShowInTaskbar` visibility during window state changes

### 🎨 **VS Code-Style UI Enhancements**
- **New RoundedToolStripRenderer** for consistent rounded borders (6-8px radius)
- **Enhanced RoundedButton** styling with professional corner radius
- **Rounded UI Components** throughout the interface for modern appearance
- **Consistent Design Language** matching VS Code's visual style

### 🎯 **Enhanced Annotation System**
- **Dual Color System**: Separate fill color and line color controls
- **Fill Color**: Semi-transparent for highlighting and backgrounds
- **Line Color**: Solid colors for borders, arrows, and text outlines
- **Improved Color Picker**: Dedicated buttons for fill and line colors
- **Professional Annotations**: Better visual separation between elements

### 🔧 **Technical Improvements**
- **Better Icon Management**: Robust icon loading with multiple fallback paths
- **Enhanced Error Handling**: Improved logging and exception management
- **Window State Management**: Better handling of minimize/maximize operations
- **Memory Management**: Optimized graphics resource disposal

### 🏗️ **Build System Updates**
- **Updated Installer**: Professional Inno Setup installer for v0.2.0-beta
- **Self-Contained Deployment**: No .NET runtime required for end users
- **Automated Build Scripts**: Enhanced build and installer generation
- **Version Synchronization**: Consistent versioning across all components

## 📋 **Previous Features (v0.1.0-beta)**
- Modern borderless window design with traffic light controls
- Enhanced carousel navigation with 200px height
- Global hotkey support (Print Screen integration)
- Advanced text annotation (point and region-based)
- Comprehensive logging and error handling
- Multi-screenshot management with thumbnail navigation
- Professional annotation tools (highlight, rectangle, line, arrow, text, counter)
- Crop functionality with visual selection overlay

## 🐛 **Bug Fixes**
- Fixed taskbar icon not appearing when application is minimized
- Resolved icon loading issues with different deployment scenarios
- Improved window state transitions and taskbar visibility
- Enhanced graphics rendering for rounded UI components

## 🔮 **Known Issues**
- This is a beta release - some features may need refinement
- File association features are optional during installation
- Large files in git repository trigger LFS warnings (non-critical)

## 📦 **Installation**
1. Download `Blueshot-Setup-v0.2.0-beta.exe`
2. Run the installer (no administrator rights required)
3. Choose your installation options:
   - Desktop shortcut
   - Startup integration
   - File associations (optional)
4. Launch Blueshot and start capturing!

## 🎮 **Usage**
- **Take Screenshot**: Press Print Screen (or Ctrl+Shift+F12)
- **Quick Access**: Right-click the system tray icon
- **Annotations**: Use the left toolbar for drawing tools
- **Colors**: Separate fill and line color controls in top toolbar
- **Navigation**: Enhanced carousel at bottom for multiple screenshots

## 🔄 **Upgrade from v0.1.0-beta**
The installer will automatically handle upgrading from previous versions. Your settings and preferences will be preserved.

## 🙏 **Feedback**
This is a beta release and we appreciate your feedback! Please report issues or suggestions at:
- GitHub Issues: https://github.com/Dilli/Blueshot/issues
- Email: [Your Email]

## 🏆 **Credits**
- **Developer**: Dilli
- **UI Framework**: .NET 8 Windows Forms
- **Installer**: Inno Setup 6
- **Icon Design**: Custom professional icon set

---

**Thank you for testing Blueshot v0.2.0-beta!** 🚀

Your feedback helps make Blueshot better for everyone. Enjoy the new VS Code-style interface and enhanced annotation capabilities!
