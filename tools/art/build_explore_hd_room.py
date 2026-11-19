"""Draw the added dispatch-room set at four source pixels per world pixel."""
from pathlib import Path
from random import Random
from PIL import Image, ImageDraw

BASE = Path(__file__).resolve().parents[2] / "Assets/Resources/Art/Explore"
P = BASE / "PropsHD"
I = BASE / "InteriorsHD"
P.mkdir(parents=True, exist_ok=True)
I.mkdir(parents=True, exist_ok=True)
C = {
    "ink":"#352B2A", "wood_dark":"#563729", "wood":"#855533",
    "wood_mid":"#A37145", "wood_light":"#C3975A", "oak":"#D0B184",
    "paper":"#E8D8C0", "paper_shadow":"#BEA98E", "cream":"#E4D5BA",
    "cream_shade":"#BDAE98", "gray":"#867F73", "gray_light":"#A49D8B",
    "blue":"#314A5D", "blue_light":"#688491", "red":"#994B3D",
    "brass":"#C39A4D", "green":"#62764B", "green_light":"#94A36A",
    "green_dark":"#40543C", "charcoal":"#3D4444", "shadow":"#433C39",
}


def image(w,h):
    im = Image.new("RGBA",(w,h),(0,0,0,0))
    return im,ImageDraw.Draw(im)


def r(d,box,col):
    d.rectangle(box,fill=C.get(col,col))


def line(d,xy,col,width=1):
    d.line(xy,fill=C.get(col,col),width=width)


def rug():
    im,d=image(384,256)
    r(d,(9,9,374,246),"ink")
    r(d,(15,15,368,240),"red")
    r(d,(24,24,359,231),"brass")
    r(d,(31,31,352,224),"blue")
    r(d,(43,43,340,212),"wood_dark")
    r(d,(49,49,334,206),"blue")
    for x in range(52,336,22):
        for y in (52,204):
            d.polygon([(x,y),(x+8,y-7),(x+16,y),(x+8,y+7)],fill=C["brass"])
    for x in range(62,329,44):
        for y in range(77,191,38):
            d.polygon([(x,y),(x+17,y-12),(x+34,y),(x+17,y+12)],fill=C["red"])
            d.polygon([(x+9,y),(x+17,y-6),(x+25,y),(x+17,y+6)],fill=C["oak"])
    for x in range(14,375,12):
        r(d,(x,2,x+2,8),"paper_shadow")
        r(d,(x,247,x+2,253),"paper_shadow")
    return im


def cabinet():
    im,d=image(128,256)
    r(d,(20,17,106,240),"shadow")
    r(d,(16,12,102,229),"gray")
    r(d,(20,14,96,22),"gray_light")
    r(d,(22,28,94,217),"cream_shade")
    for y in (32,78,124,170):
        r(d,(25,y,91,y+38),"gray")
        r(d,(29,y+3,87,y+31),"gray_light")
        r(d,(47,y+12,69,y+17),"cream")
        r(d,(52,y+17,65,y+21),"ink")
        r(d,(30,y+32,86,y+34),"shadow")
    r(d,(18,230,32,242),"ink")
    r(d,(85,230,100,242),"ink")
    return im


def shelf():
    im,d=image(256,128)
    r(d,(12,11,247,112),"wood_dark")
    r(d,(18,15,239,104),"wood")
    for y in (43,82):
        r(d,(19,y,239,y+7),"wood_light")
        r(d,(19,y+8,239,y+11),"wood_dark")
    for k,x in enumerate(range(29,228,14)):
        height=24+(k*7)%14
        col=["paper","blue","red","oak","green","paper_shadow"][k%6]
        r(d,(x,42-height,x+9,41),col)
        r(d,(x+2,42-height,x+2,39),"paper_shadow")
        r(d,(x,79-height,x+10,79),col)
        r(d,(x+3,79-height,x+3,76),"wood_dark")
    for x in (25,228):
        r(d,(x,11,x+9,105),"wood_dark")
        r(d,(x+2,13,x+5,103),"wood_light")
    return im


def plant():
    im,d=image(128,128)
    d.ellipse((17,74,112,119),fill=C["shadow"])
    r(d,(34,77,95,105),"wood")
    d.polygon([(31,76),(99,76),(89,117),(42,117)],fill=C["red"])
    r(d,(29,72,101,80),"wood_light")
    for x,y,s in [(63,14,27),(35,38,20),(89,40,20),(58,49,20),(75,27,21)]:
        d.polygon([(x,y+s),(x-s,y+8),(x-11,y),(x,y+8),(x+12,y-4),(x+s,y+12)],fill=C["green_dark"])
        d.polygon([(x,y+s-7),(x-s+5,y+9),(x-10,y+2),(x,y+11),(x+12,y-1),(x+s-7,y+12)],fill=C["green"])
        line(d,[(x,y+10),(x+6,y+s-4)],"green_light",2)
    return im


