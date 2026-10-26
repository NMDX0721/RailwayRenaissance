using System;
using System.Collections.Generic;
using RailwayRenaissance.Cab.Simulation;
using RailwayRenaissance.Cab.Research;
static partial class Program
{
    static void V2Step(CabSimulation m,SimulationInput u,CabAtoControllerV2 c,List<string> trace=null)
    {
        if((m.State.Tick-c.Snapshot.StartTick)%5==0)
        {
            var cmd=c.Decide(Observe(m.State));u.Brake=cmd.Brake;u.Throttle=cmd.Throttle;
            if(m.State.TrainCylinderKpa>=20||m.State.PipeKpa<470||u.Brake>=2)u.Throttle=0;
            trace?.Add($"{m.State.Tick}:{u.Throttle}:{u.Brake}:{cmd.Reason}");
        }
        m.Step(u);
    }
    static void ReplayV2(CabSimulation m,SimulationInput u,CabAtoControllerV2 c)
    {
        var other=new CabSimulation(1,SimulationCodec.Decode<SimulationConfig>(SimulationCodec.Encode(m.Config)));
        other.Restore(SimulationCodec.Decode<SimulationState>(SimulationCodec.Encode(m.State)));
        var v=SimulationCodec.Decode<SimulationInput>(SimulationCodec.Encode(u));
        var restored=CabAtoControllerV2.Restore(AtoRouteTask.Decode(c.Task.Encode()),SimulationCodec.Decode<AtoV2State>(SimulationCodec.Encode(c.Snapshot)),m.State.Tick);
        var a=new List<string>();var b=new List<string>();
        for(int k=0;k<250;k++){V2Step(m,u,c,a);V2Step(other,v,restored,b);}
        Assert(string.Join("|",a)==string.Join("|",b),"V2 command replay diverged");
        Assert(SimulationCodec.Encode(m.State)==SimulationCodec.Encode(other.State),"V2 physical replay diverged");
        Assert(SimulationCodec.Encode(c.Snapshot)==SimulationCodec.Encode(restored.Snapshot),"V2 state replay diverged");
        // Continue independent copies to the final stopped state without skipping phases in the probe run.
        var left=new CabSimulation(1,SimulationCodec.Decode<SimulationConfig>(SimulationCodec.Encode(m.Config)));left.Restore(SimulationCodec.Decode<SimulationState>(SimulationCodec.Encode(m.State)));
        var leftInput=SimulationCodec.Decode<SimulationInput>(SimulationCodec.Encode(u));var leftController=CabAtoControllerV2.Restore(c.Task,c.Snapshot,m.State.Tick);
        int dwell=0;
        for(int k=0;k<18000;k++)
        {
            V2Step(left,leftInput,leftController,a);V2Step(other,v,restored,b);
            if(leftController.Arrived&&Math.Abs(left.State.SpeedMps)<.01)dwell++;else dwell=0;
            if(dwell>=100)break;
        }
        Assert(dwell>=100,"Replay failed to reach final stop");
        Assert(string.Join("|",a)==string.Join("|",b)&&SimulationCodec.Encode(left.State)==SimulationCodec.Encode(other.State),"Replay final state/commands diverged");
    }
    static void CheckV2()
    {
        var(m,u)=Prepared();var c=new CabAtoControllerV2(AtoRouteTask.Standard(0),m.State.Tick,AtoObjective.Smooth);
        bool acceleration=false,sampling=false,braking=false,dwell=false;
        for(int i=0;i<15000;i++)
        {
            var snapshot=c.Snapshot;bool save=false;
            if(!acceleration&&i>=500){acceleration=true;save=true;}
            if(!sampling&&snapshot.Calibration>0){sampling=true;save=true;}
            if(!braking&&snapshot.Braking&&snapshot.Calibration==0){braking=true;save=true;}
            if(!dwell&&c.Arrived){dwell=true;save=true;}
            if(save){Console.WriteLine($"V2 replay phase at {m.State.Tick} calibration={snapshot.Calibration} braking={snapshot.Braking} arrived={c.Arrived}");ReplayV2(m,u,c);}
            if(dwell)break;V2Step(m,u,c);
        }
        Assert(acceleration&&sampling&&braking&&dwell,"Missing V2 phase checkpoint");
        string reference=null;
        foreach(int fps in new[]{30,60,144})
        {
            (m,u)=Prepared();c=new CabAtoControllerV2(AtoRouteTask.Standard(0),m.State.Tick,AtoObjective.Smooth);long start=m.State.Tick;
            var clock=new SimulationClock();var trace=new List<string>();bool switched=false;
            for(int frame=0;frame<fps*150;frame++)clock.Advance(1.0/fps,()=>
            {
                if(!switched&&m.State.Tick-start==1000){var before=c.Snapshot;c.RequestMode(AtoObjective.Punctual);var after=c.Snapshot;Assert(after.Mode==before.Mode&&after.Gain==before.Gain&&after.LastSwitch==before.LastSwitch,"Mode switch reset state");switched=true;}
                V2Step(m,u,c,trace);
            });
            string outcome=SimulationCodec.Encode(m.State)+"|"+string.Join("|",trace);
            if(reference==null)reference=outcome;else Assert(reference==outcome,"FPS altered physics or commands");
        }
        var task=AtoRouteTask.Standard(0);
        c=new CabAtoControllerV2(task,0,AtoObjective.Smooth);
        var fail=c.Decide(new AtoObservation{Tick=0,Position=906,Speed=10,PipeKpa=500,ControlKpa=500});Assert(c.Intervention&&fail.Brake==3&&fail.Throttle==0,"Overshoot fallback failed");
        c=new CabAtoControllerV2(task,0,AtoObjective.Smooth);
        fail=c.Decide(new AtoObservation{Tick=0,Position=100,Speed=29,PipeKpa=500,ControlKpa=500});Assert(c.Intervention&&fail.Brake==3&&fail.Throttle==0,"No feasible fallback failed");
        c=CabAtoControllerV2.Restore(task,new AtoV2State{StartTick=0,LastTick=0,Braking=true},5);
        fail=c.Decide(new AtoObservation{Tick=5,Position=899,Speed=9,PipeKpa=500,ControlKpa=500});Assert(c.Intervention&&c.Snapshot.Feasible==0&&fail.Brake==3&&fail.Throttle==0,"Empty candidate set did not exit");
        c=new CabAtoControllerV2(task,0,AtoObjective.Smooth);
        var lower=c.Decide(new AtoObservation{Tick=0,Position=600,Speed=11,PipeKpa=500,ControlKpa=500});
        Assert(!c.Intervention&&lower.Throttle==0&&lower.Brake>=2,"Lower segment was not anticipated before 650m");
        bool invalid=false;try{CabAtoControllerV2.Restore(task,new AtoV2State{Gain=double.NaN},100);}catch(ArgumentException){invalid=true;}Assert(invalid,"Corrupt V2 state accepted");
        Console.WriteLine("V2 four-phase exact replay, mode preservation, 30/60/144 FPS, overshoot, infeasibility and corruption checks passed.");
    }
}
