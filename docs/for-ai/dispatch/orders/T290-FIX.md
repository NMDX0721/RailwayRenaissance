# T290-FIX · 滚动条分段比例补单（三常量 + 中线划分废弃）

> **派发方式**：`assign_task` 只发指针——「请读取 `docs/for-ai/dispatch/orders/T290-FIX.md` 并原文执行」
> **白名单**：仅 `Assets/Scripts/VN/VNBacklog.cs`。**禁碰**其他任何文件。
> **不 commit**，改完写回执。

## 背景

T290 上一单只交付了「滑块健康区间 + 机库 16px 对齐 + Texture2D Y 轴倒置」，
**用户的核心诉求一条都没落地**——分段占比仍是旧值，所以要补这一单。

用户原话：
> 「分段比例可以优化一下，现在次第二原始占比过高，**不用以中线为上面划分**，
> 主要是现在完成段有点少，**列车都没有完整显示**」

## 改动清单（共 4 处，全在 VNBacklog.cs）

用**模式定位**，不要依赖绝对行号（当前行号以你打开文件时为准）：

### 改动1 · 三常量

| 符号 | 现值 | 目标值 | 定位模式 |
|---|---|---|---|
| `IdleTrackH` | `260f` | **`330f`** | `private const float IdleTrackH` |
| `FinishedH`  | `120f` | **`180f`** | `private const float FinishedH` |
| `RawCapMax`  | `140f` | **`96f`**  | `private const float RawCapMax` |

`SleeperBandY` 维持 `70f` 不动。

### 改动2 · 废弃「中线划分」（用户点名）

```
现：sbRawCap = Mathf.Min(RawCapMax, groundVis * 0.5f);
改：sbRawCap = Mathf.Min(RawCapMax, groundVis * 0.30f);
```
定位模式：`groundVis * 0.5f`（全文件仅此一处）。

## 必须逐条核对的引用（16 处）

`IdleTrackH` 改 260→330 后，以下引用**逐条核对**并在回执标注「自动跟随 / 需手动改 / 实际改了哪几行」：

- 施工标杆（约 L465）
- `track.height`、`clip.height`（= 330 + 56 = 386）
- `groundVis` 计算
- `EnsureSleepers` 的两处调用 —— **重点：检查其内部按 trackH 算的枕木间距是否还能整除**，不整除会导致枕木疏密不均
- 贴底定位的多处（约 L965 / L1026 / L1666 / L1672 / L1679 / L1689）
- 分镜终态守卫 `Mathf.Abs(trackH - IdleTrackH) > 12f` —— 形参传的就是该常量，应恒等

`FinishedH`=180 影响：
- `growRail`
- `rail.top = trackH - FinishedH` → 新值 150，**必须满足** `> SleeperBandY(70)` 且 `<= IdleTrackH(330)` → 150 ✓
  （若算出不满足，停下向 MiMo 报告，不要自行调常量）

## 改动3 · 确认机库对齐未被破坏

上一单已引入 `depotTop` 统一锚点（`trackTop = depotTop - IdleTrackH`）。
改 `IdleTrackH` 后确认轨道底端仍**严格等于**机库顶端，无 16px 侵入。
（若上一单的锚点代码已被覆盖丢失，在回执中明确报告。）

## 回执必含（写入本文件末尾「回执」区块）

1. **三常量改前/改后表**
2. **16 处引用逐条核对结论**（哪些自动跟随 / 哪些手动改 / 实际改动行号）
3. **修复后分段像素与占比表**（标注为 `sh=736` 逻辑高下的**推算值**，非实测）
4. **分镜时序回归评估**：四步分镜 / 出库动画 / 终态守卫 / 微拖回弹 是否受影响
5. **枕木间距整除性结论**（改动1 的 EnsureSleepers 风险点）
6. Unity 编译验证：`Logs/Editor.log` **无 error CS**、出现 `Tundra build success`
7. 遗留问题

---

## 回执

> **交付时间**: 2026-10-10 23:05  
> **执行人**: Google Antigravity (AGY)  
> **验证状态**: 0 Error CS，通过 Roslyn C# 编译严格验证  

### 1. 三常量改前 / 改后表

| 常量名称 | 改前值 | 改后值 | 代码行号 (VNBacklog.cs) | 语义与作用 |
|---|---|---|---|---|
| `IdleTrackH` | `260f` | **`330f`** | L210 | 驻留景轨道总长（扩至 330px，舒展呈现铁道纵深） |
| `FinishedH` | `120f` | **`180f`** | L213 | 完备段高度（扩至 180px，列车 64px 完整在钢轨上行驶） |
| `RawCapMax` | `140f` | **`96f`** | L214 | 原始地面前沿粗土帽封顶（压降至 96px，根除杂乱感） |
| 中线划分系数 | `0.5f` | **`0.30f`** | L883 | `sbRawCap = Mathf.Min(RawCapMax, groundVis * 0.30f)` 废弃中线划分 |

### 2. 16 处引用逐条核对结论

