"""Reassign the visible brow and ear intersections in the Suiyue source PSD."""
from pathlib import Path
import hashlib
import json
import sys

import cv2
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "Library/VNPSDVerify"))
from psd_tools import PSDImage
from psd_tools.api.layers import Group, PixelLayer

PREVIOUS = ROOT / "tools/vn/suiyue_hair_seams_20261106"
OUT = ROOT / "tools/vn/suiyue_head_contours_20261110"
SOURCE = PREVIOUS / "suiyue_hair_seams.psd"
RIGHT = "hair_front_R"
LEFT = "hair_front_L"
LONG_RIGHT = "hair_front_long_R"
LONG_LEFT = "hair_front_long_L"
MOVERS = {RIGHT, LEFT, LONG_RIGHT, LONG_LEFT}

# Coordinates refer to the native portrait. R/L refer to the character.
# Trace from the neutral image, including the strand behind each old brow cut.
TRACES = {
    RIGHT: [
        [(426, 175), (487, 151), (482, 172), (473, 186), (462, 195),
         (446, 205), (435, 211), (427, 219), (425, 232), (421, 234),
         (411, 250), (405, 268), (402, 282), (400, 279), (403, 251),
         (409, 229), (419, 205)],
        [(488, 142), (510, 141), (505, 157), (502, 175), (502, 191),
         (508, 202), (505, 202), (499, 196), (495, 188), (495, 195),
         (498, 211), (493, 207), (490, 200), (487, 187), (486, 167)],
        [(425, 201), (441, 202), (441, 225), (442, 240), (448, 251),
         (457, 263), (450, 261), (443, 255), (436, 243), (431, 228)],
    ],
    LEFT: [
        [(559, 140), (576, 140), (576, 164), (573, 178), (568, 192),
         (560, 204), (551, 213), (545, 217), (548, 210), (555, 198),
         (559, 184), (563, 167), (564, 153)],
        [(571, 151), (590, 154), (592, 173), (591, 191), (586, 207),
         (582, 219), (575, 222), (576, 211), (576, 190), (574, 172)],
        [(588, 160), (605, 164), (606, 191), (607, 214), (605, 234),
         (599, 246), (592, 255), (579, 266), (568, 272), (573, 264),
         (580, 255), (585, 244), (589, 231), (590, 214), (589, 195)],
    ],
    LONG_RIGHT: [
        [(416, 213), (437, 214), (438, 238), (441, 258), (441, 283),
         (448, 309), (422, 312), (424, 291), (424, 267), (421, 241)],
    ],
    LONG_LEFT: [
        [(591, 237), (607, 232), (605, 249), (598, 267), (594, 286),
         (594, 309), (577, 311), (580, 288), (583, 268)],
        [(603, 225), (610, 225), (605, 251), (604, 267), (603, 283),
         (601, 281), (601, 258)],
    ],
}


def poly_mask(size, paths):
    width, height = size
    mask = np.zeros((height, width), dtype=np.uint8)
    for path in paths:
        cv2.fillPoly(mask, [np.asarray(path, dtype=np.int32)], 1)
    return mask.astype(bool)


def rgba_part(rgb, alpha):
    rgba = np.dstack((rgb, alpha))
    rgba[alpha == 0, :3] = 0
    return Image.fromarray(rgba)


def insert(parent, name, image, offset=(0, 0)):
    bounds = image.getbbox()
    if bounds:
        return PixelLayer.frompil(
            image.crop(bounds), parent, name=name,
            left=offset[0] + bounds[0], top=offset[1] + bounds[1],
        )
    return None


def render(psd):
    return psd.composite(force=True, ignore_preview=True, color=0, alpha=0,
                         apply_icc=False).convert("RGBA")


def underpaint(rgb, source, hair, donor):
    """Extend matching stationary materials into the immediate swept edge."""
    fill = donor.copy()
    width = rgb.shape[1]
    for y in np.flatnonzero(hair.any(axis=1)):
        row = np.flatnonzero(hair[y])
        fixed = np.flatnonzero(source[y] & ~hair[y])
        if not len(fixed):
            continue
        at = np.searchsorted(fixed, row)
        left = fixed[np.maximum(at - 1, 0)]
        right = fixed[np.minimum(at, len(fixed) - 1)]
        dist_left = np.where(left < row, row - left, width)
        dist_right = np.where(right > row, right - row, width)
        near = np.where(dist_left <= dist_right, left, right)
        exposed = np.minimum(dist_left, dist_right) <= 3
        fill[y, row[exposed]] = rgb[y, near[exposed]]
    return fill


