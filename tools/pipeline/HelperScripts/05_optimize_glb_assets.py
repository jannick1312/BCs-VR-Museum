#!/usr/bin/env python3
"""Reduce textures in static GLBs while preserving their base geometry.

The source directory is read-only. Each result is validated structurally and
with glTF Validator before being moved atomically into the output directory.
Animated assets and any asset containing POINTS primitives are rejected with a report and log entry.
Assets above the configured rendered-triangle limit are rejected as well.
"""

from __future__ import annotations

import argparse
import csv
import json
import os
import shutil
import struct
import subprocess
import sys
import tempfile
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any


GLB_MAGIC = 0x46546C67
JSON_CHUNK = 0x4E4F534A
BIN_CHUNK = 0x004E4942
TRIANGLES = 4
TRIANGLE_STRIP = 5
TRIANGLE_FAN = 6
KTX2_IDENTIFIER = b"\xABKTX 20\xBB\r\n\x1A\n"
POINTS = 0
DEFAULT_MAX_TRIANGLES = 1_500_000


@dataclass
class Result:
    file: str
    status: str = "FAILED"
    primitives_before: int = 0
    primitives_after: int = 0
    triangles_before: int = 0
    triangles_after: int = 0
    points_before: int = 0
    points_after: int = 0
    triangle_reduction_percent: float = 0.0
    textures: int = 0
    vertex_color_primitives: int = 0
    unlocked_border_fallback: bool = False
    note: str = ""
    max_texture_before: str = ""
    max_texture_after: str = ""
    input_bytes: int = 0
    output_bytes: int = 0
    elapsed_seconds: float = 0.0
    rejection_reasons: str = ""
    error: str = ""


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input-dir", required=True, type=Path)
    parser.add_argument("--output-dir", required=True, type=Path)
    parser.add_argument("--max-texture-size", type=int, default=2048)
    parser.add_argument(
        "--max-triangles",
        type=int,
        default=DEFAULT_MAX_TRIANGLES,
        help="Reject assets above this rendered-triangle count (default: 1500000)",
    )
    parser.add_argument(
        "--texture-format",
        choices=("source", "uastc"),
        default="source",
        help=(
            "Keep resized PNG/JPEG textures or encode KTX2-UASTC "
            "(default: source)"
        ),
    )
    parser.add_argument("--uastc-level", type=int, choices=range(5), default=2)
    parser.add_argument("--uastc-jobs", type=int, default=4)
    parser.add_argument(
        "--uastc-zstd",
        type=int,
        choices=range(23),
        default=3,
        help="KTX2 Zstandard level; lower levels decode faster (default: 3)",
    )
    parser.add_argument(
        "--mipmaps",
        action=argparse.BooleanOptionalAction,
        default=True,
        help="Generate KTX2 mipmaps in UASTC mode (default: enabled)",
    )
    parser.add_argument(
        "--workers",
        type=int,
        default=1,
        help="Number of GLB files processed in parallel (default: 1)",
    )
    parser.add_argument("--overwrite", action="store_true")
    parser.add_argument(
        "--resume",
        action="store_true",
        help="Skip GLBs that already exist in the output directory",
    )
    parser.add_argument("--only", help="Process one filename only")
    args = parser.parse_args()
    if args.max_texture_size <= 0 or args.max_triangles <= 0:
        parser.error("limits must be greater than zero")
    if args.uastc_jobs <= 0:
        parser.error("--uastc-jobs must be greater than zero")
    if args.workers <= 0:
        parser.error("--workers must be greater than zero")
    if args.overwrite and args.resume:
        parser.error("--overwrite and --resume cannot be used together")
    return args


def read_glb(path: Path) -> tuple[dict[str, Any], bytes]:
    payload = path.read_bytes()
    if len(payload) < 20:
        raise ValueError("file is shorter than a GLB header")
    magic, version, declared_length = struct.unpack_from("<III", payload, 0)
    if magic != GLB_MAGIC or version != 2 or declared_length != len(payload):
        raise ValueError("invalid GLB 2.0 header")
    offset = 12
    document: dict[str, Any] | None = None
    binary = b""
    while offset + 8 <= len(payload):
        length, kind = struct.unpack_from("<II", payload, offset)
        offset += 8
        chunk = payload[offset : offset + length]
        if len(chunk) != length:
            raise ValueError("truncated GLB chunk")
        offset += length
        if kind == JSON_CHUNK:
            document = json.loads(chunk.rstrip(b" \t\r\n\0").decode("utf-8"))
        elif kind == BIN_CHUNK:
            binary = chunk
    if document is None:
        raise ValueError("GLB has no JSON chunk")
    return document, binary


