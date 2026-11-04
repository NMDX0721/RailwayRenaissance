# 随身听 × Folia 对照盘点

日期：2026-11-04。参考仓库 `.references/folia-major`（AGPL，只学行为，不抄代码）。本表依据 Folia 的设置页、面板、命令面板、`img/` 预览图与 docs，以及本仓库 `Assets/Scripts/UI/Tablet/Apps/Music*` 的当前代码；**没有运行过 Folia**，所以 Folia 的动效手感只看了它自带的静态预览图，下面凡涉及"动起来什么样"的判断都是推断。

目标：随身听从"能用"转到"好用"。

## 1. 已经搬过来（含本地化改写）

| Folia | 我们的做法 |
| --- | --- |
| 歌词过滤（制作信息行） | `MusicLyricFilter`，设置页可开关，额外过滤词在歌词页 |
| 封面取色 | `MusicReactiveStageView.AccentFor` + 主题层；设置页可开关或固定强调色 |
| 切歌淡入淡出 | `MusicAudioEffectsEngine` 双轨交叉淡化，设置页可开关 |
| 不可播放曲目找替代 | `Replace` 页，标题必须匹配，歌手只排序 |
| 本地曲目匹配在线歌词和封面 | `Match` 页 |
| 榜单与热搜 | `MusicCharts`（匿名网易云榜单、热搜、联想下拉） |
| 倍速、睡眠定时、歌词时间校准 | 播放设置三级面板（按曲目单独记住偏移） |
| 均衡器（频段推子 + 预设） | `MusicEqualizer`，十段 ±12 dB，7 个预设 |
| 回放增益 | 抽样估算响度，目标 -17 dBFS，压 10 dB / 提 6 dB |
| 主题快速编辑 | 设置 › 外观：封面取色、7 色固定强调色、3 种底色 |
| 可自定义播放栏栏位 | 设置 › 播放栏：倍速 / 音色 / 定时 / 音效，可排序 |
| 点曲名定位到列表 | 点播放栏曲名；封面和歌手名仍开唱片档案 |
| 首次使用教学（Ponder） | 6 步聚光圈教学，帮助页可重看（Folia 的 Ponder 是带时间轴的完整课程，这里只是精简版） |
| 独立设置页 | `RailwayMusicTerminalView.Settings.cs`，五个分页 |
| 减少动效 | 设置页开关，全局生效 |
| 逐字歌词 / 估算逐字 | `MusicLyricEstimate` |
| 本地歌词文件（LRC、增强 LRC、VTT、SRT） | `MusicTimedText` |
| 舞台模式（部分） | Luminous、云阶(Partita)、Tilt、回环(Claddagh)、心象(Cadenza)、清唱(Cappella)、摆线(Pendolo)、Still，共 8 个（Folia 另有 Fume、Lumiere、Monet、Sonnet、Tempera、Diorama 等） |

## 2. 搬了一半或做得比 Folia 浅

