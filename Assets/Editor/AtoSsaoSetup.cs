using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Adds a (disabled) screen-space ambient occlusion feature to the shared URP renderer. It stays off for every scene
// until AtoRenderQuality switches it on for the ATO scenes, so nothing else in the project changes.
public static class AtoSsaoSetup
{
    // The ATO and cab scenes render with the Forward+ renderer (see NF5KLightingRendererSetup), the default renderer is the other entry.
    static readonly string[] RendererPaths = { "Assets/Settings/URP/UniversalRenderer.asset", "Assets/Settings/URP/NF5KForwardPlus.asset" };

    public static void Batch()
    {
        foreach (var path in RendererPaths) AddTo(path);
        EditorApplication.Exit(0);
    }

    static void AddTo(string RendererPath)
    {
        var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (data == null) { Debug.LogError("SSAO_SETUP renderer data missing " + RendererPath); return; }
        ScreenSpaceAmbientOcclusion existing = null;
        foreach (var f in data.rendererFeatures) if (f is ScreenSpaceAmbientOcclusion e) existing = e;
        if (existing != null) { Tune(existing); Debug.Log("SSAO_SETUP retuned " + RendererPath); return; }
        var ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
        ssao.name = "Ato_ScreenSpaceAO";
        AssetDatabase.AddObjectToAsset(ssao, data);
        var so = new SerializedObject(data);
        var features = so.FindProperty("m_RendererFeatures");
        var map = so.FindProperty("m_RendererFeatureMap");
        features.arraySize++; features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = ssao;
        map.arraySize++; map.GetArrayElementAtIndex(map.arraySize - 1).longValue = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ssao, out _, out long id) ? id : 0;
        so.ApplyModifiedPropertiesWithoutUndo();
        ssao.SetActive(false);
        EditorUtility.SetDirty(ssao); EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Tune(ssao);
        Debug.Log("SSAO_SETUP added " + RendererPath);
    }

    // Soft and quiet: blue-noise sampling with a high-quality blur, so it reads as contact shadow rather than grain.
    static void Tune(ScreenSpaceAmbientOcclusion ssao)
    {
        var sso = new SerializedObject(ssao);
        var s = sso.FindProperty("m_Settings");
        if (s == null) { Debug.Log("SSAO_SETUP no m_Settings"); return; }
        void F(string n, float v) { var p = s.FindPropertyRelative(n); if (p != null) p.floatValue = v; else Debug.Log("SSAO_SETUP missing " + n); }
        void E(string n, int v) { var p = s.FindPropertyRelative(n); if (p != null) p.enumValueIndex = v; else Debug.Log("SSAO_SETUP missing " + n); }
        F("Intensity", .75f); F("Radius", .28f); F("DirectLightingStrength", .15f); F("Falloff", 60f);
        E("AOMethod", 0); E("Samples", 0); E("BlurQuality", 0); E("Source", 1);
        sso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(ssao);
        AssetDatabase.SaveAssets();
    }
}