def accessor_count(document: dict[str, Any], index: Any) -> int:
    accessors = document.get("accessors", [])
    if not isinstance(index, int) or not 0 <= index < len(accessors):
        return 0
    return int(accessors[index].get("count", 0) or 0)


def primitive_triangles(document: dict[str, Any], primitive: dict[str, Any]) -> int:
    attributes = primitive.get("attributes", {})
    count = accessor_count(document, primitive.get("indices"))
    if not count:
        count = accessor_count(document, attributes.get("POSITION"))
    mode = int(primitive.get("mode", TRIANGLES))
    if mode == TRIANGLES:
        return count // 3
    if mode in (TRIANGLE_STRIP, TRIANGLE_FAN):
        return max(0, count - 2)
    return 0


def mesh_statistics(document: dict[str, Any]) -> list[tuple[int, int, int]]:
    result = []
    for mesh in document.get("meshes", []):
        primitives = mesh.get("primitives", [])
        triangles = sum(primitive_triangles(document, item) for item in primitives)
        colors = sum(
            any(key.startswith("COLOR_") for key in item.get("attributes", {}))
            for item in primitives
        )
        result.append((len(primitives), triangles, colors))
    return result


def scene_statistics(document: dict[str, Any]) -> tuple[int, int, int, int]:
    stats = mesh_statistics(document)
    instances = primitives = triangles = colors = 0
    for node in document.get("nodes", []):
        mesh = node.get("mesh")
        if isinstance(mesh, int) and 0 <= mesh < len(stats):
            instances += 1
            primitives += stats[mesh][0]
            triangles += stats[mesh][1]
            colors += stats[mesh][2]
    if not instances:
        primitives = sum(item[0] for item in stats)
        triangles = sum(item[1] for item in stats)
        colors = sum(item[2] for item in stats)
    return instances, primitives, triangles, colors


def primitive_index_signature(document: dict[str, Any]) -> dict[int, int]:
    """Return total referenced element counts grouped by glTF primitive mode."""
    mesh_signatures: list[dict[int, int]] = []
    for mesh in document.get("meshes", []):
        mesh_result: dict[int, int] = {}
        for primitive in mesh.get("primitives", []):
            count = accessor_count(document, primitive.get("indices"))
            if not count:
                count = accessor_count(
                    document,
                    primitive.get("attributes", {}).get("POSITION"),
                )
            mode = int(primitive.get("mode", TRIANGLES))
            mesh_result[mode] = mesh_result.get(mode, 0) + count
        mesh_signatures.append(mesh_result)

    result: dict[int, int] = {}
    instances = 0
    for node in document.get("nodes", []):
        mesh = node.get("mesh")
        if not isinstance(mesh, int) or not 0 <= mesh < len(mesh_signatures):
            continue
        instances += 1
        for mode, count in mesh_signatures[mesh].items():
            result[mode] = result.get(mode, 0) + count
    if not instances:
        for signature in mesh_signatures:
            for mode, count in signature.items():
                result[mode] = result.get(mode, 0) + count
    return result


def point_count(document: dict[str, Any]) -> int:
    """Count every POINTS element, including meshes not instanced by a node."""
    total = 0
    for mesh in document.get("meshes", []):
        for primitive in mesh.get("primitives", []):
            if int(primitive.get("mode", TRIANGLES)) != POINTS:
                continue
            count = accessor_count(document, primitive.get("indices"))
            if not count:
                count = accessor_count(
                    document,
                    primitive.get("attributes", {}).get("POSITION"),
                )
            total += count
    return total


def rejection_kinds(
    document: dict[str, Any],
    max_triangles: int = DEFAULT_MAX_TRIANGLES,
) -> list[str]:
    reasons = []
    if document.get("animations"):
        reasons.append("ANIMATION")
    if point_count(document) > 0:
        reasons.append("POINT_CLOUD")
    if scene_statistics(document)[2] > max_triangles:
        reasons.append("TRIANGLE_LIMIT")
    return reasons


