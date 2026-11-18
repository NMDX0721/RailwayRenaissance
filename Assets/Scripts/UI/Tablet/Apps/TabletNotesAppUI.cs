using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 平板应用：媒姬百科与笔记便签（三栏联动 + 真实运营数据集成）
    /// 从 GameMainUI 剥离（T306 Batch 1-D）
    /// </summary>
    public static class TabletNotesAppUI
    {
    // ============ App: 笔记（三栏可编辑 + 真实数据自动化联动）============
    private static int currentNoteCategoryIndex = 0; // 0:全部笔记, 1:剧情书签, 2:运营备忘, 3:乘务日志, 4:机车定检, 5:成就里程碑, 6:待办事项
    private static int currentSelectedNoteId = -1;
    private static bool s_isVoiceRecording = false; // 选中的笔记 ID
    private static TextField noteTitleField;
    private static TextField noteBodyField;
    private static Label noteMetaLabel;

    public static void ReleaseTaskView(VisualElement root)
    {
        if (root == null)
        {
            noteTitleField = null;
            noteBodyField = null;
            noteMetaLabel = null;
            return;
        }
        if (noteTitleField != null && (root == noteTitleField || root.Contains(noteTitleField))) noteTitleField = null;
        if (noteBodyField != null && (root == noteBodyField || root.Contains(noteBodyField))) noteBodyField = null;
        if (noteMetaLabel != null && (root == noteMetaLabel || root.Contains(noteMetaLabel))) noteMetaLabel = null;
    }

    [System.Serializable]
    private class TabletNoteItem
    {
        public int id;
        public string category;
        public string title;
        public string date;
        public string author;
        public string content;
        public bool isSystemGenerated;
    }

    [System.Serializable]
    private class TabletNotesContainer
    {
        public System.Collections.Generic.List<TabletNoteItem> notes = new System.Collections.Generic.List<TabletNoteItem>();
    }

    private static System.Collections.Generic.List<TabletNoteItem> userTabletNotes = null;

    private static void LoadUserTabletNotes()
    {
        userTabletNotes = new System.Collections.Generic.List<TabletNoteItem>();
        string json = PlayerPrefs.GetString("TabletNotes", "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var container = JsonUtility.FromJson<TabletNotesContainer>(json);
                if (container != null && container.notes != null)
                {
                    userTabletNotes = container.notes;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[TabletNotes] Load failed: " + ex.Message);
            }
        }

        // 首次加载或没有预置笔记时，植入默认初始化笔记
        if (userTabletNotes.Count == 0)
        {
            userTabletNotes.Add(new TabletNoteItem
            {
                id = 1,
                category = "运营备忘",
                title = "雾峰村线重开财务备忘与调度复盘",
                date = RailwayRenaissance.UI.Tablet.TabletDateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                author = "林彪悍",
                content = $"今天是我们正式重启雾峰村线的第 {GameData.Day} 天。\n\n" +
                          $"根据调度系统的核算，今天沿线茶农与通勤客流达到了 {GameData.ExpectedPassengers} 人次。\n" +
                          $"当前账面可用沙币为 {GameData.Money:N0}，车况保持在 {GameData.TrainCondition}%。\n\n" +
                          "老陈说很多年没有在月台上听见这么响亮的汽笛声了。虽然沙能集团在周边城市的渗透仍在扩张，但只要我们守住这条铁道，这片土地的温度就不会熄灭。\n\n" +
                          "明日待办：检查松桥站水泵供水管道，核验道尺水平度。",
                isSystemGenerated = false
            });
            userTabletNotes.Add(new TabletNoteItem
            {
                id = 2,
                category = "机车定检",
                title = "0721号机车初次点火定检记录",
                date = "昨日 18:30",
                author = "张工",
                content = "沙能点火器压力维持在正常范围，但轴瓦润滑油有轻微渗漏，建议在第3天前重新注润滑油。\n锅炉安全阀已校验，蒸汽管路密封垫已换新。",
                isSystemGenerated = false
            });
            userTabletNotes.Add(new TabletNoteItem
            {
                id = 3,
                category = "乘务日志",
                title = "松桥道口道尺检定与道钉加固",
                date = "前天 09:12",
                author = "老陈",
                content = "松桥站前后枕木有轻微松动，已打入新道钉8根。道尺测量轨距为1435mm标准轨距，水平误差在允差±2mm以内。",
                isSystemGenerated = false
            });
            SaveUserTabletNotes();
        }

        // 动态数据化联动（自动追加，不重复）：
        // 1. 自动抓取 AchievementManager 解锁列表
        var allAchs = AchievementManager.GetAll();
        if (allAchs != null)
        {
            foreach (var ach in allAchs)
            {
                if (ach.unlocked)
                {
                    string achTitle = $"★ 成就解锁：{ach.title}";
                    if (!userTabletNotes.Exists(n => n.title == achTitle))
                    {
                        userTabletNotes.Add(new TabletNoteItem
                        {
                            id = 1000 + ach.GetHashCode() % 9000,
                            category = "成就里程碑",
                            title = achTitle,
                            date = string.IsNullOrEmpty(ach.unlockedDate) ? RailwayRenaissance.UI.Tablet.TabletDateTime.Now.ToString("yyyy-MM-dd") : ach.unlockedDate,
                            author = "系统记录",
                            content = $"【荣誉成就已达成】\n\n成就名称：{ach.title}\n稀有度：{ach.rarity}\n达成描述：{ach.description}\n\n铁道总署特发此状，以资鼓励！",
                            isSystemGenerated = true
                        });
                    }
                }
            }
        }

        // 2. 自动抓取 BookmarkManager 剧情书签
        var autoBms = BookmarkManager.GetAllAuto();
        if (autoBms != null)
        {
            foreach (var bm in autoBms)
            {
                if (bm != null && !string.IsNullOrEmpty(bm.scriptName))
                {
                    string bmTitle = $"剧情书签：第{bm.episodeNum}话 {bm.episodeTitle}";
                    if (!userTabletNotes.Exists(n => n.title == bmTitle))
                    {
                        userTabletNotes.Add(new TabletNoteItem
                        {
                            id = 2000 + bm.GetHashCode() % 8000,
                            category = "剧情书签",
                            title = bmTitle,
                            date = RailwayRenaissance.UI.Tablet.TabletDateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                            author = "岁月AI同步",
                            content = $"【剧目断点存盘】\n\n剧目：第{bm.episodeNum}话 {bm.episodeTitle}\n脚本：{bm.scriptName} (场景{bm.sceneIndex}, 对话{bm.dialogueIndex})\n摘抄：{bm.previewText}\n\n随时可在归途或VN中重温。",
                            isSystemGenerated = true
                        });
                    }
                }
            }
        }

        // 3. 自动抓取 GameData 当日运营周报摘要
        string weeklyTitle = $"运营第 {GameData.Day} 天运营简报";
        if (!userTabletNotes.Exists(n => n.title == weeklyTitle))
        {
            userTabletNotes.Add(new TabletNoteItem
            {
                id = 3000 + GameData.Day,
                category = "运营备忘",
                title = weeklyTitle,
                date = RailwayRenaissance.UI.Tablet.TabletDateTime.Now.ToString("yyyy-MM-dd"),
                author = "调度中心",
                content = $"【雾峰村线每日运营摘要】\n\n" +
                          $"• 运营天数：第 {GameData.Day} 天\n" +
                          $"• 账面可用沙币：{GameData.Money:N0} 沙币\n" +
                          $"• 0721机车车况：{GameData.TrainCondition}%\n" +
                          $"• 沿线居民信任度：{GameData.Trust}%\n" +
                          $"• 今日预计客流：{GameData.ExpectedPassengers} 人次\n" +
                          $"• 票价方案：35 沙币/人\n\n" +
                          $"调度评语：运行平稳，注意在沙能风暴前储备足够滤芯。",
                isSystemGenerated = true
            });
        }
    }

    private static void SaveUserTabletNotes()
    {
        if (userTabletNotes == null) return;
        var container = new TabletNotesContainer();
        foreach (var n in userTabletNotes)
        {
            if (!n.isSystemGenerated) // 仅持久化用户自建与编辑的笔记
                container.notes.Add(n);
        }
        string json = JsonUtility.ToJson(container);
        PlayerPrefs.SetString("TabletNotes", json);
        PlayerPrefs.Save();
    }

    private static Action<string> s_showToast;

    private static void ShowToast(string msg)
    {
        if (s_showToast != null) s_showToast(msg);
        else Debug.Log("[Notes] " + msg);
    }

    public static void BuildNotesApp(VisualElement win, Action<string> showToast = null)
    {
        if (showToast != null) s_showToast = showToast;
        ReleaseTaskView(win);
        win.Clear();
        if (userTabletNotes == null) LoadUserTabletNotes();
        var achievements = new Button(() => TabletHistoryUI.BuildAchievements(win, () => BuildNotesApp(win))) { text = "成就记录", name = "notes-achievements" };
        achievements.style.height = 48;
        TabletBookmarksUI.StyleAction(achievements);
        achievements.style.alignSelf = Align.FlexStart;
        achievements.style.flexShrink = 0;
        win.Add(achievements);

        // 统一浅色背景应用主体（三栏架构：左导航 200px + 中列表 290px + 右正文 flexGrow）
        var noteCols = new VisualElement();
        noteCols.style.flexDirection = FlexDirection.Row;
        noteCols.style.flexGrow = 1;
        noteCols.style.backgroundColor = SurfaceApp;
        win.Add(noteCols);

        // ===== 左栏：分类树（200px 浅灰底 Inset 导航）=====
        var catCol = new VisualElement();
        catCol.style.width = 205;
        catCol.style.backgroundColor = SurfaceCard;
        catCol.style.borderRightWidth = 1;
        catCol.style.borderRightColor = BorderDivider;
        catCol.style.paddingLeft = 12; catCol.style.paddingRight = 12;
        catCol.style.paddingTop = 16; catCol.style.paddingBottom = 16;
        catCol.style.flexDirection = FlexDirection.Column;
        catCol.style.justifyContent = Justify.SpaceBetween;
        noteCols.Add(catCol);

        var catList = new ScrollView();
        catList.style.flexGrow = 1;
        catList.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        catList.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        catCol.Add(catList);

        var catHeader = new Label("便签与笔记");
        catHeader.style.fontSize = 18; catHeader.style.color = InkPrimary;
        catHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        catHeader.style.unityFontDefinition = Fd();
        catHeader.style.marginBottom = 12;
        catList.Add(catHeader);

        // 语音速记挂载胶囊按键（小米超级岛联动）
        var voiceBtn = new Button(() =>
        {
            s_isVoiceRecording = !s_isVoiceRecording;
            if (s_isVoiceRecording)
            {
                RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.Instance?.RegisterSession(new RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.IslandSession
                {
                    AppType = RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.IslandAppType.Recorder,
                    Priority = 75,
                    Title = "乘务调度口令速记",
                    Subtitle = "正在录制便签声音备忘",
                    Metric = "录音中 00:15",
                    AccentColor = new Color(0.92f, 0.25f, 0.25f, 1f),
                    OnPrimaryAction = () => { /* toggle */ }
                });
                ShowToast("语音速记已开启，已在小米超级岛置顶常驻！");
            }
            else
            {
                RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.Instance?.RemoveSession(RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.IslandAppType.Recorder);
                ShowToast("语音速记已停止，音频文件已自动关联到当前便签！");
            }
            BuildNotesApp(win);
        }) { text = s_isVoiceRecording ? "正在录制 (点击停止)" : "语音速记 · 超级岛" };
        voiceBtn.style.height = 34;
        voiceBtn.style.fontSize = 12; voiceBtn.style.unityFontDefinition = Fd();
        voiceBtn.style.backgroundColor = s_isVoiceRecording ? new Color(0.92f, 0.25f, 0.25f, 1f) : SurfaceHover;
        voiceBtn.style.color = s_isVoiceRecording ? Color.white : InkPrimary;
        voiceBtn.style.borderTopLeftRadius = RadiusPill; voiceBtn.style.borderTopRightRadius = RadiusPill;
        voiceBtn.style.borderBottomLeftRadius = RadiusPill; voiceBtn.style.borderBottomRightRadius = RadiusPill;
        voiceBtn.style.borderTopWidth = 0; voiceBtn.style.borderBottomWidth = 0;
        voiceBtn.style.borderLeftWidth = 0; voiceBtn.style.borderRightWidth = 0;
        voiceBtn.style.marginBottom = 12;
        catList.Add(voiceBtn);

        // 分类定义与现代色彩
        string[] cats = { "全部笔记", "剧情书签", "运营备忘", "乘务日志", "机车定检", "成就里程碑", "待办事项" };
        string[] catIcons = { "📝", "🔖", "📊", "📋", "🔧", "🏆", "✅" };
        Color[] catColors = {
            new Color(0.23f, 0.51f, 0.96f, 1f), // 亮蓝
            new Color(0.92f, 0.28f, 0.60f, 1f), // 玫瑰粉
            new Color(0.96f, 0.62f, 0.10f, 1f), // 暖金
            new Color(0.12f, 0.72f, 0.50f, 1f), // 薄荷绿
            new Color(0.85f, 0.45f, 0.15f, 1f), // 琥珀
            new Color(0.55f, 0.32f, 0.95f, 1f), // 紫罗兰
            new Color(0.15f, 0.70f, 0.42f, 1f)  // 安全绿
        };

        // 顶部核心两项：全部笔记 + 剧情书签
        for (int i = 0; i <= 1; i++)
        {
            int catIdx = i;
            bool isSelected = (catIdx == currentNoteCategoryIndex);
            int count = userTabletNotes.FindAll(n => catIdx == 0 || n.category == cats[catIdx]).Count;

            var catBtn = new Button(() =>
            {
                currentNoteCategoryIndex = catIdx;
                BuildNotesApp(win);
            });
            catBtn.style.height = 40;
            catBtn.style.flexDirection = FlexDirection.Row;
            catBtn.style.alignItems = Align.Center;
            catBtn.style.justifyContent = Justify.SpaceBetween;
            catBtn.style.paddingLeft = 8; catBtn.style.paddingRight = 8;
            catBtn.style.marginBottom = 4;
            catBtn.style.backgroundColor = isSelected ? new Color(0.12f, 0.12f, 0.14f, 1f) : Color.clear;
            catBtn.style.borderTopLeftRadius = RadiusCard; catBtn.style.borderTopRightRadius = RadiusCard;
            catBtn.style.borderBottomLeftRadius = RadiusCard; catBtn.style.borderBottomRightRadius = RadiusCard;
            catBtn.style.borderTopWidth = 0; catBtn.style.borderBottomWidth = 0;
            catBtn.style.borderLeftWidth = 0; catBtn.style.borderRightWidth = 0;

            var leftItem = new VisualElement();
            leftItem.style.flexDirection = FlexDirection.Row;
            leftItem.style.alignItems = Align.Center;
            leftItem.Add(BuildSquircleBadge(catIcons[i], catColors[i], 24f));

            var nameLbl = new Label(cats[i]);
            nameLbl.style.fontSize = 13;
            nameLbl.style.color = isSelected ? Color.white : InkPrimary;
            if (isSelected) nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLbl.style.unityFontDefinition = Fd();
            nameLbl.style.marginLeft = 8;
            leftItem.Add(nameLbl);
            catBtn.Add(leftItem);

            var cntTag = BuildTagPill(count.ToString(), isSelected ? new Color(1f, 1f, 1f, 0.2f) : SurfaceHover, isSelected ? Color.white : InkTertiary, 10f);
            catBtn.Add(cntTag);

            catList.Add(catBtn);
        }

        // 分割线与【笔记本分类】专区标题
        var divider = new VisualElement();
        divider.style.height = 1; divider.style.backgroundColor = BorderDivider;
        divider.style.marginTop = 10; divider.style.marginBottom = 12;
        catList.Add(divider);

        var folderHeader = new Label("笔记本分类");
        folderHeader.style.fontSize = 12; folderHeader.style.color = InkTertiary;
        folderHeader.style.unityFontDefinition = Fd();
        folderHeader.style.paddingLeft = 4;
        folderHeader.style.marginBottom = 8;
        catList.Add(folderHeader);

        // 下部文件夹分类区 (索引 2..6)
        for (int i = 2; i < cats.Length; i++)
        {
            int catIdx = i;
            bool isSelected = (catIdx == currentNoteCategoryIndex);
            int count = userTabletNotes.FindAll(n => n.category == cats[catIdx]).Count;

            var catBtn = new Button(() =>
            {
                currentNoteCategoryIndex = catIdx;
                BuildNotesApp(win);
            });
            catBtn.style.height = 38;
            catBtn.style.flexDirection = FlexDirection.Row;
            catBtn.style.alignItems = Align.Center;
            catBtn.style.justifyContent = Justify.SpaceBetween;
            catBtn.style.paddingLeft = 8; catBtn.style.paddingRight = 8;
            catBtn.style.marginBottom = 3;
            catBtn.style.backgroundColor = isSelected ? new Color(0.12f, 0.12f, 0.14f, 1f) : Color.clear;
            catBtn.style.borderTopLeftRadius = RadiusCard; catBtn.style.borderTopRightRadius = RadiusCard;
            catBtn.style.borderBottomLeftRadius = RadiusCard; catBtn.style.borderBottomRightRadius = RadiusCard;
            catBtn.style.borderTopWidth = 0; catBtn.style.borderBottomWidth = 0;
            catBtn.style.borderLeftWidth = 0; catBtn.style.borderRightWidth = 0;

            var leftItem = new VisualElement();
            leftItem.style.flexDirection = FlexDirection.Row;
            leftItem.style.alignItems = Align.Center;
            leftItem.Add(BuildSquircleBadge(catIcons[i], catColors[i], 24f));

            var nameLbl = new Label(cats[i]);
            nameLbl.style.fontSize = 13;
            nameLbl.style.color = isSelected ? Color.white : InkPrimary;
            if (isSelected) nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLbl.style.unityFontDefinition = Fd();
            nameLbl.style.marginLeft = 8;
            leftItem.Add(nameLbl);
            catBtn.Add(leftItem);

            var cntTag = BuildTagPill(count.ToString(), isSelected ? new Color(1f, 1f, 1f, 0.2f) : SurfaceHover, isSelected ? Color.white : InkTertiary, 10f);
            catBtn.Add(cntTag);

            catList.Add(catBtn);
        }

        // ===== 中栏：笔记卡列表（290px 纯白卡片流 + 高对比度）=====
        var listColBox = new VisualElement();
        listColBox.style.width = 290;
        listColBox.style.backgroundColor = SurfaceApp;
        listColBox.style.paddingLeft = 14; listColBox.style.paddingRight = 14;
        listColBox.style.paddingTop = 16; listColBox.style.paddingBottom = 16;
        listColBox.style.borderRightWidth = 1;
        listColBox.style.borderRightColor = BorderDivider;
        listColBox.style.flexDirection = FlexDirection.Column;
        noteCols.Add(listColBox);

        // 过滤当前分类的笔记
        string selectedCat = cats[currentNoteCategoryIndex];
        var filteredNotes = userTabletNotes.FindAll(n => currentNoteCategoryIndex == 0 || n.category == selectedCat);

        var listHeaderRow = new VisualElement();
        listHeaderRow.style.flexDirection = FlexDirection.Row;
        listHeaderRow.style.justifyContent = Justify.SpaceBetween;
        listHeaderRow.style.alignItems = Align.Center;
        listHeaderRow.style.marginBottom = 12;
        listColBox.Add(listHeaderRow);

        var listTitleLbl = new Label(selectedCat);
        listTitleLbl.style.fontSize = 17; listTitleLbl.style.color = InkPrimary;
        listTitleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        listTitleLbl.style.unityFontDefinition = Fd();
        listHeaderRow.Add(listTitleLbl);

        var countLbl = new Label($"共 {filteredNotes.Count} 篇");
        countLbl.style.fontSize = 12; countLbl.style.color = InkTertiary;
        countLbl.style.unityFontDefinition = Fd();
        listHeaderRow.Add(countLbl);

        var listCol = new ScrollView();
        listCol.style.flexGrow = 1;
        listCol.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        listCol.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        listColBox.Add(listCol);

        // 若当前未选或选中笔记不在列表中，默认选中第一篇
        if (filteredNotes.Count > 0 && !filteredNotes.Exists(n => n.id == currentSelectedNoteId))
        {
            currentSelectedNoteId = filteredNotes[0].id;
        }

        foreach (var n in filteredNotes)
        {
            bool isSelected = (n.id == currentSelectedNoteId);
            var nCard = new VisualElement();
            nCard.style.backgroundColor = isSelected ? new Color(1f, 0.98f, 0.94f, 1f) : SurfaceCard;
            nCard.style.borderTopLeftRadius = RadiusCard; nCard.style.borderTopRightRadius = RadiusCard;
            nCard.style.borderBottomLeftRadius = RadiusCard; nCard.style.borderBottomRightRadius = RadiusCard;
            nCard.style.borderTopWidth = 1; nCard.style.borderBottomWidth = 1;
            nCard.style.borderLeftWidth = isSelected ? 3 : 1; nCard.style.borderRightWidth = 1;
            nCard.style.borderTopColor = isSelected ? AccentSystem : BorderDivider;
            nCard.style.borderBottomColor = isSelected ? AccentSystem : BorderDivider;
            nCard.style.borderLeftColor = isSelected ? AccentSystem : BorderDivider;
            nCard.style.borderRightColor = isSelected ? AccentSystem : BorderDivider;
            nCard.style.paddingLeft = 14; nCard.style.paddingRight = 14;
            nCard.style.paddingTop = 12; nCard.style.paddingBottom = 12;
            nCard.style.marginBottom = 10;
            nCard.pickingMode = PickingMode.Position;
            listCol.Add(nCard);

            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.alignItems = Align.Center;
            topRow.style.justifyContent = Justify.SpaceBetween;
            nCard.Add(topRow);

            var nt = new Label(n.title);
            nt.style.fontSize = 14; nt.style.color = InkPrimary;
            nt.style.unityFontStyleAndWeight = FontStyle.Bold;
            nt.style.unityFontDefinition = Fd();
            nt.style.flexGrow = 1;
            topRow.Add(nt);

            var catTag = BuildTagPill(n.category, new Color(0.72f, 0.52f, 0.23f, 0.12f), AccentSystem, 10f);
            catTag.style.marginLeft = 6;
            topRow.Add(catTag);

            var nd = new Label(n.date + " · " + n.author);
            nd.style.fontSize = 11; nd.style.color = InkTertiary; nd.style.unityFontDefinition = Fd();
            nd.style.marginTop = 3;
            nCard.Add(nd);

            var snippet = n.content.Length > 36 ? n.content.Substring(0, 36) + "..." : n.content;
            var ns = new Label(snippet);
            ns.style.fontSize = 12; ns.style.color = InkSecondary; ns.style.unityFontDefinition = Fd();
            ns.style.whiteSpace = WhiteSpace.Normal;
            ns.style.marginTop = 5;
            nCard.Add(ns);

            int noteId = n.id;
            nCard.RegisterCallback<ClickEvent>(_ =>
            {
                currentSelectedNoteId = noteId;
                BuildNotesApp(win);
            });
        }

        // 中栏底端：新建笔记按钮（现代胶囊药丸风格）
        var newNoteBtn = new Button(() =>
        {
            string catToAssign = currentNoteCategoryIndex == 0 ? "运营备忘" : (currentNoteCategoryIndex == 1 ? "剧情书签" : cats[currentNoteCategoryIndex]);
            var newNote = new TabletNoteItem
            {
                id = (int)(System.DateTime.UtcNow.Ticks % 1000000),
                category = catToAssign,
                title = "新备忘录 " + RailwayRenaissance.UI.Tablet.TabletDateTime.Now.ToString("MM-dd HH:mm"),
                date = RailwayRenaissance.UI.Tablet.TabletDateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                author = "林彪悍",
                content = "在此输入新的笔记与运营日志内容...",
                isSystemGenerated = false
            };
            userTabletNotes.Insert(0, newNote);
            currentSelectedNoteId = newNote.id;
            SaveUserTabletNotes();
            BuildNotesApp(win);
            ShowToast("已新建一条备忘录。");
        }) { text = "＋ 新建笔记" };
        newNoteBtn.style.height = 38;
        newNoteBtn.style.fontSize = 13; newNoteBtn.style.unityFontDefinition = Fd();
        newNoteBtn.style.backgroundColor = AccentSystem;
        newNoteBtn.style.color = Color.white;
        newNoteBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        newNoteBtn.style.borderTopLeftRadius = RadiusPill; newNoteBtn.style.borderTopRightRadius = RadiusPill;
        newNoteBtn.style.borderBottomLeftRadius = RadiusPill; newNoteBtn.style.borderBottomRightRadius = RadiusPill;
        newNoteBtn.style.borderTopWidth = 0; newNoteBtn.style.borderBottomWidth = 0;
        newNoteBtn.style.borderLeftWidth = 0; newNoteBtn.style.borderRightWidth = 0;
        newNoteBtn.style.marginTop = 10;
        listColBox.Add(newNoteBtn);

        // ===== 右栏：纯白编辑器卡片（大标题 + 元信息 + 正文 + 保存）=====
        var detailColWrapper = new VisualElement();
        detailColWrapper.style.flexGrow = 1;
        detailColWrapper.style.paddingLeft = 20; detailColWrapper.style.paddingRight = 20;
        detailColWrapper.style.paddingTop = 16; detailColWrapper.style.paddingBottom = 16;
        noteCols.Add(detailColWrapper);

        var detailCol = new VisualElement();
        detailCol.style.flexGrow = 1;
        detailCol.style.backgroundColor = SurfaceCard;
        detailCol.style.borderTopLeftRadius = 14; detailCol.style.borderTopRightRadius = 14;
        detailCol.style.borderBottomLeftRadius = 14; detailCol.style.borderBottomRightRadius = 14;
        detailCol.style.borderTopWidth = 1; detailCol.style.borderBottomWidth = 1;
        detailCol.style.borderLeftWidth = 1; detailCol.style.borderRightWidth = 1;
        detailCol.style.borderTopColor = BorderDivider; detailCol.style.borderBottomColor = BorderDivider;
        detailCol.style.borderLeftColor = BorderDivider; detailCol.style.borderRightColor = BorderDivider;
        detailCol.style.paddingLeft = 24; detailCol.style.paddingRight = 24;
        detailCol.style.paddingTop = 20; detailCol.style.paddingBottom = 20;
        detailColWrapper.Add(detailCol);

        var curNote = userTabletNotes.Find(n => n.id == currentSelectedNoteId);
        if (curNote == null && userTabletNotes.Count > 0) curNote = userTabletNotes[0];

        if (curNote != null)
        {
            // 顶栏操作行：大标题 TextField + 保存/删除按钮
            var topActionRow = new VisualElement();
            topActionRow.style.flexDirection = FlexDirection.Row;
            topActionRow.style.alignItems = Align.Center;
            topActionRow.style.justifyContent = Justify.SpaceBetween;
            topActionRow.style.marginBottom = 8;
            detailCol.Add(topActionRow);

            noteTitleField = new TextField { value = curNote.title };
            noteTitleField.style.flexGrow = 1;
            noteTitleField.style.height = 36;
            noteTitleField.style.fontSize = 20;
            noteTitleField.style.unityFontStyleAndWeight = FontStyle.Bold;
            noteTitleField.style.unityFontDefinition = Fd();
            noteTitleField.style.color = InkPrimary;
            noteTitleField.style.backgroundColor = Color.clear;
            noteTitleField.style.borderTopWidth = 0; noteTitleField.style.borderBottomWidth = 0;
            noteTitleField.style.borderLeftWidth = 0; noteTitleField.style.borderRightWidth = 0;
            noteTitleField.style.marginRight = 12;
            topActionRow.Add(noteTitleField);

            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            topActionRow.Add(btnRow);

            var saveBtn = new Button(() =>
            {
                curNote.title = noteTitleField.value;
                curNote.content = noteBodyField.value;
                curNote.date = RailwayRenaissance.UI.Tablet.TabletDateTime.Now.ToString("yyyy-MM-dd HH:mm");
                SaveUserTabletNotes();
                ShowToast("笔记已保存到平板闪存。");
                BuildNotesApp(win);
            }) { text = "保存笔记" };
            saveBtn.style.height = 34;
            saveBtn.style.paddingLeft = 14; saveBtn.style.paddingRight = 14;
            saveBtn.style.fontSize = 12; saveBtn.style.unityFontDefinition = Fd();
            saveBtn.style.backgroundColor = AccentSystem;
            saveBtn.style.color = Color.white;
            saveBtn.style.borderTopLeftRadius = 8; saveBtn.style.borderTopRightRadius = 8;
            saveBtn.style.borderBottomLeftRadius = 8; saveBtn.style.borderBottomRightRadius = 8;
            saveBtn.style.borderTopWidth = 0; saveBtn.style.borderBottomWidth = 0;
            saveBtn.style.borderLeftWidth = 0; saveBtn.style.borderRightWidth = 0;
            btnRow.Add(saveBtn);

            var pinIslandBtn = new Button(() =>
            {
                RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.Instance?.RegisterSession(new RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.IslandSession
                {
                    AppType = RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.IslandAppType.DispatchAlert,
                    Priority = 95,
                    Title = curNote.title,
                    Subtitle = "便签备忘置顶 · " + curNote.category,
                    Metric = "置顶备忘",
                    AccentColor = AccentSystem
                });
                ShowToast("笔记已置顶挂载到小米超级岛！");
            }) { text = "挂载超级岛" };
            pinIslandBtn.style.height = 34;
            pinIslandBtn.style.paddingLeft = 12; pinIslandBtn.style.paddingRight = 12;
            pinIslandBtn.style.fontSize = 12; pinIslandBtn.style.unityFontDefinition = Fd();
            pinIslandBtn.style.backgroundColor = SurfaceHover;
            pinIslandBtn.style.color = InkPrimary;
            pinIslandBtn.style.borderTopLeftRadius = RadiusPill; pinIslandBtn.style.borderTopRightRadius = RadiusPill;
            pinIslandBtn.style.borderBottomLeftRadius = RadiusPill; pinIslandBtn.style.borderBottomRightRadius = RadiusPill;
            pinIslandBtn.style.borderTopWidth = 0; pinIslandBtn.style.borderBottomWidth = 0;
            pinIslandBtn.style.borderLeftWidth = 0; pinIslandBtn.style.borderRightWidth = 0;
            pinIslandBtn.style.marginLeft = 8;
            btnRow.Add(pinIslandBtn);

            // 删除按钮（仅允许删除用户自建笔记）
            if (!curNote.isSystemGenerated)
            {
                var delBtn = new Button(() =>
                {
                    userTabletNotes.Remove(curNote);
                    SaveUserTabletNotes();
                    currentSelectedNoteId = userTabletNotes.Count > 0 ? userTabletNotes[0].id : 0;
                    ShowToast("笔记已删除。");
                    BuildNotesApp(win);
                }) { text = "删除" };
                delBtn.style.height = 34;
                delBtn.style.paddingLeft = 10; delBtn.style.paddingRight = 10;
                delBtn.style.fontSize = 12; delBtn.style.unityFontDefinition = Fd();
                delBtn.style.backgroundColor = new Color(0.95f, 0.95f, 0.96f, 1f);
                delBtn.style.color = InkSecondary;
                delBtn.style.borderTopLeftRadius = 8; delBtn.style.borderTopRightRadius = 8;
                delBtn.style.borderBottomLeftRadius = 8; delBtn.style.borderBottomRightRadius = 8;
                delBtn.style.borderTopWidth = 0; delBtn.style.borderBottomWidth = 0;
                delBtn.style.borderLeftWidth = 0; delBtn.style.borderRightWidth = 0;
                delBtn.style.marginLeft = 8;
                btnRow.Add(delBtn);
            }

            noteMetaLabel = new Label($"记录时间：{curNote.date} · 记录人：{curNote.author} · 分类：{curNote.category}");
            noteMetaLabel.style.fontSize = 12; noteMetaLabel.style.color = InkTertiary; noteMetaLabel.style.unityFontDefinition = Fd();
            noteMetaLabel.style.marginBottom = 12;
            detailCol.Add(noteMetaLabel);

            var noteDivider = new VisualElement();
            noteDivider.style.height = 1; noteDivider.style.backgroundColor = BorderDivider;
            noteDivider.style.marginBottom = 14;
            detailCol.Add(noteDivider);

            noteBodyField = new TextField { value = curNote.content, multiline = true };
            noteBodyField.style.flexGrow = 1;
            noteBodyField.style.fontSize = 14;
            noteBodyField.style.unityFontDefinition = Fd();
            noteBodyField.style.color = InkPrimary;
            noteBodyField.style.backgroundColor = Color.clear;
            noteBodyField.style.borderTopWidth = 0; noteBodyField.style.borderBottomWidth = 0;
            noteBodyField.style.borderLeftWidth = 0; noteBodyField.style.borderRightWidth = 0;
            detailCol.Add(noteBodyField);
        }
        else
        {
            var emptyLbl = new Label("暂无笔记内容。点击左侧「＋ 新建笔记」开始记录。");
            emptyLbl.style.fontSize = 15; emptyLbl.style.color = InkTertiary; emptyLbl.style.unityFontDefinition = Fd();
            detailCol.Add(emptyLbl);
        }
    }


    }
}
