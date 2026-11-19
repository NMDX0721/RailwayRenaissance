# ATO 研究与 3D 接入工作交接

2026-11-13 会话归档说明。本文件已确认保存在主项目目录，目前为本地未跟踪文件。下文的分支、提交、源码数量和验证结果记录于 2026-11-05，后续会话接手时先复核当前 AGENTS.md、Git 工作区、ATO 路线源码和 Unity 验收日志。本次归档准备仅核对交接文件，当前工程进度需要重新确认。

交接日期为 2026-11-05。交接范围包括本会话的 ATO 修复、PR 合并、验证记录与建模接口。下文将本轮已完成的工作和后续任务新增的文件分别标明，接手时先核对现场状态。

## 1. 用户目标与约定

- 推进 ATO 研究，在 Unity 中完成控制器、模拟后端、实体操纵与 3D 车辆的运行闭环。
- 用户负责轨道及相关建模，程序侧负责路线数据、控制、姿态与验证接入。制作规格见第 5 节。
- 用户要求在主项目目录工作，并明确要求后续使用 main。本会话后半段的整合工作均在主目录完成。接手时需要协调现有分支上的其他任务。
- Windows 命令使用 PowerShell 7，程序路径为 `C:/Users/Oe_Lee/AppData/Local/Programs/PowerShell/7/pwsh.exe`。
- 修改代码前阅读 `PROJECT_OVERVIEW.md` 和 `CODE_MAP.md`，代码与相关技术文档同步。场景修改通过 Unity Editor API 完成。
- 历史实验、失败案例、基线和冻结哈希保留。看过的场景进入开发或回归集合，独立测试需要事先冻结源码、协议和种子。
- 学校申报保留团队，李海宣 40%、徐臻 35%、张瑞城 25%。学生实际工作与 AI 辅助按事实记录。申报系统仅填写、保存草稿，最终提交由用户处理。

## 2. 接手时的主目录现场

项目根目录为 `D:/Unity Project/RailwayRenaissance`。

本次交接只读核对得到以下状态。

| 项目 | 核对结果 |
| --- | --- |
| 当前分支 | `feat/vn-term-backlog-icons` |
| 当前 HEAD | `e7d54e85f7cb3fb6473e60d76d41bff9e1314e30` |
| HEAD 摘要 | `fix(account): plain wrong-code message; resend cooldown 30s` |
| 本地 main | `bf0759cf9f8281d9077235c8996e1a8afcc258f3` |
| 本轮 PR #11 合并提交 | `72f7826eef85239d2bf32be40498f5931d73ad6d` |
| 祖先关系 | `git merge-base --is-ancestor 72f7826e HEAD` 返回 0 |

PR #11 合并时主目录已切回 main，随后其他任务切换了分支并继续开发。现在存在大量新的未提交改动，涉及 VN 正文、术语、对白/回顾/语音、车辆编辑器脚本、资源 meta、文档及消息总线状态。轨道制作文档也有后续新增内容。

交接文件按这个最新现场记录。恢复 ATO 工作前先确认分支及工作区所有权，再安排 main 上的继续工作。后续改动不能用 PR #11 的验收记录覆盖认定。上轮曾发现主项目 Unity 编辑器正在运行，接手时需要重查编辑器进程、当前场景和未保存状态。

## 3. 已完成的代码与合并

### PR #9

