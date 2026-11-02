using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class MusicPlayerReviewBuild
{
    public static void Batch()
    {
        string company = PlayerSettings.companyName, product = PlayerSettings.productName;
        try
        {
            PlayerSettings.companyName = "RailwayRenaissanceReview";
            PlayerSettings.productName = "MusicTerminalReview20261028";
            string[] scenes = Array.ConvertAll(Array.FindAll(EditorBuildSettings.scenes, s => s.enabled), s => s.path);
            var options = new BuildPlayerOptions { scenes = scenes, locationPathName = "tools/compile/music_player_isolated_review_v5/MusicPlayerReview.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Music Player review build failed: " + report.summary.result);
            Debug.Log("MUSIC_PLAYER_ISOLATED_BUILD_OK");
        }
        finally { PlayerSettings.companyName = company; PlayerSettings.productName = product; }
    }
}
