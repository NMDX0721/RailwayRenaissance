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
        try
        {
            PlayerSettings.companyName = "RailwayRenaissanceReview";
            PlayerSettings.productName = "NeteaseMusicReview";
            var scenes = Array.ConvertAll(Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled), scene => scene.path);
            var options = new BuildPlayerOptions { scenes = scenes, locationPathName = "tools/compile/netease_music_review_v10/RailwayMusicReview.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Review build failed: " + report.summary.result);
            Debug.Log("NETEASE_REVIEW_BUILD_OK");
        }
        finally { PlayerSettings.companyName = oldCompany; PlayerSettings.productName = oldProduct; }
    }
}

