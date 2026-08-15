using System;
using UnityEngine;

[Serializable]
public class GameConfig
{
    public string playerAlias = "";
    public string difficulty = "normal";
    // 创世核种子 ID（自 Layer 5 起，世界初始参数由此决定）
    public string seedId = "seed_001";
    // 自定义难度参数（指导司机模式）
    public float startMoney = 40000f;
    public float incomeMultiplier = 1.0f;
    public float costMultiplier = 1.0f;
    public float subsidyMultiplier = 1.0f;
    public float sandPriceMultiplier = 1.0f;
    public float passengerMultiplier = 1.0f;
    public float cargoMultiplier = 1.0f;
    public float eventFrequency = 1.0f;

    private const string SaveKey = "RailGameConfig";

    public static GameConfig Load()
    {
        var config = new GameConfig();
        if (PlayerPrefs.HasKey(SaveKey))
        {
            try
            {
                var loaded = JsonUtility.FromJson<GameConfig>(PlayerPrefs.GetString(SaveKey));
                if (loaded != null) config = loaded;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameConfig] Failed to parse saved config: " + e.Message);
            }
        }
        return config;
    }

    public void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(this));
        PlayerPrefs.Save();
    }

    /// <summary>根据预设难度套用参数。</summary>
    public void ApplyDifficultyPreset(string preset)
    {
        difficulty = preset;
        switch (preset)
        {
            case "easy": // 司炉
                startMoney = 50000f;
                incomeMultiplier = 1.3f;
                costMultiplier = 0.8f;
                subsidyMultiplier = 1.5f;
                sandPriceMultiplier = 1.0f;
                passengerMultiplier = 1.0f;
                cargoMultiplier = 1.0f;
                eventFrequency = 0.8f;
                break;
            case "hard": // 司机
                startMoney = 30000f;
                incomeMultiplier = 0.8f;
                costMultiplier = 1.2f;
                subsidyMultiplier = 0.7f;
                sandPriceMultiplier = 1.0f;
                passengerMultiplier = 1.0f;
                cargoMultiplier = 1.0f;
                eventFrequency = 1.2f;
                break;
            case "custom":
                break; // 保留滑块当前值
            default: // 副司机（普通）
                startMoney = 40000f;
                incomeMultiplier = 1.0f;
                costMultiplier = 1.0f;
                subsidyMultiplier = 1.0f;
                sandPriceMultiplier = 1.0f;
                passengerMultiplier = 1.0f;
                cargoMultiplier = 1.0f;
                eventFrequency = 1.0f;
                break;
        }
    }

    /// <summary>玩家显示名：设置过别名的用别名，否则用本名。</summary>
    public string PlayerDisplayName => string.IsNullOrWhiteSpace(playerAlias)
        ? "林彪悍"
        : playerAlias.Trim();
}