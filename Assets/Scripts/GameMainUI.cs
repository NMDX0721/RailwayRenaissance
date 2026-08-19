using UnityEngine;
using UnityEngine.UIElements;

/// <summary>模拟经营主界面（淡蓝背景，未来放置经营 UI）。</summary>
public class GameMainUI : MonoBehaviour
{
    private static GameMainUI Instance;
    private UIDocument uiDoc;
    private VisualElement overlay;
    private Font gameFont;
    private FontDefinition Fd() => new FontDefinition { font = gameFont };

    public static void Show()
    {
        if (Instance == null)
        {
            var go = new GameObject("GameMainUI");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<GameMainUI>();
            Instance.Init();
        }
        Instance.overlay.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        if (overlay != null) overlay.style.display = DisplayStyle.None;
    }

    private void Init()
    {
        gameFont = Resources.Load<Font>("Fonts/zpix");
        BuildDocument();
        BuildUI();
    }

    private void BuildDocument()
    {
        var canvasObj = new GameObject("GameMainCanvas");
        DontDestroyOnLoad(canvasObj);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var panelSettings = Resources.Load<PanelSettings>("UI/TitleScreenPanelSettings");
        uiDoc = canvasObj.AddComponent<UIDocument>();
        uiDoc.panelSettings = panelSettings;
        uiDoc.visualTreeAsset = null;
        uiDoc.rootVisualElement.pickingMode = PickingMode.Ignore;

        overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0; overlay.style.left = 0;
        overlay.style.right = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0.6f, 0.8f, 1f, 1f); // 淡蓝
        overlay.style.display = DisplayStyle.None;
        uiDoc.rootVisualElement.Add(overlay);
    }

    private void BuildUI()
    {
        var root = overlay;

        // 右下角事务按钮
        var affairsBtn = new Button(ShowAffairsPage) { text = "事务" };
        affairsBtn.style.position = Position.Absolute;
        affairsBtn.style.bottom = 40;
        affairsBtn.style.right = 40;
        affairsBtn.style.width = 80;
        affairsBtn.style.height = 80;
        affairsBtn.style.fontSize = 22;
        affairsBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        affairsBtn.style.unityFontDefinition = Fd();
        affairsBtn.style.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.8f);
        affairsBtn.style.color = new Color(0.9f, 0.9f, 1f, 1f);
        affairsBtn.style.borderTopLeftRadius = 12;
        affairsBtn.style.borderTopRightRadius = 12;
        affairsBtn.style.borderBottomLeftRadius = 12;
        affairsBtn.style.borderBottomRightRadius = 12;
        root.Add(affairsBtn);
    }

    private void ShowAffairsPage()
    {
        // 事务页面：半透明遮罩 + 选项列表
        var affairsOverlay = new VisualElement();
        affairsOverlay.style.position = Position.Absolute;
        affairsOverlay.style.top = 0; affairsOverlay.style.left = 0;
        affairsOverlay.style.right = 0; affairsOverlay.style.bottom = 0;
        affairsOverlay.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        affairsOverlay.style.alignItems = Align.Center;
        affairsOverlay.style.justifyContent = Justify.Center;
        affairsOverlay.pickingMode = PickingMode.Position;
        affairsOverlay.RegisterCallback<ClickEvent>(e => { if (e.target == affairsOverlay) overlay.Remove(affairsOverlay); });
        overlay.Add(affairsOverlay);

        var panel = new VisualElement();
        panel.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.95f);
        panel.style.borderTopWidth = 2; panel.style.borderBottomWidth = 2;
        panel.style.borderLeftWidth = 2; panel.style.borderRightWidth = 2;
        panel.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        panel.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        panel.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        panel.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        panel.style.borderTopLeftRadius = 10; panel.style.borderTopRightRadius = 10;
        panel.style.borderBottomLeftRadius = 10; panel.style.borderBottomRightRadius = 10;
        panel.style.width = 400;
        panel.style.paddingLeft = 20; panel.style.paddingRight = 20;
        panel.style.paddingTop = 16; panel.style.paddingBottom = 16;
        panel.style.flexDirection = FlexDirection.Column;
        affairsOverlay.Add(panel);

        var title = new Label("事务");
        title.style.fontSize = 24;
        title.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = Fd();
        title.style.marginBottom = 16;
        panel.Add(title);

        // 剧情 按钮
        var storyBtn = new Button(ShowStoryPage) { text = "剧情" };
        storyBtn.style.width = 200; storyBtn.style.height = 50;
        storyBtn.style.fontSize = 20; storyBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        storyBtn.style.unityFontDefinition = Fd();
        storyBtn.style.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.8f);
        storyBtn.style.color = new Color(0.9f, 0.9f, 1f, 1f);
        storyBtn.style.marginBottom = 10;
        storyBtn.style.alignSelf = Align.Center;
        panel.Add(storyBtn);
    }

    private void ShowStoryPage()
    {
        // 剧情页面：半透明遮罩 + Main Story 按钮
        var storyOverlay = new VisualElement();
        storyOverlay.style.position = Position.Absolute;
        storyOverlay.style.top = 0; storyOverlay.style.left = 0;
        storyOverlay.style.right = 0; storyOverlay.style.bottom = 0;
        storyOverlay.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        storyOverlay.style.alignItems = Align.Center;
        storyOverlay.style.justifyContent = Justify.Center;
        storyOverlay.pickingMode = PickingMode.Position;
        storyOverlay.RegisterCallback<ClickEvent>(e => { if (e.target == storyOverlay) overlay.Remove(storyOverlay); });
        overlay.Add(storyOverlay);

        var panel = new VisualElement();
        panel.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.95f);
        panel.style.borderTopWidth = 2; panel.style.borderBottomWidth = 2;
        panel.style.borderLeftWidth = 2; panel.style.borderRightWidth = 2;
        panel.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        panel.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        panel.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        panel.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        panel.style.borderTopLeftRadius = 10; panel.style.borderTopRightRadius = 10;
        panel.style.borderBottomLeftRadius = 10; panel.style.borderBottomRightRadius = 10;
        panel.style.width = 400;
        panel.style.paddingLeft = 20; panel.style.paddingRight = 20;
        panel.style.paddingTop = 16; panel.style.paddingBottom = 16;
        panel.style.flexDirection = FlexDirection.Column;
        storyOverlay.Add(panel);

        var title = new Label("剧情");
        title.style.fontSize = 24;
        title.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = Fd();
        title.style.marginBottom = 16;
        panel.Add(title);

        // Main Story 按钮
        var mainStoryBtn = new Button(() => { MainStoryUI.Show(); }) { text = "Main Story" };
        mainStoryBtn.style.width = 200; mainStoryBtn.style.height = 50;
        mainStoryBtn.style.fontSize = 20; mainStoryBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        mainStoryBtn.style.unityFontDefinition = Fd();
        mainStoryBtn.style.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.8f);
        mainStoryBtn.style.color = new Color(0.9f, 0.9f, 1f, 1f);
        mainStoryBtn.style.alignSelf = Align.Center;
        panel.Add(mainStoryBtn);
    }
}