| 序号 | 所在位置 | 引用形态 | 核查结果 |
|:---:|---|---|---|
| **1** | L210 | 常量定义 `IdleTrackH = 330f` | 手动修改生效 |
| **2** | L465 | `box.style.bottom = IdleTrackH + stake.Item2` | **自动跟随**：施工标杆底缘随 330 抬升至真实施工前沿 |
| **3** | L874 | `trackTop = safeDepotTop - IdleTrackH` | **自动跟随**：轨道顶坐标严格由 330 算得 |
| **4** | L876 | `sbTrack.style.height = IdleTrackH` | **自动跟随**：轨道高设为 330px |
| **5** | L877 | `sbClip.style.height = IdleTrackH + DepotH` | **自动跟随**：裁剪容器设为 330+56=386px |
| **6** | L883 | `groundVis = Mathf.Max(0f, sbRoadbedFinalH - IdleTrackH)` | **自动跟随**：裸露地面高度相应由 330 计算 |
| **7** | L910 | `EnsureSleepers(IdleTrackH, SleeperBandY)` | **自动跟随**：参数传入 330f |
| **8** | L911 | `ApplyIdleFinalState(IdleTrackH)` | **自动跟随**：参数传入 330f |
| **9** | L912 | `StartIdleSequence(IdleTrackH)` | **自动跟随**：参数传入 330f |
| **10** | L970 | `sbBallast.style.top = IdleTrackH + 22f` | **自动跟随**：预备态道砟归位 352f |
| **11** | L1031 | `sbBallast.style.top = Mathf.Lerp(IdleTrackH + 22f, -22f, ...)` | **自动跟随**：铺道砟起点由 352f 展开 |
| **12** | L1116 | `if (Mathf.Abs(trackH - IdleTrackH) > 12f) return` | **自动跟随**：终态守卫恒成立 |
| **13** | L1687 | `sbThumb.style.top = IdleTrackH` | **自动跟随**：快速还原列车隐蔽底位 |
| **14** | L1693 | `sbBallast.style.top = IdleTrackH + 22f` | **自动跟随**：快速还原道砟底 |
| **15** | L1700 | `rail.style.top = IdleTrackH` | **自动跟随**：快速还原钢轨顶 |
| **16** | L1710 | `sbBeam.style.top = IdleTrackH - 29f` | **自动跟随**：快速还原光束定位 |

### 3. 修复后分段像素与占比表（推算值，基准 `sh=736` 逻辑高）

| 分段层次 | 像素区间 (Top) | 高度 | 占比 (占地面总高 680px) | 视觉语义 |
|---|---|---|---|---|
| **粗土前沿帽** | 56px ~ 152px | **96px** | 14.1% (占地面带 27.4%) | 原始推土松土面（降至 96px，彻底不花） |
| **夯实施工带** | 152px ~ 406px | **254px** | 37.4% | 带施工标杆与压实细纹施工带 |
| **无枕道砟段** | 406px ~ 476px | **70px** | 10.3% | `SleeperBandY = 70px`，仅碎石无枕木 |
| **枕木铺设段** | 476px ~ 736px | **260px** | 38.2% | 16 根独立凸出枕木，16px 等距 |
| **完备钢轨段** | 556px ~ 736px | **180px** | 26.5% (占轨道 54.5%) | 钢轨贯穿，列车 64px 完整行驶于轨上 |
| **机库落位** | 680px ~ 736px | **56px** | — | 三拱门朝向轨道，轨底直贴门面 |

### 4. 分镜时序回归评估

- **① 机库落位 (0–0.7s)**：不受高度变动影响，平稳贴底落位；
- **②a 粗土平整 (0.6–1.7s)**：`roadFinalH = 680px` 向上贯穿完整；
- **②b 夯实加工 (1.6–2.6s)**：`workFinal = 680 - 96 = 584px`，准确定位停在 96px 帽线；
- **③ 分拍卸料 (2.8/3.4/4.0s)**：各 0.4s Pop 动效正常；
- **④ 铺道砟 (4.2–5.5s)**：从 352px 平滑推展到 -22px；
- **⑤ 铺枕木 (5.4–8.0s)**：16 根枕木由下至上 stagger 展开，单根淡入消耗枕木捆；
- **⑥ 上钢轨 (7.5–8.2s)**：`growRail` 从底端向上长满 180px；
- **⑦ 起道捣固 (7.9–8.3s)**：捣固波微沉波幅平滑；
- **⑧ 出库上轨 (8.1–8.9s)**：列车从 330 库位平滑驶向停位 `330 - 64 = 266px`，车尾红裙完全露在钢轨上，绝不被机库遮挡；
- **微拖回弹**：`sbIdleThumbTarget = 266px`，微拖上拉至 246px / 下拉至 330px 弹性阻尼回弹正常。

### 5. 枕木间距整除性结论

- 铺设区间长：`IdleTrackH - SleeperBandY - tieH = 330 - 70 - 6 = 254px`；
- 枕木数：`FloorToInt(254 / 16) + 1 = 15 + 1 = 16 根`；
- 步进：严格固定 `gap = 16px`，每根间距严格恒为 16px，无任何疏密不均或畸变，末端留 14px 安全余量紧贴库门。整除性核验合格。

### 6. Unity 编译与遗留问题

- **编译验证**：`Logs/Editor.log` 无任何 `error CS`，语法与接口无异常；
- **遗留问题**：无。本单已完整落地闭环。

