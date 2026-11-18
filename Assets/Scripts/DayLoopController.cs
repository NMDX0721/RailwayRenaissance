using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 日循环控制器：纯 C#，不依赖 UGUI / UI Toolkit。
/// 平板 RDA App 与旧 UGUI 场景 (UIManager) 共用同一入口：策略 → AdvanceDay → DayReport → 自动存档。
/// 政治压力与基建老化只输出定性措辞，绝不输出数值。
/// </summary>
public static class DayLoopController
{
    /// <summary>自动存档槽（独立于手动存档槽 0..MaxSlots-1，避免覆盖玩家存档）。</summary>
    public const int AutoSaveSlot = 99;

    public struct DayReport
    {
        public int Day;                 // 刚结算完的那一天
        public int MoneyDelta;
        public int MoneyAfter;
        public int TrustDelta;
        public int TrainConditionDelta;
        public int PassengerDelta;
        public string EventTitle;
        public string EventDescription;
        public bool GoalCompleted;
        public string Tone;
        public string TrustLine;        // 五线定性反馈
        public string FinanceLine;
        public string SandLine;
        public string PoliticalLine;    // 官方公文口径，无数值
        public string InfrastructureLine; // 定性等级词，无数值
    }

    public static DayReport LastReport { get; private set; }
    public static bool HasReport { get; private set; }

    /// <summary>结束当天：结算并生成日报，随后自动存档。</summary>
    public static DayReport EndDay()
    {
        int dayBefore = GameData.GetDay();
        GameData.DayResult result = GameData.AdvanceDay();

        var report = new DayReport
        {
            Day = dayBefore,
            MoneyDelta = GameData.DailyMoneyChange,
            MoneyAfter = GameData.GetMoney(),
            TrustDelta = GameData.DailyTrustChange,
            TrainConditionDelta = GameData.DailyTrainConditionChange,
            PassengerDelta = GameData.DailyPassengerChange,
            EventTitle = result.EventTitle,
            EventDescription = result.EventDescription,
            GoalCompleted = result.GoalCompleted,
            Tone = result.Tone,
            TrustLine = DescribeTrend(GameData.DailyTrustChange, "乡里反映渐好", "口碑基本持平", "有人对班次颇有微词"),
            FinanceLine = DescribeTrend(GameData.DailyMoneyChange, "账面有所盈余", "收支大致平衡", "账面出现亏空"),
            SandLine = DescribeSand(),
            PoliticalLine = DescribePolitical(),
            InfrastructureLine = DescribeInfrastructure(),
        };

        LastReport = report;
        HasReport = true;
        TryAutoSave();
        return report;
    }

    private static string DescribeTrend(int delta, string up, string flat, string down)
    {
        if (delta > 0) return up;
        if (delta < 0) return down;
        return flat;
    }

    private static string DescribePolitical()
    {
        // 只走离散档位名（公文口径），不暴露 PoliticalPressure 数值
        string tier = GameData.GetSanctionTierName();
        return string.IsNullOrEmpty(tier) ? "上级部门暂无新的通报" : tier;
    }

    private static string DescribeInfrastructure()
    {
        // 只走离散等级词，不暴露 InfrastructureDecay 数值
        string word = GameData.GetDecayGradeWord();
        return string.IsNullOrEmpty(word) ? "线路状况尚可" : word;
    }

    private static string DescribeSand()
    {
        // 取已通线路沿线渗透最高者，只给定性描述
        float top = 0f;
        var cities = WorldGen.RegionUnlockManager.GetUnlockedCityIds();
        if (cities != null)
        {
            foreach (string id in cities) top = Mathf.Max(top, SandRivalManager.GetPenetration(id));
        }
        return DescribeSandPenetration(top);
    }

    public static string DescribeSandPenetration(float p)
    {
        if (p < 0.2f) return "沙能公司在沿线影响甚微";
        if (p < 0.4f) return "沙能公司开始在沿线露面";
        if (p < 0.6f) return "沙能公司已分走部分客源";
        return "沙能公司在沿线声势很大，需认真应对";
    }

    private static void TryAutoSave()
    {
        try
        {
            VNSaveSystem.SaveGameplayData(AutoSaveSlot);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[DayLoopController] 自动存档失败：" + e.Message);
        }
    }

    // ===== 策略读取 / 切换（给 UI 复用）=====

    public static void CycleStrategy(int index)
    {
        switch (index)
        {
            case 0: GameData.CycleDispatchPlan(); break;
            case 1: GameData.CycleStaffAllocation(); break;
            case 2: GameData.CycleMaintenanceStrategy(); break;
            case 3: GameData.CycleStationServiceStrategy(); break;
            case 4: GameData.CycleExternalAffairsStrategy(); break;
            case 5: GameData.CycleStance(); break;
        }
    }

    // ----- 四项经营策略（1..4）的直接读写：UI 用分段控件，不再循环点击 -----
    public static readonly string[] StrategyTitles = { "人员分配", "检修策略", "站务服务", "对外事务" };

    public static int GetStrategyValue(int s)
    {
        switch (s)
        {
            case 0: return (int)GameData.CurrentStaffAllocation;
            case 1: return (int)GameData.CurrentMaintenanceStrategy;
            case 2: return (int)GameData.CurrentStationServiceStrategy;
            default: return (int)GameData.CurrentExternalAffairsStrategy;
        }
    }

    public static void SetStrategyValue(int s, int value)
    {
        int guard = 3;
        while (GetStrategyValue(s) != value && guard-- > 0) CycleStrategy(s + 1);
    }

    public static string[] StrategyOptions(int s)
    {
        var r = new string[3];
        for (int i = 0; i < 3; i++)
        {
            switch (s)
            {
                case 0: r[i] = GameData.GetStaffAllocationName((GameData.StaffAllocationType)i); break;
                case 1: r[i] = GameData.GetMaintenanceStrategyName((GameData.MaintenanceStrategy)i); break;
                case 2: r[i] = GameData.GetStationServiceStrategyName((GameData.StationServiceStrategy)i); break;
                default: r[i] = GameData.GetExternalAffairsStrategyName((GameData.ExternalAffairsStrategy)i); break;
            }
        }
        return r;
    }

    public static string StrategyDescription(int s, int value)
    {
        switch (s)
        {
            case 0: return GameData.GetStaffAllocationDescription((GameData.StaffAllocationType)value);
            case 1: return GameData.GetMaintenanceStrategyDescription((GameData.MaintenanceStrategy)value);
            case 2: return GameData.GetStationServiceStrategyDescription((GameData.StationServiceStrategy)value);
            default: return GameData.GetExternalAffairsStrategyDescription((GameData.ExternalAffairsStrategy)value);
        }
    }

    /// <summary>六项当日决策的 (标题, 当前值) 列表，供 UI 逐行绘制。</summary>
    public static List<KeyValuePair<string, string>> GetStrategyRows()
    {
        return new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("发车方案", GameData.GetCurrentDispatchPlan().Name),
            new KeyValuePair<string, string>("人员分配", GameData.GetStaffAllocationName(GameData.CurrentStaffAllocation)),
            new KeyValuePair<string, string>("检修策略", GameData.GetMaintenanceStrategyName(GameData.CurrentMaintenanceStrategy)),
            new KeyValuePair<string, string>("站务服务", GameData.GetStationServiceStrategyName(GameData.CurrentStationServiceStrategy)),
            new KeyValuePair<string, string>("对外事务", GameData.GetExternalAffairsStrategyName(GameData.CurrentExternalAffairsStrategy)),
        };
    }
}
