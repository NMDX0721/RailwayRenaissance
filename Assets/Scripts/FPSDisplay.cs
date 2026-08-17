using UnityEngine;
using UnityEngine.UIElements;

public class FPSDisplay : MonoBehaviour
{
    private Label fpsLabel;
    private float deltaTime;
    private bool isActive;

    private void Start()
    {
        isActive = PlayerPrefs.GetInt("ShowFPS", 0) == 1;
        if (!isActive)
        {
            enabled = false;
            return;
        }
        CreateLabel();
    }

    private void CreateLabel()
    {
        var uiDoc = FindAnyObjectByType<UIDocument>();
        if (uiDoc == null) return;

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

    private void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        if (fpsLabel != null)
        {
            fpsLabel.text = "FPS: " + Mathf.FloorToInt(fps);
            // 颜色随帧率变化：60+ 绿色，30-60 黄色，<30 红色
            if (fps >= 60) fpsLabel.style.color = new Color(0.3f, 1f, 0.3f, 0.7f);
            else if (fps >= 30) fpsLabel.style.color = new Color(1f, 1f, 0.3f, 0.7f);
            else fpsLabel.style.color = new Color(1f, 0.3f, 0.3f, 0.7f);
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
}