# 平板端小米澎湃OS（HyperOS Pad）前端：超级岛、面板、应用外观

> **版本**：v2.1（2026-11-01，追加状态栏点按、电量徽标、设置系统整理）。本文取代 2026-10-29 的 v1.0。v1.0 把超级岛写在“状态栏左侧展开”、控制中心写成 440px 实色侧栏，并声称“双纵向胶囊滑块”已实现；这些与实际画面不符，已按参考截图和当前源码改正。
> **验证口径**：下列“已验证”仅指 Unity 6000.5.8f1 批处理编译通过、对应审查输出 OK 标记、并人工看过审查截图。Android 真机、触屏手势、高对比度/减少动效的实际观感均未验证，见末尾“未验证与待办”。

## 一、参考与原则

参考对象是真实 HyperOS Pad 的界面（用户提供的 6 张设置界面与桌面/通知/控制中心截图）。原则：

1. 结构和比例向真实系统靠拢：壁纸铺满、状态栏叠在壁纸上、通知与控制中心都是整屏模糊上的卡片列。
2. 铁道游戏的元素只做颜色和细节：信号灯红黄绿、黄铜细节，不改骨架。
3. 本终端是车载内网沙箱：不虚构 Wi-Fi、蓝牙、蜂窝或设备配对状态。状态栏只放真实状态（时间、电量、勿扰）。

## 二、布局与交互（已验证）

| 区域 | 当前实现 | 代码 |
| :--- | :--- | :--- |
| 状态栏 | 无整条底色；桌面上叠一层顶部渐变保证白字可读。左侧：时间、日期（有超级岛时隐藏日期）、未读红点；右侧：勿扰月亮（开启时）和电量徽标（深灰圆角底 + 按电量铺的浅灰填充 + 白色粗体数字，深浅背景下都清晰）。**不再有“通知/控制”文字按钮**：点按左侧时间区呼出通知中心、点按右侧状态区呼出控制中心，也可用顶部下拉手势；原按钮节点保留但 `display:none`，供无障碍与审查按名称触发。打开浅色应用时换成与应用顶栏一致的浅色底和深色文字 | `GameMainUI.BuildDesktop`、`ApplyStatusBarForApp`、`BuildStatusBarScrim` |
| 超级岛紧凑态 | 状态栏左侧、紧随时间的纯黑小胶囊（高 34px，最小宽 96px）。音乐时只显示唱片缩略图和声波，不显示歌名；其他类型仍显示各自的指示（车速、录音计时等）。副岛为独立小圆球 | `TabletSuperIsland.RenderCompactContent` |
| 超级岛展开态 | 点击后在屏幕上方正中展开：宽 460px（最多 90% 屏宽）、圆角 34px、纯黑，底部有一条黄铜色把手；带缩放与淡入过渡，开启“减少动效”时直接到位。展开时左侧胶囊隐藏 | `TabletSuperIsland.SetExpanded`、`PlayExpandAnimation` |
| 通知中心 | 从状态栏**左半边**下拉触发；整屏模糊上居中一列（屏宽约 47%，限 420–640px，窄屏接近全宽）。顶部是大号时钟与日期，通知为浅色玻璃卡片，右上圆形关闭键，“全部清除”为浅色胶囊。空态文字保持“暂无通知” | `TabletShellView.RenderNotifications` |
| 控制中心 | 从状态栏**右半边**下拉触发；整屏模糊上靠右一列（屏宽约 31%，限 380–480px）。内容：勿扰/高对比度两张大胶囊，播控卡与亮度、音量两条竖向滑条，动效/强调色/设置三个圆形开关，“车载内网 · 离线沙箱”说明，打开完整设置按钮 | `TabletShellView.RenderControls` |
| 触发区 | 屏幕顶部 48px（原 24px）。上沿左半屏通知、右半屏控制；底沿与侧沿行为不变。**鼠标（Windows）**：左键从顶部 48px 起下拉同样有效（`GameMainUI.OnSystemPointer*`，仅限顶部下拉；侧滑返回、底部上滑仍只给触屏）；手势审查有三条鼠标断言 | `TabletEdgeGesture.Begin`、`GameMainUI.OnSystemPointerDown` |
| 面板动画 | 面板从上方 28px 处落下并淡入（240ms），开启减少动效时直接到位 | `TabletShellView.PlayPanelOpenAnimation` |

滑条行为：亮度滑条对应壁纸压暗（压暗范围 0–40%，填充越满越亮）；音量滑条对应主音量。点击切换（压暗开关/静音），纵向拖动调节，拖动过程只改填充，松手时一次性提交，避免重建界面打断拖动。

