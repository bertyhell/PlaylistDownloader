Unicode True

!include "MUI.nsh"

!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

Name "PlaylistDownloader 1.12.0"
BrandingText ""
OutFile "C:\Users\Bert\Documents\repos\PlaylistDownloader\PlaylistDownloader\Installer\Output\PlaylistDownloader.exe"
InstallDir "$PROGRAMFILES\PlaylistDownloader"
ShowInstDetails show
ShowUnInstDetails show

Function .onInit
  MessageBox MB_YESNO|MB_ICONQUESTION "This will install $(^Name). Do you wish to continue?" IDYES +2
  Abort
FunctionEnd

Section -InstallDelete
  RMDir /r "$INSTDIR"
SectionEnd

Section -Files
  SetOutPath "$INSTDIR"
  File /r "..\PlaylistDownloader\bin\Release\*"
  SetOutPath "$APPDATA\PlaylistDownloader"
  File "..\PlaylistDownloader\bin\Release\youtube-dl.exe"
  SetOutPath "$APPDATA\PlaylistDownloader\ffmpeg"
  File /r "..\PlaylistDownloader\bin\Release\ffmpeg\*"
  RMDir /r "$INSTDIR\ffmpeg"
  Delete "$INSTDIR\youtube-dl.exe"
SectionEnd

Section -Icons
  CreateDirectory "$SMPROGRAMS\PlaylistDownloader"
  CreateShortCut "$SMPROGRAMS\PlaylistDownloader\PlaylistDownloader.lnk" "$INSTDIR\PlaylistDownloader.exe" 
  CreateDirectory "$DESKTOP"
  CreateShortCut "$DESKTOP\PlaylistDownloader.lnk" "$INSTDIR\PlaylistDownloader.exe" 
SectionEnd

Section -PostInstall
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\{{B0011CEC-2AE2-40CF-9136-C2BD13928896}" "DisplayName" "{{B0011CEC-2AE2-40CF-9136-C2BD13928896}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\{{B0011CEC-2AE2-40CF-9136-C2BD13928896}" "UninstallString" "$INSTDIR\uninstall.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\{{B0011CEC-2AE2-40CF-9136-C2BD13928896}" "DisplayIcon" "{app}\PlaylistDownloader.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\{{B0011CEC-2AE2-40CF-9136-C2BD13928896}" "Publisher" "Taxrebel productions"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\{{B0011CEC-2AE2-40CF-9136-C2BD13928896}" "URLInfoAbout" "https://github.com/bertyhell/PlaylistDownloader"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\{{B0011CEC-2AE2-40CF-9136-C2BD13928896}" "HelpLink" "https://github.com/bertyhell/PlaylistDownloader"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\{{B0011CEC-2AE2-40CF-9136-C2BD13928896}" "URLUpdateInfo" "https://github.com/bertyhell/PlaylistDownloader"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\{{B0011CEC-2AE2-40CF-9136-C2BD13928896}" "DisplayVersion" "1.12.0"
  WriteUninstaller "$INSTDIR\uninstall.exe"
SectionEnd

Section -Run
  ExecShell "" "$INSTDIR\PlaylistDownloader.exe"
SectionEnd


#### Uninstaller code ####

Function un.onInit
  MessageBox MB_ICONQUESTION|MB_YESNO|MB_DEFBUTTON2 "Are you sure you want to completely remove $(^Name) and all of its components?" IDYES +2
  Abort
FunctionEnd

Function un.onUninstSuccess
  HideWindow
  MessageBox MB_ICONINFORMATION|MB_OK "$(^Name) was successfully removed from your computer."
FunctionEnd

Section Uninstall
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\{{B0011CEC-2AE2-40CF-9136-C2BD13928896}"

  Delete "$INSTDIR\uninstall.exe"
  Delete "$DESKTOP\PlaylistDownloader.lnk"
  Delete "$SMPROGRAMS\PlaylistDownloader\PlaylistDownloader.lnk"
  Delete "{userappdata}/PlaylistDownloader/ffmpeg\*"
  Delete "{userappdata}/PlaylistDownloader\youtube-dl.exe"
  Delete "$INSTDIR\*"

  RMDir "$DESKTOP"
  RMDir "$SMPROGRAMS\PlaylistDownloader"
  RMDir "{userappdata}/PlaylistDownloader/ffmpeg"
  RMDir "{userappdata}/PlaylistDownloader"
  RMDir "$INSTDIR"

  SetAutoClose true
SectionEnd

