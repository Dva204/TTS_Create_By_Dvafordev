"""Isolated local VieNeu v3 Turbo CPU process (JSON-lines protocol).
Run from backend/.venv-vieneu, never from the Chatterbox Python venv.
"""
from __future__ import annotations

import contextlib
import json
import sys
import traceback
from pathlib import Path

sys.stdin.reconfigure(encoding="utf-8", errors="strict")
sys.stdout.reconfigure(encoding="utf-8", errors="strict")

PREFIX = "@VIENEU_RESULT@"

def respond(value: dict) -> None:
    print(PREFIX + json.dumps(value, ensure_ascii=False), flush=True)

def _prepare_windows_onnx_cache() -> None:
    """Materialize Hugging Face ONNX symlinks without rewriting model files.

    Newer ONNX Runtime correctly forbids external .data files resolving
    outside a model directory. HF stores ONNX and .data as distinct symlinks
    into different blob subdirectories; this triggers that validation on
    Windows. Create hardlinks in a local cache so all files resolve inside
    the same directory (fall back to copies if hardlink unavailable).
    """
    if sys.platform != "win32":
        return
    import hashlib
    import os
    import shutil
    from vieneu._v3_turbo_engine.onnx_runtime_lite import OnnxV3LiteEngine

    original_fetch = OnnxV3LiteEngine._fetch

    def materialized_fetch(repo: str, files: list[str], subfolder: str | None):
        snapshot_dir = original_fetch(repo, files, subfolder)
        # Snapshot path carries the model commit hash, preventing stale caches.
        identity = f"{repo}|{subfolder}|{snapshot_dir}"
        key = hashlib.sha256(identity.encode("utf-8")).hexdigest()[:20]
        location = Path.home() / ".cache" / "vieneu" / "onnx-local" / key
        location.mkdir(parents=True, exist_ok=True)
        for filename in files:
            original = snapshot_dir / filename
            if not original.is_file():
                continue
            destination = location / filename
            if destination.exists():
                if destination.stat().st_size == original.stat().st_size:
                    continue
                raise RuntimeError(
                    f"VieNeu local model data differs at {destination}; "
                    "refusing to overwrite an existing file."
                )
            # A hardlink is the same immutable cached model blob, but ONNX
            # sees a real in-directory file and accepts its external data.
            try:
                os.link(original.resolve(), destination)
            except OSError:
                shutil.copy2(original, destination)
        return location

    OnnxV3LiteEngine._fetch = staticmethod(materialized_fetch)


def main() -> None:
    model = None
    for line in sys.stdin:
        try:
            task = json.loads(line)
            if task.get("action") == "ping":
                respond({"ok": True, "message": "worker alive"})
                continue
            if task.get("action") != "generate":
                raise ValueError("Unsupported VieNeu worker operation.")
            text = str(task.get("text") or "").strip()
            text = text.encode("utf-8", errors="replace").decode("utf-8")
            reference = Path(str(task["reference"])).resolve(strict=True)
            output = Path(str(task["output"])).resolve()
            if not text or len(text) > 5000:
                raise ValueError("Text length must be 1–5000 characters.")
            if not reference.is_file() or reference.suffix.lower() not in {".wav", ".flac", ".mp3", ".m4a"}:
                raise ValueError("A valid voice reference audio is required.")
            output.parent.mkdir(parents=True, exist_ok=True)

            # SDK (including optional initial model download) can write to
            # stdout. Keep the IPC stream clean by redirecting model logs.
            with contextlib.redirect_stdout(sys.stderr):
                if model is None:
                    from vieneu import Vieneu
                    _prepare_windows_onnx_cache()
                    model = Vieneu(mode="v3turbo", backend="onnx")
                temperature = float(task.get("temperature", 0.65))
                if not 0.1 <= temperature <= 1.5:
                    raise ValueError("VieNeu temperature must be 0.1 to 1.5")
                wav = model.infer(text=text, ref_audio=str(reference), temperature=temperature)
                model.save(wav, str(output))
            if not output.is_file() or output.stat().st_size < 44:
                raise RuntimeError("VieNeu did not produce a valid audio file.")
            respond({"ok": True, "output": str(output), "bytes": output.stat().st_size})
        except Exception as exc:
            traceback.print_exc(file=sys.stderr)
            respond({"ok": False, "error": f"{type(exc).__name__}: {exc}"})

if __name__ == "__main__":
    main()
