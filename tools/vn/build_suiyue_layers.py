"""User-authorized pixel-mask separation and PSD assembly, not Cubism rigging.

Visible pixels are partitioned from the immutable approved master. ImageGen
plates repair only occluded regions and are retained with registration receipts.
Coordinates use viewer space; R/L layer IDs use the character's anatomy.
"""
from pathlib import Path
import hashlib
import json
import sys

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'tools/vn/suiyue_layers_20261106'
SOURCE = ROOT / 'tools/vn/character_art_20261106/suiyue_with_tablet_final.png'
sys.path.insert(0, str(ROOT / 'Library/VNPSDVerify'))
from psd_tools import PSDImage
from psd_tools.api.layers import Group, PixelLayer

W, H = 992, 1586
OWNER = np.zeros((H, W), np.int16)
REGIONS = [('hair_back', 'hair', np.ones((H, W), np.uint8))]


def polygon(points):
    mask = np.zeros((H, W), np.uint8)
    cv2.fillPoly(mask, [np.array(points, np.int32)], 255)
    return mask


def ellipse(cx, cy, rx, ry, angle=0):
    mask = np.zeros((H, W), np.uint8)
    cv2.ellipse(mask, (cx, cy), (rx, ry), angle, 0, 360, 255, -1)
    return mask


def add(name, group, points=None, mask=None):
    mask = polygon(points) if mask is None else mask
    OWNER[mask > 0] = len(REGIONS)
    REGIONS.append((name, group, mask))


