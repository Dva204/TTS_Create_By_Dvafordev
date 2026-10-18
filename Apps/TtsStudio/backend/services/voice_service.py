import json
import shutil
import uuid
from datetime import datetime, timezone
from pathlib import Path
from .audio_service import normalize_reference


class VoiceService:
    def __init__(self, root: Path) -> None:
        self.root = root
        self.root.mkdir(parents=True, exist_ok=True)

    def list(self) -> list[dict]:
        voices = []
        for metadata in self.root.glob("*/voice.json"):
            try:
                voices.append(json.loads(metadata.read_text(encoding="utf-8")))
            except (OSError, json.JSONDecodeError):
                continue
        return sorted(voices, key=lambda item: item.get("created_at", ""))

    def get(self, voice_id: str) -> dict | None:
        path = self.root / voice_id / "voice.json"
        try:
            return json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None

    def add(self, name: str, language: str | None, filename: str, content: bytes) -> dict:
        voice_id = uuid.uuid4().hex
        folder = self.root / voice_id
        folder.mkdir(parents=True)
        source = folder / ("source" + Path(filename).suffix.lower())
        source.write_bytes(content)
        reference = folder / "reference.wav"
        normalized = normalize_reference(source, reference)
        if not normalized:
            reference = source
        metadata = {"id": voice_id, "name": name, "language": language, "created_at": datetime.now(timezone.utc).isoformat(), "reference_file": reference.name}
        (folder / "voice.json").write_text(json.dumps(metadata, indent=2), encoding="utf-8")
        return metadata

    def delete(self, voice_id: str) -> bool:
        folder = self.root / voice_id
        if not folder.is_dir():
            return False
        shutil.rmtree(folder)
        return True
