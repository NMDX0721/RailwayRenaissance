using UnityEngine;
using UnityEngine.Rendering;

namespace RailwayRenaissance.Cab
{
    // Scene time-of-day. One clock (hour 0-24) drives the sun and moon arcs, sun/moon light, sky, ambient, horizon haze and, optionally,
    // the cab/passenger/exterior lamps. The old presets are shortcuts to a time of day (and a cloud amount); with auto-advance on
    // the day cycles by itself. Everything is lighting state only: nothing here touches the simulation.
    [ExecuteAlways]
    public sealed class CabDaylightEnvironment : MonoBehaviour
    {
        public enum DayPreset { Clear, Overcast, LowSun, Dusk, Night }
        [SerializeField] Light sun;
        [SerializeField] Camera reviewCamera;
        [SerializeField] Material sky;
        [SerializeField] DayPreset preset;
        [SerializeField, Range(0, 24)] float hour = 12f;
        [SerializeField, Range(0, 1)] float cloud;
        [SerializeField] bool autoAdvance;
        [SerializeField, Tooltip("Real minutes for a full 24 h cycle")] float dayLengthMinutes = 12f;
        [SerializeField, Tooltip("Switch cab, passenger and head lamps on by themselves when it gets dark")] bool autoLamps = true;
        Font font;
        Material sourceSky; bool instancedSky;
        Light moon; GameObject moonDisc; Material moonMaterial;
        bool lampsAutoOn; float lastAutoCheck;

