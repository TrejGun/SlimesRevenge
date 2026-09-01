#!/usr/bin/env python3
"""Recolor FDR slime sheets by remapping the fixed 6-stop body ramp.

Fantasy Dreamland Reborn FDR_Enemy_01_{A,B,C,D}.png share the exact same
pixels — only a 6-color body palette changes (plus untouched white / black /
label alphas). Artist variants are NOT a single HSV rotate: luminances differ.
This tool treats green A as the structural template, ranks body colors by
luminance, and builds a new ramp anchored on a target key color while keeping
relative V and S ratios between stops (so center highlights and edge shadows
stay in the same roles).

Examples:
  python3 scripts/recolor_slime.py --hex '#A86C24' -o scripts/out/oil.png
  python3 scripts/recolor_slime.py --preset oil -o scripts/out/oil.png
  python3 scripts/recolor_slime.py --preset blood -o scripts/out/blood.png
  python3 scripts/recolor_slime.py --preset lava -o scripts/out/lava.png
  python3 scripts/recolor_slime.py --preset mercury -o scripts/out/mercury.png
  python3 scripts/recolor_slime.py --preset acid -o scripts/out/acid_yellowish.png
  python3 scripts/recolor_slime.py --dump-palette
"""

from __future__ import annotations

import argparse
import colorsys
import sys
from collections import Counter
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_TEMPLATE = (
    ROOT
    / "Assets/Art/Slimes/Poison.png"
)

# Game substance Color32 values (RGB only) — convenient presets.
PRESETS: dict[str, tuple[int, int, int]] = {
    # Raw game Color32 midtones.
    "water": (64, 168, 236),
    "oil_ui": (168, 108, 36),  # HUD brown — muddy for body art
    "poison": (148, 72, 204),
    "acid": (204, 228, 48),
    "blood": (204, 44, 52),
    "lava_ui": (236, 96, 32),  # HUD orange — too orange for body art
    # Art-tuned body midtones (Aug 2026 pass).
    "oil": (224, 183, 18),       # yellow → slight orange
    "lava": (199, 49, 16),       # red → slight yellow
    "mercury": (123, 141, 158),  # cool metal mid
    "blood_dark": (167, 36, 43), # blood, slightly darker
    # Pack midtones (most frequent body pixel on each sheet).
    "pack_green": (0, 140, 58),
    "pack_red": (178, 0, 6),
    "pack_blue": (0, 127, 255),
    "pack_purple": (178, 0, 148),
}

# Optional extra flags applied when --preset is used (sat/value/scales/shift).
PRESET_TUNING: dict[str, dict] = {
    "blood": {"value_scale": 0.82},
    "blood_dark": {},
    "oil": {"sat": 0.92, "value": 0.88},
    "lava": {"sat": 0.92, "value": 0.78},
    "mercury": {"sat": 0.22, "value": 0.62},
    "acid": {"hue_shift": 15},
}


def rgb_to_hsv(rgb: tuple[int, int, int]) -> tuple[float, float, float]:
    r, g, b = (c / 255.0 for c in rgb)
    return colorsys.rgb_to_hsv(r, g, b)


def hsv_to_rgb(h: float, s: float, v: float) -> tuple[int, int, int]:
    r, g, b = colorsys.hsv_to_rgb(h % 1.0, clamp01(s), clamp01(v))
    return (int(round(r * 255)), int(round(g * 255)), int(round(b * 255)))


def clamp01(x: float) -> float:
    return 0.0 if x < 0.0 else 1.0 if x > 1.0 else x


def luminance(rgb: tuple[int, int, int]) -> float:
    r, g, b = (c / 255.0 for c in rgb)
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def parse_hex(text: str) -> tuple[int, int, int]:
    t = text.strip().lstrip("#")
    if len(t) == 3:
        t = "".join(ch * 2 for ch in t)
    if len(t) != 6:
        raise argparse.ArgumentTypeError(f"bad hex color: {text!r}")
    return int(t[0:2], 16), int(t[2:4], 16), int(t[4:6], 16)


def is_black(rgb: tuple[int, int, int], a: int) -> bool:
    return a >= 250 and rgb[0] <= 8 and rgb[1] <= 8 and rgb[2] <= 8


def is_pure_white(rgb: tuple[int, int, int], a: int) -> bool:
    return a >= 250 and rgb == (255, 255, 255)


