#!/usr/bin/env python3
"""Generate 8-direction walk sheets (chicken-style) for Rat/Cat/Dog/Scorpion.

Layout: 4 frames × 8 directions = 128 × 256 px, 32×32 cells.
Directions (rows top→bottom): S, SE, E, NE, N, NW, W, SW.
"""
from __future__ import annotations

import math
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Art" / "Animals"
CELL = 32
COLS = 4
ROWS = 8  # S SE E NE N NW W SW
NONE = (0, 0, 0, 0)

# angle in degrees: 0=E, 90=N, 180=W, 270=S — we use facing vectors
DIRS = [
    (0, 1),    # S  down
    (1, 1),    # SE
    (1, 0),    # E
    (1, -1),   # NE
    (0, -1),   # N
    (-1, -1),  # NW
    (-1, 0),   # W
    (-1, 1),   # SW
]


def norm(dx: float, dy: float) -> tuple[float, float]:
    L = math.hypot(dx, dy) or 1.0
    return dx / L, dy / L


def put(px: dict[tuple[int, int], tuple], x: int, y: int, c: tuple) -> None:
    if 0 <= x < CELL and 0 <= y < CELL and c[3] > 0:
        px[x, y] = c


def disk(px, cx, cy, r, fill, outline=None) -> None:
    r2 = r * r
    for y in range(int(cy - r) - 1, int(cy + r) + 2):
        for x in range(int(cx - r) - 1, int(cx + r) + 2):
            d2 = (x - cx) ** 2 + (y - cy) ** 2
            if d2 <= r2 * 0.55:
                put(px, x, y, fill[0] if isinstance(fill, tuple) and not isinstance(fill[0], int) else fill)
            elif outline and d2 <= (r + 0.7) ** 2:
                put(px, x, y, outline)


def disk2(px, cx, cy, r, body, light, shadow, outline) -> None:
    r2 = r * r
    for y in range(int(cy - r) - 2, int(cy + r) + 3):
        for x in range(int(cx - r) - 2, int(cx + r) + 3):
            dx, dy = x - cx, y - cy
            d2 = dx * dx + dy * dy
            if d2 > (r + 0.85) ** 2:
                continue
            if d2 > r2:
                put(px, x, y, outline)
                continue
            # light from top-left
            shade = dx * -0.4 + dy * -0.6
            if shade < -r * 0.15:
                put(px, x, y, light)
            elif shade > r * 0.25:
                put(px, x, y, shadow)
            else:
                put(px, x, y, body)


def draw_rat(fx: float, fy: float, phase: int) -> Image.Image:
    outline = (56, 24, 16, 255)
    body = (196, 100, 48, 255)
    light = (236, 168, 96, 255)
    shadow = (140, 64, 32, 255)
    ear = (240, 140, 148, 255)
    nose = (232, 72, 80, 255)
    eye = (24, 16, 12, 255)
    px: dict = {}
    bob = (phase % 2) - 0
    cx, cy = 15.5, 17.5 + (1 if phase in (1, 3) else 0)
    disk2(px, cx, cy, 7.2, body, light, shadow, outline)
    # head
    hx, hy = cx + fx * 5.5, cy + fy * 5.5 - 1
    disk2(px, hx, hy, 4.2, body, light, shadow, outline)
    # ears
    px_l = (-fy, fx)
    put(px, int(hx + px_l[0] * 3 - fx), int(hy + px_l[1] * 3 - fy - 2), ear)
    put(px, int(hx - px_l[0] * 3 - fx), int(hy - px_l[1] * 3 - fy - 2), ear)
    put(px, int(hx + fx * 3.5), int(hy + fy * 3.5), nose)
    put(px, int(hx + px_l[0] * 1.5 + fx), int(hy + px_l[1] * 1.5 + fy), eye)
    put(px, int(hx - px_l[0] * 1.5 + fx), int(hy - px_l[1] * 1.5 + fy), eye)
    # tail opposite facing
    for i in range(5):
        put(px, int(cx - fx * (6 + i) + px_l[0] * (phase - 1.5)), int(cy - fy * (6 + i)), shadow)
    return pixels_to_image(px)


