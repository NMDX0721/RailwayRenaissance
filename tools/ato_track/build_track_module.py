"""ATO track module kit: flat 25 m / 5 m pieces and +-10 / +-20 permil grade pieces, plus their textures.

Frame: module origin on the centreline at rail-top level (Y=0 in Unity). Route distance s grows toward
Blender +Y, which this FBX export (forward -Z, up Y) lands on Unity -Z (checked by Unity bounds). Units are metres.
Grade pieces keep a 25 m horizontal run (slant length 25/cos) so they tile by horizontal projection; the sleeper
pitch, ballast surface noise (periodic in 5 m) and UVs line up across every joint.
Run: blender --background --python tools/ato_track/build_track_module.py
"""
import math
import zlib
import random
import bpy
import bmesh
import numpy as np
from pathlib import Path
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / 'Assets/Models/ATO'
TEX_DIR = OUT_DIR / 'Textures'
OUT_DIR.mkdir(parents=True, exist_ok=True)
TEX_DIR.mkdir(parents=True, exist_ok=True)

GAUGE_INNER = 1.435
HEAD_W = 0.065
RAIL_H = 0.172
HALF_CENTRE = (GAUGE_INNER + HEAD_W) / 2          # 0.75: rail centre to track centreline
SLEEPER_PER_25 = 40                                 # 0.625 m pitch so 25 m and 5 m modules tile exactly
SLEEPER_L = 2.6
SLEEPER_SEAT_Z = -0.185                             # sleeper top at the rail seat: rail foot -0.172 minus a 12 mm rubber pad
SLEEPER_MID_Z = -0.230                              # sleeper top in the middle section (centre is lower, Type III style)
SLEEPER_BOTTOM_Z = -0.415                           # 230 mm at the rail seat, 185 mm in the middle (flat underside)
SLEEPER_W = 0.30                                    # bottom width; top is 20 mm narrower
BALLAST_TOP_Y = -0.245                              # bed surface; stones on top bring the visible crib level to about -0.21
BALLAST_BOTTOM_Y = -0.665                           # 250 mm of ballast under the sleeper
BALLAST_TOP_HALF = 1.70                             # estimate, to be replaced by the TB 10082 section
BALLAST_BOTTOM_HALF = BALLAST_TOP_HALF + (BALLAST_TOP_Y - BALLAST_BOTTOM_Y) * 1.75
NOISE_PERIOD = 5.0                                  # ballast surface repeats every 5 m so 5 m and 25 m pieces mate

# (file name, horizontal run m, grade permil)
MODULES = [
    ('Track_Module_25m', 25.0, 0),
    ('Track_Module_5m', 5.0, 0),
    ('Track_Module_25m_Up10', 25.0, 10),
    ('Track_Module_25m_Down10', 25.0, -10),
    ('Track_Module_25m_Up20', 25.0, 20),
    ('Track_Module_25m_Down20', 25.0, -20),
]


# ---------------------------------------------------------------- textures
def smooth(t):
    return t * t * (3 - 2 * t)


def value_noise(n, cells, rng):
    """Tileable value noise, n x n, in 0..1."""
    g = rng.random((cells, cells))
    xs = np.linspace(0, cells, n, endpoint=False)
    i0 = np.floor(xs).astype(int) % cells
    i1 = (i0 + 1) % cells
    t = smooth(xs - np.floor(xs))
    a = g[np.ix_(i0, i0)] * (1 - t)[:, None] * (1 - t)[None, :]
    b = g[np.ix_(i0, i1)] * (1 - t)[:, None] * t[None, :]
    c = g[np.ix_(i1, i0)] * t[:, None] * (1 - t)[None, :]
    d = g[np.ix_(i1, i1)] * t[:, None] * t[None, :]
    return a + b + c + d


def fbm(n, base_cells, octaves, rng):
    total = np.zeros((n, n))
    amp, norm = 1.0, 0.0
    for o in range(octaves):
        total += amp * value_noise(n, base_cells * 2 ** o, rng)
        norm += amp
        amp *= 0.5
    return total / norm


