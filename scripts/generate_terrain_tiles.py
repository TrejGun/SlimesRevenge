#!/usr/bin/env python3
"""Paint 16x16 Fantasy Dreamland-style terrain with the 47-brush blob technique.

Brushes (painted once): fill, 4 edges, 4 outer corners, 4 inner corners.
Those 13 stamps compose the 47 neighbor cases of an 8-way bitmask.
"""

from __future__ import annotations

import itertools
import struct
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Assets" / "Art" / "Tiles"
SIZE = 16
COLS = 12
ROWS = 4


def valid_masks() -> list[int]:
    masks: list[int] = []
    for n, e, s, w in itertools.product((0, 1), repeat=4):
        ne_opts = (0, 1) if n and e else (0,)
        nw_opts = (0, 1) if n and w else (0,)
        se_opts = (0, 1) if s and e else (0,)
        sw_opts = (0, 1) if s and w else (0,)
        for ne, nw, se, sw in itertools.product(ne_opts, nw_opts, se_opts, sw_opts):
            mask = 0
            if nw:
                mask |= 1
            if n:
                mask |= 2
            if ne:
                mask |= 4
            if w:
                mask |= 8
            if e:
                mask |= 16
            if sw:
                mask |= 32
            if s:
                mask |= 64
            if se:
                mask |= 128
            masks.append(mask)
    masks.sort()
    if len(masks) != 47:
        raise RuntimeError(f"expected 47 masks, got {len(masks)}")
    return masks


MASKS = valid_masks()


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


class Palette(dict):
    def rgb(self, key: str) -> tuple[int, int, int]:
        return self[key]


GRASS = Palette(
    shadow=(36, 92, 44),
    dark=(52, 128, 56),
    mid=(78, 168, 74),
    light=(118, 200, 98),
    highlight=(168, 224, 128),
    outline=(28, 72, 36),
    rim=(196, 236, 150),
    flower_a=(240, 208, 72),
    flower_b=(236, 126, 154),
    flower_c=(248, 248, 228),
)

DIRT = Palette(
    shadow=(78, 46, 28),
    dark=(116, 70, 38),
    mid=(164, 106, 56),
    light=(196, 142, 82),
    highlight=(222, 180, 120),
    outline=(62, 36, 22),
    rim=(232, 196, 140),
    flower_a=(148, 140, 128),
    flower_b=(184, 176, 160),
    flower_c=(210, 198, 170),
)

SAND = Palette(
    shadow=(168, 132, 68),
    dark=(204, 168, 84),
    mid=(230, 200, 118),
    light=(244, 222, 154),
    highlight=(252, 240, 196),
    outline=(148, 112, 54),
    rim=(255, 248, 214),
    flower_a=(188, 148, 78),
    flower_b=(216, 184, 96),
    flower_c=(240, 214, 140),
)

SHEETS = {
    "Grass": ("a111711e6a5500000000000000000001", "b111711e6a5500000000000000000001", GRASS, "grass"),
    "Dirt": ("a111711ed14700000000000000000001", "b111711ed14700000000000000000001", DIRT, "dirt"),
    "Sand": ("a111711e5a7d00000000000000000001", "b111711e5a7d00000000000000000001", SAND, "sand"),
}

BLOB_SCRIPT = "f111b10b000000000000000000000001"


def put(buf: list[list[tuple[int, int, int]]], x: int, y: int, color: tuple[int, int, int]) -> None:
    if 0 <= x < SIZE and 0 <= y < SIZE:
        buf[y][x] = color


