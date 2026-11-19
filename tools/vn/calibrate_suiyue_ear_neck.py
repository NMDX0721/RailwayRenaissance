"""Match ear and neck materials to the approved colour reference."""
import hashlib
import json

import cv2
import numpy as np
from PIL import Image, ImageDraw

from calibrate_suiyue_skin import POINTS as FACE_POINTS, REFERENCE, sample
from rebuild_suiyue_ear_occlusion import OUT as EAR_DIR, probes
from rebuild_suiyue_head_contours import Group, PSDImage, ROOT, insert, poly_mask, render

OUT = ROOT / "tools/vn/suiyue_ear_neck_color"
SOURCE = EAR_DIR / "suiyue_ear_occlusion.psd"
AREAS = {
    "screen_left_ear": {
        "outline": [(383, 199), (420, 204), (428, 226), (432, 251),
                    (435, 270), (406, 275), (385, 258)],
        "anchors": [
            ("rim", (360, 310), (394, 227)),
            ("inner", (368, 314), (402, 234)),
            ("lobe", (391, 345), (417, 257)),
        ],
    },
    "screen_right_ear": {
        "outline": [(599, 228), (625, 234), (626, 268), (610, 290),
                    (568, 292), (567, 276), (580, 261), (592, 249)],
        "anchors": [
            ("rim", (648, 365), (606, 259)),
            ("lobe", (609, 387), (577, 276)),
        ],
    },
    "neck": {
        "outline": [(462, 303), (473, 304), (499, 317), (522, 320),
                    (547, 307), (562, 310), (556, 344), (580, 373),
                    (587, 404), (465, 409), (463, 352)],
        "anchors": [
            ("shadow", (479, 447), (498, 332)),
            ("light", (508, 501), (520, 364)),
        ],
    },
}


def colour_field(size, reference, before):
    width, height = size
    yy, xx = np.indices((height, width))
    field = np.zeros((height, width, 3), dtype=np.float32)
    domain = np.zeros((height, width), dtype=bool)
    samples = []
    for area, setup in AREAS.items():
        mask = poly_mask(size, [setup["outline"]])
        distance = cv2.distanceTransform(mask.astype(np.uint8), cv2.DIST_L2, 5)
        feather = np.clip(distance / 5, 0, 1)
        numerator = np.zeros_like(field)
        denominator = np.zeros_like(distance)
        for name, ref_point, art_point in setup["anchors"]:
            target = sample(reference, ref_point)
            initial = sample(before, art_point)
            x, y = art_point
            weight = 1 / (1 + ((xx - x) ** 2 + (yy - y) ** 2) / 20) ** 2
            numerator += weight[:, :, None] * (target - initial)
            denominator += weight
            samples.append({
                "area": area, "material": name,
                "referencePoint": ref_point, "workingPoint": art_point,
                "target": target.astype(int).tolist(), "before": initial.astype(int).tolist(),
            })
        field += numerator / np.maximum(denominator[:, :, None], 1e-12) * feather[:, :, None]
        domain |= mask
    return field, domain, samples


def copy_colours(source, destination, field):
    for old in source:
        if old.is_group():
            new = Group.new(destination, old.name)
            copy_colours(old, new, field)
        else:
            pixels = np.array(old.topil().convert("RGBA"))
            rgb = pixels[:, :, :3].astype(np.float32)
            red, green, blue = rgb.transpose(2, 0, 1)
            # Fade the adjustment through dark contour pixels and preserve the
            # local warm shadows. Hair and the red trim fail the skin gates.
            strength = np.clip((red - 129) / 60, 0, 1)
            strength *= np.clip((green - 62) / 45, 0, 1)
            strength *= np.clip((red - green - 12) / 10, 0, 1)
            strength *= np.clip((green - blue - 2) / 7, 0, 1)
            strength[pixels[:, :, 3] == 0] = 0
            shift = field[old.top:old.bottom, old.left:old.right]
            pixels[:, :, :3] = np.rint(rgb + strength[:, :, None] * shift).clip(0, 255).astype(np.uint8)
            new = insert(destination, old.name, Image.fromarray(pixels), old.offset)
            if new is None:
                continue
            new.clipping = old.clipping
        new.visible = old.visible
        new.opacity = old.opacity


