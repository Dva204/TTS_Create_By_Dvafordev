import json
import tempfile
import unittest
from pathlib import Path

from scene_plan import load_plan, make_template, discover_layers
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

    def test_layer_plan_with_transparent_png(self):
        folder = self.root / "layers" / "001"
        folder.mkdir(parents=True)
        from PIL import Image
        pic = Image.new("RGBA", (32, 18), (255, 255, 255, 0))
        pic.putpixel((5, 5), (35, 80, 135, 255))
        pic.save(folder / "01_character.png")
        self.base["scenes"][0]["layers"] = ["layers/001/01_character.png"]
        scenes = self.load()
        self.assertTrue(scenes[0]["layers"][0].endswith("01_character.png"))

    def test_layer_escape_rejected(self):
        self.base["scenes"][0]["layers"] = ["../../outside.png"]
        with self.assertRaisesRegex(ValueError, "outside"):
            self.load()

    def test_layer_must_be_png(self):
        folder = self.root / "layers" / "001"
        folder.mkdir(parents=True)
        (folder / "01_bad.jpg").write_bytes(b"not image")
        self.base["scenes"][0]["layers"] = ["layers/001/01_bad.jpg"]
        with self.assertRaisesRegex(ValueError, "invalid transparent PNG"):
            self.load()

    def test_auto_discover_natural_sort(self):
        folder = self.root / "layers" / "001"
        folder.mkdir(parents=True)
        for name in ["10_last.png", "2_second.png", "1_first.png"]:
            (folder / name).write_bytes(b"fake")
        order = [q.name for q in discover_layers(self.root / "001.png")]
        self.assertEqual(order, ["1_first.png", "2_second.png", "10_last.png"])
        template = make_template([self.root / "001.png"], 1,
                                 [{"cue_from": 1, "cue_to": 1}])
        self.assertEqual(len(template["scenes"][0]["layers"]), 3)

    def test_boundaries_non_integer_durations(self):
        cues = [(0, 1.7, "A"), (1.7, 3.3, "B"), (3.3, 6.6, "C"),
                (6.6, 8.9, "D"), (8.9, 12., "E")]
        scenes = boundaries(cues, 3, 12.)
        self.assertAlmostEqual(sum(s["duration"] for s in scenes), 12.)
        self.assertEqual(scenes[0]["cue_from"], 1)
        self.assertEqual(scenes[-1]["cue_to"], 5)


if __name__ == "__main__":
    unittest.main()

