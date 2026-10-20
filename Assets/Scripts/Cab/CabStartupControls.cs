using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabStartupControls : MonoBehaviour
    {
        [SerializeField] CabEngineState engineState;
        [SerializeField] GameObject keyVisual, looseKeyPrefab;
        [SerializeField] Transform keyPivot, lockPivot, pumpSelector;
        [SerializeField] Quaternion pumpOffRotation;
        [SerializeField] Renderer powerLamp, runningLamp;
        MaterialPropertyBlock lampProperties;
        [SerializeField] Quaternion offRotation;
        [SerializeField] Vector3 turnAxis;
        [SerializeField] bool keyInserted;
        [SerializeField] bool masterEnabled;
        public bool KeyInserted => keyInserted;
        public bool MasterEnabled => masterEnabled;
        public string LastWarning { get; private set; }

        public void Configure(GameObject visual, GameObject looseKey, Transform pivot, Vector3 axis, bool inserted,
            Transform pump, Renderer power, Renderer running)
        {
            keyVisual = visual; looseKeyPrefab = looseKey; keyPivot = pivot;
            turnAxis = axis.normalized; offRotation = pivot.localRotation;
            lockPivot = pivot; pumpSelector = pump; pumpOffRotation = pump.localRotation;
            powerLamp = power; runningLamp = running;
            keyInserted = inserted; masterEnabled = false; ApplyVisual();
        }
        public void BindEngineState(CabEngineState state) { engineState = state; ApplyVisual(); }
        void LateUpdate() { UpdateAuxiliaryVisuals(); }
        public bool InsertKey()
        {
            if (keyInserted) return false;
            if (keyVisual == null && looseKeyPrefab != null)
            {
                var spawned = Instantiate(looseKeyPrefab, transform);
                spawned.transform.localPosition = Vector3.zero;
                spawned.transform.localRotation = Quaternion.identity;
                keyVisual = spawned;
                foreach (var t in spawned.GetComponentsInChildren<Transform>())
                    if (t.name == "Key_Turn_Pivot") { keyPivot = t; offRotation = t.localRotation; break; }
            }
            if (keyVisual == null) { LastWarning = "缺少独立钥匙资源"; return false; }
            keyInserted = true; ApplyVisual(); return true;
        }
        public bool RemoveKey()
        {
            if (!keyInserted || masterEnabled || (engineState != null && engineState.IsEngineStarted))
            { LastWarning = "关闭主控并停机后才能拔钥匙"; return false; }
            keyInserted = false; ApplyVisual(); return true;
        }
        public bool SetMaster(bool enabled)
        {
            if (enabled && !keyInserted) { LastWarning = "请先插入主控钥匙"; return false; }
            if (!enabled && engineState != null && engineState.IsEngineStarted)
            { LastWarning = "请先停机再关闭主控"; return false; }
            masterEnabled = enabled; ApplyVisual(); return true;
        }
        public bool SetFuelPump(bool enabled)
        {
            if (engineState == null || !masterEnabled || engineState.IsEngineStarted) return false;
            engineState.FuelPumpOn = enabled; ApplyVisual(); return true;
        }
        public bool TryStartEngine()
        {
            if (engineState == null || !keyInserted || !masterEnabled || !engineState.FuelPumpOn || engineState.IsEngineStarted)
            { LastWarning = "启动条件不满足：检查钥匙、主控与燃油泵"; return false; }
            engineState.StartEngine(); ApplyVisual(); return true;
        }
        public void StopEngine() { if (engineState != null) engineState.StopEngine(); ApplyVisual(); }
        void ApplyVisual()
        {
            if (keyVisual != null) keyVisual.SetActive(keyInserted);
            if (keyPivot != null) keyPivot.localRotation = Quaternion.AngleAxis(masterEnabled ? -90 : 0, turnAxis) * offRotation;
            if (lockPivot != null && lockPivot != keyPivot)
                lockPivot.localRotation = Quaternion.AngleAxis(masterEnabled ? -90 : 0, turnAxis) * offRotation;
            UpdateAuxiliaryVisuals();
        }
        void UpdateAuxiliaryVisuals()
        {
            if (pumpSelector != null) pumpSelector.localRotation = Quaternion.AngleAxis(
                engineState != null && engineState.FuelPumpOn ? -30 : 0, turnAxis) * pumpOffRotation;
            SetLamp(powerLamp, masterEnabled && keyInserted);
            SetLamp(runningLamp, engineState != null && engineState.IsEngineStarted);
        }
        void SetLamp(Renderer lamp, bool enabled)
        {
            if (lamp == null) return;
            if (lampProperties == null) lampProperties = new MaterialPropertyBlock();
            lamp.GetPropertyBlock(lampProperties);
            lampProperties.SetColor("_EmissionColor", enabled ? new Color(.04f,.6f,.09f) : Color.black);
            lamp.SetPropertyBlock(lampProperties);
        }
    }
}
