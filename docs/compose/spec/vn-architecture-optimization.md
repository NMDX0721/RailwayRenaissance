---
feature: vn-architecture-optimization
status: proposed
updated: 2026-10-07
branch: main
---

# VN 视觉小说系统底层架构优化与技术债务备忘录

## 概述

本文档记录 2026-10-07 对 `Assets/Scripts/VN/` 模块底层架构的全面诊断结果，涵盖**数据一致性隐患**、**单体类职责过载**、**资源生命周期**及**UI样式解耦**四大方向，作为序章剧本重写全线收官后的系统级重构依据。

---

## [S1] 核心问题诊断 (Problem)

### 1. 🔴 存档系统隐性数据断层：`VNSaveData` 遗漏分支变量持久化
- **位置**：`Assets/Scripts/VN/VNSaveSystem.cs:5-13` 与 `Assets/Scripts/VN/VNManager.cs:1366-1401` (`LoadFromSave`)
- **现象**：
  - 剧本中的选择题分支通过 `setValue` 写入 `VNManager.variables` 字典（如 `arrival_feeling=excited`、`repair_choice=zhang`、`fund_choice=village`、`final_vow=determined`）。
  - 但 `VNSaveData` 仅序列化了 `(scriptName, sceneIndex, dialogueIndex, timestamp, bgName, bgmName)`。
- **危害**：
  - 玩家在中途手动存档、退出并读档后，`variables` 字典为空。
  - 后续基于 `condition`（如 `!fund_choice_city` 或 `arrival_feeling_excited`）判断的专属分支台词与经营注入奖励将全部失效或误入默认分支。

### 2. 🟡 God-Object 结构瓶颈：`VNManager.cs` 达 3,184 行
- **位置**：`Assets/Scripts/VN/VNManager.cs`
- **现象**：
  - 承担了过多正交领域的职责：剧本状态机与分支逻辑、UI Toolkit DOM 树与数千行样式手写、键盘/鼠标手势输入监听、音频事件调度、新闻/CG/书签/答题弹窗分发、以及平板桌面预加载。
- **危害**：
  - 任何局部的 UI 视觉微调或输入微调，都需触碰核心状态机，代码阅读和维护成本极高，容易引发回归缺陷。

### 3. 🟡 资源生命周期缺乏统一管理（内存与显存隐患）
- **位置**：`BackgroundManager.cs`、`CharacterSpriteManager.cs`、`VNAudioManager.cs`
- **现象**：
  - 背景、立绘、音频均散落直调 `Resources.Load<Texture2D>` / `Resources.Load<Sprite>`。
  - 虽然立绘和降采样背景写了局部 Dictionary 缓存，但**缺乏统一的卸载与淘汰机制**（章节与场景切换时无显式清理或引用计数释放）。
  - 全工程存在两套音频体系：`Assets/Scripts/AudioManager.cs` 与 `Assets/Scripts/VN/AudioManager.cs`，跨场景时缺少统一条音通道与生命周期管理。
- **危害**：
  - 在移动端（Android）或长篇剧情连播时，无用纹理常驻内存，容易造成 GC 压力与内存溢出。

### 4. 🟡 UI Toolkit 样式严重硬编码
- **位置**：`VNBacklog.cs` (1,734行)、`DialogueBox.cs` (765行)、`VNManager.cs`
- **现象**：
  - 包含大量在 C# 中逐行编写样式的代码（如 `style.top`、`style.backgroundColor`、`style.borderBottomWidth`），仅少数复用了 `UI/frosted`。
- **危害**：
  - 布局与逻辑强耦合，美术样式迭代必须重新编译 C# 代码，无法发挥 UI Toolkit（USS）样式表的热重载优势。

### 5. 🟢 条件判定与表达式扩展性不足
- **位置**：`VNManager.cs` 中的 `EvaluateCondition`
- **现象**：
  - 目前仅支持极简的字符串前缀匹配（如 `key` 或 `!key`）。
- **优化点**：
  - 后续经营期若出现跨系统剧情判定（如“声望 $\ge 60$ 且资金 $> 20000$”触发特定客商对话），现有简单判断无法扩展。

---

## [S2] 架构重构设计 (Design)

### 2.1 任务一（P0·急迫项）：`VNSaveData` 变量持久化
1. **数据模型扩展**：
   ```csharp
   [System.Serializable]
   public class VNSaveData
   {
       public string scriptName;
       public int sceneIndex;
       public int dialogueIndex;
       public string timestamp;
       public string bgName;
       public string bgmName;
       // 新增：持久化所有激活的分支变量
       public List<string> activeVariables;
   }
   ```
2. **存取联动**：
   - `SaveGame()`：将当前 `VNManager.variables` 中为 `true` 的键转存到 `activeVariables`。
   - `LoadFromSave()`：清空并重构 `variables` 字典，确保分支状态无缝还原。

### 2.2 任务二（P1·结构项）：`VNManager.cs` 职责拆分
将 3,184 行的单体类解耦为清晰的 MVC/分层架构：
1. **`VNDialogueController`**：
   - 专注负责剧本解析、当前场景/对话索引递增、选项分支分发、条件求值。
2. **`VNUIController`**：
   - 统一管理 UI Toolkit VisualElement 根节点、菜单栏、信息卡、弹窗、黑屏过渡。
3. **`VNInputHandler`**：
   - 统一拦截和处理键盘快捷键（Space长按、Shift快进、Enter推进、ESC呼出菜单），隔离输入差异。
4. **`VNVariableStore`**：
   - 统一管理剧情标记变量，并提供与经营侧（`GameData`）的双向绑定。

### 2.3 任务三（P1·性能项）：`VNAssetManager` 资源池与卸载策略
1. 提供统一的资源加载入口 `VNAssetManager`：
   - 场景预加载（Preload next scene's sprites/bg）。
   - 章节结束（`nextScript` 跳转）时显式触发无用纹理清理。
2. 统一全工程音量曲线与音频通道：
   - `VNAudioManager` 继承或直接复用主 `AudioManager`，确保音量设置一处修改全域生效。

### 2.4 任务四（P2·表现项）：USS 样式外壳抽离
1. 将 `VNBacklog`、`DialogueBox`、`LocationCard` 的视觉尺寸、颜色、边框统一抽离至 `Assets/Resources/UI/VNStyle.uss`。
2. C# 脚本仅保留核心逻辑与类名绑定（`AddToClassList`），大幅压缩 C# 行数（预计削减 1,000+ 行代码）。

---

## [S3] 实施计划与里程碑

```
[当前阶段] 序章剧本冲刺 (04/02/01)
     ↓
[阶段一] 存档分支变量修复 (P0 · 预计 0.5 天)
     ↓
[阶段二] VNManager 核心解耦 (P1 · 预计 1-2 天)
     ↓
[阶段三] 资源生命周期与 USS 样式表规范化 (P2 · 预计 1 天)
```
