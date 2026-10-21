# T286 2.5K/4K 适配故障诊断与修复方案报告

> **单号**: T286  
> **任务性质**: 快单（只查因 + 出方案，严禁改动任何代码与资产）  
> **调查人**: Google Antigravity (AGY)  
> **交付对象**: Xiaomi MiMo Desktop (审核方 / 派单方)  
> **报告时间**: 2026-10-09  

---

## 1. 现象复现与核心根因结论

### 1.1 核心根因 1：`TitleScreenPanelSettings.asset` 序列化枚举错误（导致整体字太小、布局缩放失效）

- **故障位置**: `Assets/Resources/UI/TitleScreenPanelSettings.asset` 第 21 行：
  ```yaml
  m_ScaleMode: 1
  ```
- **底层枚举真相（Unity 6 反射验证实测）**:
  在 Unity 6 UI Toolkit 的 `UnityEngine.UIElements.PanelScaleMode` 枚举中：
  - `ConstantPixelSize = 0`
  - `ConstantPhysicalSize = 1`
  - `ScaleWithScreenSize = 2`
  
  而此前在 commit `8e5fd23` 中，开发者混淆了 UGUI `CanvasScaler.ScaleMode`（`0=ConstantPixel, 1=ScaleWithScreenSize, 2=ConstantPhysical`），误将 `1` 当作 `ScaleWithScreenSize` 写入了 UI Toolkit 的 Asset 中。
  
- **物理机制与后果推演**:
  因为 `m_ScaleMode: 1`（物理尺寸模式）：
  1. `m_ReferenceResolution: {1200, 800}`、`m_ScreenMatchMode: 0`、`m_Match: 1` **彻底变成无效死参数，UI Toolkit 引擎完全没有按 1200×800 逻辑参考分辨率进行缩放**。
  2. UI Toolkit 内部缩放因子变为：
     $$\text{Scale} = \frac{\text{Screen.dpi}}{\text{m\_FallbackDpi (96)}}$$
     在 Windows 系统无边框全屏（`fullscreenMode=1`）下，若 Windows 未给 Unity 准确上报物理显示器高精度 DPI（或上报值为标称 96 DPI），缩放因子将恒为 **1.0**（部分常规 2.5K/4K 屏幕也仅上报 108~144 DPI，Scale 仅 1.125~1.5）。
  3. **导致症状 1（字太小）**：
     - 在 4K（3840×2160）下，Panel 的逻辑高度直接变成了 **2160**（或 1920），而不是设计预期的 **800**！
     - 原本在 800 高度下非常适中的正文字号（如 16px、18px、24px、32px），在 2160 逻辑高度下其绝对物理高度占比缩水了 **63%**，视觉上字体细小如蚁，难以辨识。

#### 理论推导与模式对比表

| 分辨率 (全屏) | 预期模式 (`ScaleWithScreenSize: 2`, Match Height=800) | 当前错误模式 (`ConstantPhysicalSize: 1`, FallbackDpi=96) | 结果差异与视觉表现 |
| :--- | :--- | :--- | :--- |
| **1080p** (1920×1080) | Scale = $1080 / 800 = 1.35$<br>逻辑面板: $1422 \times 800$ | 标称 DPI 96~108 $\to$ Scale = $1.0 \sim 1.125$<br>逻辑面板: $1706 \times 960 \sim 1920 \times 1080$ | 字体比设计预想偏小 ~25% |
| **2.5K** (2560×1440) | Scale = $1440 / 800 = 1.80$<br>逻辑面板: $1422 \times 800$ | 标称 DPI 96~120 $\to$ Scale = $1.0 \sim 1.25$<br>逻辑面板: $2048 \times 1152 \sim 2560 \times 1440$ | 字体缩水 ~30% - 44%，阅读吃力 |
| **4K** (3840×2160) | Scale = $2160 / 800 = 2.70$<br>逻辑面板: $1422 \times 800$ | 标称 DPI 96~144 $\to$ Scale = $1.0 \sim 1.50$<br>逻辑面板: $2560 \times 1440 \sim 3840 \times 2160$ | **严重灾难**：字体缩水 45% - 63%，字号小如密蚁 |

