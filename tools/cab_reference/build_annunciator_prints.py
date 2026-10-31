"""Original bilingual engraved diffuser legends; no manufacturer artwork copied."""
from pathlib import Path
import json,io,math,random
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Resources/Cab'
p=json.loads((OUT/'annunciator_profile.json').read_text(encoding='utf-8'))
font=ROOT/'Assets/Resources/Fonts/SarasaMonoSC-Regular.ttf'
for i,(zh,en) in enumerate(zip(p['zh'],p['en'])):
    im=Image.new('RGBA',(512,240),(255,255,255,0));d=ImageDraw.Draw(im)
    d.text((256,85),zh,font=ImageFont.truetype(str(font),68),fill='#fffbed',anchor='mm',stroke_width=1)
    d.text((256,167),en,font=ImageFont.truetype(str(font),40),fill='#fffbed',anchor='mm')
    data=io.BytesIO();im.save(data,format='PNG');path=OUT/f'Cab_Annunciator_Legend_{i:02}.png'
    if not path.exists() or path.read_bytes()!=data.getvalue():path.write_bytes(data.getvalue())
print('ANNUNCIATOR_PRINTS_OK: 10 uniform bilingual legends')

# Authored optical maps: two diffused light sources behind a shallow frosted pane.
r=random.Random(61025)
frost=Image.new('RGBA',(512,240));emission=Image.new('RGB',frost.size);normal=Image.new('RGB',frost.size)
for y in range(240):
    for x in range(512):
        u=(x+.5)/512;v=(y+.5)/240
        edge=max(0,min(1,min(u,1-u)*18,min(v,1-v)*12))
        edge=edge*edge*(3-2*edge)
        a=math.exp(-(((u-.32)/.38)**2+((v-.50)/.65)**2)*1.5)
        b=math.exp(-(((u-.68)/.38)**2+((v-.50)/.65)**2)*1.5)
        grain=r.uniform(-1,1)
        light=(.14+.66*(a+b)/1.38)*edge*(1+grain*.014)
        shade=int(216+grain*4-22*(1-edge))
        frost.putpixel((x,y),(shade,shade,shade,255))
        q=int(max(0,min(255,light*255)));emission.putpixel((x,y),(q,q,q))
        normal.putpixel((x,y),(int(128+r.uniform(-3,3)),int(128+r.uniform(-3,3)),255))
for name,im in [('Frost',frost),('Diffusion',emission),('MicroNormal',normal)]:
    data=io.BytesIO();im.save(data,format='PNG');path=OUT/f'Cab_Annunciator_{name}_V2.png'
    if not path.exists() or path.read_bytes()!=data.getvalue():path.write_bytes(data.getvalue())
print('ANNUNCIATOR_OPTICS_OK: frosted surface, bounded two-source diffusion and subtle normal')
im=Image.new('RGBA',(320,240),(255,255,255,0));d=ImageDraw.Draw(im)
d.text((160,78),'试灯',font=ImageFont.truetype(str(font),78),fill='#fffbed',anchor='mm',stroke_width=1)
d.text((160,170),'LAMP TEST',font=ImageFont.truetype(str(font),40),fill='#fffbed',anchor='mm')
data=io.BytesIO();im.save(data,format='PNG');path=OUT/'Cab_Annunciator_Test_Label_V1.png'
if not path.exists() or path.read_bytes()!=data.getvalue():path.write_bytes(data.getvalue())
