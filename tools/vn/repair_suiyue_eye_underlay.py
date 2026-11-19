"""Build a separate Suiyue PSD candidate with old eye contours removed.

The original imported PSD and editable Cubism model are intentionally preserved.
This script produces raster previews before any Cubism reimport is attempted.
"""
from pathlib import Path
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

SOURCE = ROOT / "tools/vn/suiyue_cubism_head_import/suiyue_head_import.psd"
OUT = ROOT / "tools/vn/suiyue_cubism_head_import/eye_underlay_repair"
SKIN = ROOT / "tools/vn/suiyue_layers_20261106/repairs/aligned_face_base_clean_v2.png"
TARGETS = {"face_under_eyes", "face_base_visible", "facial_edge_pixels", "fixed_surface_fragments"}
EYES = {
    "R": [(434, 207), (450, 205), (473, 205), (492, 212), (501, 221),
          (501, 235), (490, 245), (474, 249), (452, 246), (439, 235)],
    "L": [(530, 222), (552, 214), (575, 214), (594, 221), (600, 232),
          (598, 251), (581, 265), (550, 266), (535, 251)],
}


def mask_for(size):
    width, height = size
    mask = np.zeros((height, width), np.uint8)
    for vertices in EYES.values():
        cv2.fillPoly(mask, [np.asarray(vertices, dtype=np.int32)], 255)
    return mask > 0


def add_layer(parent, name, pixels, offset):
    image = Image.fromarray(pixels)
    bounds = image.getbbox()
    if bounds is None:
        return None
    return PixelLayer.frompil(image.crop(bounds), parent, name=name,
                              left=offset[0] + bounds[0], top=offset[1] + bounds[1])


def main():
    OUT.mkdir(exist_ok=True)
    source = PSDImage.open(SOURCE)
    mask = mask_for(source.size)
    clean = np.asarray(Image.open(SKIN).convert("RGBA"))
    # Render the old static contours with movable eye art hidden. The repair
    # pixels are folded into the existing facial_edge_pixels layer, preserving
    # Cubism's source-layer mapping and the original 70 ArtMeshes.
    for layer in source.descendants():
        if not layer.is_group() and layer.name in {
            "eye_R_sclera_complete", "eye_R_iris_complete",
            "eye_L_sclera_complete", "eye_L_iris_complete",
        }:
            layer.visible = False
    static = np.asarray(source.composite(force=True, ignore_preview=True, color=0,
                                         alpha=0, apply_icc=False).convert("RGBA"))
    inpainted = clean[:, :, :3].copy()
    for side, vertices in EYES.items():
        region = np.zeros(mask.shape, np.uint8)
        cv2.fillPoly(region, [np.asarray(vertices, dtype=np.int32)], 255)
        x, y, width, height = cv2.boundingRect(np.asarray(vertices, dtype=np.int32))
        sample = static[y + height + 2:y + height + 9, x + 9:x + width - 9, :3]
        donor = clean[y + height + 2:y + height + 9, x + 9:x + width - 9, :3]
        delta = np.median(sample.astype(np.int16) - donor.astype(np.int16), axis=(0, 1))
        expanded = cv2.dilate(region, np.ones((9, 9), np.uint8))
        adjusted = np.clip(clean[:, :, :3].astype(np.int16) + delta, 0, 255).astype(np.uint8)
        inpainted[expanded > 0] = adjusted[expanded > 0]
    patch = np.zeros_like(static)
    patch[:, :, :3] = inpainted
    expanded = cv2.dilate(mask.astype(np.uint8) * 255, np.ones((9, 9), np.uint8))
    alpha = cv2.GaussianBlur(expanded, (0, 0), 3)
    alpha[mask] = 255
    patch[:, :, 3] = alpha
    for layer in source.descendants():
        if not layer.is_group() and layer.name in {
            "eye_R_sclera_complete", "eye_R_iris_complete",
            "eye_L_sclera_complete", "eye_L_iris_complete",
        }:
            layer.visible = True
    result = PSDImage.new("RGBA", source.size, color=(0, 0, 0, 0))
    counts = {}

    def copy_layers(old_group, new_group):
        for old in old_group:
            if old.is_group():
                new = Group.new(new_group, old.name)
                copy_layers(old, new)
            else:
                pixels = np.asarray(old.topil().convert("RGBA")).copy()
                if old.name in TARGETS:
                    region = mask[old.top:old.bottom, old.left:old.right]
                    counts[old.name] = int(np.count_nonzero(region & (pixels[:, :, 3] > 0)))
                    pixels[region] = 0
                if old.name == "facial_edge_pixels":
                    full = Image.new("RGBA", source.size)
                    full.alpha_composite(Image.fromarray(pixels), old.offset)
                    full.alpha_composite(Image.fromarray(patch))
                    new = add_layer(new_group, old.name, np.asarray(full), (0, 0))
                else:
                    new = add_layer(new_group, old.name, pixels, old.offset)
                if new is None:
                    raise ValueError(f"Layer became empty: {old.name}")
                new.clipping = old.clipping
            new.visible = old.visible
            new.opacity = old.opacity

    copy_layers(source, result)
    path = OUT / "suiyue_head_eyes_clean_candidate.psd"
    result.save(path)
    reopened = PSDImage.open(path)
    layer_count = sum(not layer.is_group() for layer in reopened.descendants())
    group_count = sum(layer.is_group() for layer in reopened.descendants())
    if (layer_count, group_count) != (70, 8):
        raise ValueError(f"ArtMesh mapping changed: {(layer_count, group_count)}")
    opened = reopened.composite(force=True, ignore_preview=True, color=0, alpha=0,
                                apply_icc=False).convert("RGBA")
    opened.save(OUT / "eye_open.png")
    old_neutral = np.asarray(Image.open(
        ROOT / "tools/vn/suiyue_ear_neck_color/neutral.png").convert("RGBA"))
    changed = np.abs(np.asarray(opened).astype(np.int16) - old_neutral.astype(np.int16))
    review_region = np.zeros(mask.shape, bool)
    review_region[195:275, 420:610] = True
    outside_max = int(changed[~review_region].max())
    if outside_max > 1:
        raise ValueError(f"Unexpected edit outside eyes: {outside_max}")
    for layer in reopened.descendants():
        if not layer.is_group() and layer.name in {
            "eye_R_sclera_complete", "eye_R_iris_complete",
            "eye_L_sclera_complete", "eye_L_iris_complete",
        }:
            layer.visible = False
    closed = reopened.composite(force=True, ignore_preview=True, color=0, alpha=0,
                                apply_icc=False).convert("RGBA")
    closed.save(OUT / "eye_closed_alpha.png")
    for name, image in (("open", opened), ("closed", closed)):
        image.crop((395, 170, 615, 280)).resize((880, 440), Image.Resampling.NEAREST).save(
            OUT / f"eye_{name}_crop.png")
    (OUT / "result.json").write_text(json.dumps({"source": str(SOURCE),
        "sourceSha256": hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        "output": str(path), "outputSha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "layerCount": layer_count, "groupCount": group_count,
        "removedRegionPixels": counts, "outsideEyeMaxDelta": outside_max,
        "scope": "PSD raster candidate only; Cubism model not updated"}, indent=2), encoding="utf-8")
    print("SUIYUE_EYE_UNDERLAY_CANDIDATE_OK", counts)


if __name__ == "__main__":
    main()
