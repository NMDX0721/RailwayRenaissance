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
    private IVisualElementScheduledItem sbSync;
    private bool sbDragging;
    private float sbGrabDelta;
    private bool sbDeparting;                       // 出库动画进行中（thumb 由动画驱动）
    private IVisualElementScheduledItem sbDepartSched;
    private float _smokeAcc;                        // 柴油怠速清烟计时
    private readonly System.Collections.Generic.List<VisualElement> _sleepers = new();
    private float _sleepersBuiltFor = -1f;

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
        var railR = new VisualElement();
        railR.style.position = Position.Absolute;
        railR.style.right = 1; railR.style.width = 2;
        railR.style.top = 0; railR.style.bottom = 0;
        railR.style.backgroundColor = railColor;
        railR.pickingMode = PickingMode.Ignore;
        sbTrack.Add(railR);

        // 道砟道床：轨道最底层灰褐碎石，比枕木略宽；上下各延伸 22px 顶到滚动条两端（车挡桩下也铺满，不留黑口）
        var ballast = new VisualElement { name = "sb-ballast" };
        ballast.style.position = Position.Absolute;
        ballast.style.left = -8; ballast.style.right = -8;   // 宽32=纹理一整格，整块图案可见
        ballast.style.top = -22; ballast.style.bottom = -22;
        ballast.style.backgroundImage = new StyleBackground(MakeBallastTex());
        // 显式 32×64 平铺（与 MakeBallastTex 同尺寸）：不指定会被拉伸成糊，颗粒感全无
        ballast.style.backgroundSize = new BackgroundSize(Length.Pixels(32), Length.Pixels(64));
        ballast.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
        ballast.pickingMode = PickingMode.Ignore;
        sbTrack.Insert(0, ballast);   // 最底层（钢轨/枕木/滑块之上依次叠加）

        // —— 主题化滑块＝キハ07 俯视列车：奶油车身 + 朱色圆弧前脸 + 前窗带 + 浅灰圆顶 + 侧窗列 + 通风口 ——
        sbThumb = new VisualElement { name = "sb-thumb" };
        sbThumb.style.position = Position.Absolute;
        sbThumb.style.left = 7; sbThumb.style.right = 7;   // clip 内坐标（clip 左右各外扩10 → 绝对位±3，压住钢轨）
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
        lamp.style.position = Position.Absolute;
        lamp.style.top = 11; lamp.style.left = 8; lamp.style.width = 4; lamp.style.height = 4;
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
            vent.style.left = 7; vent.style.width = 6; vent.style.height = 3;
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

        // 轨道先加、端点后加：道砟上下各延伸 22px，车挡/机库须盖在道砟之上；
        // 机库在轨道之上 → 出库动画时列车从机库身后驶出（遮挡关系）
        backlogPanel.Add(sbTrack);
        backlogPanel.Add(sbUp);
        backlogPanel.Add(sbDepot);

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
            if (!sbDragging) return;
            SbSetThumbTop(e.localPosition.y - sbGrabDelta);
        });
        sbTrack.RegisterCallback<PointerUpEvent>(e =>
        {
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
        var steelGray = new Color(0.5f, 0.5f, 0.52f, 1f);
        var steelDark = new Color(0.28f, 0.28f, 0.3f, 1f);

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
        Part(5, 10, steelGray, 1, 13);              // 左柱（对齐左钢轨）
        Part(5, 10, steelGray, 13, 1);              // 右柱（对齐右钢轨）
        Part(15, 4, steelDark, 0, 0);               // 轨上底座

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

    /// <summary>底端石造机库（人吉石造车库形制 32×42，拱门面朝轨道）：
    /// 保留原下端按钮的点击向下翻页；z 在轨道之上，出库时列车从其身后驶出。</summary>
    private VisualElement MakeSbDepot()
    {
        var b = new VisualElement { name = "sb-depot" };
        b.style.position = Position.Absolute;
        b.style.width = 32; b.style.height = 42;
        b.style.backgroundColor = StyleKeyword.None;
        b.style.backgroundImage = new StyleBackground(MakeDepotTex());
        b.style.backgroundSize = new BackgroundSize(Length.Pixels(32), Length.Pixels(42));
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
    private void EnsureSleepers(float trackH)
    {
        if (trackH <= 0f || Mathf.Approximately(_sleepersBuiltFor, trackH)) return;
        foreach (var s in _sleepers) s.RemoveFromHierarchy();
        _sleepers.Clear();
        const float gap = 16f, tieH = 6f;
        int n = Mathf.CeilToInt((trackH + gap) / gap);
        var tieColor = new Color(0.42f, 0.28f, 0.16f, 1f);
        var tieEdge = new Color(0.55f, 0.38f, 0.22f, 1f);
        for (int i = 0; i < n; i++)
        {
            var tie = new VisualElement();
            tie.style.position = Position.Absolute;
            tie.style.left = -4;      // 向两侧凸出（轨道宽16，枕木宽24）
            tie.style.right = -4;
            tie.style.top = i * gap;
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
        // 顶端车挡 20 高+2 间隙；底端机库 42 高+4 间隙：
        // up(sy..sy+20) track(sy+22 .. sy+sh-46) depot(sy+sh-42 .. sy+sh)
        sbUp.style.top = sy;
        sbTrack.style.top = sy + 22;
        float trkH = Mathf.Max(40f, sh - 68);
        sbTrack.style.height = trkH;
        if (sbClip != null) sbClip.style.height = trkH + 46f;   // 轨道+间隙4+机库42：出库起点在此底缘
        sbDepot.style.top = sy + sh - 42;
        // 整列往左移防裁切：right=10 + 道砟半宽8 → 道砟右缘距面板边 2px 余量；
        // 机库宽32 居中于轨道(宽16) → right = 10 - 8 = 2
        sbUp.style.right = 10; sbTrack.style.right = 10; sbDepot.style.right = 2;

        if (sbDragging) return; // 拖拽中由指针驱动
        if (sbDeparting) return; // 出库动画中由动画驱动 thumb
        float trackH = sbTrack.contentRect.height;
        EnsureSleepers(trackH); // 高度就绪后铺枕木（无壳铁轨）
        float maxScroll = SbMaxScroll();
        bool overflowing = maxScroll > 1f;
        // 内容没溢出就不显示滚动条（与原生行为一致；全金柱观感的根因）
        var vis = overflowing ? DisplayStyle.Flex : DisplayStyle.None;
        sbTrack.style.display = vis;
        sbUp.style.display = vis;
        sbDepot.style.display = vis;
        if (!overflowing) return;
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
        var sched = sbTrack.schedule.Execute(() =>
        {
            float t = Mathf.Clamp01((Time.unscaledTime - t0) / 0.7f);
            puff.style.translate = new Translate(0, -10f * t);
            puff.style.opacity = 0.55f * (1f - t);
            if (t >= 1f) { sched.Pause(); puff.RemoveFromHierarchy(); }
        }).Every(30);
    }


    private static void StyleBacklogScrollbar(ScrollView scroll)
    {
        var track = new Color(0.12f, 0.08f, 0.05f, 0.9f);          // 轨道底
        var trackEdge = new Color(0.8f, 0.62f, 0.35f, 0.6f);       // 轨道金边
        var thumb = new Color(0.85f, 0.66f, 0.38f, 0.95f);         // 滑块
        var arrowBg = new Color(0.18f, 0.12f, 0.07f, 0.95f);       // 箭头钮底
        var glyphGold = new Color(1f, 0.86f, 0.59f, 1f);           // 三角 glyph

        // 轨道：10px 宽、深色底、1px 金边、全圆角
        void StyleTrack(VisualElement e)
        {
            e.style.width = 10;
            e.style.backgroundColor = track;
            e.style.borderTopWidth = 1;
            e.style.borderBottomWidth = 1;
            e.style.borderLeftWidth = 1;
            e.style.borderRightWidth = 1;
            e.style.borderTopColor = trackEdge;
            e.style.borderBottomColor = trackEdge;
            e.style.borderLeftColor = trackEdge;
            e.style.borderRightColor = trackEdge;
            e.style.borderTopLeftRadius = 5;
            e.style.borderTopRightRadius = 5;
            e.style.borderBottomLeftRadius = 5;
            e.style.borderBottomRightRadius = 5;
        }

        // 滑块：金色、圆角 4、左右 margin 2 让它比轨道细
        void StyleThumb(VisualElement e)
        {
            e.style.backgroundColor = thumb;
            e.style.borderTopLeftRadius = 4;
            e.style.borderTopRightRadius = 4;
            e.style.borderBottomLeftRadius = 4;
            e.style.borderBottomRightRadius = 4;
            e.style.marginLeft = 2;
            e.style.marginRight = 2;
        }

        // 上下箭头钮：深棕金边 + 自绘金三角贴图（覆盖 Unity 默认白三角 background）
        void StyleArrowButton(VisualElement e, bool isUp)
        {
            e.style.width = 10;
            e.style.height = 16;
            e.style.backgroundColor = arrowBg;
            e.style.borderTopWidth = 1;
            e.style.borderBottomWidth = 1;
            e.style.borderLeftWidth = 1;
            e.style.borderRightWidth = 1;
            e.style.borderTopColor = trackEdge;
            e.style.borderBottomColor = trackEdge;
            e.style.borderLeftColor = trackEdge;
            e.style.borderRightColor = trackEdge;
            // 覆盖默认白三角贴图 → 自绘金色像素三角
            e.style.backgroundImage = new StyleBackground(MakeArrowTex(isUp));
            e.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            e.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            e.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            e.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            e.style.unityBackgroundImageTintColor = glyphGold;
        }

        // 手写递归遍历整个 hierarchy（绕开 UQuery 的 contentContainer 语义——
        // 滚动条挂在 ScrollView 本体层，Query()/Q() 可能整棵够不着，这就是样式一直没命中的根因）
        var all = new System.Collections.Generic.List<VisualElement>();
        var parents = new System.Collections.Generic.Dictionary<VisualElement, VisualElement>();
        void Walk(VisualElement e, VisualElement p)
        {
            all.Add(e);
            parents[e] = p;
            foreach (var c in e.Children()) Walk(c, e);
        }
        Walk(scroll, null);

        int hitSlider = 0, hitScroller = 0, hitBtn = 0, hitThumb = 0, hitArrow = 0;

        // 1) 名字/class 子串分类（BEM 新旧命名都能被子串抓到）
        foreach (var e in all)
        {
            var hay = (e.name ?? "") + "|" + string.Join("|", e.GetClasses());
            var lower = hay.ToLowerInvariant();
            if (lower.Contains("up-button")) { StyleArrowButton(e, true); hitArrow++; continue; }
            if (lower.Contains("down-button")) { StyleArrowButton(e, false); hitArrow++; continue; }
            if (lower.Contains("dragger") || lower.Contains("thumb")) { StyleThumb(e); hitThumb++; continue; }
            if (lower.Contains("slider")) { StyleTrack(e); hitSlider++; continue; }
            if (lower.Contains("scroller")) { hitScroller++; continue; } // 容器只计数，不着色（防重叠）
        }

        // 2) 类型兜底（完全不依赖命名）：Slider=轨道；Scroller 下的 Button=箭头（遍历序 up→down）
        foreach (var e in all)
        {
            if (e is Slider && !e.ClassListContains("__sb_styled"))
            {
                StyleTrack(e);
                e.AddToClassList("__sb_styled");
                hitSlider++;
            }
        }
        var scrollers = new System.Collections.Generic.List<Scroller>();
        foreach (var e in all) if (e is Scroller sc && !sc.ClassListContains("__sb_sc")) { scrollers.Add(sc); sc.AddToClassList("__sb_sc"); hitScroller++; }
        foreach (var sc in scrollers)
        {
            int idx = 0;
            foreach (var e in all)
            {
                if (e is Button && IsUnder(e, sc, parents))
                {
                    StyleArrowButton(e, idx == 0); // Unity uxml 顺序：up 在前 down 在后
                    idx++;
                    hitBtn++;
                }
            }
        }

        // 诊断日志：每次调用都打一行（共三次：Init 构造 / 打开面板 / 50ms 布局后），详表只打一次
        if (TraceLog.ConsoleVerbose)
            Debug.Log($"[Backlog SB] pass all={all.Count} sliderHit={hitSlider} scrollerHit={hitScroller} btnHit={hitBtn} thumbHit={hitThumb} arrowHit={hitArrow}");
        if (TraceLog.ConsoleVerbose && all.Count > 1 && !_sbLogDone)
        {
            _sbLogDone = true;
            foreach (var e in all)
            {
                var hay = (e.name ?? "") + "|" + string.Join("|", e.GetClasses());
                var lower = hay.ToLowerInvariant();
                if (lower.Contains("scroll") || lower.Contains("slider") || lower.Contains("dragger") ||
                    lower.Contains("button") || lower.Contains("thumb") || e is Scroller || e is Slider)
                {
                    Debug.Log($"[Backlog SB] {e.GetType().Name} name='{e.name}' classes='{string.Join(" ", e.GetClasses())}'");
                }
            }
        }
    }

    private static bool _sbLogDone;

    /// <summary>铁轨纹理 16×16 平铺：木棕底 + 一条枕木横带 + 两条纵向钢轨（Repeat 铺成连续铁轨）。</summary>
    private static Texture2D MakeRailTexture()
    {
        const int s = 16;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
        };
        var tie = new Color32(46, 28, 16, 255);      // 枕木深棕
        var wood = new Color32(72, 46, 28, 255);      // 木底
        var rail = new Color32(225, 214, 188, 255);   // 磨亮钢轨面（暖银）
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                Color32 c = wood;
                if (y < 5) c = tie;                    // 枕木横带（顶部 5px，repeat 后每 16px 一条）
                if (x >= 3 && x <= 4) c = rail;        // 左钢轨
                if (x >= 11 && x <= 12) c = rail;      // 右钢轨
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    /// <summary>道砟纹理 32×64 平铺：深色石缝底 + 细密小石（半径1-2）+ 单像素微粒；
    /// 左右各 3 列做透明缺口（70% 概率挖空）→ 铺出来是碎石豁口边，不是刀切直线。
    /// 高 64 减少竖向平铺重复感。</summary>
    private static Texture2D MakeBallastTex()
    {
        const int s = 32, H = 64;
        var tex = new Texture2D(s, H, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
        };
        var gap = new Color32(78, 77, 75, 255);      // 石缝底（中深灰，实拍里石子几乎铺满，底只透缝）
        for (int y = 0; y < H; y++)
            for (int x = 0; x < s; x++)
                tex.SetPixel(x, y, gap);

        // 灰色为主（实拍参考：碎石灰阶），少量褐石/紫灰变化，整体不发棕
        var stones = new[]
        {
            new Color32(152, 150, 147, 255),   // 亮灰石
            new Color32(124, 123, 121, 255),   // 中灰石
            new Color32(98, 97, 96, 255),      // 暗灰石
            new Color32(124, 123, 121, 255),   // 中灰（加权）
            new Color32(98, 97, 96, 255),      // 暗灰（加权）
            new Color32(112, 100, 92, 255),    // 褐石点缀（约1/6）
        };
        uint seed = 20261004u;
        uint Next() { seed = seed * 1664525u + 1013904223u; return seed; }

        // 细颗粒：单像素为主（85%），少量 2px 小石增加起伏——颗粒上限就是 1px，再细只能靠密度
        for (int i = 0; i < 700; i++)
        {
            int cx = (int)(Next() % s);
            int cy = (int)(Next() % H);
            var c = stones[(int)(Next() % stones.Length)];
            if (Next() % 100 < 15)
            {
                // 15% 小石（半径2圆斑）
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx * dx + dy * dy > 1) continue;
                        tex.SetPixel((cx + dx + s) % s, (cy + dy + H) % H, c);
                    }
            }
            else
            {
                tex.SetPixel(cx, cy, c);   // 单像素石
            }
        }
        // 微粒补缝：260 个单像素
        for (int i = 0; i < 260; i++)
        {
            int x = (int)(Next() % s);
            int y = (int)(Next() % H);
            tex.SetPixel(x, y, stones[(int)(Next() % stones.Length)]);
        }
        // 边缘豁口：左右各 3 列按行概率挖透明，破掉刀切直线
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                if (Next() % 10 < 7) SetAlpha(tex, x, y, 0);
                else if (Next() % 2 == 0) SetAlpha(tex, x, y, 140);
                if (Next() % 10 < 7) SetAlpha(tex, s - 1 - x, y, 0);
                else if (Next() % 2 == 0) SetAlpha(tex, s - 1 - x, y, 140);
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

    /// <summary>机库像素画 32×42：拱门面朝轨道（图上缘），石墙三拱门 + 檐口 + 坡屋面——
    /// 人吉石造车库形制微缩。不透明（列车须被它遮住，出库才有"从库里出来"的观感）。</summary>
    private static Texture2D MakeDepotTex()
    {
        const int W = 32, H = 42;
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
            new Color32(128, 126, 122, 255),
            new Color32(116, 114, 110, 255),
            new Color32(136, 134, 130, 255),
        };
        var mortar = new Color32(94, 92, 88, 255);
        var eave = new Color32(70, 66, 62, 255);
        var door = new Color32(26, 24, 22, 255);
        var roof = new Color32(74, 74, 78, 255);
        var roofLine = new Color32(62, 62, 66, 255);
        var roofHi = new Color32(94, 94, 98, 255);
        uint seed = 77003u;
        uint Next() { seed = seed * 1664525u + 1013904223u; return seed; }

        // 檐口（上缘，贴轨道侧）
        for (int x = 0; x < W; x++) { tex.SetPixel(x, 0, eave); tex.SetPixel(x, 1, eave); }

        // 石墙 + 三拱门（拱顶朝上=门面朝轨道）
        int[] centers = { 7, 16, 25 };
        for (int y = 2; y <= 17; y++)
        {
            for (int x = 0; x < W; x++)
            {
                bool inArch = false;
                foreach (int c in centers)
                {
                    int dy = y - 2;
                    int half = dy < 2 ? 2 + dy * 2 : 4;   // 前两行收肩成拱顶圆弧
                    if (Mathf.Abs(x - c) <= half) { inArch = true; break; }
                }
                if (inArch) { tex.SetPixel(x, y, door); continue; }
                Color32 s = stone[(int)(Next() % (uint)stone.Length)];
                if (y % 4 == 3) s = mortar;               // 横灰缝
                tex.SetPixel(x, y, s);
            }
        }

        // 屋面（门面之后往下＝屋顶）
        for (int y = 18; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                Color32 c = roof;
                if (y == 18) c = eave;                                  // 檐线
                else if (y % 5 == 0) c = roofLine;                      // 瓦楞
                if (y > 18 && (x < 2 || x > W - 3)) c = roofLine;       // 两侧压暗
                if (y > 18 && x >= 15 && x <= 16) c = roofHi;           // 屋脊高光
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    /// <summary>e 是否位于 anc 之下（沿 parents 链上溯）。</summary>
    private static bool IsUnder(VisualElement e, VisualElement anc,
        System.Collections.Generic.Dictionary<VisualElement, VisualElement> parents)
    {
        VisualElement cur = e;
        int guard = 0;
        while (cur != null && guard++ < 64)
        {
            if (ReferenceEquals(cur, anc)) return true;
            if (!parents.TryGetValue(cur, out cur)) break;
        }
        return false;
    }

    /// <summary>自绘 7×4 像素金三角（覆盖 Unity 默认白三角贴图）。</summary>
    private static Texture2D MakeArrowTex(bool up)
    {
        const int w = 7, h = 4;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        var gold = new Color32(255, 219, 150, 255);
        var clear = new Color32(0, 0, 0, 0);
        for (int y = 0; y < h; y++)
        {
            int rowFromTop = up ? y : h - 1 - y;   // up：顶窄底宽；down：镜像
            int half = rowFromTop + 1;              // 行半宽 1,2,3,4
            int span = Mathf.Min(half * 2 - 1, w);
            int x0 = (w - span) / 2;
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, (x >= x0 && x < x0 + span) ? gold : clear);
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