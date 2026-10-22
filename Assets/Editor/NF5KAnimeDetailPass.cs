using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KAnimeDetailPass
{
    static Transform body;
    static Material enamel,metal,rubber;
    public static void Apply(Transform vehicle)
    {
        body=vehicle;
        enamel=Mat("Detail_Rain_Gutter_Enamel",new Color(.75f,.73f,.63f),0,.27f);
        metal=Mat("Detail_Satin_Handle_Steel",new Color(.63f,.66f,.67f),.78f,.36f);
        rubber=Mat("Detail_Dark_Gasket",new Color(.09f,.105f,.11f),0,.16f);
        var previous=body.Find("Exterior_Service_Details");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var fixedParts=new GameObject("Exterior_Service_Details").transform;fixedParts.SetParent(body,false);
        foreach(int side in new[]{-1,1})
        {
            // Folded rain rail with outward drip edge, above the moving leaves and window frames.
            Box("Continuous_Rain_Rail",new Vector3(side*1.482f,2.018f,8.61f),new Vector3(.035f,.018f,17.12f),enamel,fixedParts);
            Box("Gutter_Upstand",new Vector3(side*1.468f,2.036f,8.61f),new Vector3(.008f,.034f,17.12f),enamel,fixedParts);
            foreach(float z in new[]{.08f,17.14f})Rod("Gutter_Downturned_End",new Vector3(side*1.485f,2.022f,z),new Vector3(side*1.50f,1.976f,z),.009f,enamel,fixedParts);
            foreach(float z in new[]{1.49f,16.50f})
            {
                Box("Door_Header_Drip_Lip",new Vector3(side*1.51f,1.997f,z),new Vector3(.075f,.014f,.91f),enamel,fixedParts);
                foreach(float offset in new[]{-.405f,.405f})Rod("Header_End_Fastener",new Vector3(side*1.52f,1.998f,z+offset),new Vector3(side*1.52f,2.011f,z+offset),.004f,metal,fixedParts);
            }
        }
        int handles=0;
        foreach(var door in body.GetComponentsInChildren<CabSideDoor>(true))
        {
            var old=door.transform.Find("Exterior_Handle_Details");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var fittings=new GameObject("Exterior_Handle_Details").transform;fittings.SetParent(door.transform,false);
            bool rear=door.name.Contains("Rear_Entry");int side=body.InverseTransformPoint(door.transform.position).x<0?-1:1;
            float z=rear?16.18f:1.22f,x=rear?1.51f:1.489f;
            foreach(var renderer in door.GetComponentsInChildren<MeshRenderer>())
                if(renderer.name.EndsWith("_Door_Latch")||renderer.name.EndsWith("_Rear_Entry_Handle"))renderer.enabled=false;
            Box("Handle_Escutcheon",new Vector3(side*x,1,z),new Vector3(.008f,.16f,.056f),metal,fittings);
            Box("Escutcheon_Gasket",new Vector3(side*(x-.005f),1,z),new Vector3(.004f,.168f,.064f),rubber,fittings);
            foreach(float dz in new[]{-.045f,.045f})Rod("Handle_Standoff",new Vector3(side*x,1,z+dz),new Vector3(side*(x+.041f),1,z+dz),.007f,metal,fittings);
            Rod("Rounded_Grip",new Vector3(side*(x+.041f),1,z-.052f),new Vector3(side*(x+.041f),1,z+.052f),.008f,metal,fittings);
            Rod("Lock_Barrel",new Vector3(side*x,.951f,z),new Vector3(side*(x+.012f),.951f,z),.011f,metal,fittings);
            Box("Keyway",new Vector3(side*(x+.013f),.951f,z),new Vector3(.0015f,.012f,.0025f),rubber,fittings);
            foreach(float y in new[]{.934f,1.066f})Rod("Handle_Base_Screw",new Vector3(side*x,y,z),new Vector3(side*(x+.007f),y,z),.0035f,metal,fittings);
            var grip=fittings.Find("Rounded_Grip");var closedPosition=grip.position;
            door.SetOpen(.5f);
            if(Vector3.Distance(closedPosition,grip.position)<.1f)throw new InvalidOperationException("Handle failed to follow moving leaf");
            door.Close();
            handles++;
        }
        if(handles!=4)throw new InvalidOperationException("Expected four detailed exterior door handles");
        foreach(string name in new[]{"Body_Painted_Steel","Roof_Aged_Enamel"})
        {
            var paint=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/"+name+".mat");if(paint==null)continue;
            paint.SetTexture("_BumpMap",null);paint.DisableKeyword("_NORMALMAP");paint.SetTexture("_BaseMap",null);
            paint.SetFloat("_Smoothness",.30f);EditorUtility.SetDirty(paint);
        }
        NF5KRunningGear.FinishWheelMaterials();
        foreach(var renderer in fixedParts.GetComponentsInChildren<Renderer>())renderer.gameObject.layer=body.gameObject.layer;
        foreach(var door in body.GetComponentsInChildren<CabSideDoor>())foreach(var renderer in door.transform.Find("Exterior_Handle_Details").GetComponentsInChildren<Renderer>())renderer.gameObject.layer=body.gameObject.layer;
        Debug.Log("NF5K_ANIME_DETAIL_OK: four moving handle assemblies; paired continuous rain rails and four header drip lips; simplified paint microdetail");
    }
    static Material Mat(string name,Color color,float metallic,float smooth)
    {
        string path="Assets/Resources/Cab/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_BaseColor",color);material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(material);return material;
    }
    static GameObject Box(string name,Vector3 position,Vector3 size,Material material,Transform parent)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);
        g.transform.SetPositionAndRotation(body.TransformPoint(position),body.rotation);g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static void Rod(string name,Vector3 a,Vector3 b,float radius,Material material,Transform parent)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(parent,false);
        g.transform.SetPositionAndRotation(body.TransformPoint((a+b)*.5f),body.rotation*Quaternion.FromToRotation(Vector3.up,(b-a).normalized));
        g.transform.localScale=new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2);g.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
    }
    public static void BatchBuild()
    {
        try
        {
            foreach(string path in new[]{"CabInteriorBlockout","CabDaylightReview","CabSimulationReview","CabRailMotionReview"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+path+".unity");Apply(GameObject.Find("CabInterior_Blockout").transform);
                NF5KDoorReview.Validate();EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            UnityEngine.Object.FindFirstObjectByType<CabDaylightEnvironment>().Apply(CabDaylightEnvironment.DayPreset.Overcast);
            NF5KBogieDetailReview.ConfigureInspectionLighting();var camera=Camera.main;camera.orthographic=true;
            foreach(var angle in new[]{new Vector3(35,-50,0),new Vector3(20,125,0)})
            {
                var fill=new GameObject("Temporary_Anime_Exterior_Review_Light").AddComponent<Light>();fill.type=LightType.Directional;
                fill.intensity=angle.x==35?.8f:.35f;fill.shadows=LightShadows.None;fill.transform.rotation=Quaternion.Euler(angle);
            }
            camera.transform.position=new Vector3(14,6,-11);camera.transform.LookAt(new Vector3(0,.8f,8.6f));camera.orthographicSize=9;
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/anime_train_detail_bright.png");
            camera.transform.position=new Vector3(4,1.6f,-.1f);camera.transform.LookAt(new Vector3(1.46f,1.2f,1.5f));camera.orthographicSize=1.12f;
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/anime_train_door_bright.png");
            EditorSceneManager.OpenScene("Assets/Scenes/CabRailMotionReview.unity");NF5KSimulationPlayReview.Begin();
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
