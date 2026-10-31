using System.Collections.Generic;
using UnityEngine;

namespace RailwayRenaissance.Cab
{
    /// <summary>
    /// 3D 窗外程序化像素风场景与摄像机控制器
    /// 负责生成 3D 几何、RenderTexture 渲染输出、对象池循环推进与 9 项动态反馈
    /// </summary>
    public class Cab3DSceneController : MonoBehaviour
    {
        [Header("渲染输出")]
        public int renderWidth = 640;
        public int renderHeight = 360;
        public RenderTexture TargetTexture { get; private set; }

        [Header("组件引用")]
        public Camera CabCamera { get; private set; }
        public Light DirectionalSunLight { get; private set; }
        public Light HeadlightSpot { get; private set; }

        // 场景图层根节点
        private GameObject m_SceneRoot;
        private GameObject m_TrackGroup;
        private GameObject m_PoleGroup;
        private GameObject m_SceneryGroup;
        private GameObject m_MountainGroup;
        private GameObject m_StationGroup;
        private GameObject m_SignalGroup;
        private GameObject m_RainGroup;

        // 对象池
        private readonly List<Transform> m_Sleepers = new List<Transform>();
        private readonly List<Transform> m_Poles = new List<Transform>();
        private readonly List<Transform> m_SceneryObjects = new List<Transform>();
        private MeshRenderer m_SignalRedLight;
        private MeshRenderer m_SignalGreenLight;

        // 摄像机基准与阻尼运动变量
        private Vector3 m_BaseCamPos = new Vector3(0.55f, 2.45f, 0f); // 司机眼位（偏右）
        private float m_CurrentPitch = 0f;
        private float m_PitchVelocity = 0f;
        private float m_BumperTimer = 0f;

        // 昼夜与环境
        public float DayPhase = 0.25f; // 0.0=黎明, 0.25=正午, 0.65=黄昏, 0.85=深夜
        public bool IsRaining = false;
        public bool WipersOn { get; private set; }
        public event System.Action<bool> WiperStateChanged;
        public bool LayerVisibleTrack = true;
        public bool LayerVisiblePoles = true;
        public bool LayerVisibleScenery = true;
        public bool LayerVisibleMountains = true;
        public bool LayerVisibleStation = true;
        public bool LayerVisibleSignals = true;

        private const float SLEEPER_SPACING = 0.65f;
        private const int SLEEPER_COUNT = 65;
        private const float TRACK_VISIBLE_LENGTH = SLEEPER_COUNT * SLEEPER_SPACING;

        private const float POLE_SPACING = 24f;
        private const int POLE_COUNT = 10;

        private float m_TrackScrollOffset = 0f;
        private float m_PoleScrollOffset = 0f;
        private float m_SceneryScrollOffset = 0f;

        private CabEngineState m_BoundState;

        public enum TimeOfDay
        {
            Morning,
            Noon,
            Dusk,
            Night
        }

        public static Cab3DSceneController Create(Transform parent = null)
        {
            var go = new GameObject("Cab3DSceneController");
            if (parent != null) go.transform.SetParent(parent, false);
            var ctrl = go.AddComponent<Cab3DSceneController>();
            ctrl.Initialize(640, 360);
            return ctrl;
        }

        public void BindEngineState(CabEngineState state)
        {
            m_BoundState = state;
        }

        private void Update()
        {
            if (m_BoundState != null)
            {
                UpdateFrame(m_BoundState, Time.deltaTime);
            }
        }

        public void TriggerHornShock()
        {
            if (m_BoundState != null) m_BoundState.TriggerHorn();
        }

        public void ToggleHeadlight()
        {
            if (m_BoundState != null)
            {
                m_BoundState.HeadlightOn = !m_BoundState.HeadlightOn;
            }
            else if (HeadlightSpot != null)
            {
                HeadlightSpot.enabled = !HeadlightSpot.enabled;
            }
        }

        public void ToggleWipers()
        {
            SetWipers(!WipersOn);
        }

        public bool SetWipers(bool enabled)
        {
            if (WipersOn == enabled) return false;
            WipersOn = enabled;
            WiperStateChanged?.Invoke(enabled);
            return true;
        }

        public void ForceWeather(bool rain)
        {
            IsRaining = rain;
            if (m_RainGroup != null) m_RainGroup.SetActive(IsRaining);
        }

