using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class NeteaseMusicReviewBuild
{
    public static void Batch()
    {
        var oldCompany = PlayerSettings.companyName;
        var oldProduct = PlayerSettings.productName;
        var oldResizable = PlayerSettings.resizableWindow;
        try
        {
            PlayerSettings.companyName = "RailwayRenaissanceReview";
            PlayerSettings.productName = "NeteaseMusicReview";
            PlayerSettings.resizableWindow = true; // lets windowed test builds be maximized
            var scenes = Array.ConvertAll(Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled), scene => scene.path);
            var options = new BuildPlayerOptions { scenes = scenes, locationPathName = "tools/compile/netease_music_review_v29/RailwayMusicReview.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.None, extraScriptingDefines = new[] { "MUSIC_RUNTIME_REVIEW" } };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Review build failed: " + report.summary.result);
            Debug.Log("NETEASE_REVIEW_BUILD_OK");
        }
        finally { PlayerSettings.companyName = oldCompany; PlayerSettings.productName = oldProduct; PlayerSettings.resizableWindow = oldResizable; }
    }
}

