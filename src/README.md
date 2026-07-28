# Source Code Architecture

This directory contains all C# and Godot code for VR Museum. Godot handles the VR application. The other projects handle search, vitrivr, media files, and logging.

The Godot project root is [`GodotMuseum/`](GodotMuseum/). Open [`GodotMuseum/project.godot`](GodotMuseum/project.godot) in Godot to open the project in the editor.

## Table of contents

- [Project structure](#project-structure)
- [Project dependencies](#project-dependencies)
- [Source projects](#source-projects)
  - [Models](#models)
  - [Core](#core)
  - [Application.Abstractions](#applicationabstractions)
  - [Application](#application)
  - [Infrastructure.Vitrivr](#infrastructurevitrivr)
  - [Infrastructure.Media](#infrastructuremedia)
  - [Application.Factory](#applicationfactory)
  - [Logger](#logger)
  - [GodotMuseum](#godotmuseum)
- [Godot scene organization](#godot-scene-organization)
- [Runtime flows](#runtime-flows)
  - [Application startup](#application-startup)
  - [Server validation and museum entry](#server-validation-and-museum-entry)
  - [Text and similarity search](#text-and-similarity-search)
  - [Media resolution and loading](#media-resolution-and-loading)
  - [Media placement](#media-placement)
- [XR interaction](#xr-interaction)
- [Runtime files and logging](#runtime-files-and-logging)
- [Development](#development)

## Project structure

```text
src/
├── Application.Abstractions/     # Frontend application contract
├── Application.Factory/          # Connection of application and infrastructure
├── Application/                  # Search and validation use cases
├── Core/                         # Models for queries and search results
├── Infrastructure.Media/         # Local media getter, HTTP loading
├── Infrastructure.Vitrivr/       # vitrivr requests, parsing, health checks
├── Logger/                       # Logger for front and backend
├── Models/                       # Models and enums for front and backend
├── GodotMuseum/                  # Godot project, XR scenes, UI, assets, and controllers
└── README.md                     # This file
```

Each directory is a separate .NET project. [`GodotMuseum/BCS-VR-Museum.sln`](GodotMuseum/BCS-VR-Museum.sln) opens all nine projects together.

## Project dependencies

```text
GodotMuseum
├── Application.Abstractions
├── Application.Factory
├── Models
└── Logger

Application.Factory
├── Application.Abstractions
├── Application
├── Infrastructure.Vitrivr
└── Infrastructure.Media

Application.Abstractions
└── Models

Application
├── Application.Abstractions
├── Core
├── Models
└── Logger

Infrastructure.Vitrivr
├── Application
├── Core
├── Models
└── Logger

Infrastructure.Media
├── Application
├── Core
├── Models
└── Logger

Core
└── Models
```

The shared projects support .NET 9 and .NET 10. Godot uses .NET 10 for the editor and Windows and .NET 9 for Android.

## Source projects

### Models

[`Models/`](Models/) contains the media types used by the application and Godot:

| Type | Responsibility |
|---|---|
| [`MediaType`](Models/MediaType.cs) | Identifies images, videos, and 3D objects |
| [`MediaMode`](Models/MediaMode.cs) | Selects 2D media, 3D objects, or both |
| [`DisplayMediaItem`](Models/DisplayMediaItem.cs) | Stores a loaded file, its name, type, and CLIP vector |
| [`DisplayMediaResult`](Models/DisplayMediaResult.cs) | Stores loaded results or an error |

### Core

[`Core/`](Core/) contains the basic search types:

| Type | Responsibility |
|---|---|
| [`SearchQuery`](Core/SearchQuery.cs) | Base type for searches with a result limit |
| [`TextSearchQuery`](Core/TextSearchQuery.cs) | Stores a text query |
| [`VectorSearchQuery`](Core/VectorSearchQuery.cs) | Stores a vector for similarity search |
| [`SearchResultItem`](Core/SearchResultItem.cs) | Stores a vector, media type, local path, and remote URL |
| [`SearchResult`](Core/SearchResult.cs) | Stores search results or an error |
| [`MediaContent`](Core/MediaContent.cs) | Stores a loaded media path or an error |

### Application.Abstractions

[`Application.Abstractions/IMuseumApplication.cs`](Application.Abstractions/IMuseumApplication.cs) is the interface used by Godot. It provides:

- text search
- vector similarity search
- completion of the current media placement
- backend reachability validation

Godot can use these functions without knowing how vitrivr or media loading works.

### Application

[`Application/`](Application/) contains the search and validation logic:

| Component | Responsibility |
|---|---|
| [`ISearchEngine`](Application/ISearchEngine.cs) | Interface for a search backend |
| [`IMediaLoader`](Application/IMediaLoader.cs) | Interface for loading media |
| [`IServerHealthService`](Application/IServerHealthService.cs) | Interface for checking the server |
| [`SearchMedia`](Application/SearchMedia.cs) | Searches, filters results, and loads the required files |
| [`ValidateServer`](Application/ValidateServer.cs) | Checks whether the backend is reachable |
| [`MuseumApplication`](Application/MuseumApplication.cs) | Main application object used by Godot |

`SearchMedia` loads only as many results as the museum can display. Images and videos share the wall capacity. 3D models use the available object stands.

### Infrastructure.Vitrivr

[`Infrastructure.Vitrivr/`](Infrastructure.Vitrivr/) handles communication with vitrivr:

| Component | Responsibility |
|---|---|
| [`VitrivrSettings`](Infrastructure.Vitrivr/VitrivrSettings.cs) | Creates the required URLs from the server IP |
| [`VitrivrQueryInput`](Infrastructure.Vitrivr/VitrivrQueryInput.cs) | Converts text and vectors into vitrivr inputs |
| [`VitrivrRequestFactory`](Infrastructure.Vitrivr/VitrivrRequestFactory.cs) | Builds the vitrivr search request |
| [`VitrivrSearchService`](Infrastructure.Vitrivr/VitrivrSearchService.cs) | Sends search requests to port `7070` |
| [`VitrivrResponseParser`](Infrastructure.Vitrivr/VitrivrResponseParser.cs) | Reads file paths, media types, and CLIP vectors from the response |
| [`VitrivrServerHealthService`](Infrastructure.Vitrivr/VitrivrServerHealthService.cs) | Checks whether vitrivr is reachable |

vitrivr indexes a 3D model as a GLB. Godot needs the matching PCK at runtime. The response parser therefore changes `3d/<name>.glb` to `3dPck/<name>.pck`.

### Infrastructure.Media

[`Infrastructure.Media/`](Infrastructure.Media/) loads the files returned by a search:

| Component | Responsibility |
|---|---|
| [`MediaLoader`](Infrastructure.Media/MediaLoader.cs) | Uses a local file or downloads it from Nginx |
| [`MediaStore`](Infrastructure.Media/MediaStore.cs) | Keeps the current and next downloaded result set |

Before loading new results, `MediaStore` creates an empty `next` directory. After loading, `current` and `next` are swapped. The old files are deleted only after Godot has placed the new exhibits. If a search fails, the current exhibition remains visible.

### Application.Factory

[`Application.Factory/MuseumApplicationFactory.cs`](Application.Factory/MuseumApplicationFactory.cs) creates and connects:

- `VitrivrSearchService`
- `VitrivrServerHealthService`
- `MediaLoader`
- `MuseumApplication`

[`GodotMuseum/SearchUseCaseFactory.cs`](GodotMuseum/SearchUseCaseFactory.cs) reads the current settings and uses this factory for the Godot UI.

### Logger

[`Logger/EventLogger.cs`](Logger/EventLogger.cs) writes two log files:

- `app.log` contains one JSON object per line
- `app-readable.log` contains aligned human-readable messages

The timestamps show the time since the application started.

### GodotMuseum

[`GodotMuseum/`](GodotMuseum/) is the actual Godot project:

```text
GodotMuseum/
├── main.tscn                    # Main scene
├── project.godot                # Godot and OpenXR settings
├── export_presets.cfg           # Quest, Focus, and Windows exports
├── Menu/                        # Menu scenes
├── Menu-Scripts/                # Menu logic
├── Museum/                      # Museum scenes and display places
├── Museum-Scripts/              # Search and media placement
├── Player/                      # XR player, hands, controllers, and HUD
├── Keyboard/                    # Virtual keyboard
├── Tutorial/                    # Tutorial scenes and slides
├── Assets/                      # Materials, textures, and icons
└── addons/                      # Godot XR Tools and OpenXR plugins
```

## Godot scene organization

[`GodotMuseum/main.tscn`](GodotMuseum/main.tscn) is the main scene:

| Node | Responsibility |
|---|---|
| `XrRenderProfile` | Selects the XR graphics settings |
| `StartXR` | Starts the OpenXR runtime |
| `SearchSettingsStore` | Loads configuration, paths, and logging |
| `GameSettingsStore` | Stores hand-tracking and media settings |
| `SearchUseCaseFactory` | Creates the museum application |
| `PlatformSwitcher` | Switches between the menu and museum |
| `WorldEnvironment` | Stores the current environment |
| `Player` | Contains the XR camera, hands, controllers, movement, and HUD |
| `MuseumNode` | Contains the room, search, exhibits, and display places |
| `MenuNode` | Contains the tutorial, keyboard, and settings |

The menu and museum stay loaded at the same time. `PlatformSwitcher` shows one of them and moves the player to the correct position.

## Runtime flows

### Application startup

1. `main.tscn` loads the main scene.
2. `SearchSettingsStore` configures logging, loads `config.json` or the default settings, and creates the runtime media directories.
3. `XrRenderProfile` selects the XR quality settings for Quest, Focus, or Windows streaming.
4. `StartXR` starts OpenXR.
5. `PlatformSwitcher` disables museum movement and places the user in the menu.

### Server validation and museum entry

[`GodotMuseum/ServerValidationController.cs`](GodotMuseum/ServerValidationController.cs) validates the IPv4 address and then checks whether vitrivr is reachable. A new check cancels the previous one.

[`GodotMuseum/MuseumEntryState.cs`](GodotMuseum/MuseumEntryState.cs) allows entry only when the configured vitrivr server is reachable and the tutorial is disabled or has been completed.

### Text and similarity search

1. The user enters text or selects an exhibit for a similarity search.
2. `SearchController` sends the query through `IMuseumApplication`.
3. `SearchMedia` applies the selected media mode and the available museum capacity.
4. `VitrivrSearchService` sends the query to `POST /api/sandbox/query`.
5. `VitrivrResponseParser` reads the returned file paths, media types, and CLIP vectors.
6. `MediaLoader` uses local files or downloads missing files from Nginx.
7. The loaded files are returned as a `DisplayMediaResult`.
8. `MediaPlacementController` places the results in the museum.

### Media resolution and loading

The response parser creates a local path and an Nginx URL for every result.

`MediaLoader` tries using the local file first. If it is missing, the file is downloaded from `http://<serverIp>:9090/`.

### Media placement

[`GodotMuseum/Museum-Scripts/Placement/MediaPlacementController.cs`](GodotMuseum/Museum-Scripts/Placement/MediaPlacementController.cs) separates 2D and 3D results and places them in the museum.

The 2D strategy:

- finds the available wall places
- reads the maximum item count from the numeric parent node name and uses `2` by default
- randomly selects between `1` and this maximum for each wall place, limited by the remaining results
- creates centered display slots
- keeps the correct image and video proportions
- creates frames and collisions
- controls videos based on the player distance

The 3D strategy:

- opens the downloaded PCK
- loads `res://native/<name>.scn`
- creates the 3D scene
- reads the object size
- scales and places the object on a pillar
- stores the original size

Each exhibit stores its file path, name, and CLIP vector. **Similar** starts a new search with this vector. A 3D exhibit also provides **Original Size**.

## XR interaction

The player supports controllers and hand tracking:

- Controllers provide movement, turning, pickup, pointer, menu, and search input.
- Hand tracking is used when OpenXR reports an active hand profile.
- Fallback hand models are used when no tracking mesh is available.
- Pinch and grip gestures control the pointer, pickup, and movement.
- The left-hand input area appears while gripping in the museum.

[`GodotMuseum/Player/PlayerHandInput.cs`](GodotMuseum/Player/PlayerHandInput.cs) handles input mode, gestures, movement, and hand models. [`GodotMuseum/PlatformSwitcher.cs`](GodotMuseum/PlatformSwitcher.cs) disables movement while the user is in the menu.

## Runtime files and logging

Android and Windows use different file locations:

| Runtime | Configuration | Media | Logs |
|---|---|---|---|
| Android | `/sdcard/Android/data/VR.Museum/files/config.json` | `/sdcard/Android/data/VR.Museum/files/media/` | `/sdcard/Android/data/VR.Museum/files/logs/` |
| Windows | `config.json` in the shared directory above the executable directory | `media/` in the shared directory above the executable directory | `logs/` next to the executable |

See the [runtime configuration](../run/README.md#configuration) for the full file behavior.

## Development

Use these guides for setup and deployment:

- [`../tools/godot/README.md`](../tools/godot/README.md) covers Godot, .NET, Android, OpenXR, and exports.
- [`../run/README.md`](../run/README.md) covers configuration, builds, installation, media, and logs.
- [`../tools/README.md`](../tools/README.md) covers the complete backend and frontend setup.