        public void ForceTimeOfDay(TimeOfDay tod)
        {
            DayPhase = tod switch
            {
                TimeOfDay.Morning => 0.05f,
                TimeOfDay.Noon => 0.25f,
                TimeOfDay.Dusk => 0.65f,
                TimeOfDay.Night => 0.85f,
                _ => 0.25f
            };
        }

        public void ChangeResolution(int w, int h)
        {
            SetResolution(w, h);
        }

        public void Initialize(int width = 640, int height = 360)
        {
            renderWidth = width;
            renderHeight = height;

            RecreateRenderTexture();
            BuildSceneHierarchy();
            SetupLightingAndCamera();
            BuildTrackSystem();
            BuildPoles();
            BuildScenery();
            BuildMountains();
            BuildStation();
            BuildSignals();
            BuildRainFX();
        }

        public void SetResolution(int w, int h)
        {
            if (renderWidth == w && renderHeight == h && TargetTexture != null) return;
            renderWidth = w;
            renderHeight = h;
            RecreateRenderTexture();
            if (CabCamera != null) CabCamera.targetTexture = TargetTexture;
        }

        private void RecreateRenderTexture()
        {
            if (TargetTexture != null)
            {
                TargetTexture.Release();
                Destroy(TargetTexture);
            }

            TargetTexture = new RenderTexture(renderWidth, renderHeight, 16, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point, // 核心：Point 过滤产生纯正 3D 像素风
                antiAliasing = 1,              // 关抗锯齿，边缘无模糊过渡
                name = $"Cab_PixelRT_{renderWidth}x{renderHeight}"
            };
            TargetTexture.Create();
        }

        private void BuildSceneHierarchy()
        {
            if (m_SceneRoot != null) Destroy(m_SceneRoot);

            m_SceneRoot = new GameObject("Cab3D_World");
            m_SceneRoot.transform.position = new Vector3(0f, -500f, 0f); // 置于远离原点独立空域

            m_TrackGroup = new GameObject("TrackGroup");
            m_TrackGroup.transform.SetParent(m_SceneRoot.transform, false);

            m_PoleGroup = new GameObject("PoleGroup");
            m_PoleGroup.transform.SetParent(m_SceneRoot.transform, false);

            m_SceneryGroup = new GameObject("SceneryGroup");
            m_SceneryGroup.transform.SetParent(m_SceneRoot.transform, false);

            m_MountainGroup = new GameObject("MountainGroup");
            m_MountainGroup.transform.SetParent(m_SceneRoot.transform, false);

            m_StationGroup = new GameObject("StationGroup");
            m_StationGroup.transform.SetParent(m_SceneRoot.transform, false);

            m_SignalGroup = new GameObject("SignalGroup");
            m_SignalGroup.transform.SetParent(m_SceneRoot.transform, false);

            m_RainGroup = new GameObject("RainGroup");
            m_RainGroup.transform.SetParent(m_SceneRoot.transform, false);
        }

        private void SetupLightingAndCamera()
        {
            // 1. 摄像机
            GameObject camObj = new GameObject("CabCamera");
            camObj.transform.SetParent(m_SceneRoot.transform, false);
            camObj.transform.localPosition = m_BaseCamPos;

            CabCamera = camObj.AddComponent<Camera>();
            CabCamera.clearFlags = CameraClearFlags.SolidColor;
            CabCamera.backgroundColor = CabPalette.HexColor("#6A8CA8");
            CabCamera.fieldOfView = 58f;
            CabCamera.nearClipPlane = 0.2f;
            CabCamera.farClipPlane = 450f;
            CabCamera.targetTexture = TargetTexture;

            // 2. 太阳主方向光
            GameObject sunObj = new GameObject("Directional_Sun");
            sunObj.transform.SetParent(m_SceneRoot.transform, false);
            DirectionalSunLight = sunObj.AddComponent<Light>();
            DirectionalSunLight.type = LightType.Directional;
            DirectionalSunLight.shadows = LightShadows.None; // 像素画风格不要细碎阴影
            DirectionalSunLight.intensity = 1.0f;
            DirectionalSunLight.color = CabPalette.HexColor("#FFECC6");
            sunObj.transform.rotation = Quaternion.Euler(38f, -42f, 0f);

            // 3. 机车前照大灯 SpotLight
            GameObject headObj = new GameObject("Headlight_Spot");
            headObj.transform.SetParent(m_SceneRoot.transform, false);
            headObj.transform.localPosition = new Vector3(0f, 1.6f, 1.2f);
            headObj.transform.localRotation = Quaternion.Euler(7f, 0f, 0f);

            HeadlightSpot = headObj.AddComponent<Light>();
            HeadlightSpot.type = LightType.Spot;
            HeadlightSpot.spotAngle = 62f;
            HeadlightSpot.range = 80f;
            HeadlightSpot.intensity = 2.4f;
            HeadlightSpot.color = CabPalette.HexColor("#FFF7D9");
        }

