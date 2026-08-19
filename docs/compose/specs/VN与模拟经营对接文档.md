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
  │     └── Menu → 存档/取档/回顾/跳转/返回
  └── 书签管理（从 Menu 或 站长日志 进入）
```## 一、系统架构

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