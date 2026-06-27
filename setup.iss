#define AppName "Text to Audiobook"
#define AppVersion "14.5"
#define AppPublisher "Andrey Buchin"
#define AppExeName "TextToAudiobookCSharp.exe"
#define SourceDir "dist_v14.5"

[Setup]
AppId={{A3F2B1C4-7E5D-4A0F-9B2E-1C3D5E7F8A9B}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} v{#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=setup_output
OutputBaseFilename=TextToAudiobook_v14.5_setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=app_icon.ico
UninstallDisplayIcon={app}\{#AppExeName}
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать значок на Рабочем столе"; GroupDescription: "Дополнительные значки:"

[Files]
Source: "{#SourceDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\РУКОВОДСТВО ПОЛЬЗОВАТЕЛЯ.pdf"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\РУКОВОДСТВО ПОЛЬЗОВАТЕЛЯ.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Руководство пользователя"; Filename: "{app}\РУКОВОДСТВО ПОЛЬЗОВАТЕЛЯ.pdf"
Name: "{group}\Удалить {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Запустить {#AppName}"; Flags: nowait postinstall skipifsilent
