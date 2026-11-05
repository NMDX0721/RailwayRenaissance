using System;
using UnityEngine;
using UnityEngine.UI;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    public enum TabletThemeMode
    {
        Light = 0,
        Dark = 1
    }

    /// <summary>
    /// 平板显示偏好（站位接口）：
    /// - 屏幕亮度：已全局生效。默认 1（不压暗），调低时在整个画面上盖一层半透明黑，最低 0.2。
    /// - 外观模式（浅色 / 深色）：只保存偏好并发出 ThemeChanged 事件；各界面尚未适配深色，
    ///   以后适配时订阅 ThemeChanged / 读取 Theme 即可，不需要再改设置页。
    /// </summary>
    public static class TabletDisplayPreferences
    {
        public const float MinBrightness = 0.2f;
        public const float MaxBrightness = 1f;
        private const string ThemeKey = "TabletThemeMode";
        private const string BrightnessKey = "TabletBrightness";

        private static bool s_loaded;
        private static TabletThemeMode s_theme;
        private static float s_brightness = 1f;

        public static event Action<TabletThemeMode> ThemeChanged;
        public static event Action<float> BrightnessChanged;

        public static TabletThemeMode Theme { get { Load(); return s_theme; } }
        public static float Brightness { get { Load(); return s_brightness; } }

        /// <summary>深色外观是否已有界面适配；设置页据此决定是否提示“适配中”。</summary>
        public static bool DarkThemeAdapted => false;

        public static void SetTheme(TabletThemeMode mode)
        {
            Load();
            if (s_theme == mode) return;
            s_theme = mode;
            PlayerPrefs.SetInt(ThemeKey, (int)mode);
            PlayerPrefs.Save();
            ThemeChanged?.Invoke(mode);
        }

        public static void SetBrightness(float value)
        {
            Load();
            value = Mathf.Clamp(value, MinBrightness, MaxBrightness);
            if (Mathf.Approximately(s_brightness, value)) return;
            s_brightness = value;
            PlayerPrefs.SetFloat(BrightnessKey, value);
            PlayerPrefs.Save();
            BrightnessChanged?.Invoke(value);
        }

        private static void Load()
        {
            if (s_loaded) return;
            s_loaded = true;
            s_theme = PlayerPrefs.GetInt(ThemeKey, 0) == 1 ? TabletThemeMode.Dark : TabletThemeMode.Light;
            s_brightness = Mathf.Clamp(PlayerPrefs.GetFloat(BrightnessKey, 1f), MinBrightness, MaxBrightness);
        }

        // 全局压暗层：最高排序的 Overlay Canvas，不拦截点击
        private sealed class DimOverlay : MonoBehaviour
        {
            private Image _image;

            private void Awake()
            {
                var canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32000;
                var go = new GameObject("Dim");
                go.transform.SetParent(transform, false);
                _image = go.AddComponent<Image>();
                _image.color = Color.black;
                _image.raycastTarget = false;
                var rt = _image.rectTransform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                Apply(Brightness);
                BrightnessChanged += Apply;
            }

            private void OnDestroy() => BrightnessChanged -= Apply;

            private void Apply(float brightness)
            {
                if (_image == null) return;
                float alpha = 1f - Mathf.Clamp(brightness, MinBrightness, MaxBrightness);
                _image.color = new Color(0f, 0f, 0f, alpha);
                _image.enabled = alpha > 0.001f;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Application.isPlaying) return;
            var go = new GameObject("TabletDimOverlay") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<DimOverlay>();
        }
    }
}
