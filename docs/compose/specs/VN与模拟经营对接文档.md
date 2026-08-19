# VN与模拟经营对接文档

> 版本：v2.0  
> 说明：本文档描述视觉小说系统（VN）与模拟经营系统之间的数据流、场景切换和交互机制  
> 更新：2026-08-19 — 新增书签系统、经营记录系统、GameMainUI 架构

---

## 一、系统架构

```
┌──────────────────────────────────────────────────────────────────┐
│                        游戏全局管理                                │
│                   GameManager (Scene管理)                          │
└──────────────────────────────────────────────────────────────────┘
         │                       │                        │
         ▼                       ▼                        ▼
┌─────────────────┐   ┌──────────────────┐   ┌────────────────────┐
│   VN系统         │   │  经营系统         │   │  共用子系统         │
│  (VN场景)        │   │  (GameMainUI)     │   │                    │
│                  │   │                  │   │  - AudioManager    │
│  VNManager.cs    │   │  GameData.cs     │   │  - BookmarkSystem  │
│  DialogueBox.cs  │   │  GameMainUI.cs   │   │  - SaveSystem      │
│  JSONParser.cs   │   │  MainStoryUI.cs  │   │  - TitleArchiveUI  │
│  VNSaveSystem    │   │                  │   │                    │
└────────┬─────────┘   └────────┬─────────┘   └────────────────────┘
         │                       │
         │     数据传递通道       │
         └───────────────────────┘
               VNExitData → 书签更新
```

## 一.a 场景流转

```
标题画面
  ├── 发车 → 序章（VN 纯剧情）
  │             └── 序章结束 → GameMainUI（经营界面）
  ├── 继续运营 → 经营记录列表 → 加载存档 → GameMainUI
  ├── 站长日志 → TitleArchiveUI（独立面板）
  └── 站务公告 → StationBulletinUI（设置面板）

GameMainUI（经营主界面）
  ├── 经营玩法（核心循环）
  ├── 事务 → 剧情 → MainStoryUI
  │                 └── 选择一话 → VN（只读回看，不写经营记录）
  └── 经营记录 → 保存/加载游戏状态

VN 内
  ├── Auto / Menu（右上角）
  │     └── Menu → 存档/取档/回顾/跳转/书签/返回
  └── 书签管理（从 Menu 或 站长日志 进入）
```

## 一.b 每话结束过渡（Episode Clear）

当序章的一话（Episode）通过 `nextScript` 链式播放完成时，自动显示过渡画面：

```
┌─────────────────────────────────────────────┐
│  (半透明黑色遮罩 60%，背景场景隐约可见)       │
│                                             │
│                  ┌──────────────────┐        │
│                  │   下一话          │        │
│                  │   第3话           │        │
│                  │   边境危机        │        │
│                  │                   │        │
│                  │  Touch to continue│        │
│                  └──────────────────┘        │
│                                             │
│                         To be continued...   │
└─────────────────────────────────────────────┘
```

**实现：** `VNManager.ShowEpisodeClear()`
- 遮罩点击 → 隐藏过渡 → 调用 `StartScript(nextScript)` 加载下一话
- 话编号和标题通过 `GetEpisodeNumber()` / `GetEpisodeTitle()` 自动获取## 一、系统架构

```
┌──────────────────────────────────────────────────────────────────┐
│                        游戏全局管理                                │
│                   GameManager (Scene管理)                          │
└──────────────────────────────────────────────────────────────────┘
         │                       │                        │
         ▼                       ▼                        ▼
┌─────────────────┐   ┌──────────────────┐   ┌────────────────────┐
│   VN系统         │   │  经营系统         │   │  共用子系统         │
│  (VN场景)        │   │  (Station场景)     │   │                    │
│                  │   │                  │   │  - AudioManager    │
│  VNManager.cs    │   │  GameData.cs     │   │  - UIManager       │
│  DialogueBox.cs  │   │  UIManager.cs    │   │  - SaveSystem      │
│  JSONParser.cs   │   │  TutorialManager │   │                    │
│  VNSaveSystem    │   │                  │   │                    │
└────────┬─────────┘   └────────┬─────────┘   └────────────────────┘
         │                       │
         │     数据传递通道       │
         └───────────────────────┘
             VNExitData (结构体)
```

---

## 二、数据传递：VNExitData

