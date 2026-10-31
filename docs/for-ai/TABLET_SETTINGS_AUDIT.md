# TABLET_SETTINGS_AUDIT.md — 全量设置审计

> Codex复核说明：这是MiMo基于投递时源码快照的完整交付，行号对应该快照。投递后个性化分类9、独立JSON、壁纸压暗/减少解锁动效/系统面板强调色与高对比度已接入并通过运行检查；危险的无确认DeleteAll入口已移除，锁屏空开关已清理。因此下文“尚未集成”是投递时状态，不覆盖[TABLET_SYSTEM_SHELL.md](TABLET_SYSTEM_SHELL.md)的最新验收。跨文件locator不是运行证据，其他未核验项保留待办，不直接将建议当实现事实。

**任务**：CODEX-MIMO-TABLET-FULL-AUDIT-20261023  
**范围**：`TabletSettingsAppUI.cs` 全部 24 分类 + 开发者项，消费链、存储边界、状态矩阵、验收、下一批任务卡  
**方法**：仅依据所附源码快照与消费者定位表（Cross-file locator 为线索、非运行时证明）。未附实现的消费端一律标「待核验」，不假称已读全库或已运行。  
**交付**：本文即 `docs/for-ai/TABLET_SETTINGS_AUDIT.md` 全文，由 Codex 落盘。

> 2026-10-24 实现更新：Q4 的按键重置问题已修复。`KeyBindings.ResetToDefaults()` 将全部映射写回默认键表并保存，分类15按钮调用该方法；原快照中的“仅自赋值”结论保留为审计历史，后续验收应验证四个默认键恢复且其他设置不变。本轮未运行 Unity。
> 
> 2026-10-24 实现更新（开发者重置二次确认，DESKTOP-AGY-TABLET-RESET-CONFIRMATIONS-20261024）：
> 为设置 App 开发者选项（分类 99）中的两项破坏性调试指令增加统一二次确认弹窗（`ShowResetConfirmDialog`）：
> 1. **交互行为**：点击「重置主线剧情进度」或「重置经济模拟数据」仅弹出覆盖遮罩与确认卡片，展示明确影响边界与「取消」及「确认重置」按钮；
> 2. **零副作用取消**：点击「取消」按钮、点击半透明暗色背景遮罩、或按下键盘 Escape 键，均仅关闭弹窗，不产生任何 PlayerPrefs 删除、不改动 GameData 经济状态、不弹出 Toast；
> 3. **确认执行边界**：
>    - 确认重置主线：仅执行既有键删除逻辑（`ArchiveStory_*`、`ArchiveScene_*`、`VN_Got0721`、`LastReadScript`、`DebugUnlockAllChapters`）并调用 `PlayerPrefs.Save()`，随后弹出成功 Toast；不触及经济、车况、信任，不触及音乐独立 JSON / 歌词文件，不影响系统设置；
>    - 确认重置经济：调用 `GameData.ResetState()` 重置当前运行中的经营状态；资金按当前难度配置恢复，有种子时重应用该种子的趋势初值，无种子时使用兼容默认值，并清除监管程序/重建趋势快照；不删除经营存档槽、主线通关与阅读记录、音乐库与系统设置；会清空当前运行时的剧情补贴防重复状态；
> 4. **严格不越界**：未引入任何「恢复出厂」或 `PlayerPrefs.DeleteAll()` 调用；一键解锁全部剧情、注资、退出开发者模式等其他回调保持原样；
> 5. **静态核验与运行待办**：源码级静态引用核对通过；初次任务回执未执行 Unity。Codex 后续复核发现原金额文案固定写 50,000，且 `ResetState()` 会重载旧监管状态、漏掉种子初值和趋势快照一致性，已修复这些差异。当前需以最新复核记录为准。

> **2026-10-24 实现与自动化验收完成（DESKTOP-AGY-TABLET-RESET-FINAL-20261024）**：
> 1. **文案修正**：`TabletSettingsAppUI.cs` 行描述已由原固定数值承诺改为：「重置当前运行中的经营状态：资金按难度恢复，初始趋势依已加载种子生成，回到第 1 天」，彻底移除 50,000 沙币、信任 62、车况 70 等固定值承诺，严格符合两铁律；
> 2. **自动化测试套件**：建立 `Assets/Editor/TabletResetConfirmationReview.cs`，提供 `TabletResetConfirmationReview.Batch` 入口，通过 `UIDocument` 原生 Panel 隔离环境验证：
>    - Test 1: 弹窗标题、影响边界说明、确认/取消按钮文案与层次结构；
>    - Test 2: 点击取消按钮、点击半透明暗色背景遮罩、按 Escape 键均零副作用关闭弹窗（确认回调次数恒为 0）；
>    - Test 3: 确认按钮仅触发回调一次并自动销毁遮罩，重复打开弹窗自动去重且无残留；
>    - Test 4: 生产行静态与安全挂载，断言无 50,000 / 信任 62 / 车况 70，点击主线与经济行正确弹出二次确认，安全点击取消验证零误删；
> 3. **运行验收**：Unity 6.5 batchmode 批处理执行完成，日志 `tools/compile/tablet_reset_confirmation_review_20261024.log` 输出 `TABLET_RESET_CONFIRMATION_REVIEW_OK`，4 项用例全部通过，退出码 0。
> 
> **2026-10-25 分类 11 接入（DESKTOP-AGY-TABLET-SETTINGS-NOTIFICATIONS-20261025，最终复验通过）**：
> 1. **分类 11 专属页接入**：设置页（`TabletSettingsAppUI`）增加独立分类 11「通知与状态栏」详情页，彻底替换原三行通用占位；
> 2. **真实系统面板入口**：提供「打开通知中心」（请求 `TabletSystemPanel.Notifications`）与「打开控制面板」（请求 `TabletSystemPanel.Controls`），通过 `GameMainUI.OpenSystemPanel` / `s_openSystemPanel` 真实连接系统面板；
> 3. **安全降级保证**：未注入回调或无宿主环境时展示降级提示卡（`settings-notifications-fallback-note`），点击提供准确的未就绪 Toast 提示，不抛出任何异常；
> 4. **事实与机制对齐**：清晰说明通知来自应用实际提示、最多收存 32 条；开启勿扰时保留通知收存、仅屏蔽悬浮横幅；勿扰当前仅在进程内有效；内网沙箱运行、状态栏不虚拟 Wi-Fi/蓝牙/电池；
> 5. **自动化验收状态**：修正后的设置 Review 验证前三项并明确记录 `PARTIAL`；旧壳的面板互斥、遮罩、Escape、勿扰与布局由独立 `TabletSystemShellReview.Batch` 实际运行并通过（`TABLET_SHELL_RUNTIME_OK`，Unity 退出码 0）。详情见 Q13。
> 
> **2026-10-25 车载连接设置去假化（DESKTOP-AGY-TABLET-CONNECTIVITY-SETTINGS-20261025，Codex 复核修正）**：
> 1. **分类 4–7 专属只读页**：设置页（`TabletSettingsAppUI`）为分类 4「WLAN」、5「蓝牙」、6「互联互通」、7「更多连接方式」分别实现专属详情页（`RenderWlanSettings`、`RenderBluetoothSettings`、`RenderInterconnectSettings`、`RenderMoreConnectionsSettings`），彻底移出通用占位渲染器；
> 2. **消除假入口与模板**：四类全部移除通用的三行「此功能尚未开放」「高级选项与网络策略」「权限与策略说明」模板，所有行统一使用 `hasArrow: false` 与 `onClick: null`（`pickingMode = PickingMode.Ignore`），杜绝误导性箭头和假 Toast 操作；
> 3. **事实与边界对齐**：页面只陈述此终端没有公共 Wi-Fi、蓝牙配对、个人账户同步、热点/投屏等设置入口；不展示实时网络、设备或接口状态。删除了没有项目设定依据的“射频硬静默”“铁道部标准总线”“地面应答器调度链路”“双绞屏蔽线”和“车底维护端子”等硬件断言；
> 4. **自动化验收范围**：分类 4–7 各自用例通过。分类 11 调用 `TabletSettingsNotificationReview.Batch`，该套件 1–3 项通过，但明确报告需要单独运行 `TabletSystemShellReview.Batch` 的第 4 项 `NOT RUN`；因此本任务运行不能称为“分类 11 完整回归”。单独补跑系统壳 Review 时 Unity 在初始化阶段退出，未取得新回归证据。

