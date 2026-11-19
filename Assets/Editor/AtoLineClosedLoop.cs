using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using RailwayRenaissance.Cab;
using RailwayRenaissance.Cab.Research;

// 900 m closed loop in the normal frame loop: V3 controller -> real cockpit commands -> 50 Hz backend (CabEngineState.Update)
// -> CabRailMotion (LateUpdate pose + route-end protection) on the Blender ATO line. Only cockpit preparation (start, 306 s charge)
// is stepped by hand; every mission frame runs through Update/LateUpdate at a fixed 60 FPS frame time (Time.captureFramerate).
// Batch entry: AtoLineClosedLoop.Batch (no -quit). Evidence: tools/compile/ato_line_closed_loop.json / _trace.csv, tools/ato_track/review/loop_*.png.
public static class AtoLineClosedLoop
{
    public const string ScenePath="Assets/Scenes/AtoLineClosedLoop.unity",GradedScenePath="Assets/Scenes/AtoLineClosedLoopGraded.unity";
    public static void Build(bool graded=false)
    {
        if(!File.Exists(AtoLineReview.ScenePath))throw new InvalidOperationException("Run AtoLineReview.Batch first (it creates the line scene)");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=EditorSceneManager.OpenScene(AtoLineReview.ScenePath);
        string path=graded?GradedScenePath:ScenePath;
        if(!EditorSceneManager.SaveScene(scene,path,true))throw new IOException("Could not create closed-loop scene");
        scene=EditorSceneManager.OpenScene(path);
        if(graded)
        {
            var flat=GameObject.Find("ATO_Test_Line");if(flat!=null)UnityEngine.Object.DestroyImmediate(flat);
            AtoTestLineBuilder.Assemble("ATO_Test_Line_Graded",AtoTestLineBuilder.GradedLine);
        }
        var lineTrack=AtoTestLineBuilder.TrackFor(graded?AtoTestLineBuilder.GradedLine:AtoTestLineBuilder.FlatLine);
        AtoGroundBuilder.Build(lineTrack);
        AtoSceneryBuilder.Build(lineTrack);
        var day=UnityEngine.Object.FindAnyObjectByType<CabDaylightEnvironment>(); // the project's own daytime scene: clear sky, sun, ambient
        if(day!=null){day.enabled=true;day.Apply(CabDaylightEnvironment.DayPreset.Clear);}
        // Temporal AA on this scene's own camera: distant sleepers repeat many times per pixel and shimmer without it (the pixel-art cab camera is a different one and is left alone).
        var mainCam=Camera.main;if(mainCam!=null){var aa=mainCam.GetUniversalAdditionalCameraData();aa.antialiasing=AntialiasingMode.TemporalAntiAliasing;aa.antialiasingQuality=AntialiasingQuality.High;var taa=aa.taaSettings;taa.baseBlendFactor=.95f;taa.jitterScale=1f;taa.varianceClampScale=.9f;aa.taaSettings=taa;EditorUtility.SetDirty(aa);}
        var driver=UnityEngine.Object.FindAnyObjectByType<CabSimulationDriver>();
        if(driver.GetComponent<CabAtoResearchDriver>()==null)driver.gameObject.AddComponent<CabAtoResearchDriver>();
        if(driver.GetComponent<AtoTrackVibration>()==null)driver.gameObject.AddComponent<AtoTrackVibration>(); // visual ballast shake only
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    public static void Batch(){Run(false,60);}
    public static void BatchGraded(){Run(true,60);}
    public static void Batch30(){Run(false,30);}
    public static void BatchGraded30(){Run(true,30);}
    public static void Batch144(){Run(false,144);}
    public static void BatchGraded144(){Run(true,144);}
    // Runs the existing development-case play review on its own scene without rebuilding it (comparison for the Smooth stop error).
    public static void BatchDevHarness()
    {
        try{EditorSceneManager.OpenScene(NF5KAtoResearchReview.ScenePath);NF5KAtoPlayReview.Begin();}
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void BatchDiag(){SessionState.SetBool("AtoClosedLoop.Diag",true);Run(false,60);}
    static void Run(bool graded,int fps)
    {
        try{Build(graded);AtoLineClosedLoopPlay.Begin(graded,fps);}
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}

[InitializeOnLoad]
public static class AtoLineClosedLoopPlay
{
    const string Pending="AtoClosedLoop.Pending",Failed="AtoClosedLoop.Failed",GradedKey="AtoClosedLoop.Graded";
    static bool Graded=>SessionState.GetBool(GradedKey,false);
    static string Suffix=>(Graded?"_graded":"")+(Fps==60?"":"_"+Fps+"fps");
    const string FpsKey="AtoClosedLoop.Fps";
    static int Fps=>SessionState.GetInt(FpsKey,60);
    static int MaxMissionFrames=>Fps*400;
    [Serializable] public sealed class RunReport
    {
        public string name;
        public double stopPositionM,signedErrorM,arrivalS,maxSpeedMps,maxLimitExceedMps,maxPoseGapM,maxContactErrorM,simSeconds;
        public int frames,limitExceedFrames,emergencyFrames,interventions;
        public long minBrakeSwitchTicks=-1;
        public bool stopped,activeAtEnd;
        public string status;
    }
    [Serializable] public sealed class Report
    {
        public int fps=Fps;public string controller="V3",line="ATO_Test_Line s=-60..1200";public bool graded;public double maxGradePermilleSeen,restoreStationErrorM;
        public RunReport[] runs;
        public bool pauseResumeOk,checkpointRestoreOk,manualTakeoverOk,wheelsFixedAfterStop;
        public double pauseDriftM,restorePoseErrorM,manualStopFromM,manualStopDistanceM;
        public string scope="Nominal-task closed loop on a kinematic pose model; not wheel/rail contact force, slip or safety validation. Cockpit preparation is hand-stepped, mission frames are not.";
    }
    static IEnumerator routine;static Report report;static readonly List<RunReport> runs=new List<RunReport>();static readonly StringBuilder trace=new StringBuilder("run,t,position,speed,limit,throttle,brake,station\n");
    static CabAtoResearchDriver ato;static CabEngineState state;static CabSimulationDriver driver;static CabRailMotion motion;static AtoRouteTask task;static RunReport current;
    static int lastBrake=-1;static long lastSwitchTick=-1;static readonly List<MeshCollider> rails=new List<MeshCollider>();static Camera external;
    static AtoLineClosedLoopPlay(){EditorApplication.playModeStateChanged+=Changed;}
    public static void Begin(bool graded=false,int fps=60){SessionState.SetInt(FpsKey,fps);SessionState.SetBool(GradedKey,graded);SessionState.SetBool(Pending,true);SessionState.SetBool(Failed,false);EditorApplication.isPlaying=true;}
    static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException("ATO_CLOSED_LOOP_FAIL: "+message);}
    static IEnumerator demo;
    // Pressing Play by hand in one of the two closed-loop scenes: follow the ATO line, start the cold cockpit and engage V3 (Smooth), in real time.
    static IEnumerator Demo(bool graded)
    {
        foreach(var _ in Frames(10))yield return null;
        var a=UnityEngine.Object.FindAnyObjectByType<CabAtoResearchDriver>();if(a==null)yield break;
        var st=a.GetComponent<CabEngineState>();var drv=a.GetComponent<CabSimulationDriver>();var mo=UnityEngine.Object.FindAnyObjectByType<CabRailMotion>();
        var startup=st.GetComponentInChildren<CabStartupControls>();
        if(mo==null||startup==null||!st.enabled){Debug.LogWarning("ATO_DEMO_SKIPPED: scene lacks rail motion or startup controls");yield break;}
        mo.UseTrack(AtoTestLineBuilder.TrackFor(graded?AtoTestLineBuilder.GradedLine:AtoTestLineBuilder.FlatLine));
        // The review scene leaves the driver camera at the world origin; carry it with the body (same eye pose as the Shot() driver view) for this play session only.
        if(a.GetComponent<AtoRenderQuality>()==null)a.gameObject.AddComponent<AtoRenderQuality>(); // MSAA instead of TAA for this play session
        if(a.GetComponent<AtoCameraViews>()==null)a.gameObject.AddComponent<AtoCameraViews>(); // keys 1-5 / Tab; also puts the camera on the body
        if(!startup.KeyInserted)startup.InsertKey();
        startup.SetMaster(true);startup.SetFuelPump(true);startup.TryStartEngine();
        drv.Advance(306);
        st.SetReverser(ReverserPosition.Forward);drv.ParkingBrake=false;st.SetBrakeNotch(BrakeNotch.Run);drv.Advance(.02);
        yield return null;
        a.UseV3=true;a.UseRouteController=false;a.SelectMode(AtoObjective.Smooth);
        Debug.Log(a.Engage()?"ATO_DEMO_ENGAGED":"ATO_DEMO_FAILED "+a.Status);
    }
    static void DemoStep()
    {
        try{if(demo!=null&&demo.MoveNext())return;}catch(Exception e){Debug.LogException(e);}
        EditorApplication.update-=DemoStep;demo=null;
    }
    static void Changed(PlayModeStateChange change)
    {
        if(!SessionState.GetBool(Pending,false))
        {
            if(change==PlayModeStateChange.EnteredPlayMode)
            {
                string path=EditorSceneManager.GetActiveScene().path;
                if(path==AtoLineClosedLoop.ScenePath||path==AtoLineClosedLoop.GradedScenePath){demo=Demo(path==AtoLineClosedLoop.GradedScenePath);EditorApplication.update+=DemoStep;}
            }
            if(change==PlayModeStateChange.ExitingPlayMode){EditorApplication.update-=DemoStep;demo=null;}
            return;
        }
        if(change==PlayModeStateChange.EnteredPlayMode){routine=Mission();EditorApplication.update+=Step;}
        if(change==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Pending,false);Time.captureFramerate=0;EditorApplication.Exit(SessionState.GetBool(Failed,false)?1:0);}
    }
    static void Step()
    {
        try{if(routine.MoveNext())return;}
        catch(Exception e){SessionState.SetBool(Failed,true);Debug.LogException(e);}
        EditorApplication.update-=Step;EditorApplication.isPlaying=false;
    }
    static IEnumerable Frames(int n){for(int i=0;i<n;i++)yield return null;}

    static IEnumerator Mission()
    {
        Time.captureFramerate=Fps;
        foreach(var _ in Frames(10))yield return null;
        ato=UnityEngine.Object.FindAnyObjectByType<CabAtoResearchDriver>();state=ato.GetComponent<CabEngineState>();driver=ato.GetComponent<CabSimulationDriver>();
        motion=UnityEngine.Object.FindAnyObjectByType<CabRailMotion>();
        Require(motion!=null&&state.enabled,"Scene lacks rail motion or the frame-loop state component is disabled");
        var plan=Graded?AtoTestLineBuilder.GradedLine:AtoTestLineBuilder.FlatLine;
        motion.UseTrack(AtoTestLineBuilder.TrackFor(plan));
        foreach(var mf in GameObject.Find(Graded?"ATO_Test_Line_Graded":"ATO_Test_Line").GetComponentsInChildren<MeshFilter>())if(mf.name.StartsWith("Rail_")){var c=mf.gameObject.AddComponent<MeshCollider>();c.sharedMesh=mf.sharedMesh;rails.Add(c);}
        Physics.SyncTransforms();
        external=new GameObject("ClosedLoop_External_Camera").AddComponent<Camera>();external.enabled=false;external.clearFlags=CameraClearFlags.Skybox;external.fieldOfView=36;external.nearClipPlane=.1f;external.farClipPlane=3000;
        { // Same renderer, HDR and tonemapping volume as the cab camera so exterior shots are lit like the rest of the scene.
            var mainCam=Camera.main;external.allowHDR=mainCam.allowHDR;
            var src=mainCam.GetUniversalAdditionalCameraData();var dst=external.GetUniversalAdditionalCameraData();
            if(UnityEditorInternal.ComponentUtility.CopyComponent(src))UnityEditorInternal.ComponentUtility.PasteComponentValues(dst);
        }
        driver.BeforeSimulationStep+=OnTick;
        ato.UseV3=true;ato.UseRouteController=false;

        // Cockpit preparation: hand-stepped, as in the existing acceptance (cold start, 306 s charge).
        var startup=state.GetComponentInChildren<CabStartupControls>();Require(startup!=null,"Startup controls missing");
        if(!startup.KeyInserted)Require(startup.InsertKey(),"Key rejected");
        Require(startup.SetMaster(true)&&startup.SetFuelPump(true)&&startup.TryStartEngine(),"Startup command failed");
        driver.Advance(306);Require(state.IsEngineRunning&&state.BrakePipeKpa>490,"Charging failed");
        Require(state.SetReverser(ReverserPosition.Forward),"Forward rejected");driver.ParkingBrake=false;state.SetBrakeNotch(BrakeNotch.Run);driver.Advance(.02);
        string prepared=driver.CaptureCheckpoint();
        yield return null;

        report=new Report();if(Graded)report.line="ATO_Test_Line_Graded s=-60..1200, +-10/20 permil, net 0";report.graded=Graded;
        // Run 1: Smooth, with pause/resume and a research checkpoint restore mid-run, while frames keep running.
        var r1=new RunReport{name="Smooth"};current=r1;
        foreach(var x in Start(prepared,AtoObjective.Smooth))yield return x;
        Shot("loop_cab_start",null);Shot("loop_ext_start",new Vector3(7,2.4f,0));
        bool paused=false,restored=false,shot1=false,shot2=false;int guard=0;
        while(ato.Active&&guard++<MaxMissionFrames)
        {
            yield return null;Sample(r1);
            double pos=driver.Model.State.PositionM;
            if(!shot1&&pos>120){shot1=true;Shot("loop_ext_120m",new Vector3(7,2.4f,0));Shot("loop_cab_120m",null);}
            if(!shot2&&pos>650){shot2=true;Shot("loop_ext_650m",new Vector3(7,2.4f,0));}
            if(!paused&&pos>300)
            {
                paused=true;ato.SetPaused(true);
                double p0=driver.Model.State.PositionM,station0=motion.Station;var pose0=motion.Vehicle.position;
                foreach(var x in Frames(30))yield return x;
                report.pauseDriftM=Math.Max(Math.Abs(driver.Model.State.PositionM-p0),Math.Max(Math.Abs(motion.Station-station0),Vector3.Distance(motion.Vehicle.position,pose0)));
                report.pauseResumeOk=report.pauseDriftM<1e-9;Require(report.pauseResumeOk,"Paused run drifted by "+report.pauseDriftM);
                string cp=ato.CaptureResearchCheckpoint();double cpPos=driver.Model.State.PositionM;var cpPose=motion.Vehicle.position;
                ato.SetPaused(false);foreach(var x in Frames(180))yield return x;Sample(r1);
                Require(driver.Model.State.PositionM>cpPos+1,"Run did not advance after resume");
                ato.SetPaused(true);ato.RestoreResearchCheckpoint(cp);
                foreach(var x in Frames(3))yield return x;
                report.restorePoseErrorM=Math.Max(Math.Abs(motion.Station-cpPos),Vector3.Distance(motion.Vehicle.position,cpPose));
                report.restoreStationErrorM=Math.Abs(motion.Station-cpPos);
                // Station must match to 1 mm; the body position may differ by the +-4 mm heave spring clamp (the spring is reset on restore).
                report.checkpointRestoreOk=report.restoreStationErrorM<1e-3&&report.restorePoseErrorM<.008&&Math.Abs(driver.Model.State.PositionM-cpPos)<1e-9&&ato.Active;
                Require(report.checkpointRestoreOk,"Restore: pose error "+report.restorePoseErrorM+" m, active="+ato.Active);
                ato.SetPaused(false);restored=true;
            }
        }
        foreach(var x in Frames(30))yield return x;Sample(r1);Finish(r1);Shot("loop_ext_stop",new Vector3(7,2.4f,0));Shot("loop_cab_stop",null);
        Require(restored,"Pause/restore point never reached");Require(r1.stopped&&!r1.activeAtEnd,"Run 1 did not stop under ATO: "+r1.status);runs.Add(r1);

        // Runs 2-3: the other two objectives, nominal.
        foreach(var mode in new[]{AtoObjective.AirSaving,AtoObjective.Punctual})
        {
            var r=new RunReport{name=mode.ToString()};current=r;
            foreach(var x in Start(prepared,mode))yield return x;
            guard=0;while(ato.Active&&guard++<MaxMissionFrames){yield return null;Sample(r);}
            foreach(var x in Frames(30))yield return x;Sample(r);Finish(r);Require(r.stopped&&!r.activeAtEnd,mode+" did not stop under ATO: "+r.status);runs.Add(r);
        }

        // Run 4: manual takeover with an emergency brake at ~400 m; the 3D pose must follow the backend to a standstill and hold the wheel phase.
        var r4=new RunReport{name="ManualTakeover"};current=r4;
        foreach(var x in Start(prepared,AtoObjective.Smooth))yield return x;
        guard=0;while(ato.Active&&driver.Model.State.PositionM<400&&guard++<MaxMissionFrames){yield return null;Sample(r4);}
        report.manualStopFromM=driver.Model.State.PositionM;CabManualControlIntent.Notify(state);state.SetBrakeNotch(BrakeNotch.Emergency);
        yield return null;Sample(r4);Require(!ato.Active,"Manual emergency brake did not take over");
        guard=0;while(Math.Abs(driver.Model.State.SpeedMps)>.01&&guard++<Fps*120){yield return null;Sample(r4);}
        Require(Math.Abs(driver.Model.State.SpeedMps)<=.01,"Vehicle did not stop after manual emergency brake");
        double phase=motion.WheelAngleDegrees;foreach(var x in Frames(60))yield return x;Sample(r4);
        report.wheelsFixedAfterStop=Math.Abs(motion.WheelAngleDegrees-phase)<1e-9;Require(report.wheelsFixedAfterStop,"Wheels keep turning at standstill");
        report.manualStopDistanceM=driver.Model.State.PositionM-report.manualStopFromM;report.manualTakeoverOk=!ato.Active&&r4.emergencyFrames>0&&r4.maxPoseGapM<1e-6;
        Require(report.manualTakeoverOk,"Manual takeover run inconsistent");Finish(r4);runs.Add(r4);

        if(SessionState.GetBool("AtoClosedLoop.Diag",false))
        {
            // Diagnostics for the Smooth stop error (dev case -2.02 m vs this harness -2.74 m): same start, three variants.
            // A: hand-stepped 50 Hz (frame loop off), as in NF5KAtoResearchReview.
            var a=new RunReport{name="Diag_Smooth_ManualStep"};current=a;
            foreach(var x in Start(prepared,AtoObjective.Smooth))yield return x;
            state.enabled=false;for(int i=0;i<15000&&ato.Active;i++)driver.Advance(.02);state.enabled=true;
            yield return null;Finish(a);runs.Add(a);
            // B: dev-case sequence, hand-stepped: Advance(1), switch to Punctual and back with 0.12 s between (as the replay checks do).
            var b=new RunReport{name="Diag_Smooth_ModeToggleManual"};current=b;
            foreach(var x in Start(prepared,AtoObjective.Smooth))yield return x;
            state.enabled=false;driver.Advance(1);ato.SelectMode(AtoObjective.Punctual);driver.Advance(.12);ato.SelectMode(AtoObjective.Smooth);driver.Advance(.12);
            for(int i=0;i<15000&&ato.Active;i++)driver.Advance(.02);state.enabled=true;yield return null;Finish(b);runs.Add(b);
            // C: dev-case RelativeTargetMeters=500 setting, hand-stepped, no mode toggle.
            var c=new RunReport{name="Diag_Smooth_RelTarget500"};current=c;
            ato.RelativeTargetMeters=500;
            foreach(var x in Start(prepared,AtoObjective.Smooth))yield return x;
            state.enabled=false;for(int i=0;i<15000&&ato.Active;i++)driver.Advance(.02);state.enabled=true;yield return null;Finish(c);runs.Add(c);
        }
        report.runs=runs.ToArray();
        Directory.CreateDirectory("tools/compile");
        File.WriteAllText("tools/compile/ato_line_closed_loop"+Suffix+".json",JsonUtility.ToJson(report,true));File.WriteAllText("tools/compile/ato_line_closed_loop"+Suffix+"_trace.csv",trace.ToString());
        Debug.Log("ATO_LINE_CLOSED_LOOP_OK "+JsonUtility.ToJson(report));
    }

    static IEnumerable Start(string prepared,AtoObjective mode)
    {
        ato.SetPaused(true);driver.RestoreCheckpoint(prepared);ato.SetPaused(false);
        foreach(var x in Frames(2))yield return x;
        lastBrake=-1;lastSwitchTick=-1;
        ato.UseV3=true;ato.UseRouteController=false;ato.SelectMode(mode);
        Require(ato.Engage(),"V3 engagement failed: "+ato.Status);
        task=AtoRouteTask.Standard(driver.Model.State.PositionM);current.simSeconds=-driver.Model.State.Time;
    }
    static void OnTick(long tick)
    {
        if(current==null||!ato.Active)return;
        int b=(int)state.BrakePos;
        if(lastBrake>=0&&b!=lastBrake&&b!=(int)BrakeNotch.Emergency){if(lastSwitchTick>=0){long gap=tick-lastSwitchTick;if(current.minBrakeSwitchTicks<0||gap<current.minBrakeSwitchTicks)current.minBrakeSwitchTicks=gap;}lastSwitchTick=tick;}
        lastBrake=b;
    }
    static void Sample(RunReport r)
    {
        var s=driver.Model.State;double limit=task.LimitAt(s.PositionM);r.frames++;
        r.maxSpeedMps=Math.Max(r.maxSpeedMps,s.SpeedMps);if(driver.RouteGradePermille!=null)report.maxGradePermilleSeen=Math.Max(report.maxGradePermilleSeen,Math.Abs(driver.RouteGradePermille(s.PositionM)));
        if(s.SpeedMps>limit+1e-9){r.limitExceedFrames++;r.maxLimitExceedMps=Math.Max(r.maxLimitExceedMps,s.SpeedMps-limit);}
        if(state.BrakePos==BrakeNotch.Emergency)r.emergencyFrames++;
        if(!driver.SimulationPaused)
        {
            r.maxPoseGapM=Math.Max(r.maxPoseGapM,Math.Abs(motion.Station-s.PositionM));
            if(r.frames%30==0)r.maxContactErrorM=Math.Max(r.maxContactErrorM,ContactError());
        }
        if(r.frames%6==0)trace.Append(r.name).Append(',').Append(s.Time.ToString("F2")).Append(',').Append(s.PositionM.ToString("F3")).Append(',').Append(s.SpeedMps.ToString("F3")).Append(',').Append(limit.ToString("F1")).Append(',').Append(state.ThrottleNotch).Append(',').Append((int)state.BrakePos).Append(',').Append(motion.Station.ToString("F3")).Append('\n');
    }
    static void Finish(RunReport r)
    {
        var s=driver.Model.State;r.simSeconds+=s.Time;r.stopPositionM=s.PositionM;r.signedErrorM=ato.SignedErrorMeters;r.stopped=Math.Abs(s.SpeedMps)<.01;r.activeAtEnd=ato.Active;
        var v3=ato.RouteV3State;r.arrivalS=v3!=null?v3.Arrival:-1;r.interventions=v3!=null&&v3.Intervention?1:0;r.status=ato.Status;
    }
    // Wheel contact points of all wheelsets against the rail meshes; only rail colliders count (the vehicle has its own).
    static double ContactError()
    {
        double worst=0;
        foreach(var b in motion.Bogies)for(int i=0;i<b.axles.Length;i++)foreach(int side in new[]{-1,1})
        {
            var contact=b.axles[i].position+b.frame.right*(side*.75f)-Vector3.up*(float)CabRailMotion.RollingRadius;
            bool found=false;
            foreach(var hit in Physics.RaycastAll(contact+Vector3.up,Vector3.down,2f))if(rails.Contains(hit.collider as MeshCollider)){found=true;worst=Math.Max(worst,Math.Abs(hit.point.y-contact.y));break;}
            if(!found)worst=Math.Max(worst,1);
        }
        return worst;
    }
    static float Halton(int i,int b){float f=1,r=0;while(i>0){f/=b;r+=f*(i%b);i/=b;}return r;}
    // external == null: the scene's own (cab) camera; otherwise an offset in the vehicle frame (x right of the body axis, y up, z along it).
    static void Shot(string name,Vector3? offset)
    {
        var cam=offset==null?Camera.main:external;if(cam==null)return;
        if(offset==null)
        {
            // Driver eye: the review scene authors this camera in the vehicle frame (vehicle at the world origin), so carry it with the body.
            var v=motion.Vehicle;cam.transform.position=v.TransformPoint(new Vector3(.65f,1.38f,.78f));
            cam.transform.rotation=v.rotation*Quaternion.LookRotation(new Vector3(.60f-.65f,1.22f-1.38f,-.8f-.78f));
        }
        if(offset!=null)
        {
            var v=motion.Vehicle;cam.transform.position=v.TransformPoint(new Vector3(4.6f,2.0f,8.6f+19f));cam.transform.LookAt(v.TransformPoint(new Vector3(-.4f,1.5f,8.6f-28f)));
        }
        bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
        // Anti-aliasing: SMAA on the camera plus 2x supersampling, averaged down to 1400x800.
        const int W=2100,H=1200,SS=2;
        var aniso=QualitySettings.anisotropicFiltering;QualitySettings.anisotropicFiltering=AnisotropicFiltering.ForceEnable;
        var data=cam.GetUniversalAdditionalCameraData();var aa=data.antialiasing;var aaq=data.antialiasingQuality;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;
        float far=cam.farClipPlane;cam.farClipPlane=Mathf.Max(far,2000);
        var rt=RenderTexture.GetTemporary(W*SS,H*SS,24);var prev=RenderTexture.active;var old=cam.targetTexture;var big=new Texture2D(W*SS,H*SS,TextureFormat.RGB24,false);var img=new Texture2D(W,H,TextureFormat.RGB24,false);
        bool wasEnabled=cam.enabled;
        // Far sleepers repeat many times inside one pixel, so a single render cannot resolve them: average N sub-pixel-jittered renders (Halton 2,3).
        const int N=16;var acc=new float[W*H*3];var basePm=cam.projectionMatrix;
        try{cam.targetTexture=rt;
            for(int k=0;k<N;k++)
            {
                float jx=Halton(k+1,2)-.5f,jy=Halton(k+1,3)-.5f; // offset within one final pixel
                var pm=basePm;pm.m02+=jx*2f/W;pm.m12+=jy*2f/H;cam.projectionMatrix=pm;
                cam.Render();RenderTexture.active=rt;big.ReadPixels(new Rect(0,0,W*SS,H*SS),0,0);big.Apply();
                var src=big.GetPixels32();
                for(int y=0;y<H;y++)for(int x=0;x<W;x++){int r=0,g=0,b=0;for(int dy=0;dy<SS;dy++)for(int dx=0;dx<SS;dx++){var c=src[(y*SS+dy)*W*SS+x*SS+dx];r+=c.r;g+=c.g;b+=c.b;}int i=(y*W+x)*3;acc[i]+=r;acc[i+1]+=g;acc[i+2]+=b;}
            }
            var dst=new Color32[W*H];float inv=1f/(SS*SS*N);
            for(int i=0;i<W*H;i++)dst[i]=new Color32((byte)(acc[i*3]*inv+.5f),(byte)(acc[i*3+1]*inv+.5f),(byte)(acc[i*3+2]*inv+.5f),255);
            img.SetPixels32(dst);img.Apply();
            Directory.CreateDirectory("tools/ato_track/review");File.WriteAllBytes("tools/ato_track/review/"+name+Suffix+".png",img.EncodeToPNG());}
        finally{cam.ResetProjectionMatrix();QualitySettings.anisotropicFiltering=aniso;cam.targetTexture=old;cam.enabled=wasEnabled;cam.farClipPlane=far;data.antialiasing=aa;data.antialiasingQuality=aaq;RenderTexture.active=prev;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(big);UnityEngine.Object.DestroyImmediate(img);ShaderUtil.allowAsyncCompilation=async;}
    }
}
