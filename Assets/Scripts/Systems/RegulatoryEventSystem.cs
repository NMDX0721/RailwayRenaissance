using System;
using System.Collections.Generic;
using UnityEngine;

namespace RailwayRenaissance.Systems
{
    /// <summary>
    /// [T308] 监管事件程序系统（双层模型状态机 + 现实程序多日流水）。
    /// 严禁档位按当日数值瞬变直切，一切升档降档必须通过现实公文与法定程序推进。
    /// 现实依据：
    /// 1. 《中华人民共和国安全生产法》第六十五条、第一百一十三条（重大事故隐患挂牌督办、责令停产停业整顿）；
    /// 2. 交通运输部《交通运输安全生产警示约谈和挂牌督办办法》第三条至第十八条（警示、约谈、挂牌督办与摘牌销号程序）；
    /// 3. 《中华人民共和国企业破产法》第七十条、第七十一条（重整申请、法院裁定受理与重整投资款注入）；
    /// 4. 《铁路安全管理条例》与《铁路轨道检查车技术管理规程》（工务定期检定与等级评定）。
    /// </summary>
    public static class RegulatoryEventSystem
    {
        // ================= 状态持久化数据结构 =================
        [Serializable]
        public class ActiveProcedure
        {
            public string chainType;       // A_Check, B_Media, C_Accident, D_Blacklist, E_Delist, F_Annual, G_Crisis, Bankruptcy, SandM_A
            public int startDay;           // 发起天
            public int currentStep;        // 当前步骤
            public int daysRemaining;      // 距离下一步天数
            public int targetTier;         // 目标档位
            public string docNumber;       // 文号
        }

        [Serializable]
        public class RegulatorySaveContainer
        {
            public int politicalTier;
            public int decayGrade;
            public int bankruptcyCount;
            public int sandStage;
            public int daysInExtremePressure;
            public int daysSinceLastInspection;
            public List<ActiveProcedure> activeChains = new List<ActiveProcedure>();
        }

        // ================= 运行时状态 =================
        private static List<ActiveProcedure> activeChains = new List<ActiveProcedure>();
        private static int daysInExtremePressure = 0;
        private static int daysSinceLastInspection = 0;
        private static int sandStage = 0; // 0:正常, 1:预警通报已发, 2:交割中, 3:已交割控制

        // 现实文号序列号计数器
        private static int checkDocSeq = 11;
        private static int courtDocSeq = 4;
        private static int safetyNoticeSeq = 7;

        public static void Initialize()
        {
            activeChains.Clear();
            daysInExtremePressure = 0;
            daysSinceLastInspection = 0;
            sandStage = 0;
            LoadState();
        }

        // ================= 每日结算流水驱动 =================
        /// <summary>
        /// 在 AdvanceDay 结算调用，推进所有在途法定程序并检测新事件源。
        /// 档位只在程序节点日迁移，严禁根据数值瞬变切档。
        /// </summary>
        public static void AdvanceDay(List<string> notices)
        {
            float p = GameData.PoliticalPressure;
            int currentTier = GameData.PoliticalTier;

            // 1. 推进现有在途程序
            AdvanceInFlightChains(notices, p);

            // 2. 检测新的程序触发源（若无冲突同类在途）
            DetectNewProcedures(notices, p, currentTier);

            // 3. 极高压危机倒计时（≥0.95 连续 7 天未回落）
            CheckCrisisPressure(notices, p);

            // 4. 工务检定制定期推进（8.4 规定 30 天春秋检重算）
            AdvanceInfrastructureInspection(notices);

            // 5. 保存状态
            SaveState();
        }

        // ================= 推进在途程序 =================
        private static void AdvanceInFlightChains(List<string> notices, float p)
        {
            for (int i = activeChains.Count - 1; i >= 0; i--)
            {
                var chain = activeChains[i];
                chain.daysRemaining--;

                if (chain.daysRemaining > 0)
                    continue;

                // 达到执行节点日
                switch (chain.chainType)
                {
                    case "A_Check":
                        ExecuteCheckChainStep(chain, notices, p);
                        break;
                    case "B_Media":
                        ExecuteMediaChainStep(chain, notices, p);
                        break;
                    case "C_Accident":
                        ExecuteAccidentChainStep(chain, notices, p);
                        break;
                    case "D_Blacklist":
                        ExecuteBlacklistChainStep(chain, notices);
                        break;
                    case "E_Delist":
                        ExecuteDelistChainStep(chain, notices, p);
                        break;
                    case "G_Crisis":
                        ExecuteCrisisChainStep(chain, notices);
                        break;
                    case "Bankruptcy":
                        ExecuteBankruptcyChainStep(chain, notices);
                        break;
                    case "Sand_M&A":
                        ExecuteSandChainStep(chain, notices);
                        break;
                }

                // 若已完成，移出活跃链
                if (chain.currentStep >= 99)
                {
                    activeChains.RemoveAt(i);
                }
            }
        }