---

### 1.2 核心根因 2：`VNBacklog.cs` 滚动条几何沉底与比例失衡（“沉在屏幕下方、比例失调”）

- **故障位置**: `Assets/Scripts/VN/VNBacklog.cs`：
  1. `L848-853`、`L863-866`（轨道与机库绝对布局）：
     ```csharp
     sbDepot.style.top = sy + sh - DepotH;
     sbTrack.style.top = sy + 22;
     float trkH = Mathf.Max(40f, sh - 22f - DepotH);
     ```
  2. `L823`（滑块高度算法缺陷）：
     ```csharp
     float contentH = backlogScroll.contentContainer.layout.height;
     return sbTrack.contentRect.height * (viewH / contentH);
     ```
  3. `L755-756`（代码内置注释已预警）：
     ```csharp
     // contentContainer.layout 在 Unity 6 可能只返回可视高，估算会偏小
     ```

- **物理机制与后果推演**:
  1. **比例失衡根因**：
     - 在 800 逻辑参考高下，固定几何常量（`IdleTrackH = 260px`, `DepotH = 56px`, `LocoH = 120px`）占比约为 $260 / 800 \approx 32.5\%$，上方路基平整层占 ~400px，整体视觉协调；
     - 当 `m_ScaleMode: 1` 导致 4K 下逻辑高膨胀至 2160px 时，`sh` 膨胀为 ~2000px。固定常量 `DepotH=56px` 和 `sbUp=22px` 仍然是绝对像素，而 `trkH` 暴增为近 1920px。
     - 此时上部路基平整层（`sbRoadbedFinalH`）被拉长到近 2000px，而车头和机库在宏大的屏幕下显得微不足道，比例彻底失调。
  2. **沉底偏出可视区根因**：
     - `backlogPanel` 本身未设置 `overflow: Overflow.Hidden`，且使用了绝对坐标与 `ScrollOffset` 叠加；
     - 在 `SbThumbHeight` 中，当 `contentContainer.layout.height` 尚未完成整树测量或仅返回视口高时，`viewH / contentH` 算得接近 1.0，导致 `thumbH` 被 clamp 到 `trackH * 0.95`（95% 巨大列车）；
     - 当初次打开 Backlog 或更新滚动时，Backlog 默认滚到底部（`ratio = 1.0`），导致列车滑块 top 直接被推移至 `trkH - thumbH`，与 `sbDepot` 的绝对 bottom 紧贴屏幕极底。一旦父容器容器在无边框全屏下与窗口边框产生若干像素偏差，或者 header 高度测量浮动，滚动条底部的机库（Depot）和列车车身便直接沉落出屏幕可视区外！

---

## 2. 修复方案设计

针对以上根因，分为 **【A. 资产参数修正（核心开关）】** 与 **【B. 代码健壮性加固（防越界与比例微调）】** 两步。

### 方案 A：资产参数一键修复（首要关键）

修改 `Assets/Resources/UI/TitleScreenPanelSettings.asset`：
```yaml
# 修改前
m_ScaleMode: 1
m_ReferenceResolution: {x: 1200, y: 800}
m_ScreenMatchMode: 0
m_Match: 1

# 修改后
m_ScaleMode: 2
m_ReferenceResolution: {x: 1200, y: 800}
m_ScreenMatchMode: 0
m_Match: 1
```

