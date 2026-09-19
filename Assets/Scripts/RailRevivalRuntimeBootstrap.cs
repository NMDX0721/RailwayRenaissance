using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RailRevivalRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapActiveScene()
    {
        // 平台适配：禁用Android系统返回键退出游戏
        Input.backButtonLeavesApp = false;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "StationSlice_V1")
        {
            return;
        }

        Canvas canvas = EnsureCanvas(scene);
        EnsureEventSystem(scene);
        EnsureMainCamera(scene);

        GameObject topBar = EnsurePanel(canvas.transform, "TopBar");
        GameObject rightPanel = EnsurePanel(canvas.transform, "RightPanel");
        GameObject bottomActions = EnsurePanel(canvas.transform, "BottomActions");
        LayoutRootPanels(canvas.GetComponent<RectTransform>(), topBar.GetComponent<RectTransform>(), rightPanel.GetComponent<RectTransform>(), bottomActions.GetComponent<RectTransform>());

        GameObject statusHints = FindChild(canvas.transform, "StatusHints");
        if (statusHints != null)
        {
            statusHints.SetActive(false);
        }

        RuntimeUiRefs ui = EnsureUi(topBar.transform, rightPanel.transform, bottomActions.transform, canvas.transform);

        GameObject managers = EnsureRoot(scene, "Managers");
        GameObject uiManagerGo = EnsureChild(managers.transform, "UIManager");
        GameObject audioManagerGo = EnsureChild(managers.transform, "AudioManager");

        UIManager uiManager = EnsureComponent<UIManager>(uiManagerGo);
        AudioManager audioManager = EnsureComponent<AudioManager>(audioManagerGo);

        foreach (RuntimeUiRefs.ButtonSpec buttonSpec in ui.BottomButtons)
        {
            ButtonController controller = EnsureComponent<ButtonController>(buttonSpec.GameObject);
            controller.uiManager = uiManager;
            controller.buttonType = buttonSpec.Type;
        }

        BindUiManager(uiManager, ui);

        VisualAssetBinder binder = EnsureComponent<VisualAssetBinder>(managers);
        binder.scheduleButton = ui.GetButton("schedule");
        binder.staffButton = ui.GetButton("staff");
        binder.maintenanceButton = ui.GetButton("maintenance");
        binder.stationServiceButton = ui.GetButton("stationService");
        binder.externalAffairsButton = ui.GetButton("externalAffairs");
        binder.endDayButton = ui.GetButton("endDay");
        binder.trainObject = FindFirstInScene(scene, "TrainPlaceholder_Legacy")
            ?? FindFirstInScene(scene, "TrainCandidate_01")
            ?? FindFirstInScene(scene, "Train");
        binder.tilemapObject = FindFirstInScene(scene, "Ground");

        if (audioManager.gameObject.GetComponent<AudioSource>() == null)
        {
            audioManager.gameObject.AddComponent<AudioSource>();
        }
    }

    private static Canvas EnsureCanvas(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas canvas = root.GetComponentInChildren<Canvas>(true);
            if (canvas != null)
            {
                RectTransform rectTransform = canvas.GetComponent<RectTransform>();
                if (rectTransform != null && rectTransform.localScale == Vector3.zero)
                {
                    rectTransform.localScale = Vector3.one;
                }

                EnsureComponent<CanvasScaler>(canvas.gameObject);
                EnsureComponent<GraphicRaycaster>(canvas.gameObject);
                return canvas;
            }
        }

        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasGo, scene);
        Canvas newCanvas = canvasGo.GetComponent<Canvas>();
        newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        return newCanvas;
    }

    private static void EnsureMainCamera(Scene scene)
    {
        if (FindComponentInScene<Camera>(scene) != null)
        {
            return;
        }

        GameObject cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraGo.tag = "MainCamera";
        SceneManager.MoveGameObjectToScene(cameraGo, scene);

        Camera cameraComponent = cameraGo.GetComponent<Camera>();
        cameraComponent.orthographic = true;
        cameraComponent.orthographicSize = 5f;
        cameraComponent.clearFlags = CameraClearFlags.SolidColor;
        cameraComponent.backgroundColor = new Color(0.76f, 0.76f, 0.76f, 1f);
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void EnsureEventSystem(Scene scene)
    {
        if (FindComponentInScene<EventSystem>(scene) != null)
        {
            return;
        }

        GameObject eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        SceneManager.MoveGameObjectToScene(eventSystemGo, scene);
    }

    private static GameObject EnsurePanel(Transform parent, string name)
    {
        GameObject panel = FindChild(parent, name);
        if (panel == null)
        {
            panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
        }

        Image image = EnsureComponent<Image>(panel);
        image.color = new Color(1f, 1f, 1f, 0.15f);

        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;

        return panel;
    }

    private static void LayoutRootPanels(RectTransform canvas, RectTransform topBar, RectTransform rightPanel, RectTransform bottomActions)
    {
        if (canvas != null && canvas.localScale == Vector3.zero)
        {
            canvas.localScale = Vector3.one;
        }

        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.sizeDelta = new Vector2(0f, 72f);
        topBar.anchoredPosition = Vector2.zero;

        rightPanel.anchorMin = new Vector2(1f, 0f);
        rightPanel.anchorMax = new Vector2(1f, 1f);
        rightPanel.pivot = new Vector2(1f, 0.5f);
        rightPanel.sizeDelta = new Vector2(320f, 0f);
        rightPanel.anchoredPosition = Vector2.zero;

        bottomActions.anchorMin = new Vector2(0f, 0f);
        bottomActions.anchorMax = new Vector2(1f, 0f);
        bottomActions.pivot = new Vector2(0.5f, 0f);
        bottomActions.sizeDelta = new Vector2(0f, 96f);
        bottomActions.anchoredPosition = Vector2.zero;
    }

    private static RuntimeUiRefs EnsureUi(Transform topBar, Transform rightPanel, Transform bottomActions, Transform canvas)
    {
        Font font = GetRuntimeFont();
        RuntimeUiRefs ui = new RuntimeUiRefs();

        ui.DayText = EnsureText(topBar, "DayText", font, 16);
        ui.MoneyText = EnsureText(topBar, "MoneyText", font, 16);
        ui.TrustText = EnsureText(topBar, "TrustText", font, 16);
        ui.TrainConditionText = EnsureText(topBar, "TrainConditionText", font, 16);
        ui.ExpectedPassengersText = EnsureText(topBar, "ExpectedPassengersText", font, 16);
        ui.StatusText = EnsureText(topBar, "StatusText", font, 16);

        SetRect(ui.DayText.rectTransform, 10f, -6f, 160f, 20f);
        SetRect(ui.MoneyText.rectTransform, 180f, -6f, 180f, 20f);
        SetRect(ui.TrustText.rectTransform, 370f, -6f, 140f, 20f);
        SetRect(ui.TrainConditionText.rectTransform, 10f, -32f, 160f, 20f);
        SetRect(ui.ExpectedPassengersText.rectTransform, 180f, -32f, 180f, 20f);
        SetRect(ui.StatusText.rectTransform, 370f, -32f, 420f, 20f);

        ui.DescriptionText = EnsureText(rightPanel, "DescriptionText", font, 14, true);
        ui.BriefingText = EnsureText(rightPanel, "BriefingText", font, 13, true);
        ui.NoticesText = EnsureText(rightPanel, "NoticesText", font, 13, true);
        ui.TodosText = EnsureText(rightPanel, "TodosText", font, 13, true);

        SetRect(ui.DescriptionText.rectTransform, 10f, -10f, 300f, 220f);
        SetRect(ui.BriefingText.rectTransform, 10f, -240f, 300f, 90f);
        SetRect(ui.NoticesText.rectTransform, 10f, -340f, 300f, 110f);
        SetRect(ui.TodosText.rectTransform, 10f, -460f, 300f, 130f);

        ui.BottomButtons = new[]
        {
            EnsureButton(bottomActions, "Btn_Schedule", "发车", "schedule"),
            EnsureButton(bottomActions, "Btn_Staff", "人员", "staff"),
            EnsureButton(bottomActions, "Btn_Maintenance", "检修", "maintenance"),
            EnsureButton(bottomActions, "Btn_Service", "服务", "stationService"),
            EnsureButton(bottomActions, "Btn_External", "对外", "externalAffairs"),
            EnsureButton(bottomActions, "Btn_EndDay", "End Day", "endDay")
        };

        const float startX = 10f;
        const float width = 98f;
        const float gap = 6f;
        for (int i = 0; i < ui.BottomButtons.Length; i++)
        {
            RectTransform rect = ui.BottomButtons[i].GameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(startX + i * (width + gap), 0f);
            rect.sizeDelta = new Vector2(width, 44f);
        }

        GameObject panelsRoot = EnsureChild(canvas, "MvpPanels");
        RectTransform panelsRootRect = EnsureComponent<RectTransform>(panelsRoot);
        panelsRootRect.anchorMin = Vector2.zero;
        panelsRootRect.anchorMax = Vector2.one;
        panelsRootRect.offsetMin = Vector2.zero;
        panelsRootRect.offsetMax = Vector2.zero;

        ui.SchedulePanel = EnsureOverlayPanel(panelsRoot.transform, "SchedulePanel", font, out ui.ScheduleTitle, out ui.ScheduleContent);
        ui.StaffPanel = EnsureOverlayPanel(panelsRoot.transform, "StaffPanel", font, out ui.StaffTitle, out ui.StaffContent);
        ui.MaintenancePanel = EnsureOverlayPanel(panelsRoot.transform, "MaintenancePanel", font, out ui.MaintenanceTitle, out ui.MaintenanceContent);
        ui.StationServicePanel = EnsureOverlayPanel(panelsRoot.transform, "StationServicePanel", font, out ui.StationServiceTitle, out ui.StationServiceContent);
        ui.ExternalAffairsPanel = EnsureOverlayPanel(panelsRoot.transform, "ExternalAffairsPanel", font, out ui.ExternalAffairsTitle, out ui.ExternalAffairsContent);
        ui.EndDayPanel = EnsureOverlayPanel(panelsRoot.transform, "EndDayPanel", font, out ui.EndDayTitle, out ui.EndDayContent);
        ui.SummaryPanel = EnsureOverlayPanel(panelsRoot.transform, "SummaryPanel", font, out ui.SummaryTitle, out ui.SummaryContent);

        return ui;
    }

    private static GameObject EnsureOverlayPanel(Transform parent, string name, Font font, out Text title, out Text content)
    {
        GameObject panel = FindChild(parent, name);
        if (panel == null)
        {
            panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(parent, false);
        }

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(520f, 420f);

        Image image = EnsureComponent<Image>(panel);
        image.color = new Color(0f, 0f, 0f, 0.82f);

        title = EnsureText(panel.transform, name + "_Title", font, 18, true);
        content = EnsureText(panel.transform, name + "_Content", font, 14, true);
        title.alignment = TextAnchor.UpperCenter;
        content.alignment = TextAnchor.UpperLeft;
        content.horizontalOverflow = HorizontalWrapMode.Wrap;
        content.verticalOverflow = VerticalWrapMode.Overflow;

        SetRect(title.rectTransform, 20f, -20f, 480f, 34f);
        SetRect(content.rectTransform, 20f, -64f, 480f, 330f);

        panel.SetActive(false);
        return panel;
    }

    private static RuntimeUiRefs.ButtonSpec EnsureButton(Transform parent, string name, string label, string type)
    {
        GameObject buttonGo = FindChild(parent, name);
        if (buttonGo == null)
        {
            buttonGo = DefaultControls.CreateButton(new DefaultControls.Resources());
            buttonGo.name = name;
            buttonGo.transform.SetParent(parent, false);
        }

        Text labelText = buttonGo.GetComponentInChildren<Text>(true);
        if (labelText != null)
        {
            labelText.font = GetRuntimeFont();
            labelText.text = label;
            labelText.color = Color.black;
            labelText.alignment = TextAnchor.MiddleCenter;
        }

        return new RuntimeUiRefs.ButtonSpec(buttonGo, EnsureComponent<Button>(buttonGo), type);
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

    private static Text EnsureText(Transform parent, string name, Font font, int fontSize, bool richText = false)
    {
        GameObject textGo = FindChild(parent, name);
        if (textGo == null)
        {
            textGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(parent, false);
        }

        Text text = textGo.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.supportRichText = richText;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.localScale = Vector3.one;

        return text;
    }

    private static void BindUiManager(UIManager uiManager, RuntimeUiRefs ui)
    {
        uiManager.dayText = ui.DayText;
        uiManager.moneyText = ui.MoneyText;
        uiManager.trustText = ui.TrustText;
        uiManager.trainConditionText = ui.TrainConditionText;
        uiManager.expectedPassengersText = ui.ExpectedPassengersText;
        uiManager.statusText = ui.StatusText;
        uiManager.descriptionText = ui.DescriptionText;
        uiManager.briefingText = ui.BriefingText;
        uiManager.noticesText = ui.NoticesText;
        uiManager.todosText = ui.TodosText;

        uiManager.schedulePanel = ui.SchedulePanel;
        uiManager.schedulePanelTitle = ui.ScheduleTitle;
        uiManager.schedulePanelContent = ui.ScheduleContent;
        uiManager.staffPanel = ui.StaffPanel;
        uiManager.staffPanelTitle = ui.StaffTitle;
        uiManager.staffPanelContent = ui.StaffContent;
        uiManager.maintenancePanel = ui.MaintenancePanel;
        uiManager.maintenancePanelTitle = ui.MaintenanceTitle;
        uiManager.maintenancePanelContent = ui.MaintenanceContent;
        uiManager.stationServicePanel = ui.StationServicePanel;
        uiManager.stationServicePanelTitle = ui.StationServiceTitle;
        uiManager.stationServicePanelContent = ui.StationServiceContent;
        uiManager.externalAffairsPanel = ui.ExternalAffairsPanel;
        uiManager.externalAffairsPanelTitle = ui.ExternalAffairsTitle;
        uiManager.externalAffairsPanelContent = ui.ExternalAffairsContent;
        uiManager.endDayPanel = ui.EndDayPanel;
        uiManager.endDayPanelTitle = ui.EndDayTitle;
        uiManager.endDayPanelContent = ui.EndDayContent;
        uiManager.summaryPanel = ui.SummaryPanel;
        uiManager.summaryPanelTitle = ui.SummaryTitle;
        uiManager.summaryPanelContent = ui.SummaryContent;
    }

    private static GameObject EnsureRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }

        GameObject gameObject = new GameObject(name);
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        return gameObject;
    }

    private static GameObject EnsureChild(Transform parent, string name)
    {
        GameObject child = FindChild(parent, name);
        if (child != null)
        {
            return child;
        }

        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    private static GameObject FindFirstInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindInHierarchy(root.transform, name);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindInHierarchy(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindInHierarchy(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static GameObject FindChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static void SetRect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private sealed class RuntimeUiRefs
    {
        public Text DayText;
        public Text MoneyText;
        public Text TrustText;
        public Text TrainConditionText;
        public Text ExpectedPassengersText;
        public Text StatusText;
        public Text DescriptionText;
        public Text BriefingText;
        public Text NoticesText;
        public Text TodosText;

        public GameObject SchedulePanel;
        public Text ScheduleTitle;
        public Text ScheduleContent;
        public GameObject StaffPanel;
        public Text StaffTitle;
        public Text StaffContent;
        public GameObject MaintenancePanel;
        public Text MaintenanceTitle;
        public Text MaintenanceContent;
        public GameObject StationServicePanel;
        public Text StationServiceTitle;
        public Text StationServiceContent;
        public GameObject ExternalAffairsPanel;
        public Text ExternalAffairsTitle;
        public Text ExternalAffairsContent;
        public GameObject EndDayPanel;
        public Text EndDayTitle;
        public Text EndDayContent;
        public GameObject SummaryPanel;
        public Text SummaryTitle;
        public Text SummaryContent;

        public ButtonSpec[] BottomButtons;

        public Button GetButton(string type)
        {
            foreach (ButtonSpec spec in BottomButtons)
            {
                if (spec.Type == type)
                {
                    return spec.Button;
                }
            }

            return null;
        }

        public readonly struct ButtonSpec
        {
            public readonly GameObject GameObject;
            public readonly Button Button;
            public readonly string Type;

            public ButtonSpec(GameObject gameObject, Button button, string type)
            {
                GameObject = gameObject;
                Button = button;
                Type = type;
            }
        }
    }
}
