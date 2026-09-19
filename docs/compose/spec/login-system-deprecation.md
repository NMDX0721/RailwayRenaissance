---
feature: login-system-deprecation
status: in-progress
updated: 2026-09-19
branch: main
commits: 
---

# 登录系统废弃 + 加载优化

## Report

**What was built** — 平板桌面重构的核心基础设施：登录系统（LoginManager/AutoLoginUI/TitleScreen）标记DEPRECATED，Unity启动场景改为VN_Test（新玩家直接进序章），用户名迁移到PlayerPrefs("XiaomiAccount")，Android返回手势禁用，GameMainUI添加ESC关闭窗口，序章01新闻开场序列（标题卡+配图+日期字卡）。

**Verification** — Unity batchmode编译：0 error CS。

## [S1] Problem

当前游戏启动流程：LoginManager(2344行) → AutoLoginUI(241行) → TitleScreen(540行) → VN_Test/StationSlice_V1。

问题：
- 登录/注册是纯本地auth.json明文存储，无实际网络验证
- AutoLoginUI是假加载动画（9阶段模拟进度）
- TitleScreen与平板桌面(GameMainUI)功能重复
- 用户名系统与平板"小米账号"体系冲突
- 主角开局没有平板——平板在序章Day0从0721储物格找到，不应提前显示平板桌面

## [S2] Design

### 2.1 新玩家流程（核心）

```
启动 → 直接进入VN序章（无平板界面）
    │
    序章Day0：获得0721 → 岁月启动 → 发现平板 → 激活平板
    → 注册小米账号（取名）→ 岁月问难度（融入剧情对话）
    │
    序章结束 → 平板桌面成为经营主界面
```

**关键原则**：主角开局没有平板，平板是剧情中发现的。游戏入口=剧情，不是平板桌面。

### 2.2 老玩家流程

```
启动 → 检测存档 → 平板桌面 → RDA App → 继续运营
```

### 2.3 废弃旧系统

| 组件 | 处理 |
|------|------|
| LoginManager.cs | 标记`[Obsolete]`，代码保留 |
| AutoLoginUI.cs | 标记`[Obsolete]` |
| TitleScreen.cs | 标记`[Obsolete]` |
| auth.json | 用户名迁移到PlayerPrefs("XiaomiAccount") |
| PlayerPrefs("Username") | 保留兼容读取 |

### 2.4 序章集成

**命名系统**：序章Day0发现平板时，平板提示"注册小米账号"→玩家输入用户名→保存PlayerPrefs("XiaomiAccount")。默认"旅人"。

**难度选择**：融入剧情，岁月问主角"你喜欢什么样的节奏"，选项明确标注影响游戏难度。

### 2.5 场景加载

| 场景 | 状态 |
|------|------|
| Login场景 | 废弃 |
| TitleScreen场景 | 废弃 |
| VN_Test场景 | **新玩家启动场景** |
| StationSlice_V1场景 | 经营场景（序章后进入） |

### 2.6 序章开场序列（新玩家入口）

**设计原则**：新玩家启动直接进序章，但不能突兀。开场序列解决"新玩家看到新闻会蒙"的问题。

```
黑屏
  → 游戏标题 + 创作者信息（淡入淡出，~3秒）
  → 新闻段落1：2050沙能诞生（配背景图，非纯黑屏）
  → 新闻段落2：2053飞猪号商用（配背景图）
  → 新闻段落3：2072铁路衰落（配背景图，BGM切入melancholy）
  → 切换到实验室背景
  → 【2076年5月 · 平壤 金日成综合大学】日期字卡（高级感，仅此一处）
  → 老陈来电，剧情正式开始
```

**关键决策**：
- 新闻段落**不加日期**——新闻本身只有几句话，加日期画蛇添足
- 日期只在**正式开始（实验室场景）**显示——一处日期，高级感
- 新闻段落**配背景图**——不再是纯黑屏文字
- 游戏标题+创作者信息在最前面——给玩家"故事开始了"的信号

**新闻段落配图需求**（待生成）：
| 段落 | 内容 | 建议配图 |
|------|------|---------|
| 新闻1 | 2050沙能诞生 | 实验室场景（已有lab.jpg） |
| 新闻2 | 2053飞猪号商用 | 0721相关（可复用已有素材） |
| 新闻3 | 2072铁路衰落 | 废弃铁路（railway_track待生成） |

**实现方式**：修改prologue_01_news.json的bg字段，将black替换为对应背景图。

### 2.7 平台适配

- Android：`Input.backButtonLeavesApp = false`
- 键盘映射：空格=确认，ESC=返回

## [S3] Out of Scope

- 序章跳过答题系统
- 序章Day0发现平板的JSON剧本修改
- App功能实现
- 老玩家存档迁移
- 新闻段落配图的AI生成（素材已在美术提词文档中）

## Tasks

- [x] T1: LoginManager标记Obsolete — acceptance: 文件头添加[DEPRECATED]注释 (covers: S2.3)
- [x] T2: AutoLoginUI标记Obsolete — acceptance: 文件头添加[DEPRECATED]注释 (covers: S2.3)
- [x] T3: TitleScreen标记Obsolete — acceptance: 文件头添加[DEPRECATED]注释 (covers: S2.3)
- [x] T4: 用户名迁移逻辑 — acceptance: GameMainUI启动时读取PlayerPrefs("Username")写入PlayerPrefs("XiaomiAccount") (covers: S2.3)
- [x] T5: VN_Test设为启动场景 — acceptance: EditorBuildSettings第一场景改为VN_Test.unity (covers: S2.5)
- [x] T6: 序章开场序列 — acceptance: 黑屏→标题+创作者信息→新闻配图3段→实验室+日期字卡→剧情开始 (covers: S2.6)
- [x] T7: Android返回手势适配 — acceptance: Input.backButtonLeavesApp=false (covers: S2.7)
- [x] T8: 键盘映射 — acceptance: 空格解锁+ESC关闭App窗口 (covers: S2.7)
- [x] T9: Unity编译验证 — acceptance: 编译0 error CS (covers: S2)
