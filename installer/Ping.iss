; Ping installer (Inno Setup 6)
; Build the app first:
;   dotnet publish src/Ping -c Release -r win-x64 --self-contained ^
;     -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
; Then compile this script (ISCC installer\Ping.iss) to produce PingSetup.exe.

#define AppName "Ping"
#define AppVersion "1.0.0"
#define AppPublisher "Ping"
#define PublishDir "..\src\Ping\bin\Release\net8.0-windows\win-x64\publish"

[Setup]
AppId={{8A4C1D6E-2F3B-4A5C-9D7E-1B0F2E3C4A5B}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\Ping
DefaultGroupName=Ping
; Per-user install: no admin prompt, no UAC elevation.
PrivilegesRequired=lowest
OutputDir=.\output
OutputBaseFilename=PingSetup-{#AppVersion}
SetupIconFile=..\assets\ping.ico
UninstallDisplayIcon={app}\Ping.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "autostart"; Description: "Start Ping when Windows starts"; GroupDescription: "Extra:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\Ping.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Ping"; Filename: "{app}\Ping.exe"
Name: "{group}\Uninstall Ping"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Ping"; Filename: "{app}\Ping.exe"; Flags: unchecked

[Registry]
; Only written when the user ticks the task; Ping also manages this itself from Settings.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; \
  ValueName: "Ping"; ValueData: """{app}\Ping.exe"""; Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "{app}\Ping.exe"; Description: "Launch Ping now"; \
  Flags: nowait postinstall skipifsilent unchecked

[UninstallDelete]
; Leave %LocalAppData%\Ping (tasks, settings, print log) in place on uninstall -
; receipts are paper, but the history may still be wanted. Delete manually if not.
Type: filesandordirs; Name: "{app}"
