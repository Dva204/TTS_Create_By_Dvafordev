"""Video Studio V2 composition: SRT/ASS subtitles and optional background music.

All captions come from user-provided SRT; no model/API is needed.
"""
from __future__ import annotations

import hashlib
import json
import re
from pathlib import Path

from whiteboard_batch import parse_srt


def validate_tracks(project: dict, project_root: Path, audio_seconds: float):
    volume = project.get("musicVolume", 0.12)
    if type(volume) not in (float, int) or not (0 <= volume <= 0.5):
        raise ValueError("Music volume must be 0.00 to 0.50")
    burn = project.get("burnSubtitles", False)
    if type(burn) is not bool:
        raise ValueError("burnSubtitles must be true/false")
    music_name = project.get("musicPath") or ""
    music = (project_root / music_name).resolve() if music_name else None
    if music is not None and not music.is_file():
        raise ValueError("Background music file missing: " + music_name)
    if burn:
        subtitle_name = project.get("subtitlePath") or ""
        if not subtitle_name:
            raise ValueError("To burn subtitles, select an SRT file.")
        srt = (project_root / subtitle_name).resolve()
        if not srt.is_file():
            raise ValueError("Subtitle SRT file missing: " + subtitle_name)
        cues = parse_srt(srt)
        if any(start < 0 or end > audio_seconds + .4 for start, end, _ in cues):
            raise ValueError("Subtitle timing is outside TTS audio duration")
        return music, float(volume), True, cues
    return music, float(volume), False, []


def ass_time(seconds: float) -> str:
    centiseconds = round(max(0.0, seconds) * 100)
    return f"{centiseconds // 360000:01d}:{centiseconds // 6000 % 60:02d}:{centiseconds // 100 % 60:02d}.{centiseconds % 100:02d}"


def ass_escape(text: str) -> str:
    clean = re.sub(r"<[^>]{1,120}>", "", text)
    # The libass override syntax uses braces and backslashes.
    clean = clean.replace("\\", "＼").replace("{", "｛").replace("}", "｝")
    return " ".join(clean.split())[:200]


def create_ass(path: Path, cues: list, start: float, end: float,
               width: int, height: int) -> int:
    """Write burnable timed subtitles for a full project or selected scene."""
    if start >= end:
        raise ValueError("Invalid subtitle segment duration")
    header = (
        "[Script Info]\nScriptType: v4.00+\nCollisions: Normal\n"
        f"PlayResX: {width}\nPlayResY: {height}\n"
        "WrapStyle: 0\nScaledBorderAndShadow: yes\n"
        "\n[V4+ Styles]\n"
        "Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour,"
        " BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing,"
        " Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding\n"
        f"Style: Default,Arial,{max(20, width // 28)},&H00FFFFFF,&H00FFFFFF,"
        "&H00090909,&H6F000000,0,0,0,0,100,100,0,0,1,2,1,2,55,55,"
        f"{max(22, height // 15)},1\n"
        "\n[Events]\nFormat: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text\n"
    )
    lines = []
    for cue_start, cue_end, words in cues:
        a, b = max(cue_start, start), min(cue_end, end)
        if b <= a:
            continue
        content = ass_escape(words)
        if not content:
            continue
        lines.append(f"Dialogue: 0,{ass_time(a-start)},{ass_time(b-start)},Default,,0,0,0,,{content}")
    path.write_text(header + "\n".join(lines) + "\n", encoding="utf-8")
    return len(lines)


def track_signature(project: dict, music: Path | None, burn: bool, cues: list) -> str:
    """Export-cache key when subtitles or music change."""
    payload = {
        "burn": burn,
        "volume": project.get("musicVolume", .12),
        "music": str(music) if music else "",
        "musicBytes": "", "subtitleCues": cues if burn else [],
    }
    if music:
        digest = hashlib.sha256()
        with music.open("rb") as f:
            for chunk in iter(lambda: f.read(1024 * 1024), b""):
                digest.update(chunk)
        payload["musicBytes"] = digest.hexdigest()
    return hashlib.sha256(json.dumps(payload, ensure_ascii=False, sort_keys=True).encode()).hexdigest()
