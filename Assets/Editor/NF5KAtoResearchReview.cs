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
            state.enabled=false;
            Require(!ato.Engage(),"ATO accepted a cold, unprepared cockpit");
            var startup=state.GetComponentInChildren<CabStartupControls>();
            Require(startup!=null,"Startup controls missing");
            if(!startup.KeyInserted)Require(startup.InsertKey(),"Key rejected");
            Require(startup.SetMaster(true)&&startup.SetFuelPump(true)&&startup.TryStartEngine(),"Startup command failed");
            driver.Advance(306);Require(state.IsEngineRunning&&state.BrakePipeKpa>490,"Charging failed");
            Require(state.SetReverser(ReverserPosition.Forward),"Forward rejected");
            driver.ParkingBrake=false;state.SetBrakeNotch(BrakeNotch.Run);
            Require(ato.Engage(),"Prepared ATO rejected");driver.Advance(1);
            Require(state.ThrottleNotch>0,"ATO did not command real traction");
            state.SetBrakeNotch(BrakeNotch.Emergency);driver.Advance(.02);
            Require(!ato.Active&&state.BrakePos==BrakeNotch.Emergency&&state.ThrottleNotch==0,"Manual emergency takeover lost");
            state.SetBrakeNotch(BrakeNotch.Run);driver.Advance(30);
            Require(ato.Engage(),"Second engagement failed");
            for(int i=0;i<15000&&ato.Active;i++)driver.Advance(.02);
            Require(!ato.Active&&Math.Abs(driver.Model.State.SpeedMps)<.01,"ATO did not stop");
            Require(Math.Abs(ato.SignedErrorMeters)<=5,"ATO exceeded nominal development tolerance");
            Require(driver.BlocksPointer(new Vector2(20,Screen.height-30)),"ATO panel click-through guard missing");
            Directory.CreateDirectory("tools/compile");
            File.WriteAllText("tools/compile/ato_play_acceptance.json",JsonUtility.ToJson(new Result{errorM=ato.SignedErrorMeters,coldGuard=true,actualCommands=true,manualTakeover=true,stopped=true,panelGuard=true},true));
            Debug.Log("ATO_PLAY_ACCEPTED: actual facade commands, takeover, nominal stop error="+ato.SignedErrorMeters);
        }
        catch(Exception e){SessionState.SetBool(Failed,true);Debug.LogException(e);}
        EditorApplication.isPlaying=false;
    }
    [Serializable]sealed class Result{public double errorM;public bool coldGuard,actualCommands,manualTakeover,stopped,panelGuard;}
}
