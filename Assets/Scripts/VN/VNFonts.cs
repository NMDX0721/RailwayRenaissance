using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>剧情字体：Zpix 像素（默认）/ MiSans 清晰。设置页切换后，已创建的 VN 文字立即换字体。</summary>
public static class VNFonts
{
    public const string Pixel = "zpix", Clear = "misans";
    const string Key = "VNFont";
    public static event Action Changed;

    static Font pixel, clear;

    public static string Current
    {
        get { string v = PlayerPrefs.GetString(Key, Pixel); return v == Clear ? Clear : Pixel; }
    }

    public static Font Game
    {
        get
        {
            if (pixel == null) pixel = Resources.Load<Font>("Fonts/zpix");
            if (Current == Clear)
            {
                if (clear == null) clear = Resources.Load<Font>("Fonts/MiSans-Regular");
                if (clear != null) return clear;   // 缺字体文件时退回像素
            }
            return pixel;
        }
    }

    public static void Set(string value)
    {
        value = value == Clear ? Clear : Pixel;
        if (value == Current) return;
        var old = Game;
        PlayerPrefs.SetString(Key, value);
        PlayerPrefs.Save();
        Changed?.Invoke();
        Retarget(old);
    }

    static readonly System.Collections.Generic.List<VisualElement> roots = new System.Collections.Generic.List<VisualElement>();

    /// <summary>VN 的 UIDocument 根登记于此，字体切换时遍历重设。</summary>
    public static void Register(VisualElement root)
    {
        if (root != null && !roots.Contains(root)) roots.Add(root);
    }

    static void Retarget(Font old)
    {
        var now = Game;
        if (now == old || now == null) return;
        roots.RemoveAll(r => r == null || r.panel == null);
        foreach (var root in roots)
            root.Query<TextElement>().ForEach(t =>
            {
                if (t.resolvedStyle.unityFontDefinition.font == old || t.style.unityFontDefinition.value.font == old)
                    t.style.unityFontDefinition = new FontDefinition { font = now };
            });
    }
}
