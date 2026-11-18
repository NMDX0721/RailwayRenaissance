using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>术语高亮：从 Resources/Terms.json 载入词条。共享匹配器 FindOccurrences：
/// 长度降序防子串（会让 vs 会让站）+ skipWhen 全词组抑制（防「铁路基本」误匹配「路基」）。
/// 接线点：DialogueBox.ShowDialogue（黄字+下划线命中层）；独白整句蓝斜体不叠高亮。</summary>
public static class TermHighlightSystem
{
    private static readonly Dictionary<string, TermEntry> termDictionary = new Dictionary<string, TermEntry>();
    private static readonly Dictionary<string, TermEntry> nameToEntry = new Dictionary<string, TermEntry>();
    private static readonly List<string> namesByLenDesc = new List<string>();
    private static bool initialized;

    [System.Serializable] public class TermJsonData { public TermJsonItem[] terms; }
    [System.Serializable] public class TermJsonItem { public string id; public string name; public string py; public string[] skipWhen; public TermExplanation[] explanations; }

    public static void Initialize()
    {
        if (initialized) return;
        initialized = true;

        // 1) 铁路技术词条（30 条，角色口吻双解释）
        var asset = Resources.Load<TextAsset>("Terms");
        if (asset != null)
        {
            try
            {
                var data = JsonUtility.FromJson<TermJsonData>(asset.text);
                if (data != null && data.terms != null)
                {
                    foreach (var t in data.terms)
                    {
                        if (t == null || string.IsNullOrEmpty(t.name)) continue;
                        var entry = new TermEntry { id = t.id, name = t.name, explanations = new List<TermExplanation>() };
                        if (t.explanations != null) entry.explanations.AddRange(t.explanations);
                        if (t.skipWhen != null && t.skipWhen.Length > 0)
                            entry.skipWhen = new List<string>(t.skipWhen);
                        Register(entry);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Terms] Terms.json parse failed: " + ex.Message);
            }
        }
        else
        {
            Debug.LogWarning("[Terms] Resources/Terms.json missing");
        }

        // 2) 世界观/线路/车辆词条：直接取自媒姬百科的手写条目（Resources/Wiki/wiki_articles.json 里带 tips 名单的条目）。
        //    id 与百科条目一致，小卡上的「百科」按钮才能跳到对应条目；解释用条目的副标题（一句话，不剧透正文）。
        var wiki = Resources.Load<TextAsset>("Wiki/wiki_articles");
        if (wiki != null)
        {
            try
            {
                var data = JsonUtility.FromJson<WikiTipRoot>(wiki.text);
                if (data != null && data.articles != null)
                {
                    foreach (var a in data.articles)
                    {
                        if (a == null || string.IsNullOrEmpty(a.id) || a.tips == null || a.tips.Length == 0) continue;
                        if (termDictionary.ContainsKey(a.id)) continue;
                        var entry = new TermEntry { id = a.id, name = string.IsNullOrEmpty(a.title) ? a.tips[0] : a.title, explanations = new List<TermExplanation>() };
                        entry.explanations.Add(new TermExplanation { speaker = "", text = string.IsNullOrEmpty(a.subtitle) ? entry.name : a.subtitle });
                        termDictionary[a.id] = entry;
                        foreach (var n in a.tips) RegisterName(n, entry);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Terms] wiki_articles.json parse failed: " + ex.Message);
            }
        }
    }

    [System.Serializable] private class WikiTipRoot { public WikiTipItem[] articles; }
    [System.Serializable] private class WikiTipItem { public string id; public string title; public string subtitle; public string[] tips; }

    /// <summary>给词条登记一个可命中的写法（一个词条可以有多个写法，如「耕牛」「NF-5」）。已被别的词条占用的写法不覆盖。</summary>
    private static void RegisterName(string name, TermEntry entry)
    {
        if (string.IsNullOrEmpty(name) || nameToEntry.ContainsKey(name)) return;
        nameToEntry[name] = entry;
        namesByLenDesc.Add(name);
        namesByLenDesc.Sort((x, y) => y.Length != x.Length ? y.Length.CompareTo(x.Length) : string.CompareOrdinal(x, y));
    }

    private static void Register(TermEntry entry)
    {
        string key = string.IsNullOrEmpty(entry.id) ? entry.name : entry.id;
        termDictionary[key] = entry;
        if (!string.IsNullOrEmpty(entry.name)) nameToEntry[entry.name] = entry;
        if (!namesByLenDesc.Contains(entry.name))
        {
            namesByLenDesc.Add(entry.name);
            namesByLenDesc.Sort((a, b) => b.Length != a.Length
                ? b.Length.CompareTo(a.Length)
                : string.CompareOrdinal(a, b));
        }
    }

    /// <summary>黄金加粗包裹文本中的术语。已有 &lt;&gt; 富文本标签区原样跳过、不回扫，
    /// 从左到右贪心、每次取当前位最长匹配（长度降序表保证）。</summary>
    /// <summary>共享匹配器：从左到右、标签外匹配、每次取最长词条；skipWhen 全词组命中的实例抑制。
    /// 返回 (起点, 长度, id)——高亮层与对话框命中层共用，保证两处判定完全一致。</summary>
    public static List<(int start, int len, string id)> FindOccurrences(string text)
    {
        var res = new List<(int, int, string)>();
        if (!initialized) Initialize();
        if (string.IsNullOrEmpty(text) || namesByLenDesc.Count == 0) return res;
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            if (c == '<') // 富文本标签内不匹配
            {
                int close = text.IndexOf('>', i);
                if (close != -1) { i = close + 1; continue; }
            }
            string matched = null;
            for (int k = 0; k < namesByLenDesc.Count; k++)
            {
                string n = namesByLenDesc[k];
                if (n.Length > 0 && i + n.Length <= text.Length &&
                    string.CompareOrdinal(text, i, n, 0, n.Length) == 0)
                { matched = n; break; }
            }
            if (matched == null) { i++; continue; }
            int len = matched.Length;
            if (!IsSuppressed(text, i, matched))
                res.Add((i, len, nameToEntry.TryGetValue(matched, out var e) ? e.id : matched));
            i += len;
        }
        return res;
    }

    /// <summary>skipWhen 全词组抑制：如词条「路基」在「铁路基本」内出现时整段跳过。</summary>
    private static bool IsSuppressed(string text, int s, string name)
    {
        if (!nameToEntry.TryGetValue(name, out var e) || e.skipWhen == null) return false;
        foreach (var phrase in e.skipWhen)
        {
            if (string.IsNullOrEmpty(phrase)) continue;
            int k = phrase.IndexOf(name, System.StringComparison.Ordinal);
            if (k < 0) continue;
            if (s >= k && s + phrase.Length <= text.Length &&
                string.CompareOrdinal(text, s - k, phrase, 0, phrase.Length) == 0)
                return true;
        }
        return false;
    }

    /// <summary>文本包黄（下划线由对话框命中层画）；结果与 FindOccurrences 完全一致。</summary>
    /// <param name="withLinks">true：再套 &lt;link="词条id"&gt;，供回顾等无命中层的 Label 用 PointerUpLinkTagEvent 取点击。</param>
    /// <param name="underlineId">非空：该词条的所有出现处再套 &lt;u&gt; 下划线（回顾里悬停反馈）。</param>
    public static string HighlightTerms(string text, bool withLinks = false, string underlineId = null)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var occ = FindOccurrences(text);
        if (occ.Count == 0) return text;
        var sb = new StringBuilder(text.Length + 64);
        int last = 0;
        foreach (var (s, len, id) in occ)
        {
            if (s < last) continue;
            sb.Append(text, last, s - last);
            if (withLinks) sb.Append("<link=\"").Append(id).Append("\">");
            bool ul = underlineId != null && id == underlineId;
            sb.Append("<color=#FFD700>");
            if (ul) sb.Append("<u>");
            sb.Append(text, s, len);
            if (ul) sb.Append("</u>");
            sb.Append("</color>");
            if (withLinks) sb.Append("</link>");
            last = s + len;
        }
        sb.Append(text, last, text.Length - last);
        return sb.ToString();
    }

