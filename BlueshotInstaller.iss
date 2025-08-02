[Setup]
AppName=Blueshot - Professional Screen Capture Tool
AppVersion=0.1.0
AppVerName=Blueshot v0.1.0-beta
AppPublisher=Dilli
AppPublisherURL=https://github.com/Dilli/Blueshot
AppSupportURL=https://github.com/Dilli/Blueshot/issues
AppUpdatesURL=https://github.com/Dilli/Blueshot/releases
AppCopyright=Copyright (C) 2025 Dilli
DefaultDirName={autopf}\Blueshot
DefaultGroupName=Blueshot
AllowNoIcons=yes
LicenseFile=LICENSE
InfoAfterFile=RELEASE_NOTES_v0.1.0-beta.md
OutputDir=installer
OutputBaseFilename=Blueshot-Setup-v0.1.0-beta
SetupIconFile=icon.ico
UninstallDisplayIcon={app}\Blueshot.exe
Compression=lzma2/ultra64
SolidCompression=yes
InternalCompressLevel=ultra64
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
DisableProgramGroupPage=yes
DisableReadyPage=no
DisableFinishedPage=no
ShowLanguageDialog=no
AppId={{B8E5A8F2-4C6D-4A5B-9E8F-1D2C3E4F5A6B}
VersionInfoVersion=0.1.0.0
VersionInfoCompany=Dilli
VersionInfoDescription=Blueshot Professional Screen Capture Tool Installer - Beta
VersionInfoCopyright=Copyright (C) 2025 Dilli
VersionInfoProductName=Blueshot
VersionInfoProductVersion=0.1.0.0
VersionInfoOriginalFileName=Blueshot-Setup-v0.1.0-beta.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start Blueshot automatically when Windows starts"; GroupDescription: "Startup Options"
Name: "quicklaunch"; Description: "Create a &Quick Launch icon"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1
Name: "associatefiles"; Description: "Associate common image file types with Blueshot"; GroupDescription: "File Associations"; Flags: unchecked

[Files]
Source: "bin\Release\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "icon.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion; DestName: "README.txt"
Source: "RELEASE_NOTES_v0.1.0-beta.md"; DestDir: "{app}"; Flags: ignoreversion; DestName: "RELEASE_NOTES.txt"
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion; DestName: "LICENSE.txt"
Source: "icons\*"; DestDir: "{app}\icons"; Flags: ignoreversion recursesubdirs createallsubdirs; MinVersion: 0,6.1

[Icons]
Name: "{group}\Blueshot"; Filename: "{app}\Blueshot.exe"; IconFilename: "{app}\icon.ico"; Comment: "Professional Screen Capture Tool"
Name: "{group}\Blueshot Documentation"; Filename: "{app}\README.txt"; Comment: "Read the documentation"
Name: "{group}\Release Notes"; Filename: "{app}\RELEASE_NOTES.txt"; Comment: "What's new in this version"
Name: "{group}\{cm:UninstallProgram,Blueshot}"; Filename: "{uninstallexe}"; Comment: "Uninstall Blueshot"
Name: "{autodesktop}\Blueshot"; Filename: "{app}\Blueshot.exe"; IconFilename: "{app}\icon.ico"; Tasks: desktopicon; Comment: "Professional Screen Capture Tool"
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\Blueshot"; Filename: "{app}\Blueshot.exe"; IconFilename: "{app}\icon.ico"; Tasks: quicklaunch; Comment: "Quick access to Blueshot"

[Registry]
; Add to startup if user chooses this option
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Blueshot"; ValueData: """{app}\Blueshot.exe"""; Flags: uninsdeletevalue; Tasks: startupicon

; File associations (optional)
Root: HKCR; Subkey: ".png"; ValueType: string; ValueName: ""; ValueData: "BlueshotImage"; Flags: uninsdeletevalue; Tasks: associatefiles
Root: HKCR; Subkey: ".jpg"; ValueType: string; ValueName: ""; ValueData: "BlueshotImage"; Flags: uninsdeletevalue; Tasks: associatefiles
Root: HKCR; Subkey: ".jpeg"; ValueType: string; ValueName: ""; ValueData: "BlueshotImage"; Flags: uninsdeletevalue; Tasks: associatefiles
Root: HKCR; Subkey: ".bmp"; ValueType: string; ValueName: ""; ValueData: "BlueshotImage"; Flags: uninsdeletevalue; Tasks: associatefiles
Root: HKCR; Subkey: "BlueshotImage"; ValueType: string; ValueName: ""; ValueData: "Image File"; Flags: uninsdeletevalue; Tasks: associatefiles
Root: HKCR; Subkey: "BlueshotImage\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\Blueshot.exe,0"; Flags: uninsdeletevalue; Tasks: associatefiles
Root: HKCR; Subkey: "BlueshotImage\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Blueshot.exe"" ""%1"""; Flags: uninsdeletevalue; Tasks: associatefiles

[Run]
Filename: "{app}\Blueshot.exe"; Description: "{cm:LaunchProgram,Blueshot}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
Type: dirifempty; Name: "{app}"

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.%n%nBlueshot is a professional screenshot tool with advanced annotation capabilities, modern UI design, and powerful features.%n%nFeatures in this beta release:%n• RoundedButton UI with modern 5px radius styling%n• Borderless preview with traffic light controls%n• Enhanced carousel navigation%n• Region-based text annotation system%n• Comprehensive logging and error handling%n• Self-contained deployment%n%nIt is recommended that you close all other applications before continuing.
FinishedHeadingLabel=Completing the Blueshot v0.1.0-beta Setup Wizard
FinishedLabelNoIcons=Setup has finished installing Blueshot v0.1.0-beta on your computer. The application may be launched by selecting the installed icons.
FinishedLabel=Setup has finished installing Blueshot v0.1.0-beta on your computer. The application may be launched by selecting the installed icons.

[Code]
var
  NetRuntimeInstalled: Boolean;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    Log('Installation completed successfully');
    // Optionally register additional components or perform cleanup
  end;
end;

function CheckDotNetRuntime(): Boolean;
var
  ResultCode: Integer;
  TempFile: String;
begin
  Result := True; // Assume success for self-contained deployment
  
  // For self-contained deployments, we don't need to check .NET runtime
  // But we can still provide helpful information
  
  Log('Checking system compatibility...');
  
  // Check Windows version (minimum Windows 10 version 1809)
  if not IsWin64 then
  begin
    if MsgBox('This application is designed for 64-bit Windows systems. You may experience compatibility issues on 32-bit systems. Continue anyway?', 
              mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDNO then
    begin
      Result := False;
      Exit;
    end;
  end;
  
  Log('System compatibility check passed');
end;

function InitializeSetup(): Boolean;
begin
  Result := CheckDotNetRuntime();
  
  if Result then
  begin
    Log('Starting Blueshot installation...');
    NetRuntimeInstalled := True;
  end
  else
  begin
    Log('Installation cancelled due to system requirements');
  end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  
  // Skip components page if not needed
  case PageID of
    wpSelectComponents:
      Result := False; // Show components page for user choices
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  case CurPageID of
    wpWelcome:
      begin
        WizardForm.WelcomeLabel2.Caption := 
          'This will install Blueshot v0.1.0-beta on your computer.' + #13#10 + #13#10 +
          'Blueshot is a professional screenshot tool featuring:' + #13#10 +
          '• Modern UI with rounded buttons and borderless design' + #13#10 +
          '• Enhanced carousel navigation with larger controls' + #13#10 +
          '• Advanced text annotation (point and region-based)' + #13#10 +
          '• Traffic light window controls (minimize/maximize/close)' + #13#10 +
          '• Comprehensive logging and error handling' + #13#10 +
          '• Global hotkey support with Print Screen integration' + #13#10 +
          '• Self-contained deployment (no .NET runtime required)' + #13#10 + #13#10 +
          'This is a BETA release - feedback is appreciated!' + #13#10 + #13#10 +
          'It is recommended that you close all other applications before continuing.';
      end;
    wpFinished:
      begin
        WizardForm.FinishedLabel.Caption := 
          'Setup has finished installing Blueshot v0.1.0-beta on your computer.' + #13#10 + #13#10 +
          'The application features a modern UI with rounded buttons and enhanced navigation.' + #13#10 + #13#10 +
          'To take screenshots:' + #13#10 +
          '• Press Print Screen (or Ctrl+Shift+F12 if unavailable)' + #13#10 +
          '• Right-click the system tray icon for quick access' + #13#10 +
          '• Use the desktop shortcut if created' + #13#10 + #13#10 +
          'New in this version:' + #13#10 +
          '• Enhanced carousel with 200px height and larger buttons' + #13#10 +
          '• Region-based text annotation system' + #13#10 +
          '• Borderless preview window design' + #13#10 + #13#10 +
          'Thank you for testing this beta release!';
      end;
  end;
end;
