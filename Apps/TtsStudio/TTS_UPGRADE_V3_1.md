# TTS Studio V3.1 — Reliability updates

Scope: feature/tts-studio worktree; no Whiteboard source changed.
No model weights, voice data, or accepted VieNeu presets changed.

## Implemented
- Persist job metadata as backend/data/jobs/<id>.json with atomic replacement.
  On restart, queued/running/cancelling jobs are marked failed for retry;
  completed WAV remains available if present.
- Cooperative cancel waits for a running model thread to finish before
  starting another inference; this does not terminate inference immediately.
- HTTP DELETE on completed job still deletes job/audio after download.
- Batch writes temporary WAV/MP3, validates duration, then replaces output.
  Skip Existing validates existing audio; invalid media is regenerated.
- WAV fallback parses RIFF PCM; MP3 requires ffprobe, not estimated file size.
- VieNeu inference uses Temperature and FFmpeg postprocess uses Speed.
  Unsupported VieNeu CFG, exaggeration, denoise and boost controls are disabled.
  Previous parameter values are preserved when switching engine.
- Backend health advertises application/version/backend_root. Connecting to
  an older compatible backend still works but warns in log.

## Verification
Run from the TTS dev worktree root:
  py -3 -m unittest discover -s Apps/TtsStudio/tests -p "test_*.py" -v
  dotnet build Apps/TtsStudio/DvaForDev.TtsStudio.csproj -c Release

## Next
- Provision independent TTS Python environment and migrate voice library after backup.
- Persistent batch manifests, content hashes, resumption after restart.
- A/B voice comparison, pitch and duration analysis, per-voice presets.
- Forced alignment for improved timestamps after joining long recordings.

Do not delete legacy backend/voice directories until migration is verified.
