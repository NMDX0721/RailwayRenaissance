using System;
using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabTemperatureGauge : MonoBehaviour
    {
        [Serializable] public class Channel
        {
            public float minimumC, maximumC, minorStepC, majorStepC, redStartC, previewC;
        }
        [Serializable] class Profile { public int schemaVersion; public Channel water, oil; }
        [SerializeField] CabEngineState engineState;
        [SerializeField] bool oilChannel;
        [SerializeField] Channel calibration;
        [SerializeField] Transform indicator, origin, axisX, axisY, axisZ;
        [SerializeField] Quaternion referenceRotation;
        const float Radius = (.0715f * .0715f + .006f * .006f) / .012f;
        public float InputCelsius { get; private set; } = float.NaN;
        public float DisplayedCelsius { get; private set; }
        public bool HasValidInput { get; private set; }
        public bool UnderRange { get; private set; }
        public bool OverRange { get; private set; }
        public bool InRedZone { get; private set; }
        public bool IsOilChannel => oilChannel;
        public Channel Calibration => calibration;

        public void Configure(bool oil, Transform moving, Transform zero, Transform x, Transform y, Transform z)
        {
            var resource = Resources.Load<TextAsset>("Cab/temperature_instrument_profile");
            if (resource == null) throw new InvalidOperationException("Temperature instrument profile missing");
            var profile = JsonUtility.FromJson<Profile>(resource.text);
            if (profile == null || profile.schemaVersion != 1) throw new InvalidOperationException("Unknown temperature profile");
            calibration = oil ? profile.oil : profile.water;
            if (calibration == null || calibration.maximumC <= calibration.minimumC
                || calibration.redStartC <= calibration.minimumC || calibration.redStartC >= calibration.maximumC)
                throw new InvalidOperationException("Invalid temperature calibration");
            oilChannel = oil; indicator = moving; origin = zero; axisX = x; axisY = y; axisZ = z;
            if (moving == null || zero == null || x == null || y == null || z == null)
                throw new InvalidOperationException("Temperature calibration transforms missing");
            referenceRotation = indicator.localRotation;
            SetTemperature(float.NaN);
        }

        public void BindEngineState(CabEngineState state)
        {
            engineState = state;
            RefreshFromState();
        }
        void OnEnable()
        {
            if (engineState == null) engineState = GetComponentInParent<CabEngineState>();
            if (calibration != null) RefreshFromState();
        }
        void LateUpdate() { if (engineState != null) RefreshFromState(); }
        public void RefreshFromState()
        {
            // Oil temperature is not present in CabEngineState; never substitute water or oil pressure.
            SetTemperature(engineState != null && !oilChannel ? engineState.WaterTempCelsius : float.NaN);
        }
        public void SetTemperature(float value)
        {
            if (calibration == null || indicator == null) return;
            InputCelsius = value;
            HasValidInput = !float.IsNaN(value) && !float.IsInfinity(value);
            UnderRange = HasValidInput && value < calibration.minimumC;
            OverRange = HasValidInput && value > calibration.maximumC;
            InRedZone = HasValidInput && value >= calibration.redStartC;
            DisplayedCelsius = HasValidInput ? Mathf.Clamp(value, calibration.minimumC, calibration.maximumC) : calibration.minimumC;
            float u = Mathf.InverseLerp(calibration.minimumC, calibration.maximumC, DisplayedCelsius);
            float low = Mathf.Asin(-.063f / Radius);
            float angle = Mathf.Lerp(low, -low, u);
            float x = .001f + Radius - Radius * Mathf.Cos(angle) - .006f;
            float z = .089f + Radius * Mathf.Sin(angle);
            var world = origin.position + (axisX.position - origin.position) * (x / .01f)
                + (axisY.position - origin.position) * (-.01855f / .01f)
                + (axisZ.position - origin.position) * (z / .01f);
            indicator.localPosition = indicator.parent.InverseTransformPoint(world);
            var localX = indicator.parent.InverseTransformVector(axisX.position - origin.position);
            var localY = indicator.parent.InverseTransformVector(axisY.position - origin.position);
            var localZ = indicator.parent.InverseTransformVector(axisZ.position - origin.position);
            float handedness = Mathf.Sign(Vector3.Dot(Vector3.Cross(localX, localY), localZ));
            float referenceAngle = Mathf.Lerp(low, -low,
                Mathf.InverseLerp(calibration.minimumC, calibration.maximumC, calibration.previewC));
            indicator.localRotation = Quaternion.AngleAxis(handedness * (angle - referenceAngle) * Mathf.Rad2Deg, localY.normalized)
                * referenceRotation;
        }
    }
}