        // --- A 检查链 ---
        private static void ExecuteCheckChainStep(ActiveProcedure chain, List<string> notices, float p)
        {
            if (chain.currentStep == 1)
            {
                // D+3: 下达《责令限期整改通知书》
                chain.currentStep = 2;
                chain.daysRemaining = 5; // D+5 后复查
                if (GameData.PoliticalTier < 1)
                {
                    GameData.PoliticalTier = 1; // 升至 Ⅱ 警示通报
                }
                notices.Add($"市交通运输局〔2076〕第{chain.docNumber}号：责令限期整改通知书——安全督查发现道岔捣固不良等 3 项隐患，责令 7 日内报送整改方案。");
            }
            else if (chain.currentStep == 2)
            {
                // D+8: 复查验收
                chain.currentStep = 99; // 结束
                if (p >= 0.70f)
                {
                    GameData.PoliticalTier = Mathf.Max(GameData.PoliticalTier, 2); // 隐患未消，升至 Ⅲ 约谈整改
                    notices.Add("市交通运输局〔2076〕第19号：安全生产约谈决定书——现场复查隐患未彻底清零，依法启动警示约谈程序。");
                }
                else
                {
                    notices.Add("市交通运输局〔2076〕第20号：督查整改复查备忘录——基础整改措施初步落实，督查组准予阶段性维持运行。");
                }
            }
        }

        // --- B 舆情链 ---
        private static void ExecuteMediaChainStep(ActiveProcedure chain, List<string> notices, float p)
        {
            chain.currentStep = 99;
            if (p >= 0.70f)
            {
                GameData.PoliticalTier = Mathf.Max(GameData.PoliticalTier, 2); // 舆情升级，升至 Ⅲ
                notices.Add("市委宣传部与交通运输局联合通报：支线列车舆情升级并引发公众关切，责令主管领导向市安委会作专题检讨并限期整改。");
            }
            else
            {
                GameData.PoliticalTier = Mathf.Max(GameData.PoliticalTier, 1); // 升至 Ⅱ
                notices.Add("市交通运输行业提示函：就近期社会监督反映的乘车体验与班次问题提出警示，要求加强日常站务疏导。");
            }
        }

        // --- C 事故链 ---
        private static void ExecuteAccidentChainStep(ActiveProcedure chain, List<string> notices, float p)
        {
            chain.currentStep = 99;
            if (p >= 0.75f)
            {
                GameData.PoliticalTier = Mathf.Max(GameData.PoliticalTier, 2);
                notices.Add("市铁路安全事故联合调查组结案报告：认定为较大安全运行责任险情，下达整改督办指令并启动管理责任约谈。");
            }
            else
            {
                GameData.PoliticalTier = Mathf.Max(GameData.PoliticalTier, 1);
                notices.Add("市铁路安全运行快报核查意见：认定为一般机械走行故障，下发全员警示通报，限期开展车辆防滑制动特检。");
            }
        }

        // --- D 挂牌链 (Ⅲ -> Ⅳ) ---
        private static void ExecuteBlacklistChainStep(ActiveProcedure chain, List<string> notices)
        {
            chain.currentStep = 99;
            GameData.PoliticalTier = 3; // 升至 Ⅳ 挂牌督办
            notices.Add("市安全生产委员会〔2076〕第07号：重大事故隐患挂牌督办决定书——鉴于约谈整改期间安全短板未除，即日起实施挂牌督办，减班运行。");
        }

