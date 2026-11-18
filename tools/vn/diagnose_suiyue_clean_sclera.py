"""Inspect why the generated right-eye sclera candidate composes incorrectly."""
from pathlib import Path
import sys

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "Library/VNPSDVerify"))
from psd_tools import PSDImage

folder = ROOT / "tools/vn/suiyue_cubism_head_import/eye_underlay_repair"
generated = Image.open(folder / "hd_iris_assets/sclera_L_hd_source.png").convert("RGBA")
print("generated", generated.size, "center", generated.getpixel((generated.width//2, generated.height//2)))
print("generated alpha box", generated.getchannel("A").getbbox())
for name in ("suiyue_head_eyes_hd_anime_candidate.psd", "suiyue_head_eyes_sclera_repair_candidate.psd"):
    psd = PSDImage.open(folder / name)
    print("PSD", name)
    for index, layer in enumerate(psd.descendants()):
        if layer.is_group() or layer.name not in {"eye_L_sclera_complete", "eye_L_iris_complete", "face_base_visible", "face_under_eyes", "eye_L_lid_upper", "eye_L_lid_lower"}:
            continue
        art = layer.topil().convert("RGBA")
        a = np.asarray(art)[:, :, 3]
        print(index, layer.name, layer.bbox, "visible", layer.visible, "opacity", layer.opacity,
              "clipping", layer.clipping, "alpha", a.min(), a.max(), "alpha pixels", int((a>0).sum()),
              "center", art.getpixel((art.width//2, art.height//2)))

base = np.asarray(Image.open(folder / "hd_anime_open.png").convert("RGBA"))
fixed = np.asarray(Image.open(folder / "hd_anime_clean_sclera_open.png").convert("RGBA"))
difference = np.max(np.abs(base.astype(np.int16)-fixed.astype(np.int16)), axis=2)
changed = difference > 0
ys, xs = np.nonzero(changed)
print("composition diff", int(changed.sum()),
      "bbox", (int(xs.min()), int(ys.min()), int(xs.max()+1), int(ys.max()+1)))
print("outside right sclera", int(changed[~np.pad(np.ones((29, 51), bool),
      ((226, base.shape[0]-255), (539, base.shape[1]-590)))].sum()))