> **2026-10-24 Codex 验收修正**：`GameData.ResetState()` 现在先清除监管持久程序，再按当前 `CurrentSeed` 重应用趋势初值和评级、重建 Day 1 快照、写回全局监管/老化键；无种子时保留兼容默认值。确认提示不再承诺固定 50,000，而说明资金跟随难度配置、种子初值及运行态/存档槽边界。编译与取消/确认的 UI 自动化检查已由 TabletResetConfirmationReview 全量通过。

---

## 0. 结论摘要

1. 24 分类中 **16 类有专属渲染实现**（1/2/4/5/6/7/9/10/11/12/13/14/15/18/22/23；99 开发者选项另计），**8 类共用占位渲染**（`RenderPlaceholderCategory`，3/8/16/17/19/20/21/24）。
2. 有效消费（有源码证据）集中在：显示与亮度的帧率族键、`RetroLowResBG`（VN/BackgroundManager 确有消费）、`ShowFPS`、开发者项直接写入的经济/VN 键、音乐独立 JSON。
3. **仅写入未见消费**的键成簇出现：`TextSpeed`、`DialogAlpha`、`AutoPlay`/`SkipRead`/`ClickAdvance`/`ConfirmDialog`/`AutoInterval`（部分线索见 VNManager 但未附读取点）、音频四键写在 `GameData` 属性上（持久化路径未附）。
4. **空回调/叙事装饰**开关：锁屏 2 项（L512–513 `onChanged=null`）、无障碍 3 项（L800–802 恒 toast「尚未开放」）。
5. 三处存储边界必须严格分家：PlayerPrefs、经济/VN 持久化（同走 PlayerPrefs，但键域不同）、`MusicLibrary/library-v1.json` + 音乐旧兼容键。`PlayerPrefs.DeleteAll()` **不会**删音乐 JSON；音乐 `reduceMotion` 独立于系统减少动效。
6. **RetroLowResBG 消费确证**：`VN/BackgroundManager.cs:58` 读取，`bg_low/` 预渲染优先 + 动态降采样兜底 → 状态=**有效**（不得标「无实现」）。
7. 外观 JSON（`TabletPresentationPreferences.cs`）是自洽的持久化模块，但**接入面**（谁在读 `ReduceMotion/HighContrast/Accent/WallpaperDim`）在所附材料中**无消费者证据** → 全域矩阵中标「存储已就绪/效果待接」，不标完成。

---

## 1. 24 分类逐项归档

状态词表：`有效` = 写入 + 所附材料中有消费证据；`仅写入` = 写入键但消费端未附；`空回调` = 有交互无副作用或恒 toast；`叙事装饰` = 纯世界观展示无开关语义；`明确占位` = RenderPlaceholderCategory / 「尚未开放」；`待核验` = 线索存在但未附实现。

### 1.1 已实现分类（10 + 开发者）

#### 分类 1 · 当前账号 — `RenderAccountSettings` (L258–341)

| 控件 | 行号 | 回调 | 存储键 | 消费端证据 | 状态 |
|---|---|---|---|---|---|
| 账号卡（头像/昵称） | 263–301 | 无 | 读 `XiaomiAccount`（默认「旅人 (先民特等铁道运营官)」L287） | 读取即展示；写入端未附 | 叙事装饰 |
| 云空间卡 | 310–315 | 无 | 无 | 静态文案 | 叙事装饰 |
| 家庭服务卡 | 322–326 | 无 | 无 | 静态文案 | 叙事装饰 |
| 查找设备卡 | 328–332 | 无 | 无 | 静态文案 | 叙事装饰 |
| 账号安全等级行 | 337 | `ShowToast` | 无 | 仅 toast | 叙事装饰 |
| 个人人事档案行 | 338 | `ShowToast` | 无 | 仅 toast | 叙事装饰 |
| 系统权限授权行 | 339 | `ShowToast` | 无 | 仅 toast | 叙事装饰 |

> 注：本页文案含内网/人脸认证叙事，属世界观表达，按要求**不改写为「演示/待开发」**；无交互的卡片行已通过 `hasArrow:false` 与 `pickingMode=Ignore`（L906）呈现为只读展示，行为自洽。

#### 分类 2 · 我的设备 — `RenderMyDeviceSettings` (L344–412)

| 控件 | 行号 | 回调 | 存储键 | 消费端证据 | 状态 |
|---|---|---|---|---|---|
| 设备/OS/代号卡 | 348–366 | 无 | 无 | 静态 | 叙事装饰 |
| 硬件规格 7 行 | 369–377 | `null` 回调 | 无 | 只读 | 叙事装饰 |
| 游戏名称行 | 381 | `null` | 无 | 只读 | 叙事装饰 |
| **系统版本行（点 5 次解锁）** | 384–401 | `devModeClickCount++` → ≥5 写 `TabletDevModeUnlocked=1` + Save + 跳 99 | `TabletDevModeUnlocked` | L185 读取渲染入口；L484 退出时 DeleteKey | **有效**（入口门控） |
| 引擎/渲染管线/开发状态/许可/作者/灵感/字体/底层技术 | 403–410 | `null` | 无 | 只读 | 叙事装饰 |

#### 分类 4 · WLAN — `RenderWlanSettings`

| 控件 | 标识 | 回调 | 存储键 | 消费端证据 | 状态 |
|---|---|---|---|---|---|
| WLAN 专属页卡片 | `settings-wlan-card` | 无 | 无 | 静态只读说明：处于既有车载内网沙箱设定，不提供公共 Wi-Fi 接入/扫描入口，不显示无线链路实时状态 | **有效 · 只读去假化** |

#### 分类 5 · 蓝牙 — `RenderBluetoothSettings`

| 控件 | 标识 | 回调 | 存储键 | 消费端证据 | 状态 |
|---|---|---|---|---|---|
| 蓝牙专属页卡片 | `settings-bluetooth-card` | 无 | 无 | 静态只读说明：没有蓝牙扫描、设备发现与配对入口，不呈现附近设备状态 | **有效 · 只读去假化** |

