using System;
using UnityEngine;

/// <summary>同步落盘的硬日志：绕过 Unity Debug.Log 的缓冲（进程冻结/崩溃时最后一段日志不丢）。
/// 写入项目根 Logs/exit_trace.log；Console 镜像默认关闭（降噪），设 PlayerPrefs "VerboseLogs"=1 打开。</summary>
public static class TraceLog
{
    private static readonly object _lock = new object();

    /// <summary>控制台日志总开关：TraceLog 与 [SB sync]/[GameMainUI] 等调试日志共用；默认关（只落盘）。</summary>
    public static bool ConsoleVerbose = PlayerPrefs.GetInt("VerboseLogs", 0) == 1;

    public static void L(string msg)
    {
        try
        {
            string path = System.IO.Path.Combine(Application.dataPath, "../Logs/exit_trace.log");
            lock (_lock)
            {
                System.IO.File.AppendAllText(path, DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\n");
            }
        }
        catch { /* 文件被占等：不影响游戏 */ }
        if (ConsoleVerbose) Debug.Log(msg);
    }
}
