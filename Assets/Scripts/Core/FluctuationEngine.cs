using Unity.Mathematics;
using UnityEngine;

namespace RailwayRenaissance.Core
{
    /// <summary>
    /// 分级波动计算函数家族。
    /// L1 简单波动 → L2 加权波动 → L3 复合波动 → L4 黑箱波动。
    /// 所有系数来自 GlobalRules 配置，禁止硬编码。
    /// </summary>
    public class FluctuationEngine
    {
        private GlobalRules rules;
        private System.Random worldRandom;
        private float difficultyMult;

        public FluctuationEngine(GlobalRules rules, int worldSeed, float difficulty = 1.0f)
        {
            this.rules = rules;
            this.worldRandom = new System.Random(worldSeed);
            this.difficultyMult = difficulty;
        }

        // ===== L1: 简单波动 =====
        /// <summary>用于临时/次要数值。输出 = 基准 × (1 + 随机偏移)</summary>
        public float Simple(float baseValue, float variance = 0.1f)
        {
            float offset = (float)(worldRandom.NextDouble() * 2.0 - 1.0) * variance;
            return baseValue * (1f + offset);
        }

        // ===== L2: 加权波动 =====
        /// <summary>
        /// 用于日常结算（技能成长、工资、忠诚度）。
        /// 输出 = 基准 × (1 + Σ(因素×权重)) × (1 + snoise 连续波动)
        /// </summary>
        public float Weighted(float baseValue, WeightedFactor[] factors, string formulaName, float timeSeed = 0f)
        {
            float[] weights = rules.GetWeights(formulaName);
            float factorSum = 0f;
            int len = Mathf.Min(factors.Length, weights.Length);
            for (int i = 0; i < len; i++)
            {
                factorSum += factors[i].Value * weights[i];
            }
            // snoise 3D 提供连续波动（基于时间+种子）
            float noiseVal = noise.snoise(new float3(timeSeed * 0.01f, factors.Length > 0 ? factors[0].Value : 0f, 0f));
            return baseValue * (1f + factorSum) * (1f + noiseVal * rules.fluctuationIntensity);
        }

        // ===== L3: 复合波动 =====
        /// <summary>
        /// 用于核心经济/重大事件（票价、补贴、事故概率）。
        /// 输出 = L2 × Sigmoid(事件强度) × 难度倍率
        /// </summary>
        public float Compound(float baseValue, WeightedFactor[] factors, string formulaName, float eventStrength = 0f, float timeSeed = 0f)
        {
            float weighted = Weighted(baseValue, factors, formulaName, timeSeed);
            // Sigmoid 处理事件强度——小事件几乎无影响，大事件迅速增幅
            float sigmoid = 1f / (1f + math.exp(-eventStrength * rules.eventSigmoidSteepness));
            return weighted * (1f + sigmoid * rules.eventMaxMultiplier) * difficultyMult;
        }

        // ===== L4: 黑箱波动 =====
        /// <summary>
        /// 用于世界基准值生成（千里马创世核内部调用）。
        /// 基于 fBm 多层噪声，输入种子数据，输出独属基准值。
        /// </summary>
        public float Blackbox(string valueName, float3 inputPos, int octaves = 4)
        {
            return fBm(inputPos, octaves);
        }

        // ===== fBm: 多层噪声叠加 =====
        /// <summary>基于 snoise 3D 的分形布朗运动。</summary>
        public static float fBm(float3 pos, int octaves = 4, float lacunarity = 2f, float persistence = 0.5f)
        {
            float value = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0f;
            for (int i = 0; i < octaves; i++)
            {
                value += amplitude * noise.snoise(pos * frequency);
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            return value / maxValue; // 归一化到 [-1, 1]
        }
    }

    /// <summary>
    /// 影响因素结构体（避免 Dictionary 频繁分配导致的 GC 压力）。
    /// </summary>
    public struct WeightedFactor
    {
        public string Name { get; }
        public float Value { get; }

        public WeightedFactor(string name, float value)
        {
            Name = name;
            Value = value;
        }
    }
}