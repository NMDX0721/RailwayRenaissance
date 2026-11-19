"""Local skin palette correction of working layers against user's original.

No image is regenerated. Original and previous PSD files remain immutable.
Four corresponding material samples are not a claim of pixelwise registration.
"""
from pathlib import Path
import sys,json,hashlib
import numpy as np
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Library/VNPSDVerify'))
from psd_tools import PSDImage
from psd_tools.api.layers import Group,PixelLayer

BASE=ROOT/'tools/vn/suiyue_eye_polish_20261106'
OUT=ROOT/'tools/vn/suiyue_skin_calibration_20261106'
MASTER=ROOT/'tools/vn/character_art_20261106/suiyue_with_tablet_final.png'
REFERENCE=BASE/'repairs/user_skin_color_reference.png'
POINTS=[('forehead',(545,240),(535,165)),('cheek_R',(466,351),(472,260)),
        ('cheek_L',(570,365),(550,265)),('chin',(530,405),(515,304))]


def sample(im,xy):
    x,y=xy
    return np.median(np.array(im.crop((x-2,y-2,x+3,y+3))).reshape(-1,im.getbands().__len__())[:,:3],axis=0)


def main():
    OUT.mkdir(exist_ok=True)
    reference=Image.open(REFERENCE).convert('RGB')
    before=Image.open(BASE/'psd_reopened.png').convert('RGBA')
    h,w=before.height,before.width
    yy,xx=np.indices((h,w))
    deltas=[];records=[];weights=[]
    for label,refpt,point in POINTS:
        a=sample(reference,refpt);b=sample(before,point);deltas.append(a-b)
        records.append(dict(label=label,referencePoint=refpt,workingPoint=point,target=a.tolist(),before=b.tolist()))
        # Smooth interpolation of correction, not interpolation of face texture.
        weights.append(1/np.maximum((xx-point[0])**2+(yy-point[1])**2,1)**2)
    weights=np.stack(weights);weights/=weights.sum(axis=0)
    correction=np.einsum('nhw,nc->hwc',weights,np.array(deltas))
    region=(xx>=390)&(xx<=630)&(yy>=130)&(yy<=320)
    changed=[0]
    def correct(image,left=0,top=0):
        a=np.array(image.convert('RGBA'));rgb=a[:,:,:3].astype(float)
        y2=top+a.shape[0];x2=left+a.shape[1]
        valid=(rgb[:,:,0]>110)&(rgb[:,:,1]>65)&(rgb[:,:,2]>45)&(rgb[:,:,0]-rgb[:,:,1]>17)&(rgb[:,:,1]-rgb[:,:,2]>3)&(a[:,:,3]>0)
        valid&=region[top:y2,left:x2]
        b=np.clip(np.round(rgb+correction[top:y2,left:x2]),0,255).astype(np.uint8)
        changed[0]+=int(valid.sum());a[:,:,:3][valid]=b[valid]
        return Image.fromarray(a)
    source=PSDImage.open(BASE/'suiyue_eye_polished.psd')
    target=PSDImage.new('RGBA',source.size,color=(0,0,0,0))
    def copy_group(src,dst):
        for item in src:
            if item.is_group():
                new=Group.new(dst,item.name);copy_group(item,new)
            else:
                image=item.topil().convert('RGBA')
                new=PixelLayer.frompil(correct(image,item.left,item.top),dst,name=item.name,top=item.top,left=item.left)
                new.clipping=item.clipping
            new.visible=item.visible;new.opacity=item.opacity
    copy_group(source,target)
    path=OUT/'suiyue_eye_skin_calibrated.psd';target.save(path)
    reopened=PSDImage.open(path)
    after=reopened.composite(force=True,ignore_preview=True,color=0,alpha=0,apply_icc=False).convert('RGBA')
    after.save(OUT/'calibrated_working.png')
    expected=correct(before)
    diff=np.abs(np.array(expected).astype(int)-np.array(after).astype(int))
    visible=np.array(after)[:,:,3]>0
    for record,(_,refpt,point) in zip(records,POINTS):
        record['after']=sample(after,point).tolist()
        record['residual']=list(np.array(record['after'])-np.array(record['target']))
        assert np.max(np.abs(record['residual']))<=2,record
    # Preserve all visible non-skin artwork outside the stated face window.
    b=np.array(before);a=np.array(after)
    assert np.array_equal(b[:,:,3],a[:,:,3]),'Alpha changed'
    assert np.max(np.abs(b[~region].astype(int)-a[~region].astype(int)))<=1,'Outside-face artwork changed'
    receipt=dict(scope='Local face-skin palette calibration, not rig or full photometric matching',
        reference=str(REFERENCE),sourcePSD=str(BASE/'suiyue_eye_polished.psd'),samples=records,
        sourceMasterSha256=hashlib.sha256(MASTER.read_bytes()).hexdigest(),
        alphaUnchanged=True,layerwiseVsFlatCalibrationMax=int(diff[visible].max()),
        clippingLayers=sum(x.clipping for x in reopened.descendants() if not x.is_group()),
        limitations=['Sample points are material correspondences, not identical image coordinates',
                     'Only skin-like pixels in the stated face region are adjusted',
                     'Neck/hands are not colour-calibrated without verified material samples',
                     'New raw forehead repair still requires registration and calibration before use',
                     'No cmo3/moc3 or Cubism/Unity/device acceptance'])
    (OUT/'verification.json').write_text(json.dumps(receipt,ensure_ascii=False,indent=2),encoding='utf-8')
    sheet=Image.new('RGB',(1200,480),(225,225,225));draw=ImageDraw.Draw(sheet)
    for i,(name,image,box) in enumerate([('Original palette',reference,(405,185,655,435)),
        ('Working - before',before,(420,130,620,326)),('Working - calibrated',after,(420,130,620,326))]):
        crop=image.crop(box).resize((390,390));sheet.paste(crop,(i*400,40),crop if crop.mode=='RGBA' else None)
        draw.text((i*400+10,12),name,fill='black')
    sheet.save(OUT/'skin_comparison.png')
    print('SUIYUE_SKIN_CALIBRATED',records,'PSD max',receipt['layerwiseVsFlatCalibrationMax'])


if __name__=='__main__':main()
