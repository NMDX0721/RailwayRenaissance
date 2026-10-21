using UnityEngine;

namespace RailwayRenaissance.Cab
{
    // Capture the actual cabin, rather than reflecting the review studio everywhere.
    public sealed class CabInteriorReflections : MonoBehaviour
    {
        [SerializeField] ReflectionProbe[] probes;
        Light[] lamps;
        int previousSignature = int.MinValue;
        public void Configure(ReflectionProbe[] bindings) { probes = bindings; }
        void OnEnable() { previousSignature = int.MinValue; }
        void LateUpdate() { RefreshIfChanged(); }
        public bool RefreshIfChanged()
        {
            GetComponent<CabDiffuseFill>()?.Refresh();
            lamps ??= GetComponentsInChildren<Light>(true);
            int signature = 17;
            unchecked
            {
                foreach (var lamp in lamps)
                {
                    signature = signature * 31 + (lamp.isActiveAndEnabled ? 1 : 0);
                    signature = signature * 31 + lamp.intensity.GetHashCode();
                    signature = signature * 31 + lamp.color.GetHashCode();
                }
                signature = signature * 31 + RenderSettings.ambientSkyColor.GetHashCode();
                signature = signature * 31 + RenderSettings.ambientEquatorColor.GetHashCode();
            }
            if (signature == previousSignature || probes == null) return false;
            previousSignature = signature;
            foreach (var probe in probes) if (probe != null && probe.isActiveAndEnabled) probe.RenderProbe();
            return true;
        }
    }
}
