import json, pathlib, sys, hashlib, collections, statistics
root=pathlib.Path(sys.argv[1]) if len(sys.argv)>1 else sorted(pathlib.Path('research/ato-v2/runs').iterdir())[-1]
rows=json.loads((root/'results.json').read_text(encoding='utf-8-sig'))
protocol=json.loads((root/'protocol.json').read_text(encoding='utf-8-sig'))
assert (root/'COMPLETE.txt').exists()
assert len(rows)==len(protocol['Cases'])*protocol['Repeats']*7
for path,expected in protocol['Hashes'].items():assert hashlib.sha256(pathlib.Path(path).read_bytes()).hexdigest().upper()==expected,path
pairs=collections.defaultdict(list)
for row in rows:pairs[(row['seed'],row['scenario'],row['name'])].append(row)
for pair in pairs.values():assert len(pair)==protocol['Repeats'] and len({row['fingerprint'] for row in pair})==1
summary=[]
for name in sorted({r['name'] for r in rows}):
    group=[r for r in rows if r['name']==name and r['repeat']==0]
    normal=lambda r:r['withinStopWindow'] and not r['intervention'] and r['violationTicks']==0
    summary.append(dict(name=name,cases=len(group),normalSuccess=sum(normal(r) for r in group),interventions=sum(r['intervention'] for r in group),speedViolationCases=sum(r['violationTicks']>0 for r in group),onTimeNormal=sum(normal(r) and r['onTime'] for r in group),meanAbsStopError=statistics.mean(abs(r['error']) for r in group),meanSwitches=statistics.mean(r['switches'] for r in group),meanJerk=statistics.mean(r['jerkIntegral'] for r in group),meanExhaust=statistics.mean(r['exhaustProxyKpaL'] for r in group),meanFuel=statistics.mean(r['fuelL'] for r in group)))
(root/'analysis.json').write_text(json.dumps(dict(rows=len(rows),uniqueCases=len(protocol['Cases']),repeatsExact=True,summary=summary),indent=2),encoding='utf-8')
for item in summary:print(item)
