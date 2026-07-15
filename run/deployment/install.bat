@echo off
set "DEVICE=%~1"
adb install -r "%~dp0%DEVICE%\VR-Museum.apk"
adb shell mkdir -p /sdcard/Android/data/VR.Museum/files
adb push "%~dp0config.json" /sdcard/Android/data/VR.Museum/files/config.json
if exist "%~dp0media\" (
	adb shell mkdir -p /sdcard/Android/data/VR.Museum/files/media
	adb push "%~dp0media\." /sdcard/Android/data/VR.Museum/files/media/
)
echo Done.
pause
