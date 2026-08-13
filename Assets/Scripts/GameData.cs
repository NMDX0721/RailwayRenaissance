using System.Collections.Generic;
using UnityEngine;

public static class GameData
{
    public class DispatchPlan
    {
        public string Name;
        public string Summary;
        public int MoneyDelta;
        public int TrustDelta;
        public int TrainConditionDelta;
        public int PassengerDelta;

        public DispatchPlan(string name, string summary, int moneyDelta, int trustDelta, int trainConditionDelta, int passengerDelta)
        {
            Name = name;
            Summary = summary;
            MoneyDelta = moneyDelta;
            TrustDelta = trustDelta;
            TrainConditionDelta = trainConditionDelta;
            PassengerDelta = passengerDelta;
        }
    }

    public class DailyEvent
    {
        public string Title;
        public string Description;
        public int MoneyDelta;
        public int TrustDelta;
        public int TrainConditionDelta;
        public int PassengerDelta;

        public DailyEvent(string title, string description, int moneyDelta, int trustDelta, int trainConditionDelta, int passengerDelta)
        {
            Title = title;
            Description = description;
            MoneyDelta = moneyDelta;
            TrustDelta = trustDelta;
            TrainConditionDelta = trainConditionDelta;
            PassengerDelta = passengerDelta;
        }
    }

    public class ShortTermGoal
    {
        public string Title;
        public string Description;
        public int TargetMoney;
        public int TargetTrust;
        public int TargetPassengers;
        public int TargetCondition;
        public bool IsCompleted;

        public ShortTermGoal(string title, string description, int targetMoney = 0, int targetTrust = 0, int targetPassengers = 0, int targetCondition = 0)
        {
            Title = title;
            Description = description;
            TargetMoney = targetMoney;
            TargetTrust = targetTrust;
            TargetPassengers = targetPassengers;
            TargetCondition = targetCondition;
        }

        public bool CheckCompletion()
        {
            bool moneyMet = TargetMoney <= 0 || Money >= TargetMoney;
            bool trustMet = TargetTrust <= 0 || Trust >= TargetTrust;
            bool passengersMet = TargetPassengers <= 0 || ExpectedPassengers >= TargetPassengers;
            bool conditionMet = TargetCondition <= 0 || TrainCondition >= TargetCondition;

            IsCompleted = moneyMet && trustMet && passengersMet && conditionMet;
            return IsCompleted;
        }

        public string GetProgressDescription()
        {
            List<string> segments = new List<string>();

            if (TargetMoney > 0)
            {
                segments.Add("资金 " + Money + "/" + TargetMoney);
            }
            if (TargetTrust > 0)
            {
                segments.Add("信任 " + Trust + "/" + TargetTrust);
            }
            if (TargetPassengers > 0)
            {
                segments.Add("客流 " + ExpectedPassengers + "/" + TargetPassengers);
            }
            if (TargetCondition > 0)
            {
                segments.Add("车况 " + TrainCondition + "/" + TargetCondition);
            }

            return string.Join("  ", segments);
        }
    }

    public struct DayResult
    {
        public string EventTitle;
        public string EventDescription;
        public bool GoalCompleted;
        public string Tone;
    }

    public enum StaffAllocationType
    {
        Balanced,
        Maintenance,
        Service
    }

    public enum MaintenanceStrategy
    {
        Postpone,
        Standard,
        Enhanced
    }

    public enum StationServiceStrategy
    {
        Basic,
        Standard,
        Enhanced
    }

    public enum ExternalAffairsStrategy
    {
        Minimal,
        Standard,
        Active
    }

    // ===== 经济系统参数 =====
    private const float PopulationFactor = 8000f;
    private const float SeasonModifier = 1.0f;
    private const float SandPenetration = 0.15f;
    private const int ConductorLevel = 2;
    private const int MechanicLevel = 2;
    private const int TicketPrice = 35;
    private const float FuelConsumptionPer100km = 58.5f;
    private const float DailyDistancePerTrip = 23f;
    private const int FuelPrice = 15;
    private const int MonthlyWage = 90000;
    private const int BaseTrips = 4;

    // ===== 运行时状态 =====
    public static int Day { get; private set; } = 1;
    public static int Money { get; private set; } = 40000;
    public static int Trust { get; private set; } = 62;
    public static int TrainCondition { get; private set; } = 70;
    public static int ExpectedPassengers { get; private set; } = 22;

