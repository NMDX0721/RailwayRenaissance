using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RailwayRenaissance.Cab
{
    // Play-session anti-aliasing for the ATO line scenes. The track is thin geometry repeating to the horizon (rails, sleepers, fasteners),
    // which TAA smears and shakes while the camera rides the body, so use hardware MSAA instead. MSAA is a property of the shared URP asset,
    // so the original value is put back when this component goes away (play mode exit, scene change, quit).
    public sealed class AtoRenderQuality : MonoBehaviour
    {
        /// <summary>Switch the project's screen-space ambient occlusion feature (added disabled by AtoSsaoSetup) on or off.</summary>
        public static bool SetAmbientOcclusion(bool on)
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null) return false;
            var field = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = field?.GetValue(urp) as ScriptableRendererData[];
            if (list == null) return false;
            bool found = false;
            foreach (var data in list)
                if (data != null)
                    foreach (var feature in data.rendererFeatures)
                        if (feature is ScreenSpaceAmbientOcclusion) { feature.SetActive(on); found = true; }
            return found;
        }
        public static bool AmbientOcclusionActive
        {
            get
            {
                var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                var field = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var list = urp != null ? field?.GetValue(urp) as ScriptableRendererData[] : null;
                if (list != null) foreach (var data in list) if (data != null) foreach (var f in data.rendererFeatures) if (f is ScreenSpaceAmbientOcclusion) return f.isActive;
                return false;
            }
        }
        bool originalAo;

        [SerializeField] int msaaSamples = 8;
        UniversalRenderPipelineAsset asset;
        int originalSamples;
        UniversalAdditionalCameraData cameraData;
        AntialiasingMode originalMode;
        [SerializeField] float hazeDensity = 0.0018f;
        [SerializeField] float renderScale = 1.5f;     // supersampling: sharper rails and cab detail than any post filter
        int origCascades; float origShadowDistance, origScale;
        bool applied;
        AnisotropicFiltering originalAniso;
        bool originalFog; FogMode originalFogMode; float originalFogDensity; Color originalFogColor;

        void OnEnable()
        {
            asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (asset != null)
            {
                originalSamples = asset.msaaSampleCount;
                asset.msaaSampleCount = msaaSamples;
                origCascades = asset.shadowCascadeCount; origShadowDistance = asset.shadowDistance; origScale = asset.renderScale;
                asset.shadowCascadeCount = 4; asset.shadowDistance = 120f;   // the stock 1 cascade / 50 m smears cab shadows
                asset.renderScale = renderScale;
            }
            var cam = Camera.main;
            if (cam != null)
            {
                cam.allowMSAA = true;
                cameraData = cam.GetUniversalAdditionalCameraData();
                originalMode = cameraData.antialiasing;
                cameraData.antialiasing = AntialiasingMode.None;
            }
            // Thin repeating detail (sleeper stripes, bed grain) shimmers most far away: filter textures anisotropically and let
            // distance haze take the contrast out of it before it can alias. Both are put back in Restore.
            originalAniso = QualitySettings.anisotropicFiltering;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            originalFog = RenderSettings.fog; originalFogMode = RenderSettings.fogMode;
            originalFogDensity = RenderSettings.fogDensity; originalFogColor = RenderSettings.fogColor;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = hazeDensity;
            var env = FindAnyObjectByType<CabDaylightEnvironment>();
            RenderSettings.fogColor = env != null ? env.HorizonColor : new Color(.72f, .75f, .77f);   // haze follows the sky horizon; the time-of-day system keeps it in step
            originalAo = AmbientOcclusionActive; SetAmbientOcclusion(true);   // contact shadows under seats, in corners and along the ceiling
            applied = true;
        }

        void OnDisable() => Restore();
        void OnDestroy() => Restore();
        void OnApplicationQuit() => Restore();

        void Restore()
        {
            if (!applied) return;
            applied = false;
            if (asset != null)
            {
                asset.msaaSampleCount = originalSamples;
                asset.shadowCascadeCount = origCascades; asset.shadowDistance = origShadowDistance; asset.renderScale = origScale;
            }
            if (cameraData != null) cameraData.antialiasing = originalMode;
            SetAmbientOcclusion(originalAo);
            QualitySettings.anisotropicFiltering = originalAniso;
            RenderSettings.fog = originalFog; RenderSettings.fogMode = originalFogMode;
            RenderSettings.fogDensity = originalFogDensity; RenderSettings.fogColor = originalFogColor;
        }
    }
}
