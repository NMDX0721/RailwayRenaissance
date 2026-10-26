using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;
using RailwayRenaissance.Cab.Simulation;

public static class NF5KSimulationReview
{
    const string ScenePath="Assets/Scenes/CabSimulationReview.unity";
    static void Assert(bool condition,string message) {if(!condition)throw new InvalidOperationException(message);}
    static void Run(CabSimulation model,SimulationInput input,double seconds)
    {for(int i=0;i<(int)Math.Round(seconds/CabSimulation.StepSeconds);i++)model.Step(input);}
    static SimulationInput Starting() {return new SimulationInput {Master=true,FuelPump=true,EngineRun=true,ParkingBrake=true,Brake=1};}
    static CabSimulation Ready()
    {
        var m=new CabSimulation(20261021);Run(m,Starting(),300);
        Assert(m.State.Running&&m.State.PipeKpa>490&&m.State.AuxKpa>480,"Cold start/air charging failed");return m;
    }
    [Serializable] public sealed class Result
    {
        public string scope="Candidate lumped diesel-electric model; not certified NF5K physics";
        public bool airConservation, coldStart, tractionCoastBrake, independentBrake, adhesion, gradeRollback, thermalTrip, checkpointReplay, frameRateReplay, invalidSnapshotRejected;
        public bool noAirNoBrake, lowAirTractionGuard, movingReverseGuard, batteryDrain, seededInitials;
        public double speedAfterAccelerationKmh, serviceStopM, emergencyStopM, maxAirBalanceError;
    }
    public static Result Validate()
    {
        var result=new Result();var cold=new CabSimulation(20261021);var input=Starting();input.Master=false;Run(cold,input,10);
        Assert(!cold.State.Running&&cold.State.Rpm==0,"Engine started without master");input.Master=true;Run(cold,input,2);Assert(!cold.State.Running&&cold.State.Cranking,"Crank delay was bypassed");
        double initial=cold.StoredAirKpaL;double generated=cold.State.CompressorAirKpaL,exhaust=cold.State.ExhaustAirKpaL;
        for(int i=0;i<15000;i++)
        {
            cold.Step(input);double error=Math.Abs(cold.StoredAirKpaL-initial-(cold.State.CompressorAirKpaL-generated)+(cold.State.ExhaustAirKpaL-exhaust));
            result.maxAirBalanceError=Math.Max(result.maxAirBalanceError,error);
        }
        Assert(result.maxAirBalanceError<1e-4,"Air mass proxy is not conserved");result.airConservation=true;result.coldStart=true;
        Assert(SimulationCodec.Encode(new CabSimulation(7).State)==SimulationCodec.Encode(new CabSimulation(7).State)&&new CabSimulation(7).State.FuelL!=new CabSimulation(8).State.FuelL,"Seeded initial conditions invalid");result.seededInitials=true;
        var starved=new CabSimulation(5);var starvedInput=new SimulationInput {Brake=4,ParkingBrake=false};Run(starved,starvedInput,10);
        Assert(starved.State.TrainCylinderKpa==0&&starved.State.LocoCylinderKpa==0&&starved.State.BrakeN==0,"Air brake created pressure from empty tanks");result.noAirNoBrake=true;
        var low=Ready();var lowState=low.State.Copy();lowState.MainKpa=50;low.Restore(lowState);var lowInput=Starting();lowInput.Direction=1;lowInput.Throttle=8;lowInput.ParkingBrake=false;Run(low,lowInput,2);
        Assert(low.State.TractionN==0,"Low main pressure did not inhibit traction");result.lowAirTractionGuard=true;
        var battery=new CabSimulation(1);double batteryAt=battery.State.Battery;Run(battery,new SimulationInput{Master=true},60);
        Assert(battery.State.Battery<batteryAt,"Auxiliaries did not drain battery");result.batteryDrain=true;
        var model=Ready();input=Starting();input.Direction=1;input.Throttle=8;input.ParkingBrake=false;Run(model,input,60);
        result.speedAfterAccelerationKmh=model.State.SpeedMps*3.6;Assert(result.speedAfterAccelerationKmh>10,"Traction did not accelerate");
        input.Direction=-1;model.Step(input);Assert(model.State.ActiveDirection==1&&model.State.TractionN>0,"Moving reverser accepted opposite direction");input.Direction=1;result.movingReverseGuard=true;
        double fuel=model.State.FuelUsedL;input.Throttle=0;Run(model,input,15);Assert(model.State.TractionN==0&&model.State.FuelUsedL>fuel,"Coasting or idle fuel invalid");
        var moving=model.State.Copy();
        result.serviceStopM=StopDistance(moving,3);result.emergencyStopM=StopDistance(moving,4);
        Assert(result.emergencyStopM<result.serviceStopM&&result.emergencyStopM>0,"Emergency/service stopping relationship invalid");result.tractionCoastBrake=true;
        model=Ready();input=Starting();input.IndependentBrake=1;Run(model,input,10);
        Assert(model.State.LocoCylinderKpa>350&&model.State.TrainCylinderKpa<5&&model.State.PipeKpa>480,"Independent brake incorrectly applies the whole train");result.independentBrake=true;
        var dry=Ready();input=Starting();input.Direction=1;dry.Step(input);var wet=new CabSimulation(1);wet.Restore(dry.State);input.Throttle=8;input.ParkingBrake=false;Run(dry,input,20);
        input.RailGrip=.3;Run(wet,input,20);Assert(wet.State.AdhesionLimited&&wet.State.SpeedMps<dry.State.SpeedMps,"Adhesion limit ineffective");result.adhesion=true;
        model=new CabSimulation(13);input=new SimulationInput {ParkingBrake=false,GradePermille=20};Run(model,input,10);
        Assert(model.State.SpeedMps<0,"Unbraked train cannot roll downhill");input.ParkingBrake=true;Run(model,input,20);Assert(model.State.SpeedMps==0,"Parking brake cannot hold grade");result.gradeRollback=true;
        model=Ready();var hot=model.State.Copy();hot.CoolantC=116;model.Restore(hot);input=Starting();model.Step(input);
        Assert(model.State.ThermalTrip&&!model.State.Running&&model.State.TractionN==0&&!model.ResetThermalTrip(),"Thermal trip/reset invalid");result.thermalTrip=true;
        model=Ready();input=Starting();input.Direction=1;input.Throttle=5;input.ParkingBrake=false;Run(model,input,25);
        var clone=new CabSimulation(99);clone.Restore(SimulationCodec.Decode<SimulationState>(SimulationCodec.Encode(model.State)));
        Run(model,input,30);Run(clone,input,30);Assert(SimulationCodec.Encode(model.State)==SimulationCodec.Encode(clone.State),"Checkpoint replay diverged");result.checkpointReplay=true;
        var a=Replay(30);var b=Replay(144);
        if(SimulationCodec.Encode(a.State)!=SimulationCodec.Encode(b.State))Debug.LogError("Replay mismatch ticks/speed: "+a.State.Tick+"/"+b.State.Tick+"; "+a.State.SpeedMps+"/"+b.State.SpeedMps);
        Assert(a.State.SpeedMps>1&&SimulationCodec.Encode(a.State)==SimulationCodec.Encode(b.State),"30/144 FPS fixed-step replay diverged");result.frameRateReplay=true;
        var bad=model.State.Copy();bad.MainKpa=-1;bool rejected=false;try{model.Restore(bad);}catch(ArgumentException){rejected=true;}Assert(rejected,"Negative-pressure snapshot accepted");result.invalidSnapshotRejected=true;
        return result;
    }
    static CabSimulation Replay(int fps)
    {
        var m=new CabSimulation(7);var u=Starting();var clock=new SimulationClock();double time=0;
        while(time<200-1e-9)
        {
            double dt=Math.Min(1.0/fps,200-time);
            clock.Advance(dt,()=>{u.Direction=m.State.Tick<400?0:1;u.Throttle=m.State.Tick<7500?0:4;u.ParkingBrake=m.State.Tick<7500;m.Step(u);});time+=dt;
        }
        return m;
    }
    static double StopDistance(SimulationState start,int brake)
    {
        var m=new CabSimulation(start.Seed);m.Restore(start);var u=Starting();u.ParkingBrake=false;u.Direction=1;u.Brake=brake;
        double at=start.PositionM;
        for(int i=0;i<30000&&m.State.SpeedMps>0;i++)m.Step(u);
        Assert(m.State.SpeedMps==0,"Braking failed to stop");return m.State.PositionM-at;
    }
    [MenuItem("RailRevival/Cab/Prepare Simulation Backend Review")]
    public static void Prepare()
    {
        if(!File.Exists(NF5KDaylightReview.ScenePath))NF5KDaylightReview.Prepare();
        EditorSceneManager.OpenScene(NF5KDaylightReview.ScenePath);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),ScenePath,true);
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var state=UnityEngine.Object.FindFirstObjectByType<CabEngineState>();
        var driver=state.gameObject.AddComponent<CabSimulationDriver>();driver.ConfigureReviewSeed(20261021);
        NF5KDrivingInteractionSetup.Configure(state);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("NF5K_SIMULATION_SCENE_OK: opt-in backend, existing gauge bindings retained, cold seeded review scenario");
    }
    public static void BatchBuild()
    {
        try
        {
            var result=Validate();Directory.CreateDirectory("tools/compile");File.WriteAllText("tools/compile/cab_simulation_acceptance.json",JsonUtility.ToJson(result,true));
            Debug.Log("NF5K_SIMULATION_KERNEL_OK: "+JsonUtility.ToJson(result));Prepare();
            NF5KSimulationPlayReview.Begin();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}

[InitializeOnLoad] public static class NF5KSimulationPlayReview
{
    const string Pending="NF5KSimulationPlayPending",Failed="NF5KSimulationPlayFailed";static int frames;
    static NF5KSimulationPlayReview(){EditorApplication.playModeStateChanged+=Changed;}
    public static void Begin(){SessionState.SetBool(Pending,true);SessionState.SetBool(Failed,false);EditorApplication.isPlaying=true;}
    static void Changed(PlayModeStateChange change)
    {
        if(!SessionState.GetBool(Pending,false))return;
        if(change==PlayModeStateChange.EnteredPlayMode){frames=0;EditorApplication.update+=Check;}
        if(change==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Pending,false);EditorApplication.Exit(SessionState.GetBool(Failed,false)?1:0);}
    }
    static void Check()
    {
        if(++frames<10)return;
        try
        {
            var driver=UnityEngine.Object.FindFirstObjectByType<CabSimulationDriver>();var state=driver.GetComponent<CabEngineState>();
            driver.EnsureInitialized();var controls=state.GetComponentInChildren<CabStartupControls>();
            var targets=state.GetComponentsInChildren<CabDrivingTarget>();
            Assert(targets.Length==4,"Driving target bindings missing");
            CabDrivingTarget Driving(CabDrivingAction action)=>Array.Find(targets,t=>t.Action==action);
            var throttle=Driving(CabDrivingAction.Throttle);
            Assert(!throttle.Activate()&&state.ThrottleNotch==0,"Cold traction accepted");
            Assert(controls.InsertKey()&&controls.SetMaster(true)&&controls.SetFuelPump(true)&&controls.TryStartEngine(),"Physical startup chain failed");
            driver.Advance(6);Assert(driver.Model.State.Running&&state.IsEngineRunning,"Adapter did not expose running engine");
            driver.Advance(300);Assert(state.BrakePipeKpa>490,"Adapter pipe charging failed");
            var lights=UnityEngine.Object.FindFirstObjectByType<CabInteriorLighting>();
            Assert(lights.MainPower&&lights.BatteryPower,"Backend generator/battery supply not projected to lighting");
            Assert(ClickDriving(state,Driving(CabDrivingAction.Reverser)),"Physical reverser rejected ready engine");
            state.SetBrakeNotch(BrakeNotch.Run);driver.ParkingBrake=false;
            for(int i=0;i<6;i++)Assert(ClickDriving(state,throttle),"Physical throttle failed");
            throttle.ApplyVisual();Assert(state.ThrottleNotch==6,"Throttle detents invalid");driver.Advance(30);
            Assert(state.CurrentSpeedKmh>5&&state.EngineRpm>600&&state.TractionCurrentAmps>0,"Existing facade/gauge outputs not driven");
            var railMotion=UnityEngine.Object.FindFirstObjectByType<CabRailMotion>();
            if(railMotion!=null)
            {
                railMotion.ApplyBackend(true);
                Assert(Math.Abs(railMotion.Station-driver.Model.State.PositionM)<1e-6&&Math.Abs(railMotion.WheelAngleDegrees+driver.Model.State.PositionM/CabRailMotion.RollingRadius*180/Math.PI)<1e-5,"Live rail motion is not reading the backend");
                Debug.Log("NF5K_RAIL_MOTION_PLAY_OK: live backend signed position drives body, bogies and wheel phase");
            }
            Assert(!state.SetReverser(ReverserPosition.Reverse),"Moving reversal not rejected");
            Assert(!Driving(CabDrivingAction.Reverser).Activate(true)&&state.Reverser==ReverserPosition.Forward,"Physical moving reversal accepted");
            Assert(ClickDriving(state,Driving(CabDrivingAction.IndependentBrake)),"Independent brake ray missed");driver.Advance(3);
            Assert(state.IndependentBrakeNotch==.25f&&driver.Model.State.LocoCylinderKpa>10&&driver.Model.State.TrainCylinderKpa<5,"Physical independent brake not isolated");
            int throttleBeforeBrakeReduction=state.ThrottleNotch;
            Assert(state.BrakeCylinderKpa>=20,"Independent brake did not reach the traction-interlock threshold");
            Assert(throttle.Activate(true)&&state.ThrottleNotch==throttleBeforeBrakeReduction-1,"Traction interlock prevented a positive-notch throttle reduction");
            Assert(ClickDriving(state,Driving(CabDrivingAction.IndependentBrake),true),"Independent release ray missed");
            string saved=driver.CaptureCheckpoint();driver.Advance(1);driver.RestoreCheckpoint(saved);
            if(railMotion!=null){railMotion.ApplyBackend(true);Assert(Math.Abs(railMotion.Station-driver.Model.State.PositionM)<1e-6,"Restored checkpoint left stale visual position");}
            Assert(Math.Abs(driver.Model.State.SpeedMps*3.6-state.CurrentSpeedKmh)<1e-4,"Adapter checkpoint projection failed");
            var automatic=Driving(CabDrivingAction.AutomaticBrake);
            for(int i=0;i<3;i++)Assert(ClickDriving(state,automatic),"Automatic brake ray missed");
            Assert(state.BrakePos==BrakeNotch.Emergency&&state.ThrottleNotch==0,"Physical brake failed to cut traction");
            driver.Advance(180);Assert(Math.Abs(state.CurrentSpeedKmh)<.001,"Runtime emergency braking failed");
            Debug.Log("NF5K_DRIVING_INTERACTION_OK: four mesh raycast targets, cold interlock, accepted detents, moving reverser guard, independent cylinder and automatic emergency brake");
            var railViews=UnityEngine.Object.FindFirstObjectByType<CabRailReviewViews>();int priorView=railViews!=null?railViews.View:0;
            if(railMotion!=null){railMotion.ApplyBackend(true);railViews?.SetView(0);}
            try{NF5KContextInteractionReview.Validate(state);}finally{railViews?.SetView(priorView);}
            state.StopEngine();driver.Advance(5);Assert(!lights.MainPower&&lights.EmergencyActive,"Engine stop did not transfer lighting to backup");
            Debug.Log("NF5K_SIMULATION_PLAYMODE_OK: key/master/pump startup, delayed diesel start, charging, forward traction, moving-reverse guard, checkpoint and emergency stop");
        }
        catch(Exception e){Debug.LogException(e);SessionState.SetBool(Failed,true);}
        EditorApplication.update-=Check;EditorApplication.isPlaying=false;
    }
    static void Assert(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
    static bool ClickDriving(CabEngineState state,CabDrivingTarget target,bool secondary=false)
    {
        target.ApplyVisual();Physics.SyncTransforms();
        var pointer=state.GetComponent<CabStartupPointer>();
        foreach(var collider in target.GetComponentsInChildren<MeshCollider>())
        foreach(var direction in new[]{Vector3.up,Vector3.right,Vector3.forward,Vector3.left,Vector3.back,Vector3.down})
        {
            var bounds=collider.bounds;
            var ray=new Ray(bounds.center+direction*(bounds.extents.magnitude+.02f),-direction);
            if(Physics.Raycast(ray,out var hit,4,1<<collider.gameObject.layer,QueryTriggerInteraction.Ignore)
                &&hit.collider.GetComponentInParent<CabDrivingTarget>()==target)
                return pointer.ActivateRay(ray,secondary);
        }
        throw new InvalidOperationException("No unobstructed mesh ray for "+target.Action);
    }
}
