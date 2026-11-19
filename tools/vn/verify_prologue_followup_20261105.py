"""Coordinate/flag/flow and focused continuity checks for the follow-up revision."""
import importlib.util
import json
from decimal import Decimal
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'tools/vn/prologue_followup_20261105'
SCRIPTS = ROOT/'Assets/Resources/Scripts'
spec=importlib.util.spec_from_file_location('flow',ROOT/'tools/vn/audit_prologue_flow.py')
flow=importlib.util.module_from_spec(spec)
spec.loader.exec_module(flow)

def canonical_old(value):
    if isinstance(value,list):return [canonical_old(x) for x in value]
    if not isinstance(value,dict):return value
    result={}
    for k,v in value.items():
        if k=='text':continue
        if k=='s' and v=='徐嘉颖':v='嘉颖徐'
        if k=='condition':v={'first_run_conservative':'first_run_briefing_vehicle','first_run_aggressive':'first_run_briefing_return'}.get(v,v)
        if k=='setValue':v={
            'first_run=conservative':'first_run_briefing_vehicle=true,first_run_briefing_return=false',
            'first_run=aggressive':'first_run_briefing_vehicle=false,first_run_briefing_return=true'}.get(v,v)
        result[k]=canonical_old(v)
    return result

reports=[]
loaded={}
for path in sorted(SCRIPTS.glob('prologue_*.json')):
    before=json.loads((OUT/'before'/path.name).read_text(encoding='utf-8-sig'))
    after=json.loads(path.read_text(encoding='utf-8-sig'))
    assert canonical_old(before)==canonical_old(after),path.name
    loaded[path.stem[9:11]]=after
    reports.append(flow.audit(path))

def text(ch,scene,entry):return loaded[ch]['scenes'][scene]['d'][entry]['text']
assert '待复测' in text('06',1,21) and '只多不少' not in text('06',1,21)
assert '茶山' in text('06',1,24) and '十五公里松桥' in text('06',1,25)
repair=''.join(e.get('text','') for e in loaded['07']['scenes'][2]['d'][18:37])
assert not any(x in repair for x in ['一等奖','一千两百','一千二百','零资金额外支出','手工研磨'])
assert '台架' in repair and '耗材' in repair
assert '不能用于付款' in text('08',3,42)
assert '清点完毕' in text('09',1,27)
alltext='\n'.join(e.get('text','') for j in loaded.values() for s in j['scenes'] for e in s['d'])
assert not any(x in alltext for x in ['七千五百八十','金日成大学','金日成综合大学','陈爷爷','老陈叔','前天才买','两个星期没车跑','十九天后'])
assert '三十七年' in text('01',2,24) and '三十七年' in text('04',3,41)
assert '十天' in text('04',5,10) and '九天' in text('06',1,35)
assert '九十天' in text('09',2,20) and '十月八日' in text('10',0,36)
assert sum(map(Decimal,['8046.30','15000','5136.40']))==Decimal('28182.70')
assert Decimal('28182.70')-Decimal('20000')==Decimal('8182.70')
assert sum([8400,1600,2000,1500,4000,2000])==19500
assert 27*46==1242 and 27*420==11340
choice=loaded['08']['scenes'][1]['d'][51]
for option in choice['opts']:
    for initial in [{},{'first_run_briefing_vehicle':True,'first_run_briefing_return':True}]:
        flow.apply(option['setValue'],initial)
        assert initial['first_run_briefing_vehicle']!=initial['first_run_briefing_return']

result=dict(scope='Static checks; no Unity playback or device acceptance',
    structure='Coordinates/resources/targets unchanged; allowed display-name and briefing-flag rename only',
    continuity_checks='passed',scripts=reports)
(OUT/'flow_audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print('FOLLOWUP_STATIC_OK scripts=10 choices='+str(sum(r['choices'] for r in reports))+' options='+str(sum(r['options'] for r in reports)))