### 2.1 数据结构

```csharp
[System.Serializable]
public struct VNExitData
{
    // 基础资金
    public float startMoney;
    
    // 初始信任度 (0-100)
    public int startTrust;
    
    // 初始机车状态 (0-100)
    public int startTrainCondition;
    
    // 初始员工配置
    public CrewData[] crew;
    
    // 已完成剧情标记
    public string[] completedFlags;
    
    // 已解锁区域
    public string[] unlockedRegions;
    
    // 难度设置
    public string difficulty;
    
    // 玩家别名
    public string playerAlias;
}

[System.Serializable]
public struct CrewData
{
    public string id;
    public string name;
    public string role;       // driver/mechanic/conductor/dispatcher/attendant
    public int skillLevel;    // 1-5
    public float fatigue;     // 0-1
    public string specialty;  // 专长
}
```

### 2.2 数据传递流程

```
VNManager (VN场景)
    │
    │  VN剧本结尾处 t:"special" → "TRANSITION_TO_GAMEPLAY"
    │
    ▼
构建 VNExitData
    │
    │  money = 基础值 + 序章剧情补贴累计
    │  trust = 60 (基础值) + 序章选择加成
    │  trainCondition = 70 (基础值) + 维修选择加成
    │  crew = 5名员工初始数据
    │  completedFlags = 已完成的序章事件
    │
    ▼
PlayerPrefs 或 静态变量 暂存
    │
    ▼
SceneManager.LoadScene("StationSlice_V1")
    │
    ▼
GameData.Initialize(VNExitData)
    │
    ▼
经营模式开始
```

### 2.3 VNManager中的实现

```csharp
// 在 VNManager.cs 中新增
public void TransitionToGameplay()
{
    VNExitData exitData = new VNExitData();
    
    // 基础数据
    exitData.startMoney = CalculateTotalMoney();
    exitData.startTrust = CalculateTrust();
    exitData.startTrainCondition = CalculateTrainCondition();
    exitData.playerAlias = GameConfig.Load().PlayerDisplayName;
    exitData.difficulty = GameConfig.Load().difficulty;
    
    // 员工数据
    exitData.crew = new CrewData[]
    {
        new CrewData { id = "laochen", name = "老陈", role = "driver", 
                       skillLevel = 5, fatigue = 0, specialty = "safety" },
        new CrewData { id = "zhanggong", name = "张工", role = "mechanic", 
                       skillLevel = 5, fatigue = 0.2f, specialty = "repair" },
        new CrewData { id = "liayi", name = "李阿姨", role = "conductor", 
                       skillLevel = 2, fatigue = 0, specialty = "service" },
        new CrewData { id = "zhaoshifu", name = "赵师傅", role = "dispatcher", 
                       skillLevel = 4, fatigue = 0.1f, specialty = "management" },
        new CrewData { id = "xiaofang", name = "小芳", role = "attendant", 
                       skillLevel = 1, fatigue = 0, specialty = "learning" }
    };
    
    // 完成标记
    completedFlags = GetCompletedFlags();
    
    // 保存过渡数据
    SaveTransitionData(exitData);
    
    // 加载经营场景
    StartCoroutine(LoadGameplayScene());
}

private IEnumerator LoadGameplayScene()
{
    // 显示过渡UI
    ShowTransitionScreen("第一章·启程");
    
    yield return new WaitForSeconds(2.0f);
    
    // 异步加载场景
    AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("StationSlice_V1");
    while (!asyncLoad.isDone)
    {
        yield return null;
    }
}

private void SaveTransitionData(VNExitData data)
{
    string json = JsonUtility.ToJson(data);
    PlayerPrefs.SetString("VNExitData", json);
    PlayerPrefs.Save();
}
```

---

## 三、经营模式中的VN残留

### 3.1 经营中触发VN对话

经营模式中保留VN对话能力，用于触发剧情事件。通过以下机制：

```csharp
// 在经营场景中调用 VN 对话
public class GameplayVNTrigger : MonoBehaviour
{
    public void TriggerVNScene(string scriptName)
    {
        // 暂停经营模式
        GameData.Instance.PauseGame();
        
        // 加载 VN 场景（叠加模式，不卸载经营场景）
        SceneManager.LoadScene("VN_Scene", LoadSceneMode.Additive);
        
        // 启动指定剧本
        VNManager.Instance.StartScript(scriptName);
    }
    
    public void OnVNComplete()
    {
        // VN 结束回调
        GameData.Instance.ResumeGame();
        
        // 应用 VN 结果（资金变化、信任变化等）
        ApplyVNResults();
    }
}
```

