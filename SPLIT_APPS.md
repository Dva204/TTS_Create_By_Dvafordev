# DvaForDev — independent development projects

This is a **source split for independent development**, NOT installer or release packaging.

## Structure

Apps/TtsStudio/ owns its WinForms UI, Models/, Services/, and its own Python backend/ source.
Apps/WhiteboardStudio/ owns Whiteboard UI, AiVideoStudio/, and its own whiteboard_tools/ scripts.
Shared/ is a tiny stable shared library (application naming conventions).
DvaForDevSuite.sln builds the two independent projects.
The legacy TTS Create By DvaForDev.sln and its source files remain intact as a backup.

**No cross-project C# source links remain between new projects or to the original combined app.**
Edits to one new project are not reflected in the other or the legacy app.

## Start development independently

In PowerShell from the repository root:

    dotnet run --project Apps/TtsStudio/DvaForDev.TtsStudio.csproj
    dotnet run --project Apps/WhiteboardStudio/DvaForDev.WhiteboardStudio.csproj

Build both with:

    dotnet build DvaForDevSuite.sln -c Release

Open either .csproj directly in Visual Studio to work only on that tool.

### TTS Studio

Only TTS / Batch, Voice Library and Settings. VieNeu and Chatterbox need the TTS backend on
127.0.0.1:7865. Backend code lives in Apps/TtsStudio/backend/ and no longer depends on
the legacy root backend source.

The UI reuses an existing healthy backend on 7865. When editing backend Python, restart
the actual server running at 7865 to test the new code. A running older backend serves
older code even when the new source changed.

No .venv, model weights, voice reference recordings or user data were copied or moved.
Create an isolated Python environment for the new backend and deliberately migrate/back
up voice library data before retiring the old backend. Data belongs to whichever backend
server is currently active.

### Whiteboard Studio

Whiteboard AI for single-image tests + AI Video Studio / video editing.
Accepts external audio MP3/WAV, SRT, scene JSON and image files.
Renderer scripts now live at Apps/WhiteboardStudio/whiteboard_tools/.
Use the 'Chọn Whiteboard Engine' header button to select and remember renderer path;
the choice applies to both tabs and is stored under:
%LOCALAPPDATA%/DvaForDev/WhiteboardStudio/renderer-path.txt

Whiteboard rendering still requires a separately installed renderer and FFmpeg/FFprobe.

## Development workflow

Start feature branches off feature/split-tts-whiteboard, for example feature/tts-voices
and feature/whiteboard-render. Limit tool-specific changes to its own project folder.
Use Shared only for genuinely stable common abstractions. There are no releases or ZIPs
needed at this stage. Nothing will be pushed to GitHub without a separate request.
