"""Fill the main face's old eye apertures, retaining the existing ArtMesh IDs."""
from pathlib import Path
import argparse
import hashlib
import json
import sys

import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "Library/VNPSDVerify"))
from psd_tools import PSDImage
from psd_tools.api.layers import Group, PixelLayer

from repair_suiyue_eye_mesh_colors import EYES, EYE_PARTS, SKIN, SOURCE

OUT = ROOT / "tools/vn/suiyue_cubism_head_import/eye_underlay_repair"
NAME = "suiyue_head_eyes_face_fill_candidate.psd"


def trim_duplicate_iris_rim(pixels, old, original):
    """Keep the painted lower iris edge; remove the generated second edge below it."""
    source = original[old.top:old.bottom, old.left:old.right]
    exact = (pixels[:, :, 3] > 0) & np.all(pixels[:, :, :3] == source[:, :, :3], axis=2)
    mid = pixels.shape[0] // 2
    bottom = np.full(pixels.shape[1], -1, np.int32)
    for x in range(pixels.shape[1]):
        ys = np.flatnonzero(exact[mid:, x])
        if ys.size:
            bottom[x] = mid + int(ys.max())
    valid = np.flatnonzero(bottom >= 0)
    if not valid.size:
        raise ValueError(f"No source-painted iris pixels in {old.name}")
    nearest = valid[np.argmin(np.abs(np.arange(len(bottom))[:, None] - valid[None, :]), axis=1)]
    bottom[bottom < 0] = bottom[nearest[bottom < 0]]
    yy = np.arange(pixels.shape[0])[:, None]
    donor_lower = (yy > bottom[None, :]) & (yy > mid) & (pixels[:, :, 3] > 0) & ~exact
    last = np.full(pixels.shape[1], 0, np.int32)
    for x in range(pixels.shape[1]):
        ys = np.flatnonzero(pixels[:, x, 3] > 0)
        if ys.size:
            last[x] = int(ys.max())
    span = np.maximum(1, last-bottom)
    progress = np.clip((yy-bottom[None, :]) / span[None, :], 0, 1)
    fade = 0.55 * np.square(1-progress)
    count = int(donor_lower.sum())
    pixels[:, :, 3][donor_lower] = np.rint(
        pixels[:, :, 3][donor_lower] * fade[donor_lower]).astype(np.uint8)
    return count


def main(trim_iris=False):
    OUT.mkdir(exist_ok=True)
    source = PSDImage.open(SOURCE)
    width, height = source.size
    clean = np.asarray(Image.open(SKIN).convert("RGBA"))
    original = np.asarray(Image.open(
        ROOT / "tools/vn/suiyue_ear_neck_color/neutral.png").convert("RGBA"))
    masks = {}
    colours = {}
    for side, vertices in EYES.items():
        mask = np.zeros((height, width), np.uint8)
        cv2.fillPoly(mask, [np.asarray(vertices, np.int32)], 255)
        masks[side] = mask > 0
        x, y, w, h = cv2.boundingRect(np.asarray(vertices, np.int32))
        sample = original[y+h+2:y+h+9, x+9:x+w-9, :3]
        donor = clean[y+h+2:y+h+9, x+9:x+w-9, :3]
        delta = np.median(sample.astype(np.int16)-donor.astype(np.int16), axis=(0, 1))
        colours[side] = np.clip(clean[:, :, :3].astype(np.int16)+delta, 0, 255).astype(np.uint8)
    result = PSDImage.new("RGBA", source.size, color=(0, 0, 0, 0))
    changed = {}

    def copy(old_group, new_group):
        for old in old_group:
            if old.is_group():
                new = Group.new(new_group, old.name)
                copy(old, new)
            else:
                pixels = np.asarray(old.topil().convert("RGBA")).copy()
                if old.name == "face_base_visible":
                    full = np.zeros((height, width, 4), np.uint8)
                    full[old.top:old.bottom, old.left:old.right] = pixels
                    before = full[:, :, 3].copy()
                    for side, mask in masks.items():
                        full[:, :, :3][mask] = colours[side][mask]
                        full[:, :, 3][mask] = 255
                    changed[old.name] = int(np.count_nonzero((full[:, :, 3] != before)))
                    image = Image.fromarray(full)
                    bounds = image.getbbox()
                    new = PixelLayer.frompil(image.crop(bounds), new_group, name=old.name,
                                             left=bounds[0], top=bounds[1])
                else:
                    if trim_iris and old.name in {"eye_R_iris_complete", "eye_L_iris_complete"}:
                        changed[old.name] = trim_duplicate_iris_rim(pixels, old, original)
                    if old.name in {"facial_edge_pixels", "fixed_surface_fragments"}:
                        count = 0
                        for mask in masks.values():
                            local = mask[old.top:old.bottom, old.left:old.right]
                            count += int(np.count_nonzero(local & (pixels[:, :, 3] > 0)))
                            pixels[local] = 0
                        changed[old.name] = count
                    image = Image.fromarray(pixels)
                    bounds = image.getbbox()
                    if bounds is None:
                        raise ValueError(f"Layer became empty: {old.name}")
                    new = PixelLayer.frompil(image.crop(bounds), new_group, name=old.name,
                                             left=old.left+bounds[0], top=old.top+bounds[1])
                new.clipping = old.clipping
            new.visible = old.visible
            new.opacity = old.opacity

    copy(source, result)
    path = OUT / ("suiyue_head_eyes_face_iris_candidate.psd" if trim_iris else NAME)
    result.save(path)
    reopened = PSDImage.open(path)
    layers = [x for x in reopened.descendants() if not x.is_group()]
    if len(layers) != 70:
        raise ValueError(f"ArtMesh count changed: {len(layers)}")
    opened = reopened.composite(force=True, ignore_preview=True, color=0,
                                alpha=0, apply_icc=False).convert("RGBA")
    prefix = "eye_face_iris" if trim_iris else "eye_face_fill"
    opened.save(OUT / f"{prefix}_open.png")
    difference = np.abs(np.asarray(opened).astype(np.int16)-original.astype(np.int16))
    review_region = np.zeros((height, width), bool)
    review_region[195:275, 420:610] = True
    outside_max = int(difference[~review_region].max())
    if outside_max > 1:
        raise ValueError(f"Unexpected outside-eye edit: {outside_max}")
    for layer in reopened.descendants():
        if not layer.is_group() and layer.name in EYE_PARTS:
            layer.visible = False
    closed = reopened.composite(force=True, ignore_preview=True, color=0,
                                alpha=0, apply_icc=False).convert("RGBA")
    closed.save(OUT / f"{prefix}_closed_alpha.png")
    for label, image in (("open", opened), ("closed", closed)):
        image.crop((395, 170, 615, 280)).resize((880, 440), Image.Resampling.NEAREST).save(
            OUT / f"{prefix}_{label}_crop.png")
    receipt = {"sourceSha256": hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
               "output": str(path), "outputSha256": hashlib.sha256(path.read_bytes()).hexdigest(),
               "layers": len(layers), "groups": sum(x.is_group() for x in reopened.descendants()),
               "changedPixels": changed, "outsideEyeMaxDelta": outside_max,
               "scope": "PSD candidate with opaque clean skin on main face behind eye meshes"}
    (OUT / ("face_iris_result.json" if trim_iris else "face_fill_result.json")).write_text(
        json.dumps(receipt, indent=2), encoding="utf-8")
    print("SUIYUE_EYE_FACE_IRIS_OK" if trim_iris else "SUIYUE_EYE_FACE_FILL_OK", changed)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--trim-iris", action="store_true")
    main(parser.parse_args().trim_iris)