def normal_from_height(h, strength):
    dx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * strength
    dy = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * strength
    nz = np.ones_like(h)
    ln = np.sqrt(dx * dx + dy * dy + nz * nz)
    return np.stack([-dx / ln * .5 + .5, -dy / ln * .5 + .5, nz / ln * .5 + .5, np.ones_like(h)], axis=-1)


def save_png(name, arr):
    h, w = arr.shape[:2]
    img = bpy.data.images.new(name, w, h, alpha=True)
    img.colorspace_settings.name = 'Non-Color'          # write the numbers as they are
    img.pixels.foreach_set(np.clip(arr, 0, 1).astype(np.float32).ravel())
    img.filepath_raw = str(TEX_DIR / (name + '.png'))
    img.file_format = 'PNG'
    img.save()


def rgba(rgb, alpha):
    a = np.ones(rgb.shape[:2] + (4,))
    a[..., :3] = rgb
    a[..., 3] = alpha
    return a


def make_concrete(n=1024):
    rng = np.random.default_rng(5)
    low = fbm(n, 4, 5, rng)
    pores = value_noise(n, 160, rng)
    pit = np.clip((pores - 0.80) / 0.2, 0, 1)
    stain = smooth(np.clip((value_noise(n, 3, rng) - 0.55) / 0.25, 0, 1))
    grease = smooth(np.clip((value_noise(n, 6, rng) - 0.70) / 0.2, 0, 1))
    base = np.array([.40, .39, .37])
    col = base[None, None, :] * (0.88 + 0.24 * low[..., None])
    col *= (1 - 0.22 * pit[..., None])
    col = col * (1 - 0.14 * stain[..., None])
    col = col * (1 - 0.35 * grease[..., None]) + np.array([.03, .03, .035])[None, None, :] * 0.35 * grease[..., None]
    smooth_a = 0.14 + 0.12 * (1 - low) - 0.08 * stain + 0.18 * grease
    save_png('Track_Sleeper_BaseColor', rgba(col, 1))
    save_png('Track_Sleeper_Normal', normal_from_height(low * 0.4 + 0.6 * (1 - pit), 3.0))
    save_png('Track_Sleeper_MetallicSmoothness', rgba(np.zeros((n, n, 3)), smooth_a))


def make_ballast(n=1024):
    """Bed under the modelled stones: fine, dark, dusty gravel. No cells, so it reads as the shadowed gap between stones
    instead of a second layer of fake cobbles."""
    rng = np.random.default_rng(11)
    coarse = fbm(n, 5, 4, rng)
    grain = value_noise(n, 120, rng)
    grain2 = value_noise(n, 210, rng)
    dust = smooth(np.clip((value_noise(n, 4, rng) - 0.45) / 0.4, 0, 1))
    tone = 0.10 + 0.16 * grain * 0.6 + 0.10 * grain2 * 0.6 + 0.05 * coarse
    col = tone[..., None] * np.array([1.0, .985, .95])[None, None, :]
    col = col * (1 - 0.25 * dust[..., None]) + 0.06 * dust[..., None]
    height = 0.6 * grain + 0.4 * grain2
    save_png('Track_Ballast_BaseColor', rgba(col, 1))
    save_png('Track_Ballast_Normal', normal_from_height(height, 5.0))
    save_png('Track_Ballast_MetallicSmoothness', rgba(np.zeros((n, n, 3)), 0.10 + 0.06 * grain))