        // --- E 摘牌销号降档链 (滞回机制) ---
        private static void ExecuteDelistChainStep(ActiveProcedure chain, List<string> notices, float p)
        {
            chain.currentStep = 99;
            int cur = GameData.PoliticalTier;

            // 滞回硬约束：必须温度计回落且整改验收合格
            if (cur == 3 && p <= 0.75f) // Ⅳ -> Ⅲ (0.85 - 0.10)
            {
                GameData.PoliticalTier = 2;
                notices.Add("市安委会〔2076〕第12号：重大安全隐患挂牌督办解封摘牌批复——经专家现场复勘，重大风险源受控，准予摘牌。");
            }
            else if (cur == 2 && p <= 0.60f) // Ⅲ -> Ⅱ (0.70 - 0.10)
            {
                GameData.PoliticalTier = 1;
                notices.Add("市交通运输局〔2076〕第28号：约谈整改销号通知书——所列 7 项整改措施全部销号闭环，正式解除安全约谈监管。");
            }
            else if (cur == 1 && p <= 0.35f) // Ⅱ -> Ⅰ (0.45 - 0.10)
            {
                GameData.PoliticalTier = 0;
                notices.Add("市交通运输局〔2076〕第33号：日常安全检查合格意见书——警示风险消除，安全生产平稳，恢复为常态监管。");
            }
            else
            {
                notices.Add("市交通运输局整改复查通告：现场复查发现整改不彻底，风险裕度不足，维持当前监管档位继续整改。");
            }
        }

        // --- G 危机停运链 (Ⅳ -> Ⅴ) ---
        private static void ExecuteCrisisChainStep(ActiveProcedure chain, List<string> notices)
        {
            chain.currentStep = 99;
            GameData.PoliticalTier = 4; // 升至 Ⅴ 停产停业整顿
            notices.Add("市应急管理局与交通运输局〔2076〕第01号：责令停产停业整顿决定书——重大险情未获有效遏制，全线停止旅客发车。");
        }

        // --- 8.1 破产重整程序链 ---
        private static void ExecuteBankruptcyChainStep(ActiveProcedure chain, List<string> notices)
        {
            if (chain.currentStep == 1)
            {
                // D+3: 法院裁定受理
                chain.currentStep = 2;
                chain.daysRemaining = 4; // 再过 4 天 (D+7) 第一期到账
                notices.Add("市中级人民法院〔2076〕破字第04号民事裁定书：依法裁定受理雾峰村车站破产重整案，指定重整工作清算组。");
            }
            else if (chain.currentStep == 2)
            {
                // D+7: 第一期到账
                chain.currentStep = 3;
                chain.daysRemaining = 5; // 再过 5 天 (D+12) 第二期到账
                GameData.AddMoney(5000);
                notices.Add("市中级人民法院清算组账户划转凭证：第一期职工互助与民间纾困重整资金 5,000 沙币已划拨到账。");
            }
            else if (chain.currentStep == 3)
            {
                // D+12: 第二期到账
                chain.currentStep = 99;
                GameData.AddMoney(5000);
                notices.Add("市财政局企业重整专项基金拨付单：第二期财政应急稳岗资金 5,000 沙币划拨到账，重整程序执行完毕。");
            }
        }

        // --- 8.2 沙能交割程序链 ---
        private static void ExecuteSandChainStep(ActiveProcedure chain, List<string> notices)
        {
            if (chain.currentStep == 1)
            {
                chain.currentStep = 2;
                chain.daysRemaining = 4;
                notices.Add("市发改委与商务局合规审查通告：雾峰支线特许经营权股权受让进入合规公示期，铁龙重工完成资金托管。");
            }
            else if (chain.currentStep == 2)
            {
                chain.currentStep = 99;
                sandStage = 3; // 已交割控制
                notices.Add("铁龙重工股份有限公司公告：特许经营权交割及董事会改组完毕，关键线路进入联运控制状态。");
            }
        }

