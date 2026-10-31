using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KWorkstationLightAudit
{
    public static void BatchInspect()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            Object.FindFirstObjectByType<CabDaylightEnvironment>().Apply(CabDaylightEnvironment.DayPreset.Overcast);
            NF5KBogieDetailReview.ConfigureInspectionLighting();
            var lighting=Object.FindFirstObjectByType<CabInteriorLighting>();
            var camera=Camera.main;camera.orthographic=false;camera.fieldOfView=47;
            camera.transform.position=new Vector3(-.58f,1.16f,.40f);camera.transform.LookAt(new Vector3(-.58f,.850f,.12f));
            lighting.SetAssistantTask(false);
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/record_task_off_audit.png");
            for(int i=0;i<lighting.Fixtures.Length;i++)if(lighting.Fixtures[i].circuit==InteriorLightCircuit.AssistantTask)lighting.Fixtures[i].intensity=.5f;
            lighting.SetAssistantTask(true);
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/record_task_half_audit.png");
            Debug.Log("NF5K_WORKSTATION_LIGHT_AUDIT_OK: temporary task off / intensity 0.5 comparison; no scene save");
            EditorApplication.Exit(0);
        }
        catch(System.Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
