using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

// Exercises the actual renderers without starting VNManager or touching save/bookmark state.
[InitializeOnLoad]
public static class VNAssetCompletionReview
{
    const string Pending = "VNAssetCompletionReview.Pending";
    const string Output = "tools/vn/unity_review";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static UIDocument document;
    static PanelSettings panel;
    static RenderTexture target;
    static GameObject host, managerHost;
    static VNManager manager;
    static CharacterSpriteManager characters;
    static BackgroundManager background;
    static DialogueBox dialogue;
    static readonly string[] ProfessorEmotions = { "normal", "serious", "gentle", "sad", "smile", "angry" };
    static ScriptData journey;
    static int stage, frames;
    static double deadline;

    static VNAssetCompletionReview()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        deadline = EditorApplication.timeSinceStartup + 120;
        EditorApplication.update += Step;
    }

    public static void Batch()
    {
        if (!Application.dataPath.Replace('\\', '/').EndsWith("/VNCompiledReviewProject/Assets"))
            throw new InvalidOperationException("VN review with save fixtures must run in isolated VNCompiledReviewProject");
        Directory.CreateDirectory(Output);
        // Set import rules only for the new assets owned by this work.
        foreach (string resource in new[] { "bg/night_road", "bg/china_sky", "bg/shop", "bg/border_town", "bg/train_inside", "cg/cg_first_night", "cg/cg_tea_meet", "cg/cg_day0_leave", "cg/cg_chase", "cg/cg_arrest", "cg/cg_team_night", "cg/cg_first_run" }
            .Concat(ProfessorEmotions.Select(emotion => "characters/daoshi_" + emotion)))
        {
            string path = "Assets/Resources/" + resource + ".png";
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Require(importer != null, "Missing importer " + path);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.alphaIsTransparency = true;
            importer.filterMode = resource.StartsWith("cg/") ? FilterMode.Bilinear : FilterMode.Point;
            importer.SaveAndReimport();
            var texture = Resources.Load<Texture2D>(resource);
            Require(texture != null && texture.width > 100, "Resources load failed " + resource);
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    static void Set(string name, object value) => typeof(VNManager).GetField(name, Hidden).SetValue(manager, value);
    static object Get(string name) => typeof(VNManager).GetField(name, Hidden).GetValue(manager);
    static void Call(string name, params object[] args) => typeof(VNManager).GetMethod(name, Hidden).Invoke(manager, args);

    static void Resize(int width, int height)
    {
        if (target != null)
        {
            panel.targetTexture = null;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
        target = new RenderTexture(width, height, 24);
        target.Create();
        panel.targetTexture = target;
        float pixelScale = panel.scale;
        if (panel.scaleMode == PanelScaleMode.ScaleWithScreenSize)
        {
            var reference = panel.referenceResolution;
            pixelScale *= Mathf.Pow(2, Mathf.Lerp(Mathf.Log((float)width / reference.x, 2),
                Mathf.Log((float)height / reference.y, 2), panel.match));
        }
        document.rootVisualElement.style.width = width / pixelScale;
        document.rootVisualElement.style.height = height / pixelScale;
    }

    static VisualElement[] Slots() => new[] { "char-left", "char-center", "char-right" }
        .Select(name => document.rootVisualElement.Q<VisualElement>(name)).ToArray();
    static bool Visible(VisualElement slot) => slot.style.display.value != DisplayStyle.None;

    static void CheckPortraits()
    {
        var other = new CharacterEntry { name = "jiaying_xu", pos = "right" };
        var counterpart = Resources.Load<Texture2D>("characters/jiaying_xu_normal")
            ?? Resources.Load<Texture2D>("characters/jiaying_xu/normal");
        Require(counterpart != null, "Counterpart reference texture missing");
        var protagonistTextures = Resources.LoadAll<Texture2D>("characters")
            .Where(texture => texture.name.StartsWith("lin_biaohan"))
            .Concat(Resources.LoadAll<Texture2D>("characters/lin_biaohan")).ToHashSet();
        Require(protagonistTextures.Count > 0, "Protagonist negative check has no reference textures");
        foreach (string alias in new[] { "lin_biaohan", "林彪悍", "男大学生" })
        {
            characters.UpdateDisplay(new[] { new CharacterEntry { name = alias, pos = "left" }, other }, "serious", "林彪悍");
            Require(Slots().Count(Visible) == 1, "Mixed scene did not retain exactly one counterpart for " + alias);
            var shown = Slots().Single(Visible);
            Require(shown.style.backgroundImage.value.texture == counterpart, "Wrong counterpart texture");
            Require(shown.style.left.keyword == StyleKeyword.Auto && shown.style.right.keyword == StyleKeyword.Auto, "Counterpart not centered");
            characters.UpdateDisplay(new[] { new CharacterEntry { name = alias, pos = "center" } }, "normal", alias);
            Require(Slots().All(slot => !Visible(slot)), "Player or stale counterpart portrait visible for " + alias);
        }
        var mentor = Resources.Load<Texture2D>("characters/daoshi_normal");
        Require(mentor != null, "Mentor portrait missing");
        Require(ProfessorEmotions.Select(emotion => Resources.Load<Texture2D>("characters/daoshi_" + emotion))
            .Where(texture => texture != null).Distinct().Count() == 6, "Professor expressions are missing or not distinct resources");
        foreach (string alias in new[] { "daoshi", "导师", "教授", "佯教授", "佯明远" })
        foreach (string emotion in ProfessorEmotions)
        {
            characters.UpdateDisplay(new[] { new CharacterEntry { name = "lin_biaohan", pos = "left" },
                new CharacterEntry { name = alias, pos = "right" } }, emotion, "导师");
            Require(Slots().Count(Visible) == 1, "Mentor missing or protagonist visible for " + alias);
            var shown = Slots().Single(Visible);
            var expected = Resources.Load<Texture2D>("characters/daoshi_" + emotion);
            Require(shown.style.backgroundImage.value.texture == expected && shown.childCount == 0,
                "Mentor still uses placeholder or wrong portrait: " + alias + "/" + emotion);
        }
        Debug.Log("VN_MENTOR_PORTRAIT_OK five aliases; six distinct expressions; no fallback or placeholder");
        int scripts = 0, displays = 0;
        foreach (string path in Directory.GetFiles("Assets/Resources/Scripts", "prologue_*.json"))
        {
            var script = JsonUtility.FromJson<ScriptData>(File.ReadAllText(path));
            scripts++;
            foreach (var scene in script.scenes)
            foreach (var entry in scene.d)
            {
                if ((entry.t != "d" && entry.t != "thought") || string.IsNullOrEmpty(entry.s)) continue;
                characters.UpdateDisplay(entry.chars != null && entry.chars.Length > 0 ? entry.chars : scene.chars,
                    string.IsNullOrEmpty(entry.e) ? scene.e : entry.e, entry.s, entry.reactions);
                foreach (var slot in Slots().Where(Visible))
                {
                    var texture = slot.style.backgroundImage.value.texture;
                    Require(texture == null || !protagonistTextures.Contains(texture), "Protagonist leaked into " + path);
                }
                displays++;
            }
        }
        Require(scripts == 10 && displays > 1000, "Insufficient prologue coverage");
        characters.ClearAll();
        Debug.Log($"VN_PLAYER_PORTRAITS_HIDDEN_OK scripts={scripts} dialogueConfigurations={displays}; counterpart retained; scene input arrays unchanged");
    }

    static void Show(string name, bool small)
    {
        Call("HideCgScreen");
        Call("ShowCgScreen", name, small);
    }

    static void CheckImage(float fraction)
    {
        var image = document.rootVisualElement.Q<VisualElement>("cg-image");
        Require(image != null, "CG placeholder instead of image");
        var texture = image.style.backgroundImage.value.texture;
        Require(texture != null, "CG has no texture");
        float width = image.resolvedStyle.width, height = image.resolvedStyle.height;
        Require(width > 100 && height > 100, "CG collapsed to zero size");
        Require(width <= target.width * fraction + 2 && height <= target.height * fraction + 2, "CG escaped viewport bounds");
        Require(Mathf.Abs(width / height - (float)texture.width / texture.height) < 0.01f, "CG distorted aspect ratio");
    }

    static void Capture(string name)
    {
        var before = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        image.Apply();
        RenderTexture.active = before;
        File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
    }

    static void CheckMultiplePortraits(int expected)
    {
        var portraits = Slots().Where(Visible).Select(slot => slot.worldBound).OrderBy(rect => rect.xMin).ToArray();
        Require(portraits.Length == expected, "Wrong multi-character count");
        var viewport = document.rootVisualElement.worldBound;
        var dialogueTop = document.rootVisualElement.Q<VisualElement>("dialogue-box").worldBound.yMin;
        for (int index = 0; index < portraits.Length; index++)
        {
            var rect = portraits[index];
            Require(rect.xMin >= viewport.xMin && rect.xMax <= viewport.xMax && rect.yMin >= viewport.yMin,
                "Multi-character portrait clipped");
            Require(rect.yMin + rect.height * 0.4f < dialogueTop, "Dialogue covers portrait face area");
            if (index > 0) Require(rect.xMin - portraits[index - 1].xMax >= 20,
                "Multi-character portraits overlap or have insufficient spacing");
        }
    }

    static void ShowProfessorExpression(string emotion)
    {
        characters.UpdateDisplay(new[] { new CharacterEntry { name = "lin_biaohan", pos = "left" },
            new CharacterEntry { name = "daoshi", pos = "right" } }, emotion, "佯教授");
        var script = JsonUtility.FromJson<ScriptData>(File.ReadAllText("Assets/Resources/Scripts/prologue_01_news.json"));
        var entry = script.scenes.SelectMany(scene => scene.d).First(line => line.t == "d" && line.s == "佯教授" && line.e == emotion);
        dialogue.ShowDialogue(entry.s, entry.text);
        dialogue.SkipTyping();
    }

    static void SetupJourneyChoice()
    {
        journey = JsonUtility.FromJson<ScriptData>(File.ReadAllText("Assets/Resources/Scripts/prologue_03_journey.json"));
        Set("currentScript", journey);
        Set("currentScriptName", "prologue_03_journey");
        Set("jsonParser", new JSONParser());
        Set("characterSpriteManager", characters);
        Set("backgroundManager", background);
        Set("dialogueBox", dialogue);
        Set("isScriptRunning", true);
        Set("VN_ReplayInjected", true);
        Call("SetupOptionsUI");
        manager.ClearVariables();
        background.SetBackgroundImmediate("cabin_interior");
        Set("currentSceneIndex", 5);
        Set("currentDialogueIndex", 8);
        Call("ShowCurrentDialogue");
        manager.NextDialogue();
    }

    static void CheckCabinDayNight()
    {
        const string key = "RetroLowResBG";
        bool existed = PlayerPrefs.HasKey(key);
        int previous = PlayerPrefs.GetInt(key, 0);
        try
        {
            PlayerPrefs.SetInt(key, 0);
            background.SetBackgroundImmediate("cabin_interior");
            Require(document.rootVisualElement.Q<VisualElement>("vn-bg-old").style.backgroundImage.value.texture ==
                Resources.Load<Texture2D>("bg/cabin_interior_night"), "Logical day cabin did not resolve to daylight source");
            background.SetBackgroundImmediate("cabin_interior_night");
            Require(document.rootVisualElement.Q<VisualElement>("vn-bg-old").style.backgroundImage.value.texture ==
                Resources.Load<Texture2D>("bg/cabin_interior"), "Logical night cabin did not resolve to night source");
            Debug.Log("VN_CABIN_DAY_NIGHT_OK semantic script IDs map to corrected legacy source filenames");
        }
        finally
        {
            if (existed) PlayerPrefs.SetInt(key, previous); else PlayerPrefs.DeleteKey(key);
        }
    }

    static void SubmitJourneyChoice(int index, int expectedEntry)
    {
        var choices = document.rootVisualElement.Q<VisualElement>("options-container").Children().OfType<Button>().ToArray();
        Require(choices.Length == 2, "Journey choice did not present both options");
        using (var submit = NavigationSubmitEvent.GetPooled())
        {
            submit.target = choices[index];
            choices[index].SendEvent(submit);
        }
        Require((int)Get("currentSceneIndex") == 5 && (int)Get("currentDialogueIndex") == expectedEntry,
            "Actual option button did not reach its in-scene entry");
    }

    static List<string> WalkJourneyToMerge()
    {
        var lines = new List<string>();
        for (int step = 0; step < 40; step++)
        {
            int sceneIndex = (int)Get("currentSceneIndex"), entryIndex = (int)Get("currentDialogueIndex");
            Require(sceneIndex == 5, "Journey branch left expected scene before merge");
            if (entryIndex == 23) return lines;
            var entry = journey.scenes[sceneIndex].d[entryIndex];
            Require(entry.t != "c", "Unexpected redundant choice inside branch");
            lines.Add(entry.text ?? "");
            dialogue.SkipTyping();
            manager.NextDialogue();
        }
        throw new InvalidOperationException("Journey branch did not merge");
    }

    static void CheckMusicRouteAndSave()
    {
        SubmitJourneyChoice(0, 10);
        Require(manager.GetVariable("trip_activity_music_first") && !manager.GetVariable("trip_activity_story_first"), "Music route flags wrong");
        var lines = new List<string> { journey.scenes[5].d[10].text };
        manager.NextDialogue(); lines.Add(journey.scenes[5].d[11].text);
        manager.NextDialogue();
        const string key = "VN_Save_60";
        bool existed = PlayerPrefs.HasKey(key);
        string previous = PlayerPrefs.GetString(key, "");
        try
        {
            var saves = new VNSaveSystem();
            Require(saves.SaveGame(60, "prologue_03_journey", 5, 12, "cabin_interior", "melancholy",
                manager.CaptureVariableState()), "Isolated branch save failed");
            var loaded = saves.LoadGame(60);
            Require(loaded?.variables != null && loaded.variables.Length >= 2, "Saved variables missing");
            manager.ClearVariables();
            manager.SetVariable("trip_activity_story_first", true);
            Call("LoadFromSave", loaded);
            Require(manager.GetVariable("trip_activity_music_first") && !manager.GetVariable("trip_activity_story_first"), "Load did not restore branch flags");
            Require((int)Get("currentDialogueIndex") == 12, "Load changed saved dialogue index");
            lines.AddRange(WalkJourneyToMerge());
        }
        finally
        {
            if (existed) PlayerPrefs.SetString(key, previous); else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
        Require(lines.Count(line => line.Contains("播放：《千里马")) == 1 &&
            lines.Count(line => line.Contains("检修记录")) == 1, "Music route duplicated or omitted song/story");
        Debug.Log("VN_JOURNEY_MUSIC_SAVE_OK actual UI submit; in-scene jump; save/load restored flags; song/story each once; merged at 5/23");
        Set("currentDialogueIndex", 9);
        Call("ShowCurrentDialogue");
    }

    static void CheckStoryRoute()
    {
        SubmitJourneyChoice(1, 17);
        Require(manager.GetVariable("trip_activity_story_first") && !manager.GetVariable("trip_activity_music_first"), "Re-selected story route retained old music flag");
        var lines = WalkJourneyToMerge();
        Require(lines.Count(line => line.Contains("播放：《千里马")) == 1 &&
            lines.Count(line => line.Contains("检修记录")) == 1, "Story route duplicated or omitted song/story");
        dialogue.SkipTyping();
        Debug.Log("VN_JOURNEY_STORY_OK actual UI submit; re-selection clears opposite flag; song/story each once");
    }

    static void CheckLegacyDefault()
    {
        var legacy = JsonUtility.FromJson<ScriptData>(File.ReadAllText("Assets/Resources/Scripts/prologue_02_day0.json"));
        var option = legacy.scenes[5].d[40].opts[0];
        Require(option.nextDialogue == 0, "Legacy option does not default to entry zero");
        manager.ClearVariables();
        Set("currentScript", legacy);
        Call("SelectOption", option);
        Require((int)Get("currentSceneIndex") == 6 && (int)Get("currentDialogueIndex") == 0, "Legacy scene jump behaviour changed");
        Debug.Log("VN_JOURNEY_LEGACY_OK old JSON without nextDialogue still jumps to entry zero");
    }

    static void Step()
    {
        if (EditorApplication.isCompiling || !EditorApplication.isPlaying) return;
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Review timed out at stage " + stage);
            if (++frames < 18) return;
            frames = 0;
            switch (stage++)
            {
                case 0:
                    host = new GameObject("VNAssetCompletionReviewUI");
                    panel = ScriptableObject.CreateInstance<PanelSettings>();
                    panel.scaleMode = PanelScaleMode.ConstantPixelSize;
                    document = host.AddComponent<UIDocument>();
                    document.panelSettings = panel;
                    Resize(1280, 720);
                    background = host.AddComponent<BackgroundManager>();
                    background.Init(document);
                    CheckCabinDayNight();
                    background.SetBackgroundImmediate("tea_house");
                    characters = host.AddComponent<CharacterSpriteManager>();
                    characters.Init(document);
                    // An inactive host suppresses VNManager.Awake/Start and all game/save setup.
                    managerHost = new GameObject("VNAssetCompletionReviewInactiveManager");
                    managerHost.SetActive(false);
                    manager = managerHost.AddComponent<VNManager>();
                    Set("uiDoc", document);
                    Set("gameFont", Resources.Load<Font>("Fonts/zpix"));
                    CheckPortraits();
                    characters.UpdateDisplay(new[] { new CharacterEntry { name = "lin_biaohan", pos = "left" }, new CharacterEntry { name = "jiaying_xu", pos = "right" } }, "normal", "嘉颖徐");
                    break;
                case 1:
                    Capture("counterpart_without_protagonist");
                    characters.ClearAll();
                    Show("cg_tea_meet", true);
                    break;
                case 2:
                    CheckImage(0.6f); Capture("tea_meet_small_1280");
                    background.SetBackgroundImmediate("cabin_interior_night");
                    Show("cg_first_night", true);
                    break;
                case 3:
                    CheckImage(0.6f); Capture("first_night_small_1280");
                    Resize(1024, 768);
                    break;
                case 4:
                    CheckImage(0.6f); Capture("first_night_small_1024");
                    Show("cg_tea_meet", false);
                    break;
                case 5:
                    CheckImage(1f); Capture("tea_meet_full_1024");
                    Set("currentScript", new ScriptData { scenes = new[] { new SceneData { d = new[] {
                        new DialogueEntry { t = "cg", text = "cg_tea_meet" }, new DialogueEntry { t = "n", text = "继续" } } } } });
                    Set("isScriptRunning", true);
                    Set("VN_ReplayInjected", true);
                    var screen = document.rootVisualElement.Q<VisualElement>("cg-screen");
                    using (var click = ClickEvent.GetPooled()) { click.target = screen; screen.SendEvent(click); }
                    Require((int)Get("currentDialogueIndex") == 1 && screen.style.display.value == DisplayStyle.None, "CG click did not advance and close");
                    Debug.Log("VN_CG_LAYOUT_AND_CLICK_OK small/full at 1280x720 and 1024x768; actual VNManager callback; no saves/bookmarks initialized");
                    Show("cg_day0_leave", false);
                    break;
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                    var remaining = new[] { "cg_day0_leave", "cg_chase", "cg_arrest", "cg_team_night", "cg_first_run" };
                    int index = stage - 7;
                    CheckImage(1f);
                    Capture(remaining[index] + "_full_1024");
                    if (index + 1 < remaining.Length) Show(remaining[index + 1], false);
                    else
                    {
                        Call("HideCgScreen");
                        background.SetBackgroundImmediate("lab");
                        characters.UpdateDisplay(new[] { new CharacterEntry { name = "lin_biaohan", pos = "left" },
                            new CharacterEntry { name = "daoshi", pos = "right" } }, "normal", "导师");
                    }
                    break;
                case 11:
                    Capture("mentor_normal_no_placeholder");
                    var settings = Resources.Load<PanelSettings>("UI/TitleScreenPanelSettings");
                    Require(settings != null, "Production panel settings missing");
                    panel.targetTexture = null;
                    var previousPanel = panel;
                    panel = UnityEngine.Object.Instantiate(settings);
                    document.panelSettings = panel;
                    UnityEngine.Object.DestroyImmediate(previousPanel);
                    Resize(2560, 1440);
                    dialogue = host.AddComponent<DialogueBox>();
                    dialogue.Init(document);
                    dialogue.ShowDialogue("佯教授", "先把线路跑通，再谈模型。去看看那台机器吧。");
                    dialogue.SkipTyping();
                    break;
                case 12:
                    var portrait = Slots().Single(Visible).worldBound;
                    var viewport = document.rootVisualElement.worldBound;
                    var box = document.rootVisualElement.Q<VisualElement>("dialogue-box").worldBound;
                    Require(portrait.xMin >= viewport.xMin && portrait.xMax <= viewport.xMax,
                        "2.5K portrait extends beyond viewport");
                    Require(Mathf.Abs(portrait.center.x - viewport.center.x) < 2, "2.5K counterpart off centre");
                    Require(portrait.yMin >= viewport.yMin && portrait.yMin + portrait.height * 0.4f < box.yMin,
                        "2.5K portrait head clipped or dialogue covers face");
                    Capture("mentor_dialogue_2560x1440");
                    Debug.Log($"VN_2_5K_PORTRAIT_OK 2560x1440 production panel; logical={viewport.size}; portrait={portrait}; dialogue={box}");
                    dialogue.Hide();
                    characters.ClearAll();
                    Show("cg_tea_meet", true);
                    break;
                case 13:
                    Capture("tea_meet_small_2560x1440");
                    Require(document.rootVisualElement.Q<VisualElement>("cg-image").resolvedStyle.width > 100,
                        "2.5K small CG collapsed");
                    Show("cg_first_run", false);
                    break;
                case 14:
                    Capture("first_run_full_2560x1440");
                    Call("HideCgScreen");
                    background.SetBackgroundImmediate("lab");
                    characters.UpdateDisplay(new[] { new CharacterEntry { name = "daoshi", pos = "left" },
                        new CharacterEntry { name = "jiaying_xu", pos = "right" } }, "normal", "导师");
                    dialogue.ShowDialogue("佯教授", "资料都在这里，我们逐项核对。");
                    dialogue.SkipTyping();
                    break;
                case 15:
                    CheckMultiplePortraits(2);
                    Capture("two_character_layout_2560x1440");
                    characters.UpdateDisplay(new[] { new CharacterEntry { name = "daoshi", pos = "left" },
                        new CharacterEntry { name = "suiyue", pos = "center" },
                        new CharacterEntry { name = "jiaying_xu", pos = "right" } }, "normal", "导师");
                    break;
                case 16:
                    CheckMultiplePortraits(3);
                    Capture("three_character_layout_2560x1440");
                    Debug.Log("VN_2_5K_MULTI_CHARACTER_LAYOUT_OK two/three portraits; no overlap; faces above dialogue; layout fixture only");
                    ShowProfessorExpression(ProfessorEmotions[0]);
                    break;
                case 17:
                case 18:
                case 19:
                case 20:
                case 21:
                case 22:
                    int professorIndex = stage - 18;
                    string professorEmotion = ProfessorEmotions[professorIndex];
                    Require(Slots().Single(Visible).style.backgroundImage.value.texture ==
                        Resources.Load<Texture2D>("characters/daoshi_" + professorEmotion), "Wrong professor expression displayed");
                    Capture("professor_" + professorEmotion + "_2560x1440");
                    if (professorIndex + 1 < ProfessorEmotions.Length) ShowProfessorExpression(ProfessorEmotions[professorIndex + 1]);
                    else
                    {
                        Debug.Log("VN_PROFESSOR_SIX_EXPRESSIONS_OK six distinct 2.5K captures; actual prologue dialogue; professor speaker name");
                        SetupJourneyChoice();
                    }
                    break;
                case 23:
                    Require(document.rootVisualElement.Q<Label>("choice-prompt")?.text == journey.scenes[5].d[9].text,
                        "Choice context prompt was not displayed");
                    var firstChoice = document.rootVisualElement.Q<VisualElement>("options-container").Children().OfType<Button>().First();
                    var actor = Slots().Single(Visible).worldBound;
                    Require(firstChoice.worldBound.yMin > actor.yMin + actor.height * 0.4f,
                        "Choice buttons obscure the actor face area");
                    Capture("journey_choice_2560x1440");
                    CheckMusicRouteAndSave();
                    break;
                case 24:
                    CheckStoryRoute();
                    break;
                case 25:
                    Capture("journey_story_merge_2560x1440");
                    CheckLegacyDefault();
                    Finish(true, "VN_ASSET_COMPLETION_REVIEW_OK seven CGs; professor expressions; journey choices and isolated save/load verified");
                    break;
            }
        }
        catch (Exception e) { Debug.LogException(e); Finish(false, "VN_ASSET_COMPLETION_REVIEW_FAILED stage=" + stage); }
    }

    static void Finish(bool success, string message)
    {
        EditorApplication.update -= Step;
        SessionState.SetBool(Pending, false);
        Debug.Log(message);
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(success ? 0 : 1);
    }
}

[InitializeOnLoad]
public static class LegacyLoginStartupReview
{
    const string Pending = "LegacyLoginStartupReview.Pending";
    static double deadline;

    static LegacyLoginStartupReview()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        deadline = EditorApplication.timeSinceStartup + 40;
        EditorApplication.update += Step;
    }

    public static void Batch()
    {
        if (!Application.dataPath.Replace('\\', '/').EndsWith("/VNCompiledReviewProject/Assets"))
            throw new InvalidOperationException("Startup fixture may run only in the isolated VNCompiledReviewProject");
        Directory.CreateDirectory("Assets/StartupReview");
        var target = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("StartupReviewDestination");
        EditorSceneManager.SaveScene(target, "Assets/StartupReview/VN_Test.unity");
        var entry = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("LegacyLoginEntry").AddComponent<LoginManager>();
        EditorSceneManager.SaveScene(entry, "Assets/StartupReview/Login.unity");
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/StartupReview/Login.unity", true),
            new EditorBuildSettingsScene("Assets/StartupReview/VN_Test.unity", true) };
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    static void Step()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (UnityEngine.Object.FindObjectsByType<AutoLoginUI>(FindObjectsInactive.Include).Length != 0)
                throw new InvalidOperationException("Legacy auto-login UI was instantiated");
            if (SceneManager.GetActiveScene().name == "VN_Test")
            {
                if (GameObject.Find("StartupReviewDestination") == null)
                    throw new InvalidOperationException("Wrong startup destination");
                Debug.Log("VN_LEGACY_STARTUP_BYPASS_OK actual LoginManager.Start routed to VN_Test; no AutoLoginUI; isolated destination fixture");
                Finish(0);
            }
            else if (EditorApplication.timeSinceStartup > deadline)
                throw new InvalidOperationException("Startup did not reach VN_Test");
        }
        catch (Exception exception) { Debug.LogException(exception); Finish(1); }
    }

    static void Finish(int result)
    {
        EditorApplication.update -= Step;
        SessionState.SetBool(Pending, false);
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(result);
    }
}
