"""Lazy adapter for the real local Resemble AI Chatterbox Multilingual model."""
from pathlib import Path
from threading import Lock
from typing import Any
from .base import TtsEngine
from models.tts import TtsGenerationSettings


class ChatterboxMultilingualEngine(TtsEngine):
    name = "chatterbox_multilingual"

    def __init__(self) -> None:
        self._model: Any = None
        self._lock = Lock()
        try:
            import torch
            import torchaudio
            from chatterbox.mtl_tts import ChatterboxMultilingualTTS
            self._torch, self._torchaudio, self._model_type = torch, torchaudio, ChatterboxMultilingualTTS
            self._import_error: str | None = None
        except Exception as exc:
            self._import_error = str(exc)

    @property
    def available(self) -> tuple[bool, str | None]:
        if self._import_error:
            return False, ("Chatterbox is unavailable. Activate backend/.venv, install a CUDA-compatible PyTorch build, "
                           "then run 'pip install chatterbox-tts==0.1.7'. Import error: " + self._import_error)
        return True, None

    def generate(self, text: str, language: str, reference: Path, output: Path, settings: TtsGenerationSettings) -> None:
        ready, detail = self.available
        if not ready:
            raise RuntimeError(detail)
        with self._lock:  # keep only one model/inference call in VRAM
            if self._model is None:
                device = "cuda" if self._torch.cuda.is_available() else "cpu"
                try:
                    self._model = self._model_type.from_pretrained(device=device, t3_model="v3")
                except TypeError:
                    self._model = self._model_type.from_pretrained(device=device)
            if settings.seed is not None:
                self._torch.manual_seed(settings.seed)
            wav = self._model.generate(
                text, 
                language_id=language, 
                audio_prompt_path=str(reference),
                exaggeration=settings.exaggeration,
                cfg_weight=settings.cfg_weight,
                temperature=settings.temperature
            )
            output.parent.mkdir(parents=True, exist_ok=True)
            self._torchaudio.save(str(output), wav.detach().cpu(), self._model.sr)
