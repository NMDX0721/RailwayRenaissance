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
        CabServiceControl heldService;
        Font font;
        bool settings, firstVisit, capturing, cursorAim, guide = true;
        int selected;
        float inputAfter, holdStart = -1, feedbackUntil;
        string feedback, bindingMessage;
        Rect actionRect;
        CabSafetyEquipment equipmentInfo;
        CabWorkstationEquipment workstationInfo;
        Vector2 equipmentInfoScroll;
        public string PrimaryPath => primary.bindings[0].effectivePath;
        public string PrimaryLabel => primary.GetBindingDisplayString();
        public bool ModalOpen => settings || equipmentInfo!=null || workstationInfo!=null;
        public bool ShowWorkstationInfo(CabWorkstationEquipment equipment)
        {
            if(equipment==null||!equipment.CanOperate())return false;
            workstationInfo=equipment;equipmentInfo=null;equipmentInfoScroll=Vector2.zero;holdStart=-1;return true;
        }
        public bool ShowEquipmentInfo(CabSafetyEquipment equipment)
        {
            if(equipment==null)return false;
            equipmentInfo=equipment;workstationInfo=null;equipmentInfoScroll=Vector2.zero;holdStart=-1;return true;
        }
        public void CloseEquipmentInfo()
        {
            equipmentInfo=null;workstationInfo=null;focused=null;focusedCollider=null;holdStart=-1;inputAfter=Time.unscaledTime+.25f;
        }
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
            foreach (var t in GetComponentsInChildren<CabSafetyEquipment>())
                candidates.AddRange(t.GetComponents<Collider>());
            foreach (var t in GetComponentsInChildren<CabStorageCabinet>())
                candidates.AddRange(t.GetComponentsInChildren<Collider>());
            foreach (var t in GetComponentsInChildren<CabSunVisor>())
                candidates.AddRange(t.GetComponentsInChildren<Collider>());
            foreach (var t in GetComponentsInChildren<CabWorkstationEquipment>())
                candidates.AddRange(t.GetComponentsInChildren<Collider>());
            foreach (var t in GetComponentsInChildren<CabCirculationFan>())
                candidates.AddRange(t.GetComponentsInChildren<Collider>());
            foreach (var t in GetComponentsInChildren<CabWiperSystem>())
                candidates.AddRange(t.GetComponentsInChildren<Collider>());
            foreach (var t in GetComponentsInChildren<CabExteriorLightControl>())
                candidates.AddRange(t.GetComponents<Collider>());
            foreach(var t in GetComponentsInChildren<CabVigilance>())
                candidates.AddRange(t.GetComponentsInChildren<Collider>());
            foreach(var t in GetComponentsInChildren<CabParkingBrakeButton>())
                candidates.AddRange(t.GetComponentsInChildren<Collider>());
            foreach (var t in GetComponentsInChildren<CabLampTestButton>())
                candidates.AddRange(t.GetComponents<Collider>());
            foreach (var t in GetComponentsInChildren<CabServiceControl>())
                candidates.AddRange(t.GetComponents<Collider>());
            foreach (var t in GetComponentsInChildren<CabLightingKey>())
                candidates.AddRange(t.GetComponents<Collider>());
            var lighting=GetComponentInParent<CabInteriorLighting>();
            if(lighting!=null)foreach(var t in lighting.GetComponentsInChildren<CabPassageDoor>())
                candidates.AddRange(t.GetComponentsInChildren<Collider>());
        }
        void ReleaseServiceHold(){if(heldService!=null)heldService.Release();heldService=null;}
        void OnDisable() { ReleaseServiceHold();primary?.Dispose(); primary=null; }
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
            if (FindFirstObjectByType<CabInteriorLighting>()?.BlocksPointer(GuiPointer) == true) return true;
            if (FindFirstObjectByType<CabExteriorLighting>()?.BlocksPointer(GuiPointer) == true) return true;
            var day = FindFirstObjectByType<CabDaylightEnvironment>();
            if(day!=null&&day.isActiveAndEnabled&&new Rect(Mathf.Max(0,(Screen.width-400)/2),Screen.height-108,400,98).Contains(GuiPointer))return true;
            return FindFirstObjectByType<CabRailReviewViews>()?.BlocksPointer(Pointer)==true;
        }
        void Update()
        {
            if (view == null) view=Camera.main;
            if(heldService!=null)
            {
                if(primary==null||!primary.IsPressed()||ModalOpen||Time.timeScale==0||focused!=heldService||OverOtherUI())ReleaseServiceHold();
                else heldService.Maintain();
            }
            if(equipmentInfo!=null||workstationInfo!=null)
            {
                holdStart=-1;
                if(Keyboard.current?.escapeKey.wasPressedThisFrame==true)CloseEquipmentInfo();
                return;
            }
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
            if(heldService!=null&&focused!=heldService)ReleaseServiceHold();
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
            var vigilance=collider.GetComponentInParent<CabVigilance>();if(vigilance!=null)return vigilance;
            var parking=collider.GetComponentInParent<CabParkingBrakeButton>();if(parking!=null)return parking;
            var service=collider.GetComponentInParent<CabServiceControl>();if(service!=null)return service;
            var lampTest=collider.GetComponentInParent<CabLampTestButton>();if(lampTest!=null)return lampTest;
            var exterior=collider.GetComponentInParent<CabExteriorLightControl>();if(exterior!=null)return exterior;
            var lightingKey=collider.GetComponentInParent<CabLightingKey>();if(lightingKey!=null)return lightingKey;
            var passage=collider.GetComponentInParent<CabPassageDoor>();if(passage!=null)return passage;
            var wiper=collider.GetComponentInParent<CabWiperSystem>();if(wiper!=null)return wiper;
            var fan=collider.GetComponentInParent<CabCirculationFan>();if(fan!=null)return fan;
            var workstation=collider.GetComponentInParent<CabWorkstationEquipment>();
            if(workstation!=null)return workstation;
            var visor=collider.GetComponentInParent<CabSunVisor>();
            if(visor!=null)return visor;
            var cabinet=collider.GetComponentInParent<CabStorageCabinet>();
            if(cabinet!=null)return cabinet;
            var equipment=collider.GetComponentInParent<CabSafetyEquipment>();
            if(equipment!=null)return equipment;
            var startup=collider.GetComponent<CabStartupClickTarget>();
            return startup!=null?(Component)startup:collider.GetComponentInParent<CabDrivingTarget>();
        }
        void BuildChoices()
        {
            choices.Clear();
            if(focused is CabVigilance vigilance)
                choices.Add(new Choice(vigilance.ActionLabel,()=>vigilance.Acknowledge()));
            else if(focused is CabParkingBrakeButton parking)
                choices.Add(new Choice(parking.ActionLabel,()=>parking.Press(),true));
            else if(focused is CabServiceControl service)
                choices.Add(new Choice(service.ActionLabel,()=>service.Begin()));
            else if(focused is CabLampTestButton lampTest)
                choices.Add(new Choice(lampTest.ActionLabel,()=>lampTest.Press()));
            else if(focused is CabExteriorLightControl exterior)
            {
                for(int i=0;i<exterior.DetentCount;i++){int position=i;choices.Add(new Choice(exterior.Label(i),()=>exterior.Select(position),exterior.IsMarker&&i==4));}
                if(exterior.IsMarker&&exterior.Position==5)
                    for(int i=0;i<3;i++){int aspect=i;string[] zh={"标志灯关闭","白色标志灯","红色尾灯"};string[] en={"Markers off","White markers","Red tail markers"};choices.Add(new Choice(CabLightingKey.English?en[i]:zh[i],()=>exterior.SetManualMarker(aspect),i==2));}
            }
            else if(focused is CabLightingKey lightingKey)
            {
                choices.Add(new Choice(lightingKey.ActionLabel(true),()=>lightingKey.SetSelected(true)));
                choices.Add(new Choice(lightingKey.ActionLabel(false),()=>lightingKey.SetSelected(false)));
            }
            else if(focused is CabPassageDoor passage)
                choices.Add(new Choice(passage.NextActionCloses?"关闭通道门 / CLOSE DOOR":"打开通道门 / OPEN DOOR",()=>passage.Toggle()));
            else if(focused is CabWiperSystem wiper)
            {
                choices.Add(new Choice("关闭并回位 / OFF + PARK",()=>wiper.SelectMode(0)));
                choices.Add(new Choice("低速刮扫 / LOW",()=>wiper.SelectMode(1)));
                choices.Add(new Choice("高速刮扫 / HIGH",()=>wiper.SelectMode(2)));
            }
            else if(focused is CabWorkstationEquipment workstation)
            {
                if(workstation.Kind==CabWorkstationEquipment.EquipmentKind.Radio)
                {
                    choices.Add(new Choice(workstation.SwitchedOn?"关闭本机 / POWER OFF":"开启本机 / POWER ON",()=>workstation.Operate(0)));
                    choices.Add(new Choice("下一频道 / NEXT CHANNEL",()=>workstation.Operate(1)));
                    choices.Add(new Choice("上一频道 / PREVIOUS CHANNEL",()=>workstation.Operate(2)));
                    choices.Add(new Choice("增大音量 / VOLUME UP",()=>workstation.Operate(3)));
                    choices.Add(new Choice("减小音量 / VOLUME DOWN",()=>workstation.Operate(4)));
                }
                choices.Add(new Choice("阅读随车资料 / READ",()=>ShowWorkstationInfo(workstation)));
            }
            else if(focused is CabCirculationFan fan)
            {
                choices.Add(new Choice("关闭 / OFF",()=>fan.SelectSpeed(0)));
                choices.Add(new Choice("低档 / LOW",()=>fan.SelectSpeed(1)));
                choices.Add(new Choice("高档 / HIGH",()=>fan.SelectSpeed(2)));
                choices.Add(new Choice(fan.Oscillating?"停止摆头 / FIX DIRECTION":"开启摆头 / OSCILLATE",()=>fan.ToggleOscillation()));
            }
            else if(focused is CabSunVisor visor)
            {
                choices.Add(new Choice("降低一档",()=>visor.Adjust(1)));
                choices.Add(new Choice("抬高一档",()=>visor.Adjust(-1)));
            }
            else if(focused is CabStorageCabinet cabinet)
            {
                choices.Add(new Choice(cabinet.IsOpen?"关闭柜门 / CLOSE CABINET":"打开柜门 / OPEN CABINET",()=>cabinet.Toggle()));
            }
            else if(focused is CabSafetyEquipment equipment)
            {
                choices.Add(new Choice(equipment.IsManual?"阅读手册 / READ MANUAL":"设备资料 / EQUIPMENT DETAILS",()=>ShowEquipmentInfo(equipment)));
                if(!equipment.IsManual)choices.Add(new Choice(equipment.Held?"放回并固定 / RETURN":"取下检查 / REMOVE",()=>equipment.Toggle(),true));
            }
            else if(focused is CabDrivingTarget d)
            {
                if(d.Action==CabDrivingAction.ParkingBrake)
                {
                    choices.Add(new Choice("施加停放制动",()=>d.Activate(),true));
                    choices.Add(new Choice("缓解停放制动",()=>d.Activate(true),true));
                }
                else
                {
                string up=d.Action==CabDrivingAction.Throttle?"增加一档":d.Action==CabDrivingAction.Reverser?"向前一位":"施加一档";
                string down=d.Action==CabDrivingAction.Throttle?"减少一档":d.Action==CabDrivingAction.Reverser?"向后一位":"缓解一档";
                bool emergency=d.Action==CabDrivingAction.AutomaticBrake&&state.BrakePos==BrakeNotch.Full;
                choices.Add(new Choice(emergency?"进入紧急制动":up,()=>d.Activate(),emergency));
                choices.Add(new Choice(down,()=>d.Activate(true)));
                }
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
            if(focused is CabVigilance vigilance)return vigilance.UnavailableReason;
            if(focused is CabParkingBrakeButton parking)return parking.UnavailableReason;
            if(focused is CabServiceControl service)return service.UnavailableReason;
            if(focused is CabLampTestButton lampTest)return lampTest.UnavailableReason;
            if(focused is CabExteriorLightControl exterior)return index<exterior.DetentCount?exterior.UnavailableReason(index):!exterior.CanOperate()?(CabLightingKey.English?"Move closer to the selector":"请靠近灯光旋钮操作"):null;
            if(focused is CabLightingKey key)return !key.Connected?CabLightingKey.Labels.spare.Get(CabLightingKey.English):!key.CanOperate()?CabLightingKey.Labels.approach.Get(CabLightingKey.English):key.Selected==(index==0)?CabLightingKey.Labels.already.Get(CabLightingKey.English):null;
            if(focused is CabWiperSystem wiper)return !wiper.CanOperate()?"请靠近刮雨器控制板":wiper.Motion.Mode==index?"已在该档位":null;
            if(focused is CabCirculationFan fan)return !fan.CanOperate()?"请靠近风扇台面控制器":index<3&&fan.Speed==index?"已在该档位":null;
            if(focused is CabPassageDoor passage)return passage.Moving?"门扇转动中":null;
            if(focused is CabWorkstationEquipment workstation)
            {
                if(!workstation.CanOperate())return "请靠近设备操作";
                if(workstation.Kind==CabWorkstationEquipment.EquipmentKind.Radio)
                    return index==3&&workstation.Volume==5?"已到最大音量":index==4&&workstation.Volume==0?"已到最小音量":null;
            }
            if(focused is CabSunVisor visor)
                return index==0&&visor.Step==2?"已到最低位置":index==1&&visor.Step==0?"已完全收起":null;
            if(focused is CabDrivingTarget d)
            {
                if(d.Action==CabDrivingAction.ParkingBrake)
                {
                    if(driver==null)return "当前场景未启用模拟后端";
                    if(Mathf.Abs(state.CurrentSpeedKmh)>.1f||state.ThrottleNotch!=0)return "先停车并将牵引归零";
                    return driver.ParkingBrake==(index==0)?"已到该位置":null;
                }
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
                    var target=(ReverserPosition)Mathf.Clamp((int)state.Reverser+(index==0?1:-1),-1,1);
                    if(!state.IsEngineRunning&&target!=ReverserPosition.Neutral)return "等待柴油机运行";
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
            if(focused is CabServiceControl service)
            {
                if(accepted&&primary!=null&&primary.IsPressed())heldService=service;
                feedback=accepted?service.Hint:service.UnavailableReason;feedbackUntil=Time.unscaledTime+1;return;
            }
            if(focused is CabLampTestButton lampTest)
            {
                feedback=accepted?(CabLightingKey.English?"Testing lamps · automatic return after 2 seconds":"试灯中 · 2 秒后恢复实际状态"):lampTest.UnavailableReason;feedbackUntil=Time.unscaledTime+2.5f;return;
            }
            if(focused is CabExteriorLightControl exterior)
            {
                feedback=accepted?exterior.Hint:(CabLightingKey.English?"Move closer to the selector":"请靠近灯光旋钮操作");feedbackUntil=Time.unscaledTime+2.5f;return;
            }
            if(focused is CabLightingKey key)
            {
                feedback=accepted?key.Hint:CabLightingKey.Labels.approach.Get(CabLightingKey.English);feedbackUntil=Time.unscaledTime+2.5f;return;
            }
            if(focused is CabWiperSystem wiper)
            {
                feedback=accepted?wiper.Hint:"请靠近刮雨器控制板操作";feedbackUntil=Time.unscaledTime+2.5f;return;
            }
            if(focused is CabCirculationFan fan)
            {
                feedback=accepted?fan.Hint:"请靠近风扇操作";feedbackUntil=Time.unscaledTime+2.5f;return;
            }
            if(focused is CabPassageDoor passage)
            {
                feedback=accepted?passage.Hint:"请靠近通道门，等待门扇停稳";feedbackUntil=Time.unscaledTime+2.5f;return;
            }
            if(focused is CabWorkstationEquipment workstation)
            {
                feedback=accepted?workstation.Hint:"请靠近设备操作";feedbackUntil=ModalOpen?0:Time.unscaledTime+2.5f;return;
            }
            if(focused is CabSunVisor visor)
            {
                feedback=accepted?visor.Hint:"请靠近遮阳板操作";feedbackUntil=Time.unscaledTime+2.5f;return;
            }
            if(focused is CabStorageCabinet cabinet)
            {
                feedback=accepted?(cabinet.IsOpen?"柜门已打开":"柜门已关闭"):"请靠近柜门操作";
                feedbackUntil=Time.unscaledTime+2.5f;return;
            }
            if(focused is CabSafetyEquipment equipment)
            {
                if(selected==0&&accepted){feedbackUntil=0;return;}
                feedback=accepted?(selected==0?equipment.Description:equipment.Hint):"请靠近原挂架，一次只取用一件。\nReturn to the original bracket; carry one item at a time.";
                feedbackUntil=Time.unscaledTime+(selected==0?8:2.5f);return;
            }
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
            if(driver!=null&&driver.ParkingBrake)return "看向座旁绿色缓解按钮 → 长按缓解停放制动。";
            return state.ThrottleNotch==0?"看向牵引手轮 → 增加一档。提示随时可在交互设置里关闭。":"已完成启动与初次牵引；用实体闸把练习减速和停车。";
        }
        void OnGUI()
        {
            if(font==null)font=Resources.Load<Font>("Fonts/SarasaMonoSC-Regular");
            var old=GUI.skin.font;if(font!=null)GUI.skin.font=font;
            if(equipmentInfo!=null||workstationInfo!=null)
            {
                float width=Mathf.Min(640,Screen.width-24),height=Mathf.Min(740,Screen.height-24);
                GUILayout.BeginArea(new Rect((Screen.width-width)/2,(Screen.height-height)/2,width,height),GUI.skin.box);
                GUILayout.Label(workstationInfo!=null?"乘务工作资料 / CREW DOCUMENTS":equipmentInfo.IsManual?"使用与维护手册 / O&M MANUAL":"随车设备资料 / EQUIPMENT DOSSIER",new GUIStyle(GUI.skin.label){fontSize=22});
                GUILayout.Space(8);
                equipmentInfoScroll=GUILayout.BeginScrollView(equipmentInfoScroll);
                GUILayout.Label(workstationInfo!=null?workstationInfo.Description:equipmentInfo.Description,new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true});
                GUILayout.EndScrollView();
                if(GUILayout.Button("关闭 / CLOSE  ·  Esc",GUILayout.Height(32)))CloseEquipmentInfo();
                GUILayout.EndArea();GUI.skin.font=old;return;
            }
            if(GUI.Button(SettingsButton,"交互设置")){settings=true;inputAfter=Time.unscaledTime+.25f;holdStart=-1;}
            if(settings){DrawSettings();GUI.skin.font=old;return;}
            if(!cursorAim)GUI.Label(new Rect(Screen.width/2f-6,Screen.height/2f-12,20,24),"·");
            if(focusedCollider!=null&&focused!=null&&choices.Count>0)
            {
                Vector3 p=view.WorldToScreenPoint(focusedCollider.bounds.center);
                float h=80+choices.Count*29;
                actionRect=new Rect(Mathf.Clamp(p.x+24,8,Mathf.Max(8,Screen.width-308)),Mathf.Clamp(Screen.height-p.y-h/2,52,Mathf.Max(52,Screen.height-h-118)),300,h);
                GUI.Box(actionRect,GUIContent.none);
                GUI.Label(new Rect(actionRect.x+10,actionRect.y+6,280,24),focused is CabVigilance vigilance?vigilance.Hint:focused is CabParkingBrakeButton parking?parking.Hint:focused is CabServiceControl service?service.Hint:focused is CabLampTestButton lampTest?lampTest.Hint:focused is CabExteriorLightControl exterior?exterior.Hint:focused is CabLightingKey lightingKey?lightingKey.Hint:focused is CabWiperSystem wiper?wiper.Hint:focused is CabCirculationFan fan?fan.Hint:focused is CabPassageDoor passage?passage.Hint:focused is CabWorkstationEquipment workstation?workstation.Hint:focused is CabDrivingTarget d?d.Hint:focused is CabSafetyEquipment equipment?equipment.Hint:focused is CabStorageCabinet cabinet?cabinet.Hint:focused is CabSunVisor visor?visor.Hint:"驾驶台操作");
                for(int i=0;i<choices.Count;i++)
                {
                    string prefix=i==selected?"› ":"  ";
                    string label=prefix+choices[i].label+(choices[i].deliberate?"（长按）":"");
                    if(GUI.Button(new Rect(actionRect.x+10,actionRect.y+31+i*29,280,26),label)){selected=i;holdStart=-1;inputAfter=Time.unscaledTime+.2f;}
                }
                if(choices[selected].reason!=null)GUI.Label(new Rect(actionRect.x+10,actionRect.yMax-48,280,24),choices[selected].reason);
                GUI.Label(new Rect(actionRect.x+10,actionRect.yMax-23,280,22),$"[{PrimaryLabel}] "+((focused is CabParkingBrakeButton||focused is CabLightingKey||focused is CabExteriorLightControl||focused is CabLampTestButton||focused is CabServiceControl)?CabLightingKey.Labels.execute.Get(CabLightingKey.English):"执行 · 滚轮或点击选择"));
            }
            if(guide)GUI.Box(new Rect(12,Screen.height-162,Mathf.Min(680,Screen.width-24),45),GuideText());
            if(Time.unscaledTime<feedbackUntil)GUI.Box(new Rect(Mathf.Max(8,(Screen.width-580)/2),64,580,72),feedback,new GUIStyle(GUI.skin.box){wordWrap=true});
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