#### 分类 6 · 互联互通 — `RenderInterconnectSettings`

| 控件 | 标识 | 回调 | 存储键 | 消费端证据 | 状态 |
|---|---|---|---|---|---|
| 互联互通专属页卡片 | `settings-interconnect-card` | 无 | 无 | 静态只读说明：编组协同由行车设备管理；此终端无编组控制、个人账户或云同步入口 | **有效 · 只读去假化** |

#### 分类 7 · 更多连接方式 — `RenderMoreConnectionsSettings`

| 控件 | 标识 | 回调 | 存储键 | 消费端证据 | 状态 |
|---|---|---|---|---|---|
| 更多连接方式专属页卡片 | `settings-more-connections-card` | 无 | 无 | 静态只读说明：无热点共享、无线投屏、外接显示控制入口；不展示检修接口状态 | **有效 · 只读去假化** |

#### 分类 10 · 锁屏 — `RenderLockScreenSettings` (L504–515)

| 控件 | 行号 | 回调 | 存储键 | 消费端证据 | 状态 |
|---|---|---|---|---|---|
| 锁屏时钟样式行 | 510 | `ShowToast("已使用默认时钟样式")` | 无 | 无状态切换 | 叙事装饰（箭头有误导，见 Q12） |
| 人脸数据录入行 | 511 | `ShowToast` | 无 | 无 | 叙事装饰（保持沉浸文案，不标非真实认证） |
| 抬起唤醒开关 | 512 | **`onChanged=null`** | 无（初始恒 true） | 点击仅切换视觉 | **空回调** |
| 双击亮屏与熄屏开关 | 513 | **`onChanged=null`** | 无（初始恒 true） | 同上 | **空回调** |

#### 分类 11 · 通知与状态栏 — `RenderNotificationAndStatusSettings` (L696–744)

| 控件 | 行号 | 回调 | 存储键 | 消费端证据 | 状态 |
|---|---|---|---|---|---|
| 打开通知中心行 | 722–727 | `RequestOpenSystemPanel(Notifications)` | 内存/TabletShellState | `GameMainUI.OpenSystemPanel` / `TabletShellView` 渲染通知中心 | **有效** |
| 打开控制面板行 | 729–734 | `RequestOpenSystemPanel(Controls)` | 内存/TabletShellState | 同上渲染控制面板 | **有效** |
| 通知收存机制说明 | 739 | `null` (hasArrow:false) | 无 | 说明 32 条容量与自动淘汰机制 | 叙事/说明 |
| 勿扰模式策略说明 | 740 | `null` (hasArrow:false) | 无 | 说明保留收存仅屏蔽横幅、进程内有效 | 叙事/说明 |
| 系统连接说明 | 741 | `null` (hasArrow:false) | 无 | 说明内网沙箱、不虚拟连接状态 | 叙事/说明 |
| 降级提示卡（无服务时） | 704–715 | 静态说明 + Toast | 无 | 独立运行/无服务时安全降级提示 | **有效** |

#### 分类 12 · 桌面 — `RenderHomeScreenSettings` (L518–534)

| 控件 | 行号 | 回调 | 存储键 | 消费端证据 | 状态 |
|---|---|---|---|---|---|
| **复古画风开关** | 524–530 | 写键 + Save + toast | `RetroLowResBG` 0/1 | **`VN/BackgroundManager.cs:58` 读取**；L61–78 `bg_low/` 优先 + 动态降采样；`SetBackground`/`SetBackgroundImmediate` 均走 `LoadBgTex` | **有效**（更正：绝不可标无实现） |
| 桌面图标排布行 | 531 | `null`（hasArrow=false） | 无 | 只读描述 | 叙事装饰 |
| 岁月 AI 桌面小部件行 | 532 | `ShowToast("小部件运行中")` | 无 | 无 | 叙事装饰 |

> 生效时机：toast 明示「下次进入剧情生效」——当前实现是**加载时读键**，运行中已展示的背景不热切换。验收须按此真实行为断言（见 AC-03）。

#### 分类 13 · 显示与亮度 — `RenderDisplaySettings` (L537–627)

| 控件 | 行号 | 回调 | 存储键 | 消费端证据 | 状态 |
|---|---|---|---|---|---|
| 全屏模式开关 | 544 | `Screen.fullScreen = v`（**未写 PlayerPrefs**） | 无 | 系统属性即时生效；重启恢复依赖 Unity 默认/外部设置 | 部分有效（重启恢复待核验，Q5） |
| 垂直同步开关 | 546–556 | `vSyncCount` + 写 `VSync`/`CustomFPS` + Save + targetFrameRate + 重建页 | `VSync`,`CustomFPS` | `LoginManager.cs:111,125` 启动恢复；`StationBulletinUI.cs:237,250,256` 双写点 | **有效**（三处写点需防漂移，Q6） |
| 自定义帧率开关 | 558–576 | 与 VSync 互斥写 + Save + 重建页 | `CustomFPS`,`VSync`,`TargetFPS` | 同上 LoginManager L125 | **有效** |
| 帧率上限滑条 30–240 | 578–583 | `targetFrameRate` + `TargetFPS` + Save；`enabled=!customFpsOn` | `TargetFPS` | LoginManager L125；StationBulletinUI L241,263–266 | **有效** |
| 显示帧率开关 | 585–591 | `ShowFPS` + Save + `FPSDisplay.SetActive` | `ShowFPS` | `FPSDisplay.cs:27,118,135`（启动恢复 + 外部写） | **有效** |
| 文字速度滑条 1–10 | 593–597 | `TextSpeed` + Save | `TextSpeed` | 所附消费者**未见读取**（VN 打字机未附） | **仅写入/待核验**（Q1） |
| 对话框透明度滑条 0–100 | 599–603 | `DialogAlpha` + Save | `DialogAlpha` | 所附消费者未见 | **仅写入/待核验**（Q1） |
| 界面缩放方案行 | 605 | `null`，hasArrow=false | 无 | 描述 ScaleMode=2（实现未附） | 叙事装饰（可接受） |
| 恢复显示默认按钮 | 607–624 | 重置 7 键 + fullscreen/vSync/targetFrameRate + 关 FPS + toast + 重建 | 同上 | 与各键消费端一致 | **有效** |

#### 分类 14 · 声音与振动 — `RenderAudioSettings` (L630–672)

| 控件 | 行号 | 回调 | 存储键 | 消费端证据 | 状态 |
|---|---|---|---|---|---|
| 主音量 0–100 | 637–640 | `GameData.MasterVolume = v/100f` | 经 GameData 属性（键名未附） | DebugPanel 列出 `MasterVolume` 等键（L164）→ 写在 PlayerPrefs 的可能性高，但 setter/消费未附 | **待核验**（Q2） |
| 背景音乐 | 642–645 | `GameData.BGMVolume` | 同上 | 同上 | 待核验 |
| 音效 | 647–650 | `GameData.SFXVolume` | 同上 | 同上 | 待核验 |
| 打字机音效 | 652–655 | `GameData.TypewriterVolume` | 同上 | 同上 | 待核验 |
| 铁道音效模拟行 | 657 | `null`，hasArrow=false | 无 | 静态描述 | 叙事装饰 |
| 恢复默认音量按钮 | 659–669 | 四属性回默认 + toast + 重建 | 经 GameData | 无显式 Save → **持久化是否触发待核验**（Q2） | 待核验 |

