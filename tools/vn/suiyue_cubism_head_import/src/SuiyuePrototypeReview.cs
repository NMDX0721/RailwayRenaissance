using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Live2D.Cubism.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class SuiyuePrototypeReview
{
    private const string Pending = "SuiyuePrototypeReview.Pending";
    private const string Blink = "SuiyuePrototypeReview.Blink";
    private const string Controller = "SuiyuePrototypeReview.Controller";
    private static string ModelPath => SessionState.GetBool(Blink, false)
        ? "Assets/Review/SuiyueBlink/suiyue_head_blink_prototype.prefab"
        : "Assets/Review/SuiyuePrototype/suiyue_head_prototype.prefab";
    private static string Output => "D:/Unity Project/RailwayRenaissance/tools/vn/suiyue_cubism_head_import/" +
        (SessionState.GetBool(Controller, false) ? "unity_controller_acceptance" :
         SessionState.GetBool(Blink, false) ? "unity_blink_acceptance" : "unity_acceptance");
    private static int frames;
    private static double deadline;

    static SuiyuePrototypeReview()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        deadline = EditorApplication.timeSinceStartup + 180;
        EditorApplication.update += CheckPlay;
    }

    public static void Batch()
    {
        if (!Application.dataPath.Replace('\\', '/').EndsWith("/VNLive2DReviewProject/Assets"))
            throw new InvalidOperationException("The isolated Live2D review project is required.");
        Directory.CreateDirectory(Output);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (prefab == null) throw new InvalidOperationException("Suiyue prefab was not imported.");
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Review/Live2DReviewPipeline.asset");
        if (pipeline == null) throw new InvalidOperationException("The reviewed URP pipeline is missing.");
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        model.name = "SUIYUE_CUSTOM_STATIC_PROTOTYPE";
        foreach (var animator in model.GetComponentsInChildren<Animator>()) animator.enabled = false;
        if (SessionState.GetBool(Controller, false))
        {
            var cubism = model.GetComponent<CubismModel>();
            foreach (var id in new[] { "ParamEyeLOpen", "ParamEyeROpen" })
                cubism.Parameters.First(parameter => parameter.Id == id).gameObject
                    .AddComponent<Live2D.Cubism.Framework.CubismEyeBlinkParameter>();
            var blink = model.AddComponent<Live2D.Cubism.Framework.CubismEyeBlinkController>();
            blink.BlendMode = Live2D.Cubism.Framework.CubismParameterBlendMode.Override;
            blink.Refresh();
            model.GetComponent<Live2D.Cubism.Framework.CubismUpdateController>().Refresh();
        }
        var camera = new GameObject("SuiyuePrototypeCamera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 2;
        camera.transform.position = new Vector3(0, 0, -10);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0, 0, 0, 0);
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        EditorSceneManager.SaveScene(scene, "Assets/Review/SuiyuePrototypeReview.unity");
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    public static void BatchBlink()
    {
        SessionState.SetBool(Blink, true);
        Batch();
    }

    public static void BatchController()
    {
        SessionState.SetBool(Blink, true);
        SessionState.SetBool(Controller, true);
        Batch();
    }

    private static void CheckPlay()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Suiyue Play check timed out.");
            if (++frames < 90) return;
            var model = UnityEngine.Object.FindAnyObjectByType<CubismModel>();
            if (model == null) throw new InvalidOperationException("Suiyue CubismModel is missing in Play.");
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length < 60) throw new InvalidOperationException("Expected at least 60 Suiyue drawables; got " + renderers.Length);
            var ids = model.Parameters.Select(parameter => parameter.Id).ToArray();
            foreach (var required in new[] { "ParamEyeLOpen", "ParamEyeROpen", "ParamEyeBallX", "ParamEyeBallY", "ParamMouthOpenY" })
                if (!ids.Contains(required)) throw new InvalidOperationException("Missing Cubism parameter " + required);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10);
            camera.orthographicSize = Mathf.Max(0.5f, bounds.extents.y * 1.10f);
            var target = new RenderTexture(2560, 1440, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            var pixels = image.GetPixels32();
            int visible = pixels.Count(pixel => pixel.a > 200);
            int clear = pixels.Count(pixel => pixel.a == 0);
            if (visible < 20000 || clear < 20000)
                throw new InvalidOperationException($"Unexpected render alpha: visible={visible}, clear={clear}");
            File.WriteAllBytes(Path.Combine(Output, "suiyue_static_2560x1440.png"), image.EncodeToPNG());
            var openEyeDrawables = EyeRendererOpacities(model);
            var parameterStore = model.GetComponent<Live2D.Cubism.Framework.CubismParameterStore>();
            if (parameterStore != null) parameterStore.enabled = false;
            var leftEye = model.Parameters.First(parameter => parameter.Id == "ParamEyeLOpen");
            var rightEye = model.Parameters.First(parameter => parameter.Id == "ParamEyeROpen");
            var blinkController = model.GetComponent<Live2D.Cubism.Framework.CubismEyeBlinkController>();
            if (SessionState.GetBool(Controller, false))
            {
                blinkController.EyeOpening = 0f;
                blinkController.OnLateUpdate();
            }
            else
            {
                leftEye.Value = 0f;
                rightEye.Value = 0f;
            }
            model.ForceUpdateNow();
            // The renderer uses two mesh buffers; the second update displays the first update's opacity.
            model.ForceUpdateNow();
            camera.Render();
            var closed = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            RenderTexture.active = target;
            closed.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            closed.Apply();
            RenderTexture.active = previous;
            var closedPixels = closed.GetPixels32();
            int eyeClosedPixelDiff = 0;
            for (int i = 0; i < pixels.Length; i++)
                if (pixels[i].r != closedPixels[i].r || pixels[i].g != closedPixels[i].g ||
                    pixels[i].b != closedPixels[i].b || pixels[i].a != closedPixels[i].a)
                    eyeClosedPixelDiff++;
            File.WriteAllBytes(Path.Combine(Output, "suiyue_eye_parameters_zero_2560x1440.png"), closed.EncodeToPNG());
            Debug.Log($"SUIYUE_EYE_AUDIT left={leftEye.Value} right={rightEye.Value} pixelDiff={eyeClosedPixelDiff} open={openEyeDrawables} closed={EyeRendererOpacities(model)}");
            if (SessionState.GetBool(Blink, false) && eyeClosedPixelDiff < 100)
                throw new InvalidOperationException("Blink export did not change enough pixels: " + eyeClosedPixelDiff);
            if (SessionState.GetBool(Controller, false))
            {
                blinkController.EyeOpening = 1f;
                blinkController.OnLateUpdate();
            }
            else
            {
                leftEye.Value = 1f;
                rightEye.Value = 1f;
            }
            model.ForceUpdateNow();
            model.ForceUpdateNow();
            camera.Render();
            var reopened = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            RenderTexture.active = target;
            reopened.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            reopened.Apply();
            RenderTexture.active = previous;
            var reopenedPixels = reopened.GetPixels32();
            int eyeReopenPixelDiff = 0;
            for (int i = 0; i < pixels.Length; i++)
                if (pixels[i].r != reopenedPixels[i].r || pixels[i].g != reopenedPixels[i].g ||
                    pixels[i].b != reopenedPixels[i].b || pixels[i].a != reopenedPixels[i].a)
                    eyeReopenPixelDiff++;
            File.WriteAllBytes(Path.Combine(Output, "suiyue_eye_reopened_2560x1440.png"), reopened.EncodeToPNG());
            if (eyeReopenPixelDiff != 0)
                throw new InvalidOperationException("Reopened eye frame differs from neutral: " + eyeReopenPixelDiff);
            File.WriteAllText(Path.Combine(Output, "result.json"), JsonUtility.ToJson(new Result
            {
                passed = true, unity = Application.unityVersion, renderers = renderers.Length,
                parameters = ids.Length, visiblePixels = visible, clearPixels = clear,
                eyeClosedPixelDiff = eyeClosedPixelDiff, eyeReopenPixelDiff = eyeReopenPixelDiff, model = ModelPath,
                scope = "Custom Suiyue model and eye parameter response in isolated Unity Play; no VN integration"
            }, true));
            UnityEngine.Object.DestroyImmediate(closed);
            UnityEngine.Object.DestroyImmediate(reopened);
            UnityEngine.Object.DestroyImmediate(image);
            camera.targetTexture = null;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            Finish(true, null);
        }
        catch (Exception error) { Finish(false, error.ToString()); }
    }

    [Serializable]
    private sealed class Result
    {
        public bool passed;
        public string unity, scope, model;
        public int renderers, parameters, visiblePixels, clearPixels, eyeClosedPixelDiff, eyeReopenPixelDiff;
        public string error;
    }

    private static string EyeRendererOpacities(CubismModel model)
    {
        var type = typeof(Live2D.Cubism.Rendering.CubismRenderer);
        var opacity = type.GetProperty("Opacity", BindingFlags.Instance | BindingFlags.NonPublic);
        var displayed = type.GetProperty("DisplayedOpacity", BindingFlags.Instance | BindingFlags.NonPublic);
        return string.Join(",", model.Drawables
            .Where(drawable => drawable.Id == "eye_L_iris_complete" || drawable.Id == "eye_L_sclera_complete" ||
                               drawable.Id == "eye_R_iris_complete" || drawable.Id == "eye_R_sclera_complete")
            .Select(drawable =>
            {
                var renderer = drawable.GetComponent<Live2D.Cubism.Rendering.CubismRenderer>();
                return drawable.Id + ":" + opacity.GetValue(renderer) + "/" + displayed.GetValue(renderer);
            }));
    }

    private static void Finish(bool passed, string error)
    {
        var output = Output;
        SessionState.SetBool(Pending, false);
        EditorApplication.update -= CheckPlay;
        if (!passed)
        {
            File.WriteAllText(Path.Combine(output, "result.json"), JsonUtility.ToJson(new Result
            {
                passed = false, unity = Application.unityVersion,
                scope = "Custom Suiyue prototype in isolated Unity Play", model = ModelPath, error = error
            }, true));
        }
        SessionState.SetBool(Blink, false);
        SessionState.SetBool(Controller, false);
        Debug.Log(passed ? "SUIYUE_CUSTOM_PLAY_OK" : "SUIYUE_CUSTOM_PLAY_FAILED: " + error);
        EditorApplication.Exit(passed ? 0 : 1);
    }
}