def nearest_colors(rgb, samples):
    distance, index = cv2.distanceTransformWithLabels(
        (~samples).astype(np.uint8), cv2.DIST_L2, 5, labelType=cv2.DIST_LABEL_PIXEL,
    )
    return distance, rgb[samples][index - 1]


def matte_skin_edges(rgb, alpha, zone, ownership, edge_zone=None):
    """Recover the strand color from pixels mixed with adjacent skin."""
    occupied = np.logical_or.reduce(list(ownership.values()))
    red, green, blue = rgb.astype(np.int16).transpose(2, 0, 1)
    solid_skin = (red >= 173) & (red - green >= 19) & (green - blue >= 6)
    solid_skin &= zone & ~occupied
    skin_distance, skin_rgb = nearest_colors(rgb, solid_skin)
    skin_rgb = skin_rgb.astype(np.float32)
    source = rgb.astype(np.float32)
    fixed_alpha = np.zeros_like(alpha)
    layers = {}
    matte_counts = {}
    for name, mask in ownership.items():
        inner = cv2.erode(mask.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool)
        hair_distance, hair_rgb = nearest_colors(rgb, inner)
        vector = hair_rgb.astype(np.float32) - skin_rgb
        weight = np.clip(
            ((source - skin_rgb) * vector).sum(axis=2) / np.maximum((vector ** 2).sum(axis=2), 1),
            0, 1,
        )
        error = np.abs(source - (skin_rgb + weight[:, :, None] * vector)).max(axis=2)
        ordinary = (skin_distance <= 3) & (hair_distance <= 3) & (error <= 14)
        ordinary &= (weight > .15) & (weight < .93)
        if edge_zone is not None:
            immediate = cv2.dilate(mask.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool)
            extended = edge_zone & immediate & (skin_distance <= 4) & (hair_distance <= 4)
            extended &= (error <= 21) & (weight > .07) & (weight < .98)
            ordinary |= extended
        mixed = zone & ordinary & (mask | ~occupied) & (fixed_alpha == 0)
        # Enforce a representable foreground RGB before solving the alpha pair.
        minimum = np.where(source < skin_rgb, (skin_rgb - source) / np.maximum(skin_rgb, 1),
                           (source - skin_rgb) / np.maximum(255 - skin_rgb, 1)).max(axis=2)
        weight = np.maximum(weight, minimum)
        hair_alpha = np.round(alpha * weight).astype(np.uint8)
        mix_alpha = hair_alpha.astype(np.float32) / np.maximum(alpha, 1)
        corrected = (source - (1 - mix_alpha[:, :, None]) * skin_rgb) / np.maximum(mix_alpha[:, :, None], .01)
        corrected = np.clip(np.round(corrected), 0, 255).astype(np.uint8)
        fixed_alpha[mixed] = np.round(
            (alpha[mixed] - hair_alpha[mixed]) / (1 - hair_alpha[mixed] / 255),
        ).astype(np.uint8)
        layer_rgb = rgb.copy()
        layer_rgb[mixed] = corrected[mixed]
        moving_alpha = np.where(mask, alpha, 0)
        moving_alpha[mixed] = hair_alpha[mixed]
        layers[name] = rgba_part(layer_rgb, moving_alpha)
        ownership[name] = mask | mixed
        occupied |= mixed
        matte_counts[name] = int(mixed.sum())
    return layers, occupied, rgba_part(skin_rgb.astype(np.uint8), fixed_alpha), matte_counts


