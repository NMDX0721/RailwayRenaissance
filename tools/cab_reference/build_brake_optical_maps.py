"""Authored layered surface maps: enamel orange peel, brushed steel and handled grip."""
from pathlib import Path
from PIL import Image,ImageFilter
import numpy as np,io
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/Models/Cab/Textures/BrakeRefinement';OUT.mkdir(parents=True,exist_ok=True)
N=512
def field(r,size):
    a=r.uniform(0,255,(size,size)).astype('uint8')
    return np.asarray(Image.fromarray(a).resize((N,N),Image.Resampling.BICUBIC).filter(ImageFilter.GaussianBlur(.65)),dtype=float)/255
def save(name,a):
    im=Image.fromarray(np.uint8(np.clip(a,0,1)*255));b=io.BytesIO();im.save(b,format='PNG');p=OUT/(name+'.png')
    if not p.exists() or p.read_bytes()!=b.getvalue():p.write_bytes(b.getvalue())
for tag,rgb,metal,smooth,seed in [('Coat',(69,78,72),.10,.36,621),('CastMetal',(75,86,78),.12,.40,624),('Steel',(132,137,129),.85,.48,622),('Grip',(48,38,32),0,.36,623)]:
    r=np.random.default_rng(seed);cloud=field(r,12);fine=field(r,200);grain=r.uniform(-1,1,(N,N));yy,xx=np.mgrid[0:N,0:N]/N
    if tag=='Steel':
        radius=np.sqrt((xx-.5)**2+(yy-.5)**2);lines=np.sin(radius*2400+cloud*3)
        height=lines*.022+fine*.02;variation=1-(cloud-.2)*.10+lines*.025
        gloss=smooth-(cloud-.4)*.16+lines*.025
    elif tag in ('Coat','CastMetal'):
        height=fine*.035+grain*.001;variation=1-(cloud-.2)*.025+grain*.003
        gloss=smooth-(cloud-.35)*.10+(fine-.5)*.08
    else:
        height=fine*.025;variation=.97+cloud*.06+grain*.003;gloss=smooth+(cloud-.5)*.12
    base=np.asarray(rgb)/255*variation[...,None]
    if tag in ('Steel','CastMetal'):
        stain=np.clip((cloud-.4)*1.8,0,.5)
        base*=1+stain[...,None]*np.array([.01,-.025,-.09])
    dy,dx=np.gradient(height);normal=np.stack((-dx*2.0,-dy*2.0,np.ones_like(dx)),axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
    ms=np.zeros((N,N,4));ms[...,:3]=metal;ms[...,3]=np.clip(gloss,.1,.85)
    save(f'Brake_{tag}_BaseColor_V2',base);save(f'Brake_{tag}_MetallicSmoothness_V2',ms);save(f'Brake_{tag}_Normal_V2',normal*.5+.5)
print('BRAKE_OPTICAL_MAPS_OK: orange-peel gloss, machining microgrooves, handled polymer')
