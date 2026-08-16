using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// P5.1 招聘 UI 面板：三渠道招募（社区推荐/广告招聘/猎头推荐）、渠道冷却展示、
/// 资金不足提示、当前员工列表、招募结果反馈。
/// 全部 UGUI 元素由代码动态构建（不依赖预制体），挂载至经营场景（与 schedulePanel 同级），初始隐藏。
/// 外部通过 Create()/Show()/Hide() 控制显隐，招募经 CrewManager.RecruitByChannel 完成。
/// </summary>
public class RecruitmentUI : MonoBehaviour
{
    [SerializeField] private Text titleText;
    [SerializeField] private Text crewListText;
    [SerializeField] private Text feedbackText;
    [SerializeField] private Text skillTreeText;
    [SerializeField] private Button communityBtn, adBtn, headhunterBtn;
    [SerializeField] private Text communityCooldown, adCooldown, headhunterCooldown;

    private GameObject panelRoot;
    private Font runtimeFont;
    private bool initialized;

    private readonly Button[] channelButtons = new Button[3];
    private readonly Text[] channelCooldownTexts = new Text[3];

    private static readonly Color NormalColor = Color.white;
    private static readonly Color DisabledColor = new Color(0.55f, 0.55f, 0.55f);

    private sealed class ChannelSpec
    {
        public string Key;
        public string Label;
        public string DisplayName;
        public int Cost;

        public ChannelSpec(string key, string label, string displayName, int cost)
        {
            Key = key;
            Label = label;
            DisplayName = displayName;
            Cost = cost;
        }
    }

    private static readonly ChannelSpec[] Channels = new[]
    {
        new ChannelSpec("community", "社区推荐\n免费", "社区推荐", 0),
        new ChannelSpec("ad", "广告招聘\n500沙币", "广告招聘", 500),
        new ChannelSpec("headhunter", "猎头推荐\n2000沙币", "猎头推荐", 2000)
    };

    /// <summary>便捷入口：创建招聘面板 GameObject（加到 parent 下）并初始化。parent 传 schedulePanel 的同级父节点。</summary>
    public static RecruitmentUI Create(Transform parent, Font font)
    {
        GameObject go = new GameObject("RecruitmentPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        go.transform.SetParent(parent, false);

        RecruitmentUI ui = go.AddComponent<RecruitmentUI>();
        ui.Init(parent, font);
        return ui;
    }

    /// <summary>动态构建全部 UI（参考 RailRevivalRuntimeBootstrap 的 EnsureOverlayPanel/EnsureButton/EnsureText 风格）。</summary>
    public void Init(Transform parent, Font font)
    {
        if (initialized)
        {
            return;
        }
        initialized = true;

        panelRoot = gameObject;
        transform.SetParent(parent, false);
        transform.localScale = Vector3.one;

        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(560f, 560f);

        Image image = panelRoot.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.85f);

        runtimeFont = font != null ? font : GetRuntimeFont();

        titleText = CreateText("TitleText", 18);
        titleText.alignment = TextAnchor.UpperCenter;
        titleText.text = "招聘人才";
        SetRect(titleText.rectTransform, 20f, -20f, 480f, 36f);

        // 三渠道招募按钮 + 冷却文本
        for (int i = 0; i < Channels.Length; i++)
        {
            float x = 20f + i * 165f;

            channelButtons[i] = CreateButton("Btn_" + Channels[i].Key, Channels[i].Label);
            SetButtonRect(channelButtons[i], x, -70f, 150f, 46f);

            string key = Channels[i].Key;
            channelButtons[i].onClick.AddListener(() => OnRecruit(key));

            channelCooldownTexts[i] = CreateText("Cooldown_" + Channels[i].Key, 12);
            channelCooldownTexts[i].alignment = TextAnchor.UpperCenter;
            SetRect(channelCooldownTexts[i].rectTransform, x, -120f, 150f, 22f);
        }

        communityBtn = channelButtons[0];
        adBtn = channelButtons[1];
        headhunterBtn = channelButtons[2];
        communityCooldown = channelCooldownTexts[0];
        adCooldown = channelCooldownTexts[1];
        headhunterCooldown = channelCooldownTexts[2];

        feedbackText = CreateText("FeedbackText", 13);
        feedbackText.alignment = TextAnchor.UpperCenter;
        feedbackText.color = new Color(1f, 0.85f, 0.3f);
        feedbackText.text = "选择渠道开始招聘。";
        SetRect(feedbackText.rectTransform, 20f, -150f, 480f, 26f);

        crewListText = CreateText("CrewListText", 12);
        crewListText.supportRichText = true;
        crewListText.alignment = TextAnchor.UpperLeft;
        crewListText.horizontalOverflow = HorizontalWrapMode.Wrap;
        crewListText.verticalOverflow = VerticalWrapMode.Overflow;
        SetRect(crewListText.rectTransform, 20f, -184f, 520f, 180f);

        skillTreeText = CreateText("SkillTreeText", 11);
        skillTreeText.supportRichText = true;
        skillTreeText.alignment = TextAnchor.UpperLeft;
        skillTreeText.horizontalOverflow = HorizontalWrapMode.Wrap;
        skillTreeText.verticalOverflow = VerticalWrapMode.Overflow;
        skillTreeText.color = new Color(0.7f, 0.9f, 1.0f);
        SetRect(skillTreeText.rectTransform, 20f, -370f, 520f, 100f);

        Button closeBtn = CreateButton("Btn_Close", "关闭");
        closeBtn.onClick.AddListener(Hide);
        SetButtonRect(closeBtn, 460f, -520f, 84f, 34f);

        panelRoot.SetActive(false);
        Refresh();
    }

