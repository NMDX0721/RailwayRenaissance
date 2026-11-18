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
    public string phone;          // 非空=通话中，显示通话条；值可为对方名字（如 "老陈"）
    public DialogueEntry[] d;
}

[System.Serializable]
public class DialogueEntry
{
    public string id; // Optional stable localization ID, scoped to the script. Legacy coordinates remain unchanged.
    public string t;
    public string s;
    public string text;
    public string textJa; // Reviewed Japanese subtitle; excludes TTS tags and pronunciation substitutions.
    public string e;
    public int next;
    public string condition; // if set, only show when condition is met
    public string setValue;  // "varName=value" to set when this entry is processed
    public string bgm;       // 条目级换曲：进入该条目时立即切到此曲（不受场景级最短驻留限制）
    public string sig;       // 作者标注："stop" = 危机/转折点，供左上角信号灯提前预告（见 VNSignalForecast）
    public OptionData[] opts;
    public CharacterEntry[] chars;
    public string size;  // CG专用: "full"(全屏) / "small"(居中小插画), 默认full
    public string[] reactions; // 旁观者表情: ["lin_biaohan:serious", "suiyue:curious"] 格式
}

[System.Serializable]
public class OptionData
{
    public string text;
    public int next;
    public int nextDialogue; // Optional entry index within next scene; omitted means 0 (legacy behaviour).
    public string condition; // if set, only show when condition is met
    public string setValue;  // "varName=value", comma-separated assignments supported.
}

[System.Serializable]
public class CharacterEntry
{
    public string name;
    public string pos; // "left", "center", "right"
}
