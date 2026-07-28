#!/usr/bin/env python3
"""Import each prepared GLB and export one native Godot PCK per object."""

from __future__ import annotations

import argparse
import csv
import os
import shutil
import subprocess
import sys
import tempfile
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import asdict, dataclass
from pathlib import Path


HERE = Path(__file__).resolve().parent
TEMPLATE = HERE / "godot_pck_template"
TEMPLATE_FILES = (
    "project.godot",
    "main.tscn",
    "main.gd",
    "prepare_native.gd",
    "export_presets.cfg",
)


@dataclass
class Result:
    source_file: str
    status: str = "FAILED"
    pck_file: str = ""
    resource_path: str = ""
    source_bytes: int = 0
    pck_bytes: int = 0
    elapsed_seconds: float = 0.0
    error: str = ""


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input-dir", required=True, type=Path)
    parser.add_argument("--output-dir", required=True, type=Path)
    parser.add_argument(
        "--report-dir",
        type=Path,
        help="Manifest and log destination (default: --output-dir)",
    )
    parser.add_argument(
        "--godot",
        type=Path,
        help="Godot editor binary (default: discover godot/godot4 or common macOS paths)",
    )
    parser.add_argument(
        "--platform",
        choices=("macOS", "Linux", "Windows Desktop"),
        default=(
            "macOS"
            if sys.platform == "darwin"
            else "Windows Desktop"
            if sys.platform == "win32"
            else "Linux"
        ),
        help="Godot export platform preset (default: current host)",
    )
    parser.add_argument(
        "--workers",
        type=int,
        default=1,
        help="Concurrent Godot import processes (default: 1; try at most 2 first)",
    )
    parser.add_argument(
        "--build-dir",
        type=Path,
        help="Parent for temporary build projects (default: beside --output-dir)",
    )
    parser.add_argument("--copy", action="store_true", help="Copy instead of hard-linking GLBs")
    parser.add_argument("--keep-build", action="store_true")
    parser.add_argument("--overwrite", action="store_true")
    parser.add_argument("--resume", action="store_true")
    parser.add_argument("--only", help="Build one GLB filename only")
    args = parser.parse_args()
    if args.workers <= 0:
        parser.error("--workers must be greater than zero")
    if args.overwrite and args.resume:
        parser.error("--overwrite and --resume cannot be combined")
    return args


def discover_godot(configured: Path | None) -> Path:
    if configured:
        candidate = configured.expanduser().resolve()
        if candidate.is_file():
            return candidate
        raise FileNotFoundError(f"Godot editor binary not found: {candidate}")

    candidates = [
        shutil.which("godot"),
        shutil.which("godot4"),
        shutil.which("godot-mono"),
        "/Applications/Godot.app/Contents/MacOS/Godot",
        "/Applications/Godot_mono.app/Contents/MacOS/Godot",
    ]
    for value in candidates:
        if value and Path(value).is_file():
            return Path(value).resolve()
    raise FileNotFoundError(
        "Godot editor binary not found. Install Godot, add it to PATH, "
        "or pass --godot /absolute/path/to/godot."
    )


def run(command: list[str], log: Path) -> None:
    started = time.monotonic()
    result = subprocess.run(command, capture_output=True, text=True)
    with log.open("a", encoding="utf-8") as file:
        file.write("\n$ {}\n".format(" ".join(command)))
        file.write((result.stdout or "") + (result.stderr or ""))
        file.write(
            "\n[exit={}, elapsed={:.2f}s]\n".format(
                result.returncode, time.monotonic() - started
            )
        )
    if result.returncode:
        lines = ((result.stdout or "") + (result.stderr or "")).strip().splitlines()
        error_lines = [
            line.strip()
            for line in lines
            if "error" in line.lower()
            and line.strip()
            and set(line.strip()) != {"="}
        ]
        meaningful = [
            line.strip()
            for line in lines
            if line.strip() and set(line.strip()) != {"="}
        ]
        raise RuntimeError(
            error_lines[-1]
            if error_lines
            else meaningful[-1]
            if meaningful
            else "Godot command failed"
        )


def prepare_project(build_dir: Path, platform: str, resource_path: str) -> Path:
    assets = build_dir / "assets"
    assets.mkdir(parents=True)
    for name in TEMPLATE_FILES:
        shutil.copy2(TEMPLATE / name, build_dir / name)
    preset = build_dir / "export_presets.cfg"
    contents = preset.read_text(encoding="utf-8")
    contents = contents.replace(
        'platform="macOS"', 'platform="{}"'.format(platform), 1
    )
    contents = contents.replace("__NATIVE_SCENE__", resource_path, 1)
    preset.write_text(contents, encoding="utf-8")
    return assets


def install_source(source: Path, destination: Path, copy: bool) -> None:
    if copy:
        shutil.copy2(source, destination)
        return
    try:
        os.link(source, destination)
    except OSError:
        shutil.copy2(source, destination)


