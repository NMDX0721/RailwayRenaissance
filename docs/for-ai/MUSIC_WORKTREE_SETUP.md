# Music App 独立工作树

创建日期：2026-10-27（Asia/Shanghai）。

- 目录：`D:/Unity Project/RailwayRenaissance-MusicApp`
- 分支：`codex/musicapp-dev`，基于 `codex/musicapp` 的 `fe849c4298d2fefd2454bcaf6902c353d29c5c0c`。
- 音乐内容来源：stash `f4c59723eca493e602d626cd14cdff16812aaa24`，新增文件快照 `0a229f61224a4a89775c66368e98ee3a7912925e`。
- 仅迁移音乐相关源码、资源、校准工具与报告，共 556 项；主工作区源码与原 stash 均未修改。
- 音频引擎与播放器入口按同一音乐快照成对恢复，保留目标音轨返回契约；其他基线功能不改。
- 未提交、未推送，迁移内容保留为工作区改动。
- Unity Library 和本地模型缓存未复制，不共用 Unity Library；首次打开需要重新导入。
- 本记录不表示歌词准确同步已经完成，也不代替 Unity 运行验收。

## 本次验收

- Git 工作树登记与分支确认通过。
- 556项迁移文件逐字节匹配来源快照，主工作区音乐文件未被写入。
- 顺序约束、逐词JSON审计与分层证据回归共12项通过。
- 音频引擎目标音轨接口与调用方匹配，两个字体资源均存在。
- 未启动新目录的Unity，不声称本次已完成Unity编译/运行验收。

## 2026-10-27 依赖恢复
直接复制单个 Library/PackageCache 包未被 UPM 接受。官方 Burst 1.8.30 下载存在提前截断；保留默认 TLS 验证，以官方 CDN Range 请求逐段检查 Content-Range 与长度，完整407791913字节 SHA1=2f3d916873ea867ff077372df3fe42447ba7d4c6，与官方缓存元数据一致。仅填入本机 UPM 的该包 tarball 内容键，未降级依赖或共享 Library。已重启 RailwayMusicTerminalReview.Batch，日志 music-review-20261027-verified.log；最终结果待实际标记。


本轮实际验收发现迁移缺口：Assets/Resources/bgm 在新工作树为空，MP3被Git忽略。已从主工程补齐26首原始MP3及26份meta，仅写缺失文件并逐个SHA256比对一致；清单 Library/music-bgm-assets-restored.json。此前556项迁移校验只覆盖源码/工具清单，不代表运行资源完整。首次播放失败日志 music-review-20261027-local-package.log，补齐后复验日志 music-review-20261027-audio.log。首次列表索引异常调用栈属于 UnityEditor.Search.SearchDatabase，未改音乐逻辑规避。


## 2026-10-28 收尾验收（取代上文待验证状态）

- 新工作树Unity 6000.5.8f1编译正常退出0，无error CS：music-review-20261027-final-compile.log。
- 实际GameMainUI音乐入口及完整交互通过：music-review-20261027-audio.log含MUSIC_REVIEW_OK。覆盖播放/暂停/跳播、收藏、歌单、队列拖动、文件缺失跳过、开关窗口、统一帧推进、1024/1440布局、滑块居中、字幕跟随及舞台PCM响应/暂停冻结/减少动态。
- 独立Unity进程恢复通过：music-review-20261027-restore.log含MUSIC_RESTART_OK、MUSIC_MODES_AND_SLEEP_OK、MUSIC_REVIEW_OK。恢复暂停和保存位置、音量、偏好、模式及睡眠。
- 已实际查看主界面、唱片详情、紧凑布局/队列、南风/星光舞台截图；未见明显溢出。归档tools/compile/music_acceptance_20261028。
- 补齐26首被Git忽略的原声及meta，修正帮助遗漏逐词JSON格式。不是新增歌词校准；保留草稿和未验证标识。
- 原注册表依赖配置复验仍因Burst下载aborted退出。为保持本机可打开，最终manifest保留file:../Library/burst-1.8.30.tgz，官方同版本包完整性已核对；原manifest/lock备份在Library/MusicAcceptanceDependencyBackup。Library清理后需重新准备此包，当前配置不能直接视为跨机器可复现发布方案；不要直接提交本机路径配置。
- 本轮没有独立Windows Player构建或硬件听音验收。编辑器搜索索引异常仍出现，调用栈属于UnityEditor.Search，不将其说成全工程无异常。
- 自动审批拒绝git restore清理未提交文件，未执行恢复；保留所有相关meta、渲染及ProjectSettings自动改动，未提交或推送。
