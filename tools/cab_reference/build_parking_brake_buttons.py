"""Original bilingual two-button panel prints; source labels shared with runtime."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json
root=Path(__file__).resolve().parents[2]
profile=json.loads((root/'Assets/Resources/Cab/parking_brake_profile.json').read_text(encoding='utf-8'))
font=root/'Assets/Resources/Fonts/SarasaMonoSC-Regular.ttf'
for key in ('title','apply','release'):
    im=Image.new('RGBA',(640,240),(0,0,0,0));d=ImageDraw.Draw(im)
    d.text((320,76),profile[key]['zh'],font=ImageFont.truetype(str(font),72),fill='#e4dfd0',anchor='mm')
    d.text((320,171),profile[key]['en'],font=ImageFont.truetype(str(font),39 if key!='title' else 31),fill='#e4dfd0',anchor='mm')
    im.save(root/f'Assets/Resources/Cab/Cab_Parking_{key}_V6.png')
print('PARKING_BUTTON_PRINTS_OK')
