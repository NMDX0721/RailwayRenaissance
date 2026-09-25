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
        var kb = Keyboard.current;
        if (kb == null) return false;
        Key k = ToKey(key);
        if (k == Key.None) return false;
        return kb[k].wasPressedThisFrame;
    }

    public static bool GetKey(KeyCode key)
    {
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

    private static bool MouseButton(int button, bool down)
    {
        var mouse = Mouse.current;
        if (mouse == null) return false;
        switch (button)
        {
            case 0: return down ? mouse.leftButton.wasPressedThisFrame : mouse.leftButton.isPressed;
            case 1: return down ? mouse.rightButton.wasPressedThisFrame : mouse.rightButton.isPressed;
            case 2: return down ? mouse.middleButton.wasPressedThisFrame : mouse.middleButton.isPressed;
            default: return false;
        }
    }

    private static Key ToKey(KeyCode code)
    {
        string n = code.ToString();
        // Alpha0-9 → Digit0-9；Keypad0-9 → Numpad0-9
        if (n.Length == 6 && n.StartsWith("Alpha") && n[5] >= '0' && n[5] <= '9')
            return Key.Digit0 + (n[5] - '0');
        if (n.Length == 7 && n.StartsWith("Keypad") && n[6] >= '0' && n[6] <= '9')
            return Key.Numpad0 + (n[6] - '0');
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
