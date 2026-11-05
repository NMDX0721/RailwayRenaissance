using System;
using RailwayRenaissance.Cab.Research;
using RailwayRenaissance.Cab.Simulation;
static class DevPairs
{
 public static void Run()
 {
  foreach(string scenario in new[]{"nominal","weak_wet","strong_uphill"})foreach(AtoObjective mode in Enum.GetValues(typeof(AtoObjective)))foreach(bool v3 in new[]{false,true})
  {
   var cfg=new SimulationConfig();if(scenario=="weak_wet"){cfg.TrainMassKg*=1.08;cfg.ServiceBrakeN*=.83;cfg.LocoBrakeN*=.83;}if(scenario=="strong_uphill"){cfg.TrainMassKg*=.94;cfg.ServiceBrakeN*=1.17;cfg.LocoBrakeN*=1.17;}
   var m=new CabSimulation(1100,cfg);var u=new SimulationInput{Master=true,FuelPump=true,EngineRun=true,Brake=1};for(int i=0;i<15000;i++)m.Step(u);u.ParkingBrake=false;u.Direction=1;if(scenario=="weak_wet"){u.RailGrip=.55;u.GradePermille=-3;}if(scenario=="strong_uphill")u.GradePermille=3;
   var task=AtoRouteTask.Standard(0);var old=new CabAtoControllerV2(task,m.State.Tick,mode,true);var next=new CabAtoControllerV3(task,m.State.Tick,mode);double f=m.State.FuelUsedL,air=m.State.ExhaustAirKpaL,arrival=-1,jerk=0,prev=0;int violation=0,switches=0,lastT=0,lastB=1;bool intervention=false;
   for(int i=0;i<18000;i++){
    if(i%5==0&&!intervention){var z=m.State;var o=new AtoObservation{Tick=z.Tick,Position=z.PositionM,Speed=z.SpeedMps,Acceleration=z.Acceleration,PipeKpa=z.PipeKpa,ControlKpa=z.ControlKpa,CylinderKpa=z.TrainCylinderKpa};var command=v3?next.Decide(o,u.Throttle,u.Brake):old.Decide(o);u.Throttle=command.Throttle;u.Brake=command.Brake;if(z.TrainCylinderKpa>=20||z.PipeKpa<470||u.Brake>=2)u.Throttle=0;if(lastT!=u.Throttle||lastB!=u.Brake)switches++;lastT=u.Throttle;lastB=u.Brake;intervention=v3?next.Snapshot.Intervention:old.Intervention;arrival=v3?next.Snapshot.ArrivalSecondsCompat():old.Snapshot.ArrivalSeconds;}
    m.Step(u);if(m.State.SpeedMps>task.LimitAt(m.State.PositionM)+.15)violation++;jerk+=Math.Pow((m.State.Acceleration-prev)/.02,2)*.02;prev=m.State.Acceleration;
    if(arrival>=0||intervention&&Math.Abs(m.State.SpeedMps)<.01)break;
   }
   Console.WriteLine($"PAIR {scenario} {mode} {(v3?"V3":"V2")} error={m.State.PositionM-900:F2} arrival={arrival:F2} violationTicks={violation} intervention={intervention} switches={switches} jerk={jerk:F1} air={m.State.ExhaustAirKpaL-air:F1} fuel={m.State.FuelUsedL-f:F3}");
  }
 }
 static double ArrivalSecondsCompat(this AtoV3State state)=>state.Arrival;
}
