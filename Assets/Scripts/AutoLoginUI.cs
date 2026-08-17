using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
using System;

public class AutoLoginUI : MonoBehaviour
{
    [Header("面板")]
    public GameObject autoLoginPanel;

    [Header("文字")]
    public Text mainText;
    public Text subText;

    [Header("进度条")]
    public Image progressFill;
    public Text progressText;
    public GameObject progressBarGroup;
    private RectTransform progressFillRect;
    private float progressBgWidth = 500f;

    [Header("按钮")]
    public Button btnReturnLogin;
    public Button btnResetAccount;

    public enum AutoLoginState
    {
        AutoLogin,
        AfterRegister,
        CredentialLost
    }

    private AutoLoginState currentState;
    private string authFilePath;

    void Start()
    {
        authFilePath = Path.Combine(Application.persistentDataPath, "auth.json");

        if (progressFill != null)
        {
            progressFillRect = progressFill.GetComponent<RectTransform>();
            progressBgWidth = progressBarGroup.GetComponent<RectTransform>().sizeDelta.x;
        }

        btnReturnLogin.onClick.AddListener(OnReturnLogin);
        btnResetAccount.onClick.AddListener(OnResetAccount);

        CheckLocalCredential();
    }

    public void CheckLocalCredential()
    {
        if (File.Exists(authFilePath))
        {
            string json = File.ReadAllText(authFilePath);
            var auth = JsonUtility.FromJson<LoginManager.AuthData>(json);
            if (auth != null && !string.IsNullOrEmpty(auth.username))
            {
                SetState(AutoLoginState.AutoLogin);
                StartCoroutine(AutoLoginProcess(auth.username));
                return;
            }
        }

        SetState(AutoLoginState.CredentialLost);
    }

    void SetState(AutoLoginState state)
    {
        currentState = state;

        progressBarGroup.SetActive(true);
        btnReturnLogin.gameObject.SetActive(true);
        btnResetAccount.gameObject.SetActive(false);
        if (progressFillRect != null) progressFillRect.sizeDelta = new Vector2(0, progressFillRect.sizeDelta.y);
        progressText.text = "0%";

        switch (state)
        {
            case AutoLoginState.AutoLogin:
                mainText.text = "正在自动登录...";
                subText.text = "正在校验本地凭证...";
                break;

            case AutoLoginState.AfterRegister:
                mainText.text = "注册成功";
                subText.text = "正在完成登录...";
                break;

            case AutoLoginState.CredentialLost:
                mainText.text = "本地凭证已丢失";
                subText.text = "请重新注册账号";
                progressBarGroup.SetActive(false);
                btnReturnLogin.gameObject.SetActive(false);
                btnResetAccount.gameObject.SetActive(true);
                break;
        }
    }