### 3.2 经营中的VN事件触发条件

| 触发条件 | 剧本 | 说明 |
|---------|------|------|
| 运营第5天 | dialogue_daily_01 | 老陈关心运营情况 |
| 信任<40 | dialogue_trust_low | 村民抱怨，紧迫感 |
| 信任>80 | dialogue_trust_high | 村民庆祝，正面反馈 |
| 首次盈利 | dialogue_first_profit | 里程碑庆祝 |
| 沙能首次事件 | dialogue_sand_first | 沙能公司首次出现 |
| 首次事故 | dialogue_first_accident | 处理事故后果 |
| 机车状态<30 | dialogue_train_critical | 机车濒临报废 |
| 完成目标 | dialogue_goal_complete | 阶段性目标达成 |

### 3.3 VN事件对经营的影响

```csharp
[System.Serializable]
public class VNEventResult
{
    public int moneyDelta;           // 资金变化
    public int trustDelta;           // 信任变化
    public int trainConditionDelta;  // 车况变化
    public int passengerDelta;       // 客流变化
    public string[] flagsToSet;      // 剧情标记
    public string[] flagsToUnlock;   // 解锁内容
}
```

---

## 四、VN系统扩展：t:"special" 类型

### 4.1 新增指令类型

当前VN系统已有 `t: "n"`, `t: "d"`, `t: "c"`, `t: "scroll"`, `t: "ai"`。

新增 `t: "special"` 用于执行特殊游戏操作：

```json
{"t": "special", "text": "TRANSITION_TO_GAMEPLAY"}
```

### 4.2 特殊指令表

| 指令 | 功能 | 参数 |
|------|------|------|
| TRANSITION_TO_GAMEPLAY | 切换到经营场景 | 无 |
| ADD_MONEY | 增加资金 | text: 金额数值 |
| SET_FLAG | 设置剧情标记 | text: 标记名 |
| MODIFY_TRUST | 修改信任值 | text: 变化量 |
| MODIFY_CONDITION | 修改车况 | text: 变化量 |
| PLAY_BGM | 播放BGM | text: BGM ID |
| STOP_BGM | 停止BGM | 无 |
| SHAKE_SCREEN | 屏幕震动 | text: 强度 |

### 4.3 VNManager中的实现

```csharp
private void HandleSpecialCommand(string command)
{
    switch (command)
    {
        case "TRANSITION_TO_GAMEPLAY":
            TransitionToGameplay();
            break;
        default:
            if (command.StartsWith("ADD_MONEY|"))
            {
                int amount = int.Parse(command.Split('|')[1]);
                // 在 VNExitData 中累计
                pendingMoneyDelta += amount;
            }
            break;
    }
}
```

---

## 五、经营模式中的VN按钮

### 5.1 界面布局

```
┌─────────────────────────────────────────────────────┐
│              经营主界面                                │
│                                                      │
│  [线路图]  [人员]  [财务]  [机库]  [新闻]  [剧情]     │
│                                                      │
│  ┌─────────────────────────────────────────────────┐ │
│  │             经营核心区域                          │ │
│  │  （调度、策略、报表）                             │ │
│  │                                                 │ │
│  └─────────────────────────────────────────────────┘ │
│                                                      │
│  [推进日程]  [剧情/VN按钮]  [设置]                    │
└─────────────────────────────────────────────────────┘
```

### 5.2 "剧情"按钮行为

- 可用时：高亮，有剧情待推进
- 不可用时：灰色，显示"暂无新剧情"
- 点击后：启动VN对话（叠加场景）
- 完成后：返回经营界面，应用剧情结果

### 5.3 剧情标记系统

```csharp
public class StoryFlagManager
{
    private HashSet<string> completedFlags = new HashSet<string>();
    private Queue<string> pendingEvents = new Queue<string>();
    
    // 检查剧情是否可触发
    public bool CanTrigger(string eventId)
    {
        if (completedFlags.Contains(eventId)) return false;
        // 检查前置条件
        return CheckPrerequisites(eventId);
    }
    
    // 标记剧情完成
    public void MarkCompleted(string eventId)
    {
        completedFlags.Add(eventId);
        // 检查是否有后续事件
        CheckPendingEvents(eventId);
    }
}
```

