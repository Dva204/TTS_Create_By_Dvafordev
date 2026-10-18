from abc import ABC, abstractmethod
from pathlib import Path

from models.tts import TtsGenerationSettings


class TtsEngine(ABC):
    name: str

    @property
    @abstractmethod
    def available(self) -> tuple[bool, str | None]: ...

    @abstractmethod
    def generate(self, text: str, language: str, reference: Path, output: Path, settings: TtsGenerationSettings) -> None: ...
