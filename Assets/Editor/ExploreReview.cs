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
    private static bool interiorShot, hallShot;

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
                    GameMainUI.OpenTabletApp("RDA");
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
                    view.TeleportToSpot("garage");
                    float yBefore = view.PlayerPos.y;
                    for (int i = 0; i < 60; i++) view.Step(new Vector2(0f, -1f), 1f / 30f); // 屏幕坐标 y 向下，向北走 = -y
                    Assert(view.PlayerPos.y > 18f * ExploreMapView.T + 3f, "player walked through the garage wall (y=" + view.PlayerPos.y + ")");
                    Assert(view.PlayerPos.y < yBefore, "player did not move up at all");

                    // 水塘不可进入
                    view.SetPlayerPos(new Vector2(35.5f, 17.4f) * ExploreMapView.T);
                    for (int i = 0; i < 30; i++) view.Step(Vector2.down, 1f / 30f);
                    Assert(view.PlayerPos.y < 18f * ExploreMapView.T, "player walked into the pond: " + view.PlayerPos);

                    view.TeleportToSpot("east_crossing");
                    for (int i = 0; i < 120; i++) view.Step(Vector2.right, 1f / 30f);
                    Assert(view.PlayerPos.x < (ExploreMapView.W - 1) * ExploreMapView.T, "player escaped the east map boundary: " + view.PlayerPos);

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
                        // 站务大厅：进门 → 公告板 → 截图
                        view.TeleportToSpot("hall");
                        view.Interact();
                        Assert(view.Indoors && view.AreaId == "hall", "entering the hall door did not switch to the interior");
                        view.TeleportToSpot("hall_board");
                        view.Interact();
                        Assert(lastAction == "hall_board", "hall board interaction did not fire (got '" + lastAction + "')");
                        for (int i = 0; i < 90; i++) view.Step(new Vector2(0f, -1f), 1f / 30f);
                        Assert(view.PlayerPos.y > 3f * ExploreMapView.T, "player walked through the hall notice board (y=" + view.PlayerPos.y + ")");
                        view.TeleportToSpot("ticket_window");
                        hallShot = true;
                        view.FinishFade();
                        frame = Time.frameCount;
                        break;
                    }
                    if (hallShot)
                    {
                        Capture("02b-explore-hall");
                        hallShot = false;
                        var hallQuest = ExploreQuests.Active.Find(q => q.id == "hallboard");
                        Assert(hallQuest != null && hallQuest.Done, "quest 'hallboard' did not complete");
                        view.TeleportToSpot("platform");
                        view.LeaveIndoors();
                        Assert(!view.Indoors, "failed to leave the hall");
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
                    Capture("07-explore-dialogue");
                    view.TeleportToSpot("rail_shop");
                    bool sawShop = false;
                    var previousDialogue = view.OnDialogue;
                    view.OnDialogue = (who, line, color) =>
                    {
                        sawShop = who == "铁路商店";
                        previousDialogue?.Invoke(who, line, color);
                    };
                    view.Interact();
                    view.OnDialogue = previousDialogue;
                    Assert(sawShop, "railway shop did not open its counter dialogue");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 10:
                    Capture("08-explore-rail-shop");
                    root.Q("explore-dialogue").style.opacity = 0f;
                    root.Q("explore-toasts").Clear();
                    Assert(view.NavigateToTile(50, 13, false) && view.HasPath, "route from station to east crossing missing");
                    view.SimulateAutoWalk(4000);
                    Assert(Mathf.FloorToInt(view.PlayerPos.x / ExploreMapView.T) == 50, "player did not reach the east crossing");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 11:
                    Capture("09-explore-east-crossing");
                    // 探索点与状态件：信号机、道口栏杆、任务「闲不住的站长」
                    view.TeleportToSpot("signal_lever"); view.Interact();
                    Assert(ExploreFixtures.Get("signal_station").target > 0.5f, "signal lever did not set the semaphore to green");
                    view.TeleportToSpot("east_crossing"); view.Interact();
                    Assert(ExploreFixtures.Get("crossing_bar_n").target > 0.5f, "crossing bars did not rise");
                    {
                        Quest tinker = null;
                        foreach (var q in ExploreQuests.Active) if (q.id == "tinker") tinker = q;
                        Assert(tinker != null && tinker.Done, "quest 'tinker' not completed by signal lever + crossing");
                    }
                    // 门：靠近站务大厅门口自动打开（占位门洞）
                    view.TeleportToSpot("hall");
                    ExploreFixtures.Tick(2f, view.PlayerPos, ExploreMapView.T);
                    Assert(ExploreFixtures.Get("door_hall").t > 0.95f, "hall door did not open when the player stood at it");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 12:
                    Capture("10-explore-door-open");
                    // 世界状态：老化 2 档、政治 2 档、沙能渗透 0.5、傍晚 20:00、一架飞行器
                    GameData.DecayGrade = 2; GameData.PoliticalTier = 2;
                    SandRivalManager.SetPenetration("wufeng", 0.5f);
                    view.SetMinutesForTests(20f * 60f);
                    ExploreFlyers.Clear(); ExploreFlyers.Spawn(ExploreMapView.W * ExploreMapView.T, ExploreMapView.H * ExploreMapView.T);
                    view.TeleportToSpot("platform");
                    {
                        var fl = ExploreFlyers.Active[0];
                        fl.pos = view.PlayerPos + new Vector2(40f, 30f);   // 直接放进视口，评审里看影子
                        ExploreFlyers.Active[0] = fl;
                    }
                    ExploreFixtures.Tick(0.1f, view.PlayerPos, ExploreMapView.T);
                    // 沙能贴花会占格：占用格不可通行
                    Assert(ExploreFixtures.DecalSolid(33, 14) && ExploreFixtures.DecalSolid(30, 2), "decal footprints not solid at penetration 0.5");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 13:
                    Capture("11-explore-night-state");
                    view.SetMinutesForTests(6.5f * 60f);
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 14:
                    Capture("12-explore-morning-fog");
                    GameData.DecayGrade = 0; GameData.PoliticalTier = 0;
                    ExploreFixtures.ResetAll(); ExploreFlyers.Clear();
                    var tick = typeof(ExploreMapView).GetMethod("Tick", Hidden);
                    Assert(tick != null, "world tick missing");
                    view.TeleportToSpot("ticket_window");
                    view.SetMinutesForTests(525f);
                    var savedPosition = view.PlayerPos;
                    float savedStamina = view.Stamina;
                    view.OpenRdaRequested?.Invoke(0, 0);
                    view.JoyDir = Vector2.right;
                    tick.Invoke(view, null);
                    Assert(view.PlayerPos == savedPosition && view.Minutes == 525f && view.JoyDir == Vector2.zero,
                        "world input remained live under the management panel");

                    Call("ReturnToTabletHome");
                    view.JoyDir = Vector2.right;
                    tick.Invoke(view, null);
                    Assert(view.PlayerPos == savedPosition && view.Minutes == 525f && view.JoyDir == Vector2.zero,
                        "world input remained live in the background");
                    GameMainUI.OpenTabletApp("巡线");
                    Assert(TabletExploreAppUI.ActiveView == view, "RDA alias rebuilt the active map");

                    Call("ReturnToTabletHome");
                    Call("ReclaimInactiveTabletTasks", true);
                    GameMainUI.OpenTabletApp("RDA");
                    var restored = TabletExploreAppUI.ActiveView;
                    Assert(restored != null && restored != view && restored.Indoors && restored.PlayerPos == savedPosition &&
                        restored.Minutes == 525f && restored.Stamina == savedStamina, "reclaimed map did not restore session state");
                    Debug.Log("EXPLORE_REVIEW_OK: map, collisions, shop, crossing, fixtures, quests, time, A*, sprint, input gating and task restoration");
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
