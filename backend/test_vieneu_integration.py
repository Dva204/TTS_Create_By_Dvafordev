"""VieNeu worker / API integration tests without downloading model weights.

The fake SDK is injected only into an isolated subprocess via PYTHONPATH.
No changes are made to user voices, downloaded models or saved jobs.
"""
from __future__ import annotations

import os
import tempfile
import time
import unittest
import wave
from pathlib import Path
from unittest.mock import patch

from engines.vieneu_engine import VieNeuTurboEngine
from models.tts import TtsGenerationSettings


class VieNeuIntegrationTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(prefix="dva_vieneu_smoke_")
        self.root = Path(self.tmp.name)
        sdk = self.root / "vieneu"
        (sdk / "_v3_turbo_engine").mkdir(parents=True)
        (sdk / "_v3_turbo_engine" / "__init__.py").write_text("", encoding="utf-8")
        (sdk / "_v3_turbo_engine" / "onnx_runtime_lite.py").write_text(
            "class OnnxV3LiteEngine:\n    _fetch = staticmethod(lambda *a: None)\n",
            encoding="utf-8"
        )
        (sdk / "__init__.py").write_text(
            "import os\n"
            "import wave\n"
            "from pathlib import Path\n"
            "class Vieneu:\n"
            " def __init__(self,backend='onnx'):\n"
            "  with open(os.environ['DVA_FAKE_INIT_FILE'],'a') as f:f.write('init\\n')\n"
            " def infer(self,text,ref_audio=None,denoise=True):\n"
            "  assert ref_audio and Path(ref_audio).is_file()\n"
            "  return b'\\x00\\x00'*48000\n"
            " def save(self,audio,path):\n"
            "  with wave.open(str(path),'wb') as f:\n"
            "   f.setnchannels(1);f.setsampwidth(2);f.setframerate(48000);f.writeframes(audio)\n",
            encoding="utf-8"
        )
        self.reference = self.root / "consented_test_reference.wav"
        with wave.open(str(self.reference),"wb") as f:
            f.setnchannels(1)
            f.setsampwidth(2)
            f.setframerate(24000)
            f.writeframes(b'\0\0' * 24000)
        self.environ = patch.dict(os.environ, {
            "PYTHONPATH": str(self.root) + os.pathsep + os.environ.get("PYTHONPATH", ""),
            "DVA_FAKE_INIT_FILE": str(self.root / "init.txt"),
        })
        self.environ.start()
        self.engine = VieNeuTurboEngine()

    def tearDown(self):
        self.engine.close()
        self.environ.stop()
        self.tmp.cleanup()

    def test_worker_stays_alive_reuses_model_and_exports_wav(self):
        ready, detail = self.engine.available
        self.assertTrue(ready, detail)
        first, second = self.root / "first.wav", self.root / "second.wav"
        self.engine.generate(
            "Xin chào. Đây là đoạn thứ nhất.", "vi", self.reference, first,
            TtsGenerationSettings()
        )
        self.engine.generate(
            "Đoạn thứ hai giữ nguyên giọng thử nghiệm.", "Tiếng Việt (vi)",
            self.reference, second, TtsGenerationSettings()
        )
        for file in (first, second):
            with wave.open(str(file), "rb") as f:
                self.assertEqual(f.getframerate(), 48000)
                self.assertGreater(f.getnframes(), 0)
        self.assertEqual((self.root/"init.txt").read_text().count("init"), 1)

    def test_non_vietnamese_fails_before_worker(self):
        with self.assertRaisesRegex(ValueError, "tiếng Việt"):
            self.engine.generate("Guten Tag", "de", self.reference,
                                 self.root/"wrong.wav", TtsGenerationSettings())
        self.assertFalse((self.root / "init.txt").exists())

    def test_vieneu_route_uses_existing_voice_library_and_job_system(self):
        # Import the existing real FastAPI app and redirect its stores to
        # temporary fixtures, so no user jobs or voices are changed.
        from fastapi.testclient import TestClient
        import main
        from services.voice_store import VoiceStore
        from services.job_manager import JobManager

        new_store = VoiceStore(self.root/"voices")
        saved = new_store.add(
            "Consent test", "vi", self.reference, description="Synthetic QA sample"
        )
        with patch.object(main, "voices", new_store), \
             patch.object(main, "jobs", JobManager(
                 self.root/"jobs", main.engines, new_store)), \
             patch.dict(main.engines, {"vieneu_v3_turbo": self.engine}):
            with TestClient(main.app) as client:
                result = client.post("/api/tts/generate", json={
                    "text": "Xin chào, đây là bài kiểm tra VieNeu.",
                    "voice_id": saved["id"], "language": "vi",
                    "engine": "vieneu_v3_turbo",
                    "settings": {"speaker_boost": True}
                })
                self.assertEqual(result.status_code, 202, result.text)
                job_id = result.json()["id"]
                end = time.monotonic() + 30
                while time.monotonic() < end:
                    record = client.get(f"/api/jobs/{job_id}").json()
                    if record["status"] in ("completed", "failed"):
                        break
                    time.sleep(0.25)
                self.assertEqual(record["status"], "completed", record)
                audio_response = client.get(f"/api/jobs/{job_id}/audio")
                self.assertEqual(audio_response.status_code, 200)
                output = self.root/"api-output.wav"
                output.write_bytes(audio_response.content)
                with wave.open(str(output),"rb") as wav:
                    self.assertEqual(wav.getframerate(),48000)
                    self.assertGreater(wav.getnframes(),0)


if __name__ == "__main__":
    unittest.main()