def rejection_kind(
    document: dict[str, Any],
    max_triangles: int = DEFAULT_MAX_TRIANGLES,
) -> str | None:
    reasons = rejection_kinds(document, max_triangles=max_triangles)
    return "REJECTED_" + "_AND_".join(reasons) if reasons else None


def colored_index_count(document: dict[str, Any]) -> int:
    mesh_counts: list[int] = []
    for mesh in document.get("meshes", []):
        mesh_total = 0
        for primitive in mesh.get("primitives", []):
            attributes = primitive.get("attributes", {})
            if not any(key.startswith("COLOR_") for key in attributes):
                continue
            count = accessor_count(document, primitive.get("indices"))
            if not count:
                count = accessor_count(document, attributes.get("POSITION"))
            mesh_total += count
        mesh_counts.append(mesh_total)

    total = 0
    instances = 0
    for node in document.get("nodes", []):
        mesh = node.get("mesh")
        if not isinstance(mesh, int) or not 0 <= mesh < len(mesh_counts):
            continue
        instances += 1
        total += mesh_counts[mesh]
    if not instances:
        total = sum(mesh_counts)
    return total


def image_bytes(document: dict[str, Any], binary: bytes, image: dict[str, Any]) -> bytes:
    view_index = image.get("bufferView")
    views = document.get("bufferViews", [])
    if not isinstance(view_index, int) or not 0 <= view_index < len(views):
        return b""
    view = views[view_index]
    start = int(view.get("byteOffset", 0) or 0)
    length = int(view.get("byteLength", 0) or 0)
    return binary[start : start + length]


def jpeg_dimensions(data: bytes) -> tuple[int, int] | None:
    if not data.startswith(b"\xFF\xD8"):
        return None
    offset = 2
    sof = {0xC0, 0xC1, 0xC2, 0xC3, 0xC5, 0xC6, 0xC7, 0xC9, 0xCA, 0xCB, 0xCD, 0xCE, 0xCF}
    while offset + 4 <= len(data):
        if data[offset] != 0xFF:
            offset += 1
            continue
        marker = data[offset + 1]
        offset += 2
        if marker in (0xD8, 0xD9) or 0xD0 <= marker <= 0xD7:
            continue
        length = struct.unpack_from(">H", data, offset)[0]
        if marker in sof and offset + 7 <= len(data):
            height, width = struct.unpack_from(">HH", data, offset + 3)
            return width, height
        offset += length
    return None


def dimensions(data: bytes) -> tuple[int, int] | None:
    if data.startswith(b"\x89PNG\r\n\x1A\n") and len(data) >= 24:
        return struct.unpack_from(">II", data, 16)
    if data.startswith(KTX2_IDENTIFIER) and len(data) >= 28:
        return struct.unpack_from("<II", data, 20)
    return jpeg_dimensions(data)


def image_dimensions(path: Path) -> list[tuple[int, int]]:
    document, binary = read_glb(path)
    result = []
    for image in document.get("images", []):
        value = dimensions(image_bytes(document, binary, image))
        if value:
            result.append(value)
    return result


def max_dimension_label(values: list[tuple[int, int]]) -> str:
    if not values:
        return "none"
    width, height = max(values, key=lambda item: max(item))
    return "{}x{}".format(width, height)


def write_glb(path: Path, document: dict[str, Any], binary: bytes) -> None:
    json_chunk = json.dumps(
        document,
        ensure_ascii=False,
        separators=(",", ":"),
    ).encode("utf-8")
    json_chunk += b" " * ((-len(json_chunk)) % 4)
    binary_chunk = binary + b"\0" * ((-len(binary)) % 4)
    total_length = 12 + 8 + len(json_chunk)
    if binary_chunk:
        total_length += 8 + len(binary_chunk)

    payload = bytearray(struct.pack("<III", GLB_MAGIC, 2, total_length))
    payload.extend(struct.pack("<II", len(json_chunk), JSON_CHUNK))
    payload.extend(json_chunk)
    if binary_chunk:
        payload.extend(struct.pack("<II", len(binary_chunk), BIN_CHUNK))
        payload.extend(binary_chunk)
    path.write_bytes(payload)


