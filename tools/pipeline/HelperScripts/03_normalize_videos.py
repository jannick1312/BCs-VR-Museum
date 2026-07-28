#!/usr/bin/env python3
"""Convert videos to OGV/Theora, scale them to max 2K, cap FPS at 30, and remove unnecessary streams."""

from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
from pathlib import Path

SUPPORTED_INPUTS = {".mp4", ".mov", ".mkv", ".avi", ".webm", ".m4v", ".ogv", ".ogg"}
MAX_DIMENSION = 2048
MAX_FPS = 30
VIDEO_QUALITY = "5"
AUDIO_QUALITY = "4"


def run_text(cmd: list[str]) -> tuple[int, str]:
    result = subprocess.run(cmd, capture_output=True, text=True)
    return result.returncode, (result.stdout or "") + (result.stderr or "")


def ffmpeg_has_encoder(ffmpeg: str, name: str) -> bool:
    code, output = run_text([ffmpeg, "-hide_banner", "-encoders"])
    return code == 0 and name in output


def unique_output_path(path: Path) -> Path:
    candidate = path.with_suffix(".ogv")

    if candidate == path:
        return candidate

    if not candidate.exists():
        return candidate

    index = 1
    while True:
        candidate = path.with_name(f"{path.stem}_normalized_{index}.ogv")
        if not candidate.exists():
            return candidate
        index += 1


def should_skip_file(file: Path) -> bool:
    name = file.name.lower()

    if not file.is_file():
        return True

    if file.name.startswith("."):
        return True

    if ".tmp" in name:
        return True

    if file.suffix.lower() not in SUPPORTED_INPUTS:
        return True

    return False


def normalize_video(file: Path, remove_original: bool = True) -> tuple[bool, str]:
    ffmpeg = shutil.which("ffmpeg")

    if not ffmpeg:
        return False, "ffmpeg is not installed or not available in PATH."

    if not ffmpeg_has_encoder(ffmpeg, "libtheora"):
        return False, (
            "Your ffmpeg does not include the libtheora encoder. "
            "On macOS install ffmpeg-full with libtheora support. "
            "On Ubuntu install the normal ffmpeg package."
        )

    output = unique_output_path(file)
    temp_output = output.with_name(f".{output.stem}.tmp{output.suffix}")

    scale_filter = (
        f"scale='if(gt(iw,ih),min({MAX_DIMENSION},iw),-2)':"
        f"'if(gt(ih,iw),min({MAX_DIMENSION},ih),-2)'"
    )

    cmd = [
        ffmpeg,
        "-hide_banner",
        "-stats",
        "-y",
        "-i",
        str(file),
        "-map",
        "0:v:0",
        "-map",
        "0:a:0?",
        "-vf",
        scale_filter,
        "-r",
        str(MAX_FPS),
        "-c:v",
        "libtheora",
        "-q:v",
        VIDEO_QUALITY,
        "-pix_fmt",
        "yuv420p",
        "-c:a",
        "libvorbis",
        "-q:a",
        AUDIO_QUALITY,
        "-sn",
        "-dn",
        str(temp_output),
    ]

    result = subprocess.run(cmd)

    if result.returncode != 0:
        temp_output.unlink(missing_ok=True)
        return False, f"ffmpeg failed for {file.name}"

    if output.exists():
        output.unlink()

    temp_output.rename(output)

    if remove_original and file.resolve() != output.resolve():
        file.unlink()

    return True, f"{file.name} -> {output.name}"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--media-root", default=".", help="Path to the media directory")
    args = parser.parse_args()

    media_root = Path(args.media_root).resolve()
    video_dir = media_root / "videos"

    if not video_dir.exists():
        print(f"ERROR: Missing folder: {video_dir}")
        return 1

    files = sorted(
        p for p in video_dir.rglob("*")
        if not should_skip_file(p)
    )

    failed = 0
    processed = 0

    if not files:
        print("No video files found.")
        return 0

    print(f"Found {len(files)} video(s).")
    print(f"Target format: .ogv / Theora")
    print(f"Max dimension: {MAX_DIMENSION}px")
    print(f"Max FPS: {MAX_FPS}")
    print(f"Video quality: {VIDEO_QUALITY}")
    print()

    for i, file in enumerate(files, start=1):
        rel = file.relative_to(media_root)
        print(f"[{i}/{len(files)}] Processing {rel}")

        ok, msg = normalize_video(file)

        if ok:
            print(f"[OK] {msg}")
            processed += 1
        else:
            print(f"[ERROR] {msg}")
            failed += 1

        print()

    print(f"Processed videos: {processed}")
    print(f"Failed videos: {failed}")

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
