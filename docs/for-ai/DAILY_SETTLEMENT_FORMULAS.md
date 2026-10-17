# 每日结算公式表（T3 · AdvanceDay 重构输入）

> **编制**：MiMo Desktop（经营数值策划）
> **面向**：Antigravity（T3 沙本位经济核心重构 / 批1 结算环接线的实现依据）
> **日期**：2026-10-07
> **代码基准**：`Assets/Scripts/GameData.cs`（AdvanceDay @ L898-1044）、`GameConfig.cs`
> **设计基准**：`参考资料/设定/沙本位经济核.md`
> **原则**：本表「现状公式」= 已验证在跑的代码事实；「T3 改造」= 本次要做的增量。改造不得改变已验收的数值手感，除非本表明确标注。

---

## 一、现状：AdvanceDay 主循环公式（代码事实）

结算顺序即为执行顺序，改造后保持此顺序。

| 步 | 项 | 公式 | 代码位置 |
|---|---|---|---|
| 1 | 趟数 trips | 发车方案：保守=3 / 标准=4 / 加开=5 | `GetTripsForDispatchPlan` L539 |
| 2 | 单趟客流 p | `round( PopulationFactor × 0.0025 × 信任系数 × 季节系数 × (1−沙能渗透) × 服务质量系数 )` | `CalculateDailyPassengers` L594 |
| 2a | 信任系数 T | T<30: `0.25+T×0.005`；30≤T<70: `0.45+T×0.008`；T≥70: `0.85+(T−70)×0.003` | 同上 |
| 2b | 季节系数 | 3-5月 1.15（采茶）/ 6-8月 1.05（旅游）/ 9-11月 1.20（货运）/ 12-2月 0.90（淡季）；月= `((Day−1)%360)/30` | `GetSeasonModifier` L551 |
| 2c | 服务质量系数 | `1 + ConductorLevel(=2) × 0.03` → 1.06 | L603 |
| 3 | 容量约束 | 单趟容量 `= CarCount × CapacityPerCar(30)`；实际单趟客流 `min(p, 容量)`；超员阈值 `p > 容量×1.2` | L916-918 |
| 4 | 客运收入 | `实际单趟客流 × TicketPrice(35) × trips` | `CalculateRevenue` L609 |
| 5 | 货运收入 | `Σ(已解锁城市产业吨位 × [中断城市×0.5]) × 23km × 0.5沙币/吨公里`；无种子=0 | `CalculateFreightRevenue` L622 |
| 6 | 燃料费 | `58.5L/100km × (23×trips) /100 × (1+(100−车况)/200) × 15沙币/L` | `CalculateFuelCost` L658 |
| 7 | 工资 | 有员工：`Σ(各员工核心技能月薪表[等级]) / 30`；无员工回退 `90000/30=3000` | `CalculateWageCost` L677 |
| 8 | 维护费 | `基础(延期0/标准100/强化500) × (1+(100−车况)/100) × (1−MechanicLevel(=2)×0.08)` | `CalculateMaintenanceCost` L741 |
| 9 | 补贴 | `round(8000/30 × subsidyMultiplier)` ≈ 267×倍率 | L934 |
| 10 | 净收入 | `收入 − 燃料 − 维护 − 工资 + 补贴` → 直接 `Money +=` | L937-938 |
| 11 | 信任 Δ | 基础+1；员工分配（维护+0/服务+2）；站务（基础−2/标准0/强化+3）；外事务（最小−2/标准0/积极+3）；超员−1 | L941-965 |
| 12 | 车况 Δ | 维护策略：延期−3 / 标准0 / 强化+2；员工分配（维护+3/服务−1） | `GetConditionChangeFromMaintenance` L767、L787 |
| 13 | 随机事件 | `EventManager.TryTriggerEvent()` → money/trust/condition/passenger 增量 | L973-980 |
| 14 | 沙能竞争 | `SandRivalManager.DailyUpdate()` → `CheckForAction()` → 信任Δ+渗透Δ+叙事钩子 `OnSandRivalAction` | L982-996 |
| 15 | 收尾 | `ClampStats` → 月度累计（收入含补贴/支出三项）→ StatsManager 记账 → 月报(Day%30) → `CheckBankruptcy` → 日变化快照 | L998-1044 |

**月薪表**（level 1-5，索引−1）：
- 驾驶 driving：18000 / 22000 / 28000 / 35000 / 45000
- 维修 repair：14000 / 17000 / 22000 / 28000 / 35000
- 管理 management：12000 / 15000 / 18000 / 22000 / 28000
- 服务 service：10000 / 12000 / 14000 / 17000 / 22000