def map_board():
    im,d=image(256,128)
    r(d,(10,9,247,120),"wood_dark")
    r(d,(16,14,241,114),"wood_light")
    r(d,(24,22,233,106),"paper")
    for y in (38,68,89):
        line(d,[(29,y),(226,y)],"paper_shadow",2)
    line(d,[(35,91),(72,85),(100,63),(133,70),(171,43),(222,37)],"red",4)
    line(d,[(38,35),(78,48),(123,47),(164,82),(220,94)],"blue",3)
    for x,y in [(35,91),(100,63),(171,43),(222,37),(78,48),(164,82)]:
        d.ellipse((x-4,y-4,x+4,y+4),fill=C["brass"])
        d.ellipse((x-2,y-2,x+2,y+2),fill=C["ink"])
    for x in (36,210):
        r(d,(x,25,x+9,30),"wood")
    return im


def stove():
    im,d=image(128,128)
    r(d,(16,100,112,116),"shadow")
    r(d,(25,35,104,103),"ink")
    r(d,(29,39,99,95),"charcoal")
    r(d,(43,10,59,39),"ink")
    r(d,(46,9,56,34),"gray")
    r(d,(32,45,96,82),"wood_dark")
    r(d,(37,49,91,78),"red")
    d.polygon([(47,74),(55,59),(63,69),(69,50),(81,71),(75,78)],fill=C["brass"])
    r(d,(40,84,89,89),"gray_light")
    r(d,(23,38,105,43),"gray_light")
    r(d,(30,101,41,115),"ink")
    r(d,(86,101,99,115),"ink")
    return im


def cap(horizontal):
    im,d=image(128,48) if horizontal else image(48,128)
    w,h=im.size
    r(d,(0,0,w-1,h-1),"shadow")
    r(d,(0,0,w-1,h-9),"cream")
    if horizontal:
        r(d,(0,0,w-1,5),"paper")
        r(d,(0,h-14,w-1,h-10),"cream_shade")
        for x in (30,91):
            r(d,(x,6,x+1,h-15),"paper_shadow")
    else:
        r(d,(0,0,5,h-1),"paper")
        r(d,(w-14,0,w-10,h-1),"cream_shade")
        for y in (30,91):
            r(d,(6,y,w-15,y+1),"paper_shadow")
    return im


