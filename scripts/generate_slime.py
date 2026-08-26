#!/usr/bin/env python3
"""16x16 top-down slime in the same bright RPG pixel style as the terrain."""

from __future__ import annotations

import struct
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Art" / "Characters" / "Slime.png"
SIZE = 16

NONE = (0, 0, 0, 0)
OUTLINE = (36, 88, 156, 255)
BODY = (92, 186, 230, 255)
LIGHT = (156, 220, 244, 255)
SHINE = (236, 248, 255, 255)
SHADOW = (56, 132, 196, 255)
EYE = (28, 36, 64, 255)
EYE_SHINE = (255, 255, 255, 255)
MOUTH = (40, 96, 168, 255)


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
    # Rows are top-to-bottom. '.' empty, o outline, b body, l light, s shadow, h shine, e eye, w eye-shine, m mouth
    rows = [
        "................",
        ".....oooooo.....",
        "...ooobbbbloo...",
        "..oobblllllbbo..",
        ".oobbllhhlllboo.",
        ".oobllhhhhlllbo.",
        ".obllhheehhllbo.",
        ".obllheewwehlso.",
        ".obbllheeehllbo.",
        ".oobllhhhhllsso.",
        ".ooblllmmmllsso.",
        "..oobblllllbso..",
        "...oosbbbbsoo...",
        ".....oooooo.....",
        "................",
        "................",
    ]
    colors = {
        ".": NONE,
        "o": OUTLINE,
        "b": BODY,
        "l": LIGHT,
        "s": SHADOW,
        "h": SHINE,
        "e": EYE,
        "w": EYE_SHINE,
        "m": MOUTH,
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
