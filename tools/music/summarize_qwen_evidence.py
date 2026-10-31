"""Summarize automatic boundary stability without claiming human or word verification."""
import json
from pathlib import Path
from statistics import median
from check_qwen_boundaries import lexical, ROOT, VARIANTS


def main():
    base = ROOT / 'tools/music/alignment_review'
    summary = dict(automaticEvidenceOnly=True, humanListeningVerified=False, published=False, tracks=[])
    for track in VARIANTS:
        path = base / f'qwen/{track}.boundaries.json'
        if not path.exists():
            continue
        report = json.loads(path.read_text(encoding='utf-8'))
        lines = []
        for number in sorted({w['targetLine'] for w in report['windows']}):
            measurements = []
            for window in report['windows']:
                if window['targetLine'] != number:
                    continue
                offset = 0
                for line in window['lines']:
                    size = len(lexical(line['alignmentInput']))
                    if line['line'] == number:
                        break
                    offset += size
                indices, position = [], 0
                for index, item in enumerate(window['items']):
                    end = position + len(lexical(item['text']))
                    if position < offset + size and end > offset:
                        indices.append(index)
                    position = end
                repaired = {i for repair in window['repairs'] for i in repair['changed']}
                boundary_repaired = not indices or indices[0]*2 in repaired or indices[-1]*2+1 in repaired
                valid = (line['start'] is not None and line['end'] is not None and line['end'] > line['start']
                         and not line['tokenCrossesSourceLine'] and not boundary_repaired
                         and 'token_text_mismatch' not in window['issues'])
                measurements.append(dict(role=window['role'], padding=window['padding'], start=line['start'], end=line['end'],
                                         boundaryWasRepaired=boundary_repaired, valid=valid))
            usable = [m for m in measurements if m['valid']]
            spread = dict(start=max(m['start'] for m in usable)-min(m['start'] for m in usable),
                          end=max(m['end'] for m in usable)-min(m['end'] for m in usable)) if usable else None
            stable = len(usable)==4 and spread['start']<=.25 and spread['end']<=.35
            lines.append(dict(line=number, measurements=measurements, spread=spread, fourWayStable=stable,
                              status='stable_model_candidate_requires_cross_model_check' if stable else 'automatic_evidence_insufficient',
                              candidate=dict(start=median(m['start'] for m in usable),end=median(m['end'] for m in usable)) if stable else None))
        summary['tracks'].append(dict(id=track, lines=lines))
    output = base / 'qwen/automatic-evidence.json'
    output.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
    for track in summary['tracks']:
        print(track['id'], 'four-way stable:', sum(l['fourWayStable'] for l in track['lines']), '/', len(track['lines']))


if __name__ == '__main__':
    main()
