using UnityEngine;
using System.Collections.Generic;

public static class KeyBindings
{
    public enum Action
    {
        Advance,
        SkipBack,
        SkipForward,
        ToggleAuto,
        OpenMenu
    }

    private static readonly Dictionary<Action, KeyCode> defaults = new Dictionary<Action, KeyCode>
    {
        { Action.Advance, KeyCode.Space },
        { Action.SkipBack, KeyCode.LeftArrow },
        { Action.SkipForward, KeyCode.RightArrow },
        { Action.ToggleAuto, KeyCode.F5 },
        { Action.OpenMenu, KeyCode.Escape }
    };

    public static KeyCode GetKey(Action action)
    {
        string key = "KB_" + action.ToString();
        return (KeyCode)PlayerPrefs.GetInt(key, (int)defaults[action]);
    }

    public static void SetKey(Action action, KeyCode key)
    {
        PlayerPrefs.SetInt("KB_" + action.ToString(), (int)key);
        PlayerPrefs.Save();
    }

    public static string GetKeyName(Action action)
    {
        KeyCode k = GetKey(action);
        string n = k.ToString();
        // Clean up common names
        if (n.StartsWith("Alpha")) return n.Replace("Alpha", "");
        if (n == "LeftShift") return "Shift";
        if (n == "RightShift") return "Shift";
        if (n == "LeftControl") return "Ctrl";
        if (n == "RightControl") return "Ctrl";
        if (n == "LeftAlt") return "Alt";
        if (n == "RightAlt") return "Alt";
        if (n == "Mouse0") return "左键";
        if (n == "Escape") return "ESC";
        return n;
    }

    public static string GetActionName(Action action)
    {
        switch (action)
        {
            case Action.Advance: return "推进对话";
            case Action.SkipBack: return "后退";
            case Action.SkipForward: return "快进";
            case Action.ToggleAuto: return "自动模式";
            case Action.OpenMenu: return "菜单";
            default: return action.ToString();
        }
    }

    public static bool IsDown(Action action)
    {
        KeyCode k = GetKey(action);
        if (k == KeyCode.Mouse0) return Input.GetMouseButtonDown(0);
        return Input.GetKeyDown(k);
    }

    public static bool IsHeld(Action action)
    {
        KeyCode k = GetKey(action);
        if (k == KeyCode.Mouse0) return Input.GetMouseButton(0);
        return Input.GetKey(k);
    }
}