using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

// Batch review: VN term card placement + backlog icon buttons.
// Run: Unity.exe -batchmode -projectPath <p> -executeMethod VNTermBacklogReview.Batch -logFile tools/compile/vn_review.log
[InitializeOnLoad]
public static class VNTermBacklogReview
{
    const string PendingFile = "tools/compile/vn_review.pending";
    const string BackupFile = "tools/compile/vn_review.prefs_backup";
    static bool IsPending => File.Exists(PendingFile);
    const string Output = "tools/compile/vn_term_review";
    static readonly string[] SavedKeys = { "Bookmark_Auto_0", "Bookmark_Auto_1", "Bookmark_Auto_2", "Bookmark_Auto_3", "Bookmark_Auto_4", "Bookmark_ManualIndex" };
    static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static RenderTexture target;
    static int step; static int frame; static double checkAfter, timeout;

    static VNTermBacklogReview()
    {
        Debug.Log("[VNReview] ctor pending=" + IsPending);
        if (IsPending) { EditorApplication.update += Check; timeout = EditorApplication.timeSinceStartup + 120; }
    }

    public static void Batch()
    {
        Directory.CreateDirectory(Output);
        var backup = new List<string>();
        foreach (var k in SavedKeys) backup.Add(k + "	" + (PlayerPrefs.HasKey(k) ? "1" + PlayerPrefs.GetString(k, "") : "0"));
        if (!File.Exists(BackupFile)) File.WriteAllLines(BackupFile, backup);   // 已有备份＝上次被中断，保留最早的原值
        PlayerPrefs.SetString("VN_ReplayScript", "prologue_01_news");
        PlayerPrefs.SetInt("VN_ReplayScene", 0);
        PlayerPrefs.SetInt("VN_ReplayDialogue", 11);
        PlayerPrefs.DeleteKey("VN_AutoLoad"); PlayerPrefs.DeleteKey("VN_ShowLoadUI"); PlayerPrefs.DeleteKey("VN_LoadSaveSlot");
        PlayerPrefs.Save();
        File.WriteAllText(PendingFile, "1");
        EditorSceneManager.OpenScene("Assets/Scenes/VN_Test.unity");
        EditorApplication.update -= Check; EditorApplication.update += Check;
        timeout = EditorApplication.timeSinceStartup + 120;
        EditorApplication.isPlaying = true;
    }

    static void Finish(int code)
    {
        if (File.Exists(PendingFile)) File.Delete(PendingFile);
        if (File.Exists(BackupFile))
        {
            foreach (var line in File.ReadAllLines(BackupFile))
            {
                int t = line.IndexOf('	'); if (t < 0) continue;
                string k = line.Substring(0, t), b = line.Substring(t + 1);
                if (b.StartsWith("1")) PlayerPrefs.SetString(k, b.Substring(1)); else PlayerPrefs.DeleteKey(k);
            }
            File.Delete(BackupFile);
        }
        PlayerPrefs.Save();
        EditorApplication.update -= Check;
        Debug.Log("[VNReview] finish " + code);
        EditorApplication.Exit(code);
    }

    static void Capture(string name)
    {
        var previous = RenderTexture.active; RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG());
        UnityEngine.Object.Destroy(image);
        Debug.Log("[VNReview] captured " + name);
    }

    static void Check()
    {
        try
        {
            if (Time.frameCount % 120 == 0) Debug.Log("[VNReview] tick step=" + step + " frame=" + Time.frameCount + " vn=" + (VNManager.Instance != null));
            if (EditorApplication.timeSinceStartup > timeout) throw new TimeoutException("VN review timed out at step " + step);
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (EditorApplication.timeSinceStartup < checkAfter || Time.frameCount < frame + 3) return;
            var vn = VNManager.Instance;
            if (vn == null) return;
            switch (step)
            {
                case 0:
                {
                    var uiDoc = (UIDocument)typeof(VNManager).GetField("uiDoc", Hidden)?.GetValue(vn);
                    if (uiDoc == null || uiDoc.panelSettings == null) return;
                    target = new RenderTexture(1920, 1080, 24); target.Create(); uiDoc.panelSettings.targetTexture = target;
                    step++; Wait(1.5); return;
                }
                case 1:
                    var typingBox = (DialogueBox)typeof(VNManager).GetField("dialogueBox", Hidden).GetValue(vn);
                    if (typingBox == null || typingBox.IsTyping()) { Wait(.3); return; }
                    Wait(1.0); step++; return;
                case 2:
                {
                    var box = (DialogueBox)typeof(VNManager).GetField("dialogueBox", Hidden).GetValue(vn);
                    var hits = (List<VisualElement>)typeof(DialogueBox).GetField("_termHits", Hidden).GetValue(box);
                    Debug.Log("[VNReview] term hits: " + hits.Count);
                    if (hits.Count == 0) { Capture("00-no-hits"); throw new Exception("no term hits on line"); }
                    Capture("01-line");
                    string termId = null;
                    foreach (var (s, len, id) in TermHighlightSystem.FindOccurrences("机体抱死了，喷油嘴全炸了。"))
                        if (TermHighlightSystem.GetEntry(id)?.name == "抱死") { termId = id; break; }
                    if (termId == null) throw new Exception("term 抱死 not found in Terms.json");
                    typeof(DialogueBox).GetMethod("OpenTermCard", Hidden).Invoke(box, new object[] { termId, hits[0] });
                    step++; Wait(.6); return;
                }
                case 3:
                    Capture("02-term-card");
                    step++; Wait(.2); return;
                case 4:
                {
                    var box = (DialogueBox)typeof(VNManager).GetField("dialogueBox", Hidden).GetValue(vn);
                    typeof(DialogueBox).GetMethod("CloseTermCard", Hidden).Invoke(box, null);
                    var backlog = (VNBacklog)typeof(VNManager).GetField("vnBacklog", Hidden).GetValue(vn);
                    if (!backlog.IsOpen) backlog.ToggleBacklog();
                    step++; Wait(2.5); return;
                }
                case 5:
                    Capture("03-backlog");
                    Finish(0); return;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[VNReview] FAIL " + ex);
            Finish(1);
        }
    }

    static void Wait(double seconds) { checkAfter = EditorApplication.timeSinceStartup + seconds; frame = Time.frameCount; }
}
