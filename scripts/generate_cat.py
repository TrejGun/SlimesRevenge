#!/usr/bin/env python3
"""16x16 top-down ginger cat in the same bright RPG pixel style as the rat."""

from __future__ import annotations

import struct
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Art" / "Characters" / "Cat.png"
SIZE = 16

NONE = (0, 0, 0, 0)
OUTLINE = (32, 28, 36, 255)
BODY = (232, 140, 48, 255)
LIGHT = (252, 196, 108, 255)
SHADOW = (176, 88, 28, 255)
EAR = (236, 156, 164, 255)
NOSE = (232, 96, 120, 255)
EYE = (48, 196, 96, 255)
EYE_SHINE = (255, 255, 255, 255)
TAIL = (200, 112, 40, 255)


def write_png(path: Path, width: int, height: int, pixels: bytearray) -> None:
    def chunk(tag: bytes, data: bytes) -> bytes:
        return struct.pack(">I", len(data)) + tag + data + struct.pack(
            ">I", zlib.crc32(tag + data) & 0xFFFFFFFF
        )

    raw = b""
    stride = width * 4
    for y in range(height):
        raw += b"\x00" + bytes(pixels[y * stride : (y + 1) * stride])
    ihdr = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    path.write_bytes(
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", ihdr)
        + chunk(b"IDAT", zlib.compress(raw, 9))
        + chunk(b"IEND", b"")
    )


def main() -> None:
    # . empty, o outline, b body, l light, s shadow, e ear, n nose, y eye, w shine, t tail
    rows = [
        "....e....e.tt...",
        "...eoe..eoe.t...",
        "..eooeeoooe.t...",
        ".oobbbbbbbboo...",
        ".oobllyyllybo...",
        ".ooblwyywllbo...",
        ".oobllllnllbo...",
        ".oobllllllllso..",
        ".ooblllllllsso..",
        "..oobbbbbblso...",
        "...oosbbbsoo....",
        "....oooooo......",
        "................",
        "................",
        "................",
        "................",
    ]
    colors = {
        ".": NONE,
        "o": OUTLINE,
        "b": BODY,
        "l": LIGHT,
        "s": SHADOW,
        "e": EAR,
        "n": NOSE,
        "y": EYE,
        "w": EYE_SHINE,
        "t": TAIL,
    }
    pixels = bytearray()
    for row in rows:
        if len(row) != SIZE:
            raise RuntimeError(row)
        for ch in row:
            pixels.extend(colors[ch])
    OUT.parent.mkdir(parents=True, exist_ok=True)
    write_png(OUT, SIZE, SIZE, pixels)
    print(OUT)


if __name__ == "__main__":
    main()
