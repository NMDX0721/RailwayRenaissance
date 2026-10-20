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

        // 拟真指针与物理构件缓存
        private VisualElement needleSpeed;
        private VisualElement needleRpm;
        private VisualElement needleBrakePipe; // 列车管白针
        private VisualElement needleMainRes;   // 总风缸红针
        private VisualElement needleWaterTemp;
        private VisualElement needleOilPress;
        private VisualElement needleCurrent;   // 牵引电流
        private VisualElement throttleGripHandle;
        private VisualElement reverserGripHandle;
        private VisualElement brakeGripHandle;
        private VisualElement signalLampGreen;
        private VisualElement signalLampYellow;
        private VisualElement signalLampRedYellow;
        private VisualElement signalLampRed;

        private Label lblTotalMileage;
        private Label lblCurrentAmps;
        private Label lblMainResPress;
        private Label lblBrakePipePress;

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

        // 琴键按键高亮视觉项
        private VisualElement keyHeadlight;
        private VisualElement keyInstrument;
        private VisualElement keyFuelPump;
        private VisualElement keySand;
        private VisualElement keyHeater;

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
        /// 绘制工业车窗框 (中间镂空视窗，四周厚重金属框、中柱机车信号灯箱、雨刷臂与遮阳板)
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
            roofFrame.style.backgroundColor = CabPalette.CabGreen;
            roofFrame.style.borderBottomWidth = 3;
            roofFrame.style.borderBottomColor = CabPalette.CabConsoleDark;
            windowFrameMask.Add(roofFrame);

            // 遮阳板装饰卡条 (茶色半透明滤光板)
            var sunVisor = new VisualElement();
            sunVisor.style.width = 340;
            sunVisor.style.height = 20;
            sunVisor.style.marginLeft = 60;
            sunVisor.style.backgroundColor = new Color(0.28f, 0.22f, 0.12f, 0.88f);
            sunVisor.style.borderBottomLeftRadius = 6;
            sunVisor.style.borderBottomRightRadius = 6;
            SetBorder(sunVisor.style, 1, CabPalette.GoldTrim);
            roofFrame.Add(sunVisor);

            // 左侧立柱 A 柱 (工业墨绿 + 铆钉细节)
            var leftPillar = new VisualElement();
            leftPillar.style.position = Position.Absolute;
            leftPillar.style.left = 0;
            leftPillar.style.top = 0;
            leftPillar.style.bottom = 240;
            leftPillar.style.width = 50;
            leftPillar.style.backgroundColor = CabPalette.CabGreen;
            leftPillar.style.borderRightWidth = 3;
            leftPillar.style.borderRightColor = CabPalette.CabConsoleDark;
            AddRivets(leftPillar, 6);
            windowFrameMask.Add(leftPillar);

            // 右侧立柱
            var rightPillar = new VisualElement();
            rightPillar.style.position = Position.Absolute;
            rightPillar.style.right = 0;
            rightPillar.style.top = 0;
            rightPillar.style.bottom = 240;
            rightPillar.style.width = 50;
            rightPillar.style.backgroundColor = CabPalette.CabGreen;
            rightPillar.style.borderLeftWidth = 3;
            rightPillar.style.borderLeftColor = CabPalette.CabConsoleDark;
            AddRivets(rightPillar, 6);
            windowFrameMask.Add(rightPillar);

            // 中央中分窗柱 (制造真实机车双窗视野，并在中柱安装竖排机车信号机)
            var centerPillar = new VisualElement();
            centerPillar.style.position = Position.Absolute;
            centerPillar.style.left = Length.Percent(48.5f);
            centerPillar.style.top = 38;
            centerPillar.style.bottom = 240;
            centerPillar.style.width = 34;
            centerPillar.style.backgroundColor = CabPalette.CabGreen;
            centerPillar.style.borderLeftWidth = 2;
            centerPillar.style.borderRightWidth = 2;
            centerPillar.style.borderLeftColor = CabPalette.CabConsoleDark;
            centerPillar.style.borderRightColor = CabPalette.CabConsoleDark;
            centerPillar.style.alignItems = Align.Center;

            // 竖立在中柱上的八显示机车信号机灯箱 (Cab Signal Box)
            var signalBox = new VisualElement();
            signalBox.style.marginTop = 24;
            signalBox.style.width = 28;
            signalBox.style.paddingTop = 6;
            signalBox.style.paddingBottom = 6;
            signalBox.style.backgroundColor = CabPalette.GaugeFaceDark;
            SetBorder(signalBox.style, 2, CabPalette.GaugeBezelMetal);
            signalBox.style.borderTopLeftRadius = 4; signalBox.style.borderTopRightRadius = 4;
            signalBox.style.borderBottomLeftRadius = 4; signalBox.style.borderBottomRightRadius = 4;
            signalBox.style.alignItems = Align.Center;

            // 铭牌小字 [信号]
            var lblSigTitle = new Label("信号") { style = { fontSize = 8, color = CabPalette.RicePaper, marginBottom = 4 } };
            signalBox.Add(lblSigTitle);

            // 4 颗透镜灯 (绿 / 黄 / 红黄 / 红)
            signalLampGreen = CreateSignalLens(CabPalette.SignalLensGreen);
            signalLampYellow = CreateSignalLens(CabPalette.SignalLensYellow);
            signalLampRedYellow = CreateSignalLens(new Color(0.95f, 0.5f, 0.1f));
            signalLampRed = CreateSignalLens(CabPalette.SignalLensRed);

            signalBox.Add(signalLampGreen);
            signalBox.Add(signalLampYellow);
            signalBox.Add(signalLampRedYellow);
            signalBox.Add(signalLampRed);

            centerPillar.Add(signalBox);
            windowFrameMask.Add(centerPillar);

            root.Add(windowFrameMask);
        }

        private VisualElement CreateSignalLens(Color glowColor)
        {
            var holder = new VisualElement();
            holder.style.width = 18;
            holder.style.height = 18;
            holder.style.marginBottom = 4;
            holder.style.borderTopLeftRadius = 9; holder.style.borderTopRightRadius = 9;
            holder.style.borderBottomLeftRadius = 9; holder.style.borderBottomRightRadius = 9;
            holder.style.backgroundColor = CabPalette.SignalLensDark;
            SetBorder(holder.style, 1.5f, new Color(0.1f, 0.1f, 0.12f));
            holder.style.alignItems = Align.Center;
            holder.style.justifyContent = Justify.Center;

            var innerCore = new VisualElement();
            innerCore.name = "lens-core";
            innerCore.style.width = 10;
            innerCore.style.height = 10;
            innerCore.style.borderTopLeftRadius = 5; innerCore.style.borderTopRightRadius = 5;
            innerCore.style.borderBottomLeftRadius = 5; innerCore.style.borderBottomRightRadius = 5;
            innerCore.style.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
            holder.Add(innerCore);

            return holder;
        }

        private void AddRivets(VisualElement pillar, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var rivet = new VisualElement();
                rivet.style.position = Position.Absolute;
                rivet.style.left = 18;
                rivet.style.top = 60 + i * 55;
                rivet.style.width = 8;
                rivet.style.height = 8;
                rivet.style.borderTopLeftRadius = 4; rivet.style.borderTopRightRadius = 4;
                rivet.style.borderBottomLeftRadius = 4; rivet.style.borderBottomRightRadius = 4;
                rivet.style.backgroundColor = CabPalette.BoltSilver;
                SetBorder(rivet.style, 1, CabPalette.IronBlack);
                pillar.Add(rivet);
            }
        }

        /// <summary>
        /// 构建硬核拟真控制台 (下半部 190px 仪表板与操控手柄台)
        /// </summary>
        /// <summary>
        /// 构建硬核拟真控制台 (NF-5「耕牛·改」762mm 窄轨内燃机车工业机械操纵台)
        /// </summary>
        private void BuildHardcorePanel()
        {
            hardcorePanel = new VisualElement { name = "cab-hardcore-panel" };
            hardcorePanel.style.position = Position.Absolute;
            hardcorePanel.style.left = 0;
            hardcorePanel.style.right = 0;
            hardcorePanel.style.bottom = 0;
            hardcorePanel.style.height = 250;
            hardcorePanel.style.backgroundColor = CabPalette.CabConsoleDark;
            hardcorePanel.style.borderTopWidth = 4;
            hardcorePanel.style.borderTopColor = CabPalette.CabPanelTrim;
            hardcorePanel.style.flexDirection = FlexDirection.Column;
            hardcorePanel.style.paddingLeft = 16;
            hardcorePanel.style.paddingRight = 16;
            hardcorePanel.style.paddingTop = 0;
            hardcorePanel.style.paddingBottom = 8;

            // 0. 操纵台顶边缘黄铜铸造长方铭牌与内六角螺栓装饰条
            var topPlaqueBar = new VisualElement();
            topPlaqueBar.style.height = 24;
            topPlaqueBar.style.backgroundColor = CabPalette.CabPanelTrim;
            topPlaqueBar.style.flexDirection = FlexDirection.Row;
            topPlaqueBar.style.justifyContent = Justify.Center;
            topPlaqueBar.style.alignItems = Align.Center;
            topPlaqueBar.style.marginLeft = -16;
            topPlaqueBar.style.marginRight = -16;
            topPlaqueBar.style.borderBottomWidth = 1;
            topPlaqueBar.style.borderBottomColor = CabPalette.IronBlack;

            var plaqueText = new Label("【 NF-5 型窄轨内燃机车 · 耕牛·改 】 太原机车车辆厂 1965 定型 · 762mm 轨距");
            plaqueText.style.fontSize = 11;
            plaqueText.style.color = CabPalette.BrassPlaqueGold;
            plaqueText.style.unityFontStyleAndWeight = FontStyle.Bold;
            topPlaqueBar.Add(plaqueText);
            hardcorePanel.Add(topPlaqueBar);

            // 主操纵区域 (分为：左动力主控区、中机械仪表群、右 JZ-7 制动操控区)
            var mainDeskRow = new VisualElement();
            mainDeskRow.style.flexDirection = FlexDirection.Row;
            mainDeskRow.style.flexGrow = 1;
            mainDeskRow.style.paddingTop = 6;
            mainDeskRow.style.paddingBottom = 4;
            hardcorePanel.Add(mainDeskRow);

            // ================= 1. 左侧：动力主控台 (主控提手柄导轨 + 换向手柄) =================
            var leftCtrlDesk = new VisualElement();
            leftCtrlDesk.style.width = Length.Percent(27);
            leftCtrlDesk.style.flexDirection = FlexDirection.Column;
            leftCtrlDesk.style.justifyContent = Justify.SpaceBetween;
            leftCtrlDesk.style.paddingRight = 10;
            mainDeskRow.Add(leftCtrlDesk);

            // 开机规程标牌
            lblStartupSequenceGuide = new Label("规程: ①启机 -> ②换向 -> ③缓解 -> ④提手柄");
            lblStartupSequenceGuide.style.fontSize = 10;
            lblStartupSequenceGuide.style.color = CabPalette.AmberGlow;
            leftCtrlDesk.Add(lblStartupSequenceGuide);

            var leversRow = new VisualElement();
            leversRow.style.flexDirection = FlexDirection.Row;
            leversRow.style.justifyContent = Justify.SpaceAround;
            leversRow.style.alignItems = Align.Center;
            leversRow.style.flexGrow = 1;
            leftCtrlDesk.Add(leversRow);

            // (1) 机械换向手柄 (后退 - 中立 - 前进)
            var reverserBox = new VisualElement();
            reverserBox.style.alignItems = Align.Center;
            lblReverser = new Label("换向: 中立");
            lblReverser.style.fontSize = 11;
            lblReverser.style.color = CabPalette.RicePaper;
            reverserBox.Add(lblReverser);

            var reverserFrame = new VisualElement();
            reverserFrame.style.width = 68;
            reverserFrame.style.height = 70;
            reverserFrame.style.backgroundColor = CabPalette.GaugeFaceDark;
            SetBorder(reverserFrame.style, 1.5f, CabPalette.CabPanelTrim);
            reverserFrame.style.borderTopLeftRadius = 4; reverserFrame.style.borderTopRightRadius = 4;
            reverserFrame.style.borderBottomLeftRadius = 4; reverserFrame.style.borderBottomRightRadius = 4;
            reverserFrame.style.justifyContent = Justify.SpaceAround;
            reverserFrame.style.alignItems = Align.Center;
            reverserFrame.style.paddingTop = 2; reverserFrame.style.paddingBottom = 2;

            var btnRevFwd = new Button(() => engineState.SetReverser(ReverserPosition.Forward)) { text = "▲ 前进 F" };
            var btnRevNeu = new Button(() => engineState.SetReverser(ReverserPosition.Neutral)) { text = "● 中立 N" };
            var btnRevRev = new Button(() => engineState.SetReverser(ReverserPosition.Reverse)) { text = "▼ 后退 R" };
            StyleMiniLeverBtn(btnRevFwd); StyleMiniLeverBtn(btnRevNeu); StyleMiniLeverBtn(btnRevRev);
            reverserFrame.Add(btnRevFwd); reverserFrame.Add(btnRevNeu); reverserFrame.Add(btnRevRev);
            reverserBox.Add(reverserFrame);
            leversRow.Add(reverserBox);

            // (2) 主控提手柄实体铣槽 (0 ~ 8 阶)
            var throttleBox = new VisualElement();
            throttleBox.style.alignItems = Align.Center;
            lblThrottle = new Label("主控: 0 阶");
            lblThrottle.style.fontSize = 11;
            lblThrottle.style.color = CabPalette.RicePaper;
            throttleBox.Add(lblThrottle);

            var throttleSlotFrame = new VisualElement();
            throttleSlotFrame.style.width = 82;
            throttleSlotFrame.style.height = 96;
            throttleSlotFrame.style.backgroundColor = CabPalette.GaugeFaceDark;
            SetBorder(throttleSlotFrame.style, 1.5f, CabPalette.CabPanelTrim);
            throttleSlotFrame.style.borderTopLeftRadius = 4; throttleSlotFrame.style.borderTopRightRadius = 4;
            throttleSlotFrame.style.borderBottomLeftRadius = 4; throttleSlotFrame.style.borderBottomRightRadius = 4;
            throttleSlotFrame.style.flexDirection = FlexDirection.Row;
            throttleSlotFrame.style.alignItems = Align.Center;
            throttleSlotFrame.style.justifyContent = Justify.SpaceAround;

            // 竖槽导轨
            var trackSlot = new VisualElement();
            trackSlot.style.width = 16;
            trackSlot.style.height = 80;
            trackSlot.style.backgroundColor = new Color(0.05f, 0.05f, 0.06f);
            SetBorder(trackSlot.style, 1, new Color(0.2f, 0.2f, 0.22f));
            trackSlot.style.borderTopLeftRadius = 3; trackSlot.style.borderTopRightRadius = 3;
            trackSlot.style.borderBottomLeftRadius = 3; trackSlot.style.borderBottomRightRadius = 3;
            trackSlot.style.position = Position.Relative;

            // 手柄滑块头
            throttleGripHandle = new VisualElement();
            throttleGripHandle.style.position = Position.Absolute;
            throttleGripHandle.style.left = -6;
            throttleGripHandle.style.top = 64; // 0档位于底部
            throttleGripHandle.style.width = 28;
            throttleGripHandle.style.height = 16;
            throttleGripHandle.style.backgroundColor = CabPalette.BrassPlaqueGold;
            SetBorder(throttleGripHandle.style, 1, CabPalette.IronBlack);
            throttleGripHandle.style.borderTopLeftRadius = 3; throttleGripHandle.style.borderTopRightRadius = 3;
            throttleGripHandle.style.borderBottomLeftRadius = 3; throttleGripHandle.style.borderBottomRightRadius = 3;
            trackSlot.Add(throttleGripHandle);
            throttleSlotFrame.Add(trackSlot);

            // 快速提档/降档按钮列
            var throttleBtns = new VisualElement();
            throttleBtns.style.flexDirection = FlexDirection.Column;
            throttleBtns.style.justifyContent = Justify.SpaceAround;
            var btnThrotInc = new Button(() => engineState.ThrottleUp()) { text = "提手柄 +" };
            var btnThrotDec = new Button(() => engineState.ThrottleDown()) { text = "降手柄 -" };
            StyleMiniLeverBtn(btnThrotInc); StyleMiniLeverBtn(btnThrotDec);
            btnThrotInc.style.height = 34; btnThrotDec.style.height = 34;
            throttleBtns.Add(btnThrotInc); throttleBtns.Add(btnThrotDec);
            throttleSlotFrame.Add(throttleBtns);

            throttleBox.Add(throttleSlotFrame);
            leversRow.Add(throttleBox);

            // ================= 2. 中间：经典机械指针仪表阵列 (4 块高精度机械表盘) =================
            var gaugeDesk = new VisualElement();
            gaugeDesk.style.width = Length.Percent(46);
            gaugeDesk.style.flexDirection = FlexDirection.Row;
            gaugeDesk.style.justifyContent = Justify.SpaceAround;
            gaugeDesk.style.alignItems = Align.Center;
            mainDeskRow.Add(gaugeDesk);

            // (1) 双针风表 (红针: 总风缸 800kPa / 白针: 列车管 500kPa)
            var dualAirGauge = CreateDualAirPressureGauge(out needleMainRes, out needleBrakePipe, out lblMainResPress, out lblBrakePipePress);
            gaugeDesk.Add(dualAirGauge);

            // (2) 方形外壳老式机械速度表 (0 ~ 120 km/h)
            var speedSquareGauge = CreateSpeedGauge(out needleSpeed, out lblSpeed, out lblTotalMileage);
            gaugeDesk.Add(speedSquareGauge);

            // (3) 柴油机转速表 (0 ~ 1200 rpm)
            var rpmGauge = CreatePhysicalGauge("转速表", "RPM", 0, 1200, -120f, 120f, CabPalette.NeedleRed, out needleRpm, out lblRpm, 90);
            gaugeDesk.Add(rpmGauge);

            // (4) 柴油机工况双针表 (水温 & 机油压力)
            var engineStatusGauge = CreateEngineDualStatusGauge(out needleWaterTemp, out needleOilPress, out lblWaterTemp);
            gaugeDesk.Add(engineStatusGauge);

            // ================= 3. 右侧：JZ-7 型空气制动机与行车状态卡 =================
            var rightBrakeDesk = new VisualElement();
            rightBrakeDesk.style.width = Length.Percent(27);
            rightBrakeDesk.style.flexDirection = FlexDirection.Column;
            rightBrakeDesk.style.justifyContent = Justify.SpaceBetween;
            rightBrakeDesk.style.paddingLeft = 8;
            mainDeskRow.Add(rightBrakeDesk);

            // 闭塞与站距行车信息卡
            var infoCard = new VisualElement();
            infoCard.style.backgroundColor = CabPalette.GaugeFaceDark;
            SetBorder(infoCard.style, 1.5f, CabPalette.CabPanelTrim);
            infoCard.style.paddingLeft = 8; infoCard.style.paddingRight = 8;
            infoCard.style.paddingTop = 4; infoCard.style.paddingBottom = 4;
            infoCard.style.borderTopLeftRadius = 4; infoCard.style.borderTopRightRadius = 4;
            infoCard.style.borderBottomLeftRadius = 4; infoCard.style.borderBottomRightRadius = 4;

            lblDistanceToStation = new Label("距松桥站: 1000 m");
            lblDistanceToStation.style.fontSize = 11;
            lblDistanceToStation.style.color = CabPalette.RicePaper;
            lblDistanceToStation.style.unityFontStyleAndWeight = FontStyle.Bold;
            infoCard.Add(lblDistanceToStation);

            lblAlert = new Label("");
            lblAlert.style.fontSize = 10;
            lblAlert.style.color = CabPalette.CrimsonRed;
            infoCard.Add(lblAlert);
            rightBrakeDesk.Add(infoCard);

            // JZ-7 制动大闸操控槽
            var brakeBox = new VisualElement();
            brakeBox.style.alignItems = Align.Center;
            lblBrakeNotch = new Label("JZ-7自阀: 运转位");
            lblBrakeNotch.style.fontSize = 11;
            lblBrakeNotch.style.color = CabPalette.RicePaper;
            brakeBox.Add(lblBrakeNotch);

            var brakeSlotFrame = new VisualElement();
            brakeSlotFrame.style.width = Length.Percent(98);
            brakeSlotFrame.style.height = 68;
            brakeSlotFrame.style.backgroundColor = CabPalette.GaugeFaceDark;
            SetBorder(brakeSlotFrame.style, 1.5f, CabPalette.CabPanelTrim);
            brakeSlotFrame.style.borderTopLeftRadius = 4; brakeSlotFrame.style.borderTopRightRadius = 4;
            brakeSlotFrame.style.borderBottomLeftRadius = 4; brakeSlotFrame.style.borderBottomRightRadius = 4;
            brakeSlotFrame.style.flexDirection = FlexDirection.Row;
            brakeSlotFrame.style.justifyContent = Justify.SpaceAround;
            brakeSlotFrame.style.alignItems = Align.Center;

            var btnBrakeRel = new Button(() => engineState.BrakeReleaseStep()) { text = "缓解" };
            var btnBrakeApp = new Button(() => engineState.BrakeApplyStep()) { text = "制动" };
            var btnBrakeEmg = new Button(() => engineState.SetBrakeNotch(BrakeNotch.Emergency)) { text = "紧急排风" };
            var btnIndepBrake = new Button(() =>
            {
                float next = engineState.IndependentBrakeNotch > 0.1f ? 0f : 1f;
                engineState.SetIndependentBrake(next);
            }) { text = "小闸" };

            StyleMiniLeverBtn(btnBrakeRel); StyleMiniLeverBtn(btnBrakeApp); StyleMiniLeverBtn(btnBrakeEmg); StyleMiniLeverBtn(btnIndepBrake);
            btnBrakeEmg.style.backgroundColor = CabPalette.CrimsonRed;
            btnBrakeEmg.style.color = Color.white;

            brakeSlotFrame.Add(btnBrakeRel);
            brakeSlotFrame.Add(btnBrakeApp);
            brakeSlotFrame.Add(btnIndepBrake);
            brakeSlotFrame.Add(btnBrakeEmg);
            brakeBox.Add(brakeSlotFrame);
            rightBrakeDesk.Add(brakeBox);

            // ================= 4. 底区：一整排象牙白琴键开关 (Piano Switches) =================
            var pianoKeysRow = new VisualElement();
            pianoKeysRow.style.height = 42;
            pianoKeysRow.style.backgroundColor = new Color(0.1f, 0.09f, 0.08f);
            SetBorder(pianoKeysRow.style, 1, CabPalette.CabPanelTrim);
            pianoKeysRow.style.borderTopLeftRadius = 4; pianoKeysRow.style.borderTopRightRadius = 4;
            pianoKeysRow.style.borderBottomLeftRadius = 4; pianoKeysRow.style.borderBottomRightRadius = 4;
            pianoKeysRow.style.flexDirection = FlexDirection.Row;
            pianoKeysRow.style.justifyContent = Justify.SpaceAround;
            pianoKeysRow.style.alignItems = Align.Center;
            pianoKeysRow.style.paddingLeft = 4;
            pianoKeysRow.style.paddingRight = 4;

            // 1. 启机电门 (带黄黑警戒边框)
            var startKey = CreatePianoSwitch("启机点火", CabPalette.BrassPlaqueGold, () =>
            {
                if (!engineState.IsEngineRunning) engineState.StartEngine();
                else engineState.StopEngine();
            });
            SetBorder(startKey.style, 1.5f, CabPalette.AmberGlow);
            pianoKeysRow.Add(startKey);

            // 2. 前照大灯
            keyHeadlight = CreatePianoSwitch("前照大灯", CabPalette.RicePaper, () =>
            {
                if (sceneController != null) sceneController.ToggleHeadlight();
                engineState.HeadlightOn = !engineState.HeadlightOn;
            });
            pianoKeysRow.Add(keyHeadlight);

            // 3. 仪表照明
            keyInstrument = CreatePianoSwitch("仪表背光", CabPalette.RicePaper, () =>
            {
                engineState.InstrumentLightOn = !engineState.InstrumentLightOn;
            });
            pianoKeysRow.Add(keyInstrument);

            // 4. 燃油供给泵
            keyFuelPump = CreatePianoSwitch("燃油泵", CabPalette.RicePaper, () =>
            {
                engineState.FuelPumpOn = !engineState.FuelPumpOn;
            });
            pianoKeysRow.Add(keyFuelPump);

            // 5. 风挡除霜风机
            keyHeater = CreatePianoSwitch("除霜风机", CabPalette.RicePaper, () =>
            {
                if (sceneController != null) sceneController.ToggleWipers();
                engineState.CabHeaterOn = !engineState.CabHeaterOn;
            });
            pianoKeysRow.Add(keyHeater);

            // 6. 轨面撒砂 (防空转)
            keySand = CreatePianoSwitch("轨面撒砂", CabPalette.RicePaper, () =>
            {
                engineState.SandOn = !engineState.SandOn;
            });
            pianoKeysRow.Add(keySand);

            // 7. 车顶汽笛 (鸣笛)
            var hornKey = CreatePianoSwitch("汽笛鸣响", CabPalette.AmberGlow, () =>
            {
                if (sceneController != null) sceneController.TriggerHornShock();
                engineState.TriggerHorn();
            });
            pianoKeysRow.Add(hornKey);

            // 8. 红色大蘑菇头紧急停车拍钮
            var emergencyStopBtn = new Button(() =>
            {
                engineState.SetBrakeNotch(BrakeNotch.Emergency);
                engineState.SetThrottle(0);
                if (sceneController != null) sceneController.TriggerHornShock();
            }) { text = "● 急停拍钮" };
            emergencyStopBtn.style.height = 32;
            emergencyStopBtn.style.paddingLeft = 8; emergencyStopBtn.style.paddingRight = 8;
            emergencyStopBtn.style.fontSize = 10;
            emergencyStopBtn.style.backgroundColor = CabPalette.CrimsonRed;
            emergencyStopBtn.style.color = Color.white;
            emergencyStopBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            SetBorder(emergencyStopBtn.style, 1.5f, Color.white);
            emergencyStopBtn.style.borderTopLeftRadius = 4; emergencyStopBtn.style.borderTopRightRadius = 4;
            emergencyStopBtn.style.borderBottomLeftRadius = 4; emergencyStopBtn.style.borderBottomRightRadius = 4;
            pianoKeysRow.Add(emergencyStopBtn);

            hardcorePanel.Add(pianoKeysRow);
            root.Add(hardcorePanel);
        }

        private VisualElement CreatePianoSwitch(string labelText, Color textColor, System.Action onClick)
        {
            var btn = new Button(onClick) { text = labelText };
            btn.style.height = 32;
            btn.style.paddingLeft = 6;
            btn.style.paddingRight = 6;
            btn.style.fontSize = 10;
            btn.style.backgroundColor = CabPalette.IvoryKeyCap;
            btn.style.color = CabPalette.IronBlack;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            SetBorder(btn.style, 1, CabPalette.CabPanelTrim);
            btn.style.borderTopLeftRadius = 3; btn.style.borderTopRightRadius = 3;
            btn.style.borderBottomLeftRadius = 3; btn.style.borderBottomRightRadius = 3;
            return btn;
        }

        private VisualElement CreatePhysicalGauge(string title, string unit, float minVal, float maxVal, float minAngle, float maxAngle, Color needleColor, out VisualElement needle, out Label valLbl, int size)
        {
            var gauge = new VisualElement();
            gauge.style.width = size;
            gauge.style.height = size;
            gauge.style.backgroundColor = CabPalette.GaugeFaceDark;
            SetBorder(gauge.style, 3, CabPalette.GaugeBezelMetal);
            gauge.style.borderTopLeftRadius = size / 2;
            gauge.style.borderTopRightRadius = size / 2;
            gauge.style.borderBottomLeftRadius = size / 2;
            gauge.style.borderBottomRightRadius = size / 2;
            gauge.style.position = Position.Relative;
            gauge.style.alignItems = Align.Center;
            gauge.style.justifyContent = Justify.Center;
            gauge.style.overflow = Overflow.Hidden;

            // 内部刻度环微光
            var innerRing = new VisualElement();
            innerRing.style.position = Position.Absolute;
            innerRing.style.left = 3; innerRing.style.top = 3; innerRing.style.right = 3; innerRing.style.bottom = 3;
            SetBorder(innerRing.style, 1, new Color(0.24f, 0.24f, 0.26f, 0.8f));
            innerRing.style.borderTopLeftRadius = (size - 6) / 2; innerRing.style.borderTopRightRadius = (size - 6) / 2;
            innerRing.style.borderBottomLeftRadius = (size - 6) / 2; innerRing.style.borderBottomRightRadius = (size - 6) / 2;
            gauge.Add(innerRing);

            var lblTitle = new Label(title) { style = { position = Position.Absolute, top = 10, fontSize = 9, color = CabPalette.RicePaper, unityFontStyleAndWeight = FontStyle.Bold } };
            gauge.Add(lblTitle);

            var lblUnit = new Label(unit) { style = { position = Position.Absolute, top = 22, fontSize = 8, color = CabPalette.IvoryKeyDark } };
            gauge.Add(lblUnit);

            // 刻度范围标记
            var lblMin = new Label(minVal.ToString("F0")) { style = { position = Position.Absolute, left = 8, bottom = 14, fontSize = 8, color = CabPalette.RicePaper } };
            var lblMax = new Label(maxVal.ToString("F0")) { style = { position = Position.Absolute, right = 8, bottom = 14, fontSize = 8, color = CabPalette.RicePaper } };
            gauge.Add(lblMin); gauge.Add(lblMax);

            // 指针 (针尖向上，中心为旋转轴点)
            float needleLen = size * 0.36f;
            needle = new VisualElement();
            needle.style.position = Position.Absolute;
            needle.style.width = 2.5f;
            needle.style.height = needleLen;
            needle.style.left = (size / 2f) - 1.25f;
            needle.style.top = (size / 2f) - needleLen;
            needle.style.backgroundColor = needleColor;
            needle.style.borderTopLeftRadius = 1; needle.style.borderTopRightRadius = 1;
            needle.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));
            needle.style.rotate = new Rotate(minAngle);
            gauge.Add(needle);

            // 表心金属轴盖
            var hub = new VisualElement();
            hub.style.position = Position.Absolute;
            hub.style.width = 12; hub.style.height = 12;
            hub.style.left = (size / 2f) - 6f; hub.style.top = (size / 2f) - 6f;
            hub.style.borderTopLeftRadius = 6; hub.style.borderTopRightRadius = 6;
            hub.style.borderBottomLeftRadius = 6; hub.style.borderBottomRightRadius = 6;
            hub.style.backgroundColor = CabPalette.DarkBronze;
            SetBorder(hub.style, 1, CabPalette.GoldTrim);
            gauge.Add(hub);

            // 底部数字
            valLbl = new Label("0") { style = { position = Position.Absolute, bottom = 4, fontSize = 10, color = CabPalette.AmberGlow, unityFontStyleAndWeight = FontStyle.Bold } };
            gauge.Add(valLbl);

            return gauge;
        }

        /// <summary>
        /// 双针风表 (红针: 总风缸 0~1000 kPa, 白针: 列车管 0~1000 kPa)
        /// </summary>
        private VisualElement CreateDualAirPressureGauge(out VisualElement needleRed, out VisualElement needleWhite, out Label valRedLbl, out Label valWhiteLbl)
        {
            int size = 90;
            var gauge = new VisualElement();
            gauge.style.width = size;
            gauge.style.height = size;
            gauge.style.backgroundColor = CabPalette.GaugeFaceDark;
            SetBorder(gauge.style, 3, CabPalette.GaugeBezelMetal);
            gauge.style.borderTopLeftRadius = size / 2; gauge.style.borderTopRightRadius = size / 2;
            gauge.style.borderBottomLeftRadius = size / 2; gauge.style.borderBottomRightRadius = size / 2;
            gauge.style.position = Position.Relative;
            gauge.style.alignItems = Align.Center;
            gauge.style.justifyContent = Justify.Center;
            gauge.style.overflow = Overflow.Hidden;

            var titleLbl = new Label("双针风表 (kPa)") { style = { position = Position.Absolute, top = 8, fontSize = 9, color = CabPalette.RicePaper, unityFontStyleAndWeight = FontStyle.Bold } };
            gauge.Add(titleLbl);

            float needleLen = size * 0.35f;

            // 红针: 总风缸
            needleRed = new VisualElement();
            needleRed.style.position = Position.Absolute;
            needleRed.style.width = 2.5f; needleRed.style.height = needleLen;
            needleRed.style.left = (size / 2f) - 1.25f; needleRed.style.top = (size / 2f) - needleLen;
            needleRed.style.backgroundColor = CabPalette.NeedleRed;
            needleRed.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));
            needleRed.style.rotate = new Rotate(-120f);
            gauge.Add(needleRed);

            // 白针: 列车管
            needleWhite = new VisualElement();
            needleWhite.style.position = Position.Absolute;
            needleWhite.style.width = 2f; needleWhite.style.height = needleLen * 0.95f;
            needleWhite.style.left = (size / 2f) - 1f; needleWhite.style.top = (size / 2f) - (needleLen * 0.95f);
            needleWhite.style.backgroundColor = CabPalette.NeedleWhite;
            needleWhite.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));
            needleWhite.style.rotate = new Rotate(-120f);
            gauge.Add(needleWhite);

            // 表心轴盖
            var hub = new VisualElement();
            hub.style.position = Position.Absolute;
            hub.style.width = 12; hub.style.height = 12;
            hub.style.left = (size / 2f) - 6f; hub.style.top = (size / 2f) - 6f;
            hub.style.borderTopLeftRadius = 6; hub.style.borderTopRightRadius = 6;
            hub.style.borderBottomLeftRadius = 6; hub.style.borderBottomRightRadius = 6;
            hub.style.backgroundColor = CabPalette.CabPanelTrim;
            SetBorder(hub.style, 1, CabPalette.BoltSilver);
            gauge.Add(hub);

            // 双值标签 (总风 红 / 列车 白)
            var labelsBox = new VisualElement();
            labelsBox.style.position = Position.Absolute;
            labelsBox.style.bottom = 4;
            labelsBox.style.flexDirection = FlexDirection.Row;

            valRedLbl = new Label("820") { style = { fontSize = 9, color = CabPalette.NeedleRed, marginRight = 4, unityFontStyleAndWeight = FontStyle.Bold } };
            valWhiteLbl = new Label("500") { style = { fontSize = 9, color = CabPalette.NeedleWhite, unityFontStyleAndWeight = FontStyle.Bold } };
            labelsBox.Add(valRedLbl); labelsBox.Add(valWhiteLbl);
            gauge.Add(labelsBox);

            return gauge;
        }

        /// <summary>
        /// 方形外框老式机械速度表 (0 ~ 120 km/h + 滚轮数字里程)
        /// </summary>
        private VisualElement CreateSpeedGauge(out VisualElement needle, out Label speedLbl, out Label odoLbl)
        {
            int size = 90;
            var box = new VisualElement();
            box.style.width = size;
            box.style.height = size;
            box.style.backgroundColor = CabPalette.GaugeFaceDark;
            SetBorder(box.style, 3, CabPalette.GaugeBezelMetal);
            box.style.borderTopLeftRadius = 6; box.style.borderTopRightRadius = 6;
            box.style.borderBottomLeftRadius = 6; box.style.borderBottomRightRadius = 6;
            box.style.position = Position.Relative;
            box.style.alignItems = Align.Center;
            box.style.justifyContent = Justify.Center;
            box.style.overflow = Overflow.Hidden;

            var titleLbl = new Label("速度计 KM/H") { style = { position = Position.Absolute, top = 8, fontSize = 9, color = CabPalette.RicePaper, unityFontStyleAndWeight = FontStyle.Bold } };
            box.Add(titleLbl);

            float needleLen = size * 0.35f;
            needle = new VisualElement();
            needle.style.position = Position.Absolute;
            needle.style.width = 2.5f; needle.style.height = needleLen;
            needle.style.left = (size / 2f) - 1.25f; needle.style.top = (size / 2f) - needleLen;
            needle.style.backgroundColor = CabPalette.NeedleAmber;
            needle.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));
            needle.style.rotate = new Rotate(-120f);
            box.Add(needle);

            var hub = new VisualElement();
            hub.style.position = Position.Absolute;
            hub.style.width = 12; hub.style.height = 12;
            hub.style.left = (size / 2f) - 6f; hub.style.top = (size / 2f) - 6f;
            hub.style.borderTopLeftRadius = 6; hub.style.borderTopRightRadius = 6;
            hub.style.borderBottomLeftRadius = 6; hub.style.borderBottomRightRadius = 6;
            hub.style.backgroundColor = CabPalette.DarkBronze;
            SetBorder(hub.style, 1, CabPalette.GoldTrim);
            box.Add(hub);

            // 速度读数与机械里程计
            speedLbl = new Label("0.0") { style = { position = Position.Absolute, bottom = 14, fontSize = 11, color = CabPalette.NeedleAmber, unityFontStyleAndWeight = FontStyle.Bold } };
            odoLbl = new Label("ODO: 0012.4km") { style = { position = Position.Absolute, bottom = 2, fontSize = 7, color = CabPalette.IvoryKeyDark } };
            box.Add(speedLbl); box.Add(odoLbl);

            return box;
        }

        /// <summary>
        /// 柴油机工况双针表 (水温 40~120 ℃ & 机油压力 0~0.8 MPa)
        /// </summary>
        private VisualElement CreateEngineDualStatusGauge(out VisualElement needleTemp, out VisualElement needleOil, out Label tempLbl)
        {
            int size = 90;
            var gauge = new VisualElement();
            gauge.style.width = size;
            gauge.style.height = size;
            gauge.style.backgroundColor = CabPalette.GaugeFaceDark;
            SetBorder(gauge.style, 3, CabPalette.GaugeBezelMetal);
            gauge.style.borderTopLeftRadius = size / 2; gauge.style.borderTopRightRadius = size / 2;
            gauge.style.borderBottomLeftRadius = size / 2; gauge.style.borderBottomRightRadius = size / 2;
            gauge.style.position = Position.Relative;
            gauge.style.alignItems = Align.Center;
            gauge.style.justifyContent = Justify.Center;
            gauge.style.overflow = Overflow.Hidden;

            var titleLbl = new Label("水温 / 油压") { style = { position = Position.Absolute, top = 8, fontSize = 9, color = CabPalette.RicePaper, unityFontStyleAndWeight = FontStyle.Bold } };
            gauge.Add(titleLbl);

            float needleLen = size * 0.35f;

            // 水温针 (红)
            needleTemp = new VisualElement();
            needleTemp.style.position = Position.Absolute;
            needleTemp.style.width = 2.5f; needleTemp.style.height = needleLen;
            needleTemp.style.left = (size / 2f) - 1.25f; needleTemp.style.top = (size / 2f) - needleLen;
            needleTemp.style.backgroundColor = CabPalette.NeedleRed;
            needleTemp.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));
            needleTemp.style.rotate = new Rotate(-80f);
            gauge.Add(needleTemp);

            // 机油针 (黄)
            needleOil = new VisualElement();
            needleOil.style.position = Position.Absolute;
            needleOil.style.width = 2f; needleOil.style.height = needleLen * 0.9f;
            needleOil.style.left = (size / 2f) - 1f; needleOil.style.top = (size / 2f) - (needleLen * 0.9f);
            needleOil.style.backgroundColor = CabPalette.NeedleAmber;
            needleOil.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));
            needleOil.style.rotate = new Rotate(80f);
            gauge.Add(needleOil);

            var hub = new VisualElement();
            hub.style.position = Position.Absolute;
            hub.style.width = 12; hub.style.height = 12;
            hub.style.left = (size / 2f) - 6f; hub.style.top = (size / 2f) - 6f;
            hub.style.borderTopLeftRadius = 6; hub.style.borderTopRightRadius = 6;
            hub.style.borderBottomLeftRadius = 6; hub.style.borderBottomRightRadius = 6;
            hub.style.backgroundColor = CabPalette.DarkBronze;
            SetBorder(hub.style, 1, CabPalette.GoldTrim);
            gauge.Add(hub);

            tempLbl = new Label("75 ℃ | 0.45 MPa") { style = { position = Position.Absolute, bottom = 4, fontSize = 8, color = CabPalette.RicePaper, unityFontStyleAndWeight = FontStyle.Bold } };
            gauge.Add(tempLbl);

            return gauge;
        }

        private void StyleMiniLeverBtn(Button btn)
        {
            btn.style.height = 24;
            btn.style.marginLeft = 2;
            btn.style.marginRight = 2;
            btn.style.paddingLeft = 5;
            btn.style.paddingRight = 5;
            btn.style.fontSize = 10;
            btn.style.backgroundColor = new Color(0.2f, 0.19f, 0.18f);
            btn.style.color = CabPalette.RicePaper;
            SetBorder(btn.style, 1, CabPalette.CabPanelTrim);
            btn.style.borderTopLeftRadius = 3; btn.style.borderTopRightRadius = 3;
            btn.style.borderBottomLeftRadius = 3; btn.style.borderBottomRightRadius = 3;
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
            var titleLbl = new Label("NF-5「耕牛·改」762mm 窄轨内燃机车操纵台 · 3D程序化像素视界")
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
            // 1. 物理机械指针旋转驱动
            if (needleSpeed != null)
            {
                float speedRatio = Mathf.Clamp01(Mathf.Abs(engineState.CurrentSpeedKmh) / 120f);
                needleSpeed.style.rotate = new Rotate(Mathf.Lerp(-120f, 120f, speedRatio));
            }
            if (needleRpm != null)
            {
                float rpmRatio = Mathf.Clamp01(engineState.EngineRpm / 1200f);
                needleRpm.style.rotate = new Rotate(Mathf.Lerp(-120f, 120f, rpmRatio));
            }
            if (needleMainRes != null)
            {
                float mainResRatio = Mathf.Clamp01(engineState.MainReservoirKpa / 1000f);
                needleMainRes.style.rotate = new Rotate(Mathf.Lerp(-120f, 120f, mainResRatio));
            }
            if (needleBrakePipe != null)
            {
                float pipeRatio = Mathf.Clamp01(engineState.BrakePipeKpa / 1000f);
                needleBrakePipe.style.rotate = new Rotate(Mathf.Lerp(-120f, 120f, pipeRatio));
            }
            if (needleWaterTemp != null)
            {
                float tempRatio = Mathf.Clamp01((engineState.WaterTempCelsius - 40f) / 80f);
                needleWaterTemp.style.rotate = new Rotate(Mathf.Lerp(-80f, 0f, tempRatio));
            }
            if (needleOilPress != null)
            {
                float oilRatio = Mathf.Clamp01(engineState.OilPressureMpa / 0.8f);
                needleOilPress.style.rotate = new Rotate(Mathf.Lerp(80f, 0f, oilRatio));
            }

            // 2. 主控提手柄滑块垂直位置 (0 阶在槽底 64px, 8 阶在槽顶 4px)
            if (throttleGripHandle != null)
            {
                float throtT = engineState.ThrottleNotch / 8f;
                throttleGripHandle.style.top = Mathf.Lerp(64f, 4f, throtT);
            }

            // 3. 挡风中柱竖排八显示机车信号机灯箱联动
            UpdateSignalBoxLenses();

            // 4. 文字与读数刷新
            if (lblSpeed != null) lblSpeed.text = engineState.CurrentSpeedKmh.ToString("F1");
            if (lblRpm != null) lblRpm.text = Mathf.RoundToInt(engineState.EngineRpm).ToString();
            if (lblMainResPress != null) lblMainResPress.text = Mathf.RoundToInt(engineState.MainReservoirKpa).ToString();
            if (lblBrakePipePress != null) lblBrakePipePress.text = Mathf.RoundToInt(engineState.BrakePipeKpa).ToString();
            if (lblTotalMileage != null) lblTotalMileage.text = $"ODO: {(engineState.TotalTraveledMeters / 1000f):F1}km";
            if (lblWaterTemp != null) lblWaterTemp.text = $"{engineState.WaterTempCelsius:F0} ℃ | {engineState.OilPressureMpa:F2} MPa";

            if (lblThrottle != null) lblThrottle.text = $"主控: {engineState.ThrottleNotch} 阶";
            if (lblReverser != null) lblReverser.text = $"换向: {engineState.GetReverserString()}";
            if (lblBrakeNotch != null)
            {
                string indepStr = engineState.IndependentBrakeNotch > 0.1f ? " [小闸制动中]" : "";
                lblBrakeNotch.text = $"JZ-7自阀: {engineState.GetBrakeNotchString()}{indepStr}";
            }

            if (lblDistanceToStation != null)
            {
                lblDistanceToStation.text = $"距松桥站: {Mathf.Max(0, Mathf.RoundToInt(engineState.DistanceToStationMeters))} m";
            }

            if (lblAlert != null)
            {
                if (!engineState.IsEngineRunning)
                {
                    lblAlert.text = "【未启机】点击底部启机电门点火";
                }
                else if (engineState.CurrentReverser == ReverserPosition.Neutral && engineState.ThrottleNotch > 0)
                {
                    lblAlert.text = "【警告】换向器中立空转中！";
                }
                else if (engineState.CurrentBrakeNotch >= BrakeNotch.Common && engineState.ThrottleNotch > 0)
                {
                    lblAlert.text = "【警示】严禁带制动牵引！";
                }
                else if (engineState.BrakePos == BrakeNotch.Emergency)
                {
                    lblAlert.text = "【紧急排风】列车管气压排空全列抱死！";
                }
                else if (engineState.SandOn)
                {
                    lblAlert.text = "【撒砂中】轨面喷砂防空转";
                }
                else
                {
                    lblAlert.text = "";
                }
            }

            // 5. 琴键开关选中状态反馈
            SyncPianoKeyVisual(keyHeadlight, engineState.HeadlightOn);
            SyncPianoKeyVisual(keyInstrument, engineState.InstrumentLightOn);
            SyncPianoKeyVisual(keyFuelPump, engineState.FuelPumpOn);
            SyncPianoKeyVisual(keySand, engineState.SandOn);
            SyncPianoKeyVisual(keyHeater, engineState.CabHeaterOn);

            // 6. 更新悬浮 HUD 大字
            if (hudSpeedBig != null) hudSpeedBig.text = engineState.CurrentSpeedKmh.ToString("F1");
            if (hudNotchBig != null) hudNotchBig.text = $"{engineState.ThrottleNotch} 阶";
            if (hudBrakeBig != null) hudBrakeBig.text = engineState.GetBrakeNotchString();
            if (hudPressBig != null) hudPressBig.text = Mathf.RoundToInt(engineState.BrakePipeKpa).ToString();
            if (hudDistanceBig != null) hudDistanceBig.text = $"{Mathf.Max(0, Mathf.RoundToInt(engineState.DistanceToStationMeters))}m";

            // 保持 RenderTexture 图像实时刷新
            if (rtDisplayImage != null && sceneController != null && rtDisplayImage.image != sceneController.TargetTexture)
            {
                rtDisplayImage.image = sceneController.TargetTexture;
            }
        }

        private void SyncPianoKeyVisual(VisualElement key, bool isOn)
        {
            if (key == null) return;
            key.style.backgroundColor = isOn ? CabPalette.IvoryKeyCap : CabPalette.IvoryKeyDark;
            key.style.color = isOn ? CabPalette.IronBlack : new Color(0.2f, 0.2f, 0.25f);
        }

        private void UpdateSignalBoxLenses()
        {
            if (signalLampGreen == null) return;
            var aspect = engineState.CurrentSignalAspect;

            SetLensState(signalLampGreen, aspect == CabSignalAspect.Green, CabPalette.SignalLensGreen);
            SetLensState(signalLampYellow, aspect == CabSignalAspect.Yellow, CabPalette.SignalLensYellow);
            SetLensState(signalLampRedYellow, aspect == CabSignalAspect.RedYellow, new Color(0.95f, 0.55f, 0.1f));
            SetLensState(signalLampRed, aspect == CabSignalAspect.Red, CabPalette.SignalLensRed);
        }

        private void SetLensState(VisualElement lensHolder, bool isActive, Color activeColor)
        {
            var core = lensHolder.Q<VisualElement>("lens-core");
            if (core == null) return;

            if (isActive)
            {
                core.style.backgroundColor = activeColor;
                SetBorder(lensHolder.style, 1.5f, activeColor);
            }
            else
            {
                core.style.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
                SetBorder(lensHolder.style, 1.5f, new Color(0.1f, 0.1f, 0.12f));
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
