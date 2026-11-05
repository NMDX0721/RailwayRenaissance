using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RailwayRenaissance.UI.Tablet.Account;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Accelerated actual-manager traversal in an isolated project, with a minimal gameplay receiver.
[InitializeOnLoad]
public static class VNPrologueFlowReview
{
    const string Pending = "VNPrologueFlowReview.Pending";
    const string Output = "tools/vn/full_flow_review";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    [Serializable] public class Selection { public int scene, entry, option; }
    [Serializable] public class Case { public string script; public Selection[] selections; }
    [Serializable] public class Cases { public Case[] cases; }
    static VNManager manager;
    static UIDocument document;
    static RenderTexture target;
    static Cases plans;
    static int stage, caseIndex, selectionIndex, steps, chapterCount;
    static double deadline, episodeShown, waitUntil;
    static string chapter;
    static string pendingCapture;
    static Texture2D expectedBackground;
    static bool advanceCase;
    static readonly HashSet<string> captured = new HashSet<string>();
    static readonly List<string> outcomes = new List<string>();
    static readonly List<string> errors = new List<string>();
    static readonly List<string> editorDiagnostics = new List<string>();
    static readonly List<string> visitedChapters = new List<string>();
    static readonly Dictionary<string, HashSet<int>> baselineSelections = new Dictionary<string, HashSet<int>>();
    static object Get(string name) => typeof(VNManager).GetField(name, Hidden).GetValue(manager);
    static void Set(string name, object value) => typeof(VNManager).GetField(name, Hidden).SetValue(manager, value);
    static void Call(string name, params object[] args) => typeof(VNManager).GetMethod(name, Hidden).Invoke(manager, args);
    static void Require(bool result, string reason) { if (!result) throw new InvalidOperationException(reason); }

