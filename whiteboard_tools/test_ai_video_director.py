import json
import tempfile
import unittest
from pathlib import Path
from ai_video_director import suggest_scene, suggest_project
from director_effects import validate_events


class DirectorTests(unittest.TestCase):
    def setUp(self):
        self.cues = [(0, 3, "In 1903, the first powered flight changed everything."),
                     (3, 6, "Look closer at the hidden mechanism."),
                     (6, 9, "Nothing more specific was mentioned.")]
        self.scene = {"index": 1, "cueFrom": 1, "cueTo": 2, "startMs": 0,
                      "endMs": 6000, "styleId": "image-story", "directionEvents": []}
        self.project = {"schemaVersion": 1, "scenes": [self.scene]}

    def test_number_caption_tracks_srt(self):
        suggested = suggest_scene(self.scene, self.cues)
        self.assertEqual([item["kind"] for item in suggested], ["text", "zoom"])
        self.assertEqual(suggested[0]["text"], "1903")
        self.assertEqual(suggested[0]["startMs"], 160)
        self.assertTrue(suggested[1]["startMs"] > suggested[0]["startMs"])
        validate_events(suggested, 6000)

    def test_no_spurious_effects_on_neutral_narration(self):
        neutral = {"styleId": "whiteboard", "cueFrom": 3, "cueTo": 3,
                   "startMs": 6000, "endMs": 9000}
        self.assertEqual(suggest_scene(neutral, self.cues), [])

    def test_no_double_zoom_on_image_motion(self):
        motion = dict(self.scene, styleId="image-motion")
        self.assertEqual([e["kind"] for e in suggest_scene(motion, self.cues)], ["text"])

    def test_manual_edits_preserved_by_default(self):
        self.scene["directionEvents"] = [{
            "kind": "text", "startMs": 1, "durationMs": 1000,
            "text": "Manual override", "zoom": 1.1, "focusX": .5, "focusY": .5
        }]
        plan, changed = suggest_project(self.project, self.cues)
        self.assertEqual(changed, 0)
        self.assertEqual(plan["scenes"][0]["directionEvents"][0]["text"], "Manual override")
        self.assertIsNot(plan, self.project)

    def test_missing_cues_rejected(self):
        self.scene["cueTo"] = 99
        with self.assertRaisesRegex(ValueError, "mapping"):
            suggest_project(self.project, self.cues)

    def test_text_event_cannot_exceed_scene_duration(self):
        with self.assertRaisesRegex(ValueError, "scene duration"):
            validate_events([{"kind":"text","startMs":5500,"durationMs":800,
                              "text":"ERROR", "zoom":1.1,"focusX":.5,"focusY":.5}], 6000)

    def test_zoom_too_large_rejected(self):
        with self.assertRaisesRegex(ValueError, "safe range"):
            validate_events([{"kind":"zoom","startMs":1000,"durationMs":800,
                              "text":"", "zoom":2.0,"focusX":.5,"focusY":.5}], 6000)

    def test_too_many_zooms_rejected(self):
        one={"kind":"zoom","startMs":1000,"durationMs":800,
             "text":"", "zoom":1.1,"focusX":.5,"focusY":.5}
        with self.assertRaisesRegex(ValueError, "One camera zoom"):
            validate_events([one,dict(one,startMs=2000)], 6000)

    def test_2000_year_and_percentage(self):
        scene = dict(self.scene, cueFrom=1, cueTo=1, endMs=3000)
        cues = [(0, 3, "In 2026 growth was 35%.")]
        self.assertEqual(suggest_scene(scene, cues)[0]["text"], "2026")

    def test_empty_effect_list_unchanged(self):
        self.assertEqual(validate_events([], 5000), [])


if __name__ == "__main__":
    unittest.main()
