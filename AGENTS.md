# AGENTS.md — 铁路复兴：沙能冲击

## ATO research prototype (2026-10-25)

CabAtoController is a System-only research controller; CabAtoResearchDriver is opt-in in the separate CabAtoResearchReview scene. BeforeSimulationStep schedules commands on simulation ticks through existing facade interlocks. Keep manual takeover, signed target errors and immutable run artifacts. A frozen finite-grid holdout verified the 0.5s switching constraint; Unity Play checks passed. V2 adds versioned full checkpoints, route tasks and switchable smooth/air-saving/punctual objectives; Windows nominal Play and exact replay pass. Frozen mismatch trials retain interventions and speed violations, so full-envelope reliability and real-train calibration remain pending. See docs/for-ai/ATO_V2_IMPLEMENTATION.md and docs/research/ato_v2_results.md.

> AI 代理快速上手文档。修改代码前先读 `docs/for-ai/PROJECT_OVERVIEW.md` 和 `docs/for-ai/CODE_MAP.md`。

---

## 项目身份

| 字段 | 值 |
|------|-----|
| 引擎 | Unity 6000.5.8f1（Unity 6.5） |
| 语言 | C# |
| 美术 | 16-bit 像素画，暖色调，类似 Stardew Valley |
| UI 方案 | 混合：UGUI (Login/经营) + UI Toolkit (VN/标题) |
| 平台 | Windows + Android |
| 许可证 | MIT |

---

## 快速上手（AI 代理必读）

### 项目结构
```
Assets/
├── Scripts/             # C# 源码（2026-10-18：99 文件，约 46,895 行）
│   ├── VN/              # 视觉小说引擎
│   ├── WorldGen/        # 世界生成、种子注入与区域解锁
│   ├── Narrative/       # 模板叙事引擎
│   ├── Systems/         # 多日监管与破产程序
│   ├── Cab/             # 驾驶台状态/UI/3D（当前仍有未提交工作）
│   ├── LoginManager.cs  # 登录/注册（仍需拆分）
│   ├── GameData.cs      # 经济模拟与日推进
│   ├── UIManager.cs     # 经营 UI
│   ├── RailRevivalRuntimeBootstrap.cs  # 运行时自动构建
│   └── ...
├── Resources/           # 运行时加载资源
│   ├── Scripts/         # 10 个序章 + 9 个后续 + 1 个测试 JSON
│   ├── events.json      # 随机事件模板
│   ├── orders.json      # 订单模板
│   ├── bg/characters/bgm/sfx/  # 美术音频资源
│   └── Fonts/           # zpix 字体
├── Scenes/              # 4 个场景
├── 参考资料/             # 设计文档（GDD 主索引）
└── docs/for-ai/         # AI 代理专用文档
```

### 场景流程
```
Login.unity → TitleScreen.unity → VN_Test.unity → StationSlice_V1.unity
                                    (序章剧本)      (经营主场景)
```

### 4 个核心场景
| 场景 | 用途 | 入口 |
|------|------|------|
| `Login.unity` | 登录/注册 | app 启动 |
| `TitleScreen.unity` | 标题界面 | 登录后 |
| `VN_Test.unity` | 序章视觉小说 | 新游戏/继续 |
| `StationSlice_V1.unity` | 车站经营 | VN 结束后 |

---

## 核心系统速览

### 四大核心设计概念
| 系统 | 代码状态 | 核心文件 |
|------|----------|----------|
| 沙本位经济核 | ✅ 部分实现 | `GameData.cs` — AdvanceDay() 每日结算 |
| 千里马创世核 | ⚠️ 算法/API 已有，正常开局接线待修复 | `WorldGen/` — seedCode 与 seedId 入口尚未统一 |
| 岁月叙事引擎 | ⚠️ 模板阶段已接日结，生成式阶段待实现 | `Narrative/` — VN/订单与部分效果仍有 TODO |
| 先民人事系统 | ✅ 人事日更与成长等已有代码，运行验收另行核对 | `CrewManager.cs`、`Core/FluctuationEngine.cs` |

### 五条趋势线
| 趋势 | 代码 | 状态 |
|------|------|------|
| 信任 | `GameData.Trust` | ✅ |
| 财政 | `GameData.Money` | ✅ |
| 沙能渗透 | `SandRivalManager.cityPenetration` | ✅ |
| 政治压力 | `GameData.PoliticalPressure` + `PoliticalTier` | ✅ 连续量 + 多日监管程序 |
| 设施老化 | `InfrastructureDecay` + 三分项 + 缓存评级 | ✅ 磨耗/维护 + 定期检定 |

