using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class CabBrakeAssetSetup
{
    const string Source = "Assets/Models/Cab/UnityBrake";
    const string Output = "Assets/Resources/Cab";
    const string Request = "tools/compile/cab_brake_import.request";
    const string Result = "tools/compile/cab_brake_import_result.json";
    static CabBrakeAssetSetup() { EditorApplication.update += CheckRequest; }
    static void CheckRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        try { Build(); File.WriteAllText(Result, "{\"success\":true}"); }
        catch (Exception error)
        {
            File.WriteAllText(Result, JsonUtility.ToJson(new ImportFailure { error = error.ToString() }));
            Debug.LogException(error);
        }
    }
    [Serializable] class ImportFailure { public string error; }
    [Serializable] class MaterialList { public MaterialInfo[] materials; }
    [Serializable] class MaterialInfo
    {
        public string name, BaseColor, Normal, MetallicSmoothness;
        public float metallic, smoothness;
        public float[] color;
    }

    [MenuItem("RailRevival/驾驶台/导入制动阀定稿")]
    public static void Build()
    {
        Directory.CreateDirectory(Output);
        Directory.CreateDirectory(Source + "/Materials");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var catalog = JsonUtility.FromJson<MaterialList>(File.ReadAllText(Source + "/materials.json"));
        var mats = new Dictionary<string, Material>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("URP Lit shader unavailable");
        foreach (var info in catalog.materials)
        {
            string path = Source + "/Materials/" + info.name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = shader;
            mat.SetColor("_BaseColor", string.IsNullOrEmpty(info.BaseColor)
                ? new Color(info.color[0], info.color[1], info.color[2], info.color[3]) : Color.white);
            SetTexture(mat, "_BaseMap", info.BaseColor, true, false);
            SetTexture(mat, "_BumpMap", info.Normal, false, true);
            SetTexture(mat, "_MetallicGlossMap", info.MetallicSmoothness, false, false);
            mat.SetFloat("_Metallic", info.metallic);
            mat.SetFloat("_Smoothness", string.IsNullOrEmpty(info.MetallicSmoothness) ? info.smoothness : .65f);
            if (!string.IsNullOrEmpty(info.Normal)) mat.EnableKeyword("_NORMALMAP");
            if (!string.IsNullOrEmpty(info.MetallicSmoothness)) mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(mat);
            mats.Add(info.name, mat);
        }
        foreach (string variant in new[] { "without_handles", "with_handles" })
        {
            string modelPath = Source + "/BrakeValves_" + variant + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) throw new InvalidOperationException("FBX import failed: " + modelPath);
            var instance = UnityEngine.Object.Instantiate(model);
            instance.name = variant == "with_handles" ? "BrakeValvesWithHandles" : "BrakeValvesWithoutHandles";
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var assigned = renderer.sharedMaterials;
                for (int i = 0; i < assigned.Length; i++)
                {
                    string key = assigned[i] == null ? "" : assigned[i].name;
                    if (!mats.TryGetValue(key, out var material))
                        throw new InvalidOperationException("Missing explicit URP material mapping: " + key);
                    assigned[i] = material;
                }
                renderer.sharedMaterials = assigned;
            }
            PrefabUtility.SaveAsPrefabAsset(instance, Output + "/" + instance.name + ".prefab");
            UnityEngine.Object.DestroyImmediate(instance);
        }
        AssetDatabase.SaveAssets();
        CreateReviewScene();
        Debug.Log("CAB_BRAKE_IMPORT_OK: both URP prefabs and review scene created");
    }

    static void SetTexture(Material mat, string slot, string path, bool srgb, bool normal)
    {
        if (string.IsNullOrEmpty(path)) return;
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.sRGBTexture = srgb;
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.SaveAndReimport();
        mat.SetTexture(slot, AssetDatabase.LoadAssetAtPath<Texture2D>(path));
    }

    static void CreateReviewScene()
    {
        var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Output + "/BrakeValvesWithHandles.prefab");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        foreach (var child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
        var renderers = model.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        if (bounds.size.y < .1f || bounds.size.y > .5f)
            throw new InvalidOperationException("Unexpected imported metre scale: " + bounds.size);
        var camera = new GameObject("ReviewCamera").AddComponent<Camera>();
        camera.transform.position = bounds.center + new Vector3(-.32f,.20f,.48f);
        camera.transform.LookAt(bounds.center);
        camera.nearClipPlane = .01f;
        camera.farClipPlane = 20;
        camera.orthographic = true;
        camera.orthographicSize = .20f;
        camera.backgroundColor = new Color(.10f,.085f,.07f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.tag = "MainCamera";
        camera.cullingMask = 1 << 31;
        var light = new GameObject("ReviewLight").AddComponent<Light>();
        light.type = LightType.Directional;
        light.cullingMask = 1 << 31;
        light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(45,-35,0);
        var fill = new GameObject("ReviewFill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = .7f;
        fill.cullingMask = 1 << 31;
        fill.transform.rotation = Quaternion.Euler(20,145,0);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.35f,.35f,.35f);
        RenderSettings.reflectionIntensity = .4f;
        const string reflectionPath = "Assets/Models/Cab/UnityBrake/ReviewReflection.cubemap";
        var reflection = AssetDatabase.LoadAssetAtPath<Cubemap>(reflectionPath);
        if (reflection == null)
        {
            reflection = new Cubemap(16, TextureFormat.RGBAHalf, true);
            var pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(.3f,.3f,.3f,1);
            foreach (CubemapFace face in new[] { CubemapFace.PositiveX, CubemapFace.NegativeX,
                CubemapFace.PositiveY, CubemapFace.NegativeY, CubemapFace.PositiveZ, CubemapFace.NegativeZ })
                reflection.SetPixels(pixels, face);
            reflection.Apply(true);
            AssetDatabase.CreateAsset(reflection, reflectionPath);
        }
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflection = reflection;
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/CabComponentReview.unity");
        var target = new RenderTexture(1000, 900, 24);
        camera.targetTexture = target;
        var otherLights = new Dictionary<Light, int>();
        foreach (var other in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (other.gameObject.scene == scene) continue;
            otherLights.Add(other, other.cullingMask);
            other.cullingMask &= ~(1 << 31);
        }
        try { camera.Render(); }
        finally { foreach (var entry in otherLights) if (entry.Key != null) entry.Key.cullingMask = entry.Value; }
        var previousTarget = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(1000, 900, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1000, 900), 0, 0);
        image.Apply();
        File.WriteAllBytes("tools/cab_reference/brake_review/unity_brake_review.png", image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = previousTarget;
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(image);
        Debug.Log("CAB_BRAKE_BOUNDS: " + bounds.size + "; renderers=" + renderers.Length);
        if (previous.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
        EditorSceneManager.CloseScene(scene, true);
    }
}
