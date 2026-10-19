import asyncio
import json
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
    def __init__(self, root: Path, engines: dict[str, TtsEngine] | TtsEngine, voices: VoiceStore) -> None:
        if isinstance(engines, dict):
            self.engines = engines
            self.engine = next(iter(engines.values()))
        else:
            self.engines = {engines.name: engines}
            self.engine = engines
        self.root, self.voices = root, voices
        root.mkdir(parents=True, exist_ok=True)
        self.jobs: dict[str, dict[str, Any]] = {}
        self.tasks: dict[str, asyncio.Task] = {}
        # At most one heavyweight inference may run on the GPU/CPU model at a time.
        self.queue_lock = asyncio.Lock()
        self._restore_jobs()

    def _job_file(self, job_id: str) -> Path:
        return self.root / f"{job_id}.json"

    def _save_job(self, job: dict[str, Any]) -> None:
        # Never persist transient cancellation objects / tasks.
        record = {k: v for k, v in job.items() if k != "cancel_requested"}
        path = self._job_file(job["id"])
        temp_path = path.with_suffix(".json.tmp")
        temp_path.write_text(json.dumps(record, ensure_ascii=False), encoding="utf-8")
        temp_path.replace(path)

    def _set_status(self, job: dict[str, Any], status: str, error: str | None = None) -> None:
        job["status"] = status
        job["error"] = error
        self._save_job(job)

    def _restore_jobs(self) -> None:
        for file in self.root.glob("*.json"):
            try:
                record = json.loads(file.read_text(encoding="utf-8"))
                job_id = record.get("id")
                if not isinstance(job_id, str) or file.name != f"{job_id}.json":
                    continue
                if record.get("status") in ("queued", "running", "cancelling"):
                    record["status"] = "failed"
                    record["error"] = "Backend đã khởi động lại trước khi hoàn thành. Hãy thử lại."
                elif record.get("status") == "completed" and not (self.root / f"{job_id}.wav").is_file():
                    record["status"] = "failed"
                    record["error"] = "Không còn file âm thanh của job này."
                self.jobs[job_id] = record
                self._save_job(record)
            except (OSError, ValueError, TypeError):
                # A broken metadata file must not prevent the TTS service from starting.
                continue

    def create(self, text: str, voice_id: str, language: str, settings: TtsGenerationSettings, engine_name: str | None = None) -> dict:
        job_id = uuid.uuid4().hex
        job = {"id": job_id, "status": "queued", "error": None, "audio_url": None,
               "created_at": datetime.now(timezone.utc).isoformat()}
        self.jobs[job_id] = job
        self._save_job(job)
        task = asyncio.create_task(self._run(job_id, text, voice_id, language, settings, engine_name))
        self.tasks[job_id] = task
        task.add_done_callback(lambda _task: self.tasks.pop(job_id, None))
        return job

    async def _run(self, job_id: str, text: str, voice_id: str, language: str,
                   settings: TtsGenerationSettings, engine_name: str | None = None) -> None:
        job = self.jobs[job_id]
        output = self.root / f"{job_id}.wav"
        try:
            async with self.queue_lock:
                if job.get("cancel_requested"):
                    self._set_status(job, "cancelled", "Đã hủy trước khi bắt đầu.")
                    return
                self._set_status(job, "running")
                reference = self.voices.reference(voice_id)
                eng = self.engines.get(engine_name or "", self.engine)
                # Do not cancel this asyncio.to_thread: cancellation would release
                # queue_lock while its thread is still running model inference.
                await asyncio.to_thread(eng.generate, text, language, reference, output, settings)
                if job.get("cancel_requested"):
                    output.unlink(missing_ok=True)
                    self._set_status(job, "cancelled", "Đã hủy sau khi hoàn tất lượt suy luận.")
                    return

                filters = []
                if settings.speed != 1.0:
                    filters.append(f"atempo={settings.speed}")
                # Preserve VieNeu's accepted native WAV; no forced loudnorm.
                if settings.speaker_boost and eng.name != "vieneu_v3_turbo":
                    filters.append("loudnorm")
                ffmpeg = shutil.which("ffmpeg")
                if ffmpeg and (filters or eng.name != "vieneu_v3_turbo"):
                    temp_out = output.with_suffix(".tmp.wav")
                    args = [ffmpeg, "-y", "-i", str(output)]
                    if filters:
                        args.extend(["-af", ",".join(filters)])
                    sample_rate = "48000" if eng.name == "vieneu_v3_turbo" else "24000"
                    args.extend(["-ar", sample_rate, "-c:a", "pcm_s16le", str(temp_out)])

                    def process_audio():
                        subprocess.run(args, capture_output=True, check=True,
                                       creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
                        shutil.move(temp_out, output)

                    try:
                        await asyncio.to_thread(process_audio)
                    except subprocess.CalledProcessError as exc:
                        raise RuntimeError(
                            f"FFmpeg failed: {exc.stderr.decode('utf-8', errors='replace')}") from exc
                    finally:
                        temp_out.unlink(missing_ok=True)
                if job.get("cancel_requested"):
                    output.unlink(missing_ok=True)
                    self._set_status(job, "cancelled", "Đã hủy sau xử lý âm thanh.")
                    return
                if not output.is_file() or output.stat().st_size < 44:
                    raise RuntimeError("Engine chưa tạo được WAV hợp lệ.")
                job["audio_url"] = f"/api/jobs/{job_id}/audio"
                self._set_status(job, "completed")
        except asyncio.CancelledError:
            output.unlink(missing_ok=True)
            self._set_status(job, "cancelled", "Đã hủy job đang chờ.")
            raise
        except Exception as exc:
            output.unlink(missing_ok=True)
            self._set_status(job, "failed", str(exc))

    def cancel(self, job_id: str) -> bool:
        job = self.jobs.get(job_id)
        if job is None:
            return False
        status = job.get("status")
        if status in ("completed", "failed", "cancelled"):
            # Existing UI uses DELETE after downloading the completed result.
            self.jobs.pop(job_id, None)
            (self.root / f"{job_id}.wav").unlink(missing_ok=True)
            self._job_file(job_id).unlink(missing_ok=True)
            return True

        job["cancel_requested"] = True
        if status == "queued":
            self._set_status(job, "cancelled", "Đã hủy trước khi bắt đầu.")
            task = self.tasks.get(job_id)
            if task is not None:
                task.cancel()
        elif status == "running":
            # Cooperative cancellation. The model's blocking thread must finish
            # before queue_lock is released; expose progress as "cancelling".
            self._set_status(job, "cancelling", "Đang đợi lượt suy luận kết thúc an toàn.")
        return True

    def audio(self, job_id: str) -> Path:
        job = self.jobs.get(job_id)
        path = self.root / f"{job_id}.wav"
        if not job or job["status"] != "completed" or not path.is_file():
            raise FileNotFoundError("Completed audio is not available.")
        return path
