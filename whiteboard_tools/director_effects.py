"""Compose time-aware camera zoom and caption overlays into a video clip."""
from __future__ import annotations

import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from ai_video_pipeline import execute, font_at, wrap_text

DIRECTOR_EFFECTS_VERSION = "director-visuals-v1"


def validate_events(events: list[dict], duration_ms: int) -> list[dict]:
    if not isinstance(events, list) or len(events) > 4:
        raise ValueError("Direction events must be a list (0-4 items)")
    zoom_count = 0
    for e in events:
        if not isinstance(e, dict) or e.get("kind") not in ("text", "zoom"):
            raise ValueError("Director effect must be 'text' or 'zoom'")
        start, length = e.get("startMs"), e.get("durationMs")
        if type(start) is not int or type(length) is not int or start < 0 or length <= 0:
            raise ValueError("Director effect time must use nonnegative integer milliseconds")
        if start + length > duration_ms:
            raise ValueError("Director effect exceeds scene duration")
        factor = e.get("zoom", 1.1)
        fx, fy = e.get("focusX", .5), e.get("focusY", .5)
        if not all(type(v) in (int, float) and math.isfinite(v) for v in (factor, fx, fy)):
            raise ValueError("Invalid Director zoom coordinates")
        if not 1 <= factor <= 1.22 or not 0 <= fx <= 1 or not 0 <= fy <= 1:
            raise ValueError("Director zoom/coordinates must be within safe range")
        if not isinstance(e.get("text", ""), str) or len(e.get("text", "")) > 90:
            raise ValueError("Director text must be at most 90 characters")
        if e["kind"] == "zoom":
            zoom_count += 1
    if zoom_count > 1:
        raise ValueError("One camera zoom per scene is supported in this version")
    return events


def create_text_asset(text: str, target: Path, width: int, height: int):
    canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas)
    font = font_at(max(17, width // 24))
    text = " ".join(text.split()).strip()
    if not text:
        text = "KEY POINT"
    lines = wrap_text(text, draw, font, int(width * .80))[:2]
    if not lines:
        return
    line_h = int(font.size * 1.3)
    pad = max(10, width // 35)
    x0, x1 = width // 12, width - width // 12
    y1 = height - max(14, height // 18)
    y0 = max(6, y1 - len(lines) * line_h - 2 * pad)
    draw.rounded_rectangle((x0, y0, x1, y1), radius=max(5, pad // 2),
                           fill=(251, 249, 244, 244), outline=(40, 52, 70, 245),
                           width=max(1, width // 500))
    for i, line in enumerate(lines):
        draw.text((x0 + pad, y0 + pad + i * line_h), line,
                  font=font, fill=(26, 34, 50, 255))
    canvas.save(target)


def apply_direction(raw: Path, output: Path, events: list[dict],
                    folder: Path, width: int, height: int, fps: int, frames: int):
    if not events:
        return raw
    for_event = sorted(events, key=lambda e: e["startMs"])
    cmd = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", str(raw)]
    text_events = [e for e in for_event if e["kind"] == "text"]
    for j, event in enumerate(text_events):
        asset = folder / f"director_caption_{j:02}.png"
        create_text_asset(event.get("text", ""), asset, width, height)
        cmd.extend(["-loop", "1", "-framerate", str(fps), "-i", str(asset)])
    zoom = next((e for e in for_event if e["kind"] == "zoom"), None)
    if zoom:
        first = max(0, round(zoom["startMs"] * fps / 1000))
        count = max(1, round(zoom["durationMs"] * fps / 1000))
        last = first + count
        amp = float(zoom["zoom"]) - 1
        center_x, center_y = float(zoom["focusX"]), float(zoom["focusY"])
        z = f"if(between(on,{first},{last}),1+{amp:.6f}*sin(PI*(on-{first})/{count}),1)"
        base = (f"[0:v]fps={fps},zoompan=z='{z}':"
                f"x='max(0,min(iw-iw/zoom,iw*{center_x:.4f}-iw/zoom/2))':"
                f"y='max(0,min(ih-ih/zoom,ih*{center_y:.4f}-ih/zoom/2))':"
                f"d=1:s={width}x{height}:fps={fps},format=yuv420p[s0]")
    else:
        base = f"[0:v]fps={fps},format=yuv420p[s0]"
    filters = [base]
    latest = "s0"
    for i, event in enumerate(text_events, 1):
        start = event["startMs"] / 1000
        end = (event["startMs"] + event["durationMs"]) / 1000
        nxt = f"s{i}"
        filters.append(
            f"[{latest}][{i}:v]overlay=0:0:shortest=1:"
            f"enable='between(t,{start:.3f},{end:.3f})':"
            f"format=auto,format=yuv420p[{nxt}]"
        )
        latest = nxt
    cmd.extend(["-filter_complex", ";".join(filters), "-map", f"[{latest}]",
                "-frames:v", str(frames), "-an", "-c:v", "libx264",
                "-preset", "veryfast", "-crf", "23", "-pix_fmt", "yuv420p",
                str(output)])
    execute(cmd, "AI Director effects (camera and text)")
    return output
