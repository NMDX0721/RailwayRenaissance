using UnityEngine;

/// <summary>
/// 站长日志·统计：累计游戏时长与经营数值。时长由常驻组件累加并持久化，
/// 数值由 GameData.AdvanceDay 通过静态方法记账。
/// </summary>
public class StatsManager : MonoBehaviour
{
    private const string KeyPlaySeconds = "Stats_PlaySeconds";
    private const string KeyRevenue    = "Stats_Revenue";
    private const string KeyExpense    = "Stats_Expense";
    private const string KeyPassengers = "Stats_Passengers";
    private const string KeySubsidy    = "Stats_Subsidy";
    private const string KeyMaxDay     = "Stats_MaxDay";

    private static StatsManager _instance;
    public static StatsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("StatsManager");
                _instance = go.AddComponent<StatsManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private float _pendingPlaySeconds;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        _pendingPlaySeconds += Time.unscaledDeltaTime;
        // 每 5 秒落盘一次，避免频繁写 PlayerPrefs
        if (_pendingPlaySeconds >= 5f)
        {
            AddPlaySeconds(_pendingPlaySeconds);
            _pendingPlaySeconds = 0f;
        }
    }

    private void OnApplicationQuit()
    {
        if (_pendingPlaySeconds > 0f) AddPlaySeconds(_pendingPlaySeconds);
    }

    // ================= 时长 =================

    /// <summary>总游戏时长（秒）。</summary>
    public static long TotalPlaySeconds => PlayerPrefs.GetLong(KeyPlaySeconds, 0);

    private static void AddPlaySeconds(float seconds)
    {
        PlayerPrefs.SetLong(KeyPlaySeconds, PlayerPrefs.GetLong(KeyPlaySeconds, 0) + (long)seconds);
        PlayerPrefs.Save();
    }

    // ================= 每日记账（GameData.AdvanceDay 调用） =================

    public static void AddDailyRevenue(int revenue, int subsidy)
    {
        PlayerPrefs.SetLong(KeyRevenue, PlayerPrefs.GetLong(KeyRevenue, 0) + revenue);
        PlayerPrefs.SetLong(KeySubsidy, PlayerPrefs.GetLong(KeySubsidy, 0) + subsidy);
        PlayerPrefs.Save();
    }

    public static void AddDailyExpense(int expense)
    {
        PlayerPrefs.SetLong(KeyExpense, PlayerPrefs.GetLong(KeyExpense, 0) + expense);
        PlayerPrefs.Save();
    }

    public static void AddDailyPassengers(int passengers)
    {
        PlayerPrefs.SetLong(KeyPassengers, PlayerPrefs.GetLong(KeyPassengers, 0) + passengers);
        PlayerPrefs.Save();
    }

    public static void RecordDay(int day)
    {
        if (PlayerPrefs.GetInt(KeyMaxDay, 0) < day)
            PlayerPrefs.SetInt(KeyMaxDay, day);
    }

    // ================= 读取 =================

    public static long TotalRevenue => PlayerPrefs.GetLong(KeyRevenue, 0);
    public static long TotalExpense => PlayerPrefs.GetLong(KeyExpense, 0);
    public static long TotalSubsidy => PlayerPrefs.GetLong(KeySubsidy, 0);
    public static long TotalPassengers => PlayerPrefs.GetLong(KeyPassengers, 0);
    public static int MaxDay => PlayerPrefs.GetInt(KeyMaxDay, 0);

    /// <summary>格式化时长：X小时Y分Z秒。</summary>
    public static string FormatPlayTime(long seconds)
    {
        long h = seconds / 3600;
        long m = (seconds % 3600) / 60;
        long s = seconds % 60;
        if (h > 0) return h + "小时" + m + "分" + s + "秒";
        if (m > 0) return m + "分" + s + "秒";
        return s + "秒";
    }
}