def make_rail(n=512):
    """Atlas. u<0.5: head top across its width (polished running band). u>=0.5: weathered steel, varies along v only."""
    rng = np.random.default_rng(23)
    streak = value_noise(n, 3, rng)[:, 0:1] * np.ones((1, n))
    grain = fbm(n, 8, 4, rng)
    col = np.zeros((n, n, 3))
    sm = np.zeros((n, n))
    met = np.zeros((n, n))
    u = (np.arange(n) / n)[None, :] * np.ones((n, 1))
    top = u < 0.5
    t = u / 0.5
    band = smooth(np.clip((1 - np.abs(t - 0.5) / 0.26) * 1.6, 0, 1))
    rim = np.array([.20, .17, .15])
    run = np.array([.60, .61, .62])
    top_col = rim[None, None, :] * (1 - band[..., None]) + run[None, None, :] * band[..., None]
    top_col *= (0.92 + 0.12 * grain[..., None])
    top_col *= (0.95 + 0.08 * streak[..., None])
    steel = np.array([.27, .22, .19])
    side = steel[None, None, :] * (0.80 + 0.45 * grain[..., None]) * (0.85 + 0.3 * streak[..., None])
    side[..., 2] *= 0.9
    col = np.where(top[..., None], top_col, side)
    sm = np.where(top, 0.25 + 0.60 * band, 0.22 + 0.15 * (1 - grain))
    met = np.where(top, 0.55 + 0.35 * band, 0.55)
    save_png('Track_Rail_BaseColor', rgba(col, 1))
    save_png('Track_Rail_Normal', normal_from_height(grain * 0.5, 1.5))
    a = np.zeros((n, n, 4))
    a[..., 0] = met
    a[..., 3] = sm
    save_png('Track_Rail_MetallicSmoothness', a)


def make_stone(n=512):
    """Surface texture for the modelled surface stones: speckled grey rock with strong stone-to-stone tone range."""
    rng = np.random.default_rng(31)
    blocks = value_noise(n, 3, rng)
    fine = fbm(n, 16, 4, rng)
    speck = value_noise(n, 96, rng)
    tone = 0.13 + 0.25 * blocks
    col = tone[..., None] * np.array([1.0, .985, .95])[None, None, :] * (0.82 + 0.3 * fine[..., None])
    col *= 1 - 0.25 * np.clip((speck - 0.75) / 0.25, 0, 1)[..., None]
    col += 0.10 * np.clip((speck - 0.9) / 0.1, 0, 1)[..., None]
    save_png('Track_Stone_BaseColor', rgba(col, 1))
    save_png('Track_Stone_Normal', normal_from_height(fine * 0.6 + 0.4 * speck, 4.0))
    save_png('Track_Stone_MetallicSmoothness', rgba(np.zeros((n, n, 3)), 0.18 + 0.12 * fine))


# ---------------------------------------------------------------- geometry
def box_uv(bm, scale, offset_of=None):
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        ou, ov = offset_of(f) if offset_of else (0.0, 0.0)
        for lp in f.loops:
            c = lp.vert.co
            a, b = {0: (c.y, c.z), 1: (c.x, c.z), 2: (c.x, c.y)}[ax]
            lp[uv].uv = (a / scale + ou, b / scale + ov)


def rail_uv(bm, cx):
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        center = f.calc_center_median()
        is_top = n.z > 0.55 and center.z > -0.009
        for lp in f.loops:
            c = lp.vert.co
            if is_top:
                lp[uv].uv = (0.5 * min(max((c.x - (cx - HEAD_W / 2)) / HEAD_W, 0.0), 1.0), c.y / 2.0)
            else:
                lp[uv].uv = (0.75, c.y / 2.0)


def extrude_profile(bm, profile, y_from, y_to):
    a = [bm.verts.new((x, y_from, z)) for x, z in profile]
    b = [bm.verts.new((x, y_to, z)) for x, z in profile]
    n = len(profile)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((a[i], a[j], b[j], b[i]))
    bm.faces.new(a)
    bm.faces.new(list(reversed(b)))


