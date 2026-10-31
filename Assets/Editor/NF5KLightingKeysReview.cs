using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KLightingKeysReview
{
    static void Assert(bool value,string why){if(!value)throw new InvalidOperationException(why);}
    public static void Validate(CabEngineState state)
    {
        var keys=state.GetComponentsInChildren<CabLightingKey>().OrderBy(k=>k.Index).ToArray();
        Assert(keys.Length==10&&keys.Select(k=>k.Index).Distinct().Count()==10,"Ten unique lighting key bindings missing");
        var l=state.GetComponentInParent<CabInteriorLighting>();var cam=Camera.main;var pos=cam.transform.position;var rot=cam.transform.rotation;
        bool[] before=keys.Select(k=>k.Selected).ToArray();bool main=l.MainPower,battery=l.BatteryPower;float dimmer=l.InstrumentDimmer;
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        float savedDimmer=(float)typeof(CabInteriorLighting).GetField("lastInstrumentDimmer",flags).GetValue(l);
        var ui=state.GetComponent<CabContextInteraction>();var focus=typeof(CabContextInteraction).GetField("focused",flags);var prior=focus.GetValue(ui);
        string language=CabLightingKey.English?"en":"zh";
        try
        {
            for(int i=0;i<10;i++)
            {
                var k=keys[i];cam.transform.position=k.transform.position+new Vector3(0,.28f,.12f);cam.transform.LookAt(k.transform.position);Physics.SyncTransforms();
                Assert(Physics.Raycast(cam.transform.position,(k.transform.position-cam.transform.position).normalized,out var hit,.5f,1<<30)&&hit.collider.GetComponentInParent<CabLightingKey>()==k,"Cannot aim at individual key "+i);
                if(i>=7){Assert(!k.SetSelected(true)&&!k.Selected,"Spare key changes circuit");continue;}
                foreach(var other in keys.Take(7))other.SetSelected(false);
                l.SetSupply(false,false);Assert(k.SetSelected(true)&&k.Selected,"Unpowered switch must retain selection "+i);
                Assert(keys.Take(7).Count(t=>t.Selected)==1,"Key affects another lighting circuit");
                k.ApplyVisual(true);Assert(Mathf.Abs(Mathf.DeltaAngle(k.Pivot.localEulerAngles.x,22))<.01f,"Selected key pose incorrect");
                Assert(!l.Fixtures.Any(f=>f.circuit!=InteriorLightCircuit.Emergency&&f.light!=null&&f.light.enabled),"Normal fixture lights without supply");
                l.SetSupply(true,false);
                var expected=new[]{InteriorLightCircuit.CabCeiling,InteriorLightCircuit.Instrument,InteriorLightCircuit.Task,InteriorLightCircuit.AssistantTask,InteriorLightCircuit.Access,InteriorLightCircuit.PassengerFront,InteriorLightCircuit.PassengerRear}[i];
                Assert(l.Fixtures.Any(f=>f.circuit==expected&&f.light!=null&&f.light.enabled),"Selected key does not illuminate actual fixture "+i);
                Assert(!l.Fixtures.Any(f=>f.circuit!=expected&&f.light!=null&&f.light.enabled),"A different fixture circuit lights with key "+i);
                Assert(k.SetSelected(false)&&!k.Selected,"Switch off failed");
            }
            cam.transform.position=keys[1].transform.position+Vector3.up*.3f;
            l.SetInstrumentDimmer(.37f);keys[1].SetSelected(false);keys[1].SetSelected(true);Assert(Mathf.Abs(l.InstrumentDimmer-.37f)<.0001f,"Instrument key loses dimmer level");
            focus.SetValue(ui,keys[0]);typeof(CabContextInteraction).GetMethod("BuildChoices",flags).Invoke(ui,null);
            Assert(((System.Collections.ICollection)typeof(CabContextInteraction).GetField("choices",flags).GetValue(ui)).Count==2,"Lighting choices missing");
            typeof(CabContextInteraction).GetField("selected",flags).SetValue(ui,0);typeof(CabContextInteraction).GetMethod("ExecuteSelected",flags).Invoke(ui,null);
            Assert(keys[0].Selected,"Context action does not reach lighting circuit");
            l.SetCabCeiling(false);keys[0].ApplyVisual(true);Assert(Mathf.Abs(Mathf.DeltaAngle(keys[0].Pivot.localEulerAngles.x,-22))<.01f,"External lighting changes do not sync physical switch");
            CabLightingKey.SetLanguage("en-US");Assert(keys[0].Label=="Cab ceiling"&&keys[7].Hint.Contains("Not connected"),"English label/status missing");
            CabLightingKey.SetLanguage("zh-CN");Assert(keys[0].Label=="司机室顶灯"&&keys[7].Hint.Contains("未接线"),"Chinese label/status missing");
            cam.transform.position=keys[0].transform.position+Vector3.up*4;Assert(!keys[0].SetSelected(true),"Remote operation accepted");
            cam.transform.position=keys[0].transform.position+Vector3.up*.3f;keys[0].enabled=false;Assert(!keys[0].SetSelected(true),"Disabled operation accepted");keys[0].enabled=true;
            Debug.Log("NF5K_LIGHTING_KEYS_OK: 10 unique rays, 7 isolated real fixture circuits, 3 disconnected spares, supply retention, dimmer restore, contextual command, external sync, zh/en and distance/disable guards");
        }
        finally
        {
            cam.transform.position=keys[0].transform.position+Vector3.up*.3f;
            for(int i=0;i<7;i++)keys[i].SetSelected(before[i]);
            l.SetInstrumentDimmer(dimmer);typeof(CabInteriorLighting).GetField("lastInstrumentDimmer",flags).SetValue(l,savedDimmer);
            l.SetSupply(main,battery);foreach(var k in keys)k.ApplyVisual(true);
            cam.transform.SetPositionAndRotation(pos,rot);focus.SetValue(ui,prior);CabLightingKey.SetLanguage(language);
        }
    }
    public static void CaptureBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            UnityEngine.Object.FindAnyObjectByType<CabDaylightEnvironment>().Apply(CabDaylightEnvironment.DayPreset.Overcast);
            NF5KBogieDetailReview.ConfigureInspectionLighting();
            var keys=UnityEngine.Object.FindObjectsByType<CabLightingKey>(FindObjectsSortMode.None).OrderBy(k=>k.Index).ToArray();
            var cam=Camera.main;var center=(keys[0].transform.position+keys[9].transform.position)*.5f;
            UnityEngine.Object.FindAnyObjectByType<CabInteriorLighting>().AllOff();
            UnityEngine.Object.FindAnyObjectByType<CabDiffuseFill>()?.Refresh();
            var fill=new GameObject("Temporary_Key_Inspection_Fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.08f;fill.transform.rotation=Quaternion.Euler(40,100,0);fill.shadows=LightShadows.None;
            cam.orthographic=false;cam.fieldOfView=40;cam.transform.position=center+new Vector3(.02f,.44f,.34f);cam.transform.LookAt(center+new Vector3(0,0,.04f));
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/lighting_keys_text_close_v11.png");
            cam.fieldOfView=62;cam.transform.position=center+new Vector3(.17f,.52f,.63f);cam.transform.LookAt(center+new Vector3(.10f,0,-.06f));
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/lighting_keys_console_text_close_v11.png");
            Debug.Log("NF5K_LIGHTING_KEYS_CAPTURE_OK");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void RepositionLegendsBatch()
    {
        try
        {
            foreach(string name in new[]{"CabInteriorBlockout","CabDaylightReview","CabSimulationReview","CabRailMotionReview"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                var root=GameObject.Find("NF5K_Lighting_Key_Plates").transform;
                var back=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("Frame_Long_",StringComparison.Ordinal)).OrderBy(r=>r.bounds.center.z).First();
                var legends=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.EndsWith("_Legend",StringComparison.Ordinal)).ToArray();Assert(legends.Length==10,"Ten legend faces required");
                foreach(var label in legends){label.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;label.receiveShadows=false;var p=label.transform.position;p.z=back.bounds.min.z-.0148f+.009f;label.transform.position=p;}
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("NF5K_LEGENDS_REPOSITION_OK: 10 legends in four scenes; moved 9mm toward keys");CaptureBatch();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void RefreshPrintsBatch()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            for(int i=1;i<=10;i++)
            {
                var ink=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Cab_Lighting_Key_L"+i.ToString("00")+"_Integrated_V2.mat");
                Assert(ink!=null,"Missing lighting label material");NF5KLightingKeysSetup.ConfigureInk(ink);
            }
            AssetDatabase.SaveAssets();CaptureBatch();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void RegressionBatch()
    {
        try{EditorSceneManager.OpenScene("Assets/Scenes/CabRailMotionReview.unity");NF5KSimulationPlayReview.Begin();}
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void ProbeBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            var state=UnityEngine.Object.FindAnyObjectByType<CabEngineState>();
            foreach(var t in state.GetComponentsInChildren<Transform>().Where(t=>t.name.Contains("KeyPivot")||t.name.Contains("Rocker_Key")||t.name.Contains("Switches")||t.name.Contains("Frame_Long")))
                Debug.Log("LIGHTING_KEY_NODE "+t.name+" parent="+t.parent.name+" position="+t.position.ToString("F5")+" localEuler="+t.localEulerAngles+" scale="+t.lossyScale+" mesh="+(t.GetComponent<MeshFilter>()!=null?t.GetComponent<MeshFilter>().sharedMesh.bounds.ToString():"none"));
            var type=typeof(CabEngineState).Assembly.GetType("RailwayRenaissance.Cab.CabLightingKey");
            if(type==null)throw new InvalidOperationException("Lighting key runtime component missing");
            if(state.GetComponentsInChildren(type).Length!=10)throw new InvalidOperationException("Ten individually bound lighting keys missing");
            Debug.Log("NF5K_LIGHTING_KEY_PROBE_OK");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
