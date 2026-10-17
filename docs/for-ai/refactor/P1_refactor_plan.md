# P1 架构重构方案设计规范（T305 专项）

> **编制**：Google Antigravity (AGY)  
> **审阅**：Xiaomi MiMo Desktop（本地调度与策划核）  
> **日期**：2026-10-17  
> **任务依据**：`docs/for-ai/dispatch/orders/T305.md`（只读码出方案，不动任何代码）  
> **前置依赖**：T304（P0 核心数值三件套）已于 commit `1032587` 顺利合入，本文方案均以包含 T304 增量的代码事实为基准。

---

## 目录
- [章 1 · GameMainUI.cs 模块化拆分方案](#章-1--gamemainuics-模块化拆分方案)
- [章 2 · LoginManager.cs 遗留解耦与瘦身方案](#章-2--loginmanagercs-遗留解耦与瘦身方案)
- [章 3 · GameData.cs 静态类演进与上下文隔离评估](#章-3--gamedatacs-静态类演进与上下文隔离评估)
- [综合排期与工作量矩阵 (Size of Effort)](#综合排期与工作量矩阵-size-of-effort)

---

## 章 1 · GameMainUI.cs 模块化拆分方案

### 1.1 现状证据（真实读码）
- **文件规模**：7,071 行，347,382 字节（~347 KB）。全工程最大单体文件。
- **技术栈**：Unity UI Toolkit（`UIDocument` + `VisualElement`），纯 C# 动态样式与节点构建。
- **职责杂糅现状**：
  它不仅承担了平板系统桌面 Shell 职责，还硬编码塞入了 12 个完全独立的 App 界面渲染逻辑，行段分布如下：

| 行号区间 | 行数 | 职责划分 | 现状描述与代码实体 |
|---|---|---|---|
| **L1 – L760** | 760 | **Desktop Shell 核心容器** | 锁屏扫脸、电量/状态栏、桌面图标网格、壁纸系统、事件与生命周期入口 |
| **L761 – L1628** | 868 | **桌面交互微件群 (Widgets)** | 时钟、天气、相册快照、日历、Dock 栏、文件夹叠加层、岁月 AI 投影小部件 |
| **L1629 – L1832** | 204 | **App 窗口容器与路由系统** | `BuildAppLayer`、全屏容器外壳、`LaunchApp` 核心分发路由 |
| **L1833 – L3494** | 1,662 | **设置 App (`BuildSettingsPage`)** | 24 分类菜单、显示/音频/操作/系统信息、TIPS 术语图鉴、应用管理彩蛋 |
| **L3495 – L3767** | 273 | **RDA 调度助手 (`BuildRDAApp`)** | 发车排班、车况监控、简报待办列表、五线态势入口按钮 |
| **L3768 – L4501** | 734 | **归途 App (`BuildReturnJourneyApp`)** | 剧情回顾、蔚蓝档案风格选章/回放、历史台词流 |
| **L4502 – L4535** | 34 | **媒姬百科 App (`BuildWikiApp`)** | 铁路词条速览轻量面板 |
| **L4536 – L5083** | 548 | **运营笔记 App (`BuildNotesApp`)** | 三栏可编辑备忘录、真实数据自动化联动 |
| **L5084 – L6265** | 1,182 | **音乐 App (`BuildMusicApp`)** | 13 首全量曲目列表、毛玻璃悬浮播放器、音轨切换逻辑 |
| **L6266 – L6504** | 239 | **相册 App (`BuildGalleryApp`)** | CG 鉴赏、分类筛选、存档解锁检查 |
| **L6505 – L6614** | 110 | **通用卡片与行骨架规范** | `BuildAppBody`、`BuildAppCard`、`BuildLightRow` 基础 UI 样式组件 |
| **L6615 – L7071** | 457 | **系统轻量工具 App 群** | 时钟、日历、天气、计算器、米家遥测、安全中心、浏览器、应用商店等占位与轻交互 |

### 1.2 目标架构：组件解耦设计
重构原则：**Desktop Shell 仅负责窗口管理与系统级能力注入，所有业务 App 独立为静态/轻量组件类**，参照已验证通过的独立看板 [`TabletTrendDashboardUI.cs`](file:///d:/Unity%20Project/RailwayRenaissance/Assets/Scripts/UI/TabletTrendDashboardUI.cs) 模式。

```
Assets/Scripts/UI/Tablet/
├── GameMainUI.cs                 # 仅保留 Desktop Shell、锁屏、状态栏、微件、App 容器与 LaunchApp 路由（~1,500行）
├── TabletUIHelper.cs             # 抽取通用样式：Fd()、GlassBgSoft、BuildAppBody、BuildAppCard 等（~180行）
├── Apps/
│   ├── TabletRDAAppUI.cs         # 原 L3495-L3767（行车调度、发车、待办）（~300行）
│   ├── TabletSettingsAppUI.cs    # 原 L1833-L3494（设置、24分类、TIPS图鉴）（~1,680行）
│   ├── TabletMusicAppUI.cs       # 原 L5084-L6265（音乐播放器、曲目列表）（~1,200行）
│   ├── TabletReturnJourneyUI.cs  # 原 L3768-L4501（剧情回顾、选章回放）（~750行）
│   ├── TabletNotesAppUI.cs       # 原 L4536-L5083（运营备忘、三栏笔记）（~560行）
│   ├── TabletGalleryAppUI.cs     # 原 L6266-L6504（相册 CG 鉴赏）（~250行）
│   └── TabletSystemToolsAppUI.cs # 原 L6615-L7071（时钟/天气/日历/计算器等轻工具群）（~480行）
```

**公开接口设计规范**：
每个子 App 类统一暴露静态构建入口：
```csharp
namespace RailwayRenaissance.UI.Tablet
{
    public static class TabletRDAAppUI
    {
        public static void Build(VisualElement windowContainer, System.Action<string> launchAppCallback);
    }
}
```

### 1.3 分批迁移步骤（确保工程每步 100% 可编译）
- **批次 1-A：提取基础样式与容器助手 (`TabletUIHelper.cs`)**
  - 将 `Fd()` 字体获取、毛玻璃颜色常量（`GlassBgSoft`、`GlassBorderSoft` 等）、通用骨架（`BuildAppBody`、`BuildAppCard`、`BuildLightRow`）下沉到独立静态类。
  - `GameMainUI.cs` 内部直接复用该工具，**验证编译 0 error**。
- **批次 1-B：剥离大型媒体类 App（音乐 + 相册 + 归途）**
  - 将 `BuildMusicApp`（L5084-L6265）、`BuildGalleryApp`（L6266-L6504）、`BuildReturnJourneyApp`（L3768-L4501）移至 `TabletMusicAppUI.cs`、`TabletGalleryAppUI.cs`、`TabletReturnJourneyUI.cs`。
  - `GameMainUI.cs` 的 `LaunchApp` 仅保留调用委托，**立即削减 2,150 行代码**，验证编译。
- **批次 1-C：剥离超大单体设置 App (`TabletSettingsAppUI.cs`)**
  - 将 24 分类设置及 TIPS 图鉴（L1833-L3494）外迁至独立文件。
  - **再削减 1,660 行代码**，验证编译。
- **批次 1-D：剥离核心经营 RDA App 与轻量工具群**
  - 迁出 `TabletRDAAppUI.cs` 与 `TabletSystemToolsAppUI.cs`。
  - `GameMainUI.cs` 彻底完成瘦身，代码行数从 7,071 行收敛至 ~1,500 行纯 Shell。

### 1.4 核心风险与缓解措施
1. **字体与闭包引用依赖**：
   - *风险*：许多内部构建方法隐式捕获了 `gameFont`、`Fd()` 或 `ShowBackupToast`。
   - *缓解*：在 `TabletUIHelper` 中集中管理 `FontDefinition` 单例；将 Toast 提示方法设计为公共委托 `Action<string> showToast` 传递给各 App。
2. **音乐播放器全局状态常驻**：
   - *风险*：`BuildMusicApp` 内有悬浮小播放器的常驻心跳与背景音乐轮询。
   - *缓解*：音乐播放核心状态此前已绑定在 `AudioManager.cs`，UI 仅为视图订阅，确保外迁时仅迁移 DOM 构建树，不改变底层音频控制器引用。

### 1.5 白名单预估
- `Assets/Scripts/GameMainUI.cs`
- `Assets/Scripts/UI/Tablet/TabletUIHelper.cs` (新增)
- `Assets/Scripts/UI/Tablet/Apps/*.cs` (新增 7 个独立文件)

---

## 章 2 · LoginManager.cs 遗留解耦与瘦身方案

### 2.1 现状证据（真实读码）
- **文件规模**：2,352 行，93,890 字节（~94 KB）。
- **历史定位与矛盾**：
  文件开头明确标有 `[DEPRECATED]`（平板桌面已替代登录场景，玩家直入序章），但由于历史原因，**全工程核心系统仍强行将其作为通用基础设施访问**。
- **职责归属行段剖析**：

| 行号区间 | 行数 | 职责模块 | 现状分析与跨模块依赖 |
|---|---|---|---|
| **L10 – L68** | 59 | **全局静态元数据与光标资源槽** | `GAME_VERSION = "Beta v0.9.0"`、`cursorTexture`、`clickClip`。被 10+ 外部脚本直接引用 |
| **L69 – L210** | 142 | **启动装配与设置环境初始化** | 初始化 EventSystem、VSync、FPSDisplay、StatsManager 唤起、音频与画布建立 |
| **L211 – L870** | 660 | **UGUI 动态像素美术生成器** | 纯代码动态画九宫格 Sprite（输入框、面板底图、眼睛图标、单选框、对话框） |
| **L871 – L1240** | 370 | **本地存档与用户认证逻辑** | `auth.json` 读写、密码散列比对、多槽位持久化、自动登录检查 |
| **L1241 – L2260**| 1,020 | **登录/注册 UGUI 交互状态机** | 登录面板、注册面板、找回密码弹窗、协程倒计时、输入校验动效 |
| **L2261 – L2352**| 92 | **公共数据类与事件光标辅助器** | `public class AuthData`、`InputFieldCursor`、`ButtonHoverCursor`、`ScrollAreaCursor` |

### 2.2 目标架构：三层解耦设计
无需维护庞大的 2,300 行 UGUI 绘制代码，将其核心有用资产抽取为**通用轻量类**，使弃用模块真正安全退出：

```
Assets/Scripts/Core/
├── GameVersion.cs            # 统一版本控制（原 LoginManager.GAME_VERSION 迁移至此）（~25行）
├── GameCursorManager.cs      # 全局光标与音效托管（原 cursorTexture/clickClip/SetCursor 逻辑）（~120行）
├── Auth/
│   └── UserAuthService.cs    # 本地存档账户与 auth.json 纯数据服务，包含 AuthData（~160行）
└── Legacy/
    └── LoginManager.cs       # 纯 UGUI 废弃视图保留（若后续彻底移除 Login.unity 场景可连根移除）
```

### 2.3 分批迁移步骤
- **批次 2-A：剥离 `GameVersion.cs` 与 `GameCursorManager.cs`**
  - 将 `GAME_VERSION` 与光标纹理管理抽离出独立静态类；
  - 在 `LoginManager` 中保留 `public const string GAME_VERSION = GameVersion.Current;` 兼容垫片；
  - 外部调用脚本（`TitleScreen`、`VNManager`、`UiCursor`、`GameMainUI`）无感，验证编译。
- **批次 2-B：抽取纯数据模型 `UserAuthService.cs`**
  - 将 `AuthData` 类与 `auth.json` 的读写、保存、自动登录状态移出为独立数据服务；
  - `AutoLoginUI.cs` 改接 `UserAuthService.AuthData`，解除对弃用视图的依赖。
- **批次 2-C：瘦身 `LoginManager.cs`**
  - 移除已无引用价值的冗余 UGUI 生成代码，仅保留向后兼容的外壳，或将其封存于 `Legacy/` 目录。

### 2.4 核心风险与缓解措施
1. **老存档账户断连**：
   - *风险*：用户机器上的 `auth.json` 存放于 `Application.persistentDataPath`。
   - *缓解*：`UserAuthService` 沿用一模一样的路径与 JSON 键名（`username`、`passwordHash`、`autoLogin`），保持无缝读取。
2. **场景跳转依赖**：
   - *风险*：若某些测试场景仍从 `Login.unity` 启动，移除组件可能导致空指针。
   - *缓解*：`LoginManager.cs` 保留 MonoBehaviour 存根，保持场景中 GameObject 挂载不挂起红标。

### 2.5 白名单预估
- `Assets/Scripts/Core/GameVersion.cs` (新增)
- `Assets/Scripts/Core/GameCursorManager.cs` (新增)
- `Assets/Scripts/Auth/UserAuthService.cs` (新增)
- `Assets/Scripts/LoginManager.cs`
- `Assets/Scripts/AutoLoginUI.cs` (适配类型名)

---

## 章 3 · GameData.cs 静态类演进与上下文隔离评估

### 3.1 现状证据与调用普查（真实读码）
- **现状声明**：`public static class GameData`（`Assets/Scripts/GameData.cs:5`）。
- **全局调用普查结果**：
  全工程共有 **217 处** `GameData.` 静态直接调用点，强依赖分布于 **21 个关键业务脚本** 中：
  - `UIManager.cs`（69 处）
  - `GameMainUI.cs`（45 处）
  - `TabletTrendDashboardUI.cs`（18 处）
  - `WorldAnalyzer.cs`（17 处）
  - `NarrativeEngine.cs`（12 处）
  - `OrderManager.cs`（7 处）
  - `VNSaveSystem.cs`（7 处）
  - `CrewManager.cs`（5 处）
  - 其余 13 个脚本共 36 处。

### 3.2 深度评估：单机游戏架构下的性价比与决策建议
针对 MiMo 提出的“静态类 $\rightarrow$ 上下文单例”演进方向，AGY 结合当前代码事实给出明确的技术评估：

> **核心结论：建议采取「门面兼容式（Facade）渐进演进」，严禁在现阶段发动全工程 217 处调用点的激进重写。**

- **为什么全量非静态化在当前“性价比不足”**：
  1. **单机单局事实**：本项目为单人本地单机游戏，运行时全局严格只有一趟列车、一个雾峰站、单份经营状态。不存在“多实例沙盒对战”或“多人并行房间”的并发诉求。
  2. **缺少自动化单测套件**：工程当前未部署 NUnit/PlayMode 自动化测试套件（依赖 Unity 编辑器与 batchmode 进行集成冒烟）。一次性修改 21 个文件的 217 处调用点，极易引入隐蔽的空引用与初始化时序脱节，回归成本极高。
  3. **静态生命周期在 Unity 中运行稳定**：`GameData` 内部已通过 `initialized` 守卫与 `ResetState()`、`ApplyVNExitData()` 建立了清晰的单局重置管线，未发生跨局数据污染。

### 3.3 渐进式演进路径：上下文数据核（Context）+ 静态门面（Facade）
若未来为支持“AI 离线模拟推演”或“自动化数值沙盒”而必须解耦实例，应按如下方案渐进演化：

#### 阶段 1：状态与逻辑抽取为 `EconomyContext`，`GameData` 退化为 Facade
将 `GameData` 内部的 30+ 静态变量汇聚成独立数据类 `EconomyContext`，并在 `GameData` 内维持一个单例指针：

```csharp
// 纯数据与结算上下文（可被独立实例化、可序列化、可用于无头单测）
public class EconomyContext
{
    public int Day = 1;
    public int Money = 40000;
    public int Trust = 62;
    public float PoliticalPressure = 0.20f;
    public float InfrastructureDecay = 0.30f;
    // ...
    public DayResult AdvanceDay(GameConfig config) { /* ... */ }
}

// 静态门面（对全工程 217 处调用点保持 100% 签名与字段兼容）
public static class GameData
{
    public static EconomyContext Current { get; private set; } = new EconomyContext();

    // 委托属性：外部脚本完全零修改！
    public static int Money => Current.Money;
    public static int Trust => Current.Trust;
    public static float PoliticalPressure => Current.PoliticalPressure;
    public static float InfrastructureDecay => Current.InfrastructureDecay;

    public static DayResult AdvanceDay() => Current.AdvanceDay(GameConfig.Load());
    public static void ResetState() => Current = new EconomyContext();
}
```

#### 阶段 2：未来测试/沙盘扩展
- 需要测试不同策略时：直接 `var testContext = new EconomyContext(); testContext.AdvanceDay();`，无需触碰全局静态状态；
- 全工程既有 217 处业务调用点**改动量为 0**，重构风险降为零。

---

## 综合排期与工作量矩阵 (Size of Effort)

综合风险与架构收益，各重构子任务的量级、批次与依赖顺序规划如下：

| 阶段 | 任务目标 | 预估批次 | 改动规模 (SoE) | 核心文件白名单 | 实施风险 | 建议排期 |
|---|---|---|---|---|---|---|
| **Phase 1** | **`GameMainUI.cs` 业务 App 剥离** | 4 批 | **L (大)**<br>外迁 ~5,500 行，拆分为 7 个 App 类 | `GameMainUI.cs`<br>`Assets/Scripts/UI/Tablet/` (新增) | 中 (UI 样式闭包依赖，需保证编译) | **建议首选启动**（收益最高，彻底解决 347KB 巨石维护痛点） |
| **Phase 2** | **`LoginManager.cs` 解耦与抽离** | 3 批 | **M (中)**<br>新建 3 个轻量类，废弃冗余代码 | `GameVersion.cs`、`GameCursorManager.cs`、`UserAuthService.cs`、`LoginManager.cs` | 低 (外部调用主要集中于版本和光标) | **次选启动**（解耦遗留弃用模块） |
| **Phase 3** | **`GameData.cs` 上下文 Facade 化** | 2 批 | **S (小)**<br>内部包装 `EconomyContext`，外部 0 破坏 | `GameData.cs`、`EconomyContext.cs` (新增) | 低 (外部 217 处零改动，仅内重构) | **按需启动**（待后续有自动化单测或离线模拟诉求时触发） |

---

*本文档为 T305 专项交付物，完整记录了 P1 重构的技术路线与防撞车保护机制，待 MiMo 评审后转立具体实施单。*
