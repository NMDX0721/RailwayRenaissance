// AI-assisted audit probe only. No policy optimizer or production changes.
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Text.Json;
using RailwayRenaissance.Cab.Simulation;

static class Program
{
    static void Run(CabSimulation m, SimulationInput u, int ticks)
    { for (int i=0;i<ticks;i++) m.Step(u); }

    static SimulationState Moving()
    {
        var m=new CabSimulation(20261021);
        var u=new SimulationInput {Master=true,FuelPump=true,EngineRun=true,Brake=1};
        Run(m,u,15000);
        if(!m.State.Running || m.State.PipeKpa<490) throw new Exception("Preparation failed");
        u.ParkingBrake=false;u.Direction=1;u.Throttle=8;
        Run(m,u,3000);
        return m.State.Copy();
    }

    static object Stop(SimulationState snapshot, int brake, double grip, double massScale, double grade)
    {
        var m=new CabSimulation(snapshot.Seed);
        var s=snapshot.Copy();s.TrainMassKg*=massScale;m.Restore(s);
        var u=new SimulationInput {Master=true,FuelPump=true,EngineRun=true,ParkingBrake=false,Direction=1,Brake=brake,RailGrip=grip,GradePermille=grade};
        double start=s.PositionM;bool stopped=false;int held=0,i=0;
        for(;i<30000;i++)
        {
            m.Step(u);
            held=Math.Abs(m.State.SpeedMps)<.01?held+1:0;
            if(held>=100) {stopped=true;break;}
        }
        return new {brake,grip,massScale,grade,initialSpeedMps=s.SpeedMps,stopped,distanceM=m.State.PositionM-start,elapsedSeconds=(i+1)*.02,finalSpeedMps=m.State.SpeedMps};
    }

    static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    static void Main()
    {
        var s=Moving();var a=new CabSimulation(1);var b=new CabSimulation(2);a.Restore(s);b.Restore(s);
        var u=new SimulationInput {Master=true,FuelPump=true,EngineRun=true,ParkingBrake=false,Direction=1,Brake=3};
        Run(a,u,1000);Run(b,u,1000);
        bool replay=SimulationCodec.Encode(a.State)==SimulationCodec.Encode(b.State);
        if(!replay)throw new Exception("Checkpoint replay mismatch");
        var rows=new List<object>();
        foreach(int brake in new[]{2,3,4}) rows.Add(Stop(s,brake,1,1,0));
        foreach(double grip in new[]{.15,.3,1.0}) rows.Add(Stop(s,3,grip,1,0));
        foreach(double mass in new[]{.8,1.2}) rows.Add(Stop(s,3,1,mass,0));
        foreach(double grade in new[]{-10.0,10.0}) rows.Add(Stop(s,3,1,1,grade));
        Console.WriteLine(JsonSerializer.Serialize(new {scope="audit smoke checks, not a policy comparison or real vehicle validation",seed=s.Seed,stepSeconds=CabSimulation.StepSeconds,checkpointReplay=replay,sourceHash=Hash("Assets/Scripts/Cab/CabSimulation.cs"),codecHash=Hash("Assets/Scripts/Cab/CabSimulationCodec.cs"),runtime=Environment.Version.ToString(),cases=rows},new JsonSerializerOptions{WriteIndented=true}));
    }
}
