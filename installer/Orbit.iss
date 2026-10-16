#ifndef Arch
  #define Arch "x64"
#endif
#define Payload "..\release\installer-payload-" + Arch
#define AppVersion GetVersionNumbersString(Payload + "\Orbit.exe")
[Setup]
#if Arch == "x86"
AppId={{A2A7C189-843C-4D5F-B2F3-05A80A936C95}
DefaultDirName={localappdata}\Programs\Orbit-x86
#define ShortcutName "Orbit (32-bit)"
#else
AppId={{60E686D4-296A-41D2-B445-5DDC79FD2A2C}
DefaultDirName={localappdata}\Programs\Orbit
#define ShortcutName "Orbit"
#endif
AppName=Orbit
AppVersion={#AppVersion}
AppPublisher=Dario P.
AppPublisherURL=https://github.com/donishki/orbit
AppSupportURL=https://github.com/donishki/orbit/issues
AppUpdatesURL=https://github.com/donishki/orbit/releases/latest
DefaultGroupName=Orbit
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
SetupArchitecture=x86
#if Arch == "x64"
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#else
ArchitecturesAllowed=x86compatible
#endif
MinVersion=10.0
OutputDir=..\release
OutputBaseFilename=Orbit-Setup-win-{#Arch}
SetupIconFile=..\app.ico
UninstallDisplayIcon={app}\Orbit.exe
UninstallDisplayName={#ShortcutName}
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
CloseApplications=yes
RestartApplications=no

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#Payload}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#ShortcutName}"; Filename: "{app}\Orbit.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\{#ShortcutName}"; Filename: "{app}\Orbit.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Orbit.exe"; Description: "Launch Orbit"; Flags: nowait postinstall skipifsilent

; Deliberately do not remove the user's %LOCALAPPDATA%\Orbit settings on uninstall.
