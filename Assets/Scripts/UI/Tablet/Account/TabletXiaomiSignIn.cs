using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.SystemShell;

namespace RailwayRenaissance.UI.Tablet.Account
{
    /// <summary>
    /// “使用小米账号登录”：应用（例如音乐）向系统申请登录时弹出的悬浮窗，交互参照常见的“用某账号登录”弹窗——
    /// 先选账号，再确认授权范围。账号只有本机这一个（见 <see cref="AccountService"/>），授权记录只保存在本机，不联网。
    /// 标题栏可以拖动，点遮罩或 ✕ 取消。
    /// </summary>
    public static class TabletXiaomiSignIn
    {
        private const string GrantKey = "TabletXiaomiSignInApps";
        private static readonly Color Orange = new Color(1f, 0.41f, 0.0f, 1f);
        private static readonly Color Blue = new Color(0.10f, 0.45f, 0.91f, 1f);
        private static readonly Color Ink = new Color(0.12f, 0.12f, 0.14f, 1f);
        private static readonly Color Soft = new Color(0.40f, 0.42f, 0.46f, 1f);
        private static readonly Color Line = new Color(0.86f, 0.87f, 0.89f, 1f);
        public static event Action Changed;

        // ---------- 授权记录 ----------

        public static bool IsSignedIn(string appId)
        {
            if (!AccountService.IsRegistered || string.IsNullOrEmpty(appId)) return false;
            return Load().Contains(appId);
        }

        public static void SignOut(string appId)
        {
            var list = Load();
            if (!list.Remove(appId)) return;
            Save(list);
            Changed?.Invoke();
        }

        private static List<string> Load()
        {
            var list = new List<string>();
            string saved = null;
            try { saved = PlayerPrefs.GetString(GrantKey, string.Empty); } catch { }
            if (!string.IsNullOrEmpty(saved))
                foreach (var id in saved.Split(',')) if (id.Length > 0 && !list.Contains(id)) list.Add(id);
            return list;
        }

        private static void Save(List<string> list)
        {
            try { PlayerPrefs.SetString(GrantKey, string.Join(",", list)); PlayerPrefs.Save(); } catch { }
        }

        // ---------- 悬浮窗 ----------

        /// <summary>弹出登录悬浮窗；host 只用来找到所在界面，结果通过 done(是否登录成功) 回调。返回窗口的遮罩层。</summary>
        public static VisualElement Request(VisualElement host, string appId, string appName, Color appColor, Action<bool> done)
        {
            // 挂到宿主所在界面的最外层（紧贴面板根的那一层），这样悬浮窗盖住整个平板屏幕
            VisualElement root = host;
            while (root != null && root.parent != null && root.parent != root.panel?.visualTree) root = root.parent;
            if (root == null) { done?.Invoke(false); return null; }

            var font = TabletFonts.Ui();
            var fontBold = TabletFonts.UiBold();
            bool finished = false;

            var overlay = new VisualElement { name = "tablet-xiaomi-signin-overlay" };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0; overlay.style.top = 0; overlay.style.right = 0; overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.42f);
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;
            overlay.pickingMode = PickingMode.Position;
            overlay.style.opacity = 0f;
            overlay.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity") };
            overlay.style.transitionDuration = new List<TimeValue> { new TimeValue(TabletTheme.MotionMs(160), TimeUnit.Millisecond) };