def resize_embedded_images(
    source: Path,
    destination: Path,
    max_size: int,
    temp: Path,
    log: Path,
) -> bool:
    document, binary = read_glb(source)
    replacements: dict[int, bytes] = {}
    imagemagick = shutil.which("magick") or shutil.which("convert")

    for image_index, image in enumerate(document.get("images", [])):
        view_index = image.get("bufferView")
        if not isinstance(view_index, int):
            continue
        data = image_bytes(document, binary, image)
        size = dimensions(data)
        if not size or max(size) <= max_size:
            continue
        mime_type = image.get("mimeType")
        if mime_type not in ("image/jpeg", "image/png"):
            raise RuntimeError(
                "embedded image has unsupported resize type: {}".format(mime_type)
            )
        if not imagemagick:
            raise RuntimeError(
                "ImageMagick ('magick' or 'convert') is required to resize "
                "embedded images"
            )

        extension = ".jpg" if mime_type == "image/jpeg" else ".png"
        image_source = temp / "image-{}{}".format(image_index, extension)
        image_output = temp / "image-{}-resized{}".format(image_index, extension)
        image_source.write_bytes(data)

        # Sharp (used by `gltf-transform resize`) and a normal ImageMagick
        # resize premultiply RGB by alpha. Some Sketchfab textures intentionally
        # keep useful RGB in mostly transparent pixels and reuse that texture in
        # opaque/masked materials. Premultiplication turns those textures nearly
        # black. Resize RGB and alpha independently to preserve the source data.
        png_has_alpha = (
            mime_type == "image/png"
            and len(data) > 25
            and data.startswith(b"\x89PNG\r\n\x1A\n")
            and data[25] in (4, 6)
        )
        resize = "{}x{}>".format(max_size, max_size)
        if png_has_alpha:
            rgb_output = temp / "image-{}-rgb.png".format(image_index)
            alpha_output = temp / "image-{}-alpha.png".format(image_index)
            run([
                imagemagick, str(image_source),
                "-alpha", "off",
                "-filter", "Lanczos",
                "-resize", resize,
                str(rgb_output),
            ], log)
            run([
                imagemagick, str(image_source),
                "-alpha", "extract",
                "-filter", "Lanczos",
                "-resize", resize,
                str(alpha_output),
            ], log)
            run([
                imagemagick, str(rgb_output), str(alpha_output),
                "-alpha", "off",
                "-compose", "CopyOpacity",
                "-composite",
                str(image_output),
            ], log)
        else:
            command = [
                imagemagick, str(image_source),
                "-filter", "Lanczos",
                "-resize", resize,
            ]
            if mime_type == "image/jpeg":
                command.extend(["-quality", "95"])
            command.append(str(image_output))
            run(command, log)
        replacements[view_index] = image_output.read_bytes()

    if not replacements:
        return False

    rebuilt = bytearray()
    for view_index, view in enumerate(document.get("bufferViews", [])):
        if int(view.get("buffer", 0) or 0) != 0:
            raise RuntimeError("cannot rebuild GLB containing a non-primary bufferView")
        rebuilt.extend(b"\0" * ((-len(rebuilt)) % 4))
        start = int(view.get("byteOffset", 0) or 0)
        length = int(view.get("byteLength", 0) or 0)
        data = replacements.get(view_index, binary[start : start + length])
        view["byteOffset"] = len(rebuilt)
        view["byteLength"] = len(data)
        rebuilt.extend(data)

    buffers = document.get("buffers", [])
    if not buffers:
        document["buffers"] = [{"byteLength": len(rebuilt)}]
    else:
        buffers[0]["byteLength"] = len(rebuilt)
    write_glb(destination, document, bytes(rebuilt))
    return True


def metadata_signature(document: dict[str, Any]) -> dict[str, Any]:
    collections = (
        "scenes", "nodes", "meshes", "materials", "textures", "images",
        "animations", "skins", "cameras", "samplers",
    )
    metadata: dict[str, Any] = {
        "asset_extras": document.get("asset", {}).get("extras"),
        "root_extras": document.get("extras"),
    }
    for collection in collections:
        metadata[collection] = [
            {"name": item.get("name"), "extras": item.get("extras")}
            for item in document.get(collection, [])
        ]
    return metadata


def texture_slots(document: dict[str, Any]) -> list[list[tuple[str, int, Any]]]:
    def walk(value: Any, path: str, found: list[tuple[str, int, Any]]) -> None:
        if isinstance(value, dict):
            for key, child in value.items():
                child_path = "{}/{}".format(path, key)
                if key.endswith("Texture") and isinstance(child, dict) and isinstance(child.get("index"), int):
                    found.append((child_path, child["index"], child.get("texCoord", 0)))
                walk(child, child_path, found)
        elif isinstance(value, list):
            for index, child in enumerate(value):
                walk(child, "{}/{}".format(path, index), found)

    result = []
    for material in document.get("materials", []):
        found: list[tuple[str, int, Any]] = []
        walk(material, "", found)
        result.append(sorted(found))
    return result


