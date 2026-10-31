using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KCabReadableFinish
{
    public static void CaptureBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            var lighting=UnityEngine.Object.FindAnyObjectByType<CabInteriorLighting>();lighting.SetSupply(true,true);lighting.SetCabPreset(0);
            var record=lighting.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Crew_Record_Print");
            var cam=Camera.main;cam.fieldOfView=42;cam.transform.position=record.bounds.center+new Vector3(.07f,.46f,.26f);cam.transform.LookAt(record.bounds.center,Vector3.back);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/readability_record_final.png");
            Debug.Log("NF5K_READABLE_DETAIL_CAPTURE_OK");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void Apply(Transform body)
    {
        var lighting=body.GetComponent<CabInteriorLighting>();
        var fixtures=lighting.Fixtures;
        for(int f=0;f<fixtures.Length;f++)
        {
            var fixture=fixtures[f];
            // The broad diffuse fill supplies room brightness; close spot beams must not bleach print.
            if(fixture.circuit==InteriorLightCircuit.CabCeiling)fixture.intensity=1.0f;
            if(fixture.circuit==InteriorLightCircuit.Task)fixture.intensity=.8f;
            if(fixture.circuit==InteriorLightCircuit.AssistantTask)fixture.intensity=.15f;
            if(fixture.circuit==InteriorLightCircuit.Instrument)fixture.intensity=.06f;
            fixtures[f]=fixture;
        }
        EditorUtility.SetDirty(lighting);
        foreach(var renderer in body.GetComponentsInChildren<MeshRenderer>(true))
        {
            var materials=renderer.sharedMaterials;bool changed=false;
            for(int i=0;i<materials.Length;i++)
            {
                var source=materials[i];if(source==null)continue;
                bool key=source.name.Contains("Warm_Ivory_Phenolic");
                bool scale=source.name.Contains("Gauge_Scale")||source.name.Contains("Pressure_IvoryDial")||source.name.Contains("Dial_Ivory");
                bool paper=renderer.name=="Crew_Record_Print";
                if(!key&&!scale&&!paper)continue;
                // Keep imported source materials intact; each surface keeps its original artwork.
                string name=source.name.StartsWith("Cab_Readable_")?source.name:
                    "Cab_Readable_"+source.name+"_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
                string path="Assets/Resources/Cab/"+name+".mat";
                var finish=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(finish==null){finish=new Material(source){name=name};AssetDatabase.CreateAsset(finish,path);}
                finish.SetFloat("_Metallic",0);finish.SetFloat("_Smoothness",key?.23f:paper?.06f:.13f);
                finish.SetTexture("_MetallicGlossMap",null);finish.DisableKeyword("_METALLICSPECGLOSSMAP");
                finish.SetColor("_BaseColor",key?new Color(.63f,.61f,.53f):paper?new Color(.72f,.70f,.64f):new Color(.79f,.78f,.71f));
                finish.SetFloat("_SpecularHighlights",key?1:0);
                if(key)finish.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");else finish.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                finish.SetColor("_EmissionColor",Color.black);finish.DisableKeyword("_EMISSION");
                EditorUtility.SetDirty(finish);materials[i]=finish;changed=true;
                Debug.Log("CAB_READABLE_FINISH: "+renderer.name+" / "+source.name+" map="+source.GetTexture("_BaseMap")?.name);
            }
            if(changed){renderer.sharedMaterials=materials;EditorUtility.SetDirty(renderer);}
        }
        var record=body.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Crew_Record_Print");
        // Separate the printed face from the paper backing by a reproducible 0.8 mm.
        var backing=record.parent.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Timetable_Blank_Insert");
        var p=record.position;p.y=backing.bounds.max.y+.0008f;record.position=p;
        record.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        EditorUtility.SetDirty(record);
        var mesh=record.GetComponent<MeshFilter>().sharedMesh;
        Debug.Log("CAB_RECORD_GEOMETRY: position="+record.position+" normal="+record.TransformDirection(mesh.normals[0])+" uv="+string.Join(";",mesh.uv.Select(v=>v.ToString())));
    }
    public static void Batch()
    {
        try
        {
            foreach(string n in new[]{"CabInteriorBlockout","CabDaylightReview","CabSimulationReview","CabRailMotionReview"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+n+".unity");
                Apply(UnityEngine.Object.FindAnyObjectByType<CabInteriorLighting>().transform);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            var lighting=UnityEngine.Object.FindAnyObjectByType<CabInteriorLighting>();
            lighting.SetSupply(true,true);lighting.SetCabPreset(0);
            var record=lighting.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Crew_Record_Print");
            var cam=Camera.main;cam.fieldOfView=42;cam.transform.position=record.bounds.center+new Vector3(.07f,.46f,.26f);cam.transform.LookAt(record.bounds.center,Vector3.back);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/readability_record_lit.png");
            var original=record.sharedMaterial;
            var diagnostic=new Material(Shader.Find("Universal Render Pipeline/Unlit"));diagnostic.SetTexture("_BaseMap",original.GetTexture("_BaseMap"));diagnostic.SetColor("_BaseColor",Color.white);
            record.sharedMaterial=diagnostic;
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/readability_record_unlit_diagnostic.png");
            record.sharedMaterial=original;UnityEngine.Object.DestroyImmediate(diagnostic);
            NF5KCabWholeReview.Batch();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
