using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KExteriorLightControlReview
{
    static void Assert(bool pass,string why){if(!pass)throw new InvalidOperationException(why);}
    public static void Validate(CabEngineState state)
    {
        var controls=state.GetComponentsInChildren<CabExteriorLightControl>();Assert(controls.Length==2,"Two single-cab lighting selectors required");
        var head=controls.Single(c=>!c.IsMarker);var marker=controls.Single(c=>c.IsMarker);var light=state.GetComponentInParent<CabExteriorLighting>();
        var cam=Camera.main;var pos=cam.transform.position;var rot=cam.transform.rotation;
        var previousOperation=light.OperatingMode;var previousHead=light.HeadlightLevel;var previousMarker=light.MarkerAspect;bool power=light.PowerAvailable;
        var indoor=state.GetComponentsInChildren<CabLightingKey>().OrderBy(k=>k.Index).ToArray();var indoorBefore=indoor.Select(k=>k.Selected).ToArray();
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var ui=state.GetComponent<CabContextInteraction>();var focus=typeof(CabContextInteraction).GetField("focused",flags);var oldFocus=focus.GetValue(ui);
        try
        {
            light.SetPowerAvailable(true);NF5KLightingReview.Validate(light);
            foreach(var control in controls)
            {
                var normal=control.transform.forward;cam.transform.position=control.transform.position+normal*.40f;cam.transform.LookAt(control.transform.position);Physics.SyncTransforms();
                Assert(Physics.Raycast(cam.transform.position,-normal,out var hit,.5f,1<<30)&&hit.collider.GetComponent<CabExteriorLightControl>()==control,"Cannot aim at physical lighting selector");
                focus.SetValue(ui,control);typeof(CabContextInteraction).GetMethod("BuildChoices",flags).Invoke(ui,null);
                Assert(((System.Collections.ICollection)typeof(CabContextInteraction).GetField("choices",flags).GetValue(ui)).Count==control.DetentCount+(control.IsMarker&&control.Position==5?3:0),"Detent actions missing");
                for(int i=0;i<control.DetentCount;i++)
                {
                    control.Select((i+1)%control.DetentCount);typeof(CabContextInteraction).GetMethod("BuildChoices",flags).Invoke(ui,null);
                    typeof(CabContextInteraction).GetField("selected",flags).SetValue(ui,i);typeof(CabContextInteraction).GetMethod("ExecuteSelected",flags).Invoke(ui,null);
                    Assert(control.Position==i,"Contextual lighting command failed");control.ApplyVisual();
                    Assert(Mathf.Abs(Mathf.DeltaAngle(control.Selector.localEulerAngles.z,control.IsMarker?-100+i*40:-60+i*60))<.01f,"Selector index disagrees with detent");
                }
            }
            cam.transform.position=head.transform.position+head.transform.forward*.4f;
            light.SetMode(ExteriorLightMode.Off);head.Select(2);Assert(light.Beams.Count(b=>b.enabled)==3,"Headlights cannot run with markers off");
            light.SetMarkerAspect(ExteriorMarkerAspect.White);Assert(light.Beams.Count(b=>b.enabled)==5,"White markers not independent");
            light.SetMarkerAspect(ExteriorMarkerAspect.Red);Assert(head.Position==0&&light.Beams.Count(b=>b.enabled)==2&&light.Beams.Where(b=>b.enabled).All(b=>b.color.g<.1f),"Red markers do not exclude headlights");
            head.Select(1);Assert(light.MarkerAspect==ExteriorMarkerAspect.Off&&light.Beams.Count(b=>b.enabled)==3,"Headlight command does not release red markers");
            light.SetPowerAvailable(false);Assert(light.Beams.All(b=>!b.enabled)&&head.Position==1,"Power loss erases detent or leaves light enabled");
            head.Select(2);Assert(head.Position==2&&light.Beams.All(b=>!b.enabled),"Unpowered preselection invalid");
            light.SetPowerAvailable(true);Assert(light.Beams.Count(b=>b.enabled)==3,"Supply restoration does not restore selected circuits");
            state.HeadlightOn=false;typeof(CabExteriorLighting).GetMethod("Update",flags).Invoke(light,null);Assert(light.Mode==ExteriorLightMode.Off,"Legacy off failed");
            state.HeadlightOn=true;typeof(CabExteriorLighting).GetMethod("Update",flags).Invoke(light,null);Assert(light.HeadlightLevel==ExteriorHeadlightLevel.Bright&&light.MarkerAspect==ExteriorMarkerAspect.Off,"Legacy restore loses independent selection");
            int[] counts={0,3,5,2,2};
            for(int i=0;i<counts.Length;i++)
            {
                light.SetOperatingMode((ExteriorOperatingMode)i);Assert(light.Beams.Count(b=>b.enabled)==counts[i],"Wrong operational profile lamp count "+i);
            }
            light.SetOperatingMode(ExteriorOperatingMode.DayRunning);Assert(light.Beams[0].enabled&&light.Beams.Count(b=>b.enabled)==3,"Day running must use roof and markers");
            state.HeadlightOn=false;typeof(CabExteriorLighting).GetMethod("Update",flags).Invoke(light,null);state.HeadlightOn=true;typeof(CabExteriorLighting).GetMethod("Update",flags).Invoke(light,null);
            Assert(light.OperatingMode==ExteriorOperatingMode.DayRunning&&light.Beams.Count(b=>b.enabled)==3,"Legacy switch loses operating profile");
            light.SetOperatingMode(ExteriorOperatingMode.NightRunning);head.Select(2);Assert(light.OperatingMode==ExteriorOperatingMode.Manual,"Explicit headlight override must leave preset mode");
            for(int i=0;i<indoor.Length;i++)Assert(indoor[i].Selected==indoorBefore[i],"Exterior selector changes interior circuit");
            cam.transform.position=head.transform.position+head.transform.forward*4;Assert(!head.Select(0),"Remote lighting selector accepted");
            cam.transform.position=head.transform.position+head.transform.forward*.4f;head.enabled=false;Assert(!head.Select(0),"Disabled lighting selector accepted");head.enabled=true;
            Debug.Log("NF5K_EXTERIOR_CONTROL_OK: real rays and context for 3-detent headlight and 6-detent operating controls, 5 legacy modes, day/night/shunt/tail profiles, manual override, independent circuits, red/headlight exclusion, power retention/restore, legacy exact restore, indoor isolation and guards");
        }
        finally
        {
            light.SetHeadlightLevel(previousHead);light.SetMarkerAspect(previousMarker);light.SetPowerAvailable(power);
            if(previousOperation!=ExteriorOperatingMode.Manual)light.SetOperatingMode(previousOperation);foreach(var c in controls)c.ApplyVisual();cam.transform.SetPositionAndRotation(pos,rot);focus.SetValue(ui,oldFocus);
        }
    }
    public static void CaptureBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");UnityEngine.Object.FindAnyObjectByType<CabDaylightEnvironment>().Apply(CabDaylightEnvironment.DayPreset.Overcast);
            NF5KBogieDetailReview.ConfigureInspectionLighting();UnityEngine.Object.FindAnyObjectByType<CabInteriorLighting>().AllOff();UnityEngine.Object.FindAnyObjectByType<CabDiffuseFill>()?.Refresh();
            var head=UnityEngine.Object.FindObjectsByType<CabExteriorLightControl>(FindObjectsSortMode.None).Single(c=>!c.IsMarker);var panel=head.transform.parent;var cam=Camera.main;
            cam.orthographic=false;cam.fieldOfView=37;cam.transform.position=panel.position+panel.forward*.64f+panel.up*.035f;cam.transform.LookAt(panel.position,panel.up);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/exterior_light_control_v3.png");
            cam.fieldOfView=63;cam.transform.position=new Vector3(.40f,1.38f,.94f);cam.transform.LookAt(panel.position-panel.up*.08f);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/exterior_light_control_console_v3.png");
            var bracket=GameObject.Find("Speedometer_Folded_Metal_Bracket").transform;
            cam.fieldOfView=57;cam.transform.position=bracket.position+new Vector3(-.22f,.30f,.16f);cam.transform.LookAt(bracket.position+Vector3.up*.035f);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/speedometer_bracket_v2.png");
            var light=UnityEngine.Object.FindAnyObjectByType<CabExteriorLighting>();
            light.SetOperatingMode(ExteriorOperatingMode.NightRunning);foreach(var control in UnityEngine.Object.FindObjectsByType<CabExteriorLightControl>(FindObjectsSortMode.None))control.ApplyVisual();
            cam.fieldOfView=37;cam.transform.position=panel.position+panel.forward*.64f+panel.up*.035f;cam.transform.LookAt(panel.position,panel.up);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/exterior_light_control_night_v3.png");
            cam.fieldOfView=44;cam.transform.position=new Vector3(0,1.65f,-6.8f);cam.transform.LookAt(new Vector3(0,1.4f,-.90f));
            light.SetOperatingMode(ExteriorOperatingMode.DayRunning);NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/exterior_day_profile_v3.png");
            light.SetOperatingMode(ExteriorOperatingMode.NightRunning);NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/exterior_night_profile_v3.png");
            Debug.Log("NF5K_EXTERIOR_CONTROL_CAPTURE_OK");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