        // ================= 触发新程序检测 =================
        private static void DetectNewProcedures(List<string> notices, float p, int tier)
        {
            // 1. 上行恶化事件检测
            if (tier < 1 && p >= 0.45f && !HasActiveChain("A_Check"))
            {
                // 启动检查链
                checkDocSeq++;
                activeChains.Add(new ActiveProcedure
                {
                    chainType = "A_Check",
                    startDay = GameData.Day,
                    currentStep = 1,
                    daysRemaining = 3,
                    docNumber = checkDocSeq.ToString()
                });
                notices.Add($"市交通运输局〔2076〕第{checkDocSeq}号：关于进驻雾峰村车站开展铁路安全生产专项督查的通知。");
            }
            else if (tier == 1 && p >= 0.70f && !HasActiveChain("A_Check") && !HasActiveChain("B_Media"))
            {
                // 启动约谈推进链（通过舆情或专项督查）
                activeChains.Add(new ActiveProcedure
                {
                    chainType = "B_Media",
                    startDay = GameData.Day,
                    currentStep = 1,
                    daysRemaining = 3
                });
                notices.Add("《边陲晨报》深度调查曝光：雾峰支线老旧设施运行隐患调查报道引发关注，行业监管介入。");
            }
            else if (tier == 2 && p >= 0.85f && !HasActiveChain("D_Blacklist"))
            {
                // 启动挂牌督办链
                activeChains.Add(new ActiveProcedure
                {
                    chainType = "D_Blacklist",
                    startDay = GameData.Day,
                    currentStep = 1,
                    daysRemaining = 5
                });
                safetyNoticeSeq++;
                notices.Add($"市安全生产委员会〔2076〕第0{safetyNoticeSeq}号预警通报：雾峰村车站隐患整治进展迟缓，纳入挂牌督办拟定名单。");
            }

            // 2. 下行缓和（摘牌销号）事件检测（严格滞回）
            if (tier > 0 && !HasActiveChain("E_Delist"))
            {
                bool canDelist = false;
                if (tier == 3 && p <= 0.75f) canDelist = true;
                else if (tier == 2 && p <= 0.60f) canDelist = true;
                else if (tier == 1 && p <= 0.35f) canDelist = true;

                if (canDelist)
                {
                    activeChains.Add(new ActiveProcedure
                    {
                        chainType = "E_Delist",
                        startDay = GameData.Day,
                        currentStep = 1,
                        daysRemaining = 5
                    });
                    notices.Add("雾峰村车站〔2076〕第03号：关于报送安全隐患治理与整改闭环验收情况的报告已呈报上级主管部门。");
                }
            }

            // 3. 沙能渗透交割程序检测 (8.2)
            float sand = SandRivalManager.GetPenetration("wufeng");
            if (sandStage == 0 && sand >= 0.40f)
            {
                sandStage = 1;
                notices.Add("区域能源监督局：关于沙能重工扩大本市能源运输配额的市场风险提示函。");
            }
            else if (sandStage == 1 && sand >= 0.55f && !HasActiveChain("Sand_M&A"))
            {
                sandStage = 2;
                activeChains.Add(new ActiveProcedure
                {
                    chainType = "Sand_M&A",
                    startDay = GameData.Day,
                    currentStep = 1,
                    daysRemaining = 3
                });
                notices.Add("铁龙重工投资联合体：关于拟收购雾峰支线特许经营权的要约收购公告。");
            }
        }

        // ================= 危机停运检测 =================
        private static void CheckCrisisPressure(List<string> notices, float p)
        {
            if (p >= 0.95f)
            {
                daysInExtremePressure++;
                if (daysInExtremePressure == 3)
                {
                    notices.Add("市安委会紧急红头文书：全线安全风险指数逼近极值，启动接管听证与全线停运最后倒计时。");
                }
                else if (daysInExtremePressure >= 7 && GameData.PoliticalTier < 4 && !HasActiveChain("G_Crisis"))
                {
                    activeChains.Add(new ActiveProcedure
                    {
                        chainType = "G_Crisis",
                        startDay = GameData.Day,
                        currentStep = 1,
                        daysRemaining = 1
                    });
                }
            }
            else
            {
                daysInExtremePressure = Mathf.Max(0, daysInExtremePressure - 1);
            }
        }

        // ================= 8.1 资金链断裂触发破产重整 =================
        public static void TriggerBankruptcyProcedure(List<string> notices)
        {
            if (HasActiveChain("Bankruptcy"))
                return;

            GameData.BankruptcyCount++;
            courtDocSeq++;

            if (GameData.BankruptcyCount >= 3)
            {
                notices.Add("市中级人民法院清算预警函：重整期内未改善现金流，第三次面临破产清算与运营资质注销风险。");
                return;
            }

            activeChains.Add(new ActiveProcedure
            {
                chainType = "Bankruptcy",
                startDay = GameData.Day,
                currentStep = 1,
                daysRemaining = 3
            });
            notices.Add($"雾峰村车站破产重整事务组：关于向市中级人民法院提交重整申请的公告（第 {GameData.BankruptcyCount} 次破产预警）。");
        }

