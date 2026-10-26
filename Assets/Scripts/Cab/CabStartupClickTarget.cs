using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public enum CabStartupAction { Key, FuelPump, Start, Stop }
    public sealed class CabStartupClickTarget : MonoBehaviour
    {
        [SerializeField] CabStartupControls controls;
        [SerializeField] CabStartupAction action;
        public CabStartupAction Action => action;
        public CabStartupControls Controls => controls;
        public void Configure(CabStartupControls owner, CabStartupAction kind) { controls = owner; action = kind; }
        public bool Activate(bool secondary = false)
        {
            if (controls == null) return false;
            switch (action)
            {
                case CabStartupAction.Key:
                    return secondary ? controls.RemoveKey() : !controls.KeyInserted ? controls.InsertKey() : controls.SetMaster(!controls.MasterEnabled);
                case CabStartupAction.FuelPump: return !secondary && controls.SetFuelPump(!controls.FuelPumpEnabled);
                case CabStartupAction.Start: return !secondary && controls.TryStartEngine();
                case CabStartupAction.Stop:
                    if (secondary) return false;
                    controls.StopEngine(); return true;
                default: return false;
            }
        }
    }
}
