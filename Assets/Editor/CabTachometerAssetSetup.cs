using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using RailwayRenaissance.Cab;

[InitializeOnLoad]
public static class CabTachometerAssetSetup
{
    static int reviewFrames;
    public static void BatchBuild()
    {
        Build();
        EditorSceneManager.OpenScene("Assets/Scenes/CabTachometerReview.unity", OpenSceneMode.Single);
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
                var gauge = UnityEngine.Object.FindFirstObjectByType<CabDieselTachometer>();
                gauge.SetRpm(650f);
                var camera = Camera.main;
                var target = new RenderTexture(1000, 900, 24);
                var previous = RenderTexture.active;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(1000, 900, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1000, 900), 0, 0);
                image.Apply();
                File.WriteAllBytes("tools/cab_reference/tachometer_review/unity_tachometer_review.png", image.EncodeToPNG());
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
                Debug.Log("CAB_TACHOMETER_RELOAD_RENDER_OK");
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

    const string Source = "Assets/Models/Cab/UnityTachometer";
    const string Output = "Assets/Resources/Cab";
    const string Request = "tools/compile/cab_tachometer_import.request";
    const string Result = "tools/compile/cab_tachometer_import_result.json";
    static CabTachometerAssetSetup() { EditorApplication.update += CheckRequest; }
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

