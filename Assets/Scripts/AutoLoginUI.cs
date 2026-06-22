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
        progressFill.fillAmount = 0;
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
        yield return StartCoroutine(UpdateProgress(0f, 0.2f, 1.0f));
        
        if (!File.Exists(authFilePath))
        {
            SetState(AutoLoginState.CredentialLost);
            yield break;
        }
        
        subText.text = "正在验证账号信息...";
        string json = File.ReadAllText(authFilePath);
        var auth = JsonUtility.FromJson<LoginManager.AuthData>(json);
        if (auth == null || string.IsNullOrEmpty(auth.username))
        {
            SetState(AutoLoginState.CredentialLost);
            yield break;
        }
        yield return StartCoroutine(UpdateProgress(0.2f, 0.5f, 1.0f));
        
        subText.text = "正在加载用户数据...";
        yield return StartCoroutine(UpdateProgress(0.5f, 0.7f, 0.8f));
        
        subText.text = "正在预加载游戏资源...";
        Resources.Load<Texture2D>("Textures/sunset_railway");
        Resources.Load<Texture2D>("Textures/station_bg");
        Resources.Load<AudioClip>("Audio/Train Through Keys");
        Resources.Load<Sprite>("UI/Login/panel_bg");
        Resources.Load<Sprite>("UI/Login/button_primary");
        yield return StartCoroutine(UpdateProgress(0.7f, 0.9f, 1.2f));
        
        subText.text = "正在初始化系统...";
        yield return StartCoroutine(UpdateProgress(0.9f, 1f, 0.6f));
        
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
            float progress = Mathf.Lerp(from, to, elapsed / duration);
            progressFill.fillAmount = progress;
            progressText.text = Mathf.RoundToInt(progress / 0.05f) * 5 + "%";
            yield return null;
        }
        progressFill.fillAmount = to;
        progressText.text = Mathf.RoundToInt(to * 100) + "%";
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