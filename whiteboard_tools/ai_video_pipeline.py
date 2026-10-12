#!/usr/bin/env python3
"""Render a schemaVersion=1 AI Video Studio storyboard locally.

Styles implemented in this version: whiteboard, layer, image-motion,
image-story and text. Other future styles fail explicitly.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import shutil
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, ImageOps

CURRENT_VERSION = "ai-video-render-v2.0.0-rc1"
SUPPORTED = {"whiteboard", "layer", "image-motion", "image-story", "text"}
RESAMPLE = Image.Resampling.LANCZOS


def say(message: str):
    print(message, flush=True)


def execute(command: list[str], description: str, cwd: Path | None = None):
    say("[RUN] " + description)
    completed = subprocess.run(command, stdout=subprocess.PIPE,
                               stderr=subprocess.STDOUT, cwd=cwd, encoding="utf-8",
                               errors="replace")
    if completed.returncode:
        raise RuntimeError(f"{description} (exit {completed.returncode}):\n" +
                           completed.stdout[-4000:])


def probe_seconds(path: Path) -> float:
    completed = subprocess.run([
        "ffprobe", "-v", "error", "-show_entries", "format=duration",
        "-of", "default=noprint_wrappers=1:nokey=1", str(path)
    ], capture_output=True, encoding="utf-8", errors="replace")
    if completed.returncode or not completed.stdout.strip():
        raise ValueError("Unable to probe audio/video duration: " + str(path))
    return float(completed.stdout.strip())


def digest_file(path: Path) -> str:
    """Stream hashing for long TTS audio without loading 100+ MB into RAM."""
    checksum = hashlib.sha256()
    with path.open("rb") as reader:
        for piece in iter(lambda: reader.read(1024 * 1024), b""):
            checksum.update(piece)
    return checksum.hexdigest()


def resolve(root: Path, name: str) -> Path:
    candidate = Path(name)
    return (candidate if candidate.is_absolute() else root / candidate).resolve()


def load_project(path: Path, audio_seconds: float, scene_index: int = 0):
    raw = json.loads(path.read_text(encoding="utf-8-sig"))
    if raw.get("schemaVersion") != 1:
        raise ValueError("Unsupported schemaVersion; expected 1")
    root = path.parent
    scenes = raw.get("scenes", [])
    if not isinstance(scenes, list) or not scenes:
        raise ValueError("Project must have at least one scene")
    last = 0
    expected_cue = 1
    prepared = []
    for index, scene in enumerate(scenes, 1):
        if not isinstance(scene, dict):
            raise ValueError(f"Scene {index}: invalid JSON object")
        style = scene.get("styleId")
        selected_for_render = not scene_index or scene_index == index
        if selected_for_render and style not in SUPPORTED:
            raise ValueError(f"Scene {index}: style '{style}' has no renderer yet. "
                             "Choose Whiteboard, Layer, Zoom/Pan, Image Story or Text.")
        start, end = scene.get("startMs"), scene.get("endMs")
        if any(type(v) is not int for v in (start, end)):
            raise ValueError(f"Scene {index}: startMs/endMs must be integers")
        if start != last or end <= start:
            raise ValueError(f"Scene {index}: timeline gap/overlap or invalid duration")
        if scene.get("index") != index or scene.get("cueFrom") != expected_cue:
            raise ValueError(f"Scene {index}: invalid scene index/SRT cue order")
        cue_to = scene.get("cueTo")
        if type(cue_to) is not int or cue_to < expected_cue:
            raise ValueError(f"Scene {index}: cueTo invalid")
        image_name = scene.get("imagePath") or ""
        image = resolve(root, image_name) if image_name else None
        if selected_for_render and style != "text" and (not image or not image.is_file()):
            raise ValueError(f"Scene {index}: image file missing: {image_name}")
        if selected_for_render and style == "text" and image and not image.is_file():
            raise ValueError(f"Scene {index}: optional background image missing")
        if end / 1000 > audio_seconds + 0.4:
            raise ValueError(f"Scene {index}: scene extends beyond audio duration")
        from director_effects import validate_events
        direction = validate_events(scene.get("directionEvents") or [], end - start)
        transition = scene.get("transition", "fade")
        if transition not in {"cut", "fade"}:
            raise ValueError(f"Scene {index}: unsupported transition '{transition}'")
        prepared.append({"index": index, "style": style,
                         "start": start / 1000, "end": end / 1000,
                         "image": image,
                         "text": str(scene.get("narration") or ""),
                         "notes": str(scene.get("notes") or ""),
                         "directionEvents": direction,
                         "transition": transition})
        expected_cue = cue_to + 1
        last = end
    if audio_seconds - prepared[-1]["end"] > 10:
        raise ValueError("SRT ends more than 10 seconds before audio; check sync")
    # Extend final scene to the end of narration (usual trailing silence).
    prepared[-1]["end"] = audio_seconds
    return raw, prepared


def create_canvas(image_path: Path, destination: Path, width: int, height: int):
    with Image.open(image_path) as source:
        base = ImageOps.exif_transpose(source).convert("RGBA")
        flattened = Image.new("RGBA", base.size, "white")
        flattened.alpha_composite(base)
        fitted = ImageOps.contain(flattened.convert("RGB"), (width, height), RESAMPLE)
    canvas = Image.new("RGB", (width, height), "white")
    canvas.paste(fitted, ((width - fitted.width) // 2, (height - fitted.height) // 2))
    canvas.save(destination)


def font_at(size: int):
    for candidate in (
        Path("C:/Windows/Fonts/segoeuib.ttf"),
        Path("C:/Windows/Fonts/arialbd.ttf"),
        Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"),
    ):
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


def wrap_text(text: str, d: ImageDraw.ImageDraw, font, max_width: int):
    result, line = [], ""
    for word in text.split():
        trial = word if not line else line + " " + word
        if line and d.textbbox((0, 0), trial, font=font)[2] > max_width:
            result.append(line)
            line = word
        else:
            line = trial
    if line:
        result.append(line)
    return result[:8]


def render_text(output: Path, text: str, width: int, height: int,
                fps: int, frames: int, background: Path | None):
    base = Image.new("RGB", (width, height), "white")
    if background:
        bg = output.parent / "text_background.png"
        create_canvas(background, bg, width, height)
        base = Image.open(bg).convert("RGB")
    font = font_at(max(18, min(45, width // 16)))
    draw = ImageDraw.Draw(base)
    message = " ".join(text.split()).strip() or "Untitled scene"
    lines = wrap_text(message, draw, font, int(width * .77))
    visible_text = "\n".join(lines)
    # A dedicated lower-third paper card keeps typography legible over
    # colorful illustrations instead of crossing existing drawn objects.
    count = len(visible_text)
    duration_typing = max(1, int(frames * .65))
    line_height = max(24, int(font.size * 1.43))
    panel_y1 = max(8, int(height * .52))
    panel_y2 = min(height - 8, max(int(height * .84),
                                   panel_y1 + 30 + line_height * len(lines)))
    start_at_y = panel_y1 + max(6, (panel_y2 - panel_y1 -
                        line_height * len(lines)) // 2)
    command = [
        "ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
        "-f", "rawvideo", "-pixel_format", "rgb24", "-video_size",
        f"{width}x{height}", "-framerate", str(fps), "-i", "-",
        "-an", "-c:v", "libx264", "-preset", "veryfast", "-crf", "23",
        "-pix_fmt", "yuv420p", str(output)
    ]
    proc = subprocess.Popen(command, stdin=subprocess.PIPE, stderr=subprocess.PIPE)
    try:
        assert proc.stdin is not None
        for frame in range(frames):
            snapshot = base.copy()
            pen = ImageDraw.Draw(snapshot)
            pen.rounded_rectangle(
                (int(width * .075), panel_y1, int(width * .925), panel_y2),
                radius=max(6, width // 70), fill=(251, 250, 247),
                outline=(63, 78, 95), width=max(1, width // 300))
            progress = min(count, round(count * frame / duration_typing))
            shown = visible_text[:progress]
            for row, part in enumerate(shown.split("\n")):
                pen.text((int(width * .115), start_at_y + row * line_height),
                         part, fill=(24, 37, 53), font=font)
            proc.stdin.write(snapshot.tobytes())
        proc.stdin.close()
        errors = proc.stderr.read().decode("utf-8", "replace") if proc.stderr else ""
        code = proc.wait()
        if code:
            raise RuntimeError("Text rendering failed: " + errors[-3000:])
    except BaseException:
        if proc.poll() is None:
            proc.kill()
            proc.wait()
        raise


def render_motion(image: Path, output: Path, width: int, height: int,
                  fps: int, frames: int, folder: Path, static: bool):
    prepared = folder / "prepared.png"
    create_canvas(image, prepared, width, height)
    if static:
        filters = f"fps={fps},format=yuv420p"
        cmd = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
               "-loop", "1", "-framerate", str(fps), "-i", str(prepared),
               "-vf", filters, "-frames:v", str(frames), "-an",
               "-c:v", "libx264", "-preset", "veryfast", "-crf", "23",
               "-pix_fmt", "yuv420p", str(output)]
    else:
        # Zoom by no more than 12%; always focus the center to prevent black edges.
        step = .12 / max(1, frames)
        vf = (f"zoompan=z='min(zoom+{step:.8f},1.12)':"
              "x='iw/2-iw/zoom/2':y='ih/2-ih/zoom/2':"
              f"d={frames}:s={width}x{height}:fps={fps},format=yuv420p")
        cmd = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
               "-i", str(prepared), "-vf", vf, "-frames:v", str(frames),
               "-an", "-c:v", "libx264", "-preset", "veryfast", "-crf", "23",
               "-pix_fmt", "yuv420p", str(output)]
    execute(cmd, "Image motion / story")


def render_whiteboard(scene: dict, output: Path, folder: Path,
                      width: int, height: int, fps: int, engine: Path,
                      python_folder: Path):
    if not engine.is_file():
        raise ValueError("Whiteboard engine not found: " + str(engine))
    from whiteboard_batch import prepare_raster
    from scene_plan import discover_layers
    from layer_render import render_layered_scene
    image = scene["image"]
    assert image
    layers = [str(p.resolve()) for p in discover_layers(image)]
    if scene["style"] == "layer" and layers:
        render_layered_scene(layers=layers,
                             scene_seconds=scene["end"] - scene["start"],
                             scene_dir=folder, output=output, engine=engine,
                             width=width, height=height, fps=fps, hand="asian",
                             prepare_raster=prepare_raster, source_canvas=image)
        return
    # A missing layer collection is an error, not a silent downgrade.
    if scene["style"] == "layer":
        raise ValueError("Layer style requires transparent PNGs under images/layers/<image stem>/")
    from PIL import UnidentifiedImageError
    source_image = None
    if image.suffix.lower() == ".svg":
        strokes = image
    else:
        strokes, source_image = prepare_raster(image, folder, width, height, "edges")
    length = scene["end"] - scene["start"]
    tail = max(.35, min(2.2, length * .3))
    cmd = [str(engine), "render-image", str(strokes),
           "-o", str(output), "--duration", f"{length:.6f}",
           "--width", str(width), "--height", str(height),
           "--fps", str(fps), "--hand", "asian", "--animation-preset",
           "classic", "--color-fill", "fade",
           "--tail-color", f"{tail:.3f}"]
    if source_image:
        cmd += ["--source-image", str(source_image), "--source-fit", "exact"]
    execute(cmd, "Whiteboard scene")


def normalize_clip(raw: Path, destination: Path, width: int,
                   height: int, fps: int, frames: int, fade_in: bool = False):
    # All scene clips share codec, dimensions, pixel format and timebase.
    # A subtle white fade never overlaps two clips or moves SRT audio timing.
    chain = (f"scale={width}:{height}:flags=lanczos,"
             f"fps={fps},tpad=stop_mode=clone:stop_duration=2")
    if fade_in:
        seconds = min(.28, (frames / fps) * .35)
        chain += f",fade=t=in:st=0:d={seconds:.3f}:color=white"
    chain += ",format=yuv420p"
    execute([
        "ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
        "-i", str(raw), "-vf", chain,
        "-frames:v", str(frames), "-an", "-c:v", "libx264", "-preset",
        "veryfast", "-crf", "23", "-pix_fmt", "yuv420p",
        "-video_track_timescale", str(fps * 1000), str(destination)
    ], "Normalize scene")


def digest_scene(scene: dict, width: int, height: int, fps: int, frames: int):
    chunks = [CURRENT_VERSION, scene["style"], str(width), str(height),
              str(fps), str(frames), scene["text"], scene["notes"],
              json.dumps(scene.get("directionEvents", []), sort_keys=True),
              scene.get("transition", "fade")]
    image = scene["image"]
    if image:
        chunks += [str(image), hashlib.sha256(image.read_bytes()).hexdigest()]
        if scene["style"] == "layer":
            from scene_plan import discover_layers
            for layer in discover_layers(image):
                chunks += [str(layer), hashlib.sha256(layer.read_bytes()).hexdigest()]
    return hashlib.sha256("\n".join(chunks).encode("utf-8")).hexdigest()


def scene_frame_count(scene: dict, fps: int) -> int:
    """Round global timeline boundaries to avoid drift across many scenes."""
    return round(scene["end"] * fps) - round(scene["start"] * fps)


def render_project(args):
    project_file = Path(args.project).resolve()
    if not project_file.is_file():
        raise FileNotFoundError("Project JSON missing: " + str(project_file))
    project = json.loads(project_file.read_text(encoding="utf-8-sig"))
    audio = resolve(project_file.parent, str(project.get("audioPath") or ""))
    if not project.get("audioPath") or not audio.is_file():
        raise ValueError("Choose a valid TTS audio before rendering")
    audio_seconds = probe_seconds(audio)
    project, scenes = load_project(project_file, audio_seconds, args.scene_index)
    from video_tracks import validate_tracks, create_ass, track_signature
    music, music_volume, burn_subtitles, subtitle_cues = validate_tracks(
        project, project_file.parent, audio_seconds)
    width, height, fps = (640, 360, 20) if args.quality == "preview" else (1280, 720, 24)
    export_w, export_h = (640, 360) if args.quality == "preview" else (1920, 1080)
    output = Path(args.output).resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    workspace = output.parent / (output.stem + ".ai_video_work")
    workspace.mkdir(exist_ok=True)
    if args.validate_only:
        say(f"[VALID] {len(scenes)} scenes, {audio_seconds:.3f}s, "
            f"music={bool(music)}, subtitles={burn_subtitles}")
        return
    engine = Path(args.engine).resolve() if args.engine else Path("missing-whiteboard.exe")
    if args.scene_index:
        if args.scene_index < 1 or args.scene_index > len(scenes):
            raise ValueError(f"Requested scene {args.scene_index} does not exist")
        work_scenes = [scenes[args.scene_index - 1]]
    else:
        work_scenes = scenes
    clips = []
    fingerprints = []
    for scene in work_scenes:
        idx = scene["index"]
        frames = scene_frame_count(scene, fps)
        if frames < 1:
            raise ValueError(f"Scene {idx} has no video frames")
        folder = workspace / f"scene_{idx:03d}"
        folder.mkdir(exist_ok=True)
        clip, cache = folder / "clip.mp4", folder / "cache.txt"
        fingerprint = digest_scene(scene, width, height, fps, frames)
        fingerprints.append(fingerprint)
        if (clip.is_file() and cache.is_file() and
                cache.read_text(encoding="ascii") == fingerprint):
            say(f"[CACHE] {idx}/{len(scenes)} {scene['style']}")
        else:
            raw = folder / "raw.mp4"
            say(f"[SCENE] {idx}/{len(scenes)} {scene['style']}")
            if scene["style"] in ("whiteboard", "layer"):
                render_whiteboard(scene, raw, folder, width, height, fps,
                                  engine, project_file.parent)
            elif scene["style"] == "text":
                render_text(raw, scene["text"], width, height, fps, frames, scene["image"])
            else:
                render_motion(scene["image"], raw, width, height, fps, frames,
                              folder, scene["style"] == "image-story")
            fade = scene["index"] > 1 and scene["transition"] == "fade"
            if scene.get("directionEvents"):
                from director_effects import apply_direction
                directed = folder / "director_effects.mp4"
                apply_direction(raw, directed, scene["directionEvents"],
                                folder, width, height, fps, frames)
                normalize_clip(directed, clip, width, height, fps, frames, fade_in=fade)
            else:
                normalize_clip(raw, clip, width, height, fps, frames, fade_in=fade)
            cache.write_text(fingerprint, encoding="ascii")
        clips.append(clip)
    # Final-export cache: unchanged scenes and identical source audio do not
    # need to be concatenated and remuxed again.
    audio_checksum = digest_file(audio)
    export_signature = hashlib.sha256(json.dumps({
        "renderer": CURRENT_VERSION,
        "audio": audio_checksum, "clips": fingerprints,
        "quality": args.quality, "width": export_w, "height": export_h,
        "fps": fps, "sceneIndex": args.scene_index,
        "duration": audio_seconds,
        "tracks": track_signature(project, music, burn_subtitles, subtitle_cues),
    }, sort_keys=True).encode("utf-8")).hexdigest()
    export_stamp = workspace / "export.signature"
    manifest = output.with_suffix(".ai-video-manifest.json")
    if (output.is_file() and output.stat().st_size > 1000
            and manifest.is_file() and export_stamp.is_file()
            and export_stamp.read_text(encoding="ascii") == export_signature):
        say(f"[CACHE] Finished MP4 unchanged: {output}")
        return

    if args.scene_index:
        joined = clips[0]
        selected = work_scenes[0]
        audio_start = selected["start"]
        mux_seconds = selected["end"] - selected["start"]
        # Keep the original TTS audio; trim just the selected sentence range.
        audio_args = ["-ss", f"{audio_start:.6f}", "-t", f"{mux_seconds:.6f}",
                      "-i", str(audio)]
    else:
        playlist = workspace / "clips.ffconcat"
        playlist.write_text("ffconcat version 1.0\n" + "".join(
            "file '" + str(c).replace("\\", "/").replace("'", "'\\''") + "'\n"
            for c in clips), encoding="utf-8")
        joined = workspace / "joined.mp4"
        execute(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
                 "-f", "concat", "-safe", "0", "-i", str(playlist),
                 "-c:v", "copy", "-an", str(joined)], "Join all scenes")
        mux_seconds = audio_seconds
        audio_args = ["-i", str(audio)]
    filters = f"scale={export_w}:{export_h}:flags=lanczos,format=yuv420p"
    if burn_subtitles:
        first_second = work_scenes[0]["start"] if args.scene_index else 0.
        create_ass(workspace / "subtitles.ass", subtitle_cues,
                   first_second, first_second + mux_seconds, export_w, export_h)
        filters += ",ass=subtitles.ass"
    # Keep narration as the primary audio track; BGM is quiet and looped.
    audio_source_args = audio_args + (
        ["-stream_loop", "-1", "-i", str(music)] if music is not None else [])
    common = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
              "-i", str(joined)] + audio_source_args
    if music:
        mix = (f"[1:a]aresample=async=1,asetpts=PTS-STARTPTS[voice];"
               f"[2:a]volume={music_volume:.4f},"
               f"atrim=duration={mux_seconds:.5f},asetpts=PTS-STARTPTS[bed];"
               "[voice][bed]amix=inputs=2:duration=first:"
               "dropout_transition=0:normalize=0[mix]")
        common += ["-filter_complex", mix, "-map", "0:v:0", "-map", "[mix]"]
    else:
        common += ["-map", "0:v:0", "-map", "1:a:0"]
    # Never overwrite a valid previous output until the new MP4 is validated.
    pending = output.with_name(output.stem + ".rendering.mp4")
    tail = ["-c:a", "aac", "-b:a", "192k", "-t", f"{mux_seconds:.5f}",
            "-movflags", "+faststart", str(pending)]
    try:
        if args.quality == "youtube":
            try:
                execute(common + ["-vf", filters, "-c:v", "h264_nvenc",
                                  "-preset", "p4", "-cq", "23"] + tail,
                        "Export Full HD (NVENC)", cwd=workspace)
            except RuntimeError:
                say("[WARN] NVENC unavailable; falling back to libx264")
                execute(common + ["-vf", filters, "-c:v", "libx264",
                                  "-preset", "veryfast", "-crf", "23"] + tail,
                        "Export Full HD (CPU)", cwd=workspace)
        elif burn_subtitles:
            execute(common + ["-vf", filters, "-c:v", "libx264",
                              "-preset", "veryfast", "-crf", "23",
                              "-pix_fmt", "yuv420p"] + tail,
                    "Preview with burned-in captions", cwd=workspace)
        else:
            execute(common + ["-c:v", "copy"] + tail,
                    "Mux total TTS and optional background music", cwd=workspace)
        expected = mux_seconds
        actual = probe_seconds(pending)
        if abs(expected - actual) > max(0.3, 1 / fps + .05):
            raise RuntimeError(f"Export duration mismatch: expected {expected:.3f}s, "
                               f"got {actual:.3f}s")
        verify = subprocess.run([
            "ffprobe", "-v", "error", "-show_entries", "stream=codec_type",
            "-of", "csv=p=0", str(pending)
        ], capture_output=True, text=True, check=True)
        streams = verify.stdout.split()
        if "video" not in streams or "audio" not in streams:
            raise RuntimeError("Export must contain one video and one audio stream")
        pending.replace(output)
    finally:
        pending.unlink(missing_ok=True)
    manifest.write_text(json.dumps({
        "renderer": CURRENT_VERSION, "project": str(project_file),
        "output": str(output), "audioSeconds": audio_seconds,
        "width": export_w, "height": export_h, "fps": fps,
        "selectedScene": args.scene_index or None,
        "burnSubtitles": burn_subtitles,
        "backgroundMusic": str(music) if music else None,
        "musicVolume": music_volume if music else None,
        "scenes": [{"index": s["index"], "style": s["style"],
                    "start": s["start"], "end": s["end"],
                    "frames": scene_frame_count(s, fps),
                    "transition": s["transition"],
                    "directorEvents": len(s.get("directionEvents", []))} for s in work_scenes]
    }, ensure_ascii=False, indent=2), encoding="utf-8")
    export_stamp.write_text(export_signature, encoding="ascii")
    say(f"[DONE] {output} - {len(work_scenes)} scenes, {mux_seconds:.2f}s")


def main():
    parser = argparse.ArgumentParser(description="Local AI Video Studio multi-engine renderer")
    parser.add_argument("--project", required=True, help="Saved project.ai-video.json")
    parser.add_argument("--output", required=True, help="Output MP4")
    parser.add_argument("--quality", choices=["preview", "youtube"], default="preview")
    parser.add_argument("--engine", default="", help="Existing whiteboard.exe")
    parser.add_argument("--validate-only", action="store_true",
                        help="Validate scene timings and optional tracks without rendering")
    parser.add_argument("--scene-index", type=int, default=0,
                        help="Preview only one scene with the matching slice of total TTS")
    args = parser.parse_args()
    try:
        render_project(args)
        return 0
    except (OSError, ValueError, RuntimeError, KeyError,
            json.JSONDecodeError) as exc:
        print("[ERROR] " + str(exc), file=sys.stderr, flush=True)
        return 1


if __name__ == "__main__":
    sys.exit(main())

