using System;
using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabDuplexPressureGauges : MonoBehaviour
    {
        [SerializeField] CabEngineState engineState;
        [SerializeField] Transform[] pointers = new Transform[4];
        [SerializeField] Quaternion[] referenceRotations = new Quaternion[4];
        [SerializeField] Vector3[] axes = new Vector3[4];
        static readonly float[] FullScale = { 1600f, 1600f, 1000f, 1000f };
        static readonly float[] ReferenceKpa = { 840f, 180f, 500f, 470f };
        public bool HasEqualizingReservoirSignal { get; private set; }
        public bool[] ValidChannels { get; } = new bool[4];
        public bool[] OutOfRange { get; } = new bool[4];
        public float[] DisplayedKpa { get; } = new float[4];

        public void Configure(Transform[] moving, Transform[] modelAxes, Transform[] modelOrigins)
        {
            if (moving.Length != 4 || modelAxes.Length != 4 || modelOrigins.Length != 4)
                throw new ArgumentException("Four independent pressure channels required");
            pointers = moving;
            referenceRotations = new Quaternion[4]; axes = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                if (pointers[i] == null || modelAxes[i] == null || modelOrigins[i] == null)
                    throw new ArgumentException("Pressure gauge calibration missing");
                referenceRotations[i] = pointers[i].localRotation;
                // Blender's Y rotation is converted to Unity using a handedness marker from export.
                var y = pointers[i].parent.InverseTransformVector(modelAxes[i].position - modelOrigins[i].position);
                axes[i] = -y.normalized;
            }
        }

        void OnEnable()
        {
            if (engineState == null) engineState = GetComponentInParent<CabEngineState>();
        }

        void LateUpdate() { RefreshFromState(); }

        public void BindEngineState(CabEngineState state)
        {
            engineState = state;
            if (state == null) SetPressures(float.NaN, float.NaN, float.NaN);
            else RefreshFromState();
        }

        public void RefreshFromState()
        {
            if (engineState != null)
                SetPressures(engineState.MainReservoirKpa, engineState.BrakeCylinderKpa, engineState.BrakePipeKpa);
        }

        public void SetPressures(float mainReservoir, float brakeCylinder, float brakePipe,
            float equalizingReservoir = float.NaN)
        {
            HasEqualizingReservoirSignal = !float.IsNaN(equalizingReservoir) && !float.IsInfinity(equalizingReservoir);
            ApplyChannel(0, mainReservoir);
            ApplyChannel(1, brakeCylinder);
            ApplyChannel(2, brakePipe);
            ApplyChannel(3, equalizingReservoir);
        }

        void ApplyChannel(int i, float value)
        {
            bool valid = !float.IsNaN(value) && !float.IsInfinity(value);
            ValidChannels[i] = valid;
            OutOfRange[i] = valid && (value < 0 || value > FullScale[i]);
            DisplayedKpa[i] = valid ? Mathf.Clamp(value, 0f, FullScale[i]) : 0f;
            if (pointers[i] == null) return;
            // Keep the physical needle. Zero is a parked position when ValidChannels[i] is false.
            pointers[i].gameObject.SetActive(true);
            float delta = 270f * (DisplayedKpa[i] - ReferenceKpa[i]) / FullScale[i];
            pointers[i].localRotation = Quaternion.AngleAxis(delta, axes[i]) * referenceRotations[i];
        }
    }
}
