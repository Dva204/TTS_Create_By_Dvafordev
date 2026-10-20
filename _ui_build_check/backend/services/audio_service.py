import shutil
import subprocess
import wave
from pathlib import Path


def normalize_reference(source: Path, destination: Path) -> bool:
    """Create mono 24 kHz PCM WAV. Returns False if no safe converter is available."""
    destination.parent.mkdir(parents=True, exist_ok=True)
    try:
        with wave.open(str(source), "rb") as audio:
            if audio.getnchannels() == 1 and audio.getframerate() == 24000 and audio.getsampwidth() == 2:
                shutil.copy2(source, destination)
                return True
    except (wave.Error, EOFError):
        pass
    ffmpeg = shutil.which("ffmpeg")
    if not ffmpeg:
        return False
    result = subprocess.run(
        [ffmpeg, "-y", "-i", str(source), "-ac", "1", "-ar", "24000", "-c:a", "pcm_s16le", str(destination)],
        capture_output=True, text=True, check=False,
    )
    return result.returncode == 0 and destination.exists()
