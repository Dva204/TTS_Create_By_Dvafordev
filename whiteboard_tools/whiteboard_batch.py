#!/usr/bin/env python3
"""Local whiteboard batch renderer using existing TTS audio + SRT timeline.

No AI model or paid API is needed. Each ordered image gets a contiguous SRT
group; the linework is rendered before its original color image appears.
"""
from __future__ import annotations

import argparse
import bisect
import hashlib
import json
import re
import shutil
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageFilter, ImageOps, ImageEnhance, ImageChops
from scene_plan import load_plan, make_template, discover_layers
from layer_render import render_layered_scene, COMPOSITOR_VERSION

IMAGE_EXT = {".png", ".jpg", ".jpeg", ".webp", ".bmp", ".svg"}
RASTER_EXT = IMAGE_EXT - {".svg"}


def natural_key(path: Path):
    return [int(s) if s.isdigit() else s.lower()
            for s in re.split(r"(\d+)", path.name)]


def run(cmd: list[str], label: str) -> None:
    print(f"[RUN] {label}", flush=True)
    result = subprocess.run(cmd, stdout=subprocess.PIPE,
                            stderr=subprocess.STDOUT, text=True,
                            encoding="utf-8", errors="replace")
    if result.returncode:
        print(result.stdout[-8000:], flush=True)
        raise RuntimeError(f"{label} failed (exit {result.returncode})")


def duration_of(audio: Path) -> float:
    args = ["ffprobe", "-v", "error", "-show_entries", "format=duration",
            "-of", "default=noprint_wrappers=1:nokey=1", str(audio)]
    proc = subprocess.run(args, capture_output=True, text=True, encoding="utf-8")
    if proc.returncode:
        raise RuntimeError("ffprobe failed: " + proc.stderr[-1000:])
    return float(proc.stdout.strip())


def srt_seconds(value: str) -> float:
    value = value.replace(",", ".")
    h, m, s = value.split(":")
    return int(h) * 3600 + int(m) * 60 + float(s)


def parse_srt(path: Path) -> list[tuple[float, float, str]]:
    raw = path.read_text(encoding="utf-8-sig")
    cues = []
    for block in re.split(r"\r?\n\s*\r?\n", raw.strip()):
        lines = [line.strip() for line in block.splitlines() if line.strip()]
        if not lines:
            continue
        line_index = next((i for i, line in enumerate(lines) if "-->" in line), None)
        if line_index is None:
            continue
        timing = lines[line_index].split("-->")
        start, end = srt_seconds(timing[0].strip()), srt_seconds(timing[1].strip())
        if end <= start or start < 0:
            raise ValueError("Invalid SRT timing: " + lines[line_index])
        cues.append((start, end, " ".join(lines[line_index + 1:])))
    if not cues:
        raise ValueError("SRT contains no usable cues")
    if any(b[0] < a[0] for a, b in zip(cues, cues[1:])):
        raise ValueError("SRT cues must be sorted")
    return cues


def boundaries(cues: list[tuple[float, float, str]], count: int, total: float):
    if count > len(cues):
        raise ValueError(
            f"{count} images but only {len(cues)} SRT entries; provide fewer images")
    if total <= 0:
        raise ValueError("Audio duration must be greater than zero")
    positions = [c[0] for c in cues]
    result = [0]
    # Greedily partition cues around target timestamps, preserving at least one
    # cue in every image group. Boundaries fall on the next cue start.
    for part in range(1, count):
        low = result[-1] + 1
        high = len(cues) - (count - part)
        target = total * part / count
        insertion = bisect.bisect_left(positions, target, low, high + 1)
        candidates = [max(low, min(high, insertion)),
                      max(low, min(high, insertion - 1))]
        idx = min(candidates, key=lambda pos: abs(positions[pos] - target))
        result.append(idx)
    result.append(len(cues))
    scenes = []
    for i in range(count):
        start = 0.0 if i == 0 else positions[result[i]]
        end = total if i == count - 1 else positions[result[i + 1]]
        if end <= start:
            raise ValueError(f"Scene {i + 1} has zero duration")
        scenes.append({"index": i + 1, "start": start,
                       "end": end, "duration": end - start,
                       "cue_from": result[i] + 1, "cue_to": result[i + 1],
                       "text": " ".join(t for _, _, t in cues[result[i]:result[i + 1]])})
    return scenes


