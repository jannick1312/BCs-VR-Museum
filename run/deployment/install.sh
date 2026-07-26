#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd -- "$(dirname -- "$0")" && pwd)"
RUN_DIR="$(dirname -- "$SCRIPT_DIR")"
DEVICE="$1"
PACKAGE="VR.Museum"
REMOTE_FILES="/sdcard/Android/data/VR.Museum/files"

adb install -r "$SCRIPT_DIR/$DEVICE/VR-Museum.apk"
adb shell pm clear "$PACKAGE"
adb shell rm -rf "$REMOTE_FILES"
adb shell mkdir -p "$REMOTE_FILES"
adb push "$RUN_DIR/config.json" "$REMOTE_FILES/config.json"

if [ -d "$RUN_DIR/media" ]; then
	adb shell mkdir -p "$REMOTE_FILES/media/images" "$REMOTE_FILES/media/videos" "$REMOTE_FILES/media/3dPck"

	for media_type in images videos 3dPck; do
		if [ -d "$RUN_DIR/media/$media_type" ]; then
			adb push "$RUN_DIR/media/$media_type/." "$REMOTE_FILES/media/$media_type/"
		fi
	done
fi

adb shell chmod -R 777 "$REMOTE_FILES"

printf 'Done.\n'