---

## 六、场景切换流程

### 6.1 完整游戏流程

```
标题画面 (TitleScreen.unity)
    │
    ▼ 点击"新游戏"
新游戏设置 (NewGameSetupUI)
    │
    ▼ 确认开始
序章 VN 序列 (prologue_01 → prologue_02 → ... → prologue_10)
    │
    ▼ 序章结束
过渡界面 ("第一章·启程")
    │
    ▼ 点击任意位置
经营模式 (StationSlice_V1.unity)
    │
    ├── 日常经营循环
    ├── 触发 VN 事件 (叠加场景)
    └── 推进章节
    │
    ▼ 完成章节目标
下一章 VN 序列
    │
    ▼ 循环...
```

### 6.2 场景管理

```csharp
public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Title,
        NewGameSetup,
        VNSequence,
        Gameplay,
        VNEvent
    }
    
    private GameState currentState;
    
    public void StartNewGame()
    {
        currentState = GameState.VNSequence;
        SceneManager.LoadScene("VN_Scene");
    }
    
    public void TransitionToGameplay()
    {
        currentState = GameState.Gameplay;
        SceneManager.LoadScene("StationSlice_V1");
    }
    
    public void TriggerVNEvent(string scriptName)
    {
        currentState = GameState.VNEvent;
        SceneManager.LoadScene("VN_Scene", LoadSceneMode.Additive);
        VNManager.Instance.StartScript(scriptName);
    }
}
```

---

## 七、数据持久化

### 7.1 存档结构

```
Save Slot
├── VNProgress
│   ├── currentScript
│   ├── currentScene
│   ├── completedFlags[]
│   └── variables{}
├── GameplayData
│   ├── money
│   ├── trust
│   ├── trainCondition
│   ├── crew[]
│   ├── infrastructure[]
│   └── day
└── Metadata
    ├── saveTime
    ├── playerAlias
    ├── difficulty
    └── chapter
```

### 7.2 存档兼容性

VN存档和经营存档共享同一存档系统：

```csharp
public class UnifiedSaveSystem
{
    public void SaveGame(int slotIndex)
    {
        SaveData data = new SaveData();
        
        // 保存 VN 进度
        if (VNManager.Instance != null && VNManager.Instance.IsRunning())
        {
            data.vnProgress = VNManager.Instance.GetSaveData();
        }
        
        // 保存经营数据
        if (GameData.Instance != null)
        {
            data.gameplayData = GameData.Instance.GetSaveData();
        }
        
        // 保存到文件
        SaveToFile(slotIndex, data);
    }
    
    public void LoadGame(int slotIndex)
    {
        SaveData data = LoadFromFile(slotIndex);
        
        if (data.vnProgress != null)
        {
            // 如果在 VN 中，恢复 VN
            SceneManager.LoadScene("VN_Scene");
            VNManager.Instance.RestoreFromSave(data.vnProgress);
        }
        else if (data.gameplayData != null)
        {
            // 如果在经营中，恢复经营
            SceneManager.LoadScene("StationSlice_V1");
            GameData.Instance.RestoreFromSave(data.gameplayData);
        }
    }
}
```

---

## 八、序章完成后经营模式初始状态

### 8.1 初始数值表

| 参数 | 基础值 | 序章加成 | 最终值 | 说明 |
|------|-------|---------|-------|------|
| 资金 | 40,000 | +30,000(序章补贴) | 70,000 | 序章补贴已累积 |
| 信任 | 60 | +2(首班车) | 62 | 初始可接受 |
| 车况 | 70 | +2~5(维修) | 72~75 | 取决于维修选择 |
| 客流 | 22人/天 | 0 | 22人/天 | 约20%上座率 |
| 员工 | 5人 | 0 | 5人 | 初始团队 |
| 沙能渗透 | 0.15 | 0 | 0.15 | 初始值 |
| 政治压力 | 0.20 | 0 | 0.20 | 初始值 |

### 8.2 解锁状态

