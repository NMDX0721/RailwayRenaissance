using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldGen
{
    /// <summary>世界种子数据包——千里马创世核的产出物。</summary>
    [Serializable]
    public class WorldSeedData
    {
        public string seedId;
        public string name;
        public string version = "v3.0";
        public string seedCode;

        public Dictionary<string, CityData> cities = new Dictionary<string, CityData>();
        public ResourceDistributionData resourceDistribution = new ResourceDistributionData();
        public string politicalTendency = "market";
        public int politicalCycleLength = 180;
        public float politicalFavor = 0.5f;
        public float stability = 0.6f;
        public TrendInitialValues initialTrends = new TrendInitialValues();
        public List<RailEdgeData> railEdges = new List<RailEdgeData>();
        public GlobalRules globalRules = new GlobalRules();
        public string[] storyTags = Array.Empty<string>();
    }

    [Serializable]
    public class CityData
    {
        public string name;
        public string type;          // agriculture / mining / industrial / port / tourism / administrative
        public int population;
        public float populationGrowth;
        public Vector2Int coordinates;
        public string[] industries = Array.Empty<string>();
        public DependencyData dependencies = new DependencyData();
        public float sandPenetrationBase = 0.15f;
        public string politicalLean = "neutral";
        public int unlockRegion;
        public string[] npcPool = Array.Empty<string>();
    }

    [Serializable]
    public class DependencyData
    {
        public string[] imports = Array.Empty<string>();  // "city:resource"
        public string[] exports = Array.Empty<string>();  // "city:resource"
    }

    [Serializable]
    public class ResourceDistributionData
    {
        public string pattern = "dispersed";  // concentrated / dispersed / political
        public string[] criticalNodes = Array.Empty<string>();
        public float bottleneckRisk = 0.15f;
        public float governmentInterventionProb = 0.05f;
    }

    [Serializable]
    public class TrendInitialValues
    {
        public float trust = 0.62f;
        public float fiscalPressure = 0.30f;
        public float sandPenetration = 0.15f;
        public float politicalPressure = 0.20f;
        public float infrastructureDecay = 0.30f;
    }

    [Serializable]
    public class RailEdgeData
    {
        public string fromCity;
        public string toCity;
        public float travelTime;   // minutes
        public int capacity;       // passengers per trip
        public string trackType = "single";  // single / double
    }

    [Serializable]
    public class GlobalRules
    {
        public float incidentBaseChance = 0.005f;
        public float trustNaturalDecay = 0.0002f;
        public float fiscalGrowthBase = 0.001f;
        public float maxRepairCostMultiplier = 2.0f;
        public float subsidyBase = 8000f;

        // ── 千里马创世核黑箱基线 ──────────────────────────────────
        public float fluctuationIntensity = 0.05f;
        public float baseLearningRate = 0.8f;
        public float gapBonusMaxRate = 0.3f;
        public float inertiaCoefficient = 0.1f;
        public List<WeightTable> fluctuationWeightsList = new List<WeightTable>();

        [Serializable]
        public class WeightTable
        {
            public string formulaName;
            public float[] weights;
        }
    }

    /// <summary>7个预设城市的模板常量，供 Layer 2 随机化时参考或直接加载。</summary>
    public static class CityTemplates
    {
        // ── 雾峰村 ──────────────────────────────────────────────
        public static CityData MistPeakVillage => new CityData
        {
            name = "雾峰村",
            type = "agriculture",
            population = 8000,
            populationGrowth = 0.0002f,
            coordinates = new Vector2Int(120, 340),
            industries = new[] { "tea", "tourism" },
            dependencies = new DependencyData
            {
                imports = new[] { "乌金岭:coal" },
                exports = new[] { "青溪镇:tea" }
            },
            sandPenetrationBase = 0.15f,
            politicalLean = "neutral",
            unlockRegion = 0,
            npcPool = new[] { "laochen", "liayi", "laocheng_villager" }
        };

        // ── 乌金岭 ──────────────────────────────────────────────
        public static CityData BlackGoldRidge => new CityData
        {
            name = "乌金岭",
            type = "mining",
            population = 4000,
            populationGrowth = -0.0003f,
            coordinates = new Vector2Int(200, 380),
            industries = new[] { "coal", "iron" },
            dependencies = new DependencyData
            {
                imports = new[] { "青溪镇:machinery" },
                exports = new[] { "雾峰村:coal", "青溪镇:iron" }
            },
            sandPenetrationBase = 0.20f,
            politicalLean = "neutral",
            unlockRegion = 0,
            npcPool = new[] { "zhanggong", "zhaoshifu" }
        };

        // ── 青溪镇 ──────────────────────────────────────────────
        public static CityData ClearStreamTown => new CityData
        {
            name = "青溪镇",
            type = "industrial",
            population = 12000,
            populationGrowth = 0.0004f,
            coordinates = new Vector2Int(340, 300),
            industries = new[] { "manufacturing", "light_industry" },
            dependencies = new DependencyData
            {
                imports = new[] { "乌金岭:coal", "乌金岭:iron" },
                exports = new[] { "云渡港:machinery", "雾峰村:machinery" }
            },
            sandPenetrationBase = 0.25f,
            politicalLean = "neutral",
            unlockRegion = 1,
            npcPool = new[] { "wangxiaodi", "qingxi_merchant" }
        };

        // ── 云渡港 ──────────────────────────────────────────────
        public static CityData CloudFerryPort => new CityData
        {
            name = "云渡港",
            type = "port",
            population = 25000,
            populationGrowth = 0.0006f,
            coordinates = new Vector2Int(460, 420),
            industries = new[] { "shipping", "trade" },
            dependencies = new DependencyData
            {
                imports = new[] { "青溪镇:machinery", "白鹭洲:electronics" },
                exports = new[] { "雾峰村:shipping_goods" }
            },
            sandPenetrationBase = 0.35f,
            politicalLean = "market",
            unlockRegion = 2,
            npcPool = new[] { "chenhenian", "yundu_harbor_master" }
        };

        // ── 白鹭洲 ──────────────────────────────────────────────
        public static CityData WhiteEgretIslet => new CityData
        {
            name = "白鹭洲",
            type = "administrative",
            population = 50000,
            populationGrowth = 0.0004f,
            coordinates = new Vector2Int(580, 260),
            industries = new[] { "finance", "education", "administration" },
            dependencies = new DependencyData
            {
                imports = new[] { "青溪镇:machinery", "云渡港:shipping_goods" },
                exports = new[] { "枫林渡:electronics", "望海港:electronics" }
            },
            sandPenetrationBase = 0.40f,
            politicalLean = "authoritarian",
            unlockRegion = 3,
            npcPool = new[] { "baiguan", "bailuzhou_official" }
        };

        // ── 枫林渡 ──────────────────────────────────────────────
        public static CityData MapleForestCrossing => new CityData
        {
            name = "枫林渡",
            type = "industrial",
            population = 35000,
            populationGrowth = 0.0003f,
            coordinates = new Vector2Int(620, 180),
            industries = new[] { "heavy_industry", "chemical" },
            dependencies = new DependencyData
            {
                imports = new[] { "乌金岭:coal", "白鹭洲:finance" },
                exports = new[] { "望海港:steel", "白鹭洲:steel" }
            },
            sandPenetrationBase = 0.30f,
            politicalLean = "welfare",
            unlockRegion = 4,
            npcPool = new[] { "fenglin_foreman" }
        };

        // ── 望海港 ──────────────────────────────────────────────
        public static CityData SeaViewHarbor => new CityData
        {
            name = "望海港",
            type = "port",
            population = 40000,
            populationGrowth = 0.0005f,
            coordinates = new Vector2Int(700, 400),
            industries = new[] { "international_trade", "shipping" },
            dependencies = new DependencyData
            {
                imports = new[] { "枫林渡:steel", "白鹭洲:electronics" },
                exports = new[] { "云渡港:imported_goods" }
            },
            sandPenetrationBase = 0.50f,
            politicalLean = "market",
            unlockRegion = 5,
            npcPool = new[] { "wanghai_merchant" }
        };

        /// <summary>返回所有预设城市的字典，key 为城市名。</summary>
        public static Dictionary<string, CityData> GetAll()
        {
            return new Dictionary<string, CityData>
            {
                { "雾峰村", MistPeakVillage },
                { "乌金岭", BlackGoldRidge },
                { "青溪镇", ClearStreamTown },
                { "云渡港", CloudFerryPort },
                { "白鹭洲", WhiteEgretIslet },
                { "枫林渡", MapleForestCrossing },
                { "望海港", SeaViewHarbor }
            };
        }
    }
}