**常量总表**：TicketPrice=35、CapacityPerCar=30、OvercrowdThreshold=1.2、DailyDistancePerTrip=23、FuelConsumptionPer100km=58.5、FuelPrice=15、MonthlyWage=90000、ConductorLevel=2、MechanicLevel=2、CarUpgradeCost=100000、startMoney=40000（难度 50000/30000）。

**边界规则**：
- 破产：资金≤0 → 政府救助注入 5000（带政治压力叙事），`bankruptcyCount` 累计，二次破产条件收紧（`GameData.cs` L1235 起）
- 月净利 < −50000 → 财政压力警告；月报 Day%30 生成，月度净利≥100000 → 成就
- Trust / TrainCondition / ExpectedPassengers 均过 `ClampStats` 截断

---

## 二、现状缺口（批1 需要补的接线）

| # | 缺口 | 事实 | 处理 |
|---|---|---|---|
| G1 | **OrderManager 未接入** | `AdvanceDay` 全程无 OrderManager 调用；`OrderManager.cs:223` 自带 `Money<5000 → "freight"` 判定但无人驱动 | 接入：日订单结算在步骤 5.5（工资前）执行，产出并入步骤 10 净收入；订单收入计入 `CurrentMonth.TotalRevenue` |
| G2 | **CrewManager 日更未接入** | 工资只读月薪表；疲劳/忠诚/技能成长/师徒加成的日推进（CrewManager 内已实现）无人调用 | 接入：步骤 9.5 调 CrewManager 日更；**工资公式保持读技能等级**（成长次日生效，避免同日双算） |
| G3 | **SandRival 已接入** | 步骤 14 已在跑（audit 报告「未接入」为过期表述） | 不动；若批1 实现时发现重复调用点，删新不删旧 |
| G4 | **6 个难度乘数未被公式消费** | `incomeMultiplier / costMultiplier / passengerMultiplier / cargoMultiplier / sandPriceMultiplier / eventFrequency` 在 NewGameSetupUI 设置、GameConfig 存储，但 `GameData` 公式**一处都没读** | 按第三节乘数表接入 |
| G5 | **OrderManager 资金门槛语义** | `Money<5000 → freight` 是「没钱才接货运」的策略判定 | 接入时明确：这是订单推荐规则不是硬约束，参数化到表 |

---

## 三、T3 改造：乘数接入规范（G4）

接入原则：**乘数只乘在「汇总出口」，不进分项**，避免叠加爆炸与取整漂移。

| 乘数 | 乘点 | 公式改造 |
|---|---|---|
| `passengerMultiplier` | 步骤 2 客流出口 | `p = round(p × passengerMultiplier)`（容量约束之前） |
| `cargoMultiplier` | 步骤 5 货运出口 | `freight = round(freight × cargoMultiplier)` |
| `incomeMultiplier` | 步骤 10 净收入·收入侧 | `net = (dailyRevenue × incomeMultiplier) − costs + subsidy`，`dailyRevenue` 分项不变 |
| `costMultiplier` | 步骤 10 净收入·成本侧 | `cost = (fuel + maintenance + wage) × costMultiplier`，再取整 |
| `sandPriceMultiplier` | 沙能事件/交易（非本循环） | 交 SandRivalManager 与市场事件消费；AdvanceDay 不读 |
| `eventFrequency` | 步骤 13 | 传入 `EventManager.TryTriggerEvent` 的概率门（若 EventManager 暂无入口，先在 AdvanceDay 做 `Random.value < freq` 门控，回执注明） |

取整规范：**所有乘法后统一 `Mathf.RoundToInt` 一次**；分项保持整数沙币，不做级联四舍五入。

---

## 四、T3/T304 改造：结算环目标结构（实现契约）

```
AdvanceDay():
  1 trips（受政治压力红牌整顿制约）
  2 客流(×passengerMultiplier × 老化准点率折减) → 容量约束
  3-5 收入(客运/货运×cargoMultiplier)
  5.5 OrderManager.DailySettle() → orderIncome      ← G1
  6-8 燃料(含老化阻力加成)/工资/维护(×costMultiplier 于步骤10统一乘)
  9 补贴（受政治压力黄牌/红牌削减）
  9.5 CrewManager.AdvanceDay() → 疲劳/成长/忠诚日更  ← G2
  10 net = (dailyRevenue×incomeMultiplier + orderIncome) − costs×costMultiplier + subsidy
  11-12 信任/车况 delta 链
  12.5 [T304 新增] 全线路网老化演化（Weibull危险率磨耗 - 工务团队主动维护抑制）
  12.6 [T304 新增] 政治商业周期演化（Nordhaus 180天宏观波形 + 负面损失厌恶非对称冲击）
  13 事件(×eventFrequency 门控，含路网老化事故概率放大)
  14 沙能（现状，高渗透率反哺政治游说）
  15 收尾（月度累计、赤字反哺政治压力[最多连续10日后半衰、单次周期累计上限+0.08防死亡螺旋]、TrendSnapshot 写入 360 天环形缓冲供看板读取）
```

