using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ProloguePlaytestBuild
{
    public static void Batch()
    {
        string company = PlayerSettings.companyName, product = PlayerSettings.productName;
        try
        {
            PlayerSettings.companyName = "RailwayRenaissancePlaytest";
            PlayerSettings.productName = "PrologueFinalPlaytest";
            string[] scenes = Array.ConvertAll(Array.FindAll(EditorBuildSettings.scenes, s => s.enabled), s => s.path);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "tools/compile/prologue_final_playtest_20261105/PrologueFinalPlaytest.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Prologue playtest build failed: " + report.summary.result);
            Debug.Log("PROLOGUE_PLAYTEST_BUILD_OK");
        }
        finally { PlayerSettings.companyName = company; PlayerSettings.productName = product; }
    }
}