def define_regions():
    # Back to front. All masks are retained for manual contour review.
    add('backpack_body', 'backpack', [(264,512),(326,516),(372,595),(355,768),(322,811),(280,782),(254,717),(251,611)])
    add('leg_R_skin', 'legs', [(370,937),(490,945),(494,1011),(463,1127),(455,1205),(435,1251),(359,1249),(354,1207),(373,1121),(373,1036)])
    add('leg_L_skin', 'legs', [(494,945),(631,942),(628,1060),(608,1141),(609,1207),(588,1255),(516,1249),(502,1197),(508,1106),(489,1034)])
    add('sock_R', 'legs', [(353,1208),(380,1229),(424,1236),(454,1215),(451,1305),(445,1403),(455,1497),(423,1513),(367,1487),(359,1401),(353,1300)])
    add('sock_L', 'legs', [(509,1214),(551,1236),(588,1227),(608,1206),(607,1302),(595,1405),(601,1498),(568,1520),(517,1490),(510,1409),(516,1310)])
    add('shoe_R', 'shoes', [(361,1454),(405,1454),(436,1468),(456,1501),(472,1534),(469,1567),(435,1580),(387,1573),(357,1548),(352,1509)])
    add('shoe_L', 'shoes', [(516,1454),(553,1452),(581,1466),(601,1504),(607,1543),(601,1572),(564,1582),(524,1577),(505,1559),(504,1516)])
    add('skirt', 'body', [(351,748),(617,740),(686,794),(734,920),(721,947),(674,963),(612,971),(547,983),(480,977),(411,968),(347,949),(296,925),(307,878)])
    add('jacket_torso', 'body', [(405,313),(459,319),(506,337),(565,327),(607,319),(641,363),(670,411),(684,479),(700,557),(705,668),(703,750),(713,829),(729,916),(666,938),(631,918),(608,857),(585,815),(531,787),(484,787),(442,821),(404,871),(390,942),(319,930),(299,906),(319,823),(322,755),(303,689),(289,604),(307,511),(317,425),(349,361)])
    add('neck', 'face', [(486,296),(565,292),(568,344),(588,377),(571,405),(538,415),(495,393),(470,365),(483,337)])
    add('sailor_blouse', 'body', [(487,383),(534,383),(564,390),(583,424),(597,479),(607,608),(587,768),(483,776),(455,647),(456,503),(462,432)])
    add('sailor_collar', 'body', [(478,389),(505,400),(534,391),(562,395),(580,419),(590,446),(554,479),(518,472),(473,443),(462,415)])
    add('neckerchief', 'body', [(480,439),(521,462),(545,467),(585,446),(587,474),(563,485),(552,498),(560,537),(550,584),(531,565),(524,507),(511,494),(484,479)])
    add('arm_R_upper', 'arms', [(356,372),(386,396),(402,451),(392,512),(367,570),(337,622),(306,656),(279,650),(269,603),(281,533),(298,453),(321,402)])
    add('arm_R_forearm', 'arms', [(306,596),(334,618),(365,659),(411,687),(448,694),(449,733),(426,756),(389,754),(352,734),(316,704),(286,670),(279,638)])
    add('arm_L_upper', 'arms', [(642,381),(676,409),(696,463),(721,516),(734,568),(721,609),(686,613),(666,573),(648,531),(627,467),(623,419)])
    add('arm_L_forearm', 'arms', [(681,575),(721,572),(735,602),(736,639),(716,670),(684,683),(652,675),(633,649),(635,613),(654,584)])
    add('backpack_strap_R', 'backpack', [(375,375),(394,373),(423,390),(447,428),(456,466),(461,522),(447,563),(430,555),(431,504),(422,452),(402,410),(382,394)])
    add('backpack_strap_L', 'backpack', [(601,375),(617,365),(638,390),(653,430),(662,479),(669,519),(661,543),(648,535),(642,489),(634,443),(619,403)])
    add('sleeve_flag', 'accessories', [(318,490),(377,484),(384,540),(318,554),(309,527)])
    add('leader_badge', 'accessories', [(595,445),(635,443),(640,472),(597,478)])
    add('tablet_body', 'props', [(477,503),(638,496),(649,500),(651,514),(632,718),(626,733),(612,737),(460,745),(449,740),(449,725),(465,520)])
    add('tablet_camera', 'props', [(481,510),(525,510),(527,555),(482,563),(474,553)])
    add('tablet_wordmark', 'props', [(539,578),(561,578),(560,650),(538,650)])
    add('hand_R_support', 'hands', [(433,698),(450,694),(470,698),(487,710),(493,720),(510,727),(534,724),(550,727),(553,735),(541,746),(521,755),(497,760),(474,757),(450,749),(429,739),(423,719)])
    add('hand_L_grip', 'hands', [(635,571),(650,576),(657,589),(662,607),(664,627),(657,651),(648,661),(638,658),(624,648),(610,635),(604,629),(604,622),(621,621),(603,616),(599,609),(602,603),(622,603),(604,596),(604,589),(621,584),(636,585),(626,581)])
    # Face/ears are separated before the front hair curtains.
    add('face_base_visible', 'face', [(499,137),(538,136),(572,150),(595,185),(607,224),(603,269),(586,295),(551,311),(520,323),(493,311),(459,291),(438,270),(430,238),(434,208),(456,184),(483,161)])
    add('ear_R', 'face', [(403,211),(419,209),(433,225),(433,249),(425,268),(409,262),(398,250),(393,229)])
    add('ear_L', 'face', [(598,240),(614,240),(624,252),(620,275),(609,288),(590,287),(587,276)])
    add('nose', 'face', [(515,247),(526,246),(530,262),(519,265),(514,256)])
    add('brow_R', 'brows', [(463,181),(479,183),(495,191),(508,200),(506,204),(491,196),(473,190),(460,188)])
    add('brow_L', 'brows', [(537,200),(548,194),(569,189),(589,190),(595,197),(575,197),(558,199),(540,204)])
    add('eye_R_white', 'eyes', [(436,209),(450,205),(471,208),(490,213),(501,223),(494,234),(479,243),(465,242),(450,234),(441,223)])
    add('eye_L_white', 'eyes', [(535,227),(550,219),(574,219),(593,225),(595,235),(584,248),(570,254),(553,255),(542,249),(535,239)])
    add('eye_R_iris', 'eyes', mask=ellipse(470,224,14,19, -7))
    add('eye_L_iris', 'eyes', mask=ellipse(555,237,14,19, 10))
    add('eye_R_pupil', 'eyes', mask=ellipse(469,222,4,10))
    add('eye_L_pupil', 'eyes', mask=ellipse(553,236,4,10))
    add('eye_R_glint', 'eyes', mask=ellipse(470,214,3,4))
    add('eye_L_glint', 'eyes', mask=ellipse(554,228,3,4))
    add('eye_R_lid_upper', 'eyes', [(432,207),(441,202),(452,200),(460,201),(470,203),(481,205),(491,210),(499,217),(504,224),(495,219),(485,215),(472,213),(458,210),(444,211),(437,215)])
    add('eye_L_lid_upper', 'eyes', [(532,229),(539,222),(552,217),(566,216),(581,218),(592,220),(603,221),(599,230),(590,232),(583,226),(569,224),(554,225),(543,229),(535,234)])
    add('eye_R_lid_lower', 'eyes', [(449,233),(461,240),(477,241),(490,234),(492,238),(478,245),(462,244),(450,237)])
    add('eye_L_lid_lower', 'eyes', [(541,247),(551,253),(568,253),(582,246),(585,248),(571,257),(552,258),(541,251)])
    add('mouth_closed', 'mouth', [(495,274),(506,277),(519,278),(530,276),(533,283),(522,288),(508,287),(497,282)])
    # Original silhouette is retained; these groups are material chunks, not rigged deformers.
    add('hair_front_R', 'hair', [(498,81),(505,119),(496,159),(487,196),(485,204),(474,199),(470,177),(469,158),(451,184),(431,203),(414,218),(407,262),(423,280),(415,288),(392,262),(391,235),(377,258),(361,291),(349,322),(346,359),(368,382),(357,399),(329,377),(311,344),(323,294),(352,249),(382,210),(419,164),(457,119)])
    add('hair_front_L', 'hair', [(558,95),(581,114),(598,147),(610,191),(611,224),(596,263),(584,282),(578,283),(588,263),(597,230),(591,209),(582,211),(580,181),(576,164),(573,189),(561,207),(553,214),(560,191),(567,163),(567,134)])
    add('hair_side_L', 'hair', [(613,146),(649,165),(682,224),(703,276),(714,329),(745,377),(774,431),(802,488),(799,538),(771,567),(733,552),(718,516),(699,471),(680,428),(653,396),(633,373),(617,344),(617,292),(624,249)])
    add('hair_side_R', 'hair', [(386,188),(378,236),(358,286),(340,330),(323,371),(306,410),(293,454),(271,505),(242,548),(207,576),(175,557),(170,513),(195,469),(228,429),(260,394),(285,351),(311,302),(338,252)])
    add('hair_front_long_R', 'hair', [(426,234),(436,252),(434,290),(434,328),(445,373),(459,413),(456,439),(442,417),(430,382),(420,334),(414,297)])
    add('hair_front_long_L', 'hair', [(602,271),(611,278),(610,321),(614,358),(605,391),(589,423),(581,428),(585,406),(598,374),(601,343)])
    add('ribbon', 'accessories', [(358,157),(327,167),(290,185),(289,213),(304,235),(289,254),(291,279),(318,278),(341,293),(358,270),(365,236),(382,219),(379,190)])


