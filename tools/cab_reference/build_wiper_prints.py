"""Original bilingual labels for NF-5K wiper controls and internal drives."""
from pathlib import Path
import io
import math
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Resources/Cab'
FONT=ROOT/'Assets/Resources/Fonts/SarasaMonoSC-Regular.ttf'
def text(d,xy,value,size):
    assert '?' not in value
    font=ImageFont.truetype(str(FONT),size)
    d.text(xy,value,font=font,fill='#283b40',anchor='mm')
def save(im,name):
    data=io.BytesIO();im.save(data,format='PNG');p=OUT/(name+'.png')
    if not p.exists() or p.read_bytes()!=data.getvalue():p.write_bytes(data.getvalue())
im=Image.new('RGB',(1024,1054),'#c8c4af');d=ImageDraw.Draw(im)
def point(x,z):return((-x/68+.5)*1024,(z/70+.5)*1054)
text(d,point(0,-28),'双刮雨器 / WIPERS',48)
for i,(cn,en) in enumerate([('关','OFF'),('低','LOW'),('高','HIGH')]):
    a=math.radians(-60+i*60)
    d.line((point(16*math.sin(a),16*math.cos(a)),point(20*math.sin(a),20*math.cos(a))),fill='#263c40',width=8)
    x,z=26*math.sin(a),26*math.cos(a)
    text(d,point(x,z),cn,48);text(d,point(x,z+4),en,26)
text(d,point(0,-18),'W1 + W2',32)
save(im,'Cab_Wiper_Control_Scale_V1')
for index in (1,2):
    im=Image.new('RGB',(1024,384),'#c8c4af');d=ImageDraw.Draw(im)
    d.rectangle((10,10,1013,373),outline='#566361',width=3)
    text(d,(512,84),'刮雨器驱动组',78)
    text(d,(512,180),'WIPER DRIVE',62)
    text(d,(512,289),f'W{index} · 检修前断电 / ISOLATE BEFORE SERVICE',36)
    save(im,f'Cab_Wiper_Drive_W{index}_V1')
print('Wiper control and drive identification prints generated')
