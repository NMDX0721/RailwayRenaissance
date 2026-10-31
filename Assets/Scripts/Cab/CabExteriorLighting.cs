using UnityEngine;
using UnityEngine.InputSystem;

namespace RailwayRenaissance.Cab
{
    public enum ExteriorLightMode { Off, Marker, Dim, Bright, Tail }
    public enum ExteriorHeadlightLevel { Off, Dim, Bright }
    public enum ExteriorMarkerAspect { Off, White, Red }
    public enum ExteriorOperatingMode { Off, DayRunning, NightRunning, Shunting, Tail, Manual }

    public sealed class CabExteriorLighting : MonoBehaviour
    {
        [SerializeField] CabEngineState engineState;
        [SerializeField] Light[] beams;
        [SerializeField] Renderer[] emitters;
        [SerializeField] bool[] marker;
        [SerializeField] ExteriorLightMode mode;
        [SerializeField] bool powerAvailable = true;
        [SerializeField] bool reviewControls = true;
        static readonly Rect ReviewPanelRect = new Rect(16,16,220,240);
        [SerializeField] ExteriorOperatingMode operatingMode=ExteriorOperatingMode.Manual, previousOperatingMode=ExteriorOperatingMode.Manual;
        [SerializeField] bool separateSelections;
        [SerializeField] ExteriorHeadlightLevel headlightLevel, previousHeadlightLevel=ExteriorHeadlightLevel.Dim;
        [SerializeField] ExteriorMarkerAspect markerAspect, previousMarkerAspect=ExteriorMarkerAspect.White;
        MaterialPropertyBlock properties;
        bool lastLegacySwitch;
        [SerializeField] ExteriorLightMode previousOnMode = ExteriorLightMode.Dim;
        public ExteriorLightMode Mode => mode;
        public bool BlocksPointer(Vector2 guiPoint) => isActiveAndEnabled && reviewControls && ReviewPanelRect.Contains(guiPoint);
        public bool PowerAvailable => powerAvailable;
        public Light[] Beams => beams;
        public ExteriorOperatingMode OperatingMode=>operatingMode;
        public ExteriorHeadlightLevel HeadlightLevel { get { EnsureSelections();return headlightLevel; } }
        public ExteriorMarkerAspect MarkerAspect { get { EnsureSelections();return markerAspect; } }
        public void SetReviewControls(bool enabled){reviewControls=enabled;}
        void EnsureSelections()
        {
            if(separateSelections)return;
            Decode(mode,out headlightLevel,out markerAspect);Decode(previousOnMode,out previousHeadlightLevel,out previousMarkerAspect);separateSelections=true;
        }
        static void Decode(ExteriorLightMode value,out ExteriorHeadlightLevel head,out ExteriorMarkerAspect aspect)
        {
            head=value==ExteriorLightMode.Bright?ExteriorHeadlightLevel.Bright:value==ExteriorLightMode.Dim?ExteriorHeadlightLevel.Dim:ExteriorHeadlightLevel.Off;
            aspect=value==ExteriorLightMode.Off?ExteriorMarkerAspect.Off:value==ExteriorLightMode.Tail?ExteriorMarkerAspect.Red:ExteriorMarkerAspect.White;
        }
        public void SetHeadlightLevel(ExteriorHeadlightLevel value)
        {
            if((int)value<0||(int)value>2)throw new System.ArgumentOutOfRangeException(nameof(value));EnsureSelections();
            Select(value,value!=ExteriorHeadlightLevel.Off&&markerAspect==ExteriorMarkerAspect.Red?ExteriorMarkerAspect.Off:markerAspect);
        }
        public void SetMarkerAspect(ExteriorMarkerAspect value)
        {
            if((int)value<0||(int)value>2)throw new System.ArgumentOutOfRangeException(nameof(value));EnsureSelections();
            Select(value==ExteriorMarkerAspect.Red?ExteriorHeadlightLevel.Off:headlightLevel,value);
        }
        public void SetOperatingMode(ExteriorOperatingMode value)
        {
            if((int)value<0||(int)value>5)throw new System.ArgumentOutOfRangeException(nameof(value));EnsureSelections();
            switch(value)
            {
                case ExteriorOperatingMode.Off:Select(ExteriorHeadlightLevel.Off,ExteriorMarkerAspect.Off);break;
                case ExteriorOperatingMode.DayRunning:Select(ExteriorHeadlightLevel.Bright,ExteriorMarkerAspect.White);break;
                case ExteriorOperatingMode.NightRunning:Select(ExteriorHeadlightLevel.Dim,ExteriorMarkerAspect.White);break;
                case ExteriorOperatingMode.Shunting:Select(ExteriorHeadlightLevel.Off,ExteriorMarkerAspect.White);break;
                case ExteriorOperatingMode.Tail:Select(ExteriorHeadlightLevel.Off,ExteriorMarkerAspect.Red);break;
            }
            operatingMode=value;if(mode!=ExteriorLightMode.Off)previousOperatingMode=value;Apply();
        }
        void Select(ExteriorHeadlightLevel head,ExteriorMarkerAspect aspect)
        {
            operatingMode=ExteriorOperatingMode.Manual;headlightLevel=head;markerAspect=aspect;separateSelections=true;
            mode=head==ExteriorHeadlightLevel.Bright?ExteriorLightMode.Bright:head==ExteriorHeadlightLevel.Dim?ExteriorLightMode.Dim:aspect==ExteriorMarkerAspect.Red?ExteriorLightMode.Tail:aspect==ExteriorMarkerAspect.White?ExteriorLightMode.Marker:ExteriorLightMode.Off;
            if(mode!=ExteriorLightMode.Off){previousOnMode=mode;previousHeadlightLevel=head;previousMarkerAspect=aspect;previousOperatingMode=ExteriorOperatingMode.Manual;}Apply();
        }

