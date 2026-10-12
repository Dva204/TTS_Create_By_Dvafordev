#!/usr/bin/env python3
"""Rule-based AI Video Director V1: deterministic editable effect suggestions.

No external LLM, image recognition or network requests. This is a "smart rules"
baseline, not a semantic understanding model.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

from whiteboard_batch import parse_srt

DIRECTOR_VERSION = "rules-v1"
NUMBER = re.compile(r"\b(?:1[0-9]{3}|20[0-9]{2}|[0-9]{1,3}(?:[,.][0-9]{3})+|[0-9]+(?:\.[0-9]+)?%)\b")
STRONG = re.compile(r"\b(first|never|secret|hidden|discovered|breakthrough|impossible|critical|changed everything|finally|the reason|astonishing)\b", re.I)
ZOOM_CUE = re.compile(r"\b(look|closer|discover|secret|hidden|first|revealed|suddenly|inside|tiny|important|changed|mysterious)\b", re.I)


def clamp(value: int, lo: int, hi: int) -> int:
    return max(lo, min(value, hi))


def suggest_scene(scene: dict, cues: list[tuple[float, float, str]]) -> list[dict]:
    start = scene["startMs"]
    duration = scene["endMs"] - start
    if duration < 1100 or scene.get("styleId") == "text":
        return []
    cue_lo, cue_hi = scene["cueFrom"], scene["cueTo"]
    candidates = cues[cue_lo - 1:cue_hi]
    if not candidates:
        return []
    chosen = None
    for cue_start, _, text in candidates:
        match = NUMBER.search(text)
        if match:
            chosen = (match.group(0), cue_start, "number")
            break
    if chosen is None:
        for cue_start, _, text in candidates:
            match = STRONG.search(text)
            if match:
                chosen = (match.group(0), cue_start, "emphasis")
                break
    result = []
    if chosen:
        word, when, _ = chosen
        # Timing is relative to actual cue in SRT rather than placing
        # every overlay at the start of the scene.
        trigger = int(round(when * 1000)) - start
        begin = clamp(trigger + 160, 0, max(0, duration - 900))
        display = min(1800, duration - begin)
        if display >= 900:
            result.append({
                "kind": "text", "startMs": begin,
                "durationMs": display, "text": word.upper(),
                "zoom": 1.1, "focusX": .5, "focusY": .5
            })
    # Camera gestures are suggestions only: centered crop, not object detection.
    # Do not double-zoom the image-motion engine.
    style = scene.get("styleId", "")
    spoken = " ".join(text for _, _, text in candidates)
    if style in {"whiteboard", "layer", "image-story"} and ZOOM_CUE.search(spoken) and duration >= 2500:
        zoom_start = clamp(int(duration * .35), 0, duration - 1500)
        zoom_length = min(2500, duration - zoom_start)
        # Avoid covering the same instant with text + camera if possible.
        if result and zoom_start < result[0]["startMs"] + result[0]["durationMs"]:
            zoom_start = clamp(result[0]["startMs"] + result[0]["durationMs"] + 150,
                               0, duration - 1500)
            zoom_length = min(2200, duration - zoom_start)
        if zoom_length >= 1400:
            result.append({
                "kind": "zoom", "startMs": zoom_start,
                "durationMs": zoom_length, "text": "",
                "zoom": 1.10, "focusX": .5, "focusY": .5
            })
    return result[:2]


def suggest_project(project: dict, cues: list[tuple[float, float, str]],
                    replace_existing: bool = False) -> tuple[dict, int]:
    if project.get("schemaVersion") != 1:
        raise ValueError("Expected schemaVersion=1")
    scenes = project.get("scenes")
    if not isinstance(scenes, list) or not scenes:
        raise ValueError("No scenes in project")
    prepared = json.loads(json.dumps(project))
    proposed = 0
    for s in prepared["scenes"]:
        if s.get("directionEvents") and not replace_existing:
            continue
        a, b = s["cueFrom"], s["cueTo"]
        if type(a) is not int or type(b) is not int or not 1 <= a <= b <= len(cues):
            raise ValueError(f"Scene {s.get('index')}: invalid SRT cue mapping")
        suggestions = suggest_scene(s, cues)
        s["directionEvents"] = suggestions
        proposed += len(suggestions)
    return prepared, proposed


def main() -> int:
    parser = argparse.ArgumentParser(description="Suggest automatic captions and zoom cues using local rules")
    parser.add_argument("--project", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--replace-existing", action="store_true")
    args = parser.parse_args()
    src = Path(args.project).resolve()
    dst = Path(args.output).resolve()
    if not src.is_file():
        raise FileNotFoundError(src)
    if src == dst:
        raise ValueError("Refusing to overwrite source project; use a separate output")
    if dst.exists():
        raise FileExistsError(f"Suggestion file already exists: {dst}")
    project = json.loads(src.read_text(encoding="utf-8-sig"))
    srt_path = Path(project.get("subtitlePath") or "")
    if not str(srt_path):
        raise ValueError("Project has no subtitlePath")
    srt = (srt_path if srt_path.is_absolute() else src.parent / srt_path).resolve()
    cues = parse_srt(srt)
    planned, count = suggest_project(project, cues, args.replace_existing)
    dst.parent.mkdir(parents=True, exist_ok=True)
    dst.write_text(json.dumps(planned, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"[DIRECTOR] Suggested {count} effects across {len(planned['scenes'])} scenes", flush=True)
    print(f"[REVIEW] {dst}", flush=True)
    print("[INFO] Rules-only suggestions; zoom is centered, not object-aware.", flush=True)
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, KeyError, json.JSONDecodeError) as ex:
        print(f"[ERROR] {ex}", file=sys.stderr)
        sys.exit(1)
