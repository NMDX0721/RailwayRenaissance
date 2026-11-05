#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace RailwayRenaissance.EditorScripts
{
    public static class MusicServiceBuildPostprocessor
    {
        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.StandaloneWindows && target != BuildTarget.StandaloneWindows64) return;

            string buildDir = Path.GetDirectoryName(pathToBuiltProject);
            if (string.IsNullOrEmpty(buildDir) || !Directory.Exists(buildDir)) return;

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string sourceTools = Path.Combine(projectRoot, "tools", "music");
            if (!Directory.Exists(sourceTools)) return;

            string targetTools = Path.Combine(buildDir, "tools", "music");
            UnityEngine.Debug.Log($"[MusicServiceBuild] 正在同步音乐服务运行时至独立包目录: {targetTools}");

            try
            {
                // 使用 Windows 内置 robocopy 进行多线程高速增量同步
                var psi = new ProcessStartInfo
                {
                    FileName = "robocopy",
                    Arguments = $"\"{sourceTools}\" \"\"{targetTools}\"\" /E /MT:8 /NDL /NFL /NJH /NJS /R:1 /W:1",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (var proc = Process.Start(psi))
                {
                    proc?.WaitForExit(30000);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[MusicServiceBuild] robocopy 增量复制失败，尝试降级复制: {ex.Message}");
            }

            // 同步 node.exe 独立运行时，使独立包无需玩家机器预装 Node
            try
            {
                string targetNode = Path.Combine(buildDir, "node.exe");
                if (!File.Exists(targetNode))
                {
                    string systemNode = null;
                    foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
                    {
                        try
                        {
                            string candidate = Path.Combine(directory.Trim().Trim('"'), "node.exe");
                            if (File.Exists(candidate)) { systemNode = candidate; break; }
                        }
                        catch { }
                    }

                    if (!string.IsNullOrEmpty(systemNode) && File.Exists(systemNode))
                    {
                        File.Copy(systemNode, targetNode, true);
                        UnityEngine.Debug.Log($"[MusicServiceBuild] 已同步独立 node.exe 便携运行时至: {targetNode}");
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[MusicServiceBuild] node.exe 便携复制跳过: {ex.Message}");
            }
        }
    }
}
#endif
