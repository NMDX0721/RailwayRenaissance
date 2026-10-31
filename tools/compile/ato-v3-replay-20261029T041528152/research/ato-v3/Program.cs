using System;
using RailwayRenaissance.Cab.Simulation;
using RailwayRenaissance.Cab.Research;
if(Array.IndexOf(args,"--freeze-v2")>=0){FrozenV3.Freeze(2);return;}
if(Array.IndexOf(args,"--test-v2")>=0){FrozenV3.Run(2);return;}
if(Array.IndexOf(args,"--freeze")>=0){FrozenV3.Freeze();return;}
if(Array.IndexOf(args,"--test")>=0){FrozenV3.Run();return;}
var task=AtoRouteTask.Standard(37);var p=AtoTrajectoryPlanner.Plan(task,18,.4,.45);
for(int i=0;i<p.Position.Length;i++){
 if(p.Speed[i]>task.LimitAt(p.Position[i])+1e-9)throw new Exception("limit");
 if(i>0&&(p.Time[i]<=p.Time[i-1]||p.Speed[i]*p.Speed[i]-p.Speed[i-1]*p.Speed[i-1]>2*.4*(p.Position[i]-p.Position[i-1])+1e-8||p.Speed[i-1]*p.Speed[i-1]-p.Speed[i]*p.Speed[i]>2*.45*(p.Position[i]-p.Position[i-1])+1e-8))throw new Exception("envelope");
}
if(p.Speed[0]!=0||p.Speed[^1]!=0||p.Position[^1]!=task.Target)throw new Exception("terminal");
Console.WriteLine($"REFERENCE_CHECK_OK samples={p.Position.Length} nominal_seconds={p.PredictedArrival:F2}");

var timed=AtoTrajectoryPlanner.PlanForArrival(task,120,.4,.45);
if(Math.Abs(timed.PredictedArrival-120)>1e-6)throw new Exception("arrival target");
bool rejected=false;try{AtoTrajectoryPlanner.PlanForArrival(task,30,.4,.45);}catch(InvalidOperationException){rejected=true;}
if(!rejected)throw new Exception("infeasible arrival accepted");
var moving=AtoTrajectoryPlanner.PlanForArrival(task,120,.4,.45,8);
if(moving.Speed[0]!=8||Math.Abs(moving.PredictedArrival-120)>1e-6)throw new Exception("moving start reference");
Console.WriteLine($"ARRIVAL_REFERENCE_OK seconds={timed.PredictedArrival:F6}; infeasible target rejected");

foreach(AtoObjective mode in Enum.GetValues(typeof(AtoObjective)))
{
 var model=new CabSimulation(1100);var input=new SimulationInput{Master=true,FuelPump=true,EngineRun=true,Brake=1};
 for(int i=0;i<15000;i++)model.Step(input);input.ParkingBrake=false;input.Direction=1;
 var c=new CabAtoControllerV3(AtoRouteTask.Standard(model.State.PositionM),model.State.Tick,mode);int violations=0;double fuel=model.State.FuelUsedL,air=model.State.ExhaustAirKpaL;string reason="";
 for(int i=0;i<18000;i++){
  if(i%5==0){var z=model.State;var cmd=c.Decide(new AtoObservation{Tick=z.Tick,Position=z.PositionM,Speed=z.SpeedMps,Acceleration=z.Acceleration,PipeKpa=z.PipeKpa,ControlKpa=z.ControlKpa,CylinderKpa=z.TrainCylinderKpa},input.Throttle,input.Brake);input.Throttle=cmd.Throttle;input.Brake=cmd.Brake;if(z.TrainCylinderKpa>=20||z.PipeKpa<470||input.Brake>=2)input.Throttle=0;reason=cmd.Reason;}
  model.Step(input);if(model.State.SpeedMps>AtoRouteTask.Standard(0).LimitAt(model.State.PositionM)+.15)violations++;
  if(c.Snapshot.Arrival>=0||c.Snapshot.Intervention&&model.State.SpeedMps<.01)break;
 }
 Console.WriteLine($"DEV mode={mode} error={model.State.PositionM-900:F2} arrival={c.Snapshot.Arrival:F2} violations={violations} intervention={c.Snapshot.Intervention} air={model.State.ExhaustAirKpaL-air:F1} fuel={model.State.FuelUsedL-fuel:F3} reason={reason}");
}
V3Replay.Check();
if(Array.IndexOf(args,"--pairs")>=0)DevPairs.Run();
