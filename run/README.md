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
  "serverIp": "10.34.64.208",
  "tutorial": true,
  "query": "default"
}
```

- `serverIp` must be an IPv4 address in dotted-decimal notation without a protocol, port or path.
- `tutorial` is loaded as the configured tutorial flag.
- `query` is loaded as the configured query text and runs once after the first successful server validation.
- The application reads `config.json` but never modifies it.
- If no `config.json` is found, the built-in defaults are `10.34.64.208`, `tutorial: true` and `query: "default"`.
- Empty or invalid IPv4 values fall back to `10.34.64.208`, and empty query values fall back to `"default"`. Invalid JSON or incompatible value types cause the complete built-in configuration to be used.

Media and log paths are not configurable because their names and locations are
determined internally.

### Runtime file locations

The application uses different physical locations depending on where it runs:

| Runtime | Configuration lookup order | Local media root | Log directory |
| --- | --- | --- | --- |
| Android headset | 1. `/sdcard/Android/data/VR.Museum/files/config.json`<br>2. built-in default | `/sdcard/Android/data/VR.Museum/files/media` | `/sdcard/Android/data/VR.Museum/files/logs` |
| Windows streaming | 1. `config.json` in the parent directory of the `.exe` directory<br>2. built-in default | `media` in the parent directory of the `.exe` directory | `logs` next to the `.exe` |

The built-in default is used when no configuration file exists in the applicable lookup order or when the selected file cannot be deserialized.

### Creation, copying, and overwrite behavior

| Operation | Configuration | Media | Logs |
| --- | --- | --- | --- |
| Android sideload<br>(`install.sh`/<br>`install.bat`) | The shared `config.json` is copied to the external Android path, creating or overwriting the destination file. | If the shared `media` directory exists, the scripts create the remote `images`, `videos`, and `3d` directories before copying the local directories. Files at the same relative path are overwritten and files that exist only on the headset remain untouched. | The installation scripts do not create or modify log files. |
| Deployment from Godot | An existing external `config.json` remains in place and is used. If it is absent, the built-in default is used. | No project media is copied to the external Android `media` directory. Existing external media  remains in place during an APK update. | Godot deployment does not create or modify log files. |
| Startup on Android | Existing configuration file is read if present. Otherwise the built-in default is used. | The application creates `media` and its subfolder if missing. | The application creates `logs` if missing and creates or clears `.log` files. |
| Windows streaming startup | The shared `config.json` next to `stream` is read if present. Otherwise, the built-in default is used. | The application creates the shared `media` directory and its subfolders if missing. | The application creates `logs` next to the `.exe` if missing and creates or clears `.log` files. |

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

Configuration and media are shared by sideloaded and streamed builds. Quest and
Focus use separate APK directories, and Godot exports each preset directly to its matching directory:

```text
run/
├── config.json             # shared by sideloading and streaming
├── media/                  # shared locally and copied to Android
│   ├── images/
│   ├── videos/
│   └── 3d/
├── deployment/
│   ├── install.bat
│   ├── install.sh
│   ├── logs.bat
│   ├── logs.sh
│   ├── quest/
│   │   └── VR-Museum.apk
│   └── focus/
│       └── VR-Museum.apk
└── stream/
```

Before running the scripts, ensure that `adb` is installed, the matching APK and
`run/config.json` exist, USB debugging is enabled, and the connected headset is available. Edit the shared configuration and pass `quest` or `focus` to the installation script. The scripts install the matching APK and copy the configuration to:

`/sdcard/Android/data/VR.Museum/files/config.json`

The scripts resolve all paths from their own location, so they can be invoked
from any working directory. For example, from the repository root on Windows:

```bat
run\deployment\install.bat quest
run\deployment\install.bat focus
```

From the repository root on macOS or Linux:

```bash
./run/deployment/install.sh quest
./run/deployment/install.sh focus
```

Alternatively, first change to the script directory and run the shorter
commands:

```bash
cd run/deployment
./install.sh quest
```

The scripts perform these operations:

1. Install or update the APK using `adb install -r`.
2. Create the app-specific external files directory.
3. Copy `config.json` to the headset.
4. If `run/media` exists, create the remote `images`, `videos`, and `3d` directories and copy each corresponding local directory that exists.
5. Recursively grant read, write, and execute permissions (`777`) to the external app files directory and all its contents.

The scripts stop immediately if an installation, directory creation, copy, or permission operation fails.

After installation and the first application startup, the headset layout is:

```text
/sdcard/Android/data/VR.Museum/files/
├── config.json
├── media/        # created by the app, optionally populated from run/media
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

Place the exported Windows application in the `stream` directory. It reads the
same configuration and media directories used by the sideload scripts:

```text
run/
├── config.json
├── media/        # created by the app, shared with Android sideloading
│   ├── images/
│   ├── videos/
│   └── 3d/
└── stream/
    ├── VR-Museum.exe
    ├── libgodotopenxrvendors.dll
    └── logs/     # created when the application starts
        ├── app.log
        └── app-readable.log
```
