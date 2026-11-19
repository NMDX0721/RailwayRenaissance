"""Repair baked eye contours without changing existing ArtMesh alpha footprints."""
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
SKIN = ROOT / "tools/vn/suiyue_layers_20261106/repairs/aligned_face_base_clean_v2.png"
OUT = ROOT / "tools/vn/suiyue_cubism_head_import/eye_underlay_repair"
NAME = "suiyue_head_eyes_recolor_candidate.psd"
TARGETS = {"face_under_eyes", "face_base_visible", "facial_edge_pixels", "fixed_surface_fragments"}
EYES = {
    "R": [(434, 207), (450, 205), (473, 205), (492, 212), (501, 221),
          (501, 235), (490, 245), (474, 249), (452, 246), (439, 235)],
    "L": [(530, 222), (552, 214), (575, 214), (594, 221), (600, 232),
          (598, 251), (581, 265), (550, 266), (535, 251)],
}
EYE_PARTS = {"eye_R_sclera_complete", "eye_R_iris_complete",
             "eye_L_sclera_complete", "eye_L_iris_complete"}


def main():
    OUT.mkdir(exist_ok=True)
    source = PSDImage.open(SOURCE)
    width, height = source.size
    clean = np.asarray(Image.open(SKIN).convert("RGBA"))
    original = np.asarray(Image.open(
        ROOT / "tools/vn/suiyue_ear_neck_color/neutral.png").convert("RGBA"))
    regions = {}
    colours = {}
    for side, vertices in EYES.items():
        mask = np.zeros((height, width), np.uint8)
        cv2.fillPoly(mask, [np.asarray(vertices, np.int32)], 255)
        regions[side] = mask > 0
        x, y, w, h = cv2.boundingRect(np.asarray(vertices, np.int32))
        sample = original[y+h+2:y+h+9, x+9:x+w-9, :3]
        donor = clean[y+h+2:y+h+9, x+9:x+w-9, :3]
        delta = np.median(sample.astype(np.int16)-donor.astype(np.int16), axis=(0, 1))
        colours[side] = np.clip(clean[:, :, :3].astype(np.int16)+delta, 0, 255).astype(np.uint8)
    result = PSDImage.new("RGBA", source.size, color=(0, 0, 0, 0))
    counts = {}

    def copy(old_group, new_group):
        for old in old_group:
            if old.is_group():
                new = Group.new(new_group, old.name)
                copy(old, new)
            else:
                pixels = np.asarray(old.topil().convert("RGBA")).copy()
                if old.name in TARGETS:
                    counts[old.name] = 0
                    for side, mask in regions.items():
                        local = mask[old.top:old.bottom, old.left:old.right] & (pixels[:, :, 3] > 0)
                        donor = colours[side][old.top:old.bottom, old.left:old.right]
                        counts[old.name] += int(local.sum())
                        pixels[:, :, :3][local] = donor[local]
                image = Image.fromarray(pixels)
                new = PixelLayer.frompil(image, new_group, name=old.name,
                                         left=old.left, top=old.top)
                new.clipping = old.clipping
                if new.bbox != old.bbox:
                    raise ValueError(f"ArtMesh bounds changed: {old.name}")
            new.visible = old.visible
            new.opacity = old.opacity

    copy(source, result)
    path = OUT / NAME
    result.save(path)
    reopened = PSDImage.open(path)
    layers = [layer for layer in reopened.descendants() if not layer.is_group()]
    if len(layers) != 70:
        raise ValueError(f"Unexpected layer count: {len(layers)}")
    opened = reopened.composite(force=True, ignore_preview=True, color=0,
                                alpha=0, apply_icc=False).convert("RGBA")
    opened.save(OUT / "eye_recolor_open.png")
    changed = np.abs(np.asarray(opened).astype(np.int16)-original.astype(np.int16))
    review_region = np.zeros((height, width), bool)
    review_region[195:275, 420:610] = True
    outside_max = int(changed[~review_region].max())
    if outside_max > 1:
        raise ValueError(f"Unexpected outside-eye edit: {outside_max}")
    for layer in reopened.descendants():
        if not layer.is_group() and layer.name in EYE_PARTS:
            layer.visible = False
    closed = reopened.composite(force=True, ignore_preview=True, color=0,
                                alpha=0, apply_icc=False).convert("RGBA")
    closed.save(OUT / "eye_recolor_closed_alpha.png")
    for label, image in (("open", opened), ("closed", closed)):
        image.crop((395, 170, 615, 280)).resize((880, 440), Image.Resampling.NEAREST).save(
            OUT / f"eye_recolor_{label}_crop.png")
    receipt = {"sourceSha256": hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
               "output": str(path), "outputSha256": hashlib.sha256(path.read_bytes()).hexdigest(),
               "layers": len(layers), "groups": sum(x.is_group() for x in reopened.descendants()),
               "recoloredPixels": counts, "outsideEyeMaxDelta": outside_max,
               "scope": "PSD candidate preserving every ArtMesh alpha footprint; Cubism reimport pending"}
    (OUT / "recolor_result.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    print("SUIYUE_EYE_MESH_RECOLOR_OK", counts)


if __name__ == "__main__":
    main()
