using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.IO;

[RequireComponent(typeof(UIDocument))]
public class TitleScreen : MonoBehaviour
{
    private Label usernameLabel;
    private Button btnNewGame;
    private Button btnContinue;
    private Button btnArchive;
    private Button btnSettings;
    private Button btnExit;
    private Button announcementBtn;
    private VideoPlayer videoPlayer;
    private NewGameSetupUI newGameSetup;
    private TitleArchiveUI archiveUI;
    private StationBulletinUI bulletinUI;

    private readonly Color glassBg = new Color(40f/255f, 25f/255f, 15f/255f, 0.35f);
    private readonly Color glassBgHover = new Color(40f/255f, 25f/255f, 15f/255f, 0.55f);
    private readonly Color glassBgPress = new Color(40f/255f, 25f/255f, 15f/255f, 0.70f);
    private readonly Color borderNormal = new Color(1f, 220f/255f, 150f/255f, 0.55f);
    private readonly Color borderHover = new Color(1f, 230f/255f, 170f/255f, 0.85f);
    private readonly Color textNormal = new Color(1f, 215f/255f, 0f, 0.95f);
    private readonly Color textHover = new Color(1f, 230f/255f, 100f/255f, 1f);
    private static Texture2D handCursor;
    private Texture2D arrowCursor;