    public static int DailyMoneyChange { get; private set; }
    public static int DailyTrustChange { get; private set; }
    public static int DailyTrainConditionChange { get; private set; }
    public static int DailyPassengerChange { get; private set; }

    public static List<string> Briefing { get; private set; } = new List<string>();
    public static List<string> Notices { get; private set; } = new List<string>();
    public static List<string> Todos { get; private set; } = new List<string>();

    public static int SelectedDispatchPlanIndex { get; private set; } = 1;
    public static StaffAllocationType CurrentStaffAllocation { get; private set; } = StaffAllocationType.Balanced;
    public static MaintenanceStrategy CurrentMaintenanceStrategy { get; private set; } = MaintenanceStrategy.Standard;
    public static StationServiceStrategy CurrentStationServiceStrategy { get; private set; } = StationServiceStrategy.Standard;
    public static ExternalAffairsStrategy CurrentExternalAffairsStrategy { get; private set; } = ExternalAffairsStrategy.Standard;

    public static ShortTermGoal CurrentGoal { get; private set; }

    private static bool initialized;
    private static DailyEvent currentDailyEvent;
    private static HashSet<string> completedStoryGrants = new HashSet<string>();

    public static readonly DispatchPlan[] DispatchPlans =
    {
        new DispatchPlan("保守运行", "减少一班次，优先稳住车况和口碑。", -40, 3, 5, -2),
        new DispatchPlan("标准运行", "按当前能力维持常规运营。", 70, 0, -2, 2),
        new DispatchPlan("加开一班", "趁客流上涨多跑一趟，争取更高收入。", 160, -2, -6, 8)
    };

    private static readonly DailyEvent[] DailyEvents =
    {
        new DailyEvent("旅客致谢", "值班人员帮助旅客找回了遗失物，站内口碑提升。", 0, 5, 0, 2),
        new DailyEvent("售票机故障", "老设备临时出问题，现场处理花掉了一笔维修费。", -120, -2, 0, -1),
        new DailyEvent("地方报道", "本地媒体提到你们坚持运营支线，乘客对车站更有好感。", 0, 6, 0, 3),
        new DailyEvent("配件短缺", "今天等到的零件不够，检修效果打了折扣。", 0, 0, -5, 0),
        new DailyEvent("志愿者活动", "社区志愿者来帮忙维持秩序，服务体验有所改善。", 40, 3, 0, 2)
    };

    private static readonly ShortTermGoal[] ShortTermGoals =
    {
        new ShortTermGoal("稳定现金流", "把资金积累到 2600，给后续扩建留出空间。", targetMoney: 2600),
        new ShortTermGoal("修复口碑", "把乘客信任提升到 75。", targetTrust: 75),
        new ShortTermGoal("恢复车况", "把车况拉回到 82 以上。", targetCondition: 82),
        new ShortTermGoal("拉高客流", "让预计客流达到 30。", targetPassengers: 30)
    };

    // ===== 剧情补贴系统 =====

    public static void CompleteStoryGrant(string grantId, int amount)
    {
        Money += amount;
        completedStoryGrants.Add(grantId);
    }

    public static bool IsStoryGrantCompleted(string grantId)
    {
        return completedStoryGrants.Contains(grantId);
    }

    // ===== 初始化 =====

