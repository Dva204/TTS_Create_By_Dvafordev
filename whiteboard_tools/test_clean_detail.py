"""Regression tests for Whiteboard V3 raster extraction."""
import tempfile
import unittest
from pathlib import Path
from PIL import Image, ImageDraw
from whiteboard_batch import prepare_raster


class CleanDetailTests(unittest.TestCase):
    def test_dark_shape_keeps_white_interior(self):
        with tempfile.TemporaryDirectory() as temp:
            inp = Path(temp) / "input.png"
            image = Image.new("RGB", (128, 128), "white")
            ImageDraw.Draw(image).rectangle((20, 20, 105, 105), fill="black")
            image.save(inp)
            output, source = prepare_raster(inp, Path(temp) / "result",
                                            128, 128, "clean-detail")
            line = Image.open(output).convert("L")
            self.assertGreater(line.getpixel((64, 64)), 240)
            self.assertLess(line.getpixel((20, 64)), 128)
            with Image.open(source) as src:
                self.assertEqual(src.size, (128, 128))

    def test_alpha_compositing_and_letterbox(self):
        with tempfile.TemporaryDirectory() as temp:
            inp = Path(temp) / "transparent.png"
            image = Image.new("RGBA", (100, 50), (255, 0, 0, 0))
            ImageDraw.Draw(image).line((5, 25, 95, 25), fill="black", width=2)
            image.save(inp)
            output, source = prepare_raster(inp, Path(temp) / "result",
                                            200, 150, "clean-detail")
            with Image.open(output) as strokes:
                self.assertEqual(strokes.size, (200, 150))
            self.assertEqual(Image.open(source).convert("RGB").getpixel((1, 1)),
                             (255, 255, 255))


if __name__ == "__main__":
    unittest.main()
