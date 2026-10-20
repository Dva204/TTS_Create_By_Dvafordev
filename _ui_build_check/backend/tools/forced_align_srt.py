"""Optional actual WhisperX forced-alignment of an existing SRT and audio file.

Install whisperx into backend/.venv-align; this module does not import or
download alignment dependencies when unused. Only explicit UI invocation runs.
Writes a *new* SRT, never the source captions.
"""
import argparse
import re
from pathlib import Path


TIMESTAMP = re.compile(r"(\d{2}):(\d{2}):(\d{2})[,.](\d{3})")


def stamp_to_seconds(stamp):
    match = TIMESTAMP.fullmatch(stamp)
    if not match:
        raise ValueError(f"Invalid timestamp: {stamp}")
    h, m, s, ms = map(int, match.groups())
    return 3600*h + 60*m + s + ms / 1000


def seconds_to_stamp(value):
    value = max(0, int(round(value * 1000)))
    h, value = divmod(value, 3600000)
    minute, value = divmod(value, 60000)
    sec, ms = divmod(value, 1000)
    return f"{h:02}:{minute:02}:{sec:02},{ms:03}"


def read_srt(path):
    text = Path(path).read_text(encoding="utf-8-sig").replace("\r\n","\n")
    result = []
    for block in re.split(r"\n\s*\n+", text.strip()):
        lines = block.split("\n")
        pos = next((i for i,line in enumerate(lines) if "-->" in line), None)
        if pos is None or pos == len(lines) - 1:
            raise ValueError("Invalid SRT cue format")
        t1,t2=[t.strip() for t in lines[pos].split("-->", 1)]
        result.append({"start":stamp_to_seconds(t1), "end":stamp_to_seconds(t2),
                       "text":" ".join(lines[pos+1:]).strip()})
    if not result: raise ValueError("Empty SRT")
    return result


def align_srt(audio, srt, language, output):
    try:
        import whisperx
    except ImportError as exc:
        raise RuntimeError("Missing whisperx. Install only in separate .venv-align.") from exc
    segments = read_srt(srt)
    if not all(seg["text"] for seg in segments):
        raise ValueError("SRT has an empty cue")
    audio_samples = whisperx.load_audio(str(audio))
    # The forced-alignment model is distinct from an ASR/transcription model.
    # The first explicit run may download a language-specific model.
    model, metadata = whisperx.load_align_model(language_code=language, device="cpu")
    result = whisperx.align(segments, model, metadata, audio_samples, "cpu",
                            return_char_alignments=False)
    aligned = result.get("segments", [])
    if len(aligned) != len(segments):
        raise RuntimeError("WhisperX returned incomplete alignment; no file was written")
    parts, matched = [], 0
    previous = 0.0
    for index, (original, new) in enumerate(zip(segments, aligned), start=1):
        words=[w for w in new.get("words", []) if "start" in w and "end" in w]
        if words:
            start=min(float(w["start"]) for w in words)
            end=max(float(w["end"]) for w in words)
            matched += 1
        else:
            # Do not silently call an unaligned span "aligned".
            raise RuntimeError(f"Cue {index} could not be aligned; output not saved")
        if end <= start or start < previous - 0.05:
            raise RuntimeError(f"Cue {index} has invalid/overlapping alignment")
        previous=end
        parts.append(f"{index}\n{seconds_to_stamp(start)} --> {seconds_to_stamp(end)}\n"
                     f"{original['text']}\n")
    path=Path(output)
    if path.exists():raise FileExistsError("Aligned SRT output already exists")
    path.write_text("\n".join(parts)+"\n",encoding="utf-8")
    return matched


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--audio", type=Path,required=True)
    parser.add_argument("--srt",type=Path,required=True)
    parser.add_argument("--language",required=True)
    parser.add_argument("--output",type=Path,required=True)
    args=parser.parse_args()
    count=align_srt(args.audio,args.srt,args.language,args.output)
    print(f"WhisperX force-aligned {count} cues to {args.output}",flush=True)


if __name__=="__main__":
    main()