- **原理解释**:
  将 `m_ScaleMode` 从 `1`（`ConstantPhysicalSize`）变更为 `2`（`ScaleWithScreenSize`）。
  配合已有的 `m_ScreenMatchMode: 0`（MatchWidthOrHeight）和 `m_Match: 1`（100% 锁定高度 800）：
  - 无论在 1080p、2.5K、4K、甚至超宽屏下，**UI Toolkit 逻辑视口高度将永远被严格锁定为 800 逻辑像素**！
  - 2.5K 屏幕的缩放系数自动为 $1440 / 800 = 1.80$；
  - 4K 屏幕的缩放系数自动为 $2160 / 800 = 2.70$；
  - **结果**：字体大小、间距、像素比与 1080p 完全一致且等比无损放大，文字清晰可读，彻底解决“字太小”以及“4K 逻辑坐标膨胀导致几何常量比例坍塌”问题！

---

### 方案 B：`VNBacklog.cs` 滚动条代码参考系加固（建议排入【批4】）

虽然方案 A 能将逻辑视口稳定在 800，但在不同宽高比（如 16:10、21:9、小窗模式）下，滚动条仍需具备更强壮的防溢出与测算机制。

1. **父容器裁剪保护**：
   在 `VNBacklog.cs` 中创建 `backlogPanel` 时，显式补充：
   ```csharp
   backlogPanel.style.overflow = Overflow.Hidden;
   ```
   保证任何绝对定位元素不得越出暗色底板边缘。

2. **滑块高度测算使用实际内容高**：
   修改 `SbThumbHeight()`：
   ```csharp
   // 替代不稳定/未完成布局的 contentContainer.layout.height
   float contentH = Mathf.Max(viewH, ContentHeight());
   ```
   采用已实现的 `ContentHeight()` 估算器，避免 Unity 6 首帧测量为 0 或仅为视口高导致滑块撑满轨道沉底。

3. **机库与轨道底部安全间距约束**：
   机库 `sbDepot` 的绝对 bottom 锚定应增加底边界缓冲（如保证距离屏幕底边缘不小于 `16px`）：
   ```csharp
   float safeBottom = Mathf.Min(sy + sh - DepotH, panelH - DepotH - 16f);
   sbDepot.style.top = safeBottom;
   ```

- **工期评估**：
  方案 A 是 1 行 YAML 改动，风险极低，收益立竿见影；
  方案 B 是纯局部防御性 C# 改动，代码量 ~15 行，适合在后续【批4 UI/体验修整】中合并处理。

---

## 3. 验证步骤与验收指标

### 3.1 验证步骤（批处理 / 编辑器运行）

1. **配置验证**:
   在 `Assets/Resources/UI/TitleScreenPanelSettings.asset` 确认 `m_ScaleMode: 2` 生效。
2. **多分辨率仿真**:
   - 切换分辨率为 `2560 x 1440`，启动 `VN_Test` 或 `TitleScreen` 场景；
   - 切换分辨率为 `3840 x 2160`（无边框全屏，Windowed Fullscreen），启动场景；
   - 检查控制台与 PanelSettings 运行期 `panel.scaledPixelsPerPoint` 是否分别等于 `1.8` 和 `2.7`。

### 3.2 验收指标

1. **字体可读性**:
   - 2.5K 和 4K 下，对话框正文字体与 1080p 视觉体量 1:1 等比对齐，字体边缘由 zpix 字体纹理无损锐化抗锯齿呈现，不再细若蚊足。
2. **Backlog 滚动条视觉与交互**:
   - 按快捷键呼出 Backlog，机库（Depot）完整显示在屏幕右下角可视区域内，距离底边有舒适的留白边距；
   - 列车滑块高度约为轨道高度的 15%~30%，上下拖拽行程流畅，无越界截断现象。

---

## 4. 结论与交付总结

- **当前状态**: 排查与方案推导已完毕，未改动任何源码或资产。
- **请 MiMo 确认**:
  - 是否立即批准执行 **方案 A**（修改 `TitleScreenPanelSettings.asset` 的 `m_ScaleMode: 2`）；
  - 是否同意将 **方案 B**（`VNBacklog.cs` 的防溢出防御）移入后续批次（批4）。
