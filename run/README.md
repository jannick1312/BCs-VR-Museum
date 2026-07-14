# Running and deploying BCS VR Museum

This directory contains the runtime configuration and helper scripts for the
three supported ways of running the application:

1. Deploy directly from the Godot editor.
2. Sideload an Android APK to a Meta Quest or VIVE Focus headset.
3. Run the exported Windows application and stream it to a headset.

## Configuration

Every mode uses the same JSON format:

```json
{
  "serverIp": "10.34.64.208"
}
```

- `serverIp` specifies the address of the vitrivr server.
- The application only reads `config.json`.
- If no `config.json` is found, `10.34.64.208` is used silently as the built-in
  default.

Media and log paths are not configurable because their names and locations are
determined internally.

## 1. Direct Godot deployment

Godot reads:

`src/GodotMuseum/config.json`

When Godot deploys the Android project, this file is included in the APK through the `*.json` export include filter. On Android, an external sideload configuration takes priority if one is present; otherwise, the file embedded in the APK is used.

Before deploying from Godot, select the correct export preset for the target VR
headset under **Project -> Export...**, as the headset type cannot be detected
automatically.

## 2. Android APK sideload

Quest and Focus use separate deployment directories. Godot exports each preset
directly to its matching directory:

```text
deployment/
├── install.bat
├── install.sh
├── logs.bat
├── logs.sh
├── config.json
├── quest/
│   └── VR-Museum.apk
└── focus/
    └── VR-Museum.apk
```

The instructions assume that `adb`, the APK, the configuration, USB debugging,
and an authorized USB-connected headset are already available. Edit the shared
`deployment/config.json` and pass `quest` or `focus` to the installation script.
The scripts install the matching APK and copy the shared configuration to:

`/sdcard/Android/data/VR.Museum/files/config.json`

On Windows:

```bat
install.bat quest
install.bat focus
```

On macOS or Linux, make the shared scripts executable once, then select the
target device:

```bash
chmod +x install.sh logs.sh
./install.sh quest
./install.sh focus
```

The scripts perform only these operations:

1. Install or update the APK using `adb install -r`.
2. Create the app-specific external files directory.
3. Copy `config.json` to the headset.

The headset file layout then is:

```text
/sdcard/Android/data/VR.Museum/files/
├── config.json
├── media/        # normally absent or empty
└── logs/
    ├── app.log
    └── app-readable.log
```

No media files are copied to the headset. If a requested local media file does
not exist, the application loads it from its remote URL. The deployment scripts
only install the APK and copy `config.json`.

### Live Android logs

Start the application on the headset before opening the live log. On Windows:

```bat
logs.bat
```

On macOS or Linux:

```bash
./logs.sh
```

Both scripts show the readable log lines and then follow new lines.

## 3. Windows streaming build

Place the exported Windows application in the `stream` directory with this
layout:

```text
stream/
├── VR-Museum.exe
├── config.json
├── libgodotopenxrvendors.dll
├── media/
└── logs/
```

The `.dll` file is part of the exported application and must remain next to the main `.exe`.

The application loads `config.json` from the directory containing the `.exe`.
The `media` and `logs` directories are also resolved relative to the `.exe`, so
absolute Windows paths are not required.
