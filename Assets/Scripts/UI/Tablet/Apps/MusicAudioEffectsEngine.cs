using System;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 随身听声音工程与调频滤镜引擎 (MusicAudioEffectsEngine)
    /// 借鉴 Folia audioEffects (wowBranch, noiseBranch) & automix (crossfadePlanner)
    /// 包含：智能平滑交叉淡入淡出 (Crossfade)、老式磁带抖晃 (Wow & Flutter)、
    /// 荒原电台调频质感拨钮、停站自熄睡眠定时器。
    /// </summary>
    public static class MusicAudioEffectsEngine
    {
        public enum RadioFilterMode
        {
            Clean = 0,      // 纯净现代高保真
            Locomotive = 1, // 工务机车电台 (轻度磁带抖晃 + 沿线沙风微粒)
            Pioneer = 2,    // 先民真空电台 (经典电子管微晃 + 暖调老收音机)
            VintageTubeFM = 3 // 电子管调频立体声
        }

        public enum SleepTimerOption
        {
            Off = 0,
            Min15 = 15,
            Min30 = 30,
            Min45 = 45,
            EndOfTrack = 999
        }

        // 当前滤镜与定时器状态
        public static RadioFilterMode CurrentFilterMode = RadioFilterMode.Locomotive; // 默认工务机车电台质感
        public static SleepTimerOption CurrentSleepOption = SleepTimerOption.Off;
        public static float RemainingSleepSeconds = 0f;
        public static bool IsSleepTimerActive = false;

        // 双通道平滑交叉淡化 (Deck A & Deck B Crossfade)
        private static AudioSource s_deckA;
        private static AudioSource s_deckB;
        private static bool s_isDeckAPrimary = true;
        private static bool s_isCrossfading = false;
        private static bool s_paused;
        private static float s_crossfadeTimer = 0f;
        private static float s_crossfadeDuration = 1.6f; // 1.6 秒等功率交叉过渡

        // Wow & Flutter (磁带抖晃物理微扰算法，模拟老旧机车磁带走带颠簸)
        private static float s_wowTimer = 0f;
        private static float s_flutterTimer = 0f;

        // 睡眠淡出
        private static bool s_isFadingToSleep = false;
        private static float s_sleepFadeTimer = 0f;

        /// <summary>
        /// 初始化或获取双通道 AudioSource
        /// </summary>
        public static void EnsureDecks(AudioSource existingSource)
        {
            if (s_deckA == null)
            {
                if (existingSource != null)
                {
                    s_deckA = existingSource;
                    s_deckA.name = "TabletMusic_DeckA";
                }
                else
                {
                    var goA = new GameObject("TabletMusic_DeckA");
                    UnityEngine.Object.DontDestroyOnLoad(goA);
                    s_deckA = goA.AddComponent<AudioSource>();
                }
            }

            if (s_deckB == null)
            {
                var goB = new GameObject("TabletMusic_DeckB");
                UnityEngine.Object.DontDestroyOnLoad(goB);
                s_deckB = goB.AddComponent<AudioSource>();
            }
        }

        public static AudioSource GetActiveDeck()
        {
            return s_isDeckAPrimary ? s_deckA : s_deckB;
        }

        public static AudioSource GetStandbyDeck()
        {
            return s_isDeckAPrimary ? s_deckB : s_deckA;
        }

        /// <summary>
        /// 启动平滑交叉淡化切歌 (Equal-Power Crossfade)
        /// </summary>
        public static void PlayWithCrossfade(AudioClip newClip, float baseVolume, Action onComplete = null)
        {
            if (newClip == null) return;
            s_paused = false;
            s_isCrossfading = false;

            var activeDeck = GetActiveDeck();
            var nextDeck = GetStandbyDeck();

            if (activeDeck == null || nextDeck == null || !activeDeck.isPlaying)
            {
                if (nextDeck != null) nextDeck.Stop();
                // 无在播曲目时直接单轨起播
                if (activeDeck != null)
                {
                    activeDeck.clip = newClip;
                    activeDeck.volume = baseVolume;
                    activeDeck.pitch = 1.0f;
                    activeDeck.Play();
                }
                return;
            }

            // 启动平滑交替淡化
            nextDeck.Stop();
            nextDeck.clip = newClip;
            nextDeck.time = 0f;
            nextDeck.volume = 0f;
            nextDeck.pitch = 1.0f;
            nextDeck.Play();

            s_isDeckAPrimary = !s_isDeckAPrimary;
            s_isCrossfading = true;
            s_crossfadeTimer = 0f;
        }

        internal static AudioSource PlayWithCrossfadeAndGetTarget(AudioClip newClip, float baseVolume)
        {
            EnsureDecks(null);
            AudioSource targetDeck = GetActiveDeck();
            if (targetDeck != null && targetDeck.isPlaying)
            {
                targetDeck = GetStandbyDeck();
            }

            PlayWithCrossfade(newClip, baseVolume);
            return targetDeck;
        }

        /// <summary>
        /// 每帧更新音频效果、磁带抖晃微扰与睡眠定时器
        /// </summary>
        public static void Update(float deltaTime, float userVolume, Action onSleepFired)
        {
            if (s_paused) return;
            var activeDeck = GetActiveDeck();
            var standbyDeck = GetStandbyDeck();

            // 1. 处理 Crossfade 交叉渐变
            if (s_isCrossfading && activeDeck != null && standbyDeck != null)
            {
                s_crossfadeTimer += deltaTime;
                float progress = Mathf.Clamp01(s_crossfadeTimer / s_crossfadeDuration);

                // 等功率交叉淡化曲线 (Equal Power: cos & sin)
                float outVol = Mathf.Cos(progress * Mathf.PI * 0.5f) * userVolume;
                float inVol = Mathf.Sin(progress * Mathf.PI * 0.5f) * userVolume;

                standbyDeck.volume = outVol;
                activeDeck.volume = inVol;

                if (progress >= 1.0f)
                {
                    standbyDeck.Stop();
                    standbyDeck.volume = 0f;
                    s_isCrossfading = false;
                }
            }
            else if (activeDeck != null && !s_isFadingToSleep)
            {
                activeDeck.volume = userVolume;
            }

            // 2. 处理 Wow & Flutter 物理抖晃音调微变 (基于 Folia wowBranch 参数)
            if (activeDeck != null && activeDeck.isPlaying)
            {
                if (CurrentFilterMode == RadioFilterMode.Locomotive)
                {
                    // 慢速行走抖动 (0.75 Hz) + 快速齿轮微震 (6.5 Hz)
                    s_wowTimer += deltaTime * 0.75f * Mathf.PI * 2f;
                    s_flutterTimer += deltaTime * 6.5f * Mathf.PI * 2f;
                    float wow = Mathf.Sin(s_wowTimer) * 0.0035f;
                    float flutter = Mathf.Sin(s_flutterTimer) * 0.0012f;
                    activeDeck.pitch = Mathf.Clamp(1.0f + wow + flutter, 0.98f, 1.02f);
                }
                else if (CurrentFilterMode == RadioFilterMode.Pioneer)
                {
                    // 浓厚电子管温润微晃
                    s_wowTimer += deltaTime * 0.45f * Mathf.PI * 2f;
                    float wow = Mathf.Sin(s_wowTimer) * 0.0075f;
                    activeDeck.pitch = Mathf.Clamp(1.0f + wow, 0.97f, 1.03f);
                }
                else
                {
                    activeDeck.pitch = 1.0f;
                }
            }

            // 3. 处理睡眠定时器 (Sleep Timer)
            if (IsSleepTimerActive && RemainingSleepSeconds > 0f)
            {
                RemainingSleepSeconds -= deltaTime;
                if (RemainingSleepSeconds <= 3.0f && !s_isFadingToSleep)
                {
                    s_isFadingToSleep = true;
                    s_sleepFadeTimer = 0f;
                }

                if (s_isFadingToSleep && activeDeck != null)
                {
                    s_sleepFadeTimer += deltaTime;
                    float fadeRatio = Mathf.Clamp01(1f - (s_sleepFadeTimer / 3.0f));
                    activeDeck.volume = userVolume * fadeRatio;
                }

                if (RemainingSleepSeconds <= 0f)
                {
                    RemainingSleepSeconds = 0f;
                    IsSleepTimerActive = false;
                    s_isFadingToSleep = false;
                    if (activeDeck != null)
                    {
                        Pause();
                    }
                    onSleepFired?.Invoke();
                }
            }
        }

        /// <summary>
        /// 设定睡眠定时器
        /// </summary>
        public static void SetSleepTimer(SleepTimerOption option, float currentTrackRemaining = 0f)
        {
            CurrentSleepOption = option;
            s_isFadingToSleep = false;
            if (option == SleepTimerOption.Off)
            {
                IsSleepTimerActive = false;
                RemainingSleepSeconds = 0f;
            }
            else if (option == SleepTimerOption.EndOfTrack)
            {
                IsSleepTimerActive = true;
                RemainingSleepSeconds = Mathf.Max(5f, currentTrackRemaining);
            }
            else
            {
                IsSleepTimerActive = true;
                RemainingSleepSeconds = (int)option * 60f;
            }
        }

        public static string FormatSleepTimeRemaining()
        {
            if (!IsSleepTimerActive || RemainingSleepSeconds <= 0f) return "关";
            int m = (int)(RemainingSleepSeconds / 60f);
            int s = (int)(RemainingSleepSeconds % 60f);
            return $"{m:D2}:{s:D2}";
        }

        public static bool IsPlaying()
        {
            var a = GetActiveDeck();
            var b = GetStandbyDeck();
            return (a != null && a.isPlaying) || (b != null && b.isPlaying);
        }

        public static void Pause()
        {
            s_paused = true;
            if (s_deckA != null && s_deckA.isPlaying) s_deckA.Pause();
            if (s_deckB != null && s_deckB.isPlaying) s_deckB.Pause();
        }

        public static void UnPause()
        {
            s_paused = false;
            var active = GetActiveDeck();
            if (active != null) active.UnPause();
            if (s_isCrossfading)
            {
                var standby = GetStandbyDeck();
                if (standby != null) standby.UnPause();
            }
        }

        public static void Stop()
        {
            s_isCrossfading = false;
            if (s_deckA != null) { s_deckA.Stop(); s_deckA.clip = null; }
            if (s_deckB != null) { s_deckB.Stop(); s_deckB.clip = null; }
        }
    }
}
