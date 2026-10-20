using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using RailwayRenaissance.Cab;

[InitializeOnLoad]
public static class CabTemperatureAssetSetup
{
    static int reviewFrames;
    public static void BatchBuild()
    {
        Build();
        EditorSceneManager.OpenScene("Assets/Scenes/CabTemperatureReview.unity", OpenSceneMode.Single);
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
                var target = new RenderTexture(1000, 900, 24);
                var previous = RenderTexture.active;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(1000, 900, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1000, 900), 0, 0);
                image.Apply();
                File.WriteAllBytes("tools/cab_reference/temperature_review/unity_temperature_review.png", image.EncodeToPNG());
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
                Debug.Log("CAB_TEMPERATURE_RELOAD_RENDER_OK");
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

    const string Source = "Assets/Models/Cab/UnityTemperatureGauges";
    const string Output = "Assets/Resources/Cab";
    const string Request = "tools/compile/cab_temperature_import.request";
    const string Result = "tools/compile/cab_temperature_import_result.json";
    static CabTemperatureAssetSetup() { EditorApplication.update += CheckRequest; }
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

    [MenuItem("RailRevival/Cab/Import Final Temperature Gauges")]
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
            string modelPath = Source + "/TemperatureGauges_final.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) throw new InvalidOperationException("FBX import failed: " + modelPath);
            var instance = UnityEngine.Object.Instantiate(model);
            instance.name = "TemperatureGaugesFinal";
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
            foreach (string tag in new[] { "Water", "Oil" })
            {
                var gauge = Find(tag + "_Temperature_Meter").gameObject.AddComponent<CabTemperatureGauge>();
                gauge.Configure(tag == "Oil", Find(tag + "_Temperature_Indicator"), Find(tag + "_Calibration_Origin"),
                    Find(tag + "_Calibration_AxisX"), Find(tag + "_Calibration_AxisY"), Find(tag + "_Calibration_AxisZ"));
                ValidateGauge(gauge, Find(tag + "_Temperature_Indicator"), Find(tag + "_Calibration_Origin"),
                    Find(tag + "_Calibration_AxisX"), Find(tag + "_Calibration_AxisY"), Find(tag + "_Calibration_AxisZ"));
                gauge.SetTemperature(float.NaN);
            }
            PrefabUtility.SaveAsPrefabAsset(instance, Output + "/" + instance.name + ".prefab");
            UnityEngine.Object.DestroyImmediate(instance);
        }
        AssetDatabase.SaveAssets();
        CreateReviewScene();
        Debug.Log("CAB_TEMPERATURE_IMPORT_OK: URP prefab and review scene created");
    }

    static void ValidateGauge(CabTemperatureGauge gauge, Transform moving, Transform origin,
        Transform axisX, Transform axisY, Transform axisZ)
    {
        var basis = Matrix4x4.identity;
        basis.SetColumn(0, (axisX.position - origin.position) / .01f);
        basis.SetColumn(1, (axisY.position - origin.position) / .01f);
        basis.SetColumn(2, (axisZ.position - origin.position) / .01f);
        basis.SetColumn(3, new Vector4(origin.position.x, origin.position.y, origin.position.z, 1));
        var c = gauge.Calibration;
        float radius = (.0715f * .0715f + .006f * .006f) / .012f;
        float low = Mathf.Asin(-.063f / radius), maxError = 0;
        gauge.SetTemperature(c.previewC);
        var referenceDirection = moving.InverseTransformVector(axisX.position - origin.position);
        for (int i = 0; i <= 300; i++)
        {
            float u = i / 300f;
            gauge.SetTemperature(Mathf.Lerp(c.minimumC, c.maximumC, u));
            float angle = Mathf.Lerp(low, -low, u);
            var expected = new Vector3(.001f + radius - radius * Mathf.Cos(angle) - .006f,
                -.01855f, .089f + radius * Mathf.Sin(angle));
            float error = Vector3.Distance(basis.inverse.MultiplyPoint3x4(moving.position), expected);
            maxError = Mathf.Max(maxError, error);
            if (error > .00001f) throw new InvalidOperationException("Temperature FBX coordinate mismatch");
            float delta = angle - Mathf.Lerp(low, -low, Mathf.InverseLerp(c.minimumC,c.maximumC,c.previewC));
            var expectedDirection = (axisX.position-origin.position)*Mathf.Cos(delta)
                - (axisZ.position-origin.position)*Mathf.Sin(delta);
            if (Vector3.Distance(moving.TransformVector(referenceDirection), expectedDirection) > .00001f)
                throw new InvalidOperationException("Temperature rotation mismatch");
        }
        gauge.SetTemperature(-20);
        if (!gauge.UnderRange || gauge.InputCelsius != -20 || gauge.DisplayedCelsius != c.minimumC)
            throw new InvalidOperationException("Cold raw value lost");
        gauge.SetTemperature(180);
        if (!gauge.OverRange || !gauge.InRedZone || gauge.InputCelsius != 180 || gauge.DisplayedCelsius != c.maximumC)
            throw new InvalidOperationException("Overrange raw value lost");
        gauge.SetTemperature(c.redStartC - .01f);
        if (gauge.InRedZone) throw new InvalidOperationException("Red zone early");
        gauge.SetTemperature(c.redStartC);
        if (!gauge.InRedZone) throw new InvalidOperationException("Red zone threshold failed");
        foreach (float invalid in new[] {float.NaN, float.PositiveInfinity, float.NegativeInfinity})
        {
            gauge.SetTemperature(invalid);
            if (gauge.HasValidInput || gauge.UnderRange || gauge.OverRange || gauge.InRedZone)
                throw new InvalidOperationException("Invalid temperature treated as measurement");
        }
        var state = gauge.gameObject.AddComponent<CabEngineState>();
        gauge.BindEngineState(state);
        if (gauge.IsOilChannel && gauge.HasValidInput) throw new InvalidOperationException("Missing oil signal was invented");
        if (!gauge.IsOilChannel && gauge.InputCelsius != state.WaterTempCelsius)
            throw new InvalidOperationException("Water signal binding failed");
        state.StartEngine(); state.UpdatePhysics(.5f); gauge.RefreshFromState();
        if (!gauge.IsOilChannel && gauge.InputCelsius != state.WaterTempCelsius)
            throw new InvalidOperationException("Live water update failed");
        gauge.BindEngineState(null);
        if (gauge.HasValidInput) throw new InvalidOperationException("Unbound input remains valid");
        UnityEngine.Object.DestroyImmediate(state);
        Debug.Log("CAB_TEMPERATURE_VALIDATION_OK: oil=" + gauge.IsOilChannel + "; samples=301; max_error_m=" + maxError);
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
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Output + "/TemperatureGaugesFinal.prefab");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        foreach (var child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
        var state = new GameObject("ReviewEngineState").AddComponent<CabEngineState>();
        var liveGauges = model.GetComponentsInChildren<CabTemperatureGauge>();
        foreach (var channel in liveGauges) channel.BindEngineState(state);
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
        const string reflectionPath = "Assets/Models/Cab/UnityTemperatureGauges/ReviewReflection.cubemap";
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
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/CabTemperatureReview.unity");
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
        File.WriteAllBytes("tools/cab_reference/temperature_review/unity_temperature_review.png", image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = previousTarget;
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(image);
        Debug.Log("CAB_TEMPERATURE_BOUNDS: " + bounds.size + "; renderers=" + renderers.Length);
        foreach (var channel in liveGauges)
        {
            channel.BindEngineState(state);
            PrefabUtility.RecordPrefabInstancePropertyModifications(channel);
        }
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/CabTemperatureReview.unity");
        if (previous.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
        if (!replaceBatchPlaceholder) EditorSceneManager.CloseScene(scene, true);
    }
}
