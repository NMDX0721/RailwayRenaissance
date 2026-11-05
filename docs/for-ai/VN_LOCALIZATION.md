# VN多语言底层与翻译交付

2026-11-03用户确认：简体中文为源语言，日语与英语均须支持；批量翻译交给Gemini。字幕按句配对，小号副语言在上、大号主语言在下；主语言暖白、副语言浅金。ElevenLabs仍暂停，任何生成/保存/付费前另行审批。

最新排版澄清：逐句对应不代表每句强制换行。句组在宽度允许时同行排列，超宽才自然流到下一行；同一视觉行对齐主语言的基线。按用户“句子间不要空格，正常连续”修正，同行句组间距为0，组宽仅由主语言文字决定；副语言自行在对应主句宽度内换行，不撑宽主句。主标签水平margin/padding为0。流排换行间留8，组内副文在上、主文在下间距4。长句内部主文折行保留12逻辑像素行距，不在每句后额外插入空白行。

副语言在每个对应主句的宽度内水平居中（UpperCenter），不是按整段或整屏居中；主语言保持左对齐，不插入人为句间空格。

## 资源与运行时

- `Resources/Localization/VN/languages.json`登记语言code/name；设置页主/副语言选项由该目录生成，不用为增加语言再写一套日语专用UI。
- 译文按`Resources/Localization/VN/{locale}/{scriptId}.json`加载。`VNLocalization`按语言和章节缓存命中及缺文件结果；清缓存入口`ClearCache`。加载不在打字机逐字循环执行。
- `DialogueEntry.id`为可选稳定ID，推荐新编剧时填写且全章唯一；译文key为`scriptId/id`。既有无ID剧本回退`scriptId/s{sceneIndex}/d{dialogueIndex}`，同时校验源文SHA-256，移动/修改条目后不能把旧译文静默绑定到新句子。选择项追加`/option-{原始索引}`，不按条件过滤后的按钮序号绑定。
- 源文中文仍在原剧本`text`，不将世界观、控制指令、资源ID或条件变量翻译。旧`textJa`只做兼容，正式新增译文用独立文件。
- 主字幕缺译回退中文；副字幕缺译隐藏；回退与副语言相同则只显示一份。支持中文/日文/英文作为主、副语言的任意不同组合。
- `VNManager`对白走`VNLocalizedLine`；选择提示/选项、地点卡、滚动新闻、启动文本支持主语言显示。CG、特殊指令等仍使用原资源/命令值。姓名牌、平板系统UI、回顾列表与存档预览仍保持原有中文内容，不能声称全游戏已本地化。

## 逐句配对

`VNLocalizedLine.Sentences()`按翻译文件显式`segments`关联源句与译句。每个segment的source/text连接后必须分别等于整条源文/译文；两种非中文语言共同显示时还需共享相同source边界。不会按两种语言的句号数量猜配对；无明确分段的旧译文只作为一个配对单元，不伪造逐句对应。

`DialogueBox.Localization.cs`负责句组排版。每组副字幕在上，主字幕在下；同一语言职责的字号/颜色一致。主字32，副字小16/标准18/大22可选，标准约为主字56%；`VNSecondarySubtitleSizeV2`保存新尺寸，旧尺寸18/20/24依次兼容到16/18/22。长文本在同框内滚动，正文视口高最多占逻辑画布48%，避免整块遮满背景。滚动区按下会抑制VN同帧推进，避免触摸滑动误跳句；继续箭头在滚动区外。`TypewriterEffect.TextUpdated`将同一主文本打字进度投射到各句标签，点击跳字仍一次补全整条，完成事件只触发一次；中文术语命中按各句真实容器挂接，滚动时随句组移动。

副字幕可整句提前显示；主/副语言或字号改变会重排当前条目，已完成的条目立即补全。翻译对应单位是明确源句，不承诺每个自动折行的视觉行都可逐字对齐。英文主文本折行优先按空格移动完整单词，超长单词仍允许兜底拆分。

## 译文文件协议 v1

```json
{
  "schemaVersion": 1,
  "scriptId": "vn_localization_sample",
  "locale": "en",
  "entries": [{
    "key": "vn_localization_sample/suiyue_deadpan",
    "sourceHash": "由导出工具生成，不能修改",
    "status": "ready",
    "text": "完整译文",
    "segments": [{"source": "对应源句，逐字保留", "text": "该句译文"}]
  }]
}
```