def draw_cat(fx: float, fy: float, phase: int) -> Image.Image:
    outline = (40, 40, 48, 255)
    body = (180, 180, 190, 255)
    light = (230, 230, 235, 255)
    shadow = (110, 110, 120, 255)
    ear = (240, 160, 170, 255)
    eye = (60, 180, 80, 255)
    nose = (220, 100, 120, 255)
    px: dict = {}
    cx, cy = 15.5, 18.0 + (1 if phase in (1, 3) else 0)
    disk2(px, cx, cy, 7.5, body, light, shadow, outline)
    hx, hy = cx + fx * 5.0, cy + fy * 5.0 - 1.5
    disk2(px, hx, hy, 4.5, body, light, shadow, outline)
    perp = (-fy, fx)
    # pointy ears
    for s in (-1, 1):
        ex = int(hx + perp[0] * 3 * s - fx * 2)
        ey = int(hy + perp[1] * 3 * s - fy * 2 - 3)
        put(px, ex, ey, outline)
        put(px, ex, ey + 1, ear)
        put(px, ex + int(perp[0] * s), ey + 1, body)
    put(px, int(hx + fx * 3), int(hy + fy * 3), nose)
    put(px, int(hx + perp[0] * 1.8 + fx * 0.5), int(hy + perp[1] * 1.8 + fy * 0.5), eye)
    put(px, int(hx - perp[0] * 1.8 + fx * 0.5), int(hy - perp[1] * 1.8 + fy * 0.5), eye)
    # tail curl
    for i in range(6):
        put(
            px,
            int(cx - fx * (5 + i * 0.7) + perp[0] * math.sin(i * 0.8 + phase) * 2),
            int(cy - fy * (5 + i * 0.7) + perp[1] * math.sin(i * 0.8 + phase) * 2 - 2),
            shadow,
        )
    return pixels_to_image(px)


def draw_dog(fx: float, fy: float, phase: int) -> Image.Image:
    outline = (60, 36, 20, 255)
    body = (168, 112, 64, 255)
    light = (210, 160, 100, 255)
    shadow = (110, 70, 40, 255)
    ear = (90, 55, 35, 255)
    eye = (20, 16, 12, 255)
    nose = (30, 24, 20, 255)
    px: dict = {}
    cx, cy = 15.5, 18.0 + (1 if phase in (1, 3) else 0)
    disk2(px, cx, cy, 8.0, body, light, shadow, outline)
    hx, hy = cx + fx * 6.0, cy + fy * 6.0 - 1
    disk2(px, hx, hy, 4.8, body, light, shadow, outline)
    # snout
    disk2(px, hx + fx * 3, hy + fy * 3, 2.4, light, light, shadow, outline)
    put(px, int(hx + fx * 4.5), int(hy + fy * 4.5), nose)
    perp = (-fy, fx)
    # floppy ears
    for s in (-1, 1):
        for k in range(3):
            put(
                px,
                int(hx + perp[0] * (2 + k) * s - fx),
                int(hy + perp[1] * (2 + k) * s + 1),
                ear,
            )
    put(px, int(hx + perp[0] * 1.5 + fx), int(hy + perp[1] * 1.5 + fy), eye)
    put(px, int(hx - perp[0] * 1.5 + fx), int(hy - perp[1] * 1.5 + fy), eye)
    # legs bob
    for s in (-1, 1):
        leg = 2 if (phase + (0 if s < 0 else 1)) % 2 == 0 else 0
        put(px, int(cx + perp[0] * 3 * s + fx), int(cy + 6 + leg), outline)
    return pixels_to_image(px)


