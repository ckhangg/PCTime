#define AppVersion "0.1.0"
[Setup]
AppId={{536D0295-10B0-40E5-858E-1AA39174A7DE}
AppName=PC Time
AppVersion={#AppVersion}
AppPublisher=ckhangg
AppPublisherURL=https://github.com/ckhangg/PCTime
AppSupportURL=https://github.com/ckhangg/PCTime/issues
AppUpdatesURL=https://github.com/ckhangg/PCTime/releases
VersionInfoVersion=0.1.0.0
VersionInfoProductName=PC Time
VersionInfoCompany=ckhangg
DefaultDirName={localappdata}\Programs\PC-Time
DefaultGroupName=PC Time
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
OutputDir=..\artifacts
OutputBaseFilename=PC-Time-Setup-0.1.0-win-x64
UninstallDisplayName=PC Time
UninstallDisplayIcon={app}\PcTime.exe
Compression=lzma2/fast
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
[Files]
Source: "..\artifacts\PC-Time-v0.1.0-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Tasks]
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked
[Icons]
Name: "{autoprograms}\PC Time"; Filename: "{app}\PcTime.exe"; AppUserModelID: "ckhangg.PCTime"
Name: "{autodesktop}\PC Time"; Filename: "{app}\PcTime.exe"; AppUserModelID: "ckhangg.PCTime"; Tasks: desktopicon
[Run]
Filename: "{app}\PcTime.exe"; Description: "{cm:LaunchProgram,PC Time}"; Flags: nowait postinstall skipifsilent
