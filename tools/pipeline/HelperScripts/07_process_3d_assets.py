#!/usr/bin/env python3
"""Run the integrated ZIP/glTF/GLB to optimized GLB and Godot PCK pipeline."""

from __future__ import annotations

import argparse
import csv
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Any


HERE = Path(__file__).resolve().parent
DEFAULT_MAX_TRIANGLES = 1_500_000


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--media-root", default=".", type=Path)
    parser.add_argument("--godot", type=Path, help="Godot editor binary")
    parser.add_argument("--texture-workers", type=int, default=2)
    parser.add_argument("--pck-workers", type=int, default=1)
    parser.add_argument("--max-texture-size", type=int, default=2048)
    parser.add_argument("--max-triangles", type=int, default=DEFAULT_MAX_TRIANGLES)
    parser.add_argument("--only", help="Process one ZIP, glTF, or GLB filename")
    parser.add_argument(
        "--overwrite",
        action="store_true",
        help="Reconvert matching sources and rebuild matching PCKs",
    )
    parser.add_argument(
        "--overwrite-pcks",
        action="store_true",
        help="Rebuild matching PCKs without forcing source reconversion",
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


def run(command: list[str]) -> bool:
    print("$", " ".join(command), flush=True)
    return subprocess.run(command).returncode == 0


def read_json(path: Path, fallback: Any) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError):
        return fallback


def read_csv(path: Path) -> list[dict[str, str]]:
    if not path.is_file():
        return []
    with path.open(newline="", encoding="utf-8") as file:
        return list(csv.DictReader(file))


def install_source(source: Path, destination: Path) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists() or destination.is_symlink():
        destination.unlink()
    try:
        os.link(source, destination)
    except OSError:
        shutil.copy2(source, destination)


def install_atomic(source: Path, destination: Path) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    temporary = destination.with_name(f".{destination.name}.pipeline-tmp")
    temporary.unlink(missing_ok=True)
    shutil.copy2(source, temporary)
    os.replace(temporary, destination)


def selected(path: Path, only: str | None) -> bool:
    return not only or path.stem == Path(only).stem


def rejection_categories(detail: str) -> list[str]:
    lowered = detail.lower()
    categories = []
    if "animation" in lowered:
        categories.append("ANIMATION")
    if "point cloud" in lowered or "points primitive" in lowered:
        categories.append("POINT_CLOUD")
    if "triangle limit" in lowered:
        categories.append("TRIANGLE_LIMIT")
    if "validation failed" in lowered or "invalid glb" in lowered:
        categories.append("VALIDATION")
    return categories or ["OTHER_POLICY"]


