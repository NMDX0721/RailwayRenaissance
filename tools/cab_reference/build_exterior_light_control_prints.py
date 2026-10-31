"""Original two-selector lighting face; print and runtime names share a catalog."""
from pathlib import Path
import json,math,io
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/Resources/Cab'
p=json.loads((OUT/'exterior_light_control_profile.json').read_text(encoding='utf-8'))
im=Image.new('RGBA',(2048,602),(255,251,237,0));d=ImageDraw.Draw(im)
def point(x,y):return((.5-x/.320)*2048,(.5-y/.094)*602)
def text(x,y,s,size):
    assert '?' not in s
    d.text(point(x,y),s,font=ImageFont.truetype(str(ROOT/'Assets/Resources/Fonts/SarasaMonoSC-Regular.ttf'),size),fill='#fffbed',stroke_width=1,anchor='mm')
for kind,cx in [('head',.078),('mode',-.078)]:
    for i in range(3 if kind=='head' else 6):
        a=math.radians(-60+i*60 if kind=='head' else -100+i*40)
        d.line((point(cx-math.sin(a)*.022,-.004+math.cos(a)*.022),point(cx-math.sin(a)*.027,-.004+math.cos(a)*.027)),fill='#fffbed',width=5)
        x=cx-math.sin(a)*.038;y=-.004+math.cos(a)*.038
        text(x,y+.002,p[kind+'ScaleZh'][i],33);text(x,y-.0045,p[kind+'ScaleEn'][i],23)
    text(cx,-.030,p[kind+'TitleZh'],33);text(cx,-.037,p[kind+'TitleEn'],22)
data=io.BytesIO();im.save(data,format='PNG');path=OUT/'Cab_Exterior_Control_Face_V1.png'
if not path.exists() or path.read_bytes()!=data.getvalue():path.write_bytes(data.getvalue())
print('EXTERIOR_LIGHT_CONTROL_PRINT_OK')
