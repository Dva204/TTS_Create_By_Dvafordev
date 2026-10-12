"""Propose editable object regions and export aligned transparent PNG layers.

This is light-weight connected-component analysis for whiteboard-style images.
It does NOT semantically identify characters, maps, or timeline labels.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from collections import deque
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageOps
import numpy as np

ALGORITHM = "whiteboard-connected-regions-v1"


def detect_regions(image: Image.Image, max_regions: int = 12) -> list[dict]:
    original = image.convert("RGB")
    sample = ImageOps.contain(original, (640, 480), Image.Resampling.BILINEAR)
    arr = np.asarray(sample, dtype=np.uint8)
    mask = np.min(arr, axis=2) < 225
    coverage = float(mask.mean())
    if coverage > 0.65:
        raise ValueError("Background is not mostly white; automatic segmentation would be unreliable")
    joined = Image.fromarray((mask.astype(np.uint8) * 255), mode="L").filter(
        ImageFilter.MaxFilter(9))
    binary = np.asarray(joined) > 0
    rows, cols = binary.shape
    visited = np.zeros((rows, cols), dtype=bool)
    candidates = []
    for y in range(rows):
        for x in range(cols):
            if not binary[y, x] or visited[y, x]:
                continue
            queue = deque([(x, y)])
            visited[y, x] = True
            x0 = x1 = x
            y0 = y1 = y
            pixels = 0
            while queue:
                cx, cy = queue.popleft()
                pixels += 1
                x0, y0 = min(cx, x0), min(cy, y0)
                x1, y1 = max(cx, x1), max(cy, y1)
                for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
                    nx, ny = cx + dx, cy + dy
                    if 0 <= nx < cols and 0 <= ny < rows and binary[ny, nx] and not visited[ny, nx]:
                        visited[ny, nx] = True
                        queue.append((nx, ny))
            if pixels < 60 or (x1 - x0 + 1) * (y1 - y0 + 1) < 260:
                continue
            margin = 7
            bx0, by0 = max(0, x0 - margin), max(0, y0 - margin)
            bx1, by1 = min(cols, x1 + margin + 1), min(rows, y1 + margin + 1)
            scale_x, scale_y = original.width / cols, original.height / rows
            ox0, oy0 = round(bx0 * scale_x), round(by0 * scale_y)
            ox1, oy1 = round(bx1 * scale_x), round(by1 * scale_y)
            candidates.append((ox0, oy0, min(original.width, ox1), min(original.height, oy1)))
    candidates.sort(key=lambda b: (b[1] // max(1, original.height // 6), b[0]))
    if not candidates:
        raise ValueError("No separate foreground regions found; prepare PNG layers manually")
    if len(candidates) > max_regions:
        raise ValueError(f"Found {len(candidates)} areas; simplify source or define regions manually")
    return [{"name": f"object_{i:02d}", "box": [x0, y0, x1, y1]}
            for i, (x0, y0, x1, y1) in enumerate(candidates, 1)]


def validate_regions(document: dict, width: int, height: int) -> list[dict]:
    regions = document.get("regions") if isinstance(document, dict) else None
    if not isinstance(regions, list) or not (1 <= len(regions) <= 12):
        raise ValueError("regions must be a list of 1-12 objects")
    validated = []
    seen = set()
    for index, region in enumerate(regions, 1):
        if not isinstance(region, dict):
            raise ValueError(f"Region {index} must be an object")
        name = region.get("name", f"object_{index:02d}")
        if not isinstance(name, str) or not re.fullmatch(r"[A-Za-z0-9_-]{1,48}", name):
            raise ValueError(f"Invalid region name: {name}")
        if name in seen:
            raise ValueError("Region names must be unique")
        seen.add(name)
        box = region.get("box")
        if not isinstance(box, list) or len(box) != 4 or any(type(v) is not int for v in box):
            raise ValueError(f"Region {name} requires integer [left,top,right,bottom]")
        x0,y0,x1,y1 = box
        if not (0 <= x0 < x1 <= width and 0 <= y0 < y1 <= height):
            raise ValueError(f"Region {name} is outside image bounds")
        validated.append({"name": name, "box": box})
    return validated


def extract_layers(image: Image.Image, regions: list[dict], folder: Path, force: bool = False):
    output_files = [folder / f"{i:02d}_{region['name']}.png"
                    for i, region in enumerate(regions, 1)]
    if not force and any(out.exists() for out in output_files):
        raise FileExistsError("Layer PNGs already exist; use --force only if overwriting is intended")
    rgb = np.asarray(image.convert("RGB")).astype(np.int32)
    colored = np.asarray(image.convert("RGBA")).copy()
    # White and near-white pixels become transparent. Soft edge remains.
    difference = 255 - rgb.min(axis=2)
    alpha = np.clip((difference - 5) * 255 / 24, 0, 255).astype(np.uint8)
    for region, out in zip(regions, output_files):
        x0,y0,x1,y1 = region["box"]
        masked = np.zeros_like(colored)
        masked[y0:y1, x0:x1] = colored[y0:y1, x0:x1]
        masked[y0:y1, x0:x1, 3] = (
            masked[y0:y1, x0:x1, 3].astype(np.uint16) *
            alpha[y0:y1, x0:x1].astype(np.uint16) // 255
        ).astype(np.uint8)
        Image.fromarray(masked, "RGBA").save(out)
        print("[LAYER_CREATED]", out, flush=True)
    return output_files


def main():
    parser = argparse.ArgumentParser(description="Generate whiteboard layer regions from white-background images")
    parser.add_argument("--image", required=True, help="Source PNG/JPG/WebP image")
    parser.add_argument("--output-dir", help="Where numbered transparent PNGs and regions.json will go")
    parser.add_argument("--apply", action="store_true", help="Extract PNG layers from editable regions.json")
    parser.add_argument("--force", action="store_true", help="Explicitly allow overwriting existing layer PNG files")
    args = parser.parse_args()
    image_path = Path(args.image).resolve()
    if not image_path.is_file():
        raise FileNotFoundError(image_path)
    folder = (Path(args.output_dir).resolve() if args.output_dir
              else image_path.parent / "layers" / image_path.stem)
    folder.mkdir(parents=True, exist_ok=True)
    regions_file = folder / "regions.json"
    with Image.open(image_path) as source:
        original = ImageOps.exif_transpose(source).convert("RGB")
    if regions_file.is_file():
        document = json.loads(regions_file.read_text(encoding="utf-8-sig"))
    else:
        regions = detect_regions(original)
        document = {"_note": "These are geometry-based suggestions, not object recognition. "
                             "Review/edit box coordinates and order, then rerun with --apply.",
                    "source": image_path.name, "width": original.width,
                    "height": original.height, "algorithm": ALGORITHM, "regions": regions}
        regions_file.write_text(json.dumps(document, ensure_ascii=False, indent=2), encoding="utf-8")
    regions = validate_regions(document, original.width, original.height)
    preview = original.copy()
    pen = ImageDraw.Draw(preview)
    for index, entry in enumerate(regions, 1):
        x0,y0,x1,y1 = entry["box"]
        pen.rectangle((x0,y0,x1,y1),outline=(230,30,60),width=3)
        pen.text((x0+4,max(0,y0-15)),f"{index}: {entry['name']}",fill=(205,20,45))
    preview_path = folder / "regions_preview.jpg"
    preview.save(preview_path,quality=90)
    print("[REGIONS]",regions_file,flush=True)
    print("[PREVIEW]",preview_path,flush=True)
    print(f"[COUNT] {len(regions)} regions",flush=True)
    if args.apply:
        extract_layers(original, regions, folder, force=args.force)


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, json.JSONDecodeError) as exc:
        print("[ERROR]",exc,file=sys.stderr,flush=True)
        sys.exit(1)

