using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RailwayRenaissance.Cab;

// Batch check of real-time anti-aliasing on the ATO line: one un-accumulated frame per setting, so what is saved is what the player's GPU draws.
public static class AtoAaReview
{
    public static void Batch()
    {
        Directory.CreateDirectory("tools/ato_track/review");
        EditorSceneManager.OpenScene(AtoLineClosedLoop.GradedScenePath);
        var cam = Camera.main;
        var data = cam.GetUniversalAdditionalCameraData();
        var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        int originalSamples = asset.msaaSampleCount;
        var originalMode = data.antialiasing;
        float y = CabReviewTrack.RailDatum;
        var group = GameObject.Find("ATO_Test_Line_Graded");
        Debug.Log("AA_REVIEW rail datum " + y + " line=" + (group != null));
        cam.transform.SetParent(null);
        cam.transform.position = new Vector3(0.65f, 1.38f, 0.78f);   // review scene: vehicle at the origin, driver eye in the vehicle frame
        cam.transform.rotation = Quaternion.LookRotation(new Vector3(-.05f, -.16f, -1.58f));
        cam.fieldOfView = 60; cam.farClipPlane = 3000; cam.nearClipPlane = 0.1f;
        try
        {
            foreach (var (name, samples, mode) in new[] { ("aa_off", 1, AntialiasingMode.None), ("aa_msaa4", 4, AntialiasingMode.None), ("aa_msaa8", 8, AntialiasingMode.None), ("aa_final", 8, AntialiasingMode.None) })
            {
                asset.msaaSampleCount = samples;
                bool final = name == "aa_final";
                QualitySettings.anisotropicFiltering = final ? AnisotropicFiltering.ForceEnable : AnisotropicFiltering.Enable;
                RenderSettings.fog = final; RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = 0.0018f;
                asset.shadowCascadeCount = final ? 4 : 1; asset.shadowDistance = final ? 120f : 50f; asset.renderScale = final ? 1.5f : 1f; RenderSettings.fogColor = new Color(.72f, .75f, .77f);
                data.antialiasing = mode;
                var rt = new RenderTexture(2000, 1000, 24, RenderTextureFormat.ARGB32) { antiAliasing = samples };
                var previous = RenderTexture.active; var old = cam.targetTexture;
                cam.targetTexture = rt;
                for (int i = 0; i < 3; i++) cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(2000, 1000, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 2000, 1000), 0, 0); tex.Apply();
                File.WriteAllBytes("tools/ato_track/review/" + name + ".png", tex.EncodeToPNG());
                cam.targetTexture = old; RenderTexture.active = previous;
                Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
        finally { asset.shadowCascadeCount = 1; asset.shadowDistance = 50f; asset.renderScale = 1f; asset.msaaSampleCount = originalSamples; data.antialiasing = originalMode; }
        Debug.Log("AA_REVIEW done");
        EditorApplication.Exit(0);
    }
}
