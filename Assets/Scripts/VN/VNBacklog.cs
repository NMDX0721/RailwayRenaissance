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

    // ================= 自绘滚动条（Unity 6 原生滚动条无法主题化：隐藏原生，自画轨道/滑块/箭头） =================
    private VisualElement sbTrack, sbThumb, sbUp, sbDown;
    private IVisualElementScheduledItem sbSync;
    private bool sbDragging;
    private float sbGrabDelta;

    private void BuildCustomScrollbar()
    {
        // 隐藏原生滚动条（Unity 6 公开 API + 打开时再强杀一次，双保险）
        backlogScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        backlogScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        sbUp = MakeSbArrow(true);
        sbDown = MakeSbArrow(false);

        // —— 主题化轨道＝铁轨：16px 宽、2px 亮金边框、内部程序纹理（两条钢轨+枕木横纹 Repeat）——
        sbTrack = new VisualElement { name = "sb-track" };
        sbTrack.style.position = Position.Absolute;
        sbTrack.style.width = 16;
        sbTrack.style.backgroundColor = new Color(0.28f, 0.18f, 0.11f, 1f); // 木棕底（纹理兜底）
        sbTrack.style.backgroundImage = new StyleBackground(MakeRailTexture());
        sbTrack.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
        sbTrack.style.backgroundSize = new BackgroundSize(16, 16);
        sbTrack.style.borderTopWidth = 2; sbTrack.style.borderBottomWidth = 2;
        sbTrack.style.borderLeftWidth = 2; sbTrack.style.borderRightWidth = 2;
        var edge = new Color(0.82f, 0.62f, 0.35f, 0.85f);
        sbTrack.style.borderTopColor = edge; sbTrack.style.borderBottomColor = edge;
        sbTrack.style.borderLeftColor = edge; sbTrack.style.borderRightColor = edge;
        sbTrack.style.borderTopLeftRadius = 8; sbTrack.style.borderTopRightRadius = 8;
        sbTrack.style.borderBottomLeftRadius = 8; sbTrack.style.borderBottomRightRadius = 8;
        sbTrack.style.paddingLeft = 2; sbTrack.style.paddingRight = 2;
        sbTrack.style.paddingTop = 2; sbTrack.style.paddingBottom = 2;
        sbTrack.style.overflow = Overflow.Hidden;

        // —— 主题化滑块：比轨道内缩、亮金实心、1px 暗金内边、三条防滑纹 ——
        sbThumb = new VisualElement { name = "sb-thumb" };
        sbThumb.style.position = Position.Absolute;
        sbThumb.style.left = 2; sbThumb.style.right = 2;
        sbThumb.style.backgroundColor = new Color(0.95f, 0.8f, 0.5f, 1f);
        sbThumb.style.borderTopWidth = 1; sbThumb.style.borderBottomWidth = 1;
        sbThumb.style.borderLeftWidth = 1; sbThumb.style.borderRightWidth = 1;
        sbThumb.style.borderTopColor = new Color(1f, 0.96f, 0.85f, 1f);   // 顶部高光＝车厢金属顶
        sbThumb.style.borderBottomColor = new Color(0.55f, 0.38f, 0.18f, 1f);
        sbThumb.style.borderLeftColor = new Color(0.55f, 0.38f, 0.18f, 1f);
        sbThumb.style.borderRightColor = new Color(0.55f, 0.38f, 0.18f, 1f);
        sbThumb.style.borderTopLeftRadius = 6; sbThumb.style.borderTopRightRadius = 6;
        sbThumb.style.borderBottomLeftRadius = 6; sbThumb.style.borderBottomRightRadius = 6;
        // 防滑纹：三条暗金横线（滑块中部）
        for (int i = -1; i <= 1; i++)
        {
            var groove = new VisualElement();
            groove.style.position = Position.Absolute;
            groove.style.height = 1;
            groove.style.left = 4; groove.style.right = 4;
            groove.style.top = new Length(50, LengthUnit.Percent);
            groove.style.translate = new Translate(0, i * 4);
            groove.style.backgroundColor = new Color(0.6f, 0.42f, 0.2f, 0.7f);
            groove.pickingMode = PickingMode.Ignore;
            sbThumb.Add(groove);
        }
        sbTrack.Add(sbThumb);

        backlogPanel.Add(sbUp);
        backlogPanel.Add(sbTrack);
        backlogPanel.Add(sbDown);

        // 可交互：悬停切换手型（轨道/箭头/滑块统一）
        foreach (var h in new[] { sbTrack, sbUp, sbDown })
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

    private VisualElement MakeSbArrow(bool up)
    {
        var b = new VisualElement { name = up ? "sb-up" : "sb-down" };
        b.style.position = Position.Absolute;
        // 主题尺寸：与轨道同宽 16、高 20，2px 金边圆角钮（菜单条按钮同语法）
        b.style.width = 16; b.style.height = 20;
        b.style.backgroundColor = new Color(0.14f, 0.09f, 0.05f, 0.98f);
        b.style.borderTopWidth = 2; b.style.borderBottomWidth = 2;
        b.style.borderLeftWidth = 2; b.style.borderRightWidth = 2;
        var edge = new Color(0.82f, 0.62f, 0.35f, 0.85f);
        b.style.borderTopColor = edge; b.style.borderBottomColor = edge;
        b.style.borderLeftColor = edge; b.style.borderRightColor = edge;
        b.style.borderTopLeftRadius = 6; b.style.borderTopRightRadius = 6;
        b.style.borderBottomLeftRadius = 6; b.style.borderBottomRightRadius = 6;
        b.style.backgroundImage = new StyleBackground(MakeArrowTex(up));
        b.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        b.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
        b.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        b.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        b.style.unityBackgroundImageTintColor = new Color(1f, 0.86f, 0.59f, 1f);
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

    /// <summary>打开面板时强杀原生滚动条：隐藏 ScrollView 下所有「不含内容容器」的直接分支（滚动条控件层）。</summary>
    private void HideNativeScrollbars()
    {
        var cc = backlogScroll.contentContainer;
        var snapshot = new List<VisualElement>();
        foreach (var c in backlogScroll.Children()) snapshot.Add(c); // 手写快照：Children() 无 ToList（未引 Linq）
        foreach (var c in snapshot)
        {
            if (ReferenceEquals(c, cc)) continue;
            if (SubtreeContains(c, cc)) continue; // 包装了内容容器的层要保留
            c.style.display = DisplayStyle.None;
        }
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
        // 钮 20 高 + 2 间隙：up(sy..sy+20) track(sy+22 .. sh-22) down(sy+sh-20..)
        sbUp.style.top = sy;
        sbTrack.style.top = sy + 22;
        sbTrack.style.height = Mathf.Max(40f, sh - 44);
        sbDown.style.top = sy + sh - 20;
        sbUp.style.right = 4; sbTrack.style.right = 4; sbDown.style.right = 4;

        if (sbDragging) return; // 拖拽中由指针驱动
        float trackH = sbTrack.contentRect.height;
        float maxScroll = SbMaxScroll();
        bool overflowing = maxScroll > 1f;
        // 内容没溢出就不显示滚动条（与原生行为一致；全金柱观感的根因）
        var vis = overflowing ? DisplayStyle.Flex : DisplayStyle.None;
        sbTrack.style.display = vis;
        sbUp.style.display = vis;
        sbDown.style.display = vis;
        if (!overflowing) return;
        float thumbH = Mathf.Min(Mathf.Max(SbThumbHeight(), 28f), trackH * 0.95f); // 封顶 95%，强制露出轨道层次
        float ratio = Mathf.Clamp01(backlogScroll.scrollOffset.y / maxScroll);
        if (!sbLogged)
        {
            sbLogged = true;
            Debug.Log($"[SB sync] view={backlogScroll.contentRect.height:F0} content={backlogScroll.contentContainer.layout.height:F0} max={maxScroll:F0} thumb={thumbH:F0}/{trackH:F0}");
        }
        sbThumb.style.height = thumbH;
        sbThumb.style.top = ratio * Mathf.Max(0f, trackH - thumbH);
    }

    private bool sbLogged; // 每次打开面板只记一次数值


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
        Debug.Log($"[Backlog SB] pass all={all.Count} sliderHit={hitSlider} scrollerHit={hitScroller} btnHit={hitBtn} thumbHit={hitThumb} arrowHit={hitArrow}");
        if (all.Count > 1 && !_sbLogDone)
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
        entries.Add(new BacklogEntry { speaker = speaker, text = text, sceneIndex = sceneIndex, dialogueIndex = dialogueIndex });
        if (entries.Count > MaxEntries)
            entries.RemoveAt(0);
        UpdateBacklogDisplay();
    }

    /// <summary>标点钉住：在行首危险标点前插 WORD JOINER（U+2060），禁止其被换到行首（CJK 避头尾）。</summary>
    private static string PinPunctuation(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
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

    private void UpdateBacklogDisplay()
    {
        if (backlogScroll == null) return;
        backlogScroll.Clear();
        var fontDef = GetFontDef();

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
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

            // 右侧"跳转"按钮：专门跳转到该对话
            int capturedIdx = i;
            var jumpBtn = new UnityEngine.UIElements.Button(() => JumpToEntry(capturedIdx)) { text = "跳转" };
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

            backlogScroll.Add(entryRow);
        }
    }

    private void JumpToEntry(int index)
    {
        if (index < 0 || index >= entries.Count) return;
        var entry = entries[index];
        // 跳转到该条目的位置（sceneIndex, dialogueIndex）
        // 这样点击后显示的是该条目之后的下一句
        OnEntryClicked?.Invoke(entry.sceneIndex, entry.dialogueIndex);
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
            }).StartingIn(50);
        }
        else
        {
            sbSync?.Pause();
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