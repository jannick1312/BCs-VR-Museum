#!/usr/bin/env bash
SCRIPT_DIR="$(cd -- "$(dirname -- "$0")" && pwd)"
DEVICE="$1"

adb install -r "$SCRIPT_DIR/$DEVICE/VR-Museum.apk"
adb shell mkdir -p /sdcard/Android/data/VR.Museum/files
adb push "$SCRIPT_DIR/config.json" /sdcard/Android/data/VR.Museum/files/config.json

printf 'Done.\n'