def paint_fill(kind: str, pal: Palette) -> list[list[tuple[int, int, int]]]:
    buf = [[pal.rgb("mid") for _ in range(SIZE)] for _ in range(SIZE)]
    for y in range(SIZE):
        for x in range(SIZE):
            n = (x * 3 + y * 5) % 8
            if n in (0, 4):
                put(buf, x, y, pal.rgb("dark"))
            elif n in (2, 6):
                put(buf, x, y, pal.rgb("light"))
            if (x + y * 2) % 11 == 0:
                put(buf, x, y, pal.rgb("shadow"))
            if (x * 5 + y) % 13 == 3:
                put(buf, x, y, pal.rgb("highlight"))

    if kind == "grass":
        for x, y, key in (
            (3, 4, "flower_a"),
            (4, 4, "flower_c"),
            (10, 2, "flower_b"),
            (11, 2, "flower_c"),
            (7, 11, "flower_a"),
            (13, 9, "flower_b"),
            (2, 13, "flower_a"),
        ):
            put(buf, x, y, pal.rgb(key))
        for x, y in ((5, 7), (12, 12), (8, 1), (1, 8)):
            put(buf, x, y, pal.rgb("dark"))
    elif kind == "dirt":
        for x, y, key in (
            (4, 5, "flower_a"),
            (5, 5, "flower_b"),
            (11, 3, "flower_c"),
            (9, 12, "flower_a"),
            (2, 10, "flower_b"),
            (13, 8, "flower_c"),
        ):
            put(buf, x, y, pal.rgb(key))
    else:
        for x, y in ((2, 3), (8, 6), (13, 11), (5, 12), (10, 1)):
            put(buf, x, y, pal.rgb("shadow"))
        for x, y in ((6, 2), (12, 7), (3, 9), (9, 14)):
            put(buf, x, y, pal.rgb("highlight"))
    return buf


def apply_edge_n(buf, pal):
    for x in range(SIZE):
        put(buf, x, 0, pal.rgb("outline"))
        put(buf, x, 1, pal.rgb("rim"))


def apply_edge_s(buf, pal):
    for x in range(SIZE):
        put(buf, x, 15, pal.rgb("outline"))
        put(buf, x, 14, pal.rgb("shadow"))


def apply_edge_w(buf, pal):
    for y in range(SIZE):
        put(buf, 0, y, pal.rgb("outline"))
        if 1 <= y <= 14:
            put(buf, 1, y, pal.rgb("rim"))


def apply_edge_e(buf, pal):
    for y in range(SIZE):
        put(buf, 15, y, pal.rgb("outline"))
        if 1 <= y <= 14:
            put(buf, 14, y, pal.rgb("shadow"))


def apply_outer_nw(buf, pal):
    put(buf, 0, 0, pal.rgb("outline"))
    put(buf, 1, 0, pal.rgb("outline"))
    put(buf, 0, 1, pal.rgb("outline"))
    put(buf, 1, 1, pal.rgb("rim"))
    put(buf, 2, 1, pal.rgb("rim"))
    put(buf, 1, 2, pal.rgb("rim"))


def apply_outer_ne(buf, pal):
    put(buf, 15, 0, pal.rgb("outline"))
    put(buf, 14, 0, pal.rgb("outline"))
    put(buf, 15, 1, pal.rgb("outline"))
    put(buf, 14, 1, pal.rgb("rim"))
    put(buf, 13, 1, pal.rgb("rim"))
    put(buf, 14, 2, pal.rgb("shadow"))


def apply_outer_sw(buf, pal):
    put(buf, 0, 15, pal.rgb("outline"))
    put(buf, 1, 15, pal.rgb("outline"))
    put(buf, 0, 14, pal.rgb("outline"))
    put(buf, 1, 14, pal.rgb("shadow"))
    put(buf, 2, 14, pal.rgb("shadow"))
    put(buf, 1, 13, pal.rgb("shadow"))


def apply_outer_se(buf, pal):
    put(buf, 15, 15, pal.rgb("outline"))
    put(buf, 14, 15, pal.rgb("outline"))
    put(buf, 15, 14, pal.rgb("outline"))
    put(buf, 14, 14, pal.rgb("shadow"))
    put(buf, 13, 14, pal.rgb("shadow"))
    put(buf, 14, 13, pal.rgb("shadow"))


def apply_inner_nw(buf, pal):
    put(buf, 0, 0, pal.rgb("outline"))
    put(buf, 1, 0, pal.rgb("shadow"))
    put(buf, 0, 1, pal.rgb("shadow"))


def apply_inner_ne(buf, pal):
    put(buf, 15, 0, pal.rgb("outline"))
    put(buf, 14, 0, pal.rgb("shadow"))
    put(buf, 15, 1, pal.rgb("shadow"))


def apply_inner_sw(buf, pal):
    put(buf, 0, 15, pal.rgb("outline"))
    put(buf, 1, 15, pal.rgb("shadow"))
    put(buf, 0, 14, pal.rgb("shadow"))


