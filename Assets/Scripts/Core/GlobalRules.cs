using System.Collections.Generic;
using UnityEngine;

namespace RailwayRenaissance.Core
{
    /// <summary>
    /// 全局规则配置——所有系数、阈值、权重均在此，禁止代码内硬编码。
    /// 每个世界种子携带独属的 GlobalRules 实例。
    /// </summary>
    [System.Serializable]
    public class GlobalRules
    {
        // ===== 波动引擎 =====
        public float fluctuationIntensity = 0.08f;      // Perlin 噪声强度
        public float eventSigmoidSteepness = 1.0f;     // 事件 Sigmoid 陡度
        public float eventMaxMultiplier = 0.5f;        // 事件最大影响倍率

        // ===== 技能成长 =====
        public float baseLearningRate = 1.0f;           // 基准学习速度
        public float gapBonusMaxRate = 0.5f;            // 追赶最大倍率
        public float inertiaCoefficient = 0.15f;        // 惯性系数
        public float inertiaClampMin = 0.5f;            // 惯性最小值
        public float inertiaClampMax = 1.5f;            // 惯性最大值
        public float parentFeedbackRate = 0.3f;          // 子→母反馈率
        public float talentModifierBase = 0.5f;         // 天赋修正基准
        public float talentModifierScale = 0.5f;        // 天赋修正缩放
        public float intelligenceModifierBase = 0.5f;   // 智力修正基准
        public float intelligenceModifierScale = 0.5f;  // 智力修正缩放
        public float macroModifierBase = 0.8f;          // 宏观环境修正基准
        public float macroModifierScale = 0.4f;         // 宏观环境修正缩放

        // ===== 疲劳 =====
        public float baseFatigueGain = 10f;              // 基础疲劳增长
        public float overtimeThreshold = 7f;             // 连班触发天数
        public float overtimePenalty = 5f;               // 连班额外疲劳
        public float driverFatigueBonus = 3f;            // 司机额外疲劳
        public float fatigueRestRecovery = 30f;          // 休息恢复量
        public float fatigueForceRestThreshold = 80f;    // 强制休息阈值

        // ===== 忠诚度 =====
        public float baseLoyaltyChange = 0.1f;           // 基准日变化
        public float wageLoyaltyImpact = 0.5f;           // 工资对忠诚度影响
        public float socialComparisonRate = 0.1f;        // 社会对比系数
        public float salaryComparisonThreshold = 1.1f;  // 工资对比触发阈值
        public float salaryJealousyMagnitude = 0.1f;    // 嫉妒程度系数
        public float quitProbabilityBase = 0.1f;         // 离职概率基准
        public float synergyCoefficient = 0.2f;          // 协同效应系数

        // ===== 师徒传承 =====
        public float mentorshipApprenticeBonus = 2.0f;    // 学徒学习倍率
        public float mentorshipMentorGainRate = 0.1f;     // 师傅收益比例
        public float mentorshipMentorFatigue = 5f;        // 师傅每日额外疲劳
        public float mentorHighLevelThreshold = 4f;       // 师傅高等级阈值
        public float mentorHighLevelMultiplier = 2.0f;    // 高等级师傅倍率
        public float mentorNormalMultiplier = 1.5f;       // 普通师傅倍率

        // ===== 工资谈判 =====
        public float wageNegotiationThreshold = 5f;       // 触发工资谈判的技能提升阈值
        public float wageNegotiationAcceptBonus = 5f;     // 接受加薪的忠诚度奖励
        public float wageNegotiationRefusePenalty = 15f;  // 拒绝加薪的忠诚度惩罚

        // ===== 岗位匹配系数 =====
        public float matchCoefficientCore = 1.0f;          // 核心技能匹配
        public float matchCoefficientRelated = 0.5f;       // 相关技能匹配
        public float matchCoefficientUnrelated = 0.2f;     // 不相关技能匹配

        // ===== 培训 =====
        public float trainingCost = 200f;                  // 培训费用
        public float trainingCooldownDays = 7f;             // 培训冷却天数

        // ===== 非线性阈值 =====
        public float fatigueDangerThreshold = 80f;       // 疲劳危险阈值
        public float fatigueDangerMultiplier = 2.0f;     // 疲劳危险倍率
        public float loyaltyDangerThreshold = 30f;       // 忠诚危险阈值
        public float loyaltyDangerMultiplier = 1.5f;     // 忠诚危险倍率
        public float skillCeilingThreshold = 80f;        // 技能天花板阈值
        public float skillCeilingMultiplier = 0.5f;      // 天花板减速
        public float skillNewbieThreshold = 20f;         // 新手红利阈值
        public float skillNewbieMultiplier = 1.5f;       // 新手加速

        // ===== 波动权重表（由世界种子生成独属权重） =====
        // 注：不用 Dictionary（Unity 不支持序列化），改用 List<WeightTable>
        public List<WeightTable> fluctuationWeightsList = new List<WeightTable>();

        [System.Serializable]
        public struct WeightTable
        {
            public string formulaName;
            public float[] weights;
        }

        /// <summary>获取指定公式名的权重数组。</summary>
        public float[] GetWeights(string formulaName)
        {
            for (int i = 0; i < fluctuationWeightsList.Count; i++)
            {
                if (fluctuationWeightsList[i].formulaName == formulaName)
                    return fluctuationWeightsList[i].weights;
            }
            return new float[] { 1.0f }; // 默认等权重
        }
    }
}