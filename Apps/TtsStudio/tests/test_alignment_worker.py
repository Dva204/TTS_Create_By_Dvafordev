import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

sys.path.insert(0,str(Path(__file__).resolve().parents[1] / "backend" / "tools"))
from forced_align_srt import read_srt, seconds_to_stamp, align_srt


class FakeWhisperX:
    @staticmethod
    def load_audio(path):
        return b"pretend-audio"
    @staticmethod
    def load_align_model(language_code, device):
        assert language_code == "vi" and device == "cpu"
        return object(), {"lang": "vi"}
    @staticmethod
    def align(segments, model, metadata, audio, device, return_char_alignments=False):
        return {"segments": [{
            "words": [{"start": segment["start"] + .1, "end": segment["end"] - .05}]
        } for segment in segments]}


class AlignmentWorkerTests(unittest.TestCase):
    def test_parse_and_format_srt_times(self):
        with tempfile.TemporaryDirectory() as temp:
            source=Path(temp) / "subtitle.srt"
            source.write_text("1\n00:00:00,000 --> 00:00:01,000\nXin chao\n\n"+
                              "2\n00:00:01,000 --> 00:00:02,100\nBan\n",encoding="utf-8")
            cues=read_srt(source)
            self.assertEqual(len(cues),2)
            self.assertEqual(seconds_to_stamp(2.1),"00:00:02,100")
    def test_mocked_alignment_keeps_original_and_refuses_overwrite(self):
        with tempfile.TemporaryDirectory() as temp:
            src=Path(temp) / "subtitles.srt"
            dst=Path(temp) / "subtitles_aligned.srt"
            audio=Path(temp) / "audio.wav"
            original="1\n00:00:00,000 --> 00:00:01,000\nXin chao\n"
            src.write_text(original,encoding="utf-8")
            audio.write_bytes(b"not-real-audio")
            with patch.dict(sys.modules,{"whisperx":FakeWhisperX}):
                self.assertEqual(align_srt(audio,src,"vi",dst),1)
                self.assertIn("00:00:00,100 --> 00:00:00,950",
                              dst.read_text(encoding="utf-8"))
                self.assertEqual(src.read_text(encoding="utf-8"),original)
                with self.assertRaises(FileExistsError):
                    align_srt(audio,src,"vi",dst)


if __name__=="__main__":unittest.main()
