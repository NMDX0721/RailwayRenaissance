"""Author facial details in the existing Live2D texture layers."""
import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

from fit_suiyue_iris_height import Group, OUT, PSDImage, PixelLayer, ROOT, TARGET


OUTPUT = OUT / "suiyue_fullbody_hd_face_polish_candidate.psd"
REVIEW = ROOT / "tools/vn/cubism_eye_rig_review/face_polish_psd_comparison.png"
PORTRAIT = ROOT / "tools/vn/character_art_20261106/suiyue_with_tablet_final.png"
FACE_VIEW = (760, 270, 1250, 805)
FEATURES = {"nose": (1023, 486, 1067, 538), "mouth_closed": (985, 544, 1073, 583)}
FAR_LIDS = {"eye_L_sclera", "eye_L_upper_lid", "eye_L_lower_lid"}
BROW_REGIONS = ((892, 341, 1030, 447), (1064, 355, 1190, 434))


def coordinates(layer):
    y, x = np.indices((layer.height, layer.width), dtype=np.float32)
    return x + layer.left, y + layer.top


def skin_gate(rgb):
    red, green, blue = rgb.astype(np.float32).transpose(2, 0, 1)
    return np.clip((red - 155) / 64, 0, 1) * np.clip((green - blue - 4) / 9, 0, 1)


def tint(rgba, color, weight):
    weight = np.clip(weight, 0, 1)[..., None]
    rgb = rgba[:, :, :3]
    rgb[:] = np.rint(rgb * (1 - weight) + np.asarray(color) * weight).clip(0, 255)


def blend_feature_base(rgba, x, y, bounds):
    left, top, right, bottom = bounds
    edge = np.minimum.reduce((x - left, right - x, y - top, bottom - y))
    rgb, alpha = rgba[:, :, :3], rgba[:, :, 3]
    red, green, blue = rgb.transpose(2, 0, 1)
    samples = ((edge < -2) & (edge >= -12) & (alpha >= 250) & (red >= 238) &
               (green >= 176) & (green <= 223) & (blue < 207))
    inside = edge >= 0
    points = np.stack((np.ones(x.shape), x - (left + right) / 2, y - (top + bottom) / 2), axis=-1)
    palette = np.linalg.lstsq(points[samples], rgb[samples], rcond=None)[0]
    weight = np.clip((edge[inside] + 1) / 5, 0, 1)[:, None]
    rgb[inside] = np.rint(rgb[inside] * (1 - weight) + (points[inside] @ palette) * weight).clip(0, 255)
    alpha[inside] = np.maximum(alpha[inside], np.rint(weight[:, 0] * 255))


def soften_brow_edges(rgba, x, y, bounds):
    left, top, right, bottom = bounds
    rgb = rgba[:, :, :3]
    green = rgb[:, :, 1].astype(np.float32)
    area = (x >= left) & (x < right) & (y >= top) & (y < bottom)
    tint(rgba, (252, 210, 191), area * np.clip((green - 221) / 21, 0, 1))


def face_paint(rgba, layer, bangs_shadow):
    x, y = coordinates(layer)
    rgb = rgba[:, :, :3]
    if layer.name == "face_base_visible":
        for bounds in FEATURES.values():
            blend_feature_base(rgba, x, y, bounds)
        for bounds in BROW_REGIONS:
            soften_brow_edges(rgba, x, y, bounds)
    skin = skin_gate(rgb)
    cheek = .22 * np.exp(-(((x - 949) / 35)**2 + ((y - 510) / 20)**2) / 2)
    cheek += .19 * np.exp(-(((x - 1131) / 32)**2 + ((y - 539) / 20)**2) / 2)
    tint(rgba, (237, 147, 130), cheek * skin)
    shadow = bangs_shadow[layer.top:layer.bottom, layer.left:layer.right]
    tint(rgba, (172, 120, 118), shadow * skin * .38 * np.clip((430 - y) / 22, 0, 1))


def feature_line(layer):
    scale = 4
    image = Image.new("RGBA", (layer.width * scale, layer.height * scale))
    draw = ImageDraw.Draw(image)

    def path(points, color):
        local = [((x - layer.left) * scale, (y - layer.top) * scale) for x, y in points]
        draw.polygon(local, fill=color)

    if layer.name == "nose":
        path([(1042, 497), (1047, 505), (1048, 512), (1044, 509), (1041, 502)], (173, 110, 101, 46))
        path([(1042, 512), (1047, 511), (1051, 514), (1048, 518), (1044, 516)], (150, 86, 77, 128))
        path([(1039, 510), (1043, 509), (1045, 512), (1041, 513)], (255, 234, 216, 126))
    else:
        path([(1000, 555), (1003, 555), (1008, 559), (1017, 562), (1025, 564),
              (1015, 564), (1006, 562), (1001, 559)], (108, 58, 65, 198))
        path([(1027, 564), (1037, 563), (1045, 561), (1052, 557), (1056, 556),
              (1057, 558), (1055, 560), (1046, 564), (1036, 565)], (111, 59, 66, 192))
        path([(1009, 570), (1020, 572), (1033, 572), (1045, 568), (1040, 573),
              (1028, 575), (1017, 574)], (208, 126, 122, 71))
    return image.resize((layer.width, layer.height), Image.Resampling.LANCZOS)