def extract_body_ramp(image: Image.Image) -> list[tuple[int, int, int]]:
    """Return the 6 opaque body colors, darkest → brightest (by luminance)."""
    counts: Counter[tuple[int, int, int]] = Counter()
    for r, g, b, a in image.copy().getdata():
        if a < 250:
            continue
        rgb = (r, g, b)
        if is_black(rgb, a) or is_pure_white(rgb, a):
            continue
        counts[rgb] += 1
    if len(counts) != 6:
        raise SystemExit(
            f"expected 6 body colors on template, found {len(counts)}: "
            f"{counts.most_common()}"
        )
    return sorted(counts.keys(), key=luminance)


def mid_index(ramp: list[tuple[int, int, int]], image: Image.Image) -> int:
    """Index of the most frequent body color (artist 'main' midtone)."""
    counts = Counter()
    body = set(ramp)
    for r, g, b, a in image.copy().getdata():
        if a < 250:
            continue
        rgb = (r, g, b)
        if rgb in body:
            counts[rgb] += 1
    main = counts.most_common(1)[0][0]
    return ramp.index(main)


def build_ramp(
    template: list[tuple[int, int, int]],
    key_rgb: tuple[int, int, int],
    key_index: int,
    hue: float | None,
    sat: float | None,
    value: float | None,
    hue_shift_deg: float,
    sat_scale: float,
    value_scale: float,
) -> list[tuple[int, int, int]]:
    """Build a 6-stop ramp by scaling template HSV around the key stop."""
    t_hsv = [rgb_to_hsv(c) for c in template]
    kh, ks, kv = t_hsv[key_index]

    target_h, target_s, target_v = rgb_to_hsv(key_rgb)
    if hue is not None:
        target_h = (hue % 360.0) / 360.0
    target_h = (target_h + hue_shift_deg / 360.0) % 1.0
    if sat is not None:
        target_s = clamp01(sat)
    if value is not None:
        target_v = clamp01(value)
    target_s = clamp01(target_s * sat_scale)
    target_v = clamp01(target_v * value_scale)

    # Relative S/V vs key stop = "how much darker / paler than the center".
    # Hue is one axis; yellow (R+G) falls out of HSV automatically — no need
    # to hand-split channels.
    #
    # Shadows: V scales by template ratio (stays below key).
    # Lights: if key*ratio would clip past 1, remap template light range
    # kv..vmax onto target_v..1 so the key color stays exact and stops
    # do not collapse into identical clipped yellows/whites.
    max_tv = max(tv for _, _, tv in t_hsv)
    out: list[tuple[int, int, int]] = []
    for th, ts, tv in t_hsv:
        s_ratio = (ts / ks) if ks > 1e-6 else 1.0
        if tv <= kv + 1e-6:
            v = target_v * ((tv / kv) if kv > 1e-6 else 1.0)
        elif max_tv <= kv + 1e-6:
            v = target_v
        else:
            t = (tv - kv) / (max_tv - kv)
            v = target_v + t * (1.0 - target_v)
        out.append(hsv_to_rgb(target_h, target_s * s_ratio, v))
    return out


def recolor(
    image: Image.Image,
    src_ramp: list[tuple[int, int, int]],
    dst_ramp: list[tuple[int, int, int]],
) -> Image.Image:
    mapping = dict(zip(src_ramp, dst_ramp))
    pixels = list(image.copy().getdata())
    out = []
    for r, g, b, a in pixels:
        rgb = (r, g, b)
        if a >= 250 and rgb in mapping:
            nr, ng, nb = mapping[rgb]
            out.append((nr, ng, nb, a))
        else:
            out.append((r, g, b, a))
    result = Image.new("RGBA", image.size)
    result.putdata(out)
    return result


def dump_palette(path: Path) -> None:
    im = Image.open(path).convert("RGBA")
    ramp = extract_body_ramp(im)
    mid = mid_index(ramp, im)
    print(f"template: {path}")
    print(f"mid index: {mid}  mid rgb: {ramp[mid]}")
    print("stop  RGB                 L      H      S      V      role")
    for i, rgb in enumerate(ramp):
        h, s, v = rgb_to_hsv(rgb)
        role = "KEY" if i == mid else ("shadow" if i < mid else "light")
        print(
            f"{i:4d}  {rgb!s:<18}  {luminance(rgb):.3f}  "
            f"{h*360:5.1f}  {s:.3f}  {v:.3f}  {role}"
        )


def resolve_key(args: argparse.Namespace) -> tuple[int, int, int]:
    if args.preset:
        return PRESETS[args.preset]
    if args.hex:
        return args.hex
    if args.hue is not None:
        # Neutral mid sat/value defaults when only hue is given.
        s = 0.95 if args.sat is None else args.sat
        v = 0.70 if args.value is None else args.value
        return hsv_to_rgb(args.hue / 360.0, s, v)
    raise SystemExit("provide --preset, --hex, or --hue")


