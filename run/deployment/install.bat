@echo off
set "DEVICE=%~1"
for %%I in ("%~dp0..") do set "RUN_DIR=%%~fI"
set "PACKAGE=VR.Museum"
set "REMOTE_FILES=/sdcard/Android/data/VR.Museum/files"

adb install -r "%~dp0%DEVICE%\VR-Museum.apk"
if errorlevel 1 goto error
adb shell pm clear %PACKAGE%
if errorlevel 1 goto error
adb shell rm -rf %REMOTE_FILES%
if errorlevel 1 goto error
adb shell mkdir -p %REMOTE_FILES%
if errorlevel 1 goto error
adb push "%RUN_DIR%\config.json" %REMOTE_FILES%/config.json
if errorlevel 1 goto error

if exist "%RUN_DIR%\media\" (
	adb shell mkdir -p %REMOTE_FILES%/media/images %REMOTE_FILES%/media/videos %REMOTE_FILES%/media/3dPck
	if errorlevel 1 goto error
	for %%D in (images videos 3dPck) do (
		if exist "%RUN_DIR%\media\%%D\" (
			adb push "%RUN_DIR%\media\%%D\." %REMOTE_FILES%/media/%%D/
			if errorlevel 1 goto error
		)
	)
)

adb shell chmod -R 777 %REMOTE_FILES%
if errorlevel 1 goto error

echo Done.
pause
exit /b 0

:error
echo Installation failed.
pause
exit /b 1
