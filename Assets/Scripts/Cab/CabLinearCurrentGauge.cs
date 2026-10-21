using System;
using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabLinearCurrentGauge : MonoBehaviour
    {
        public const float FullScaleAmps = 1500f;
        const float AxisLength = .01f;
        const float Radius = (.0715f * .0715f + .006f * .006f) / .012f;
        [SerializeField] CabEngineState engineState;
        [SerializeField] Transform indicator, origin, axisX, axisY, axisZ;
        [SerializeField] Quaternion referenceRotation;
        public float InputAmps { get; private set; }
        public float DisplayedAmps { get; private set; }
        public bool IsOutOfRange { get; private set; }
        public bool HasValidInput { get; private set; }

        public void Configure(Transform moving, Transform zero, Transform x, Transform y, Transform z)
        {
            indicator = moving; origin = zero; axisX = x; axisY = y; axisZ = z;
            if (indicator == null || origin == null || axisX == null || axisY == null || axisZ == null)
                throw new InvalidOperationException("Current gauge calibration transforms missing");
            referenceRotation = indicator.localRotation;
        }

        public void BindEngineState(CabEngineState state)
        {
            engineState = state;
            SetCurrentAmps(state == null ? 0f : state.TractionCurrentAmps);
        }

        void OnEnable()
        {
            if (engineState == null) engineState = GetComponentInParent<CabEngineState>();
        }

        void LateUpdate()
        {
            RefreshFromState();
        }

        public void RefreshFromState()
        {
            if (engineState != null) SetCurrentAmps(engineState.TractionCurrentAmps);
        }

        public void SetCurrentAmps(float amps)
        {
            if (indicator == null || origin == null) return;
            InputAmps = amps;
            HasValidInput = !float.IsNaN(amps) && !float.IsInfinity(amps);
            IsOutOfRange = HasValidInput && (amps < 0f || amps > FullScaleAmps);
            DisplayedAmps = HasValidInput ? Mathf.Clamp(amps, 0f, FullScaleAmps) : 0f;
            float low = Mathf.Asin(-.063f / Radius);
            float angle = Mathf.Lerp(low, -low, DisplayedAmps / FullScaleAmps);
            float x = .001f + Radius - Radius * Mathf.Cos(angle) - .006f;
            float z = .089f + Radius * Mathf.Sin(angle);
            var world = origin.position + (axisX.position - origin.position) * (x / AxisLength)
                + (axisY.position - origin.position) * (-.0189f / AxisLength)
                + (axisZ.position - origin.position) * (z / AxisLength);
            indicator.localPosition = indicator.parent.InverseTransformPoint(world);
            var localX = indicator.parent.InverseTransformVector(axisX.position - origin.position);
            var localY = indicator.parent.InverseTransformVector(axisY.position - origin.position);
            var localZ = indicator.parent.InverseTransformVector(axisZ.position - origin.position);
            var rotationAxis = localY.normalized;
            float handedness = Mathf.Sign(Vector3.Dot(Vector3.Cross(localX, localY), localZ));
            float referenceAngle = Mathf.Lerp(low, -low, .30f);
            indicator.localRotation = Quaternion.AngleAxis(handedness * (angle - referenceAngle) * Mathf.Rad2Deg, rotationAxis)
                * referenceRotation;
        }
    }
}
