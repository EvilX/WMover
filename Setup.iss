; Version is passed by Publish-Setup.ps1 (ISCC /DAppVer=X.Y.Z).
; When compiled directly, it is read from the published exe.
#ifndef AppVer
  #define VerMajor
  #define VerMinor
  #define VerRev
  #define VerBuild
  #expr GetVersionComponents(AddBackslash(SourcePath) + "bin\SetupLayout\WMover.exe", VerMajor, VerMinor, VerRev, VerBuild)
  #define AppVer Str(VerMajor) + "." + Str(VerMinor) + "." + Str(VerRev)
#endif

[Setup]
AppName=WMover
AppVersion={#AppVer}
AppPublisher=LN
AppCopyright=Copyright (C) 2025 LN
MinVersion=10.0.26200
PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\WMover
DisableProgramGroupPage=yes
OutputDir=bin
OutputBaseFilename=WMover_Setup_{#StringChange(AppVer, ".", "_")}
SetupIconFile=src\Icon.ico
UninstallDisplayIcon={app}\WMover.exe
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
DisableWelcomePage=yes
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked
Name: "startup";     Description: "Run WMover when Windows starts";  GroupDescription: "Startup:"

[Files]
Source: "bin\SetupLayout\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\WMover"; Filename: "{app}\WMover.exe"
Name: "{autodesktop}\WMover";  Filename: "{app}\WMover.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "WMover"; ValueData: """{app}\WMover.exe"""; \
  Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\WMover.exe"; Description: "Launch WMover"; \
  Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill.exe"; Parameters: "/f /im WMover.exe"; Flags: runhidden; RunOnceId: "KillWMover"
