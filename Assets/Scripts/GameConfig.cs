using System;
using UnityEngine;

[Serializable]
public class GameConfig
{
    public string playerAlias = "";
    public string difficulty = "normal";
    // 创世核种子 ID（自 Layer 5 起，世界初始参数由此决定；仅供无有效 seedCode 时旧档静态资源兼容回退）
    public string seedId = "seed_001";
    // 种子码（RR-XXXXX-YYYYY 格式，默认随机生成或快捷预设/输入）
    public string seedCode = "";
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

    [Serializable]
    public struct SeedPreset
    {
        public int seed;
        public string code;
        public string name;
        public string description;

        public SeedPreset(int seed, string code, string name, string description)
        {
            this.seed = seed;
            this.code = code;
            this.name = name;
            this.description = description;
        }
    }

    public static readonly SeedPreset[] Presets = new SeedPreset[]
    {
        new SeedPreset(1,   "RR-00001-00003", "标准拓扑", "均衡开局 (Seed 1) · 紧凑产销环线"),
        new SeedPreset(42,  "RR-0002A-0007E", "紧凑网络", "近邻聚落 (Seed 42) · 短距离密集连接"),
        new SeedPreset(77,  "RR-0004D-000E7", "干线走廊", "长距走廊 (Seed 77) · 东西主干线延伸"),
        new SeedPreset(113, "RR-00071-00153", "环状干线", "多重连通 (Seed 113) · 双向冗余环网"),
        new SeedPreset(999, "RR-003E7-00BB5", "边陲高地", "极远端节点 (Seed 999) · 矿区深山孤立")
    };

    /// <summary>生成 20-bit [1, 0xFFFFF] 范围内的随机有效种子码。</summary>
    public static string GenerateRandomSeedCode(System.Random rng = null)
    {
        int seed = rng != null ? rng.Next(1, 0x100000) : UnityEngine.Random.Range(1, 0x100000);
        return WorldGen.WorldGenerator.EncodeSeed(seed);
    }

    /// <summary>验证种子码格式及校验和是否合法。</summary>
    public static bool IsValidSeedCode(string code)
    {
        return WorldGen.WorldGenerator.DecodeSeed(code) >= 0;
    }

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