        // ================= 8.4 工务检定制定期推进 =================
        private static void AdvanceInfrastructureInspection(List<string> notices)
        {
            daysSinceLastInspection++;
            // 30 天周期进行一次工务质量检定
            if (daysSinceLastInspection >= 30)
            {
                daysSinceLastInspection = 0;
                float d = GameData.InfrastructureDecay;
                GameData.DecayGrade = CalculateDecayGradeFromValue(d);
                GameData.DecayGradeRoadbed = CalculateDecayGradeFromValue(GameData.DecayRoadbed);
                GameData.DecayGradeBridge = CalculateDecayGradeFromValue(GameData.DecayBridge);
                GameData.DecayGradeSignal = CalculateDecayGradeFromValue(GameData.DecaySignal);

                string gradeWord = GetDecayGradeWord(GameData.DecayGrade);
                notices.Add($"工务段定期线路质量检定报告：综合评定为〈{gradeWord}〉，已归档入设备台账。");
            }
        }

        public static int CalculateDecayGradeFromValue(float d)
        {
            if (d < 0.30f) return 0;
            if (d < 0.55f) return 1;
            if (d < 0.75f) return 2;
            return 3;
        }

        public static string GetDecayGradeWord(int grade)
        {
            switch (grade)
            {
                case 0: return "状态合格";
                case 1: return "经常保养";
                case 2: return "临时补修";
                case 3: return "评定失格";
                default: return "经常保养";
            }
        }

        // ================= 状态查询与存盘 =================
        private static bool HasActiveChain(string chainType)
        {
            for (int i = 0; i < activeChains.Count; i++)
            {
                if (activeChains[i].chainType == chainType)
                    return true;
            }
            return false;
        }

        public static bool IsSandControlled()
        {
            return sandStage >= 3;
        }

        private static void SaveState()
        {
            try
            {
                var container = new RegulatorySaveContainer
                {
                    politicalTier = GameData.PoliticalTier,
                    decayGrade = GameData.DecayGrade,
                    bankruptcyCount = GameData.BankruptcyCount,
                    sandStage = sandStage,
                    daysInExtremePressure = daysInExtremePressure,
                    daysSinceLastInspection = daysSinceLastInspection,
                    activeChains = new List<ActiveProcedure>(activeChains)
                };
                string json = JsonUtility.ToJson(container);
                PlayerPrefs.SetString("GameData_RegulatoryState", json);
                PlayerPrefs.SetInt("GameData_PoliticalTier", GameData.PoliticalTier);
                PlayerPrefs.SetInt("GameData_DecayGrade", GameData.DecayGrade);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RegulatoryEventSystem] Save failed: " + ex.Message);
            }
        }

        private static void LoadState()
        {
            string json = PlayerPrefs.GetString("GameData_RegulatoryState", "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var container = JsonUtility.FromJson<RegulatorySaveContainer>(json);
                    if (container != null)
                    {
                        GameData.PoliticalTier = container.politicalTier;
                        GameData.DecayGrade = container.decayGrade;
                        GameData.BankruptcyCount = container.bankruptcyCount;
                        sandStage = container.sandStage;
                        daysInExtremePressure = container.daysInExtremePressure;
                        daysSinceLastInspection = container.daysSinceLastInspection;
                        activeChains = container.activeChains ?? new List<ActiveProcedure>();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[RegulatoryEventSystem] Load failed: " + ex.Message);
                }
            }

            // 老档向下兼容：按现有连续量映射初始档位，无在途程序
            float p = GameData.PoliticalPressure;
            if (p < 0.45f) GameData.PoliticalTier = 0;
            else if (p < 0.70f) GameData.PoliticalTier = 1;
            else if (p < 0.85f) GameData.PoliticalTier = 2;
            else if (p < 0.95f) GameData.PoliticalTier = 3;
            else GameData.PoliticalTier = 4;

            float d = GameData.InfrastructureDecay;
            GameData.DecayGrade = CalculateDecayGradeFromValue(d);
            GameData.DecayGradeRoadbed = CalculateDecayGradeFromValue(GameData.DecayRoadbed);
            GameData.DecayGradeBridge = CalculateDecayGradeFromValue(GameData.DecayBridge);
            GameData.DecayGradeSignal = CalculateDecayGradeFromValue(GameData.DecaySignal);
        }
    }
}
