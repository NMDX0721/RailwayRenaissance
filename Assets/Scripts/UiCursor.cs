using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 全局手型光标：可交互元素悬停显示手型，离开恢复箭头。
/// 统一 3 倍点阵缩放 + ForceSoftware，保证手型与箭头同尺寸、跨面板一致。
/// </summary>
public static class UiCursor
{
    private static Texture2D arrowTex;
    private static Texture2D handTex;

    public static void EnsureLoaded()
    {
        if (arrowTex == null)
            arrowTex = LoginManager.LoadCursorTexture("Cursors/cursor_arrow", 3);
        if (handTex == null)
            handTex = LoginManager.LoadCursorTexture("Cursors/cursor_hand", 3);
    }

    public static void ShowHand()
    {
        EnsureLoaded();
        if (handTex != null)
            UnityEngine.Cursor.SetCursor(handTex, Vector2.zero, CursorMode.ForceSoftware);
    }

    public static void ShowArrow()
    {
        EnsureLoaded();
        if (arrowTex != null)
            UnityEngine.Cursor.SetCursor(arrowTex, Vector2.zero, CursorMode.ForceSoftware);
    }

    /// <summary>给可交互元素挂手型 hover；Leave 恢复箭头。</summary>
    public static void Bind(VisualElement element)
    {
        if (element == null) return;
        EnsureLoaded();
        element.RegisterCallback<PointerEnterEvent>(e => ShowHand());
        element.RegisterCallback<PointerLeaveEvent>(e => ShowArrow());
    }
}