        public float Hour => hour;
        public float SunElevation { get; private set; }
        public float NightFactor => 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-8f, 6f, SunElevation));
        public static CabDaylightEnvironment Current { get; private set; }
        /// <summary>Diffuse skylight coming in through the windows, for interiors that fake global illumination (CabDiffuseFill).</summary>
        public Color InteriorSkylight { get; private set; }
        public Color HorizonColor { get; private set; } = new Color(.72f, .75f, .77f);

        /// <summary>Batch reviews call this so sky tweaks go to a throw-away copy, never the shared sky asset.</summary>
        public void UseSkyInstance() { instancedSky = false; sourceSky = sky; if (sky != null) { sky = new Material(sky); instancedSky = true; RenderSettings.skybox = sky; } }
        public void Configure(Light source, Camera camera, Material skyMaterial) { sun = source; reviewCamera = camera; sky = skyMaterial; Apply(DayPreset.Clear); }
        void OnEnable() { Current = this; if (sun != null) ApplyTime(hour); }
        void OnDisable() { if (Current == this) Current = null; DestroyMoon(); }

        public void SetHour(float value) { hour = Mathf.Repeat(value, 24f); ApplyTime(hour); }
        public void SetAutoAdvance(bool on) { autoAdvance = on; }

        public void Apply(DayPreset value)
        {
            preset = value;
            switch (value)
            {
                case DayPreset.Overcast: cloud = 1f; hour = 12f; break;
                case DayPreset.LowSun: cloud = 0f; hour = 16.9f; break;
                case DayPreset.Dusk: cloud = 0f; hour = 18.35f; break;
                case DayPreset.Night: cloud = 0f; hour = 23.5f; break;
                default: cloud = 0f; hour = 12f; break;
            }
            ApplyTime(hour);
            if (reviewCamera != null) reviewCamera.clearFlags = CameraClearFlags.Skybox;
            Object.FindAnyObjectByType<CabDiffuseFill>()?.Refresh();
            Object.FindAnyObjectByType<CabInteriorReflections>()?.RefreshIfChanged();
        }

        void Update()
        {
            if (sun == null) return;
            if (autoAdvance && Application.isPlaying)
            {
                hour = Mathf.Repeat(hour + Time.deltaTime * 24f / Mathf.Max(1f, dayLengthMinutes * 60f), 24f);
                ApplyTime(hour);
            }
            if (autoLamps && Application.isPlaying && Time.unscaledTime - lastAutoCheck > .5f) { lastAutoCheck = Time.unscaledTime; AutoLamps(); }
        }

        void LateUpdate() { if (moonDisc != null && Camera.main != null) PlaceMoon(Camera.main.transform.position); }

        // ---- the clock ----
        static float Key(float x, float[] xs, float[] ys)
        {
            if (x <= xs[0]) return ys[0];
            for (int i = 1; i < xs.Length; i++)
                if (x <= xs[i]) return Mathf.Lerp(ys[i - 1], ys[i], Mathf.InverseLerp(xs[i - 1], xs[i], x));
            return ys[ys.Length - 1];
        }
        static Color Key(float x, float[] xs, Color[] cs)
        {
            if (x <= xs[0]) return cs[0];
            for (int i = 1; i < xs.Length; i++)
                if (x <= xs[i]) return Color.Lerp(cs[i - 1], cs[i], Mathf.InverseLerp(xs[i - 1], xs[i], x));
            return cs[cs.Length - 1];
        }
        static readonly float[] E = { -18f, -6f, 0f, 6f, 15f, 35f };
        static readonly Color[] AmbSky = { new Color(.07f,.09f,.17f), new Color(.10f,.10f,.20f), new Color(.30f,.24f,.32f), new Color(.40f,.40f,.48f), new Color(.38f,.44f,.54f), new Color(.36f,.44f,.54f) };
        static readonly Color[] AmbEq = { new Color(.05f,.06f,.11f), new Color(.09f,.07f,.12f), new Color(.30f,.18f,.13f), new Color(.27f,.22f,.18f), new Color(.22f,.24f,.26f), new Color(.20f,.24f,.27f) };
        static readonly Color[] AmbGround = { new Color(.015f,.017f,.025f), new Color(.02f,.02f,.03f), new Color(.06f,.05f,.045f), new Color(.075f,.07f,.06f), new Color(.075f,.085f,.065f), new Color(.075f,.085f,.065f) };
        static readonly Color[] Horizon = { new Color(.03f,.04f,.08f), new Color(.08f,.07f,.13f), new Color(.55f,.38f,.30f), new Color(.70f,.62f,.52f), new Color(.72f,.75f,.77f), new Color(.72f,.75f,.77f) };
        static readonly Color[] SkyTintKeys = { new Color(.18f,.20f,.30f), new Color(.25f,.26f,.38f), new Color(.58f,.46f,.44f), new Color(.55f,.48f,.46f), new Color(.5f,.5f,.5f), new Color(.5f,.5f,.5f) };
        static readonly float[] SunPower = { 0f, 0f, .5f, 1.0f, 1.25f, 1.4f };
        static readonly Color[] SunColor = { new Color(1,.4f,.2f), new Color(1,.4f,.2f), new Color(1,.45f,.2f), new Color(1,.72f,.45f), new Color(1,.88f,.72f), new Color(1,.97f,.91f) };
        static readonly float[] Exposure = { .10f, .16f, .7f, .95f, 1f, 1f };
        static readonly float[] Thickness = { 1f, 1.2f, 1.7f, 1.4f, 1.1f, 1f };

        public void ApplyTime(float h)
        {
            if (sun == null) return;
            hour = Mathf.Repeat(h, 24f);
            // Sunrise ~06:00, sunset ~18:30, peak ~58 deg. Azimuth sweeps through the cab's lit side during the day.
            float elev = 58f * Mathf.Cos(2f * Mathf.PI * (hour - 12.25f) / 24f) + 4f;
            float yaw = -38f + (hour - 12f) * 10f;
            SunElevation = elev;
            float overcast = cloud;

            sun.enabled = true; sun.type = LightType.Directional;
            sun.intensity = Key(elev, E, SunPower) * Mathf.Lerp(1f, .2f, overcast);
            sun.color = Key(elev, E, SunColor);
            sun.transform.rotation = Quaternion.Euler(Mathf.Max(elev, 0f), yaw, 0);   // below the horizon the sun light intensity is already 0
            sun.shadows = LightShadows.Soft; sun.shadowStrength = Mathf.Lerp(.85f, .4f, overcast) * Mathf.Clamp01(Mathf.InverseLerp(-1f, 6f, elev));
            RenderSettings.sun = sun; RenderSettings.skybox = sky;

            // Moon: opposite the sun, soft cold light with no shadow, strong enough that the track and the cab stay readable at night.
            EnsureMoon();
            float moonElev = Mathf.Clamp(-elev * .9f + 8f, -10f, 65f);
            float moonUp = Mathf.InverseLerp(-6f, 12f, moonElev) * NightFactor;
            moon.transform.rotation = Quaternion.Euler(Mathf.Max(moonElev, 2f), yaw + 180f, 0);
            moon.intensity = .32f * moonUp * Mathf.Lerp(1f, .35f, overcast);
            moon.color = new Color(.55f, .66f, 1f);
            moon.enabled = moon.intensity > .001f;
            if (moonDisc != null)
            {
                moonDisc.SetActive(moonUp > .02f && overcast < .8f);
                moonDir = Quaternion.Euler(moonElev, yaw + 180f, 0) * Vector3.back;
                if (Camera.main != null) PlaceMoon(Camera.main.transform.position);   // the light points down, so the moon sits behind its forward vector
                if (moonMaterial != null) { var c = new Color(.9f, .93f, 1f) * (2f * moonUp); moonMaterial.SetColor("_BaseColor", c); moonMaterial.SetColor("_Color", c); }
            }

            OpenGlassToSunlight();
            float dayLevel = 1f - NightFactor;
            ScaleInteriorKeyLights(Mathf.Lerp(.07f, 1f, dayLevel) * Mathf.Lerp(1f, .7f, overcast));
            // Skylight through the windscreen onto the desk and panels (interior layer only): it is what makes the cab readable by day
            // with every lamp off. It follows the sun's colour, softens under cloud and fades to a trace of moonlight at night.
            InteriorSkylight = Color.Lerp(RenderSettings.ambientSkyColor, sun.color, .25f) * (dayLevel * dayLevel * Mathf.Lerp(1f, .6f, overcast));
            EnsureWindowLight();
            windowLight.color = Color.Lerp(Key(elev, E, SunColor), new Color(.75f, .82f, 1f), .45f + .3f * overcast);
            windowLight.intensity = Mathf.Lerp(.025f, 1.5f, dayLevel) * Mathf.Lerp(1f, .7f, overcast);

            var skyAmb = Key(elev, E, AmbSky); var eqAmb = Key(elev, E, AmbEq);
            var grey = new Color(.42f, .44f, .46f) * Mathf.Clamp(Key(elev, E, Exposure), .15f, 1f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(skyAmb, Color.Lerp(skyAmb, grey, .7f), overcast);
            RenderSettings.ambientEquatorColor = Color.Lerp(eqAmb, Color.Lerp(eqAmb, grey * .6f, .7f), overcast);
            RenderSettings.ambientGroundColor = Key(elev, E, AmbGround);

            HorizonColor = Color.Lerp(Key(elev, E, Horizon), new Color(.62f, .64f, .66f) * Mathf.Clamp(Key(elev, E, Exposure), .1f, 1f), overcast * .8f);
            if (RenderSettings.fog) RenderSettings.fogColor = HorizonColor;

            if (sky != null && sky.HasProperty("_Exposure") && (Application.isPlaying || instancedSky))
            {
                if (!instancedSky) { sourceSky = sky; sky = new Material(sky) { name = sky.name + "(runtime)" }; instancedSky = true; RenderSettings.skybox = sky; }
                sky.SetFloat("_Exposure", Key(elev, E, Exposure) * Mathf.Lerp(1f, .8f, overcast));
                sky.SetFloat("_AtmosphereThickness", Key(elev, E, Thickness));
                sky.SetColor("_SkyTint", Color.Lerp(Key(elev, E, SkyTintKeys), new Color(.5f, .5f, .5f), overcast * .6f));
            }
            // In play mode the fill and probes refresh themselves every frame; in edit mode (batch reviews) nothing would.
            if (!Application.isPlaying)
            {
                FindAnyObjectByType<CabDiffuseFill>()?.Refresh();
                FindAnyObjectByType<CabInteriorReflections>()?.RefreshIfChanged();
            }
        }

        // The cab/saloon review rig has its own directional "key" and "fill" on the interior layer; they stand in for daylight coming
        // through the windows, so they have to fade with the sun or the cab stays noon-bright at night.
        struct InteriorKey { public Light light; public float baseIntensity; }
        InteriorKey[] interiorKeys;
        void ScaleInteriorKeyLights(float factor)
        {
            if (interiorKeys == null)
            {
                var list = new System.Collections.Generic.List<InteriorKey>();
                foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Include))
                    if (l != sun && l != moon && l.type == LightType.Directional && (l.cullingMask & (1 << 30)) != 0 && (l.cullingMask & 1) == 0)
                        list.Add(new InteriorKey { light = l, baseIntensity = l.intensity });
                interiorKeys = list.ToArray();
            }
            foreach (var k in interiorKeys) if (k.light != null) k.light.intensity = k.baseIntensity * factor;
        }

        // The saloon and cab windows are thin glass quads (alpha ~0.08) that still cast full shadows, so no sunlight ever reaches the seats or
        // the floor and the interior looks evenly grey. Glass should not stop the sun: with this off the sun draws window patches that
        // slide across the seats as the day moves.
        bool glassOpened;
        void OpenGlassToSunlight()
        {
            if (glassOpened) return;
            glassOpened = true;
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                var m = r.sharedMaterial;
                if (m != null && m.name.Contains("Glass") && r.shadowCastingMode != ShadowCastingMode.Off) r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        Light windowLight;
        void EnsureWindowLight()
        {
            if (windowLight != null) return;
            var go = new GameObject("Cab_Window_Light") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            go.transform.rotation = Quaternion.Euler(38f, 8f, 0f);   // from the nose end, over the screen, down onto the desk
            windowLight = go.AddComponent<Light>(); windowLight.type = LightType.Directional; windowLight.shadows = LightShadows.None;
            windowLight.cullingMask = 1 << 30;
        }

        // ---- moon ----
        Vector3 moonDir = Vector3.up;
        void EnsureMoon()
        {
            if (moon == null)
            {
                var go = new GameObject("Moon_Light") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                moon = go.AddComponent<Light>(); moon.type = LightType.Directional; moon.shadows = LightShadows.None;
            }
            if (moonDisc == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) return;
                moonDisc = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                moonDisc.name = "Moon_Disc"; moonDisc.hideFlags = HideFlags.DontSave;
                Object.DestroyImmediate(moonDisc.GetComponent<Collider>());
                moonMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
                var r = moonDisc.GetComponent<Renderer>();
                r.sharedMaterial = moonMaterial; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                moonDisc.transform.localScale = Vector3.one * 150f;   // ~1.7 deg at 2500 m: larger than the real moon so it reads on screen
                moonDisc.SetActive(false);
            }
        }
        void PlaceMoon(Vector3 cameraPosition) { moonDisc.transform.position = cameraPosition + moonDir * 2500f; }
        void DestroyMoon()
        {
            if (Application.isPlaying)
            {
                if (windowLight != null) Destroy(windowLight.gameObject);
                if (moonDisc != null) Destroy(moonDisc);
                if (moon != null) Destroy(moon.gameObject);
                if (moonMaterial != null) Destroy(moonMaterial);
            }
            else
            {
                if (windowLight != null) DestroyImmediate(windowLight.gameObject);
                if (moonDisc != null) DestroyImmediate(moonDisc);
                if (moon != null) DestroyImmediate(moon.gameObject);
                if (moonMaterial != null) DestroyImmediate(moonMaterial);
            }
            moonDisc = null; moon = null; moonMaterial = null; windowLight = null;
        }

        // ---- automatic lamps: dark enough -> cab/passenger/head lamps come on; bright again -> only the ones we switched on go off ----
        void AutoLamps()
        {
            var lamps = Object.FindAnyObjectByType<CabInteriorLighting>();
            var outside = Object.FindAnyObjectByType<CabExteriorLighting>();
            bool dark = SunElevation < 3f;
            if (dark && !lampsAutoOn)
            {
                lampsAutoOn = true;
                if (lamps != null && !lamps.CabCeilingOn && !lamps.TaskOn) lamps.SetCabPreset(1);
                if (lamps != null) { lamps.SetPassengerZone(0, true); lamps.SetPassengerZone(1, true); }
                if (outside != null && outside.Mode == ExteriorLightMode.Off) outside.SetMode(ExteriorLightMode.Dim);
            }
            else if (!dark && SunElevation > 8f && lampsAutoOn)
            {
                lampsAutoOn = false;
                if (lamps != null) { lamps.SetPassengerZone(0, false); lamps.SetPassengerZone(1, false); }
                if (outside != null && outside.Mode == ExteriorLightMode.Dim) outside.SetMode(ExteriorLightMode.Off);
            }
        }

        [ContextMenu("Daylight/Clear")] void Clear() { Apply(DayPreset.Clear); }
        [ContextMenu("Daylight/Overcast")] void Overcast() { Apply(DayPreset.Overcast); }
        [ContextMenu("Daylight/Low sun")] void LowSun() { Apply(DayPreset.LowSun); }
        [ContextMenu("Daylight/Dusk")] void Dusk() { Apply(DayPreset.Dusk); }
        [ContextMenu("Daylight/Night")] void Night() { Apply(DayPreset.Night); }
        public void SetView(int view)
        {
            reviewCamera.orthographic=false;
            if(view==0){reviewCamera.transform.position=new Vector3(.65f,1.38f,.78f);reviewCamera.transform.LookAt(new Vector3(.60f,1.22f,-.8f));reviewCamera.fieldOfView=65;}
            else if(view==1){reviewCamera.transform.position=new Vector3(0,1.58f,2.18f);reviewCamera.transform.LookAt(new Vector3(0,1.28f,11));reviewCamera.fieldOfView=60;}
            else {reviewCamera.transform.position=new Vector3(11,6,-9);reviewCamera.transform.LookAt(new Vector3(0,.65f,7));reviewCamera.fieldOfView=55;}
        }
        [ContextMenu("View/Passenger")] void PassengerView() { SetView(1); }
        [ContextMenu("View/Driver")] void DriverView() { SetView(0); }
        [ContextMenu("View/Exterior")] void ExteriorView() { SetView(2); }
        void OnGUI()
        {
            if(!Application.isPlaying)return;
            if(font==null)font=Resources.Load<Font>("Fonts/SarasaMonoSC-Regular");
            var old=GUI.skin.font;if(font!=null)GUI.skin.font=font;
            GUILayout.BeginArea(new Rect(Mathf.Max(0,(Screen.width-560)/2),Screen.height-150,560,140),GUI.skin.box);
            GUILayout.Label($"时间 {(int)hour:00}:{(int)((hour%1f)*60f):00} · 太阳高度 {SunElevation:0}° · 车内灯可独立开关");
            float h2=GUILayout.HorizontalSlider(hour,0,24); if(Mathf.Abs(h2-hour)>.001f)SetHour(h2);
            GUILayout.BeginHorizontal();
            bool auto=GUILayout.Toggle(autoAdvance,"自动流逝"); if(auto!=autoAdvance)autoAdvance=auto;
            bool lampsOn=GUILayout.Toggle(autoLamps,"自动开灯"); if(lampsOn!=autoLamps)autoLamps=lampsOn;
            GUILayout.Label("云量"); float c2=GUILayout.HorizontalSlider(cloud,0,1,GUILayout.Width(90)); if(Mathf.Abs(c2-cloud)>.001f){cloud=c2;ApplyTime(hour);}
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("晴天"))Apply(DayPreset.Clear);
            if(GUILayout.Button("阴天"))Apply(DayPreset.Overcast);
            if(GUILayout.Button("低角度日光"))Apply(DayPreset.LowSun);
            if(GUILayout.Button("夕阳"))Apply(DayPreset.Dusk);
            if(GUILayout.Button("夜晚"))Apply(DayPreset.Night);
            GUILayout.EndHorizontal();GUILayout.BeginHorizontal();
            if(GUILayout.Button("驾驶位"))SetView(0);
            if(GUILayout.Button("客舱"))SetView(1);
            if(GUILayout.Button("车外"))SetView(2);
            GUILayout.EndHorizontal();GUILayout.EndArea();GUI.skin.font=old;
        }
    }
}
