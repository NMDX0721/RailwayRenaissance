using UnityEngine;

/// <summary>
/// 系统管控彩蛋：澎湃32朝鲜特供版中被替换的4个小米原生应用。
/// 玩家可在「设置→应用管理」中找到隐藏条目并尝试恢复。
/// 恢复第4个后触发时间胶囊，系统自动恢复出厂设置。
/// </summary>
public class SystemAppsEasterEgg
{
    private const string PrefKey_RestoredCount = "SysApp_RestoredCount";
    private const string PrefKey_App0 = "SysApp_Restored_0";
    private const string PrefKey_App1 = "SysApp_Restored_1";
    private const string PrefKey_App2 = "SysApp_Restored_2";
    private const string PrefKey_App3 = "SysApp_Restored_3";
    private const string PrefKey_CapsuleTriggered = "SysApp_CapsuleTriggered";

    public struct DisabledApp
    {
        public string OriginalName;    // 原小米应用名
        public string ReplacedName;    // 朝鲜替换版名
        public string FlashText;       // 恢复后闪现的描述
        public string PopupMessage;    // 弹窗消息
        public string JumpTarget;      // 跳转目标（null=无跳转/死路）
        public string IconSymbol;      // 图标符号
    }

    public static readonly DisabledApp[] Apps = new DisabledApp[]
    {
        new DisabledApp
        {
            OriginalName = "小米应用商店",
            ReplacedName = "阿里郎商店",
            FlashText = "小米应用商店",
            PopupMessage = "该应用已被系统管控替代",
            JumpTarget = "阿里郎商店",
            IconSymbol = "\u2606" // ☆
        },
        new DisabledApp
        {
            OriginalName = "小米浏览器",
            ReplacedName = "未来网",
            FlashText = "Chrome风格首页",
            PopupMessage = "外部网络连接已被防火墙拦截",
            JumpTarget = "未来网",
            IconSymbol = "\u25CB" // ○
        },
        new DisabledApp
        {
            OriginalName = "小米安全中心",
            ReplacedName = "白头疫苗",
            FlashText = "安全中心盾牌",
            PopupMessage = "安全服务已由白头疫苗统一管理",
            JumpTarget = "白头疫苗",
            IconSymbol = "\u25A0" // ■
        },
        new DisabledApp
        {
            OriginalName = "小米云服务",
            ReplacedName = "(已移除)",
            FlashText = "云服务登录页",
            PopupMessage = "云服务不可用·设备已离线",
            JumpTarget = null, // 死路，无跳转
            IconSymbol = "\u25D1" // ◑
        }
    };

    // 时间胶囊两行字
    public const string TimeCapsule_Line1 = "有些东西会停，有些东西不会。";
    public const string TimeCapsule_Line2 = "如果你还能读到这行字——世界还在转。替我看看它。";

    public static int GetRestoredCount()
    {
        return PlayerPrefs.GetInt(PrefKey_RestoredCount, 0);
    }

    public static bool IsAppRestored(int index)
    {
        string key = $"SysApp_Restored_{index}";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    public static bool IsCapsuleTriggered()
    {
        return PlayerPrefs.GetInt(PrefKey_CapsuleTriggered, 0) == 1;
    }

    /// <summary>
    /// 恢复指定应用。返回恢复结果。
    /// </summary>
    public static RestoreResult RestoreApp(int index)
    {
        if (index < 0 || index >= Apps.Length)
            return RestoreResult.InvalidIndex;

        if (IsAppRestored(index))
            return RestoreResult.AlreadyRestored;

        if (IsCapsuleTriggered())
            return RestoreResult.CapsuleTriggered;

        // 标记为已恢复
        string key = $"SysApp_Restored_{index}";
        PlayerPrefs.SetInt(key, 1);

        int count = GetRestoredCount() + 1;
        PlayerPrefs.SetInt(PrefKey_RestoredCount, count);
        PlayerPrefs.Save();

        // 检查是否触发时间胶囊
        if (count >= Apps.Length)
        {
            PlayerPrefs.SetInt(PrefKey_CapsuleTriggered, 1);
            PlayerPrefs.Save();
            return RestoreResult.CapsuleTriggered;
        }

        return RestoreResult.Success;
    }

    /// <summary>
    /// 恢复出厂设置（时间胶囊触发后）。
    /// </summary>
    public static void FactoryReset()
    {
        PlayerPrefs.DeleteKey(PrefKey_RestoredCount);
        PlayerPrefs.DeleteKey(PrefKey_CapsuleTriggered);
        for (int i = 0; i < Apps.Length; i++)
            PlayerPrefs.DeleteKey($"SysApp_Restored_{i}");
        PlayerPrefs.Save();
    }

    public enum RestoreResult
    {
        Success,
        AlreadyRestored,
        InvalidIndex,
        CapsuleTriggered
    }
}
