# Blueshot Development Guidelines

This project is a C# WinForms application that implements screen capture functionality inspired by Greenshot. Here are the key guidelines for working with this codebase:

## Project Structure

- **Blueshot.csproj**: .NET 8 WinForms project targeting Windows
- **MainForm.cs**: Primary application window with modern UI
- **RegionSelectorOverlay.cs**: Fullscreen overlay for interactive region selection
- **ScreenCaptureManager.cs**: Core screen capture functionality
- **Program.cs**: Application entry point

## Architecture Principles

### Separation of Concerns
- UI logic is contained in Form classes
- Screen capture logic is isolated in ScreenCaptureManager
- Event-driven communication between components

### User Experience Focus
- Greenshot-inspired overlay system with real-time feedback
- Modern Windows 11 flat design principles
- Responsive UI with proper error handling
- System tray integration for quick access

### Technical Implementation
- Native Windows API integration for reliable screen capture
- Double-buffered drawing for smooth overlay rendering
- Event-driven architecture for mouse/keyboard interaction
- Proper resource disposal for images and graphics objects

## Key Components

### MainForm
- Entry point for user interaction
- Handles application lifecycle and tray integration
- Coordinates between UI and capture functionality
- Implements modern flat button styling

### RegionSelectorOverlay
- Fullscreen transparent form for region selection
- Captures background screenshot for context
- Implements real-time selection rectangle rendering
- Handles mouse drag operations and keyboard shortcuts

### ScreenCaptureManager
- Provides multiple capture modes (region, window, fullscreen)
- Handles Windows API calls for screen capture
- Supports multi-monitor configurations
- Includes error handling and validation

## Development Guidelines

### Code Style
- Use modern C# features (properties, using statements, etc.)
- Follow Microsoft naming conventions
- Include proper error handling and user feedback
- Implement IDisposable for graphics resources

### UI/UX Principles
- Maintain consistency with Windows design language
- Provide clear visual feedback for all operations
- Handle edge cases gracefully (escape key, invalid regions)
- Ensure accessibility and keyboard navigation

### Performance Considerations
- Use double buffering for smooth rendering
- Dispose graphics objects properly
- Minimize memory allocations during capture
- Cache background images when appropriate

## Common Tasks

### Adding New Capture Modes
1. Extend CaptureType enum in ScreenCaptureManager
2. Implement capture logic in ScreenCaptureManager
3. Add UI controls in MainForm if needed
4. Update RegionSelectorOverlay if overlay changes required

### Enhancing the Overlay
1. Modify OnPaint method in RegionSelectorOverlay
2. Add new mouse/keyboard handlers as needed
3. Update visual feedback in DrawSelectionInfo
4. Test across multiple monitor configurations

### Adding Settings
1. Create new settings form or panel
2. Implement CaptureSettings extensions
3. Add persistence layer for user preferences
4. Update MainForm to integrate settings UI

## Testing Considerations

- Test on multiple monitor setups
- Verify capture accuracy across different DPI settings
- Test overlay rendering performance
- Validate file save operations and formats
- Check memory usage during extended use

## Greenshot Inspiration Elements

This project draws inspiration from Greenshot's excellent UX:
- Fullscreen overlay with darkened background
- Real-time selection rectangle with size display
- Smooth mouse interaction model
- Clear visual feedback and escape handling

When extending the application, maintain these UX principles that make screen capture tools intuitive and efficient.
