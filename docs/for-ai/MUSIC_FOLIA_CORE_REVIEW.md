# Folia 核心体验对照与随身听打磨（2026-10-23）

## 参考来源

找到现有参考仓库 `.references/folia-major`。工作树 HEAD 为 `d7bce036affb645bb407cff5409a53f0bffeb9f4`（2026-10-18，0.7.20），origin 指向 `https://github.com/chthollyphile/folia-major.git`，未发现单独个人 fork 的远程信息。

本轮通过一次性禁用失效本机代理的 Git 参数，读取并 fetch 上游 main：`5767dd6fbb20287972a10081289a63c75dc1217d`（2026-10-23，0.7.21）。只更新参考库 Git 对象与远程引用，未切换或覆盖旧工作树。参考文件包括：`src/utils/lyrics/parserCore.ts`、`localSongLyrics.ts`、`localLyricFormatOrder.ts`、`lyricOffsetMemory.ts`，以及新提交的 `lyricSeekTime.ts`、歌词版式与底栏动作实现。

参考仓库标注 AGPL-3.0。游戏中新增的是独立 C# / UI Toolkit 实现；没有把 Folia 的 TypeScript、样式或依赖复制进 Assets，也没有把整个播放器称为 Folia 移植版。

## 对照后的实现

| 核心体验 | Unity 随身听行为 |
|---|---|
| 统一歌词时间轴 | 直接使用当前活动 AudioSource 的秒进度，叠加每曲手动偏移；暂停、拖动、切歌共用同一时钟 |
| 真实字幕与歌词 | LRC、增强 LRC、SRT、WebVTT；多时间戳、LRC 文件 offset、同时间戳双语、独立翻译文件 |
| 逐词播放表现 | 仅对文件提供的真实词时间绘制黄铜色横向填充；普通 LRC 按行高亮，不猜造逐词时间 |
| 沉浸播放 | 顶部字幕图标或 L 打开全屏页；封面、曲名、字幕、翻译、字号、减少动态选项与播控保持铁路主题 |
| 手动浏览与回到当前句 | 滚轮或拖动滚动条后暂停跟随，明确出现「跟随当前句」按钮；点击恢复，不抢回滚动位置 |
| 点击字幕定位 | 将字幕时间反算为媒体时间，正值手动偏移表示提前；定位到音频末尾及以后会提示并拒绝，不触发切歌 |
| 每曲校准记忆 | ±100ms 按钮、±10s 上限、一键重置；字幕来源与偏移、字号、翻译和减少动态选项写入独立曲库 JSON |
| 本地字幕发现 | 音频旁同名 `.lrc` → `.vtt` → `.srt`，同名 `.t.lrc` 翻译；导入音频时一起拷贝对应侧车文件；也可在字幕页单独导入 |
| 明确空状态 | 缺失、空文件、坏时间格式、编码和读取错误均有提示；不以剧情短句或原声氛围说明冒充歌词 |
| 播放操作细节 | 上一首优先播放历史，超过 3 秒先回到本曲开头；前进可恢复历史；历史限本次会话 100 项，队列仍优先 |
| 稳定曲库操作 | 收藏/模式变化保留列表、队列和导航滚动位置及可识别的按钮焦点；音量调整不重建曲库 |
| 键盘与帮助 | 音乐界面内空格播放/暂停、左右 ±5s、L 字幕、Ctrl+F 搜索、Esc 关闭；文本框和滑块保留原生输入，聚焦按钮的空格保留按钮语义 |

未实现或未在本轮扩张：在线歌词搜索/匹配、TTML/QRC/YRC/KRC/FIA、运行时通用 ID3 歌词提取、音频预览片段起点、AI 分词、桌面壁纸/远程播放和整套 Folia 可视化插件。不把这些写成已有能力。四首原声的内嵌歌词已通过开发期提取打包，见后文。

## 数据、限制与接线

- `MusicTimedText` 独立解析标准字幕，`MusicLyricsView` 显示与交互；`RailwayMusicTerminalView` 提供入口和仅界面内的快捷键，未改 VN 的全局输入绑定。
- 字幕文件最多 1MB、5000 行。明确导入的字幕优先于侧车文件和内置资源；读取失败不会悄悄替换来源。重新导入失败保留原有可用字幕。
- 自定义文件拷贝到音乐状态目录下 `Lyrics/`，以曲目 ID 的散列命名以避免路径字符冲突；UI 显示原始文件名而非散列。原声资源可放 `Resources/Music/Lyrics/<曲目id>.txt`，内容为带时间标记的字幕。
- JSON 版本保持 1，新增可选 `lyrics` 数组、`lyricFontSize`、`lyricTranslation`、`reduceMotion`。旧状态缺字段时补默认值；每首记录 ID、主字幕/翻译文件名、显示来源名和校准毫秒。
- 活跃句查找使用二分；处理 AudioSource 采样/float 舍入时允许 1ms 边界容差。逐帧只刷新当前句的词填充，换句/定位时刷新其他词状态，不再每帧重建字幕文本。
- 目前没有附送已校准的原声时间轴。四首歌曲附送真实歌词阅读稿；`05-lyrics.png` 与 `06-lyrics-compact.png` 中的同步文字仍属于隔离测试字幕，不是正式歌曲歌词。

## 验证

扩展 `RailwayMusicTerminalReview.Batch` 与 `BatchRestore`，仍经实际平板入口、使用独立曲库。检查 LRC/增强词边界/多时间戳/双语/offset、SRT/VTT 多行与间隔、坏格式及媒体末尾定位拒绝；实际文件导入、点击定位、反算偏移、当前句高亮、字号、手动浏览/恢复跟随、Esc、搜索输入不触发播放、无效导入不覆盖，以及播放历史。重启检查字幕来源、每曲偏移、字号和原有播放状态恢复。

日志：`tools/compile/music_lyrics_runtime.log`、`music_lyrics_restart.log`。截图：`tools/compile/music_terminal_review/05-lyrics.png`（1440×900）与 `06-lyrics-compact.png`（1024×768）。原生文件弹窗点选和硬件听音仍需人工验证；文件导入核心、解码 PCM 与播放时间状态由自动验收覆盖。

本轮结果：运行验收含 `MUSIC_TIMED_TEXT_OK`、`MUSIC_LYRICS_UI_OK`、`MUSIC_REVIEW_OK`；独立进程含 `MUSIC_RESTART_OK`、`MUSIC_MODES_AND_SLEEP_OK`、`MUSIC_REVIEW_OK`，均退出 0。宽屏与紧凑截图已逐张查看，确认当前句、真实时间词填充、翻译、主题开关、来源显示与底栏无重叠。

## 四首歌曲歌词追溯与同步方案（2026-10-23 追加）

《美术资产AI提词》当前 BGM 章节描述纯音乐，未保留四首歌曲歌词正文；不能把这一说明推广到全部原声。实际检查四个 MP3 的 `lyrics-eng` 标签，均找到完整原始歌词（该标签名不代表歌词为英语）。嵌入标题不同，因此以实际音频文件和曲目 ID 绑定，禁止只凭显示标题猜测。

| 曲目 ID | 曲库显示名 | 音频内嵌标题 | 阅读行数 |
|---|---|---|---|
| south_wind | 南风 | 통일 도시락 | 44 |
| starlit_rails | 星光铁轨 | 철길처럼 | 52 |
| chollima_ride | 千里马驰骋新时代 | 천리마 신시대 | 49 |
| sleepers | 铁轨沉睡者 | The Rails Remain | 33 |

`tools/music/extract_ost_lyric_sheets.py` 用 ffprobe 提取到 `Resources/Music/LyricSheets/<id>.json`，保留原始全文和原始标题；阅读正文剔除 `[Verse]` 等段落标签、括号中的演唱/编曲指令与和弦提示，不增写歌词或翻译。运行时无需 ffprobe。`MusicTimedText` 在没有已导入/内置同步资源时读取该稿；`MusicLyricsView` 明示“歌词阅读 · 尚无同步时间”，不生成定位按钮、高亮或跟随。导入真实 LRC 后使用已有同步路径。

