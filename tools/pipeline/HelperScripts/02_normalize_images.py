#!/usr/bin/env python3
"""Convert images to JPG and resize them to a maximum of 2K."""

from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
from pathlib import Path

SUPPORTED_INPUTS = {".jpg", ".jpeg", ".png", ".webp", ".tif", ".tiff", ".bmp"}
MAX_SIZE = "2048x2048>"


def unique_output_path(path: Path) -> Path:
    candidate = path.with_suffix(".jpg")

    if candidate == path:
        return candidate

    if not candidate.exists():
        return candidate

    index = 1

    while True:
        candidate = path.with_name(f"{path.stem}_{index}.jpg")

        if not candidate.exists():
            return candidate

        index += 1


def find_imagemagick() -> str | None:
    return shutil.which("magick") or shutil.which("convert")


def convert_image(file: Path, remove_original: bool = True) -> tuple[bool, str]:
    imagemagick = find_imagemagick()

    if not imagemagick:
        return False, "ImageMagick is not installed or not available in PATH."

    suffix = file.suffix.lower()

    if suffix not in SUPPORTED_INPUTS:
        return False, f"unsupported image format: {suffix}"

    output = unique_output_path(file)
    temp_output = output.with_name(f".{output.stem}.tmp{output.suffix}")

    cmd = [
        imagemagick,
        str(file),
        "-auto-orient",
        "-resize",
        MAX_SIZE,
        "-colorspace",
        "sRGB",
        "-strip",
        "-quality",
        "90",
        str(temp_output),
    ]

    result = subprocess.run(cmd, capture_output=True, text=True)

    if result.returncode != 0:
        temp_output.unlink(missing_ok=True)
        msg = (result.stderr or result.stdout or "unknown error").strip()
        return False, msg

    temp_output.replace(output)

    if remove_original and file.resolve() != output.resolve() and file.exists():
        file.unlink()

    return True, f"{file.name} -> {output.name}"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--media-root", default=".", help="Path to the media directory")
    args = parser.parse_args()

    media_root = Path(args.media_root).resolve()
    image_dir = media_root / "images"

    if not image_dir.exists():
        print(f"ERROR: Missing folder: {image_dir}")
        return 1

    files = sorted(p for p in image_dir.rglob("*") if p.is_file())
    failed = 0
    processed = 0

    if not files:
        print("No image files found.")
        return 0

    for file in files:
        if file.suffix.lower() not in SUPPORTED_INPUTS:
            print(f"[ERROR] {file.relative_to(media_root)}: unsupported image format")
            failed += 1
            continue

        ok, msg = convert_image(file)
        status = "OK" if ok else "ERROR"
        print(f"[{status}] {msg}")

        if ok:
            processed += 1
        else:
            failed += 1

    print(f"Processed images: {processed}")
    print(f"Failed images: {failed}")

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
