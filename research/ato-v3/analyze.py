import json,pathlib,sys,hashlib,zipfile,statistics,collections
root=pathlib.Path(sys.argv[1]) if len(sys.argv)>1 else pathlib.Path('research/ato-v3/runs/20261028T121643698Z')
p=json.loads((root/'protocol.json').read_text(encoding='utf-8-sig'));rows=json.loads((root/'results.json').read_text(encoding='utf-8-sig'))
assert (root/'COMPLETE.txt').exists() and len(rows)==len(p['seeds'])*len(p['scenarios'])*3*2*p['repeats']
with zipfile.ZipFile(root/'frozen_sources.zip') as z:
 for name,expected in p['hashes'].items():assert hashlib.sha256(z.read(name)).hexdigest().upper()==expected,name
pairs=collections.defaultdict(list)
for row in rows:pairs[(row['seed'],row['scenario'],row['mode'],row['controller'])].append(row)
for values in pairs.values():assert len(values)==p['repeats'] and len({v['fingerprint'] for v in values})==1
summary=[]
for controller in ('V2Adaptive','V3'):
 for mode in ('Smooth','AirSaving','Punctual'):
  group=[r for r in rows if r['controller']==controller and r['mode']==mode and r['repeat']==0]
  summary.append(dict(controller=controller,mode=mode,cases=len(group),success=sum(r['success'] for r in group),interventions=sum(r['intervention'] for r in group),violationCases=sum(r['violationTicks']>0 for r in group),meanSwitches=statistics.mean(r['switches'] for r in group),meanJerk=statistics.mean(r['jerkIntegral'] for r in group),meanExhaust=statistics.mean(r['exhaustProxyKpaL'] for r in group),meanFuel=statistics.mean(r['fuelL'] for r in group),maxControlP95Ms=max(r['controlP95Ms'] for r in group),maxInitialPlanningMs=max(r['initialPlanningMs'] for r in group)))
(root/'analysis.json').write_text(json.dumps(dict(rows=len(rows),independentCases=len(p['seeds'])*len(p['scenarios']),repeatFingerprintsMatch=True,summary=summary),indent=2),encoding='utf-8')
for entry in summary:print(entry)
