using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Static inspection track only; not a route or wheel/rail dynamics implementation.
public static class NF5KWheelTrackReview
{
    public static void Prepare()
    {
        foreach(string path in new[]{"Assets/Scenes/CabInteriorBlockout.unity","Assets/Scenes/CabDaylightReview.unity","Assets/Scenes/CabSimulationReview.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path);
            Install();
            NF5KBoardingAudit.Validate("tools/compile/wheel_continuous_boarding_"+scene.name+".json");
            EditorSceneManager.SaveScene(scene);
        }
    }
    public static void Install()
    {
        const float top=-1.3612222f;
        var ground=GameObject.Find("Review_Ground_Not_Vehicle");
        var p=ground.transform.position;p.y=-1.57f;ground.transform.position=p;
        var old=GameObject.Find("Inspection_Track_Not_Vehicle");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
        var root=new GameObject("Inspection_Track_Not_Vehicle").transform;
        var head=Material("Inspection_Rail_Head",new Color(.48f,.50f,.51f),.9f,.4f);
        var body=Material("Inspection_Rail_Web",new Color(.27f,.20f,.14f),.4f,.22f);
        var apron=Material("Inspection_Boarding_Apron",new Color(.48f,.49f,.48f),0,.12f);
        foreach(int side in new[]{-1,1})
        {
            // Maintain the boarding datum alongside the lowered rail bed.
            Box(side<0?"Inspection_Apron_Left":"Inspection_Apron_Right",new Vector3(side*1.9f,-1.46f,8),new Vector3(1.2f,.18f,26),apron,root);
            Box("Rail_Head",new Vector3(side*.75f,top-.019f,8),new Vector3(.065f,.038f,26),head,root);
            Box("Rail_Web",new Vector3(side*.75f,top-.092f,8),new Vector3(.018f,.108f,26),body,root);
            Box("Rail_Foot",new Vector3(side*.75f,top-.157f,8),new Vector3(.15f,.022f,26),body,root);
        }
        int count=0;
        var sharedWheel=NF5KWheelGeometry.Build();
        foreach(var wheel in GameObject.Find("Running_Gear").GetComponentsInChildren<MeshFilter>())
        {
            if(wheel.name!="Steel_Wheel")continue;count++;
            wheel.sharedMesh=sharedWheel;
            // Find tread crossing at rail centre without assuming a fixed profile segment index.
            var verts=wheel.sharedMesh.vertices;
            float railX=Mathf.Sign(wheel.transform.position.x)*.75f;
            float contactY=float.PositiveInfinity;
            int stride=2*(NF5KWheelGeometry.Segments+1);
            for(int band=0;band<verts.Length/stride;band++)
            {
                int start=band*stride+NF5KWheelGeometry.Segments;
                Vector3 a=wheel.transform.TransformPoint(verts[start]),b=wheel.transform.TransformPoint(verts[start+1]);
                if(Mathf.Abs(b.x-a.x)<1e-7f||railX<Mathf.Min(a.x,b.x)||railX>Mathf.Max(a.x,b.x))continue;
                float height=a.y+(b.y-a.y)*(railX-a.x)/(b.x-a.x);
                if(height<contactY)contactY=height;
            }
            if(Mathf.Abs(contactY-top)>.0005f)throw new InvalidOperationException("Wheel / rail gap: "+(contactY-top));
            if(wheel.GetComponent<Renderer>().bounds.min.y<=ground.GetComponent<Renderer>().bounds.max.y)
                throw new InvalidOperationException("Wheel intersects inspection ground");
        }
        if(count!=8)throw new InvalidOperationException("Expected 8 wheels");
        Debug.Log("NF5K_WHEEL_RAIL_STATIC_OK: 8 tread contacts within 0.5 mm; gauge 1435 mm; flange clear of ground. Static inspection only.");
        AssetDatabase.SaveAssets();
    }
    static Material Material(string name,Color color,float metal,float smooth)
    {
        string path="Assets/Resources/Cab/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
    }
    static void Box(string name,Vector3 pos,Vector3 size,Material mat,Transform parent)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.layer=30;g.transform.SetParent(parent,false);
        g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;
    }
}
