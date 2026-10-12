"""V2 compositor unit checks: caption safety, track validation and cache."""
import json
import tempfile
import unittest
from pathlib import Path

from video_tracks import ass_escape, ass_time, create_ass, track_signature, validate_tracks

class ComposerTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.root=Path(self.temp.name)
        (self.root/"voice.wav").write_bytes(b"test audio")
        (self.root/"music.wav").write_bytes(b"test background music")
        (self.root/"words.srt").write_text(
            "1\n00:00:00,000 --> 00:00:01,200\nHello viewer!\n\n"
            "2\n00:00:01,200 --> 00:00:02,400\nYear 1903 is important.\n",encoding="utf-8")
        self.project={"schemaVersion":1,"audioPath":"voice.wav",
                      "subtitlePath":"words.srt","musicPath":"music.wav",
                      "musicVolume":.12,"burnSubtitles":True}
    def tearDown(self):
        self.temp.cleanup()
    def test_load_tracks_valid(self):
        music,vol,burn,cues=validate_tracks(self.project,self.root,2.4)
        self.assertEqual(music,self.root/"music.wav")
        self.assertAlmostEqual(vol,.12)
        self.assertTrue(burn)
        self.assertEqual(len(cues),2)
    def test_optional_tracks_off_without_srt(self):
        p={"musicPath":"","burnSubtitles":False}
        self.assertEqual(validate_tracks(p,self.root,2.4),(None,.12,False,[]))
    def test_reject_missing_music(self):
        self.project["musicPath"]="missing.wav"
        with self.assertRaisesRegex(ValueError,"missing"):
            validate_tracks(self.project,self.root,2.4)
    def test_reject_negative_volume(self):
        self.project["musicVolume"]=-.01
        with self.assertRaisesRegex(ValueError,"volume"):
            validate_tracks(self.project,self.root,2.4)
    def test_reject_excessive_volume(self):
        self.project["musicVolume"]=.99
        with self.assertRaisesRegex(ValueError,"volume"):
            validate_tracks(self.project,self.root,2.4)
    def test_reject_missing_srt_for_burn(self):
        self.project["subtitlePath"]=""
        with self.assertRaisesRegex(ValueError,"SRT"):
            validate_tracks(self.project,self.root,2.4)
    def test_reject_srt_beyond_audio_duration(self):
        with self.assertRaisesRegex(ValueError,"timing"):
            validate_tracks(self.project,self.root,1.0)
    def test_escaping_ass_prevents_override_commands(self):
        self.assertNotIn("{",ass_escape(r"1903 {\pos(0,0)}"))
        self.assertNotIn("\\pos",ass_escape(r"1903 {\pos(0,0)}"))
        self.assertEqual(ass_time(1.25),"0:00:01.25")
    def test_single_scene_subtitle_trim(self):
        _,_,_,cues=validate_tracks(self.project,self.root,2.4)
        count=create_ass(self.root/"subtitles.ass",cues,1.0,2.4,640,360)
        content=(self.root/"subtitles.ass").read_text(encoding="utf-8")
        self.assertEqual(count,2)
        self.assertIn("0:00:00.00,0:00:00.20",content)
        self.assertIn("0:00:00.20,0:00:01.40",content)
        self.assertIn("Hello viewer",content)
        self.assertIn("Year 1903",content)
    def test_track_signature_invalidates_music_and_cues(self):
        m,v,b,c=validate_tracks(self.project,self.root,2.4)
        previous=track_signature(self.project,m,b,c)
        (self.root/"music.wav").write_bytes(b"changed")
        self.assertNotEqual(previous,track_signature(self.project,m,b,c))
        self.assertNotEqual(previous,track_signature(self.project,m,b,c+[(-1,0,"extra")]))
    def test_valid_no_captions_creates_empty_events(self):
        self.assertEqual(create_ass(self.root/"empty.ass",[],0,2.0,640,360),0)

if __name__=="__main__":
    unittest.main()
