"""Draw exact, crisp 128px Wufeng material tiles from a shared palette."""
from pathlib import Path
from random import Random
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2] / "Assets/Resources/Art/Explore/TilesHD"
ROOT.mkdir(parents=True, exist_ok=True)
N = 128
C = {
    "grass": "#52673A", "grass2": "#617842", "grass3": "#70864A",
    "grass_dark": "#34472F", "grass_shadow": "#405331", "grass_light": "#98A55B",
    "straw": "#B5A05A", "flower": "#DB9C9B", "flower_light": "#E9C4AB",
    "dirt": "#A67B53", "dirt2": "#B68A5A", "dirt_light": "#D0A36A",
    "dirt_dark": "#76563F", "dirt_shadow": "#8E6649",
    "stone": "#74665F", "stone2": "#87766A", "stone3": "#9A8476",
    "stone_light": "#B5A08B", "stone_dark": "#4C4746", "stone_shadow": "#625750",
    "pave": "#BDAA94", "pave2": "#CBB69B", "pave_light": "#E1C7A4",
    "pave_shadow": "#9C8C7F", "mortar": "#776F68",
    "wood": "#755037", "wood2": "#906241", "wood_light": "#B88350",
    "wood_dark": "#4A352B", "rail": "#32393D", "rail_side": "#5F6461",
    "rail_top": "#BBC0B5", "rail_glint": "#E8D3A6", "yellow": "#D9B257",
}


def canvas(base):
    im = Image.new("RGB", (N, N), C[base])
    return im, ImageDraw.Draw(im)


def rect(d, box, color):
    d.rectangle(box, fill=C.get(color, color))


def wrap_poly(d, points, color):
    for dx in (-N, 0, N):
        for dy in (-N, 0, N):
            d.polygon([(x + dx, y + dy) for x, y in points], fill=C[color])


def flecks(d, rng, palette, count, length=3):
    for _ in range(count):
        x, y = rng.randrange(N), rng.randrange(N)
        col = C[palette[rng.randrange(len(palette))]]
        w, h = rng.randint(1, length), rng.randint(1, 2)
        for dx in (-N, 0, N):
            for dy in (-N, 0, N):
                d.rectangle((x + dx, y + dy, x + dx + w, y + dy + h), fill=col)


def ground(kind, variant):
    rng = Random(7307 + variant * 109 + (0 if kind == "grass" else 1000))
    im, d = canvas("grass" if kind == "grass" else "dirt")
    if kind == "grass":
        flecks(d, rng, ["grass2", "grass3", "grass_shadow"], 700, 4)
        for _ in range(75):
            x, y = rng.randrange(N), rng.randrange(N)
            wrap_poly(d, [(x-4,y+4),(x,y+1),(x+5,y+4),(x+1,y+7)], "grass_dark")
            for ox, oy, col in [(-3,-3,"grass2"),(0,-5,"grass_light"),(3,-2,"grass3")]:
                wrap_poly(d, [(x,y+4),(x+ox,y+oy),(x+ox+1,y+oy),(x+1,y+4)], col)
        for _ in range(16):
            x, y = rng.randrange(N), rng.randrange(N)
            wrap_poly(d, [(x,y),(x+2,y-2),(x+4,y),(x+2,y+2)], "flower" if rng.random()<.6 else "straw")
    else:
        flecks(d, rng, ["dirt2", "dirt_light", "dirt_dark", "dirt_shadow"], 1100, 4)
        for _ in range(95):
            x, y = rng.randrange(N), rng.randrange(N)
            wrap_poly(d, [(x-3,y),(x+2,y-2),(x+6,y),(x+2,y+3)], "dirt_dark")
            wrap_poly(d, [(x-2,y-1),(x+2,y-3),(x+4,y-2)], "dirt_light")
        for _ in range(14):
            x, y = rng.randrange(N), rng.randrange(N)
            wrap_poly(d, [(x,y+4),(x-1,y-3),(x+2,y+1)], "grass_shadow")
    return im


