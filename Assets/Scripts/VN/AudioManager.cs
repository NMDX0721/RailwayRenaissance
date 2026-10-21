using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VNAudioManager : MonoBehaviour
{
    public static VNAudioManager Instance { get; private set; }

    // 歌曲开始播放时触发（参数：歌曲名）
    public event System.Action<string> OnBGMStarted;

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private string currentBGM;
    private Coroutine bgmFadeCoroutine;
    private float bgmTargetVolume = 0.6f;
    private const float DefaultFadeDuration = 1.0f;
    // 缓存已加载的音频资源，避免高频播放时重复Resources.Load
    private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = 0f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
    }

    private AudioClip LoadClip(string folder, string name)
    {
        string key = folder + "/" + name;
        if (!clipCache.TryGetValue(key, out var clip))
        {
            clip = Resources.Load<AudioClip>(key);
            if (clip != null) clipCache[key] = clip;
        }
        return clip;
    }

    public void PlayBGM(string name)
    {
        PlayBGM(name, DefaultFadeDuration, true);
    }

    public void PlayBGM(string name, float fadeDuration)
    {
        PlayBGM(name, fadeDuration, true);
    }

    /// <summary>最短驻留字数（≈1800字≈4分钟）：当前曲播满之前，顺序推进的换曲请求挂起而不打断。</summary>
    private const int BgmMinResidencyChars = 1800;
    private int charsSinceBgmStart;
    private string pendingBgm;

    /// <summary>人声曲（歌曲类）：只给高潮与章末（剧本侧约束）；代码侧与所有曲共用最短驻留闸门，保证不被提前切断。</summary>
    private static readonly HashSet<string> VocalBgms = new HashSet<string>
    {
        "south_wind", "starlit_rails", "chollima_ride", "train_through_keys",
    };

    /// <summary>每显示一句台词上报字数，驱动最短驻留闸门；闸门放开后补播被挂起的换曲请求。</summary>
    public void ReportChars(int chars)
    {
        if (chars <= 0) return;
        charsSinceBgmStart += chars;
        if (pendingBgm != null && charsSinceBgmStart >= BgmMinResidencyChars)
        {
            var p = pendingBgm;
            pendingBgm = null;
            if (p != currentBGM) PlayBGM(p, DefaultFadeDuration, true);
        }
    }

    /// <summary>force=true：读档/书签跳转/剧本开场等越场上下文，无视驻留闸门直接换曲；
    /// force=false：顺序推进的场景换曲，受 1800 字最短驻留约束。</summary>
    public void PlayBGM(string name, float fadeDuration, bool force)
    {
        if (string.IsNullOrEmpty(name)) return;

        // 旧 BGM 名称 → 新文件名映射（兼容旧存档）
        name = MapBgmName(name);

        // 规则2 同曲继承：相邻场景同曲不重播，进度延续
        if (currentBGM == name && bgmSource.isPlaying) { pendingBgm = null; return; }

        var clip = LoadClip("bgm", name);
        if (clip == null)
        {
            Debug.LogWarning("[VN Audio] BGM not found: " + name);
            return;
        }

        // 规则1 最短驻留：当前曲未播满 1800 字 → 请求挂起，到点由 ReportChars 补播
        if (!force && bgmSource.isPlaying && !string.IsNullOrEmpty(currentBGM)
            && charsSinceBgmStart < BgmMinResidencyChars)
        {
            pendingBgm = name;
            Debug.Log($"[VN Audio] BGM deferred ({charsSinceBgmStart}/{BgmMinResidencyChars} chars): {currentBGM} -> {name}");
            return;
        }

        pendingBgm = null;
        charsSinceBgmStart = 0;

        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);

        bgmFadeCoroutine = StartCoroutine(CrossfadeBGM(clip, name, fadeDuration));
        OnBGMStarted?.Invoke(name);

        // 剧情遭遇即解锁：将曲目标记存入档案库（随身听与系统退出时统一批量持久化，杜绝切歌 I/O 卡顿）
        if (PlayerPrefs.GetInt("ArchiveMusic_" + name, 0) == 0)
        {
            PlayerPrefs.SetInt("ArchiveMusic_" + name, 1);
        }
    }

    /// <summary>当前是否人声曲（歌曲类）。</summary>
    public bool IsCurrentVocalBgm => currentBGM != null && VocalBgms.Contains(currentBGM);

    public void StopBGM()
    {
        StopBGM(DefaultFadeDuration);
    }

    /// <summary>旧 BGM 名称 → 新文件名映射（兼容旧存档）。</summary>
    private string MapBgmName(string name)
    {
        switch (name)
        {
            case "determination": return "embers";
            case "emotional":     return "night_cargo";
            case "travel":        return "cloud_rail";
            case "adventure":     return "cloud_rail";
            case "suspense":      return "borderline";
            case "fun":           return "wheels_joke";
            case "warm":          return "platform";
            case "morning":       return "first_light";
            case "peaceful":      return "first_light";
            default:              return name;
        }
    }

    public void StopBGM(float fadeDuration)
    {
        pendingBgm = null;
        charsSinceBgmStart = 0;
        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);

        if (bgmSource.isPlaying)
            bgmFadeCoroutine = StartCoroutine(FadeOutBGM(fadeDuration));
        else
            currentBGM = null;
    }

    private IEnumerator CrossfadeBGM(AudioClip newClip, string newName, float duration)
    {
        // Fade out current
        if (bgmSource.isPlaying && bgmSource.volume > 0.01f)
        {
            float elapsed = 0f;
            float startVol = bgmSource.volume;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
        }

        // Switch clip
        bgmSource.clip = newClip;
        bgmSource.Play();
        currentBGM = newName;

        // Fade in new
        float elapsed2 = 0f;
        while (elapsed2 < duration)
        {
            elapsed2 += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, bgmTargetVolume, elapsed2 / duration);
            yield return null;
        }
        bgmSource.volume = bgmTargetVolume;
        bgmFadeCoroutine = null;
    }

    private IEnumerator FadeOutBGM(float duration)
    {
        float elapsed = 0f;
        float startVol = bgmSource.volume;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        bgmSource.Stop();
        bgmSource.volume = 0f;
        currentBGM = null;
        bgmFadeCoroutine = null;
    }

    public void PlaySFX(string name)
    {
        var clip = LoadClip("sfx", name);
        if (clip == null)
        {
            Debug.LogWarning("[VN Audio] SFX not found: " + name);
            return;
        }

        sfxSource.PlayOneShot(clip, GameData.SFXVolume * GameData.MasterVolume);
    }

    public void PlayTypewriterSFX()
    {
        var clip = LoadClip("sfx", "button_click");
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, GameData.TypewriterVolume * GameData.MasterVolume);
    }

    public void ApplyVolumeSettings(float master, float bgm, float sfx)
    {
        bgmTargetVolume = 0.6f * master * bgm;
        if (bgmSource.isPlaying)
            bgmSource.volume = bgmTargetVolume;
        sfxSource.volume = 1f;
    }
}
