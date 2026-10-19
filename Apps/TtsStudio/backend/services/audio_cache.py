"""Opt-in, private WAV result cache.

Disabled by default: DVA_TTS_AUDIO_CACHE=1 explicitly enables.
Content fingerprint covers voice reference bytes, engine identity, model/runtime
revision, text, language and all settings. Never read/write user voice.json.
"""
import hashlib
import json
import os
import shutil
import uuid
import wave
from pathlib import Path


CACHE_REVISION = "audio-cache-v1-chatterbox017-vieneu383"


class AudioResultCache:
    def __init__(self, root: Path, enabled: bool | None = None) -> None:
        self.root = root
        self.enabled = (os.getenv("DVA_TTS_AUDIO_CACHE", "") == "1") if enabled is None else enabled

    @staticmethod
    def digest(text: str, voice_id: str, language: str, engine: str,
               settings, reference_file: Path) -> str:
        reference_hash = hashlib.sha256(reference_file.read_bytes()).hexdigest()
        params = settings.model_dump(mode="json") if hasattr(settings, "model_dump") else settings
        data = json.dumps({
            "version": CACHE_REVISION, "text": text, "voice_id": voice_id,
            "lang": language, "engine": engine, "settings": params,
            "reference_sha256": reference_hash
        }, sort_keys=True, ensure_ascii=False, separators=(",", ":"))
        return hashlib.sha256(data.encode("utf-8")).hexdigest()

    def restore(self, digest: str, destination: Path) -> bool:
        if not self.enabled:
            return False
        path = self.root / (digest + ".wav")
        if not path.is_file():
            return False
        try:
            # Reject stale or truncated cache entries.
            with wave.open(str(path), "rb") as audio:
                if audio.getnframes() < 240 or audio.getframerate() < 8000:
                    return False
            shutil.copyfile(path, destination)
            return True
        except (OSError, ValueError, EOFError, wave.Error):
            return False

    def remember(self, digest: str, source: Path) -> None:
        if not self.enabled:
            return
        self.root.mkdir(parents=True, exist_ok=True)
        target = self.root / (digest + ".wav")
        if target.exists():
            return
        staging = self.root / (digest + "." + uuid.uuid4().hex + ".tmp")
        try:
            shutil.copyfile(source, staging)
            os.replace(staging, target)
        finally:
            staging.unlink(missing_ok=True)
