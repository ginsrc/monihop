#ifndef AppVersion
  #error AppVersion must be supplied by packaging/build-release.ps1
#endif

#ifndef PublishDirectory
  #error PublishDirectory must be supplied by packaging/build-release.ps1
#endif

#define AppName "MoniHop"
#define AppPublisher "ginsrc"
#define AppExeName "MoniHop.exe"
#define AppRepositoryUrl "https://github.com/ginsrc/monihop"

[Setup]
AppId={{143343E8-6B18-4BD1-82D2-9BCCEA1B694C}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppRepositoryUrl}
AppSupportURL={#AppRepositoryUrl}/issues
AppUpdatesURL={#AppRepositoryUrl}/releases
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0.22000
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#AppExeName}
LicenseFile={#SourcePath}\..\LICENSE
SetupIconFile={#SourcePath}\..\src\MoniHop.Desktop\Assets\MoniHop.ico
WizardStyle=modern dynamic
Compression=lzma2
SolidCompression=yes
CloseApplications=force
RestartApplications=no
AppMutex=Local\MoniHop.Desktop.SingleInstance.v1
OutputBaseFilename=MoniHop-{#AppVersion}-win-x64-setup

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDirectory}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\MoniHop"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\MoniHop"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,MoniHop}"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "MoniHop"; Flags: uninsdeletevalue

[UninstallRun]
Filename: "{sys}\schtasks.exe"; Parameters: "/Delete /F /TN ""MoniHop Startup"""; Flags: runhidden waituntilterminated; RunOnceId: "RemoveMoniHopStartupTask"

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\MoniHop"
