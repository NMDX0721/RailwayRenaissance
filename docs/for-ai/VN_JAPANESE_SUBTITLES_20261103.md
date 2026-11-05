# 日语副字幕与翻译接入

本文件记录早期实现。用户后续确认的当前方案为中/日/英通用译文目录、逐句配对且不强制每句换行、小副语言在上、大主语言在下。架构、流排与Gemini工作包以[VN多语言协议](VN_LOCALIZATION.md)为准；下文整段上下字幕及日语专用字段是历史说明。

用户确认：正式配音前先完成日文翻译；中文主字幕保留，另支持日语副字幕。男主不配音。

## 2026-11-03布局与语言设置修订（当前实现）

用户否定外置日文条后，移除`japanese-subtitle`与“日语副字幕”布尔开关，改为同一对白框内的主文字、间距和细分隔线、较小副字幕。两种文字共享正文左边缘，副字幕不再从画面左缘横跨另起黑条；长句按实测高度扩展同一框，不将译文叠在框外。副字幕整句显示，中文打字机与术语命中仍保持；副字幕语言/字号改变不会重新启动主文字打字机。主语言改变会重新渲染当前句。

新增`VNSubtitleSettings`统一管理剧情字幕，平板设置→显示与亮度→剧情字幕语言提供主字幕（简体中文/日本語）、副字幕（关闭/另一语言）、副字幕字号（小/标准/大）和即时预览。不会把同一种语言同时作为主副字幕，互换主语言时保留双语组合。缺日文时主字幕回退中文，不重复再显示中文副字幕。界面其余部分仍为中文，不把该选项当作系统语言切换。日文翻译未齐的说明放在设置内。

偏好键：`VNPrimarySubtitleLanguage`默认`zh-Hans`、`VNSecondarySubtitleLanguage`默认`none`、`VNSecondarySubtitleSize`默认20（18/20/24）。只对明确存在且为1的旧`VNJapaneseSubtitles`迁移成日文副字幕，未设置过的新用户不会自动启用。恢复显示设置重置中文主字幕、副字幕关闭。旧布尔键只读兼容，新键存在后不再生效。

主字幕在双语或日文模式下最小28逻辑像素，副字幕最大24，保持层级并通过框高扩展容纳换行。姓名牌与主字幕对齐；文字区右侧留64逻辑像素供继续箭头使用。新增完整设置页DropdownField回调、语言交换、持久化、缺译、隐藏与四画幅的检查。下文第一轮外置字幕描述及证据仅为历史实现，不作为当前布局依据。

当前验证入口仍为`VNJapaneseSubtitleReview.Batch`，其v2测试输出`tools/vn/subtitle_language_v2`（隔离项目内）；编译目录`tools/vn/subtitle_language_v2_compile`。测试通过生产VNManager和JsonUtility显示正文及译文，等待打字机完成后验证整句，再检查共享框边界、字体层级、继续箭头留白及实际设置页控件回调。新设置页的控件value变更触发真实回调和即时预览，尚未声称安卓原生触屏点击弹出菜单验收。

最终`subtitle_language_v2_20261103_accepted.log`进程exit 0、`VN_SUBTITLE_LANGUAGE_REVIEW_OK`、结果success:true，任务异常0。Windows 2560×1440/1280×720、平板2560×1600/2048×1536四画幅通过完整文本、同框边界、箭头不遮挡、语言交换、旧偏好/新默认、缺译回退与设置回调检查；实际查看2.5K正文、4:3平板设置截图。193个运行时/Editor源文件哈希与编译输入一致，截图/日志/结果/源码编译清单冻结到`tools/vn/subtitle_language_v2_acceptance_20261103`。

首轮候选截图曾在打字机未完成时捕获，已保留在`tools/vn/subtitle_language_v2_first`，不作为最终完整文本证据。箭头边界两次失败日志保留，最终按实测坐标调整。Editor既有搜索索引异常完整堆栈在结果`editorDiagnostics`中单列；任务检查通过不等于编辑器无任何诊断。未进行Android平板真机或独立Windows Player验收。

## 数据与显示

`DialogueEntry.textJa` 是可选、审校后的日文字幕，不包含TTS情绪标签、假名发音替换和角色姓名前缀。中文仍使用 `text`。缺字段的旧JSON和存档兼容，不改动章节/场景/条目顺序；本轮没有把试听翻译草案写入正式剧本，不能宣称十章日文翻译已齐。

`VNManager.ShowCurrentDialogue` 将 `textJa` 传入 `DialogueBox.ShowDialogue(speaker, text, textJa = null)`。独立的 `japanese-subtitle` 标签在中文框上方整句显示、自然换行，不参与中文打字机或术语命中区，不替代中文；关闭对话框时跟随隐藏。窗口宽度变化重排时保留当前译文；推进到无译文句子清空，不残留上一句。

平板设置 → 显示 → 对话与阅读 → 日语副字幕；`VNJapaneseSubtitles` 默认为1。`DialogueBox.SetJapaneseSubtitlesEnabled` 持久化并通过事件立即更新现有对话框，不在逐字循环查询设置。恢复显示默认值也恢复此开关。

当前范围是常规对白/旁白/心理活动的主对话框，不包括选项翻译、滚动新闻、地点卡或回顾列表的双语显示。后者需要各自的翻译字段与布局，不能把本次功能泛称全游戏日语本地化。

## 制作流程

中文行ID/源哈希冻结 → 翻译草案 → 人设语气与术语审校 → textJa → 单独派生TTS发音/表演稿 → 用户审批生成 → 音频听校 → 按稳定行ID绑定。两种文本均须版本化；任何中文/日文变动都重审音频，不能按易变数组下标盲配。日文文本显示与音频播放相互独立。

## 附带编译修复

现有 `VNManager.RestoreCharacterSpritesUpTo` 中 `activeChars` 被声明为 `string[]`、`activeReactions` 为不存在的 `ReactionData[]`，与现有 `CharacterEntry[]`、`string[]` 接口不符。只修正两处局部变量类型，未改回顾恢复算法。

## 验证

`VNJapaneseSubtitleReview.Batch` 只允许在隔离项目 `Library/VNCompiledReviewProject` 中运行，独立公司/产品Prefs名称；通过生产JSON反序列化和VNManager显示链检查中文保留、日文内容、缺译清空、实时开关、隐藏、字形和多画幅边界。源码编译目录 `tools/vn/japanese_subtitle_compile`；冻结证据在 `tools/vn/japanese_subtitle_acceptance_20261103`。

191个运行时源文件与专用Editor检查编译通过。隔离Unity进程在`-noUpm`模式exit 0，`VN_JAPANESE_SUBTITLE_REVIEW_OK`与结果success:true；2560×1440、2560×1600、2048×1536、1280×720四画幅均通过上述检查。实际查看Windows 2.5K与4:3平板截图，日文正常显示、中文保留。当前测试译文在fixture中，正式剧本未灌入草稿；完整日文长句覆盖仍需翻译后逐句验证。

首次沙箱启动卡在许可IPC，重试遇到隔离包管理path错误，最终使用已有依赖并跳过包管理运行。原日志均保留。最终日志含已有`UnityEditor.Search.SearchDatabase`启动索引异常与包管理/网络诊断；已检查堆栈，未把这些诊断说成任务代码无异常的证据。测试本身的功能断言均通过，未验证正式平板设置按钮交互、回顾双语或完整游戏流程。编辑器模拟不等于安卓平板真机或Windows Player。
