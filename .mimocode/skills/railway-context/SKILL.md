---
name: railway-context
description: Project context for RailwayRenaissance (铁路复兴：沙能冲击). Use when working on this project to quickly locate files, understand architecture, and avoid re-exploring known paths.
---

# RailwayRenaissance Project Context

## Key Paths

| What | Path |
|------|------|
| Unity project root | `D:/Unity Project/RailwayRenaissance` |
| Scripts | `Assets/Scripts/` |
| VN scripts (JSON) | `Assets/Resources/Scripts/` |
| **所有游戏文档** | **`参考资料/`**（统一读取/创建路径） |
| 文档导航 | `参考资料/INDEX.md` |
| **设定** | `参考资料/设定/`（世界观/角色/经济/时间线） |
| **引擎** | `参考资料/引擎/`（五大核心系统） |
| **剧情** | `参考资料/剧情/`（plot-outline/序章/小说） |
| **系统** | `参考资料/系统/`（平板/VN/科技树/区域等） |
| **资产** | `参考资料/资产/`（AI提词/音色/UI参考） |
| **管理** | `参考资料/管理/`（分工/进度/审计/报告） |
| AI helper docs | `docs/for-ai/` |
| Portfolio | `docs/portfolio/` |
| Tablet desktop UI | `Assets/Scripts/GameMainUI.cs` |
| Settings panel | `Assets/Scripts/StationBulletinUI.cs` |
| Game data | `Assets/Scripts/GameData.cs` |
| VN manager | `Assets/Scripts/VN/VNManager.cs` |

## Architecture

- **Unity 6000.5.8f1** with UI Toolkit (UIDocument)
- **Two UI layers**: GameMainUI (tablet desktop, screen-space overlay sortingOrder=200) + StationBulletinUI (game settings)
- **Save system**: PlayerPrefs-based
- **Code style**: 4-space indent, no comments unless non-obvious why

## Character Quick Ref

| Name | Role | First Appearance |
|------|------|-----------------|
| 林彪悍 (lin_biaohan) | 主角，25岁研究生 | prologue_01 |
| 老陈 (laochen) | 68岁，守线四年 | prologue_01 (phone) |
| 岁月 (suiyue) | AI助手，13岁少女形态 | prologue_02 |
| 嘉颖徐 (jiaying_xu) | 铁路大亨，资助者 | prologue_02 |
| 张工 | 退休机械师，62岁 | prologue_06 |
| 李阿姨 | 财务/站务，55岁 | prologue_06 |
| 王小弟 | 杂工，22岁 | prologue_06 |
| 赵师傅 | 线路维护，55岁 | prologue_06 |
| 小芳 | 志愿者，45岁 | prologue_06 |
| 赵铁山 | 铁路监督员 | post_002 |
| 陈鹤年 | 市长 | post_004 |
| 周鼎铭 | USET会社长，镜像反派 | post_006 |

## Timeline Quick Ref

- 2050: 沙能诞生
- 2053: 岁月AI制造，0721号出厂，小米Pad 36 Ultra发布
- 2056: 铁路客运量暴跌，0721号最后运行
- 2072: 全球铁路停运
- 2076: 游戏开始（Day 0）
- 2077: 沙鲸计划预计面市（deadline）

## Design Decisions (Fixed)

- **爷爷遇难**: 纯意外，不揭晓真相，各方各执一词
- **周鼎铭倒戈**: 动态浮动条件，根据创世核种子+亲密度变化
- **嘉颖徐动机**: 纯粹看好林彪悍，不求利益
- **岁月身世**: 保持模糊，三种解释并存
- **0721封存**: 2053年制造后封存，23年一致

## App System (Tablet)

**三分法（2026-09-10定稿）：**
- **System (小米真名)**: 相册/音乐/笔记/设置/浏览器/安全中心/米家/应用商店
- **Korean (保留原名)**: 阿里郎商店/未来网/白头疫苗（替换对应系统App）
- **Third party (嘉颖徐基金)**: RDA(铁路行车调度助手)/归途(剧情回顾)/媒姬百科(铁路百科)
- **Tools**: 时钟/日历/天气/计算器/文件管理/地图
- **岁月** = 桌面AI投影（非App图标，是常驻浮窗）
- **文件夹"铁路"** = 收纳归途/媒姬百科/音乐
- **Dock**: RDA/归途/笔记/设置
- **Easter egg**: 设置→应用管理→4个被替换应用恢复

## Script Completion Status

| Script | Status |
|--------|--------|
| prologue_01~10 | ✅ All complete |
| post_001 | ✅ |
| post_002 | ✅ |
| post_003 | ✅ |
| post_004 | ✅ |
| post_005 | ✅ |
| post_006 | ✅ |
| post_007 | ✅ |
| post_008 | ✅ |
| post_009 | ✅ |
