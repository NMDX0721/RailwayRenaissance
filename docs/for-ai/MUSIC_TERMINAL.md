# 随身听：铁路音乐终端（2026-10-23）

**2026-10-29 当前状态：** 网易云、QQ、酷狗、波点、Subsonic 与 WebDAV 已加入新版云端入口，真实账号和各自限制见 [渠道能力清单](MUSIC_CHANNEL_CAPABILITIES.md)。原声解锁、用户数据和歌词时间轴沿用原规则；以下早期记录保留历史背景。

本日后续追加：已参考本地 Folia 仓库并 fetch 上游 0.7.21，实现真实本地歌词/字幕、沉浸播放页、逐词时间填充、点击定位、每曲校准记忆、翻译和输入/滚动体验优化。完整对照、格式支持范围和验收见 [Folia 核心体验复核](MUSIC_FOLIA_CORE_REVIEW.md)。

## 当前入口与视觉

实际入口仍为 `GameMainUI.LaunchApp("音乐")` → `TabletMusicAppUI.BuildMusicApp`，由 `RailwayMusicTerminalView` 构建 UI Toolkit 界面，样式源为 `Resources/UI/MusicTerminal.uss`。

暖灰设备面板、黄铜分隔、档案编号与纸质封套构成铁路视觉；按用户追加要求，所有运行时封面停止引用场景截图，改用 9 张新生成的铁路主题唱片插画（内置 imagegen，非手工像素逐点绘制），保存在 `Resources/Music/Covers`。26 首原声按主题映射到站台、铁与灰、云轨、余烬、夜行货、整备、回忆、广播八种封套；本地音乐与无匹配曲目使用私人磁带封套。封面不是每首独占的一套 26 张图片。图像中的景物属于唱片艺术，不新增世界观事实或玩法内容。黑胶仅在按需打开的「唱片档案」中旋转，说明文字明确不是同步歌词。常规操作不添加汽笛或广播音效。Folia 是体验参考，未宣称移植其源码。

所有按钮包含原生矢量图形；播控、曲目操作、队列和关闭采用图形按钮，导航及菜单采用图标加名称。图形由 `MusicTerminalIcons` 使用 Painter2D 绘制，不依赖字体字符或 Emoji。播放/暂停、收藏与模式图形随状态变化，保留 tooltip 和标准 Button 键盘提交。封面通过 `MusicCoverImportSettings` 限定为 512px、Point、无 mipmap、压缩纹理。

## 曲库与播放约定

2026-10-23 滚动栏主题化：音乐终端内所有垂直 ScrollView（曲库、歌单导航、队列、菜单和云端配置）统一使用 12px 暖灰凹槽、细轨与黄铜滑块；悬停提亮，按下/键盘聚焦使用亮黄铜，隐藏默认白色上下箭头与滑块外框。保留 Unity 按内容计算的滑块长度及原生滚轮/拖动行为，样式仅作用于 `.music-terminal`，不影响其他平板 App 或底部进度/音量条。

本轮验证：现有 `RailwayMusicTerminalReview.Batch` 退出码 0、`MUSIC_REVIEW_OK`，无 C# 或 USS 解析错误；已查看更新后的主界面截图，确认默认白色滚动栏被替换。日志：`tools/compile/music_terminal_scrollbars.log`。悬停/按下配色已配置，本轮未单独留存这两种状态截图。

- 导航为原声、本地音乐、收藏、云端、自建歌单；顶部搜索跨原声和本地曲库匹配曲名、作者与分类。作者未知时不编造。
- 原声使用已有 ID 和剧情解锁条件。本地曲目以 `local:` 加含扩展名的小写文件名为标识，避免同名不同格式冲突。导入仍拷贝到 `CustomMusic`，支持 MP3/WAV/OGG。
- 歌单可创建、改名、删除、增删曲目、拖动或按箭头排序；同一歌单内曲目去重。删除歌单不删除音频。
- 队列是「下一首」列表，允许重复，支持前插、追加、移除、清空、拖动和箭头排序。「播放全部」用当前列表中可播放曲目替换队列。
- 手动播放锁定或缺失曲目会提示；连续播放跳过不可用曲目。队列耗尽后按播放模式遍历可用曲库，无可用曲目则暂停。
- 时长来自实际 AudioClip，未知显示 `--:--`。本地曲目在实际解码后得到时长。
- `MusicAudioEffectsEngine` 的活动声道始终指向新曲，旧曲在备用声道淡出；暂停冻结淡入淡出，恢复继续。
- `MusicSessionDriver` 持续驱动音频，`UpdateAudioProgress` 按 `Time.frameCount` 去重，兼容已有 GameMainUI 调用。UI 不再另启音频计时器。关窗继续播放，重开不自动起播或重置进度。

## 状态与迁移

`MusicLibrarySession` 统一持有曲目、收藏、队列和歌单；`MusicLibraryStore` 使用 Unity JsonUtility，保存在 `Application.persistentDataPath/MusicLibrary/library-v1.json`，独立于经营槽位，按本机共享。

版本 1 保存本地文件记录、收藏、队列、歌单、当前曲目、秒进度、音量和播放模式。首次建立曲库时迁移可识别的 `MusicFav_*` 和 `TabletMusic_Vol`，保留旧 PlayerPrefs。每 5 秒、操作完成、窗口关闭、应用暂停或退出时保存。采用临时文件与替换备份 `.bak`；损坏或未知版本文件不覆盖，本次状态仅在内存生效并留日志。

重启只恢复曲目信息和进度，不加载播放；玩家点击播放后才加载并定位到保存位置。缺失的本地文件保留在曲库及歌单中，标为不可用。刷新或重新导入可重新检查文件。

## 云端能力边界（2026-10-23 历史记录；现状见渠道能力清单）

云端入口保留链接/UID 配置，不以配置存在宣称账号已登录或歌单已同步。未实现的在线解析器返回明确失败，不再返回固定虚构曲目。网易云旧查询路径仅接收真实响应中的歌单 ID、名称与数量，不再离线生成歌单或随机数量；B 站扫码未收到真实凭据时失败，不伪造 Cookie。本地真实时间轴歌词/字幕已在后续追加中实现；在线歌词匹配、在线音乐及 Android 导入未纳入验收。

## 验证入口

### 原声同步样板追加

《铁轨沉睡者》现附 35 行逐句自动对齐草稿，补齐原始音频内嵌稿缺失的桥段；显示“待试听校准”，未声称人工听音通过。其余三首已生成离线候选与差异报告，仍以歌词阅读为默认。韩语歌词改用已有更纱字体，空白间奏行紧凑显示；暂停定位按活动 clip 身份选择声道。来源、自动证据、实际样本验证及未完成项见 [音频对齐样板](MUSIC_FOLIA_CORE_REVIEW.md)。

### 2026-10-23 按键比例、滑块与交互追加

- 底部五键改为“模式 / 上一首 / 播放 / 下一首 / 收藏”，主键居中，常规热区 38px、主键 48px；音色与定时独立靠右。紧凑窗口保持组内对称。
- 水平 Slider 滑轨和圆点都以同一容器 50% 高度定位，按各自半高回移；新增实际音量/播放进度黄铜填充。仅作用于 `Slider`，保留垂直滚动条自动计算的滑块长度。
- 按键增加 120ms 颜色/按压反馈，曲目悬停 140ms、弹层进入 180ms。减少动态效果会停用这些过渡、按压缩放和唱片旋转；不新建音频计时器或持续装饰动画。
- 四首歌曲的真实内嵌歌词现已接入阅读页，来源、原始标题差异与同步实现边界见 [歌词追溯](MUSIC_FOLIA_CORE_REVIEW.md)。缺少时间轴时只提供阅读，不显示假逐句定位。
- 验证日志：`tools/compile/music_controls_polish.log`。宽/窄窗口断言滑轨与圆点中心偏差小于 1px，主播放键组内居中；沿用播放、队列、搜索、拖动、字幕导入/定位完整回归。
- 本轮 `MUSIC_CONTROLS_GEOMETRY_OK`（1440/1024）、`MUSIC_OST_SHEETS_OK`、`MUSIC_ORIGINAL_LYRICS_UI_OK` 与 `MUSIC_REVIEW_OK` 已通过；`07-original-lyrics.png` 展示真实《铁轨沉睡者》歌词阅读页，未播放或解锁该歌曲。阅读模式中时间校准禁用，操作提示相应切换。