### 每日结算流程
```
调整策略 → End Day → GameData.AdvanceDay():
  1. 发车方案 → 趟数
  2. 计算客流
  3. 收入 - 燃料费 - 工资 - 维护费
  4. 信任/车况变化
  5. 随机事件 + 沙能竞争 + 员工更新 + 订单更新
  6. 教程检查 → 刷新 UI
```

---

## 编辑器外验证

项目没有独立的 build/test CLI。验证代码变更的唯一可靠方式：

1. **检查编译错误**:
   ```powershell
   Get-Content "C:\Users\Oe_Lee\AppData\Local\Unity\Editor\Editor.log" -Tail 200 | Select-String "error CS"
   ```
2. **批处理编译验证**:
   `"D:\Unity Hub\Unity 6.5\Editor\Unity.exe" -quit -batchmode -projectPath "D:\Unity Project\RailwayRenaissance" -logFile compile_check.log`

---

## Git 工作流

- **隔离**：每个独立写入任务使用一个 Codex 对话、主题分支和 Git worktree；不要在主工作区切分支来并行任务。分支格式为 `codex/<主题>-<任务ID>`、`agy/<主题>-<任务ID>` 或 `mimo/<主题>-<任务ID>`。
- **创建**：优先从最新 `origin/main` 建立 worktree，使用 `pwsh -NoProfile -File tools/git/railway-worktree.ps1 Start -TaskId <唯一ID> -Topic <主题> -Owner <负责人>`。脚本使用 GitHub 已配置代理；仅当本机代理不可达且已确认 FlClash Meta TUN 路由时，才临时绕过失效代理设置通过 TUN fetch。否则停止，不切换主工作区或改走物理网卡直连。
- **提交与 PR**：一个 commit 对应一个逻辑单元。任务完成并通过相应验证后再 push 主题分支、建立 PR；不强制 squash。只有存在真实代码依赖时才堆叠 PR，并按底层到上层审阅。Codex 不替用户合并 PR。
- **状态与清理**：用 `pwsh -NoProfile -File tools/git/railway-worktree.ps1 Status` 查看所有 worktree。仅在工作树干净且 PR 已合并后，运行 `Finish -TaskId <唯一ID> -Confirm`；清理不删除分支。不得自动移动、推送或删除用户现有工作区、提交或分支。
- **脏工作区**：开始任务前记录主工作区状态；已有改动及未跟踪文件默认归原工作区，不纳入新 PR。不得为创建 worktree 而 stash、reset、checkout 或清理它们。

---

## 关键约定

- **代码改动必同步文档（核心记忆铁律）**：任何功能、数值或架构变更，**必须强制同步更新对应的设计/接口/公式文档**（如 `docs/for-ai/DAILY_SETTLEMENT_FORMULAS.md`、`docs/for-ai/CODE_MAP.md`、派单回执等），严禁“只改代码不更文档”！
- **数值两铁律（2026-10-17 用户拍板）**：①**一切存档初值由千里马创世核按种子抖动生成，严禁固定数值**（`Jitter(基准, ±0.05, r)`；常量只许作无种子/老档兼容兜底）；②**政治压力与线路老化绝对禁止数值外显**——UI 只准监管评级/定性档位词，**钱与信任可外显**。新数值功能先过这两条再动 UI（细则见 `docs/for-ai/DAILY_SETTLEMENT_FORMULAS.md` §六）。
- **不要直接编辑 `.unity` YAML 文件** — 用 Unity Editor 或编辑器脚本
- **中文 UI 优先**
- **资源命名**: `characters/{ID}/{表情}.png`, `bg/{ID}.png`, `bgm/{ID}.ogg`, `Scripts/prologue_XX_{名称}.json`

---

## 设计文档索引

### AI 代理专用文档（`docs/for-ai/`）
| 文档 | 内容 | 阅读顺序 |
|------|------|----------|
| **PROJECT_OVERVIEW.md** | 项目总览：架构/数据流/场景/设计差距 | 第1个读 |
| **CODE_MAP.md** | 代码地图：类依赖/修改路径/资源引用 | 第2个读 |