def rail_profile(cx):
    # 60 kg/m style half profile, x = half width, z down from the running surface. Gauge face stays at 0.0325 so the
    # 1.435 m gauge (measured 16 mm below the top) and the 0.75 m rail centre used by the wheel contact are unchanged.
    half = [(0.0140, -0.0004), (0.0235, -0.0016), (0.0292, -0.0042), (0.0316, -0.0078), (0.0325, -0.0125),
            (0.0325, -0.0225), (0.0312, -0.0300), (0.0262, -0.0365), (0.0190, -0.0425), (0.0120, -0.0500),
            (0.0086, -0.0620), (0.0083, -0.1000), (0.0090, -0.1220), (0.0140, -0.1330), (0.0270, -0.1410),
            (0.0480, -0.1490), (0.0700, -0.1555), (0.0750, -0.1580), (0.0750, -0.1720)]
    right = [(cx + x, z) for x, z in half]
    left = [(cx - x, z) for x, z in reversed(half)]
    return right + left


def add_box(bm, centre, size, rot=None):
    """Cube of the given size at centre, optionally rotated (3x3 Matrix) about its own centre."""
    for v in bmesh.ops.create_cube(bm, size=1.0)['verts']:
        p = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
        if rot is not None:
            p = rot @ p
        v.co = p + Vector(centre)


def add_prism(bm, centre, radius, depth, segments):
    """Vertical prism (bolt, nut, washer) built from a capped cone with equal radii."""
    res = bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segments, radius1=radius, radius2=radius, depth=depth)
    for v in res['verts']:
        v.co = v.co + Vector(centre)


def build_sleepers(bm, count, pitch):
    """Type III style prestressed sleeper: flat underside, raised rail seats, lower and narrower middle, chamfered ends.
    A little per-sleeper scatter (offset, yaw, settlement) so the line does not look stamped."""
    xs = [-1.30, -1.27, -0.60, -0.42, 0.42, 0.60, 1.27, 1.30]
    tops = [-0.200, SLEEPER_SEAT_Z, SLEEPER_SEAT_Z, SLEEPER_MID_Z, SLEEPER_MID_Z, SLEEPER_SEAT_Z, SLEEPER_SEAT_Z, -0.200]
    widths = [0.140, 0.150, 0.150, 0.138, 0.138, 0.150, 0.150, 0.140]
    for i in range(count):
        rng = random.Random(i * 104729 + 17)
        yc = (i + 0.5) * pitch + rng.uniform(-0.004, 0.004)
        dx, dz = rng.uniform(-0.008, 0.008), rng.uniform(-0.003, 0.0)
        yaw = rng.uniform(-0.004, 0.004)
        ca, sa = math.cos(yaw), math.sin(yaw)
        rings = []
        for x, top, w in zip(xs, tops, widths):
            pts = [(-w, SLEEPER_BOTTOM_Z), (w, SLEEPER_BOTTOM_Z), (w - 0.02, top), (-w + 0.02, top)]
            ring = []
            for yy, zz in pts:
                px, py = x * ca - yy * sa, x * sa + yy * ca
                ring.append(bm.verts.new((px + dx, py + yc, zz + dz)))
            rings.append(ring)
        for a, b in zip(rings[:-1], rings[1:]):
            for k in range(4):
                bm.faces.new((a[k], a[(k + 1) % 4], b[(k + 1) % 4], b[k]))
        bm.faces.new(rings[0])
        bm.faces.new(list(reversed(rings[-1])))