        private void BuildTrackSystem()
        {
            Material ballastMat = CabPalette.GetMaterial(CabPalette.ColorBallast);
            Material railMat = CabPalette.GetMaterial(CabPalette.ColorSteelRail);
            Material sleeperMat = CabPalette.GetMaterial(CabPalette.ColorSleeper);

            // 道砟路基 (厚石盒)
            GameObject ballast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ballast.name = "Ballast_Bed";
            ballast.transform.SetParent(m_TrackGroup.transform, false);
            ballast.transform.localPosition = new Vector3(0f, -0.2f, TRACK_VISIBLE_LENGTH * 0.5f);
            ballast.transform.localScale = new Vector3(3.6f, 0.4f, TRACK_VISIBLE_LENGTH + 10f);
            ballast.GetComponent<MeshRenderer>().sharedMaterial = ballastMat;

            // 左右两条钢轨 (标准轨距 1.435m)
            float halfGauge = 0.717f;
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = $"Rail_{(i < 0 ? "L" : "R")}";
                rail.transform.SetParent(m_TrackGroup.transform, false);
                rail.transform.localPosition = new Vector3(i * halfGauge, 0.16f, TRACK_VISIBLE_LENGTH * 0.5f);
                rail.transform.localScale = new Vector3(0.09f, 0.14f, TRACK_VISIBLE_LENGTH + 10f);
                rail.GetComponent<MeshRenderer>().sharedMaterial = railMat;
            }

            // 枕木阵列池 (深棕木块)
            m_Sleepers.Clear();
            for (int i = 0; i < SLEEPER_COUNT; i++)
            {
                GameObject sleeper = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sleeper.name = $"Sleeper_{i}";
                sleeper.transform.SetParent(m_TrackGroup.transform, false);
                sleeper.transform.localPosition = new Vector3(0f, 0.04f, i * SLEEPER_SPACING);
                sleeper.transform.localScale = new Vector3(2.4f, 0.11f, 0.22f);
                sleeper.GetComponent<MeshRenderer>().sharedMaterial = sleeperMat;
                m_Sleepers.Add(sleeper.transform);
            }
        }

        private void BuildPoles()
        {
            Material poleMat = CabPalette.GetMaterial(CabPalette.ColorPoleWood);
            Material armMat = CabPalette.GetMaterial(CabPalette.ColorDarkBrown);

            m_Poles.Clear();
            for (int i = 0; i < POLE_COUNT; i++)
            {
                GameObject poleObj = new GameObject($"Pole_{i}");
                poleObj.transform.SetParent(m_PoleGroup.transform, false);
                poleObj.transform.localPosition = new Vector3(3.2f, 0f, i * POLE_SPACING);

                // 主立柱 (高 5.5m 圆柱)
                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.transform.SetParent(poleObj.transform, false);
                trunk.transform.localPosition = new Vector3(0f, 2.75f, 0f);
                trunk.transform.localScale = new Vector3(0.24f, 2.75f, 0.24f);
                trunk.GetComponent<MeshRenderer>().sharedMaterial = poleMat;

                // 横担支架
                GameObject crossArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crossArm.transform.SetParent(poleObj.transform, false);
                crossArm.transform.localPosition = new Vector3(-0.35f, 4.8f, 0f);
                crossArm.transform.localScale = new Vector3(1.1f, 0.12f, 0.12f);
                crossArm.GetComponent<MeshRenderer>().sharedMaterial = armMat;

                m_Poles.Add(poleObj.transform);
            }
        }