#### 分类 15 · 触控笔与键盘 — `RenderControlSettings` (L675–748)

| 控件 | 行号 | 回调 | 存储键 | 消费端证据 | 状态 |
|---|---|---|---|---|---|
| 每个 `KeyBindings.Action` 行 | 682–713 | `StartTabletRebind` → `TabletRebindAction` 挂接（L728–748） | 经 `KeyBindings.SetKey`（内部键未附） | 消费端：**谁调用 `TabletRebindAction` 未附**；标题行说明「Escape 取消」（L679）+ L734 处理 Escape/None → 取消逻辑在本文件内自洽 | **待核验**（Q3：按键分发器） |
| 恢复默认按键按钮 | 715–723 | `SetKey(action, GetKey(action))` | 同上 | **疑似自赋值 bug**：`GetKey` 若已是当前键则无「默认」语义，应为 `GetDefaultKey`（若有） | **存疑**（Q4，高优先级） |

#### 分类 18 · 应用 — `RenderAppsSettings` (L751–761)

| 控件 | 行号 | 回调 | 存储键 | 消费端证据 | 状态 |
|---|---|---|---|---|---|
| 进入应用管控行 | 757 | `ShowAppManagementPage` (L1391–1495) | 无（读 `SystemAppsEasterEgg` 状态） | 页面 + `RestoreApp`/`FactoryReset` 流程完整 | **有效** |
| 默认应用设置行 | 758 | `null`，hasArrow=false | 无 | 只读 | 叙事装饰 |
| 特殊权限设置行 | 759 | `ShowToast("权限运转正常")` | 无 | 无 | 叙事装饰 |

> 应用管理子页细节：`OnRestoreApp`(1497)、`PlayRestoreAnimation`(1519)、`PlayTimeCapsule`(1547) 均走 `ShowAppToast→ShowToast`；时间胶囊触发后 `FactoryReset()` 会重置 4 应用状态——**这是 EasterEgg 自身存储，不碰经济/VN/音乐**（若其内部键在 PlayerPrefs，属独立键域，需在数据保护用例中断言，见 AC-14）。

#### 分类 22 · 无障碍 — `RenderAccessibilitySettings` (L764–804)

| 控件 | 行号 | 回调 | 存储键 | 消费端证据 | 状态 |
|---|---|---|---|---|---|
| 4 胶囊 Tab（常规/视觉/听觉/肢体） | 775–796 | 改 `accessibilityTab` + 重建 | 无（静态字段） | 仅换标题文案（L798 同卡复用） | 有效（导航）但内容未分 Tab（Q7） |
| 无障碍快捷方式开关 | 800 | `_ => ShowToast("此功能尚未开放")`，初始 false | 无 | 点击开关视觉**会翻转**（BuildToggleRow 先翻 UI 再调 onChanged）→ 与 toast 矛盾 | **空回调 + 状态撒谎**（Q8，中优先级） |
| 高对比度文本开关 | 801 | 同上 | 无（**未接** `TabletPresentationPreferences.HighContrast`） | 外观 JSON 已有 `SetHighContrast`，但本页未调用 | **空回调**（Q9） |
| TalkBack 行 | 802 | 恒 toast 尚未开放 | 无 | 无 | **明确占位**（可隐藏） |

#### 分类 23 · 更多设置 — `RenderAdditionalSettings` (L807–845)

| 控件 | 行号 | 回调 | 存储键 | 消费端证据 | 状态 |
|---|---|---|---|---|---|
| 自动播放模式开关 | 813–817 | 写键 + Save | `AutoPlay` | VNManager 有独立 `isAutoPlay` 内存态与 Auto 按钮（L2937+），**启动时是否读 `AutoPlay` 键未附** | **仅写入/待核验**（Q10） |
| 跳过已读文本开关 | 818–822 | 写键 + Save | `SkipRead` | 读取端未附 | 仅写入/待核验 |
| 点击对话框推进开关 | 823–827 | 写键 + Save | `ClickAdvance` | 读取端未附 | 仅写入/待核验 |
| 确认对话框开关 | 828–832 | 写键 + Save | `ConfirmDialog` | VNManager 有 `ShowConfirmDialog`(1296)/`SetupConfirmDialog`(989)，是否读键未附 | 仅写入/待核验 |
| 自动播放间隔滑条 1–10 秒 | 833–837 | 写键 + Save | `AutoInterval` | AutoPlayTimer(3146) 的间隔来源未附 | 仅写入/待核验 |
| 本地数据备份与恢复行 | 842 | `ShowBackupRestorePage` (L1051) | 备份键族见 §3.2 | 页面内保存/恢复完整 | **有效** |
| 铁路百科图鉴行 | 843 | `ShowTipsPage(null)` (L1157) | 读 `Resources/Terms` | 完整渲染 + 立绘映射 | **有效** |

#### 分类 99 · 开发者选项 — `RenderDeveloperOptions` (L415–501) 【风险单列】

| 控件 | 行号 | 回调 | 存储键 | 消费/影响面 | 状态与风险 |
|---|---|---|---|---|---|
| 警示卡 | 419–431 | 无 | 无 | — | 叙事/警示 |
| 重置主线剧情进度 | 579–599 | 弹窗二次确认后逐 key Delete：`ArchiveStory_*`、`ArchiveScene_*`、`VN_Got0721`、`LastReadScript`、`DebugUnlockAllChapters` + Save | VN 键域 | 仅动 VN/成就类键，**不碰经济/音乐**；取消零副作用 | **有效 · 已接入二次确认**（Q11 已修） |
| 一键解锁全部剧情与CG | 449–460 | SetInt 上述键 + Save | VN 键域 | 同上 | 有效（中风险：同样无确认） |
| 重置经济模拟数据 | 617–629 | 弹窗二次确认后调用 `GameData.ResetState()` | 经济持久化（路径未附） | 仅经济；取消零副作用 | **有效 · 已接入二次确认**（Q11 已修） |
| 注入调试测试资金 | 471–476 | `AddMoney(100000)` + `AddTrust(30)` | 经济 | 仅经济 | 有效/待核验；信任+30 属**内部数值**（不外显于 UI 文案，保持） |
| 退出开发者模式 | 481–489 | 内存 false + `DeleteKey(TabletDevModeUnlocked)` + Save + 跳分类 2 | 门控键 | 入口即隐 | **有效** |
| 恢复出厂设置 | 491–499 | **`PlayerPrefs.DeleteAll()`** + Save + 重建 | **全部 PlayerPrefs** | **不含** `MusicLibrary/library-v1.json`；**会**清经济/VN/成就/音乐旧兼容键/账号名/门控键 | **有效 · 最高风险**（见 §3、AC-11/12） |

### 1.2 占位分类（14 类共用）