    [MenuItem("RailRevival/驾驶台/导入柴油机转速表定稿")]
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
            mat.SetFloat("_BumpScale", info.name == "Gauge_Scale" ? .25f : 1f);
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
            string modelPath = Source + "/Tachometer_final.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) throw new InvalidOperationException("FBX import failed: " + modelPath);
            var instance = UnityEngine.Object.Instantiate(model);
            instance.name = "DieselTachometerFinal";
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
            var gauge = instance.AddComponent<CabDieselTachometer>();
            gauge.Configure(Find("Tachometer_Reading_Carriage"), Find("RPM_Calibration_Origin"),
                Find("RPM_Calibration_AxisX"), Find("RPM_Calibration_AxisY"), Find("RPM_Calibration_AxisZ"));
            ValidateGauge(gauge, Find("Tachometer_Reading_Carriage"), Find("RPM_Calibration_Origin"),
                Find("RPM_Calibration_AxisX"), Find("RPM_Calibration_AxisY"), Find("RPM_Calibration_AxisZ"));
            gauge.SetRpm(650f);
            PrefabUtility.SaveAsPrefabAsset(instance, Output + "/" + instance.name + ".prefab");
            UnityEngine.Object.DestroyImmediate(instance);
        }
        AssetDatabase.SaveAssets();
        CreateReviewScene();
        Debug.Log("CAB_TACHOMETER_IMPORT_OK: URP prefab and review scene created");
    }

    static void ValidateGauge(CabDieselTachometer gauge, Transform moving, Transform origin,
        Transform axisX, Transform axisY, Transform axisZ)
    {
        var basis = Matrix4x4.identity;
        basis.SetColumn(0, (axisX.position - origin.position) / .01f);
        basis.SetColumn(1, (axisY.position - origin.position) / .01f);
        basis.SetColumn(2, (axisZ.position - origin.position) / .01f);
        basis.SetColumn(3, new Vector4(origin.position.x, origin.position.y, origin.position.z, 1));
        float radius = (.0715f * .0715f + .006f * .006f) / .012f;
        float low = Mathf.Asin(-.063f / radius);
        var referenceDirection = moving.InverseTransformVector(axisX.position - origin.position);
        float maxError = 0f;
        for (int i = 0; i <= 300; i++)
        {
            float u = i / 300f;
            gauge.SetRpm(u * 1500f);
            float angle = Mathf.Lerp(low, -low, u);
            var expected = new Vector3(.001f + radius - radius * Mathf.Cos(angle), -.0189f,
                .089f + radius * Mathf.Sin(angle));
            expected.x -= .006f;
            float error = Vector3.Distance(basis.inverse.MultiplyPoint3x4(moving.position), expected);
            maxError = Mathf.Max(maxError, error);
            if (error > .00001f) throw new InvalidOperationException("Gauge FBX coordinate mismatch: " + error);
            float delta = angle - Mathf.Lerp(low, -low, (650f / 1500f));
            var expectedDirection = (axisX.position - origin.position) * Mathf.Cos(delta)
                - (axisZ.position - origin.position) * Mathf.Sin(delta);
            if (Vector3.Distance(moving.TransformVector(referenceDirection), expectedDirection) > .00001f)
                throw new InvalidOperationException("Gauge FBX rotation mismatch");
        }
        gauge.SetRpm(1800f);
        if (!gauge.IsOutOfRange || gauge.DisplayedRpm != 1500f) throw new InvalidOperationException("High range handling failed");
        gauge.SetRpm(-1f);
        if (!gauge.IsOutOfRange || gauge.DisplayedRpm != 0f) throw new InvalidOperationException("Low range handling failed");
        gauge.SetRpm(float.NaN);
        if (gauge.HasValidInput || gauge.DisplayedRpm != 0f) throw new InvalidOperationException("Invalid input handling failed");
        var state = gauge.gameObject.AddComponent<CabEngineState>();
        state.StartEngine(); state.SetReverser(ReverserPosition.Forward); state.SetThrottle(4);
        state.UpdatePhysics(.5f);
        gauge.BindEngineState(state);
        if (state.EngineRpm <= 0 || gauge.InputRpm != state.EngineRpm)
            throw new InvalidOperationException("Live engine binding failed");
        state.UpdatePhysics(.5f);
        gauge.RefreshFromState();
        if (gauge.InputRpm != state.EngineRpm)
            throw new InvalidOperationException("Live engine frame update failed");
        state.SetThrottle(8); state.UpdatePhysics(3f); gauge.RefreshFromState();
        if (gauge.InputRpm <= 1500f || !gauge.IsOutOfRange || gauge.DisplayedRpm != 1500f)
            throw new InvalidOperationException("Engine overrange must remain visible to binding");
        state.StopEngine(); state.UpdatePhysics(3f); gauge.RefreshFromState();
        if (gauge.DisplayedRpm != 0f || gauge.IsOutOfRange)
            throw new InvalidOperationException("Engine stop did not return the tachometer to zero");
        gauge.BindEngineState(null);
        UnityEngine.Object.DestroyImmediate(state);
        Debug.Log("CAB_TACHOMETER_VALIDATION_OK: samples=301; max_model_error_m=" + maxError
            + "; range/invalid/live_engine checks passed");
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
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Output + "/DieselTachometerFinal.prefab");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        foreach (var child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
        var state = new GameObject("ReviewEngineState").AddComponent<CabEngineState>();
        var liveGauge = model.GetComponent<CabDieselTachometer>();
        liveGauge.BindEngineState(state);
        liveGauge.SetRpm(650f);
        var renderers = model.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        if (bounds.size.y < .1f || bounds.size.y > .5f)
            throw new InvalidOperationException("Unexpected imported metre scale: " + bounds.size);
        var camera = new GameObject("ReviewCamera").AddComponent<Camera>();
        camera.transform.position = bounds.center + new Vector3(-.13f,.04f,.38f);
        camera.transform.LookAt(bounds.center);
        camera.nearClipPlane = .01f;
        camera.farClipPlane = 20;
        camera.orthographic = true;
        camera.orthographicSize = .105f;
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
        const string reflectionPath = "Assets/Models/Cab/UnityTachometer/ReviewReflection.cubemap";
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
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/CabTachometerReview.unity");
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
        File.WriteAllBytes("tools/cab_reference/tachometer_review/unity_tachometer_review.png", image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = previousTarget;
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(image);
        Debug.Log("CAB_TACHOMETER_BOUNDS: " + bounds.size + "; renderers=" + renderers.Length);
        liveGauge.BindEngineState(state);
        PrefabUtility.RecordPrefabInstancePropertyModifications(liveGauge);
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/CabTachometerReview.unity");
        if (previous.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
        if (!replaceBatchPlaceholder) EditorSceneManager.CloseScene(scene, true);
    }
}
