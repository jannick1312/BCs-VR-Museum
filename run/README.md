# Running and deploying BCS VR Museum

This directory contains the runtime configuration and helper scripts for the
three supported ways of running the application:

1. Deploy directly from the Godot editor.
2. Sideload an Android APK to a Meta Quest or VIVE Focus headset.
3. Run the exported Windows application and stream it to a headset.

## Configuration

External configuration files use this JSON format:

```json
{
  "serverIp": "10.34.64.208"
}
```

- `serverIp` specifies the address of the vitrivr server.
- The application reads `config.json` but never modifies it.
- If no `config.json` is found, `10.34.64.208` is used silently as the built-in
  default.

Media and log paths are not configurable because their names and locations are
determined internally.

### Runtime file locations

The application uses different physical locations depending on where it runs:

| Runtime | Configuration lookup order | Local media root | Log directory |
| --- | --- | --- | --- |
| Android headset | 1. `/sdcard/Android/data/VR.Museum/files/config.json`<br>2. built-in default | `/sdcard/Android/data/VR.Museum/files/media` | `/sdcard/Android/data/VR.Museum/files/logs` |
| Windows streaming | 1. `config.json` next to the `.exe`<br>2. built-in default | `media` next to the `.exe` | `logs` next to the `.exe` |

The built-in default is used only when none of the configuration files in the
applicable lookup order exists.

### Creation, copying, and overwrite behavior

| Operation | Configuration | Media | Logs |
| --- | --- | --- | --- |
| Android sideload<br>(`install.sh`/<br>`install.bat`) | `deployment/config.json` is copied to the external Android path, creating or overwriting the destination file. | If `deployment/media` exists, all of its contents are copied into the external Android `media` directory. Files at the same relative path are overwritten and files that exist only on the headset remain untouched. | The installation scripts do not create or modify log files. |
| Deployment from Godot | An existing external `config.json` remains in place and is used. If it is absent, the built-in default is used. | No project media is copied to the external Android `media` directory. Existing external media  remains in place during an APK update. | Godot deployment does not create or modify log files. |
| Startup on Android | Existing configuration file is read if present. Otherwise the built-in default is used. | The application creates `media` and its subfolder if missing. | The application creates `logs` if missing and creates or clears `.log` files. |
| Windows streaming startup | `config.json` next to the `.exe` is read if present. Otherwise, the built-in default is used. | The application creates `media` and its subfolder if missing. | The application creates `logs` next to the `.exe` if missing and creates or clears `.log` files. |

Running the application locally with F5/F6 inside the Godot editor is not supported.

An APK update preserves Android's external app files. A full uninstall
or clearing the app data can remove them. Changing the package ID makes the new
app use a different data area.

## 1. Direct Android deployment from Godot

If an external configuration from an earlier sideload exists at
`/sdcard/Android/data/VR.Museum/files/config.json`, the updated application reads
it. Otherwise, it uses the built-in default IP address.

Before deploying from Godot, select the correct export preset for the target VR
headset under **Project -> Export...** because the headset type cannot be detected automatically.

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
├── media/                  # copied to the headset when present
│   ├── images/
│   ├── videos/
│   └── 3d/
├── quest/
│   └── VR-Museum.apk
└── focus/
    └── VR-Museum.apk
```

Before running the scripts, ensure that `adb` is installed, the matching APK and
`deployment/config.json` exist, USB debugging is enabled, and the connected
headset is available. Edit the shared configuration and pass `quest` or `focus` to the installation script. The scripts install the matching APK and copy the configuration to:

`/sdcard/Android/data/VR.Museum/files/config.json`

On Windows:

```bat
install.bat quest
install.bat focus
```

On macOS or Linux:

```bash
./install.sh quest
./install.sh focus
```

The scripts perform these operations:

1. Install or update the APK using `adb install -r`.
2. Create the app-specific external files directory.
3. Copy `config.json` to the headset.
4. If `deployment/media` exists, recursively copy its contents to the headset's
   `media` directory.

After installation and the first application startup, the headset layout is:

```text
/sdcard/Android/data/VR.Museum/files/
├── config.json
├── media/        # created by the app, optionally populated from deployment/media
│   ├── images/
│   ├── videos/
│   └── 3d/
└── logs/         # created when the application starts
    ├── app.log
    └── app-readable.log
```

### Live Android logs

Start the application on the headset before opening the live log. On Windows:

```bat
logs.bat
```

On macOS or Linux:

```bash
./logs.sh
```

Both scripts show the last 100 readable log lines and then continue following
new lines.

## 3. Windows streaming build

Place the exported Windows application in the `stream` directory with this
layout:

```text
stream/
├── VR-Museum.exe
├── config.json
├── libgodotopenxrvendors.dll
├── media/        # created by the app, optionally populated
│   ├── images/
│   ├── videos/
│   └── 3d/
└── logs/         # created when the application starts
    ├── app.log
    └── app-readable.log
```