    static VNPrologueFlowReview()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        deadline = EditorApplication.timeSinceStartup + 600;
        EditorApplication.update += Step;
        Application.logMessageReceived += Log;
    }

    public static void Batch()
    {
        Require(Application.dataPath.Replace('\\', '/').EndsWith("/VNCompiledReviewProject/Assets"), "Isolated project required");
        Directory.CreateDirectory(Output);
        Directory.CreateDirectory("Assets/FlowReview");
        var receiver = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("ProductionUIManagerReceiver").AddComponent<UIManager>();
        new GameObject("ReceiverAudioListener").AddComponent<AudioListener>();
        EditorSceneManager.SaveScene(receiver, "Assets/FlowReview/StationSlice_V1.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/FlowReview/StationSlice_V1.unity", true) };
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // Different PlayerPrefs namespace from the user's main game; never delete the user's saves.
        PlayerSettings.companyName = "CodexVNFlowReview";
        PlayerSettings.productName = "IsolatedPrologue20261103";
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    static void Log(string message, string trace, LogType type)
    {
        if (manager == null && trace.Contains("UnityEditor.Search.SearchDatabase"))
        {
            editorDiagnostics.Add(message);
            return;
        }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            errors.Add(message);
    }

    static void Setup()
    {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        new GameObject("FlowReviewAudioListener").AddComponent<AudioListener>();
        PlayerPrefs.SetInt("RetroLowResBG", 0);
        PlayerPrefs.SetInt("AutoPlay", 0);
        AccountService.ResetForTests();
        Require(AccountService.Register("13800000000", "RailTest!83", "测试旅人", 0) == null, "Fixture account registration failed");
        // This run covers already-registered TABLET_SETUP resumption; registration UI is separate.
        plans = JsonUtility.FromJson<Cases>(Resources.Load<TextAsset>("vn_flow_cases").text);
        Require(plans.cases.Length == 25, "Expected 25 option cases");
        var host = new GameObject("FlowReviewVNManager");
        host.SetActive(false);
        manager = host.AddComponent<VNManager>();
        manager.enabled = false;
        host.SetActive(true);
        Call("SetupCanvas");
        document = (UIDocument)Get("uiDoc");
        document.panelSettings = UnityEngine.Object.Instantiate(document.panelSettings);
        target = new RenderTexture(2560, 1440, 24);
        target.Create();
        document.panelSettings.targetTexture = target;
        document.rootVisualElement.style.width = 1422.2222f;
        document.rootVisualElement.style.height = 800;
        Set("openingLogoShown", true);
        Set("VN_ReplayInjected", true);
        BeginCase();
    }

    static void BeginCase()
    {
        selectionIndex = 0;
        manager.StartScript(plans.cases[caseIndex].script);
    }

    static void Click(VisualElement element)
    {
        using (var click = ClickEvent.GetPooled()) { click.target = element; element.SendEvent(click); }
    }

    static void Capture(string name)
    {
        if (!captured.Add(name)) return;
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        image.Apply();
        File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
        RenderTexture.active = previous;
    }

    static void Submit(OptionData option, string key)
    {
        var buttons = document.rootVisualElement.Q<VisualElement>("options-container").Children().OfType<Button>().ToArray();
        var button = buttons.Single(b => b.text == option.text);
        Require(button.enabledInHierarchy, "Disabled option " + key);
        var before = manager.CaptureVariableState();
        int si = (int)Get("currentSceneIndex"), di = (int)Get("currentDialogueIndex");
        string script = (string)Get("currentScriptName");
        var data = (ScriptData)Get("currentScript");
        var saves = new VNSaveSystem();
        var scene = data.scenes[si];
        Require(saves.SaveGame(60, script, si, di, scene.bg, scene.bgm, before), "Save failed " + key);
        manager.SetVariable("review_sentinel", true);
        Call("LoadFromSave", saves.LoadGame(60));
        Require(!manager.GetVariable("review_sentinel"), "Save did not restore variables " + key);
        Require(manager.CaptureVariableState().Length == before.Length, "Variable count mismatch " + key);
        buttons = document.rootVisualElement.Q<VisualElement>("options-container").Children().OfType<Button>().ToArray();
        button = buttons.Single(b => b.text == option.text);
        using (var submit = NavigationSubmitEvent.GetPooled()) { submit.target = button; button.SendEvent(submit); }
        Require((int)Get("currentSceneIndex") == option.next, "Wrong target scene " + key);
        Require((int)Get("currentDialogueIndex") >= option.nextDialogue, "Wrong target entry " + key);
        var destination = data.scenes[option.next];
        if (si != option.next && !string.IsNullOrEmpty(destination.bg))
        {
            string resource = destination.bg == "cabin_interior" ? "cabin_interior_night" :
                destination.bg == "cabin_interior_night" ? "cabin_interior" : destination.bg;
            var expected = Resources.Load<Texture2D>("bg/" + resource);
            expectedBackground = expected;
        }
        waitUntil = EditorApplication.timeSinceStartup + 1.0;
        outcomes.Add(key);
        Debug.Log("VN_OPTION_RUNTIME_OK " + key);
    }

    static bool WalkOne(bool baseline)
    {
        if ((bool)Get("episodeCardActive")) return false;
        var script = (ScriptData)Get("currentScript");
        if (script == null)
        {
            Require(baseline, "Target option case ended before reaching its option");
            var overlay = document.rootVisualElement.Q<VisualElement>("episode-clear");
            Require(overlay != null, "Missing chapter transition");
            if (episodeShown == 0) { episodeShown = EditorApplication.timeSinceStartup; return false; }
            // Exercise both transition phases across repeated reuse of the same overlay.
            if (EditorApplication.timeSinceStartup - episodeShown < (chapterCount % 2 == 0 ? 2.1 : 0.1)) return false;
            Click(overlay);
            Require(document.rootVisualElement.Query<VisualElement>("episode-title-card").ToList().Count == 1,
                "One chapter click started multiple scripts");
            chapterCount++;
            episodeShown = 0;
            return false;
        }
        string name = (string)Get("currentScriptName");
        if (baseline && name != chapter)
        {
            chapter = name;
            visitedChapters.Add(name);
            Debug.Log("VN_CHAPTER_RUNTIME_ENTER " + name);
            if (name == "prologue_08_first_run")
                Require(manager.GetVariable("repair_choice_zhang"), "Previous chapter repair choice was lost");
        }
        int si = (int)Get("currentSceneIndex"), di = (int)Get("currentDialogueIndex");
        var entry = script.scenes[si].d[di];
        var dialogue = (DialogueBox)Get("dialogueBox");
        dialogue.SkipTyping();
        if (entry.t == "c")
        {
            string key = name + "/" + si + "/" + di;
            if (!captured.Contains("choice_" + name + "_" + si + "_" + di))
            {
                if (pendingCapture != key)
                {
                    pendingCapture = key;
                    waitUntil = EditorApplication.timeSinceStartup + 1.0;
                    return false;
                }
                Capture("choice_" + name + "_" + si + "_" + di);
                return false;
            }
            OptionData option;
            if (baseline)
            {
                if (!baselineSelections.TryGetValue(key, out var chosen)) baselineSelections[key] = chosen = new HashSet<int>();
                var buttons = document.rootVisualElement.Q<VisualElement>("options-container").Children().OfType<Button>().ToArray();
                int index = Array.FindIndex(entry.opts, candidate => buttons.Any(b => b.text == candidate.text) && !chosen.Contains(Array.IndexOf(entry.opts, candidate)));
                Require(index >= 0, "Baseline revisited exhausted choice " + key);
                chosen.Add(index);
                option = entry.opts[index];
            }
            else
            {
                var selected = plans.cases[caseIndex].selections[selectionIndex++];
                Require(selected.scene == si && selected.entry == di, "Runtime differs from planned prefix " + key);
                option = entry.opts[selected.option];
            }
            Submit(option, key + "/" + Array.IndexOf(entry.opts, option));
            if (!baseline && selectionIndex == plans.cases[caseIndex].selections.Length)
            {
                caseIndex++;
                advanceCase = true;
                return false;
            }
        }
        else if (entry.t == "scroll") ((FullScreenNews)Get("fullScreenNews")).Close();
        else if (entry.t == "cg") Click(document.rootVisualElement.Q<VisualElement>("cg-screen"));
        else manager.NextDialogue();
        return entry.t != "c";
    }

    static void Step()
    {
        if (EditorApplication.isCompiling || !EditorApplication.isPlaying) return;
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Walkthrough timeout stage=" + stage);
            if (stage == 0) { Setup(); stage = 1; return; }
            if (stage == 3)
            {
                if (SceneManager.GetActiveScene().name != "StationSlice_V1" || PlayerPrefs.HasKey("VNExitData")) return;
                Require(GameData.Day == 1 && CrewManager.GetAllCrew().Count == 5, "Gameplay receiver data mismatch");
                Require(visitedChapters.Count == 10 && chapterCount == 9, "Chapter chain incomplete");
                Debug.Log("VN_TEN_CHAPTERS_RUNTIME_OK chapters=10 transitions=9; accelerated real manager traversal");
                Debug.Log("VN_GAMEPLAY_RECEIVER_OK scene loaded; production UIManager consumed VNExitData; day=1 crew=5; minimal scene fixture");
                Finish(true);
                return;
            }
            if (EditorApplication.timeSinceStartup < waitUntil) return;
            if (expectedBackground != null)
            {
                Require(new[] { "vn-bg-old", "vn-bg-new" }.Any(name =>
                    document.rootVisualElement.Q<VisualElement>(name).style.backgroundImage.value.texture == expectedBackground),
                    "Option kept previous scene background");
                expectedBackground = null;
            }
            if (advanceCase)
            {
                advanceCase = false;
                manager.StopAllCoroutines();
                if (caseIndex < plans.cases.Length) BeginCase();
                else
                {
                    stage = 2;
                    manager.StartScript("prologue_01_news");
                    Debug.Log("VN_ALL_25_OPTIONS_RUNTIME_OK actual buttons, pre-choice save/load and scene backgrounds");
                }
                return;
            }
            // Wait a rendered frame at every prompt and chapter rather than only manipulating indices.
            for (int i = 0; i < 12; i++)
            {
                if (++steps > 18000) throw new InvalidOperationException("Unexpected runtime loop");
                if (stage == 2 && (ScriptData)Get("currentScript") is ScriptData current)
                {
                    int si = (int)Get("currentSceneIndex"), di = (int)Get("currentDialogueIndex");
                    if (current.scenes[si].d[di].text == "TRANSITION_TO_GAMEPLAY") { stage = 3; return; }
                    // Special transition executes immediately on entry, so also detect manager shutdown below.
                }
                if (!WalkOne(stage == 2)) { waitUntil = Math.Max(waitUntil, EditorApplication.timeSinceStartup + 0.05); break; }
                if (stage == 2 && !(bool)Get("isScriptRunning") && PlayerPrefs.HasKey("VNExitData")) { stage = 3; return; }
            }
        }
        catch (Exception exception) { Debug.LogException(exception); Finish(false); }
    }

    static void Finish(bool success)
    {
        EditorApplication.update -= Step;
        Application.logMessageReceived -= Log;
        SessionState.SetBool(Pending, false);
        File.WriteAllText(Output + "/results.json", JsonUtility.ToJson(new Results {
            success = success && errors.Count == 0, cases = caseIndex, steps = steps,
            chapters = visitedChapters.ToArray(), options = outcomes.Distinct().ToArray(), errors = errors.ToArray(),
            editorDiagnostics = editorDiagnostics.ToArray() }, true));
        Debug.Log(success && errors.Count == 0 ? "VN_FULL_FLOW_REVIEW_OK" : "VN_FULL_FLOW_REVIEW_FAILED");
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(success && errors.Count == 0 ? 0 : 1);
    }
    [Serializable] public class Results { public bool success; public int cases, steps; public string[] chapters, options, errors, editorDiagnostics; }
}
