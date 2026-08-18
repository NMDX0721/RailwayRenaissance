using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class FPSDisplay : MonoBehaviour
{
    private Label fpsLabel;
    private int frameCount;
    private float elapsedAccum;
    private bool isActive;
    private Color lastColor;
    private int lastFps = -1;
    private const float UpdateInterval = 0.5f; // 每 0.5 秒更新一次，读数更稳

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        isActive = PlayerPrefs.GetInt("ShowFPS", 0) == 1;
        if (!isActive)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        CreateLabel();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 场景切换后旧 UIDocument 销毁，需把标签挂到新场景的 UIDocument 上
        if (isActive)
            CreateLabel();
    }

    private void OnDestroy()
    {
        if (fpsLabel != null && fpsLabel.panel != null && fpsLabel.parent != null)
            fpsLabel.parent.Remove(fpsLabel);
    }

    private void CreateLabel()
    {
        var uiDoc = FindAnyObjectByType<UIDocument>();
        if (uiDoc == null) return;
        // 已挂载到当前文档则跳过
        if (fpsLabel != null && fpsLabel.panel != null && fpsLabel.panel == uiDoc.rootVisualElement.panel)
            return;

        // 清除旧的（可能已失效的）标签
        if (fpsLabel != null && fpsLabel.parent != null)
            fpsLabel.parent.Remove(fpsLabel);

        fpsLabel = new Label("FPS: 0");
        fpsLabel.style.position = Position.Absolute;
        fpsLabel.style.bottom = 10;
        fpsLabel.style.right = 10;
        fpsLabel.style.fontSize = 14;
        fpsLabel.style.color = new Color(0.3f, 1f, 0.3f, 0.7f);
        fpsLabel.style.backgroundColor = new Color(0, 0, 0, 0.3f);
        fpsLabel.style.paddingLeft = 6;
        fpsLabel.style.paddingRight = 6;
        fpsLabel.style.paddingTop = 2;
        fpsLabel.style.paddingBottom = 2;
        fpsLabel.style.borderTopLeftRadius = 4;
        fpsLabel.style.borderTopRightRadius = 4;
        fpsLabel.style.borderBottomLeftRadius = 4;
        fpsLabel.style.borderBottomRightRadius = 4;
        fpsLabel.style.unityFontDefinition = new FontDefinition { font = Resources.Load<Font>("Fonts/zpix") };
        fpsLabel.style.display = DisplayStyle.Flex;
        uiDoc.rootVisualElement.Add(fpsLabel);
    }

    private void LateUpdate()
    {
        if (fpsLabel == null) return;
        // 标签失效时自动重新挂载
        if (fpsLabel.panel == null)
        {
            CreateLabel();
            return;
        }

        frameCount++;
        elapsedAccum += Time.unscaledDeltaTime;
        if (elapsedAccum < UpdateInterval) return; // 窗口未满，等待

        int fps = Mathf.Clamp(Mathf.RoundToInt(frameCount / elapsedAccum), 0, 999);
        frameCount = 0;
        elapsedAccum = 0f;
        // 只在数值变化时更新文本，颜色变化时更新样式，避免每帧触发样式重算
        if (fps != lastFps)
        {
            lastFps = fps;
            fpsLabel.text = "FPS: " + fps;
        }
        Color c;
        if (fps >= 60) c = new Color(0.3f, 1f, 0.3f, 0.7f);
        else if (fps >= 30) c = new Color(1f, 1f, 0.3f, 0.7f);
        else c = new Color(1f, 0.3f, 0.3f, 0.7f);
        if (c != lastColor)
        {
            lastColor = c;
            fpsLabel.style.color = c;
        }
    }

    public static void SetActive(bool show)
    {
        PlayerPrefs.SetInt("ShowFPS", show ? 1 : 0);
        PlayerPrefs.Save();
        var instance = FindAnyObjectByType<FPSDisplay>();
        if (instance != null)
        {
            Destroy(instance.gameObject);
        }
        if (show)
        {
            var go = new GameObject("FPSDisplay");
            go.AddComponent<FPSDisplay>();
            DontDestroyOnLoad(go);
        }
    }

    public static void Init()
    {
        if (PlayerPrefs.GetInt("ShowFPS", 0) == 1 && FindAnyObjectByType<FPSDisplay>() == null)
        {
            var go = new GameObject("FPSDisplay");
            go.AddComponent<FPSDisplay>();
            DontDestroyOnLoad(go);
        }
    }
}