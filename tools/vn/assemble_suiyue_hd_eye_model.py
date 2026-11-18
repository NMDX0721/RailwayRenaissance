"""Register the HD eye layers to a 2× copy of Suiyue's full-body PSD."""
import argparse

from PIL import Image

from build_suiyue_hd_eye_layers import OUT, PSDImage, Group, PixelLayer

SCALE = 2
# Per-eye registration: study pivot, original portrait pivot, and source-pixel scale.
REGISTRATION = {
    "R": ((590, 265), (469, 224), (.171, .187)),
    "L": ((1174, 347), (563, 239), (.157, .194)),
}


def register(layer, side, compact):
    source, target, scale = REGISTRATION[side]
    if compact:
        target = (target[0] + (4 if side == "R" else -4), target[1])
        scale = (scale[0] * .91, scale[1] * .92)
    left, top, right, bottom = layer.bbox
    sx, sy = (v * SCALE for v in scale)
    x0 = round(target[0] * SCALE + (left - source[0]) * sx)
    y0 = round(target[1] * SCALE + (top - source[1]) * sy)
    art = layer.topil().convert("RGBA").resize(
        (max(1, round((right - left) * sx)), max(1, round((bottom - top) * sy))),
        Image.Resampling.LANCZOS)
    return art, x0, y0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--compact-eyes", action="store_true", help="Write a smaller, closer-set eye candidate")
    compact = parser.parse_args().compact_eyes
    source = PSDImage.open(OUT / "suiyue_head_eyes_face_fill_candidate.psd")
    eye_parts = PSDImage.open(OUT / "suiyue_eyes_hd_parts.psd")
    by_name = {layer.name: layer for layer in eye_parts}
    result = PSDImage.new("RGBA", (source.width * SCALE, source.height * SCALE), color=(0, 0, 0, 0))

    def copy(old, new):
        side = old.name[-1] if old.name in ("eye_R", "eye_L") else None
        if side:
            for layer in by_name[old.name]:
                art, x, y = register(layer, side, compact)
                part = PixelLayer.frompil(art, new, name=layer.name, left=x, top=y)
                part.clipping = layer.clipping
        for layer in old:
            if layer.is_group():
                part = Group.new(new, layer.name)
                copy(layer, part)
            elif side and not layer.name.endswith("_brow"):
                continue
            else:
                art = layer.topil().convert("RGBA")
                art = art.resize((art.width * SCALE, art.height * SCALE), Image.Resampling.LANCZOS)
                part = PixelLayer.frompil(art, new, name=layer.name, left=layer.left * SCALE, top=layer.top * SCALE)
                part.clipping = layer.clipping
            part.visible = layer.visible
            part.opacity = layer.opacity

    copy(source, result)
    stem = "suiyue_fullbody_hd_eyes_compact" if compact else "suiyue_fullbody_hd_eyes"
    target = OUT / f"{stem}_candidate.psd"
    result.save(target)
    reopened = PSDImage.open(target)
    layers = [item for item in reopened.descendants() if not item.is_group()]
    if len(layers) != 76 or sum(layer.clipping for layer in layers) != 6:
        raise ValueError(f"Unexpected imported layer structure: {len(layers)} layers")
    view = reopened.composite(force=True, ignore_preview=True, color=0, alpha=0, apply_icc=False).convert("RGBA")
    view.crop((790, 330, 1230, 580)).resize((880, 500), Image.Resampling.LANCZOS).save(
        OUT / f"{stem}_face.png")
    view.thumbnail((700, 900), Image.Resampling.LANCZOS)
    view.save(OUT / f"{stem}_preview.png")
    print(target, reopened.size, len(layers))


if __name__ == "__main__":
    main()
