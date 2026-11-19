using System;

// Derive props from the playback cursor, never from persistent account activation.
public static class VNPortraitStoryStage
{
    public static bool SuiyueHasTablet(ScriptData script, int sceneIndex, int dialogueIndex)
    {
        if (script == null || string.IsNullOrEmpty(script.id)) return true;
        if (script.id == "prologue_01_news") return false;
        if (script.id != "prologue_02_day0") return true;
        if (script.scenes == null || sceneIndex < 0 || dialogueIndex < 0) return false;

        for (int si = 0; si < script.scenes.Length && si <= sceneIndex; si++)
        {
            var entries = script.scenes[si]?.d;
            if (entries == null) continue;
            int count = si == sceneIndex ? Math.Min(dialogueIndex, entries.Length) : entries.Length;
            for (int di = 0; di < count; di++)
            {
                var entry = entries[di];
                if (entry != null && entry.t == "special" && entry.text == "TABLET_SETUP" &&
                    string.IsNullOrEmpty(entry.condition)) return true;
            }
        }
        return false;
    }
}