def prepare_raster(path: Path, target: Path, width: int, height: int, mode: str = "edges") -> tuple[Path, Path]:
    target.mkdir(parents=True, exist_ok=True)
    with Image.open(path) as inp:
        src = ImageOps.exif_transpose(inp).convert("RGBA")
        bg = Image.new("RGBA", src.size, "white")
        bg.alpha_composite(src)
        colored = ImageOps.contain(bg.convert("RGB"), (width, height),
                                   method=Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", (width, height), "white")
    canvas.paste(colored, ((width - colored.width) // 2, (height - colored.height) // 2))
    color_path = target / "source.png"
    canvas.save(color_path)
    # CPU modes; useful for AI illustrations drawn on light backgrounds.
    # None of these modes performs semantic object segmentation.
    gray = ImageOps.autocontrast(canvas.convert("L"))
    if mode == "clean-detail":
        # V3: combine thin luminance edges with the border of dark shapes.
        # Unlike an ink threshold, solid hair/clothing regions remain WHITE
        # inside. This prevents dark blobs from appearing as thick brush strokes.
        contour = gray.filter(ImageFilter.GaussianBlur(radius=0.75))
        contour = contour.filter(ImageFilter.FIND_EDGES)
        contour = contour.point(lambda value: 0 if value >= 34 else 255)
        dark = gray.point(lambda value: 0 if value < 126 else 255)
        dark_border = ImageOps.invert(ImageChops.difference(
            dark, dark.filter(ImageFilter.MaxFilter(3))))
        edges = ImageChops.darker(contour, dark_border)
    elif mode == "ink":
        # Preserve dark outlines instead of outlining all color transitions.
        edges = gray.point(lambda value: 0 if value < 140 else 255)
    else:
        blur_radius = 0.7 if mode == "detail" else 1.1
        threshold = 26 if mode == "detail" else 48
        edges = gray.filter(ImageFilter.GaussianBlur(radius=blur_radius))
        edges = edges.filter(ImageFilter.FIND_EDGES)
        edges = edges.point(lambda value: 0 if value >= threshold else 255)
        if mode == "detail":
            dark_ink = gray.point(lambda value: 0 if value < 68 else 255)
            edges = ImageChops.darker(edges, dark_ink)
    line_path = target / "lineart.png"
    edges.save(line_path)
    return line_path, color_path


def render(args) -> None:
    audio, srt, images_dir = Path(args.audio), Path(args.srt), Path(args.images)
    engine = Path(args.engine)
    out = Path(args.output).resolve()
    for path in (audio, srt, engine):
        if not path.is_file():
            raise FileNotFoundError(f"Missing file: {path}")
    if not images_dir.is_dir():
        raise NotADirectoryError(str(images_dir))
    images = sorted([p for p in images_dir.iterdir()
                     if p.is_file() and p.suffix.lower() in IMAGE_EXT],
                    key=natural_key)
    if not images:
        raise ValueError("No supported images in folder")
    total = duration_of(audio)
    cues = parse_srt(srt)
    if cues[-1][1] > total + 0.5:
        raise ValueError(f"SRT ends at {cues[-1][1]:.2f}s but audio ends at {total:.2f}s")
    scenes = boundaries(cues, len(images), total)
    if args.write_template:
        template = make_template(images, len(cues), scenes)
        dest = Path(args.write_template).resolve()
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_text(json.dumps(template, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"[TEMPLATE] {dest}", flush=True)
        return
    if args.scene_plan:
        scenes = load_plan(Path(args.scene_plan), images_dir, len(cues),
                           cues, total, IMAGE_EXT)
    else:
        for scene, img in zip(scenes, images):
            scene["layers"] = [str(p.resolve()) for p in discover_layers(img)]
    out.parent.mkdir(parents=True, exist_ok=True)
    work = out.parent / (out.stem + ".whiteboard_work")
    work.mkdir(parents=True, exist_ok=True)
    segment_list = []
    for scene in scenes:
        img = Path(scene["image"]) if "image" in scene else images[scene["index"] - 1]
        n = scene["index"]
        scene["image"] = str(img.resolve())
        scene_dir = work / f"scene_{n:03d}"
        scene_dir.mkdir(exist_ok=True)
        clip = scene_dir / "clip.mp4"
        preset = scene.get("preset", args.preset)
        fill_style = "fade" if preset == "classic" else args.color_fill
        color_seconds = max(0.65, min(2.5, scene["duration"] * 0.30))
        layers = scene.get("layers", [])
        parts = [str(img.resolve()), str(img.stat().st_size), str(img.stat().st_mtime_ns),
                 f"{scene['duration']:.5f}", str(args.width), str(args.height),
                 str(args.fps), preset, args.hand, fill_style, str(color_seconds),
                 args.lineart_mode, str(scene.get("draw_blocks", args.draw_blocks)),
                 scene.get("block_order", args.block_order),
                 json.dumps(scene.get("block_sequence", [])), COMPOSITOR_VERSION]
        for asset in layers:
            layer_path = Path(asset)
            parts += [str(layer_path), str(layer_path.stat().st_size),
                      str(layer_path.stat().st_mtime_ns)]
        override = scene.get("lineart", "")
        if override:
            lp = Path(override)
            parts += [str(lp), str(lp.stat().st_size), str(lp.stat().st_mtime_ns)]
        fingerprint = hashlib.sha256("|".join(parts).encode("utf-8")).hexdigest()
        old_hash = scene_dir / "render_hash.txt"
        if not (clip.exists() and old_hash.exists()
                and old_hash.read_text(encoding="ascii") == fingerprint
                and duration_of(clip) > 0.1):
            if layers:
                print(f"[SCENE {n}/{len(scenes)}] {len(layers)} ordered layers, {scene['duration']:.2f}s", flush=True)
                render_layered_scene(
                    layers=layers, scene_seconds=scene["duration"],
                    scene_dir=scene_dir, output=clip, engine=engine,
                    width=args.width, height=args.height,
                    fps=args.fps, hand=args.hand,
                    prepare_raster=prepare_raster,
                    source_canvas=img)
                old_hash.write_text(fingerprint, encoding="ascii")
                segment_list.append(clip)
                continue
            source = None
            if img.suffix.lower() in RASTER_EXT:
                lineart, source = prepare_raster(img, scene_dir, args.width,
                                                 args.height, args.lineart_mode)
                source_input = Path(override) if override else lineart
            else:
                source_input = Path(override) if override else img
            command = [str(engine), "render-image", str(source_input),
                       "-o", str(clip), "--duration",
                       f"{scene['duration']:.6f}", "--width", str(args.width),
                       "--height", str(args.height), "--fps", str(args.fps),
                       "--hand", args.hand, "--animation-preset", preset,
                       "--color-fill", fill_style,
                       "--tail-color", f"{color_seconds:.3f}",
                       "--draw-blocks", str(scene.get("draw_blocks", args.draw_blocks)),
                       "--block-order", scene.get("block_order", args.block_order)] 
            block_seq = scene.get("block_sequence", [])
            if block_seq:
                command.extend(["--block-sequence", ",".join(str(v) for v in block_seq)])
            if source:
                command += ["--source-image", str(source),
                            "--source-fit", "exact"]
            print(f"[SCENE {n}/{len(scenes)}] {scene['duration']:.2f}s {img.name}", flush=True)
            run(command, "Render scene " + str(n))
            old_hash.write_text(fingerprint, encoding="ascii")
        else:
            print(f"[CACHE] Scene {n} unchanged", flush=True)
        segment_list.append(clip)
    concat = work / "scenes.ffconcat"
    # Escape single quotes for ffconcat; paths are written as forward slashes.
    concat.write_text(
        "ffconcat version 1.0\n" +
        "".join("file '" + str(p.resolve()).replace("\\", "/").replace("'", "'\\''")
                + "'\n" for p in segment_list), encoding="utf-8")
    temp_output = work / "combined.tmp.mp4"
    run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-f", "concat",
         "-safe", "0", "-i", str(concat), "-c:v", "copy", "-an",
         str(temp_output)], "Join scene clips")
    # Upscale only AFTER the expensive pen-stroke rendering. The 720p render
    # remains cached and reusable when the final export profile changes.
    final_w = args.final_width or args.width
    final_h = args.final_height or args.height
    common = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
              "-i", str(temp_output), "-i", str(audio), "-map", "0:v:0",
              "-map", "1:a:0"]
    common_end = ["-c:a", "aac", "-b:a", "192k", "-t", f"{total:.6f}",
                  "-movflags", "+faststart", str(out)]
    if (final_w != args.width or final_h != args.height):
        filters = f"scale={final_w}:{final_h}:flags=lanczos,format=yuv420p"
        try:
            run(common + ["-vf", filters, "-c:v", "h264_nvenc",
                          "-preset", "p4", "-cq", "22"] + common_end,
                "Export upscale NVENC")
        except RuntimeError:
            print("[WARN] NVENC failed, retrying CPU x264", flush=True)
            run(common + ["-vf", filters, "-c:v", "libx264",
                          "-crf", "22", "-preset", "veryfast"] + common_end,
                "Export upscale CPU")
    else:
        run(common + ["-c:v", "copy"] + common_end, "Mux narration")
    manifest = out.with_suffix(".whiteboard.json")
    manifest.write_text(json.dumps({
        "audio": str(audio.resolve()), "srt": str(srt.resolve()),
        "output": str(out), "audio_seconds": total,
        "render_width": args.width, "render_height": args.height,
        "width": final_w, "height": final_h,
        "fps": args.fps, "scene_count": len(scenes),
        "scenes": scenes,
    }, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"[DONE] {out} ({total:.2f}s, {len(scenes)} scenes)", flush=True)


def main() -> int:
    parser = argparse.ArgumentParser(description="Local whiteboard video batch generator")
    parser.add_argument("--images", required=True, help="Folder of ordered images")
    parser.add_argument("--audio", required=True, help="Full WAV/MP3 TTS audio")
    parser.add_argument("--srt", required=True, help="SRT synchronized with audio")
    parser.add_argument("--output", required=True, help="Output MP4")
    parser.add_argument("--engine", required=True, help="Absolute path to whiteboard.exe")
    parser.add_argument("--width", type=int, default=1280)
    parser.add_argument("--height", type=int, default=720)
    parser.add_argument("--final-width", type=int, default=0,
                        help="Optional final export width; uses Lanczos upscaling")
    parser.add_argument("--final-height", type=int, default=0,
                        help="Optional final export height")
    parser.add_argument("--fps", type=int, default=24)
    parser.add_argument("--hand", default="asian")
    parser.add_argument("--preset", choices=["classic", "block-speedpaint"],
                        default="classic")
    parser.add_argument("--color-fill", default="contour-wipe")
    parser.add_argument("--lineart-mode", choices=["edges", "detail", "ink", "clean-detail"],
                        default="edges")
    parser.add_argument("--draw-blocks", type=int, default=4)
    parser.add_argument("--block-order", choices=["reading", "source"], default="reading")
    parser.add_argument("--scene-plan", default="", help="Optional explicit scene-to-SRT JSON map")
    parser.add_argument("--write-template", default="", help="Write editable scene plan JSON and exit")
    args = parser.parse_args()
    try:
        if args.width <= 0 or args.height <= 0 or args.fps <= 0:
            raise ValueError("Invalid dimensions or FPS")
        if args.width % 2 or args.height % 2 or args.final_width % 2 or args.final_height % 2:
            raise ValueError("Video dimensions must be even for H.264")
        if args.final_width < 0 or args.final_height < 0:
            raise ValueError("Final dimensions cannot be negative")
        if bool(args.final_width) != bool(args.final_height):
            raise ValueError("Specify both final-width and final-height")
        if not 1 <= args.draw_blocks <= 12:
            raise ValueError("draw-blocks must be between 1 and 12")
        render(args)
        return 0
    except (OSError, ValueError, RuntimeError) as error:
        print(f"[ERROR] {error}", file=sys.stderr, flush=True)
        return 1


if __name__ == "__main__":
    sys.exit(main())

