#!/usr/bin/env python3
"""Write one consolidated human-readable report for the complete pipeline."""

from __future__ import annotations

import argparse
import json
import os
import sys
from collections import Counter, defaultdict
from pathlib import Path
from typing import Any


EXPECTED = {
    "images": {".jpg"},
    "videos": {".ogv"},
    "3d": {".glb"},
    "3dPck": {".pck"},
}
def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--media-root", default=".", type=Path)
    parser.add_argument("--run-data", type=Path)
    parser.add_argument("--output", type=Path)
    return parser.parse_args()


def read_json(path: Path | None, fallback: Any) -> Any:
    if not path or not path.is_file():
        return fallback
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError):
        return fallback


def visible_files(root: Path, media_root: Path) -> list[Path]:
    if not root.is_dir():
        return []
    return sorted(
        path
        for path in root.rglob("*")
        if path.is_file()
    )


def add_inventory(lines: list[str], media_root: Path) -> int:
    invalid_total = 0
    lines.extend(["", "FINAL MEDIA INVENTORY", "-" * 78])
    for folder, allowed in EXPECTED.items():
        root = media_root / folder
        files = visible_files(root, media_root)
        invalid = [path for path in files if path.suffix.lower() not in allowed]
        formats = Counter(path.suffix.lower() or "<no extension>" for path in files)
        status = "MISSING" if not root.is_dir() else "FAILED" if invalid else "OK"
        lines.append(f"{folder}/")
        lines.append(f"  Files: {len(files)}")
        lines.append(f"  Expected: {', '.join(sorted(allowed))}")
        lines.append(f"  Status: {status}")
        lines.append(
            "  Formats: "
            + (
                ", ".join(f"{suffix}={count}" for suffix, count in sorted(formats.items()))
                if formats
                else "none"
            )
        )
        for path in invalid:
            lines.append(f"  INVALID: {path.relative_to(media_root)}")
        if not root.is_dir():
            invalid_total += 1
        invalid_total += len(invalid)
    return invalid_total


def categories_from_conversion(detail: str) -> list[str]:
    lowered = detail.lower()
    result = []
    if "animation" in lowered:
        result.append("animation")
    if "point cloud" in lowered or "points primitive" in lowered:
        result.append("point cloud")
    if "triangle limit" in lowered:
        result.append("triangle limit")
    if "validation failed" in lowered or "invalid glb" in lowered:
        result.append("glTF validation")
    return result or ["other policy"]


