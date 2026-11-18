"""Refine internal hair overlap and retain honest layer-offset probes.

An art-source check only. Main VN and Cubism model exports are unchanged.
"""
from pathlib import Path
import sys,json,hashlib
import numpy as np
import cv2
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Library/VNPSDVerify'))
from psd_tools import PSDImage
from psd_tools.api.layers import Group,PixelLayer
BASE=ROOT/'tools/vn/suiyue_skin_calibration_20261106'
ART=ROOT/'tools/vn/suiyue_eye_polish_20261106/repairs/forehead_underpaint_generated.png'
OUT=ROOT/'tools/vn/suiyue_hair_seams_20261106'
W,H=992,1586


def polygon(points):
    a=np.zeros((H,W),np.uint8);cv2.fillPoly(a,[np.array(points,np.int32)],255);return a>0


def image_mask(rgb,mask):
    a=np.zeros((H,W,4),np.uint8);a[:,:,:3]=rgb;a[:,:,3]=mask*255;a[~mask,:3]=0;return Image.fromarray(a)


def median(image,xy):
    x,y=xy;return np.median(image[y-2:y+3,x-2:x+3,:3].reshape(-1,3),axis=0)


def field_match(donor,target,points):
    yy,xx=np.indices((H,W));weights=[];delta=[]
    for xy in points:
        delta.append(median(target,xy)-median(donor,xy));weights.append(1/np.maximum((xx-xy[0])**2+(yy-xy[1])**2,1)**2)
    weights=np.stack(weights);weights/=weights.sum(axis=0)
    shift=np.einsum('nhw,nc->hwc',weights,np.array(delta))
    return np.clip(np.round(donor[:,:,:3].astype(float)+shift),0,255).astype(np.uint8),[x.tolist() for x in delta]


