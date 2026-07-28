#!/usr/bin/env python3
"""Create a timestamped backup of the media folders."""

from __future__ import annotations

import argparse
import shutil
import sys
from datetime import datetime
from pathlib import Path

REQUIRED_FOLDERS = ["3d", "images", "videos"]
OPTIONAL_FOLDERS = ["3dPck"]


def copy_folder(src: Path, dst: Path) -> tuple[bool, str]:
    if not src.exists():
        return False, f"Missing source folder: {src.name}/"

    try:
        shutil.copytree(src, dst)
    except Exception as exc:
        return False, str(exc)

    return True, f"Copied {src.name}/ -> {dst}"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--media-root", default=".", help="Path to the media directory")
    args = parser.parse_args()

    media_root = Path(args.media_root).resolve()
    backup_root = media_root / "BACKUP"
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    current_backup = backup_root / f"media_backup_{timestamp}"

    backup_root.mkdir(exist_ok=True)
    index = 1
    while current_backup.exists():
        current_backup = backup_root / f"media_backup_{timestamp}_{index}"
        index += 1
    current_backup.mkdir(exist_ok=False)

    failed = 0

    print(f"Backup directory: {current_backup}")

    for folder in REQUIRED_FOLDERS:
        ok, msg = copy_folder(media_root / folder, current_backup / folder)
        status = "OK" if ok else "ERROR"
        print(f"[{status}] {msg}")
        if not ok:
            failed += 1

    for folder in OPTIONAL_FOLDERS:
        source = media_root / folder
        if not source.exists():
            print(f"[SKIPPED] Optional folder does not exist yet: {folder}/")
            continue
        ok, msg = copy_folder(source, current_backup / folder)
        print(f"[{'OK' if ok else 'ERROR'}] {msg}")
        if not ok:
            failed += 1

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