        private void BuildScenery()
        {
            Material foliageDark = CabPalette.GetMaterial(CabPalette.ColorForestGreen);
            Material foliageLight = CabPalette.GetMaterial(CabPalette.ColorFoliageLight);
            Material houseWall = CabPalette.GetMaterial(CabPalette.ColorStationWall);
            Material houseRoof = CabPalette.GetMaterial(CabPalette.ColorDarkRed);

            m_SceneryObjects.Clear();
            int count = 28;
            for (int i = 0; i < count; i++)
            {
                float z = i * 7.5f;
                float side = (i % 2 == 0) ? -1f : 1f;
                float x = side * Random.Range(6.5f, 24f);

                GameObject item;
                if (i % 4 == 0)
                {
                    // 乡村平房盒体 (墙+红瓦屋顶)
                    item = new GameObject($"House_{i}");
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.transform.SetParent(item.transform, false);
                    wall.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                    wall.transform.localScale = new Vector3(3.6f, 3f, 4f);
                    wall.GetComponent<MeshRenderer>().sharedMaterial = houseWall;

                    GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    roof.transform.SetParent(item.transform, false);
                    roof.transform.localPosition = new Vector3(0f, 3.5f, 0f);
                    roof.transform.localScale = new Vector3(4.2f, 1.1f, 4.4f);
                    roof.transform.localRotation = Quaternion.Euler(0f, 0f, 8f * side);
                    roof.GetComponent<MeshRenderer>().sharedMaterial = houseRoof;
                }
                else
                {
                    // 像素松树 (树干 + 绿冠)
                    item = new GameObject($"PineTree_{i}");
                    GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    trunk.transform.SetParent(item.transform, false);
                    trunk.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                    trunk.transform.localScale = new Vector3(0.35f, 1.2f, 0.35f);
                    trunk.GetComponent<MeshRenderer>().sharedMaterial = CabPalette.GetMaterial(CabPalette.ColorPoleWood);

                    GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    foliage.transform.SetParent(item.transform, false);
                    foliage.transform.localPosition = new Vector3(0f, 3.2f, 0f);
                    float s = Random.Range(2.2f, 3.8f);
                    foliage.transform.localScale = new Vector3(s, s * 1.5f, s);
                    foliage.GetComponent<MeshRenderer>().sharedMaterial = (i % 3 == 0) ? foliageLight : foliageDark;
                }

                item.transform.SetParent(m_SceneryGroup.transform, false);
                item.transform.localPosition = new Vector3(x, 0f, z);
                m_SceneryObjects.Add(item.transform);
            }
        }

        private void BuildMountains()
        {
            Material farMat = CabPalette.GetMaterial(CabPalette.ColorMountainFar);
            Material midMat = CabPalette.GetMaterial(CabPalette.ColorMountainMid);

            // 远景层叠三角山脊
            float[] dists = { 160f, 240f, 320f };
            for (int layer = 0; layer < dists.Length; layer++)
            {
                float z = dists[layer];
                Material mat = (layer == 0) ? midMat : farMat;

                for (int m = -4; m <= 4; m++)
                {
                    GameObject mnt = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    mnt.name = $"Mountain_L{layer}_{m}";
                    mnt.transform.SetParent(m_MountainGroup.transform, false);
                    float w = 55f + layer * 20f;
                    float h = 32f + (4 - Mathf.Abs(m)) * 6f - layer * 5f;
                    mnt.transform.localPosition = new Vector3(m * 38f, h * 0.45f - 4f, z);
                    mnt.transform.localScale = new Vector3(w, h, w * 0.8f);
                    mnt.transform.localRotation = Quaternion.Euler(0f, 25f * (m % 2), 45f);
                    mnt.GetComponent<MeshRenderer>().sharedMaterial = mat;
                }
            }
        }

