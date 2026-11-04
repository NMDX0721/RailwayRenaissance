using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RailwayRenaissance.Cab.Research;
using RailwayRenaissance.Cab.Simulation;
static class FrozenV3
{
 sealed class Protocol {public string frozenUtc {get;set;} public int[] seeds {get;set;} public string[] scenarios {get;set;} public int repeats {get;set;} public string task {get;set;} public string scenarioDefinitions {get;set;} public string metrics {get;set;} public Dictionary<string,string> hashes {get;set;}}
 static string[] Sources={"Assets/Scripts/Cab/CabSimulation.cs","Assets/Scripts/Cab/CabSimulationCodec.cs","Assets/Scripts/Cab/Research/AtoRouteTask.cs","Assets/Scripts/Cab/Research/AtoTrajectoryPlanner.cs","Assets/Scripts/Cab/Research/CabAtoController.cs","Assets/Scripts/Cab/Research/CabAtoControllerV2.cs","Assets/Scripts/Cab/Research/CabAtoControllerV3.cs","research/ato-v3/FrozenV3.cs","research/ato-v3/AtoV3.csproj","research/ato-v3/Program.cs"};
 static string Hash(byte[] b)=>Convert.ToHexString(SHA256.HashData(b));
 static string Digest(string path)=>Hash(File.ReadAllBytes(path));
 public static void Freeze(int version=1)
 {
  var p=new Protocol{frozenUtc=DateTime.UtcNow.ToString("O"),seeds=version==1?new[]{62001,62002,62003}:new[]{63001,63002,63003},scenarios=version==1?new[]{"nominal","weak_wet","changing_conditions"}:new[]{"nominal","weak_wet_v2","changing_conditions_v2"},repeats=2,task="900m; 12/18/10 m/s; arrival 120 +/- 5s; stop +/-5m; 50Hz plant, 10Hz control",scenarioDefinitions=version==1?"nominal: default; weak_wet: mass 1.10, brakes 0.80, grip 0.50, grade -4; changing_conditions: mass 0.95, brakes 1.15, grade +4 then at t=40s grip 0.45 and grade -4":"nominal: default; weak_wet_v2: mass 1.07, brakes 0.77, grip 0.48, grade -5; changing_conditions_v2: mass 1.03, brakes 1.10, grade +5 then at t=45s grip 0.40 and grade -5",metrics="stop error, first arrival, safety violation episodes and ticks above 0.15m/s, failure, switches, squared jerk integral, exhaust kPaL, fuel L, controller time, exact repeat fingerprint",hashes=new Dictionary<string,string>()};foreach(var f in Sources)p.hashes.Add(f,Digest(f));
  using var stream=new FileStream($"research/ato-v3/protocol-v{version}.json",FileMode.CreateNew,FileAccess.Write);JsonSerializer.Serialize(stream,p,new JsonSerializerOptions{WriteIndented=true});Console.WriteLine("V3 protocol frozen before test outputs");
 }
 public static void Run(int version=1)
 {
  var p=JsonSerializer.Deserialize<Protocol>(File.ReadAllText($"research/ato-v3/protocol-v{version}.json"));foreach(var pair in p.hashes)if(Digest(pair.Key)!=pair.Value)throw new Exception("Frozen source mismatch: "+pair.Key);
  string dir="research/ato-v3/runs/"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ");Directory.CreateDirectory(dir);File.Copy($"research/ato-v3/protocol-v{version}.json",dir+"/protocol.json");
  var results=new List<object>();var repeats=new Dictionary<string,string>();
  foreach(int seed in p.seeds)foreach(string scenario in p.scenarios)foreach(AtoObjective mode in Enum.GetValues(typeof(AtoObjective)))foreach(bool v3 in new[]{false,true})for(int repeat=0;repeat<p.repeats;repeat++)
  {
   var cfg=new SimulationConfig();if(scenario=="weak_wet"){cfg.TrainMassKg*=1.1;cfg.ServiceBrakeN*=.8;cfg.LocoBrakeN*=.8;}if(scenario=="changing_conditions"){cfg.TrainMassKg*=.95;cfg.ServiceBrakeN*=1.15;cfg.LocoBrakeN*=1.15;}if(scenario=="weak_wet_v2"){cfg.TrainMassKg*=1.07;cfg.ServiceBrakeN*=.77;cfg.LocoBrakeN*=.77;}if(scenario=="changing_conditions_v2"){cfg.TrainMassKg*=1.03;cfg.ServiceBrakeN*=1.10;cfg.LocoBrakeN*=1.10;}
   var m=new CabSimulation(seed,cfg);var u=new SimulationInput{Master=true,FuelPump=true,EngineRun=true,Brake=1};for(int k=0;k<15000;k++)m.Step(u);u.ParkingBrake=false;u.Direction=1;if(scenario=="weak_wet"){u.RailGrip=.5;u.GradePermille=-4;}if(scenario=="changing_conditions")u.GradePermille=4;if(scenario=="weak_wet_v2"){u.RailGrip=.48;u.GradePermille=-5;}if(scenario=="changing_conditions_v2")u.GradePermille=5;
   long start=m.State.Tick;var task=AtoRouteTask.Standard(m.State.PositionM);var old=new CabAtoControllerV2(task,start,mode,true);var planning=Stopwatch.StartNew();var next=new CabAtoControllerV3(task,start,mode);planning.Stop();double planningMs=planning.Elapsed.TotalMilliseconds;string key=$"{seed}-{scenario}-{mode}-{(v3?"v3":"v2")}";string id=key+"-r"+repeat;
   File.WriteAllText(dir+"/"+id+"-initial.json",JsonSerializer.Serialize(new{config=SimulationCodec.Encode(cfg),state=SimulationCodec.Encode(m.State),input=SimulationCodec.Encode(u),task=task.Encode()}));
   double fuel=m.State.FuelUsedL,air=m.State.ExhaustAirKpaL,arrival=-1,jerk=0,previous=m.State.Acceleration,maxOverspeed=0;int switches=0,lastT=0,lastB=1,violationTicks=0,violationEpisodes=0,dwell=0;bool intervention=false,timeout=true,previousViolation=false;string reason="";var trace=new List<string>();var durations=new List<double>();
   for(int i=0;i<18000;i++){
    if(scenario=="changing_conditions"&&i>=2000){u.RailGrip=.45;u.GradePermille=-4;}if(scenario=="changing_conditions_v2"&&i>=2250){u.RailGrip=.40;u.GradePermille=-5;}
    if(i%5==0&&!intervention){var z=m.State;var observation=new AtoObservation{Tick=z.Tick,Position=z.PositionM,Speed=z.SpeedMps,Acceleration=z.Acceleration,PipeKpa=z.PipeKpa,ControlKpa=z.ControlKpa,CylinderKpa=z.TrainCylinderKpa};var sw=Stopwatch.StartNew();var command=v3?next.Decide(observation,u.Throttle,u.Brake):old.Decide(observation);sw.Stop();durations.Add(sw.Elapsed.TotalMilliseconds);reason=command.Reason;int requested=command.Throttle;u.Throttle=requested;u.Brake=command.Brake;if(z.TrainCylinderKpa>=20||z.PipeKpa<470||u.Brake>=2)u.Throttle=0;if(lastT!=u.Throttle||lastB!=u.Brake)switches++;lastT=u.Throttle;lastB=u.Brake;intervention=v3?next.Snapshot.Intervention:old.Intervention;arrival=v3?next.Snapshot.Arrival:old.Snapshot.ArrivalSeconds;trace.Add($"{z.Tick},{requested},{command.Brake},{u.Throttle},{u.Brake}");}
    m.Step(u);double excess=Math.Max(0,m.State.SpeedMps-task.LimitAt(m.State.PositionM));maxOverspeed=Math.Max(maxOverspeed,excess);bool violation=excess>.15;if(violation){violationTicks++;if(!previousViolation)violationEpisodes++;}previousViolation=violation;jerk+=Math.Pow((m.State.Acceleration-previous)/.02,2)*.02;previous=m.State.Acceleration;
    if(arrival>=0&&Math.Abs(m.State.SpeedMps)<.01)dwell++;else dwell=0;
    if(dwell>=100||intervention&&Math.Abs(m.State.SpeedMps)<.01){timeout=false;break;}
   }
   string final=SimulationCodec.Encode(m.State);string fingerprint=Hash(Encoding.UTF8.GetBytes(final+string.Join("\n",trace)));if(repeats.TryGetValue(key,out string previousFingerprint)){if(fingerprint!=previousFingerprint)throw new Exception("Repeat mismatch: "+key);}else repeats.Add(key,fingerprint);
   durations.Sort();double p95=durations.Count==0?0:durations[(int)Math.Floor((durations.Count-1)*.95)];double error=m.State.PositionM-task.Target;
   var result=new{id,seed,scenario,mode=mode.ToString(),controller=v3?"V3":"V2Adaptive",repeat,error,arrival,arrivalDeviation=arrival<0?(double?)null:arrival-task.TargetSeconds,success=!intervention&&!timeout&&violationTicks==0&&Math.Abs(error)<=5&&arrival>=115&&arrival<=125,intervention,timeout,violationEpisodes,violationTicks,violationSeconds=violationTicks*.02,maxOverspeed,switches,jerkIntegral=jerk,exhaustProxyKpaL=m.State.ExhaustAirKpaL-air,fuelL=m.State.FuelUsedL-fuel,controlP95Ms=p95,initialPlanningMs=v3?planningMs:0,reason,finalController=v3?SimulationCodec.Encode(next.Snapshot):SimulationCodec.Encode(old.Snapshot),finalState=final,fingerprint};results.Add(result);
   File.WriteAllLines(dir+"/"+id+"-commands.csv",new[]{"tick,requestedThrottle,requestedBrake,acceptedThrottle,acceptedBrake"}.ConcatCompat(trace));File.WriteAllText(dir+"/results.json",JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
   Console.WriteLine($"{id} error={error:F2} arrival={arrival:F2} violation={violationTicks} intervention={intervention} timeout={timeout}");
  }
  File.WriteAllText(dir+"/COMPLETE.txt","All paired runs completed; exact repeat fingerprints match.\n");Console.WriteLine(dir);
 }
 static IEnumerable<string> ConcatCompat(this string[] header,List<string> trace){foreach(var h in header)yield return h;foreach(var line in trace)yield return line;}
}
