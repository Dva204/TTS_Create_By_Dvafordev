import tempfile
import unittest
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

from extract_layers import detect_regions, extract_layers, validate_regions


class AutoLayerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.folder = Path(self.temp.name)
        self.img = Image.new("RGB", (320, 180), "white")
        d = ImageDraw.Draw(self.img)
        d.ellipse((30, 40, 80, 100), fill=(225, 170, 125), outline=(30, 30, 30), width=4)
        d.rectangle((32, 105, 80, 153), fill=(100, 160, 200), outline=(30, 30, 30), width=4)
        d.line((155, 90, 285, 90), fill=(22, 80, 120), width=5)
        d.ellipse((185, 70, 206, 90), fill=(170, 110, 50), outline=(23, 23, 23), width=3)

    def tearDown(self):
        self.temp.cleanup()

    def test_detect_two_groups(self):
        regions = detect_regions(self.img)
        self.assertEqual(len(regions), 2)

    def test_dark_ink_retains_alpha_without_overflow(self):
        regions = detect_regions(self.img)
        files = extract_layers(self.img, regions, self.folder)
        self.assertEqual(len(files), 2)
        self.assertTrue(all(p.is_file() for p in files))
        for f in files:
            with Image.open(f) as png:
                self.assertEqual(png.mode, "RGBA")
                self.assertEqual(png.size, self.img.size)
                alpha = np.asarray(png.getchannel("A"))
                self.assertGreater(np.count_nonzero(alpha > 200), 100)
                self.assertGreater(np.count_nonzero(alpha == 0), 100)

    def test_does_not_overwrite_without_permission(self):
        regions = detect_regions(self.img)
        extract_layers(self.img, regions, self.folder)
        with self.assertRaises(FileExistsError):
            extract_layers(self.img, regions, self.folder)

    def test_rejects_non_white_background(self):
        dark = Image.new("RGB", (320, 180), (20, 30, 40))
        with self.assertRaisesRegex(ValueError, "not mostly white"):
            detect_regions(dark)

    def test_rejects_invalid_region_rect(self):
        with self.assertRaisesRegex(ValueError, "outside image bounds"):
            validate_regions({"regions": [{"name":"a","box":[-1,0,50,50]}]},320,180)


if __name__ == "__main__":
    unittest.main()

