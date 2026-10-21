using System;
using UnityEditor;
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
}