以下为审计初期的 14 类占位记录（L253, L848–861），三行统一「此功能尚未开放」toast。
> **2026-10-25 状态更新**：个性化（9）、通知与状态栏（11）、WLAN（4）、蓝牙（5）、互联互通（6）、更多连接方式（7）已全部移出占位表并建立专属页面。当前仍走占位渲染的分类仅剩 8 项（3、8、16、17、19、20、21、24）。

| # | 分类 | 历史状态 | 当前状态 |
|---|---|---|---|
| 3 | 平板特色功能 | 占位 | 占位 |
| 4 | WLAN | 占位 | **已去假化实现（只读说明页）** |
| 5 | 蓝牙 | 占位 | **已去假化实现（只读说明页）** |
| 6 | 互联互通 | 占位 | **已去假化实现（只读说明页）** |
| 7 | 更多连接方式 | 占位 | **已去假化实现（只读说明页）** |
| 8 | 小米超级AI | 占位 | 占位 |
| 9 | 个性化 | 占位 | **已接入实现（专属外观页）** |
| 11 | 通知与状态栏 | 占位 | **已接入实现（系统面板入口）** |
| 16 | 人脸与锁屏密码 | 占位 | 占位 |
| 17 | 隐私与安全 | 占位 | 占位 |
| 19 | 电池 | 占位 | 占位 |
| 20 | 健康使用设备 | 占位 | 占位 |
| 21 | 儿童模式 | 占位 | 占位 |
| 24 | 反馈 | 占位 | 占位 |

\* **例外更正**：分类 11「通知与状态栏」在 `SettingsCategories` 中为 id=11，`RenderSettingsDetail` **未给 case** → 实际落占位。但已核验状态称「首批通知/控制面板…通过 Unity」——**通知/控制面板功能在其他组件**（如 Control Panel/通知中心）实现，**不等于本设置页 11 号有实现**。这段为审计快照时点判断；后续接入与本轮验收边界见 Q13。

**占位页状态**：`明确占位`。按验收导向，建议从「开放感」文案改为禁用态/真实状态（如 WLAN 行显示「本机为离线内网终端」而非三行「尚未开放」）——文案建议见 §2。

### 1.3 开发者风险专节（与普通玩家入口隔离）

- **入口隐蔽性**：5 连击系统版本行解锁（L384），符合 Android 惯例；`TabletDevModeUnlocked` 持久化后左栏出现橙色入口（L217）。普通玩家可达但成本合理。
- **风险分级**：
  - **R1 致命（已消除）**：`恢复出厂设置`（`PlayerPrefs.DeleteAll()`）无确认入口此前已由 Codex 审查完全移除，当前源码不存在全清入口；
  - **R2 高（已解决）**：`重置经济`/`重置主线` 现已接入统一二次确认弹窗（`ShowResetConfirmDialog`），点击行仅展示影响范围与取消/确认选项，误触链已消除；
  - **R3 中**：DeleteAll 后未重新加载经济/VN 运行态（仅重建设置页）→ **内存中 GameData 与磁盘不一致**，直到下次冷启动才收敛（Q14）。
  - **R4 低**：解锁键 `TabletDevModeUnlocked` 会被 Debug 类工具列出（DebugPanel 未列出该键，暂低危）。
- **文案原则**：开发者页保持现有警示语气即可（已是「高权限调试指令，请谨慎操作」）；**不**给玩家入口贴「开发中」标签；但**必须**加确认（见任务卡 B3）。

---

## 2. 消费链追踪问题/确认清单（22 项）

优先级：P0=阻断数据安全或核心体验；P1=功能正确性；P2=一致性/文案；P3=优化。

| # | 优先级 | 问题/确认项 | 证据 | 责任人 | 中文推荐文案（如涉及 UI） |
|---|---|---|---|---|---|
| Q1 | P1 | `TextSpeed`/`DialogAlpha` 读取端未附；若 VN 打字机/对话框不读，则两滑条为**仅写入** | L593–603；locator 无命中 | VN 负责人 | 若接通：无需改文案；若确认不接：暂时禁用滑条并注「随剧情系统一并开放」 |
| Q2 | P0 | `GameData.MasterVolume` 等四属性的持久化与消费（是否 Set 时写 PlayerPrefs、是否 Save、AudioMixer 消费点）未附；恢复默认按钮**无显式 Save** | L637–669；DebugPanel 仅列键名 | 经济/音频负责人 | toast 保持「音频设置已恢复默认」 |
| Q3 | P1 | `TabletRebindAction` 的分发器（设置页期间的按键捕获、Escape 分层归属）未附——已核验 Escape 分层通过，但**绑定页内**的调用点需确认 | L54,728–748 | Tablet Shell | 按钮态文案已用「按下新键...」可保留 |
| Q4 | 已修复，待Unity验收 | 恢复默认按键 `SetKey(action, GetKey(action))` 曾为自赋值，现按钮调用 `KeyBindings.ResetToDefaults()` | 原快照 L717–718；现源码 `TabletSettingsAppUI.cs:729` | Tablet Shell | 打乱多个键后恢复四个默认键；确认其他设置不变 |
| Q5 | P2 | `Screen.fullScreen` 不落盘，重启行为依赖引擎默认 | L544 | 显示 | 开关文案可加「重启后恢复全屏」 |
| Q6 | P2 | VSync/CustomFPS/TargetFPS 三写点（设置页、StationBulletinUI、LoginManager 读）存在漂移风险（如 Bulletin 只写 VSync 不写 CustomFPS 的分支） | locator L237–266 | 显示 | — |
| Q7 | P3 | 无障碍 4 Tab 内容完全相同（同 card 复用），Tab 仅改标题 | L775–803 | 无障碍页 | 分 Tab 补真实分组，或收敛为单页 |
| Q8 | P1 | 快捷方式/高对比度开关：**UI 翻转后 toast「尚未开放」**，状态与反馈矛盾 | L800–801 + BuildToggleRow L986–992 | 无障碍/外观 | 改为**禁用态**（SetEnabled(false)）+ 副标题「尚未开放」，点击不翻转；或隐藏 |
| Q9 | P1 | 高对比度未接 `TabletPresentationPreferences.SetHighContrast` | L801 vs TabletPresentationPreferences L170 | 外观（反重力） | 接通后：「高对比度文本 / 增强浅色界面的文字辨识度」 |
| Q10 | P1 | `AutoPlay/SkipRead/ClickAdvance/ConfirmDialog/AutoInterval` 启动读取点未附；VNManager Auto 为内存态，**设置页与 VN 内 Auto 按钮可能不同步** | L813–837；VNManager 3134+ | VN | 「自动播放模式 / 开场后自动推进对话」保持 |
| Q11 | ✅ 代码已修且自动化验收通过 (2026-10-24) | 开发者两项破坏性操作（重置主线进度、重置经济数据）已接入统一二次确认；经济重置行文案与弹窗文案完全消除固定数值承诺，说明难度资金与种子初值；`TabletResetConfirmationReview.Batch` 4 项自动化用例在 Unity 6.5 batchmode 全量通过（`TABLET_RESET_CONFIRMATION_REVIEW_OK`，退出码 0） | `TabletSettingsAppUI.cs:437-629`; `Assets/Editor/TabletResetConfirmationReview.cs` | Tablet Shell | 经济提示现说明难度资金、种子值及存档槽边界；取消/确认/去重/生产行绑定自动化验收全部通过 |
| Q12 | P2 | 锁屏时钟样式/人脸行有箭头暗示可点，实为 toast | L510–511 | 锁屏 | 去掉箭头（hasArrow=false）或文案保持现状但去 chevron |
| Q13 | ✅ 最终复验通过 (2026-10-25) | 分类 11 专属详情、通知/控制面板入口、无回调降级、状态纯洁性与文案断言通过；修正后的通知 Review 将自身范围标为前三项 `PARTIAL`，旧系统壳行为另由 `TabletSystemShellReview.Batch` 实际 Play Mode 验证通过，覆盖面板互斥、遮罩关闭、Escape、勿扰与 1440/1024 布局（`TABLET_SHELL_RUNTIME_OK`，退出码 0） | `Assets/Scripts/UI/Tablet/Apps/TabletSettingsAppUI.cs`; `Assets/Scripts/GameMainUI.cs`; `Assets/Editor/TabletSettingsNotificationReview.cs`; `Assets/Editor/TabletSystemShellReview.cs`; `tools/compile/tablet_settings_notifications_review_codex_20261025.log`; `tools/compile/tablet_system_shell_review_complete_20261025.log` | Tablet Shell | 分类 11 与既有系统壳回归均已通过；Android 实体平板 QA 仍需真机检查 |
| Q14 | P0 | DeleteAll 后运行态未重载（经济/VN/账号名） | L491–499 | 经济 + Shell | toast 后追加「即将返回登录/重载存档」并实际触发重载 |
| Q15 | P0 | **三存储边界回归**：DeleteAll 后断言音乐 JSON 仍存；反向断言恢复出厂**不**声称清空音乐库 | §3 | 测试 | 出厂 toast 现为「已清除所有本地存储」→ **不准确**，应改「已重置本机档案与偏好」（音乐文件库不受影响） |
| Q16 | P1 | 音乐旧兼容键 `TabletMusic_Vol` 迁移（MusicLibrarySession L128）在 DeleteAll 后：迁移已完成的存档无碍；**未迁移用户**出厂后卷宗初始值变化 | locator L128 | 音乐 | — |
| Q17 | P2 | 搜索框每次击键全量重建页面（L119–123），输入焦点在重建后丢失风险 | L112–123 | Tablet Shell | — |
| Q18 | P2 | 侧栏搜索仅匹配 `cat.name.Contains`，不索引详情项与「开发者选项」兼容名依赖 L186 特判 | L137,186 | Tablet Shell | — |
| Q19 | P3 | `BuildToggleRow` 无 `SetEnabled` 参数 → 无法表达禁用态（Q8 的根因） | L941–995 | Tablet Shell 控件库 | — |
| Q20 | P1 | 备份快照收集 61+6+4 键，但 `RestoreBackup` **只恢复 `VN_Save_`/`SaveSlot_`**（L1382）→ Achievements/Bookmarks/VNExitData 备份了不恢复 | L1349–1388 | 存档 | toast「恢复完成」→ 应说明恢复范围：「已恢复存档槽（成就与书签暂不随恢复写入）」或补恢复 |
| Q21 | P2 | 备份快照格式 `key=value\n`：值含换行会破坏解析（L1364,1375–1386） | 同上 | 存档 | — |
| Q22 | P0 | 外观 JSON 四态（减少动效/高对比/强调色/壁纸压暗）**无任何系统级消费者证据**（未见锁屏/桌面/通知读取） | TabletPresentationPreferences 全文；locator 无命中 | 外观（反重力） | 见 §4 矩阵；接入前设置页**不得**放出这四个总开关（避免再次「仅写入」） |

