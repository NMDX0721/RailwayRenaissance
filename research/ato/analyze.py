"""Deterministic finite-grid summaries; no real-world probability inference."""
import csv
import hashlib
import json
from pathlib import Path
import sys
from collections import defaultdict
from statistics import mean

root = Path(sys.argv[1])
rows = json.loads((root / 'results.json').read_text(encoding='utf-8'))
manifest = json.loads((root / 'manifest.json').read_text(encoding='utf-8'))
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest().upper()
assert sha(root / 'protocol.json') == manifest['protocolHash']
assert sha(root / 'CabAtoController.cs.txt') == manifest['controllerHash']
assert sha(root / 'CabSimulation.cs.txt') == manifest['sourceHash']
groups = defaultdict(list)
initials = {}
for r in rows:
    key = tuple(r[k] for k in ('seed', 'mass', 'grip', 'grade', 'targetDistance', 'targetSpeed'))
    assert initials.setdefault(key, r['initialState']) == r['initialState'], 'Unpaired initial state'
    groups[(r['method'], r['dwellTicks'])].append(r)
assert len(rows) == manifest['rowCount'] == 448
assert len(initials) == 64
summary = []
minimum_gap = None
checked_changes = 0
for (method, dwell), group in sorted(groups.items()):
    assert len(group) == 64
    summary.append(dict(method=method, dwellTicks=dwell, n=len(group),
                        stopped=sum(r['stopped'] for r in group),
                        within5m=sum(r['reachedTolerance'] for r in group),
                        meanAbsoluteError=mean(abs(r['errorM']) for r in group),
                        maxAbsoluteError=max(abs(r['errorM']) for r in group),
                        meanSwitches=mean(r['switches'] for r in group)))
    if dwell != 25 or method not in ('EarlyPredictive', 'AdaptivePredictive'):
        continue
    for r in group:
        filename = f"{method}-{r['seed']}-{r['mass']:g}-{r['grip']:g}-{r['grade']:g}-{r['targetDistance']:g}-{r['targetSpeed']:g}-{dwell}.csv"
        last_brake = last_change = None
        with (root / filename).open(newline='', encoding='utf-8') as stream:
            for sample in csv.DictReader(stream):
                tick, brake = int(sample['tick']), int(sample['brake'])
                regular = int(sample['plans']) > 0 and float(sample['speed_mps']) > .01 and float(sample['position_m']) < float(sample['target_m'])
                if not regular:
                    last_brake = last_change = None
                    continue
                if last_brake is not None and brake != last_brake:
                    if last_change is not None:
                        gap = tick - last_change
                        assert gap >= dwell, f'Residence violation: {filename} {tick}'
                        minimum_gap = gap if minimum_gap is None else min(minimum_gap, gap)
                        checked_changes += 1
                    last_change = tick
                last_brake = brake
report = dict(scope='Finite held-out scenario grid, not independent field samples',
              pairedConditions=len(initials), rows=len(rows),
              regularSwitchIntervalsChecked=checked_changes,
              minimumRegularIntervalTicks=minimum_gap,
              terminalAndCalibrationTransitionsExcluded=True, summary=summary)
out = root / 'analysis.json'
if out.exists():
    raise SystemExit('Refusing to overwrite analysis.json')
out.write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
