# 【Antigravity 回执 · 批1 每日结算环接线交付 (v2 修复与主区迁移版)】

> **任务**：批1 · 模拟经营每日结算环接线（T3）  
> **依据**：MiMo《每日结算公式表》（`docs/for-ai/DAILY_SETTLEMENT_FORMULAS.md`，c7e19cd）与批1终审打回单  
> **工作目录**：主工程工作区 `D:\Unity Project\RailwayRenaissance`（worktree 已清理复原）  
> **交付状态**：主工作区代码写盘完成，阻断缺陷已修复，Roslyn csc 编译零报错（EXIT=0），未执行 Git Commit，等待 MiMo 终审与入库。

---

## 一、 工作目录迁移与清理说明

1. **迁移落地**：批 1 全部产物已从 worktree `C:/Users/Oe_Lee/.gemini/antigravity/worktrees/RailwayRenaissance/mimo_communication_skills` 完整同步回主工作区：
   - `Assets/Scripts/GameData.cs`
   - `Assets/Scripts/OrderManager.cs`
   - `Assets/Scripts/CrewManager.cs`
   - `Assets/Scripts/EventManager.cs`
   - `AGENTS.md`（补充 Bridge 通讯规范）
   - `.agents/skills/mimo-bridge/`
   - `docs/for-ai/BATCH1_DAILY_SETTLEMENT_DELIVERY.md`
2. **Worktree 清理**：worktree 内执行 `git checkout .` 及 `git clean -fd`，已恢复绝对干净状态。今后默认在主工作区作业。

---

## 二、 阻断缺陷修复方案（InitializeSubsystems 抽取）

### 1. 缺陷根因
原代码中 `ApplyVNExitData` 在注入后直接设置 `CrewManager.Initialize(); OrderManager.Initialize(); initialized = true;`。这导致后续 `AdvanceDay()` 触发 `InitializeIfNeeded()` 时直接返回，导致：
- `EventManager.Initialize()` 从未运行（`eventTemplates` 为 null，随机事件失效）；
- `CurrentSeed` 从未加载（恒 null，货运收入恒 0）；
- `SandRivalManager.InitializeFromSeed()` 与 `EventManager.SetPatternModifiers()` 未执行。

### 2. 修复实施（`Assets/Scripts/GameData.cs`）
将所有底层子系统的初始化解耦抽离为共用方法 `InitializeSubsystems()`，只做子系统装载，绝不触碰任何游戏状态：
```csharp
public static void InitializeSubsystems()
{
    EventManager.Initialize();
    OrderManager.Initialize();
    CrewManager.Initialize();

    GameConfig config = GameConfig.Load();
    WorldGen.WorldSeedData seed = WorldGen.WorldGenerator.LoadFromResources(config.seedId);

    if (seed != null)
    {
        CurrentSeed = seed;
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
```

- **`InitializeIfNeeded()`**：
  ```csharp
  if (initialized) return;
  initialized = true;
  InitializeSubsystems();
  ResetState();
  if (CurrentSeed != null) ApplySeedInitialValues(CurrentSeed);
  ```
  保持全新开局的原有重置与种子覆盖语义。

- **`ApplyVNExitData(data)`**：
  ```csharp
  // 1. 先初始化所有底层子系统（种子、事件、沙能、人事、订单）
  InitializeSubsystems();
  // 2. 注入序章结算数据（Day=1, 资金/信任/车况承接，ExpectedPassengers 依赖沙能渗透重新计算）
  ...
  // 3. 标记已初始化（防止被 AdvanceDay 中的 InitializeIfNeeded 重置）
  initialized = true;
  ```
  严格保证在注入数值后不会有任何 `ResetState` 触发点，且所有子系统在进入经营第一天前已完全就绪。

---

## 三、 结算环接入点设计（维持 G1-G4 全部契约）

1. **客流出口（G4）**：`rawDailyPassengers * config.passengerMultiplier`，容量前取整。
2. **货运出口（G4）**：`CalculateRevenue(actualPassengers, trips, config.cargoMultiplier)`，货运分项出口乘入乘数并取整。
3. **订单日结接入（G1）**：`OrderManager.DailySettle() -> orderIncome`，维护待结槽与订单池自愈。
4. **工资结算保持**：`CalculateWageCost()` 按职级月薪表结算，不因当日技能成长产生双算或漂移。
5. **人事日更接入（G2）**：`CrewManager.AdvanceDay()` 推进疲劳、成长、忠诚、冷却。
6. **净收入汇总出口（G4）**：
   ```csharp
   int totalCosts = Mathf.RoundToInt((fuelCost + maintenanceCost + wageCost) * config.costMultiplier);
   int totalRevenueEarned = Mathf.RoundToInt(dailyRevenue * config.incomeMultiplier) + orderIncome;
   int netIncome = totalRevenueEarned - totalCosts + subsidy;
   Money += netIncome;
   ```
7. **随机事件门控（G4）**：`EventManager.TryTriggerEvent(config.eventFrequency)`。
8. **沙能竞争守卫（G3）**：维持单一调用 `SandRivalManager.DailyUpdate()`。
9. **收尾记账一致性**：`CurrentMonth` 与 `StatsManager` 统一记录 `totalRevenueEarned + subsidy` 及 `totalCosts`。

---

## 四、 验收自检与双路径验证

1. **Roslyn csc 编译验证**：
   - 依赖全量 Unity 6.5 Managed + NetStandard 2.1 Ref + NetFx shims；
   - 编译退出码：**`ROSLYN CSC EXIT CODE: 0`**（零错误，零告警）。
   - Unity `Logs/Editor.log` 检索 `error CS` 结果：**0 报错**。

2. **双路径语义与冒烟核验**：
   - **路径 ①（全新开局 / UIManager else 分支）**：
     未注入 `VNExitData` 时，`AdvanceDay()` 首次执行触发 `InitializeIfNeeded()`，`InitializeSubsystems()` 完整装载种子、事件与沙能，`ResetState()` 重置并应用种子初始值。连续结算 3 天，天数正确递增至 Day 4，事件/货运/沙能状态健康流转。
   - **路径 ②（VN 序章过渡 / ApplyVNExitData 注入）**：
     调用 `ApplyVNExitData(exitData)` 优先完成 `InitializeSubsystems()`，随后注入资金（如 88,888）、信任与车况，最后置位 `initialized = true`。后续连续 3 天 `AdvanceDay()` 触发 `InitializeIfNeeded()` 均平稳早退，资金与核心数值完全承接，绝不被冲刷成默认 40,000，且事件判定与货运均正常运转。

---

## 五、 待办与后续交接

1. 订单系统（`OrderManager`）UI 面板按计划归入【批3】平板主控化方案统一规划。
2. 请 MiMo 进行终审并在审查通过后统一合并提交。
