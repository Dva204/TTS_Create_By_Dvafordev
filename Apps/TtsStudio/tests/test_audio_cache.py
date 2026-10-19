import tempfile
import unittest
import wave
from pathlib import Path
import sys

sys.path.insert(0,str(Path(__file__).resolve().parents[1] / "backend"))
from services.audio_cache import AudioResultCache
from models.tts import TtsGenerationSettings


class AudioCacheTests(unittest.TestCase):
    def test_only_exact_reference_voice_and_settings_match(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp)
            ref=root/"reference.wav"
            ref.write_bytes(b"reference-1")
            params=TtsGenerationSettings()
            key=AudioResultCache.digest("hello","voice-1","en","chatterbox_multilingual",params,ref)
            self.assertNotEqual(key,AudioResultCache.digest("hello!","voice-1","en","chatterbox_multilingual",params,ref))
            self.assertNotEqual(key,AudioResultCache.digest("hello","voice-2","en","chatterbox_multilingual",params,ref))
            ref.write_bytes(b"reference-2")
            self.assertNotEqual(key,AudioResultCache.digest("hello","voice-1","en","chatterbox_multilingual",params,ref))
    def test_off_by_default_and_valid_wav_only(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp)
            target=root/"output.wav"
            disabled=AudioResultCache(root/"cache",enabled=False)
            self.assertFalse(disabled.restore("a"*64,target))
            enabled=AudioResultCache(root/"cache",enabled=True)
            source=root/"original.wav"
            with wave.open(str(source),"wb") as sound:
                sound.setnchannels(1);sound.setsampwidth(2);sound.setframerate(16000)
                sound.writeframes(b"\0\0"*1600)
            enabled.remember("a"*64,source)
            self.assertTrue(enabled.restore("a"*64,target))
            self.assertEqual(target.read_bytes(),source.read_bytes())
            target.unlink()
            (root/"cache"/("b"*64+".wav")).write_bytes(b"corrupt")
            self.assertFalse(enabled.restore("b"*64,target))
if __name__=="__main__":unittest.main()
