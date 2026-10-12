"""Optional scene plan: map numbered SRT cues to images and renderer options."""
from __future__ import annotations

import json
import re
from pathlib import Path


def discover_layers(image: Path) -> list[Path]:
    """Optional layers/<image-stem>/*.png, ordered by numeric filename."""
    folder = image.parent / "layers" / image.stem
    if not folder.is_dir():
        return []
    candidates = [p for p in folder.iterdir()
                  if p.is_file() and p.suffix.lower() == ".png"]
    def numeric_key(path: Path):
        return [int(chunk) if chunk.isdigit() else chunk.lower()
                for chunk in re.split(r"(\d+)", path.name)]
    return sorted(candidates, key=numeric_key)


def make_template(images: list[Path], cue_count: int, boundaries) -> dict:
    # Produces an editable, explicit scene-to-SRT mapping.
    # Auto boundaries should only be used to create the initial template.
    result = []
    for scene, image in zip(boundaries, images):
        result.append({
            "image": image.name,
            "cue_from": scene["cue_from"],
            "cue_to": scene["cue_to"],
            "draw_blocks": 4,
            "block_order": "reading",
            "block_sequence": [],
            "preset": "classic",
            "lineart": "",
            "layers": [str(p.relative_to(image.parent)).replace("\\", "/")
                       for p in discover_layers(image)],
        })
    return {
        "_help": "cue_from/cue_to are inclusive 1-based SRT indices. "
                 "Change the ranges so each scene matches the spoken topic. "
                 "block_sequence are optional inferred block IDs, e.g. [1,0]. "
                 "Optional lineart is a PNG path relative to the images folder.",
        "scenes": result
    }


def load_plan(path: Path, images_dir: Path, cue_count: int,
              cues: list[tuple[float, float, str]], total: float,
              allowed_exts: set[str]) -> list[dict]:
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(data, dict) or not isinstance(data.get("scenes"), list) or not data["scenes"]:
        raise ValueError("Scene plan must be an object with a non-empty 'scenes' list")
    groups = []
    expected = 1
    for index, spec in enumerate(data["scenes"], start=1):
        if not isinstance(spec, dict):
            raise ValueError(f"Scene {index}: expected JSON object")
        raw_img = spec.get("image")
        if not isinstance(raw_img, str) or not raw_img.strip():
            raise ValueError(f"Scene {index}: missing 'image'")
        image = (images_dir / raw_img).resolve()
        try:
            image.relative_to(images_dir.resolve())
        except ValueError as e:
            raise ValueError(f"Scene {index}: image outside selected folder") from e
        if image.suffix.lower() not in allowed_exts or not image.is_file():
            raise ValueError(f"Scene {index}: image missing or unsupported: {raw_img}")
        lo, hi = spec.get("cue_from"), spec.get("cue_to")
        if type(lo) is not int or type(hi) is not int or lo != expected or hi < lo or hi > cue_count:
            raise ValueError(f"Scene {index}: SRT cue range must start at {expected} and end between {expected} and {cue_count}")
        expected = hi + 1
        order = spec.get("block_order", "reading")
        if order not in ("reading", "source"):
            raise ValueError(f"Scene {index}: invalid block_order '{order}'")
        blocks = spec.get("draw_blocks", 4)
        if type(blocks) is not int or not 1 <= blocks <= 12:
            raise ValueError(f"Scene {index}: draw_blocks must be 1-12")
        sequence = spec.get("block_sequence", [])
        if not isinstance(sequence, list) or any(type(v) is not int or v < 0 for v in sequence) or len(sequence) != len(set(sequence)):
            raise ValueError(f"Scene {index}: block_sequence must contain unique positive-or-zero integers")
        preset = spec.get("preset", "classic")
        if preset not in ("classic", "block-speedpaint"):
            raise ValueError(f"Scene {index}: invalid preset")
        raw_layers = spec.get("layers", [])
        if not isinstance(raw_layers, list) or len(raw_layers) > 12:
            raise ValueError(f"Scene {index}: layers must be a list of up to 12 PNG files")
        layer_paths = []
        for raw in raw_layers:
            if not isinstance(raw, str) or not raw:
                raise ValueError(f"Scene {index}: each layer must be a PNG filename")
            layer = (images_dir / raw).resolve()
            try:
                layer.relative_to(images_dir.resolve())
            except ValueError as e:
                raise ValueError(f"Scene {index}: layer outside selected folder") from e
            if layer.suffix.lower() != ".png" or not layer.is_file():
                raise ValueError(f"Scene {index}: invalid transparent PNG layer: {raw}")
            layer_paths.append(str(layer))
        lineart_name = spec.get("lineart", "")
        lineart = None
        if lineart_name:
            if not isinstance(lineart_name, str):
                raise ValueError(f"Scene {index}: invalid lineart path")
            lineart = (images_dir / lineart_name).resolve()
            try:
                lineart.relative_to(images_dir.resolve())
            except ValueError as e:
                raise ValueError(f"Scene {index}: lineart outside selected folder") from e
            if lineart.suffix.lower() not in (".png", ".svg") or not lineart.is_file():
                raise ValueError(f"Scene {index}: lineart must be a real PNG/SVG file")
        start = 0.0 if lo == 1 else cues[lo - 1][0]
        end = total if hi == cue_count else cues[hi][0]
        if end <= start:
            raise ValueError(f"Scene {index}: zero or negative duration")
        groups.append({
            "index": index, "start": start, "end": end,
            "duration": end - start,
            "text": " ".join(t for _, _, t in cues[lo - 1:hi]),
            "cue_from": lo, "cue_to": hi, "image": str(image),
            "draw_blocks": blocks, "block_order": order,
            "block_sequence": sequence, "preset": preset,
            "lineart": str(lineart) if lineart else "",
            "layers": layer_paths,
        })
    if expected != cue_count + 1:
        raise ValueError(f"Scene plan ends at cue {expected - 1} but SRT has {cue_count} cues")
    return groups