| 项 | 差在哪 | 建议 |
| --- | --- | --- |
| **舞台视觉** | Folia 是 Pixi GPU 着色器 + 滤镜 + 3D 镜头（约 2 万行）；我们是 UI Toolkit 元素，只有位移/透明度/缩放。背景几乎是纯色渐变，最能看出差距 | 单独立项：RenderTexture + 着色器背景层、粒子光尘、逐字发光拖尾、镜头推拉（见 §5） |
| 命令面板 | Folia 有 `Ctrl+K` 命令面板：搜命令、搜队列、`/` 语法、最近命令、可固定命令、自定义快捷键 | 先做精简版：搜索框内 `>` 进入命令模式，列出"打开设置 / 切换舞台 / 睡眠定时 / 定位当前歌曲 / 重看教学"，按使用频率排序 |
| 睡眠定时 | Folia 有分钟自定义、播完本曲、命令面板输入 | 加自定义分钟（现在固定 15/30/45/播完本曲） |
| 队列 | Folia 的队列可搜索、可按语法筛选、按匹配度跳转 | 队列页加搜索框 |
| 本地歌词格式 | Folia 还支持 TTML、QRC、YRC、KRC，且格式优先级可设 | 先补 YRC/QRC（网易云、QQ 歌词常用，逐字时间现成），格式优先级设置放到歌词页 |
| 全局歌词偏移 | Folia 有"全局偏移"加"每首偏移"；我们只有每首 | 设置 › 歌词加全局偏移 |
| 播放栏栏位 | Folia 是主/次两个栏位槽，每槽可选任意命令 | 我们只开放四个固定按钮；可扩到"队列/歌词/档案/随机"等按钮也可隐藏 |
| 主题 | Folia 有日夜模式、AI 主题、自定义主题公园 | 日间（浅色）主题要重写整套 USS，另立项；AI 生成不做 |
| 响度 | Folia 有"单曲/专辑"两种模式 | 现在只有单曲；专辑模式需要同专辑响度，价值低 |
| 首页 | Folia 的网格墙（Lattice / Bravais）、卡片衰减、3D 卡片 | 我们是封面磁贴 + 横滑榜单，够用；除非要做"沉浸选曲墙"，否则不搬 |

## 3. 还没研究、值得先看

- `src/components/panelTab/`：封面 / 控制 / 队列 / 账户 / 本地 / 导航 / 在线歌词七个标签的整合方式（我们现在是分散的抽屉和弹层）。
- `FloatingPlayerControls.tsx` 与 `PlayerBottomBarPositioner.tsx`：悬浮播放条的位置记忆、自动隐藏、鼠标闲置隐藏。
- `useThemeRecommendedColors.ts`：主题推荐色算法，可对照我们的 `AccentFor`（现在只取主色）。
- `LyricsTimelineModal` + `GlobalLyricOffsetRuler`：用标尺可视化调歌词偏移，比我们的 ±100 ms 按钮直观。
- `NewFeaturesIntro` / `ReleaseNotesDialog`：新功能引导，可借给"这次更新了什么"。
- `UnavailableReplacementDialog` 已搬；`LocalLibraryEntityPanel`（艺术家 / 专辑实体页）没有搬。
- `songHandover.ts`、`styleTransition/`：切歌、切风格时舞台的过渡动画，是"好用"的关键细节。
- 音频输出设备选择（`AudioOutputDeviceRow`）：Unity 里能做吗要查。

## 4. 不搬（与本游戏无关或不可行）

| Folia 能力 | 理由 |
| --- | --- |
| Automix（beat_this 节拍 + htdemucs 人声分离模型，约 190 MB，外加 Python 运行环境） | 游戏内不打包；模型下载 + 子进程在游戏里不合适。**"下载模型本地分析歌词"并不存在**：这些模型只服务于自动混音。歌词对齐我们在 `tools/music/` 有开发期离线脚本 |
| 歌词接口（127.0.0.1:32109）、OBS 浏览器源、Discord Rich Presence、Now Playing 接入 | 桌面应用的对外集成，游戏里没有对应场景 |
| 模组系统 Folium、模组市场 | 同上 |
| 壁纸模式、遥控窗、Wayland/Hyprland 适配 | 桌面端能力 |
| 多端部署（Vercel、Docker、PWA） | 无关 |
| AI 主题生成（Gemini/OpenAI 密钥） | 需要联网与密钥，不进游戏 |
| Spotify / YouTube Music / 波点（桌面端） | 仓库里已有各自的服务与取舍（Spotify 属不发行的本机测试） |

## 5. 建议的推进顺序

