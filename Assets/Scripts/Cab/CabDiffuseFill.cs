using UnityEngine;
using UnityEngine.Rendering;

namespace RailwayRenaissance.Cab
{
    // Smooth diffuse irradiance approximation; not baked or ray-traced GI.
    public sealed class CabDiffuseFill : MonoBehaviour
    {
        [SerializeField] Renderer[] receivers;
        CabInteriorLighting lighting;
        MaterialPropertyBlock block;
        readonly SphericalHarmonicsL2[] sample = new SphericalHarmonicsL2[1];
        int previous = int.MinValue;
        LightProbeUsage[] originalUsage;
        public void Configure(Renderer[] surfaces) { receivers = surfaces; }
        void OnEnable() { previous = int.MinValue; }
        void LateUpdate() { Refresh(); }
        static int Q(Color c) => Mathf.RoundToInt(c.r * 32f) * 1089 + Mathf.RoundToInt(c.g * 32f) * 33 + Mathf.RoundToInt(c.b * 32f);   // quantised so a drifting time of day does not rebuild every frame
        public void Refresh()
        {
            if (!isActiveAndEnabled || receivers == null) return;
            lighting ??= GetComponent<CabInteriorLighting>();
            if (lighting == null) return;
            int signature = Q(RenderSettings.ambientSkyColor) ^ (Q(RenderSettings.ambientEquatorColor) * 7);
            var env = CabDaylightEnvironment.Current;
            var skylight = env != null ? env.InteriorSkylight : Color.black;
            signature ^= Q(skylight) * 13;
            unchecked { foreach (var f in lighting.Fixtures) signature = signature * 31 + (f.light.isActiveAndEnabled ? f.light.intensity.GetHashCode() : 0); }
            if (signature == previous) return;
            previous = signature; block ??= new MaterialPropertyBlock();
            if(originalUsage==null) { originalUsage=new LightProbeUsage[receivers.Length]; for(int i=0;i<receivers.Length;i++) if(receivers[i]!=null)originalUsage[i]=receivers[i].lightProbeUsage; }
            foreach (var surface in receivers)
            {
                if (surface == null) continue;
                var sh = new SphericalHarmonicsL2();
                sh.AddAmbientLight(RenderSettings.ambientEquatorColor);
                sh.AddDirectionalLight(Vector3.up, RenderSettings.ambientSkyColor, .35f);
                // Skylight through the windows: soft from both sides plus a little from above, so ceilings, seat backs and walls read as
                // daylit rather than dark. Window position in the vehicle frame decides nothing here: the saloon is long and evenly glazed.
                sh.AddAmbientLight(skylight * .45f);
                sh.AddDirectionalLight(Vector3.right, skylight, .28f);
                sh.AddDirectionalLight(Vector3.left, skylight, .28f);
                sh.AddDirectionalLight(Vector3.down, skylight * .6f, .22f);   // floor bounce lifts ceilings and undersides
                // Receiver and lamp positions must share the vehicle frame: the
                // rail-motion review can translate and rotate the complete car.
                var center = transform.InverseTransformPoint(surface.transform.TransformPoint(surface.localBounds.center));
                foreach (var f in lighting.Fixtures)
                {
                    if (f.bounce == null || !f.light.isActiveAndEnabled) continue;
                    bool passenger = f.circuit == InteriorLightCircuit.PassengerFront || f.circuit == InteriorLightCircuit.PassengerRear;
                    // Partition separates cab/passenger energy; no inverse-square point hot spots.
                    var fixturePosition = transform.InverseTransformPoint(f.light.transform.position);
                    if ((center.z > 2.05f) != passenger) continue;
                    float dz = center.z - fixturePosition.z;
                    float weight = Mathf.Exp(-dz * dz / (passenger ? 5.0f : 1.8f));
                    float energy = weight * (passenger ? 1f : .7f);
                    sh.AddAmbientLight(f.light.color * energy * .50f);
                    sh.AddDirectionalLight(Vector3.up, f.light.color, energy * .14f);
                    sh.AddDirectionalLight(Vector3.down, new Color(1,.95f,.86f), energy * .12f);
                }
                sample[0] = sh;
                surface.lightProbeUsage = LightProbeUsage.CustomProvided;
                surface.GetPropertyBlock(block); block.CopySHCoefficientArraysFrom(sample); surface.SetPropertyBlock(block);
            }
        }
        void OnDisable()
        {
            if (receivers == null) return;
            for(int i=0;i<receivers.Length;i++)if(receivers[i]!=null&&originalUsage!=null)receivers[i].lightProbeUsage=originalUsage[i];
        }
    }
}
