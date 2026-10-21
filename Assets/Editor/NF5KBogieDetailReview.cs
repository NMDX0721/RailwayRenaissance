using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NF5KBogieDetailReview
{
    [MenuItem("RailRevival/Cab/Refine Bogies Across Review Scenes")]
    public static void Prepare()
    {
        foreach(string path in new[]{"Assets/Scenes/CabInteriorBlockout.unity","Assets/Scenes/CabDaylightReview.unity","Assets/Scenes/CabSimulationReview.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path);
            var old=GameObject.Find("Running_Gear");
            if(old==null)throw new InvalidOperationException("No running gear in "+path);
            var parent=old.transform.parent;
            UnityEngine.Object.DestroyImmediate(old);
            NF5KRunningGear.Build(parent);NF5KRunningGear.ValidateReload();
            NF5KAccessLadder.Build(parent);
            NF5KBoardingAudit.Validate();
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("NF5K_BOGIE_DETAIL_SCENES_OK: three existing review scenes updated without rebuilding cab input or lighting");
    }
    public static void BatchBuild()
    {
        try
        {
            Prepare();
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            var day=UnityEngine.Object.FindFirstObjectByType<RailwayRenaissance.Cab.CabDaylightEnvironment>();
            day.Apply(RailwayRenaissance.Cab.CabDaylightEnvironment.DayPreset.Overcast);
            var camera=Camera.main;camera.orthographic=true;camera.orthographicSize=1.62f;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.72f,.75f,.77f);
            UnityEngine.RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            UnityEngine.RenderSettings.ambientSkyColor=new Color(.65f,.68f,.72f);
            UnityEngine.RenderSettings.ambientEquatorColor=new Color(.45f,.47f,.49f);
            UnityEngine.RenderSettings.ambientGroundColor=new Color(.26f,.27f,.29f);
            var fill=new GameObject("Bogie_Review_Soft_Fill_Not_Vehicle").AddComponent<Light>();
            fill.type=LightType.Directional;fill.intensity=.75f;fill.color=new Color(.95f,.97f,1);fill.shadows=LightShadows.None;fill.transform.rotation=Quaternion.Euler(35,-50,0);
            camera.transform.position=new Vector3(4.3f,-.15f,4.4f);camera.transform.LookAt(new Vector3(.3f,-.63f,2.5f));
            Shot(camera,"tools/cab_reference/interior_review/dished_wheel_boarding_overview.png");
            camera.transform.position=new Vector3(3.2f,-.25f,4.8f);camera.transform.LookAt(new Vector3(.7f,-.70f,3.6f));camera.orthographicSize=.78f;
            Shot(camera,"tools/cab_reference/interior_review/dished_wheel_detail.png");
            var wheel=GameObject.Find("Running_Gear").GetComponentsInChildren<MeshRenderer>();
            MeshRenderer sample=null;foreach(var renderer in wheel)if(renderer.name=="Steel_Wheel"&&renderer.transform.position.x>0){sample=renderer;break;}
            var hidden=new System.Collections.Generic.List<MeshRenderer>();
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if(renderer!=sample&&renderer.enabled){renderer.enabled=false;hidden.Add(renderer);}
            camera.transform.position=sample.bounds.center+new Vector3(1.8f,.5f,1.2f);camera.transform.LookAt(sample.bounds.center);camera.orthographicSize=.65f;
            Shot(camera,"tools/cab_reference/interior_review/wheel_profile_review_final.png");
            foreach(var renderer in hidden)renderer.enabled=true;
            camera.transform.position=new Vector3(3.1f,.18f,2.25f);camera.transform.LookAt(new Vector3(1.5f,-.35f,1.49f));camera.orthographicSize=.65f;
            Shot(camera,"tools/cab_reference/interior_review/four_step_ladder_detail.png");
            var figure=NF5KBoardingAudit.Figure();camera.transform.position=new Vector3(4.3f,.8f,3.5f);camera.transform.LookAt(new Vector3(1.5f,.10f,1.49f));camera.orthographicSize=1.55f;
            Shot(camera,"tools/cab_reference/interior_review/boarding_175cm_review_final.png");UnityEngine.Object.DestroyImmediate(figure);
            var shader=Shader.Find("RailwayRenaissance/RetroreflectiveSheet");
            if(shader==null||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Reflective sheet shader compilation failed");
            EditorSceneManager.OpenScene("Assets/Scenes/CabSimulationReview.unity");
            NF5KSimulationPlayReview.Begin();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Shot(Camera camera,string path)
    {
        var old=camera.targetTexture;var active=RenderTexture.active;
        var target=new RenderTexture(2800,2000,24,RenderTextureFormat.ARGBHalf);
        var reduced=new RenderTexture(1400,1000,0);
        var image=new Texture2D(1400,1000,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=target;camera.Render();camera.Render();Graphics.Blit(target,reduced);RenderTexture.active=reduced;
            image.ReadPixels(new Rect(0,0,1400,1000),0,0);image.Apply();Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,image.EncodeToPNG());
        }
        finally{camera.targetTexture=old;RenderTexture.active=active;target.Release();reduced.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(reduced);UnityEngine.Object.DestroyImmediate(image);}
    }
}