### 2026-10-23 铁路终端视觉细化

- 导航增加雾峰站搪瓷站牌，顶部使用中英声音档案铭牌；仅作现有世界观的视觉识别，不新增剧情或解锁条件。
- 曲库标题改为暖纸色档案卡，以双线轨道压印收边；深绿设备面板承接导航、列表、抽屉和固定播控。封套保留新绘像素插画，使用纸边与深色装订脊。
- 图形按钮使用细微明暗边表现设备按键；列表操作改为轻量图标，悬停时显示操作底座，键盘焦点保持金色可见。当前播放行、选中导航、主播放键共用黄铜色。
- 字幕页左侧设置收进独立面板，右侧保留阅读空间，当前句使用低亮底与黄铜标记；不增加环境音、闪烁或额外动画。
- 1440×900 与 1024×768 的主界面、队列、详情、字幕截图通过现有实际平板入口生成并检查。完整音乐回归通过（`tools/compile/music_visual_refinement.log`，`MUSIC_REVIEW_OK`）；测试字幕仍为隔离样例，不代表官方歌词。

`RailwayMusicTerminalReview.Batch` 使用独立 `Library/MusicTerminalReview` 曲库与生成的 WAV，经过实际平板入口进行 Play Mode 检查。`BatchRestore` 在另一次 Unity 进程启动后检查恢复与点击续播。产物位于 `tools/compile/music_terminal_review`，原始日志为 `music_terminal_runtime.log` 与 `music_terminal_restart.log`。

2026-10-23 已通过：Unity 批处理编译；实际平板入口、搜索/空结果/剧情锁、收藏、UI 创建/改名/删除歌单、队列、歌单及队列的真实 pointer 拖动、文件导入过滤、真实 WAV 解码非零 PCM、暂停/续播/定位、双声道切曲、缺失文件跳过、单帧计时去重及关窗重开。独立 Unity 进程验证曲库/偏好/进度恢复且不自动播放，并检查真实曲尾单曲循环、队列顺序连播、随机和睡眠暂停。

截图已逐张查看：`01-library.png`、`02-queue.png`、`03-record-details.png` 为 1440×900，`04-compact.png` 为 1024×768。曲目操作及播控为图形按钮，导航与菜单带图标；唱片详情和列表均显示新绘封套。

测试库与音频样本仅写入 `Library/MusicTerminalReview`，不改个人音乐曲库。测试场景为临时内存场景，通过编辑器 API 创建，未编辑 `.unity` YAML。原生 Windows 文件选择弹窗尚未人工点选；共用导入逻辑和解码播放状态已验。批处理 GetOutputData 未观察到输出波形，已验证的是非零解码 PCM 和 AudioSource 播放/时间状态，不宣称硬件听音通过。

### 2026-10-24 歌词表现与滚动条修正

歌词面板提供静读、随行、舞台三个表现，通过图标按钮切换，`lyricMotion`（0/1/2，默认1）写入独立音乐 JSON。静读取消行过渡；随行保留平滑跟随；舞台增加当前句轻微浮起与缩放，有真实逐词时间时才按音频时钟增加词浮起、缩放与金色辉光。暂停冻结、定位重建；减少动态效果优先。逐行草稿不会生成虚构逐词时间。本轮为独立 Unity 实现，未复制 Folia AGPL 源码，也未实现其全部空间布局、旋转或模糊渲染。

水平滑块使用 `music-horizontal-slider` 专用类，避免样式污染 ScrollView 内部竖向 Slider。竖向轨道与胶囊统一以50%居中，取消胶囊28px最小高度：此前26px短轨道出现胶囊越界。验收脚本检查可见滚动条中心与边界，并检查音量/进度居中及舞台模式切换、偏好存储；截图保留在 tools/compile/music_terminal_review。首轮实际平板入口回归通过，最终修订验收以 music_motion_final.log 为准。

最终验证状态：music_motion_scroll.log 的首轮完整回归为 MUSIC_REVIEW_OK；补充舞台切换测试最初因重建行后立即检查旧高亮而失败，现已调整检查顺序。后续最终重跑遇到同项目 NF5KLightingKeysReview.CaptureBatch 并发占用而退出，未干预该进程。因此新增断言的最终完整回归尚待执行，不应标为全部验收通过。

### 2026-10-24 最终验收补记

此前并发占用后的回归已补完，以上“尚待执行”状态现由本条更新。`music_motion_verified.log`：MUSIC_MOTION_SCROLL_OK、MUSIC_REVIEW_OK，退出0；`music_motion_restore.log`：MUSIC_RESTART_OK、MUSIC_MODES_AND_SLEEP_OK、MUSIC_REVIEW_OK，退出0。覆盖实际平板入口、1024×768与1440×900、竖向胶囊首/中/尾居中与边界、三种模式切换、真实时间词浮起、减少动态效果关闭词浮起、独立进程恢复lyricMotion与字幕设置及暂停进度。截图复核还发现舞台缩放导致默认白色横向滚动条出现，现歌词ScrollView明确仅纵向并隐藏横向条，已通过小窗口截图和断言。重新留存05-lyrics、06-lyrics-compact等截图。自动对齐原声仍为待试听草稿，本次界面验收不改变听校状态。

2026-10-24：三首韩语原声已完成small模型独立识别与新候选，异常仍在，继续原文阅读。候选、差异清单和试听片段见tools/music/alignment_review；实际证据及不发布原因见MUSIC_FOLIA_CORE_REVIEW.md最新补记。

2026-10-24纠正：南风完整44句原文保持，撤回依据ASR漏句推断重复/删句的判断。完整原稿重新对齐仍有8项异常，继续阅读模式，未发布不可靠同步时间。

### 2026-10-24 交互与舞台升级及验收范围

工具栏队列/歌词改图标加短标签、队列显示数量与打开状态，搜索可一键清空并返回输入焦点；普通图形按钮40px、播控42px、主播放52px。窄于1280px时队列覆盖曲库，填满应用body且不遮挡固定播控，配外部点击收起遮罩和Esc返回入口焦点；宽屏继续并列。空队列禁用清空。弹窗支持外侧点击收起及关闭后焦点返回，通知不挡鼠标。页面/抽屉增加180ms入场，减少动态效果可停用。

舞台补充远近句缩放层次、真实句时间进度、低亮封套背景及按实际音频时间缓慢位移，暂停冻结，未实现高斯模糊或整套Folia插件。新增专注阅读/显示设置按钮，隐藏设置列扩大正文；逐词仍仅依靠已有真实词时间。截图05/06为隔离测试字幕，09-focused-stage不是官方歌词。

首次整个工作树编译被新出现的Assets/Editor/TabletCalculatorReview.cs缺失TabletCalculatorEngine挡住，未改该其他工作。prepare_interaction_review.py复制原运行时代码、Resources、UI/UI Toolkit主题和项目/包配置到Library/MusicInteractionReviewProject，只包含音乐Editor验收脚本；相关文件SHA256保存在source-snapshot.json。首次快照漏主题依赖已补齐。最终隔离快照日志music_ergonomics_isolated.log：MUSIC_ERGONOMICS_OK/MUSIC_REVIEW_OK；music_ergonomics_restore_isolated.log：MUSIC_RESTART_OK/MUSIC_MODES_AND_SLEEP_OK/MUSIC_REVIEW_OK，均退出0。不可据此宣称原完整工作树通过编译。Unity Search索引初始化还输出一条自身的ArgumentOutOfRangeException，不属于音乐断言，未改搜索数据库。