def structural_signature(document: dict[str, Any]) -> dict[str, Any]:
    instances, primitives, _, colors = scene_statistics(document)
    primitive_attributes = [
        sorted(item.get("attributes", {}).keys())
        for mesh in document.get("meshes", [])
        for item in mesh.get("primitives", [])
    ]
    primitive_materials = [
        item.get("material")
        for mesh in document.get("meshes", [])
        for item in mesh.get("primitives", [])
    ]
    return {
        "counts": {
            key: len(document.get(key, []))
            for key in (
                "scenes", "nodes", "meshes", "materials", "textures", "images",
                "animations", "skins", "cameras", "samplers",
            )
        },
        "scene_mesh_instances": instances,
        "scene_primitives": primitives,
        "scene_color_primitives": colors,
        "primitive_attributes": primitive_attributes,
        "primitive_materials": primitive_materials,
        "texture_slots": texture_slots(document),
        "metadata": metadata_signature(document),
    }


def run(command: list[str], log: Path) -> None:
    started = time.monotonic()
    result = subprocess.run(command, capture_output=True, text=True)
    with log.open("a", encoding="utf-8") as file:
        file.write("\n$ {}\n".format(" ".join(command)))
        file.write((result.stdout or "") + (result.stderr or ""))
        file.write("\n[exit={}, elapsed={:.2f}s]\n".format(result.returncode, time.monotonic() - started))
    if result.returncode:
        lines = ((result.stdout or "") + (result.stderr or "")).strip().splitlines()
        raise RuntimeError(lines[-1] if lines else "command failed")


def require_tools(
    has_textures: bool,
    has_ktx2: bool,
    texture_format: str,
) -> None:
    if not shutil.which("gltf-transform"):
        raise RuntimeError("gltf-transform is not installed")
    if has_ktx2 and not shutil.which("ktx"):
        raise RuntimeError("KTX tool 'ktx' is required to decompress source KTX2 textures")
    if has_textures and texture_format == "uastc" and not shutil.which("toktx"):
        raise RuntimeError("KTX tool 'toktx' is required to create UASTC textures")


def install_atomic(temporary: Path, final: Path, overwrite: bool) -> None:
    if final.exists():
        if not overwrite:
            raise FileExistsError("output exists (use --overwrite): {}".format(final))
        final.unlink()
    os.replace(temporary, final)