    public void Show()
    {
        if (!initialized)
        {
            return;
        }

        panelRoot.SetActive(true);
        if (feedbackText != null)
        {
            feedbackText.text = "选择渠道开始招募，不同渠道的花费与冷却各不相同。";
        }
        Refresh();
    }

    public void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    /// <summary>刷新冷却、列表与按钮可用状态（不影响已显示的反馈文本）。</summary>
    public void Refresh()
    {
        if (!initialized)
        {
            return;
        }

        UpdateChannelStates();
        UpdateCrewList();
    }

    private void UpdateChannelStates()
    {
        Dictionary<string, int> cooldowns = CrewManager.GetChannelCooldowns();
        int money = GameData.GetMoney();

        for (int i = 0; i < Channels.Length; i++)
        {
            ChannelSpec spec = Channels[i];

            int remaining = 0;
            if (cooldowns.TryGetValue(spec.Key, out int days) && days > 0)
            {
                remaining = days;
            }

            channelCooldownTexts[i].text = remaining > 0 ? "冷却剩余 " + remaining + " 天" : "今日可招募";

            bool ready = remaining <= 0;
            bool affordable = money >= spec.Cost;
            Text labelText = channelButtons[i].GetComponentInChildren<Text>(true);
            if (labelText != null)
            {
                // 冷却中或资金不足 → 按钮置灰（仍可点击，点击时给出具体提示）
                labelText.color = (ready && affordable) ? NormalColor : DisabledColor;
            }
        }
    }

    private void UpdateCrewList()
    {
        List<string> lines = new List<string> { "—— 当前员工 ——" };
        foreach (CrewMember member in CrewManager.GetAllCrew())
        {
            if (member == null)
            {
                continue;
            }
            lines.Add(member.name + " - " + GetRoleName(member.role) + " - " + CrewManager.GetCrewStatusText(member.id));
        }
        crewListText.text = string.Join("\n", lines);
    }

