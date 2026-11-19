"""Draw the low-contrast outdoor terrain set for the Wufeng station map."""

from pathlib import Path

from PIL import Image, ImageDraw


SIZE = 128
DESTINATION = Path(__file__).resolve().parents[2] / "Assets/Resources/Art/Explore/TilesWufeng"
GRASS = "#667f4d"
DIRT = "#ae8d64"


def canvas(color):
    image = Image.new("RGB", (SIZE, SIZE), color)
    return image, ImageDraw.Draw(image)


def save(image, name):
    image.save(DESTINATION / f"tile_{name}.png", optimize=True)


def tuft(draw, x, y):
    draw.polygon([(x - 8, y), (x + 7, y), (x + 11, y + 3), (x - 7, y + 3)], fill="#5f7548")
    draw.line([(x, y + 1), (x - 2, y - 5), (x - 6, y - 8)], fill="#748952", width=2)
    draw.line([(x + 2, y), (x + 3, y - 8), (x + 6, y - 11)], fill="#7a9058", width=2)
    draw.line([(x + 4, y + 1), (x + 8, y - 5)], fill="#728751", width=2)


def grass():
    # All variants share the base tone at the border, including when mixed.
    patches = (
        ([(14, 21), (32, 14), (62, 17), (76, 29), (88, 34), (83, 47), (65, 54), (35, 50), (19, 41)], (41, 38)),
        ([(47, 72), (64, 64), (94, 65), (115, 79), (108, 100), (86, 108), (60, 103), (40, 93)], (86, 91)),
        ([(23, 60), (41, 53), (65, 62), (74, 75), (66, 90), (49, 97), (25, 90), (17, 77)], (43, 80)),
    )
    for index, (patch, position) in enumerate(patches, 1):
        image, draw = canvas(GRASS)
        draw.polygon(patch, fill="#6a824e" if index != 2 else "#627b4a")
        if index != 3:
            tuft(draw, *position)
        save(image, f"ground_grass_{index:02d}")


def dirt():
    features = ((31, 47), (75, 82), (52, 95))
    for index, (x, y) in enumerate(features, 1):
        image, draw = canvas(DIRT)
        draw.polygon([(x - 17, y - 2), (x - 9, y - 8), (x + 12, y - 8), (x + 24, y - 3),
                      (x + 20, y + 2), (x + 6, y + 5), (x - 11, y + 4)], fill="#b29168")
        draw.line([(x - 9, y + 13), (x + 1, y + 13), (x + 6, y + 11)], fill="#a88760", width=2)
        if index == 2:
            draw.rectangle((x + 16, y + 17, x + 20, y + 19), fill="#aa875f")
        save(image, f"ground_dirt_{index:02d}")


def platform(edge):
    image, draw = canvas("#c2b49c")
    for y in (0, 40, 80, 120):
        draw.line((0, y, SIZE - 1, y), fill="#a99b85", width=2)
    for y, joints in ((0, (39, 101)), (40, (21, 81)), (80, (39, 101))):
        for x in joints:
            draw.line((x, y + 1, x, y + 39), fill="#ad9f89", width=2)
    if edge:
        draw.rectangle((0, 94, 127, 98), fill="#c7a046")
        draw.rectangle((0, 99, 127, 101), fill="#b38f3f")
        draw.rectangle((0, 113, 127, 116), fill="#d0c1a4")
        draw.rectangle((0, 117, 127, 127), fill="#8d806d")
    save(image, "platform_edge_yellow_01" if edge else "platform_surface_01")


def track():
    image, draw = canvas("#676254")
    draw.rectangle((0, 6, 127, 121), fill="#776d5d")
    draw.rectangle((0, 15, 127, 111), fill="#746752")
    for x in (16, 58, 100):
        draw.rectangle((x, 9, x + 11, 116), fill="#514739")
        draw.rectangle((x + 1, 10, x + 8, 113), fill="#816a51")
        draw.line((x + 2, 14, x + 2, 111), fill="#90775a", width=2)
    for y in (29, 94):
        draw.rectangle((0, y - 2, 127, y + 7), fill="#494843")
        draw.rectangle((0, y, 127, y + 3), fill="#aaa79b")
        draw.line((0, y + 4, 127, y + 4), fill="#79776e")
    save(image, "track_horizontal_01")


if __name__ == "__main__":
    DESTINATION.mkdir(parents=True, exist_ok=True)
    grass()
    dirt()
    platform(False)
    platform(True)
    track()
