"""Build Windows/GDI+ friendly PNG previews for image formats it cannot decode.

Source files are never modified. All preview assets live in user-local cache.
Run one Python process per storyboard, not one per image.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import sys
from pathlib import Path

from PIL import Image, ImageOps

RENDER_LIMIT = (960, 540)
ALLOWED = {".webp"}


def preview_key(source: Path) -> str:
    metadata = source.stat()
    marker = f"{source.resolve()}|{metadata.st_mtime_ns}|{metadata.st_size}|v1"
    return hashlib.sha256(marker.encode("utf-8")).hexdigest()


def prepare(sources: list[str], folder: Path) -> dict[str, str]:
    folder.mkdir(parents=True, exist_ok=True)
    result: dict[str, str] = {}
    for entry in sources:
        file = Path(entry).resolve()
        if not file.is_file() or file.suffix.lower() not in ALLOWED:
            continue
        target = folder / f"{preview_key(file)}.png"
        if not target.is_file() or target.stat().st_size == 0:
            scratch = folder / (target.stem + f".{os.getpid()}.partial.png")
            try:
                with Image.open(file) as original:
                    source = ImageOps.exif_transpose(original)
                    source.thumbnail(RENDER_LIMIT, Image.Resampling.LANCZOS)
                    if source.mode not in ("RGB", "RGBA"):
                        source = source.convert("RGBA" if "A" in source.getbands() else "RGB")
                    source.save(scratch, format="PNG", optimize=False)
                os.replace(scratch, target)
            finally:
                scratch.unlink(missing_ok=True)
        result[str(file)] = str(target)
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate lossless local thumbnails for WebP.")
    parser.add_argument("--input-json", required=True)
    parser.add_argument("--output-json", required=True)
    parser.add_argument("--cache-dir", required=True)
    args = parser.parse_args()
    sources = json.loads(Path(args.input_json).read_text(encoding="utf-8-sig"))
    if not isinstance(sources, list) or len(sources) > 250 or any(not isinstance(s, str) for s in sources):
        raise ValueError("Expected an array of at most 250 image paths")
    mapping = prepare(sources, Path(args.cache_dir))
    destination = Path(args.output_json)
    destination.write_text(json.dumps(mapping, ensure_ascii=False), encoding="utf-8")
    print(f"Created or reused {len(mapping)} preview image(s).", flush=True)
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, json.JSONDecodeError) as error:
        print(f"Image preview conversion failed: {error}", file=sys.stderr)
        sys.exit(1)
