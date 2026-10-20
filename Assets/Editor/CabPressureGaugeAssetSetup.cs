using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using RailwayRenaissance.Cab;

[InitializeOnLoad]
public static class CabPressureGaugeAssetSetup
{
    static int reviewFrames;
    public static void BatchBuild()
    {
        Build();
        EditorSceneManager.OpenScene("Assets/Scenes/CabPressureGaugeReview.unity", OpenSceneMode.Single);
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
                var gauge = UnityEngine.Object.FindFirstObjectByType<CabDuplexPressureGauges>();
                gauge.SetPressures(840f,180f,500f,470f);
                var camera = Camera.main;
                var target = new RenderTexture(1000, 900, 24);
                var previous = RenderTexture.active;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(1000, 900, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1000, 900), 0, 0);
                image.Apply();
                File.WriteAllBytes("tools/cab_reference/pressure_review/unity_pressure_gauges_review.png", image.EncodeToPNG());
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
                Debug.Log("CAB_PRESSURE_GAUGES_RELOAD_RENDER_OK");
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

    const string Source = "Assets/Models/Cab/UnityPressureGauges";
    const string Output = "Assets/Resources/Cab";
    const string Request = "tools/compile/cab_pressure_gauges_import.request";
    const string Result = "tools/compile/cab_pressure_gauges_import_result.json";
    static CabPressureGaugeAssetSetup() { EditorApplication.update += CheckRequest; }
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

