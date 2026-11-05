# VN收尾、平板内容迁移与缓存修复

用户最新目标：文本和立绘最后检查、书签基本操作、Windows与Android平板适配；确认旧站长日志迁移并正式删除；修复音量热路径与缺图负缓存。Android以平板为主，不将手机刘海屏当成主验收目标。

## 旧界面已正式移除

`Assets/Scripts/TitleArchiveUI.cs`、`MainStoryUI.cs`及各自`.meta`已物理删除。原文件GUID没有场景/预制体/资产引用，运行时源码不再依赖这两个类。不是保留旧界面再隐藏。

| 原内容 | 平板中的正式去向 |
|---|---|
| 剧情章节、手动/自动书签 | 归途；新增“阅读书签”入口 |
| CG、场景鉴赏 | 相册；补齐七张真实序章插画及原场景目录 |
| 原声鉴赏 | 音乐；现有曲库覆盖全部旧音乐ID |
| 人物、列车图鉴 | 媒姬百科；补齐原目录及佯明远教授 |
| 成就 | 笔记中的成就记录；保留已解锁成就笔记联动 |
| 运营累计数据 | RDA中的运营统计 |

解锁逻辑独立为`StoryArchiveProgress`，数据目录为`StoryArchiveCatalog`，回放路由为`StoryReplayService`。既有`ArchiveCG_*`、`ArchiveScene_*`、`ArchiveStory_*`、`ArchiveChar_*`、`ArchiveTrain_*`、`ArchiveMusic_*`、`Achievements_Data`、`Stats_*`、`Bookmark_*`沿用，不批量删除或重建用户进度。旧标题兼容按钮重定向归途；归途回放不再调用旧Main Story页面。

相册由Sprite加载改为Texture2D，兼容现有Default纹理导入；客舱昼夜沿用运行时正确映射。旧规划中无图片的`cg_village`/`cg_museum`不显示为空白占位，当前七张实际序章CG可进入灯箱。原始目录元数据仍保留在独立数据目录，不构成旧界面。

“进入章节”仍记录`ArchiveStory_*`；真实完成章节写`ArchiveScene_<script>`，不再把领取0721的标记当作十章全部通关。旧已访问章节仍可通过既有阅读记录回看。

## 文本与人物

- 对十章2399条旁白/对白/心理活动检查占位、编码异常、说话人及37条thought格式；同时在五种画幅调用生产折行逻辑检查100逻辑像素正文区容量。
- 03章两处冗长叙述压缩；检查员态度转变保留讽刺意味，空域屏幕改用“四个红色标记”。场景/条目顺序不变，保护旧存档与书签坐标。
- 归途的十幕标题/简介按实际剧情修正，去掉“★样板”、错误毕业描述、0721机车/沙能点火等不符内容；百科区分NF-5K内燃头车与0721飞行载具。
- 男主立绘继续隐藏；其他主立绘保持正常比例。嘉颖徐半身立绘多人时等比缩放至原构图的80%，避免头部相对过大；单人不改。没有重画服装或同质化角色。
- 当前资源清单缺图0、表情回退0；不能据此声称所有人物美术已达到主观最终质量或全游戏资源齐备。

## 书签接口与行为

`Bookmark.variables`保存选择变量快照，恢复前还原。手动ID仍为`bm_`加17位时间数字，毫秒冲突时递增，兼容本地备份校验；`updatedAt`按持久化最大值和当前UTC时间单调递增，保证快速更新也能区分先后。

五个自动槽滚动保存，恢复最新有效记录；重读会撤销旧完成标记，暂停时落盘。跳转检查脚本/场景/条目，清掉冲突的读档入口，只启动一次目标位置；“读档标志=0”的残留键不再挡住恢复提示。手动删除有取消/确认，保留其他书签和存档。

旧书签没有快照，无法推导当时未知选择；保持旧位置兼容和安全跳过条件条目，不宣称无损还原历史分支。新增字段随原书签JSON保存，本地备份保留原始JSON，不改变其动态ID规则。

