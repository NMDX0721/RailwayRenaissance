using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KBrakeRefinement
{
    public static void Apply(Transform body)
    {
        var root=body.GetComponentsInChildren<Transform>().Single(t=>t.name=="Brake_Valve_Photo_Study");int count=0;var prepared=new HashSet<string>();
        foreach(var renderer in root.GetComponentsInChildren<Renderer>())
        {
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {
                var source=materials[i];string key=source.name;
                string tag=key.Contains("CastSilver")||key=="NF5K_Brake_Coat"?"Coat":key.Contains("MachinedSilver")||key=="NF5K_Brake_Steel"?"Steel":key.Contains("OldSteel")||key=="NF5K_Brake_DarkSteel"?"DarkSteel":key.Contains("WornBlack")||key=="NF5K_Brake_Grip"?"Grip":key=="Joint_Clay"||key=="NF5K_Brake_Seal"?"Seal":null;
                if(key=="NF5K_Brake_CastMetal")tag="CastMetal";
                if(tag==null)throw new InvalidOperationException("Unmapped brake material "+key);
                if(tag=="Coat"&&(renderer.name.Contains("Casting")||renderer.name.Contains("Flange")||renderer.name.Contains("Cap_Ear")))tag="CastMetal";
                if(renderer.name.Contains("Flange")||renderer.name.Contains("Cap_Ear"))tag="Coat";
                string path="Assets/Resources/Cab/NF5K_Brake_"+tag+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(m==null){m=new Material(source);AssetDatabase.CreateAsset(m,path);}else if(m!=source)m.CopyPropertiesFromMaterial(source);
                if(m.HasProperty("_EmissionColor"))m.SetColor("_EmissionColor",Color.black);m.DisableKeyword("_EMISSION");
                if(tag=="Seal")
                {
                    m.shader=Shader.Find("Universal Render Pipeline/Lit");
                    m.SetTexture("_BaseMap",null);m.SetTexture("_MetallicGlossMap",null);m.DisableKeyword("_METALLICSPECGLOSSMAP");m.SetColor("_BaseColor",new Color(.016f,.017f,.018f));m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.12f);
                }
                else
                {
                    m.shader=Shader.Find("RailwayRenaissance/BrakeLayeredMetal");
                    string map=tag=="DarkSteel"?"Steel":tag;
                    foreach(string suffix in new[]{"BaseColor","MetallicSmoothness","Normal"})
                    {
                        string texture=$"Assets/Models/Cab/Textures/BrakeRefinement/Brake_{map}_{suffix}_V2.png";var importer=(TextureImporter)AssetImporter.GetAtPath(texture);
                        var type=suffix=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                        bool changed=importer.textureType!=type||importer.sRGBTexture!=(suffix=="BaseColor")||!importer.mipmapEnabled||importer.wrapMode!=TextureWrapMode.Repeat||importer.textureCompression!=TextureImporterCompression.Uncompressed;
                        importer.textureType=type;importer.sRGBTexture=suffix=="BaseColor";importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.textureCompression=TextureImporterCompression.Uncompressed;
                        if(prepared.Add(texture)&&changed)importer.SaveAndReimport();
                        m.SetTexture(suffix=="BaseColor"?"_BaseMap":suffix=="Normal"?"_BumpMap":"_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
                    }
                    m.SetColor("_BaseColor",tag=="DarkSteel"?new Color(.36f,.34f,.31f):Color.white);m.SetFloat("_BumpScale",tag=="Coat"||tag=="CastMetal"?.18f:.22f);m.SetFloat("_PaintLayer",tag=="Coat"||tag=="CastMetal"?1:0);m.SetFloat("_GripLayer",tag=="Grip"?1:0);m.EnableKeyword("_NORMALMAP");
                }
                EditorUtility.SetDirty(m);materials[i]=m;count++;
            }
            renderer.sharedMaterials=materials;
            BakeWearMask(renderer);
        }
        Debug.Log("NF5K_BRAKE_FINISH_OK: neutral coated casting, machined steel, dark fasteners/grips/seals; material slots "+count);
    }
    static void BakeWearMask(Renderer renderer)
    {
        var filter=renderer.GetComponent<MeshFilter>();if(filter==null)return;var source=filter.sharedMesh;var vertices=source.vertices;var normals=source.normals;var triangles=source.triangles;
        var bend=new float[vertices.Length];
        for(int i=0;i<triangles.Length;i+=3)for(int a=0;a<3;a++)for(int b=0;b<3;b++)
        {int x=triangles[i+a],y=triangles[i+b];bend[x]=Mathf.Max(bend[x],1-Vector3.Dot(normals[x],normals[y]));}
        var colors=new Color[vertices.Length];var uv=source.uv;var submeshes=Enumerable.Range(0,source.subMeshCount).Select(s=>source.GetTriangles(s)).ToArray();var grain=new List<Vector3>();var scale=renderer.transform.lossyScale;
        scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
        for(int i=0;i<vertices.Length;i++)
        {
            float height=Mathf.InverseLerp(renderer.bounds.min.y,renderer.bounds.max.y,renderer.transform.TransformPoint(vertices[i]).y);
            float edge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,.28f,bend[i]));
            colors[i]=new Color(edge*.24f,Mathf.Pow(1-height,9)*.20f,Mathf.Sin(height*Mathf.PI)*.65f,1);
            grain.Add(Vector3.Scale(vertices[i],scale));
        }
        string path="Assets/Models/Cab/NF5KBodyMeshes/Brake_Surface_"+renderer.name+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.name="Brake_Surface_"+renderer.name;mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.SetUVs(1,grain);mesh.colors=colors;mesh.subMeshCount=submeshes.Length;
        for(int i=0;i<submeshes.Length;i++)mesh.SetTriangles(submeshes[i],i);mesh.RecalculateBounds();mesh.RecalculateTangents();mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);filter.sharedMesh=mesh;
    }
    [Serializable] sealed class Row {public string name,materials;public Vector3 min,max;}
    [Serializable] sealed class Report {public Row[] parts;}
    public static void Validate(CabEngineState state)
    {
        var brake=state.GetComponentsInChildren<Transform>().Single(t=>t.name=="Brake_Valve_Photo_Study");
        var shader=Shader.Find("RailwayRenaissance/BrakeLayeredMetal");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Brake layered shader did not compile");
        if(brake.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Any(m=>!m.name.StartsWith("NF5K_Brake_")))throw new InvalidOperationException("Brake retains old material");
        Bounds InCab(Renderer r)
        {
            var frame=state.GetComponentInParent<CabInteriorLighting>().transform;var rotation=Quaternion.Inverse(frame.rotation);
            // The imported console has a scale of 100 and a converted axis basis; clearance is in vehicle metres.
            var f=r.GetComponent<MeshFilter>();var points=f.sharedMesh.vertices.Select(p=>rotation*(f.transform.TransformPoint(p)-frame.position)).ToArray();var result=new Bounds(points[0],Vector3.zero);foreach(var p in points)result.Encapsulate(p);return result;
        }
        var plate=state.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Service_Control_Seated_Plate");var b=InCab(plate);b.Expand(new Vector3(.030f,.006f,.030f));
        foreach(var r in brake.GetComponentsInChildren<Renderer>())if(b.Intersects(InCab(r)))throw new InvalidOperationException("Brake envelope too close to service controls: "+r.name+" plate="+b.ToString("F5")+" brake="+InCab(r).ToString("F5"));
        Debug.Log("NF5K_BRAKE_REFINEMENT_OK: both valves use coordinated finishes; service plate 15 mm expanded horizontal clearance survives actual driving pose");
    }
    public static void CaptureBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");UnityEngine.Object.FindAnyObjectByType<CabDaylightEnvironment>().Apply(CabDaylightEnvironment.DayPreset.Overcast);NF5KBogieDetailReview.ConfigureInspectionLighting();
            var state=UnityEngine.Object.FindAnyObjectByType<CabEngineState>();var brake=state.GetComponentsInChildren<Transform>().Single(t=>t.name=="Brake_Valve_Photo_Study");
            var rs=brake.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
            var cam=Camera.main;cam.fieldOfView=46;cam.transform.position=bounds.center+new Vector3(-.25f,.22f,.52f);cam.transform.LookAt(bounds.center+new Vector3(-.055f,-.035f,.025f));
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/brake_finish_pair_integrated_v2.png");
            cam.fieldOfView=36;cam.transform.position=bounds.center+new Vector3(-.15f,.10f,.43f);cam.transform.LookAt(bounds.center+Vector3.up*.035f);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/brake_metal_detail_integrated_v2.png");
            var plate=state.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Service_Control_Seated_Plate");
            cam.fieldOfView=43;cam.transform.position=plate.bounds.center+new Vector3(-.10f,.30f,.19f);cam.transform.LookAt(plate.bounds.center+new Vector3(.035f,.025f,0),Vector3.back);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/brake_service_gap_integrated_v2.png");
            cam.fieldOfView=64;cam.transform.position=new Vector3(.40f,1.38f,.94f);cam.transform.LookAt(plate.bounds.center+new Vector3(-.20f,0,-.08f));
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/brake_console_integrated_v2.png");
            Debug.Log("NF5K_BRAKE_REFINEMENT_CAPTURE_OK");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void PaletteBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");var state=UnityEngine.Object.FindAnyObjectByType<CabEngineState>();
            Apply(state.transform);Validate(state);AssetDatabase.SaveAssets();CaptureBatch();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void AuditBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");var state=UnityEngine.Object.FindAnyObjectByType<CabEngineState>();
            var rows=state.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.bounds.min.y<.90f&&r.bounds.max.y>.83f&&r.bounds.min.z<.40f&&r.bounds.max.z>.20f&&r.bounds.min.x<1.2f&&r.bounds.max.x>.25f)
                .Select(r=>new Row{name=r.name,materials=string.Join(",",r.sharedMaterials.Select(m=>m.name)),min=r.bounds.min,max=r.bounds.max}).ToArray();
            File.WriteAllText("tools/compile/brake_layout_audit.json",JsonUtility.ToJson(new Report{parts=rows},true));Debug.Log("NF5K_BRAKE_LAYOUT_AUDIT_OK");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