---

## 3. 三类真实存储边界（更正上一轮误报）

### 3.1 PlayerPrefs（键值本地存储）

- **覆盖面**：显示族（VSync/CustomFPS/TargetFPS/ShowFPS/TextSpeed/DialogAlpha）、游戏行为族（AutoPlay/SkipRead/ClickAdvance/ConfirmDialog/AutoInterval）、门控（TabletDevModeUnlocked）、账号显示名（XiaomiAccount）、复古画风（RetroLowResBG）、备份（SUIYUE_Backup_Json/Time）、VN/成就键（VN_*、Archive_*、LastReadScript、Achievements_Data、Bookmarks_Data、SaveSlot_*）、以及**疑似**音频族（MasterVolume 等，待 Q2）、**疑似**音乐旧兼容键（TabletMusic_Vol）与按键映射。
- **`PlayerPrefs.DeleteAll()` 影响**：清空上述全部；**不触及** `Application.persistentDataPath` 下的文件。
- **经济/VN**：经济（GameData）与 VN 存档均属 PlayerPrefs 键域（VN 侧键名有直接证据；经济持久化机制待 Q2 确认）——**同域不同键**，DeleteAll 同时清空两者，但**正常操作路径下**（设置页/开发者页各自回调）互不清空。

### 3.2 备份键（PlayerPrefs 内的独立命名空间）

- `SUIYUE_Backup_Json` / `SUIYUE_Backup_Time`：**不会**被 `SaveBackup` 之外的键逻辑触碰；`RestoreBackup` 只回写 VN_Save_*/SaveSlot_*。
- DeleteAll 会清备份 → 出厂后「暂无备份可恢复」为预期行为（AC-11 断言）。
- **风险**：备份键在 PlayerPrefs 内 → DeleteAll 无差别清除（可接受，但 toast 文案需准确，Q15）。

### 3.3 独立音乐 JSON（`Application.persistentDataPath/MusicLibrary/library-v1.json`）

- **证据**：`MusicLibrarySession.cs:119` 路径；`MusicLibraryStore.Load/Save`（L47–91）文件读写；`MusicLibrarySession.Save()` 由 `MusicSessionDriver` 的 Pause/Quit（L8–9）与各视图回调触发。
- **更正 1（上一轮误报）**：`PlayerPrefs.DeleteAll()` **不会**直接删除 `MusicLibrary/library-v1.json`，也不会删除曲目文件/歌词文件（LyricsDirectory 同为 persistentDataPath 下文件系统）。**「恢复出厂清空音乐库」是误报**。
- **更正 2**：`RetroLowResBG` 在 `VN/BackgroundManager.cs:58` **确有消费**（含 bg_low 预渲染与动态降采样两级实现），**不得**标为无实现/纯写入。
- **风险分述（勿混谈）**：
  - **R-音乐A**：`library-v1.json` 内 `volume`、`lyricMotion`、`lyricFontSize`、`reduceMotion`、favorites/playlists/queue 等**完全独立**，DeleteAll 后保持 → 出厂≠音乐偏好重置（若产品语义要「全清」，需另行显式删文件，**本批不做**，且不得与系统减少动效设置混淆）。
  - **R-音乐B（旧兼容键）**：`PlayerPrefs.GetFloat("TabletMusic_Vol", 0.65f)` 迁移逻辑（locator L128）——迁移已完成（键已写入 JSON）的设备无感；**迁移前**设备若先出厂再进音乐 App，卷宗将回到 JSON 默认而非用户旧音量。与「音乐解锁元数据」（`Unlocked`/收藏等若曾依赖 PlayerPrefs 键，所附材料未见旧键名）**必须分开描述**：解锁元数据在 JSON 内则不受 DeleteAll 影响；若有未知旧解锁键，列**待核验 Q23**（不在本批材料内臆断）。
  - **R-音乐C**：DeleteAll 后 `TabletDevModeUnlocked`、账号名等重置，与音乐 JSON 并存 → 出厂后可能出现「档案已重置但歌单仍在」——这是**正确**的边界行为，验收需确认 UI 不误报音乐被清。

