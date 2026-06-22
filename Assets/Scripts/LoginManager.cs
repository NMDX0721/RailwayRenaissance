using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.IO;

public class LoginManager : MonoBehaviour
{
    private InputField usernameInput;
    private InputField passwordInput;
    private InputField confirmPasswordInput;
    private Button loginButton;
    private Button registerButton;
    private Button switchToRegisterButton;
    private Button switchToLoginButton;

    private Button forgotPasswordButton;
    private Button togglePasswordButton;
    private Toggle rememberPasswordToggle;
    private Text hintText;

    private Text versionText;
    private Text announcementText;
    private GameObject loginPanel;
    private GameObject registerPanel;
    private GameObject autoLoginPanel;
    private GameObject loginPanelBg;
    private GameObject registerPanelBg;
    private RawImage backgroundImage;
    private AudioSource bgmSource;

    private Sprite panelSprite;
    private Sprite inputSprite;
    private Sprite buttonSprite;
    private Sprite titleLogoSprite;
    private Sprite eyeOpenSprite;
    private Sprite eyeClosedSprite;
    private Sprite checkboxSprite;
    private Sprite checkboxCheckedSprite;
    private Sprite dialogBgSprite;
    private Sprite confirmBtnSprite;
    private Sprite cancelBtnSprite;
    public static Texture2D cursorTexture;
    private AudioSource sfxSource;
    private static AudioClip clickClip;
    public static bool sfxEnabled = true;

    private InputField regUsernameInput;
    private InputField regPasswordInput;
    private Button toggleRegPasswordButton;
    private Button toggleConfirmPasswordButton;
    private Toggle agreementToggle;
    private Text regUsernameHint;
    private Text regPasswordHint;
    private Text regConfirmHint;
    private Text titleTextObj;
    private GameObject panelTitleObj;

    private string authFilePath;
    private bool isPasswordVisible = false;
    private bool isRegPasswordVisible = false;
    private bool isConfirmPasswordVisible = false;
    private const string GAME_VERSION = "v1.0.0";
    public static bool showAutoLoginOnStart = true;
    public static bool showRegisterOnStart = false;

    void Start()
    {
        authFilePath = Path.Combine(Application.persistentDataPath, "auth.json");

        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        LoadSprites();
        GenerateCodeSprites();
        SetupCamera();
        SetupCanvas();
        LoadBackground();
        SetupAudio();
        SetupGameCursor();
        CheckAutoLogin();
    }

    private Font customFont;

