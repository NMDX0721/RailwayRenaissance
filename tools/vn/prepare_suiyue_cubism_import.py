"""Prepare the editable head prototype art for Cubism Editor import."""
import hashlib
import json
import re

import numpy as np
from PIL import Image, ImageCms

from calibrate_suiyue_ear_neck import OUT as COLOR_DIR
from rebuild_suiyue_head_contours import Group, PSDImage, ROOT, insert, render
from psd_tools.constants import ColorMode, Resource
from psd_tools.psd.image_resources import ImageResource

OUT = ROOT / "tools/vn/suiyue_cubism_head_import"
SOURCE = COLOR_DIR / "suiyue_ear_neck_calibrated.psd"
CLIPS = {"eye_R_iris_complete": "eye_R_sclera_complete", "eye_L_iris_complete": "eye_L_sclera_complete"}


def copy_import_layers(source, destination, parents=()):
    for layer in source:
        if not layer.visible:
            continue
        if layer.is_group():
            group = Group.new(destination, layer.name)
            copy_import_layers(layer, group, (*parents, layer.name))
            continue
        name = layer.name
        if parents and parents[-1] in {"eye_R", "eye_L"}:
            name = f"{parents[-1]}_{name}"
        part = insert(destination, name, layer.topil().convert("RGBA"), layer.offset)
        if part is None:
            continue
        part.opacity = layer.opacity
        # The full iris is retained as an independent ArtMesh. Cubism clipping
        # will be assigned against the corresponding sclera ArtMesh ID.
        if layer.clipping:
            assert name in CLIPS, name


def readback_import(path):
    project = PSDImage.open(path)
    layers = [layer for layer in project.descendants() if not layer.is_group()]
    names = [layer.name for layer in layers]
    assert len(names) == len(set(names)) and len(names) <= 100
    assert sum(layer.is_group() for layer in project.descendants()) <= 30
    assert all(re.fullmatch(r"[A-Za-z0-9_]+", name) for name in names)
    assert all(layer.is_visible() and not layer.clipping and not layer.has_mask() for layer in layers)
    assert project.depth == 8 and project.color_mode == ColorMode.RGB
    assert project.image_resources.get_data(Resource.ICC_PROFILE)
    for iris, mask in CLIPS.items():
        layer = layers[names.index(iris)]
        assert layer.parent[layer.parent.index(layer) - 1].name == mask
        layer.clipping = True
    return project, layers


def main():
    OUT.mkdir(exist_ok=True)
    source = PSDImage.open(SOURCE)
    target = PSDImage.new("RGBA", source.size, color=(0, 0, 0, 0))
    profile = ImageCms.ImageCmsProfile(ImageCms.createProfile("sRGB")).tobytes()
    target.image_resources[Resource.ICC_PROFILE] = ImageResource(key=Resource.ICC_PROFILE, data=profile)
    copy_import_layers(source, target)
    path = OUT / "suiyue_head_import.psd"
    target.save(path)

    preview_project, layers = readback_import(path)
    reconstructed = render(preview_project)
    original = Image.open(COLOR_DIR / "neutral.png").convert("RGBA")
    difference = np.abs(np.asarray(reconstructed).astype(np.int16) - np.asarray(original).astype(np.int16))
    assert difference[:, :, 3].max() == 0
    assert difference[:, :, :3].max() <= 1

    # Present the reconstructed import art on an actual 2.5K canvas. The
    # preview emulates the two eye clipping assignments for offline comparison.
    preview = Image.new("RGBA", (2560, 1440), (36, 33, 41, 255))
    cutout = reconstructed.crop(reconstructed.getbbox())
    cutout.thumbnail((1250, 1330), Image.Resampling.LANCZOS)
    preview.alpha_composite(cutout, ((2560 - cutout.width) // 2, (1440 - cutout.height) // 2))
    preview.convert("RGB").save(OUT / "neutral_masked_2560x1440.png")

    manifest = {
        "status": "PSD source preflight complete; Cubism Editor import pending",
        "sourceSha256": hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        "importPsdSha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "nativeCanvas": list(source.size),
        "reviewCanvas": [2560, 1440],
        "pixelLayerCount": len(layers),
        "groupCount": sum(layer.is_group() for layer in preview_project.descendants()),
        "colourMode": "RGB / 8 bits per channel / sRGB",
        "irisClippingAssignments": CLIPS,
        "neutralMaxRgbDeltaAfterOfflineClipping": int(difference[:, :, :3].max()),
        "neutralMaxAlphaDeltaAfterOfflineClipping": int(difference[:, :, 3].max()),
        "layersInBackToFrontOrder": [
            {"name": layer.name, "group": layer.parent.name, "bounds": list(layer.bbox)} for layer in layers
        ],
    }
    (OUT / "preflight.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(f"SUIYUE_IMPORT_PSD_PREFLIGHT_OK layers={len(layers)} groups={manifest['groupCount']}", flush=True)


if __name__ == "__main__":
    main()
