using UnityEngine;
using System.IO;

namespace RailwayRenaissance.Core
{
    /// <summary>
    /// 全局光标与音效托管类。解耦自旧版 LoginManager 中的光标/音效逻辑。
    /// </summary>
    public static class GameCursorManager
    {
        public static Texture2D cursorTexture;
        public static AudioClip clickClip;
        public static bool sfxEnabled = true;

        public static Texture2D GetArrowTexture()
        {
            if (cursorTexture == null) cursorTexture = LoadCursorTexture("Cursors/cursor_arrow", 3);
            return cursorTexture;
        }

        public static void SetDefaultCursor()
        {
            if (cursorTexture == null) cursorTexture = LoadCursorTexture("Cursors/cursor_arrow", 3);
            Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.ForceSoftware);
        }

        public static Texture2D LoadCursorTexture(string resourcePath, int scale)
        {
            Texture2D src = Resources.Load<Texture2D>(resourcePath);
            if (src == null) return Texture2D.whiteTexture;

            if (scale <= 1) return src;

            int ow = src.width, oh = src.height;
            int nw = ow * scale, nh = oh * scale;
            Color[] srcPx = src.GetPixels();
            Color[] dstPx = new Color[nw * nh];
            for (int y = 0; y < nh; y++)
            {
                for (int x = 0; x < nw; x++)
                {
                    dstPx[y * nw + x] = srcPx[(y / scale) * ow + (x / scale)];
                }
            }
            Texture2D tex = new Texture2D(nw, nh, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.SetPixels(dstPx);
            tex.Apply();
            return tex;
        }

        public static void EnsureClickClipLoaded()
        {
            if (clickClip == null)
            {
                clickClip = Resources.Load<AudioClip>("Audio/button_click");
            }
        }

        public static void PlayClickSound()
        {
            if (!sfxEnabled) return;
            EnsureClickClipLoaded();
            if (clickClip == null) return;
            var loginMgr = Object.FindAnyObjectByType<LoginManager>();
            if (loginMgr != null)
            {
                var src = loginMgr.GetComponentInChildren<AudioSource>();
                if (src != null)
                {
                    src.PlayOneShot(clickClip, 0.8f);
                }
            }
        }
    }
}
