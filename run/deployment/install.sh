#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd -- "$(dirname -- "$0")" && pwd)"
DEVICE="$1"
REMOTE_FILES="/sdcard/Android/data/VR.Museum/files"

adb install -r "$SCRIPT_DIR/$DEVICE/VR-Museum.apk"
adb shell mkdir -p "$REMOTE_FILES"
adb push "$SCRIPT_DIR/config.json" "$REMOTE_FILES/config.json"

if [ -d "$SCRIPT_DIR/media" ]; then
	adb shell mkdir -p "$REMOTE_FILES/media/images" "$REMOTE_FILES/media/videos" "$REMOTE_FILES/media/3d"

	for media_type in images videos 3d; do
		if [ -d "$SCRIPT_DIR/media/$media_type" ]; then
			adb push "$SCRIPT_DIR/media/$media_type/." "$REMOTE_FILES/media/$media_type/"
		fi
	done
fi

adb shell chmod -R 777 "$REMOTE_FILES"

printf 'Done.\n'
