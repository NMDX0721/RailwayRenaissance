using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;

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
                StartCoroutine(AutoLoginProcess());
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
                mainText.text = "自动登录中...";
                subText.text = "正在检测本地凭证...";
                break;
                
            case AutoLoginState.AfterRegister:
                mainText.text = "注册成功";
                subText.text = "正在登录...";
                break;
                
            case AutoLoginState.CredentialLost:
                mainText.text = "本地账号凭证丢失";
                subText.text = "请重置账号";
                progressBarGroup.SetActive(false);
                btnReturnLogin.gameObject.SetActive(false);
                btnResetAccount.gameObject.SetActive(true);
                break;
        }
    }
    
    IEnumerator AutoLoginProcess()
    {
        subText.text = "正在检测本地凭证...";
        yield return StartCoroutine(UpdateProgress(0f, 0.08f, 0.6f));
        yield return StartCoroutine(PauseProgress(0.08f, 0.3f));
        yield return StartCoroutine(UpdateProgress(0.08f, 0.12f, 0.2f));
        
        if (!File.Exists(authFilePath))
        {
            SetState(AutoLoginState.CredentialLost);
            yield break;
        }
        
        subText.text = "正在验证账号信息...";
        yield return StartCoroutine(UpdateProgress(0.12f, 0.25f, 0.8f));
        yield return StartCoroutine(PauseProgress(0.25f, 0.2f));
        string json = File.ReadAllText(authFilePath);
        var auth = JsonUtility.FromJson<LoginManager.AuthData>(json);
        if (auth == null || string.IsNullOrEmpty(auth.username))
        {
            SetState(AutoLoginState.CredentialLost);
            yield break;
        }
        yield return StartCoroutine(UpdateProgress(0.25f, 0.35f, 0.5f));
        
        subText.text = "正在加载用户数据...";
        yield return StartCoroutine(UpdateProgress(0.35f, 0.42f, 0.4f));
        yield return StartCoroutine(PauseProgress(0.42f, 0.15f));
        yield return StartCoroutine(UpdateProgress(0.42f, 0.48f, 0.3f));
        
        subText.text = "正在预加载游戏资源...";
        yield return StartCoroutine(UpdateProgress(0.48f, 0.65f, 1.0f));
        yield return StartCoroutine(PauseProgress(0.65f, 0.25f));
        Resources.Load<Texture2D>("Textures/sunset_railway");
        Resources.Load<Texture2D>("Textures/station_bg");
        yield return StartCoroutine(UpdateProgress(0.65f, 0.78f, 0.6f));
        yield return StartCoroutine(PauseProgress(0.78f, 0.2f));
        Resources.Load<AudioClip>("Audio/Train Through Keys");
        Resources.Load<Sprite>("UI/Login/panel_bg");
        Resources.Load<Sprite>("UI/Login/button_primary");
        yield return StartCoroutine(UpdateProgress(0.78f, 0.88f, 0.5f));
        
        subText.text = "正在初始化系统...";
        yield return StartCoroutine(UpdateProgress(0.88f, 0.92f, 0.3f));
        yield return StartCoroutine(PauseProgress(0.92f, 0.3f));
        yield return StartCoroutine(UpdateProgress(0.92f, 0.95f, 0.2f));
        yield return StartCoroutine(PauseProgress(0.95f, 0.4f));
        yield return StartCoroutine(UpdateProgress(0.95f, 1f, 0.3f));
        
        mainText.text = "登录成功";
        subText.text = "欢迎回来，" + auth.username;
        
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene("TitleScreen");
    }
    
    IEnumerator UpdateProgress(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float progress = Mathf.Lerp(from, to, t * t * (3f - 2f * t));
            if (progressFillRect != null)
            {
                progressFillRect.sizeDelta = new Vector2(progressBgWidth * progress, progressFillRect.sizeDelta.y);
            }
            progressText.text = Mathf.FloorToInt(progress * 100f) + "%";
            yield return null;
        }
        if (progressFillRect != null)
        {
            progressFillRect.sizeDelta = new Vector2(progressBgWidth * to, progressFillRect.sizeDelta.y);
        }
        progressText.text = Mathf.FloorToInt(to * 100f) + "%";
    }

    IEnumerator PauseProgress(float percent, float holdTime)
    {
        progressText.text = Mathf.FloorToInt(percent * 100f) + "%";
        yield return new WaitForSeconds(holdTime);
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
        StartCoroutine(AutoLoginProcess());
    }
}