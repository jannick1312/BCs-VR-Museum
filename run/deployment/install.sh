#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd -- "$(dirname -- "$0")" && pwd)"
RUN_DIR="$(dirname -- "$SCRIPT_DIR")"
DEVICE="$1"
REMOTE_FILES="/sdcard/Android/data/VR.Museum/files"

adb install -r "$SCRIPT_DIR/$DEVICE/VR-Museum.apk"
adb shell mkdir -p "$REMOTE_FILES"
adb push "$RUN_DIR/config.json" "$REMOTE_FILES/config.json"

if [ -d "$RUN_DIR/media" ]; then
	adb shell mkdir -p "$REMOTE_FILES/media/images" "$REMOTE_FILES/media/videos" "$REMOTE_FILES/media/3d"

	for media_type in images videos 3d; do
		if [ -d "$RUN_DIR/media/$media_type" ]; then
			adb push "$RUN_DIR/media/$media_type/." "$REMOTE_FILES/media/$media_type/"
		fi
	done
fi

adb shell chmod -R 777 "$REMOTE_FILES"

printf 'Done.\n'
