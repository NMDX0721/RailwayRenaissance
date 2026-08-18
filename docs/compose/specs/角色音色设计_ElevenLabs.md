# 铁路复兴：沙能冲击 — 角色音色设计（ElevenLabs Voice Design）

> 工具：ElevenLabs Voice Design（v3，文本提示生成音色，兼容 Eleven v3 + audio tags）
> 文档依据：ElevenLabs 官方《Voice Design Prompting Guide》
> 使用方式：ElevenLabs → Voice Lab → Voice Design → 粘贴【Prompt】→ 粘贴【Preview Text】→ 调整 Guidance Scale → 生成 3 个候选 → 选型保存 `voice_{角色id}`

---

## 🔊 全局语言策略

**配音语言：全韩语。** 所有角色统一韩语配音，中文字幕 + 英文字幕分别发布。

**理由：** 主角林彪悍主要语言是韩语（金日成综合大学深造）；统一语言避免同框对话语码切换的割裂感；多语言字幕翻译即可，配音管线不变。

**角色音色底色**：底层文化背景（老陈的"乡村温暖"、张工的"东北爽朗"）保留在 Persona/Emotion/描述中，作为性格特征，不改变语言。

---

## 一、岁月（Suiyue）— AI 伙伴

**角色定位**：0721号搭载的 AI 原型，2053 年制造，沉睡 23 年至 2076。冷静精准、偶尔冷幽默，对情感只有理论理解，藏着"23 年无人对话"的孤独。

**【Prompt】**
```
Native Korean, standard Pyongyang dialect. Female, 20–25. Excellent audio quality.
Persona: in-car AI assistant. Emotion: calm, precise, faintly lonely.
Smooth, clean timbre with a subtle processed resonance, like an old navigation system gracefully restored. Speaks at a slow, deliberate natural pace, pausing briefly before emotional words as if consulting a database. Dry humor delivered perfectly straight, never breaking character.
```

**【Preview Text】（韩语，含音频标签，覆盖冷静→孤独→幽默→收尾）**
```
[calm] 심박수 백사십칠을 기록했습니다. 당신은 멈추지 않았어요.
[pensive] 제가 잠들어 있던 스물세 해 동안, 아무도 저와 이야기하지 않았습니다. 오늘, 처음으로 대화 상대가 생겼네요.
[dry] 저는 AI입니다. 위로라는 걸 잘 몰라요. 하지만 철길은 결코 사라지지 않는다는 것, 그것만은 알고 있습니다.
[gentle] 다음 보급 지점까지 한 시간 반 남았어요. 그동안 조금 쉬시겠어요?
```

**Guidance Scale：** 38%

---

## 二、林彪悍（Lin Biaohan）— 主角

**角色定位**：25 岁，金日成综合大学荣誉研究生（智能调度）。韩语为主要沟通语言。倔强克制、冷静坚定——不是莽撞少年，是"知道自己选了更难的路"的年轻男人。

**【Prompt】**
```
Native Korean, standard Pyongyang dialect with a subtle non-native warmth — a Chinese-Korean bilingual who has spent years in Pyongyang academia. Male, 25–28. Excellent audio quality.
Persona: quiet young stationkeeper. Emotion: restrained, determined, weary.
Low, settled timbre, slightly deeper than his age suggests. Speaks at a calm, measured natural pace with quiet intensity — frustration surfaces as throat tension, clipped, never shouting; when gentle, drops to a near-whisper. Educated, grounded, bilingual in Korean and Chinese.
```

**【Preview Text】（韩语，含音频标签，覆盖疲惫→坚定→愤怒→温柔）**
```
[tired] 오늘은 정말 길었어. 검문소에서 네 대에 둘러싸였을 때, 손이 떨리고 있었지.
[pensive] 할아버지가 남긴 이 철길… 이십삼 년 동안 끊겨 있던 그 길을, 내가 다시 이을 수 있을까.
[determined] 하지만 돌아가기로 했어. 난 이제 더 이상 도망치고 싶지 않아. 이 철길은 내가 다시 일으킬 거야.
[sharp] 나는 네 의견을 묻지 않았어! 가속해!
[gentle] 서월아, 가자.
```

**Guidance Scale：** 36%

---

## 三、老陈（Lao Chen）— 末代站长

**角色定位**：68 岁，雾峰村末代站长，主角导师。温暖朴实、固执善良，守线四年，苍老里藏着欣慰的泪光。

**【Prompt】**
```
Native Korean, with a warm northern Chinese rural undertone softening the edges — a village elder who has lived near the border. Male, 65–70. Ok quality.
Persona: weathered village stationmaster. Emotion: warm, wistful, stubbornly hopeful.
Rough but gentle timbre, gravel wrapped in cotton; resonant chest voice softened by age. Speaks at a slow, drawn-out natural pace, pausing to cough or sigh. Fondness seeps in when recalling old days; a quiet heaviness when speaking of the closed line. Joy cracks the voice into reluctant smiles.
```

