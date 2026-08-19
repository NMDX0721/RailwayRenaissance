using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>书签管理器：自动书签（5个滚动销毁）+ 手动书签（永久）。存储于 PlayerPrefs。</summary>
public static class BookmarkManager
{
    // ================= 数据结构 =================

    [Serializable]
    public class Bookmark
    {
        public string id;
        public string name;
        public string scriptName;
        public int sceneIndex;
        public int dialogueIndex;
        public int episodeNum;
        public string episodeTitle;
        public string previewText;
        public bool isAuto;
        public bool isCompleted;
        public long updatedAt;
    }

    private const int AutoSlotCount = 5;
    private const string AutoKey = "Bookmark_Auto_";     // Bookmark_Auto_0 ~ 4
    private const string ManualKey = "Bookmark_Manual_"; // Bookmark_Manual_<id>
    private const string ManualIndexKey = "Bookmark_ManualIndex"; // 手动书签 id 列表

    // ================= 自动书签 =================

    /// <summary>更新当前话的自动书签（自动推进）。已存在则覆盖，否则新建；满员滚动覆盖最旧。</summary>
    public static void UpdateAutoBookmark(string scriptName, string episodeTitle, int episodeNum, int sceneIndex, int dialogueIndex, string preview)
    {
        for (int i = 0; i < AutoSlotCount; i++)
        {
            var bm = LoadAuto(i);
            if (bm != null && bm.scriptName == scriptName)
            {
                bm.sceneIndex = sceneIndex;
                bm.dialogueIndex = dialogueIndex;
                bm.previewText = TrimPreview(preview);
                bm.episodeTitle = episodeTitle;
                bm.episodeNum = episodeNum;
                bm.updatedAt = Now();
                SaveAuto(i, bm);
                return;
            }
        }

        int slot = FindOldestAutoSlot();
        var nb = new Bookmark
        {
            id = "auto_" + slot,
            name = "第" + episodeNum + "话 " + episodeTitle,
            scriptName = scriptName,
            sceneIndex = sceneIndex,
            dialogueIndex = dialogueIndex,
            episodeNum = episodeNum,
            episodeTitle = episodeTitle,
            previewText = TrimPreview(preview),
            isAuto = true,
            isCompleted = false,
            updatedAt = Now()
        };
        SaveAuto(slot, nb);
    }

    /// <summary>话完成：销毁该话的自动书签（从头回看不需书签）。</summary>
    public static void ClearAutoBookmark(string scriptName)
    {
        for (int i = 0; i < AutoSlotCount; i++)
        {
            var bm = LoadAuto(i);
            if (bm != null && bm.scriptName == scriptName)
            {
                PlayerPrefs.DeleteKey(AutoKey + i);
                PlayerPrefs.Save();
                return;
            }
        }
    }

    /// <summary>获取所有自动书签。</summary>
    public static List<Bookmark> GetAllAuto()
    {
        var list = new List<Bookmark>();
        for (int i = 0; i < AutoSlotCount; i++)
        {
            var bm = LoadAuto(i);
            if (bm != null) list.Add(bm);
        }
        return list;
    }

    /// <summary>查找未完成的自动书签（用于强制退出恢复提示）；无则返回 null。</summary>
    public static Bookmark FindIncompleteAuto()
    {
        for (int i = 0; i < AutoSlotCount; i++)
        {
            var bm = LoadAuto(i);
            if (bm != null && !bm.isCompleted) return bm;
        }
        return null;
    }

    /// <summary>标记自动书签已完成（话完成时先标记，若后续回看仍保留位置但不再提示）。</summary>
    public static void MarkAutoCompleted(string scriptName)
    {
        for (int i = 0; i < AutoSlotCount; i++)
        {
            var bm = LoadAuto(i);
            if (bm != null && bm.scriptName == scriptName)
            {
                bm.isCompleted = true;
                SaveAuto(i, bm);
                return;
            }
        }
    }

    // ================= 手动书签 =================

