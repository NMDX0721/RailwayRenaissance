"""Fixed-camera daylight lamp comparison; these image values are not lux."""
import json
from pathlib import Path
from PIL import Image, ImageStat

base = Path(__file__).resolve().parent
results = {}
for preset in ("Clear", "Overcast", "LowSun"):
    pair = {}
    for state in ("off", "on"):
        with Image.open(base / f"interior_review/day_{preset}_lamps_{state}.png") as image:
            pair[state] = ImageStat.Stat(image.convert("L").crop((150, 400, 1250, 950))).mean[0]
    assert pair["on"] > pair["off"] + 2, (preset, pair)
    results[preset] = pair
output = json.dumps(results, indent=2)
(base / "lighting_audit/daylight_pairs.json").write_text(output + "\n", encoding="utf-8")
print(output)