`draft`不进入运行时；`ready`必须有显式segments，表示译文已整理为可接入文本，不代表声优、母语审校或用户已经验收。导出包还提供只读`source/kind/speaker/emotion/sceneIndex/dialogueIndex`供翻译理解语境。输入源文统一CRLF→LF后以UTF-8计算SHA-256，不做简繁、全半角或标点归一化。坐标回退键不等于永久ID；若要调整旧剧本场景/条目顺序，必须先补稳定ID或重导出/重审翻译，不能仅凭同文本哈希推断语境没有变化。

## 给Gemini的工作包

`tools/vn/gemini_translation_package_20261103/ja`与`en`分别包含十章序章的翻译模板，当前text为空、status=draft，批量翻译未由Codex执行。文件含角色、表情、类型及固定源句分段。

1. 只填写text及segments[].text。保持key/sourceHash/源文分段不变，译文整段必须与分段译文逐字连接一致；英文句间需要空格时放在前一段译文末尾。
2. 原意、数据、术语、冷幽默及角色语气必须保留。人名日语读音未定的条目留draft并单独提出问题，不自行省略姓名或改成日本姓名。
3. 不在显示译文中放TTS情绪标签、读音括注或导演说明。另行制作TTS稿与发音表。
4. 文本审校完成后再将对应status改为ready；缺译可保留draft。
5. 回传同名JSON文件。先验证，再将文件复制到对应Resources语言目录，通过Unity显示检查后接入。不是把模板直接覆盖中文剧本。

导出（拒绝覆盖已存在翻译工作）：

```powershell
python tools/vn/translation_package.py --locale ja --out tools/vn/new_ja_package
python tools/vn/translation_package.py --locale en --out tools/vn/new_en_package
```

验证回传：

```powershell
python tools/vn/translation_package.py --locale ja --validate <回传目录>
python tools/vn/translation_package.py --locale en --validate <回传目录>
```

验证源ID、哈希、重复/未知key、分段源文/译文拼接、空ready内容和常见TTS标签。工具只在本地读取/导出/校验，不发送消息、不调用翻译API、不改正式剧本。只允许已登记的locale和安全scriptId文件名。

## 样例与验收

`Scripts/vn_localization_sample.json`与ja/en同名译文各提供：岁月三句逐句配对、故意缺译条目、翻译过的选择提示/选项。只是独立展示样例，不改序章剧情、不表示全篇翻译完成。可在VNManager的测试入口加载该scriptId；非默认开局。

Unity检查入口`VNLocalizationReview.Batch`（旧`VNJapaneseSubtitleReview.Batch`委托到此入口）仅允许在隔离项目运行。输出`tools/vn/localization_review`，检查语言注册、源哈希失效、分段校验、80次命中/缺文件重复请求无新Resources查找、生产管理器三组字幕、字号/颜色/居中/零句间距、缺译回退、英文选择项及实际语言设置回调。

最终`localization_20261103_centered.log` exit 0，`VN_LOCALIZATION_REVIEW_OK`，任务异常0；194个运行时源码及2个Editor检查源码编译通过，196份源哈希、4份语言/样例JSON及实际隔离程序集一致。Windows2560×1440/1280×720、平板2560×1600/2048×1536四画幅通过，含短句自然同行、长段落限高滚动、滚动输入抑制误推进、一次跳字单次完成、中日/英日组合和真实设置控件回调。实际查看2.5K的18/20副字号对照与最终居中截图、4:3平板截图；默认采用18/32。

证据冻结`tools/vn/localization_acceptance_20261103`，含最终结果/截图/全部本轮日志、编译输入/资源哈希及翻译校验结果。Python校验拒绝七类无效回传，十章ja/en模板分别验证2466条源单元、ready=0；中日英样例每种译文4个ready、2个故意缺译单元。首次流排尝试因脱离Panel的文字测量失败，日志保留；已将运行测量移到附着元素后并修正设置预览构建，最终通过。已知Editor SearchDatabase启动索引异常完整堆栈单列`editorDiagnostics`，不混作任务异常或删掉原诊断。

以上是编辑器开发/回归检查，不等于安卓平板真机、Windows Player、全篇翻译/术语审校或完整发行验收。回顾列表与姓名牌尚未多语言化。
