using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RailwayRenaissance.Cab
{
    public enum InteriorLightCircuit { CabCeiling, Task, Instrument, PassengerFront, PassengerRear, Emergency, AssistantTask, Access }

    public sealed class CabInteriorLighting : MonoBehaviour
    {
        [Serializable]
        public struct Fixture
        {
            public Light light;
            public Light bounce;
            public float bounceIntensity;
            public Light lowerBounce;
            public float lowerBounceIntensity;
            public Renderer diffuser;
            public InteriorLightCircuit circuit;
            public float intensity;
        }
        [SerializeField] CabEngineState engineState;
        [SerializeField] Fixture[] fixtures;
        [SerializeField] bool cabCeiling, task, passengerFront, passengerRear, assistantTask, access;
        [SerializeField, Range(0,1)] float instrumentDimmer;
        [SerializeField] float lastInstrumentDimmer = .25f;
        [SerializeField] bool mainPower = true, batteryPower = true;
        [SerializeField] bool reviewControls = true;
        Rect ReviewPanelRect => new Rect(Mathf.Max(250, Screen.width - 260),16,244,400);
        MaterialPropertyBlock properties;
        bool previousLegacy;
        Font reviewFont;
        public bool CabCeilingOn => cabCeiling;
        public bool TaskOn => task;
        public bool AssistantTaskOn => assistantTask;
        public bool BlocksPointer(Vector2 guiPoint) => isActiveAndEnabled && reviewControls && ReviewPanelRect.Contains(guiPoint);
        public bool AccessOn => access;
        public bool MainPower => mainPower;
        public bool BatteryPower => batteryPower;
        public bool EmergencyActive => !mainPower && batteryPower;
        public float InstrumentDimmer => instrumentDimmer;
        public Fixture[] Fixtures => fixtures;
        public bool PassengerZoneOn(int zone)
        {
            if (zone < 0 || zone > 1) throw new ArgumentOutOfRangeException(nameof(zone));
            return zone == 0 ? passengerFront : passengerRear;
        }
        public void Configure(CabEngineState state, Fixture[] bindings)
        {
            engineState = state; fixtures = bindings ?? throw new ArgumentNullException(nameof(bindings));
            AllOff();
        }
        void Awake() { Apply(); }
        void OnEnable() { Apply(); }
        void Update()
        {
            if (reviewControls && Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame) SetCabCeiling(!cabCeiling);
            if (engineState != null && engineState.InstrumentLightOn != previousLegacy)
                SetInstrumentDimmer(engineState.InstrumentLightOn ? lastInstrumentDimmer : 0);
        }
        public void SetCabCeiling(bool on) { cabCeiling = on; Apply(); }
        public void SetTask(bool on) { task = on; Apply(); }
        public void SetAssistantTask(bool on) { assistantTask = on; Apply(); }
        public void SetAccess(bool on) { access = on; Apply(); }
        public void SetCabPreset(int preset)
        {
            if (preset < 0 || preset > 2) throw new ArgumentOutOfRangeException(nameof(preset));
            cabCeiling = preset != 1; task = assistantTask = preset == 2; access = true;
            SetInstrumentDimmer(preset == 1 ? .25f : preset == 0 ? .5f : .65f);
        }
        public void SetPassengerZone(int zone, bool on)
        {
            if (zone < 0 || zone > 1) throw new ArgumentOutOfRangeException(nameof(zone));
            if (zone == 0) passengerFront = on; else passengerRear = on;
            Apply();
        }
        public void SetInstrumentDimmer(float level)
        {
            if (float.IsNaN(level) || float.IsInfinity(level)) throw new ArgumentOutOfRangeException(nameof(level));
            instrumentDimmer = Mathf.Clamp01(level);
            if (instrumentDimmer > 0) lastInstrumentDimmer = instrumentDimmer;
            Apply();
        }
        public void SetSupply(bool normal, bool backup) { mainPower = normal; batteryPower = backup; Apply(); }
        public void AllOff()
        {
            cabCeiling = task = passengerFront = passengerRear = assistantTask = access = false; instrumentDimmer = 0; Apply();
        }
        float Level(InteriorLightCircuit circuit)
        {
            if (!isActiveAndEnabled) return 0;
            if (circuit == InteriorLightCircuit.Emergency) return EmergencyActive ? 1 : 0;
            if (!mainPower) return 0;
            switch (circuit)
            {
                case InteriorLightCircuit.CabCeiling: return cabCeiling ? 1 : 0;
                case InteriorLightCircuit.Task: return task ? 1 : 0;
                case InteriorLightCircuit.AssistantTask: return assistantTask ? 1 : 0;
                case InteriorLightCircuit.Access: return access ? 1 : 0;
                case InteriorLightCircuit.Instrument: return instrumentDimmer;
                case InteriorLightCircuit.PassengerFront: return passengerFront ? 1 : 0;
                case InteriorLightCircuit.PassengerRear: return passengerRear ? 1 : 0;
                default: return 0;
            }
        }
        void Apply()
        {
            previousLegacy = instrumentDimmer > 0;
            if (engineState != null) engineState.InstrumentLightOn = previousLegacy;
            if (fixtures == null) return;
            properties ??= new MaterialPropertyBlock();
            foreach (var fixture in fixtures)
            {
                float level = Level(fixture.circuit);
                if (fixture.light != null) { fixture.light.enabled = level > 0; fixture.light.intensity = fixture.intensity * level; }
                if (fixture.bounce != null) { fixture.bounce.enabled = level > 0; fixture.bounce.intensity = fixture.bounceIntensity * level; }
                if (fixture.lowerBounce != null) { fixture.lowerBounce.enabled = level > 0; fixture.lowerBounce.intensity = fixture.lowerBounceIntensity * level; }
                if (fixture.diffuser == null) continue;
                fixture.diffuser.GetPropertyBlock(properties);
                var tint = fixture.light != null ? fixture.light.color : Color.white;
                properties.SetColor("_EmissionColor", tint * level * (fixture.circuit == InteriorLightCircuit.Emergency ? .35f : .65f));
                fixture.diffuser.SetPropertyBlock(properties);
            }
            GetComponent<CabDiffuseFill>()?.Refresh();
        }
        void OnDisable() { Apply(); }
        void OnGUI()
        {
            if (!reviewControls) return;
            if (reviewFont == null) reviewFont = Resources.Load<Font>("Fonts/SarasaMonoSC-Regular");
            var font = GUI.skin.font; if (reviewFont != null) GUI.skin.font = reviewFont;
            GUILayout.BeginArea(ReviewPanelRect, GUI.skin.box);
            GUILayout.Label("室内照明（I 切换顶灯）");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("正常")) SetCabPreset(0);
            if (GUILayout.Button("夜间")) SetCabPreset(1);
            if (GUILayout.Button("检修")) SetCabPreset(2);
            GUILayout.EndHorizontal();
            bool ceiling = GUILayout.Toggle(cabCeiling,"司机室顶灯"); if (ceiling != cabCeiling) SetCabCeiling(ceiling);
            bool work = GUILayout.Toggle(task,"司机工作灯"); if (work != task) SetTask(work);
            bool assistant = GUILayout.Toggle(assistantTask,"副司机阅读灯"); if (assistant != assistantTask) SetAssistantTask(assistant);
            bool doors = GUILayout.Toggle(access,"两侧门区低位灯"); if (doors != access) SetAccess(doors);
            GUILayout.Label("仪表照明");
            float dim = GUILayout.HorizontalSlider(instrumentDimmer,0,1); if (!Mathf.Approximately(dim,instrumentDimmer)) SetInstrumentDimmer(dim);
            bool front = GUILayout.Toggle(passengerFront,"客舱前区"); if (front != passengerFront) SetPassengerZone(0,front);
            bool rear = GUILayout.Toggle(passengerRear,"客舱后区"); if (rear != passengerRear) SetPassengerZone(1,rear);
            bool normal = GUILayout.Toggle(mainPower,"主照明电源");
            bool backup = GUILayout.Toggle(batteryPower,"应急电源可用");
            if (normal != mainPower || backup != batteryPower) SetSupply(normal,backup);
            GUILayout.Label(EmergencyActive ? "应急通道照明已接通" : !mainPower && !batteryPower ? "照明电源均不可用" : "应急回路待命");
            if (GUILayout.Button("关闭普通照明")) AllOff();
            GUILayout.EndArea(); GUI.skin.font = font;
        }
    }
}
