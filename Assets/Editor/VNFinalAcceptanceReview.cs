using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RailwayRenaissance.UI.Tablet.Apps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class VNFinalAcceptanceReview
{
    const string Pending = "VNFinalAcceptanceReview.Pending", Output = "tools/vn/final_review";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static VNManager manager;
    static UIDocument document, archiveDocument;
    static VisualElement tabletContent;
    static VNViewportLayout viewport;
    static RenderTexture target;
    static int stage, screen;
    static double deadline, next;
    static string bookmarkId, deletedId;
    static int audioReadBaseline;
    static TypewriterEffect performanceTypewriter;
    static readonly List<string> checks = new List<string>(), errors = new List<string>(), editorDiagnostics = new List<string>();
    static readonly (string name, int width, int height, bool touch, float dpi, Rect safe)[] Screens = {
        ("windows_2_5k",2560,1440,false,96,new Rect(0,0,2560,1440)),
        ("windows_720p",1280,720,false,96,new Rect(0,0,1280,720)),
        ("windows_4_3",1024,768,false,96,new Rect(0,0,1024,768)),
        ("android_pad_2_5k",2560,1600,true,280,new Rect(0,32,2560,1568)),
        ("android_pad_4_3",2048,1536,true,264,new Rect(0,48,2048,1488))
    };
    static object Get(string name) => typeof(VNManager).GetField(name, Hidden).GetValue(manager);
    static void Set(string name, object value) => typeof(VNManager).GetField(name, Hidden).SetValue(manager, value);
    static void Call(string name, params object[] args) => typeof(VNManager).GetMethod(name, Hidden).Invoke(manager, args);
    static void Require(bool result, string reason) { if (!result) throw new InvalidOperationException(reason); }
    static void Passed(string message) { checks.Add(message); Debug.Log(message); }

    static VNFinalAcceptanceReview()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        deadline = EditorApplication.timeSinceStartup + 150;
        EditorApplication.update += Step;
        Application.logMessageReceived += Log;
    }
    public static void Batch()
    {
        Require(Application.dataPath.Replace('\\','/').EndsWith("/VNCompiledReviewProject/Assets"), "Isolated project required");
        Directory.CreateDirectory(Output);
        Directory.CreateDirectory("Assets/FinalReview");
        var replay = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("VN_TestManager").AddComponent<VNManager>();
        new GameObject("AudioListener").AddComponent<AudioListener>();
        EditorSceneManager.SaveScene(replay, "Assets/FinalReview/VN_Test.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/FinalReview/VN_Test.unity", true) };
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PlayerSettings.companyName = "CodexVNReview";
        PlayerSettings.productName = "IsolatedFinal20261103";
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }
    static void Log(string message, string trace, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
        if (trace.Contains("UnityEditor.Search.SearchDatabase") && trace.Contains("UnityEditor.Search.SearchInit.IndexationOnStartup"))
            editorDiagnostics.Add(message + "\n" + trace);
        else errors.Add(message + "\n" + trace);
    }
    static void Submit(Button button)
    {
        Require(button != null && button.enabledInHierarchy, "Missing/disabled button");
        using (var submit = NavigationSubmitEvent.GetPooled()) { submit.target = button; button.SendEvent(submit); }
    }
    static void Tap(Button button)
    {
        Require(button != null && button.enabledInHierarchy, "Missing touch button");
        var touch = new Touch { fingerId = 0, position = button.worldBound.center, phase = TouchPhase.Began };
        using (var down = PointerDownEvent.GetPooled(touch, EventModifiers.None)) { down.target = button; button.SendEvent(down); }
        touch.phase = TouchPhase.Ended;
        using (var up = PointerUpEvent.GetPooled(touch, EventModifiers.None)) { up.target = button; button.SendEvent(up); }
    }
    static void Setup()
    {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        PlayerPrefs.DeleteAll(); // Dedicated isolated product namespace set before Play.
        PlayerPrefs.SetInt("TextSpeed",10);
        PlayerPrefs.SetInt("RetroLowResBG",0);
        new GameObject("ReviewListener").AddComponent<AudioListener>();
        var host = new GameObject("FinalReviewManager"); host.SetActive(false);
        manager = host.AddComponent<VNManager>(); manager.enabled = false; host.SetActive(true);
        Call("SetupCanvas");
        document = (UIDocument)Get("uiDoc");
        document.panelSettings = UnityEngine.Object.Instantiate(document.panelSettings);
        document.panelSettings.clearColor = true;
        document.panelSettings.colorClearValue = Color.black;
        ((BackgroundManager)Get("backgroundManager")).SetBackgroundImmediate("cabin_interior");
        viewport = host.GetComponent<VNViewportLayout>(); viewport.UseScreenMetrics = false;
        Set("VN_ReplayInjected",false);
        manager.SetVariable("trip_activity_story_first",true);
        manager.SetVariable("trip_activity_music_first",false);
        Set("currentScript",new JSONParser().LoadScript("prologue_03_journey"));
        Set("currentScriptName","prologue_03_journey"); Set("currentSceneIndex",5); Set("currentDialogueIndex",17); Set("isScriptRunning",true);
        Call("ShowCurrentDialogue");
        ConfigureScreen();
    }
    static void ConfigureScreen()
    {
        var s = Screens[screen];
        if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
        target = new RenderTexture(s.width,s.height,24); target.Create();
        document.panelSettings.targetTexture = target;
        document.rootVisualElement.style.width = s.width * 800f / s.height;
        document.rootVisualElement.style.height = 800;
    }
    static void ApplyViewport()
    {
        var s = Screens[screen]; viewport.Apply(s.safe,new Vector2(s.width,s.height),s.dpi,s.touch);
    }
    static void Capture(UIDocument doc, string name)
    {
        var prior = RenderTexture.active; RenderTexture.active = target;
        var image = new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
        File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image); RenderTexture.active = prior;
    }
    static void CheckTextAndPortraits()
    {
        var dialogue = (DialogueBox)Get("dialogueBox");
        float available = (float)typeof(DialogueBox).GetMethod("AvailableTextWidth",Hidden).Invoke(dialogue,null);
        int lines = 0;
        string longest = "";
        foreach (var asset in Resources.LoadAll<TextAsset>("Scripts").Where(asset => asset.name.StartsWith("prologue_")))
        foreach (var scene in JsonUtility.FromJson<ScriptData>(asset.text).scenes)
        foreach (var entry in scene.d)
        {
            if (entry.t != "n" && entry.t != "d" && entry.t != "thought") continue;
            if (entry.text.Length > longest.Length) longest = entry.text;
            if (entry.t == "thought") Require(!string.IsNullOrEmpty(entry.s) && !string.IsNullOrEmpty(entry.e) &&
                entry.text.StartsWith("[") && entry.text.EndsWith("]"), "Invalid thought schema");
            string text = string.IsNullOrEmpty(entry.s) ? entry.text : "「" + entry.text + "」";
            var args = new object[] { text,available,0 };
            string wrapped = (string)typeof(DialogueBox).GetMethod("FitAndWrap",Hidden).Invoke(dialogue,args);
            int rows = wrapped.Split('\n').Length, fs = (int)args[2];
            Require(rows * fs + (rows-1)*12 <= 100, "Dialogue clipping " + asset.name + " " + Screens[screen].name);
            lines++;
        }
        var professorLine = new JSONParser().LoadScript("prologue_01_news").scenes.SelectMany(scene=>scene.d)
            .First(entry=>entry.t=="d"&&entry.s=="佯教授");
        dialogue.ShowDialogue(professorLine.s,professorLine.text); dialogue.SkipTyping();
        var characters = (CharacterSpriteManager)Get("characterSpriteManager");
        characters.UpdateDisplay(new[] { new CharacterEntry { name="daoshi",pos="left" },
            new CharacterEntry { name="suiyue",pos="center" },new CharacterEntry { name="jiaying_xu",pos="right" },
            new CharacterEntry { name="lin_biaohan",pos="left" } }, "normal", "佯教授");
        Passed("VN_TEXT_FIT_OK " + Screens[screen].name + " entries=" + lines);
    }
    static void AssertLayout()
    {
        var s=Screens[screen];
        var root=document.rootVisualElement;
        float left=s.safe.xMin*800/s.height, right=s.safe.xMax*800/s.height;
        foreach(string name in new[]{"dialogue-box","menu-bar","character-container"})
        {
            var rect=root.Q<VisualElement>(name).worldBound;
            Require(rect.xMin >= left-1 && rect.xMax <= right+1, "Outside safe bounds: " + name);
        }
        var slots=new[]{"char-left","char-center","char-right"}.Select(name=>root.Q<VisualElement>(name)).ToArray();
        for(int i=0;i<3;i++)
        {
            Require(slots[i].style.backgroundImage.value.texture!=null,"Missing portrait");
            Require(!slots[i].style.backgroundImage.value.texture.name.StartsWith("lin_biaohan"),"Protagonist shown");
            if(i>0)Require(slots[i-1].worldBound.xMax<=slots[i].worldBound.xMin,"Portrait overlap");
        }
        Capture(document,"portraits_"+s.name);
        var choice=new JSONParser().LoadScript("prologue_07_first_repair").scenes[1].d[51];
        Call("ShowOptions",choice.opts,choice.text); ApplyViewport();
    }
    static void CheckChoices()
    {
        var root=document.rootVisualElement;
        var buttons=root.Q<VisualElement>("options-container").Children().OfType<Button>().ToArray();
        Require(buttons.Length==3,"Repair choices missing");
        foreach(var button in buttons)Require(button.worldBound.xMin>=0 && button.worldBound.xMax<=root.contentRect.width &&
            button.worldBound.yMin>=0 && button.worldBound.yMax<=800,"Choice clipped");
        Capture(document,"choices_"+Screens[screen].name);
        Call("HideOptions");
        Passed("VN_VIEWPORT_OK "+Screens[screen].name+" portraits=3 safe-area choices=3");
    }
    static void CheckBookmarkData()
    {
        for(int i=0;i<20;i++)BookmarkManager.AddManual("prologue_03_journey","边境危机",3,5,17,"连续添加",manager.CaptureVariableState());
        var burst=BookmarkManager.GetAllManual();
        Require(burst.Count==20 && burst.Select(b=>b.id).Distinct().Count()==20,"Bookmark ID collision");
        Require(burst.All(b=>System.Text.RegularExpressions.Regex.IsMatch(b.id,@"^bm_\d{17}$")),"Backup ID compatibility broken");
        foreach(var bm in burst)BookmarkManager.RemoveManual(bm.id);
        var chapters=Resources.LoadAll<TextAsset>("Scripts").Where(asset=>asset.name.StartsWith("prologue_")).OrderBy(asset=>asset.name).Take(7).ToArray();
        foreach(var chapter in chapters)BookmarkManager.UpdateAutoBookmark(chapter.name,"滚动检查",1,0,0,"自动",manager.CaptureVariableState());
        Require(BookmarkManager.GetAllAuto().Count==5&&!BookmarkManager.GetAllAuto().Any(b=>b.scriptName==chapters[0].name),"Auto slots did not roll");
        foreach(var bm in BookmarkManager.GetAllAuto())BookmarkManager.ClearAutoBookmark(bm.scriptName);
        BookmarkManager.UpdateAutoBookmark("prologue_01_news","广播",1,0,0,"旧",manager.CaptureVariableState());
        BookmarkManager.UpdateAutoBookmark("prologue_03_journey","边境",3,5,17,"新",manager.CaptureVariableState());
        Require(BookmarkManager.FindIncompleteAuto().scriptName=="prologue_03_journey","Resume did not choose latest");
        BookmarkManager.MarkAutoCompleted("prologue_03_journey");
        BookmarkManager.UpdateAutoBookmark("prologue_03_journey","边境",3,5,17,"重读",manager.CaptureVariableState());
        Require(!BookmarkManager.GetAllAuto().First().isCompleted,"Re-read auto bookmark remained completed");
        BookmarkManager.ClearAutoBookmark("prologue_01_news"); BookmarkManager.ClearAutoBookmark("prologue_03_journey");
        Require(!BookmarkManager.JumpToBookmark(new BookmarkManager.Bookmark{scriptName="missing",sceneIndex=-1}),"Invalid bookmark accepted");
        Call("HideOptions"); Set("currentSceneIndex",5);Set("currentDialogueIndex",17);Call("ShowCurrentDialogue");
        Tap(document.rootVisualElement.Q<Button>("vn-menu-btn"));
    }
    static void CheckBookmarkButton()
    {
        Require(document.rootVisualElement.Q<VisualElement>("menu-expanded").style.display==DisplayStyle.Flex,"Touch did not open Menu");
        Tap(document.rootVisualElement.Q<Button>("vn-bookmark-btn"));
        Require(BookmarkManager.GetAllManual().Count==1,"Touch did not add bookmark");
        Require((int)Get("currentDialogueIndex")==17,"Bookmark touch advanced dialogue");
        var saved=BookmarkManager.GetAllManual().Single();bookmarkId=saved.id;
        Require(saved.variables.Any(v=>v.name=="trip_activity_story_first"&&v.value),"Bookmark lost variables");
        BookmarkManager.AddManual("prologue_03_journey","边境危机",3,5,17,"只删除此项",manager.CaptureVariableState());
        deletedId=BookmarkManager.GetAllManual().First().id;
        Passed("VN_BOOKMARK_DATA_AND_TOUCH_OK burst=20 unique; newest auto; re-read; invalid; add without advancing");
        var tabletHost = new GameObject("TabletBookmarkReview");
        archiveDocument = tabletHost.AddComponent<UIDocument>();
        archiveDocument.panelSettings = UnityEngine.Object.Instantiate(document.panelSettings);
        archiveDocument.panelSettings.clearColor = true;
        archiveDocument.panelSettings.colorClearValue = Color.black;
        archiveDocument.panelSettings.targetTexture = target;
        archiveDocument.rootVisualElement.style.width = Screens[screen].width * 800f / Screens[screen].height;
        archiveDocument.rootVisualElement.style.height = 800;
        tabletContent = new VisualElement { name = "tablet-app-content" };
        tabletContent.style.flexGrow = 1;
        archiveDocument.rootVisualElement.Add(tabletContent);
        TabletReturnJourneyUI.BuildReturnJourneyApp(tabletContent);
        Submit(tabletContent.Q<Button>("return-journey-bookmarks"));
    }
    static void BeginPerformanceCheck()
    {
        manager.enabled=false; manager.StopAllCoroutines();
        ((DialogueBox)Get("dialogueBox")).Hide();
        GameData.TypewriterVolume=0.12f;
        audioReadBaseline=GameData.AudioPreferenceReadCount;
        performanceTypewriter=manager.GetComponent<TypewriterEffect>();
        performanceTypewriter.StartTypewriter(new string('字',200),()=>{});
        var characters=(CharacterSpriteManager)Get("characterSpriteManager");
        characters.ClearSpriteCache();
        var missing=new[]{new CharacterEntry{name="__missing_sprite_fixture",pos="left"}};
        for(int i=0;i<80;i++)characters.UpdateDisplay(missing,"angry","__missing_sprite_fixture");
        Require(characters.ResourceLookupCount==4,"Missing sprite repeated native lookups");
        var fallback=new[]{new CharacterEntry{name="daoshi",pos="center"}};
        for(int i=0;i<80;i++)characters.UpdateDisplay(fallback,"__missing_expression_fixture","佯教授");
        Require(characters.ResourceLookupCount==7,"Missing expression repeated native lookups");
        characters.ClearSpriteCache();characters.UpdateDisplay(fallback,"normal","佯教授");
        Require(characters.ResourceLookupCount==1,"Cache invalidation failed");
        Passed("VN_NEGATIVE_SPRITE_CACHE_OK 80 missing lines=4 lookups; 80 fallback lines=3 additional; explicit invalidation reloads");
    }
    static void CheckPerformanceAndMigration()
    {
        Require(!performanceTypewriter.IsTyping,"200-character typewriter did not finish");
        Require(GameData.AudioPreferenceReadCount==audioReadBaseline,"Typewriter performed per-character audio preference reads");
        GameData.TypewriterVolume=0.23f;Require(Mathf.Approximately(GameData.TypewriterVolume,0.23f),"Live volume setter stale");
        PlayerPrefs.SetFloat("TypewriterVolume",0.45f);GameData.ApplyVolume();
        Require(Mathf.Approximately(GameData.TypewriterVolume,0.45f),"Audio reload stale");
        Passed("VN_AUDIO_CACHE_OK actual 200-character typewriter=0 audio preference reads; live setter and explicit reload verified");
        Require(Type.GetType("TitleArchiveUI, Assembly-CSharp")==null&&Type.GetType("MainStoryUI, Assembly-CSharp")==null,"Legacy UI remains compiled");
        PlayerPrefs.SetInt("ArchiveCG_cg_tea_meet",1);
        Require(TabletGalleryAppUI.IsGalleryPhotoUnlocked("cg/cg_tea_meet"),"Existing CG unlock lost");
        Require(TabletGalleryAppUI.MasterGalleryItems.Count(item=>item.resId.StartsWith("cg/"))==7,"Prologue CGs missing from new gallery");
        foreach(var scene in StoryArchiveCatalog.SceneEntries)
        {
            Require(TabletGalleryAppUI.MasterGalleryItems.Any(item=>item.resId==scene.id),"Legacy scene not migrated");
            Require(TabletGalleryAppUI.LoadGalleryTexture(scene.id)!=null,"Legacy scene texture missing: "+scene.id);
        }
        foreach(var entry in StoryArchiveCatalog.MusicEntries)Require(TabletMusicAppUI.TabletMusicList.Any(item=>item.id==entry.id),"Legacy music missing");
        PlayerPrefs.SetInt("VN_Got0721",1);
        Require(!TabletReturnJourneyUI.IsStoryEpisodeCompleted(9),"0721 receipt marked entire prologue completed");
        StoryArchiveProgress.CompleteEpisode("prologue_01_news");
        Require(TabletReturnJourneyUI.IsStoryEpisodeCompleted(0),"Episode completion not recorded");
        foreach(var entry in StoryArchiveCatalog.Characters)PlayerPrefs.SetInt(entry.prefsKey,1);
        foreach(var entry in StoryArchiveCatalog.Trains)PlayerPrefs.SetInt(entry.prefsKey,1);
        var view=new VisualElement();
        document.rootVisualElement.Add(view);
        TabletNotesAppUI.BuildWikiApp(view);
        var labels=view.Query<Label>().ToList();
        foreach(var item in StoryArchiveCatalog.Characters.Concat(StoryArchiveCatalog.Trains))Require(labels.Any(label=>label.text==item.name),"Encyclopedia record missing: "+item.name);
        TabletNotesAppUI.BuildNotesApp(view);
        Submit(view.Q<Button>("notes-achievements"));
        foreach(var achievement in AchievementManager.GetAll())Require(view.Q("achievement-"+achievement.id)!=null,"Achievement missing");
        TabletRDAAppUI.BuildRDAApp(view);
        Submit(view.Q<Button>("rda-statistics"));
        Require(view.Query<Label>().ToList().Any(label=>label.text.StartsWith("累计游戏时长")),"Statistics not migrated to RDA");
        Passed("VN_ARCHIVE_MIGRATION_DATA_OK legacy UI types absent; 7 CGs, all scene/music/catalogue records, achievements and statistics present in tablet apps");
        GameMainUI.OpenTabletApp("归途");
        archiveDocument=(UIDocument)typeof(GameMainUI).GetField("uiDoc",Hidden).GetValue(GameMainUI.Instance);
        archiveDocument.panelSettings=UnityEngine.Object.Instantiate(archiveDocument.panelSettings);
        archiveDocument.panelSettings.targetTexture=target;
        archiveDocument.panelSettings.clearColor=true;archiveDocument.panelSettings.colorClearValue=Color.black;
        archiveDocument.rootVisualElement.style.width=Screens[screen].width*800f/Screens[screen].height;
        archiveDocument.rootVisualElement.style.height=800;
    }
    static void Step()
    {
        if(EditorApplication.isCompiling||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+0.18;
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Final review timeout stage="+stage);
            switch(stage++)
            {
                case 0:Setup();break;
                case 1:ApplyViewport();break;
                case 2:CheckTextAndPortraits();next=EditorApplication.timeSinceStartup+2.0;break;
                case 3:AssertLayout();break;
                case 4:CheckChoices();if(++screen<Screens.Length){ConfigureScreen();stage=1;}else{screen=Screens.Length-1;CheckBookmarkData();}break;
                case 5:CheckBookmarkButton();break;
                case 6:
                    Require(tabletContent.Q("tablet-bookmarks") != null,"Bookmarks missing from new Return Journey app");break;
                case 7:
                    Capture(archiveDocument,"bookmarks_android");
                    Tap(archiveDocument.rootVisualElement.Q<Button>("bookmark-delete-"+deletedId));break;
                case 8:
                    var confirmation=archiveDocument.rootVisualElement.Q<VisualElement>("bookmark-delete-confirm");
                    Require(confirmation!=null,"Delete confirmation missing");Submit(confirmation.Query<Button>().ToList().Single(b=>b.text=="取消"));
                    Require(BookmarkManager.GetAllManual().Count==2,"Cancel deleted bookmark");
                    Submit(archiveDocument.rootVisualElement.Q<Button>("bookmark-delete-"+deletedId));break;
                case 9:
                    var confirm=archiveDocument.rootVisualElement.Q<VisualElement>("bookmark-delete-confirm");
                    Submit(confirm.Query<Button>().ToList().Single(b=>b.text=="确认删除"));
                    Require(BookmarkManager.GetAllManual().Single().id==bookmarkId,"Delete affected wrong bookmark");
                    Passed("VN_BOOKMARK_DELETE_UI_OK cancel then confirm, other bookmark retained");
                    PlayerPrefs.SetInt("VN_ShowLoadUI",1);PlayerPrefs.SetInt("VN_LoadSaveSlot",60);
                    Submit(archiveDocument.rootVisualElement.Q<Button>("bookmark-jump-"+bookmarkId));break;
                case 10:
                    manager=VNManager.Instance;
                    if(SceneManager.GetActiveScene().name!="VN_Test"||manager==null||!(bool)Get("isScriptRunning")){stage=10;break;}
                    Require((int)Get("currentSceneIndex")==5&&(int)Get("currentDialogueIndex")==17,"Replay did not restore exact branch position");
                    Require(manager.GetVariable("trip_activity_story_first")&&!manager.GetVariable("trip_activity_music_first"),"Replay variables wrong");
                    document=(UIDocument)Get("uiDoc");
                    Require(document.rootVisualElement.Q("episode-title-card")==null,"Bookmark replay created duplicate opening/title");
                    Require(!PlayerPrefs.HasKey("VN_ReplayVariables")&&!PlayerPrefs.HasKey("VN_LoadSaveSlot"),"Stale replay/load handoff");
                    Require(GameObject.Find("TabletBookmarkReview") == null,"Tablet fixture stayed over restored VN");
                    Passed("VN_BOOKMARK_SCENE_RESTORE_OK actual VN_Test Start; exact position; true/false flags; no title duplication; tablet app left before replay");
                    Set("VN_ReplayInjected",false);Call("OnApplicationPause",true);
                    Require(BookmarkManager.FindIncompleteAuto()?.dialogueIndex==17,"Pause did not flush auto reading position");
                    SceneManager.LoadScene("VN_Test");break;
                case 11:
                    manager=VNManager.Instance;
                    if(manager==null){stage=11;break;}
                    document=(UIDocument)Get("uiDoc");
                    var resume=document?.rootVisualElement.Q<VisualElement>("resume-dialog");
                    if(resume==null){stage=11;break;}
                    Require(resume.style.display==DisplayStyle.Flex,"Automatic resume prompt missing");
                    Submit(resume.Query<Button>().ToList().Single(b=>b.text=="继续观看"));break;
                case 12:
                    manager=VNManager.Instance;
                    if(manager==null||!(bool)Get("isScriptRunning")){stage=12;break;}
                    Require((int)Get("currentDialogueIndex")==17&&manager.GetVariable("trip_activity_story_first"),"Auto resume lost branch/position");
                    document=(UIDocument)Get("uiDoc");
                    Passed("VN_BOOKMARK_AUTO_RESUME_OK five-slot rotation; pause flush; actual restart prompt and continue button");
                    BeginPerformanceCheck();next=EditorApplication.timeSinceStartup+4.5;break;
                case 13:CheckPerformanceAndMigration();break;
                case 14:
                    if (archiveDocument.rootVisualElement.Q("tablet-bookmarks")==null)
                        Submit(archiveDocument.rootVisualElement.Q<Button>("return-journey-bookmarks"));
                    break;
                case 15:
                    Require(archiveDocument.rootVisualElement.Q("tablet-bookmarks")!=null,"Real tablet shell did not open reading bookmarks");
                    Capture(archiveDocument,"tablet_shell_return_journey_bookmarks");
                    Passed("VN_TABLET_MIGRATION_RUNTIME_OK real GameMainUI launcher -> Return Journey -> bookmarks; legacy UI absent");
                    Finish(true);break;
            }
        }
        catch(Exception ex){Debug.LogException(ex);Finish(false);}
    }
    static void Finish(bool success)
    {
        EditorApplication.update-=Step;Application.logMessageReceived-=Log;SessionState.SetBool(Pending,false);
        bool passed=success&&errors.Count==0;
        File.WriteAllText(Output+"/results.json",JsonUtility.ToJson(new Results{success=passed,checks=checks.ToArray(),errors=errors.ToArray(),editorDiagnostics=editorDiagnostics.ToArray()},true));
        Debug.Log(passed?"VN_FINAL_ACCEPTANCE_OK":"VN_FINAL_ACCEPTANCE_FAILED");
        EditorApplication.isPlaying=false;EditorApplication.Exit(passed?0:1);
    }
    [Serializable]public class Results{public bool success;public string[]checks,errors,editorDiagnostics;}
}
