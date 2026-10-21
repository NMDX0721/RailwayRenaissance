using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 平板应用：归途（蔚蓝档案两级架构 · 主线剧情时间轴与各幕回溯）
    /// 从 GameMainUI 剥离（T306 Batch 1-B）
    /// </summary>
    public static class TabletReturnJourneyUI
    {
    // ============ App: 归途（蔚蓝档案两级架构 · 本作深棕像素调色）============
    private static int returnJourneyCurrentChapter = 0;
    private static bool returnJourneyInDetail = false;

    public static readonly (string id, string chapterNum, string title, string subTitle, string desc, string coverBg)[] StoryChapters = {
        ("prologue", "序章", "归乡", "雾峰村线重开纪事 · 破晓到明天", "带着金日成综合大学毕业行囊回到沉寂四年的雾峰村，拾起万能道尺，重组先民班组，唤醒0721号动力机车。", "bg/station"),
        ("chapter1", "第一章", "求生", "沙能风暴逼近 · 荒野生机", "沙能载具步步紧逼，在严酷的沙币结算体系下，维持雾峰村线基本运力与村民信任。", "bg/platform_evening"),
        ("chapter2", "第二章", "扩张", "联通乌金岭 · 铁轨向前", "勘探荒废的重载矿区支线，克服设施老化与政治压力，打通区域经济大动脉。", "bg/railway_track"),
        ("chapter3", "第三章", "对抗", "沙能铁龙暗面 · 基金博弈", "沙能巨头以保护之名行垄断之实，嘉颖徐铁路基金携手地方守护铁道尊严。", "bg/hangar"),
        ("chapter4", "终章", "复兴", "汽笛破长空 · 岁月如歌", "全线鸣笛通车，先民铁道科技与沙本位经济终迎曙光，重塑归途传奇。", "bg/songqiao_station")
    };

    public static readonly (string scriptId, string episode, string title, string desc)[] PrologueEpisodes = {
        ("prologue_01_news", "01", "破晓之行", "金日成综合大学荣誉毕业，带着行囊回到雾峰村。"),
        ("prologue_02_day0", "02", "归乡旧站", "老旧的站房与铁栅售票口，四年前停运的日历仍未翻动。"),
        ("prologue_03_journey", "03", "荒草长轨", "铁轨长满杂草，锈迹斑斑的双孔石拱桥跨过溪水。"),
        ("prologue_04_arrival", "04", "雾峰暮色", "老陈守在月台，茶园梯田笼罩在金黄色的晚霞里。"),
        ("prologue_05_inspection", "05", "铁轨与道尺 ★样板", "拿起爷爷留下的万能道尺，摔尺、正尺，排查轨距绝不妥协。"),
        ("prologue_06_team", "06", "故人相见", "聚集村中的老机修工张工与调度员，重组先民班组。"),
        ("prologue_07_first_repair", "07", "首试扳手", "机库深处的0721号机车，拧紧轴承的第一声金属撞击。"),
        ("prologue_08_first_run", "08", "汽笛重响", "沙能点火，雾峰村线上空响起了沉寂四年的第一声鸣笛。"),
        ("prologue_09_funding", "09", "筹措前路", "嘉颖徐铁路基金的援助与来自乌金岭矿区的猜忌。"),
        ("prologue_10_transition", "10", "走向明天", "支线正式通车，准备迎接新的沙本位经济挑战。")
    };

    public static bool IsStoryChapterUnlocked(int chIdx)
    {
        if (PlayerPrefs.GetInt("DebugUnlockAllChapters", 0) == 1) return true;
        // 主线剧情当前仅开放序章，后续章节打问号锁定
        return chIdx == 0;
    }

    public static bool IsStoryChapterCompleted(int chIdx)
    {
        if (chIdx == 0)
        {
            return PlayerPrefs.GetInt("ArchiveScene_prologue_10_transition", 0) == 1
                || PlayerPrefs.GetInt("ArchiveStory_prologue_10_transition", 0) == 1
                || PlayerPrefs.GetInt("VN_Got0721", 0) == 1;
        }
        return false;
    }

    public static bool IsStoryEpisodeUnlocked(int epIdx)
    {
        if (PlayerPrefs.GetInt("DebugUnlockAllChapters", 0) == 1) return true;
        if (epIdx == 0) return true; // 第 1 幕默认解锁
        string prevScript = PrologueEpisodes[epIdx - 1].scriptId;
        return PlayerPrefs.GetInt("ArchiveScene_" + prevScript, 0) == 1
            || PlayerPrefs.GetInt("ArchiveStory_" + prevScript, 0) == 1
            || PlayerPrefs.GetInt("VN_Got0721", 0) == 1;
    }

    public static bool IsStoryEpisodeCompleted(int epIdx)
    {
        string curScript = PrologueEpisodes[epIdx].scriptId;
        return PlayerPrefs.GetInt("ArchiveScene_" + curScript, 0) == 1
            || PlayerPrefs.GetInt("ArchiveStory_" + curScript, 0) == 1
            || PlayerPrefs.GetInt("VN_Got0721", 0) == 1;
    }

        private static Action s_onExitApp;
    private static Action<string> s_showToast;

    private static void ShowToast(string msg)
    {
        if (s_showToast != null) s_showToast(msg);
        else Debug.Log("[ReturnJourney] " + msg);
    }

    public static void BuildReturnJourneyApp(VisualElement win, Action onExitApp = null, Action<string> showToast = null)
    {
        if (onExitApp != null) s_onExitApp = onExitApp;
        if (showToast != null) s_showToast = showToast;
        win.Clear();

        if (!returnJourneyInDetail)
        {
            // ===== 一级视图：主线剧情 · 铁路线时间轴（上下错落 + 斜切卡片 + 仅序章开放）=====
            var rootCol = new VisualElement();
            rootCol.style.flexGrow = 1;
            rootCol.style.backgroundColor = SurfaceApp;
            rootCol.style.paddingLeft = 20; rootCol.style.paddingRight = 20;
            rootCol.style.paddingTop = 14; rootCol.style.paddingBottom = 16;
            win.Add(rootCol);

            // 顶栏：标题、名言与运营状态
            var topHeader = new VisualElement();
            topHeader.style.flexDirection = FlexDirection.Row;
            topHeader.style.justifyContent = Justify.SpaceBetween;
            topHeader.style.alignItems = Align.Center;
            topHeader.style.marginBottom = 10;
            rootCol.Add(topHeader);

            var titleLeft = new VisualElement();
            titleLeft.style.flexDirection = FlexDirection.Row;
            titleLeft.style.alignItems = Align.Center;
            topHeader.Add(titleLeft);

            var mainTitle = new Label("主线剧情 · Main Story");
            mainTitle.style.fontSize = 22;
            mainTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            mainTitle.style.color = InkPrimary;
            mainTitle.style.unityFontDefinition = Fd();
            titleLeft.Add(mainTitle);

            var act2Btn = new Button(() => ShowToast("第二部主线正在规划中，敬请期待！")) { text = "切换至第二部 ❯" };
            act2Btn.style.height = 28;
            act2Btn.style.fontSize = 11; act2Btn.style.unityFontDefinition = Fd();
            act2Btn.style.backgroundColor = SurfaceCard;
            act2Btn.style.color = InkSecondary;
            act2Btn.style.borderTopLeftRadius = 8; act2Btn.style.borderBottomRightRadius = 8;
            act2Btn.style.borderTopWidth = 1; act2Btn.style.borderBottomWidth = 1;
            act2Btn.style.borderLeftWidth = 1; act2Btn.style.borderRightWidth = 1;
            act2Btn.style.borderTopColor = BorderDivider; act2Btn.style.borderBottomColor = BorderDivider;
            act2Btn.style.borderLeftColor = BorderDivider; act2Btn.style.borderRightColor = BorderDivider;
            act2Btn.style.marginLeft = 16;
            titleLeft.Add(act2Btn);

            var quoteLbl = new Label("『铁路最动人的不是出发，是回来。每一趟归途，都是一次胜利。』");
            quoteLbl.style.fontSize = 12; quoteLbl.style.color = InkTertiary; quoteLbl.style.unityFontDefinition = Fd();
            topHeader.Add(quoteLbl);

            // 主体分栏：左侧水平滚动时间轴 + 右侧固定章节明细面板
            var mainRow = new VisualElement();
            mainRow.style.flexDirection = FlexDirection.Row;
            mainRow.style.flexGrow = 1;
            rootCol.Add(mainRow);

            // 左侧：横向滚动时间轴
            var timelineScrollView = new ScrollView(ScrollViewMode.Horizontal);
            timelineScrollView.style.flexGrow = 1;
            timelineScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            timelineScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            timelineScrollView.style.marginRight = 16;
            mainRow.Add(timelineScrollView);

            // 时间轴容器（宽 1480px，高 350px）
            var timelineContainer = new VisualElement();
            timelineContainer.style.width = 1480;
            timelineContainer.style.height = 350;
            timelineContainer.style.position = Position.Relative;
            timelineScrollView.Add(timelineContainer);

            // 铁路线中心轴（双轨 + 枕木工业手绘）
            var trackAxis = new VisualElement();
            trackAxis.style.position = Position.Absolute;
            trackAxis.style.left = 20; trackAxis.style.right = 20; trackAxis.style.top = 170;
            trackAxis.style.height = 8;
            trackAxis.style.backgroundColor = new Color(0.24f, 0.22f, 0.20f, 0.85f);
            trackAxis.style.borderTopLeftRadius = 4; trackAxis.style.borderBottomRightRadius = 4;
            timelineContainer.Add(trackAxis);

            // 轨面高亮双线（琥珀金色铁轨）
            var railTop = new VisualElement();
            railTop.style.position = Position.Absolute;
            railTop.style.left = 0; railTop.style.right = 0; railTop.style.top = 0; railTop.style.height = 2;
            railTop.style.backgroundColor = AccentSystem;
            trackAxis.Add(railTop);

            var railBottom = new VisualElement();
            railBottom.style.position = Position.Absolute;
            railBottom.style.left = 0; railBottom.style.right = 0; railBottom.style.bottom = 0; railBottom.style.height = 2;
            railBottom.style.backgroundColor = AccentSystem;
            trackAxis.Add(railBottom);

            // 枕木线条
            for (int s = 30; s < 1440; s += 36)
            {
                var sleeper = new VisualElement();
                sleeper.style.position = Position.Absolute;
                sleeper.style.left = s; sleeper.style.top = 166;
                sleeper.style.width = 3; sleeper.style.height = 16;
                sleeper.style.backgroundColor = new Color(0.38f, 0.32f, 0.26f, 0.7f);
                timelineContainer.Add(sleeper);
            }

            // 章节卡片上下交错排列（偶数在轴上，奇数在轴下，斜切平行四边形）
            for (int i = 0; i < StoryChapters.Length; i++)
            {
                int chIdx = i;
                var ch = StoryChapters[i];
                bool unlocked = IsStoryChapterUnlocked(i);
                bool completed = IsStoryChapterCompleted(i);
                bool isSelected = (returnJourneyCurrentChapter == chIdx);

                float cardLeft = 40 + i * 280;
                bool isTop = (i % 2 == 0); // 偶数在上，奇数在下

                // 连接线与站标圆钮
                var connLine = new VisualElement();
                connLine.style.position = Position.Absolute;
                connLine.style.left = cardLeft + 120;
                connLine.style.width = 2;
                connLine.style.backgroundColor = isSelected ? AccentSystem : (unlocked ? new Color(0.6f, 0.5f, 0.4f, 0.8f) : new Color(0.7f, 0.7f, 0.7f, 0.5f));
                if (isTop)
                {
                    connLine.style.top = 150; connLine.style.height = 22;
                }
                else
                {
                    connLine.style.top = 176; connLine.style.height = 22;
                }
                timelineContainer.Add(connLine);

                // 站标圆纽
                var stationNode = new VisualElement();
                stationNode.style.position = Position.Absolute;
                stationNode.style.left = cardLeft + 114; stationNode.style.top = 167;
                stationNode.style.width = 14; stationNode.style.height = 14;
                stationNode.style.borderTopLeftRadius = 7; stationNode.style.borderTopRightRadius = 7;
                stationNode.style.borderBottomLeftRadius = 7; stationNode.style.borderBottomRightRadius = 7;
                stationNode.style.backgroundColor = isSelected ? AccentSystem : (unlocked ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f));
                stationNode.style.borderTopWidth = 2; stationNode.style.borderBottomWidth = 2;
                stationNode.style.borderLeftWidth = 2; stationNode.style.borderRightWidth = 2;
                stationNode.style.borderTopColor = isSelected ? CrimsonSeal : AccentSystem;
                stationNode.style.borderBottomColor = isSelected ? CrimsonSeal : AccentSystem;
                stationNode.style.borderLeftColor = isSelected ? CrimsonSeal : AccentSystem;
                stationNode.style.borderRightColor = isSelected ? CrimsonSeal : AccentSystem;
                timelineContainer.Add(stationNode);

                // 斜切卡片本体
                var card = new VisualElement();
                card.style.position = Position.Absolute;
                card.style.left = cardLeft;
                card.style.top = isTop ? 14 : 196;
                card.style.width = 250; card.style.height = 138;
                card.style.borderTopLeftRadius = 24; card.style.borderBottomRightRadius = 24;
                card.style.borderTopWidth = isSelected ? 2.5f : 1.5f;
                card.style.borderBottomWidth = isSelected ? 2.5f : 1.5f;
                card.style.borderLeftWidth = isSelected ? 2.5f : 1.5f;
                card.style.borderRightWidth = isSelected ? 2.5f : 1.5f;
                card.style.borderTopColor = isSelected ? AccentSystem : (unlocked ? BorderDivider : new Color(0.85f, 0.85f, 0.88f, 1f));
                card.style.borderBottomColor = isSelected ? AccentSystem : (unlocked ? BorderDivider : new Color(0.85f, 0.85f, 0.88f, 1f));
                card.style.borderLeftColor = isSelected ? AccentSystem : (unlocked ? BorderDivider : new Color(0.85f, 0.85f, 0.88f, 1f));
                card.style.borderRightColor = isSelected ? AccentSystem : (unlocked ? BorderDivider : new Color(0.85f, 0.85f, 0.88f, 1f));
                card.style.backgroundColor = unlocked ? (isSelected ? new Color(1f, 0.98f, 0.94f, 1f) : SurfaceCard) : new Color(0.93f, 0.93f, 0.95f, 0.95f);
                card.style.overflow = Overflow.Hidden;
                card.style.justifyContent = Justify.SpaceBetween;
                card.pickingMode = PickingMode.Position;
                timelineContainer.Add(card);

                // 卡片右上斜切徽章
                var badge = new VisualElement();
                badge.style.position = Position.Absolute;
                badge.style.top = 8; badge.style.right = 8;
                badge.style.paddingLeft = 8; badge.style.paddingRight = 8;
                badge.style.paddingTop = 3; badge.style.paddingBottom = 3;
                badge.style.borderTopLeftRadius = 8; badge.style.borderBottomRightRadius = 8;
                badge.style.backgroundColor = completed ? CrimsonSeal : (unlocked ? AccentSystem : new Color(0.6f, 0.6f, 0.65f, 1f));
                card.Add(badge);

                var badgeTxt = new Label(completed ? "Clear ★" : (unlocked ? "探索中" : "待开放 🔒"));
                badgeTxt.style.fontSize = 10; badgeTxt.style.color = Color.white; badgeTxt.style.unityFontDefinition = Fd();
                badge.Add(badgeTxt);

                // 卡片正文
                var cardBody = new VisualElement();
                cardBody.style.paddingLeft = 14; cardBody.style.paddingRight = 14; cardBody.style.paddingTop = 10;
                card.Add(cardBody);

                var chNumLbl = new Label(unlocked ? ch.chapterNum : (ch.chapterNum + " · ???"));
                chNumLbl.style.fontSize = 12; chNumLbl.style.color = unlocked ? AccentSystem : InkTertiary;
                chNumLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                chNumLbl.style.unityFontDefinition = Fd();
                cardBody.Add(chNumLbl);

                var chDescLbl = new Label(unlocked ? ch.desc : "后续章节正在规划中，完成前序章节方可开放。");
                chDescLbl.style.fontSize = 11; chDescLbl.style.color = unlocked ? InkSecondary : InkTertiary;
                chDescLbl.style.whiteSpace = WhiteSpace.Normal;
                chDescLbl.style.marginTop = 4; chDescLbl.style.height = 48;
                cardBody.Add(chDescLbl);

                // 卡底实色斜切标题条（纯白实底 + 深色大字）
                var bottomBar = new VisualElement();
                bottomBar.style.height = 36;
                bottomBar.style.backgroundColor = unlocked ? (isSelected ? AccentSystem : new Color(0.96f, 0.96f, 0.98f, 1f)) : new Color(0.88f, 0.88f, 0.90f, 1f);
                bottomBar.style.borderTopWidth = 1; bottomBar.style.borderTopColor = BorderDivider;
                bottomBar.style.justifyContent = Justify.Center;
                bottomBar.style.alignItems = Align.Center;
                card.Add(bottomBar);

                var titleLbl = new Label(unlocked ? (ch.chapterNum + " · " + ch.title) : "???");
                titleLbl.style.fontSize = 13;
                titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLbl.style.color = unlocked ? (isSelected ? Color.white : InkPrimary) : InkTertiary;
                titleLbl.style.unityFontDefinition = Fd();
                bottomBar.Add(titleLbl);

                // 交互点击
                if (unlocked)
                {
                    card.RegisterCallback<ClickEvent>(_ =>
                    {
                        returnJourneyCurrentChapter = chIdx;
                        BuildReturnJourneyApp(win);
                    });
                }
                else
                {
                    card.RegisterCallback<ClickEvent>(_ => ShowToast("后续章节尚未开放，请先完成前置序章！"));
                }
            }

            // 右侧：固定章节明细面板（270px 宽，斜切风格）
            var sidePanel = new VisualElement();
            sidePanel.style.width = 270;
            sidePanel.style.backgroundColor = SurfaceCard;
            sidePanel.style.borderTopLeftRadius = 18; sidePanel.style.borderBottomRightRadius = 18;
            sidePanel.style.borderTopWidth = 1; sidePanel.style.borderBottomWidth = 1;
            sidePanel.style.borderLeftWidth = 1; sidePanel.style.borderRightWidth = 1;
            sidePanel.style.borderTopColor = BorderDivider; sidePanel.style.borderBottomColor = BorderDivider;
            sidePanel.style.borderLeftColor = BorderDivider; sidePanel.style.borderRightColor = BorderDivider;
            sidePanel.style.paddingLeft = 16; sidePanel.style.paddingRight = 16;
            sidePanel.style.paddingTop = 16; sidePanel.style.paddingBottom = 16;
            sidePanel.style.flexDirection = FlexDirection.Column;
            sidePanel.style.justifyContent = Justify.SpaceBetween;
            mainRow.Add(sidePanel);

            var panelTopBox = new VisualElement();
            sidePanel.Add(panelTopBox);

            var curChData = StoryChapters[returnJourneyCurrentChapter];
            bool curUnlocked = IsStoryChapterUnlocked(returnJourneyCurrentChapter);

            var panelHead = new Label(curUnlocked ? ($"▼ {curChData.chapterNum} · {curChData.title}") : ($"▼ {curChData.chapterNum} · ??? 🔒"));
            panelHead.style.fontSize = 16; panelHead.style.color = AccentSystem;
            panelHead.style.unityFontStyleAndWeight = FontStyle.Bold;
            panelHead.style.unityFontDefinition = Fd();
            panelTopBox.Add(panelHead);

            var panelSub = new Label(curUnlocked ? curChData.subTitle : "前置剧目尚未全部完成");
            panelSub.style.fontSize = 12; panelSub.style.color = InkSecondary; panelSub.style.unityFontDefinition = Fd();
            panelSub.style.marginTop = 4; panelSub.style.marginBottom = 12;
            panelTopBox.Add(panelSub);

            // 剧目预览列表
            if (curUnlocked && returnJourneyCurrentChapter == 0)
            {
                var actScroll = new ScrollView();
                actScroll.style.height = 170;
                actScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                panelTopBox.Add(actScroll);

                for (int p = 0; p < PrologueEpisodes.Length; p++)
                {
                    var pep = PrologueEpisodes[p];
                    bool pUnlocked = IsStoryEpisodeUnlocked(p);
                    bool pDone = IsStoryEpisodeCompleted(p);

                    var pRow = new VisualElement();
                    pRow.style.flexDirection = FlexDirection.Row;
                    pRow.style.justifyContent = Justify.SpaceBetween;
                    pRow.style.alignItems = Align.Center;
                    pRow.style.paddingLeft = 8; pRow.style.paddingRight = 8;
                    pRow.style.paddingTop = 4; pRow.style.paddingBottom = 4;
                    pRow.style.marginBottom = 3;
                    pRow.style.backgroundColor = pUnlocked ? new Color(0.96f, 0.96f, 0.98f, 1f) : new Color(0.92f, 0.92f, 0.94f, 0.6f);
                    pRow.style.borderTopLeftRadius = 6; pRow.style.borderBottomRightRadius = 6;
                    actScroll.Add(pRow);

                    var pTitle = new Label($"{pep.episode}. {pep.title}");
                    pTitle.style.fontSize = 11;
                    pTitle.style.color = pUnlocked ? InkPrimary : InkTertiary;
                    pTitle.style.unityFontDefinition = Fd();
                    pRow.Add(pTitle);

                    var pTag = new Label(pDone ? "★" : (pUnlocked ? "▶" : "🔒"));
                    pTag.style.fontSize = 10;
                    pTag.style.color = pDone ? CrimsonSeal : (pUnlocked ? AccentSystem : InkTertiary);
                    pTag.style.unityFontDefinition = Fd();
                    pRow.Add(pTag);
                }
            }
            else
            {
                var lockBox = new VisualElement();
                lockBox.style.height = 170;
                lockBox.style.justifyContent = Justify.Center;
                lockBox.style.alignItems = Align.Center;
                panelTopBox.Add(lockBox);

                var lockIc = new Label("🔒");
                lockIc.style.fontSize = 28; lockIc.style.unityFontDefinition = Fd();
                lockBox.Add(lockIc);

                var lockTxt = new Label("本章节尚未开放探索");
                lockTxt.style.fontSize = 12; lockTxt.style.color = InkTertiary; lockTxt.style.unityFontDefinition = Fd();
                lockTxt.style.marginTop = 6;
                lockBox.Add(lockTxt);
            }

            // 右侧面板底端：斜切进入按钮
            var enterChapterBtn = new Button(() =>
            {
                if (curUnlocked)
                {
                    returnJourneyInDetail = true;
                    BuildReturnJourneyApp(win);
                }
                else
                {
                    ShowToast("本章节尚未开放！");
                }
            }) { text = curUnlocked ? "前往幕次列表 · Enter ❯" : "尚未开放 🔒" };
            enterChapterBtn.style.height = 40;
            enterChapterBtn.style.fontSize = 13; enterChapterBtn.style.unityFontDefinition = Fd();
            enterChapterBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            enterChapterBtn.style.backgroundColor = curUnlocked ? AccentSystem : new Color(0.8f, 0.8f, 0.85f, 1f);
            enterChapterBtn.style.color = Color.white;
            enterChapterBtn.style.borderTopLeftRadius = 12; enterChapterBtn.style.borderBottomRightRadius = 12;
            enterChapterBtn.style.borderTopWidth = 0; enterChapterBtn.style.borderBottomWidth = 0;
            enterChapterBtn.style.borderLeftWidth = 0; enterChapterBtn.style.borderRightWidth = 0;
            sidePanel.Add(enterChapterBtn);

            // 底部栏：通关进度条 + 最终幕特别篇按钮
            var bottomActionRow = new VisualElement();
            bottomActionRow.style.flexDirection = FlexDirection.Row;
            bottomActionRow.style.justifyContent = Justify.SpaceBetween;
            bottomActionRow.style.alignItems = Align.Center;
            bottomActionRow.style.marginTop = 12;
            rootCol.Add(bottomActionRow);

            int doneEps = 0;
            for (int k = 0; k < PrologueEpisodes.Length; k++) if (IsStoryEpisodeCompleted(k)) doneEps++;
            var progBox = new VisualElement();
            progBox.style.flexDirection = FlexDirection.Row;
            progBox.style.alignItems = Align.Center;
            bottomActionRow.Add(progBox);

            var progLbl = new Label($"全线通关进度：{doneEps} / {PrologueEpisodes.Length} 幕");
            progLbl.style.fontSize = 12; progLbl.style.color = InkSecondary; progLbl.style.unityFontDefinition = Fd();
            progBox.Add(progLbl);

            var progBarBg = new VisualElement();
            progBarBg.style.width = 160; progBarBg.style.height = 8;
            progBarBg.style.backgroundColor = BorderDivider;
            progBarBg.style.borderTopLeftRadius = 4; progBarBg.style.borderBottomRightRadius = 4;
            progBarBg.style.marginLeft = 10;
            progBox.Add(progBarBg);

            var progFill = new VisualElement();
            progFill.style.height = 8;
            progFill.style.width = Length.Percent((float)doneEps / PrologueEpisodes.Length * 100f);
            progFill.style.backgroundColor = AccentSystem;
            progFill.style.borderTopLeftRadius = 4; progFill.style.borderBottomRightRadius = 4;
            progBarBg.Add(progFill);

            var finalEpBtn = new Button(() => ShowToast("序章终局通关后开放特别篇。")) { text = "Final Ep. · 终章特别篇" };
            finalEpBtn.style.height = 32;
            finalEpBtn.style.fontSize = 12; finalEpBtn.style.unityFontDefinition = Fd();
            finalEpBtn.style.backgroundColor = SurfaceCard;
            finalEpBtn.style.color = InkSecondary;
            finalEpBtn.style.borderTopLeftRadius = 8; finalEpBtn.style.borderBottomRightRadius = 8;
            finalEpBtn.style.borderTopWidth = 1; finalEpBtn.style.borderBottomWidth = 1;
            finalEpBtn.style.borderLeftWidth = 1; finalEpBtn.style.borderRightWidth = 1;
            finalEpBtn.style.borderTopColor = BorderDivider; finalEpBtn.style.borderBottomColor = BorderDivider;
            finalEpBtn.style.borderLeftColor = BorderDivider; finalEpBtn.style.borderRightColor = BorderDivider;
            bottomActionRow.Add(finalEpBtn);
        }
        else
        {
            // ===== 二级视图：明细页（左固定大插画 + 右侧从下往上显示节 + 自动定位最新解锁项）=====
            var curCh = StoryChapters[returnJourneyCurrentChapter];
            bool isCompleted = IsStoryChapterCompleted(returnJourneyCurrentChapter);

            var rootCol = new VisualElement();
            rootCol.style.flexGrow = 1;
            rootCol.style.backgroundColor = SurfaceApp;
            rootCol.style.paddingLeft = 20; rootCol.style.paddingRight = 20;
            rootCol.style.paddingTop = 14; rootCol.style.paddingBottom = 16;
            win.Add(rootCol);

            // 顶部导航栏
            var topNav = new VisualElement();
            topNav.style.flexDirection = FlexDirection.Row;
            topNav.style.justifyContent = Justify.SpaceBetween;
            topNav.style.alignItems = Align.Center;
            topNav.style.marginBottom = 12;
            rootCol.Add(topNav);

            var backBtn = new Button(() =>
            {
                returnJourneyInDetail = false;
                BuildReturnJourneyApp(win);
            }) { text = "❮ 返回章节总览" };
            backBtn.style.height = 34;
            backBtn.style.fontSize = 13; backBtn.style.unityFontDefinition = Fd();
            backBtn.style.backgroundColor = SurfaceCard;
            backBtn.style.color = InkPrimary;
            backBtn.style.borderTopLeftRadius = 10; backBtn.style.borderBottomRightRadius = 10;
            backBtn.style.borderTopWidth = 1; backBtn.style.borderBottomWidth = 1;
            backBtn.style.borderLeftWidth = 1; backBtn.style.borderRightWidth = 1;
            backBtn.style.borderTopColor = BorderDivider; backBtn.style.borderBottomColor = BorderDivider;
            backBtn.style.borderLeftColor = BorderDivider; backBtn.style.borderRightColor = BorderDivider;
            topNav.Add(backBtn);

            var statusBanner = new VisualElement();
            statusBanner.style.backgroundColor = isCompleted ? CrimsonSeal : AccentSystem;
            statusBanner.style.paddingLeft = 14; statusBanner.style.paddingRight = 14;
            statusBanner.style.paddingTop = 6; statusBanner.style.paddingBottom = 6;
            statusBanner.style.borderTopLeftRadius = 12; statusBanner.style.borderBottomRightRadius = 12;
            topNav.Add(statusBanner);

            var statusBannerTxt = new Label(isCompleted ? "★ 全部剧幕已通关 (All Episodes Cleared)" : "◆ 剧幕正在探索中 (In Progress)");
            statusBannerTxt.style.fontSize = 12; statusBannerTxt.style.color = Color.white; statusBannerTxt.style.unityFontDefinition = Fd();
            statusBanner.Add(statusBannerTxt);

            // 明细主体分栏
            var detailRow = new VisualElement();
            detailRow.style.flexDirection = FlexDirection.Row;
            detailRow.style.flexGrow = 1;
            rootCol.Add(detailRow);

            // 左侧：画报插画面板（320px，斜切 + 印章）
            var leftPoster = new VisualElement();
            leftPoster.style.width = 320;
            leftPoster.style.backgroundColor = SurfaceCard;
            leftPoster.style.borderTopLeftRadius = 20; leftPoster.style.borderBottomRightRadius = 20;
            leftPoster.style.borderTopWidth = 1; leftPoster.style.borderBottomWidth = 1;
            leftPoster.style.borderLeftWidth = 1; leftPoster.style.borderRightWidth = 1;
            leftPoster.style.borderTopColor = BorderDivider; leftPoster.style.borderBottomColor = BorderDivider;
            leftPoster.style.borderLeftColor = BorderDivider; leftPoster.style.borderRightColor = BorderDivider;
            leftPoster.style.marginRight = 16;
            leftPoster.style.overflow = Overflow.Hidden;
            leftPoster.style.position = Position.Relative;
            detailRow.Add(leftPoster);

            var coverSp = Resources.Load<Sprite>(curCh.coverBg);
            if (coverSp != null)
            {
                var coverImg = new Image { sprite = coverSp };
                coverImg.style.position = Position.Absolute;
                coverImg.style.top = 0; coverImg.style.left = 0; coverImg.style.right = 0; coverImg.style.bottom = 0;
                coverImg.scaleMode = ScaleMode.ScaleAndCrop;
                leftPoster.Add(coverImg);

                var posterMask = new VisualElement();
                posterMask.style.position = Position.Absolute;
                posterMask.style.top = 0; posterMask.style.left = 0; posterMask.style.right = 0; posterMask.style.bottom = 0;
                posterMask.style.backgroundColor = new Color(0.10f, 0.08f, 0.06f, 0.55f);
                leftPoster.Add(posterMask);
            }

            var posterInfo = new VisualElement();
            posterInfo.style.position = Position.Absolute;
            posterInfo.style.left = 18; posterInfo.style.right = 18; posterInfo.style.bottom = 20;
            leftPoster.Add(posterInfo);

            var postChNum = new Label(curCh.chapterNum);
            postChNum.style.fontSize = 14; postChNum.style.color = AccentSystem;
            postChNum.style.unityFontStyleAndWeight = FontStyle.Bold;
            postChNum.style.unityFontDefinition = Fd();
            posterInfo.Add(postChNum);

            var postTitle = new Label(curCh.title);
            postTitle.style.fontSize = 24; postTitle.style.color = Color.white; postTitle.style.unityFontDefinition = Fd();
            postTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            posterInfo.Add(postTitle);

            var postSub = new Label(curCh.subTitle);
            postSub.style.fontSize = 12; postSub.style.color = new Color(0.9f, 0.9f, 0.9f, 0.9f); postSub.style.unityFontDefinition = Fd();
            postSub.style.marginTop = 4;
            posterInfo.Add(postSub);

            var postDesc = new Label(curCh.desc);
            postDesc.style.fontSize = 11; postDesc.style.color = new Color(0.85f, 0.85f, 0.85f, 0.85f);
            postDesc.style.whiteSpace = WhiteSpace.Normal;
            postDesc.style.marginTop = 8;
            posterInfo.Add(postDesc);

            // COMPLETE! 圆形公文印章
            var seal = new VisualElement();
            seal.style.position = Position.Absolute;
            seal.style.right = 14; seal.style.bottom = 14;
            seal.style.width = 72; seal.style.height = 72;
            seal.style.borderTopLeftRadius = 36; seal.style.borderTopRightRadius = 36;
            seal.style.borderBottomLeftRadius = 36; seal.style.borderBottomRightRadius = 36;
            seal.style.borderTopWidth = 2; seal.style.borderBottomWidth = 2;
            seal.style.borderLeftWidth = 2; seal.style.borderRightWidth = 2;
            seal.style.borderTopColor = isCompleted ? CrimsonSeal : AccentSystem;
            seal.style.borderBottomColor = isCompleted ? CrimsonSeal : AccentSystem;
            seal.style.borderLeftColor = isCompleted ? CrimsonSeal : AccentSystem;
            seal.style.borderRightColor = isCompleted ? CrimsonSeal : AccentSystem;
            seal.style.backgroundColor = isCompleted ? new Color(0.55f, 0.23f, 0.16f, 0.35f) : new Color(0.72f, 0.52f, 0.23f, 0.25f);
            seal.style.alignItems = Align.Center;
            seal.style.justifyContent = Justify.Center;
            leftPoster.Add(seal);

            var sealStar = new Label(isCompleted ? "★ ★ ★" : "◆ ◆ ◆");
            sealStar.style.fontSize = 10; sealStar.style.color = isCompleted ? CrimsonSeal : AccentSystem; sealStar.style.unityFontDefinition = Fd();
            seal.Add(sealStar);

            var sealTxt = new Label(isCompleted ? "COMPLETE!" : "IN PROGRESS");
            sealTxt.style.fontSize = 9; sealTxt.style.color = isCompleted ? CrimsonSeal : AccentSystem;
            sealTxt.style.unityFontStyleAndWeight = FontStyle.Bold;
            sealTxt.style.unityFontDefinition = Fd();
            seal.Add(sealTxt);

            // 右侧：从下往上编号条目列表（斜切平行四边形 + 默认定位到最新解锁节）
            var epScrollView = new ScrollView();
            epScrollView.style.flexGrow = 1;
            epScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            detailRow.Add(epScrollView);

            // 计算最新解锁的剧目索引（用于默认自动滚动定位）
            int latestUnlockedIdx = 0;
            for (int k = 0; k < PrologueEpisodes.Length; k++)
            {
                if (IsStoryEpisodeUnlocked(k)) latestUnlockedIdx = k;
            }

            VisualElement targetFocusElement = null;

            // 倒序遍历：10 幕倒序生成，使最底部的为第 01 幕，从下往上逐节推进
            for (int i = PrologueEpisodes.Length - 1; i >= 0; i--)
            {
                int epIdx = i;
                var ep = PrologueEpisodes[i];
                bool unlocked = IsStoryEpisodeUnlocked(i);
                bool epDone = IsStoryEpisodeCompleted(i);

                // 外层条目容器（包含条外大号序号 + 斜切条本体）
                var itemRow = new VisualElement();
                itemRow.style.flexDirection = FlexDirection.Row;
                itemRow.style.alignItems = Align.Center;
                itemRow.style.marginBottom = 10;
                epScrollView.Add(itemRow);

                // 条外大号序号（蔚蓝档案原图：左下两位序号 01, 02...）
                var numLbl = new Label(ep.episode);
                numLbl.style.width = 38;
                numLbl.style.fontSize = 22;
                numLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                numLbl.style.color = unlocked ? (epDone ? AccentSystem : InkPrimary) : InkTertiary;
                numLbl.style.unityFontDefinition = Fd();
                numLbl.style.unityTextAlign = TextAnchor.MiddleRight;
                numLbl.style.marginRight = 10;
                itemRow.Add(numLbl);

                // 斜切条本体（平行四边形切角）
                var item = new VisualElement();
                item.style.flexGrow = 1;
                item.style.flexDirection = FlexDirection.Row;
                item.style.justifyContent = Justify.SpaceBetween;
                item.style.alignItems = Align.Center;
                item.style.backgroundColor = unlocked ? SurfaceCard : new Color(0.93f, 0.93f, 0.95f, 0.8f);
                item.style.borderTopLeftRadius = 14; item.style.borderBottomRightRadius = 14;
                item.style.borderTopWidth = 1; item.style.borderBottomWidth = 1;
                item.style.borderLeftWidth = unlocked ? (epDone ? 1 : 2.5f) : 1; item.style.borderRightWidth = 1;
                item.style.borderTopColor = unlocked ? (epDone ? BorderDivider : AccentSystem) : BorderDivider;
                item.style.borderBottomColor = unlocked ? (epDone ? BorderDivider : AccentSystem) : BorderDivider;
                item.style.borderLeftColor = unlocked ? (epDone ? BorderDivider : AccentSystem) : BorderDivider;
                item.style.borderRightColor = unlocked ? (epDone ? BorderDivider : AccentSystem) : BorderDivider;
                item.style.paddingLeft = 14; item.style.paddingRight = 14;
                item.style.paddingTop = 10; item.style.paddingBottom = 10;
                itemRow.Add(item);

                if (i == latestUnlockedIdx)
                {
                    targetFocusElement = itemRow;
                }

                // 左半部分：图标 + 标题 + 描述
                var leftPart = new VisualElement();
                leftPart.style.flexDirection = FlexDirection.Row;
                leftPart.style.alignItems = Align.Center;
                item.Add(leftPart);

                var docIcon = new Label(unlocked ? (epDone ? "📜" : "📐") : "🔒");
                docIcon.style.fontSize = 16; docIcon.style.marginRight = 10;
                docIcon.style.unityFontDefinition = Fd();
                leftPart.Add(docIcon);

                var infoBox = new VisualElement();
                leftPart.Add(infoBox);

                var epTitleLbl = new Label("第 " + ep.episode + " 幕 · " + ep.title);
                epTitleLbl.style.fontSize = 14;
                epTitleLbl.style.color = unlocked ? InkPrimary : InkTertiary;
                epTitleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                epTitleLbl.style.unityFontDefinition = Fd();
                infoBox.Add(epTitleLbl);

                var epDescLbl = new Label(unlocked ? ep.desc : "前置剧目尚未通关，完成前一幕即可解锁。");
                epDescLbl.style.fontSize = 11;
                epDescLbl.style.color = unlocked ? InkSecondary : InkTertiary;
                epDescLbl.style.unityFontDefinition = Fd();
                epDescLbl.style.marginTop = 2;
                infoBox.Add(epDescLbl);

                // 右半部分：状态 + 斜切进入按钮
                var rightPart = new VisualElement();
                rightPart.style.flexDirection = FlexDirection.Row;
                rightPart.style.alignItems = Align.Center;
                item.Add(rightPart);

                var stLbl = new Label(epDone ? "已通关 ★" : (unlocked ? "可开始 ▶" : "未解锁 🔒"));
                stLbl.style.fontSize = 11;
                stLbl.style.color = epDone ? new Color(0.2f, 0.65f, 0.35f, 1f) : (unlocked ? AccentSystem : InkTertiary);
                stLbl.style.unityFontDefinition = Fd();
                stLbl.style.marginRight = 12;
                rightPart.Add(stLbl);

                string btnText = unlocked ? (epDone ? "重温本幕" : "开始本幕") : "未解锁";
                var replayBtn = new Button(() =>
                {
                    if (unlocked)
                    {
                        s_onExitApp?.Invoke();
                        MainStoryUI.StartEpisodeStatic(ep.scriptId);
                    }
                    else
                    {
                        ShowToast("本幕尚未解锁，请先完成前置剧目。");
                    }
                }) { text = btnText };
                replayBtn.style.height = 32;
                replayBtn.style.paddingLeft = 14; replayBtn.style.paddingRight = 14;
                replayBtn.style.fontSize = 12; replayBtn.style.unityFontDefinition = Fd();
                replayBtn.style.backgroundColor = unlocked ? (epDone ? new Color(0.92f, 0.92f, 0.95f, 1f) : AccentSystem) : new Color(0.85f, 0.85f, 0.88f, 0.6f);
                replayBtn.style.color = unlocked ? (epDone ? InkPrimary : Color.white) : InkTertiary;
                replayBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                replayBtn.style.borderTopLeftRadius = 8; replayBtn.style.borderBottomRightRadius = 8;
                replayBtn.style.borderTopWidth = 0; replayBtn.style.borderBottomWidth = 0;
                replayBtn.style.borderLeftWidth = 0; replayBtn.style.borderRightWidth = 0;
                replayBtn.SetEnabled(unlocked);
                rightPart.Add(replayBtn);
            }

            // 默认定位到最新解锁的地方（如果仅第 1 幕则定位在最下面）
            epScrollView.schedule.Execute(() =>
            {
                if (targetFocusElement != null)
                {
                    epScrollView.ScrollTo(targetFocusElement);
                }
                else
                {
                    epScrollView.scrollOffset = new Vector2(0, 99999f);
                }
            });
        }
    }


    }
}