def process(source: Path, output: Path, args: argparse.Namespace, log: Path) -> Result:
    started = time.monotonic()
    result = Result(file=source.name, input_bytes=source.stat().st_size)
    source_document, _ = read_glb(source)
    source_signature = structural_signature(source_document)
    _, source_primitives, source_triangles, source_colors = scene_statistics(source_document)
    result.primitives_before = source_primitives
    source_dims = image_dimensions(source)
    result.triangles_before = source_triangles
    result.points_before = point_count(source_document)
    result.textures = len(source_document.get("textures", []))
    result.vertex_color_primitives = source_colors
    result.max_texture_before = max_dimension_label(source_dims)
    has_textures = bool(source_document.get("textures") or source_document.get("images"))
    has_ktx2 = "KHR_texture_basisu" in source_document.get("extensionsUsed", [])

    animation_count = len(source_document.get("animations", []))
    channel_count = sum(
        len(animation.get("channels", []))
        for animation in source_document.get("animations", [])
    )
    source_points = point_count(source_document)

    rejection_reasons = []
    if animation_count:
        rejection_reasons.append(
            "source contains {} animation(s) and {} animation channel(s)".format(
                animation_count, channel_count
            )
        )
    if source_points:
        rejection_reasons.append(
            "source contains {} POINTS element(s)".format(source_points)
        )
    if source_triangles > args.max_triangles:
        rejection_reasons.append(
            "source contains {:,} rendered triangles, exceeding the "
            "{:,} triangle limit".format(source_triangles, args.max_triangles)
        )

    if rejection_reasons:
        if output.exists():
            if args.overwrite:
                output.unlink()
            else:
                raise FileExistsError(
                    "rejected asset has an old output "
                    "(use --overwrite or a clean output directory)"
                )

        result.status = (
            rejection_kind(source_document, max_triangles=args.max_triangles)
            or "FAILED"
        )
        result.rejection_reasons = ",".join(
            rejection_kinds(
                source_document,
                max_triangles=args.max_triangles,
            )
        )

        result.note = "rejected: {}; no output GLB written".format(
            "; ".join(rejection_reasons)
        )
        log.write_text(
            "STATUS: {}\n"
            "SOURCE: {}\n"
            "REASON: {}\n".format(result.status, source, result.note),
            encoding="utf-8",
        )
        result.elapsed_seconds = round(time.monotonic() - started, 3)
        return result

    require_tools(has_textures, has_ktx2, args.texture_format)

    output.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix=".optimize-", dir=output.parent) as temp_name:
        temp = Path(temp_name)
        current = source

        if has_ktx2:
            decompressed = temp / "01-decompressed.glb"
            run(["gltf-transform", "ktxdecompress", str(current), str(decompressed)], log)
            current = decompressed

        if has_textures:
            resized = temp / "02-resized.glb"
            if resize_embedded_images(
                current,
                resized,
                args.max_texture_size,
                temp,
                log,
            ):
                current = resized

        if has_textures and args.texture_format == "uastc":
            encoded = temp / "04-uastc.glb"
            run([
                "gltf-transform", "uastc", str(current), str(encoded),
                "--level", str(args.uastc_level),
                "--jobs", str(args.uastc_jobs),
                "--mipmaps", str(args.mipmaps).lower(),
                "--zstd", str(args.uastc_zstd),
            ], log)
            current = encoded

        final_document, _ = read_glb(current)
        final_signature = structural_signature(final_document)
        if source_signature != final_signature:
            for key in source_signature:
                if source_signature[key] != final_signature.get(key):
                    raise RuntimeError("structural preservation failed at: {}".format(key))
            raise RuntimeError("structural preservation failed")

        allowed_added_extensions = (
            {"KHR_texture_basisu"}
            if has_textures and args.texture_format == "uastc"
            else set()
        )
        for key in ("extensionsUsed", "extensionsRequired"):
            source_extensions = set(source_document.get(key, []))
            final_extensions = set(final_document.get(key, []))
            # ktxdecompress intentionally replaces KTX2/BasisU images with
            # ordinary embedded textures, so this extension must disappear in
            # source-texture mode. All other source extensions remain strict.
            allowed_removed_extensions = (
                {"KHR_texture_basisu"}
                if has_ktx2 and args.texture_format == "source"
                else set()
            )
            missing = (
                source_extensions
                - final_extensions
                - allowed_removed_extensions
            )
            unexpected = final_extensions - source_extensions - allowed_added_extensions
            if missing or unexpected:
                raise RuntimeError(
                    "extension preservation failed for {} (missing={}, unexpected={})".format(
                        key, sorted(missing), sorted(unexpected)
                    )
                )

        _, final_primitives, final_triangles, final_colors = scene_statistics(final_document)
        final_points = point_count(final_document)
        if final_colors != source_colors:
            raise RuntimeError("vertex-color primitive count changed")
        final_dims = image_dimensions(current)
        oversized = [value for value in final_dims if max(value) > args.max_texture_size]
        if oversized:
            raise RuntimeError("texture size cap not reached: {}".format(oversized))

        # Some skinned museum assets contain hundreds of thousands of harmless
        # zero-weight joint warnings. Formatting that list can take minutes or
        # overflow Node's call stack. Ignore that specific source-data warning;
        # all other warnings and every actual validation error remain enabled.
        run([
            "gltf-transform", "validate", str(current),
            "--format", "csv",
            "--limit", "10000",
            "--ignore", "ACCESSOR_JOINTS_USED_ZERO_WEIGHT",
        ], log)
        final_temp = temp / "validated-output.glb"
        shutil.copy2(current, final_temp)
        install_atomic(final_temp, output, args.overwrite)

    result.status = "OK"
    result.primitives_after = final_primitives
    result.triangles_after = final_triangles
    result.points_after = final_points
    result.note = "base geometry preserved; triangle count intentionally unchanged"
    if source_triangles:
        result.triangle_reduction_percent = round(
            100.0 * (source_triangles - final_triangles) / source_triangles, 2
        )
    result.max_texture_after = max_dimension_label(final_dims)
    result.output_bytes = output.stat().st_size
    result.elapsed_seconds = round(time.monotonic() - started, 3)
    return result


