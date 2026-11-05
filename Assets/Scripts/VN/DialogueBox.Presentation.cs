using System;
using UnityEngine;
using UnityEngine.UIElements;

public partial class DialogueBox
{
    public Action OnBookmark, OnVoiceReplay, OnChromeInteraction;
    public Action<float> OnVoiceSeek;
    private Button bookmarkButton, replayButton;
    private VisualElement voiceTrack, voiceRail, voiceFill, voiceThumb;
    private VNScenicRoute scenicRoute;
    private float readingHit = 44;
    public void SetReadingHitSize(float size)
    {
        readingHit=size;
        foreach(var b in new[]{replayButton,bookmarkButton})if(b!=null){b.style.width=size;b.style.height=size;}
        SetVoiceState(replayButton!=null&&replayButton.style.display==DisplayStyle.Flex,voiceFill==null?0:voiceFill.style.width.value.value/100);
    }

    private Button ReadingButton(string name, VNArtIcon kind, string hint, Action clicked)
    {
        var button=new Button(()=>clicked()){name=name,tooltip=hint,focusable=false};
        button.style.width=44;button.style.height=44;button.style.paddingLeft=button.style.paddingRight=6;
        button.style.paddingTop=button.style.paddingBottom=6;
        button.style.backgroundColor=new Color(.1f,.07f,.045f,.45f);
        button.style.borderTopWidth=button.style.borderRightWidth=button.style.borderBottomWidth=button.style.borderLeftWidth=1;
        button.style.borderTopColor=button.style.borderRightColor=button.style.borderBottomColor=button.style.borderLeftColor=new Color(.78f,.6f,.34f,.48f);
        button.style.borderTopLeftRadius=button.style.borderTopRightRadius=button.style.borderBottomLeftRadius=button.style.borderBottomRightRadius=5;
        var icon=new VNInkIcon(kind);icon.style.flexGrow=1;button.Add(icon);
        button.RegisterCallback<PointerDownEvent>(e=>{OnChromeInteraction?.Invoke();e.StopImmediatePropagation();});
        button.RegisterCallback<PointerUpEvent>(e=>e.StopImmediatePropagation());
        button.RegisterCallback<ClickEvent>(e=>{OnChromeInteraction?.Invoke();e.StopImmediatePropagation();});
        button.RegisterCallback<PointerEnterEvent>(_=>{button.style.backgroundColor=new Color(1,.85f,.5f,.1f);UiCursor.ShowHand();});
        button.RegisterCallback<PointerLeaveEvent>(_=>{button.style.backgroundColor=new Color(.1f,.07f,.045f,.45f);UiCursor.ShowArrow();});
        return button;
    }
    private void BuildReadingControls()
    {
        dialogueBox.style.paddingLeft=18;
        nameArea.style.width=262;
        replayButton=ReadingButton("vn-voice-replay",VNArtIcon.Replay,"重播本句配音",()=>OnVoiceReplay?.Invoke());
        replayButton.style.position=Position.Absolute;replayButton.style.left=0;replayButton.style.top=Length.Percent(50);
        replayButton.style.translate=new Translate(0,Length.Percent(-50));
        nameArea.Add(replayButton);
        bookmarkButton=ReadingButton("vn-bookmark-btn",VNArtIcon.Bookmark,"收藏当前台词",()=>OnBookmark?.Invoke());
        bookmarkButton.style.position=Position.Absolute;bookmarkButton.style.left=48;bookmarkButton.style.top=Length.Percent(50);
        bookmarkButton.style.translate=new Translate(0,Length.Percent(-50));
        nameArea.Add(bookmarkButton);
        speakerName.style.left=98;speakerName.style.right=8;speakerName.style.fontSize=32;
        speakerName.style.overflow = Overflow.Hidden;
        speakerName.style.textOverflow = TextOverflow.Ellipsis;
        speakerName.style.unityTextAlign=TextAnchor.MiddleCenter;
        voiceTrack=new VisualElement{name="vn-voice-progress",tooltip="拖动调整本句配音进度"};
        voiceTrack.style.position=Position.Absolute;voiceTrack.style.left=4;voiceTrack.style.right=12;voiceTrack.style.bottom=10;
        voiceTrack.style.height=24;voiceTrack.style.justifyContent=Justify.Center;
        voiceRail = new VisualElement { pickingMode = PickingMode.Ignore };
        voiceRail.style.height = 5;
        voiceRail.style.marginLeft = voiceRail.style.marginRight = 5;
        voiceRail.style.backgroundColor = new Color(.85f, .73f, .56f, .21f);
        Round(voiceRail, 2.5f);
        voiceFill = new VisualElement { name = "vn-voice-fill", pickingMode = PickingMode.Ignore };
        voiceFill.style.height = 5;
        voiceFill.style.backgroundColor = new Color(.87f, .68f, .40f);
        Round(voiceFill, 2.5f);
        voiceThumb = new VisualElement { pickingMode = PickingMode.Ignore };
        voiceThumb.style.position = Position.Absolute;
        voiceThumb.style.top = -2;
        voiceThumb.style.width = voiceThumb.style.height = 9;
        voiceThumb.style.translate = new Translate(-4.5f, 0);
        voiceThumb.style.backgroundColor = new Color(1f, .89f, .68f);
        Round(voiceThumb, 4.5f);
        voiceRail.Add(voiceFill);
        voiceRail.Add(voiceThumb);
        voiceTrack.Add(voiceRail);
        nameArea.Add(voiceTrack);
        void Seek(float x)
        {
            OnVoiceSeek?.Invoke(Mathf.Clamp01((x - voiceRail.layout.x) / Mathf.Max(1, voiceRail.contentRect.width)));
        }
        voiceTrack.RegisterCallback<PointerDownEvent>(e=>{OnChromeInteraction?.Invoke();voiceTrack.CapturePointer(e.pointerId);Seek(e.localPosition.x);e.StopImmediatePropagation();});
        voiceTrack.RegisterCallback<PointerMoveEvent>(e=>{if(voiceTrack.HasPointerCapture(e.pointerId)){Seek(e.localPosition.x);e.StopImmediatePropagation();}});
        voiceTrack.RegisterCallback<PointerUpEvent>(e=>{if(voiceTrack.HasPointerCapture(e.pointerId))voiceTrack.ReleasePointer(e.pointerId);e.StopImmediatePropagation();});
        voiceTrack.RegisterCallback<ClickEvent>(e=>e.StopImmediatePropagation());
        voiceTrack.RegisterCallback<PointerEnterEvent>(_ => UiCursor.ShowHand());
        voiceTrack.RegisterCallback<PointerLeaveEvent>(_ => UiCursor.ShowArrow());
        SetVoiceState(false,0);
    }
    private static void Round(VisualElement element, float radius)
    {
        element.style.borderTopLeftRadius = element.style.borderTopRightRadius = radius;
        element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = radius;
    }
    public void SetVoiceState(bool available,float progress)
    {
        if(replayButton==null)return;
        replayButton.style.display=voiceTrack.style.display=available?DisplayStyle.Flex:DisplayStyle.None;
        var position = Length.Percent(Mathf.Clamp01(progress) * 100);
        voiceFill.style.width = position;
        voiceThumb.style.left = position;
        bool named=!string.IsNullOrEmpty(speakerName.text);
        // Keep bookmarking narration; only spoken, voiced lines get replay and audio transport.
        nameArea.style.visibility=Visibility.Visible;nameArea.style.width=named?readingHit*2+174:readingHit+12;
        bookmarkButton.style.left=available?readingHit+4:0;speakerName.style.left=available?readingHit*2+10:readingHit+4;
    }
    public void SetStoryRoute(string script)=>scenicRoute?.SetRoute(script);
    private void BuildJourneyProgress(VisualElement root)
    {
        progressBar=new VisualElement{name="progress-bar"};
        progressBar.style.position=Position.Absolute;progressBar.style.left=0;progressBar.style.right=0;progressBar.style.bottom=0;
        progressBar.style.height=26;progressBar.style.flexDirection=FlexDirection.Row;
        progressBar.style.borderLeftWidth = progressBar.style.borderRightWidth = 2;
        progressBar.style.borderLeftColor = dialogueBox.style.borderLeftColor;
        progressBar.style.borderRightColor = dialogueBox.style.borderRightColor;
        var frost = new VisualElement { pickingMode = PickingMode.Ignore };
        frost.style.position = Position.Absolute;
        frost.style.left = frost.style.right = -2;
        frost.style.top = -24;
        frost.style.bottom = 0;
        frost.style.backgroundColor = VNCapsuleEnd.FrostTint;
        frost.generateVisualContent += VNCapsuleEnd.DrawGrain;
        progressBar.Add(frost);
        progressBar.style.display=DisplayStyle.None;
        var capsule = new VisualElement { name = "progress-capsule" };
        capsule.style.flexGrow = 1;
        capsule.style.flexDirection = FlexDirection.Row;
        capsule.style.marginLeft = capsule.style.marginRight = 14;
        capsule.style.marginTop = capsule.style.marginBottom = 4;
        capsule.style.overflow = Overflow.Hidden;
        Round(capsule, 9);
        progressBar.Add(capsule);
        var body=new VisualElement{name="progress-capsule-body",pickingMode=PickingMode.Ignore};
        body.style.position=Position.Absolute;body.style.left=body.style.right=9;body.style.top=body.style.bottom=0;
        body.style.backgroundColor=VNCapsuleEnd.CapsuleTint;
        body.style.borderTopWidth = 0;
        body.style.borderBottomWidth = 1;
        body.style.borderBottomColor = new Color(200f/255f,150f/255f,80f/255f,.42f);
        capsule.Add(body);
        scenicRoute=new VNScenicRoute();capsule.Add(scenicRoute);
        capsule.Add(new VNCapsuleEnd(false));capsule.Add(new VNCapsuleEnd(true));root.Add(progressBar);
        progressBar.PlaceBehind(dialogueBox);
        progressThumb=scenicRoute.Q("progress-thumb");
        void Seek(Vector2 panel){OnProgressSeek?.Invoke(scenicRoute.Normalize(scenicRoute.WorldToLocal(panel).x));}
        progressBar.RegisterCallback<PointerDownEvent>(e=>{OnChromeInteraction?.Invoke();OnProgressDragChanged?.Invoke(true);progressBar.CapturePointer(e.pointerId);Seek(e.position);e.StopImmediatePropagation();});
        progressBar.RegisterCallback<PointerMoveEvent>(e=>{if(progressBar.HasPointerCapture(e.pointerId)){Seek(e.position);e.StopImmediatePropagation();}});
        progressBar.RegisterCallback<PointerUpEvent>(e=>{if(progressBar.HasPointerCapture(e.pointerId))progressBar.ReleasePointer(e.pointerId);OnProgressDragChanged?.Invoke(false);e.StopImmediatePropagation();});
        progressBar.RegisterCallback<PointerCaptureOutEvent>(_=>OnProgressDragChanged?.Invoke(false));
        progressBar.RegisterCallback<ClickEvent>(e=>e.StopImmediatePropagation());
        progressBar.RegisterCallback<PointerEnterEvent>(_=>UiCursor.ShowHand());progressBar.RegisterCallback<PointerLeaveEvent>(_=>UiCursor.ShowArrow());
    }
}