本轮跨进程检查发现真实退出存档问题：音源销毁后CurrentPosition兼容返回0，Detach保存覆盖已有进度。新增TryGetCurrentPosition明确表示音源/clip是否有效；MusicLibrarySession.Save只在有效音源下刷新位置，其他设置仍保存。退出前后文件均保留37.14479秒，下一进程正常暂停恢复与点击续播。截图及结果归档tools/compile/music_ergonomics_review。

内容校准同步推进：三首均有重叠窗口识别，refine_window_evidence.py仅对唯一且完整逐字匹配、时间不冲突的窗口生成候选修正；重复文本、跨窗口歧义、邻句冲突拒绝。分别接受南风4、星光1、千里马1项自动时间候选，保留全部44/52/49句，未改游戏内资源。更严格的全局区间检查仍发现未解问题；这些不是已听校时间，三首完整准确同步尚未完成。已向用户询问是否存在最终演唱稿/原始工程，尚不能用漏识别推断删词。sleepers35行仍为待听校草稿。

### 2026-10-24 找回提词原稿：来源结论更新

用户指出“AI提词文档”后，进一步追溯Git找到docs/compose/specs/AI配乐提示词.md，最新删除前版本2318294a3fb98cccfe84eb3adc0ac1fe83a6fde0。该文件在3c8da73合并文档时被删除，当前美术总集没有保留四首全文。原始780行文档已逐字恢复到参考资料/资产/AI配乐提示词_历史原稿.md，并在美术总集顶部补链接。此前“未找到文档歌词，只能依据ID3”的判断已过时。

原稿/内嵌稿行数：南风39/44；星光铁轨47/52；千里马49/49；铁轨沉睡者34/33。南风内嵌稿多出一组五行重复段，星光内嵌稿尾段与原稿不同；这次是文档逐句对照的明确差异，不能与先前依据ASR猜删混为一谈。阅读正文改用提词原稿；originalText保留原始ID3全文，embeddedPlainText保留原清理稿，promptText/Revision/Document/SHA256记录追溯来源。重新运行内嵌提取器不会覆盖已确认的原稿。Sleepers34行原稿与35行演唱稿归一化文字完全一致，差别仅标点与They're still here/Waiting at the door拆行；同步草稿继续35行，待听校标记保留。