def rgba_from_mask(rgb, alpha, mask):
    result = np.zeros((H, W, 4), np.uint8)
    result[:, :, :3] = rgb
    result[:, :, 3] = np.where(mask, alpha, 0)
    result[result[:, :, 3] == 0, :3] = 0
    return Image.fromarray(result)


def registered_plate(name, box, stable_mask):
    reference = np.array(Image.open(SOURCE).convert('RGBA').crop(box))
    width, height = box[2]-box[0], box[3]-box[1]
    generated = np.array(Image.open(OUT/'repairs'/name).convert('RGBA').resize((width,height),Image.Resampling.LANCZOS))
    warp = np.eye(2,3,dtype=np.float32)
    template = cv2.cvtColor(reference[:,:,:3],cv2.COLOR_RGB2GRAY).astype(np.float32)/255
    moving = cv2.cvtColor(generated[:,:,:3],cv2.COLOR_RGB2GRAY).astype(np.float32)/255
    local_mask = stable_mask[box[1]:box[3],box[0]:box[2]].astype(np.uint8)*255
    score = None
    try:
        score, warp = cv2.findTransformECC(template,moving,warp,cv2.MOTION_AFFINE,
            (cv2.TERM_CRITERIA_COUNT|cv2.TERM_CRITERIA_EPS,150,1e-5),local_mask,5)
        # Reject unstable alignment rather than silently warping a design.
        if abs(float(np.linalg.det(warp[:,:2]))-1)>.15 or np.max(np.abs(warp[:,2]))>15:
            raise ValueError('registration changed geometry too much')
    except (cv2.error,ValueError):
        score = None
        warp = np.eye(2,3,dtype=np.float32)
    aligned = cv2.warpAffine(generated,warp,(width,height),flags=cv2.INTER_LINEAR|cv2.WARP_INVERSE_MAP,
                             borderMode=cv2.BORDER_REFLECT_101)
    canvas = np.zeros((H,W,4),np.uint8)
    canvas[box[1]:box[3],box[0]:box[2]] = aligned
    Image.fromarray(canvas).save(OUT/'repairs'/('aligned_'+name))
    return canvas, dict(source=name,box=box,correlation=score,affine=warp.tolist(),
                       note='Automatic context registration; hidden repair contours still require artistic review')