| 系统 | 状态 | 说明 |
|------|------|------|
| 雾峰村-矿区线 | ✅ 已解锁 | 初始线路 |
| 人员系统 | ✅ 已解锁 | 5人团队 |
| 维护系统 | ✅ 已解锁 | 基础维护 |
| 调度系统 | ✅ 已解锁 | 每日发车方案 |
| 科技树 | ❌ 未解锁 | 运营30天后解锁 |
| 沙能竞争 | ❌ 未解锁 | 运营30天后触发 |
| 新线路 | ❌ 未解锁 | 运营60天后解锁 |
| 政治系统 | ❌ 未解锁 | 运营120天后解锁 |

---

## 九、开发优先级

| 优先级 | 任务 | 依赖 |
|--------|------|------|
| P0 | 创建 prologue_04~08 JSON（核心序章剧情） | 无 |
| P0 | VNManager 新增 t:"special" 处理 | 视觉小说系统设计.md |
| P0 | VNExitData 结构体定义 | 无 |
| P0 | VN→经营场景切换实现 | VNManager.cs |
| P1 | 经营中触发VN事件（叠加场景） | 无 |
| P1 | 创建 prologue_09a~c JSON（剧情补贴） | 无 |
| P1 | 创建 prologue_10 JSON（过渡场景） | 无 |
| P2 | 统一存档系统 | VNSaveSystem.cs |
| P2 | 剧情标记系统 | 无 |
| P3 | 全流程调试 | 以上全部 |

---

*本文档与 视觉小说系统设计.md、核心玩法循环.md、序章后续剧情设计.md、GameData.cs、VNManager.cs 联动使用。*

---

## 附录：合并自 新游戏设置系统.md

# 新游戏设置系统

> 版本：v2.0（整合版）
> 日期：2026-07-18
> 说明：本文件已被主文档《游戏开发文档.md》整合，此版本为独立参考副本。

---

## 一、设置流程

| 步骤 | 内容 |
|------|------|
| 1 | 选择剧本（大废线末期/一切的伊始） |
| 2 | 输入主角"字"（表字） |
| 3 | 选择难度 |
| 4 | 自定义参数（指导司机难度） |
| 5 | 确认开始 |

---

## 二、主角名称

| 项目 | 设定 |
|------|------|
| 本名 | 林彪悍（固定） |
| "字" | 用户可自定义 |
| 日常对话 | 显示"字" |
| 关键剧情 | 显示本名 |

---

## 三、难度系统

### 司炉（简单）
| 参数 | 值 |
|------|-----|
| 初始资金 | 50,000沙 |
| 收入倍率 | ×1.3 |
| 成本倍率 | ×0.8 |
| 补贴倍率 | ×1.5 |
| 剧情补贴倍率 | ×1.5 |
| 盈亏平衡 | 3节车厢，60%上座率 |

### 副司机（普通）
| 参数 | 值 |
|------|-----|
| 初始资金 | 40,000沙 |
| 收入倍率 | ×1.0 |
| 成本倍率 | ×1.0 |
| 补贴倍率 | ×1.0 |
| 剧情补贴倍率 | ×1.0 |
| 盈亏平衡 | 4节车厢，70%上座率 |

### 司机（困难）
| 参数 | 值 |
|------|-----|
| 初始资金 | 30,000沙 |
| 收入倍率 | ×0.8 |
| 成本倍率 | ×1.2 |
| 补贴倍率 | ×0.7 |
| 剧情补贴倍率 | ×0.7 |
| 盈亏平衡 | 5节车厢，75%上座率 |

### 指导司机（自定义）
| 参数 | 范围 |
|------|------|
| 初始资金 | 10,000-50,000沙 |
| 收入倍率 | 0.5-2.0 |
| 成本倍率 | 0.5-2.0 |
| 补贴倍率 | 0.5-2.0 |
| 沙子价格倍率 | 0.5-2.0 |
| 客运量倍率 | 0.5-2.0 |
| 货运量倍率 | 0.5-2.0 |
| 事件频率 | 0.5-2.0 |

---

## 四、货币系统

| 货币 | 说明 |
|------|------|
| 沙币（沙） | 国内主要货币，与沙子价格正相关 |
| 大额沙币（万沙） | 1万沙 = 10,000沙，大额交易用 |

> 注：早期设定中曾有"朝币"作为国际结算货币（1朝币≈50沙），当前版本暂不启用。

---

*本文档的详细内容已整合至主文档《游戏开发文档.md》第六章。*


