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
    private RawImage backgroundImage;
    private AudioSource bgmSource;

    private string authFilePath;

    void Start()
    {
        authFilePath = Path.Combine(Application.persistentDataPath, "auth.json");

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        SetupCamera();
        SetupCanvas();
        LoadBackground();
        PlayBGM();
        CheckAutoLogin();
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

        loginPanel = CreatePanel(canvasObj.transform, "LoginPanel");
        registerPanel = CreatePanel(canvasObj.transform, "RegisterPanel");
        autoLoginPanel = CreatePanel(canvasObj.transform, "AutoLoginPanel");
        registerPanel.SetActive(false);
        autoLoginPanel.SetActive(false);

        SetupLoginPanel();
        SetupRegisterPanel();
        SetupAutoLoginPanel();
        SetupAudio(canvasObj.transform);
    }

    void SetupLoginPanel()
    {
        CreateText(loginPanel.transform, "Title", "铁路复兴：沙能冲击", 36, Color.white, 200);
        CreateText(loginPanel.transform, "Subtitle", "Railway Renaissance: Sand Energy Impact", 18, new Color(1, 1, 1, 0.6f), 150);

        usernameInput = CreateInputField(loginPanel.transform, "UsernameInput", "用户名", 60);
        passwordInput = CreateInputField(loginPanel.transform, "PasswordInput", "密码", 0);
        passwordInput.contentType = InputField.ContentType.Password;

        loginButton = CreateButton(loginPanel.transform, "LoginButton", "登录", -80);
        loginButton.onClick.AddListener(OnLogin);

        switchToRegisterButton = CreateButton(loginPanel.transform, "SwitchToRegister", "没有账号？注册", -130);
        switchToRegisterButton.onClick.AddListener(ShowRegister);

        hintText = CreateText(loginPanel.transform, "Hint", "", 14, Color.white, -170);
    }

    void SetupRegisterPanel()
    {
        CreateText(registerPanel.transform, "Title", "注册新账号", 32, Color.white, 200);

        var regUsernameInput = CreateInputField(registerPanel.transform, "RegUsernameInput", "用户名（至少3个字符）", 100);
        var regPasswordInput = CreateInputField(registerPanel.transform, "RegPasswordInput", "密码（至少4个字符）", 40);
        regPasswordInput.contentType = InputField.ContentType.Password;
        confirmPasswordInput = CreateInputField(registerPanel.transform, "ConfirmPasswordInput", "确认密码", -20);
        confirmPasswordInput.contentType = InputField.ContentType.Password;

        registerButton = CreateButton(registerPanel.transform, "RegisterButton", "注册", -100);
        registerButton.onClick.AddListener(OnRegister);

        switchToLoginButton = CreateButton(registerPanel.transform, "SwitchToLogin", "已有账号？登录", -150);
        switchToLoginButton.onClick.AddListener(ShowLogin);

        hintText = CreateText(registerPanel.transform, "Hint", "", 14, Color.white, -190);
    }

    void SetupAutoLoginPanel()
    {
        CreateText(autoLoginPanel.transform, "Title", "欢迎回来", 32, Color.white, 100);
        autoLoginUserText = CreateText(autoLoginPanel.transform, "Username", "", 24, new Color(0.83f, 0.72f, 0.44f), 40);
        CreateText(autoLoginPanel.transform, "Hint", "点击任意处继续", 16, new Color(1, 1, 1, 0.5f), -40);

        autoLoginButton = CreateButton(autoLoginPanel.transform, "AutoLoginButton", "进入游戏", -120);
        autoLoginButton.onClick.AddListener(OnAutoLogin);
    }

    void SetupAudio(Transform parent)
    {
        var audioObj = new GameObject("BGM");
        audioObj.transform.SetParent(parent, false);
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

    void PlayBGM()
    {
        if (bgmSource == null) return;
        bgmSource.loop = true;
        bgmSource.volume = 0.3f;
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
        hintText.text = "";
    }

    public void ShowRegister()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        autoLoginPanel.SetActive(false);
        hintText.text = "";
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
            hintText.color = success ? new Color(0.32f, 0.78f, 0.1f) : new Color(1f, 0.42f, 0.62f);
        }
    }

    GameObject CreatePanel(Transform parent, string name)
    {
        var panelObj = new GameObject(name);
        panelObj.transform.SetParent(parent, false);
        var rect = panelObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.3f, 0.1f);
        rect.anchorMax = new Vector2(0.7f, 0.9f);
        rect.sizeDelta = Vector2.zero;
        var image = panelObj.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.7f);
        return panelObj;
    }

    Text CreateText(Transform parent, string name, string content, int fontSize, Color color, float yOffset)
    {
        var textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        var rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, yOffset);
        rect.sizeDelta = new Vector2(400, 40);
        var text = textObj.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        return text;
    }

    InputField CreateInputField(Transform parent, string name, string placeholder, float yOffset)
    {
        var inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent, false);
        var rect = inputObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, yOffset);
        rect.sizeDelta = new Vector2(300, 40);
        var image = inputObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        var input = inputObj.AddComponent<InputField>();

        var placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(inputObj.transform, false);
        var placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;
        placeholderRect.offsetMin = new Vector2(10, 0);
        placeholderRect.offsetMax = new Vector2(-10, 0);
        var placeholderText = placeholderObj.AddComponent<Text>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 14;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f);
        placeholderText.alignment = TextAnchor.MiddleLeft;
        placeholderText.font = Font.CreateDynamicFontFromOSFont("Arial", 14);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(inputObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);
        var text = textObj.AddComponent<Text>();
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.font = Font.CreateDynamicFontFromOSFont("Arial", 14);

        input.textComponent = text;
        input.placeholder = placeholderText;
        return input;
    }

    Button CreateButton(Transform parent, string name, string label, float yOffset)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, yOffset);
        rect.sizeDelta = new Vector2(200, 45);
        var image = btnObj.AddComponent<Image>();
        image.color = new Color(0.3f, 0.5f, 0.8f);
        var button = btnObj.AddComponent<Button>();

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        var text = textObj.AddComponent<Text>();
        text.text = label;
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
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
