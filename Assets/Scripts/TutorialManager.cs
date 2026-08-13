using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    private static int gameDay = 1;
    private static bool[] tipsShown = new bool[10];

    public GameObject tutorialPanel;
    public Text tutorialText;
    public Button nextButton;
    public Button skipButton;

    private static string pendingTip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(OnDismissTip);
        if (skipButton != null)
            skipButton.onClick.AddListener(OnDismissTip);

        ShowPendingTip();
    }

    public static void SetGameDay(int day)
    {
        gameDay = day;
    }

    /// <summary>检查当天应解锁的提示，返回提示文本；无提示则返回 null。</summary>
    public static string CheckForTip()
    {
        // 第1-7天：只显示基础信息（资金、信任、车况、客流）
        if (gameDay == 1 && !tipsShown[0])
        {
            tipsShown[0] = true;
            return "欢迎来到雾峰村车站！\n每天你可以调整发车方案、人员分配、维护策略等。\n先看看站内情况，再决定今天怎么经营。";
        }
        if (gameDay == 3 && !tipsShown[1])
        {
            tipsShown[1] = true;
            return "提示：点击「推进日程」结束当天运营，进入下一天。\n每天的收入和支出会自动结算。";
        }
        // 第8-30天：解锁沙能渗透、员工满意度信息
        if (gameDay >= 8 && gameDay <= 30 && !tipsShown[2])
        {
            tipsShown[2] = true;
            return "新信息已解锁：沙能渗透率、员工满意度。\n注意沙能公司可能正在蚕食你的客流。";
        }
        // 第31-90天：解锁趋势线图表
        if (gameDay >= 31 && !tipsShown[3])
        {
            tipsShown[3] = true;
            return "趋势图表现已解锁！\n你可以查看信任、沙能渗透、财政压力的长期变化趋势。";
        }
        // 第91天+：全部开放
        if (gameDay >= 91 && !tipsShown[4])
        {
            tipsShown[4] = true;
            return "所有数据已开放。\n你现在可以查看全部运营指标和远期预测。";
        }

        return null;
    }

    /// <summary>破产保护。返回 true 表示已注入紧急资金。</summary>
    public static bool CheckBankruptcyProtection(ref int money)
    {
        if (money <= 0 && !tipsShown[5])
        {
            tipsShown[5] = true;
            money = 10000; // 老陈紧急资金
            return true;
        }
        if (money <= 0 && !tipsShown[6])
        {
            tipsShown[6] = true;
            money = 5000; // 政府救助（需签苛刻条件）
            return true;
        }
        return false;
    }

    /// <summary>显示一条提示（从静态上下文中调用）。</summary>
    public static void ShowTip(string tip)
    {
        pendingTip = tip;
        if (Instance != null)
            Instance.ShowPendingTip();
    }

    private void ShowPendingTip()
    {
        if (string.IsNullOrEmpty(pendingTip) || tutorialPanel == null)
            return;

        tutorialText.text = pendingTip;
        tutorialPanel.SetActive(true);
        pendingTip = null;

        CanvasGroup cg = tutorialPanel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0;
            StartCoroutine(FadeIn(cg, 0.3f));
        }
    }

    private IEnumerator FadeIn(CanvasGroup cg, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0, 1, t / duration);
            yield return null;
        }
        cg.alpha = 1;
    }

    private void OnDismissTip()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUIClick();
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }
}