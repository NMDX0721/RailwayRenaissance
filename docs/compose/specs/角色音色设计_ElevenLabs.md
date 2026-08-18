# 铁路复兴：沙能冲击 — 角色音色设计（ElevenLabs Voice Design）

> 工具：ElevenLabs Voice Design（v3，文本提示生成音色，兼容 Eleven v3 + audio tags）
> 文档依据：ElevenLabs 官方《Voice Design Prompting Guide》
> 使用方式：ElevenLabs → Voice Lab → Voice Design → 粘贴【Prompt】→ 粘贴【Preview Text】→ 调整 Guidance Scale → 生成 3 个候选 → 选型保存 `voice_{角色id}`

---

## 🔊 全局语言策略（最终方案）

**配音语言：全韩语。** 游戏内所有角色统一用韩语配音，中文字幕 + 英文字幕分别发布。

**理由：**
- 主角林彪悍主要语言是韩语（金日成综合大学深造，平壤日常）
- 统一语言让玩家进入"朝鲜语境"的沉浸感，不因同框对话语码切换而出戏
- 未来支持英文时，多语言字幕翻译即可，配音管线不变

**角色音色底色**：底层的"中国人民间温暖""东北爽朗"等保留在 Prompt 的 Persona/Emotion/描述中，作为音色性格特征，不改变语言。

---

## 一、岁月（Suiyue）— AI 伙伴

**角色定位**：0721号沙子飞猪号搭载的 AI 原型，2053 年制造，沉睡 23 年（至 2076）。冷静精准、偶尔冷幽默，对情感只有理论理解，隐藏着"23 年无人对话"的孤独。

**【Prompt】**
```
Native Korean, standard Pyongyang dialect. Female, 20–25. Excellent audio quality.
Persona: in-car AI assistant. Emotion: calm, precise, faintly lonely.
Smooth, clean timbre with a subtle processed resonance, like an old navigation system gracefully restored. Speaks at a slow, deliberate natural pace, pausing briefly before emotional words as if consulting a database. Dry humor delivered perfectly straight, never breaking character.
```

**【Preview Text】（韩语长文本，配合冷静+孤独感）**
```
심박수 백사십칠을 기록했습니다. 당신은 멈추지 않았어요. 제가 잠들어 있던 스물세 해 동안, 아무도 저와 이야기하지 않았습니다. 오늘, 처음으로 대화 상대가 생겼네요. 저는 AI입니다. 위로라는 걸 잘 몰라요. 하지만 철길은 결코 사라지지 않는다는 것, 그것만은 알고 있습니다. 다음 보급 지점까지 한 시간 반 남았어요. 그동안 조금 쉬시겠어요?
```

**Guidance Scale：** 38%

---

## 二、林彪悍（Lin Biaohan）— 主角

**角色定位**：25 岁，金日成综合大学荣誉研究生（智能调度方向）。在金日成大学的数年学术生涯里，韩语已成为他的主要沟通语言。倔强克制、冷静坚定——不是莽撞少年，是"知道自己选了更难的路"的年轻男人。

**【Prompt】**
```
Native Korean, standard Pyongyang dialect with a subtle non-native warmth — a Chinese-Korean bilingual who has spent years in Pyongyang academia. Male, 25–28. Excellent audio quality.
Persona: quiet young stationkeeper. Emotion: restrained, determined, weary.
Low, settled timbre, slightly deeper than his age suggests. Speaks at a calm, measured natural pace with quiet intensity — frustration surfaces as throat tension, clipped, never shouting; when gentle, drops to a near-whisper. Educated, grounded, bilingual in Korean and Chinese.
```

**【Preview Text】（韩语长文本，配合克制+决心）**
```
오늘은 정말 길었어. 검문소에서 네 대에 둘러싸였을 때, 손이 떨리고 있었지. 할아버지가 남긴 이 철길… 이십삼 년 동안 끊겨 있던 그 길을, 내가 다시 이을 수 있을까. 모르겠어. 하지만 돌아가기로 했어. 난 이제 더 이상, 도망치고 싶지 않아. 이 철길은 내가 다시 일으킬 거야. 서월아, 가자.
```