    [MenuItem("RailRevival/驾驶台/导入双针压力表定稿")]
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
            mat.SetFloat("_BumpScale", info.name == "Pressure_IvoryDial" ? .20f : 1f);
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
                mat.SetColor("_BaseColor", new Color(.92f,.96f,.94f,.08f));
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
            string modelPath = Source + "/PressureGauges_final.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) throw new InvalidOperationException("FBX import failed: " + modelPath);
            var instance = UnityEngine.Object.Instantiate(model);
            instance.name = "DuplexPressureGaugesFinal";
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
            var gauge = instance.AddComponent<CabDuplexPressureGauges>();
            var moving = new[] { Find("MR_BC_Red_Pivot"), Find("MR_BC_Black_Pivot"),
                Find("BP_ER_Red_Pivot"), Find("BP_ER_Black_Pivot") };
            var origins = new[] { Find("MR_BC_Calibration_Origin"), Find("MR_BC_Calibration_Origin"),
                Find("BP_ER_Calibration_Origin"), Find("BP_ER_Calibration_Origin") };
            var ys = new[] { Find("MR_BC_Calibration_AxisY"), Find("MR_BC_Calibration_AxisY"),
                Find("BP_ER_Calibration_AxisY"), Find("BP_ER_Calibration_AxisY") };
            var xs = new[] { Find("MR_BC_Calibration_AxisX"), Find("MR_BC_Calibration_AxisX"),
                Find("BP_ER_Calibration_AxisX"), Find("BP_ER_Calibration_AxisX") };
            var zs = new[] { Find("MR_BC_Calibration_AxisZ"), Find("MR_BC_Calibration_AxisZ"),
                Find("BP_ER_Calibration_AxisZ"), Find("BP_ER_Calibration_AxisZ") };
            gauge.Configure(moving, ys, origins);
            ValidateGauge(gauge, moving, origins, xs, zs);
            gauge.SetPressures(840f,180f,500f,470f);
            PrefabUtility.SaveAsPrefabAsset(instance, Output + "/" + instance.name + ".prefab");
            UnityEngine.Object.DestroyImmediate(instance);
        }
        AssetDatabase.SaveAssets();
        CreateReviewScene();
        Debug.Log("CAB_PRESSURE_GAUGES_IMPORT_OK: URP prefab and review scene created");
    }

    static void ValidateGauge(CabDuplexPressureGauges gauge, Transform[] moving, Transform[] origins,
        Transform[] xs, Transform[] zs)
    {
        float[] reference = { 840f/1600f,180f/1600f,.5f,.47f };
        var directions = new Vector3[4];
        for (int c=0;c<4;c++) directions[c] = moving[c].InverseTransformVector(xs[c].position-origins[c].position);
        for (int i=0;i<=360;i++)
        {
            float u=i/360f;
            gauge.SetPressures(1600f*u,1600f*u,1000f*u,1000f*u);
            for (int c=0;c<4;c++)
            {
                float delta=270f*(u-reference[c])*Mathf.Deg2Rad;
                var expected=(xs[c].position-origins[c].position)*Mathf.Cos(delta)
                    -(zs[c].position-origins[c].position)*Mathf.Sin(delta);
                if (Vector3.Distance(moving[c].TransformVector(directions[c]),expected)>.00001f)
                    throw new InvalidOperationException("Duplex pointer coordinate/rotation mismatch: channel "+c);
            }
        }
        gauge.SetPressures(-1f,2000f,float.NaN,float.PositiveInfinity);
        if (!gauge.OutOfRange[0] || !gauge.OutOfRange[1] || gauge.DisplayedKpa[0]!=0f
            || gauge.DisplayedKpa[1]!=1600f || gauge.ValidChannels[2] || gauge.HasEqualizingReservoirSignal
            || !moving[3].gameObject.activeSelf || gauge.ValidChannels[3]) throw new InvalidOperationException("Duplex invalid/range handling failed");
        var state=gauge.gameObject.AddComponent<CabEngineState>();
        state.StartEngine();state.SetBrakeNotch(BrakeNotch.Full);state.UpdatePhysics(.5f);
        gauge.BindEngineState(state);
        state.UpdatePhysics(.5f);gauge.RefreshFromState();
        if (gauge.DisplayedKpa[0]!=state.MainReservoirKpa || gauge.DisplayedKpa[1]!=state.BrakeCylinderKpa
            || gauge.DisplayedKpa[2]!=state.BrakePipeKpa || gauge.HasEqualizingReservoirSignal)
            throw new InvalidOperationException("Duplex live pressure binding failed");
        gauge.BindEngineState(null);UnityEngine.Object.DestroyImmediate(state);
        Debug.Log("CAB_PRESSURE_GAUGES_VALIDATION_OK: 4x361 rotation samples; range/invalid/live-pressure checks passed; ER unavailable");
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
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Output + "/DuplexPressureGaugesFinal.prefab");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        foreach (var child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
        var state = new GameObject("ReviewEngineState").AddComponent<CabEngineState>();
        var liveGauge = model.GetComponent<CabDuplexPressureGauges>();
        liveGauge.BindEngineState(state);
        liveGauge.SetPressures(840f,180f,500f,470f);
        var renderers = model.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        if (bounds.size.y < .1f || bounds.size.y > .5f)
            throw new InvalidOperationException("Unexpected imported metre scale: " + bounds.size);
        var camera = new GameObject("ReviewCamera").AddComponent<Camera>();
        camera.transform.position = bounds.center + new Vector3(.18f,.10f,.50f);
        camera.transform.LookAt(bounds.center);
        camera.nearClipPlane = .01f;
        camera.farClipPlane = 20;
        camera.orthographic = true;
        camera.orthographicSize = .17f;
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
        const string reflectionPath = "Assets/Models/Cab/UnityPressureGauges/ReviewReflection.cubemap";
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
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/CabPressureGaugeReview.unity");
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
        File.WriteAllBytes("tools/cab_reference/pressure_review/unity_pressure_gauges_review.png", image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = previousTarget;
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(image);
        Debug.Log("CAB_PRESSURE_GAUGES_BOUNDS: " + bounds.size + "; renderers=" + renderers.Length);
        liveGauge.BindEngineState(state);
        PrefabUtility.RecordPrefabInstancePropertyModifications(liveGauge);
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/CabPressureGaugeReview.unity");
        if (previous.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
        if (!replaceBatchPlaceholder) EditorSceneManager.CloseScene(scene, true);
    }
}
