"""Original Baiyue artwork; reference: Amerex B402 catalog and pressure-gauge tech tip."""
from pathlib import Path
import json
import math
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[2]
FONT=ROOT/'Assets/Resources/Fonts/SarasaMonoSC-Regular.ttf'
P=json.loads((ROOT/'Assets/Resources/Cab/safety_equipment_profile.json').read_text(encoding='utf-8'))
OUT=ROOT/'Assets/Resources/Cab'
def font(n): return ImageFont.truetype(str(FONT),n)
im=Image.new('RGB',(1536,2700),'#eee8cf');d=ImageDraw.Draw(im)
d.rectangle((35,35,1500,2665),outline='#30372f',width=7)
d.rectangle((55,55,1480,290),fill='#8b1914')
d.text((90,75),'ABC 干粉灭火器',font=font(90),fill='#fff4db')
d.text((90,200),'ABC DRY CHEMICAL EXTINGUISHER',font=font(48),fill='#fff4db')
d.polygon([(1320,225),(1370,100),(1420,225),(1370,265)],outline='#fff4db',width=7)
d.line([(1340,220),(1370,150),(1400,220)],fill='#fff4db',width=6)
y=325
def line(text,size=44,color='#202823'):
    global y
    assert d.textbbox((0,0),text,font=font(size))[2]<1350,text
    d.text((90,y),text,font=font(size),fill=color);y+=size+12
def pair(cn,en): line(cn,48);line(en,39)
def section(cn,en):
    global y
    y+=14;d.line((90,y,1440,y),fill='#8b1914',width=5);y+=15;line(cn+' / '+en,50)
pair(P['series']+'  '+P['model'],P['seriesEnglish']+'  '+P['model'])
pair('药剂：'+P['agent'],'AGENT: '+P['agentEnglish'])
pair('充装质量：'+P['charge'],'AGENT CHARGE: '+P['charge'].replace('（',' (').replace('）',')'))
pair('额定工作压力：'+P['pressure'],'NOMINAL PRESSURE: '+P['pressure'])
pair('工作温度范围：'+P['temperature'],'TEMPERATURE RANGE: -54 TO +49 C')
section('适用范围','FIRE CLASSES')
pair('A  木材、纸张、织物等固体','A  ORDINARY COMBUSTIBLES: WOOD, PAPER, TEXTILES')
pair('B  可燃液体及气体','B  FLAMMABLE LIQUIDS AND GASES')
pair('C  带电电气设备','C  ENERGIZED ELECTRICAL EQUIPMENT')
pair('灭火级别：'+P['rating'],'FIRE RATING: '+P['rating'])
pair('不用于可燃金属及烹饪油脂火灾','NOT FOR COMBUSTIBLE METALS OR COOKING-OIL FIRES')
section('操作要点','OPERATION')
pair('1  保持直立，拔出保险销','1  KEEP UPRIGHT. PULL THE SAFETY PIN.')
pair('2  喷嘴对准火焰根部','2  AIM THE NOZZLE AT THE BASE OF THE FIRE.')
pair('3  压下压柄，左右扫射','3  SQUEEZE THE LEVER. SWEEP SIDE TO SIDE.')
section('检查与维护','MAINTENANCE')
pair('检查压力绿色区、保险销、封签和软管','CHECK GREEN ZONE, PIN, SEAL AND HOSE.')
pair('使用后交专业人员检查和充装','AFTER USE: PROFESSIONAL INSPECTION AND RECHARGE.')
section('制造单位','MANUFACTURER')
pair(P['manufacturer'],P['manufacturerEnglish'])
pair('编号见挂架，维护记录见随车检修册','SEE BRACKET FOR ID; SEE ONBOARD SERVICE LOG.')
assert y<2660,y
im.save(OUT/'Extinguisher_Baiyue_Bilingual_V3.png')

g=Image.new('RGB',(1024,1024),'#e7e5d5');a=ImageDraw.Draw(g)
for start,end,col in [(135,250,'#a42a22'),(250,290,'#32834c'),(290,405,'#a42a22')]:
    a.arc((100,100,924,924),start,end,fill=col,width=100)
for i in range(19):
    t=math.radians(135+i*270/18)
    a.line([(512+math.cos(t)*435,512+math.sin(t)*435),(512+math.cos(t)*465,512+math.sin(t)*465)],fill='#30352e',width=8)
def gauge_text(x,y,s,size): a.text((x,y),s,font=font(size),fill='#222c26',anchor='mm')
gauge_text(512,68,'正常 / CHARGED',38)
gauge_text(265,620,'欠压',45);gauge_text(265,670,'LOW',36)
gauge_text(765,620,'过压',45);gauge_text(765,670,'HIGH',36)
gauge_text(512,760,'压力 / PRESSURE',40)
gauge_text(512,820,'1.34 MPa',45);gauge_text(512,875,'195 psig',36)
g.save(OUT/'Extinguisher_Baiyue_Gauge_V3.png')

book=Image.new('RGB',(768,1152),'#29463e');b=ImageDraw.Draw(book)
b.rectangle((25,25,742,1125),outline='#d8d1ad',width=6)
for yy,txt,sz in [(85,'白岳消防器材厂',53),(165,'BAIYUE FIRE EQUIPMENT',36),(345,'随车消防器材',68),(435,'使用与维护手册',68),(585,'ONBOARD FIRE EQUIPMENT',38),(645,'OPERATION & MAINTENANCE',38),(845,'守安系列 / SHOUAN SERIES',37),(920,'BYF-23R',58)]:
    b.text((384,yy),txt,font=font(sz),fill='#e8dfbd',anchor='mt')
book.save(OUT/'Baiyue_Fire_Manual_Cover.png')
print('Bilingual label, dial and manual generated. Label end:',y)
