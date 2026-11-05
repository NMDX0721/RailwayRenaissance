using UnityEngine;
using UnityEngine.SceneManagement;

public static class StoryReplayService
{
    public static void OpenLibrary() => GameMainUI.OpenTabletApp("归途");

    public static void StartEpisode(string scriptName)
    {
        if (Resources.Load<TextAsset>("Scripts/" + scriptName) == null) return;
        if (scriptName != "prologue_01_news" && VNManager.NeedsQuizForSkipStatic()) scriptName = "prologue_01_news";
        PlayerPrefs.DeleteKey("VN_ReplayScene");
        PlayerPrefs.DeleteKey("VN_ReplayDialogue");
        PlayerPrefs.DeleteKey("VN_ReplayVariables");
        PlayerPrefs.DeleteKey("VN_AutoLoad");
        PlayerPrefs.DeleteKey("VN_ShowLoadUI");
        PlayerPrefs.DeleteKey("VN_LoadSaveSlot");
        PlayerPrefs.SetString("VN_ReplayScript", scriptName);
        PlayerPrefs.SetInt("VN_FromGameMain", 1);
        PlayerPrefs.Save();
        GameMainUI.Instance?.Hide();
        SceneManager.LoadScene("VN_Test");
    }
}
