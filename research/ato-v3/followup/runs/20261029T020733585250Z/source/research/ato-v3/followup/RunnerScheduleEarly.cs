using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RailwayRenaissance.Cab.Simulation;
using RailwayRenaissance.Cab.Research;

// Separate experiment: no modifications to the game controller or physics kernel.
string variant=args[0], output=args[1]; Directory.CreateDirectory(output);
var rows=new List<object>();
foreach(int seed in new[]{63001,63003}) foreach(string scenario in new[]{"weak_wet"})
foreach(AtoObjective mode in Enum.GetValues(typeof(AtoObjective))) for(int repeat=0;repeat<2;repeat++) {
 var cfg=new SimulationConfig(); double strength=scenario=="weak_wet"||scenario=="weak_brake"?.77:scenario=="strong_brake"?1.15:1;
 cfg.ServiceBrakeN*=strength;cfg.LocoBrakeN*=strength;if(scenario=="weak_wet")cfg.TrainMassKg*=1.07;
 var m=new CabSimulation(seed,cfg);var u=new SimulationInput{Master=true,FuelPump=true,EngineRun=true,Brake=1};for(int k=0;k<15000;k++)m.Step(u);
 u.ParkingBrake=false;u.Direction=1;u.GradePermille=scenario=="weak_wet"||scenario=="downhill"?-5:scenario=="uphill"?5:0;
 if(scenario=="weak_wet"||scenario=="low_grip")u.RailGrip=.48;
 var task=AtoRouteTask.Standard(m.State.PositionM);var c=new CabAtoControllerV3(task,m.State.Tick,mode);long start=m.State.Tick;
 double fuel=m.State.FuelUsedL,air=m.State.ExhaustAirKpaL,arrival=-1,jerk=0,lastAccel=m.State.Acceleration;int switches=0,lastT=0,lastB=1,violationTicks=0,dwell=0;bool intervention=false,timeout=true;var trace=new StringBuilder("tick,position,speed,acceleration,throttle,brake,coast,brakeGain,eta\n");
 var commands=new StringBuilder();
 for(int i=0;i<18000;i++){
  if(i%5==0&&!intervention){var z=m.State;var o=new AtoObservation{Tick=z.Tick,Position=z.PositionM,Speed=z.SpeedMps,Acceleration=z.Acceleration,PipeKpa=z.PipeKpa,ControlKpa=z.ControlKpa,CylinderKpa=z.TrainCylinderKpa};var cmd=c.Decide(o,u.Throttle,u.Brake);int requested=cmd.Throttle;u.Throttle=requested;u.Brake=cmd.Brake;if(z.TrainCylinderKpa>=20||z.PipeKpa<470||u.Brake>=2)u.Throttle=0;if(lastT!=u.Throttle||lastB!=u.Brake)switches++;lastT=u.Throttle;lastB=u.Brake;intervention=c.Snapshot.Intervention;arrival=c.Snapshot.Arrival;commands.Append($"{z.Tick},{requested},{cmd.Brake},{u.Throttle},{u.Brake}\n");
   if(i%25==0){var st=c.Snapshot;trace.Append(FormattableString.Invariant($"{z.Tick-start},{z.PositionM-task.Origin:R},{z.SpeedMps:R},{z.Acceleration:R},{u.Throttle},{u.Brake},{st.Coast:R},{st.BrakeGain:R},{st.PredictedArrival:R}\n"));}}
  m.Step(u);if(m.State.SpeedMps>task.LimitAt(m.State.PositionM)+.15)violationTicks++;jerk+=Math.Pow((m.State.Acceleration-lastAccel)/.02,2)*.02;lastAccel=m.State.Acceleration;
  if(arrival>=0&&Math.Abs(m.State.SpeedMps)<.01)dwell++;else dwell=0;if(dwell>=100||intervention&&Math.Abs(m.State.SpeedMps)<.01){timeout=false;break;}
 }
 double error=m.State.PositionM-task.Target;string id=$"{seed}-{scenario}-{mode}-{variant}-r{repeat}";string final=SimulationCodec.Encode(m.State);string fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(final+commands)));
 rows.Add(new{id,seed,scenario,mode=mode.ToString(),variant,repeat,error,arrival,success=!intervention&&!timeout&&violationTicks==0&&Math.Abs(error)<=5&&arrival>=115&&arrival<=125,intervention,timeout,violationTicks,switches,jerk,air=m.State.ExhaustAirKpaL-air,fuel=m.State.FuelUsedL-fuel,fingerprint});
 File.WriteAllText(Path.Combine(output,id+"-trace.csv"),trace.ToString());Console.WriteLine(id+" arrival="+arrival+" intervention="+intervention);
}
File.WriteAllText(Path.Combine(output,variant+"-results.json"),JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
