using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>独立调试面板（版本号点击 5 次触发，不依赖站长日志）。</summary>
public class DebugPanel : MonoBehaviour
{
    private static DebugPanel Instance;
    private UIDocument uiDoc;
    private VisualElement overlay;
    private Font gameFont;
    private FontDefinition Fd() => new FontDefinition { font = gameFont };
    private ScrollView scroll;

    public static void Show()
    {
        if (Instance == null)
        {
            var go = new GameObject("DebugPanel");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<DebugPanel>();
            Instance.Init();
        }
        Instance.overlay.style.display = DisplayStyle.Flex;
    }

    private void Init()
    {
        gameFont = Resources.Load<Font>("Fonts/zpix");
        AchievementManager.Initialize();

        var canvasObj = new GameObject("DebugCanvas");
        DontDestroyOnLoad(canvasObj);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var panelSettings = Resources.Load<PanelSettings>("UI/TitleScreenPanelSettings");
        uiDoc = canvasObj.AddComponent<UIDocument>();
        uiDoc.panelSettings = panelSettings;
        uiDoc.visualTreeAsset = null;
        uiDoc.rootVisualElement.pickingMode = PickingMode.Ignore;

        var root = uiDoc.rootVisualElement;

        overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0; overlay.style.left = 0;
        overlay.style.right = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0.08f, 0.05f, 0.03f, 1f);
        overlay.style.display = DisplayStyle.None;
        root.Add(overlay);

        var panel = new VisualElement();
        panel.style.position = Position.Absolute;
        panel.style.top = 0; panel.style.left = 0; panel.style.right = 0; panel.style.bottom = 0;
        panel.style.backgroundColor = new Color(0.10f, 0.06f, 0.04f, 0.97f);
        panel.style.paddingLeft = 40; panel.style.paddingRight = 40;
        panel.style.paddingTop = 28; panel.style.paddingBottom = 28;
        overlay.Add(panel);

        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 16;
        panel.Add(header);

        var title = new Label("调试面板");
        title.style.fontSize = 24;
        title.style.color = new Color(1f, 0.5f, 0.3f, 1f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = Fd();
        title.style.flexGrow = 1;
        header.Add(title);

        var closeBtn = new Button(() => overlay.style.display = DisplayStyle.None) { text = "关闭" };
        closeBtn.style.width = 80;
        closeBtn.style.height = 36;
        closeBtn.style.fontSize = 17;
        closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        closeBtn.style.unityFontDefinition = Fd();
        closeBtn.style.backgroundColor = new Color(0.3f, 0.15f, 0.1f, 0.5f);
        closeBtn.style.color = new Color(1f, 0.8f, 0.4f, 1f);
        header.Add(closeBtn);

        scroll = new ScrollView(ScrollViewMode.Vertical);
        scroll.style.flexGrow = 1;
        scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        panel.Add(scroll);

        // ====== 成就管理 ======
        AddSection("成就管理");
        AddBtn("一键获取所有成就", () => { var a = AchievementManager.GetAll(); for (int i = 0; i < a.Length; i++) AchievementManager.Unlock(a[i].id); }, new Color(0.2f, 0.4f, 0.2f, 0.6f), new Color(0.6f, 1f, 0.6f, 1f));
        AddBtn("重置所有成就", () => AchievementManager.ResetAll(), new Color(0.4f, 0.2f, 0.2f, 0.6f), new Color(1f, 0.6f, 0.6f, 1f));

        var allAch = AchievementManager.GetAll();
        for (int i = 0; i < allAch.Length; i++)
        {
            var ach = allAch[i];
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            row.style.paddingLeft = 12; row.style.paddingRight = 12; row.style.paddingTop = 6; row.style.paddingBottom = 6;
            row.style.marginBottom = 4; row.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.6f);
            scroll.Add(row);

            var nl = new Label(ach.title);
            nl.style.fontSize = 17; nl.style.color = ach.unlocked ? new Color(0.5f, 1f, 0.5f, 1f) : new Color(0.6f, 0.6f, 0.6f, 0.8f);
            nl.style.unityFontDefinition = Fd(); nl.style.flexGrow = 1;
            row.Add(nl);

            var sl = new Label(ach.unlocked ? "已解锁" : "未解锁");
            sl.style.fontSize = 15; sl.style.color = ach.unlocked ? new Color(0.5f, 1f, 0.5f, 0.8f) : new Color(1f, 1f, 1f, 0.35f);
            sl.style.unityFontDefinition = Fd(); sl.style.marginRight = 12;
            row.Add(sl);

            if (!ach.unlocked)
            {
                var ub = new Button(() => { AchievementManager.Unlock(ach.id); }) { text = "解锁" };
                ub.style.width = 70; ub.style.height = 28; ub.style.fontSize = 14; ub.style.unityTextAlign = TextAnchor.MiddleCenter; ub.style.unityFontDefinition = Fd();
                row.Add(ub);
            }
            else
            {
                var rb = new Button(() => AchievementManager.ResetAll()) { text = "重置" };
                rb.style.width = 70; rb.style.height = 28; rb.style.fontSize = 14; rb.style.unityTextAlign = TextAnchor.MiddleCenter; rb.style.unityFontDefinition = Fd();
                rb.style.backgroundColor = new Color(0.4f, 0.2f, 0.2f, 0.5f); rb.style.color = new Color(1f, 0.6f, 0.6f, 1f);
                row.Add(rb);
            }
        }