---

## 附录：视觉小说系统设计（合并自参考资料）

# 视觉小说系统设计

> 版本：v2.1（AI集成版，补充实际实现格式）

---

## 一、JSON剧本格式（v2.0扩展）

```json
{
  "id": "prologue_001",
  "ai_enabled": true,
  "ai_model": "mimo-v2.5",
  "scenes": [
    {
      "bg": "train_interior",
      "bgm": "train_ambient",
      "d": [
        {"t": "n", "text": "旁白文本", "dur": 2},
        {"t": "d", "s": "老陈", "text": "台词内容", "e": "smile"},
        {"t": "c", "text": "问题", "opts": [{"text": "选项", "next": 5}]},
        {"t": "ai", "s": "老陈", "prompt": "老陈看到主角回来，心情如何？", "context": "老陈是站长，等了4年"}
      ]
    }
  ]
}
```

### 字段说明（v2.0新增，v2.1补充）

| 字段 | 说明 |
|------|------|
| t: "n" | 旁白（无说话者） |
| t: "d" | 对话（有说话者） |
| t: "c" | 选择（分支选项） |
| t: "scroll" | 滚动长文本（如新闻全文，v2.1新增） |
| t: "ai" | AI生成对话 |
| s | 说话者名称 |
| text | 文本内容 |
| e | 表情（可选） |
| opts | 选项数组 |
| next | 跳转场景索引 |
| bg | 背景ID |
| bgm | BGM ID |
| transition | 场景过渡效果（fade/cut/ dissolve） |
| prompt | AI生成提示词 |
| context | AI上下文 |
| ai_enabled | 是否启用AI |
| ai_model | AI模型选择 |

**t: "scroll" 说明**：
- 用于显示大段可滚动文本（如新闻全文、历史文件）
- 玩家可手动滚动阅读，点击任意位置继续
- 已有实现：prologue_01_news.json 使用此格式

---

## 二、对话框设计（v2.0扩展）

### 布局（含AI输入框）

```
┌─────────────────────────────────────────────────────┐
│                    [背景画面]                         │
│                                                     │
│  ┌─────────────────────────────────────────────────┐│
│  │ 老陈                                             ││
│  │ ─────────────────────────────────────────────── ││
│  │ "孩子，这线路已经荒废四年了..."                   ││
│  │                                        ▼ 点击继续 ││
│  └─────────────────────────────────────────────────┘│
│                                                     │
│  ┌─────────────────────────────────────────────────┐│
│  │ [AI输入框] 输入你想说的话...          [发送] [选项] ││
│  └─────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────┘
```

### AI输入框参数

| 属性 | 值 |
|------|-----|
| 位置 | 对话框下方 |
| 高度 | 60px |
| 背景色 | rgba(20, 15, 10, 0.9) |
| 边框 | 1px solid rgba(150, 100, 50, 0.5) |
| 字号 | 18px |
| 占位符 | "输入你想说的话..." |

---

## 三、AI对话系统（v2.0新增）

### 3.1 AI接口设计

**接口类型**：自定义API + 本地模型

**支持的AI模型**：
| 模型 | 说明 | 优先级 |
|------|------|--------|
| MiMo 2.5 | 主力模型，中文优化 | 首选 |
| GPT-4 | 备用模型 | 备选 |
| 本地模型 | 离线可用 | 降级方案 |

**API配置**：
```json
{
  "ai_config": {
    "provider": "custom",
    "base_url": "https://api.mimo.ai/v1",
    "api_key": "user_api_key",
    "model": "mimo-v2.5",
    "max_tokens": 200,
    "temperature": 0.8,
    "timeout": 10
  }
}
```

### 3.2 AI对话流程

```
玩家输入 → 上下文组装 → AI生成 → 角色响应 → 好感度变化
     ↓           ↓           ↓           ↓           ↓
  文本框    场景+角色+历史   API调用    显示回复    数值更新
```

### 3.3 上下文系统

**上下文组成**：
| 类型 | 内容 | 权重 |
|------|------|------|
| 场景信息 | 当前地点、时间、事件 | 高 |
| 角色信息 | 角色身份、性格、关系 | 高 |
| 历史对话 | 最近5轮对话记录 | 中 |
| 好感度 | 当前好感度数值 | 中 |
| 玩家状态 | 资金、运营状况 | 低 |