def write_psd(path, layers, reference):
    psd=PSDImage.new('RGBA',(W,H),color=(0,0,0,0))
    # psd-tools uses bottom-to-top list ordering. Explicit groups keep z order.
    last_group=None
    group=None
    for name, category, image, visible in layers:
        if category!=last_group:
            group=Group.new(psd,name=category)
            last_group=category
        bbox=image.getbbox()
        if bbox is None:
            continue
        layer=PixelLayer.frompil(image.crop(bbox),group,name=name,top=bbox[1],left=bbox[0])
        layer.visible=visible
    psd.save(path)
    reopened=PSDImage.open(path)
    rendered=reopened.composite(force=True,ignore_preview=True,color=0,alpha=0,apply_icc=False).convert('RGBA')
    rendered.save(OUT/(path.stem+'_reopened.png'))
    a=np.array(rendered).astype(np.int16);b=np.array(reference).astype(np.int16)
    # RGB below alpha zero is intentionally normalized, not visible artwork.
    visible=(a[:,:,3]>0)|(b[:,:,3]>0)
    delta=np.abs(a-b)
    return dict(pixelLayers=sum(not x.is_group() for x in reopened.descendants()),
                groups=sum(x.is_group() for x in reopened.descendants()),
                maxDelta=int(delta[visible].max()),meanDelta=float(delta[visible].mean()),
                alphaMaxDelta=int(delta[:,:,3].max()),size=list(reopened.size))


