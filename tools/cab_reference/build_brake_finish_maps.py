"""Deterministic neutral industrial coatings for the two cab brake valves."""
from pathlib import Path
from PIL import Image
import math,random,io
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/Models/Cab/Textures/BrakeRefinement';OUT.mkdir(parents=True,exist_ok=True)
for tag,rgb,metal,smooth,seed in [('Coat',(77,85,91),.04,.24,211),('Steel',(146,150,153),.88,.40,212),('Grip',(30,32,33),0,.21,213)]:
    r=random.Random(seed);base=Image.new('RGB',(256,256));surface=Image.new('RGBA',base.size)
    for y in range(256):
        for x in range(256):
            grain=r.uniform(-2,2);patch=math.sin(x*.049)*math.cos(y*.037)*2
            base.putpixel((x,y),tuple(int(max(0,min(255,c+grain+patch))) for c in rgb))
            m=int(metal*255);a=int(max(0,min(255,smooth*255+grain*2+patch)))
            surface.putpixel((x,y),(m,m,m,a))
    for suffix,im in [('BaseColor',base),('MetallicSmoothness',surface)]:
        b=io.BytesIO();im.save(b,format='PNG');p=OUT/f'Brake_{tag}_{suffix}_V1.png'
        if not p.exists() or p.read_bytes()!=b.getvalue():p.write_bytes(b.getvalue())
print('BRAKE_FINISH_MAPS_OK: neutral coating, exposed steel and dark grip')
