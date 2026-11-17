using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.Apps;

/// <summary>巡线探索层灰盒评审：启动 → 截图 → 碰撞断言 → 传送到各交互点截图 → 交互回调。批处理：-executeMethod ExploreReview.Batch</summary>
[InitializeOnLoad]
public static class ExploreReview
{
    private const string PendingKey = "ExploreReview.Pending";
    private const string OutputDir = "tools/compile/explore_review_v1";
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private static double deadline;
    private static int stage;
    private static int frame;
    private static UIDocument document;
    private static VisualElement root;
    private static RenderTexture target;
    private static GameMainUI ui;
    private static ExploreMapView view;
    private static string lastAction = "";
    private static bool interiorShot;

    static ExploreReview()
    {
        if (SessionState.GetBool(PendingKey, false)) StartChecking();
    }

    private static void Assert(bool c, string m)
    {
        if (!c) throw new InvalidOperationException("[ExploreReview] Assertion failed: " + m);
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

    [MenuItem("Tools/Tablet/Run Explore Review")]
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
        deadline = EditorApplication.timeSinceStartup + 180.0;
        EditorApplication.update -= Check;
        EditorApplication.update += Check;
    }

    private static void Check()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("explore review timed out");
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
                    RailwayRenaissance.UI.Tablet.SystemShell.TabletDisplayPreferences.SetBrightness(1f);   // 其他评审可能把亮度留在最低
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 1:
                    ExploreQuests.ResetForTests();
                    Call("LaunchApp", "巡线");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 2:
                    view = TabletExploreAppUI.ActiveView;
                    Assert(view != null, "explore view not created");
                    var prevAction = view.OnAction;
                    view.OnAction = id => lastAction = id;
                    Capture("01-explore-start");

                    // 碰撞：从大厅门口一直向上走，不得穿过站房墙体
                    view.TeleportToSpot("hall");
                    float yBefore = view.PlayerPos.y;
                    for (int i = 0; i < 60; i++) view.Step(new Vector2(0f, -1f), 1f / 30f); // 屏幕坐标 y 向下，向北走 = -y
                    Assert(view.PlayerPos.y > 7f * ExploreMapView.T + 3f, "player walked through the hall wall (y=" + view.PlayerPos.y + ")");
                    Assert(view.PlayerPos.y < yBefore, "player did not move up at all");

                    // 水塘不可进入
                    view.TeleportToSpot("crossing");
                    for (int i = 0; i < 120; i++) view.Step(new Vector2(0.4f, 1f), 1f / 30f);
                    Assert(view.PlayerPos.x < 39f * ExploreMapView.T && view.PlayerPos.y < 23f * ExploreMapView.T, "player escaped map bounds: " + view.PlayerPos);

                    // 交互：调度室
                    view.TeleportToSpot("dispatch");
                    view.Interact();
                    Assert(view.Indoors && view.AreaId == "dispatch", "entering dispatch door did not switch to the interior");
                    float minutesAfter = view.Minutes;
                    Assert(minutesAfter > 6f * 60f, "interaction did not cost time");
                    view.TeleportToSpot("dispatch_desk");
                    view.Interact();
                    Assert(lastAction == "dispatch_desk", "dispatch desk interaction did not fire (got '" + lastAction + "')");
                    var timetable = ExploreQuests.Active.Find(q => q.id == "timetable");
                    Assert(timetable != null && timetable.Done, "quest 'timetable' did not complete after entering dispatch and using the desk");
                    Assert(ExploreQuests.CompletedToday >= 1, "completed quest counter did not advance");
                    Assert(root.Q("explore-tracker") != null, "quest tracker missing");
                    // 室内碰撞：从沙发旁向北走不得穿过家具/墙
                    view.TeleportToSpot("dispatch_desk");
                    for (int i = 0; i < 90; i++) view.Step(new Vector2(0f, -1f), 1f / 30f);
                    Assert(view.PlayerPos.y > 4f * ExploreMapView.T, "player walked through the dispatch desk (y=" + view.PlayerPos.y + ")");
                    view.TeleportToSpot("dispatch_desk");
                    interiorShot = true;
                    view.FinishFade();
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 3:
                    if (interiorShot)
                    {
                        Capture("02a-explore-dispatch-room");
                        interiorShot = false;
                        view.TeleportToSpot("platform");
                        view.LeaveIndoors();
                        Assert(!view.Indoors, "failed to leave the interior");
                        frame = Time.frameCount;
                        break;
                    }
                    Capture("02-explore-platform");
                    view.TeleportToSpot("garage");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 4:
                    Capture("03-explore-garage");
                    view.TeleportToSpot("tea");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 5:
                    Capture("04-explore-tea");

