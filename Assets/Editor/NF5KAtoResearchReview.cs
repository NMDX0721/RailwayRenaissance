using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;
using RailwayRenaissance.Cab.Research;

public static class NF5KAtoResearchReview
{
    public const string ScenePath="Assets/Scenes/CabAtoResearchReview.unity";
    [MenuItem("RailRevival/Research/Build ATO Review")]
    public static void Build()
    {
        const string source="Assets/Scenes/CabRailMotionReview.unity";
        if(!File.Exists(source))throw new InvalidOperationException("Prepare the existing rail motion review first");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=EditorSceneManager.OpenScene(source);
        if(!EditorSceneManager.SaveScene(scene,ScenePath,true))throw new IOException("Could not create research scene");
        scene=EditorSceneManager.OpenScene(ScenePath);
        var driver=UnityEngine.Object.FindFirstObjectByType<CabSimulationDriver>();
        if(driver==null)throw new InvalidOperationException("No detailed simulation driver");
        if(driver.GetComponent<CabAtoResearchDriver>()==null)driver.gameObject.AddComponent<CabAtoResearchDriver>();
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("ATO_RESEARCH_SCENE_CREATED: separate opt-in scene; no real-train validation");
    }
    public static void Batch()
    {
        try{NF5KSimulationReview.Validate();Build();NF5KAtoPlayReview.Begin();}
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}

[InitializeOnLoad]
public static class NF5KAtoPlayReview
{
    const string Pending="ATOResearch.Pending",Failed="ATOResearch.Failed";
    static int frames;
    static NF5KAtoPlayReview(){EditorApplication.playModeStateChanged+=Changed;}
    public static void Begin(){SessionState.SetBool(Pending,true);SessionState.SetBool(Failed,false);EditorApplication.isPlaying=true;}
    static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    static void Changed(PlayModeStateChange change)
    {
        if(!SessionState.GetBool(Pending,false))return;
        if(change==PlayModeStateChange.EnteredPlayMode){frames=0;EditorApplication.update+=Check;}
        if(change==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Pending,false);EditorApplication.Exit(SessionState.GetBool(Failed,false)?1:0);}
    }
    static void Check()
    {
        if(++frames<10)return;
        EditorApplication.update-=Check;
        try
        {
            var ato=UnityEngine.Object.FindFirstObjectByType<CabAtoResearchDriver>();
            var state=ato.GetComponent<CabEngineState>();var driver=ato.GetComponent<CabSimulationDriver>();
            state.enabled=false;ato.UseRouteController=false;
            Require(!ato.Engage(),"ATO accepted a cold, unprepared cockpit");
            var startup=state.GetComponentInChildren<CabStartupControls>();
            Require(startup!=null,"Startup controls missing");
            if(!startup.KeyInserted)Require(startup.InsertKey(),"Key rejected");
            Require(startup.SetMaster(true)&&startup.SetFuelPump(true)&&startup.TryStartEngine(),"Startup command failed");
            driver.Advance(306);Require(state.IsEngineRunning&&state.BrakePipeKpa>490,"Charging failed");
            Require(state.SetReverser(ReverserPosition.Forward),"Forward rejected");
            driver.ParkingBrake=false;state.SetBrakeNotch(BrakeNotch.Run);driver.Advance(.02);string preparedPhysics=driver.CaptureCheckpoint();
            Require(ato.Engage(),"Pre-manual engagement failed");
            var independent=Array.Find(state.GetComponentsInChildren<CabDrivingTarget>(),t=>t.Action==CabDrivingAction.IndependentBrake);
            Require(independent!=null&&independent.Activate(true),"Physical no-change action failed");
            Require(!ato.Active&&state.IndependentBrakeNotch==0,"Same-setting manual input did not relinquish");
            Require(ato.Engage(),"Prepared ATO rejected");driver.Advance(1);
            Require(state.ThrottleNotch>0,"ATO did not command real traction");
            ato.SetPaused(true);string research=ato.CaptureResearchCheckpoint();string unchanged=driver.CaptureCheckpoint();
            driver.Advance(1);Require(driver.CaptureCheckpoint()==unchanged,"Paused physics advanced");
            var legacy1=JsonUtility.FromJson<CabAtoResearchDriver.ResearchCheckpoint>(research);legacy1.version=1;legacy1.useV3=false;legacy1.v3=null;ato.RestoreResearchCheckpoint(JsonUtility.ToJson(legacy1));Require(ato.Active&&driver.CaptureCheckpoint()==unchanged,"V1 research checkpoint compatibility failed");
            bool rejected=false;try{ato.RestoreResearchCheckpoint(research.Replace("\"version\":3","\"version\":99"));}catch(ArgumentException){rejected=true;}
            Require(rejected&&driver.CaptureCheckpoint()==unchanged,"Bad research checkpoint mutated physics");
            ato.SetPaused(false);driver.Advance(2);ato.SetPaused(true);string continued=driver.CaptureCheckpoint();
            ato.RestoreResearchCheckpoint(research);ato.SetPaused(false);driver.Advance(2);ato.SetPaused(true);
            Require(driver.CaptureCheckpoint()==continued,"Research adapter resume diverged");
            ato.SetPaused(false);driver.Advance(.5);
            Require(state.ThrottleNotch>0,"ATO traction was not restored before paused manual brake test");
            ato.SetPaused(true);CabManualControlIntent.Notify(state);state.SetBrakeNotch(BrakeNotch.Emergency);
            Require(!ato.Active&&state.BrakePos==BrakeNotch.Emergency&&state.ThrottleNotch==0,"Paused manual emergency brake did not take over");
            string pausedManual=ato.CaptureResearchCheckpoint();
            var manualEnvelope=JsonUtility.FromJson<CabAtoResearchDriver.ResearchCheckpoint>(pausedManual);
            var manualPhysics=JsonUtility.FromJson<CabSimulationDriver.Checkpoint>(manualEnvelope.physics);
            var manualInput=RailwayRenaissance.Cab.Simulation.SimulationCodec.Decode<RailwayRenaissance.Cab.Simulation.SimulationInput>(manualPhysics.exactInput);
            Require(manualInput.Brake==(int)BrakeNotch.Emergency&&manualInput.Throttle==0,"Paused manual controls were stale in the saved checkpoint");
            ato.RestoreResearchCheckpoint(pausedManual);
            Require(state.BrakePos==BrakeNotch.Emergency&&state.ThrottleNotch==0,"Paused checkpoint restore discarded the emergency brake");
            Debug.Log("ATO_PAUSED_MANUAL_CHECKPOINT_OK: emergency brake and zero throttle survive capture and restore without advancing physics");
            ato.SetPaused(false);
            state.SetBrakeNotch(BrakeNotch.Run);driver.Advance(30);
            Require(ato.Engage(),"Second engagement failed");
            for(int i=0;i<15000&&ato.Active;i++)driver.Advance(.02);
            Require(!ato.Active&&Math.Abs(driver.Model.State.SpeedMps)<.01,"ATO did not stop");
            Require(Math.Abs(ato.SignedErrorMeters)<=5,"ATO exceeded nominal development tolerance");
            Require(driver.BlocksPointer(new Vector2(20,Screen.height-30)),"ATO panel click-through guard missing");
            ato.SetPaused(true);
            var exactPack=JsonUtility.FromJson<CabSimulationDriver.Checkpoint>(preparedPhysics);
            var exactInput=RailwayRenaissance.Cab.Simulation.SimulationCodec.Decode<RailwayRenaissance.Cab.Simulation.SimulationInput>(exactPack.exactInput);
            exactInput.RailGrip=.37;exactInput.GradePermille=1.123456789;exactPack.exactInput=RailwayRenaissance.Cab.Simulation.SimulationCodec.Encode(exactInput);
            driver.RestoreCheckpoint(JsonUtility.ToJson(exactPack));ato.SetPaused(false);driver.Advance(.02);
            Require(driver.Input.RailGrip==.37&&driver.Input.GradePermille==1.123456789,"Restored exact environment was rounded/quantized");
            var v2Results=new System.Collections.Generic.List<string>();
            foreach(AtoObjective mode in Enum.GetValues(typeof(AtoObjective)))
            {
                ato.SetPaused(true);driver.RestoreCheckpoint(preparedPhysics);Require(!ato.Active,"Physical checkpoint retained automatic ownership");ato.SetPaused(false);
                ato.UseV3=false;ato.UseRouteController=true;ato.SelectMode(mode);Require(ato.Engage(),"V2 engagement failed");
                if(mode==AtoObjective.Smooth){driver.Advance(.12);ato.SetPaused(true);string original=ato.CaptureResearchCheckpoint();var legacy2=JsonUtility.FromJson<CabAtoResearchDriver.ResearchCheckpoint>(original);legacy2.version=2;legacy2.useV3=false;legacy2.v3=null;ato.RestoreResearchCheckpoint(JsonUtility.ToJson(legacy2));Require(ato.Active,"V2 research checkpoint compatibility failed");ato.SetPaused(false);}
                bool a=false,b=false,c=false,d=false;
                for(int i=0;i<18000&&ato.Active;i++)
                {
                    driver.Advance(.02);var rs=ato.RouteState;
                    bool capture=false;
                    if(mode==AtoObjective.Smooth){if(!a&&i>50){a=true;capture=true;}if(!b&&rs.Calibration>0){b=true;capture=true;}if(!c&&rs.Braking&&rs.Calibration==0){c=true;capture=true;}if(!d&&rs.ArrivalSeconds>=0){d=true;capture=true;}}
                    if(capture)
                    {
                        ato.SetPaused(true);string point=ato.CaptureResearchCheckpoint();
                        string disk="tools/compile/ato_v2_checkpoint_test.json";ato.SaveResearchFile(disk);Require(File.ReadAllText(disk)==point,"Disk checkpoint lost scalars");
                        string beforeBad=driver.CaptureCheckpoint();var damaged=JsonUtility.FromJson<CabAtoResearchDriver.ResearchCheckpoint>(point);damaged.route+="x";
                        bool badTask=false;try{ato.RestoreResearchCheckpoint(JsonUtility.ToJson(damaged));}catch(ArgumentException){badTask=true;}Require(badTask&&driver.CaptureCheckpoint()==beforeBad,"Damaged route mutated runtime");
                        ato.LoadResearchFile(disk);ato.SetPaused(false);driver.Advance(.4);ato.SetPaused(true);string future=driver.CaptureCheckpoint();
                        ato.RestoreResearchCheckpoint(point);ato.SetPaused(false);driver.Advance(.4);ato.SetPaused(true);Require(driver.CaptureCheckpoint()==future,"V2 complete replay diverged");ato.SetPaused(false);
                    }
                }
                Require(!ato.Active&&Math.Abs(driver.Model.State.SpeedMps)<.01&&Math.Abs(ato.SignedErrorMeters)<=5&&ato.RouteState.ArrivalSeconds>=0,"V2 nominal task failed: "+mode+" "+ato.Status);
                if(mode==AtoObjective.Smooth)Require(a&&b&&c&&d,"Missing V2 checkpoint phases");
                v2Results.Add(mode+" error="+ato.SignedErrorMeters+" arrival="+ato.RouteState.ArrivalSeconds);
            }
            foreach(AtoObjective mode in Enum.GetValues(typeof(AtoObjective)))
            {
                ato.SetPaused(true);driver.RestoreCheckpoint(preparedPhysics);ato.SetPaused(false);
                ato.UseV3=true;ato.UseRouteController=true;ato.SelectMode(mode);Require(ato.Engage(),"V3 engagement failed");
                if(mode==AtoObjective.Smooth){Require(independent.Activate(true)&&!ato.Active&&state.IndependentBrakeNotch==0,"V3 same-setting manual takeover failed");Require(ato.Engage(),"V3 re-engagement failed");}
                driver.Advance(1);
                if(mode==AtoObjective.Smooth){var before=ato.RouteV3State;ato.SelectMode(AtoObjective.Punctual);var pending=ato.RouteV3State;Require(pending.Mode==before.Mode&&pending.PendingMode==(int)AtoObjective.Punctual&&pending.BrakeGain==before.BrakeGain,"V3 mode changed early or reset estimator");driver.Advance(.12);Require(ato.RouteV3State.Mode==(int)AtoObjective.Punctual,"V3 mode did not change on control cycle");ato.SelectMode(mode);driver.Advance(.12);}
                ato.SetPaused(true);string checkpoint=ato.CaptureResearchCheckpoint();
                var corrupt=JsonUtility.FromJson<CabAtoResearchDriver.ResearchCheckpoint>(checkpoint);var badState=RailwayRenaissance.Cab.Simulation.SimulationCodec.Decode<AtoV3State>(corrupt.v3);badState.BrakeGain=double.NaN;corrupt.v3=RailwayRenaissance.Cab.Simulation.SimulationCodec.Encode(badState);string beforeCorrupt=driver.CaptureCheckpoint();bool invalidV3=false;try{ato.RestoreResearchCheckpoint(JsonUtility.ToJson(corrupt));}catch(ArgumentException){invalidV3=true;}Require(invalidV3&&driver.CaptureCheckpoint()==beforeCorrupt,"V3 corrupt checkpoint changed runtime");
                ato.SetPaused(false);driver.Advance(2);ato.SetPaused(true);string future=driver.CaptureCheckpoint();
                ato.RestoreResearchCheckpoint(checkpoint);ato.SetPaused(false);driver.Advance(2);ato.SetPaused(true);Require(future==driver.CaptureCheckpoint(),"V3 replay diverged");ato.SetPaused(false);
                bool accelerationPhase=false,coastingPhase=false,brakePhase=false,dwellPhase=false;
                for(int i=0;i<18000&&ato.Active;i++){
                    driver.Advance(.02);if(mode!=AtoObjective.Smooth)continue;
                    var current=driver.Model.State;var stage=ato.RouteV3State;double relative=900+ato.SignedErrorMeters;bool capture=false;
                    if(!accelerationPhase&&relative>10&&current.SpeedMps>1){accelerationPhase=true;capture=true;}
                    if(!coastingPhase&&relative>250&&relative<650&&current.Acceleration<-.005&&state.ThrottleNotch==0&&current.SpeedMps>1){coastingPhase=true;capture=true;}
                    if(!brakePhase&&relative>650&&stage.LastBrake>=2&&current.SpeedMps>1){brakePhase=true;capture=true;}
                    if(!dwellPhase&&stage.Arrival>=0){dwellPhase=true;capture=true;}
                    if(capture){ato.SetPaused(true);string stageCheckpoint=ato.CaptureResearchCheckpoint();ato.SetPaused(false);driver.Advance(.4);ato.SetPaused(true);string stageFuture=driver.CaptureCheckpoint();ato.RestoreResearchCheckpoint(stageCheckpoint);ato.SetPaused(false);driver.Advance(.4);ato.SetPaused(true);Require(stageFuture==driver.CaptureCheckpoint(),"V3 phase checkpoint replay diverged");ato.SetPaused(false);}
                }
                if(mode==AtoObjective.Smooth)Require(accelerationPhase&&coastingPhase&&brakePhase&&dwellPhase,"V3 phase checkpoint coverage incomplete");
                Require(!ato.Active&&ato.RouteV3State.Arrival>=115&&ato.RouteV3State.Arrival<=125&&Math.Abs(ato.SignedErrorMeters)<=5,"V3 nominal failed: "+mode+" "+ato.Status);
                v2Results.Add("V3 "+mode+" error="+ato.SignedErrorMeters+" arrival="+ato.RouteV3State.Arrival);
            }
            double nominalV3Error=ato.SignedErrorMeters;
            ato.SetPaused(true);driver.RestoreCheckpoint(preparedPhysics);ato.SetPaused(false);ato.UseV3=true;ato.SelectMode(AtoObjective.Punctual);
            Require(state.SetThrottle(5),"Moving-start traction setup failed");
            for(int i=0;i<4000&&driver.Model.State.SpeedMps<4;i++)driver.Advance(.02);
            Require(driver.Model.State.SpeedMps>=4,"Moving-start speed setup failed");state.SetThrottle(0);double startSpeed=driver.Model.State.SpeedMps;
            Require(ato.Engage()&&Math.Abs(ato.RouteV3State.InitialSpeed-startSpeed)<1e-9,"V3 did not retain actual initial speed");
            driver.Advance(1);ato.SetPaused(true);string movingPoint=ato.CaptureResearchCheckpoint();ato.SetPaused(false);driver.Advance(2);ato.SetPaused(true);string movingFuture=driver.CaptureCheckpoint();ato.RestoreResearchCheckpoint(movingPoint);ato.SetPaused(false);driver.Advance(2);ato.SetPaused(true);Require(movingFuture==driver.CaptureCheckpoint(),"Moving-start checkpoint replay diverged");ato.SetPaused(false);ato.TakeOver();
            Debug.Log("ATO_V3_MOVING_START_OK: actual speed used by planner and checkpoint replay");
            Debug.Log("ATO_V3_PLAY_ACCEPTED: three nominal modes and checkpoint replay");
            File.WriteAllLines("tools/compile/ato_v2_play_acceptance.txt",v2Results);
            Debug.Log("ATO_V2_PLAY_ACCEPTED: three modes, full replay, facade and pure-physics manual restore");
            Directory.CreateDirectory("tools/compile");
            File.WriteAllText("tools/compile/ato_acceptance_"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ")+".json",JsonUtility.ToJson(new Result{errorM=nominalV3Error,coldGuard=true,actualCommands=true,manualTakeover=true,stopped=true,panelGuard=true,pausedManualCheckpoint=true},true));
            Debug.Log("ATO_PLAY_ACCEPTED: actual facade commands, takeover, nominal stop error="+nominalV3Error);
        }
        catch(Exception e){SessionState.SetBool(Failed,true);Debug.LogException(e);}
        EditorApplication.isPlaying=false;
    }
    [Serializable]sealed class Result{public double errorM;public bool coldGuard,actualCommands,manualTakeover,stopped,panelGuard,pausedManualCheckpoint;}
}
