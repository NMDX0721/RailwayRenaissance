"""Copy the HD rig source PSD with localized, alpha-preserving skin calibration."""
import numpy as np
from PIL import Image

from build_suiyue_hd_eye_layers import Group, OUT, PSDImage, PixelLayer


FACE = (-1, -5, -4)
NECK = (-3, -16, -15)
NECK_SHADOW = (-23, -2, -5)
EAR = (3, 6, 6)
SKIN_DELTA = {
    "face_base_visible": FACE,
    "face_under_eyes": FACE,
    "skin_under_antialiased_strands": FACE,
    "nose": FACE,
    "mouth_closed": FACE,
    "neck": NECK,
    "ear_L": EAR,
    "ear_L_continuous_surface": EAR,
    "ear_L_contour": EAR,
}


def calibrate(art, name):
    rgba = np.array(art)
    rgb = rgba[:, :, :3].astype(np.float32)
    red, green, blue = rgb.transpose(2, 0, 1)
    strength = np.clip((red - 156) / 63, 0, 1)
    strength *= np.clip((green - 90) / 56, 0, 1)
    strength *= np.clip((red - green - 12) / 15, 0, 1)
    strength *= np.clip((green - blue - 3) / 7, 0, 1)
    strength[rgba[:, :, 3] == 0] = 0
    update = np.array(SKIN_DELTA[name], dtype=np.float32)
    if name == "neck":
        light = np.clip((green - 165) / 57, 0, 1)[:, :, None]
        update = NECK_SHADOW * (1 - light) + update * light
    rgba[:, :, :3] = np.rint(rgb + strength[:, :, None] * update).clip(0, 255).astype(np.uint8)
    return Image.fromarray(rgba)


def copy_layers(original, result):
    for layer in original:
        if layer.is_group():
            copied = Group.new(result, layer.name)
            copy_layers(layer, copied)
        else:
            art = layer.topil().convert("RGBA")
            if layer.name in SKIN_DELTA:
                art = calibrate(art, layer.name)
            copied = PixelLayer.frompil(art, result, layer.name, layer.top, layer.left)
            copied.clipping = layer.clipping
        copied.opacity = layer.opacity
        copied.visible = layer.visible


def main():
    source = PSDImage.open(OUT / "suiyue_fullbody_hd_eyes_candidate.psd")
    target = OUT / "suiyue_fullbody_hd_skin_candidate.psd"
    result = PSDImage.new("RGBA", source.size, color=(0, 0, 0, 0))
    copy_layers(source, result)
    result.save(target)
    reopened = PSDImage.open(target)
    changed = []
    for old, new in zip(source.descendants(), reopened.descendants(), strict=True):
        assert (old.name, old.bbox, old.visible, old.opacity, old.clipping) == (
            new.name, new.bbox, new.visible, new.opacity, new.clipping)
        if old.is_group():
            continue
        old_art, new_art = (layer.topil().convert("RGBA") for layer in (old, new))
        assert old_art.getchannel("A").tobytes() == new_art.getchannel("A").tobytes(), old.name
        if old_art.tobytes() != new_art.tobytes():
            assert old.name in SKIN_DELTA, old.name
            changed.append(old.name)
    assert {"face_base_visible", "face_under_eyes", "neck"}.issubset(changed), changed
    print(target, "changed:", ", ".join(changed))


if __name__ == "__main__":
    main()
