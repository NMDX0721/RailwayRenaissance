"""Original CF-17R rear rating plate and physically indexed desktop controls."""
from pathlib import Path
import io
import json
import math
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Resources/Cab'
P = json.loads((OUT / 'circulator_profile.json').read_text(encoding='utf-8'))
FONT = ROOT / 'Assets/Resources/Fonts/SarasaMonoSC-Regular.ttf'


def label(draw, xy, value, size, fill='#25353b'):
    assert '?' not in value, ('Corrupted print text', value)
    font = ImageFont.truetype(str(FONT), size)
    bounds = draw.textbbox(xy, value, font=font, anchor='mm')
    assert bounds[0] >= 4 and bounds[2] <= draw._image.width-4, (value, bounds)
    draw.text(xy, value, font=font, anchor='mm', fill=fill)


def save(im, name):
    stream = io.BytesIO()
    im.save(stream, format='PNG')
    path = OUT / (name + '.png')
    if not path.exists() or path.read_bytes() != stream.getvalue():
        path.write_bytes(stream.getvalue())


im = Image.new('RGB', (1200, 900), '#c4c4b6')
d = ImageDraw.Draw(im)
d.rectangle((18, 18, 1181, 881), outline='#445056', width=5)
label(d, (600, 118), P['manufacturer'], 110)
label(d, (600, 226), 'CHUANLAN', 72)
label(d, (600, 310), 'ELECTROMECHANICAL WORKS', 64)
d.line((75, 380, 1125, 380), fill='#65716e', width=3)
label(d, (600, 478), P['model'], 156)
label(d, (600, 622), 'DC 24 V   0 / I / II', 77)
label(d, (600, 734), '车辆循环风扇 / VEHICLE FAN', 63)
label(d, (600, 822), '定向 / 摆头   FIXED / OSCILLATING', 47)
save(im, 'Cab_Circulator_Rating_Plate_V1')

im = Image.new('RGB', (1024, 1054), '#c8c4af')
d = ImageDraw.Draw(im)
def point(x, z):
    return ((-x/68+.5)*1024, (z/70+.5)*1054)
label(d, point(0, -30), '循环风扇 / FAN', 52)
label(d, point(-12, -18), '风速 / SPEED', 34)
for i, number in enumerate(['0', 'I', 'II']):
    a = math.radians(-60+i*60)
    d.line((point(-12+16*math.sin(a), 16*math.cos(a)),
            point(-12+19*math.sin(a), 19*math.cos(a))), fill='#273c40', width=8)
    label(d, point(-12+23*math.sin(a), 23*math.cos(a)), number, 62)
label(d, point(21, -21), '定向', 48)
label(d, point(21, -17), 'FIXED', 27)
label(d, point(21, 19), '摆动', 48)
label(d, point(21, 23), 'SWEEP', 27)
label(d, point(0, 32), 'CF-17R · CHUANLAN', 27)
save(im, 'Cab_Circulator_Control_Scale_V2')

im = Image.new('RGB', (1024, 1024), '#254d52')
d = ImageDraw.Draw(im)
d.ellipse((18, 18, 1005, 1005), outline='#a9b6af', width=10)
d.ellipse((42, 42, 981, 981), outline='#3f676a', width=4)
ivory = '#e4e0c5'
for x in (474, 550):
    d.line((x, 274, x, 628), fill=ivory, width=28)
d.line((438, 594, 586, 594), fill=ivory, width=27)
for tier in range(3):
    y = 330+tier*88
    outer = 144+tier*44
    d.polygon(((430, y+20), (outer, y-18), (outer, y+20), (430, y+57)), fill=ivory)
    d.polygon(((594, y+20), (1024-outer, y-18), (1024-outer, y+20), (594, y+57)), fill=ivory)
label(d, (512, 789), 'CHUANLAN', 120, ivory)
save(im, 'Cab_Circulator_Manufacturer_Medallion_V1')
print('CF-17R original plate and indexed scale generated')
