@echo off
set "DEVICE=%~1"
adb install -r "%~dp0%DEVICE%\VR-Museum.apk"
adb shell mkdir -p /sdcard/Android/data/VR.Museum/files
adb push "%~dp0config.json" /sdcard/Android/data/VR.Museum/files/config.json
echo Done.
pause
