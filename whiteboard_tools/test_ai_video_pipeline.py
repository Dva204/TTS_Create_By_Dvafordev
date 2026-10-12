"""Unit tests for the multi-engine project renderer. No GPU rendering required."""
import json
import tempfile
import unittest
from pathlib import Path

from PIL import Image
from ai_video_pipeline import CURRENT_VERSION, SUPPORTED, load_project, digest_scene


class ProjectPipelineTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.image = self.root / "scene.png"
        Image.new("RGB", (100, 60), "white").save(self.image)
        self.plan = self.root / "project.ai-video.json"
        self.example = {
            "schemaVersion": 1,
            "audioPath": "tts.wav",
            "scenes": [
                {"index": 1, "cueFrom": 1, "cueTo": 2,
                 "startMs": 0, "endMs": 3500, "styleId": "image-motion",
                 "imagePath": "scene.png", "narration": "Illustration",
                 "notes": ""},
                {"index": 2, "cueFrom": 3, "cueTo": 3,
                 "startMs": 3500, "endMs": 7000, "styleId": "text",
                 "imagePath": "", "narration": "Important text", "notes": ""}
            ]
        }

    def tearDown(self):
        self.temp.cleanup()

    def parse(self, duration=7.0):
        self.plan.write_text(json.dumps(self.example), encoding="utf-8")
        return load_project(self.plan, duration)[1]

    def test_valid_mixed_project(self):
        scenes = self.parse(7.2)
        self.assertEqual(len(scenes), 2)
        self.assertAlmostEqual(scenes[-1]["end"], 7.2)
        self.assertEqual(scenes[0]["image"], self.image)
        self.assertEqual(scenes[-1]["style"], "text")

    def test_invalid_style_rejected_instead_of_misrender(self):
        self.example["scenes"][0]["styleId"] = "map"
        with self.assertRaisesRegex(ValueError, "no renderer yet"):
            self.parse()

    def test_gap_rejected(self):
        self.example["scenes"][1]["startMs"] = 3600
        with self.assertRaisesRegex(ValueError, "gap/overlap"):
            self.parse()

    def test_invalid_cue_order_rejected(self):
        self.example["scenes"][1]["cueFrom"] = 5
        with self.assertRaisesRegex(ValueError, "cue order"):
            self.parse()

    def test_missing_image_rejected(self):
        self.example["scenes"][0]["imagePath"] = "missing.png"
        with self.assertRaisesRegex(ValueError, "image file missing"):
            self.parse()

    def test_audio_short_rejected(self):
        with self.assertRaisesRegex(ValueError, "beyond audio duration"):
            self.parse(6.0)

    def test_empty_project_rejected(self):
        self.example["scenes"] = []
        with self.assertRaisesRegex(ValueError, "at least one"):
            self.parse()

    def test_cache_fingerprint_changes_with_image_and_text(self):
        scene = self.parse()[0]
        baseline = digest_scene(scene, 640, 360, 20, 70)
        self.assertEqual(baseline, digest_scene(scene, 640, 360, 20, 70))
        scene["text"] = "new narration"
        self.assertNotEqual(baseline, digest_scene(scene, 640, 360, 20, 70))
        scene["text"] = "Illustration"
        Image.new("RGB", (100, 60), "red").save(self.image)
        self.assertNotEqual(baseline, digest_scene(scene, 640, 360, 20, 70))


if __name__ == "__main__":
    unittest.main()