    void Start()
    {
        SetupVideoBackground();
        SetupBGM();
        SetupGameCursor();
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(0.55f, 0.38f, 0.32f, 1f);
        Camera.main.transform.position = new Vector3(0f, 0f, -10f);

        var root = GetComponent<UIDocument>().rootVisualElement;

        var styleSheet = Resources.Load<StyleSheet>("UI/TitleScreenStyles");
        if (styleSheet != null) root.styleSheets.Add(styleSheet);

        LoadAvatarImages(root);

        usernameLabel = root.Q<Label>("username");
        btnNewGame = root.Q<Button>("btn-new-game");
        btnContinue = root.Q<Button>("btn-continue");
        btnArchive = root.Q<Button>("btn-archive");
        btnSettings = root.Q<Button>("btn-settings");
        btnExit = root.Q<Button>("btn-exit");
        announcementBtn = root.Q<Button>("announcement-btn");

        SetupGlassButton(btnNewGame);
        SetupGlassButton(btnContinue);
        SetupGlassButton(btnArchive);
        SetupGlassButton(btnSettings);
        SetupGlassButton(btnExit);

        if (btnNewGame != null) btnNewGame.clicked += OnNewGame;
        if (btnContinue != null) btnContinue.clicked += OnContinue;
        if (btnArchive != null) btnArchive.clicked += OnArchive;
        if (btnSettings != null) btnSettings.clicked += OnSettings;
        if (btnExit != null) btnExit.clicked += OnExit;
        if (announcementBtn != null)
        {
            announcementBtn.RegisterCallback<PointerEnterEvent>(evt =>
            {
                if (handCursor != null)
                    UnityEngine.Cursor.SetCursor(handCursor, new Vector2(0, 0), UnityEngine.CursorMode.ForceSoftware);
                announcementBtn.style.borderTopColor = borderHover;
                announcementBtn.style.borderRightColor = borderHover;
                announcementBtn.style.borderBottomColor = borderHover;
                announcementBtn.style.borderLeftColor = borderHover;
            });
            announcementBtn.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                if (arrowCursor != null)
                    UnityEngine.Cursor.SetCursor(arrowCursor, Vector2.zero, UnityEngine.CursorMode.ForceSoftware);
                announcementBtn.style.borderTopColor = new Color(1f, 220f/255f, 150f/255f, 0.35f);
                announcementBtn.style.borderRightColor = new Color(1f, 220f/255f, 150f/255f, 0.35f);
                announcementBtn.style.borderBottomColor = new Color(1f, 220f/255f, 150f/255f, 0.35f);
                announcementBtn.style.borderLeftColor = new Color(1f, 220f/255f, 150f/255f, 0.35f);
            });
            announcementBtn.clicked += OnAnnouncement;
        }

        LoadUserInfo();
        CheckSaveData();

        newGameSetup = gameObject.AddComponent<NewGameSetupUI>();
        newGameSetup.Init(GetComponent<UIDocument>(), () =>
        {
            PlayerPrefs.SetInt("VN_AutoLoad", 0);
            PlayerPrefs.Save();
            SceneManager.LoadScene("VN_Test");
        });

        archiveUI = gameObject.AddComponent<TitleArchiveUI>();
        archiveUI.Init(GetComponent<UIDocument>());

        bulletinUI = new GameObject("StationBulletinUI").AddComponent<StationBulletinUI>();
        bulletinUI.Init(GetComponent<UIDocument>());

        videoPlayer = FindAnyObjectByType<VideoPlayer>();
        AdaptQuadToScreen();
    }

    void AdaptQuadToScreen()
    {
        var videoBg = GameObject.Find("VideoBackground");
        if (videoBg == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        float visibleHeight = cam.orthographicSize * 2f;
        float visibleWidth = visibleHeight * (float)Screen.width / Screen.height;

        videoBg.transform.localScale = new Vector3(visibleWidth, visibleHeight, 1f);
        videoBg.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
    }

    void Update()
    {
        AdaptQuadToScreen();
    }

    void SetupGlassButton(Button btn)
    {
        if (btn == null) return;

        btn.RegisterCallback<PointerEnterEvent>(evt =>
        {
            if (btn.enabledSelf && handCursor != null)
                UnityEngine.Cursor.SetCursor(handCursor, new Vector2(0, 0), UnityEngine.CursorMode.ForceSoftware);
            btn.style.backgroundColor = glassBgHover;
            btn.style.borderTopColor = borderHover;
            btn.style.borderRightColor = borderHover;
            btn.style.borderBottomColor = borderHover;
            btn.style.borderLeftColor = borderHover;
            btn.style.color = textHover;
            btn.style.translate = new Translate(0, -2);
        });

        btn.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            if (arrowCursor != null)
                UnityEngine.Cursor.SetCursor(arrowCursor, Vector2.zero, UnityEngine.CursorMode.ForceSoftware);
            btn.style.backgroundColor = glassBg;
            btn.style.borderTopColor = borderNormal;
            btn.style.borderRightColor = borderNormal;
            btn.style.borderBottomColor = borderNormal;
            btn.style.borderLeftColor = borderNormal;
            btn.style.color = textNormal;
            btn.style.translate = new Translate(0, 0);
        });

        btn.RegisterCallback<MouseDownEvent>(evt =>
        {
            btn.style.backgroundColor = glassBgPress;
            btn.style.translate = new Translate(0, 0);
        });

        btn.RegisterCallback<MouseUpEvent>(evt =>
        {
            btn.style.backgroundColor = glassBgHover;
            btn.style.translate = new Translate(0, -2);
        });
    }

    void SetupBGM()
    {
        var existing = GameObject.Find("BGM");
        if (existing != null) return;

        var audioObj = new GameObject("BGM");
        audioObj.transform.SetParent(null);
        DontDestroyOnLoad(audioObj);
        var src = audioObj.AddComponent<AudioSource>();
        src.loop = true;
        src.volume = 0.3f;
        var clip = Resources.Load<AudioClip>("Audio/Train Through Keys");
        if (clip != null)
        {
            src.clip = clip;
            src.Play();
        }
    }

    void SetupGameCursor()
    {
        if (LoginManager.cursorTexture == null)
            LoginManager.cursorTexture = LoginManager.LoadCursorTexture("Cursors/cursor_arrow", 3);
        arrowCursor = LoginManager.cursorTexture;
        if (handCursor == null)
            handCursor = LoginManager.LoadCursorTexture("Cursors/cursor_hand", 3);
        UnityEngine.Cursor.SetCursor(arrowCursor, Vector2.zero, UnityEngine.CursorMode.ForceSoftware);
    }

    void LoadAvatarImages(VisualElement root)
    {
        var frame = root.Q<VisualElement>("avatar-frame");
        if (frame != null)
        {
            var tex = Resources.Load<Texture2D>("UI/AvatarFrame");
            if (tex != null) frame.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));
        }

        var icon = root.Q<VisualElement>("avatar-icon");
        if (icon != null)
        {
            var tex = Resources.Load<Texture2D>("UI/DefaultAvatar");
            if (tex != null) icon.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));
        }
    }

    void LoadUserInfo()
    {
        if (usernameLabel == null)
            usernameLabel = GetComponent<UIDocument>().rootVisualElement.Q<Label>("username");
        if (usernameLabel == null) return;

        string username = PlayerPrefs.GetString("Username", "");

        if (string.IsNullOrEmpty(username))
        {
            string authPath = Path.Combine(Application.persistentDataPath, "auth.json");
            if (File.Exists(authPath))
            {
                string json = File.ReadAllText(authPath);
                var auth = JsonUtility.FromJson<LoginManager.AuthData>(json);
                if (auth != null && !string.IsNullOrEmpty(auth.username))
                {
                    username = auth.username;
                    PlayerPrefs.SetString("Username", username);
                    PlayerPrefs.Save();
                }
            }
        }

        usernameLabel.text = string.IsNullOrEmpty(username) ? "未知用户" : username;
    }

    void CheckSaveData()
    {
        if (btnContinue == null) return;
        bool hasSaveData = false;
        for (int i = 0; i < 3; i++)
        {
            if (PlayerPrefs.HasKey("VN_Save_" + i)) { hasSaveData = true; break; }
        }
        if (!hasSaveData)
        {
            btnContinue.SetEnabled(false);
            btnContinue.style.backgroundColor = new Color(40f/255f, 25f/255f, 15f/255f, 0.15f);
            btnContinue.style.borderTopColor = new Color(1f, 220f/255f, 150f/255f, 0.15f);
            btnContinue.style.borderRightColor = new Color(1f, 220f/255f, 150f/255f, 0.15f);
            btnContinue.style.borderBottomColor = new Color(1f, 220f/255f, 150f/255f, 0.15f);
            btnContinue.style.borderLeftColor = new Color(1f, 220f/255f, 150f/255f, 0.15f);
            btnContinue.style.color = new Color(1f, 215f/255f, 0f, 0.3f);
        }
    }

    void OnNewGame()
    {
        newGameSetup?.Show();
    }

    void OnContinue()
    {
        PlayerPrefs.SetInt("VN_ShowLoadUI", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("VN_Test");
    }

    void OnArchive() { archiveUI?.Show(); }
    void OnSettings() { bulletinUI?.Show(); }
    void OnAnnouncement() { Debug.Log("打开快讯"); }
    void OnExit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void SetupVideoBackground()
    {
        var clip = Resources.Load<VideoClip>("Videos/cloud_sea_bg");
        if (clip == null) return;

        var go = new GameObject("VideoBackground");
        go.transform.SetParent(transform);

        var player = go.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.isLooping = true;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.clip = clip;

        var rt = new RenderTexture(1920, 1080, 0);
        rt.Create();
        player.targetTexture = rt;

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.SetParent(go.transform);
        quad.transform.localPosition = new Vector3(0, 0, 10);
        quad.transform.localScale = new Vector3(37.5f, 21f, 1);
        quad.name = "VideoQuad";

        Shader shader = Shader.Find("Unlit/Texture");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit");
        var mat = new Material(shader);
        mat.mainTexture = rt;
        quad.GetComponent<Renderer>().material = mat;

        player.Play();
    }
}
