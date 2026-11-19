using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.Account;
using RailwayRenaissance.UI.Tablet.SystemShell;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 媒姬百科的手感层：页面进入动画、悬停/按下过渡、带惯性的滚轮滚动、会淡出的细滚动条、
    /// 顶部/底部的渐变遮边、文章阅读进度条、回到顶部按钮，以及返回时恢复滚动位置。
    /// 全部是 UI Toolkit 内联样式过渡，不依赖 USS；<see cref="Motion"/> 关掉后一切立即到位（截图评审用）。
    /// </summary>
    public static partial class TabletWikiAppUI
    {
        /// <summary>是否启用动效。截图批处理会关掉它，避免截到过渡中间帧。</summary>
        public static bool Motion = true;

        private static View s_shown;                 // 上一次渲染的页面：同一页重渲染（换字号、点关注）时不重播进入动画、不回到顶部
        private static string s_lastSelTab;
        private static VisualElement s_topFade, s_progress, s_toTop;
        private static bool s_toTopShown, s_topFadeShown;

        // ---------------- 过渡工具 ----------------

        private static void Ease(VisualElement e, int ms, params string[] props)
        {
            if (e == null || !Motion) return;
            var names = new List<StylePropertyName>(props.Length);
            var durs = new List<TimeValue>(props.Length);
            var fns = new List<EasingFunction>(props.Length);
            foreach (var p in props)
            {
                names.Add(new StylePropertyName(p));
                durs.Add(new TimeValue(ms, TimeUnit.Millisecond));
                fns.Add(new EasingFunction(EasingMode.EaseOutCubic));
            }
            e.style.transitionProperty = names;
            e.style.transitionDuration = durs;
            e.style.transitionTimingFunction = fns;
        }

        /// <summary>按下时轻微缩小，松开回弹。</summary>
        private static void Pressable(VisualElement e, float pressed = 0.975f)
        {
            if (e == null || !Motion) return;
            e.RegisterCallback<PointerDownEvent>(_ => e.style.scale = new Scale(new Vector3(pressed, pressed, 1f)));
            e.RegisterCallback<PointerUpEvent>(_ => e.style.scale = new Scale(Vector3.one));
            e.RegisterCallback<PointerLeaveEvent>(_ => e.style.scale = new Scale(Vector3.one));
        }

        /// <summary>从略小、略偏的位置淡入到位（弹层、选中胶囊、页面内容）。</summary>
        private static void PopIn(VisualElement e, float fromY = 0f, float fromScale = 1f, int ms = 200)
        {
            if (e == null || !Motion) return;
            e.style.opacity = 0f;
            if (fromY != 0f) e.style.translate = new Translate(0, fromY);
            if (fromScale != 1f) e.style.scale = new Scale(new Vector3(fromScale, fromScale, 1f));
            e.schedule.Execute(() =>
            {
                Ease(e, ms, "opacity", "translate", "scale");
                e.style.opacity = 1f;
                e.style.translate = new Translate(0, 0);
                e.style.scale = new Scale(Vector3.one);
            }).StartingIn(16);
        }

        // ---------------- 主滚动区的外壳 ----------------

        /// <summary>把主滚动区包进一个容器，叠上顶部遮边、阅读进度条和回到顶部按钮。</summary>
        private static VisualElement WrapMain(ScrollView sv)
        {
            var wrap = new VisualElement { name = "wiki-main-wrap" };
            wrap.style.flexGrow = 1; wrap.style.flexShrink = 1; wrap.style.flexDirection = FlexDirection.Column;
            wrap.Add(sv);

            // 触屏拖动：带惯性和回弹
            sv.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;
            sv.scrollDecelerationRate = 0.12f;
            sv.elasticity = 0.08f;

            // 顶部遮边：内容滚到工具栏下面时出现一条由底色渐隐的边，不再是生硬的一刀切
            s_topFade = Gradient(Paper, new Color(Paper.r, Paper.g, Paper.b, 0f), 22f);
            s_topFade.style.top = 0; s_topFade.style.opacity = 0f; s_topFadeShown = false;
            Ease(s_topFade, 180, "opacity");
            wrap.Add(s_topFade);

            // 底部遮边：内容在底栏上方渐隐
            var bottomFade = Gradient(new Color(Paper.r, Paper.g, Paper.b, 0f), Paper, 18f);
            bottomFade.style.bottom = 0;
            wrap.Add(bottomFade);

            // 阅读进度条（只在词条页显示）
            s_progress = new VisualElement { name = "wiki-progress", pickingMode = PickingMode.Ignore };
            s_progress.style.position = Position.Absolute; s_progress.style.left = 0; s_progress.style.top = 0;
            s_progress.style.height = 3; s_progress.style.width = 0;
            s_progress.style.backgroundColor = Accent;
            s_progress.style.borderTopRightRadius = 2; s_progress.style.borderBottomRightRadius = 2;
            wrap.Add(s_progress);

            // 回到顶部
            var top = new VisualElement { name = "wiki-to-top" };
            top.style.position = Position.Absolute; top.style.right = 26; top.style.bottom = 22;
            top.style.width = 46; top.style.height = 46; Radius(top, 23);
            top.style.backgroundColor = Color.white; Border(top, 1, Hairline);
            top.style.alignItems = Align.Center; top.style.justifyContent = Justify.Center;
            var arrow = new TabletIconElement(TabletIconKind.Back, 17, Accent) { pickingMode = PickingMode.Ignore };
            arrow.style.rotate = new Rotate(new Angle(90f, AngleUnit.Degree));
            top.Add(arrow);
            top.style.opacity = 0f; top.style.display = DisplayStyle.None; s_toTopShown = false;
            top.pickingMode = PickingMode.Position;
            Color idle = Color.white, hot = Tint;
            top.RegisterCallback<PointerEnterEvent>(_ => { top.style.backgroundColor = hot; UiCursor.ShowHand(); });
            top.RegisterCallback<PointerLeaveEvent>(_ => { top.style.backgroundColor = idle; UiCursor.ShowArrow(); });
            top.RegisterCallback<ClickEvent>(_ => ScrollTo(0f));
            Ease(top, 180, "opacity", "background-color", "scale", "translate");
            Pressable(top, 0.92f);
            s_toTop = top;
            wrap.Add(top);

            sv.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            if (sv.verticalScroller != null) sv.verticalScroller.valueChanged += _ => UpdateScrollChrome();
            return wrap;
        }

        /// <summary>竖向渐变条（顶点色网格，不用贴图）。</summary>
        private static VisualElement Gradient(Color topColor, Color bottomColor, float height)
        {
            var g = new VisualElement { pickingMode = PickingMode.Ignore };
            g.style.position = Position.Absolute; g.style.left = 0; g.style.right = 12; g.style.height = height;
            g.generateVisualContent += ctx =>
            {
                var r = ctx.visualElement.contentRect;
                if (r.width <= 0 || r.height <= 0) return;
                var mesh = ctx.Allocate(4, 6);
                Color32 t = topColor, b = bottomColor;
                mesh.SetNextVertex(new Vertex { position = new Vector3(0, 0, Vertex.nearZ), tint = t });
                mesh.SetNextVertex(new Vertex { position = new Vector3(r.width, 0, Vertex.nearZ), tint = t });
                mesh.SetNextVertex(new Vertex { position = new Vector3(r.width, r.height, Vertex.nearZ), tint = b });
                mesh.SetNextVertex(new Vertex { position = new Vector3(0, r.height, Vertex.nearZ), tint = b });
                mesh.SetNextIndex(0); mesh.SetNextIndex(1); mesh.SetNextIndex(2);
                mesh.SetNextIndex(0); mesh.SetNextIndex(2); mesh.SetNextIndex(3);
            };
            return g;
        }

        private static void UpdateScrollChrome()
        {
            if (!Alive()) return;
            float y = s_main.scrollOffset.y;
            float max = s_main.verticalScroller != null ? Mathf.Max(0f, s_main.verticalScroller.highValue) : 0f;

            bool fade = y > 6f;
            if (s_topFade != null && fade != s_topFadeShown) { s_topFadeShown = fade; s_topFade.style.opacity = fade ? 1f : 0f; }

            if (s_progress != null)
            {
                bool article = s_cur != null && s_cur.Kind == Kind.Article && max > 40f;
                s_progress.style.width = article ? new Length(Mathf.Clamp01(y / max) * 100f, LengthUnit.Percent) : new Length(0);
            }

            bool showTop = y > 520f;
            if (s_toTop != null && showTop != s_toTopShown)
            {
                s_toTopShown = showTop;
                var b = s_toTop;
                if (showTop)
                {
                    b.style.display = DisplayStyle.Flex;
                    b.style.translate = new Translate(0, 10);
                    b.schedule.Execute(() => { if (s_toTopShown) { b.style.opacity = 1f; b.style.translate = new Translate(0, 0); } }).StartingIn(16);
                }
                else
                {
                    b.style.opacity = 0f;
                    b.schedule.Execute(() => { if (!s_toTopShown) b.style.display = DisplayStyle.None; }).StartingIn(Motion ? 200 : 0);
                }
            }
            NudgeScroller();
        }

        // ---------------- 带惯性的滚轮 ----------------

        private static float s_scrollTarget, s_scrollLast;
        private static bool s_scrolling;
        private static IVisualElementScheduledItem s_scrollTick;

        private static void OnWheel(WheelEvent e)
        {
            if (!Motion || !Alive() || s_main.verticalScroller == null) return;
            float max = Mathf.Max(0f, s_main.verticalScroller.highValue);
            if (max <= 0f) return;
            float cur = s_main.scrollOffset.y;
            if (!s_scrolling) s_scrollTarget = cur;
            s_scrollTarget = Mathf.Clamp(s_scrollTarget + Mathf.Clamp(e.delta.y, -6f, 6f) * 36f, 0f, max);
            StartScrollAnim();
            e.StopImmediatePropagation();
        }

        /// <summary>平滑滚到指定位置。</summary>
        private static void ScrollTo(float y)
        {
            if (!Alive()) return;
            float max = s_main.verticalScroller != null ? Mathf.Max(0f, s_main.verticalScroller.highValue) : 0f;
            y = Mathf.Clamp(y, 0f, max);
            if (!Motion) { s_main.scrollOffset = new Vector2(0, y); return; }
            s_scrollTarget = y;
            StartScrollAnim();
        }

        private static void StartScrollAnim()
        {
            s_scrollLast = s_main.scrollOffset.y;
            if (s_scrolling) return;
            s_scrolling = true;
            s_scrollTick = s_main.schedule.Execute(ScrollStep).Every(8);
        }

        private static void StopScrollAnim()
        {
            s_scrolling = false;
            if (s_scrollTick != null) { s_scrollTick.Pause(); s_scrollTick = null; }
        }

        private static void ScrollStep()
        {
            if (!Alive()) { StopScrollAnim(); return; }
            float cur = s_main.scrollOffset.y;
            // 用户拖了滚动条或用手指拖动：让出控制权
            if (Mathf.Abs(cur - s_scrollLast) > 2f) { StopScrollAnim(); return; }
            float dt = Mathf.Clamp(Time.unscaledDeltaTime, 0.004f, 0.05f);
            float next = Mathf.Lerp(cur, s_scrollTarget, 1f - Mathf.Exp(-dt * 15f));
            if (Mathf.Abs(s_scrollTarget - next) < 0.6f) next = s_scrollTarget;
            s_main.scrollOffset = new Vector2(0, next);
            s_scrollLast = s_main.scrollOffset.y;
            if (Mathf.Approximately(next, s_scrollTarget) || Mathf.Abs(s_scrollLast - next) > 1f) StopScrollAnim();
        }

        // ---------------- 滚动条 ----------------

        private static VisualElement s_dragger;
        private static bool s_scrollerHover;
        private static IVisualElementScheduledItem s_scrollerFade;
        private static readonly Color DragRest = new Color(0.10f, 0.36f, 0.32f, 0.16f);
        private static readonly Color DragActive = new Color(0.10f, 0.36f, 0.32f, 0.42f);
        private static readonly Color DragHot = new Color(0.12f, 0.52f, 0.46f, 0.80f);

        /// <summary>滑块：平时是一条很淡的细线，滚动时加深，指针靠近时变宽变亮，停下约一秒后淡回去。</summary>
        private static void PolishDragger(Scroller scroller, VisualElement dragger)
        {
            s_dragger = dragger; s_scrollerHover = false;
            scroller.style.width = 12;
            dragger.style.backgroundColor = DragRest;
            dragger.style.width = 4; dragger.style.marginLeft = 5; dragger.style.minHeight = 44;
            Radius(dragger, 4);
            Ease(dragger, 160, "background-color", "width", "margin-left");
            scroller.RegisterCallback<PointerEnterEvent>(_ =>
            {
                s_scrollerHover = true;
                dragger.style.width = 8; dragger.style.marginLeft = 2; dragger.style.backgroundColor = DragHot;
            });
            scroller.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                s_scrollerHover = false;
                dragger.style.width = 4; dragger.style.marginLeft = 5; dragger.style.backgroundColor = DragActive;
                QueueScrollerFade();
            });
        }

        private static void NudgeScroller()
        {
            if (s_dragger == null || s_scrollerHover) return;
            s_dragger.style.backgroundColor = DragActive;
            QueueScrollerFade();
        }

        private static void QueueScrollerFade()
        {
            if (s_dragger == null) return;
            if (s_scrollerFade != null) s_scrollerFade.Pause();
            var d = s_dragger;
            s_scrollerFade = d.schedule.Execute(() => { if (!s_scrollerHover) d.style.backgroundColor = DragRest; }).StartingIn(900);
        }

        // ---------------- 渲染前后 ----------------

        /// <summary>渲染完成后：恢复滚动位置（返回上一页时），播放进入动画，刷新进度条等。</summary>
        private static void AfterRender(VisualElement host, float restoreY, bool entering)
        {
            if (restoreY > 1f)
            {
                int tries = 0;
                EventCallback<GeometryChangedEvent> cb = null;
                cb = _ =>
                {
                    if (!Alive()) return;
                    float max = s_main.verticalScroller != null ? s_main.verticalScroller.highValue : 0f;
                    tries++;
                    if (max >= restoreY - 1f || tries > 40)
                    {
                        s_main.scrollOffset = new Vector2(0, Mathf.Min(restoreY, Mathf.Max(0f, max)));
                        s_main.contentContainer.UnregisterCallback(cb);
                        UpdateScrollChrome();
                    }
                };
                s_main.contentContainer.RegisterCallback(cb);
            }
            if (entering)
            {
                host.usageHints = UsageHints.DynamicTransform;
                PopIn(host, 12f, 1f, 220);
            }
            host.schedule.Execute(UpdateScrollChrome).StartingIn(0);
        }

        /// <summary>长列表分批建：先建一屏，其余每帧补一批，打开大分类时不卡一下。</summary>
        private static void AddRowsInBatches<T>(VisualElement parent, IReadOnlyList<T> items, Action<VisualElement, T, int> addOne, int first = 16, int batch = 14)
        {
            int i = 0;
            void Run(int count)
            {
                for (int n = 0; n < count && i < items.Count; n++, i++) addOne(parent, items[i], i);
            }
            Run(Motion ? first : items.Count);
            if (i >= items.Count) return;
            IVisualElementScheduledItem tick = null;
            tick = parent.schedule.Execute(() =>
            {
                if (parent.panel == null) { tick?.Pause(); return; }
                Run(batch);
                if (i >= items.Count) tick?.Pause();
            }).Every(16);
        }

        // ---------------- 列表缓存（关注 / 足迹） ----------------

        private static readonly Dictionary<string, List<string>> s_listCache = new Dictionary<string, List<string>>();
    }
}
