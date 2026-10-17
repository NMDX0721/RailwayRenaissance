# 铁路复兴：沙能冲击 — 代码地图

> 每个脚本的职责、依赖关系、修改影响范围。供 AI 代理快速定位代码。

---

## 一、类依赖图

```
LoginManager ──────────→ TitleScreen ──────────→ NewGameSetupUI
  │                       │ 依赖:                   │ 依赖:
  │ 依赖:                  │  UIDocument            │  UIDocument
  │  Resources/*.png      │  VideoPlayer           │  GameConfig
  │  PlayerPrefs          │  Resources/UI/         │
  │  auth.json            │  LoginManager          │
  │                       │  NewGameSetupUI        │
  │                       │  TitleArchiveUI        │
  │                       ▼                        │
  │                  VN_Test (VNManager)            │
  │                    │ 依赖:                      │
  │                    │  JSONParser                │
  │                    │  DialogueBox               │
  │                    │  BackgroundManager         │
  │                    │  CharacterSpriteManager    │
  │                    │  VNBacklog                 │
  │                    │  VNSaveSystem              │
  │                    │  VNSaveLoadUI              │
  │                    │  FullScreenNews            │
  │                    │  VNAudioManager            │
  │                    │  GameConfig                │
  │                    │                            │
  │                    ▼                            │
  │              StationSlice_V1                     │
  │              [RuntimeInitializeOnLoadMethod]    │
  │              RailRevivalRuntimeBootstrap        │
  │                    │ 构建 UI 布局               │
  │                    ▼                            │
  │              UIManager ───── GameData ──── EventManager
  │                 │            │                  │ 依赖:
  │                 │            │ 依赖:             │  Resources/events.json
  │                 │            │  GameConfig      │
  │                 │            │  EventManager    │
  │                 │            │  SandRivalManager│
  │                 │            │  TutorialManager │
  │                 │            │                  │
  │                 ├── ButtonController           │
  │                 ├── VisualAssetBinder          │
  │                 ├── AudioManager               │
  │                 │                              │
  │                 ▼                              ▼
  │          OrderManager ──── CrewManager ──── SandRivalManager
  │             │                 │                  │
  │             │ 依赖:           │ 依赖:             │ 依赖: 无
  │             │  Resources/    │  Resources/       │ (纯数据)
  │             │  orders.json   │  (无外部资源)      │
  │             │  GameData      │                   │
  │             │                 │                   │
  │             ▼                 ▼                   ▼
  │          TutorialManager   AchievementManager
  │             │                 │
  │             │ 依赖:           │ 依赖:
  │             │  GameData       │  GameData
  │             │  AudioManager   │
  └─────────────┴─────────────────┘
```

---

## 二、静态类 vs MonoBehaviours

### 静态类（无 GameObject，全局访问）
| 类 | 访问方式 | 初始化时机 |
|----|----------|-----------|
| `GameData` | `GameData.XXX` | `InitializeIfNeeded()` 在第一次 `AdvanceDay()` 时调用 |
| `CrewManager` | `CrewManager.XXX` | 手动调用 `Initialize()` |
| `EventManager` | `EventManager.XXX` | `GameData.InitializeIfNeeded()` 中调用 |
| `SandRivalManager` | `SandRivalManager.XXX` | 同上 |
| `OrderManager` | `OrderManager.XXX` | 手动调用 `Initialize()` |

### 单例
| 类 | Instance | 创建时机 |
|----|----------|----------|
| `VNManager` | `VNManager.Instance` | VN_Test 场景中 Awake() |
| `VNAudioManager` | `VNAudioManager.Instance` | VNManager.Awake() 创建 |
| `AudioManager` | `AudioManager.Instance` | Bootstrap 创建 |
| `TutorialManager` | `TutorialManager.Instance` | 场景中挂载 |

---

## 三、关键修改路径

### 3.1 要改经济系统参数
```
GameData.cs (常量)           ← 直接改
  ↓
GameConfig.cs (PlayerPrefs)  ← 运行时改
```

### 3.2 要加新的 VN 剧本
```
1. 创建 Resources/Scripts/prologue_XX_xxx.json
2. 在上一本最后一句改 nextScript 指向新剧本
3. 或修改 VNManager.Start() 中的默认启动剧本
```

### 3.3 要加新的随机事件
```
1. 编辑 Resources/events.json
2. 按格式添加事件模板
3. EventManager 自动加载
```