def build_fastenings(bm_pad, bm_steel, count, pitch):
    """Rubber pad + insulating gauge blocks (pads object) and elastic clips with washer, nut and stud (fastener object)."""
    for i in range(count):
        rng = random.Random(i * 104729 + 17)
        yc = (i + 0.5) * pitch + rng.uniform(-0.004, 0.004)
        dx, dz = rng.uniform(-0.008, 0.008), rng.uniform(-0.003, 0.0)
        for cx in (-HALF_CENTRE, HALF_CENTRE):
            x0 = cx + dx
            add_box(bm_pad, (x0, yc, SLEEPER_SEAT_Z + dz + 0.006), (0.17, 0.19, 0.012))
            for d in (-1, 1):
                add_box(bm_pad, (x0 + d * 0.0985, yc, -0.1670 + dz), (0.047, 0.07, 0.036))
                add_box(bm_pad, (x0 + d * 0.0640, yc, -0.1500 + dz), (0.030, 0.07, 0.009))
                for leg in (-1, 1):
                    yl = yc + leg * 0.030
                    add_box(bm_steel, (x0 + d * 0.0585, yl, -0.1480 + dz), (0.034, 0.011, 0.008))
                    rot = Matrix.Rotation(d * math.radians(-32), 3, 'Y')
                    add_box(bm_steel, (x0 + d * 0.0920, yl, -0.1360 + dz), (0.060, 0.011, 0.010), rot)
                add_box(bm_steel, (x0 + d * 0.1120, yc, -0.1250 + dz), (0.014, 0.090, 0.011))
                add_prism(bm_steel, (x0 + d * 0.1120, yc, -0.1135 + dz), 0.0165, 0.004, 10)
                add_prism(bm_steel, (x0 + d * 0.1120, yc, -0.1075 + dz), 0.0140, 0.012, 6)
                add_prism(bm_steel, (x0 + d * 0.1120, yc, -0.0930 + dz), 0.0060, 0.028, 8)


def ballast_noise(x, y):
    """Crib-level undulation, periodic in NOISE_PERIOD along the route so every module mates."""
    k = 2 * math.pi / NOISE_PERIOD
    n = (0.010 * math.sin(k * 3 * y + 1.3 * x) + 0.007 * math.sin(k * 7 * y - 2.1 * x + 0.8)
         + 0.005 * math.sin(k * 11 * y + 4.0 * x + 2.2) + 0.004 * math.sin(k * 17 * y - 7.0 * x))
    return n


def build_ballast(bm, length):
    xs_top = [-BALLAST_TOP_HALF + 0.2 * i for i in range(int(2 * BALLAST_TOP_HALF / 0.2) + 1)]
    if abs(xs_top[-1] - BALLAST_TOP_HALF) > 1e-6:
        xs_top.append(BALLAST_TOP_HALF)
    rows = max(2, int(round(length / 0.5)))
    ring_rows = []
    for r in range(rows + 1):
        y = length * r / rows
        ring = [bm.verts.new((-BALLAST_BOTTOM_HALF, y, BALLAST_BOTTOM_Y))]
        for i, x in enumerate(xs_top):
            edge = 0.0
            if i == 0 or i == len(xs_top) - 1:
                edge = -0.03                                  # rounded shoulder
            ring.append(bm.verts.new((x, y, BALLAST_TOP_Y + ballast_noise(x, y) + edge)))
        ring.append(bm.verts.new((BALLAST_BOTTOM_HALF, y, BALLAST_BOTTOM_Y)))
        ring_rows.append(ring)
    m = len(ring_rows[0])
    for r in range(rows):
        a, b = ring_rows[r], ring_rows[r + 1]
        for i in range(m - 1):
            bm.faces.new((a[i], a[i + 1], b[i + 1], b[i]))
        bm.faces.new((a[m - 1], a[0], b[0], b[m - 1]))      # flat underside
    bm.faces.new(ring_rows[0])
    bm.faces.new(list(reversed(ring_rows[-1])))


STONE_DENSITY = 240           # stones per m of ballast length (x 3.8 m width); dense enough to hide almost all of the bed
SLEEPER_TOP_Z = SLEEPER_SEAT_Z


def surface_z(x, y):
    ax = abs(x)
    if ax <= BALLAST_TOP_HALF:
        return BALLAST_TOP_Y + ballast_noise(x, y)
    t = (ax - BALLAST_TOP_HALF) / (BALLAST_BOTTOM_HALF - BALLAST_TOP_HALF)
    return (BALLAST_TOP_Y - 0.03) * (1 - t) + BALLAST_BOTTOM_Y * t