第三方实时歌词的两个环节应分开：①检索现成时间轴，例如 [LRCLIB API](https://lrclib.net/docs) 用曲名、作者、专辑与时长匹配（其精确检索要求时长相差不超过约 2 秒）；②原创未收录歌曲，用歌词与**对应版本音频**进行离线强制对齐，再逐首试听修正后导出 LRC/增强 LRC。歌唱对齐不等于普通语音识别，已有[歌唱声学模型研究](https://www.music-ir.org/mirex/abstracts/2018/GSLW1.pdf)说明需处理歌曲特征。此轮不扩展在线服务，不上传音频或安装识别模型；未将文字按总时长均分冒充同步。

上述为阅读稿交付时的状态；后续自动对齐样板见下节。人工听音及逐词时间轴仍未验收。

## 音频对齐样板（2026-10-23 后续）

本机运行 stable-ts 2.19.1 + faster-whisper 1.2.1，使用固定提交、SHA256 校验的 Whisper base 模型。先独立识别唱词，再按文本强制对齐；音频始终留在本机，模型/工具依赖存入 `Library/MusicAlignment`，不进入 Unity 运行依赖。命令与复核页见 [`tools/music/README.md`](../../tools/music/README.md)。

首轮四首均已生成 ASR、原始对齐、逐句候选与异常清单，存于 `tools/music/alignment_review/base/`；它们是候选，不是四首已经校准完成。第一首选《铁轨沉睡者》：原音频 259 秒，内嵌稿 33 行，但约 158–184 秒唱出了原稿缺失的桥段，原稿末部还有与实际演唱不符的回填段。原始强制对齐被拦截，未直接发布。

通过完整音频识别及三个重叠窗口的独立识别，恢复实际桥段并整理为 35 行演唱稿；原始内嵌稿不改。随后以独立识别字符位置约束每句范围，再执行 `align_words`，解决全曲自由对齐抢占间奏的问题。样板每行独立识别字符召回率最低 0.839，句区间均有效；仍有一处零时长词，因此**只打包逐行草稿，不发布逐词填充**。自动证据不等于人工听音，置信度也不是时间误差上限。

- 内置样板：`Resources/Music/Lyrics/sleepers.txt`，LRC 内容；`sleepers_info.json` 保存音频/演唱稿哈希、模型提交与 `humanListeningVerified=false`。字幕来源明确显示“自动对齐草稿（待试听校准）”，不冒充完成校准。
- 长于 1.5 秒的段间空隙插入真实句尾空白边界，尤其桥段前、吉他间奏和尾奏；短句之间保持普通 LRC 呈现。空白边界以紧凑间奏标记显示，计数只统计 35 行唱词。
- 韩语正文使用项目已有 `SarasaMonoSC-Regular.ttf`：实查 zpix 缺少韩文字形，而更纱含这些字符。中文控件保留原像素字体。
- 暂停定位按活动声道的 clip 身份选择目标，避免把 `isPlaying=false` 当作声道失效；真实样本测试先初始化 clip，再暂停定位。
- 本地 `alignment_review/index.html` 提供原音频、逐句试听、ASR 对照与时间/文本编辑；全部勾选听核对且区间有效后才允许导出复核 JSON。页面不修改项目资源。浏览器自动化因插件内部缺失 `browser-service.mjs` 未接通，因此页面交互未通过浏览器运行验收，不宣称可替代人工听音。

Unity 验收使用实际平板入口与独立存储，直接装载真实 `sleepers` AudioClip（不解锁该剧情歌曲），核对桥段在 155 秒后、高亮、暂停点击定位和草稿来源标记。证据日志 `tools/compile/music_ost_alignment.log`；截图 `08-original-sync-draft.png`。`07-original-lyrics.png` 改为三首未发布同步字幕之一《南风》的韩语阅读页。其余三首仍使用原文阅读，不把首轮存在问题的候选放进正式资源。

本轮最终检查：`MUSIC_OST_SYNC_DRAFT_OK`、`MUSIC_REVIEW_OK`；跨进程日志 `music_ost_alignment_restart.log` 中 `MUSIC_RESTART_OK`、`MUSIC_MODES_AND_SLEEP_OK`、`MUSIC_REVIEW_OK` 通过，均退出 0。Python 检查覆盖重复唱词锚点顺序、拒绝找不到音频证据的句子、时间进位、音频哈希及打包文件与候选逐字节一致；复核页脚本通过 Node 语法检查，但不替代浏览器交互验收。`sleepers-bridge-preview.mp3` 是原曲 150–190 秒的试听片段，保存在开发复核目录，不作为游戏的新音轨。

### 2026-10-24 歌词表现与滚动条修正

歌词面板提供静读、随行、舞台三个表现，通过图标按钮切换，`lyricMotion`（0/1/2，默认1）写入独立音乐 JSON。静读取消行过渡；随行保留平滑跟随；舞台增加当前句轻微浮起与缩放，有真实逐词时间时才按音频时钟增加词浮起、缩放与金色辉光。暂停冻结、定位重建；减少动态效果优先。逐行草稿不会生成虚构逐词时间。本轮为独立 Unity 实现，未复制 Folia AGPL 源码，也未实现其全部空间布局、旋转或模糊渲染。

水平滑块使用 `music-horizontal-slider` 专用类，避免样式污染 ScrollView 内部竖向 Slider。竖向轨道与胶囊统一以50%居中，取消胶囊28px最小高度：此前26px短轨道出现胶囊越界。验收脚本检查可见滚动条中心与边界，并检查音量/进度居中及舞台模式切换、偏好存储；截图保留在 tools/compile/music_terminal_review。首轮实际平板入口回归通过，最终修订验收以 music_motion_final.log 为准。

最终验证状态：music_motion_scroll.log 的首轮完整回归为 MUSIC_REVIEW_OK；补充舞台切换测试最初因重建行后立即检查旧高亮而失败，现已调整检查顺序。后续最终重跑遇到同项目 NF5KLightingKeysReview.CaptureBatch 并发占用而退出，未干预该进程。因此新增断言的最终完整回归尚待执行，不应标为全部验收通过。

### 2026-10-24 最终验收补记

此前并发占用后的回归已补完，以上“尚待执行”状态现由本条更新。`music_motion_verified.log`：MUSIC_MOTION_SCROLL_OK、MUSIC_REVIEW_OK，退出0；`music_motion_restore.log`：MUSIC_RESTART_OK、MUSIC_MODES_AND_SLEEP_OK、MUSIC_REVIEW_OK，退出0。覆盖实际平板入口、1024×768与1440×900、竖向胶囊首/中/尾居中与边界、三种模式切换、真实时间词浮起、减少动态效果关闭词浮起、独立进程恢复lyricMotion与字幕设置及暂停进度。截图复核还发现舞台缩放导致默认白色横向滚动条出现，现歌词ScrollView明确仅纵向并隐藏横向条，已通过小窗口截图和断言。重新留存05-lyrics、06-lyrics-compact等截图。自动对齐原声仍为待试听草稿，本次界面验收不改变听校状态。

### 2026-10-24 三首韩语曲重新识别与候选复核

完成 small 模型公开文件续传和完整 SHA256 校验，固定 revision `536b0662742c02347bc0e980a01041f333bce120`；模型仅在 Library，不上传音频。实际运行三首独立 ASR（--asr-only）及原稿逐行对齐，原 base 报告保留，small 候选写 tools/music/alignment_review/small。局部窗口原文平均召回：南风0.581→0.793，星光铁轨0.628→0.742，千里马0.563→0.635；该指标改善不代表时间正确或听校通过。

南风第32–36句被自动对齐挤到103.18–103.38秒。此前据此推断原稿重复段未演唱的判断已撤回：识别漏句不能作为删词依据。按用户纠正保留完整原稿，删除五句的performance-hypothesis仅作为已撤回记录隔离，不作为输入。其余候选问题仍须分段核对。星光铁轨第43–48句全部挤在201.66秒；千里马第27句零时长及多处结尾/词形问题仍未解决。三首继续原文阅读，禁止把small候选当完成版。

compare_ost_asr.py生成含原稿/ASR哈希、字符匹配和时间异常的逐行证据，summarize_alignment_review.py生成SUMMARY.md/summary.json；Python语法检查与真实数据运行通过。复核页重新生成并含small候选，但浏览器连接仍缺内部browser-service.mjs，因此没有浏览器交互验收。已导出南风90–116秒、星光铁轨183–210秒、千里马94–118秒原音片段供人工核对。没有新的运行时代码改动，此轮未重复Unity测试；sleepers仍保留35行待听校自动草稿。

### 2026-10-24 原稿保留与判断纠正

用户指出不存在此前所说的重复问题。撤回“原稿重复段未演唱”的推断，不能把自动ASR未识别当作删词证据。南风完整44行原稿SHA256与此前evidence记录一致；32–36句保留。删五句试验稿隔离到alignment_review/withdrawn，禁止作为对齐输入。align_ost_lyrics.py现在拒绝withdrawn目录输入，并对其他显式演唱稿覆盖记录逐行增删差异（sourceChanges/originalLineCount），避免改词被静默隐藏。

check_ost_windows.py在small模型下分别独立识别75–110、90–125、105–145秒，保存音频哈希、模型提交和绝对时间到small/south_wind.window-check.json。重叠窗口没有解决五句的独立时间定位，不能据此断言缺唱，也不生成五句虚构时间。继续保留原文阅读及待核对状态。Python语法检查、44行/哈希不变检查及三窗口实际识别完成；此轮未改运行时，无需重新Unity验收。

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

律动舞台跨进程补验：music_reactive_stage_restore.log包含MUSIC_RESTART_OK、MUSIC_MODES_AND_SLEEP_OK、MUSIC_REVIEW_OK，退出0。源文件哈希与验收快照一致；无声预览1024×768、16帧。

### 2026-10-24 准确同步继续：局部交叉复核与人声参考

新增recheck_ost_alignment.py：以提词原稿为准，完整保留39/47/49/34句；用base/small两个本地模型，在有前后2句上下文的实际音频片段中重新强制对齐，记录每句两模型起止差。最初不带上下文的南风有16句争议，加入上下文后为6句；说明分段截断可误导模型，不能把“完成对齐”当准确。诊断阈值为起点差>0.3s或终点差>0.4s，另查倒序/重叠/零长度；阈值不是听音认证。当前四首局部模型争议分别6/39、22/47、20/49、17/34；与此前词识别异常数口径不同，不应混比或据此断言现有Sleepers草稿全部错误。正式运行时字幕本轮未替换。

为检查伴奏干扰，使用已安装torchaudio的HDEMUCS_HIGH_MUSDB_PLUS进行三首韩语歌曲本地人声分离。来源为官方https://download.pytorch.org/torchaudio/models/hdemucs_high_trained.pt；完整大小334697255、ZIP成员CRC通过，计算SHA256 a004b2790d73ffeaa535db458a1a79b539dfdbafbccc31f275d07e632ebd7816（没有把计算值称为发布方给定SHA）。音频、权重、依赖仅在Library/MusicAlignment；原曲不改、不上传。separate_ost_vocals.py保持原采样点数、0偏移并记录原曲/分离音频哈希。独立人声识别及对齐为authored-vocals变体，不覆盖原曲ASR或原稿候选。分离也会丢失柔弱尾音，不能默认优于原曲。南风尾奏和识别词形仍有疑点，未据此删改原文。

medium识别模型曾尝试下载，因下载较大而暂停本任务下载进程，保留约327MB分段，未加载、未用于任何结果。人声权重首次直接下载被截断约1MB，加载失败，已通过download_vocal_model.py分段续传完整验证后重试成功，不使用损坏权重。

新增build_calibration_packet.py，将四首逐行模型结果加入现有复核页，并生成calibration/SUMMARY.md和南风6个原曲疑点片段（句2、5、19、31、36、38）。复核页候选选择器现在显示具体来源文件，区分原稿/人声/局部上下文版本；起终差不是ASR字符召回。导出仍需全部人工勾选及有效时间，未伪造听校记录。Python语法、真实模型推理、原稿逐句一致/哈希/采样点数保留检查完成；生成页面脚本通过node --check，浏览器交互仍未重新验收。本轮只改开发工具与复核产物，未改Unity运行时。

### 2026-10-25 南风39句音素自动校准草稿接入

已引入独立于Whisper的官方torchaudio MMS_FA音素强制对齐，并使用uroman 1.3.1将已确认的原稿转换为对齐用发音符号；显示歌词不改。权重来自https://dl.fbaipublicfiles.com/mms/torchaudio/ctc_alignment_mling_uroman/model.pt，完整1262047414字节、ZIP成员CRC通过、计算SHA256 20ef12963ab4924bef49ac4fc7f58ad5da2ee43b2c11bc8c853c9b90ecdbc680。官方模型为CC-BY-NC 4.0，权重与依赖仅在Library开发缓存，不随Assets发布。官方说明：https://docs.pytorch.org/audio/stable/generated/torchaudio.pipelines.MMS_FA.html。

align_ost_phonemes.py对原曲与分离人声分别提取8秒核心/前后1秒上下文的音素概率，按卷积320样本步长与400样本感受野换算真实时间，保留原采样时间线。音素跨长停顿跳入乐器声时，仅在多数音素已形成紧邻组的情况下局部重对齐；唯一词可用独立高置信ASR窗口约束，不按总时长均分。所有修正前位置、约束来源、后验分数保留。尾句따뜻해另记录标准发音候选따뜨태（原文仍为따뜻해），不伪造词或清除源文字。该方法仍为自动证据，不是人工听校。

南风原曲39句均无本工具的弱音素/倒序标记；38句的行边界在原曲与人声参考之间接近（起点差≤0.25s、终点差≤0.35s），最后一句由独立Whisper人声识别中的整句匹配补充支持，未隐去另一音素结果的0.36s起点/0.8s终点差。publish_phonetic_draft.py要求原稿/音频哈希匹配、39句不变、原曲音素门槛、行边界交叉支持及有效区间，未通过会拒绝写入；发布检查0失败不表示已听校。

已打包Resources/Music/Lyrics/south_wind.txt与south_wind_info.json，仅逐行时间，source明确“音素自动校准草稿（待试听确认）”、humanListeningVerified=false。片尾标题起点133.92s、왠지143.32s、最后一句147.32s；最后清空148.90s。普通句保持到下一句，避免音素结束早于延长元音时提前清空；仅≥5秒长间隙及尾奏添加空行，释放时间取原曲参考支持的较晚结束点，112.26–133.92s保留为长间奏。没有发布增强LRC逐词数据。手动导入字幕仍优先，不覆盖用户文件。专注舞台的标题也保留“待听校”，不能因隐藏设置列而丢失草稿来源。

证据与发布检查按内容哈希归档到alignment_review/releases；info记录报告、检查、LRC的SHA256及发布工具哈希。测试快照资源逐字节相同，证据/检查/LRC哈希校验通过。隔离实际平板入口music_south_calibrated_runtime.log有MUSIC_SOUTH_CALIBRATION_OK、MUSIC_SOUTH_CUE_SEEK_OK、MUSIC_REVIEW_OK并退出0；检查39句、间奏无唱词、原曲暂停点句定位、尾句高亮及专注模式待听校标记。跨进程music_south_calibrated_restore.log有MUSIC_RESTART_OK/MUSIC_REVIEW_OK，退出0。归档截图/源哈希在tools/compile/music_south_calibrated_review，15-south-calibrated-outro.png为真实南风片尾。

其他三首已完成首次MMS双源对齐，但自动发布检查尚有星光11项、千里马7项、沉睡者18项未通过（检查项数不等于句数，也不等同现有草稿全部错误）。新候选未替换星光/千里马阅读稿或沉睡者既有35行草稿。准确听校和逐词校准仍未全部完成，不把南风的自动门槛通过说成全部完成。

上轮最后一组复验/归档被自动审批额度错误阻止，2026-10-25已恢复并补做；不是一次代码测试失败。旧“仅找到内嵌稿/三首均无同步”的陈述按后续追溯和本节更新解释。


### 2026-10-25 桥段联合复核与未通过项

本轮仅更新本地校准工具和证据，不替换未通过的运行时字幕。align_ost_phonemes.py 新增独立 spoken/local/sections 候选：发音别名不修改显示原文；局部重对齐必须改善碎裂/弱词质量，同时保持分数下限和相邻顺序；桥段依据提词原稿的段落标签联合对齐，保留全部原句。拒绝低置信度或跨邻句的结果，并记录拒绝原因。孤立아及Mmm等非词汇哼唱单独标记，单帧高分不能证明持续元音起点。

千里马 spoken.local.sections 原曲仍有第17句音素碎裂；第42句原曲/人声边界差0.42/1.62秒，第44句起点差2.22秒。发布检查从首次7项降至3项，但仍拒绝写入。独立small窗口62–76/158–176/186–202秒已实跑：前窗出现错词，中窗识别三次近似口号，尾窗未识别；这些结果未解决原稿两句口号与实际演唱的对应，不据此删改原稿或伪造时间。星光正字法local.sections原曲5/47、人声3/47句仍有标记，AI发音别名没有稳定消除问题；尾奏Mmm仍无可靠时间。沉睡者继续保留既有35行待听校草稿。

publish_phonetic_draft.py 新增显式 --variant，失败检查按候选分别保存，不覆盖其他轮次。南风已发布版本所用发布工具先按SHA256快照至releases，既有发布证据继续可追溯。build_calibration_packet.py 现在使用千里马最新检查，导出星光/千里马未通过句的原曲片段及原曲时间、音频/候选/片段哈希；SUMMARY.md是检查项清单，不是听校认证。复核页重建、Python语法和生成JS的node --check通过；浏览器交互未复验。本轮未改Unity运行时，不重复声称整工作树编译通过。

本地ID3检查未找到SYLT同步歌词帧；GEOB为C2PA来源清单，不能当时间轴。查询原Suno生成记录的动作被自动审批拒绝，因为它会发送本地提取的歌曲ID给外部服务，现有授权不足。已请求仅发送星光/千里马ID的明确许可；答复前不查询这些ID，不上传音频或歌词。本地复核照常进行。南风39句与沉睡者35行仍均为明确标注的自动草稿，尚未取得人工逐句听校通过。


### 2026-10-25 Suno公开来源查询（已获仅发送两首ID的授权）

用户明确同意仅发送《星光铁轨》《千里马》的歌曲ID后，匿名GET两首suno.com/song页面均返回200。公开页面渲染歌词经相同clean规则清理后，星光47行、千里马49行均与追溯恢复的AI配乐提词原稿逐句完全一致；星光公开47行与本地MP3内嵌52行不一致，进一步支持保留原稿来源，但生成输入仍不能直接证明实际演唱内容。

读取公开页面所引用的前端脚本，确认已有同步时间接口GET /api/gen/{clip_id}/aligned_lyrics/v2及v3；对这两首分别匿名GET，四次均HTTP401、正文Unauthorized，未取得原始时轴。没有使用登录凭据、POST触发生成、上传歌词/音频，或修改游戏在线功能。网页公开可读不等于同步接口匿名开放；此前查询授权待定现已解决，当前限制是服务要求登录。没有据此请求更大权限或绕过认证。

原页面、响应及哈希保存在tools/music/alignment_review；audit_suno_public_evidence.py只对已下载文件离线核对，输出suno-public-evidence.json并保留公开生成稿文本。该脚本断言响应类型、检查两首原稿/内嵌对比，实跑通过。原稿和Assets字幕未改；千里马3项、星光弱词/哼唱问题仍需本地校准或有效原始时轴支持，不宣称公开查询解决了准确同步。


### 2026-10-25 本地审阅页校验修正

回到本地校准链路，确认千里马最新检查仍3项未通过，未替换Assets字幕。build_alignment_review.py修复空时间输入被一元加号转成0的问题，改读valueAsNumber，空起止时间不能通过导出门槛；补曲目/起止时间/歌词可访问标签，试听失败显示本地音频访问提示。生成页node --check及合成fixture验证通过：合法0秒起点允许，空起点/空终点及未核对条目拒绝。fixture不是人工听校，也没有生成已听校的正式歌词。Python语法检查通过。

浏览器实际验证未通过：本机127.0.0.1:18765首次超时，错误页reload又被data URL策略阻止，未绕过策略；审阅页试听/下载/布局仍待实际浏览器验收。没有为此开放局域网、修改代理或防火墙。当前正式时轴仍为南风39句及沉睡者35行待听校草稿。


### 2026-10-25 本地剩余句继续复核

align_ost_phonemes.py新增--all-sections，仅在--section-context/--local-context下运行，把存在弱音素的原稿副歌/主歌也作为整段联合CTC候选。独立.allsections文件保留，原稿逐句不变；仍按分数和前后顺序拒绝失败候选，不放宽发布门槛。千里马原词形和发音别名两版均实跑：原曲仍第17句碎裂、人声0句标记；未改善既有3个未通过检查（句17/42/44），两版均被发布器拒绝。原曲第17句67.62–71.48、人声67.62–71.42接近，但原曲词内音素仍有1.30秒间隔；不能据此宣称逐词同步通过。

check_ost_windows.py新增--role vocals，必须使用独立tag、校验分离音频/原曲SHA和0时间偏移，输出中同时记录源音频与实际波形哈希。针对65–77、158–176、186–202、190–206秒四个人声窗口完成small独立识别；首窗和第三窗为空，口号误识别和末窗低概率高风险文字均保留在remaining-boundaries.vocals.window-check.json，不能拿来删原稿、确定长音或补唱词。原曲ASR与其他候选未覆盖。

Python语法、真实双源模型运行、49句源文本保留和拒绝发布已验证；南风/沉睡者正式资源未替换。此轮不含新的Unity运行时改动、人工听校或逐词发布。浏览器扩展只确认安装，原生侧边栏未打开，未取得Suno时间轴。本地审阅页上一轮本机连接问题仍是独立待验项。


### 2026-10-25 独立模型与边界诊断推进（进行中）

check_ost_windows.py增加--language expected/auto/ko/en与--device cpu/cuda；非默认语言必须使用独立tag，输出记录请求语言、检测语言/概率、设备与计算精度，不覆盖既有原曲证据。small在原曲58–80及154–178秒自动检测均为ko（0.837/0.732），仍有错词及三次近似口号；只能排查整段语言选错，不能据此确定实际歌词或时间。CUDA实际推理因缺少cublas64_12.dll失败，留有music_small_gpu_validation.log，无GPU成功声明或系统环境改动。

已恢复此前medium模型分段下载，仍处于下载阶段；只有完整大小和发布方SHA256通过后才可加载，现阶段没有medium识别结果。下载进程与进度以tools/compile/music_medium_download_resume.log为准，不把缓存字节数当推理完成。

build_boundary_diagnostics.py生成6张原曲/分离人声20ms RMS对照图与manifest.json，覆盖千里马17/42/44、星光21/25/46；对原曲、分离音频和候选报告校验来源哈希及0偏移，图中词区间是未听校候选，并非能量估算所得。能量不证明歌词发音，不能把分离人声的乐器泄漏当唱词；人声分离也可能丢失演唱。图在alignment_review/boundary_diagnostics，实际生成成功，Python语法通过。

本轮未替换任何Assets字幕；准确逐词校准仍未完成。


2026-10-25 补充第17句跨方法疑点：small原曲ASR把천리마定位69.74–71.14s、概率0.989，而MMS原曲为67.62–69.58s（分离人声67.62–68.52s）；高ASR概率也不是时间准确率。证据在boundary_diagnostics/chollima-line17-cross-method.json。两个MMS源相近仍不能消除独立ASR冲突，不将第17句直接放行。medium下载仍进行中，尚未加载。


### 2026-10-25 medium与归一化人声的实跑结果

medium已完成下载和加载，先前“仍下载/未加载”是旧阶段状态。Systran/faster-whisper-medium revision 08e178d48790749d25932bbc082711ddcfdfbc4f，model.bin 1527906378字节，下载器与发布方LFS SHA256校验通过，计算SHA256 9b45e1009dcc4ab601eff815b61d80e60ce3fd8c74c1a14f4a282258286b51ae；verification.json存于Library开发缓存。

CPU medium对千里马58–80/158–176/186–206三窗和星光30–43/68–80/98–113/126–140/207–235五窗完成独立识别。千里马仍有错词和三次近似口号，最后长音结果极低概率；星光68–80窗出现与原稿无关的“끝까지 시청해주셔서 감사합니다.”，哼唱没有有效识别。高概率不等于唱词或时间准确，不用这些结果更新正式字幕。原始输出和BOUNDARY_REVIEW.json保留在alignment_review/medium。

NVIDIA官方PyPI库本地安装尝试因网络中断续传及WinError32文件占用失败，日志music_cuda_local_dependencies.log；未宣称GPU成功、未修改系统PATH。识别实测均CPU/int8。

对照PyTorch官方hybrid_demucs_tutorial.py后，separate_ost_vocals.py新增--normalize候选：按单声道参考均值/标准差归一化，分离后恢复幅度；不覆盖原结果。chollima_ride.normalized.wav实际生成，10072440采样点及0偏移、SHA、有限输出校验通过；元数据记录归一化系数与工具哈希。check_ost_windows.py新增--vocals-variant normalized并使用独立输出。normalized人声的medium三窗实跑后，首窗仍错词，口号与长音窗无识别，未解决疑点。官方示例：https://github.com/pytorch/audio/blob/main/examples/tutorials/hybrid_demucs_tutorial.py。

本地file审阅尝试被浏览器协议策略明确拒绝，仅允许http/https，未绕过策略或换用其他控制机制；此前本机HTTP也未验收通过。文件语法检查不能替代试听验收。当前千里马17/42/44和星光弱词/哼唱仍缺少可信时轴，南风39句及沉睡者35行仍待听校，增强逐词同步未发布。需要针对争议片段的人工听校记录或有效原始同步数据才能验证“准确同步”，不能再把工具运行成功或模型一致当完成。


### 2026-10-25 恢复后的独立导出路径核对

源文件与medium失败证据仍未变，当前没有新的准确时间戳。补查Chrome商店另一扩展Suno Lyric Downloader（hhplbhnaldbldkgfkcfjklfneggokijm）：发布者说明在歌曲封面直接增加LRC/SRT按钮，避免原生Side Panel依赖，且只导出已有同步数据、不下载音频。来源https://zhanghe.dev/products/suno-lyric-downloader。此为发布者声明，未实测数据读取成功；官网旧版号与商店当前信息不同，以实际安装界面为准。

已在内置浏览器打开商店页并点击添加，状态仍正在处理安装，未当成功。鉴于新扩展会获得Suno页面/会话读取能力，已向用户明确询问仅读取两首歌词的授权；答复及原生权限确认前，不运行读取。截图tools/compile/suno-inline-export-install.png。此次是新的可行路径核对，不表示本地准确同步已解决。


2026-10-25 用户已明确允许Suno Lyric Downloader读取这两首歌词并导出；授权待定已解决。商店仍显示安装处理中，未取得安装成功或原生权限确认；授权本身不能代替实际安装状态，也未读取到同步时轴。


### 2026-10-25 页面内导出已成功，校验发现源时轴缺陷

用户完成安装后，Suno Lyric Downloader实际在歌曲封面出现下载LRC/SRT按钮。通过已授权的两首歌曲页点击按钮，真实下载千里马与星光各LRC/SRT共4文件，原文件保留在用户下载目录，工作副本归档tools/music/alignment_review/suno_exports；没有下载音频。tools/compile/suno-inline-export-success.png为按钮生效证据。先前“待安装/无导出”是旧阶段状态，现已解决；登录已核实nmdx，不应继续要求用户重复登录。

audit_suno_timed_exports.py逐条解析SRT、按既有规则移除舞台说明、对照LRC起点及原稿顺序，并与实际本地音频时长/双源MMS比较。原曲、歌词源、导出及MMS报告SHA保留。千里马清理后49条，原稿顺序一致，LRC/SRT起点一致；句44导出170.903–198.909秒，跨度28.006秒，较MMS原曲起点早20.619秒；句43导出164.441秒而MMS约170.862秒。原稿17句导出67.739秒，与MMS67.622秒接近，支持起点而不能确认词内或结束时间。

星光清理后53条，对应原稿47行顺序不一致；5条桥段句均挤在176.568–176.588秒，最终副歌第一句还共用该起点，不能当真实同步。共有7条区间检查标记，且存在6句共用起点的一组。两个格式一致只证明导出一致，不能证明Suno对齐准确。provider/local原曲起点差>0.5秒的句在审计中列出，不自动选任何一方为真值。

归档包含原始LRC/SRT、audit.json、SUMMARY.md，以及保留真实导出条数的suno-export.review.json；未把星光53条强行删成47条，也未伪造逐词标签。审阅页新增这些来源用于人工对比；修正非ASR来源显示NaN一致率的问题。解析器实跑、Python语法和生成JS的node --check通过；未宣称浏览器试听验收或人工听校已通过。正式Assets字幕保持不变。当前需要解决的是提供方时轴与实际演唱的差异，而非登录或插件安装。


### 2026-10-25 导出约束与声学证据融合候选

针对星光导出时间坍缩，另跑medium原曲164–194/174–204两窗：均识别出“인간은 이걸 위로라고 부르죠”（约185.3秒），small完整原曲也识别桥段其余内容。因此不能把导出20ms句解释为桥段未演唱，更不能删除这5句；假设需由实际音频证据检验。

refine_suno_context.py读取带SHA校验的导出审计及现有双源MMS缓存，只在导出文本与原稿同句、LRC/SRT一致、区间0.15–15秒、无重复起点且导出起点与至少一路既有声学结果差≤0.5秒时作为局部对齐窗口。窗口再受前后句边界约束，必须减少碎裂/弱词且保持posterior≥max(0.15,旧值×0.8)，最终无标记才接收；其余拒绝理由和分数保存。不是直接采用Suno导出，更没有修改源歌词或减少检查门槛。

实跑仅接受星光原曲第21句：从100.222秒的跨间奏错误起点改到105.682–108.722秒，消除音素碎裂；导出起点105.718、人声参考105.702秒相近。完整原稿47句保留。第14句候选虽然消除碎裂，分数保留门槛未通过，仍拒绝；哼唱、未获交叉支持的起点及坍缩桥段不采用。千里马无新增接收。新.provider报告、parent/audit/工具哈希和原结果均保留。

严格发布器实跑仍拒绝两首：星光9项、千里马3项未通过（检查项不是句数）。build_calibration_packet.py更新到最新provider候选与疑点片段，审阅页同步加入候选。Python语法、实际CTC运行、原稿保持及生成JS node --check通过。未新增Unity运行时改动、未替换Assets字幕、没有人工听校通过声明。


### 2026-10-25 边界冲突候选继续收敛

refine_suno_context.py新增显式--include-boundary-conflicts，除原音素标记外，也检查双源起点差>0.25秒或终点差>0.35秒的句；使用独立.provider.boundaries输出，继续保留相同的导出真实性/区间/起点交叉支持、前后句限制、分数改善与置信度门槛。未修改发布阈值。

星光原曲第16句从79.322改到79.622秒，与人声79.622秒一致，分数0.382→0.403；第20句从92.102改到92.642秒，与人声92.662秒接近，分数0.657→0.659。第21句上一轮修正105.682秒保留。千里马第42句只改善音素分数，没有解决双源边界差。严格发布器实际运行：星光从9项降至7项，千里马仍3项，均拒绝写Assets。

星光剩余检查涉及原稿句4/14/26/41/46（7项），千里马仍17/42/44。对small多词连续短语另做只读核对：完整文字匹配也仍有时间冲突或首词概率不足，未降低概率/时间门槛来强行放行。medium桥段两窗的首词概率约0.705/0.654，模型一致也不能替代听校。build_calibration_packet.py和审阅页切到最新候选；实跑、Python语法通过，源歌词与正式字幕保持。


### 2026-10-25 波形试听与歌曲时间轴项目调研

`build_alignment_review.py` 改用独立 `review_ui.html` 模板；从项目四首真实 MP3 解码 8 kHz 单声道、20 ms min/max 包络，按音频 SHA-256 缓存于 Library/MusicAlignment/review_waveforms，再内嵌到离线页面。波形只表示振幅，不是人声/歌词边界证明。支持播放游标、局部缩放、起止把手及整体平移、方向键微调、播放位置设边界、全曲定位、变速与循环。默认连续播放，片段自动暂停需主动勾选；句前1.5秒、句后3秒可调，增加试听尾部不改变歌词结束时间。

校时改动180 ms去抖自动保存；JSON可随时导出部分进度并恢复。音频哈希、候选稳定reviewKey、曲目与行号检查避免跨候选覆盖；完整已核对稿仍需全部确认及合法不重叠区间。修改时间/歌词撤销该句确认。备用本地音频读取 SHA-256，匹配后才替换；校验失败阻止完整复核导出。全部处理在本机，不上传歌曲。

实跑：四曲波形与HTML生成成功；`node tools/music/check_review_ui.cjs` 输出 REVIEW_UI_LOGIC_OK，覆盖连续/定界/循环播放逻辑、整体拖动时长保持、编辑撤销确认、自动保存恢复、错误导入拒绝、空时间、音频不符导出拦截、部分备份。Python编译检查通过。该测试模拟DOM/音频/Canvas，不能代替实际浏览器布局与声音验收；本轮浏览器工具未发现可检查的标签，因此未宣称视觉/实际听音通过。没有修改正式歌词或通过人工听校。

GitHub官方仓库调研（2026-10-25读取，尚未安装或对本项目歌曲实测）：
- [LyricsSync](https://github.com/elecpure2/LyricsSync)：Windows本地逐字/逐词校时、波形编辑和锚点工作流；README说明使用BS-RoFormer、MMS/uroman、Beat This。其MMS与现有方案同类，不能据此保证解决现有难句；节拍也不能替代唱词时间。优先参考人工锚点与局部重对齐交互。
- [Vilm Lyrics Aligner](https://github.com/banjuman/vilm-lyrics-aligner)：针对韩英混唱、保留输入原词、输出行级SRT。[架构文档](https://github.com/banjuman/vilm-lyrics-aligner/blob/main/docs/ARCHITECTURE.md)说明Demucs+Whisper small/stable-ts、局部窗口和人声活动修正；与现有路径有重叠，不是已验证的逐字替代品。
- [SOFA](https://github.com/qiuqiao/SOFA)：专门针对歌声的音素强制对齐，需匹配的模型权重、字典/G2P；默认字典是opencpop-extension。[G2P说明](https://github.com/qiuqiao/SOFA/blob/main/modules/g2p/readme_g2p.md)允许自定义映射，但韩语适配、权重覆盖和许可尚未核验，不能直接套用中文模型宣称解决韩语。

三者仅完成官方说明/部分架构文档核对，不等于源代码全审计。Git克隆被现有127.0.0.1代理连接拒绝；未改系统代理或安装器环境。下一步模型实验应先核验实际权重/语言支持，再仅针对现存8个疑点句比较，保留原稿及自动草稿标签。


### 2026-10-25 全自动校准路线与在线对齐实测

用户明确不熟悉韩语，要求不依赖人工听校；新的后续路线以本地模型交叉验证为主，不把手动试听作为用户必须完成的步骤。历史自动草稿与未确认标识继续保留，不能将模型一致改称人工确认。

新增 download_qwen_aligner.py、check_qwen_boundaries.py、summarize_qwen_evidence.py。独立Python3.11环境在Library/MusicAlignment/qwen-env；qwen-asr固定0.0.6，官方依赖transformers4.57.6，CPU推理。PyPI直连下载多次截断，最终清华PyPI镜像安装成功；未关闭TLS验证、未改系统代理/全局Python。Qwen/Qwen3-ForcedAligner-0.6B固定修订c7cbfc2048c462b0d63a45797104fc9db3ad62b7，模型1,835,544,544字节。官方HuggingFace下载较慢，改由官方ModelScope续传，最终按固定HF LFS SHA-256及小文件Git blob ID完整复核，verification.json留在Library。推理设置HF_HUB_OFFLINE/TRANSFORMERS_OFFLINE，没有向模型服务发送歌曲。

官方支持韩语；ASR说明包含歌声，但ForcedAligner本身主要面向语音，不能混称保证歌声精度。读取官方推理源码发现fix_timestamp会修补倒序时间；复核器保留raw/fixed与所有修补索引，端点被修补不计入稳定证据。每个疑点带前后原稿句，分别运行原曲/已验证同时间基准的人声、1秒/3秒上下文；四份结果及哈希、输入正规化与原稿映射保留。100→백仅作模型发音输入，保存原文不改。

实际完成8个难句共32个窗口推理。qwen/automatic-evidence.json显示星光5句中0句四向稳定，千里马3句中仅17句四向稳定，候选67.422–73.342秒，起止极差均0.24秒；该终点与旧MMS约71.42秒仍冲突，因此仍是需跨模型校验的候选，不发布。其余存在窗口漂移/时间修补，均未写Assets。Python语法、模型导入与实际CPU推理通过；没有宣称准确率、人工校准或逐字发布完成。

网站官方说明核对：LyricSmith明确列出韩语、原歌词对齐、免费无需账号及LRC/逐词TTML/SRT导出；LyriSync提供音频+原词逐词对齐和Enhanced LRC、一首≤5分钟试用，但韩语未核验。用户另外授权仅向LyricSmith上传星光/千里马MP3与原词。实际用内置浏览器打开LyricSmith，点击按钮和页尾原歌词入口、重载#with-lyrics后仍无textarea/dialog，未成功上传。online_input保存两份原词TXT与音频/歌词SHA-256清单，uploadCompleted=false；不将已授权/已打开当作已上传、已对齐或已导出。授权不扩展到其他在线站点。

来源：[Qwen官方代码](https://github.com/QwenLM/Qwen3-ASR)、[固定模型](https://huggingface.co/Qwen/Qwen3-ForcedAligner-0.6B)、[官方时间修补实现](https://github.com/QwenLM/Qwen3-ASR/blob/main/qwen_asr/inference/qwen3_forced_aligner.py)、[LyricSmith](https://www.lyricsmith.dev/en)、[LyriSync](https://lyrisync.com/)。


### 2026-10-25 LyricSmith首次实际上传成功

重开页面后等待交互就绪，韩语语言选择和原词面板正常；先前“面板无法打开”只是当时的观测，不再作为持续故障结论。用户在同一活动工具轮次选中starlit_rails.mp3后，按钮启用；Codex提交Start alignment，页面从Preparing upload到Aligning，进入Ready 47 lines编辑器。47句原稿保持，音频播放器显示253.359979秒。仅星光已上传和生成，千里马此时仍未上传；此前“uploadCompleted=false”保留整组未完成并新增逐曲权威状态。

Save已点击，LRC/SRT/Enhanced导出也已点击，但常用下载目录和用户端均未见下载提示，不能宣称获得原生导出文件。用户随后提出亲自下载。为避免结果丢失，通过已显示DOM逐句读取47个起点，进入网站Word by word预览后依次定位47句，保存108个实际渲染词的CSS --s/--d（起点/动画时长）。仅DOM可见数据，无隐藏状态、会话提取或网络请求绕过；visible-lines/visible-words.json存于alignment_review/lyricsmith。此备份不是原生LRC/SRT，也不能把动画时长当作声学终点。

新增audit_lyricsmith_ui.py，实跑LYRICSMITH_UI_AUDIT_OK：原文/词覆盖完整、47句108词；与最新本地原曲/人声结果起点均差>0.25秒的句为1/2/16/25/29/45。全部标明rendered_duration_not_native_export，不作为正式逐词发布依据。其他起点接近不等于整首已校准，网站后端未核验，不声称独立模型。真实编辑器截图tools/compile/lyricsmith-starlit-aligning.png实际已为Ready结果页（文件名沿用准备阶段），尚未改Assets正式字幕。


### 2026-10-25 内置浏览器导出“已停止”排查

用户亲自尝试LyricSmith导出后明确报告内置浏览器“下载已停止”。此时星光网站结果已Ready 47句；不能把文件传输中断说成歌词分析失败。既有常用下载目录未见新增LRC/SRT。只读查看当天Codex应用日志未取得本次字幕下载的具体取消事件；日志中的其他运行时下载ERR_CONNECTION_CLOSED与本次字幕导出没有因果证据，未拿来归因。没有改代理、TLS、下载权限、浏览器资料或重启应用。

按用户要求上网检索，官方Browser文档确认内置浏览器有独立配置、默认下载到系统Downloads，可在Settings > Browser选择下载目录/询问保存位置；这只能指导设置核查，不解释本次Stopped。GitHub插件读取openai/codex #53500（状态open），报告高度类似：Windows被agent操作过的标签出现Stopped，随后同页手动与刷新仍失败，未被agent操作的新标签手动成功；报告者明确未确定哪个组件取消下载。因此仅作为同症状报告与复测依据，未宣称本机根因已证实或厂商已修复。

建议对照：用户手动用Edge打开已保存的LyricSmith星光结果页导出Enhanced LRC/SRT；或在用户手动新建且未由agent操作的内置浏览器标签执行导出。后者测试时不接管新标签，避免影响对照。此机械下载步骤不要求用户进行韩语听校。千里马49句的第二页已填原稿并选韩语，但音频尚未选择/上传，不能报作两曲完成。

来源：[官方Browser说明](https://learn.chatgpt.com/docs/browser)、[同症状公开问题53500](https://github.com/openai/codex/issues/53500)。


### 2026-10-26 四份原生导出审计、完整候选恢复

用户提供Edge目录下starlit_rails.lrc、starlit_rails (1).lrc、starlit_rails.srt、starlit_rails (1).srt。audit_lyricsmith_exports.py逐字节归档至alignment_review/lyricsmith/native并记录SHA-256，原文件不修改。实际LRC分别18/32句；SRT分别130条（32条不同时间+文字，98条精确重复）和43条（18条不同，25条精确重复）。这些是导出记录问题，不能据此宣称歌曲或用户原稿重复。

四文件合计只覆盖47句中的32个演唱位置；第24–38句共15句不在原生导出中。按文字与时间双重匹配，不因相同副歌文字就全局去重或套用第一次副歌的时间。原生增强LRC逐词与SRT行边界、此前逐句保存的DOM词起点/时长在支持的32句中吻合（允许0.011秒的导出量化差）。另外15句继续明确记录为仅网页渲染证据，不伪称原生导出。

基于已归档网页数据恢复starlit_rails.recovered-candidate.lrc/.srt与.review.json：47句108词，原文保持，时间不插值、不均分。词终点沿用实际观测的页面时间数据，未被原生文件支持的词终点继续带word_endpoints_only_observed_in_rendered_ui标记。候选输出在工具目录而非Assets，humanListeningVerified=false、published=false。native-audit.json记录每句原生支持文件与缺失/本地起点冲突。

refine_suno_context.py新增--provider lyricsmith（默认Suno不变），从最新既有provider.boundaries起步；只采用经哈希核对、增强LRC词数据及SRT共同支持的原生区间，使用原始SRT起止而非网页近似显示值；不采用15句恢复数据、不放宽原声/人声起点支持、质量改善/分数保留/非哼唱限制。使用原有MMS声学缓存实跑，0项接收、6项拒绝；独立新报告starlit_rails.local.sections.provider.boundaries.lyricsmith.json保留诊断，没有改变既有候选或正式字幕。

实际验证：LYRICSMITH_NATIVE_AUDIT_OK、LYRICSMITH_RECOVERY_ROUNDTRIP_OK（47源句/108词、LRC与SRT起止一致、句间无重叠）、Python语法检查及REVIEW_UI_LOGIC_OK通过。build_alignment_review.py重新生成含恢复候选的波形页。未改Unity运行时代码，本轮没有新增Unity运行验收，也没有宣称听校或最终精确同步完成。用户要求无需韩语人工校准，当前流程不把人工听校作为必须提交的工作。


### 2026-10-26 独立无提示歌声识别与分层自动证据

本轮由继续目标触发；上一轮仅解释状态，归类无进展。本轮新增真实独立ASR，而非继续复述未完成状态。download_qwen_aligner.py扩展--model asr，固定Qwen/Qwen3-ASR-0.6B修订5eb144179a02acc5e5ba31e748d22b0cf3e303b0、权重1,876,091,704字节。官方ModelScope下载后按固定HF权重SHA256与小文件Git blob完整校验；环境沿用Library/MusicAlignment/qwen-env，不上传歌曲、不将模型放入Assets。

check_qwen_asr_windows.py使用离线原曲PCM、language=Korean、context为空、不给歌词提示、max_new_tokens=128，不请求强制对齐时间戳。报告明确lyricPromptProvided=false、wordTimestampsProvided=false，并保留模型/音频/工具哈希。--tag支持独立诊断文件，避免覆盖基准。实际完成星光8窗口、千里马5窗口、星光第14句扩窗2次，共15次CPU识别，所有进程实际退出0。

独立识别结果：星光35–40.5s完整识别原稿第4句；184.4–189.8s完整识别第41句，181.5–184.5s只识别上一句89%内容，支持排除旧183.342秒起点；132–138.5s完整识别第26句。第14句不同窗口均识别“다시 밝아”，比原稿“다시 밝아와”少末音节，不能据此擅改原稿或称完整匹配。哼唱窗口213–220与220–228.8只输出非词汇元音，不提供可靠逐词边界。千里马多窗仍有明显识别差异，不按近似文本强行通过。

audit_blind_consensus.py新增独立分层证据审计，不替换旧发布器、不改旧置信度阈值、不写Assets。它要求确切的无提示原曲ASR文字、干净且达到既有绝对门槛的人声音素、网站/人声行边界一致、原曲/人声一致或被前句对照片段排除的旧起点；逐词范围另须原生增强LRC支持、逐词文字及起点<=0.25秒一致。保留选择的audioRole=vocals及所有旧起点对照，而非把人声时间伪称原曲重新测量。此审计不将模型一致等同于人工准确证明。

实跑qwen/starlit_rails.blind-consensus.json：第4/41句获得word_onsets_and_line_end_corroborated范围；第26句只有line_only_corroborated（二词起点网站135.336 vs人声136.162秒仍差约0.826秒，输出words为空）；第14/46句unresolved。此范围仅为自动交叉证据，不是整首可发布或所有词尾的精确认证。

test_blind_consensus.py六项回归通过：当前实际分层结果、无确切ASR拒绝、缺少原生词数据降级、弱音素拒绝、0.8秒词漂移降级、旧起点无负对照拒绝。工具语法通过，原稿与正式字幕未变。已初步核对韩语专用kresnik/wav2vec2-large-xlsr-korean模型卡与固定修订，可作后续不用罗马字的独立边界实验；其说明是语音语料评测，未宣称歌声精度。镜像短请求失败，未下载其权重或运行它，不能把调研说成实测。


### 2026-10-26 韩文CTC准备、续传修复与尾段活动证据

前一轮字表核验是实际进展；本轮保留该缓存继续。作者kresnik/wav2vec2-large-xlsr-korean固定修订629c9a3501c10ba128bf3fa1eebb12af3be03f61，Apache-2.0，1205符号字表覆盖星光全部韩文，只缺AI/Mmm等拉丁字母。工具仅在模型输入将AI→에이아이、100→백；可选천리마→철리마发音候选独立保存；原稿不改，Mmm非韩文不强塞成伪词目标。模型卡是语音语料评测，未宣称歌声效果。

download_qwen_aligner.py新增--model korean-ctc，白名单仅取推理配置、字表、safetensors与README，不下载optimizer/trainer/pickle。hf-mirror来源仅作公开文件镜像，所有大小/内容最终按原作者固定HF LFS SHA256及Git blob校验。下载运行中尚未形成verification.json，不可宣称模型已验证/已推理。因实测部分网络响应会提前断开，download_alignment_model.ranged_part改read1，短缓冲先写入再续传，避免read(n)在EOF前抛错丢弃缓冲；总文件最终哈希仍必须通过。两个传输回归通过：中断/已有前缀逐字节恢复、服务器不遵守Range拒绝。适度调到12路续传，已完成块保留；旧下载进程经显式停止退出1，再启动新的续传句柄，不是因观察超时重启。

新增korean_ctc_path.py标准CTC Viterbi，保留空白和重复标签约束；test_korean_ctc_path.py用完整枚举路径独立核对单字/相异字/连续重复字/三字序列最优分数，另检查帧不足必须拒绝。两项测试通过。check_korean_ctc.py已实现韩文直接目标、前后原稿上下文、原曲/人声及两种截取窗口、字级分数/实际音频时间与无提示贪婪识别并存；模型权重尚未完成时未执行该神经推理，不把语法/DP单测说成歌声验收。

同时analyze_vocal_activity.py在已验证同时间基准的HDemucs人声上实际执行librosa.pyin+RMS诊断。210–237秒与230–250秒分别留档；后一个窗口的高周期性候选区间为230.04–231.12及233.50–237.96秒。它说明原声末段234.2秒后仍有可检测的有调性残留，但不证明是歌词、持音还是分离残留/混响，semanticVocalIdentityVerified=false，不直接改歌词结束。哼唱中215–228秒亦有多段活动，不能用此捏造Mmm的逐字时刻。新增cropBoundaryIsNotVoiceBoundary和intervalTouchesCropEnd，避免把截取窗口末端当真实发声结束。两个音频诊断进程均实际退出0。

本轮所有工具语法与四个路径/网络负例测试通过，正式字幕与Unity运行时代码未变。韩文模型权重续传进程仍需后续按活句柄确认完成，完成完整哈希验证后才能运行CTC诊断。

### 2026-10-26 后续自动校准实测（未发布）

此前“韩文CTC权重仍在下载”的状态已过期：固定版本的权重已完整下载并校验。实际执行星光第14/26句原曲/人声两种窗口共8次，以及第41句口白对照4次；口白也出现低后验与明显边界漂移，故不采用此模型输出，也没有继续把同样失败的方法套到千里马。

Qwen3-ASR-1.7B固定修订7278e1e70fe206f11671096ffdd38061171dd6e5完成校验并离线执行两首各4个窗口；不提供歌词提示。其结果仍未解决千里马口号及第14句词尾，不把同一模型家族的一致输出当作原稿错误证据。check_qwen_asr_windows.py新增独立模型选项与报告后缀；下载器改流式SHA256，避免大权重整文件读入内存。Qwen强制对齐新增--single-line独立输出，星光第14/26句8次实测仍有零长度/时间修补，未采用。probe_mms_consonants.py的16次重复辅音编码对照提高了部分分数，但第26句仍随上下文漂移，不改原稿、不发布。

已从Suno实际可见歌词核对星光原稿：清理提示标签后47句逐句完全一致，证据为tools/music/alignment_review/suno_exports/starlit_rails.source-visible.check.json。识别器省略“와”不足以修改原文。132–138.5秒人声的RMS/基频诊断另有JSON和PNG；音高转折不能单独证明词起点，所以未据此移动歌词。

证据目录：alignment_review/korean_ctc、qwen、phonetic_probes、activity、suno_exports。所有候选继续留在工具目录，未覆盖Assets歌词。以上为本地自动实验和源文字核验，不是准确同步完成；本轮未改Unity运行时代码，也未新增Unity验收。

### 2026-10-26 当前完整工程音乐运行验收

旧记录中“完整工程编译受Calculator依赖阻塞”不再代表当前状态。当前主工程直接执行Unity 6000.5.8f1批处理编译，日志正常退出return code 0，无error CS；不是隔离快照。随后在当前工程分别运行RailwayMusicTerminalReview.Batch和BatchRestore，两进程均产生MUSIC_REVIEW_OK。测试采用既有独立音乐测试存储及临时空场景，不保存场景、不使用经营存档。

本轮实际覆盖：真实GameMainUI音乐入口、收藏/歌单/队列交互与拖动、音量/进度滑块居中、歌词滚动条顶中底边界、1440及1024窗口、导入逐词字幕/翻译、点击跳转与偏移、舞台抬升/各曲配色/RMS/暂停冻结/减少动态、原稿阅读、已有两首自动草稿、跨进程暂停恢复与续播、真实曲末连播/单曲/随机和睡眠暂停。重新查看主界面、紧凑歌词和专注舞台截图，未见控件重叠或滑块偏离轨道中心。

归档tools/compile/music_acceptance_20261026包含三份日志、截图/运行结果和本轮关键源码SHA256。此为Editor中的Windows编译/Play验收，未生成或检查独立Windows Player。它不证明星光/千里马歌词时间准确：二者尚未发布同步候选，南风/枕木仍为自动草稿。Suno可见下载对话框仅提供音频/视频/分轨，无直接字幕格式；本轮没有点击Unlock & Download或消耗下载配额。原生下载选择不能替代歌词时间核验。

2026-10-26 第二曲在线对齐准备：重新打开LyricSmith，选择Korean并填入千里马49句原稿；页面Start alignment仍禁用，音频尚未选择，不能报告上传/对齐完成。已请用户仅操作本机文件选择，无需听校。audit_lyricsmith_ui.py新增--id支持两曲，比较前校验本地报告的曲目ID、实际MP3/原稿SHA256与行数，防止旧音频/旧原文交叉核验。星光47句108词重新审计通过，起点冲突仍为1/2/16/25/29/45，不放宽精度标准。test_lyricsmith_comparison.py验证正常身份及错误ID/音频哈希/原稿哈希/实际音频更换拒绝，运行通过；仅工具改动，不改正式字幕或Unity运行时代码。

2026-10-26 用户确认千里马文件已选择后，实际接管其现有LyricSmith标签：可见chollima_ride.mp3 Selected, ready to upload、Korean；重新展开原稿模式并填入49句，Start alignment可用且已点击。网站返回Security check failed. Please try again；通过可见Retry正常重试一次仍为相同错误，没有进入任务编辑器或获得结果。uploadCompleted保持false，仅记录uploadAttempted及可见错误。未读取安全令牌、Cookie或隐藏接口，未绕过验证；已请用户在保留原页/文件的情况下手动Retry并处理网站安全验证。此为提交被安全检查阻止，不能说音频分析已开始或失败。

2026-10-26 手动Retry后新结果：用户实际页面进入editor/bcbce3b5-7fea-4b0b-95b8-ea2566851543，Ready 32 lines，标题chollima_ride，播放器3:48。这不是49句原稿对齐，而是自由识别，包含英语/中文/日语/意大利语误识别，不能套回原稿或发布。逐句通过可见Go to line按钮和DOM词预览CSS完整保存32行102词到lyricsmith/chollima_ride.recognition.visible-{lines,words}.json，明确not authored alignment/native export。网站Save已点击；结果URL已记入manifest，uploadCompleted=true但alignmentCompleted=false，不再把文件未选作为当前状态。无法从结果页找到整曲按原稿重新对齐入口，因此回到同一用户标签准备Korean+49句原稿表单；仍需原生文件选择后用Start alignment提交，不能使用Start AI recognition。原稿和Assets正式歌词均未改。

2026-10-26 LyricTimestamps实跑：用户另行明确授权仅上传千里马MP3+49句原稿到本服务；授权前填词曾被自动审批拒绝，得到新接收方授权后才重试。登录账户实际剩余15分钟免费额度，未订阅/付费/生成API key。用户选择本机文件后，原稿49句填入并提交，网站进入完整行/词时间轴，49句原文保留；逐词步骤显示待查60处。为取得未人工调整的自动候选，仅推进网站确认步骤，未作听校或拖动时间，不能将网站“已对照录音检查”文案视为听校证据；humanListeningVerified=false。关闭默认加长朗读时间，不把静默延长视为真实发声。

标准JSON导出按钮已点击，网页提示已保存chollima_ride (标准).json，但常用下载目录尚未取得该文件。网站提供blob新标签页链接，工具打开被浏览器URL安全策略拒绝（仅允许http/https）；没有使用fetch/隐藏状态/替代浏览器绕过。已请用户手动保存JSON并给路径。submission.json标alignmentCompleted=true、nativeExportObtained=false、published=false；尚无可供本地对比的原生时间数据，未更新Assets正式字幕。

2026-10-26 用户提供Edge下载文件chollima_ride (标准).json（11,509字节）：audit_lyrictimestamps.py实际归档原文件到alignment_review/lyrictimestamps/native，核对授权音频/提交歌词/当前原稿和本地报告SHA256，49句97词逐字覆盖，无负区间/逆序/重叠。输出chollima_ride.native-audit.json，保留提供方confidence但明确不是准确率、后端独立性未验证、未听校未发布。结构正确不能等同精确同步。

原曲/人声MMS比较中，行起点均不相符的仅43/44；第43句网站164.47–172.66秒，与本地约170.84秒起点冲突（网站confidence仍0.906）；第44句网站198.10秒，本地人声189.302秒，且首词仅80ms、confidence0.496。第17句网站67.62–73.49秒与本地起点接近但词尾更长，不能单凭对比宣称网站或本地谁正确。多句词尾有差异，需区分持音与对齐漂移。所有数据保留，不修改Assets正式歌词、不根据高confidence直接放行。submission更新nativeExportObtained=true。

test_lyrictimestamps_audit.py检查有效输入/真实起点冲突标记，以及缺行、错词、NaN、重叠、零长度、越界confidence/index拒绝，2项测试通过。此为工具与证据改动，没有新增Unity运行验证。

2026-10-26 使用千里马原生JSON约束再对齐：refine_suno_context.py新增--provider lyrictimestamps，校验原生文件哈希、音频与原稿身份，保留原有窗口佐证/质量/分数门槛和独立输出。实际运行--include-boundary-conflicts，0项接收/5项拒绝：17句原曲音素碎裂反增，42句未改善或分数下降，44句导出窗口缺乏佐证；没有放宽门槛或修改正式歌词。

此外通过网站原生“比较各个时间轴版本”对话框，逐词点击Next，读取97个词各四个直接显示的起止时间；未选radio、未提交自定义选择、未播放或下载额外版本。保存chollima_ride.visible-comparison.json及variants-audit.json。每个可见词计数/总数及标准版时间均与原生JSON逐项核对（显示精度0.01秒）。该对话框直接展示数据，不读取隐藏状态、网络或受阻blob。行引导/字对齐/共识版本均未解决关键冲突：43句标准与字对齐同为164.47–172.66，行引导174.65起、共识167.86起；44句首词分别198.10、180.66、196.09、191.62起，差异极大。共识是网站给出的版本名称，不代表独立方法准确验证。候选仍未发布。

2026-10-26 重复副歌声学对照：新增probe_chorus_dtw.py，对哈希验证/零偏移的人声使用librosa 0.11.0，16kHz/20ms帧，MFCC与chroma分别做限斜率子序列DTW和独立固定速率偏移比较。前两段已知重复副歌为正对照，桥段与第二段主歌为无关对照（桥段并非保证无声/无歌词）。两特征的固定偏移正对照均67.20秒，最后副歌分别143.98/144.00秒；最后副歌相隔至少0.5秒的次优偏移代价明显更差，无关片段偏移优势弱。映射首个既有“아”候选到196.14/196.16秒。完整报告及8组特征/片段结果位于chorus_dtw/chollima_ride.json。自由DTW首词映射MFCC199.22 vs chroma196.54仍不一致，不能隐去这一局部不稳定或将整段固定偏移当逐词准确认证。

无提示原曲Qwen0.6B对照：188–195.5秒返回“음악”，195.5–198.2秒返回“아.”，198.2–200.8秒返回“설립하”（非原文确切匹配）。第一窗口完成并落盘后，旧工具因Windows重定向stdout的GBK编码异常退出1；check_qwen_asr_windows.py修复stdout为UTF-8，随后独立标签只补跑剩余两窗口并退出0，未将未执行片段报完成。两个JSON标签final-onset-control/final-onset-followup分别留档。

新增probe_chorus_phonetics.py，以两特征偏移及非邻近备选代价为前置检查，在196.15秒附近分别用0.35/0.7秒边距重算原曲/人声，4次均将아对到198.10–198.12秒，捕获的可能只是长元音末端；不把20ms路径当真实整词起止。另对194–201秒运行pyin/RMS，既有高阈值未检出可信区间（空结果），未由音高数据证明语义人声起点。结果共同支持继续检查196秒附近，但尚未达到精确首音确认，boundaryTransferVerified=false、humanListeningVerified=false、published=false。未改Assets歌词、未改Unity运行时。所有新增/修改工具语法检查通过。

DTW来源为librosa官方Music Synchronization与sequence.dtw文档：https://librosa.org/doc/0.10.2/generated/librosa.sequence.dtw.html 。声学匹配不等于歌词语义识别。

### 2026-10-26 直接导入逐词JSON，保留真实词尾

MusicTimedText新增.json数组格式：每项word/start/end/line_index，line_index从0连续分组；按原始词顺序保留每个词的显式起止和词间空白，不均分、不插值、不将confidence当准确证明。拒绝缺字段、空数组、错序/重叠、跳行索引、非有限/非正区间；沿用1MB与5000行限制，另限10000词。Windows字幕选择器及本地同名字幕伴随导入支持.json，已有LRC/VTT/SRT优先级不变。导入只是用户选定的字幕数据，不改变内置原稿/解锁规则，也不自动导入当前未校准的千里马候选。

归一化词尾逻辑改为同时取现有词尾、下一词起点和行尾的最小值，避免把JSON已知词尾拉长到下一词。LRC原有按相邻词时间推导的行为保持，JSON可准确表达词间静默；当前不支持任意其他网站JSON封装。界面沿用现有导入字幕入口与本机独立音乐存储。

当前完整工程RailwayMusicTerminalReview.Batch重新编译/运行通过，日志tools/compile/music_json_runtime.log含MUSIC_NATIVE_JSON_OK（真实下载49句97词，14.44词尾与14.46下一词起点分别保留）、MUSIC_JSON_IMPORT_OK（导入/读取与坏文件保护）、MUSIC_REVIEW_OK。新增缺start/line_index、错序重叠、缺行索引、错误封装/空数组负例均由Unity实际解析检查。原有舞台/滚动/暂停/字幕交互回归通过。此轮未再运行第二进程恢复或独立Windows Player；不将原生数据解析成功称为歌词时间校准成功。

2026-10-26 星光LyricTimestamps实跑：用户明确新增授权星光MP3+47句原稿，用户选本机文件后实际填词提交，进入完整47行结果；显示63处待查，关闭默认加长朗读时间，未人工调时间或编辑原文。只推进候选导出流程，网站“准确/已检查”按钮文案不构成实际听校。未下载或打开受阻blob；直接通过本来提供的比较面板逐词Next读取108词各四版本显示时间、当前高亮词和整句上下文，取消对话框未提交任何选择。保存starlit_rails.visible-comparison.json。

新增audit_lyrictimestamps_visible.py：逐项核对UI词序计数、总108词、高亮词正文及上下文与47句原稿，验证实际音频/原稿/本地比较哈希；显示精度0.01秒，不伪称原生导出或补造confidence。标准版区间有效，起点与两个本地源均差>0.25秒的为1/2/16/22/25/45/46；行引导与字对齐版有逆序/重叠，拒绝作为完整时间数据；共识版有效但起点冲突34句。第26句网站第二词135.34秒，接近旧LyricSmith135.336但仍与MMS136.162冲突；第14句73.98–77.32较本地词尾长；口白41句184.78–189.34一致；哼唱46句222.82–226.32与其他结果冲突。未更新Assets正式歌词。

audit_lyrictimestamps.py新增--id支持已分别授权的两首；仅可见证据模式显式允许confidence缺失并加provider_confidence_not_visible，默认原生检查仍拒绝缺失confidence。新增负例确保不伪造置信度，3项测试通过。相关源和提交状态保存在alignment_review/lyrictimestamps；本轮无Unity运行时代码改动，未重复Unity验收。

2026-10-26 星光新片段及内部时序复查：Qwen0.6B原曲无提示4窗口实际退出0，online-onset-control.blind-asr.json中14–18.5秒返回quietness（非原文），18.5–21秒返回검은서 불빛（非确切原文），125.5–129.4秒返回나는 야，129.4–133秒返回하지만。不能称其为原文确切听写或精确时间；它提示开头网站长窗口和25句本地晚起点都不能仅凭模型一致放行。

只读检查当前两曲最新本地原曲/人声报告的词内部时序，local_word_integrity_audit.json发现星光original25/41与vocals24/25有词重叠/词超出行范围；其中三项旧flags为空，千里马未发现此类结构异常。publish_phonetic_draft.py补充原曲词区间有限性、顺序、行内范围检查；无有效人声词序或带弱证据flags的人声不能仅凭外层边界接近作交叉支持，仍可走既有确切ASR替代规则。未降低阈值、未改原稿或既有发布草稿。

新增test_phonetic_word_integrity.py验证真实四项异常及有效口白人声对照，覆盖重叠、NaN/Infinity、越界、零长度、空词，2项通过。实际尝试星光最新候选发布被12项未解决检查正确拒绝，生成publication-check，未写入Assets歌词。此为修复自动验收漏检，非准确同步完成，也未因此宣称已发布草稿经本轮重新听校。

2026-10-26 定位并修复词倒序根因：align_ost_phonemes.py的逐词ASR窗口/孤立音素修复此前不检查前后词，星光25句首次全曲CTC原起点126.67秒，被ASR错误窗口128.32–130.92及局部音素簇继续推到129.80/129.94秒，越过第二词。新增span_stays_ordered，所有逐词ASR/音素簇/发音修复只在已选前词末尾和下一原始词起点之间接收，拒绝记录保留orderAccepted=false；异常基准直接失败。没有排序、裁切、均分或编造修复时间。

新增--tag保留独立证据，实际缓存重算starlit_rails --local-context --section-context --tag ordered退出0，输出local.sections.ordered。原曲和人声各47行的内部词区间均有效，25句两源均恢复126.662秒起点。仅将最新provider.boundaries报告中四个结构无效行替换成新有效重算，其他全部既有修正逐字段保持，保存provider.boundaries.ordered，不覆盖原报告。orig41外层边界仍不一致，vocals24暴露fragmented标记，不能因结构修复就声称声学准确。

测试：邻词边界防跨越与真实合并保留所有有效旧修正，2项通过。发布器新增这一独立variant供检查，实际9项未解决检查拒绝发布（此前12项）；Assets正式歌词未变。来源/音频哈希保留，新旧报告SHA256记在合并报告内；Unity运行时代码未改，无新增Unity验收。

2026-10-26 分句选源候选：新增select_corroborated_lyric_lines.py，读取顺序修复后的本地报告、原生/已观测网页边界审计和无歌词提示ASR，校验音频/原稿/所有原生导出哈希；只针对已有明确分层证据的4/26/41句采用人声报告，并保留selectedAudioRole=vocals和完整selectionEvidence，不把它们改名为原曲测量。第4/41句仅词起点和行尾有交叉支持；第26句仅整行有支持，输出words为空，不能据此启用逐词高亮。输出独立starlit_rails.scoped-selection.json，不改原报告或Assets。

其余行保留original候选并明确wordTimingScope=unverified_phonetic_candidate，不因两个音源接近而宣称准确。unresolvedLocalGateLines为14/24/44/46，只表示当前本地结构/质量/两音源门槛仍失败，不代表其他行所有网站冲突已解决，也不是整首准确率认证。该候选没有进入默认发布器。分层审计6项正负例再次通过，脚本实际运行成功；本轮无Unity运行时改动、无新增Unity验收。

2026-10-26 星光24句音素簇窗口修复：官方国立国语院标准发音第18条说明ㄷ在ㄴ前鼻音化为ㄴ（https://m.korean.go.kr/front/onlineQna/onlineQnaView.do?mn_id=216&pageIndex=1&qna_seq=313220）；据此为듣는单词新增显式--nasal-assimilation模型输入候选든는，原文不改，policy单列。首次ordered-nasal实测提高分数但未消除人声碎裂，不报成功。进一步将孤立音素簇的重算窗口限制在已选前词末尾和下一原始词起点之间；窗口帧数不足保留原span，原有ASR约束仍采取拒绝越界，避免强行压缩到错误ASR窗口。

ordered-nasal-bounded实际原曲/人声重算退出0：24句外层起止不变，original约120.102–123.682，vocals约120.122–123.682；人声듣는词末123.102修正为121.962，maxInternalGap1.18降到0.46、词分数0.057升到0.134，整句分数0.163升到0.263，原有碎裂标记消除；原曲整句分数0.210到0.236。对应词语义仍由原稿保留，不声称由该分数认证整首精准度。

只合并24句两源到独立provider.boundaries.ordered.nasal，其余行逐字段不变并保存父报告/新报告SHA256。分层选源工具改用新候选，unresolvedLocalGateLines现在14/44/46；该列表仅为本地检查失败，其他在线起点冲突及未验证词尾不据此消失。新增真实合并回归，连同邻词边界与前次顺序修复保留检查3项通过。

同轮无提示ASR原曲119.5–125.5返回첨드는 이야기、206.5–209.5返回차철길처럼，均非原稿确切全句匹配，未用来放行任何句子。证据在remaining-phrases.blind-asr.json。没有发布Assets歌词或改Unity运行时，无新增Unity验收。

2026-10-26 星光44句独立Whisper检验及14句拖音：small与medium分别执行206.6–209.8/205.4–211.8/206.8–209.4三个原曲窗口，两个进程均退出0；结果在各自line44-control.window-check.json。small短词저概率仅0.008、另一窗误识别차；medium确切文字短窗저/철길처럼概率0.629/0.708，宽窗合并词0.706且起点207.78偏晚。没有达到既有独立ASR门槛，不采用截取窗口起点为声学起点，不放宽阈值放行44句。

14句另在人声73.5–78.2秒实际执行原配置pyin/RMS，候选高周期区间74.94–77.08以及77.78–77.96；后段属于可能下一句，未混入14句。音素人声末尾75.182秒之后、网站候选末尾77.32秒之前仍有88帧高周期性候选（具体计数以line14-tail-audit.json为准），最后高周期帧约77.20秒。这是“音素末尾不等于持音结束”的实际证据：不能把75.18秒直接当整句声学终点。它仍不单独证明该残留的语义词身份或77.32秒精确词尾，semanticWordEndVerified=false，未修改Assets。

证据activity/starlit_rails.73.5-78.2.json及line14-tail-audit.json记录原音频哈希与父证据哈希；不使用静默延长或帧间均分生成歌词时间。本轮无代码改动、无Unity重复验收。

2026-10-28 多渠道对照：Folia在线来源为网易云、QQ、酷狗、波点、Spotify、YouTube Music，另有Navidrome/Subsonic；本工程另有B站、WebDAV、Apple Music、SoundCloud旧入口。新版接入状态以 MUSIC_CHANNEL_CAPABILITIES.md 为准。

## 2026-10-29 多来源与体验更新

参考 Folia 的来源划分、扫码、封面取色、播放和歌词舞台行为，Unity 部分独立实现多来源桥接与统一曲库；能力和真实验证分项见 [渠道能力清单](MUSIC_CHANNEL_CAPABILITIES.md)。网易云、QQ、酷狗、波点采用独立子进程，Subsonic/WebDAV 支持私有服务器；实际账号与服务端兼容性尚需核验。在线来源封面可驱动歌词舞台色彩，来源真实 LRC 可用于逐行同步；未制作新的原声歌词时间轴。
