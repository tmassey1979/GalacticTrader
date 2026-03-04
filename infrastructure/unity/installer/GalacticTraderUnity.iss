#define MyAppId "{{6A7F426E-73A4-4703-8BF1-9F44F49E3A11}}"
#define MyAppName "GalacticTrader Unity Client"
#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif
#ifndef BuildRoot
  #define BuildRoot "..\\..\\..\\dist\\windows-build"
#endif
#ifndef OutputDir
  #define OutputDir "..\\..\\..\\dist\\installer"
#endif

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppPublisher=GalacticTrader
DefaultDirName={autopf}\\GalacticTrader Unity Client
DefaultGroupName=GalacticTrader Unity Client
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=GalacticTrader-Unity-Setup-{#AppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\\GalacticTrader.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "{#BuildRoot}\\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\\GalacticTrader Unity Client"; Filename: "{app}\\GalacticTrader.exe"
Name: "{autodesktop}\\GalacticTrader Unity Client"; Filename: "{app}\\GalacticTrader.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\\GalacticTrader.exe"; Description: "Launch GalacticTrader Unity Client"; Flags: nowait postinstall skipifsilent