        private void BuildStation()
        {
            Material wallMat = CabPalette.GetMaterial(CabPalette.ColorStationWall);
            Material roofMat = CabPalette.GetMaterial(CabPalette.ColorStationRoof);
            Material platformMat = CabPalette.GetMaterial(CabPalette.ColorBallast);

            // 站台主体 (长条混凝土台)
            GameObject plat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plat.name = "Station_Platform_Slab";
            plat.transform.SetParent(m_StationGroup.transform, false);
            plat.transform.localPosition = new Vector3(-2.8f, 0.45f, 0f);
            plat.transform.localScale = new Vector3(3.2f, 0.9f, 65f);
            plat.GetComponent<MeshRenderer>().sharedMaterial = platformMat;

            // 站房 (灰砖红陶瓦房)
            GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house.name = "Station_Building";
            house.transform.SetParent(m_StationGroup.transform, false);
            house.transform.localPosition = new Vector3(-6.2f, 2.5f, 0f);
            house.transform.localScale = new Vector3(5.5f, 4.5f, 18f);
            house.GetComponent<MeshRenderer>().sharedMaterial = wallMat;

            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Station_Roof";
            roof.transform.SetParent(m_StationGroup.transform, false);
            roof.transform.localPosition = new Vector3(-6.2f, 5.2f, 0f);
            roof.transform.localScale = new Vector3(6.5f, 1.4f, 20f);
            roof.GetComponent<MeshRenderer>().sharedMaterial = roofMat;

            // 站牌立柱与牌面
            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "Station_Nameplate";
            sign.transform.SetParent(m_StationGroup.transform, false);
            sign.transform.localPosition = new Vector3(-1.8f, 1.8f, -10f);
            sign.transform.localScale = new Vector3(0.12f, 1.1f, 2.4f);
            sign.GetComponent<MeshRenderer>().sharedMaterial = CabPalette.GetMaterial(CabPalette.ColorOffWhite);

            m_StationGroup.transform.localPosition = new Vector3(0f, 0f, 1000f); // 初始置于远方
        }

        private void BuildSignals()
        {
            Material postMat = CabPalette.GetMaterial(CabPalette.ColorPoleWood);

            // 信号机立柱
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Signal_Post";
            post.transform.SetParent(m_SignalGroup.transform, false);
            post.transform.localPosition = new Vector3(2.6f, 2.2f, 0f);
            post.transform.localScale = new Vector3(0.2f, 2.2f, 0.2f);
            post.GetComponent<MeshRenderer>().sharedMaterial = postMat;

            // 信号箱
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Signal_Box";
            box.transform.SetParent(m_SignalGroup.transform, false);
            box.transform.localPosition = new Vector3(2.6f, 3.8f, 0f);
            box.transform.localScale = new Vector3(0.5f, 1.2f, 0.35f);
            box.GetComponent<MeshRenderer>().sharedMaterial = CabPalette.GetMaterial(CabPalette.ColorDarkBrown);

            // 红灯 (下)
            GameObject redLight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            redLight.name = "Signal_Red";
            redLight.transform.SetParent(m_SignalGroup.transform, false);
            redLight.transform.localPosition = new Vector3(2.6f, 3.5f, -0.2f);
            redLight.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
            m_SignalRedLight = redLight.GetComponent<MeshRenderer>();
            m_SignalRedLight.sharedMaterial = CabPalette.GetMaterial(CabPalette.ColorSignalRed);

            // 绿灯 (上)
            GameObject greenLight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            greenLight.name = "Signal_Green";
            greenLight.transform.SetParent(m_SignalGroup.transform, false);
            greenLight.transform.localPosition = new Vector3(2.6f, 4.1f, -0.2f);
            greenLight.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
            m_SignalGreenLight = greenLight.GetComponent<MeshRenderer>();
            m_SignalGreenLight.sharedMaterial = CabPalette.GetMaterial(CabPalette.HexColor("#1A4D2B")); // 初始暗绿

            m_SignalGroup.transform.localPosition = new Vector3(0f, 0f, 85f);
        }

        private void BuildRainFX()
        {
            Material rainMat = CabPalette.GetMaterial(CabPalette.HexColor("#B0C4DE"));
            for (int i = 0; i < 35; i++)
            {
                GameObject rainDrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rainDrop.name = $"Rain_{i}";
                rainDrop.transform.SetParent(m_RainGroup.transform, false);
                rainDrop.transform.localPosition = new Vector3(Random.Range(-3.5f, 3.5f), Random.Range(1.2f, 4.5f), Random.Range(1f, 14f));
                rainDrop.transform.localScale = new Vector3(0.015f, 0.45f, 0.015f);
                rainDrop.transform.localRotation = Quaternion.Euler(18f, 0f, -6f);
                rainDrop.GetComponent<MeshRenderer>().sharedMaterial = rainMat;
            }
            m_RainGroup.SetActive(false);
        }

