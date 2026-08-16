# Wiki 内容补充计划 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 补齐 Wiki 缺失的 27 条目（54 个中英页面），修复全部死链、回链与图片问题，使 Home.md 与中文维基.md 所有链接可解析。

**Architecture:** 三批并行子代理分别产出角色页/城市+势力页/系统+世界+载具+剧情页，主代理收尾统一修链接与图片。所有页面数据以参考资料为唯一来源，遵循统一模板规范，不编造设定。

**Tech Stack:** Markdown（GitHub Wiki / GFM）、wikitable 信息框（风格兼容）、HTML `<img>` 标签

## Global Constraints

1. 页面文件存放：`D:\Unity Project\RailwayRenaissance\中转站\wiki\`，图片在 `images/` 子目录
2. 页面数据必须来自 spec 中列明的参考资料文件，**禁止编造任何设定数值**
3. 信息框格式：沿用现有页面风格，无立绘条目用纯文字 wikitable 信息框，不配图
4. 中英双版：中文页链接到中文维基，英文页链接到 Home
5. 底部回链格式：中文页 `[返回XXX](中文维基)`；英文页 `[Back to XXX](Home)`
6. 文件名必须与 spec S3 表格中的列名完全一致（ASCII 文件名用于英文页，中文文件名用于中文页）
7. 系统页速览式：定位→核心机制表→主要数值→联动→详见原文链接，不搬运公式细节
8. 不修改任何现有 26 页，除 spec S5 明确列出的修改外
9. 图片引用路径统一 `images/xxx.png` 相对路径

---

### Task 1: 批 1 — 角色页（王小弟/赵铁山/陈鹤年/林悍 中英 8 页）

**Covers:** [S3.1]

**Files:**
- Create: `中转站/wiki/王小弟.md`, `中转站/wiki/Wang-Xiaodi.md`
- Create: `中转站/wiki/赵铁山.md`, `中转站/wiki/Zhao-Tieshan.md`
- Create: `中转站/wiki/陈鹤年.md`, `中转站/wiki/Chen-Henian.md`
- Create: `中转站/wiki/林悍.md`, `中转站/wiki/Lin-Han.md`

**Interfaces:**
- Consumes: spec S3.1 角色清单、参考资料/角色设定.md、参考资料/世界观扩展设定.md
- Produces: 8 个角色页文件；王小弟页需补充进两个索引（由 Task 4 完成）

- [ ] **Step 1: 派发子代理（batch1-characters）**

用 actor 工具 spawn，subagent_type=general，模型 ds0718。Prompt 必须包含：
- 项目路径与 wiki 路径
- 阅读 `参考资料/角色设定.md` §3.4（王小弟技能属性）、§六（林悍）与 `参考资料/世界观扩展设定.md` §4.5（林悍担保事迹）、§1.5.6（陈鹤年与周鼎铭关系）
- 赵铁山：只存在于 Home/中文维基索引（铁路监督员，《铁路委托运营条例》执行者，见 `参考资料/世界观扩展设定.md` §5.3），无独立设定文档——按现有线索撰写（身份/职责/与条例的关系），标注"信息待补充"
- 页面结构：wikitable 文字信息框（不配图）+ 简介 + 能力/关系表 + 剧情档案 + 底部回链
- 王小弟信息框：年龄 22 / 身份 刚毕业大学生 / 性格 阳光热血 / 技能：驾驶1级(潜力5级) / 维修0级(潜力4级) / 管理0级(潜力3级) / 服务1级(潜力3级)
- 王小弟英文页文件名 Wang-Xiaodi.md；林悍英文页 Lin-Han.md；赵铁山 Zhao-Tieshan.md；陈鹤年 Chen-Henian.md

- [ ] **Step 2: 验收批 1**

主代理检查：
- 8 个文件全部存在，文件名正确
- 王小弟技能数值与角色设定.md §3.4 一致
- 无立绘、无编造素材、无编造数值
- 底部回链正确（中文→中文维基，英文→Home）

- [ ] **Step 3: 提交（可选，等待用户同意）**

```bash
git add "中转站/wiki/王小弟.md" "中转站/wiki/Wang-Xiaodi.md" "中转站/wiki/赵铁山.md" "中转站/wiki/Zhao-Tieshan.md" "中转站/wiki/陈鹤年.md" "中转站/wiki/Chen-Henian.md" "中转站/wiki/林悍.md" "中转站/wiki/Lin-Han.md"
git commit -m "docs(wiki): add missing character pages (Wang Xiaodi, Zhao Tieshan, Chen Henian, Lin Han)"
```

---

### Task 2: 批 2 — 城市 5 + 势力 2（中英 14 页）

**Covers:** [S3.2, S3.3]

**Files:**
- Create: `中转站/wiki/青溪镇.md`, `中转站/wiki/Clear-Stream-Town.md`
- Create: `中转站/wiki/云渡港.md`, `中转站/wiki/Cloud-Ferry-Port.md`
- Create: `中转站/wiki/白鹭洲.md`, `中转站/wiki/White-Egret-Islet.md`
- Create: `中转站/wiki/枫林渡.md`, `中转站/wiki/Maple-Forest-Crossing.md`
- Create: `中转站/wiki/望海港.md`, `中转站/wiki/Sea-View-Harbor.md`
- Create: `中转站/wiki/白头山动力总会社.md`, `中转站/wiki/Baekdu-Mountain-Power.md`
- Create: `中转站/wiki/国际铁路遗产保护基金会.md`, `中转站/wiki/Railway-Heritage-Foundation.md`

**Interfaces:**
- Consumes: spec S3.2/S3.3、`Assets/Resources/Seeds/seed_*.json`（城市原始数据）、参考资料/世界观扩展设定.md §1.2/§2
- Produces: 14 个页面文件

- [ ] **Step 1: 派发子代理（batch2-cities-factions）**

actor spawn，general，ds0718。Prompt 必须包含：
- 城市数据来源：读取 `Assets/Resources/Seeds/` 下所有 seed JSON，提取 5 城（青溪镇/云渡港/白鹭洲/枫林渡/望海港）的：类型/人口/距离/区域/沙能渗透/政治倾向；若 JSON 无该城则读取 `docs/compose/specs/千里马创世核.md` 确认城市架构，数据缺失的城市写"信息待补充"
- 城市页模板参照现有 `雾峰村.md`：img 仅当 images/ 有对应图时使用，否则纯文字数据表
- 白头山动力总会社数据：世界观扩展设定.md §1.2（登记49%、实控、先军国防公社/沙能科学研究院/沙能保障总局、三份协议、命名解释）
- 国际铁路遗产保护基金会数据：世界观扩展设定.md §2（公开壳、铁路抢救小组/遗产保护巡回展/志愿者招募、伪造遗产档案 §2.9）
- 势力页模板参照现有 `USET（联合沙能科技）.md`：概览表 + 结构图 + 游戏内角色
- 底部回链：中文→`[返回势力图鉴](中文维基)`/`[返回城市图鉴](中文维基)`；英文→`[Back to Factions](Home)`/`[Back to Locations](Home)`

- [ ] **Step 2: 验收批 2**

主代理检查：
- 14 个文件全部存在
- 城市数据与 seed JSON 保持一致（抽查 2 城）
- 势力页数据与世界观扩展设定.md §1.2/§2 一致
- 无编造

---

### Task 3: 批 3 — 系统 5 + 世界 4 + 载具 3 + 剧情 4（中英 32 页）

**Covers:** [S3.4, S3.5, S3.6, S3.7]

**Files:**
- Create: `中转站/wiki/沙本位制.md`, `中转站/wiki/Sand-Standard-Currency.md`
- Create: `中转站/wiki/铁路大废线.md`, `中转站/wiki/Great-Railway-Abandonment.md`
- Create: `中转站/wiki/铁路委托运营条例.md`, `中转站/wiki/Railway-Delegation-Act.md`
- Create: `中转站/wiki/五条趋势线.md`, `中转站/wiki/Five-Trendlines.md`
- Create: `中转站/wiki/沙子飞猪号.md`, `中转站/wiki/Sand-Flying-Pig-0721.md`
- Create: `中转站/wiki/NF-5耕牛.md`, `中转站/wiki/NF-5-Gengniu.md`
- Create: `中转站/wiki/沙驴号.md`, `中转站/wiki/Sand-Donkey.md`
- Create: `中转站/wiki/沙本位经济核.md`, `中转站/wiki/Sand-Standard-Economy.md`
- Create: `中转站/wiki/千里马创世核.md`, `中转站/wiki/Chollima-Genesis-Core.md`
- Create: `中转站/wiki/岁月叙事引擎.md`, `中转站/wiki/Suiyue-Narrative-Engine.md`
- Create: `中转站/wiki/先民人事系统.md`, `中转站/wiki/Seonmin-Personnel-System.md`
- Create: `中转站/wiki/铁龙竞争系统.md`, `中转站/wiki/Iron-Dragon-Competition.md`
- Create: `中转站/wiki/序章-归乡.md`, `中转站/wiki/Prologue-Homecoming.md`
- Create: `中转站/wiki/大学篇.md`, `中转站/wiki/University-Chapters.md`
- Create: `中转站/wiki/旅途篇.md`, `中转站/wiki/Journey-Chapters.md`
- Create: `中转站/wiki/雾峰篇.md`, `中转站/wiki/Wufeng-Chapters.md`

**Interfaces:**
- Consumes: spec S3.4-S3.7、参考资料/世界观扩展设定.md §3/§5、参考资料/世界观与车辆设定.md §2/§4、参考资料/沙本位经济核.md、docs/compose/specs/千里马创世核.md、docs/compose/specs/岁月叙事引擎.md、docs/compose/specs/先民人事系统.md、docs/compose/specs/铁龙竞争系统.md、docs/compose/specs/核心玩法循环.md、参考资料/序章剧本_归乡.md、docs/compose/specs/序章后续剧情设计.md
- Produces: 32 个页面文件

- [ ] **Step 1: 派发子代理（batch3-systems-world）**

actor spawn，general，ds0718。Prompt 必须包含：
- 系统页 5 个采用速览式：定位→核心机制表→主要数值→联动→`详见 docs/compose/specs/xxx.md` 原文链接
- 世界页 4 个：沙本位制（世界观扩展 §3：沙币=10kg标准工业沙、SB-62 国标、提炼权配额、朝鲜人民银行发行、万沙）；铁路大废线（世界观与车辆设定 §一：2062 二百支线/2068 91国/2072 停运）；铁路委托运营条例（世界观扩展 §5：七条关键条款）；五条趋势线（核心玩法循环.md 内容）
- 载具页 3 个：沙子飞猪号（速度/载客2人/补给1500kg/续航1000km/0721号岁月载体）；NF-5耕牛（柴油机2000kW/最高80km/h/整备135t/初始状态70）；沙驴号（地面通勤60km/h/4人/售价为飞猪1/5）
- 剧情页 4 个：序章-归乡（剧本结构概览）；大学篇/旅途篇/雾峰篇（序章后续剧情设计.md 起名与结构，不含剧透细节，以章节名+内容摘要呈现）
- 底部回链：中文→`[返回世界百科](中文维基)`/`[返回载具图鉴](中文维基)`/`[返回系统设定](中文维基)`/`[返回剧情档案](中文维基)`；英文→`[Back to Lore](Home)`/`[Back to Vehicles](Home)`/`[Back to Systems](Home)`/`[Back to Story](Home)`
- 明确告知：所有系统页**不配图**、不搬运大段公式

- [ ] **Step 2: 验收批 3**

主代理检查：
- 32 个文件全部存在，文件名与 spec S3 一致
- 系统页结构符合速览式（有"详见"原文链接）
- 载具数值与世界观与车辆设定.md 一致（抽查 2 项）
- 无编造、无大段公式搬运

---

### Task 4: 链接与图片修复（C/D 类）

**Covers:** [S5]

**Files:**
- Create: `中转站/wiki/Wufeng-Village.md`, `Black-Gold-Ridge.md`, `Sand-Energy-Technology.md`, `Timeline.md`, `USET.md`, `Iron-Dragon-Project.md`（6 个英文镜像页，翻译现有中文页内容）
- Modify: `中转站/wiki/Home.md`（英文链接 8 处 + 新增索引）
- Modify: `中转站/wiki/中文维基.md`（新增索引）
- Modify: `中转站/wiki/Lin-Biaohan.md`, `Old-Chen.md`, `Suiyue.md`, `Zhou-Dingming.md`, `Zhang-Gong.md`, `Li-Ayi.md`, `Zhao-Shifu.md`, `Xiao-Fang.md` 底部回链（Home-EN→Home）
- Modify: `中转站/wiki/雾峰村.md`（station.png→lab.jpg）
- Delete: `中转站/wiki/images/chen_sheet.png`（与 chen_portrait.png 同字节）
- Delete: `中转站/wiki/images/lin_card.png`（与 lin_portrait.png 同字节）

**Interfaces:**
- Consumes: Task 1-3 创建的全部新页面文件名
- Produces: 全站链接可解析状态

- [ ] **Step 1: 创建 6 个英文镜像页**

基于现有中文页翻译创建（内容对应，不新增设定）：
- `Wufeng-Village.md` ← 雾峰村.md
- `Black-Gold-Ridge.md` ← 乌金岭.md
- `Sand-Energy-Technology.md` ← 沙能技术.md
- `Timeline.md` ← 时间线.md
- `USET.md` ← USET（联合沙能科技）.md
- `Iron-Dragon-Project.md` ← 铁龙计划.md
- 英文页底部回链：`*[Back to Home](Home)`
- 信息框/图片沿用原中文页（保留 lab.jpg 等既有引用）

- [ ] **Step 2: 修改 Home.md 英文链接**

将 8 处指中文文件名的链接改为英文页链接：
- `[Wufeng Village](Wufeng-Village)` → 目标 `Wufeng-Village.md`（本任务 Step 1 已创建）
- `[Black Gold Ridge](Black-Gold-Ridge)` → `Black-Gold-Ridge.md`
- `[Sand Energy Technology](Sand-Energy-Technology)` → `Sand-Energy-Technology.md`
- `[Timeline](Timeline)` → `Timeline.md`
- `[USET (United Sand Energy Technology)](USET)` → `USET.md`
- `[Iron Dragon Project](Iron-Dragon-Project)` → `Iron-Dragon-Project.md`
- `Kim Il Sung University` → 保留 `金日成综合大学` 中文链接（无英文镜像，索引语义正确）
- 语法：`[显示名](文件BaseName)`
- 同时将 S3 新建条目（角色/城市/势力/世界/载具/系统/剧情）补入 Home.md 对应分区

- [ ] **Step 3: 修改 8 个英文角色页回链**

`*[Back to Character Index](Home-EN)` → `*[Back to Character Index](Home)`

- [ ] **Step 4: 修改雾峰村.md 图片**

`<img src="images/station.png"` → `<img src="images/lab.jpg"`

- [ ] **Step 5: 删除重复大图**

```powershell
Add-Type -AssemblyName Microsoft.VisualBasic
[Microsoft.VisualBasic.FileIO.FileSystem]::DeleteFile("D:\Unity Project\RailwayRenaissance\中转站\wiki\images\chen_sheet.png", 'OnlyErrorDialogs', 'SendToRecycleBin')
[Microsoft.VisualBasic.FileIO.FileSystem]::DeleteFile("D:\Unity Project\RailwayRenaissance\中转站\wiki\images\lin_card.png", 'OnlyErrorDialogs', 'SendToRecycleBin')
```
> 项目规则：删除走回收站，不永久删除。

- [ ] **Step 6: 更新中文维基索引**

在中文维基.md 相应分类下补入 Task 1-3 创建的全部新条目链接（角色/势力/城市/世界/载具/系统/剧情）。

---

### Task 5: 全站死链扫描与验证

**Covers:** [S6.2, S7]

**Files:**
- None（只读验证）

- [ ] **Step 1: 遍历扫描所有页面链接**

写临时 PowerShell/Python 脚本：解析 `中转站/wiki/*.md` 中所有 `[...](xxx)` 与 `<img src="images/xxx">`，验证目标文件存在于 wiki 目录或 images/ 目录。输出所有断链清单。

- [ ] **Step 2: 图片重复校验**

对比 images/ 下所有文件字节数，输出同字节文件对。确认仅存的重复大图为 0 对。

- [ ] **Step 3: 修复扫描发现的残余断链**

对 Step 1 发现的断链逐条修复（重定向到正确页面或建缺失页）。

- [ ] **Step 4: 完成标准核对**

对照 spec S7 六项逐一验证：
1. 27 条目 54 新页创建完毕 ✅/❌
2. 全部链接可解析 ✅/❌
3. 无 Home-EN 残链接 ✅/❌
4. station.png 引用已替换 ✅/❌
5. 两索引已补新条目 ✅/❌
6. 原 26 页无意外改动 ✅/❌

---

## Self-Review

- **Spec coverage:** S3.1→Task1；S3.2/S3.3→Task2；S3.4-S3.7→Task3；S5→Task4；S6.2/S7→Task5；S6.1（三批并行）贯穿 Task1-3。全部 [Sn] 均有覆盖。
- **Placeholder scan:** 无 TBD/TODO；城市数据"待补充"为 spec 已明确的合法占位（seed JSON 无该城时）。
- **Type consistency:** 页面文件名三处引用（spec S3 / Task Files / Home 链接）已逐一核对一致；回链格式规范统一。

*计划文档生成完毕，待执行。*