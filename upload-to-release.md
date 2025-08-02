# Upload Installer to GitHub Release

## Manual Upload Instructions

1. **Navigate to your GitHub repository:**
   - Go to: https://github.com/Dilli/Blueshot

2. **Access the Release Page:**
   - Click on "Releases" in the right sidebar
   - Find the "v0.1.0-beta" release
   - Click "Edit" button

3. **Upload the Installer:**
   - Scroll down to the "Attach binaries by dropping them here or selecting them" section
   - Drag and drop or click to select: `installer\Blueshot-Setup-v0.1.0-beta.exe`
   - Wait for the upload to complete (47.99 MB file)

4. **Update Release Description (Optional):**
   Add this text to highlight the new installer:
   ```
   ## 🚀 Professional Installer Available!
   
   **New in this release**: Professional Windows installer with complete setup wizard!
   
   ### Download Options:
   - **🔧 [Blueshot-Setup-v0.1.0-beta.exe](installer-download-link)** - Professional installer (Recommended)
   - **📦 [Blueshot-v0.1.0-beta-win-x64.zip](zip-download-link)** - Portable version
   
   ### Installer Features:
   ✅ One-click installation  
   ✅ Desktop shortcut creation  
   ✅ Start menu integration  
   ✅ Automatic startup option  
   ✅ File type associations  
   ✅ Professional uninstaller  
   ```

5. **Save the Release:**
   - Click "Update release" to save your changes

## Alternative: GitHub CLI Method

If you have GitHub CLI installed, you can run:
```bash
gh release upload v0.1.0-beta "installer\Blueshot-Setup-v0.1.0-beta.exe" --clobber
```

## Alternative: REST API Method

If you have a GitHub token, you can use PowerShell:
```powershell
# Set your GitHub token
$token = "your_github_token_here"

# Upload using REST API
$headers = @{
    "Authorization" = "token $token"
    "Accept" = "application/vnd.github.v3+json"
}

# Get release ID first, then upload asset
# (See full script in repository)
```

## Files Ready for Upload:
- 📁 **Installer**: `installer\Blueshot-Setup-v0.1.0-beta.exe` (47.99 MB)
- 📄 **Release Notes**: `RELEASE_NOTES_v0.1.0-beta.md`
- ⚙️ **Build Scripts**: `build-installer.bat`, `build-installer.ps1`

Your professional installer is ready and includes all the modern UI features we've implemented!
