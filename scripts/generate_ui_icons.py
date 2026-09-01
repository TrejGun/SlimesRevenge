#!/usr/bin/env python3
"""Generate 64x64 placeholder UI icons for substances and status effects."""

from __future__ import annotations

import math
import struct
import uuid
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Resources" / "Icons"
SIZE = 64

SUBSTANCES = {
    "substance_water": (64, 168, 236),
    "substance_oil": (168, 108, 36),
    "substance_poison": (148, 72, 204),
    "substance_acid": (204, 228, 48),
    "substance_blood": (204, 44, 52),
    "substance_lava": (236, 96, 32),
    "substance_mercury": (176, 192, 208),
}

# name -> (circle_rgb, glyph_kind)
STATUSES = {
    "status_poisoned": ((148, 72, 204), "drop"),
    "status_corroding": ((180, 220, 48), "bubble"),
    "status_instability": ((96, 120, 160), "zigzag"),
    "status_burning": ((236, 120, 32), "flame"),
    "status_wet": ((64, 168, 236), "waves"),
    "status_fireproof": ((140, 150, 160), "shield"),
    "status_invisible": ((176, 192, 208), "ghost"),
    "status_flammable": ((168, 108, 36), "oil_spark"),
    "status_retaliation": ((204, 60, 60), "arrows"),
    "status_regeneration": ((220, 100, 140), "plus"),
    "status_vampirism": ((140, 24, 40), "fang"),
    "status_poisonous": ((120, 48, 160), "drop_x"),
    "status_hates_rats": ((200, 120, 48), "hunt"),
    "status_hates_cats": ((180, 90, 40), "hunt"),
    "status_fears_cats": ((130, 170, 110), "flee"),
    "status_fears_dogs": ((120, 160, 100), "flee"),
    "status_fears_water": ((120, 160, 200), "flee"),
    "status_digesting": ((180, 140, 80), "bubble"),
}

BLOB = [
    "........................",
    "..........XXXX..........",
    "........XXXXXXXX........",
    "......XXXXXXXXXXXX......",
    ".....XXXXXXXXXXXXXX.....",
    "....XXXXXXXXXXXXXXXX....",
    "...XXXXXXXXXXXXXXXXXX...",
    "...XXXXXXXXXXXXXXXXXX...",
    "..XXXXXXXXXXXXXXXXXXXX..",
    "..XXXXXXXXXXXXXXXXXXXX..",
    ".XXXXXXXXXXXXXXXXXXXXXX.",
    ".XXXXXXXXXXXXXXXXXXXXXX.",
    ".XXXXXXXXXXXXXXXXXXXXXX.",
    ".XXXXXXXXXXXXXXXXXXXXXX.",
    "..XXXXXXXXXXXXXXXXXXXX..",
    "..XXXXXXXXXXXXXXXXXXXX..",
    "...XXXXXXXXXXXXXXXXXX...",
    "...XXXXXXXXXXXXXXXXXX...",
    "....XXXXXXXXXXXXXXXX....",
    ".....XXXXXXXXXXXXXX.....",
    "......XXXXXXXXXXXX......",
    "........XXXXXXXX........",
    "..........XXXX..........",
    "........................",
]


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


def meta(guid: str, ppu: int = 64) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 1
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 0
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings: []
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
  spritePackingTag: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def folder_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def set_px(pixels: bytearray, x: int, y: int, rgba: tuple[int, int, int, int]) -> None:
    if x < 0 or y < 0 or x >= SIZE or y >= SIZE:
        return
    i = (y * SIZE + x) * 4
    pixels[i : i + 4] = bytes(rgba)


def lighten(rgb: tuple[int, int, int], amount: float) -> tuple[int, int, int]:
    return tuple(min(255, int(c + (255 - c) * amount)) for c in rgb)  # type: ignore