**初始化与防双倍**（配合 B-2/B-3 的 VNExitData 注入）：
- `OrderManager` / `CrewManager` 初始化一次（场景启动），**注入 VNExitData 不得触发二次 Reset**
- 注入后的首个 `AdvanceDay`：正常走全流程，不得出现「注入资金又被 ResetState 清零」或「CrewManager 注册与 G2 日更同日叠加」
- 验证路径（批2 交付物）：编辑器内 ①新游戏 → Day1 结算 → 记录 money/trust/condition；②带序章 VNExitData 进入 → 首日结算 → 注入值只被净收入改动

---

## 五、验收清单（我终审用）

1. 分项公式与第一节「现状」逐行比对**无意外改动**（手感不变）
2. G1/G2/G4 全部接上，G3 无重复调用
3. 乘数只出现在第三节规定的乘点；编辑器改 difficulty 后 6 个乘数逐一生效可见
4. 月度累计口径与净收入一致（收入侧含 orderIncome 与 incomeMultiplier）
5. 编译零错 + 一天推进冒烟 + 序章注入防双倍路径回执
6. 本表未尽事项（如 OrderManager 具体订单表、沙价乘数的市场侧消费）列入回执「遗留设计点」，不擅自扩权

---

## 六、初值与外显规范（2026-10-17 用户拍板，与本表公式同级）

### 6.1 初值铁律：一切来自千里马创世核，严禁固定数值

| 趋势 | 创世核基准 | 抖动 | 代码路径 |
|---|---|---|---|
| 信任 | 0.62 | ±0.05 | `WorldGenerator` → `initialTrends.trust` → `GameData.ApplySeedInitialValues` |
| 财政压力 | 0.30 | ±0.05 | 同上 `fiscalPressure` |
| 沙能渗透 | 线网人口加权均值 | ±0.05 | `sandPenetration` |
| 政治压力 | 0.20 | ±0.05 | `politicalPressure` |
| 线路老化 | 0.30 | ±0.05 | `infrastructureDecay`（读回时按路基0.40/桥隧0.35/信号0.25 加权拆分） |

- **确定性**：同一世界种子重开 → 同一组初值（`System.Random r` 派生，n创世核 v3.0 确定性算法）。
- **禁固定**：`GameData.ResetState()` 的 Trust=62 / PoliticalPressure=0.20 / InfrastructureDecay=0.30，以及 `RestoreFromSave` 的 PlayerPrefs 缺字段默认值，**仅是无种子与老档兼容兜底**——正常新档只要存在 `CurrentSeed` 就必须被 `ApplySeedInitialValues` 覆盖为抖动值。

### 6.2 外显红线：政治压力 / 线路老化只准定性

| 状态 | UI 允许呈现 | 禁止 |
|---|---|---|
| 政治压力 | 监管评级：Ⅰ 信任蜜月期 / Ⅱ 审慎监管区 / Ⅲ 黄牌通报 / Ⅳ 红牌整顿 / ≥0.95 听证接管，+ 定性描述句 | 任何 `xx%`、0-1 读数、指数数值 |
| 线路老化（总/分项） | 四档词：**<0.30 状态良好 ／ 0.30~0.55 轻度磨损 ／ 0.55~0.75 明显劣化 ／ ≥0.75 严重劣化** | 百分比、分项读数、权重披露（路基40%/桥隧35%/信号25% 不得上 UI） |
| 信任 / 财政（钱） | **可外显**数值（沿用现有 0-100、沙币口径） | —— |
| 沙能渗透 | 保持现状（未列入禁令） | —— |

- **落点**：`Assets/Scripts/UI/TabletTrendDashboardUI.cs` —— `DecayLevelWord()` 为档词唯一定义处（阈值必须与本节一致）；悬停 tooltip 属死元素，若未来启用只准显示日期/评级档。
- **允许的百分比**：制裁罚则公告（补贴−25%/−50%、发车−1）属政策后果披露，**允许**；被禁的是政治压力与老化**状态本身**的读数。
- 哲学出处：延续 `PROJECT_OVERVIEW.md` §3.4「疲劳与忠诚不在 UI 显示具体数值，而是通过行为、状态与对话自然流露」。
