# -*- coding: utf-8 -*-
import json, os, re, sys
sys.stdout.reconfigure(encoding='utf-8')
base = r'D:/Unity Project/RailwayRenaissance/Assets/Resources'
files = [
 'Scripts/prologue_01_news.json',
 'Scripts/prologue_02_day0.json',
 'Scripts/prologue_03_journey.json',
 'Scripts/prologue_04_arrival.json',
 'Scripts/prologue_05_inspection.json',
 'Scripts/prologue_06_team.json',
 'Scripts/prologue_07_first_repair.json',
 'Scripts/prologue_08_first_run.json',
 'Scripts/prologue_09_funding.json',
 'Scripts/prologue_10_transition.json',
]
bg_dir = os.path.join(base, 'bg')
bgm_dir = os.path.join(base, 'bgm')
bg_files = set()
for f in os.listdir(bg_dir):
    if f.lower().endswith(('.png','.jpg','.jpeg')):
        bg_files.add(os.path.splitext(f)[0])
bgm_files = set()
for f in os.listdir(bgm_dir):
    if f.lower().endswith(('.mp3','.ogg','.wav')):
        bgm_files.add(os.path.splitext(f)[0])
EXPR = {'normal','smile','sad','surprise','serious','curious','excited','worried','angry','bored','gentle','happy','shocked','shout','smug','wink','cry','calm','shy'}
print('BG assets:', sorted(bg_files))
print('BGM assets:', sorted(bgm_files))
print()
for rel in files:
    path = os.path.join(base, rel)
    with open(path, encoding='utf-8') as fh:
        raw = fh.read()
        data = json.loads(raw)
    lines = raw.splitlines()
    print('='*60)
    print(rel, 'scenes=', len(data['scenes']))
    for i, line in enumerate(lines, 1):
        m = re.search(r'"bg":\s*"([^"]*)"', line)
        if m:
            v = m.group(1)
            ok = 'OK' if v in bg_files else 'MISSING'
            print(f'  L{i} bg={v} {ok}')
        m = re.search(r'"bgm":\s*"([^"]*)"', line)
        if m:
            v = m.group(1)
            if v == '':
                print(f'  L{i} bgm=(empty)')
            else:
                ok = 'OK' if v in bgm_files else 'MISSING'
                print(f'  L{i} bgm={v} {ok}')
    for i, line in enumerate(lines, 1):
        m = re.search(r'"e":\s*"([^"]*)"', line)
        if m:
            v = m.group(1)
            if v not in EXPR:
                print(f'  L{i} e={v} INVALID')
    # reactions blocks
    for i, line in enumerate(lines, 1):
        if '"reactions"' in line:
            block = []
            for j in range(i-1, min(i+8, len(lines))):
                block.append(lines[j])
                if ']' in lines[j] and j > i-1:
                    break
            btxt = ' '.join(block)
            for r in re.findall(r'"([a-z_]+:[a-z_]+)"', btxt):
                name, emo = r.split(':')
                if emo not in EXPR:
                    print(f'  L{i} reaction {r} INVALID_EMO')
                else:
                    print(f'  L{i} reaction OK {r}')
    speakers = {}
    for i, line in enumerate(lines, 1):
        m = re.search(r'"s":\s*"([^"]*)"', line)
        if m:
            speakers.setdefault(m.group(1), []).append(i)
    print('  speakers:', {k: (len(v), v[0]) for k,v in speakers.items()})
    # dialogue action parens
    for i, line in enumerate(lines, 1):
        if '"text":' in line:
            m = re.search(r'"text":\s*"(.*)"\s*,?\s*$', line)
            if m:
                t = m.group(1)
                if re.search(r'[（(][^）)]{1,30}[）)]', t):
                    print(f'  L{i} text_paren: {t[:100]}')
                if t.strip().startswith('[') and t.count('[') > 1:
                    print(f'  L{i} multi_bracket: {t[:100]}')
    for i, line in enumerate(lines, 1):
        hits = re.findall(r'(Day\s*\d+|207\d年|205\d年|206\d年|第[一二三四五1-9]天)', line)
        if hits:
            print(f'  L{i} time: {hits} | {line.strip()[:100]}')
    for i, line in enumerate(lines, 1):
        m = re.search(r'"next":\s*(\d+)', line)
        if m:
            print(f'  L{i} choice next={m.group(1)} total_scenes={len(data["scenes"])}')
    m = re.search(r'"nextScript":\s*"([^"]+)"', raw)
    print('  nextScript:', m.group(1) if m else '(none)')
    # cg refs
    for i, line in enumerate(lines, 1):
        m = re.search(r'"t":\s*"cg"', line)
        if m and i+1 <= len(lines):
            m2 = re.search(r'"text":\s*"([^"]+)"', lines[i])
            if m2:
                print(f'  L{i+1} cg={m2.group(1)}')
print('DONE')