    /// <summary>添加手动书签。</summary>
    public static void AddManual(string scriptName, string episodeTitle, int episodeNum, int sceneIndex, int dialogueIndex, string preview)
    {
        string id = "bm_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
        var bm = new Bookmark
        {
            id = id,
            name = "第" + episodeNum + "话 " + episodeTitle + " · " + TrimPreview(preview),
            scriptName = scriptName,
            sceneIndex = sceneIndex,
            dialogueIndex = dialogueIndex,
            episodeNum = episodeNum,
            episodeTitle = episodeTitle,
            previewText = TrimPreview(preview),
            isAuto = false,
            isCompleted = false,
            updatedAt = Now()
        };
        PlayerPrefs.SetString(ManualKey + id, JsonUtility.ToJson(bm));
        AddToIndex(id);
        PlayerPrefs.Save();
    }

    /// <summary>获取全部手动书签（按时间倒序）。</summary>
    public static List<Bookmark> GetAllManual()
    {
        var list = new List<Bookmark>();
        foreach (string id in ManualIndex())
        {
            var bm = LoadManual(id);
            if (bm != null) list.Add(bm);
            else RemoveFromIndex(id);
        }
        list.Sort((a, b) => b.updatedAt.CompareTo(a.updatedAt));
        return list;
    }

    /// <summary>删除手动书签。</summary>
    public static void RemoveManual(string id)
    {
        PlayerPrefs.DeleteKey(ManualKey + id);
        RemoveFromIndex(id);
        PlayerPrefs.Save();
    }

    /// <summary>跳转到书签位置（故事回看，走 VN_ReplayScript 机制）。</summary>
    public static void JumpToBookmark(Bookmark bm)
    {
        if (bm == null || string.IsNullOrEmpty(bm.scriptName)) return;
        PlayerPrefs.SetString("VN_ReplayScript", bm.scriptName);
        PlayerPrefs.SetInt("VN_ReplayScene", bm.sceneIndex);
        PlayerPrefs.SetInt("VN_ReplayDialogue", bm.dialogueIndex);
        PlayerPrefs.SetInt("VN_AutoLoad", 0);
        PlayerPrefs.Save();
    }

    // ================= 索引维护 =================

    private static void AddToIndex(string id)
    {
        var list = ManualIndex();
        if (!list.Contains(id)) list.Add(id);
        PlayerPrefs.SetString(ManualIndexKey, string.Join(",", list.ToArray()));
    }

    private static void RemoveFromIndex(string id)
    {
        var list = ManualIndex();
        list.Remove(id);
        PlayerPrefs.SetString(ManualIndexKey, string.Join(",", list.ToArray()));
    }

    private static List<string> ManualIndex()
    {
        string raw = PlayerPrefs.GetString(ManualIndexKey, "");
        var list = new List<string>();
        if (!string.IsNullOrEmpty(raw))
            list.AddRange(raw.Split(','));
        return list;
    }

    // ================= 存储实现 =================

    private static void SaveAuto(int slot, Bookmark bm)
    {
        PlayerPrefs.SetString(AutoKey + slot, JsonUtility.ToJson(bm));
        PlayerPrefs.Save();
    }

    private static Bookmark LoadAuto(int slot)
    {
        string json = PlayerPrefs.GetString(AutoKey + slot, "");
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonUtility.FromJson<Bookmark>(json); }
        catch { return null; }
    }

    private static Bookmark LoadManual(string id)
    {
        string json = PlayerPrefs.GetString(ManualKey + id, "");
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonUtility.FromJson<Bookmark>(json); }
        catch { return null; }
    }

    /// <summary>滚动覆盖最旧的自动书签槽。</summary>
    private static int FindOldestAutoSlot()
    {
        long oldest = long.MaxValue;
        int slot = 0;
        for (int i = 0; i < AutoSlotCount; i++)
        {
            var bm = LoadAuto(i);
            if (bm == null) return i;
            if (bm.updatedAt < oldest) { oldest = bm.updatedAt; slot = i; }
        }
        return slot;
    }

    private static long Now() => DateTime.UtcNow.Ticks;

    private static string TrimPreview(string text)
    {
        if (string.IsNullOrEmpty(text)) return "(无文本)";
        text = text.Trim();
        if (text.Length > 40) return text.Substring(0, 40) + "...";
        return text;
    }
}