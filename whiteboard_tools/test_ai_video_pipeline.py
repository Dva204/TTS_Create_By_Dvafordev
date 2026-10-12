"""Unit tests for the multi-engine project renderer. No GPU rendering required."""
import json
import tempfile
import unittest
from pathlib import Path

from PIL import Image
from ai_video_pipeline import CURRENT_VERSION, SUPPORTED, load_project, digest_scene, scene_frame_count


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

    def test_direction_events_loaded_and_cache_invalidated(self):
        scene = self.example["scenes"][0]
        scene["directionEvents"] = [
            {"kind": "text", "startMs": 150, "durationMs": 1100,
             "text": "1903", "zoom": 1.1, "focusX": .5, "focusY": .5}
        ]
        parsed = self.parse()[0]
        self.assertEqual(parsed["directionEvents"][0]["text"], "1903")
        base = digest_scene(parsed, 640, 360, 20, 70)
        parsed["directionEvents"][0]["text"] = "1904"
        self.assertNotEqual(base, digest_scene(parsed, 640, 360, 20, 70))

    def test_invalid_direction_time_rejected(self):
        self.example["scenes"][0]["directionEvents"] = [
            {"kind": "text", "startMs": 3200, "durationMs": 1000,
             "text": "Wrong", "zoom": 1.1, "focusX": .5, "focusY": .5}
        ]
        with self.assertRaisesRegex(ValueError, "scene duration"):
            self.parse()

    def test_invalid_zoom_focus_rejected(self):
        self.example["scenes"][0]["directionEvents"] = [
            {"kind": "zoom", "startMs": 800, "durationMs": 1600,
             "text": "", "zoom": 1.12, "focusX": 2.5, "focusY": .5}
        ]
        with self.assertRaisesRegex(ValueError, "safe range"):
            self.parse()

    def test_cumulative_frame_rounding_prevents_drift(self):
        scenes = [{"start": i * .133, "end": (i+1) * .133}
                  for i in range(100)]
        self.assertEqual(sum(scene_frame_count(s, 24) for s in scenes),
                         round(13.3 * 24))

    def test_transition_changes_cache(self):
        scene = self.parse()[0]
        initial = digest_scene(scene, 640, 360, 20, 70)
        scene["transition"] = "cut"
        self.assertNotEqual(initial, digest_scene(scene, 640, 360, 20, 70))

    def test_preview_selected_scene_ignores_unfinished_other_scene(self):
        self.example["scenes"][0]["styleId"] = "map"
        self.example["scenes"][0]["imagePath"] = "unassigned.png"
        self.plan.write_text(json.dumps(self.example), encoding="utf-8")
        scenes = load_project(self.plan, 7.0, scene_index=2)[1]
        self.assertEqual(scenes[1]["style"], "text")
        with self.assertRaisesRegex(ValueError, "no renderer yet"):
            load_project(self.plan, 7.0)

    def test_unknown_transition_rejected(self):
        self.example["scenes"][1]["transition"] = "unknown"
        with self.assertRaisesRegex(ValueError, "unsupported transition"):
            self.parse()

    def test_whiteboard_quality_defaults_are_backward_compatible(self):
        self.example["scenes"][0]["styleId"] = "whiteboard"
        scene = self.parse()[0]
        self.assertEqual(self.example.get("whiteboardRenderScale", 1.0), 1.0)
        self.assertEqual(scene["style"], "whiteboard")

    def test_reject_invalid_quality_scales_and_sharpen(self):
        for value in (-1, 0, 2.1, "not-a-number", float("nan")):
            self.example["whiteboardRenderScale"] = value
            with self.assertRaisesRegex(ValueError, "whiteboardRenderScale"):
                self.parse()
        self.example["whiteboardRenderScale"] = 1.5
        for amount in (-.1, .51, float("inf")):
            self.example["whiteboardSharpenAmount"] = amount
            with self.assertRaisesRegex(ValueError, "whiteboardSharpenAmount"):
                self.parse()

    def test_native_hd_scene_changes_cache_fingerprint(self):
        self.example["scenes"][0]["styleId"] = "whiteboard"
        scene = self.parse()[0]
        scene["whiteboardScale"] = 1.0
        ordinary = digest_scene(scene, 1280, 720, 24, 85,
                                final_size=(1280, 720))
        scene["whiteboardScale"] = 1.5
        scene["whiteboardSharpen"] = .15
        native = digest_scene(scene, 1920, 1080, 24, 85,
                              final_size=(1920, 1080))
        self.assertNotEqual(ordinary, native)
        scene["whiteboardSharpen"] = .3
        self.assertNotEqual(native, digest_scene(
            scene, 1920, 1080, 24, 85, final_size=(1920, 1080)))

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

