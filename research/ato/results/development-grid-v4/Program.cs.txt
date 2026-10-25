using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using RailwayRenaissance.Cab.Simulation;
using RailwayRenaissance.Cab.Research;

static class Program
{
    static string F(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
    static AtoObservation Observe(SimulationState s)=>new AtoObservation{Tick=s.Tick,Position=s.PositionM,Speed=s.SpeedMps,Acceleration=s.Acceleration,PipeKpa=s.PipeKpa,ControlKpa=s.ControlKpa,CylinderKpa=s.TrainCylinderKpa};
    static void Require(bool ok,string why){if(!ok)throw new Exception(why);}
    static SimulationState Ready(int seed)
    {
        var m=new CabSimulation(seed);var u=new SimulationInput{Master=true,FuelPump=true,EngineRun=true,Brake=1};
        for(int i=0;i<15000;i++)m.Step(u);
        Require(m.State.Running&&m.State.PipeKpa>490,"Startup preparation failed");return m.State.Copy();
    }
    sealed class Result
    {
        public string method {get;set;} public int seed {get;set;}
        public double mass {get;set;} public double grip {get;set;} public double grade {get;set;}
        public bool stopped {get;set;} public bool reachedTolerance {get;set;}
        public double errorM {get;set;} public double seconds {get;set;} public int switches {get;set;}
        public double decisionMaxMs {get;set;} public double decisionP95Ms {get;set;}
        public string finalState {get;set;}
        public string initialState {get;set;} public string configuration {get;set;}
    }
    static Result Run(AtoMethod method,int seed,double mass,double grip,double grade,string trace=null,int frame=1)
    {
        var start=Ready(seed);start.TrainMassKg*=mass;
        var m=new CabSimulation(seed);m.Restore(start);
        double target=start.PositionM+900;var controller=new CabAtoController(method,target,16);
        var u=new SimulationInput{Master=true,FuelPump=true,EngineRun=true,ParkingBrake=false,Direction=1,Brake=1,RailGrip=grip,GradePermille=grade};
        using var writer=trace==null?null:new StreamWriter(trace);
        writer?.WriteLine("tick,time_s,position_m,target_m,speed_mps,pipe_kpa,cylinder_kpa,throttle,brake,requested_throttle,estimated_decel,plans");
        int stopped=0,switches=0,last=1,ticks=0;bool didStop=false;var times=new List<double>();
        while(ticks<15000&&!didStop)
        {
            for(int f=0;f<frame&&ticks<15000&&!didStop;f++)
            {
                int requested=u.Throttle;
                if(ticks%5==0)
                {
                    var watch=Stopwatch.StartNew();var command=controller.Decide(Observe(m.State));watch.Stop();times.Add(watch.Elapsed.TotalMilliseconds);
                    requested=command.Throttle;u.Brake=command.Brake;
                    // Match the production facade's traction availability guards.
                    u.Throttle=requested>0&&(m.State.PipeKpa<470||m.State.MainKpa<600||m.State.TrainCylinderKpa>=20||u.Brake>=2)?0:requested;
                    if(u.Brake!=last)switches++;last=u.Brake;
                }
                m.Step(u);ticks++;
                if(ticks%5==0)writer?.WriteLine(string.Join(",",m.State.Tick,F(m.State.Time),F(m.State.PositionM),F(target),F(m.State.SpeedMps),F(m.State.PipeKpa),F(m.State.TrainCylinderKpa),u.Throttle,u.Brake,requested,F(controller.EstimatedDeceleration),controller.PlansLastUpdate));
                stopped=controller.BrakingPhase&&Math.Abs(m.State.SpeedMps)<.01?stopped+1:0;
                didStop=stopped>=100;
            }
        }
        times.Sort();double error=m.State.PositionM-target;
        return new Result{method=method.ToString(),seed=seed,mass=mass,grip=grip,grade=grade,stopped=didStop,reachedTolerance=didStop&&Math.Abs(error)<=5,errorM=error,seconds=ticks*.02,switches=switches,decisionMaxMs=times.Max(),decisionP95Ms=times[(int)((times.Count-1)*.95)],initialState=SimulationCodec.Encode(start),configuration=SimulationCodec.Encode(m.Config),finalState=SimulationCodec.Encode(m.State)};
    }
    static string Hash(string p)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)));
    static void Main(string[] args)
    {
        string dir=args.Length>0?args[0]:"research/ato/results/development-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        if(File.Exists(Path.Combine(dir,"results.json")))throw new IOException("Refusing to overwrite an existing experiment");
        Directory.CreateDirectory(dir);
        var results=new List<Result>();
        foreach(AtoMethod method in Enum.GetValues<AtoMethod>())
        {
            var a=Run(method,1000,1,1,0,Path.Combine(dir,method+".csv"));
            var b=Run(method,1000,1,1,0,null,7);
            Require(a.finalState==b.finalState,"Frame-group replay mismatch: "+method);
            results.Add(a);
            results.Add(Run(method,1001,1.2,.3,-10));
        }
        if(args.Length>1&&args[1]=="--grid")
        {
            results.Clear();
            foreach(AtoMethod method in Enum.GetValues<AtoMethod>())foreach(int seed in new[]{1000,1001})
            foreach(double mass in new[]{.8,1.0,1.2})foreach(double grip in new[]{.3,.55,1.0})foreach(double grade in new[]{-10.0,0,10.0})
                results.Add(Run(method,seed,mass,grip,grade,Path.Combine(dir,$"{method}-{seed}-{F(mass)}-{F(grip)}-{F(grade)}.csv")));
        }
        bool invalid=false;try{new CabAtoController(AtoMethod.Predictive,1,0);}catch(ArgumentException){invalid=true;}
        Require(invalid,"Invalid task accepted");
        var controller=new CabAtoController(AtoMethod.Predictive,100,16);var o=new AtoObservation{Tick=1};controller.Decide(o);
        invalid=false;try{controller.Decide(o);}catch(ArgumentException){invalid=true;}Require(invalid,"Duplicate observation accepted");
        var overshoot=new CabAtoController(AtoMethod.Predictive,100,16).Decide(new AtoObservation{Tick=1,Position=101,Speed=4});
        Require(overshoot.Throttle==0&&overshoot.Brake==3,"Overshoot not braked");
        File.WriteAllText(Path.Combine(dir,"results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
        foreach(string source in new[]{"Assets/Scripts/Cab/CabSimulation.cs","Assets/Scripts/Cab/CabSimulationCodec.cs","Assets/Scripts/Cab/Research/CabAtoController.cs","research/ato/Program.cs","research/ato/AtoResearch.csproj"})
            File.Copy(source,Path.Combine(dir,Path.GetFileName(source)+".txt"),false);
        File.WriteAllText(Path.Combine(dir,"manifest.json"),JsonSerializer.Serialize(new{scope="Development only. No held-out test or real-train claim.",utc=DateTime.UtcNow,sourceHash=Hash("Assets/Scripts/Cab/CabSimulation.cs"),controllerHash=Hash("Assets/Scripts/Cab/Research/CabAtoController.cs"),runtime=Environment.Version.ToString(),controlTicks=5,stepSeconds=.02,seedSet=new[]{1000,1001},targetRelativeM=900,speedLimitMps=16,stopSpeedMps=.01,stopHoldTicks=100,toleranceM=5,replayFrameGrouping=true},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("ATO development checks passed: replay, invalid inputs, overshoot guard.");
        foreach(var group in results.GroupBy(r=>r.method))Console.WriteLine($"{group.Key} n={group.Count()} stopped={group.Count(r=>r.stopped)} within5m={group.Count(r=>r.reachedTolerance)} meanAbsError={group.Average(r=>Math.Abs(r.errorM)):F2}m meanSwitches={group.Average(r=>r.switches):F1} maxDecision={group.Max(r=>r.decisionMaxMs):F2}ms");
    }
}
