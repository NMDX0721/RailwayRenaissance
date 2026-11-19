"""Draw grid-aligned outdoor fixtures for the Wufeng station map."""
from pathlib import Path
from random import Random
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[2]/"Assets/Resources/Art/Explore/PropsHD"
ROOT.mkdir(parents=True,exist_ok=True)
C={"ink":"#303A3F","blue":"#254663","blue2":"#416985","paper":"#E8D8C0",
   "paper2":"#F5E7CB","brass":"#C9A251","red":"#AA5144","green":"#639063",
   "green_dark":"#38523F","green_light":"#9CAB68","wood":"#765036","wood2":"#A67449",
   "wood_light":"#CB9863","stone":"#857A6D","stone_light":"#B8AA92","shadow":"#4B4038"}
def new(w,h):
    im=Image.new("RGBA",(w,h),(0,0,0,0)); return im,ImageDraw.Draw(im)
def box(d,xy,c): d.rectangle(xy,fill=C[c])
def line(d,pts,c,width=1): d.line(pts,fill=C[c],width=width)

def station_sign():
    im,d=new(256,256)
    for x in (38,207):
        box(d,(x,70,x+12,250),"wood")
        box(d,(x+3,71,x+5,240),"wood_light")
    box(d,(16,26,240,162),"ink")
    box(d,(23,32,233,153),"paper")
    box(d,(23,108,233,153),"blue")
    box(d,(30,114,226,120),"paper2")
    box(d,(28,40,228,45),"brass")
    d.ellipse((103,54,152,101),fill=C["blue"])
    d.ellipse((111,60,144,93),fill=C["paper2"])
    d.polygon([(111,77),(127,61),(143,77),(127,93)],fill=C["red"])
    for y in (127,137): line(d,[(35,y),(219,y)],"paper",2)
    return im

def signal(color):
    im,d=new(128,256)
    box(d,(55,74,73,246),"ink")
    box(d,(59,76,63,238),"stone_light")
    box(d,(35,212,96,232),"stone")
    box(d,(21,23,108,112),"ink")
    box(d,(27,29,102,106),"blue")
    d.ellipse((43,39,86,82),fill=C["shadow"])
    d.ellipse((48,44,81,77),fill=C[color])
    d.ellipse((53,47,63,57),fill=C["paper2"] if color=="green" else C["brass"])
    box(d,(32,87,97,94),"wood2")
    box(d,(39,98,90,102),"brass")
    return im

def bench():
    im,d=new(256,128)
    for x in (32,205):
        box(d,(x,72,x+12,119),"ink")
        box(d,(x+3,75,x+7,111),"wood2")
    for y in (22,45,76):
        box(d,(17,y,239,y+17),"wood")
        box(d,(20,y+2,236,y+5),"wood_light")
        for x in (35,119,211): box(d,(x,y+10,x+3,y+12),"brass")
    box(d,(15,98,241,104),"ink")
    return im

def notice():
    im,d=new(256,128)
    box(d,(12,5,244,112),"wood")
    box(d,(17,10,239,106),"wood_light")
    box(d,(24,17,232,100),"paper")
    for i,(x,y) in enumerate(((33,26),(106,25),(172,27),(44,63),(121,65),(184,66))):
        w=43 if i%2 else 49
        box(d,(x,y,x+w,y+26),"paper2" if i%2 else "stone_light")
        box(d,(x+4,y+6,x+w-5,y+7),"blue")
        box(d,(x+4,y+13,x+w-9,y+14),"stone")
        box(d,(x+4,y+20,x+w-18,y+21),"stone")
        d.ellipse((x+w-7,y+2,x+w-3,y+6),fill=C["red"])
    for x in (29,216): box(d,(x,111,x+11,126),"wood")
    return im

def crossing():
    im,d=new(256,128)
    box(d,(28,16,51,126),"ink")
    box(d,(31,19,46,122),"stone")
    d.ellipse((16,10,62,55),fill=C["ink"])
    d.ellipse((24,18,54,47),fill=C["brass"])
    box(d,(48,29,248,50),"paper2")
    for x in (65,100,135,170,205):
        d.polygon([(x,30),(x+17,30),(x+5,50),(x-12,50)],fill=C["red"])
    box(d,(238,31,249,48),"ink")
    return im

def tea_row():
    rng=Random(56)
    im,d=new(128,128)
    d.polygon([(2,99),(63,62),(126,99),(126,127),(2,127)],fill=C["shadow"])
    for y in (26,54,82):
        for x in range(14+(y//28)%2*12,128,29):
            d.polygon([(x-14,y+14),(x,y-9),(x+14,y+14),(x,y+20)],fill=C["green_dark"])
            d.polygon([(x-11,y+8),(x,y-10),(x+11,y+8),(x,y+13)],fill=C["green"])
            for j in range(5):
                xx=x+rng.randrange(-9,10); yy=y+rng.randrange(-2,11)
                box(d,(xx,yy,xx+3,yy+2),"green_light")
    return im

def pine():
    im,d=new(256,384)
    box(d,(113,239,144,379),"wood")
    box(d,(118,242,124,376),"wood_light")
    for y,radius in ((44,55),(102,72),(157,91),(218,105)):
        d.polygon([(128,y-31),(128-radius,y+62),(128+radius,y+62)],fill=C["green_dark"])
        d.polygon([(128,y-27),(128-radius+16,y+50),(128,y+49)],fill=C["green"])
        for k in range(9):
            xx=128-radius+20+k*(radius*2-40)//8
            yy=y+20+((k*17+y)%30)
            line(d,[(xx,yy),(xx+8,yy+3)],"green_light",2)
    return im

assets={"prop_station_sign":station_sign(),"prop_signal_post_red":signal("red"),
        "prop_signal_post_green":signal("green"),"prop_platform_bench":bench(),
        "prop_notice_board":notice(),"prop_crossing_bar":crossing(),
        "prop_tea_row":tea_row(),"prop_pine_tree":pine()}
for name,im in assets.items(): im.save(ROOT/(name+".png"))
sheet=Image.new("RGB",(4*300,2*340),C["paper"]); sd=ImageDraw.Draw(sheet)
for i,(name,im) in enumerate(assets.items()):
    x,y=(i%4)*300,(i//4)*340
    scale=min(280/im.width,292/im.height)
    scaled=im.resize((int(im.width*scale),int(im.height*scale)),Image.Resampling.NEAREST)
    sheet.paste(scaled,(x+8,y+8),scaled)
    sd.text((x+8,y+310),name,fill=C["ink"])
sheet.save(ROOT/"outdoor_props_hd_contact_sheet.png")
