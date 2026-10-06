; Browser Selector installer (Inno Setup 6). Per user, no admin rights.
; Build with tools\build-release.ps1, or:
;   ISCC.exe /DAppVersion=0.1.0-beta.2 /DFileVersion=0.1.0.0 /DSourceExe=<BrowserSelector.exe> installer\BrowserSelector.iss
; Saved as UTF-8 with BOM so the Arabic messages compile correctly.

#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif
#ifndef SourceExe
  #define SourceExe "..\Browser-Selector\bin\Release\net48\BrowserSelector.exe"
#endif
#ifndef FileVersion
  #define FileVersion "0.0.0.0"
#endif
#ifndef OutputDir
  #define OutputDir "..\release"
#endif

[Setup]
AppId={{6B0D6E52-3A0B-4C8E-9C61-2F4F3B8C7A15}
AppName=Browser Selector
AppVersion={#AppVersion}
AppVerName=Browser Selector {#AppVersion}
AppPublisher=Mohammad Diab
AppPublisherURL=https://github.com/Mohammad-Diab/Browser-Selector
AppSupportURL=https://github.com/Mohammad-Diab/Browser-Selector/issues
AppUpdatesURL=https://github.com/Mohammad-Diab/Browser-Selector/releases
; Per user: %LOCALAPPDATA%\Programs\Browser Selector, nothing that needs admin.
PrivilegesRequired=lowest
DefaultDirName={autopf}\Browser Selector
DisableProgramGroupPage=yes
DisableDirPage=auto
DisableReadyPage=yes
UninstallDisplayName=Browser Selector
UninstallDisplayIcon={app}\BrowserSelector.exe
; Setup has its own icon (the app icon with an install badge) so it isn't mistaken for the app.
; Artwork comes from tools\make-icon.py.
SetupIconFile=setup.ico
WizardSmallImageFile=wizard-small.png
WizardImageFile=wizard-large.png
WizardStyle=modern
ShowLanguageDialog=auto
Compression=lzma2/ultra64
SolidCompression=yes
OutputDir={#OutputDir}
OutputBaseFilename=BrowserSelector-Setup-{#AppVersion}
VersionInfoVersion={#FileVersion}
VersionInfoProductName=Browser Selector
VersionInfoCompany=Mohammad Diab
VersionInfoCopyright=Copyright (c) 2024-2026 Mohammad Diab
LicenseFile=..\LICENSE

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[CustomMessages]
english.ChooseDefault=Choose Browser Selector as my default browser now
arabic.ChooseDefault=اختيار Browser Selector كمتصفحي الافتراضي الآن
english.OpenSettings=Open Browser Selector settings
arabic.OpenSettings=فتح إعدادات Browser Selector
english.RegisterFailed=Browser Selector was installed, but registering it as a browser failed. Open Browser Selector from the Start menu and click Register.
arabic.RegisterFailed=تم تثبيت Browser Selector، لكن فشل تسجيله كمتصفح. افتحه من قائمة Start واضغط "تسجيل".
english.DeleteSettings=Also delete your Browser Selector settings and site rules?
arabic.DeleteSettings=هل تريد حذف إعدادات Browser Selector وقواعد المواقع أيضاً؟

[Files]
Source: "{#SourceExe}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"

[Icons]
Name: "{autoprograms}\Browser Selector"; Filename: "{app}\BrowserSelector.exe"; Comment: "Browser Selector settings"

[Run]
; (Registering as a browser happens in [Code], so a failure can be reported.)
; Windows lets only the user pick the default browser: open Default apps on our page.
Filename: "ms-settings:defaultapps?registeredAppUser=Browser%20Selector"; Description: "{cm:ChooseDefault}"; Flags: postinstall shellexec nowait
Filename: "{app}\BrowserSelector.exe"; Description: "{cm:OpenSettings}"; Flags: postinstall nowait unchecked

[UninstallRun]
; Remove the registry keys the app added, when they belong to this copy (Windows then falls back to another browser).
Filename: "{app}\BrowserSelector.exe"; Parameters: "--unregister"; Flags: runhidden waituntilterminated; RunOnceId: "Unregister"

[Code]
// Register for this user (HKCU), pointing at the installed copy, and say so if it fails.
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
    if not Exec(ExpandConstant('{app}\BrowserSelector.exe'), '--register', '', SW_HIDE, ewWaitUntilTerminated, ResultCode)
       or (ResultCode <> 0) then
      MsgBox(CustomMessage('RegisterFailed'), mbError, MB_OK);
end;

// Settings live in %APPDATA%, which a portable copy without its own settings.json shares: ask before deleting
// them (a silent uninstall leaves nothing behind).
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Dir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    Dir := ExpandConstant('{userappdata}\BrowserSelector');
    if DirExists(Dir) and (UninstallSilent or (MsgBox(CustomMessage('DeleteSettings'), mbConfirmation, MB_YESNO) = IDYES)) then
      DelTree(Dir, True, True, True);
  end;
end;