def eye_details(rgba, layer):
    y, x = np.indices(rgba.shape[:2], dtype=np.float32)
    if layer.name.endswith("_iris"):
        alpha = rgba[:, :, 3]
        y0, y1 = np.where(alpha >= 128)[0][[0, -1]]
        height = y1 - y0 + 1
        tint(rgba, (43, 32, 59), np.clip((y0 + height * .48 - y) / (height * .40), 0, 1) * .36)
        glint = .70 * np.exp(-(((x - layer.width * .29) / 1.2)**2 + ((y - (y0 + height * .69)) / 1.25)**2) / 2)
        tint(rgba, (245, 227, 245), glint * (alpha > 160))
    elif layer.name.endswith("_lower_lid"):
        alpha = rgba[:, :, 3]
        tint(rgba, (219, 155, 145), (alpha > 0) * .48)
        art = Image.fromarray(rgba)
        accents = Image.new("RGBA", art.size)
        draw = ImageDraw.Draw(accents)
        for fraction in ((.17, .25) if layer.name.startswith("eye_R") else (.76, .84)):
            px = round(layer.width * fraction)
            rows = np.flatnonzero(alpha[:, px] > 35)
            if rows.size:
                py = int(rows.max())
                dx = -3 if layer.name.startswith("eye_R") else 3
                draw.polygon([(px - 1, py - 2), (px + 2, py - 1), (px + dx, py + 2)], fill=(108, 67, 76, 157))
        rgba[:] = np.asarray(Image.alpha_composite(art, accents))
    elif layer.name.endswith("_brow") or layer.name.startswith("brow_"):
        rgb = rgba[:, :, :3].astype(np.float32)
        ink = np.clip((143 - rgb.max(axis=2)) / 73, 0, 1)
        tint(rgba, (77, 63, 86), ink * .64)
        x, y = coordinates(layer)
        soften_brow_edges(rgba, x, y, layer.bbox)


def finish(art, layer, bangs_shadow):
    if layer.name in FEATURES:
        return feature_line(layer)
    rgba = np.array(art)
    name = layer.name
    if name in {"face_base_visible", "face_under_eyes", "skin_under_antialiased_strands"}:
        face_paint(rgba, layer, bangs_shadow)
    elif name in {"facial_edge_pixels", "fixed_surface_fragments"}:
        x, y = coordinates(layer)
        for bounds in BROW_REGIONS:
            soften_brow_edges(rgba, x, y, bounds)
    elif name == "neck":
        x, y = coordinates(layer)
        boundary = 701 - 18 * ((x - 1030) / 95) ** 2 + .075 * (x - 1030)
        tint(rgba, (177, 110, 102), np.clip((boundary - y) / 2.2, 0, 1) * skin_gate(rgba[:, :, :3]) * .30)
    else:
        eye_details(rgba, layer)
    result = Image.fromarray(rgba)
    if name in FAR_LIDS:
        center = 1134 - layer.left
        width = .95
        result = result.transform(result.size, Image.Transform.AFFINE,
                                  (1 / width, 0, center * (1 - 1 / width), 0, 1, 0), Image.Resampling.BICUBIC)
    return result


def copy_layers(source, destination, bangs_shadow):
    for item in source:
        if item.is_group():
            layer = Group.new(destination, item.name)
            copy_layers(item, layer, bangs_shadow)
        else:
            art = finish(item.topil().convert("RGBA"), item, bangs_shadow)
            layer = PixelLayer.frompil(art, destination, item.name, item.top, item.left)
            layer.clipping = item.clipping
        layer.visible = item.visible
        layer.opacity = item.opacity


def composite(psd):
    return psd.composite(viewport=FACE_VIEW, force=True, ignore_preview=True, color=0, alpha=0,
                         apply_icc=False).convert("RGBA")


def save_comparison(source, candidate):
    board = Image.new("RGBA", (992, 577), "#28262c")
    draw = ImageDraw.Draw(board)
    font = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 19)
    for i, (label, psd) in enumerate((("精修前", source), ("精修后 · 分层稿", candidate))):
        x = i * 502
        draw.text((x + 10, 7), label, fill="white", font=font)
        board.alpha_composite(composite(psd), dest=(x, 37))
    board.save(REVIEW)


def main():
    source = PSDImage.open(TARGET)
    original = np.array(Image.open(PORTRAIT).convert("RGB").resize(source.size, Image.Resampling.BICUBIC))
    hair = ((original.max(axis=2) < 125) & (original[:, :, 2].astype(int) - original[:, :, 0] > -12)).astype(np.float32)
    hair[382:] = 0
    hair[328:382, :975] *= .65
    hair[328:382, 1140:] *= .65
    shift = np.float32([[1, 0, 6], [0, 1, 11]])
    shadow = cv2.warpAffine(hair, shift, source.size)
    shadow = cv2.GaussianBlur(shadow, (5, 5), .75)
    result = PSDImage.new("RGBA", source.size, color=(0, 0, 0, 0))
    copy_layers(source, result, shadow)
    result.save(OUTPUT)
    reopened = PSDImage.open(OUTPUT)
    changed = []
    for old, new in zip(source.descendants(), reopened.descendants(), strict=True):
        assert (old.name, old.bbox, old.visible, old.opacity, old.clipping) == (
            new.name, new.bbox, new.visible, new.opacity, new.clipping), old.name
        if not old.is_group() and old.topil().tobytes() != new.topil().tobytes():
            changed.append(old.name)
    assert {"nose", "mouth_closed", "neck", "eye_L_sclera", "eye_R_iris", "eye_L_iris"}.issubset(changed)
    save_comparison(source, reopened)
    print(OUTPUT, "changed:", ", ".join(changed))


if __name__ == "__main__":
    main()