保留的节点名（审查依赖）：`tablet-shell-panel`、`tablet-shell-launcher`、`tablet-status-right`、`tablet-notifications-open`、`tablet-controls-open`、`tablet-quiet-toggle`、`tablet-high-contrast-toggle`、`tablet-reduce-motion-toggle`、`tablet-accent-cycle`、`tablet-wallpaper-dim-toggle`、`tablet-audio-mute-toggle`、`tablet-media-card`、`tablet-media-play-toggle`、`tablet-notifications-clear`、`tablet-shell-settings`、`settings-hardware-offline-box`、`super-island-pill`、`super-island-expanded`、`super-island-sub-bubble`。

## 三、主题令牌、图标与字体

- **`TabletTheme`**（`Assets/Scripts/UI/Tablet/TabletTheme.cs`）：深色玻璃面的语义色、白色叠层阶梯、文字三档、状态色；信号灯 `SignalRed/Yellow/Green` 与黄铜 `Brass`；运行时外观状态（强调色、高对比度、减少动效）由 `GameMainUI.ApplyPresentation` 写入，`MotionMs()` 在减少动效时返回 0。`TabletUIHelper.AccentSystem` 现在跟随强调色。
- **`TabletIcons`**（`TabletIcons.cs`）：用 Painter2D 绘制的矢量图标：月亮、太阳、喇叭（含静音）、闪电、调色盘、齿轮、对比度、铃铛、音符、搜索。尺寸按元素缩放，颜色可随状态改变。桌面“音乐/设置/天气”图标、控制中心磁贴和状态栏“通知/控制”按钮使用它们。其余桌面图标仍是单字符号，不是矢量图。
- **`TabletFonts`**（`TabletFonts.cs`）：系统界面使用 MiSans（`Resources/Fonts/MiSans-Regular.ttf` 常规、`MiSans-Semibold.ttf` 标题）；找不到时退回像素字体 zpix。剧情、登录等游戏内容仍用 zpix，音乐窗口歌词区单独指定像素字体。
- **字体许可未确认**：压缩包内没有许可协议文件。官网 FAQ 称可作为嵌入字体，但需在软件中注明使用 MiSans，且不得单独修改字体；发布前须通读《MiSans 字体知识产权许可协议》并在“关于”页署名（尚未添加）。

## 四、应用外观

- **应用窗口与顶栏**：默认浅色（窗口 0.97 灰白、顶栏同色、返回/关闭为淡灰圆角块）。深色保留给 `五线态势`、`岁月 · AI对话`、`驾驶台`（`GameMainUI.BuildFullScreenAppContainer`）。音乐应用使用自己的窗口，不经此容器。
- **桌面与 Dock**：应用图标统一为 60px 正方形圆角方块（`BuildIconTile`，桌面与 Dock 共用）；Dock 为浅色磨砂圆角条，图标 54px；“铁路”文件夹缩略图为固定 2×2；桌面底部中央有小指示条。底部导航条按钮去掉外框，只留淡色符号。
- **设置侧栏**（`TabletSettingsAppUI.BuildSettingsPage`）：大标题“设置”、胶囊搜索框（矢量放大镜与“搜索设置”占位字）、按分组的白色圆角卡片、34px 图标徽章、选中项灰底。右侧页面标题与分区标题使用 MiSans 半粗体。
- **设置页分区与取值行**：卡片圆角 24px、无描边；短说明（不超过 12 字）显示为行右侧取值，长说明留在标题下方；新增 `BuildInfoRow`（标题 + 右侧取值 + 一行说明 + 可选箭头）。滑条为细圆角轨道加强调色圆形滑块（`StyleHyperSlider`）。分区标题为淡紫灰色。
- **账号页**：头像 76px、名字 24px 半粗体、副标题灰色；云空间为淡蓝大卡带“512 GB”大数字；安全等级/人事档案/权限授权三行为右侧取值（最高特级 / 已同步 / 22 个应用），原长句移到点击提示。侧栏顶部账号行为圆形头像 + 名称 + 一行说明。
- **我的设备页**：顶部为 HyperOS 渐变横幅（薰衣草→粉→淡蓝，居中 “Xiaomi HyperOS” 与版本号）；四项主规格为两列“数值 + 说明”网格；新增“备份与恢复”入口，打开真实的本地存档快照页。
- **主题化页面**（小米超级AI、人脸与锁屏密码、隐私与安全、健康使用设备、儿童模式、反馈）：改成取值行，去掉“本页没有…/不代表…”式免责长句。内容只用设定文档里的事实和如实的实现状态：岁月核心运行在 0721 车机、通过平板投影、与平板共享所有权；对话来自本地预设台词；内网沙箱内无外部大模型；终端防护由白头疫苗统一管理；音乐页只在用户操作时联网。未实现的一律标“未接入/未提供/未启用”，不虚构功能。1–24 号分类现在都有专属页面，`RenderPlaceholderCategory` 已无入口。
- **电池页**：顶部新增电量大色块，绿色填充宽度等于 `SystemInfo.batteryLevel`；平台无读数时为灰色并显示“平台未提供电量读数”。此读数仅代表运行平台的设备电池，不代表机车蓄电池。

