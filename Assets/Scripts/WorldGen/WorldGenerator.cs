using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WorldGen
{
    /// <summary>
    /// 千里马创世核 Layer 2 确定性生成器。
    /// 全部随机使用 System.Random(seed)，同一种子两次生成完全一致。
    /// </summary>
    public static class WorldGenerator
    {
        // ======================================================================
        // 资源映射：各城市类型产出资源
        // ======================================================================
        private static readonly Dictionary<string, string[]> TypeResources = new()
        {
            { "agriculture",    new[] { "rice", "tea", "bamboo" } },
            { "mining",         new[] { "coal", "iron", "limestone" } },
            { "industrial",     new[] { "machinery", "textiles", "chemicals", "steel", "shipbuilding" } },
            { "port",           new[] { "shipping", "logistics", "fish" } },
            { "tourism",        new[] { "tourism", "pharmaceuticals", "herbs" } },
            { "administrative", new[] { "administration", "education", "healthcare" } },
        };

        // 各城市类型从其他类型进口的资源需求（不与自己产出重叠）
        private static readonly Dictionary<string, string[]> TypeNeeds = new()
        {
            { "agriculture",    new[] { "coal", "machinery", "medicine" } },
            { "mining",         new[] { "machinery", "bamboo", "shipping" } },
            { "industrial",     new[] { "coal", "iron", "rice" } },
            { "port",           new[] { "steel", "tourism", "textiles" } },
            { "tourism",        new[] { "shipping", "education", "textiles" } },
            { "administrative", new[] { "machinery", "fish", "herbs" } },
        };

        // 城市类型作为"高价值资源城市"的判断（用于 political 资源分布模式）
        private static readonly HashSet<string> HighValueTypes = new() { "mining", "industrial", "port" };

        // 城市类型 → 铁路边容量
        private static readonly Dictionary<string, int> TypeCapacity = new()
        {
            { "agriculture",    30 },
            { "mining",         35 },
            { "industrial",     40 },
            { "port",           45 },
            { "tourism",        30 },
            { "administrative", 50 },
        };

        // ======================================================================
        // 公共 API
        // ======================================================================

        /// <summary>根据种子生成完整的世界种子数据包。</summary>
        public static WorldSeedData Generate(int seed)
        {
            var r = new System.Random(seed);
            var data = new WorldSeedData();
            data.seedId = "seed_" + seed.ToString("D5");
            data.seedCode = EncodeSeed(seed);

            // 2.1 城市生成
            var allTemplates = CityTemplates.GetAll();
            data.cities = GenerateCities(r, allTemplates);

            // 2.2 依赖图生成
            GenerateDependencyGraph(r, data.cities);

            // 2.3 资源分布 + 政治倾向 + 趋势线
            GenerateResourceDistribution(r, data);
            GeneratePoliticalTendency(r, data);
            GenerateInitialTrends(r, data);

            // 2.4 铁路边生成
            data.railEdges = GenerateRailEdges(r, data.cities);

            // 预留种子注入点（Layer 5 实现）
            ApplySeed(data);

            return data;
        }

        /// <summary>从 Resources/Seeds/ 加载种子。</summary>
        public static WorldSeedData LoadFromResources(string seedId)
        {
            TextAsset json = Resources.Load<TextAsset>("Seeds/" + seedId);
            if (json == null) return null;
            return JsonUtility.FromJson<WorldSeedData>(json.text);
        }

        /// <summary>从种子编码还原并生成世界。</summary>
        public static WorldSeedData GenerateFromSeedCode(string seedCode)
        {
            int seed = DecodeSeed(seedCode);
            if (seed < 0) return null;
            return Generate(seed);
        }

        /// <summary>编码种子为 RR-XXXXX-YYYYY 格式。</summary>
        public static string EncodeSeed(int seed)
        {
            int checksum = (seed * 3) % 0xFFFFF;
            return "RR-" + seed.ToString("X5") + "-" + checksum.ToString("X5");
        }

        /// <summary>解码 RR-XXXXX-YYYYY，验证校验和，返回种子值。</summary>
        public static int DecodeSeed(string seedCode)
        {
            if (string.IsNullOrEmpty(seedCode) || !seedCode.StartsWith("RR-"))
                return -1;

            string[] parts = seedCode.Split('-');
            if (parts.Length != 3)
                return -1;

            try
            {
                int seed = Convert.ToInt32(parts[1], 16);
                int expectedChecksum = (seed * 3) % 0xFFFFF;
                int providedChecksum = Convert.ToInt32(parts[2], 16);
                return providedChecksum == expectedChecksum ? seed : -1;
            }
            catch
            {
                return -1;
            }
        }

        // ======================================================================
        // 2.1 城市生成
        // ======================================================================

        private static Dictionary<string, CityData> GenerateCities(
            System.Random r, Dictionary<string, CityData> templates)
        {
            // 必选：雾峰村 + 乌金岭
            var result = new Dictionary<string, CityData>
            {
                { "雾峰村", DeepCloneCity(templates["雾峰村"]) },
                { "乌金岭", DeepCloneCity(templates["乌金岭"]) },
            };

            // 可选池：青溪镇/云渡港/白鹭洲/枫林渡/望海港
            var pool = new Dictionary<string, CityData>
            {
                { "青溪镇", templates["青溪镇"] },
                { "云渡港", templates["云渡港"] },
                { "白鹭洲", templates["白鹭洲"] },
                { "枫林渡", templates["枫林渡"] },
                { "望海港", templates["望海港"] },
            };

            // 随机选 2-5 个
            var poolKeys = pool.Keys.ToList();
            int count = r.Next(2, 6); // 2-5
            for (int i = 0; i < count && poolKeys.Count > 0; i++)
            {
                int idx = r.Next(poolKeys.Count);
                string key = poolKeys[idx];
                result[key] = DeepCloneCity(pool[key]);
                poolKeys.RemoveAt(idx);
            }

            // 人口 ±20% 抖动
            foreach (var kvp in result)
            {
                float jitter = 1.0f + (float)(r.NextDouble() * 0.4 - 0.2);
                kvp.Value.population = Mathf.Max(500, (int)(kvp.Value.population * jitter));
            }

            return result;
        }

        /// <summary>深拷贝城市数据（避免模板数据被修改）。</summary>
        private static CityData DeepCloneCity(CityData src)
        {
            return new CityData
            {
                name = src.name,
                type = src.type,
                population = src.population,
                populationGrowth = src.populationGrowth,
                coordinates = src.coordinates,
                industries = (string[])src.industries.Clone(),
                dependencies = new DependencyData
                {
                    imports = Array.Empty<string>(),
                    exports = Array.Empty<string>(),
                },
                sandPenetrationBase = src.sandPenetrationBase,
                politicalLean = src.politicalLean,
                unlockRegion = src.unlockRegion,
                npcPool = (string[])src.npcPool.Clone(),
            };
        }

        // ======================================================================
        // 2.2 依赖图生成
        // ======================================================================

        private static void GenerateDependencyGraph(
            System.Random r, Dictionary<string, CityData> cities)
        {
            // 找出每个资源由哪些城市生产
            var resourceProducers = new Dictionary<string, List<string>>();
            foreach (var kvp in cities)
            {
                foreach (string res in kvp.Value.industries)
                {
                    if (!resourceProducers.ContainsKey(res))
                        resourceProducers[res] = new List<string>();
                    resourceProducers[res].Add(kvp.Key);
                }
            }

            // 构建有向图
            Dictionary<string, List<string>> graph = new();
            foreach (var kvp in cities)
                graph[kvp.Key] = new List<string>();

            bool success = false;
            for (int attempt = 0; attempt < 3 && !success; attempt++)
            {
                // 重置依赖
                foreach (var kvp in cities)
                {
                    kvp.Value.dependencies.imports = Array.Empty<string>();
                    kvp.Value.dependencies.exports = Array.Empty<string>();
                }
                graph.Clear();
                foreach (var kvp in cities)
                    graph[kvp.Key] = new List<string>();

                // 为每个城市生成进口需求
                foreach (var kvp in cities)
                {
                    string cityName = kvp.Key;
                    CityData city = kvp.Value;
                    string[] needs = TypeNeeds.ContainsKey(city.type) ? TypeNeeds[city.type] : TypeNeeds["industrial"];
                    int numNeeds = 1 + r.Next(needs.Length); // 至少1个，最多全部

                    var usedResources = new HashSet<string>();
                    var importList = new List<string>();

                    for (int i = 0; i < numNeeds; i++)
                    {
                        // 选一个还没用过的需求资源
                        var available = needs.Where(n => !usedResources.Contains(n)).ToList();
                        if (available.Count == 0) break;
                        string need = available[r.Next(available.Count)];
                        usedResources.Add(need);

                        // 找最近的供应商
                        string supplier = FindClosestProducer(need, cityName, cities, resourceProducers, r);
                        if (supplier != null)
                        {
                            importList.Add(supplier + ":" + need);
                            graph[cityName].Add(supplier);
                        }
                    }

                    city.dependencies.imports = importList.ToArray();
                }

                // 补全 exports（从 imports 反向推导）
                foreach (var kvp in cities)
                {
                    var exports = new List<string>();
                    foreach (var other in cities)
                    {
                        if (other.Key == kvp.Key) continue;
                        foreach (string imp in other.Value.dependencies.imports)
                        {
                            // imp 格式: "城市:资源"
                            var parts = imp.Split(':');
                            if (parts.Length == 2 && parts[0] == kvp.Key)
                                exports.Add(other.Key + ":" + parts[1]);
                        }
                    }
                    kvp.Value.dependencies.exports = exports.ToArray();
                }

                // 强连通检验
                success = IsStronglyConnected(graph);
            }

            // 最多3次重试后仍不满足，强制补边保证闭环
            if (!success)
            {
                ForceStrongConnectivity(graph, cities);
            }
        }

        /// <summary>找能生产某资源的最远（或指定）城市。</summary>
        private static string FindClosestProducer(
            string resource, string fromCity,
            Dictionary<string, CityData> cities,
            Dictionary<string, List<string>> resourceProducers,
            System.Random r)
        {
            if (!resourceProducers.ContainsKey(resource) || resourceProducers[resource].Count == 0)
            {
                // 没有城市生产此资源，随机选一个城市赋予它
                var candidates = cities.Keys.Where(k => k != fromCity).ToList();
                if (candidates.Count == 0) return null;
                string chosen = candidates[r.Next(candidates.Count)];
                var city = cities[chosen];
                var newIndustries = city.industries.ToList();
                newIndustries.Add(resource);
                city.industries = newIndustries.ToArray();
                resourceProducers[resource] = new List<string> { chosen };
                return chosen;
            }

            Vector2Int from = cities[fromCity].coordinates;
            Vector2Int? fromNullable = from;
            string best = null;
            float bestDist = float.MaxValue;

            foreach (string producer in resourceProducers[resource])
            {
                if (producer == fromCity) continue;
                float dist = Vector2Int.Distance(from, cities[producer].coordinates);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = producer;
                }
            }

            // 如果所有生产者都是自己（不可能，但兜底）
            if (best == null && resourceProducers[resource].Count > 0)
            {
                best = resourceProducers[resource][0];
            }

            return best;
        }

        // ======================================================================
        // 强连通检验（Kosaraju 算法）
        // ======================================================================

        private static bool IsStronglyConnected(Dictionary<string, List<string>> graph)
        {
            if (graph.Count <= 1) return true;

            var nodes = graph.Keys.ToList();
            var visited = new HashSet<string>();
            var stack = new Stack<string>();

            // 第一次 DFS：记录完成顺序
            void Dfs1(string node)
            {
                visited.Add(node);
                foreach (string neighbor in graph[node])
                {
                    if (!visited.Contains(neighbor))
                        Dfs1(neighbor);
                }
                stack.Push(node);
            }

            Dfs1(nodes[0]);

            // 所有节点必须可达
            if (visited.Count != nodes.Count)
                return false;

            // 构建反向图
            var reverseGraph = new Dictionary<string, List<string>>();
            foreach (string n in nodes)
                reverseGraph[n] = new List<string>();
            foreach (string n in nodes)
            {
                foreach (string neighbor in graph[n])
                {
                    reverseGraph[neighbor].Add(n);
                }
            }

            // 第二次 DFS：按完成顺序在反向图上遍历
            visited.Clear();
            void Dfs2(string node)
            {
                visited.Add(node);
                foreach (string neighbor in reverseGraph[node])
                {
                    if (!visited.Contains(neighbor))
                        Dfs2(neighbor);
                }
            }

            // 从栈顶（最后完成的节点）开始
            Dfs2(stack.Peek());

            return visited.Count == nodes.Count;
        }

        /// <summary>强制补边使图强连通（添加一个哈密顿环）。</summary>
        private static void ForceStrongConnectivity(
            Dictionary<string, List<string>> graph,
            Dictionary<string, CityData> cities)
        {
            var nodeList = graph.Keys.ToList();
            if (nodeList.Count <= 1) return;

            // 添加环：city1→city2→...→cityN→city1
            for (int i = 0; i < nodeList.Count; i++)
            {
                string from = nodeList[i];
                string to = nodeList[(i + 1) % nodeList.Count];

                // 仅在无此边时添加
                if (!graph[from].Contains(to))
                {
                    graph[from].Add(to);

                    // 更新依赖数据
                    var city = cities[from];
                    var imports = city.dependencies.imports.ToList();
                    imports.Add(to + ":emergency_supply");
                    city.dependencies.imports = imports.ToArray();
                }
            }

            // 重新计算 exports
            foreach (var kvp in cities)
            {
                var exports = new List<string>();
                foreach (var other in cities)
                {
                    if (other.Key == kvp.Key) continue;
                    foreach (string imp in other.Value.dependencies.imports)
                    {
                        var parts = imp.Split(':');
                        if (parts.Length == 2 && parts[0] == kvp.Key)
                            exports.Add(other.Key + ":" + parts[1]);
                    }
                }
                kvp.Value.dependencies.exports = exports.ToArray();
            }
        }

        // ======================================================================
        // 2.3 资源分布 + 政治倾向 + 趋势线
        // ======================================================================

        private static void GenerateResourceDistribution(
            System.Random r, WorldSeedData data)
        {
            // 先计算入度
            var inDegree = new Dictionary<string, int>();
            foreach (var kvp in data.cities)
                inDegree[kvp.Key] = 0;
            foreach (var kvp in data.cities)
            {
                foreach (string imp in kvp.Value.dependencies.imports)
                {
                    // "城市:资源"
                    var parts = imp.Split(':');
                    if (parts.Length == 2 && inDegree.ContainsKey(parts[0]))
                        inDegree[parts[0]]++;
                }
            }

            int cityCount = data.cities.Count;

            // 加权抽样 resourceDistribution.pattern
            string[] patterns = { "concentrated", "dispersed", "political" };
            float[] weights = { 0.35f, 0.35f, 0.30f };
            string pattern = WeightedSample(patterns, weights, r);

            data.resourceDistribution.pattern = pattern;

            switch (pattern)
            {
                case "concentrated":
                {
                    // 入度最高的 1-2 个城市
                    var sorted = inDegree.OrderByDescending(kvp => kvp.Value).ToList();
                    int topN = Mathf.Min(2, Mathf.Max(1, sorted.Count));
                    data.resourceDistribution.criticalNodes = sorted.Take(topN)
                        .Select(kvp => kvp.Key).ToArray();
                    data.resourceDistribution.bottleneckRisk = 0.35f;
                    data.resourceDistribution.governmentInterventionProb = 0.05f;
                    break;
                }
                case "dispersed":
                {
                    data.resourceDistribution.criticalNodes = Array.Empty<string>();
                    data.resourceDistribution.bottleneckRisk = 0.10f;
                    data.resourceDistribution.governmentInterventionProb = 0.05f;
                    break;
                }
                case "political":
                {
                    // 高价值资源城市（mining/industrial/port）
                    var highValue = data.cities
                        .Where(kvp => HighValueTypes.Contains(kvp.Value.type))
                        .Select(kvp => kvp.Key)
                        .ToArray();
                    data.resourceDistribution.criticalNodes = highValue.Length > 0
                        ? highValue
                        : new[] { data.cities.Keys.First() };
                    data.resourceDistribution.bottleneckRisk = 0.25f;
                    data.resourceDistribution.governmentInterventionProb = 0.15f;
                    break;
                }
            }
        }

        private static void GeneratePoliticalTendency(
            System.Random r, WorldSeedData data)
        {
            // 全局政治倾向加权抽样
            string[] tendencies = { "authoritarian", "market", "welfare" };
            float[] weights = { 0.30f, 0.40f, 0.30f };
            string globalTendency = WeightedSample(tendencies, weights, r);
            data.politicalTendency = globalTendency;

            // 政治周期长度 180±30 天
            data.politicalCycleLength = 180 + r.Next(-30, 31);

            // 政治好感度 0.4-0.6
            data.politicalFavor = 0.4f + (float)(r.NextDouble() * 0.2);

            // 稳定性 0.5-0.7
            data.stability = 0.5f + (float)(r.NextDouble() * 0.2);

            // 城市级倾向：30% 概率偏移 ±1 级
            foreach (var kvp in data.cities)
            {
                if (r.NextDouble() < 0.3)
                {
                    kvp.Value.politicalLean = GetShiftedLean(globalTendency, r);
                }
                else
                {
                    kvp.Value.politicalLean = globalTendency;
                }
            }
        }

        /// <summary>按偏移规则生成城市级政治倾向。</summary>
        private static string GetShiftedLean(string global, System.Random r)
        {
            switch (global)
            {
                case "authoritarian":
                    // 只能向温和偏移（market）
                    return "market";
                case "market":
                    // 可向 authoritarian 或 welfare 偏移
                    return r.Next(2) == 0 ? "authoritarian" : "welfare";
                case "welfare":
                    // 只能向市场化偏移
                    return "market";
                default:
                    return global;
            }
        }

        private static void GenerateInitialTrends(
            System.Random r, WorldSeedData data)
        {
            // 城市渗透加权均值
            float totalPop = 0f;
            float weightedSand = 0f;
            foreach (var kvp in data.cities)
            {
                totalPop += kvp.Value.population;
                weightedSand += kvp.Value.sandPenetrationBase * kvp.Value.population;
            }
            float avgSandPenetration = totalPop > 0 ? weightedSand / totalPop : 0.15f;

            // 各趋势线对称抖动 ±0.05
            data.initialTrends.trust = Jitter(0.62f, 0.05f, r);
            data.initialTrends.fiscalPressure = Jitter(0.30f, 0.05f, r);
            data.initialTrends.sandPenetration = Jitter(avgSandPenetration, 0.05f, r);
            data.initialTrends.politicalPressure = Jitter(0.20f, 0.05f, r);
            data.initialTrends.infrastructureDecay = Jitter(0.30f, 0.05f, r);
        }

        /// <summary>对称抖动：baseValue ± range（0-1 范围钳制）。</summary>
        private static float Jitter(float baseValue, float range, System.Random r)
        {
            float result = baseValue + (float)(r.NextDouble() * range * 2f - range);
            return Mathf.Clamp01(result);
        }

        // ======================================================================
        // 2.4 铁路边生成（Kruskal MST + 20% 随机边）
        // ======================================================================

        private static List<RailEdgeData> GenerateRailEdges(
            System.Random r, Dictionary<string, CityData> cities)
        {
            var cityList = cities.Values.ToList();
            if (cityList.Count < 2) return new List<RailEdgeData>();

            // 计算所有边
            var allEdges = new List<(int fromIdx, int toIdx, float distance)>();
            for (int i = 0; i < cityList.Count; i++)
            {
                for (int j = i + 1; j < cityList.Count; j++)
                {
                    float dist = Vector2Int.Distance(
                        cityList[i].coordinates, cityList[j].coordinates);
                    allEdges.Add((i, j, dist));
                }
            }

            // Kruskal 最小生成树
            allEdges.Sort((a, b) => a.distance.CompareTo(b.distance));
            var parent = new int[cityList.Count];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;

            int Find(int x)
            {
                while (parent[x] != x)
                {
                    parent[x] = parent[parent[x]];
                    x = parent[x];
                }
                return x;
            }

            void Union(int x, int y)
            {
                int rx = Find(x), ry = Find(y);
                if (rx != ry) parent[ry] = rx;
            }

            var mstEdges = new List<(int fromIdx, int toIdx, float distance)>();
            foreach (var edge in allEdges)
            {
                if (Find(edge.fromIdx) != Find(edge.toIdx))
                {
                    Union(edge.fromIdx, edge.toIdx);
                    mstEdges.Add(edge);
                }
            }

            // 额外 20% 随机边
            var nonMstEdges = allEdges.Where(e =>
                !mstEdges.Any(m => (m.fromIdx == e.fromIdx && m.toIdx == e.toIdx))).ToList();

            int extraCount = Mathf.Max(1, (int)(mstEdges.Count * 0.2f));
            for (int i = 0; i < extraCount && nonMstEdges.Count > 0; i++)
            {
                int idx = r.Next(nonMstEdges.Count);
                mstEdges.Add(nonMstEdges[idx]);
                nonMstEdges.RemoveAt(idx);
            }

            // 转成 RailEdgeData
            var result = new List<RailEdgeData>();
            var addedSet = new HashSet<(int, int)>();

            foreach (var edge in mstEdges)
            {
                var fromCity = cityList[edge.fromIdx];
                var toCity = cityList[edge.toIdx];

                // 去重
                var key = (Math.Min(edge.fromIdx, edge.toIdx), Math.Max(edge.fromIdx, edge.toIdx));
                if (addedSet.Contains(key)) continue;
                addedSet.Add(key);

                // 容量取两端城市容量的平均值
                int capFrom = TypeCapacity.ContainsKey(fromCity.type)
                    ? TypeCapacity[fromCity.type] : 30;
                int capTo = TypeCapacity.ContainsKey(toCity.type)
                    ? TypeCapacity[toCity.type] : 30;
                int capacity = Mathf.RoundToInt((capFrom + capTo) * 0.5f);

                result.Add(new RailEdgeData
                {
                    fromCity = fromCity.name,
                    toCity = toCity.name,
                    travelTime = edge.distance * 0.5f,
                    capacity = capacity,
                    trackType = "single",
                });
            }

            return result;
        }

        // ======================================================================
        // 种子注入点（Layer 5 实现）
        // ======================================================================

        private static void ApplySeed(WorldSeedData data)
        {
            // 预留：种子注入 GameData/SandRivalManager/EventManager 等
            // 在 Layer 5 中实现
        }

        // ======================================================================
        // 工具方法
        // ======================================================================

        /// <summary>加权随机抽样。</summary>
        private static T WeightedSample<T>(T[] items, float[] weights, System.Random r)
        {
            float total = weights.Sum();
            float roll = (float)(r.NextDouble() * total);
            float cumulative = 0f;
            for (int i = 0; i < items.Length; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                    return items[i];
            }
            return items[^1];
        }
    }
}