"""Export shortest real-script choice prefixes for every reachable option."""
import collections
import json
from pathlib import Path
from audit_prologue_flow import apply, condition

ROOT = Path(__file__).resolve().parents[2]
cases = []
for source in sorted((ROOT / 'Assets/Resources/Scripts').glob('prologue_*.json')):
    scenes = json.loads(source.read_text(encoding='utf-8-sig'))['scenes']
    queue = collections.deque([(0, 0, {}, [])])
    seen, covered = set(), set()
    while queue:
        si, di, flags, path = queue.popleft()
        key = (si, di, tuple(sorted(flags.items())))
        if key in seen or si >= len(scenes):
            continue
        seen.add(key)
        if di >= len(scenes[si]['d']):
            queue.append((si + 1, 0, flags, path))
            continue
        entry = scenes[si]['d'][di]
        if not condition(entry.get('condition'), flags):
            queue.append((si, di + 1, flags, path))
            continue
        flags = dict(flags)
        apply(entry.get('setValue'), flags)
        if entry.get('t') == 'c':
            for index, option in enumerate(entry['opts']):
                if not condition(option.get('condition'), flags):
                    continue
                prefix = path + [dict(scene=si, entry=di, option=index)]
                if (si, di, index) not in covered:
                    covered.add((si, di, index))
                    cases.append(dict(script=source.stem, selections=prefix))
                selected = dict(flags)
                apply(option.get('setValue'), selected)
                queue.append((option['next'], option.get('nextDialogue', 0), selected, prefix))
        elif entry.get('text') != 'TRANSITION_TO_GAMEPLAY':
            queue.append((si, di + 1, flags, path))
output = ROOT / 'Library/VNCompiledReviewProject/Assets/Resources/vn_flow_cases.json'
output.write_text(json.dumps(dict(cases=cases), ensure_ascii=False, indent=2), encoding='utf-8')
print('VN_RUNTIME_CASES', len(cases))
