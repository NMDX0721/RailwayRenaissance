"""Second art pass: real eye contours, complete iris assets and mouth parts.

Creates a separate working PSD; never rewrites the first-pass/source artwork.
The gaze previews are raster layer checks, NOT Cubism motion acceptance.
"""
from pathlib import Path
import json
import hashlib
import sys
import cv2
import numpy as np
from PIL import Image,ImageDraw

ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'tools/vn/suiyue_layers_20261106'
OUT=ROOT/'tools/vn/suiyue_eye_polish_20261106'
SOURCE=ROOT/'tools/vn/character_art_20261106/suiyue_with_tablet_final.png'
sys.path.insert(0,str(ROOT/'Library/VNPSDVerify'))
from psd_tools import PSDImage
from psd_tools.api.layers import Group,PixelLayer

W,H=992,1586
SRC=np.array(Image.open(SOURCE).convert('RGBA'))
RGB=SRC[:,:,:3]


def poly(points):
    result=np.zeros((H,W),np.uint8)
    cv2.fillPoly(result,[np.array(points,np.int32)],255)
    return result>0


def ellipse(cx,cy,rx,ry):
    result=np.zeros((H,W),np.uint8)
    cv2.ellipse(result,(cx,cy),(rx,ry),0,0,360,255,-1)
    return result>0


def extract(rgb,mask,alpha=None):
    result=np.zeros((H,W,4),np.uint8)
    result[:,:,:3]=rgb
    result[:,:,3]=np.where(mask,255 if alpha is None else alpha,0)
    result[result[:,:,3]==0,:3]=0
    return Image.fromarray(result)


def shift(image,dx,dy):
    return Image.fromarray(cv2.warpAffine(np.array(image),np.float32([[1,0,dx],[0,1,dy]]),(W,H)))


def masked(image,mask):
    result=np.array(image).copy();result[:,:,3]=np.where(mask,result[:,:,3],0)
    result[result[:,:,3]==0,:3]=0
    return Image.fromarray(result)


def align_mouth(filename='mouth_open_generated.png',eyes_edit=False):
    box=(360,110,664,414)
    donor=np.array(Image.open(OUT/'repairs'/filename).convert('RGBA').resize((304,304),Image.Resampling.LANCZOS))
    original=np.array(Image.open(BASE/'face_repair_reference.png').convert('RGB'))
    mask=np.ones((304,304),np.uint8)*255;mask[155:195,125:180]=0
    if eyes_edit:mask[70:155,55:265]=0
    warp=np.eye(2,3,dtype=np.float32)
    score,warp=cv2.findTransformECC(cv2.cvtColor(original,cv2.COLOR_RGB2GRAY).astype(np.float32)/255,
        cv2.cvtColor(donor[:,:,:3],cv2.COLOR_RGB2GRAY).astype(np.float32)/255,warp,cv2.MOTION_AFFINE,
        (cv2.TERM_CRITERIA_EPS|cv2.TERM_CRITERIA_COUNT,120,1e-5),mask,5)
    donor=cv2.warpAffine(donor,warp,(304,304),flags=cv2.INTER_LINEAR|cv2.WARP_INVERSE_MAP,borderMode=cv2.BORDER_REFLECT_101)
    canvas=np.zeros((H,W,4),np.uint8);canvas[110:414,360:664]=donor
    Image.fromarray(canvas).save(OUT/'repairs'/(Path(filename).stem+'_aligned.png'))
    return canvas,dict(correlation=score,affine=warp.tolist(),crop=box)


