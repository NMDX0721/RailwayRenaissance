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

        public static float CurrentPosition
        {
            get => TryGetCurrentPosition(out float position) ? position : 0f;
        }
        public static bool TryGetCurrentPosition(out float position)
        {
            position = 0;
            var deck = MusicAudioEffectsEngine.GetActiveDeck();
            var source = deck != null && deck.clip == tabletCurrentClip ? deck : tabletMusicSource;
            if (source == null || tabletCurrentClip == null || source.clip != tabletCurrentClip) return false;
            position = Mathf.Clamp(source.time, 0f, tabletCurrentClip.length);
            return true;
        }

        public static void ConfigurePlayback(float volume,int mode)
        {
            s_currentVolume=float.IsNaN(volume)||float.IsInfinity(volume)?0.65f:Mathf.Clamp01(volume);
            tabletPlayMode=(TabletPlayMode)Mathf.Clamp(mode,0,2);
            bool loop=tabletPlayMode==TabletPlayMode.SingleRepeat;
            float output=s_isMuted?0f:s_currentVolume;
            if(tabletMusicSource!=null){tabletMusicSource.loop=loop;tabletMusicSource.volume=output;}
            var deck=MusicAudioEffectsEngine.GetActiveDeck();
            if(deck!=null){deck.loop=loop;deck.volume=output;}
        }
        private static bool tabletShowLyrics = true;
        public static MusicSidebarNavView.MusicViewId s_currentView = MusicSidebarNavView.MusicViewId.Discover;

        public static string GetTrackDurationText(TabletMusicEntry m)
        {
            var clip = m == null ? null : Resources.Load<AudioClip>("bgm/" + m.clipName);
            return clip != null ? FormatAudioTime(clip.length) : "--:--";
        }
        public static void SetVolume(float volume)
        {
            ConfigurePlayback(volume, (int)tabletPlayMode);
            MusicLibrarySession.State.volume = s_currentVolume;
            MusicLibrarySession.Save();
        }
        public static void SetPlayMode(int mode)
        {
            ConfigurePlayback(s_currentVolume, mode);
            MusicLibrarySession.Commit();
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
        public static List<string> PlayQueue => MusicLibrarySession.State.queue;
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
            return !string.IsNullOrEmpty(trackId) && MusicLibrarySession.State.favorites.Contains(trackId);
        }

        public static void ToggleTabletMusicFavorite(string trackId)
        {
            if (string.IsNullOrEmpty(trackId)) return;
            MusicLibrarySession.ToggleFavorite(trackId);
            UpdateFavoriteVisuals(trackId);
        }

        public static int GetTabletFavoriteCount()
        {
            return MusicLibrarySession.State.favorites.Count;
        }

        private static VisualElement s_toastPill;
        private static Label s_toastLabel;
        private static IVisualElementScheduledItem s_toastScheduledItem;
        private static IVisualElementScheduledItem s_progressScheduledItem;
        private static Action<string> s_showToast;

        public static void ShowToast(string msg)
        {
            if (RailwayMusicTerminalView.ShowToast(msg)) return;
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
            MusicLibrarySession.Ensure();
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

            }

            MusicAudioEffectsEngine.EnsureDecks(tabletMusicSource);

            string prevTrackId = tabletCurrentTrackId;

            float targetVol = s_isMuted ? 0f : s_currentVolume;
            tabletMusicSource = MusicAudioEffectsEngine.PlayWithCrossfadeAndGetTarget(clip, targetVol);
            if (tabletMusicSource != null)
                tabletMusicSource.loop = (tabletPlayMode == TabletPlayMode.SingleRepeat);

            tabletCurrentClip = clip;
            tabletCurrentTrackId = m.id;
            tabletCurrentTrackTitle = m.title;
            tabletCurrentTrackCategory = m.category;
            tabletIsPlaying = true;
            MusicLibrarySession.Started(m.id, clip);

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

            }

            MusicAudioEffectsEngine.EnsureDecks(tabletMusicSource);
            string prevTrackId = tabletCurrentTrackId;

            MusicLibrarySession.Ensure();
            float targetVol = s_isMuted ? 0f : s_currentVolume;
            tabletMusicSource = MusicAudioEffectsEngine.PlayWithCrossfadeAndGetTarget(clip, targetVol);
            if (tabletMusicSource != null)
                tabletMusicSource.loop = (tabletPlayMode == TabletPlayMode.SingleRepeat);

            tabletCurrentClip = clip;
            tabletCurrentTrackId = trackId;
            tabletCurrentTrackTitle = title;
            tabletCurrentTrackCategory = category;
            tabletIsPlaying = true;

            MusicLibrarySession.Started(trackId, clip);
            UpdateCardState(prevTrackId);
            UpdateCardState(trackId);
            UpdatePlayerBarUI();
        }

        public static void ToggleTabletMusicPlayPause()
        {
            if (MusicLibrarySession.Loading) { MusicLibrarySession.CancelPending(); return; }
            if (tabletCurrentClip == null)
            {
                MusicLibrarySession.Ensure();
                var saved = MusicLibrarySession.State.currentId;
                if (!string.IsNullOrEmpty(saved)) MusicLibrarySession.Play(saved, true);
                else MusicLibrarySession.Advance(1);
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

            MusicLibrarySession.Commit();
            UpdateCardState(tabletCurrentTrackId);
            UpdatePlayerBarUI();
        }

        public static void PlayNextTabletTrack()
        {
            MusicLibrarySession.Advance(1);
        }

        public static void PlayPrevTabletTrack()
        {
            MusicLibrarySession.Advance(-1);
        }

        public static void SeekTabletMusic(float norm)
        {
            var activeDeck = MusicAudioEffectsEngine.GetActiveDeck();
            var targetSource = (activeDeck != null && activeDeck.clip == tabletCurrentClip) ? activeDeck : tabletMusicSource;
            if (targetSource != null && tabletCurrentClip != null && targetSource.clip == tabletCurrentClip && tabletCurrentClip.length > 0.1f)
            {
                targetSource.time = Mathf.Clamp(norm * tabletCurrentClip.length, 0f, tabletCurrentClip.length - 0.1f);
                MusicLibrarySession.Save();
                RailwayMusicTerminalView.UpdatePlayback();
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
        private static int s_audioFrame = -1;
        private static bool s_wasAudible;
        public static void UpdateAudioProgress()
        {
            if (s_audioFrame == Time.frameCount) return;
            s_audioFrame = Time.frameCount;
            MusicAudioEffectsEngine.Update(Time.unscaledDeltaTime, s_isMuted ? 0f : s_currentVolume, () => {
                tabletIsPlaying = false;
                MusicLibrarySession.Commit();
                ShowToast("睡眠定时已到，随身听已暂停");
            });
            var active = MusicAudioEffectsEngine.GetActiveDeck();
            if (active != null) tabletMusicSource = active;
            if (tabletIsPlaying && !MusicLibrarySession.Loading && tabletCurrentClip != null && active != null)
            {
                if (active.isPlaying) s_wasAudible = true;
                else if (s_wasAudible)
                {
                    s_wasAudible = false;
                    PlayNextTabletTrack();
                }
            }
            else s_wasAudible = false;
            RailwayMusicTerminalView.UpdatePlayback();
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
            s_progressScheduledItem?.Pause();
            s_cardBindings.Clear(); s_waveBars.Clear();
            s_playerBarHandle = null; s_nowPlayingSidebarHandle = null;
            s_toastPill = null; s_toastLabel = null;
            MusicLibrarySession.Ensure();
            RailwayMusicTerminalView.Build(win, () => { MusicLibrarySession.Save(); s_onClose?.Invoke(); });
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
