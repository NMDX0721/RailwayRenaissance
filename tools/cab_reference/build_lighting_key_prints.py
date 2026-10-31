"""Bilingual fixed hardware labels from the same catalog as contextual interaction."""
import io
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Resources/Cab'
PROFILE=json.loads((OUT/'lighting_keys_profile.json').read_text(encoding='utf-8'))
FONT=ROOT/'Assets/Resources/Fonts/SarasaMonoSC-Regular.ttf'

def text(d,xy,value,size):
    assert '?' not in value
    f=ImageFont.truetype(str(FONT),size)
    box=d.multiline_textbbox((0,0),value,font=f,spacing=6,align='center')
    assert box[2]-box[0]<360, (value,box)
    d.multiline_text((xy[0],xy[1]),value,font=f,fill='#fffbed',stroke_width=1,stroke_fill='#fffbed',anchor='mm',align='center',spacing=8)

def save(im,name):
    data=io.BytesIO();im.save(data,format='PNG');path=OUT/(name+'.png')
    if not path.exists() or path.read_bytes()!=data.getvalue():path.write_bytes(data.getvalue())

for i,e in enumerate(PROFILE['entries']):
    im=Image.new('RGBA',(384,600),(255,251,237,0));d=ImageDraw.Draw(im)
    text(d,(192,250),e['printZh'],104 if len(e['printZh'].split('\n')[0])<4 else 84)
    text(d,(192,515),e['printEn'],56)
    save(im,f'Cab_Lighting_Key_L{i+1:02}_Integrated_V2')
print('LIGHTING_KEY_PRINTS_OK: function-only bilingual inserts, no title or visible numbering')