1. **舞台升级**（最大的体感差距）：先做背景着色器层（流体 / 烟雾，随低中高频与封面色起伏），再加粒子光尘和逐字发光。预估要新增 RenderTexture 管线，单独一个分支。
2. **命令面板精简版 + 队列搜索**：把分散的入口收成一个键盘入口。
3. **歌词偏移标尺 + 全局偏移 + YRC/QRC 解析**：歌词体验的"好用"部分。
4. **切歌 / 切风格过渡**（`songHandover`）：对照 Folia 的实际表现再定。
5. **运行 Folia 对照截图**：装依赖并起开发服务需要下载，先征得同意；舞台升级前最好做一次，避免只凭静态图判断。

## 6. 更新（2026-11-04 晚）

### 已完成（上面表格里对应行以此为准）
舞台着色器背景 + 逐字光晕/残影 + 换句推进、命令面板（`Ctrl+K`，命令 + 队列歌曲 + 最近）、歌词时间标尺 + 全局偏移、YRC/QRC 解析、切歌过渡（背景图案缓动 + 歌词滑入）。

### 再扫一遍 Folia 源码后，我们还没做的
| 项 | Folia 位置 | 判断 |
| --- | --- | --- |
| 舞台风格过渡 | `visualizer/styleTransition/` | 切风格现在直接换；值得做，和切歌过渡同一类 |
| 更多舞台风格：Fume、Lumiere、Monet、Sonnet、Tempera、Diorama | `visualizer/*` | 需要着色器/图层；Fume、Lumiere 可复用已有背景着色器，先做这两个 |
| 副歌/和声叠层 | `chorusDetector.ts`、`VisualizerHarmonyOverlay.tsx` | 按歌词行重复频率找副歌，舞台上加强；纯 C# 可做，成本低 |
| 悬浮播放条：位置记忆、闲置自动隐藏、光标隐藏 | `floating-player/`、`usePlayerChromeAutoHide`、`useIdleCursorHide` | 舞台沉浸感，成本中 |
| 队列批量操作与"加入方式"（下一首/末尾/替换） | `queueBatchOperations.ts`、`queueAddBehavior.ts`、`queueReorder.ts` | 好用度高；我们只有基础排序和加入 |
| 私人 FM 模式 | `usePersonalFmModeController.ts`、`fmQueue.ts` | 依赖网易云账号，视在线功能范围而定 |
| 本地库：艺术家/专辑实体页、文件夹忽略、自动扫描、m3u 歌单导入 | `local-library-entity/`、`localLibraryFolderIgnore.ts`、`localPlaylistFileService.ts` | m3u 导入和文件夹忽略便宜；实体页大 |
| 预取下一首 / 已播缓存 | `prefetchService.ts`、`playedTrackCache.ts`、`audioCache.ts` | 切歌更快；在线曲目有用，本地没必要 |
| 播放恢复（出错重试/转码回退） | `playbackRecovery/`、`useTranscodeFallback.ts` | 我们有不可播放替换，但没有自动重试 |
| 歌词导出（含视频导出） | `lyricExport/`、`videoExportFramePipeline.ts` | 视频导出不搬；文本/LRC 导出可做 |
| 自定义歌词字体 | `customLyricsFont.ts` | 游戏内可放字体文件选择，低优先 |
| 歌词分词（ICU 断词） | `useIcuFoliaSegmentation.ts`、`lyricSegmentation.ts` | 我们估算逐字按字数；中文断词能让估算更自然，值得看 |
| 听歌记录/Scrobble、Navidrome | `navidromeScrobble.ts`、`navidromeService.ts` | 我们有 Navidrome 同步引擎；scrobble 不搬 |
| 媒体会话（系统媒体键/锁屏） | `mediaSessionSync.ts` | 桌面端能力，不搬 |
| 主题公园、AI 主题、首页网格墙、3D 轮播 | `ThemePark`、`wall/`、`Carousel3D.tsx` | 已在 §2/§4 |

### 推荐下一批
1. 舞台风格过渡（便宜，补完过渡）。
2. 副歌检测 + 舞台强调（便宜，效果直观）。
3. 队列批量操作与加入方式（好用度）。
4. 中文断词改进估算逐字。
5. 再做 Fume / Lumiere 两个舞台风格。