    void SetupGameCursor()
    {
        if (cursorTexture == null) cursorTexture = LoadCursorTexture("Cursors/cursor_arrow", 3);
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.ForceSoftware);
    }

    public static Texture2D GetArrowTexture()
    {
        if (cursorTexture == null) cursorTexture = LoadCursorTexture("Cursors/cursor_arrow", 3);
        return cursorTexture;
    }

    public static Texture2D LoadCursorTexture(string resourcePath, int scale)
    {
        Texture2D src = Resources.Load<Texture2D>(resourcePath);
        if (src == null) return Texture2D.whiteTexture;

        if (scale <= 1) return src;

        int ow = src.width, oh = src.height;
        int nw = ow * scale, nh = oh * scale;
        Color[] srcPx = src.GetPixels();
        Color[] dstPx = new Color[nw * nh];
        for (int y = 0; y < nh; y++)
        {
            for (int x = 0; x < nw; x++)
            {
                dstPx[y * nw + x] = srcPx[(y / scale) * ow + (x / scale)];
            }
        }
        Texture2D tex = new Texture2D(nw, nh, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixels(dstPx);
        tex.Apply();
        return tex;
    }

    void LoadSprites()
    {
        panelSprite = Resources.Load<Sprite>("UI/Login/panel_bg");
        inputSprite = Resources.Load<Sprite>("UI/Login/input_field");
        buttonSprite = Resources.Load<Sprite>("UI/Login/button_primary");
        titleLogoSprite = Resources.Load<Sprite>("UI/Login/title_logo");
        dialogBgSprite = Resources.Load<Sprite>("UI/Dialog/dialog_bg");
        confirmBtnSprite = Resources.Load<Sprite>("UI/Dialog/button_confirm");
        cancelBtnSprite = Resources.Load<Sprite>("UI/Dialog/button_cancel");
        customFont = Resources.Load<Font>("Fonts/zpix");
    }

    void GenerateCodeSprites()
    {
        eyeOpenSprite = GenerateEyeSprite(true);
        eyeClosedSprite = GenerateEyeSprite(false);
        checkboxSprite = GenerateCheckboxSprite(false);
        checkboxCheckedSprite = GenerateCheckboxSprite(true);
    }

    Sprite GenerateEyeSprite(bool isOpen)
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Color clearColor = new Color(0, 0, 0, 0);
        Color eyeColor = new Color(0.4f, 0.35f, 0.25f);

        for (int i = 0; i < pixels.Length; i++) pixels[i] = clearColor;

        int cx = size / 2;
        int cy = size / 2;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dx = x - cx;
                float dy = y - cy;

                if (isOpen)
                {
                    // 睁眼：椭圆轮廓
                    float outer = (dx * dx) / (28f * 28f) + (dy * dy) / (18f * 18f);
                    float inner = (dx * dx) / (22f * 22f) + (dy * dy) / (12f * 12f);
                    bool onRing = outer <= 1f && inner >= 1f;

                    // 瞳孔
                    float pupil = (dx * dx) / (6f * 6f) + (dy * dy) / (6f * 6f);
                    bool onPupil = pupil <= 1f;

                    if (onRing || onPupil)
                    {
                        pixels[y * size + x] = eyeColor;
                    }
                }
                else
                {
                    // 闭眼：椭圆轮廓 + 斜线划过 + 瞳孔
                    float outer = (dx * dx) / (28f * 28f) + (dy * dy) / (18f * 18f);
                    float inner = (dx * dx) / (22f * 22f) + (dy * dy) / (12f * 12f);
                    bool onRing = outer <= 1f && inner >= 1f;

                    // 斜线：左上到右下
                    float lineDist = Mathf.Abs(dy - dx * 0.6f);
                    bool onLine = lineDist < 2.5f && Mathf.Abs(dx) < 30f && Mathf.Abs(dy) < 18f;

                    // 瞳孔
                    float pupil = (dx * dx) / (6f * 6f) + (dy * dy) / (6f * 6f);
                    bool onPupil = pupil <= 1f;

                    if (onRing || onLine || onPupil)
                    {
                        pixels[y * size + x] = eyeColor;
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    Sprite GenerateCheckboxSprite(bool isChecked)
    {
        int size = 64;
        int padding = 4;
        Texture2D tex = new Texture2D(size + padding * 2, size + padding * 2, TextureFormat.RGBA32, false);
        int total = size + padding * 2;
        Color[] pixels = new Color[total * total];
        Color clearColor = new Color(0, 0, 0, 0);
        Color borderColor = new Color(0.7f, 0.65f, 0.55f);
        Color fillColor = new Color(0.3f, 0.25f, 0.18f);
        Color checkColor = new Color(0.94f, 0.82f, 0.38f);

        for (int i = 0; i < pixels.Length; i++) pixels[i] = clearColor;

        for (int x = 0; x < total; x++)
        {
            for (int y = 0; y < total; y++)
            {
                bool isBorder = x <= padding + 1 || x >= total - padding - 2 || y <= padding + 1 || y >= total - padding - 2;
                bool isFill = x > padding + 2 && x < total - padding - 3 && y > padding + 2 && y < total - padding - 3;

                if (isBorder)
                {
                    pixels[y * total + x] = borderColor;
                }
                else if (isFill)
                {
                    pixels[y * total + x] = fillColor;
                }
            }
        }

        if (isChecked)
        {
            // 对勾 ✓：扁平形状，短笔约60°，长笔约20°
            for (int x = 0; x < total; x++)
            {
                for (int y = 0; y < total; y++)
                {
                    // 短笔：左上到中下（y减小=往下到交汇点）
                    float d1 = DistanceToSegment(x, y, 8, 44, 24, 14);
                    // 长笔：中下到右上（y增大=往上升，右端更高超出框框）
                    float d2 = DistanceToSegment(x, y, 24, 14, 66, 56);

                    if (d1 < 4.5f || d2 < 4.5f)
                    {
                        pixels[y * total + x] = checkColor;
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, total, total), new Vector2(0.5f, 0.5f));
    }

    float DistanceToSegment(float px, float py, float x1, float y1, float x2, float y2)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float lengthSq = dx * dx + dy * dy;

        if (lengthSq == 0) return Mathf.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));

        float t = Mathf.Clamp01(((px - x1) * dx + (py - y1) * dy) / lengthSq);
        float projX = x1 + t * dx;
        float projY = y1 + t * dy;

        return Mathf.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));
    }

    Font GetFont(int size)
    {
        if (customFont != null) return customFont;
        return Font.CreateDynamicFontFromOSFont("Microsoft YaHei", size);
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camObj = new GameObject("MainCamera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.05f, 0.15f);
        cam.orthographic = true;
        cam.orthographicSize = 5;
        cam.transform.position = new Vector3(0, 0, -10);
    }

    void SetupCanvas()
    {
        var canvasObj = new GameObject("LoginCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        backgroundImage = bgObj.AddComponent<RawImage>();

        loginPanel = CreateLoginPanel(canvasObj.transform);
        registerPanel = CreateRegisterPanel(canvasObj.transform);
        autoLoginPanel = CreateAutoLoginPanel(canvasObj.transform);
        registerPanel.SetActive(false);
        autoLoginPanel.SetActive(false);

        CreateTitleLogo(canvasObj.transform);

        // 版本号显示（左下角）
        CreateVersionText(canvasObj.transform);

        // 公告入口（右上角）
        announcementText = CreateAnnouncementEntry(canvasObj.transform);

        // 面板标题"登录"（Canvas级别，确保在最顶层）
        CreatePanelTitle(canvasObj.transform);

        // 错误提示文字（面板内，带背景）
        hintText = CreateHintText(canvasObj.transform);
        hintText.transform.parent.SetAsLastSibling();
    }

    Text CreateHintText(Transform parent)
    {
        var hintObj = new GameObject("HintPanel");
        hintObj.transform.SetParent(parent, false);
        var rect = hintObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -310);
        rect.sizeDelta = new Vector2(600, 50);

        var hintImg = hintObj.AddComponent<Image>();
        hintImg.color = new Color(0, 0, 0, 0);
        var hintBtn = hintObj.AddComponent<Button>();
        hintBtn.targetGraphic = hintImg;
        hintObj.AddComponent<ButtonHoverCursor>();
        hintBtn.onClick.AddListener(() =>
        {
            CancelInvoke("HideHint");
            HideHint();
        });

        // 背景
        var bgObj = new GameObject("Bg");
        bgObj.transform.SetParent(hintObj.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.8f, 0.2f, 0.15f, 0.85f);
        bgImg.raycastTarget = false;

        // 文字
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(hintObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        var text = textObj.AddComponent<Text>();
        text.raycastTarget = false;
        text.text = "";
        text.fontSize = 22;
        text.color = new Color(1f, 0.95f, 0.9f);
        text.alignment = TextAnchor.MiddleCenter;
        text.font = GetFont(22);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;

        var outline = textObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.1f, 0.05f);
        outline.effectDistance = new Vector2(1, -1);

        hintObj.SetActive(false);
        return text;
    }

    void CreatePanelTitle(Transform parent)
    {
        var titleObj = new GameObject("PanelTitle");
        panelTitleObj = titleObj;
        titleObj.transform.SetParent(parent, false);
        var rect = titleObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, 270);
        rect.sizeDelta = new Vector2(500, 90);

        titleTextObj = titleObj.AddComponent<Text>();
        titleTextObj.text = "登    录";
        titleTextObj.fontSize = 58;
        titleTextObj.color = new Color(0.94f, 0.82f, 0.38f);
        titleTextObj.alignment = TextAnchor.MiddleCenter;
        titleTextObj.font = GetFont(58);
        titleTextObj.horizontalOverflow = HorizontalWrapMode.Overflow;

        var outline = titleObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.15f, 0.05f);
        outline.effectDistance = new Vector2(2, -2);

        var shadow = titleObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(0, -3);
    }

    void CreateTitleLogo(Transform parent)
    {
        if (titleLogoSprite == null) return;

        var titleLogoObj = new GameObject("TitleLogo");
        titleLogoObj.transform.SetParent(parent, false);
        var titleRect = titleLogoObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0, 450);
        titleRect.sizeDelta = new Vector2(1361, 340);
        var titleImg = titleLogoObj.AddComponent<Image>();
        titleImg.sprite = titleLogoSprite;
        titleImg.type = Image.Type.Simple;
        titleImg.preserveAspect = true;
    }

    GameObject CreateLoginPanel(Transform parent)
    {
        var panel = new GameObject("LoginPanel");
        panel.transform.SetParent(parent, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0, -40);
        panelRect.sizeDelta = new Vector2(1600, 1200);

        if (panelSprite != null)
        {
            var panelImg = panel.AddComponent<Image>();
            panelImg.sprite = panelSprite;
            panelImg.type = Image.Type.Simple;
            panelImg.preserveAspect = false;
        }
        else
        {
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.7f);
        }

        loginPanelBg = panel;

        // 用户名标签（输入框左侧外面）
        CreateLabel(panel.transform, "UsernameLabel", "用户名", -530, 80);

        // 用户名输入框（与木框图片位置对齐）
        usernameInput = CreateInputField(panel.transform, "UsernameInput", "", 80, 80f);
        usernameInput.gameObject.AddComponent<InputFieldCursor>();

        // 密码标签（输入框左侧外面）
        CreateLabel(panel.transform, "PasswordLabel", "密码", -530, -120);

        // 密码输入框（与木框图片位置对齐）
        passwordInput = CreateInputField(panel.transform, "PasswordInput", "", -120, 80f);
        passwordInput.contentType = InputField.ContentType.Password;
        passwordInput.gameObject.AddComponent<InputFieldCursor>();

        // 密码可见切换按钮
        togglePasswordButton = CreateToggleButton(panel.transform, "TogglePassword", 440, -120);
        togglePasswordButton.onClick.AddListener(OnTogglePassword);

        // 自动登录复选框

        rememberPasswordToggle = CreateCheckbox(panel.transform, "AutoLogin", "自动登录", -380, -230);

        // 重置账号链接（与自动登录同行右侧）
        forgotPasswordButton = CreateTextButton(panel.transform, "ResetAccount", "重置账号？", 350, -230);
        forgotPasswordButton.onClick.AddListener(OnForgotPassword);

        // 登录按钮（加宽）
        loginButton = CreateButton(panel.transform, "LoginButton", "登录", -350, -260f, 550f, 100f);
        loginButton.onClick.AddListener(OnLogin);

        // 没有账号？注册按钮
        switchToRegisterButton = CreateButton(panel.transform, "SwitchToRegister", "没有账号？注册", -350, 300f, 450f, 100f);
        switchToRegisterButton.onClick.AddListener(ShowRegister);

        return panel;
    }

    GameObject CreateRegisterPanel(Transform parent)
    {
        var panel = new GameObject("RegisterPanel");
        panel.transform.SetParent(parent, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0, -40);
        panelRect.sizeDelta = new Vector2(1600, 1200);

        if (panelSprite != null)
        {
            var panelImg = panel.AddComponent<Image>();
            panelImg.sprite = panelSprite;
            panelImg.type = Image.Type.Simple;
            panelImg.preserveAspect = false;
        }
        else
        {
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.7f);
        }

        registerPanelBg = panel;

        CreateLabel(panel.transform, "RegUsernameLabel", "用户名", -530, 120);
        regUsernameInput = CreateInputField(panel.transform, "RegUsernameInput", "3-12位字符，支持中英文", 120, 80f);
        regUsernameInput.gameObject.AddComponent<InputFieldCursor>();

        CreateLabel(panel.transform, "RegPasswordLabel", "密码", -530, -10);
        regPasswordInput = CreateInputField(panel.transform, "RegPasswordInput", "6-20位，建议包含大小写字母和数字", -10, 80f);
        regPasswordInput.contentType = InputField.ContentType.Password;
        regPasswordInput.gameObject.AddComponent<InputFieldCursor>();
        toggleRegPasswordButton = CreateToggleButton(panel.transform, "ToggleRegPassword", 440, -10);
        toggleRegPasswordButton.onClick.AddListener(OnToggleRegPassword);
        CreatePasswordStrengthIndicator(panel.transform, regPasswordInput, -10);

        CreateLabel(panel.transform, "RegConfirmLabel", "确认密码", -530, -180);
        confirmPasswordInput = CreateInputField(panel.transform, "ConfirmPasswordInput", "请再次输入密码", -180, 80f);
        confirmPasswordInput.contentType = InputField.ContentType.Password;
        confirmPasswordInput.gameObject.AddComponent<InputFieldCursor>();
        toggleConfirmPasswordButton = CreateToggleButton(panel.transform, "ToggleConfirmPassword", 440, -180);
        toggleConfirmPasswordButton.onClick.AddListener(OnToggleConfirmPassword);

        agreementToggle = CreateAgreementCheckboxWithLinks(panel.transform, "AgreementToggle", 30, -260);

        registerButton = CreateButton(panel.transform, "RegisterButton", "注册", -350, -260f, 550f, 100f);
        registerButton.onClick.AddListener(OnRegister);

        switchToLoginButton = CreateButton(panel.transform, "SwitchToLogin", "已有账号？登录", -350, 300f, 450f, 100f);
        switchToLoginButton.onClick.AddListener(ShowLogin);

        return panel;
    }

    GameObject CreateAutoLoginPanel(Transform parent)
    {
        var panel = new GameObject("AutoLoginPanel");
        panel.transform.SetParent(parent, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0, -40);
        panelRect.sizeDelta = new Vector2(960, 720);

        if (panelSprite != null)
        {
            var panelImg = panel.AddComponent<Image>();
            panelImg.sprite = panelSprite;
            panelImg.type = Image.Type.Simple;
            panelImg.preserveAspect = false;
        }
        else
        {
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.7f);
        }

        var titleObj = CreateText(panel.transform, "Title", "自动登录", 34, new Color(0.94f, 0.82f, 0.38f), 190);
        titleObj.rectTransform.sizeDelta = new Vector2(400, 60);
        titleObj.fontSize = 36;

        var mainTextObj = new GameObject("MainText");
        mainTextObj.transform.SetParent(panel.transform, false);
        var mainTextRect = mainTextObj.AddComponent<RectTransform>();
        mainTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        mainTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        mainTextRect.anchoredPosition = new Vector2(0, 80);
        mainTextRect.sizeDelta = new Vector2(500, 60);
        var mainTextComp = mainTextObj.AddComponent<Text>();
        mainTextComp.text = "自动登录中...";
        mainTextComp.fontSize = 40;
        mainTextComp.color = new Color(1f, 0.84f, 0f);
        mainTextComp.alignment = TextAnchor.MiddleCenter;
        mainTextComp.font = GetFont(40);

        var subTextObj = new GameObject("SubText");
        subTextObj.transform.SetParent(panel.transform, false);
        var subTextRect = subTextObj.AddComponent<RectTransform>();
        subTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        subTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        subTextRect.anchoredPosition = new Vector2(0, 30);
        subTextRect.sizeDelta = new Vector2(500, 40);
        var subTextComp = subTextObj.AddComponent<Text>();
        subTextComp.text = "正在检测本地凭证...";
        subTextComp.fontSize = 28;
        subTextComp.color = new Color(0.6f, 0.6f, 0.6f);
        subTextComp.alignment = TextAnchor.MiddleCenter;
        subTextComp.font = GetFont(28);

        var progressBgObj = new GameObject("ProgressBg");
        progressBgObj.transform.SetParent(panel.transform, false);
        var progressBgRect = progressBgObj.AddComponent<RectTransform>();
        progressBgRect.anchorMin = new Vector2(0.5f, 0.5f);
        progressBgRect.anchorMax = new Vector2(0.5f, 0.5f);
        progressBgRect.anchoredPosition = new Vector2(0, -30);
        progressBgRect.sizeDelta = new Vector2(500, 30);
        var progressBgImg = progressBgObj.AddComponent<Image>();
        progressBgImg.color = new Color(0.24f, 0.17f, 0.12f);

        var progressFillObj = new GameObject("ProgressFill");
        progressFillObj.transform.SetParent(progressBgObj.transform, false);
        var progressFillRect = progressFillObj.AddComponent<RectTransform>();
        progressFillRect.anchorMin = new Vector2(0f, 0f);
        progressFillRect.anchorMax = new Vector2(0f, 1f);
        progressFillRect.sizeDelta = new Vector2(0, 0);
        var progressFillImg = progressFillObj.AddComponent<Image>();
        progressFillImg.color = new Color(1f, 0.42f, 0.21f);

        var progressTextObj = new GameObject("ProgressText");
        progressTextObj.transform.SetParent(progressBgObj.transform, false);
        var progressTextRect = progressTextObj.AddComponent<RectTransform>();
        progressTextRect.anchorMin = Vector2.zero;
        progressTextRect.anchorMax = Vector2.one;
        progressTextRect.sizeDelta = Vector2.zero;
        var progressTextComp = progressTextObj.AddComponent<Text>();
        progressTextComp.text = "0%";
        progressTextComp.fontSize = 22;
        progressTextComp.color = new Color(1f, 0.84f, 0f);
        progressTextComp.alignment = TextAnchor.MiddleCenter;
        progressTextComp.font = GetFont(22);

        var btnReturnObj = new GameObject("BtnReturnLogin");
        btnReturnObj.transform.SetParent(panel.transform, false);
        var btnReturnRect = btnReturnObj.AddComponent<RectTransform>();
        btnReturnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnReturnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnReturnRect.anchoredPosition = new Vector2(0, -120);
        btnReturnRect.sizeDelta = new Vector2(280, 80);
        var btnReturnImg = btnReturnObj.AddComponent<Image>();
        btnReturnImg.color = new Color(0.4f, 0.4f, 0.4f);
        var btnReturnComp = btnReturnObj.AddComponent<Button>();
        btnReturnObj.AddComponent<ButtonHoverCursor>();

        var btnReturnTextObj = new GameObject("Text");
        btnReturnTextObj.transform.SetParent(btnReturnObj.transform, false);
        var btnReturnTextRect = btnReturnTextObj.AddComponent<RectTransform>();
        btnReturnTextRect.anchorMin = Vector2.zero;
        btnReturnTextRect.anchorMax = Vector2.one;
        btnReturnTextRect.sizeDelta = Vector2.zero;
        var btnReturnTextComp = btnReturnTextObj.AddComponent<Text>();
        btnReturnTextComp.text = "手动登录";
        btnReturnTextComp.fontSize = 28;
        btnReturnTextComp.color = new Color(1f, 0.95f, 0.9f);
        btnReturnTextComp.alignment = TextAnchor.MiddleCenter;
        btnReturnTextComp.font = GetFont(28);

        var btnResetObj = new GameObject("BtnResetAccount");
        btnResetObj.transform.SetParent(panel.transform, false);
        var btnResetRect = btnResetObj.AddComponent<RectTransform>();
        btnResetRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnResetRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnResetRect.anchoredPosition = new Vector2(0, -120);
        btnResetRect.sizeDelta = new Vector2(280, 80);
        var btnResetImg = btnResetObj.AddComponent<Image>();
        if (buttonSprite != null)
        {
            btnResetImg.sprite = buttonSprite;
            btnResetImg.type = Image.Type.Simple;
            btnResetImg.preserveAspect = false;
        }
        else
        {
            btnResetImg.color = new Color(0.8f, 0.5f, 0.2f);
        }
        var btnResetComp = btnResetObj.AddComponent<Button>();
        btnResetObj.AddComponent<ButtonHoverCursor>();

        var btnResetTextObj = new GameObject("Text");
        btnResetTextObj.transform.SetParent(btnResetObj.transform, false);
        var btnResetTextRect = btnResetTextObj.AddComponent<RectTransform>();
        btnResetTextRect.anchorMin = Vector2.zero;
        btnResetTextRect.anchorMax = Vector2.one;
        btnResetTextRect.sizeDelta = Vector2.zero;
        var btnResetTextComp = btnResetTextObj.AddComponent<Text>();
        btnResetTextComp.text = "重置账号";
        btnResetTextComp.fontSize = 28;
        btnResetTextComp.color = new Color(1f, 0.95f, 0.9f);
        btnResetTextComp.alignment = TextAnchor.MiddleCenter;
        btnResetTextComp.font = GetFont(28);

        btnResetObj.SetActive(false);

        var autoLoginUI = panel.AddComponent<AutoLoginUI>();
        autoLoginUI.autoLoginPanel = panel;
        autoLoginUI.mainText = mainTextComp;
        autoLoginUI.subText = subTextComp;
        autoLoginUI.progressFill = progressFillImg;
        autoLoginUI.progressText = progressTextComp;
        autoLoginUI.progressBarGroup = progressBgObj;
        autoLoginUI.btnReturnLogin = btnReturnComp;
        autoLoginUI.btnResetAccount = btnResetComp;

        return panel;
    }

    void SetupAudio()
    {
        var audioObj = new GameObject("BGM");
        audioObj.transform.SetParent(transform, false);
        bgmSource = audioObj.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = 0.3f;

        AudioClip clip = Resources.Load<AudioClip>("Audio/Train Through Keys");
        if (clip != null)
        {
            bgmSource.clip = clip;
            bgmSource.Play();
        }

        var sfxObj = new GameObject("SFX");
        sfxObj.transform.SetParent(transform, false);
        sfxSource = sfxObj.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        clickClip = Resources.Load<AudioClip>("Audio/button_click");
    }

    public static void PlayClickSound()
    {
        if (!sfxEnabled || clickClip == null) return;
        var sfx = FindAnyObjectByType<LoginManager>();
        if (sfx != null && sfx.sfxSource != null)
            sfx.sfxSource.PlayOneShot(clickClip, 0.8f);
    }

    void LoadBackground()
    {
        if (backgroundImage == null) return;
        Texture2D tex = Resources.Load<Texture2D>("Textures/sunset_railway");
        if (tex == null) tex = Resources.Load<Texture2D>("Textures/station_bg");
        if (tex != null) backgroundImage.texture = tex;
    }

    void CheckAutoLogin()
    {
        if (showRegisterOnStart)
        {
            showRegisterOnStart = false;
            ShowRegister();
            return;
        }
        
        if (showAutoLoginOnStart)
        {
            showAutoLoginOnStart = false;
            if (File.Exists(authFilePath))
            {
                string json = File.ReadAllText(authFilePath);
                var auth = JsonUtility.FromJson<AuthData>(json);
                if (auth != null && !string.IsNullOrEmpty(auth.username))
                {
                    loginPanel.SetActive(false);
                    registerPanel.SetActive(false);
                    autoLoginPanel.SetActive(true);
                    if (panelTitleObj != null) panelTitleObj.SetActive(false);
                    return;
                }
            }
            ShowLogin();
        }
        else
        {
            ShowLogin();
        }
    }

    void ShowLogin()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        autoLoginPanel.SetActive(false);
        if (hintText != null) hintText.transform.parent.gameObject.SetActive(false);
        if (titleTextObj != null) titleTextObj.text = "登    录";
        if (panelTitleObj != null) panelTitleObj.SetActive(true);
    }

    void ShowRegister()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        autoLoginPanel.SetActive(false);
        if (hintText != null) hintText.transform.parent.gameObject.SetActive(false);
        if (titleTextObj != null) titleTextObj.text = "注    册";
        if (panelTitleObj != null) panelTitleObj.SetActive(true);
    }

    void ShowAutoLogin(string username)
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        autoLoginPanel.SetActive(true);
    }

    public void OnLogin()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username))
        {
            ShowHint("请输入用户名", false);
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            ShowHint("请输入密码", false);
            return;
        }
        if (!File.Exists(authFilePath))
        {
            ShowHint("用户名或密码错误", false);
            return;
        }

        string json = File.ReadAllText(authFilePath);
        var auth = JsonUtility.FromJson<AuthData>(json);
        if (auth == null || auth.username != username || auth.password != password)
        {
            ShowHint("用户名或密码错误", false);
            return;
        }

        auth.lastLogin = System.DateTime.Now.ToString("o");
        File.WriteAllText(authFilePath, JsonUtility.ToJson(auth, true));
        ShowHint("登录成功！", true);
        Invoke("EnterGame", 1f);
    }

    void OnRegister()
    {
        string username = regUsernameInput.text.Trim();
        string password = regPasswordInput.text;
        string confirm = confirmPasswordInput.text;

        if (string.IsNullOrEmpty(username) || username.Length < 3 || username.Length > 12)
        {
            ShowHint("用户名需3-12位字符", false);
            return;
        }
        if (string.IsNullOrEmpty(password) || password.Length < 6 || password.Length > 20)
        {
            ShowHint("密码需6-20位", false);
            return;
        }
        if (password != confirm)
        {
            ShowHint("两次输入的密码不一致", false);
            return;
        }
        if (agreementToggle == null || !agreementToggle.isOn)
        {
            ShowHint("请先同意用户协议和隐私政策", false);
            return;
        }

        var auth = new AuthData
        {
            username = username,
            password = password,
            lastLogin = System.DateTime.Now.ToString("o")
        };
        File.WriteAllText(authFilePath, JsonUtility.ToJson(auth, true));
        ShowHint("注册成功！正在跳转...", true);
        
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        autoLoginPanel.SetActive(true);
        if (panelTitleObj != null) panelTitleObj.SetActive(false);
        
        var autoLoginUI = autoLoginPanel.GetComponent<AutoLoginUI>();
        if (autoLoginUI != null)
        {
            autoLoginUI.StartAfterRegister();
        }
        else
        {
            Invoke("ShowLogin", 1.5f);
        }
    }

    public void OnAutoLogin()
    {
        EnterGame();
    }

    void EnterGame()
    {
        SceneManager.LoadScene("TitleScreen");
    }

    void ShowHint(string message, bool success)
    {
        if (hintText != null)
        {
            CancelInvoke("HideHint");
            hintText.text = message;
            hintText.color = success ? new Color(0.7f, 1f, 0.6f) : new Color(1f, 0.95f, 0.9f);
            hintText.transform.parent.gameObject.SetActive(true);
            hintText.transform.parent.SetAsLastSibling();

            var bg = hintText.transform.parent.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = success
                    ? new Color(0.2f, 0.6f, 0.15f, 0.85f)
                    : new Color(0.8f, 0.2f, 0.15f, 0.85f);
            }

            Invoke("HideHint", 3f);
        }
    }

    void HideHint()
    {
        if (hintText != null && hintText.transform.parent != null)
            hintText.transform.parent.gameObject.SetActive(false);
    }

    Text CreateText(Transform parent, string name, string content, int fontSize, Color color, float yOffset)
    {
        var textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        var rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, yOffset);
        rect.sizeDelta = new Vector2(540, 48);
        var text = textObj.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = GetFont(fontSize);
        return text;
    }

    InputField CreateInputField(Transform parent, string name, string placeholder, float yOffset, float xOffset = 0f)
    {
        // 外层容器（定位用）
        var containerObj = new GameObject(name);
        containerObj.transform.SetParent(parent, false);
        var containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(xOffset, yOffset);
        containerRect.sizeDelta = new Vector2(1000, 100);

        // 对象1：视觉层（米白色 + 边框）
        var visualObj = new GameObject("Visual");
        visualObj.transform.SetParent(containerObj.transform, false);
        var visualRect = visualObj.AddComponent<RectTransform>();
        visualRect.anchorMin = Vector2.zero;
        visualRect.anchorMax = Vector2.one;
        visualRect.sizeDelta = Vector2.zero;

        // 米白色背景
        var bgObj = new GameObject("BG");
        bgObj.transform.SetParent(visualObj.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = new Vector2(-120, -40);
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.91f, 0.86f, 0.78f);
        bgImg.raycastTarget = false;

        // 边框图片
        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(visualObj.transform, false);
        var borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        var borderImg = borderObj.AddComponent<Image>();
        if (inputSprite != null)
        {
            borderImg.sprite = inputSprite;
            borderImg.type = Image.Type.Simple;
            borderImg.preserveAspect = false;
        }
        borderImg.raycastTarget = false;

        // 对象2：InputField层（透明背景 + 文字 + InputField组件）
        var inputObj = new GameObject("Input");
        inputObj.transform.SetParent(containerObj.transform, false);
        var inputRect = inputObj.AddComponent<RectTransform>();
        inputRect.anchorMin = Vector2.zero;
        inputRect.anchorMax = Vector2.one;
        inputRect.sizeDelta = Vector2.zero;
        inputRect.offsetMax = new Vector2(-100, 0);

        var inputImg = inputObj.AddComponent<Image>();
        inputImg.color = new Color(0, 0, 0, 0);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(inputObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(75, 10);
        textRect.offsetMax = new Vector2(-15, -10);
        var text = textObj.AddComponent<Text>();
        text.fontSize = 36;
        text.color = new Color(0.15f, 0.1f, 0.05f);
        text.alignment = TextAnchor.MiddleLeft;
        text.font = GetFont(36);

        // Placeholder 提示文字
        if (!string.IsNullOrEmpty(placeholder))
        {
            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputObj.transform, false);
            var placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            placeholderRect.offsetMin = new Vector2(75, 10);
            placeholderRect.offsetMax = new Vector2(-15, -10);
            var placeholderText = placeholderObj.AddComponent<Text>();
            placeholderText.text = placeholder;
            placeholderText.fontSize = 28;
            placeholderText.color = new Color(0.55f, 0.5f, 0.42f, 0.6f);
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.font = GetFont(36);
            placeholderText.fontStyle = FontStyle.Italic;

            var input = inputObj.AddComponent<InputField>();
            input.caretColor = Color.black;
            input.caretWidth = 2;
            input.selectionColor = new Color(0.85f, 0.65f, 0.15f, 0.8f);
            input.textComponent = text;
            input.placeholder = placeholderText;
            input.targetGraphic = inputImg;
            input.interactable = true;
            input.transition = Selectable.Transition.None;
            return input;
        }

        var inputDefault = inputObj.AddComponent<InputField>();
        inputDefault.caretColor = Color.black;
        inputDefault.caretWidth = 2;
        inputDefault.selectionColor = new Color(0.85f, 0.65f, 0.15f, 0.8f);
        inputDefault.textComponent = text;
        inputDefault.targetGraphic = inputImg;
        inputDefault.interactable = true;
        inputDefault.transition = Selectable.Transition.None;

        return inputDefault;
    }

    Button CreateButton(Transform parent, string name, string label, float yOffset, float xOffset = 0f, float width = 800f, float height = 225f)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(xOffset, yOffset);
        rect.sizeDelta = new Vector2(width, height);

        var img = btnObj.AddComponent<Image>();
        if (buttonSprite != null)
        {
            img.sprite = buttonSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.fillAmount = 1;
        }
        else
        {
            img.color = new Color(0.8f, 0.5f, 0.2f);
        }

        var button = btnObj.AddComponent<Button>();
        btnObj.AddComponent<ButtonHoverCursor>();

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        var text = textObj.AddComponent<Text>();
        text.text = label;
        text.fontSize = 38;
        text.color = new Color(1f, 0.92f, 0.75f);
        text.alignment = TextAnchor.MiddleCenter;
        text.font = GetFont(38);

        return button;
    }

    void CreateLabel(Transform parent, string name, string content, float xOffset, float yOffset)
    {
        var labelObj = new GameObject(name);
        labelObj.transform.SetParent(parent, false);
        var rect = labelObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(xOffset, yOffset);
        rect.sizeDelta = new Vector2(200, 50);
        var text = labelObj.AddComponent<Text>();
        text.text = content;
        text.fontSize = 36;
        text.color = new Color(0.94f, 0.82f, 0.38f);
        text.alignment = TextAnchor.MiddleRight;
        text.font = GetFont(36);
    }

    Button CreateToggleButton(Transform parent, string name, float xOffset, float yOffset)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(xOffset, yOffset);
        rect.sizeDelta = new Vector2(70, 70);

        var img = btnObj.AddComponent<Image>();
        if (eyeClosedSprite != null)
        {
            img.sprite = eyeClosedSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
        }
        else
        {
            img.color = new Color(0.7f, 0.65f, 0.55f);
        }

        var button = btnObj.AddComponent<Button>();
        btnObj.AddComponent<ButtonHoverCursor>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        button.colors = colors;

        return button;
    }

    Toggle CreateCheckbox(Transform parent, string name, string label, float xOffset, float yOffset)
    {
        var checkObj = new GameObject(name);
        checkObj.transform.SetParent(parent, false);
        var rect = checkObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(xOffset, yOffset);
        rect.sizeDelta = new Vector2(280, 50);

        var checkImg = checkObj.AddComponent<Image>();
        checkImg.color = new Color(0, 0, 0, 0);

        // 复选框背景
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(checkObj.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.5f);
        bgRect.anchorMax = new Vector2(0f, 0.5f);
        bgRect.anchoredPosition = new Vector2(25, 0);
        bgRect.sizeDelta = new Vector2(40, 40);
        var bgImg = bgObj.AddComponent<Image>();
        if (checkboxSprite != null)
        {
            bgImg.sprite = checkboxSprite;
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;
        }
        else
        {
            bgImg.color = new Color(0.3f, 0.25f, 0.2f);
        }

        // 勾选标记
        var checkmarkObj = new GameObject("Checkmark");
        checkmarkObj.transform.SetParent(bgObj.transform, false);
        var checkmarkRect = checkmarkObj.AddComponent<RectTransform>();
        checkmarkRect.anchorMin = Vector2.zero;
        checkmarkRect.anchorMax = Vector2.one;
        checkmarkRect.sizeDelta = Vector2.zero;
        var checkmarkImg = checkmarkObj.AddComponent<Image>();
        if (checkboxCheckedSprite != null)
        {
            checkmarkImg.sprite = checkboxCheckedSprite;
            checkmarkImg.type = Image.Type.Simple;
            checkmarkImg.preserveAspect = false;
        }
        else
        {
            checkmarkImg.color = new Color(0.94f, 0.82f, 0.38f);
        }

        // 标签文字
        var textObj = new GameObject("Label");
        textObj.transform.SetParent(checkObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0.5f);
        textRect.anchorMax = new Vector2(1f, 0.5f);
        textRect.anchoredPosition = new Vector2(35, 0);
        textRect.sizeDelta = new Vector2(-50, 35);
        var text = textObj.AddComponent<Text>();
        text.text = label;
        text.fontSize = 28;
        text.color = new Color(0.85f, 0.8f, 0.7f);
        text.alignment = TextAnchor.MiddleLeft;
        text.font = GetFont(24);

        var toggle = checkObj.AddComponent<Toggle>();
        checkObj.AddComponent<ButtonHoverCursor>();
        toggle.targetGraphic = checkImg;
        toggle.graphic = checkmarkImg;
        toggle.isOn = false;

        return toggle;
    }

    Button CreateTextButton(Transform parent, string name, string label, float xOffset, float yOffset)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(xOffset, yOffset);
        rect.sizeDelta = new Vector2(200, 40);

        var text = btnObj.AddComponent<Text>();
        text.text = label;
        text.fontSize = 24;
        text.color = new Color(0.8f, 0.75f, 0.65f);
        text.alignment = TextAnchor.MiddleCenter;
        text.font = GetFont(24);

        var button = btnObj.AddComponent<Button>();
        btnObj.AddComponent<ButtonHoverCursor>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.9f, 0.7f);
        colors.pressedColor = new Color(0.8f, 0.7f, 0.5f);
        button.colors = colors;

        return button;
    }

    Text CreateHintBelowInput(Transform parent, string name, string content, float inputYOffset, float inputHeight)
    {
        var hintObj = new GameObject(name);
        hintObj.transform.SetParent(parent, false);
        var rect = hintObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, inputYOffset - inputHeight / 2 - 18);
        rect.sizeDelta = new Vector2(800, 30);
        var text = hintObj.AddComponent<Text>();
        text.text = content;
        text.fontSize = 20;
        text.color = new Color(0.6f, 0.55f, 0.45f, 0.7f);
        text.alignment = TextAnchor.MiddleLeft;
        text.font = GetFont(20);
        return text;
    }

    void SetupPlaceholder(InputField input, Text placeholderText)
    {
        input.onValueChanged.AddListener(val =>
        {
            if (placeholderText != null)
                placeholderText.gameObject.SetActive(string.IsNullOrEmpty(val));
        });

        var trigger = input.gameObject.AddComponent<EventTrigger>();
        var selectEntry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
        selectEntry.callback.AddListener(_ =>
        {
            if (placeholderText != null)
                placeholderText.gameObject.SetActive(false);
        });
        trigger.triggers.Add(selectEntry);

        var deselectEntry = new EventTrigger.Entry { eventID = EventTriggerType.Deselect };
        deselectEntry.callback.AddListener(_ =>
        {
            if (placeholderText != null)
                placeholderText.gameObject.SetActive(string.IsNullOrEmpty(input.text));
        });
        trigger.triggers.Add(deselectEntry);
    }

    Toggle CreateAgreementCheckbox(Transform parent, string name, string label, float xOffset, float yOffset)
    {
        var containerObj = new GameObject(name);
        containerObj.transform.SetParent(parent, false);
        var rect = containerObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(xOffset, yOffset);
        rect.sizeDelta = new Vector2(500, 40);

        var containerImg = containerObj.AddComponent<Image>();
        containerImg.color = new Color(0, 0, 0, 0);

        var checkObj = new GameObject("Checkmark");
        checkObj.transform.SetParent(containerObj.transform, false);
        var checkRect = checkObj.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0f, 0.5f);
        checkRect.anchorMax = new Vector2(0f, 0.5f);
        checkRect.anchoredPosition = new Vector2(15, 0);
        checkRect.sizeDelta = new Vector2(28, 28);
        var checkImg = checkObj.AddComponent<Image>();
        if (checkboxSprite != null)
        {
            checkImg.sprite = checkboxSprite;
            checkImg.type = Image.Type.Simple;
            checkImg.preserveAspect = false;
        }
        else
        {
            checkImg.color = new Color(0.7f, 0.65f, 0.55f);
        }

        var checkmarkObj = new GameObject("Check");
        checkmarkObj.transform.SetParent(checkObj.transform, false);
        var checkmarkRect = checkmarkObj.AddComponent<RectTransform>();
        checkmarkRect.anchorMin = Vector2.zero;
        checkmarkRect.anchorMax = Vector2.one;
        checkmarkRect.sizeDelta = Vector2.zero;
        var checkmarkImg = checkmarkObj.AddComponent<Image>();
        if (checkboxCheckedSprite != null)
        {
            checkmarkImg.sprite = checkboxCheckedSprite;
            checkmarkImg.type = Image.Type.Simple;
            checkmarkImg.preserveAspect = false;
        }
        else
        {
            checkmarkImg.color = new Color(0.94f, 0.82f, 0.38f);
        }

        var textObj = new GameObject("Label");
        textObj.transform.SetParent(containerObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0.5f);
        textRect.anchorMax = new Vector2(1f, 0.5f);
        textRect.anchoredPosition = new Vector2(32, 0);
        textRect.sizeDelta = new Vector2(-44, 32);
        var text = textObj.AddComponent<Text>();
        text.supportRichText = true;
        text.text = label;
        text.fontSize = 22;
        text.color = new Color(0.85f, 0.8f, 0.7f);
        text.alignment = TextAnchor.MiddleLeft;
        text.font = GetFont(22);

        var toggle = containerObj.AddComponent<Toggle>();
        containerObj.AddComponent<ButtonHoverCursor>();
        toggle.targetGraphic = containerImg;
        toggle.graphic = checkmarkImg;
        toggle.isOn = false;

        return toggle;
    }

    Toggle CreateAgreementCheckboxWithLinks(Transform parent, string name, float xOffset, float yOffset)
    {
        var containerObj = new GameObject(name);
        containerObj.transform.SetParent(parent, false);
        var rect = containerObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(xOffset, yOffset);
        rect.sizeDelta = new Vector2(560, 50);

        var containerImg = containerObj.AddComponent<Image>();
        containerImg.color = new Color(0, 0, 0, 0);

        // 勾选框背景
        var checkObj = new GameObject("Checkmark");
        checkObj.transform.SetParent(containerObj.transform, false);
        var checkRect = checkObj.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.anchoredPosition = new Vector2(-240, 0);
        checkRect.sizeDelta = new Vector2(32, 32);
        var checkImg = checkObj.AddComponent<Image>();
        if (checkboxSprite != null)
        {
            checkImg.sprite = checkboxSprite;
            checkImg.type = Image.Type.Simple;
            checkImg.preserveAspect = false;
        }
        else
        {
            checkImg.color = new Color(0.7f, 0.65f, 0.55f);
        }

        // 勾选标记
        var checkmarkObj = new GameObject("Check");
        checkmarkObj.transform.SetParent(checkObj.transform, false);
        var checkmarkRect = checkmarkObj.AddComponent<RectTransform>();
        checkmarkRect.anchorMin = Vector2.zero;
        checkmarkRect.anchorMax = Vector2.one;
        checkmarkRect.sizeDelta = Vector2.zero;
        var checkmarkImg = checkmarkObj.AddComponent<Image>();
        if (checkboxCheckedSprite != null)
        {
            checkmarkImg.sprite = checkboxCheckedSprite;
            checkmarkImg.type = Image.Type.Simple;
            checkmarkImg.preserveAspect = false;
        }
        else
        {
            checkmarkImg.color = new Color(0.94f, 0.82f, 0.38f);
        }

        // "同意"
        CreateSmallLabel(containerObj.transform, "AgreeLabel", "同意", -185, 26, new Color(0.85f, 0.8f, 0.7f));

        // "[用户协议]" 链接文字
        CreateSmallLabel(containerObj.transform, "Link1Text", "[用户协议]", -88, 26, new Color(0.4f, 0.6f, 1f));
        // 下划线
        CreateUnderline(containerObj.transform, -88, 140);
        // 点击区域
        var link1Obj = new GameObject("UserAgreementLink");
        link1Obj.transform.SetParent(containerObj.transform, false);
        var link1Rect = link1Obj.AddComponent<RectTransform>();
        link1Rect.anchorMin = new Vector2(0.5f, 0.5f);
        link1Rect.anchorMax = new Vector2(0.5f, 0.5f);
        link1Rect.anchoredPosition = new Vector2(-88, 0);
        link1Rect.sizeDelta = new Vector2(150, 40);
        var link1Btn = link1Obj.AddComponent<Button>();
        var link1Img = link1Obj.AddComponent<Image>();
        link1Img.color = new Color(0, 0, 0, 0);
        link1Btn.targetGraphic = link1Img;
        link1Btn.onClick.AddListener(ShowUserAgreementDialog);

        // "和"
        CreateSmallLabel(containerObj.transform, "AndLabel", "和", 18, 26, new Color(0.85f, 0.8f, 0.7f));

        // "[隐私政策]" 链接文字
        CreateSmallLabel(containerObj.transform, "Link2Text", "[隐私政策]", 110, 26, new Color(0.4f, 0.6f, 1f));
        // 下划线
        CreateUnderline(containerObj.transform, 110, 140);
        // 点击区域
        var link2Obj = new GameObject("PrivacyLink");
        link2Obj.transform.SetParent(containerObj.transform, false);
        var link2Rect = link2Obj.AddComponent<RectTransform>();
        link2Rect.anchorMin = new Vector2(0.5f, 0.5f);
        link2Rect.anchorMax = new Vector2(0.5f, 0.5f);
        link2Rect.anchoredPosition = new Vector2(110, 0);
        link2Rect.sizeDelta = new Vector2(150, 40);
        var link2Btn = link2Obj.AddComponent<Button>();
        var link2Img = link2Obj.AddComponent<Image>();
        link2Img.color = new Color(0, 0, 0, 0);
        link2Btn.targetGraphic = link2Img;
        link2Btn.onClick.AddListener(ShowPrivacyPolicyDialog);

        var toggle = containerObj.AddComponent<Toggle>();
        containerObj.AddComponent<ButtonHoverCursor>();
        toggle.targetGraphic = containerImg;
        toggle.graphic = checkmarkImg;
        toggle.isOn = false;

        return toggle;
    }

    void CreatePasswordStrengthIndicator(Transform parent, InputField passwordField, float inputYOffset)
    {
        float barY = inputYOffset - 80;
        float barWidth = 440;
        float barHeight = 16;

        var strengthObj = new GameObject("PasswordStrength");
        strengthObj.transform.SetParent(parent, false);
        var strengthRect = strengthObj.AddComponent<RectTransform>();
        strengthRect.anchorMin = new Vector2(0.5f, 0.5f);
        strengthRect.anchorMax = new Vector2(0.5f, 0.5f);
        strengthRect.anchoredPosition = new Vector2(0, barY);
        strengthRect.sizeDelta = new Vector2(barWidth + 200, 50);
        strengthObj.SetActive(false);

        // 淡色背景
        var bgObj = new GameObject("Bg");
        bgObj.transform.SetParent(strengthObj.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.12f, 0.08f, 0.4f);

        // 条形背景
        var barBgObj = new GameObject("BarBg");
        barBgObj.transform.SetParent(strengthObj.transform, false);
        var barBgRect = barBgObj.AddComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0.5f, 0.5f);
        barBgRect.anchorMax = new Vector2(0.5f, 0.5f);
        barBgRect.anchoredPosition = new Vector2(-70, 0);
        barBgRect.sizeDelta = new Vector2(barWidth, barHeight);
        var barBgImg = barBgObj.AddComponent<Image>();
        barBgImg.color = new Color(0.1f, 0.08f, 0.06f, 0.7f);

        // 条形填充
        var barFillObj = new GameObject("BarFill");
        barFillObj.transform.SetParent(strengthObj.transform, false);
        var barFillRect = barFillObj.AddComponent<RectTransform>();
        barFillRect.anchorMin = new Vector2(0.5f, 0.5f);
        barFillRect.anchorMax = new Vector2(0.5f, 0.5f);
        barFillRect.anchoredPosition = new Vector2(-70, 0);
        barFillRect.sizeDelta = new Vector2(0, barHeight);
        var barFillImg = barFillObj.AddComponent<Image>();
        barFillImg.color = Color.clear;

        // 强度文字
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(strengthObj.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(230, 0);
        labelRect.sizeDelta = new Vector2(100, 32);
        var labelText = labelObj.AddComponent<Text>();
        labelText.text = "";
        labelText.fontSize = 30;
        labelText.color = Color.clear;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.font = GetFont(30);

        // 不足6位提示
        var warnObj = new GameObject("WarnText");
        warnObj.transform.SetParent(strengthObj.transform, false);
        var warnRect = warnObj.AddComponent<RectTransform>();
        warnRect.anchorMin = new Vector2(0.5f, 0.5f);
        warnRect.anchorMax = new Vector2(0.5f, 0.5f);
        warnRect.anchoredPosition = new Vector2(230, 0);
        warnRect.sizeDelta = new Vector2(200, 32);
        var warnText = warnObj.AddComponent<Text>();
        warnText.text = "密码至少6位";
        warnText.fontSize = 30;
        warnText.color = new Color(0.9f, 0.3f, 0.2f, 0.9f);
        warnText.alignment = TextAnchor.MiddleLeft;
        warnText.font = GetFont(30);

        passwordField.onValueChanged.AddListener(val =>
        {
            if (string.IsNullOrEmpty(val))
            {
                strengthObj.SetActive(false);
                return;
            }

            strengthObj.SetActive(true);

            if (val.Length < 6)
            {
                barFillRect.sizeDelta = new Vector2(0, barHeight);
                barFillRect.anchoredPosition = new Vector2(-70 - barWidth / 2, 0);
                barFillImg.color = Color.clear;
                labelText.text = "";
                labelText.color = Color.clear;
                warnText.gameObject.SetActive(true);
                return;
            }

            warnText.gameObject.SetActive(false);

            int score = 0;
            if (val.Length >= 8) score++;
            bool hasLower = false, hasUpper = false, hasDigit = false;
            foreach (char c in val)
            {
                if (char.IsLower(c)) hasLower = true;
                if (char.IsUpper(c)) hasUpper = true;
                if (char.IsDigit(c)) hasDigit = true;
            }
            if (hasLower) score++;
            if (hasUpper) score++;
            if (hasDigit) score++;

            Color barColor;
            string label;
            float fill;
            if (score <= 1)
            {
                barColor = new Color(0.9f, 0.25f, 0.2f);
                label = "弱";
                fill = 0.15f;
            }
            else if (score == 2)
            {
                barColor = new Color(0.9f, 0.8f, 0.2f);
                label = "一般";
                fill = 0.4f;
            }
            else if (score == 3)
            {
                barColor = new Color(0.95f, 0.6f, 0.15f);
                label = "良好";
                fill = 0.7f;
            }
            else
            {
                barColor = new Color(0.3f, 0.85f, 0.35f);
                label = "强";
                fill = 1f;
            }

            barFillImg.color = barColor;
            float fillWidth = barWidth * fill;
            barFillRect.sizeDelta = new Vector2(fillWidth, barHeight);
            barFillRect.anchoredPosition = new Vector2(-70 - barWidth / 2 + fillWidth / 2, 0);
            labelText.text = label;
            labelText.color = barColor;
        });
    }

    void CreateSmallLabel(Transform parent, string name, string content, float xOffset, float fontSize, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(xOffset, 0);
        rect.sizeDelta = new Vector2(150, 36);
        var text = obj.AddComponent<Text>();
        text.text = content;
        text.fontSize = (int)fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = GetFont((int)fontSize);
    }

    void CreateUnderline(Transform parent, float centerX, float width)
    {
        var obj = new GameObject("Underline");
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(centerX, -14);
        rect.sizeDelta = new Vector2(width, 2);
        var img = obj.AddComponent<Image>();
        img.color = new Color(0.4f, 0.6f, 1f, 0.8f);
    }

    void ShowContentDialog(string title, string content)
    {
        Canvas canvas = null;
        var loginCanvas = GameObject.Find("LoginCanvas");
        if (loginCanvas != null) canvas = loginCanvas.GetComponent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();

        var dialogObj = new GameObject("ContentDialog");
        if (canvas != null)
            dialogObj.transform.SetParent(canvas.transform, false);
        else
            dialogObj.transform.SetParent(transform, false);

        dialogObj.transform.SetAsLastSibling();

        var dialogRect = dialogObj.AddComponent<RectTransform>();
        dialogRect.anchorMin = Vector2.zero;
        dialogRect.anchorMax = Vector2.one;
        dialogRect.sizeDelta = Vector2.zero;

        var maskObj = new GameObject("Mask");
        maskObj.transform.SetParent(dialogObj.transform, false);
        var maskRect = maskObj.AddComponent<RectTransform>();
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.sizeDelta = Vector2.zero;
        var maskImg = maskObj.AddComponent<Image>();
        maskImg.color = new Color(0, 0, 0, 0.7f);
        var maskBtn = maskObj.AddComponent<Button>();
        maskBtn.targetGraphic = maskImg;
        maskBtn.onClick.AddListener(() => Destroy(dialogObj));

        var panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(dialogObj.transform, false);
        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(1968, 1092);
        var panelImg = panelObj.AddComponent<Image>();
        if (dialogBgSprite != null)
        {
            panelImg.sprite = dialogBgSprite;
            panelImg.type = Image.Type.Simple;
            panelImg.preserveAspect = true;
        }
        else
        {
            panelImg.color = new Color(0.18f, 0.12f, 0.06f, 0.95f);
        }

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.71f);
        titleRect.anchorMax = new Vector2(0.9f, 0.91f);
        titleRect.sizeDelta = Vector2.zero;
        var titleText = titleObj.AddComponent<Text>();
        titleText.text = title;
        titleText.fontSize = 56;
        titleText.color = new Color(0.94f, 0.82f, 0.38f);
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.font = GetFont(56);
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;

        var textObj = new GameObject("Content");
        textObj.transform.SetParent(panelObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.18f, 0.30f);
        textRect.anchorMax = new Vector2(0.88f, 0.72f);
        textRect.sizeDelta = Vector2.zero;
        var textBgImg = textObj.AddComponent<Image>();
        textBgImg.color = new Color(0, 0, 0, 0);
        textObj.AddComponent<ScrollAreaCursor>();

        // 滚动条
        var scrollbarObj = new GameObject("Scrollbar");
        scrollbarObj.transform.SetParent(textObj.transform, false);
        var scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 0.5f);
        scrollbarRect.anchoredPosition = new Vector2(-40, 0);
        scrollbarRect.sizeDelta = new Vector2(12, 0);
        var scrollbarImg = scrollbarObj.AddComponent<Image>();
        scrollbarImg.color = new Color(0.1f, 0.08f, 0.06f, 0.3f);

        var scrollbarHandleImgObj = new GameObject("Handle");
        scrollbarHandleImgObj.transform.SetParent(scrollbarObj.transform, false);
        var scrollbarHandleRect = scrollbarHandleImgObj.AddComponent<RectTransform>();
        scrollbarHandleRect.anchorMin = Vector2.zero;
        scrollbarHandleRect.anchorMax = new Vector2(1, 1);
        scrollbarHandleRect.sizeDelta = Vector2.zero;
        var scrollbarHandleImg = scrollbarHandleImgObj.AddComponent<Image>();
        scrollbarHandleImg.color = new Color(0.7f, 0.65f, 0.55f, 0.8f);

        var scrollbar = scrollbarObj.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = scrollbarHandleRect;
        scrollbar.targetGraphic = scrollbarHandleImg;

        var viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(textObj.transform, false);
        var viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = new Vector2(-20, 0);
        var viewportImg = viewportObj.AddComponent<Image>();
        viewportImg.color = new Color(0, 0, 0, 0.01f);
        var mask = viewportObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var scrollContent = new GameObject("ScrollContent");
        scrollContent.transform.SetParent(viewportObj.transform, false);
        var scrollContentRect = scrollContent.AddComponent<RectTransform>();
        scrollContentRect.anchorMin = new Vector2(0, 1);
        scrollContentRect.anchorMax = new Vector2(1, 1);
        scrollContentRect.pivot = new Vector2(0.5f, 1);
        scrollContentRect.sizeDelta = new Vector2(0, 0);

        var fitter = scrollContent.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var text = scrollContent.AddComponent<Text>();
        text.text = content;
        text.fontSize = 36;
        text.color = new Color(0.9f, 0.85f, 0.75f);
        text.alignment = TextAnchor.UpperLeft;
        text.font = GetFont(36);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.lineSpacing = 1.4f;
        text.raycastTarget = false;

        var sr = textObj.AddComponent<ScrollRect>();
        sr.content = scrollContentRect;
        sr.viewport = viewportRect;
        sr.horizontal = false;
        sr.vertical = true;
        sr.verticalScrollbar = scrollbar;
        sr.scrollSensitivity = 40f;
        sr.movementType = ScrollRect.MovementType.Clamped;

        var closeBtnObj = new GameObject("CloseBtn");
        closeBtnObj.transform.SetParent(panelObj.transform, false);
        closeBtnObj.transform.SetAsLastSibling();
        var closeRect = closeBtnObj.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0, 260);
        closeRect.sizeDelta = new Vector2(422, 120);
        var closeImg = closeBtnObj.AddComponent<Image>();
        if (cancelBtnSprite != null)
        {
            closeImg.sprite = cancelBtnSprite;
            closeImg.type = Image.Type.Simple;
            closeImg.preserveAspect = false;
        }
        else
        {
            closeImg.color = new Color(0.35f, 0.3f, 0.22f);
        }
        var closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtnObj.AddComponent<ButtonHoverCursor>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(() => Destroy(dialogObj));

        var closeTextObj = new GameObject("Text");
        closeTextObj.transform.SetParent(closeBtnObj.transform, false);
        var closeTextRect = closeTextObj.AddComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.sizeDelta = Vector2.zero;
        var closeText = closeTextObj.AddComponent<Text>();
        closeText.text = "关闭";
        closeText.fontSize = 32;
        closeText.color = new Color(0.9f, 0.85f, 0.75f);
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.font = GetFont(32);
    }

    void ShowUserAgreementDialog()
    {
        ShowContentDialog("《铁路复兴：沙能冲击》用户协议",
            "一、游戏说明\n" +
            "本游戏为非商用免费独立游戏，由开发者'牛马东西'制作，\n" +
            "仅供个人学习、娱乐和教育用途。\n\n" +
            "二、知识产权\n" +
            "1. 游戏代码以开源形式发布，遵循对应开源协议\n" +
            "2. 游戏美术素材、音乐、文本内容的版权归原作者所有\n" +
            "3. 未经许可不得将本游戏素材用于商业用途\n\n" +
            "三、使用规则\n" +
            "1. 禁止对本游戏进行反编译、逆向工程\n" +
            "2. 禁止利用本游戏从事违法违规活动\n" +
            "3. 禁止篡改游戏数据后进行传播\n\n" +
            "四、免责声明\n" +
            "1. 本游戏按'现状'提供，不保证无bug或完全兼容所有设备\n" +
            "2. 因使用本游戏产生的任何直接或间接损失，开发者不承担责任\n" +
            "3. 游戏内容均为虚构，与现实人物、公司、事件无关\n\n" +
            "五、修改与更新\n" +
            "开发者保留随时修改本协议的权利，修改后的协议将在游戏内公布。");
    }

    void ShowPrivacyPolicyDialog()
    {
        ShowContentDialog("《铁路复兴：沙能冲击》隐私政策",
            "一、数据收集\n" +
            "本游戏为纯单机游戏，不收集、不传输、不存储任何\n" +
            "玩家个人信息到远程服务器。\n\n" +
            "二、本地数据存储\n" +
            "本游戏在您的设备本地存储以下数据：\n" +
            "1. 账号信息（用户名、密码，仅保存在本地）\n" +
            "2. 游戏存档进度\n" +
            "3. 游戏设置（音量、画质等）\n" +
            "上述数据仅保存在您的设备上，不会上传至任何服务器。\n\n" +
            "三、第三方服务\n" +
            "本游戏不接入任何第三方数据分析、广告或追踪服务。\n\n" +
            "四、数据删除\n" +
            "您可以通过以下方式删除所有本地数据：\n" +
            "1. 游戏内'清除数据'功能\n" +
            "2. 卸载游戏\n" +
            "3. 手动删除游戏存档目录\n\n" +
            "五、未成年人保护\n" +
            "本游戏不收集任何个人信息，对所有年龄段用户一视同仁。\n\n" +
            "六、联系方式\n" +
            "如有疑问，请通过以下方式联系开发者：\n" +
            "lihaixuan3@gmail.com");
    }

    void CreateVersionText(Transform parent)
    {
        var versionObj = new GameObject("VersionText");
        versionObj.transform.SetParent(parent, false);
        var rect = versionObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(120, 40);
        rect.sizeDelta = new Vector2(200, 30);
        versionText = versionObj.AddComponent<Text>();
        versionText.text = GAME_VERSION;
        versionText.fontSize = 18;
        versionText.color = new Color(1f, 1f, 1f, 0.4f);
        versionText.alignment = TextAnchor.MiddleLeft;
        versionText.font = GetFont(18);
    }

    Text CreateAnnouncementEntry(Transform parent)
    {
        var announceObj = new GameObject("AnnouncementEntry");
        announceObj.transform.SetParent(parent, false);
        var rect = announceObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-140, -50);
        rect.sizeDelta = new Vector2(160, 50);

        // 背景底色（子对象）
        var bgObj = new GameObject("Bg");
        bgObj.transform.SetParent(announceObj.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.1f, 0.05f, 0.9f);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(announceObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        var text = textObj.AddComponent<Text>();
        text.text = "公告";
        text.fontSize = 24;
        text.color = new Color(1f, 0.92f, 0.7f);
        text.alignment = TextAnchor.MiddleCenter;
        text.font = GetFont(24);

        var button = announceObj.AddComponent<Button>();
        announceObj.AddComponent<ButtonHoverCursor>();
        button.targetGraphic = bgImg;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.9f, 0.7f);
        button.colors = colors;

        return text;
    }

    void OnTogglePassword()
    {
        isPasswordVisible = !isPasswordVisible;

        string saved = passwordInput.text;

        passwordInput.contentType = isPasswordVisible
            ? InputField.ContentType.Standard
            : InputField.ContentType.Password;

        passwordInput.text = "";
        passwordInput.text = saved;

        if (togglePasswordButton != null)
        {
            var img = togglePasswordButton.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = isPasswordVisible ? eyeOpenSprite : eyeClosedSprite;
            }
        }
    }

    void OnToggleRegPassword()
    {
        isRegPasswordVisible = !isRegPasswordVisible;

        string saved = regPasswordInput.text;

        regPasswordInput.contentType = isRegPasswordVisible
            ? InputField.ContentType.Standard
            : InputField.ContentType.Password;

        regPasswordInput.text = "";
        regPasswordInput.text = saved;

        if (toggleRegPasswordButton != null)
        {
            var img = toggleRegPasswordButton.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = isRegPasswordVisible ? eyeOpenSprite : eyeClosedSprite;
            }
        }
    }

    void OnToggleConfirmPassword()
    {
        isConfirmPasswordVisible = !isConfirmPasswordVisible;

        string saved = confirmPasswordInput.text;

        confirmPasswordInput.contentType = isConfirmPasswordVisible
            ? InputField.ContentType.Standard
            : InputField.ContentType.Password;

        confirmPasswordInput.text = "";
        confirmPasswordInput.text = saved;

        if (toggleConfirmPasswordButton != null)
        {
            var img = toggleConfirmPasswordButton.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = isConfirmPasswordVisible ? eyeOpenSprite : eyeClosedSprite;
            }
        }
    }

    void OnForgotPassword()
    {
        ShowConfirmDialog("确定要重置账号吗？\n将删除本地账号凭证，需要重新注册", () =>
        {
            if (File.Exists(authFilePath))
            {
                File.Delete(authFilePath);
            }
            ShowHint("账号已重置，请重新注册", true);
            Invoke("ShowRegister", 1.5f);
        });
    }

    void ShowConfirmDialog(string message, System.Action onConfirm)
    {
        Canvas canvas = null;
        var loginCanvas = GameObject.Find("LoginCanvas");
        if (loginCanvas != null) canvas = loginCanvas.GetComponent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();

        var dialogObj = new GameObject("ConfirmDialog");
        if (canvas != null)
            dialogObj.transform.SetParent(canvas.transform, false);
        else
            dialogObj.transform.SetParent(transform, false);

        dialogObj.transform.SetAsLastSibling();

        var dialogRect = dialogObj.AddComponent<RectTransform>();
        dialogRect.anchorMin = Vector2.zero;
        dialogRect.anchorMax = Vector2.one;
        dialogRect.sizeDelta = Vector2.zero;

        // 半透明遮罩
        var maskObj = new GameObject("Mask");
        maskObj.transform.SetParent(dialogObj.transform, false);
        var maskRect = maskObj.AddComponent<RectTransform>();
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.sizeDelta = Vector2.zero;
        var maskImg = maskObj.AddComponent<Image>();
        maskImg.color = new Color(0, 0, 0, 0.7f);
        var maskBtn = maskObj.AddComponent<Button>();
        maskBtn.targetGraphic = maskImg;

        // 弹窗主面板（使用素材或纯色）
        var panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(dialogObj.transform, false);
        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(1640, 910);
        var panelImg = panelObj.AddComponent<Image>();
        if (dialogBgSprite != null)
        {
            panelImg.sprite = dialogBgSprite;
            panelImg.type = Image.Type.Simple;
            panelImg.preserveAspect = true;
        }
        else
        {
            panelImg.color = new Color(0.18f, 0.12f, 0.06f, 0.95f);
        }

        // 标题文字
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.71f);
        titleRect.anchorMax = new Vector2(0.9f, 0.91f);
        titleRect.sizeDelta = Vector2.zero;
        var titleText = titleObj.AddComponent<Text>();
        titleText.text = "账  号  重  置";
        titleText.fontSize = 64;
        titleText.color = new Color(0.94f, 0.82f, 0.38f);
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.font = GetFont(64);
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;

        // 提示文字
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(panelObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.31f);
        textRect.anchorMax = new Vector2(0.95f, 0.74f);
        textRect.sizeDelta = Vector2.zero;
        var text = textObj.AddComponent<Text>();
        text.text = message;
        text.fontSize = 42;
        text.color = new Color(0.9f, 0.85f, 0.75f);
        text.alignment = TextAnchor.MiddleCenter;
        text.font = GetFont(42);
        text.lineSpacing = 1.3f;

        // 确认按钮（使用素材）
        var confirmBtnObj = new GameObject("ConfirmBtn");
        confirmBtnObj.transform.SetParent(panelObj.transform, false);
        var confirmRect = confirmBtnObj.AddComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.5f, 0f);
        confirmRect.anchorMax = new Vector2(0.5f, 0f);
        confirmRect.anchoredPosition = new Vector2(-220, 220);
        confirmRect.sizeDelta = new Vector2(407, 120);
        var confirmImg = confirmBtnObj.AddComponent<Image>();
        if (confirmBtnSprite != null)
        {
            confirmImg.sprite = confirmBtnSprite;
            confirmImg.type = Image.Type.Simple;
            confirmImg.preserveAspect = false;
        }
        else
        {
            confirmImg.color = new Color(0.7f, 0.2f, 0.12f);
        }
        var confirmBtn = confirmBtnObj.AddComponent<Button>();
        confirmBtnObj.AddComponent<ButtonHoverCursor>();
        confirmBtn.targetGraphic = confirmImg;

        var confirmTextObj = new GameObject("Text");
        confirmTextObj.transform.SetParent(confirmBtnObj.transform, false);
        var confirmTextRect = confirmTextObj.AddComponent<RectTransform>();
        confirmTextRect.anchorMin = Vector2.zero;
        confirmTextRect.anchorMax = Vector2.one;
        confirmTextRect.sizeDelta = Vector2.zero;
        var confirmText = confirmTextObj.AddComponent<Text>();
        confirmText.text = "确认重置";
        confirmText.fontSize = 32;
        confirmText.color = new Color(1f, 0.95f, 0.9f);
        confirmText.alignment = TextAnchor.MiddleCenter;
        confirmText.font = GetFont(32);

        // 取消按钮（使用素材）
        var cancelBtnObj = new GameObject("CancelBtn");
        cancelBtnObj.transform.SetParent(panelObj.transform, false);
        var cancelRect = cancelBtnObj.AddComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.5f, 0f);
        cancelRect.anchorMax = new Vector2(0.5f, 0f);
        cancelRect.anchoredPosition = new Vector2(220, 220);
        cancelRect.sizeDelta = new Vector2(422, 120);
        var cancelImg = cancelBtnObj.AddComponent<Image>();
        if (cancelBtnSprite != null)
        {
            cancelImg.sprite = cancelBtnSprite;
            cancelImg.type = Image.Type.Simple;
            cancelImg.preserveAspect = false;
        }
        else
        {
            cancelImg.color = new Color(0.35f, 0.3f, 0.22f);
        }
        var cancelBtn = cancelBtnObj.AddComponent<Button>();
        cancelBtnObj.AddComponent<ButtonHoverCursor>();
        cancelBtn.targetGraphic = cancelImg;

        var cancelTextObj = new GameObject("Text");
        cancelTextObj.transform.SetParent(cancelBtnObj.transform, false);
        var cancelTextRect = cancelTextObj.AddComponent<RectTransform>();
        cancelTextRect.anchorMin = Vector2.zero;
        cancelTextRect.anchorMax = Vector2.one;
        cancelTextRect.sizeDelta = Vector2.zero;
        var cancelText = cancelTextObj.AddComponent<Text>();
        cancelText.text = "取消";
        cancelText.fontSize = 32;
        cancelText.color = new Color(0.9f, 0.85f, 0.75f);
        cancelText.alignment = TextAnchor.MiddleCenter;
        cancelText.font = GetFont(32);

        maskBtn.onClick.AddListener(() => Destroy(dialogObj));

        confirmBtn.onClick.AddListener(() =>
        {
            onConfirm?.Invoke();
            Destroy(dialogObj);
        });

        cancelBtn.onClick.AddListener(() =>
        {
            Destroy(dialogObj);
        });
    }

    [System.Serializable]
    public class AuthData
    {
        public string username;
        public string password;
        public string lastLogin;
    }
}

    public class InputFieldCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static Texture2D iBeamTexture;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (iBeamTexture == null) iBeamTexture = LoginManager.LoadCursorTexture("Cursors/cursor_ibeam", 3);
        Cursor.SetCursor(iBeamTexture, new Vector2(iBeamTexture.width / 2, iBeamTexture.height / 2), CursorMode.ForceSoftware);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Cursor.SetCursor(LoginManager.GetArrowTexture(), Vector2.zero, CursorMode.ForceSoftware);
    }
}

