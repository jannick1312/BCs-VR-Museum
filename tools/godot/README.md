# Godot VR Museum Development

This guide explains how to open, export, and test the Godot frontend. The application queries vitrivr on port `7070` and downloads media from Nginx on port `9090`.

Open the existing project at:

```text
/<pathTo>/src/GodotMuseum/project.godot
```

Replace `/<pathTo>` with the absolute path to this cloned repository.

## Tested development environment

The versions below describe the environment used to verify this project.

| Component | Version |
|---|---|
| Android Studio | Panda 4, 2025.3.4 |
| Android Emulator | 36.5.11 |
| Android SDK Build-Tools | Not recorded |
| Android SDK Platform | Android 16, API 36.1 |
| Android SDK Platform-Tools | 37.0.0 |
| Sources for Android | Not recorded |
| Godot with .NET support | 4.6.2 |
| .NET SDK | 10.0.203 |
| Target headsets | Meta Quest 3 and VIVE Focus 3 |

The C# project targets .NET 10 for the editor and Windows, and .NET 9 for Android deployment. OpenXR, Godot XR Tools, the OpenXR vendor plugin, and the action map are already configured in the repository.

## 1. Install the development tools

Install [Android Studio](https://developer.android.com/studio) and use its SDK Manager to install the SDK components listed above.

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

Install [.NET SDK 10.0.203](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) and [Godot 4.6.2](https://godotengine.org/download/archive/4.6.2-stable/). Select the Godot build with .NET support.

## 2. Open the project

Import `src/GodotMuseum/project.godot` in Godot and wait for the initial import to finish. You can ignore warnings about missing or incorrect IDs during this first import.

## 3. Configure Android export

In `Godot -> Editor Settings -> Export -> Android`, configure the Java and Android SDK paths:

```text
Java SDK:    /<pathToApplications>/Android Studio.app/Contents/jbr/Contents/Home
Android SDK: /<pathToLibrary>/Android/sdk
```

Open `Project -> Install Android Build Template...` and install the template.

## 4. Use the export presets

| Preset | Target | OpenXR loader | Default output |
|---|---|---|---|
| `Quest` | Meta Quest, Android arm64 | Meta | `run/deployment/quest/VR-Museum.apk` |
| `Focus` | VIVE Focus, Android arm64 | Khronos with HTC support | `run/deployment/focus/VR-Museum.apk` |
| `Streaming` | Windows x86_64 | PC OpenXR runtime | `run/stream/VR-Museum.exe` |

To deploy directly to a headset, enable **Runnable** for the corresponding preset in `Project -> Export...`. Alternatively, export the APK and sideload it.

## 5. Run directly on a headset

Enable developer mode and USB debugging on the headset, connect it with a USB-C cable and accept the debugging prompt inside the headset.

### 5.1 Meta Quest 3

Enable **Runnable** for the `Quest` preset and use Godot's one-click deployment to build, install, and launch the application. To install a prepared APK together with its runtime configuration and media, follow the Quest sideload procedure in [`../../run/README.md`](../../run/README.md).

### 5.2 VIVE Focus 3

Enable **Runnable** for the `Focus` preset and use Godot's one-click deployment to build, install, and launch the application. To install a prepared APK together with its runtime configuration and media, follow the Focus sideload procedure in [`../../run/README.md`](../../run/README.md).

## 6. Stream the Windows build

Export the `Streaming` preset and place the resulting files in `run/stream/`. The complete runtime layout is documented in [`../../run/README.md`](../../run/README.md).

### 6.1 Meta Quest 3 with Meta Horizon Link

1. Install [Meta Horizon Link](https://www.meta.com/quest/setup/) on a compatible Windows PC.
2. Connect the headset using a suitable USB-C cable.
3. Set Meta Horizon Link as the active OpenXR runtime.
4. Start `run/stream/VR-Museum.exe`.

### 6.2 VIVE Focus 3 with VIVE Business Streaming

1. Install [VIVE Business Streaming](https://business.vive.com/us/solutions/streaming/) on the Windows PC and its client on the headset. Install Steam and SteamVR on the PC as well.
2. Connect the headset by USB or Wi-Fi and activate VIVE Business Streaming.
3. Set SteamVR as the current OpenXR runtime.
4. Start `run/stream/VR-Museum.exe`.

## Verify

Before Android deployment, confirm:

```bash
adb version
adb devices
```

For the complete deployment instructions, see [`../../run/README.md`](../../run/README.md).
