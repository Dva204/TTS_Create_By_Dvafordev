import json
import tempfile
import unittest
from pathlib import Path

from scene_plan import load_plan, make_template
from whiteboard_batch import boundaries, parse_srt


class ScenePlanTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        for i in range(1, 4):
            (self.root / f"{i:03d}.png").write_bytes(b"dummy")
        self.cues = [(0, 2, "A"), (2, 4, "B"), (4, 6, "C"),
                     (6, 8, "D"), (8, 10, "E"), (10, 12, "F")]
        groups = boundaries(self.cues, 3, 12.0)
        self.base = make_template(
            [self.root / f"{i:03d}.png" for i in range(1, 4)],
            len(self.cues), groups)
        self.path = self.root / "plan.json"

    def tearDown(self):
        self.tmp.cleanup()

    def load(self):
        self.path.write_text(json.dumps(self.base), encoding="utf-8")
        return load_plan(self.path, self.root, len(self.cues),
                         self.cues, 12.0, {".png", ".svg"})

    def test_default_plan(self):
        scenes = self.load()
        self.assertEqual([s["duration"] for s in scenes], [4.0] * 3)
        self.assertEqual([s["cue_to"] for s in scenes], [2, 4, 6])

    def test_custom_ranges(self):
        specs = self.base["scenes"]
        specs[0]["cue_to"] = 1
        specs[1]["cue_from"] = 2
        specs[1]["cue_to"] = 5
        specs[2]["cue_from"] = 6
        specs[0]["draw_blocks"] = 3
        scenes = self.load()
        self.assertEqual([s["duration"] for s in scenes], [2., 8., 2.])
        self.assertEqual(scenes[0]["draw_blocks"], 3)

    def test_gap_rejected(self):
        self.base["scenes"][1]["cue_from"] = 4
        with self.assertRaisesRegex(ValueError, "range"):
            self.load()

    def test_path_traversal_rejected(self):
        self.base["scenes"][0]["image"] = "../escape.png"
        with self.assertRaisesRegex(ValueError, "outside"):
            self.load()

    def test_duplicate_block_id_rejected(self):
        self.base["scenes"][0]["block_sequence"] = [1, 1]
        with self.assertRaisesRegex(ValueError, "unique"):
            self.load()

    def test_missing_cover_rejected(self):
        self.base["scenes"].pop()
        with self.assertRaisesRegex(ValueError, "ends at cue"):
            self.load()

    def test_boundaries_non_integer_durations(self):
        cues = [(0, 1.7, "A"), (1.7, 3.3, "B"), (3.3, 6.6, "C"),
                (6.6, 8.9, "D"), (8.9, 12., "E")]
        scenes = boundaries(cues, 3, 12.)
        self.assertAlmostEqual(sum(s["duration"] for s in scenes), 12.)
        self.assertEqual(scenes[0]["cue_from"], 1)
        self.assertEqual(scenes[-1]["cue_to"], 5)


if __name__ == "__main__":
    unittest.main()

