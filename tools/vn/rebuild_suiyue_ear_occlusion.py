"""Use the registered ear donor as an active stationary surface in the PSD."""
import hashlib
import json

import cv2
import numpy as np
from PIL import Image, ImageDraw

from rebuild_suiyue_head_contours import Group, MOVERS, PSDImage, ROOT, insert, poly_mask, render, rgba_part
from polish_suiyue_contact_edges import anchored_part

PREVIOUS = ROOT / "tools/vn/suiyue_contact_polish"
OUT = ROOT / "tools/vn/suiyue_ear_occlusion"
SOURCE = PREVIOUS / "suiyue_head_contours.psd"
DONOR = OUT / "ear_donor_generated.png"
CROP = (380, 125, 640, 320)
EAR_SETUP = {
    "R": {
        "landmarks": ([(392, 229), (405, 211), (428, 266)],
                      [(391, 229), (405, 213), (425, 266)]),
        "zone": [(387, 206), (423, 207), (422, 222), (419, 237),
                 (424, 250), (429, 267), (416, 274), (390, 269)],
        "trace": [
            [(419, 204), (442, 205), (430, 225), (419, 248),
             (412, 266), (408, 278), (400, 278), (400, 262),
             (405, 244), (411, 224)],
            [(419, 226), (436, 225), (439, 245), (442, 272),
             (446, 282), (424, 282), (425, 262), (423, 243)],
        ],
    },
    "L": {
        "landmarks": ([(603, 244), (608, 255), (575, 286)],
                      [(607, 242), (612, 253), (575, 284)]),
        "zone": [(601, 240), (619, 236), (622, 266), (614, 288),
                 (586, 290), (580, 283), (587, 265), (594, 253)],
        "trace": [
            [(590, 231), (612, 232), (608, 249), (600, 265),
             (595, 288), (594, 297), (578, 295), (578, 280),
             (584, 267), (571, 275), (576, 264), (585, 249)],
            [(606, 236), (612, 238), (608, 253), (605, 273),
             (601, 286), (596, 285), (599, 267), (600, 250)],
        ],
    },
}


def is_hair(name):
    return name in MOVERS or name.removesuffix("_contour") in MOVERS or "_ear_overlap_" in name


def skin_mask(rgb):
    red, green, blue = rgb.astype(np.int16).transpose(2, 0, 1)
    return (red >= 164) & (red - green >= 21) & (green - blue >= 6)


def distance_map(mask):
    return cv2.distanceTransform((~mask).astype(np.uint8), cv2.DIST_L2, 5)


def registered_donor(size, rgb, donor, landmarks, zone, moving):
    src, dst = (np.array(points, np.float32) for points in landmarks)
    src -= (360, 110)
    affine = cv2.getAffineTransform(src, dst)
    assert .65 < np.linalg.det(affine[:, :2]) < 1.5
    registered = cv2.warpAffine(donor, affine, size, flags=cv2.INTER_LINEAR)
    # Estimate the colour shift from skin visible in both images, away from
    # moving strands and the outer ear rim.
    overlap = zone & skin_mask(rgb) & skin_mask(registered) & (distance_map(moving) >= 2)
    assert int(overlap.sum()) >= 60
    delta = rgb[overlap].astype(np.int16) - registered[overlap].astype(np.int16)
    adjustment = np.clip(np.median(delta, axis=0), -22, 22)
    corrected = np.rint(registered + adjustment).clip(0, 255).astype(np.uint8)
    return corrected, affine.tolist(), adjustment.tolist()


