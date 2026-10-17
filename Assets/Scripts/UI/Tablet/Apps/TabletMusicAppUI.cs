using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 平板应用：音乐（13 首全量曲目 + 毛玻璃悬浮播放器）
    /// 从 GameMainUI 剥离（T306 Batch 1-B）
    /// </summary>
    public static class TabletMusicAppUI
    {
    // ============ App: 音乐（13 首全量曲目 + 毛玻璃悬浮播放器 · 完整迁移）============
    public class TabletMusicEntry
    {
        public string id;
        public string title;
        public string category; // "BGM" 或 "歌曲"
        public string clipName;
        public string condition;
    }

    public static readonly TabletMusicEntry[] TabletMusicList = {
        new TabletMusicEntry { id = "iron_and_ash", title = "Iron & Ash 铁与灰", category = "BGM", clipName = "iron_and_ash", condition = "标题画面自动解锁" },
        new TabletMusicEntry { id = "cloud_rail", title = "Cloud-Rail 云轨", category = "BGM", clipName = "cloud_rail", condition = "启程飞行途中解锁" },
        new TabletMusicEntry { id = "embers", title = "Embers 余烬", category = "BGM", clipName = "embers", condition = "抵达雾峰站时解锁" },
        new TabletMusicEntry { id = "night_cargo", title = "Night Cargo 夜行货", category = "BGM", clipName = "night_cargo", condition = "边境夜航时解锁" },
        new TabletMusicEntry { id = "first_light", title = "First Light 晨光", category = "BGM", clipName = "first_light", condition = "飞越华北平原时解锁" },
        new TabletMusicEntry { id = "platform", title = "Platform 站台", category = "BGM", clipName = "platform", condition = "到站后解锁" },
        new TabletMusicEntry { id = "borderline", title = "Borderline 国境线", category = "BGM", clipName = "borderline", condition = "边境危机时解锁" },
        new TabletMusicEntry { id = "wheels_joke", title = "The Wheel's Joke 方向盘在笑", category = "BGM", clipName = "wheels_joke", condition = "统一便当店购物" },
        new TabletMusicEntry { id = "train_through_keys", title = "Train Through Keys 旧曲", category = "BGM", clipName = "train_through_keys", condition = "已废弃，保留试听" },
        new TabletMusicEntry { id = "south_wind", title = "남풍 南风", category = "歌曲", clipName = "south_wind", condition = "统一便当店购物后解锁" },
        new TabletMusicEntry { id = "starlit_rails", title = "별빛 철길 星光铁轨", category = "歌曲", clipName = "starlit_rails", condition = "边境危机时解锁夜航" },
        new TabletMusicEntry { id = "chollima_ride", title = "천리마 신시대에 달리다 千里马驰骋新时代", category = "歌曲", clipName = "chollima_ride", condition = "边境途中播放新闻时解锁" },
        new TabletMusicEntry { id = "sleepers", title = "Sleepers 铁轨沉睡者", category = "歌曲", clipName = "sleepers", condition = "片尾曲（待生成）" }
    };

    private static string tabletMusicFilter = "全部"; // "全部", "BGM", "歌曲"
    private static int tabletMusicNavTab = 0; // 0: 推荐/乐库, 1: 我的/收藏
    public static AudioSource tabletMusicSource;
    public static AudioClip tabletCurrentClip;
    public static string tabletCurrentTrackId;
    public static string tabletCurrentTrackTitle;
    public static string tabletCurrentTrackCategory;
    public static bool tabletIsPlaying;
    public enum TabletPlayMode { SingleRepeat, Sequential, Shuffle }
    public static TabletPlayMode tabletPlayMode = TabletPlayMode.Sequential;
    private static VisualElement tabletProgressTrack;
    private static VisualElement tabletProgressFill;
    private static VisualElement tabletProgressKnob;
    private static Label tabletCurTimeLbl;
    private static Label tabletTotalTimeLbl;
    private static Label tabletPlayerTitleLbl;
    private static Label tabletPlayerSubLbl;
    private static Button tabletPlayToggleBtn;
    private static Button tabletModeToggleBtn;
    private static Button tabletFavToggleBtn;
    private static bool tabletProgressScrubbing;
    private static float tabletMusicProgressTimer;

    public static bool IsTabletMusicUnlocked(TabletMusicEntry m)
    {
        if (m.id == "iron_and_ash" || m.id == "cloud_rail" || m.id == "embers" || m.id == "platform") return true;
        return PlayerPrefs.GetInt("ArchiveMusic_" + m.id, 0) == 1 || PlayerPrefs.GetInt("VN_Got0721", 0) == 1;
    }

    public static bool IsTabletMusicFavorited(string trackId)
    {
        return PlayerPrefs.GetInt("MusicFav_" + trackId, 0) == 1;
    }

    public static void ToggleTabletMusicFavorite(string trackId)
    {
        bool cur = IsTabletMusicFavorited(trackId);
        PlayerPrefs.SetInt("MusicFav_" + trackId, cur ? 0 : 1);
        PlayerPrefs.Save();
    }

    public static int GetTabletFavoriteCount()
    {
        int count = 0;
        foreach (var m in TabletMusicList)
        {
            if (IsTabletMusicFavorited(m.id)) count++;
        }
        return count;
    }

    public static void PlayTabletTrack(TabletMusicEntry m)
    {
        var clip = Resources.Load<AudioClip>("bgm/" + m.clipName);
        if (clip == null)
        {
            ShowToast("音频资源缺失：bgm/" + m.clipName);
            return;
        }

        if (tabletMusicSource == null)
        {
            var go = new GameObject("TabletMusicPlayer");
            UnityEngine.Object.DontDestroyOnLoad(go);
            tabletMusicSource = go.AddComponent<AudioSource>();
        }

        tabletMusicSource.clip = clip;
        tabletMusicSource.volume = 0.65f;
        tabletMusicSource.loop = (tabletPlayMode == TabletPlayMode.SingleRepeat);
        tabletMusicSource.Play();

        tabletCurrentClip = clip;
        tabletCurrentTrackId = m.id;
        tabletCurrentTrackTitle = m.title;
        tabletCurrentTrackCategory = m.category;
        tabletIsPlaying = true;

        if (tabletPlayerTitleLbl != null) tabletPlayerTitleLbl.text = m.title;
        if (tabletPlayerSubLbl != null) tabletPlayerSubLbl.text = m.category + " · 铁道原声品质";
        if (tabletTotalTimeLbl != null) tabletTotalTimeLbl.text = FormatAudioTime(clip.length);
        if (tabletPlayToggleBtn != null) tabletPlayToggleBtn.text = "❚❚";
        if (tabletFavToggleBtn != null)
        {
            bool isFav = IsTabletMusicFavorited(m.id);
            tabletFavToggleBtn.text = isFav ? "♥" : "♡";
            tabletFavToggleBtn.style.color = isFav ? new Color(0.92f, 0.25f, 0.30f, 1f) : new Color(0.6f, 0.6f, 0.65f, 1f);
        }
    }

    public static void ToggleTabletMusicPlayPause()
    {
        if (tabletMusicSource == null || tabletCurrentClip == null)
        {
            // 若当前未播，播放第一首已解锁曲目
            foreach (var m in TabletMusicList)
            {
                if (IsTabletMusicUnlocked(m))
                {
                    PlayTabletTrack(m);
                    return;
                }
            }
            return;
        }

        if (tabletMusicSource.isPlaying)
        {
            tabletMusicSource.Pause();
            tabletIsPlaying = false;
            if (tabletPlayToggleBtn != null) tabletPlayToggleBtn.text = "▶";
        }
        else
        {
            tabletMusicSource.UnPause();
            tabletIsPlaying = true;
            if (tabletPlayToggleBtn != null) tabletPlayToggleBtn.text = "❚❚";
        }
    }

    public static void PlayNextTabletTrack()
    {
        if (tabletPlayMode == TabletPlayMode.Shuffle)
        {
            var unlocked = new System.Collections.Generic.List<TabletMusicEntry>();
            foreach (var m in TabletMusicList) if (IsTabletMusicUnlocked(m)) unlocked.Add(m);
            if (unlocked.Count > 0)
            {
                int r = UnityEngine.Random.Range(0, unlocked.Count);
                PlayTabletTrack(unlocked[r]);
            }
            return;
        }

        int curIdx = -1;
        for (int i = 0; i < TabletMusicList.Length; i++)
        {
            if (TabletMusicList[i].id == tabletCurrentTrackId) { curIdx = i; break; }
        }

        for (int i = curIdx + 1; i < TabletMusicList.Length; i++)
        {
            if (IsTabletMusicUnlocked(TabletMusicList[i]))
            {
                PlayTabletTrack(TabletMusicList[i]);
                return;
            }
        }
        for (int i = 0; i < curIdx; i++)
        {
            if (IsTabletMusicUnlocked(TabletMusicList[i]))
            {
                PlayTabletTrack(TabletMusicList[i]);
                return;
            }
        }
    }

    public static void PlayPrevTabletTrack()
    {
        int curIdx = -1;
        for (int i = 0; i < TabletMusicList.Length; i++)
        {
            if (TabletMusicList[i].id == tabletCurrentTrackId) { curIdx = i; break; }
        }

        for (int i = curIdx - 1; i >= 0; i--)
        {
            if (IsTabletMusicUnlocked(TabletMusicList[i]))
            {
                PlayTabletTrack(TabletMusicList[i]);
                return;
            }
        }
        for (int i = TabletMusicList.Length - 1; i > curIdx; i--)
        {
            if (IsTabletMusicUnlocked(TabletMusicList[i]))
            {
                PlayTabletTrack(TabletMusicList[i]);
                return;
            }
        }
    }

    public static string FormatAudioTime(float sec)
    {
        if (float.IsNaN(sec) || sec < 0) sec = 0;
        int m = (int)(sec / 60f);
        int s = (int)(sec % 60f);
        return $"{m}:{s:D2}";
    }

        private static Action<string> s_showToast;

    private static void ShowToast(string msg)
    {
        if (s_showToast != null) s_showToast(msg);
        else Debug.Log("[Music] " + msg);
    }

    public static void UpdateAudioProgress()
    {
        if (tabletIsPlaying && tabletMusicSource != null && tabletCurrentClip != null)
        {
            tabletMusicProgressTimer += Time.unscaledDeltaTime;
            if (tabletMusicProgressTimer >= 0.1f)
            {
                tabletMusicProgressTimer = 0f;
                float curTime = tabletMusicSource.time;
                float totalLen = tabletCurrentClip.length;
                if (!tabletProgressScrubbing && totalLen > 0.1f)
                {
                    float pct = Mathf.Clamp01(curTime / totalLen) * 100f;
                    if (tabletProgressFill != null) tabletProgressFill.style.width = Length.Percent(pct);
                    if (tabletProgressKnob != null) tabletProgressKnob.style.left = Length.Percent(pct);
                }
                if (tabletCurTimeLbl != null) tabletCurTimeLbl.text = FormatAudioTime(curTime);

                // 播放结束自动切下一首
                if (curTime >= totalLen - 0.2f && totalLen > 0.1f)
                {
                    if (tabletPlayMode == TabletPlayMode.SingleRepeat)
                    {
                        tabletMusicSource.time = 0;
                        tabletMusicSource.Play();
                    }
                    else
                    {
                        PlayNextTabletTrack();
                    }
                }
            }
        }
    }

    public static void BuildMusicApp(VisualElement win, Action<string> showToast = null)
    {
        if (showToast != null) s_showToast = showToast;
        win.Clear();

        // ===== 整体结构：外层容器（晨雾玉浅灰背景 + 双栏 Navigation Rail + 悬浮播放条）=====
        var root = new VisualElement();
        root.style.flexDirection = FlexDirection.Row;
        root.style.flexGrow = 1;
        root.style.backgroundColor = new Color(0.96f, 0.97f, 0.98f, 1f); // 柔浅玉灰底，杜绝纯白生硬
        win.Add(root);

        // ===== 左侧导航 Rail（固定 86px 宽，参考 Pad 端音乐）=====
        var navRail = new VisualElement();
        navRail.style.width = 86;
        navRail.style.backgroundColor = Color.white;
        navRail.style.borderRightWidth = 1;
        navRail.style.borderRightColor = new Color(0.90f, 0.91f, 0.93f, 1f);
        navRail.style.alignItems = Align.Center;
        navRail.style.paddingTop = 20;
        navRail.style.paddingBottom = 20;
        root.Add(navRail);

        // 顶部品牌黑胶 Logo
        var brandIconBox = new VisualElement();
        brandIconBox.style.width = 44; brandIconBox.style.height = 44;
        brandIconBox.style.borderTopLeftRadius = 22; brandIconBox.style.borderTopRightRadius = 22;
        brandIconBox.style.borderBottomLeftRadius = 22; brandIconBox.style.borderBottomRightRadius = 22;
        brandIconBox.style.backgroundColor = new Color(0.18f, 0.72f, 0.42f, 1f); // 翡翠绿
        brandIconBox.style.alignItems = Align.Center;
        brandIconBox.style.justifyContent = Justify.Center;
        brandIconBox.style.marginBottom = 6;
        navRail.Add(brandIconBox);

        var brandIconLbl = new Label("♫");
        brandIconLbl.style.fontSize = 20; brandIconLbl.style.color = Color.white;
        brandIconLbl.style.unityFontDefinition = Fd();
        brandIconBox.Add(brandIconLbl);

        var brandTitle = new Label("随身听");
        brandTitle.style.fontSize = 11;
        brandTitle.style.color = new Color(0.35f, 0.35f, 0.40f, 1f);
        brandTitle.style.unityFontDefinition = Fd();
        brandTitle.style.marginBottom = 24;
        navRail.Add(brandTitle);

        // 导航项目构建辅助方法
        System.Action<int, string, string> addNavTab = (tabIdx, iconSymbol, labelText) =>
        {
            bool isSelected = (tabletMusicNavTab == tabIdx);
            var tabBtn = new VisualElement();
            tabBtn.style.width = 70; tabBtn.style.height = 54;
            tabBtn.style.alignItems = Align.Center;
            tabBtn.style.justifyContent = Justify.Center;
            tabBtn.style.marginBottom = 12;
            tabBtn.style.borderTopLeftRadius = 12; tabBtn.style.borderTopRightRadius = 12;
            tabBtn.style.borderBottomLeftRadius = 12; tabBtn.style.borderBottomRightRadius = 12;
            tabBtn.style.backgroundColor = isSelected ? new Color(0.92f, 0.97f, 0.94f, 1f) : Color.clear;
            tabBtn.pickingMode = PickingMode.Position;

            var tabIcon = new Label(iconSymbol);
            tabIcon.style.fontSize = 18;
            tabIcon.style.color = isSelected ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.55f, 0.55f, 0.60f, 1f);
            tabIcon.style.unityFontDefinition = Fd();
            tabBtn.Add(tabIcon);

            var tabTxt = new Label(labelText);
            tabTxt.style.fontSize = 11;
            tabTxt.style.unityFontStyleAndWeight = isSelected ? FontStyle.Bold : FontStyle.Normal;
            tabTxt.style.color = isSelected ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.55f, 0.55f, 0.60f, 1f);
            tabTxt.style.unityFontDefinition = Fd();
            tabTxt.style.marginTop = 2;
            tabBtn.Add(tabTxt);

            tabBtn.RegisterCallback<ClickEvent>(_ =>
            {
                tabletMusicNavTab = tabIdx;
                BuildMusicApp(win);
            });
            navRail.Add(tabBtn);
        };

        addNavTab(0, "☵", "推荐");
        addNavTab(1, "♥", "我的");

        // ===== 右侧主内容区域（flexGrow=1）=====
        var mainArea = new VisualElement();
        mainArea.style.flexGrow = 1;
        mainArea.style.flexDirection = FlexDirection.Column;
        mainArea.style.position = Position.Relative;
        root.Add(mainArea);

        // 主体滚动内容区（留出底部 78px 避免被悬浮播放栏遮挡）
        var contentScroll = new ScrollView();
        contentScroll.style.flexGrow = 1;
        contentScroll.style.paddingLeft = 24; contentScroll.style.paddingRight = 24;
        contentScroll.style.paddingTop = 18; contentScroll.style.paddingBottom = 82;
        mainArea.Add(contentScroll);

        if (tabletMusicNavTab == 0)
        {
            // ==================== TAB 0: 推荐 / 乐库视图（参考图二）====================
            // 1. 金刚区（4 大方圆快捷功能卡片）
            var quickGrid = new VisualElement();
            quickGrid.style.flexDirection = FlexDirection.Row;
            quickGrid.style.justifyContent = Justify.SpaceBetween;
            quickGrid.style.marginBottom = 18;
            contentScroll.Add(quickGrid);

            System.Action<string, string, Color, System.Action> addQuickCard = (name, iconText, iconBg, onClick) =>
            {
                var qCard = new VisualElement();
                qCard.style.width = 175; qCard.style.height = 70;
                qCard.style.flexDirection = FlexDirection.Row;
                qCard.style.alignItems = Align.Center;
                qCard.style.backgroundColor = Color.white;
                qCard.style.borderTopLeftRadius = 14; qCard.style.borderTopRightRadius = 14;
                qCard.style.borderBottomLeftRadius = 14; qCard.style.borderBottomRightRadius = 14;
                qCard.style.borderTopWidth = 1; qCard.style.borderBottomWidth = 1;
                qCard.style.borderLeftWidth = 1; qCard.style.borderRightWidth = 1;
                qCard.style.borderTopColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                qCard.style.borderBottomColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                qCard.style.borderLeftColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                qCard.style.borderRightColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                qCard.style.paddingLeft = 12; qCard.style.paddingRight = 12;
                qCard.pickingMode = PickingMode.Position;

                var qIcon = new VisualElement();
                qIcon.style.width = 40; qIcon.style.height = 40;
                qIcon.style.borderTopLeftRadius = 20; qIcon.style.borderTopRightRadius = 20;
                qIcon.style.borderBottomLeftRadius = 20; qIcon.style.borderBottomRightRadius = 20;
                qIcon.style.backgroundColor = iconBg;
                qIcon.style.alignItems = Align.Center; qIcon.style.justifyContent = Justify.Center;
                qIcon.style.marginRight = 10;
                qCard.Add(qIcon);

                var qIconTxt = new Label(iconText);
                qIconTxt.style.fontSize = 16; qIconTxt.style.color = Color.white; qIconTxt.style.unityFontDefinition = Fd();
                qIcon.Add(qIconTxt);

                var qInfo = new VisualElement();
                qCard.Add(qInfo);

                var qName = new Label(name);
                qName.style.fontSize = 13; qName.style.unityFontStyleAndWeight = FontStyle.Bold;
                qName.style.color = new Color(0.12f, 0.12f, 0.14f, 1f);
                qName.style.unityFontDefinition = Fd();
                qInfo.Add(qName);

                var qSub = new Label("铁道甄选");
                qSub.style.fontSize = 10; qSub.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
                qSub.style.unityFontDefinition = Fd();
                qInfo.Add(qSub);

                qCard.RegisterCallback<ClickEvent>(_ => onClick?.Invoke());
                quickGrid.Add(qCard);
            };

            addQuickCard("猜你想听", "♪", new Color(0.18f, 0.72f, 0.42f, 1f), () =>
            {
                // 随机播放一首已解锁曲目
                var unlocked = new System.Collections.Generic.List<TabletMusicEntry>();
                foreach (var m in TabletMusicList) if (IsTabletMusicUnlocked(m)) unlocked.Add(m);
                if (unlocked.Count > 0)
                {
                    int r = UnityEngine.Random.Range(0, unlocked.Count);
                    PlayTabletTrack(unlocked[r]);
                    BuildMusicApp(win);
                }
            });

            addQuickCard("每日精选", "★", new Color(0.25f, 0.65f, 0.90f, 1f), () =>
            {
                if (TabletMusicList.Length > 0 && IsTabletMusicUnlocked(TabletMusicList[0]))
                {
                    PlayTabletTrack(TabletMusicList[0]);
                    BuildMusicApp(win);
                }
            });

            addQuickCard("铁道原声", "❖", new Color(0.85f, 0.55f, 0.20f, 1f), () =>
            {
                tabletMusicFilter = "BGM";
                BuildMusicApp(win);
            });

            addQuickCard("先民歌谣", "🎙", new Color(0.75f, 0.32f, 0.45f, 1f), () =>
            {
                tabletMusicFilter = "歌曲";
                BuildMusicApp(win);
            });

            // 2. 「大家都在听」横条 + 高对比度分类筛选胶囊
            var sectionHeader = new VisualElement();
            sectionHeader.style.flexDirection = FlexDirection.Row;
            sectionHeader.style.justifyContent = Justify.SpaceBetween;
            sectionHeader.style.alignItems = Align.Center;
            sectionHeader.style.marginBottom = 14;
            contentScroll.Add(sectionHeader);

            var secLeft = new VisualElement();
            secLeft.style.flexDirection = FlexDirection.Row;
            secLeft.style.alignItems = Align.Center;
            sectionHeader.Add(secLeft);

            var secTitle = new Label("大家都在听 · 广播台音轨");
            secTitle.style.fontSize = 17; secTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            secTitle.style.color = new Color(0.10f, 0.10f, 0.12f, 1f);
            secTitle.style.unityFontDefinition = Fd();
            secTitle.style.marginRight = 12;
            secLeft.Add(secTitle);

            var playAllPill = new Button(() =>
            {
                // 播放全部
                foreach (var m in TabletMusicList)
                {
                    if (IsTabletMusicUnlocked(m))
                    {
                        PlayTabletTrack(m);
                        BuildMusicApp(win);
                        return;
                    }
                }
            }) { text = "▶ 播放全部" };
            playAllPill.style.height = 26; playAllPill.style.paddingLeft = 10; playAllPill.style.paddingRight = 10;
            playAllPill.style.fontSize = 11; playAllPill.style.unityFontDefinition = Fd();
            playAllPill.style.backgroundColor = new Color(0.92f, 0.97f, 0.94f, 1f);
            playAllPill.style.color = new Color(0.12f, 0.65f, 0.36f, 1f);
            playAllPill.style.borderTopLeftRadius = 13; playAllPill.style.borderTopRightRadius = 13;
            playAllPill.style.borderBottomLeftRadius = 13; playAllPill.style.borderBottomRightRadius = 13;
            playAllPill.style.borderTopWidth = 1; playAllPill.style.borderBottomWidth = 1;
            playAllPill.style.borderLeftWidth = 1; playAllPill.style.borderRightWidth = 1;
            playAllPill.style.borderTopColor = new Color(0.70f, 0.90f, 0.78f, 1f);
            playAllPill.style.borderBottomColor = new Color(0.70f, 0.90f, 0.78f, 1f);
            playAllPill.style.borderLeftColor = new Color(0.70f, 0.90f, 0.78f, 1f);
            playAllPill.style.borderRightColor = new Color(0.70f, 0.90f, 0.78f, 1f);
            secLeft.Add(playAllPill);

            // 右侧高对比度分类胶囊 Tab（完全解决白底看不清的问题）
            var filterGroup = new VisualElement();
            filterGroup.style.flexDirection = FlexDirection.Row;
            sectionHeader.Add(filterGroup);

            string[] categories = { "全部", "BGM", "歌曲" };
            foreach (var cat in categories)
            {
                string cName = cat;
                bool isSelected = (tabletMusicFilter == cName);
                var fBtn = new Button(() =>
                {
                    tabletMusicFilter = cName;
                    BuildMusicApp(win);
                }) { text = cName };
                fBtn.style.height = 28; fBtn.style.paddingLeft = 12; fBtn.style.paddingRight = 12;
                fBtn.style.fontSize = 12; fBtn.style.unityFontDefinition = Fd();
                fBtn.style.marginLeft = 6;
                fBtn.style.borderTopLeftRadius = 14; fBtn.style.borderTopRightRadius = 14;
                fBtn.style.borderBottomLeftRadius = 14; fBtn.style.borderBottomRightRadius = 14;
                fBtn.style.borderTopWidth = 1; fBtn.style.borderBottomWidth = 1;
                fBtn.style.borderLeftWidth = 1; fBtn.style.borderRightWidth = 1;
                if (isSelected)
                {
                    fBtn.style.backgroundColor = new Color(0.10f, 0.10f, 0.12f, 1f); // 纯黑底
                    fBtn.style.color = Color.white; // 纯白高对比字
                    fBtn.style.borderTopColor = new Color(0.10f, 0.10f, 0.12f, 1f);
                    fBtn.style.borderBottomColor = new Color(0.10f, 0.10f, 0.12f, 1f);
                    fBtn.style.borderLeftColor = new Color(0.10f, 0.10f, 0.12f, 1f);
                    fBtn.style.borderRightColor = new Color(0.10f, 0.10f, 0.12f, 1f);
                }
                else
                {
                    fBtn.style.backgroundColor = Color.white; // 纯白底
                    fBtn.style.color = new Color(0.25f, 0.25f, 0.28f, 1f); // 墨黑字
                    fBtn.style.borderTopColor = new Color(0.85f, 0.85f, 0.88f, 1f);
                    fBtn.style.borderBottomColor = new Color(0.85f, 0.85f, 0.88f, 1f);
                    fBtn.style.borderLeftColor = new Color(0.85f, 0.85f, 0.88f, 1f);
                    fBtn.style.borderRightColor = new Color(0.85f, 0.85f, 0.88f, 1f);
                }
                filterGroup.Add(fBtn);
            }

            // 3. 歌曲卡片网格
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.justifyContent = Justify.SpaceBetween;
            contentScroll.Add(grid);

            foreach (var m in TabletMusicList)
            {
                if (tabletMusicFilter != "全部" && m.category != tabletMusicFilter) continue;

                bool unlocked = IsTabletMusicUnlocked(m);
                bool isCurrentPlaying = (m.id == tabletCurrentTrackId && tabletIsPlaying);
                bool isFav = IsTabletMusicFavorited(m.id);

                var card = new VisualElement();
                card.style.width = 360; card.style.height = 74;
                card.style.marginBottom = 10;
                card.style.flexDirection = FlexDirection.Row;
                card.style.alignItems = Align.Center;
                card.style.justifyContent = Justify.SpaceBetween;
                card.style.paddingLeft = 10; card.style.paddingRight = 12;
                card.style.backgroundColor = Color.white;
                card.style.borderTopLeftRadius = 12; card.style.borderTopRightRadius = 12;
                card.style.borderBottomLeftRadius = 12; card.style.borderBottomRightRadius = 12;
                card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
                card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
                card.style.borderTopColor = isCurrentPlaying ? new Color(0.18f, 0.72f, 0.42f, 0.8f) : new Color(0.92f, 0.92f, 0.95f, 1f);
                card.style.borderBottomColor = isCurrentPlaying ? new Color(0.18f, 0.72f, 0.42f, 0.8f) : new Color(0.92f, 0.92f, 0.95f, 1f);
                card.style.borderLeftColor = isCurrentPlaying ? new Color(0.18f, 0.72f, 0.42f, 0.8f) : new Color(0.92f, 0.92f, 0.95f, 1f);
                card.style.borderRightColor = isCurrentPlaying ? new Color(0.18f, 0.72f, 0.42f, 0.8f) : new Color(0.92f, 0.92f, 0.95f, 1f);
                grid.Add(card);

                // 左侧：黑胶唱片封套视觉（代码绘图方块）
                var coverBox = new VisualElement();
                coverBox.style.width = 50; coverBox.style.height = 50;
                coverBox.style.borderTopLeftRadius = 8; coverBox.style.borderTopRightRadius = 8;
                coverBox.style.borderBottomLeftRadius = 8; coverBox.style.borderBottomRightRadius = 8;
                coverBox.style.backgroundColor = isCurrentPlaying ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.18f, 0.18f, 0.22f, 1f);
                coverBox.style.alignItems = Align.Center; coverBox.style.justifyContent = Justify.Center;
                coverBox.style.marginRight = 10;
                card.Add(coverBox);

                var discLabel = new Label(isCurrentPlaying ? "♪" : "◎");
                discLabel.style.fontSize = 18; discLabel.style.color = isCurrentPlaying ? Color.white : new Color(0.85f, 0.75f, 0.5f, 1f);
                discLabel.style.unityFontDefinition = Fd();
                coverBox.Add(discLabel);

                // 中间：歌名与副标
                var midInfo = new VisualElement();
                midInfo.style.flexGrow = 1;
                midInfo.style.overflow = Overflow.Hidden;
                card.Add(midInfo);

                var titleRow = new VisualElement();
                titleRow.style.flexDirection = FlexDirection.Row;
                titleRow.style.alignItems = Align.Center;
                midInfo.Add(titleRow);

                var tLbl = new Label(unlocked ? m.title : "??? 铁道保密曲目");
                tLbl.style.fontSize = 13; tLbl.style.unityFontStyleAndWeight = isCurrentPlaying ? FontStyle.Bold : FontStyle.Normal;
                tLbl.style.color = unlocked ? (isCurrentPlaying ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.12f, 0.12f, 0.14f, 1f)) : new Color(0.6f, 0.6f, 0.65f, 1f);
                tLbl.style.unityFontDefinition = Fd();
                titleRow.Add(tLbl);

                var subRow = new VisualElement();
                subRow.style.flexDirection = FlexDirection.Row;
                subRow.style.alignItems = Align.Center;
                subRow.style.marginTop = 3;
                midInfo.Add(subRow);

                var tagLbl = new Label(m.category);
                tagLbl.style.fontSize = 9; tagLbl.style.unityFontDefinition = Fd();
                tagLbl.style.paddingLeft = 4; tagLbl.style.paddingRight = 4;
                tagLbl.style.paddingTop = 1; tagLbl.style.paddingBottom = 1;
                tagLbl.style.borderTopLeftRadius = 4; tagLbl.style.borderTopRightRadius = 4;
                tagLbl.style.borderBottomLeftRadius = 4; tagLbl.style.borderBottomRightRadius = 4;
                tagLbl.style.backgroundColor = m.category == "BGM" ? new Color(0.90f, 0.94f, 0.98f, 1f) : new Color(0.98f, 0.93f, 0.88f, 1f);
                tagLbl.style.color = m.category == "BGM" ? new Color(0.20f, 0.50f, 0.80f, 1f) : new Color(0.80f, 0.45f, 0.15f, 1f);
                tagLbl.style.marginRight = 6;
                subRow.Add(tagLbl);

                var condLbl = new Label(unlocked ? (isCurrentPlaying ? "正在播放中..." : "轻触试听") : "解锁: " + m.condition);
                condLbl.style.fontSize = 10;
                condLbl.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
                condLbl.style.unityFontDefinition = Fd();
                subRow.Add(condLbl);

                // 右侧：爱心收藏 + 播放圆钮
                var rightActions = new VisualElement();
                rightActions.style.flexDirection = FlexDirection.Row;
                rightActions.style.alignItems = Align.Center;
                card.Add(rightActions);

                if (unlocked)
                {
                    // 收藏心形按钮
                    var heartBtn = new Button(() =>
                    {
                        ToggleTabletMusicFavorite(m.id);
                        BuildMusicApp(win);
                    }) { text = isFav ? "♥" : "♡" };
                    heartBtn.style.width = 28; heartBtn.style.height = 28;
                    heartBtn.style.fontSize = 15; heartBtn.style.unityFontDefinition = Fd();
                    heartBtn.style.backgroundColor = Color.clear;
                    heartBtn.style.color = isFav ? new Color(0.92f, 0.25f, 0.30f, 1f) : new Color(0.65f, 0.65f, 0.70f, 1f);
                    heartBtn.style.borderTopWidth = 0; heartBtn.style.borderBottomWidth = 0;
                    heartBtn.style.borderLeftWidth = 0; heartBtn.style.borderRightWidth = 0;
                    heartBtn.style.marginRight = 4;
                    rightActions.Add(heartBtn);

                    // 播放圆钮
                    var pBtn = new Button(() =>
                    {
                        if (isCurrentPlaying)
                        {
                            ToggleTabletMusicPlayPause();
                        }
                        else
                        {
                            PlayTabletTrack(m);
                        }
                        BuildMusicApp(win);
                    }) { text = isCurrentPlaying ? "❚❚" : "▶" };
                    pBtn.style.width = 30; pBtn.style.height = 30;
                    pBtn.style.fontSize = 11; pBtn.style.unityFontDefinition = Fd();
                    pBtn.style.borderTopLeftRadius = 15; pBtn.style.borderTopRightRadius = 15;
                    pBtn.style.borderBottomLeftRadius = 15; pBtn.style.borderBottomRightRadius = 15;
                    pBtn.style.backgroundColor = isCurrentPlaying ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.12f, 0.12f, 0.14f, 1f);
                    pBtn.style.color = Color.white;
                    rightActions.Add(pBtn);
                }
                else
                {
                    var lockLbl = new Label("🔒");
                    lockLbl.style.fontSize = 13; lockLbl.style.color = new Color(0.7f, 0.7f, 0.75f, 1f);
                    lockLbl.style.unityFontDefinition = Fd();
                    rightActions.Add(lockLbl);
                }
            }
        }
        else
        {
            // ==================== TAB 1: 我的 / 收藏视图（参考图三）====================
            // 1. 用户/会员横幅卡片
            var userProfileCard = new VisualElement();
            userProfileCard.style.flexDirection = FlexDirection.Row;
            userProfileCard.style.alignItems = Align.Center;
            userProfileCard.style.backgroundColor = Color.white;
            userProfileCard.style.borderTopLeftRadius = 16; userProfileCard.style.borderTopRightRadius = 16;
            userProfileCard.style.borderBottomLeftRadius = 16; userProfileCard.style.borderBottomRightRadius = 16;
            userProfileCard.style.borderTopWidth = 1; userProfileCard.style.borderBottomWidth = 1;
            userProfileCard.style.borderLeftWidth = 1; userProfileCard.style.borderRightWidth = 1;
            userProfileCard.style.borderTopColor = new Color(0.90f, 0.91f, 0.94f, 1f);
            userProfileCard.style.borderBottomColor = new Color(0.90f, 0.91f, 0.94f, 1f);
            userProfileCard.style.borderLeftColor = new Color(0.90f, 0.91f, 0.94f, 1f);
            userProfileCard.style.borderRightColor = new Color(0.90f, 0.91f, 0.94f, 1f);
            userProfileCard.style.paddingLeft = 18; userProfileCard.style.paddingRight = 18;
            userProfileCard.style.paddingTop = 14; userProfileCard.style.paddingBottom = 14;
            userProfileCard.style.marginBottom = 16;
            contentScroll.Add(userProfileCard);

            var avatarCircle = new VisualElement();
            avatarCircle.style.width = 46; avatarCircle.style.height = 46;
            avatarCircle.style.borderTopLeftRadius = 23; avatarCircle.style.borderTopRightRadius = 23;
            avatarCircle.style.borderBottomLeftRadius = 23; avatarCircle.style.borderBottomRightRadius = 23;
            avatarCircle.style.backgroundColor = new Color(0.20f, 0.55f, 0.85f, 1f);
            avatarCircle.style.alignItems = Align.Center; avatarCircle.style.justifyContent = Justify.Center;
            avatarCircle.style.marginRight = 14;
            userProfileCard.Add(avatarCircle);

            var avatarLetter = new Label("林");
            avatarLetter.style.fontSize = 20; avatarLetter.style.color = Color.white;
            avatarLetter.style.unityFontDefinition = Fd();
            avatarCircle.Add(avatarLetter);

            var userInfoBox = new VisualElement();
            userProfileCard.Add(userInfoBox);

            var uNameRow = new VisualElement();
            uNameRow.style.flexDirection = FlexDirection.Row;
            uNameRow.style.alignItems = Align.Center;
            userInfoBox.Add(uNameRow);

            var uName = new Label("林彪悍 · 首席调度员");
            uName.style.fontSize = 15; uName.style.unityFontStyleAndWeight = FontStyle.Bold;
            uName.style.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            uName.style.unityFontDefinition = Fd();
            uName.style.marginRight = 8;
            uNameRow.Add(uName);

            var vipBadge = new Label("VIP 铁道特权");
            vipBadge.style.fontSize = 10; vipBadge.style.color = Color.white;
            vipBadge.style.backgroundColor = new Color(0.18f, 0.72f, 0.42f, 1f);
            vipBadge.style.paddingLeft = 6; vipBadge.style.paddingRight = 6;
            vipBadge.style.paddingTop = 1; vipBadge.style.paddingBottom = 1;
            vipBadge.style.borderTopLeftRadius = 4; vipBadge.style.borderTopRightRadius = 4;
            vipBadge.style.borderBottomLeftRadius = 4; vipBadge.style.borderBottomRightRadius = 4;
            vipBadge.style.unityFontDefinition = Fd();
            uNameRow.Add(vipBadge);

            var uSub = new Label("母带原声特权生效中 · 13 首铁道音轨已就绪");
            uSub.style.fontSize = 11; uSub.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
            uSub.style.unityFontDefinition = Fd();
            uSub.style.marginTop = 3;
            userInfoBox.Add(uSub);

            // 2. 快捷四数据卡片（参考图三：喜欢 / 最近 / 本地 / 已购）
            var myGrid = new VisualElement();
            myGrid.style.flexDirection = FlexDirection.Row;
            myGrid.style.justifyContent = Justify.SpaceBetween;
            myGrid.style.marginBottom = 18;
            contentScroll.Add(myGrid);

            int favCount = GetTabletFavoriteCount();
            int unlockedCount = 0;
            foreach (var m in TabletMusicList) if (IsTabletMusicUnlocked(m)) unlockedCount++;

            System.Action<string, string, string, Color> addStatCard = (title, countStr, iconText, iconCol) =>
            {
                var sCard = new VisualElement();
                sCard.style.width = 175; sCard.style.height = 76;
                sCard.style.backgroundColor = Color.white;
                sCard.style.borderTopLeftRadius = 14; sCard.style.borderTopRightRadius = 14;
                sCard.style.borderBottomLeftRadius = 14; sCard.style.borderBottomRightRadius = 14;
                sCard.style.borderTopWidth = 1; sCard.style.borderBottomWidth = 1;
                sCard.style.borderLeftWidth = 1; sCard.style.borderRightWidth = 1;
                sCard.style.borderTopColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                sCard.style.borderBottomColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                sCard.style.borderLeftColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                sCard.style.borderRightColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                sCard.style.alignItems = Align.Center; sCard.style.justifyContent = Justify.Center;

                var sIcon = new Label(iconText);
                sIcon.style.fontSize = 18; sIcon.style.color = iconCol; sIcon.style.unityFontDefinition = Fd();
                sCard.Add(sIcon);

                var sTitle = new Label(title);
                sTitle.style.fontSize = 12; sTitle.style.color = new Color(0.2f, 0.2f, 0.22f, 1f);
                sTitle.style.unityFontDefinition = Fd();
                sTitle.style.marginTop = 2;
                sCard.Add(sTitle);

                var sCount = new Label(countStr);
                sCount.style.fontSize = 10; sCount.style.color = new Color(0.6f, 0.6f, 0.65f, 1f);
                sCount.style.unityFontDefinition = Fd();
                sCard.Add(sCount);

                myGrid.Add(sCard);
            };

            addStatCard("喜欢", favCount + " 首", "♥", new Color(0.92f, 0.25f, 0.30f, 1f));
            addStatCard("最近播放", (string.IsNullOrEmpty(tabletCurrentTrackTitle) ? "暂无" : "1 首"), "◷", new Color(0.18f, 0.72f, 0.42f, 1f));
            addStatCard("本地原声", "13 首", "💾", new Color(0.25f, 0.65f, 0.90f, 1f));
            addStatCard("已收录", unlockedCount + " 首", "✓", new Color(0.85f, 0.55f, 0.20f, 1f));

            // 3. 我喜欢的音乐列表
            var favSectionHead = new VisualElement();
            favSectionHead.style.flexDirection = FlexDirection.Row;
            favSectionHead.style.justifyContent = Justify.SpaceBetween;
            favSectionHead.style.alignItems = Align.Center;
            favSectionHead.style.marginBottom = 12;
            contentScroll.Add(favSectionHead);

            var favSecTitle = new Label("我喜欢的音乐 (" + favCount + ")");
            favSecTitle.style.fontSize = 16; favSecTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            favSecTitle.style.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            favSecTitle.style.unityFontDefinition = Fd();
            favSectionHead.Add(favSecTitle);

            var favList = new System.Collections.Generic.List<TabletMusicEntry>();
            foreach (var m in TabletMusicList)
            {
                if (IsTabletMusicFavorited(m.id) && IsTabletMusicUnlocked(m)) favList.Add(m);
            }

            if (favList.Count == 0)
            {
                // 空状态卡片
                var emptyCard = new VisualElement();
                emptyCard.style.paddingTop = 30; emptyCard.style.paddingBottom = 30;
                emptyCard.style.alignItems = Align.Center; emptyCard.style.justifyContent = Justify.Center;
                emptyCard.style.backgroundColor = Color.white;
                emptyCard.style.borderTopLeftRadius = 14; emptyCard.style.borderTopRightRadius = 14;
                emptyCard.style.borderBottomLeftRadius = 14; emptyCard.style.borderBottomRightRadius = 14;
                emptyCard.style.borderTopWidth = 1; emptyCard.style.borderBottomWidth = 1;
                emptyCard.style.borderLeftWidth = 1; emptyCard.style.borderRightWidth = 1;
                emptyCard.style.borderTopColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                emptyCard.style.borderBottomColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                emptyCard.style.borderLeftColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                emptyCard.style.borderRightColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                contentScroll.Add(emptyCard);

                var emptyIcon = new Label("♡");
                emptyIcon.style.fontSize = 32; emptyIcon.style.color = new Color(0.75f, 0.75f, 0.80f, 1f);
                emptyIcon.style.unityFontDefinition = Fd();
                emptyCard.Add(emptyIcon);

                var emptyText = new Label("暂无收藏曲目。轻触乐库中的爱心即可将歌曲加入这里！");
                emptyText.style.fontSize = 13; emptyText.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
                emptyText.style.unityFontDefinition = Fd();
                emptyText.style.marginTop = 8;
                emptyCard.Add(emptyText);

                var goExploreBtn = new Button(() =>
                {
                    tabletMusicNavTab = 0;
                    BuildMusicApp(win);
                }) { text = "前往乐库发现音乐" };
                goExploreBtn.style.height = 30; goExploreBtn.style.paddingLeft = 14; goExploreBtn.style.paddingRight = 14;
                goExploreBtn.style.fontSize = 12; goExploreBtn.style.unityFontDefinition = Fd();
                goExploreBtn.style.backgroundColor = new Color(0.12f, 0.65f, 0.36f, 1f);
                goExploreBtn.style.color = Color.white;
                goExploreBtn.style.borderTopLeftRadius = 15; goExploreBtn.style.borderTopRightRadius = 15;
                goExploreBtn.style.borderBottomLeftRadius = 15; goExploreBtn.style.borderBottomRightRadius = 15;
                goExploreBtn.style.marginTop = 12;
                emptyCard.Add(goExploreBtn);
            }
            else
            {
                var favGrid = new VisualElement();
                favGrid.style.flexDirection = FlexDirection.Row;
                favGrid.style.flexWrap = Wrap.Wrap;
                favGrid.style.justifyContent = Justify.SpaceBetween;
                contentScroll.Add(favGrid);

                foreach (var m in favList)
                {
                    bool isCurrentPlaying = (m.id == tabletCurrentTrackId && tabletIsPlaying);

                    var card = new VisualElement();
                    card.style.width = 360; card.style.height = 68;
                    card.style.marginBottom = 10;
                    card.style.flexDirection = FlexDirection.Row;
                    card.style.alignItems = Align.Center;
                    card.style.justifyContent = Justify.SpaceBetween;
                    card.style.paddingLeft = 10; card.style.paddingRight = 12;
                    card.style.backgroundColor = Color.white;
                    card.style.borderTopLeftRadius = 12; card.style.borderTopRightRadius = 12;
                    card.style.borderBottomLeftRadius = 12; card.style.borderBottomRightRadius = 12;
                    card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
                    card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
                    card.style.borderTopColor = isCurrentPlaying ? new Color(0.18f, 0.72f, 0.42f, 0.8f) : new Color(0.92f, 0.92f, 0.95f, 1f);
                    card.style.borderBottomColor = isCurrentPlaying ? new Color(0.18f, 0.72f, 0.42f, 0.8f) : new Color(0.92f, 0.92f, 0.95f, 1f);
                    card.style.borderLeftColor = isCurrentPlaying ? new Color(0.18f, 0.72f, 0.42f, 0.8f) : new Color(0.92f, 0.92f, 0.95f, 1f);
                    card.style.borderRightColor = isCurrentPlaying ? new Color(0.18f, 0.72f, 0.42f, 0.8f) : new Color(0.92f, 0.92f, 0.95f, 1f);
                    favGrid.Add(card);

                    var coverBox = new VisualElement();
                    coverBox.style.width = 46; coverBox.style.height = 46;
                    coverBox.style.borderTopLeftRadius = 8; coverBox.style.borderTopRightRadius = 8;
                    coverBox.style.borderBottomLeftRadius = 8; coverBox.style.borderBottomRightRadius = 8;
                    coverBox.style.backgroundColor = isCurrentPlaying ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.92f, 0.25f, 0.30f, 0.9f);
                    coverBox.style.alignItems = Align.Center; coverBox.style.justifyContent = Justify.Center;
                    coverBox.style.marginRight = 10;
                    card.Add(coverBox);

                    var discLabel = new Label("♥");
                    discLabel.style.fontSize = 18; discLabel.style.color = Color.white;
                    discLabel.style.unityFontDefinition = Fd();
                    coverBox.Add(discLabel);

                    var midInfo = new VisualElement();
                    midInfo.style.flexGrow = 1;
                    card.Add(midInfo);

                    var tLbl = new Label(m.title);
                    tLbl.style.fontSize = 13; tLbl.style.unityFontStyleAndWeight = isCurrentPlaying ? FontStyle.Bold : FontStyle.Normal;
                    tLbl.style.color = isCurrentPlaying ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.12f, 0.12f, 0.14f, 1f);
                    tLbl.style.unityFontDefinition = Fd();
                    midInfo.Add(tLbl);

                    var subLbl = new Label(m.category + " · 已加入红心收藏");
                    subLbl.style.fontSize = 10; subLbl.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
                    subLbl.style.unityFontDefinition = Fd();
                    midInfo.Add(subLbl);

                    var rightActions = new VisualElement();
                    rightActions.style.flexDirection = FlexDirection.Row;
                    rightActions.style.alignItems = Align.Center;
                    card.Add(rightActions);

                    var unFavBtn = new Button(() =>
                    {
                        ToggleTabletMusicFavorite(m.id);
                        BuildMusicApp(win);
                    }) { text = "♥" };
                    unFavBtn.style.width = 28; unFavBtn.style.height = 28;
                    unFavBtn.style.fontSize = 15; unFavBtn.style.unityFontDefinition = Fd();
                    unFavBtn.style.backgroundColor = Color.clear;
                    unFavBtn.style.color = new Color(0.92f, 0.25f, 0.30f, 1f);
                    unFavBtn.style.borderTopWidth = 0; unFavBtn.style.borderBottomWidth = 0;
                    unFavBtn.style.borderLeftWidth = 0; unFavBtn.style.borderRightWidth = 0;
                    rightActions.Add(unFavBtn);

                    var pBtn = new Button(() =>
                    {
                        if (isCurrentPlaying) ToggleTabletMusicPlayPause();
                        else PlayTabletTrack(m);
                        BuildMusicApp(win);
                    }) { text = isCurrentPlaying ? "❚❚" : "▶" };
                    pBtn.style.width = 30; pBtn.style.height = 30;
                    pBtn.style.fontSize = 11; pBtn.style.unityFontDefinition = Fd();
                    pBtn.style.borderTopLeftRadius = 15; pBtn.style.borderTopRightRadius = 15;
                    pBtn.style.borderBottomLeftRadius = 15; pBtn.style.borderBottomRightRadius = 15;
                    pBtn.style.backgroundColor = isCurrentPlaying ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.12f, 0.12f, 0.14f, 1f);
                    pBtn.style.color = Color.white;
                    pBtn.style.marginLeft = 4;
                    rightActions.Add(pBtn);
                }
            }
        }

        // ===== 底部悬浮流媒体播放条（参考图二图三下方悬浮胶囊栏）=====
        var bar = new VisualElement();
        bar.style.position = Position.Absolute;
        bar.style.bottom = 10;
        bar.style.left = 16;
        bar.style.right = 16;
        bar.style.height = 66;
        bar.style.flexDirection = FlexDirection.Row;
        bar.style.alignItems = Align.Center;
        bar.style.justifyContent = Justify.SpaceBetween;
        bar.style.backgroundColor = new Color(1f, 1f, 1f, 0.98f);
        bar.style.borderTopLeftRadius = 16; bar.style.borderTopRightRadius = 16;
        bar.style.borderBottomLeftRadius = 16; bar.style.borderBottomRightRadius = 16;
        bar.style.borderTopWidth = 1; bar.style.borderBottomWidth = 1;
        bar.style.borderLeftWidth = 1; bar.style.borderRightWidth = 1;
        bar.style.borderTopColor = new Color(0.86f, 0.87f, 0.90f, 1f);
        bar.style.borderBottomColor = new Color(0.86f, 0.87f, 0.90f, 1f);
        bar.style.borderLeftColor = new Color(0.86f, 0.87f, 0.90f, 1f);
        bar.style.borderRightColor = new Color(0.86f, 0.87f, 0.90f, 1f);
        bar.style.paddingLeft = 14; bar.style.paddingRight = 16;
        mainArea.Add(bar);

        // 左部：曲目封面 + 标题 + 收藏心标
        var barLeft = new VisualElement();
        barLeft.style.width = 230;
        barLeft.style.flexDirection = FlexDirection.Row;
        barLeft.style.alignItems = Align.Center;
        bar.Add(barLeft);

        var miniCover = new VisualElement();
        miniCover.style.width = 40; miniCover.style.height = 40;
        miniCover.style.borderTopLeftRadius = 8; miniCover.style.borderTopRightRadius = 8;
        miniCover.style.borderBottomLeftRadius = 8; miniCover.style.borderBottomRightRadius = 8;
        miniCover.style.backgroundColor = tabletIsPlaying ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.18f, 0.18f, 0.22f, 1f);
        miniCover.style.alignItems = Align.Center; miniCover.style.justifyContent = Justify.Center;
        miniCover.style.marginRight = 10;
        barLeft.Add(miniCover);

        var miniDiscIcon = new Label(tabletIsPlaying ? "♫" : "◎");
        miniDiscIcon.style.fontSize = 16; miniDiscIcon.style.color = Color.white;
        miniDiscIcon.style.unityFontDefinition = Fd();
        miniCover.Add(miniDiscIcon);

        var barTextGroup = new VisualElement();
        barTextGroup.style.flexGrow = 1;
        barTextGroup.style.overflow = Overflow.Hidden;
        barLeft.Add(barTextGroup);

        tabletPlayerTitleLbl = new Label(string.IsNullOrEmpty(tabletCurrentTrackTitle) ? "未选择播放曲目" : tabletCurrentTrackTitle);
        tabletPlayerTitleLbl.style.fontSize = 13; tabletPlayerTitleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        tabletPlayerTitleLbl.style.color = new Color(0.12f, 0.12f, 0.14f, 1f);
        tabletPlayerTitleLbl.style.unityFontDefinition = Fd();
        barTextGroup.Add(tabletPlayerTitleLbl);

        tabletPlayerSubLbl = new Label(string.IsNullOrEmpty(tabletCurrentTrackCategory) ? "轻触上方曲目即可收听" : tabletCurrentTrackCategory + " · 铁道原声品质");
        tabletPlayerSubLbl.style.fontSize = 10; tabletPlayerSubLbl.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
        tabletPlayerSubLbl.style.unityFontDefinition = Fd();
        tabletPlayerSubLbl.style.marginTop = 2;
        barTextGroup.Add(tabletPlayerSubLbl);

        bool currentIsFav = !string.IsNullOrEmpty(tabletCurrentTrackId) && IsTabletMusicFavorited(tabletCurrentTrackId);
        tabletFavToggleBtn = new Button(() =>
        {
            if (!string.IsNullOrEmpty(tabletCurrentTrackId))
            {
                ToggleTabletMusicFavorite(tabletCurrentTrackId);
                BuildMusicApp(win);
            }
        }) { text = currentIsFav ? "♥" : "♡" };
        tabletFavToggleBtn.style.width = 30; tabletFavToggleBtn.style.height = 30;
        tabletFavToggleBtn.style.fontSize = 16; tabletFavToggleBtn.style.unityFontDefinition = Fd();
        tabletFavToggleBtn.style.backgroundColor = Color.clear;
        tabletFavToggleBtn.style.color = currentIsFav ? new Color(0.92f, 0.25f, 0.30f, 1f) : new Color(0.60f, 0.60f, 0.65f, 1f);
        tabletFavToggleBtn.style.borderTopWidth = 0; tabletFavToggleBtn.style.borderBottomWidth = 0;
        tabletFavToggleBtn.style.borderLeftWidth = 0; tabletFavToggleBtn.style.borderRightWidth = 0;
        tabletFavToggleBtn.style.marginLeft = 6;
        barLeft.Add(tabletFavToggleBtn);

        // 中部：控制按钮组 + 灵敏点击/拖动跳转进度条
        var barCenter = new VisualElement();
        barCenter.style.flexGrow = 1;
        barCenter.style.flexDirection = FlexDirection.Column;
        barCenter.style.alignItems = Align.Center;
        barCenter.style.justifyContent = Justify.Center;
        barCenter.style.marginLeft = 16; barCenter.style.marginRight = 16;
        bar.Add(barCenter);

        // 控制按钮排（上一首 / 播放暂停圆钮 / 下一首）
        var ctrlRow = new VisualElement();
        ctrlRow.style.flexDirection = FlexDirection.Row;
        ctrlRow.style.alignItems = Align.Center;
        ctrlRow.style.marginBottom = 4;
        barCenter.Add(ctrlRow);

        var prevBtn = new Button(() => { PlayPrevTabletTrack(); BuildMusicApp(win); }) { text = "|◀" };
        prevBtn.style.width = 28; prevBtn.style.height = 28;
        prevBtn.style.fontSize = 11; prevBtn.style.unityFontDefinition = Fd();
        prevBtn.style.backgroundColor = Color.clear;
        prevBtn.style.color = new Color(0.3f, 0.3f, 0.35f, 1f);
        prevBtn.style.borderTopWidth = 0; prevBtn.style.borderBottomWidth = 0;
        prevBtn.style.borderLeftWidth = 0; prevBtn.style.borderRightWidth = 0;
        ctrlRow.Add(prevBtn);

        tabletPlayToggleBtn = new Button(ToggleTabletMusicPlayPause) { text = (tabletIsPlaying ? "❚❚" : "▶") };
        tabletPlayToggleBtn.style.width = 34; tabletPlayToggleBtn.style.height = 34;
        tabletPlayToggleBtn.style.fontSize = 13; tabletPlayToggleBtn.style.unityFontDefinition = Fd();
        tabletPlayToggleBtn.style.borderTopLeftRadius = 17; tabletPlayToggleBtn.style.borderTopRightRadius = 17;
        tabletPlayToggleBtn.style.borderBottomLeftRadius = 17; tabletPlayToggleBtn.style.borderBottomRightRadius = 17;
        tabletPlayToggleBtn.style.backgroundColor = new Color(0.12f, 0.65f, 0.36f, 1f); // 翡翠绿大圆钮
        tabletPlayToggleBtn.style.color = Color.white;
        tabletPlayToggleBtn.style.borderTopWidth = 0; tabletPlayToggleBtn.style.borderBottomWidth = 0;
        tabletPlayToggleBtn.style.borderLeftWidth = 0; tabletPlayToggleBtn.style.borderRightWidth = 0;
        tabletPlayToggleBtn.style.marginLeft = 12; tabletPlayToggleBtn.style.marginRight = 12;
        ctrlRow.Add(tabletPlayToggleBtn);

        var nextBtn = new Button(() => { PlayNextTabletTrack(); BuildMusicApp(win); }) { text = "▶|" };
        nextBtn.style.width = 28; nextBtn.style.height = 28;
        nextBtn.style.fontSize = 11; nextBtn.style.unityFontDefinition = Fd();
        nextBtn.style.backgroundColor = Color.clear;
        nextBtn.style.color = new Color(0.3f, 0.3f, 0.35f, 1f);
        nextBtn.style.borderTopWidth = 0; nextBtn.style.borderBottomWidth = 0;
        nextBtn.style.borderLeftWidth = 0; nextBtn.style.borderRightWidth = 0;
        ctrlRow.Add(nextBtn);

        // 灵敏进度条排（当前时间 + 自定义点击/滑动轨道 + 总时间）
        var progRow = new VisualElement();
        progRow.style.width = Length.Percent(100);
        progRow.style.flexDirection = FlexDirection.Row;
        progRow.style.alignItems = Align.Center;
        barCenter.Add(progRow);

        tabletCurTimeLbl = new Label(tabletMusicSource != null ? FormatAudioTime(tabletMusicSource.time) : "0:00");
        tabletCurTimeLbl.style.fontSize = 10; tabletCurTimeLbl.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
        tabletCurTimeLbl.style.unityFontDefinition = Fd();
        tabletCurTimeLbl.style.width = 30;
        progRow.Add(tabletCurTimeLbl);

        // 高灵敏交互轨道：高度 16px 充当点击碰撞区，中间 4px 细滑轨
        tabletProgressTrack = new VisualElement();
        tabletProgressTrack.style.flexGrow = 1;
        tabletProgressTrack.style.height = 16;
        tabletProgressTrack.style.marginLeft = 8; tabletProgressTrack.style.marginRight = 8;
        tabletProgressTrack.style.justifyContent = Justify.Center;
        tabletProgressTrack.pickingMode = PickingMode.Position;
        progRow.Add(tabletProgressTrack);

        var trackBg = new VisualElement();
        trackBg.style.height = 4;
        trackBg.style.backgroundColor = new Color(0.88f, 0.89f, 0.92f, 1f);
        trackBg.style.borderTopLeftRadius = 2; trackBg.style.borderTopRightRadius = 2;
        trackBg.style.borderBottomLeftRadius = 2; trackBg.style.borderBottomRightRadius = 2;
        trackBg.style.position = Position.Relative;
        trackBg.pickingMode = PickingMode.Ignore;
        tabletProgressTrack.Add(trackBg);

        float initialPct = 0f;
        if (tabletMusicSource != null && tabletCurrentClip != null && tabletCurrentClip.length > 0.1f)
        {
            initialPct = Mathf.Clamp01(tabletMusicSource.time / tabletCurrentClip.length) * 100f;
        }

        tabletProgressFill = new VisualElement();
        tabletProgressFill.style.height = Length.Percent(100);
        tabletProgressFill.style.width = Length.Percent(initialPct);
        tabletProgressFill.style.backgroundColor = new Color(0.12f, 0.65f, 0.36f, 1f);
        tabletProgressFill.style.borderTopLeftRadius = 2; tabletProgressFill.style.borderTopRightRadius = 2;
        tabletProgressFill.style.borderBottomLeftRadius = 2; tabletProgressFill.style.borderBottomRightRadius = 2;
        tabletProgressFill.pickingMode = PickingMode.Ignore;
        trackBg.Add(tabletProgressFill);

        tabletProgressKnob = new VisualElement();
        tabletProgressKnob.style.width = 10; tabletProgressKnob.style.height = 10;
        tabletProgressKnob.style.borderTopLeftRadius = 5; tabletProgressKnob.style.borderTopRightRadius = 5;
        tabletProgressKnob.style.borderBottomLeftRadius = 5; tabletProgressKnob.style.borderBottomRightRadius = 5;
        tabletProgressKnob.style.backgroundColor = Color.white;
        tabletProgressKnob.style.borderTopWidth = 1.5f; tabletProgressKnob.style.borderBottomWidth = 1.5f;
        tabletProgressKnob.style.borderLeftWidth = 1.5f; tabletProgressKnob.style.borderRightWidth = 1.5f;
        tabletProgressKnob.style.borderTopColor = new Color(0.12f, 0.65f, 0.36f, 1f);
        tabletProgressKnob.style.borderBottomColor = new Color(0.12f, 0.65f, 0.36f, 1f);
        tabletProgressKnob.style.borderLeftColor = new Color(0.12f, 0.65f, 0.36f, 1f);
        tabletProgressKnob.style.borderRightColor = new Color(0.12f, 0.65f, 0.36f, 1f);
        tabletProgressKnob.style.position = Position.Absolute;
        tabletProgressKnob.style.top = -3;
        tabletProgressKnob.style.left = Length.Percent(initialPct);
        tabletProgressKnob.pickingMode = PickingMode.Ignore;
        trackBg.Add(tabletProgressKnob);

        // 核心：彻底支持点击跳转 + 滑动跳转
        System.Action<Vector2> handleSeek = (localPos) =>
        {
            if (tabletMusicSource == null || tabletCurrentClip == null) return;
            float trackWidth = tabletProgressTrack.resolvedStyle.width;
            if (trackWidth <= 10f) return;
            float ratio = Mathf.Clamp01(localPos.x / trackWidth);
            tabletMusicSource.time = ratio * tabletCurrentClip.length;
            if (tabletCurTimeLbl != null) tabletCurTimeLbl.text = FormatAudioTime(tabletMusicSource.time);
            if (tabletProgressFill != null) tabletProgressFill.style.width = Length.Percent(ratio * 100f);
            if (tabletProgressKnob != null) tabletProgressKnob.style.left = Length.Percent(ratio * 100f);
        };

        tabletProgressTrack.RegisterCallback<PointerDownEvent>(evt =>
        {
            tabletProgressScrubbing = true;
            handleSeek(evt.localPosition);
            tabletProgressTrack.CapturePointer(evt.pointerId);
        });
        tabletProgressTrack.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (tabletProgressScrubbing) handleSeek(evt.localPosition);
        });
        tabletProgressTrack.RegisterCallback<PointerUpEvent>(evt =>
        {
            tabletProgressScrubbing = false;
            if (tabletProgressTrack.HasPointerCapture(evt.pointerId))
                tabletProgressTrack.ReleasePointer(evt.pointerId);
        });
        tabletProgressTrack.RegisterCallback<PointerCaptureOutEvent>(_ => tabletProgressScrubbing = false);

        tabletTotalTimeLbl = new Label(tabletCurrentClip != null ? FormatAudioTime(tabletCurrentClip.length) : "0:00");
        tabletTotalTimeLbl.style.fontSize = 10; tabletTotalTimeLbl.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
        tabletTotalTimeLbl.style.unityFontDefinition = Fd();
        tabletTotalTimeLbl.style.width = 30;
        progRow.Add(tabletTotalTimeLbl);

        // 右部：播放模式切换 + 音量快捷控制
        var barRight = new VisualElement();
        barRight.style.width = 170;
        barRight.style.flexDirection = FlexDirection.Row;
        barRight.style.alignItems = Align.Center;
        barRight.style.justifyContent = Justify.FlexEnd;
        bar.Add(barRight);

        string modeStr = (tabletPlayMode == TabletPlayMode.SingleRepeat ? "单曲循环" : (tabletPlayMode == TabletPlayMode.Sequential ? "顺序播放" : "随机播放"));
        tabletModeToggleBtn = new Button(() =>
        {
            tabletPlayMode = (TabletPlayMode)(((int)tabletPlayMode + 1) % 3);
            if (tabletMusicSource != null) tabletMusicSource.loop = (tabletPlayMode == TabletPlayMode.SingleRepeat);
            BuildMusicApp(win);
        }) { text = modeStr };
        tabletModeToggleBtn.style.height = 26; tabletModeToggleBtn.style.paddingLeft = 8; tabletModeToggleBtn.style.paddingRight = 8;
        tabletModeToggleBtn.style.fontSize = 11; tabletModeToggleBtn.style.unityFontDefinition = Fd();
        tabletModeToggleBtn.style.backgroundColor = new Color(0.95f, 0.95f, 0.97f, 1f);
        tabletModeToggleBtn.style.color = new Color(0.25f, 0.25f, 0.28f, 1f);
        tabletModeToggleBtn.style.borderTopLeftRadius = 13; tabletModeToggleBtn.style.borderTopRightRadius = 13;
        tabletModeToggleBtn.style.borderBottomLeftRadius = 13; tabletModeToggleBtn.style.borderBottomRightRadius = 13;
        tabletModeToggleBtn.style.borderTopWidth = 1; tabletModeToggleBtn.style.borderBottomWidth = 1;
        tabletModeToggleBtn.style.borderLeftWidth = 1; tabletModeToggleBtn.style.borderRightWidth = 1;
        tabletModeToggleBtn.style.borderTopColor = new Color(0.88f, 0.88f, 0.92f, 1f);
        tabletModeToggleBtn.style.borderBottomColor = new Color(0.88f, 0.88f, 0.92f, 1f);
        tabletModeToggleBtn.style.borderLeftColor = new Color(0.88f, 0.88f, 0.92f, 1f);
        tabletModeToggleBtn.style.borderRightColor = new Color(0.88f, 0.88f, 0.92f, 1f);
        barRight.Add(tabletModeToggleBtn);

        var volBtn = new Button(() =>
        {
            if (tabletMusicSource != null)
            {
                tabletMusicSource.volume = (tabletMusicSource.volume > 0.05f) ? 0f : 0.65f;
                ShowToast(tabletMusicSource.volume > 0.05f ? "音量已恢复" : "已静音");
            }
        }) { text = "🔊" };
        volBtn.style.width = 28; volBtn.style.height = 28;
        volBtn.style.fontSize = 13; volBtn.style.unityFontDefinition = Fd();
        volBtn.style.backgroundColor = Color.clear;
        volBtn.style.borderTopWidth = 0; volBtn.style.borderBottomWidth = 0;
        volBtn.style.borderLeftWidth = 0; volBtn.style.borderRightWidth = 0;
        volBtn.style.marginLeft = 6;
        barRight.Add(volBtn);
    }


    }
}
