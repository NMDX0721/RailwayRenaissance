using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.Editor
{
    /// <summary>
    /// 设置页“文字速度”接入剧情打字机的运行验证：同一段文字在速度 1 与速度 10 下，固定时间内打出的字数必须明显不同，
    /// 且默认档（5）不应比旧实现（0.03 秒/字）更慢。入口：TabletVnSettingsReview.Batch。
    /// 只覆盖 TextSpeed；对话框透明度、自动播放、点击推进需要完整剧情场景，不在此验证。
    /// </summary>
    [InitializeOnLoad]
    public static class TabletVnSettingsReview
    {
        private const string PendingKey = "TabletVnSettingsReview.Pending";
        private const string SavedSpeedKey = "TabletVnSettingsReview.SavedSpeed";
        private const double WindowSeconds = 1.5;

        private static double deadline;
        private static double stageStartedAt;
        private static int stage;
        private static GameObject host;
        private static TypewriterEffect typewriter;
        private static Label label;
        private static int slowLength, defaultLength, fastLength;

        static TabletVnSettingsReview()
        {
            if (SessionState.GetBool(PendingKey, false))
            {
                stage = 0;
                deadline = EditorApplication.timeSinceStartup + 90.0;
                EditorApplication.update += Step;
            }
        }

        [MenuItem("Tools/Tablet/Run VN Settings Review")]
        public static void Batch()
        {
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(SavedSpeedKey, PlayerPrefs.HasKey("TextSpeed") ? PlayerPrefs.GetInt("TextSpeed") : -1);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static string LongText()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 40; i++) sb.Append("雾峰村线的清晨，汽笛声从远处传来。");
            return sb.ToString();
        }

        private static void StartTyping(int speedSetting)
        {
            PlayerPrefs.SetInt("TextSpeed", speedSetting);
            typewriter.StopTyping();
            label.text = string.Empty;
            typewriter.StartTypewriter(LongText(), null);
            stageStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void Finish(bool success, string message)
        {
            EditorApplication.update -= Step;
            SessionState.SetBool(PendingKey, false);
            int saved = SessionState.GetInt(SavedSpeedKey, -1);
            if (saved >= 0) PlayerPrefs.SetInt("TextSpeed", saved); else PlayerPrefs.DeleteKey("TextSpeed");
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            EditorApplication.isPlaying = false;
            if (success)
            {
                Debug.Log("[TABLET_VN_TEXT_SPEED_REVIEW_OK] " + message);
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("[TABLET_VN_TEXT_SPEED_REVIEW_FAILED] " + message);
                EditorApplication.Exit(1);
            }
        }

        private static void Step()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (EditorApplication.timeSinceStartup > deadline)
            {
                Finish(false, "Timed out at stage " + stage);
                return;
            }

            try
            {
                double elapsed = EditorApplication.timeSinceStartup - stageStartedAt;
                switch (stage)
                {
                    case 0:
                        host = new GameObject("TabletVnSettingsReviewHost");
                        typewriter = host.AddComponent<TypewriterEffect>();
                        label = new Label();
                        typewriter.Initialize(label);
                        StartTyping(1);
                        stage = 1;
                        break;

                    case 1:
                        if (elapsed >= WindowSeconds)
                        {
                            slowLength = label.text.Length;
                            StartTyping(5);
                            stage = 2;
                        }
                        break;

                    case 2:
                        if (elapsed >= WindowSeconds)
                        {
                            defaultLength = label.text.Length;
                            StartTyping(10);
                            stage = 3;
                        }
                        break;

                    case 3:
                        if (elapsed >= WindowSeconds)
                        {
                            fastLength = label.text.Length;
                            string detail = $"typed in {WindowSeconds}s: speed1={slowLength}, speed5={defaultLength}, speed10={fastLength}";
                            if (slowLength <= 0) throw new InvalidOperationException("Speed 1 typed nothing; typewriter did not run. " + detail);
                            if (!(defaultLength > slowLength * 2)) throw new InvalidOperationException("Default speed is not clearly faster than speed 1. " + detail);
                            if (!(fastLength > defaultLength * 1.5)) throw new InvalidOperationException("Speed 10 is not clearly faster than the default. " + detail);
                            // 默认档 5 对应旧实现 0.03 秒/字：1.5 秒理论约 50 字，给出宽松下限，防止默认值被意外放慢
                            if (defaultLength < 25) throw new InvalidOperationException("Default speed is slower than the original 0.03s/char. " + detail);
                            Finish(true, detail);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Finish(false, ex.Message);
            }
        }
    }
}