### 3.4 要加新的订单类型
```
1. 编辑 Resources/orders.json
2. 按格式添加订单模板
3. OrderManager 自动加载
```

### 3.5 要改 UI 布局
```
StationSlice_V1 → RailRevivalRuntimeBootstrap.cs (560行)
  → LayoutRootPanels() 控制面板位置
  → EnsureUi() 控制所有 UI 元素创建
  → 所有 UI 用代码创建（没有预制体）
```

### 3.6 要改 VN 对话框与术语系统 (DialogueBox.cs & Terms.json)
```
VN/DialogueBox.cs (1,061行) — BuildUI()、ShowDialogue()、TermClickSystem
  → 术语悬浮交互：默认不交互时不显示下划线，鼠标悬停时基线动态高亮下划线，移出消失
  → 术语弹出卡片（Bottom Dock）：正下方展开、单人词典释义+角色头像框展示，弹性盒排版防御（防图鉴按钮被挤飞）
  → 术语词库：Resources/Terms.json（全篇 65 条核心工业术语，覆盖动力总成、机车空气制动、工务保线等，346处高亮）
  → 样式：Resources/UI/VN/DialogueBox.uss
```

### 3.7 要改 VN 对话回顾与铁道滚动条 (VNBacklog.cs)
```
VN/VNBacklog.cs (1,959行)
  → 有色半透明背景：MakeBacklogWashiTex() 128×128 双线性平铺，包含造纸横向经纬丝、32px菱形方格暗纹、悬浮金箔/沙能微斑；透光度 65%，底层场景/立绘清晰可见
  → 顶部渐隐与磨砂：MakeBacklogFadeMaskTex() 与 Resources/UI/frosted.uss，暖沙褐过渡
  → 第一阶段（驻留景）：道砟平齐轨顶(top=0)防遮挡、列车加长至104px、完备段(200px)内自由拖拽并就地停驻（Park）、无反弹拖影
  → 第二阶段（正常滚动）：SbThumbHeight() 上下文自适应弹性缩放（最高达轨长70%）、拦截原生大刻度步进、16ms动量指数阻尼平滑滑行(* 0.88f)
  → 条目排版：暖木纹姓名横向缎带、高反差象牙白台词、柔和缩进旁白、纤细暖金分割线、黑底金边圆角「跳转」药丸按钮
```

### 3.8 要改标题界面
```
TitleScreen.cs — 使用 UI Toolkit
  → 布局在 Resources/UI/TitleScreen.uxml
  → 样式在 Resources/UI/TitleScreenStyles.uss
  → PanelSettings 在 Resources/UI/TitleScreenPanelSettings.asset
```

### 3.9 平板系统架构 (Tablet UI & Apps)
```
GameMainUI.cs (Tablet OS Shell, ~2,000行)
  ├── TabletUIHelper.cs (色系定义/字体定义/统一样式卡片骨架)
  └── Assets/Scripts/UI/Tablet/Apps/
        ├── TabletReturnJourneyUI.cs   (归途：主线剧情时间轴/全幕回溯)
        ├── TabletMusicAppUI.cs        (音乐：13首原声/后台播放器/悬浮进度)
        ├── TabletGalleryAppUI.cs      (相册：回忆画廊/全屏大图灯箱)
        ├── TabletSettingsAppUI.cs     (设置：24截图分类/TIPS图鉴/按键映射/开发者)
        ├── TabletRDAAppUI.cs          (RDA：调度助手/线路车况/T304五线态势联动)
        ├── TabletNotesAppUI.cs        (百科与便签：媒姬百科/三栏自动化备忘)
        └── TabletSystemToolsAppUI.cs  (系统工具集：时钟/日历/天气/计算器/米家/安全等)
```

---

---

## 四、资源引用关系

### 4.1 Resources（运行时加载）
```
路径                              被谁加载
Resources/Scripts/*.json          JSONParser → VNManager
Resources/events.json             EventManager
Resources/orders.json             OrderManager
Resources/bg/*.png                BackgroundManager
Resources/characters/*.png        CharacterSpriteManager
Resources/bgm/*.ogg               VNAudioManager
Resources/sfx/*.ogg               VNAudioManager
Resources/Fonts/zpix              LoginManager, VNManager, DialogueBox 等
Resources/UI/Login/*.png          LoginManager
Resources/UI/TitleScreen*         TitleScreen
Resources/UI/VN/DialogueBox.uss   VNManager
Resources/Cursors/*.png           LoginManager, VNManager
```

