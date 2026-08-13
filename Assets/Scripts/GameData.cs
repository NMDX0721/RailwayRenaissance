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

    public static void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        ResetState();
    }

    public static void ResetState()
    {
        Day = 1;
        Money = 40000;
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

        foreach (ShortTermGoal goal in ShortTermGoals)
        {
            goal.IsCompleted = false;
        }

        AutoSelectGoal();
        RefreshBoards("新的一天开始了，先看一眼站内情况，再决定今天怎么经营。");
    }

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

    public static DayResult AdvanceDay()
    {
        InitializeIfNeeded();

        int startMoney = Money;
        int startTrust = Trust;
        int startCondition = TrainCondition;
        int startPassengers = ExpectedPassengers;

        DispatchPlan plan = GetCurrentDispatchPlan();

        ApplyDelta(plan.MoneyDelta + Random.Range(-40, 91), plan.TrustDelta + Random.Range(-2, 5), plan.TrainConditionDelta + Random.Range(-3, 3), plan.PassengerDelta + Random.Range(-2, 5));
        ApplyStaffAllocationEffects(CurrentStaffAllocation);
        ApplyMaintenanceEffects(CurrentMaintenanceStrategy);
        ApplyStationServiceEffects(CurrentStationServiceStrategy);
        ApplyExternalAffairsEffects(CurrentExternalAffairsStrategy);

        currentDailyEvent = TriggerDailyEvent();
        if (currentDailyEvent != null)
        {
            ApplyDelta(currentDailyEvent.MoneyDelta, currentDailyEvent.TrustDelta, currentDailyEvent.TrainConditionDelta, currentDailyEvent.PassengerDelta);
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

    private static DailyEvent TriggerDailyEvent()
    {
        if (Random.Range(0, 100) < 35)
        {
            return DailyEvents[Random.Range(0, DailyEvents.Length)];
        }

        return null;
    }

    private static void ApplyStaffAllocationEffects(StaffAllocationType type)
    {
        switch (type)
        {
            case StaffAllocationType.Balanced:
                ApplyDelta(0, 2, 2, 2);
                break;
            case StaffAllocationType.Maintenance:
                ApplyDelta(-50, 0, 8, -1);
                break;
            case StaffAllocationType.Service:
                ApplyDelta(-30, 5, -2, 5);
                break;
        }
    }

    private static void ApplyMaintenanceEffects(MaintenanceStrategy strategy)
    {
        switch (strategy)
        {
            case MaintenanceStrategy.Postpone:
                ApplyDelta(90, 0, -8, 0);
                break;
            case MaintenanceStrategy.Standard:
                ApplyDelta(-120, 0, 7, 0);
                break;
            case MaintenanceStrategy.Enhanced:
                ApplyDelta(-240, 1, 13, 1);
                break;
        }
    }

    private static void ApplyStationServiceEffects(StationServiceStrategy strategy)
    {
        switch (strategy)
        {
            case StationServiceStrategy.Basic:
                ApplyDelta(40, -2, 0, -2);
                break;
            case StationServiceStrategy.Standard:
                ApplyDelta(-40, 2, 0, 2);
                break;
            case StationServiceStrategy.Enhanced:
                ApplyDelta(-120, 4, 0, 4);
                break;
        }
    }

    private static void ApplyExternalAffairsEffects(ExternalAffairsStrategy strategy)
    {
        switch (strategy)
        {
            case ExternalAffairsStrategy.Minimal:
                ApplyDelta(80, -3, 0, 0);
                break;
            case ExternalAffairsStrategy.Standard:
                ApplyDelta(-80, 3, 0, 1);
                break;
            case ExternalAffairsStrategy.Active:
                ApplyDelta(-180, 6, 0, 2);
                break;
        }
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

        return "围绕“" + CurrentGoal.Title + "”调整今天的决策。";
    }
}
