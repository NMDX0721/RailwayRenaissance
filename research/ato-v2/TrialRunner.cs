using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using RailwayRenaissance.Cab.Simulation;
using RailwayRenaissance.Cab.Research;
static partial class Program
{
    sealed class TrialCase {public int Seed {get;set;}public double MassScale {get;set;}=1;public double BrakeScale {get;set;}=1;public double Grade {get;set;}public double Grip {get;set;}=1;public string Name {get;set;}="";}
    sealed class TrialProtocol {public int Version {get;set;}=1;public string FrozenUtc {get;set;}="";public int Repeats {get;set;}=2;public TrialCase[] Cases {get;set;}=Array.Empty<TrialCase>();public Dictionary<string,string> Hashes {get;set;}=new Dictionary<string,string>();}
    static readonly string[] FrozenFiles={"Assets/Scripts/Cab/CabSimulation.cs","Assets/Scripts/Cab/CabSimulationCodec.cs","Assets/Scripts/Cab/Research/CabAtoController.cs","Assets/Scripts/Cab/Research/CabAtoControllerV2.cs","Assets/Scripts/Cab/Research/AtoRouteTask.cs","research/ato-v2/TrialRunner.cs"};
    static string Digest(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    static readonly JsonSerializerOptions Pretty=new JsonSerializerOptions{WriteIndented=true};
    static void FreezeTrials()
    {
        const string path="research/ato-v2/protocol-v1.json";if(File.Exists(path))throw new Exception("Protocol already exists; create a new version instead of overwriting");
        var cases=new List<TrialCase>();foreach(int seed in new[]{4100,4101})
        {cases.Add(new TrialCase{Seed=seed,Name="nominal"});cases.Add(new TrialCase{Seed=seed,Name="weak-wet-heavy",MassScale=1.08,BrakeScale=.83,Grip=.55,Grade=-3});cases.Add(new TrialCase{Seed=seed,Name="strong-uphill",MassScale=.94,BrakeScale=1.17,Grade=3});}
        File.WriteAllText(path,JsonSerializer.Serialize(new TrialProtocol{FrozenUtc=DateTime.UtcNow.ToString("O"),Cases=cases.ToArray(),Hashes=FrozenFiles.ToDictionary(p=>p,Digest)},Pretty));Console.WriteLine("Fresh trial protocol frozen before viewing outputs.");
    }
    static void RunTrials()
    {
        var protocol=JsonSerializer.Deserialize<TrialProtocol>(File.ReadAllText("research/ato-v2/protocol-v1.json"));
        foreach(var pair in protocol.Hashes)Assert(Digest(pair.Key)==pair.Value,"Frozen source changed: "+pair.Key);
        string directory="research/ato-v2/runs/"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ");Directory.CreateDirectory(directory);
        File.Copy("research/ato-v2/protocol-v1.json",directory+"/protocol.json");
        var results=new List<object>();var deterministic=new Dictionary<string,string>();
        foreach(var scenario in protocol.Cases)for(int repeat=0;repeat<protocol.Repeats;repeat++)for(int method=0;method<7;method++)
        {
            var cfg=new SimulationConfig();cfg.TrainMassKg*=scenario.MassScale;cfg.ServiceBrakeN*=scenario.BrakeScale;cfg.LocoBrakeN*=scenario.BrakeScale;
            var m=new CabSimulation(scenario.Seed,cfg);var u=new SimulationInput{Master=true,FuelPump=true,EngineRun=true,Brake=1};for(int k=0;k<15000;k++)m.Step(u);
            u.ParkingBrake=false;u.Direction=1;u.GradePermille=scenario.Grade;u.RailGrip=scenario.Grip;
            long start=m.State.Tick;double initialAir=m.State.ExhaustAirKpaL,initialFuel=m.State.FuelUsedL;
            var task=AtoRouteTask.Standard(m.State.PositionM);
            var baseline=method==0?new CabAtoController(AtoMethod.AdaptivePredictive,task.Target,12,180,.15,25):null;
            var v2=method==0?null:new CabAtoControllerV2(task,start,(AtoObjective)((method-1)/2),(method-1)%2==1);
            string name=method==0?"FrozenV1RouteGuard":((AtoObjective)((method-1)/2)).ToString()+((method-1)%2==1?"Adaptive":"Fixed");
            string id=$"{scenario.Seed}-{scenario.Name}-{name}-r{repeat}";
            File.WriteAllText(directory+"/"+id+"-initial.json",JsonSerializer.Serialize(new{config=SimulationCodec.Encode(cfg),state=SimulationCodec.Encode(m.State),input=SimulationCodec.Encode(u),task=task.Encode()},Pretty));
            var trace=new List<string>();int switches=0,violationTicks=0,dwell=0,lastThrottle=0,lastBrake=1;double jerk=0,previousA=m.State.Acceleration,maxOverspeed=0,arrival=-1;bool intervention=false,finished=false;string reason="";double controllerMs=0;
            for(int k=0;k<18000;k++)
            {
                if(k%5==0&&!finished)
                {
                    var timer=Stopwatch.StartNew();var o=Observe(m.State);var cmd=baseline!=null?baseline.Decide(o):v2.Decide(o);
                    // Frozen V1 has no task API: an explicit, frozen route guard supplies the same profile information.
                    if(baseline!=null&&!baseline.BrakingPhase&&o.Speed>task.PreviewSpeed(o.Position+o.Speed*3,.4)-.5)cmd=new AtoCommand(0,o.Speed>task.PreviewSpeed(o.Position,.4)?2:1,"V1公共路线提前限速适配");
                    timer.Stop();controllerMs+=timer.Elapsed.TotalMilliseconds;reason=cmd.Reason;
                    u.Brake=cmd.Brake;u.Throttle=cmd.Throttle;
                    if(m.State.TrainCylinderKpa>=20||m.State.PipeKpa<470||u.Brake>=2)u.Throttle=0;
                    if(lastThrottle!=u.Throttle||lastBrake!=u.Brake)switches++;lastThrottle=u.Throttle;lastBrake=u.Brake;
                    trace.Add($"{m.State.Tick},{cmd.Throttle},{cmd.Brake},{u.Throttle},{u.Brake},{reason}");
                    if(v2!=null&&v2.Intervention){intervention=true;finished=true;}
                }
                m.Step(u);double excess=Math.Max(0,m.State.SpeedMps-task.LimitAt(m.State.PositionM));maxOverspeed=Math.Max(maxOverspeed,excess);if(excess>.15)violationTicks++;
                jerk+=Math.Pow((m.State.Acceleration-previousA)/.02,2)*.02;previousA=m.State.Acceleration;
                bool stopped=Math.Abs(m.State.SpeedMps)<.01&&(baseline!=null?baseline.BrakingPhase:v2.Snapshot.Braking);
                if(stopped){if(arrival<0)arrival=(m.State.Tick-start)*.02;dwell++;}else{dwell=0;arrival=-1;}
                if(dwell>=100)break;
            }
            string final=SimulationCodec.Encode(m.State);string key=$"{scenario.Seed}-{scenario.Name}-{name}";string fingerprint=DigestText(final+string.Join("\n",trace));
            if(deterministic.TryGetValue(key,out string previous))Assert(previous==fingerprint,"Repeat differs: "+key);else deterministic[key]=fingerprint;
            double error=m.State.PositionM-task.Target;
            var result=new{id,name,scenario=scenario.Name,seed=scenario.Seed,repeat,error,arrival,timeError=arrival<0?(double?)null:arrival-120,withinStopWindow=arrival>=0&&Math.Abs(error)<=5,onTime=arrival>=0&&Math.Abs(arrival-120)<=5,intervention,violationTicks,violationSeconds=violationTicks*.02,maxOverspeed,switches,jerkIntegral=jerk,exhaustProxyKpaL=m.State.ExhaustAirKpaL-initialAir,fuelL=m.State.FuelUsedL-initialFuel,controllerMs,reason,finalState=final,fingerprint};
            results.Add(result);File.WriteAllLines(directory+"/"+id+"-commands.csv",new[]{"tick,requestedThrottle,requestedBrake,acceptedThrottle,acceptedBrake,reason"}.Concat(trace));
            File.WriteAllText(directory+"/results.json",JsonSerializer.Serialize(results,Pretty));Console.WriteLine($"{id}: error={error:F2}, arrival={arrival:F2}, intervene={intervention}, violations={violationTicks}");
        }
        File.WriteAllText(directory+"/COMPLETE.txt","All frozen paired trials completed. Exact repeats matched.\n");Console.WriteLine(directory);
    }
    static string DigestText(string s)=>Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(s)));
}
