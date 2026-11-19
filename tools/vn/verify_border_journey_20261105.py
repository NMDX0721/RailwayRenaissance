"""Verify coordinates, reachability and the reviewed journey continuity constraints."""
import importlib.util
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'tools/vn/prologue_border_20261105'
SCRIPTS=ROOT/'Assets/Resources/Scripts'
spec=importlib.util.spec_from_file_location('flow',ROOT/'tools/vn/audit_prologue_flow.py')
flow=importlib.util.module_from_spec(spec)
spec.loader.exec_module(flow)

def without_text(value):
    if isinstance(value,dict):return {k:without_text(v) for k,v in value.items() if k!='text'}
    if isinstance(value,list):return [without_text(v) for v in value]
    return value

scripts={};reports=[]
for p in sorted(SCRIPTS.glob('prologue_*.json')):
    before=json.loads((OUT/'before'/p.name).read_text(encoding='utf-8-sig'))
    after=json.loads(p.read_text(encoding='utf-8-sig'))
    assert without_text(before)==without_text(after),p.name
    scripts[p.stem[9:11]]=after
    reports.append(flow.audit(p))

def entry(ch,s,d):return scripts[ch]['scenes'][s]['d'][d]
def text(ch,s,d):return entry(ch,s,d)['text']

all_text='\n'.join(e.get('text','') for j in scripts.values() for s in j['scenes'] for e in s['d'])
for obsolete in ['砂能','Hello Kitty','工签名','沙子飞猪号','九八年','六八年','四天半','超音速','徐嘉颖',
                 '七级机车钳工','八级工','特级技师','高级技师','报国务院','查无此事','他们知道我们要来']:
    if obsolete=='六八年':
        assert not any(e.get('text','').startswith('六八年') for s in scripts['06']['scenes'] for e in s['d'])
    else:
        assert obsolete not in all_text,obsolete
assert '刻' in text('02',0,32) and '刻' in text('02',0,34) and '刻' in text('02',0,35)
assert '不代替出境许可' in text('02',2,8)
assert '请先看一眼' in text('03',2,0) and '不是出境放行单' in text('03',2,4)
assert '朝鲜一侧' in text('03',0,1) and '朝鲜一侧' in text('03',1,0)
assert '出境放行后' in text('03',2,128) and '越过江面' in text('03',2,128)
assert '不是科研样本' in text('03',2,43) and '没批过这种清单' in text('03',3,30)
assert '没有撤' in text('03',2,87) and '记录别改' in text('03',3,24)
assert '平板' in text('03',2,33) and '免提' in text('03',2,40)
assert '午后' in text('03',3,0) and '午后' in text('03',3,55)
assert '现钞' in text('03',3,53)
assert '机车检修技师' in text('06',0,36) and '机车检修技师' in text('07',1,31)
assert '二〇六八年' in text('06',3,38)
assert '明天下午' in text('03',0,20) and 'Day 2 下午'==text('03',4,25)
assert '一千三百' in text('03',4,33) and '一千三百' in text('03',4,51)
assert '两千公里' in text('03',5,47) and '五百' in text('03',5,47)
assert '第四天' in text('02',6,25) and text('03',5,55)=='Day 4 上午'
assert '旧铁路图' in text('03',4,55) and '落地' in text('03',4,55)

journey=dict(scope='Authored continuity baseline, not a flight simulation or a measured route',
    planned_total_km=2500,maximum_speed_kmh=80,minimum_airborne_hours=2500/80,
    rounded_narrative_airborne_hours=32,planned_maximum_refuel_interval_km=1000,
    vehicle_theoretical_range_km=1200,
    checkpoints=[dict(day=0,place='平壤出发',cumulative_km=0),
                 dict(day=1,place='朝鲜一侧江岸补给站／边检',cumulative_km=400),
                 dict(day=2,place='河北补给站',cumulative_km=1200),
                 dict(day=3,place='河南补给站',cumulative_km=2000),
                 dict(day=4,place='雾峰村，下午抵达',cumulative_km=2500)],
    delays='Night ground stops, border checks, refuelling and terrain/navigation holds; exact durations unspecified')
points=journey['checkpoints']
assert sum(b['cumulative_km']-a['cumulative_km'] for a,b in zip(points,points[1:]))==2500
assert all(0<b['cumulative_km']-a['cumulative_km']<=1000 for a,b in zip(points,points[1:]))
(OUT/'journey_baseline.json').write_text(json.dumps(journey,ensure_ascii=False,indent=2),encoding='utf-8')
(OUT/'flow_audit.json').write_text(json.dumps(dict(scope='Static validation only, not Unity playback',
    structure='All non-text JSON fields equal before snapshot',focused_continuity_checks='passed',scripts=reports),
    ensure_ascii=False,indent=2),encoding='utf-8')
print('BORDER_JOURNEY_STATIC_OK scripts='+str(len(reports))+' choices='+str(sum(r['choices'] for r in reports))
      +' options='+str(sum(r['options'] for r in reports))+' minimum_airborne_hours=31.25')
