@echo off
set "DEVICE=%~1"
adb install -r "%~dp0%DEVICE%\VR-Museum.apk"
if errorlevel 1 goto error
adb shell mkdir -p /sdcard/Android/data/VR.Museum/files
if errorlevel 1 goto error
adb push "%~dp0config.json" /sdcard/Android/data/VR.Museum/files/config.json
if errorlevel 1 goto error
if exist "%~dp0media\" (
	adb shell mkdir -p /sdcard/Android/data/VR.Museum/files/media/images /sdcard/Android/data/VR.Museum/files/media/videos /sdcard/Android/data/VR.Museum/files/media/3d
	if errorlevel 1 goto error
	for %%D in (images videos 3d) do (
		if exist "%~dp0media\%%D\" (
			adb push "%~dp0media\%%D\." /sdcard/Android/data/VR.Museum/files/media/%%D/
			if errorlevel 1 goto error
		)
	)
)
adb shell chmod -R 777 /sdcard/Android/data/VR.Museum/files
if errorlevel 1 goto error
echo Done.
pause
exit /b 0

:error
echo Installation failed.
pause
exit /b 1
