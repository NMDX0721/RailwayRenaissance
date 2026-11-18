using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet;
using RailwayRenaissance.UI.Tablet.Paper;
using static RailwayRenaissance.UI.Tablet.Paper.PaperTokens;
using static RailwayRenaissance.UI.Tablet.Paper.PaperUI;

/// <summary>「站台纸本」样式画廊：一页展示 token / 组件 / 纸物样张，供风格确认。批处理：-executeMethod PaperUIGalleryReview.Batch</summary>
[InitializeOnLoad]
public static class PaperUIGalleryReview
{
    private const string PendingKey = "PaperUIGalleryReview.Pending";
    private const string OutputDir = "tools/compile/paper_gallery_v1";
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private static double deadline;
    private static int stage, frame;
    private static UIDocument document;
    private static VisualElement root;
    private static RenderTexture target;
    private static GameMainUI ui;

    static PaperUIGalleryReview()
    {
        if (SessionState.GetBool(PendingKey, false)) StartChecking();
    }

    private static object Field(string n) => typeof(GameMainUI).GetField(n, Hidden)?.GetValue(ui);
    private static object Call(string n, params object[] a) => typeof(GameMainUI).GetMethod(n, Hidden)?.Invoke(ui, a);

    private static void Resize(int w, int h)
    {
        if (target != null) { document.panelSettings.targetTexture = null; target.Release(); UnityEngine.Object.Destroy(target); }
        target = new RenderTexture(w, h, 24); target.Create();
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

    [MenuItem("Tools/Tablet/Run Paper UI Gallery Review")]
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
        stage = 0; frame = 0;
        deadline = EditorApplication.timeSinceStartup + 180.0;
        EditorApplication.update -= Check;
        EditorApplication.update += Check;
    }

