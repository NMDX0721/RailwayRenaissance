using System;
using System.Collections.Generic;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    /// <summary>
    /// 桌面图标的排布：第一页 12 个格子（空串为空格）、第二页的顺序列表、“铁路”文件夹里的应用。
    /// 用户拖动图标后保存到 PlayerPrefs，下次进入保持原样；读到不认识的名字会丢掉，新增的应用补到第二页末尾。
    /// 文件夹本身（“铁路”）固定在第一页，可以当作放入目标，不能被拖走。
    /// </summary>
    public static class TabletDesktopLayout
    {
        public const string FolderName = "铁路";
        public const int Page0Slots = 12;
        private const string Key = "TabletDesktopLayout_v1";

        [Serializable]
        private class Data { public string[] page0; public string[] page1; public string[] folder; }

        public static readonly string[] DefaultFolder = { "归途", "媒姬百科", "音乐", "RDA" };

        public static List<string> Page0 { get; private set; } = new List<string>();
        public static List<string> Page1 { get; private set; } = new List<string>();
        public static List<string> Folder { get; private set; } = new List<string>();

        private static bool s_loaded;
        private static string[] s_defaultPage0;
        private static string[] s_all;

        public static event Action Changed;

        public static void Configure(string[] defaultPage0, string[] allApps)
        {
            s_defaultPage0 = defaultPage0; s_all = allApps;
            s_loaded = false;
        }

        public static void EnsureLoaded()
        {
            if (s_loaded) return;
            s_loaded = true;
            Data d = null;
            try
            {
                string json = PlayerPrefs.GetString(Key, string.Empty);
                if (!string.IsNullOrEmpty(json)) d = JsonUtility.FromJson<Data>(json);
            }
            catch { d = null; }
            Normalize(d);
        }

        private static void Normalize(Data d)
        {
            var known = new HashSet<string>(s_all ?? new string[0]);
            Page0 = new List<string>(); Page1 = new List<string>(); Folder = new List<string>();
            var placed = new HashSet<string>();
            if (d == null || d.page0 == null)
            {
                foreach (var a in s_defaultPage0) { Page0.Add(a); placed.Add(a); }
                while (Page0.Count < Page0Slots) Page0.Add(string.Empty);
                Folder.AddRange(DefaultFolder);
            }
            else
            {
                for (int i = 0; i < Page0Slots; i++)
                {
                    string a = i < d.page0.Length ? d.page0[i] : string.Empty;
                    if (!string.IsNullOrEmpty(a) && (a == FolderName || known.Contains(a)) && placed.Add(a)) Page0.Add(a);
                    else Page0.Add(string.Empty);
                }
                if (d.folder != null) foreach (var a in d.folder) if (known.Contains(a) && a != FolderName && !Folder.Contains(a)) Folder.Add(a);
                if (d.page1 != null) foreach (var a in d.page1) if (known.Contains(a) && a != FolderName && placed.Add(a)) Page1.Add(a);
            }
            if (!Page0.Contains(FolderName))
            {
                int free = Page0.IndexOf(string.Empty);
                if (free >= 0) Page0[free] = FolderName; else Page1.Insert(0, FolderName);
                placed.Add(FolderName);
            }
            if (s_all != null)
                foreach (var a in s_all)
                    if (!placed.Contains(a) && !Folder.Contains(a)) { Page1.Add(a); placed.Add(a); }
            // 第二页不放已在第一页的应用
            Page1.RemoveAll(a => Page0.Contains(a));
        }

        private static void Save()
        {
            try
            {
                var d = new Data { page0 = Page0.ToArray(), page1 = Page1.ToArray(), folder = Folder.ToArray() };
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(d));
                PlayerPrefs.Save();
            }
            catch { }
            Changed?.Invoke();
        }

        public static void ResetToDefault()
        {
            try { PlayerPrefs.DeleteKey(Key); } catch { }
            s_loaded = false;
            EnsureLoaded();
            Changed?.Invoke();
        }

        private static void Remove(string app)
        {
            int i = Page0.IndexOf(app);
            if (i >= 0) Page0[i] = string.Empty;
            Page1.Remove(app);
        }

        public static void MoveToFolder(string app)
        {
            EnsureLoaded();
            if (app == FolderName) return;
            Remove(app);
            if (!Folder.Contains(app)) Folder.Add(app);
            Save();
        }

        /// <summary>放到第一页第 slot 格：格子空就直接放；是文件夹就进文件夹；是别的应用就互换位置。</summary>
        public static void MoveToPage0(string app, int slot)
        {
            EnsureLoaded();
            if (app == FolderName || slot < 0 || slot >= Page0Slots) return;
            string occupant = Page0[slot];
            if (occupant == app) return;
            if (occupant == FolderName) { MoveToFolder(app); return; }
            int from = Page0.IndexOf(app);
            bool fromPage1 = Page1.Contains(app);
            bool fromFolder = Folder.Remove(app);
            Remove(app);
            Page0[slot] = app;
            if (!string.IsNullOrEmpty(occupant))
            {
                if (from >= 0) Page0[from] = occupant;
                else if (fromPage1) Page1.Add(occupant);
                else Page1.Add(occupant);
            }
            Save();
        }

        /// <summary>放到第二页第 index 个位置（从文件夹或第一页拿出来也走这里）。</summary>
        public static void MoveToPage1(string app, int index)
        {
            EnsureLoaded();
            if (app == FolderName) return;
            int old = Page1.IndexOf(app);
            Folder.Remove(app);
            Remove(app);
            if (old >= 0 && old < index) index--;
            index = Mathf.Clamp(index, 0, Page1.Count);
            Page1.Insert(index, app);
            Save();
        }

        /// <summary>从文件夹拿出来：优先第一页的空格，没有空格放第二页末尾。</summary>
        public static void TakeOutOfFolder(string app)
        {
            EnsureLoaded();
            if (!Folder.Contains(app)) return;
            if (Page0.Contains(app) || Page1.Contains(app)) { Folder.Remove(app); Save(); return; }   // 桌面上已经有一份
            int free = Page0.IndexOf(string.Empty);
            if (free >= 0) { Folder.Remove(app); Page0[free] = app; Save(); }
            else MoveToPage1(app, Page1.Count);
        }
    }
}
