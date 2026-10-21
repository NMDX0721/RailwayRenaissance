using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Keep the project's default renderer and colour space intact for the 2D scenes.
public static class NF5KLightingRendererSetup
{
    public static void Configure(Camera camera)
    {
        var pipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null) throw new InvalidOperationException("NF5K requires URP");
        const string path = "Assets/Settings/URP/NF5KForwardPlus.asset";
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, path);
        }
        renderer.renderingMode = RenderingMode.ForwardPlus;
        EditorUtility.SetDirty(renderer);
        var serialized = new SerializedObject(pipeline);
        // URP needs both the pipeline capability and each lamp's shadow request.
        serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue = true;
        serialized.FindProperty("m_SoftShadowsSupported").boolValue = true;
        serialized.FindProperty("m_SupportsLightLayers").boolValue = true;
        serialized.FindProperty("m_ReflectionProbeBlending").boolValue = true;
        serialized.FindProperty("m_ReflectionProbeBoxProjection").boolValue = true;
        serialized.FindProperty("m_AdditionalLightsShadowmapResolution").intValue = 4096;
        var list = serialized.FindProperty("m_RendererDataList");
        int index = -1;
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == renderer) index = i;
        if (index < 0)
        {
            index = list.arraySize;
            list.InsertArrayElementAtIndex(index);
            list.GetArrayElementAtIndex(index).objectReferenceValue = renderer;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        camera.GetUniversalAdditionalCameraData().SetRenderer(index);
        Debug.Log("NF5K_LIGHTING_RENDERER_OK: camera uses Forward+; default renderer unchanged; colour space=" + QualitySettings.activeColorSpace + "; additional shadows=" + pipeline.supportsAdditionalLightShadows);
    }

    public static void ConfigureSurfaces(Transform root, Camera camera)
    {
        const string layerName = "NF5K Approximate Indirect";
        int layerIndex = Array.IndexOf(RenderingLayerMask.GetDefinedRenderingLayerNames(), layerName);
        if (layerIndex < 0)
        {
            layerIndex = RenderingLayerMask.GetRenderingLayerCount();
            if (!RenderPipelineEditorUtility.TryAddRenderingLayerName(layerName)) throw new InvalidOperationException("Cannot register indirect rendering layer");
        }
        else layerIndex = Mathf.RoundToInt(Mathf.Log(RenderingLayerMask.GetDefinedRenderingLayerValues()[layerIndex],2));
        uint indirectMask = 1u << layerIndex;
        foreach (var surface in root.GetComponentsInChildren<Renderer>(true))
        {
            bool transparent = false;
            foreach (var material in surface.sharedMaterials)
                if (material != null && material.renderQueue >= 3000) transparent = true;
            // Real lamps affect both; approximate bounce must not appear as bulbs in glass.
            surface.renderingLayerMask = transparent ? 1u : 1u | indirectMask;
        }
        foreach (var lamp in root.GetComponentsInChildren<Light>(true))
            if (lamp.name.Contains("Bounce_Approximation") || lamp.name.Contains("Reflected_Light_Approximation"))
                lamp.GetUniversalAdditionalLightData().renderingLayers = indirectMask;
        Debug.Log("NF5K_INDIRECT_LAYER_OK: defined mask=" + indirectMask + "; glass excluded from approximation lights");
        const string path = "Assets/Settings/URP/NF5KLightingVolume.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path); }
        if (!profile.TryGet<Tonemapping>(out var tone)) { tone = profile.Add<Tonemapping>(); AssetDatabase.AddObjectToAsset(tone, profile); }
        tone.mode.Override(TonemappingMode.Neutral); EditorUtility.SetDirty(profile); EditorUtility.SetDirty(tone);
        var volume = new GameObject("NF5K_Lighting_Tonemapping").AddComponent<Volume>();
        volume.gameObject.layer = 30; volume.transform.SetParent(root, false); volume.isGlobal = true; volume.priority = 10; volume.sharedProfile = profile;
        camera.allowHDR = true;
        var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true; data.volumeLayerMask = 1 << 30;
        var probes = new ReflectionProbe[4];
        for (int i=0;i<probes.Length;i++)
        {
            var probe = new GameObject("Cabin_Local_Reflection_"+i).AddComponent<ReflectionProbe>();
            probe.transform.SetParent(root,false); probe.gameObject.layer=30;
            probe.transform.localPosition = new Vector3(0,1.1f,i==0?.7f:4.4f+(i-1)*5.1f);
            probe.size = new Vector3(2.75f,2.2f,i==0?2.4f:5.6f);
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.resolution=128; probe.hdr=true; probe.boxProjection=true; probe.blendDistance=.35f;
            probe.cullingMask=1<<30; probe.nearClipPlane=.05f; probe.farClipPlane=19;
            probe.clearFlags=UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor=Color.black; probe.intensity=.65f;
            probes[i]=probe;
        }
        root.gameObject.AddComponent<RailwayRenaissance.Cab.CabInteriorReflections>().Configure(probes);
    }
}
