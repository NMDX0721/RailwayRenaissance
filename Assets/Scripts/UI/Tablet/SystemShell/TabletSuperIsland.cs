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
            DispatchAlert, // 铁道调度电报 / 紧急通报
            Custom         // 其他应用：用 IslandSession.Key + Icon + Actions 自行登记，走通用模板
        }

        public enum IslandActionStyle { Neutral, Primary, Danger }

        /// <summary>通用大岛上的一个按钮。Icon 不为空时画成圆形图标按钮，否则是文字胶囊。</summary>
        public class IslandAction
        {
            public string Label;
            public TabletIconKind? Icon;
            public IslandActionStyle Style;
            public Action OnClick;
            /// <summary>点击后是否收起大岛。</summary>
            public bool CloseOnClick;
        }

        public class IslandSession
        {
            public IslandAppType AppType;
            public int Priority; // 100: DispatchAlert, 80: TrainCab, 60: Recorder/Timer, 40: CloudSync, 20: Music
            public string Title;
            public string Metric;
            public float Progress; // 0..1
            public Color AccentColor = TabletTheme.Accent;
            public Action OnClick;
            public Action OnPrimaryAction;
            public Action OnSecondaryAction;
            public float AuxValue; // e.g. Speed, elapsed seconds, or volume
            public string Subtitle;

            // ---- 通用（Custom）会话字段；预设类型也可以用 Key 同时登记多个会话 ----
            /// <summary>会话唯一键；为空则用 AppType 名称。Custom 类型必须填写。</summary>
            public string Key;
            public TabletIconKind Icon = TabletIconKind.Bell;
            /// <summary>大岛顶部小标题，如“录音机 · 录制中”。</summary>
            public string Header;
            public readonly List<IslandAction> Actions = new List<IslandAction>();

            public string EffectiveKey => string.IsNullOrEmpty(Key) ? AppType.ToString() : Key;

            public IslandSession WithAction(string label, Action onClick, IslandActionStyle style = IslandActionStyle.Neutral, bool closeOnClick = false)
            {
                if (Actions.Count < 3) Actions.Add(new IslandAction { Label = label, OnClick = onClick, Style = style, CloseOnClick = closeOnClick });
                return this;
            }

            public IslandSession WithIconAction(TabletIconKind icon, Action onClick, IslandActionStyle style = IslandActionStyle.Neutral)
            {
                if (Actions.Count < 3) Actions.Add(new IslandAction { Icon = icon, OnClick = onClick, Style = style });
                return this;
            }
        }

        public static TabletSuperIsland Instance { get; private set; }

        private readonly VisualElement _parentContainer;
        private readonly VisualElement _pillAnchor;
        private readonly Action<string> _onLaunchApp;
        private readonly Dictionary<string, IslandSession> _activeSessions = new Dictionary<string, IslandSession>();

        // 顶层根节点与两态视图
        public VisualElement Root { get; private set; }
        private VisualElement _pillContainer;
        private VisualElement _subBubble;
        private VisualElement _expandedCard;
        private VisualElement _expandedHandle;
        private VisualElement _expandedGrab;
        private bool _morphing;
        private int _morphToken;

        /// <summary>把手下滑越过阈值时触发：返回 true 表示已处理（岛随后收起）；返回 false 则卡片弹回。</summary>
        public Func<IslandSession, bool> FloatRequested;
        private VisualElement _scrimOverlay;

        // 动效模拟心跳
        private int _tickCount = 0;
        private bool _isExpanded = false;
        private IslandAppType _currentPrimaryType = IslandAppType.None;
        private IslandAppType _currentSecondaryType = IslandAppType.None;
        private string _primaryKey = "";
        private string _secondaryKey = "";

        public bool IsExpanded => _isExpanded;

        /// <summary>胶囊至少和它顶替的日期一样宽，替换时前后长度协调。</summary>
        public void SetPillMinWidth(float width)
        {
            if (width < 60f) return;
            _pillContainer.style.minWidth = Mathf.Min(width, 220f);
        }

        /// <summary>岛的显示状态变了（有/无会话、展开/收起）：状态栏据此决定日期是否被岛盖住。</summary>
        public event Action VisualStateChanged;
        public IslandAppType PrimaryType => _currentPrimaryType;
        public string PrimaryKey => _primaryKey;

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
            _scrimOverlay.style.backgroundColor = TabletTheme.IslandScrim;
            _scrimOverlay.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity") };
            _scrimOverlay.style.transitionDuration = new List<TimeValue> { new TimeValue(200, TimeUnit.Millisecond) };
            _scrimOverlay.style.opacity = 0f;
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
                // 岛占的是日期的位置：绝对定位叠在日期槽上，不挤占别的元素；日期槽自己的宽度不变，岛收放时旁边不会跳
                islandRow.style.position = Position.Absolute;
                islandRow.style.left = 0; islandRow.style.top = 0; islandRow.style.bottom = 0;
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
            _pillContainer.style.height = 28;
            _pillContainer.style.minWidth = 84;
            _pillContainer.style.paddingLeft = 9;
            _pillContainer.style.paddingRight = 11;
            _pillContainer.style.borderTopLeftRadius = 14;
            _pillContainer.style.borderTopRightRadius = 14;
            _pillContainer.style.borderBottomLeftRadius = 14;
            _pillContainer.style.borderBottomRightRadius = 14;
            _pillContainer.style.backgroundColor = TabletTheme.IslandBlack;   // 参考 HyperOS：纯黑胶囊
            _pillContainer.style.borderTopWidth = 1;
            _pillContainer.style.borderLeftWidth = 1;
            _pillContainer.style.borderRightWidth = 1;
            _pillContainer.style.borderBottomWidth = 1;
            var borderCol = TabletTheme.OverlaySubtle;
            _pillContainer.style.borderTopColor = borderCol;
            _pillContainer.style.borderLeftColor = borderCol;
            _pillContainer.style.borderRightColor = borderCol;
            _pillContainer.style.borderBottomColor = borderCol;
            _pillContainer.style.flexDirection = FlexDirection.Row;
            _pillContainer.style.alignItems = Align.Center;
            _pillContainer.style.justifyContent = Justify.SpaceBetween;
            _pillContainer.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity"), new StylePropertyName("scale") };
            _pillContainer.style.transitionDuration = new List<TimeValue> { new TimeValue(180, TimeUnit.Millisecond) };
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
            _subBubble.style.width = 28;
            _subBubble.style.height = 28;
            _subBubble.style.marginLeft = 6;
            _subBubble.style.borderTopLeftRadius = 14;
            _subBubble.style.borderTopRightRadius = 14;
            _subBubble.style.borderBottomLeftRadius = 14;
            _subBubble.style.borderBottomRightRadius = 14;
            _subBubble.style.backgroundColor = TabletTheme.IslandPill;
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
            // 展开态：从左侧小胶囊变大，移到屏幕上方正中（对齐 HyperOS 超级岛展开形态）
            _expandedCard.style.width = 440;
            _expandedCard.style.maxWidth = Length.Percent(90);
            _expandedCard.style.minHeight = 112;
            _expandedCard.style.position = Position.Absolute;
            _expandedCard.style.top = 10;
            _expandedCard.style.left = Length.Percent(50);
            _expandedCard.style.translate = new Translate(Length.Percent(-50), 0);
            _expandedCard.style.transformOrigin = new TransformOrigin(Length.Percent(50), 0);
            _expandedCard.style.borderTopLeftRadius = 34;
            _expandedCard.style.borderTopRightRadius = 34;
            _expandedCard.style.borderBottomLeftRadius = 34;
            _expandedCard.style.borderBottomRightRadius = 34;
            _expandedCard.style.backgroundColor = TabletTheme.IslandBlack;
            _expandedCard.style.paddingLeft = 22;
            _expandedCard.style.paddingRight = 22;
            _expandedCard.style.paddingTop = 14;
            _expandedCard.style.paddingBottom = 18;
            _expandedCard.style.overflow = Overflow.Hidden;

            // 底部把手：黄铜色细条，既是 HyperOS 的下拉把手，也是铁道铭牌细节
            var handle = _expandedHandle = new VisualElement { name = "super-island-handle", pickingMode = PickingMode.Ignore };
            handle.style.position = Position.Absolute;
            handle.style.bottom = 7;
            handle.style.left = Length.Percent(50);
            handle.style.translate = new Translate(Length.Percent(-50), 0);
            handle.style.width = 44;
            handle.style.height = 4;
            handle.style.borderTopLeftRadius = handle.style.borderTopRightRadius = 2;
            handle.style.borderBottomLeftRadius = handle.style.borderBottomRightRadius = 2;
            handle.style.backgroundColor = TabletTheme.Brass;
            // 把手所在的整条底边是可拖动区：下滑呼出小窗、上滑收起
            _expandedGrab = new VisualElement { name = "super-island-grab" };
            _expandedGrab.style.position = Position.Absolute;
            _expandedGrab.style.left = 0; _expandedGrab.style.right = 0; _expandedGrab.style.bottom = 0;
            _expandedGrab.style.height = 26;
            _expandedGrab.pickingMode = PickingMode.Position;
            _expandedGrab.Add(handle);
            AttachHandleDrag(_expandedGrab);
            _expandedCard.Add(_expandedGrab);
            _expandedCard.pickingMode = PickingMode.Position;
            _expandedCard.style.display = DisplayStyle.None;
            _expandedCard.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            Root.Add(_expandedCard);

            _parentContainer.Add(Root);
        }

        public void RegisterSession(IslandSession session)
        {
            if (session == null || session.AppType == IslandAppType.None) return;
            // GameMainUI 每帧都会重新登记播放/遥测会话：外观没变就只换回调，不重建整座岛（否则每帧销毁重建 UI 树）
            string key = session.EffectiveKey;
            bool unchanged = _activeSessions.TryGetValue(key, out var existing) && SameVisual(existing, session);
            _activeSessions[key] = session;
            if (unchanged) return;
            EvaluateActiveSessions();
        }

        private static bool SameVisual(IslandSession a, IslandSession b)
        {
            return a.AppType == b.AppType && a.Priority == b.Priority && a.Key == b.Key && a.Icon == b.Icon && a.Header == b.Header &&
                   a.Actions.Count == b.Actions.Count &&
                   a.Title == b.Title && a.Subtitle == b.Subtitle && a.Metric == b.Metric &&
                   Mathf.Approximately(a.Progress, b.Progress) && Mathf.Approximately(a.AuxValue, b.AuxValue) &&
                   a.AccentColor == b.AccentColor;
        }

        public void UpdateSession(IslandAppType type, Action<IslandSession> update) => UpdateSession(type.ToString(), update);

        public void UpdateSession(string key, Action<IslandSession> update)
        {
            if (!string.IsNullOrEmpty(key) && _activeSessions.TryGetValue(key, out var session))
            {
                update?.Invoke(session);
                EvaluateActiveSessions();
            }
        }

        public void RemoveSession(IslandAppType type) => RemoveSession(type.ToString());

        public void RemoveSession(string key)
        {
            if (!string.IsNullOrEmpty(key) && _activeSessions.Remove(key))
            {
                EvaluateActiveSessions();
            }
        }

        public void ClearAllSessions()
        {
            _activeSessions.Clear();
            EvaluateActiveSessions();
        }

        private static readonly StylePropertyName[] MorphProps =
        {
            new StylePropertyName("left"), new StylePropertyName("top"), new StylePropertyName("width"), new StylePropertyName("height"),
            new StylePropertyName("border-top-left-radius"), new StylePropertyName("border-top-right-radius"),
            new StylePropertyName("border-bottom-left-radius"), new StylePropertyName("border-bottom-right-radius"),
            new StylePropertyName("translate"), new StylePropertyName("opacity")
        };

        private void SetCardTransition(int ms, EasingMode ease)
        {
            _expandedCard.style.transitionProperty = new List<StylePropertyName>(MorphProps);
            _expandedCard.style.transitionDuration = new List<TimeValue> { new TimeValue(ms, TimeUnit.Millisecond) };
            _expandedCard.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(ease) };
        }

        private void SetCardRadius(float r)
        {
            _expandedCard.style.borderTopLeftRadius = r; _expandedCard.style.borderTopRightRadius = r;
            _expandedCard.style.borderBottomLeftRadius = r; _expandedCard.style.borderBottomRightRadius = r;
        }

        private Rect RootLocalRect(VisualElement e)
        {
            Rect w = e.worldBound, r = Root.worldBound;
            return new Rect(w.x - r.x, w.y - r.y, w.width, w.height);
        }

        /// <summary>变形期间把内容按最终宽度固定住，卡片只是“揭开”它，避免窄卡片里内容挤成一团。</summary>
        private void PinChildren(float width)
        {
            foreach (var c in _expandedCard.Children())
            {
                if (c == _expandedGrab) continue;
                c.style.width = width; c.style.flexShrink = 0;
            }
        }

        private void UnpinChildren()
        {
            foreach (var c in _expandedCard.Children())
            {
                if (c == _expandedGrab) continue;
                c.style.width = StyleKeyword.Auto; c.style.flexShrink = 1;
                c.style.transitionDuration = new List<TimeValue> { new TimeValue(0, TimeUnit.Millisecond) };
                c.style.opacity = 1f;
            }
        }

        private void FadeChildren(float to, int ms, int delay)
        {
            foreach (var c in _expandedCard.Children())
            {
                if (c == _expandedGrab) continue;
                c.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity") };
                c.style.transitionDuration = new List<TimeValue> { new TimeValue(ms, TimeUnit.Millisecond) };
                c.style.transitionDelay = new List<TimeValue> { new TimeValue(delay, TimeUnit.Millisecond) };
                c.style.opacity = to;
            }
        }

        private void RestoreCardLayout()
        {
            _expandedCard.style.left = Length.Percent(50);
            _expandedCard.style.top = 10;
            _expandedCard.style.width = 440;
            _expandedCard.style.height = StyleKeyword.Auto;
            _expandedCard.style.minHeight = 112;
            _expandedCard.style.maxWidth = Length.Percent(90);
            _expandedCard.style.translate = new Translate(Length.Percent(-50), 0);
            SetCardRadius(34);
        }

        public void SetExpanded(bool expanded)
        {
            bool targetExpanded = expanded && (_currentPrimaryType != IslandAppType.None);
            if (_isExpanded == targetExpanded && _expandedCard.style.display == (_isExpanded ? DisplayStyle.Flex : DisplayStyle.None)) return;
            _isExpanded = targetExpanded;
            VisualStateChanged?.Invoke();
            int token = ++_morphToken;
            int ms = TabletTheme.MotionMs(340);

            if (_isExpanded)
            {
                Root.BringToFront();
                _scrimOverlay.style.display = DisplayStyle.Flex;
                _expandedCard.style.display = DisplayStyle.Flex;
                _expandedCard.BringToFront();
                RenderExpandedContent();
                _pillContainer.pickingMode = PickingMode.Ignore;

                if (ms <= 0)
                {
                    _morphing = false;
                    RestoreCardLayout();
                    SetCardTransition(0, EasingMode.Linear);
                    _expandedCard.style.opacity = 1f;
                    _scrimOverlay.style.opacity = 1f;
                    _pillContainer.style.opacity = 0f;
                    return;
                }

                // 从小胶囊的位置和大小“长”成居中的大岛：先在胶囊位置就位（不可见），下一帧起过渡到最终尺寸
                _morphing = true;
                SetCardTransition(0, EasingMode.Linear);
                _expandedCard.style.opacity = 0f;
                _expandedCard.schedule.Execute(() =>
                {
                    if (token != _morphToken) return;
                    Rect pill = RootLocalRect(_pillContainer);
                    float cardW = _expandedCard.layout.width, cardH = _expandedCard.layout.height;
                    if (cardW <= 1f || cardH <= 1f) { _morphing = false; RestoreCardLayout(); _expandedCard.style.opacity = 1f; return; }
                    float endLeft = (Root.layout.width - cardW) * 0.5f;
                    PinChildren(cardW - 44f);
                    _expandedCard.style.maxWidth = StyleKeyword.None;
                    _expandedCard.style.minHeight = 0;
                    _expandedCard.style.translate = new Translate(0, 0);
                    _expandedCard.style.left = pill.x; _expandedCard.style.top = pill.y;
                    _expandedCard.style.width = pill.width; _expandedCard.style.height = pill.height;
                    SetCardRadius(pill.height * 0.5f);
                    _expandedCard.style.opacity = 1f;
                    _pillContainer.style.opacity = 0f;
                    foreach (var c in _expandedCard.Children()) { if (c == _expandedGrab) continue; c.style.transitionDuration = new List<TimeValue> { new TimeValue(0, TimeUnit.Millisecond) }; c.style.opacity = 0f; }
                    _expandedCard.schedule.Execute(() =>
                    {
                        if (token != _morphToken) return;
                        SetCardTransition(ms, EasingMode.EaseOutCubic);
                        _expandedCard.style.left = endLeft; _expandedCard.style.top = 10;
                        _expandedCard.style.width = cardW; _expandedCard.style.height = cardH;
                        SetCardRadius(34);
                        _scrimOverlay.style.opacity = 1f;
                        FadeChildren(1f, 200, ms / 5);
                    }).ExecuteLater(16);
                    _expandedCard.schedule.Execute(() =>
                    {
                        if (token != _morphToken) return;
                        UnpinChildren();
                        SetCardTransition(0, EasingMode.Linear);
                        RestoreCardLayout();
                        _morphing = false;
                    }).ExecuteLater(ms + 60);
                }).ExecuteLater(16);
            }
            else
            {
                _pillContainer.pickingMode = PickingMode.Position;
                if (ms <= 0)
                {
                    _morphing = false;
                    _scrimOverlay.style.opacity = 0f;
                    _scrimOverlay.style.display = DisplayStyle.None;
                    _expandedCard.style.opacity = 0f;
                    _expandedCard.style.display = DisplayStyle.None;
                    _pillContainer.style.opacity = 1f;
                    return;
                }

                // 收回：卡片缩回胶囊的位置和大小，胶囊在收尾时淡入接替
                int closeMs = (int)(ms * 0.8f);
                _morphing = true;
                Rect from = RootLocalRect(_expandedCard);
                Rect to = RootLocalRect(_pillContainer);
                SetCardTransition(0, EasingMode.Linear);
                PinChildren(from.width - 44f);
                _expandedCard.style.maxWidth = StyleKeyword.None;
                _expandedCard.style.minHeight = 0;
                _expandedCard.style.translate = new Translate(0, 0);
                _expandedCard.style.left = from.x; _expandedCard.style.top = from.y;
                _expandedCard.style.width = from.width; _expandedCard.style.height = from.height;
                _expandedCard.schedule.Execute(() =>
                {
                    if (token != _morphToken) return;
                    SetCardTransition(closeMs, EasingMode.EaseInOutCubic);
                    _expandedCard.style.left = to.x; _expandedCard.style.top = to.y;
                    _expandedCard.style.width = to.width; _expandedCard.style.height = to.height;
                    SetCardRadius(to.height * 0.5f);
                    _scrimOverlay.style.opacity = 0f;
                    FadeChildren(0f, closeMs / 3, 0);
                }).ExecuteLater(16);
                _pillContainer.schedule.Execute(() =>
                {
                    if (token != _morphToken) return;
                    _pillContainer.style.opacity = 1f;
                }).ExecuteLater((int)(closeMs * 0.75f));
                _expandedCard.schedule.Execute(() =>
                {
                    if (token != _morphToken || _isExpanded) return;
                    UnpinChildren();
                    SetCardTransition(0, EasingMode.Linear);
                    _expandedCard.style.display = DisplayStyle.None;
                    _scrimOverlay.style.display = DisplayStyle.None;
                    RestoreCardLayout();
                    _morphing = false;
                }).ExecuteLater(closeMs + 60);
            }
        }

        // ---------- 把手拖动：下滑呼出小窗，上滑收起 ----------
        private void AttachHandleDrag(VisualElement grab)
        {
            int pointer = -1;
            float startY = 0f;
            const float FloatThreshold = 56f;
            grab.RegisterCallback<PointerDownEvent>(e =>
            {
                if (_morphing) return;
                pointer = e.pointerId; startY = e.position.y;
                grab.CapturePointer(pointer);
                SetCardTransition(0, EasingMode.Linear);
                e.StopPropagation();
            });
            grab.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (pointer != e.pointerId) return;
                float dy = e.position.y - startY;
                float shown = dy > 0f ? Mathf.Min(dy, FloatThreshold * 2f) * 0.6f : Mathf.Max(dy, -40f) * 0.5f;
                _expandedCard.style.translate = new Translate(Length.Percent(-50), shown);
                e.StopPropagation();
            });
            EventCallback<PointerUpEvent> release = e =>
            {
                if (pointer != e.pointerId) return;
                grab.ReleasePointer(pointer);
                pointer = -1;
                float dy = e.position.y - startY;
                if (dy > FloatThreshold && _activeSessions.TryGetValue(_primaryKey, out var session) && FloatRequested != null && FloatRequested(session))
                {
                    SetExpanded(false);
                }
                else if (dy < -28f)
                {
                    SetExpanded(false);
                }
                else
                {
                    SetCardTransition(TabletTheme.MotionMs(240), EasingMode.EaseOutBack);
                    _expandedCard.style.translate = new Translate(Length.Percent(-50), 0);
                }
                e.StopPropagation();
            };
            grab.RegisterCallback(release);
            grab.RegisterCallback<PointerCancelEvent>(_ =>
            {
                if (pointer < 0) return;
                grab.ReleasePointer(pointer); pointer = -1;
                SetCardTransition(TabletTheme.MotionMs(240), EasingMode.EaseOutBack);
                _expandedCard.style.translate = new Translate(Length.Percent(-50), 0);
            });
        }

        /// <summary>岛会话对应的桌面应用名（用于“呼出小窗/打开应用”）；没有对应应用返回 null。</summary>
        public static string AppNameFor(IslandSession s)
        {
            if (s == null) return null;
            switch (s.AppType)
            {
                case IslandAppType.Music: return "音乐";
                case IslandAppType.Timer: return "时钟";
                case IslandAppType.Recorder: return "录音机";
                case IslandAppType.TrainCab: return "驾驶台";
                case IslandAppType.DispatchAlert: return "RDA";
                case IslandAppType.CloudSync: return "设置";
                case IslandAppType.Custom: return s.Key;
                default: return null;
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

            // 周期刷新动画状态（声波跳动、录音红点闪烁、旋转环等）；约每 6 帧一次，不再每 4 帧重建
            if (_tickCount % 6 == 0)
            {
                if (!_isExpanded)
                {
                    RenderCompactContent();
                }
                else if (!_morphing)
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
                _primaryKey = ""; _secondaryKey = "";
                _subBubble.style.display = DisplayStyle.None;
                SetExpanded(false);
                VisualStateChanged?.Invoke();
                if (_pillContainer.style.display == DisplayStyle.Flex && TabletTheme.MotionMs(180) > 0)
                {
                    _pillContainer.style.opacity = 0f;
                    _pillContainer.schedule.Execute(() =>
                    {
                        if (_activeSessions.Count == 0) _pillContainer.style.display = DisplayStyle.None;
                    }).ExecuteLater(220);
                }
                else
                {
                    _pillContainer.style.display = DisplayStyle.None;
                }
                return;
            }

            // 按优先级排序：最高者为 Primary，次高者为 Secondary
            var list = new List<IslandSession>(_activeSessions.Values);
            list.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            _currentPrimaryType = list[0].AppType;
            _currentSecondaryType = list.Count > 1 ? list[1].AppType : IslandAppType.None;
            _primaryKey = list[0].EffectiveKey;
            _secondaryKey = list.Count > 1 ? list[1].EffectiveKey : "";

            bool pillWasHidden = _pillContainer.style.display != DisplayStyle.Flex;
            _pillContainer.style.display = DisplayStyle.Flex;
            if (pillWasHidden && !_isExpanded && TabletTheme.MotionMs(180) > 0)
            {
                // 日期先淡出，岛随后在同一位置浮现
                _pillContainer.style.transitionDuration = new List<TimeValue> { new TimeValue(0, TimeUnit.Millisecond) };
                _pillContainer.style.opacity = 0f;
                _pillContainer.style.scale = new Scale(new Vector3(0.9f, 0.9f, 1f));
                _pillContainer.schedule.Execute(() =>
                {
                    _pillContainer.style.transitionDuration = new List<TimeValue> { new TimeValue(200, TimeUnit.Millisecond) };
                    _pillContainer.style.transitionDelay = new List<TimeValue> { new TimeValue(120, TimeUnit.Millisecond) };
                    _pillContainer.style.opacity = 1f;
                    _pillContainer.style.scale = new Scale(Vector3.one);
                }).ExecuteLater(16);
                _pillContainer.schedule.Execute(() => _pillContainer.style.transitionDelay = new List<TimeValue> { new TimeValue(0, TimeUnit.Millisecond) }).ExecuteLater(500);
            }
            else if (pillWasHidden)
            {
                _pillContainer.style.opacity = 1f;
            }
            VisualStateChanged?.Invoke();
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
            if (_activeSessions.TryGetValue(_secondaryKey, out var sub))
            {
                // 暂时提升其优先级
                sub.Priority += 200;
                EvaluateActiveSessions();
            }
        }

        // ==================== 微胶囊态渲染 (Compact Pill) ====================
        private void RenderCompactContent()
        {
            if (!_activeSessions.TryGetValue(_primaryKey, out var s)) return;
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

                    // 紧凑态只显示缩略图与声波，歌名留给展开态
                    break;

                case IslandAppType.TrainCab:
                    // 🚄 机车头 + 信号绿点 + 实时车速胶囊 (e.g. 78 km/h)
                    var locoIcon = new TabletIconElement(TabletIconKind.Train, 16, TabletTheme.TextStrong);
                    locoIcon.style.marginRight = 4;
                    leftBox.Add(locoIcon);

                    var sigDot = CreateSignalDot(s.Progress > 0.9f ? TabletTheme.Caution : TabletTheme.Success);
                    leftBox.Add(sigDot);

                    var speedLbl = CreatePillLabel(s.Metric ?? $"{s.AuxValue:F0} km/h", 12, true);
                    speedLbl.style.color = TabletTheme.SpeedGold;
                    speedLbl.style.marginLeft = 6;
                    leftBox.Add(speedLbl);

                    var statusBadge = CreatePillLabel(s.Subtitle ?? "ATO巡航", 11, false);
                    statusBadge.style.color = TabletTheme.TextSecondary;
                    statusBadge.style.marginLeft = 6;
                    rightBox.Add(statusBadge);
                    break;

                case IslandAppType.Recorder:
                    // 🔴 呼吸红点 + 计时 (01:23) + 拾音振幅条
                    var recDot = CreateSignalDot(TabletTheme.SignalRed, pulsing: true);
                    leftBox.Add(recDot);

                    var recWave = CreateMicroEqualizer(TabletTheme.SignalRed);
                    recWave.style.marginLeft = 6;
                    leftBox.Add(recWave);

                    var recTime = CreatePillLabel(s.Metric ?? "00:00", 12, true);
                    recTime.style.marginLeft = 6;
                    rightBox.Add(recTime);
                    break;

                case IslandAppType.Timer:
                    // ⏳ 沙漏 + 倒计时
                    var timerIc = new TabletIconElement(TabletIconKind.Hourglass, 15, TabletTheme.Info);
                    timerIc.style.marginRight = 4;
                    leftBox.Add(timerIc);

                    var timerMetric = CreatePillLabel(s.Metric ?? "00:00", 12, true);
                    timerMetric.style.color = TabletTheme.Info;
                    leftBox.Add(timerMetric);

                    var timerSub = CreatePillLabel("计时中", 11, false);
                    timerSub.style.marginLeft = 6;
                    rightBox.Add(timerSub);
                    break;

                case IslandAppType.CloudSync:
                    // ☁️ 云同步 + 进度百分比
                    var syncIc = new TabletIconElement(TabletIconKind.Cloud, 16, TabletTheme.TextStrong);
                    syncIc.style.marginRight = 4;
                    leftBox.Add(syncIc);

                    var syncBar = CreateMicroProgressBar(s.Progress, s.AccentColor);
                    leftBox.Add(syncBar);

                    var syncPct = CreatePillLabel(s.Metric ?? $"{(s.Progress * 100):F0}%", 12, true);
                    syncPct.style.marginLeft = 6;
                    rightBox.Add(syncPct);
                    break;

                case IslandAppType.Custom:
                    // 通用模板：图标 + 标题，右侧指标
                    var cIc = new TabletIconElement(s.Icon, 16, s.AccentColor);
                    cIc.style.marginRight = 6;
                    leftBox.Add(cIc);
                    if (s.Progress > 0f)
                    {
                        leftBox.Add(CreateMicroProgressBar(s.Progress, s.AccentColor));
                    }
                    else
                    {
                        var cTitle = CreatePillLabel(s.Title ?? "", 12, true);
                        cTitle.style.maxWidth = 120; cTitle.style.overflow = Overflow.Hidden;
                        leftBox.Add(cTitle);
                    }
                    if (!string.IsNullOrEmpty(s.Metric))
                    {
                        var cMetric = CreatePillLabel(s.Metric, 12, true);
                        cMetric.style.marginLeft = 6;
                        rightBox.Add(cMetric);
                    }
                    break;

                case IslandAppType.DispatchAlert:
                    // 调度警标 + 指令简报
                    var alertIc = new TabletIconElement(TabletIconKind.Bolt, 14, TabletTheme.Alert);
                    alertIc.style.marginRight = 4;
                    leftBox.Add(alertIc);

                    var alertTitle = CreatePillLabel(s.Title ?? "紧急调度", 12, true);
                    alertTitle.style.color = TabletTheme.Alert;
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
            if (!_activeSessions.TryGetValue(_secondaryKey, out var s))
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
                    _subBubble.Add(new TabletIconElement(TabletIconKind.Train, 16, TabletTheme.TextStrong));
                    break;
                case IslandAppType.Recorder:
                    _subBubble.Add(CreateSignalDot(TabletTheme.SignalRed, pulsing: true));
                    break;
                case IslandAppType.Timer:
                    _subBubble.Add(new TabletIconElement(TabletIconKind.Hourglass, 15, TabletTheme.Info));
                    break;
                case IslandAppType.CloudSync:
                    _subBubble.Add(new TabletIconElement(TabletIconKind.Cloud, 16, TabletTheme.TextStrong));
                    break;
                case IslandAppType.DispatchAlert:
                    _subBubble.Add(new TabletIconElement(TabletIconKind.Bolt, 15, TabletTheme.Alert));
                    break;
                case IslandAppType.Custom:
                    _subBubble.Add(new TabletIconElement(s.Icon, 16, s.AccentColor));
                    break;
                default:
                    _subBubble.Add(CreateSignalDot(TabletTheme.Accent));
                    break;
            }
        }

        // ==================== 大岛展开态渲染 (Expanded Island Card) ====================
        private void RenderExpandedContent()
        {
            if (!_activeSessions.TryGetValue(_primaryKey, out var s)) return;
            _expandedCard.Clear();
            if (_expandedGrab != null) _expandedCard.Add(_expandedGrab);

            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.justifyContent = Justify.SpaceBetween;
            topRow.style.alignItems = Align.Center;
            topRow.style.marginBottom = 8;
            _expandedCard.Add(topRow);

            var closeBtn = new Button(() => SetExpanded(false));
            closeBtn.style.width = 24; closeBtn.style.height = 24;
            closeBtn.style.borderTopLeftRadius = 12; closeBtn.style.borderTopRightRadius = 12;
            closeBtn.style.borderBottomLeftRadius = 12; closeBtn.style.borderBottomRightRadius = 12;
            closeBtn.style.backgroundColor = TabletTheme.OverlaySoft;
            closeBtn.style.borderTopWidth = 0; closeBtn.style.borderBottomWidth = 0;
            closeBtn.style.borderLeftWidth = 0; closeBtn.style.borderRightWidth = 0;
            closeBtn.style.alignItems = Align.Center; closeBtn.style.justifyContent = Justify.Center;
            closeBtn.style.paddingLeft = 0; closeBtn.style.paddingRight = 0;
            closeBtn.style.paddingTop = 0; closeBtn.style.paddingBottom = 0;
            closeBtn.Add(new TabletIconElement(TabletIconKind.Close, 14, Color.white));

            switch (s.AppType)
            {
                // ---------- 1. 音乐大岛 ----------
                case IslandAppType.Music:
                    var musicHeader = CreatePillLabel("车载随身听 · 正在播放", 11, false);
                    musicHeader.style.color = TabletTheme.TextSecondary;
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
                    subLbl.style.color = TabletTheme.TextMuted;
                    subLbl.style.marginTop = 2;
                    musicText.Add(subLbl);

                    // 播控胶囊按键
                    var controls = new VisualElement();
                    controls.style.flexDirection = FlexDirection.Row;
                    controls.style.alignItems = Align.Center;
                    musicBody.Add(controls);

                    var prevBtn = CreateIconActionBtn(TabletIconKind.Back, () => s.OnSecondaryAction?.Invoke());
                    var playBtn = CreateIconActionBtn(TabletIconKind.Play, () => s.OnPrimaryAction?.Invoke(), isAccent: true);
                    playBtn.style.width = 36; playBtn.style.height = 36;
                    playBtn.style.marginLeft = 6; playBtn.style.marginRight = 6;
                    var appBtn = CreateIconActionBtn(TabletIconKind.Apps, () => { SetExpanded(false); _onLaunchApp?.Invoke("音乐"); });

                    controls.Add(prevBtn);
                    controls.Add(playBtn);
                    controls.Add(appBtn);
                    break;

                // ---------- 2. 列控遥测大岛 ----------
                case IslandAppType.TrainCab:
                    var cabHeader = CreatePillLabel("列控遥测系统 · NF-5K 机车", 11, false);
                    cabHeader.style.color = TabletTheme.TextSecondary;
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
                    spdLbl.style.color = TabletTheme.SpeedGold;
                    speedBox.Add(spdLbl);

                    var subSpd = CreatePillLabel(s.Subtitle ?? "限速 80 km/h · 自动巡航中", 11, false);
                    subSpd.style.color = TabletTheme.TextSecondary;
                    speedBox.Add(subSpd);

                    var cabActions = new VisualElement();
                    cabActions.style.flexDirection = FlexDirection.Row;
                    cabActions.style.alignItems = Align.Center;
                    cabBody.Add(cabActions);

                    var brakeBtn = CreateTextActionBtn("紧急制动", TabletTheme.Danger, () => s.OnPrimaryAction?.Invoke());
                    brakeBtn.style.marginRight = 8;
                    cabActions.Add(brakeBtn);

                    var rdaBtn = CreateTextActionBtn("驾驶室", TabletTheme.Accent, () => { SetExpanded(false); _onLaunchApp?.Invoke("归途"); });
                    cabActions.Add(rdaBtn);
                    break;

                // ---------- 3. 录音大岛 ----------
                case IslandAppType.Recorder:
                    var recHeader = CreatePillLabel("车载声音备忘 · 拾音录制中", 11, false);
                    recHeader.style.color = TabletTheme.SignalRed;
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
                    subRec.style.color = TabletTheme.TextSecondary;
                    recInfo.Add(subRec);

                    var recActions = new VisualElement();
                    recActions.style.flexDirection = FlexDirection.Row;
                    recActions.style.alignItems = Align.Center;
                    recBody.Add(recActions);

                    var flagBtn = CreateTextActionBtn("标记", TabletTheme.OverlayMedium, () => s.OnSecondaryAction?.Invoke());
                    flagBtn.style.marginRight = 8;
                    recActions.Add(flagBtn);

                    var stopBtn = CreateTextActionBtn("保存", TabletTheme.SignalRed, () => s.OnPrimaryAction?.Invoke());
                    recActions.Add(stopBtn);
                    break;

                // ---------- 4. 倒计时大岛 ----------
                case IslandAppType.Timer:
                    var timerHeader = CreatePillLabel("运行区间测时秒表", 11, false);
                    timerHeader.style.color = TabletTheme.Info;
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
                    tVal.style.color = TabletTheme.Info;
                    tInfo.Add(tVal);

                    var tSub = CreatePillLabel(s.Subtitle ?? "区间行车时分测定", 11, false);
                    tSub.style.color = TabletTheme.TextSecondary;
                    tInfo.Add(tSub);

                    var tActions = new VisualElement();
                    tActions.style.flexDirection = FlexDirection.Row;
                    tActions.style.alignItems = Align.Center;
                    timerBody.Add(tActions);

                    var rstBtn = CreateTextActionBtn("复位", TabletTheme.OverlayMedium, () => s.OnSecondaryAction?.Invoke());
                    rstBtn.style.marginRight = 8;
                    tActions.Add(rstBtn);

                    var pauseBtn = CreateTextActionBtn("暂停", TabletTheme.InfoButton, () => s.OnPrimaryAction?.Invoke());
                    tActions.Add(pauseBtn);
                    break;

                // ---------- 5. 云同步大岛 ----------
                case IslandAppType.CloudSync:
                    var syncHeader = CreatePillLabel("系统备份与恢复 · 传输中", 11, false);
                    syncHeader.style.color = TabletTheme.Info;
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

                // ---------- 通用大岛（任何应用登记的会话） ----------
                case IslandAppType.Custom:
                    var gHeader = CreatePillLabel(s.Header ?? s.Title ?? "", 11, false);
                    gHeader.style.color = s.AccentColor;
                    topRow.Add(gHeader);
                    topRow.Add(closeBtn);

                    var gBody = new VisualElement();
                    gBody.style.flexDirection = FlexDirection.Row;
                    gBody.style.alignItems = Align.Center;
                    gBody.style.justifyContent = Justify.SpaceBetween;
                    _expandedCard.Add(gBody);

                    var gLeft = new VisualElement();
                    gLeft.style.flexDirection = FlexDirection.Row;
                    gLeft.style.alignItems = Align.Center;
                    gLeft.style.flexGrow = 1; gLeft.style.flexShrink = 1;
                    gBody.Add(gLeft);
                    var gBadge = new VisualElement();
                    gBadge.style.width = 44; gBadge.style.height = 44; gBadge.style.marginRight = 12; gBadge.style.flexShrink = 0;
                    gBadge.style.alignItems = Align.Center; gBadge.style.justifyContent = Justify.Center;
                    gBadge.style.backgroundColor = new Color(s.AccentColor.r, s.AccentColor.g, s.AccentColor.b, 0.22f);
                    gBadge.style.borderTopLeftRadius = gBadge.style.borderTopRightRadius = 14;
                    gBadge.style.borderBottomLeftRadius = gBadge.style.borderBottomRightRadius = 14;
                    gBadge.Add(new TabletIconElement(s.Icon, 24, s.AccentColor));
                    gLeft.Add(gBadge);
                    var gText = new VisualElement();
                    gText.style.flexShrink = 1;
                    gLeft.Add(gText);
                    gText.Add(CreatePillLabel(s.Metric ?? s.Title ?? "", 20, true));
                    if (!string.IsNullOrEmpty(s.Subtitle))
                    {
                        var gSub = CreatePillLabel(s.Subtitle, 11, false);
                        gSub.style.color = TabletTheme.TextSecondary;
                        gText.Add(gSub);
                    }

                    var gActions = new VisualElement();
                    gActions.style.flexDirection = FlexDirection.Row;
                    gActions.style.alignItems = Align.Center;
                    gBody.Add(gActions);
                    foreach (var act in s.Actions)
                    {
                        var captured = act;
                        Action click = () => { captured.OnClick?.Invoke(); if (captured.CloseOnClick) SetExpanded(false); };
                        VisualElement ab;
                        if (act.Icon.HasValue)
                            ab = CreateIconActionBtn(act.Icon.Value, click, isAccent: act.Style == IslandActionStyle.Primary);
                        else
                            ab = CreateTextActionBtn(act.Label ?? "", act.Style == IslandActionStyle.Danger ? TabletTheme.Danger : act.Style == IslandActionStyle.Primary ? s.AccentColor : TabletTheme.OverlayMedium, click);
                        ab.style.marginLeft = 8;
                        gActions.Add(ab);
                    }
                    if (s.Progress > 0f)
                    {
                        var gBar = CreateMicroProgressBar(s.Progress, s.AccentColor, width: 396, height: 5);
                        gBar.style.marginTop = 10;
                        _expandedCard.Add(gBar);
                    }
                    break;

                // ---------- 6. 调度警报大岛 ----------
                case IslandAppType.DispatchAlert:
                    var alHeader = CreatePillLabel("铁道调度紧急令书", 11, true);
                    alHeader.style.color = TabletTheme.Alert;
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
                    alDesc.style.color = TabletTheme.TextStrong;
                    alDesc.style.marginTop = 2;
                    alText.Add(alDesc);

                    var alActions = new VisualElement();
                    alActions.style.flexDirection = FlexDirection.Row;
                    alActions.style.alignItems = Align.Center;
                    alBody.Add(alActions);

                    var ackBtn = CreateTextActionBtn("已知晓", TabletTheme.SuccessButton, () =>
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
            disc.style.backgroundColor = TabletTheme.DiscFill;
            disc.style.borderTopWidth = 1; disc.style.borderBottomWidth = 1;
            disc.style.borderLeftWidth = 1; disc.style.borderRightWidth = 1;
            var bc = TabletTheme.DiscRim;
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
            track.style.backgroundColor = TabletTheme.OverlayMedium;
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

        private Button CreateIconActionBtn(TabletIconKind kind, Action onClick, bool isAccent = false)
        {
            var btn = new Button(onClick);
            btn.style.width = 30;
            btn.style.height = 30;
            btn.style.borderTopLeftRadius = 15;
            btn.style.borderTopRightRadius = 15;
            btn.style.borderBottomLeftRadius = 15;
            btn.style.borderBottomRightRadius = 15;
            btn.style.backgroundColor = isAccent ? TabletTheme.Accent : TabletTheme.OverlaySoft;
            btn.style.borderTopWidth = 0; btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0; btn.style.borderRightWidth = 0;
            btn.style.alignItems = Align.Center;
            btn.style.justifyContent = Justify.Center;
            btn.style.paddingLeft = 0; btn.style.paddingRight = 0;
            btn.style.paddingTop = 0; btn.style.paddingBottom = 0;
            btn.Add(new TabletIconElement(kind, 16, Color.white));
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