        public void Configure(CabEngineState state, Light[] lights, Renderer[] surfaces, bool[] markers)
        {
            if (lights == null || surfaces == null || markers == null || lights.Length != surfaces.Length || lights.Length != markers.Length)
                throw new System.ArgumentException("Exterior light bindings must have matching lengths");
            engineState = state; beams = lights; emitters = surfaces; marker = markers;
            SetMode(ExteriorLightMode.Off);
        }
        void Awake() { Apply(); }
        void OnEnable() { Apply(); }
        void Update()
        {
            if (reviewControls && Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame) CycleMode();
            if (engineState != null && engineState.HeadlightOn != lastLegacySwitch)
            {
                if(engineState.HeadlightOn){if(previousOperatingMode==ExteriorOperatingMode.Manual)Select(previousHeadlightLevel,previousMarkerAspect);else SetOperatingMode(previousOperatingMode);}else SetMode(ExteriorLightMode.Off);
            }
        }
        public void CycleMode() { SetMode((ExteriorLightMode)(((int)mode + 1) % 5)); }
        public void SetMode(ExteriorLightMode value)
        {
            if((int)value<0||(int)value>4)throw new System.ArgumentOutOfRangeException(nameof(value));
            Decode(value,out var head,out var aspect);Select(head,aspect);
        }
        public void SetPowerAvailable(bool available) { powerAvailable = available; Apply(); }
        public void Refresh() { Apply(); }
        void Apply()
        {
            EnsureSelections();
            lastLegacySwitch = mode != ExteriorLightMode.Off;
            if (engineState != null) engineState.HeadlightOn = lastLegacySwitch;
            if (beams == null || emitters == null || marker == null) return;
            properties ??= new MaterialPropertyBlock();
            for (int i = 0; i < beams.Length; i++)
            {
                bool small = marker[i];
                bool active=isActiveAndEnabled&&powerAvailable&&(small?markerAspect!=ExteriorMarkerAspect.Off:headlightLevel!=ExteriorHeadlightLevel.Off&&(operatingMode!=ExteriorOperatingMode.DayRunning||i==0));
                var color=small&&markerAspect==ExteriorMarkerAspect.Red?new Color(1,.025f,.012f):new Color(1,.86f,.69f);
                float intensity = small ? (markerAspect == ExteriorMarkerAspect.Red ? 3 : 5) :
                    (i == 0 ? (headlightLevel == ExteriorHeadlightLevel.Bright ? 900 : 170) : (headlightLevel == ExteriorHeadlightLevel.Bright ? 120 : 35));
                if (beams[i] != null)
                {
                    beams[i].enabled = active; beams[i].color = color; beams[i].intensity = active ? intensity : 0;
                    if (!small) { beams[i].spotAngle = headlightLevel == ExteriorHeadlightLevel.Bright ? (i == 0 ? 22 : 40) : (i == 0 ? 32 : 48); }
                }
                if (emitters[i] == null) continue;
                emitters[i].GetPropertyBlock(properties);
                properties.SetColor("_EmissionColor", active ? color * (small ? 2 : headlightLevel == ExteriorHeadlightLevel.Bright ? 5 : 2.5f) : Color.black);
                properties.SetColor("_BaseColor", active ? color : new Color(.24f,.22f,.17f));
                emitters[i].SetPropertyBlock(properties);
            }
        }
        void OnDisable()
        {
            if (beams != null) foreach (var beam in beams) if (beam != null) beam.enabled = false;
            if (emitters == null) return;
            properties ??= new MaterialPropertyBlock();
            foreach (var surface in emitters) if (surface != null)
            {
                surface.GetPropertyBlock(properties); properties.SetColor("_EmissionColor", Color.black); surface.SetPropertyBlock(properties);
            }
        }
        Font reviewFont;
        void OnGUI()
        {
            if (!reviewControls) return;
            if (reviewFont == null) reviewFont = Resources.Load<Font>("Fonts/SarasaMonoSC-Regular");
            var previousFont = GUI.skin.font; if (reviewFont != null) GUI.skin.font = reviewFont;
            GUILayout.BeginArea(ReviewPanelRect, GUI.skin.box);
            GUILayout.Label("NF-5K 外部照明（L 切换）");
            string[] labels = { "关闭", "白色标志灯", "弱光", "强光", "红色尾灯" };
            for (int i = 0; i < labels.Length; i++)
                if (GUILayout.Button(((int)mode == i ? "● " : "") + labels[i])) SetMode((ExteriorLightMode)i);
            bool powered = GUILayout.Toggle(powerAvailable, "照明电源");
            if (powered != powerAvailable) SetPowerAvailable(powered);
            GUILayout.EndArea(); GUI.skin.font = previousFont;
        }
    }
}