def wood_floor(variant):
    rng=Random(610+variant)
    im,d=image(128,128)
    r(d,(0,0,127,127),"oak")
    for row,y in enumerate(range(-24,152,32)):
        r(d,(0,y,127,y+2),"wood_dark")
        r(d,(0,y+3,127,y+4),"wood_light")
        for x in range((-31 if (row+variant)%2 else 20)-64,196,64):
            r(d,(x,y+5,x+61,y+29),"wood_mid" if (row+x//64)%3 else "wood")
            r(d,(x+2,y+6,x+59,y+7),"wood_light")
            r(d,(x+61,y+3,x+63,y+31),"wood_dark")
            for _ in range(8):
                xx=x+rng.randrange(5,56)
                yy=y+rng.randrange(9,28)
                r(d,(xx,yy,xx+rng.randrange(3,10),yy),rng.choice(["wood_light","wood_dark","oak"]))
            for nx in (x+5,x+54):
                r(d,(nx,y+10,nx+1,y+11),"ink")
    return im


def tile_floor(variant):
    rng=Random(982+variant)
    im,d=image(128,128)
    r(d,(0,0,127,127),"gray")
    for row,y in enumerate(range(-32,160,32)):
        for x in range((-32 if (row+variant)%2 else 0)-64,192,64):
            r(d,(x+2,y+2,x+61,y+29),rng.choice(["cream_shade","gray_light","oak"]))
            r(d,(x+3,y+3,x+59,y+4),"cream")
            for _ in range(10):
                xx=x+rng.randrange(5,59); yy=y+rng.randrange(6,27)
                r(d,(xx,yy,xx+1,yy),rng.choice(["gray","paper_shadow","cream"]))
    return im


def wall_part(kind):
    h=192 if kind=="front" else 128
    im,d=image(128,h)
    r(d,(0,0,127,h-1),"cream")
    r(d,(0,0,127,8),"paper")
    for row,y in enumerate(range(14,h-13,27)):
        for x in range((-26 if row%2 else 14)-64,192,64):
            r(d,(x,y,x+61,y+1),"cream_shade")
            r(d,(x+61,y,x+62,y+25),"paper_shadow")
            r(d,(x+3,y+3,x+16,y+3),"paper")
    if kind=="front":
        r(d,(0,h-28,127,h-19),"wood_light")
        r(d,(0,h-18,127,h-1),"wood_dark")
        r(d,(0,h-14,127,h-13),"wood_mid")
    else:
        r(d,(0,118,127,127),"wood")
    if kind.startswith("corner"):
        if kind.endswith("w"):
            r(d,(0,0,9,h-1),"wood_dark")
            r(d,(10,0,12,h-1),"wood_light")
        else:
            r(d,(116,0,127,h-1),"wood_dark")
            r(d,(113,0,115,h-1),"wood_light")
    return im


def door(opened):
    im,d=image(128,256)
    r(d,(14,6,113,253),"wood_dark")
    r(d,(20,12,107,248),"wood_light")
    r(d,(28,18,99,242),"wood_dark" if opened else "wood")
    if opened:
        r(d,(31,20,96,239),"ink")
        r(d,(34,22,92,236),"shadow")
        r(d,(28,20,36,239),"wood_mid")
    else:
        for y in (30,115,177):
            r(d,(34,y,94,y+55),"wood_dark")
            r(d,(39,y+5,89,y+49),"wood_mid")
            r(d,(41,y+7,44,y+47),"wood_light")
        r(d,(82,137,89,145),"brass")
        r(d,(84,139,86,142),"ink")
    r(d,(10,247,117,255),"wood_dark")
    return im


def window():
    im,d=image(128,192)
    r(d,(11,7,116,177),"wood_dark")
    r(d,(18,13,109,169),"wood_light")
    r(d,(24,18,103,164),"blue")
    for x,y in [(26,20),(67,20),(26,95),(67,95)]:
        r(d,(x,y,x+34,y+66),"blue_light")
        d.polygon([(x,y+34),(x+30,y+2),(x+34,y+2),(x+34,y+13),(x,y+47)],fill=C["paper_shadow"])
        r(d,(x+2,y+48,x+13,y+52),"cream")
    r(d,(60,18,66,165),"wood_dark")
    r(d,(24,88,103,95),"wood_dark")
    r(d,(2,171,125,185),"wood_mid")
    r(d,(5,171,122,175),"wood_light")
    return im


def dispatch_desk():
    im,d=image(256,128)
    r(d,(12,49,247,115),"wood_dark")
    r(d,(16,42,239,95),"wood")
    r(d,(17,42,238,52),"wood_light")
    r(d,(22,54,233,58),"wood_mid")
    for x in (28,209):
        r(d,(x,95,x+20,122),"wood_dark")
        r(d,(x+3,97,x+15,118),"wood_mid")
    r(d,(71,14,184,78),"paper_shadow")
    r(d,(76,18,179,74),"paper")
    for y in (28,38,48,58):
        line(d,[(82,y),(171,y)],"gray",2)
    for x in (110,141): line(d,[(x,25),(x,66)],"paper_shadow",1)
    r(d,(201,29,222,44),"blue")
    r(d,(204,31,218,35),"blue_light")
    r(d,(200,55,227,66),"wood_dark")
    r(d,(202,56,225,60),"brass")
    return im


def strategy_board():
    im,d=image(128,256)
    r(d,(15,7,114,248),"wood_dark")
    r(d,(20,12,109,238),"wood_light")
    r(d,(27,20,102,230),"paper")
    r(d,(28,25,101,44),"blue")
    for y in (57,93,129,165,201):
        r(d,(34,y,93,y+25),"paper_shadow")
        for off in (6,12,18): r(d,(40,y+off,86,y+off+1),"blue")
        r(d,(84,y+4,91,y+11),"red")
    for x,y in ((23,32),(104,32),(23,225),(104,225)):
        d.ellipse((x-2,y-2,x+2,y+2),fill=C["brass"])
    return im


def route_screen(on):
    im,d=image(256,128)
    r(d,(10,12,246,103),"ink")
    r(d,(17,19,239,95),"blue" if on else "charcoal")
    if on:
        for y in (38,61,81): line(d,[(23,y),(231,y)],"blue_light",1)
        line(d,[(28,74),(62,74),(88,47),(135,47),(161,69),(225,69)],"brass",5)
        for x,y in ((28,74),(88,47),(161,69),(225,69)):
            d.ellipse((x-5,y-5,x+5,y+5),fill=C["paper"])
            d.ellipse((x-2,y-2,x+2,y+2),fill=C["red"])
    else:
        r(d,(29,27,228,29),"gray")
        r(d,(29,83,228,85),"shadow")
    r(d,(105,103,151,114),"gray")
    r(d,(70,114,186,120),"ink")
    return im


def radio():
    im,d=image(128,128)
    r(d,(17,43,112,103),"ink")
    r(d,(21,47,106,96),"blue")
    r(d,(25,52,84,77),"gray")
    r(d,(30,56,78,72),"blue_light")
    for x in (31,38,45,52,59,66,73): line(d,[(x,57),(x,71)],"paper_shadow",1)
    d.ellipse((88,56,101,69),fill=C["brass"])
    d.ellipse((89,75,100,86),fill=C["wood_light"])
    r(d,(49,39,78,45),"wood_dark")
    line(d,[(94,44),(101,6)],"ink",3)
    return im


def clock():
    im,d=image(128,128)
    d.ellipse((16,15,112,111),fill=C["wood_dark"])
    d.ellipse((22,21,106,105),fill=C["brass"])
    d.ellipse((28,27,100,99),fill=C["paper"])
    for x,y in ((64,34),(94,63),(64,92),(35,63)):
        r(d,(x-2,y-2,x+2,y+2),"ink")
    line(d,[(64,64),(64,39)],"ink",4)
    line(d,[(64,64),(82,73)],"wood_dark",4)
    d.ellipse((60,60,68,68),fill=C["brass"])
    return im


def sofa():
    im,d=image(256,128)
    r(d,(15,55,241,109),"wood_dark")
    r(d,(19,28,237,98),"red")
    r(d,(23,34,232,53),"wood")
    for x in (31,105,178):
        r(d,(x,56,x+68,91),"red")
        r(d,(x+3,59,x+64,61),"wood_mid")
        r(d,(x+3,88,x+64,91),"wood_dark")
    for x in (17,220):
        r(d,(x,64,x+18,106),"wood_dark")
        r(d,(x+3,65,x+15,97),"wood")
    for x in (39,208): r(d,(x,109,x+10,120),"wood_dark")
    return im


def tea_table():
    im,d=image(128,128)
    r(d,(15,76,113,103),"wood_dark")
    r(d,(19,71,109,88),"wood")
    r(d,(20,69,108,74),"wood_light")
    for x in (25,93): r(d,(x,91,x+11,116),"wood_dark")
    d.ellipse((48,36,80,63),fill=C["paper_shadow"])
    d.ellipse((53,39,75,56),fill=C["blue"])
    r(d,(59,28,69,41),"paper")
    r(d,(81,52,99,68),"paper")
    r(d,(84,55,96,61),"red")
    return im


assets={"prop_rug":rug(),"prop_filing_cabinet":cabinet(),"prop_shelf_docs":shelf(),
        "prop_plant":plant(),"prop_wall_map":map_board(),"prop_stove":stove(),
        "prop_dispatch_desk":dispatch_desk(),"prop_strategy_board":strategy_board(),
        "prop_route_screen_off":route_screen(False),"prop_route_screen_on":route_screen(True),
        "prop_radio":radio(),"prop_wall_clock":clock(),"prop_sofa":sofa(),
        "prop_teatable":tea_table()}
for name,im in assets.items(): im.save(P/f"{name}.png")
for name,im in {"int_wall_cap_h":cap(True),"int_wall_cap_v":cap(False)}.items():
    im.save(I/f"{name}.png")
interiors={**{f"int_floor_wood_{v:02d}":wood_floor(v) for v in (1,2,3)},
           **{f"int_floor_tile_{v:02d}":tile_floor(v) for v in (1,2)},
           "int_wall_top":wall_part("top"),"int_wall_front":wall_part("front"),
           **{f"int_wall_corner_{c}":wall_part(f"corner_{c}") for c in ("nw","ne","sw","se")},
           "int_door_open":door(True),"int_door_closed":door(False),"int_window_01":window()}
for name,im in interiors.items():
    if name.startswith("int_floor_"):
        for yy in range(128): im.putpixel((127,yy),im.getpixel((0,yy)))
        for xx in range(128): im.putpixel((xx,127),im.getpixel((xx,0)))
    im.save(I/f"{name}.png")
sheet=Image.new("RGB",(960,5*320),C["paper"])
sd=ImageDraw.Draw(sheet)
for i,(name,im) in enumerate(assets.items()):
    x,y=(i%3)*320,(i//3)*320
    scale=min(300/im.width,270/im.height)
    size=(int(im.width*scale),int(im.height*scale))
    sheet.paste(im.resize(size,Image.Resampling.NEAREST),(x+8,y+8),im.resize(size,Image.Resampling.NEAREST))
    sd.text((x+8,y+287),name,fill=C["ink"])
sheet.save(P/"dispatch_additions_hd_contact_sheet.png")
room_sheet=Image.new("RGB",(5*270,3*300),C["paper"])
rs=ImageDraw.Draw(room_sheet)
for i,(name,im) in enumerate(sorted(interiors.items())):
    x,y=(i%5)*270,(i//5)*300
    scale=min(250/im.width,260/im.height)
    size=(int(im.width*scale),int(im.height*scale))
    scaled=im.resize(size,Image.Resampling.NEAREST)
    room_sheet.paste(scaled,(x+8,y+8),scaled)
    rs.text((x+8,y+270),name,fill=C["ink"])
room_sheet.save(I/"dispatch_core_hd_contact_sheet.png")
