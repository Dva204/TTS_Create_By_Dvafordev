import platform
import shutil
import sys
import tempfile
from pathlib import Path

from fastapi import FastAPI, File, Form, HTTPException, UploadFile
from fastapi.responses import FileResponse
from pydantic import BaseModel, Field

from engines.chatterbox import ChatterboxMultilingualEngine
from engines.vieneu_engine import VieNeuTurboEngine
from services.job_manager import JobManager
from services.voice_store import VoiceStore
from models.tts import GenerateRequest, TtsGenerationSettings

BASE = Path(__file__).resolve().parent
DATA = BASE / "data"
voices = VoiceStore(DATA / "voices")
chatterbox = ChatterboxMultilingualEngine()
vieneu = VieNeuTurboEngine()
engines = {
    chatterbox.name: chatterbox,
    vieneu.name: vieneu,
}
engine = chatterbox
jobs = JobManager(DATA / "jobs", engines, voices)
app = FastAPI(title="Local AI TTS Studio", version="1.0.0")


@app.get("/api/health")
def health():
    cb_ready, cb_detail = chatterbox.available
    vieneu_ready, vieneu_detail = vieneu.available
    return {
        "status": "ok",
        "application": "DvaForDev.TtsStudio",
        "backend_version": "3.1",
        "backend_root": str(BASE),
        "engine": chatterbox.name,
        "ready": cb_ready or vieneu_ready,
        "detail": None if (cb_ready or vieneu_ready) else (
            cb_detail or vieneu_detail
        ),
        "engines": {
            chatterbox.name: {"ready": cb_ready, "detail": cb_detail},
            vieneu.name: {"ready": vieneu_ready, "detail": vieneu_detail},
        },
    }


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
            "ffmpeg": shutil.which("ffmpeg") or "not found", "engines": list(engines.keys())}


@app.get("/api/voices")
def list_voices(q: str | None = None):
    return voices.list(q=q)

@app.get("/api/voices/{voice_id}")
def get_voice(voice_id: str):
    voice = voices.get(voice_id)
    if not voice:
        raise HTTPException(404, "Voice not found.")
    return voice

@app.get("/api/voices/{voice_id}/reference")
def get_voice_reference(voice_id: str):
    try:
        path = voices.reference(voice_id)
        media_type = "audio/mpeg" if path.suffix.lower() == ".mp3" else "audio/wav"
        return FileResponse(path, media_type=media_type)
    except (ValueError, FileNotFoundError) as exc:
        raise HTTPException(404, str(exc)) from exc

@app.post("/api/voices", status_code=201)
async def add_voice(name: str = Form(...), language: str | None = Form(None), gender: str | None = Form(None), description: str | None = Form(None), id: str | None = Form(None), file: UploadFile = File(...)):
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
        return voices.add(name.strip(), language, temp_path, gender, description, id)
    except (ValueError, RuntimeError) as exc:
        raise HTTPException(400, str(exc)) from exc
    finally:
        await file.close()
        if temp_path:
            temp_path.unlink(missing_ok=True)

@app.put("/api/voices/{voice_id}/reference", status_code=200)
async def replace_voice_reference(voice_id: str, file: UploadFile = File(...)):
    suffix = Path(file.filename or "").suffix.lower()
    if suffix not in VoiceStore.allowed:
        raise HTTPException(400, "Supported reference formats: WAV, MP3, M4A, and FLAC.")
    temp_path: Path | None = None
    try:
        with tempfile.NamedTemporaryFile(delete=False, suffix=suffix) as temp:
            temp_path = Path(temp.name)
            while chunk := await file.read(1024 * 1024):
                temp.write(chunk)
        return voices.replace_reference(voice_id, temp_path)
    except FileNotFoundError:
        raise HTTPException(404, "Voice not found.")
    except (ValueError, RuntimeError) as exc:
        raise HTTPException(400, str(exc)) from exc
    finally:
        await file.close()
        if temp_path:
            temp_path.unlink(missing_ok=True)

@app.post("/api/voices/{voice_id}/preview", status_code=202)
async def preview_voice(voice_id: str):
    voice = voices.get(voice_id)
    if not voice:
        raise HTTPException(404, "Voice not found.")

    language = (voice.get("language") or "en").lower().strip()
    if "(" in language:
        language = language[language.rfind("(") + 1 : language.rfind(")")].strip()

    if language in ("vi", "vietnamese"):
        text = "Xin chào, đây là bản nghe thử giọng nói tiếng Việt của tôi."
        engine_name = vieneu.name
    else:
        text = "Hello, this is a preview of my voice. I hope you like it!"
        engine_name = "chatterbox_multilingual"

    selected_engine = engines.get(engine_name, chatterbox)
    ready, detail = selected_engine.available
    if not ready:
        raise HTTPException(503, detail)
    try:
        voices.reference(voice_id)
    except (ValueError, FileNotFoundError) as exc:
        raise HTTPException(404, str(exc)) from exc
    settings = TtsGenerationSettings()
    return jobs.create(text, voice_id, language, settings, engine_name=engine_name)


@app.delete("/api/voices/{voice_id}", status_code=204)
def delete_voice(voice_id: str):
    try:
        if not voices.delete(voice_id):
            raise HTTPException(404, "Voice not found.")
    except ValueError as exc:
        raise HTTPException(400, str(exc)) from exc


@app.post("/api/tts/generate", status_code=202)
async def generate(request: GenerateRequest):
    req_engine = (request.engine or "").strip()
    lang_code = (request.language or "").lower().strip()
    if "(" in lang_code:
        lang_code = lang_code[lang_code.rfind("(") + 1 : lang_code.rfind(")")].strip()

    # Vietnamese uses VieNeu; supported foreign languages use Chatterbox.
    if req_engine not in engines or (req_engine == chatterbox.name and lang_code in ("vi", "vi-vn", "vietnamese")):
        req_engine = vieneu.name if lang_code in ("vi", "vi-vn", "vietnamese") else chatterbox.name

    if req_engine == vieneu.name and lang_code not in ("vi", "vi-vn", "vietnamese"):
        raise HTTPException(400, "VieNeu v3 Turbo chỉ hỗ trợ tiếng Việt (vi).")

    selected_engine = engines[req_engine]
    ready, detail = selected_engine.available
    if not ready:
        raise HTTPException(503, detail)
    try:
        voices.reference(request.voice_id)
    except (ValueError, FileNotFoundError) as exc:
        raise HTTPException(404, str(exc)) from exc
    return jobs.create(request.text.strip(), request.voice_id, request.language, request.settings, engine_name=req_engine)


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
