"""Original integrated bilingual parking selector scale; not a certified diagram."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import math

root = Path(__file__).resolve().parents[2]
font = root / 'Assets/Resources/Fonts/SarasaMonoSC-Regular.ttf'
im = Image.new('RGBA', (900, 1300), (0, 0, 0, 0))
d = ImageDraw.Draw(im)
ink = '#e2decd'
def label(x, y, zh, en, size):
    d.text((x, y), zh, font=ImageFont.truetype(str(font), size), fill=ink, anchor='mm')
    d.text((x, y + size), en, font=ImageFont.truetype(str(font), int(size*.53)), fill=ink, anchor='mm')
label(450, 85, '停放制动', 'PARKING BRAKE', 70)
# Both legends align with the rotating hub pointer, opposite the grip.
# Plane mapping: pixel x increases toward -localX, pixel y toward +localZ.
for angle, zh, en in [(0, '缓解', 'RELEASE'), (65, '施加', 'APPLY')]:
    a = math.radians(angle)
    label(450 + math.sin(a)*.065/.165*900,
          650 - math.cos(a)*.065/.242*1300, zh, en, 52)
    inner, outer = .033, .041
    d.line((450+math.sin(a)*inner/.165*900, 650-math.cos(a)*inner/.242*1300,
            450+math.sin(a)*outer/.165*900, 650-math.cos(a)*outer/.242*1300), fill=ink, width=6)
# Short ungraduated connecting arc; this remains a two-position selector.
points=[]
for degree in range(66):
    a=math.radians(degree)
    points.append((450+math.sin(a)*.037/.165*900,650-math.cos(a)*.037/.242*1300))
d.line(points,fill=ink,width=3)
out = root / 'Assets/Resources/Cab/Cab_Parking_Selector_Print_V1.png'
im.save(out)
print('PARKING_SELECTOR_PRINT_OK')