def rebuild_ear(size, art, owner_masks, donor, setup):
    rgb = art[:, :, :3]
    original_alpha = art[:, :, 3]
    zone = poly_mask(size, [setup["zone"]]) & (original_alpha >= 250)
    trace = poly_mask(size, setup["trace"])
    moves = np.logical_or.reduce(list(owner_masks.values()))
    red, green, blue = rgb.astype(np.int16).transpose(2, 0, 1)
    dark = (red <= 152) & (red - blue <= 27) & (blue >= green - 3)
    ear_donor, affine, adjustment = registered_donor(size, rgb, donor, setup["landmarks"], zone, moves)
    near_old_mover = distance_map(moves) <= 4
    core = zone & dark & (moves | (trace & near_old_mover))
    # On the artificial cut through an otherwise continuous lock, a bright
    # donor pixel would become a skin-coloured stripe. Retain hair texture
    # there unless the visible ear is the nearer stationary material.
    fixed_skin = zone & skin_mask(rgb) & ~moves
    fixed_hair = zone & dark & ~moves
    near_skin = distance_map(fixed_skin)
    near_hair = distance_map(fixed_hair)
    internal_cut = core & (near_hair + 1.25 < near_skin)
    ear_donor[internal_cut] = rgb[internal_cut]
    inner = cv2.erode(core.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool)
    dist, index = cv2.distanceTransformWithLabels(
        (~inner).astype(np.uint8), cv2.DIST_L2, 5, labelType=cv2.DIST_LABEL_PIXEL,
    )
    hair_rgb = rgb[inner][index - 1].astype(np.float32)
    ear_rgb = ear_donor.astype(np.float32)
    reference_rgb = rgb.astype(np.float32)
    vector = hair_rgb - ear_rgb
    weight = np.clip(((reference_rgb - ear_rgb) * vector).sum(axis=2)
                     / np.maximum((vector ** 2).sum(axis=2), 1), 0, 1)
    error = np.abs(reference_rgb - (ear_rgb + weight[:, :, None] * vector)).max(axis=2)
    fringe = zone & (trace | moves) & skin_mask(ear_donor) & (dist <= 3.5)
    fringe &= (weight > .06) & (weight < .97) & (error < 24)
    moving = core | fringe
    # Keep pixels attributed to the same strand. For recovered pixels, pick
    # the nearest of the existing strand supports.
    owner_distances = np.stack([distance_map(mask) for mask in owner_masks.values()])
    owner = owner_distances.argmin(axis=0)
    fixed_rgb = rgb.copy()
    fixed_rgb[moving] = ear_donor[moving]
    foreground_rgb = rgb.copy()
    minimum = np.where(reference_rgb < ear_rgb, (ear_rgb - reference_rgb) / np.maximum(ear_rgb, 1),
                       (reference_rgb - ear_rgb) / np.maximum(255 - ear_rgb, 1)).max(axis=2)
    opacity = np.where(fringe, np.maximum(weight, minimum), 1)
    alpha = np.rint(opacity * 255).astype(np.uint8)
    alpha_ratio = alpha.astype(np.float32) / 255
    recovered = (reference_rgb - (1 - alpha_ratio[:, :, None]) * ear_rgb) / np.maximum(alpha_ratio[:, :, None], .01)
    foreground_rgb[fringe] = np.rint(recovered[fringe]).clip(0, 255).astype(np.uint8)
    fixed_alpha = np.where(zone, original_alpha, 0)
    fixed_alpha[moving] = 255
    moving_images = {}
    for i, name in enumerate(owner_masks):
        part_alpha = np.where(moving & (owner == i), alpha, 0)
        if part_alpha.any():
            moving_images[name] = rgba_part(foreground_rgb, part_alpha)
    return zone, rgba_part(fixed_rgb, fixed_alpha), moving_images, {
        "affine": affine,
        "skinRgbAdjustment": adjustment,
        "coveredEarPixels": int(moving.sum()),
        "mattedEdgePixels": int(fringe.sum()),
        "originalHairBackingPixels": int(internal_cut.sum()),
    }


def copy_layers(source, destination, rework, visible=True, underlay=False):
    for layer in source:
        active = visible and layer.visible
        if layer.is_group():
            new = Group.new(destination, layer.name)
            copy_layers(layer, new, rework, active,
                        underlay or layer.name == "00_hidden_surface_underlays")
        else:
            pixels = np.array(layer.topil().convert("RGBA"))
            if active or underlay or layer.name == "local_surface_overlap":
                pixels[rework[layer.top:layer.bottom, layer.left:layer.right]] = 0
            new = insert(destination, layer.name, Image.fromarray(pixels), layer.offset)
            if new is None:
                continue
            new.clipping = layer.clipping
        new.visible = layer.visible
        new.opacity = layer.opacity


def crop_render(model):
    return model.composite(
        viewport=CROP, force=True, ignore_preview=True, color=0, alpha=0, apply_icc=False,
    ).convert("RGBA")


def probes(model, anchored):
    parts = list(model.descendants())
    hair = [part for part in parts if not part.is_group() and is_hair(part.name)]
    for part in parts:
        if part.name in {"00_hidden_surface_underlays", "local_surface_overlap"}:
            part.visible = True
    results = []
    for dx in (-2, 2):
        temporary = []
        offsets = [part.offset for part in hair]
        for layer in hair:
            if anchored:
                image, offset = anchored_part(layer, dx)
                replacement = insert(layer.parent, layer.name, image, offset)
                replacement.opacity = layer.opacity
                layer.parent.insert(layer.parent.index(layer), replacement)
                temporary.append(replacement)
                layer.visible = False
            else:
                layer.offset = (layer.left + dx, layer.top)
        results.append(crop_render(model))
        for part in temporary:
            part.parent.remove(part)
        for part, offset in zip(hair, offsets):
            part.visible = True
            part.offset = offset
    return results


