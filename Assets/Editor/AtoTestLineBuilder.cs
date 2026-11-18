using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

// ATO track kit: Blender-made modules (tools/ato_track) -> materials + prefabs -> a route assembled from a piece list.
// Grade pieces are already inclined in their own mesh, so assembling is just stacking start points: z -= run, y += rise.
public static class AtoTestLineBuilder
{
    const string ModelDir="Assets/Models/ATO/",TexDir="Assets/Models/ATO/Textures/",MatDir="Assets/Materials/ATO/",PrefabDir="Assets/Prefabs/ATO/";
    public const string ScenePath="Assets/Scenes/AtoTestLine.unity",KitScenePath="Assets/Scenes/AtoTrackKitDemo.unity";
    const double LineStart=-60;
    static readonly (string name,double run,double rise)[] Kit={
        ("Track_Module_25m",25,0),("Track_Module_5m",5,0),
        ("Track_Module_25m_Up10",25,.25),("Track_Module_25m_Down10",25,-.25),
        ("Track_Module_25m_Up20",25,.5),("Track_Module_25m_Down20",25,-.5)};
    // Test line s=-60..1200: flat, 50 x 25 m + 2 x 5 m.
    public static readonly (string,int)[] FlatLine={("Track_Module_25m",50),("Track_Module_5m",2)};
    // Same span as FlatLine (-60..1200) with grades: +-10/20 permil hills, net height change zero, level start station (first 65 m flat), stop target on a hill section.
    public static readonly (string,int)[] GradedLine={("Track_Module_25m",5),("Track_Module_25m_Up10",4),("Track_Module_25m",2),("Track_Module_25m_Up20",4),("Track_Module_25m",2),("Track_Module_25m_Down20",4),("Track_Module_25m",2),("Track_Module_25m_Down10",4),("Track_Module_25m",2),("Track_Module_25m_Up10",4),("Track_Module_25m_Down10",4),("Track_Module_25m_Down20",3),("Track_Module_25m_Up20",3),("Track_Module_25m",7),("Track_Module_5m",2)};
    // Kit demo: flat, +20, flat, -20, +10, -10 permil, with short pieces mixed in.
    public static readonly (string,int)[] DemoLine={("Track_Module_25m",3),("Track_Module_25m_Up20",4),("Track_Module_25m",2),("Track_Module_5m",2),("Track_Module_25m_Down20",4),("Track_Module_25m_Up10",3),("Track_Module_25m_Down10",3),("Track_Module_25m",2)};
    static readonly (string,double)[] Markers={("RouteStart",0),("Marker_250",250),("Marker_650",650),("StopTarget",900),("Marker_900",900),("RouteEnd",1200)};