### 核心设计文档（`参考资料/`）
| 文档 | 优先级 |
|------|--------|
| `游戏开发文档.md` | ⭐⭐⭐ 主索引 |
| `沙本位经济核.md` v4.2 | ⭐⭐⭐ 基于真实数据的经济模型+数学底层 |
| `视觉小说系统设计.md` v2.1 | ⭐⭐⭐ VN+AI设计 |
| `角色设定.md` | ⭐⭐ |
| `世界观扩展设定.md` | ⭐⭐ |
| `序章剧本_归乡.md` | ⭐⭐ |

### 设计文档（`docs/compose/specs/`）
| 文档 | 优先级 |
|------|--------|
| `核心玩法循环.md` v2.0 | ⭐⭐⭐ 三层时间+五条趋势线 |
| `跨系统联动公式.md` v1.0 | ⭐⭐⭐ 8个公式 |
| `科技树设计.md` v2.0 | ⭐⭐ 4领域28节点 |
| `岁月叙事引擎.md` v2.0 | ⭐⭐ 三阶段AI策略 |
| `铁龙竞争系统.md` | ⭐⭐ |
| `区域解锁与政治系统.md` | ⭐⭐ |

---

## 设计-实现差距（待办）

### 优先整合（2026-10-18 静态核对）
- 正常开局统一种子码与种子注入，核对 VN 过渡是否遵守初值铁律
- 完整槽位存档，避免六项基础状态与全局监管/世界状态混装
- 统一日结后置效果、快照、日变化与公告，完成 Unity 运行验收
- 政治/老化及趋势看板已有实现，不再列为「未编码」；证据见 `docs/for-ai/dispatch/orders/CODEX-AUDIT-20261018.md`

### P1 — 系统性重构
- `LoginManager.cs` 拆分（2308行 → 3-4文件）
- `GameData.cs` 静态类 → 非静态
- 硬编码常量抽取到配置

### P2 — 玩法扩展
- 科技树系统（28节点）
- 人事定性反馈验收、老化事故倍率与天气影响接线
- 月/年玩法扩展（30天月报/360天季节已存在）、区域剧情/科技门槛补齐

### P3 — 高级系统
- VN AI 三模式对话
- 创世核入口与存档验收 / 岁月叙事生成式阶段（模板阶段已有）
- 手机 AI 助手

---

## 子代理使用规范

### 核心原则
子代理必须使用与主代理**不同**的模型，否则触发并发上限。

### 模型选择
| 主代理模型 | 子代理模型 | 参数 |
|-----------|-----------|------|
| ds（deepseek-v4-flash） | `sensenova0718/deepseek-v4-flash`（0718） | `model: "sensenova0718/deepseek-v4-flash"` |
| 0718（sensenova0718/deepseek-v4-flash） | ds（默认，不指定） | 不指定 model 参数 |
| 识图/视觉分析 | `xiaomi/mimo-v2.5` | `model: "xiaomi/mimo-v2.5"` |

### 规则速记
> 主代理与子代理用相反模型。主代理是 ds 则子代理用 0718，主代理是 0718 则子代理用 ds。

### 并发规则
- 编码子代理最多同时 1 个
- 视觉分析任务可同时 2 个

### 任务绑定
- spawn 时若任务已被 task 工具追踪，传递 `task_id` 参数

---

## Tri-Agent 协作架构与剧本写作规范（Codex/GPT · MiMo · Antigravity）

