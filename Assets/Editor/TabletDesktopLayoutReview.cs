using System.Collections.Generic;
using RailwayRenaissance.UI.Tablet.SystemShell;
using UnityEditor;
using UnityEngine;

/// <summary>桌面排布模型审查：放进文件夹、互换、跨页、拿出文件夹，并确认能保存、重置。</summary>
public static class TabletDesktopLayoutReview
{
    private static readonly string[] Default = { "相册", "音乐", "驾驶台", "媒姬百科", "米家", "地图", "铁路", "浏览器", "安全中心", "设置", "文件管理", "应用商店" };
    private static readonly string[] All = {
        "RDA", "归途", "媒姬百科", "相册", "音乐", "铁路", "驾驶台", "笔记", "设置", "浏览器", "安全中心", "米家",
        "地图", "应用商店", "阿里郎商店", "未来网", "白头疫苗", "时钟", "日历", "天气", "计算器", "文件管理" };

    public static void Batch()
    {
        int code = 0;
        try { Run(); Debug.Log("TabletDesktopLayoutReview PASS"); }
        catch (System.Exception e) { Debug.LogError("TabletDesktopLayoutReview FAIL: " + e.Message); code = 1; }
        EditorApplication.Exit(code);
    }

    private static void Check(bool ok, string msg) { if (!ok) throw new System.Exception(msg); }

    private static void Run()
    {
        PlayerPrefs.DeleteKey("TabletDesktopLayout_v1");
        TabletDesktopLayout.Configure(Default, All);
        TabletDesktopLayout.EnsureLoaded();
        Check(TabletDesktopLayout.Page0.Count == 12 && TabletDesktopLayout.Page0.Contains("铁路"), "default page0");
        Check(TabletDesktopLayout.Folder.Count == 4, "default folder");

        TabletDesktopLayout.MoveToFolder("相册");
        Check(TabletDesktopLayout.Folder.Contains("相册") && !TabletDesktopLayout.Page0.Contains("相册"), "move to folder");

        TabletDesktopLayout.MoveToPage0("音乐", 1 + 1);   // 与「驾驶台」互换
        Check(TabletDesktopLayout.Page0[2] == "音乐" && !TabletDesktopLayout.Page0.Contains("相册"), "swap in page0");

        TabletDesktopLayout.MoveToPage1("浏览器", 0);
        Check(TabletDesktopLayout.Page1[0] == "浏览器" && !TabletDesktopLayout.Page0.Contains("浏览器"), "to page1");

        TabletDesktopLayout.TakeOutOfFolder("相册");
        Check(!TabletDesktopLayout.Folder.Contains("相册") && TabletDesktopLayout.Page0.Contains("相册"), "take out of folder");

        // 保存后重新读取应当一致
        var before = new List<string>(TabletDesktopLayout.Page0);
        TabletDesktopLayout.Configure(Default, All);
        TabletDesktopLayout.EnsureLoaded();
        Check(before.Count == TabletDesktopLayout.Page0.Count, "reload count");
        for (int i = 0; i < before.Count; i++) Check(before[i] == TabletDesktopLayout.Page0[i], "reload slot " + i);

        TabletDesktopLayout.ResetToDefault();
        Check(TabletDesktopLayout.Page0[0] == "相册" && TabletDesktopLayout.Folder.Count == 4, "reset");
        PlayerPrefs.DeleteKey("TabletDesktopLayout_v1");
    }
}
