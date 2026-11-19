"""Measure eye-layer alpha centers in a Suiyue candidate PSD."""
from pathlib import Path
import sys

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
for name in ("iris_hd_source.png", "sclera_L_hd_source.png"):
    art = Image.open(ROOT / "tools/vn/suiyue_cubism_head_import/eye_underlay_repair/hd_iris_assets" / name).convert("RGBA")
    print(name, art.size, "alpha box", art.getchannel("A").getbbox(), "alpha range", art.getchannel("A").getextrema())
sys.path.insert(0, str(ROOT / "Library/VNPSDVerify"))
from psd_tools import PSDImage

path = ROOT / "tools/vn/suiyue_cubism_head_import/eye_underlay_repair/suiyue_head_eyes_hd_anime_candidate.psd"
psd = PSDImage.open(path)
layers = {layer.name: layer for layer in psd.descendants() if not layer.is_group()}
for layer in psd.descendants():
    if layer.name not in {"eye_R_iris_complete", "eye_L_iris_complete", "eye_R_sclera_complete", "eye_L_sclera_complete"}:
        continue
    a = np.asarray(layer.topil().convert("RGBA"))[:, :, 3].astype(np.float64)
    yy, xx = np.indices(a.shape)
    weight = a.sum()
    cx = float((xx * a).sum()/weight + layer.left)
    cy = float((yy * a).sum()/weight + layer.top)
    print(layer.name, "bbox", layer.bbox, "alpha-centroid", round(cx, 2), round(cy, 2))

for side in ("R", "L"):
    sclera = layers[f"eye_{side}_sclera_complete"]
    iris = layers[f"eye_{side}_iris_complete"]
    s = np.zeros((psd.height, psd.width), np.float64)
    i = np.zeros_like(s)
    s[sclera.top:sclera.bottom, sclera.left:sclera.right] = np.asarray(sclera.topil().convert("RGBA"))[:, :, 3]/255
    i[iris.top:iris.bottom, iris.left:iris.right] = np.asarray(iris.topil().convert("RGBA"))[:, :, 3]/255
    a = s*i
    yy, xx = np.indices(a.shape)
    print(side, "clipped-iris-centroid", round(float((xx*a).sum()/a.sum()), 2),
          round(float((yy*a).sum()/a.sum()), 2), "coverage", round(float(a.sum()), 2))
