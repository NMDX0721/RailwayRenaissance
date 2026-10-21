using System.Collections.Generic;
using UnityEngine;
using RailwayRenaissance.Core;

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

    public enum ManagementStance
    {
        PeopleFirst,
        ProfitFirst,
        Conservative
    }

    // ===== 经济系统参数 =====
    /// <summary>
    /// 客流基准人口（G4：从创世核主线城市population读取，无种子时回退8000）。
    /// </summary>
    private static float PopulationFactor
    {
        get
        {
            if (CurrentSeed != null)
            {
                // 找主线城市（unlockRegion==0 的第一个城市，如雾峰村8000）
                foreach (var kvp in CurrentSeed.cities)
                {
                    if (kvp.Value.unlockRegion == 0)
                        return kvp.Value.population;
                }
            }
            return 8000f;
        }
    }
    private const int ConductorLevel = 2;
    private const int MechanicLevel = 2;
    private const int TicketPrice = 35;
    private const float FuelConsumptionPer100km = 58.5f;
    private const float DailyDistancePerTrip = 23f;
    private const int FuelPrice = 15;
    private const int MonthlyWage = 90000;
    private const int BaseTrips = 4;

    // ===== G5：产业→货运吨位映射（用于货运收入计算） =====
    private static readonly Dictionary<string, int> IndustryTonnage = new Dictionary<string, int>
    {
        { "coal", 5 },
        { "iron", 3 },
        { "tea", 2 },
        { "machinery", 4 },
        { "shipping", 6 },
        { "steel", 5 },
        // 其他未列出的产业默认 1 吨/天
    };

    // ===== 车厢容量（A3） =====
    private const int CapacityPerCar = 30;
    private const float OvercrowdThreshold = 1.2f;

    // ===== 运行时状态 =====

    // ——— G4：创世核种子引用（由 WorldInitializer.ApplySeed 或 InitializeIfNeeded 赋值） ———
    public static WorldGen.WorldSeedData CurrentSeed;

    // ——— G6：依赖图中断城市集合（由 Layer6/事件系统写入） ———
    /// <summary>
    /// 记录当前线路中断的城市ID集合。
    /// 完整线路中断概念依赖铁路网络系统（未建），此为接口实现。
    /// 中断城市的产业产出在货运计算时 ×0.5。
    /// </summary>
    public static HashSet<string> DisruptedCityIds = new HashSet<string>();

    /// <summary>设置城市中断状态（供 Layer6 事件系统调用）。</summary>
    /// <param name="cityId">城市ID（如 "wufeng"）</param>
    /// <param name="disrupted">true=中断，false=恢复</param>
    public static void SetCityDisrupted(string cityId, bool disrupted)
    {
        if (disrupted)
            DisruptedCityIds.Add(cityId);
        else
            DisruptedCityIds.Remove(cityId);
    }

    public static int Day { get; private set; } = 1;
    public static int Money { get; private set; } = 40000;
    public static int Trust { get; private set; } = 62;
    public static int TrainCondition { get; private set; } = 70;
    public static int ExpectedPassengers { get; private set; } = 22;
    public static int CarCount { get; private set; } = 2;

    // ===== T304 P0 核心数值：政治压力 + 全线路网老化 =====
    public static float PoliticalPressure { get; private set; } = 0.20f;
    public static float InfrastructureDecay { get; private set; } = 0.30f;
    public static float DecayRoadbed { get; private set; } = 0.30f;
    public static float DecayBridge { get; private set; } = 0.25f;
    public static float DecaySignal { get; private set; } = 0.20f;

    public static float DailyPoliticalPressureChange { get; private set; }
    public static float DailyInfrastructureDecayChange { get; private set; }

    // ===== T308 P0 离散状态机：政治监管档位 + 工务检定评级 =====
    public static int PoliticalTier { get; set; } = 0; // 0: Ⅰ常态监管, 1: Ⅱ警示通报, 2: Ⅲ约谈整改, 3: Ⅳ挂牌督办, 4: Ⅴ停产停业整顿
    public static int DecayGrade { get; set; } = 0;    // 0: 状态合格, 1: 经常保养, 2: 临时补修, 3: 评定失格
    public static int DecayGradeRoadbed { get; set; } = 0; // 工务分项检定缓存：路基
    public static int DecayGradeBridge { get; set; } = 0;  // 工务分项检定缓存：桥隧
    public static int DecayGradeSignal { get; set; } = 0;  // 工务分项检定缓存：信号
    public static int BankruptcyCount { get => bankruptcyCount; set => bankruptcyCount = value; }

    // 连续赤字与反哺保护状态（MiMo 审批放行规范）
    private static int consecutiveDeficitDays = 0;
    private static float cumulativeDeficitPressure = 0f;

    // 环形历史快照缓冲（360 天日记录，供五线趋势看板消费）
    [System.Serializable]
    public struct TrendSnapshot
    {
        public int day;
        public float trust;
        public int money;
        public float sandPenetration;
        public float politicalPressure;
        public float infrastructureDecay;
    }

    public const int TrendHistoryCapacity = 360;
    private static TrendSnapshot[] trendHistory = new TrendSnapshot[TrendHistoryCapacity];
    private static int trendHistoryCount = 0;
    private static int trendHistoryHead = 0;

    public static int DailyMoneyChange { get; private set; }
    public static int DailyTrustChange { get; private set; }
    public static int DailyTrainConditionChange { get; private set; }
    public static int DailyPassengerChange { get; private set; }

    public static List<string> Briefing { get; private set; } = new List<string>();
    public static List<string> Notices { get; private set; } = new List<string>();
    public static List<string> Todos { get; private set; } = new List<string>();

    // ===== A6：月度财务闭环 =====
    public class MonthlySummary
    {
        public int TotalRevenue;
        public int TotalExpenses;
        public int NetProfit => TotalRevenue - TotalExpenses;
        public int DaysInMonth;
    }
    public static MonthlySummary CurrentMonth { get; private set; } = new MonthlySummary();
    public static List<string> MonthlyReports { get; private set; } = new List<string>();

    // ===== A7：破产保护 =====
    private static int bankruptcyCount = 0;

    public static int SelectedDispatchPlanIndex { get; private set; } = 1;
    public static StaffAllocationType CurrentStaffAllocation { get; private set; } = StaffAllocationType.Balanced;
    public static MaintenanceStrategy CurrentMaintenanceStrategy { get; private set; } = MaintenanceStrategy.Standard;
    public static StationServiceStrategy CurrentStationServiceStrategy { get; private set; } = StationServiceStrategy.Standard;
    public static ExternalAffairsStrategy CurrentExternalAffairsStrategy { get; private set; } = ExternalAffairsStrategy.Standard;
    public static ManagementStance CurrentStance { get; private set; } = ManagementStance.Conservative;

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
        new ShortTermGoal("拉高客流", "让预计日客流达到 80。", targetPassengers: 80)
    };

    // ===== 公共数值调整方法 =====

    public static void AddMoney(int amount)
    {
        Money += amount;
    }

    public static void AddTrust(int amount)
    {
        Trust += amount;
    }

    // ===== 车厢扩张（A3） =====

    private const int CarUpgradeCost = 100000;

    /// <summary>购买一节车厢，扩大容量。成功返回 true。</summary>
    public static bool TryUpgradeCapacity()
    {
        if (Money < CarUpgradeCost)
        {
            return false;
        }

        Money -= CarUpgradeCost;
        CarCount += 1;
        return true;
    }

    public static int GetCapacityUpgradeCost()
    {
        return CarUpgradeCost;
    }

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

    // ===== 存档/读档支持 =====

    public static int GetMoney() { return Money; }
    public static int GetTrust() { return Trust; }
    public static int GetTrainCondition() { return TrainCondition; }
    public static int GetExpectedPassengers() { return ExpectedPassengers; }
    public static int GetDay() { return Day; }
    public static float GetPoliticalPressure() { return PoliticalPressure; }
    public static float GetInfrastructureDecay() { return InfrastructureDecay; }

    /// <summary>获取当前监管制裁评级（按离散状态机 PoliticalTier 返回，禁止按连续量瞬变直切）。</summary>
    public static string GetSanctionTierName()
    {
        // 现实依据：交通运输部《交通运输安全生产警示约谈和挂牌督办办法》警示→约谈→挂牌督办，末档依《安全生产法》责令停产停业整顿
        switch (PoliticalTier)
        {
            case 0: return "Ⅰ 常态监管";
            case 1: return "Ⅱ 警示通报";
            case 2: return "Ⅲ 约谈整改";
            case 3: return "Ⅳ 挂牌督办";
            case 4: return "停产停业整顿";
            default: return "Ⅰ 常态监管";
        }
    }

    /// <summary>获取当前工务检定综合评级（按工务定期检定缓存档位返回，禁止实时翻牌）。</summary>
    public static string GetDecayGradeWord()
    {
        return RailwayRenaissance.Systems.RegulatoryEventSystem.GetDecayGradeWord(DecayGrade);
    }

    /// <summary>获取当前工务检定路基分项评级（按工务定期检定缓存档位返回，禁止实时翻牌）。</summary>
    public static string GetDecayGradeRoadbedWord()
    {
        return RailwayRenaissance.Systems.RegulatoryEventSystem.GetDecayGradeWord(DecayGradeRoadbed);
    }

    /// <summary>获取当前工务检定桥隧分项评级（按工务定期检定缓存档位返回，禁止实时翻牌）。</summary>
    public static string GetDecayGradeBridgeWord()
    {
        return RailwayRenaissance.Systems.RegulatoryEventSystem.GetDecayGradeWord(DecayGradeBridge);
    }

    /// <summary>获取当前工务检定信号分项评级（按工务定期检定缓存档位返回，禁止实时翻牌）。</summary>
    public static string GetDecayGradeSignalWord()
    {
        return RailwayRenaissance.Systems.RegulatoryEventSystem.GetDecayGradeWord(DecayGradeSignal);
    }

    /// <summary>记录每日五线趋势快照（360 天环形存储）。</summary>
    public static void RecordTrendSnapshot(int day, float trust, int money, float sand, float politics, float decay)
    {
        trendHistory[trendHistoryHead] = new TrendSnapshot
        {
            day = day,
            trust = trust,
            money = money,
            sandPenetration = sand,
            politicalPressure = politics,
            infrastructureDecay = decay
        };
        trendHistoryHead = (trendHistoryHead + 1) % TrendHistoryCapacity;
        if (trendHistoryCount < TrendHistoryCapacity)
            trendHistoryCount++;
    }

    /// <summary>获取全部有效趋势快照列表（时间正序）。</summary>
    public static List<TrendSnapshot> GetTrendHistory()
    {
        List<TrendSnapshot> list = new List<TrendSnapshot>(trendHistoryCount);
        int startIndex = (trendHistoryHead - trendHistoryCount + TrendHistoryCapacity) % TrendHistoryCapacity;
        for (int i = 0; i < trendHistoryCount; i++)
        {
            int idx = (startIndex + i) % TrendHistoryCapacity;
            list.Add(trendHistory[idx]);
        }
        return list;
    }

    public static void RestoreFromSave(GameDataSaveData data)
    {
        Money = data.money;
        Trust = data.trust;
        TrainCondition = data.trainCondition;
        ExpectedPassengers = data.expectedPassengers;
        Day = data.day;
        CarCount = data.carCount > 0 ? data.carCount : 2;

        // T304: 从本地持久化还原或安全默认值（老档向下兼容）
        PoliticalPressure = PlayerPrefs.GetFloat("GameData_PoliticalPressure", 0.20f);
        InfrastructureDecay = PlayerPrefs.GetFloat("GameData_InfrastructureDecay", 0.30f);
        DecayRoadbed = PlayerPrefs.GetFloat("GameData_DecayRoadbed", InfrastructureDecay);
        DecayBridge = PlayerPrefs.GetFloat("GameData_DecayBridge", Mathf.Max(0f, InfrastructureDecay - 0.05f));
        DecaySignal = PlayerPrefs.GetFloat("GameData_DecaySignal", Mathf.Max(0f, InfrastructureDecay - 0.10f));
        PoliticalTier = PlayerPrefs.GetInt("GameData_PoliticalTier", 0);
        DecayGrade = PlayerPrefs.GetInt("GameData_DecayGrade", 0);
        DecayGradeRoadbed = PlayerPrefs.GetInt("GameData_DecayGradeRoadbed", DecayGrade);
        DecayGradeBridge = PlayerPrefs.GetInt("GameData_DecayGradeBridge", DecayGrade);
        DecayGradeSignal = PlayerPrefs.GetInt("GameData_DecayGradeSignal", DecayGrade);
        RailwayRenaissance.Systems.RegulatoryEventSystem.Initialize();
    }

    // ===== 种子数据注入 =====

    /// <summary>从种子覆盖初始趋势值（信任、车况等），替代硬编码。</summary>
    public static void ApplySeedInitialValues(WorldGen.WorldSeedData seed)
    {
        if (seed == null) return;

        Trust = Mathf.RoundToInt(seed.initialTrends.trust * 100f);
        TrainCondition = 70 - Mathf.RoundToInt(seed.initialTrends.infrastructureDecay * 50f);
        TrainCondition = Mathf.Clamp(TrainCondition, 0, 100);

        // T304: 对齐创世核世界种子初值
        PoliticalPressure = Mathf.Clamp01(seed.initialTrends.politicalPressure);
        InfrastructureDecay = Mathf.Clamp01(seed.initialTrends.infrastructureDecay);
        DecayRoadbed = InfrastructureDecay;
        DecayBridge = Mathf.Clamp01(InfrastructureDecay * 0.9f);
        DecaySignal = Mathf.Clamp01(InfrastructureDecay * 0.8f);

        // ExpectedPassengers 保持由 A9 公式计算，但 Trust 已变，需重新计算
        ExpectedPassengers = Mathf.RoundToInt(CalculateDailyPassengers() * GetTripsForDispatchPlan());
    }

    // ===== 初始化 =====

    /// <summary>
    /// 初始化所有底层子系统（事件/订单/员工/沙能/种子配置），不修改任何运行时玩法数值。
    /// </summary>
    public static void InitializeSubsystems()
    {
        EventManager.Initialize();
        OrderManager.Initialize();
        CrewManager.Initialize();

        // 从 GameConfig 读取种子 ID，加载种子数据
        GameConfig config = GameConfig.Load();
        WorldGen.WorldSeedData seed = WorldGen.WorldGenerator.LoadFromResources(config.seedId);

        if (seed != null)
        {
            // G4：保存种子引用供经济核读取
            CurrentSeed = seed;

            // G9：资源分布 pattern → 事件概率修正（需在 EventManager.Initialize 之后）
            EventManager.SetPatternModifiers(seed);

            SandRivalManager.InitializeFromSeed(seed);
        }
        else
        {
            CurrentSeed = null;
            EventManager.SetPatternModifiers(null);
            SandRivalManager.Initialize();
        }
    }

    public static void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        InitializeSubsystems();
        ResetState();

        if (CurrentSeed != null)
        {
            ApplySeedInitialValues(CurrentSeed);
        }
    }

    public static void ResetState()
    {
        // 从 GameConfig 读取初始资金
        GameConfig config = GameConfig.Load();
        Day = 1;
        Money = Mathf.RoundToInt(config.startMoney);
        Trust = 62;
        TrainCondition = 70;
        ExpectedPassengers = Mathf.RoundToInt(CalculateDailyPassengers() * GetTripsForDispatchPlan());
        CarCount = 2;

        PoliticalPressure = 0.20f;
        PoliticalTier = 0;
        InfrastructureDecay = 0.30f;
        DecayGrade = 0;
        DecayRoadbed = 0.30f;
        DecayBridge = 0.25f;
        DecaySignal = 0.20f;
        DailyPoliticalPressureChange = 0f;
        DailyInfrastructureDecayChange = 0f;
        consecutiveDeficitDays = 0;
        cumulativeDeficitPressure = 0f;
        trendHistoryCount = 0;
        trendHistoryHead = 0;
        RailwayRenaissance.Systems.RegulatoryEventSystem.Initialize();

        // 记录 Day 1 初始快照
        RecordTrendSnapshot(1, Trust, Money, 0.15f, PoliticalPressure, InfrastructureDecay);

        DailyMoneyChange = 0;
        DailyTrustChange = 0;
        DailyTrainConditionChange = 0;
        DailyPassengerChange = 0;

        SelectedDispatchPlanIndex = 1;
        CurrentStaffAllocation = StaffAllocationType.Balanced;
        CurrentMaintenanceStrategy = MaintenanceStrategy.Standard;
        CurrentStationServiceStrategy = StationServiceStrategy.Standard;
        CurrentExternalAffairsStrategy = ExternalAffairsStrategy.Standard;
        CurrentStance = ManagementStance.Conservative;
        currentDailyEvent = null;
        completedStoryGrants.Clear();

        foreach (ShortTermGoal goal in ShortTermGoals)
        {
            goal.IsCompleted = false;
        }

        AutoSelectGoal();
        RefreshBoards("新的一天开始了，先看一眼站内情况，再决定今天怎么经营。");
    }

    /// <summary>
    /// 从序章过渡数据 (VNExitData) 注入经营状态，实现无缝承接。
    /// </summary>
    public static void ApplyVNExitData(VNExitData data)
    {
        // 1. 先初始化所有底层子系统（加载种子、沙能、事件等）
        InitializeSubsystems();

        // 2. 注入序章结算数据
        Day = 1;
        Money = Mathf.RoundToInt(data.startMoney > 0 ? data.startMoney : 40000f);
        Trust = data.startTrust > 0 ? data.startTrust : 60;
        TrainCondition = data.startTrainCondition > 0 ? data.startTrainCondition : 70;
        ExpectedPassengers = Mathf.RoundToInt(CalculateDailyPassengers() * GetTripsForDispatchPlan());
        CarCount = 2;

        DailyMoneyChange = 0;
        DailyTrustChange = 0;
        DailyTrainConditionChange = 0;
        DailyPassengerChange = 0;

        SelectedDispatchPlanIndex = 1;
        CurrentStaffAllocation = StaffAllocationType.Balanced;
        CurrentMaintenanceStrategy = MaintenanceStrategy.Standard;
        CurrentStationServiceStrategy = StationServiceStrategy.Standard;
        CurrentExternalAffairsStrategy = ExternalAffairsStrategy.Standard;
        CurrentStance = ManagementStance.Conservative;
        currentDailyEvent = null;
        completedStoryGrants.Clear();

        foreach (ShortTermGoal goal in ShortTermGoals)
        {
            goal.IsCompleted = false;
        }

        AutoSelectGoal();
        RefreshBoards("欢迎接手雾峰站。序章已结，老陈和大家都在等你做今天的排班决策。");

        // 3. 标记已初始化（防止被 AdvanceDay 中的 InitializeIfNeeded 覆盖或重置）
        initialized = true;
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

    public static void CycleStance()
    {
        CurrentStance = (ManagementStance)(((int)CurrentStance + 1) % 3);
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

    /// <summary>根据天数计算季节系数（A1：春采茶/夏旅游/秋货运/冬淡季）。</summary>
    private static float GetSeasonModifier()
    {
        // 每月30天，一年12个月（360天制）
        int month = ((Day - 1) % 360) / 30;
        switch (month)
        {
            case 2:  // 3月
            case 3:  // 4月
            case 4:  // 5月：春·采茶季
                return 1.15f;
            case 5:  // 6月
            case 6:  // 7月
            case 7:  // 8月：夏·旅游
                return 1.05f;
            case 8:  // 9月
            case 9:  // 10月
            case 10: // 11月：秋·货运旺季
                return 1.20f;
            default: // 12月/1月/2月：冬·淡季
                return 0.90f;
        }
    }

    /// <summary>根据天数计算季节应力系数（T304：雨季与冻融加剧路基桥梁损耗）。</summary>
    private static float GetSeasonStressModifier()
    {
        int month = ((Day - 1) % 360) / 30;
        switch (month)
        {
            case 2:
            case 3:
            case 4:  // 春雨翻浆
                return 1.15f;
            case 5:
            case 6:
            case 7:  // 夏季高温
                return 1.05f;
            case 8:
            case 9:
            case 10: // 秋收干爽
                return 0.95f;
            default: // 冬季冻融
                return 1.25f;
        }
    }

    /// <summary>获取主线城市沙能渗透率（A2：从铁龙竞争系统读取，替代写死值）。</summary>
    private static float GetSandPenetration()
    {
        // 雾峰村-矿区线的主线城市
        return SandRivalManager.GetPenetration("wufeng");
    }

    /// <summary>计算今日最大可承运客流（A3：车厢容量约束）。</summary>
    private static int GetDailyCapacity(int trips)
    {
        return CarCount * CapacityPerCar * trips;
    }

    /// <summary>计算每日客流（A4 & T308 §8.3：平滑连续化信任系数分段）。</summary>
    /// <remarks>
    /// [T308 §8.3 根治突变]：
    /// 原突变：T=29 为 0.395，T=30 为 0.690（跳跃 +75%）；T=69 为 1.002，T=70 为 0.850（跳变 -15%）。
    /// 平滑后：在 27~33 与 67~73 设平滑插值过渡带，消除断崖跳变，全区间严格单调连续。
    /// </remarks>
    private static int CalculateDailyPassengers()
    {
        float trustCoefficient = CalculateTrustCoefficient(Trust);
        float serviceQualityCoefficient = 1.0f + ConductorLevel * 0.03f;
        float raw = PopulationFactor * 0.0025f * trustCoefficient * GetSeasonModifier() * (1f - GetSandPenetration()) * serviceQualityCoefficient;
        return Mathf.RoundToInt(raw);
    }

    /// <summary>
    /// [T308 §8.3] 计算平滑信任客流系数（在 27~33 与 67~73 设过渡插值带，左右极限完全相等）。
    /// </summary>
    public static float CalculateTrustCoefficient(int trust)
    {
        float t = Mathf.Clamp(trust, 0, 100);
        float f1(float v) => 0.25f + v * 0.005f;        // 雪崩区基准
        float f2(float v) => 0.45f + v * 0.008f;        // 线性区基准
        float f3(float v) => 0.85f + (v - 70f) * 0.003f;// 饱和区基准

        if (t <= 27f)
            return f1(t);
        if (t < 33f)
        {
            float alpha = (t - 27f) / 6f;
            return Mathf.Lerp(f1(27f), f2(33f), alpha);
        }
        if (t <= 67f)
            return f2(t);
        if (t < 73f)
        {
            float beta = (t - 67f) / 6f;
            return Mathf.Lerp(f2(67f), f3(73f), beta);
        }
        return f3(t);
    }

    /// <summary>计算日收入（G5：客运 + 货运），货运出口支持难度乘数 cargoMultiplier（G4）。</summary>
    private static int CalculateRevenue(int dailyPassengers, int trips, float cargoMultiplier = 1.0f)
    {
        int passengerRevenue = dailyPassengers * TicketPrice * trips;
        int freightRevenue = Mathf.RoundToInt(CalculateFreightRevenue() * cargoMultiplier);
        return passengerRevenue + freightRevenue;
    }

    private static int CalculateRevenue(int dailyPassengers, int trips)
    {
        return CalculateRevenue(dailyPassengers, trips, 1.0f);
    }

    /// <summary>获取当前线网货运日总重载吨位（供线路老化动力学计算）。</summary>
    public static float GetDailyFreightTonnage()
    {
        if (CurrentSeed == null) return 5f;

        float totalFreightTonnage = 0f;
        var unlockedCityIds = WorldGen.RegionUnlockManager.GetUnlockedCityIds();

        foreach (string cityId in unlockedCityIds)
        {
            if (!CurrentSeed.cities.TryGetValue(cityId, out var city))
                continue;

            float cityOutput = 0f;
            foreach (string industry in city.industries)
            {
                float tonnage = IndustryTonnage.TryGetValue(industry, out int t) ? t : 1f;
                cityOutput += tonnage;
            }

            if (DisruptedCityIds.Contains(cityId))
            {
                cityOutput *= 0.5f;
            }

            totalFreightTonnage += cityOutput;
        }

        return totalFreightTonnage > 0f ? totalFreightTonnage : 5f;
    }

    /// <summary>计算货运收入（G5：基于已解锁城市产业 × 23km × 0.5沙币/吨公里）。</summary>
    /// <remarks>
    /// 遍历已解锁城市，对其 industries 求和吨位，再乘以统一定价。
    /// G6：若城市在 DisruptedCityIds 中，产业产出 ×0.5。
    /// 无种子时返回 0（货运系统未激活）。
    /// </remarks>
    private static int CalculateFreightRevenue()
    {
        if (CurrentSeed == null) return 0;
        return Mathf.RoundToInt(GetDailyFreightTonnage() * 23f * 0.5f);
    }

    /// <summary>计算日燃料费。</summary>
    private static int CalculateFuelCost(int trips)
    {
        float vehicleConditionCoefficient = 1f + (100f - TrainCondition) / 200f;
        float dailyDistance = DailyDistancePerTrip * trips;
        float dailyFuelLiters = FuelConsumptionPer100km * dailyDistance / 100f * vehicleConditionCoefficient;
        return Mathf.RoundToInt(dailyFuelLiters * FuelPrice);
    }

    /// <summary>计算日工资（A5：动态化，按员工职级月薪求和/30）。</summary>
    /// <remarks>
    /// 映射 role → 核心技能名：driver→driving, mechanic→repair,
    /// dispatcher→management, conductor→service, attendant→service。
    /// 取该技能 level 查月薪表（《先民人事系统.md》§七工资标准）：
    ///   驾驶(driving):    [18000, 22000, 28000, 35000, 45000]
    ///   维修(repair):     [14000, 17000, 22000, 28000, 35000]
    ///   管理(management):  [12000, 15000, 18000, 22000, 28000]
    ///   服务(service):     [10000, 12000, 14000, 17000, 22000]
    /// 若 CrewManager 未初始化/无员工，回退固定 MonthlyWage / 30。
    /// </remarks>
    private static int CalculateWageCost()
    {
        var allCrew = CrewManager.GetAllCrew();
        if (allCrew == null || allCrew.Count == 0)
            return MonthlyWage / 30; // 回退固定值

        int totalMonthly = 0;
        foreach (var member in allCrew)
        {
            // role → 核心技能名
            string skillName;
            switch (member.role)
            {
                case "driver":     skillName = "driving";     break;
                case "mechanic":   skillName = "repair";      break;
                case "dispatcher": skillName = "management";  break;
                case "conductor":
                case "attendant":  skillName = "service";     break;
                default:           skillName = "service";     break;
            }

            // 找该技能
            int level = 0;
            foreach (var s in member.skills)
            {
                if (s.skillName == skillName)
                {
                    level = s.level;
                    break;
                }
            }

            // 月薪表（level 1-5，索引减1）
            int monthly;
            switch (skillName)
            {
                case "driving":
                    monthly = level >= 1 && level <= 5
                        ? (new int[] { 18000, 22000, 28000, 35000, 45000 })[level - 1]
                        : 18000;
                    break;
                case "repair":
                    monthly = level >= 1 && level <= 5
                        ? (new int[] { 14000, 17000, 22000, 28000, 35000 })[level - 1]
                        : 14000;
                    break;
                case "management":
                    monthly = level >= 1 && level <= 5
                        ? (new int[] { 12000, 15000, 18000, 22000, 28000 })[level - 1]
                        : 12000;
                    break;
                default: // service
                    monthly = level >= 1 && level <= 5
                        ? (new int[] { 10000, 12000, 14000, 17000, 22000 })[level - 1]
                        : 10000;
                    break;
            }
            totalMonthly += monthly;
        }

        return Mathf.RoundToInt(totalMonthly / 30f);
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

    // ===== Task 8: 经营姿态联动 =====

    private static FluctuationEngine stanceEngine;
    private static bool stanceInitialized = false;

    private static void EnsureStanceEngine()
    {
        if (stanceInitialized) return;
        var rules = new GlobalRules();
        rules.fluctuationWeightsList.Add(new GlobalRules.WeightTable
        {
            formulaName = "stance",
            weights = new float[] { 1.0f }
        });
        stanceEngine = new FluctuationEngine(rules, 42, 1.0f);
        stanceInitialized = true;
    }

    /// <summary>
    /// 根据当前经营姿态返回指定技能系统的效率修正倍率。
    /// PeopleFirst：站务技能 +20%，驾驶技能 -10%
    /// ProfitFirst：驾驶技能 +20%，站务技能 -10%
    /// Conservative：无修正
    /// 修正值通过 FluctuationEngine.Compound() 计算，避免硬编码 20%。
    /// </summary>
    /// <param name="skillSystem">技能系统标识（如 "driving", "service", "management", "freight", "station_skills", "driving_skills"）</param>
    /// <returns>效率修正倍率（1.0 = 无修正）</returns>
    public static float GetStanceModifier(string skillSystem)
    {
        EnsureStanceEngine();

        bool isStationSkill = skillSystem == "station_skills" || skillSystem == "service" || skillSystem == "management";
        bool isDrivingSkill = skillSystem == "driving_skills" || skillSystem == "driving" || skillSystem == "freight";

        float baseModifier = 0f;
        switch (CurrentStance)
        {
            case ManagementStance.PeopleFirst:
                if (isStationSkill) baseModifier = 0.20f;
                else if (isDrivingSkill) baseModifier = -0.10f;
                break;
            case ManagementStance.ProfitFirst:
                if (isDrivingSkill) baseModifier = 0.20f;
                else if (isStationSkill) baseModifier = -0.10f;
                break;
            case ManagementStance.Conservative:
                return 1.0f;
        }

        if (baseModifier == 0f) return 1.0f;

        // 通过 FluctuationEngine.Compound() 计算实际修正值，不硬编码 20%
        float stanceStrength = Mathf.Abs(baseModifier) / 0.20f;
        WeightedFactor[] factors = new WeightedFactor[]
        {
            new WeightedFactor("baseModifier", baseModifier),
            new WeightedFactor("stanceStrength", stanceStrength)
        };
        float actualModifier = stanceEngine.Compound(baseModifier, factors, "stance", stanceStrength);
        return 1.0f + actualModifier;
    }

    // ===== 核心流程 =====

    public static DayResult AdvanceDay()
    {
        InitializeIfNeeded();

        int startMoney = Money;
        int startTrust = Trust;
        int startCondition = TrainCondition;
        int startPassengers = ExpectedPassengers;

        // 加载难度与游戏配置（G4）
        GameConfig config = GameConfig.Load();

        // 1. 发车方案决定今日趟数（受政治压力挂牌督办制约，改读离散状态机 PoliticalTier，禁数值直切）
        int trips = GetTripsForDispatchPlan();
        if (PoliticalTier >= 3)
        {
            trips = Mathf.Max(1, trips - 1);
            Notices.Add("⚠ 重大隐患挂牌督办期间：安全督查要求减班限速，今日最高发车趟数上限 -1。");
        }
        if (PoliticalTier >= 4)
        {
            trips = 0;
            Notices.Add("⛔ 停产停业整顿期间：根据应急与交通部门决定，全线暂停旅客列车发车。");
        }

        // 2. 计算单趟客流（A9：统一单趟口径），按 passengerMultiplier 接入（G4）
        // T304：路网老化导致颠簸与小幅晚点，折减客流吸引力（对齐《跨系统联动公式》§12）
        float punctualityFactor = Mathf.Clamp01(1.0f - InfrastructureDecay * 0.18f);
        int rawDailyPassengers = Mathf.RoundToInt(CalculateDailyPassengers() * punctualityFactor);
        int dailyPassengers = Mathf.RoundToInt(rawDailyPassengers * config.passengerMultiplier);
        // 日客流 = 单趟 × 趟数（用于显示/目标）
        ExpectedPassengers = dailyPassengers * trips;

        // 2.5 容量约束（A9 O9）：实际客流按单趟比较
        int perTripCapacity = CarCount * CapacityPerCar;
        int actualPassengers = Mathf.Min(dailyPassengers, perTripCapacity);
        bool overcrowded = dailyPassengers > Mathf.RoundToInt(perTripCapacity * OvercrowdThreshold);

        // 3-5. 日收入（客运 + 货运×cargoMultiplier 难度乘数 G4）
        int dailyRevenue = CalculateRevenue(actualPassengers, trips, config.cargoMultiplier);

        // 5.5 [新] 订单系统每日结算（G1）
        int orderIncome = OrderManager.DailySettle();

        // 6. 日燃料费（T304：线路轨道老化导致轮轨动阻力增加，百公里油耗额外增加，对齐《跨系统联动公式》§12）
        float decayDragMultiplier = 1.0f + InfrastructureDecay * 0.25f;
        int fuelCost = Mathf.RoundToInt(CalculateFuelCost(trips) * decayDragMultiplier);

        // 7. 日工资（按当前职级月薪表结算）
        int wageCost = CalculateWageCost();

        // 8. 日维护费
        int maintenanceCost = CalculateMaintenanceCost();

        // 9. 基础补贴（A8）：8000/30 × subsidyMultiplier，计入净收入
        // T304 & T308：受政治压力离散监管档位影响（对齐《跨系统联动公式》§11.3 & §11.4，禁数值直切）
        int rawSubsidy = Mathf.RoundToInt(8000f / 30f * config.subsidyMultiplier);
        int subsidy = rawSubsidy;
        if (PoliticalTier == 0)
        {
            subsidy = Mathf.RoundToInt(rawSubsidy * 1.10f); // Ⅰ 常态监管：安全生产考核优良奖励 +10%
        }
        else if (PoliticalTier == 1)
        {
            subsidy = rawSubsidy; // Ⅱ 警示通报：维持基准，无核减无奖励
        }
        else if (PoliticalTier == 2)
        {
            subsidy = Mathf.RoundToInt(rawSubsidy * 0.75f); // Ⅲ 约谈整改：核减 25%
            Notices.Add("⚠ 市政府约谈整改：责令限期提交整改方案，地方财政补贴核减 25%。");
        }
        else if (PoliticalTier == 3)
        {
            subsidy = Mathf.RoundToInt(rawSubsidy * 0.50f); // Ⅳ 挂牌督办：核减 50%
            Notices.Add("⚠ 重大隐患挂牌督办：减班限速运行，地方财政补贴核减 50%。");
        }
        else if (PoliticalTier >= 4)
        {
            subsidy = 0; // Ⅴ 停产停业整顿：停发补贴
            Notices.Add("⛔ 停产停业整顿：根据整顿决定，全线停发政府运营财政补贴。");
        }

        // 9.5 [新] 先民人事系统每日更新（G2：疲劳/成长/忠诚/冷却，不改当日工资）
        CrewManager.AdvanceDay();

        // 10. 日净收入 = (日收入 × incomeMultiplier + 订单收入) - (燃料费 + 维护费 + 工资) × costMultiplier + 补贴
        int totalCosts = Mathf.RoundToInt((fuelCost + maintenanceCost + wageCost) * config.costMultiplier);
        int totalRevenueEarned = Mathf.RoundToInt(dailyRevenue * config.incomeMultiplier) + orderIncome;
        int netIncome = totalRevenueEarned - totalCosts + subsidy;
        Money += netIncome;

        // 11. 信任变化 = 正常运营+1/天
        int trustDelta = 1;

        // 12. 车况变化 = 维护策略决定（已包含基础日损耗-1）
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

        // 拥挤负反馈（A3）：需求超容量120%时，乘客体验下降
        if (overcrowded)
        {
            trustDelta -= 1;
            passengerDelta -= 1;
        }

        // 应用所有delta
        Trust += trustDelta;
        TrainCondition += conditionDelta;
        ExpectedPassengers += passengerDelta;

        // ===== 12.5 全线路网老化物理演化（v3.0 威布尔危险率物理损伤模型） =====
        float startDecay = InfrastructureDecay;
        float D_base = 0.0018f;
        float beta = 1.85f;
        float kappa = 1.25f;
        float D_0 = 0.30f;
        float tripsRatio = (float)trips / (float)BaseTrips;
        float cargoTonnage = GetDailyFreightTonnage();
        float loadFactor = 1.0f + (cargoTonnage / 50.0f) * 0.25f;
        float seasonStress = GetSeasonStressModifier();
        float noiseEnv = Random.Range(-0.0001f, 0.0001f);

        // 威布尔疲劳磨耗增量
        float wear = D_base * (1.0f + kappa * Mathf.Pow(Mathf.Clamp01(InfrastructureDecay) / D_0, beta - 1.0f)) * tripsRatio * loadFactor * seasonStress + noiseEnv;
        // 工务队伍日常巡线与主动维护
        float maintenancePower = CrewManager.GetMaintenancePower();
        float stanceEffort = CurrentMaintenanceStrategy == MaintenanceStrategy.Enhanced ? 1.8f : (CurrentMaintenanceStrategy == MaintenanceStrategy.Standard ? 1.0f : 0f);
        float repair = maintenancePower * stanceEffort;
        float netDecayDelta = wear - repair;

        DecayRoadbed = Mathf.Clamp01(DecayRoadbed + netDecayDelta * 1.1f);
        DecayBridge = Mathf.Clamp01(DecayBridge + netDecayDelta * 0.9f);
        DecaySignal = Mathf.Clamp01(DecaySignal + netDecayDelta * 1.0f);
        InfrastructureDecay = Mathf.Clamp01(0.40f * DecayRoadbed + 0.35f * DecayBridge + 0.25f * DecaySignal);
        DailyInfrastructureDecayChange = InfrastructureDecay - startDecay;

        // ===== 12.6 政治商业周期演化（v3.0 Nordhaus 180天宏观波形 + 前景理论非对称损失） =====
        float startPressure = PoliticalPressure;
        int cycleLen = CurrentSeed != null ? CurrentSeed.politicalCycleLength : 180;
        float stability = CurrentSeed != null ? CurrentSeed.stability : 0.6f;
        // 宏观大选/审核大势微分增量
        float macroCycleDelta = (0.12f * (1f - stability)) * (2f * Mathf.PI / cycleLen) * Mathf.Cos(2f * Mathf.PI * Day / cycleLen);

        float externalImpact = 0f;
        if (CurrentExternalAffairsStrategy == ExternalAffairsStrategy.Minimal) externalImpact += 0.003f;
        else if (CurrentExternalAffairsStrategy == ExternalAffairsStrategy.Active) externalImpact -= 0.004f;

        // 沙能资本游说侵蚀：沙能渗透率 > 50% 时，放大负面外力 30%
        float currentSandPen = SandRivalManager.GetPenetration("wufeng");
        if (currentSandPen > 0.50f && externalImpact > 0f) externalImpact *= 1.30f;

        float microDelta = 0f;
        if (externalImpact > 0f)
        {
            // 负面危机：损失厌恶 lambda_L = 2.25 放大
            microDelta = externalImpact * 2.25f * (1f - stability * 0.4f);
        }
        else if (externalImpact < 0f)
        {
            // 正面公关：缓慢修复
            microDelta = externalImpact * (1f / 2.25f) * (1f + stability * 0.4f);
        }

        float microNoise = Random.Range(-0.0005f, 0.0005f);
        PoliticalPressure = Mathf.Clamp01(PoliticalPressure + macroCycleDelta + microDelta + microNoise);
        DailyPoliticalPressureChange = PoliticalPressure - startPressure;

        // 13. 随机事件（按 eventFrequency 概率门控 G4）
        GameEvent evt = EventManager.TryTriggerEvent(config.eventFrequency);
        if (evt != null)
        {
            Money += evt.effects.moneyDelta;
            Trust += evt.effects.trustDelta;
            TrainCondition += evt.effects.trainConditionDelta;
            ExpectedPassengers += evt.effects.passengerDelta;
        }

        // 14. 沙能竞争系统（G3：保持单一调用，勿重复！）
        SandRivalManager.DailyUpdate();
        SandAction sandAction = SandRivalManager.CheckForAction();
        if (sandAction != null)
        {
            Trust += sandAction.trustDelta;
            // 应用渗透率变化
            SandRivalManager.SetPenetration(sandAction.cityId,
                SandRivalManager.GetPenetration(sandAction.cityId) + sandAction.penetrationDelta);

            // N4.6（GAP6 修复）：铁龙行动 → 叙事事件（sand_action_*）
            Narrative.NarrativeEngine.OnSandRivalAction(sandAction.type);
        }

        ClampStats();

        // 15. 收尾：月度累计（收入记 totalRevenueEarned + subsidy，支出记 totalCosts，与净收入口径完全一致）
        CurrentMonth.TotalRevenue += totalRevenueEarned + subsidy;
        CurrentMonth.TotalExpenses += totalCosts;
        CurrentMonth.DaysInMonth++;

        // 站长日志统计：记账
        StatsManager.AddDailyRevenue(totalRevenueEarned, subsidy);
        StatsManager.AddDailyExpense(totalCosts);
        StatsManager.AddDailyPassengers(actualPassengers * trips);
        StatsManager.RecordDay(Day);

        // 赤字反哺政治压力（带双重防死亡螺旋约束：最多连续10天后半衰，单周期累计封顶+0.08，只改连续量温度计禁直切档位）
        if (Money < 5000 && netIncome < 0)
        {
            consecutiveDeficitDays++;
            if (consecutiveDeficitDays <= 10 && cumulativeDeficitPressure < 0.08f)
            {
                float addPressure = Mathf.Min(0.004f * 2.25f, 0.08f - cumulativeDeficitPressure);
                cumulativeDeficitPressure += addPressure;
                PoliticalPressure = Mathf.Clamp01(PoliticalPressure + addPressure);
                Notices.Add("⚠ 市议会质询：财政赤字持续扩大，地方监管部门已列入关注清单。");
            }
            else if (consecutiveDeficitDays > 10 && cumulativeDeficitPressure < 0.08f)
            {
                float damping = Mathf.Pow(0.5f, consecutiveDeficitDays - 10);
                float addPressure = Mathf.Min(0.004f * 2.25f * damping, 0.08f - cumulativeDeficitPressure);
                cumulativeDeficitPressure += addPressure;
                PoliticalPressure = Mathf.Clamp01(PoliticalPressure + addPressure);
            }
        }
        else if (netIncome >= 0)
        {
            consecutiveDeficitDays = 0;
            cumulativeDeficitPressure = Mathf.Max(0f, cumulativeDeficitPressure - 0.005f);
        }

        // 记录 360 天环形趋势快照
        RecordTrendSnapshot(Day, Trust, Money, SandRivalManager.GetPenetration("wufeng"), PoliticalPressure, InfrastructureDecay);

        // 本地落盘向下兼容持久化
        PlayerPrefs.SetFloat("GameData_PoliticalPressure", PoliticalPressure);
        PlayerPrefs.SetFloat("GameData_InfrastructureDecay", InfrastructureDecay);
        PlayerPrefs.SetFloat("GameData_DecayRoadbed", DecayRoadbed);
        PlayerPrefs.SetFloat("GameData_DecayBridge", DecayBridge);
        PlayerPrefs.SetFloat("GameData_DecaySignal", DecaySignal);
        PlayerPrefs.SetInt("GameData_PoliticalTier", PoliticalTier);
        PlayerPrefs.SetInt("GameData_DecayGrade", DecayGrade);
        PlayerPrefs.SetInt("GameData_DecayGradeRoadbed", DecayGradeRoadbed);
        PlayerPrefs.SetInt("GameData_DecayGradeBridge", DecayGradeBridge);
        PlayerPrefs.SetInt("GameData_DecayGradeSignal", DecayGradeSignal);

        // 成就：完成第一个月运营
        if (CurrentMonth.DaysInMonth >= 30)
            AchievementManager.Unlock("railway_newbie");

        // 财政压力：累计月亏损 > 50,000 时输出警告
        if (CurrentMonth.NetProfit < -50000)
        {
            Notices.Add("⚠ 财政压力：本月已累计亏损 " + (-CurrentMonth.NetProfit) + " 沙币");
        }

        // 月报：Day % 30 == 0 时生成（注意 Day 尚未 +1，当前 Day 是月末日）
        if (Day % 30 == 0)
        {
            string report = "=== 第 " + (Day / 30) + " 月财务报告 ==="
                + "\n  收入: " + CurrentMonth.TotalRevenue
                + "\n  支出: " + CurrentMonth.TotalExpenses
                + "\n  利润: " + CurrentMonth.NetProfit
                + "\n  运营天数: " + CurrentMonth.DaysInMonth;
            MonthlyReports.Add(report);
            Notices.Add("📊 " + report.Replace("\n", " | "));

            // 成就：首次月度盈利 10 万
            if (CurrentMonth.NetProfit >= 100000)
                AchievementManager.Unlock("revivalist");

            // 重置月度累计
            CurrentMonth = new MonthlySummary();
        }

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

        // 区域解锁系统：每日检查，条件满足时自动解锁区域
        WorldGen.RegionUnlockManager.DailyCheck();

        // 教程系统：更新天数并检查提示
        TutorialManager.SetGameDay(Day);
        string tip = TutorialManager.CheckForTip();
        if (!string.IsNullOrEmpty(tip))
        {
            TutorialManager.ShowTip(tip);
        }

        RefreshBoards(tone);

        // [T308/T312] 监管法定程序推进与破产保护检查：必须在 RefreshBoards 之后调用，防止被 RefreshBoards 重建 Notices 清空覆盖
        RailwayRenaissance.Systems.RegulatoryEventSystem.AdvanceDay(Notices);
        CheckBankruptcy();

        // 叙事引擎检查（N4.8）：Day 已推进（沙能竞争之后），检查并挂载叙事事件。
        // 注意：必须在 RefreshBoards 之后调用——RefreshBoards 以 new List 重建 Briefing/Notices，
        // 若在此之前 DispatchAll，叙事文本会被板面重建覆盖无法显示（GAP8：文本进简报/公告列表供 UIManager 显示）。
        var narrativeEvents = Narrative.NarrativeEngine.CheckForEvent();
        Narrative.NarrativeEngine.DispatchAll(narrativeEvents);

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

    /// <summary>A7：破产保护检查。在 ClampStats 后调用。</summary>
    /// <remarks>
    /// 首次资金≤0：注入 10,000（老陈紧急资金）
    /// 二次资金≤0：注入 5,000（政府救助）
    /// 三次资金≤0：输出"破产"警告（不强制结束游戏，留给上层处理）
    /// 与 TutorialManager.CheckBankruptcyProtection 共存，经济核作为更完整的替代。
    /// </remarks>
    /// <summary>
    /// [T308 §8.1] 资金链断裂救助改走法定破产重整程序链（现实依据：《企业破产法》第七十条）。
    /// 严禁资金归零当日立即加钱，必须经法院受理、指定清算组后多日分期注资。
    /// </summary>
    private static void CheckBankruptcy()
    {
        if (Money > 0)
            return;

        RailwayRenaissance.Systems.RegulatoryEventSystem.TriggerBankruptcyProcedure(Notices);
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

    // 音量设置
    public static float MasterVolume { get => PlayerPrefs.GetFloat("MasterVolume", 1f); set { PlayerPrefs.SetFloat("MasterVolume", value); ApplyVolume(); } }
    public static float BGMVolume { get => PlayerPrefs.GetFloat("BGMVolume", 0.8f); set { PlayerPrefs.SetFloat("BGMVolume", value); ApplyVolume(); } }
    public static float SFXVolume { get => PlayerPrefs.GetFloat("SFXVolume", 1f); set { PlayerPrefs.SetFloat("SFXVolume", value); ApplyVolume(); } }
    public static float TypewriterVolume { get => PlayerPrefs.GetFloat("TypewriterVolume", 0.12f); set { PlayerPrefs.SetFloat("TypewriterVolume", value); ApplyVolume(); } }

    public static void ApplyVolume()
    {
        var bgmObj = GameObject.Find("BGM");
        if (bgmObj != null)
        {
            var src = bgmObj.GetComponent<AudioSource>();
            if (src != null) src.volume = 0.3f * MasterVolume * BGMVolume;
        }
        if (VNAudioManager.Instance != null)
            VNAudioManager.Instance.ApplyVolumeSettings(MasterVolume, BGMVolume, SFXVolume);
    }
}