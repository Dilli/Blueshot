# Changelog

All notable changes to Blueshot will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-08-01

### Added
- 🎯 **Global Screenshot Capture**
  - Print Screen hotkey with automatic fallback to Ctrl+Shift+F12
  - Smart conflict detection with OneDrive, Teams, and other applications
  - System tray integration with right-click context menu
  - Double-click tray icon for instant capture

- 🎨 **Professional Annotation Tools**
  - Select tool for moving and resizing annotations
  - Highlight tool with transparency support
  - Rectangle, Line, and Arrow drawing tools
  - Text annotations with font customization
  - Auto-incrementing counter numbers
  - Color picker with full color spectrum
  - Adjustable line thickness (1-15px)
  - Crop tool with visual preview

- 🖼️ **Screenshot Gallery System**
  - Multi-screenshot management in single window
  - Thumbnail navigation panel
  - Previous/Next navigation buttons
  - Screenshot counter display
  - Individual annotation sets per screenshot

- 💾 **Export and Sharing**
  - Copy to clipboard with annotations included
  - Quick save to desktop (Ctrl+S)
  - Save As dialog with custom location (Ctrl+Shift+S)
  - PNG, JPG, BMP format support
  - High-quality image export

- 🔧 **System Integration**
  - System tray minimization
  - Professional custom icon
  - Windows startup integration (optional)
  - Auto-startup configuration
  - Registry-based startup management

- ⌨️ **Keyboard Shortcuts**
  - Global hotkeys: Print Screen, Ctrl+Shift+F12
  - Preview window: Ctrl+S, Ctrl+Shift+S, Ctrl+C, Ctrl+0, Ctrl+F, Esc
  - Zoom controls: Ctrl++, Ctrl+-
  - Navigation: Arrow keys, Delete key
  - Tool shortcuts: S, H, R, L, A, T, C, X

- 🏗️ **Self-Contained Deployment**
  - .NET 8 runtime included (160MB total)
  - No external dependencies required
  - Portable ZIP package (67MB compressed)
  - Professional Windows installer with Inno Setup
  - Simple installation scripts (Install.bat, Start Blueshot.bat)

- 📖 **Documentation and Distribution**
  - Comprehensive README with usage instructions
  - Professional installer configuration
  - User-friendly documentation
  - Troubleshooting guides
  - Multiple installation options

### Technical Implementation
- **Framework**: .NET 8 with Windows Forms
- **Architecture**: Event-driven with clean separation of concerns
- **UI Design**: Professional icons and modern Windows 11 styling
- **Memory Management**: Proper disposal of graphics resources
- **Error Handling**: Comprehensive exception handling and user feedback
- **Performance**: Optimized for low memory usage and fast startup

### Build System
- PowerShell build automation (build-standalone.ps1)
- Batch file alternatives (build-standalone.bat)
- Inno Setup installer configuration
- Self-contained deployment with runtime
- Automated ZIP package creation

### Security and Compatibility
- Windows 10 version 1809+ compatibility
- 64-bit architecture support
- Global hotkey registration with proper cleanup
- System tray integration following Windows guidelines
- Administrator privileges not required for basic operation

### Known Limitations
- Windows-only (cross-platform support planned for v2.0)
- Single monitor primary support (multi-monitor in development)
- No animated capture (GIF support planned)
- Limited to bitmap-based annotations

---

## Future Releases

### [2.0.0] - Planned
- Cross-platform support (Linux, macOS)
- Multi-monitor enhanced support
- Animated GIF capture
- Cloud storage integration
- Plugin system architecture

### [1.1.0] - Planned
- OCR text extraction from screenshots
- Enhanced annotation tools (blur, pixelate)
- Batch export functionality
- Command-line interface
- Performance optimizations

---

## Release Notes Format

Each release includes:
- **Added**: New features
- **Changed**: Changes in existing functionality
- **Deprecated**: Soon-to-be removed features
- **Removed**: Now removed features
- **Fixed**: Bug fixes
- **Security**: Vulnerability fixes

---

**Note**: This changelog follows the [Keep a Changelog](https://keepachangelog.com/) format and includes all changes since the initial release. For detailed technical changes, see the Git commit history.
