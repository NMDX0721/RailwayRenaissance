"""Clean the stationary surface revealed by small front-strand movement."""
import json

import cv2
import numpy as np
from PIL import Image, ImageDraw

from rebuild_suiyue_head_contours import MOVERS, OUT, ROOT, PSDImage, insert, main, poly_mask, underpaint


REVIEW = ROOT / "tools/vn/suiyue_contact_polish"

CONTACT_PATHS = [
    [(474, 153), (507, 147), (506, 183), (488, 183), (474, 181)],
    [(551, 149), (583, 143), (587, 183), (560, 189)],
    [(410, 216), (432, 219), (435, 261), (416, 265), (409, 251)],
    [(590, 236), (610, 234), (611, 264), (604, 283), (586, 281)],
]


def polished_overlap(rgb, source, hair, donor):
    fill = underpaint(rgb, source, hair, donor)
    red, green, blue = rgb.astype(np.int16).transpose(2, 0, 1)
    skin = source & ~hair & (red >= 180) & (red - green >= 22) & (green - blue >= 6)
    # Stable midtones from the same surface. Separate ears from the cheeks so
    # a highlight from the opposite side cannot be extended across a strand.
    surfaces = [
        [(450, 146), (563, 143), (579, 181), (574, 188), (513, 195),
         (502, 188), (452, 181)],
        [(562, 174), (596, 170), (596, 187), (581, 188), (560, 193)],
        [(391, 208), (434, 213), (428, 249), (425, 270), (405, 268), (390, 246)],
        [(425, 232), (440, 232), (451, 255), (457, 265), (439, 275), (429, 265)],
        [(596, 231), (617, 233), (615, 267), (604, 283), (592, 279), (595, 250)],
        [(573, 252), (597, 244), (597, 273), (585, 288), (570, 285)],
    ]
    # Preserve brow strokes in the short spans that are genuinely shadowed.
    brow_r = poly_mask((rgb.shape[1], rgb.shape[0]), [
        [(449, 181), (480, 182), (512, 193), (512, 201), (480, 190), (449, 188)],
        [(546, 193), (571, 186), (596, 187), (598, 196), (572, 196), (547, 203)],
    ])
    changed = np.zeros_like(hair)
    for surface in surfaces:
        region = poly_mask((rgb.shape[1], rgb.shape[0]), [surface])
        region &= ~brow_r
        seed = (skin & region).astype(np.float32)
        if not seed.any():
            continue
        weight = cv2.GaussianBlur(seed, (11, 7), 2.25, sigmaY=1.15)
        weighted = cv2.GaussianBlur(rgb * seed[:, :, None], (11, 7), 2.25, sigmaY=1.15)
        estimate = weighted / np.maximum(weight[:, :, None], 1e-5)
        fill_red, _, fill_blue = fill.astype(np.int16).transpose(2, 0, 1)
        warm_fill = (fill_red >= 145) & (fill_red - fill_blue > 28)
        apply = hair & region & (weight >= .065) & warm_fill
        correction = np.clip(estimate[apply] - fill[apply], -32, 32)
        fill[apply] = np.rint(fill[apply] + .65 * correction).clip(0, 255).astype(np.uint8)
        changed |= apply
    print(f"CONTACT_BACKING_PIXELS={int(changed.sum())}", flush=True)
    return fill


def anchored_part(layer, displacement):
    pixels = np.asarray(layer.topil().convert("RGBA"), dtype=np.float32) / 255
    pixels[:, :, :3] *= pixels[:, :, 3:]
    pixels = np.pad(pixels, ((0, 0), (3, 3), (0, 0)))
    yy, xx = np.indices(pixels.shape[:2], dtype=np.float32)
    # Every front piece uses the same native-canvas displacement field.
    # It stays continuous where two source pieces share a boundary.
    t = np.clip((yy + layer.top - 150) / 85, 0, 1)
    weight = t * t * (3 - 2 * t)
    warped = cv2.remap(pixels, xx - displacement * weight, yy, cv2.INTER_LINEAR)
    alpha = warped[:, :, 3:]
    warped[:, :, :3] /= np.maximum(alpha, 1e-6)
    image = Image.fromarray(np.rint(warped * 255).clip(0, 255).astype(np.uint8))
    return image, (layer.left - 3, layer.top)


def render_anchored():
    path = REVIEW / "suiyue_head_contours.psd"
    model = PSDImage.open(path)
    parts = list(model.descendants())
    for part in parts:
        if part.name in {"00_hidden_surface_underlays", "local_surface_overlap"}:
            part.visible = True
    hair = [part for part in parts if not part.is_group()
            and part.name.removesuffix("_contour") in MOVERS]
    assert len(hair) == 8
    preview = Image.new("RGB", (1440, 382), (214, 213, 217))
    draw = ImageDraw.Draw(preview)
    for i, displacement in enumerate((0, -2, 2)):
        temporary = []
        if displacement:
            for layer in hair:
                parent = layer.parent
                image, offset = anchored_part(layer, displacement)
                moved = insert(parent, layer.name, image, offset)
                moved.opacity = layer.opacity
                parent.insert(parent.index(layer), moved)
                layer.visible = False
                temporary.append(moved)
        frame = model.composite(
            viewport=(390, 135, 630, 310), force=True, ignore_preview=True,
            color=0, alpha=0, apply_icc=False,
        ).convert("RGBA").resize((468, 341), Image.Resampling.LANCZOS)
        draw.text((i * 480 + 8, 8), f"Anchored root / tip {displacement:+d} px", fill="black")
        preview.paste(frame, (i * 480 + 6, 30), frame)
        for moved in temporary:
            moved.parent.remove(moved)
        for layer in hair:
            layer.visible = True
    preview.save(REVIEW / "root_anchored_review.png")
    receipt_path = REVIEW / "verification.json"
    receipt = json.loads(receipt_path.read_text(encoding="utf-8"))
    receipt["rootAnchoredRasterProbe"] = {"holdThroughY": 150, "fullTravelFromY": 235, "tipOffsets": [-2, 2]}
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main(output=REVIEW, overlap=polished_overlap, comparison=OUT, edge_paths=CONTACT_PATHS)
    render_anchored()