## 四·补 设置项接入剧情与阅读（2026-11-01）

这些设置原先只写入 PlayerPrefs、没有消费者（见 `TABLET_SETTINGS_AUDIT.md` Q1/Q10），现已接入；放在“显示与亮度”（对话与阅读分区）和“更多设置”里，不另开分类。

| 设置 | 键 | 消费者 | 说明 |
| :--- | :--- | :--- | :--- |
| 文字速度 1–10（默认 5） | `TextSpeed` | `TypewriterEffect.TypeText` | 每字间隔 = 0.03s × 5 / 档位，限 0.006–0.15s；默认值保持原速 |
| 对话框透明度 0–100（默认 80） | `DialogAlpha` | `DialogueBox.ApplyDialogAlpha` | 不透明度 = 0.92 × 值 / 80，限 0.15–1；默认值对应原来的 0.92 |
| 自动播放模式 | `AutoPlay` | `VNManager.Awake` | 进入剧情时按设置开启，并刷新 Auto 按钮外观 |
| 自动播放间隔 1–10 秒 | `AutoInterval` | `VNManager.AutoPlayTimer` | 未设置过时沿用原来的 2 秒；设置页默认值同步改为 2 |
| 点击对话框推进 | `ClickAdvance` | `VNManager.AdvanceOnClick` | 关闭后点击不再推进，键盘、Auto 和菜单仍可用 |
| 跳过已读文本 | `SkipRead` | 无 | 剧情系统没有已读记录；开关灰显禁用并说明 |
| 确认对话框 | `ConfirmDialog` | 无 | 剧情内确认弹窗固定显示；开关灰显禁用并说明 |

“对话与阅读”分区带实时预览（`BuildDialoguePreview`）：模拟对话框按当前速度逐字打出示例句、按当前透明度调整底色，点击可重播，换算与剧情一致。**这些设置只验证了编译与平板审查，尚未在剧情场景里实测。**

## 四·再补 Windows / Android 适配（2026-11-01）

- **鼠标**：见上表“触发区”；另有状态栏点按（点左侧时间区开通知、点右侧状态区开控制中心）。
- **安全区**（`GameMainUI.ApplySafeArea`，仅 Android/iOS）：按 `Screen.safeArea` 的顶部与左右缺口加高状态栏并加内边距，应用层与系统面板顶部顺延（`TabletTheme.SafeInsetTop`）。**只处理顶部与左右，未处理底部系统手势条；未在真机验证。**
- **触控目标**：设置里的开关整行可点（原来只有 46×26 的开关本体可点）。
- **返回键**：Android 返回键在 Unity 中即 `Escape`，已接入系统返回，无需改动。
- **新增审查**：`TabletVnSettingsReview.Batch`（Play Mode，验证文字速度 1/5/10 在 1.5 秒内分别打出 9/50/100 字）；HyperOS 审查新增阶段 16–21：高对比度控制中心与通知、减少动效断言（面板完全不透明且无位移）、竖屏（800×1280）通知与控制中心。高对比度与竖屏截图在关闭动画的条件下截取，否则会截到淡入中途。

## 四·又补 账号、整体缩放、全面屏手势与矢量图标（2026-11-01）

**本地小米账号**（无网络，纯叙事）：`UI/Tablet/Account/AccountService.cs`。资料存 PlayerPrefs `XiaomiAccountProfile_v1`，`TabletSetupDone_v1` 记开机向导是否走完；密码加盐 SHA-256 存哈希，规则自定为 8–16 位、字母加数字、仅 ASCII；验证码 6 位、5 分钟有效、最多试 5 次，只在内存里，通过平板通知“短信”送达。**这不是真实小米规则，只是仿真。**

- **开机向导** `TabletSetupWizard`：欢迎、协议、网络、账号、手机号、验证码、密码、昵称与头像、完成。两种流程：`Activation`（可跳过）与 `RegisterOnly`（设置里“登录小米账号”进入）。
- **剧情接入**：`prologue_02_day0.json` 在“拆封平板”处插入 `special` 条目 `TABLET_SETUP`，由 `VNManager.ShowTabletSetup` 在剧情界面上叠向导；已注册则直接跳过；向导打开时点击、键盘、自动播放都不推进剧情。台词是草稿，可改。跳过序章的玩家可在设置里自行注册，桌面首次进入时会发一条登录提示。
- **账号中心**（设置 → 当前账号）：改昵称、选头像色、改密码（验证旧密码）、忘记密码（短信验证码重置）。
- **开发者选项**新增“成就与收藏调试”：成就解锁/重置、13 首音乐收藏解锁。