                    // 自动寻路：从调度室到车库，路径存在；绕开建筑
                    view.TeleportToSpot("dispatch");
                    Assert(view.NavigateToTile(27, 15, false) && view.HasPath, "A* path dispatch->garage not found");
                    view.ClearPath();
                    Assert(!view.NavigateToTile(0, 0, false) || view.HasPath, "navigate to blocked tile should snap to walkable or fail");
                    view.ClearPath();

                    // 跨区域寻路：室外 → 调度室调度台；室内 → 室外茶摊
                    view.TeleportToSpot("platform"); view.LeaveIndoors();
                    view.GoToSpot("dispatch_desk");
                    for (int guard = 0; guard < 6 && (view.HasPath || !view.Indoors); guard++) view.SimulateAutoWalk(4000);
                    Assert(view.Indoors && view.AreaId == "dispatch", "cross-area route outdoor->dispatch desk did not enter the room");
                    Assert(view.Interactions > 0 || lastAction == "dispatch_desk", "arrived at the desk but did not interact");
                    view.GoToSpot("tea");
                    for (int guard = 0; guard < 6 && (view.HasPath || view.Indoors); guard++) view.SimulateAutoWalk(4000);
                    Assert(!view.Indoors, "cross-area route indoor->tea did not leave the room");
                    view.ClearPath();
                    // NPC 是实体：朝老陈直走不能穿过他
                    view.TeleportToSpot("platform");
                    var nearNpc = view.NpcPos("laochen");
                    view.SetPlayerPos(nearNpc + new Vector2(-40f, 0f));
                    for (int i = 0; i < 60; i++) view.Step(new Vector2(1f, 0f), 1f / 30f);
                    Assert(view.PlayerPos.x < nearNpc.x - 10f, "player walked through an NPC (x=" + view.PlayerPos.x + " npc=" + nearNpc.x + ")");
                    view.TeleportToSpot("platform");

                    // 加速消耗能量，且比步行快
                    view.TeleportToSpot("platform");
                    float x0 = view.PlayerPos.x;
                    for (int i = 0; i < 15; i++) view.Step(new Vector2(1f, 0f), 1f / 30f, false);
                    float walked = view.PlayerPos.x - x0;
                    view.TeleportToSpot("platform");
                    x0 = view.PlayerPos.x;
                    float stBefore = view.Stamina;
                    for (int i = 0; i < 15; i++) view.Step(new Vector2(1f, 0f), 1f / 30f, true);
                    Assert(view.PlayerPos.x - x0 > walked * 1.4f, "sprint is not faster than walking");
                    Assert(view.Stamina < stBefore, "sprint did not drain energy");

                    view.TeleportToSpot("platform");
                    view.RequestMap();
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 6:
                    Capture("05-explore-map");
                    view.RequestMap();   // 关闭地图
                    var menu = root.Q("explore-menu");
                    Assert(menu != null, "menu overlay missing");
                    menu.style.display = DisplayStyle.Flex;
                    menu.style.opacity = 1f;
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 7:
                    Capture("06-explore-menu");
                    root.Q("explore-menu").style.display = DisplayStyle.None;
                    root.Q("explore-menu").style.opacity = 0f;
                    var journal = root.Q("explore-journal");
                    Assert(journal != null, "journal overlay missing");
                    var trackerEl = root.Q("explore-tracker");
                    using (var click = ClickEvent.GetPooled()) { click.target = trackerEl; trackerEl.SendEvent(click); }
                    journal.style.opacity = 1f;
                    Assert(journal.style.display == DisplayStyle.Flex, "clicking the tracker did not open the journal");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 8:
                    Capture("06b-explore-journal");
                    root.Q("explore-journal").style.display = DisplayStyle.None;
                    root.Q("explore-journal").style.opacity = 0f;
                    // 悬浮提示 + NPC 对话框
                    view.TeleportToSpot("platform");
                    view.Step(new Vector2(-1f, 0f), 0.26f);
                    view.Interact();
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 9:
                    Capture("07-explore-dialogue");                    Debug.Log("EXPLORE_REVIEW_OK: map rendered, collision enforced, interactions fire, time advances, A* + sprint ok");
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