def main():
    (OUT/'layers').mkdir(exist_ok=True)
    manifest=json.loads((BASE/'manifest.json').read_text(encoding='utf-8'))
    dark=(RGB[:,:,0]<175)&(RGB[:,:,1]<135)
    eyes={
        'R':dict(opening=[(441,214),(452,211),(466,212),(482,216),(494,222),(493,231),(485,237),(474,241),(462,240),(451,234),(444,226)],
          lid=[(429,209),(440,204),(451,202),(463,204),(478,207),(491,211),(501,220),(502,226),(491,219),(478,216),(464,213),(451,213),(441,217)],
          brow=[(457,181),(475,183),(490,190),(508,199),(507,204),(495,199),(474,190),(457,188)],
          protect=[(475,169),(485,170),(491,207),(483,215),(477,201)],iris=(469,225,14,20)),
        'L':dict(opening=[(539,233),(550,227),(563,226),(579,229),(589,233),(588,242),(579,249),(567,254),(555,254),(545,249),(540,242)],
          lid=[(532,230),(541,223),(555,219),(569,218),(584,220),(593,223),(604,223),(602,233),(592,233),(582,228),(569,226),(553,228),(541,234)],
          brow=[(536,199),(551,192),(572,189),(591,191),(594,197),(576,197),(554,199),(537,204)],
          protect=[(560,175),(572,173),(575,205),(564,215),(556,217),(563,199)],iris=(555,236,14,20))}
    feature_masks={}
    for side,e in eyes.items():
        e['opening_mask']=poly(e['opening'])
        e['upper_mask']=poly(e['lid'])&dark
        e['brow_mask']=poly(e['brow'])&dark&~poly(e['protect'])
        # Single-pixel anti-alias neighbours belong to the same contour.
        e['brow_mask']=(cv2.dilate(e['brow_mask'].astype(np.uint8),np.ones((3,3),np.uint8))>0)&poly(e['brow'])&~poly(e['protect'])
        for kind,mask in [('opening',e['opening_mask']),('upper_lid',e['upper_mask']),('brow',e['brow_mask'])]:feature_masks[side+'_'+kind]=mask
    # Remove only the old iris-rim fragments beneath the nominal eye aperture.
    # Broad contour rebuilding changed the accepted eye silhouette and is rejected.
    yy,xx=np.indices((H,W))
    residue_masks={}
    for side,e in eyes.items():
        cx,cy,rx,ry=e['iris']
        guard=ellipse(cx,cy,rx+3,ry+3)
        residue_masks[side]=guard&~e['opening_mask']&(yy>cy+10)&(RGB[:,:,0]<195)&(RGB[:,:,1]<180)
    residue=np.logical_or.reduce(list(residue_masks.values()))
    cut=np.logical_or.reduce(list(feature_masks.values()))|residue
    # Merge the first-pass facial pieces before recutting. All remaining source
    # pieces retain their exact visible pixels and z order.
    merged=Image.new('RGBA',(W,H))
    old=[]
    for r in manifest['layers']:
        image=Image.open(BASE/'layers'/(r['name']+'.png')).convert('RGBA')
        if r['category'] in ('eyes','brows'):
            merged=Image.alpha_composite(merged,image)
        else:
            old.append((r['name'],r['category'],image))
    old.append(('facial_edge_pixels','face',merged))
    original_layers=[(name,category,masked(image,~cut)) for name,category,image in old]
    clean_face=np.array(Image.open(BASE/'repairs/aligned_face_base_clean_v2.png').convert('RGBA'))
    facefill=np.zeros((H,W),bool)
    for e in eyes.values():facefill|=e['opening_mask']|e['upper_mask']|e['brow_mask']
    facefill|=residue
    # This plate is entirely beneath the retained eye/brow artwork at rest.
    plate=extract(clean_face[:,:,:3],facefill)
    blank=np.array(Image.open(BASE/'repairs/aligned_eye_sclera_generated.png').convert('RGBA'))
    discs=np.array(Image.open(OUT/'repairs/iris_discs_generated.png').convert('RGBA'))
    components,labels,stats,centroids=cv2.connectedComponentsWithStats((discs[:,:,3]>100).astype(np.uint8))
    candidates=sorted([s for s in stats[1:] if s[4]>10000],key=lambda s:s[0])
    assert len(candidates)==2,'Iris donor must contain two separate discs'
    eye_assets={}
    for i,(side,e) in enumerate(eyes.items()):
        cx,cy,rx,ry=e['iris'];opening=e['opening_mask']
        iris_mask=ellipse(cx,cy,rx,ry)
        x,y,w,h,area=candidates[i]
        donor=Image.fromarray(discs[y:y+h,x:x+w]).resize((rx*2+1,ry*2+1),Image.Resampling.LANCZOS)
        iris=np.zeros((H,W,4),np.uint8);iris[cy-ry:cy+ry+1,cx-rx:cx+rx+1]=np.array(donor)
        iris[:,:,3]=np.where(iris_mask,255,0)
        # Preserve the approved visible iris pixels; generated texture fills only
        # the previously occluded portion of the complete disc.
        visible=iris_mask&opening&~e['upper_mask']
        # Retain the contiguous actual iris, not isolated lower-lid flecks that
        # happen to fall inside an approximate elliptical extraction mask.
        candidate=visible&(RGB.max(axis=2)<200)&(RGB[:,:,2]>=RGB[:,:,1])
        n,labels,stats,_=cv2.connectedComponentsWithStats(candidate.astype(np.uint8))
        largest=1+int(np.argmax(stats[1:,4]))
        contours,_=cv2.findContours((labels==largest).astype(np.uint8),cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
        contiguous=np.zeros((H,W),np.uint8);cv2.drawContours(contiguous,contours,-1,255,-1)
        visible&=contiguous>0
        iris[visible]=SRC[visible]
        iris[iris[:,:,3]==0,:3]=0
        white=np.clip(blank[:,:,:3],160,255)
        # The original restoration also copied the iris rim just outside its
        # nominal ellipse into the stationary sclera. Restore only genuine white.
        guard=ellipse(cx,cy,rx+3,ry+3)
        safe_white=(RGB.min(axis=2)>165)&(RGB.max(axis=2).astype(int)-RGB.min(axis=2).astype(int)<45)
        white[opening&~guard&safe_white]=RGB[opening&~guard&safe_white]
        sclera=extract(white,opening)
        iris_image=Image.fromarray(iris)
        upper=extract(RGB,e['upper_mask'],SRC[:,:,3])
        brow=extract(RGB,e['brow_mask'],SRC[:,:,3])
        eye_assets[side]=dict(sclera=sclera,iris=iris_image,upper=upper,brow=brow,mask=opening)
        for name,image in [('sclera_complete',sclera),('iris_complete',iris_image),('lid_upper',upper),('brow',brow)]:
            image.save(OUT/'layers'/(side+'_'+name+'.png'))
    donor,registration=align_mouth()
    roi=poly([(489,270),(535,270),(535,302),(489,302)])
    # Detect the generated mouth contour rather than relying on nominal crop size.
    mouthdark=roi&(donor[:,:,0]<220)&(donor[:,:,1]<155)&(donor[:,:,0].astype(int)-donor[:,:,1].astype(int)>10)
    dental=roi&(donor[:,:,1]>180)&(np.abs(donor[:,:,0].astype(int)-donor[:,:,1].astype(int))<30)
    n,labels,stats,_=cv2.connectedComponentsWithStats((mouthdark|dental).astype(np.uint8))
    label=1+int(np.argmax(stats[1:,4]));seed=(labels==label).astype(np.uint8)
    contours,_=cv2.findContours(seed,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
    outer=np.zeros((H,W),np.uint8);cv2.drawContours(outer,contours,-1,255,-1);outer=outer>0
    yy,xx=np.indices((H,W));ys=yy[outer];middle=(int(ys.min())+int(ys.max()))/2
    teeth=outer&(donor[:,:,0]>165)&(donor[:,:,1]>140)
    tongue=outer&~teeth&(yy>middle)&(donor[:,:,0]>100)&(donor[:,:,1]>35)
    cavity=outer&~teeth&~tongue
    # Preserve the painted lip antialias and immediate skin transition. Cutting
    # only the dark cavity made the first open-mouth preview look serrated.
    edge=(cv2.dilate(outer.astype(np.uint8),np.ones((7,7),np.uint8))>0)&~outer
    upper_lip=edge&(yy<=middle)
    lower_lip=edge&(yy>middle)
    mouth_assets={name:extract(donor[:,:,:3],mask) for name,mask in [('mouth_cavity',cavity),('mouth_teeth',teeth),('mouth_tongue',tongue),('mouth_upper_lip',upper_lip),('mouth_lower_lip',lower_lip)]}
    assert all(image.getbbox() for image in mouth_assets.values()),'Empty mouth material'
    for name,image in mouth_assets.items():image.save(OUT/'layers'/(name+'.png'))
    closed_donor,closed_registration=align_mouth('closed_eyes_generated.png',eyes_edit=True)
    closed_patches={}
    closed_shapes={
        'R':[(434,203),(457,198),(487,206),(506,221),(497,241),(479,248),(457,245),(440,232)],
        'L':[(529,223),(552,214),(583,216),(606,227),(597,244),(577,260),(549,260),(533,247)]}
    for side,points in closed_shapes.items():
        region=poly(points)
        edge=np.minimum(cv2.distanceTransform(region.astype(np.uint8),cv2.DIST_L2,3)*128,255).astype(np.uint8)
        closed_patches[side]=extract(closed_donor[:,:,:3],region,edge)
        closed_patches[side].save(OUT/'layers'/('closed_lid_key_reference_'+side+'.png'))
    plate.save(OUT/'layers/face_under_eyes.png')
    def render(dx=0,dy=0,mouth_open=False):
        result=plate.copy()
        for name,category,image in original_layers:
            if mouth_open and name=='mouth_closed':continue
            result=Image.alpha_composite(result,image)
        for side,a in eye_assets.items():
            result=Image.alpha_composite(result,a['sclera'])
            result=Image.alpha_composite(result,masked(shift(a['iris'],dx,dy),a['mask']))
            result=Image.alpha_composite(result,a['upper'])
            result=Image.alpha_composite(result,a['brow'])
        if mouth_open:
            hole=np.array(Image.open(BASE/'layers/mouth_closed.png'))[:,:,3]>0
            skin=extract(clean_face[:,:,:3],cv2.dilate(hole.astype(np.uint8),np.ones((5,5),np.uint8))>0)
            result=Image.alpha_composite(result,skin)
            for image in mouth_assets.values():result=Image.alpha_composite(result,image)
        return result
    neutral=render();neutral.save(OUT/'neutral_working.png')
    bbox=(405,180,612,262)
    checks=[];frames=[]
    for dx,dy in [(-2,0),(0,0),(2,0),(0,-2),(0,2),(0,0)]:
        image=render(dx,dy);a=np.array(image)
        for side,e in eyes.items():
            assert np.all(a[:,:,3][e['opening_mask']]==255),'Eye opening contains alpha holes'
        image.crop(bbox).resize((828,328),Image.Resampling.NEAREST).save(OUT/('gaze_%s_%s.png'%(dx,dy)))
        frames.append(image.crop((380,135,633,328)).resize((759,579)))
        checks.append(dict(dx=dx,dy=dy,opaqueEyeOpenings=True))
    frames[0].save(OUT/'gaze_layer_review.gif',save_all=True,append_images=frames[1:],duration=500,loop=0,disposal=2)
    render(mouth_open=True).save(OUT/'mouth_open_working.png')
    closed=neutral.copy()
    for image in closed_patches.values():closed=Image.alpha_composite(closed,image)
    closed.save(OUT/'closed_eyes_working.png')
    psd=PSDImage.new('RGBA',(W,H),color=(0,0,0,0))
    def put(parent,name,image,clipping=False,visible=True):
        bounds=image.getbbox();assert bounds,name
        layer=PixelLayer.frompil(image.crop(bounds),parent,name=name,top=bounds[1],left=bounds[0]);layer.clipping=clipping;layer.visible=visible
        return layer
    group=Group.new(psd,'00_face_underlay');put(group,'face_under_eyes',plate)
    group=Group.new(psd,'01_original_parts')
    for name,category,image in original_layers:
        if image.getbbox():put(group,name,image)
        image.save(OUT/'layers'/('source_'+name+'.png'))
    for side,a in eye_assets.items():
        group=Group.new(psd,'eye_'+side)
        put(group,'sclera_complete',a['sclera'])
        put(group,'iris_complete',a['iris'],clipping=True)
        put(group,'upper_lid',a['upper'])
        put(group,'brow',a['brow'])
    group=Group.new(psd,'mouth_open_ALTERNATIVE_HIDDEN')
    for name,image in mouth_assets.items():put(group,name,image)
    group.visible=False
    group=Group.new(psd,'closed_eyes_KEY_REFERENCE_HIDDEN')
    for side,image in closed_patches.items():put(group,'closed_lid_key_reference_'+side,image)
    group.visible=False
    path=OUT/'suiyue_eye_polished.psd';psd.save(path)
    reopened=PSDImage.open(path)
    reread=reopened.composite(force=True,ignore_preview=True,color=0,alpha=0,apply_icc=False).convert('RGBA');reread.save(OUT/'psd_reopened.png')
    diff=np.abs(np.array(reread).astype(int)-np.array(neutral).astype(int));active=np.array(neutral)[:,:,3]>0
    original_diff=np.abs(np.array(neutral).astype(int)-SRC.astype(int))
    receipt=dict(scope='Raster source-layer refinement, NOT Cubism/rig acceptance',canvas=[W,H],sourceSha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        gazeChecks=checks,irisClippingLayers=sum(x.clipping for x in reopened.descendants() if not x.is_group()),
        psdRoundTrip=dict(maxDelta=int(diff[active].max()),meanDelta=float(diff[active].mean())),
        changedOriginalPixels=int(np.any(original_diff>0,axis=2).sum()),mouthRegistration=registration,
        layerCount=sum(not x.is_group() for x in reopened.descendants()),mouthParts=list(mouth_assets),closedEyeRegistration=closed_registration,
        removedStaticRimPixels={side:int(mask.sum()) for side,mask in residue_masks.items()},
        fixes=['Keep original eye aperture and upper lashes','Prevent original iris rim from being copied into sclera','Remove only static iris-rim residue below aperture'],
        pending=['Visual seam/contour review','Convert closed-lid key reference to actual deformed mesh/keys','Mouth cavity overlap completion',
                 'Forehead/neck/arms occlusion completion','No-tablet pose matching','Cubism clipping configuration and actual import','cmo3/moc3 and Unity dynamic acceptance'])
    (OUT/'verification.json').write_text(json.dumps(receipt,ensure_ascii=False,indent=2),encoding='utf-8')
    assert receipt['psdRoundTrip']['meanDelta']<.05,receipt['psdRoundTrip']
    print('SUIYUE_FACE_REFINEMENT_SAVED',receipt)


if __name__=='__main__':main()
