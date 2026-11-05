"""Structural final text checks; literary review remains a separate judgement."""
import collections
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
report = dict(scope=__doc__, counts={}, source_sha256={}, errors=[], long_entries=[])
counts = collections.Counter()
for path in sorted((ROOT / 'Assets/Resources/Scripts').glob('prologue_*.json')):
    report['source_sha256'][path.stem] = hashlib.sha256(path.read_bytes()).hexdigest()
    for si, scene in enumerate(json.loads(path.read_text(encoding='utf-8-sig'))['scenes']):
        for di, entry in enumerate(scene['d']):
            kind, text = entry.get('t'), entry.get('text', '')
            counts[kind] += 1
            location = f'{path.stem}/{si}/{di}'
            if kind == 'thought' and not (entry.get('s') and entry.get('e') and text.startswith('[') and text.endswith(']')):
                report['errors'].append(dict(location=location, issue='thought schema'))
            if kind in ('d', 'n', 'thought') and re.search(r'TODO|待生成|占位|待补|\ufffd', text):
                report['errors'].append(dict(location=location, issue='placeholder or encoding', text=text))
            if kind == 'd' and not entry.get('s'):
                report['errors'].append(dict(location=location, issue='missing speaker'))
            if kind in ('d', 'n', 'thought') and len(text) > 90:
                report['long_entries'].append(dict(location=location, text=text))
report['counts'] = dict(counts)
output = ROOT / 'tools/vn/final_text_audit_20261103.json'
output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print('VN_FINAL_TEXT_AUDIT', 'errors=' + str(len(report['errors'])), 'readable_entries=' + str(sum(counts[k] for k in ('d', 'n', 'thought'))))
if report['errors']:
    raise SystemExit(1)
