[Setup]
AppName=Blueshot - Screen Capture Tool
AppVersion=1.0.0
AppVerName=Blueshot v1.0.0
AppPublisher=Blueshot Team
AppPublisherURL=https://github.com/yourusername/blueshot
AppSupportURL=https://github.com/yourusername/blueshot/issues
AppUpdatesURL=https://github.com/yourusername/blueshot/releases
AppCopyright=Copyright (C) 2025 Blueshot Team
DefaultDirName={autopf}\Blueshot
DefaultGroupName=Blueshot
AllowNoIcons=yes
LicenseFile=
InfoAfterFile=
OutputDir=installer
OutputBaseFilename=Blueshot-Setup-v1.0.0
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
VersionInfoVersion=1.0.0.0
VersionInfoCompany=Blueshot Team
VersionInfoDescription=Blueshot Screen Capture Tool Installer
VersionInfoCopyright=Copyright (C) 2025 Blueshot Team
VersionInfoProductName=Blueshot
VersionInfoProductVersion=1.0.0
VersionInfoOriginalFileName=Blueshot-Setup-v1.0.0.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start Blueshot automatically when Windows starts"; GroupDescription: "Startup Options"; Flags: checked
Name: "quicklaunch"; Description: "Create a &Quick Launch icon"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1
Name: "associatefiles"; Description: "Associate common image file types with Blueshot"; GroupDescription: "File Associations"; Flags: unchecked

[Files]
Source: "bin\Release\net8.0-windows\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "icon.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion; DestName: "README.txt"

[Icons]
Name: "{group}\Blueshot"; Filename: "{app}\Blueshot.exe"; IconFilename: "{app}\icon.ico"
Name: "{group}\Blueshot Help"; Filename: "{app}\README.txt"
Name: "{group}\{cm:UninstallProgram,Blueshot}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Blueshot"; Filename: "{app}\Blueshot.exe"; IconFilename: "{app}\icon.ico"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\Blueshot"; Filename: "{app}\Blueshot.exe"; IconFilename: "{app}\icon.ico"; Tasks: quicklaunch

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
WelcomeLabel2=This will install [name/ver] on your computer.%n%nBlueshot is a powerful screenshot tool with annotation capabilities and global hotkey support.%n%nIt is recommended that you close all other applications before continuing.
FinishedHeadingLabel=Completing the Blueshot Setup Wizard
FinishedLabelNoIcons=Setup has finished installing Blueshot on your computer. The application may be launched by selecting the installed icons.
FinishedLabel=Setup has finished installing Blueshot on your computer. The application may be launched by selecting the installed icons.

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
          'This will install Blueshot v1.0.0 on your computer.' + #13#10 + #13#10 +
          'Blueshot is a powerful screenshot tool featuring:' + #13#10 +
          '• Global Print Screen hotkey with fallback options' + #13#10 +
          '• Professional annotation tools' + #13#10 +
          '• Screenshot gallery and management' + #13#10 +
          '• System tray integration' + #13#10 +
          '• Self-contained deployment (no additional software required)' + #13#10 + #13#10 +
          'It is recommended that you close all other applications before continuing.';
      end;
    wpFinished:
      begin
        WizardForm.FinishedLabel.Caption := 
          'Setup has finished installing Blueshot on your computer.' + #13#10 + #13#10 +
          'The application will automatically register screenshot hotkeys and appear in your system tray.' + #13#10 + #13#10 +
          'To take screenshots:' + #13#10 +
          '• Press Print Screen (or Ctrl+Shift+F12 if unavailable)' + #13#10 +
          '• Right-click the system tray icon' + #13#10 +
          '• Use the desktop shortcut if created' + #13#10 + #13#10 +
          'The application may be launched by selecting the installed icons.';
      end;
  end;
end;
