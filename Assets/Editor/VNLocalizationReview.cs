using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using RailwayRenaissance.UI.Tablet.Apps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class VNLocalizationReview
{
    const string Pending="VNLocalizationReview.Pending", Output="tools/vn/localization_review";
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static VNManager manager;static DialogueBox box;static UIDocument document;static VisualElement settings;
    static RenderTexture target;static int screen,stage;static double next,deadline;
    static int longCompletionCount;
    static int sizeComparison;
    static void OnLongComplete() { longCompletionCount++; }
    static readonly List<string> checks=new List<string>(),errors=new List<string>(),diagnostics=new List<string>();
    static readonly (string name,int width,int height)[] Screens={ ("windows_2_5k",2560,1440),("android_pad_2_5k",2560,1600),("android_pad_4_3",2048,1536),("windows_720p",1280,720) };
    static VNLocalizationReview()
    {
        if(!SessionState.GetBool(Pending,false))return;
        deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Step;Application.logMessageReceived+=Log;
    }
    public static void Batch()
    {
        Require(Application.dataPath.Replace('\\','/').EndsWith("/VNCompiledReviewProject/Assets"),"Isolated project required");
        Directory.CreateDirectory(Output);PlayerSettings.companyName="CodexVNReview";PlayerSettings.productName="Localization20261103";
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void Require(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
    static void Passed(string text){checks.Add(text);Debug.Log(text);}
    static void Set(string name,object value)=>typeof(VNManager).GetField(name,Hidden).SetValue(manager,value);
    static object Get(string name)=>typeof(VNManager).GetField(name,Hidden).GetValue(manager);
    static void Call(string name)=>typeof(VNManager).GetMethod(name,Hidden).Invoke(manager,null);
    static void Log(string message,string trace,LogType type)
    {
        if(type!=LogType.Exception&&type!=LogType.Assert)return;
        if(trace.Contains("UnityEditor.Search.SearchDatabase"))diagnostics.Add(message+"\n"+trace);else errors.Add(message+"\n"+trace);
    }
    static void Setup()
    {
        PlayerPrefs.DeleteAll();VNLocalization.ClearCache();
        Require(VNSubtitleSettings.PrimaryLanguage=="zh-Hans"&&VNSubtitleSettings.SecondaryLanguage=="none","Fresh defaults");
        Require(VNLocalization.Languages.Select(l=>l.code).SequenceEqual(new[]{"zh-Hans","ja","en"}),"Language registry");
        PlayerPrefs.SetInt("VNJapaneseSubtitles",1);Require(VNSubtitleSettings.SecondaryLanguage=="ja","Legacy migration");VNSubtitleSettings.Reset();
        var script=new JSONParser().LoadScript("vn_localization_sample");var entry=script.scenes[0].d[0];
        string key=VNLocalization.Key(script.id,0,0,entry);
        var line=VNLocalization.Resolve(script.id,key,entry.text);
        Require(line.Text("en").Contains("recording")&&line.Text("ja").Contains("事実"),"Resource translation loading");
        int loads=VNLocalization.ResourceLoadCount;
        for(int i=0;i<80;i++)VNLocalization.Resolve(script.id,key,entry.text);
        Require(VNLocalization.ResourceLoadCount==loads,"Repeated Resources lookup");
        var stale=VNLocalization.Resolve(script.id,key,entry.text+"变更");Require(stale.Text("ja")==null&&stale.Text("en")==null,"Stale source translation accepted");
        VNLocalization.Resolve("missing_locale_sample","missing_locale_sample/id","未翻译");loads=VNLocalization.ResourceLoadCount;
        for(int i=0;i<80;i++)VNLocalization.Resolve("missing_locale_sample","missing_locale_sample/id","未翻译");
        Require(VNLocalization.ResourceLoadCount==loads,"Missing catalogue not negatively cached");
        Require(!VNLocalization.ValidEntry(new VNTranslationEntry{status="ready",text="x",segments=new[]{new VNTranslationSegment{source="错",text="x"}}},"正确"),"Misaligned translation accepted");
        Passed("VN_LOCALIZATION_DATA_OK zh/ja/en registry; stable ID; source hash; segment validation; hit/miss caching");
        new GameObject("Listener").AddComponent<AudioListener>();var host=new GameObject("LocalizationReview");host.SetActive(false);
        manager=host.AddComponent<VNManager>();manager.enabled=false;host.SetActive(true);Call("SetupCanvas");
        document=(UIDocument)Get("uiDoc");box=(DialogueBox)Get("dialogueBox");document.panelSettings=UnityEngine.Object.Instantiate(document.panelSettings);
        document.panelSettings.clearColor=true;document.panelSettings.colorClearValue=Color.black;
        ((BackgroundManager)Get("backgroundManager")).SetBackgroundImmediate("cabin_interior");
        Set("currentScript",script);Set("currentScriptName",script.id);Set("currentSceneIndex",0);Set("currentDialogueIndex",0);Set("isScriptRunning",true);
        Configure();
    }
    static void Configure()
    {
        var s=Screens[screen];if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
        target=new RenderTexture(s.width,s.height,24);target.Create();document.panelSettings.targetTexture=target;
        document.rootVisualElement.style.width=s.width*800f/s.height;document.rootVisualElement.style.height=800;
        sizeComparison=0;VNSubtitleSettings.SetPrimaryLanguage("zh-Hans");VNSubtitleSettings.SetSecondaryLanguage("ja");VNSubtitleSettings.SetSecondaryFontSize(18);
        Set("currentDialogueIndex",0);Call("ShowCurrentDialogue");box.SkipTyping();
    }
    static void CheckPairs(string suffix)
    {
        var root=document.rootVisualElement;var panel=box.RootElement.worldBound;
        var groups=root.Query<VisualElement>().ToList().Where(v=>v.name!=null&&v.name.StartsWith("subtitle-sentence-")&&!v.name.Contains("primary")&&!v.name.Contains("secondary")).ToArray();
        Require(groups.Length==3,"Expected three aligned sentence groups");
        Rect previous=default;
        for(int i=0;i<3;i++)
        {
            var main=root.Q<Label>("subtitle-sentence-primary-"+i);var sub=root.Q<Label>("subtitle-sentence-secondary-"+i);
            Require(!string.IsNullOrWhiteSpace(main.text)&&!string.IsNullOrWhiteSpace(sub.text),"Sentence disappeared");
            Require(sub.worldBound.yMax<=main.worldBound.yMin+1,"Secondary should be above its primary");
            Require(sub.resolvedStyle.unityTextAlign==TextAnchor.UpperCenter,"Secondary not centered within sentence");
            Require(main.resolvedStyle.fontSize>sub.resolvedStyle.fontSize&&main.resolvedStyle.color!=sub.resolvedStyle.color,"Font hierarchy/color missing");
            Require(main.worldBound.xMin>=panel.xMin&&main.worldBound.xMax<=panel.xMax,"Sentence outside panel");
            if(i>0)
            {
                bool sameRow=Mathf.Abs(groups[i].worldBound.yMin-previous.yMin)<2;
                Require(sameRow?Mathf.Abs(groups[i].worldBound.xMin-previous.xMax)<1:groups[i].worldBound.yMin-previous.yMax>=6,"Artificial sentence gap or invalid row spacing");
            }
            previous=groups[i].worldBound;
        }
        Require(panel.yMin>=0&&panel.yMax<=root.worldBound.yMax,"Panel outside screen");
        Require(groups[0].worldBound.yMin>=panel.yMin&&groups[2].worldBound.yMax<=panel.yMax,"Short sample unnecessarily clipped/scrolled");
        if(screen==0)Require(Mathf.Abs(groups[0].worldBound.yMin-groups[1].worldBound.yMin)<2,"Each sentence forced onto a new line");
        Capture(Screens[screen].name+"_"+suffix);Passed("VN_SENTENCE_PAIRS_OK "+Screens[screen].name+" "+suffix);
    }
    static void BuildSettings()
    {
        settings=new VisualElement{name="localization-settings"};settings.style.position=Position.Absolute;
        settings.style.left=settings.style.right=settings.style.top=settings.style.bottom=0;settings.style.backgroundColor=Color.white;
        document.rootVisualElement.Add(settings);
        typeof(TabletSettingsAppUI).GetField("currentSettingsCategory",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,13);
        TabletSettingsAppUI.BuildSettingsPage(settings);
    }
    static void ShowLongFixture()
    {
        var script=new JSONParser().LoadScript("vn_localization_sample");var entry=script.scenes[0].d[0];
        var original=VNLocalization.Resolve(script.id,VNLocalization.Key(script.id,0,0,entry),entry.text);
        string source=string.Concat(Enumerable.Repeat(entry.text,8));
        var line=new VNLocalizedLine(source);
        foreach(string locale in new[]{"ja","en"})
        {
            var sentences=original.Text(locale);var translated=JsonUtility.FromJson<VNTranslationFile>(Resources.Load<TextAsset>("Localization/VN/"+locale+"/"+script.id).text).entries[0];
            line.Add(locale,new VNTranslationEntry{text=string.Concat(Enumerable.Repeat(sentences,8)),segments=Enumerable.Range(0,8).SelectMany(_=>translated.segments).ToArray()});
        }
        longCompletionCount=0;box.OnTypewriterComplete+=OnLongComplete;
        box.ShowDialogue("岁月",source,null,line);box.SkipTyping();
    }
    static void CheckLongFixture()
    {
        var scroll=document.rootVisualElement.Q<ScrollView>("subtitle-sentences");
        Require(scroll!=null&&scroll.verticalScroller.highValue>0,"Long passage not scrollable");
        Require(box.RootElement.worldBound.height<=document.rootVisualElement.worldBound.height*.53f,"Long passage covers screen");
        Require(longCompletionCount==1,"Typewriter completed multiple times");box.OnTypewriterComplete-=OnLongComplete;
        Set("clickedButtonThisFrame",false);
        using(var down=PointerDownEvent.GetPooled()){down.target=scroll;scroll.SendEvent(down);}
        Require((bool)Get("clickedButtonThisFrame"),"Scroll pointer can advance dialogue");
        Passed("VN_LOCALIZATION_LONG_TEXT_OK "+Screens[screen].name+" scroll cap; single completion; scroll input suppression");
    }
    static void CheckSettings()
    {
        var primary=settings.Q<DropdownField>("settings-primary-subtitle-language");
        Require(primary.choices.Contains("English"),"English missing in settings");primary.value="English";
        settings.Q<DropdownField>("settings-secondary-subtitle-language").value="日本語";
        Require(VNSubtitleSettings.PrimaryLanguage=="en"&&VNSubtitleSettings.SecondaryLanguage=="ja","Arbitrary language combination failed");
        Require(settings.Q<Label>("subtitle-preview-primary-0").text.Contains("Five"),"English live preview failed");
        settings.Q<DropdownField>("settings-secondary-subtitle-size").value="大";Require(VNSubtitleSettings.SecondaryFontSize==22,"Size preference failed");
        settings.Q<ScrollView>("settings-right-pane").ScrollTo(settings.Q("settings-subtitle-language-card"));
    }
    static void Capture(string name)
    {
        var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=old;
    }
    static void Step()
    {
        if(EditorApplication.timeSinceStartup>deadline){Finish(false,"Timeout");return;}if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+0.7;
        try
        {
            switch(stage)
            {
                case 0:Setup();stage=1;break;
                case 1:
                    var s=Screens[screen];var viewport=manager.GetComponent<VNViewportLayout>();viewport.UseScreenMetrics=false;
                    viewport.Apply(new Rect(0,32,s.width,s.height-64),new Vector2(s.width,s.height),264,screen==1||screen==2);stage=2;break;
                case 2:
                    if(box.IsTyping()){box.SkipTyping();break;}CheckPairs(sizeComparison==0?"chinese_japanese":"chinese_japanese_20");
                    if(screen==0&&sizeComparison==0){sizeComparison=1;VNSubtitleSettings.SetSecondaryFontSize(20);box.SkipTyping();break;}
                    VNSubtitleSettings.SetSecondaryFontSize(18);VNSubtitleSettings.SetPrimaryLanguage("en");box.SkipTyping();stage=3;break;
                case 3:
                    if(box.IsTyping()){box.SkipTyping();break;}CheckPairs("english_japanese");
                    Require(document.rootVisualElement.Q<Label>("subtitle-sentence-primary-0").text.Contains("recording"),"English primary missing");
                    ShowLongFixture();stage=4;break;
                case 4:
                    if(box.IsTyping()){box.SkipTyping();break;}CheckLongFixture();
                    Set("currentDialogueIndex",1);Call("ShowCurrentDialogue");box.SkipTyping();
                    Require(document.rootVisualElement.Q("subtitle-sentences")==null,"Missing translation retained previous pairs");
                    Set("currentDialogueIndex",2);Call("ShowCurrentDialogue");
                    var buttons=document.rootVisualElement.Q("options-container").Query<Button>().ToList();
                    Require(buttons.Any(b=>b.text=="Talk about the old railway"),"Localized choice missing");
                    BuildSettings();stage=5;break;
                case 5:CheckSettings();stage=6;break;
                case 6:
                    Capture(Screens[screen].name+"_settings");settings.RemoveFromHierarchy();
                    Passed("VN_MULTILINGUAL_UI_OK "+Screens[screen].name+" fallback; choices; English/ja settings; preview");
                    if(++screen==Screens.Length){Finish(true,"");return;}Configure();stage=1;break;
            }
        }
        catch(Exception ex){Debug.LogException(ex);Finish(false,ex.ToString());}
    }
    static void Finish(bool ok,string error)
    {
        bool success=ok&&errors.Count==0;EditorApplication.update-=Step;Application.logMessageReceived-=Log;SessionState.SetBool(Pending,false);
        File.WriteAllText(Output+"/result.json",JsonUtility.ToJson(new Result{success=success,error=error,checks=checks.ToArray(),errors=errors.ToArray(),editorDiagnostics=diagnostics.ToArray()},true));
        Debug.Log(success?"VN_LOCALIZATION_REVIEW_OK":"VN_LOCALIZATION_REVIEW_FAILED");EditorApplication.isPlaying=false;EditorApplication.Exit(success?0:1);
    }
    [Serializable]public class Result{public bool success;public string error;public string[] checks,errors,editorDiagnostics;}
}
