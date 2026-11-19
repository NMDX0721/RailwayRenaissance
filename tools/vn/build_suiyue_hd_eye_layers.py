"""Split the approved-style eye study into aligned, movable PSD layers."""
from pathlib import Path
import sys

import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "Library/VNPSDVerify"))
from psd_tools import PSDImage
from psd_tools.api.layers import Group, PixelLayer

OUT = ROOT / "tools/vn/suiyue_cubism_head_import/eye_underlay_repair"
ASSETS = OUT / "hd_iris_assets"

# Coordinates are traced on the 1683×935 face study. R/L are character sides.
EYES = {
    "R": (((420, 228), (504, 191), (628, 192), (697, 242), (725, 279)),
          ((420, 228), (441, 315), (555, 368), (681, 351), (725, 279)),
          (590, 265), (164, 206),
          ((347, 174), (396, 178), (420, 167), (504, 139), (584, 163), (635, 159), (706, 209), (753, 287),
           (710, 257), (674, 234), (622, 214), (560, 202), (503, 205), (454, 215), (423, 228), (390, 215))),
    "L": (((1033, 340), (1111, 282), (1241, 283), (1336, 331), (1359, 328)),
          ((1033, 340), (1012, 395), (1095, 423), (1212, 441), (1310, 411), (1359, 328)),
          (1174, 347), (160, 200),
          ((1010, 361), (1043, 300), (1080, 276), (1127, 253), (1219, 252), (1260, 247), (1290, 278), (1330, 287), (1399, 308),
           (1370, 335), (1341, 342), (1290, 321), (1240, 304), (1185, 297), (1132, 306), (1087, 319), (1040, 342))),
}


def curve(points):
    points = np.asarray(points, dtype=np.float64)
    samples = []
    for t in np.linspace(0, 1, 96):
        row = points.copy()
        while len(row) > 1:
            row = (1 - t) * row[:-1] + t * row[1:]
        samples.append(row[0])
    return np.asarray(samples)


def mask_for(size, points):
    mask = np.zeros((size[1], size[0]), np.uint8)
    cv2.fillPoly(mask, [np.rint(points * 16).astype(np.int32)], 255,
                 lineType=cv2.LINE_AA, shift=4)
    return mask


def solid_layer(rgb, alpha):
    return Image.fromarray(np.dstack((rgb, alpha)).astype(np.uint8))


def place(part, size, center, offset=(0, 0)):
    result = Image.new("RGBA", size)
    result.paste(part, (round(center[0] + offset[0] - part.width / 2),
                        round(center[1] + offset[1] - part.height / 2)))
    return result


def clip(part, alpha):
    pixels = np.asarray(part).copy()
    pixels[:, :, 3] = np.rint(pixels[:, :, 3] * (alpha / 255)).astype(np.uint8)
    return Image.fromarray(pixels)


def eyeset(reference, side):
    size = reference.size
    upper_points, lower_points, center, iris_size, lash_outline = EYES[side]
    upper, lower = curve(upper_points), curve(lower_points)
    aperture = mask_for(size, np.vstack((upper, lower[::-1])))
    pixels = np.asarray(reference.convert("RGB"))
    # Sample the uploaded art away from the iris and carry the tonal ramp across the mask.
    shade = np.asarray(Image.open(ASSETS / "sclera_L_hd_source.png").convert("RGBA"))
    ramp = shade[290:601, 887, :3]
    _, top, _, bottom = Image.fromarray(aperture).getbbox()
    project = np.interp(np.arange(size[0]), upper[:, 0], upper[:, 1])
    depth = np.clip((np.arange(size[1])[:, None] - project[None, :]) / (bottom - top), 0, 1)
    sclera = solid_layer(ramp[np.rint(depth * (len(ramp) - 1)).astype(int)], aperture)

    donor = Image.open(ASSETS / "iris_hd_source.png").convert("RGBA")
    bounds = donor.getchannel("A").point(lambda value: 255 if value > 80 else 0).getbbox()
    donor = donor.crop(bounds).resize(iris_size, Image.Resampling.LANCZOS)
    rgba = np.asarray(donor).copy()
    gray = cv2.cvtColor(rgba[:, :, :3], cv2.COLOR_RGB2GRAY)
    yy, xx = np.indices(gray.shape)
    central = (abs(xx - donor.width / 2) < donor.width * .19) & (yy > donor.height * .13) & (yy < donor.height * .71)
    dark = ((gray < 42) & central & (rgba[:, :, 3] > 180)).astype(np.uint8)
    _, labels, stats, _ = cv2.connectedComponentsWithStats(dark)
    pupil_core = labels == (1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA])))
    color_edges = cv2.dilate(pupil_core.astype(np.uint8), np.ones((5, 5), np.uint8))
    pupil_alpha = cv2.GaussianBlur(color_edges * 255, (5, 5), 0.8)
    highlight = ((gray > 180) & central & (yy < donor.height * .45)).astype(np.uint8)
    remove = cv2.dilate((color_edges | highlight), np.ones((7, 7), np.uint8))
    clean = cv2.inpaint(rgba[:, :, :3], remove, 5, cv2.INPAINT_TELEA)
    parts = [solid_layer(clean, rgba[:, :, 3]), solid_layer(rgba[:, :, :3], pupil_alpha),
             solid_layer(rgba[:, :, :3], highlight * 255)]
    parts = [part.rotate(-7, Image.Resampling.BICUBIC, expand=True) for part in parts]

    luminance = pixels.max(axis=2)
    lash_gate = mask_for(size, np.asarray(lash_outline)) / 255
    warm_lash = (pixels[:, :, 0].astype(int) >= pixels[:, :, 2].astype(int)) & (luminance < 77)
    lash_alpha = cv2.morphologyEx(warm_lash.astype(np.uint8) * 255, cv2.MORPH_CLOSE,
                                 cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (7, 5)))
    lash_rgb = np.minimum(pixels, np.asarray([37, 26, 26], np.uint8))
    lash = solid_layer(lash_rgb, lash_gate * cv2.GaussianBlur(lash_alpha, (3, 3), .6))
    # Take a skin-tone sample from below the reference eye, never from the iris.
    sample = pixels[bottom + 14:bottom + 24, center[0] - 25:center[0] + 25]
    skin = np.median(sample, axis=(0, 1))
    seam = mask_for(size, np.vstack((lower - (0, 2), (lower + (0, 5))[::-1])))
    seam = cv2.GaussianBlur(seam, (5, 5), 1)
    lower_lid = solid_layer(np.broadcast_to(skin * (.78, .67, .62), pixels.shape), seam * .66)
    return sclera, parts, lash, lower_lid, aperture, center, upper, lower


