"""Original layout study from source dimensions; not a Unity render or measured floor plan."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONT = ROOT / 'Assets/Resources/Fonts/SarasaMonoSC-Regular.ttf'
out = ROOT / 'tools/cab_reference/interior_review/cab_layout_balance_proposal.png'
im = Image.new('RGB', (1600, 1240), '#f2f0e8')
d = ImageDraw.Draw(im)

def text(x, y, value, size=26, color='#263a35', anchor='mm'):
    d.text((x, y), value, font=ImageFont.truetype(str(FONT), size), fill=color, anchor=anchor)

def sx(x):
    return 150 + (1.45 - x) / 2.9 * 1300

text(800, 53, 'NF-5K 驾驶室：左右工作区布局研究', 42)
text(800, 100, '面向车头：图左为司机侧。源码尺寸示意，不是实车图纸或改动后的模型。', 23, '#66716a')

def card(y, proposed):
    d.rounded_rectangle((65, y, 1535, y+448), radius=18, fill='#ffffff', outline='#ced4cb', width=2)
    text(104, y+40, 'B  建议布局（尚未应用）' if proposed else 'A  现有布局（依据源码与模型记录）', 31, anchor='lm')
    for side, label in [(1, '司机侧前窗'), (-1, '辅助侧前窗')]:
        center=side*.555
        d.rounded_rectangle((sx(center+.5225), y+82, sx(center-.5225), y+132), radius=7, fill='#e5eceb', outline='#9aaba7', width=2)
        text(sx(center), y+107, label, 23)
    for yy in range(y+75, y+375, 15):
        d.line((sx(0), yy, sx(0), yy+7), fill='#c7b88e', width=2)
    main_left, main_right=sx(.55+.775), sx(.55-.775)
    aux_center, aux_width=(-.80,.95) if proposed else (-.86,.65)
    aux_left, aux_right=sx(aux_center+aux_width/2), sx(aux_center-aux_width/2)
    if proposed:
        d.rectangle((main_right, y+164, aux_left, y+191), fill='#becac1')
    d.rounded_rectangle((main_left, y+155, main_right, y+254), radius=9, fill='#3c5b4e')
    d.rounded_rectangle((aux_left, y+155, aux_right, y+254), radius=9, fill='#60777e')
    text((main_left+main_right)/2, y+190, '单套主操纵台', 30, '#f3efdc')
    text((main_left+main_right)/2, y+232, '宽约 1.55 m · 保留现有控制链', 22, '#d9e3d5')
    text((aux_left+aux_right)/2, y+188, '完整辅助工作台' if proposed else '辅助小搁板', 26, '#f3efdc')
    text((aux_left+aux_right)/2, y+229, '通信 / 文件 / 检查' if proposed else '宽约 0.65 m', 21, '#e1e6df')
    for x, label in [(.65,'司机席'),(-.65 if proposed else -.85,'副司机席')]:
        cx=sx(x)
        d.line((cx,y+267,cx,y+289), fill='#879b8e', width=2)
        d.rounded_rectangle((cx-80,y+294,cx+80,y+349), radius=12, fill='#e5e8dc', outline='#94a392', width=2)
        text(cx,y+321,label,24)
    text(800,y+392,'辅助台约 0.95 m；副司机席向内约 0.20 m；前沿衔接，后方留通道。' if proposed else '主台与座位基本对应；另一侧形体薄弱，副司机席比司机席额外靠外。',25)

card(141,False)
card(621,True)
text(800,1124,'设计方向：功能分区清楚、整体线型连贯；两侧工作区用途不同。',27)
text(800,1170,'具体台面深度、膝部空间、门扇扫掠与触达范围，需在下一轮三维试装复核。',23,'#66716a')
im.save(out)
print(out)