    /// <summary>词条解释：多角色按行拼接（老陈：…/张工：…）；无角色的旧词条返回原句。</summary>
    public static string GetExplanation(string termId)
    {
        if (!initialized) Initialize();
        if (!termDictionary.TryGetValue(termId, out var e) || e.explanations == null || e.explanations.Count == 0)
            return "";
        var s = new StringBuilder();
        foreach (var x in e.explanations)
        {
            if (s.Length > 0) s.Append('\n');
            if (!string.IsNullOrEmpty(x.speaker)) s.Append(x.speaker).Append("：");
            s.Append(x.text);
        }
        return s.ToString();
    }

    public static bool ContainsTerm(string text, out string termId)
    {
        if (!initialized) Initialize();
        termId = null;
        if (string.IsNullOrEmpty(text)) return false;
        foreach (var kvp in termDictionary)
        {
            if (!string.IsNullOrEmpty(kvp.Value.name) && text.Contains(kvp.Value.name))
            {
                termId = kvp.Key;
                return true;
            }
        }
        return false;
    }

    /// <summary>给 UI 用的 (termId, 术语名) 全表（命中层扫描用）。</summary>
    public static System.Collections.Generic.List<(string id, string name)> GetTerms()
    {
        if (!initialized) Initialize();
        var list = new System.Collections.Generic.List<(string, string)>();
        foreach (var kv in termDictionary)
            if (!string.IsNullOrEmpty(kv.Value.name))
                list.Add((kv.Key, kv.Value.name));
        return list;
    }

    /// <summary>按 id 取词条（小卡渲染用）；不存在返回 null。</summary>
    public static TermEntry GetEntry(string id)
    {
        if (!initialized) Initialize();
        return !string.IsNullOrEmpty(id) && termDictionary.TryGetValue(id, out var e) ? e : null;
    }
}

public class TermEntry
{
    public string id;
    public string name;
    public List<TermExplanation> explanations;
    public List<string> skipWhen;   // 子串误匹配抑制（全词组，如「铁路基本」）
}

[System.Serializable]
public class TermExplanation
{
    public string speaker;
    public string text;
}
