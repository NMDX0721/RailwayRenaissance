using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 输入适配层：Active Input Handling = Both（或旧版）时走 UnityEngine.Input，
/// 切到 Input System Package Only 后自动切到新 API，调用方无需改动。
/// </summary>
public static class LegacyInputAdapter
{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    // ---- Input System Package Only ----

    public static bool GetKeyDown(KeyCode key)
    {
        int mb = MouseButtonIndex(key);
        if (mb >= 0) return MouseButton(mb, down: true);
        var kb = Keyboard.current;
        if (kb == null) return false;
        Key k = ToKey(key);
        if (k == Key.None) return false;
        return kb[k].wasPressedThisFrame;
    }

    public static bool GetKey(KeyCode key)
    {
        int mb = MouseButtonIndex(key);
        if (mb >= 0) return MouseButton(mb, down: false);
        var kb = Keyboard.current;
        if (kb == null) return false;
        Key k = ToKey(key);
        if (k == Key.None) return false;
        return kb[k].isPressed;
    }

    public static bool GetMouseButtonDown(int button) => MouseButton(button, down: true);
    public static bool GetMouseButton(int button) => MouseButton(button, down: false);

    public static Vector3 MousePosition
    {
        get
        {
            var mouse = Mouse.current;
            return mouse != null ? (Vector3)mouse.position.ReadValue() : Vector3.zero;
        }
    }

    private static int MouseButtonIndex(KeyCode code)
    {
        int v = (int)code - (int)KeyCode.Mouse0;
        return v >= 0 && v <= 6 ? v : -1;
    }

    private static bool MouseButton(int button, bool down)
    {
        var mouse = Mouse.current;
        if (mouse == null) return false;
        switch (button)
        {
            case 0: return down ? mouse.leftButton.wasPressedThisFrame : mouse.leftButton.isPressed;
            case 1: return down ? mouse.rightButton.wasPressedThisFrame : mouse.rightButton.isPressed;
            case 2: return down ? mouse.middleButton.wasPressedThisFrame : mouse.middleButton.isPressed;
            case 3: return down ? mouse.backButton.wasPressedThisFrame : mouse.backButton.isPressed;
            case 4: return down ? mouse.forwardButton.wasPressedThisFrame : mouse.forwardButton.isPressed;
            default: return false;
        }
    }

    private static Key ToKey(KeyCode code)
    {
        string n = code.ToString();
        // Alpha0-9：Key 枚举顺序是 Digit1..Digit9,Digit0（Digit0 在末尾，不能用偏移加法）
        if (n.Length == 6 && n.StartsWith("Alpha") && n[5] >= '0' && n[5] <= '9')
            return n[5] == '0' ? Key.Digit0 : Key.Digit1 + (n[5] - '1');
        // Keypad* → Numpad*
        if (n.StartsWith("Keypad"))
        {
            string rest = n.Substring(6);
            if (rest.Length == 1 && rest[0] >= '0' && rest[0] <= '9')
                return Key.Numpad0 + (rest[0] - '0');
            return System.Enum.TryParse("Numpad" + rest, out Key np) ? np : Key.None;
        }
        // KeyCode 与 Key 枚举的命名差异
        switch (n)
        {
            case "Return": return Key.Enter;
            case "BackQuote": return Key.Backquote;
            case "Print": return Key.PrintScreen;
            case "Menu": return Key.ContextMenu;
            case "Alt": return Key.LeftAlt;
            case "Control": return Key.LeftCtrl;
            case "Shift": return Key.LeftShift;
            case "Numlock": return Key.NumLock;
            case "LeftWindows": return Key.LeftMeta;
            case "RightWindows": return Key.RightMeta;
        }
        if (System.Enum.TryParse(n, out Key k)) return k;
        return Key.None;
    }
#else
    // ---- UnityEngine.Input（旧版 / Both 过渡期）----

    public static bool GetKeyDown(KeyCode key) => Input.GetKeyDown(key);
    public static bool GetKey(KeyCode key) => Input.GetKey(key);
    public static bool GetMouseButtonDown(int button) => Input.GetMouseButtonDown(button);
    public static bool GetMouseButton(int button) => Input.GetMouseButton(button);
    public static Vector3 MousePosition => Input.mousePosition;
#endif
}
