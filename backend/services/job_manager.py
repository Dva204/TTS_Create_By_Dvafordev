import asyncio
import uuid
import subprocess
import shutil
import tempfile
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from engines.base import TtsEngine
from services.voice_store import VoiceStore
from models.tts import TtsGenerationSettings


class JobManager:
    def __init__(self, root: Path, engine: TtsEngine | dict[str, TtsEngine], voices: VoiceStore) -> None:
        self.engines = engine if isinstance(engine, dict) else {engine.name: engine}
        self.root, self.engine, self.voices = root, next(iter(self.engines.values())), voices
        root.mkdir(parents=True, exist_ok=True)
        self.jobs: dict[str, dict[str, Any]] = {}
        self.tasks: dict[str, asyncio.Task] = {}
        self.queue_lock = asyncio.Lock()

    def create(self, text: str, voice_id: str, language: str, settings: TtsGenerationSettings,
               engine_name: str | None = None) -> dict:
        job_id = uuid.uuid4().hex
        job = {"id": job_id, "status": "queued", "error": None, "audio_url": None, "created_at": datetime.now(timezone.utc).isoformat()}
        self.jobs[job_id] = job
        self.tasks[job_id] = asyncio.create_task(
            self._run(job_id, text, voice_id, language, settings, engine_name)
        )
        return job

    async def _run(self, job_id: str, text: str, voice_id: str, language: str,
                   settings: TtsGenerationSettings, engine_name: str | None = None) -> None:
        job = self.jobs[job_id]
        try:
            async with self.queue_lock:
                job["status"] = "running"
                output = self.root / f"{job_id}.wav"
                reference = self.voices.reference(voice_id)
                selected = self.engines.get(engine_name or "", self.engine)
                await asyncio.to_thread(selected.generate, text, language, reference, output, settings)
                
                # Post-processing (Speed and Speaker Boost)
                filters = []
                if settings.speed != 1.0:
                    filters.append(f"atempo={settings.speed}")
                if settings.speaker_boost:
                    filters.append("loudnorm")
                
                ffmpeg = shutil.which("ffmpeg")
                if ffmpeg:
                    temp_out = output.with_suffix(".tmp.wav")
                    args = [ffmpeg, "-y", "-i", str(output)]
                    if filters:
                        args.extend(["-af", ",".join(filters)])
                    out_rate = "48000" if selected.name == "vieneu_v3_turbo" else "24000"
                    args.extend(["-ar", out_rate, "-c:a", "pcm_s16le", str(temp_out)])
                    
                    def _process():
                        subprocess.run(
                            args, capture_output=True, check=True, creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0)
                        )
                        shutil.move(temp_out, output)
                    
                    try:
                        await asyncio.to_thread(_process)
                    except subprocess.CalledProcessError as e:
                        raise RuntimeError(f"FFmpeg failed: {e.stderr.decode('utf-8', errors='replace')}") from e

                job["status"], job["audio_url"] = "completed", f"/api/jobs/{job_id}/audio"
        except asyncio.CancelledError:
            job["status"], job["error"] = "cancelled", "Generation was cancelled. GPU inference may finish before resources are released."
        except Exception as exc:
            job["status"], job["error"] = "failed", str(exc)

    def cancel(self, job_id: str) -> bool:
        task = self.tasks.get(job_id)
        if task and not task.done():
            task.cancel()
        
        self.jobs.pop(job_id, None)
        self.tasks.pop(job_id, None)
        path = self.root / f"{job_id}.wav"
        if path.is_file():
            try: path.unlink()
            except OSError: pass
            
        return True

    def audio(self, job_id: str) -> Path:
        job = self.jobs.get(job_id)
        path = self.root / f"{job_id}.wav"
        if not job or job["status"] != "completed" or not path.is_file():
            raise FileNotFoundError("Completed audio is not available.")
        return path
