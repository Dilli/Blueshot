# Wiki Setup Instructions

This document explains how to set up the Blueshot Wiki on GitHub using the prepared content.

## 📚 Wiki Content Ready

I've prepared comprehensive wiki content in the `wiki/` folder:

### Created Wiki Pages
- **`Home.md`** - Main wiki homepage with overview and navigation
- **`Installation-Guide.md`** - Complete installation instructions  
- **`Quick-Start-Tutorial.md`** - 5-minute getting started guide
- **`Keyboard-Shortcuts.md`** - Comprehensive shortcut reference

### Additional Pages to Create
The Home page references these additional pages that should be created:
- **Screenshot Capture** - Different capture methods
- **Annotation Tools** - Complete annotation guide
- **Gallery Management** - Multi-screenshot workflows
- **Export Options** - Saving and sharing
- **System Integration** - Tray, startup, hotkeys
- **Troubleshooting** - Common issues and solutions
- **Configuration** - Settings and customization
- **Performance Tips** - Optimization guide
- **Building from Source** - Developer setup
- **API Reference** - Technical documentation
- **Contributing Guide** - Contribution workflow
- **Release Process** - How releases are made

## 🚀 Setting Up GitHub Wiki

### Method 1: Manual Setup (Recommended)

1. **Enable Wiki on GitHub:**
   - Go to https://github.com/Dilli/Blueshot
   - Click "Settings" tab
   - Scroll down to "Features" section
   - Check "Wikis" to enable wiki feature

2. **Create Wiki Pages:**
   - Go to https://github.com/Dilli/Blueshot/wiki
   - Click "Create the first page"
   - Title: "Home"
   - Copy content from `wiki/Home.md`
   - Click "Save Page"

3. **Add Additional Pages:**
   - Click "New Page" for each additional page
   - Use the exact titles referenced in Home.md
   - Copy content from corresponding `.md` files

### Method 2: GitHub CLI (If Available)

```powershell
# Navigate to your repository
cd "C:\Users\djanarthanam\OneDrive - DXC Production\Workspace\Blueshot"

# Clone the wiki repository
git clone https://github.com/Dilli/Blueshot.wiki.git blueshot-wiki

# Copy wiki files
Copy-Item "wiki\*.md" "blueshot-wiki\"

# Rename files for GitHub wiki format
Rename-Item "blueshot-wiki\Home.md" "blueshot-wiki\Home.md"
Rename-Item "blueshot-wiki\Installation-Guide.md" "blueshot-wiki\Installation-Guide.md"
Rename-Item "blueshot-wiki\Quick-Start-Tutorial.md" "blueshot-wiki\Quick-Start-Tutorial.md" 
Rename-Item "blueshot-wiki\Keyboard-Shortcuts.md" "blueshot-wiki\Keyboard-Shortcuts.md"

# Commit and push
cd blueshot-wiki
git add .
git commit -m "Initial wiki setup with comprehensive documentation"
git push origin master
```

### Method 3: Direct File Upload

1. **Access Wiki:**
   - Go to https://github.com/Dilli/Blueshot/wiki

2. **For Each Page:**
   - Click "New Page"
   - Enter page title (exactly as referenced in Home.md)
   - Copy content from corresponding `.md` file in `wiki/` folder
   - Click "Save Page"

## 📖 Page Creation Order

Create pages in this order for best user experience:

### Essential Pages (Create First)
1. **Home** - Main landing page
2. **Installation Guide** - Users need this first
3. **Quick Start Tutorial** - Getting started guide
4. **Keyboard Shortcuts** - Essential reference

### User Guide Pages (Create Second)
5. **Screenshot Capture** - Different capture methods
6. **Annotation Tools** - Tool-by-tool guide
7. **Gallery Management** - Multi-screenshot workflows
8. **Export Options** - Saving and sharing options

### Advanced Pages (Create Third)
9. **System Integration** - Tray, startup, hotkeys
10. **Configuration** - Settings and customization
11. **Troubleshooting** - Common issues
12. **Performance Tips** - Optimization

### Developer Pages (Create Last)
13. **Building from Source** - Development setup
14. **API Reference** - Technical docs
15. **Contributing Guide** - Contribution process
16. **Release Process** - Release workflow

## 🎨 Wiki Formatting Guidelines