def build_arg_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument(
        "--template",
        type=Path,
        default=DEFAULT_TEMPLATE,
        help="source sheet (default: green FDR_Enemy_01_A.png)",
    )
    p.add_argument("--dump-palette", action="store_true", help="print template ramp and exit")
    p.add_argument("-o", "--output", type=Path, help="output PNG path")
    g = p.add_mutually_exclusive_group()
    g.add_argument("--preset", choices=sorted(PRESETS), help="anchor on a known RGB")
    g.add_argument("--hex", type=parse_hex, help="anchor RGB, e.g. #A86C24")
    g.add_argument("--hue", type=float, help="anchor hue in degrees 0..360")
    p.add_argument("--sat", type=float, help="override key saturation 0..1")
    p.add_argument("--value", type=float, help="override key value 0..1")
    p.add_argument("--hue-shift", type=float, default=0.0, help="extra degrees after anchor hue")
    p.add_argument("--sat-scale", type=float, default=1.0, help="multiply whole ramp saturation")
    p.add_argument("--value-scale", type=float, default=1.0, help="multiply whole ramp value")
    p.add_argument(
        "--also-meta-from",
        type=Path,
        help="copy Unity .meta from this png beside the output (optional)",
    )
    return p


def main(argv: list[str] | None = None) -> int:
    args = build_arg_parser().parse_args(argv)
    template_path = args.template
    if not template_path.is_file():
        print(f"missing template: {template_path}", file=sys.stderr)
        return 1

    if args.dump_palette:
        dump_palette(template_path)
        return 0

    if args.output is None:
        print("need -o/--output (or --dump-palette)", file=sys.stderr)
        return 1

    image = Image.open(template_path).convert("RGBA")
    src_ramp = extract_body_ramp(image)
    key_i = mid_index(src_ramp, image)
    key = resolve_key(args)
    tune = PRESET_TUNING.get(args.preset or "", {})
    sat = args.sat if args.sat is not None else tune.get("sat")
    value = args.value if args.value is not None else tune.get("value")
    hue_shift = args.hue_shift if args.hue_shift != 0.0 else float(tune.get("hue_shift", 0.0))
    # CLI --hue-shift 0 is common default; if preset has hue_shift and user left 0, use preset.
    if args.preset and args.hue_shift == 0.0 and "hue_shift" in tune:
        hue_shift = float(tune["hue_shift"])
    sat_scale = args.sat_scale if args.sat_scale != 1.0 else float(tune.get("sat_scale", 1.0))
    value_scale = args.value_scale if args.value_scale != 1.0 else float(tune.get("value_scale", 1.0))
    if args.preset and args.value_scale == 1.0 and "value_scale" in tune:
        value_scale = float(tune["value_scale"])
    if args.preset and args.sat_scale == 1.0 and "sat_scale" in tune:
        sat_scale = float(tune["sat_scale"])
    dst_ramp = build_ramp(
        src_ramp,
        key_rgb=key,
        key_index=key_i,
        hue=args.hue if args.preset is None and args.hex is None else None,
        sat=sat,
        value=value,
        hue_shift_deg=hue_shift,
        sat_scale=sat_scale,
        value_scale=value_scale,
    )

    print("source ramp → destination ramp")
    for s, d in zip(src_ramp, dst_ramp):
        sh, ss, sv = rgb_to_hsv(s)
        dh, ds, dv = rgb_to_hsv(d)
        print(
            f"  {s} L={luminance(s):.3f} → {d} L={luminance(d):.3f}  "
            f"H {sh*360:5.1f}→{dh*360:5.1f}  S {ss:.2f}→{ds:.2f}  V {sv:.2f}→{dv:.2f}"
        )

    result = recolor(image, src_ramp, dst_ramp)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    result.save(args.output)
    print(f"wrote {args.output}")

    if args.also_meta_from is not None:
        src_meta = Path(str(args.also_meta_from) + ".meta")
        if not src_meta.is_file():
            print(f"warn: no meta at {src_meta}", file=sys.stderr)
        else:
            # New guid so Unity treats it as a distinct texture.
            import uuid

            text = src_meta.read_text()
            lines = []
            for line in text.splitlines(keepends=True):
                if line.startswith("guid:"):
                    lines.append(f"guid: {uuid.uuid4().hex}\n")
                else:
                    lines.append(line)
            out_meta = Path(str(args.output) + ".meta")
            out_meta.write_text("".join(lines))
            print(f"wrote {out_meta}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
