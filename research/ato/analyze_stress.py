"""Describe paired finite stress profiles; do not infer field probabilities."""
import hashlib
import json
import math
from pathlib import Path
from collections import defaultdict
from statistics import mean
import sys

root = Path(sys.argv[1])
rows = json.loads((root / 'results.json').read_text(encoding='utf-8'))
manifest = json.loads((root / 'manifest.json').read_text(encoding='utf-8'))
protocol = json.loads((root / 'protocol.json').read_text(encoding='utf-8'))
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest().upper()
assert sha(root / 'protocol.json') == manifest['protocolHash']
assert sha(root / 'CabAtoController.cs.txt') == protocol['controllerHash'] == manifest['controllerHash']
assert sha(root / 'CabSimulation.cs.txt') == manifest['sourceHash']
assert manifest['legacyMetricCompatibilityChecked']
assert len(rows) == manifest['rowCount'] == 48
groups = defaultdict(list)
pairs = {}
for r in rows:
    key = (r['seed'], r['stressCase'])
    pair = (r['initialState'], r['configuration'])
    assert pairs.setdefault(key, pair) == pair, 'Unpaired initial state/config'
    assert r['observationDelayTicks'] in (0, 5, 15)
    for name in ('fuelUsedL', 'exhaustAirKpaL', 'compressorAirKpaL', 'movingJerkRms', 'peakJerk', 'preBrakeSpeedRmse'):
        assert math.isfinite(r[name]) and r[name] >= 0, name
    if r['gripDrop']:
        assert r['loweredGripTicks'] > 0, 'Grip profile never applied'
    groups[(r['method'], r['dwellTicks'])].append(r)

def summarize(group):
    return dict(n=len(group), within5m=sum(r['reachedTolerance'] for r in group),
                meanAbsoluteError=mean(abs(r['errorM']) for r in group),
                maxAbsoluteError=max(abs(r['errorM']) for r in group),
                meanSwitches=mean(r['switches'] for r in group),
                meanMovingJerkRms=mean(r['movingJerkRms'] for r in group),
                maxRawPeakJerk=max(r['peakJerk'] for r in group),
                meanExhaustKpaL=mean(r['exhaustAirKpaL'] for r in group),
                meanCompressorKpaL=mean(r['compressorAirKpaL'] for r in group),
                meanFuelL=mean(r['fuelUsedL'] for r in group),
                meanPreBrakeSpeedRmse=mean(r['preBrakeSpeedRmse'] for r in group),
                maxPeakSpeedMps=max(r['peakSpeedMps'] for r in group),
                adhesionCapBindingTicks=sum(r['brakeAdhesionCapTicks'] for r in group))

summary = [dict(method=k[0], dwellTicks=k[1], **summarize(g)) for k,g in sorted(groups.items())]
cases = []
for case in protocol['cases']:
    name = case['name']
    for (method, dwell), group in sorted(groups.items()):
        cases.append(dict(stressCase=name, method=method, dwellTicks=dwell,
                          **summarize([r for r in group if r['stressCase'] == name])))

if len(sys.argv) > 2:
    earlier = json.loads((Path(sys.argv[2]) / 'results.json').read_text(encoding='utf-8'))
    original = {(r['method'], r['seed'], r['dwellTicks'], r['stressCase']):r for r in earlier}
    for r in rows:
        old = original[(r['method'], r['seed'], r['dwellTicks'], r['stressCase'])]
        assert r['finalState'] == old['finalState'] and r['errorM'] == old['errorM'] and r['switches'] == old['switches'], 'Instrumentation changed results'

result = dict(scope='Finite paired stress profiles; not independent field samples',
              pairedProfiles=len(pairs), trajectoryCount=len(rows),
              controllerUnchanged=True, summary=summary, cases=cases,
              gasUnits='gauge pressure-volume proxy kPa L, not actual gas mass',
              jerk='raw 20ms acceleration derivative; moving RMS excludes speeds <=0.5m/s')
out = root / 'metrics_analysis.json'
if out.exists():
    raise SystemExit('Refusing to overwrite metrics_analysis.json')
out.write_text(json.dumps(result, indent=2), encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k != 'cases'}, indent=2))
