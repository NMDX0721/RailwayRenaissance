using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 平板应用：随身听 · 铁道内网广播台（全量 26 首曲目 + 剧情探索解锁 + 局部状态驱动无闪烁交互）
    /// </summary>
    public static class TabletMusicAppUI
    {
        public class TabletMusicEntry
        {
            public string id;
            public string title;
            public string category; // "BGM", "歌曲", "广播", "白噪音"
            public string clipName;
            public string condition;
            public string desc; // 专属世界观氛围短句
        }

        // ============ 全量 26 首官方原声曲库与沉浸式短句 ============
        public static readonly TabletMusicEntry[] TabletMusicList = {
            new TabletMusicEntry { id = "platform", title = "Platform 站台", category = "BGM", clipName = "platform", condition = "标题画面自动解锁", desc = "残阳如血，锈轨伸向旧时代的沙海深处" },
            new TabletMusicEntry { id = "iron_and_ash", title = "Iron & Ash 铁与灰", category = "BGM", clipName = "iron_and_ash", condition = "标题画面自动解锁", desc = "锅炉低吼，沉重的活塞推开凝固的历史" },
            new TabletMusicEntry { id = "cloud_rail", title = "Cloud-Rail 云轨", category = "BGM", clipName = "cloud_rail", condition = "启程飞行途中解锁", desc = "风鸣于千米之上，飞空艇俯瞰茫茫云海与旧轨" },
            new TabletMusicEntry { id = "embers", title = "Embers 余烬", category = "BGM", clipName = "embers", condition = "抵达雾峰站时解锁", desc = "雾峰站炉火微明，风雪中旅人抖落大衣上的霜粒" },
            new TabletMusicEntry { id = "calm", title = "Calm 站前微风", category = "BGM", clipName = "calm", condition = "雾峰站站台微风解锁", desc = "破晓微风拂过空旷站台，远方传来清脆的鸟鸣" },
            new TabletMusicEntry { id = "night_cargo", title = "Night Cargo 夜行货", category = "BGM", clipName = "night_cargo", condition = "边境夜航时解锁", desc = "边境夜航，漆黑车厢满载矿砂与不可言说的秘密" },
            new TabletMusicEntry { id = "first_light", title = "First Light 晨光", category = "BGM", clipName = "first_light", condition = "飞越华北平原时解锁", desc = "第一缕金曦刺破夜雾，照亮漫漫关山路基" },
            new TabletMusicEntry { id = "morning", title = "Morning 破晓晨曦", category = "BGM", clipName = "morning", condition = "首班班车破晓出发解锁", desc = "列车准点启动，汽笛长鸣拉开新一天的巡程" },
            new TabletMusicEntry { id = "borderline", title = "Borderline 国境线", category = "BGM", clipName = "borderline", condition = "边境危机时解锁", desc = "探照灯划过高耸铁丝网，荷枪实弹的边境联合哨所" },
            new TabletMusicEntry { id = "wheels_joke", title = "The Wheel's Joke 方向盘在笑", category = "BGM", clipName = "wheels_joke", condition = "统一便当店购物解锁", desc = "关东便当店热气腾腾，老店主爽朗的谈笑与算盘声" },
            new TabletMusicEntry { id = "determination", title = "Determination 破沙前行", category = "BGM", clipName = "determination", condition = "线路巡视与整备解锁", desc = "道锤撞击锰钢道钉，保线工人在风沙中筑起生命线" },
            new TabletMusicEntry { id = "emotional", title = "Emotional 岁月回声", category = "BGM", clipName = "emotional", condition = "旧人重逢叙旧解锁", desc = "岁月流转，故人重聚在老站房茶案前的长吁轻叹" },
            new TabletMusicEntry { id = "melancholy", title = "Melancholy 铁轨叹息", category = "BGM", clipName = "melancholy", condition = "追忆爷爷旧车库解锁", desc = "荒废机库内，尘埃在穿透破损屋顶的阳光中起舞" },
            new TabletMusicEntry { id = "mystery", title = "Mystery 边境暗涌", category = "BGM", clipName = "mystery", condition = "边境联合追捕解锁", desc = "沙能暗渠深处的幽蓝微光，各方势力的无声竞逐" },
            new TabletMusicEntry { id = "curious", title = "Curious 沙能微光", category = "BGM", clipName = "curious", condition = "大学实验室探秘解锁", desc = "大学实验室试管震颤，神秘晶体散发奇异的高频嗡鸣" },
            new TabletMusicEntry { id = "warm", title = "Warmth 车厢炉火", category = "BGM", clipName = "warm", condition = "老伙计们聚首饮茶解锁", desc = "车长室小煤炉炖着热汤，老旧收音机沙沙作响的温暖" },
            new TabletMusicEntry { id = "silence", title = "Silence 荒原长夜", category = "BGM", clipName = "silence", condition = "夜宿雾峰老工棚解锁", desc = "深夜荒原，唯有漫天星斗与铁轨冷却时的轻微弹响" },
            new TabletMusicEntry { id = "train_through_keys", title = "Train Through Keys 旧曲", category = "BGM", clipName = "train_through_keys", condition = "已废弃，保留试听", desc = "黑白琴键敲出的旧旋律，流淌着留声机年代的余温" },
            new TabletMusicEntry { id = "south_wind", title = "남풍 南风", category = "歌曲", clipName = "south_wind", condition = "统一便当店采购后解锁", desc = "来自暖湿洋面的风，穿过山峦吹拂向广阔的北方" },
            new TabletMusicEntry { id = "starlit_rails", title = "별빛 철길 星光铁轨", category = "歌曲", clipName = "starlit_rails", condition = "边境夜航穿越风沙解锁", desc = "夜行平原，两侧双线闭塞信号灯在星空下如红绿宝石" },
            new TabletMusicEntry { id = "chollima_ride", title = "천리마 신시대에 달리다 千里马驰骋新时代", category = "歌曲", clipName = "chollima_ride", condition = "边境途中播放新闻解锁", desc = "沙能引擎全功率轰鸣，风驰电掣的新时代钢铁动脉" },
            new TabletMusicEntry { id = "sleepers", title = "Sleepers 铁轨沉睡者", category = "歌曲", clipName = "sleepers", condition = "片尾曲（待生成）", desc = "埋入路基的千万根枕木，无言托举起滚滚向前的时代" },
            new TabletMusicEntry { id = "news", title = "News 时代电波", category = "广播", clipName = "news", condition = "序章第一章广播电台解锁", desc = "收音机电子管微热，调频广播传出沙能重组的新闻" },
            new TabletMusicEntry { id = "news_explosive", title = "News Flash 重大快讯", category = "广播", clipName = "news_explosive", condition = "沙能革命爆发通讯解锁", desc = "高音喇叭紧急插播，改变整座大陆命运的时代巨震" },
            new TabletMusicEntry { id = "ambient_nature", title = "Wilds 风沙自然", category = "白噪音", clipName = "ambient_nature", condition = "穿越华北平原解锁", desc = "呼啸风沙掠过戈壁石滩，万籁俱寂的大自然呼吸" },
            new TabletMusicEntry { id = "train_ambient", title = "Train Rhythm 轮轨声响", category = "白噪音", clipName = "train_ambient", condition = "首发班车平稳运行解锁", desc = "重型列车富有节奏的铿锵轮轨声，旅途最安心的伴奏" }
        };

        public static string tabletMusicFilter = "全部"; // "全部", "BGM", "歌曲", "广播/白噪音"
        public static AudioSource tabletMusicSource;
        public static AudioClip tabletCurrentClip;
        public static string tabletCurrentTrackId;
        public static string tabletCurrentTrackTitle;
        public static string tabletCurrentTrackCategory;
        public static bool tabletIsPlaying;
        public enum TabletPlayMode { SingleRepeat, Sequential, Shuffle }
        public static TabletPlayMode tabletPlayMode = TabletPlayMode.Sequential;
        private static bool tabletShowLyrics = true;
        public static MusicSidebarNavView.MusicViewId s_currentView = MusicSidebarNavView.MusicViewId.Discover;

        private static readonly Dictionary<string, string> s_trackDurations = new Dictionary<string, string>
        {
            { "platform", "03:14" },
            { "iron_and_ash", "04:07" },
            { "cloud_rail", "02:58" },
            { "embers", "03:22" },
            { "calm", "02:46" },
            { "night_cargo", "03:35" },
            { "first_light", "02:50" },
            { "morning", "03:08" },
            { "borderline", "03:42" },
            { "wheels_joke", "02:30" },
            { "determination", "03:18" },
            { "emotional", "03:52" },
            { "melancholy", "03:10" },
            { "mystery", "02:44" },
            { "curious", "02:36" },
            { "warm", "03:02" },
            { "silence", "03:28" },
            { "train_through_keys", "02:20" },
            { "south_wind", "03:45" },
            { "starlit_rails", "04:12" },
            { "chollima_ride", "03:30" },
            { "sleepers", "04:05" },
            { "news", "01:45" },
            { "news_explosive", "01:28" },
            { "ambient_nature", "04:30" },
            { "train_ambient", "05:00" }
        };

        public static string GetTrackDurationText(TabletMusicEntry m)
        {
            if (m == null) return "03:00";
            if (m.id == tabletCurrentTrackId && tabletCurrentClip != null)
            {
                return FormatAudioTime(tabletCurrentClip.length);
            }
            if (s_trackDurations.TryGetValue(m.id, out string dur))
            {
                return dur;
            }
            return "03:15";
        }

        private static float s_currentVolume = 0.65f;
        private static bool s_isMuted = false;

        // UI 引用与绑定缓存（消除全局重建重绘）
        private static VisualElement s_activeWindow;
        private static VisualElement tabletProgressTrack;
        private static VisualElement tabletProgressFill;
        private static VisualElement tabletProgressKnob;
        private static Label tabletCurTimeLbl;
        private static Label tabletTotalTimeLbl;
        private static Label tabletPlayerTitleLbl;
        private static Label tabletPlayerSubLbl;
        private static VisualElement tabletMiniCover;
        private static Label tabletMiniDiscIcon;
        private static Button tabletPlayToggleBtn;
        private static VisualElement tabletModeToggleBtn;
        private static Button tabletFavToggleBtn;
        private static Action s_onClose;
        private static Button tabletVolBtn;
        private static Slider tabletVolSlider;
        private static bool tabletProgressScrubbing;
        private static float tabletMusicProgressTimer;

        // MiMo 审核追加 6：数据层状态留口 (待播队列、播放计数、最近播放)
        public static readonly List<string> PlayQueue = new List<string>();
        public static readonly Dictionary<string, int> PlayCountMap = new Dictionary<string, int>();
        public static readonly Dictionary<string, float> LastPlayedMap = new Dictionary<string, float>();

        // Phase 1 & 3 拆分视图句柄
        private static MusicSidebarNavView.SidebarHandle s_sidebarHandle;
        private static MusicPlayerBarView.PlayerBarHandle s_playerBarHandle;
        private static MusicNowPlayingSidebarView.SidebarHandle s_nowPlayingSidebarHandle;

        // 音浪动画组件引用
        private static readonly List<VisualElement> s_waveBars = new List<VisualElement>();
        private static float s_waveAnimTime = 0f;

        // 卡片与音轨绑定字典：trackId -> CardControls
        private class CardBinding
        {
            public VisualElement cardElement;
            public VisualElement coverBox;
            public Label discLabel;
            public Label titleLabel;
            public Label condLabel;
            public Button playBtn;
            public Button heartBtn;
            public VisualElement waveBox;
            public Label trackIndexLabel;
            public TabletMusicEntry entry;
        }
        private static readonly Dictionary<string, CardBinding> s_cardBindings = new Dictionary<string, CardBinding>();

        // 右侧正在播放大视窗引用（高质感 16-bit 像素发光黑胶唱盘与世界观剧情档案）
        private static bool s_isImmersiveMode = false;
        private static VisualElement s_nowPlayingVinylDisc;
        private static VisualElement s_nowPlayingHaloGlow;
        private static MusicIconHelper.VuMeterHandle s_vuMeterHandle;
        private static float s_nowPlayingRotation = 0f;
        private static float s_vinylAngularVelocity = 0f; // 黑胶机械飞轮角速度（带惯性阻尼）
        private static Label s_nowPlayingTitleLbl;
        private static Label s_nowPlayingCategoryLbl;
        private static Label s_nowPlayingLoreLbl;
        private static Label s_nowPlayingNextTitleLbl;
        private static Action s_playNextAction;

        // ============ 解锁与收藏状态判断 ============
        public static bool IsTabletMusicUnlocked(TabletMusicEntry m)
        {
            if (m == null) return false;
            // 初始标题画面解锁 platform 与 iron_and_ash（经典设定）
            if (m.id == "platform" || m.id == "iron_and_ash") return true;
            return PlayerPrefs.GetInt("ArchiveMusic_" + m.clipName, 0) == 1 || PlayerPrefs.GetInt("ArchiveMusic_" + m.id, 0) == 1;
        }

        public static bool IsTabletMusicFavorited(string trackId)
        {
            if (string.IsNullOrEmpty(trackId)) return false;
            return PlayerPrefs.GetInt("MusicFav_" + trackId, 0) == 1;
        }

        public static void ToggleTabletMusicFavorite(string trackId)
        {
            if (string.IsNullOrEmpty(trackId)) return;
            bool cur = IsTabletMusicFavorited(trackId);
            PlayerPrefs.SetInt("MusicFav_" + trackId, cur ? 0 : 1);
            PlayerPrefs.Save();

            // 局部刷新卡片与底栏红心
            UpdateFavoriteVisuals(trackId);
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

        private static VisualElement s_toastPill;
        private static Label s_toastLabel;
        private static IVisualElementScheduledItem s_toastScheduledItem;
        private static IVisualElementScheduledItem s_progressScheduledItem;
        private static Action<string> s_showToast;

        public static void ShowToast(string msg)
        {
            if (s_toastPill != null && s_toastLabel != null)
            {
                s_toastLabel.text = msg;
                s_toastPill.BringToFront();
                s_toastPill.style.display = DisplayStyle.Flex;
                s_toastPill.style.opacity = 1f;
                s_toastScheduledItem?.Pause();
                s_toastScheduledItem = s_toastPill.schedule.Execute(() =>
                {
                    s_toastPill.style.display = DisplayStyle.None;
                }).StartingIn(2500);
            }
            else if (s_showToast != null)
            {
                s_showToast(msg);
            }
            else
            {
                Debug.Log("[随身听] " + msg);
            }
        }

        // ============ 老版本自动起播逻辑（优先 Platform 单曲循环） ============
        public static void AutoPlayDefaultMusic()
        {
            TabletMusicEntry target = null;
            // 1. 优先尝试 Platform 站台
            foreach (var m in TabletMusicList)
            {
                if (m.id == "platform" && IsTabletMusicUnlocked(m))
                {
                    target = m;
                    break;
                }
            }
            // 2. 兜底第一首已解锁曲目
            if (target == null)
            {
                foreach (var m in TabletMusicList)
                {
                    if (IsTabletMusicUnlocked(m))
                    {
                        target = m;
                        break;
                    }
                }
            }

            if (target != null)
            {
                tabletPlayMode = TabletPlayMode.SingleRepeat; // 还原老版本氛围模式单曲循环
                PlayTabletTrack(target);
            }
        }

        // ============ 核心播放控制 ============
        public static void PlayTabletTrack(TabletMusicEntry m)
        {
            if (m == null) return;
            if (!IsTabletMusicUnlocked(m))
            {
                ShowToast("该音轨受沙能通讯加密，需在剧情旅途中解锁！");
                return;
            }

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
                s_currentVolume = PlayerPrefs.GetFloat("TabletMusic_Vol", 0.65f);
            }

            MusicAudioEffectsEngine.EnsureDecks(tabletMusicSource);
            tabletMusicSource = MusicAudioEffectsEngine.GetActiveDeck();

            string prevTrackId = tabletCurrentTrackId;

            float targetVol = s_isMuted ? 0f : s_currentVolume;
            MusicAudioEffectsEngine.PlayWithCrossfade(clip, targetVol);
            tabletMusicSource.loop = (tabletPlayMode == TabletPlayMode.SingleRepeat);

            tabletCurrentClip = clip;
            tabletCurrentTrackId = m.id;
            tabletCurrentTrackTitle = m.title;
            tabletCurrentTrackCategory = m.category;
            tabletIsPlaying = true;

            // 局部高效更新：只刷新受影响的旧卡片、新卡片与底栏控件，彻底杜绝全局重绘闪烁
            UpdateCardState(prevTrackId);
            UpdateCardState(m.id);
            UpdatePlayerBarUI();
        }

        public static void PlayExternalClip(string trackId, string title, string category, AudioClip clip)
        {
            if (clip == null) return;
            if (tabletMusicSource == null)
            {
                var go = new GameObject("TabletMusicPlayer");
                UnityEngine.Object.DontDestroyOnLoad(go);
                tabletMusicSource = go.AddComponent<AudioSource>();
                s_currentVolume = PlayerPrefs.GetFloat("TabletMusic_Vol", 0.65f);
            }

            string prevTrackId = tabletCurrentTrackId;

            tabletMusicSource.clip = clip;
            tabletMusicSource.volume = s_isMuted ? 0f : s_currentVolume;
            tabletMusicSource.loop = (tabletPlayMode == TabletPlayMode.SingleRepeat);
            tabletMusicSource.Play();

            tabletCurrentClip = clip;
            tabletCurrentTrackId = trackId;
            tabletCurrentTrackTitle = title;
            tabletCurrentTrackCategory = category;
            tabletIsPlaying = true;

            UpdateCardState(prevTrackId);
            UpdateCardState(trackId);
            UpdatePlayerBarUI();
        }

        public static void ToggleTabletMusicPlayPause()
        {
            if (tabletCurrentClip == null)
            {
                AutoPlayDefaultMusic();
                return;
            }

            if (MusicAudioEffectsEngine.IsPlaying())
            {
                MusicAudioEffectsEngine.Pause();
                if (tabletMusicSource != null && tabletMusicSource.isPlaying) tabletMusicSource.Pause();
                tabletIsPlaying = false;
            }
            else
            {
                MusicAudioEffectsEngine.UnPause();
                tabletMusicSource = MusicAudioEffectsEngine.GetActiveDeck();
                if (tabletMusicSource != null && !tabletMusicSource.isPlaying && !MusicAudioEffectsEngine.IsPlaying())
                {
                    tabletMusicSource.UnPause();
                }
                tabletIsPlaying = true;
            }

            UpdateCardState(tabletCurrentTrackId);
            UpdatePlayerBarUI();
        }

        public static void PlayNextTabletTrack()
        {
            if (tabletPlayMode == TabletPlayMode.Shuffle)
            {
                var unlocked = new List<TabletMusicEntry>();
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

        public static void SeekTabletMusic(float norm)
        {
            var activeDeck = MusicAudioEffectsEngine.GetActiveDeck();
            var targetSource = (activeDeck != null && activeDeck.isPlaying) ? activeDeck : tabletMusicSource;
            if (targetSource != null && tabletCurrentClip != null && tabletCurrentClip.length > 0.1f)
            {
                targetSource.time = Mathf.Clamp(norm * tabletCurrentClip.length, 0f, tabletCurrentClip.length - 0.1f);
                UpdateAudioProgress();
            }
        }

        public static string FormatAudioTime(float sec)
        {
            if (float.IsNaN(sec) || sec < 0) sec = 0;
            int m = (int)(sec / 60f);
            int s = (int)(sec % 60f);
            return $"{m}:{s:D2}";
        }

        // ============ 每帧音频进度更新与音浪动效驱动（含黑胶飞轮惯性阻尼系统） ============
        public static void UpdateAudioProgress()
        {
            // Folia 核心声音效果引擎驱动 (平滑交叉淡化、磁带抖晃 Wow & Flutter、停站自熄定时器)
            MusicAudioEffectsEngine.Update(Time.unscaledDeltaTime, s_isMuted ? 0f : s_currentVolume, onSleepFired: () => {
                tabletIsPlaying = false;
                ShowToast(" 随身听停站自熄：已进入睡眠省电模式");
                UpdatePlayerBarUI();
            });

            // 物理飞轮惯性阻尼驱动黑胶唱盘旋转（启动加速平滑，暂停带摩擦阻尼滑行缓冲）
            float targetVel = tabletIsPlaying ? 1.25f : 0f;
            float accelRate = tabletIsPlaying ? 2.6f : 0.75f;
            s_vinylAngularVelocity = Mathf.MoveTowards(s_vinylAngularVelocity, targetVel, Time.unscaledDeltaTime * accelRate);
            if (s_vinylAngularVelocity > 0.001f)
            {
                s_nowPlayingRotation = (s_nowPlayingRotation + s_vinylAngularVelocity) % 360f;
                if (s_nowPlayingVinylDisc != null)
                {
                    s_nowPlayingVinylDisc.style.rotate = new Rotate(Angle.Degrees(s_nowPlayingRotation));
                }
            }

            // 1. Folia 风格黑胶外圈流光光晕呼吸动效
            if (s_nowPlayingHaloGlow != null)
            {
                float baseA = tabletIsPlaying ? 0.38f : 0.08f;
                float breath = tabletIsPlaying ? (Mathf.Sin(s_waveAnimTime * 2.2f) * 0.16f) : 0f;
                float curAlpha = Mathf.Clamp01(baseA + breath);
                Color glowCol = new Color(0.83f, 0.66f, 0.26f, curAlpha);
                s_nowPlayingHaloGlow.style.borderTopColor = glowCol;
                s_nowPlayingHaloGlow.style.borderBottomColor = glowCol;
                s_nowPlayingHaloGlow.style.borderLeftColor = glowCol;
                s_nowPlayingHaloGlow.style.borderRightColor = glowCol;
            }

            // 2. 双联先民动圈模拟电平表（VU Meter）指针动态驱动
            if (s_vuMeterHandle != null)
            {
                float lEnergy = 0f;
                float rEnergy = 0f;
                if (tabletIsPlaying)
                {
                    float waveL = Mathf.Sin(s_waveAnimTime * 4.4f) * 0.5f + 0.5f;
                    float waveR = Mathf.Cos(s_waveAnimTime * 3.8f + 1.2f) * 0.5f + 0.5f;
                    lEnergy = Mathf.Clamp01(waveL * 0.70f + 0.20f);
                    rEnergy = Mathf.Clamp01(waveR * 0.72f + 0.18f);
                }
                s_vuMeterHandle.UpdateLevels(lEnergy, rEnergy, Time.unscaledDeltaTime);
            }

            if (tabletIsPlaying && (tabletMusicSource != null || MusicAudioEffectsEngine.GetActiveDeck() != null) && tabletCurrentClip != null)
            {
                float curTime = 0f;
                float totalLen = tabletCurrentClip.length;

                var activeDeck = MusicAudioEffectsEngine.GetActiveDeck();
                var currentSource = (activeDeck != null && activeDeck.isPlaying && activeDeck.clip != null) ? activeDeck : tabletMusicSource;
                if (currentSource != null && currentSource.isPlaying && currentSource.clip != null)
                {
                    curTime = currentSource.time;
                    totalLen = currentSource.clip.length;
                }
                else if (currentSource != null && currentSource.clip != null)
                {
                    curTime = currentSource.time;
                    totalLen = currentSource.clip.length;
                    // 若处于自然播放状态且已到达曲目终点，自动接续下一首
                    if (tabletIsPlaying && curTime >= totalLen - 0.3f && tabletPlayMode != TabletPlayMode.SingleRepeat)
                    {
                        PlayNextTabletTrack();
                        return;
                    }
                }

                    if (!tabletProgressScrubbing && totalLen > 0.1f)
                    {
                        float pct = Mathf.Clamp01(curTime / totalLen) * 100f;
                        if (tabletProgressFill != null) tabletProgressFill.style.width = Length.Percent(pct);
                        if (tabletProgressKnob != null) tabletProgressKnob.style.left = Length.Percent(pct);
                    }
                    if (tabletCurTimeLbl != null) tabletCurTimeLbl.text = FormatAudioTime(curTime);

                    // 驱动 Phase 1 专业底栏进度与确定性伪 VU 指针
                    if (s_playerBarHandle != null)
                    {
                        if (!s_playerBarHandle.isScrubbing && totalLen > 0.1f)
                        {
                            float pct = Mathf.Clamp01(curTime / totalLen) * 100f;
                            if (s_playerBarHandle.progressFill != null) s_playerBarHandle.progressFill.style.width = Length.Percent(pct);
                            if (s_playerBarHandle.progressKnob != null) s_playerBarHandle.progressKnob.style.left = Length.Percent(pct);
                            if (s_playerBarHandle.curTimeLbl != null) s_playerBarHandle.curTimeLbl.text = FormatAudioTime(curTime);
                        }
                        else if (s_playerBarHandle.isScrubbing && totalLen > 0.1f)
                        {
                            if (s_playerBarHandle.curTimeLbl != null) s_playerBarHandle.curTimeLbl.text = FormatAudioTime(s_playerBarHandle.scrubNormalized * totalLen);
                        }
                        if (s_playerBarHandle.totalTimeLbl != null && totalLen > 0.1f) s_playerBarHandle.totalTimeLbl.text = FormatAudioTime(totalLen);
                        if (s_playerBarHandle.vuNeedle != null)
                        {
                            MusicPlayerBarView.UpdateVuNeedle(s_playerBarHandle.vuNeedle, tabletIsPlaying, curTime);
                        }
                    }

                    // 驱动 Phase 3 右侧栏 Folia 歌词流光动态滚动 (逐行自适应高亮与淡出)
                    if (s_nowPlayingSidebarHandle != null && totalLen > 0.1f)
                    {
                        MusicNowPlayingSidebarView.UpdateLyricsProgress(s_nowPlayingSidebarHandle, Mathf.Clamp01(curTime / totalLen));
                    }

                    // 驱动跳动音浪（多频段谐波拟合算法，极具真实频谱韵律与呼吸感）
                    s_waveAnimTime += 0.20f;
                    for (int i = 0; i < s_waveBars.Count; i++)
                    {
                        var bar = s_waveBars[i];
                        if (bar != null)
                        {
                            int phase = i % 4;
                            float wave = (Mathf.Sin(s_waveAnimTime * 3.6f + phase * 1.6f) * 0.45f +
                                          Mathf.Cos(s_waveAnimTime * 6.2f + phase * 2.4f) * 0.35f +
                                          Mathf.Sin(s_waveAnimTime * 9.0f + phase * 3.2f) * 0.20f + 1f);
                            float h = 4f + wave * 5.2f;
                            bar.style.height = h;
                        }
                    }

                    // 播放结束自动切下一首
                    if (curTime >= totalLen - 0.25f && totalLen > 0.5f)
                    {
                        if (tabletPlayMode == TabletPlayMode.SingleRepeat)
                        {
                            if (activeDeck != null && activeDeck.isPlaying)
                            {
                                activeDeck.time = 0f;
                            }
                            else if (tabletMusicSource != null)
                            {
                                tabletMusicSource.time = 0f;
                                tabletMusicSource.Play();
                            }
                        }
                        else
                        {
                            PlayNextTabletTrack();
                        }
                    }
            }
            else
            {
                // 暂停时音浪平息回落至平静基准线
                for (int i = 0; i < s_waveBars.Count; i++)
                {
                    if (s_waveBars[i] != null) s_waveBars[i].style.height = 3f;
                }
            }
        }

        private static TabletMusicEntry GetNextTrackEntry()
        {
            if (TabletMusicList == null || TabletMusicList.Length == 0) return null;
            int curIdx = -1;
            for (int i = 0; i < TabletMusicList.Length; i++)
            {
                if (TabletMusicList[i].id == tabletCurrentTrackId)
                {
                    curIdx = i;
                    break;
                }
            }
            for (int step = 1; step <= TabletMusicList.Length; step++)
            {
                int nextIdx = (curIdx + step) % TabletMusicList.Length;
                var cand = TabletMusicList[nextIdx];
                if (IsTabletMusicUnlocked(cand)) return cand;
            }
            return null;
        }

        // ============ 局部 UI 更新方法（实现 0 闪烁交互） ============
        private static void UpdateCardState(string trackId)
        {
            if (string.IsNullOrEmpty(trackId)) return;
            if (s_cardBindings.TryGetValue(trackId, out var b))
            {
                bool isCur = (trackId == tabletCurrentTrackId && tabletIsPlaying);
                b.cardElement.style.backgroundColor = isCur ? new Color(0.24f, 0.20f, 0.14f, 1f) : new Color(0.16f, 0.17f, 0.21f, 0.85f);
                MusicIconHelper.ApplyIndustrialBevel(b.cardElement, isCur, isCur);

                b.titleLabel.style.color = isCur ? new Color(0.96f, 0.82f, 0.45f, 1f) : new Color(0.92f, 0.90f, 0.85f, 1f);
                string descText = isCur ? "▶ 正在巡回播放中..." : (!string.IsNullOrEmpty(b.entry?.desc) ? b.entry.desc : "铁道原声品质");
                if (b.condLabel != null)
                {
                    b.condLabel.text = descText;
                    b.condLabel.style.color = isCur ? new Color(0.96f, 0.82f, 0.45f, 1f) : new Color(0.68f, 0.65f, 0.60f, 1f);
                }

                if (b.playBtn != null)
                {
                    b.playBtn.Clear();
                    b.playBtn.Add(isCur ? MusicIconHelper.CreatePauseIcon(2.5f, 11f, 3.5f, Color.white) : MusicIconHelper.CreatePlayIcon(11f, Color.white));
                    b.playBtn.style.backgroundColor = isCur ? new Color(0.83f, 0.66f, 0.26f, 1f) : new Color(0.24f, 0.25f, 0.30f, 1f);
                    MusicIconHelper.ApplyIndustrialBevel(b.playBtn, isCur, isCur);
                }

                if (b.trackIndexLabel != null)
                {
                    b.trackIndexLabel.style.color = isCur ? new Color(0.96f, 0.82f, 0.45f, 1f) : new Color(0.55f, 0.53f, 0.48f, 1f);
                }

                if (b.waveBox != null)
                {
                    b.waveBox.style.display = isCur ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private static void UpdateFavoriteVisuals(string trackId)
        {
            bool isFav = IsTabletMusicFavorited(trackId);
            if (s_cardBindings.TryGetValue(trackId, out var b) && b.heartBtn != null)
            {
                b.heartBtn.text = isFav ? "♥" : "♡";
                b.heartBtn.style.color = isFav ? new Color(0.92f, 0.25f, 0.30f, 1f) : new Color(0.60f, 0.60f, 0.65f, 1f);
            }
            if (trackId == tabletCurrentTrackId && tabletFavToggleBtn != null)
            {
                tabletFavToggleBtn.text = isFav ? "♥" : "♡";
                tabletFavToggleBtn.style.color = isFav ? new Color(0.92f, 0.25f, 0.30f, 1f) : new Color(0.60f, 0.60f, 0.65f, 1f);
            }
        }

        private static void CyclePlayMode()
        {
            if (tabletPlayMode == TabletPlayMode.Sequential)
            {
                tabletPlayMode = TabletPlayMode.SingleRepeat;
                ShowToast("【调频模式】单曲回旋 · REPEAT ONE");
            }
            else if (tabletPlayMode == TabletPlayMode.SingleRepeat)
            {
                tabletPlayMode = TabletPlayMode.Shuffle;
                ShowToast("【调频模式】穿梭随机 · SHUFFLE");
            }
            else
            {
                tabletPlayMode = TabletPlayMode.Sequential;
                ShowToast("【调频模式】全轨循环 · REPEAT ALL");
            }

            if (tabletMusicSource != null)
            {
                tabletMusicSource.loop = (tabletPlayMode == TabletPlayMode.SingleRepeat);
            }

            var activeDeck = MusicAudioEffectsEngine.GetActiveDeck();
            if (activeDeck != null)
            {
                activeDeck.loop = (tabletPlayMode == TabletPlayMode.SingleRepeat);
            }

            UpdatePlayerBarUI();
        }

        public static void UpdatePlayerBarUI()
        {
            if (tabletPlayerTitleLbl != null) tabletPlayerTitleLbl.text = string.IsNullOrEmpty(tabletCurrentTrackTitle) ? "未选择播放曲目" : tabletCurrentTrackTitle;
            if (tabletPlayerSubLbl != null) tabletPlayerSubLbl.text = string.IsNullOrEmpty(tabletCurrentTrackCategory) ? "随身听已就绪 · 点击上方曲目开启调频" : tabletCurrentTrackCategory + " · 铁道原声品质";
            if (tabletCurrentClip != null && tabletTotalTimeLbl != null) tabletTotalTimeLbl.text = FormatAudioTime(tabletCurrentClip.length);
            
            if (tabletPlayToggleBtn != null)
            {
                tabletPlayToggleBtn.Clear();
                tabletPlayToggleBtn.Add(tabletIsPlaying ? MusicIconHelper.CreatePauseIcon(2.8f, 13f, 4.5f, Color.white) : MusicIconHelper.CreatePlayIcon(13f, Color.white));
            }

            if (tabletMiniCover != null)
            {
                Texture2D cov = MusicAlbumArtResolver.GetAlbumCover(tabletCurrentTrackId, tabletCurrentTrackCategory);
                if (cov != null)
                {
                    tabletMiniCover.style.backgroundImage = new StyleBackground(cov);
                    tabletMiniCover.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                    if (tabletMiniDiscIcon != null) tabletMiniDiscIcon.style.display = DisplayStyle.None;
                }
                else
                {
                    tabletMiniCover.style.backgroundImage = StyleKeyword.Null;
                    tabletMiniCover.style.backgroundColor = tabletIsPlaying ? new Color(0.83f, 0.66f, 0.26f, 0.3f) : new Color(0.20f, 0.21f, 0.26f, 1f);
                    if (tabletMiniDiscIcon != null)
                    {
                        tabletMiniDiscIcon.style.display = DisplayStyle.Flex;
                        tabletMiniDiscIcon.text = tabletIsPlaying ? "♫" : "◎";
                        tabletMiniDiscIcon.style.color = tabletIsPlaying ? new Color(0.96f, 0.82f, 0.45f, 1f) : new Color(0.70f, 0.68f, 0.62f, 1f);
                    }
                }
            }

            if (tabletFavToggleBtn != null)
            {
                bool isFav = IsTabletMusicFavorited(tabletCurrentTrackId);
                tabletFavToggleBtn.text = isFav ? "♥" : "♡";
                tabletFavToggleBtn.style.color = isFav ? new Color(0.92f, 0.25f, 0.30f, 1f) : new Color(0.60f, 0.60f, 0.65f, 1f);
            }

            if (tabletModeToggleBtn != null && tabletModeToggleBtn.parent != null)
            {
                var parent = tabletModeToggleBtn.parent;
                int idx = parent.IndexOf(tabletModeToggleBtn);
                int modeVal = (tabletPlayMode == TabletPlayMode.SingleRepeat) ? 1 : 0;
                var newMode = MusicIconHelper.CreateUnifiedPlayModeControl(modeVal, CyclePlayMode);
                newMode.style.marginRight = 12;
                parent.Insert(idx, newMode);
                parent.Remove(tabletModeToggleBtn);
                tabletModeToggleBtn = newMode;
            }

            // 同步刷新 Phase 1 专业底栏
            if (s_playerBarHandle != null)
            {
                if (s_playerBarHandle.trackTitle != null)
                    s_playerBarHandle.trackTitle.text = string.IsNullOrEmpty(tabletCurrentTrackTitle) ? "未选择播放曲目" : tabletCurrentTrackTitle;
                if (s_playerBarHandle.trackCategory != null)
                    s_playerBarHandle.trackCategory.text = string.IsNullOrEmpty(tabletCurrentTrackCategory) ? "随身听已就绪 · 点击曲目开启调频" : tabletCurrentTrackCategory + " · 铁道原声品质";
                if (s_playerBarHandle.totalTimeLbl != null && tabletCurrentClip != null)
                    s_playerBarHandle.totalTimeLbl.text = FormatAudioTime(tabletCurrentClip.length);
                if (s_playerBarHandle.favBtn != null)
                {
                    bool isFav = IsTabletMusicFavorited(tabletCurrentTrackId);
                    s_playerBarHandle.favBtn.text = isFav ? "♥" : "♡";
                    s_playerBarHandle.favBtn.style.color = isFav ? new Color(0.92f, 0.25f, 0.30f, 1f) : new Color(0.68f, 0.65f, 0.60f, 1f);
                }

                // 统一精准刷新播控按键状态（播放/暂停、单键三态模式轮转：0顺序, 1单曲, 2随机）
                int modeVal = 0;
                if (tabletPlayMode == TabletPlayMode.SingleRepeat) modeVal = 1;
                else if (tabletPlayMode == TabletPlayMode.Shuffle) modeVal = 2;

                MusicPlayerBarView.UpdateControlsState(
                    s_playerBarHandle,
                    tabletIsPlaying,
                    tabletPlayMode == TabletPlayMode.Shuffle,
                    modeVal);
                MusicPlayerBarView.UpdateVolumeMuteVisuals(s_playerBarHandle, s_isMuted || s_currentVolume <= 0.02f, s_currentVolume);
                if (s_playerBarHandle.sleepTimerBtn != null)
                {
                    s_playerBarHandle.sleepTimerBtn.text = " " + MusicAudioEffectsEngine.FormatSleepTimeRemaining();
                    s_playerBarHandle.sleepTimerBtn.style.color = MusicAudioEffectsEngine.IsSleepTimerActive ? new Color(0.96f, 0.82f, 0.45f, 1f) : new Color(0.68f, 0.65f, 0.60f, 1f);
                }
                if (s_playerBarHandle.radioFilterBtn != null)
                {
                    string fName = MusicAudioEffectsEngine.CurrentFilterMode == MusicAudioEffectsEngine.RadioFilterMode.Clean ? " 原声" :
                        (MusicAudioEffectsEngine.CurrentFilterMode == MusicAudioEffectsEngine.RadioFilterMode.Locomotive ? " 机车" : " 先民");
                    s_playerBarHandle.radioFilterBtn.text = fName;
                }
            }

            // 同步更新右侧“正在播放”大视窗
            if (s_nowPlayingTitleLbl != null) s_nowPlayingTitleLbl.text = string.IsNullOrEmpty(tabletCurrentTrackTitle) ? "未选择播放曲目" : tabletCurrentTrackTitle;
            if (s_nowPlayingCategoryLbl != null) s_nowPlayingCategoryLbl.text = string.IsNullOrEmpty(tabletCurrentTrackCategory) ? "铁道调频电波 · 待机" : (tabletCurrentTrackCategory + " · 先民工业原声");

            string curLore = "残阳如血，锈轨伸向旧时代的沙海深处。蒸汽与黄沙交织的岁月中，随身听记录下每一个在铁道上奋力前行的身影。";
            if (!string.IsNullOrEmpty(tabletCurrentTrackId))
            {
                foreach (var m in TabletMusicList)
                {
                    if (m.id == tabletCurrentTrackId)
                    {
                        if (!string.IsNullOrEmpty(m.desc)) curLore = m.desc + "。在漫长漫长的铁道纪元里，这段旋律承载着无数开拓者的希望与执着。";
                        break;
                    }
                }
            }
            if (s_nowPlayingLoreLbl != null) s_nowPlayingLoreLbl.text = curLore;

            if (s_nowPlayingNextTitleLbl != null)
            {
                var nextTrack = GetNextTrackEntry();
                s_nowPlayingNextTitleLbl.text = nextTrack != null ? nextTrack.title : "播放队列已尽";
            }

            if (s_nowPlayingSidebarHandle != null && s_nowPlayingSidebarHandle.coverElement != null)
            {
                Texture2D cov = MusicAlbumArtResolver.GetAlbumCover(tabletCurrentTrackId, tabletCurrentTrackCategory);
                s_nowPlayingSidebarHandle.coverElement.style.backgroundImage = new StyleBackground(cov);
            }
        }

        // ==================== 中间主舞台与全功能子视图 ====================
        // 注：发现主页、原声全集、收藏、本地磁带、时代云端已完整解耦拆分至 MusicMainStageView.cs

        // ============ 整体界面构建 ============
        public static void BuildMusicApp(VisualElement win, Action<string> showToast = null, Action onClose = null)
        {
            if (showToast != null) s_showToast = showToast;
            if (onClose != null) s_onClose = onClose;
            s_activeWindow = win;
            win.Clear();
            s_cardBindings.Clear();
            s_waveBars.Clear();

            // 根容器（垂直流式排版，顶栏控制条 + 上部三栏 + 底部常驻播放条）
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1;
            root.style.backgroundColor = new Color(0.10f, 0.11f, 0.13f, 1f); // 工业墨黑沉浸
            win.Add(root);

            // 顶栏深色沉浸控制条（◀ 返回主界面 + 居中电台名称 + ✕ 关闭）
            var topHeader = new VisualElement();
            topHeader.style.height = 36;
            topHeader.style.backgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
            topHeader.style.borderBottomWidth = 1;
            topHeader.style.borderBottomColor = new Color(0.24f, 0.23f, 0.20f, 0.7f);
            topHeader.style.flexDirection = FlexDirection.Row;
            topHeader.style.alignItems = Align.Center;
            topHeader.style.justifyContent = Justify.SpaceBetween;
            topHeader.style.paddingLeft = 14; topHeader.style.paddingRight = 14;
            root.Add(topHeader);

            var backBtn = new Button();
            backBtn.text = "◀ 返回桌面";
            backBtn.style.height = 24; backBtn.style.paddingLeft = 8; backBtn.style.paddingRight = 8;
            backBtn.style.fontSize = 10; backBtn.style.unityFontDefinition = Fd();
            backBtn.style.backgroundColor = new Color(0.18f, 0.19f, 0.24f, 1f);
            backBtn.style.color = new Color(0.96f, 0.82f, 0.45f, 1f);
            backBtn.style.borderTopLeftRadius = 12; backBtn.style.borderTopRightRadius = 12;
            backBtn.style.borderBottomLeftRadius = 12; backBtn.style.borderBottomRightRadius = 12;
            backBtn.style.borderTopWidth = 1; backBtn.style.borderBottomWidth = 1;
            backBtn.style.borderLeftWidth = 1; backBtn.style.borderRightWidth = 1;
            backBtn.style.borderTopColor = new Color(0.35f, 0.33f, 0.28f, 0.8f);
            backBtn.style.borderBottomColor = backBtn.style.borderTopColor;
            backBtn.style.borderLeftColor = backBtn.style.borderTopColor;
            backBtn.style.borderRightColor = backBtn.style.borderTopColor;
            MusicIconHelper.ApplyPhysicalMicroInteraction(backBtn, onClick: () => s_onClose?.Invoke(), enableFloat: true, isBrass: false);
            topHeader.Add(backBtn);

            var headerTitle = new Label("随身听 · 铁道内网广播台 (WALKMAN) · 先民工业原声");
            headerTitle.style.fontSize = 11;
            headerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerTitle.style.color = new Color(0.75f, 0.72f, 0.65f, 1f);
            headerTitle.style.unityFontDefinition = Fd();
            topHeader.Add(headerTitle);

            var closeBtn = new Button();
            closeBtn.text = "✕";
            closeBtn.style.width = 24; closeBtn.style.height = 24;
            closeBtn.style.fontSize = 12; closeBtn.style.unityFontDefinition = Fd();
            closeBtn.style.backgroundColor = Color.clear;
            closeBtn.style.color = new Color(0.70f, 0.68f, 0.62f, 1f);
            closeBtn.style.borderTopWidth = 0; closeBtn.style.borderBottomWidth = 0;
            closeBtn.style.borderLeftWidth = 0; closeBtn.style.borderRightWidth = 0;
            MusicIconHelper.ApplyPhysicalMicroInteraction(closeBtn, onClick: () => s_onClose?.Invoke(), enableFloat: false, isBrass: false);
            topHeader.Add(closeBtn);

            // ===== 上半部分：现代三栏视窗 (左曲库 210px + 中内容 flexGrow + 右播放 230px) =====
            var upperContainer = new VisualElement();
            upperContainer.style.flexDirection = FlexDirection.Row;
            upperContainer.style.flexGrow = 1;
            upperContainer.style.overflow = Overflow.Hidden;
            root.Add(upperContainer);

            // 1. 左侧音乐库侧边栏（220px 纯正 Spotify 4 大导航 + 铁道调频语汇，单一状态机彻底消灭串台）
            s_sidebarHandle = MusicSidebarNavView.BuildSidebar(
                upperContainer,
                s_currentView,
                onViewChanged: (newView) =>
                {
                    s_currentView = newView;
                    if (newView == MusicSidebarNavView.MusicViewId.RadioAuto)
                    {
                        ShowToast(" 已接通「先民时代电台」全自动轮播！");
                        tabletPlayMode = TabletPlayMode.Sequential;
                        PlayTabletTrack(TabletMusicList[0]);
                    }
                    BuildMusicApp(win);
                });

            // 2. 中间主视口滚动容器 (集成 Folia 风格自适应极光氛围光晕衬底)
            var centerArea = new VisualElement();
            centerArea.style.flexGrow = 1;
            centerArea.style.flexDirection = FlexDirection.Column;
            centerArea.style.position = Position.Relative;
            centerArea.style.overflow = Overflow.Hidden;
            Color dominant = MusicAlbumArtResolver.GetDominantColor(tabletCurrentTrackId, tabletCurrentTrackCategory);
            centerArea.style.backgroundColor = Color.Lerp(new Color(0.12f, 0.13f, 0.16f, 1f), dominant, 0.20f);
            upperContainer.Add(centerArea);

            var auroraGlow = new VisualElement();
            auroraGlow.style.position = Position.Absolute;
            auroraGlow.style.top = 0; auroraGlow.style.left = 0; auroraGlow.style.right = 0;
            auroraGlow.style.height = 140;
            auroraGlow.style.backgroundColor = dominant;
            auroraGlow.style.opacity = 0.28f;
            auroraGlow.pickingMode = PickingMode.Ignore;
            centerArea.Add(auroraGlow);

            // 右上角浮动调频信标小胶囊（代替丑陋粗长条Toast）
            s_toastPill = new VisualElement();
            s_toastPill.style.position = Position.Absolute;
            s_toastPill.style.top = 10; s_toastPill.style.right = 16;
            s_toastPill.style.height = 28;
            s_toastPill.style.paddingLeft = 12; s_toastPill.style.paddingRight = 12;
            s_toastPill.style.backgroundColor = new Color(0.10f, 0.10f, 0.12f, 0.95f);
            s_toastPill.style.borderTopWidth = 1.2f; s_toastPill.style.borderBottomWidth = 1.2f;
            s_toastPill.style.borderLeftWidth = 1.2f; s_toastPill.style.borderRightWidth = 1.2f;
            s_toastPill.style.borderTopColor = new Color(0.83f, 0.66f, 0.26f, 0.9f);
            s_toastPill.style.borderBottomColor = s_toastPill.style.borderTopColor;
            s_toastPill.style.borderLeftColor = s_toastPill.style.borderTopColor;
            s_toastPill.style.borderRightColor = s_toastPill.style.borderTopColor;
            s_toastPill.style.borderTopLeftRadius = 14; s_toastPill.style.borderTopRightRadius = 14;
            s_toastPill.style.borderBottomLeftRadius = 14; s_toastPill.style.borderBottomRightRadius = 14;
            s_toastPill.style.flexDirection = FlexDirection.Row;
            s_toastPill.style.alignItems = Align.Center;
            s_toastPill.style.display = DisplayStyle.None;
            s_toastPill.pickingMode = PickingMode.Ignore;
            centerArea.Add(s_toastPill);
            s_toastPill.BringToFront();

            var beaconDot = new VisualElement();
            beaconDot.style.width = 6; beaconDot.style.height = 6;
            beaconDot.style.backgroundColor = new Color(0.95f, 0.78f, 0.35f, 1f);
            beaconDot.style.borderTopLeftRadius = 3; beaconDot.style.borderTopRightRadius = 3;
            beaconDot.style.borderBottomLeftRadius = 3; beaconDot.style.borderBottomRightRadius = 3;
            beaconDot.style.marginRight = 6;
            beaconDot.pickingMode = PickingMode.Ignore;
            s_toastPill.Add(beaconDot);

            s_toastLabel = new Label("");
            s_toastLabel.style.fontSize = 10;
            s_toastLabel.style.color = new Color(0.96f, 0.92f, 0.85f, 1f);
            s_toastLabel.style.unityFontDefinition = Fd();
            s_toastLabel.pickingMode = PickingMode.Ignore;
            s_toastPill.Add(s_toastLabel);

            // 主体滚动容器（垂直模式，隐藏水平滚动条，美化垂直滚动条）
            var contentScroll = new ScrollView();
            contentScroll.mode = ScrollViewMode.Vertical;
            contentScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            contentScroll.style.flexGrow = 1;
            contentScroll.style.paddingLeft = 16; contentScroll.style.paddingRight = 16;
            contentScroll.style.paddingTop = 14; contentScroll.style.paddingBottom = 16;
            centerArea.Add(contentScroll);
            MusicIconHelper.BeautifyScrollView(contentScroll);

            // 2. 中间主舞台分流渲染（委托至 MusicMainStageView）
            MusicMainStageView.RenderStage(contentScroll, win, s_currentView);

            // 3. 右侧正在播放大视窗（委托至 MusicNowPlayingSidebarView 独立抽屉）
            var rightSidebar = new VisualElement();
            upperContainer.Add(rightSidebar);
            var nextTrack = GetNextTrackEntry();
            s_nowPlayingSidebarHandle = MusicNowPlayingSidebarView.BuildSidebar(
                rightSidebar,
                tabletCurrentTrackId,
                string.IsNullOrEmpty(tabletCurrentTrackTitle) ? "未选择播放曲目" : tabletCurrentTrackTitle,
                string.IsNullOrEmpty(tabletCurrentTrackCategory) ? "随车电台" : tabletCurrentTrackCategory,
                nextTrack != null ? nextTrack.title : "待播队列为空",
                tabletIsPlaying,
                tabletShowLyrics,
                onLyricSeek: (norm) => SeekTabletMusic(norm),
                onPlayNext: () => PlayNextTabletTrack(),
                onOpenPonderArchive: null
            );

            // ===== 底部全局复古收音机专业播放控制台 (接入 MusicPlayerBarView) =====
            s_playerBarHandle = MusicPlayerBarView.BuildPlayerBar(
                root,
                onPlayToggle: ToggleTabletMusicPlayPause,
                onPrev: PlayPrevTabletTrack,
                onNext: PlayNextTabletTrack,
                onModeCycle: CyclePlayMode,
                onShuffle: null,
                onFavToggle: () => {
                    if (!string.IsNullOrEmpty(tabletCurrentTrackId))
                    {
                        ToggleTabletMusicFavorite(tabletCurrentTrackId);
                    }
                },
                onSeek: SeekTabletMusic,
                onVolChange: (vol) => {
                    s_currentVolume = vol;
                    s_isMuted = false;
                    if (tabletMusicSource != null) tabletMusicSource.volume = s_currentVolume;
                    PlayerPrefs.SetFloat("TabletMusic_Vol", s_currentVolume);
                    PlayerPrefs.Save();
                },
                onVolMuteToggle: () => {
                    s_isMuted = !s_isMuted;
                    if (tabletMusicSource != null)
                    {
                        tabletMusicSource.volume = s_isMuted ? 0f : s_currentVolume;
                    }
                    ShowToast(s_isMuted ? "随身听已静音" : $"音量已恢复 ({(int)(s_currentVolume * 100)}%)");
                    UpdatePlayerBarUI();
                },
                onLyricsToggle: () => {
                    tabletShowLyrics = !tabletShowLyrics;
                    ShowToast(tabletShowLyrics ? "歌词视窗已开启" : "歌词视窗已折叠");
                    if (win != null) BuildMusicApp(win);
                },
                onCycleSleepTimer: () => {
                    int next = (int)MusicAudioEffectsEngine.CurrentSleepOption switch
                    {
                        0 => 15,
                        15 => 30,
                        30 => 45,
                        45 => 999,
                        _ => 0
                    };
                    float remaining = (tabletCurrentClip != null && tabletMusicSource != null) ? (tabletCurrentClip.length - tabletMusicSource.time) : 0f;
                    MusicAudioEffectsEngine.SetSleepTimer((MusicAudioEffectsEngine.SleepTimerOption)next, remaining);
                    string tip = next == 0 ? "睡眠定时器已关闭" : (next == 999 ? "已设为：当前音轨播完后停站自熄" : $"已设为：{next} 分钟后停站自熄");
                    ShowToast(" " + tip);
                    UpdatePlayerBarUI();
                },
                onCycleRadioFilter: () => {
                    int next = ((int)MusicAudioEffectsEngine.CurrentFilterMode + 1) % 3;
                    MusicAudioEffectsEngine.CurrentFilterMode = (MusicAudioEffectsEngine.RadioFilterMode)next;
                    string name = next == 0 ? "纯净现代原声" : (next == 1 ? "工务机车电台 (轻度微晃+风沙)" : "先民真空电台 (经典电子管微晃)");
                    ShowToast(" 调频质感已切换为: " + name);
                    UpdatePlayerBarUI();
                },
                onToggleImmersive: () => {
                    s_isImmersiveMode = !s_isImmersiveMode;
                    ShowToast(s_isImmersiveMode ? "已开启 Folia 全屏沉浸巨幕模式" : "已返回标准控制台视图");
                    if (win != null) BuildMusicApp(win);
                }
            );

            // 保持旧字段引用兼容，防止旧代码出现空引用
            tabletMiniCover = s_playerBarHandle.miniCover;
            tabletMiniDiscIcon = s_playerBarHandle.miniCoverIcon;
            tabletPlayerTitleLbl = s_playerBarHandle.trackTitle;
            tabletPlayerSubLbl = s_playerBarHandle.trackCategory;
            tabletFavToggleBtn = s_playerBarHandle.favBtn;
            tabletPlayToggleBtn = s_playerBarHandle.playToggleBtn;
            tabletVolBtn = s_playerBarHandle.volBtn;
            tabletVolSlider = s_playerBarHandle.volSlider;
            tabletCurTimeLbl = s_playerBarHandle.curTimeLbl;
            tabletProgressTrack = s_playerBarHandle.progressTrack;
            tabletProgressFill = s_playerBarHandle.progressFill;
            tabletProgressKnob = s_playerBarHandle.progressKnob;

            // ===== 挂载 UI Toolkit 独立每帧/定时驱动循环（30FPS 毫秒级自驱动，彻底解决进度条不动） =====
            s_progressScheduledItem?.Pause();
            s_progressScheduledItem = win.schedule.Execute(UpdateAudioProgress).Every(33);

            UpdatePlayerBarUI();

            // ===== 打开随身听默认起播机制检查 =====
            if (!tabletIsPlaying && (tabletMusicSource == null || !tabletMusicSource.isPlaying))
            {
                AutoPlayDefaultMusic();
            }

            // ===== 首次使用自动呼出功能向导 =====
            if (PlayerPrefs.GetInt("TabletMusic_GuideSeen", 0) == 0)
            {
                PlayerPrefs.SetInt("TabletMusic_GuideSeen", 1);
                PlayerPrefs.Save();
                ShowOnboardingGuideModal(win);
            }
        }


        // ==================== Folia 全屏沉浸黑胶视听巨幕 ====================
        private static void BuildImmersiveStageView(VisualElement root, VisualElement win)
        {
            var stage = new VisualElement();
            stage.style.flexGrow = 1;
            stage.style.flexDirection = FlexDirection.Column;
            stage.style.backgroundColor = new Color(0.07f, 0.06f, 0.06f, 1f);
            stage.style.position = Position.Relative;
            root.Add(stage);

            // 1. 动态自适应色彩光晕
            var glow = new VisualElement();
            glow.style.position = Position.Absolute;
            glow.style.left = Length.Percent(8);
            glow.style.top = Length.Percent(12);
            glow.style.width = Length.Percent(48);
            glow.style.height = Length.Percent(70);
            Color domColor = MusicAlbumArtResolver.GetDominantColor(tabletCurrentTrackId, tabletCurrentTrackCategory);
            glow.style.backgroundColor = new Color(domColor.r, domColor.g, domColor.b, 0.22f);
            glow.style.borderTopLeftRadius = 180; glow.style.borderTopRightRadius = 180;
            glow.style.borderBottomLeftRadius = 180; glow.style.borderBottomRightRadius = 180;
            glow.pickingMode = PickingMode.Ignore;
            stage.Add(glow);

            // 2. 顶栏返回与状态
            var topBar = new VisualElement();
            topBar.style.height = 40;
            topBar.style.flexDirection = FlexDirection.Row;
            topBar.style.justifyContent = Justify.SpaceBetween;
            topBar.style.alignItems = Align.Center;
            topBar.style.paddingLeft = 20; topBar.style.paddingRight = 20;
            stage.Add(topBar);

            var exitBtn = new Button(() =>
            {
                s_isImmersiveMode = false;
                if (win != null) BuildMusicApp(win);
            }) { text = "⛶ 退出沉浸模式" };
            exitBtn.style.height = 24; exitBtn.style.fontSize = 10; exitBtn.style.unityFontDefinition = Fd();
            exitBtn.style.backgroundColor = new Color(0.20f, 0.17f, 0.14f, 0.85f);
            exitBtn.style.color = MusicIconHelper.BrassGold;
            exitBtn.style.borderTopLeftRadius = 12; exitBtn.style.borderTopRightRadius = 12;
            exitBtn.style.borderBottomLeftRadius = 12; exitBtn.style.borderBottomRightRadius = 12;
            exitBtn.style.borderTopWidth = 1; exitBtn.style.borderBottomWidth = 1;
            exitBtn.style.borderLeftWidth = 1; exitBtn.style.borderRightWidth = 1;
            exitBtn.style.borderTopColor = new Color(0.83f, 0.66f, 0.26f, 0.6f);
            exitBtn.style.borderBottomColor = exitBtn.style.borderTopColor;
            exitBtn.style.borderLeftColor = exitBtn.style.borderTopColor;
            exitBtn.style.borderRightColor = exitBtn.style.borderTopColor;
            MusicIconHelper.SetHandCursor(exitBtn);
            topBar.Add(exitBtn);

            var modeTag = new Label("FM-88.5 铁道解调广播 · Folia 沉浸黑胶视听巨幕");
            modeTag.style.fontSize = 11;
            modeTag.style.color = new Color(0.80f, 0.75f, 0.68f, 0.85f);
            modeTag.style.unityFontDefinition = Fd();
            topBar.Add(modeTag);

            var rightPill = MusicIconHelper.CreatePlatformCapsule("[IMMERSIVE]", MusicIconHelper.BrassGold);
            topBar.Add(rightPill);

            // 3. 左右分栏巨幕
            var mainContent = new VisualElement();
            mainContent.style.flexGrow = 1;
            mainContent.style.flexDirection = FlexDirection.Row;
            mainContent.style.alignItems = Align.Center;
            mainContent.style.justifyContent = Justify.SpaceAround;
            mainContent.style.paddingLeft = 32; mainContent.style.paddingRight = 32;
            stage.Add(mainContent);

            // 左侧：200px 发光黑胶唱盘与曲目元数据
            var leftCol = new VisualElement();
            leftCol.style.width = Length.Percent(44);
            leftCol.style.alignItems = Align.Center;
            leftCol.style.justifyContent = Justify.Center;
            mainContent.Add(leftCol);

            var vinylDisc = MusicIconHelper.CreateVinylDiscLarge(190);
            vinylDisc.style.marginBottom = 14;
            leftCol.Add(vinylDisc);
            s_nowPlayingVinylDisc = vinylDisc;

            var bigTitle = new Label(string.IsNullOrEmpty(tabletCurrentTrackTitle) ? "随身听待机中" : tabletCurrentTrackTitle);
            bigTitle.style.fontSize = 17;
            bigTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            bigTitle.style.color = new Color(0.98f, 0.95f, 0.90f, 1f);
            bigTitle.style.unityFontDefinition = Fd();
            bigTitle.style.marginBottom = 4;
            leftCol.Add(bigTitle);

            var bigSub = new Label(string.IsNullOrEmpty(tabletCurrentTrackCategory) ? "随车电波" : (tabletCurrentTrackCategory + " · 先民工业原声巡礼"));
            bigSub.style.fontSize = 10.5f;
            bigSub.style.color = MusicIconHelper.TextMuted;
            bigSub.style.unityFontDefinition = Fd();
            leftCol.Add(bigSub);

            // 右侧：大字瀑布流光歌词
            var rightCol = new VisualElement();
            rightCol.style.width = Length.Percent(50);
            rightCol.style.height = Length.Percent(82);
            rightCol.style.backgroundColor = new Color(0.10f, 0.11f, 0.13f, 0.65f);
            rightCol.style.borderTopLeftRadius = 10; rightCol.style.borderTopRightRadius = 10;
            rightCol.style.borderBottomLeftRadius = 10; rightCol.style.borderBottomRightRadius = 10;
            rightCol.style.borderTopWidth = 1; rightCol.style.borderBottomWidth = 1;
            rightCol.style.borderLeftWidth = 1; rightCol.style.borderRightWidth = 1;
            rightCol.style.borderTopColor = new Color(1f, 1f, 1f, 0.08f);
            rightCol.style.borderBottomColor = rightCol.style.borderTopColor;
            rightCol.style.borderLeftColor = rightCol.style.borderTopColor;
            rightCol.style.borderRightColor = rightCol.style.borderTopColor;
            rightCol.style.paddingLeft = 24; rightCol.style.paddingRight = 24;
            rightCol.style.paddingTop = 20; rightCol.style.paddingBottom = 20;
            rightCol.style.justifyContent = Justify.Center;
            mainContent.Add(rightCol);

            var logs = MusicNowPlayingSidebarView.GetTrackArchiveLogs(tabletCurrentTrackId, tabletCurrentTrackCategory);
            for (int i = 0; i < logs.Length; i++)
            {
                var lineLbl = new Label(logs[i]);
                lineLbl.style.fontSize = (i == 1 || i == 2) ? 13.5f : 11f;
                lineLbl.style.unityFontStyleAndWeight = (i == 1 || i == 2) ? FontStyle.Bold : FontStyle.Normal;
                lineLbl.style.color = (i == 1 || i == 2) ? MusicIconHelper.BrassGold : MusicIconHelper.TextMuted;
                lineLbl.style.unityFontDefinition = Fd();
                lineLbl.style.marginBottom = 12;
                rightCol.Add(lineLbl);
            }
        }

        // ==================== 随身听调频指南 · 列车长手册风格大弹窗 ====================
        public static void ShowOnboardingGuideModal(VisualElement win)
        {
            MusicMainStageView.ShowOnboardingGuideModal(win);
        }
    }
}