### 4.2 PlayerPrefs 键值
```
Key                             类型          用途
VN_Save_{0-2}                   JSON string   VN 存档
SaveSlot_{0-2}                  JSON string   经营存档
RailGameConfig                  JSON string   游戏配置
VNExitData                      JSON string   VN→经营过渡
VN_AutoLoad                     int           自动加载标记
Username                        string        用户名
```

---

## 五、各场景必须的组件

### Login.unity
- `LoginManager` (脚本)
- 自动创建：Canvas, EventSystem, Camera, AudioSource

### TitleScreen.unity
- `UIDocument` (组件)
- `TitleScreen` (脚本)
- `VideoPlayer` (可选，视频背景)
- 自动创建：Camera, EventSystem, BGM AudioSource

### VN_Test.unity
- `VNManager` (脚本)
- `TutorialManager` (脚本，可选)
- 自动创建：Canvas (UI Toolkit), EventSystem, VNAudioManager

### StationSlice_V1.unity
- 无必需组件（Bootstrap 自动创建一切）
- 可选：`TrainPlaceholder_Legacy` / `TrainCandidate_01` / `Train`, `Ground` (Tilemap)

---

## 六、存档/读档路径

### VN 存档
```
VNManager 菜单 → 存档 → VNSaveLoadUI.OpenSavePanel()
  → VNSaveSystem.SaveGame(slot, scriptName, sceneIndex, dialogueIndex, bgName, bgmName)
  → PlayerPrefs.SetString("VN_Save_" + slot, JsonUtility.ToJson(data))
  → PlayerPrefs.Save()
```

### VN 读档
```
VNManager 菜单 → 读档 → VNSaveLoadUI.OpenLoadPanel()
  → VNSaveSystem.LoadGame(slot)
  → VNManager.LoadFromSave(data)
  → 恢复场景/背景/BGM/对话位置
```

### 自动加载（"继续运营"）
```
TitleScreen.OnContinue()
  → PlayerPrefs.SetInt("VN_AutoLoad", 1)
  → SceneManager.LoadScene("VN_Test")
  → VNManager.Start() 检测 VN_AutoLoad=1
  → LoadLatestSave() → 加载最近存档
```

### 经营存档
```
VNSaveSystem.SaveGameplayData(slotIndex) — 保存 Money/Trust/TrainCondition/Passengers/Day
VNSaveSystem.LoadGameplayData(slotIndex) — 恢复上述数据
```

---

## 七、编辑器脚本

### 编辑器脚本（Assets/Editor/）
| 脚本 | 用途 |
|------|------|
| `FixSpriteImports.cs` | 修复精灵导入设置 |
| `SpriteImportFixer.cs` | 精灵导入修复 |
| `VNFinalSetup.cs` | VN 场景最终设置 |
| `TitleScreenSetup.cs` | 标题界面场景设置 |
| `CloudSeaSetup.cs` | 云海场景设置 |
| `RailRevivalMvpSetup.cs` | MVP 场景设置 |

---

## 八、常见问题排查

### 8.1 编译错误
```
Unity Editor 日志: C:\Users\Oe_Lee\AppData\Local\Unity\Editor\Editor.log
命令行验证: Unity.exe -quit -batchmode -projectPath "D:\Unity Project\RailwayRenaissance"
```

### 8.2 VN 剧本不显示
- 检查 `Resources/Scripts/{name}.json` 是否存在
- 检查 JSON 格式是否匹配 `ScriptData` 结构
- 检查 `VNManager.Start()` 中启动的剧本名

### 8.3 经营场景不显示 UI
- `RailRevivalRuntimeBootstrap` 需 `[RuntimeInitializeOnLoadMethod]`
- 检查 `StationSlice_V1` 是否在 Build Settings 中
- 检查 `Resources/UI/TitleScreenPanelSettings.asset` 是否存在

### 8.4 资源加载失败
- 所有资源必须放在 `Resources/` 目录下
- 路径名需匹配（`Resources/bg/lab` → `bg/lab` 无扩展名）
- 资源类型需匹配（`Resources.Load<Texture2D>` 读图片，`Resources.Load<AudioClip>` 读音频）

---

*本文档由 AI 代理于 2026-08-14 自动生成，修改代码后请同步更新。*