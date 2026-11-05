using UnityEngine;
using System.Collections.Generic;

public static class StoryArchiveProgress
{
    private const string CgKeyPrefix = "ArchiveCG_";
    public static void CompleteEpisode(string scriptName)
    {
        if (string.IsNullOrEmpty(scriptName)) return;
        PlayerPrefs.SetInt("ArchiveScene_" + scriptName, 1);
        PlayerPrefs.Save();
    }
    public static void UnlockCG(string id)
    {
        PlayerPrefs.SetInt(CgKeyPrefix + id, 1);
        PlayerPrefs.Save();
    }

    /// <summary>解锁图鉴条目（角色/列车，幂等）。</summary>
    public static void UnlockArchive(string prefsKey)
    {
        PlayerPrefs.SetInt(prefsKey, 1);
        PlayerPrefs.Save();
    }

    /// <summary>按序章脚本名自动解锁对应 CG / 角色 / 列车。由 VNManager 在进入脚本时调用。</summary>
    public static void AutoUnlock(string scriptName)
    {
        switch (scriptName)
        {
            case "prologue_01_news":
                UnlockArchive("ArchiveChar_daoshi");
                UnlockArchive("ArchiveChar_lin");
                UnlockArchive("ArchiveChar_laochen"); // 老陈来电
                UnlockMusic("iron_and_ash");
                UnlockStory("prologue_01_news");
                break;
            case "prologue_02_day0":
                UnlockArchive("ArchiveChar_suiyue"); // 领取 0721
                UnlockArchive("ArchiveChar_jiaying"); // 茶馆会面嘉颖徐
                UnlockArchive("ArchiveTrain_nf5");
                UnlockMusic("cloud_rail");
                UnlockMusic("wheels_joke");
                UnlockMusic("south_wind");
                UnlockScene("hangar");
                UnlockScene("lab");
                UnlockScene("professor_office");
                UnlockScene("tea_house");
                UnlockScene("car_interior");
                UnlockScene("cabin_interior");
                UnlockStory("prologue_02_day0");
                break;
            case "prologue_03_journey":
                UnlockMusic("borderline");
                UnlockMusic("starlit_rails");
                UnlockMusic("chollima_ride");
                UnlockScene("car_interior_night");
                UnlockScene("cabin_interior_night");
                UnlockStory("prologue_03_journey");
                break;
            case "prologue_04_arrival":
                UnlockMusic("embers");
                UnlockMusic("platform");
                UnlockScene("station");
                UnlockStory("prologue_04_arrival");
                break;
            case "prologue_05_inspection":
                UnlockStory("prologue_05_inspection");
                break;
            case "prologue_06_team":
                UnlockArchive("ArchiveChar_zhanggong");
                UnlockArchive("ArchiveChar_liaiyi");
                UnlockArchive("ArchiveChar_wangxiaodi");
                UnlockArchive("ArchiveChar_zhaoshifu");
                UnlockArchive("ArchiveChar_xiaofang");
                UnlockStory("prologue_06_team");
                break;
            case "prologue_07_first_repair":
                UnlockArchive("ArchiveTrain_nf5");
                UnlockStory("prologue_07_first_repair");
                break;
            case "prologue_08_first_run":
                UnlockArchive("ArchiveTrain_sy22");
                UnlockMusic("first_light");
                UnlockStory("prologue_08_first_run");
                break;
            case "prologue_09_funding":
                UnlockStory("prologue_09_funding");
                break;
            case "prologue_10_transition":
                UnlockStory("prologue_10_transition");
                break;
        }
    }

    /// <summary>本次会话新增解锁项（供 VN 话结束时奖励弹窗消费）。由 UnlockXX 方法在首次解锁时填充。</summary>
    private static readonly List<string> pendingUnlocks = new List<string>();

    /// <summary>取走并清空新增解锁清单（奖励弹窗用）。</summary>
    public static string[] TakePendingUnlocks()
    {
        var result = pendingUnlocks.ToArray();
        pendingUnlocks.Clear();
        return result;
    }

    private static void MarkNewUnlock(string label)
    {
        if (!pendingUnlocks.Contains(label))
            pendingUnlocks.Add(label);
    }

    /// <summary>解锁音乐（幂等；首次解锁记录供奖励弹窗）。</summary>
    public static void UnlockMusic(string id)
    {
        if (PlayerPrefs.GetInt("ArchiveMusic_" + id, 0) == 1) return;
        PlayerPrefs.SetInt("ArchiveMusic_" + id, 1);
        PlayerPrefs.Save();
        MarkNewUnlock("音乐 · " + id);
    }

    /// <summary>解锁场景（幂等；首次解锁记录）。</summary>
    public static void UnlockScene(string id)
    {
        if (PlayerPrefs.GetInt("ArchiveScene_" + id, 0) == 1) return;
        PlayerPrefs.SetInt("ArchiveScene_" + id, 1);
        PlayerPrefs.Save();
        MarkNewUnlock("场景 · " + id);
    }

    /// <summary>解锁故事章节（幂等；首次解锁记录）。</summary>
    public static void UnlockStory(string id)
    {
        if (PlayerPrefs.GetInt("ArchiveStory_" + id, 0) == 1) return;
        PlayerPrefs.SetInt("ArchiveStory_" + id, 1);
        PlayerPrefs.Save();
        MarkNewUnlock("章节 · " + id);
    }
}
