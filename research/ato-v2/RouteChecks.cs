using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using RailwayRenaissance.Cab.Simulation;
using RailwayRenaissance.Cab.Research;
static partial class Program
{
    static void RouteExperiments()
    {
        var results=new List<object>();
        foreach(AtoObjective mode in Enum.GetValues(typeof(AtoObjective)))foreach(bool adaptive in new[]{false,true})
        {
            var(m,u)=Prepared();long start=m.State.Tick;double fuel=m.State.FuelUsedL,air=m.State.ExhaustAirKpaL;
            var c=new CabAtoControllerV2(AtoRouteTask.Standard(m.State.PositionM),start,mode,adaptive);
            int switches=0,violations=0,oldT=0,oldB=1;double jerk=0,pa=0;bool intervention=false;double arrival=-1;
            for(int k=0;k<15000;k++)
            {
                if(k%5==0){var cmd=c.Decide(Observe(m.State));u.Brake=cmd.Brake;u.Throttle=cmd.Throttle;
                    if(m.State.TrainCylinderKpa>=20||m.State.PipeKpa<470||u.Brake>=2)u.Throttle=0;
                    if(oldT!=u.Throttle||oldB!=u.Brake)switches++;oldT=u.Throttle;oldB=u.Brake;
                    if(c.Intervention){if(!intervention)Console.WriteLine($"FAIL at {m.State.PositionM} v={m.State.SpeedMps} {cmd.Reason}");intervention=true;}
                }
                m.Step(u);jerk+=Math.Pow((m.State.Acceleration-pa)/.02,2)*.02;pa=m.State.Acceleration;
                if(m.State.SpeedMps>c.Task.LimitAt(m.State.PositionM)+.15)violations++;
                if(c.Arrived){arrival=c.Snapshot.ArrivalSeconds;break;}
                if(intervention&&m.State.SpeedMps<.01)break;
            }
            var r=new{mode=mode.ToString(),adaptive,stopError=m.State.PositionM-c.Task.Target,arrival,timeError=arrival<0?(double?)null:arrival-120,switches,jerk,air=m.State.ExhaustAirKpaL-air,fuel=m.State.FuelUsedL-fuel,violations,intervention};results.Add(r);Console.WriteLine(JsonSerializer.Serialize(r));
        }
        File.WriteAllText("research/ato-v2/development_results.json",JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
    }
}
