using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public enum CabDrivingAction { Throttle, AutomaticBrake, IndependentBrake, Reverser, ParkingBrake }

    public sealed class CabDrivingTarget : MonoBehaviour
    {
        [SerializeField] CabEngineState state;
        [SerializeField] CabDrivingAction action;
        [SerializeField] Quaternion restRotation;
        [SerializeField] Vector3 axis = Vector3.forward;
        [SerializeField] float travelDegrees;
        public CabDrivingAction Action => action;
        public string Hint => action switch
        {
            CabDrivingAction.Throttle => $"牵引手轮 · {state.ThrottleNotch}/8",
            CabDrivingAction.AutomaticBrake => $"自动制动 · {state.GetBrakeNotchString()}",
            CabDrivingAction.IndependentBrake => $"独立制动 · {state.IndependentBrakeNotch:P0}",
            CabDrivingAction.ParkingBrake => "停放制动 · " + (state.GetComponent<CabSimulationDriver>()?.ParkingBrake == true ? "已施加" : "已缓解"),
            _ => $"换向 · {state.GetReverserString()}"
        };
        public void Configure(CabEngineState owner, CabDrivingAction kind, Vector3 localAxis, float travel)
        {
            state = owner; action = kind; axis = localAxis.normalized;
            travelDegrees = travel; restRotation = transform.localRotation;
        }
        public bool Activate(bool secondary = false)
        {
            if (state == null) return false;
            int step = secondary ? -1 : 1;
            switch (action)
            {
                case CabDrivingAction.Throttle: return state.SetThrottle(state.ThrottleNotch + step);
                case CabDrivingAction.AutomaticBrake:
                    state.SetBrakeNotch((BrakeNotch)Mathf.Clamp((int)state.BrakePos + step, 0, 4)); break;
                case CabDrivingAction.IndependentBrake:
                    state.SetIndependentBrake(state.IndependentBrakeNotch + step * .25f); break;
                case CabDrivingAction.Reverser:
                    return state.SetReverser((ReverserPosition)Mathf.Clamp((int)state.Reverser + step, -1, 1));
                case CabDrivingAction.ParkingBrake:
                    var driver = state.GetComponent<CabSimulationDriver>();
                    if (driver == null || Mathf.Abs(state.CurrentSpeedKmh) > .1f || state.ThrottleNotch != 0) return false;
                    driver.ParkingBrake = !secondary; break;
            }
            ApplyVisual(); return true;
        }
        void LateUpdate() { ApplyVisual(); }
        public void ApplyVisual()
        {
            if (state == null) return;
            float fraction = action switch
            {
                CabDrivingAction.Throttle => state.ThrottleNotch / 8f,
                CabDrivingAction.AutomaticBrake => ((int)state.BrakePos - 1) / 4f,
                CabDrivingAction.IndependentBrake => state.IndependentBrakeNotch,
                CabDrivingAction.ParkingBrake => state.GetComponent<CabSimulationDriver>()?.ParkingBrake == true ? 1 : 0,
                _ => (int)state.Reverser
            };
            transform.localRotation = restRotation * Quaternion.AngleAxis(fraction * travelDegrees, axis);
        }
    }
}