public class ButtonHoverCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private static Texture2D handTexture;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (handTexture == null) handTexture = LoginManager.LoadCursorTexture("Cursors/cursor_hand", 3);
        Cursor.SetCursor(handTexture, new Vector2(0, 0), CursorMode.ForceSoftware);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Cursor.SetCursor(LoginManager.GetArrowTexture(), Vector2.zero, CursorMode.ForceSoftware);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        LoginManager.PlayClickSound();
    }
}

public class ScrollAreaCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static Texture2D scrollCursor;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (scrollCursor == null) scrollCursor = LoadScrollCursor();
        if (scrollCursor != null)
            Cursor.SetCursor(scrollCursor, new Vector2(scrollCursor.width / 2, scrollCursor.height / 2), CursorMode.ForceSoftware);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Cursor.SetCursor(LoginManager.GetArrowTexture(), Vector2.zero, CursorMode.ForceSoftware);
    }

    static Texture2D LoadScrollCursor()
    {
        byte[] pngData = File.ReadAllBytes(Path.Combine(Application.dataPath, "Resources/Cursors/cursor_resize_vertical.png"));
        if (pngData == null || pngData.Length == 0) return null;

        Texture2D src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        src.LoadImage(pngData);
        src.Apply();

        int scale = 3;
        int nw = src.width * scale;
        int nh = src.height * scale;
        Texture2D scaled = new Texture2D(nw, nh, TextureFormat.RGBA32, false);
        Color[] srcPx = src.GetPixels();
        Color[] dstPx = new Color[nw * nh];
        for (int y = 0; y < nh; y++)
        {
            for (int x = 0; x < nw; x++)
            {
                dstPx[y * nw + x] = srcPx[(y / scale) * src.width + (x / scale)];
            }
        }
        scaled.SetPixels(dstPx);
        scaled.Apply();
        Object.Destroy(src);
        return scaled;
    }
}