### Page Structure
```markdown
# Page Title

Brief introduction paragraph.

## 🎯 Section 1
Content with emoji headers for visual appeal.

### Subsection
- Use bullet points for lists
- **Bold** for emphasis
- `Code formatting` for technical terms

## 📚 Section 2
More content...

---

## Next Steps
Links to related pages using [[Wiki Link]] format.
```

### Wiki Link Format
- **Internal links:** `[[Page Title]]` - Links to other wiki pages
- **External links:** `[Link Text](URL)` - Links to external sites
- **Repository links:** `[File](https://github.com/Dilli/Blueshot/blob/main/filename)`

### Image Guidelines
- **Repository images:** Use raw GitHub URLs
- **Logo/icons:** `![Alt Text](https://github.com/Dilli/Blueshot/raw/main/icon.ico)`
- **Screenshots:** Upload to GitHub and link with full URLs
- **Badges:** Use shields.io badges for consistency

## 🔗 Navigation Setup

### Sidebar Navigation
GitHub wikis automatically generate a sidebar from page titles. Ensure consistent naming:

- Use title case: "Installation Guide" not "installation guide"
- Use hyphens in URLs: "Installation-Guide" 
- Keep titles concise but descriptive

### Footer Links
Add consistent footer to each page:
```markdown
---

## 📞 Quick Links
- 🏠 **[[Home]]** - Wiki home page
- 📦 **[Latest Release](https://github.com/Dilli/Blueshot/releases/latest)** - Download
- 🐛 **[Report Issues](https://github.com/Dilli/Blueshot/issues)** - Bug reports
- 💬 **[Discussions](https://github.com/Dilli/Blueshot/discussions)** - Community
```

## ✅ Wiki Content Checklist

### Home Page
- [ ] Welcome message and project overview
- [ ] Navigation to all major sections
- [ ] Latest release information
- [ ] Feature highlights
- [ ] Quick links to important pages

### Installation Guide
- [ ] Professional installer instructions
- [ ] Portable version instructions
- [ ] System requirements
- [ ] Troubleshooting installation issues
- [ ] Security considerations

### Quick Start Tutorial
- [ ] 5-minute getting started guide
- [ ] Basic screenshot capture
- [ ] Essential annotation tools
- [ ] Save and share workflow
- [ ] Common issues for beginners

### Keyboard Shortcuts
- [ ] Global hotkeys
- [ ] Preview window shortcuts
- [ ] Tool selection shortcuts
- [ ] Text editing shortcuts
- [ ] Mouse combinations

## 🔧 Maintenance and Updates

### Regular Updates
- **Release updates:** Update Home page with new release info
- **Feature additions:** Add documentation for new features
- **User feedback:** Address common questions in Troubleshooting
- **Link verification:** Check all links remain valid

### Content Standards
- **Clear headings:** Use descriptive section headers
- **Step-by-step:** Break complex tasks into numbered steps
- **Visual aids:** Include screenshots where helpful
- **Cross-references:** Link related pages together

## 🚀 Making the Wiki Live

Once you've created the wiki pages on GitHub:

1. **Verify navigation:** Check all [[Wiki Links]] work correctly
2. **Test on mobile:** Ensure wiki is mobile-friendly
3. **Update README:** Add link to wiki from main README.md
4. **Announce:** Let users know about the comprehensive documentation

### Linking from README
Add this section to your main README.md:
```markdown
## 📖 Documentation

**Complete documentation is available in our [Wiki](https://github.com/Dilli/Blueshot/wiki):**
- 📥 [Installation Guide](https://github.com/Dilli/Blueshot/wiki/Installation-Guide)
- 🚀 [Quick Start Tutorial](https://github.com/Dilli/Blueshot/wiki/Quick-Start-Tutorial) 
- ⌨️ [Keyboard Shortcuts](https://github.com/Dilli/Blueshot/wiki/Keyboard-Shortcuts)
- 🎨 [Annotation Tools](https://github.com/Dilli/Blueshot/wiki/Annotation-Tools)
```

## 🎉 Your Professional Wiki Awaits!

The wiki content is ready and comprehensive. Follow these steps to create a professional documentation site that will help users get the most out of Blueshot!

Need help with any step? The content is all prepared in the `wiki/` folder - just copy and paste into GitHub's wiki interface.
