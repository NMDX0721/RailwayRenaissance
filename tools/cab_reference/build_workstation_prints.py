"""Original bilingual cab communication and crew record artwork."""
from pathlib import Path
import json
import io
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Resources/Cab'
PROFILE = json.loads((OUT / 'workstation_equipment_profile.json').read_text(encoding='utf-8'))
FONT = ROOT / 'Assets/Resources/Fonts/SarasaMonoSC-Regular.ttf'

def label(draw, xy, value, size, color='#242c28'):
    draw.text(xy, value, font=ImageFont.truetype(str(FONT), size), fill=color)

def save(image, name):
    stream = io.BytesIO()
    image.save(stream, format='PNG')
    path = OUT / name
    if not path.exists() or path.read_bytes() != stream.getvalue():
        path.write_bytes(stream.getvalue())

im = Image.new('RGB', (1024, 384), '#87978c')
d = ImageDraw.Draw(im)
label(d, (28, 14), PROFILE['radioTitle'], 34)
d.line((24, 62, 1000, 62), fill='#343f39', width=2)
label(d, (245, 78), '音量 / VOL', 32)
label(d, (28, 78), '频道 / CH', 32)
label(d, (570, 310), 'SL-R8  •  NF-5K', 26)
label(d, (280, 330), '0—5', 24)
label(d, (48, 330), '01—08', 24)
save(im, 'Cab_Radio_Face_V1.png')

im = Image.new('RGB', (1024, 1408), '#d8d4be')
d = ImageDraw.Draw(im)
for y in range(1408):
    if y % 17 == 0:
        d.line((0, y, 1024, y), fill='#d5d1bb')
d.rectangle((38, 38, 986, 1370), outline='#576458', width=3)
label(d, (76, 74), 'NF-5K  随车检查记录', 53)
label(d, (76, 147), 'ONBOARD CHECK RECORD   CW-01', 34)
label(d, (76, 215), '日期 DATE ______  乘务 CREW ______', 30)
entries = [('制动与风压', 'BRAKES & AIR'), ('通信设备', 'COMMUNICATIONS'),
           ('车门与通道', 'DOORS & AISLES'), ('安全器材', 'SAFETY EQUIPMENT')]
for i, (cn, en) in enumerate(entries):
    y = 310 + i * 138
    d.rectangle((78, y+12, 112, y+46), outline='#455247', width=3)
    label(d, (140, y), cn, 42)
    label(d, (140, y+56), en, 30)
    d.line((78, y+113, 944, y+113), fill='#8c9480', width=2)
label(d, (78, 914), '途中记录 / EN-ROUTE NOTES', 35)
for y in (1000, 1085, 1170):
    d.line((78, y, 944, y), fill='#8c9480', width=2)
label(d, (78, 1240), '交班 OUT ______  接班 IN ______', 32)
label(d, (78, 1310), '随车留存 / KEEP ON BOARD', 26)
save(im, 'Cab_Crew_Record_V1.png')
