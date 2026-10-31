"""Original engraved legends and a replaceable synthetic three-chime horn loop."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import io,math,random,struct,wave
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/Resources/Cab'
font=ROOT/'Assets/Resources/Fonts/SarasaMonoSC-Regular.ttf'
for tag,zh,en in [('Horn','鸣笛','HORN'),('Sand','撒砂','SAND')]:
    im=Image.new('RGBA',(512,150),(255,255,255,0));d=ImageDraw.Draw(im)
    d.text((256,49),zh,font=ImageFont.truetype(str(font),65),fill='#fffbed',anchor='mm',stroke_width=1)
    d.text((256,115),en,font=ImageFont.truetype(str(font),40),fill='#fffbed',anchor='mm')
    b=io.BytesIO();im.save(b,format='PNG');path=OUT/f'Cab_Service_{tag}_Legend_V1.png'
    if not path.exists() or path.read_bytes()!=b.getvalue():path.write_bytes(b.getvalue())
rate=44100;r=random.Random(2510)
noise=[r.uniform(-1,1) for _ in range(80)]
samples=[]
for i in range(rate):
    t=i/rate;value=0
    for f,g in [(311,1),(370,.75),(440,.6)]:
        for h in range(1,9):value+=g*math.sin(2*math.pi*f*h*t+.19*h)/(h**1.35)
    p=t*80;j=int(p);a=p-j
    value=(value*.19+(noise[j%80]*(1-a)+noise[(j+1)%80]*a)*.008)
    samples.append(struct.pack('<h',int(max(-.95,min(.95,value))*32767)))
data=io.BytesIO()
with wave.open(data,'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate);w.writeframes(b''.join(samples))
path=OUT/'NF5K_Horn_Synthetic_Loop_V1.wav'
if not path.exists() or path.read_bytes()!=data.getvalue():path.write_bytes(data.getvalue())
print('SERVICE_ASSETS_OK: bilingual print and original synthetic loop (not a vehicle recording)')