def icosahedron():
    t = (1 + 5 ** 0.5) / 2
    v = [(-1, t, 0), (1, t, 0), (-1, -t, 0), (1, -t, 0), (0, -1, t), (0, 1, t), (0, -1, -t), (0, 1, -t),
         (t, 0, -1), (t, 0, 1), (-t, 0, -1), (-t, 0, 1)]
    f = [(0, 11, 5), (0, 5, 1), (0, 1, 7), (0, 7, 10), (0, 10, 11), (1, 5, 9), (5, 11, 4), (11, 10, 2), (10, 7, 6),
         (7, 1, 8), (3, 9, 4), (3, 4, 2), (3, 2, 6), (3, 6, 8), (3, 8, 9), (4, 9, 5), (2, 4, 11), (6, 2, 10),
         (8, 6, 7), (9, 8, 1)]
    v = np.array(v, dtype=float)
    return v / np.linalg.norm(v, axis=1)[:, None], f


def build_stones(name, length, count_sleepers, angle, mat, parent, seed):
    """Real stone geometry on the whole ballast: angular low-poly rocks, half buried, none under sleepers and none above the
    sleeper seat. Split into chunks of two sleeper bays so the game can shake them independently."""
    rng = random.Random(seed)
    nrng = np.random.default_rng(seed)
    base_v, base_f = icosahedron()
    pitch = length / count_sleepers
    chunk_len = 2 * pitch
    chunks = int(round(length / chunk_len))
    total = int(STONE_DENSITY * length * 3.8)
    data = [([], [], []) for _ in range(chunks)]
    ca, sa = math.cos(angle), math.sin(angle)
    placed = 0
    while placed < total:
        x = rng.uniform(-BALLAST_BOTTOM_HALF + 0.08, BALLAST_BOTTOM_HALF - 0.08)
        y = rng.uniform(0.04, length - 0.04)
        k = int(y // pitch)
        yc = (k + 0.5) * pitch
        if abs(x) < 1.36 and abs(y - yc) < SLEEPER_W / 2 + 0.03:
            continue                                                   # under a sleeper
        size = 0.024 + 0.040 * rng.random() ** 1.7                      # radius 2.4..6.4 cm
        sx, sy, sz = size * rng.uniform(.8, 1.3), size * rng.uniform(.8, 1.3), size * rng.uniform(.5, .9)
        surf = surface_z(x, y)
        cz = surf - 0.2 * sz
        if abs(x) < 1.40:                                               # crib / shoulder: stay below the sleeper seat
            limit = SLEEPER_TOP_Z - 0.010
            sz = max(0.012, min(sz, limit - cz))
        yaw = rng.uniform(0, math.pi * 2)
        tilt = (rng.uniform(-.3, .3), rng.uniform(-.3, .3))
        ry = nrng.uniform(0.74, 1.15, len(base_v))[:, None]             # per-vertex scatter: angular, irregular rock
        p = base_v * ry * np.array([sx, sy, sz])
        c, s_ = math.cos(yaw), math.sin(yaw)
        p = np.stack([p[:, 0] * c - p[:, 1] * s_, p[:, 0] * s_ + p[:, 1] * c, p[:, 2]], axis=1)
        p[:, 0] += p[:, 2] * tilt[0]
        p[:, 1] += p[:, 2] * tilt[1]
        p += np.array([x, y, cz])
        py = p[:, 1] * ca - p[:, 2] * sa
        pz = p[:, 1] * sa + p[:, 2] * ca
        p = np.stack([p[:, 0], py, pz], axis=1)
        verts, faces, uvs = data[min(chunks - 1, int(y // chunk_len))]
        base = len(verts)
        verts.extend(p.tolist())
        faces.extend([(base + a, base + b, base + c_) for a, b, c_ in base_f])
        ou, ov = rng.random(), rng.random()
        for a, b, c_ in base_f:
            for idx in (a, b, c_):
                uvs.append((base_v[idx][0] * 0.35 + ou, base_v[idx][1] * 0.35 + ov))
        placed += 1
    objs = []
    for ci, (verts, faces, uvs) in enumerate(data):
        if not verts:
            continue
        mesh = bpy.data.meshes.new('%s_%02d' % (name, ci))
        mesh.from_pydata(verts, [], faces)
        mesh.update()
        uv = mesh.uv_layers.new(name='UVMap')
        uv.data.foreach_set('uv', np.array(uvs, dtype=np.float32).ravel())
        mesh.materials.append(mat)
        for poly in mesh.polygons:
            poly.use_smooth = False
        obj = bpy.data.objects.new('%s_%02d' % (name, ci), mesh)
        bpy.context.collection.objects.link(obj)
        obj.parent = parent
        objs.append(obj)
    return objs


def finish(name, bm, mat, parent, angle):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    if angle:
        bmesh.ops.rotate(bm, verts=bm.verts[:], cent=(0, 0, 0), matrix=Matrix.Rotation(angle, 3, 'X'))
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(mat)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    for poly in mesh.polygons:
        poly.use_smooth = False
    return obj


def make_materials():
    def mat(name):
        m = bpy.data.materials.new(name)
        m.diffuse_color = (.5, .5, .5, 1)
        return m
    return (mat('Track_Rail_Steel'), mat('Track_Sleeper_Concrete'), mat('Track_Ballast'), mat('Track_Ballast_Stone'),
            mat('Track_Fastener_Steel'), mat('Track_Rubber'))


def build_module(name, run, permil):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    steel, concrete, ballast_m, stone_m, fastener_m, rubber_m = make_materials()
    angle = math.atan(permil / 1000.0)
    length = run / math.cos(angle)                       # slant length so the horizontal run stays exact
    root = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(root)
    root['horizontal_run_m'] = run
    root['grade_permil'] = permil
    root['rise_m'] = run * permil / 1000.0
    root['gauge_inner_m'] = GAUGE_INNER
    root['origin'] = 'centreline at rail-top level; s increases toward Blender +Y'

    for side, cx in (('L', -HALF_CENTRE), ('R', HALF_CENTRE)):
        bm = bmesh.new()
        extrude_profile(bm, rail_profile(cx), 0.0, length)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        rail_uv(bm, cx)
        finish('Rail_' + side, bm, steel, root, angle)

    count = int(round(run / 25.0 * SLEEPER_PER_25))
    pitch = length / count
    bm = bmesh.new()
    build_sleepers(bm, count, pitch)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])

    def offset(face):
        idx = int(face.calc_center_median().y // pitch)
        r = random.Random(idx * 7919 + 13)
        return r.random(), r.random()
    box_uv(bm, 1.0, offset)
    finish('Sleepers', bm, concrete, root, angle)

    bm_pad, bm_steel = bmesh.new(), bmesh.new()
    build_fastenings(bm_pad, bm_steel, count, pitch)
    box_uv(bm_pad, 1.0)
    box_uv(bm_steel, 1.0)
    finish('Pads', bm_pad, rubber_m, root, angle)
    finish('Fasteners', bm_steel, fastener_m, root, angle)

    bm = bmesh.new()
    build_ballast(bm, length)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    box_uv(bm, 1.0)
    finish('Ballast', bm, ballast_m, root, angle)
    build_stones('Ballast_Stones', length, count, angle, stone_m, root, zlib.crc32(name.encode()) % 100000 + 7)

    for ename, y in (('Module_Start', 0.0), ('Module_End', length)):
        e = bpy.data.objects.new(ename, None)
        bpy.context.collection.objects.link(e)
        e.parent = root
        e.location = (0, y * math.cos(angle) if y else 0.0, y * math.sin(angle))

    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.data.objects:
        obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUT_DIR / (name + '.fbx')), use_selection=True,
                             axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
                             object_types={'EMPTY', 'MESH'}, mesh_smooth_type='FACE', path_mode='COPY',
                             embed_textures=False)
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'tools/ato_track' / (name + '.blend')))


make_concrete()
make_ballast()
make_rail()
make_stone()
for mod in MODULES:
    build_module(*mod)
print('TRACK_KIT_OK')
