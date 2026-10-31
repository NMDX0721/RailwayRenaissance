"""Original equipment labels, not medical instructions or certification artwork."""
from pathlib import Path
import io
import json
import random
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Resources/Cab'
PROFILE = json.loads((OUT / 'first_aid_label_profile.json').read_text(encoding='utf-8'))
FONT = ROOT / 'Assets/Resources/Fonts/SarasaMonoSC-Regular.ttf'

def save(image, name):
    buf = io.BytesIO()
    image.save(buf, format='PNG')
    path = OUT / name
    if not path.exists() or path.read_bytes() != buf.getvalue():
        path.write_bytes(buf.getvalue())

def text(draw, xy, value, size, color, anchor='mm', bold=False):
    font = ImageFont.truetype(str(FONT), size)
    box = draw.textbbox(xy, value, font=font, anchor=anchor, stroke_width=1 if bold else 0)
    assert box[0] >= 22 and box[2] <= 1002, (value, box)
    draw.text(xy, value, font=font, fill=color, anchor=anchor,
              stroke_width=1 if bold else 0, stroke_fill=color)

im = Image.new('RGB', (1024, 792), '#184a38')
d = ImageDraw.Draw(im)
d.rectangle((24, 24, 999, 767), outline='#b8c5ad', width=2)
text(d, (64, 55), 'NF-5K  /  随车装备', 26, '#d1d9c2', 'lm')
text(d, (64, 87), 'ONBOARD EQUIPMENT', 19, '#d1d9c2', 'lm')
text(d, (957, 70), PROFILE['cabinetId'], 30, '#e5e8d7', 'rm', True)
d.line((64, 108, 960, 108), fill='#94aa95', width=2)
d.rectangle((481, 154, 543, 344), fill='#efeddb')
d.rectangle((417, 218, 607, 280), fill='#efeddb')
text(d, (512, 423), '急救用品', 84, '#efeddb', bold=True)
text(d, (512, 506), 'FIRST AID', 48, '#efeddb')
d.line((194, 573, 830, 573), fill='#94aa95', width=2)
text(d, (512, 625), '保持取用通道畅通', 30, '#d1d9c2')
text(d, (512, 669), 'KEEP ACCESS CLEAR', 26, '#d1d9c2')
text(d, (512, 733), '客舱安全设备 / PASSENGER SAFETY EQUIPMENT', 23, '#b8c5ad')
save(im, 'First_Aid_Cabinet_Print_V3.png')

for n, p in enumerate(PROFILE['packs']):
    im = Image.new('RGB', (1024, 888), '#e5e2cc')
    d = ImageDraw.Draw(im)
    d.rectangle((28, 28, 995, 859), outline=p['accent'], width=3)
    d.rectangle((48, 48, 975, 143), fill=p['accent'])
    text(d, (82, 96), p['category'], 36, '#f4efda', 'lm')
    text(d, (512, 230), p['name'], 87, '#1b302b', bold=True)
    text(d, (512, 324), p['english'], 43, '#1b302b')
    d.line((98, 386, 926, 386), fill=p['accent'], width=3)
    text(d, (512, 463), p['spec'], 55, '#1b302b')
    text(d, (512, 543), p['quantity'], 39, '#1b302b')
    text(d, (512, 643), '保持干燥 / KEEP DRY', 32, '#42544b')
    d.line((98, 711, 926, 711), fill='#9ba594', width=2)
    text(d, (98, 762), p['id'], 31, '#42544b', 'lm')
    text(d, (926, 762), 'NF-5K / FA-01', 27, '#42544b', 'rm')
    text(d, (512, 819), '随车备品 / ONBOARD SUPPLIES', 25, '#42544b')
    rng = random.Random(2310 + n)
    for _ in range(48):
        x = rng.choice([rng.randrange(29, 40), rng.randrange(985, 995)])
        y = rng.randrange(160, 840)
        d.line((x, y, x + 2, y + 5), fill='#c7cbb6', width=1)
    save(im, p['id'] + '_Print_V2.png')
print('Generated cabinet sign and 3 full-size bilingual package labels; bounds checked.')
