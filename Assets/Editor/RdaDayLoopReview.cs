using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.Apps;

/// <summary>RDA 图形化界面评审：一级/二级页面逐页截图 + 结束一天 + 日报。批处理：-executeMethod RdaDayLoopReview.Batch</summary>
[InitializeOnLoad]
public static class RdaDayLoopReview
{
    private const string PendingKey = "RdaDayLoopReview.Pending";
    private const string OutputDir = "tools/compile/rda_dayloop_review_v2";
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    // (一级, 二级, 文件名)；(-1,-1) = 结束一天并截日报
    private static readonly (int l1, int l2, string name)[] Pages =
    {
        (0, 0, "01-overview-route"), (0, 1, "02-overview-brief"),
        (1, 0, "03-dispatch-plans"), (1, 1, "04-dispatch-strategy"),
        (2, 0, "05-crew"), (3, 0, "06-orders"), (4, 0, "07-market"),
        (-1, -1, "08-day-report"),
        (5, 0, "09-finance-trend"), (5, 1, "10-finance-report"),
    };

    private static double deadline;
    private static int stage;
    private static int frame;
    private static int pageIdx;
    private static string pendingName;
    private static UIDocument document;
    private static VisualElement root;
    private static RenderTexture target;
    private static GameMainUI ui;
    private static int dayBefore;

    static RdaDayLoopReview()
    {
        if (SessionState.GetBool(PendingKey, false)) StartChecking();
    }

    private static void Assert(bool c, string m)
    {
        if (!c) throw new InvalidOperationException("[RdaDayLoopReview] Assertion failed: " + m);
    }

    private static object Field(string n) => typeof(GameMainUI).GetField(n, Hidden)?.GetValue(ui);
    private static object Call(string n, params object[] a) => typeof(GameMainUI).GetMethod(n, Hidden)?.Invoke(ui, a);

    private static void Resize(int w, int h)
    {
        if (target != null)
        {
            document.panelSettings.targetTexture = null;
            target.Release();
            UnityEngine.Object.Destroy(target);
        }
        target = new RenderTexture(w, h, 24);
        target.Create();
        document.panelSettings.targetTexture = target;
    }

    private static void Capture(string name)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = target;
        var img = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        img.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        img.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes(Path.Combine(OutputDir, name + ".png"), img.EncodeToPNG());
        UnityEngine.Object.Destroy(img);
    }

    private static void Click(VisualElement e)
    {
        Assert(e != null, "click target is null");
        if (e is Button b)
        {
            using var s = NavigationSubmitEvent.GetPooled();
            s.target = b;
            b.SendEvent(s);
        }
        else
        {
            using var c = ClickEvent.GetPooled();
            c.target = e;
            e.SendEvent(c);
        }
    }

    /// <summary>RDA 窗口 = 一级导航栏 → body → win。</summary>
    private static VisualElement RdaWin()
    {
        var nav = root.Q("rda-nav-0");
        Assert(nav != null, "RDA nav rail missing");
        return nav.parent.parent.parent;
    }

    [MenuItem("Tools/Tablet/Run RDA Day Loop Review")]
    public static void Batch()
    {
        Directory.CreateDirectory(OutputDir);
        SessionState.SetBool(PendingKey, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        StartChecking();
        EditorApplication.isPlaying = true;
    }

    private static void StartChecking()
    {
        stage = 0;
        frame = 0;
        pageIdx = 0;
        pendingName = null;
        deadline = EditorApplication.timeSinceStartup + 240.0;
        EditorApplication.update -= Check;
        EditorApplication.update += Check;
    }

    private static void Check()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("RDA review timed out");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (stage > 0 && Time.frameCount - frame < 30) return;

            switch (stage)
            {
                case 0:
                    GameMainUI.Show();
                    ui = GameMainUI.Instance;
                    Assert(ui != null, "tablet entry failed");
                    Call("EnsureDesktopBuiltSync");
                    document = (UIDocument)Field("uiDoc");
                    root = document.rootVisualElement;
                    var settings = UnityEngine.Object.Instantiate(document.panelSettings);
                    settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                    settings.scale = 1;
                    settings.clearColor = true;
                    settings.colorClearValue = new Color(0.06f, 0.07f, 0.06f);
                    document.panelSettings = settings;
                    Resize(1920, 1080);
                    Call("Unlock");
                    GameData.InitializeIfNeeded();
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 1:
                    Call("LaunchApp", "RDA");
                    dayBefore = GameData.GetDay();
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 2:
                    // 先截上一页，再切换到下一页
                    if (pendingName != null) { Capture(pendingName); pendingName = null; }
                    if (pageIdx >= Pages.Length) { stage++; break; }
                    var pg = Pages[pageIdx++];
                    if (pg.l1 < 0)
                    {
                        Click(root.Q<Button>("rda-end-day"));
                        Assert(GameData.GetDay() > dayBefore, "Day did not advance after End Day");
                        Assert(root.Q("rda-day-report") != null, "day report overlay missing");
                        pendingName = pg.name;
                        frame = Time.frameCount;
                        break;
                    }
                    if (root.Q("rda-day-report") != null) root.Q("rda-day-report").RemoveFromHierarchy();
                    TabletExploreAppUI.ActiveView.OpenRdaRequested(pg.l1, pg.l2);
                    pendingName = pg.name;
                    frame = Time.frameCount;
                    break;

                case 3:
                    var rep = DayLoopController.LastReport;
                    Assert(!rep.PoliticalLine.Contains("%") && !rep.InfrastructureLine.Contains("%"), "qualitative lines leaked numbers");
                    Debug.Log("RDA_DAYLOOP_REVIEW_OK: " + Pages.Length + " pages rendered, end-day advanced. Day=" + GameData.GetDay());
                    SessionState.SetBool(PendingKey, false);
                    EditorApplication.isPlaying = false;
                    EditorApplication.Exit(0);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            SessionState.SetBool(PendingKey, false);
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(1);
        }
    }
}
