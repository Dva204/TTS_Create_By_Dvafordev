from pydantic import BaseModel, Field


class TtsGenerationSettings(BaseModel):
    speed: float = 1.0
    cfg_weight: float = 0.5
    temperature: float = 0.8
    exaggeration: float = 0.5
    speaker_boost: bool = True
    seed: int | None = None


class GenerateRequest(BaseModel):
    text: str = Field(min_length=1, max_length=5000)
    voice_id: str
    language: str = Field(min_length=0, max_length=16)
    engine: str = "chatterbox_multilingual"
    settings: TtsGenerationSettings = Field(default_factory=TtsGenerationSettings)
