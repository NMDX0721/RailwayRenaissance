"""Fit Suiyue's complete iris textures to their eye openings for PSD re-import."""
from PIL import Image, ImageDraw, ImageFont

from build_suiyue_hd_eye_layers import Group, OUT, PSDImage, PixelLayer, ROOT


REFERENCE = OUT / "suiyue_fullbody_hd_skin_candidate.psd"
TARGET = OUT / "suiyue_fullbody_hd_iris_fit_candidate.psd"
REVIEW = ROOT / "tools/vn/cubism_eye_rig_review/iris_height_comparison.png"
PARTS = {f"eye_{side}_{name}" for side in ("R", "L") for name in ("iris", "pupil")}
HIGHLIGHTS = {"eye_R_highlight", "eye_L_highlight"}


def visible_bounds(layer):
    left, top, right, bottom = layer.topil().getchannel("A").point(lambda a: 255 if a >= 128 else 0).getbbox()
    return (layer.left + left, layer.top + top, layer.left + right, layer.top + bottom)


def fit_parameters(source):
    layers = {layer.name: layer for layer in source.descendants()}
    fits = {}
    for side in ("R", "L"):
        _, iris_top, _, iris_bottom = visible_bounds(layers[f"eye_{side}_iris"])
        _, eye_top, _, eye_bottom = visible_bounds(layers[f"eye_{side}_sclera"])
        height = iris_bottom - iris_top
        # Keep a narrow amount of material past the baseline lid opening at neutral.
        target_height = round((eye_bottom - eye_top) * 1.12)
        fits[side] = ((iris_top + iris_bottom) / 2, target_height / height)
    return fits


def copy_layers(source, result, fits):
    for layer in source:
        if layer.is_group():
            copied = Group.new(result, layer.name)
            copy_layers(layer, copied, fits)
        else:
            art = layer.topil().convert("RGBA")
            if layer.name in PARTS:
                pivot, scale = fits[layer.name.split("_")[1]]
                if layer.name.endswith("_pupil"):
                    pivot = (layer.top + layer.bottom) / 2
                    scale = .88
                local_pivot = pivot - layer.top
                art = art.transform(art.size, Image.Transform.AFFINE,
                                    (1, 0, 0, 0, 1 / scale, local_pivot * (1 - 1 / scale)),
                                    Image.Resampling.BICUBIC)
            copied = PixelLayer.frompil(art, result, layer.name, layer.top, layer.left)
            copied.clipping = layer.clipping
        copied.opacity = layer.opacity
        copied.visible = layer.visible


def render_eye_strip(psd):
    return psd.composite(force=True, ignore_preview=True, color=0, alpha=0,
                         apply_icc=False).crop((815, 350, 1245, 550)).convert("RGBA")


def review(source, target):
    board = Image.new("RGB", (876, 476), "#28262c")
    draw = ImageDraw.Draw(board)
    font = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 18)
    for column, (label, psd) in enumerate((("原虹膜", source), ("压短后", target))):
        x = column * 442
        masked = render_eye_strip(psd)
        for layer in psd.descendants():
            if layer.name in PARTS or layer.name in HIGHLIGHTS:
                layer.clipping = False
        unmasked = render_eye_strip(psd)
        draw.text((x + 8, 4), f"{label} · 无剪贴", fill="white", font=font)
        draw.text((x + 8, 242), f"{label} · 正常剪贴", fill="white", font=font)
        board.paste(unmasked, (x, 29), unmasked)
        board.paste(masked, (x, 267), masked)
    board.save(REVIEW)


def main():
    source = PSDImage.open(REFERENCE)
    result = PSDImage.new("RGBA", source.size, color=(0, 0, 0, 0))
    copy_layers(source, result, fit_parameters(source))
    result.save(TARGET)
    reopened = PSDImage.open(TARGET)
    for old, new in zip(source.descendants(), reopened.descendants(), strict=True):
        assert (old.name, old.bbox, old.visible, old.opacity, old.clipping) == (
            new.name, new.bbox, new.visible, new.opacity, new.clipping)
        if old.is_group():
            continue
        same_pixels = old.topil().tobytes() == new.topil().tobytes()
        assert same_pixels == (old.name not in PARTS), old.name
        if old.name.endswith("_iris"):
            old_box = visible_bounds(old)
            new_box = visible_bounds(new)
            print(old.name, old_box[3] - old_box[1], "->", new_box[3] - new_box[1])
    review(source, reopened)
    print(TARGET)


if __name__ == "__main__":
    main()
