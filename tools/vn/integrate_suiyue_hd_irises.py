"""Fit image-generated anime irises into a review-only Suiyue PSD."""
from pathlib import Path
import argparse
import json
import sys

from PIL import Image
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "Library/VNPSDVerify"))
from psd_tools import PSDImage
from psd_tools.api.layers import Group, PixelLayer

OUT = ROOT / "tools/vn/suiyue_cubism_head_import/eye_underlay_repair"
GENERATED_IRIS = OUT / "hd_iris_assets/iris_hd_source.png"
GENERATED_SCLERA = OUT / "hd_iris_assets/sclera_L_hd_source.png"
SOURCES = {
    "eye_R_iris_complete": (GENERATED_IRIS, (455, 205, 484, 246)),
    # The independently generated L iris placed its pupil left of centre.
    # Reuse the centred R design to keep the pair coherent for this candidate.
    "eye_L_iris_complete": (GENERATED_IRIS, (541, 216, 570, 257)),
}
SCLERA_BOX = (539, 226, 590, 255)


def main(clean_sclera=False):
    source = PSDImage.open(OUT / "suiyue_head_eyes_face_fill_candidate.psd")
    sources = dict(SOURCES)
    # Keep the original sclera: its lower grey and pink pixels define the eyelid.
    result = PSDImage.new("RGBA", source.size, color=(0, 0, 0, 0))
    assets = OUT / "hd_iris_assets"
    assets.mkdir(exist_ok=True)
    sclera = {layer.name: layer for layer in source.descendants()
              if layer.name in {"eye_R_sclera_complete", "eye_L_sclera_complete"}}

    def fitted(path, box):
        original = Image.open(path).convert("RGBA")
        bounds = original.getchannel("A").getbbox()
        if bounds is None:
            raise ValueError(f"Empty generated eye asset: {path}")
        return original.crop(bounds).resize(
            (box[2]-box[0], box[3]-box[1]), Image.Resampling.LANCZOS)

    clean_sclera_color = np.asarray(Image.open(GENERATED_SCLERA).convert("RGBA"))[
        440, 887, :3].astype(np.float64) if clean_sclera else None

    def copy(group, target, preview_clip=False):
        for old in group:
            if old.is_group():
                new = Group.new(target, old.name)
                copy(old, new, preview_clip)
            elif old.name in sources:
                path, box = sources[old.name]
                if old.name == "eye_L_sclera_complete" and clean_sclera:
                    # Preserve the original eye aperture and Cubism clipping mask.
                    # The generated standalone almond is narrower than that mask.
                    array = np.asarray(old.topil().convert("RGBA")).copy()
                    original_rgb = array[:, :, :3].astype(np.float64)
                    yy = np.arange(array.shape[0], dtype=np.float64)[:, None, None]
                    strength = np.clip((yy - 3.0)/6.0, 0.0, 1.0)
                    dark = original_rgb.mean(axis=2, keepdims=True) < 225
                    repaired = np.where(dark,
                                        original_rgb*(1-strength) + clean_sclera_color[None, None, :]*strength,
                                        original_rgb)
                    array[:, :, :3] = np.rint(repaired).clip(0, 255).astype(np.uint8)
                    art = Image.fromarray(array)
                else:
                    art = fitted(path, box)
                art.save(assets / (old.name + ".png"))
                if preview_clip and "_iris_" in old.name:
                    eye = "R" if "_R_" in old.name else "L"
                    white = sclera[f"eye_{eye}_sclera_complete"]
                    mask = np.zeros((source.height, source.width), np.uint8)
                    mask[white.top:white.bottom, white.left:white.right] = np.asarray(
                        white.topil().convert("RGBA"))[:, :, 3]
                    array = np.asarray(art).copy()
                    local = mask[box[1]:box[3], box[0]:box[2]].astype(np.uint16)
                    array[:, :, 3] = ((array[:, :, 3].astype(np.uint16)*local+127)//255).astype(np.uint8)
                    art = Image.fromarray(array)
                new = PixelLayer.frompil(art, target, name=old.name, left=box[0], top=box[1])
            else:
                new = PixelLayer.frompil(old.topil().convert("RGBA"), target, name=old.name,
                                         left=old.left, top=old.top)
            if not old.is_group():
                new.clipping = old.clipping
            new.visible = old.visible
            new.opacity = old.opacity

    copy(source, result)
    prefix = "hd_anime_original_sclera" if clean_sclera else "hd_anime"
    path = OUT / ("suiyue_head_eyes_eyelid_candidate.psd" if clean_sclera else
                  "suiyue_head_eyes_hd_anime_candidate.psd")
    result.save(path)
    check = PSDImage.open(path)
    layers = [layer for layer in check.descendants() if not layer.is_group()]
    if len(layers) != 70:
        raise ValueError(f"Unexpected layer count: {len(layers)}")
    image = check.composite(force=True, ignore_preview=True, color=0,
                            alpha=0, apply_icc=False).convert("RGBA")
    image.save(OUT / f"{prefix}_open.png")
    image.crop((395, 170, 615, 280)).resize((880, 440), Image.Resampling.LANCZOS).save(
        OUT / f"{prefix}_open_crop.png")
    preview = PSDImage.new("RGBA", source.size, color=(0, 0, 0, 0))
    copy(source, preview, preview_clip=True)
    clipped_image = preview.composite(force=True, ignore_preview=True, color=0,
                                      alpha=0, apply_icc=False).convert("RGBA")
    clipped_image.crop((395, 170, 615, 280)).resize((880, 440), Image.Resampling.LANCZOS).save(
        OUT / f"{prefix}_clipped_crop.png")
    print(json.dumps({"psd": str(path), "layers": len(layers), "image": str(OUT / f"{prefix}_clipped_crop.png")},
                     ensure_ascii=False))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--clean-sclera", action="store_true")
    main(parser.parse_args().clean_sclera)
