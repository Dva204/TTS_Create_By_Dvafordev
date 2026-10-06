import json
import shutil
import subprocess
import uuid
from datetime import datetime, timezone
from pathlib import Path


class VoiceStore:
    allowed = {".wav", ".mp3", ".m4a", ".flac"}

    def __init__(self, root: Path) -> None:
        self.root = root
        root.mkdir(parents=True, exist_ok=True)

    def list(self, q: str | None = None) -> list[dict]:
        voices = []
        for metadata in self.root.glob("*/voice.json"):
            try:
                voices.append(json.loads(metadata.read_text(encoding="utf-8")))
            except (OSError, json.JSONDecodeError):
                continue
        if q:
            q = q.casefold()
            voices = [v for v in voices if q in v.get("name", "").casefold() or q in (v.get("language") or "").casefold() or q in (v.get("description") or "").casefold() or q in (v.get("gender") or "").casefold() or q in v.get("id", "").casefold()]
        return sorted(voices, key=lambda x: x.get("name", "").casefold())

    def get(self, voice_id: str) -> dict | None:
        path = self._safe_dir(voice_id) / "voice.json"
        if not path.is_file():
            return None
        return json.loads(path.read_text(encoding="utf-8"))

    def add(self, name: str, language: str | None, source: Path, gender: str | None = None, description: str | None = None, voice_id: str | None = None) -> dict:
        if source.suffix.lower() not in self.allowed:
            raise ValueError("Supported reference formats: WAV, MP3, M4A, and FLAC.")
        
        if not voice_id:
            voice_id = uuid.uuid4().hex
        else:
            voice_id = "".join(c if c.isalnum() or c in "-_" else "_" for c in voice_id) # sanitize

        folder = self.root / voice_id
        if folder.exists() and (folder / "voice.json").exists():
            raise ValueError(f"Voice ID '{voice_id}' already exists.")
            
        folder.mkdir(exist_ok=True)
        destination = folder / "reference.wav"
        try:
            ffmpeg = shutil.which("ffmpeg")
            if ffmpeg:
                result = subprocess.run(
                    [ffmpeg, "-y", "-i", str(source), "-ac", "1", "-ar", "24000", "-c:a", "pcm_s16le", str(destination)],
                    capture_output=True, text=True, timeout=180, creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0)
                )
                if result.returncode != 0:
                    raise RuntimeError("ffmpeg could not normalize this audio: " + result.stderr[-500:])
            elif source.suffix.lower() == ".wav":
                shutil.copy2(source, destination)
            else:
                raise RuntimeError("ffmpeg is required to import MP3, M4A, or FLAC. Install it and add it to PATH.")
            record = {"id": voice_id, "name": name, "language": language, "gender": gender, "description": description, "created_at": datetime.now(timezone.utc).isoformat(), "reference_file": "reference.wav"}
            (folder / "voice.json").write_text(json.dumps(record, indent=2, ensure_ascii=False), encoding="utf-8")
            return record
        except Exception:
            shutil.rmtree(folder, ignore_errors=True)
            raise

    def replace_reference(self, voice_id: str, source: Path) -> dict:
        if source.suffix.lower() not in self.allowed:
            raise ValueError("Supported reference formats: WAV, MP3, M4A, and FLAC.")
        folder = self._safe_dir(voice_id)
        if not folder.is_dir():
            raise FileNotFoundError("Voice not found.")
        destination = folder / "reference.wav"
        
        ffmpeg = shutil.which("ffmpeg")
        if ffmpeg:
            result = subprocess.run(
                [ffmpeg, "-y", "-i", str(source), "-ac", "1", "-ar", "24000", "-c:a", "pcm_s16le", str(destination)],
                capture_output=True, text=True, timeout=180, creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0)
            )
            if result.returncode != 0:
                raise RuntimeError("ffmpeg could not normalize this audio: " + result.stderr[-500:])
        elif source.suffix.lower() == ".wav":
            shutil.copy2(source, destination)
        else:
            raise RuntimeError("ffmpeg is required to import MP3, M4A, or FLAC. Install it and add it to PATH.")
            
        record_path = folder / "voice.json"
        try:
            record = json.loads(record_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            record = {"id": voice_id}
        record["updated_at"] = datetime.now(timezone.utc).isoformat()
        record_path.write_text(json.dumps(record, indent=2, ensure_ascii=False), encoding="utf-8")
        return record

    def delete(self, voice_id: str) -> bool:
        folder = self._safe_dir(voice_id)
        if not folder.is_dir():
            return False
        shutil.rmtree(folder)
        return True

    def reference(self, voice_id: str) -> Path:
        result = self._safe_dir(voice_id) / "reference.wav"
        if not result.is_file():
            raise FileNotFoundError("Reference voice does not exist.")
        return result

    def _safe_dir(self, voice_id: str) -> Path:
        if not voice_id or not all(c.isalnum() or c in "-_" for c in voice_id):
            raise ValueError("Invalid voice id.")
        path = (self.root / voice_id).resolve()
        if self.root.resolve() not in path.parents:
            raise ValueError("Invalid voice id.")
        return path
