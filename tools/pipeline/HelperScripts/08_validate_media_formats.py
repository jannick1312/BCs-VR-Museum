#!/usr/bin/env python3
"""Validate that media folders contain only the expected final file formats."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

EXPECTED = {
    "3d": {".glb"},
    "3dPck": {".pck"},
    "images": {".jpg"},
    "videos": {".ogv"},
}

def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--media-root", default=".", help="Path to the media directory")
    args = parser.parse_args()

    media_root = Path(args.media_root).resolve()
    problems: list[str] = []

    for folder, allowed in EXPECTED.items():
        path = media_root / folder

        if not path.exists():
            problems.append(f"Missing directory: {folder}/")
            continue

        for file in sorted(p for p in path.rglob("*") if p.is_file()):
            if file.suffix.lower() not in allowed:
                rel = file.relative_to(media_root)
                expected = ", ".join(sorted(allowed))
                problems.append(f"{rel} -> expected: {expected}")

    if problems:
        print("Found files with unexpected formats:")
        for item in problems:
            print(f" - {item}")
        return 1

    print("All media files use the expected final formats.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
