@echo off
set "DEVICE=%~1"
for %%I in ("%~dp0..") do set "RUN_DIR=%%~fI"
adb install -r "%~dp0%DEVICE%\VR-Museum.apk"
if errorlevel 1 goto error
adb shell mkdir -p /sdcard/Android/data/VR.Museum/files
if errorlevel 1 goto error
adb push "%RUN_DIR%\config.json" /sdcard/Android/data/VR.Museum/files/config.json
if errorlevel 1 goto error
if exist "%RUN_DIR%\media\" (
	adb shell mkdir -p /sdcard/Android/data/VR.Museum/files/media/images /sdcard/Android/data/VR.Museum/files/media/videos /sdcard/Android/data/VR.Museum/files/media/3dPck
	if errorlevel 1 goto error
	for %%D in (images videos 3dPck) do (
		if exist "%RUN_DIR%\media\%%D\" (
			adb push "%RUN_DIR%\media\%%D\." /sdcard/Android/data/VR.Museum/files/media/%%D/
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
