#!/usr/bin/env python3
"""Convert ZIP-packaged glTF models in media/3d to GLB."""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path

DEFAULT_MAX_TRIANGLES = 1_500_000
TRIANGLES = 4
TRIANGLE_STRIP = 5
TRIANGLE_FAN = 6


def find_gltf(root: Path) -> Path | None:
    files = sorted(root.rglob("*.gltf"))
    if not files:
        return None

    for name in ("scene.gltf", "model.gltf"):
        for file in files:
            if file.name.lower() == name:
                return file

    return files[0]


def accessor_count(document: dict, index: object) -> int:
    accessors = document.get("accessors", [])
    if not isinstance(index, int) or not 0 <= index < len(accessors):
        return 0
    return int(accessors[index].get("count", 0) or 0)


def triangle_count(document: dict) -> int:
    """Count rendered triangles, including repeated mesh node instances."""
    mesh_counts: list[int] = []
    for mesh in document.get("meshes", []):
        total = 0
        for primitive in mesh.get("primitives", []):
            attributes = primitive.get("attributes", {})
            count = accessor_count(document, primitive.get("indices"))
            if not count:
                count = accessor_count(document, attributes.get("POSITION"))
            mode = int(primitive.get("mode", TRIANGLES))
            if mode == TRIANGLES:
                total += count // 3
            elif mode in (TRIANGLE_STRIP, TRIANGLE_FAN):
                total += max(0, count - 2)
        mesh_counts.append(total)

    instances = [
        node.get("mesh")
        for node in document.get("nodes", [])
        if isinstance(node.get("mesh"), int)
        and 0 <= node["mesh"] < len(mesh_counts)
    ]
    return (
        sum(mesh_counts[index] for index in instances)
        if instances
        else sum(mesh_counts)
    )


def rejection_reason(
    gltf_path: Path,
    max_triangles: int = DEFAULT_MAX_TRIANGLES,
) -> str | None:
    """Return the configured rejection reason without changing the source."""
    try:
        document = json.loads(gltf_path.read_text(encoding="utf-8-sig"))
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        raise ValueError(f"cannot read glTF JSON: {error}") from error

    animations = document.get("animations", [])
    has_points = any(
        int(primitive.get("mode", 4)) == 0
        for mesh in document.get("meshes", [])
        for primitive in mesh.get("primitives", [])
    )
    reasons = []
    if animations:
        reasons.append(f"contains {len(animations)} animation(s)")
    if has_points:
        reasons.append("contains a POINTS primitive (point cloud)")
    triangles = triangle_count(document)
    if triangles > max_triangles:
        reasons.append(
            f"contains {triangles:,} triangles, exceeding the "
            f"{max_triangles:,} triangle limit"
        )
    return "; ".join(reasons) if reasons else None


def validate_glb(path: Path) -> tuple[bool, str]:
    result = subprocess.run(
        [
            "gltf-transform", "validate", str(path),
            "--format", "csv",
            "--limit", "10000",
            "--ignore", "ACCESSOR_JOINTS_USED_ZERO_WEIGHT",
        ],
        capture_output=True,
        text=True,
    )
    if result.returncode == 0:
        return True, ""
    details = (result.stdout or result.stderr or "validation failed").strip()
    return False, details


def convert_gltf(
    gltf_path: Path,
    out_path: Path,
    overwrite: bool = False,
    max_triangles: int = DEFAULT_MAX_TRIANGLES,
) -> tuple[bool, str]:
    gltf_transform = shutil.which("gltf-transform")
    if not gltf_transform:
        return False, "gltf-transform is not installed or not available in PATH."

    try:
        reason = rejection_reason(gltf_path, max_triangles=max_triangles)
    except ValueError as error:
        return False, str(error)
    if reason:
        out_path.unlink(missing_ok=True)
        return True, f"rejected, no GLB stored: {reason}"

    if out_path.exists() and not overwrite:
        valid, validation_message = validate_glb(out_path)
        if not valid:
            out_path.unlink()
            return True, (
                "rejected, existing invalid GLB removed: "
                f"{validation_message}"
            )
        return True, f"skipped, already exists: {out_path.name}"

    if out_path.exists():
        out_path.unlink()

    out_path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="gltf_convert_", dir=out_path.parent) as tmp_name:
        temporary_glb = Path(tmp_name) / out_path.name
        cmd = [gltf_transform, "copy", str(gltf_path), str(temporary_glb)]
        result = subprocess.run(cmd, capture_output=True, text=True)

        if result.returncode != 0:
            msg = (result.stderr or result.stdout or "unknown error").strip()
            return False, msg

        valid, validation_message = validate_glb(temporary_glb)
        if not valid:
            return True, (
                "rejected, no GLB stored: GLB validation failed:\n"
                f"{validation_message}"
            )

        temporary_glb.replace(out_path)

    return True, f"created: {out_path.name}"


def safe_extract(archive: zipfile.ZipFile, destination: Path) -> None:
    """Extract an archive without allowing paths outside the temp directory."""
    destination = destination.resolve()
    for member in archive.infolist():
        target = (destination / member.filename).resolve()
        if os.path.commonpath((destination, target)) != str(destination):
            raise ValueError(f"unsafe ZIP member path: {member.filename}")
    archive.extractall(destination)


