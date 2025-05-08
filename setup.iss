#define MyAppName "ReignCraft Launcher"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "ReignCraft"
#define MyAppExeName "RCL.exe"

[Setup]
AppId={{B5BDF0B0-319E-43C9-8ECD-C276887C36C6}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\ReignCraft
DisableDirPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
DisableProgramGroupPage=yes
OutputBaseFilename=rcl
SetupIconFile=C:\Users\Cunny\source\repos\ReignLauncher\minecraft.ico
SolidCompression=yes
Compression = zip
WizardStyle=modern

[Languages]
Name: "reign"; MessagesFile: "compiler:Languages\Reign.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "C:\Users\Cunny\RiderProjects\AvaloniaRCL\bin\Release\net8.0\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Users\Cunny\net8.0desktop_x64.exe"; DestDir: "{tmp}"; Flags: dontcopy deleteafterinstall

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet6Installed: Boolean;
var
  regVersion: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.WindowsDesktop.App', '8.0.15', regVersion);
end;

function InitializeSetup: Boolean;
var
  dotNet6Installed: Boolean;
  ResultCode: Integer;
begin
  Result := True;
  
  dotNet6Installed := IsDotNet6Installed;

  if not dotNet6Installed then
  begin
    MsgBox('Библиотеки .NET 6 Desktop не найдены на вашем компьютере. Нажмите ОК, чтобы установить необходимые библиотеки.', mbInformation, MB_OK);
    ExtractTemporaryFile('net6.0desktop_x86.exe');
    Exec(ExpandConstant('{tmp}\net6.0desktop_x86.exe'), '', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode);
    dotNet6Installed := IsDotNet6Installed;
    if not dotNet6Installed then
    begin
      MsgBox('Установка .NET 6 Desktop завершена с ошибкой. Перезапустите установщик.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;