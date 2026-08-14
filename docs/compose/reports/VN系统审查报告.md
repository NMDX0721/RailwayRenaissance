# VN 系统 + 全局操作 + 快捷键审查报告

> 版本：v1.0  
> 审查范围：VNManager.cs、DialogueBox.cs、VNBacklog.cs、VNSaveLoadUI.cs、VNSaveSystem.cs、CharacterSpriteManager.cs、FullScreenNews.cs、TitleScreen.cs  
> 日期：2026-08-14

---

## 一、已确认的漏洞（需修复）

### 🔴 P0-1: "继续运营"永远从头播放序章
- **位置**：`TitleScreen.cs:265` 设置 `VN_AutoLoad=1`，但 `VNManager.cs` 从未读取该标志
- **影响**：标题界面点击"继续运营"→ 进入 VN 场景 → `StartScript("prologue_01_news")` 无条件从头播放，存档完全无效
- **修复**：VNManager.Start() 读取 `VN_AutoLoad`，为 1 时加载最近存档

### 🔴 P0-2: ESC 键在全屏新闻时完全失效
- **位置**：`VNManager.cs:456` `if (fullScreenNews.IsActive) return;` 拦截了所有输入
- **影响**：新闻全屏播放时，按 ESC 无任何反应（不能跳过、不能返回）
- **修复**：全屏新闻时 ESC 应关闭新闻（相当于点击）

### 🟡 P1-3: 菜单栏缺少"返回标题"按钮
- **位置**：`VNManager.cs:201` 菜单只有 回顾/存档/读档/自动
- **影响**：玩家只能靠 ESC 返回标题，界面没有显式入口
- **修复**：菜单栏增加"返回"按钮，触发二次确认弹窗

### 🟡 P1-4: VNBacklog 无上限
- **位置**：`VNBacklog.cs:16` `entries` 无限累加
- **影响**：超长剧本 + 多次跳转后，backlog 条目堆积，内存与渲染增长
- **修复**：限制 max 500 条，超出移除最旧

### 🟡 P1-5: 存档时无反馈提示
- **位置**：`VNSaveLoadUI.cs:325` 保存后直接 RefreshSlots
- **影响**：玩家不知道保存是否成功
- **修复**：保存后显示"已保存"短暂提示

### 🟡 P1-6: ReturnToTitle 未清除 VN_AutoLoad
- **位置**：`VNManager.cs:340`
- **影响**：从 VN 返回标题后标志残留，再次进 VN 会错误续玩
- **修复**：ReturnToTitle 中 `PlayerPrefs.SetInt("VN_AutoLoad", 0)`

---

## 二、设计核对（确认无问题）

| 项目 | 结论 |
|------|------|
| ESC 确认弹窗 | ✅ 已存在（confirmDialog + 确认/取消） |
| 选项分支暂停 | ✅ optionsContainer 显示时拦截点击 |
| 自带工具条光标 | ✅ AddCursorHover 已挂 |
| 存档槽位删除 | ✅ DeleteSave 已实现 |

---

## 三、审查通过项（无异常）

- DialogueBox 打字机 + 跳过：✅
- CharacterSpriteManager 缓存 + 三槽位：✅
- FullScreenNews 滚动 + Shift 加速：✅
- VNSaveSystem 槽位校验：✅

---

*修复优先级：P0 阻断（续玩/ESC），P1 体验（菜单/反馈/内存）。*