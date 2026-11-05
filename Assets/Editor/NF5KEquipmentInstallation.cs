using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KEquipmentInstallation
{
    const string Folder="Assets/Resources/Cab/";
    const string Output="tools/cab_reference/equipment_installed/";
    static readonly string[] Scenes={"CabInteriorBlockout","CabDaylightReview","CabSimulationReview","CabRailMotionReview"};
    [Serializable] sealed class Report {public int scenes,poses;public string[] conflicts;public string scope="Installed visual assets, sampled external interference only; no new fuel or electrical simulation";}
    public static void Apply(Transform body)
    {
        var previous=body.Find("Underfloor_Detailed_Equipment");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var root=new GameObject("Underfloor_Detailed_Equipment").transform;root.SetParent(body,false);root.gameObject.layer=30;
        foreach(var name in new[]{"Battery_Box","Fuel_Tank"}){
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"NF5K_"+name+".prefab");
            if(prefab==null)throw new InvalidOperationException("Detailed equipment prefab missing: "+name);
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);obj.name=name;obj.transform.localPosition=name=="Battery_Box"?new Vector3(-1.17f,-.58f,6):new Vector3(0,-.70f,10.70f);obj.transform.localRotation=Quaternion.identity;obj.transform.localScale=Vector3.one;
        }
        // Reserve the fuel tank's established station without discarding the remaining generic cabinet.
        foreach(var t in body.GetComponentsInChildren<Transform>(true)){
            if(t.name!="Underfloor_Equipment_Cabinet"&&t.name!="Cabinet_Service_Cover")continue;
            if(Mathf.Abs(body.InverseTransformPoint(t.position).z-10.10f)>.05f)continue;
            var mesh=t.GetComponent<MeshFilter>();if(mesh==null||mesh.sharedMesh==null)continue;
            var scale=t.localScale;scale.z=(t.name=="Underfloor_Equipment_Cabinet"?.65f:.53f)/mesh.sharedMesh.bounds.size.z;t.localScale=scale;
        }
    }
    public static void Batch()
    {
        try {
            Directory.CreateDirectory(Output);EditorSceneManager.OpenScene("Assets/Scenes/CabFuelTankReview.unity");
            foreach(var name in new[]{"Battery_Box","Fuel_Tank"}){
                var source=GameObject.Find(name);if(source==null)throw new InvalidOperationException("Source model missing: "+name);
                PrefabUtility.SaveAsPrefabAsset(source,Folder+"NF5K_"+name+".prefab");
            }
            var conflicts=new HashSet<string>();int poses=0;
            foreach(var name in Scenes){
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");var body=GameObject.Find("CabInterior_Blockout").transform;Apply(body);
                var motion=UnityEngine.Object.FindAnyObjectByType<CabRailMotion>();
                var equipment=body.Find("Underfloor_Detailed_Equipment");
                var moving=equipment.GetComponentsInChildren<BoxCollider>();
                var obstacles=body.GetComponentsInChildren<Collider>().Where(c=>!c.isTrigger&&!c.transform.IsChildOf(equipment)).ToArray();
                void Check(){Physics.SyncTransforms();foreach(var c in moving)foreach(var e in obstacles)if(c.bounds.Intersects(e.bounds)&&Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,e,e.transform.position,e.transform.rotation,out _,out float depth)&&depth>.002f)conflicts.Add(name+": "+c.name+" / "+e.name+" "+depth.ToString("F4"));poses++;}
                if(motion!=null){foreach(double station in new[]{0d,35,60,100,145,170})foreach(double speed in new[]{-3d,0,3}){motion.Apply(station,speed,.3,.5,0,20261021,true);Check();}motion.Apply(0,0,0,0,0,20261021,true);}else Check();
                if(conflicts.Count>0)throw new InvalidOperationException(string.Join("\n",conflicts));
                EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
                EditorSceneManager.OpenScene(scene.path);if(GameObject.Find("Underfloor_Detailed_Equipment").GetComponentsInChildren<MeshFilter>().Length<200)throw new InvalidOperationException("Saved equipment meshes missing");
                Debug.Log("NF5K_EQUIPMENT_INSTALLED_OK: "+name+"; prefab instances persisted; external collision checks passed");
            }
            File.WriteAllText(Output+"acceptance.json",JsonUtility.ToJson(new Report{scenes=Scenes.Length,poses=poses,conflicts=conflicts.ToArray()},true));
            EditorSceneManager.OpenScene("Assets/Scenes/CabRailMotionReview.unity");var vehicle=GameObject.Find("CabInterior_Blockout").transform;
            var rail=UnityEngine.Object.FindAnyObjectByType<CabRailMotion>();rail.Apply(0,0,0,0,0,20261021,true);
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(!renderer.transform.IsChildOf(vehicle))renderer.enabled=false;
            var camera=Camera.main;camera.orthographic=true;camera.orthographicSize=6.8f;camera.farClipPlane=100;
            var light=new GameObject("Equipment_Review_Fill").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.transform.rotation=Quaternion.Euler(15,65,0);
            camera.transform.position=vehicle.TransformPoint(new Vector3(18,6,-8));camera.transform.LookAt(vehicle.TransformPoint(new Vector3(0,.3f,8)));Shot(camera,"train.png");
            camera.orthographicSize=2.2f;camera.transform.position=vehicle.TransformPoint(new Vector3(-8,-.3f,7.5f));camera.transform.LookAt(vehicle.TransformPoint(new Vector3(-.5f,-.55f,7.6f)));Shot(camera,"battery_on_train.png");
            camera.transform.position=vehicle.TransformPoint(new Vector3(8,-.6f,10.7f));camera.transform.LookAt(vehicle.TransformPoint(new Vector3(.3f,-.6f,10.7f)));Shot(camera,"fuel_on_train.png");
            EditorSceneManager.OpenScene("Assets/Scenes/CabRailMotionReview.unity");NF5KSimulationPlayReview.Begin();
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Shot(Camera camera,string name){bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;var rt=RenderTexture.GetTemporary(4200,3000,24);var resolved=RenderTexture.GetTemporary(1400,1000,0);var previous=RenderTexture.active;var old=camera.targetTexture;var pixels=new Texture2D(1400,1000,TextureFormat.RGB24,false);try{camera.targetTexture=rt;camera.Render();camera.Render();Graphics.Blit(rt,resolved);RenderTexture.active=resolved;pixels.ReadPixels(new Rect(0,0,1400,1000),0,0);pixels.Apply();File.WriteAllBytes(Output+name,pixels.EncodeToPNG());}finally{camera.targetTexture=old;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);RenderTexture.ReleaseTemporary(resolved);UnityEngine.Object.DestroyImmediate(pixels);ShaderUtil.allowAsyncCompilation=async;}}
}
