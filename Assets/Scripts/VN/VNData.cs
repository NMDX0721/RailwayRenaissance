using UnityEngine;

[System.Serializable]
public class ScriptData
{
    public string id;
    public string nextScript; // 自动加载的下一个剧本（序章链用）
    public SceneData[] scenes;
}

public enum TransitionType
{
    None,
    Fade,
    SlideLeft,
    SlideRight
}

[System.Serializable]
public class SceneData
{
    public string bg;
    public string bgm;
    public string transition; // "fade", "slideLeft", "slideRight", ""
    public CharacterEntry[] chars; // 场景级默认立绘（可选）
    public string e;              // 场景级默认表情（可选）
    public DialogueEntry[] d;
}

[System.Serializable]
public class DialogueEntry
{
    public string t;
    public string s;
    public string text;
    public string e;
    public int next;
    public string condition; // if set, only show when condition is met
    public string setValue;  // "varName=value" to set when this entry is processed
    public OptionData[] opts;
    public CharacterEntry[] chars;
}

[System.Serializable]
public class OptionData
{
    public string text;
    public int next;
    public string condition; // if set, only show when condition is met
    public string setValue;  // "varName=value" to set when selected
}

[System.Serializable]
public class CharacterEntry
{
    public string name;
    public string pos; // "left", "center", "right"
}