### 1. 三方协同分工与消息总线（Bus）
- **Codex/GPT**：全局协调、项目上下文整理、任务拆分、代码审阅与整合验收。
- **Google Antigravity (AGY)**：主要代码实现方；仅按 ACP 会话中实测可用的模型、权限和工具执行任务。
- **Xiaomi MiMo**：项目知识补充、信息检索、设计推演和独立复核；不预设它是本地队列调度器。
- **统一技能源**：三方协作核心技能唯一源为当前用户的 `~/.codex/skills/tri-agent-collaboration/`；MiMo 与 Antigravity 用户级技能入口通过目录联接指向它。项目 `.mimocode/skills/` 仅保留铁路项目专用技能。
  - **实时通信**：Codex 分别通过各自原生 ACP 会话联系 MiMo 和 Antigravity。每个任务需核验新会话的模型与能力；这不代表三个代理能自动互聊、接续桌面旧聊天或互相转发。
  - **规格与交付**：任务写入 `docs/for-ai/dispatch/orders/` 并按需要更新 `QUEUE.md`；每张任务单末尾预留「完成回执」区，执行方直接在同一任务单追加回执，列明改动、实际验证证据及未验证范围。除用户要求或回执确实过长需独立报告外，不另建重复回执文件；队列只记状态和任务单链接。ACP 消息发送任务 ID、文档路径、范围和必要上下文。普通 ACP 交接无需写旧信箱或执行 `handoff`。
  - **并行**：`tools/compile/acp_tablet_dispatch.py` 默认跨项目串行；显式传入 `--allow-parallel-worktrees` 后，最多允许 Antigravity 与 MiMo 各一个 ACP 任务同时运行，且必须位于不同 worktree。同一 worktree 永远互斥；每个任务仍须唯一负责人和不重叠文件范围。ACP 并行试跑只有在各代理串行只读基线通过后才可启用。会话并存不等于任务并行或成功。
  - **授权与兼容**：只在用户授权范围内通信。旧 Gemini Cloud/文件总线流程仅在用户明确要求时使用，详见 [旧总线归档](docs/for-ai/bridge/BUS_LEGACY_ARCHIVE.md)；不得自动消费历史积压任务。
  - **状态报告**：区分已投递、已确认、已完成、已验收；ACP 超时或后端错误不是完成证明。技能更新本身不触发任务投递。




### 2. 序章剧本写作六规则与 05 样板基准
- **样板基准**：以 `prologue_05_inspection` 最新作者手改版（~7551字）为全篇改写标杆。
- **去水与密度**：彻底去除重复说教与空泛比喻，段均 ~1500-1800 字，节奏明快，动作紧凑。
- **首钩强化**：开篇第一句必须以受挫或核心危机入题（如“我输给了爷爷留下的那台机器”），拒绝拖沓起手。
- **动作做实**：专业操作必须有具象动作链（如读道尺歪尺->摔尺->正尺、递扳手柄予长辈、撕纸钉板接续字迹）。
- **JSON 技术标签规范**：
  - 心理活动统一使用 `{"t": "thought", "s": "说话人", "e": "表情", "text": "[...]"}`（必须带说话人 `s` 与表情 `e`，text 保留方括号，DialogueBox 遵守独白不叠色规则，清空或缺失 `s` 会导致姓名牌丢失及立绘被清空）；
  - 地点卡统一使用 `{"t": "loc", "text": "..."}`；
  - 核心铁路专有名词必须完整保留，以保证 `Terms.json` 的 65 条术语（涵盖工务保线、机车动力机械、车辆制动、电气信号闭塞）能被 `TermHighlightSystem` 稳定高亮并支持弹窗；
  - 对话回顾面板视觉规范：采用有色半透明和纸沙质图层（`MakeBacklogWashiTex` 128×128 双线性平铺，约 65% 透光度，底层 CG/立绘清晰可见），暖木纹姓名缎带，黑底金边「跳转」药丸按键；第一阶段完备轨段支持自由拖动就地停驻（Park），第二阶段滑块自适应上下文弹性缩放（最高70%轨长）；
  - BGM 切换遵守驻留规则（`ambient_nature` -> `melancholy` -> `calm` -> `embers`）。

---

*核心纪律：代码改动必须同步更新 `docs/for-ai/` 相关文档及公式表，添加或重构重大系统时必须更新 `AGENTS.md` 与代码地图。*


## Tablet system shell (2026-10-23)

`TabletPresentationPreferences` owns separate `TabletSystem/presentation-v1.json`; category9 mounts `TabletPresentationSettingsView`. Actual consumers cover wallpaper dim, reduced-motion unlock and system-panel accent/high contrast, not all app themes or music effects. Future schemas are read-only; save failures retain the original; reset only affects presentation settings. The old unconfirmed PlayerPrefs.DeleteAll setting was removed. `TabletPresentationStorageReview` plus `TabletSystemShellReview` verify isolated persistence and actual UI paths. True background blur remains a separately delegated prototype, not a released feature.

`TabletShellState`/`TabletShellView` add process-local bounded notifications, mutually exclusive notification/control panels, quiet notification presentation and settings entry. `GameMainUI` reserves a 48px system region above apps, gives an open panel first Escape handling, hides it on lock, and records actual app feedback. Antigravity authored the initial two files through ACP; Codex reviewed/applied/integrated them; MiMo provided scoped interaction audit. Do not call translucency Gaussian blur or imply real connectivity. Business notifications, persistence, immersive reveal, full settings cleanup and Android device QA remain pending. See `docs/for-ai/TABLET_SYSTEM_SHELL.md`; isolated verification entry is `TabletSystemShellReview.Batch`.

