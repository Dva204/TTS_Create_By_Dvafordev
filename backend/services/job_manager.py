import asyncio
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from engines.base import TtsEngine
from services.voice_store import VoiceStore


class JobManager:
    def __init__(self, root: Path, engine: TtsEngine, voices: VoiceStore) -> None:
        self.root, self.engine, self.voices = root, engine, voices
        root.mkdir(parents=True, exist_ok=True)
        self.jobs: dict[str, dict[str, Any]] = {}
        self.tasks: dict[str, asyncio.Task] = {}
        self.queue_lock = asyncio.Lock()

    def create(self, text: str, voice_id: str, language: str) -> dict:
        job_id = uuid.uuid4().hex
        job = {"id": job_id, "status": "queued", "error": None, "audio_url": None, "created_at": datetime.now(timezone.utc).isoformat()}
        self.jobs[job_id] = job
        self.tasks[job_id] = asyncio.create_task(self._run(job_id, text, voice_id, language))
        return job

    async def _run(self, job_id: str, text: str, voice_id: str, language: str) -> None:
        job = self.jobs[job_id]
        try:
            async with self.queue_lock:
                job["status"] = "running"
                output = self.root / f"{job_id}.wav"
                reference = self.voices.reference(voice_id)
                await asyncio.to_thread(self.engine.generate, text, language, reference, output)
                job["status"], job["audio_url"] = "completed", f"/api/jobs/{job_id}/audio"
        except asyncio.CancelledError:
            job["status"], job["error"] = "cancelled", "Generation was cancelled. GPU inference may finish before resources are released."
        except Exception as exc:
            job["status"], job["error"] = "failed", str(exc)

    def cancel(self, job_id: str) -> bool:
        task = self.tasks.get(job_id)
        if not task or task.done():
            return False
        task.cancel()
        return True

    def audio(self, job_id: str) -> Path:
        job = self.jobs.get(job_id)
        path = self.root / f"{job_id}.wav"
        if not job or job["status"] != "completed" or not path.is_file():
            raise FileNotFoundError("Completed audio is not available.")
        return path
