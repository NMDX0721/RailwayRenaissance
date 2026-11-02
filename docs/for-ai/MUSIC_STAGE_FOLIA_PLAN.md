# 舞台模式 Folia 深度模仿计划（2026-10-31，草案）

## 0. 先说清楚"舞台"指什么

Folia 的"舞台"不是一个效果，而是一组可切换的歌词样式（`src/components/visualizer/<mode>/`，注册表自动发现）。参考仓库（`.references/folia-major`，AGPL-3.0，HEAD d7bce03）里共 14 个：

| 序 | id | 显示名 | 参考实现规模 | 渲染方式（按目录名/规模判断，未逐个读过） |
|---|---|---|---|---|
| 10 | sonnet | 商籁 | ~11.3k 行/59 文件 | Pixi，日式 MG 歌词 PV 导演 |
| 20 | tempera | 凝彩 | ~11.6k 行/56 文件 | Pixi，色块分割构图 PV 导演 |
| 25 | lumiere | 绘光 | ~11.7k 行/65 文件 | 最大，独立设置面板 |
| 30 | classic | Luminous | ~0.5k 行/3 文件 | DOM+framer-motion，逐词 GlowWord |
| 40 | cadenza | Mindscape | ~1.8k 行 | 包络/碎片/叠层节点 |
| 50 | partita | 云阶 | ~0.8k 行 | 与 classic 共用逐词时序 |
| 60 | fume | 浮名 | ~4.5k 行 | Pixi |
| 70 | tilt | Tilt | ~0.8k 行 | |
| 80 | claddagh | Claddagh | ~1.3k 行 | |
| 90 | monet | Monet | ~3.9k 行 | 逐字符偏移布局 |
| 100 | pendolo | Pendolo | ~3.7k 行 | |
| 110 | cappella | Cappella | ~2.2k 行 | |
| 120 | diorama | 镜台 | ~6.8k 行 | three.js |
| 130 | still | Still | ~0.1k 行 | 静态 |

另有跨样式共享层：`VisualizerBackdrop`（背景/视频/透明度，换样式时不重来）、`VisualizerSubtitleOverlay`（译文 + 下一句预览，字号统一来源）、`VisualizerHarmonyOverlay`（TTML 背景人声）、`runtime.ts`（当前行/上一句/下一句/预热窗口）、歌词管线（renderHints、cjkSemanticLayout、graphemeTiming）。

我们现在的 `MusicStageLyrics`（214 行）+ `MusicLyricUnits`（190 行）只对应 classic 的一个子集。**结论：我们没有"样式系统"，只有一种样式。**

## 1. classic 对照：已有 / 缺口

已有：布局单元（语义/粘连）、waiting/active/passed 三态弹簧、逐字符点亮、副歌涟漪、快慢行 reveal、间奏呼吸点、按音频能量的整行呼吸、暂停冻结/定位重建、真实词时间才逐词。

缺口（按对观感影响排序）：
1. **量字宽的布局**：Folia 用字宽 × 激活缩放精确算词间距，避免放大时重叠；我们是固定位置。
2. **每行确定性随机版式**：以行起始时间为种子决定整行对齐方式（起/中/末/分散）、透视、每词偏移/旋转，`intensity` 控制狂暴程度。
3. **整行入场/离场动画**（`getGlowWordLineContainerMotion`），不是简单淡入。
4. **GlowWord 体变体**（`glowWordVariants`）：发光、本体、涟漪的分层，而非仅文字颜色+阴影。
5. **调节项**：词旋转开关、呼吸浮动倍率、词间距、旧版布局开关；每曲/全局持久化。
6. **音频分频段**：Folia 的 `audioBands`；我们只有 RMS。
7. **字幕层**：译文与下一句预览（可模糊），字号两处来源统一。
8. **背景人声层**（TTML）：网易 YRC 没有这类数据，暂不做。

## 2. 架构：先建样式系统，再填样式

```
IStageStyle { Id; Build(ctx); Tick(ctx, dt); Dispose(); }
StageContext { 文档, 活跃行, 上一句/下一句, 音频时钟, 能量+分频段, 调色板, 字号倍率, 减少动态, 译文 }
StageStyleRegistry  → 样式 id、排序、显示名、预览、调节项定义
```
- 现有 `MusicStageLyrics` 改名迁为第一个样式 `Luminous`，行为不变，作为回归基线。
- 背景（`MusicReactiveStageView`）与样式分离，换样式时背景保持，对应 Folia 的 Backdrop。
- 样式选择与各样式调节项写入音乐 JSON（沿用现有 `lyricMotion` 做法，向后兼容）。
- 样式选择器：用真实样式现画的缩略预览，不准备静态图。
- 许可：AGPL。只对照行为和算法，**独立写 C# / UI Toolkit**，不复制 TS/样式；沿用 `MUSIC_FOLIA_CORE_REVIEW.md` 的表述，不称"Folia 移植版"。

## 3. 分阶段

**阶段 0（先做，约 0.5 天）**：逐个读 Tier A 样式源码，记录行为清单（本文第 0 节的"渲染方式"目前是推断，要核实）；确认 UI Toolkit 能否承载每种效果。

**阶段 1：样式系统 + Luminous 补齐**
- 抽 `IStageStyle`/注册表/上下文，迁移现有舞台，回归 36 步全绿。
- 补缺口 1–5、7：量宽布局、行级确定性版式、入场离场、体变体分层、调节面板、字幕层。
- 缺口 6：音频分频段（先用 Unity `GetSpectrumData` 做低/中/高三段）。
- 验收：隔离快照下的逐帧截图序列（沿用 reactive-stage 的 16 帧预览做法）+ 暂停冻结/定位重建断言。

**阶段 2：低成本样式（DOM 型）**：partita（云阶，共用逐词时序）、tilt、still。三者合计约 1.7k 行参考实现，UI Toolkit 可直接承载。

**阶段 3：中等样式**：cadenza、claddagh、cappella、pendolo。需先评估是否要引入 Painter2D 自绘层或 Mesh。

**阶段 4：重型样式（单独立项，需用户拍板）**：monet、fume、diorama、lumiere、sonnet、tempera。多为 Pixi/three.js/着色器，参考实现各 4–12k 行；在 Unity 里需要 RenderTexture/自定义着色器路线，成本和风险高一个量级，不建议和前面混在一起估。

## 4. 每阶段的验收口径（沿用既有做法）
- 实际平板入口、独立曲库、1440×900 与 1024×768 截图。
- 动效用逐帧序列验证，不只看静帧；暂停冻结、定位重建、减少动态都要有断言。
- 只用真实词时间画逐词效果，不为"好看"造假时间。
- 不宣称完整工作树编译通过，除非真的跑过。

## 5. 风险与待定
- 参考实现规模很大，**14 个样式全做不现实**；需要用户定范围。
- 阶段 4 的渲染路线（RenderTexture + 着色器 vs 放弃该类样式）待定。
- 分频段需要实机听感验证，自动测试只能证明数值在变。
- 目前只通读了 classic，其余样式只看了规模和入口注释。