## Tablet railway music terminal (2026-10-23)

OST alignment tools run locally under tools/music with model/dependency caches in Library/MusicAlignment. The sleepers built-in LRC is a 35-line automatic draft with explicit pending-listening provenance, not a listening-approved or word-synced release. Preserve original embedded sheets; other songs' failed candidates stay in alignment_review. Korean lyric labels use the existing Sarasa font because zpix lacks Hangul. See MUSIC_FOLIA_CORE_REVIEW.md for source/audio discrepancies and actual Unity checks.

MusicTimedText and MusicLyricsView add local timed LRC/enhanced LRC/SRT/WebVTT, translations, lyric-click seeking, per-track offsets and immersive viewing. Use actual audio time; do not synthesize word timings or lyrics for instrumental music. Lyric files and settings share the independent music state directory. Playback history and focused UI shortcuts retain text-input behavior. Folia 0.7.21 is a behavioral reference, not a copied source dependency. See docs/for-ai/MUSIC_FOLIA_CORE_REVIEW.md for verified scope and exclusions.

TabletMusicAppUI delegates the runtime view to RailwayMusicTerminalView. MusicLibrarySession/Store own local/OST identities, favorites, queue, playlists and versioned per-machine JSON preferences outside economic save slots. MusicSessionDriver drives audio with frame deduplication; reopening does not autoplay. MusicTerminalIcons draws native vector buttons; covers use nine newly generated railway sleeve illustrations under Resources/Music/Covers, never scene screenshots. Cloud configuration must never imply verified login/sync. RailwayMusicTerminalReview uses isolated storage for actual tablet-entry and cross-process restore checks. See docs/for-ai/MUSIC_TERMINAL.md.

## NF-5K exterior lighting (2026-10-21)
CabExteriorLighting supplies Off/Marker/Dim/Bright/Tail modes and auxiliary power input, compatible with CabEngineState.HeadlightOn. NF5KFrontLightingSetup builds optics; NF5KLightingReview verifies dark renders and Play Mode. Light output is art-tuned; automatic consist end selection, battery and fault simulation remain pending. See docs/for-ai/NF5K_EXTERIOR_LIGHTING.md.


## NF-5K interior lighting (2026-10-21)
CabInteriorLighting controls separate cab/task, instrument dimmer, two passenger zones and automatic emergency lights from explicit normal/backup supply inputs. InstrumentLightOn remains compatible. Generation/review: NF5KInteriorLightingSetup and NF5KInteriorLightingReview. See docs/for-ai/NF5K_INTERIOR_LIGHTING.md; battery and physical reflection simulation remain pending.


## NF-5K passenger fittings (2026-10-21)
NF5KPassengerInterior generates shared native seat meshes, luggage racks, inner window returns and vestibule fittings. ValidateReload checks 48 seats and candidate aisle clearance; NF5KDoorReview includes new fixed seat/rack colliders. See docs/for-ai/NF5K_PASSENGER_INTERIOR.md. Geometry capacity is not connected to the economy or passenger simulation.


## NF5K diffuse lighting (2026-10-21)

CabDiffuseFill supplies circuit-dependent custom SH irradiance to opaque cabin surfaces. Approximation point lights remain bindings with cullingMask=0. Refresh diffuse data before local reflection capture. This is authored fill, not baked or traced GI. Keep normal/night/emergency comparisons and the lower-visibility/highlight check in tools/cab_reference/compare_diffuse_fill.py. See docs/for-ai/NF5K_LIGHTING_AUDIT.md.


## NF5K daylight review (2026-10-21)

NF5KDaylightReview generates a separate CabDaylightReview scene and measures cabin proportions. CabDaylightEnvironment supplies clear/overcast/low-sun presets and driver/passenger/exterior inspection views. Preserve the original night/emergency regression scene. See docs/for-ai/NF5K_DAYLIGHT_REVIEW.md for usage and scale/lighting limits.

## Cab contextual interaction (2026-10-21)

CabContextInteraction owns contextual action lists, runtime primary binding and first-use driving guidance in CabSimulationReview. CabStartupPointer retains raycast verification but suppresses its legacy frame input when the contextual component exists. Preserve physical control interlocks; assistance must use the same command path. Mouse helper delegation, complete route tutorials and global shortcut conflict handling remain pending. See 参考资料/系统/场景交互与驾驶教学设计.md.


