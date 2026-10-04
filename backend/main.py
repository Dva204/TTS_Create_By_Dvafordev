import platform
import shutil
import sys
import tempfile
from pathlib import Path

from fastapi import FastAPI, File, Form, HTTPException, UploadFile
from fastapi.responses import FileResponse
from pydantic import BaseModel, Field

from engines.chatterbox import ChatterboxMultilingualEngine
from services.job_manager import JobManager
from services.voice_store import VoiceStore

BASE = Path(__file__).resolve().parent
DATA = BASE / "data"
voices = VoiceStore(DATA / "voices")
engine = ChatterboxMultilingualEngine()
jobs = JobManager(DATA / "jobs", engine, voices)
app = FastAPI(title="Local AI TTS Studio", version="1.0.0")


class GenerateRequest(BaseModel):
    text: str = Field(min_length=1, max_length=5000)
    voice_id: str
    language: str = Field(min_length=2, max_length=16)
    engine: str = "chatterbox_multilingual"


@app.get("/api/health")
def health():
    ready, detail = engine.available
    return {"status": "ok", "engine": engine.name, "ready": ready, "detail": detail}


@app.get("/api/system")
def system_info():
    cuda, gpu = False, None
    try:
        import torch
        cuda = torch.cuda.is_available()
        gpu = torch.cuda.get_device_name(0) if cuda else None
    except Exception:
        pass
    return {"platform": platform.platform(), "python": sys.version.split()[0], "cuda_available": cuda, "gpu_name": gpu,
            "ffmpeg": shutil.which("ffmpeg") or "not found", "engines": [engine.name]}


@app.get("/api/voices")
def list_voices():
    return voices.list()


@app.post("/api/voices", status_code=201)
async def add_voice(name: str = Form(...), language: str | None = Form(None), file: UploadFile = File(...)):
    suffix = Path(file.filename or "").suffix.lower()
    if suffix not in VoiceStore.allowed:
        raise HTTPException(400, "Supported reference formats: WAV, MP3, M4A, and FLAC.")
    if not name.strip():
        raise HTTPException(400, "Voice name is required.")
    temp_path: Path | None = None
    try:
        with tempfile.NamedTemporaryFile(delete=False, suffix=suffix) as temp:
            temp_path = Path(temp.name)
            while chunk := await file.read(1024 * 1024):
                temp.write(chunk)
        return voices.add(name.strip(), language, temp_path)
    except (ValueError, RuntimeError) as exc:
        raise HTTPException(400, str(exc)) from exc
    finally:
        await file.close()
        if temp_path:
            temp_path.unlink(missing_ok=True)


@app.delete("/api/voices/{voice_id}", status_code=204)
def delete_voice(voice_id: str):
    try:
        if not voices.delete(voice_id):
            raise HTTPException(404, "Voice not found.")
    except ValueError as exc:
        raise HTTPException(400, str(exc)) from exc


@app.post("/api/tts/generate", status_code=202)
async def generate(request: GenerateRequest):
    if request.engine != engine.name:
        raise HTTPException(400, f"Unknown engine: {request.engine}")
    ready, detail = engine.available
    if not ready:
        raise HTTPException(503, detail)
    try:
        voices.reference(request.voice_id)
    except (ValueError, FileNotFoundError) as exc:
        raise HTTPException(404, str(exc)) from exc
    return jobs.create(request.text.strip(), request.voice_id, request.language)


@app.get("/api/jobs/{job_id}")
def get_job(job_id: str):
    job = jobs.jobs.get(job_id)
    if not job:
        raise HTTPException(404, "Job not found.")
    return job


@app.delete("/api/jobs/{job_id}", status_code=204)
def cancel_job(job_id: str):
    if job_id not in jobs.jobs:
        raise HTTPException(404, "Job not found.")
    jobs.cancel(job_id)


@app.get("/api/jobs/{job_id}/audio")
def job_audio(job_id: str):
    try:
        return FileResponse(jobs.audio(job_id), media_type="audio/wav", filename=f"tts-{job_id}.wav")
    except FileNotFoundError as exc:
        raise HTTPException(404, str(exc)) from exc
