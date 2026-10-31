using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    /// <summary>
    /// 小米澎湃OS (HyperOS Pad) 超级岛 / 焦点通知 (Super Island)
    /// 为车载多软件（音乐、列车遥测、录音、倒计时、备份同步、调度警报）提供多端焦点的微胶囊态与大岛展开态。
    /// 遵循高审美、强图形化、极简文字规范。
    /// </summary>
    public class TabletSuperIsland
    {
        public enum IslandAppType
        {
            None,
            Music,         // 车载随身听 / 广播电台
            TrainCab,      // 列车驾驶 / ATO 遥测 / 车速
            Recorder,      // 声音备忘录 / 拾音
            Timer,         // 运行倒计时 / 测时秒表
            CloudSync,     // 本地备份与云端同步
            DispatchAlert  // 铁道调度电报 / 紧急通报
        }

        public class IslandSession
        {
            public IslandAppType AppType;
            public int Priority; // 100: DispatchAlert, 80: TrainCab, 60: Recorder/Timer, 40: CloudSync, 20: Music
            public string Title;
            public string Metric;
            public float Progress; // 0..1
            public Color AccentColor = new Color(0.85f, 0.45f, 0.15f, 1f);
            public Action OnClick;
            public Action OnPrimaryAction;
            public Action OnSecondaryAction;
            public float AuxValue; // e.g. Speed, elapsed seconds, or volume
            public string Subtitle;
        }

        public static TabletSuperIsland Instance { get; private set; }

        private readonly VisualElement _parentContainer;
        private readonly VisualElement _pillAnchor;
        private readonly Action<string> _onLaunchApp;
        private readonly Dictionary<IslandAppType, IslandSession> _activeSessions = new Dictionary<IslandAppType, IslandSession>();

        // 顶层根节点与两态视图
        public VisualElement Root { get; private set; }
        private VisualElement _pillContainer;
        private VisualElement _subBubble;
        private VisualElement _expandedCard;
        private VisualElement _scrimOverlay;

        // 动效模拟心跳
        private int _tickCount = 0;
        private bool _isExpanded = false;
        private IslandAppType _currentPrimaryType = IslandAppType.None;
        private IslandAppType _currentSecondaryType = IslandAppType.None;

        public bool IsExpanded => _isExpanded;
        public IslandAppType PrimaryType => _currentPrimaryType;

        public TabletSuperIsland(VisualElement parentContainer, Action<string> onLaunchApp = null, VisualElement pillAnchor = null)
        {
            _parentContainer = parentContainer;
            _pillAnchor = pillAnchor;
            _onLaunchApp = onLaunchApp;
            Instance = this;

            BuildUI();
        }

        private void BuildUI()
        {
            Root = new VisualElement { name = "super-island-root" };
            Root.style.position = Position.Absolute;
            Root.style.top = 0;
            Root.style.left = 0;
            Root.style.right = 0;
            Root.style.bottom = 0;
            Root.pickingMode = PickingMode.Ignore;

            // 1. 半透明遮罩 (点击收起大岛)
            _scrimOverlay = new VisualElement { name = "super-island-scrim" };
            _scrimOverlay.style.position = Position.Absolute;
            _scrimOverlay.style.top = 0;
            _scrimOverlay.style.left = 0;
            _scrimOverlay.style.right = 0;
            _scrimOverlay.style.bottom = 0;
            _scrimOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.25f);
            _scrimOverlay.style.display = DisplayStyle.None;
            _scrimOverlay.RegisterCallback<ClickEvent>(evt =>
            {
                SetExpanded(false);
                evt.StopPropagation();
            });
            Root.Add(_scrimOverlay);

            // 2. 岛容器（横向排列主胶囊与次要副岛）
            // 遵循小米澎湃OS 平板端（HyperOS Pad）人机工程：位于状态栏左侧，紧随时间右侧
            var islandRow = new VisualElement { name = "super-island-row" };
            islandRow.style.flexDirection = FlexDirection.Row;
            islandRow.style.alignItems = Align.Center;
            islandRow.pickingMode = PickingMode.Ignore;

            if (_pillAnchor != null)
            {
                islandRow.style.marginLeft = 12;
                _pillAnchor.Add(islandRow);
            }
            else
            {
                islandRow.style.position = Position.Absolute;
                islandRow.style.top = 7;
                islandRow.style.left = 84;
                Root.Add(islandRow);
            }

            // 主胶囊 (微胶囊态，高度 32px，圆角 16px)
            _pillContainer = new VisualElement { name = "super-island-pill" };
            _pillContainer.style.height = 32;
            _pillContainer.style.minWidth = 150;
            _pillContainer.style.paddingLeft = 10;
            _pillContainer.style.paddingRight = 12;
            _pillContainer.style.borderTopLeftRadius = 16;
            _pillContainer.style.borderTopRightRadius = 16;
            _pillContainer.style.borderBottomLeftRadius = 16;
            _pillContainer.style.borderBottomRightRadius = 16;
            _pillContainer.style.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 0.94f);
            _pillContainer.style.borderTopWidth = 1;
            _pillContainer.style.borderLeftWidth = 1;
            _pillContainer.style.borderRightWidth = 1;
            _pillContainer.style.borderBottomWidth = 1;
            var borderCol = new Color(1f, 1f, 1f, 0.16f);
            _pillContainer.style.borderTopColor = borderCol;
            _pillContainer.style.borderLeftColor = borderCol;
            _pillContainer.style.borderRightColor = borderCol;
            _pillContainer.style.borderBottomColor = borderCol;
            _pillContainer.style.flexDirection = FlexDirection.Row;
            _pillContainer.style.alignItems = Align.Center;
            _pillContainer.style.justifyContent = Justify.SpaceBetween;
            _pillContainer.pickingMode = PickingMode.Position;
            _pillContainer.style.display = DisplayStyle.None;
            _pillContainer.RegisterCallback<ClickEvent>(evt =>
            {
                ToggleExpanded();
                evt.StopPropagation();
            });
            islandRow.Add(_pillContainer);

            // 次要副岛气泡 (Dual-Island Capsule, 类似 HyperOS 2 / iOS 灵动岛小圆球)
            _subBubble = new VisualElement { name = "super-island-sub-bubble" };
            _subBubble.style.width = 32;
            _subBubble.style.height = 32;
            _subBubble.style.marginLeft = 6;
            _subBubble.style.borderTopLeftRadius = 16;
            _subBubble.style.borderTopRightRadius = 16;
            _subBubble.style.borderBottomLeftRadius = 16;
            _subBubble.style.borderBottomRightRadius = 16;
            _subBubble.style.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 0.94f);
            _subBubble.style.borderTopWidth = 1;
            _subBubble.style.borderLeftWidth = 1;
            _subBubble.style.borderRightWidth = 1;
            _subBubble.style.borderBottomWidth = 1;
            _subBubble.style.borderTopColor = borderCol;
            _subBubble.style.borderLeftColor = borderCol;
            _subBubble.style.borderRightColor = borderCol;
            _subBubble.style.borderBottomColor = borderCol;
            _subBubble.style.alignItems = Align.Center;
            _subBubble.style.justifyContent = Justify.Center;
            _subBubble.pickingMode = PickingMode.Position;
            _subBubble.style.display = DisplayStyle.None;
            _subBubble.RegisterCallback<ClickEvent>(evt =>
            {
                PromoteSubSession();
                evt.StopPropagation();
            });
            islandRow.Add(_subBubble);

            // 3. 大岛展开态卡片 (Expanded Island Card，左侧下沉展开，宽度 390px，悬浮于时间与微胶囊下方)
            _expandedCard = new VisualElement { name = "super-island-expanded" };
            _expandedCard.style.width = 390;
            _expandedCard.style.minHeight = 98;
            _expandedCard.style.position = Position.Absolute;
            _expandedCard.style.top = 46; // 状态栏 48px 正下方
            _expandedCard.style.left = 20; // 与时间左边界对齐
            _expandedCard.style.borderTopLeftRadius = 22;
            _expandedCard.style.borderTopRightRadius = 22;
            _expandedCard.style.borderBottomLeftRadius = 22;
            _expandedCard.style.borderBottomRightRadius = 22;
            _expandedCard.style.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.96f);
            _expandedCard.style.borderTopWidth = 1;
            _expandedCard.style.borderLeftWidth = 1;
            _expandedCard.style.borderRightWidth = 1;
            _expandedCard.style.borderBottomWidth = 1;
            var expBorder = new Color(1f, 1f, 1f, 0.20f);
            _expandedCard.style.borderTopColor = expBorder;
            _expandedCard.style.borderLeftColor = expBorder;
            _expandedCard.style.borderRightColor = expBorder;
            _expandedCard.style.borderBottomColor = expBorder;
            _expandedCard.style.paddingLeft = 16;
            _expandedCard.style.paddingRight = 16;
            _expandedCard.style.paddingTop = 12;
            _expandedCard.style.paddingBottom = 12;
            _expandedCard.pickingMode = PickingMode.Position;
            _expandedCard.style.display = DisplayStyle.None;
            _expandedCard.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            Root.Add(_expandedCard);

            _parentContainer.Add(Root);
        }

        public void RegisterSession(IslandSession session)
        {
            if (session == null || session.AppType == IslandAppType.None) return;
            _activeSessions[session.AppType] = session;
            EvaluateActiveSessions();
        }

        public void UpdateSession(IslandAppType type, Action<IslandSession> update)
        {
            if (_activeSessions.TryGetValue(type, out var session))
            {
                update?.Invoke(session);
                EvaluateActiveSessions();
            }
        }

        public void RemoveSession(IslandAppType type)
        {
            if (_activeSessions.Remove(type))
            {
                EvaluateActiveSessions();
            }
        }

        public void ClearAllSessions()
        {
            _activeSessions.Clear();
            EvaluateActiveSessions();
        }

        public void SetExpanded(bool expanded)
        {
            _isExpanded = expanded && (_currentPrimaryType != IslandAppType.None);
            _scrimOverlay.style.display = _isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            _expandedCard.style.display = _isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            _pillContainer.style.opacity = _isExpanded ? 0f : 1f;
            _pillContainer.pickingMode = _isExpanded ? PickingMode.Ignore : PickingMode.Position;

            if (_isExpanded)
            {
                RenderExpandedContent();
            }
        }

        public void ToggleExpanded()
        {
            SetExpanded(!_isExpanded);
        }

        public void Tick()
        {
            _tickCount++;
            if (_activeSessions.Count == 0) return;

            // 周期刷新动画状态（声波跳动、录音红点闪烁、旋转环等）
            if (_tickCount % 4 == 0)
            {
                if (!_isExpanded)
                {
                    RenderCompactContent();
                }
                else
                {
                    RenderExpandedContent();
                }
            }
        }

        private void EvaluateActiveSessions()
        {
            if (_activeSessions.Count == 0)
            {
                _currentPrimaryType = IslandAppType.None;
                _currentSecondaryType = IslandAppType.None;
                _pillContainer.style.display = DisplayStyle.None;
                _subBubble.style.display = DisplayStyle.None;
                SetExpanded(false);
                return;
            }

            // 按优先级排序：最高者为 Primary，次高者为 Secondary
            var list = new List<IslandSession>(_activeSessions.Values);
            list.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            _currentPrimaryType = list[0].AppType;
            _currentSecondaryType = list.Count > 1 ? list[1].AppType : IslandAppType.None;

            _pillContainer.style.display = DisplayStyle.Flex;
            _subBubble.style.display = _currentSecondaryType != IslandAppType.None && !_isExpanded ? DisplayStyle.Flex : DisplayStyle.None;

            if (_isExpanded)
            {
                RenderExpandedContent();
            }
            else
            {
                RenderCompactContent();
                RenderSubBubble();
            }
        }

        private void PromoteSubSession()
        {
            if (_currentSecondaryType == IslandAppType.None) return;
            if (_activeSessions.TryGetValue(_currentSecondaryType, out var sub))
            {
                // 暂时提升其优先级
                sub.Priority += 200;
                EvaluateActiveSessions();
            }
        }

        // ==================== 微胶囊态渲染 (Compact Pill) ====================
        private void RenderCompactContent()
        {
            if (!_activeSessions.TryGetValue(_currentPrimaryType, out var s)) return;
            _pillContainer.Clear();

            var leftBox = new VisualElement();
            leftBox.style.flexDirection = FlexDirection.Row;
            leftBox.style.alignItems = Align.Center;
            _pillContainer.Add(leftBox);

            var rightBox = new VisualElement();
            rightBox.style.flexDirection = FlexDirection.Row;
            rightBox.style.alignItems = Align.Center;
            _pillContainer.Add(rightBox);

            switch (s.AppType)
            {
                case IslandAppType.Music:
                    // 🎵 旋转唱片徽标 + 3 条跳动音柱 + 极简曲名
                    var disc = CreateMicroDisc(s.AccentColor);
                    leftBox.Add(disc);

                    var eq = CreateMicroEqualizer(s.AccentColor);
                    eq.style.marginLeft = 6;
                    leftBox.Add(eq);

                    var songLbl = CreatePillLabel(s.Title ?? "正在播放", 12, true);
                    songLbl.style.maxWidth = 110;
                    songLbl.style.marginLeft = 6;
                    rightBox.Add(songLbl);
                    break;

                case IslandAppType.TrainCab:
                    // 🚄 机车头 + 信号绿点 + 实时车速胶囊 (e.g. 78 km/h)
                    var locoIcon = CreatePillLabel("🚂", 13, false);
                    locoIcon.style.marginRight = 4;
                    leftBox.Add(locoIcon);

                    var sigDot = CreateSignalDot(s.Progress > 0.9f ? Color.yellow : new Color(0.2f, 0.8f, 0.35f, 1f));
                    leftBox.Add(sigDot);

                    var speedLbl = CreatePillLabel(s.Metric ?? $"{s.AuxValue:F0} km/h", 12, true);
                    speedLbl.style.color = new Color(0.96f, 0.72f, 0.20f, 1f);
                    speedLbl.style.marginLeft = 6;
                    leftBox.Add(speedLbl);

                    var statusBadge = CreatePillLabel(s.Subtitle ?? "ATO巡航", 11, false);
                    statusBadge.style.color = new Color(0.70f, 0.75f, 0.85f, 1f);
                    statusBadge.style.marginLeft = 6;
                    rightBox.Add(statusBadge);
                    break;

                case IslandAppType.Recorder:
                    // 🔴 呼吸红点 + 计时 (01:23) + 拾音振幅条
                    var recDot = CreateSignalDot(Color.red, pulsing: true);
                    leftBox.Add(recDot);

                    var recWave = CreateMicroEqualizer(Color.red);
                    recWave.style.marginLeft = 6;
                    leftBox.Add(recWave);

                    var recTime = CreatePillLabel(s.Metric ?? "00:00", 12, true);
                    recTime.style.marginLeft = 6;
                    rightBox.Add(recTime);
                    break;

                case IslandAppType.Timer:
                    // ⏳ 沙漏 + 倒计时
                    var timerIc = CreatePillLabel("⏳", 12, false);
                    timerIc.style.marginRight = 4;
                    leftBox.Add(timerIc);

                    var timerMetric = CreatePillLabel(s.Metric ?? "00:00", 12, true);
                    timerMetric.style.color = new Color(0.35f, 0.75f, 1f, 1f);
                    leftBox.Add(timerMetric);

                    var timerSub = CreatePillLabel("计时中", 11, false);
                    timerSub.style.marginLeft = 6;
                    rightBox.Add(timerSub);
                    break;

                case IslandAppType.CloudSync:
                    // ☁️ 云同步 + 进度百分比
                    var syncIc = CreatePillLabel("☁", 12, false);
                    syncIc.style.marginRight = 4;
                    leftBox.Add(syncIc);

                    var syncBar = CreateMicroProgressBar(s.Progress, s.AccentColor);
                    leftBox.Add(syncBar);

                    var syncPct = CreatePillLabel(s.Metric ?? $"{(s.Progress * 100):F0}%", 12, true);
                    syncPct.style.marginLeft = 6;
                    rightBox.Add(syncPct);
                    break;

                case IslandAppType.DispatchAlert:
                    // ⚠️ 调度闪烁红黄警标 + 指令简报
                    var alertIc = CreatePillLabel("⚠️", 13, false);
                    alertIc.style.marginRight = 4;
                    leftBox.Add(alertIc);

                    var alertTitle = CreatePillLabel(s.Title ?? "紧急调度", 12, true);
                    alertTitle.style.color = new Color(1f, 0.35f, 0.25f, 1f);
                    leftBox.Add(alertTitle);

                    var alertMetric = CreatePillLabel(s.Metric ?? "限速命令", 11, false);
                    alertMetric.style.marginLeft = 6;
                    rightBox.Add(alertMetric);
                    break;
            }
        }

        // ==================== 次要副岛小气泡渲染 ====================
        private void RenderSubBubble()
        {
            if (!_activeSessions.TryGetValue(_currentSecondaryType, out var s))
            {
                _subBubble.style.display = DisplayStyle.None;
                return;
            }
            _subBubble.style.display = DisplayStyle.Flex;
            _subBubble.Clear();

            switch (s.AppType)
            {
                case IslandAppType.Music:
                    _subBubble.Add(CreateMicroDisc(s.AccentColor, 18));
                    break;
                case IslandAppType.TrainCab:
                    _subBubble.Add(CreatePillLabel("🚂", 12, false));
                    break;
                case IslandAppType.Recorder:
                    _subBubble.Add(CreateSignalDot(Color.red, pulsing: true));
                    break;
                case IslandAppType.Timer:
                    _subBubble.Add(CreatePillLabel("⏱", 12, false));
                    break;
                case IslandAppType.CloudSync:
                    _subBubble.Add(CreatePillLabel("☁", 11, false));
                    break;
                default:
                    _subBubble.Add(CreatePillLabel("●", 10, false));
                    break;
            }
        }

        // ==================== 大岛展开态渲染 (Expanded Island Card) ====================
        private void RenderExpandedContent()
        {
            if (!_activeSessions.TryGetValue(_currentPrimaryType, out var s)) return;
            _expandedCard.Clear();

            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.justifyContent = Justify.SpaceBetween;
            topRow.style.alignItems = Align.Center;
            topRow.style.marginBottom = 8;
            _expandedCard.Add(topRow);

            var closeBtn = new Button(() => SetExpanded(false)) { text = "✕" };
            closeBtn.style.width = 24; closeBtn.style.height = 24;
            closeBtn.style.borderTopLeftRadius = 12; closeBtn.style.borderTopRightRadius = 12;
            closeBtn.style.borderBottomLeftRadius = 12; closeBtn.style.borderBottomRightRadius = 12;
            closeBtn.style.backgroundColor = new Color(1f, 1f, 1f, 0.12f);
            closeBtn.style.color = Color.white;
            closeBtn.style.fontSize = 11;
            closeBtn.style.borderTopWidth = 0; closeBtn.style.borderBottomWidth = 0;
            closeBtn.style.borderLeftWidth = 0; closeBtn.style.borderRightWidth = 0;

            switch (s.AppType)
            {
                // ---------- 1. 音乐大岛 ----------
                case IslandAppType.Music:
                    var musicHeader = CreatePillLabel("车载随身听 · 正在播放", 11, false);
                    musicHeader.style.color = new Color(0.7f, 0.7f, 0.75f, 1f);
                    topRow.Add(musicHeader);
                    topRow.Add(closeBtn);

                    var musicBody = new VisualElement();
                    musicBody.style.flexDirection = FlexDirection.Row;
                    musicBody.style.alignItems = Align.Center;
                    musicBody.style.justifyContent = Justify.SpaceBetween;
                    _expandedCard.Add(musicBody);

                    // 唱片徽标 + 歌名/歌手
                    var leftMusic = new VisualElement();
                    leftMusic.style.flexDirection = FlexDirection.Row;
                    leftMusic.style.alignItems = Align.Center;
                    leftMusic.style.flexGrow = 1;
                    musicBody.Add(leftMusic);

                    var discLarge = CreateMicroDisc(s.AccentColor, 44);
                    discLarge.style.marginRight = 10;
                    leftMusic.Add(discLarge);

                    var musicText = new VisualElement();
                    leftMusic.Add(musicText);

                    var titleLbl = CreatePillLabel(s.Title ?? "铁道原声", 14, true);
                    musicText.Add(titleLbl);

                    var subLbl = CreatePillLabel(s.Subtitle ?? "高品质车厢广播音频", 11, false);
                    subLbl.style.color = new Color(0.6f, 0.65f, 0.75f, 1f);
                    subLbl.style.marginTop = 2;
                    musicText.Add(subLbl);

                    // 播控胶囊按键
                    var controls = new VisualElement();
                    controls.style.flexDirection = FlexDirection.Row;
                    controls.style.alignItems = Align.Center;
                    musicBody.Add(controls);

                    var prevBtn = CreateIconActionBtn("⏮", () => s.OnSecondaryAction?.Invoke());
                    var playBtn = CreateIconActionBtn("⏯", () => s.OnPrimaryAction?.Invoke(), isAccent: true);
                    playBtn.style.width = 36; playBtn.style.height = 36;
                    playBtn.style.marginLeft = 6; playBtn.style.marginRight = 6;
                    var appBtn = CreateIconActionBtn("↗", () => { SetExpanded(false); _onLaunchApp?.Invoke("音乐"); });

                    controls.Add(prevBtn);
                    controls.Add(playBtn);
                    controls.Add(appBtn);
                    break;

                // ---------- 2. 列控遥测大岛 ----------
                case IslandAppType.TrainCab:
                    var cabHeader = CreatePillLabel("列控遥测系统 · NF-5K 机车", 11, false);
                    cabHeader.style.color = new Color(0.7f, 0.7f, 0.75f, 1f);
                    topRow.Add(cabHeader);
                    topRow.Add(closeBtn);

                    var cabBody = new VisualElement();
                    cabBody.style.flexDirection = FlexDirection.Row;
                    cabBody.style.alignItems = Align.Center;
                    cabBody.style.justifyContent = Justify.SpaceBetween;
                    _expandedCard.Add(cabBody);

                    var speedBox = new VisualElement();
                    speedBox.style.alignItems = Align.FlexStart;
                    cabBody.Add(speedBox);

                    var spdLbl = CreatePillLabel(s.Metric ?? $"{s.AuxValue:F1} km/h", 22, true);
                    spdLbl.style.color = new Color(0.96f, 0.72f, 0.20f, 1f);
                    speedBox.Add(spdLbl);

                    var subSpd = CreatePillLabel(s.Subtitle ?? "限速 80 km/h · 自动巡航中", 11, false);
                    subSpd.style.color = new Color(0.65f, 0.70f, 0.80f, 1f);
                    speedBox.Add(subSpd);

                    var cabActions = new VisualElement();
                    cabActions.style.flexDirection = FlexDirection.Row;
                    cabActions.style.alignItems = Align.Center;
                    cabBody.Add(cabActions);

                    var brakeBtn = CreateTextActionBtn("紧急制动", new Color(0.85f, 0.22f, 0.22f, 1f), () => s.OnPrimaryAction?.Invoke());
                    brakeBtn.style.marginRight = 8;
                    cabActions.Add(brakeBtn);

                    var rdaBtn = CreateTextActionBtn("驾驶室", AccentSystem, () => { SetExpanded(false); _onLaunchApp?.Invoke("归途"); });
                    cabActions.Add(rdaBtn);
                    break;

                // ---------- 3. 录音大岛 ----------
                case IslandAppType.Recorder:
                    var recHeader = CreatePillLabel("车载声音备忘 · 拾音录制中", 11, false);
                    recHeader.style.color = Color.red;
                    topRow.Add(recHeader);
                    topRow.Add(closeBtn);

                    var recBody = new VisualElement();
                    recBody.style.flexDirection = FlexDirection.Row;
                    recBody.style.alignItems = Align.Center;
                    recBody.style.justifyContent = Justify.SpaceBetween;
                    _expandedCard.Add(recBody);

                    var recInfo = new VisualElement();
                    recBody.Add(recInfo);

                    var durLbl = CreatePillLabel(s.Metric ?? "00:00", 22, true);
                    durLbl.style.color = Color.white;
                    recInfo.Add(durLbl);

                    var subRec = CreatePillLabel(s.Subtitle ?? "PCM 48kHz 立体声", 11, false);
                    subRec.style.color = new Color(0.65f, 0.70f, 0.80f, 1f);
                    recInfo.Add(subRec);

                    var recActions = new VisualElement();
                    recActions.style.flexDirection = FlexDirection.Row;
                    recActions.style.alignItems = Align.Center;
                    recBody.Add(recActions);

                    var flagBtn = CreateTextActionBtn("标记", new Color(1f, 1f, 1f, 0.15f), () => s.OnSecondaryAction?.Invoke());
                    flagBtn.style.marginRight = 8;
                    recActions.Add(flagBtn);

                    var stopBtn = CreateTextActionBtn("保存", Color.red, () => s.OnPrimaryAction?.Invoke());
                    recActions.Add(stopBtn);
                    break;

                // ---------- 4. 倒计时大岛 ----------
                case IslandAppType.Timer:
                    var timerHeader = CreatePillLabel("运行区间测时秒表", 11, false);
                    timerHeader.style.color = new Color(0.35f, 0.75f, 1f, 1f);
                    topRow.Add(timerHeader);
                    topRow.Add(closeBtn);

                    var timerBody = new VisualElement();
                    timerBody.style.flexDirection = FlexDirection.Row;
                    timerBody.style.alignItems = Align.Center;
                    timerBody.style.justifyContent = Justify.SpaceBetween;
                    _expandedCard.Add(timerBody);

                    var tInfo = new VisualElement();
                    timerBody.Add(tInfo);

                    var tVal = CreatePillLabel(s.Metric ?? "00:00", 22, true);
                    tVal.style.color = new Color(0.35f, 0.75f, 1f, 1f);
                    tInfo.Add(tVal);

                    var tSub = CreatePillLabel(s.Subtitle ?? "区间行车时分测定", 11, false);
                    tSub.style.color = new Color(0.65f, 0.70f, 0.80f, 1f);
                    tInfo.Add(tSub);

                    var tActions = new VisualElement();
                    tActions.style.flexDirection = FlexDirection.Row;
                    tActions.style.alignItems = Align.Center;
                    timerBody.Add(tActions);

                    var rstBtn = CreateTextActionBtn("复位", new Color(1f, 1f, 1f, 0.15f), () => s.OnSecondaryAction?.Invoke());
                    rstBtn.style.marginRight = 8;
                    tActions.Add(rstBtn);

                    var pauseBtn = CreateTextActionBtn("暂停", new Color(0.20f, 0.65f, 0.95f, 1f), () => s.OnPrimaryAction?.Invoke());
                    tActions.Add(pauseBtn);
                    break;

                // ---------- 5. 云同步大岛 ----------
                case IslandAppType.CloudSync:
                    var syncHeader = CreatePillLabel("系统备份与恢复 · 传输中", 11, false);
                    syncHeader.style.color = new Color(0.4f, 0.75f, 0.95f, 1f);
                    topRow.Add(syncHeader);
                    topRow.Add(closeBtn);

                    var syncBody = new VisualElement();
                    _expandedCard.Add(syncBody);

                    var rowProgress = new VisualElement();
                    rowProgress.style.flexDirection = FlexDirection.Row;
                    rowProgress.style.justifyContent = Justify.SpaceBetween;
                    rowProgress.style.marginBottom = 6;
                    syncBody.Add(rowProgress);

                    var fileLbl = CreatePillLabel(s.Title ?? "SUIYUE_Backup.json", 13, true);
                    rowProgress.Add(fileLbl);

                    var pctLbl = CreatePillLabel(s.Metric ?? $"{(s.Progress * 100):F0}%", 13, true);
                    pctLbl.style.color = s.AccentColor;
                    rowProgress.Add(pctLbl);

                    var fullBar = CreateMicroProgressBar(s.Progress, s.AccentColor, width: 350, height: 6);
                    syncBody.Add(fullBar);
                    break;

                // ---------- 6. 调度警报大岛 ----------
                case IslandAppType.DispatchAlert:
                    var alHeader = CreatePillLabel("⚠️ 铁道调度紧急令书", 11, true);
                    alHeader.style.color = new Color(1f, 0.35f, 0.25f, 1f);
                    topRow.Add(alHeader);
                    topRow.Add(closeBtn);

                    var alBody = new VisualElement();
                    alBody.style.flexDirection = FlexDirection.Row;
                    alBody.style.alignItems = Align.Center;
                    alBody.style.justifyContent = Justify.SpaceBetween;
                    _expandedCard.Add(alBody);

                    var alText = new VisualElement();
                    alText.style.flexGrow = 1;
                    alBody.Add(alText);

                    var alTitle = CreatePillLabel(s.Title ?? "前方区间临时限速", 14, true);
                    alText.Add(alTitle);

                    var alDesc = CreatePillLabel(s.Subtitle ?? "沙暴扬沙警告，严格控制在45km/h以下", 11, false);
                    alDesc.style.color = new Color(0.85f, 0.85f, 0.88f, 1f);
                    alDesc.style.marginTop = 2;
                    alText.Add(alDesc);

                    var alActions = new VisualElement();
                    alActions.style.flexDirection = FlexDirection.Row;
                    alActions.style.alignItems = Align.Center;
                    alBody.Add(alActions);

                    var ackBtn = CreateTextActionBtn("已知晓", new Color(0.25f, 0.55f, 0.35f, 1f), () =>
                    {
                        s.OnPrimaryAction?.Invoke();
                        SetExpanded(false);
                    });
                    alActions.Add(ackBtn);
                    break;
            }
        }

        // ==================== 微型图形组件 Helpers ====================
        private VisualElement CreateMicroDisc(Color accent, float size = 20)
        {
            var disc = new VisualElement();
            disc.style.width = size;
            disc.style.height = size;
            disc.style.borderTopLeftRadius = size * 0.5f;
            disc.style.borderTopRightRadius = size * 0.5f;
            disc.style.borderBottomLeftRadius = size * 0.5f;
            disc.style.borderBottomRightRadius = size * 0.5f;
            disc.style.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);
            disc.style.borderTopWidth = 1; disc.style.borderBottomWidth = 1;
            disc.style.borderLeftWidth = 1; disc.style.borderRightWidth = 1;
            var bc = new Color(0.3f, 0.3f, 0.35f, 0.8f);
            disc.style.borderTopColor = bc; disc.style.borderBottomColor = bc;
            disc.style.borderLeftColor = bc; disc.style.borderRightColor = bc;
            disc.style.alignItems = Align.Center;
            disc.style.justifyContent = Justify.Center;

            // 中心彩色轴心
            var core = new VisualElement();
            core.style.width = size * 0.45f;
            core.style.height = size * 0.45f;
            core.style.borderTopLeftRadius = size * 0.225f;
            core.style.borderTopRightRadius = size * 0.225f;
            core.style.borderBottomLeftRadius = size * 0.225f;
            core.style.borderBottomRightRadius = size * 0.225f;
            core.style.backgroundColor = accent;
            disc.Add(core);

            return disc;
        }

        private VisualElement CreateMicroEqualizer(Color barColor)
        {
            var box = new VisualElement();
            box.style.flexDirection = FlexDirection.Row;
            box.style.alignItems = Align.FlexEnd;
            box.style.height = 12;

            int[] heights = { 4 + (_tickCount % 3) * 3, 9 - ((_tickCount + 1) % 4) * 2, 6 + ((_tickCount + 2) % 3) * 2 };
            for (int i = 0; i < 3; i++)
            {
                var bar = new VisualElement();
                bar.style.width = 2;
                bar.style.height = Mathf.Clamp(heights[i], 3, 11);
                bar.style.backgroundColor = barColor;
                bar.style.borderTopLeftRadius = 1;
                bar.style.borderTopRightRadius = 1;
                bar.style.marginRight = 1;
                box.Add(bar);
            }
            return box;
        }

        private VisualElement CreateSignalDot(Color dotColor, bool pulsing = false)
        {
            var dot = new VisualElement();
            dot.style.width = 7;
            dot.style.height = 7;
            dot.style.borderTopLeftRadius = 3.5f;
            dot.style.borderTopRightRadius = 3.5f;
            dot.style.borderBottomLeftRadius = 3.5f;
            dot.style.borderBottomRightRadius = 3.5f;
            dot.style.backgroundColor = dotColor;
            if (pulsing && (_tickCount % 8 < 4))
            {
                dot.style.opacity = 0.3f;
            }
            return dot;
        }

        private VisualElement CreateMicroProgressBar(float progress, Color fillCol, float width = 50, float height = 4)
        {
            var track = new VisualElement();
            track.style.width = width;
            track.style.height = height;
            track.style.backgroundColor = new Color(1f, 1f, 1f, 0.15f);
            track.style.borderTopLeftRadius = height * 0.5f;
            track.style.borderTopRightRadius = height * 0.5f;
            track.style.borderBottomLeftRadius = height * 0.5f;
            track.style.borderBottomRightRadius = height * 0.5f;
            track.style.overflow = Overflow.Hidden;

            var fill = new VisualElement();
            fill.style.width = Length.Percent(Mathf.Clamp01(progress) * 100f);
            fill.style.height = Length.Percent(100);
            fill.style.backgroundColor = fillCol;
            track.Add(fill);

            return track;
        }

        private Label CreatePillLabel(string text, int fontSize, bool bold)
        {
            var l = new Label(text);
            l.style.fontSize = fontSize;
            l.style.color = Color.white;
            if (bold) l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.unityFontDefinition = Fd();
            return l;
        }

        private Button CreateIconActionBtn(string icon, Action onClick, bool isAccent = false)
        {
            var btn = new Button(onClick) { text = icon };
            btn.style.width = 30;
            btn.style.height = 30;
            btn.style.borderTopLeftRadius = 15;
            btn.style.borderTopRightRadius = 15;
            btn.style.borderBottomLeftRadius = 15;
            btn.style.borderBottomRightRadius = 15;
            btn.style.backgroundColor = isAccent ? AccentSystem : new Color(1f, 1f, 1f, 0.12f);
            btn.style.color = Color.white;
            btn.style.fontSize = 12;
            btn.style.borderTopWidth = 0; btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0; btn.style.borderRightWidth = 0;
            btn.style.alignItems = Align.Center;
            btn.style.justifyContent = Justify.Center;
            btn.style.unityFontDefinition = Fd();
            return btn;
        }

        private Button CreateTextActionBtn(string text, Color bg, Action onClick)
        {
            var btn = new Button(onClick) { text = text };
            btn.style.height = 32;
            btn.style.paddingLeft = 12;
            btn.style.paddingRight = 12;
            btn.style.borderTopLeftRadius = 16;
            btn.style.borderTopRightRadius = 16;
            btn.style.borderBottomLeftRadius = 16;
            btn.style.borderBottomRightRadius = 16;
            btn.style.backgroundColor = bg;
            btn.style.color = Color.white;
            btn.style.fontSize = 12;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.borderTopWidth = 0; btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0; btn.style.borderRightWidth = 0;
            btn.style.unityFontDefinition = Fd();
            return btn;
        }
    }
}
