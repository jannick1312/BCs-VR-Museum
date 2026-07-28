# Media Pipeline

The pipeline prepares media inside `vitrivr-engine/sandbox/media/`. It creates a backup, normalizes files, filters unsupported 3D assets, creates Godot PCK files, and writes `pipeline_report.txt`. All required software is installed in [`../ubuntu/README.md`](../ubuntu/README.md).

## Is the complete pipeline required?

vitrivr can ingest media without running the complete pipeline with compatible images, videos, and GLB files.

However, the current Godot 3D flow requires one matching PCK file for every GLB. After a search result returns `3d/<name>.glb`, the frontend requests `3dPck/<name>.pck` from Nginx. `HelperScripts/06_build_godot_asset_packs.py` is the Python script that creates these files. This step should not be skipped unless the matching PCK files are produced another way.

If the files are already prepared and only the required PCK files are missing, run the PCK generator from `sandbox/media/`:

```bash
python3 HelperScripts/06_build_godot_asset_packs.py \
  --input-dir 3d \
  --output-dir 3dPck \
  --report-dir pck-report \
  --resume
```

## Processing details

`run_media_pipeline.py` executes all helper scripts in the required order. If the safety backup fails, it skips all media-changing stages and still writes a report explaining the failure.

### Script 01: Backup

`HelperScripts/01_backup_media.py` copies `3d/`, `images/`, and `videos/` into a timestamped `BACKUP/media_backup_<timestamp>/` directory. An existing `3dPck/` directory is backed up as well. The pipeline does not modify any media when this safety backup fails.

### Script 02: Images

`HelperScripts/02_normalize_images.py`:

- Converts supported input files to JPEG
- Applies EXIF orientation
- Limits both dimensions to 2048 pixels while preserving aspect ratio
- Colors to sRGB
- Removes metadata
- JPEG files at quality 90
- Installs each result atomically and removes the source after successful conversion

### Script 03: Videos

`HelperScripts/03_normalize_videos.py`:

- Converts supported input files to `.ogv`
- Limits the longest dimension to 2048 pixels
- Limits the output to 30 FPS
- Uses Theora video and Vorbis audio
- Keeps the first video stream and the optional first audio stream
- Removes subtitle and data streams
- Installs each result after successful conversion and then removes the source

### Script 04: 3D conversion and policy filters

`HelperScripts/04_convert_3d_assets.py` safely extracts each ZIP into a temporary directory, converts ZIP-packaged or loose glTF files to GLB with `gltf-transform`, validates the result, and never installs a partial GLB.

An 3d file is rejected when it contains any of the following:

- One or more animations
- A glTF `POINTS` primitive, meaning a point cloud
- More than **1,500,000 rendered triangles**
- Semantic glTF or GLB validation errors

### Script 05: 3D optimization

`HelperScripts/05_optimize_glb_assets.py`:

- Decompresses KTX2 or BasisU textures when required
- Resizes embedded JPEG and PNG textures to at most 2048×2048 pixels
- Preserves base mesh geometry, hierarchy, materials, UVs, vertex colors, metadata, and triangle count
- Verifies that the structure was preserved
- Validates the final GLB again
- Installs the accepted GLB atomically

Godot generates imported LOD and shadow-mesh data for the PCK, but the prepared base GLB remains unchanged.

### Script 06: Godot PCK generation

`HelperScripts/06_build_godot_asset_packs.py` runs the Godot editor with `--headless`. For each accepted GLB it:

1. Creates a temporary project from `HelperScripts/godot_pck_template/`
2. Imports the GLB into that project
3. Saves the imported `PackedScene` as `res://native/<name>.scn`
4. Exports one `<name>.pck`
5. Atomically installs the PCK in `3dPck/`

The PCK builder automatically discovers `godot`, `godot4`, `godot-mono`, and common application paths. Use `--godot` to provide a different editor binary explicitly.

### Script 07: 3D orchestration

`HelperScripts/07_process_3d_assets.py` coordinates scripts `04`, `05`, and `06`.

### Scripts 08 and 09: Final validation and report

`HelperScripts/08_validate_media_formats.py` checks that `3d/` contains only GLB files, `3dPck/` only PCK files, `images/` only JPG files, and `videos/` only OGV files.

`HelperScripts/09_pipeline_report.py` combines the runner data and detailed 3D results into `pipeline_report.txt`. The report contains the overall status, timestamps, backup location, step durations, starting and final inventories, rejection reasons, technical errors, and PCK results. It is written atomically and replaces the previous report.

## 1. Copy the pipeline

The original assets must already be in:

```text
/<pathToVitrivr>/vitrivr-engine/sandbox/media/
├── 3d/
├── images/
└── videos/
```

Copy the runner and helpers beside these directories:

```bash
cp /<pathTo>/tools/pipeline/run_media_pipeline.py \
  /<pathToVitrivr>/vitrivr-engine/sandbox/media/
cp -R /<pathTo>/tools/pipeline/HelperScripts \
  /<pathToVitrivr>/vitrivr-engine/sandbox/media/
```

## 2. Run the complete pipeline

```bash
cd /<pathToVitrivr>/vitrivr-engine/sandbox/media
python3 run_media_pipeline.py
```

## Input and output

| Directory | Accepted input | Final output |
|---|---|---|
| `images/` | JPG, JPEG, PNG, WebP, TIF, TIFF, BMP | JPG |
| `videos/` | MP4, MOV, MKV, AVI, WebM, M4V, OGV, OGG | OGV with Theora and Vorbis |
| `3d/` | ZIP-packaged glTF, loose glTF with its referenced files, GLB | Optimized GLB |
| `3dPck/` | Created by the pipeline | One PCK per accepted GLB |

Images and embedded textures are limited to 2048 pixels. Videos are limited to 2048 pixels and 30 FPS.

A 3D asset is rejected when it contains animations, point primitives, more than 1,500,000 rendered triangles and/or glTF validation errors.

Before modifying anything, the pipeline creates `BACKUP/media_backup_<timestamp>/`. Successfully converted or rejected sources are removed from the working directories but remain in the backup.

After a successful run, the layout is:

```text
sandbox/media/
├── 3d/                  # optimized GLB files
├── 3dPck/               # matching Godot PCK files
├── images/              # normalized JPG files
├── videos/              # normalized OGV files
├── BACKUP/
├── HelperScripts/
├── pipeline_report.txt
└── run_media_pipeline.py
```

## Verify

The runner already executes the final format validation. Check that the pipeline completed successfully in the report.

If the report shows a rejection or technical error, the reason and the affected files are listed.

Return to [`../vitrivr/README.md`](../vitrivr/README.md) for ingestion.
