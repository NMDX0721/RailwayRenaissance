using System.Collections;
using UnityEngine;

public class VNAudioManager : MonoBehaviour
{
    public static VNAudioManager Instance { get; private set; }

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private string currentBGM;
    private Coroutine bgmFadeCoroutine;
    private float bgmTargetVolume = 0.6f;
    private const float DefaultFadeDuration = 1.0f;

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

    public void PlayBGM(string name)
    {
        PlayBGM(name, DefaultFadeDuration);
    }

    public void PlayBGM(string name, float fadeDuration)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (currentBGM == name && bgmSource.isPlaying) return;

        var clip = Resources.Load<AudioClip>("bgm/" + name);
        if (clip == null)
        {
            Debug.LogWarning("[VN Audio] BGM not found: " + name);
            return;
        }

        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);

        bgmFadeCoroutine = StartCoroutine(CrossfadeBGM(clip, name, fadeDuration));
    }

    public void StopBGM()
    {
        StopBGM(DefaultFadeDuration);
    }

    public void StopBGM(float fadeDuration)
    {
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
        var clip = Resources.Load<AudioClip>("sfx/" + name);
        if (clip == null)
        {
            Debug.LogWarning("[VN Audio] SFX not found: " + name);
            return;
        }

        sfxSource.PlayOneShot(clip);
    }
}