def write_json_atomic(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.tmp")
    temporary.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    os.replace(temporary, path)


def main() -> int:
    args = parse_args()
    media_root = args.media_root.expanduser().resolve()
    model_dir = media_root / "3d"
    pck_dir = media_root / "3dPck"
    work_dir = media_root / ".media_pipeline_work" / "3d"
    converted_dir = work_dir / "converted"
    optimized_dir = work_dir / "optimized"
    conversion_report = work_dir / "conversion_results.json"
    optimization_report = optimized_dir / "results.csv"
    godot_report_dir = work_dir / "godot"
    result_report = work_dir.parent / "3d_pipeline_results.json"

    if not model_dir.is_dir():
        print(f"ERROR: Missing directory: {model_dir}", file=sys.stderr)
        return 1

    if work_dir.exists():
        shutil.rmtree(work_dir)
    converted_dir.mkdir(parents=True)
    optimized_dir.mkdir(parents=True)
    pck_dir.mkdir(parents=True, exist_ok=True)
    (pck_dir / "manifest.csv").unlink(missing_ok=True)
    for legacy_directory in ("logs", "diagnostics"):
        legacy_path = pck_dir / legacy_directory
        if legacy_path.is_dir():
            shutil.rmtree(legacy_path)

    zip_sources = sorted(
        path for path in model_dir.glob("*.zip") if selected(path, args.only)
    )
    gltf_sources = sorted(
        path for path in model_dir.glob("*.gltf") if selected(path, args.only)
    )
    glb_sources = sorted(
        path for path in model_dir.glob("*.glb") if selected(path, args.only)
    )
    starting = {
        "zip": len(zip_sources),
        "gltf": len(gltf_sources),
        "glb": len(glb_sources),
    }

    for source in glb_sources:
        install_source(source, converted_dir / source.name)

    conversion_command = [
        sys.executable,
        str(HERE / "04_convert_3d_assets.py"),
        "--source-dir",
        str(model_dir),
        "--output-dir",
        str(converted_dir),
        "--keep-sources",
        "--max-triangles",
        str(args.max_triangles),
        "--report",
        str(conversion_report),
    ]
    if args.overwrite:
        conversion_command.append("--overwrite")
    if args.only:
        conversion_command.extend(["--only", args.only])
    conversion_ok = run(conversion_command)
    conversion_payload = read_json(conversion_report, {"results": []})
    conversion_results = conversion_payload.get("results", [])

    converted_sources = sorted(converted_dir.glob("*.glb"))
    optimization_ok = True
    if converted_sources:
        optimization_command = [
            sys.executable,
            str(HERE / "05_optimize_glb_assets.py"),
            "--input-dir",
            str(converted_dir),
            "--output-dir",
            str(optimized_dir),
            "--texture-format",
            "source",
            "--max-texture-size",
            str(args.max_texture_size),
            "--max-triangles",
            str(args.max_triangles),
            "--workers",
            str(args.texture_workers),
            "--overwrite",
        ]
        if args.only:
            optimization_command.extend(
                ["--only", Path(args.only).with_suffix(".glb").name]
            )
        optimization_ok = run(optimization_command)
    else:
        print("No GLB reached the optimization stage.")

    optimization_results = read_csv(optimization_report)

    for source in sorted(optimized_dir.glob("*.glb")):
        install_atomic(source, model_dir / source.name)

    rejected_stems: set[str] = set()
    accepted_stems = {
        Path(row["file"]).stem
        for row in optimization_results
        if row.get("status") == "OK" and row.get("file")
    }
    for row in optimization_results:
        if row.get("status", "").startswith("REJECTED_") and row.get("file"):
            rejected_stems.add(Path(row["file"]).stem)
    for row in conversion_results:
        if row.get("status") == "REJECTED" and row.get("file"):
            rejected_stems.add(Path(row["file"]).stem)

    for stem in rejected_stems:
        (model_dir / f"{stem}.glb").unlink(missing_ok=True)
        stale_pck = pck_dir / f"{stem}.pck"
        if stale_pck.exists():
            stale_pck.unlink()
            print(f"Removed stale PCK for rejected asset: {stale_pck.name}")

    conversion_status = {
        Path(row["file"]).stem: row.get("status")
        for row in conversion_results
        if row.get("file")
    }
    removable_stems = accepted_stems | rejected_stems
    for source in zip_sources + gltf_sources:
        source_status = conversion_status.get(source.stem)
        if source_status == "ERROR":
            continue
        if (
            source.stem in removable_stems
            or source_status == "REJECTED"
        ):
            source.unlink(missing_ok=True)

    pack_ok = True
    prepared = sorted(optimized_dir.glob("*.glb"))
    if prepared:
        pack_command = [
            sys.executable,
            str(HERE / "06_build_godot_asset_packs.py"),
            "--input-dir",
            str(optimized_dir),
            "--output-dir",
            str(pck_dir),
            "--report-dir",
            str(godot_report_dir),
            "--build-dir",
            str(work_dir / "godot_builds"),
            "--workers",
            str(args.pck_workers),
            "--overwrite"
            if args.overwrite or args.overwrite_pcks
            else "--resume",
        ]
        if args.godot:
            pack_command.extend(["--godot", str(args.godot)])
        if args.only:
            pack_command.extend(
                ["--only", Path(args.only).with_suffix(".glb").name]
            )
        pack_ok = run(pack_command)
    else:
        print("No accepted GLB is available for PCK generation.")

    pck_manifest = [
        row
        for row in read_csv(godot_report_dir / "manifest.csv")
        if row.get("status") == "FAILED"
        or (
            row.get("pck_file")
            and (pck_dir / row["pck_file"]).is_file()
        )
    ]
    conversion_rejections = []
    for row in conversion_results:
        if row.get("status") == "REJECTED":
            conversion_rejections.append(
                {
                    **row,
                    "rejection_reasons": rejection_categories(
                        row.get("detail", "")
                    ),
                }
            )

    result_payload = {
        "settings": {
            "max_texture_size": args.max_texture_size,
            "max_triangles": args.max_triangles,
            "texture_workers": args.texture_workers,
            "pck_workers": args.pck_workers,
        },
        "starting_sources": starting,
        "conversion_results": conversion_results,
        "conversion_rejections": conversion_rejections,
        "optimization_results": optimization_results,
        "pck_results": pck_manifest,
        "stage_success": {
            "conversion": conversion_ok,
            "optimization": optimization_ok,
            "pck_generation": pack_ok,
        },
        "final_counts": {
            "glb": len(list(model_dir.glob("*.glb"))),
            "pck": len(list(pck_dir.glob("*.pck"))),
        },
    }
    write_json_atomic(result_report, result_payload)

    success = conversion_ok and optimization_ok and pack_ok
    if success:
        shutil.rmtree(work_dir)
        work_parent = work_dir.parent
        if work_parent.exists() and not any(work_parent.iterdir()):
            work_parent.rmdir()
    else:
        print(
            "3D pipeline finished with errors. Important details will be "
            "copied into pipeline_report.txt."
        )
    return 0 if success else 1


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except KeyboardInterrupt:
        raise SystemExit(130)