recover_ost_prompt_source.py执行与重复提取保护、39/47/49/34行数及保留原始字段检查通过。align_ost_lyrics.py新增--variant保存独立来源候选；三首新稿输出small/*.authored-prompt.*，保留此前44/52行失败候选。原稿对齐仍有自动异常（南风4项、星光14项、千里马21项），不自动发布为准确同步；部分为零时长词、误识别及拟声/尾音，仍需校时。原稿来源问题已确定，全部准确时间尚未确定。

实际阅读接线在隔离运行时快照验证：39/47/49行及“提词原稿”来源断言通过，music_prompt_source_runtime.log中MUSIC_REVIEW_OK，退出0。原工程其他Calculator编辑器依赖问题的隔离限制沿用上一节，不宣称完整工作树编译通过。截图与结果保存在tools/compile/music_prompt_source_review。

### 2026-10-24 真正的音频律动背景（替代低亮封套平移）

重新对照本地Folia visualizer/README.md、VisualizerBackdrop.tsx、backgrounds/definition.ts与coverPaletteAnalysis.ts：背景独立于歌词，共享歌曲标识/封面和音频功率/频段输入。游戏新增MusicReactiveStageView为独立Unity实现，未复制AGPL源码，不宣称完整移植Folia渲染模式。

背景由原生UI Toolkit渐变网格光场与细曲线组成，已经替代此前的封套截图式低亮平移。封面在切换时经24×24 GPU取样聚合色相（缓存，不在每帧读像素），歌曲ID决定稳定配色微调与三类光场构图；四首原声另有艺术配色：南风暖金青绿、星光靛蓝紫、千里马锈红黄铜、沉睡者暮蓝琥珀。歌词/控件区域保留低亮衬底与高对比文字。切歌复用背景对象，配色约650ms过渡。

ReadPower读取实际AudioSource输出RMS；输出不可见且当前clip为已解压数据时，从实际timeSamples对应的512帧PCM窗口取能量。不是随机VU、均分歌词或正弦伪节拍。声强和短时能量上升驱动光场大小/亮度，实际音频秒进度驱动缓慢流动。当前不是BPM估计、鼓点识别或完整FFT多频段可视化。背景最多30Hz刷新，不另推音频时钟；暂停冻结音频驱动姿态，减少动态/静读关闭位移与脉冲、保留静态每曲配色。词填充仍仅使用真实词时间，歌词正文校时状态不因此改变。

隔离快照实际平板入口验证：三首真实歌曲加载对应AudioClip（仅测试预览，不改变剧情解锁），检查各自颜色与构图；用真实生成AudioClip的静音/强音段检查RMS差异与PCM回退；真实歌曲播放能量>0、暂停姿态冻结、减少动态归零。music_reactive_stage.log包含MUSIC_REACTIVE_STAGE_OK/MUSIC_REVIEW_OK并退出0。连续16帧实际播放画面编码为reactive-stage-preview.mp4（无声预览，12fps呈现，不是性能基准）。产物tools/compile/music_reactive_stage_review，11/12/13为南风/星光/千里马实际舞台，14为连续画面，附源文件哈希。仍沿用隔离快照验证范围，不等同原完整工作树编译通过或硬件听音验收。

2026-10-24继续校时：四首已完成带上下文的双模型局部复核，三首韩语增加本地分离人声参考。南风剩余6处模型边界争议已导出原曲试听，其他曲目有逐行差异报告。没有将自动一致冒充听校，本轮正式字幕未替换。见MUSIC_FOLIA_CORE_REVIEW.md和tools/music/alignment_review/calibration/SUMMARY.md。

### 2026-10-25 南风39句音素自动校准草稿接入

已引入独立于Whisper的官方torchaudio MMS_FA音素强制对齐，并使用uroman 1.3.1将已确认的原稿转换为对齐用发音符号；显示歌词不改。权重来自https://dl.fbaipublicfiles.com/mms/torchaudio/ctc_alignment_mling_uroman/model.pt，完整1262047414字节、ZIP成员CRC通过、计算SHA256 20ef12963ab4924bef49ac4fc7f58ad5da2ee43b2c11bc8c853c9b90ecdbc680。官方模型为CC-BY-NC 4.0，权重与依赖仅在Library开发缓存，不随Assets发布。官方说明：https://docs.pytorch.org/audio/stable/generated/torchaudio.pipelines.MMS_FA.html。

align_ost_phonemes.py对原曲与分离人声分别提取8秒核心/前后1秒上下文的音素概率，按卷积320样本步长与400样本感受野换算真实时间，保留原采样时间线。音素跨长停顿跳入乐器声时，仅在多数音素已形成紧邻组的情况下局部重对齐；唯一词可用独立高置信ASR窗口约束，不按总时长均分。所有修正前位置、约束来源、后验分数保留。尾句따뜻해另记录标准发音候选따뜨태（原文仍为따뜻해），不伪造词或清除源文字。该方法仍为自动证据，不是人工听校。

南风原曲39句均无本工具的弱音素/倒序标记；38句的行边界在原曲与人声参考之间接近（起点差≤0.25s、终点差≤0.35s），最后一句由独立Whisper人声识别中的整句匹配补充支持，未隐去另一音素结果的0.36s起点/0.8s终点差。publish_phonetic_draft.py要求原稿/音频哈希匹配、39句不变、原曲音素门槛、行边界交叉支持及有效区间，未通过会拒绝写入；发布检查0失败不表示已听校。

已打包Resources/Music/Lyrics/south_wind.txt与south_wind_info.json，仅逐行时间，source明确“音素自动校准草稿（待试听确认）”、humanListeningVerified=false。片尾标题起点133.92s、왠지143.32s、最后一句147.32s；最后清空148.90s。普通句保持到下一句，避免音素结束早于延长元音时提前清空；仅≥5秒长间隙及尾奏添加空行，释放时间取原曲参考支持的较晚结束点，112.26–133.92s保留为长间奏。没有发布增强LRC逐词数据。手动导入字幕仍优先，不覆盖用户文件。专注舞台的标题也保留“待听校”，不能因隐藏设置列而丢失草稿来源。

证据与发布检查按内容哈希归档到alignment_review/releases；info记录报告、检查、LRC的SHA256及发布工具哈希。测试快照资源逐字节相同，证据/检查/LRC哈希校验通过。隔离实际平板入口music_south_calibrated_runtime.log有MUSIC_SOUTH_CALIBRATION_OK、MUSIC_SOUTH_CUE_SEEK_OK、MUSIC_REVIEW_OK并退出0；检查39句、间奏无唱词、原曲暂停点句定位、尾句高亮及专注模式待听校标记。跨进程music_south_calibrated_restore.log有MUSIC_RESTART_OK/MUSIC_REVIEW_OK，退出0。归档截图/源哈希在tools/compile/music_south_calibrated_review，15-south-calibrated-outro.png为真实南风片尾。

其他三首已完成首次MMS双源对齐，但自动发布检查尚有星光11项、千里马7项、沉睡者18项未通过（检查项数不等于句数，也不等同现有草稿全部错误）。新候选未替换星光/千里马阅读稿或沉睡者既有35行草稿。准确听校和逐词校准仍未全部完成，不把南风的自动门槛通过说成全部完成。

上轮最后一组复验/归档被自动审批额度错误阻止，2026-10-25已恢复并补做；不是一次代码测试失败。旧“仅找到内嵌稿/三首均无同步”的陈述按后续追溯和本节更新解释。


2026-10-25 歌词桥段联合校准复核：千里马最新候选仍有3项未通过（句17/42/44），星光仍有弱发音和哼唱时间疑点，均未发布；新增带原曲时间及哈希的疑点试听包。南风39句及沉睡者35行继续标注自动草稿待听校。工具、实测及外部查询授权边界见[MUSIC_FOLIA_CORE_REVIEW](MUSIC_FOLIA_CORE_REVIEW.md)。


2026-10-25 获准仅发送两首ID后完成Suno匿名来源查询：公开歌词与星光47/千里马49行提词原稿一致；v2/v3时间接口均401，未取得时间轴。离线证据工具audit_suno_public_evidence.py及完整限制见docs/for-ai/MUSIC_FOLIA_CORE_REVIEW.md；未改Assets字幕或在线接入。


2026-10-25 本地歌词继续复核：新增弱主歌/副歌整段候选及带来源哈希的人声窗口识别。千里马两版仍3项未通过，分离人声识别失败证据保留，未改正式字幕或放宽发布门槛。详见docs/for-ai/MUSIC_FOLIA_CORE_REVIEW.md。


2026-10-25 本地校准新增边界RMS图（6个争议句，非时间真值）和带语言/设备追溯的独立窗口识别；medium续传中尚无推理结果，CUDA缺库已保留失败证据。见MUSIC_FOLIA_CORE_REVIEW.md。


2026-10-25 medium已完成权重校验和8个原曲窗口识别，归一化人声另3窗亦完成；仍有错词、无识别及时间争议，未发布新字幕。归一化、来源哈希和失败证据见MUSIC_FOLIA_CORE_REVIEW.md；准确同步需听校或原始时轴支持。


2026-10-25 页面内插件已真实导出两首LRC/SRT，audit_suno_timed_exports.py核对：千里马49句含28秒异常长句，星光53条与47行原稿顺序不符且桥段时间坍缩。原始文件与审计在tools/music/alignment_review/suno_exports；未替换正式字幕。详见docs/for-ai/MUSIC_FOLIA_CORE_REVIEW.md。


2026-10-25 refine_suno_context.py新增导出/声学交叉支持的局部候选，星光第21句错误早起点修到105.682秒；严格发布仍9项未通过，千里马仍3项。正式字幕未替换，最新疑点包已更新。见MUSIC_FOLIA_CORE_REVIEW.md。


2026-10-25 边界冲突候选新增星光16句79.622秒、20句92.642秒收敛；严格发布仍7项未通过，千里马仍3项。最新provider.boundaries候选及疑点包保留，正式字幕未替换。见MUSIC_FOLIA_CORE_REVIEW.md。


2026-10-25：本地歌词复核页改为真实波形＋可拖动起止/整体范围、连续试听、可调尾部、部分JSON保存恢复；build_alignment_review.py → review_ui.html，逻辑回归入口check_review_ui.cjs。仅模拟DOM/音频逻辑检查通过，实际浏览器试听/布局待验；未更新正式字幕。GitHub歌曲对齐项目选型及证据限制见docs/for-ai/MUSIC_FOLIA_CORE_REVIEW.md同日章节。

2026-10-25自动校准后续：独立Qwen韩语对齐环境及32窗口实跑完成，8个疑点仅1个四向稳定候选且仍与MMS终点冲突，未更新正式字幕。新增download_qwen_aligner/check_qwen_boundaries/summarize_qwen_evidence；用户不需韩语听校。LyricSmith两曲上传已获授权，但网页对齐面板未响应，尚未上传或导出。证据和限制见MUSIC_FOLIA_CORE_REVIEW.md同日章节。

2026-10-26：audit_lyricsmith_exports.py归档4份原生导出，发现18/32句与SRT重复记录；用已有网页证据恢复47句108词独立候选，未写Assets。refine_suno_context.py新增哈希及原生LRC/SRT约束的lyricsmith来源，实跑0接收/6拒绝，未放宽门槛。恢复稿往返/顺序、工具语法及试听页逻辑验证通过；准确同步仍未全部通过，详见MUSIC_FOLIA_CORE_REVIEW.md同日记录。

2026-10-26无人工听校后续：check_qwen_asr_windows.py完成15次独立无歌词提示原曲识别；audit_blind_consensus.py为星光第4/41句建立逐词起点/行终点交叉支持，第26句仅行级，14/46仍不足。六项负例/分层测试通过；旧发布器/正式字幕未改，详见MUSIC_FOLIA_CORE_REVIEW.md同日记录。

2026-10-26：新增korean_ctc_path/check_korean_ctc及4项路径/断线续传测试；韩文模型下载仍待完整验证，尚无该模型歌声结果。analyze_vocal_activity已产生真实尾段周期性活动诊断，不能当语义歌词边界，未改正式字幕。详见MUSIC_FOLIA_CORE_REVIEW.md同日记录。

2026-10-26 校准后续：韩文CTC权重已校验并实际执行12个窗口，但口白对照也失败，未采用；Qwen1.7B无提示识别8窗口、单句强制对齐8次与MMS编码对照16次仍未解决全部边界。Suno可见星光歌词与47句原稿完全一致。工具新增模型/单句选项、独立报告和流式权重校验；正式歌词及Unity运行时未变。详细证据与未完成范围见 docs/for-ai/MUSIC_FOLIA_CORE_REVIEW.md 的“后续自动校准实测”。

### 2026-10-26 当前完整工程音乐运行验收

旧记录中“完整工程编译受Calculator依赖阻塞”不再代表当前状态。当前主工程直接执行Unity 6000.5.8f1批处理编译，日志正常退出return code 0，无error CS；不是隔离快照。随后在当前工程分别运行RailwayMusicTerminalReview.Batch和BatchRestore，两进程均产生MUSIC_REVIEW_OK。测试采用既有独立音乐测试存储及临时空场景，不保存场景、不使用经营存档。

本轮实际覆盖：真实GameMainUI音乐入口、收藏/歌单/队列交互与拖动、音量/进度滑块居中、歌词滚动条顶中底边界、1440及1024窗口、导入逐词字幕/翻译、点击跳转与偏移、舞台抬升/各曲配色/RMS/暂停冻结/减少动态、原稿阅读、已有两首自动草稿、跨进程暂停恢复与续播、真实曲末连播/单曲/随机和睡眠暂停。重新查看主界面、紧凑歌词和专注舞台截图，未见控件重叠或滑块偏离轨道中心。

归档tools/compile/music_acceptance_20261026包含三份日志、截图/运行结果和本轮关键源码SHA256。此为Editor中的Windows编译/Play验收，未生成或检查独立Windows Player。它不证明星光/千里马歌词时间准确：二者尚未发布同步候选，南风/枕木仍为自动草稿。Suno可见下载对话框仅提供音频/视频/分轨，无直接字幕格式；本轮没有点击Unlock & Download或消耗下载配额。原生下载选择不能替代歌词时间核验。

### 2026-10-26 直接导入逐词JSON，保留真实词尾

MusicTimedText新增.json数组格式：每项word/start/end/line_index，line_index从0连续分组；按原始词顺序保留每个词的显式起止和词间空白，不均分、不插值、不将confidence当准确证明。拒绝缺字段、空数组、错序/重叠、跳行索引、非有限/非正区间；沿用1MB与5000行限制，另限10000词。Windows字幕选择器及本地同名字幕伴随导入支持.json，已有LRC/VTT/SRT优先级不变。导入只是用户选定的字幕数据，不改变内置原稿/解锁规则，也不自动导入当前未校准的千里马候选。

归一化词尾逻辑改为同时取现有词尾、下一词起点和行尾的最小值，避免把JSON已知词尾拉长到下一词。LRC原有按相邻词时间推导的行为保持，JSON可准确表达词间静默；当前不支持任意其他网站JSON封装。界面沿用现有导入字幕入口与本机独立音乐存储。

当前完整工程RailwayMusicTerminalReview.Batch重新编译/运行通过，日志tools/compile/music_json_runtime.log含MUSIC_NATIVE_JSON_OK（真实下载49句97词，14.44词尾与14.46下一词起点分别保留）、MUSIC_JSON_IMPORT_OK（导入/读取与坏文件保护）、MUSIC_REVIEW_OK。新增缺start/line_index、错序重叠、缺行索引、错误封装/空数组负例均由Unity实际解析检查。原有舞台/滚动/暂停/字幕交互回归通过。此轮未再运行第二进程恢复或独立Windows Player；不将原生数据解析成功称为歌词时间校准成功。

## 2026-10-27 本轮范围与验收
用户明确要求优先编译、界面和交互，歌词疑难段不再作为本轮阻塞。保留现有自动草稿、候选及未验证标记，不将其改称准确同步完成。独立工作树为 D:/Unity Project/RailwayRenaissance-MusicApp（codex/musicapp-dev）。首次批处理在包管理解析取消后退出；已重试首次导入，最终编译/Play结果须以新日志 MUSIC_REVIEW_OK 为准。


实际启动复验：三次均未进入编译/Play。重试日志 music-review-20261027-retry.log 与移除本次子进程 HTTP_PROXY/HTTPS_PROXY 后的 music-review-20261027-direct.log 均报告 com.unity.burst 1.8.30 下载 aborted；详情 Logs/upm.log。尚不能将中断归因于代理。未降级依赖、未关闭 TLS 校验、未复制共享 Library；本轮界面截图及运行验收仍待包恢复后执行。


2026-10-27：操作帮助补充现有逐词 JSON 格式提示（word/start/end/line_index），避免导入能力已支持但帮助未同步。工具既有顺序/原生导出审计/共识回归12项通过；Unity验收仍在恢复下载依赖。


## 2026-10-28 收尾验收（取代上文待验证状态）

- 新工作树Unity 6000.5.8f1编译正常退出0，无error CS：music-review-20261027-final-compile.log。
- 实际GameMainUI音乐入口及完整交互通过：music-review-20261027-audio.log含MUSIC_REVIEW_OK。覆盖播放/暂停/跳播、收藏、歌单、队列拖动、文件缺失跳过、开关窗口、统一帧推进、1024/1440布局、滑块居中、字幕跟随及舞台PCM响应/暂停冻结/减少动态。
- 独立Unity进程恢复通过：music-review-20261027-restore.log含MUSIC_RESTART_OK、MUSIC_MODES_AND_SLEEP_OK、MUSIC_REVIEW_OK。恢复暂停和保存位置、音量、偏好、模式及睡眠。
- 已实际查看主界面、唱片详情、紧凑布局/队列、南风/星光舞台截图；未见明显溢出。归档tools/compile/music_acceptance_20261028。
- 补齐26首被Git忽略的原声及meta，修正帮助遗漏逐词JSON格式。不是新增歌词校准；保留草稿和未验证标识。
- 原注册表依赖配置复验仍因Burst下载aborted退出。为保持本机可打开，最终manifest保留file:../Library/burst-1.8.30.tgz，官方同版本包完整性已核对；原manifest/lock备份在Library/MusicAcceptanceDependencyBackup。Library清理后需重新准备此包，当前配置不能直接视为跨机器可复现发布方案；不要直接提交本机路径配置。
- 本轮没有独立Windows Player构建或硬件听音验收。编辑器搜索索引异常仍出现，调用栈属于UnityEditor.Search，不将其说成全工程无异常。
- 自动审批拒绝git restore清理未提交文件，未执行恢复；保留所有相关meta、渲染及ProjectSettings自动改动，未提交或推送。

2026-10-28 体验修正：按钮聚焦时左右方向键交还原生按钮处理，避免误触全局快进/快退；歌词对齐暂不调整。待Unity运行复验。

2026-10-28 复验：music-ux-20261028.log 含 MUSIC_REVIEW_OK，音乐入口回归通过；按钮方向键保护和操作帮助说明已更新。该回归未单独模拟按钮聚焦后方向键事件，仍需实际操作核验。

2026-10-28 Windows体验验收进行中：基于现有启用场景 VN_Test、StationSlice_V1 构建独立测试包 tools/compile/music_windows_review_20261028；使用本机经SHA1验证Burst 1.8.30离线包，不能视作可移植依赖配置。构建与实机结果以该目录 build.log 和实际运行记录为准。焦点按钮方向键仍待单独事件验证，歌词对齐不在本轮范围。


## 2026-10-28 Windows体验验收进展
- Windows Player 按当前启用的 VN_Test、StationSlice_V1 场景构建至 tools/compile/music_windows_review_20261028，build.log 返回码0；使用本机已校验 Burst 1.8.30 tgz 路径，非可移植依赖方案。
- 新增UI Toolkit聚焦按钮和音量滑块方向键事件回归，keyboard-review.log 含 MUSIC_KEYBOARD_FOCUS_OK 和 MUSIC_REVIEW_OK；已有音乐入口/滚动条/播放交互检查一并通过。
- 实际独立程序界面、硬件听音、导入与重启恢复仍未执行；窗口动效和布局尚未完成新一轮精修，不能将构建通过等同整轮验收完成。原声含本地补齐的资源；场景入口与本机依赖按上述构建设置记录。

2026-10-28 体验打磨：歌单和队列的上移/下移图标补充明确悬停提示，搜索无结果提示补全作者这一已有检索字段；待Unity复验。

排序提示和搜索空结果修改的实际平板入口复验通过：tools/compile/music_windows_review_20261028/ux-review.log 包含 MUSIC_KEYBOARD_FOCUS_OK 与 MUSIC_REVIEW_OK。Windows测试包须重新构建以包含此修改；独立运行和硬件听音仍待验。


2026-10-28 v2 Windows包：tools/compile/music_windows_review_20261028_v2/build.log 退出码0；后台实际启动Player进程38404，日志player-startup.log显示Unity及Direct3D初始化且进程响应，随后结束本次测试进程。仅为启动检查，未进入平板或进行听音、导入和重启恢复。使用本机Burst离线依赖；本轮歌单/队列按钮提示及搜索无结果说明已包含。


2026-10-28 体验优化：本地音乐、收藏和自建歌单空状态分别提示导入、心形收藏和更多菜单添加的实际操作；搜索空结果保持跨曲库提示。待Unity实际回归。

空状态修改后的平板入口回归：tools/compile/music_windows_review_20261028_v2/empty-state-review.log 含 MUSIC_KEYBOARD_FOCUS_OK 和 MUSIC_REVIEW_OK；该回归未逐一断言三种空状态文案。


2026-10-28 紧凑截图复核发现深色背景搜索标签对比度偏低，为标签指定暖色前景；待运行截图复验。Windows v3构建返回码0，包含空状态修改，尚不包含此样式变更。

2026-10-28 搜索标签对比复验：contrast-review.log 含 MUSIC_REVIEW_OK；重新查看 tools/compile/music_terminal_review/10-compact-queue.png，搜索标签已由此前偏蓝暗色变为暖色，暗化遮罩下仍可辨认。实际Windows Player音乐入口、硬件听音及重启恢复仍待验证。


新增Editor实际界面断言：空收藏与空歌单显示对应操作提示；本地音乐空态仍需单独检查。Windows v4构建通过，独立Player音乐入口及硬件听音仍待验。

空状态新增断言首跑失败（empty-guidance-review.log：Empty favorites guidance is missing）；隔离存储初次迁移仍可能读取旧偏好，已显式清空隔离收藏后重新运行 empty-guidance-review2.log，结果待确认。原用户收藏未清理。


空收藏/歌单界面断言复跑通过：empty-guidance-review2.log 含 MUSIC_EMPTY_GUIDANCE_OK 与 MUSIC_REVIEW_OK；测试在隔离音乐库清空收藏后执行，不改用户收藏。本地音乐空状态仍未单独断言。Windows v4 包此前构建通过；硬件听音和独立Player音乐入口仍等待实际结果。


本地空状态新增隔离界面断言：临时隐藏测试库内本地条目、检查导入提示，再在finally恢复条目标识；尚待本次运行结果。

本地空状态实际界面验证通过：local-empty-review.log 包含 MUSIC_LOCAL_EMPTY_GUIDANCE_OK 与 MUSIC_REVIEW_OK；测试使用隔离音乐库，临时状态在 finally 恢复。独立 Player 内的音乐入口、听音和重启恢复尚无结果。


2026-10-28 队列体验修正：直接播放缺失或锁定条目前先检查可用性并提示，保留队列项；正常连播跳过逻辑不变。新增缺失文件界面回归，结果待确认。

队列直接播放缺失文件回归通过：missing-queue-review.log 含 MUSIC_MISSING_QUEUE_RETAIN_OK 和 MUSIC_REVIEW_OK；不代表已覆盖文件存在但异步解码失败的情况。


2026-10-28 队列异步播放：直接播放的队列项仅在音频开始后移除；加载失败或取消时保留，加载期间队列变化则保守保留，防止误删重复曲目。常规连播保持原有跳过逻辑。待Unity运行回归。

异步队列常规回归 async-queue-review.log 含 MUSIC_REVIEW_OK；新增真实损坏WAV解码失败及队列保留检查，使用隔离测试目录并在finally恢复文件，待执行结果。

实际损坏WAV测试 decode-failure-review.log 含 MUSIC_BAD_DECODE_QUEUE_RETAIN_OK 和 MUSIC_REVIEW_OK，测试音频已恢复。新增正常WAV成功后消费一条队列的正向检查，待结果。

队列异步正负路径复验：decode-success-review.log 同时含 MUSIC_BAD_DECODE_QUEUE_RETAIN_OK、MUSIC_GOOD_DECODE_QUEUE_CONSUME_OK、MUSIC_REVIEW_OK；实际损坏WAV保留并标为不可用，正常WAV成功启动后消费一项。加载途中队列改变和取消路径尚未单独运行验证。


Windows v6构建返回码0，包含队列异步修复。新增隔离真实WAV的加载中队列修改与立即取消检查，结果待运行。

队列异步竞态回归通过：queue-race-review.log 含 MUSIC_QUEUE_EDIT_DURING_LOAD_OK、MUSIC_QUEUE_CANCEL_LOAD_OK 与 MUSIC_REVIEW_OK。真实WAV加载时修改队列保留条目，取消加载也保留条目；测试使用隔离音乐库。Windows Player内操作、硬件听音及重启恢复仍待实际验收。


Windows v6独立程序启动检查：player-startup.log显示Unity 6000.5.8f1及Direct3D初始化，进程35712响应后结束；仅证明启动，不证明音乐入口、硬件听音或偏好重启。隔离音乐存储接口仅限UNITY_EDITOR，Player中未进行会写入用户音乐偏好的自动测试。


Windows v6资源包核对：构建报告列出了Assets/Resources/bgm中26/26首MP3，包括platform、iron_and_ash和四首带歌词曲目；资源列入构建不等于Player内成功播放或硬件听音。


2026-10-28 交互尺寸：曲库和队列图标按钮命中区由34调整为38像素，图形尺寸保持20像素；紧凑布局及运行截图待复验。

曲库/队列图标命中区复验：target-size-review.log 含 MUSIC_ERGONOMICS_OK、MUSIC_REVIEW_OK；已查看1024×768本地曲库和队列截图，未见明显挤压。实际鼠标操作体验和Windows Player仍需实机验收。


Windows v7 构建返回码0；构建报告中的原声清单及可执行文件SHA256见 tools/compile/music_windows_review_20261028_v7/verification.json。Player平板操作、听音和重启恢复仍未验证。

Player自动验收隔离审计：MusicLibrarySession/LocalMusicFileImporter的测试目录钩子仅在UNITY_EDITOR编译；此外GameMainUI.MigrateUsername在XiaomiAccount缺失时写入并保存PlayerPrefs。因此仅隔离音乐JSON和音频目录不足以保证Player测试不改用户偏好。待采用独立测试应用身份及独立测试目录后验证；未清理、覆盖用户偏好。


2026-10-28：新增开发版专用、命令行显式触发的独立Player验收入口；构建临时使用独立company/product以隔离PlayerPrefs和persistentDataPath，并在finally恢复编辑器配置。检查真实平板入口、搜索、原声状态、歌单、队列与独立进程恢复；不声称硬件听音或常规游戏入口验收。待构建及运行结果。


独立Player首次运行入口/内存状态通过，但跨进程恢复失败；隔离LocalLow目录中File.Replace探针返回Access denied。开发版验收钩子改用测试包内独立review-state目录，应用身份仍独立，常规发布不启用；待重新构建及复验。

独立Windows Player v2：music-isolated-player-build-v2.log含MUSIC_PLAYER_ISOLATED_BUILD_OK、退出码0；构建后PlayerSettings恢复DefaultCompany/RailwayRenaissance。首次Player日志player-review.log含MUSIC_PLAYER_REVIEW_OK，实际调用GameMainUI音乐入口并检查搜索、platform播放状态、歌单去重和队列；状态目录为测试包review-state。第二独立进程player-restore.log含MUSIC_PLAYER_RESTORE_OK，验证暂停、platform、10秒位置、音量0.42及收藏；JSON亦保存歌单和队列。未断言恢复后点击继续、模式切换、原生文件选择、人工鼠标和硬件听音，也未检查普通游戏流程进入平板。首次LocalLow File.Replace拒绝问题不能据此宣称普通环境可复现或已修复。


独立Player验收扩展：第二进程断言模式、队列和歌单，再向真实播放按钮发送导航提交事件，检查从保存位置继续；测试包仍使用独立身份和目录，待构建及运行。

独立Player v3：music-isolated-player-build-v3.log 构建成功；first.log 含 MUSIC_PLAYER_REVIEW_OK。第二进程 restore.log 含 MUSIC_PLAYER_RESUME_OK，断言收藏、音量、模式、队列、歌单、暂停及保存位置，并向真实播放按钮发送导航提交事件，从约10.02秒继续。隔离身份与review-state目录，实际硬件听音、人工鼠标/原生文件选择及完整游戏入口仍待验收。


独立Player验收新增1440×900曲库/详情与1024×768曲库/队列截图、紧凑播控边界及音量滑块居中检查；测试包隔离应用身份与数据，待构建及实际截图核验。

独立Player v4实际音乐入口、1024紧凑播控/音量滑块几何和跨进程按钮恢复播放通过；曲库截图可读。详情/队列截图捕获在入场动画期间，不能作为最终视觉验收；新增实时等待后复拍，待构建与核验。

## 2026-10-28 后续实施计划：Folia 与多渠道

以铁路终端体验、本地音乐与在线来源为主；保留已有原声解锁和用户改动，暂不对齐歌词。先清点Folia和现有工程的渠道实际能力，建立来源标识和统一播放适配；第一阶段网易云采用本机回环服务、真实登录/歌单/搜索/播放及失败反馈；后续逐步处理QQ、酷狗、波点、Spotify、YouTube Music和既有B站、Subsonic、WebDAV、Apple Music、SoundCloud入口。各渠道以接口、授权和实测为准，不把配置或模拟数据标记为已连接。

本地音乐改进导入、重复、元数据/封面、字幕、缺失与损坏文件提示；对照Folia打磨播放器、队列、封面和舞台动效，支持键盘、减少动态及铁路主题。逐项核对上游许可和分发，本机服务仅绑定回环；原声和可播放来源共用播放器。每渠道分别记录连接、登录、曲库、搜索、播放以及失败条件，Windows实际入口、恢复、鼠标及听音分别验收，真实账号验证与自动检查分开。先Windows，Android导入不纳入。

当前基线：新版云端仅保存链接配置，不提供真实登录/同步/在线播放；本机Burst临时依赖路径不可作为可移植发布方案。各渠道能力和本轮结果将在实施中持续更新。

多来源标识基础：MusicSourceId保留旧原声ID和local:小写文件名，新增cloud:来源:曲目ID命名空间以防跨平台冲突；目前在线来源尚未接入共享曲库，待运行回归。

### 网易云本机桥接进展（2026-10-29）

新增 `tools/music/netease-service/bridge.js`：使用子进程标准输入输出调用固定版本 API，未开放 HTTP 端口；支持扫码、登录状态、搜索、歌单和临时播放地址查询。登录 Cookie 仅保存在子进程内，不随协议返回，不执行解锁音源。此桥接尚未与 Unity 相连，真实登录和播放尚未验证，Node 运行时打包及依赖许可审核仍待完成。

音乐检查记录 `MUSIC_SOURCE_ID_OK`、`MUSIC_REVIEW_OK`；日志另有 Unity Editor Search 启动索引的 `ArgumentOutOfRangeException`，来源为编辑器搜索组件，未据此宣称整个编辑器无异常。

Unity 侧新增 `NeteaseLocalBridge` 子进程管理器，消息回调在主线程执行，覆盖服务缺失、通信失败、退出与超时；尚未连接界面或播放器，未完成 Windows 分发。

云端页面新增网易云二维码和登录状态入口；尚未完成真实账号验证、在线歌单和播放。二维码登录会话仅保存在桥接进程内，退出后需要重新登录。

网易云开发接入：云端页面支持扫码、状态查询、搜索、读取账号歌单和展示曲目；已将来源标识与曲目元数据接入现有收藏、歌单、队列和暂停恢复。播放时临时解析地址，并拒绝试听片段及非 HTTPS 地址；地址和登录 Cookie 不写入音乐存档。登录会话仅在进程内，重启需重新扫码。该功能尚需真实账号、音频和 Windows 打包验证；其他云端来源依然仅为配置入口。

Windows 隔离测试构建入口为 `NeteaseMusicReviewBuild.Batch`；`tools/music/netease-service/package-review.ps1` 将固定依赖及本机 Node 复制到测试构建目录，不修改项目 StreamingAssets。依赖包含 LGPL-3.0-only 组件；该测试包不代表可发布，分发前需完成全部传递许可、版权声明及源码义务核验。

Windows Development Player 提供隔离的 `-netease-review` 自动检查入口：使用独立测试存档验证真实匿名搜索、在线音频加载与曲目持久化；不包含账号登录和硬件听音。

本地导入遇到同名文件时比较内容：相同内容跳过，不同内容使用带序号的新文件名；同名字幕跟随对应音频的新文件名，现有文件不会被覆盖。

本地 MP3 在导入或刷新时读取普通 ID3v2.3/v2.4 的曲名和作者；没有支持的标签或标签损坏时回退文件名。当前未解析嵌入封面、WAV/OGG 标签及复杂 ID3 扩展标签。

网易云曲目的歌词舞台在没有手动导入字幕时请求来源提供的 LRC 和可用翻译，按返回时间逐行显示；导入字幕优先，不生成或校准新时间轴。歌词请求尚需在 Unity 界面与真实账号中验证。

云端连接区域整体可滚动，以避免紧凑尺寸下二维码挤占歌单和搜索控件；本地列表显示已读取的作者。网易云歌词与用户导入的翻译可以组合使用。

本地导入同时保留同名 PNG/JPEG 封面，优先显示同名封面并随刷新更新；图片不可用时回退铁路档案封套，原有封面文件不会被覆盖。尚不读取音频内嵌图片。

### 网易云 Windows 隔离验证

2026-10-29，`tools/compile/netease_music_review_v3` 在隔离数据目录运行 `-netease-review`，日志记录 `NETEASE_PLAYER_REVIEW search=True playback=True persisted=True` 和 `NETEASE_PLAYER_REVIEW_OK`：实际匿名搜索、测试曲目的 HTTPS 音频解码播放状态以及云曲目元数据保存通过。此测试不等于硬件听音、扫码登录、私人歌单、版权受限歌曲或正式发布验收；该构建早于随后加入的在线歌词和本地封面/元数据变更。测试包的依赖与 Node 仅供本机检查，完整分发许可仍待审核。

云端搜索结果和扫码过程在播放、收藏或队列状态变化时保留，避免整个云端页面被重建；退出页面时释放二维码图片。

Windows 测试入口增加真实平板云端页面的搜索操作检查；此前 v3 检查仅覆盖通信和播放器，新增界面检查的结果另行记录。

QQ 音乐实验接入：固定 `@yakult-green-tea/qq-music-api` 3.3.3 的 serverless 子入口，经私有子进程实现二维码、搜索、歌单与播放地址请求并复用播放器。实测二维码生成成功，匿名搜索上游返回 2001；扫码登录、私人歌单、受权播放均尚未用真实账号验证。依赖 MIT，包说明将其定位为技术研究和兼容性用途；发行前还需核对平台条件和完整传递依赖。波点候选 `bodian-music-api` 0.1.2 为 AGPL-3.0，不能当作 MIT 依赖直接忽略其分发义务。

酷狗实验桥接使用 `kugoumusicapi` v1.6.0 的编程式接口，不打开网络监听。匿名搜索返回 30 项，扫码图生成成功；匿名播放请求失败，登录、账号歌单及实际播放尚未验证。曲目标识包含音频 hash、歌曲和专辑编号，失败显示提示，不伪造可播放结果。

本地 MP3 的普通 ID3 APIC 可提供嵌入 JPEG/PNG 封面；同名外部图片优先，损坏或不支持时仍使用铁路档案封套。复杂 ID3 标签和其他音频格式内嵌封面尚未实现。

波点实验来源使用 `bodian-music-api` 0.1.2，需 Node 24+。匿名搜索、扫码图和一首曲目的 HTTPS 地址已实测；真实账号、歌单和 Unity 音频播放未验证。账号状态只表示本机会话，并不声称重新向服务验证。地区限制会提示并停止自动跳歌。依赖为 AGPL-3.0，发行前必须完成适用许可和源码义务核对。

Subsonic/Navidrome 实验桥接支持 HTTPS 服务器的账号连接、搜索、歌单与 MP3 转码播放；密码仅在子进程内，本机回环 HTTP 可用于本机服务。模拟服务器验证通过，真实服务器、播放器和转码兼容性尚未验证；不读取旧版明文密码。

WebDAV 实验来源支持 HTTPS 根目录的搜索、直接子目录及 MP3/WAV/OGG 播放，不递归；凭据仅保留在进程内，播放时授权头经私有管道传递，禁止跨根目录和重定向。本机模拟服务验证通过，真实服务器和 Unity 音频播放未验证，不读取旧版明文偏好。

远程音频解码异常会终止加载并提示，防止播放控件一直处于加载状态。

在线曲目不再被当作游戏原声资源路径加载；缺少来源歌词时显示对应来源名称，并保留手动字幕导入。

切换云端来源时，旧来源迟到的状态、连接和退出结果不会覆盖当前来源的界面状态。

波点地区限制返回时播放器进入暂停并提示，避免连播持续重试或保留错误的播放状态。

WebDAV 解析器现固定 5.11.2，官方 npm audit 本次未报告漏洞；网易云依赖链仍有上游无自动修复的高危报告，测试包不得当作正式发行包。

后续将网易云传递依赖 `basic-ftp` 覆盖至 6.2.3，官方 npm audit 当前未报告漏洞，真实搜索、地址解析和扫码初始化已复测；该覆盖跨越原依赖主版本，PAC 代理不在本桥接支持范围，真实账号及发行许可仍待验证。

Windows 多来源测试包 v6 的实际 Player 日志 `tools/compile/netease_music_review_v6/multisource-player.log`：网易云界面搜索、音频播放及曲目存档通过；QQ 扫码图生成通过；酷狗匿名搜索通过；波点匿名搜索与一首音频加载播放通过。账号登录、个人歌单、酷狗播放、私有服务和设备听音未覆盖。该构建早于 WebDAV 集成及网易云传递依赖修复，不能代表最新源码或正式发行验收。

WebDAV 搜索框留空时可浏览根目录的音频，独立按钮显示直接子目录；未连接时提示连接服务器。

统一来源服务的失败提示；QQ 播放解析允许较长超时，超时后说明需要重试，不误称后台已重启。

云端来源按钮随窗口宽度换行并高亮当前来源，便于识别正在操作的账号和服务。

当前在线歌单分页尚未实现完整续读；对于来源明确返回或达到已知上限的结果，界面提示“仅显示部分，更多尚未加载”，不将第一页称为全部。

网易云、QQ、酷狗和波点的歌单及曲目页面支持按来源游标继续加载，重复条目去重；当来源表示仍有内容但没有可用续读游标时，明确提示无法继续读取。实际账号及不同服务器的分页仍待验证。

搜索、歌单切换和退出登录会使旧查询失效；迟到的结果不会覆盖当前列表。

网易云和波点返回可用 HTTPS 封面时，曲目列表与歌词舞台异步读取封面并按封面更新舞台色彩；封面地址只保留在本次内存中，失败时继续显示铁路档案封套。其他来源缺少可靠封面时使用同一回退样式。

波点歌单续读保留来源类型，自建和喜欢歌单使用对应来源参数；真实账号及收藏歌单仍需核对。

波点服务启动时检查 Node 的版本；编辑器可在 PATH 中选择 Node 24 及以上，缺少时提示实际原因，测试包使用独立运行时，不写入本机固定路径。

当 Unity 拒绝启动音频请求时（例如平台连接限制），加载状态会结束并提示；不会把异常留在播放器内部。

Windows Development Player 的隔离检查新增本机 Subsonic 与 WebDAV 服务夹具，尝试从真实 Unity 播放器连接、搜索并加载音频；仅用于测试，不代表真实服务器兼容。

重新生成二维码时立即隐藏旧图，避免扫码旧会话。

酷狗状态查询需要远端资料成功且用户编号和昵称有效，避免把空响应显示为登录成功。

云端页面为 Spotify、YouTube Music、Apple Music、SoundCloud 和 B 站展示尚未接通的原因；历史链接配置保留，已接入的来源不再重复显示旧 UID/链接输入框。

WebDAV 目录校验同时拒绝编码路径分隔符和目录穿越，模拟测试覆盖这一边界。

歌单续读失败时保留“加载更多”按钮，可直接重试；加载期间按钮暂时禁用以避免重复请求。

Windows 多来源测试包 v7 在实际 Player 中记录 `PRIVATE_PLAYER_REVIEW subsonic=True webdav=True`：本机模拟服务的连接、搜索和音频加载播放通过；网易云、QQ 扫码图、酷狗搜索及波点音频检查也通过。测试不代表真实私有服务器、实际账号歌单、受限音源或硬件听音已验证；测试包早于后续界面提示和分页重试改动，仅供本机审阅。

隔离 Windows 测试版支持 `-music-open-review`：直接进入平板音乐界面并保持打开，以便用户进行扫码和手动操作；使用独立测试存档，不自动退出。

Windows 测试包 v8（`tools/compile/netease_music_review_v8`）已包含最新六来源界面、本地封面、分页与路径边界；隔离 Player 记录 `NETEASE_PLAYER_REVIEW_OK`、`QQ_PLAYER_QR True`、`kugouSearch=True`、`bodianSearch=True bodianPlayback=True`、`PRIVATE_PLAYER_REVIEW subsonic=True webdav=True`。其中私有服务使用本机模拟服务器；真实账号、完整分页、实际私人服务与硬件听音仍待验证。测试启动器 `打开随身听测试.cmd` 直接进入平板，使用独立测试存档。

连播扫描队列时跳过缺失或解码失败的曲目，但保留该条目；下一首成功开始后才消费其队列项，异步加载期间的队列修改仍受保护。

自动连播保留缺失队列条目的 Unity 检查记录 `MUSIC_AUTO_QUEUE_RETAIN_OK` 与 `MUSIC_REVIEW_OK`，日志为 `music-queue-retention-review.log`。

Windows 测试包 v9（`tools/compile/netease_music_review_v9`）包含自动连播保留缺失队列条目的修复。实际 Player 检查退出 0：网易云界面搜索/匿名播放/存档、QQ 二维码、酷狗搜索、波点匿名播放，以及本机模拟 Subsonic/WebDAV 的搜索和音频加载均通过。构建日志列出了 26 首项目原声资源。真实账号、个人歌单、真实私人服务器、实际听音和发行许可仍待核验；启动器使用独立测试存档。


### 2026-10-29 网易云扫码显示排查

用户反馈 v8 点击扫码后停留在“正在生成”。本机在沙箱外直接调用 v8 内置 bridge 的 `qr_start` 能返回二维码数据，尚不能证明 UI 已显示，也无法据此判断用户故障原因。已在开发版运行时检查中增加按钮触发、图片可见性及状态的检查点，结果纳入最终通过条件；当前 Unity 批处理启动退出码 1，尚未完成新构建或运行该检查。未读取或记录账号凭据。

2026-10-29 后续：Unity 后台构建已完成，v10 将二维码设为不可收缩的 150×150 区域，显示后滚动到二维码；生成期间禁用重复点击并标明 45 秒超时。Windows Player 实际点击网易云按钮后记录 `NETEASE_QR_UI visible=True size=150x150`；用户现场持续生成的具体根因尚未确定，真实扫码和歌单仍待用户验证。测试不记录二维码或凭据。

2026-10-29 用户复测 v10：二维码已显示，网易云手机端扫码提示“当前设备环境异常”。这是实际账号验收的阻碍，具体原因尚未确定；真实账号和歌单未通过。


### 2026-10-29 网易云设备环境与 Spotify Folia 移植

用户确认二维码已显示，但手机提示设备环境异常。本轮核对并补齐 Folia 的设备身份、匿名会话和扫码错误传播；拒绝结果如实显示，不保证消除服务端风控。用户要求直接参照 Folia 推进 Spotify，改为独立 AGPL 服务保留上游源码及许可证，Unity 接浏览器 PKCE、曲库、搜索、播放与歌词。凭据仅在进程内；原生依赖、真实授权和 Premium 播放逐项验证。此前“本轮不接入 Spotify”决策由本次用户要求更新，发行条件仍单独记录。
