using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;
using RailwayRenaissance.UI.Tablet.Paper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 平板应用「巡线」的 2D 俯视自由探索层。
    /// 屏幕布局：左上状态胶囊 · 右上 地图/菜单 图标键 · 底部物品栏 · 右下能量条与加速键 · 左下虚拟摇杆（可开关）。
    /// 经营页（调度/人员/订单/市场/财务）统一收进菜单面板；返回桌面也在菜单里，不再占据角落。
    /// </summary>
    public static class TabletExploreAppUI
    {
        public static ExploreMapView ActiveView { get; private set; }

        private static readonly Color Panel = new Color(0.985f, 0.972f, 0.935f, 0.94f);
        private static readonly Color PanelLine = new Color(0.42f, 0.32f, 0.20f, 0.32f);
        private static readonly Color Ink = new Color(0.17f, 0.14f, 0.11f, 1f);
        private static readonly Color InkSoft = new Color(0.17f, 0.14f, 0.11f, 0.62f);
        private static readonly Color Gold = new Color(0.80f, 0.52f, 0.12f, 1f);

        private struct Item { public string name; public TabletIconKind icon; public int count; public Color color; }

        private static readonly Item[] Items =
        {
            new Item { name = "扳手",   icon = TabletIconKind.Gear,     count = 1, color = new Color(0.38f, 0.44f, 0.52f) },
            new Item { name = "信号灯", icon = TabletIconKind.Bulb,     count = 1, color = new Color(0.85f, 0.62f, 0.10f) },
            new Item { name = "记事本", icon = TabletIconKind.Note,     count = 1, color = new Color(0.55f, 0.45f, 0.30f) },
            new Item { name = "车票",   icon = TabletIconKind.Bookmark, count = 12, color = new Color(0.20f, 0.50f, 0.74f) },
            new Item { name = "茶叶",   icon = TabletIconKind.Sprout,   count = 0, color = new Color(0.28f, 0.58f, 0.26f) },
        };

        public static void Build(VisualElement win, Action<string> onAction, Action onEndDay, Action<string> toast, Action<string> launchApp = null, Action openStory = null, Action openAffairs = null)
        {
            win.style.flexDirection = FlexDirection.Column;

            var host = new VisualElement { name = "explore-root" };
            host.style.flexGrow = 1;
            host.style.position = Position.Relative;
            host.style.overflow = Overflow.Hidden;
            win.Add(host);

            var view = new ExploreMapView();
            view.OnAction = onAction;
            host.Add(view);
            ActiveView = view;

            var status = BuildStatus(view, out var updateStatus);
            Fade(status, 0.3f);
            host.Add(status);

            var actions = new VisualElement { name = "explore-actions", pickingMode = PickingMode.Ignore };
            actions.style.position = Position.Absolute;
            actions.style.top = 46; actions.style.right = 18;
            actions.style.flexDirection = FlexDirection.Row;
            actions.Add(IconButton(TabletIconKind.Map, "地图", () => ToggleMapRef?.Invoke()));
            actions.Add(IconButton(TabletIconKind.Gear, "系统", () => ToggleMenuRef?.Invoke()));
            host.Add(actions);

            // ===== 面板（地图 / 菜单）=====
            var mapOverlay = BuildMapOverlay(view, () => { });
            var menuOverlay = new VisualElement { name = "explore-menu" };
            host.Add(mapOverlay);
            host.Add(menuOverlay);
            // 面板淡入淡出：不是瞬间出现/消失
            Fade(mapOverlay, 0.16f); Fade(menuOverlay, 0.16f);
            mapOverlay.style.display = DisplayStyle.None; mapOverlay.style.opacity = 0f;
            menuOverlay.style.display = DisplayStyle.None; menuOverlay.style.opacity = 0f;
            bool IsOpen(VisualElement o) => o.style.display == DisplayStyle.Flex && o.style.opacity.value > 0.5f;
            void ShowPanel(VisualElement o)
            {
                o.style.display = DisplayStyle.Flex; o.style.opacity = 0f;
                o.schedule.Execute(() => o.style.opacity = 1f).ExecuteLater(10);
            }
            void HidePanel(VisualElement o)
            {
                if (o.style.display == DisplayStyle.None) return;
                o.style.opacity = 0f;
                o.schedule.Execute(() => { if (o.style.opacity.value < 0.05f) o.style.display = DisplayStyle.None; }).ExecuteLater(220);
            }
            VisualElement journalOverlay = null;
            void CloseAll() { HidePanel(mapOverlay); HidePanel(menuOverlay); if (journalOverlay != null) HidePanel(journalOverlay); }
            void ToggleMap() { bool was = IsOpen(mapOverlay); CloseAll(); if (!was) { ShowPanel(mapOverlay); (mapOverlay.userData as Action)?.Invoke(); } }
            void ToggleMenu() { bool was = IsOpen(menuOverlay); CloseAll(); if (!was) ShowPanel(menuOverlay); }
            ToggleMapRef = ToggleMap; ToggleMenuRef = ToggleMenu;
            mapOverlay.Q<VisualElement>("map-pick-close")?.RegisterCallback<ClickEvent>(_ => CloseAll());
            view.OpenMapRequested += ToggleMap;
            view.OnMapPicked = CloseAll;


            // ===== RDA 主体面板：经营页（调度/人员/订单/市场/财务）作为场景内的大面板打开 =====
            var rdaOverlay = new VisualElement { name = "explore-rda" };
            rdaOverlay.style.position = Position.Absolute;
            rdaOverlay.style.left = 0; rdaOverlay.style.right = 0; rdaOverlay.style.top = 0; rdaOverlay.style.bottom = 0;
            rdaOverlay.style.backgroundColor = new Color(0.08f, 0.07f, 0.05f, 0.42f);
            rdaOverlay.style.alignItems = Align.Center; rdaOverlay.style.justifyContent = Justify.Center;
            Fade(rdaOverlay, 0.18f);
            rdaOverlay.style.display = DisplayStyle.None; rdaOverlay.style.opacity = 0f;
            var rdaCard = new VisualElement();
            rdaCard.style.width = Length.Percent(94); rdaCard.style.height = Length.Percent(90);
            rdaCard.style.marginTop = 24;
            rdaCard.style.flexDirection = FlexDirection.Column;
            rdaCard.style.backgroundColor = new Color(0.975f, 0.962f, 0.925f, 0.99f);
            Round(rdaCard, 28);
            rdaCard.style.overflow = Overflow.Hidden;
            rdaCard.generateVisualContent += ctx => DrawDeco(ctx, rdaCard, 28f);
            var rdaHead = new VisualElement();
            rdaHead.style.flexDirection = FlexDirection.Row; rdaHead.style.alignItems = Align.Center;
            rdaHead.style.height = 56; rdaHead.style.paddingLeft = 26; rdaHead.style.paddingRight = 16;
            var rdaTitle = new Label("RDA") { pickingMode = PickingMode.Ignore };
            rdaTitle.style.fontSize = 22; rdaTitle.style.color = Ink; rdaTitle.style.unityFontDefinition = FdBold();
            var rdaSub = new Label("铁路行车调度助手") { pickingMode = PickingMode.Ignore };
            rdaSub.style.fontSize = 13; rdaSub.style.color = InkSoft; rdaSub.style.unityFontDefinition = Fd(); rdaSub.style.marginLeft = 10; rdaSub.style.flexGrow = 1;
            var rdaClose = new VisualElement();
            rdaClose.style.width = 38; rdaClose.style.height = 38; rdaClose.style.alignItems = Align.Center; rdaClose.style.justifyContent = Justify.Center;
            rdaClose.style.backgroundColor = new Color(0.45f, 0.32f, 0.16f, 0.12f); Round(rdaClose, 19);
            rdaClose.Add(new TabletIconElement(TabletIconKind.Close, 18, Ink));
            rdaHead.Add(rdaTitle); rdaHead.Add(rdaSub); rdaHead.Add(rdaClose);
            var rdaBody = new VisualElement { name = "explore-rda-body" };
            rdaBody.style.flexGrow = 1; rdaBody.style.minHeight = 0; rdaBody.style.paddingBottom = 14;
            rdaCard.Add(rdaHead); rdaCard.Add(rdaBody);
            rdaOverlay.Add(rdaCard);
            host.Add(rdaOverlay);
            void CloseRda() { HidePanel(rdaOverlay); }
            rdaClose.RegisterCallback<ClickEvent>(_ => CloseRda());
            rdaOverlay.RegisterCallback<ClickEvent>(e => { if (e.target == rdaOverlay) CloseRda(); });
            void OpenRda(int l1, int l2, string where = null)
            {
                CloseAll();
                TabletRDAAppUI.OpenPage(l1, l2, where != null);
                rdaTitle.text = where ?? "RDA";
                rdaSub.text = where != null ? "在此处理本地业务" : "铁路行车调度助手";
                rdaBody.Clear();
                TabletRDAAppUI.BuildRDAApp(rdaBody, name => { if (name == "巡线" || name == "RDA") CloseRda(); else launchApp?.Invoke(name); }, openStory, openAffairs, view.OnToast);
                ShowPanel(rdaOverlay);
            }
            TabletRDAAppUI.OnDayEnded = () => view.ResetDay();
            view.OpenRdaRequested = (a, b) => OpenRda(a, b);
            void Route(string id)
            {
                switch (id)
                {
                    // 经营功能只能在地图对应地点办理（走到地点按 E / 点击）；总览为随身只读
                    case "dispatch_desk": OpenRda(1, 0, "调度台 · 发车方案"); break;
                    case "strategy_board": OpenRda(1, 1, "策略板 · 经营策略"); break;
                    case "hall_board": OpenRda(3, 0, "委托公告板 · 在办订单"); break;
                    case "tea": OpenRda(3, 0, "茶摊 · 货主委托"); break;
                    case "garage": OpenRda(2, 0, "车库 · 人员"); break;
                    case "board": OpenRda(4, 0, "公告栏 · 沙能动向"); break;
                    case "finance": OpenRda(5, 0, "账房 · 财务"); break;
                    case "rest": onEndDay?.Invoke(); view.ResetDay(); OpenRda(0, 0, "宿舍 · 结算"); break;
                    case "overview": OpenRda(0, 0, "总览 · 只读"); break;
                    default: onAction?.Invoke(id); break;
                }
            }
            view.OnAction = Route;

            BuildSystemMenu(menuOverlay, view, Route, CloseAll);

            // ===== 底部：物品栏 =====
            var hotbar = new VisualElement { name = "explore-hotbar" };
            hotbar.style.position = Position.Absolute;
            hotbar.style.left = 0; hotbar.style.right = 0; hotbar.style.bottom = 22;
            hotbar.style.alignItems = Align.Center;
            hotbar.pickingMode = PickingMode.Ignore;
            host.Add(hotbar);

            // 悬浮交互提示：跟随目标头顶，键帽 + 名称，淡入淡出
            var prompt = new VisualElement { name = "explore-prompt" };
            prompt.pickingMode = PickingMode.Ignore;
            prompt.style.position = Position.Absolute;
            prompt.style.flexDirection = FlexDirection.Row; prompt.style.alignItems = Align.Center;
            StyleCard(prompt, 18, 8, 6);
            prompt.style.translate = new Translate(Length.Percent(-50), Length.Percent(-100));
            prompt.style.opacity = 0f;
            Fade(prompt, 0.14f);
            var keycap = new Label("E") { pickingMode = PickingMode.Ignore };
            keycap.style.width = 26; keycap.style.height = 26; keycap.style.unityTextAlign = TextAnchor.MiddleCenter;
            keycap.style.fontSize = 15; keycap.style.color = new Color(0.10f, 0.12f, 0.16f); keycap.style.unityFontDefinition = FdBold();
            keycap.style.backgroundColor = Gold; Round(keycap, 8); keycap.style.marginRight = 8;
            prompt.Add(keycap);
            var promptText = new Label { pickingMode = PickingMode.Ignore };
            promptText.style.fontSize = 15; promptText.style.color = Ink; promptText.style.unityFontDefinition = Fd();
            promptText.style.marginRight = 6;
            prompt.Add(promptText);
            host.Add(prompt);
            bool touchUi = Application.isMobilePlatform || Touchscreen.current != null;
            if (touchUi) keycap.text = "点";
            view.PromptChanged += text =>
            {
                if (!string.IsNullOrEmpty(text)) promptText.text = text;
                prompt.style.opacity = string.IsNullOrEmpty(text) ? 0f : 1f;
            };
            host.schedule.Execute(() =>
            {
                Vector2 p = view.WorldToView(view.PromptWorld);
                prompt.style.left = p.x; prompt.style.top = p.y;
            }).Every(16);

            // 悬浮提示条（顶部居中，替代系统 Toast）
            var toasts = new VisualElement { name = "explore-toasts" };
            toasts.pickingMode = PickingMode.Ignore;
            toasts.style.position = Position.Absolute;
            toasts.style.left = 0; toasts.style.right = 0; toasts.style.top = 52;
            toasts.style.alignItems = Align.Center;
            host.Add(toasts);
            view.OnToast = msg =>
            {
                var t = new Label(msg) { pickingMode = PickingMode.Ignore };
                t.style.fontSize = 16; t.style.color = Ink; t.style.unityFontDefinition = Fd();
                t.style.marginBottom = 6; t.style.maxWidth = 640; t.style.whiteSpace = WhiteSpace.Normal;
                StyleCard(t, 16, 18, 9);
                t.style.opacity = 0f; Fade(t, 0.2f);
                toasts.Add(t);
                t.schedule.Execute(() => t.style.opacity = 1f).ExecuteLater(10);
                t.schedule.Execute(() => t.style.opacity = 0f).ExecuteLater(2600);
                t.schedule.Execute(() => t.RemoveFromHierarchy()).ExecuteLater(2900);
                while (toasts.childCount > 3) toasts.RemoveAt(0);
            };

            // 对话框：NPC 说话时出现在物品栏上方，点击或数秒后收起
            var dlg = new VisualElement { name = "explore-dialogue" };
            dlg.style.position = Position.Absolute;
            dlg.style.left = 0; dlg.style.right = 0; dlg.style.bottom = 150;
            dlg.style.alignItems = Align.Center;
            dlg.pickingMode = PickingMode.Ignore;
            dlg.style.opacity = 0f; Fade(dlg, 0.18f);
            var dlgCard = new VisualElement();
            dlgCard.style.flexDirection = FlexDirection.Row; dlgCard.style.alignItems = Align.Center;
            dlgCard.style.width = 700;
            PaperUI.StylePanel(dlgCard, PaperTokens.RadiusL, 22, 16, true, true);
            var avatar = new Label { pickingMode = PickingMode.Ignore };
            avatar.style.width = 64; avatar.style.height = 64; avatar.style.unityTextAlign = TextAnchor.MiddleCenter;
            avatar.style.fontSize = 28; avatar.style.color = Color.white; avatar.style.unityFontDefinition = FdBold();
            PaperUI.Round(avatar, 32); PaperUI.Border(avatar, 3f, PaperTokens.PaperDeep); avatar.style.marginRight = 18;
            dlgCard.Add(avatar);
            var dlgBody = new VisualElement { pickingMode = PickingMode.Ignore }; dlgBody.style.flexGrow = 1; dlgBody.style.flexShrink = 1;
            var dlgName = new Label { pickingMode = PickingMode.Ignore };
            dlgName.style.fontSize = 15; dlgName.style.color = Gold; dlgName.style.unityFontDefinition = FdBold();
            var dlgText = new Label { pickingMode = PickingMode.Ignore };
            dlgText.style.fontSize = 18; dlgText.style.color = Ink; dlgText.style.unityFontDefinition = Fd();
            dlgText.style.whiteSpace = WhiteSpace.Normal; dlgText.style.marginTop = 4;
            dlgName.style.color = PaperTokens.Amber;
            var dlgRule = PaperUI.PerfDivider(); dlgRule.style.marginTop = 4;
            dlgBody.Add(dlgName); dlgBody.Add(dlgRule); dlgBody.Add(dlgText);
            dlgCard.Add(dlgBody);
            var more = PaperUI.Text("▼", PaperTokens.FsSmall, PaperTokens.Amber); more.pickingMode = PickingMode.Ignore;
            more.style.alignSelf = Align.FlexEnd; more.style.marginLeft = 10;
            dlgCard.Add(more);
            dlg.Add(dlgCard);
            host.Add(dlg);
            IVisualElementScheduledItem dlgHide = null;
            view.OnDialogue = (who, line, col) =>
            {
                avatar.text = who.Substring(0, 1); avatar.style.backgroundColor = col;
                dlgName.text = who; dlgText.text = line;
                dlg.style.opacity = 1f;
                dlgHide?.Pause();
                dlgHide = dlg.schedule.Execute(() => dlg.style.opacity = 0f);
                dlgHide.ExecuteLater(4200);
            };

            var bar = new VisualElement { name = "explore-hotbar-slots" };
            bar.style.flexDirection = FlexDirection.Row;
            StyleCard(bar, 17, 7, 6);
            Fade(bar, 0.3f);
            hotbar.Add(bar);
            var slots = new List<VisualElement>();
            int selected = 0;
            var itemName = new Label { name = "explore-item-name" };
            itemName.pickingMode = PickingMode.Ignore;
            itemName.style.fontSize = 13; itemName.style.color = Ink; itemName.style.unityFontDefinition = Fd();
            itemName.style.marginBottom = 5;
            StyleCard(itemName, 9, 8, 2);
            void Select(int i)
            {
                selected = i;
                for (int k = 0; k < slots.Count; k++) SetSlotSelected(slots[k], k == i);
                itemName.text = i < Items.Length ? Items[i].name : "空";
                view.SelectedItem = i < Items.Length ? Items[i].name : "";
            }
            for (int i = 0; i < 9; i++)
            {
                int idx = i;
                var slot = BuildSlot(i, i < Items.Length ? (Item?)Items[i] : null);
                slot.RegisterCallback<ClickEvent>(_ => Select(idx));
                slot.RegisterCallback<PointerEnterEvent>(_ => itemName.text = idx < Items.Length ? Items[idx].name : "空");
                slot.RegisterCallback<PointerLeaveEvent>(_ => itemName.text = selected < Items.Length ? Items[selected].name : "空");
                bar.Add(slot);
                slots.Add(slot);
            }
            hotbar.Insert(0, itemName);
            Select(0);

            // ===== 右下：能量条 + 加速键 =====
            var energy = new VisualElement { name = "explore-energy" };
            energy.style.position = Position.Absolute;
            energy.style.right = 22; energy.style.bottom = 22;
            energy.style.alignItems = Align.Center;
            host.Add(energy);

            var sprintBtn = new VisualElement { name = "explore-sprint" };
            sprintBtn.style.width = 84; sprintBtn.style.height = 84;
            StyleCard(sprintBtn, 42, 0, 0);
            sprintBtn.style.alignItems = Align.Center; sprintBtn.style.justifyContent = Justify.Center;
            sprintBtn.Add(new TabletIconElement(TabletIconKind.Bolt, 30, Gold));
            var sprintTxt = new Label("加速") { pickingMode = PickingMode.Ignore };
            sprintTxt.style.fontSize = 13; sprintTxt.style.color = Ink; sprintTxt.style.unityFontDefinition = Fd();
            sprintBtn.Add(sprintTxt);
            sprintBtn.RegisterCallback<PointerDownEvent>(e => { sprintBtn.CapturePointer(e.pointerId); view.SprintHeld = true; sprintBtn.style.backgroundColor = new Color(0.78f, 0.5f, 0.16f, 0.9f); e.StopPropagation(); });
            sprintBtn.RegisterCallback<PointerUpEvent>(e => { if (sprintBtn.HasPointerCapture(e.pointerId)) sprintBtn.ReleasePointer(e.pointerId); });
            sprintBtn.RegisterCallback<PointerCaptureOutEvent>(_ => { view.SprintHeld = false; sprintBtn.style.backgroundColor = Panel; });
            energy.Add(sprintBtn);

            var track = new VisualElement { name = "explore-energy-track" };
            track.style.width = 96; track.style.height = 24; track.style.marginTop = 6;
            float energyFrac = 1f; Color energyCol = new Color(0.36f, 0.72f, 0.30f);
            track.generateVisualContent += ctx => PaperDraw.Ruler(ctx.painter2D, 2f, track.layout.width - 2f, 12f, energyFrac, energyCol, energyCol, false);
            energy.Add(track);
            var fill = new VisualElement { name = "explore-energy-fill" };   // 仅保留名称供评审/旧逻辑查询，不显示
            fill.style.display = DisplayStyle.None;
            track.Add(fill);
            var shiftHint = new Label("Shift 加速") { pickingMode = PickingMode.Ignore };
            shiftHint.style.fontSize = 11; shiftHint.style.color = InkSoft; shiftHint.style.unityFontDefinition = Fd();
            shiftHint.style.marginTop = 4;
            energy.Add(shiftHint);

            // ===== 左下：虚拟摇杆（移动端默认开，Windows 可在菜单里开启）=====
            var joy = BuildJoystick(view);
            host.Add(joy);
            bool joyOn = Application.isMobilePlatform || Touchscreen.current != null || PlayerPrefs.GetInt("explore_joystick", 0) == 1;
            void ApplyJoy(bool on)
            {
                joyOn = on;
                joy.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                sprintBtn.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                shiftHint.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
                try { PlayerPrefs.SetInt("explore_joystick", on ? 1 : 0); } catch { }
                if (!on) { view.JoyDir = Vector2.zero; view.SprintHeld = false; }
            }
            ApplyJoy(joyOn);
            menuOverlay.userData = (Action)(() => ApplyJoy(!joyOn));

            // ===== 任务：左侧追踪卡 + 站长手记（任务簿）=====
            ExploreQuests.EnsureToday();
            var journalBody = default(VisualElement);
            journalOverlay = PaperUI.Sheet("站长手记", "今日任务 · 每天重置", out journalBody, () => HidePanel(journalOverlay), 62f, 82f);
            journalOverlay.name = "explore-journal";
            host.Add(journalOverlay);
            void RebuildJournal()
            {
                journalBody.Clear();
                var scroll = new ScrollView(ScrollViewMode.Vertical);
                scroll.style.flexGrow = 1; scroll.style.paddingLeft = 24; scroll.style.paddingRight = 24; scroll.style.paddingTop = 14;
                journalBody.Add(scroll);
                scroll.Add(PaperUI.Text($"今天已完成 {ExploreQuests.CompletedToday} / {ExploreQuests.Active.Count}", PaperTokens.FsSmall, PaperTokens.InkSoft));
                foreach (var q in ExploreQuests.Active)
                {
                    var card = PaperUI.Card(PaperTokens.S3, false, q.Done ? PaperTokens.Good : PaperTokens.Amber);
                    card.style.marginTop = 10;
                    var head = new VisualElement(); head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center;
                    var t = PaperUI.Text(q.title, PaperTokens.FsSub, q.Done ? PaperTokens.InkSoft : PaperTokens.Ink, true); t.style.flexGrow = 1;
                    head.Add(t); head.Add(PaperUI.Tag(q.giver, PaperTokens.Amber));
                    if (q.Done) { var d = PaperUI.Tag("已完成", PaperTokens.Good); d.style.marginLeft = 6; head.Add(d); }
                    card.Add(head);
                    var desc = PaperUI.Text(q.desc, PaperTokens.FsSmall, PaperTokens.InkSoft); desc.style.whiteSpace = WhiteSpace.Normal; desc.style.marginTop = 4;
                    card.Add(desc);
                    foreach (var o in q.objectives)
                    {
                        var r = PaperUI.Text((o.Done ? "✓  " : "○  ") + o.text + (o.need > 1 ? $"（{o.have}/{o.need}）" : ""), PaperTokens.FsBody, o.Done ? PaperTokens.Good : PaperTokens.Ink);
                        r.style.marginTop = 4; card.Add(r);
                    }
                    if (q.RewardText.Length > 0) { var rw = PaperUI.Text("奖励：" + q.RewardText, PaperTokens.FsSmall, PaperTokens.InkSoft); rw.style.marginTop = 6; card.Add(rw); }
                    scroll.Add(card);
                }
            }
            void OpenJournal() { CloseAll(); RebuildJournal(); ShowPanel(journalOverlay); }

            var tracker = PaperUI.Panel(PaperTokens.RadiusM, 12, 7, true, false);
            tracker.name = "explore-tracker";
            tracker.style.position = Position.Absolute; tracker.style.left = 18; tracker.style.top = 128; tracker.style.width = 292;
            tracker.pickingMode = PickingMode.Position;
            tracker.RegisterCallback<ClickEvent>(_ => OpenJournal());
            host.Add(tracker);
            void RefreshTracker()
            {
                tracker.Clear();
                var hd = new VisualElement { pickingMode = PickingMode.Ignore }; hd.style.flexDirection = FlexDirection.Row; hd.style.alignItems = Align.Center;
                var tt = PaperUI.Text("站长手记", PaperTokens.FsSmall, PaperTokens.InkSoft, true); tt.style.flexGrow = 1; tt.pickingMode = PickingMode.Ignore;
                var ct = PaperUI.Text($"{ExploreQuests.CompletedToday}/{ExploreQuests.Active.Count}", PaperTokens.FsSmall, PaperTokens.Amber, true); ct.pickingMode = PickingMode.Ignore;
                hd.Add(tt); hd.Add(ct); tracker.Add(hd);
                var q = ExploreQuests.First(true);
                if (q == null) { var ok = PaperUI.Text("今天的事都办完了", PaperTokens.FsBody, PaperTokens.Good, true); ok.style.marginTop = 4; ok.pickingMode = PickingMode.Ignore; tracker.Add(ok); return; }
                var title = PaperUI.Text(q.title, PaperTokens.FsBody, PaperTokens.Ink, true); title.style.marginTop = 4; title.pickingMode = PickingMode.Ignore; tracker.Add(title);
                var objective = q.objectives.FirstOrDefault(o => !o.Done);
                if (objective != null)
                {
                    var l = PaperUI.Text(objective.text, PaperTokens.FsSmall, PaperTokens.InkSoft);
                    l.style.whiteSpace = WhiteSpace.Normal;
                    l.style.marginTop = 2; l.pickingMode = PickingMode.Ignore; tracker.Add(l);
                }
            }
            RefreshTracker();
            Action onQuestChanged = () => { RefreshTracker(); if (journalOverlay.style.display == DisplayStyle.Flex) RebuildJournal(); };
            Action<Quest> onQuestDone = q => view.OnToast?.Invoke($"任务完成：{q.title}　{q.RewardText}");
            ExploreQuests.Changed += onQuestChanged; ExploreQuests.Completed += onQuestDone;
            host.RegisterCallback<DetachFromPanelEvent>(_ => { ExploreQuests.Changed -= onQuestChanged; ExploreQuests.Completed -= onQuestDone; });

            // 大面板盖在所有常驻 HUD 之上
            mapOverlay.BringToFront(); rdaOverlay.BringToFront(); menuOverlay.BringToFront(); journalOverlay.BringToFront();

            host.schedule.Execute(() =>
            {
                updateStatus();
                // 走动时 HUD 退到半透明，停下来再亮起，不遮挡画面
                prompt.style.display = (IsOpen(mapOverlay) || IsOpen(menuOverlay) || IsOpen(rdaOverlay)) ? DisplayStyle.None : DisplayStyle.Flex;
                float a = view.IsMoving ? 0.5f : 1f;
                status.style.opacity = a; bar.style.opacity = a; tracker.style.opacity = a;
                tracker.style.display = prompt.style.display;
                float f = Mathf.Clamp01(view.Stamina / ExploreMapView.MaxStamina);
                energyFrac = f;
                energyCol = view.SprintLocked ? PaperTokens.Bad : f < 0.35f ? PaperTokens.Warn : PaperTokens.Good;
                track.MarkDirtyRepaint();
            }).Every(100);
        }

        // ---------- 分区：系统菜单（只放系统项：摇杆开关 / 返回桌面）----------
        private static void BuildSystemMenu(VisualElement overlay, ExploreMapView view, Action<string> onAction, Action close)
        {
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0; overlay.style.right = 0; overlay.style.top = 0; overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.0f);
            overlay.style.alignItems = Align.FlexEnd; overlay.style.justifyContent = Justify.FlexStart;
            overlay.RegisterCallback<ClickEvent>(e => { if (e.target == overlay) close(); });

            var card = new VisualElement();
            card.style.marginTop = 106; card.style.marginRight = 18;
            card.style.width = 236;
            StyleCard(card, 20, 10, 10);
            overlay.Add(card);

            void Row(TabletIconKind icon, string text, Func<string> state, Action click)
            {
                var r = new VisualElement();
                r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center;
                r.style.height = 46; r.style.paddingLeft = 10; r.style.paddingRight = 10;
                Round(r, 12);
                var ic = new TabletIconElement(icon, 22, Ink); ic.style.marginRight = 12; r.Add(ic);
                var l = new Label(text) { pickingMode = PickingMode.Ignore };
                l.style.fontSize = 16; l.style.color = Ink; l.style.unityFontDefinition = Fd(); l.style.flexGrow = 1;
                r.Add(l);
                if (state != null)
                {
                    var st = new Label(state()) { pickingMode = PickingMode.Ignore, name = "state" };
                    st.style.fontSize = 13; st.style.color = InkSoft; st.style.unityFontDefinition = Fd();
                    r.Add(st);
                }
                r.RegisterCallback<ClickEvent>(_ => { click(); var st = r.Q<Label>("state"); if (st != null && state != null) st.text = state(); });
                r.RegisterCallback<PointerEnterEvent>(_ => r.style.backgroundColor = new Color(0.45f, 0.32f, 0.16f, 0.10f));
                r.RegisterCallback<PointerLeaveEvent>(_ => r.style.backgroundColor = new Color(0, 0, 0, 0));
                card.Add(r);
            }

            Row(TabletIconKind.Gear, "虚拟摇杆", () => PlayerPrefs.GetInt("explore_joystick", 0) == 1 ? "已开启" : "已关闭", () => { (overlay.userData as Action)?.Invoke(); });
            Row(TabletIconKind.Note, "站务总览（只读）", null, () => { close(); onAction?.Invoke("overview"); });
            Row(TabletIconKind.Home, "返回桌面", null, () => { close(); onAction?.Invoke("back"); });
        }

        // ---------- 面板：地图 ----------
        private static VisualElement BuildMapOverlay(ExploreMapView view, Action onPicked)
        {
            var overlay = new VisualElement { name = "explore-map-overlay" };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0; overlay.style.right = 0; overlay.style.top = 0; overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
            overlay.style.alignItems = Align.Center; overlay.style.justifyContent = Justify.Center;

            var card = new VisualElement();
            card.style.width = Length.Percent(80); card.style.height = Length.Percent(78);
            card.style.paddingLeft = 18; card.style.paddingRight = 18; card.style.paddingTop = 14; card.style.paddingBottom = 18;
            card.style.backgroundColor = new Color(0.985f, 0.972f, 0.935f, 0.98f);
            Round(card, 24);
            overlay.Add(card);

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center; head.style.marginBottom = 10;
            var t = new Label("站区地图  ·  点击任意地点自动前往");
            t.style.fontSize = 18; t.style.color = Ink; t.style.unityFontDefinition = FdBold(); t.style.flexGrow = 1;
            head.Add(t);
            var close = new VisualElement { name = "map-pick-close" };
            close.style.width = 36; close.style.height = 36; close.style.alignItems = Align.Center; close.style.justifyContent = Justify.Center;
            close.style.backgroundColor = new Color(0.45f, 0.32f, 0.16f, 0.12f); Round(close, 18);
            close.Add(new TabletIconElement(TabletIconKind.Close, 18, Ink));
            head.Add(close);
            card.Add(head);

            var map = new MiniMapView(view);
            map.style.flexGrow = 1;
            card.Add(map);
            overlay.userData = (Action)(() => map.MarkDirtyRepaint());
            return overlay;
        }

        // ---------- 虚拟摇杆 ----------
        private static VisualElement BuildJoystick(ExploreMapView view)
        {
            const float size = 168f, knobSize = 72f, radius = 64f;
            var joy = new VisualElement { name = "explore-joystick" };
            joy.style.position = Position.Absolute;
            joy.style.left = 34; joy.style.bottom = 40;
            joy.style.width = size; joy.style.height = size;
            joy.style.backgroundColor = new Color(0.985f, 0.972f, 0.935f, 0.70f);
            Round(joy, size / 2f);
            joy.style.borderTopWidth = 2; joy.style.borderBottomWidth = 2; joy.style.borderLeftWidth = 2; joy.style.borderRightWidth = 2;
            var bc = new Color(0.42f, 0.32f, 0.20f, 0.45f);
            joy.style.borderTopColor = bc; joy.style.borderBottomColor = bc; joy.style.borderLeftColor = bc; joy.style.borderRightColor = bc;

            joy.generateVisualContent += ctx => DrawJoyBase(ctx, joy);
            var knob = new VisualElement { pickingMode = PickingMode.Ignore };
            knob.style.position = Position.Absolute;
            knob.style.width = knobSize; knob.style.height = knobSize;
            knob.style.left = (size - knobSize) / 2f; knob.style.top = (size - knobSize) / 2f;
            knob.style.backgroundColor = new Color(0.80f, 0.52f, 0.12f, 0.85f);
            Round(knob, knobSize / 2f);
            joy.Add(knob);

            void Reset()
            {
                view.JoyDir = Vector2.zero;
                knob.style.left = (size - knobSize) / 2f; knob.style.top = (size - knobSize) / 2f;
            }
            void Move(Vector2 local)
            {
                Vector2 v = local - new Vector2(size / 2f, size / 2f);
                float len = v.magnitude;
                Vector2 dir = len > 0.001f ? v / len : Vector2.zero;
                float clamped = Mathf.Min(len, radius);
                view.JoyDir = dir * Mathf.Clamp01(len / radius);
                knob.style.left = size / 2f + dir.x * clamped - knobSize / 2f;
                knob.style.top = size / 2f + dir.y * clamped - knobSize / 2f;
            }
            joy.RegisterCallback<PointerDownEvent>(e => { joy.CapturePointer(e.pointerId); Move(e.localPosition); e.StopPropagation(); });
            joy.RegisterCallback<PointerMoveEvent>(e => { if (joy.HasPointerCapture(e.pointerId)) Move(e.localPosition); });
            joy.RegisterCallback<PointerUpEvent>(e => { if (joy.HasPointerCapture(e.pointerId)) joy.ReleasePointer(e.pointerId); Reset(); });
            joy.RegisterCallback<PointerCaptureOutEvent>(_ => Reset());
            return joy;
        }

        // ---------- 小部件 ----------
        // ================= 代码精绘装饰（Painter2D）=================
        private static void RoundRectPath(Painter2D p, Rect r, float rad)
        {
            rad = Mathf.Max(0.5f, Mathf.Min(rad, Mathf.Min(r.width, r.height) / 2f));
            p.MoveTo(new Vector2(r.xMin + rad, r.yMin));
            p.LineTo(new Vector2(r.xMax - rad, r.yMin));
            p.ArcTo(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMin + rad), rad);
            p.LineTo(new Vector2(r.xMax, r.yMax - rad));
            p.ArcTo(new Vector2(r.xMax, r.yMax), new Vector2(r.xMax - rad, r.yMax), rad);
            p.LineTo(new Vector2(r.xMin + rad, r.yMax));
            p.ArcTo(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMax - rad), rad);
            p.LineTo(new Vector2(r.xMin, r.yMin + rad));
            p.ArcTo(new Vector2(r.xMin, r.yMin), new Vector2(r.xMin + rad, r.yMin), rad);
            p.ClosePath();
        }

        /// <summary>纸面卡片装饰：内描边、顶部高光、底部暗线，大卡片四角铆钉。</summary>
        private static void DrawDeco(MeshGenerationContext ctx, VisualElement e, float radius)
        {
            float w = e.layout.width, h = e.layout.height;
            if (w < 12f || h < 12f) return;
            var p = ctx.painter2D;
            p.strokeColor = new Color(0.42f, 0.32f, 0.20f, 0.20f); p.lineWidth = 1f;
            p.BeginPath(); RoundRectPath(p, new Rect(3.5f, 3.5f, w - 7f, h - 7f), Mathf.Max(2f, radius - 3.5f)); p.Stroke();
            p.strokeColor = new Color(1f, 1f, 1f, 0.85f); p.lineWidth = 1.2f;
            p.BeginPath(); p.MoveTo(new Vector2(radius * 0.8f, 1.2f)); p.LineTo(new Vector2(w - radius * 0.8f, 1.2f)); p.Stroke();
            p.strokeColor = new Color(0.30f, 0.20f, 0.10f, 0.14f); p.lineWidth = 1.2f;
            p.BeginPath(); p.MoveTo(new Vector2(radius * 0.8f, h - 1.2f)); p.LineTo(new Vector2(w - radius * 0.8f, h - 1.2f)); p.Stroke();
            if (w > 200f && h > 110f)
            {
                p.fillColor = new Color(0.42f, 0.32f, 0.20f, 0.32f);
                foreach (var c in new[] { new Vector2(11f, 11f), new Vector2(w - 11f, 11f), new Vector2(11f, h - 11f), new Vector2(w - 11f, h - 11f) })
                { p.BeginPath(); p.Arc(c, 2.2f, Angle.Degrees(0), Angle.Degrees(360)); p.Fill(); }
            }
        }

        /// <summary>按钮高光：上半部白色柔光 + 下沿暗线，避免纯色块的塑料感。</summary>
        private static void DrawGlossy(MeshGenerationContext ctx, VisualElement e, float radius)
        {
            float w = e.layout.width, h = e.layout.height;
            if (w < 8f || h < 8f) return;
            var p = ctx.painter2D;
            p.fillColor = new Color(1f, 1f, 1f, 0.20f);
            p.BeginPath(); RoundRectPath(p, new Rect(2f, 2f, w - 4f, h * 0.46f), radius); p.Fill();
            p.strokeColor = new Color(0.30f, 0.15f, 0.04f, 0.28f); p.lineWidth = 1.5f;
            p.BeginPath(); p.MoveTo(new Vector2(radius, h - 1f)); p.LineTo(new Vector2(w - radius, h - 1f)); p.Stroke();
        }

        /// <summary>圆形徽章：淡色底 + 色环 + 高光弧。</summary>
        private static void Medal(VisualElement m, Color tint)
        {
            m.style.backgroundColor = new Color(tint.r, tint.g, tint.b, 0.14f);
            Round(m, 20);
            m.generateVisualContent += ctx =>
            {
                float w = m.layout.width, h = m.layout.height;
                var p = ctx.painter2D; var c = new Vector2(w / 2f, h / 2f);
                p.strokeColor = new Color(tint.r, tint.g, tint.b, 0.55f); p.lineWidth = 1.6f;
                p.BeginPath(); p.Arc(c, w / 2f - 1.6f, Angle.Degrees(0), Angle.Degrees(360)); p.Stroke();
                p.strokeColor = new Color(1f, 1f, 1f, 0.85f); p.lineWidth = 1.6f;
                p.BeginPath(); p.Arc(c, w / 2f - 4f, Angle.Degrees(200), Angle.Degrees(260)); p.Stroke();
            };
        }

        /// <summary>物品格：内凹斜面（左上暗、右下亮）。</summary>
        private static void DrawSlot(MeshGenerationContext ctx, VisualElement e)
        {
            float w = e.layout.width, h = e.layout.height;
            var p = ctx.painter2D;
            p.lineWidth = 2f;
            p.strokeColor = new Color(0.30f, 0.20f, 0.10f, 0.22f);
            p.BeginPath(); p.MoveTo(new Vector2(4f, h - 5f)); p.LineTo(new Vector2(4f, 4f)); p.LineTo(new Vector2(w - 5f, 4f)); p.Stroke();
            p.strokeColor = new Color(1f, 1f, 1f, 0.9f);
            p.BeginPath(); p.MoveTo(new Vector2(w - 4f, 5f)); p.LineTo(new Vector2(w - 4f, h - 4f)); p.LineTo(new Vector2(5f, h - 4f)); p.Stroke();
        }

        /// <summary>摇杆底盘：刻度环 + 四向箭头 + 内圈。</summary>
        private static void DrawJoyBase(MeshGenerationContext ctx, VisualElement e)
        {
            float w = e.layout.width; var c = new Vector2(w / 2f, e.layout.height / 2f);
            var p = ctx.painter2D;
            p.strokeColor = new Color(0.42f, 0.32f, 0.20f, 0.45f); p.lineWidth = 1.4f;
            for (int i = 0; i < 36; i++)
            {
                float a = i * 10f * Mathf.Deg2Rad; float r0 = w / 2f - (i % 9 == 0 ? 13f : 8f), r1 = w / 2f - 4f;
                p.BeginPath(); p.MoveTo(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r0); p.LineTo(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r1); p.Stroke();
            }
            p.strokeColor = new Color(0.42f, 0.32f, 0.20f, 0.28f);
            p.BeginPath(); p.Arc(c, w / 2f - 24f, Angle.Degrees(0), Angle.Degrees(360)); p.Stroke();
            p.fillColor = new Color(0.42f, 0.32f, 0.20f, 0.55f);
            foreach (var d in new[] { new Vector2(0, -1), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(1, 0) })
            {
                Vector2 t = c + d * (w / 2f - 21f); Vector2 n = new Vector2(-d.y, d.x);
                p.BeginPath(); p.MoveTo(t + d * 5f); p.LineTo(t - d * 3f + n * 5f); p.LineTo(t - d * 3f - n * 5f); p.ClosePath(); p.Fill();
            }
        }

        private static void Fade(VisualElement e, float seconds)
        {
            e.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity") };
            e.style.transitionDuration = new List<TimeValue> { new TimeValue(seconds, TimeUnit.Second) };
        }

        private static void Round(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
        }

        private static void StyleCard(VisualElement e, float radius, float padX, float padY)
        {
            e.style.backgroundColor = Panel;
            e.style.paddingLeft = padX; e.style.paddingRight = padX; e.style.paddingTop = padY; e.style.paddingBottom = padY;
            Round(e, radius);
            e.style.borderTopWidth = 1; e.style.borderBottomWidth = 1; e.style.borderLeftWidth = 1; e.style.borderRightWidth = 1;
            e.style.borderTopColor = PanelLine; e.style.borderBottomColor = PanelLine; e.style.borderLeftColor = PanelLine; e.style.borderRightColor = PanelLine;
            e.generateVisualContent += ctx => DrawDeco(ctx, e, radius);
        }

        private static Label Chip(VisualElement parent, TabletIconKind icon, Color tint)
        {
            var wrap = new VisualElement { pickingMode = PickingMode.Ignore };
            wrap.style.flexDirection = FlexDirection.Row; wrap.style.alignItems = Align.Center; wrap.style.marginRight = 14;
            var ic = new TabletIconElement(icon, 20, tint);
            ic.style.marginRight = 6;
            wrap.Add(ic);
            var l = new Label { pickingMode = PickingMode.Ignore };
            l.style.fontSize = 16; l.style.color = Ink; l.style.unityFontDefinition = FdBold();
            wrap.Add(l);
            parent.Add(wrap);
            return l;
        }

        private static Action ToggleMapRef, ToggleMenuRef;

        /// <summary>打卡机工牌卡：顶部穿孔条 + 头像窗 + 姓名/地点 + 大号时间 + 日程刻度条 + 资金/信任。</summary>
        private static VisualElement BuildStatus(ExploreMapView view, out Action update)
        {
            var card = PaperUI.Panel(PaperTokens.RadiusM, 12, 7, true, false);
            card.name = "explore-status";
            card.pickingMode = PickingMode.Ignore;
            card.style.position = Position.Absolute;
            card.style.left = 18; card.style.top = 46;
            card.style.width = 292; card.style.height = 76;

            var header = new VisualElement { pickingMode = PickingMode.Ignore };
            header.style.flexDirection = FlexDirection.Row; header.style.alignItems = Align.Center;
            var clock = PaperUI.Text("06:00", 25, PaperTokens.Ink, true);
            var day = PaperUI.Text("", PaperTokens.FsSmall, PaperTokens.InkSoft);
            day.style.marginLeft = 9;
            var location = PaperUI.Text("", PaperTokens.FsSmall, PaperTokens.InkSoft);
            location.style.marginLeft = 10; location.style.flexGrow = 1;
            location.style.unityTextAlign = TextAnchor.MiddleRight;
            header.Add(clock); header.Add(day); header.Add(location);
            card.Add(header);

            var stats = new VisualElement { pickingMode = PickingMode.Ignore };
            stats.style.flexDirection = FlexDirection.Row; stats.style.alignItems = Align.Center;
            stats.style.marginTop = 5;
            var moneyIcon = new TabletIconElement(TabletIconKind.Bolt, 15, PaperTokens.Good) { pickingMode = PickingMode.Ignore };
            moneyIcon.style.marginRight = 4;
            var money = PaperUI.Text("", PaperTokens.FsSmall, PaperTokens.Ink, true);
            var trustIcon = new TabletIconElement(TabletIconKind.Heart, 15, PaperTokens.Bad) { pickingMode = PickingMode.Ignore };
            trustIcon.style.marginLeft = 14; trustIcon.style.marginRight = 4;
            var trust = PaperUI.Text("", PaperTokens.FsSmall, PaperTokens.Ink, true);
            stats.Add(moneyIcon); stats.Add(money); stats.Add(trustIcon); stats.Add(trust);
            card.Add(stats);

            update = () =>
            {
                clock.text = view.ClockText;
                clock.style.color = view.Minutes >= 21f * 60f ? PaperTokens.Bad : PaperTokens.Ink;
                day.text = $"第 {GameData.GetDay()} 天";
                location.text = view.NearLabel;
                money.text = GameData.GetMoney().ToString("N0");
                trust.text = GameData.GetTrust().ToString();
            };
            update();
            return card;
        }


        private static VisualElement IconButton(TabletIconKind icon, string tip, Action click)
        {
            var b = new VisualElement { tooltip = tip };
            b.style.width = 52; b.style.height = 52; b.style.marginLeft = 10;
            b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
            StyleCard(b, 26, 0, 0);
            b.Add(new TabletIconElement(icon, 26, Ink));
            b.RegisterCallback<ClickEvent>(_ => click());
            b.RegisterCallback<PointerEnterEvent>(_ => b.style.backgroundColor = new Color(0.93f, 0.88f, 0.78f, 0.97f));
            b.RegisterCallback<PointerLeaveEvent>(_ => b.style.backgroundColor = Panel);
            return b;
        }

        private static VisualElement BuildSlot(int index, Item? item)
        {
            var s = new VisualElement();
            s.style.width = 50; s.style.height = 50; s.style.marginLeft = index == 0 ? 0 : 5;
            s.style.alignItems = Align.Center; s.style.justifyContent = Justify.Center;
            s.style.backgroundColor = new Color(0.45f, 0.32f, 0.16f, 0.09f);
            Round(s, 12);
            s.style.borderTopWidth = 2; s.style.borderBottomWidth = 2; s.style.borderLeftWidth = 2; s.style.borderRightWidth = 2;
            SetSlotSelected(s, false);
            s.generateVisualContent += ctx => DrawSlot(ctx, s);

            var num = new Label((index + 1).ToString()) { pickingMode = PickingMode.Ignore };
            num.style.position = Position.Absolute; num.style.left = 6; num.style.top = 2;
            num.style.fontSize = 11; num.style.color = InkSoft; num.style.unityFontDefinition = Fd();
            s.Add(num);
            if (item.HasValue)
            {
                s.Add(new TabletIconElement(item.Value.icon, 25, item.Value.color));
                if (item.Value.count > 1)
                {
                    var c = new Label(item.Value.count.ToString()) { pickingMode = PickingMode.Ignore };
                    c.style.position = Position.Absolute; c.style.right = 6; c.style.bottom = 2;
                    c.style.fontSize = 12; c.style.color = Ink; c.style.unityFontDefinition = FdBold();
                    s.Add(c);
                }
            }
            return s;
        }

        private static void SetSlotSelected(VisualElement s, bool on)
        {
            var c = on ? Gold : new Color(0.42f, 0.32f, 0.20f, 0.28f);
            s.style.borderTopColor = c; s.style.borderBottomColor = c; s.style.borderLeftColor = c; s.style.borderRightColor = c;
            s.style.backgroundColor = on ? new Color(0.80f, 0.52f, 0.12f, 0.18f) : new Color(0.45f, 0.32f, 0.16f, 0.09f);
        }
    }

    /// <summary>全图缩略地图：点击任意可走地点，自动寻路前往；标出交互点与玩家。</summary>
    public sealed class MiniMapView : VisualElement
    {
        private readonly ExploreMapView view;
        private readonly List<Label> labels = new List<Label>();
        private readonly List<(string label, int tx, int ty, string id)> spotList;

        public MiniMapView(ExploreMapView v)
        {
            view = v;
            spotList = v.MapSpots();
            generateVisualContent += Draw;
            RegisterCallback<PointerDownEvent>(OnDown);
            foreach (var s in spotList)
            {
                var l = new Label(s.label) { pickingMode = PickingMode.Ignore };
                l.style.position = Position.Absolute;
                l.style.fontSize = 13; l.style.color = Color.white; l.style.unityFontDefinition = Fd();
                l.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
                l.style.paddingLeft = 6; l.style.paddingRight = 6;
                l.style.borderTopLeftRadius = 6; l.style.borderTopRightRadius = 6; l.style.borderBottomLeftRadius = 6; l.style.borderBottomRightRadius = 6;
                Add(l);
                labels.Add(l);
            }
            schedule.Execute(MarkDirtyRepaint).Every(200);
        }

        private float TileSize(Rect r) => Mathf.Min(r.width / ExploreMapView.W, r.height / ExploreMapView.H);
        private Vector2 Offset(Rect r, float ts) => new Vector2((r.width - ts * ExploreMapView.W) / 2f, (r.height - ts * ExploreMapView.H) / 2f);

        private void OnDown(PointerDownEvent e)
        {
            Rect r = contentRect;
            float ts = TileSize(r);
            Vector2 o = Offset(r, ts);
            int tx = Mathf.FloorToInt((e.localPosition.x - o.x) / ts), ty = Mathf.FloorToInt((e.localPosition.y - o.y) / ts);
            if (tx < 0 || ty < 0 || tx >= ExploreMapView.W || ty >= ExploreMapView.H) return;
            bool onSpot = false;
            foreach (var s in spotList) if (Mathf.Abs(s.tx - tx) <= 1 && Mathf.Abs(s.ty - ty) <= 1) { tx = s.tx; ty = s.ty; onSpot = true; break; }
            if (view.RouteToOutdoorTile(tx, ty, onSpot)) view.OnMapPicked?.Invoke();
            e.StopPropagation();
        }

        private void Draw(MeshGenerationContext mgc)
        {
            Rect r = contentRect;
            if (r.width < 4f || r.height < 4f) return;
            var p = mgc.painter2D;
            float ts = TileSize(r);
            Vector2 o = Offset(r, ts);
            var buckets = new Dictionary<Color, List<Vector4>>();
            for (int y = 0; y < ExploreMapView.H; y++)
                for (int x = 0; x < ExploreMapView.W; x++)
                {
                    Color c = view.TileColor(view.TileAt(x, y), x, y);
                    if (!buckets.TryGetValue(c, out var list)) buckets[c] = list = new List<Vector4>();
                    list.Add(new Vector4(o.x + x * ts, o.y + y * ts, ts + 0.5f, ts + 0.5f));
                }
            foreach (var kv in buckets)
            {
                p.fillColor = kv.Key;
                p.BeginPath();
                foreach (var q in kv.Value)
                {
                    p.MoveTo(new Vector2(q.x, q.y)); p.LineTo(new Vector2(q.x + q.z, q.y)); p.LineTo(new Vector2(q.x + q.z, q.y + q.w)); p.LineTo(new Vector2(q.x, q.y + q.w)); p.ClosePath();
                }
                p.Fill();
            }
            // 交互点
            for (int i = 0; i < spotList.Count; i++)
            {
                Vector2 c = new Vector2(o.x + (spotList[i].tx + 0.5f) * ts, o.y + (spotList[i].ty + 0.5f) * ts);
                p.fillColor = new Color(1f, 0.85f, 0.3f);
                p.BeginPath(); p.Arc(c, ts * 0.55f, Angle.Degrees(0), Angle.Degrees(360)); p.Fill();
                labels[i].style.left = c.x - 20f; labels[i].style.top = c.y - ts * 1.9f;
            }
            // 玩家
            Vector2 pt = view.MapPlayerPos / ExploreMapView.T;
            Vector2 pc = new Vector2(o.x + pt.x * ts, o.y + pt.y * ts);
            p.fillColor = Color.white; p.BeginPath(); p.Arc(pc, ts * 0.75f, Angle.Degrees(0), Angle.Degrees(360)); p.Fill();
            p.fillColor = new Color(0.20f, 0.36f, 0.62f); p.BeginPath(); p.Arc(pc, ts * 0.5f, Angle.Degrees(0), Angle.Degrees(360)); p.Fill();
        }
    }

    public sealed class ExploreMapView : VisualElement
    {
        private const int StationWidth = 40;
        public const int W = StationWidth + 16, H = 24;
        public const float T = 32f;          // 世界像素/格
        private const float Speed = 150f;    // 世界像素/秒
        private const float Half = 8f;       // 碰撞半宽（世界像素）
        private const float DayStart = 6f * 60f, DayEnd = 22f * 60f;

        // ---- 对外 ----
        public Action<string> OnAction;
        public Action<string> OnToast;
        public event Action<string> PromptChanged;
        public float Minutes { get; private set; } = DayStart;
        /// <summary>评审用：直接设游戏内分钟（夜景/晨雾截图）。</summary>
        public void SetMinutesForTests(float minutes) { Minutes = minutes; }
        public Vector2 PlayerPos => player;
        public string ClockText => $"{(int)(Minutes / 60):00}:{(int)(Minutes % 60):00}";

        // ---- 地图 ----
        private struct Spot { public string id, label; public int tx, ty; public float cost; }
        private struct Npc { public string id, name; public Vector2 pos; public Color color; public string[] lines; public int talk; }
        /// <summary>家具/设备：以 tx,ty 为底行左端，sprite 宽 wt 格、高 ht 格，底边对齐格线。</summary>
        private struct Prop { public string tex; public int tx, ty, wt, ht; public bool wall; }
        /// <summary>按脚底 y 排序的遮挡层：树、列车、室内落地家具与角色同序绘制（北面的人被南面的高物体挡住）。</summary>
        private readonly List<(float y, Action draw)> sortedExtras = new List<(float y, Action draw)>();
        /// <summary>区域：室外站区或某栋建筑的室内。grid/spots/npcs/props 全部按区域独立。</summary>
        private sealed class Area
        {
            public string id; public int w, h;
            public char[,] grid;
            public List<Spot> spots = new List<Spot>();
            public List<Npc> npcs = new List<Npc>();
            public List<Prop> props = new List<Prop>();
            public Vector2 spawn;
            public string floor = "int_floor_wood_01";
        }
        private readonly Dictionary<string, Area> areas = new Dictionary<string, Area>();
        private readonly Dictionary<Vector2Int, string> doorLinks = new Dictionary<Vector2Int, string>();
        private Area cur, outdoor;
        private char[,] grid;
        private int gw = W, gh = H;
        private List<Spot> spots;
        private List<Npc> npcs;
        private Vector2 returnPos;
        private float fadeA;
        private VisualElement fadeEl;
        public bool Indoors => cur != outdoor;
        public string AreaId => cur?.id ?? "outdoor";
        private readonly Dictionary<string, Label> npcLabels = new Dictionary<string, Label>();
        private readonly Dictionary<string, Image> charImgs = new Dictionary<string, Image>();
        private static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
        private static Texture2D Tex(string name)
        {
            if (texCache.TryGetValue(name, out var t)) return t;
            string folder = name.StartsWith("int_") ? "Interiors/" : name.StartsWith("scene_") ? "Scenes/"
                : name.StartsWith("fx_") ? "Fixtures/" : name.StartsWith("fog_") ? "Scenes/"
                : name.StartsWith("train_") ? "Vehicles/" : "Props/";
            t = Resources.Load<Texture2D>("Art/Explore/" + folder + name);
            texCache[name] = t;
            return t;
        }

        // ---- 状态 ----
        private Vector2 player = new Vector2(10.5f * T, 9.6f * T);
        private Vector2 facing = Vector2.down;
        // 自动寻路
        private List<Vector2> path;
        private int pathIdx;
        private Vector2Int goalTile;
        private bool pendingInteract;
        private float stuck;
        public bool HasPath => path != null;
        public int Interactions { get; private set; }
        public Vector2 NpcPos(string id) { foreach (var a in areas.Values) foreach (var n in a.npcs) if (n.id == id) return n.pos; return Vector2.zero; }
        public void SetPlayerPos(Vector2 p) { player = p; }

        // 加速 / 能量
        public const float MaxStamina = 100f;
        private const float SprintMul = 1.7f, SprintDrain = 28f, RegenRate = 14f;
        public float Stamina { get; private set; } = MaxStamina;
        public bool SprintLocked { get; private set; }
        private float regenDelay;

        // 外部输入（虚拟摇杆 / 加速键）
        public Vector2 JoyDir;
        public bool SprintHeld;
        public event Action OpenMapRequested;
        public Action OnMapPicked;
        public Action<int, int> OpenRdaRequested;
        public Action<string, string, Color> OnDialogue;
        public Vector2 PromptWorld;
        public bool IsMoving { get; private set; }
        /// <summary>世界坐标 → 视图局部坐标（悬浮 UI 跟随世界物体用）。</summary>
        public Vector2 WorldToView(Vector2 w) { float s = Scale; return (w - CameraOrigin(s)) * s; }
        public string SelectedItem = "";
        public void RequestMap() => OpenMapRequested?.Invoke();

        private string lastPrompt = "";
        private bool lateNotified;
        private int trustDay = -1;
        private IVisualElementScheduledItem loop;
        private float walkPhase;
        private float dustT;
        private struct Dust { public Vector2 pos; public float age, life, size; }
        private readonly List<Dust> dust = new List<Dust>();

        // ---- 配色 ----
        private static readonly Color Grass1 = new Color(0.42f, 0.62f, 0.33f), Grass2 = new Color(0.39f, 0.59f, 0.31f);
        private static readonly Color Dirt = new Color(0.66f, 0.55f, 0.38f), Plat = new Color(0.78f, 0.76f, 0.70f);
        private static readonly Color Ballast = new Color(0.50f, 0.47f, 0.42f), Rail = new Color(0.22f, 0.22f, 0.25f), Sleeper = new Color(0.36f, 0.26f, 0.18f);
        private static readonly Color Wall = new Color(0.62f, 0.45f, 0.34f), Roof = new Color(0.62f, 0.25f, 0.22f), Door = new Color(0.28f, 0.18f, 0.12f), Win = new Color(0.82f, 0.90f, 0.95f);
        private static readonly Color Tea = new Color(0.28f, 0.48f, 0.24f), Water = new Color(0.30f, 0.55f, 0.75f), Trunk = new Color(0.35f, 0.24f, 0.15f), Canopy = new Color(0.20f, 0.42f, 0.22f);
        private static readonly Color Fence = new Color(0.50f, 0.36f, 0.22f), Shadow = new Color(0f, 0f, 0f, 0.28f);

        public ExploreMapView()
        {
            style.position = Position.Absolute;
            style.left = 0; style.right = 0; style.top = 0; style.bottom = 0;
            style.backgroundColor = new Color(0.12f, 0.2f, 0.12f);
            style.overflow = Overflow.Hidden;
            focusable = true;
            outdoor = new Area { id = "outdoor", w = W, h = H, grid = new char[H, W] };
            areas["outdoor"] = outdoor;
            UseArea(outdoor);
            BuildMap();
            BuildDispatchRoom();
            BuildHall();
            UseArea(outdoor);
            generateVisualContent += Draw;
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<AttachToPanelEvent>(_ => StartLoop());
            RegisterCallback<DetachFromPanelEvent>(_ => loop?.Pause());
            var allNpcs = areas.Values.SelectMany(a => a.npcs).ToList();
            foreach (var id in new[] { "player" }.Concat(allNpcs.Select(n => n.id)))
            {
                var im = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.StretchToFill };
                im.style.position = Position.Absolute; im.style.display = DisplayStyle.None;
                Add(im); charImgs[id] = im;
            }
            foreach (var n in allNpcs)
            {
                var l = new Label(n.name);
                l.pickingMode = PickingMode.Ignore;
                l.style.position = Position.Absolute;
                l.style.fontSize = 13; l.style.color = Color.white; l.style.unityFontDefinition = Fd();
                l.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f);
                l.style.paddingLeft = 5; l.style.paddingRight = 5;
                l.style.borderTopLeftRadius = 6; l.style.borderTopRightRadius = 6;
                l.style.borderBottomLeftRadius = 6; l.style.borderBottomRightRadius = 6;
                l.style.display = DisplayStyle.None;
                Add(l);
                npcLabels[n.id] = l;
            }
            fadeEl = new VisualElement { pickingMode = PickingMode.Ignore };
            fadeEl.style.position = Position.Absolute;
            fadeEl.style.left = 0; fadeEl.style.right = 0; fadeEl.style.top = 0; fadeEl.style.bottom = 0;
            fadeEl.style.backgroundColor = new Color(0.05f, 0.04f, 0.03f);
            fadeEl.style.opacity = 0f;
            Add(fadeEl);
        }

        // ================= 区域切换 =================
        private void UseArea(Area a)
        {
            style.backgroundColor = a == outdoor ? new Color(0.12f, 0.2f, 0.12f) : new Color(0.93f, 0.90f, 0.83f);
            cur = a; grid = a.grid; gw = a.w; gh = a.h; spots = a.spots; npcs = a.npcs;
            foreach (var im in charImgs.Values) im.style.display = DisplayStyle.None;
            foreach (var l in npcLabels.Values) l.style.display = DisplayStyle.None;
        }

        private void EnterArea(string id)
        {
            if (!areas.TryGetValue(id, out var a) || a == cur) return;
            if (cur == outdoor) returnPos = player;
            ClearPath(); dust.Clear();
            UseArea(a);
            player = a.spawn; facing = Vector2.up;
            fadeA = 1f;
            lastPrompt = "";
            ExploreQuests.Report(QuestEvent.Enter, id);
            RunQueued();
        }

        private void ExitArea()
        {
            if (cur == outdoor) return;
            ClearPath(); dust.Clear();
            UseArea(outdoor);
            player = returnPos + new Vector2(0f, T);   // 出门落在门口下方一格
            facing = Vector2.down;
            fadeA = 1f;
            lastPrompt = "";
            RunQueued();
        }

        // ---- 跨区域导航：先走到门，切换区域后再继续走向最终目标 ----
        private Action queuedAfterSwitch;
        private void RunQueued() { var q = queuedAfterSwitch; queuedAfterSwitch = null; q?.Invoke(); }

        private Vector2Int ExitDoorTile()
        {
            for (int y = 0; y < gh; y++) for (int x = 0; x < gw; x++) if (grid[y, x] == 'D') return new Vector2Int(x, y);
            return PlayerTile;
        }

        private string AreaOfSpot(string id)
        {
            foreach (var a in areas.Values) foreach (var sp in a.spots) if (sp.id == id) return a.id;
            return null;
        }

        /// <summary>要去的目标在别的区域时，规划“先出门/先进门”。返回 false 表示目标就在当前区域。</summary>
        private bool RouteAcrossAreas(string targetArea, Action arrive)
        {
            if (targetArea == null || targetArea == AreaId) return false;
            if (Indoors)
            {
                // 先出门；出门后若目标在别的建筑，再继续进门
                queuedAfterSwitch = () => { if (!RouteAcrossAreas(targetArea, arrive)) arrive(); };
                var d = ExitDoorTile();
                NavigateToTile(d.x, d.y, false);
                return true;
            }
            Vector2Int door = default; bool found = false;
            foreach (var kv in doorLinks) if (kv.Value == targetArea) { door = kv.Key; found = true; break; }
            if (!found) return false;
            queuedAfterSwitch = arrive;
            NavigateToTile(door.x, door.y, false);
            return true;
        }

        private void CheckDoor()
        {
            var t = PlayerTile;
            if (cur == outdoor) { if (doorLinks.TryGetValue(t, out var id)) EnterArea(id); }
            else if (grid[t.y, t.x] == 'D') ExitArea();
        }

        // ================= 地图 =================
        private void Fill(int x, int y, int w, int h, char c)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    if (i >= 0 && i < W && j >= 0 && j < H) grid[j, i] = c;
        }

        private void Building(int x, int y, int w, int h, int doorX, int doorY)
        {
            Fill(x, y, w, h, '#');
            Fill(x, y, w, 2, 'R');
            grid[doorY, doorX] = 'D';
        }

        private void BuildMap()
        {
            Fill(0, 0, W, H, '.');
            Fill(0, 0, W, 1, 'T'); Fill(0, H - 1, W, 1, 'T'); Fill(0, 0, 1, H, 'T'); Fill(W - 1, 0, 1, H, 'T');

            Fill(4, 9, 33, 1, ',');            // 站前土路
            Fill(4, 10, 33, 1, 'P');           // 站台
            Fill(1, 11, W - 2, 1, '=');        // 正线贯通东段
            Fill(1, 12, W - 2, 1, '=');        // 侧线贯通东段
            Fill(8, 11, 7, 1, 'X');            // NF-5K「耕牛」与客厢停靠（不可穿越）

            Building(5, 3, 10, 6, 10, 8);      // 站务大厅
            Building(18, 3, 6, 5, 20, 7); Fill(20, 8, 1, 1, ',');   // 调度室
            Building(26, 3, 7, 5, 29, 7); Fill(29, 8, 1, 1, ',');   // 候车室
            Building(24, 15, 8, 5, 27, 19);    // 车库（门朝南：屋顶在北、立面在南，门在立面上）
            Fill(27, 20, 1, 2, ',');
            Fill(StationWidth, 10, W - StationWidth - 1, 1, 'P');
            Fill(49, 2, 6, 4, '#');             // 东段货棚墙体
            Fill(51, 6, 4, 1, ',');
            Fill(50, 13, 1, 2, ',');
            Fill(54, 15, 1, 6, '~');            // 东缘排水沟
            Fill(2, 15, 10, 6, 'f');           // 茶田
            Building(13, 16, 4, 3, 14, 18);    // 茶摊
            Building(18, 15, 5, 5, 19, 19);    // 铁路商店
            Fill(19, 20, 2, 2, ',');
            Fill(33, 18, 5, 4, '~');           // 水塘
            Fill(35, 13, 1, 4, ',');           // 道口小径
            Fill(2, 2, 2, 2, 'T'); Fill(36, 5, 2, 2, 'T'); Fill(3, 7, 1, 1, 'T');
            Fill(1, 14, 12, 1, 'F');           // 茶田围栏

            spots.Add(new Spot { id = "hall", label = "站务大厅", tx = 10, ty = 8, cost = 20 });
            spots.Add(new Spot { id = "dispatch", label = "调度室", tx = 20, ty = 7, cost = 15 });
            spots.Add(new Spot { id = "waiting", label = "候车室", tx = 29, ty = 7, cost = 30 });
            spots.Add(new Spot { id = "platform", label = "站台", tx = 17, ty = 10, cost = 10 });
            spots.Add(new Spot { id = "garage", label = "车库", tx = 27, ty = 19, cost = 30 });
            spots.Add(new Spot { id = "tea", label = "茶摊", tx = 14, ty = 18, cost = 15 });
            spots.Add(new Spot { id = "rail_shop", label = "铁路商店", tx = 19, ty = 19 });
            spots.Add(new Spot { id = "east_crossing", label = "东端道口", tx = 50, ty = 13 });
            spots.Add(new Spot { id = "east_freight", label = "货运栈台", tx = 53, ty = 6 });
            spots.Add(new Spot { id = "crossing", label = "道口", tx = 35, ty = 13, cost = 20 });
            spots.Add(new Spot { id = "board", label = "公告栏", tx = 6, ty = 9, cost = 10 });
            spots.Add(new Spot { id = "finance", label = "账房", tx = 33, ty = 9, cost = 15 });
            spots.Add(new Spot { id = "rest", label = "宿舍（休息 · 结算今日）", tx = 36, ty = 9, cost = 0 });
            // 探索点：每个玩法不同（搜寻一次 / 收集成套 / 拨动机关 ×2 / 每日摇树），避免重复同类互动
            spots.Add(new Spot { id = "pond_look", label = "水塘 · 俯身看看", tx = 35, ty = 17, cost = 5 });
            spots.Add(new Spot { id = "tea_nails", label = "茶田边 · 拨开杂草", tx = 12, ty = 15, cost = 5 });
            spots.Add(new Spot { id = "signal_lever", label = "信号机 · 扳动手柄", tx = 36, ty = 7, cost = 5 });
            spots.Add(new Spot { id = "blossom", label = "樱树 · 摇一摇", tx = 4, ty = 6, cost = 5 });

            npcs.Add(new Npc { id = "laochen", name = "老陈", pos = new Vector2(16.5f * T, 9.5f * T), color = new Color(0.55f, 0.55f, 0.60f), lines = new[] { "今天水箱水位正常，随时能发车。", "松桥那边的线路，早晚得去看看。" } });
            npcs.Add(new Npc { id = "liayi", name = "李阿姨", pos = new Vector2(28f * T, 9.5f * T), color = new Color(0.78f, 0.45f, 0.50f), lines = new[] { "候车的乘客们都等着呢。", "站里的茶水我都备好了。" } });
            npcs.Add(new Npc { id = "zhanggong", name = "张工", pos = new Vector2(25.5f * T, 13.5f * T), color = new Color(0.30f, 0.45f, 0.65f), lines = new[] { "车况还撑得住，别太压榨它。", "闸瓦该换了，等有钱再说。" } });
            npcs.Add(new Npc { id = "chasao", name = "茶嫂", pos = new Vector2(15.5f * T, 19.5f * T), color = new Color(0.85f, 0.60f, 0.25f), lines = new[] { "霜降前这批茶得下山。", "哟，小站长，尝尝新茶？" } });
            doorLinks[new Vector2Int(20, 7)] = "dispatch";
            doorLinks[new Vector2Int(10, 8)] = "hall";
            npcs.Add(new Npc { id = "xiaofang", name = "小芳", pos = new Vector2(7.5f * T, 9.5f * T), color = new Color(0.60f, 0.78f, 0.55f), lines = new[] { "我在学看信号灯，您教教我吧。" } });
        }

        // 室内：a 北墙顶 / b 北墙正面 / s 侧墙与南墙 / o 家具占位
        private static bool IsSolid(char c) => c == '#' || c == 'R' || c == 'T' || c == '~' || c == 'F' || c == 'X' || c == 'a' || c == 'b' || c == 's' || c == 'o';

        /// <summary>调度室（20×12）：北墙挂钟/线路屏/四扇窗，中部调度台/策略板/电台，南侧两组沙发茶几与出口。素材由 GPT 逐格交付。</summary>
        private void BuildDispatchRoom()
        {
            const int RW = 20, RH = 12;
            var a = new Area { id = "dispatch", w = RW, h = RH, floor = "int_floor_wood_01" };
            a.grid = new char[RH, RW];
            for (int y = 0; y < RH; y++)
                for (int x = 0; x < RW; x++)
                    a.grid[y, x] = y == 0 ? 'a' : y == 1 ? 'b' : (y == RH - 1 || x == 0 || x == RW - 1) ? 's' : '.';
            a.grid[RH - 1, 10] = 'D';
            a.spawn = new Vector2(10.5f * T, (RH - 1.5f) * T);
            void Occupy(int x, int y, int w, int h) { for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) a.grid[j, i] = 'o'; }
            void P(string tex, int tx, int ty, int wt, int ht, bool solid = true)
            {
                a.props.Add(new Prop { tex = tex, tx = tx, ty = ty, wt = wt, ht = ht, wall = !solid });
                if (solid) Occupy(tx, ty - (ht - 1), wt, ht);
            }
            // 墙面装饰（不占地）：窗、挂钟、线路屏
            foreach (int wx in new[] { 2, 5, 14, 17 }) P("int_window_01", wx, 1, 1, 1, false);
            P("prop_wall_clock", 7, 1, 1, 1, false);
            P("prop_route_screen_on", 9, 1, 2, 1, false);
            // 地面家具
            P("prop_radio", 3, 3, 1, 1);
            P("prop_strategy_board", 16, 3, 1, 2);
            P("prop_dispatch_desk", 9, 5, 2, 1);
            P("prop_sofa", 2, 9, 2, 1); P("prop_teatable", 4, 9, 1, 1);
            P("prop_sofa", 15, 9, 2, 1); P("prop_teatable", 14, 9, 1, 1);

            a.spots.Add(new Spot { id = "dispatch_desk", label = "调度台 · 发车方案", tx = 10, ty = 6, cost = 15 });
            a.spots.Add(new Spot { id = "strategy_board", label = "策略板 · 经营策略", tx = 16, ty = 4, cost = 15 });
            a.spots.Add(new Spot { id = "radio", label = "电台", tx = 3, ty = 4, cost = 5 });
            a.spots.Add(new Spot { id = "sofa", label = "沙发 · 歇一会儿", tx = 3, ty = 10, cost = 20 });
            a.spots.Add(new Spot { id = "sofa2", label = "沙发 · 歇一会儿", tx = 16, ty = 10, cost = 20 });
            a.npcs.Add(new Npc { id = "zhaoshifu", name = "赵师傅", pos = new Vector2(13.5f * T, 6.5f * T), color = new Color(0.34f, 0.50f, 0.44f), lines = new[] { "今天的班次表我排好了。", "沙能那边最近动作不小，得留心。" } });
            areas[a.id] = a;
        }

        /// <summary>站务大厅（16×11）：北墙委托公告板/信息屏/窗，售票窗口与售票员，两组长椅。地板用瓷砖。</summary>
        private void BuildHall()
        {
            const int RW = 16, RH = 11;
            var a = new Area { id = "hall", w = RW, h = RH, floor = "tile" };
            a.grid = new char[RH, RW];
            for (int y = 0; y < RH; y++)
                for (int x = 0; x < RW; x++)
                    a.grid[y, x] = y == 0 ? 'a' : y == 1 ? 'b' : (y == RH - 1 || x == 0 || x == RW - 1) ? 's' : '.';
            a.grid[RH - 1, 8] = 'D';
            a.spawn = new Vector2(8.5f * T, (RH - 1.5f) * T);
            void Occupy(int x, int y, int w, int h) { for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) a.grid[j, i] = 'o'; }
            void P(string tex, int tx, int ty, int wt, int ht, bool solid = true)
            {
                a.props.Add(new Prop { tex = tex, tx = tx, ty = ty, wt = wt, ht = ht, wall = !solid });
                if (solid) Occupy(tx, ty - (ht - 1), wt, ht);
            }
            foreach (int wx in new[] { 2, 6, 10, 13 }) P("int_window_01", wx, 1, 1, 1, false);
            P("prop_wall_clock", 8, 1, 1, 1, false);
            P("prop_route_screen_off", 4, 1, 2, 1, false);          // 信息屏
            P("prop_strategy_board", 3, 3, 1, 2);                   // 委托公告板
            P("prop_dispatch_desk", 10, 4, 2, 1);                   // 售票窗口
            P("prop_radio", 14, 3, 1, 1);                           // 广播机
            P("prop_sofa", 2, 8, 2, 1); P("prop_teatable", 4, 8, 1, 1);     // 长椅
            P("prop_sofa", 11, 8, 2, 1); P("prop_teatable", 10, 8, 1, 1);

            a.spots.Add(new Spot { id = "hall_board", label = "委托公告板 · 在办订单", tx = 3, ty = 4, cost = 15 });
            a.spots.Add(new Spot { id = "ticket_window", label = "售票窗口", tx = 10, ty = 5, cost = 10 });
            a.spots.Add(new Spot { id = "bench", label = "长椅 · 歇一会儿", tx = 3, ty = 9, cost = 15 });
            a.npcs.Add(new Npc { id = "laoqian", name = "老钱", pos = new Vector2(12.5f * T, 5.5f * T), color = new Color(0.52f, 0.42f, 0.60f), lines = new[] { "今天的票卖得不多，山里天冷了。", "要买票排队，别挤窗口。", "公告板上的委托，早接早有钱赚。" } });
            areas[a.id] = a;
        }

        private bool Blocked(float px, float py)
        {
            foreach (float dx in new[] { -Half, Half })
                foreach (float dy in new[] { -Half + 4f, Half })
                {
                    int tx = Mathf.FloorToInt((px + dx) / T), ty = Mathf.FloorToInt((py + dy) / T);
                    if (tx < 0 || ty < 0 || tx >= gw || ty >= gh) return true;
                    if (IsSolid(grid[ty, tx])) return true;
                    if (!Indoors && ExploreFixtures.DecalSolid(tx, ty)) return true;
                }
            return NpcBlocks(px, py);
        }

        // ================= 主循环 =================
        private void StartLoop()
        {
            loop?.Pause();
            loop = schedule.Execute(Tick).Every(16);
        }

        /// <summary>供评审脚本/回放调用：按给定方向与时间步进一次。</summary>
        public void Step(Vector2 dir, float dt, bool sprint = false)
        {
            bool moving = dir.sqrMagnitude > 0.001f;
            IsMoving = moving;
            float mul = 1f;
            if (moving && sprint && Stamina > 0f && !SprintLocked)
            {
                mul = SprintMul;
                Stamina = Mathf.Max(0f, Stamina - SprintDrain * dt);
                regenDelay = 0.8f;
                if (Stamina <= 0f) SprintLocked = true;
            }
            else
            {
                regenDelay -= dt;
                if (regenDelay <= 0f)
                {
                    Stamina = Mathf.Min(MaxStamina, Stamina + RegenRate * dt);
                    if (SprintLocked && Stamina >= 25f) SprintLocked = false;
                }
            }
            if (moving)
            {
                dir.Normalize();
                facing = dir;
                Vector2 delta = dir * Speed * mul * dt;
                if (!Blocked(player.x + delta.x, player.y)) player.x += delta.x;
                if (!Blocked(player.x, player.y + delta.y)) player.y += delta.y;
                CheckDoor();
                walkPhase += dt * 9f * mul;
                dustT -= dt;
                if (dustT <= 0f)
                {
                    dustT = mul > 1f ? 0.05f : 0.14f;
                    char g = grid[Mathf.Clamp(Mathf.FloorToInt(player.y / T), 0, gh - 1), Mathf.Clamp(Mathf.FloorToInt(player.x / T), 0, gw - 1)];
                    if (cur == outdoor && g != 'P' && g != '=' && dust.Count < 40)
                        dust.Add(new Dust { pos = player + new Vector2(UnityEngine.Random.Range(-4f, 4f), 7f), age = 0f, life = mul > 1f ? 0.5f : 0.38f, size = mul > 1f ? 4.5f : 3f });
                }
            }
            for (int di = dust.Count - 1; di >= 0; di--)
            {
                var dd = dust[di]; dd.age += dt;
                if (dd.age >= dd.life) dust.RemoveAt(di); else dust[di] = dd;
            }
            Minutes = Mathf.Min(DayEnd + 30f, Minutes + dt * 2f);
            if (Minutes >= DayEnd && !lateNotified)
            {
                lateNotified = true;
                OnToast?.Invoke("夜深了，该回站休息了");
            }
        }

        private void Tick()
        {
            if (panel == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            Vector2 dir = Vector2.zero;
            bool shift = false;
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.wKey.isPressed || k.upArrowKey.isPressed) dir.y -= 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) dir.y += 1f;
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) dir.x -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) dir.x += 1f;
                shift = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
                if (k.eKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) Interact();
                if (k.mKey.wasPressedThisFrame) OpenMapRequested?.Invoke();
            }
            if (JoyDir.sqrMagnitude > 0.04f) dir += JoyDir;
            bool sprint = shift || SprintHeld;
            if (fadeA > 0f) { fadeA = Mathf.Max(0f, fadeA - dt * 4.5f); fadeEl.style.opacity = fadeA; }

            if (dir.sqrMagnitude > 0.001f) { ClearPath(); queuedAfterSwitch = null; Step(dir, dt, sprint); }
            else if (path != null) FollowPath(dt, sprint);
            else Step(Vector2.zero, dt);
            if (!Indoors)
            {
                ExploreFixtures.Tick(dt, player, T);
                ExploreFlyers.Update(dt, W * T, H * T, SandRivalManager.GetPenetration("wufeng"));
            }
            UpdatePrompt();
            MarkDirtyRepaint();
        }

        // ================= 自动寻路（A*，8 向，不切角）=================
        private bool Walk(int x, int y) => x >= 0 && y >= 0 && x < gw && y < gh && !IsSolid(grid[y, x]) && (Indoors || !ExploreFixtures.DecalSolid(x, y));

        /// <summary>寻路用：地形可走且不是 NPC 脚下的格子（NPC 是实体障碍，路径要绕开）。</summary>
        private bool NavWalk(int x, int y) => Walk(x, y) && !NpcOnTile(x, y);

        private bool NpcOnTile(int x, int y)
        {
            foreach (var n in npcs)
                if (Mathf.FloorToInt(n.pos.x / T) == x && Mathf.FloorToInt(n.pos.y / T) == y) return true;
            return false;
        }

        /// <summary>移动碰撞：NPC 占一个小方块；玩家已经与某 NPC 重叠时放行（避免出生/刷新时被卡死）。</summary>
        private bool NpcBlocks(float px, float py)
        {
            foreach (var n in npcs)
            {
                if (OverlapsNpc(player.x, player.y, n)) continue;
                if (OverlapsNpc(px, py, n)) return true;
            }
            return false;
        }
        private static bool OverlapsNpc(float px, float py, Npc n) => Mathf.Abs(px - n.pos.x) < 15f && Mathf.Abs(py - n.pos.y) < 12f;

        public Vector2Int PlayerTile => new Vector2Int(Mathf.Clamp(Mathf.FloorToInt(player.x / T), 0, gw - 1), Mathf.Clamp(Mathf.FloorToInt(player.y / T), 0, gh - 1));

        private bool NearestWalkable(Vector2Int t, out Vector2Int result) => ExplorePathfinding.NearestWalkable(NavWalk, t, 6, out result);

        private List<Vector2> FindPath(Vector2Int start, Vector2Int goal)
        {
            if (!NavWalk(goal.x, goal.y)) return null;
            if (!NavWalk(start.x, start.y) && !NearestWalkable(start, out start)) return null;
            var tiles = ExplorePathfinding.FindPath(gw, gh, NavWalk, start, goal);
            if (tiles == null) return null;
            var pts = new List<Vector2>(tiles.Count);
            foreach (var t in tiles) pts.Add(new Vector2((t.x + 0.5f) * T, (t.y + 0.5f) * T));
            return pts;
        }

        /// <summary>自动导航到格子；interact=到达后自动交互。地图点击、场景点击共用。</summary>
        public bool NavigateToTile(int tx, int ty, bool interact = false)
        {
            var goal = new Vector2Int(tx, ty);
            if (!NavWalk(goal.x, goal.y) && !NearestWalkable(goal, out goal)) { OnToast?.Invoke("走不过去"); return false; }
            var p = Plan(goal);
            if (p == null) { OnToast?.Invoke("没有路可走"); return false; }
            path = p; pathIdx = 1; goalTile = goal; pendingInteract = interact; stuck = 0f; repaths = 0;
            return true;
        }

        /// <summary>评审/回放用：按固定步长推进自动寻路（等价于 Tick 里没有手动输入的分支）。返回是否仍在行走。</summary>
        public bool SimulateAutoWalk(int maxSteps, float dt = 1f / 30f)
        {
            for (int i = 0; i < maxSteps && path != null; i++) FollowPath(dt, false);
            return path != null;
        }

        public void ClearPath() { path = null; pendingInteract = false; stuck = 0f; }

        private bool LineClear(Vector2 a, Vector2 b)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / 6f));
            for (int k = 0; k <= n; k++)
                if (Blocked(Vector2.Lerp(a, b, k / (float)n).x, Vector2.Lerp(a, b, k / (float)n).y)) return false;
            return true;
        }

        /// <summary>拉直路径（string pulling）：用玩家碰撞盒做视线检测，去掉 8 向锯齿与贴墙。</summary>
        private List<Vector2> Smooth(List<Vector2> pts)
        {
            if (pts.Count < 3) return pts;
            var r = new List<Vector2> { pts[0] };
            int i = 0;
            while (i < pts.Count - 1)
            {
                int j = pts.Count - 1;
                while (j > i + 1 && !LineClear(pts[i], pts[j])) j--;
                r.Add(pts[j]); i = j;
            }
            return r;
        }

        private List<Vector2> Plan(Vector2Int goal)
        {
            var raw = FindPath(PlayerTile, goal);
            if (raw == null) return null;
            raw.Insert(0, player);
            return Smooth(raw);
        }

        private int repaths;

        private void FinishPath()
        {
            bool go = pendingInteract;
            path = null; pendingInteract = false;
            if (go) Interact();
        }

        private void FollowPath(float dt, bool sprint)
        {
            float reach = Mathf.Max(4f, Speed * SprintMul * dt * 1.2f);
            // 同一帧内连续越过已到达的路点：不在每个路点停顿一帧（否则走路动画会每格抖一下）
            while (path != null && pathIdx < path.Count && (path[pathIdx] - player).magnitude < reach) pathIdx++;
            if (path == null || pathIdx >= path.Count) { FinishPath(); Step(Vector2.zero, dt); return; }
            Vector2 d = path[pathIdx] - player;
            Vector2 before = player;
            Step(d, dt, sprint);
            if ((player - before).sqrMagnitude < 0.0001f)
            {
                stuck += dt;
                if (stuck > 0.4f)
                {
                    if (++repaths > 3) { ClearPath(); OnToast?.Invoke("被挡住了"); return; }
                    var p = Plan(goalTile);
                    if (p == null) ClearPath(); else { path = p; pathIdx = 1; stuck = 0f; }
                }
            }
            else stuck = 0f;
        }

        private void OnPointerDown(PointerDownEvent e)
        {
            queuedAfterSwitch = null;
            Vector2 world = ScreenToWorld(e.localPosition);
            // 点中交互点 / NPC：走过去并自动交互
            foreach (var s in spots)
                if (Vector2.Distance(world, SpotCenter(s)) < 0.9f * T) { NavigateToTile(s.tx, s.ty, true); return; }
            foreach (var n in npcs)
                if (Vector2.Distance(world, n.pos) < 0.9f * T)
                {
                    NavigateToTile(Mathf.FloorToInt(n.pos.x / T), Mathf.FloorToInt(n.pos.y / T), true);
                    return;
                }
            int wx = Mathf.FloorToInt(world.x / T), wy = Mathf.FloorToInt(world.y / T);
            if (wx >= 0 && wy >= 0 && wx < gw && wy < gh && (grid[wy, wx] == '#' || grid[wy, wx] == 'R'))
            {
                // 点到建筑墙体/屋顶：走到离点击处最近的门并进入，而不是随便贴到哪面墙
                float best = float.MaxValue; Vector2Int door = default; bool found = false;
                for (int y = 0; y < gh; y++)
                    for (int x = 0; x < gw; x++)
                        if (grid[y, x] == 'D')
                        {
                            float d = (x - wx) * (x - wx) + (y - wy) * (y - wy);
                            if (d < best && d <= 144f) { best = d; door = new Vector2Int(x, y); found = true; }
                        }
                if (found) { NavigateToTile(door.x, door.y, true); return; }
            }
            NavigateToTile(wx, wy, false);
        }

        // ---- 给地图面板用 ----
        public char TileAt(int x, int y) => outdoor.grid[y, x];
        /// <summary>地图面板用的玩家位置：在室内时显示为所在建筑门口。</summary>
        public Vector2 MapPlayerPos => Indoors ? returnPos : player;
        public void FinishFade() { fadeA = 0f; if (fadeEl != null) fadeEl.style.opacity = 0f; }
        public void LeaveIndoors() { if (Indoors) { player = returnPos; ExitArea(); } }

        public Color TileColor(char c, int x, int y)
        {
            switch (c)
            {
                case ',': return Dirt;
                case 'P': return Plat;
                case '=': case 'X': return Ballast;
                case '#': return Wall;
                case 'R': return Roof;
                case 'D': return Door;
                case 'f': return Tea;
                case '~': return Water;
                case 'T': return Canopy;
                case 'F': return Fence;
                default: return ((x + y) & 1) == 0 ? Grass1 : Grass2;
            }
        }

        public List<(string label, int tx, int ty, string id)> MapSpots()
        {
            var r = new List<(string, int, int, string)>();
            foreach (var s in outdoor.spots) r.Add((s.label, s.tx, s.ty, s.id));
            return r;
        }

        // ================= 交互 =================
        private Vector2 SpotCenter(Spot s) => new Vector2((s.tx + 0.5f) * T, (s.ty + 0.5f) * T);

        private bool Nearest(out Spot spot, out Npc npc, out bool isNpc)
        {
            spot = default; npc = default; isNpc = false;
            float best = 1.6f * T;
            foreach (var s in spots)
            {
                float d = Vector2.Distance(player, SpotCenter(s));
                if (d < best) { best = d; spot = s; isNpc = false; }
            }
            for (int i = 0; i < npcs.Count; i++)
            {
                float d = Vector2.Distance(player, npcs[i].pos);
                if (d < best) { best = d; npc = npcs[i]; isNpc = true; }
            }
            return best < 1.6f * T;
        }

        public string NearLabel { get; private set; } = "雾峰站";
        private bool pondFound;
        private int nailsFound;
        private int blossomDay = -1;

        private void UpdatePrompt()
        {
            string text = "";
            if (Nearest(out var s, out var n, out bool isNpc))
            {
                text = isNpc ? $"和 {n.name} 说话" : s.label;
                PromptWorld = isNpc ? n.pos + new Vector2(0f, -34f) : SpotCenter(s) + new Vector2(0f, -30f);
            }
            NearLabel = string.IsNullOrEmpty(text) ? "雾峰站" : text;
            if (text != lastPrompt)
            {
                lastPrompt = text;
                PromptChanged?.Invoke(text);
            }
        }

        public void Interact()
        {
            Interactions++;
            if (!Nearest(out var s, out var n, out bool isNpc)) return;
            if (isNpc)
            {
                int idx = npcs.FindIndex(x => x.name == n.name);
                var cur = npcs[idx];
                string line = cur.lines[cur.talk % cur.lines.Length];
                if (OnDialogue != null) OnDialogue(cur.name, line, cur.color);
                else OnToast?.Invoke($"{cur.name}：{line}");
                cur.talk++;
                npcs[idx] = cur;
                Minutes += 5f;
                ExploreQuests.Report(QuestEvent.Talk, cur.id);
                return;
            }

            Minutes += s.cost;
            ExploreQuests.Report(QuestEvent.Interact, s.id);
            switch (s.id)
            {
                case "dispatch": EnterArea("dispatch"); break;   // 室外调度室门口 → 进入室内
                case "hall": EnterArea("hall"); break;
                case "ticket_window": OnToast?.Invoke("窗口里的老钱把今天的车票理了一遍，余票还很充足。"); break;
                case "bench":
                    Stamina = MaxStamina; SprintLocked = false;
                    OnToast?.Invoke("在长椅上坐了一会儿，精力恢复了。");
                    break;
                case "radio": OnToast?.Invoke("电台里只有杂音，沙能那边暂时没有新通告。"); break;
                case "sofa": case "sofa2":
                    Stamina = MaxStamina; SprintLocked = false;
                    OnToast?.Invoke("在沙发上歇了一会儿，精力恢复了。");
                    break;
                case "waiting":
                    if (trustDay != GameData.GetDay())
                    {
                        trustDay = GameData.GetDay();
                        GameData.AddTrust(1);
                        OnToast?.Invoke("和候车的乘客聊了聊，口碑 +1（每天一次）");
                    }
                    else OnToast?.Invoke("今天已经和乘客们聊过了。");
                    break;
                case "platform":
                    OnToast?.Invoke($"NF-5K「耕牛」停在 1 道，车况 {GameData.GetTrainCondition()}%");
                    break;
                case "rail_shop":
                    OnDialogue?.Invoke("铁路商店", "柜台里摆着手旗、信号灯和检修扳手。", new Color(0.33f, 0.56f, 0.51f));
                    break;
                case "east_crossing":
                {
                    bool up = ExploreFixtures.Get("crossing_bar_n").target < 0.5f;
                    ExploreFixtures.Set("crossing_bar_n", up ? 1f : 0f);
                    ExploreFixtures.Set("crossing_bar_s", up ? 1f : 0f);
                    OnToast?.Invoke(up ? "抬起栏杆，旧村道放行。" : "放下栏杆，红灯开始交替闪烁。");
                    break;
                }
                case "signal_lever":
                {
                    bool green = ExploreFixtures.Get("signal_station").target < 0.5f;
                    ExploreFixtures.Set("signal_station", green ? 1f : 0f);
                    OnToast?.Invoke(green ? "扳下手柄，臂板落下——绿灯，进路开通。" : "手柄扳回，臂板抬起——红灯，禁止通过。");
                    break;
                }
                case "pond_look":
                    if (!pondFound)
                    {
                        pondFound = true; GameData.AddMoney(20);
                        OnToast?.Invoke("荷叶底下压着一张褪色的旧车票，背面写着「雾峰—松桥」。资金 +20");
                    }
                    else OnToast?.Invoke("水面上漂着几片荷叶，一只青蛙「扑通」跳进水里。");
                    break;
                case "tea_nails":
                    if (nailsFound < 3)
                    {
                        nailsFound++;
                        if (nailsFound < 3) OnToast?.Invoke($"杂草里露出一枚锈迹斑斑的旧道钉（{nailsFound}/3）");
                        else { GameData.AddMoney(60); OnToast?.Invoke("第三枚道钉！三枚凑成一套，老陈说能当镇纸，卖了 资金 +60"); }
                    }
                    else OnToast?.Invoke("茶田边的杂草里再也翻不出东西了。");
                    break;
                case "blossom":
                    if (blossomDay != GameData.GetDay())
                    {
                        blossomDay = GameData.GetDay();
                        Stamina = MaxStamina; SprintLocked = false;
                        OnToast?.Invoke("摇了摇樱树，花瓣落了一身，心情好多了。精力恢复（每天一次）");
                    }
                    else OnToast?.Invoke("树上的花瓣已经被你摇光了，明天再来。");
                    break;
                case "east_freight":
                    OnToast?.Invoke("货运栈台：枕木和零件已收进货棚。");
                    break;
                case "crossing":
                    OnToast?.Invoke("道口无异常（巡检效果待接入）");
                    break;
                default:
                    OnAction?.Invoke(s.id);
                    break;
            }
        }

        public void ResetDay()
        {
            Minutes = DayStart;
            lateNotified = false;
            ClearPath();
            Stamina = MaxStamina; SprintLocked = false;
            if (Indoors) UseArea(outdoor);
            player = new Vector2(20.5f * T, 8.5f * T); // 起床点：调度室门口
        }

        /// <summary>自动寻路前往某地点并在到达后触发交互；地点在别的区域（室内/室外）时会先走到门、切换区域后再继续。</summary>
        public void GoToSpot(string id)
        {
            string target = AreaOfSpot(id);
            if (target == null) return;
            void Arrive() { foreach (var sp in spots) if (sp.id == id) { NavigateToTile(sp.tx, sp.ty, true); return; } }
            if (RouteAcrossAreas(target, Arrive)) return;
            Arrive();
        }

        /// <summary>地图面板点击：目标是室外某格；人在室内时先走出门。</summary>
        public bool RouteToOutdoorTile(int tx, int ty, bool interact)
        {
            if (!Indoors) return NavigateToTile(tx, ty, interact);
            queuedAfterSwitch = () => NavigateToTile(tx, ty, interact);
            var d = ExitDoorTile();
            return NavigateToTile(d.x, d.y, false);
        }

        public void TeleportToSpot(string id)
        {
            foreach (var a in areas.Values)
                foreach (var s in a.spots)
                    if (s.id == id)
                    {
                        if (a != cur) { if (cur == outdoor) returnPos = player; UseArea(a); ClearPath(); }
                        player = SpotCenter(s);
                        return;
                    }
        }

        // ================= 绘制 =================
        // 室外视口高 16 格；室内整个房间居中显示（房间高 + 2 格留白）
        private float Scale => Mathf.Max(0.3f, contentRect.height / ((Indoors ? gh + 2.2f : 16f) * T));

        private Vector2 CameraOrigin(float scale)
        {
            float vw = contentRect.width / scale, vh = contentRect.height / scale;
            float cx = Mathf.Clamp(player.x, vw / 2f, Mathf.Max(vw / 2f, gw * T - vw / 2f));
            float cy = Mathf.Clamp(player.y, vh / 2f, Mathf.Max(vh / 2f, gh * T - vh / 2f));
            if (gw * T < vw) cx = gw * T / 2f;
            if (gh * T < vh) cy = gh * T / 2f;
            return new Vector2(cx - vw / 2f, cy - vh / 2f);
        }

        /// <summary>绘制带贴图的矩形（点采样图）。uv 方向若与预期相反，改 FlipV。</summary>
        private const bool FlipV = false;
        private const float FloorLift = 0.62f;
        private static void Quad(MeshGenerationContext mgc, Texture tex, float x, float y, float w, float h, bool mirror = false,
            float uMin = 0f, float uMax = 1f, float vMin = 0f, float vMax = 1f, byte leftAlpha = 255, byte alpha = 255)
        {
            if (tex == null) return;
            var mw = mgc.Allocate(4, 6, tex);
            float u0 = mirror ? uMax : uMin, u1 = mirror ? uMin : uMax;
            float vTop = FlipV ? vMin : vMax, vBot = FlipV ? vMax : vMin;
            Color32 white = new Color32(255, 255, 255, alpha);
            Color32 leftTint = new Color32(255, 255, 255, (byte)Mathf.Min(leftAlpha, alpha));
            mw.SetNextVertex(new Vertex { position = new Vector3(x, y, Vertex.nearZ), tint = leftTint, uv = new Vector2(u0, vTop) });
            mw.SetNextVertex(new Vertex { position = new Vector3(x + w, y, Vertex.nearZ), tint = white, uv = new Vector2(u1, vTop) });
            mw.SetNextVertex(new Vertex { position = new Vector3(x + w, y + h, Vertex.nearZ), tint = white, uv = new Vector2(u1, vBot) });
            mw.SetNextVertex(new Vertex { position = new Vector3(x, y + h, Vertex.nearZ), tint = leftTint, uv = new Vector2(u0, vBot) });
            mw.SetNextIndex(0); mw.SetNextIndex(1); mw.SetNextIndex(2);
            mw.SetNextIndex(0); mw.SetNextIndex(2); mw.SetNextIndex(3);
        }

        /// <summary>
        /// 室内（参考星露谷）：只有北墙有可见立面（墙纸上部 + 护墙板下部，GPT 素材），
        /// 左右与南侧只是地板边缘的一圈深色描边，外面是黑褐色虚空；门在南缘，用门垫表示。
        /// </summary>
        private void DrawInterior(MeshGenerationContext mgc, Painter2D p, Vector2 origin, float s)
        {
            Rect view = contentRect;
            Color void_ = new Color(0.13f, 0.10f, 0.08f), outline = new Color(0.20f, 0.13f, 0.09f);
            p.fillColor = void_;
            p.BeginPath(); Rect(p, 0, 0, view.width, view.height); p.Fill();
            float ts = T * s;
            Texture2D[] wood = cur.floor == "tile"
                ? new[] { Tex("int_floor_tile_01"), Tex("int_floor_tile_02"), Tex("int_floor_tile_01") }
                : new[] { Tex("int_floor_wood_01"), Tex("int_floor_wood_02"), Tex("int_floor_wood_03") };
            var wallTop = Tex("int_wall_top"); var wallFront = Tex("int_wall_front");
            float L = (1 * T - origin.x) * s, R = ((gw - 1) * T - origin.x) * s;      // 地板左右边界（屏幕）
            float N = (2 * T - origin.y) * s, S = ((gh - 1) * T - origin.y) * s;      // 北墙下沿 / 地板南缘
            // 地板：x 1..gw-2，y 2..gh-2
            for (int y = 2; y < gh - 1; y++)
                for (int x = 1; x < gw - 1; x++)
                {
                    float sx = (x * T - origin.x) * s, sy = (y * T - origin.y) * s;
                    Quad(mgc, wood[((x * 7 + y * 3) % 3 + 3) % 3], sx, sy, ts + 0.5f, ts + 0.5f);
                }
            // 地板提亮（GPT 原图偏深；以后换浅色贴图时把 FloorLift 设 0）
            if (FloorLift > 0f)
            {
                p.fillColor = new Color(0.98f, 0.96f, 0.91f, FloorLift);
                p.BeginPath(); Rect(p, L, N, R - L, S - N); p.Fill();
            }
            // 北墙：第 0 行顶帽 + 第 1 行正面（32×48，底边对齐第 1 行下沿）
            for (int x = 1; x < gw - 1; x++)
            {
                float sx = (x * T - origin.x) * s;
                Quad(mgc, wallTop, sx, (0 - origin.y) * s, ts + 0.5f, ts + 0.5f);
                Quad(mgc, wallFront, sx, N - 48f * s, ts + 0.5f, 48f * s);
            }
            p.fillColor = new Color(0.98f, 0.96f, 0.92f, 0.5f);   // 墙面提亮
            p.BeginPath(); Rect(p, L, (0 - origin.y) * s, R - L, N - (0 - origin.y) * s); p.Fill();
            // 北墙下沿的踢脚阴影，让墙与地面分开
            p.fillColor = new Color(0.30f, 0.20f, 0.14f, 0.35f);
            p.BeginPath(); Rect(p, L, N, R - L, 5f * s); p.Fill();
            // 描边：左右与南缘（门洞处断开），北墙顶部
            float ow = 6f * s;
            p.fillColor = outline;
            p.BeginPath();
            Rect(p, L - ow, (0 - origin.y) * s - ow, ow, S - (0 - origin.y) * s + 2f * ow);          // 左
            Rect(p, R, (0 - origin.y) * s - ow, ow, S - (0 - origin.y) * s + 2f * ow);               // 右
            Rect(p, L - ow, (0 - origin.y) * s - ow, R - L + 2f * ow, ow);                           // 北顶
            for (int x = 1; x < gw - 1; x++)
                if (grid[gh - 1, x] != 'D') Rect(p, (x * T - origin.x) * s, S, ts + 0.5f, ow);       // 南缘
            p.Fill();
            // 门垫（出口格）
            for (int x = 1; x < gw - 1; x++)
                if (grid[gh - 1, x] == 'D')
                {
                    float sx = (x * T - origin.x) * s;
                    p.fillColor = outline; p.BeginPath(); Rect(p, sx, S - ts * 0.02f, ts, ts * 0.78f); p.Fill();
                    p.fillColor = new Color(0.55f, 0.20f, 0.16f); p.BeginPath(); Rect(p, sx + ts * 0.08f, S + ts * 0.05f, ts * 0.84f, ts * 0.6f); p.Fill();
                    p.fillColor = new Color(0.82f, 0.62f, 0.34f); p.BeginPath(); Rect(p, sx + ts * 0.16f, S + ts * 0.13f, ts * 0.68f, ts * 0.06f); p.Fill();
                }
            // 家具：底边对齐 ty 行下沿，原图尺寸；墙面装饰先画，落地家具进入遮挡排序
            foreach (var pr in cur.props)
            {
                var tx = Tex(pr.tex);
                if (tx == null) continue;
                float sx = (pr.tx * T - origin.x) * s, sy = ((pr.ty + 1) * T - origin.y) * s;
                var txc = tx;
                if (pr.wall) Quad(mgc, tx, sx, sy - tx.height * s, tx.width * s, tx.height * s);
                else sortedExtras.Add(((pr.ty + 1) * T, () => Quad(mgc, txc, sx, sy - txc.height * s, txc.width * s, txc.height * s)));
            }
        }

        private Vector2 ScreenToWorld(Vector2 local)
        {
            float sc = Scale;
            return CameraOrigin(sc) + local / sc;
        }

        private static void Rect(Painter2D p, float x, float y, float w, float h)
        {
            p.MoveTo(new Vector2(x, y));
            p.LineTo(new Vector2(x + w, y));
            p.LineTo(new Vector2(x + w, y + h));
            p.LineTo(new Vector2(x, y + h));
            p.ClosePath();
        }

        private static void Disc(Painter2D p, Color c, Vector2 center, float r)
        {
            p.fillColor = c;
            p.BeginPath();
            p.Arc(center, r, Angle.Degrees(0), Angle.Degrees(360));
            p.Fill();
        }

        private void Draw(MeshGenerationContext mgc)
        {
            Rect view = contentRect;
            if (view.width <= 1f || view.height <= 1f) return;
            var p = mgc.painter2D;
            sortedExtras.Clear();   // 室内/室外切换后不得残留上一区域的树、列车
            float s = Scale;
            Vector2 origin = CameraOrigin(s);
            float ts = T * s;

            if (Indoors)
            {
                DrawInterior(mgc, p, origin, s);
                DrawOverlay(mgc, p, origin, s, ts, false);
                return;
            }
            var station = Tex("scene_wufeng_station_v2");
            var east = Tex("scene_wufeng_east");
            if (station != null && east != null)
            {
                DrawOutdoorArt(mgc, station, east, origin, s, ts);
                DrawOverlay(mgc, p, origin, s, ts, true);
                DrawFlyers(mgc, p, origin, s);
                DrawNight(mgc, p, origin, s, ts);
                DrawFog(mgc, p, origin, s);
                return;
            }
            int x0 = Mathf.Max(0, Mathf.FloorToInt(origin.x / T)), x1 = Mathf.Min(gw - 1, Mathf.CeilToInt((origin.x + view.width / s) / T));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(origin.y / T)), y1 = Mathf.Min(gh - 1, Mathf.CeilToInt((origin.y + view.height / s) / T));

            // 按颜色分桶，减少路径数量
            var buckets = new Dictionary<Color, List<Vector4>>();
            void Add(Color c, float x, float y, float w, float h)
            {
                if (!buckets.TryGetValue(c, out var list)) buckets[c] = list = new List<Vector4>();
                list.Add(new Vector4(x, y, w, h));
            }

            sortedExtras.Clear();
            var trees = new List<(Vector2 c, float key)>();
            for (int ty = y0; ty <= y1; ty++)
                for (int tx = x0; tx <= x1; tx++)
                {
                    char c = grid[ty, tx];
                    float sx = (tx * T - origin.x) * s, sy = (ty * T - origin.y) * s;
                    Color baseC;
                    switch (c)
                    {
                        case ',': baseC = Dirt; break;
                        case 'P': baseC = Plat; break;
                        case '=': case 'X': baseC = Ballast; break;
                        case '#': baseC = Wall; break;
                        case 'R': baseC = Roof; break;
                        case 'D': baseC = Door; break;
                        case 'f': baseC = Tea; break;
                        case '~': baseC = Water; break;
                        default: baseC = ((tx + ty) & 1) == 0 ? Grass1 : Grass2; break;
                    }
                    Add(baseC, sx, sy, ts + 0.6f, ts + 0.6f);

                    if (c == 'P' && ty + 1 < gh && (grid[ty + 1, tx] == '=' || grid[ty + 1, tx] == 'X'))
                    {
                        Add(new Color(0.79f, 0.63f, 0.26f), sx, sy + ts * 0.81f, ts + 0.15f, ts * 0.048f);
                    }
                    else if (c == '=' || c == 'X')
                    {
                        Add(Rail, sx, sy + ts * 0.25f, ts + 0.6f, Mathf.Max(2f, ts * 0.08f));
                        Add(Rail, sx, sy + ts * 0.68f, ts + 0.6f, Mathf.Max(2f, ts * 0.08f));
                        Add(Sleeper, sx + ts * 0.12f, sy + ts * 0.18f, ts * 0.14f, ts * 0.64f);
                        Add(Sleeper, sx + ts * 0.62f, sy + ts * 0.18f, ts * 0.14f, ts * 0.64f);
                    }
                    else if (c == '#')
                    {
                        // 立面：屋檐投影、整齐的窗（紧贴屋顶下第一排墙，隔格一扇，带窗框和窗台）、底部勒脚
                        bool firstRow = ty > 0 && grid[ty - 1, tx] == 'R';
                        bool lastRow = ty + 1 >= H || grid[ty + 1, tx] != '#';
                        if (firstRow) Add(new Color(0.40f, 0.27f, 0.21f), sx, sy, ts + 0.6f, ts * 0.14f);
                        if (firstRow && (tx & 1) == 0 && tx > 0 && tx + 1 < W && grid[ty, tx - 1] == '#' && grid[ty, tx + 1] == '#')
                        {
                            Add(new Color(0.30f, 0.20f, 0.14f), sx + ts * 0.2f, sy + ts * 0.26f, ts * 0.6f, ts * 0.56f);
                            Add(Win, sx + ts * 0.26f, sy + ts * 0.32f, ts * 0.48f, ts * 0.44f);
                            Add(new Color(0.30f, 0.20f, 0.14f), sx + ts * 0.48f, sy + ts * 0.32f, Mathf.Max(1.5f, ts * 0.04f), ts * 0.44f);
                            Add(new Color(0.78f, 0.66f, 0.50f), sx + ts * 0.16f, sy + ts * 0.82f, ts * 0.68f, ts * 0.07f);
                        }
                        if (lastRow) Add(new Color(0.46f, 0.33f, 0.26f), sx, sy + ts * 0.86f, ts + 0.6f, ts * 0.14f);
                    }
                    else if (c == 'F')
                    {
                        Add(Fence, sx, sy + ts * 0.4f, ts + 0.6f, ts * 0.12f);
                        Add(Fence, sx + ts * 0.4f, sy + ts * 0.2f, ts * 0.12f, ts * 0.6f);
                    }
                    else if (c == 'f' && (ty & 1) == 0)
                        Add(new Color(0.22f, 0.40f, 0.20f), sx, sy + ts * 0.4f, ts + 0.6f, ts * 0.18f);
                    else if (c == 'T')
                        trees.Add((new Vector2(sx + ts / 2f, sy + ts / 2f), (ty + 1) * T));
                }

            foreach (var kv in buckets)
            {
                p.fillColor = kv.Key;
                p.BeginPath();
                foreach (var r in kv.Value) Rect(p, r.x, r.y, r.z, r.w);
                p.Fill();
            }

            foreach (var tr in trees)
            {
                var t = tr.c;
                sortedExtras.Add((tr.key, () =>
                {
                    Disc(p, Shadow, t + new Vector2(0f, ts * 0.28f), ts * 0.38f);
                    Disc(p, Trunk, t + new Vector2(0f, ts * 0.22f), ts * 0.12f);
                    Disc(p, Canopy, t - new Vector2(0f, ts * 0.05f), ts * 0.46f);
                }));
            }

            DrawOverlay(mgc, p, origin, s, ts, true);
        }

        // 黄线和四条钢轨在两张原画边缘的实际行号。按这些行配准，避免拼接处错轨。
        private static readonly int[] StationSeamRows = { 0, 397, 440, 465, 486, 511, 971 };
        private static readonly int[] EastSeamRows = { 0, 642, 686, 716, 740, 770, 1536 };

        private void DrawOutdoorArt(MeshGenerationContext mgc, Texture station, Texture east, Vector2 origin, float s, float ts)
        {
            float x = -origin.x * s;
            float y = -origin.y * s;
            float height = H * ts;
            float stationWidth = StationWidth * ts;
            float eastWidth = (W - StationWidth) * ts;
            float seamX = x + stationWidth;
            float blendWidth = 0.75f * ts;
            float eastBlendU = blendWidth / eastWidth;
            Quad(mgc, station, x, y, stationWidth, height);
            Quad(mgc, station, seamX, y, blendWidth, height, true, uMin: 1f - blendWidth / stationWidth);
            for (int i = 0; i < StationSeamRows.Length - 1; i++)
            {
                float top = StationSeamRows[i] / (float)station.height;
                float bottom = StationSeamRows[i + 1] / (float)station.height;
                float minV = 1f - EastSeamRows[i + 1] / (float)east.height;
                float maxV = 1f - EastSeamRows[i] / (float)east.height;
                float bandY = y + top * height;
                float bandHeight = (bottom - top) * height;
                Quad(mgc, east, seamX, bandY, blendWidth, bandHeight, uMax: eastBlendU, vMin: minV, vMax: maxV, leftAlpha: 0);
                Quad(mgc, east, seamX + blendWidth, bandY, eastWidth - blendWidth, bandHeight, uMin: eastBlendU, vMin: minV, vMax: maxV);
            }
            DrawFixtures(mgc, mgc.painter2D, origin, s);
            // 线路老化磨损叠层：1 经常保养 / 2 临时补修 / 3 评定失格（0 状态合格=整图原样）。叠层自成一套，一次只画一档。
            int grade = Mathf.Clamp(GameData.DecayGrade, 0, 3);
            if (grade > 0)
                DrawSceneLayer(mgc, Tex($"scene_wufeng_station_v2_wear{grade}"), Tex($"scene_wufeng_east_wear{grade}"), origin, s, ts, 255);
        }

        /// <summary>整图级叠层（与两张整图同尺寸）：站区直铺，东段按钢轨行逐段配准，与 DrawOutdoorArt 一致。</summary>
        private void DrawSceneLayer(MeshGenerationContext mgc, Texture gs, Texture ge, Vector2 origin, float s, float ts, byte alpha)
        {
            float x = -origin.x * s, y = -origin.y * s, height = H * ts;
            float stationWidth = StationWidth * ts, eastWidth = (W - StationWidth) * ts;
            if (gs != null) Quad(mgc, gs, x, y, stationWidth, height, alpha: alpha);
            if (ge == null) return;
            var east = Tex("scene_wufeng_east"); var station = Tex("scene_wufeng_station_v2");
            if (east == null || station == null) return;
            float seamX = x + stationWidth;
            for (int i = 0; i < StationSeamRows.Length - 1; i++)
            {
                float top = StationSeamRows[i] / (float)station.height, bottom = StationSeamRows[i + 1] / (float)station.height;
                float minV = 1f - EastSeamRows[i + 1] / (float)east.height, maxV = 1f - EastSeamRows[i] / (float)east.height;
                Quad(mgc, ge, seamX, y + top * height, eastWidth, (bottom - top) * height, vMin: minV, vMax: maxV, alpha: alpha);
            }
        }

        /// <summary>飞行器影子：有 fx_flyer_shadow_a/b/c 用贴图（机头朝右，向左飞镜像），否则程序影子。</summary>
        private void DrawFlyers(MeshGenerationContext mgc, Painter2D p, Vector2 origin, float s)
        {
            foreach (var f in ExploreFlyers.Active)
            {
                float w = f.size.x * s, h = f.size.y * s;
                float x = (f.pos.x - origin.x) * s - w / 2f, y = (f.pos.y - origin.y) * s - h / 2f;
                if (x > contentRect.width || y > contentRect.height || x + w < 0 || y + h < 0) continue;
                var tex = Tex("fx_flyer_shadow_" + (char)('a' + f.type));
                if (tex != null) { Quad(mgc, tex, x, y, w, h, mirror: f.vel.x < 0f); continue; }
                p.fillColor = new Color(0f, 0f, 0f, 0.26f);
                p.BeginPath(); Rect(p, x + w * 0.05f, y + h * 0.40f, w * 0.90f, h * 0.20f); p.Fill();      // 机身
                p.BeginPath(); Rect(p, x + w * (f.vel.x > 0f ? 0.34f : 0.46f), y, w * 0.20f, h); p.Fill();   // 机翼
                p.BeginPath(); Rect(p, x + w * (f.vel.x > 0f ? 0.05f : 0.78f), y + h * 0.28f, w * 0.17f, h * 0.44f); p.Fill(); // 尾翼
            }
        }

        /// <summary>晨雾（6:00–9:00 渐退）：有 Scenes/fog_tile.png（256×256 可平铺）就滚动叠加，没有就一层淡雾色。</summary>
        private void DrawFog(MeshGenerationContext mgc, Painter2D p, Vector2 origin, float s)
        {
            float h = Minutes / 60f;
            float fog = Mathf.Clamp01((9f - h) / 3f);
            if (fog <= 0.01f) return;
            p.fillColor = new Color(0.86f, 0.90f, 0.92f, 0.20f * fog);
            p.BeginPath(); Rect(p, 0, 0, contentRect.width, contentRect.height); p.Fill();
            var tile = Tex("fog_tile");
            if (tile == null) return;
            float size = 256f * s * 1.6f, drift = (Time.unscaledTime * 6f * s) % size;
            byte a = (byte)Mathf.RoundToInt(255f * 0.55f * fog);
            for (float yy = -size; yy < contentRect.height + size; yy += size)
                for (float xx = -size; xx < contentRect.width + size; xx += size)
                    Quad(mgc, tile, xx + drift, yy, size, size, alpha: a);
        }

        /// <summary>夜色强度 0..1：6:00–6:30 微暗，18:00 起渐入，21:00 最深。</summary>
        public float Night
        {
            get
            {
                float h = Minutes / 60f;
                if (h < 6.6f) return Mathf.Lerp(0.45f, 0f, Mathf.Clamp01((h - 6f) / 0.6f));
                if (h >= 18f) return Mathf.Clamp01((h - 18f) / 3f) * 0.9f;
                return 0f;
            }
        }

        /// <summary>
        /// 夜晚：先整体压一层冷蓝色，再叠「光晕层」（scene_wufeng_*_glow.png：窗光、灯晕，
        /// 与原图同尺寸、除发光处全透明）。没有光晕图时只有色调。
        /// </summary>
        private void DrawNight(MeshGenerationContext mgc, Painter2D p, Vector2 origin, float s, float ts)
        {
            float n = Night;
            if (n <= 0.01f) return;
            p.fillColor = new Color(0.06f, 0.09f, 0.26f, 0.56f * n);
            p.BeginPath(); Rect(p, 0, 0, contentRect.width, contentRect.height); p.Fill();
            byte a = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(n * 1.15f));
            DrawSceneLayer(mgc, Tex("scene_wufeng_station_v2_glow"), Tex("scene_wufeng_east_glow"), origin, s, ts, a);
        }

        /// <summary>
        /// 动画补丁：门/栏杆/灯/信号机/环境循环。有 fx_&lt;id&gt; 帧条贴图就取对应帧，
        /// 没有则只对「门」画程序占位（深色门洞），其余保持原画。
        /// </summary>
        private void DrawFixtures(MeshGenerationContext mgc, Painter2D p, Vector2 origin, float s)
        {
            foreach (var f in ExploreFixtures.All)
            {
                if (!ExploreFixtures.NeedsPatch(f)) continue;
                Rect r = ExploreFixtures.ToWorld(f, T, StationWidth, H);
                float x = (r.x - origin.x) * s, y = (r.y - origin.y) * s, w = r.width * s, h = r.height * s;
                if (x > contentRect.width || y > contentRect.height || x + w < 0 || y + h < 0) continue;
                var tex = Tex(f.asset);
                if (tex != null)
                {
                    int fr = f.Frame;
                    Quad(mgc, tex, x, y, w, h, uMin: fr / (float)f.frames, uMax: (fr + 1) / (float)f.frames);
                }
                else if (f.kind == FixtureKind.Door)
                {
                    float k = f.t;
                    float gw2 = w * 0.62f * k, gx = x + (w - gw2) * 0.5f;
                    p.fillColor = new Color(0.10f, 0.07f, 0.05f, 0.82f);
                    p.BeginPath(); Rect(p, gx, y + h * 0.10f, gw2, h * 0.86f); p.Fill();
                    p.fillColor = new Color(1f, 0.82f, 0.45f, 0.32f * k);
                    p.BeginPath(); Rect(p, gx, y + h * 0.80f, gw2, h * 0.16f); p.Fill();
                }
            }
        }

        private void DrawOverlay(MeshGenerationContext mgc, Painter2D p, Vector2 origin, float s, float ts, bool outdoorTrain)
        {
            // 自动寻路路线与终点
            if (path != null)
            {
                p.fillColor = new Color(1f, 0.92f, 0.45f, 0.75f);
                p.BeginPath();
                for (int i = pathIdx; i < path.Count; i++)
                {
                    Vector2 c = (path[i] - origin) * s;
                    p.MoveTo(c + new Vector2(ts * 0.09f, 0f));
                    p.Arc(c, ts * 0.09f, Angle.Degrees(0), Angle.Degrees(360));
                }
                p.Fill();
                Vector2 gc = (path[path.Count - 1] - origin) * s;
                p.strokeColor = new Color(1f, 0.92f, 0.45f, 0.95f);
                p.lineWidth = 3f;
                p.BeginPath();
                p.Arc(gc, ts * 0.3f, Angle.Degrees(0), Angle.Degrees(360));
                p.Stroke();
            }

            if (outdoorTrain) sortedExtras.Add((11.95f * T, () => DrawTrain(mgc, p, origin, s)));

            // 脚步扬尘
            foreach (var dd in dust)
            {
                float k = dd.age / dd.life;
                p.fillColor = new Color(0.82f, 0.75f, 0.62f, 0.42f * (1f - k));
                Vector2 dp = (dd.pos - origin) * s + new Vector2(0f, -k * 9f * s);
                p.BeginPath(); p.Arc(dp, (dd.size + k * 4f) * s, Angle.Degrees(0), Angle.Degrees(360)); p.Fill();
            }

            // 实体按 y 排序绘制（32×48 规格角色：有真图用真图，否则程序精绘）
            var drawList = new List<(float y, Action draw)>();
            int walkFrame = ((int)(walkPhase / 1.55f)) & 3;
            float now = Time.realtimeSinceStartup;
            for (int i = 0; i < npcs.Count; i++)
            {
                var n = npcs[i];
                Vector2 toPlayer = player - n.pos;
                CharDir nd = toPlayer.magnitude < 3f * T ? DirOf(toPlayer) : CharDir.Down;
                drawList.Add((n.pos.y, () => DrawChar(mgc, p, n.id, n.pos, origin, s, nd, 0, false, now)));
            }
            drawList.Add((player.y, () => DrawChar(mgc, p, "player", player, origin, s, DirOf(facing), walkFrame, IsMoving, now)));
            drawList.AddRange(sortedExtras);
            drawList.Sort((a, b) => a.y.CompareTo(b.y));
            foreach (var d in drawList) d.draw();

            // 交互点标记
            foreach (var sp in spots)
            {
                if (Indoors && Vector2.Distance(player, SpotCenter(sp)) > 2.6f * T) continue;   // 室内只在靠近时显示光圈
                Vector2 c = (SpotCenter(sp) - origin) * s;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * 3f);
                p.strokeColor = new Color(1f, 0.9f, 0.4f, 0.45f + 0.35f * pulse);
                p.lineWidth = 2.5f;
                p.BeginPath();
                p.Arc(c, ts * (0.42f + 0.06f * pulse), Angle.Degrees(0), Angle.Degrees(360));
                p.Stroke();
            }

            // NPC 名牌跟随
            for (int i = 0; i < npcs.Count; i++)
            {
                if (!npcLabels.TryGetValue(npcs[i].id, out var lb)) continue;
                Vector2 sp = (npcs[i].pos - origin) * s;
                lb.style.display = DisplayStyle.Flex;
                lb.style.left = sp.x - 22f;
                lb.style.top = sp.y - ts * 1.38f;
            }
        }

        private void DrawTrain(MeshGenerationContext mgc, Painter2D p, Vector2 origin, float s)
        {
            float ts = T * s;
            Vector2 a = (new Vector2(8f * T, 11f * T) - origin) * s;
            var train = Tex("train_nf5k_parked");
            if (train != null)
            {
                float height = 2.05f * ts;
                float wheelLine = a.y + 0.54f * ts;
                Quad(mgc, train, a.x, wheelLine - height, 7f * ts, height);
                return;
            }
            // NF-5K 朝鲜原厂深蓝涂装的头车与两节客厢；正式贴图接入前的灰盒示意。
            Color[] cols = { new Color(0.10f, 0.22f, 0.48f), new Color(0.10f, 0.22f, 0.48f), new Color(0.10f, 0.22f, 0.48f) };
            float[] lens = { 3f, 2f, 2f };
            float x = a.x;
            for (int i = 0; i < 3; i++)
            {
                float w = lens[i] * ts - 4f;
                p.fillColor = Shadow; p.BeginPath(); Rect(p, x + 2f, a.y + ts * 0.8f, w, ts * 0.18f); p.Fill();
                p.fillColor = cols[i]; p.BeginPath(); Rect(p, x + 2f, a.y - ts * 0.35f, w, ts * 1.1f); p.Fill();
                p.fillColor = new Color(0.96f, 0.92f, 0.85f); p.BeginPath(); Rect(p, x + 2f, a.y + ts * 0.47f, w, ts * 0.07f); p.Fill();
                p.fillColor = new Color(0.80f, 0.16f, 0.18f); p.BeginPath(); Rect(p, x + 2f, a.y + ts * 0.56f, w, ts * 0.08f); p.Fill();
                p.fillColor = new Color(0.85f, 0.92f, 0.98f); p.BeginPath();
                if (i == 0) Rect(p, x + 2f + w * 0.62f, a.y - ts * 0.15f, w * 0.28f, ts * 0.4f);
                else { Rect(p, x + 2f + w * 0.12f, a.y - ts * 0.15f, w * 0.3f, ts * 0.4f); Rect(p, x + 2f + w * 0.56f, a.y - ts * 0.15f, w * 0.3f, ts * 0.4f); }
                p.Fill();
                x += lens[i] * ts;
            }
        }

        private static CharDir DirOf(Vector2 v)
        {
            if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) return v.x < 0f ? CharDir.Left : CharDir.Right;
            return v.y < 0f ? CharDir.Up : CharDir.Down;
        }

        private void DrawChar(MeshGenerationContext mgc, Painter2D p, string id, Vector2 world, Vector2 origin, float s, CharDir dir, int frame, bool moving, float time)
        {
            Vector2 feet = (world - origin) * s + new Vector2(0f, 9f * s);
            var tex = ExploreCharacters.TrySprite(id, moving, dir, frame, out bool mirror);
            if (tex != null)
            {
                // 位图角色与程序绘制一样参与 y 排序（不再用 Image 子元素，避免永远盖在家具/树上）
                const float playerSpriteScale = 1.0625f;
                float spriteScale = id == "player" ? s * playerSpriteScale : s;
                Quad(mgc, tex, feet.x - 16f * spriteScale, feet.y - 48f * spriteScale,
                    32f * spriteScale, 48f * spriteScale, mirror);
                return;
            }
            ExploreCharacters.Draw(p, ExploreCharacters.Get(id), feet, s, dir, frame, moving, time);
        }
    }
}
