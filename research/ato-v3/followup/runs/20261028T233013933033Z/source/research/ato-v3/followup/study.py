"""Freeze and run a separate sensitivity/ablation study; never overwrites prior results."""
import hashlib,json,pathlib,shutil,subprocess,sys,zipfile,datetime
ROOT=pathlib.Path(__file__).resolve().parents[3]
BASE=ROOT/'research/ato-v3/followup'
FILES=['Assets/Scripts/Cab/CabSimulation.cs','Assets/Scripts/Cab/CabSimulationCodec.cs','Assets/Scripts/Cab/Research/AtoRouteTask.cs','Assets/Scripts/Cab/Research/AtoTrajectoryPlanner.cs','Assets/Scripts/Cab/Research/CabAtoController.cs','Assets/Scripts/Cab/Research/CabAtoControllerV2.cs','Assets/Scripts/Cab/Research/CabAtoControllerV3.cs','research/ato-v3/followup/Runner.cs','research/ato-v3/followup/study.py']
def digest(data): return hashlib.sha256(data).hexdigest().upper()
def freeze():
    BASE.mkdir(exist_ok=True)
    protocol={'frozenUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'seeds':[64001,64002],'scenarios':['nominal','weak_wet','weak_brake','strong_brake','downhill','uphill','low_grip'],'modes':['Smooth','AirSaving','Punctual'],'repeats':2,'variants':{'baseline':'V3 unchanged','fixed_estimates':'disable both coast and brake-gain updates, retain their initial values','shared_reference':'all modes use the Punctual reference; mode-specific local scoring remains'},'constraints':'900 m, first arrival 115-125 s, stop +/-5 m, 100-tick dwell, no intervention or speed violation above 0.15 m/s','hashes':{p:digest((ROOT/p).read_bytes()) for p in FILES}}
    with (BASE/'protocol-v2.json').open('x',encoding='utf-8') as f: json.dump(protocol,f,indent=2)
    with zipfile.ZipFile(BASE/'frozen_sources-v2.zip','x',zipfile.ZIP_DEFLATED) as z:
        for p in FILES:z.write(ROOT/p,p)
    print('Protocol and source archive frozen before outputs.')
def run():
    p=json.loads((BASE/'protocol-v2.json').read_text(encoding='utf-8')); out=BASE/'runs'/datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S%fZ');out.mkdir(parents=True)
    shutil.copy(BASE/'protocol-v2.json',out/'protocol.json'); shutil.copy(BASE/'frozen_sources-v2.zip',out/'frozen_sources.zip')
    with zipfile.ZipFile(BASE/'frozen_sources-v2.zip') as z:
        for name,h in p['hashes'].items():
            assert digest(z.read(name))==h,name
        z.extractall(out/'source')
    source=out/'source'; controller=source/'Assets/Scripts/Cab/Research/CabAtoControllerV3.cs'; original=controller.read_text(encoding='utf-8')
    coast='if(acceptedThrottle==0&&acceptedBrake<=1&&o.CylinderKpa<5&&o.Speed>2)state.Coast=Clamp(.9*state.Coast+.1*o.Acceleration,-.3,.3);'
    brake='if(acceptedThrottle==0&&o.CylinderKpa>140&&o.Speed>2){double gain=(state.Coast-o.Acceleration)/(o.CylinderKpa/380);state.BrakeGain=Clamp(.9*state.BrakeGain+.1*Clamp(gain,.35,1.8),.35,1.8);}'
    for variant in p['variants']:
        code=original
        if variant=='fixed_estimates':
            assert code.count(coast)==1 and code.count(brake)==1;code=code.replace(coast,'').replace(brake,'')
        if variant=='shared_reference':
            a='PlanForMode(task,objective,initialSpeed)';b='PlanForMode(task,(AtoObjective)state.PendingMode,state.InitialSpeed)'
            assert code.count(a)==1 and code.count(b)==1;code=code.replace(a,'PlanForMode(task,AtoObjective.Punctual,initialSpeed)').replace(b,'PlanForMode(task,AtoObjective.Punctual,state.InitialSpeed)')
        controller.write_text(code,encoding='utf-8'); project=source/'Study.csproj'
        project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net6.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+x+'" />' for x in FILES if x.endswith('.cs'))+'</ItemGroup></Project>',encoding='utf-8')
        subprocess.run(['dotnet','run','-c','Release','--project',str(project),'--',variant,str(out)],check=True,cwd=ROOT)
    controller.write_text(original,encoding='utf-8')
    rows=[]
    for variant in p['variants']:rows.extend(json.loads((out/(variant+'-results.json')).read_text(encoding='utf-8')))
    groups={}
    for row in rows:groups.setdefault((row['seed'],row['scenario'],row['mode'],row['variant']),[]).append(row)
    assert len(rows)==len(p['seeds'])*len(p['scenarios'])*len(p['modes'])*len(p['variants'])*p['repeats']
    for key,r in groups.items():assert len(r)==2 and r[0]['fingerprint']==r[1]['fingerprint'],key
    (out/'COMPLETE.txt').write_text('All repeats match per case and variant.\n',encoding='utf-8');print('COMPLETE',out)
if __name__=='__main__': {'freeze':freeze,'run':run}[sys.argv[1]]()
