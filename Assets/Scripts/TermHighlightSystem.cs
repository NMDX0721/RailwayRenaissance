using System.Collections.Generic;

public static class TermHighlightSystem
{
    private static Dictionary<string, TermEntry> termDictionary;
    private static bool initialized;

    public static void Initialize()
    {
        if (initialized) return;
        initialized = true;

        termDictionary = new Dictionary<string, TermEntry>();

        // 预置术语
        AddTerm("沙能科技", "沙能科技", "以沙子为能源的交通技术，2050年由英国发明");
        AddTerm("沙子飞猪号", "沙子飞猪号", "第一代沙能载具，2053年商用化，可飞行的汽车");
        AddTerm("大废线", "大废线", "2072年全球铁路系统基本停运的历史事件");
        AddTerm("NF-5耕牛", "NF-5耕牛", "东风4型内燃机车，2000kW柴油机，老旧但可靠");
        AddTerm("沙能渗透", "沙能渗透", "沙能科技在特定城市的市场占有率");
        AddTerm("代际断层", "代际断层", "当城市信任度长期低于阈值时，新人口默认不乘坐铁路");
        AddTerm("雾峰村", "雾峰村", "游戏初始地点，主角长大的村庄");
        AddTerm("金日成综合大学", "金日成综合大学", "位于平壤的全球顶尖大学，主角的母校");
    }

    public static void AddTerm(string id, string name, string explanation)
    {
        termDictionary[id] = new TermEntry { id = id, name = name, explanation = explanation };
    }

    // 检测文本中的术语，返回标记后的富文本
    public static string HighlightTerms(string text)
    {
        if (!initialized) Initialize();
        string result = text;

        foreach (var kvp in termDictionary)
        {
            string term = kvp.Value.name;
            if (result.Contains(term))
            {
                // 用黄色标签包裹
                string replacement = "<color=#FFD700><link=\"" + kvp.Key + "\">" + term + "</link></color>";
                result = result.Replace(term, replacement);
            }
        }

        return result;
    }

    // 获取术语解释
    public static string GetExplanation(string termId)
    {
        if (termDictionary.ContainsKey(termId))
            return termDictionary[termId].explanation;
        return "";
    }

    // 检查文本是否包含术语
    public static bool ContainsTerm(string text, out string termId)
    {
        termId = null;
        foreach (var kvp in termDictionary)
        {
            if (text.Contains(kvp.Value.name))
            {
                termId = kvp.Key;
                return true;
            }
        }
        return false;
    }
}

public class TermEntry
{
    public string id;
    public string name;
    public string explanation;
}