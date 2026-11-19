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
    /// 平板应用：媒姬百科。参照维基百科的版式：左侧导航、顶部搜索、词条正文 + 右侧信息框、目录、参见、分类栏；
    /// 合并了原「术语图鉴」与 VN 的角色/列车/章节/场景/CG/音乐图鉴；登录小米账号后可关注词条并保存阅读足迹。
    /// </summary>
    public static partial class TabletWikiAppUI
    {
        private const string AppId = "wiki";
        private const string AppName = "媒姬百科";
        // 配色：暖纸底 + 深墨绿（与音乐终端同系）+ 琥珀点缀；链接用深青绿，未收录用砖红
        private static readonly Color Paper = new Color(0.949f, 0.957f, 0.953f, 1f);
        private static readonly Color Tint = new Color(0.875f, 0.945f, 0.925f, 1f);        // 浅青绿底（浅色模式下的强调块，不用深色）
        private static readonly Color TintDeep = new Color(0.80f, 0.91f, 0.88f, 1f);
        private static readonly Color Accent = new Color(0.12f, 0.52f, 0.46f, 1f);
        private static readonly Color Ink = new Color(0.125f, 0.153f, 0.137f, 1f);
        private static readonly Color Amber = new Color(0.83f, 0.66f, 0.26f, 1f);
        private static readonly Color WikiBlue = new Color(0.07f, 0.41f, 0.36f, 1f);
        private static readonly Color Hairline = new Color(0f, 0f, 0f, 0.06f);
        private static readonly Color InfoHead = new Color(0.93f, 0.92f, 0.89f, 1f);
        private static readonly Color InfoBg = new Color(0.975f, 0.97f, 0.955f, 1f);
        private static readonly Color Notice = new Color(0.99f, 0.96f, 0.86f, 1f);
        private static readonly Color NoticeLine = new Color(0.90f, 0.78f, 0.40f, 1f);
        private static readonly Color RedLink = new Color(0.72f, 0.22f, 0.17f, 1f);

        private enum Kind { Home, Category, Article, Search, SearchHome, Watch, History, More }
        private sealed class View { public Kind Kind; public string Arg; public float Scroll; }

        private static readonly List<View> s_back = new List<View>();
        private static readonly List<View> s_fwd = new List<View>();
        private static readonly List<string> s_session = new List<string>();
        private static HashSet<string> s_lockedArticles;
        private static View s_cur;
        private static string s_pending;

        private static VisualElement s_root, s_bottom, s_headLeft, s_searchBox, s_account, s_popup, s_navBack, s_navFwd;
        private static int s_bodySize = 15;
        private static List<WikiSection> s_tocSections;
        private static List<VisualElement> s_tocHeaders;
        private static ScrollView s_main;
        private static TextField s_search;

        // ============ 外部入口 ============

        /// <summary>打开某个词条（id 或标题）。应用已在前台则直接跳转，否则在下次构建时打开。</summary>
        public static void OpenArticle(string idOrTitle)
        {
            if (string.IsNullOrEmpty(idOrTitle)) return;
            s_fromVn = false; s_freshRoot = false; s_exitToVn = null;
            s_pending = idOrTitle;
            if (Alive()) ConsumePending();
        }

        private static bool s_fromVn, s_freshRoot;
        private static Action s_exitToVn;

        /// <summary>从 VN 术语小卡进入：词条作为新的根页（没有更早的历史），顶部出现「返回剧情」，退出即回到 VN。</summary>
        public static void OpenFromVN(string idOrTitle, Action exitToVn)
        {
            if (string.IsNullOrEmpty(idOrTitle)) return;
            s_pending = idOrTitle;
            s_fromVn = true; s_freshRoot = true; s_exitToVn = exitToVn;
            if (Alive()) ConsumePending();
        }

        /// <summary>离开百科（回桌面或回 VN）时清掉 VN 来源标记。</summary>
        public static void ClearVnOrigin() { s_fromVn = false; s_freshRoot = false; s_exitToVn = null; }

        /// <summary>系统返回键 / 手势：先收浮层，再在百科内后退。已无可退时返回 false，由外壳退出应用。</summary>
        public static bool TryGoBack()
        {
            if (!Alive()) return false;
            if (s_popup != null) { ClosePopup(); return true; }
            if (s_back.Count == 0) return false;
            GoBack();
            return true;
        }

        private static bool Alive() => s_root != null && s_root.panel != null && s_main != null;

        private static void ConsumePending()
        {
            string p = s_pending;
            s_pending = null;
            var a = TabletWikiData.Get(p);
            if (a == null) return;
            if (s_freshRoot)
            {
                s_freshRoot = false;
                s_back.Clear(); s_fwd.Clear();
                s_cur = new View { Kind = Kind.Article, Arg = a.Id };
                RecordRead(a.Id);
                ClosePopup();
                if (s_search != null) s_search.SetValueWithoutNotify(string.Empty);
                if (Alive()) Render();
                return;
            }
            Go(new View { Kind = Kind.Article, Arg = a.Id });
        }

        // ============ 构建 ============

        public static void BuildWikiApp(VisualElement win)
        {
            win.Clear();
            win.style.paddingLeft = 0; win.style.paddingRight = 0; win.style.paddingTop = 0; win.style.paddingBottom = 0;
            s_popup = null; s_shown = null; s_listCache.Clear(); StopScrollAnim();
            var root = new VisualElement { name = "wiki-root" };
            root.style.flexGrow = 1;
            root.style.flexDirection = FlexDirection.Column;
            root.style.backgroundColor = Paper;
            win.Add(root);
            s_root = root;

            BuildToolbar(root);

            s_main = new ScrollView { name = "wiki-main" };
            s_main.style.flexGrow = 1; s_main.style.flexShrink = 1;
            SlimScroll(s_main);
            root.Add(WrapMain(s_main));

            s_bottom = new VisualElement { name = "wiki-bottom" };
            s_bottom.style.flexShrink = 0; s_bottom.style.flexDirection = FlexDirection.Row;
            s_bottom.style.height = 90; s_bottom.style.paddingBottom = 14;
            s_bottom.style.backgroundColor = Color.white;
            s_bottom.style.borderTopWidth = 1; s_bottom.style.borderTopColor = Hairline;
            s_bottom.style.paddingLeft = 40; s_bottom.style.paddingRight = 40;
            root.Add(s_bottom);

            TabletXiaomiSignIn.Changed -= OnAccountChanged;
            AccountService.Changed -= OnAccountChanged;
            TabletXiaomiSignIn.Changed += OnAccountChanged;
            AccountService.Changed += OnAccountChanged;
            root.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                TabletXiaomiSignIn.Changed -= OnAccountChanged;
                AccountService.Changed -= OnAccountChanged;
            });

            RefreshAccount();
            s_lockedArticles = LockedArticles();
            if (s_cur == null) s_cur = new View { Kind = Kind.Home };
            if (!string.IsNullOrEmpty(s_pending)) ConsumePending(); else Render();
        }

        private static HashSet<string> LockedArticles()
        {
            var locked = new HashSet<string>();
            foreach (var article in TabletWikiData.All)
                if (!article.Unlocked) locked.Add(article.Id);
            return locked;
        }

        internal static void RefreshStoryProgress()
        {
            if (!Alive()) return;
            var locked = LockedArticles();
            bool unlockChanged = !s_lockedArticles.SetEquals(locked);
            if (unlockChanged) s_lockedArticles = locked;
            bool dynamicArticle = s_cur.Kind == Kind.Article && TabletWikiData.Get(s_cur.Arg)?.DynamicSections != null;
            if (unlockChanged || dynamicArticle) Render();
        }

        private static void OnAccountChanged()
        {
            if (!Alive()) return;
            if (Signed) MergeLocalWatch(WatchKey());
            RefreshAccount();
            Render();
        }

        // ============ 导航 ============

        private static void Go(View v, bool push = true)
        {
            if (v == null) return;
            if (s_cur != null && Alive()) s_cur.Scroll = s_main.scrollOffset.y;
            if (push && s_cur != null && !(s_cur.Kind == v.Kind && s_cur.Arg == v.Arg))
            {
                s_back.Add(s_cur);
                if (s_back.Count > 60) s_back.RemoveAt(0);
                s_fwd.Clear();
            }
            s_cur = v;
            if (v.Kind == Kind.Article) RecordRead(v.Arg);
            ClosePopup();
            if (s_search != null && v.Kind != Kind.Search) s_search.SetValueWithoutNotify(string.Empty);
            if (Alive()) Render();
        }

        private static void GoBack()
        {
            if (s_back.Count == 0) return;
            if (Alive()) s_cur.Scroll = s_main.scrollOffset.y;
            s_fwd.Add(s_cur);
            s_cur = s_back[s_back.Count - 1];
            s_back.RemoveAt(s_back.Count - 1);
            Render();
        }

        private static void GoForward()
        {
            if (s_fwd.Count == 0) return;
            if (Alive()) s_cur.Scroll = s_main.scrollOffset.y;
            s_back.Add(s_cur);
            s_cur = s_fwd[s_fwd.Count - 1];
            s_fwd.RemoveAt(s_fwd.Count - 1);
            Render();
        }

        private static void Render()
        {
            if (!Alive()) return;
            ClosePopup();
            // 同一页重渲染（换字号、点关注）保持当前位置；后退/前进恢复离开时的位置
            bool same = s_shown != null && s_shown.Kind == s_cur.Kind && s_shown.Arg == s_cur.Arg;
            float keepY = same ? s_main.scrollOffset.y : s_cur.Scroll;
            StopScrollAnim();
            s_main.Clear();
            s_main.scrollOffset = Vector2.zero;
            var host = new VisualElement();
            host.style.paddingLeft = 28; host.style.paddingRight = 28; host.style.paddingTop = 8; host.style.paddingBottom = 36;
            host.style.maxWidth = 1180; host.style.width = new Length(100, LengthUnit.Percent); host.style.alignSelf = Align.Center;
            s_tocSections = null; s_tocHeaders = null;
            s_main.Add(host);
            switch (s_cur.Kind)
            {
                case Kind.Home: RenderHome(host); break;
                case Kind.Category: RenderCategory(host, s_cur.Arg); break;
                case Kind.Article:
                    var a = TabletWikiData.Get(s_cur.Arg);
                    if (a == null) { s_cur = new View { Kind = Kind.Home }; RenderHome(host); }
                    else RenderArticle(host, a);
                    break;
                case Kind.Search: RenderSearch(host, s_cur.Arg); break;
                case Kind.SearchHome: RenderSearchHome(host); break;
                case Kind.More: RenderMore(host); break;
                case Kind.Watch: RenderWatch(host); break;
                case Kind.History: RenderHistory(host); break;
            }
            RefreshHeader();
            RefreshBottom();
            s_shown = s_cur;
            AfterRender(host, keepY, !same);
        }

        // ============ 小部件 ============

        private static void Radius(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
        }

        private static void Border(VisualElement e, float w, Color c)
        {
            e.style.borderTopWidth = w; e.style.borderBottomWidth = w; e.style.borderLeftWidth = w; e.style.borderRightWidth = w;
            e.style.borderTopColor = c; e.style.borderBottomColor = c; e.style.borderLeftColor = c; e.style.borderRightColor = c;
        }

        private static void Pad(VisualElement e, float l, float t, float r, float b)
        {
            e.style.paddingLeft = l; e.style.paddingTop = t; e.style.paddingRight = r; e.style.paddingBottom = b;
        }

        private static Label Text(string s, int size, Color color, bool bold = false)
        {
            var l = new Label(s);
            l.style.fontSize = size; l.style.color = color;
            l.style.unityFontDefinition = bold ? FdBold() : Fd();
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginLeft = 0; l.style.marginRight = 0; l.style.marginTop = 0; l.style.marginBottom = 0;
            l.style.paddingLeft = 0; l.style.paddingRight = 0; l.style.paddingTop = 0; l.style.paddingBottom = 0;
            return l;
        }

        /// <summary>带站内链接的富文本标签；点链接跳转，悬停变手形。</summary>
        private static Label Rich(string markup, int size, Color color, string selfId = null, bool autoLink = false)
        {
            var l = Text(TabletWikiData.ToRich(markup, selfId, autoLink), size, color);
            l.enableRichText = true;
            l.RegisterCallback<UnityEngine.UIElements.Experimental.PointerUpLinkTagEvent>(ev => Go(new View { Kind = Kind.Article, Arg = ev.linkID }));
            l.RegisterCallback<UnityEngine.UIElements.Experimental.PointerOverLinkTagEvent>(_ => UiCursor.ShowHand());
            l.RegisterCallback<UnityEngine.UIElements.Experimental.PointerOutLinkTagEvent>(_ => UiCursor.ShowArrow());
            l.RegisterCallback<PointerLeaveEvent>(_ => UiCursor.ShowArrow());
            return l;
        }

        private static VisualElement Card()
        {
            var c = new VisualElement();
            c.style.backgroundColor = SurfaceCard;
            Radius(c, 22); Border(c, 1, Hairline); SoftShadow(c, 22);
            Pad(c, 26, 22, 26, 24);
            c.style.marginBottom = 16;
            return c;
        }

        private static VisualElement Rule(float marginTop = 6, float marginBottom = 10)
        {
            var r = new VisualElement();
            r.style.height = 1; r.style.backgroundColor = Hairline;
            r.style.marginTop = marginTop; r.style.marginBottom = marginBottom;
            return r;
        }

        /// <summary>琥珀竖条 + 标题，统一的小节标题。</summary>
        private static VisualElement SectionTitle(string t, int size = 18)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            row.style.marginBottom = 12;
            var bar = new VisualElement(); bar.style.width = 4; bar.style.height = size; Radius(bar, 2);
            bar.style.backgroundColor = Amber; bar.style.marginRight = 10;
            row.Add(bar);
            row.Add(Text(t, size, Ink, true));
            return row;
        }

        /// <summary>细滚动条：隐藏上下箭头与轨道，只留一条圆角琥珀灰滑块（参照音乐应用）。</summary>
        private static void SlimScroll(ScrollView sv)
        {
            if (sv == null) return;
            sv.mode = ScrollViewMode.Vertical;
            sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            sv.verticalScrollerVisibility = ScrollerVisibility.Auto;
            if (sv.horizontalScroller != null) { sv.horizontalScroller.style.display = DisplayStyle.None; sv.horizontalScroller.style.height = 0; }
            var scroller = sv.verticalScroller;
            if (scroller == null) return;
            var lo = scroller.Q(className: "unity-scroller__low-button"); if (lo != null) lo.style.display = DisplayStyle.None;
            var hi = scroller.Q(className: "unity-scroller__high-button"); if (hi != null) hi.style.display = DisplayStyle.None;
            scroller.style.width = 10; scroller.style.backgroundColor = Color.clear;
            scroller.style.borderLeftWidth = 0; scroller.style.borderRightWidth = 0; scroller.style.borderTopWidth = 0; scroller.style.borderBottomWidth = 0;
            var slider = scroller.slider;
            if (slider == null) return;
            slider.style.backgroundColor = Color.clear; slider.style.marginLeft = 0; slider.style.marginRight = 0; slider.style.marginTop = 4; slider.style.marginBottom = 4;
            slider.style.borderLeftWidth = 0; slider.style.borderRightWidth = 0; slider.style.borderTopWidth = 0; slider.style.borderBottomWidth = 0;
            var tracker = slider.Q(className: "unity-base-slider__tracker");
            if (tracker != null)
            {
                tracker.style.backgroundColor = Color.clear;
                tracker.style.borderLeftWidth = 0; tracker.style.borderRightWidth = 0; tracker.style.borderTopWidth = 0; tracker.style.borderBottomWidth = 0;
            }
            var dragger = slider.Q(className: "unity-base-slider__dragger");
            if (dragger != null)
            {
                dragger.style.borderLeftWidth = 0; dragger.style.borderRightWidth = 0; dragger.style.borderTopWidth = 0; dragger.style.borderBottomWidth = 0;
                PolishDragger(scroller, dragger);
            }
        }

        private static readonly Dictionary<string, TabletIconKind> s_catIcons = new Dictionary<string, TabletIconKind>
        {
            { TabletWikiData.CatPeople, TabletIconKind.Person }, { TabletWikiData.CatVehicle, TabletIconKind.Train },
            { TabletWikiData.CatPlace, TabletIconKind.Map }, { TabletWikiData.CatWorld, TabletIconKind.Globe },
            { TabletWikiData.CatTerm, TabletIconKind.Ruler }, { TabletWikiData.CatStory, TabletIconKind.Bookmark },
            { TabletWikiData.CatArchive, TabletIconKind.Image },
        };

        private static VisualElement Chip(string text, Color fg, Action click, bool soft = true)
        {
            var c = Text(text, 12, fg, true);
            Pad(c, 13, 6, 13, 6); Radius(c, 15);
            c.style.backgroundColor = soft ? Paper : new Color(1f, 1f, 1f, 0.10f);
            c.style.marginRight = 8; c.style.marginBottom = 8;
            Clickable(c, click, soft ? new Color(0.90f, 0.88f, 0.83f, 1f) : new Color(1f, 1f, 1f, 0.20f));
            return c;
        }

        private static VisualElement Clickable(VisualElement e, Action click, Color? hover = null)
        {
            e.pickingMode = PickingMode.Position;
            StyleColor normal = e.style.backgroundColor;   // 离开时回到原来的内联底色（可能是未设置）
            Color hv = hover ?? new Color(0f, 0f, 0f, 0.05f);
            e.RegisterCallback<PointerEnterEvent>(_ => { e.style.backgroundColor = hv; UiCursor.ShowHand(); });
            e.RegisterCallback<PointerLeaveEvent>(_ => { e.style.backgroundColor = normal; UiCursor.ShowArrow(); });
            e.RegisterCallback<ClickEvent>(_ => click());
            Ease(e, 140, "background-color", "scale");
            Pressable(e);
            return e;
        }

        private static Button IconBtn(TabletIconKind kind, string tip, Action click, float size = 38)
        {
            var b = TabletAppBar.IconButton(kind, 18, Ink, new Color(0f, 0f, 0f, 0.06f), new Color(0f, 0f, 0f, 0.12f), click);
            b.style.width = size; b.style.height = size;
            b.tooltip = tip;
            return b;
        }

        private static void SetNavState(VisualElement b, bool enabled)
        {
            if (b == null) return;
            b.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ============ 顶栏 / 账号 / 搜索 ============

        private static void BuildToolbar(VisualElement root)
        {
            var bar = new VisualElement { name = "wiki-toolbar" };
            bar.style.height = 72; bar.style.flexShrink = 0;
            bar.style.flexDirection = FlexDirection.Row; bar.style.alignItems = Align.Center;
            bar.style.backgroundColor = Paper;
            Pad(bar, 24, 0, 24, 0);
            root.Add(bar);

            // 左侧：后退（有历史时）与首页品牌，随页面刷新；应用没有顶部退出键，退出走手势条
            s_headLeft = new VisualElement { name = "wiki-head-left" };
            s_headLeft.style.flexDirection = FlexDirection.Row; s_headLeft.style.alignItems = Align.Center; s_headLeft.style.flexShrink = 0;
            bar.Add(s_headLeft);
            s_navBack = IconBtn(TabletIconKind.Back, "后退", GoBack); s_navBack.name = "wiki-nav-back";
            s_navFwd = null;

            // 搜索框
            var box = new VisualElement { name = "wiki-search-box" };
            box.style.flexGrow = 1; box.style.maxWidth = 560; box.style.height = 44; box.style.marginLeft = 16; s_searchBox = box;
            box.style.flexDirection = FlexDirection.Row; box.style.alignItems = Align.Center;
            box.style.backgroundColor = Color.white;
            Radius(box, 22); Border(box, 1, Hairline);
            Pad(box, 16, 0, 16, 0);
            box.Add(new TabletIconElement(TabletIconKind.Search, 17, InkTertiary) { pickingMode = PickingMode.Ignore });

            var field = new TextField { name = "wiki-search" };
            s_search = field;
            field.style.flexGrow = 1; field.style.marginLeft = 8; field.style.marginTop = 0; field.style.marginBottom = 0;
            field.style.fontSize = 14; field.style.color = Ink; field.style.unityFontDefinition = Fd();
            var input = field.Q("unity-text-input");
            if (input != null)
            {
                input.style.backgroundColor = Color.clear;
                input.style.borderTopWidth = 0; input.style.borderBottomWidth = 0; input.style.borderLeftWidth = 0; input.style.borderRightWidth = 0;
                input.style.paddingTop = 0; input.style.paddingBottom = 0;
            }
            field.textEdition.placeholder = "搜索人物、车辆、车站、术语";
            field.textEdition.hidePlaceholderOnFocus = true;
            // 联想做 90ms 防抖：连续打字时不每个字都重建一次弹层
            IVisualElementScheduledItem suggest = null;
            field.RegisterValueChangedCallback(e =>
            {
                string v = e.newValue;
                suggest?.Pause();
                if (!Motion || string.IsNullOrWhiteSpace(v)) { ShowSuggestions(v); return; }
                suggest = field.schedule.Execute(() => ShowSuggestions(field.value)).StartingIn(90);
            });
            Ease(box, 160, "border-left-color", "border-right-color", "border-top-color", "border-bottom-color");
            field.RegisterCallback<FocusInEvent>(_ => Border(box, 1, Accent));
            field.RegisterCallback<FocusOutEvent>(_ => Border(box, 1, Hairline));
            field.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    string q = field.value.Trim();
                    if (q.Length > 0) { Go(new View { Kind = Kind.Search, Arg = q }); }
                    e.StopPropagation();
                }
                else if (e.keyCode == KeyCode.Escape) { field.value = string.Empty; ClosePopup(); }
            }, TrickleDown.TrickleDown);
            box.Add(field);
            bar.Add(box);

            var spacer = new VisualElement(); spacer.style.flexGrow = 1; spacer.pickingMode = PickingMode.Ignore;
            bar.Add(spacer);

            s_account = null; // 小米账号入口已收进「更多」页
        }

        private static bool Signed => AccountService.IsRegistered && TabletXiaomiSignIn.IsSignedIn(AppId);

        private static void RequestSignIn(Action<bool> after = null)
        {
            TabletXiaomiSignIn.Request(s_root, AppId, AppName, WikiBlue, ok =>
            {
                if (Alive()) { RefreshAccount(); if (ok) Render(); }
                after?.Invoke(ok);
            });
        }

        private static void RefreshAccount()
        {
            if (s_account == null) return;
            s_account.Clear();
            if (Signed)
            {
                var p = AccountService.Profile;
                var chip = new VisualElement { name = "wiki-account-chip" };
                chip.style.flexDirection = FlexDirection.Row; chip.style.alignItems = Align.Center;
                chip.style.height = 44; Pad(chip, 5, 0, 16, 0);
                Radius(chip, 22);
                chip.style.backgroundColor = Color.white; Border(chip, 1, Hairline);
                var av = TabletAvatar.Build(p, 34); av.pickingMode = PickingMode.Ignore; av.style.marginRight = 8;
                chip.Add(av);
                chip.Add(Text(AccountService.DisplayName, 13, InkPrimary, true));
                chip.RegisterCallback<ClickEvent>(_ => ShowAccountMenu());
                chip.RegisterCallback<PointerEnterEvent>(_ => UiCursor.ShowHand());
                chip.RegisterCallback<PointerLeaveEvent>(_ => UiCursor.ShowArrow());
                s_account.Add(chip);
            }
            else
            {
                var btn = new Button(() => RequestSignIn()) { text = "小米账号登录", name = "wiki-login" };
                btn.style.height = 42; Pad(btn, 20, 0, 20, 0);
                btn.style.marginLeft = 0; btn.style.marginRight = 0; btn.style.marginTop = 0; btn.style.marginBottom = 0;
                btn.style.borderTopWidth = 0; btn.style.borderBottomWidth = 0; btn.style.borderLeftWidth = 0; btn.style.borderRightWidth = 0;
                Radius(btn, 21);
                btn.style.backgroundColor = new Color(1f, 0.41f, 0f, 1f);
                btn.style.color = Color.white; btn.style.fontSize = 13; btn.style.unityFontDefinition = FdBold();
                s_account.Add(btn);
            }
        }

        // 浮层（搜索建议 / 账号菜单）：整屏透明遮罩，点空白处收起
        private static VisualElement OpenPopup(float top, float right, float left, float width)
        {
            ClosePopup();
            var shade = new VisualElement { name = "wiki-popup" };
            shade.style.position = Position.Absolute;
            shade.style.left = 0; shade.style.right = 0; shade.style.top = 0; shade.style.bottom = 0;
            shade.pickingMode = PickingMode.Position;
            shade.RegisterCallback<PointerDownEvent>(e => { if (e.target == shade) ClosePopup(); });
            var panel = new VisualElement();
            panel.style.position = Position.Absolute; panel.style.top = top;
            if (left >= 0) panel.style.left = left; else panel.style.right = right;
            panel.style.width = width;
            panel.style.backgroundColor = SurfaceCard;
            Radius(panel, 20); Border(panel, 1, Hairline);
            Pad(panel, 8, 8, 8, 8);
            shade.Add(panel);
            s_root.Add(shade);
            s_popup = shade;
            PopIn(panel, -8f, 0.98f, 160);
            return panel;
        }

        private static void ClosePopup()
        {
            if (s_popup != null) { s_popup.RemoveFromHierarchy(); s_popup = null; }
        }

        private static void ShowSuggestions(string q)
        {
            if (!Alive()) return;
            string key = (q ?? "").Trim();
            if (key.Length == 0) { ClosePopup(); return; }
            var hits = TabletWikiData.Search(key, 7);
            float sx = 100, sy = 66;
            if (s_searchBox != null && s_root != null && s_searchBox.worldBound.width > 0)
            {
                sx = s_searchBox.worldBound.x - s_root.worldBound.x;
                sy = s_searchBox.worldBound.yMax - s_root.worldBound.y + 8;
            }
            var panel = OpenPopup(sy, 0, sx, 480);
            // 遮罩会抢走输入框焦点：这里不让它拦截输入框所在区域的事件
            s_popup.pickingMode = PickingMode.Ignore;
            if (hits.Count == 0)
            {
                panel.Add(Text("没有找到与「" + key + "」相关的词条", 13, InkTertiary));
                panel.style.paddingLeft = 14; panel.style.paddingTop = 12; panel.style.paddingBottom = 12;
                return;
            }
            foreach (var a in hits)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
                Pad(row, 10, 8, 10, 8); Radius(row, 8);
                row.Add(Text(a.Title, 14, a.Unlocked ? InkPrimary : InkTertiary, true));
                var sub = Text(a.Category, 11, InkTertiary); sub.style.marginLeft = 8;
                row.Add(sub);
                string id = a.Id;
                Clickable(row, () => { if (s_search != null) s_search.SetValueWithoutNotify(string.Empty); Go(new View { Kind = Kind.Article, Arg = id }); });
                panel.Add(row);
            }
            var more = Text("搜索「" + key + "」的全部结果", 12, WikiBlue);
            Pad(more, 10, 8, 10, 6);
            Clickable(more, () => Go(new View { Kind = Kind.Search, Arg = key }));
            panel.Add(more);
        }

        private static void ShowAccountMenu()
        {
            if (!Alive() || !Signed) return;
            var panel = OpenPopup(66, 24, -1, 230);
            void Item(string text, Action click)
            {
                var row = Text(text, 14, InkPrimary);
                Pad(row, 12, 10, 12, 10); Radius(row, 8);
                Clickable(row, click);
                panel.Add(row);
            }
            var head = Text(AccountService.DisplayName, 14, InkPrimary, true);
            Pad(head, 12, 8, 12, 0); panel.Add(head);
            var sub = Text("小米 ID " + AccountService.Profile.xiaomiId, 11, InkTertiary);
            Pad(sub, 12, 2, 12, 8); panel.Add(sub);
            panel.Add(Rule(2, 4));
            Item("我的关注列表", () => Go(new View { Kind = Kind.Watch }));
            Item("阅读足迹", () => Go(new View { Kind = Kind.History }));
            panel.Add(Rule(4, 4));
            Item("退出登录", () => { ClosePopup(); TabletXiaomiSignIn.SignOut(AppId); });
        }

        // ============ 账号数据：关注与足迹 ============

        private static string UserKey(string prefix) => Signed ? prefix + AccountService.Profile.xiaomiId : null;

        // 关注无需登录：未登录存本机，登录后合并进账号列表
        private const string LocalWatchKey = "WikiWatch_local";
        private static string WatchKey() => Signed ? "WikiWatch_" + AccountService.Profile.xiaomiId : LocalWatchKey;

        private static void MergeLocalWatch(string accountKey)
        {
            var local = LoadList(LocalWatchKey);
            if (local.Count == 0) return;
            var list = LoadList(accountKey);
            for (int i = local.Count - 1; i >= 0; i--) if (!list.Contains(local[i])) list.Insert(0, local[i]);
            SaveList(accountKey, list);
            try
            {
                PlayerPrefs.DeleteKey(LocalWatchKey);
                s_listCache.Remove(LocalWatchKey);
                PlayerPrefs.Save();
            }
            catch { }
        }

        private static List<string> LoadList(string key)
        {
            var list = new List<string>();
            if (key == null) return list;
            if (s_listCache.TryGetValue(key, out var cached)) { list.AddRange(cached); return list; }
            string raw = null;
            try { raw = PlayerPrefs.GetString(key, string.Empty); } catch { }
            if (!string.IsNullOrEmpty(raw))
                foreach (var id in raw.Split(',')) if (id.Length > 0 && !list.Contains(id)) list.Add(id);
            s_listCache[key] = new List<string>(list);
            return list;
        }

        private static void SaveList(string key, List<string> list)
        {
            if (key == null) return;
            s_listCache[key] = new List<string>(list);
            try { PlayerPrefs.SetString(key, string.Join(",", list)); PlayerPrefs.Save(); } catch { }
        }

        private static void RecordRead(string id)
        {
            s_session.Remove(id); s_session.Insert(0, id);
            if (s_session.Count > 30) s_session.RemoveAt(s_session.Count - 1);
            string key = UserKey("WikiHistory_");
            if (key == null) return;
            var list = LoadList(key);
            list.Remove(id); list.Insert(0, id);
            if (list.Count > 40) list.RemoveRange(40, list.Count - 40);
            SaveList(key, list);
        }

        private static bool IsWatched(string id) => LoadList(WatchKey()).Contains(id);

        private static void ToggleWatch(WikiArticle a)
        {
            string key = WatchKey();
            var list = LoadList(key);
            if (list.Remove(a.Id)) TabletNotify.Toast("已取消关注「" + a.Title + "」");
            else { list.Insert(0, a.Id); TabletNotify.Toast("已关注「" + a.Title + "」"); }
            SaveList(key, list);
            Render();
        }

        // ============ 侧栏 ============

        // ============ 顶部标题区 / 底部导航 ============

        /// <summary>评审与外部跳转用：打开底部标签页之一（search / watch / history / more / home）。</summary>
        public static void OpenTab(string tab)
        {
            Kind k = tab == "search" ? Kind.SearchHome : tab == "watch" ? Kind.Watch : tab == "history" ? Kind.History : tab == "more" ? Kind.More : Kind.Home;
            Go(new View { Kind = k });
        }

        private static void RefreshHeader()
        {
            if (s_headLeft == null) return;
            s_headLeft.Clear();
            if (s_back.Count > 0)
            {
                s_navBack.style.display = DisplayStyle.Flex;
                s_navBack.style.marginRight = 12;
                s_headLeft.Add(s_navBack);
            }
            if (s_fromVn)
            {
                var pill = new VisualElement { name = "wiki-back-vn" };
                pill.style.flexDirection = FlexDirection.Row; pill.style.alignItems = Align.Center;
                pill.style.height = 44; Pad(pill, 14, 0, 18, 0); Radius(pill, 22);
                pill.style.backgroundColor = Tint; pill.style.marginRight = 12; pill.style.flexShrink = 0;
                var arrow = new TabletIconElement(TabletIconKind.Back, 16, Accent) { pickingMode = PickingMode.Ignore };
                arrow.style.marginRight = 6; pill.Add(arrow);
                var lbl = Text("返回剧情", 14, Accent, true); lbl.pickingMode = PickingMode.Ignore; pill.Add(lbl);
                Clickable(pill, () => s_exitToVn?.Invoke(), TintDeep);
                s_headLeft.Add(pill);
            }
            if (s_cur.Kind == Kind.Home)
            {
                var logo = new VisualElement { name = "wiki-logo" };
                logo.style.flexDirection = FlexDirection.Row; logo.style.alignItems = Align.Center;
                var mark = new VisualElement();
                mark.style.width = 38; mark.style.height = 38; Radius(mark, 12);
                mark.style.alignItems = Align.Center; mark.style.justifyContent = Justify.Center;
                mark.pickingMode = PickingMode.Ignore;
                mark.style.overflow = Overflow.Hidden; mark.Add(new TabletMediaMascot(38));
                logo.Add(mark);
                var nm = Text(AppName, 22, Ink, true); nm.style.marginLeft = 12; nm.pickingMode = PickingMode.Ignore;
                logo.Add(nm);
                s_headLeft.Add(logo);
            }
        }

        private static void BottomItem(VisualElement bar, string label, TabletIconKind icon, bool selected, Action click, string name = null)
        {
            var item = new VisualElement();
            if (name != null) item.name = name;
            item.style.flexGrow = 1; item.style.flexBasis = 0;
            item.style.alignItems = Align.Center; item.style.justifyContent = Justify.Center;
            item.style.paddingTop = 8; item.style.paddingBottom = 6;
            var pill = new VisualElement();
            pill.style.width = 68; pill.style.height = 32; Radius(pill, 16);
            pill.style.alignItems = Align.Center; pill.style.justifyContent = Justify.Center;
            pill.style.backgroundColor = selected ? TintDeep : new Color(0f, 0f, 0f, 0f);
            pill.pickingMode = PickingMode.Ignore;
            Ease(pill, 160, "background-color", "scale", "opacity");
            if (selected) { if (name != s_lastSelTab) PopIn(pill, 0f, 0.6f, 220); s_lastSelTab = name; }
            pill.Add(new TabletIconElement(icon, 20, selected ? Accent : InkSecondary) { pickingMode = PickingMode.Ignore });
            item.Add(pill);
            var t = Text(label, 12, selected ? Accent : InkSecondary, selected);
            t.style.marginTop = 3; t.pickingMode = PickingMode.Ignore;
            item.Add(t);
            item.pickingMode = PickingMode.Position;
            item.RegisterCallback<PointerEnterEvent>(_ => { if (!selected) pill.style.backgroundColor = new Color(0f, 0f, 0f, 0.06f); UiCursor.ShowHand(); });
            item.RegisterCallback<PointerLeaveEvent>(_ => { pill.style.backgroundColor = selected ? TintDeep : new Color(0f, 0f, 0f, 0f); UiCursor.ShowArrow(); });
            item.RegisterCallback<ClickEvent>(_ => click());
            item.RegisterCallback<PointerDownEvent>(_ => { if (Motion) pill.style.scale = new Scale(new Vector3(0.9f, 0.9f, 1f)); });
            item.RegisterCallback<PointerUpEvent>(_ => pill.style.scale = new Scale(Vector3.one));
            bar.Add(item);
        }

        private static void RefreshBottom()
        {
            if (s_bottom == null) return;
            s_bottom.Clear();
            Kind k = s_cur.Kind;
            if (k == Kind.Article)
            {
                var a = TabletWikiData.Get(s_cur.Arg);
                bool watched = a != null && IsWatched(a.Id);
                BottomItem(s_bottom, watched ? "已关注" : "关注", TabletIconKind.Star, watched, () => { if (a != null && a.Unlocked) ToggleWatch(a); }, "wiki-act-watch");
                BottomItem(s_bottom, "目录", TabletIconKind.Bookmark, false, ShowContents, "wiki-act-toc");
                BottomItem(s_bottom, "字号 " + (s_bodySize == 15 ? "中" : s_bodySize == 17 ? "大" : "特大"), TabletIconKind.Note, false, () => { s_bodySize = s_bodySize == 15 ? 17 : s_bodySize == 17 ? 20 : 15; Render(); }, "wiki-act-size");
                BottomItem(s_bottom, "分类", TabletIconKind.Map, false, () => { if (a != null) Go(new View { Kind = Kind.Category, Arg = a.Category }); }, "wiki-act-cat");
                return;
            }
            BottomItem(s_bottom, "首页", TabletIconKind.Home, k == Kind.Home, () => Go(new View { Kind = Kind.Home }), "wiki-tab-home");
            BottomItem(s_bottom, "关注", TabletIconKind.Star, k == Kind.Watch, () => Go(new View { Kind = Kind.Watch }), "wiki-tab-watch");
            BottomItem(s_bottom, "搜索", TabletIconKind.Search, k == Kind.SearchHome || k == Kind.Search, () => Go(new View { Kind = Kind.SearchHome }), "wiki-tab-search");
            BottomItem(s_bottom, "足迹", TabletIconKind.Clock, k == Kind.History, () => Go(new View { Kind = Kind.History }), "wiki-tab-history");
            BottomItem(s_bottom, "更多", TabletIconKind.Apps, k == Kind.More || k == Kind.Category, () => Go(new View { Kind = Kind.More }), "wiki-tab-more");
        }

        private static void ShowContents()
        {
            if (!Alive() || s_tocSections == null || s_tocSections.Count == 0)
            {
                TabletNotify.Toast("这个词条没有目录");
                return;
            }
            var panel = OpenPopup(0, 0, 0, 340);
            panel.style.top = StyleKeyword.Auto; panel.style.bottom = 88;
            panel.style.left = StyleKeyword.Auto; panel.style.right = 0;
            panel.style.marginRight = 24;
            panel.Add(Text("目录", 12, InkTertiary, true));
            panel.Q<Label>().style.marginLeft = 12; panel.Q<Label>().style.marginBottom = 6; panel.Q<Label>().style.marginTop = 6;
            for (int i = 0; i < s_tocSections.Count; i++)
            {
                int idx = i;
                var row = Text((i + 1) + "   " + s_tocSections[i].Heading, 15, Ink);
                Pad(row, 12, 11, 12, 11); Radius(row, 12);
                Clickable(row, () => { ClosePopup(); if (idx < s_tocHeaders.Count && s_tocHeaders[idx].panel != null) s_main.ScrollTo(s_tocHeaders[idx]); });
                panel.Add(row);
            }
        }

        // ============ 搜索页与更多页 ============

        /// <summary>词条左侧的小徽标：有立绘用立绘，没有就用分类图标。</summary>
        private static VisualElement Badge(WikiArticle a, float size)
        {
            var b = new VisualElement();
            b.style.width = size; b.style.height = size; b.style.flexShrink = 0; Radius(b, size * 0.3f);
            b.style.backgroundColor = Paper; b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
            b.style.overflow = Overflow.Hidden; b.pickingMode = PickingMode.Ignore;
            var tex = a.Unlocked ? LoadPortrait(a.Portrait) : null;
            if (tex != null)
            {
                var img = new Image { image = tex, scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
                img.style.width = size; img.style.height = size;
                b.Add(img);
            }
            else
            {
                TabletIconKind ic;
                if (!s_catIcons.TryGetValue(a.Category, out ic)) ic = TabletIconKind.Globe;
                b.Add(new TabletIconElement(ic, size * 0.45f, a.Unlocked ? InkSecondary : InkTertiary) { pickingMode = PickingMode.Ignore });
            }
            return b;
        }

        private static void RenderSearchHome(VisualElement host)
        {
            PageTitle(host, "搜索", "在上方输入关键词，或从下面的最近阅读与随手翻翻开始。");
            var recent = RecentList();
            var card = Card();
            card.Add(SectionTitle("最近阅读"));
            int shown = 0;
            foreach (var id in recent)
            {
                var a = TabletWikiData.Get(id);
                if (a == null) continue;
                card.Add(ListRow(a)); if (++shown >= 8) break;
            }
            if (shown == 0) card.Add(Text("还没有阅读记录，先翻几个词条吧。", 14, InkTertiary));
            host.Add(card);

            var pool = new List<WikiArticle>();
            foreach (var a in TabletWikiData.All) if (a.Unlocked && a.Category != TabletWikiData.CatArchive && !a.IsTerm) pool.Add(a);
            if (pool.Count > 0)
            {
                var more = Card();
                more.Add(SectionTitle("随手翻翻"));
                int start = TabletDateTime.Now.DayOfYear * 7;
                for (int i = 0; i < Mathf.Min(5, pool.Count); i++) more.Add(ListRow(pool[(start + i * 3) % pool.Count]));
                host.Add(more);
            }
        }

        private static void RenderAccountCard(VisualElement host)
        {
            var card = Card();
            card.name = "wiki-account-card";
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            if (Signed)
            {
                var p = AccountService.Profile;
                var av = TabletAvatar.Build(p, 56); av.pickingMode = PickingMode.Ignore; av.style.marginRight = 16;
                row.Add(av);
                var col = new VisualElement(); col.style.flexGrow = 1; col.style.flexShrink = 1;
                col.Add(Text(AccountService.DisplayName, 18, Ink, true));
                var id = Text("小米 ID " + p.xiaomiId + " · 关注与足迹已同步", 12, InkTertiary); id.style.marginTop = 2;
                col.Add(id);
                row.Add(col);
                var outBtn = new Button(() => TabletXiaomiSignIn.SignOut(AppId)) { text = "退出登录", name = "wiki-signout" };
                outBtn.style.height = 38; Pad(outBtn, 18, 0, 18, 0); Radius(outBtn, 19);
                outBtn.style.marginLeft = 12; outBtn.style.marginRight = 0; outBtn.style.marginTop = 0; outBtn.style.marginBottom = 0;
                outBtn.style.backgroundColor = Paper; Border(outBtn, 1, Hairline);
                outBtn.style.color = Ink; outBtn.style.fontSize = 13; outBtn.style.unityFontDefinition = FdBold();
                row.Add(outBtn);
            }
            else
            {
                var ic = new VisualElement(); ic.style.width = 56; ic.style.height = 56; Radius(ic, 28);
                ic.style.alignItems = Align.Center; ic.style.justifyContent = Justify.Center; ic.style.marginRight = 16; ic.pickingMode = PickingMode.Ignore;
                ic.style.overflow = Overflow.Hidden; ic.Add(new TabletMediaMascot(56));
                row.Add(ic);
                var col = new VisualElement(); col.style.flexGrow = 1; col.style.flexShrink = 1;
                col.Add(Text("登录小米账号", 18, Ink, true));
                var d = Text("保存阅读足迹，并在设备间同步你关注的词条。", 12, InkSecondary); d.style.marginTop = 2;
                col.Add(d);
                row.Add(col);
                var b = PrimaryButton("登录", "wiki-login", () => RequestSignIn()); b.style.marginLeft = 12;
                row.Add(b);
            }
            card.Add(row);
            host.Add(card);
        }

        private static void RenderMore(VisualElement host)
        {
            PageTitle(host, "更多", "账号、收录进度与分类浏览。");
            RenderAccountCard(host);
            TabletWikiData.Progress(out int have, out int total);
            var prog = Card();
            var pr = new VisualElement(); pr.style.flexDirection = FlexDirection.Row; pr.style.alignItems = Align.FlexEnd;
            pr.Add(Text(have.ToString(), 34, Ink, true));
            var pt = Text(" / " + total + " 条已收录", 13, InkTertiary); pt.style.marginLeft = 6; pt.style.marginBottom = 5; pr.Add(pt);
            prog.Add(pr);
            var bar = BuildProgressBar(total == 0 ? 0f : (float)have / total, Amber, 8f, new Color(0f, 0f, 0f, 0.07f));
            bar.style.marginTop = 10;
            prog.Add(bar);
            var hint = Text("推进剧情、翻看章节与收听音乐，会解锁更多词条、角色与资料库。", 12, InkSecondary); hint.style.marginTop = 10;
            prog.Add(hint);
            host.Add(prog);

            var card = Card();
            card.Add(SectionTitle("分类"));
            foreach (var cat in TabletWikiData.Categories)
            {
                string c = cat;
                var list = TabletWikiData.InCategory(c);
                int open = list.FindAll(x => x.Unlocked).Count;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
                Pad(row, 12, 12, 14, 12); Radius(row, 16);
                var ib = new VisualElement(); ib.style.width = 42; ib.style.height = 42; Radius(ib, 14);
                ib.style.backgroundColor = Tint; ib.style.alignItems = Align.Center; ib.style.justifyContent = Justify.Center; ib.pickingMode = PickingMode.Ignore;
                ib.Add(new TabletIconElement(s_catIcons[c], 20, Accent) { pickingMode = PickingMode.Ignore });
                row.Add(ib);
                var tx = new VisualElement(); tx.style.marginLeft = 14; tx.style.flexGrow = 1; tx.pickingMode = PickingMode.Ignore;
                var nm = Text(c, 16, Ink, true); nm.pickingMode = PickingMode.Ignore; tx.Add(nm);
                var n = Text(open + " / " + list.Count + " 已收录", 12, InkTertiary); n.style.marginTop = 2; n.pickingMode = PickingMode.Ignore; tx.Add(n);
                row.Add(tx);
                var chev = new TabletIconElement(TabletIconKind.Back, 14, InkTertiary) { pickingMode = PickingMode.Ignore };
                chev.style.rotate = new Rotate(new Angle(180f, AngleUnit.Degree));
                row.Add(chev);
                Clickable(row, () => Go(new View { Kind = Kind.Category, Arg = c }));
                card.Add(row);
            }
            host.Add(card);
        }

        // ============ 首页 ============

        private static void RenderHome(VisualElement host)
        {
            // 顶部大横幅：深墨绿底、琥珀点缀、右侧淡淡的“百”字水印
            var hero = new VisualElement { name = "wiki-hero" };
            hero.style.backgroundColor = Tint; Radius(hero, 28); SoftShadow(hero, 28, 1.2f); Pad(hero, 38, 34, 38, 34);
            hero.style.marginBottom = 18;
            var eyebrow = Text("RAILWAY RENAISSANCE · ENCYCLOPEDIA", 11, Accent, true);
            eyebrow.style.letterSpacing = 2;
            hero.Add(eyebrow);
            var title = Text("欢迎来到媒姬百科", 36, Ink, true); title.style.marginTop = 10;
            hero.Add(title);
            var tag = Text("人物、车辆、线路与术语——记录铁路复兴世界里每一条值得被记住的事。", 14, InkSecondary);
            tag.style.marginTop = 8; tag.style.maxWidth = 620;
            hero.Add(tag);
            TabletWikiData.Progress(out int have, out int total);
            var stats = new VisualElement(); stats.style.flexDirection = FlexDirection.Row; stats.style.marginTop = 18; stats.style.flexWrap = Wrap.Wrap;
            void Pill(string t) { var p = BuildTagPill(t, Color.white, Accent, 12f); p.style.marginRight = 8; stats.Add(p); }
            Pill("已收录 " + have + " / " + total + " 条");
            Pill(TabletWikiData.InCategory(TabletWikiData.CatTerm).Count + " 条铁路术语");
            Pill(TabletWikiData.InCategory(TabletWikiData.CatPeople).Count + " 位人物");
            hero.Add(stats);
            host.Add(hero);

            // 今日词条 + 你知道吗：并排
            var pair = new VisualElement(); pair.style.flexDirection = FlexDirection.Row;
            host.Add(pair);

            var featuredPool = new List<WikiArticle>();
            foreach (var a in TabletWikiData.All)
                if (a.Unlocked && !a.IsTerm && a.Category != TabletWikiData.CatArchive && a.Category != TabletWikiData.CatStory && !string.IsNullOrEmpty(a.Lead))
                    featuredPool.Add(a);
            if (featuredPool.Count > 0)
            {
                var day = TabletDateTime.Now;
                var f = featuredPool[(day.Year * 372 + day.DayOfYear) % featuredPool.Count];
                var card = Card();
                card.style.flexGrow = 3; card.style.flexBasis = 0; card.style.minWidth = 0; card.style.marginRight = 16;
                card.Add(SectionTitle("今日词条"));
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row;
                var tex = LoadPortrait(f.Portrait);
                if (tex != null)
                {
                    var img = new Image { image = tex, scaleMode = ScaleMode.ScaleAndCrop };
                    img.style.width = 108; img.style.height = 132; Radius(img, 18); img.style.marginRight = 18; img.style.flexShrink = 0;
                    img.style.backgroundColor = Paper;
                    row.Add(img);
                }
                var col = new VisualElement(); col.style.flexGrow = 1; col.style.flexShrink = 1; col.style.minWidth = 0;
                col.Add(Text(f.Title, 22, Ink, true));
                var sb = Text(f.Subtitle, 12, InkTertiary); sb.style.marginTop = 2; col.Add(sb);
                var lead = Rich(f.Lead, 14, InkSecondary, f.Id); lead.style.marginTop = 8;
                col.Add(lead);
                string fid = f.Id;
                var more = Text("阅读全文", 13, Color.white, true);
                Pad(more, 18, 8, 18, 8); Radius(more, 18); more.style.backgroundColor = Accent; more.style.marginTop = 12; more.style.alignSelf = Align.FlexStart;
                Clickable(more, () => Go(new View { Kind = Kind.Article, Arg = fid }), new Color(0.09f, 0.45f, 0.40f, 1f));
                col.Add(more);
                row.Add(col);
                card.Add(row);
                pair.Add(card);
            }

            var dyk = TabletWikiData.DidYouKnow;
            if (dyk.Length > 0)
            {
                var card = Card();
                card.style.flexGrow = 2; card.style.flexBasis = 0; card.style.minWidth = 0;
                card.Add(SectionTitle("你知道吗"));
                int start = TabletDateTime.Now.DayOfYear;
                for (int i = 0; i < Mathf.Min(4, dyk.Length); i++)
                {
                    var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.marginBottom = 9;
                    var dot = new VisualElement(); dot.style.width = 6; dot.style.height = 6; Radius(dot, 3);
                    dot.style.backgroundColor = Amber; dot.style.marginTop = 7; dot.style.marginRight = 10; dot.style.flexShrink = 0;
                    r.Add(dot);
                    var l = Text(dyk[(start + i) % dyk.Length], 13, Ink); l.style.flexShrink = 1; l.style.flexGrow = 1;
                    r.Add(l);
                    card.Add(r);
                }
                pair.Add(card);
            }

            // 分类入口：图标方块 + 名称 + 收录进度
            var catCard = Card();
            catCard.Add(SectionTitle("分类浏览"));
            var grid = new VisualElement(); grid.style.flexDirection = FlexDirection.Row; grid.style.flexWrap = Wrap.Wrap;
            foreach (var cat in TabletWikiData.Categories)
            {
                string c = cat;
                var list = TabletWikiData.InCategory(c);
                int open = list.FindAll(x => x.Unlocked).Count;
                var cell = new VisualElement();
                cell.style.width = new Length(31.5f, LengthUnit.Percent); cell.style.marginRight = new Length(1.5f, LengthUnit.Percent); cell.style.marginBottom = 12;
                cell.style.flexDirection = FlexDirection.Row; cell.style.alignItems = Align.Center;
                Pad(cell, 14, 14, 14, 14); Radius(cell, 18); cell.style.backgroundColor = Paper;
                var ib = new VisualElement(); ib.style.width = 42; ib.style.height = 42; Radius(ib, 14);
                ib.style.backgroundColor = Tint; ib.style.alignItems = Align.Center; ib.style.justifyContent = Justify.Center; ib.pickingMode = PickingMode.Ignore;
                ib.Add(new TabletIconElement(s_catIcons[c], 20, Accent) { pickingMode = PickingMode.Ignore });
                cell.Add(ib);
                var tx = new VisualElement(); tx.style.marginLeft = 12; tx.style.flexGrow = 1; tx.style.flexShrink = 1; tx.pickingMode = PickingMode.Ignore;
                var nm = Text(c, 15, Ink, true); nm.pickingMode = PickingMode.Ignore; tx.Add(nm);
                var n = Text(open + " / " + list.Count + " 已收录", 11, InkTertiary); n.style.marginTop = 2; n.pickingMode = PickingMode.Ignore;
                tx.Add(n);
                cell.Add(tx);
                Clickable(cell, () => Go(new View { Kind = Kind.Category, Arg = c }), new Color(0.90f, 0.88f, 0.83f, 1f));
                grid.Add(cell);
            }
            catCard.Add(grid);
            host.Add(catCard);

            // 最近阅读
            var recent = RecentList();
            if (recent.Count > 0)
            {
                var card = Card();
                card.Add(SectionTitle("最近阅读"));
                var chips = new VisualElement(); chips.style.flexDirection = FlexDirection.Row; chips.style.flexWrap = Wrap.Wrap;
                for (int i = 0; i < Mathf.Min(10, recent.Count); i++)
                {
                    var a = TabletWikiData.Get(recent[i]);
                    if (a == null) continue;
                    string id = a.Id;
                    chips.Add(Chip(a.Title, a.Unlocked ? WikiBlue : RedLink, () => Go(new View { Kind = Kind.Article, Arg = id })));
                }
                card.Add(chips);
                host.Add(card);
            }
        }

        private static List<string> RecentList()
        {
            var persisted = LoadList(UserKey("WikiHistory_"));
            return Signed && persisted.Count > 0 ? persisted : new List<string>(s_session);
        }

        private static Texture2D LoadPortrait(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var t = Resources.Load<Texture2D>(path);
            if (t == null) t = Resources.Load<Texture2D>(path + "_normal");
            return t;
        }

        // ============ 词条列表（分类 / 搜索 / 关注 / 足迹） ============

        private static VisualElement ListRow(WikiArticle a)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            Pad(row, 12, 10, 14, 10); Radius(row, 16);
            row.Add(Badge(a, 48));
            var col = new VisualElement(); col.style.marginLeft = 14; col.style.flexGrow = 1; col.style.flexShrink = 1; col.style.minWidth = 0; col.pickingMode = PickingMode.Ignore;
            var t = Text(a.Title, 16, a.Unlocked ? Ink : RedLink, true); t.pickingMode = PickingMode.Ignore;
            col.Add(t);
            if (a.Unlocked)
            {
                if (!string.IsNullOrEmpty(a.Subtitle))
                {
                    var s = Text(a.Subtitle, 12, InkTertiary); s.style.marginTop = 2; s.pickingMode = PickingMode.Ignore;
                    col.Add(s);
                }
            }
            else
            {
                var s = Text("未收录 · 尚待解锁", 12, RedLink); s.style.marginTop = 2; s.pickingMode = PickingMode.Ignore;
                col.Add(s);
            }
            row.Add(col);
            var chev = new TabletIconElement(TabletIconKind.Back, 14, InkTertiary) { pickingMode = PickingMode.Ignore };
            chev.style.rotate = new Rotate(new Angle(180f, AngleUnit.Degree));
            row.Add(chev);
            string id = a.Id;
            Clickable(row, () => Go(new View { Kind = Kind.Article, Arg = id }));
            return row;
        }

        private static void PageTitle(VisualElement host, string title, string sub = null)
        {
            var wrap = new VisualElement(); wrap.style.marginBottom = 18; wrap.style.marginLeft = 4;
            wrap.Add(Text(title, 30, Ink, true));
            if (!string.IsNullOrEmpty(sub))
            {
                var s = Text(sub, 13, InkSecondary); s.style.marginTop = 6; wrap.Add(s);
            }
            host.Add(wrap);
        }

        private static void RenderCategory(VisualElement host, string category)
        {
            PageTitle(host, "分类：" + category, "本分类下的全部词条；红色标题表示尚未收录。");
            var list = TabletWikiData.InCategory(category);
            var card = Card();
            AddRowsInBatches(card, list, (parent, a, i) =>
            {
                string prev = i > 0 ? list[i - 1].Group : null;
                if (a.Group != prev && !string.IsNullOrEmpty(a.Group))
                {
                    var g = Text(a.Group, 13, InkSecondary, true);
                    Pad(g, 12, i == 0 ? 0 : 14, 0, 4);
                    parent.Add(g);
                }
                parent.Add(ListRow(a));
            });
            if (list.Count == 0) card.Add(Text("这个分类里还没有词条。", 14, InkTertiary));
            host.Add(card);
        }

        private static void RenderSearch(VisualElement host, string q)
        {
            PageTitle(host, "搜索：" + q);
            var hits = TabletWikiData.Search(q, 60);
            var card = Card();
            if (hits.Count == 0)
            {
                card.Add(Text("没有找到与「" + q + "」相关的词条。试试更短的关键词，或从左侧分类浏览。", 14, InkTertiary));
            }
            else
            {
                card.Add(Text("找到 " + hits.Count + " 条结果", 12, InkTertiary));
                foreach (var a in hits)
                {
                    card.Add(ListRow(a));
                    if (a.Unlocked && !string.IsNullOrEmpty(a.Lead))
                    {
                        string plain = System.Text.RegularExpressions.Regex.Replace(a.Lead, @"'''|\[\[[^\]\|]+\||\]\]", string.Empty);
                        if (plain.Length > 70) plain = plain.Substring(0, 70) + "…";
                        var sn = Text(plain, 12, InkSecondary); Pad(sn, 12, 0, 12, 6);
                        card.Add(sn);
                    }
                }
            }
            host.Add(card);
        }

        private static Button PrimaryButton(string text, string name, Action click)
        {
            var b = new Button(() => click()) { text = text, name = name };
            b.style.height = 42; Pad(b, 24, 0, 24, 0); Radius(b, 21);
            b.style.marginLeft = 0; b.style.marginRight = 0; b.style.marginTop = 0; b.style.marginBottom = 0;
            b.style.borderTopWidth = 0; b.style.borderBottomWidth = 0; b.style.borderLeftWidth = 0; b.style.borderRightWidth = 0;
            b.style.backgroundColor = new Color(1f, 0.41f, 0f, 1f); b.style.color = Color.white;
            b.style.unityFontDefinition = FdBold(); b.style.fontSize = 14;
            return b;
        }

        private static void RenderWatch(VisualElement host)
        {
            PageTitle(host, "我的关注列表", Signed ? "关注的词条会记在你的小米账号下。" : "无需登录即可关注；登录小米账号后可在设备间同步。");
            var card = Card();
            var ids = LoadList(WatchKey());
            int shown = 0;
            foreach (var id in ids)
            {
                var a = TabletWikiData.Get(id);
                if (a == null) continue;
                card.Add(ListRow(a)); shown++;
            }
            if (shown == 0) card.Add(Text("还没有关注的词条。打开任意词条，点标题右侧的星标即可关注。", 14, InkTertiary));
            host.Add(card);
            if (!Signed)
            {
                var sync = Card();
                sync.name = "wiki-watch-sync";
                sync.Add(Text("同步关注列表", 16, Ink, true));
                var t = Text("登录小米账号，在其他设备上也能看到你关注的词条。", 13, InkSecondary); t.style.marginTop = 4; t.style.marginBottom = 12;
                sync.Add(t);
                var b = PrimaryButton("小米账号登录", "wiki-watch-login", () => RequestSignIn()); b.style.alignSelf = Align.FlexStart;
                sync.Add(b);
                host.Add(sync);
            }
        }

        private static void RenderHistory(VisualElement host)
        {
            if (!Signed)
            {
                var box = new VisualElement { name = "wiki-history-login" };
                box.style.flexGrow = 1; box.style.alignItems = Align.Center; box.style.justifyContent = Justify.Center;
                box.style.paddingTop = 60; box.style.paddingBottom = 60;
                var ring = new VisualElement(); ring.style.width = 120; ring.style.height = 120; Radius(ring, 60);
                ring.style.backgroundColor = Tint; ring.style.alignItems = Align.Center; ring.style.justifyContent = Justify.Center;
                var ring2 = new VisualElement(); ring2.style.width = 84; ring2.style.height = 84; Radius(ring2, 42);
                ring2.style.backgroundColor = TintDeep; ring2.style.alignItems = Align.Center; ring2.style.justifyContent = Justify.Center;
                ring2.Add(new TabletIconElement(TabletIconKind.Clock, 40, Accent) { pickingMode = PickingMode.Ignore });
                ring.Add(ring2);
                box.Add(ring);
                var t = Text("登录后查看阅读足迹", 22, Ink, true); t.style.marginTop = 26; box.Add(t);
                var d = Text("登录小米账号，即可查看并长期保存你在媒姬百科读过的词条。", 14, InkSecondary);
                d.style.marginTop = 8; d.style.marginBottom = 24; d.style.maxWidth = 420; d.style.unityTextAlign = TextAnchor.MiddleCenter; d.style.whiteSpace = WhiteSpace.Normal;
                box.Add(d);
                box.Add(PrimaryButton("小米账号登录", "wiki-history-signin", () => RequestSignIn()));
                host.Add(box);
                return;
            }
            PageTitle(host, "阅读足迹", "最近读过的词条，随小米账号保存。");
            var card = Card();
            int shown = 0;
            foreach (var id in RecentList())
            {
                var a = TabletWikiData.Get(id);
                if (a == null) continue;
                card.Add(ListRow(a)); shown++;
            }
            if (shown == 0) card.Add(Text("还没有阅读记录。", 14, InkTertiary));
            host.Add(card);
        }

        // ============ 词条页 ============

        private static void RenderArticle(VisualElement host, WikiArticle a)
        {
            // 词条头：深墨绿横幅，类别胶囊 + 大标题 + 副标题，右侧星标
            var head = new VisualElement { name = "wiki-article" };
            head.style.backgroundColor = Tint; Radius(head, 28); SoftShadow(head, 28, 1.2f); Pad(head, 34, 28, 26, 28);
            head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center;
            head.style.marginBottom = 16;
            host.Add(head);

            var left = new VisualElement(); left.style.flexGrow = 1; left.style.flexShrink = 1; left.style.minWidth = 0;
            head.Add(left);
            string cat = a.Category;
            var catChip = Text(cat, 11, Accent, true);
            Pad(catChip, 12, 4, 12, 4); Radius(catChip, 13); catChip.style.backgroundColor = Color.white;
            catChip.style.alignSelf = Align.FlexStart;
            Clickable(catChip, () => Go(new View { Kind = Kind.Category, Arg = cat }), new Color(0.93f, 0.97f, 0.95f, 1f));
            left.Add(catChip);
            var title = Text(a.Title, 36, Ink, true);
            title.name = "wiki-article-title";
            title.style.marginTop = 10;
            left.Add(title);
            string sub = a.Unlocked ? a.Subtitle : "尚未收录";
            if (!string.IsNullOrEmpty(sub))
            {
                var subLabel = Text(sub, 14, InkSecondary);
                subLabel.style.marginTop = 6;
                left.Add(subLabel);
            }

            if (a.Unlocked)
            {
                bool watched = IsWatched(a.Id);
                var star = TabletAppBar.IconButton(TabletIconKind.Star, 22, watched ? Amber : InkSecondary,
                    Color.white, new Color(0.93f, 0.97f, 0.95f, 1f), () => ToggleWatch(a));
                star.name = "wiki-watch";
                star.style.width = 46; star.style.height = 46;
                star.tooltip = watched ? "取消关注" : "关注此词条";
                head.Add(star);
            }

            if (!a.Unlocked)
            {
                var box = Card();
                box.style.backgroundColor = Notice; Border(box, 1, NoticeLine);
                box.Add(Text("此词条尚未收录", 18, Ink, true));
                var hint = Text(string.IsNullOrEmpty(a.UnlockHint) ? "继续推进剧情即可解锁。" : "解锁条件：" + a.UnlockHint + "。", 14, InkSecondary);
                hint.style.marginTop = 6;
                box.Add(hint);
                host.Add(box);
                return;
            }

            var body = new VisualElement { name = "wiki-article-body" };
            body.style.flexDirection = FlexDirection.Row; body.style.alignItems = Align.FlexStart;
            host.Add(body);

            var text = Card();
            text.name = "wiki-article-text";
            text.style.flexGrow = 1; text.style.flexShrink = 1; text.style.minWidth = 0; text.style.marginBottom = 0;
            body.Add(text);

            VisualElement info = BuildInfobox(a);
            if (info != null)
            {
                body.Add(info);
                bool narrow = false;
                body.RegisterCallback<GeometryChangedEvent>(e =>
                {
                    bool n = e.newRect.width > 0 && e.newRect.width < 680;
                    if (n == narrow && e.oldRect.width > 0) return;
                    narrow = n;
                    body.style.flexDirection = n ? FlexDirection.ColumnReverse : FlexDirection.Row;
                    body.style.alignItems = n ? Align.Stretch : Align.FlexStart;
                    info.style.width = n ? new StyleLength(StyleKeyword.Auto) : new StyleLength(292f);
                    info.style.marginLeft = n ? 0 : 16;
                    info.style.marginBottom = n ? 16 : 0;
                });
            }

            if (!string.IsNullOrEmpty(a.Lead))
            {
                var lead = Rich(a.Lead, s_bodySize + 1, Ink, a.Id, a.IsTerm);
                lead.style.marginBottom = 14;
                text.Add(lead);
            }

            var sections = a.GetSections();
            var headers = new List<VisualElement>();
            s_tocSections = sections; s_tocHeaders = headers;

            if (sections.Count >= 2)
            {
                var toc = new VisualElement { name = "wiki-toc" };
                toc.style.flexDirection = FlexDirection.Row; toc.style.flexWrap = Wrap.Wrap; toc.style.alignItems = Align.Center;
                toc.style.marginBottom = 8;
                var tt = Text("本文", 12, InkTertiary, true); tt.style.marginRight = 10; tt.style.marginBottom = 8;
                toc.Add(tt);
                text.Add(toc);
                for (int i = 0; i < sections.Count; i++)
                {
                    int idx = i;
                    toc.Add(Chip(sections[i].Heading, WikiBlue, () => { if (idx < headers.Count && headers[idx].panel != null) s_main.ScrollTo(headers[idx]); }));
                }
            }

            foreach (var sec in sections)
            {
                var h = new VisualElement();
                h.style.marginTop = 14; h.style.marginBottom = 4;
                h.Add(SectionTitle(sec.Heading, 20));
                text.Add(h);
                headers.Add(h);
                foreach (var raw in sec.Text.Split('\n'))
                {
                    string line = raw.Trim();
                    if (line.Length == 0) continue;
                    if (line[0] == '@')
                    {
                        var blk = BuildBlock(line, a.Id);
                        if (blk != null) { text.Add(blk); continue; }
                    }
                    var l = Rich(line, s_bodySize, Ink, a.Id, a.IsTerm);
                    l.style.marginBottom = 8;
                    if (line.StartsWith("· ")) l.style.marginLeft = 10;
                    text.Add(l);
                }
            }

            if (!string.IsNullOrEmpty(a.ChapterScript))
            {
                var replay = Text("在「归途」中回看此章", 14, Color.white, true);
                replay.name = "wiki-replay";
                Pad(replay, 20, 10, 20, 10); Radius(replay, 20); replay.style.backgroundColor = Accent;
                replay.style.alignSelf = Align.FlexStart; replay.style.marginTop = 12;
                Clickable(replay, () => StoryReplayService.OpenLibrary(), new Color(0.09f, 0.45f, 0.40f, 1f));
                text.Add(replay);
            }

            if (a.See != null && a.See.Length > 0)
            {
                var h = new VisualElement(); h.style.marginTop = 18;
                h.Add(SectionTitle("参见", 20));
                text.Add(h);
                var chips = new VisualElement(); chips.style.flexDirection = FlexDirection.Row; chips.style.flexWrap = Wrap.Wrap;
                foreach (var id in a.See)
                {
                    var t = TabletWikiData.Get(id);
                    if (t == null) continue;
                    string tid = t.Id;
                    chips.Add(Chip(t.Title, t.Unlocked ? WikiBlue : RedLink, () => Go(new View { Kind = Kind.Article, Arg = tid })));
                }
                text.Add(chips);
            }
        }

        private static VisualElement BuildInfobox(WikiArticle a)
        {
            var tex = LoadPortrait(a.Portrait);
            if (a.Infobox.Count == 0 && tex == null) return null;
            var box = new VisualElement { name = "wiki-infobox" };
            box.style.width = 292; box.style.flexShrink = 0; box.style.alignSelf = Align.FlexStart;
            box.style.backgroundColor = SurfaceCard; Radius(box, 22); Border(box, 1, Hairline);
            box.style.overflow = Overflow.Hidden;

            if (tex != null)
            {
                var stage = new VisualElement();
                stage.style.backgroundColor = InfoHead; stage.style.height = 232;
                stage.style.alignItems = Align.Center; stage.style.justifyContent = Justify.Center;
                var img = new Image { image = tex, scaleMode = ScaleMode.ScaleToFit };
                img.style.height = 208; img.style.width = 208;
                stage.Add(img);
                box.Add(stage);
            }

            var cap = Text(a.Title, 17, Ink, true);
            Pad(cap, 20, 16, 20, 2);
            box.Add(cap);
            var capSub = Text(a.Subtitle, 12, InkTertiary);
            Pad(capSub, 20, 0, 20, 10);
            box.Add(capSub);

            foreach (var kv in a.Infobox)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.borderTopWidth = 1; row.style.borderTopColor = Hairline;
                Pad(row, 20, 10, 20, 10);
                var k = Text(kv.Key, 12, InkTertiary, true);
                k.style.width = 70; k.style.flexShrink = 0; k.style.marginTop = 1;
                row.Add(k);
                var v = Rich(kv.Value, 13, Ink, a.Id);
                v.style.flexGrow = 1; v.style.flexShrink = 1;
                row.Add(v);
                box.Add(row);
            }
            var pad = new VisualElement(); pad.style.height = 6; box.Add(pad);
            return box;
        }
    }
}
