using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>术语高亮：从 Resources/Terms.json 载入词条（字段见 参考资料/设定/铁路术语词条.md §二），
/// 富文本黄金加粗包裹；匹配按词条名长度降序（防「会让」吃掉「会让站」这类子串冲突）。
/// 接线点：DialogueBox.ShowDialogue——独白整句蓝斜体不叠高亮，避免嵌套 color 截断外层色。</summary>
public static class TermHighlightSystem
{
    private static readonly Dictionary<string, TermEntry> termDictionary = new Dictionary<string, TermEntry>();
    private static readonly List<string> namesByLenDesc = new List<string>();
    private static bool initialized;

    [System.Serializable] public class TermJsonData { public TermJsonItem[] terms; }
    [System.Serializable] public class TermJsonItem { public string id; public string name; public TermExplanation[] explanations; }

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

        // 2) 旧架空词条（单句解释，无角色口吻；后续可并入 JSON）
        RegisterLore("sand_energy", "沙能科技", "以沙子为能源的交通技术，2050年由英国发明");
        RegisterLore("sand_pig", "沙子飞猪号", "第一代沙能载具，2053年商用化，可飞行的汽车");
        RegisterLore("great_dereg", "大废线", "2072年全球铁路系统基本停运的历史事件");
        RegisterLore("nf5", "NF-5耕牛", "东风4型内燃机车，2000kW柴油机，老旧但可靠");
        RegisterLore("sand_penetration", "沙能渗透", "沙能科技在特定城市的市场占有率");
        RegisterLore("generational_gap", "代际断层", "当城市信任度长期低于阈值时，新人口默认不乘坐铁路");
        RegisterLore("wufeng_village", "雾峰村", "游戏初始地点，主角长大的村庄");
        RegisterLore("kuwon_univ", "金日成综合大学", "位于平壤的全球顶尖大学，主角的母校");
    }

    private static void Register(TermEntry entry)
    {
        string key = string.IsNullOrEmpty(entry.id) ? entry.name : entry.id;
        termDictionary[key] = entry;
        if (!namesByLenDesc.Contains(entry.name))
        {
            namesByLenDesc.Add(entry.name);
            namesByLenDesc.Sort((a, b) => b.Length != a.Length
                ? b.Length.CompareTo(a.Length)
                : string.CompareOrdinal(a, b));
        }
    }

    private static void RegisterLore(string id, string name, string explanation)
    {
        var entry = new TermEntry { id = id, name = name, explanations = new List<TermExplanation>() };
        entry.explanations.Add(new TermExplanation { speaker = "", text = explanation });
        Register(entry);
    }

    /// <summary>黄金加粗包裹文本中的术语。已有 &lt;&gt; 富文本标签区原样跳过、不回扫，
    /// 从左到右贪心、每次取当前位最长匹配（长度降序表保证）。</summary>
    public static string HighlightTerms(string text)
    {
        if (!initialized) Initialize();
        if (string.IsNullOrEmpty(text) || namesByLenDesc.Count == 0) return text;

        var sb = new StringBuilder(text.Length + 64);
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            if (c == '<') // 已有富文本标签原样跳过
            {
                int close = text.IndexOf('>', i);
                if (close != -1) { sb.Append(text, i, close - i + 1); i = close + 1; continue; }
            }
            string matched = null;
            for (int k = 0; k < namesByLenDesc.Count; k++)
            {
                string n = namesByLenDesc[k];
                if (n.Length > 0 && i + n.Length <= text.Length &&
                    string.CompareOrdinal(text, i, n, 0, n.Length) == 0)
                { matched = n; break; }
            }
            if (matched != null)
            {
                sb.Append("<color=#FFD700>").Append(matched).Append("</color>");   // 黄字，下划线由对话框命中层画
                i += matched.Length;
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }
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
}

[System.Serializable]
public class TermExplanation
{
    public string speaker;
    public string text;
}