## NF5K simulation backend (2026-10-21)

CabSimulation is a pure 50Hz lumped diesel-electric/air-brake kernel with seeded cold conditions. CabSimulationDriver is opt-in through CabEngineState; legacy scenes keep their prior physics. CabSimulationCodec preserves exact snapshot scalars. NF5KSimulationReview generates CabSimulationReview and checks kernel invariants plus Play Mode startup, traction, braking and lighting supply. Candidate parameters are not certified NF5K data; train-pipe propagation, wheel slip dynamics, real route movement, physical handles and global save slots remain pending. See docs/for-ai/NF5K_SIMULATION_BACKEND.md.

## NF5K rail motion review (2026-10-22)

CabRailMotion reads signed backend PositionM to place the vehicle, steer bogies and rotate wheelsets in the separate CabRailMotionReview scene. CabReviewTrack supplies an authored straight/transition/180m curve inspection alignment; CabRailReviewViews supplies following views. NF5KRailMotionReview checks wheel phase, reverse, conservative contact anchors, shoe clearance, bounded suspension settling and actual backend wiring. This is kinematic rail following plus suspension presentation, not a wheel/rail contact-force, slip or derailment solver. Preserve static review scenes and non-rotating drive housings. See docs/for-ai/NF5K_RAIL_MOTION.md.

## NF5K wall equipment (2026-10-23)

`NF5KCabCirculators` replaces the two fan placeholders with shared wire-cage/three-blade meshes, motor, roof mounting and restrained wiring. `CabCirculationFan` uses the existing contextual binding for off/low/high at separate desk-mounted controls (Control is the distance anchor), reads explicit interior normal/backup supply, and keeps distance/disable guards. V23 uses rounded blade meshes, 384-segment continuous V3 wire rings, motor-shaft/rear-guard anchors and tilt-aligned pivot supports; the ceiling head has no interaction collider. V25 binds the fixed wing-and-rail logo to original manufacturer Chuanlan and model CF-17R through Cab/circulator_profile.json; lore lives in 参考资料/设定/川岚机电厂.md. Nominal 24 V is lore, not a new electrical simulation input. V32 uses a thin 3 mm desk plate, a shallow graphite dial with indexed scale, a labelled FIXED/SWEEP rocker and four corner fasteners; checks include dial height, screw seating and both rocker states. V30 keeps whole-fan size but uses a 43 mm colour medallion, rear manufacturer/model plate and indexed 0/I/II desk scale. CabCirculationFan adds authored acceleration/coast, independent +/-25 degree oscillation and a following service-loop cable; explicit supply and distance guards remain. V24 adds low-profile slotted-head fasteners/washers, a 12 mm stem with locking collars, a continuous folded strap yoke and separate painted/steel/aluminium finishes. Review checks ring normals, screw seating and support/worktop contact geometry, not structural strength. `NF5KCabCirculatorsReview` verifies rays, detents, supply, rotor envelope and roof attachment in driving Play review. Rotation is visual tuning, not airflow/thermal/load simulation. See `docs/for-ai/NF5K_CAB_CIRCULATORS.md`.

Passage door v16 uses a 0.90 m structural opening, 0.86 x 1.87 m leaf, 0.68 x 0.75 m glazing and offset hinge (-0.44,0,1.85). The compact handle backplate stays below the window. `NF5KPassageDoorDetail` migrates partitions/geometry; `CabInteriorBlockoutSetup` matches new construction. `NF5KCabWorkstationReview` records actual leaf/window/handle dimensions. Original bilingual prints come from `Cab/passage_door_profile.json` and `build_passage_door_prints.py`. See `docs/for-ai/NF5K_PASSAGE_DOOR.md` for actual-reference limits and review results; candidate leaf sizes are not copied from a certified cab-door drawing.

`NF5KPassageDoorDetail` adds cab/passenger passage framing and connected hardware; `CabPassageDoor` provides contextual swing with local BoxCollider and camera-envelope guards, checked by `NF5KPassageDoorReview`. It is a normal interior door; full player capsule, latch mechanics, sounds and certified egress remain pending. See `docs/for-ai/NF5K_PASSAGE_DOOR.md`.