---

## 4. 外观四态全域状态矩阵

存储就绪 ≠ 效果生效。`TabletPresentationPreferences` 提供：`ReduceMotion` / `HighContrast` / `Accent(Amber|Teal|Blue)` / `WallpaperDim(0–0.4)`，JSON 持久化 + 只读高版本 + 原子写（temp+Replace）——**模块本身质量良好**。但所附材料中：

- 未见该类的**构造/持有者**（谁 new、路径从哪来）；
- 未见 `Changed` 事件的订阅者；
- 未见锁屏/桌面/通知/设置页读取这四态的代码。

因此矩阵如下（本批已接 = 所附材料有消费证据）：

| 目标面 | 减少动效 | 高对比度 | 强调色 | 壁纸压暗 | 证据/说明 |
|---|---|---|---|---|---|
| 存储层（JSON） | ✅ 就绪 | ✅ 就绪 | ✅ 就绪 | ✅ 就绪 | TabletPresentationPreferences.cs 全文 |
| 设置页 UI（开关暴露） | ❌ 未接（无障碍页未调用） | ❌ 未接（L801 空回调） | ❌ 未接 | ❌ 未接 | 待第二批接入审查（Codex 进行中） |
| 锁屏 | ⬜ 待接 | ⬜ 待接 | ⬜ 待接 | ⬜ 待接 | 无消费证据 |
| 桌面（壁纸/Dock/图标） | ⬜ 待接 | ⬜ 待接 | ⬜ 待接 | ⬜ 待接（压暗目标态即壁纸层） | 无消费证据 |
| 通知 / 控制面板 | ⬜ 待接 | ⬜ 待接 | ⬜ 待接 | N/A（无壁纸） | 首批通知已通过验证，但**未**读外观四态 |
| 设置页内部动效（重建页动画/开关过渡） | ⬜ 待接 | ⬜ 待接（现用固定 Ink 色） | ⬜ 待接（AccentSystem 硬编码） | N/A | — |
| VN 背景转场 | ⬜ 待接（减少动效应缩短 0.4s/0.5s fade/slide） | ⬜ 待接 | N/A | N/A | BackgroundManager 独立动画，**未**读 ReduceMotion |
| **音乐 App** | ✅ **独立生效**（`State.reduceMotion` → `music-reduce-motion` 类、歌词滚动/舞台效果抑制，locator L84–85,232,241,442,463） | ⬜ 待接 | ⬜ 待接 | N/A | **本批不修改**音乐独立减少动效设置（用户明确约束） |
| 其他 App（笔记/浏览器等） | ⬜ 待接 | ⬜ 待接 | ⬜ 待接 | ⬜ 待接 | 未提供 |

**判定规则**：「偏好存储代码交回 ≠ 效果已生效」。矩阵中仅音乐减少动效与存储层为 ✅；其余全部 ⬜/❌，直到出现具体读取点。第二批接入由 Codex 审查中，**本文不预标完成**。

---

## 5. 可执行验收用例（16 条）

> 声明：以下为**设计好的验收用例，本文未执行任何一条**。执行前置 Unity 播放/构建；断言以 PlayerPrefs、JSON 文件与 UI 行为准。

| ID | 前置 | 动作 | 期望 | 数据保护断言 |
|---|---|---|---|---|
| AC-01 | 开发者已解锁 | 设置内 Escape 分层：从子页→分类列表→关闭 | 按已核验的 Escape 分层逐级退出，不直接杀 App | 无写操作；键数不变 |
| AC-02 | 分类 13 改 TargetFPS=144、ShowFPS=开 | 退出设置→重启游戏→进设置与角落 HUD | TargetFPS 显示 144；FPS 角标出现（FPSDisplay L27,135） | 断言键值仍在；不触碰 VN/经济/音乐键 |
| AC-03 | RetroLowResBG=0，VN 有 bg 资源 | 开复古画风→toast 提示→返回→重进 VN 场景 | 进入后背景为低清/点采样（bg_low 或 4x 降采样）；**本局已显示背景不热切换** | JSON 与 PlayerPrefs 音乐键不变 |
| AC-04 | RetroLowResBG=1 | 关闭复古→toast「已恢复原始高清背景」→重进 VN | 背景走 `bg/` 高清路径（L58–59 分支） | 同 AC-03 |
| AC-05 | 设置页打开，无备份 | 点击「恢复上次备份」 | toast「暂无备份可恢复」；不崩溃 | `SUIYUE_Backup_*` 不被创建 |
| AC-06 | 已备份（含 VN_Save_1 非空） | 改 VN_Save_1 为他值→恢复备份 | VN_Save_1 回到备份值；toast 行为符合 Q20 结论文案 | Achievements_Data 若未恢复，断言其**保持现值**并在 UI 文案中如实说明 |
| AC-07 | 焦点在侧栏搜索框 | 输入「显示」→ 每键观察 | 过滤只剩分类 13（+命中项）；**焦点与光标不丢**（Q17 若未修则记 bug） | 无写 |
| AC-08 | 分类 13 状态打乱 | 点「恢复显示默认设置」 | 7 键回默认、FPS 关、重建页显示默认值 | 断言 `RetroLowResBG`、音乐 JSON、VN 键**未变** |
| AC-09 | 分类 15 按键绑定页，按流程点「重新绑定」 | 按 Escape | **取消**：按钮文案回原键，不写新键（L734–744） | KeyBindings 存储键字节级不变 |
| AC-10 | 分类 15 打乱多个键 | 点「恢复默认按键配置」 | 四个映射恢复为默认表（Space、RightArrow、F5、Escape） | 其他设置不变；需Unity验收 |
| AC-11 | 记录全量：经济快照、VN 进度、成就、门控键、音乐 JSON 内容 hash、音乐目录文件数 | 开发者→恢复出厂设置 | **弹出确认**（Q11 修复后）→ 确认后：PlayerPrefs 键基本清空；toast 文案不宣称清空音乐库（Q15） | **断言 `MusicLibrary/library-v1.json` 内容与文件仍在、未被改写**；音乐曲目/歌词文件数不变 |
| AC-12 | AC-11 完成后同会话 | 不重启，查看经济数值/账号名/设置页 | 运行态与磁盘一致（Q14 修复：触发重载）或明确提示需重载 | — |
| AC-13 | 屏幕 1440×1024 与另一分辨率（如 1920×1200/非整数缩放） | 打开设置全 24 分类 + 应用管理 + 图鉴 | 左栏 300px 与右栏布局不破版；滑条/开关可点（已核验 1440/1024 通过 → 本条为回归 + 外推） | 无写 |
| AC-14 | 时间胶囊未触发；经济为 X、VN 第 3 幕、音乐 JSON hash=H | 应用管理依次恢复 4 App 至触发时间胶囊 | 彩蛋按 L1547–1568 播放并 FactoryReset（EasterEgg 自身状态） | **断言经济 X 不变、VN 第 3 幕不变、音乐 JSON hash=H** |
| AC-15 | 外观 JSON 文件伪造 `schemaVersion:2` | 构造 prefs → 任意 SetReduceMotion | 进入只读：LastError 提示、**不覆盖写**（L101–116,156–161） | 原文件字节不变 |
| AC-16 | 外观 JSON 删除/清空/非 JSON 文本 三种坏文件 | 构造 prefs | 回退默认 + LastError；文件保持原样（L71–99） | 不写回不删除 |
| AC-17（补充） | 未解锁开发者 | 查看左栏 | 无「开发者选项」入口（L185 条件）；搜索「开发者」也不出现 | 门控键=0 |
| AC-18（补充） | 触摸设备/触摸+鼠标混合 | 开关、滑条、5 连击、重绑定按钮 | Pointer/Click 事件均可达；5 连击在触摸下计数正确；未知硬件（无键盘）下重绑定页给出触摸提示或禁用行而非卡死 | 无写 |