**Guidance Scale：** 36%

---

## 三、老陈（Lao Chen）— 末代站长

**角色定位**：68 岁，雾峰村最后一任站长，主角的导师。温暖朴实、固执善良，读着林悍的遗愿独自守线四年，苍老里藏着欣慰的泪光。中国边境村庄的老人家，在韩语语境下用韩语表达。

**【Prompt】**
```
Native Korean, with a warm northern Chinese rural undertone softening the edges — a village elder who has lived near the border. Male, 65–70. Ok quality.
Persona: weathered village stationmaster. Emotion: warm, wistful, stubbornly hopeful.
Rough but gentle timbre, gravel wrapped in cotton; resonant chest voice softened by age. Speaks at a slow, drawn-out natural pace, pausing to cough or sigh. Fondness seeps in when recalling old days; a quiet heaviness when speaking of the closed line. Joy cracks the voice into reluctant smiles.
```

**【Preview Text】（韩语长文本，配合温暖+感伤）**
```
옛날에는… 이 철길이 하루에 두 번 갔어요. 역마다 다 정차했지. 차를 실어 나르는 사람, 광산 일하는 사람… 정말 북적였어요. 지금은… 보세요, 플랫폼에 풀까지 자라났지. 임한이 계셨다면 분명 또 중얼거리셨을 거야. 그분 참, 입으로는 "철길이 남아 있으니, 언젠가 누군가는 올 거야" 하면서도, 마음은 누구보다 조급했었지. 표호야… 돌아와 줘서 고맙다. 이 철길은 아직… 너를 기다리고 있어.
```

**Guidance Scale：** 34%

---

## 四、嘉颖徐（Jiaying Xu）— 铁路大亨

**角色定位**：46 岁，东北亚铁路大亨，手持多条电气化干线。爽朗、务实、有远见，与林悍有旧交，欣赏有勇气的人，赠黑金卡资助林彪悍。

**【Prompt】**
```
Native Korean, Seoul-influenced business register (international businesswoman). Female, 40–50. Excellent audio quality.
Persona: savvy railway tycoon. Emotion: confident, generous, passionate.
Clear, authoritative timbre with warmth beneath the business polish; a genuine spark when speaking of railways. Speaks at an unhurried steady natural pace, generous with pauses; laughs easily but briefly. When advising, tone shifts to near-motherly gentleness. Decisive, direct, no hedging.
```

**【Preview Text】（韩语长文本，配合自信+欣赏）**
```
당신이 임한의 손자라니. 그분이 당신 이야기를 하실 때면, 아직 고등학생이었죠. 그분이 지키신 그 철길은, 마지막까지 남아 있던 민영 노선 중 하나였어요. 그분이 떠나신 후로도 4년을 버틴 것만으로도, 이미 기적이에요. 이 카드를 받으세요. 매달 만 원의 한도로, 노선 초기 복구에 사용하시면 돼요. 선물이 아니에요. 빌려드리는 거예요. 수익이 나면, 원금과 이자를 갚으세요. 만약 잃게 된다면… 그건 제가 임한에게 보내는 예의라고 생각할게요. 당신의 성과를 기대하겠습니다.
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

Voice Design 生成音色后，用 **Multilingual v3** 模型输出韩语台词，wav 文件直接复用，无需重复配音。

---

## 七、生成与测试流程

1. **逐角色**粘贴 Prompt + Preview Text → Guidance Scale 调至标称值 → 点 Generate（3 候选）
2. **稳定性测试**：同一音色跑 5 句韩语 + 2 句中文，确认跨语言一致
3. **情感测试**：用 audio tags（`[whispers]` `[calm]` `[dry humor]`）测情感张力
4. **选型**：每角色保留 2 候选 → 游戏内试听 → 定稿，命名 `voice_{角色id}` 存入 Voice Library
5. **效果不满意时**：优先调 Guidance Scale（音色准确→提高 / 音质→降低），其次改 Emotion 形容词，最后改 Persona