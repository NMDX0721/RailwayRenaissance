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
    }

    /// <summary>7个预设城市的模板常量，供 Layer 2 随机化时参考或直接加载。</summary>
    public static class CityTemplates
    {
        // ── 雾峰村 ──────────────────────────────────────────────
        public static CityData MistPeakVillage => new CityData
        {
            name = "雾峰村",
            type = "agriculture",
            population = 3200,
            populationGrowth = 0.012f,
            coordinates = new Vector2Int(12, 8),
            industries = new[] { "rice", "tea", "bamboo" },
            dependencies = new DependencyData
            {
                imports = new[] { "乌金岭:coal", "枫林渡:medicine" },
                exports = new[] { "青溪镇:rice", "云渡港:tea" }
            },
            sandPenetrationBase = 0.08f,
            politicalLean = "neutral",
            unlockRegion = 1,
            npcPool = new[] { "village_head", "tea_master", "bamboo_carver" }
        };

        // ── 乌金岭 ──────────────────────────────────────────────
        public static CityData BlackGoldRidge => new CityData
        {
            name = "乌金岭",
            type = "mining",
            population = 5800,
            populationGrowth = 0.008f,
            coordinates = new Vector2Int(15, 4),
            industries = new[] { "coal", "iron", "limestone" },
            dependencies = new DependencyData
            {
                imports = new[] { "青溪镇:machinery", "雾峰村:bamboo" },
                exports = new[] { "青溪镇:coal", "枫林渡:iron", "云渡港:limestone" }
            },
            sandPenetrationBase = 0.12f,
            politicalLean = "labor",
            unlockRegion = 1,
            npcPool = new[] { "mine_foreman", "union_rep", "geologist" }
        };

        // ── 青溪镇 ──────────────────────────────────────────────
        public static CityData ClearStreamTown => new CityData
        {
            name = "青溪镇",
            type = "industrial",
            population = 12500,
            populationGrowth = 0.015f,
            coordinates = new Vector2Int(18, 10),
            industries = new[] { "machinery", "textiles", "chemicals" },
            dependencies = new DependencyData
            {
                imports = new[] { "乌金岭:coal", "雾峰村:rice", "白鹭洲:chemicals" },
                exports = new[] { "乌金岭:machinery", "云渡港:textiles", "枫林渡:machinery" }
            },
            sandPenetrationBase = 0.18f,
            politicalLean = "progressive",
            unlockRegion = 1,
            npcPool = new[] { "factory_owner", "union_leader", "engineer" }
        };

        // ── 云渡港 ──────────────────────────────────────────────
        public static CityData CloudFerryPort => new CityData
        {
            name = "云渡港",
            type = "port",
            population = 22000,
            populationGrowth = 0.020f,
            coordinates = new Vector2Int(22, 7),
            industries = new[] { "shipping", "logistics", "fishing" },
            dependencies = new DependencyData
            {
                imports = new[] { "青溪镇:textiles", "白鹭洲:tourism", "望海港:steel" },
                exports = new[] { "枫林渡:fish", "望海港:logistics", "白鹭洲:shipping" }
            },
            sandPenetrationBase = 0.25f,
            politicalLean = "merchant",
            unlockRegion = 2,
            npcPool = new[] { "harbor_master", "shipping_magnate", "dockworker" }
        };

        // ── 白鹭洲 ──────────────────────────────────────────────
        public static CityData WhiteEgretIslet => new CityData
        {
            name = "白鹭洲",
            type = "tourism",
            population = 4100,
            populationGrowth = 0.005f,
            coordinates = new Vector2Int(20, 12),
            industries = new[] { "tourism", "pharmaceuticals", "herbs" },
            dependencies = new DependencyData
            {
                imports = new[] { "云渡港:shipping", "望海港:pharmaceuticals" },
                exports = new[] { "青溪镇:chemicals", "枫林渡:herbs", "雾峰村:medicine" }
            },
            sandPenetrationBase = 0.10f,
            politicalLean = "conservative",
            unlockRegion = 2,
            npcPool = new[] { "herbalist", "pharma_researcher", "tourism_official" }
        };

        // ── 枫林渡 ──────────────────────────────────────────────
        public static CityData MapleForestCrossing => new CityData
        {
            name = "枫林渡",
            type = "administrative",
            population = 38000,
            populationGrowth = 0.010f,
            coordinates = new Vector2Int(16, 14),
            industries = new[] { "administration", "education", "healthcare" },
            dependencies = new DependencyData
            {
                imports = new[] { "青溪镇:machinery", "云渡港:fish", "白鹭洲:herbs" },
                exports = new[] { "雾峰村:medicine", "乌金岭:healthcare", "望海港:education" }
            },
            sandPenetrationBase = 0.22f,
            politicalLean = "bureaucratic",
            unlockRegion = 1,
            npcPool = new[] { "governor", "judge", "academy_head" }
        };

        // ── 望海港 ──────────────────────────────────────────────
        public static CityData SeaViewHarbor => new CityData
        {
            name = "望海港",
            type = "industrial",
            population = 45000,
            populationGrowth = 0.018f,
            coordinates = new Vector2Int(25, 5),
            industries = new[] { "steel", "shipbuilding", "refining", "pharmaceuticals" },
            dependencies = new DependencyData
            {
                imports = new[] { "乌金岭:iron", "云渡港:logistics", "枫林渡:education" },
                exports = new[] { "云渡港:steel", "白鹭洲:pharmaceuticals", "青溪镇:shipbuilding" }
            },
            sandPenetrationBase = 0.30f,
            politicalLean = "corporate",
            unlockRegion = 3,
            npcPool = new[] { "ceo", "shipyard_director", "port_commissioner" }
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