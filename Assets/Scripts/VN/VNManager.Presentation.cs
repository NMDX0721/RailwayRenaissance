using UnityEngine;
using UnityEngine.UIElements;

public static class VNPresentationSettings
{
    public static bool StorySignal { get; private set; } = PlayerPrefs.GetInt("VNStorySignal",1)==1;
    public static float VoiceVolume { get; private set; } = Mathf.Clamp01(PlayerPrefs.GetFloat("VNVoiceVolume",1));
    public static void SetSignal(bool value){StorySignal=value;PlayerPrefs.SetInt("VNStorySignal",value?1:0);PlayerPrefs.Save();}
    public static void SetVoiceVolume(float value){VoiceVolume=Mathf.Clamp01(value);PlayerPrefs.SetFloat("VNVoiceVolume",VoiceVolume);PlayerPrefs.Save();}
}

public partial class VNManager
{
    private VNVoicePlayer lineVoice;
    private VisualElement storySignal;
    private VNInkIcon signalArt;
    private VNSignalAspect shownAspect;
    private bool signalSet;
    private void SetupReadingPresentation()
    {
        lineVoice=gameObject.AddComponent<VNVoicePlayer>();
        dialogueBox.OnBookmark=AddBookmark;
        dialogueBox.OnChromeInteraction=()=>{clickedButtonThisFrame=true;SuppressNextAdvance();};
        dialogueBox.OnVoiceReplay=()=>{lineVoice.Replay();if(isAutoPlay)StartAutoPlayTimer();};
        dialogueBox.OnVoiceSeek=p=>{lineVoice.Seek(p);if(isAutoPlay)StartAutoPlayTimer();};
        storySignal=new VisualElement{name="vn-story-signal",pickingMode=PickingMode.Ignore};
        storySignal.style.position=Position.Absolute;storySignal.style.left=14;storySignal.style.top=16;
        storySignal.style.width=72;storySignal.style.height=152;storySignal.style.backgroundColor=Color.clear;
        storySignal.style.borderTopWidth=storySignal.style.borderBottomWidth=storySignal.style.borderLeftWidth=storySignal.style.borderRightWidth=0;
        storySignal.style.paddingTop=storySignal.style.paddingBottom=4;storySignal.style.paddingLeft=storySignal.style.paddingRight=8;
        signalArt=new VNInkIcon(VNArtIcon.Signal);signalArt.style.flexGrow=1;storySignal.Add(signalArt);
        uiDoc.rootVisualElement.Add(storySignal);
        UpdateReadingPresentation();
    }
    public void RefreshVoiceLanguage()
    {
        ResetLineVoice();
        if(!isScriptRunning||currentScript?.scenes==null||currentSceneIndex>=currentScript.scenes.Length)return;
        var entries=currentScript.scenes[currentSceneIndex].d;
        if(entries!=null&&currentDialogueIndex<entries.Length)BindLineVoice(entries[currentDialogueIndex]);
    }
    private void ResetLineVoice(){lineVoice?.Clear();dialogueBox?.SetVoiceState(false,0);}
    private void BindLineVoice(DialogueEntry entry)
    {
        if(lineVoice==null||progressSeeking)return;
        lineVoice.Bind(!string.IsNullOrEmpty(currentScript?.id)?currentScript.id:currentScriptName,currentSceneIndex,currentDialogueIndex,entry);
        dialogueBox.SetVoiceState(lineVoice.HasClip,lineVoice.Progress);
    }
    // 右键隐藏期间点击推进会让 DialogueBox.Show / 进度条等重新置为显示；每帧末把被隐藏的容器压回去
    private void LateUpdate()
    {
        if (!uiHidden || hiddenUiSnapshots.Count == 0) return;
        foreach (var hidden in hiddenUiSnapshots.Keys)
            if (hidden != null && hidden.style.display != DisplayStyle.None) hidden.style.display = DisplayStyle.None;
    }
    private int bookmarkShownKey = int.MinValue;
    private void UpdateReadingPresentation()
    {
        // 换句时刷新对话框书签键的收藏状态（PlayerPrefs 解析较重，不逐帧做）
        int bookmarkKey = (currentSceneIndex * 100003 + currentDialogueIndex) * 31 + (currentScriptName != null ? currentScriptName.GetHashCode() : 0);
        if (bookmarkKey != bookmarkShownKey && dialogueBox != null)
        {
            bookmarkShownKey = bookmarkKey;
            dialogueBox.SetBookmarked(isScriptRunning && IsBookmarkedAt(currentSceneIndex, currentDialogueIndex));
        }
        if (backlogVoice != null && backlogVoice.HasClip && (vnBacklog == null || !vnBacklog.IsOpen)) backlogVoice.Clear();
        if(lineVoice!=null)
        {
            bool blocked=!Application.isFocused||uiHidden||menuExpanded||AdvanceBlockedByPanels();
            lineVoice.Pause(blocked);dialogueBox?.SetVoiceState(lineVoice.HasClip,lineVoice.Progress);
        }
        if(storySignal==null)return;
        storySignal.style.display=VNPresentationSettings.StorySignal&&isScriptRunning&&!uiHidden&&!IsCinematicBlocking()&&!AdvanceBlockedByPanels()?DisplayStyle.Flex:DisplayStyle.None;
        var profile=VNRouteProfile.Find(currentScriptName);
        // Authored atmosphere beats; no analysis of unread dialogue, identities or choices.
        // 信号表示"前方区间"：显示再往前一小段的基调，话末时预告下一话；不读取未读台词，仅用预设的氛围段
        // 优先用作者标注的危机点按阅读时间预告（约 5 分钟）；本话与下一话都没有标注时退回预设氛围段
        VNSignalAspect aspect;
        if(!(isScriptRunning&&currentScript!=null&&VNSignalForecast.TryAspect(currentScript,currentSceneIndex,currentDialogueIndex,out aspect)))
            aspect=profile.AspectAhead(progressShown,currentScript?.nextScript);
        if(!signalSet||aspect!=shownAspect)
        {
            signalSet=true;shownAspect=aspect;signalArt.Aspect=aspect;signalArt.MarkDirtyRepaint();
            string label=aspect==VNSignalAspect.Proceed?"进行":aspect==VNSignalAspect.Reduced?"减速":aspect==VNSignalAspect.Caution?"注意":aspect==VNSignalAspect.Restricted?"警戒":"停止";
        }
    }
}