    private static void Check()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("gallery review timed out");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (stage > 0 && Time.frameCount - frame < 30) return;
            switch (stage)
            {
                case 0:
                    GameMainUI.Show();
                    ui = GameMainUI.Instance;
                    Call("EnsureDesktopBuiltSync");
                    document = (UIDocument)Field("uiDoc");
                    root = document.rootVisualElement;
                    var settings = UnityEngine.Object.Instantiate(document.panelSettings);
                    settings.scaleMode = PanelScaleMode.ConstantPixelSize; settings.scale = 1;
                    settings.clearColor = true; settings.colorClearValue = new Color(0.06f, 0.07f, 0.06f);
                    document.panelSettings = settings;
                    Resize(1920, 1080);
                    root.Add(BuildGallery());
                    frame = Time.frameCount; stage++;
                    break;
                case 1:
                    Capture("01-gallery");
                    Debug.Log("PAPER_GALLERY_OK");
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

    // ================= 画廊内容 =================
    private static VisualElement Section(string title, float width)
    {
        var s = new VisualElement();
        s.style.width = width; s.style.marginRight = 20;
        var t = Text(title, FsSub, Ink, true); t.style.marginBottom = 8;
        s.Add(t);
        s.Add(RailDivider());
        return s;
    }

    private static VisualElement Row(float mb = 12f)
    {
        var r = new VisualElement();
        r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginTop = mb; r.style.flexWrap = Wrap.Wrap;
        return r;
    }

    private static VisualElement BuildGallery()
    {
        var page = new VisualElement();
        page.style.position = Position.Absolute;
        page.style.left = 0; page.style.right = 0; page.style.top = 0; page.style.bottom = 0;
        page.style.backgroundColor = new Color(0.90f, 0.86f, 0.77f);
        page.style.paddingLeft = 36; page.style.paddingRight = 16; page.style.paddingTop = 30;
        page.style.flexDirection = FlexDirection.Column;

        var head = Row(0f);
        head.Add(Text("站台纸本 · 设计系统", 30, Ink, true));
        var sub = Text("Station Paper — 暖纸底 · 深褐墨字 · 铁路琥珀 · 代码精绘纸物", FsBody, InkSoft); sub.style.marginLeft = 16;
        head.Add(sub);
        page.Add(head);

        var cols = new VisualElement(); cols.style.flexDirection = FlexDirection.Row; cols.style.marginTop = 14; cols.style.flexGrow = 1;
        page.Add(cols);

        // ---------- 列 1：Token ----------
        var c1 = Section("Tokens", 360); cols.Add(c1);
        var sw = Row(); c1.Add(sw);
        void Swatch(string n, Color c)
        {
            var w = new VisualElement(); w.style.width = 82; w.style.marginRight = 8; w.style.marginBottom = 10;
            var chip = new VisualElement(); chip.style.height = 40; chip.style.backgroundColor = c; Round(chip, RadiusS); Border(chip, 1f, Line);
            w.Add(chip); var l = Text(n, FsNote, InkSoft); l.style.marginTop = 3; w.Add(l);
            sw.Add(w);
        }
        Swatch("PaperBg", PaperBg); Swatch("PaperDeep", PaperDeep); Swatch("Card", PaperTokens.Card); Swatch("Ink", Ink);
        Swatch("Amber", Amber); Swatch("Good", Good); Swatch("Warn", Warn); Swatch("Bad", Bad);
        Swatch("调度", Dispatch); Swatch("人员", Crew); Swatch("订单", Orders); Swatch("市场", Market); Swatch("财务", Finance); Swatch("印章", PaperTokens.Stamp);

        var type = Panel(RadiusM, S3, S2, false); type.style.marginTop = 8; c1.Add(type);
        type.Add(Text("32  数字 50,000", FsNumber, Ink, true));
        type.Add(Text("22  标题 车站事务", FsTitle, Ink, true));
        type.Add(Text("18  小标题 调度 · 发车方案", FsSub, Ink, true));
        type.Add(Text("15  正文 减少一班次，优先稳住车况和口碑。", FsBody, Ink));
        type.Add(Text("13  辅助 较昨日持平 · 运力约 120 座", FsSmall, InkSoft));
        type.Add(Text("11  注脚 影响甚微", FsNote, InkMute));

        var radii = Row(14); c1.Add(radii);
        foreach (var r in new[] { RadiusS, RadiusM, RadiusL, RadiusXL })
        {
            var b = new VisualElement(); b.style.width = 64; b.style.height = 64; b.style.marginRight = 10;
            b.style.backgroundColor = PaperTokens.Card; Round(b, r); Border(b, 1.2f, Line);
            b.Add(Text("R" + (int)r, FsSmall, InkSoft)); b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
            radii.Add(b);
        }

        // ---------- 列 2：组件 ----------
        var c2 = Section("组件", 540); cols.Add(c2);
        var btns = Row(); c2.Add(btns);
        btns.Add(Button("回站休息", ButtonKind.Primary, null, TabletIconKind.Moon)); btns.Add(Spacer());
        btns.Add(Button("安排休息", ButtonKind.Secondary, null)); btns.Add(Spacer());
        btns.Add(Button("培训", ButtonKind.Ghost, null)); btns.Add(Spacer());
        btns.Add(Button("解雇", ButtonKind.Danger, null));
        var icons = Row(); c2.Add(icons);
        foreach (var k in new[] { TabletIconKind.Map, TabletIconKind.Gear, TabletIconKind.Home, TabletIconKind.Bolt })
        { icons.Add(IconButton(k, "", null)); icons.Add(Spacer()); }
        var segW = Row(); c2.Add(segW);
        segW.Add(Segmented(new[] { "发车方案", "经营策略" }, 0, null));
        var tags = Row(); c2.Add(tags);
        tags.Add(Tag("司机", Dispatch)); tags.Add(Spacer(6)); tags.Add(Tag("技师", Orders)); tags.Add(Spacer(6)); tags.Add(Tag("站务", Crew)); tags.Add(Spacer(6)); tags.Add(Tag("警告", Bad));

        var tabsPanel = Panel(RadiusL, S2, S2, false); tabsPanel.style.marginTop = 14; tabsPanel.style.flexDirection = FlexDirection.Row; c2.Add(tabsPanel);
        tabsPanel.Add(SideTab(TabletIconKind.Home, "总览", Amber, true, null));
        tabsPanel.Add(SideTab(TabletIconKind.Train, "调度", Dispatch, false, null));
        tabsPanel.Add(SideTab(TabletIconKind.Person, "人员", Crew, false, null));
        tabsPanel.Add(SideTab(TabletIconKind.Note, "订单", Orders, false, null));
        tabsPanel.Add(SideTab(TabletIconKind.Chart, "市场", Market, false, null));

        var dials = Row(14); c2.Add(dials);
        foreach (var (v, col, t) in new[] { (0.62f, Good, "62"), (0.55f, Warn, "55"), (0.18f, Bad, "18") })
        { dials.Add(Dial(v, col, t, 92)); dials.Add(Spacer(12)); }

        var gauges = Panel(RadiusM, S3, S2, false); gauges.style.marginTop = 10; c2.Add(gauges);
        gauges.Add(Text("疲劳", FsSmall, InkSoft)); gauges.Add(Gauge(0.3f, Good));
        gauges.Add(Text("忠诚", FsSmall, InkSoft)); gauges.Add(Gauge(0.72f, Amber));
        gauges.Add(Text("资金 +160", FsSmall, InkSoft)); gauges.Add(DeltaRuler(0.7f, Good, Bad));
        gauges.Add(Text("车况 -6", FsSmall, InkSoft)); gauges.Add(DeltaRuler(-0.4f, Good, Bad));
        gauges.Add(PerfDivider());

        // ---------- 列 3：纸物样张 ----------
        var c3 = Section("纸物样张", 800); cols.Add(c3);

        // 车票（订单）
        var ticket = Ticket(150, new Color(0.90f, 0.86f, 0.77f), Orders);
        ticket.style.marginTop = 12; ticket.style.flexDirection = FlexDirection.Row; ticket.style.minHeight = 112;
        var tl = new VisualElement(); tl.style.flexGrow = 1; tl.style.paddingRight = 20;
        var trow = Row(0f); trow.Add(Tag("货运", Orders)); var tt = Text("清溪茶叶外运", FsSub, Ink, true); tt.style.marginLeft = 10; trow.Add(tt); tl.Add(trow);
        var route = Text("雾峰村  ──●──  矿区 · 23 km", FsBody, InkSoft); route.style.marginTop = 8; tl.Add(route);
        tl.Add(Text("运费 3,200 · 须在 5 天内送达", FsSmall, InkSoft));
        var stub = new VisualElement(); stub.style.width = 130; stub.style.alignItems = Align.Center; stub.style.justifyContent = Justify.Center; stub.style.paddingLeft = 8;
        stub.Add(Text("剩余", FsSmall, InkSoft)); stub.Add(Text("5 天", FsNumber - 4, Orders, true));
        ticket.Add(tl); ticket.Add(stub); c3.Add(ticket);

        // 工牌（人员）
        var badge = Card(S3); badge.style.marginTop = 12; badge.style.flexDirection = FlexDirection.Row; badge.style.alignItems = Align.Center;
        var photo = new VisualElement(); photo.style.width = 64; photo.style.height = 76; photo.style.backgroundColor = Tint(Crew, 0.20f); Round(photo, RadiusS); Border(photo, 2f, Tint(Crew, 0.6f));
        photo.style.alignItems = Align.Center; photo.style.justifyContent = Justify.Center; photo.Add(Text("陈", 28, Crew, true));
        badge.Add(photo);
        var binfo = new VisualElement(); binfo.style.flexGrow = 1; binfo.style.marginLeft = 16;
        var brow = Row(0f); brow.Add(Text("老陈", FsSub, Ink, true)); var bt = Tag("司机", Dispatch); bt.style.marginLeft = 10; brow.Add(bt); binfo.Add(brow);
        binfo.Add(Text("老陈今天精神不错。", FsSmall, InkSoft)); binfo.Add(Gauge(0.2f, Good)); binfo.Add(Gauge(0.6f, Amber));
        badge.Add(binfo); c3.Add(badge);

        // 时刻表方案（选中）
        var plans = Row(12); plans.style.flexWrap = Wrap.NoWrap; c3.Add(plans);
        foreach (var (name, sel, pv) in new[] { ("保守运行", false, -0.2f), ("标准运行", true, 0.35f), ("加开一班", false, 0.8f) })
        {
            var pc = Card(S3, sel, Dispatch); pc.style.width = 250; pc.style.marginRight = 12;
            pc.Add(Text(name, FsSub, Ink, true));
            pc.Add(PerfDivider());
            pc.Add(Text("资金", FsSmall, InkSoft)); pc.Add(DeltaRuler(pv, Good, Bad));
            pc.Add(Text("口碑", FsSmall, InkSoft)); pc.Add(DeltaRuler(-pv / 2f, Good, Bad));
            plans.Add(pc);
        }

        // 公文 + 印章 / 账页
        var docRow = Row(12); docRow.style.flexWrap = Wrap.NoWrap; docRow.style.alignItems = Align.FlexStart; c3.Add(docRow);
        var doc = Card(S4); doc.style.width = 390; doc.style.height = 200; doc.style.marginRight = 12;
        doc.Add(Text("第 1 天 · 日报", FsTitle, Ink, true)); doc.Add(RailDivider());
        doc.Add(Text("民情   乡里反映渐好", FsBody, Ink)); doc.Add(Text("财务   账面有所盈余", FsBody, Ink)); doc.Add(Text("上级   常态监管", FsBody, Ink));
        var st = Stamp("已阅", 92); st.style.position = Position.Absolute; st.style.right = 16; st.style.bottom = 12; doc.Add(st);
        docRow.Add(doc);
        var ledger = Card(S3); ledger.style.width = 390; ledger.style.height = 200;
        ledger.generateVisualContent += ctx => PaperDraw.LedgerGrid(ctx.painter2D, ledger.layout.width, ledger.layout.height, 30f, 40f);
        ledger.style.paddingLeft = 48;
        ledger.Add(Text("财务 · 账页", FsSub, Ink, true));
        ledger.Add(Text("11-10   运费收入      +3,200", FsBody, Good)); ledger.Add(Text("11-10   燃油 / 工资    -1,480", FsBody, Bad)); ledger.Add(Text("11-11   维护费用       -  600", FsBody, Bad));
        docRow.Add(ledger);

        return page;
    }

    private static VisualElement Spacer(float w = 10f)
    {
        var s = new VisualElement(); s.style.width = w; s.style.height = 1; return s;
    }
}