def comparison(reference, before, after, probes_before, probes_after):
    sheet = Image.new("RGB", (1392, 1020), (43, 40, 47))
    draw = ImageDraw.Draw(sheet)
    reference_crop = reference.crop((333, 175, 667, 545))
    art_crop = (379, 135, 632, 409)
    for i, (name, image) in enumerate((
        ("Approved palette", reference_crop), ("Previous PSD", before.crop(art_crop)),
        ("Ear and neck calibrated", after.crop(art_crop)),
    )):
        x = i * 464
        draw.text((x + 12, 10), name, fill="white")
        tile = image.convert("RGBA").resize((436, 474), Image.Resampling.LANCZOS)
        sheet.paste(tile, (x + 12, 30), tile)
    for i, (name, frame) in enumerate((
        ("Previous / tip +2 px", probes_before[1]), ("Calibrated / tip -2 px", probes_after[0]),
        ("Calibrated / tip +2 px", probes_after[1]),
    )):
        x = i * 464
        draw.text((x + 12, 550), name, fill="white")
        tile = frame.resize((436, 327), Image.Resampling.LANCZOS)
        sheet.paste(tile, (x + 12, 575), tile)
    sheet.crop((0, 0, 1392, 922)).save(OUT / "palette_comparison.png")


def main():
    OUT.mkdir(exist_ok=True)
    reference = Image.open(REFERENCE).convert("RGB")
    before = Image.open(EAR_DIR / "neutral.png").convert("RGBA")
    source = PSDImage.open(SOURCE)
    field, domain, samples = colour_field(source.size, reference, before)
    corrected = PSDImage.new("RGBA", source.size, color=(0, 0, 0, 0))
    copy_colours(source, corrected, field)
    path = OUT / "suiyue_ear_neck_calibrated.psd"
    corrected.save(path)
    reopened = PSDImage.open(path)
    after = render(reopened)
    delta = np.abs(np.asarray(after).astype(np.int16) - np.asarray(before).astype(np.int16))
    assert delta[:, :, 3].max() == 0
    assert delta[~domain].max() == 0
    for _, _, xy in FACE_POINTS:
        assert np.array_equal(sample(before, xy), sample(after, xy))
    before_error = 0
    after_error = 0
    for record in samples:
        actual = sample(after, record["workingPoint"]).astype(int)
        record["after"] = actual.tolist()
        before_error += int(np.abs(np.array(record["before"]) - record["target"]).sum())
        after_error += int(np.abs(actual - record["target"]).sum())
    assert after_error < before_error / 2, (before_error, after_error)
    after.save(OUT / "neutral.png")
    prior_motion = probes(source, anchored=True)
    revised_motion = probes(reopened, anchored=True)
    for old, new in zip(prior_motion, revised_motion):
        assert np.array_equal(np.asarray(old)[:, :, 3], np.asarray(new)[:, :, 3])
    comparison(reference, before, after, prior_motion, revised_motion)
    receipt = {
        "referenceSha256": hashlib.sha256(REFERENCE.read_bytes()).hexdigest(),
        "sourceSha256": hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        "psdSha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "layers": sum(not part.is_group() for part in reopened.descendants()),
        "samples": samples,
        "sampleAbsoluteErrorBefore": before_error,
        "sampleAbsoluteErrorAfter": after_error,
        "outsideColourMaskMaxDelta": int(delta[~domain].max()),
        "alphaMaxDelta": int(delta[:, :, 3].max()),
    }
    (OUT / "verification.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    print(json.dumps(receipt), flush=True)


if __name__ == "__main__":
    main()
