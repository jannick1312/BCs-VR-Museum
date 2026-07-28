#!/usr/bin/env python3
"""Run the complete image, video, and 3D media preparation pipeline."""

from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
import time
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


HERE = Path(__file__).resolve().parent
HELPERS = HERE / "HelperScripts"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--media-root",
        type=Path,
        default=HERE,
        help="Directory containing 3d/, images/, and videos/ (default: script directory)",
    )
    parser.add_argument("--godot", type=Path, help="Godot editor binary")
    parser.add_argument("--texture-workers", type=int, default=2)
    parser.add_argument("--pck-workers", type=int, default=1)
    parser.add_argument("--max-texture-size", type=int, default=2048)
    parser.add_argument("--max-triangles", type=int, default=1_500_000)
    parser.add_argument(
        "--overwrite-pcks",
        action="store_true",
        help="Rebuild PCK files that already exist",
    )
    args = parser.parse_args()
    if min(
        args.texture_workers,
        args.pck_workers,
        args.max_texture_size,
        args.max_triangles,
    ) <= 0:
        parser.error("worker counts and limits must be greater than zero")
    return args


def inventory(media_root: Path) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for folder in ("images", "videos", "3d", "3dPck"):
        root = media_root / folder
        files = (
            sorted(path for path in root.rglob("*") if path.is_file())
            if root.is_dir()
            else []
        )
        result[folder] = {
            "exists": root.is_dir(),
            "files": len(files),
            "formats": dict(
                sorted(
                    Counter(
                        path.suffix.lower() or "<no extension>" for path in files
                    ).items()
                )
            ),
        }
    return result


def run_step(
    name: str,
    script_name: str,
    media_root: Path,
    extra: list[str] | None = None,
) -> dict[str, Any]:
    script = HELPERS / script_name
    print(f"\n{'=' * 78}\n{name}\n{'=' * 78}", flush=True)
    started = time.monotonic()
    if not script.is_file():
        print(f"ERROR: Missing helper script: {script}", file=sys.stderr)
        return {
            "name": name,
            "status": "ERROR",
            "return_code": 1,
            "elapsed_seconds": 0.0,
            "detail": f"Missing helper script: {script}",
        }

    command = [
        sys.executable,
        str(script),
        "--media-root",
        str(media_root),
        *(extra or []),
    ]
    return_code = subprocess.run(command).returncode
    status = "OK" if return_code == 0 else "ERROR"
    elapsed = round(time.monotonic() - started, 3)
    print(f"\n[{status}] {name} ({elapsed:.1f}s)", flush=True)
    return {
        "name": name,
        "status": status,
        "return_code": return_code,
        "elapsed_seconds": elapsed,
    }


def write_run_data(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.tmp")
    temporary.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


def main() -> int:
    args = parse_args()
    media_root = args.media_root.expanduser().resolve()
    if not media_root.is_dir():
        print(f"ERROR: Media root is not a directory: {media_root}", file=sys.stderr)
        return 2
    started_at = datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds")
    run_data_path = media_root / ".media_pipeline_work" / "run_data.json"
    before_backups = set((media_root / "BACKUP").glob("media_backup_*"))

    print("Integrated Media Preparation Pipeline")
    print(f"Media root: {media_root}")
    print("Order: backup -> images -> videos -> 3D/GLB/PCK -> validation -> report")

    run_data: dict[str, Any] = {
        "started_at": started_at,
        "settings": {
            "max_texture_size": args.max_texture_size,
            "max_triangles": args.max_triangles,
            "texture_workers": args.texture_workers,
            "pck_workers": args.pck_workers,
            "overwrite_pcks": args.overwrite_pcks,
        },
        "initial_inventory": inventory(media_root),
        "steps": [],
    }

    backup = run_step(
        "Create timestamped backup",
        "01_backup_media.py",
        media_root,
    )
    run_data["steps"].append(backup)

    if backup["status"] == "OK":
        run_data["steps"].append(
            run_step(
                "Normalize all images to JPG (max 2048 px)",
                "02_normalize_images.py",
                media_root,
            )
        )
        run_data["steps"].append(
            run_step(
                "Normalize all videos to OGV/Theora (max 2048 px, 30 FPS)",
                "03_normalize_videos.py",
                media_root,
            )
        )
        three_d_args = [
            "--texture-workers",
            str(args.texture_workers),
            "--pck-workers",
            str(args.pck_workers),
            "--max-texture-size",
            str(args.max_texture_size),
            "--max-triangles",
            str(args.max_triangles),
        ]
        if args.godot:
            three_d_args.extend(["--godot", str(args.godot)])
        if args.overwrite_pcks:
            three_d_args.append("--overwrite-pcks")
        run_data["steps"].append(
            run_step(
                "Convert, filter, optimize, and package all 3D assets",
                "07_process_3d_assets.py",
                media_root,
                three_d_args,
            )
        )
        run_data["steps"].append(
            run_step(
                "Validate final media formats",
                "08_validate_media_formats.py",
                media_root,
            )
        )
    else:
        for name in (
            "Normalize all images",
            "Normalize all videos",
            "Process all 3D assets",
            "Validate final media formats",
        ):
            run_data["steps"].append(
                {
                    "name": name,
                    "status": "SKIPPED",
                    "return_code": None,
                    "elapsed_seconds": 0.0,
                    "detail": "Skipped because the safety backup failed.",
                }
            )

    after_backups = set((media_root / "BACKUP").glob("media_backup_*"))
    created_backups = sorted(after_backups - before_backups)
    run_data["backup_directory"] = (
        str(created_backups[-1]) if created_backups else ""
    )
    run_data["finished_at"] = datetime.now(timezone.utc).astimezone().isoformat(
        timespec="seconds"
    )
    run_data["final_inventory"] = inventory(media_root)
    write_run_data(run_data_path, run_data)

    report = run_step(
        "Write consolidated pipeline report",
        "09_pipeline_report.py",
        media_root,
        ["--run-data", str(run_data_path)],
    )
    run_data["steps"].append(report)
    work_root = run_data_path.parent
    if work_root.exists():
        shutil.rmtree(work_root)

    failed = any(step["status"] == "ERROR" for step in run_data["steps"])
    print(f"\n{'=' * 78}\nPipeline result\n{'=' * 78}")
    print(f"Report: {media_root / 'pipeline_report.txt'}")
    if failed:
        print("Completed with errors. Read pipeline_report.txt for details.")
        return 1
    print("Completed successfully.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
