; Inno Setup script for TradewatchSetup.exe. Built by the Release workflow; locally:
;   iscc /DAppVersion=1.2.0 installer\Tradewatch.iss
; It expects the single-file build in publish\Tradewatch.exe (see README "Building from source").

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

[Setup]
; Never change AppId: Windows uses it to recognise upgrades of the same app.
AppId={{6F2C3B1E-8A4D-4E7B-9C1F-2D5E8A7B3C40}
AppName=Tradewatch
AppVersion={#AppVersion}
AppPublisher=joseoguedes
AppPublisherURL=https://github.com/joseoguedes/trade-watch
; Per-user install: no admin prompt, lives in %LOCALAPPDATA%\Programs\Tradewatch
PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\Tradewatch
DisableProgramGroupPage=yes
DisableDirPage=yes
OutputDir=Output
OutputBaseFilename=TradewatchSetup
SetupIconFile=..\Tradewatch\app.ico
UninstallDisplayIcon={app}\Tradewatch.exe
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Same name as the app's single-instance mutex, so Setup asks to close a running Tradewatch
AppMutex=Tradewatch.SingleInstance
CloseApplications=yes

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"
Name: "startup"; Description: "Start Tradewatch when &Windows starts (in the tray)"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
Source: "..\publish\Tradewatch.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\Tradewatch"; Filename: "{app}\Tradewatch.exe"
Name: "{userdesktop}\Tradewatch"; Filename: "{app}\Tradewatch.exe"; Tasks: desktopicon

[Registry]
; Same entry Settings > Start with Windows writes (see StartupRegistration.cs)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Tradewatch"; ValueData: """{app}\Tradewatch.exe"" --tray"; Tasks: startup
; Whether or not the task was picked (the app can set it too), remove the entry on uninstall
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "Tradewatch"; Flags: dontcreatekey uninsdeletevalue

[Run]
Filename: "{app}\Tradewatch.exe"; Description: "Launch Tradewatch"; Flags: nowait postinstall skipifsilent
