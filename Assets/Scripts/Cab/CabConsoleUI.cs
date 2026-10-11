using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.Cab
{
    /// <summary>
    /// 驾驶台 2D 控制台 (UI Toolkit 纯代码构建)
    /// 包含：车窗框遮罩(露出 3D RenderTexture)、硬核模拟与悬浮 HUD 双皮肤、
    /// 顶部测试面板(分辨率/天气/昼夜/速度覆盖/图层)、动力学交互与 9 项动态反馈联动。
    /// </summary>
    public class CabConsoleUI : MonoBehaviour
    {
        private UIDocument uiDoc;
        private VisualElement root;
        private VisualElement viewportContainer;
        private Image rtDisplayImage;
        private VisualElement windowFrameMask;
        private VisualElement hardcorePanel;
        private VisualElement hudPanel;
        private VisualElement debugPanel;
        private VisualElement topBar;

        // 控制与状态
        private CabEngineState engineState;
        private Cab3DSceneController sceneController;

        // UI 引用缓存
        private Label lblSpeed;
        private Label lblRpm;
        private Label lblBrakePressure;
        private Label lblWaterTemp;
        private Label lblThrottle;
        private Label lblReverser;
        private Label lblBrakeNotch;
        private Label lblSignalStatus;
        private Label lblDistanceToStation;
        private Label lblStartupSequenceGuide;
        private Label lblAlert;

        // 悬浮 HUD 引用
        private Label hudSpeedBig;
        private Label hudNotchBig;
        private Label hudBrakeBig;
        private Label hudPressBig;
        private Label hudDistanceBig;

        private bool isHardcoreSkin = true;
        private bool isDebugPanelOpen = false;

        // 外部退出回调
        public System.Action OnCloseRequested;

        private static void SetBorder(IStyle s, float w, Color c)
        {
            s.borderTopWidth = w;
            s.borderBottomWidth = w;
            s.borderLeftWidth = w;
            s.borderRightWidth = w;
            s.borderTopColor = c;
            s.borderBottomColor = c;
            s.borderLeftColor = c;
            s.borderRightColor = c;
        }

        public static CabConsoleUI Create(Transform parent = null)
        {
            var go = new GameObject("CabConsoleUI");
            if (parent != null) go.transform.SetParent(parent, false);
            var console = go.AddComponent<CabConsoleUI>();
            return console;
        }

        private void Awake()
        {
            uiDoc = gameObject.AddComponent<UIDocument>();
            var panelSettings = Resources.Load<PanelSettings>("UI/TitleScreenPanelSettings");
            if (panelSettings == null)
            {
                panelSettings = Resources.Load<PanelSettings>("CabPanelSettings");
            }
            if (panelSettings == null)
            {
                var existingDocs = FindObjectsByType<UIDocument>();
                foreach (var d in existingDocs)
                {
                    if (d != null && d.panelSettings != null)
                    {
                        panelSettings = d.panelSettings;
                        break;
                    }
                }
            }
            uiDoc.panelSettings = panelSettings;
            uiDoc.sortingOrder = 999;

            // 初始化动力物理状态机与 3D 渲染控制器
            engineState = gameObject.AddComponent<CabEngineState>();
            sceneController = Cab3DSceneController.Create(transform);
            sceneController.BindEngineState(engineState);

            BuildUI();
        }

        private void Update()
        {
            if (engineState == null) return;
            UpdateUI();
        }

        private void OnDestroy()
        {
            if (sceneController != null)
            {
                Destroy(sceneController.gameObject);
            }
        }

        private void BuildUI()
        {
            root = uiDoc.rootVisualElement;
            root.Clear();
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.backgroundColor = Color.black;
            root.style.overflow = Overflow.Hidden;

            // 1. 3D 视口层 (放置 640x360 Point 过滤 RenderTexture)
            viewportContainer = new VisualElement { name = "cab-viewport-container" };
            viewportContainer.style.position = Position.Absolute;
            viewportContainer.style.left = 0;
            viewportContainer.style.top = 0;
            viewportContainer.style.right = 0;
            viewportContainer.style.bottom = 0;
            viewportContainer.style.justifyContent = Justify.Center;
            viewportContainer.style.alignItems = Align.Center;
            root.Add(viewportContainer);

            rtDisplayImage = new Image { name = "cab-rt-image" };
            rtDisplayImage.style.width = Length.Percent(100);
            rtDisplayImage.style.height = Length.Percent(100);
            rtDisplayImage.scaleMode = ScaleMode.ScaleAndCrop;
            if (sceneController != null && sceneController.TargetTexture != null)
            {
                rtDisplayImage.image = sceneController.TargetTexture;
            }
            viewportContainer.Add(rtDisplayImage);

            // 2. 车窗框架遮罩 (工字型/工业驾驶室铆钉框架)
            BuildWindowFrameMask();

            // 3. 2D 控制台主体 (硬核模拟面板)
            BuildHardcorePanel();

            // 4. 悬浮 HUD 模式面板 (初始隐藏)
            BuildHudPanel();

            // 5. 顶栏操作区 (退出按钮、皮肤切换、调试展开)
            BuildTopBar();

            // 6. 顶部高级调试抽屉面板
            BuildDebugDrawer();

            SetSkinMode(true);
        }

        /// <summary>
        /// 绘制工业车窗框 (中间镂空视窗，四周厚重金属框、雨刷臂与遮阳板)
        /// </summary>
        private void BuildWindowFrameMask()
        {
            windowFrameMask = new VisualElement { name = "cab-window-frame" };
            windowFrameMask.style.position = Position.Absolute;
            windowFrameMask.style.left = 0;
            windowFrameMask.style.top = 0;
            windowFrameMask.style.right = 0;
            windowFrameMask.style.bottom = 0;
            windowFrameMask.pickingMode = PickingMode.Ignore;

            // 顶部车顶包边与遮阳板
            var roofFrame = new VisualElement();
            roofFrame.style.position = Position.Absolute;
            roofFrame.style.left = 0;
            roofFrame.style.right = 0;
            roofFrame.style.top = 0;
            roofFrame.style.height = 42;
            roofFrame.style.backgroundColor = CabPalette.DarkBronze;
            roofFrame.style.borderBottomWidth = 3;
            roofFrame.style.borderBottomColor = CabPalette.IronBlack;
            windowFrameMask.Add(roofFrame);

            // 遮阳板装饰卡条
            var sunVisor = new VisualElement();
            sunVisor.style.width = 320;
            sunVisor.style.height = 18;
            sunVisor.style.marginLeft = 60;
            sunVisor.style.backgroundColor = new Color(0.18f, 0.16f, 0.14f, 0.95f);
            sunVisor.style.borderBottomLeftRadius = 6;
            sunVisor.style.borderBottomRightRadius = 6;
            roofFrame.Add(sunVisor);

            // 左侧立柱 A 柱
            var leftPillar = new VisualElement();
            leftPillar.style.position = Position.Absolute;
            leftPillar.style.left = 0;
            leftPillar.style.top = 0;
            leftPillar.style.bottom = 180;
            leftPillar.style.width = 48;
            leftPillar.style.backgroundColor = CabPalette.DarkBronze;
            leftPillar.style.borderRightWidth = 3;
            leftPillar.style.borderRightColor = CabPalette.IronBlack;
            windowFrameMask.Add(leftPillar);

            // 右侧立柱
            var rightPillar = new VisualElement();
            rightPillar.style.position = Position.Absolute;
            rightPillar.style.right = 0;
            rightPillar.style.top = 0;
            rightPillar.style.bottom = 180;
            rightPillar.style.width = 48;
            rightPillar.style.backgroundColor = CabPalette.DarkBronze;
            rightPillar.style.borderLeftWidth = 3;
            rightPillar.style.borderLeftColor = CabPalette.IronBlack;
            windowFrameMask.Add(rightPillar);

            // 中央中分窗柱 (制造真实驾驶室双窗视野)
            var centerPillar = new VisualElement();
            centerPillar.style.position = Position.Absolute;
            centerPillar.style.left = Length.Percent(49.2f);
            centerPillar.style.top = 40;
            centerPillar.style.bottom = 180;
            centerPillar.style.width = 18;
            centerPillar.style.backgroundColor = CabPalette.WarmBronze;
            centerPillar.style.borderLeftWidth = 2;
            centerPillar.style.borderRightWidth = 2;
            centerPillar.style.borderLeftColor = CabPalette.IronBlack;
            centerPillar.style.borderRightColor = CabPalette.IronBlack;
            windowFrameMask.Add(centerPillar);

            root.Add(windowFrameMask);
        }

        /// <summary>
        /// 构建硬核拟真控制台 (下半部 190px 仪表板与操控手柄台)
        /// </summary>
        private void BuildHardcorePanel()
        {
            hardcorePanel = new VisualElement { name = "cab-hardcore-panel" };
            hardcorePanel.style.position = Position.Absolute;
            hardcorePanel.style.left = 0;
            hardcorePanel.style.right = 0;
            hardcorePanel.style.bottom = 0;
            hardcorePanel.style.height = 190;
            hardcorePanel.style.backgroundColor = new Color(0.14f, 0.12f, 0.11f, 0.98f);
            hardcorePanel.style.borderTopWidth = 4;
            hardcorePanel.style.borderTopColor = CabPalette.WarmBronze;
            hardcorePanel.style.flexDirection = FlexDirection.Row;
            hardcorePanel.style.paddingLeft = 24;
            hardcorePanel.style.paddingRight = 24;
            hardcorePanel.style.paddingTop = 10;
            hardcorePanel.style.paddingBottom = 10;

            // 1. 左侧：操纵手柄与钥匙区 (换向手柄 + 主控推杆 + 大闸手柄)
            var leverSection = new VisualElement();
            leverSection.style.width = Length.Percent(32);
            leverSection.style.flexDirection = FlexDirection.Column;
            leverSection.style.justifyContent = Justify.SpaceBetween;
            hardcorePanel.Add(leverSection);

            // 开机程序卡与状态指示
            lblStartupSequenceGuide = new Label("【开机规程】①启机 -> ②挂前进档 -> ③制动缓解 -> ④提手柄牵引");
            lblStartupSequenceGuide.style.fontSize = 11;
            lblStartupSequenceGuide.style.color = CabPalette.AmberGlow;
            leverSection.Add(lblStartupSequenceGuide);

            // 控制手柄水平列
            var leversRow = new VisualElement();
            leversRow.style.flexDirection = FlexDirection.Row;
            leversRow.style.justifyContent = Justify.SpaceAround;
            leversRow.style.alignItems = Align.Center;
            leversRow.style.flexGrow = 1;
            leverSection.Add(leversRow);

            // (1) 启机电门按键
            var startBtn = new Button(() =>
            {
                if (!engineState.IsEngineRunning)
                {
                    engineState.StartEngine();
                }
                else
                {
                    engineState.StopEngine();
                }
            }) { text = "柴油机启机" };
            StyleLeverButton(startBtn, CabPalette.GoldTrim);
            leversRow.Add(startBtn);

            // (2) 换向手柄 (后 - 中 - 前)
            var reverserBox = new VisualElement();
            reverserBox.style.alignItems = Align.Center;
            lblReverser = new Label("换向: 中立");
            lblReverser.style.fontSize = 12;
            lblReverser.style.color = CabPalette.RicePaper;
            reverserBox.Add(lblReverser);

            var reverserBtnRow = new VisualElement();
            reverserBtnRow.style.flexDirection = FlexDirection.Row;
            var btnRevBack = new Button(() => engineState.SetReverser(ReverserPosition.Reverse)) { text = "后" };
            var btnRevNeutral = new Button(() => engineState.SetReverser(ReverserPosition.Neutral)) { text = "中" };
            var btnRevForward = new Button(() => engineState.SetReverser(ReverserPosition.Forward)) { text = "前" };
            StyleMiniButton(btnRevBack); StyleMiniButton(btnRevNeutral); StyleMiniButton(btnRevForward);
            reverserBtnRow.Add(btnRevBack); reverserBtnRow.Add(btnRevNeutral); reverserBtnRow.Add(btnRevForward);
            reverserBox.Add(reverserBtnRow);
            leversRow.Add(reverserBox);

            // (3) 主控手柄 (0 - 8 阶)
            var throttleBox = new VisualElement();
            throttleBox.style.alignItems = Align.Center;
            lblThrottle = new Label("主控: 0 阶");
            lblThrottle.style.fontSize = 12;
            lblThrottle.style.color = CabPalette.RicePaper;
            throttleBox.Add(lblThrottle);

            var throttleBtnRow = new VisualElement();
            throttleBtnRow.style.flexDirection = FlexDirection.Row;
            var btnThrottleDec = new Button(() => engineState.ThrottleDown()) { text = "降档 -" };
            var btnThrottleInc = new Button(() => engineState.ThrottleUp()) { text = "升档 +" };
            StyleMiniButton(btnThrottleDec); StyleMiniButton(btnThrottleInc);
            throttleBtnRow.Add(btnThrottleDec); throttleBtnRow.Add(btnThrottleInc);
            throttleBox.Add(throttleBtnRow);
            leversRow.Add(throttleBox);

            // (4) 大闸制动手柄 (充/运/初/常/紧)
            var brakeBox = new VisualElement();
            brakeBox.style.alignItems = Align.Center;
            lblBrakeNotch = new Label("大闸: 运转位");
            lblBrakeNotch.style.fontSize = 12;
            lblBrakeNotch.style.color = CabPalette.RicePaper;
            brakeBox.Add(lblBrakeNotch);

            var brakeBtnRow = new VisualElement();
            brakeBtnRow.style.flexDirection = FlexDirection.Row;
            var btnBrakeDec = new Button(() => engineState.BrakeReleaseStep()) { text = "缓解" };
            var btnBrakeInc = new Button(() => engineState.BrakeApplyStep()) { text = "制动" };
            var btnBrakeEmg = new Button(() => engineState.SetBrakeNotch(BrakeNotch.Emergency)) { text = "紧急" };
            StyleMiniButton(btnBrakeDec); StyleMiniButton(btnBrakeInc); StyleMiniButton(btnBrakeEmg);
            btnBrakeEmg.style.backgroundColor = CabPalette.CrimsonRed;
            brakeBtnRow.Add(btnBrakeDec); brakeBtnRow.Add(btnBrakeInc); brakeBtnRow.Add(btnBrakeEmg);
            brakeBox.Add(brakeBtnRow);
            leversRow.Add(brakeBox);

            // 2. 中间：拟真圆形仪表盘 (速度表、柴油机转速表、总风/列车管压表、水温表)
            var gaugeSection = new VisualElement();
            gaugeSection.style.width = Length.Percent(44);
            gaugeSection.style.flexDirection = FlexDirection.Row;
            gaugeSection.style.justifyContent = Justify.SpaceAround;
            gaugeSection.style.alignItems = Align.Center;
            hardcorePanel.Add(gaugeSection);

            // 速度表圆盘
            var speedDial = CreateRoundDial("速度 (km/h)", out lblSpeed);
            gaugeSection.Add(speedDial);

            // 转速表圆盘
            var rpmDial = CreateRoundDial("柴油机转速 (rpm)", out lblRpm);
            gaugeSection.Add(rpmDial);

            // 风压表圆盘
            var pressDial = CreateRoundDial("制动风压 (kPa)", out lblBrakePressure);
            gaugeSection.Add(pressDial);

            // 水温表圆盘
            var tempDial = CreateRoundDial("冷却水温 (℃)", out lblWaterTemp);
            gaugeSection.Add(tempDial);

            // 3. 右侧：信号机状态、鸣笛、车灯、雨刷辅助按键
            var auxSection = new VisualElement();
            auxSection.style.width = Length.Percent(24);
            auxSection.style.flexDirection = FlexDirection.Column;
            auxSection.style.justifyContent = Justify.SpaceAround;
            auxSection.style.paddingLeft = 12;
            hardcorePanel.Add(auxSection);

            // 信号与进站信息卡
            var infoCard = new VisualElement();
            infoCard.style.backgroundColor = new Color(0.08f, 0.08f, 0.09f, 0.9f);
            SetBorder(infoCard.style, 1, CabPalette.WarmBronze);
            infoCard.style.paddingLeft = 8; infoCard.style.paddingRight = 8;
            infoCard.style.paddingTop = 4; infoCard.style.paddingBottom = 4;
            infoCard.style.borderTopLeftRadius = 4; infoCard.style.borderTopRightRadius = 4;
            infoCard.style.borderBottomLeftRadius = 4; infoCard.style.borderBottomRightRadius = 4;

            lblSignalStatus = new Label("前方信号: [绿灯] 通行");
            lblSignalStatus.style.fontSize = 12;
            lblSignalStatus.style.color = new Color(0.2f, 0.9f, 0.3f);
            infoCard.Add(lblSignalStatus);

            lblDistanceToStation = new Label("距松桥站: 1000 m");
            lblDistanceToStation.style.fontSize = 12;
            lblDistanceToStation.style.color = CabPalette.RicePaper;
            infoCard.Add(lblDistanceToStation);

            lblAlert = new Label("");
            lblAlert.style.fontSize = 11;
            lblAlert.style.color = CabPalette.CrimsonRed;
            infoCard.Add(lblAlert);

            auxSection.Add(infoCard);

            // 鸣笛与辅助设备按键列
            var auxButtons = new VisualElement();
            auxButtons.style.flexDirection = FlexDirection.Row;
            auxButtons.style.justifyContent = Justify.SpaceBetween;

            // 鸣笛按钮 (带惊动震颤反馈)
            var hornBtn = new Button(() =>
            {
                if (sceneController != null) sceneController.TriggerHornShock();
            }) { text = "汽笛鸣笛" };
            StyleAuxButton(hornBtn, CabPalette.AmberGlow);
            auxButtons.Add(hornBtn);

            // 前照灯开关
            var lightBtn = new Button(() =>
            {
                if (sceneController != null) sceneController.ToggleHeadlight();
            }) { text = "前照大灯" };
            StyleAuxButton(lightBtn, CabPalette.GoldTrim);
            auxButtons.Add(lightBtn);

            // 雨刷开关
            var wiperBtn = new Button(() =>
            {
                if (sceneController != null) sceneController.ToggleWipers();
            }) { text = "风挡雨刷" };
            StyleAuxButton(wiperBtn, CabPalette.RicePaper);
            auxButtons.Add(wiperBtn);

            auxSection.Add(auxButtons);

            root.Add(hardcorePanel);
        }

        /// <summary>
        /// 构建悬浮 HUD 模式面板 (极简数字大字流)
        /// </summary>
        private void BuildHudPanel()
        {
            hudPanel = new VisualElement { name = "cab-hud-panel" };
            hudPanel.style.position = Position.Absolute;
            hudPanel.style.left = 32;
            hudPanel.style.bottom = 32;
            hudPanel.style.right = 32;
            hudPanel.style.height = 120;
            hudPanel.style.backgroundColor = new Color(0.05f, 0.08f, 0.1f, 0.82f);
            SetBorder(hudPanel.style, 2, CabPalette.AmberGlow);
            hudPanel.style.borderTopLeftRadius = 12;
            hudPanel.style.borderTopRightRadius = 12;
            hudPanel.style.borderBottomLeftRadius = 12;
            hudPanel.style.borderBottomRightRadius = 12;
            hudPanel.style.flexDirection = FlexDirection.Row;
            hudPanel.style.justifyContent = Justify.SpaceAround;
            hudPanel.style.alignItems = Align.Center;
            hudPanel.style.paddingLeft = 20;
            hudPanel.style.paddingRight = 20;

            // 速度大字
            var hudSpeedBox = new VisualElement { style = { alignItems = Align.Center } };
            hudSpeedBig = new Label("0.0");
            hudSpeedBig.style.fontSize = 44;
            hudSpeedBig.style.unityFontStyleAndWeight = FontStyle.Bold;
            hudSpeedBig.style.color = CabPalette.AmberGlow;
            var lblSpeedUnit = new Label("速度 KM/H") { style = { fontSize = 11, color = CabPalette.RicePaper } };
            hudSpeedBox.Add(hudSpeedBig); hudSpeedBox.Add(lblSpeedUnit);
            hudPanel.Add(hudSpeedBox);

            // 主控档位
            var hudNotchBox = new VisualElement { style = { alignItems = Align.Center } };
            hudNotchBig = new Label("0 阶");
            hudNotchBig.style.fontSize = 32;
            hudNotchBig.style.color = CabPalette.GoldTrim;
            var lblNotchUnit = new Label("牵引工况") { style = { fontSize = 11, color = CabPalette.RicePaper } };
            hudNotchBox.Add(hudNotchBig); hudNotchBox.Add(lblNotchUnit);
            hudPanel.Add(hudNotchBox);

            // 制动状态
            var hudBrakeBox = new VisualElement { style = { alignItems = Align.Center } };
            hudBrakeBig = new Label("运转");
            hudBrakeBig.style.fontSize = 28;
            hudBrakeBig.style.color = CabPalette.RicePaper;
            var lblBrakeUnit = new Label("大闸工况") { style = { fontSize = 11, color = CabPalette.RicePaper } };
            hudBrakeBox.Add(hudBrakeBig); hudBrakeBox.Add(lblBrakeUnit);
            hudPanel.Add(hudBrakeBox);

            // 风压
            var hudPressBox = new VisualElement { style = { alignItems = Align.Center } };
            hudPressBig = new Label("600");
            hudPressBig.style.fontSize = 30;
            hudPressBig.style.color = CabPalette.RailSteel;
            var lblPressUnit = new Label("风压 kPa") { style = { fontSize = 11, color = CabPalette.RicePaper } };
            hudPressBox.Add(hudPressBig); hudPressBox.Add(lblPressUnit);
            hudPanel.Add(hudPressBox);

            // 距松桥站
            var hudDistBox = new VisualElement { style = { alignItems = Align.Center } };
            hudDistanceBig = new Label("1000m");
            hudDistanceBig.style.fontSize = 30;
            hudDistanceBig.style.color = CabPalette.RicePaper;
            var lblDistUnit = new Label("距松桥站") { style = { fontSize = 11, color = CabPalette.RicePaper } };
            hudDistBox.Add(hudDistanceBig); hudDistBox.Add(lblDistUnit);
            hudPanel.Add(hudDistBox);

            // HUD 快捷拇指操作栏
            var hudControls = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var hudBtnThrottleUp = new Button(() => engineState.ThrottleUp()) { text = "牵引 +" };
            var hudBtnThrottleDown = new Button(() => engineState.ThrottleDown()) { text = "牵引 -" };
            var hudBtnBrake = new Button(() => engineState.BrakeApplyStep()) { text = "减速制动" };
            var hudBtnRelease = new Button(() => engineState.BrakeReleaseStep()) { text = "缓解" };
            StyleMiniButton(hudBtnThrottleUp); StyleMiniButton(hudBtnThrottleDown);
            StyleMiniButton(hudBtnBrake); StyleMiniButton(hudBtnRelease);
            hudControls.Add(hudBtnThrottleUp); hudControls.Add(hudBtnThrottleDown);
            hudControls.Add(hudBtnBrake); hudControls.Add(hudBtnRelease);
            hudPanel.Add(hudControls);

            root.Add(hudPanel);
        }

        /// <summary>
        /// 顶栏工具条 (返回桌面、皮肤切换、测试抽屉展开)
        /// </summary>
        private void BuildTopBar()
        {
            topBar = new VisualElement { name = "cab-top-bar" };
            topBar.style.position = Position.Absolute;
            topBar.style.left = 0;
            topBar.style.right = 0;
            topBar.style.top = 0;
            topBar.style.height = 38;
            topBar.style.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 0.75f);
            topBar.style.flexDirection = FlexDirection.Row;
            topBar.style.justifyContent = Justify.SpaceBetween;
            topBar.style.alignItems = Align.Center;
            topBar.style.paddingLeft = 16;
            topBar.style.paddingRight = 16;

            // 左侧标题
            var titleBox = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var iconLbl = new Label("🚂") { style = { fontSize = 18, marginRight = 8 } };
            var titleLbl = new Label("0721型机车驾驶台 · 3D程序化像素视界")
            {
                style = { fontSize = 13, color = CabPalette.AmberGlow, unityFontStyleAndWeight = FontStyle.Bold }
            };
            titleBox.Add(iconLbl); titleBox.Add(titleLbl);
            topBar.Add(titleBox);

            // 右侧功能按钮组
            var rightTools = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            // 皮肤切换按钮
            var btnToggleSkin = new Button(() =>
            {
                SetSkinMode(!isHardcoreSkin);
            }) { text = isHardcoreSkin ? "切换: 悬浮HUD" : "切换: 硬核控制台" };
            StyleTopBarButton(btnToggleSkin);
            rightTools.Add(btnToggleSkin);

            // 调试抽屉切换
            var btnToggleDebug = new Button(() =>
            {
                isDebugPanelOpen = !isDebugPanelOpen;
                debugPanel.style.display = isDebugPanelOpen ? DisplayStyle.Flex : DisplayStyle.None;
            }) { text = "⚙ 调试面板" };
            StyleTopBarButton(btnToggleDebug);
            rightTools.Add(btnToggleDebug);

            // 退出 / 返回桌面
            var btnExit = new Button(() =>
            {
                CloseCab();
            }) { text = "✕ 返回桌面" };
            StyleTopBarButton(btnExit);
            btnExit.style.backgroundColor = CabPalette.CrimsonRed;
            rightTools.Add(btnExit);

            topBar.Add(rightTools);
            root.Add(topBar);
        }

        /// <summary>
        /// 顶部调试抽屉面板 (可测试速度覆盖、昼夜切换、天气雨雪、单层显隐、RT 分辨率阶梯)
        /// </summary>
        private void BuildDebugDrawer()
        {
            debugPanel = new VisualElement { name = "cab-debug-drawer" };
            debugPanel.style.position = Position.Absolute;
            debugPanel.style.top = 40;
            debugPanel.style.right = 16;
            debugPanel.style.width = 460;
            debugPanel.style.backgroundColor = new Color(0.1f, 0.11f, 0.14f, 0.95f);
            SetBorder(debugPanel.style, 2, CabPalette.GoldTrim);
            debugPanel.style.borderTopLeftRadius = 8; debugPanel.style.borderTopRightRadius = 8;
            debugPanel.style.borderBottomLeftRadius = 8; debugPanel.style.borderBottomRightRadius = 8;
            debugPanel.style.paddingLeft = 14; debugPanel.style.paddingRight = 14;
            debugPanel.style.paddingTop = 10; debugPanel.style.paddingBottom = 12;
            debugPanel.style.display = DisplayStyle.None;

            var dbgTitle = new Label("【驾驶台 3D 场景实测面板】")
            {
                style = { fontSize = 14, color = CabPalette.GoldTrim, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 }
            };
            debugPanel.Add(dbgTitle);

            // 1. 速度覆盖测试
            var speedDebugRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
            var lblSpeedDbg = new Label("速度测试:") { style = { width = 70, fontSize = 12, color = CabPalette.RicePaper } };
            var btnSpd0 = new Button(() => SetDebugSpeed(0)) { text = "0" };
            var btnSpd30 = new Button(() => SetDebugSpeed(30)) { text = "30" };
            var btnSpd60 = new Button(() => SetDebugSpeed(60)) { text = "60" };
            var btnSpd90 = new Button(() => SetDebugSpeed(90)) { text = "90" };
            var btnSpd120 = new Button(() => SetDebugSpeed(120)) { text = "120" };
            StyleMiniButton(btnSpd0); StyleMiniButton(btnSpd30); StyleMiniButton(btnSpd60); StyleMiniButton(btnSpd90); StyleMiniButton(btnSpd120);
            speedDebugRow.Add(lblSpeedDbg);
            speedDebugRow.Add(btnSpd0); speedDebugRow.Add(btnSpd30); speedDebugRow.Add(btnSpd60); speedDebugRow.Add(btnSpd90); speedDebugRow.Add(btnSpd120);
            debugPanel.Add(speedDebugRow);

            // 2. 昼夜切换
            var todRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
            var lblTod = new Label("昼夜时段:") { style = { width = 70, fontSize = 12, color = CabPalette.RicePaper } };
            var btnMorning = new Button(() => sceneController?.ForceTimeOfDay(Cab3DSceneController.TimeOfDay.Morning)) { text = "清晨" };
            var btnNoon = new Button(() => sceneController?.ForceTimeOfDay(Cab3DSceneController.TimeOfDay.Noon)) { text = "正午" };
            var btnDusk = new Button(() => sceneController?.ForceTimeOfDay(Cab3DSceneController.TimeOfDay.Dusk)) { text = "黄昏" };
            var btnNight = new Button(() => sceneController?.ForceTimeOfDay(Cab3DSceneController.TimeOfDay.Night)) { text = "深夜" };
            StyleMiniButton(btnMorning); StyleMiniButton(btnNoon); StyleMiniButton(btnDusk); StyleMiniButton(btnNight);
            todRow.Add(lblTod); todRow.Add(btnMorning); todRow.Add(btnNoon); todRow.Add(btnDusk); todRow.Add(btnNight);
            debugPanel.Add(todRow);

            // 3. 天气切换
            var weatherRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
            var lblWeather = new Label("天气模拟:") { style = { width = 70, fontSize = 12, color = CabPalette.RicePaper } };
            var btnClear = new Button(() => sceneController?.ForceWeather(false)) { text = "晴天" };
            var btnRain = new Button(() => sceneController?.ForceWeather(true)) { text = "沙雨天" };
            StyleMiniButton(btnClear); StyleMiniButton(btnRain);
            weatherRow.Add(lblWeather); weatherRow.Add(btnClear); weatherRow.Add(btnRain);
            debugPanel.Add(weatherRow);

            // 4. RenderTexture 像素分辨率切换
            var resRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
            var lblRes = new Label("像素精细:") { style = { width = 70, fontSize = 12, color = CabPalette.RicePaper } };
            var btnRes320 = new Button(() => sceneController?.ChangeResolution(320, 180)) { text = "320×180" };
            var btnRes480 = new Button(() => sceneController?.ChangeResolution(480, 270)) { text = "480×270" };
            var btnRes640 = new Button(() => sceneController?.ChangeResolution(640, 360)) { text = "640×360(标)" };
            var btnRes960 = new Button(() => sceneController?.ChangeResolution(960, 540)) { text = "960×540" };
            StyleMiniButton(btnRes320); StyleMiniButton(btnRes480); StyleMiniButton(btnRes640); StyleMiniButton(btnRes960);
            resRow.Add(lblRes); resRow.Add(btnRes320); resRow.Add(btnRes480); resRow.Add(btnRes640); resRow.Add(btnRes960);
            debugPanel.Add(resRow);

            // 5. 动态反馈与镜头测试
            var feedbackRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var lblFb = new Label("动态触发:") { style = { width = 70, fontSize = 12, color = CabPalette.RicePaper } };
            var btnHorn = new Button(() => sceneController?.TriggerHornShock()) { text = "鸣笛抖动" };
            var btnLight = new Button(() => sceneController?.ToggleHeadlight()) { text = "切换车灯" };
            var btnWiper = new Button(() => sceneController?.ToggleWipers()) { text = "切换雨刷" };
            StyleMiniButton(btnHorn); StyleMiniButton(btnLight); StyleMiniButton(btnWiper);
            feedbackRow.Add(lblFb); feedbackRow.Add(btnHorn); feedbackRow.Add(btnLight); feedbackRow.Add(btnWiper);
            debugPanel.Add(feedbackRow);

            root.Add(debugPanel);
        }

        private void SetDebugSpeed(float targetSpeed)
        {
            if (engineState != null)
            {
                if (!engineState.IsEngineRunning && targetSpeed > 0)
                {
                    engineState.StartEngine();
                    engineState.SetReverser(ReverserPosition.Forward);
                    engineState.SetBrakeNotch(BrakeNotch.Release);
                }
                engineState.OverrideSpeed(targetSpeed);
            }
        }

        private void SetSkinMode(bool hardcore)
        {
            isHardcoreSkin = hardcore;
            hardcorePanel.style.display = hardcore ? DisplayStyle.Flex : DisplayStyle.None;
            hudPanel.style.display = hardcore ? DisplayStyle.None : DisplayStyle.Flex;
            windowFrameMask.style.display = hardcore ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void CloseCab()
        {
            if (OnCloseRequested != null)
            {
                OnCloseRequested.Invoke();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void UpdateUI()
        {
            // 更新硬核面板文字
            if (lblSpeed != null) lblSpeed.text = engineState.CurrentSpeedKmh.ToString("F1");
            if (lblRpm != null) lblRpm.text = Mathf.RoundToInt(engineState.EngineRpm).ToString();
            if (lblBrakePressure != null) lblBrakePressure.text = Mathf.RoundToInt(engineState.BrakeCylinderKpa).ToString();
            if (lblWaterTemp != null) lblWaterTemp.text = engineState.WaterTempCelsius.ToString("F0");

            if (lblThrottle != null) lblThrottle.text = $"主控: {engineState.ThrottleNotch} 阶";
            if (lblReverser != null) lblReverser.text = $"换向: {engineState.GetReverserString()}";
            if (lblBrakeNotch != null) lblBrakeNotch.text = $"大闸: {engineState.GetBrakeNotchString()}";

            if (lblDistanceToStation != null)
            {
                lblDistanceToStation.text = $"距松桥站: {Mathf.Max(0, Mathf.RoundToInt(engineState.DistanceToStationMeters))} m";
            }

            if (lblSignalStatus != null)
            {
                bool isGreen = engineState.SignalIsGreen;
                lblSignalStatus.text = isGreen ? "前方信号: [绿灯] 允许运行" : "前方信号: [红灯] 停车警戒";
                lblSignalStatus.style.color = isGreen ? new Color(0.2f, 0.9f, 0.3f) : CabPalette.CrimsonRed;
            }

            if (lblAlert != null)
            {
                if (!engineState.IsEngineRunning)
                {
                    lblAlert.text = "【未启机】点击电门启机";
                }
                else if (engineState.CurrentReverser == ReverserPosition.Neutral && engineState.ThrottleNotch > 0)
                {
                    lblAlert.text = "【警告】换向器居中空转中！";
                }
                else if (engineState.CurrentBrakeNotch >= BrakeNotch.Common && engineState.ThrottleNotch > 0)
                {
                    lblAlert.text = "【警示】严禁带制动牵引！";
                }
                else
                {
                    lblAlert.text = "";
                }
            }

            // 更新悬浮 HUD 大字
            if (hudSpeedBig != null) hudSpeedBig.text = engineState.CurrentSpeedKmh.ToString("F1");
            if (hudNotchBig != null) hudNotchBig.text = $"{engineState.ThrottleNotch} 阶";
            if (hudBrakeBig != null) hudBrakeBig.text = engineState.GetBrakeNotchString();
            if (hudPressBig != null) hudPressBig.text = Mathf.RoundToInt(engineState.BrakeCylinderKpa).ToString();
            if (hudDistanceBig != null) hudDistanceBig.text = $"{Mathf.Max(0, Mathf.RoundToInt(engineState.DistanceToStationMeters))}m";

            // 保持 RenderTexture 图像实时刷新
            if (rtDisplayImage != null && sceneController != null && rtDisplayImage.image != sceneController.TargetTexture)
            {
                rtDisplayImage.image = sceneController.TargetTexture;
            }
        }

        private VisualElement CreateRoundDial(string title, out Label valLabel)
        {
            var dial = new VisualElement();
            dial.style.width = 96;
            dial.style.height = 96;
            dial.style.backgroundColor = new Color(0.08f, 0.08f, 0.09f, 0.95f);
            SetBorder(dial.style, 3, CabPalette.WarmBronze);
            dial.style.borderTopLeftRadius = 48;
            dial.style.borderTopRightRadius = 48;
            dial.style.borderBottomLeftRadius = 48;
            dial.style.borderBottomRightRadius = 48;
            dial.style.alignItems = Align.Center;
            dial.style.justifyContent = Justify.Center;

            valLabel = new Label("0");
            valLabel.style.fontSize = 20;
            valLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valLabel.style.color = CabPalette.AmberGlow;
            dial.Add(valLabel);

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 9;
            titleLabel.style.color = CabPalette.RicePaper;
            dial.Add(titleLabel);

            return dial;
        }

        private void StyleLeverButton(Button btn, Color accent)
        {
            btn.style.height = 36;
            btn.style.paddingLeft = 10;
            btn.style.paddingRight = 10;
            btn.style.fontSize = 12;
            btn.style.backgroundColor = new Color(0.2f, 0.18f, 0.15f, 0.95f);
            btn.style.color = accent;
            SetBorder(btn.style, 1, accent);
            btn.style.borderTopLeftRadius = 4; btn.style.borderTopRightRadius = 4;
            btn.style.borderBottomLeftRadius = 4; btn.style.borderBottomRightRadius = 4;
        }

        private void StyleMiniButton(Button btn)
        {
            btn.style.height = 26;
            btn.style.marginLeft = 2;
            btn.style.marginRight = 2;
            btn.style.paddingLeft = 6;
            btn.style.paddingRight = 6;
            btn.style.fontSize = 11;
            btn.style.backgroundColor = new Color(0.22f, 0.22f, 0.25f, 0.9f);
            btn.style.color = CabPalette.RicePaper;
            SetBorder(btn.style, 1, new Color(0.4f, 0.4f, 0.45f, 0.6f));
            btn.style.borderTopLeftRadius = 3; btn.style.borderTopRightRadius = 3;
            btn.style.borderBottomLeftRadius = 3; btn.style.borderBottomRightRadius = 3;
        }

        private void StyleAuxButton(Button btn, Color fg)
        {
            btn.style.height = 30;
            btn.style.marginLeft = 2;
            btn.style.marginRight = 2;
            btn.style.paddingLeft = 8;
            btn.style.paddingRight = 8;
            btn.style.fontSize = 11;
            btn.style.backgroundColor = new Color(0.18f, 0.18f, 0.22f, 0.9f);
            btn.style.color = fg;
            SetBorder(btn.style, 1, fg);
            btn.style.borderTopLeftRadius = 4; btn.style.borderTopRightRadius = 4;
            btn.style.borderBottomLeftRadius = 4; btn.style.borderBottomRightRadius = 4;
        }

        private void StyleTopBarButton(Button btn)
        {
            btn.style.height = 26;
            btn.style.marginLeft = 6;
            btn.style.paddingLeft = 10;
            btn.style.paddingRight = 10;
            btn.style.fontSize = 11;
            btn.style.backgroundColor = new Color(0.2f, 0.22f, 0.26f, 0.9f);
            btn.style.color = CabPalette.RicePaper;
            SetBorder(btn.style, 1, new Color(0.5f, 0.5f, 0.6f, 0.5f));
            btn.style.borderTopLeftRadius = 4; btn.style.borderTopRightRadius = 4;
            btn.style.borderBottomLeftRadius = 4; btn.style.borderBottomRightRadius = 4;
        }
    }
}
