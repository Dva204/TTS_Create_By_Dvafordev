"""Prepare a raster image as separate whiteboard strokes and color plate."""
import argparse
from pathlib import Path
from whiteboard_batch import prepare_raster

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("image", type=Path)
    parser.add_argument("output_dir", type=Path)
    parser.add_argument("--width", type=int, default=1280)
    parser.add_argument("--height", type=int, default=720)
    parser.add_argument("--mode", choices=("edges","detail","ink"), default="edges")
    args = parser.parse_args()
    if not args.image.is_file():
        parser.error("Image not found")
    lines, source = prepare_raster(args.image, args.output_dir,
                                   args.width, args.height, args.mode)
    print(str(lines.resolve()))
    print(str(source.resolve()))

if __name__ == "__main__":
    main()