        // ====== 音乐/场景/故事解锁 ======
        AddSection("音乐 & 场景 & 故事");
        AddBtn("一键解锁所有音乐", () => {
            foreach (var m in new[] { "iron_and_ash", "cloud_rail", "embers", "night_cargo", "first_light", "platform", "borderline", "wheels_joke", "train_through_keys", "south_wind", "starlit_rails", "chollima_ride", "sleepers" })
                PlayerPrefs.SetInt("ArchiveMusic_" + m, 1);
            PlayerPrefs.Save();
        }, new Color(0.2f, 0.4f, 0.2f, 0.6f), new Color(0.6f, 1f, 0.6f, 1f));

        AddBtn("一键解锁所有场景", () => {
            foreach (var s in new[] { "hangar", "lab", "professor_office", "tea_house", "car_interior", "car_interior_night", "cabin_interior", "cabin_interior_night", "station" })
                PlayerPrefs.SetInt("ArchiveScene_" + s, 1);
            PlayerPrefs.Save();
        }, new Color(0.2f, 0.4f, 0.2f, 0.6f), new Color(0.6f, 1f, 0.6f, 1f));

        AddBtn("一键解锁所有故事章节", () => {
            for (int i = 1; i <= 10; i++)
                PlayerPrefs.SetInt("ArchiveStory_prologue_0" + (i < 10 ? "0" + i : i.ToString()) + (i == 10 ? "_transition" : (i == 1 ? "_news" : (i == 2 ? "_day0" : (i == 3 ? "_journey" : (i == 4 ? "_arrival" : (i == 5 ? "_inspection" : (i == 6 ? "_team" : (i == 7 ? "_first_repair" : (i == 8 ? "_first_run" : (i == 9 ? "_funding" : ""))))))))), 1);
            PlayerPrefs.Save();
        }, new Color(0.2f, 0.4f, 0.2f, 0.6f), new Color(0.6f, 1f, 0.6f, 1f));

        // ====== PlayerPrefs 管理 ======
        AddSection("PlayerPrefs 管理");
        AddBtn("列出所有 PlayerPrefs 键", () => {
            // PlayerPrefs 无法枚举所有键，仅显示已知的关键键
            var keys = new[] { "Achievements_Data", "VN_AutoLoad", "VN_ShowLoadUI", "VN_ReplayScript", "VN_LoadSaveSlot", "ShowFPS", "VSync", "CustomFPS", "TargetFPS", "BGMVolume", "MasterVolume", "SFXVolume", "TypewriterVolume", "Stats_PlayTime", "Stats_MaxDay", "Stats_TotalPassengers", "Stats_TotalRevenue", "Stats_TotalSubsidy", "Stats_TotalExpense" };
            string msg = "=== PlayerPrefs 关键键 ===\n";
            foreach (var k in keys) msg += k + " = " + PlayerPrefs.GetString(k, "(未设置)") + "\n";
            Debug.Log(msg);
            ShowToast(msg);
        }, new Color(0.2f, 0.3f, 0.4f, 0.6f), new Color(0.6f, 0.8f, 1f, 1f));

        AddBtn("清除所有 PlayerPrefs", () => { PlayerPrefs.DeleteAll(); PlayerPrefs.Save(); ShowToast("所有 PlayerPrefs 已清除"); }, new Color(0.4f, 0.2f, 0.2f, 0.6f), new Color(1f, 0.6f, 0.6f, 1f));

        // ====== 重置游戏进度 ======
        AddSection("重置游戏进度");
        AddBtn("重置所有存档（清空全部）", () => {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            AchievementManager.ResetAll();
            ShowToast("所有存档已重置");
        }, new Color(0.4f, 0.2f, 0.2f, 0.6f), new Color(1f, 0.6f, 0.6f, 1f));
    }

    private void AddSection(string title)
    {
        var h = new Label(title);
        h.style.fontSize = 20; h.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        h.style.unityFontDefinition = Fd(); h.style.marginTop = 14; h.style.marginBottom = 8;
        scroll.Add(h);
    }

    private void AddBtn(string text, Action action, Color bg, Color fg)
    {
        var btn = new Button(() => action()) { text = text };
        btn.style.width = 300; btn.style.height = 38; btn.style.fontSize = 17;
        btn.style.unityTextAlign = TextAnchor.MiddleCenter; btn.style.unityFontDefinition = Fd();
        btn.style.backgroundColor = bg; btn.style.color = fg; btn.style.marginBottom = 8;
        btn.style.alignSelf = Align.CenterStart;
        scroll.Add(btn);
    }

    private void ShowToast(string msg)
    {
        // 简易 Toast：显示在面板底部
        var toast = new Label(msg);
        toast.style.position = Position.Absolute;
        toast.style.bottom = 20; toast.style.left = 40; toast.style.right = 40;
        toast.style.fontSize = 16; toast.style.color = new Color(1f, 1f, 0.5f, 1f);
        toast.style.backgroundColor = new Color(0, 0, 0, 0.8f);
        toast.style.paddingLeft = 16; toast.style.paddingRight = 16;
        toast.style.paddingTop = 8; toast.style.paddingBottom = 8;
        toast.style.whiteSpace = WhiteSpace.Normal;
        toast.style.unityFontDefinition = Fd();
        overlay.Add(toast);
        overlay.schedule.Execute(() => overlay.Remove(toast)).StartingIn(3000);
    }
}