def convert_zip(
    zip_path: Path,
    out_dir: Path,
    overwrite: bool = False,
    remove_source: bool = True,
    max_triangles: int = DEFAULT_MAX_TRIANGLES,
) -> tuple[bool, str]:
    out_path = out_dir / f"{zip_path.stem}.glb"

    with tempfile.TemporaryDirectory(prefix="gltf_zip_") as tmp_name:
        tmp = Path(tmp_name)

        try:
            with zipfile.ZipFile(zip_path, "r") as zf:
                safe_extract(zf, tmp)
        except zipfile.BadZipFile:
            return False, "ZIP file is broken or not a valid ZIP archive."
        except (OSError, ValueError) as error:
            return False, f"cannot safely extract ZIP: {error}"

        gltf_path = find_gltf(tmp)
        if gltf_path is None:
            return False, "no .gltf file found inside ZIP."

        ok, msg = convert_gltf(
            gltf_path,
            out_path,
            overwrite=overwrite,
            max_triangles=max_triangles,
        )

        is_rejected = ok and msg.startswith("rejected,")
        if is_rejected and remove_source:
            zip_path.unlink()
            msg = f"{msg}; removed rejected source ZIP: {zip_path.name}"
        elif ok and remove_source and out_path.exists():
            zip_path.unlink()
            msg = f"{msg}; removed source ZIP: {zip_path.name}"

        return ok, msg


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--media-root", default=".", help="Path to the media directory")
    parser.add_argument("--source-dir", type=Path, help="Optional ZIP/glTF source instead of <media-root>/3d")
    parser.add_argument("--output-dir", type=Path, help="Optional GLB destination instead of the source 3d directory")
    parser.add_argument("--only", help="Process one ZIP or glTF filename only")
    parser.add_argument("--overwrite", action="store_true", help="Overwrite existing .glb files")
    parser.add_argument("--keep-sources", action="store_true", help="Keep ZIP and loose .gltf source files after successful conversion")
    parser.add_argument(
        "--max-triangles",
        type=int,
        default=DEFAULT_MAX_TRIANGLES,
        help="Reject assets above this rendered-triangle count (default: 1500000)",
    )
    parser.add_argument("--report", type=Path, help="Write a machine-readable JSON result report")
    args = parser.parse_args()
    if args.max_triangles <= 0:
        parser.error("--max-triangles must be greater than zero")

    media_root = Path(args.media_root).resolve()
    model_dir = args.source_dir.expanduser().resolve() if args.source_dir else media_root / "3d"
    output_dir = args.output_dir.expanduser().resolve() if args.output_dir else model_dir

    if not model_dir.exists():
        print(f"ERROR: Missing folder: {model_dir}")
        return 1
    output_dir.mkdir(parents=True, exist_ok=True)

    failed = 0
    rejected = 0
    converted_or_skipped = 0
    results: list[dict[str, str]] = []

    zip_files = sorted(model_dir.glob("*.zip"))
    loose_gltf_files = sorted(model_dir.glob("*.gltf"))
    if args.only:
        requested_stem = Path(args.only).stem
        zip_files = [item for item in zip_files if item.stem == requested_stem]
        loose_gltf_files = [item for item in loose_gltf_files if item.stem == requested_stem]

    if not zip_files and not loose_gltf_files:
        print("No ZIP or loose .gltf files found in 3d/.")
        if args.report:
            args.report.parent.mkdir(parents=True, exist_ok=True)
            args.report.write_text(
                json.dumps({"settings": {"max_triangles": args.max_triangles}, "results": []}, indent=2) + "\n",
                encoding="utf-8",
            )
        return 0

    for zip_path in zip_files:
        ok, msg = convert_zip(
            zip_path,
            output_dir,
            overwrite=args.overwrite,
            remove_source=not args.keep_sources,
            max_triangles=args.max_triangles,
        )
        is_rejected = ok and msg.startswith("rejected,")
        status = "REJECTED" if is_rejected else "OK" if ok else "ERROR"
        print(f"[{status}] {zip_path.name}: {msg}")
        results.append({"file": zip_path.name, "status": status, "detail": msg})
        if is_rejected:
            rejected += 1
        elif ok:
            converted_or_skipped += 1
        else:
            failed += 1

    for gltf_path in loose_gltf_files:
        out_path = output_dir / f"{gltf_path.stem}.glb"
        ok, msg = convert_gltf(
            gltf_path,
            out_path,
            overwrite=args.overwrite,
            max_triangles=args.max_triangles,
        )
        is_rejected = ok and msg.startswith("rejected,")
        if is_rejected and not args.keep_sources:
            gltf_path.unlink()
            msg = f"{msg}; removed rejected source glTF: {gltf_path.name}"
        elif ok and not args.keep_sources and out_path.exists():
            gltf_path.unlink()
            msg = f"{msg}; removed source glTF: {gltf_path.name}"
        status = "REJECTED" if is_rejected else "OK" if ok else "ERROR"
        print(f"[{status}] {gltf_path.name}: {msg}")
        results.append({"file": gltf_path.name, "status": status, "detail": msg})
        if is_rejected:
            rejected += 1
        elif ok:
            converted_or_skipped += 1
        else:
            failed += 1

    print(f"Processed 3D sources: {converted_or_skipped + rejected + failed}")
    print(f"Rejected by policy: {rejected}")
    print(f"Failed conversions: {failed}")

    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        temporary = args.report.with_name(f".{args.report.name}.tmp")
        temporary.write_text(
            json.dumps(
                {
                    "settings": {"max_triangles": args.max_triangles},
                    "results": results,
                },
                indent=2,
            )
            + "\n",
            encoding="utf-8",
        )
        temporary.replace(args.report)

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
