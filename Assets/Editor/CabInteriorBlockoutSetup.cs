using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using RailwayRenaissance.Cab;

public static class CabInteriorBlockoutSetup
{
    const string ScenePath="Assets/Scenes/CabInteriorBlockout.unity";
    const string Output="tools/cab_reference/interior_review/";
    static int frames;
    public static void BatchBuild()
    {
        try
        {
            Build();EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            frames=0;EditorApplication.update+=Review;
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Review()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try
        {
            if(++frames==30)
            {
                var camera=Camera.main;
                Shot(camera,Output+"driver_view.png");
                var eyePosition=camera.transform.position;var eyeRotation=camera.transform.rotation;
                camera.transform.position=new Vector3(12,8,-11);
                camera.transform.LookAt(new Vector3(0,.65f,8.0f));camera.orthographic=true;camera.orthographicSize=7.0f;
                Shot(camera,Output+"exterior_reference_view.png");
                camera.transform.position=new Vector3(7,3.8f,-7);camera.transform.LookAt(new Vector3(0,.65f,1.4f));camera.orthographicSize=3.8f;
                Shot(camera,Output+"front_detail_view.png");
                camera.transform.position=new Vector3(0,1.05f,-5);camera.transform.LookAt(new Vector3(0,.85f,-.65f));camera.orthographicSize=1.85f;
                Shot(camera,Output+"front_livery_view.png");
                camera.transform.position=new Vector3(2.2f,.75f,-3.5f);camera.transform.LookAt(new Vector3(.10f,-.40f,-.95f));camera.orthographicSize=.90f;
                Shot(camera,Output+"front_mechanical_view.png");
                NF5KFrontMechanical.ValidateReload();
                camera.transform.position=new Vector3(5,.85f,3.45f);camera.transform.LookAt(new Vector3(1.46f,.70f,3.45f));camera.orthographicSize=.65f;
                Shot(camera,Output+"sakura_livery_view.png");
                camera.transform.position=new Vector3(4, .15f, 4.5f);camera.transform.LookAt(new Vector3(0,-.65f,2.5f));camera.orthographicSize=1.75f;
                Shot(camera,Output+"powered_bogie_view.png");
                camera.transform.position=new Vector3(6,3,21);camera.transform.LookAt(new Vector3(0,.9f,16));camera.orthographicSize=3.5f;
                Shot(camera,Output+"rear_complete_view.png");
                camera.transform.position=new Vector3(5,1.65f,.25f);
                camera.transform.LookAt(new Vector3(1.45f,.85f,1.48f));camera.orthographicSize=1.60f;
                Shot(camera,Output+"door_reference_view.png");
                var junctionPosition=camera.transform.position;var junctionRotation=camera.transform.rotation;float junctionSize=camera.orthographicSize;
                camera.transform.position=new Vector3(3.8f,1.70f,.85f);camera.transform.LookAt(new Vector3(1.47f,1.50f,1.08f));camera.orthographicSize=.65f;
                Shot(camera,Output+"door_window_junction_view.png");
                camera.transform.position=junctionPosition;camera.transform.rotation=junctionRotation;camera.orthographicSize=junctionSize;
                var leftDoor=GameObject.Find("Left_Door_Pivot").GetComponent<CabSideDoor>();
                leftDoor.Open();Shot(camera,Output+"door_open_view.png");leftDoor.Close();
                NF5KDoorReview.Validate();
                NF5KLightingReview.Render(camera,Shot);
                NF5KInteriorLightingReview.Render(camera,Shot);
                camera.orthographic=false;camera.transform.position=eyePosition;camera.transform.rotation=eyeRotation;
                var hidden=new List<GameObject>();
                foreach(var name in new[]{"Roof","Interior_Lighting_Assembly","Rear_Assembly","Left_Upper_Wall","Left_Lower_Wall","Left_Door_Pivot","Left_Header"})
                {
                    var obj=GameObject.Find(name);if(obj!=null){hidden.Add(obj);obj.SetActive(false);}
                }
                camera.transform.position=new Vector3(5,5,8.5f);
                camera.transform.LookAt(new Vector3(0,1,2.6f));camera.orthographic=true;camera.orthographicSize=3.6f;
                Shot(camera,Output+"cutaway_view.png");
                foreach(var obj in hidden)obj.SetActive(true);
                camera.orthographic=false;camera.transform.position=new Vector3(0,1.45f,.15f);
                camera.transform.LookAt(new Vector3(0,1.30f,1.91f));camera.fieldOfView=90;
                Shot(camera,Output+"rear_wall_view.png");
                foreach(var side in new[]{"Left","Right"})
                {
                    if(GameObject.Find(side+"_Door_Leaf_Fill")!=null||GameObject.Find(side+"_Door_Window_Fill")==null)
                        throw new InvalidOperationException("Steel door window opening was lost on scene reload");
                    if(GameObject.Find(side+"_Access_Step_2")==null)
                        throw new InvalidOperationException("Access ladder was lost on scene reload");
                }
                if(AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/Cab/NF5KBodyMeshes/Barrel_Roof.asset")==null)
                    throw new InvalidOperationException("Curved roof mesh was not persisted");
                NF5KRunningGear.ValidateReload();
                Debug.Log("NF5K_REFERENCE_RELOAD_AUDIT_OK: two open-window steel leaves, access ladders, persistent curved roof");
                Debug.Log("CAB_INTERIOR_RELOAD_RENDER_OK");
            }
            if(frames>=40){EditorApplication.update-=Review;NF5KLightingPlayReview.Begin();}
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.update-=Review;EditorApplication.Exit(1);}
    }
    static void Shot(Camera camera,string path)
    {
        const int width=1400,height=1000,scale=3;
        var target=new RenderTexture(width*scale,height*scale,24){filterMode=FilterMode.Bilinear};
        var resolved=new RenderTexture(width,height,0);
        var previous=RenderTexture.active;var previousTarget=camera.targetTexture;
        Texture2D image=null;
        try
        {
            camera.targetTexture=target;camera.Render();
            // A 3:1 bilinear reduction alone misses source pixels; explicitly average all nine samples.
            var shader=Shader.Find("Hidden/NF5K/ReviewDownsample");
            if(shader==null)throw new InvalidOperationException("Review downsample shader is missing");
            var material=new Material(shader);
            try{Graphics.Blit(target,resolved,material);}finally{UnityEngine.Object.DestroyImmediate(material);}
            RenderTexture.active=resolved;
            image=new Texture2D(width,height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=previousTarget;RenderTexture.active=previous;
            target.Release();resolved.Release();
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(resolved);
            if(image!=null)UnityEngine.Object.DestroyImmediate(image);
        }
    }
    [MenuItem("RailRevival/Cab/Build Complete Interior Blockout")]
    public static void Build()
    {
        Directory.CreateDirectory(Output);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("CabInterior_Blockout");
        Material Mat(string name,Color color,float metallic=0)
        {
            string path="Assets/Resources/Cab/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",.2f);EditorUtility.SetDirty(mat);return mat;
        }
        var wall=Mat("Interior_Blockout_Wall",new Color(.36f,.43f,.39f));
        var floor=Mat("Interior_Blockout_Floor",new Color(.15f,.17f,.16f));
        var trim=Mat("Interior_Blockout_Trim",new Color(.08f,.10f,.09f));
        var seat=Mat("Interior_Blockout_Seat",new Color(.12f,.15f,.14f));
        var glass=Mat("Interior_Blockout_Glass",new Color(.65f,.8f,.78f,.08f));
        var blue=Mat("Interior_NF5_Blue_Exterior",new Color(.055f,.145f,.32f));
        var cream=Mat("Interior_NF5_Cream_Exterior",new Color(.84f,.81f,.73f));
        var stripe=Mat("Interior_NF5_Red_Stripe",new Color(.70f,.045f,.085f));
        var hardware=Mat("Interior_NF5_Hardware",new Color(.33f,.35f,.36f),.75f);
        glass.SetFloat("_Surface",1);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        glass.SetFloat("_ZWrite",0);glass.SetFloat("_Smoothness",.93f);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=3000;glass.SetShaderPassEnabled("ShadowCaster",false);
        GameObject Box(string name,Vector3 position,Vector3 size,Material material,Transform parent=null,bool solid=true)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.position=position;obj.transform.localScale=size;
            obj.transform.SetParent(parent==null?root.transform:parent,true);obj.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid)UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());return obj;
        }
        Box("Floor",new Vector3(0,-.045f,.7f),new Vector3(2.8f,.09f,2.4f),floor);
        NF5KReferenceBody.Build(root.transform,blue,cream,stripe,trim,glass,hardware);
        var rear=new GameObject("Rear_Assembly");rear.transform.SetParent(root.transform);
        foreach(float x in new[]{-.855f,.855f})
            Box("Rear_Passenger_Partition",new Vector3(x,1.05f,1.91f),new Vector3(1.09f,2.1f,.08f),wall,rear.transform);
        Box("Rear_Passenger_Door_Header",new Vector3(0,2f,1.91f),new Vector3(.62f,.20f,.08f),wall,rear.transform);
        var passage=new GameObject("Passenger_Passage_Door_Pivot");passage.transform.SetParent(rear.transform);passage.transform.position=new Vector3(-.30f,0,1.91f);
        Box("Passenger_Passage_Door_Lower",new Vector3(0,.50f,1.91f),new Vector3(.58f,.96f,.045f),wall,passage.transform);
        Box("Passenger_Passage_Door_Glass",new Vector3(0,1.41f,1.91f),new Vector3(.50f,.76f,.009f),glass,passage.transform,false);
        Box("Passenger_Door_Handle",new Vector3(.23f,.98f,1.87f),new Vector3(.07f,.025f,.025f),trim,passage.transform);
        Box("Rear_Electrical_Service_Panel",new Vector3(.85f,1.48f,1.85f),new Vector3(.65f,.35f,.035f),trim,rear.transform);
        var passenger=new GameObject("Integrated_Passenger_Bay_Blockout");passenger.transform.SetParent(root.transform);
        for(int module=0;module<4;module++)Box("Passenger_Floor_Module_"+module,new Vector3(0,-.045f,3.85f+module*3.9f),new Vector3(2.8f,.09f,3.9f),floor,passenger.transform);
        foreach(float x in new[]{-1.4f,1.4f})
        {
            Box("Passenger_Lower_Wall",new Vector3(x,.5f,8.98f),new Vector3(.07f,1f,14.16f),wall,passenger.transform);
            Box("Passenger_Rear_End_Lower_Wall",new Vector3(x,.5f,17.22f),new Vector3(.07f,1f,.56f),wall,passenger.transform);
            for(int i=0;i<12;i++)
            {
                float z=2.8f+i*1.1f;
                Box("Passenger_Window",new Vector3(x,1.5f,z),new Vector3(.009f,.80f,.94f),glass,passenger.transform,false);
                Box("Passenger_Window_Post",new Vector3(x,1.5f,z+.51f),new Vector3(.07f,1f,.10f),wall,passenger.transform);
            }
            Box("Passenger_Header",new Vector3(x,2.04f,9.7f),new Vector3(.07f,.15f,15.6f),wall,passenger.transform);
        }
        for(int row=0;row<12;row++)foreach(float x in new[]{-.84f,.84f})
        {
            float z=2.85f+row*1.1f;
            Box("Passenger_Seat_Block",new Vector3(x,.48f,z),new Vector3(.95f,.12f,.48f),seat,passenger.transform);
            Box("Passenger_Seat_Back",new Vector3(x,.80f,z+.23f),new Vector3(.95f,.56f,.08f),seat,passenger.transform);
        }
        Box("Ceiling_Light_Base",new Vector3(0,2.085f,.65f),new Vector3(.35f,.035f,.16f),trim);
        Box("Ceiling_Light_Diffuser",new Vector3(0,2.06f,.65f),new Vector3(.30f,.015f,.12f),wall);
        foreach(float x in new[]{-.75f,.75f})
        {
            var fan=GameObject.CreatePrimitive(PrimitiveType.Cylinder);fan.name="Ceiling_Fan_Reserved";fan.transform.SetParent(root.transform);fan.transform.position=new Vector3(x,2.03f,.25f);fan.transform.localScale=new Vector3(.23f,.025f,.23f);fan.GetComponent<Renderer>().sharedMaterial=trim;
        }
        Box("Driver_Sun_Visor_Reserved",new Vector3(.65f,1.93f,-.35f),new Vector3(.63f,.17f,.015f),trim);
        foreach(float x in new[]{.65f,-.85f})
        {
            string name=x>0?"Driver":"Assistant";
            var pivot=new GameObject(name+"_Seat_Pivot");pivot.transform.SetParent(root.transform);pivot.transform.position=new Vector3(x,0,.93f);
            Box(name+"_Seat_Pedestal",new Vector3(x,.22f,.93f),new Vector3(.20f,.44f,.20f),trim,pivot.transform);
            Box(name+"_Seat_Cushion",new Vector3(x,.49f,.93f),new Vector3(.46f,.10f,.46f),seat,pivot.transform);
            Box(name+"_Seat_Back",new Vector3(x,.78f,1.16f),new Vector3(.46f,.54f,.09f),seat,pivot.transform);
        }
        Box("Driver_Footrest",new Vector3(.65f,.09f,.43f),new Vector3(.48f,.035f,.24f),trim);
        Box("Vigilance_Pedal_Reserved",new Vector3(.24f,.12f,.43f),new Vector3(.09f,.025f,.14f),wall);
        var flag=new GameObject("DPRK_Flag_Plaque_Reserved");flag.transform.SetParent(root.transform);flag.transform.position=new Vector3(-1.34f,1.12f,.95f);
        var consolePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Cab/MainConsoleStudy.prefab");
        if(consolePrefab==null)throw new InvalidOperationException("Main console prefab missing");
        var console=(GameObject)PrefabUtility.InstantiatePrefab(consolePrefab);console.transform.SetParent(root.transform,true);console.transform.position+=Vector3.right*.55f;
        var camera=new GameObject("Driver_View_Camera").AddComponent<Camera>();camera.tag="MainCamera";
        camera.transform.position=new Vector3(.65f,1.38f,.78f);camera.transform.LookAt(new Vector3(.60f,1.22f,-.8f));
        camera.fieldOfView=75;camera.nearClipPlane=.025f;camera.farClipPlane=40;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.72f,.78f,.80f);camera.cullingMask=1<<30;
        foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
        var pointer=console.GetComponent<CabStartupPointer>();pointer.Configure(camera,1<<30);PrefabUtility.RecordPrefabInstancePropertyModifications(pointer);
        var light=new GameObject("Interior_Review_Key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(45,-30,0);light.cullingMask=1<<30;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.45f,.45f,.45f);
        NF5KSurfaceFinish.Build(root.transform);
        NF5KIndustrialFinish.Build(root.transform);
        NF5KInteriorLightingSetup.Build(root.transform);
        light.shadows=LightShadows.Soft;light.shadowStrength=.70f;light.shadowBias=.02f;light.shadowNormalBias=.02f;light.intensity=.90f;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.34f,.37f,.40f);RenderSettings.ambientEquatorColor=new Color(.20f,.22f,.24f);RenderSettings.ambientGroundColor=new Color(.07f,.075f,.08f);
        var fill=new GameObject("Industrial_Review_Fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.28f;fill.color=new Color(.80f,.87f,1);fill.transform.rotation=Quaternion.Euler(20,-110,0);fill.cullingMask=1<<30;
        var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="Review_Ground_Not_Vehicle";ground.layer=30;
        ground.transform.position=new Vector3(0,-1.39f,8);ground.transform.localScale=new Vector3(18,.04f,32);
        ground.GetComponent<Renderer>().sharedMaterial=Mat("Industrial_Review_Ground",new Color(.57f,.59f,.60f));
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
        var state=console.GetComponent<CabEngineState>();
        if(state==null||console.GetComponentsInChildren<CabStartupControls>().Length!=1)throw new InvalidOperationException("Interior lost console startup/state binding");
        if(camera.transform.position.y<1.05f||camera.transform.position.y>1.8f)throw new InvalidOperationException("Driver eye coordinate invalid");
        Debug.Log("CAB_INTERIOR_BLOCKOUT_OK: estimated 2.8m internal width; left desk only; two side doors; passenger passage; startup retained");
        // NF-5K is a combined driving/passenger head car, not two cabs on a short locomotive.
        Debug.Log("NF5K_HEADCAR_LAYOUT_OK: single driving compartment and integrated passenger bay; opposite end deferred to consist layout");

    }
}
