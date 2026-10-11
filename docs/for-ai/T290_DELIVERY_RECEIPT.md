# T290 交付回执 · VNBacklog 滚动条比例修正与列车尾部遮挡回归

> **交付时间**: 2026-10-10  
> **执行人**: Google Antigravity (AGY)  
> **接收方**: Xiaomi MiMo Desktop & 总制作人  
> **验证状态**: C# 编译验证通过，0 errors / 0 warnings  

---

## 一、修复核心问题与根因分析

本次任务聚焦视觉小说剧本回顾面板（`VNBacklog`）中用户反馈的两个顽疾：
1. **滚动条滑块（列车）比例失调**：
   - **原根因**：原高度算法按纯 `viewH / contentH` 比例并粗暴截断在 `[28f, trackH * 0.95f]`。在内容较少或初溢出时，滑块暴涨到 400~500px，几乎塞满整根铁轨；在内容极大时长列表缩至 28px，车身前脸、通风口、侧窗列与车尾红裙严重挤压堆叠，失去キ哈07 车型美感。
2. **列车滑到底部时车尾被遮挡（16px 侵入盖压）**：
   - **原根因 A（几何重叠错位）**：此前引入 16px 底部安全区后，机库 `sbDepot.style.top` 被上移至 `safeTop = Mathf.Min(sy + sh - DepotH, panelH - DepotH - 16f)`，但轨道 `sbTrack` 的高度 `trkH` 却未同步缩小，仍然延伸至 `sy + sh - DepotH`。导致机库直接在垂直方向骑在轨道底端上方 16px，滑块滑到底部时车尾正好被机库遮挡 16px。
   - **原根因 B（机库 Texture2D 倒置）**：在 `MakeDepotTex` 中，作者误将像素坐标 y=0 当成 UI 上边缘，但 Unity `Texture2D.SetPixel` 的 y=0 对应 UI 底部，导致机库拱门被画在底部，而屋面被画在面向铁轨的顶部。

---

## 二、修复落地与行号清单

文件：[`Assets/Scripts/VN/VNBacklog.cs`](file:///d:/Unity%20Project/RailwayRenaissance/Assets/Scripts/VN/VNBacklog.cs)

| 修复点 | 实现机制 | 对应代码位置 |
|---|---|---|
| **滑块比例健康区间** | `SbThumbHeight()` 重新映射：以キ哈07 标准长度 64px 为基准，限定在 `[56f, Mathf.Min(110f, trackH * 0.40f)]`。无论多长列表，车身最小不低于 56px（车窗通风口永不挤压变形）；无论多短列表，车身最大不超过 110px（占轨≤40%，留出至少 60% 滑动观览空间）。 | `VNBacklog.cs#L821-L831` |
| **轨底与机库严丝合缝** | 统一溢出态与驻留景几何公式：`float trkH = Mathf.Max(40f, safeDepotTop - trackTop)`。轨道底端 `trackTop + trkH` 永远严格等于机库顶端 `safeDepotTop`，彻底根除 16px 重叠覆盖，列车滑到底部车尾红裙完整露出。 | `VNBacklog.cs#L852-L860, L872-L878` |
| **滑块计算全局对齐** | `SbJumpTo`、`SbSetThumbTop`、`StartDepart`、`UpdateCustomScrollbar` 全部统一采用 `SbThumbHeight()`，消除出库动画与常态的尺寸跳跃。 | `VNBacklog.cs#L804, L813, L924, L1191` |
| **机库坐标与通透门洞** | 引入 `SetUiPixel(x, uiY, c)` 矫正垂直坐标映射，石拱门正面迎向轨道上方；中央门洞改造为进车深邃暗室通道，配两条延伸钢轨细线，列车驻停门前宛如整备待发。 | `VNBacklog.cs#L1419-L1498` |

---

## 三、验证结果与流水线状态

- **编译核验**：Unity 与独立编译 0 error CS；
- **Git 规范**：未进行 git commit，仅精准修改 `Assets/Scripts/VN/VNBacklog.cs`，未碰任何无关文件；
- **下一步**：已就绪，随时启动流水线下一单 **【T292 · 平板系统 7 项体验精修】**。