**【Preview Text】（韩语，含音频标签，覆盖回忆→感伤→温暖）**
```
[warm] 옛날에는… 이 철길이 하루에 두 번 갔어요. 역마다 다 정차했지. 차를 실어 나르는 사람, 광산 일하는 사람… 정말 북적였어요.
[sigh] 지금은… 보세요, 플랫폼에 풀까지 자라났지.
[gentle] 표호야… 돌아와 줘서 고맙다. 이 철길은 아직… 너를 기다리고 있어.
```

**Guidance Scale：** 34%

---

## 四、嘉颖徐（Jiaying Xu）— 铁路大亨

**角色定位**：24 岁，东北亚铁路大亨，手持多条电气化干线。爷爷林悍晚年的忘年交——19 岁祖父去世那年她已是圈内人。青春外表下藏着远超年龄的笃定，爽朗务实、有远见，赠黑金卡资助林彪悍。

**【Prompt】**
```
Native Korean, Seoul-influenced business register (international businesswoman). Female, 20–25. Excellent audio quality.
Persona: young railway tycoon. Emotion: confident, generous, sharp.
Youthful but settled timbre — bright and clear, a voice that sounds like it belongs to a 24-year-old yet carries the authority of someone who closed her first deal at nineteen. Speaks at a brisk, decisive natural pace; laughs easily and openly, then shifts on a dime to business precision. When advising, a peer-to-peer candor, never motherly — she respects the listener as an equal. Decisive, direct, no hedging.
```

**【Preview Text】（韩语，含音频标签，覆盖自信→回忆→认真→鼓励）**
```
[confident] 당신이 임한의 손자라니. 그분이 당신 이야기를 하실 때면, 아직 고등학생이었죠.
[warm] 그분이 지키신 그 철길은, 마지막까지 남아 있던 민영 노선 중 하나였어요. 그분이 떠나신 후로도 4년을 버틴 것만으로도, 이미 기적이에요.
[serious] 이 카드를 받으세요. 선물이 아니에요. 빌려드리는 거예요. 수익이 나면, 원금과 이자를 갚으세요. 만약 잃게 된다면… 그건 제가 임한에게 보내는 예의라고 생각할게요.
[encouraging] 당신의 성과를 기대하겠습니다. 우리 세대가, 이 철길을 다시 달리게 해요.
```

**Guidance Scale：** 35%

---

## 五、配角音色速查

| 角色 | 年龄 | 语言设定 | Persona | Emotion | 语音特征 |
|------|------|---------|---------|---------|---------|
| **张工** | 62 | 韩语（中国东北底蕴） | jovial old mechanic | boisterous, warm | 大嗓门，笑声贯穿，东北人底色的爽朗通过韩语表达 |
| **李阿姨** | 55 | 韩语 | warm station auntie | fussy, kind | 温暖絮叨，尾音上扬，像照顾人的阿姨 |
| **赵师傅** | 55 | 韩语 | steady track worker | taciturn, reliable | 寡言节省，每句有分量，老铁路人笃定 |
| **王小弟** | 22 | 韩语 | eager young recruit | excitable, earnest | 语速快，兴奋破音，带年轻人毛躁 |
| **小芳** | 45 | 韩语 | gentle volunteer | patient, soft | 句尾轻放，像哄人说话 |
| **检查员** | 35 | 韩语 | border inspector | clipped, official | 官方腔，命令式短句，无个人情绪 |

---

## 六、多语言发布说明

配音管线统一输出**韩语 wav**。不同语言版本通过字幕切换实现：

| 版本 | 配音 | 字幕 | 处理方式 |
|------|------|------|---------|
| **中文版** | 韩语 | 中文字幕 | 台词表内嵌中文翻译 → 字幕文件 |
| **英文版** | 韩语 | 英文字幕 | 台词表翻译英文 → 字幕文件 |

Voice Design 生成音色后，用 **Multilingual v3** 模型输出韩语台词，wav 文件直接复用。

---

## 七、生成与测试流程

1. **逐角色**粘贴 Prompt + Preview Text → Guidance Scale 调至标称值 → 点 Generate（3 候选）
2. **稳定性测试**：同一音色跑 5 句韩语 + 2 句中文，确认跨语言一致
3. **情感测试**：Preview Text 已内嵌 `[calm]` `[tired]` `[sharp]` `[gentle]` 等标签，生成后逐段回放确认情感区分度
4. **选型**：每角色保留 2 候选 → 游戏内试听 → 定稿，命名 `voice_{角色id}` 存入 Voice Library
5. **效果不满意时**：优先调 Guidance Scale（音色准确→提高 / 音质→降低），其次改 Emotion 形容词，最后改 Persona