            var card = new VisualElement { name = "tablet-xiaomi-signin-window" };
            card.style.width = 400;
            card.style.backgroundColor = Color.white;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = 28;
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 28;
            card.style.paddingLeft = 32; card.style.paddingRight = 32; card.style.paddingBottom = 26; card.style.paddingTop = 0;
            card.style.scale = new Scale(new Vector3(0.94f, 0.94f, 1f));
            card.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("scale") };
            card.style.transitionDuration = new List<TimeValue> { new TimeValue(TabletTheme.MotionMs(200), TimeUnit.Millisecond) };
            card.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseOut) };
            overlay.Add(card);

            void Finish(bool ok)
            {
                if (finished) return;
                finished = true;
                overlay.style.opacity = 0f;
                card.style.scale = new Scale(new Vector3(0.96f, 0.96f, 1f));
                overlay.schedule.Execute(() => overlay.RemoveFromHierarchy()).StartingIn(Mathf.Max(30, TabletTheme.MotionMs(170)));
                done?.Invoke(ok);
            }

            Label Text(string s, int size, bool bold, Color color, TextAnchor align = TextAnchor.MiddleLeft)
            {
                var l = new Label(s) { pickingMode = PickingMode.Ignore };
                l.style.fontSize = size; l.style.color = color; l.style.unityTextAlign = align;
                l.style.whiteSpace = WhiteSpace.Normal;
                l.style.marginLeft = 0; l.style.marginRight = 0; l.style.marginTop = 0; l.style.marginBottom = 0;
                l.style.paddingLeft = 0; l.style.paddingRight = 0; l.style.paddingTop = 0; l.style.paddingBottom = 0;
                var f = bold ? fontBold : font;
                if (f != null) l.style.unityFontDefinition = FontDefinition.FromFont(f);
                return l;
            }

            VisualElement Avatar(AccountProfile p, float size)
            {
                var a = TabletAvatar.Build(p, size);
                a.style.flexShrink = 0;
                return a;
            }

            Button TextButton(string label, bool primary, Action click)
            {
                var b = new Button(click) { text = label };
                b.style.height = 44; b.style.minWidth = 92;
                b.style.marginLeft = 8; b.style.marginRight = 0; b.style.marginTop = 0; b.style.marginBottom = 0;
                b.style.paddingLeft = 22; b.style.paddingRight = 22; b.style.paddingTop = 0; b.style.paddingBottom = 0;
                b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 0;
                b.style.borderTopLeftRadius = b.style.borderTopRightRadius = 22;
                b.style.borderBottomLeftRadius = b.style.borderBottomRightRadius = 22;
                b.style.backgroundColor = primary ? Blue : Color.clear;
                b.style.color = primary ? Color.white : Blue;
                b.style.fontSize = 15;
                if (fontBold != null) b.style.unityFontDefinition = FontDefinition.FromFont(fontBold);
                return b;
            }

            // ---- 标题栏：小米标识 + 关闭；整条可拖动 ----
            var bar = new VisualElement { name = "tablet-xiaomi-signin-bar" };
            bar.style.height = 64;
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.justifyContent = Justify.SpaceBetween;
            bar.style.marginLeft = -32; bar.style.marginRight = -32;
            bar.style.paddingLeft = 32; bar.style.paddingRight = 14;
            var brand = new VisualElement { pickingMode = PickingMode.Ignore };
            brand.style.flexDirection = FlexDirection.Row; brand.style.alignItems = Align.Center;
            var logo = Text("mi", 15, true, Color.white, TextAnchor.MiddleCenter);
            logo.style.width = 30; logo.style.height = 30; logo.style.backgroundColor = Orange;
            logo.style.borderTopLeftRadius = logo.style.borderTopRightRadius = 8;
            logo.style.borderBottomLeftRadius = logo.style.borderBottomRightRadius = 8;
            brand.Add(logo);
            var brandName = Text("小米账号", 16, true, Ink);
            brandName.style.marginLeft = 10;
            brand.Add(brandName);
            bar.Add(brand);
            var close = new Button(() => Finish(false)) { name = "tablet-xiaomi-signin-close", tooltip = "取消" };
            close.style.width = 36; close.style.height = 36;
            close.style.marginLeft = 0; close.style.marginRight = 0; close.style.marginTop = 0; close.style.marginBottom = 0;
            close.style.paddingLeft = close.style.paddingRight = close.style.paddingTop = close.style.paddingBottom = 0;
            close.style.backgroundColor = Color.clear;
            close.style.borderTopWidth = close.style.borderBottomWidth = close.style.borderLeftWidth = close.style.borderRightWidth = 0;
            close.style.alignItems = Align.Center; close.style.justifyContent = Justify.Center;
            close.Add(new TabletIconElement(TabletIconKind.Close, 16, Soft) { pickingMode = PickingMode.Ignore });
            close.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            bar.Add(close);
            card.Add(bar);

            Vector2 dragStart = Vector2.zero, dragOrigin = Vector2.zero; bool dragging = false;
            bar.RegisterCallback<PointerDownEvent>(e =>
            {
                dragging = true; dragStart = e.position;
                dragOrigin = new Vector2(card.resolvedStyle.translate.x, card.resolvedStyle.translate.y);
                bar.CapturePointer(e.pointerId);
            });
            bar.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!dragging) return;
                Vector2 p = dragOrigin + ((Vector2)e.position - dragStart);
                card.style.translate = new Translate(p.x, p.y);
            });
            bar.RegisterCallback<PointerUpEvent>(e => { dragging = false; if (bar.HasPointerCapture(e.pointerId)) bar.ReleasePointer(e.pointerId); });

            var body = new VisualElement { name = "tablet-xiaomi-signin-body" };
            card.Add(body);

            void ShowChooser()
            {
                body.Clear();
                var p = AccountService.Profile;
                var title = Text("使用小米账号登录", 24, true, Ink, TextAnchor.MiddleCenter);
                title.style.marginTop = 6;
                body.Add(title);
                var sub = Text(p == null ? "这台平板还没有小米账号" : "选择账号以继续使用「" + appName + "」", 14, false, Soft, TextAnchor.MiddleCenter);
                sub.style.marginTop = 8; sub.style.marginBottom = 22;
                body.Add(sub);

                if (p == null)
                {
                    var tip = Text("请先在“设置 → 小米账号”里注册，之后各个应用都可以用它登录。", 14, false, Ink);
                    tip.style.marginBottom = 22;
                    body.Add(tip);
                    var rowEnd = new VisualElement(); rowEnd.style.flexDirection = FlexDirection.Row; rowEnd.style.justifyContent = Justify.FlexEnd;
                    rowEnd.Add(TextButton("知道了", true, () => Finish(false)));
                    body.Add(rowEnd);
                    return;
                }

                var row = new Button(() => ShowConsent(p)) { name = "tablet-xiaomi-signin-account" };
                row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
                row.style.height = 72;
                row.style.marginLeft = -32; row.style.marginRight = -32; row.style.marginTop = 0; row.style.marginBottom = 0;
                row.style.paddingLeft = 32; row.style.paddingRight = 32;
                row.style.backgroundColor = Color.clear;
                row.style.borderLeftWidth = row.style.borderRightWidth = 0;
                row.style.borderTopWidth = row.style.borderBottomWidth = 1;
                row.style.borderTopColor = row.style.borderBottomColor = Line;
                row.style.borderTopLeftRadius = row.style.borderTopRightRadius = row.style.borderBottomLeftRadius = row.style.borderBottomRightRadius = 0;
                var av = Avatar(p, 40); av.style.marginRight = 14; av.pickingMode = PickingMode.Ignore;
                row.Add(av);
                var col = new VisualElement { pickingMode = PickingMode.Ignore }; col.style.flexGrow = 1;
                col.Add(Text(p.nickname, 16, true, Ink));
                var phone = Text(AccountService.MaskedPhone(p), 13, false, Soft); phone.style.marginTop = 2;
                col.Add(phone);
                row.Add(col);
                body.Add(row);

                var foot = Text("继续即表示你同意把昵称与头像提供给「" + appName + "」。账号资料只保存在这台平板上。", 12, false, Soft);
                foot.style.marginTop = 18;
                body.Add(foot);
            }

            void ShowConsent(AccountProfile p)
            {
                body.Clear();
                var chip = new VisualElement { pickingMode = PickingMode.Ignore };
                chip.style.flexDirection = FlexDirection.Row; chip.style.alignItems = Align.Center; chip.style.alignSelf = Align.Center;
                chip.style.paddingLeft = 6; chip.style.paddingRight = 16; chip.style.paddingTop = 6; chip.style.paddingBottom = 6;
                chip.style.borderTopWidth = chip.style.borderBottomWidth = chip.style.borderLeftWidth = chip.style.borderRightWidth = 1;
                chip.style.borderTopColor = chip.style.borderBottomColor = chip.style.borderLeftColor = chip.style.borderRightColor = Line;
                chip.style.borderTopLeftRadius = chip.style.borderTopRightRadius = chip.style.borderBottomLeftRadius = chip.style.borderBottomRightRadius = 22;
                var av = Avatar(p, 28); av.style.marginRight = 10; chip.Add(av);
                chip.Add(Text(p.nickname, 14, false, Ink));
                chip.style.marginTop = 6;
                body.Add(chip);

                var appBadge = Text(appName.Substring(0, 1), 22, true, Color.white, TextAnchor.MiddleCenter);
                appBadge.style.width = 52; appBadge.style.height = 52; appBadge.style.alignSelf = Align.Center; appBadge.style.marginTop = 20;
                appBadge.style.backgroundColor = appColor;
                appBadge.style.borderTopLeftRadius = appBadge.style.borderTopRightRadius = 14;
                appBadge.style.borderBottomLeftRadius = appBadge.style.borderBottomRightRadius = 14;
                body.Add(appBadge);

                var title = Text("「" + appName + "」想要访问你的小米账号", 20, true, Ink, TextAnchor.MiddleCenter);
                title.style.marginTop = 14; title.style.marginBottom = 18;
                body.Add(title);

                var lead = Text("这会让「" + appName + "」：", 14, false, Soft);
                lead.style.marginBottom = 10;
                body.Add(lead);
                foreach (var s in new[] { "查看你的昵称和头像", "在这台平板上记住你的登录状态" })
                {
                    var item = new VisualElement { pickingMode = PickingMode.Ignore };
                    item.style.flexDirection = FlexDirection.Row; item.style.alignItems = Align.Center; item.style.marginBottom = 8;
                    item.Add(new TabletIconElement(TabletIconKind.Check, 16, Blue) { pickingMode = PickingMode.Ignore });
                    var t = Text(s, 14, false, Ink); t.style.marginLeft = 10;
                    item.Add(t);
                    body.Add(item);
                }
                var note = Text("不会共享你的手机号和密码，也不会发送到其他地方。随时可以在应用里退出登录。", 12, false, Soft);
                note.style.marginTop = 8; note.style.marginBottom = 22;
                body.Add(note);

                var buttons = new VisualElement(); buttons.style.flexDirection = FlexDirection.Row; buttons.style.justifyContent = Justify.FlexEnd;
                buttons.Add(TextButton("取消", false, () => Finish(false)));
                var allow = TextButton("允许", true, () =>
                {
                    var list = Load();
                    if (!list.Contains(appId)) { list.Add(appId); Save(list); }
                    Changed?.Invoke();
                    Finish(true);
                });
                allow.name = "tablet-xiaomi-signin-allow";
                buttons.Add(allow);
                body.Add(buttons);
            }

            ShowChooser();

            overlay.RegisterCallback<ClickEvent>(e => { if (e.target == overlay) Finish(false); });
            root.Add(overlay);
            overlay.BringToFront();
            overlay.schedule.Execute(() => { overlay.style.opacity = 1f; card.style.scale = new Scale(Vector3.one); }).StartingIn(16);
            return overlay;
        }
    }
}
