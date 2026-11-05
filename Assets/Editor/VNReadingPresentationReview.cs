using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class VNReadingPresentationReview
{
    const string Pending="VNReadingPresentationReview.Pending", Output="tools/vn/reading_presentation_review";
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static VNManager manager;static DialogueBox box;static UIDocument doc;static VNVoicePlayer voice;
    static RenderTexture target;static Texture2D captureBuffer;static int stage,screen,route;static double next,deadline;
    static readonly List<string> checks=new(),errors=new(),diagnostics=new();
    static readonly (string name,int w,int h)[] Screens={ ("windows_2_5k",2560,1440) };
    static VNReadingPresentationReview()
    {
        if(!SessionState.GetBool(Pending,false))return;
        deadline=EditorApplication.timeSinceStartup+160;EditorApplication.update+=Step;Application.logMessageReceived+=Log;
    }
    public static void Batch()
    {
        Require(Application.dataPath.Replace('\\','/').EndsWith("/VNCompiledReviewProject/Assets"),"Isolated project required");
        Directory.CreateDirectory(Output);PlayerSettings.companyName="CodexVNReview";PlayerSettings.productName="ReadingChrome20261103";
        // This tone is a deterministic playback fixture, never production TTS or a character voice.
        var script=new JSONParser().LoadScript("prologue_02_day0");var entry=script.scenes[5].d[8];
        string dir="Assets/Resources/Voice/ja/"+script.id;Directory.CreateDirectory(dir);
        using(var f=new BinaryWriter(File.Create(dir+"/fixture.wav")))
        {
            int count=44100*12;f.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));f.Write(36+count*2);f.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            f.Write(16);f.Write((short)1);f.Write((short)1);f.Write(44100);f.Write(88200);f.Write((short)2);f.Write((short)16);
            f.Write(System.Text.Encoding.ASCII.GetBytes("data"));f.Write(count*2);for(int i=0;i<count;i++)f.Write((short)(Math.Sin(i*2*Math.PI*440/44100)*1200));
        }
        string key=VNLocalization.Key(script.id,5,8,entry),ja="音声プレイヤーの動作確認です。";
        string translationDir="Assets/Resources/Localization/VN/ja";Directory.CreateDirectory(translationDir);
        File.WriteAllText(translationDir+"/"+script.id+".json",JsonUtility.ToJson(new VNTranslationFile{schemaVersion=1,scriptId=script.id,locale="ja",entries=new[]{new VNTranslationEntry{key=key,sourceHash=VNLocalization.Hash(entry.text),status="ready",text=ja,segments=new[]{new VNTranslationSegment{source=entry.text,text=ja}}}}}));
        File.WriteAllText(dir+"/catalogue.json",JsonUtility.ToJson(new VNVoiceCatalogue{entries=new[]{new VNVoiceLine{key=key,sourceHash=VNLocalization.Hash(entry.text),textHash=VNLocalization.Hash(ja),clip="fixture"}}}));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    public static void BatchPrimary()
    {
        SessionState.SetBool(Pending+".PrimaryOnly",true);Batch();
    }
    static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
    static void Pass(string value){checks.Add(value);Debug.Log(value);}
    static void Set(string name,object value)=>typeof(VNManager).GetField(name,Hidden).SetValue(manager,value);
    static object Get(string name)=>typeof(VNManager).GetField(name,Hidden).GetValue(manager);
    static void Call(string name)=>typeof(VNManager).GetMethod(name,Hidden).Invoke(manager,null);
    static void Log(string message,string trace,LogType type)
    {
        if(type!=LogType.Exception&&type!=LogType.Assert)return;
        if(trace.Contains("UnityEditor.Search.SearchDatabase"))diagnostics.Add(message);else errors.Add(message+"\n"+trace);
    }
    static void Setup()
    {
        PlayerPrefs.SetString("VNVoiceLanguage","ja");VNPresentationSettings.SetSignal(true);VNSubtitleSettings.SetPrimaryLanguage("zh-Hans");VNSubtitleSettings.SetSecondaryLanguage("none");
        new GameObject("Listener").AddComponent<AudioListener>();var host=new GameObject("ReadingReview");host.SetActive(false);
        manager=host.AddComponent<VNManager>();manager.enabled=false;host.SetActive(true);Call("SetupCanvas");
        doc=(UIDocument)Get("uiDoc");box=(DialogueBox)Get("dialogueBox");voice=(VNVoicePlayer)Get("lineVoice");
        doc.panelSettings=UnityEngine.Object.Instantiate(doc.panelSettings);doc.panelSettings.clearColor=true;doc.panelSettings.colorClearValue=Color.black;
        manager.GetComponent<VNViewportLayout>().UseScreenMetrics=false;
        Configure();
    }
    static void Configure()
    {
        var s=Screens[screen];if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
        target=new RenderTexture(s.w,s.h,24);target.Create();doc.panelSettings.targetTexture=target;
        doc.rootVisualElement.style.width=s.w*800f/s.h;doc.rootVisualElement.style.height=800;
        Show("prologue_02_day0",5,8,"cabin_interior");
        Call("ToggleMenuExpanded");
    }
    static void Show(string id,int si,int di,string bg)
    {
        var script=new JSONParser().LoadScript(id);Require(script!=null,"Script missing: "+id);
        Set("currentScript",script);Set("currentScriptName",id);Set("currentSceneIndex",si);Set("currentDialogueIndex",di);Set("isScriptRunning",true);Set("progressShown",0f);
        ((CharacterSpriteManager)Get("characterSpriteManager")).ClearAll();
        ((BackgroundManager)Get("backgroundManager")).SetBackgroundImmediate(bg);Call("ShowCurrentDialogue");box.SkipTyping();Call("UpdateReadingPresentation");
    }
    static void ApplyViewport()
    {
        var s=Screens[screen];manager.GetComponent<VNViewportLayout>().Apply(new Rect(0,0,s.w,s.h),new Vector2(s.w,s.h),264,false);
    }
    static void CheckLayout()
    {
        var root=doc.rootVisualElement;var replay=root.Q<Button>("vn-voice-replay");var bookmark=root.Q<Button>("vn-bookmark-btn");var name=root.Q<Label>("speaker-name");
        Require(voice.HasClip,"Reviewed fixture did not bind");
        Require(replay.worldBound.xMax<=bookmark.worldBound.xMin+1&&bookmark.worldBound.xMax<=name.worldBound.xMin+1,"Replay/bookmark/name overlap");
        Require(root.Q("vn-voice-progress").resolvedStyle.display==DisplayStyle.Flex,"Voice progress hidden");
        var menu=root.Q("menu-expanded");Require(menu.Query<Label>().ToList().All(l=>string.IsNullOrEmpty(l.text)),"Expanded menu still has captions");
        Require(menu.Query<Button>().ToList().Count==3&&root.Q("vn-signal-btn")==null,"Signal toggle must be settings-only");
        Require(Mathf.Abs(root.Q<Button>("vn-auto-btn").worldBound.height-40)<1&&Mathf.Abs(root.Q<Button>("vn-menu-btn").worldBound.height-40)<1,"Toolbar proportions changed");
        var bar=root.Q("progress-bar").worldBound;Require(box.RootElement.worldBound.yMax<=bar.yMin+1,"Landscape overlaps dialogue");
        Require(root.Q("progress-bar").parent==root&&root.Q("dialogue-footer")==null,"Progress must be outside the dialogue panel");
        Require(Mathf.Abs(box.RootElement.worldBound.yMax-bar.yMin)<1&&Mathf.Abs(bar.yMax-root.worldBound.yMax)<1,"Bottom band must be completely filled without vertical gaps");
        var capsule = root.Q("progress-capsule").worldBound;
        Require(capsule.height<=19&&root.Q("progress-cap-left")!=null&&root.Q("progress-cap-right")!=null,"Progress should use thin body and separate end caps");
        Require(Mathf.Abs(capsule.yMin-bar.yMin-4)<1&&Mathf.Abs(bar.yMax-capsule.yMax-4)<1,"Progress needs even vertical padding");
        Require(box.RootElement.worldBound.height>=159&&root.Q("vn-story-signal").worldBound.height>=151,"Dialogue/signal proportions too small");
        Require(menu.Query<Button>().ToList().All(b=>b.worldBound.height<=41),"Expanded menu items too large");
        Require(bar.xMin>=0&&bar.xMax<=root.worldBound.xMax&&bar.yMax<=root.worldBound.yMax,"Route outside screen");
        Require(root.Q("vn-story-signal").worldBound.xMin>=0,"Signal outside safe area");
        if(screen==1||screen==2)Require(bookmark.worldBound.width>=63.5f&&replay.worldBound.width>=63.5f,"Tablet controls too small");
        int before=(int)Get("currentDialogueIndex");Set("clickedButtonThisFrame",false);
        using(var down=PointerDownEvent.GetPooled()){down.target=bookmark;bookmark.SendEvent(down);}
        Require((bool)Get("clickedButtonThisFrame")&&(int)Get("currentDialogueIndex")==before,"Bookmark pointer can advance story");
        box.OnBookmark?.Invoke();Require((int)Get("currentDialogueIndex")==before,"Bookmark changed line");
        voice.Seek(.42f);voice.Pause(true);box.SetVoiceState(true,.42f);
        Capture(Screens[screen].name+"_voice_menu");Pass("VN_READING_LAYOUT_OK "+Screens[screen].name);
        Call("CloseMenuExpanded");
    }
    static void CheckVoice()
    {
        var script=new JSONParser().LoadScript("prologue_02_day0");var entry=script.scenes[5].d[8];
        voice.Pause(false);voice.Replay();voice.Seek(.5f);Require(voice.Progress>.4f&&voice.Progress<.65f,"Voice seek failed");
        voice.Pause(true);Require(voice.IsPending,"Pause lost pending state");voice.Pause(false);voice.Replay();Require(voice.Progress<.1f,"Voice replay failed");
        voice.Pause(true);Set("isAutoPlay",true);
        var timer=(IEnumerator)typeof(VNManager).GetMethod("AutoPlayTimer",Hidden).Invoke(manager,null);
        Require(timer.MoveNext(),"Auto timer missing delay");Require(timer.MoveNext()&&timer.Current==null,"Auto did not wait for voice");Set("isAutoPlay",false);
        entry.text+="源文已改";voice.Bind(script.id,5,8,entry);Require(!voice.HasClip,"Stale audio accepted");
        entry.s="林彪悍";Require(!VNVoicePlayer.CanVoice(entry),"Protagonist voiced");
        entry.s="岁月";entry.t="thought";Require(!VNVoicePlayer.CanVoice(entry),"Thought voiced");
        Show("prologue_02_day0",1,0,"cabin_interior");Require(doc.rootVisualElement.Q("vn-voice-replay").resolvedStyle.display==DisplayStyle.None,"Narration has voice control");
        Show("prologue_02_day0",1,16,"cabin_interior");Require(!voice.HasClip&&doc.rootVisualElement.Q("vn-voice-replay").resolvedStyle.display==DisplayStyle.None,"Protagonist has voice control");
        Show("prologue_02_day0",1,17,"cabin_interior");Require(!voice.HasClip,"Missing audio retained previous clip");
        VNPresentationSettings.SetSignal(false);Call("UpdateReadingPresentation");Require(doc.rootVisualElement.Q("vn-story-signal").style.display==DisplayStyle.None,"Signal switch ineffective");
        VNPresentationSettings.SetSignal(true);Call("UpdateReadingPresentation");
        Pass("VN_VOICE_TRANSPORT_OK fixture bind; replay; seek; pause; Auto wait; stale reject; protagonist/narration/thought/missing suppression; signal toggle");
    }
    static void ShowRoute()
    {
        var profile=VNRouteProfile.All[route];var script=new JSONParser().LoadScript(profile.Script);
        int si=0,di=Array.FindIndex(script.scenes[0].d,e=>e.t=="d");
        if(di<0)di=Array.FindIndex(script.scenes[0].d,e=>e.t=="n"&&!string.IsNullOrWhiteSpace(e.text));
        Show(profile.Script,si,Mathf.Max(0,di),profile.Script.Contains("day0")?"cabin_interior":"station_sunset");
        box.SetProgress(.54f);box.SetStoryRoute(profile.Script);
    }
    static void Capture(string name)
    {
        var old=RenderTexture.active;RenderTexture.active=target;
        if(captureBuffer==null||captureBuffer.width!=target.width||captureBuffer.height!=target.height)
        {
            if(captureBuffer!=null)UnityEngine.Object.DestroyImmediate(captureBuffer);
            captureBuffer=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        }
        captureBuffer.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
        File.WriteAllBytes(Output+"/"+name+".png",captureBuffer.EncodeToPNG());RenderTexture.active=old;
    }
    static void Step()
    {
        if(EditorApplication.timeSinceStartup>deadline){Finish(false,"Timeout");return;}if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+.6;
        try
        {
            switch(stage)
            {
                case 0:Setup();stage=1;break;
                case 1:ApplyViewport();stage=2;break;
                case 2:
                    if(box.IsTyping()){box.SkipTyping();break;}
                    voice.Seek(.42f);voice.Pause(true);box.SetVoiceState(true,.42f);stage=5;break;
                case 5:
                    CheckLayout();CheckVoice();
                    VNSubtitleSettings.SetSecondaryLanguage("ja");Show("vn_localization_sample",0,0,"cabin_interior");stage=6;break;
                case 6:
                    box.SkipTyping();
                    var main=doc.rootVisualElement.Q<Label>("subtitle-sentence-primary-0");var secondary=doc.rootVisualElement.Q<Label>("subtitle-sentence-secondary-0");
                    Require(main!=null&&secondary!=null&&secondary.worldBound.yMax<=main.worldBound.yMin+1,"Bilingual pairing regressed");
                    Require(main.resolvedStyle.fontSize>secondary.resolvedStyle.fontSize&&secondary.resolvedStyle.unityTextAlign==TextAnchor.UpperCenter,"Bilingual hierarchy regressed");
                    Require(box.RootElement.worldBound.yMax<=doc.rootVisualElement.Q("progress-bar").worldBound.yMin+1,"Bilingual box overlaps capsule");
                    Capture("windows_2_5k_bilingual");Pass("VN_COMPACT_BILINGUAL_OK 2560x1440");
                    if(SessionState.GetBool(Pending+".PrimaryOnly",false)){Finish(true,"");return;}
                    VNSubtitleSettings.SetSecondaryLanguage("none");route=0;ShowRoute();stage=3;break;
                case 3:ApplyViewport();stage=4;break;
                case 4:
                    box.SkipTyping();Capture(VNRouteProfile.All[route].Script+"_route");
                    if(++route<VNRouteProfile.All.Length){ShowRoute();stage=3;}else{Pass("VN_ROUTE_GALLERY_OK 19 scripts");Finish(true,"");}break;
            }
        }
        catch(Exception ex){Debug.LogException(ex);Finish(false,ex.ToString());}
    }
    static void Finish(bool ok,string error)
    {
        bool success=ok&&errors.Count==0;EditorApplication.update-=Step;Application.logMessageReceived-=Log;SessionState.SetBool(Pending,false);SessionState.SetBool(Pending+".PrimaryOnly",false);
        if(captureBuffer!=null)UnityEngine.Object.DestroyImmediate(captureBuffer);
        File.WriteAllText(Output+"/result.json",JsonUtility.ToJson(new Result{success=success,error=error,checks=checks.ToArray(),errors=errors.ToArray(),editorDiagnostics=diagnostics.ToArray()},true));
        Debug.Log(success?"VN_READING_PRESENTATION_OK":"VN_READING_PRESENTATION_FAILED");EditorApplication.isPlaying=false;EditorApplication.Exit(success?0:1);
    }
    [Serializable]public class Result{public bool success;public string error;public string[] checks,errors,editorDiagnostics;}
}
