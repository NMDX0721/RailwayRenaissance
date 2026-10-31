using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KServiceControlReview
{
    static void Assert(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    public static void Validate(CabEngineState state)
    {
        NF5KBrakeRefinement.Validate(state);
        var controls=state.GetComponentsInChildren<CabServiceControl>();Assert(controls.Length==2,"Two service controls required");
        var horn=controls.Single(c=>!c.Sanding);var sand=controls.Single(c=>c.Sanding);var driver=state.GetComponent<CabSimulationDriver>();
        var audio=state.GetComponentInChildren<CabHornAudio>();Assert(audio!=null&&audio.Source.clip!=null,"Horn audio missing");
        var cam=Camera.main;var position=cam.transform.position;var rotation=cam.transform.rotation;string snapshot=driver.CaptureCheckpoint();
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var ui=state.GetComponent<CabContextInteraction>();var focus=typeof(CabContextInteraction).GetField("focused",flags);var old=focus.GetValue(ui);
        try
        {
            foreach(var c in controls)
            {
                cam.transform.position=c.transform.position+Vector3.up*.35f;cam.transform.LookAt(c.transform.position,Vector3.forward);Physics.SyncTransforms();
                Assert(Physics.Raycast(cam.transform.position,Vector3.down,out var hit,.45f,1<<30)&&hit.collider.GetComponent<CabServiceControl>()==c,"Service button ray blocked");
                focus.SetValue(ui,c);typeof(CabContextInteraction).GetMethod("BuildChoices",flags).Invoke(ui,null);
                Assert(((System.Collections.ICollection)typeof(CabContextInteraction).GetField("choices",flags).GetValue(ui)).Count==1,"Service action missing");
                var rest=c.Cap.localPosition;typeof(CabContextInteraction).GetField("selected",flags).SetValue(ui,0);typeof(CabContextInteraction).GetMethod("ExecuteSelected",flags).Invoke(ui,null);
                Assert(c.Active&&Mathf.Abs(c.Cap.localPosition.y-rest.y+.002f)<.00001f,"Momentary cap/output failed");
                if(c.Sanding){driver.Advance(.02);Assert(state.SandOn&&driver.Input.Sand,"Physical sanding not routed to backend");}
                else {Assert(state.IsHornActive,"Physical horn not active");audio.Refresh(.2f);Assert(audio.Source.isPlaying&&audio.Source.volume>0,"Horn audio did not start");}
                c.Maintain();Assert(c.Active,"Hold lost active state");
                typeof(CabServiceControl).GetField("until",flags).SetValue(c,Time.time-1);c.Refresh();
                Assert(!c.Active&&c.Cap.localPosition==rest,"Pulse expiry did not return cap");
                if(c.Sanding){driver.Advance(.02);Assert(!state.SandOn&&!driver.Input.Sand,"Sanding remained after release");}
                else {Assert(!state.IsHornActive,"Horn latched after release");audio.Refresh(1);Assert(!audio.Source.isPlaying&&audio.Source.volume==0,"Horn audio did not fade/stop");}
                Assert(c.Begin(),"Service restart failed");bool master=driver.Input.Master;driver.Input.Master=false;c.Refresh();Assert(!c.Active&&!c.Begin(),"No-power command accepted");driver.Input.Master=master;
                double main=driver.Model.State.MainKpa;driver.Model.State.MainKpa=100;Assert(!c.Begin(),"Empty-air command accepted");driver.Model.State.MainKpa=main;
                cam.transform.position=c.transform.position+Vector3.up*4;Assert(!c.Begin(),"Remote service command accepted");
                cam.transform.position=c.transform.position+Vector3.up*.35f;Assert(c.Begin(),"Service restart rejected");c.enabled=false;Assert(!c.Active&&!c.Begin(),"Disabled service stuck on");c.enabled=true;
                Assert(c.Begin(),"Pause probe could not start");float scale=Time.timeScale;
                try{Time.timeScale=0;c.Refresh();Assert(!c.Active&&!c.Begin(),"Paused short pulse remains active");}finally{Time.timeScale=scale;}
            }
            var clip=audio.Source.clip;var samples=new float[clip.samples*clip.channels];Assert(clip.GetData(samples,0),"Horn PCM unavailable");
            double rms=Math.Sqrt(samples.Sum(v=>(double)v*v)/samples.Length);Assert(rms>.03&&samples.All(v=>!float.IsNaN(v)&&Math.Abs(v)<1),"Horn PCM invalid or silent");
            double maxStep=0;for(int i=1;i<samples.Length;i++)maxStep=Math.Max(maxStep,Math.Abs(samples[i]-samples[i-1]));
            Assert(Math.Abs(samples[0]-samples[samples.Length-1])<=maxStep*1.1+.0001,"Horn loop seam exceeds interior waveform steps");
            Debug.Log($"NF5K_HORN_PCM_OK: {clip.frequency} Hz, RMS {rms:F4}, seam {Math.Abs(samples[0]-samples[samples.Length-1]):F4}, max interior step {maxStep:F4}");
            Debug.Log("NF5K_SERVICE_CONTROL_OK: two real rays/context, momentary cap/hold/release, horn playback/fade, sand to backend, power/air/distance/disable guards, bounded non-silent PCM");
        }
        finally
        {
            foreach(var c in controls){c.Release();c.enabled=true;}audio.Refresh(1);driver.RestoreCheckpoint(snapshot);cam.transform.SetPositionAndRotation(position,rotation);focus.SetValue(ui,old);
        }
    }
    public static void CaptureBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");UnityEngine.Object.FindAnyObjectByType<CabDaylightEnvironment>().Apply(CabDaylightEnvironment.DayPreset.Overcast);NF5KBogieDetailReview.ConfigureInspectionLighting();
            var annunciator=UnityEngine.Object.FindAnyObjectByType<CabAnnunciatorPanel>();
            foreach(var material in annunciator.Lenses.Concat(annunciator.Legends).Select(r=>r.sharedMaterial).Distinct())NF5KAnnunciatorSetup.EnableEmission(material);AssetDatabase.SaveAssets();annunciator.ApplyReading(null,false);
            var c=UnityEngine.Object.FindObjectsByType<CabServiceControl>(FindObjectsSortMode.None).Single(c=>!c.Sanding);var root=c.transform.parent;var cam=Camera.main;
            cam.fieldOfView=40;cam.transform.position=root.position+new Vector3(-.10f,.27f,.17f);cam.transform.LookAt(root.position,Vector3.back);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/horn_sand_controls_v2.png");
            cam.fieldOfView=64;cam.transform.position=new Vector3(.40f,1.38f,.94f);cam.transform.LookAt(root.position+new Vector3(-.20f,0,-.08f));
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/horn_sand_console_v2.png");
            Debug.Log("NF5K_SERVICE_CONTROL_CAPTURE_OK");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
