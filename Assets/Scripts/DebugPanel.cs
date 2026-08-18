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

        // 自建 UIDocument
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

        // 全屏遮罩
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

        // 标题 + 关闭
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

        // 内容区
        var scroll = new ScrollView(ScrollViewMode.Vertical);
        scroll.style.flexGrow = 1;
        scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        panel.Add(scroll);

        // 成就管理
        var achHeader = new Label("成就管理");
        achHeader.style.fontSize = 20;
        achHeader.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        achHeader.style.unityFontDefinition = Fd();
        achHeader.style.marginTop = 10;
        achHeader.style.marginBottom = 8;
        scroll.Add(achHeader);

        // 一键获取所有成就
        var unlockAllBtn = new Button(() =>
        {
            var all = AchievementManager.GetAll();
            for (int i = 0; i < all.Length; i++)
                AchievementManager.Unlock(all[i].id);
        }) { text = "一键获取所有成就" };
        StyleBtn(unlockAllBtn, new Color(0.2f, 0.4f, 0.2f, 0.6f), new Color(0.6f, 1f, 0.6f, 1f));
        scroll.Add(unlockAllBtn);

        // 重置所有成就
        var resetAllBtn = new Button(() => AchievementManager.ResetAll()) { text = "重置所有成就" };
        StyleBtn(resetAllBtn, new Color(0.4f, 0.2f, 0.2f, 0.6f), new Color(1f, 0.6f, 0.6f, 1f));
        scroll.Add(resetAllBtn);

        // 单个成就
        var allAch = AchievementManager.GetAll();
        for (int i = 0; i < allAch.Length; i++)
        {
            var ach = allAch[i];
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 12; row.style.paddingRight = 12;
            row.style.paddingTop = 6; row.style.paddingBottom = 6;
            row.style.marginBottom = 4;
            row.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.6f);
            scroll.Add(row);

            var nameLabel = new Label(ach.title);
            nameLabel.style.fontSize = 17;
            nameLabel.style.color = ach.unlocked ? new Color(0.5f, 1f, 0.5f, 1f) : new Color(0.6f, 0.6f, 0.6f, 0.8f);
            nameLabel.style.unityFontDefinition = Fd();
            nameLabel.style.flexGrow = 1;
            row.Add(nameLabel);

            var statusLabel = new Label(ach.unlocked ? "已解锁" : "未解锁");
            statusLabel.style.fontSize = 15;
            statusLabel.style.color = ach.unlocked ? new Color(0.5f, 1f, 0.5f, 0.8f) : new Color(1f, 1f, 1f, 0.35f);
            statusLabel.style.unityFontDefinition = Fd();
            statusLabel.style.marginRight = 12;
            row.Add(statusLabel);

            if (!ach.unlocked)
            {
                var unlockBtn = new Button(() =>
                {
                    AchievementManager.Unlock(ach.id);
                    RebuildRow(row, ach);
                }) { text = "解锁" };
                unlockBtn.style.width = 70; unlockBtn.style.height = 28;
                unlockBtn.style.fontSize = 14; unlockBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
                unlockBtn.style.unityFontDefinition = Fd();
                row.Add(unlockBtn);
            }
            else
            {
                var resetBtn = new Button(() =>
                {
                    AchievementManager.ResetAll();
                    // 简单刷新：重新初始化
                }) { text = "重置" };
                resetBtn.style.width = 70; resetBtn.style.height = 28;
                resetBtn.style.fontSize = 14; resetBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
                resetBtn.style.unityFontDefinition = Fd();
                resetBtn.style.backgroundColor = new Color(0.4f, 0.2f, 0.2f, 0.5f);
                resetBtn.style.color = new Color(1f, 0.6f, 0.6f, 1f);
                row.Add(resetBtn);
            }
        }
    }

    private void RebuildRow(VisualElement row, AchievementData ach)
    {
        // 重新初始化（简化处理）
        AchievementManager.ResetAll();
    }

    private void StyleBtn(Button btn, Color bg, Color fg)
    {
        btn.style.width = 240; btn.style.height = 38;
        btn.style.fontSize = 17; btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.style.unityFontDefinition = Fd();
        btn.style.backgroundColor = bg;
        btn.style.color = fg;
        btn.style.marginBottom = 10;
    }
}