    public static void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        EventManager.Initialize();
        ResetState();
    }

    public static void ResetState()
    {
        // 从 GameConfig 读取初始资金
        GameConfig config = GameConfig.Load();
        Day = 1;
        Money = Mathf.RoundToInt(config.startMoney);
        Trust = 62;
        TrainCondition = 70;
        ExpectedPassengers = 22;

        DailyMoneyChange = 0;
        DailyTrustChange = 0;
        DailyTrainConditionChange = 0;
        DailyPassengerChange = 0;

        SelectedDispatchPlanIndex = 1;
        CurrentStaffAllocation = StaffAllocationType.Balanced;
        CurrentMaintenanceStrategy = MaintenanceStrategy.Standard;
        CurrentStationServiceStrategy = StationServiceStrategy.Standard;
        CurrentExternalAffairsStrategy = ExternalAffairsStrategy.Standard;
        currentDailyEvent = null;
        completedStoryGrants.Clear();

        foreach (ShortTermGoal goal in ShortTermGoals)
        {
            goal.IsCompleted = false;
        }

        AutoSelectGoal();
        RefreshBoards("新的一天开始了，先看一眼站内情况，再决定今天怎么经营。");
    }

    // ===== 策略循环 =====

    public static DispatchPlan GetCurrentDispatchPlan()
    {
        return DispatchPlans[SelectedDispatchPlanIndex];
    }

    public static void CycleDispatchPlan()
    {
        SelectedDispatchPlanIndex = (SelectedDispatchPlanIndex + 1) % DispatchPlans.Length;
    }

    public static void CycleStaffAllocation()
    {
        CurrentStaffAllocation = (StaffAllocationType)(((int)CurrentStaffAllocation + 1) % 3);
    }

    public static void CycleMaintenanceStrategy()
    {
        CurrentMaintenanceStrategy = (MaintenanceStrategy)(((int)CurrentMaintenanceStrategy + 1) % 3);
    }

    public static void CycleStationServiceStrategy()
    {
        CurrentStationServiceStrategy = (StationServiceStrategy)(((int)CurrentStationServiceStrategy + 1) % 3);
    }

    public static void CycleExternalAffairsStrategy()
    {
        CurrentExternalAffairsStrategy = (ExternalAffairsStrategy)(((int)CurrentExternalAffairsStrategy + 1) % 3);
    }

    // ===== 目标系统 =====

    public static void AutoSelectGoal()
    {
        foreach (ShortTermGoal goal in ShortTermGoals)
        {
            if (!goal.IsCompleted)
            {
                CurrentGoal = goal;
                return;
            }
        }

        CurrentGoal = ShortTermGoals[0];
    }

    public static bool CheckAndUpdateGoalStatus()
    {
        if (CurrentGoal == null)
        {
            return false;
        }

        return CurrentGoal.CheckCompletion();
    }

    // ===== 核心经济计算 =====

    /// <summary>根据当前发车方案获取今日趟数。</summary>
    private static int GetTripsForDispatchPlan()
    {
        // 保守运行=3趟，标准运行=4趟，加开一班=5趟
        switch (SelectedDispatchPlanIndex)
        {
            case 0: return 3;
            case 2: return 5;
            default: return 4;
        }
    }

    /// <summary>计算每日客流。</summary>
    private static int CalculateDailyPassengers()
    {
        float trustCoefficient = 0.5f + Trust * 0.005f;
        float serviceQualityCoefficient = 1.0f + ConductorLevel * 0.03f;
        float raw = PopulationFactor * 0.001f * trustCoefficient * SeasonModifier * (1f - SandPenetration) * serviceQualityCoefficient;
        return Mathf.RoundToInt(raw);
    }

    /// <summary>计算日收入。</summary>
    private static int CalculateRevenue(int dailyPassengers, int trips)
    {
        return dailyPassengers * TicketPrice * trips;
    }

    /// <summary>计算日燃料费。</summary>
    private static int CalculateFuelCost(int trips)
    {
        float vehicleConditionCoefficient = 1f + (100f - TrainCondition) / 200f;
        float dailyDistance = DailyDistancePerTrip * trips;
        float dailyFuelLiters = FuelConsumptionPer100km * dailyDistance / 100f * vehicleConditionCoefficient;
        return Mathf.RoundToInt(dailyFuelLiters * FuelPrice);
    }

    /// <summary>计算日工资。</summary>
    private static int CalculateWageCost()
    {
        return MonthlyWage / 30;
    }

    /// <summary>根据维护策略计算日维护费。</summary>
    private static int CalculateMaintenanceCost()
    {
        float vehicleConditionCoefficient = 1f + (100f - TrainCondition) / 100f;
        float mechanicSkillCoefficient = 1f - MechanicLevel * 0.08f;

        float baseCost;
        switch (CurrentMaintenanceStrategy)
        {
            case MaintenanceStrategy.Postpone:
                baseCost = 0f;
                break;
            case MaintenanceStrategy.Standard:
                baseCost = 100f;
                break;
            case MaintenanceStrategy.Enhanced:
                baseCost = 500f;
                break;
            default:
                baseCost = 100f;
                break;
        }

        return Mathf.RoundToInt(baseCost * vehicleConditionCoefficient * mechanicSkillCoefficient);
    }

    /// <summary>根据维护策略计算车况日变化量。</summary>
    private static int GetConditionChangeFromMaintenance()
    {
        // 基础日损耗-1，维护策略影响：
        // Postpone: 无维护，额外-2 → 总计-3
        // Standard: 维护抵消部分损耗 → 总计0
        // Enhanced: 强化维护，车况回升 → 总计+2
        switch (CurrentMaintenanceStrategy)
        {
            case MaintenanceStrategy.Postpone:
                return -3;
            case MaintenanceStrategy.Standard:
                return 0;
            case MaintenanceStrategy.Enhanced:
                return 2;
            default:
                return -1;
        }
    }

    /// <summary>根据员工分配计算信任/车况/客流修正。</summary>
    private static (int trustDelta, int conditionDelta, int passengerDelta) GetStaffAllocationDeltas(StaffAllocationType type)
    {
        switch (type)
        {
            case StaffAllocationType.Balanced:
                return (0, 0, 0);
            case StaffAllocationType.Maintenance:
                return (0, 3, 0);
            case StaffAllocationType.Service:
                return (2, -1, 2);
            default:
                return (0, 0, 0);
        }
    }

    /// <summary>根据站务服务策略计算信任/客流修正。</summary>
    private static (int trustDelta, int passengerDelta) GetStationServiceDeltas(StationServiceStrategy strategy)
    {
        switch (strategy)
        {
            case StationServiceStrategy.Basic:
                return (-2, -2);
            case StationServiceStrategy.Standard:
                return (0, 0);
            case StationServiceStrategy.Enhanced:
                return (3, 3);
            default:
                return (0, 0);
        }
    }

    /// <summary>根据外部事务策略计算信任修正。</summary>
    private static int GetExternalAffairsTrustDelta(ExternalAffairsStrategy strategy)
    {
        switch (strategy)
        {
            case ExternalAffairsStrategy.Minimal:
                return -2;
            case ExternalAffairsStrategy.Standard:
                return 0;
            case ExternalAffairsStrategy.Active:
                return 3;
            default:
                return 0;
        }
    }

    // ===== 核心流程 =====

    public static DayResult AdvanceDay()
    {
        InitializeIfNeeded();

        int startMoney = Money;
        int startTrust = Trust;
        int startCondition = TrainCondition;
        int startPassengers = ExpectedPassengers;

        // 1. 发车方案决定今日趟数
        int trips = GetTripsForDispatchPlan();

        // 2. 计算每日客流
        int dailyPassengers = CalculateDailyPassengers();
        ExpectedPassengers = dailyPassengers;

        // 3. 日收入
        int dailyRevenue = CalculateRevenue(dailyPassengers, trips);

        // 4. 日燃料费
        int fuelCost = CalculateFuelCost(trips);

        // 5. 日工资
        int wageCost = CalculateWageCost();

        // 6. 日维护费
        int maintenanceCost = CalculateMaintenanceCost();

        // 7. 日净收入 = 收入 - 燃料费 - 维护费 - 工资
        int netIncome = dailyRevenue - fuelCost - maintenanceCost - wageCost;
        Money += netIncome;

        // 8. 信任变化 = 正常运营+1/天
        int trustDelta = 1;

        // 9. 车况变化 = 维护策略决定（已包含基础日损耗-1）
        int conditionDelta = GetConditionChangeFromMaintenance();

        // 应用员工分配影响
        var staffDeltas = GetStaffAllocationDeltas(CurrentStaffAllocation);
        trustDelta += staffDeltas.trustDelta;
        conditionDelta += staffDeltas.conditionDelta;
        int passengerDelta = staffDeltas.passengerDelta;

        // 应用站务服务影响
        var serviceDeltas = GetStationServiceDeltas(CurrentStationServiceStrategy);
        trustDelta += serviceDeltas.trustDelta;
        passengerDelta += serviceDeltas.passengerDelta;

        // 应用外部事务影响
        trustDelta += GetExternalAffairsTrustDelta(CurrentExternalAffairsStrategy);

        // 应用所有delta
        Trust += trustDelta;
        TrainCondition += conditionDelta;
        ExpectedPassengers += passengerDelta;

        // 随机事件
        GameEvent evt = EventManager.TryTriggerEvent();
        if (evt != null)
        {
            Money += evt.effects.moneyDelta;
            Trust += evt.effects.trustDelta;
            TrainCondition += evt.effects.trainConditionDelta;
            ExpectedPassengers += evt.effects.passengerDelta;
        }

        ClampStats();

        DailyMoneyChange = Money - startMoney;
        DailyTrustChange = Trust - startTrust;
        DailyTrainConditionChange = TrainCondition - startCondition;
        DailyPassengerChange = ExpectedPassengers - startPassengers;

        bool goalCompleted = CheckAndUpdateGoalStatus();
        if (goalCompleted)
        {
            AutoSelectGoal();
        }

        string tone = BuildDailyTone();
        Day += 1;
        RefreshBoards(tone);

        return new DayResult
        {
            EventTitle = currentDailyEvent != null ? currentDailyEvent.Title : "无突发事件",
            EventDescription = currentDailyEvent != null ? currentDailyEvent.Description : "今天整体按计划推进，没有额外插曲。",
            GoalCompleted = goalCompleted,
            Tone = tone
        };
    }

    // ===== 策略名称/描述（保持UI绑定） =====

    public static string GetStaffAllocationName(StaffAllocationType type)
    {
        switch (type)
        {
            case StaffAllocationType.Balanced:
                return "均衡排班";
            case StaffAllocationType.Maintenance:
                return "偏重检修";
            default:
                return "偏重服务";
        }
    }

    public static string GetStaffAllocationDescription(StaffAllocationType type)
    {
        switch (type)
        {
            case StaffAllocationType.Balanced:
                return "站务、检修、客运都照顾一点，整体最稳。";
            case StaffAllocationType.Maintenance:
                return "把更多人手压到检修上，今天会更像是在养车。";
            default:
                return "让更多人去照顾乘客，适合冲口碑和客流。";
        }
    }

    public static string GetMaintenanceStrategyName(MaintenanceStrategy strategy)
    {
        switch (strategy)
        {
            case MaintenanceStrategy.Postpone:
                return "延后检修";
            case MaintenanceStrategy.Standard:
                return "标准整备";
            default:
                return "强化检修";
        }
    }

    public static string GetMaintenanceStrategyDescription(MaintenanceStrategy strategy)
    {
        switch (strategy)
        {
            case MaintenanceStrategy.Postpone:
                return "先省钱，拿今天的运营去换明天的风险。";
            case MaintenanceStrategy.Standard:
                return "按部就班，花得起也修得住。";
            default:
                return "重投入保车况，适合连日高压后回血。";
        }
    }

    public static string GetStationServiceStrategyName(StationServiceStrategy strategy)
    {
        switch (strategy)
        {
            case StationServiceStrategy.Basic:
                return "基础服务";
            case StationServiceStrategy.Standard:
                return "标准服务";
            default:
                return "强化服务";
        }
    }

    public static string GetStationServiceStrategyDescription(StationServiceStrategy strategy)
    {
        switch (strategy)
        {
            case StationServiceStrategy.Basic:
                return "只保住基本秩序，能省一点是一点。";
            case StationServiceStrategy.Standard:
                return "维持常规服务品质，尽量不掉链子。";
            default:
                return "优先照顾候车体验，争取让乘客愿意再来。";
        }
    }

    public static string GetExternalAffairsStrategyName(ExternalAffairsStrategy strategy)
    {
        switch (strategy)
        {
            case ExternalAffairsStrategy.Minimal:
                return "最低投入";
            case ExternalAffairsStrategy.Standard:
                return "常规沟通";
            default:
                return "积极争取";
        }
    }

    public static string GetExternalAffairsStrategyDescription(ExternalAffairsStrategy strategy)
    {
        switch (strategy)
        {
            case ExternalAffairsStrategy.Minimal:
                return "少跑关系，先把钱留在站里。";
            case ExternalAffairsStrategy.Standard:
                return "和地方、供应商维持正常来往。";
            default:
                return "主动去争取支持和资源，但要多花钱。";
        }
    }

    public static string FormatSigned(int value, string suffix)
    {
        if (value > 0)
        {
            return "+" + value + suffix;
        }
        if (value < 0)
        {
            return value + suffix;
        }
        return "0" + suffix;
    }

    // ===== 内部方法 =====

    private static DailyEvent TriggerDailyEvent()
    {
        if (Random.Range(0, 100) < 35)
        {
            return DailyEvents[Random.Range(0, DailyEvents.Length)];
        }

        return null;
    }

    private static void ApplyDelta(int moneyDelta, int trustDelta, int conditionDelta, int passengerDelta)
    {
        Money += moneyDelta;
        Trust += trustDelta;
        TrainCondition += conditionDelta;
        ExpectedPassengers += passengerDelta;
    }

    private static void ClampStats()
    {
        Money = Mathf.Max(0, Money);
        Trust = Mathf.Clamp(Trust, 0, 100);
        TrainCondition = Mathf.Clamp(TrainCondition, 0, 100);
        ExpectedPassengers = Mathf.Max(0, ExpectedPassengers);
    }

    private static void RefreshBoards(string tone)
    {
        Briefing = new List<string>
        {
            "第 " + Day + " 天运营简报",
            "发车方案：" + GetCurrentDispatchPlan().Name,
            "人员安排：" + GetStaffAllocationName(CurrentStaffAllocation),
            "检修策略：" + GetMaintenanceStrategyName(CurrentMaintenanceStrategy),
            "站务服务：" + GetStationServiceStrategyName(CurrentStationServiceStrategy),
            "外部事务：" + GetExternalAffairsStrategyName(CurrentExternalAffairsStrategy)
        };

        Notices = new List<string>
        {
            "口碑状态：" + GetTrustLevel(),
            "车况状态：" + GetConditionLevel(),
            "客流判断：" + GetPassengerLevel(),
            "今日评语：" + tone
        };

        if (currentDailyEvent != null)
        {
            Notices.Add("突发事件：" + currentDailyEvent.Title + " - " + currentDailyEvent.Description);
        }

        Todos = new List<string>
        {
            BuildPrimaryTodo(),
            BuildSecondaryTodo(),
            "多点几次同一个按钮可以轮换今天的方案。",
            "确认满意后，再按一次“End Day”推进到下一天。"
        };

        if (CurrentGoal != null)
        {
            Todos.Insert(0, "当前目标：" + CurrentGoal.Title);
            Todos.Insert(1, "目标进度：" + CurrentGoal.GetProgressDescription());
        }
    }

    private static string BuildDailyTone()
    {
        if (DailyMoneyChange >= 0 && DailyTrustChange >= 0)
        {
            return "今天运营算稳，资金和口碑都在往上走。";
        }
        if (DailyMoneyChange >= 0 && DailyTrustChange < 0)
        {
            return "今天赚到了钱，但乘客感受有点被透支。";
        }
        if (DailyMoneyChange < 0 && DailyTrustChange >= 0)
        {
            return "今天像是在用资金换稳定，口碑保住了。";
        }
        return "今天过得有点吃力，明天需要更谨慎地排方案。";
    }

    private static string GetTrustLevel()
    {
        if (Trust >= 80)
        {
            return "旅客很愿意继续支持这条线。";
        }
        if (Trust >= 60)
        {
            return "站内评价尚可，但还谈不上安心。";
        }
        if (Trust >= 40)
        {
            return "抱怨开始变多，再失误就会掉口碑。";
        }
        return "旅客明显不放心，服务和准点都得尽快稳住。";
    }

    private static string GetConditionLevel()
    {
        if (TrainCondition >= 80)
        {
            return "机车状态可靠，短期内还能继续扛。";
        }
        if (TrainCondition >= 60)
        {
            return "还能跑，但已经不能再长期拖检修。";
        }
        if (TrainCondition >= 40)
        {
            return "车况偏差，任何冒进方案都会放大风险。";
        }
        return "机车状态危险，再硬撑很容易出事。";
    }

    private static string GetPassengerLevel()
    {
        if (ExpectedPassengers >= 32)
        {
            return "站里已经开始热闹起来，可以考虑冲收入。";
        }
        if (ExpectedPassengers >= 24)
        {
            return "客流温和增长，适合稳扎稳打。";
        }
        return "客流还是偏低，需要更主动地争取乘客。";
    }

    private static string BuildPrimaryTodo()
    {
        if (TrainCondition < 50)
        {
            return "优先把检修拉上来，不然下一天的波动会很难受。";
        }
        if (Trust < 55)
        {
            return "服务和对外沟通都别太省，先把信任拉回安全线。";
        }
        if (Money < 1500)
        {
            return "现金有压力，今天尽量不要把所有方案都选成高投入。";
        }
        return "可以围绕当前目标做一次更有针对性的组合。";
    }

    private static string BuildSecondaryTodo()
    {
        if (CurrentGoal == null)
        {
            return "先试一轮不同组合，找出自己喜欢的经营节奏。";
        }

        return "围绕\"" + CurrentGoal.Title + "\"调整今天的决策。";
    }
}