def sheet(old_frames, new_frames, name, prefix):
    output = Image.new("RGB", (1440, 760), (214, 213, 217))
    draw = ImageDraw.Draw(output)
    for row, frames in enumerate((old_frames, new_frames)):
        for i, frame in enumerate(frames):
            image = frame.crop((10, 10, 250, 185)).resize((468, 341), Image.Resampling.LANCZOS)
            x, y = i * 480, row * 380
            label = "Prior" if row == 0 else "Donor-backed"
            draw.text((x + 8, y + 8), f"{label} / {prefix} {(-2, 0, 2)[i]:+d} px", fill="black")
            output.paste(image, (x + 6, y + 30), image)
    output.save(OUT / name)


def main():
    baseline = PSDImage.open(SOURCE)
    neutral = Image.open(PREVIOUS / "neutral.png").convert("RGBA")
    art = np.asarray(neutral)
    size = baseline.size
    donor = np.asarray(Image.open(DONOR).convert("RGB").resize((304, 304), Image.Resampling.LANCZOS))
    owners = {name: np.zeros(art.shape[:2], dtype=bool) for name in sorted(MOVERS)}
    for layer in baseline.descendants():
        if layer.is_group() or not is_hair(layer.name):
            continue
        name = layer.name.removesuffix("_contour")
        pixels = np.asarray(layer.topil().convert("RGBA"))
        owners[name][layer.top:layer.bottom, layer.left:layer.right] |= pixels[:, :, 3] >= 25
    ears = {side: rebuild_ear(size, art, owners, donor, setup) for side, setup in EAR_SETUP.items()}
    rework = np.logical_or.reduce([value[0] for value in ears.values()])
    psd = PSDImage.new("RGBA", size, color=(0, 0, 0, 0))
    copy_layers(baseline, psd, rework)
    original_parts = next(layer for layer in psd if layer.name == "01_original_parts")
    first_hair = next(layer for layer in original_parts if is_hair(layer.name))
    fixed = Group.new(original_parts, "06_rebuilt_stationary_ear_temple")
    original_parts.insert(original_parts.index(first_hair), fixed)
    moving = Group.new(psd, "07_hair_over_ear")
    for side, (_, fixed_image, parts, _) in ears.items():
        insert(fixed, f"ear_{side}_continuous_surface", fixed_image)
        fixed_image.save(OUT / f"ear_{side}_surface.png")
        for name, image in parts.items():
            insert(moving, f"{name}_ear_overlap_{side}", image)
    path = OUT / "suiyue_ear_occlusion.psd"
    psd.save(path)
    candidate = PSDImage.open(path)
    rest = render(candidate)
    rest.save(OUT / "neutral.png")
    delta = np.abs(np.asarray(rest).astype(np.int16) - art)
    assert delta[~rework].max() == 0
    assert delta[rework, :3].max() <= 2, int(delta[rework, :3].max())
    assert delta[:, :, 3].max() <= 5
    old_rigid = probes(baseline, anchored=False)
    new_rigid = probes(candidate, anchored=False)
    old_anchor = probes(baseline, anchored=True)
    new_anchor = probes(candidate, anchored=True)
    left, top, right, bottom = CROP
    probe_mask = rework[top:bottom, left:right]
    min_alpha = [int(np.asarray(frame)[:, :, 3][probe_mask].min())
                 for frame in new_rigid + new_anchor]
    assert min(min_alpha) >= 250, min_alpha
    sheet([old_rigid[0], neutral.crop(CROP), old_rigid[1]],
          [new_rigid[0], rest.crop(CROP), new_rigid[1]], "ear_rigid_comparison.png", "shift")
    sheet([old_anchor[0], neutral.crop(CROP), old_anchor[1]],
          [new_anchor[0], rest.crop(CROP), new_anchor[1]], "ear_anchored_comparison.png", "tip")
    receipt = {
        "sourceSha256": hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        "donorSha256": hashlib.sha256(DONOR.read_bytes()).hexdigest(),
        "psdSha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "pixelLayers": sum(not layer.is_group() for layer in candidate.descendants()),
        "neutralMaxRgbDelta": int(delta[:, :, :3].max()),
        "neutralMaxAlphaDelta": int(delta[:, :, 3].max()),
        "outsideEarZonesMaxDelta": int(delta[~rework].max()),
        "earUnderlaysDefaultVisible": True,
        "minimumProbeAlpha": {"rigid": min_alpha[:2], "anchored": min_alpha[2:]},
        "ears": {side: value[3] for side, value in ears.items()},
    }
    (OUT / "verification.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    print(json.dumps(receipt), flush=True)


if __name__ == "__main__":
    main()