**上下文模板**：
```
你是{角色名}，{性格描述}。
当前场景：{场景描述}
你和玩家的关系：{好感度等级}
最近对话：{历史对话}
玩家说：{玩家输入}
请以{角色名}的身份回复，保持角色一致性。
```

### 3.4 AI回复约束

**回复规则**：
1. **角色一致性**：回复必须符合角色性格
2. **长度限制**：50-100字（可配置）
3. **情感控制**：根据好感度调整语气
4. **安全过滤**：屏蔽敏感内容

**好感度影响**：
| 好感度 | 语气 | 回复倾向 |
|--------|------|----------|
| 90-100 | 热情友好 | 积极、帮助 |
| 70-89 | 正常 | 中立、合作 |
| 50-69 | 冷淡 | 保留、敷衍 |
| 30-49 | 不满 | 抱怨、拒绝 |
| 0-29 | 敌对 | 对抗、离开 |

### 3.5 API预留接口

**自定义API配置**：
```csharp
public class AIConfig
{
    public string Provider { get; set; } = "mimo";
    public string BaseUrl { get; set; } = "https://api.mimo.ai/v1";
    public string ApiKey { get; set; }
    public string Model { get; set; } = "mimo-v2.5";
    public int MaxTokens { get; set; } = 200;
    public float Temperature { get; set; } = 0.8f;
    public int Timeout { get; set; } = 10;
}

public class AIContext
{
    public string SceneId { get; set; }
    public string CharacterId { get; set; }
    public string CharacterName { get; set; }
    public string CharacterPersonality { get; set; }
    public int Favorability { get; set; }
    public List<string> History { get; set; }
    public string PlayerInput { get; set; }
}

public class AIResponse
{
    public string Text { get; set; }
    public string Emotion { get; set; }
    public int FavorabilityChange { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
}
```

---

## 四、AI对话场景类型

### 4.1 预设对话（传统选项）

```
老陈：孩子，这线路已经荒废四年了...
玩家：[选项1] 我会修复它的
      [选项2] 我需要考虑一下
      [选项3] 为什么找我？
```

### 4.2 AI自由对话（新增）

```
老陈：孩子，这线路已经荒废四年了...
玩家：[输入框] 老陈爷爷，您辛苦了，我这就回来
老陈：好孩子...你爷爷要是知道你回来了，一定很高兴...
好感度 +5
```

### 4.3 混合模式（推荐）

```
老陈：孩子，这线路已经荒废四年了...
玩家：[选项] 我会修复它的
      [AI输入] 自由输入你想说的话
老陈：（根据玩家选择或输入生成回复）
```

---

## 五、AI系统配置

### 5.1 模型选择

| 场景 | 推荐模型 | 原因 |
|------|----------|------|
| 主线剧情 | MiMo 2.5 | 中文优化，角色扮演强 |
| 支线对话 | MiMo 2.5 | 一致性好 |
| 随机NPC | 本地模型 | 成本低，速度快 |
| 复杂剧情 | GPT-4 | 创意强，但成本高 |

### 5.2 成本控制

| 策略 | 说明 |
|------|------|
| 缓存常见回复 | 相同上下文返回缓存 |
| 限制每日调用 | 免费版100次/天 |
| 本地模型降级 | API不可用时用本地 |
| 压缩上下文 | 只发送最近5轮对话 |

### 5.3 安全机制

| 机制 | 说明 |
|------|------|
| 内容过滤 | 屏蔽敏感词 |
| 回复审核 | AI回复前检查 |
| 人工干预 | 异常时切换预设 |
| 日志记录 | 记录所有AI对话 |

---

## 六、开发阶段（v2.0更新）

| 阶段 | 内容 | 状态 |
|------|------|------|
| 1 | JSON解析器+对话框+打字机 | 已完成 |
| 2 | 对话框UI优化 | 进行中 |
| 3 | 角色立绘系统+Auto/Skip/Log | 待开始 |
| 4 | AI对话系统接口预留 | v2.0新增 |
| 5 | 自定义API配置界面 | v2.0新增 |
| 6 | 术语高亮+存档系统 | 待开始 |

---

*本文档为视觉小说系统的完整设计（v2.0 AI集成版）*
*支持传统选项+AI自由对话+自定义API配置*