def add_3d_results(lines: list[str], media_root: Path) -> None:
    report_path = (
        media_root
        / ".media_pipeline_work"
        / "3d_pipeline_results.json"
    )
    data = read_json(report_path, {})
    lines.extend(["", "3D PROCESSING RESULTS", "-" * 78])
    if not data:
        lines.append("No 3D result data was produced.")
        return

    starting = data.get("starting_sources", {})
    final = data.get("final_counts", {})
    lines.append(
        "Started with: "
        f"{starting.get('zip', 0)} ZIP, "
        f"{starting.get('gltf', 0)} loose glTF, "
        f"{starting.get('glb', 0)} existing GLB"
    )
    lines.append(
        f"Finished with: {final.get('glb', 0)} GLB, "
        f"{final.get('pck', 0)} PCK"
    )
    settings = data.get("settings", {})
    lines.append(
        "Policy: reject animations, POINTS point clouds, invalid glTF/GLB, "
        f"and assets above {int(settings.get('max_triangles', 1_500_000)):,} "
        "rendered triangles"
    )
    conversion_statuses = Counter(
        row.get("status", "<missing>")
        for row in data.get("conversion_results", [])
    )
    optimization_statuses = Counter(
        row.get("status", "<missing>")
        for row in data.get("optimization_results", [])
    )
    lines.append(
        "Conversion statuses: "
        + (
            ", ".join(
                f"{status}={count}"
                for status, count in sorted(conversion_statuses.items())
            )
            if conversion_statuses
            else "none"
        )
    )
    lines.append(
        "Optimization statuses: "
        + (
            ", ".join(
                f"{status}={count}"
                for status, count in sorted(optimization_statuses.items())
            )
            if optimization_statuses
            else "none"
        )
    )

    rejected_assets: dict[str, set[str]] = defaultdict(set)
    technical_errors: list[str] = []
    for row in data.get("conversion_results", []):
        name = row.get("file", "<unknown>")
        status = row.get("status", "")
        detail = row.get("detail", "")
        if status == "REJECTED":
            rejected_assets[Path(name).stem].update(
                categories_from_conversion(detail)
            )
        elif status == "ERROR":
            technical_errors.append(f"{name}: {detail}")

    for row in data.get("optimization_results", []):
        name = row.get("file", "<unknown>")
        status = row.get("status", "")
        if status.startswith("REJECTED_"):
            raw_reasons = row.get("rejection_reasons", "")
            reasons = [
                item.strip().lower().replace("_", " ")
                for item in raw_reasons.split(",")
                if item.strip()
            ]
            rejected_assets[Path(name).stem].update(reasons or ["other policy"])
        elif status == "FAILED":
            technical_errors.append(
                f"{name}: {row.get('error', 'optimization failed')}"
            )

    reason_counts = Counter(
        reason for reasons in rejected_assets.values() for reason in reasons
    )
    lines.append(f"Rejected assets: {len(rejected_assets)}")
    if reason_counts:
        lines.append("Rejections by reason:")
        for reason, count in sorted(reason_counts.items()):
            lines.append(f"  {reason}: {count}")
    else:
        lines.append("Rejections by reason: none")

    pck_results = data.get("pck_results", [])
    accepted = [
        row
        for row in data.get("optimization_results", [])
        if row.get("status") == "OK"
    ]
    lines.append(f"Accepted/optimized assets: {len(accepted)}")
    pck_statuses = Counter(row.get("status", "<missing>") for row in pck_results)
    lines.append(
        "PCK build statuses: "
        + (
            ", ".join(
                f"{status}={count}" for status, count in sorted(pck_statuses.items())
            )
            if pck_statuses
            else "none"
        )
    )
    for row in pck_results:
        if row.get("status") == "FAILED":
            technical_errors.append(
                f"{row.get('source_file', '<unknown>')}: "
                f"{row.get('error', 'PCK generation failed')}"
            )

    stages = data.get("stage_success", {})
    for name, ok in stages.items():
        if not ok:
            technical_errors.append(
                f"{name} stage failed"
            )
    lines.append(
        "3D stages: "
        + ", ".join(
            f"{name}={'OK' if ok else 'ERROR'}"
            for name, ok in stages.items()
        )
    )
    lines.append(f"Technical 3D errors: {len(technical_errors)}")
    for detail in technical_errors:
        lines.append(f"  ERROR: {detail}")


def main() -> int:
    args = parse_args()
    media_root = args.media_root.expanduser().resolve()
    output = (
        args.output.expanduser().resolve()
        if args.output
        else media_root / "pipeline_report.txt"
    )
    run_data = read_json(args.run_data, {})
    steps = run_data.get("steps", [])
    pipeline_status = (
        "ERROR"
        if any(step.get("status") == "ERROR" for step in steps)
        else "OK"
    )

    lines = [
        "MEDIA PREPARATION PIPELINE REPORT",
        "=" * 78,
        f"Status: {pipeline_status}",
        f"Started: {run_data.get('started_at', 'unknown')}",
        f"Finished: {run_data.get('finished_at', 'unknown')}",
        f"Media root: {media_root}",
        f"Backup: {run_data.get('backup_directory') or 'not created'}",
        "",
        "PIPELINE STEPS",
        "-" * 78,
    ]
    for step in steps:
        elapsed = float(step.get("elapsed_seconds", 0.0) or 0.0)
        lines.append(
            f"[{step.get('status', 'UNKNOWN')}] "
            f"{step.get('name', '<unnamed>')} ({elapsed:.1f}s)"
        )
        if step.get("detail"):
            lines.append(f"  {step['detail']}")

    initial = run_data.get("initial_inventory", {})
    lines.extend(["", "STARTING INVENTORY", "-" * 78])
    for folder in ("images", "videos", "3d", "3dPck"):
        item = initial.get(folder, {})
        formats = item.get("formats", {})
        lines.append(
            f"{folder}/: {item.get('files', 0)} file(s); "
            + (
                ", ".join(
                    f"{suffix}={count}" for suffix, count in formats.items()
                )
                if formats
                else "no formats"
            )
        )

    invalid_total = add_inventory(lines, media_root)
    add_3d_results(lines, media_root)
    lines.extend(["", "All important pipeline results are included above.", ""])

    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = output.with_name(f".{output.name}.tmp")
    temporary.write_text("\n".join(lines), encoding="utf-8")
    os.replace(temporary, output)
    print(f"Consolidated report written to: {output}")
    return 1 if invalid_total else 0


if __name__ == "__main__":
    sys.exit(main())