Auxiliary workstation refinement: `NF5KCabCommunicationDetail` adds underdeck support/cable trunk, a restrained fixed handset, SL-R8 local radio controls and a readable CW-01 clipboard. `CabWorkstationEquipment` uses the existing contextual binding and lighting supply states; `NF5KCabCommunicationReview` checks rays, controls, supply, modal reading and restoration. Dispatch transmission/audio, handset pickup and persistent settings are pending. See `docs/for-ai/NF5K_CAB_COMMUNICATION.md`.

CabSunVisor and NF5KCabVisors replace the old sun-visor placeholder with independently adjustable three-position leaves and physical mounts. CabContextInteraction reuses player bindings, while NF5KSafetyEquipmentReview checks real rays, independent stops, vertex envelopes and restoration. First-aid graphics use NF5KEquipmentPrint and first_aid_label_profile.json; printed pack quantities are not gameplay inventory. See docs/for-ai/NF5K_CAB_VISORS.md and NF5K_WALL_EQUIPMENT.md.

NF5KWallEquipment adds a detailed cab rear electrical cover and passenger first-aid cabinet. CabStorageCabinet opens/closes the real hinged leaf through CabContextInteraction using the existing player binding; distance and disable restoration are guarded. Contents are restrained sealed-pack visuals, without inventory, medical effects or electrical simulation. NF5KSafetyEquipmentReview verifies cabinet rays/actions and selected geometry clearances. See docs/for-ai/NF5K_WALL_EQUIPMENT.md.


## NF5K twin wipers (2026-10-24)

Cab3DSceneController.WipersOn/SetWipers/WiperStateChanged separate selection from rain. CabWiperMotion adds candidate 36/60-cycle/min speeds, phase continuity, powered self-park and power-loss freeze/resume. CabWiperSystem drives two mirrored fixed-length parallelograms, reads explicit lighting supply (resolved lazily for fresh scene generation), and offers near-distance contextual three-detent commands. Legacy binding is explicit and optional. NF5KWiperSetup replaces static exterior links and adds inner drive covers, retained harness returns, sealed shafts and a shared desk control in four review scenes. NF5KWiperMotionReview validates full-cycle rubber/glass contact, aperture clearance, hardware spacing, late supply and real ray/context paths. No washer/water-film, friction, motor current or certification is implied. See docs/for-ai/NF5K_WIPERS.md. Fan v33 uses recessed screw-head geometry and fine selector materials; see NF5K_CAB_CIRCULATORS.md.


## NF5K lighting piano keys (2026-10-24)

CabLightingKey binds ten original independent pivots to seven CabInteriorLighting circuits and three disconnected spare positions. NF5KLightingKeysSetup extends the original bezel to carry inset bilingual function names and per-key hit targets in four review scenes. Separate desk plaques, visible numbering and the redundant group heading were removed on user review. Context actions reuse the primary binding, retain selection on power loss, and preserve instrument dimming. lighting_keys_profile.json is shared by printed labels and zh/en prompts; SetLanguage is a process-local adapter, not an integrated global language setting. Exterior operating lamps and automatic emergency lighting remain separate. NF5KLightingKeysReview verifies actual rays/circuits, isolation, supply, dimmer, context, language and guards. See docs/for-ai/NF5K_LIGHTING_KEYS.md.


Console layout revision (2026-10-24): lighting legends use the instrument-side extension of the original key bezel. NF5KConsoleRecessSetup removes the startup spacer, seats the independent key/fuel-pump/start panel flange 1.5 mm above the actual desk, and generates a real through-opening with cut-edge walls for the recessed enclosure from a preserved source mesh. Called by both console import and fittings reconstruction; no source FBX or scene YAML edits. See NF5K_LIGHTING_KEYS.md for evidence and limits.

Lighting layout v7: NF5KLightingKeysSetup generates one continuous aperture bezel spanning keys and upper legends, with antialiased transparent bilingual ink on the shared finish. Original frame renderers are hidden as dimensional sources. The recessed startup panel moves 35 mm toward the instruments using an absolute mount reference, regenerating the opening from the preserved deck mesh. Clearance thresholds are authored candidates, not regulatory dimensions. See NF5K_LIGHTING_KEYS.md.

Exterior light control panel (2026-10-24): single-cab physical headlight 3-detent and operating-profile 6-detent selectors occupy the existing auxiliary panel. CabExteriorLighting supports Off/DayRunning/NightRunning/Shunting/Tail/Manual; authored day/night lamp masks differ, and manual headlight changes leave preset mode. Red markers exclude headlights, power loss retains selection, and legacy switches restore the full profile. NF5KExteriorLightControlReview verifies actual rays/context, profiles, old modes, supply and indoor isolation; full driving Play review passes. Country references are versioned research, not a universal rule or certified NF5K lighting standard. Rear fixtures and consist-end detection remain pending. See docs/for-ai/NF5K_EXTERIOR_CONTROL_PANEL.md.