    // ---------------------------------------------------------------- kit: textures, materials, prefabs
    [MenuItem("Railway/ATO/Build Track Kit")]
    public static void BuildKit()
    {
        System.IO.Directory.CreateDirectory(MatDir);System.IO.Directory.CreateDirectory(PrefabDir);
        foreach(var part in new[]{"Rail","Sleeper","Ballast","Stone"})
        {
            Texture(TexDir+"Track_"+part+"_BaseColor.png",true,false);
            Texture(TexDir+"Track_"+part+"_Normal.png",false,true);
            Texture(TexDir+"Track_"+part+"_MetallicSmoothness.png",false,false);
        }
        var steel=Material("Track_Rail_Steel","Rail",1f);var concrete=Material("Track_Sleeper_Concrete","Sleeper",1f);var ballast=Material("Track_Ballast","Ballast",1f);var stone=Material("Track_Ballast_Stone","Stone",1f);
        ballast.SetColor("_BaseColor",new Color(.6f,.6f,.6f)); // darker bed: it reads as shadowed gaps between the modelled stones
        var fastener=PlainMaterial("Track_Fastener_Steel",new Color(.17f,.15f,.13f),.7f,.38f);
        var rubber=PlainMaterial("Track_Rubber",new Color(.055f,.055f,.06f),0f,.18f);
        foreach(var (name,_,_) in Kit)
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir+name+".fbx");
            if(model==null){Debug.LogError("ATO_KIT_MISSING "+name);continue;}
            var go=Object.Instantiate(model);go.name=name;
            var near=new List<Renderer>();var far=new List<Renderer>();
            foreach(var r in go.GetComponentsInChildren<Renderer>())
            {
                bool stones=r.name.StartsWith("Ballast_Stones");bool small=r.name=="Pads"||r.name=="Fasteners";
                r.sharedMaterial=r.name.StartsWith("Rail")?steel:r.name=="Sleepers"?concrete:stones?stone:r.name=="Pads"?rubber:r.name=="Fasteners"?fastener:ballast;
                if(stones||small)r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; // hundreds of thousands of tris: not worth a shadow-map pass
                near.Add(r);if(!stones&&!small)far.Add(r);
            }
            // LOD0: modelled surface stones; LOD1 (beyond ~70 m): the textured bed only.
            var lod=go.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.5f,near.ToArray()),new LOD(.005f,far.ToArray())});lod.RecalculateBounds();
            PrefabUtility.SaveAsPrefabAsset(go,PrefabDir+name+".prefab");Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();Debug.Log("ATO_KIT_OK prefabs="+Kit.Length);
    }
    static void Texture(string path,bool srgb,bool normal)
    {
        var imp=AssetImporter.GetAtPath(path) as TextureImporter;if(imp==null){AssetDatabase.ImportAsset(path);imp=AssetImporter.GetAtPath(path) as TextureImporter;}
        if(imp==null)return;
        imp.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;imp.sRGBTexture=srgb;imp.alphaSource=TextureImporterAlphaSource.FromInput;
        imp.alphaIsTransparency=false;imp.mipmapEnabled=true;imp.anisoLevel=16;imp.maxTextureSize=1024;imp.SaveAndReimport();
    }
    static Material PlainMaterial(string name,Color color,float metallic,float smoothness)
    {
        string path=MatDir+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smoothness);EditorUtility.SetDirty(m);return m;
    }
    static Material Material(string name,string part,float smoothScale)
    {
        string path=MatDir+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir+"Track_"+part+"_BaseColor.png"));m.SetColor("_BaseColor",Color.white);
        m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir+"Track_"+part+"_Normal.png"));m.SetFloat("_BumpScale",1f);
        m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir+"Track_"+part+"_MetallicSmoothness.png"));
        m.SetFloat("_Metallic",1f);m.SetFloat("_Smoothness",smoothScale);m.SetFloat("_SmoothnessTextureChannel",0);
        m.EnableKeyword("_NORMALMAP");m.EnableKeyword("_METALLICSPECGLOSSMAP");EditorUtility.SetDirty(m);return m;
    }

    // ---------------------------------------------------------------- route assembly
    // Returns height of the rail top above the datum at each piece start, so markers can sit on the rails.
    public static void Assemble(string rootName,(string,int)[] plan)
    {
        var old=GameObject.Find(rootName);if(old!=null)Object.DestroyImmediate(old);
        // Same datum as CabReviewTrack: rail top at RailDatum, reference point offset 8.6 m along Z.
        var root=new GameObject(rootName).transform;root.position=new Vector3(0,CabReviewTrack.RailDatum,8.6f);
        var starts=new List<(double s,double y,double run,double rise)>();double s=LineStart,y=0;int index=0;
        foreach(var (name,count) in plan)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir+name+".prefab");
            if(prefab==null){Debug.LogError("ATO_LINE_MISSING_PREFAB "+name);return;}
            var spec=System.Array.Find(Kit,k=>k.name==name);
            for(int i=0;i<count;i++)
            {
                var m=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);
                m.name="Track_"+(index++).ToString("D3")+"_"+name.Replace("Track_Module_","");
                m.transform.localPosition=new Vector3(0,(float)y,-(float)s);
                starts.Add((s,y,spec.run,spec.rise));s+=spec.run;y+=spec.rise;
            }
        }
        var marks=new GameObject("Route_Markers").transform;marks.SetParent(root,false);
        foreach(var (name,at) in Markers)
        {
            if(at>s)continue;double h=0;foreach(var p in starts)if(at>=p.s&&at<=p.s+p.run){h=p.y+p.rise*(at-p.s)/p.run;break;}
            var g=new GameObject(name).transform;g.SetParent(marks,false);g.localPosition=new Vector3(0,(float)h,-(float)at);
        }
        Debug.Log("ATO_LINE_OK "+rootName+" pieces="+index+" span="+LineStart+".."+s+" endHeight="+y);
    }

    // The same piece list as a route for the vehicle: straight line, rail height follows the pieces' rises exactly.
    public static CabReviewTrack TrackFor((string,int)[] plan)
    {
        var runs=new List<double>();var rises=new List<double>();double end=LineStart;
        foreach(var (name,count) in plan){var spec=System.Array.Find(Kit,k=>k.name==name);for(int i=0;i<count;i++){runs.Add(spec.run);rises.Add(spec.rise);end+=spec.run;}}
        return CabReviewTrack.Straight(LineStart,end,runs.ToArray(),rises.ToArray());
    }

    [MenuItem("Railway/ATO/Build Test Line")]
    public static void Build()=>Assemble("ATO_Test_Line",FlatLine);

    // Batch entry: kit, then both scenes, with bounds reported.
    public static void Batch()
    {
        BuildKit();
        Scene("ATO_Test_Line",FlatLine,ScenePath);
        Scene("ATO_Kit_Demo",DemoLine,KitScenePath);
        EditorApplication.Exit(0);
    }
    static void Scene(string rootName,(string,int)[] plan,string path)
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,UnityEditor.SceneManagement.NewSceneMode.Single);
        Assemble(rootName,plan);
        var all=GameObject.Find(rootName).GetComponentsInChildren<Renderer>();var b=all[0].bounds;foreach(var r in all)b.Encapsulate(r.bounds);
        Debug.Log("ATO_BOUNDS "+rootName+" min="+b.min+" max="+b.max);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,path);Debug.Log("ATO_SAVED "+path);
    }

    // Batch entry: screenshots (world z = 8.6 - s, rail top y = RailDatum at s where the demo is flat).
    public static void Shots()
    {
        System.IO.Directory.CreateDirectory("tools/ato_track/review");
        bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
        float y=CabReviewTrack.RailDatum;float Z(double s)=>8.6f-(float)s;
        Shoot(ScenePath,new (string,Vector3,Vector3)[]{
            ("line_start",new Vector3(2.2f,y+1.4f,Z(-45)),new Vector3(0,y,Z(-15))),
            ("line_sleeper",new Vector3(1.9f,y+.7f,Z(5)),new Vector3(0,y-.15f,Z(9))),
            ("line_ballast",new Vector3(3.2f,y+.45f,Z(15)),new Vector3(1.6f,y-.35f,Z(20))),
            ("line_stones",new Vector3(.3f,y+.9f,Z(11)),new Vector3(.9f,y-.2f,Z(14))),
            ("line_shoulder",new Vector3(2.9f,y+.5f,Z(4)),new Vector3(1.9f,y-.2f,Z(8))),
            ("line_head",new Vector3(.9f,y+.18f,Z(8)),new Vector3(.75f,y,Z(11))),
            ("line_900",new Vector3(3.5f,y+2.2f,Z(870)),new Vector3(0,y,Z(900))),
            ("line_end",new Vector3(4,y+3,Z(1160)),new Vector3(0,y,Z(1200)))});
        Shoot(KitScenePath,new (string,Vector3,Vector3)[]{
            ("kit_overview",new Vector3(60,y+90,Z(-120)),new Vector3(0,y+10,Z(500))),
            ("kit_side",new Vector3(120,y+12,Z(300)),new Vector3(0,y+5,Z(300))),
            ("kit_joint",new Vector3(2.5f,y+1.4f,Z(75)),new Vector3(0,y+.2f,Z(125)))});
        ShaderUtil.allowAsyncCompilation=async;Debug.Log("ATO_SHOTS_OK");EditorApplication.Exit(0);
    }
    static void Shoot(string scenePath,(string,Vector3,Vector3)[] views)
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
        var cam=Camera.main;if(cam==null)cam=new GameObject("Cam").AddComponent<Camera>();
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.72f,.75f,.77f);cam.fieldOfView=45;cam.nearClipPlane=.05f;cam.farClipPlane=3000;
        foreach(var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude)){l.intensity=Mathf.Max(l.intensity,1.2f);}
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.57f,.6f);
        foreach(var (name,pos,look) in views)
        {
            cam.transform.position=pos;cam.transform.LookAt(look);
            var rt=RenderTexture.GetTemporary(1400,800,24);var prev=RenderTexture.active;var old=cam.targetTexture;
            var img=new Texture2D(1400,800,TextureFormat.RGB24,false);
            try{cam.targetTexture=rt;cam.Render();cam.Render();RenderTexture.active=rt;img.ReadPixels(new Rect(0,0,1400,800),0,0);img.Apply();
                System.IO.File.WriteAllBytes("tools/ato_track/review/"+name+".png",img.EncodeToPNG());}
            finally{cam.targetTexture=old;RenderTexture.active=prev;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(img);}
        }
    }
}
