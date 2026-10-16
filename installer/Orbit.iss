#define AppVersion GetVersionNumbersString("..\release\installer-payload\Orbit.exe")
[Setup]
AppId={{60E686D4-296A-41D2-B445-5DDC79FD2A2C}
AppName=Orbit
AppVersion={#AppVersion}
AppPublisher=Dario P.
AppPublisherURL=https://github.com/donishki/orbit
AppSupportURL=https://github.com/donishki/orbit/issues
AppUpdatesURL=https://github.com/donishki/orbit/releases/latest
DefaultDirName={localappdata}\Programs\Orbit
DefaultGroupName=Orbit
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\release
OutputBaseFilename=Orbit-Setup-win-x64
SetupIconFile=..\app.ico
UninstallDisplayIcon={app}\Orbit.exe
UninstallDisplayName=Orbit
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
CloseApplications=yes
RestartApplications=no

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "..\release\installer-payload\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Orbit"; Filename: "{app}\Orbit.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Orbit"; Filename: "{app}\Orbit.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Orbit.exe"; Description: "Launch Orbit"; Flags: nowait postinstall skipifsilent

; Deliberately do not remove the user's %LOCALAPPDATA%\Orbit settings on uninstall.
