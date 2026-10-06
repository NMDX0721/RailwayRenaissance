using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class VNBacklog : MonoBehaviour
{
    private UIDocument uiDoc;
    private Font gameFont;

    private VisualElement backlogPanel;
    private VisualElement backlogHeader;
    private ScrollView backlogScroll;
    private bool isOpen;

    private const int MaxEntries = 500;
    private readonly List<BacklogEntry> entries = new List<BacklogEntry>();

    private struct BacklogEntry
    {
        public string speaker;
        public string text;
        public int sceneIndex;
        public int dialogueIndex;
    }

    public Action<int, int> OnEntryClicked;

    public void Init(UIDocument document)
    {
        uiDoc = document;
        gameFont = Resources.Load<Font>("Fonts/zpix");
        BuildUI();
    }

    private FontDefinition GetFontDef()
    {
        return new FontDefinition { font = gameFont };
    }

    private void BuildUI()
    {
        var root = uiDoc.rootVisualElement;
        var fontDef = GetFontDef();

        backlogPanel = new VisualElement { name = "backlog-panel" };
        backlogPanel.style.position = Position.Absolute;
        backlogPanel.style.top = 0;
        backlogPanel.style.left = 0;
        backlogPanel.style.right = 0;
        backlogPanel.style.bottom = 0;
        backlogPanel.style.backgroundColor = new Color(0.05f, 0.03f, 0.02f, 0.85f);
        // 磨砂：半透明底 + USS backdrop-filter（引擎不支持该属性时静默忽略）
        var frosted = Resources.Load<StyleSheet>("UI/frosted");
        if (frosted != null)
        {
            backlogPanel.styleSheets.Add(frosted);
            backlogPanel.AddToClassList("frosted");
        }
        backlogPanel.style.display = DisplayStyle.None;
        backlogPanel.pickingMode = PickingMode.Position;
        // 面板内任意点击都不冒泡到 VN root（避免关历史/点条目时推进对话）
        backlogPanel.RegisterCallback<PointerDownEvent>(evt =>
        {
            evt.StopImmediatePropagation();
            VNManager.Instance?.SuppressNextAdvance();
        });
        backlogPanel.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopImmediatePropagation();
            VNManager.Instance?.SuppressNextAdvance();
        });
        root.Add(backlogPanel);

        backlogHeader = new VisualElement { name = "backlog-header" };
        backlogHeader.style.flexDirection = FlexDirection.Row;
        backlogHeader.style.justifyContent = Justify.SpaceBetween;
        backlogHeader.style.alignItems = Align.Center;
        backlogHeader.style.paddingLeft = 40;
        backlogHeader.style.paddingRight = 40;
        backlogHeader.style.paddingTop = 20;
        backlogHeader.style.paddingBottom = 20;
        backlogHeader.style.borderBottomWidth = 2;
        backlogHeader.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        backlogPanel.Add(backlogHeader);

        var titleLabel = new Label("对话回顾");
        titleLabel.style.fontSize = 32;
        titleLabel.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.unityFontDefinition = fontDef;
        backlogHeader.Add(titleLabel);

        var closeBtn = new UnityEngine.UIElements.Button(() => ToggleBacklog()) { text = "✕" };
        closeBtn.focusable = false;
        UiCursor.Bind(closeBtn);
        // 阻止冒泡到 VN root：否则关闭瞬间会被当成「推进下一句」
        // 并通知 VNManager 吞掉同一次点击（IsOpen 已 false 后仍可能 Bubble 到 OnRootClick）
        closeBtn.RegisterCallback<PointerDownEvent>(evt =>
        {
            evt.StopImmediatePropagation();
            VNManager.Instance?.SuppressNextAdvance();
        });
        closeBtn.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopImmediatePropagation();
            VNManager.Instance?.SuppressNextAdvance();
        });
        closeBtn.RegisterCallback<PointerUpEvent>(evt => evt.StopImmediatePropagation());
        // 40×40 金边圆角关闭按钮（深色底 + 金色 ✕）
        var closeBg = new Color(0.14f, 0.09f, 0.05f, 0.9f);
        var closeBorder = new Color(0.82f, 0.62f, 0.35f, 0.7f);
        closeBtn.style.width = 40;
        closeBtn.style.height = 40;
        closeBtn.style.borderTopLeftRadius = 8;
        closeBtn.style.borderTopRightRadius = 8;
        closeBtn.style.borderBottomLeftRadius = 8;
        closeBtn.style.borderBottomRightRadius = 8;
        closeBtn.style.backgroundColor = closeBg;
        closeBtn.style.borderTopWidth = 1;
        closeBtn.style.borderBottomWidth = 1;
        closeBtn.style.borderLeftWidth = 1;
        closeBtn.style.borderRightWidth = 1;
        closeBtn.style.borderTopColor = closeBorder;
        closeBtn.style.borderBottomColor = closeBorder;
        closeBtn.style.borderLeftColor = closeBorder;
        closeBtn.style.borderRightColor = closeBorder;
        closeBtn.style.fontSize = 20;
        closeBtn.style.color = new Color(1f, 0.86f, 0.59f, 1f);
        closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        closeBtn.style.unityFontDefinition = fontDef;
        // hover：提亮背景与金边，移出恢复
        closeBtn.RegisterCallback<PointerEnterEvent>(evt =>
        {
            closeBtn.style.backgroundColor = new Color(0.3f, 0.18f, 0.09f, 0.95f);
            var bright = new Color(1f, 0.85f, 0.5f, 1f);
            closeBtn.style.borderTopColor = bright;
            closeBtn.style.borderBottomColor = bright;
            closeBtn.style.borderLeftColor = bright;
            closeBtn.style.borderRightColor = bright;
        });
        closeBtn.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            closeBtn.style.backgroundColor = closeBg;
            closeBtn.style.borderTopColor = closeBorder;
            closeBtn.style.borderBottomColor = closeBorder;
            closeBtn.style.borderLeftColor = closeBorder;
            closeBtn.style.borderRightColor = closeBorder;
        });
        backlogHeader.Add(closeBtn);

        backlogScroll = new ScrollView(ScrollViewMode.Vertical);
        backlogScroll.name = "backlog-scroll";
        backlogScroll.style.flexGrow = 1;
        backlogScroll.style.paddingLeft = 40;
        backlogScroll.style.paddingRight = 40;
        backlogScroll.style.paddingTop = 20;
        backlogScroll.style.paddingBottom = 20;
        backlogPanel.Add(backlogScroll);
        BuildCustomScrollbar(); // 自绘滚动条（隐藏原生）
    }

    // ================= 自绘滚动条（Unity 6 原生滚动条无法主题化：隐藏原生，自画轨道/滑块/车挡桩） =================
    private VisualElement sbTrack, sbThumb, sbUp, sbDepot;   // sbDepot=底端石造机库（替下端车挡）
    private VisualElement sbClip;                            // thumb 裁剪容器（出库动画防溢出）
    private VisualElement sbRailL, sbRailR, sbLamp;          // 分镜要单独控制的部件
    private VisualElement sbBallast, sbRoadbed, sbBeam, sbRaw; // 道砟 / 路基(含原始地面前沿帽) / 光束
    private readonly System.Collections.Generic.List<VisualElement> _roadPiles = new(); // 路基底缘卸料堆
    private readonly System.Collections.Generic.List<VisualElement> _matTies = new();   // 散放在路基上的枕木捆
    private readonly System.Collections.Generic.List<VisualElement> _matRails = new();  // 散放在路基上的钢轨捆
    private bool sbIdleAnimDone;                             // 分镜每次打开重播（打开时重置）
    private bool sbIdlePlaying;                              // 分镜播放中
    private bool sbIdleApplied;                              // 驻留景视觉已套用（切回溢出态要还原）
    private bool sbIdleDragging;                             // 驻留景微拖（含回弹过程）
    private float sbIdleGrab;
    private float sbIdleThumbTarget;                         // 驻留景列车停位（微拖回弹目标）
    private float sbRoadbedFinalH;                           // 路基终态高（几何分支算，分镜读，不依赖布局）
    private IVisualElementScheduledItem sbIdleSched;
    private IVisualElementScheduledItem sbIdleSnapSched;
    private const float IdleTrackH = 260f;                  // 驻留景轨道总长（占比均衡：完工段不再全挤在底部）
    private const float IdleThumbH = 64f;                   // 驻留景短列车长
    private const float SleeperBandY = 70f;                  // 枕木带起点：其上=无枕道砟段
    private const float FinishedH = 120f;                   // 完备段高（钢轨+列车）
    private const float RawCapMax = 140f;                    // 原始地面前沿帽上限（占地面带≤50%）
    private const float DepotH = 56f;                        // 机库高
    private const float DepotW = 36f;                        // 机库宽（与道砟列居中）
    private IVisualElementScheduledItem sbSync;
    private bool sbDragging;
    private float sbGrabDelta;
    private bool sbDeparting;                       // 出库动画进行中（thumb 由动画驱动）
    private IVisualElementScheduledItem sbDepartSched;
    private float _smokeAcc;                        // 柴油怠速清烟计时
    private readonly System.Collections.Generic.List<VisualElement> _sleepers = new();
    private float _sleepersBuiltFor = -1f;
    private float _sleepersStartY = -1f;                    // 枕木带起点键（驻留景≠0）

    private void BuildCustomScrollbar()
    {
        // 隐藏原生滚动条（Unity 6 公开 API + 打开时再强杀一次，双保险）
        backlogScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        backlogScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        sbUp = MakeSbBufferStop(true);
        sbDepot = MakeSbDepot();          // 下端＝机库出发点（车挡只留顶端线路尽头）

        // —— 无壳铁轨（去胶囊外壳）：独立凸出枕木 + 左右钢轨条 + 金色车厢滑块 ——
        sbTrack = new VisualElement { name = "sb-track" };
        sbTrack.style.position = Position.Absolute;
        sbTrack.style.width = 16;
        sbTrack.style.overflow = Overflow.Visible; // 枕木向两侧凸出
        sbTrack.style.backgroundColor = StyleKeyword.None;
        sbTrack.style.borderTopWidth = 0; sbTrack.style.borderBottomWidth = 0;
        sbTrack.style.borderLeftWidth = 0; sbTrack.style.borderRightWidth = 0;
        sbTrack.style.borderTopLeftRadius = 0; sbTrack.style.borderTopRightRadius = 0;
        sbTrack.style.borderBottomLeftRadius = 0; sbTrack.style.borderBottomRightRadius = 0;
        sbTrack.style.paddingLeft = 0; sbTrack.style.paddingRight = 0;
        sbTrack.style.paddingTop = 0; sbTrack.style.paddingBottom = 0;

        // 钢轨：左右各 2px 亮金属条，贯穿全高（画在枕木之上）
        var railColor = new Color(0.92f, 0.86f, 0.7f, 1f);
        var railL = new VisualElement();
        railL.style.position = Position.Absolute;
        railL.style.left = 1; railL.style.width = 2;
        railL.style.top = 0; railL.style.bottom = 0;
        railL.style.backgroundColor = railColor;
        railL.pickingMode = PickingMode.Ignore;
        sbTrack.Add(railL);
        sbRailL = railL;
        var railR = new VisualElement();
        railR.style.position = Position.Absolute;
        railR.style.right = 1; railR.style.width = 2;
        railR.style.top = 0; railR.style.bottom = 0;
        railR.style.backgroundColor = railColor;
        railR.pickingMode = PickingMode.Ignore;
        sbTrack.Add(railR);
        sbRailR = railR;

        // 道砟道床：轨道最底层灰褐碎石，比枕木略宽；上下各延伸 22px 顶到滚动条两端（车挡桩下也铺满，不留黑口）
        var ballast = new VisualElement { name = "sb-ballast" };
        sbBallast = ballast;
        ballast.style.position = Position.Absolute;
        ballast.style.left = -8; ballast.style.right = -8;   // 宽32=纹理一整格，整块图案可见
        ballast.style.top = -22; ballast.style.bottom = -22;
        ballast.style.backgroundImage = new StyleBackground(MakeBallastTex());
        // 显示 32×64 游戏像素；纹理本体 64×128（2×密度 → 细颗粒），Point 采样
        ballast.style.backgroundSize = new BackgroundSize(Length.Pixels(32), Length.Pixels(64));
        ballast.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
        ballast.pickingMode = PickingMode.Ignore;
        sbTrack.Insert(0, ballast);   // 最底层（钢轨/枕木/滑块之上依次叠加）

        // —— 主题化滑块＝キハ07 俯视列车：奶油车身 + 朱色圆弧前脸 + 前窗带 + 浅灰圆顶 + 侧窗列 + 通风口 ——
        sbThumb = new VisualElement { name = "sb-thumb" };
        sbThumb.style.position = Position.Absolute;
        sbThumb.style.left = 5; sbThumb.style.right = 5;   // clip 内坐标（clip 左右各外扩10 → 绝对位±5，车宽26）
        sbThumb.style.backgroundColor = new Color(0.88f, 0.78f, 0.58f, 1f); // 黄かっ色2号 奶油车身
        sbThumb.style.borderTopWidth = 1; sbThumb.style.borderBottomWidth = 1;
        sbThumb.style.borderLeftWidth = 1; sbThumb.style.borderRightWidth = 1;
        var bodyEdge = new Color(0.55f, 0.44f, 0.26f, 1f);
        sbThumb.style.borderTopColor = bodyEdge; sbThumb.style.borderBottomColor = bodyEdge;
        sbThumb.style.borderLeftColor = bodyEdge; sbThumb.style.borderRightColor = bodyEdge;
        sbThumb.style.borderTopLeftRadius = 11; sbThumb.style.borderTopRightRadius = 11;  // 半圆前脸（キハ07 流线形）
        sbThumb.style.borderBottomLeftRadius = 9; sbThumb.style.borderBottomRightRadius = 9;
        sbThumb.style.overflow = Overflow.Hidden; // 子元素裁进轮廓

        var bandRed = new Color(0.78f, 0.24f, 0.15f, 1f);   // 朱色下裙/前后脸
        var glass = new Color(0.15f, 0.19f, 0.24f, 1f);     // 车窗玻璃
        void ThumbPart(float top, float h, Color c, float left, float right)
        {
            var e = new VisualElement();
            e.style.position = Position.Absolute;
            e.style.top = top; e.style.height = h;
            e.style.left = left; e.style.right = right;
            e.style.backgroundColor = c;
            e.pickingMode = PickingMode.Ignore;
            sbThumb.Add(e);
        }

        // 车头：朱色圆弧前脸(6) + 前挡风窗带(4)
        ThumbPart(0, 6, bandRed, 0, 0);
        ThumbPart(6, 4, glass, 0, 0);
        // 尾部：后窗带 + 朱色带
        var tailGlass = new VisualElement();
        tailGlass.style.position = Position.Absolute;
        tailGlass.style.bottom = 5; tailGlass.style.height = 4;
        tailGlass.style.left = 0; tailGlass.style.right = 0;
        tailGlass.style.backgroundColor = glass;
        tailGlass.pickingMode = PickingMode.Ignore;
        sbThumb.Add(tailGlass);
        var tailRed = new VisualElement();
        tailRed.style.position = Position.Absolute;
        tailRed.style.bottom = 0; tailRed.style.height = 5;
        tailRed.style.left = 0; tailRed.style.right = 0;
        tailRed.style.backgroundColor = bandRed;
        tailRed.pickingMode = PickingMode.Ignore;
        sbThumb.Add(tailRed);

        // 浅灰圆顶屋面（前窗带之后到尾窗带之前），前鼻圆弧收头
        var roof = new VisualElement();
        roof.style.position = Position.Absolute;
        roof.style.top = 10; roof.style.bottom = 9; roof.style.left = 4; roof.style.right = 4;
        roof.style.backgroundColor = new Color(0.58f, 0.58f, 0.56f, 1f);   // 屋面中灰（做底）
        roof.style.borderTopWidth = 1; roof.style.borderBottomWidth = 1;
        roof.style.borderLeftWidth = 1; roof.style.borderRightWidth = 1;
        var roofEdge = new Color(0.40f, 0.40f, 0.39f, 1f);
        roof.style.borderTopColor = roofEdge; roof.style.borderBottomColor = roofEdge;
        roof.style.borderLeftColor = roofEdge; roof.style.borderRightColor = roofEdge;
        roof.style.borderTopLeftRadius = 7; roof.style.borderTopRightRadius = 7;   // 屋顶前鼻圆弧
        roof.style.borderBottomLeftRadius = 6; roof.style.borderBottomRightRadius = 6;
        roof.style.overflow = Overflow.Hidden;
        roof.pickingMode = PickingMode.Ignore;
        // 纵向三段明暗：两侧檐口暗、中间弧顶亮——打破"平灰长方形"的板状观感
        void RoofStrip(Length left, Length right, Color c)
        {
            var e = new VisualElement();
            e.style.position = Position.Absolute;
            e.style.top = 0; e.style.bottom = 0;
            e.style.left = left; e.style.right = right;
            e.style.backgroundColor = c;
            e.pickingMode = PickingMode.Ignore;
            roof.Add(e);
        }
        RoofStrip(0, new Length(65, LengthUnit.Percent), new Color(0.45f, 0.45f, 0.44f, 1f));          // 左檐口暗
        RoofStrip(new Length(35, LengthUnit.Percent), new Length(35, LengthUnit.Percent), new Color(0.74f, 0.74f, 0.72f, 1f)); // 弧顶高光
        RoofStrip(new Length(65, LengthUnit.Percent), 0, new Color(0.45f, 0.45f, 0.44f, 1f));          // 右檐口暗
        sbThumb.Add(roof);

        // 顶置前灯：屋面鼻端一点暖光（キハ07 大圆灯位）
        var lamp = new VisualElement();
        sbLamp = lamp;
        lamp.style.position = Position.Absolute;
        lamp.style.top = 11; lamp.style.width = 4; lamp.style.height = 4;
        lamp.style.left = Length.Percent(50);              // 自居中：不依赖车宽假设，杜绝偏右
        lamp.style.translate = new Translate(-2f, 0);      // 半宽回移
        lamp.style.backgroundColor = new Color(1f, 0.93f, 0.68f, 1f);
        lamp.style.borderTopLeftRadius = 2; lamp.style.borderTopRightRadius = 2;
        lamp.style.borderBottomLeftRadius = 2; lamp.style.borderBottomRightRadius = 2;
        lamp.pickingMode = PickingMode.Ignore;
        sbThumb.Add(lamp);

        // 屋面通风口：三只小灰盒（模型车顶特征）
        foreach (var pct in new[] { 32, 48, 64 })
        {
            var vent = new VisualElement();
            vent.style.position = Position.Absolute;
            vent.style.width = 6; vent.style.height = 3;   // 自居中
            vent.style.left = Length.Percent(50);
            vent.style.translate = new Translate(-3f, 0);
            vent.style.top = new Length(pct, LengthUnit.Percent);
            vent.style.backgroundColor = new Color(0.44f, 0.44f, 0.43f, 1f);
            vent.pickingMode = PickingMode.Ignore;
            sbThumb.Add(vent);
        }

        // 侧窗列：奶油带上的深色窗格（俯视时车厢两侧的窗线）
        foreach (var pct in new[] { 26, 38, 50, 62, 74 })
        {
            foreach (var side in new[] { 0, 1 })   // 0=左 1=右
            {
                var win = new VisualElement();
                win.style.position = Position.Absolute;
                win.style.top = new Length(pct, LengthUnit.Percent);
                win.style.height = 4; win.style.width = 2;
                if (side == 0) win.style.left = 1; else win.style.right = 1;
                win.style.backgroundColor = glass;
                win.pickingMode = PickingMode.Ignore;
                sbThumb.Add(win);
            }
        }
        // thumb 裁剪容器：出库动画时列车在轨道底端之外，须被裁住（配合机库遮挡）；
        // 左右各外扩 10 容纳宽于轨道的车身，picking 忽略（事件穿到轨道）
        sbClip = new VisualElement { name = "sb-thumb-clip" };
        sbClip.style.position = Position.Absolute;
        sbClip.style.left = -10; sbClip.style.right = -10;
        sbClip.style.top = 0; sbClip.style.height = 100;
        sbClip.style.overflow = Overflow.Hidden;
        sbClip.pickingMode = PickingMode.Ignore;
        sbTrack.Add(sbClip);
        sbClip.Add(sbThumb);

        // 前灯光束：向上渐淡三段（clip 坐标系，位置随 thumb 顶由分镜/终态驱动）
        sbBeam = new VisualElement { name = "sb-beam" };
        sbBeam.style.position = Position.Absolute;
        sbBeam.style.left = 10; sbBeam.style.width = 16;   // clip 中心=18，锥形光柱容器
        sbBeam.style.top = -21; sbBeam.style.height = 21;
        sbBeam.style.opacity = 0;
        sbBeam.pickingMode = PickingMode.Ignore;
        // 光锥：近灯窄而亮、远端宽而淡（桶口朝上）
        foreach (var band in new[] { (8f, 15f, 6f, 0.9f), (11f, 9f, 6f, 0.6f), (14f, 3f, 6f, 0.4f), (16f, 0f, 3f, 0.2f) })
        {
            var b = new VisualElement();
            b.style.position = Position.Absolute;
            b.style.width = band.Item1; b.style.height = band.Item2;
            b.style.left = (16f - band.Item1) / 2f;
            b.style.top = band.Item3;
            b.style.backgroundColor = new Color(1f, 0.95f, 0.72f, band.Item4);
            b.pickingMode = PickingMode.Ignore;
            sbBeam.Add(b);
        }
        sbClip.Add(sbBeam);

        // 路基＝一个连续整体：从机库口一路铺到列顶；顶部「原始地面前沿帽」是它的子层，
        // 随生长一起上移（不再单独淡入——上一版两层各自为政导致中段割裂）
        sbRoadbed = new VisualElement { name = "sb-roadbed" };
        sbRoadbed.style.position = Position.Absolute;
        sbRoadbed.style.width = 32;
        sbRoadbed.style.right = 2;
        sbRoadbed.style.top = 0; sbRoadbed.style.height = 0;   // 几何/分镜驱动（终态只在播放完后由几何设）
        sbRoadbed.style.backgroundImage = new StyleBackground(MakeRoadbedTex());
        sbRoadbed.style.backgroundSize = new BackgroundSize(Length.Pixels(32), Length.Pixels(32));
        sbRoadbed.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
        sbRoadbed.style.overflow = Overflow.Hidden;
        sbRoadbed.pickingMode = PickingMode.Ignore;
        sbRoadbed.style.display = DisplayStyle.None;           // 仅驻留景显示

        sbRaw = new VisualElement { name = "sb-raw" };         // 前沿帽：路基顶上的未动土
        sbRaw.style.position = Position.Absolute;
        sbRaw.style.top = 0; sbRaw.style.left = 0; sbRaw.style.right = 0;
        sbRaw.style.height = 120;                              // 由几何按实际地面带再校
        sbRaw.style.backgroundImage = new StyleBackground(MakeRawGroundTex());
        sbRaw.style.backgroundSize = new BackgroundSize(Length.Pixels(32), Length.Pixels(32));
        sbRaw.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
        sbRaw.pickingMode = PickingMode.Ignore;
        sbRoadbed.Add(sbRaw);

        // 施工标杆：底缘锚定（贴施工前沿，=轨道顶之上），固定像素
        foreach (var stake in new[] { (7f, 6f), (21f, 34f) })
        {
            var box = new VisualElement();
            box.style.position = Position.Absolute;
            box.style.left = stake.Item1;
            box.style.bottom = (IdleTrackH + 4f) + stake.Item2;
            box.style.width = 8; box.style.height = 17;
            box.pickingMode = PickingMode.Ignore;
            var pole = new VisualElement();
            pole.style.position = Position.Absolute;
            pole.style.left = 3; pole.style.bottom = 0; pole.style.width = 2; pole.style.height = 14;
            pole.style.backgroundColor = new Color(0.45f, 0.38f, 0.28f, 1f);
            pole.pickingMode = PickingMode.Ignore;
            var flag = new VisualElement();
            flag.style.position = Position.Absolute;
            flag.style.left = 1; flag.style.bottom = 14; flag.style.width = 6; flag.style.height = 3;
            flag.style.backgroundColor = new Color(0.82f, 0.3f, 0.22f, 1f);   // 测量红标
            flag.pickingMode = PickingMode.Ignore;
            box.Add(pole);
            box.Add(flag);
            sbRoadbed.Add(box);
        }

        backlogPanel.Add(sbRoadbed);
        backlogPanel.Add(sbTrack);
        backlogPanel.Add(sbUp);
        backlogPanel.Add(sbDepot);
        sbUp.BringToFront();   // 车挡永不被道砟/路基盖住

        // 卸料道砟堆：骑在轨道顶缘外（衔接路基前沿），铺砟工序时亮出
        foreach (var pile in new[] { (2f, 10f, 0.62f), (13f, 9f, 0.68f), (23f, 10f, 0.56f) })
        {
            var st = new VisualElement();
            st.style.position = Position.Absolute;
            st.style.left = pile.Item1; st.style.width = pile.Item2; st.style.height = 5;
            st.style.top = -6;
            st.style.backgroundColor = new Color(pile.Item3, pile.Item3 - 0.04f, pile.Item3 - 0.08f, 1f);
            st.pickingMode = PickingMode.Ignore;
            st.style.opacity = 0;
            sbTrack.Add(st);
            _roadPiles.Add(st);
        }

        // 散放在路基上的材料（真实工序：钢轨/轨枕先沿线散布在路基两侧，再方正入槽）
        // 枕木捆：三根横放的深色木条，位于轨道顶之上的施工带上
        for (int i = 0; i < 3; i++)
        {
            var m = new VisualElement();
            m.style.position = Position.Absolute;
            m.style.left = -5; m.style.right = -5;
            m.style.height = 3;
            m.style.top = -44 + i * 5;
            m.style.backgroundColor = new Color(0.34f, 0.23f, 0.13f, 1f);
            m.pickingMode = PickingMode.Ignore;
            m.style.opacity = 0;
            sbTrack.Add(m);
            _matTies.Add(m);
        }
        // 钢轨捆：两根细长亮色轨条，躺在更远的路基上
        for (int i = 0; i < 2; i++)
        {
            var m = new VisualElement();
            m.style.position = Position.Absolute;
            m.style.left = -7; m.style.right = -7;
            m.style.height = 2;
            m.style.top = -60 + i * 4;
            m.style.backgroundColor = new Color(0.82f, 0.80f, 0.74f, 1f);
            m.pickingMode = PickingMode.Ignore;
            m.style.opacity = 0;
            sbTrack.Add(m);
            _matRails.Add(m);
        }

        // 可交互：悬停切换手型（轨道/车挡/机库统一）
        foreach (var h in new[] { sbTrack, sbUp, sbDepot })
        {
            h.RegisterCallback<PointerEnterEvent>(e => UiCursor.ShowHand());
            h.RegisterCallback<PointerLeaveEvent>(e => UiCursor.ShowArrow());
        }

        // 拖拽/点击统一在 track 上处理（thumb 冒泡上来，localPosition 相对 track）
        sbTrack.RegisterCallback<PointerDownEvent>(e =>
        {
            VNManager.Instance?.SuppressNextAdvance();
            e.StopImmediatePropagation();
            // 分镜播放中：吞掉点击（场景在演，不响应）
            if (sbIdleApplied && !sbIdleAnimDone) return;
            // 驻留景（终态）：可交互手型 + 列车微拖（上下一小段，松手回弹），其余区域不响应
            if (sbIdleApplied && sbIdleAnimDone)
            {
                float ty = sbThumb.style.top.value.value;
                float y0 = e.localPosition.y;
                if (y0 >= ty - 6f && y0 <= ty + IdleThumbH + 6f)
                {
                    sbIdleDragging = true;
                    sbIdleGrab = y0 - ty;
                    sbTrack.CapturePointer(e.pointerId);
                }
                return;
            }
            float y = e.localPosition.y;
            float thumbTop = sbThumb.layout.y, thumbH = sbThumb.layout.height;
            if (y >= thumbTop && y <= thumbTop + thumbH)
            {
                sbDragging = true;
                sbGrabDelta = y - thumbTop;
            }
            else
            {
                SbJumpTo(y); // 点轨道空白：跳转
            }
            sbTrack.CapturePointer(e.pointerId);
        });
        sbTrack.RegisterCallback<PointerMoveEvent>(e =>
        {
            if (sbIdleDragging)
            {
                // 微拖：上 −12 / 下 +52（可明显退进车库），松手回弹
                float top = Mathf.Clamp(e.localPosition.y - sbIdleGrab, sbIdleThumbTarget - 12f, sbIdleThumbTarget + 52f);
                sbThumb.style.top = top;
                if (sbBeam != null) sbBeam.style.top = top - 7f;
                return;
            }
            if (!sbDragging) return;
            SbSetThumbTop(e.localPosition.y - sbGrabDelta);
        });
        sbTrack.RegisterCallback<PointerUpEvent>(e =>
        {
            if (sbIdleDragging)
            {
                // 回弹到停位（0.25s），期间保持 sbIdleDragging 挡住终态覆写
                float from = sbThumb.style.top.value.value;
                float to = sbIdleThumbTarget;
                float t0s = Time.unscaledTime;
                sbIdleSnapSched?.Pause();
                sbIdleSnapSched = backlogPanel.schedule.Execute(() =>
                {
                    float t = Mathf.Clamp01((Time.unscaledTime - t0s) / 0.25f);
                    float top = Mathf.Lerp(from, to, 1f - Mathf.Pow(1f - t, 3f));
                    sbThumb.style.top = top;
                    if (sbBeam != null) sbBeam.style.top = top - 7f;
                    if (t >= 1f)
                    {
                        sbIdleDragging = false;
                        sbIdleSnapSched?.Pause();
                    }
                }).Every(16);
                if (sbTrack.HasPointerCapture(e.pointerId)) sbTrack.ReleasePointer(e.pointerId);
                return;
            }
            sbDragging = false;
            if (sbTrack.HasPointerCapture(e.pointerId)) sbTrack.ReleasePointer(e.pointerId);
        });
        sbTrack.RegisterCallback<PointerCaptureOutEvent>(e => { sbDragging = false; });
    }

    /// <summary>轨道端点「車止め」车挡桩：红白横梁 + 双柱 + 轨上底座（替代上下箭头，点击翻页功能不变）。
    /// up=true 桩在轨道上方（梁朝下对轨道），几何按 up 定义后对 down 做垂直镜像。</summary>
    private VisualElement MakeSbBufferStop(bool up)
    {
        var b = new VisualElement { name = up ? "sb-up" : "sb-down" };
        b.style.position = Position.Absolute;
        b.style.width = 16; b.style.height = 20;
        b.style.backgroundColor = StyleKeyword.None; // 透明底，只画桩

        // up=true：梁(顶)→双柱→底座(贴轨道)；up=false 垂直镜像
        float Y(float top, float h) => up ? top : 20f - top - h;

        var steelRed = new Color(0.78f, 0.25f, 0.17f, 1f);
        var steelWhite = new Color(0.95f, 0.93f, 0.88f, 1f);
        var steelPost = new Color(0.30f, 0.30f, 0.33f, 1f);   // 深色立柱：在道砟上也读得出
        var steelEdge = new Color(0.50f, 0.48f, 0.45f, 1f);    // 座顶亮边
        var steelDark = new Color(0.20f, 0.19f, 0.18f, 1f);    // 底座

        void Part(float top, float h, Color c, float left, float right)
        {
            var e = new VisualElement();
            e.style.position = Position.Absolute;
            e.style.top = Y(top, h); e.style.height = h;
            e.style.left = left; e.style.right = right;
            e.style.backgroundColor = c;
            e.pickingMode = PickingMode.Ignore;
            b.Add(e);
        }

        Part(1, 4, steelRed, 0, 0);                 // 缓冲梁（红）
        Part(1, 4, steelWhite, 6, 6);               // 梁中央白条
        Part(5, 8, steelPost, 1, 12);               // 左柱（3px，压住左轨）
        Part(5, 8, steelPost, 12, 1);               // 右柱（3px，压住右轨）
        Part(13, 1, steelEdge, 0, 0);               // 座顶亮边（结构分层）
        Part(14, 5, steelDark, 0, 0);               // 轨上底座（加高，不被道砟吃掉）

        b.RegisterCallback<PointerDownEvent>(e =>
        {
            VNManager.Instance?.SuppressNextAdvance();
            e.StopImmediatePropagation();
            SbScrollBy(up);
        });
        b.RegisterCallback<ClickEvent>(e =>
        {
            VNManager.Instance?.SuppressNextAdvance();
            e.StopImmediatePropagation();
        });
        return b;
    }

    /// <summary>底端石造机库（人吉石造车库形制 36×56，拱门面朝轨道）：
    /// 保留原下端按钮的点击向下翻页；z 在轨道之上，出库时列车从其身后驶出。</summary>
    private VisualElement MakeSbDepot()
    {
        var b = new VisualElement { name = "sb-depot" };
        b.style.position = Position.Absolute;
        b.style.width = DepotW; b.style.height = DepotH;
        b.style.backgroundColor = StyleKeyword.None;
        b.style.backgroundImage = new StyleBackground(MakeDepotTex());
        b.style.backgroundSize = new BackgroundSize(Length.Pixels((int)DepotW), Length.Pixels((int)DepotH));
        b.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
        b.pickingMode = PickingMode.Position;
        b.RegisterCallback<PointerDownEvent>(e =>
        {
            VNManager.Instance?.SuppressNextAdvance();
            e.StopImmediatePropagation();
            SbScrollBy(false);
        });
        b.RegisterCallback<ClickEvent>(e =>
        {
            VNManager.Instance?.SuppressNextAdvance();
            e.StopImmediatePropagation();
        });
        return b;
    }

    /// <summary>铺设独立凸出枕木（无壳铁轨）：每 16px 一根，比轨道左右各宽 4px 形成凸出；高度变化时重建。</summary>
    private void EnsureSleepers(float trackH, float startY = 0f)
    {
        if (trackH <= 0f || (Mathf.Approximately(_sleepersBuiltFor, trackH) && Mathf.Approximately(_sleepersStartY, startY))) return;
        foreach (var s in _sleepers) s.RemoveFromHierarchy();
        _sleepers.Clear();
        const float gap = 16f, tieH = 6f;
        // 驻留景只在枕木带（startY 起）铺枕：其上是无枕道砟段/路基/原始地面（越往上越原始）
        int n = startY >= trackH ? 0 : Mathf.FloorToInt((trackH - startY - tieH) / gap) + 1;
        var tieColor = new Color(0.42f, 0.28f, 0.16f, 1f);
        var tieEdge = new Color(0.55f, 0.38f, 0.22f, 1f);
        for (int i = 0; i < n; i++)
        {
            var tie = new VisualElement();
            tie.style.position = Position.Absolute;
            tie.style.left = -4;      // 向两侧凸出（轨道宽16，枕木宽24）
            tie.style.right = -4;
            tie.style.top = startY + i * gap;
            tie.style.height = tieH;
            tie.style.backgroundColor = tieColor;
            tie.style.borderTopWidth = 1; tie.style.borderTopColor = tieEdge;
            tie.style.borderBottomWidth = 1; tie.style.borderBottomColor = new Color(0.3f, 0.2f, 0.12f, 1f);
            tie.style.borderTopLeftRadius = 1; tie.style.borderTopRightRadius = 1;
            tie.style.borderBottomLeftRadius = 1; tie.style.borderBottomRightRadius = 1;
            tie.pickingMode = PickingMode.Ignore; // 不挡滑块拖拽
            sbTrack.Insert(1, tie);               // index0 是道砟：枕木压在道砟上、钢轨之下
            _sleepers.Add(tie);
        }
        _sleepersBuiltFor = trackH;
        _sleepersStartY = startY;
    }

    private void SbScrollBy(bool up)
    {
        float viewH = backlogScroll.contentRect.height;
        float y = Mathf.Clamp(backlogScroll.scrollOffset.y + (up ? -0.5f : 0.5f) * viewH, 0f, SbMaxScroll());
        backlogScroll.scrollOffset = new Vector2(0, y);
        UpdateCustomScrollbar();
    }

    private float SbMaxScroll()
    {
        float viewH = backlogScroll.contentRect.height;
        float contentH = ContentHeight();
        return Mathf.Max(0f, contentH - viewH);
    }

    /// <summary>内容真实高度：取条目实际 layout 底边（contentContainer.layout 在 Unity 6 可能只返回可视高，估算会偏小）。</summary>
    private float ContentHeight()
    {
        float maxBottom = 0f;
        foreach (var c in backlogScroll.contentContainer.Children())
        {
            float b = c.layout.y + c.layout.height;
            if (b > maxBottom) maxBottom = b;
        }
        return Mathf.Max(maxBottom, backlogScroll.contentContainer.layout.height);
    }

    /// <summary>打开面板时强杀原生滚动条：隐藏 ScrollView 下「不含内容容器且不含文字」的直接分支（滚动条控件层）。
    /// 含 TextElement 的分支一律保留——曾因 contentContainer 判定失灵把整个内容层误杀导致回顾空白。</summary>
    private void HideNativeScrollbars()
    {
        var cc = backlogScroll.contentContainer;
        var snapshot = new List<VisualElement>();
        foreach (var c in backlogScroll.Children()) snapshot.Add(c);
        foreach (var c in snapshot)
        {
            if (ReferenceEquals(c, cc)) continue;
            if (SubtreeContains(c, cc)) continue;   // 包装了内容容器的层
            if (SubtreeHasText(c)) continue;        // 含文字=条目内容层，必须保留
            c.style.display = DisplayStyle.None;
        }
    }

    private static bool SubtreeHasText(VisualElement e)
    {
        if (e is Label) return true; // 条目内容
        if (e is TextElement te && !string.IsNullOrEmpty(te.text)) return true; // 有文字的按钮（如「跳转」）
        foreach (var c in e.Children())
            if (SubtreeHasText(c)) return true;
        return false;
    }

    private static bool SubtreeContains(VisualElement root, VisualElement target)
    {
        if (ReferenceEquals(root, target)) return true;
        foreach (var c in root.Children())
            if (SubtreeContains(c, target)) return true;
        return false;
    }

    private void SbJumpTo(float trackY)
    {
        float trackH = sbTrack.contentRect.height;
        float thumbH = Mathf.Max(28f, SbThumbHeight());
        float usable = Mathf.Max(1f, trackH - thumbH);
        float ratio = Mathf.Clamp01((trackY - thumbH * 0.5f) / usable);
        backlogScroll.scrollOffset = new Vector2(0, ratio * SbMaxScroll());
        UpdateCustomScrollbar();
    }

    private void SbSetThumbTop(float top)
    {
        float trackH = sbTrack.contentRect.height;
        float thumbH = Mathf.Max(28f, SbThumbHeight());
        float usable = Mathf.Max(1f, trackH - thumbH);
        float ratio = Mathf.Clamp01(top / usable);
        sbThumb.style.top = ratio * usable;
        backlogScroll.scrollOffset = new Vector2(0, ratio * SbMaxScroll());
    }

    private float SbThumbHeight()
    {
        float viewH = backlogScroll.contentRect.height;
        float contentH = backlogScroll.contentContainer.layout.height;
        if (contentH <= 0f) return 9999f;
        return sbTrack.contentRect.height * (viewH / contentH);
    }

    /// <summary>每帧同步：对齐 scroll 区域、按比例缩放/定位 thumb（打开期间 schedule 驱动）。</summary>
    private void UpdateCustomScrollbar()
    {
        if (sbTrack == null || backlogScroll == null) return;
        float sy = backlogScroll.layout.y;
        float sh = backlogScroll.layout.height;
        if (sh <= 0f) return;

        float maxScroll = SbMaxScroll();
        bool overflowing = maxScroll > 1f;

        if (overflowing)
        {
            // 正常滚动条几何：车挡(sy..sy+20) 轨道(sy+22..sh-60) 机库(sh-DepotH..sh)
            sbUp.style.top = sy;
            sbTrack.style.top = sy + 22;
            float trkH = Mathf.Max(40f, sh - 22f - (4f + DepotH));
            sbTrack.style.height = trkH;
            if (sbClip != null) sbClip.style.height = trkH + 4f + DepotH;   // 轨道+间隙4+机库高：出库起点在此底缘
            sbDepot.style.top = sy + sh - DepotH;
            // 整列往左移防裁切（道砟右缘留2px余量）；机库宽36 居中于轨道(16) → right = 10-10 = 0
            sbUp.style.right = 10; sbTrack.style.right = 10; sbDepot.style.right = 0;
            if (sbRoadbed != null) sbRoadbed.style.display = DisplayStyle.None;
        }
        else
        {
            // 驻留景几何：地面带(原始帽+夯实施工) → 轨道260(道砟段/枕木带/完备段) → 间隙4 → 机库56
            sbDepot.style.top = sy + sh - DepotH;
            float trackTop = sy + sh - 4f - DepotH - IdleTrackH;
            sbTrack.style.top = trackTop;
            sbTrack.style.height = IdleTrackH;
            if (sbClip != null) sbClip.style.height = IdleTrackH + 4f + DepotH;
            sbTrack.style.right = 10; sbDepot.style.right = 0;
            // 路基终态高（分镜读这个，不依赖布局）；前沿帽高度=地面带的一半、封顶140
            sbRoadbedFinalH = Mathf.Max(0f, sh - DepotH);
            if (sbRoadbed != null)
            {
                float groundVis = Mathf.Max(0f, sbRoadbedFinalH - (IdleTrackH + 4f));
                if (sbRaw != null) sbRaw.style.height = Mathf.Min(RawCapMax, groundVis * 0.5f);
                // 终态才显示完整路基；预备/动画态由分镜姿态驱动（根除“开场就铺好”的闪帧）
                if (sbIdleAnimDone && !sbIdlePlaying)
                {
                    sbRoadbed.style.display = DisplayStyle.Flex;
                    sbRoadbed.style.top = sy;
                    sbRoadbed.style.height = sbRoadbedFinalH;
                }
            }
        }

        if (sbDragging) return; // 拖拽中由指针驱动
        if (sbDeparting) return; // 出库动画中由动画驱动 thumb
        float trackH = sbTrack.contentRect.height;

        if (!overflowing)
        {
            // 无溢出 = 驻留景：施工梯度 + 机库 + 短列车（可微拖），分镜每次打开重播
            sbTrack.style.display = DisplayStyle.Flex;
            sbDepot.style.display = DisplayStyle.Flex;
            sbUp.style.display = DisplayStyle.None;
            if (sbIdlePlaying) return;                 // 分镜逐帧驱动中
            if (sbIdleDragging) return;                // 微拖/回弹中，别让终态覆写 thumb
            // 分镜/终态全部用常量几何（不等内容布局）→ 打开当帧即入预备态，零闪帧
            EnsureSleepers(IdleTrackH, SleeperBandY);   // 枕木只铺枕木带（其上=无枕道砟/路基）
            if (sbIdleAnimDone) ApplyIdleFinalState(IdleTrackH);
            else StartIdleSequence(IdleTrackH);
            return;
        }

        // 溢出态：若之前是驻留景，先还原交互与视觉
        if (sbIdleApplied) ExitIdleVisuals();

        EnsureSleepers(trackH); // 高度就绪后铺枕木（无壳铁轨）
        var vis = DisplayStyle.Flex;
        sbTrack.style.display = vis;
        sbUp.style.display = vis;
        sbDepot.style.display = vis;
        // 柴油机车怠速清烟：schedule 每 80ms 一拍，约每 3s 飘一缕
        _smokeAcc += 0.08f;
        if (_smokeAcc >= 3f) { _smokeAcc = 0f; SpawnExhaustPuff(); }
        float thumbH = Mathf.Min(Mathf.Max(SbThumbHeight(), 28f), trackH * 0.95f); // 封顶 95%，强制露出轨道层次
        float ratio = Mathf.Clamp01(backlogScroll.scrollOffset.y / maxScroll);
        if (!sbLogged)
        {
            sbLogged = true;
            if (TraceLog.ConsoleVerbose)
                Debug.Log($"[SB sync] view={backlogScroll.contentRect.height:F0} content={backlogScroll.contentContainer.layout.height:F0} max={maxScroll:F0} thumb={thumbH:F0}/{trackH:F0}");
        }
        sbThumb.style.height = thumbH;
        sbThumb.style.top = ratio * Mathf.Max(0f, trackH - thumbH);
    }

    /// <summary>驻留景分镜（每次打开完整重播）——按真实有砟轨道工序，放慢加细节（总长约8.4s）：
    /// ①机库落位0–0.7 ②路基自机库口贯穿到顶0.6–2.4（前沿帽随行，一体不割裂）
    /// ③卸料2.3–3.4（砟堆/枕木捆/钢轨捆沿线散布出现）④铺道砟3.3–4.6
    /// ⑤铺枕木4.5–5.9（自车库口逐根，枕木捆随用随减）⑥上钢轨5.8–6.5（钢轨捆随用消失）
    /// ⑦起道捣固6.4–7.0（枕木逐根微沉波）⑧出库6.9–7.7 ⑨前灯脉冲+光束7.7–8.4。
    /// 终态=施工梯度：原始帽→夯实施工带→无枕道砟→枕木带→完备段+列车（越上越原始）。</summary>
    private void StartIdleSequence(float trackH)
    {
        if (sbIdlePlaying) return;

        sbIdlePlaying = true;
        sbIdleApplied = true;

        // 路基：收拢到机库口（动画自机库口向上贯穿到顶）；终态高来自几何分支字段
        float roadFinalH = sbRoadbedFinalH;
        float roadBottom = sbDepot.style.top.value.value;
        if (sbRoadbed != null)
        {
            sbRoadbed.style.display = DisplayStyle.Flex;
            sbRoadbed.style.top = roadBottom;
            sbRoadbed.style.height = 0f;
        }

        // 预备态：道砟零高贴轨底、枕木/钢轨/卸料/材料全隐、thumb 藏库后、机库透明、光束灭
        EnsureSleepers(trackH, SleeperBandY);
        foreach (var t in _sleepers) { t.style.opacity = 0; t.style.translate = new Translate(0, 0); }
        if (sbBallast != null)
        {
            sbBallast.style.opacity = 1;
            sbBallast.style.top = IdleTrackH + 22f;   // bottom=-22 固定 → 零高贴底
        }
        foreach (var rail in new[] { sbRailL, sbRailR })
        {
            rail.style.opacity = 0;
            rail.style.top = trackH;
            rail.style.height = 0f;
        }
        foreach (var p in _roadPiles) p.style.opacity = 0;
        foreach (var m in _matTies) m.style.opacity = 0;
        foreach (var m in _matRails) m.style.opacity = 0;
        sbDepot.style.opacity = 0;
        sbThumb.style.height = IdleThumbH;
        sbThumb.style.top = trackH;
        if (sbBeam != null)
        {
            sbBeam.style.opacity = 0;
            sbBeam.style.top = trackH - 7f;
        }
        _smokeAcc = 0f;

        float t0 = Time.unscaledTime;
        var lampBase = new Color(1f, 0.93f, 0.68f, 1f);
        int puffed = 0;
        sbIdleSched?.Pause();
        sbIdleSched = backlogPanel.schedule.Execute(() =>
        {
            float e = Time.unscaledTime - t0;
            float EaseOut3(float x) => 1f - Mathf.Pow(1f - x, 3f);
            float Pop(float start, float dur = 0.3f) => Mathf.Clamp01((e - start) / dur);

            // ① 机库落位 0–0.7s
            float p1 = EaseOut3(Mathf.Clamp01(e / 0.7f));
            sbDepot.style.opacity = p1;
            sbDepot.style.translate = new Translate(0, 4f * (1f - p1));

            // ② 路基自机库口贯穿到顶 0.6–2.4s（一个整体，前沿帽随行）
            float grow = roadFinalH * EaseOut3(Mathf.Clamp01((e - 0.6f) / 1.8f));
            if (sbRoadbed != null)
            {
                sbRoadbed.style.top = roadBottom - grow;
                sbRoadbed.style.height = grow;
            }

            // ③ 卸料 2.3–3.4s：枕木捆/钢轨捆沿线散布出现（工序：材料先到路基两侧）
            for (int i = 0; i < _matTies.Count; i++)
                _matTies[i].style.opacity = Mathf.Clamp01((e - (2.3f + i * 0.15f)) / 0.3f);
            for (int i = 0; i < _matRails.Count; i++)
                _matRails[i].style.opacity = Mathf.Clamp01((e - (2.8f + i * 0.2f)) / 0.3f);

            // ④ 铺道砟 3.3–4.6s：top 从轨道底升到-22（bottom=-22 固定）＝自下向上展开
            if (sbBallast != null)
                sbBallast.style.top = Mathf.Lerp(IdleTrackH + 22f, -22f, EaseOut3(Mathf.Clamp01((e - 3.3f) / 1.3f)));
            foreach (var p in _roadPiles) p.style.opacity = Pop(3.4f);

            // ⑤ 铺枕木 4.5–5.9s：自车库口逐根向上；枕木捆随用随减
            int n = _sleepers.Count;
            float stag = n > 1 ? 1.4f / n : 1.4f;
            for (int i = 0; i < n; i++)
            {
                float st = 4.5f + (n - 1 - i) * stag;
                _sleepers[i].style.opacity = Mathf.Clamp01((e - st) / 0.14f);
            }
            float tieFade = 1f - Mathf.Clamp01((e - 4.5f) / 1.0f);
            for (int i = 0; i < _matTies.Count; i++)
            {
                float pop = Mathf.Clamp01((e - (2.3f + i * 0.15f)) / 0.3f);
                _matTies[i].style.opacity = Mathf.Min(pop, tieFade);   // 出现后随铺设消耗
            }

            // ⑥ 上钢轨 5.8–6.5s：只长完备段，自下向上；钢轨捆消失
            float rp = Mathf.Clamp01((e - 5.8f) / 0.08f);
            float growRail = EaseOut3(Mathf.Clamp01((e - 5.8f) / 0.7f)) * FinishedH;
            foreach (var rail in new[] { sbRailL, sbRailR })
            {
                rail.style.opacity = rp;
                rail.style.top = trackH - growRail;
                rail.style.height = growRail;
            }
            float railFade = 1f - Mathf.Clamp01((e - 5.8f) / 0.6f);
            for (int i = 0; i < _matRails.Count; i++)
            {
                float pop = Mathf.Clamp01((e - (2.8f + i * 0.2f)) / 0.3f);
                _matRails[i].style.opacity = Mathf.Min(pop, railFade);
            }

            // ⑦ 起道捣固 6.4–7.0s：枕木逐根微沉波（工序：捣固机沿线路走）
            if (e > 6.4f)
            {
                for (int i = 0; i < n; i++)
                {
                    float p = Mathf.Clamp01((e - (6.4f + (n - 1 - i) * 0.035f)) / 0.28f);
                    float ty = p >= 1f ? 0f : (1f - p) * 2f * Mathf.Sin(p * 10f * Mathf.PI);
                    _sleepers[i].style.translate = new Translate(0, ty);
                }
            }

            // ⑧ 出库上轨 6.9–7.7s：back-out 回弹
            if (e > 6.9f)
            {
                float p = Mathf.Clamp01((e - 6.9f) / 0.8f);
                float c1 = 1.2f;
                float pm = p - 1f;
                float be = 1f + (c1 + 1f) * pm * pm * pm + c1 * pm * pm;
                sbThumb.style.top = Mathf.LerpUnclamped(trackH, trackH - IdleThumbH, be);
                if (p >= 1f && puffed == 0) { puffed = 1; SpawnExhaustPuff(); }
            }

            // ⑨ 前灯脉冲 + 光束 7.7–8.4s（灯亮=光束出）
            if (e > 7.7f && e < 8.4f && sbLamp != null)
            {
                float lp = ((e - 7.7f) % 0.22f) / 0.22f;
                bool on = lp < 0.5f;
                sbLamp.style.backgroundColor = on ? new Color(1f, 0.98f, 0.8f, 1f) : lampBase;
                if (sbBeam != null)
                {
                    sbBeam.style.top = (trackH - IdleThumbH) - 7f;
                    sbBeam.style.opacity = on ? 1f : 0.2f;
                }
            }

            // 终态
            if (e >= 8.4f)
            {
                if (sbLamp != null) sbLamp.style.backgroundColor = lampBase;
                sbIdleAnimDone = true;
                sbIdlePlaying = false;
                sbIdleSched?.Pause();
                ApplyIdleFinalState(trackH);
            }
        }).Every(16);
    }

    /// <summary>驻留景终态：施工梯度完整呈现 + 列车停位 + 微光光束（可微拖）。</summary>
    private void ApplyIdleFinalState(float trackH)
    {
        if (Mathf.Abs(trackH - IdleTrackH) > 12f) return;
        sbIdleApplied = true;
        sbIdleThumbTarget = trackH - IdleThumbH;
        sbDepot.style.opacity = 1f;
        sbDepot.style.translate = new Translate(0, 0);
        foreach (var t in _sleepers) t.style.opacity = 1f;
        if (sbBallast != null)
        {
            sbBallast.style.opacity = 1f;
            sbBallast.style.top = -22f;
        }
        foreach (var rail in new[] { sbRailL, sbRailR })
        {
            rail.style.opacity = 1f;
            rail.style.top = trackH - FinishedH;    // 只覆盖完备段
            rail.style.height = StyleKeyword.Auto;  // top..bottom0 拉伸
        }
        foreach (var p in _roadPiles) p.style.opacity = 1f;
        foreach (var t in _sleepers) t.style.translate = new Translate(0, 0);
        foreach (var m in _matTies) m.style.opacity = 0;      // 材料已用尽
        foreach (var m in _matRails) m.style.opacity = 0;
        sbThumb.style.height = IdleThumbH;
        if (!sbIdleDragging) sbThumb.style.top = sbIdleThumbTarget;
        if (sbBeam != null)
        {
            sbBeam.style.top = sbIdleThumbTarget - 7f;
            sbBeam.style.opacity = 0.85f;           // 常亮光柱：明显可辨
        }
    }

    /// <summary>驻留景 → 正常滚动条：还原交互、透明度、灯色、钢轨拉伸、道砟位置与路基/原始地面隐藏。</summary>
    private void ExitIdleVisuals()
    {
        sbIdleApplied = false;
        sbIdlePlaying = false;
        sbIdleDragging = false;
        sbIdleSched?.Pause();
        sbIdleSnapSched?.Pause();
        sbDepot.style.opacity = 1f;
        sbDepot.style.translate = new Translate(0, 0);
        foreach (var t in _sleepers) t.style.opacity = 1f;
        if (sbBallast != null)
        {
            sbBallast.style.opacity = 1f;
            sbBallast.style.top = -22f;             // 恢复全高（含上下延伸）
        }
        foreach (var rail in new[] { sbRailL, sbRailR })
        {
            rail.style.opacity = 1f;
            rail.style.top = 0;
            rail.style.height = StyleKeyword.Auto;   // 恢复 top0/bottom0 拉伸
        }
        foreach (var p in _roadPiles) p.style.opacity = 0;   // 卸料堆只属于施工场景
        foreach (var m in _matTies) m.style.opacity = 0;
        foreach (var m in _matRails) m.style.opacity = 0;
        if (sbRoadbed != null) sbRoadbed.style.display = DisplayStyle.None;   // 原始帽是其子层，随隐藏
        if (sbBeam != null) sbBeam.style.opacity = 0f;
        if (sbLamp != null) sbLamp.style.backgroundColor = new Color(1f, 0.93f, 0.68f, 1f);
    }

    private bool sbLogged; // 每次打开面板只记一次数值

    /// <summary>出库动画：打开回顾时列车从机库身后（轨道底端之外）驶出到当前滚动位，0.7s 三次缓出。</summary>
    private void StartDepart()
    {
        if (sbDeparting || sbTrack == null || sbThumb == null) return;
        float trackH = sbTrack.contentRect.height;
        float maxScroll = SbMaxScroll();
        if (trackH <= 0f || maxScroll <= 1f) return;   // 无溢出：留给二期专属分镜
        float thumbH = Mathf.Min(Mathf.Max(SbThumbHeight(), 28f), trackH * 0.95f);
        sbThumb.style.height = thumbH;
        float usable = Mathf.Max(0f, trackH - thumbH);
        float ratio = Mathf.Clamp01(backlogScroll.scrollOffset.y / maxScroll);
        float from = trackH;          // 轨道底之下＝机库身后，被机库遮住
        float to = ratio * usable;    // 当前滚动位（通常=底部＝库门口）
        sbDeparting = true;
        sbThumb.style.top = from;
        float t0 = Time.unscaledTime;
        sbDepartSched?.Pause();
        sbDepartSched = backlogPanel.schedule.Execute(() =>
        {
            if (sbDragging) { sbDeparting = false; sbDepartSched?.Pause(); return; }
            float t = Mathf.Clamp01((Time.unscaledTime - t0) / 0.7f);
            float e = 1f - Mathf.Pow(1f - t, 3f);              // cubic ease-out
            sbThumb.style.top = Mathf.Lerp(from, to, e);
            if (t >= 1f)
            {
                sbDeparting = false;
                sbDepartSched?.Pause();
                SpawnExhaustPuff();                             // 停稳排气两缕
                backlogPanel.schedule.Execute(() => SpawnExhaustPuff()).StartingIn(180);
            }
        }).Every(16);
    }

    /// <summary>柴油清烟：3×3 淡灰小方块自车顶排气口上升 10px 淡出（0.7s）。</summary>
    private void SpawnExhaustPuff()
    {
        if (sbTrack == null || sbThumb == null) return;
        if (sbTrack.style.display != DisplayStyle.Flex) return;
        float thumbH = sbThumb.layout.height > 0f ? sbThumb.layout.height : sbThumb.style.height.value.value;
        if (thumbH < 8f) return;   // thumb 尚未布局好
        var puff = new VisualElement();
        puff.style.position = Position.Absolute;
        puff.style.width = 3; puff.style.height = 3;
        puff.style.left = UnityEngine.Random.Range(5, 11);
        puff.style.top = sbThumb.layout.y + 14f;
        puff.style.backgroundColor = new Color(0.72f, 0.73f, 0.75f, 1f);
        puff.style.opacity = 0.55f;
        puff.style.borderTopLeftRadius = 1; puff.style.borderTopRightRadius = 1;
        puff.pickingMode = PickingMode.Ignore;
        sbTrack.Add(puff);   // 后加=在 thumb 之上
        float t0 = Time.unscaledTime;
        IVisualElementScheduledItem sched = null;   // 先声明：lambda 内不能引用自身初始化中的局部变量(CS0841)
        sched = sbTrack.schedule.Execute(() =>
        {
            float t = Mathf.Clamp01((Time.unscaledTime - t0) / 0.7f);
            puff.style.translate = new Translate(0, -10f * t);
            puff.style.opacity = 0.55f * (1f - t);
            if (t >= 1f) { sched?.Pause(); puff.RemoveFromHierarchy(); }
        }).Every(30);
    }



    /// <summary>道砟纹理 64×128（以 32×64 游戏像素显示 = 2×线密度）：逐纹素多色细噪——
    /// 每个纹素直接是石色（8 色加权），约 1/9 加深为石缝点，2×2 局部 70% 同色成微团；
    /// 显示粒径 ≈0.5–0.7 游戏像素（约为上一版 1/5）。边缘阶梯挖空破直线。</summary>
    private static Texture2D MakeBallastTex()
    {
        const int s = 64, H = 128;
        var tex = new Texture2D(s, H, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
        };

        // 石色：中性灰阶为主，掺紫灰与褐石（对照实拍道砟）
        var palette = new[]
        {
            new Color32(150, 148, 143, 255),
            new Color32(132, 130, 126, 255),
            new Color32(114, 112, 108, 255),
            new Color32(96, 94, 91, 255),
            new Color32(78, 76, 74, 255),
            new Color32(130, 120, 126, 255),   // 紫灰
            new Color32(116, 104, 96, 255),    // 褐
            new Color32(158, 152, 140, 255),   // 亮暖
        };
        // 加权抽取：灰阶占大头，紫灰/褐/亮暖点缀
        int[] weight = { 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 5, 6, 7 };

        uint seed = 20261006u;
        uint Next() { seed = seed * 1664525u + 1013904223u; return seed; }
        Color32 Pick() => palette[weight[Next() % (uint)weight.Length]];

        // 2×2 微团（70%）：团内同色、团间各异 → 粒径贴在纹素级
        for (int cy = 0; cy < H; cy += 2)
        {
            for (int cx = 0; cx < s; cx += 2)
            {
                Color32 cell = Pick();
                bool solid = Next() % 10 < 7;
                for (int dy = 0; dy < 2; dy++)
                {
                    for (int dx = 0; dx < 2; dx++)
                    {
                        Color32 c = solid ? cell : Pick();
                        if (Next() % 9 == 0)   // 石缝点
                            c = new Color32((byte)(c.r * 3 / 5), (byte)(c.g * 3 / 5), (byte)(c.b * 3 / 5), 255);
                        tex.SetPixel((cx + dx) % s, (cy + dy) % H, c);
                    }
                }
            }
        }

        // 边缘豁口：左右各 6 纹素（≈1 游戏像素）阶梯挖空，破刀切直线
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < 6; x++)
            {
                int p = x < 3 ? 60 : 30;
                if (Next() % 100 < p) SetAlpha(tex, x, y, 0);
                else if (Next() % 3 == 0) SetAlpha(tex, x, y, 130);
                if (Next() % 100 < p) SetAlpha(tex, s - 1 - x, y, 0);
                else if (Next() % 3 == 0) SetAlpha(tex, s - 1 - x, y, 130);
            }
        }
        tex.Apply();
        return tex;
    }

    private static void SetAlpha(Texture2D tex, int x, int y, byte a)
    {
        var c = tex.GetPixel(x, y);
        c.a = a / 255f;
        tex.SetPixel(x, y, c);
    }

    /// <summary>建设中路基纹理 32×32：夯实施工土 + 压实横纹 + 白灰放样中线 + 两侧边坡压暗 + 细土粒。</summary>
    private static Texture2D MakeRoadbedTex()
    {
        const int s = 32, H = 32;
        var tex = new Texture2D(s, H, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
        };
        uint seed = 90210u;
        uint Next() { seed = seed * 1664525u + 1013904223u; return seed; }
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < s; x++)
            {
                Color32 c;
                if (y % 7 == 0) c = new Color32(74, 56, 40, 255);            // 压实横纹
                else
                {
                    int m = (int)(Next() % 10);
                    if (m == 0) c = new Color32(106, 84, 60, 255);           // 亮土粒
                    else if (m == 1) c = new Color32(66, 50, 36, 255);       // 暗土粒
                    else c = new Color32(88, 68, 50, 255);                   // 夯土底
                }
                // 白灰放样中线：断续虚线（施工测量线）
                if ((x == 15 || x == 16) && y % 8 < 5) c = new Color32(198, 194, 182, 255);
                // 两侧边坡压暗（路基横断面读感）
                if (x < 3 || x > s - 4) c = new Color32((byte)(c.r * 3 / 4), (byte)(c.g * 3 / 4), (byte)(c.b * 3 / 4), 255);
                tex.SetPixel(x, y, c);
            }
        }
        // 边缘豁口（与道砟一致的破直线索引）+ 底缘抖动过渡（与夯实施工带衔接不硬切）
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < 2; x++)
            {
                if (Next() % 10 < 5) SetAlpha(tex, x, y, 0);
                if (Next() % 10 < 5) SetAlpha(tex, s - 1 - x, y, 0);
            }
        }
        for (int y = H - 4; y < H; y++)   // 底部4行渐进挖空 → 抖动接缝
        {
            int hole = (y - (H - 4) + 1) * 20;   // 20/40/60/80%
            for (int x = 0; x < s; x++)
                if (Next() % 100 < hole) SetAlpha(tex, x, y, 0);
        }
        tex.Apply();
        return tex;
    }

    /// <summary>原始地面纹理 32×32：未动土（深浅土块 + 稀疏草茎）——施工梯度最顶层的「原始」。</summary>
    private static Texture2D MakeRawGroundTex()
    {
        const int s = 32, H = 32;
        var tex = new Texture2D(s, H, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
        };
        uint seed = 31337u;
        uint Next() { seed = seed * 1664525u + 1013904223u; return seed; }
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < s; x++)
            {
                int m = (int)(Next() % 10);
                Color32 c = m == 0 ? new Color32(58, 50, 38, 255)
                         : m == 1 ? new Color32(84, 74, 56, 255)
                                  : new Color32(72, 64, 48, 255);   // 未动土底
                tex.SetPixel(x, y, c);
            }
        }
        // 稀疏草茎（顶部起 3–5px 的橄榄绿竖点）
        for (int i = 0; i < 10; i++)
        {
            int gx = (int)(Next() % s);
            int gh = 3 + (int)(Next() % 3);
            for (int y = 0; y < gh; y++)
                tex.SetPixel(gx, (y + (int)(Next() % 4)) % H, new Color32(84, 96, 54, 255));
        }
        // 边缘豁口
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < 2; x++)
            {
                if (Next() % 10 < 5) SetAlpha(tex, x, y, 0);
                if (Next() % 10 < 5) SetAlpha(tex, s - 1 - x, y, 0);
            }
        }
        tex.Apply();
        return tex;
    }

    /// <summary>机库像素画 36×56：拱门面朝轨道（图上缘），石墙三拱门 + 檐口 + 坡屋面——
    /// 人吉石造车库形制加宽加长版。不透明（列车须被它遮住，出库才有"从库里出来"的观感）。</summary>
    private static Texture2D MakeDepotTex()
    {
        const int W = 36, H = 56;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                tex.SetPixel(x, y, new Color32(0, 0, 0, 0));

        var stone = new[]
        {
            new Color32(130, 128, 124, 255),
            new Color32(118, 116, 112, 255),
            new Color32(140, 138, 134, 255),
        };
        var mortar = new Color32(94, 92, 88, 255);
        var eave = new Color32(70, 66, 62, 255);
        var roof = new Color32(74, 74, 78, 255);
        var roofLine = new Color32(62, 62, 66, 255);
        var roofHi = new Color32(94, 94, 98, 255);
        uint seed = 77003u;
        uint Next() { seed = seed * 1664525u + 1013904223u; return seed; }

        // 檐口（上缘，贴轨道侧）
        for (int x = 0; x < W; x++) { tex.SetPixel(x, 0, eave); tex.SetPixel(x, 1, eave); }

        // 石墙 + 三拱门（拱顶朝上=门面朝轨道）：先建拱形掩膜，再分层上色——
        // 木门（竖板纹）+ 1px 亮石门框 + 墙体（解决“三个黑条条”问题）
        int[] centers = { 8, 18, 28 };
        var archMask = new bool[W, H];
        for (int y = 2; y <= 27; y++)
        {
            int dy = y - 2;
            int half = dy == 0 ? 2 : dy == 1 ? 3 : 4;   // 三行收肩成拱
            if (dy < 0) continue;
            foreach (int c in centers)
                for (int x = c - half; x <= c + half; x++)
                    if (x >= 0 && x < W) archMask[x, y] = true;
        }
        var woodDark = new Color32(52, 38, 26, 255);
        var wood = new Color32(68, 50, 34, 255);
        var frame = new Color32(158, 154, 146, 255);
        for (int y = 2; y <= 27; y++)
        {
            for (int x = 0; x < W; x++)
            {
                if (archMask[x, y])
                {
                    // 木门：竖板纹（每4px一缝）
                    int cNear = 0; int bestD = int.MaxValue;
                    foreach (int c in centers) { int dist = Mathf.Abs(x - c); if (dist < bestD) { bestD = dist; cNear = c; } }
                    Color32 d = ((x - cNear + 100) % 4 == 0) ? woodDark : wood;
                    // 拱顶两行压暗一点（门楣阴影）
                    if (y <= 3) d = new Color32((byte)(d.r * 4 / 5), (byte)(d.g * 4 / 5), (byte)(d.b * 4 / 5), 255);
                    tex.SetPixel(x, y, d);
                    continue;
                }
                // 门框：非掩膜但 8 邻域含掩膜 → 亮石框
                bool ring = false;
                for (int ny = -1; ny <= 1 && !ring; ny++)
                    for (int nx = -1; nx <= 1 && !ring; nx++)
                    {
                        int ax = x + nx, ay = y + ny;
                        if (ax >= 0 && ax < W && ay >= 0 && ay < H && archMask[ax, ay]) ring = true;
                    }
                if (ring) { tex.SetPixel(x, y, frame); continue; }
                // 墙体：石色 + 横灰缝
                Color32 s = stone[(int)(Next() % (uint)stone.Length)];
                if (y % 4 == 3) s = mortar;
                tex.SetPixel(x, y, s);
            }
        }

        // 屋面（门面之后往下＝屋顶）
        for (int y = 28; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                Color32 c = roof;
                if (y == 28) c = eave;                                  // 檐线
                else if (y % 5 == 0) c = roofLine;                      // 瓦楞
                if (y > 28 && (x < 2 || x > W - 3)) c = roofLine;       // 两侧压暗
                if (y > 28 && x >= 17 && x <= 18) c = roofHi;           // 屋脊高光
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }


    public void AddEntry(string speaker, string text, int sceneIndex, int dialogueIndex)
    {
        var entry = new BacklogEntry { speaker = speaker, text = text, sceneIndex = sceneIndex, dialogueIndex = dialogueIndex };
        entries.Add(entry);
        if (entries.Count > MaxEntries)
        {
            entries.RemoveAt(0);
            // 新来旧去：数据与行同步丢最旧一条（有限窗口）
            if (backlogScroll != null && backlogScroll.contentContainer.childCount > 0)
                backlogScroll.RemoveAt(0);
        }
        // 增量追加单行：不再全量 Clear+重建，滚动位置/视口稳定
        if (backlogScroll == null) return;
        backlogScroll.Add(BuildEntryRow(entry));
    }

    /// <summary>标点钉住：在行首危险标点前插 WORD JOINER（U+2060），禁止其被换到行首（CJK 避头尾）。</summary>
    private static string PinPunctuation(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        // zpix 的 ——（U+2014）字形是两段短横 → 换 U+2500 框线横双连（2em 整段长横）
        s = s.Replace("——", "──");
        var sb = new System.Text.StringBuilder(s.Length + 16);
        foreach (char c in s)
        {
            switch (c)
            {
                // 行首禁则：这些标点不能出现在行首 → 钉在前字符后
                case '。': case '，': case '、': case '；': case '：':
                case '！': case '？': case '…': case '～': case '·':
                case '」': case '』': case '）': case '】': case '》':
                case '”': case '’':
                    sb.Append('⁠'); // U+2060 WORD JOINER
                    break;
                // 行尾禁则：这些开符号不能留在行尾 → 钉在后字符前
                case '「': case '『': case '（': case '【': case '《': case '“': case '‘':
                    sb.Append(c);
                    sb.Append('⁠');
                    continue;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>构建单条回顾行：AddEntry 增量追加用——不再每条全量重建整表，滚动位置与视口稳定。</summary>
    private VisualElement BuildEntryRow(BacklogEntry entry)
    {
        var fontDef = GetFontDef();
        var entryRow = new VisualElement();
        entryRow.style.flexDirection = FlexDirection.Row;
        entryRow.style.alignItems = Align.Stretch;
        entryRow.style.marginBottom = 26;
        entryRow.style.paddingBottom = 18;
        entryRow.style.borderBottomWidth = 1;
        entryRow.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.15f);
        entryRow.style.paddingTop = 12;
        entryRow.style.paddingLeft = 8;
        entryRow.style.paddingRight = 8;
        entryRow.style.borderTopLeftRadius = 4;
        entryRow.style.borderTopRightRadius = 4;
        entryRow.style.borderBottomLeftRadius = 4;
        entryRow.style.borderBottomRightRadius = 4;

        // 文本内容容器（占满剩余宽度）
        var contentBox = new VisualElement();
        contentBox.style.flexGrow = 1;
        contentBox.style.paddingRight = 12;

        bool isNarration = string.IsNullOrEmpty(entry.speaker) || entry.speaker == "n";

        if (!isNarration)
        {
            var speakerLabel = new Label(entry.speaker);
            speakerLabel.style.fontSize = 24;
            speakerLabel.style.letterSpacing = 2;
            speakerLabel.style.color = new Color(1f, 200f / 255f, 100f / 255f, 0.9f);
            speakerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            speakerLabel.style.marginBottom = 10;
            speakerLabel.style.unityFontDefinition = fontDef;
            // 底色只包人名：Label 默认 stretch 会把棕条拉满整行——收缩到文字宽
            speakerLabel.style.alignSelf = Align.FlexStart;
            speakerLabel.style.backgroundColor = new Color(0.15f, 0.08f, 0.04f, 0.6f);
            speakerLabel.style.paddingLeft = 8;
            speakerLabel.style.paddingRight = 8;
            speakerLabel.style.paddingTop = 4;
            speakerLabel.style.paddingBottom = 4;
            speakerLabel.style.borderTopLeftRadius = 4;
            speakerLabel.style.borderTopRightRadius = 4;
            speakerLabel.style.borderBottomLeftRadius = 4;
            speakerLabel.style.borderBottomRightRadius = 4;
            speakerLabel.style.borderLeftWidth = 3;
            speakerLabel.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.6f);
            contentBox.Add(speakerLabel);
        }

        var textLabel = new Label(PinPunctuation(entry.text));
        textLabel.style.fontSize = 26;
        textLabel.style.letterSpacing = 2;
        textLabel.style.color = new Color(1f, 1f, 1f, 0.85f);
        textLabel.style.whiteSpace = WhiteSpace.Normal;
        textLabel.style.marginBottom = 8;
        textLabel.style.unityFontDefinition = fontDef;
        contentBox.Add(textLabel);
        entryRow.Add(contentBox);

        // 右侧"跳转"按钮：跳转到该条目（闭包抓 scene/dialogue 索引，不依赖窗口内序号，淘汰不失效）
        var jumpBtn = new UnityEngine.UIElements.Button(() => JumpToEntry(entry.sceneIndex, entry.dialogueIndex)) { text = "跳转" };
        jumpBtn.style.width = 90;
        jumpBtn.style.height = 40;
        jumpBtn.style.alignSelf = Align.FlexEnd;
        jumpBtn.style.flexShrink = 0;
        jumpBtn.style.marginLeft = 8;
        jumpBtn.style.marginBottom = 8;
        jumpBtn.style.fontSize = 22;
        jumpBtn.style.color = new Color(1f, 1f, 1f, 0.9f);
        jumpBtn.style.backgroundColor = new Color(0.25f, 0.14f, 0.08f, 0.9f);
        jumpBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        jumpBtn.style.unityFontDefinition = fontDef;
        jumpBtn.style.borderTopWidth = 1;
        jumpBtn.style.borderBottomWidth = 1;
        jumpBtn.style.borderLeftWidth = 1;
        jumpBtn.style.borderRightWidth = 1;
        jumpBtn.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        jumpBtn.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        jumpBtn.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        jumpBtn.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        jumpBtn.style.borderTopLeftRadius = 5;
        jumpBtn.style.borderTopRightRadius = 5;
        jumpBtn.style.borderBottomLeftRadius = 5;
        jumpBtn.style.borderBottomRightRadius = 5;
        jumpBtn.RegisterCallback<PointerEnterEvent>(e =>
        {
            jumpBtn.style.backgroundColor = new Color(0.35f, 0.2f, 0.1f, 0.95f);
            jumpBtn.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
            UiCursor.ShowHand();
        });
        jumpBtn.RegisterCallback<PointerLeaveEvent>(e =>
        {
            jumpBtn.style.backgroundColor = new Color(0.25f, 0.14f, 0.08f, 0.9f);
            jumpBtn.style.color = new Color(1f, 1f, 1f, 0.9f);
            UiCursor.ShowArrow();
        });
        entryRow.Add(jumpBtn);
        return entryRow;
    }

    private void JumpToEntry(int sceneIndex, int dialogueIndex)
    {
        OnEntryClicked?.Invoke(sceneIndex, dialogueIndex);
        ToggleBacklog();
    }

    public void ToggleBacklog()
    {
        isOpen = !isOpen;
        backlogPanel.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;

        if (isOpen)
        {
            // 强杀原生滚动条（子元素在面板显示后才生成，必须打开时执行——重叠根因）
            backlogScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            backlogScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            HideNativeScrollbars();
            // 自绘滚动条：布局后刷一次 + 打开期间持续同步（拖拽/滚动/内容变化）
            sbLogged = false;
            // 驻留景分镜：每次打开完整重播（用户要求；溢出态下这些标记不参与）
            sbIdleSched?.Pause();
            sbIdleSnapSched?.Pause();
            sbIdlePlaying = false;
            sbIdleAnimDone = false;
            sbIdleDragging = false;
            UpdateCustomScrollbar();
            if (sbSync != null) sbSync.Pause();
            sbSync = backlogPanel.schedule.Execute(UpdateCustomScrollbar).StartingIn(60).Every(80);
            // 延迟一帧滚到最底部（最新对话），布局完成后同步 thumb；再杀一次原生条（溢出后动态生成的）
            backlogScroll.schedule.Execute(() =>
            {
                HideNativeScrollbars();
                backlogScroll.scrollOffset = new Vector2(0, backlogScroll.contentContainer.layout.height);
                UpdateCustomScrollbar();
                StartDepart();   // 滚到底就位后：列车自机库身后驶出
            }).StartingIn(50);
        }
        else
        {
            sbSync?.Pause();
            sbDeparting = false;          // 关闭即中断出库动画
            sbDepartSched?.Pause();
            // 关闭即恢复箭头：跳转/关闭瞬间鼠标可能仍停在按钮上，Leave 不会触发
            UiCursor.ShowArrow();
        }
    }

    public bool IsOpen => isOpen;

    public void Clear()
    {
        entries.Clear();
        backlogScroll.Clear();
    }
}