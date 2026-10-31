using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;
using RailwayRenaissance.Cab.Simulation;

public static class NF5KAnnunciatorReview
{
    static void Assert(bool pass,string message){if(!pass)throw new InvalidOperationException(message);}
    public static void RuntimeBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/CabSimulationReview.unity");
        var panel=UnityEngine.Object.FindAnyObjectByType<CabAnnunciatorPanel>();
        Assert(panel.Lenses.Concat(panel.Legends).All(r=>r.sharedMaterial.IsKeywordEnabled("_EMISSION")),"Reloaded emission variant missing");
        foreach(string map in new[]{"Frost","Diffusion","MicroNormal"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath($"Assets/Resources/Cab/Cab_Annunciator_{map}_V2.png");
            Assert(importer.textureType==(map=="MicroNormal"?TextureImporterType.NormalMap:TextureImporterType.Default)&&importer.sRGBTexture==(map=="Frost")&&importer.mipmapEnabled,"Optical map settings did not persist "+map);
        }
        Debug.Log("NF5K_ANNUNCIATOR_OPTICS_RELOAD_OK: emission variants, linear diffusion, normal maps and mipmaps survive fresh process");
        NF5KSimulationPlayReview.Begin();
    }
    public static void Validate(CabEngineState state)
    {
        var panel=state.GetComponentInChildren<CabAnnunciatorPanel>();Assert(panel!=null&&panel.Lenses.Length==10&&panel.Legends.Length==10,"Ten connected windows required");
        Assert(panel.Lenses.Concat(panel.Legends).All(r=>r.sharedMaterial.IsKeywordEnabled("_EMISSION")),"URP emission shader variant lost on import");
        var temporary=panel.GetComponentsInChildren<MeshFilter>().Select(f=>{var hit=f.gameObject.AddComponent<MeshCollider>();hit.sharedMesh=f.sharedMesh;return hit;}).ToArray();
        try
        {
            Physics.SyncTransforms();
            for(int i=0;i<10;i++)
            {
                var label=panel.Legends[i];var normal=panel.transform.forward;
                Assert(Physics.Raycast(label.bounds.center+normal*.06f,-normal,out var hit,.08f,1<<30)&&hit.collider.gameObject==label.gameObject,"Annunciator legend hidden behind solid geometry "+i);
            }
        }
        finally{foreach(var hit in temporary)UnityEngine.Object.DestroyImmediate(hit);}
        var logic=new CabAnnunciatorLogic();var model=new CabSimulation(20261021);var s=model.State.Copy();var u=new SimulationInput {Master=true};var c=model.Config;
        string original=SimulationCodec.Encode(s);logic.Evaluate(s,u,c);
        Assert(logic.Powered&&logic.Active[2]&&logic.Active[3]&&logic.Active[5]&&!logic.Active[0]&&!logic.Active[9],"Cold master-on indication incorrect");
        Assert(SimulationCodec.Encode(s)==original,"Annunciator mutates backend state");
        u.Master=false;logic.Evaluate(s,u,c);Assert(!logic.Powered,"Master-off indication powered");
        u.Master=true;s.Battery=.1;logic.Evaluate(s,u,c);Assert(!logic.Powered,"Depleted battery indication powered");
        s.Battery=.9;s.Running=true;s.Rpm=c.IdleRpm;s.OilMpa=.05;s.MainKpa=620;s.Time=1;logic.Evaluate(s,u,c);
        Assert(!logic.Active[0]&&logic.Active[2]&&!logic.Active[3]&&logic.Active[9],"Startup grace or supply invalid");
        s.Time=5;logic.Evaluate(s,u,c);Assert(logic.Active[0],"Low oil missed");
        s.OilMpa=.16;s.MainKpa=640;logic.Evaluate(s,u,c);Assert(logic.Active[0]&&logic.Active[2],"Pressure chatter");
        s.OilMpa=.19;s.MainKpa=660;logic.Evaluate(s,u,c);Assert(!logic.Active[0]&&!logic.Active[2],"Pressure recovery missed");
        s.CoolantC=c.TripTemperatureC-19;logic.Evaluate(s,u,c);Assert(logic.Active[1],"High temperature missed");
        s.CoolantC=c.TripTemperatureC-23;logic.Evaluate(s,u,c);Assert(logic.Active[1],"Temperature chatter");
        s.CoolantC=c.TripTemperatureC-26;logic.Evaluate(s,u,c);Assert(!logic.Active[1],"Temperature recovery missed");
        s.ThermalTrip=true;s.Running=false;logic.Evaluate(s,u,c);Assert(logic.Active[1]&&!logic.Active[0],"Trip or stopped-oil suppression invalid");
        s.ThermalTrip=false;s.Cranking=true;s.AdhesionLimited=true;s.TrainCylinderKpa=40;u.Brake=4;logic.Evaluate(s,u,c);
        Assert(logic.Active[4]&&logic.Active[6]&&logic.Active[7]&&logic.Active[8],"Brake/adhesion/crank missing");
        s.Cranking=false;s.AdhesionLimited=false;s.TrainCylinderKpa=15;u.Brake=1;u.ParkingBrake=false;logic.Evaluate(s,u,c);
        Assert(!logic.Active[4]&&!logic.Active[5]&&logic.Active[6]&&!logic.Active[7]&&!logic.Active[8],"Independent recovery invalid");
        s.TrainCylinderKpa=9;logic.Evaluate(s,u,c);Assert(!logic.Active[6],"Air brake recovery invalid");
        s.Time=0;s.Running=true;s.OilMpa=.01;logic.Evaluate(s,u,c);Assert(!logic.Active[0],"Rewind retained stale timer");
        var driver=state.GetComponent<CabSimulationDriver>();panel.Refresh();
        var live=new CabAnnunciatorLogic();live.Evaluate(driver.Model.State,driver.Input,driver.Model.Config);
        var properties=new MaterialPropertyBlock();panel.Lenses[4].GetPropertyBlock(properties);
        Assert((properties.GetColor("_EmissionColor").maxColorComponent>0)==(live.Powered&&live.Active[4]),"Runtime emergency renderer disconnected");
        panel.enabled=false;panel.Lenses[4].GetPropertyBlock(properties);Assert(properties.GetColor("_EmissionColor").maxColorComponent==0,"Disabled illumination remains");panel.enabled=true;panel.Refresh();
        Assert(state.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Unbound_Warning_Lens_")).All(r=>!r.enabled),"Old unbound lamps visible");
        ValidateLampTest(state,panel,driver);
        Debug.Log("NF5K_ANNUNCIATOR_OK: cold/running/cranking, oil grace, pressure/temperature hysteresis, brakes, adhesion, supply, rewind, real renderer, disable; no backend writes");
    }
    static void ValidateLampTest(CabEngineState state,CabAnnunciatorPanel panel,CabSimulationDriver driver)
    {
        var button=panel.GetComponentInChildren<CabLampTestButton>();Assert(button!=null,"Physical lamp-test button missing");
        var cam=Camera.main;var oldPosition=cam.transform.position;var oldRotation=cam.transform.rotation;
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var ui=state.GetComponent<CabContextInteraction>();var focus=typeof(CabContextInteraction).GetField("focused",flags);var previous=focus.GetValue(ui);
        bool master=driver.Input.Master;string checkpoint=driver.CaptureCheckpoint();
        var block=new MaterialPropertyBlock();var before=new Color[10];panel.Refresh();
        for(int i=0;i<10;i++){panel.Lenses[i].GetPropertyBlock(block);before[i]=block.GetColor("_EmissionColor");}
        var rest=button.Cap.localPosition;
        try
        {
            cam.transform.position=button.transform.position+button.transform.forward*.35f;cam.transform.LookAt(button.transform.position);Physics.SyncTransforms();
            Assert(Physics.Raycast(cam.transform.position,-button.transform.forward,out var hit,.45f,1<<30)&&hit.collider.GetComponent<CabLampTestButton>()==button,"Cannot aim at physical test button");
            focus.SetValue(ui,button);typeof(CabContextInteraction).GetMethod("BuildChoices",flags).Invoke(ui,null);
            Assert(((System.Collections.ICollection)typeof(CabContextInteraction).GetField("choices",flags).GetValue(ui)).Count==1,"Test action missing");
            typeof(CabContextInteraction).GetField("selected",flags).SetValue(ui,0);typeof(CabContextInteraction).GetMethod("ExecuteSelected",flags).Invoke(ui,null);
            Assert(panel.IsTesting&&!button.Press(),"Test did not start or retrigger guard missing");
            Assert(Mathf.Abs(button.Cap.localPosition.z-rest.z+.0015f)<.00001f,"Spring cap travel incorrect");
            foreach(var lens in panel.Lenses){lens.GetPropertyBlock(block);Assert(block.GetColor("_EmissionColor").maxColorComponent>0,"Test leaves a window dark");}
            float deadline=(float)typeof(CabAnnunciatorPanel).GetField("testUntil",flags).GetValue(panel);
            Assert(deadline-Time.unscaledTime>1.8f&&deadline-Time.unscaledTime<=2.01f,"Test timer not two seconds");
            typeof(CabAnnunciatorPanel).GetField("testUntil",flags).SetValue(panel,Time.unscaledTime-1);panel.Refresh();button.ApplyVisual();
            Assert(!panel.IsTesting&&button.Cap.localPosition==rest,"Expired test did not return");
            for(int i=0;i<10;i++){panel.Lenses[i].GetPropertyBlock(block);Assert(block.GetColor("_EmissionColor")==before[i],"Test erased actual indication "+i);}
            Assert(button.Press(),"Second test rejected after timeout");driver.Input.Master=false;panel.Refresh();button.ApplyVisual();
            Assert(!panel.IsTesting&&!button.Press()&&button.Cap.localPosition==rest,"Power loss leaves testing/cap active");
            foreach(var lens in panel.Lenses){lens.GetPropertyBlock(block);Assert(block.GetColor("_EmissionColor").maxColorComponent==0,"Power loss leaves test light");}
            driver.Input.Master=master;panel.Refresh();Assert(!panel.IsTesting,"Supply restoration resumes stale test");
            cam.transform.position=button.transform.position+button.transform.forward*4;Assert(!button.Press(),"Remote test accepted");
            cam.transform.position=button.transform.position+button.transform.forward*.35f;button.enabled=false;Assert(!button.Press(),"Disabled button accepted");button.enabled=true;
            Assert(driver.CaptureCheckpoint()==checkpoint,"Lamp test changed driving commands or backend");
            Debug.Log("NF5K_LAMP_TEST_OK: physical ray/context, ten windows, two-second deadline, spring travel, timeout restores real signals, no retrigger, power loss/no stale restart, reach/disable guards; checkpoint unchanged");
        }
        finally{driver.Input.Master=master;panel.CancelLampTest();panel.Refresh();button.enabled=true;button.ApplyVisual();cam.transform.SetPositionAndRotation(oldPosition,oldRotation);focus.SetValue(ui,previous);}
    }
    public static void CaptureBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            UnityEngine.Object.FindAnyObjectByType<CabDaylightEnvironment>().Apply(CabDaylightEnvironment.DayPreset.Overcast);NF5KBogieDetailReview.ConfigureInspectionLighting();
            UnityEngine.Object.FindAnyObjectByType<CabInteriorLighting>().AllOff();UnityEngine.Object.FindAnyObjectByType<CabDiffuseFill>()?.Refresh();
            var panel=UnityEngine.Object.FindAnyObjectByType<CabAnnunciatorPanel>();var root=panel.transform;var cam=Camera.main;
            foreach(var material in panel.Lenses.Concat(panel.Legends).Select(r=>r.sharedMaterial).Distinct())NF5KAnnunciatorSetup.EnableEmission(material);AssetDatabase.SaveAssets();
            foreach(string map in new[]{"Frost","Diffusion","MicroNormal"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath($"Assets/Resources/Cab/Cab_Annunciator_{map}_V2.png");importer.userData="CabAnnunciatorOpticalMap";
                importer.textureType=map=="MicroNormal"?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=map=="Frost";importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.SaveAndReimport();
            }
            cam.fieldOfView=36;cam.transform.position=root.position+root.forward*.73f-root.up*.035f;cam.transform.LookAt(root.position-root.up*.035f,root.up);
            panel.ApplyReading(null,false);NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/annunciator_unpowered_v6.png");
            var m=new CabSimulation(20261021);var u=new SimulationInput {Master=true,FuelPump=true,EngineRun=true,Brake=1};var logic=new CabAnnunciatorLogic();
            logic.Evaluate(m.State,u,m.Config);panel.ApplyReading(logic.Active,logic.Powered);NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/annunciator_cold_v6.png");
            for(int i=0;i<15000;i++)m.Step(u);logic.Evaluate(m.State,u,m.Config);panel.ApplyReading(logic.Active,logic.Powered);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/annunciator_running_v6.png");
            var lamps=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);var intensities=lamps.Select(l=>l.intensity).ToArray();var ambient=RenderSettings.ambientMode;var ambientColor=RenderSettings.ambientLight;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.015f,.018f,.023f);
            foreach(var lamp in lamps)lamp.intensity*=.10f;
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/annunciator_dark_v6.png");
            for(int i=0;i<lamps.Length;i++)lamps[i].intensity=intensities[i];RenderSettings.ambientMode=ambient;RenderSettings.ambientLight=ambientColor;
            cam.fieldOfView=63;cam.transform.position=new Vector3(.40f,1.38f,.94f);cam.transform.LookAt(root.position-root.up*.11f);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/annunciator_console_v6.png");
            Debug.Log("NF5K_ANNUNCIATOR_CAPTURE_OK: unpowered, actual model cold-start and 300-second idle; temporary inspection light only");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void CaptureTestBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabSimulationReview.unity");
            var daylight=UnityEngine.Object.FindAnyObjectByType<CabDaylightEnvironment>();if(daylight!=null)daylight.Apply(CabDaylightEnvironment.DayPreset.Overcast);
            NF5KBogieDetailReview.ConfigureInspectionLighting();
            var driver=UnityEngine.Object.FindAnyObjectByType<CabSimulationDriver>();driver.EnsureInitialized();
            var startup=driver.GetComponentInChildren<CabStartupControls>();startup.InsertKey();Assert(startup.SetMaster(true),"Preview master could not be enabled");driver.Advance(.02);
            var panel=driver.GetComponentInChildren<CabAnnunciatorPanel>();var button=panel.GetComponentInChildren<CabLampTestButton>();var cam=Camera.main;var root=panel.transform;
            cam.transform.position=button.transform.position+button.transform.forward*.35f;cam.transform.LookAt(button.transform.position);
            string before=driver.CaptureCheckpoint();Assert(button.Press(),"Actual preview test press rejected");
            cam.fieldOfView=36;cam.transform.position=root.position+root.forward*.73f-root.up*.035f;cam.transform.LookAt(root.position-root.up*.035f,root.up);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/annunciator_lamp_test_v1.png");
            panel.CancelLampTest();panel.Refresh();button.ApplyVisual();
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/annunciator_after_test_v1.png");
            cam.fieldOfView=34;cam.transform.position=button.transform.position+button.transform.forward*.24f+button.transform.up*.012f;cam.transform.LookAt(button.transform.position+button.transform.up*.012f,button.transform.up);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/annunciator_test_button_v1.png");
            Assert(driver.CaptureCheckpoint()==before,"Preview test changed simulation");Debug.Log("NF5K_LAMP_TEST_CAPTURE_OK: actual powered button press and restored cold indications, no simulation change");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
