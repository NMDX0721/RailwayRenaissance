using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using RailwayRenaissance.Cab;

[InitializeOnLoad]
public static class CabMainConsoleAssetSetup
{
    static int reviewFrames;
    public static void BatchBuild()
    {
        Build();
        EditorSceneManager.OpenScene("Assets/Scenes/CabMainConsoleReview.unity", OpenSceneMode.Single);
        reviewFrames = 0;
        EditorApplication.update += RenderAfterImport;
    }

    static void RenderAfterImport()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try
        {
            reviewFrames++;
            if (reviewFrames == 30)
            {
                var gauge = UnityEngine.Object.FindFirstObjectByType<CabTemperatureGauge>();
                foreach (var channel in UnityEngine.Object.FindObjectsByType<CabTemperatureGauge>(FindObjectsSortMode.None))
                    channel.SetTemperature(channel.Calibration.previewC);
                var camera = Camera.main;
                var target = new RenderTexture(1400, 1050, 24);
                var previous = RenderTexture.active;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(1400, 1050, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1400, 1050), 0, 0);
                image.Apply();
                File.WriteAllBytes("tools/cab_reference/console_review/unity_main_console_review.png", image.EncodeToPNG());
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
                Debug.Log("CAB_MAIN_CONSOLE_RELOAD_RENDER_OK");
            }
            if (reviewFrames >= 40)
            {
                EditorApplication.update -= RenderAfterImport;
                EditorApplication.Exit(0);
            }
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.update -= RenderAfterImport;
            EditorApplication.Exit(1);
        }
    }

    const string Source = "Assets/Models/Cab/UnityMainConsole";
    const string Output = "Assets/Resources/Cab";
    const string Request = "tools/compile/cab_main_console_import.request";
    const string Result = "tools/compile/cab_main_console_import_result.json";
    static CabMainConsoleAssetSetup() { EditorApplication.update += CheckRequest; }
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
        public float metallic, smoothness, transmission;
        public float[] color;
    }

    [MenuItem("RailRevival/Cab/Build Main Console Study")]
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
            mat.SetFloat("_BumpScale", info.name == "Gauge_Scale" ? .08f : .22f);
            SetTexture(mat, "_MetallicGlossMap", info.MetallicSmoothness, false, false);
            mat.SetFloat("_Metallic", info.metallic);
            mat.SetFloat("_Smoothness", string.IsNullOrEmpty(info.MetallicSmoothness) ? info.smoothness : .65f);
            if (!string.IsNullOrEmpty(info.Normal)) mat.EnableKeyword("_NORMALMAP");
            if (!string.IsNullOrEmpty(info.MetallicSmoothness)) mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            if (info.transmission > .5f)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0);
                mat.SetColor("_BaseColor", new Color(.86f,.94f,.91f,.12f));
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.SetShaderPassEnabled("ShadowCaster", false);
            }
            EditorUtility.SetDirty(mat);
            mats.Add(info.name, mat);
        }
        foreach (string variant in new[] { "final" })
        {
            string modelPath = Source + "/MainConsole_study.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) throw new InvalidOperationException("FBX import failed: " + modelPath);
            var instance = UnityEngine.Object.Instantiate(model);
            instance.name = "MainConsoleStudy";
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
            Transform Find(string name)
            {
                foreach (var t in instance.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
                throw new InvalidOperationException("Missing gauge node: " + name);
            }
            var state = instance.AddComponent<CabEngineState>();
            foreach (string tag in new[] { "Water", "Oil" })
            {
                var gauge = Find(tag + "_Temperature_Meter").gameObject.AddComponent<CabTemperatureGauge>();
                gauge.Configure(tag == "Oil", Find(tag + "_Temperature_Indicator"), Find(tag + "_Calibration_Origin"),
                    Find(tag + "_Calibration_AxisX"), Find(tag + "_Calibration_AxisY"), Find(tag + "_Calibration_AxisZ"));
                gauge.BindEngineState(state);
            }
            var current = instance.AddComponent<CabLinearCurrentGauge>();
            current.Configure(Find("Curved_Indicator_Carriage"), Find("Current_Calibration_Origin"),
                Find("Current_Calibration_AxisX"), Find("Current_Calibration_AxisY"), Find("Current_Calibration_AxisZ"));
            current.BindEngineState(state);
            var rpm = instance.AddComponent<CabDieselTachometer>();
            rpm.Configure(Find("Tachometer_Reading_Carriage"), Find("RPM_Calibration_Origin"),
                Find("RPM_Calibration_AxisX"), Find("RPM_Calibration_AxisY"), Find("RPM_Calibration_AxisZ"));
            rpm.BindEngineState(state);
            var pressure = instance.AddComponent<CabDuplexPressureGauges>();
            pressure.Configure(new[] {Find("MR_BC_Red_Pivot"),Find("MR_BC_Black_Pivot"),Find("BP_ER_Red_Pivot"),Find("BP_ER_Black_Pivot")},
                new[] {Find("MR_BC_Calibration_AxisY"),Find("MR_BC_Calibration_AxisY"),Find("BP_ER_Calibration_AxisY"),Find("BP_ER_Calibration_AxisY")},
                new[] {Find("MR_BC_Calibration_Origin"),Find("MR_BC_Calibration_Origin"),Find("BP_ER_Calibration_Origin"),Find("BP_ER_Calibration_Origin")});
            pressure.BindEngineState(state);
            ValidateAssembly(instance, state);
            PrefabUtility.SaveAsPrefabAsset(instance, Output + "/" + instance.name + ".prefab");
            UnityEngine.Object.DestroyImmediate(instance);
        }
        AssetDatabase.SaveAssets();
        CreateReviewScene();
        Debug.Log("CAB_MAIN_CONSOLE_IMPORT_OK: URP prefab and review scene created");
    }

    static void ValidateAssembly(GameObject model, CabEngineState state)
    {
        state.StartEngine(); state.SetReverser(ReverserPosition.Forward); state.SetThrottle(4); state.UpdatePhysics(.5f);
        var rpm=model.GetComponent<CabDieselTachometer>();rpm.RefreshFromState();
        var current=model.GetComponent<CabLinearCurrentGauge>();current.RefreshFromState();
        var pressure=model.GetComponent<CabDuplexPressureGauges>();pressure.RefreshFromState();
        if(rpm.InputRpm!=state.EngineRpm || current.InputAmps!=state.TractionCurrentAmps)
            throw new InvalidOperationException("Shared engine state binding failed");
        foreach(var gauge in model.GetComponentsInChildren<CabTemperatureGauge>())
        {
            gauge.RefreshFromState();
            if(gauge.IsOilChannel ? gauge.HasValidInput : gauge.InputCelsius!=state.WaterTempCelsius)
                throw new InvalidOperationException("Temperature source mismatch");
        }
        if(pressure.HasEqualizingReservoirSignal)throw new InvalidOperationException("Missing ER channel invented");
        state.StopEngine();state.UpdatePhysics(3);rpm.RefreshFromState();current.RefreshFromState();
        if(rpm.DisplayedRpm!=0)throw new InvalidOperationException("Stop state mismatch");
        Debug.Log("CAB_MAIN_CONSOLE_BINDINGS_OK: shared state; missing oil/ER invalid; stop checked");
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
        bool replaceBatchPlaceholder = Application.isBatchMode && string.IsNullOrEmpty(previous.path);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
            replaceBatchPlaceholder ? NewSceneMode.Single : NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Output + "/MainConsoleStudy.prefab");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        foreach (var child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
        var state = model.GetComponent<CabEngineState>();
        var liveGauges = model.GetComponentsInChildren<CabTemperatureGauge>();
        foreach (var channel in liveGauges) channel.BindEngineState(state);
        var renderers = model.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        if (bounds.size.y < .8f || bounds.size.y > 1.6f)
            throw new InvalidOperationException("Unexpected imported metre scale: " + bounds.size);
        var camera = new GameObject("ReviewCamera").AddComponent<Camera>();
        camera.transform.position = bounds.center + new Vector3(-1.7f,1.15f,2.7f);
        camera.transform.LookAt(bounds.center);
        camera.nearClipPlane = .01f;
        camera.farClipPlane = 20;
        camera.orthographic = true;
        camera.orthographicSize = .84f;
        camera.backgroundColor = new Color(.78f,.80f,.82f);
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
        const string reflectionPath = "Assets/Models/Cab/UnityMainConsole/ReviewReflection.cubemap";
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
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/CabMainConsoleReview.unity");
        var target = new RenderTexture(1400, 1050, 24);
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
        var image = new Texture2D(1400, 1050, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1400, 1050), 0, 0);
        image.Apply();
        File.WriteAllBytes("tools/cab_reference/console_review/unity_main_console_review.png", image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = previousTarget;
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(image);
        Debug.Log("CAB_MAIN_CONSOLE_BOUNDS: " + bounds.size + "; renderers=" + renderers.Length);
        foreach (var channel in liveGauges)
        {
            channel.BindEngineState(state);
            PrefabUtility.RecordPrefabInstancePropertyModifications(channel);
        }
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/CabMainConsoleReview.unity");
        if (previous.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
        if (!replaceBatchPlaceholder) EditorSceneManager.CloseScene(scene, true);
    }
}