def contact_sheet(layers):
    columns=7;tilew=200;tileh=220
    sheet=Image.new('RGB',(columns*tilew,((len(layers)+columns-1)//columns)*tileh),(218,218,218))
    draw=ImageDraw.Draw(sheet)
    for i,(name,category,layer,visible) in enumerate(layers):
        x=i%columns*tilew;y=i//columns*tileh
        box=layer.getbbox()
        if box:
            thumb=layer.crop(box);thumb.thumbnail((188,183))
            sheet.paste(thumb,(x+(tilew-thumb.width)//2,y+12),thumb)
        draw.text((x+5,y+195),name,fill=(15,15,15))
    sheet.save(OUT/'layer_contact_sheet.jpg',quality=93)


def main():
    OUT.mkdir(exist_ok=True)
    (OUT/'layers').mkdir(exist_ok=True)
    (OUT/'masks').mkdir(exist_ok=True)
    source=Image.open(SOURCE).convert('RGBA')
    assert source.size==(W,H)
    raw=np.array(source);rgb=raw[:,:,:3];alpha=raw[:,:,3]
    define_regions()
    # Keep imperfect fringe/contour ownership explicit instead of calling all
    # otherwise-unassigned pixels "back hair". This is a first lasso pass.
    indices={r[0]:i for i,r in enumerate(REGIONS)}
    yy,xx=np.indices((H,W))
    residue=(OWNER==0)&(yy>=700)&(alpha>0)
    add('edge_residue_review','review',mask=(residue*255).astype(np.uint8))
    # Face-base chunks must not silently contain pieces of dark bangs.
    face_index=indices['face_base_visible']
    dark_bang=(OWNER==face_index)&(yy<276)&(rgb[:,:,0]<125)
    OWNER[dark_bang]=indices['hair_front_R']
    # Keep the visible brow contours in their own layers even where a broad
    # fringe lasso crosses them. Final lash/bang occlusion masks remain a review.
    brow_r=polygon([(457,180),(480,182),(499,193),(511,201),(508,207),(496,203),(477,191),(457,189)])>0
    brow_l=polygon([(536,198),(552,191),(575,188),(594,191),(595,198),(575,198),(552,201),(537,205)])>0
    OWNER[brow_r]=indices['brow_R']
    OWNER[brow_l]=indices['brow_L']
    layers=[];records=[]
    for index,(name,category,mask) in enumerate(REGIONS):
        owned=(OWNER==index)&(alpha>0)
        if not owned.any():continue
        image=rgba_from_mask(rgb,alpha,owned)
        image.save(OUT/'layers'/(name+'.png'))
        Image.fromarray((owned*255).astype(np.uint8)).save(OUT/'masks'/(name+'.png'))
        layers.append((name,category,image,True))
        records.append(dict(name=name,category=category,bounds=image.getbbox(),pixels=int(owned.sum()),
                            source='exact original pixel partition',canvas=[W,H]))
    combined=Image.new('RGBA',(W,H))
    for _,_,image,_ in layers:combined=Image.alpha_composite(combined,image)
    combined.save(OUT/'recomposed_exact.png')
    norm=np.array(source);norm[norm[:,:,3]==0,:3]=0
    assert np.array_equal(np.array(combined),norm),'Original pixels failed exact reconstruction'
    # Hidden underpainting. It never replaces visible approved pixels by default.
    repairs=[];registrations=[]
    stable=np.ones((H,W),bool)
    stable[170:295,430:610]=False
    face,receipt=registered_plate('face_base_clean_v2.png',(360,110,664,414),stable)
    registrations.append(receipt)
    feature_names=['brow_R','brow_L','eye_R_white','eye_L_white','eye_R_iris','eye_L_iris',
        'eye_R_pupil','eye_L_pupil','eye_R_glint','eye_L_glint','eye_R_lid_upper','eye_L_lid_upper',
        'eye_R_lid_lower','eye_L_lid_lower','mouth_closed']
    features=np.isin(OWNER,[i for i,r in enumerate(REGIONS) if r[0] in feature_names])
    face_mask=cv2.dilate(features.astype(np.uint8),np.ones((7,7),np.uint8))>0
    face_mask &= face[:,:,3]>0
    # Match donor colour using only genuine surrounding skin. Poisson cloning
    # is deliberately avoided: dark lashes at the boundary contaminated skin.
    ring=(cv2.dilate(face_mask.astype(np.uint8),np.ones((15,15),np.uint8))>0)&~face_mask
    skin=(rgb[:,:,0]>170)&(rgb[:,:,0].astype(int)-rgb[:,:,1].astype(int)>20)&(rgb[:,:,1].astype(int)-rgb[:,:,2].astype(int)>5)
    donor_skin=(face[:,:,0]>170)&(face[:,:,0].astype(int)-face[:,:,1].astype(int)>20)
    samples=ring&skin&donor_skin
    offset=np.median(rgb[samples].astype(float)-face[:,:,:3][samples].astype(float),axis=0) if samples.any() else np.zeros(3)
    repaired_skin=np.clip(face[:,:,:3].astype(float)+offset,0,255).astype(np.uint8)
    repairs.append(('repair_face_under_features','repairs',rgba_from_mask(repaired_skin,np.full((H,W),255,np.uint8),face_mask),False))
    sclera,receipt=registered_plate('eye_sclera_generated.png',(360,110,664,414),stable)
    registrations.append(receipt)
    for side,cx,cy,rx,ry in [('R',470,224,15,19),('L',555,237,15,19)]:
        mask=ellipse(cx,cy,rx,ry)>0
        repairs.append(('repair_sclera_'+side,'repairs',rgba_from_mask(sclera[:,:,:3],np.full((H,W),255,np.uint8),mask),False))
    stable=np.ones((H,W),bool);stable[565:765,420:675]=False
    device,receipt=registered_plate('tablet_clean_generated.png',(400,460,720,780),stable)
    registrations.append(receipt)
    tablet_shape=polygon([(477,499),(627,493),(643,499),(646,513),(631,708),(624,731),(606,735),(461,744),(449,737),(452,711),(465,518)])>0
    repairs.append(('repair_tablet_under_fingers','repairs',rgba_from_mask(device[:,:,:3],np.full((H,W),255,np.uint8),tablet_shape),False))
    stable=np.ones((H,W),bool);stable[495:785,410:680]=False
    torso,receipt=registered_plate('torso_clean_generated.png',(300,330,750,980),stable)
    registrations.append(receipt)
    center=polygon([(462,483),(611,474),(662,576),(676,666),(640,765),(594,797),(452,792),(421,738),(416,697),(449,623)])>0
    repairs.append(('repair_uniform_under_tablet','repairs',rgba_from_mask(torso[:,:,:3],np.full((H,W),255,np.uint8),center),False))
    for name,category,image,visible in repairs:image.save(OUT/'layers'/(name+'.png'))
    all_layers=repairs+layers
    receipt=write_psd(OUT/'suiyue_tablet_separation.psd',all_layers,combined)
    # Use exact composition as an invariant; repair plates are off by default.
    assert receipt['alphaMaxDelta']<=1 and receipt['meanDelta']<.05,receipt
    contact_sheet(all_layers)
    (OUT/'manifest.json').write_text(json.dumps(dict(source=str(SOURCE),sourceSha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        canvas=[W,H],sideConvention='R/L are character anatomical sides; coordinates are viewer space',
        exactOriginalRecomposition=True,psdRoundTrip=receipt,layers=records,
        repairLayers=[dict(name=x[0],visible=False,bounds=x[2].getbbox()) for x in repairs],
        registration=registrations,status='first separation pass, NOT rig-ready; repairs hidden pending seam/contour review',
        pending=['Contour refinement at fringe/eyelid/arm overlaps','Mouth cavity/lip topology and open-mouth artwork',
                 'Complete forehead under bangs and head rotation occlusion','Complete hidden hand/arm geometry and no-tablet pose',
                 'Rig-specific parameter/deformer/mesh/texture-atlas work','Cubism PSD import and motion acceptance']),ensure_ascii=False,indent=2),encoding='utf-8')
    print('SUIYUE_SEPARATION_SAVED',len(layers),'original layers',len(repairs),'hidden repair layers',receipt)


if __name__=='__main__':main()
