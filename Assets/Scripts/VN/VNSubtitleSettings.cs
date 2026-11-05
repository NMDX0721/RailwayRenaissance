using System;
using UnityEngine;

public static class VNSubtitleSettings
{
    public const string Chinese = "zh-Hans", Japanese = "ja", Off = "none";
    const string PrimaryKey = "VNPrimarySubtitleLanguage", SecondaryKey = "VNSecondarySubtitleLanguage";
    public static event Action Changed;

    public static string PrimaryLanguage
    {
        get { string code=PlayerPrefs.GetString(PrimaryKey,Chinese);return VNLocalization.Supports(code)?code:Chinese; }
    }
    public static string SecondaryLanguage
    {
        get
        {
            string language = PlayerPrefs.HasKey(SecondaryKey) ? PlayerPrefs.GetString(SecondaryKey) :
                PlayerPrefs.HasKey("VNJapaneseSubtitles") && PlayerPrefs.GetInt("VNJapaneseSubtitles") == 1 ? Japanese : Off;
            return VNLocalization.Supports(language) && language != PrimaryLanguage ? language : Off;
        }
    }
    public static float SecondaryFontSize
    {
        get
        {
            if (PlayerPrefs.HasKey("VNSecondarySubtitleSizeV2")) return Mathf.Clamp(PlayerPrefs.GetInt("VNSecondarySubtitleSizeV2"), 16, 22);
            if (!PlayerPrefs.HasKey("VNSecondarySubtitleSize")) return 18;
            int old = PlayerPrefs.GetInt("VNSecondarySubtitleSize");
            return old <= 18 ? 16 : old >= 24 ? 22 : 18;
        }
    }

    public static void SetPrimaryLanguage(string language)
    {
        string previous = PrimaryLanguage, secondary = SecondaryLanguage;
        string primary = VNLocalization.Supports(language) ? language : Chinese;
        if (secondary == primary) secondary = previous != primary ? previous : Off;
        Store(primary, secondary);
    }

    public static void SetSecondaryLanguage(string language)
    {
        string secondary = VNLocalization.Supports(language) && language != PrimaryLanguage ? language : Off;
        Store(PrimaryLanguage, secondary);
    }

    public static void SetSecondaryFontSize(int size)
    {
        PlayerPrefs.SetInt("VNSecondarySubtitleSizeV2", Mathf.Clamp(size, 16, 22));
        PlayerPrefs.Save(); Changed?.Invoke();
    }

    public static void Reset()
    {
        PlayerPrefs.SetInt("VNSecondarySubtitleSizeV2", 18);
        Store(Chinese, Off);
    }

    static void Store(string primary, string secondary)
    {
        PlayerPrefs.SetString(PrimaryKey, primary);
        PlayerPrefs.SetString(SecondaryKey, secondary);
        PlayerPrefs.Save(); Changed?.Invoke();
    }

}
