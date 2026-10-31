using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KUpperCabLightingReview
{
    public static void Batch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            var env=UnityEngine.Object.FindAnyObjectByType<CabDaylightEnvironment>();var lighting=UnityEngine.Object.FindAnyObjectByType<CabInteriorLighting>();
            var fill=lighting.GetComponent<CabDiffuseFill>();var cam=Camera.main;
            lighting.SetSupply(true,true);lighting.SetCabPreset(0);
            cam.fieldOfView=82;cam.transform.position=new Vector3(.65f,1.38f,.78f);cam.transform.LookAt(new Vector3(.1f,1.96f,.25f));
            foreach(bool night in new[]{false,true})
            {
                env.Apply(CabDaylightEnvironment.DayPreset.Overcast);
                if(night)
                {
                    RenderSettings.sun.enabled=false;RenderSettings.skybox=null;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.005f,.008f,.016f);
                    RenderSettings.ambientSkyColor=new Color(.015f,.02f,.03f);RenderSettings.ambientEquatorColor=new Color(.01f,.012f,.018f);RenderSettings.ambientGroundColor=Color.black;
                }
                foreach(bool enhanced in new[]{false,true})
                {
                    fill.UpperCabBounce=enhanced?.30f:0;fill.Refresh();lighting.GetComponent<CabInteriorReflections>()?.RefreshIfChanged();
                    NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/cab_upper_"+(night?"night":"day")+(enhanced?"_after":"_before")+".png");
                    if(enhanced&&!night)
                    {
                        var rotation=cam.transform.rotation;cam.transform.LookAt(new Vector3(0,2.08f,1.65f));
                        NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/cab_upper_rear_detail.png");
                        var position=cam.transform.position;var fov=cam.fieldOfView;
                        cam.fieldOfView=55;cam.transform.position=new Vector3(.30f,1.64f,1.05f);cam.transform.LookAt(new Vector3(0,2.134f,1.42f));
                        NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/cab_access_label_close.png");
                        var fixture=Array.Find(lighting.Fixtures,f=>f.circuit==InteriorLightCircuit.CabCeiling);
                        var lamp=fixture.light.transform.parent;
                        cam.fieldOfView=48;cam.transform.position=lamp.position+new Vector3(.32f,-.22f,.32f);cam.transform.LookAt(lamp.position,Vector3.up);
                        NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/cab_lamp_mount_close.png");
                        cam.fieldOfView=58;cam.transform.position=new Vector3(.48f,1.84f,.63f);cam.transform.LookAt(new Vector3(1.30f,2.115f,.30f));
                        NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/cab_fan_ceiling_joint.png");
                        cam.transform.position=position;cam.fieldOfView=fov;cam.transform.rotation=rotation;
                    }
                }
            }
            Debug.Log("NF5K_UPPER_CAB_RENDER_OK: fixed camera, day/night paired upper-cabin diffuse comparison; no scene saves");
            EditorSceneManager.OpenScene("Assets/Scenes/CabRailMotionReview.unity");NF5KSimulationPlayReview.Begin();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
