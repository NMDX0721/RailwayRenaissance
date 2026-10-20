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
                var hidden=new List<GameObject>();
                foreach(var name in new[]{"Roof","Rear_Assembly","Left_Upper_Wall","Left_Lower_Wall","Left_Door_Pivot","Left_Header"})
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
                Debug.Log("CAB_INTERIOR_RELOAD_RENDER_OK");
            }
            if(frames>=40){EditorApplication.update-=Review;EditorApplication.Exit(0);}
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.update-=Review;EditorApplication.Exit(1);}
    }
    static void Shot(Camera camera,string path)
    {
        var target=new RenderTexture(1400,1000,24);var previous=RenderTexture.active;
        camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        var image=new Texture2D(1400,1000,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1400,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=previous;target.Release();
        UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
    }
    [MenuItem("RailRevival/Cab/Build Complete Interior Blockout")]
    public static void Build()
    {
        Directory.CreateDirectory(Output);
        var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
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
        var blue=Mat("Interior_NF5_Blue_Exterior",new Color(.025f,.09f,.28f));
        var cream=Mat("Interior_NF5_Cream_Exterior",new Color(.78f,.73f,.60f));
        var stripe=Mat("Interior_NF5_Red_Stripe",new Color(.45f,.025f,.025f));
        glass.SetFloat("_Surface",1);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        glass.SetFloat("_ZWrite",0);glass.SetFloat("_Smoothness",.93f);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=3000;glass.SetShaderPassEnabled("ShadowCaster",false);
        GameObject Box(string name,Vector3 position,Vector3 size,Material material,Transform parent=null,bool solid=true)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.position=position;obj.transform.localScale=size;
            obj.transform.SetParent(parent==null?root.transform:parent,true);obj.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid)UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());return obj;
        }
        Box("Floor",new Vector3(0,-.045f,.7f),new Vector3(2.8f,.09f,2.4f),floor);
        Box("Roof",new Vector3(0,2.15f,.7f),new Vector3(2.8f,.08f,2.4f),wall);
        Box("Front_Lower_Bulkhead",new Vector3(0,.52f,-.51f),new Vector3(2.8f,1.04f,.08f),wall);
        Box("NF5_Blue_Nose_Envelope",new Vector3(0,.59f,-.562f),new Vector3(2.8f,.85f,.025f),blue);
        Box("NF5_Cream_Lower_Band",new Vector3(0,.25f,-.580f),new Vector3(2.8f,.10f,.012f),cream);
        Box("NF5_Red_Lower_Pinstripe",new Vector3(0,.31f,-.587f),new Vector3(2.8f,.018f,.008f),stripe);
        Box("Front_Header",new Vector3(0,2.05f,-.34f),new Vector3(2.8f,.15f,.08f),wall);
        Box("Front_Center_Pillar",new Vector3(0,1.53f,-.43f),new Vector3(.08f,.92f,.085f),trim).transform.rotation=Quaternion.Euler(10,0,0);
        foreach(float x in new[]{-1.36f,1.36f})Box("Front_Corner_Pillar",new Vector3(x,1.53f,-.51f),new Vector3(.08f,.9f,.085f),wall);
        foreach(float x in new[]{-.68f,.68f})
        {
            var frame=new GameObject("Windscreen_Frame");frame.transform.SetParent(root.transform);frame.transform.position=new Vector3(x,1.53f,-.43f);
            Box("Front_Windscreen",frame.transform.position,new Vector3(1.26f,.88f,.009f),glass,frame.transform,false);
            foreach(float dx in new[]{-.64f,.64f})Box("Glass_Side_Seal",frame.transform.position+Vector3.right*dx,new Vector3(.025f,.92f,.022f),trim,frame.transform);
            foreach(float dy in new[]{-.45f,.45f})Box("Glass_Cross_Seal",frame.transform.position+Vector3.up*dy,new Vector3(1.30f,.025f,.022f),trim,frame.transform);
            frame.transform.rotation=Quaternion.Euler(10,0,0);
        }
        foreach(var side in new[]{"Left","Right"})
        {
            // Facing -Z: the driver's left is +X.
            float x=side=="Left"?1.40f:-1.40f;
            Box(side+"_Lower_Wall",new Vector3(x,.50f,.3f),new Vector3(.07f,1f,1.6f),wall);
            Box(side+"_Upper_Wall",new Vector3(x,1.55f,-.44f),new Vector3(.07f,1.1f,.14f),wall);
            Box(side+"_Window",new Vector3(x,1.52f,.34f),new Vector3(.009f,.82f,1.36f),glass,null,false);
            Box(side+"_Header",new Vector3(x,2.04f,.70f),new Vector3(.07f,.16f,2.4f),wall);
            var pivot=new GameObject(side+"_Door_Pivot");pivot.transform.SetParent(root.transform);pivot.transform.position=new Vector3(x,0,1.12f);
            Box(side+"_Door_Lower",new Vector3(x,.52f,1.43f),new Vector3(.065f,1.04f,.60f),wall,pivot.transform);
            Box(side+"_Door_Glass",new Vector3(x,1.5f,1.43f),new Vector3(.009f,.84f,.53f),glass,pivot.transform,false);
            Box(side+"_Door_Rear_Frame",new Vector3(x,1.0f,1.76f),new Vector3(.08f,2f,.08f),wall);
            Box(side+"_Door_Handle",new Vector3(x+(x<0?.05f:-.05f),.96f,1.64f),new Vector3(.03f,.035f,.10f),trim,pivot.transform);
        }
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
        Box("Passenger_Floor_First_Module",new Vector3(0,-.045f,4.0f),new Vector3(2.8f,.09f,4.2f),floor,passenger.transform);
        foreach(float x in new[]{-1.4f,1.4f})
        {
            Box("Passenger_Lower_Wall",new Vector3(x,.5f,4),new Vector3(.07f,1f,4.2f),wall,passenger.transform);
            for(int i=0;i<3;i++)
            {
                float z=2.8f+i*1.1f;
                Box("Passenger_Window",new Vector3(x,1.5f,z),new Vector3(.009f,.80f,.94f),glass,passenger.transform,false);
                Box("Passenger_Window_Post",new Vector3(x,1.5f,z+.51f),new Vector3(.07f,1f,.10f),wall,passenger.transform);
            }
            Box("Passenger_Header",new Vector3(x,2.04f,4),new Vector3(.07f,.15f,4.2f),wall,passenger.transform);
        }
        for(int row=0;row<3;row++)foreach(float x in new[]{-.84f,.84f})
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
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
        var state=console.GetComponent<CabEngineState>();
        if(state==null||console.GetComponentsInChildren<CabStartupControls>().Length!=1)throw new InvalidOperationException("Interior lost console startup/state binding");
        if(camera.transform.position.y<1.05f||camera.transform.position.y>1.8f)throw new InvalidOperationException("Driver eye coordinate invalid");
        Debug.Log("CAB_INTERIOR_BLOCKOUT_OK: estimated 2.8m internal width; left desk only; two side doors; passenger passage; startup retained");
        // NF-5K is a combined driving/passenger head car, not two cabs on a short locomotive.
        Debug.Log("NF5K_HEADCAR_LAYOUT_OK: single driving compartment and integrated passenger bay; opposite end deferred to consist layout");

    }
}
