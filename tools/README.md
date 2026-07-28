# Development and Reproduction Guide

This directory contains the complete step-by-step setup for reproducing the backend, preparing the media, and developing the Godot frontend.

Replace `/<pathTo>` with the absolute path to this cloned repository. Replace `/<pathToVitrivr>` with the absolute path where the vitrivr backend workspace should be created. The two cloned vitrivr repositories and the service scripts are placed directly inside that path.

## Guides

| Guide | Purpose |
|---|---|
| [`ubuntu/README.md`](ubuntu/README.md) | Install the Ubuntu server dependencies |
| [`vitrivr/README.md`](vitrivr/README.md) | Build, configure, start, reset, and ingest with vitrivr |
| [`pipeline/README.md`](pipeline/README.md) | Prepare images, videos, 3D models, and Godot PCK files |
| [`nginX/README.md`](nginX/README.md) | Serve prepared media on port `9090` |
| [`godot/README.md`](godot/README.md) | Open, build, and deploy the Godot frontend |
| [`../run/README.md`](../run/README.md) | Install builds, copy runtime data, and inspect logs |

## Complete contents

### [Ubuntu Server Setup](ubuntu/README.md)

- [1. Install the Ubuntu packages](ubuntu/README.md#1-install-the-ubuntu-packages)
- [2. Install PostgreSQL 17 and pgvector](ubuntu/README.md#2-install-postgresql-17-and-pgvector)
- [3. Install Node.js 22 and glTF-Transform](ubuntu/README.md#3-install-nodejs-22-and-gltf-transform)
- [4. Install KTX-Software 4.4.2](ubuntu/README.md#4-install-ktx-software-442)
- [5. Install Godot 4.6.2](ubuntu/README.md#5-install-godot-462)
- [Verify](ubuntu/README.md#verify)

### [vitrivr Backend](vitrivr/README.md)

- [File destinations](vitrivr/README.md#file-destinations)
- [1. Clone and build](vitrivr/README.md#1-clone-and-build)
- [2. Prepare the Descriptor Server](vitrivr/README.md#2-prepare-the-descriptor-server)
- [3. Configure PostgreSQL](vitrivr/README.md#3-configure-postgresql)
- [4. Copy the configuration and scripts](vitrivr/README.md#4-copy-the-configuration-and-scripts)
- [5. Create the sandbox](vitrivr/README.md#5-create-the-sandbox)
- [6. Prepare the media](vitrivr/README.md#6-prepare-the-media)
- [7. Start and ingest](vitrivr/README.md#7-start-and-ingest)
- [Verify](vitrivr/README.md#verify)

### [Media Pipeline](pipeline/README.md)

- [Is the complete pipeline required?](pipeline/README.md#is-the-complete-pipeline-required)
- [Processing details](pipeline/README.md#processing-details)
  - [Script 01: Backup](pipeline/README.md#script-01-backup)
  - [Script 02: Images](pipeline/README.md#script-02-images)
  - [Script 03: Videos](pipeline/README.md#script-03-videos)
  - [Script 04: 3D conversion and policy filters](pipeline/README.md#script-04-3d-conversion-and-policy-filters)
  - [Script 05: 3D optimization](pipeline/README.md#script-05-3d-optimization)
  - [Script 06: Godot PCK generation](pipeline/README.md#script-06-godot-pck-generation)
  - [Script 07: 3D orchestration](pipeline/README.md#script-07-3d-orchestration)
  - [Scripts 08 and 09: Final validation and report](pipeline/README.md#scripts-08-and-09-final-validation-and-report)
- [1. Copy the pipeline](pipeline/README.md#1-copy-the-pipeline)
- [2. Run the complete pipeline](pipeline/README.md#2-run-the-complete-pipeline)
- [Input and output](pipeline/README.md#input-and-output)
- [Verify](pipeline/README.md#verify)

### [Nginx Media Server](nginX/README.md)

- [1. Create the media site](nginX/README.md#1-create-the-media-site)
- [2. Configure the port](nginX/README.md#2-configure-the-port)
- [Verify](nginX/README.md#verify)

### [Godot VR Museum Development](godot/README.md)

- [Tested development environment](godot/README.md#tested-development-environment)
- [1. Install the development tools](godot/README.md#1-install-the-development-tools)
- [2. Open the project](godot/README.md#2-open-the-project)
- [3. Configure Android export](godot/README.md#3-configure-android-export)
- [4. Use the export presets](godot/README.md#4-use-the-export-presets)
- [5. Run directly on a headset](godot/README.md#5-run-directly-on-a-headset)
  - [5.1 Meta Quest 3](godot/README.md#51-meta-quest-3)
  - [5.2 VIVE Focus 3](godot/README.md#52-vive-focus-3)
- [6. Stream the Windows build](godot/README.md#6-stream-the-windows-build)
  - [6.1 Meta Quest 3 with Meta Horizon Link](godot/README.md#61-meta-quest-3-with-meta-horizon-link)
  - [6.2 VIVE Focus 3 with VIVE Business Streaming](godot/README.md#62-vive-focus-3-with-vive-business-streaming)
- [Verify](godot/README.md#verify)

### [Running and deploying](../run/README.md)

- [Requirements](../run/README.md#requirements)
- [Obtaining the builds](../run/README.md#obtaining-the-builds)
- [Configuration](../run/README.md#configuration)
  - [Runtime file locations](../run/README.md#runtime-file-locations)
  - [Creation, copying, and overwrite behavior](../run/README.md#creation-copying-and-overwrite-behavior)
- [Direct Android deployment from Godot](../run/README.md#direct-android-deployment-from-godot)
- [Android APK sideload](../run/README.md#android-apk-sideload)
  - [Live Android logs](../run/README.md#live-android-logs)
- [Windows streaming build](../run/README.md#windows-streaming-build)

## Reproduction order

1. Complete [`ubuntu/README.md`](ubuntu/README.md) on the Ubuntu server.
2. Follow steps 1–5 in [`vitrivr/README.md`](vitrivr/README.md) to clone the needed repositories, configure the services, configure the database, and create the sandbox directories.
3. Copy the original assets into the matching `sandbox/media/3d`, `sandbox/media/images`, and `sandbox/media/videos` directories.
4. Copy and run the scripts from [`pipeline/README.md`](pipeline/README.md).
5. Return to step 7 in [`vitrivr/README.md`](vitrivr/README.md). Start both services, initialize the `sandbox` schema and run the 3D, image, and video extraction pipelines.
6. Configure Nginx using [`nginX/README.md`](nginX/README.md).
7. Open, export, and test the frontend using [`godot/README.md`](godot/README.md).
8. Use [`../run/README.md`](../run/README.md) to configure and install an exported or prebuilt application.
