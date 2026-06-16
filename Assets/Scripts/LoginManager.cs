using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
    private Text hintText;
    private Text autoLoginUserText;
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

    private string authFilePath;

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
        SetupCamera();
        SetupCanvas();
        LoadBackground();
        SetupAudio();
        CheckAutoLogin();
    }

    private Font customFont;

    void LoadSprites()
    {
        panelSprite = Resources.Load<Sprite>("UI/Login/panel_bg");
        inputSprite = Resources.Load<Sprite>("UI/Login/input_field");
        buttonSprite = Resources.Load<Sprite>("UI/Login/button_primary");
        titleLogoSprite = Resources.Load<Sprite>("UI/Login/title_logo");
        customFont = Resources.Load<Font>("Fonts/SarasaMonoSC-Regular");
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

        CreateTitleLogo(canvasObj.transform);

        loginPanel = CreateLoginPanel(canvasObj.transform);
        registerPanel = CreateRegisterPanel(canvasObj.transform);
        autoLoginPanel = CreateAutoLoginPanel(canvasObj.transform);
        registerPanel.SetActive(false);
        autoLoginPanel.SetActive(false);
    }

    void CreateTitleLogo(Transform parent)
    {
        if (titleLogoSprite == null) return;

        var titleLogoObj = new GameObject("TitleLogo");
        titleLogoObj.transform.SetParent(parent, false);
        var titleRect = titleLogoObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0, 700);
        titleRect.sizeDelta = new Vector2(900, 220);
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

        usernameInput = CreateInputField(panel.transform, "UsernameInput", "用户名", 120, 50f);
        passwordInput = CreateInputField(panel.transform, "PasswordInput", "密码", -80, 50f);
        passwordInput.contentType = InputField.ContentType.Password;

        loginButton = CreateButton(panel.transform, "LoginButton", "登录", -230, -200f, 750f, 225f);
        loginButton.onClick.AddListener(OnLogin);

        switchToRegisterButton = CreateButton(panel.transform, "SwitchToRegister", "没有账号？注册", -230, 300f, 550f, 180f);
        switchToRegisterButton.onClick.AddListener(ShowRegister);

        hintText = CreateText(panel.transform, "Hint", "", 17, Color.white, -360);

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

        hintText = CreateText(panel.transform, "Hint", "", 17, Color.white, -620);

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
        if (hintText != null) hintText.text = "";
    }

    public void ShowRegister()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        autoLoginPanel.SetActive(false);
        if (hintText != null) hintText.text = "";
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
        if (!File.Exists(authFilePath))
        {
            ShowHint("请先注册账号", false);
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
            hintText.color = success ? new Color(0.5f, 0.85f, 0.3f) : new Color(1f, 0.4f, 0.4f);
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
        var inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent, false);
        var rect = inputObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(xOffset, yOffset);
        rect.sizeDelta = new Vector2(1000, 168);

        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(inputObj.transform, false);
        var fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = new Vector2(-150, -92);
        fillRect.anchoredPosition = Vector2.zero;
        var fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.91f, 0.86f, 0.78f);

        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(inputObj.transform, false);
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

        var input = inputObj.AddComponent<InputField>();
        input.caretColor = new Color(0.95f, 0.9f, 0.82f);
        input.selectionColor = new Color(0.94f, 0.82f, 0.38f, 0.3f);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(inputObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(480, 0);
        textRect.offsetMax = new Vector2(-15, 0);
        var text = textObj.AddComponent<Text>();
        text.fontSize = 36;
        text.color = new Color(0.95f, 0.9f, 0.82f);
        text.alignment = TextAnchor.MiddleLeft;
        text.font = GetFont(36);

        var placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(inputObj.transform, false);
        var placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;
        placeholderRect.offsetMin = new Vector2(480, 0);
        placeholderRect.offsetMax = new Vector2(-15, 0);
        var placeholderText = placeholderObj.AddComponent<Text>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 36;
        placeholderText.color = new Color(0.55f, 0.5f, 0.45f);
        placeholderText.alignment = TextAnchor.MiddleLeft;
        placeholderText.font = GetFont(36);

        input.textComponent = text;
        input.placeholder = placeholderText;

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

    [System.Serializable]
    public class AuthData
    {
        public string username;
        public string password;
        public string lastLogin;
    }
}