def apply_inner_se(buf, pal):
    put(buf, 15, 15, pal.rgb("outline"))
    put(buf, 14, 15, pal.rgb("shadow"))
    put(buf, 15, 14, pal.rgb("shadow"))


def compose(kind: str, pal: Palette, mask: int) -> list[list[tuple[int, int, int]]]:
    n = bool(mask & 2)
    e = bool(mask & 16)
    s = bool(mask & 64)
    w = bool(mask & 8)
    ne = bool(mask & 4)
    nw = bool(mask & 1)
    se = bool(mask & 128)
    sw = bool(mask & 32)
    buf = paint_fill(kind, pal)
    if not n:
        apply_edge_n(buf, pal)
    if not e:
        apply_edge_e(buf, pal)
    if not s:
        apply_edge_s(buf, pal)
    if not w:
        apply_edge_w(buf, pal)
    if not n and not w:
        apply_outer_nw(buf, pal)
    if not n and not e:
        apply_outer_ne(buf, pal)
    if not s and not w:
        apply_outer_sw(buf, pal)
    if not s and not e:
        apply_outer_se(buf, pal)
    if n and w and not nw:
        apply_inner_nw(buf, pal)
    if n and e and not ne:
        apply_inner_ne(buf, pal)
    if s and w and not sw:
        apply_inner_sw(buf, pal)
    if s and e and not se:
        apply_inner_se(buf, pal)
    return buf


def flatten(buf: list[list[tuple[int, int, int]]]) -> bytearray:
    out = bytearray()
    for row in buf:
        for r, g, b in row:
            out.extend((r, g, b, 255))
    return out


def sprite_meta(name: str, guid: str) -> str:
    sprites = []
    table = []
    for i, _mask in enumerate(MASKS):
        col = i % COLS
        row = i // COLS
        unity_y = (ROWS * SIZE) - (row + 1) * SIZE
        sprite_id = f"{i:02x}{guid[2:32]}"
        file_id = 21300000 + i
        sprite_name = f"{name}_{i:02d}"
        sprites.append(
            f"""    - serializedVersion: 2
      name: {sprite_name}
      rect:
        serializedVersion: 2
        x: {col * SIZE}
        y: {unity_y}
        width: {SIZE}
        height: {SIZE}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: {sprite_id}
      internalID: {file_id}
      vertices: []
      indices: 
      edges: []
      weights: []"""
        )
        table.append(f"      {sprite_name}: {file_id}")
    sprite_block = "\n".join(sprites)
    table_block = "\n".join(table)
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 11
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
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 256
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
  spriteMode: 2
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
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 256
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
{sprite_block}
    outline: []
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable:
{table_block}
  spritePackingTag: 
  pSDRemoveMatte: 0
  pSDShowRemoveMatteOption: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def tile_asset(name: str, sheet_guid: str, tile_guid: str) -> str:
    refs = "\n".join(
        f"  - {{fileID: {21300000 + i}, guid: {sheet_guid}, type: 3}}" for i in range(47)
    )
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {BLOB_SCRIPT}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier: 
  sprites:
{refs}
"""


def tile_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def main() -> None:
    ART.mkdir(parents=True, exist_ok=True)
    sheet_w, sheet_h = COLS * SIZE, ROWS * SIZE
    for name, (sheet_guid, tile_guid, pal, kind) in SHEETS.items():
        pixels = bytearray(sheet_w * sheet_h * 4)
        for i, mask in enumerate(MASKS):
            col, row = i % COLS, i // COLS
            tile = compose(kind, pal, mask)
            for y in range(SIZE):
                for x in range(SIZE):
                    r, g, b = tile[y][x]
                    px = ((row * SIZE + y) * sheet_w + col * SIZE + x) * 4
                    pixels[px : px + 4] = bytes((r, g, b, 255))
        png = ART / f"{name}.png"
        write_png(png, sheet_w, sheet_h, pixels)
        (ART / f"{name}.png.meta").write_text(sprite_meta(name, sheet_guid))
        (ART / f"{name}.asset").write_text(tile_asset(name, sheet_guid, tile_guid))
        (ART / f"{name}.asset.meta").write_text(tile_meta(tile_guid))
        print(f"{name}: {png} 47 brushes")


if __name__ == "__main__":
    main()
