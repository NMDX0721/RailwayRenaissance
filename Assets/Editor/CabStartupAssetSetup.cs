using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class CabStartupAssetSetup
{
    const string Source = "Assets/Models/Cab/UnityStartupControls/";
    const string Output = "Assets/Resources/Cab/";
    [Serializable] class Catalog { public Entry[] materials; }
    [Serializable] class Entry { public string name; public float[] color; public float metallic, roughness; }
    public static void BatchBuild()
    {
        try { Build(); EditorApplication.Exit(0); }
        catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
    [MenuItem("RailRevival/Cab/Build Startup Control Variants")]
    public static void Build()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Directory.CreateDirectory(Source+"Materials");
        var catalog=JsonUtility.FromJson<Catalog>("{\"materials\":"+File.ReadAllText(Source+"materials.json")+"}");
        var materials=new Dictionary<string,Material>();
        foreach(var info in catalog.materials)
        {
            string path=Source+"Materials/"+info.name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path); }
            material.SetColor("_BaseColor",new Color(info.color[0],info.color[1],info.color[2],1));
            material.SetFloat("_Metallic",info.metallic);material.SetFloat("_Smoothness",1-info.roughness);
            if(info.name=="Startup_Unlit_Green_Lens")material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);materials.Add(info.name,material);
        }
        GameObject Make(string variant)
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(Source+variant+".fbx");
            if(model==null)throw new InvalidOperationException("Startup FBX missing: "+variant);
            var instance=UnityEngine.Object.Instantiate(model);
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var assigned=renderer.sharedMaterials;
                for(int i=0;i<assigned.Length;i++)assigned[i]=materials[assigned[i].name];
                renderer.sharedMaterials=assigned;
            }
            return instance;
        }
        var key=Make("key_only");key.name="MasterKey";
        var keyPrefab=PrefabUtility.SaveAsPrefabAsset(key,Output+"MasterKey.prefab");
        UnityEngine.Object.DestroyImmediate(key);
        foreach(string variant in new[]{"with_key","without_key"})
        {
            var instance=Make(variant);instance.name=variant=="with_key"?"StartupControlsWithKey":"StartupControlsWithoutKey";
            Transform Find(string name)
            {
                foreach(var t in instance.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;
                throw new InvalidOperationException("Missing startup node: "+name);
            }
            var pivot=Find("Key_Turn_Pivot");
            var control=instance.AddComponent<CabStartupControls>();
            control.Configure(variant=="with_key"?Find("Removable_Master_Key").gameObject:null,
                keyPrefab,pivot,pivot.parent.InverseTransformVector(Vector3.up),variant=="with_key",
                Find("Pump_Selector"),Find("Master_Power_Lens").GetComponent<Renderer>(),Find("Engine_Running_Lens").GetComponent<Renderer>());
            // Validate on a clone so inserting the separate key cannot alter the saved variant.
            var test=UnityEngine.Object.Instantiate(instance);
            var c=test.GetComponent<CabStartupControls>();var state=test.AddComponent<CabEngineState>();
            c.BindEngineState(state);
            if(c.TryStartEngine())throw new InvalidOperationException("Started with master disabled");
            if(c.KeyInserted)c.RemoveKey();
            if(c.SetMaster(true))throw new InvalidOperationException("Enabled without a key");
            if(!c.InsertKey() || !c.SetMaster(true))throw new InvalidOperationException("Insert/turn failed");
            if(c.RemoveKey())throw new InvalidOperationException("Removed key in ON position");
            c.SetFuelPump(false);
            if(c.TryStartEngine())throw new InvalidOperationException("Started with pump off");
            if(!c.SetFuelPump(true)||!c.TryStartEngine()||!state.IsEngineStarted)throw new InvalidOperationException("Valid start failed");
            if(c.SetMaster(false)||c.RemoveKey())throw new InvalidOperationException("Running key interlock failed");
            c.StopEngine();
            if(!c.SetMaster(false)||!c.RemoveKey())throw new InvalidOperationException("Stopped key removal failed");
            UnityEngine.Object.DestroyImmediate(test);
            PrefabUtility.SaveAsPrefabAsset(instance,Output+instance.name+".prefab");
            UnityEngine.Object.DestroyImmediate(instance);
            Debug.Log("CAB_STARTUP_VARIANT_VALIDATED: "+variant);
        }
        AssetDatabase.SaveAssets();Debug.Log("CAB_STARTUP_IMPORT_OK");
    }
}