**覆盖核对**：退出/返回（AC-01）、重启恢复（02/03/04）、输入焦点（07）、取消/重置（05/08/09/10）、保存失败（AC-15/16 覆盖只读/坏文件；TrySave 失败路径 L291–295 断言「仅会话生效」toast）、分辨率（13）、触摸与未知硬件（18）、数据保护（06/11/12/14）。

---

## 6. 下一批 3 张功能任务卡

### 卡 A1 — 设置页「占位/空回调」治理与文案对齐（P1）

- **范围**：14 个占位分类、锁屏 2 空开关、无障碍 3 假开关、分类 11 衔接、箭头误导行。
- **依赖**：Q13 确认通知/控制面板的跳转 API；Q8 需要 `BuildToggleRow` 支持禁用态（可与卡 A3 合并改控件库）。
- **具体交付**：
  1. `BuildToggleRow` 增加 `enabled` 参数（默认 true），禁用时灰显且点击不翻转；
  2. 锁屏「抬起唤醒/双击亮屏」：无真实消费前设为禁用+副文案「跟随系统感应策略」（**不**写「未实现」）；
  3. 无障碍：TalkBack 行隐藏或禁用；「无障碍快捷方式」禁用态；「高对比度文本」**暂**禁用直至卡 A2 接通；
  4. 占位分类改单行真实态文案（例：WLAN→「内网专线：已连接（无需手动配置）」；蓝牙/互联互通→「本终端未开放无线协议」；反馈→跳转已有反馈/事务入口或禁用；电池/健康/儿童模式→禁用态单行）；
  5. 分类 11 接入已实现的通知/控制面板入口；未接通前禁用。
- **验收**：AC-01/07/13/17 + 目视 24 类无「三行尚未开放」模板；所有禁用开关点击**不**翻转。
- **不碰**：`MusicLibrarySession.cs`、`MusicLyricsView.cs`、任何音乐文件；`TabletPresentationPreferences.cs`（卡 A2 负责）；经济/VN 键写入逻辑；`BackgroundManager.cs`。

### 卡 A2 — 外观 JSON 全域接入（减少动效/高对比度/强调色/壁纸压暗）

- **范围**：为 `TabletPresentationPreferences` 四态建立**读取端**与设置页开关；按 §4 矩阵逐面接通。
- **依赖**：第二批外观 JSON/视图代码（反重力交回，Codex 审查中）→ 审查通过才开工；构造点/文件路径确定。
- **具体交付**：
  1. 持有者（单例）+ `Changed` 广播；
  2. 设置页新分组（建议分类 13 或 9）暴露四控件：减少动效（总）、高对比度、强调色三选、壁纸压暗 0–40% 滑条——**接入一个开放一个**；
  3. 消费面（本批最低集）：锁屏、桌面壁纸（压暗叠加层）、设置页自身 Accent/动效、通知面板高对比；VN 转场缩短为**可选**（若动则读总开关，不改音乐独立 reduceMotion）；
  4. 接通高对比度后回填无障碍页 Q9。
- **验收**：AC-15/16 + 每态「开关→跨 App 可见→重启恢复」；**断言音乐 `State.reduceMotion` 不被总开关改写**。
- **不碰**：`MusicLyricsView.cs`、`RailwayMusicTerminalView.cs` 的 reduceMotion 逻辑；经济/VN；`TabletSettingsAppUI` 占位文案（卡 A1）。

### 卡 A3 — 数据安全：破坏性操作确认 + 备份语义修正 + 出厂边界

- **范围**：开发者页四操作确认、DeleteAll 运行态重载、出厂 toast 文案、备份恢复范围（Q20/21）。
- **依赖**：Q2 经济持久化路径确认；Q14 重载钩子（回登录或 GameData 重初始化）。
- **具体交付**：
  1. 统一确认弹窗组件（复用 VN/LowLoginManager 既有 `ShowConfirmDialog` 模式）：重置经济、重置主线、注入资金（可选）、**恢复出厂**；
  2. 出厂流程：确认 → DeleteAll → 显示态/经济/VN 运行态重载或回到登录；toast 改「已重置本机档案与偏好（音乐文件库保留）」；
  3. 备份：恢复范围与文案一致（恢复成就/书签或改 toast）；快照编码防换行（换行转义或 JSON 快照）；
  4. Q4 按键默认恢复修复。
- **验收**：AC-05/06/08/10/11/12/14 全绿；**音乐 JSON hash 断言**为本卡门禁。
- **不碰**：`MusicLibrarySession` 的 Load/Save 与迁移逻辑；外观 JSON（卡 A2）；占位文案（卡 A1）；`BackgroundManager`。

---

### 可直接写入 PROJECT_OVERVIEW 的进度摘要

> **平板设置全量审计（2026-10-23）**：完成 24 分类源码级归档——10 类已实现 + 开发者 99、14 类占位共用渲染；`RetroLowResBG` 经 `VN/BackgroundManager` 确证有效（bg_low 预渲染 + 动态降采样）；显示帧率族（VSync/CustomFPS/TargetFPS/ShowFPS）三写点消费确证；文字速度/透明度/VN 行为五键与音频四键消费端待核验。存储三边界厘清：`PlayerPrefs.DeleteAll()` **不清** `MusicLibrary/library-v1.json`（上轮误报已更正），音乐旧兼容键 `TabletMusic_Vol` 迁移风险单列；经济/VN/音乐正常路径互不清空。外观 JSON（减少动效/高对比/强调色/壁纸压暗）持久化就绪但**全域消费未接**，矩阵全部待接（音乐 reduceMotion 独立、保持不动）。开发者页四项破坏性操作无二次确认、出厂后运行态不重载列为 P0。已产出 22 项问题清单（含 4 项 P0）、18 条验收用例（**未执行**）与下一批任务卡 A1 占位治理 / A2 外观接入 / A3 数据安全确认，待 Codex 审第二批接入后开工。

---

**免责与边界**：本文仅基于所附三份源码快照与 cross-file locator；locator 标记为「线索而非运行时证明」；未附消费端一律标待核验；未运行任何测试/游戏；未做联网研究；未修改任何文件。
