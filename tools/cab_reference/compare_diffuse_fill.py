"""Compare the same lower-cabin ROI; grayscale is not a lux measurement."""
import json
from pathlib import Path
from PIL import Image, ImageStat

base = Path(__file__).resolve().parent
results = {}
for name, path in {
    "point_fill": base / "lighting_audit/diffuse_previous.png",
    "diffuse_fill": base / "interior_review/passenger_lights_on.png",
}.items():
    with Image.open(path) as source:
        image = source.convert("RGB").crop((150, 650, 1250, 1000))
        pixels = list(image.get_flattened_data())
        results[name] = {
            "lower_mean": ImageStat.Stat(image.convert("L")).mean[0],
            "near_white_fraction": sum(min(c) > 240 for c in pixels) / len(pixels),
        }
assert results["diffuse_fill"]["near_white_fraction"] < results["point_fill"]["near_white_fraction"]
assert results["diffuse_fill"]["lower_mean"] > 65.0  # Readability guard above the earlier 52.11/255 baseline.
output = json.dumps(results, indent=2)
(base / "lighting_audit/diffuse_comparison.json").write_text(output + "\n", encoding="utf-8")
print(output)