        /// <summary>
        /// 每帧更新 3D 窗外渲染与 9 项动态反馈
        /// </summary>
        public void UpdateFrame(CabEngineState state, float dt)
        {
            if (state == null) return;

            float speedKmh = state.CurrentSpeedKmh;
            float speedMps = speedKmh / 3.6f;

            // 1. 图层显隐控制
            if (m_TrackGroup != null) m_TrackGroup.SetActive(LayerVisibleTrack);
            if (m_PoleGroup != null) m_PoleGroup.SetActive(LayerVisiblePoles);
            if (m_SceneryGroup != null) m_SceneryGroup.SetActive(LayerVisibleScenery);
            if (m_MountainGroup != null) m_MountainGroup.SetActive(LayerVisibleMountains);
            if (m_StationGroup != null) m_StationGroup.SetActive(LayerVisibleStation);
            if (m_SignalGroup != null) m_SignalGroup.SetActive(LayerVisibleSignals);
            if (m_RainGroup != null) m_RainGroup.SetActive(IsRaining);

            // 2. 枕木对象池推进 (制造过枕木的强节奏速度感)
            m_TrackScrollOffset = (m_TrackScrollOffset - speedMps * dt) % SLEEPER_SPACING;
            if (m_TrackScrollOffset > 0f) m_TrackScrollOffset -= SLEEPER_SPACING;
            for (int i = 0; i < m_Sleepers.Count; i++)
            {
                float z = i * SLEEPER_SPACING + m_TrackScrollOffset;
                m_Sleepers[i].localPosition = new Vector3(0f, 0.04f, z);
            }

            // 3. 电线杆循环回收推进
            m_PoleScrollOffset = (m_PoleScrollOffset - speedMps * dt) % POLE_SPACING;
            if (m_PoleScrollOffset > 0f) m_PoleScrollOffset -= POLE_SPACING;
            for (int i = 0; i < m_Poles.Count; i++)
            {
                float z = i * POLE_SPACING + m_PoleScrollOffset;
                m_Poles[i].localPosition = new Vector3(3.2f, 0f, z);
            }

            // 4. 中景树屋对象池回收推进
            float sceneryLoop = m_SceneryObjects.Count * 7.5f;
            m_SceneryScrollOffset = (m_SceneryScrollOffset - speedMps * dt) % sceneryLoop;
            if (m_SceneryScrollOffset > 0f) m_SceneryScrollOffset -= sceneryLoop;
            for (int i = 0; i < m_SceneryObjects.Count; i++)
            {
                Vector3 cur = m_SceneryObjects[i].localPosition;
                float z = (i * 7.5f + m_SceneryScrollOffset);
                if (z < 0f) z += sceneryLoop;
                m_SceneryObjects[i].localPosition = new Vector3(cur.x, cur.y, z);
            }

            // 5. 进站可见与车站位置定位 (剩余距离 <150m 自然放大到停正中)
            float stationZ = state.DistanceToNextStation;
            m_StationGroup.transform.localPosition = new Vector3(0f, 0f, stationZ);

            // 6. 信号机红绿灯反馈 (接近车站50米内变红，否则绿灯)
            bool isGreen = state.DistanceToNextStation > 55f;
            if (m_SignalGreenLight != null)
            {
                m_SignalGreenLight.sharedMaterial = CabPalette.GetMaterial(isGreen ? CabPalette.ColorSignalGreen : CabPalette.HexColor("#123D1F"));
            }
            if (m_SignalRedLight != null)
            {
                m_SignalRedLight.sharedMaterial = CabPalette.GetMaterial(!isGreen ? CabPalette.ColorSignalRed : CabPalette.HexColor("#421414"));
            }

            // 7. 前照大灯 SpotLight 开关控制
            if (HeadlightSpot != null)
            {
                HeadlightSpot.enabled = state.HeadlightOn;
            }

            // 8. 昼夜变化 (方向光旋转与环境光插值)
            UpdateDayNightLighting(dt);

            // 9. 摄像机物理震动与 9 项反馈：
            // - 加减速俯仰 (SmoothDamp ±2°内，禁止瞬变)
            float targetPitch = state.TargetPitchAngle;
            m_CurrentPitch = Mathf.SmoothDamp(m_CurrentPitch, targetPitch, ref m_PitchVelocity, 0.22f);

            // - 轨道颠簸 (频率 ∝ 车速，小幅垂直微抖动)
            float speedAbs = Mathf.Abs(speedKmh);
            float bumpAmp = Mathf.Clamp(speedAbs / 100f * 0.035f, 0f, 0.04f);
            m_BumperTimer += dt * Mathf.Max(2f, speedAbs * 0.45f);
            float verticalBump = Mathf.Sin(m_BumperTimer) * bumpAmp;

            // - 制动顿挫 (突发制动急刹沉回弹)
            float brakeDip = (state.BrakeImpactTimer > 0f) ? Mathf.Sin(state.BrakeImpactTimer * Mathf.PI * 4f) * 0.06f : 0f;

            // - 鸣笛惊动 (前推位移微冲)
            float hornThrust = (state.IsHornActive) ? Mathf.Sin(state.HornPulseTimer * Mathf.PI * 10f) * 0.04f : 0f;

            // 组装司机眼位最终 Transform
            Vector3 camPos = m_BaseCamPos + new Vector3(0f, verticalBump - brakeDip, hornThrust);
            CabCamera.transform.localPosition = camPos;
            CabCamera.transform.localRotation = Quaternion.Euler(m_CurrentPitch, 0f, 0f);
        }

