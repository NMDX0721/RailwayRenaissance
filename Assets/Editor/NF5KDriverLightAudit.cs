using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RailwayRenaissance.Cab;

public static class NF5KDriverLightAudit
{
    public static void Batch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            var env=UnityEngine.Object.FindAnyObjectByType<CabDaylightEnvironment>();var cam=Camera.main;
            foreach(CabDaylightEnvironment.DayPreset preset in Enum.GetValues(typeof(CabDaylightEnvironment.DayPreset)))
            {
                env.Apply(preset);env.SetView(0);
                NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/driver_light_"+preset+".png");
                var lighting=UnityEngine.Object.FindAnyObjectByType<CabInteriorLighting>();lighting.SetCabPreset(0);
                UnityEngine.Object.FindAnyObjectByType<CabDiffuseFill>()?.Refresh();
                NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/driver_light_"+preset+"_ceiling.png");
                lighting.AllOff();
            }
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                Debug.Log("DRIVER_LIGHT_SOURCE "+light.name+" enabled="+light.enabled+" type="+light.type+" intensity="+light.intensity+" shadows="+light.shadows+" mask="+light.cullingMask);
            SessionState.SetBool("NF5KDriverLightAuditCapture",true);
            EditorSceneManager.OpenScene("Assets/Scenes/CabRailMotionReview.unity");NF5KSimulationPlayReview.Begin();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void CaptureRuntime(CabEngineState state)
    {
        if(!SessionState.GetBool("NF5KDriverLightAuditCapture",false))return;
        SessionState.SetBool("NF5KDriverLightAuditCapture",false);
        var body=state.GetComponentInParent<CabInteriorLighting>().transform;var cam=Camera.main;
        var pos=cam.transform.position;var rot=cam.transform.rotation;float fov=cam.fieldOfView;
        try
        {
            cam.fieldOfView=65;cam.transform.position=body.TransformPoint(new Vector3(.65f,1.38f,.78f));cam.transform.LookAt(body.TransformPoint(new Vector3(.60f,1.22f,-.8f)),body.up);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/driver_light_runtime_forward.png");
            cam.transform.LookAt(state.GetComponentInChildren<CabAnnunciatorPanel>().transform.position,body.up);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/driver_light_runtime_panel.png");
            cam.transform.position=body.TransformPoint(new Vector3(1.1f,.36f,.73f));cam.transform.LookAt(body.TransformPoint(new Vector3(.7f,.06f,.46f)),body.up);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/driver_light_runtime_footwell.png");
        }
        finally{cam.transform.SetPositionAndRotation(pos,rot);cam.fieldOfView=fov;}
    }
}