    IEnumerator AutoLoginProcess(string username)
    {
        // 阶段1: 校验凭证 (0-15%)
        subText.text = "正在校验本地凭证...";
        yield return StartCoroutine(LoadResource("校验凭证", 0f, 0.15f, 0.6f, () => {
            if (!File.Exists(authFilePath)) return false;
            string json = File.ReadAllText(authFilePath);
            var auth = JsonUtility.FromJson<LoginManager.AuthData>(json);
            return auth != null && !string.IsNullOrEmpty(auth.username);
        }));

        if (currentState == AutoLoginState.CredentialLost) yield break;

        // 阶段2: 加载标题界面Logo (15-30%)
        subText.text = "正在加载标题界面Logo...";
        yield return StartCoroutine(LoadResource("Logo", 0.15f, 0.30f, 0.5f, () => {
            Resources.Load<Sprite>("UI/Login/title_logo");
            return true;
        }));

        // 阶段3: 加载头像框架 (30-40%)
        subText.text = "正在加载头像框架资源...";
        yield return StartCoroutine(LoadResource("头像框架", 0.30f, 0.40f, 0.4f, () => {
            Resources.Load<Sprite>("UI/AvatarFrame");
            Resources.Load<Sprite>("UI/DefaultAvatar");
            return true;
        }));

        // 阶段4: 加载UI布局配置 (40-55%)
        subText.text = "正在加载界面布局配置...";
        yield return StartCoroutine(LoadResource("UI布局", 0.40f, 0.55f, 0.6f, () => {
            Resources.Load<UnityEngine.UIElements.PanelSettings>("UI/TitleScreenPanelSettings");
            Resources.Load<TextAsset>("UI/TitleScreen");
            Resources.Load<TextAsset>("UI/GlobalStyle");
            return true;
        }));

        // 阶段5: 加载像素字体 (55-65%)
        subText.text = "正在加载像素字体...";
        yield return StartCoroutine(LoadResource("字体", 0.55f, 0.65f, 0.4f, () => {
            Resources.Load<Font>("Fonts/zpix");
            return true;
        }));

        // 阶段6: 加载视频背景 (65-80%)
        subText.text = "正在初始化视频播放器...";
        yield return StartCoroutine(LoadResource("视频", 0.65f, 0.80f, 0.5f, () => {
            Resources.Load<Material>("Materials/VideoBackground");
            var clip = Resources.Load<VideoClip>("Videos/cloud_sea_bg");
            return clip != null;
        }));

        // 阶段7: 加载背景音乐 (80-90%)
        subText.text = "正在加载背景音乐...";
        yield return StartCoroutine(LoadResource("音乐", 0.80f, 0.90f, 0.4f, () => {
            Resources.Load<AudioClip>("Audio/Train Through Keys");
            return true;
        }));

        // 阶段8: 加载游戏配置 (90-95%)
        subText.text = "正在加载游戏配置...";
        yield return StartCoroutine(LoadResource("配置", 0.90f, 0.95f, 0.3f, () => {
            GameConfig.Load();
            return true;
        }));

        // 阶段9: 完成初始化 (95-100%)
        subText.text = "正在完成系统初始化...";
        yield return StartCoroutine(LoadResource("初始化", 0.95f, 1f, 0.3f, () => {
            // 预加载VN场景组件
            Resources.Load<Font>("Fonts/zpix");
            Resources.Load<UnityEngine.UIElements.PanelSettings>("UI/TitleScreenPanelSettings");
            return true;
        }));

        mainText.text = "登录成功";
        subText.text = "欢迎回来，" + (username ?? "玩家");

        yield return new WaitForSeconds(0.3f);
        if (Camera.main != null) Camera.main.backgroundColor = Color.black;
        SceneManager.LoadScene("TitleScreen");
    }

    IEnumerator LoadResource(string name, float from, float to, float duration, System.Func<bool> loader)
    {
        float elapsed = 0f;
        bool done = false;
        bool result = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float progress = Mathf.Lerp(from, to, t * t * (3f - 2f * t));

            // 在进度条动画期间执行加载
            if (!done && t > 0.3f)
            {
                done = true;
                result = loader();
            }

            if (progressFillRect != null)
                progressFillRect.sizeDelta = new Vector2(progressBgWidth * progress, progressFillRect.sizeDelta.y);
            progressText.text = Mathf.FloorToInt(progress * 100f) + "%";
            yield return null;
        }

        if (!done) { done = true; result = loader(); }

        if (progressFillRect != null)
            progressFillRect.sizeDelta = new Vector2(progressBgWidth * to, progressFillRect.sizeDelta.y);
        progressText.text = Mathf.FloorToInt(to * 100f) + "%";
    }

    void OnReturnLogin()
    {
        SceneManager.LoadScene("Login");
    }

    void OnResetAccount()
    {
        if (File.Exists(authFilePath))
        {
            File.Delete(authFilePath);
        }

        LoginManager.showAutoLoginOnStart = false;
        LoginManager.showRegisterOnStart = true;
        SceneManager.LoadScene("Login");
    }

    public void StartAfterRegister()
    {
        SetState(AutoLoginState.AfterRegister);
        StartCoroutine(AutoLoginProcess(null));
    }
}