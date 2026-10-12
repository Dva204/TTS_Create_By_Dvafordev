"""Local WebP preview bridge tests, no user files modified."""
import tempfile
import time
import unittest
from pathlib import Path

from PIL import Image
from preview_image_bridge import prepare

class PreviewBridgeTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.source = self.root / "scene.webp"
        Image.new("RGB", (1920, 1280), (125, 180, 220)).save(
            self.source, format="WEBP", quality=85
        )
        self.cache = self.root / "cache"

    def tearDown(self):
        self.temp.cleanup()

    def test_converts_webp_into_gdi_friendly_png_without_touching_source(self):
        original_bytes = self.source.read_bytes()
        result = prepare([str(self.source)], self.cache)
        output = Path(result[str(self.source)])
        self.assertTrue(output.exists())
        self.assertEqual(output.suffix, ".png")
        with Image.open(output) as preview:
            self.assertEqual(preview.format, "PNG")
            self.assertLessEqual(preview.width, 960)
            self.assertLessEqual(preview.height, 540)
        self.assertEqual(original_bytes, self.source.read_bytes())

    def test_preview_cache_reuse_when_source_unchanged(self):
        first = prepare([str(self.source)], self.cache)
        target = Path(first[str(self.source)])
        original_ns = target.stat().st_mtime_ns
        second = prepare([str(self.source)], self.cache)
        self.assertEqual(first, second)
        self.assertEqual(original_ns, target.stat().st_mtime_ns)

    def test_cache_invalidation_after_replacing_source(self):
        first = prepare([str(self.source)], self.cache)
        Image.new("RGB", (640, 480), (50, 30, 20)).save(
            self.source, format="WEBP", quality=80
        )
        second = prepare([str(self.source)], self.cache)
        self.assertNotEqual(first[str(self.source)], second[str(self.source)])
        with Image.open(second[str(self.source)]) as img:
            self.assertEqual(img.size, (640, 480))

    def test_invalid_source_skipped(self):
        self.assertEqual(prepare([str(self.root / "not_found.webp")], self.cache), {})

if __name__ == "__main__":
    unittest.main()
