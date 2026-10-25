using System;
using System.IO;
using System.Text.Json;
using RailwayRenaissance.Cab.Simulation;
using RailwayRenaissance.Cab.Research;

static partial class Program
{
    static void Assert(bool ok,string why){if(!ok)throw new Exception(why);}
    static AtoObservation Observe(SimulationState s)=>new AtoObservation{Tick=s.Tick,Position=s.PositionM,Speed=s.SpeedMps,Acceleration=s.Acceleration,PipeKpa=s.PipeKpa,ControlKpa=s.ControlKpa,CylinderKpa=s.TrainCylinderKpa};
    static (CabSimulation,SimulationInput) Prepared()
    {
        var m=new CabSimulation(1100);var u=new SimulationInput{Master=true,FuelPump=true,EngineRun=true,Brake=1};
        for(int i=0;i<15000;i++)m.Step(u);
        u.ParkingBrake=false;u.Direction=1;return(m,u);
    }
    static void LegacyStep(CabSimulation m,SimulationInput u,CabAtoController c)
    {
        if(m.State.Tick%5==0){var cmd=c.Decide(Observe(m.State));u.Brake=cmd.Brake;u.Throttle=cmd.Throttle;
            if(m.State.TrainCylinderKpa>=20||m.State.PipeKpa<470||u.Brake>=2)u.Throttle=0;}
        m.Step(u);
    }
    static void CheckLegacyReplay()
    {
        int residenceAt=-1;
        var(probe,probeInput)=Prepared();var probeController=new CabAtoController(AtoMethod.AdaptivePredictive,900,16,180,.15,25);
        for(int i=1;i<=6000;i++)
        {
            LegacyStep(probe,probeInput,probeController);var point=CabAtoCheckpoint.Capture(probeController);
            if(point.Braking&&point.CalibrationUpdates==0&&point.Plans>0&&point.LastTick-point.LastSwitchTick<25){residenceAt=i;break;}
        }
        Assert(residenceAt>0,"No residence checkpoint found");
        foreach(int after in new[]{500,2700,3000,residenceAt})
        {
            var(m,u)=Prepared();var c=new CabAtoController(AtoMethod.AdaptivePredictive,900,16,180,.15,25);
            for(int i=0;i<after;i++)LegacyStep(m,u,c);
            var saved=CabAtoCheckpoint.Capture(c);
            Console.WriteLine($"capture={after} braking={saved.Braking} calibration={saved.CalibrationUpdates} switchAge={saved.LastTick-saved.LastSwitchTick}");
            var other=new CabSimulation(1,SimulationCodec.Decode<SimulationConfig>(SimulationCodec.Encode(m.Config)));
            other.Restore(SimulationCodec.Decode<SimulationState>(SimulationCodec.Encode(m.State)));
            var input=SimulationCodec.Decode<SimulationInput>(SimulationCodec.Encode(u));
            var restored=CabAtoCheckpoint.Restore(SimulationCodec.Decode<AtoLegacyState>(SimulationCodec.Encode(saved)));
            for(int i=0;i<600;i++){LegacyStep(m,u,c);LegacyStep(other,input,restored);}
            Assert(SimulationCodec.Encode(m.State)==SimulationCodec.Encode(other.State),"Legacy restore changed physics");
            Assert(SimulationCodec.Encode(CabAtoCheckpoint.Capture(c))==SimulationCodec.Encode(CabAtoCheckpoint.Capture(restored)),"Legacy restore changed controller");
        }
        bool invalid=false;try{CabAtoCheckpoint.Restore(new AtoLegacyState{Version=9});}catch(ArgumentException){invalid=true;}
        Assert(invalid,"Bad snapshot accepted");
    }
    static void Main(string[] args)
    {
        CheckLegacyReplay();Console.WriteLine("ATO exact V1 sidecar replay: four capture times passed.");
    }
}