def add(target, name, image, clipping=False):
    box = image.getchannel("A").getbbox()
    if box is None:
        raise ValueError(f"Empty layer: {name}")
    layer = PixelLayer.frompil(image.crop(box), target, name=name, left=box[0], top=box[1])
    layer.clipping = clipping


def main():
    reference = Image.open(OUT / "suiyue_eye_rebuild_hd_reference.png")
    if reference.size != (1683, 935):
        raise ValueError(f"Unexpected face study size: {reference.size}")
    psd = PSDImage.new("RGBA", reference.size, color=(0, 0, 0, 0))
    eyes = {side: eyeset(reference, side) for side in EYES}
    frames = [Image.new("RGBA", reference.size, (248, 205, 185, 255)) for _ in range(3)]
    blinks = [frame.copy() for frame in frames]
    ys, xs = np.indices((reference.height, reference.width), dtype=np.float32)
    for side, (sclera, moving, lash, lower, aperture, center, top_edge, bottom_edge) in eyes.items():
        group = Group.new(psd, f"eye_{side}")
        add(group, f"eye_{side}_sclera", sclera)
        for name, part in zip(("iris", "pupil", "highlight"), moving):
            add(group, f"eye_{side}_{name}", place(part, reference.size, center), clipping=True)
        add(group, f"eye_{side}_lower_lid", lower)
        add(group, f"eye_{side}_upper_lid", lash)
        for frame, dx in zip(frames, (-27, 0, 27)):
            frame.alpha_composite(sclera)
            for part in moving:
                frame.alpha_composite(clip(place(part, reference.size, center, (dx, 0)), aperture))
            frame.alpha_composite(lower)
            frame.alpha_composite(lash)
        # Simulate a descending upper lid and clip the three moving parts to the opening.
        columns = np.arange(reference.width)
        upper_y = np.interp(columns, top_edge[:, 0], top_edge[:, 1])
        lower_y = np.interp(columns, bottom_edge[:, 0], bottom_edge[:, 1])
        for frame, openness in zip(blinks, (1.0, .5, 0.0)):
            shift = (lower_y - upper_y) * (1 - openness)
            lid_y = upper_y + shift
            opening = np.clip(ys - lid_y[None, :] + .5, 0, 1)
            blink_mask = np.rint(aperture * opening).astype(np.uint8) if openness else np.zeros_like(aperture)
            frame.alpha_composite(clip(sclera, np.where(blink_mask > 0, np.rint(opening * 255), 0)))
            for part in moving:
                frame.alpha_composite(clip(place(part, reference.size, center), blink_mask))
            frame.alpha_composite(lower)
            lid = cv2.remap(np.asarray(lash), xs, (ys - shift[None, :]).astype(np.float32), cv2.INTER_LINEAR)
            frame.alpha_composite(Image.fromarray(lid))

    output = OUT / "suiyue_eyes_hd_parts.psd"
    psd.save(output)
    reopened = PSDImage.open(output)
    parts = [layer for layer in reopened.descendants() if not layer.is_group()]
    if len(parts) != 12 or sum(layer.clipping for layer in parts) != 6:
        raise ValueError("Expected twelve eye layers and six layers clipped to sclera")
    tiles = [frame.crop((343, 140, 1405, 465)).resize((850, 260), Image.Resampling.LANCZOS)
             for frame in frames]
    review = Image.new("RGBA", (850, 780))
    for index, tile in enumerate(tiles):
        review.paste(tile, (0, index * 260))
    review.save(OUT / "suiyue_eyes_hd_gaze_review.png")
    blink_tiles = [frame.crop((343, 140, 1405, 465)).resize((850, 260), Image.Resampling.LANCZOS)
                   for frame in blinks]
    for index, tile in enumerate(blink_tiles):
        review.paste(tile, (0, index * 260))
    review.save(OUT / "suiyue_eyes_hd_blink_review.png")
    print(output)


if __name__ == "__main__":
    main()
