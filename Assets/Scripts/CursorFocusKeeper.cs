using UnityEngine;

/// <summary>
/// 游戏窗口失焦后恢复光标 → 自动重新应用自定义箭头光标（无需点击）。
/// 常驻场景，DontDestroyOnLoad。
/// </summary>
public class CursorFocusKeeper : MonoBehaviour
{
    private static CursorFocusKeeper _instance;
    public static CursorFocusKeeper Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("CursorFocusKeeper");
                _instance = go.AddComponent<CursorFocusKeeper>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 启动时确保光标正确
        ApplyArrowCursor();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            ApplyArrowCursor();
    }

    public static void ApplyArrowCursor()
    {
        var tex = LoginManager.GetArrowTexture();
        if (tex != null)
            Cursor.SetCursor(tex, Vector2.zero, CursorMode.ForceSoftware);
    }
}