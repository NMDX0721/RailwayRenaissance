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
        static int Quantise(Color c) => Mathf.RoundToInt(c.r * 24f) * 961 + Mathf.RoundToInt(c.g * 24f) * 31 + Mathf.RoundToInt(c.b * 24f);
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
                    signature = signature * 31 + Mathf.RoundToInt(lamp.intensity * 10f);
                    signature = signature * 31 + lamp.color.GetHashCode();
                }
                // Quantised: the time-of-day system moves these every frame, and a probe re-render per frame would be far too costly.
                signature = signature * 31 + Quantise(RenderSettings.ambientSkyColor);
                signature = signature * 31 + Quantise(RenderSettings.ambientEquatorColor);
                signature = signature * 31 + Quantise(RenderSettings.ambientGroundColor);
                if(RenderSettings.sun!=null)
                {
                    signature=signature*31+Mathf.RoundToInt(RenderSettings.sun.intensity*8f);
                    signature=signature*31+Quantise(RenderSettings.sun.color);
                    var e=RenderSettings.sun.transform.eulerAngles;
                    signature=signature*31+Mathf.RoundToInt(e.x/3f);signature=signature*31+Mathf.RoundToInt(e.y/3f);
                }
            }
            if (signature == previousSignature || probes == null) return false;
            previousSignature = signature;
            foreach (var probe in probes) if (probe != null && probe.isActiveAndEnabled) probe.RenderProbe();
            return true;
        }
    }
}