**整体缩放**：`GameMainUI.BuildDocument` 复制 `TitleScreenPanelSettings`，只对平板生效：`ScaleWithScreenSize`、参考分辨率 1245×700、按高度匹配。1080p 窗口下缩放约 1.55，逻辑画布约 1240×700，比标题界面大约 15%。共享资源未改。向导在剧情界面里仍用剧情自己的缩放。

**全面屏手势导航**：底部导航栏平时收在屏幕下方，应用内容铺满到底，只留一条细提示条。底边短上滑（14–72px）或鼠标顶到底边，导航栏滑出，3 秒后收回；长上滑回主页，上滑后停住开最近应用，左右边缘侧滑返回；触摸屏幕不再弹出导航栏。**手感未在真机验证。**

**沉浸式状态栏**：状态栏是叠在 root 上的覆盖层，应用层顶部不再预留；通用全屏应用窗口自己 `paddingTop = 48 + 安全区`，自建窗口的应用需自行留边。渐变暗影只在桌面出现，应用内透明。

**矢量图标**：不再使用 emoji。`TabletIcons.cs` 有约 70 个 Painter2D 图标，`TabletGlyphMap` 把旧的 emoji 键映射成矢量图标，`BuildSquircleBadge` / `BuildGlyphIcon` 先查表再退回文字。设置侧栏、账号头像、桌面与 Dock、“铁路”文件夹、小组件、超级岛、各 App 的卡片图标都已替换；文字里的 emoji 前缀已删除。

**桌面文件夹**：缩略图是浅色半透明方块加 3 列迷你图标；展开后桌面其余层隐藏，只剩压暗的壁纸，大标题在左上，点标题可重命名。壁纸目前是纯色，没有模糊。

## 五、验证记录

| 项目 | 命令 / 日志 | 结果 |
| :--- | :--- | :--- |
| 系统壳图形审查 | `TabletSystemShellReview.Batch`，`tools/compile/bold_battery2_TabletSystemShellReview.log` | `TABLET_SYSTEM_SHELL_GRAPHIC_REVIEW_OK` |
| HyperOS 15 阶段审查 | `RailwayRenaissance.Editor.TabletHyperOSAestheticReview.Batch`，`tools/compile/bold_battery2_TabletHyperOSAestheticReview.log` | `TABLET_HYPEROS_AESTHETIC_REVIEW_OK`；截图在 `tools/compile/tablet_hyperos_aesthetic_v1/` |
| 设置与控制中心审查 | `TabletSettingsControlReview.Batch`，`tools/compile/bold_battery2_TabletSettingsControlReview.log` | `TABLET_SETTINGS_CONTROL_GRAPHIC_REVIEW_OK` |

审查脚本修正：`TabletHyperOSAestheticReview.Capture` 过去在动作发生的同一帧读取渲染目标，截到的是上一阶段的画面（例如“06 控制中心”里根本没有面板）。现在截图推迟到下一次检查（已等待 20 帧）再读取。2026-10-29 版本归档的 11 张截图因此不可信，本文以重跑后的截图为准。

提交（按时间）：`f08d1953` 主题令牌；`e0d4ef12` 信号灯与黄铜；`5d717db8` 状态栏/通知/控制中心/超级岛布局；`6b08c602` 矢量图标；`1a4cd3ae` 两行磁贴与图标按钮；`12a52396` MiSans；`15a81b7f` 浅色应用顶栏、图标与 Dock；`3d994efa` 状态栏随应用换肤；`2a8caca1` 设置侧栏；`98339c10` 半粗体与电池大色块。

## 六、未验证与待办

- **未验证**：Android 真机（安全区、触屏手势、软键盘）；Windows 触屏手势与 48px 触发区、状态栏点按区的误触；剧情场景里对话框透明度、自动播放、点击推进的实际效果（文字速度已有 Play Mode 审查）。高对比度、减少动效、竖屏的观感已看过审查截图。电池页、我的设备页、账号页、显示页及主题化页面已有审查截图（`tools/compile/tablet_hyperos_aesthetic_v1/09_*`，HyperOS 审查现在依次截 14 个设置页）。
- **待办**：账号中心页面与 Day0 向导尚未在真实剧情里跑过（审查里只走了向导流程和断言）；MiSans 许可确认与署名；主屏更多应用；通知与状态栏、桌面、声音等页按参考图再细化；“岁月 少女”小窗和天气小组件文字仍偏挤；应用打开/切换/返回桌面的动画；主屏小组件文字与“岁月”小窗的文案；在 `TabletHyperOSAestheticReview` 中补通知中心、电池页的截图阶段。
- **已知差异**：参考图状态栏有网速、VPN、蓝牙、Wi-Fi 图标，本终端按事实不显示。
