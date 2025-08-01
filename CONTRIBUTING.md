# Contributing to Blueshot

Thank you for your interest in contributing to Blueshot! This document provides guidelines and information for contributors.

## 🎯 Ways to Contribute

### 🐛 Bug Reports
- Search existing issues before creating new ones
- Use the bug report template
- Include system information and steps to reproduce
- Attach screenshots if applicable

### ✨ Feature Requests
- Check if the feature has been requested before
- Use the feature request template
- Provide clear use cases and benefits
- Consider backward compatibility

### 💻 Code Contributions
- Fork the repository
- Create a feature branch
- Follow coding standards
- Add tests for new functionality
- Update documentation as needed

### 📖 Documentation
- Improve README clarity
- Add code comments
- Update user guides
- Fix typos and grammar

## 🛠️ Development Setup

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- Git
- Windows 10/11 for testing

### Getting Started
1. **Fork and Clone**
   ```bash
   git clone https://github.com/yourusername/blueshot.git
   cd blueshot
   ```

2. **Build the Project**
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Run Tests**
   ```bash
   dotnet test
   ```

4. **Run the Application**
   ```bash
   dotnet run --project Blueshot.csproj
   ```

## 📝 Coding Standards

### C# Style Guidelines
- Use PascalCase for public members
- Use camelCase for private fields and local variables
- Use meaningful names for variables and methods
- Follow Microsoft's C# coding conventions

### Code Organization
- Keep methods focused and small
- Use XML documentation for public APIs
- Group related functionality into classes
- Maintain separation of concerns

### Example Code Style
```csharp
/// <summary>
/// Captures a screenshot of the specified region.
/// </summary>
/// <param name="region">The region to capture</param>
/// <returns>The captured screenshot as a Bitmap</returns>
public Bitmap CaptureRegion(Rectangle region)
{
    // Implementation here
}

private readonly ScreenCaptureManager captureManager;
private bool isCapturing = false;
```

## 🧪 Testing Guidelines

### Unit Tests
- Test public methods and edge cases
- Use descriptive test names
- Follow Arrange-Act-Assert pattern
- Mock external dependencies

### Integration Tests
- Test complete workflows
- Verify UI interactions
- Test with different screen configurations
- Validate hotkey functionality

### Manual Testing
- Test on Windows 10 and 11
- Verify with multiple monitors
- Test hotkey conflicts
- Check memory usage with multiple screenshots

## 📋 Pull Request Process

### Before Submitting
1. **Update Documentation**
   - Update README if adding features
   - Add XML comments for new public APIs
   - Update changelog if applicable

2. **Test Thoroughly**
   - Run all automated tests
   - Test manually on target platforms
   - Verify no regressions

3. **Code Review Checklist**
   - Code follows style guidelines
   - No unnecessary changes
   - Commit messages are clear
   - Branch is up to date with main

### Pull Request Template
```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Documentation update
- [ ] Performance improvement

## Testing
- [ ] Unit tests pass
- [ ] Manual testing completed
- [ ] No regressions found

## Screenshots
(If applicable)
```

## 🏗️ Project Structure

### Core Components
- **MainForm.cs** - Main application window and system tray
- **PreviewForm.cs** - Screenshot preview and annotation UI
- **ScreenCaptureManager.cs** - Screen capture logic
- **GlobalHotkey.cs** - Global hotkey registration
- **RegionSelectorOverlay.cs** - Region selection overlay

### Key Patterns
- **Event-driven architecture** for UI interactions
- **Factory pattern** for annotation tools
- **Observer pattern** for hotkey notifications
- **Strategy pattern** for different capture modes

## 🐛 Debugging Tips

### Common Issues
- **Hotkey conflicts** - Check Windows Event Viewer
- **UI threading** - Use Invoke for cross-thread calls
- **Memory leaks** - Dispose of Bitmap objects properly
- **DPI awareness** - Test on high-DPI displays

### Debugging Tools
- Visual Studio debugger
- Performance profiler for memory issues
- Process Monitor for file/registry access
- Windows Event Viewer for system events

## 📚 Resources

### Documentation
- [.NET 8 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Windows Forms Guide](https://docs.microsoft.com/en-us/dotnet/desktop/winforms/)
- [Win32 API Reference](https://docs.microsoft.com/en-us/windows/win32/api/)

### Tools
- [Visual Studio Community](https://visualstudio.microsoft.com/vs/community/)
- [Git for Windows](https://git-scm.com/download/win)
- [Inno Setup](https://jrsoftware.org/isinfo.php)

## 🤝 Code of Conduct

### Our Standards
- Be respectful and inclusive
- Focus on constructive feedback
- Help newcomers learn
- Assume good intentions

### Unacceptable Behavior
- Harassment or discrimination
- Offensive comments or personal attacks
- Publishing others' private information
- Spam or self-promotion

## 📞 Getting Help

### Communication Channels
- **GitHub Issues** - Bug reports and feature requests
- **GitHub Discussions** - General questions and ideas
- **Code Reviews** - Technical discussions on PRs

### Response Times
- Issues: Within 48 hours
- Pull requests: Within 72 hours
- Security issues: Within 24 hours

## 🎉 Recognition

Contributors will be:
- Added to the CONTRIBUTORS.md file
- Mentioned in release notes
- Invited to join the core team (for significant contributions)

Thank you for helping make Blueshot better! 🚀
