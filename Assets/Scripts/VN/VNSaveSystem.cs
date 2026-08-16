using System;
using UnityEngine;

[System.Serializable]
public class VNSaveData
{
    public string scriptName;
    public int sceneIndex;
    public int dialogueIndex;
    public string timestamp;
    public string bgName;
    public string bgmName;
}

public class VNSaveSystem
{
    private const int MaxSlots = 61;
    private const string SaveKeyPrefix = "VN_Save_";

    public bool SaveGame(int slot, string scriptName, int sceneIndex, int dialogueIndex, string bgName, string bgmName)
    {
        if (slot < 0 || slot >= MaxSlots)
        {
            Debug.LogError("[VN Save] Invalid slot: " + slot);
            return false;
        }

        var saveData = new VNSaveData
        {
            scriptName = scriptName,
            sceneIndex = sceneIndex,
            dialogueIndex = dialogueIndex,
            timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm"),
            bgName = bgName,
            bgmName = bgmName
        };

        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SaveKeyPrefix + slot, json);
        PlayerPrefs.Save();

        return true;
    }

    public VNSaveData LoadGame(int slot)
    {
        if (slot < 0 || slot >= MaxSlots)
        {
            Debug.LogError("[VN Save] Invalid slot: " + slot);
            return null;
        }

        string key = SaveKeyPrefix + slot;
        if (!PlayerPrefs.HasKey(key))
        {
            return null;
        }

        string json = PlayerPrefs.GetString(key);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            var saveData = JsonUtility.FromJson<VNSaveData>(json);
            return saveData;
        }
        catch (Exception e)
        {
            Debug.LogError("[VN Save] Failed to parse save data for slot " + slot + ": " + e.Message);
            return null;
        }
    }

    public void DeleteSave(int slot)
    {
        if (slot < 0 || slot >= MaxSlots) return;
        PlayerPrefs.DeleteKey(SaveKeyPrefix + slot);
        PlayerPrefs.Save();
    }

    public VNSaveData[] GetAllSaves()
    {
        var saves = new VNSaveData[MaxSlots];
        for (int i = 0; i < MaxSlots; i++)
        {
            saves[i] = LoadGame(i);
        }
        return saves;
    }

    public bool HasSave(int slot)
    {
        if (slot < 0 || slot >= MaxSlots) return false;
        return PlayerPrefs.HasKey(SaveKeyPrefix + slot);
    }

    public int MaxSlotCount => MaxSlots;

    public static void SaveGameplayData(int slotIndex)
    {
        // 保存经营数据
        GameDataSaveData gameplay = new GameDataSaveData();
        gameplay.money = GameData.GetMoney();
        gameplay.trust = GameData.GetTrust();
        gameplay.trainCondition = GameData.GetTrainCondition();
        gameplay.expectedPassengers = GameData.GetExpectedPassengers();
        gameplay.day = GameData.GetDay();
        gameplay.carCount = GameData.CarCount;

        string json = JsonUtility.ToJson(gameplay);
        PlayerPrefs.SetString("SaveSlot_" + slotIndex, json);
        PlayerPrefs.Save();
    }

    public static bool LoadGameplayData(int slotIndex)
    {
        string json = PlayerPrefs.GetString("SaveSlot_" + slotIndex, "");
        if (string.IsNullOrEmpty(json)) return false;

        GameDataSaveData data = JsonUtility.FromJson<GameDataSaveData>(json);
        GameData.RestoreFromSave(data);
        return true;
    }
}

[System.Serializable]
public class GameDataSaveData
{
    public int money;
    public int trust;
    public int trainCondition;
    public int expectedPassengers;
    public int day;
    public int carCount = 2;
}
