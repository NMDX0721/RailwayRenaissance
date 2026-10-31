using System;
using System.Collections.Generic;
using RailwayRenaissance.Cab.Research;
using RailwayRenaissance.Cab.Simulation;
static class V3Replay
{
 static void Step(CabSimulation m,SimulationInput u,CabAtoControllerV3 c,List<string> trace=null)
 {
  if(m.State.Tick%5==0){var s=m.State;var command=c.Decide(new AtoObservation{Tick=s.Tick,Position=s.PositionM,Speed=s.SpeedMps,Acceleration=s.Acceleration,PipeKpa=s.PipeKpa,ControlKpa=s.ControlKpa,CylinderKpa=s.TrainCylinderKpa},u.Throttle,u.Brake);u.Throttle=command.Throttle;u.Brake=command.Brake;if(s.TrainCylinderKpa>=20||s.PipeKpa<470||u.Brake>=2)u.Throttle=0;trace?.Add($"{s.Tick},{command.Throttle},{command.Brake},{u.Throttle},{u.Brake},{command.Reason}");}
  m.Step(u);
 }
 public static void Check()
 {
  var probe=new CabSimulation(1100);var probeInput=new SimulationInput{Master=true,FuelPump=true,EngineRun=true,Brake=1};for(int i=0;i<15000;i++)probe.Step(probeInput);probeInput.ParkingBrake=false;probeInput.Direction=1;
  var probeController=new CabAtoControllerV3(AtoRouteTask.Standard(0),probe.State.Tick,AtoObjective.Punctual);int brakingCapture=-1,slowCapture=-1;
  for(int i=0;i<9000;i++){Step(probe,probeInput,probeController);if(slowCapture<0&&probe.State.Acceleration<-.005&&probeInput.Throttle==0&&probe.State.SpeedMps>1&&probe.State.PositionM>250&&probe.State.PositionM<650)slowCapture=i+1;if(probeController.Snapshot.LastBrake>=2&&probe.State.SpeedMps>1&&probe.State.PositionM>650){brakingCapture=i+1;break;}}
  if(brakingCapture<0||slowCapture<0)throw new Exception("No deceleration/brake phase captured");
  foreach(int capture in new[]{500,slowCapture,brakingCapture,6500}){
   var m=new CabSimulation(1100);var u=new SimulationInput{Master=true,FuelPump=true,EngineRun=true,Brake=1};for(int i=0;i<15000;i++)m.Step(u);u.ParkingBrake=false;u.Direction=1;
   var task=AtoRouteTask.Standard(0);var c=new CabAtoControllerV3(task,m.State.Tick,AtoObjective.Punctual);for(int i=0;i<capture;i++)Step(m,u,c);
   var other=new CabSimulation(1,SimulationCodec.Decode<SimulationConfig>(SimulationCodec.Encode(m.Config)));other.Restore(SimulationCodec.Decode<SimulationState>(SimulationCodec.Encode(m.State)));
   var input=SimulationCodec.Decode<SimulationInput>(SimulationCodec.Encode(u));var restored=CabAtoControllerV3.Restore(AtoRouteTask.Decode(task.Encode()),SimulationCodec.Decode<AtoV3State>(SimulationCodec.Encode(c.Snapshot)),m.State.Tick);
   Console.WriteLine($"V3_CAPTURE tick={capture} brake={c.Snapshot.LastBrake} arrival={c.Snapshot.Arrival:F2}");
   var leftTrace=new List<string>();var rightTrace=new List<string>();
   for(int i=0;i<300;i++){Step(m,u,c,leftTrace);Step(other,input,restored,rightTrace);}
   if(SimulationCodec.Encode(m.State)!=SimulationCodec.Encode(other.State)||SimulationCodec.Encode(c.Snapshot)!=SimulationCodec.Encode(restored.Snapshot))throw new Exception("V3 replay diverged");
   int dwell=0;for(int i=0;i<18000;i++){Step(m,u,c,leftTrace);Step(other,input,restored,rightTrace);if(c.Snapshot.Arrival>=0&&Math.Abs(m.State.SpeedMps)<.01)dwell++;else dwell=0;if(dwell>=100)break;}
   if(dwell<100||string.Join("|",leftTrace)!=string.Join("|",rightTrace)||SimulationCodec.Encode(m.State)!=SimulationCodec.Encode(other.State)||SimulationCodec.Encode(c.Snapshot)!=SimulationCodec.Encode(restored.Snapshot))throw new Exception("V3 final replay diverged");
  }
  string expected=null;
  foreach(int fps in new[]{30,60,144}){
   var model=new CabSimulation(1100);var input=new SimulationInput{Master=true,FuelPump=true,EngineRun=true,Brake=1};for(int i=0;i<15000;i++)model.Step(input);input.ParkingBrake=false;input.Direction=1;
   var controller=new CabAtoControllerV3(AtoRouteTask.Standard(0),model.State.Tick,AtoObjective.Smooth);long start=model.State.Tick;var clock=new SimulationClock();bool switched=false;var trace=new List<string>();
   for(int frame=0;frame<fps*130;frame++)clock.Advance(1.0/fps,()=>{if(!switched&&model.State.Tick-start==1000){controller.RequestMode(AtoObjective.Punctual);switched=true;}Step(model,input,controller,trace);});
   string actual=SimulationCodec.Encode(model.State)+SimulationCodec.Encode(controller.Snapshot)+string.Join("|",trace);if(expected==null)expected=actual;else if(actual!=expected)throw new Exception("FPS scheduling diverged");
  }
  var route=AtoRouteTask.Standard(0);var controllerCheck=new CabAtoControllerV3(route,0,AtoObjective.Smooth);
  var overshoot=controllerCheck.Decide(new AtoObservation{Tick=0,Position=906,Speed=5,PipeKpa=500,ControlKpa=500},0,1);
  if(!controllerCheck.Snapshot.Intervention||overshoot.Throttle!=0||overshoot.Brake!=3)throw new Exception("Overshoot did not trigger safe exit");
  controllerCheck=new CabAtoControllerV3(route,0,AtoObjective.Smooth);
  var impossible=controllerCheck.Decide(new AtoObservation{Tick=0,Position=899,Speed=9,PipeKpa=500,ControlKpa=500},0,1);
  if(!controllerCheck.Snapshot.Intervention||impossible.Throttle!=0||impossible.Brake!=3)throw new Exception("No feasible candidate did not exit");
  controllerCheck=new CabAtoControllerV3(route,0,AtoObjective.Smooth);
  var late=controllerCheck.Decide(new AtoObservation{Tick=6500,Position=500,Speed=8,PipeKpa=500,ControlKpa=500},0,1);
  if(controllerCheck.Snapshot.Intervention||!late.Reason.Contains("预计晚点"))throw new Exception("Late but safe case did not continue with warning");
  var bad=controllerCheck.Snapshot;bad.BrakeGain=double.NaN;bool rejected=false;try{CabAtoControllerV3.Restore(route,bad,6500);}catch(ArgumentException){rejected=true;}if(!rejected)throw new Exception("Invalid V3 state accepted");
  Console.WriteLine("V3_REPLAY_OK: four capture points replay to confirmed stop; mode switch at 30/60/144 FPS; overshoot, infeasible, late and corrupt-state guards");
 }
}
