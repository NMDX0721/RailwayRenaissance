"""Original NF-5K crew-access plates and transparent glazing lettering."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import io, json

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Resources/Cab'
P = json.loads((OUT / 'passage_door_profile.json').read_text(encoding='utf-8'))
FONT = ROOT / 'Assets/Resources/Fonts/SarasaMonoSC-Regular.ttf'

def text(draw, xy, value, size, color, anchor='mm'):
    font = ImageFont.truetype(str(FONT), size)
    box = draw.textbbox(xy, value, font=font, anchor=anchor)
    assert box[0] >= 16 and box[2] <= 1008, (value, box)
    draw.text(xy, value, font=font, fill=color, anchor=anchor)

def save(image, name):
    stream = io.BytesIO(); image.save(stream, format='PNG')
    path = OUT / (name + '.png')
    if not path.exists() or path.read_bytes() != stream.getvalue():
        path.write_bytes(stream.getvalue())

im = Image.new('RGBA', (1024, 320), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
text(d, (512, 110), P['identityCn'], 180, '#ecede4')
text(d, (512, 250), P['identityEn'], 72, '#ecede4')
save(im, 'Passage_Glass_Lettering_V1')

for name, cn, en in [('Passage_Crew_Plate_V1', P['accessCn'], P['accessEn']),
                     ('Passage_Clear_Plate_V1', P['clearCn'], P['clearEn'])]:
    im = Image.new('RGB', (1024, 320), '#dedecb'); d = ImageDraw.Draw(im)
    d.rectangle((12, 12, 1011, 307), outline='#515c52', width=3)
    d.rectangle((12, 12, 1011, 38), fill='#843c36')
    text(d, (512, 120), cn, 66, '#303a33')
    text(d, (512, 222), en, 55, '#303a33')
    save(im, name)

# Direct ink on the narrow, elongated handle backplate; no separate sign background.
for name, cn, en in [('Passage_Pull_Integrated_V2', P['pullCn'], P['pullEn']),
                     ('Passage_Push_Integrated_V2', P['pushCn'], P['pushEn'])]:
    im = Image.new('RGBA', (512, 524), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    text(d, (256, 210), cn, 260, '#17221d')
    text(d, (256, 440), en, 110, '#17221d')
    name = name.replace('_V2', '_V4')
    save(im, name)

for name, cn, en in [('Passage_Pull_V1', P['pullCn'], P['pullEn']),
                     ('Passage_Push_V1', P['pushCn'], P['pushEn'])]:
    im = Image.new('RGB', (1024, 640), '#d8dacb'); d = ImageDraw.Draw(im)
    text(d, (512, 225), cn, 240, '#303a33')
    text(d, (512, 510), en, 130, '#303a33')
    save(im, name)
