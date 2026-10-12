"""Render ordered transparent PNG layers into one whiteboard scene.

The layer manifest is *explicit*. This is not AI semantic segmentation: the
author chooses which object appears first. PIL works on CPU; ffmpeg composites
each layer while preserving previously completed drawings.
"""
from __future__ import annotations

import subprocess
from pathlib import Path
from PIL import Image, ImageOps

COMPOSITOR_VERSION = "layer-compose-v1"


def _call(cmd: list[str], label: str):
    print(f"[LAYER] {label}", flush=True)
    proc = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                          text=True, encoding="utf-8", errors="replace")
    if proc.returncode:
        raise RuntimeError(f"{label} failed (exit {proc.returncode}):\n{proc.stdout[-3500:]}")


def _prepare_asset(path: Path, dims: tuple[int, int],
                   original_size: tuple[int, int]) -> Image.Image:
    with Image.open(path) as source:
        if source.format != "PNG":
            raise ValueError(f"Layer must be transparent PNG: {path}")
        original = ImageOps.exif_transpose(source).convert("RGBA")
        extrema = original.getchannel("A").getextrema()
        if extrema[0] >= 255:
            raise ValueError(f"PNG layer lacks transparent pixels: {path}")
        if original.size != original_size:
            raise ValueError(f"Layer {path.name} is {original.size}, expected full source canvas {original_size}")
        # Scale the ENTIRE source canvas (not each object bounding box).
        # Geometry stays registered across all layers and the source image.
        fitted = ImageOps.contain(original, dims, method=Image.Resampling.LANCZOS)
        target = Image.new("RGBA", dims, (0, 0, 0, 0))
        target.alpha_composite(
            fitted, ((dims[0] - fitted.width) // 2,
                     (dims[1] - fitted.height) // 2))
        return target


def render_layered_scene(*, layers: list[str], scene_seconds: float, scene_dir: Path,
                         output: Path, engine: Path, width: int, height: int,
                         fps: int, hand: str, prepare_raster,
                         source_canvas: Path):
    if len(layers) > 12 or not layers:
        raise ValueError("One scene supports 1-12 transparent layers")
    if scene_seconds < len(layers) * 0.75:
        raise ValueError(f"Scene duration {scene_seconds:.2f}s too short for {len(layers)} layers")
    scene_dir.mkdir(parents=True, exist_ok=True)
    with Image.open(source_canvas) as source:
        original_size = source.size
    images = [_prepare_asset(Path(p), (width, height), original_size)
              for p in layers]
    scene_clip_paths: list[Path] = []
    complete = Image.new("RGBA", (width, height), (255, 255, 255, 255))
    for index, (img, layer_path) in enumerate(zip(images, layers), start=1):
        layer_folder = scene_dir / f"layer_{index:02d}"
        layer_folder.mkdir(exist_ok=True)
        backdrop = layer_folder / "previous.png"
        complete.convert("RGB").save(backdrop)
        # Export current layer on white to drive the pen-stroke engine.
        pure_layer = Image.new("RGBA", (width, height), "white")
        pure_layer.alpha_composite(img)
        target_image = layer_folder / "layer.png"
        pure_layer.convert("RGB").save(target_image)
        outline, colored = prepare_raster(target_image, layer_folder, width, height, "edges")
        length = (scene_seconds * index / len(layers)
                  - scene_seconds * (index - 1) / len(layers))
        raw_clip = layer_folder / "raw.mp4"
        paint_tail = max(0.3, min(1.4, length * 0.32))
        _call([str(engine), "render-image", str(outline),
               "-o", str(raw_clip),
               "--source-image", str(colored), "--source-fit", "exact",
               "--duration", f"{length:.6f}", "--fps", str(fps),
               "--width", str(width), "--height", str(height),
               "--hand", hand, "--animation-preset", "classic",
               "--color-fill", "fade", "--tail-color", f"{paint_tail:.3f}"],
              f"draw {index}/{len(layers)}: {Path(layer_path).name}")
        composed_clip = layer_folder / "segment.mp4"
        _call(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
               "-loop", "1", "-framerate", str(fps), "-i", str(backdrop),
               "-i", str(raw_clip),
               "-filter_complex",
               "[1:v]format=rgba,colorkey=0xFFFFFF:0.10:0.07[ink];"
               "[0:v][ink]overlay=shortest=1:format=auto,format=yuv420p[v]",
               "-map", "[v]", "-t", f"{length:.6f}",
               "-r", str(fps), "-c:v", "libx264", "-preset", "veryfast",
               "-crf", "23", "-pix_fmt", "yuv420p", str(composed_clip)],
              f"composite layer {index}/{len(layers)}")
        scene_clip_paths.append(composed_clip)
        # The reference composite is always based on ORIGINAL PNG with alpha,
        # never on chroma-keyed output; subsequent layers start in correct state.
        complete.alpha_composite(img)
    complete.convert("RGB").save(scene_dir / "complete.png")
    list_file = scene_dir / "layers.ffconcat"
    list_file.write_text(
        "ffconcat version 1.0\n" +
        "".join("file '" + str(p.resolve()).replace("\\", "/").replace("'", "'\\''")
                + "'\n" for p in scene_clip_paths), encoding="utf-8")
    _call(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
           "-f", "concat", "-safe", "0", "-i", str(list_file),
           "-an", "-c:v", "copy", str(output)],
          "assemble ordered layers")

