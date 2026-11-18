using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>平板系统自测包：输出到 tools/compile 下的独立目录，不碰 发行/ 和 中转站/。</summary>
public static class TabletPlaytestBuild
{
    public static void Batch()
    {
        string[] scenes = Array.ConvertAll(Array.FindAll(EditorBuildSettings.scenes, s => s.enabled), s => s.path);
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "tools/compile/tablet_playtest_20261107/RailwayRenaissance.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Tablet playtest build failed: " + report.summary.result);
        Debug.Log("TABLET_PLAYTEST_BUILD_OK");
    }
}
