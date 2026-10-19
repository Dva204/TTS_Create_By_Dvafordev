import asyncio
import sys
import tempfile
import threading
import unittest
import wave
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "backend"))
from services.job_manager import JobManager
from models.tts import TtsGenerationSettings

class VoiceStore:
    def reference(self, id):
        return Path(__file__)

class Engine:
    name = "vieneu_v3_turbo"
    def __init__(self):
        self.release = threading.Event()
        self.entered = threading.Event()
        self.calls = 0
        self.active = 0
        self.overlap = False
    def generate(self, text, language, reference, output, settings):
        self.calls += 1
        self.active += 1
        self.overlap |= self.active > 1
        self.entered.set()
        if not self.release.wait(4):
            raise TimeoutError("test worker blocked")
        with wave.open(str(output),"wb") as f:
            f.setnchannels(1);f.setsampwidth(2);f.setframerate(24000)
            f.writeframes(b"\x00\x00" * 1200)
        self.active -= 1

class QueueTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.engine = Engine()
        self.manager = JobManager(self.root,self.engine,VoiceStore())
        self.settings = TtsGenerationSettings(speed=1,speaker_boost=False)
    async def asyncTearDown(self):
        self.engine.release.set()
        await asyncio.gather(*list(self.manager.tasks.values()),return_exceptions=True)
        self.tmp.cleanup()
    async def until(self,predicate):
        for _ in range(150):
            if predicate():return
            await asyncio.sleep(.02)
        self.fail("wait timed out")
    async def test_cancel_running_never_overlaps_worker(self):
        a=self.manager.create("a","v1","vi",self.settings)
        await self.until(lambda:a["status"]=="running")
        self.assertTrue(self.manager.cancel(a["id"]))
        self.assertEqual(a["status"],"cancelling")
        b=self.manager.create("b","v1","vi",self.settings)
        await asyncio.sleep(.08)
        self.assertEqual(self.engine.calls,1)
        self.assertEqual(b["status"],"queued")
        self.engine.release.set()
        await self.until(lambda:b["status"]=="completed")
        self.assertFalse(self.engine.overlap)
        self.assertEqual(a["status"],"cancelled")
        self.assertFalse((self.root / f"{a['id']}.wav").exists())
        self.assertTrue((self.root / f"{b['id']}.wav").exists())
    async def test_completed_recovered_and_deleted_on_consumption(self):
        self.engine.release.set()
        job=self.manager.create("x","v1","vi",self.settings)
        await self.until(lambda:job["status"]=="completed")
        recovered=JobManager(self.root,self.engine,VoiceStore())
        self.assertEqual(recovered.jobs[job["id"]]["status"],"completed")
        self.assertTrue(recovered.audio(job["id"]).exists())
        self.assertTrue(recovered.cancel(job["id"]))
        self.assertFalse(recovered._job_file(job["id"]).exists())
    async def test_interrupted_jobs_restore_failed(self):
        job={"id":"a"*32,"status":"running","audio_url":None,"created_at":"today","error":None}
        self.manager._save_job(job)
        recovered=JobManager(self.root,self.engine,VoiceStore())
        self.assertEqual(recovered.jobs[job["id"]]["status"],"failed")
    async def test_cancel_queued_does_not_execute(self):
        a=self.manager.create("a","v1","vi",self.settings)
        await self.until(lambda:a["status"]=="running")
        b=self.manager.create("b","v1","vi",self.settings)
        self.manager.cancel(b["id"])
        self.assertEqual(b["status"],"cancelled")
        self.engine.release.set()
        await self.until(lambda:a["status"]=="completed")
        self.assertEqual(self.engine.calls,1)

if __name__=="__main__":unittest.main()