def write_reports(output_dir: Path, results: list[Result], args: argparse.Namespace) -> None:
    payload = {
        "settings": {
            "max_texture_size": args.max_texture_size,
            "max_triangles": args.max_triangles,
            "texture_format": args.texture_format,
            "uastc_level": args.uastc_level,
            "uastc_zstd": args.uastc_zstd,
            "mipmaps": args.mipmaps,
            "workers": args.workers,
            "uastc_jobs_per_worker": args.uastc_jobs,
            "triangle_policy": "preserve_base_geometry",
            "animation_policy": "reject_without_output",
            "point_cloud_policy": "reject_any_points_primitive_without_output",
            "triangle_policy_limit": "reject_above_{}_rendered_triangles".format(
                args.max_triangles
            ),
        },
        "results": [asdict(item) for item in results],
    }
    (output_dir / "summary.json").write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    with (output_dir / "results.csv").open("w", newline="", encoding="utf-8") as file:
        writer = csv.DictWriter(file, fieldnames=list(asdict(results[0]).keys()) if results else list(Result.__dataclass_fields__))
        writer.writeheader()
        for item in results:
            writer.writerow(asdict(item))


def process_one(
    index: int,
    source: Path,
    output_dir: Path,
    logs: Path,
    args: argparse.Namespace,
) -> tuple[int, Result]:
    started = time.monotonic()
    log = logs / "{}.log".format(source.stem)
    if log.exists():
        log.unlink()
    try:
        item = process(source, output_dir / source.name, args, log)
    except Exception as error:
        item = Result(
            file=source.name,
            input_bytes=source.stat().st_size,
            elapsed_seconds=round(time.monotonic() - started, 3),
            error="{}: {}".format(type(error).__name__, error),
        )
    return index, item


def main() -> int:
    args = parse_args()
    input_dir = args.input_dir.expanduser().resolve()
    output_dir = args.output_dir.expanduser().resolve()
    if not input_dir.is_dir():
        raise NotADirectoryError(input_dir)
    if input_dir == output_dir:
        raise ValueError("input and output directories must be different")
    output_dir.mkdir(parents=True, exist_ok=True)
    logs = output_dir / "logs"
    logs.mkdir(exist_ok=True)
    sources = sorted(input_dir.glob("*.glb"))
    if args.only:
        sources = [item for item in sources if item.name == args.only]
    if not sources:
        raise RuntimeError("no matching GLB files found")
    if args.resume:
        existing = [source for source in sources if (output_dir / source.name).exists()]
        sources = [source for source in sources if not (output_dir / source.name).exists()]
        print(
            "Resume: skipped {} existing output file(s).".format(len(existing)),
            flush=True,
        )
        if not sources:
            print("Nothing left to process.", flush=True)
            return 0

    print(
        "Processing {} file(s) with {} worker(s), {} UASTC job(s) per worker.".format(
            len(sources), args.workers, args.uastc_jobs
        ),
        flush=True,
    )

    results_by_index: dict[int, Result] = {}
    completed = 0
    with ThreadPoolExecutor(max_workers=args.workers) as executor:
        futures = [
            executor.submit(process_one, index, source, output_dir, logs, args)
            for index, source in enumerate(sources)
        ]
        for future in as_completed(futures):
            index, item = future.result()
            completed += 1
            print(
                "[{}/{}] {}".format(completed, len(sources), item.file),
                flush=True,
            )
            if item.status in ("OK", "PARTIAL"):
                print(
                    "  {}: {} -> {} triangles, {} -> {}, {:.1f}s".format(
                        item.status,
                        item.triangles_before, item.triangles_after,
                        item.max_texture_before, item.max_texture_after,
                        item.elapsed_seconds,
                    ),
                    flush=True,
                )
            elif item.status.startswith("REJECTED_"):
                print(
                    "  {}: no output written; {}".format(item.status, item.note),
                    flush=True,
                )
            else:
                print("  FAILED: {}".format(item.error), file=sys.stderr, flush=True)
            results_by_index[index] = item
            results = [results_by_index[key] for key in sorted(results_by_index)]
            write_reports(output_dir, results, args)
    results = [results_by_index[key] for key in sorted(results_by_index)]
    return 1 if any(item.status == "FAILED" for item in results) else 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except KeyboardInterrupt:
        raise SystemExit(130)