## 音量缓存与立绘负缓存

`GameData`四个音量getter改读内存；首次访问或显式`ApplyVolume`时集中读取四项PlayerPrefs。设置setter持久化后重新应用，句中修改仍及时生效。逐字循环和`PlayTypewriterSFX`读取音量时不再反复调用PlayerPrefs。外部直接改PlayerPrefs后应调用`ApplyVolume`刷新；既有设置UI均走属性setter。

`CharacterSpriteManager`缓存查空结果：命中过的null路径直接跳过，正常图/默认表情照常缓存；同一角色/表情的缺图警告只记录一次。`ClearSpriteCache`提供显式失效入口，便于运行中补资源后重新加载；新场景实例自然重新建缓存。计数器`AudioPreferenceReadCount`和`ResourceLookupCount`用于量化查找次数，不将其等同实际磁盘I/O次数。

## 适配与验证边界

Windows画幅：2560×1440、1280×720、1024×768。Android平板模拟画幅：2560×1600（16:10）、2048×1536（4:3），包含底部系统区域留白。`VNViewportLayout`对人物、菜单、对白、回顾、存读档及选项应用安全区域；触摸按钮增大命中范围，菜单图标增加中文标题。正文按实际文本区宽度折行，改变宽度后重排。

Android已配置ARMv7＋ARM64与IL2CPP。此机器安装SDK/NDK/JDK，但ADB设备列表为空。以上布局与触摸在Windows Unity Editor内模拟；没有Android平板实机、APK或独立Windows Player验收，不把编辑器检查写成平台发包通过。

`VNFinalAcceptanceReview.Batch`只允许在`Library/VNCompiledReviewProject`运行，测试公司/产品名独立，避免写入用户正式存档。书签经实际UI按钮、模拟Touch事件及真实VN_Test场景重载；迁移另验证实际GameMainUI启动归途书签。失败尝试日志与原始结果保留。Editor已知SearchDatabase启动异常单列完整堆栈，其他异常仍使测试失败。

最终成功标记、截图及源码哈希以冻结目录`tools/vn/final_acceptance_20261103/`为准。原始旧界面截图仅是迁移前证据，不代表新版本入口仍保留。

### 最终结果

- `tablet_archive_migration_20261103_final.log`进程exit 0，`results.json`为`success:true`，任务错误0；已知Editor SearchDatabase启动异常另列完整堆栈，没有删掉原始诊断。
- 191个运行时源码及1个Editor验收源码编译通过；冻结核对192个源文件、十章JSON、106张PNG和实际加载程序集哈希一致。
- 五种画幅各检查2399条文本容量、三人立绘与三选项显示。书签添加触摸、删除取消/确认、五槽滚动、暂停落盘、真实重启提示及分支位置恢复通过。
- `VN_AUDIO_CACHE_OK`：真实200字打字机过程中音量偏好新增读取0；设置setter及显式刷新立即生效。`VN_NEGATIVE_SPRITE_CACHE_OK`：80句全缺图仅首次4次资源查询；80句缺表情仅新增3次（包含normal回退），显式清理后可重新加载。
- `VN_ARCHIVE_MIGRATION_DATA_OK`与`VN_TABLET_MIGRATION_RUNTIME_OK`：旧UI类型不在程序集，七张实际CG、九个旧场景、十三个旧音乐ID、十个人物条目（含新增教授）、两个列车条目及成就/统计均有新应用去向；真实GameMainUI可启动归途书签。自动状态保留已经打开的书签页。
- `archive_migration_audit_20261103.json`确认四个旧源文件/meta物理删除、生产源码引用0、场景/预制体/资产悬空GUID引用0。历史PlayerPrefs键未批量清除。
- 本轮所属源码/文档差异空白检查通过。共享`ProjectSettings.asset`另含既有Unity生成字段的行尾空白，未为清洁报告去改动那些无关配置。

日语方案与四人试听台本草案见 [日语配音制作](VN_JAPANESE_VOICE_PLAN_20261103.md)；本轮未生成音频。