def ballast(seed=91):
    rng = Random(seed)
    im, d = canvas("stone_shadow")
    flecks(d, rng, ["stone", "stone2", "stone_dark", "dirt_dark"], 900, 3)
    for _ in range(270):
        x, y = rng.randrange(N), rng.randrange(N)
        r = rng.randint(2, 7)
        wrap_poly(d, [(x-r,y),(x-r//2,y-r//2),(x+r//2,y-r),(x+r,y),(x+r//3,y+r//2),(x-r//2,y+r//2)], "stone_dark")
        wrap_poly(d, [(x-r+1,y-1),(x-r//2,y-r//2),(x+r//2,y-r+1),(x+r-1,y)], rng.choice(["stone", "stone2", "stone3"]))
        wrap_poly(d, [(x-r//2,y-r//2),(x+r//2,y-r+1),(x+1,y-1)], "stone_light")
    return im


def track():
    im = ballast(91)
    d = ImageDraw.Draw(im)
    for x in (8, 42, 76, 110):
        rect(d, (x-3, 3, x+18, 124), "wood_dark")
        rect(d, (x, 4, x+15, 120), "wood")
        rect(d, (x+1, 6, x+3, 116), "wood_light")
        rect(d, (x+12, 5, x+14, 122), "wood2")
        for y in (18, 36, 63, 90, 108):
            rect(d, (x+5, y, x+10, y+1), "wood2")
    for y in (27, 94):
        rect(d, (0, y-3, 127, y+10), "rail")
        rect(d, (0, y, 127, y+6), "rail_side")
        rect(d, (0, y, 127, y+2), "rail_top")
        rect(d, (0, y, 127, y), "rail_glint")
        for x in (16, 50, 84, 118):
            rect(d, (x-3,y-6,x+5,y-2), "rail")
            rect(d, (x-1,y-5,x+3,y-4), "stone_light")
    return im


def platform(edge=False):
    rng = Random(151 if edge else 149)
    im, d = canvas("pave")
    for row, y0 in enumerate((-18, 18, 54, 90, 126)):
        offset = -26 if row % 2 else 14
        for x0 in range(offset-78, N+80, 64):
            shade = rng.choice(["pave", "pave2", "pave_light"])
            rect(d, (x0+2,y0+2,x0+62,y0+34), shade)
            rect(d, (x0+3,y0+3,x0+58,y0+4), "pave_light")
            rect(d, (x0+3,y0+32,x0+61,y0+33), "pave_shadow")
            for _ in range(18):
                px, py = x0+rng.randrange(4,60), y0+rng.randrange(5,30)
                rect(d,(px,py,px+rng.randrange(1,4),py+1),rng.choice(["pave_shadow","pave_light"]))
    if edge:
        rect(d, (0,88,127,92), "yellow")
        rect(d, (0,93,127,94), "dirt_dark")
        rect(d, (0,100,127,101), "mortar")
        rect(d, (0,105,127,127), "stone_shadow")
        rect(d, (0,107,127,109), "stone_light")
        flecks(d, Random(155), ["stone","stone2","stone_dark"], 70, 3)
        rect(d, (0,88,127,92), "yellow")
        rect(d, (0,93,127,94), "dirt_dark")
    return im


assets = {**{f"tile_ground_grass_{v:02d}": ground("grass",v) for v in (1,2,3)},
          **{f"tile_ground_dirt_{v:02d}": ground("dirt",v) for v in (1,2,3)},
          "tile_ground_ballast_01": ballast(), "tile_track_horizontal_01": track(),
          "tile_platform_surface_01": platform(), "tile_platform_edge_yellow_01": platform(True)}
for name, im in assets.items():
    # The world grid repeats these files directly. Make opposite edge samples
    # identical while leaving the hand-authored rail geometry unchanged.
    for y in range(N):
        im.putpixel((N-1, y), im.getpixel((0, y)))
    if name.startswith("tile_ground_"):
        for x in range(N):
            im.putpixel((x, N-1), im.getpixel((x, 0)))
    im.save(ROOT / f"{name}.png")

sheet = Image.new("RGB", (5*260, 2*290), "#E8D8C0")
from PIL import ImageFont
sd = ImageDraw.Draw(sheet)
for i, (name, im) in enumerate(sorted(assets.items())):
    x, y = (i%5)*260, (i//5)*290
    sheet.paste(im.resize((256,256), Image.Resampling.NEAREST), (x,y))
    sd.text((x+4,y+260),name,fill="#3A2010")
sheet.save(ROOT / "outdoor_hd_contact_sheet.png")
