using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public sealed class VNVoiceCatalogue { public VNVoiceLine[] entries; }
[Serializable] public sealed class VNVoiceLine { public string key, sourceHash, textHash, clip; }

// Reviewed audio is indexed independently from subtitles. A changed source must be re-approved.
public sealed class VNVoicePlayer : MonoBehaviour
{
    private AudioSource source;
    private readonly Dictionary<string, Dictionary<string, VNVoiceLine>> catalogues = new();
    private readonly Dictionary<string, AudioClip> clips = new();
    public bool HasClip => source != null && source.clip != null;
    public bool IsPlaying => HasClip && source.isPlaying;
    private bool paused;
    public bool IsPending => IsPlaying || paused;
    public float Progress => !HasClip ? 0 : source.isPlaying || paused ? Mathf.Clamp01(source.time / source.clip.length) : completed ? 1 : 0;
    private bool completed;
    private float volume = -1;
    public static string Language => PlayerPrefs.GetString("VNVoiceLanguage", "ja");

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.loop = false; source.playOnAwake = false;
    }
    private void Update()
    {
        if (source == null) return;
        float value = GameData.MasterVolume * VNPresentationSettings.VoiceVolume;
        if (!Mathf.Approximately(value, volume)) source.volume = volume = value;
        if (HasClip && !paused && !source.isPlaying) completed = true;
    }
    public static bool CanVoice(DialogueEntry entry)
    {
        if (entry == null || entry.t != "d" || string.IsNullOrEmpty(entry.s)) return false;
        if (entry.s == "林彪悍" || entry.s == "男大学生" || entry.s == "lin_biaohan" || entry.s.StartsWith("林彪悍（")) return false;
        return !string.IsNullOrWhiteSpace(entry.text) && !entry.text.TrimStart().StartsWith("[");
    }
    public void Bind(string script, int scene, int dialogue, DialogueEntry entry)
    {
        Clear();
        string language = Language;
        if (!CanVoice(entry) || language == "none") return;
        string path = "Voice/" + language + "/" + script;
        if (!catalogues.TryGetValue(path, out var catalogue))
        {
            catalogue = new Dictionary<string, VNVoiceLine>();
            var asset = Resources.Load<TextAsset>(path + "/catalogue");
            if (asset != null)
            {
                try
                {
                    var data = JsonUtility.FromJson<VNVoiceCatalogue>(asset.text);
                    foreach (var line in data.entries ?? Array.Empty<VNVoiceLine>())
                        if (!string.IsNullOrEmpty(line.key)) catalogue[line.key] = line;
                }
                catch (Exception ex) { Debug.LogWarning("[VN Voice] Invalid catalogue: " + path + " " + ex.Message); }
            }
            catalogues[path] = catalogue;
        }
        string key = VNLocalization.Key(script, scene, dialogue, entry);
        if (!catalogue.TryGetValue(key, out var record) || record.sourceHash != VNLocalization.Hash(entry.text) || string.IsNullOrEmpty(record.clip)) return;
        string spoken = language == VNSubtitleSettings.Chinese ? entry.text : VNLocalization.Resolve(script, key, entry.text, entry.textJa).Text(language);
        if (string.IsNullOrWhiteSpace(spoken) || record.textHash != VNLocalization.Hash(spoken)) return;
        // Restrict catalogue entries to the selected script's resource folder.
        if (record.clip.Contains("..") || record.clip.Contains(":") || record.clip.StartsWith("/") || record.clip.Contains("\\")) return;
        string clipPath = path + "/" + record.clip;
        if (!clips.TryGetValue(clipPath, out var clip)) clips[clipPath] = clip = Resources.Load<AudioClip>(clipPath);
        if (clip == null) return;
        source.clip = clip;
        Replay();
    }
    public void Replay()
    {
        if (!HasClip) return;
        paused = completed = false; source.time = 0;
        source.volume = volume = GameData.MasterVolume * VNPresentationSettings.VoiceVolume;
        source.Play();
    }
    public void Seek(float progress)
    {
        if (!HasClip) return;
        paused = completed = false;
        source.time = Mathf.Clamp01(progress) * Mathf.Max(0, source.clip.length - .02f);
        source.Play();
    }
    public void Pause(bool value)
    {
        if (!HasClip || paused == value) return;
        if (value) { if (!source.isPlaying) return; source.Pause(); paused = true; }
        else { source.UnPause(); paused = false; }
    }
    public void Clear()
    {
        if (source != null) { source.Stop(); source.clip = null; }
        paused = completed = false;
    }
    private void OnDisable() => Clear();
}