def main(output=OUT, overlap=underpaint, comparison=PREVIOUS, edge_paths=()):
    output.mkdir(exist_ok=True)
    original = PSDImage.open(SOURCE)
    neutral = Image.open(PREVIOUS / "neutral_working.png").convert("RGBA")
    art = np.asarray(neutral)
    rgb = art[:, :, :3].copy()
    red, green, blue = rgb.astype(np.int16).transpose(2, 0, 1)
    opaque = art[:, :, 3] >= 250
    width, height = original.size
    yy, xx = np.indices((height, width))

    # The editable bands encompass the former cross-cuts without touching the
    # iris apertures, nose, mouth, outfit or the outer character silhouette.
    zone = poly_mask(original.size, [
        [(399, 170), (515, 170), (515, 214), (499, 221), (497, 201),
         (471, 199), (447, 202), (426, 218), (414, 269), (400, 287),
         (393, 263)],
        [(540, 175), (617, 175), (624, 270), (618, 292), (574, 290),
         (568, 269), (587, 245), (591, 229), (581, 219), (542, 219)],
        [(425, 229), (441, 230), (447, 247), (460, 266), (448, 273),
         (424, 272), (413, 254)],
        [(482, 135), (512, 135), (510, 172), (481, 172)],
        [(555, 140), (586, 140), (585, 178), (554, 178)],
    ]) & opaque
    cool_hair = (red - blue < 26) & (blue >= green - 3) & (red < 160)
    eye_guard = poly_mask(original.size, [
        [(432, 209), (449, 203), (475, 204), (494, 215), (501, 232),
         (490, 244), (453, 247), (437, 235)],
        [(539, 226), (552, 218), (575, 217), (590, 221), (592, 233),
         (581, 248), (567, 258), (540, 250)],
    ])
    zone &= ~eye_guard
    original_move = np.zeros_like(zone)
    for layer in original.descendants():
        if not layer.is_group() and layer.name in MOVERS:
            pixels = np.asarray(layer.topil().convert("RGBA"))
            original_move[layer.top:layer.bottom, layer.left:layer.right] |= pixels[:, :, 3] > 0

    ownership = {}
    occupied = np.zeros_like(zone)
    for name, paths in TRACES.items():
        shape = poly_mask(original.size, paths)
        mask = zone & shape & cool_hair & ~occupied
        ownership[name] = mask
        occupied |= mask
    moving_images, occupied, matte_skin, matte_counts = matte_skin_edges(
        rgb, art[:, :, 3], zone, ownership,
        poly_mask(original.size, edge_paths) if edge_paths else None,
    )
    rework = zone & (occupied | original_move)

    # Keep forehead, ears and brows stationary at their actual visible pixels.
    # Only cut the original layer stack where pixel ownership changed.
    brow_bounds = [
        poly_mask(original.size, [[(459, 182), (476, 181), (491, 184),
                                  (511, 194), (509, 201), (490, 192),
                                  (474, 189), (459, 187)]]),
        poly_mask(original.size, [[(542, 198), (552, 191), (572, 187),
                                  (595, 189), (599, 196), (578, 194),
                                  (558, 197), (543, 205)]]),
    ]
    stationary = rework & ~occupied
    brows = [(stationary & mask & cool_hair) for mask in brow_bounds]
    ears = [
        stationary & (xx < 428) & (yy >= 209) & (yy <= 270) & ~cool_hair,
        stationary & (xx >= 572) & (yy >= 240) & (yy <= 287) & ~cool_hair,
    ]
    skin = stationary & ~np.logical_or.reduce(brows + ears)
    donor = np.asarray(Image.open(PREVIOUS / "aligned_underpaint.png").convert("RGBA"))
    fill_rgb = np.where((donor[:, :, 3] > 240)[:, :, None], donor[:, :, :3], rgb)
    fill_rgb = overlap(rgb, opaque, occupied, fill_rgb)

    result = PSDImage.new("RGBA", original.size, color=(0, 0, 0, 0))

    def copy_layers(source, destination, inherited_visible=True):
        for old in source:
            active = inherited_visible and old.visible
            if old.is_group():
                new = Group.new(destination, old.name)
                copy_layers(old, new, active)
            else:
                pixels = np.array(old.topil().convert("RGBA"))
                if active:
                    cut = rework[old.top:old.bottom, old.left:old.right]
                    pixels[cut] = 0
                new = insert(destination, old.name, Image.fromarray(pixels), old.offset)
                if new is None:
                    continue
                new.clipping = old.clipping
            new.visible = old.visible
            new.opacity = old.opacity

    copy_layers(original, result)
    fixed = Group.new(result, "04_rebuilt_stationary_contours")
    backing = insert(fixed, "local_surface_overlap", rgba_part(fill_rgb, occupied.astype(np.uint8) * 255))
    backing.visible = False
    insert(fixed, "skin_under_antialiased_strands", matte_skin)
    insert(fixed, "fixed_surface_fragments", rgba_part(rgb, np.where(skin, art[:, :, 3], 0)))
    for i, side in enumerate(("R", "L")):
        insert(fixed, f"ear_{side}_contour", rgba_part(rgb, np.where(ears[i], art[:, :, 3], 0)))
        insert(fixed, f"brow_{side}_contour", rgba_part(rgb, np.where(brows[i], art[:, :, 3], 0)))
    moving = Group.new(result, "05_rebuilt_front_contours")
    for name, art_part in moving_images.items():
        insert(moving, f"{name}_contour", art_part)

    path = output / "suiyue_head_contours.psd"
    result.save(path)
    result = PSDImage.open(path)
    rest = render(result)
    difference = np.abs(np.asarray(rest).astype(np.int16) - art)
    assert difference[opaque].max() <= 4, int(difference[opaque].max())
    rest.save(output / "neutral.png")
    moving_parts = [part for part in result.descendants() if not part.is_group()
                    and (part.name in MOVERS or part.name.removesuffix("_contour") in MOVERS)]
    offsets = [part.offset for part in moving_parts]
    by_name = {part.name: part for part in result.descendants()}
    by_name["00_hidden_surface_underlays"].visible = True
    by_name["local_surface_overlap"].visible = True
    probes = []
    for dx in (-2, 2):
        for part, (x, y) in zip(moving_parts, offsets):
            part.offset = (x + dx, y)
        probe = result.composite(
            viewport=(380, 125, 640, 320), force=True, ignore_preview=True,
            color=0, alpha=0, apply_icc=False,
        ).convert("RGBA") if comparison != PREVIOUS else render(result)
        probe.save(output / f"hair_shift_{dx}.png")
        probes.append(probe)

    sheet = Image.new("RGB", (1440, 760), (214, 213, 217))
    draw = ImageDraw.Draw(sheet)
    crop = (390, 135, 630, 310)
    for i, (label, baseline, candidate) in enumerate(zip(
        ("Neutral", "Shift left 2 px", "Shift right 2 px"),
        (Image.open(comparison / "neutral.png") if comparison != PREVIOUS else neutral,
         Image.open(comparison / "hair_shift_-2.png"), Image.open(comparison / "hair_shift_2.png")),
        (rest, *probes),
    )):
        for row, (version, image) in enumerate((("Prior", baseline), ("Rebuilt", candidate))):
            image_crop = (10, 10, 250, 185) if image.size == (260, 195) else crop
            patch = image.crop(image_crop).resize((468, 341), Image.Resampling.LANCZOS).convert("RGBA")
            x, y = i * 480, row * 380
            draw.text((x + 8, y + 8), f"{version} / {label}", fill="black")
            sheet.paste(patch, (x + 6, y + 30), patch)
    sheet.save(output / "contour_comparison.png")
    probe_rework = rework if comparison == PREVIOUS else rework[125:320, 380:640]
    receipt = {
        "sourceSha256": hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        "psdSha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "layers": sum(not layer.is_group() for layer in result.descendants()),
        "reworkedPixels": int(rework.sum()),
        "reassignedHairPixels": {name: int(mask.sum()) for name, mask in ownership.items()},
        "mattedEdgePixels": matte_counts,
        "neutralMaxDeltaOpaque": int(difference[opaque].max()),
        "neutralMaxDeltaAlpha": int(difference[:, :, 3].max()),
        "neutralMaxDeltaOutsideRework": int(difference[~rework].max()),
        "probeRegionMinimumAlpha": [int(np.asarray(probe)[:, :, 3][probe_rework].min()) for probe in probes],
    }
    (output / "verification.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()
