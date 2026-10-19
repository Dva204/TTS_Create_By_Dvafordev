# DvaForDev TTS Studio V4 - Development milestone

Worktree: D:\TestTTS\DvaForDev_TTS_Dev
Branch: feature/tts-studio. No Whiteboard source changes.
All voice/reference data and VieNeu C presets preserved.

## Included

1. Voice Cloning V2 foundation: listen to reference/clone A/B, generate with
   selected voice/settings, inspect approximate pitch, RMS and clipping.
   Acoustic diagnostics are NOT proof of identical cloned voice.
2. Voice Library V3 foundation: favorites, tags, per-voice presets stored
   separately under LOCALAPPDATA/DvaForDev/TtsStudio, plus manual ZIP backup.
   Does NOT rewrite backend/data/voices/*/voice.json.
3. Smart SRT validation: overlap/gap/end times vs audio length from ffprobe.
   Real WhisperX forced-alignment code is optional; it requires manually
   installing Python 3.11 environment with backend/install_alignment_cpu.ps1.
   Model may download on first use after explicit action. Not yet tested on
   actual 30-minute audio; original SRT never overwritten.
4. Backend Manager: health/version/root, GPU/VRAM diagnosis, safe reconnect.
5. TTS UI V4: 4 new sidebar workspaces and stable header/body layout.
6. Performance: content-addressed WAV cache with reference SHA256, voice,
   text, engine, settings; disabled by default, switch affects NEXT start.
7. Project Manager: JSON project save/load, audio/SRT safe folder export,
   explicit warnings about missing audio and protection against path escapes.

## Test commands from the worktree root

dotnet build Apps/TtsStudio/DvaForDev.TtsStudio.csproj -c Release
py -3 -m unittest discover -s Apps/TtsStudio/tests -p test_*.py -v

## Not complete / not validated end-to-end

- Full VieNeu timbre optimization, automatic voice similarity ranking.
- WhisperX alignment runtime/model install and validation for all languages.
- Production 10-30 minute audio, crash-resumable WinForms Batch and snapshots.
- Per-model unload/benchmark and independent GPU memory scheduling.
- Full voice archive restore / historical references.

Never remove or commit private voice/reference assets and user voice.json edits.
Do not push until explicitly requested.


## Streamlined navigation (local UI update)

- Sidebar contains just **Tạo audio**, **Giọng đọc** and **Quản lý**.
- Horizontal contextual subtabs show only related workflows:
  - Tạo audio: TTS/Hàng loạt, Dự án & Xuất, SRT & Căn chỉnh.
  - Giọng đọc: Thư viện giọng, Clone giọng.
  - Quản lý: Cài đặt, Backend & Hiệu năng.
- Switching a main section returns to its last visited subtab.
- Internal TabPage indexes remain unchanged to preserve existing event handlers.
- Navigation has its own regression tests at normal and compact screen sizes.
