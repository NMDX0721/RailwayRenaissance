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
    private Button autoLoginButton;
    private Button forgotPasswordButton;
    private Button togglePasswordButton;
    private Toggle rememberPasswordToggle;
    private Text hintText;
    private Text autoLoginUserText;
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

    private string authFilePath;
    private bool isPasswordVisible = false;
    private const string GAME_VERSION = "v1.0.0";

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
        if (cursorTexture == null) cursorTexture = GenerateArrowForSetup();
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.ForceSoftware);
    }

    public static Texture2D GenerateArrowForSetup()
    {
        int w = 48, h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        Color clear = new Color(0, 0, 0, 0);
        Color fillColor = new Color(0.94f, 0.82f, 0.38f, 1f);
        Color edgeColor = new Color(0.25f, 0.15f, 0.08f, 1f);
        Color highlightColor = new Color(1f, 0.95f, 0.8f, 1f);

        for (int i = 0; i < px.Length; i++) px[i] = clear;

        string[] shape = {
            "*...............................................",
            "*#..............................................",
            "*##.............................................",
            "*###............................................",
            "*####...........................................",
            "*#####..........................................",
            "*######.........................................",
            "*#######........................................",
            "*########.......................................",
            "*#########......................................",
            "*##########.....................................",
            "*###########....................................",
            "*############...................................",
            "*#############..................................",
            "*##############................................",
            "..............................................",
            ".*.............................................",
            "..*............................................",
            "...*...........................................",
            "....*..........................................",
            ".....*.........................................",
            "......*........................................",
            ".......*.......................................",
            "........*......................................",
            ".........*.....................................",
            ".............................................."
        };

        for (int y = 0; y < shape.Length && y < h; y++)
        {
            for (int x = 0; x < shape[y].Length && x < w; x++)
            {
                if (shape[y][x] == '*' || shape[y][x] == '#')
                {
                    int py = y * w + x;
                    px[py] = fillColor;

                    bool leftFilled = x > 0 && (shape[y][x - 1] == '*' || shape[y][x - 1] == '#');
                    bool topFilled = y > 0 && x < shape[y - 1].Length && (shape[y - 1][x] == '*' || shape[y - 1][x] == '#');
                    if (!leftFilled) px[py - 1] = edgeColor;
                    if (!topFilled) px[py - w] = edgeColor;
                    if (x <= 3 && y <= 3) px[py] = highlightColor;
                }
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        AddOutlineToTex(tex, new Color(0.15f, 0.1f, 0.05f, 1f));
        return tex;
    }

    static void AddOutlineToTex(Texture2D tex, Color outlineColor)
    {
        int w = tex.width, h = tex.height;
        Color[] px = tex.GetPixels();
        Color clear = new Color(0, 0, 0, 0);
        Color[] result = new Color[w * h];
        for (int i = 0; i < result.Length; i++) result[i] = clear;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (px[y * w + x].a > 0.1f)
                {
                    result[y * w + x] = px[y * w + x];
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                            {
                                if (px[ny * w + nx].a < 0.1f)
                                    result[ny * w + nx] = outlineColor;
                            }
                        }
                    }
                }
            }
        }
        tex.SetPixels(result);
        tex.Apply();
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
                    // 闭眼：椭圆轮廓 + 斜线划过
                    float outer = (dx * dx) / (24f * 24f) + (dy * dy) / (14f * 14f);
                    float inner = (dx * dx) / (18f * 18f) + (dy * dy) / (9f * 9f);
                    bool onRing = outer <= 1f && inner >= 1f;

                    // 斜线：左上到右下
                    float lineDist = Mathf.Abs(dy - dx * 0.6f);
                    bool onLine = lineDist < 2.5f && Mathf.Abs(dx) < 20f && Mathf.Abs(dy) < 12f;

                    if (onRing || onLine)
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
        hintText.transform.SetAsLastSibling();
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

        // 背景
        var bgObj = new GameObject("Bg");
        bgObj.transform.SetParent(hintObj.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.8f, 0.2f, 0.15f, 0.85f);

        // 文字
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(hintObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        var text = textObj.AddComponent<Text>();
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
        titleObj.transform.SetParent(parent, false);
        var rect = titleObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, 270);
        rect.sizeDelta = new Vector2(500, 90);

        var text = titleObj.AddComponent<Text>();
        text.text = "登    录";
        text.fontSize = 58;
        text.color = new Color(0.94f, 0.82f, 0.38f);
        text.alignment = TextAnchor.MiddleCenter;
        text.font = GetFont(58);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;

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

        // 记住密码复选框
        rememberPasswordToggle = CreateCheckbox(panel.transform, "RememberPassword", "记住密码", -380, -230);

        // 忘记密码链接（与记住密码同行右侧）
        forgotPasswordButton = CreateTextButton(panel.transform, "ForgotPassword", "忘记密码？", 350, -230);
        forgotPasswordButton.onClick.AddListener(OnForgotPassword);

        // 登录按钮（加宽）
        loginButton = CreateButton(panel.transform, "LoginButton", "登录", -350, -200f, 850f, 200f);
        loginButton.onClick.AddListener(OnLogin);

        // 没有账号？注册按钮
        switchToRegisterButton = CreateButton(panel.transform, "SwitchToRegister", "没有账号？注册", -350, 300f, 550f, 200f);
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

        CreateText(panel.transform, "Title", "注册新账号", 34, new Color(0.94f, 0.82f, 0.38f), 420);

        var regUsernameInput = CreateInputField(panel.transform, "RegUsernameInput", "用户名（至少3个字符）", 250);
        var regPasswordInput = CreateInputField(panel.transform, "RegPasswordInput", "密码（至少4个字符）", -50);
        regPasswordInput.contentType = InputField.ContentType.Password;
        confirmPasswordInput = CreateInputField(panel.transform, "ConfirmPasswordInput", "确认密码", -350);
        confirmPasswordInput.contentType = InputField.ContentType.Password;

        registerButton = CreateButton(panel.transform, "RegisterButton", "注册", -520, -200f, 750f, 225f);
        registerButton.onClick.AddListener(OnRegister);

        switchToLoginButton = CreateButton(panel.transform, "SwitchToLogin", "已有账号？登录", -520, 300f, 550f, 180f);
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

        CreateText(panel.transform, "Title", "欢迎回来", 34, new Color(0.94f, 0.82f, 0.38f), 380);
        autoLoginUserText = CreateText(panel.transform, "Username", "", 26, new Color(0.94f, 0.82f, 0.38f), 250);
        CreateText(panel.transform, "Hint", "点击进入游戏", 17, new Color(1, 1, 1, 0.4f), 150);

        autoLoginButton = CreateButton(panel.transform, "AutoLoginButton", "进入游戏", 0);
        autoLoginButton.onClick.AddListener(OnAutoLogin);

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
        if (File.Exists(authFilePath))
        {
            string json = File.ReadAllText(authFilePath);
            var auth = JsonUtility.FromJson<AuthData>(json);
            if (auth != null && !string.IsNullOrEmpty(auth.username))
            {
                ShowAutoLogin(auth.username);
                return;
            }
        }
        ShowLogin();
    }

    public void ShowLogin()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        autoLoginPanel.SetActive(false);
        if (hintText != null) hintText.transform.parent.gameObject.SetActive(false);
    }

    public void ShowRegister()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        autoLoginPanel.SetActive(false);
        if (hintText != null) hintText.transform.parent.gameObject.SetActive(false);
    }

    void ShowAutoLogin(string username)
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        autoLoginPanel.SetActive(true);
        autoLoginUserText.text = username;
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

    public void OnRegister()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;
        string confirm = confirmPasswordInput.text;

        if (string.IsNullOrEmpty(username) || username.Length < 3)
        {
            ShowHint("用户名至少需要3个字符", false);
            return;
        }
        if (string.IsNullOrEmpty(password) || password.Length < 4)
        {
            ShowHint("密码至少需要4个字符", false);
            return;
        }
        if (password != confirm)
        {
            ShowHint("两次输入的密码不一致", false);
            return;
        }

        var auth = new AuthData
        {
            username = username,
            password = password,
            lastLogin = System.DateTime.Now.ToString("o")
        };
        File.WriteAllText(authFilePath, JsonUtility.ToJson(auth, true));
        ShowHint("注册成功！正在进入游戏...", true);
        Invoke("EnterGame", 1f);
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
            hintText.text = message;
            hintText.color = success ? new Color(0.7f, 1f, 0.6f) : new Color(1f, 0.95f, 0.9f);
            hintText.transform.parent.gameObject.SetActive(true);

            var bg = hintText.transform.parent.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = success
                    ? new Color(0.2f, 0.6f, 0.15f, 0.85f)
                    : new Color(0.8f, 0.2f, 0.15f, 0.85f);
            }
        }
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

        var input = inputObj.AddComponent<InputField>();
        input.caretColor = Color.black;
        input.caretWidth = 2;
        input.selectionColor = new Color(0.85f, 0.65f, 0.15f, 0.8f);
        input.textComponent = text;
        input.targetGraphic = inputImg;
        input.interactable = true;
        input.transition = Selectable.Transition.None;

        return input;
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
        toggle.targetGraphic = bgImg;
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
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.9f, 0.7f);
        colors.pressedColor = new Color(0.8f, 0.7f, 0.5f);
        button.colors = colors;

        return button;
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

        var text = announceObj.AddComponent<Text>();
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

        // 先保存文本
        string saved = passwordInput.text;

        // 切换类型
        passwordInput.contentType = isPasswordVisible
            ? InputField.ContentType.Standard
            : InputField.ContentType.Password;

        // 清空并重新赋值，触发刷新
        passwordInput.text = "";
        passwordInput.text = saved;

        // 更新眼睛图标
        if (togglePasswordButton != null)
        {
            var img = togglePasswordButton.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = isPasswordVisible ? eyeOpenSprite : eyeClosedSprite;
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
    private static Texture2D arrowTexture;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (iBeamTexture == null) iBeamTexture = GenerateIBeam();
        Cursor.SetCursor(iBeamTexture, new Vector2(8, 17), CursorMode.ForceSoftware);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (arrowTexture == null) arrowTexture = LoginManager.GenerateArrowForSetup();
        Cursor.SetCursor(arrowTexture, Vector2.zero, CursorMode.ForceSoftware);
    }

    static void AddOutline(Texture2D tex, Color outlineColor)
    {
        int w = tex.width, h = tex.height;
        Color[] px = tex.GetPixels();
        Color clear = new Color(0, 0, 0, 0);
        Color[] result = new Color[w * h];
        for (int i = 0; i < result.Length; i++) result[i] = clear;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (px[y * w + x].a > 0.1f)
                {
                    result[y * w + x] = px[y * w + x];
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                            {
                                if (px[ny * w + nx].a < 0.1f)
                                    result[ny * w + nx] = outlineColor;
                            }
                        }
                    }
                }
            }
        }
        tex.SetPixels(result);
        tex.Apply();
    }

    static Texture2D GenerateIBeam()
    {
        int w = 18, h = 38;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        Color clear = new Color(0, 0, 0, 0);
        Color fillColor = new Color(0.15f, 0.1f, 0.05f, 1f);

        for (int i = 0; i < px.Length; i++) px[i] = clear;

        for (int y = 0; y < h; y++)
        {
            for (int x = 6; x <= 10; x++) px[y * w + x] = fillColor;

            if (y >= h - 3)
            {
                for (int x = 2; x <= 15; x++) px[y * w + x] = fillColor;
            }
            if (y <= 2)
            {
                for (int x = 2; x <= 15; x++) px[y * w + x] = fillColor;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        AddOutline(tex, new Color(1f, 0.95f, 0.8f, 1f));
        return tex;
    }
}

public class ButtonHoverCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private static Texture2D handTexture;
    private static Texture2D arrowTexture;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (handTexture == null) handTexture = GenerateHand();
        Cursor.SetCursor(handTexture, new Vector2(3, 0), CursorMode.ForceSoftware);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (arrowTexture == null) arrowTexture = LoginManager.GenerateArrowForSetup();
        Cursor.SetCursor(arrowTexture, Vector2.zero, CursorMode.ForceSoftware);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        LoginManager.PlayClickSound();
    }

    static void AddOutline(Texture2D tex, Color outlineColor)
    {
        int w = tex.width, h = tex.height;
        Color[] px = tex.GetPixels();
        Color clear = new Color(0, 0, 0, 0);
        Color[] result = new Color[w * h];
        for (int i = 0; i < result.Length; i++) result[i] = clear;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (px[y * w + x].a > 0.1f)
                {
                    result[y * w + x] = px[y * w + x];
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                            {
                                if (px[ny * w + nx].a < 0.1f)
                                    result[ny * w + nx] = outlineColor;
                            }
                        }
                    }
                }
            }
        }
        tex.SetPixels(result);
        tex.Apply();
    }

    static Texture2D GenerateHand()
    {
        int w = 32, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        Color clear = new Color(0, 0, 0, 0);
        Color fillColor = new Color(0.94f, 0.82f, 0.38f, 1f);

        for (int i = 0; i < px.Length; i++) px[i] = clear;

        string[] shape = {
            "......*.........................",
            ".....**.........................",
            ".....*#*........................",
            ".....*##*.......................",
            ".....*#*#*......................",
            ".....*#*#*#.....................",
            ".....*#*#*#*....................",
            ".....*#*#*#*#*..................",
            ".....*#*#*#*#*#*................",
            ".....*#*#*#*#*#*#...............",
            ".....*#*#*#*#*#*#*..............",
            ".....*#*#*#*#*#*#*#.............",
            ".....*#*#*#*#*#*#*#*............",
            ".....*#*#*#*#*#*#*#*#...........",
            ".....*#*#*#*#*#*#*#*#*..........",
            ".....*#*#*#*#*#*#*#*#*#*........",
            ".....*#*#*#*#*#*#*#*#*#*#.......",
            ".....*#*#*#*#*#*#*#*#*#*#*......",
            ".....*#*#*#*#*#*#*#*#*#*#*#.....",
            ".....*#*#*#*#*#*#*#*#*#*#*......",
            ".....*#*#*#*#*#*#*#*#*#*#.......",
            ".....*#*#*#*#*#*#*#*#*#*........",
            ".....*#*#*#*#*#*#*#*#*#.........",
            ".....*#*#*#*#*#*#*#*#*..........",
            ".....*#*#*#*#*#*#*#*#...........",
            "......*#*#*#*#*#*#*#*...........",
            ".......*#*#*#*#*#*#*............",
            "........*#*#*#*#*#*#............",
            ".........*#*#*#*#*#*............",
            "..........*#*#*#*#*#............",
            "...........*#*#*#*#*............",
            "............*******************."
        };

        for (int y = 0; y < shape.Length && y < h; y++)
        {
            for (int x = 0; x < shape[y].Length && x < w; x++)
            {
                if (shape[y][x] == '*' || shape[y][x] == '#')
                {
                    int py = y * w + x;
                    px[py] = fillColor;
                }
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        AddOutline(tex, new Color(0.15f, 0.1f, 0.05f, 1f));
        return tex;
    }
}