    private void OnRecruit(string channel)
    {
        if (!initialized || feedbackText == null)
        {
            return;
        }

        int cost = GetCost(channel);
        Dictionary<string, int> cooldowns = CrewManager.GetChannelCooldowns();
        bool cooling = cooldowns.TryGetValue(channel, out int remaining) && remaining > 0;

        if (cooling)
        {
            feedbackText.text = GetChannelDisplayName(channel) + "冷却中，剩余 " + remaining + " 天。";
            UpdateChannelStates();
            return;
        }

        if (GameData.GetMoney() < cost)
        {
            feedbackText.text = "资金不足，" + GetChannelDisplayName(channel) + "需要 " + cost + " 沙币。";
            UpdateChannelStates();
            return;
        }

        bool ok = CrewManager.RecruitByChannel(channel);
        if (ok)
        {
            feedbackText.text = "招募成功！新员工已加入团队。";

            // 显示新员工的技能树
            var allCrew = CrewManager.GetAllCrew();
            if (allCrew != null && allCrew.Count > 0)
            {
                CrewMember newest = allCrew[allCrew.Count - 1];
                DisplaySkillTree(newest);
            }

            // 同步顶部资金显示（不修改 UIManager，仅调用其公开刷新接口）
            UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateStatusBar();
            }
        }
        else
        {
            feedbackText.text = "招募失败，请稍后再试。";
        }

        Refresh();
    }

    private void DisplaySkillTree(CrewMember member)
    {
        if (skillTreeText == null || member == null) return;

        string tree = "—— 技能树 ——\n";
        if (member.skillTree != null)
        {
            foreach (var node in member.skillTree)
            {
                tree += $"<color=#FFD700>{node.systemName}</color> ({node.parentSkillLevel:F0})\n";
                if (node.subSkills != null)
                {
                    foreach (var sub in node.subSkills)
                    {
                        string status = sub.isUnlocked ? $"{sub.level:F0}" : "<color=#888>锁定</color>";
                        tree += $"  └ {sub.skillName}: {status}\n";
                    }
                }
            }
        }
        else
        {
            tree += "（无技能树数据）\n";
        }
        skillTreeText.text = tree;
    }

    private static int GetCost(string channel)
    {
        for (int i = 0; i < Channels.Length; i++)
        {
            if (Channels[i].Key == channel)
            {
                return Channels[i].Cost;
            }
        }
        return 0;
    }

    private static string GetChannelDisplayName(string channel)
    {
        for (int i = 0; i < Channels.Length; i++)
        {
            if (Channels[i].Key == channel)
            {
                return Channels[i].DisplayName;
            }
        }
        return channel;
    }

    private static string GetRoleName(string role)
    {
        switch (role)
        {
            case "driver":     return "司机";
            case "mechanic":   return "机械师";
            case "conductor":  return "乘务员";
            case "dispatcher": return "调度员";
            case "attendant":  return "服务员";
            default:           return role;
        }
    }

    private Text CreateText(string name, int fontSize)
    {
        GameObject textGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(transform, false);

        Text text = textGo.GetComponent<Text>();
        text.font = runtimeFont;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.supportRichText = true;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rect = text.rectTransform;
        rect.localScale = Vector3.one;
        SetRect(rect, 0f, 0f, 100f, 30f);
        return text;
    }

    private Button CreateButton(string name, string label)
    {
        GameObject buttonGo = DefaultControls.CreateButton(new DefaultControls.Resources());
        buttonGo.name = name;
        buttonGo.transform.SetParent(transform, false);

        Text labelText = buttonGo.GetComponentInChildren<Text>(true);
        if (labelText != null)
        {
            labelText.font = runtimeFont;
            labelText.fontSize = 14;
            labelText.text = label;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;
        }

        return buttonGo.GetComponent<Button>();
    }

    private static void SetButtonRect(Button button, float x, float y, float width, float height)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void SetRect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static Font GetRuntimeFont()
    {
        Font font = null;

        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
        }

        if (font == null)
        {
            try
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch
            {
            }
        }

        return font;
    }
}