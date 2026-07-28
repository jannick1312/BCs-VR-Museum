# Running and deploying

This directory contains the runtime configuration and helper scripts for the three supported ways of running the application:

1. Deploy directly from the Godot editor.
2. Sideload an Android APK to a Meta Quest or VIVE Focus headset.
3. Stream the Museum to a headset.

## Requirements

ADB is required only for direct Android deployment, APK sideloading, and Android logs. It is not required for Windows streaming.

Install ADB on macOS:

```bash
brew install android-platform-tools
adb version
```

Install ADB on Windows:

```powershell
winget install Google.PlatformTools
adb version
```

Enable developer mode and USB debugging on the headset, connect it to the computer, and accept the debugging prompt inside the headset.

## Obtaining the builds

Before sideloading or streaming, the application build must be available. Either export it from Godot using **Project -> Export...** and the `Quest`, `Focus`, or `Streaming` preset or download a packaged build from the [GitHub Releases page](https://github.com/jannick1312/BCs-VR-Museum/releases).

The export presets place the built files in the locations expected by the instructions and helper scripts:

- `Quest` exports to `run/deployment/quest/VR-Museum.apk`.
- `Focus` exports to `run/deployment/focus/VR-Museum.apk`.
- `Streaming` exports to `run/stream/VR-Museum.exe`.

When using a downloaded release, copy its files into the correct directory listed above.

## Configuration

External configuration files use this JSON format:

```json
{
  "serverIp": "10.34.64.208",
  "tutorial": true,
  "query": "default"
}
```

- `serverIp` must be an IPv4 address without a protocol, port or path.
- `tutorial` is loaded as the configured tutorial flag.
- `query` is loaded as the configured query text and runs once after the first successful server validation.
- If no `config.json` is found, the built-in defaults are `10.34.64.208`, `tutorial: true` and `query: "default"`.
- Invalid JSON or incompatible value types cause the complete built-in configuration to be used.

The configured server must be reachable from the computer or headset on vitrivr port `7070` and Nginx port `9090`.

### Runtime file locations

The application uses different physical locations depending on where it runs:

| Runtime | Configuration lookup order | Local media root | Log directory |
| --- | --- | --- | --- |
| Android headset | 1. `/sdcard/Android/data/VR.Museum/files/config.json`<br>2. built-in default | `/sdcard/Android/data/VR.Museum/files/media` | `/sdcard/Android/data/VR.Museum/files/logs` |
| Windows streaming | 1. `config.json` in the parent directory of the `.exe` directory<br>2. built-in default | `media` in the parent directory of the `.exe` directory | `logs` next to the `.exe` |

### Creation, copying, and overwrite behavior

| Operation | Configuration | Media | Logs |
| --- | --- | --- | --- |
| Android sideload<br>(`install.sh`/<br>`install.bat`) | Existing app data is deleted before the `config.json` is copied. | Existing remote media is removed and the local `images`, `videos`, and `3dPck` directories are copied from scratch. | Existing logs are removed when the app data is cleared. |
| Deployment from Godot | An existing external `config.json` remains in place and is used. If it is absent, the built-in default is used. | No project media is copied to the external Android `media` directory. Existing external media remains in place during an APK update. | Godot deployment does not create or modify log files. |
| Startup on Android | Existing configuration file is read if present. Otherwise the built-in default is used. | The application creates `media` and its subfolders if missing. | The application creates `logs` if missing and creates or clears `.log` files. |
| Windows streaming startup | The shared `config.json` next to `stream` is read if present. Otherwise, the built-in default is used. | The application creates the shared `media` directory and its subfolders if missing. | The application creates `logs` next to the `.exe` if missing and creates or clears `.log` files. |

Running the application locally inside the Godot editor is not supported. An APK update preserves Android's external app files. A full uninstall or a new sideload with the provided scripts removes the application data.

## Direct Android deployment from Godot

If an external configuration from an earlier sideload exists at `/sdcard/Android/data/VR.Museum/files/config.json`, the updated application reads it. Otherwise, it uses the built-in default IP address.

Before deploying from Godot, select the correct export preset for the target VR headset under **Project -> Export...** because the headset type cannot be detected automatically.

## Android APK sideload

Configuration and media are shared by sideloaded and streamed builds. Quest and Focus use separate APK directories, and Godot exports each preset directly to its matching directory:

```text
run/
├── config.json             # shared by sideloading and streaming
├── media/                  # shared locally and copied to Android
│   ├── images/
│   ├── videos/
│   └── 3dPck/
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

Before running the scripts, ensure that `adb` is installed, the matching APK and `run/config.json` exist, USB debugging is enabled, and the connected headset is available. Edit the shared configuration and pass `quest` or `focus` to the installation script. The scripts install the matching APK and copy the configuration.

The scripts resolve all paths from their own location, so they can be invoked from any working directory. For example, from the repository root on Windows:

```bat
run\deployment\install.bat quest
run\deployment\install.bat focus
```

Alternatively, first change to the script directory and run the shorter commands. On macOS or Linux:

```bash
cd run/deployment
./install.sh quest
```

The scripts perform these operations:

1. Install or update the APK using `adb install -r`.
2. Clear all existing application data, including configuration, media, and logs.
3. Recreate the app-specific external files directory.
4. Copy `config.json` to the headset.
5. If `run/media` exists, create the remote `images`, `videos`, and `3dPck` directories and copy each corresponding local directory that exists.
6. Grant read, write, and execute permissions (`777`) to the files directory and all its contents.

The scripts stop immediately if an installation, directory creation, or copy operation fails.

After installation and the first application startup, the headset layout is:

```text
/sdcard/Android/data/VR.Museum/files/
├── config.json
├── media/        # created by the app, optionally populated from run/media
│   ├── images/
│   ├── videos/
│   └── 3dPck/
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

Both scripts show the last 100 readable log lines and then continue following new lines.

## Windows streaming build

Place the exported Windows application in the `stream` directory. It reads the same configuration and media directories used by the sideload scripts:

```text
run/
├── config.json
├── media/        # shared with Android sideloading
│   ├── images/
│   ├── videos/
│   └── 3dPck/
└── stream/
    ├── VR-Museum.exe
    ├── libgodotopenxrvendors.dll
    └── logs/     # created when the application starts
        ├── app.log
        └── app-readable.log
```

For Meta Quest 3, connect the headset through Meta Horizon Link and set it as the active OpenXR runtime. For VIVE Focus 3, connect through VIVE Business Streaming and set SteamVR as the active OpenXR runtime.

After the headset is connected, run `VR-Museum.exe`.