def paint_blob(rgb: tuple[int, int, int]) -> bytearray:
    pixels = bytearray(SIZE * SIZE * 4)
    # scale 24x24 mask into 64x64 centered
    mask_w = len(BLOB[0])
    mask_h = len(BLOB)
    ox = (SIZE - mask_w * 2) // 2
    oy = (SIZE - mask_h * 2) // 2
    for my, row in enumerate(BLOB):
        for mx, ch in enumerate(row):
            if ch != "X":
                continue
            for dy in range(2):
                for dx in range(2):
                    x = ox + mx * 2 + dx
                    y = oy + my * 2 + dy
                    # highlight top-left
                    factor = 0.35 if my < mask_h // 3 and mx < mask_w // 3 else 0.0
                    c = lighten(rgb, factor)
                    set_px(pixels, x, y, (*c, 255))
    return pixels


def fill_rect(pixels: bytearray, x0: int, y0: int, x1: int, y1: int, rgba: tuple[int, int, int, int]) -> None:
    for y in range(y0, y1):
        for x in range(x0, x1):
            set_px(pixels, x, y, rgba)


def fill_circle(pixels: bytearray, cx: int, cy: int, r: int, rgba: tuple[int, int, int, int]) -> None:
    r2 = r * r
    for y in range(cy - r, cy + r + 1):
        for x in range(cx - r, cx + r + 1):
            if (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r2:
                set_px(pixels, x, y, rgba)


def paint_glyph(pixels: bytearray, kind: str, color: tuple[int, int, int, int] = (255, 255, 255, 255)) -> None:
    cx, cy = 32, 32
    if kind == "drop":
        for y in range(18, 46):
            w = 2 + (y - 18) // 3 if y < 34 else 14 - (y - 34) // 2
            for x in range(cx - w, cx + w):
                set_px(pixels, x, y, color)
    elif kind == "bubble":
        fill_circle(pixels, cx, cy, 10, color)
        fill_circle(pixels, cx - 4, cy - 4, 3, (40, 40, 40, 255))
    elif kind == "zigzag":
        pts = [(18, 40), (26, 24), (34, 40), (42, 24), (50, 40)]
        for i in range(len(pts) - 1):
            x0, y0 = pts[i]
            x1, y1 = pts[i + 1]
            steps = max(abs(x1 - x0), abs(y1 - y0))
            for s in range(steps + 1):
                t = s / max(1, steps)
                x = int(x0 + (x1 - x0) * t)
                y = int(y0 + (y1 - y0) * t)
                for d in range(-1, 2):
                    set_px(pixels, x + d, y, color)
                    set_px(pixels, x, y + d, color)
    elif kind == "flame":
        for y in range(20, 48):
            for peak, ox in enumerate((-8, 0, 8)):
                h = 22 - abs(peak - 1) * 4
                yy = 48 - (y - 20)
                if yy > h:
                    continue
                w = max(1, 6 - abs(yy - h // 2) // 2)
                for x in range(cx + ox - w, cx + ox + w):
                    set_px(pixels, x, y, color)
    elif kind == "waves":
        for wave, ybase in enumerate((28, 38)):
            for x in range(18, 46):
                y = ybase + int(3 * ((x // 4) % 2))
                set_px(pixels, x, y, color)
                set_px(pixels, x, y + 1, color)
    elif kind == "shield":
        for y in range(18, 48):
            w = 14 if y < 36 else max(2, 14 - (y - 36))
            for x in range(cx - w, cx + w):
                set_px(pixels, x, y, color)
        fill_rect(pixels, cx - 10, 22, cx + 10, 40, (40, 50, 45, 255))
    elif kind == "oil_spark":
        paint_glyph(pixels, "drop", color)
        fill_circle(pixels, 44, 22, 3, (255, 220, 80, 255))
    elif kind == "arrows":
        for x in range(18, 46):
            set_px(pixels, x, cy, color)
            set_px(pixels, x, cy + 1, color)
        for d in range(6):
            set_px(pixels, 18 + d, cy - d, color)
            set_px(pixels, 18 + d, cy + d, color)
            set_px(pixels, 45 - d, cy - d, color)
            set_px(pixels, 45 - d, cy + d, color)
    elif kind == "plus":
        fill_rect(pixels, cx - 3, 20, cx + 3, 44, color)
        fill_rect(pixels, 20, cy - 3, 44, cy + 3, color)
    elif kind == "fang":
        for y in range(22, 46):
            w = max(1, 8 - (y - 22) // 3)
            for x in range(cx - w, cx + w):
                set_px(pixels, x, y, color)
    elif kind == "drop_x":
        paint_glyph(pixels, "drop", color)
        for i in range(-6, 7):
            set_px(pixels, cx + i, cy + i, (255, 200, 200, 255))
            set_px(pixels, cx + i, cy - i, (255, 200, 200, 255))
    elif kind == "hunt":
        for x in range(20, 48):
            set_px(pixels, x, cy, color)
            set_px(pixels, x, cy + 1, color)
        for d in range(7):
            set_px(pixels, 48 - d, cy - d, color)
            set_px(pixels, 48 - d, cy + d, color)
    elif kind == "flee":
        for x in range(16, 44):
            set_px(pixels, x, cy, color)
            set_px(pixels, x, cy + 1, color)
        for d in range(7):
            set_px(pixels, 16 + d, cy - d, color)
            set_px(pixels, 16 + d, cy + d, color)
    elif kind == "ghost":
        for angle in range(0, 360, 12):
            rad = math.radians(angle)
            x = int(cx + math.cos(rad) * 14)
            y = int(cy + math.sin(rad) * 14)
            if angle % 24 == 0:
                set_px(pixels, x, y, color)
                set_px(pixels, x, y + 1, color)
        fill_circle(pixels, cx, cy, 6, (*color[:3], 120))


def paint_status(rgb: tuple[int, int, int], glyph: str) -> bytearray:
    pixels = bytearray(SIZE * SIZE * 4)
    frame = (24, 32, 28, 255)
    fill = (40, 52, 40, 255)
    fill_rect(pixels, 0, 0, SIZE, SIZE, frame)
    fill_rect(pixels, 4, 4, SIZE - 4, SIZE - 4, fill)
    fill_circle(pixels, 32, 32, 22, (*rgb, 255))
    fill_circle(pixels, 32, 32, 18, (*lighten(rgb, 0.15), 255))
    paint_glyph(pixels, glyph)
    return pixels


def save(name: str, pixels: bytearray, guid: str) -> None:
    path = OUT / f"{name}.png"
    write_png(path, SIZE, SIZE, pixels)
    (OUT / f"{name}.png.meta").write_text(meta(guid))
    print(path)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    resources = ROOT / "Assets" / "Resources"
    resources.mkdir(parents=True, exist_ok=True)
    if not (resources / "Resources.meta").exists():
        # Resources folder meta lives next to Resources as Assets/Resources.meta conceptually —
        # Unity uses Assets/Resources.meta for the folder Assets/Resources
        pass
    resources_meta = ROOT / "Assets" / "Resources.meta"
    if not resources_meta.exists():
        resources_meta.write_text(folder_meta("b1119dd1000000000000000000000001"))
    (ROOT / "Assets" / "Resources" / "Icons.meta").write_text(
        folder_meta("b1119dd1000000000000000000000002")
    )

    for i, (name, rgb) in enumerate(SUBSTANCES.items()):
        guid = f"b1119dd1e00000000000000000000{i+1:03d}"
        save(name, paint_blob(rgb), guid)

    for i, (name, (rgb, glyph)) in enumerate(STATUSES.items()):
        guid = f"b1119dd1f00000000000000000000{i+1:03d}"
        save(name, paint_status(rgb, glyph), guid)


if __name__ == "__main__":
    main()
