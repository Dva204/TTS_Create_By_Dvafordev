"""Optional VieNeu-TTS v3 Turbo zero-shot Vietnamese cloning via isolated ONNX/CPU.

Preserves Chatterbox dependencies. Worker is kept alive
so multiple preview/generation jobs reuse the loaded CPU model.
"""
from __future__ import annotations

import atexit
import importlib.util
import json
import os
import queue
import subprocess
import threading
from pathlib import Path
from typing import TextIO

from .base import TtsEngine
from models.tts import TtsGenerationSettings

BASE = Path(__file__).resolve().parents[1]
RESULT_PREFIX = "@VIENEU_RESULT@"

class VieNeuTurboEngine(TtsEngine):
    name = "vieneu_v3_turbo"

    def __init__(self) -> None:
        self._lock = threading.RLock()
        self._worker: subprocess.Popen[str] | None = None
        self._events: queue.Queue[str] = queue.Queue()
        self._log: TextIO | None = None
        atexit.register(self.close)

    @staticmethod
    def python_path() -> Path:
        configured = os.getenv("DVA_VIENEU_PYTHON", "").strip()
        if configured:
            return Path(configured).expanduser()
        directory = BASE / ".venv-vieneu"
        return directory / ("Scripts/python.exe" if os.name == "nt" else "bin/python")

    @property
    def available(self) -> tuple[bool, str | None]:
        executable = self.python_path()
        if not executable.is_file():
            return False, (
                "VieNeu chưa được cài. Chạy file backend/install_vieneu_cpu.ps1 "
                "để tạo môi trường CPU riêng; không ảnh hưởng engine cũ."
            )
        # Check the isolated environment without importing model weights.
        site_packages = executable.parent.parent / "Lib" / "site-packages"
        if os.name == "nt" and not (site_packages / "vieneu" / "__init__.py").is_file():
            return False, "VieNeu chưa được cài trong môi trường .venv-vieneu."
        return True, None

    def _read_output(self, process: subprocess.Popen[str], events: queue.Queue[str]) -> None:
        assert process.stdout is not None
        for line in process.stdout:
            if line.startswith(RESULT_PREFIX):
                events.put(line[len(RESULT_PREFIX):].strip())

    def _start_worker(self) -> None:
        if self._worker and self._worker.poll() is None:
            return
        self.close()
        ready, detail = self.available
        if not ready:
            raise RuntimeError(detail)
        logdir = BASE / "data" / "logs"
        logdir.mkdir(parents=True, exist_ok=True)
        self._log = (logdir / "vieneu_worker.log").open("a", encoding="utf-8")
        options = {
            "stdin": subprocess.PIPE, "stdout": subprocess.PIPE,
            "stderr": self._log, "text": True, "encoding": "utf-8",
            "errors": "replace", "bufsize": 1, "cwd": str(BASE)
        }
        if os.name == "nt":
            options["creationflags"] = getattr(subprocess, "CREATE_NO_WINDOW", 0)
        self._worker = subprocess.Popen(
            [str(self.python_path()), "-u", str(BASE / "vieneu_worker.py")], **options
        )
        self._events = queue.Queue()
        threading.Thread(
            target=self._read_output, args=(self._worker, self._events),
            daemon=True, name="vieneu-stdout"
        ).start()

    def generate(self, text: str, language: str, reference: Path,
                 output: Path, settings: TtsGenerationSettings) -> None:
        normalized = (language or "").lower().strip()
        if "(" in normalized and ")" in normalized:
            normalized = normalized[normalized.rfind("(")+1:normalized.rfind(")")]
        if not (normalized == "vi" or normalized.startswith("vi-") or normalized == "vietnamese"):
            raise ValueError("VieNeu v3 Turbo chỉ hỗ trợ TTS tiếng Việt (vi).")
        if not text.strip():
            raise ValueError("TTS input cannot be empty.")
        if not reference.is_file():
            raise FileNotFoundError("Không tìm thấy audio mẫu trong Voice Library.")
        with self._lock:
            self._start_worker()
            assert self._worker is not None and self._worker.stdin is not None
            payload = {
                "action": "generate",
                "text": text,
                "reference": str(reference.resolve()),
                "output": str(output.resolve()),
                "temperature": float(settings.temperature),
            }
            try:
                self._worker.stdin.write(json.dumps(payload, ensure_ascii=False)+"\n")
                self._worker.stdin.flush()
                result = self._events.get(timeout=1200)  # model download / long passage
            except (BrokenPipeError, OSError, queue.Empty) as exc:
                self.close()
                raise RuntimeError(
                    "VieNeu không phản hồi. Xem backend/data/logs/vieneu_worker.log "
                    "để biết lỗi model hoặc RAM."
                ) from exc
            response = json.loads(result)
            if not response.get("ok"):
                raise RuntimeError("VieNeu: " + str(response.get("error", "Lỗi không xác định")))
            if not output.is_file() or output.stat().st_size < 44:
                raise RuntimeError("VieNeu báo hoàn tất nhưng file WAV không tồn tại.")

    def close(self) -> None:
        # Called by atexit and before restarting a worker. Do not destroy
        # another engine or the shared FastAPI server.
        with self._lock:
            process = self._worker
            self._worker = None
            if process is not None:
                try:
                    if process.poll() is None:
                        process.terminate()
                        try: process.wait(timeout=3)
                        except subprocess.TimeoutExpired: process.kill()
                except OSError:
                    pass
                for pipe in (process.stdin, process.stdout):
                    if pipe is not None:
                        try: pipe.close()
                        except OSError: pass
            if self._log is not None:
                try: self._log.close()
                except OSError: pass
                self._log = None
