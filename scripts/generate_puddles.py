#!/usr/bin/env python3
"""Generate 16x16 puddle sprites for each substance color."""

from __future__ import annotations

import struct
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Art" / "Puddles"
SIZE = 16

COLORS = {
    "Water": (64, 168, 236, 255),
    "Oil": (168, 108, 36, 255),
    "Poison": (148, 72, 204, 255),
    "Acid": (204, 228, 48, 255),
    "Blood": (204, 44, 52, 255),
    "Lava": (236, 96, 32, 255),
    "Mercury": (176, 192, 208, 255),
}

MASK = [
    "................",
    "......XXXX......",
    "....XXXXXXXX....",
    "...XXXXXXXXXX...",
    "..XXXXXXXXXXXX..",
    "..XXXXXXXXXXXX..",
    ".XXXXXXXXXXXXXX.",
    ".XXXXXXXXXXXXXX.",
    ".XXXXXXXXXXXXXX.",
    ".XXXXXXXXXXXXXX.",
    "..XXXXXXXXXXXX..",
    "..XXXXXXXXXXXX..",
    "...XXXXXXXXXX...",
    "....XXXXXXXX....",
    "......XXXX......",
    "................",
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


def meta(guid: str) -> str:
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
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 16
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


GUIDS = {
    "Water": "a1119dd1e00000000000000000000001",
    "Oil": "a1119dd1e00000000000000000000002",
    "Poison": "a1119dd1e00000000000000000000003",
    "Acid": "a1119dd1e00000000000000000000004",
    "Blood": "a1119dd1e00000000000000000000005",
    "Lava": "a1119dd1e00000000000000000000006",
    "Mercury": "a1119dd1e00000000000000000000007",
}


def paint(color: tuple[int, int, int, int]) -> bytearray:
    pixels = bytearray(SIZE * SIZE * 4)
    for y, row in enumerate(MASK):
        for x, ch in enumerate(row):
            px = (y * SIZE + x) * 4
            if ch == "X":
                pixels[px : px + 4] = bytes(color)
            else:
                pixels[px : px + 4] = bytes((0, 0, 0, 0))
    return pixels


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT.parent / "Puddles.meta").write_text(
        """fileFormatVersion: 2
guid: a1119dd1000000000000000000000001
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    )
    for name, color in COLORS.items():
        path = OUT / f"{name}.png"
        write_png(path, SIZE, SIZE, paint(color))
        (OUT / f"{name}.png.meta").write_text(meta(GUIDS[name]))
        print(path)


if __name__ == "__main__":
    main()
