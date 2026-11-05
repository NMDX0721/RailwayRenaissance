"""Static conditional-flow traversal; not a substitute for Unity UI/playthrough acceptance."""
import collections
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = ROOT / 'Assets/Resources/Scripts'


def condition(value, flags):
    if not value:
        return True
    value = value.strip()
    return not flags.get(value[1:].strip(), False) if value.startswith('!') else flags.get(value, False)


def apply(value, flags):
    for assignment in (value or '').split(','):
        parts = assignment.split('=')
        if len(parts) != 2 or not parts[0].strip():
            continue
        name, value = (part.strip() for part in parts)
        if value in ('true', '1', 'false', '0'):
            flags[name] = value in ('true', '1')
        else:
            flags[name + '_' + value.lower()] = True


def audit(path):
    raw = path.read_bytes()
    script = json.loads(raw.decode('utf-8-sig'))
    scenes = script['scenes']
    choices, all_options = set(), set()
    for si, scene in enumerate(scenes):
        for di, entry in enumerate(scene['d']):
            if entry.get('t') != 'c':
                continue
            choices.add((si, di))
            for oi, option in enumerate(entry.get('opts', [])):
                target, index = option['next'], option.get('nextDialogue', 0)
                assert 0 <= target < len(scenes), (path.name, si, di, target)
                assert 0 <= index < len(scenes[target]['d']), (path.name, si, di, index)
                all_options.add((si, di, oi))
    queue = collections.deque([(0, 0, {})])
    visited, reached_choices, reached_options = set(), set(), set()
    endings = 0
    while queue:
        si, di, flags = queue.popleft()
        key = (si, di, tuple(sorted(flags.items())))
        if key in visited:
            continue
        visited.add(key)
        assert len(visited) < 50000, 'Unexpected flow expansion or loop'
        if si >= len(scenes):
            endings += 1
            continue
        if di >= len(scenes[si]['d']):
            queue.append((si + 1, 0, flags))
            continue
        entry = scenes[si]['d'][di]
        if not condition(entry.get('condition'), flags):
            queue.append((si, di + 1, flags))
            continue
        flags = dict(flags)
        apply(entry.get('setValue'), flags)
        if entry.get('t') == 'c':
            reached_choices.add((si, di))
            available = [(oi, opt) for oi, opt in enumerate(entry['opts']) if condition(opt.get('condition'), flags)]
            assert available, (path.name, si, di, 'No visible options')
            for oi, option in available:
                reached_options.add((si, di, oi))
                selected = dict(flags)
                apply(option.get('setValue'), selected)
                queue.append((option['next'], option.get('nextDialogue', 0), selected))
        elif entry.get('t') == 'special' and entry.get('text') == 'TRANSITION_TO_GAMEPLAY':
            endings += 1
        else:
            queue.append((si, di + 1, flags))
    assert choices == reached_choices, (path.name, 'Unreachable choices', choices - reached_choices)
    assert all_options == reached_options, (path.name, 'Unreachable options', all_options - reached_options)
    assert endings > 0, (path.name, 'No exit')
    next_script = script.get('nextScript')
    assert not next_script or (SCRIPTS / (next_script + '.json')).exists(), next_script
    return dict(script=path.stem, sha256=hashlib.sha256(raw).hexdigest(), scenes=len(scenes),
        choices=len(choices), options=len(all_options), visited_states=len(visited), exit_states=endings)


if __name__ == '__main__':
    reports = [audit(path) for path in sorted(SCRIPTS.glob('prologue_*.json'))]
    output = ROOT / 'tools/vn/prologue_flow_audit_20261102.json'
    output.write_text(json.dumps(dict(scope=__doc__, scripts=reports), indent=2), encoding='utf-8')
    print('PROLOGUE_STATIC_FLOW_OK scripts=' + str(len(reports)) + ' choices=' + str(sum(r['choices'] for r in reports))
        + ' options=' + str(sum(r['options'] for r in reports)) + '; runtime acceptance separate')
