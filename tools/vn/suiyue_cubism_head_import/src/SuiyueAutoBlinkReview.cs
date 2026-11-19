using System;
using System.IO;
using System.Linq;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SuiyueAutoBlinkReview
{
    private const string Pending = "SuiyueAutoBlinkReview.Pending";
    private const string Prefab = "Assets/Review/SuiyueBlink/suiyue_head_blink_prototype.prefab";
    private const string Output = "D:/Unity Project/RailwayRenaissance/tools/vn/suiyue_cubism_head_import/unity_auto_blink_acceptance/result.json";
    private static double deadline;
    private static double pausedAt;
    private static bool closed, reopened, resumed;
    private static int closedFrame, reopenedFrame, resumedFrame;

    static SuiyueAutoBlinkReview()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        deadline = EditorApplication.timeSinceStartup + 120;
        EditorApplication.update += CheckPlay;
    }

    public static void Batch()
    {
        if (!Application.dataPath.Replace('\\', '/').EndsWith("/VNLive2DReviewProject/Assets"))
            throw new InvalidOperationException("Isolated Live2D review project required.");
        Directory.CreateDirectory(Path.GetDirectoryName(Output));
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        if (prefab == null) throw new InvalidOperationException("Suiyue blink prefab missing.");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        root.name = "SUIYUE_AUTO_BLINK_REVIEW";
        foreach (var animator in root.GetComponentsInChildren<Animator>()) animator.enabled = false;
        var model = root.GetComponent<CubismModel>();
        foreach (var id in new[] { "ParamEyeLOpen", "ParamEyeROpen" })
            model.Parameters.First(parameter => parameter.Id == id).gameObject.AddComponent<CubismEyeBlinkParameter>();
        var controller = root.AddComponent<CubismEyeBlinkController>();
        controller.BlendMode = CubismParameterBlendMode.Override;
        controller.Refresh();
        var loop = root.AddComponent<SuiyueBlinkLoop>();
        loop.Controller = controller;
        loop.IntervalSeconds = 1.2f;
        loop.BlinkDurationSeconds = 0.6f;
        root.GetComponent<CubismUpdateController>().Refresh();
        EditorSceneManager.SaveScene(scene, "Assets/Review/SuiyueAutoBlinkReview.unity");
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    private static void CheckPlay()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Auto blink review timed out.");
            var loop = UnityEngine.Object.FindAnyObjectByType<SuiyueBlinkLoop>();
            if (loop == null) return;
            var opening = loop.CurrentOpening;
            if (!closed && opening <= 0.01f)
            {
                closed = true;
                closedFrame = Time.frameCount;
            }
            else if (closed && !reopened && opening >= 0.99f && Time.frameCount > closedFrame + 2)
            {
                reopened = true;
                reopenedFrame = Time.frameCount;
                loop.SetPaused(true);
                pausedAt = EditorApplication.timeSinceStartup;
            }
            else if (reopened && !resumed && EditorApplication.timeSinceStartup - pausedAt >= 0.5)
            {
                if (!loop.IsPaused || loop.CurrentOpening < 0.99f)
                    throw new InvalidOperationException("Pause did not hold eyes open.");
                loop.SetPaused(false);
                resumed = true;
                resumedFrame = Time.frameCount;
            }
            else if (resumed && opening <= 0.01f && Time.frameCount > resumedFrame + 2)
            {
                File.WriteAllText(Output, JsonUtility.ToJson(new Result
                {
                    passed = true, closedFrame = closedFrame, reopenedFrame = reopenedFrame,
                    resumedFrame = resumedFrame, secondClosedFrame = Time.frameCount,
                    scope = "Timed CubismEyeBlinkController loop and pause/resume in isolated Unity Play"
                }, true));
                Finish(true, null);
            }
        }
        catch (Exception error) { Finish(false, error.ToString()); }
    }

    [Serializable]
    private sealed class Result
    {
        public bool passed;
        public int closedFrame, reopenedFrame, resumedFrame, secondClosedFrame;
        public string scope, error;
    }

    private static void Finish(bool passed, string error)
    {
        SessionState.SetBool(Pending, false);
        EditorApplication.update -= CheckPlay;
        if (!passed) File.WriteAllText(Output, JsonUtility.ToJson(new Result { passed = false, error = error }, true));
        Debug.Log(passed ? "SUIYUE_AUTO_BLINK_OK" : "SUIYUE_AUTO_BLINK_FAILED: " + error);
        EditorApplication.Exit(passed ? 0 : 1);
    }
}