def write_manifest(output_dir: Path, results: list[Result]) -> None:
    destination = output_dir / "manifest.csv"
    temporary = output_dir / ".manifest.csv.tmp"
    with temporary.open("w", newline="", encoding="utf-8") as file:
        writer = csv.DictWriter(file, fieldnames=list(Result.__dataclass_fields__))
        writer.writeheader()
        for result in results:
            writer.writerow(asdict(result))
    os.replace(temporary, destination)


def build_one(
    source: Path,
    output_dir: Path,
    report_dir: Path,
    build_root: Path,
    godot: Path,
    args: argparse.Namespace,
) -> Result:
    started = time.monotonic()
    destination = output_dir / source.with_suffix(".pck").name
    resource_path = "res://native/{}.scn".format(source.stem)
    result = Result(
        source_file=source.name,
        pck_file=destination.name,
        resource_path=resource_path,
        source_bytes=source.stat().st_size,
    )
    log = report_dir / "logs" / "{}.log".format(source.stem)
    if log.exists():
        log.unlink()
    if destination.exists() and not args.overwrite:
        result.status = "SKIPPED" if args.resume else "FAILED"
        result.error = "output exists" if not args.resume else ""
        result.pck_bytes = destination.stat().st_size
        result.elapsed_seconds = round(time.monotonic() - started, 3)
        return result

    build_dir = Path(
        tempfile.mkdtemp(prefix=".pck-{}-".format(source.stem[:32]), dir=build_root)
    )
    try:
        assets = prepare_project(build_dir, args.platform, resource_path)
        install_source(source, assets / source.name, args.copy)
        temporary_pck = build_dir / "object.pck"
        run([str(godot), "--headless", "--path", str(build_dir), "--import"], log)
        run([
            str(godot), "--headless", "--path", str(build_dir),
            "--script", "res://prepare_native.gd",
        ], log)
        run([
            str(godot), "--headless", "--path", str(build_dir),
            "--export-pack", "Asset Pack", str(temporary_pck),
        ], log)
        if not temporary_pck.is_file():
            raise RuntimeError("Godot did not create the PCK")
        if destination.exists():
            destination.unlink()
        os.replace(temporary_pck, destination)
        result.status = "OK"
        result.pck_bytes = destination.stat().st_size
    except Exception as error:
        result.error = "{}: {}".format(type(error).__name__, error)
    finally:
        if not args.keep_build:
            shutil.rmtree(build_dir, ignore_errors=True)
    result.elapsed_seconds = round(time.monotonic() - started, 3)
    return result


def main() -> int:
    args = parse_args()
    input_dir = args.input_dir.expanduser().resolve()
    output_dir = args.output_dir.expanduser().resolve()
    report_dir = (
        args.report_dir.expanduser().resolve()
        if args.report_dir
        else output_dir
    )
    godot = discover_godot(args.godot)
    build_root = (
        args.build_dir.expanduser().resolve()
        if args.build_dir
        else output_dir.parent / ".{}-godot-build".format(output_dir.name)
    )
    if not input_dir.is_dir():
        raise NotADirectoryError(input_dir)
    if not godot.is_file():
        raise FileNotFoundError(godot)
    sources = sorted(input_dir.glob("*.glb"))
    if args.only:
        sources = [source for source in sources if source.name == args.only]
    if not sources:
        raise RuntimeError("no matching top-level GLB files found in {}".format(input_dir))
    output_dir.mkdir(parents=True, exist_ok=True)
    report_dir.mkdir(parents=True, exist_ok=True)
    (report_dir / "logs").mkdir(exist_ok=True)
    build_root.mkdir(parents=True, exist_ok=True)

    results_by_name: dict[str, Result] = {}
    with ThreadPoolExecutor(max_workers=args.workers) as executor:
        futures = {
            executor.submit(
                build_one,
                source,
                output_dir,
                report_dir,
                build_root,
                godot,
                args,
            ): source
            for source in sources
        }
        completed = 0
        for future in as_completed(futures):
            item = future.result()
            completed += 1
            results_by_name[item.source_file] = item
            print(
                "[{}/{}] {}: {} ({:.1f}s)".format(
                    completed, len(sources), item.source_file, item.status,
                    item.elapsed_seconds,
                ),
                flush=True,
            )
            if item.error:
                print("  {}".format(item.error), file=sys.stderr, flush=True)
            write_manifest(
                report_dir,
                [results_by_name[name] for name in sorted(results_by_name)],
            )

    results = [results_by_name[name] for name in sorted(results_by_name)]
    failures = sum(item.status == "FAILED" for item in results)
    print(
        "Created {} individual PCK(s); {} failed.".format(
            sum(item.status == "OK" for item in results), failures
        )
    )
    if not args.keep_build and args.build_dir is None:
        try:
            build_root.rmdir()
        except OSError:
            pass
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