        private void UpdateDayNightLighting(float dt)
        {
            // 缓慢自转推进（一循环约3分钟）
            DayPhase = (DayPhase + dt / 180f) % 1f;

            // 插值计算光照与背景天空
            Color skyColor;
            Color sunColor;
            float sunIntensity;
            float sunAngleX;

            if (DayPhase < 0.25f)
            {
                // 黎明 -> 正午
                float t = DayPhase / 0.25f;
                skyColor = Color.Lerp(CabPalette.HexColor("#D49B74"), CabPalette.HexColor("#6A95B8"), t);
                sunColor = Color.Lerp(CabPalette.HexColor("#FFB073"), CabPalette.HexColor("#FFF7D9"), t);
                sunIntensity = Mathf.Lerp(0.6f, 1.15f, t);
                sunAngleX = Mathf.Lerp(12f, 58f, t);
            }
            else if (DayPhase < 0.65f)
            {
                // 正午 -> 黄昏
                float t = (DayPhase - 0.25f) / 0.4f;
                skyColor = Color.Lerp(CabPalette.HexColor("#6A95B8"), CabPalette.HexColor("#C86749"), t);
                sunColor = Color.Lerp(CabPalette.HexColor("#FFF7D9"), CabPalette.HexColor("#FFA852"), t);
                sunIntensity = Mathf.Lerp(1.15f, 0.75f, t);
                sunAngleX = Mathf.Lerp(58f, 15f, t);
            }
            else
            {
                // 黄昏 -> 深夜 -> 黎明
                float t = (DayPhase - 0.65f) / 0.35f;
                skyColor = Color.Lerp(CabPalette.HexColor("#C86749"), CabPalette.HexColor("#101A28"), Mathf.PingPong(t * 2f, 1f));
                sunColor = CabPalette.HexColor("#4B6584");
                sunIntensity = 0.22f;
                sunAngleX = 10f;
            }

            if (IsRaining)
            {
                skyColor = Color.Lerp(skyColor, CabPalette.HexColor("#3F4E5A"), 0.65f);
                sunIntensity *= 0.6f;
            }

            if (CabCamera != null) CabCamera.backgroundColor = skyColor;
            if (DirectionalSunLight != null)
            {
                DirectionalSunLight.color = sunColor;
                DirectionalSunLight.intensity = sunIntensity;
                DirectionalSunLight.transform.rotation = Quaternion.Euler(sunAngleX, -45f, 0f);
            }
        }

        private void OnDestroy()
        {
            if (TargetTexture != null)
            {
                TargetTexture.Release();
                Destroy(TargetTexture);
                TargetTexture = null;
            }
            if (m_SceneRoot != null)
            {
                Destroy(m_SceneRoot);
                m_SceneRoot = null;
            }
            CabPalette.Clear();
        }
    }
}