Music interaction follow-up (2026-10-24): MusicLyricsView adds focused stage/depth and audio-clock ambience. TabletMusicAppUI.TryGetCurrentPosition distinguishes a destroyed/mismatched source from real zero; MusicLibrarySession.Save preserves last valid position on teardown. Current full-project compilation is blocked by the unrelated missing TabletCalculatorEngine review dependency; music runtime snapshot and separate-process restore passed. See MUSIC_TERMINAL.md for evidence and uncompleted lyric calibration.

Music reactive stage (2026-10-24): MusicReactiveStageView owns cached cover palettes, track-specific field layouts and native gradient meshes. It measures actual output/decoded PCM RMS and uses audio time for motion; no synthetic beat detector or copied Folia code. Pause freezes and reduced motion removes pulses. Isolated runtime/PCM checks passed; lyric timing approval and full-project compilation remain separate. See MUSIC_FOLIA_CORE_REVIEW.md.

Music timing calibration (2026-10-25): south_wind now ships a 39-line original-prompt phonetic calibration draft, explicitly pending listening and never word-synced. MMS_FA weights/dependencies stay in Library, not Assets; publish_phonetic_draft archives immutable checks and preserves humanListeningVerified=false. Focused lyrics retain the draft label. Actual clip seeking/highlight and process restore passed isolated runtime checks; other songs' new phonetic candidates failed publication checks and remain unpublished. See MUSIC_FOLIA_CORE_REVIEW.md.

2026-10-25：十格状态/报警灯窗由NF5KAnnunciatorSetup替换四颗未接线圆灯；CabAnnunciatorPanel只读详细模拟，包含供电、启动低油压延时、压力/温度迟滞及中英印刷。来源、状态定义和验证见docs/for-ai/NF5K_ANNUNCIATOR.md。
2026-10-25：CabLampTestButton为灯窗右侧实体试灯按钮，复用主交互绑定，点按触发2秒十格检查及短行程弹回；断电终止、超时恢复真实指示，未改保护或驾驶指令。四场景、实际射线/上下文、供电/禁用守卫和checkpoint不变已通过。见NF5K_ANNUNCIATOR.md。
2026-10-25：NF5KServiceControlSetup在原鸣笛位置生成横向双按钮和贴台面薄底板；CabServiceControl接入主交互键保持/释放及旧SandOn后端，CabHornAudio播放原创合成汽笛。四场景生成、实际射线/上下文、供电/风压/距离/禁用守卫、PCM播放和底板支承已检查，详见NF5K_SERVICE_CONTROLS.md。
2026-10-25：NF5KBrakeRefinement统一大小闸冷灰涂层/裸钢/深色握把，按钮底板缩至110×60 mm并移位，消除与小闸安装耳约6 mm旧包络重叠，新横向间隙约27 mm。生成及实际Play姿态检查15 mm扩展包络，交互回归通过。见NF5K_BRAKE_REFINEMENT.md。
2026-10-26大小闸材质v2：原图旧金属为主、现代产品图补充参考；BrakeLayeredMetal使用URP PBR，分铸件/加工面/涂层/握把，局部米制三向投影、网格磨损/积污遮罩；间隙检查改为旋转车体米制坐标。四场景及驾驶回归通过，见NF5K_BRAKE_REFINEMENT.md。

NF5K parking controls v6 (2026-10-28): CabParkingBrakeButton and NF5KParkingBrakePanelSetup replace the unsupported rotary assembly with fixed apply/release buttons through the existing ParkingBrake command facade. Per-cap contextual actions require the existing deliberate hold, preserve stop/zero-traction interlocks and add reach/control-power/pause guards. No physical actuator feedback is implied; verification scope is recorded in NF5K_PARKING_SELECTOR.md.

## NF5K vigilance candidate (2026-10-28)
CabVigilance is a manual periodic acknowledgement candidate, driven on simulation ticks via CabSimulationDriver. The optional checkpoint extension preserves timer/latch and manual controls separately. Emergency braking approximates protection; no national certification, control-activity auto-reset or casual assistance is implied. See docs/for-ai/NF5K_VIGILANCE.md for current verification.