def draw_scorpion(fx: float, fy: float, phase: int) -> Image.Image:
    outline = (40, 20, 12, 255)
    body = (160, 72, 36, 255)
    light = (200, 110, 50, 255)
    shadow = (100, 40, 20, 255)
    claw = (180, 90, 40, 255)
    sting = (220, 40, 40, 255)
    px: dict = {}
    cx, cy = 15.5, 17.0 + (0.5 if phase % 2 else 0)
    # segmented body along facing
    for i in range(5):
        disk2(px, cx + fx * (i - 1) * 2.2, cy + fy * (i - 1) * 2.2, 3.2 - i * 0.25, body, light, shadow, outline)
    # claws
    perp = (-fy, fx)
    for s in (-1, 1):
        bx = cx + perp[0] * 5 * s + fx * 2
        by = cy + perp[1] * 5 * s + fy * 2
        disk2(px, bx, by, 2.2, claw, light, shadow, outline)
        put(px, int(bx + fx * 2 + perp[0] * s), int(by + fy * 2 + perp[1] * s), outline)
    # tail arch over body opposite-ish then sting forward-up
    for i in range(6):
        t = i / 5.0
        tx = cx - fx * (2 + t * 3) + perp[0] * math.sin(t * math.pi) * (2 + phase % 2)
        ty = cy - fy * (2 + t * 3) - 4 * math.sin(t * math.pi)
        put(px, int(tx), int(ty), shadow if i < 4 else sting)
        put(px, int(tx), int(ty) - 1, outline if i < 4 else sting)
    put(px, int(cx + fx * 3), int(cy + fy * 3 - 1), (20, 12, 8, 255))  # eye
    return pixels_to_image(px)


def pixels_to_image(px: dict) -> Image.Image:
    im = Image.new("RGBA", (CELL, CELL), NONE)
    for (x, y), c in px.items():
        if 0 <= x < CELL and 0 <= y < CELL:
            im.putpixel((x, y), c)
    return im


def build_sheet(drawer) -> Image.Image:
    sheet = Image.new("RGBA", (COLS * CELL, ROWS * CELL), (0, 0, 0, 255))
    for row, (dx, dy) in enumerate(DIRS):
        fx, fy = norm(dx, dy)
        for col in range(COLS):
            frame = drawer(fx, fy, col)
            sheet.paste(frame, (col * CELL, row * CELL))
    return sheet


def write_meta(png: Path, guid: str) -> None:
    sprites = []
    name_table = []
    internal = 100000
    for row in range(ROWS):
        for col in range(COLS):
            name = f"{png.stem}_{row}_{col}"
            # Unity rect y from bottom
            x = col * CELL
            y = (ROWS - 1 - row) * CELL
            sprites.append(
                f"""    - serializedVersion: 2
      name: {name}
      rect:
        serializedVersion: 2
        x: {x}
        y: {y}
        width: {CELL}
        height: {CELL}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: {guid[:16]}{row:02d}{col:02d}aaaa
      internalID: {internal + row * COLS + col}
      vertices: []
      indices: 
      edges: []
      weights: []"""
            )
            name_table.append(f"      {name}: {internal + row * COLS + col}")
    meta = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 12
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
  isReadable: 0
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
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 32
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
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
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
{chr(10).join(sprites)}
    outline: []
    physicsShape: []
    bones: []
    spriteID: {guid}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable:
{chr(10).join(name_table)}
  spritePackingTag: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    png.with_suffix(".png.meta").write_text(meta)


def main() -> None:
    import uuid

    OUT.mkdir(parents=True, exist_ok=True)
    animals = {
        "Rat": draw_rat,
        "Cat": draw_cat,
        "Dog": draw_dog,
        "Scorpion": draw_scorpion,
    }
    for name, drawer in animals.items():
        path = OUT / f"{name}.png"
        build_sheet(drawer).save(path)
        write_meta(path, uuid.uuid4().hex)
        print("wrote", path)


if __name__ == "__main__":
    main()
