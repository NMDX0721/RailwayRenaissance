using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Text dayText;
    public Text moneyText;
    public Text trustText;
    public Text trainConditionText;
    public Text expectedPassengersText;
    public Text statusText;

    public Text descriptionText;
    public Text briefingText;
    public Text noticesText;
    public Text todosText;

    public GameObject schedulePanel;
    public GameObject staffPanel;
    public GameObject maintenancePanel;
    public GameObject stationServicePanel;
    public GameObject externalAffairsPanel;
    public GameObject endDayPanel;
    public GameObject summaryPanel;

    public Text schedulePanelTitle;
    public Text staffPanelTitle;
    public Text maintenancePanelTitle;
    public Text stationServicePanelTitle;
    public Text externalAffairsPanelTitle;
    public Text endDayPanelTitle;
    public Text summaryPanelTitle;

    public Text schedulePanelContent;
    public Text staffPanelContent;
    public Text maintenancePanelContent;
    public Text stationServicePanelContent;
    public Text externalAffairsPanelContent;
    public Text endDayPanelContent;
    public Text summaryPanelContent;

    private bool endingDay;
    private GameData.DayResult lastDayResult;

    private void Start()
    {
        if (PlayerPrefs.HasKey("VNExitData"))
        {
            string json = PlayerPrefs.GetString("VNExitData");
            PlayerPrefs.DeleteKey("VNExitData");
            PlayerPrefs.Save();
            try
            {
                VNExitData exitData = JsonUtility.FromJson<VNExitData>(json);
                GameData.ApplyVNExitData(exitData);
                Debug.Log("[UIManager] 成功承接序章过渡数据 (VNExitData): 资金=" + GameData.Money + ", 信任=" + GameData.Trust + ", 车况=" + GameData.TrainCondition);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[UIManager] 解析 VNExitData 失败，回退默认初始状态: " + ex);
                GameData.ResetState();
            }
        }
        else
        {
            // 无前置过渡数据且未加载存档时才执行全新重置
            GameData.ResetState();
        }

        RefreshAll();
        HideAllPanels();
        SetDefaultDescription();
    }

    public void UpdateStatusBar()
    {
        if (dayText != null) dayText.text = "第 " + GameData.Day + " 天";
        if (moneyText != null) moneyText.text = "资金: " + GameData.Money;
        if (trustText != null) trustText.text = "信任: " + GameData.Trust + "%";
        if (trainConditionText != null) trainConditionText.text = "车况: " + GameData.TrainCondition + "%";
        if (expectedPassengersText != null) expectedPassengersText.text = "客流: " + GameData.ExpectedPassengers;
        if (statusText != null) statusText.text = "当前阶段: 排班决策";
    }

    public void UpdateNoticePanel()
    {
        if (briefingText != null) briefingText.text = JoinLines(GameData.Briefing);
        if (noticesText != null) noticesText.text = JoinLines(GameData.Notices);
        if (todosText != null) todosText.text = JoinBullets(GameData.Todos);
    }

    public void ShowPanel(GameObject panel)
    {
        HideAllPanels();

        if (panel == null)
        {
            SetDefaultDescription();
            return;
        }

        panel.SetActive(true);

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            StartCoroutine(FadeIn(canvasGroup, 0.18f));
        }

        if (panel == schedulePanel)
        {
            SelectPanel(schedulePanelTitle, "当前正在查看发车方案。再次点击底部同名按钮会切换方案。");
        }
        else if (panel == staffPanel)
        {
            SelectPanel(staffPanelTitle, "当前正在查看人员安排。再次点击底部同名按钮会切换分工。");
        }
        else if (panel == maintenancePanel)
        {
            SelectPanel(maintenancePanelTitle, "当前正在查看检修策略。再次点击底部同名按钮会切换投入强度。");
        }
        else if (panel == stationServicePanel)
        {
            SelectPanel(stationServicePanelTitle, "当前正在查看站务服务。再次点击底部同名按钮会切换服务档位。");
        }
        else if (panel == externalAffairsPanel)
        {
            SelectPanel(externalAffairsPanelTitle, "当前正在查看对外事务。再次点击底部同名按钮会切换沟通策略。");
        }
        else if (panel == endDayPanel)
        {
            SelectPanel(endDayPanelTitle, "确认今天的组合后，再按一次 End Day 就会结算并进入下一天。");
        }
        else if (panel == summaryPanel)
        {
            SelectPanel(summaryPanelTitle, "这是刚刚结算后的结果。你可以继续调整，准备下一天。");
        }
    }

    public void HideAllPanels()
    {
        ResetPanelColors();

        SetPanelState(schedulePanel, false);
        SetPanelState(staffPanel, false);
        SetPanelState(maintenancePanel, false);
        SetPanelState(stationServicePanel, false);
        SetPanelState(externalAffairsPanel, false);
        SetPanelState(endDayPanel, false);
        SetPanelState(summaryPanel, false);
    }

    public void OnScheduleButtonClick()
    {
        HandleStrategyButton(schedulePanel, RefreshSchedulePanel, GameData.CycleDispatchPlan);
    }

    public void OnStaffButtonClick()
    {
        HandleStrategyButton(staffPanel, RefreshStaffPanel, GameData.CycleStaffAllocation);
    }

    public void OnMaintenanceButtonClick()
    {
        HandleStrategyButton(maintenancePanel, RefreshMaintenancePanel, GameData.CycleMaintenanceStrategy);
    }

    public void OnStationServiceButtonClick()
    {
        HandleStrategyButton(stationServicePanel, RefreshStationServicePanel, GameData.CycleStationServiceStrategy);
    }

    public void OnExternalAffairsButtonClick()
    {
        HandleStrategyButton(externalAffairsPanel, RefreshExternalAffairsPanel, GameData.CycleExternalAffairsStrategy);
    }

    public void OnEndDayButtonClick()
    {
        if (endingDay)
        {
            return;
        }

        if (endDayPanel != null && endDayPanel.activeSelf)
        {
            StartCoroutine(AdvanceDayFlow());
            return;
        }

        RefreshEndDayPanel();
        ShowPanel(endDayPanel);
        PlaySound("confirm");
    }

    public void RefreshSummaryPanel()
    {
        if (summaryPanelTitle != null)
        {
            summaryPanelTitle.text = "日结总结";
        }

        if (summaryPanelContent == null)
        {
            return;
        }

        List<string> lines = new List<string>
        {
            "<b>今天的结果</b>",
            "资金变化: " + GameData.FormatSigned(GameData.DailyMoneyChange, ""),
            "信任变化: " + GameData.FormatSigned(GameData.DailyTrustChange, "%"),
            "车况变化: " + GameData.FormatSigned(GameData.DailyTrainConditionChange, "%"),
            "客流变化: " + GameData.FormatSigned(GameData.DailyPassengerChange, ""),
            "",
            "<b>本日决策</b>",
            "发车: " + GameData.GetCurrentDispatchPlan().Name,
            "人员: " + GameData.GetStaffAllocationName(GameData.CurrentStaffAllocation),
            "检修: " + GameData.GetMaintenanceStrategyName(GameData.CurrentMaintenanceStrategy),
            "服务: " + GameData.GetStationServiceStrategyName(GameData.CurrentStationServiceStrategy),
            "对外: " + GameData.GetExternalAffairsStrategyName(GameData.CurrentExternalAffairsStrategy),
            "",
            "<b>突发事件</b>",
            lastDayResult.EventTitle,
            lastDayResult.EventDescription,
            "",
            "<b>站长记要</b>",
            lastDayResult.Tone
        };

        if (lastDayResult.GoalCompleted)
        {
            lines.Add("短期目标达成，下一项目标已经自动接替。");
        }

        summaryPanelContent.text = string.Join("\n", lines);
    }

    private void RefreshAll()
    {
        UpdateStatusBar();
        UpdateNoticePanel();
        RefreshSchedulePanel();
        RefreshStaffPanel();
        RefreshMaintenancePanel();
        RefreshStationServicePanel();
        RefreshExternalAffairsPanel();
        RefreshEndDayPanel();
        RefreshSummaryPanel();
    }

    private void RefreshSchedulePanel()
    {
        if (schedulePanelTitle != null)
        {
            schedulePanelTitle.text = "发车安排";
        }

        if (schedulePanelContent == null)
        {
            return;
        }

        GameData.DispatchPlan plan = GameData.GetCurrentDispatchPlan();
        schedulePanelContent.text =
            "当前方案: " + plan.Name + "\n" +
            plan.Summary + "\n\n" +
            "预计影响\n" +
            "资金 " + GameData.FormatSigned(plan.MoneyDelta, "") + "\n" +
            "信任 " + GameData.FormatSigned(plan.TrustDelta, "%") + "\n" +
            "车况 " + GameData.FormatSigned(plan.TrainConditionDelta, "%") + "\n" +
            "客流 " + GameData.FormatSigned(plan.PassengerDelta, "") + "\n\n" +
            GetScheduleRisk(plan.Name);
    }

    private void RefreshStaffPanel()
    {
        if (staffPanelTitle != null)
        {
            staffPanelTitle.text = "人员安排";
        }

        if (staffPanelContent == null)
        {
            return;
        }

        GameData.StaffAllocationType current = GameData.CurrentStaffAllocation;
        staffPanelContent.text =
            "当前安排: " + GameData.GetStaffAllocationName(current) + "\n" +
            GameData.GetStaffAllocationDescription(current) + "\n\n" +
            "排班提示\n" +
            GetStaffEffectText(current) + "\n\n" +
            "站内人手\n" +
            "李阿姨: 擅长服务和安抚乘客\n" +
            "老周: 擅长检修和设备保养\n" +
            "小王: 可以在多个岗位补位";
    }

    private void RefreshMaintenancePanel()
    {
        if (maintenancePanelTitle != null)
        {
            maintenancePanelTitle.text = "维护整备";
        }

        if (maintenancePanelContent == null)
        {
            return;
        }

        GameData.MaintenanceStrategy current = GameData.CurrentMaintenanceStrategy;
        maintenancePanelContent.text =
            "当前策略: " + GameData.GetMaintenanceStrategyName(current) + "\n" +
            GameData.GetMaintenanceStrategyDescription(current) + "\n\n" +
            "设备观察\n" +
            "当前车况: " + GameData.TrainCondition + "%\n" +
            GetMaintenanceRiskText() + "\n\n" +
            "本策略影响\n" +
            GetMaintenanceEffectText(current);
    }

    private void RefreshStationServicePanel()
    {
        if (stationServicePanelTitle != null)
        {
            stationServicePanelTitle.text = "站务服务";
        }

        if (stationServicePanelContent == null)
        {
            return;
        }

        GameData.StationServiceStrategy current = GameData.CurrentStationServiceStrategy;
        stationServicePanelContent.text =
            "当前服务档位: " + GameData.GetStationServiceStrategyName(current) + "\n" +
            GameData.GetStationServiceStrategyDescription(current) + "\n\n" +
            "乘客体验观察\n" +
            GetServiceMoodText() + "\n\n" +
            "本策略影响\n" +
            GetStationServiceEffectText(current);
    }

    private void RefreshExternalAffairsPanel()
    {
        if (externalAffairsPanelTitle != null)
        {
            externalAffairsPanelTitle.text = "对外事务";
        }

        if (externalAffairsPanelContent == null)
        {
            return;
        }

        GameData.ExternalAffairsStrategy current = GameData.CurrentExternalAffairsStrategy;
        externalAffairsPanelContent.text =
            "当前策略: " + GameData.GetExternalAffairsStrategyName(current) + "\n" +
            GameData.GetExternalAffairsStrategyDescription(current) + "\n\n" +
            "外部环境\n" +
            GetExternalMoodText() + "\n\n" +
            "本策略影响\n" +
            GetExternalEffectText(current);
    }

    private void RefreshEndDayPanel()
    {
        if (endDayPanelTitle != null)
        {
            endDayPanelTitle.text = "结束当天";
        }

        if (endDayPanelContent == null)
        {
            return;
        }

        endDayPanelContent.text =
            "今天将按以下组合结算\n\n" +
            "发车: " + GameData.GetCurrentDispatchPlan().Name + "\n" +
            "人员: " + GameData.GetStaffAllocationName(GameData.CurrentStaffAllocation) + "\n" +
            "检修: " + GameData.GetMaintenanceStrategyName(GameData.CurrentMaintenanceStrategy) + "\n" +
            "服务: " + GameData.GetStationServiceStrategyName(GameData.CurrentStationServiceStrategy) + "\n" +
            "对外: " + GameData.GetExternalAffairsStrategyName(GameData.CurrentExternalAffairsStrategy) + "\n\n" +
            "确认后再次点击 End Day，系统会结算今天并进入下一天。";
    }

    private IEnumerator AdvanceDayFlow()
    {
        endingDay = true;
        PlaySound("confirm");
        yield return new WaitForSeconds(0.2f);

        lastDayResult = GameData.AdvanceDay();
        RefreshAll();
        ShowPanel(summaryPanel);

        endingDay = false;
    }

    private void HandleStrategyButton(GameObject panel, System.Action refreshAction, System.Action cycleAction)
    {
        if (endingDay)
        {
            return;
        }

        if (panel != null && panel.activeSelf)
        {
            cycleAction();
            refreshAction();
            RefreshEndDayPanel();
            UpdateStatusBar();
            UpdateNoticePanel();
            PlaySound("switch");
            return;
        }

        refreshAction();
        ShowPanel(panel);
        PlaySound("click");
    }

    private void PlaySound(string soundType)
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        switch (soundType)
        {
            case "switch":
                AudioManager.Instance.PlayUISwitch();
                break;
            case "confirm":
                AudioManager.Instance.PlayUIConfirm();
                break;
            default:
                AudioManager.Instance.PlayUIClick();
                break;
        }
    }

    private void SelectPanel(Text title, string description)
    {
        ResetPanelColors();
        if (title != null)
        {
            title.color = Color.yellow;
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
        }
    }

    private void ResetPanelColors()
    {
        SetTitleColor(schedulePanelTitle, Color.white);
        SetTitleColor(staffPanelTitle, Color.white);
        SetTitleColor(maintenancePanelTitle, Color.white);
        SetTitleColor(stationServicePanelTitle, Color.white);
        SetTitleColor(externalAffairsPanelTitle, Color.white);
        SetTitleColor(endDayPanelTitle, Color.white);
        SetTitleColor(summaryPanelTitle, Color.white);
    }

    private void SetTitleColor(Text title, Color color)
    {
        if (title != null)
        {
            title.color = color;
        }
    }

    private void SetPanelState(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private void SetDefaultDescription()
    {
        if (descriptionText != null)
        {
            descriptionText.text =
                "今天的节奏很简单：\n" +
                "1. 先看状态\n" +
                "2. 再给五个经营模块定方案\n" +
                "3. 最后按两次 End Day 完成结算";
        }
    }

    private static string JoinLines(List<string> items)
    {
        return string.Join("\n", items);
    }

    private static string JoinBullets(List<string> items)
    {
        List<string> lines = new List<string>();
        foreach (string item in items)
        {
            lines.Add("• " + item);
        }

        return string.Join("\n", lines);
    }

    private static IEnumerator FadeIn(CanvasGroup canvasGroup, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private string GetScheduleRisk(string planName)
    {
        if (planName == "加开一班")
        {
            return "风险判断: 收入更高，但对车况和乘客情绪都更苛刻。";
        }
        if (planName == "保守运行")
        {
            return "风险判断: 很稳，但今天的现金回收会慢一些。";
        }
        return "风险判断: 整体均衡，适合先观察今天会怎么变化。";
    }

    private string GetStaffEffectText(GameData.StaffAllocationType type)
    {
        switch (type)
        {
            case GameData.StaffAllocationType.Balanced:
                return "信任 +2%\n车况 +2%\n客流 +2";
            case GameData.StaffAllocationType.Maintenance:
                return "资金 -50\n车况 +8%\n客流 -1";
            default:
                return "资金 -30\n信任 +5%\n客流 +5\n车况 -2%";
        }
    }

    private string GetMaintenanceEffectText(GameData.MaintenanceStrategy strategy)
    {
        switch (strategy)
        {
            case GameData.MaintenanceStrategy.Postpone:
                return "资金 +90\n车况 -8%";
            case GameData.MaintenanceStrategy.Standard:
                return "资金 -120\n车况 +7%";
            default:
                return "资金 -240\n信任 +1%\n车况 +13%\n客流 +1";
        }
    }

    private string GetStationServiceEffectText(GameData.StationServiceStrategy strategy)
    {
        switch (strategy)
        {
            case GameData.StationServiceStrategy.Basic:
                return "资金 +40\n信任 -2%\n客流 -2";
            case GameData.StationServiceStrategy.Standard:
                return "资金 -40\n信任 +2%\n客流 +2";
            default:
                return "资金 -120\n信任 +4%\n客流 +4";
        }
    }

    private string GetExternalEffectText(GameData.ExternalAffairsStrategy strategy)
    {
        switch (strategy)
        {
            case GameData.ExternalAffairsStrategy.Minimal:
                return "资金 +80\n信任 -3%";
            case GameData.ExternalAffairsStrategy.Standard:
                return "资金 -80\n信任 +3%\n客流 +1";
            default:
                return "资金 -180\n信任 +6%\n客流 +2";
        }
    }

    private string GetMaintenanceRiskText()
    {
        if (GameData.TrainCondition < 40)
        {
            return "机车已经很勉强了，再拖会越来越难收场。";
        }
        if (GameData.TrainCondition < 65)
        {
            return "还能撑，但接下来最好别连续冒进。";
        }
        return "当前车况还算稳，可以根据目标做取舍。";
    }

    private string GetServiceMoodText()
    {
        if (GameData.Trust < 50)
        {
            return "乘客明显开始不耐烦，站内服务会直接影响口碑。";
        }
        if (GameData.ExpectedPassengers > 28)
        {
            return "站里人多起来了，服务档位越低越容易掉评价。";
        }
        return "今天的服务压力不算极端，适合做温和调整。";
    }

    private string GetExternalMoodText()
    {
        if (GameData.Money < 1500)
        {
            return "账上偏紧，外部投入每一步都得算清楚。";
        }
        if (GameData.Trust < 60)
        {
            return "地方和乘客都在看你们能不能稳住，沟通价值更高。";
        }
        return "外部环境还算平稳，可以按目标决定要不要主动出击。";
    }
}
