using UnityEngine;
using UnityEngine.InputSystem;

namespace RailwayRenaissance.Cab
{
    public enum ExteriorLightMode { Off, Marker, Dim, Bright, Tail }

    public sealed class CabExteriorLighting : MonoBehaviour
    {
        [SerializeField] CabEngineState engineState;
        [SerializeField] Light[] beams;
        [SerializeField] Renderer[] emitters;
        [SerializeField] bool[] marker;
        [SerializeField] ExteriorLightMode mode;
        [SerializeField] bool powerAvailable = true;
        [SerializeField] bool reviewControls = true;
        MaterialPropertyBlock properties;
        bool lastLegacySwitch;
        [SerializeField] ExteriorLightMode previousOnMode = ExteriorLightMode.Dim;
        public ExteriorLightMode Mode => mode;
        public bool PowerAvailable => powerAvailable;
        public Light[] Beams => beams;

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
                SetMode(engineState.HeadlightOn ? previousOnMode : ExteriorLightMode.Off);
        }
        public void CycleMode() { SetMode((ExteriorLightMode)(((int)mode + 1) % 5)); }
        public void SetMode(ExteriorLightMode value)
        {
            mode = value;
            if (value != ExteriorLightMode.Off) previousOnMode = value;
            Apply();
        }
        public void SetPowerAvailable(bool available) { powerAvailable = available; Apply(); }
        public void Refresh() { Apply(); }
        void Apply()
        {
            lastLegacySwitch = mode != ExteriorLightMode.Off;
            if (engineState != null) engineState.HeadlightOn = lastLegacySwitch;
            if (beams == null || emitters == null || marker == null) return;
            properties ??= new MaterialPropertyBlock();
            for (int i = 0; i < beams.Length; i++)
            {
                bool small = marker[i];
                bool active = isActiveAndEnabled && powerAvailable && mode != ExteriorLightMode.Off &&
                    (small || mode == ExteriorLightMode.Dim || mode == ExteriorLightMode.Bright);
                var color = mode == ExteriorLightMode.Tail ? new Color(1,.025f,.012f) : new Color(1,.86f,.69f);
                float intensity = small ? (mode == ExteriorLightMode.Tail ? 3 : 5) :
                    (i == 0 ? (mode == ExteriorLightMode.Bright ? 900 : 170) : (mode == ExteriorLightMode.Bright ? 120 : 35));
                if (beams[i] != null)
                {
                    beams[i].enabled = active; beams[i].color = color; beams[i].intensity = active ? intensity : 0;
                    if (!small) { beams[i].spotAngle = mode == ExteriorLightMode.Bright ? (i == 0 ? 22 : 40) : (i == 0 ? 32 : 48); }
                }
                if (emitters[i] == null) continue;
                emitters[i].GetPropertyBlock(properties);
                properties.SetColor("_EmissionColor", active ? color * (small ? 2 : mode == ExteriorLightMode.Bright ? 5 : 2.5f) : Color.black);
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
            GUILayout.BeginArea(new Rect(16,16,220,240), GUI.skin.box);
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