def main():
    (OUT/'layers').mkdir(parents=True,exist_ok=True)
    before=Image.open(BASE/'calibrated_working.png').convert('RGBA');target=np.array(before)
    source_crop=np.array(Image.open(ROOT/'tools/vn/suiyue_layers_20261106/face_repair_reference.png').convert('RGB'))
    donor=np.array(Image.open(ART).convert('RGBA').resize((304,304),Image.Resampling.LANCZOS))
    stable=np.zeros((304,304),np.uint8)
    cv2.fillPoly(stable,[np.array([(83,107),(230,120),(228,175),(157,207),(94,179)],np.int32)],255)
    warp=np.eye(2,3,dtype=np.float32)
    score,warp=cv2.findTransformECC(cv2.cvtColor(source_crop,cv2.COLOR_RGB2GRAY).astype(np.float32)/255,
        cv2.cvtColor(donor[:,:,:3],cv2.COLOR_RGB2GRAY).astype(np.float32)/255,warp,cv2.MOTION_AFFINE,
        (cv2.TERM_CRITERIA_COUNT|cv2.TERM_CRITERIA_EPS,180,1e-6),stable,5)
    assert abs(float(np.linalg.det(warp[:,:2]))-1)<.1 and abs(warp[:,2]).max()<10,warp
    donor=cv2.warpAffine(donor,warp,(304,304),flags=cv2.INTER_LINEAR|cv2.WARP_INVERSE_MAP,borderMode=cv2.BORDER_REFLECT_101)
    full=np.zeros((H,W,4),np.uint8);full[110:414,360:664]=donor
    Image.fromarray(full).save(OUT/'aligned_underpaint.png')
    face_rgb,face_delta=field_match(full,target,[(535,165),(472,260),(550,265),(515,304)])
    neck_rgb,neck_delta=field_match(full,target,[(500,330),(515,365)])
    source=PSDImage.open(BASE/'suiyue_eye_skin_calibrated.psd')
    front_names={'hair_front_R','hair_front_L','hair_front_long_R','hair_front_long_L'}
    front=np.zeros((H,W),bool)
    cleaned_parts={};static_parts={};reclassified={};matte_counts={}
    for layer in source.descendants():
        if layer.is_group() or layer.name not in front_names:continue
        pixels=np.array(layer.topil().convert('RGBA'));c=pixels[:,:,:3].astype(int)
        hair_like=(c[:,:,2]>=c[:,:,1]-2)&(c[:,:,0]-c[:,:,2]<35)&(c.max(axis=2)<195)
        original=pixels[:,:,3]>0
        moving=pixels.copy();moving[:,:,3]=np.where(hair_like,pixels[:,:,3],0);moving[moving[:,:,3]==0,:3]=0
        stationary=pixels.copy();stationary[:,:,3]=np.where(~hair_like,pixels[:,:,3],0);stationary[stationary[:,:,3]==0,:3]=0
        # Decontaminate source antialias where hair was originally painted over
        # skin. A binary cut retains skin-coloured fringe RGB as a pale trail.
        bg=face_rgb[layer.top:layer.bottom,layer.left:layer.right].astype(float)
        core=original&hair_like&(c.max(axis=2)<125)
        if core.any():
            _,nearest=cv2.distanceTransformWithLabels((~core).astype(np.uint8),cv2.DIST_L2,5,labelType=cv2.DIST_LABEL_PIXEL)
            core_colors=c[core].astype(float)
            fg=core_colors[np.clip(nearest-1,0,len(core_colors)-1)]
            vector=fg-bg;denom=(vector*vector).sum(axis=2)
            fraction=np.clip(((c-bg)*vector).sum(axis=2)/np.maximum(denom,1),0,1)
            residual=np.abs(c-(bg+vector*fraction[:,:,None])).max(axis=2)
            minimum=np.where(c<bg,(bg-c)/np.maximum(bg,1),(c-bg)/np.maximum(255-bg,1)).max(axis=2)
            fraction=np.clip(np.maximum(fraction,minimum+1e-4),0,1)
            edge_distance=cv2.distanceTransform(original.astype(np.uint8),cv2.DIST_L2,3)
            bg_skin=(bg[:,:,0]>170)&(bg[:,:,0]-bg[:,:,1]>18)&(bg[:,:,1]-bg[:,:,2]>3)
            matte=original&bg_skin&(edge_distance<=4)&(residual<20)&(fraction>.02)&(fraction<.95)
            a=pixels[:,:,3].astype(float)/255
            moving_a=a*fraction
            fixed_a=np.clip((a-moving_a)/np.maximum(1-moving_a,1e-6),0,1)
            recovered=np.clip((c-bg*(1-fraction[:,:,None]))/np.maximum(fraction[:,:,None],.02),0,255)
            moving[:,:,:3][matte]=np.round(recovered[matte]).astype(np.uint8)
            moving[:,:,3][matte]=np.round(moving_a[matte]*255).astype(np.uint8)
            stationary[:,:,:3][matte]=bg[matte].astype(np.uint8)
            stationary[:,:,3][matte]=np.round(fixed_a[matte]*255).astype(np.uint8)
            matte_counts[layer.name]=int(matte.sum())
        cleaned_parts[layer.name]=Image.fromarray(moving);static_parts[layer.name]=Image.fromarray(stationary)
        reclassified[layer.name]=int((original&~hair_like).sum())
        front[layer.top:layer.bottom,layer.left:layer.right]|=moving[:,:,3]>0
    support=cv2.dilate(front.astype(np.uint8),np.ones((7,7),np.uint8))>0
    skin=(full[:,:,0]>160)&(full[:,:,0].astype(int)-full[:,:,1].astype(int)>18)&(full[:,:,1].astype(int)-full[:,:,2].astype(int)>3)
    # Only paint behind existing opaque hair, never replace the visible face.
    opaque=target[:,:,3]>=250
    forehead=polygon([(420,197),(444,160),(476,138),(513,135),(548,143),(582,165),(601,207),(593,234),(561,221),(528,205),(496,201),(462,219),(438,242)])
    temples=polygon([(396,210),(437,206),(445,244),(430,273),(403,263)])|polygon([(584,232),(619,232),(625,267),(609,290),(589,283)])
    face_mask=(forehead|temples)&front&skin&opaque
    neck=polygon([(432,295),(467,294),(554,298),(579,395),(505,407),(450,366)])
    neck_mask=neck&front&skin&opaque
    assert int(face_mask.sum())>100,face_mask.sum()
    assets=[('forehead_temples_underpaint',image_mask(face_rgb,face_mask))]
    if neck_mask.any():assets.append(('neck_under_front_strands',image_mask(neck_rgb,neck_mask)))
    # A skin-only mask left unfilled edges where the strand moved across back
    # hair or clothing. Use the same registered, strand-free source materials
    # under the complete original foreground-strand footprint.
    yy,xx=np.indices((H,W))
    head_support=support&(xx>=360)&(xx<664)&(yy>=110)&(yy<414)&(full[:,:,3]>240)&opaque
    head_rgb=full[:,:,:3].copy();head_rgb[skin]=face_rgb[skin]
    assets.insert(0,('head_surface_behind_front_strands',image_mask(head_rgb,head_support)))
    clothes=np.array(Image.open(ROOT/'tools/vn/suiyue_layers_20261106/repairs/aligned_torso_clean_generated.png').convert('RGBA'))
    cloth_support=support&(yy>=414)&(yy<470)&(clothes[:,:,3]>240)&opaque
    if cloth_support.any():assets.insert(0,('cloth_behind_strand_tips',image_mask(clothes[:,:,:3],cloth_support)))
    # Internal hair-to-hair cut boundaries need overlapping HAIR, not forehead
    # skin. Extend the neighbouring stationary hair texture by four pixels.
    feature=np.zeros((H,W),bool)
    for layer in source.descendants():
        if layer.is_group() or layer.name not in {'brow','upper_lid','sclera_complete','iris_complete'}:continue
        pixels=np.array(layer.topil().convert('RGBA'))
        feature[layer.top:layer.bottom,layer.left:layer.right]|=pixels[:,:,3]>0
    c=target[:,:,:3].astype(int)
    stationary_hair=~front&opaque&(c[:,:,2]>=c[:,:,1]-2)&(c[:,:,0]-c[:,:,2]<35)&(c.max(axis=2)<175)&(yy<414)&~feature
    distance,nearest=cv2.distanceTransformWithLabels((~stationary_hair).astype(np.uint8),cv2.DIST_L2,5,labelType=cv2.DIST_LABEL_PIXEL)
    hair_colors=target[:,:,:3][stationary_hair]
    seam_rgb=hair_colors[np.clip(nearest-1,0,len(hair_colors)-1)]
    seam=front&(distance<=4)&~(cv2.dilate(feature.astype(np.uint8),np.ones((5,5),np.uint8))>0)&(yy>=110)&(yy<414)&opaque
    assets.append(('back_hair_overlap_margin_4px',image_mask(seam_rgb,seam)))
    # Remaining bright specks were internal partitions, protected out of the
    # generic overlap mask near brows/ears. They must expose hair, not skin.
    # Preserve material continuity only deep inside the ORIGINAL hair region;
    # actual forehead-facing contours remain free to reveal the skin underlay.
    original_skin=(c[:,:,0]>170)&(c[:,:,0]-c[:,:,1]>20)&(c[:,:,1]-c[:,:,2]>3)
    skin_distance=cv2.distanceTransform((~original_skin).astype(np.uint8),cv2.DIST_L2,5)
    original_hair=(c[:,:,2]>=c[:,:,1]-2)&(c[:,:,0]-c[:,:,2]<35)&(c.max(axis=2)<170)
    continuity=support&original_hair&(skin_distance>3)&(yy>=110)&(yy<414)&opaque
    assets.append(('internal_hair_continuity_texture',image_mask(target[:,:,:3],continuity)))
    psd=PSDImage.new('RGBA',(W,H),color=(0,0,0,0));under=Group.new(psd,'00_hidden_surface_underlays')
    def put(parent,name,img):
        box=img.getbbox();assert box,name
        return PixelLayer.frompil(img.crop(box),parent,name=name,top=box[1],left=box[0])
    for name,image in assets:image.save(OUT/'layers'/(name+'.png'));put(under,name,image)
    under.visible=False
    def copy(src,dst):
        for item in src:
            if item.is_group():new=Group.new(dst,item.name);copy(item,new)
            else:
                image=item_to_canvas_crop(item)
                if item.name in cleaned_parts:
                    stationary=static_parts[item.name]
                    if stationary.getbbox():
                        fixed=PixelLayer.frompil(stationary,dst,name=item.name+'_stationary_surface',top=item.top,left=item.left)
                        fixed.visible=item.visible;fixed.opacity=item.opacity
                    image=cleaned_parts[item.name]
                new=PixelLayer.frompil(image,dst,name=item.name,top=item.top,left=item.left);new.clipping=item.clipping
            new.visible=item.visible;new.opacity=item.opacity
    # Pixel image coordinates are relative to the original layer bounds.
    def item_to_canvas_crop(item):return item.topil().convert('RGBA')
    copy(source,psd)
    path=OUT/'suiyue_hair_seams.psd';psd.save(path)
    psd=PSDImage.open(path)
    def composite():return psd.composite(force=True,ignore_preview=True,color=0,alpha=0,apply_icc=False).convert('RGBA')
    rest=composite();rest.save(OUT/'neutral_working.png')
    a=np.array(rest);visible=target[:,:,3]>0;delta=np.abs(a.astype(int)-target.astype(int))
    alpha_delta=np.abs(a[:,:,3].astype(int)-target[:,:,3].astype(int))
    assert delta[visible].max()<=4 and alpha_delta.max()<=1,'Neutral changed beyond matting-rounding tolerance'
    movers=[x for x in psd.descendants() if not x.is_group() and x.name in front_names]
    assert len(movers)==4,len(movers)
    offsets=[x.offset for x in movers];checks=[]
    for dx in [-2,2]:
        for layer,xy in zip(movers,offsets):layer.offset=(xy[0]+dx,xy[1])
        psd[0].visible=True
        with_under=composite();with_under.save(OUT/('hair_shift_%s.png'%dx))
        psd[0].visible=False;without=composite();psd[0].visible=True
        diff=np.abs(np.array(with_under).astype(int)-np.array(without).astype(int))
        exposed=int(np.any(diff>1,axis=2).sum())
        roi=face_mask|neck_mask
        min_alpha=int(np.array(with_under)[:,:,3][roi].min())
        # psd-tools float compositing can round a covered 255 to 254; retain
        # the measured value rather than describing it as exactly opaque.
        assert min_alpha>=254,'Hole in underpaint region'
        # Compare the same deep-hair material mismatch criterion used to locate
        # the previous bright fragments. It is a regression probe, not a rig test.
        domain=original_hair&(skin_distance>4)&(xx>=400)&(xx<620)&(yy>=135)&(yy<295)
        def bright_internal(image):
            rgb=np.array(image)[:,:,:3].astype(float)
            return domain&(rgb.mean(axis=2)-c.mean(axis=2)>35)
        current=bright_internal(with_under)
        previous=Image.open(ROOT/'tools/vn/suiyue_head_underlay_20261106'/('hair_shift_%s.png'%dx))
        baseline=bright_internal(previous)
        checks.append(dict(dx=dx,changedByUnderlayPixels=exposed,minimumUnderpaintAlpha=min_alpha,
                           brightInternalPixelsBefore=int(baseline.sum()),brightInternalPixelsAfter=int(current.sum())))
    for layer,xy in zip(movers,offsets):layer.offset=xy
    psd[0].visible=False
    receipt=dict(scope='Head underpaint/raster hair-layer offset check, not head-turn rigging',
        registration=dict(correlation=score,affine=warp.tolist(),crop=[360,110,664,414]),
        sourcePSD=str(BASE/'suiyue_eye_skin_calibrated.psd'),sourceArtSha256=hashlib.sha256(ART.read_bytes()).hexdigest(),
        facePaletteOffsets=face_delta,neckPaletteOffsets=neck_delta,neckReference='Current working neck, NOT unverified old-reference sample',
        faceUnderpaintPixels=int(face_mask.sum()),headSupportPixels=int(head_support.sum()),hairOverlapPixels=int(seam.sum()),internalContinuityPixels=int(continuity.sum()),clothSupportPixels=int(cloth_support.sum()),neckUnderpaintPixels=int(neck_mask.sum()),reclassifiedNonHairPixels=reclassified,
        neckStatus='No valid hidden neck pixels in this forehead donor; separate neck/shoulder repair remains pending' if not neck_mask.any() else 'Local neck repair candidate',
        neutralMaxDelta=int(delta[visible].max()),neutralAlphaMaxDelta=int(alpha_delta.max()),neutralAlphaUnchanged=bool(alpha_delta.max()==0),edgeMattePixels=matte_counts,underlayDefaultVisible=False,hairShiftChecks=checks,
        layerCount=sum(not x.is_group() for x in psd.descendants()),
        visualAcceptance=False,
        visualFindings=['Internal hair backing added; residual hairline/brow/ear-front contour fragments remain',
                        'Brightness metric also counts legitimate shifted hair shading; it is not a seam-quality score'],
        scriptSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
        outputPSDSha256=hashlib.sha256(path.read_bytes()).hexdigest(),
        pending=['Full back-head/hair reconstruction for actual head rotation','Hair/ear/neck contour and seam review under deformation',
                 'Arm/wrist/garment occlusion completion','No-tablet arm pose alignment','Actual Cubism import, mesh and parameter keys','cmo3/moc3 and VN runtime acceptance'])
    (OUT/'verification.json').write_text(json.dumps(receipt,ensure_ascii=False,indent=2),encoding='utf-8')
    sheet=Image.new('RGB',(1200,500),(220,220,220));draw=ImageDraw.Draw(sheet)
    for i,(name,file) in enumerate([('Rest','neutral_working.png'),('Hair shift -2px','hair_shift_-2.png'),('Hair shift +2px','hair_shift_2.png')]):
        image=Image.open(OUT/file).crop((390,135,635,390)).resize((390,406));sheet.paste(image,(i*400,32),image);draw.text((i*400+10,10),name,fill='black')
    sheet.save(OUT/'head_underlay_review.png')
    # Review callouts remain separate from production/source pixels.
    detail=rest.crop((390,135,635,390)).resize((735,765))
    detail=detail.convert('RGB');pen=ImageDraw.Draw(detail)
    for label,box in [('A',(440,181,494,228)),('B',(579,237,619,288)),('C',(396,194,432,248))]:
        scaled=tuple((v-(390 if i%2==0 else 135))*3 for i,v in enumerate(box))
        pen.rectangle(scaled,outline=(250,90,70),width=2);pen.text((scaled[0],scaled[1]-12),label,fill=(250,90,70))
    detail.save(OUT/'contour_rework_map.png')
    print('SUIYUE_HEAD_UNDERLAY_SAVED',receipt)


if __name__=='__main__':main()
