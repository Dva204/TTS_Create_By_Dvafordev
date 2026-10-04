# TTS Studio

TTS Studio is a native .NET 8 WinForms desktop application backed by a local FastAPI service and Chatterbox TTS. Phase 2 is operational: the application can import a local reference voice, queue a synthesis job, generate speech with Chatterbox on CUDA, and download the resulting WAV file.

The WinForms interface uses a light theme and includes backend/GPU status, engine, language, and voice selectors, a text editor, and controls for Add Voice, Generate, Stop, Play, Save WAV, and Logs.

## Phase 2 status

The following configuration and end-to-end behavior have been verified on the Windows test machine:

- A project-local Python environment exists at `backend/.venv`.
- Python 3.13.7 is in use.
- `torch 2.6.0+cu124` and `torchaudio 2.6.0+cu124` are installed, and CUDA is working.
- The tested GPU is an NVIDIA GeForce RTX 3050 Laptop GPU with 4 GB of VRAM.
- `chatterbox-tts 0.1.7` is installed.
- `setuptools` must remain pinned to `<81` because `resemble-perth` currently relies on `pkg_resources` in this setup.
- FFmpeg is available. Imported reference audio is normalized to mono, 24 kHz WAV.
- A real Chatterbox model load completed successfully on CUDA.
- Real local TTS generation completed successfully.
- The complete API flow was verified: import a reference voice, queue a job, generate with Chatterbox, and download the output WAV.

In a short test, the loaded model used about 3.1 GB of allocated VRAM and reached about 3.2 GB peak usage. A 4 GB GPU is therefore a tight fit; other GPU activity or larger workloads may cause out-of-memory errors.

The first model load may also need to download model files and can take noticeably longer. Later runs are faster after those files have been cached.

## Build and run

From the project directory, build and start the desktop application:

```powershell
dotnet build "TTS Create By DvaForDev.csproj"
dotnet run --project "TTS Create By DvaForDev.csproj"
```

The desktop application starts the local backend and captures its output. Python dependencies are declared in `backend/requirements.txt` and are installed in the project-local `backend/.venv` on the verified setup.

## Use

1. Select **Add Voice** and choose a local reference audio file that you have permission to use. Supported import formats are WAV, MP3, M4A, and FLAC.
2. Choose the language and the imported reference voice.
3. Enter the text to synthesize.
4. Select **Generate** and wait for the queued Chatterbox job to finish.
5. Use **Play** to preview the result or **Save WAV** to save it locally.

The application accepts a local reference audio file; it does not claim ElevenLabs compatibility or grant permission to use any voice or recording.

## Local API

The backend exposes the following endpoints for the desktop application:

- `GET /api/health`
- `GET /api/system`
- `GET /api/voices`
- `POST /api/voices`
- `DELETE /api/voices/{id}`
- `POST /api/tts/generate`
- `GET /api/jobs/{id}`

Job cancellation and completed-audio download are also exposed for the desktop controls. Voice metadata is stored under `backend/data/voices/{id}/voice.json`, with normalized reference audio stored as `reference.wav`.
