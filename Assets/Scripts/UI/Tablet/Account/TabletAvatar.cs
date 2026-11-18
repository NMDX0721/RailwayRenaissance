using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Account
{
    /// <summary>
    /// 账号头像的共用部分：6 个预设（底色 + 绘制的图标）和自定义图片。
    /// 自定义图片由 Windows 文件选择窗口选取，裁成正方形、缩到 256 px，以 PNG 存进 persistentDataPath，资料里只记文件名。
    /// 设置页、注册向导等都通过 Decorate / Build 取头像，保证各处长得一样。
    /// </summary>
    public static class TabletAvatar
    {
        public const int PresetCount = 6;
        public const int TextureSize = 256;
        private const long MaxSourceBytes = 20L * 1024 * 1024;

        public static readonly Color[] Colors =
        {
            new Color(0.97f, 0.52f, 0.30f), new Color(0.30f, 0.60f, 0.95f), new Color(0.25f, 0.75f, 0.50f),
            new Color(0.62f, 0.45f, 0.95f), new Color(0.95f, 0.45f, 0.62f), new Color(0.35f, 0.72f, 0.82f)
        };

        public static readonly TabletIconKind[] Icons =
        {
            TabletIconKind.Person, TabletIconKind.Train, TabletIconKind.Star,
            TabletIconKind.Heart, TabletIconKind.Sprout, TabletIconKind.Bulb
        };

        private static Texture2D s_cachedTex;
        private static string s_cachedName;

        // ---------------- 视图 ----------------

        /// <summary>把任意元素装饰成账号头像：自定义图片优先，否则是预设底色 + 图标。</summary>
        public static void Decorate(VisualElement e, AccountProfile p, float size)
        {
            e.Clear();
            if (e is Label lab) lab.text = string.Empty;
            e.style.width = size; e.style.height = size;
            e.style.flexShrink = 0;
            e.style.alignItems = Align.Center; e.style.justifyContent = Justify.Center;
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = size / 2f;
            e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = size / 2f;
            e.style.overflow = Overflow.Hidden;
            var tex = p != null ? LoadTexture(p) : null;
            if (tex != null)
            {
                e.style.backgroundColor = Color.clear;
                e.style.backgroundImage = new StyleBackground(tex);
                e.style.unityBackgroundImageTintColor = Color.white;
                return;
            }
            e.style.backgroundImage = StyleKeyword.None;
            int idx = Mathf.Clamp(p != null ? p.avatar : 0, 0, PresetCount - 1);
            e.style.backgroundColor = Colors[idx];
            e.Add(new TabletIconElement(Icons[idx], size * 0.56f, Color.white) { pickingMode = PickingMode.Ignore });
        }

        public static VisualElement Build(AccountProfile p, float size)
        {
            var e = new VisualElement { name = "account-avatar" };
            Decorate(e, p, size);
            return e;
        }

        /// <summary>预设头像圆（选择页用）。</summary>
        public static VisualElement BuildPreset(int index, float size)
        {
            index = Mathf.Clamp(index, 0, PresetCount - 1);
            var e = new VisualElement { name = "avatar-preset-" + index };
            e.style.width = size; e.style.height = size; e.style.flexShrink = 0;
            e.style.alignItems = Align.Center; e.style.justifyContent = Justify.Center;
            e.style.backgroundColor = Colors[index];
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = size / 2f;
            e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = size / 2f;
            e.Add(new TabletIconElement(Icons[index], size * 0.56f, Color.white) { pickingMode = PickingMode.Ignore });
            return e;
        }

        /// <summary>“自定义”头像格：相册图标或已选图片，右下角带一枚“+”角标，一眼能看出这格是用来选图的。</summary>
        public static VisualElement BuildCustomTile(float size, Texture2D preview)
        {
            var e = new VisualElement { name = "avatar-custom" };
            e.style.width = size; e.style.height = size; e.style.flexShrink = 0;
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = size / 2f;
            e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = size / 2f;

            var disc = new VisualElement { pickingMode = PickingMode.Ignore };
            disc.style.position = Position.Absolute;
            disc.style.left = 0; disc.style.top = 0; disc.style.right = 0; disc.style.bottom = 0;
            disc.style.alignItems = Align.Center; disc.style.justifyContent = Justify.Center;
            disc.style.overflow = Overflow.Hidden;
            disc.style.borderTopLeftRadius = disc.style.borderTopRightRadius = size / 2f;
            disc.style.borderBottomLeftRadius = disc.style.borderBottomRightRadius = size / 2f;
            if (preview != null)
            {
                disc.style.backgroundImage = new StyleBackground(preview);
            }
            else
            {
                disc.style.backgroundColor = new Color(0.86f, 0.88f, 0.93f, 1f);
                disc.Add(new TabletIconElement(TabletIconKind.Image, size * 0.46f, new Color(0.30f, 0.34f, 0.46f)) { pickingMode = PickingMode.Ignore });
            }
            e.Add(disc);

            // “+” 角标
            float bs = Mathf.Max(18f, size * 0.36f);
            var badge = new VisualElement { pickingMode = PickingMode.Ignore, name = "avatar-custom-badge" };
            badge.style.position = Position.Absolute;
            badge.style.right = -bs * 0.12f; badge.style.bottom = -bs * 0.12f;
            badge.style.width = bs; badge.style.height = bs;
            badge.style.backgroundColor = new Color(0.16f, 0.50f, 0.98f, 1f);
            badge.style.borderTopLeftRadius = badge.style.borderTopRightRadius = bs / 2f;
            badge.style.borderBottomLeftRadius = badge.style.borderBottomRightRadius = bs / 2f;
            badge.style.borderTopWidth = badge.style.borderBottomWidth = badge.style.borderLeftWidth = badge.style.borderRightWidth = 2;
            badge.style.borderTopColor = badge.style.borderBottomColor = badge.style.borderLeftColor = badge.style.borderRightColor = Color.white;
            badge.style.alignItems = Align.Center; badge.style.justifyContent = Justify.Center;
            float bar = bs * 0.46f, th = Mathf.Max(2f, bs * 0.1f);
            var h = new VisualElement { pickingMode = PickingMode.Ignore };
            h.style.position = Position.Absolute; h.style.width = bar; h.style.height = th; h.style.backgroundColor = Color.white;
            var v = new VisualElement { pickingMode = PickingMode.Ignore };
            v.style.position = Position.Absolute; v.style.width = th; v.style.height = bar; v.style.backgroundColor = Color.white;
            badge.Add(h); badge.Add(v);
            e.Add(badge);
            return e;
        }

        // ---------------- 自定义图片 ----------------

        public static string FolderPath => Application.persistentDataPath;

        public static Texture2D LoadTexture(AccountProfile p)
        {
            if (p == null || string.IsNullOrEmpty(p.avatarImage)) return null;
            if (s_cachedTex != null && s_cachedName == p.avatarImage) return s_cachedTex;
            try
            {
                string path = Path.Combine(FolderPath, p.avatarImage);
                if (!File.Exists(path)) return null;
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(path))) { UnityEngine.Object.Destroy(tex); return null; }
                if (s_cachedTex != null) UnityEngine.Object.Destroy(s_cachedTex);
                s_cachedTex = tex; s_cachedName = p.avatarImage;
                return tex;
            }
            catch (Exception) { return null; }
        }

        public static void ForgetCache()
        {
            if (s_cachedTex != null) UnityEngine.Object.Destroy(s_cachedTex);
            s_cachedTex = null; s_cachedName = null;
        }

        /// <summary>读取用户选的原图，裁成居中的正方形并缩到 256 px。失败返回 null 并给出原因。</summary>
        public static Texture2D LoadAndCrop(string sourcePath, out string error)
        {
            error = null;
            try
            {
                var info = new FileInfo(sourcePath);
                if (!info.Exists) { error = "找不到这个文件"; return null; }
                if (info.Length > MaxSourceBytes) { error = "图片太大了，请选 20 MB 以内的图片"; return null; }
                var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(src, File.ReadAllBytes(sourcePath))) { UnityEngine.Object.Destroy(src); error = "这不是可以识别的图片"; return null; }

                float w = src.width, h = src.height, side = Mathf.Min(w, h);
                var scale = new Vector2(side / w, side / h);
                var offset = new Vector2((1f - scale.x) * 0.5f, (1f - scale.y) * 0.5f);
                var rt = RenderTexture.GetTemporary(TextureSize, TextureSize, 0, RenderTextureFormat.ARGB32);
                var prev = RenderTexture.active;
                src.filterMode = FilterMode.Bilinear;
                Graphics.Blit(src, rt, scale, offset);
                RenderTexture.active = rt;
                var result = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                result.ReadPixels(new Rect(0, 0, TextureSize, TextureSize), 0, 0);
                result.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.Destroy(src);
                return result;
            }
            catch (Exception ex)
            {
                error = "读取图片失败：" + ex.Message;
                return null;
            }
        }

        /// <summary>把裁好的头像写成 PNG（persistentDataPath 下），返回文件名。</summary>
        public static string SavePng(Texture2D cropped, string xiaomiId)
        {
            string name = "account_avatar_" + xiaomiId + ".png";
            File.WriteAllBytes(Path.Combine(FolderPath, name), ImageConversion.EncodeToPNG(cropped));
            ForgetCache();
            return name;
        }

        public static void DeleteFile(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            try
            {
                string path = Path.Combine(FolderPath, name);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception) { }
            ForgetCache();
        }
    }

    /// <summary>Windows 的“打开文件”窗口（comdlg32）。非 Windows 平台返回 null。</summary>
    public static class TabletFilePicker
    {
        public static bool Available
        {
            get
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                return true;
#else
                return false;
#endif
            }
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class OpenFileName
        {
            public int structSize = Marshal.SizeOf(typeof(OpenFileName));
            public IntPtr owner;
            public IntPtr instance;
            public string filter;
            public string customFilter;
            public int maxCustomFilter;
            public int filterIndex;
            public StringBuilder file;
            public int maxFile;
            public StringBuilder fileTitle;
            public int maxFileTitle;
            public string initialDir;
            public string title;
            public int flags;
            public short fileOffset;
            public short fileExtension;
            public string defExt;
            public IntPtr custData;
            public IntPtr hook;
            public string templateName;
            public IntPtr reserved;
            public int reserved2;
            public int flagsEx;
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();
#endif

        /// <summary>弹出系统窗口选一张图片；取消或失败返回 null。会阻塞到窗口关闭。</summary>
        public static string PickImage(string title)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                var ofn = new OpenFileName
                {
                    owner = GetActiveWindow(),
                    filter = "图片文件 (*.png;*.jpg;*.jpeg;*.bmp;*.tga)\0*.png;*.jpg;*.jpeg;*.bmp;*.tga\0所有文件 (*.*)\0*.*\0\0",
                    filterIndex = 1,
                    file = new StringBuilder(1024),
                    maxFile = 1024,
                    fileTitle = new StringBuilder(260),
                    maxFileTitle = 260,
                    initialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    title = title,
                    flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008,   // EXPLORER | FILEMUSTEXIST | PATHMUSTEXIST | NOCHANGEDIR
                };
                return GetOpenFileName(ofn) ? ofn.file.ToString() : null;
            }
            catch (Exception) { return null; }
#else
            return null;
#endif
        }
    }
}
