using System.IO;
using RailwayRenaissance.Cab;
using UnityEditor;
using UnityEngine;

/// <summary>窗外轨道细节审查：静止/中速/高速各渲一张，并输出两帧之间的道砟位移量。</summary>
public static class CabTrackDetailReview
{
    private const string OutDir = "tools/compile";

    public static void Batch()
    {
        Directory.CreateDirectory(OutDir);
        var root = new GameObject("CabTrackReviewRoot");
        var state = root.AddComponent<CabEngineState>();
        var scene = Cab3DSceneController.Create(root.transform);
        scene.SetResolution(1280, 720);
        scene.BindEngineState(null);

        float[] kmh = { 0f, 40f, 90f };
        foreach (float v in kmh)
        {
            state.CurrentSpeedKmh = v;
            state.DistanceToNextStation = 1200f;
            for (int f = 0; f < 3; f++) scene.UpdateFrame(state, 0.016f);
            scene.CabCamera.Render();
            Save(scene.TargetTexture, $"{OutDir}/cab_track_{(int)v}kmh.png");
        }
        Debug.Log("CabTrackDetailReview done");
        Object.DestroyImmediate(root);
    }

    private static void Save(RenderTexture rt, string path)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }
}