[PR #9](https://github.com/NMDX0721/RailwayRenaissance/pull/9) 已合并，合并提交为 `a3feb4972649ec0d828a72846620cc9bcf1b16c8`。修复后的源分支头为 `e27c95e7ee24c6d0d366ddf1d6748e878ad5a2a2`。

- V3 新状态版本 3 保存 `LastBrakeSwitch`，普通制动切换保持至少 25 个 50 Hz 物理 Tick。预测先模拟剩余保持时间，再评估候选动作。安全干预与到达保持分别统计。
- Engage 先创建完整候选控制器，成功后更新运行字段。恢复检查点先验证控制器、路线与时间关系，再恢复物理状态。
- `CabAtoControllerV3.DecodeCheckpoint` 支持缺少 `LastBrakeSwitch` 的历史版本 1 载荷，保留版本 1 和原有控制策略。
- V3 的带符号停车误差、路线遥测使用 V3 实际任务，关闭 V2 路线开关时仍有效。
- `CabEngineState.ActiveInstance` 提供启用驾驶室实例，供平板机车遥测读取。

主要文件为 `CabSimulationCodec.cs`、`Research/CabAtoControllerV3.cs`、`Research/CabAtoResearchDriver.cs`、`CabEngineState.cs` 和 `Assets/Editor/NF5KAtoResearchReview.cs`。策略与兼容规则见 [ATO_V3_CONTROL.md](ATO_V3_CONTROL.md)。

### PR #11

[PR #11](https://github.com/NMDX0721/RailwayRenaissance/pull/11) 已按用户授权 Squash 合并，合并提交为 `72f7826e`。最终审阅的源分支头为 `680f9269bd9c3b65f4cc85bd3eed3f1f68d782fb`。合并前 CodeQL 全部通过，Codex 最新审阅状态为 completed，未解决发现为 false。

该 PR 整合了当时工作目录中的正式源码、资源、文档，以及此前 46 个平板/音乐开发提交。功能覆盖 VN 本地化、阅读呈现、平板档案迁移、NF5K 设备/冷却细化与打包等。整合说明见 [MAIN_INTEGRATION_20261105.md](MAIN_INTEGRATION_20261105.md)。本地 scratch、编译输出、研究运行归档及竞赛申报备份保留在本地。

审阅期间修复了六项问题。

1. 音乐服务 robocopy 目标路径使用一对引号，支持带空格路径。
2. 打包等待 robocopy 结束，并将退出码 8 及以上报告为构建失败。
3. 歌词校准窗口绑定打开时的歌曲 ID，切歌后关闭，旧回调检查绑定；关闭时暂停调度并清除引用。
4. 电量历史先处理时钟回拨，再执行采样间隔过滤。
5. 本地密码重置在通知投递失败时于页面显示验证码，覆盖关闭短信通知的情况。
6. EQ 旁路达到或超过输出奈奎斯特频率的频段，输出采样率无效时旁路 EQ。

后续任务已继续修改账号代码，包括 30 秒重发间隔。当前源码以最新提交和差异为准。

## 4. 验证证据和适用范围

| 检查 | 本轮结果与证据 |
| --- | --- |
| 整合后源码编译 | 220 个运行时源码、76 个编辑器源码编译成功。使用 Roslyn 与缓存 Unity 引用。报告在 `tools/compile/main-integrated-20261105/result.json` |
| 两轮修复源码编译 | `tools/compile/pr11-review-final-20261105/result.json` 与 `tools/compile/pr11-second-review-20261105/result.json` 均记录运行时和编辑器 exit_code 0 |
| 历史载荷兼容 | `V3_LEGACY_CHECKPOINT_OK`，54 份历史 V3 控制器载荷恢复通过，原始归档保持原样 |
| V3 回放 | `V3_REPLAY_OK`，四阶段检查点回放与 30/60/144 FPS 固定 Tick 调度检查通过 |
| 独立适配器检查 | 本轮此前记录 `PR9_V3_ONLY_ROUTE_OK`、`PR9_ADAPTER_HEADLESS_OK`，包含 V2 开关关闭后的 V3 目标误差 |
| 开发名义任务 | 平稳/省气/准点停车误差约 -2.02/-2.27/-2.07 米，运行记录中无速度违规或干预。这三次结果属于开发案例 |
| 档案与剧情静态检查 | `ARCHIVE_UI_RETIRED_OK` 和 `PROLOGUE_STATIC_FLOW_OK`，分别覆盖旧档案 UI 清理和十章静态条件流程 |
| 打包、电量、EQ 回归 | `tools/compile/pr11-regression/result.log` 记录四个 `PR11_*_OK` 标记 |

聚焦回归在最小 Unity API 替身下执行实际源码，测试工程位于 `tools/compile/pr11-regression/`。其中包含真实 robocopy 带空格路径复制、锁定源文件导致失败、持久化未来电量采样恢复、八种 8–96 kHz 采样率与七种 EQ 预设的极点和有限输出检查。歌词校准及账号页面交互仍需 Unity 验证。

另有 54 次已查看工况的控制器回归记录及 27 对重复指纹，详见 `ATO_V3_CONTROL.md`。它们与上表的 54 份历史载荷恢复分别验证控制表现和存档兼容。独立测试、完整工况可靠性、实车参数标定及安全论证继续待做。

上述编译报告记录的是当时源码哈希。接手现场的新增改动需要按自身变化重新验证。

## 5. 建模文件与后续轨道进展

- [铁轨制作要求](../../参考资料/系统/ATO试验线路制作要求.md) 包含米制坐标、1.435 米轨距、65 毫米轨头对应的 1.500 米轨中心距、里程标记与交付要求。
- [列车模型要求](../../参考资料/系统/ATO列车模型接入要求.md) 包含当前候选轮半径 0.461222 米、12.2 米转向架中心距、2.2 米轴距、节点层级、轴心、车门及操纵件要求。

原始建议采用水平单线，里程 s=-60 至 1200 米，任务起点为 0，停车目标为 900 米，保留 300 米后段。列车按现有 NF5K 节点装配基准制作，车头朝局部 -Z，轮对绕局部 X 旋转。列车参数属于原型接入基准。

交接时发现后续任务已增加轨道套件。这部分是本次交接的新现场信息。

- 已存在 `tools/ato_track/build_track_module.py`、`Assets/Models/ATO` 的六个 FBX 模块和 `Assets/Editor/AtoTestLineBuilder.cs`。
- 模块包括平直 25 米、5 米以及 ±10‰、±20‰ 的 25 米坡度件。最新制作文档约定 0.625 米轨枕间距，并说明其与早先 0.6 米参考值的区别。
- Unity 菜单为 `Railway/ATO/Build Track Kit` 与 `Railway/ATO/Build Test Line`。场景路径为 `Assets/Scenes/AtoTestLine.unity`、`Assets/Scenes/AtoTrackKitDemo.unity`。
- 已核对 `tools/compile/ato_Batch.log` 中的 `ATO_KIT_OK prefabs=6`、52 件测试线 `span=-60..1200`、23 件演示线和两份 `ATO_SAVED` 记录。另有 `ato_Shots.log` 与 `tools/ato_track/review/`。本次交接未重新执行生成或复验截图。
- 后续模块文档采用水平投影长度。接入模拟行程时需要明确里程定义与坡度换算，并把坡度输入传入物理后端。

## 6. 最高优先级的未完成工作

### 路线几何与后端统一

本次交接重新核对源码，以下三个旧约束仍存在。

- `CabReviewTrack.Start=-60`，`End=500`。
- `NF5KRailMotionReview` 道床生成循环固定到 560 米。
- `CabRailMotion.ProtectReviewRouteEnd` 按旧检查线边界请求紧急制动。

标准 `AtoRouteTask` 则在起点后 900 米停车，目标运行时间 120 秒、容差 5 秒，限速段为 0/250/650 米，对应 12/18/10 m/s。新套件的渲染长度已达到 1200 米，但车辆循迹、后端坡度和末端保护仍待接入。

接手时先将路线采样、轨道几何、标记、后端环境输入和末端保护建立一致的数据来源。保留原 180 米半径检查线的回归用途，具体接口选择需记录在技术文档中。

### 正常帧循环与人工接管验收

`NF5KAtoResearchReview.Build` 从 `CabRailMotionReview` 复制独立 ATO 场景，菜单为 `RailRevival/Research/Build ATO Review`。驱动默认 `UseV3=false`，验收时明确选中 V3 并记录配置。

`NF5KAtoResearchReview.Batch` 会进入 Play 检查，但其名义任务测试主要通过同步 `Advance` 循环推进物理。完整 3D 验收还需要正常帧循环覆盖 `LateUpdate` 的姿态投影与末端保护。

接线应完整检查以下路径。

```text
CabAtoResearchDriver
  → BeforeSimulationStep 按 Tick 调度
  → CabEngineState 既有指令与联锁
  → CabSimulationDriver / CabSimulation 50 Hz 状态
  → CabRailMotion 车体、转向架与轮对
  → 界面读数、检查点与运行证据
```

验收一趟 900 米任务，记录带符号停车误差、速度违规、普通制动切换间隔、安全干预、人工接管、暂停恢复与检查点恢复。截图或录像同时覆盖驾驶位与外部运动。先验证无 500 米末端保护误介入，再比较三目标控制表现。

## 7. Unity 启动经验

历史隔离重试先遇到 UPM 的 undefined path 错误。给子进程补齐 `USERPROFILE`、`LOCALAPPDATA`、`APPDATA`、`ALLUSERSPROFILE`、`ProgramData`、`HOMEDRIVE` 和 `HOMEPATH` 后已越过该错误，后续在获取 Burst 1.8.30 时中断。日志在 `scratch/pr9-ready-20261104/ato-unity-*-20261105.log`。

后续轨道任务已有主目录 Unity 批处理生成记录，因此应从当前主项目重新核验环境。已打开编辑器时优先协调使用现有实例和场景状态。异步进入 Play 的批处理入口省略 `-quit`，以实际验收标记、错误日志和运行证据判断完成。

## 8. 接手后的执行顺序

1. 核对当前分支、HEAD、工作区、Unity 进程和场景，协调 VN 等正在进行的任务。
2. 阅读两份建模文档及新轨道套件源码，明确水平里程与实际行程、轨顶和车辆参考点的映射。
3. 接通新轨道、采样、坡度环境与末端保护，完成正常帧的 V3 900 米闭环和人工接管。
4. 重跑与变更相关的 .NET 回放和 Unity 验收，按冻结协议保留版本、配置、源码哈希、命令与失败案例。
5. 再冻结新的独立测试，更新竞赛报告、图表、演示和 AI 辅助记录。

竞赛本地材料入口为 `docs/research/competition_revision_20261101/`，其中已有匿名报告、图片附注、文献比较底稿、AI 辅助记录、专项强化作业与团队执行答辩材料。当前内容与后续数据之间的对应关系由接手者核对，申报系统最终提交继续遵循用户约定。

## 9. 复查入口

在项目根目录用 PowerShell 7 执行以下只读检查与已有测试。

```powershell
git branch --show-current
git status --short
git log -1 --oneline
dotnet run -c Release --project research/ato-v3/AtoV3.csproj
python tools/vn/compile_review_sources.py --help
```

编译 helper 支持重复传入 `--editor-source`，完整编辑器编译需要枚举全部相关编辑器源码。聚焦回归输出保存在本地 tools/compile，具体复跑命令为 `dotnet run -c Release --project tools/compile/pr11-regression/Regression.csproj`。复跑会执行 robocopy 并更新该测试输出目录，应在当前工作区允许该操作时进行。

本交接文档由本会话新建，保存于本地，等待后续正常提交安排。
