using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RailwayRenaissance.Cab
{
    public sealed class CabContextInteraction : MonoBehaviour
    {
        const string BindingPref = "Cab.Interaction.Primary.v1", IntroPref = "Cab.Interaction.Intro.v1";
        const string AimPref = "Cab.Interaction.Aim.v1", GuidePref = "Cab.Interaction.Guide.v1";
        readonly List<Choice> choices = new List<Choice>();
        readonly List<Collider> candidates = new List<Collider>();
        InputAction primary;
        Camera view;
        Component focused;
        Collider focusedCollider;
        CabEngineState state;
        CabSimulationDriver driver;
        Font font;
        bool settings, firstVisit, capturing, cursorAim, guide = true;
        int selected;
        float inputAfter, holdStart = -1, feedbackUntil;
        string feedback, bindingMessage;
        Rect actionRect;
        public string PrimaryPath => primary.bindings[0].effectivePath;
        public string PrimaryLabel => primary.GetBindingDisplayString();
        public bool ModalOpen => settings;
        struct Choice
        {
            public string label;
            public string reason;
            public Func<bool> execute;
            public bool deliberate;
            public Choice(string text, Func<bool> action, bool hold = false) { label=text;execute=action;deliberate=hold;reason=null; }
        }
        void OnEnable()
        {
            state = GetComponent<CabEngineState>(); driver = GetComponent<CabSimulationDriver>();
            if(driver!=null)driver.ReviewPanelVisible=false;
            view = Camera.main;
            primary = new InputAction("CabInteract", InputActionType.Button, "<Keyboard>/e");
            SetPrimaryPath(PlayerPrefs.GetString(BindingPref, "<Keyboard>/e"), false);
            primary.Enable();
            cursorAim = PlayerPrefs.GetInt(AimPref, 1) == 1;
            guide = PlayerPrefs.GetInt(GuidePref, 1) == 1;
            firstVisit = PlayerPrefs.GetInt(IntroPref, 0) == 0;
            settings = firstVisit;
            inputAfter = Time.unscaledTime + .25f;
            foreach (var t in GetComponentsInChildren<CabStartupClickTarget>())
                candidates.AddRange(t.GetComponents<Collider>());
            foreach (var t in GetComponentsInChildren<CabDrivingTarget>())
                candidates.AddRange(t.GetComponentsInChildren<Collider>());
        }
        void OnDisable() { primary?.Dispose(); primary=null; }
        public bool SetPrimaryPath(string path, bool persist = true)
        {
            if (primary == null || !ValidPath(path)) return false;
            primary.ApplyBindingOverride(0, path);
            if (persist) { PlayerPrefs.SetString(BindingPref, path); PlayerPrefs.Save(); }
            inputAfter=Time.unscaledTime+.25f;holdStart=-1;return true;
        }
        public static bool ValidPath(string path)
        {
            if (path == "<Mouse>/leftButton" || path == "<Mouse>/rightButton") return true;
            if (path == null || !path.StartsWith("<Keyboard>/", StringComparison.Ordinal)) return false;
            string key = path.Substring(11);
            if (!Enum.TryParse<Key>(key, true, out var parsed)) return false;
            // Navigation, overlays and modifiers must remain available during rebinding.
            return parsed != Key.None && parsed != Key.Escape && parsed != Key.F2 && parsed != Key.Tab
                && parsed != Key.LeftShift && parsed != Key.RightShift && parsed != Key.LeftCtrl
                && parsed != Key.RightCtrl && parsed != Key.LeftAlt && parsed != Key.RightAlt
                && parsed != Key.LeftMeta && parsed != Key.RightMeta;
        }
        Rect SettingsButton => new Rect(Screen.width-142,12,130,30);
        Rect ModalRect => new Rect(Mathf.Max(8,(Screen.width-510)/2),Mathf.Max(8,(Screen.height-450)/2),510,450);
        Vector2 Pointer => Mouse.current == null ? new Vector2(-1,-1) : Mouse.current.position.ReadValue();
        Vector2 GuiPointer => new Vector2(Pointer.x, Screen.height-Pointer.y);
        bool OverOtherUI()
        {
            if (SettingsButton.Contains(GuiPointer)) return true;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return true;
            if (driver != null && driver.BlocksPointer(Pointer)) return true;
            var day = FindFirstObjectByType<CabDaylightEnvironment>();
            return day != null && new Rect(Mathf.Max(0,(Screen.width-400)/2),Screen.height-108,400,98).Contains(GuiPointer);
        }
        void Update()
        {
            if (view == null) view=Camera.main;
            if (settings)
            {
                holdStart=-1;
                if (capturing) Capture();
                else if (!firstVisit && Keyboard.current?.escapeKey.wasPressedThisFrame==true) CloseSettings();
                return;
            }
            if(view!=null&&focusedCollider!=null&&(!focusedCollider.gameObject.activeInHierarchy
                ||Vector3.Distance(view.transform.position,focusedCollider.bounds.center)>2.4f))
            {focused=null;focusedCollider=null;holdStart=-1;}
            bool overActions=focused!=null&&actionRect.Contains(GuiPointer);
            if (view == null || OverOtherUI()) { focused=null;choices.Clear();holdStart=-1;return; }
            if(!overActions)
            {
            var point = cursorAim ? Pointer : new Vector2(Screen.width*.5f,Screen.height*.5f);
            var ray=view.ScreenPointToRay(point);
            Component next=null;Collider nextCollider=null;
            if (Physics.Raycast(ray,out var hit,2.2f,~0,QueryTriggerInteraction.Ignore))
            { next=Target(hit.collider);if(next!=null)nextCollider=hit.collider; }
            // Proximity hints never execute an action or steal an aimed target.
            if (next==null)
            {
                float best=1.15f;
                foreach(var c in candidates)
                {
                    if(c==null||!c.enabled||!c.gameObject.activeInHierarchy)continue;
                    var position=c.bounds.center;
                    var viewport=view.WorldToViewportPoint(position);
                    if(viewport.z<=0||viewport.x<0||viewport.x>1||viewport.y<0||viewport.y>1)continue;
                    float distance=Vector3.Distance(view.transform.position,position);
                    if(distance>=best)continue;
                    var direction=position-view.transform.position;
                    if(Physics.Raycast(view.transform.position,direction.normalized,out var sight,distance+.02f,~0,QueryTriggerInteraction.Ignore)
                        &&Target(sight.collider)==Target(c))
                    {best=distance;next=Target(c);nextCollider=c;}
                }
            }
            if(next!=focused){focused=next;focusedCollider=nextCollider;selected=0;holdStart=-1;inputAfter=Time.unscaledTime+.12f;}
            }
            BuildChoices();
            if(choices.Count==0)return;
            float wheel=Mouse.current?.scroll.ReadValue().y??0;
            if(wheel!=0){selected=(selected+(wheel<0?1:choices.Count-1))%choices.Count;holdStart=-1;}
            if(overActions&&PrimaryPath.StartsWith("<Mouse>/",StringComparison.Ordinal)){holdStart=-1;return;}
            if(Time.unscaledTime<inputAfter)return;
            if(choices[selected].deliberate)
            {
                if(primary.WasPressedThisFrame())holdStart=Time.unscaledTime;
                if(!primary.IsPressed())holdStart=-1;
                if(holdStart>=0&&Time.unscaledTime-holdStart>=.7f){ExecuteSelected();holdStart=-1;inputAfter=Time.unscaledTime+.25f;}
            }
            else if(primary.WasPressedThisFrame())ExecuteSelected();
        }
        static Component Target(Collider collider)
        {
            var startup=collider.GetComponent<CabStartupClickTarget>();
            return startup!=null?(Component)startup:collider.GetComponentInParent<CabDrivingTarget>();
        }
        void BuildChoices()
        {
            choices.Clear();
            if(focused is CabDrivingTarget d)
            {
                string up=d.Action==CabDrivingAction.Throttle?"增加一档":d.Action==CabDrivingAction.Reverser?"向前一位":"施加一档";
                string down=d.Action==CabDrivingAction.Throttle?"减少一档":d.Action==CabDrivingAction.Reverser?"向后一位":"缓解一档";
                bool emergency=d.Action==CabDrivingAction.AutomaticBrake&&state.BrakePos==BrakeNotch.Full;
                choices.Add(new Choice(emergency?"进入紧急制动":up,()=>d.Activate(),emergency));
                choices.Add(new Choice(down,()=>d.Activate(true)));
            }
            else if(focused is CabStartupClickTarget s)
            {
                var c=s.Controls;
                switch(s.Action)
                {
                    case CabStartupAction.Key:
                        if(!c.KeyInserted)choices.Add(new Choice("插入钥匙",()=>s.Activate()));
                        else {choices.Add(new Choice(c.MasterEnabled?"关闭主控":"开启主控",()=>s.Activate()));choices.Add(new Choice("拔出钥匙",()=>s.Activate(true)));}break;
                    case CabStartupAction.FuelPump: choices.Add(new Choice(c.FuelPumpEnabled?"关闭燃油泵":"开启燃油泵",()=>s.Activate()));break;
                    case CabStartupAction.Start: choices.Add(new Choice("启动柴油机",()=>s.Activate()));break;
                    case CabStartupAction.Stop: choices.Add(new Choice("停止柴油机",()=>s.Activate(),true));break;
                }
            }
            selected=Mathf.Clamp(selected,0,Mathf.Max(0,choices.Count-1));
            for(int i=0;i<choices.Count;i++)
            {
                var choice=choices[i];choice.reason=UnavailableReason(i);choices[i]=choice;
            }
        }
        string UnavailableReason(int index)
        {
            if(focused is CabDrivingTarget d)
            {
                if(d.Action==CabDrivingAction.Throttle)
                {
                    if(index==1)return state.ThrottleNotch==0?"已归零":null;
                    if(state.ThrottleNotch==8)return "已到最高档";
                    if(!state.IsEngineRunning)return "等待柴油机运行";
                    if(state.Reverser==ReverserPosition.Neutral)return "先选择行驶方向";
                    if(driver!=null&&driver.ParkingBrake)return "先解除停放制动";
                    if(state.MainReservoirKpa<600||state.BrakePipeKpa<470)return "等待充风";
                    if(state.BrakeCylinderKpa>=20||state.BrakePos>=BrakeNotch.Common)return "先缓解制动";
                }
                if(d.Action==CabDrivingAction.AutomaticBrake&&((index==0&&state.BrakePos==BrakeNotch.Emergency)||(index==1&&state.BrakePos==BrakeNotch.Release)))return "已到端位";
                if(d.Action==CabDrivingAction.IndependentBrake&&((index==0&&state.IndependentBrakeNotch>=1)||(index==1&&state.IndependentBrakeNotch<=0)))return "已到端位";
                if(d.Action==CabDrivingAction.Reverser)
                {
                    if(Mathf.Abs(state.CurrentSpeedKmh)>.5f||state.ThrottleNotch!=0)return "先停车并将牵引归零";
                    if((index==0&&state.Reverser==ReverserPosition.Forward)||(index==1&&state.Reverser==ReverserPosition.Reverse))return "已到端位";
                    if(!state.IsEngineRunning)return "等待柴油机运行";
                }
            }
            if(focused is CabStartupClickTarget s)
            {
                var c=s.Controls;
                if(s.Action==CabStartupAction.Key&&c.KeyInserted)
                {
                    if(state.IsEngineStarted)return "先停止柴油机";
                    if(index==1&&c.MasterEnabled)return "先关闭主控";
                }
                if(s.Action==CabStartupAction.FuelPump)
                {if(!c.MasterEnabled)return "先开启主控";if(state.IsEngineStarted)return "运行中保持油泵状态";}
                if(s.Action==CabStartupAction.Start)
                {if(state.IsEngineStarted)return "已发出启动请求";if(!c.MasterEnabled||!c.FuelPumpEnabled)return "先开启主控和油泵";}
            }
            return null;
        }
        void ExecuteSelected()
        {
            if(selected>=choices.Count)return;
            if(choices[selected].reason!=null){feedback=choices[selected].reason;feedbackUntil=Time.unscaledTime+2.5f;return;}
            bool accepted=choices[selected].execute();
            feedback=accepted?"操作已提交":focused is CabStartupClickTarget s?
                (string.IsNullOrEmpty(s.Controls.LastWarning)?"当前条件不允许此操作":s.Controls.LastWarning):state.LastProcedureWarning;
            feedbackUntil=Time.unscaledTime+2.5f;
        }
        void Capture()
        {
            if(Time.unscaledTime<inputAfter)return;
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true){capturing=false;bindingMessage="已取消，保留原按键";return;}
            string path=null;
            if(Mouse.current?.leftButton.wasPressedThisFrame==true)path="<Mouse>/leftButton";
            else if(Mouse.current?.rightButton.wasPressedThisFrame==true)path="<Mouse>/rightButton";
            else if(Keyboard.current!=null)foreach(var key in Keyboard.current.allKeys)if(key.wasPressedThisFrame){path="<Keyboard>/"+key.keyCode.ToString();break;}
            if(path==null)return;
            if(!SetPrimaryPath(path)){bindingMessage="该键保留给导航或系统，请换一个";return;}
            capturing=false;bindingMessage="已修改为 "+PrimaryLabel;
        }
        void CloseSettings()
        {
            settings=false;firstVisit=false;capturing=false;focused=null;inputAfter=Time.unscaledTime+.3f;
            PlayerPrefs.SetInt(IntroPref,1);PlayerPrefs.SetInt(AimPref,cursorAim?1:0);PlayerPrefs.SetInt(GuidePref,guide?1:0);PlayerPrefs.Save();
        }
        string GuideText()
        {
            var c=GetComponentInChildren<CabStartupControls>();
            if(c==null)return null;
            if(!c.KeyInserted)return "看向钥匙座 → 插入钥匙。只需使用你选择的交互键。";
            if(!c.MasterEnabled)return "继续操作钥匙 → 开启主控。同一部件可有多个动作。";
            if(!c.FuelPumpEnabled)return "看向燃油泵选择开关 → 开启油泵。";
            if(!state.IsEngineStarted)return "看向启动按钮 → 启动柴油机。";
            if(!state.IsEngineRunning)return "柴油机正在启动，观察转速与运行指示灯。";
            if(state.MainReservoirKpa<600||state.BrakePipeKpa<470)return "等待充风，观察总风和制动管压力；暂不能施加牵引。";
            if(state.Reverser==ReverserPosition.Neutral)return "看向换向手柄 → 置前进位。";
            if(state.BrakePos>=BrakeNotch.Common||state.IndependentBrakeNotch>.01f||state.BrakeCylinderKpa>=20)return "操作大小闸缓解制动，等待缸压下降。";
            if(driver!=null&&driver.ParkingBrake)return "交互设置 → 打开测试面板 → 解除停放制动（机械操作件待制作）。";
            return state.ThrottleNotch==0?"看向牵引手轮 → 增加一档。提示随时可在交互设置里关闭。":"已完成启动与初次牵引；用实体闸把练习减速和停车。";
        }
        void OnGUI()
        {
            if(font==null)font=Resources.Load<Font>("Fonts/SarasaMonoSC-Regular");
            var old=GUI.skin.font;if(font!=null)GUI.skin.font=font;
            if(GUI.Button(SettingsButton,"交互设置")){settings=true;inputAfter=Time.unscaledTime+.25f;holdStart=-1;}
            if(settings){DrawSettings();GUI.skin.font=old;return;}
            if(!cursorAim)GUI.Label(new Rect(Screen.width/2f-6,Screen.height/2f-12,20,24),"·");
            if(focusedCollider!=null&&focused!=null&&choices.Count>0)
            {
                Vector3 p=view.WorldToScreenPoint(focusedCollider.bounds.center);
                float h=80+choices.Count*29;
                actionRect=new Rect(Mathf.Clamp(p.x+24,8,Mathf.Max(8,Screen.width-308)),Mathf.Clamp(Screen.height-p.y-h/2,52,Mathf.Max(52,Screen.height-h-118)),300,h);
                GUI.Box(actionRect,GUIContent.none);
                GUI.Label(new Rect(actionRect.x+10,actionRect.y+6,280,24),focused is CabDrivingTarget d?d.Hint:"驾驶台操作");
                for(int i=0;i<choices.Count;i++)
                {
                    string prefix=i==selected?"› ":"  ";
                    string label=prefix+choices[i].label+(choices[i].deliberate?"（长按）":"");
                    if(GUI.Button(new Rect(actionRect.x+10,actionRect.y+31+i*29,280,26),label)){selected=i;holdStart=-1;inputAfter=Time.unscaledTime+.2f;}
                }
                if(choices[selected].reason!=null)GUI.Label(new Rect(actionRect.x+10,actionRect.yMax-48,280,24),choices[selected].reason);
                GUI.Label(new Rect(actionRect.x+10,actionRect.yMax-23,280,22),$"[{PrimaryLabel}] 执行 · 滚轮或点击选择");
            }
            if(guide)GUI.Box(new Rect(12,Screen.height-162,Mathf.Min(680,Screen.width-24),45),GuideText());
            if(Time.unscaledTime<feedbackUntil)GUI.Box(new Rect(Mathf.Max(8,(Screen.width-580)/2),64,580,38),feedback);
            GUI.skin.font=old;
        }
        void DrawSettings()
        {
            GUILayout.BeginArea(ModalRect,GUI.skin.box);
            GUILayout.Label(firstVisit?"第一次驾驶 · 先选一个顺手的交互键":"驾驶交互设置");
            GUILayout.Label("看向实体部件，读取就近提示，再执行操作。\n无需背下一整张快捷键表；可随时从右上角修改。");
            GUILayout.Space(12);GUILayout.Label("当前交互键："+PrimaryLabel);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("使用 E"))SetPrimaryPath("<Keyboard>/e");
            if(GUILayout.Button("使用鼠标左键"))SetPrimaryPath("<Mouse>/leftButton");
            if(GUILayout.Button("自定义…")){capturing=true;inputAfter=Time.unscaledTime+.3f;}
            GUILayout.EndHorizontal();
            GUILayout.Label(capturing?"请按下新按键；Esc取消。录入不会操作驾驶台。":bindingMessage??"按键选择会保存，下次启动继续使用。");
            GUILayout.Space(8);
            cursorAim=GUILayout.Toggle(cursorAim,"用鼠标指向部件（关闭后使用屏幕中央准心）");
            guide=GUILayout.Toggle(guide,"显示分步驾驶引导（依据实际状态推进）");
            GUILayout.Label("多动作：滚轮切换或点击动作名称，再按交互键。\n靠近只显示提示，不自动操作。停机、紧急位需长按确认。\n设置面板打开时停止交互，机车模拟继续运行。");
            GUILayout.Space(8);
            GUILayout.Label("硬核：以实体部件操作为主，快捷键作为可选补充。\n休闲：规划由小黄鼠协助复杂流程；当前尚未接入。");
            if(driver!=null&&GUILayout.Button("打开测试面板（临时停放制动入口）")){driver.ReviewPanelVisible=true;CloseSettings();}
            GUI.enabled=!capturing;
            if(GUILayout.Button(firstVisit?"按这个设置开始练习":"保存并返回"))CloseSettings();
            GUI.enabled=true;GUILayout.EndArea();
        }
    }
}
