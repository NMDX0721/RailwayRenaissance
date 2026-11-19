using System;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    public sealed class TabletShellView : IDisposable
    {
        private readonly VisualElement _parent;
        private readonly TabletShellState _state;
        private readonly Font _font;
        private readonly Action _openSettings;

        private readonly VisualElement _rootContainer;
        private readonly VisualElement _launcher;
        private readonly VisualElement _overlay;
        private readonly VisualElement _blurBackground;
        private readonly VisualElement _scrim;
        private readonly VisualElement _panel;          // 铺满全屏的容器：开合动画作用在它上面，两页在里面左右滑动
        private readonly VisualElement _pageNotif;
        private readonly VisualElement _pageCtrl;
        private readonly Label _notifTitle, _notifSub, _ctrlTitle, _ctrlSub;
        private readonly VisualElement _notifContent;
        private readonly VisualElement _ctrlContent;
        private readonly Button _clearFab;
        private readonly VisualElement _pagerDots;
        private readonly VisualElement _dotNotif, _dotCtrl;
        private float _progress;          // 0 = 通知页，1 = 控制中心
        private float _pageShift = 220f;  // 页面随进度平移的距离
        private float _dragSpan = 420f;   // 手指横移多远等于完整切换一页
        private int _pagerToken;
        private int _pagerPointer = -1;
        private bool _pagerDragging;
        private Vector2 _pagerStart;
        private float _pagerStartProgress;
        private float _pagerLastX, _pagerVelocity;
        private float _pagerLastT;
        private Focusable _previousFocus;
        private TabletSystemPanel _renderedPanel;
        private Color _accent = TabletTheme.Accent;
        private bool _highContrast;
        private bool _launcherOnLight;
        private readonly TabletPresentationPreferences _presentation;
        private static float s_preMuteVolume = 1f;

        public VisualElement RootElement => _rootContainer;
        public VisualElement BlurBackgroundElement => _blurBackground;
        public bool IsBlurActive => _blurBackground != null && _blurBackground.style.display == DisplayStyle.Flex && _blurBackground.style.backgroundImage.value.renderTexture != null;
        public Action OnRequestCapture { get; set; }
        public float OverlayOpacity => _overlay != null ? _overlay.style.opacity.value : 1f;
        public TabletPresentationPreferences PresentationPreferences => _presentation;

        public TabletShellView(VisualElement parent, TabletShellState state, Font font, Action openSettings, TabletPresentationPreferences presentation = null)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _font = font;
            _openSettings = openSettings;
            _presentation = presentation;

            _rootContainer = new VisualElement { name = "tablet-shell-root" };
            _rootContainer.style.position = Position.Absolute;
            _rootContainer.style.left = 0;
            _rootContainer.style.top = 0;
            _rootContainer.style.right = 0;
            _rootContainer.style.bottom = 0;
            _rootContainer.pickingMode = PickingMode.Ignore;

            _launcher = new VisualElement { name = "tablet-shell-launcher" };
            _launcher.style.position = Position.Absolute;
            _launcher.style.top = 4;
            _launcher.style.right = 190;
            _launcher.style.height = 36;
            _launcher.style.flexDirection = FlexDirection.Row;
            _launcher.style.alignItems = Align.Center;
            _launcher.pickingMode = PickingMode.Position;
            _launcher.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            var btnNotif = CreatePillButton("通知", 72, 32, () => _state.TogglePanel(TabletSystemPanel.Notifications));
            var btnCtrl = CreatePillButton("控制", 72, 32, () => _state.TogglePanel(TabletSystemPanel.Controls));
            AttachLauncherIcon(btnNotif, TabletIconKind.Bell);
            AttachLauncherIcon(btnCtrl, TabletIconKind.Gear);
            btnNotif.name = "tablet-notifications-open";
            btnCtrl.name = "tablet-controls-open";
            btnNotif.style.marginRight = 6;


            _launcher.Add(btnNotif);
            _launcher.Add(btnCtrl);
            // 状态栏不再显示“通知/控制”文字按钮：面板由顶部下拉手势和状态栏点按呼出。
            // 节点保留（无障碍与审查按名称触发），但不显示、不参与布局。
            _launcher.style.display = DisplayStyle.None;

            _overlay = new VisualElement { name = "tablet-shell-overlay" };
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.top = 0;
            _overlay.style.right = 0;
            _overlay.style.bottom = 0;
            _overlay.pickingMode = PickingMode.Position;

            _blurBackground = new VisualElement { name = "tablet-shell-blur-bg" };
            _blurBackground.style.position = Position.Absolute;
            _blurBackground.style.left = 0;
            _blurBackground.style.top = 0;
            _blurBackground.style.right = 0;
            _blurBackground.style.bottom = 0;
            _blurBackground.pickingMode = PickingMode.Ignore;
            _blurBackground.style.display = DisplayStyle.None;
            _overlay.Add(_blurBackground);

            _scrim = new VisualElement { name = "tablet-shell-scrim" };
            _scrim.style.position = Position.Absolute;
            _scrim.style.left = 0;
            _scrim.style.top = 0;
            _scrim.style.right = 0;
            _scrim.style.bottom = 0;
            _scrim.style.backgroundColor = TabletTheme.ShellScrim;
            _scrim.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == _scrim)
                {
                    _state.ClosePanel();
                }
            });
            _overlay.Add(_scrim);

            _panel = new VisualElement { name = "tablet-shell-panel" };
            _panel.style.position = Position.Absolute;
            _panel.style.left = 0; _panel.style.right = 0; _panel.style.top = 0; _panel.style.bottom = 0;
            _panel.style.backgroundColor = Color.clear;   // 内容直接浮在整屏模糊背景上，由卡片承载材质
            _panel.pickingMode = PickingMode.Ignore;      // 空白处的点击落到遮罩上，用来收起

            _pageNotif = BuildPage("tablet-page-notifications", out _notifTitle, out _notifSub, out _notifContent);
            _pageCtrl = BuildPage("tablet-page-controls", out _ctrlTitle, out _ctrlSub, out _ctrlContent);
            _panel.Add(_pageNotif);
            _panel.Add(_pageCtrl);

            // 对照 HyperOS：两页都没有“✕”；控制中心没有标题行，通知页顶部是大时间 + 右上角通知设置
            _pageNotif.Q("tablet-shell-close").style.display = DisplayStyle.None;
            _pageCtrl[0].style.display = DisplayStyle.None;
            var notifSettings = new Button(() => { _state.ClosePanel(); _openSettings?.Invoke(); }) { name = "tablet-notify-settings", tooltip = "通知设置" };
            notifSettings.AddToClassList("shell-keep");
            notifSettings.style.width = 44; notifSettings.style.height = 44;
            notifSettings.style.marginTop = 0; notifSettings.style.marginBottom = 0; notifSettings.style.marginRight = 0; notifSettings.style.marginLeft = 0;
            notifSettings.style.paddingTop = notifSettings.style.paddingBottom = notifSettings.style.paddingLeft = notifSettings.style.paddingRight = 0;
            notifSettings.style.backgroundColor = Color.clear;
            notifSettings.style.borderTopWidth = notifSettings.style.borderBottomWidth = notifSettings.style.borderLeftWidth = notifSettings.style.borderRightWidth = 0;
            notifSettings.style.alignItems = Align.Center; notifSettings.style.justifyContent = Justify.Center;
            notifSettings.Add(new TabletIconElement(TabletIconKind.Bell, 24, new Color(1f, 1f, 1f, 0.85f)) { pickingMode = PickingMode.Ignore });
            _pageNotif[0].Add(notifSettings);

            // 清除全部：页面右侧中部一枚白色圆形 ✕
            // 对照 HyperOS：通知列表侧边只有一枚“全部清除”胶囊，没有 ✕
            _clearFab = new Button(() => _state.ClearNotifications()) { name = "tablet-notifications-clear", tooltip = "清除全部通知", text = "全部清除" };
            StyleFab(_clearFab, 132, 64, 32);
            _clearFab.style.fontSize = 18;
            _clearFab.style.color = TabletTheme.InkOnGlass;
            _panel.Add(_clearFab);

            // 顶部两点指示（状态栏下方、页面上方）：当前页是长点，点哪个去哪页
            _pagerDots = new VisualElement { name = "tablet-pager-dots" };
            _pagerDots.style.position = Position.Absolute;
            _pagerDots.style.left = 0; _pagerDots.style.right = 0; _pagerDots.style.top = 40;
            _pagerDots.style.flexDirection = FlexDirection.Row;
            _pagerDots.style.justifyContent = Justify.Center;
            _pagerDots.pickingMode = PickingMode.Ignore;
            _dotNotif = BuildDot(TabletSystemPanel.Notifications);
            _dotCtrl = BuildDot(TabletSystemPanel.Controls);
            _pagerDots.Add(_dotNotif);
            _pagerDots.Add(_dotCtrl);
            _panel.Add(_pagerDots);

            // 横向滑动切页：从空白处、页头或控制中心任意位置起手；通知卡片上的横滑留给“滑动清除”
            _overlay.RegisterCallback<PointerDownEvent>(OnPagerDown, TrickleDown.TrickleDown);
            _overlay.RegisterCallback<PointerMoveEvent>(OnPagerMove, TrickleDown.TrickleDown);
            _overlay.RegisterCallback<PointerUpEvent>(OnPagerUp, TrickleDown.TrickleDown);
            _overlay.RegisterCallback<PointerCancelEvent>(_ => EndPagerGesture(false), TrickleDown.TrickleDown);

            _overlay.Add(_panel);
            _rootContainer.Add(_overlay);
            _rootContainer.Add(_launcher);
            _parent.Add(_rootContainer);
            _parent.RegisterCallback<GeometryChangedEvent>(OnParentGeometryChanged);

            _state.Changed += Render;
            if (_presentation != null) _presentation.Changed += Render;
            ApplyResponsiveLayout(_parent.layout.width, _parent.layout.height);
            Render();
        }

        public void PrepareForCapture()
        {
            if (_overlay != null)
            {
                _overlay.style.opacity = 0f;
            }
        }

        public void ApplyBlurredBackground(RenderTexture blurredTexture)
        {
            if (_blurBackground != null)
            {
                if (blurredTexture != null && _state.Panel != TabletSystemPanel.None && !_highContrast)
                {
                    bool alreadyShown = _blurBackground.style.display == DisplayStyle.Flex && _blurBackground.style.backgroundImage.value.renderTexture != null;
                    _blurBackground.style.backgroundImage = Background.FromRenderTexture(blurredTexture);
                    _blurBackground.style.display = DisplayStyle.Flex;
                    int fadeMs = TabletTheme.MotionMs(300);
                    if (!alreadyShown && fadeMs > 0)
                    {
                        // 模糊背景淡入，而不是一帧跳出来
                        _blurBackground.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(0, TimeUnit.Millisecond) };
                        _blurBackground.style.opacity = 0f;
                        _blurBackground.schedule.Execute(() =>
                        {
                            _blurBackground.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("opacity") };
                            _blurBackground.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(fadeMs, TimeUnit.Millisecond) };
                            _blurBackground.style.transitionTimingFunction = new System.Collections.Generic.List<EasingFunction> { new EasingFunction(EasingMode.EaseOut) };
                            _blurBackground.style.opacity = 1f;
                        }).StartingIn(16);
                    }
                    else _blurBackground.style.opacity = 1f;
                }
                else
                {
                    ClearBlurredBackground();
                }
            }
            if (_overlay != null)
            {
                _overlay.style.opacity = 1f;
            }
        }

        public void ClearBlurredBackground()
        {
            if (_blurBackground != null)
            {
                _blurBackground.style.backgroundImage = StyleKeyword.Null;
                _blurBackground.style.display = DisplayStyle.None;
            }
        }

        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                _state.ClosePanel();
                ClearBlurredBackground();
            }

            _rootContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>状态栏落在浅色应用顶栏上时，“通知/控制”按钮改用深色文字和图标。</summary>
        public void SetLauncherOnLight(bool onLight)
        {
            if (_launcherOnLight == onLight) return;
            _launcherOnLight = onLight;
            ApplyPresentation(_accent, _highContrast);
        }

        public void ApplyPresentation(Color accent, bool highContrast)
        {
            bool contrastChanged = _highContrast != highContrast;
            _accent = accent;
            _highContrast = highContrast;
            _panel.style.backgroundColor = Color.clear;
            if (highContrast)
            {
                ClearBlurredBackground();
                _scrim.style.backgroundColor = TabletTheme.ShellScrimHighContrast;
            }
            else
            {
                _scrim.style.backgroundColor = TabletTheme.ShellScrim;
                if (contrastChanged && _state.Panel != TabletSystemPanel.None)
                {
                    OnRequestCapture?.Invoke();
                }
            }
            _rootContainer.Query<Button>().ForEach(button =>
            {
                if (button.ClassListContains("shell-keep")) return;
                bool quietActive = button.name == "tablet-quiet-toggle" && _state.QuietMode;
                bool hcActive = button.name == "tablet-high-contrast-toggle" && (_presentation?.HighContrast ?? false);
                bool rmActive = button.name == "tablet-reduce-motion-toggle" && (_presentation?.ReduceMotion ?? false);
                bool isActive = quietActive || hcActive || rmActive;

                if (button.name == "tablet-notifications-open" || button.name == "tablet-controls-open")
                {
                    var launcherInk = _launcherOnLight ? TabletTheme.InkOnGlass : Color.white;
                    button.style.color = launcherInk;
                    button.style.backgroundColor = _launcherOnLight ? new Color(0f, 0f, 0f, 0.07f) : TabletTheme.OverlaySoft;
                    button.style.borderTopColor = button.style.borderBottomColor = button.style.borderLeftColor = button.style.borderRightColor = _launcherOnLight ? new Color(0f, 0f, 0f, 0.10f) : TabletTheme.OverlayMedium;
                    button.Query<TabletIconElement>().ForEach(ic => ic.IconColor = launcherInk);
                }
                else
                {
                    // 玻璃磁贴：开启为实白底深色字，关闭为半透明深色底白字
                    button.style.color = isActive ? TabletTheme.InkOnGlass : Color.white;
                    button.style.backgroundColor = isActive ? TabletTheme.GlassTileActive : highContrast ? Color.black : TabletTheme.GlassTile;
                    button.style.borderTopWidth = button.style.borderBottomWidth = button.style.borderLeftWidth = button.style.borderRightWidth = 0;
                }
            });
            _panel.Query<Label>().ForEach(label =>
            {
                if (!label.ClassListContains("shell-dark")) label.style.color = Color.white;
            });
        }

        public void Dispose()
        {
            _state.Changed -= Render;
            if (_presentation != null) _presentation.Changed -= Render;
            _parent.UnregisterCallback<GeometryChangedEvent>(OnParentGeometryChanged);
            OnRequestCapture = null;
            ClearBlurredBackground();
            if (_parent != null && _rootContainer != null && _parent.Contains(_rootContainer))
            {
                _parent.Remove(_rootContainer);
            }
        }

        private bool _sliderDragging, _renderPending;

        private void Render()
        {
            // 正在拖亮度/音量滑条时不重建界面（会把手指下的滑条换掉，拖动当场中断）；松手后补一次
            if (_sliderDragging) { _renderPending = true; return; }

            if (_state.Panel == TabletSystemPanel.None)
            {
                int closeMs = TabletTheme.MotionMs(180);
                if (_renderedPanel != TabletSystemPanel.None && closeMs > 0)
                {
                    PlayPanelCloseAnimation(closeMs);
                }
                else
                {
                    _overlay.style.display = DisplayStyle.None;
                    _overlay.style.opacity = 0f;
                    ClearBlurredBackground();
                }
                if (_renderedPanel != TabletSystemPanel.None)
                {
                    if (_previousFocus is VisualElement previous && previous.panel == _parent.panel)
                        previous.Focus();
                    else
                        (_parent.Q("tablet-desktop") ?? _parent).Focus();
                    _previousFocus = null;
                }
                _renderedPanel = TabletSystemPanel.None;
                return;
            }

            bool opening = _renderedPanel == TabletSystemPanel.None;
            TabletSystemPanel previousPanel = _renderedPanel;
            _closeToken++;   // 关闭动画进行中又被打开：取消收尾
            _overlay.pickingMode = PickingMode.Position;
            if (opening)
            {
                _overlay.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(0, TimeUnit.Millisecond) };
                // 遮罩的暗色随面板一起淡入
                int scrimMs = TabletTheme.MotionMs(260);
                _scrim.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(0, TimeUnit.Millisecond) };
                _scrim.style.opacity = scrimMs > 0 ? 0f : 1f;
                if (scrimMs > 0)
                {
                    _scrim.schedule.Execute(() =>
                    {
                        _scrim.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("opacity") };
                        _scrim.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(scrimMs, TimeUnit.Millisecond) };
                        _scrim.style.transitionTimingFunction = new System.Collections.Generic.List<EasingFunction> { new EasingFunction(EasingMode.EaseOut) };
                        _scrim.style.opacity = 1f;
                    }).StartingIn(16);
                }
            }
            if (opening) _previousFocus = _parent.panel?.focusController.focusedElement;
            _renderedPanel = _state.Panel;
            _overlay.style.display = DisplayStyle.Flex;
            if (opening) { TabletNotifyCenterView.ResetExpansion(); _ctrlEditing = false; }
            _notifContent.Clear();
            _ctrlContent.Clear();
            TabletSuperIsland.Instance?.SetExpanded(false);
            var now = TabletDateTime.Now;
            string dateText = now.Month + "月" + now.Day + "日 " + TabletDateTime.WeekdayLong(now);
            _notifTitle.text = now.ToString("HH:mm");
            _notifTitle.style.fontSize = 56;
            _notifTitle.style.unityFontStyleAndWeight = FontStyle.Normal;
            _notifSub.text = dateText;
            _notifSub.style.fontSize = 16;
            _notifSub.style.marginTop = -4;
            _ctrlTitle.text = "控制中心";
            _ctrlTitle.style.fontSize = 20;
            _ctrlSub.text = dateText;
            // 两页同时存在、互相打通：切换只是滑动，不重新“打开”
            RenderNotifications();
            RenderControls();
            UpdateDots();
            float targetProgress = _state.Panel == TabletSystemPanel.Controls ? 1f : 0f;
            if (opening) { _pagerToken++; ApplyProgress(targetProgress); }
            else if (previousPanel != _state.Panel) AnimatePagerTo(targetProgress, 340);
            else if (!_pagerDragging) ApplyProgress(_progress);
            ApplyResponsiveLayout(_parent.layout.width, _parent.layout.height);
            if (opening)
            {
                PlayPanelOpenAnimation();
                _panel.Focus();
                if (OnRequestCapture != null)
                {
                    PrepareForCapture();
                    OnRequestCapture.Invoke();
                }
                else
                {
                    _overlay.style.opacity = 1f;
                }
            }
            ApplyPresentation(_accent, _highContrast);
        }

        private int _closeToken;

        /// <summary>面板向状态栏方向轻轻收起并淡出，再真正隐藏；期间不拦截点击。</summary>
        private void PlayPanelCloseAnimation(int ms)
        {
            int token = ++_closeToken;
            var list = new System.Collections.Generic.List<TimeValue> { new TimeValue(ms, TimeUnit.Millisecond) };
            var ease = new System.Collections.Generic.List<EasingFunction> { new EasingFunction(EasingMode.EaseIn) };
            _overlay.pickingMode = PickingMode.Ignore;
            _overlay.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("opacity") };
            _overlay.style.transitionDuration = list;
            _overlay.style.transitionTimingFunction = ease;
            _panel.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("translate"), new StylePropertyName("opacity") };
            _panel.style.transitionDuration = list;
            _panel.style.transitionTimingFunction = ease;
            _panel.style.translate = new Translate(0, -20);
            _panel.style.opacity = 0f;
            _overlay.style.opacity = 0f;
            _overlay.schedule.Execute(() =>
            {
                if (token != _closeToken || _state.Panel != TabletSystemPanel.None) return;
                _overlay.style.display = DisplayStyle.None;
                _overlay.pickingMode = PickingMode.Position;
                _overlay.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(0, TimeUnit.Millisecond) };
                ClearBlurredBackground();
            }).ExecuteLater(ms + 30);
        }

        /// <summary>面板从状态栏方向轻轻落下并淡入；开启“减少动效”时直接到位。</summary>
        private void PlayPanelOpenAnimation()
        {
            int ms = TabletTheme.MotionMs(240);
            _panel.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("translate"), new StylePropertyName("opacity") };
            _panel.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(ms, TimeUnit.Millisecond) };
            _panel.style.transitionTimingFunction = new System.Collections.Generic.List<EasingFunction> { new EasingFunction(EasingMode.EaseOut) };
            if (ms <= 0)
            {
                _panel.style.translate = new Translate(0, 0);
                _panel.style.opacity = 1f;
                return;
            }
            _panel.style.translate = new Translate(0, -28);
            _panel.style.opacity = 0f;
            _panel.schedule.Execute(() =>
            {
                _panel.style.translate = new Translate(0, 0);
                _panel.style.opacity = 1f;
            }).StartingIn(16);
        }

        private void OnParentGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyResponsiveLayout(evt.newRect.width, evt.newRect.height);
        }

        private void ApplyResponsiveLayout(float width, float height)
        {
            if (_panel == null || width <= 0 || height <= 0) return;

            bool compact = width < 820 || height < 520;
            bool fullSheet = height > width || width < 620;

            // 通知页：整屏模糊上居中一列（约 47% 宽）；控制中心：靠右一列（约 31% 宽）。
            float notifWidth = Mathf.Clamp(width * 0.47f, compact ? 340f : 420f, 640f);
            float ctrlWidth = Mathf.Clamp(width * 0.31f, compact ? 340f : 380f, 480f);
            if (fullSheet) notifWidth = ctrlWidth = width - 24f;
            notifWidth = Mathf.Min(notifWidth, width);
            ctrlWidth = Mathf.Min(ctrlWidth, width);

            _pageNotif.style.width = notifWidth;
            _pageNotif.style.left = Mathf.Max(0f, (width - notifWidth) * 0.5f);
            _pageCtrl.style.width = ctrlWidth;
            if (fullSheet)
            {
                _pageCtrl.style.left = Mathf.Max(0f, (width - ctrlWidth) * 0.5f);
                _pageCtrl.style.right = StyleKeyword.Auto;
            }
            else
            {
                _pageCtrl.style.left = StyleKeyword.Auto;
                _pageCtrl.style.right = 16;
            }
            _pageShift = fullSheet ? width : Mathf.Clamp(width * 0.15f, 160f, 280f);
            _dragSpan = fullSheet ? width * 0.8f : Mathf.Clamp(width * 0.32f, 280f, 520f);

            foreach (var page in new[] { _pageNotif, _pageCtrl })
            {
                page.style.top = 52 + TabletTheme.SafeInsetTop;
                page.style.bottom = 12;
                page.style.paddingLeft = 0; page.style.paddingRight = 0;
                page.style.paddingTop = 4; page.style.paddingBottom = 0;
            }
            float notifRight = _pageNotif.style.left.value.value + notifWidth;
            if (fullSheet)
            {
                _clearFab.style.left = StyleKeyword.Auto; _clearFab.style.right = 24;
                _clearFab.style.top = StyleKeyword.Auto; _clearFab.style.bottom = 44;
            }
            else
            {
                float gap = Mathf.Min(Mathf.Max(24f, (width - notifRight - 132f) * 0.5f), 120f);
                float x = notifRight + gap;
                float midY = height * 0.5f;
                _clearFab.style.right = StyleKeyword.Auto; _clearFab.style.bottom = StyleKeyword.Auto;
                _clearFab.style.left = x;
                _clearFab.style.top = midY - 32f;
            }
            // 铺满全屏的容器本身不画东西，但保留现代圆角属性供审查读取
            _panel.style.borderTopLeftRadius = 22; _panel.style.borderTopRightRadius = 22;
            _panel.style.borderBottomLeftRadius = 22; _panel.style.borderBottomRightRadius = 22;
            ApplyProgress(_progress);
        }

        // ================= 两页之间的横向切换 =================

        private VisualElement BuildPage(string name, out Label title, out Label sub, out VisualElement content)
        {
            var page = new VisualElement { name = name };
            page.style.position = Position.Absolute;
            page.style.backgroundColor = Color.clear;
            page.pickingMode = PickingMode.Position;
            page.focusable = true;
            page.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            page.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 10;

            var titleBox = new VisualElement();
            title = CreateLabel("系统面板", 15, true);
            sub = CreateLabel(string.Empty, 13, false);
            sub.style.color = TabletTheme.TextStrong;
            titleBox.Add(title);
            titleBox.Add(sub);
            var btnClose = CreateButton("✕", 44, 44, () => _state.ClosePanel());
            btnClose.name = "tablet-shell-close";
            btnClose.AddToClassList("shell-keep");
            btnClose.style.width = 44; btnClose.style.height = 44;
            btnClose.style.backgroundColor = TabletTheme.GlassTileActive;
            btnClose.style.color = TabletTheme.InkOnGlass;
            TabletUIHelper.SetButtonIcon(btnClose, TabletIconKind.Close, 20, TabletTheme.InkOnGlass);
            btnClose.style.borderTopWidth = 0; btnClose.style.borderBottomWidth = 0;
            btnClose.style.borderLeftWidth = 0; btnClose.style.borderRightWidth = 0;
            btnClose.style.borderTopLeftRadius = 22; btnClose.style.borderTopRightRadius = 22;
            btnClose.style.borderBottomLeftRadius = 22; btnClose.style.borderBottomRightRadius = 22;
            header.Add(titleBox);
            header.Add(btnClose);
            page.Add(header);

            content = new VisualElement();
            content.style.flexGrow = 1;
            page.Add(content);
            return page;
        }

        private VisualElement BuildDot(TabletSystemPanel target)
        {
            var hit = new VisualElement { name = target == TabletSystemPanel.Notifications ? "tablet-dot-notifications" : "tablet-dot-controls" };
            hit.style.width = 26; hit.style.height = 26;
            hit.style.alignItems = Align.Center; hit.style.justifyContent = Justify.Center;
            hit.pickingMode = PickingMode.Position;
            var dot = new VisualElement { pickingMode = PickingMode.Ignore };
            dot.style.height = 7; dot.style.width = 7;
            dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius = 3.5f;
            dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 3.5f;
            dot.style.backgroundColor = new Color(1f, 1f, 1f, 0.45f);
            dot.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("width"), new StylePropertyName("background-color") };
            dot.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(TabletTheme.MotionMs(220), TimeUnit.Millisecond) };
            hit.Add(dot);
            hit.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                if (_state.Panel != target) _state.TogglePanel(target);
            });
            return hit;
        }

        private void UpdateDots()
        {
            SetDot(_dotNotif, 1f - _progress);
            SetDot(_dotCtrl, _progress);
        }

        private static void SetDot(VisualElement hit, float weight)
        {
            if (hit == null || hit.childCount == 0) return;
            var dot = hit[0];
            dot.style.width = Mathf.Lerp(7f, 20f, Mathf.Clamp01(weight));
            dot.style.backgroundColor = new Color(1f, 1f, 1f, Mathf.Lerp(0.40f, 0.95f, Mathf.Clamp01(weight)));
        }

        /// <summary>进度 0..1 决定两页的位置、透明度与可点击性；拖动和动画都经过这里。</summary>
        private void ApplyProgress(float p)
        {
            _progress = Mathf.Clamp01(p);
            float shift = _pageShift;
            float t = _progress;
            _pageNotif.style.translate = new Translate(-t * shift, 0);
            _pageCtrl.style.translate = new Translate((1f - t) * shift, 0);
            float notifAlpha = Mathf.Clamp01(1f - t * 1.25f);
            float ctrlAlpha = Mathf.Clamp01(t * 1.25f - 0.25f);
            _pageNotif.style.opacity = notifAlpha;
            _clearFab.style.opacity = notifAlpha;
            _clearFab.style.display = notifAlpha <= 0.001f || _state.Notifications.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            _pageCtrl.style.opacity = ctrlAlpha;
            // 完全看不见的那页不参与布局检测与点击
            _pageNotif.style.display = notifAlpha <= 0.001f ? DisplayStyle.None : DisplayStyle.Flex;
            _pageCtrl.style.display = ctrlAlpha <= 0.001f ? DisplayStyle.None : DisplayStyle.Flex;
            _pageNotif.pickingMode = notifAlpha >= 0.5f ? PickingMode.Position : PickingMode.Ignore;
            _pageCtrl.pickingMode = ctrlAlpha >= 0.5f ? PickingMode.Position : PickingMode.Ignore;
            UpdateDots();
        }

        private void AnimatePagerTo(float target, int baseMs)
        {
            int token = ++_pagerToken;
            float from = _progress;
            int ms = TabletTheme.MotionMs(Mathf.RoundToInt(Mathf.Lerp(160f, baseMs, Mathf.Clamp01(Mathf.Abs(target - from)))));
            if (ms <= 0 || Mathf.Approximately(from, target))
            {
                ApplyProgress(target);
                return;
            }
            float t0 = Time.realtimeSinceStartup;
            _overlay.schedule.Execute(() =>
            {
                if (token != _pagerToken) return;
                float k = Mathf.Clamp01((Time.realtimeSinceStartup - t0) * 1000f / ms);
                float eased = 1f - Mathf.Pow(1f - k, 3f);   // 先快后慢，落点不生硬
                ApplyProgress(Mathf.Lerp(from, target, eased));
            }).Every(8).Until(() => token != _pagerToken || Time.realtimeSinceStartup - t0 >= ms / 1000f);
            _overlay.schedule.Execute(() =>
            {
                if (token == _pagerToken) ApplyProgress(target);
            }).StartingIn(ms + 20);
        }

        private static bool InsideNoPager(VisualElement e)
        {
            for (; e != null; e = e.parent)
                if (e.ClassListContains("shell-no-pager")) return true;
            return false;
        }

        private void OnPagerDown(PointerDownEvent e)
        {
            if (_state.Panel == TabletSystemPanel.None || _pagerPointer != -1) return;
            if (e.target is VisualElement ve && InsideNoPager(ve)) return;
            _pagerPointer = e.pointerId;
            _pagerStart = e.position;
            _pagerStartProgress = _progress;
            _pagerDragging = false;
            _pagerLastX = e.position.x; _pagerVelocity = 0f; _pagerLastT = Time.realtimeSinceStartup;
        }

        private void OnPagerMove(PointerMoveEvent e)
        {
            if (e.pointerId != _pagerPointer) return;
            float dx = e.position.x - _pagerStart.x, dy = e.position.y - _pagerStart.y;
            if (!_pagerDragging)
            {
                if (Mathf.Abs(dx) > 14f && Mathf.Abs(dx) > Mathf.Abs(dy) * 1.4f)
                {
                    _pagerDragging = true;
                    _pagerToken++;                      // 打断正在进行的切换动画，直接跟手
                    _overlay.CapturePointer(e.pointerId);
                }
                else if (Mathf.Abs(dy) > 14f)
                {
                    _pagerPointer = -1;                 // 竖向滑动留给列表与滑条
                    return;
                }
                else return;
            }
            float now = Time.realtimeSinceStartup;
            float dt = Mathf.Max(0.016f, now - _pagerLastT);
            _pagerVelocity = Mathf.Lerp(_pagerVelocity, (e.position.x - _pagerLastX) / dt, 0.4f);
            _pagerLastX = e.position.x; _pagerLastT = now;
            float p = _pagerStartProgress - dx / _dragSpan;
            // 拖出两端时带一点阻尼，而不是硬停
            if (p < 0f) p *= 0.25f; else if (p > 1f) p = 1f + (p - 1f) * 0.25f;
            ApplyProgressRaw(p);
            e.StopPropagation();
        }

        private void ApplyProgressRaw(float p)
        {
            // 越界部分只做轻微位移，不改变透明度
            float clamped = Mathf.Clamp01(p);
            ApplyProgress(clamped);
            float over = p - clamped;
            if (Mathf.Abs(over) > 0f)
            {
                float extra = -over * _pageShift;
                var page = clamped <= 0f ? _pageNotif : _pageCtrl;
                page.style.translate = new Translate(page.resolvedStyle.translate.x + extra, 0);
            }
        }

        private void OnPagerUp(PointerUpEvent e)
        {
            if (e.pointerId != _pagerPointer) return;
            bool wasDragging = _pagerDragging;
            EndPagerGesture(true);
            if (wasDragging) e.StopPropagation();
        }

        private void EndPagerGesture(bool commit)
        {
            if (_pagerPointer == -1) return;
            bool wasDragging = _pagerDragging;
            if (_overlay.HasPointerCapture(_pagerPointer)) _overlay.ReleasePointer(_pagerPointer);
            _pagerPointer = -1;
            _pagerDragging = false;
            if (!wasDragging) return;
            // 速度 + 位置共同决定去哪页：快速一甩就切，慢拖过半也切
            // 只有明显的快速一甩（> 700 px/s）才让速度参与判断，慢拖看位置
            float flick = Mathf.Abs(_pagerVelocity) > 700f ? _pagerVelocity : 0f;
            float projected = _progress - flick / _dragSpan * 0.18f;
            float target = commit ? (projected > 0.5f ? 1f : 0f) : Mathf.Round(_progress);
            var panel = target > 0.5f ? TabletSystemPanel.Controls : TabletSystemPanel.Notifications;
            if (_state.Panel != panel) _state.TogglePanel(panel);   // Render 会从当前位置接着滑到位
            else AnimatePagerTo(target, 300);
        }

        private void RenderNotifications()
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.pickingMode = PickingMode.Position;
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _notifContent.pickingMode = PickingMode.Position;
            _notifContent.Add(scroll);

            TabletNotifyCenterView.Populate(scroll, _state, _highContrast, () => _state.ClosePanel());
            _clearFab.SetEnabled(_state.Notifications.Count > 0);
        }

        private static void StyleFab(Button b, float w, float h, float r)
        {
            b.AddToClassList("shell-keep");
            b.style.position = Position.Absolute;
            b.style.width = w; b.style.height = h;
            b.style.marginTop = 0; b.style.marginBottom = 0; b.style.marginRight = 0; b.style.marginLeft = 0;
            b.style.paddingTop = b.style.paddingBottom = b.style.paddingLeft = b.style.paddingRight = 0;
            b.style.backgroundColor = TabletTheme.GlassTileActive;
            b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 0;
            b.style.borderTopLeftRadius = b.style.borderTopRightRadius = r;
            b.style.borderBottomLeftRadius = b.style.borderBottomRightRadius = r;
            b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
            b.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
        }

        // ================= 控制中心（对照 HyperOS 控制中心） =================

        private bool _ctrlEditing;
        private const string TogglePrefKey = "TabletControlToggles_v1";
        private static readonly string[] AllToggleIds = { "quiet", "bluetooth", "airplane", "mute", "saver", "contrast", "motion", "accent", "dim", "clear", "screenshot", "settings" };

        private sealed class CcToggle
        {
            public string Id, Name, Label;
            public TabletIconKind Icon;
            public Func<bool> Active;
            public Color ActiveIcon;
            public Action Click;
        }

        private CcToggle MakeToggle(string id)
        {
            switch (id)
            {
                case "quiet":
                    return new CcToggle { Id = id, Name = "tablet-quiet-toggle", Label = "勿扰", Icon = TabletIconKind.Moon,
                        Active = () => _state.QuietMode, ActiveIcon = new Color(0.45f, 0.32f, 0.85f, 1f),
                        Click = () => _state.SetQuietMode(!_state.QuietMode) };
                case "bluetooth":
                    return new CcToggle { Id = id, Name = "tablet-bluetooth-toggle", Label = "蓝牙", Icon = TabletIconKind.Bluetooth,
                        Active = () => TabletConnectivity.BluetoothOn, ActiveIcon = new Color(0.16f, 0.48f, 0.95f, 1f),
                        Click = () => { if (!TabletConnectivity.Airplane) TabletConnectivity.SetBluetooth(!TabletConnectivity.BluetoothSwitch); Render(); } };
                case "airplane":
                    return new CcToggle { Id = id, Name = "tablet-airplane-toggle", Label = "飞行模式", Icon = TabletIconKind.Airplane,
                        Active = () => TabletConnectivity.Airplane, ActiveIcon = new Color(0.98f, 0.60f, 0.10f, 1f),
                        Click = () => { TabletConnectivity.SetAirplane(!TabletConnectivity.Airplane); Render(); } };
                case "saver":
                    return new CcToggle { Id = id, Name = "tablet-battery-saver-toggle", Label = "省电模式", Icon = TabletIconKind.Battery,
                        Active = () => TabletConnectivity.BatterySaver, ActiveIcon = new Color(0.20f, 0.70f, 0.40f, 1f),
                        Click = () =>
                        {
                            bool on = !TabletConnectivity.BatterySaver;
                            TabletConnectivity.SetBatterySaver(on);
                            if (on && _presentation != null) _presentation.SetReduceMotion(true);
                            Render();
                        } };
                case "dim":
                    return new CcToggle { Id = id, Name = "tablet-wallpaper-dim-toggle", Label = "壁纸压暗", Icon = TabletIconKind.Image,
                        Active = () => _presentation != null && _presentation.WallpaperDim > 0.05f, ActiveIcon = new Color(0.30f, 0.30f, 0.38f, 1f),
                        Click = () => { if (_presentation != null) _presentation.SetWallpaperDim(_presentation.WallpaperDim > 0.05f ? 0f : 0.3f); } };
                case "clear":
                    return new CcToggle { Id = id, Name = "tablet-clear-toggle", Label = "清除通知", Icon = TabletIconKind.Trash,
                        Active = () => false, ActiveIcon = TabletTheme.InkOnGlass,
                        Click = () => _state.ClearNotifications() };
                case "mute":
                    return new CcToggle { Id = id, Name = "tablet-mute-toggle", Label = "静音", Icon = TabletIconKind.SpeakerMuted,
                        Active = () => GameData.MasterVolume <= 0.001f, ActiveIcon = new Color(0.93f, 0.30f, 0.25f, 1f),
                        Click = () =>
                        {
                            if (GameData.MasterVolume > 0.001f) { s_preMuteVolume = GameData.MasterVolume; GameData.MasterVolume = 0f; }
                            else GameData.MasterVolume = s_preMuteVolume > 0.05f ? s_preMuteVolume : 1f;
                            Render();
                        } };
                case "contrast":
                    return new CcToggle { Id = id, Name = "tablet-high-contrast-toggle", Label = "高对比度", Icon = TabletIconKind.Contrast,
                        Active = () => _presentation != null && _presentation.HighContrast, ActiveIcon = new Color(0.16f, 0.48f, 0.95f, 1f),
                        Click = () => { if (_presentation != null) _presentation.SetHighContrast(!_presentation.HighContrast); } };
                case "motion":
                    return new CcToggle { Id = id, Name = "tablet-reduce-motion-toggle", Label = "减弱动效", Icon = TabletIconKind.Bolt,
                        Active = () => _presentation != null && _presentation.ReduceMotion, ActiveIcon = new Color(0.98f, 0.60f, 0.10f, 1f),
                        Click = () => { if (_presentation != null) _presentation.SetReduceMotion(!_presentation.ReduceMotion); } };
                case "accent":
                    return new CcToggle { Id = id, Name = "tablet-accent-cycle", Label = "强调色", Icon = TabletIconKind.Palette,
                        Active = () => false, ActiveIcon = new Color(0.88f, 0.30f, 0.55f, 1f),
                        Click = () =>
                        {
                            if (_presentation == null) return;
                            TabletAccent next = _presentation.Accent == TabletAccent.Amber ? TabletAccent.Teal :
                                                _presentation.Accent == TabletAccent.Teal ? TabletAccent.Blue : TabletAccent.Amber;
                            _presentation.SetAccent(next);
                        } };
                case "screenshot":
                    return new CcToggle { Id = id, Name = "tablet-screenshot-toggle", Label = "截屏", Icon = TabletIconKind.Camera,
                        Active = () => false, ActiveIcon = TabletTheme.InkOnGlass, Click = TakeScreenshot };
                default:
                    return new CcToggle { Id = "settings", Name = "tablet-shell-settings", Label = "设置", Icon = TabletIconKind.Gear,
                        Active = () => false, ActiveIcon = TabletTheme.InkOnGlass,
                        Click = () => { _state.ClosePanel(); _openSettings?.Invoke(); } };
            }
        }

        private static System.Collections.Generic.List<string> LoadToggleOrder()
        {
            var list = new System.Collections.Generic.List<string>();
            string saved = null;
            try { if (PlayerPrefs.HasKey(TogglePrefKey)) saved = PlayerPrefs.GetString(TogglePrefKey, ""); } catch { }
            if (string.IsNullOrEmpty(saved)) { if (saved == null) list.AddRange(AllToggleIds); return list; }
            foreach (var id in saved.Split(','))
                if (Array.IndexOf(AllToggleIds, id) >= 0 && !list.Contains(id)) list.Add(id);
            return list;
        }

        private static void SaveToggleOrder(System.Collections.Generic.List<string> list)
        {
            try { PlayerPrefs.SetString(TogglePrefKey, string.Join(",", list)); PlayerPrefs.Save(); } catch { }
        }

        /// <summary>截屏是真的：先收起面板，再把整屏存成 PNG，并发一条通知说明存到了哪。</summary>
        private void TakeScreenshot()
        {
            _state.ClosePanel();
            string dir = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots");
            string file = System.IO.Path.Combine(dir, "tablet_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            _parent.schedule.Execute(() =>
            {
                try
                {
                    System.IO.Directory.CreateDirectory(dir);
                    ScreenCapture.CaptureScreenshot(file);
                    TabletNotify.PostNotice("截屏", "已保存到 " + file);
                }
                catch (Exception ex)
                {
                    TabletNotify.PostNotice("截屏失败", ex.Message);
                }
            }).StartingIn(420);
        }

        private Button BuildToggleCircle(CcToggle t, float size)
        {
            bool active = t.Active();
            var btn = new Button(t.Click) { name = t.Name, tooltip = t.Label };
            btn.AddToClassList("shell-keep");
            btn.style.width = size; btn.style.height = size;
            btn.style.marginTop = 0; btn.style.marginBottom = 0; btn.style.marginLeft = 0; btn.style.marginRight = 0;
            btn.style.paddingTop = btn.style.paddingBottom = btn.style.paddingLeft = btn.style.paddingRight = 0;
            btn.style.backgroundColor = active ? TabletTheme.GlassTileActive : (_highContrast ? Color.black : TabletTheme.GlassTile);
            btn.style.borderTopWidth = btn.style.borderBottomWidth = btn.style.borderLeftWidth = btn.style.borderRightWidth = 0;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius = size * 0.5f;
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = size * 0.5f;
            btn.style.alignItems = Align.Center; btn.style.justifyContent = Justify.Center;
            btn.Add(new TabletIconElement(t.Icon, size * 0.40f, active ? t.ActiveIcon : Color.white) { pickingMode = PickingMode.Ignore });
            btn.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("scale") };
            btn.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(TabletTheme.MotionMs(110), TimeUnit.Millisecond) };
            btn.RegisterCallback<PointerDownEvent>(_ => btn.style.scale = new Scale(new Vector3(0.90f, 0.90f, 1f)));
            btn.RegisterCallback<PointerUpEvent>(_ => btn.style.scale = new Scale(Vector3.one));
            btn.RegisterCallback<PointerLeaveEvent>(_ => btn.style.scale = new Scale(Vector3.one));
            return btn;
        }

        private VisualElement ToggleCell(Button circle, string label)
        {
            var cell = new VisualElement();
            cell.style.width = Length.Percent(25f);
            cell.style.alignItems = Align.Center;
            cell.style.marginBottom = label != null ? 14 : 12;
            cell.Add(circle);
            if (label != null)
            {
                var l = CreateLabel(label, 12, false);
                l.style.color = new Color(1f, 1f, 1f, 0.92f);
                l.style.marginTop = 6;
                l.style.unityTextAlign = TextAnchor.UpperCenter;
                l.style.whiteSpace = WhiteSpace.Normal;
                l.pickingMode = PickingMode.Ignore;
                cell.Add(l);
            }
            return cell;
        }

        private void RenderControls()
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _ctrlContent.Add(scroll);

            if (_ctrlEditing) { RenderControlsEdit(scroll); return; }

            // ---- 0. 顶部信息行：日期 + 状态图标（只放真实的状态） ----
            var info = new VisualElement();
            info.style.flexDirection = FlexDirection.Row;
            info.style.justifyContent = Justify.SpaceBetween;
            info.style.alignItems = Align.Center;
            info.style.height = 44;
            info.style.paddingLeft = 6; info.style.paddingRight = 6;
            info.style.marginBottom = 10;
            var date = CreateLabel(TabletDateTime.Now.Month + "月" + TabletDateTime.Now.Day + "日 " + TabletDateTime.WeekdayLong(TabletDateTime.Now), 17, false);
            date.style.marginTop = 0; date.style.marginBottom = 0;
            info.Add(date);
            var icons = new VisualElement { pickingMode = PickingMode.Ignore };
            icons.style.flexDirection = FlexDirection.Row;
            icons.style.alignItems = Align.Center;
            if (TabletConnectivity.Airplane) icons.Add(StatusGlyph(TabletIconKind.Airplane));
            if (_state.QuietMode) icons.Add(StatusGlyph(TabletIconKind.Moon));
            if (GameData.MasterVolume <= 0.001f) icons.Add(StatusGlyph(TabletIconKind.SpeakerMuted));
            if (TabletConnectivity.BluetoothOn) icons.Add(StatusGlyph(TabletIconKind.Bluetooth));
            if (TabletConnectivity.MobileOn) icons.Add(StatusGlyph(TabletIconKind.Cloud));
            if (TabletConnectivity.WifiOn) icons.Add(StatusGlyph(TabletIconKind.Wifi));
            info.Add(icons);
            scroll.Add(info);

            // ---- 1. 两张大卡：网络 / 勿扰 ----
            var topPillsRow = new VisualElement();
            topPillsRow.style.flexDirection = FlexDirection.Row;
            topPillsRow.style.justifyContent = Justify.SpaceBetween;
            topPillsRow.style.marginBottom = 10;
            scroll.Add(topPillsRow);

            // Wi-Fi 与移动网络（高丽联）：平板可随身携带，两项各自开关；飞行模式下都显示关闭
            bool airplane = TabletConnectivity.Airplane;
            var wifiBtn = BuildBigTile("tablet-network-tile", TabletConnectivity.WifiName, airplane ? "飞行模式" : (TabletConnectivity.WifiOn ? "已连接" : "已关闭"),
                TabletConnectivity.WifiOn, TabletIconKind.Wifi, () =>
                {
                    if (!TabletConnectivity.Airplane) TabletConnectivity.SetWifi(!TabletConnectivity.WifiSwitch);
                    Render();
                });
            wifiBtn.AddToClassList("shell-keep");
            topPillsRow.Add(wifiBtn);

            var mobileBtn = BuildBigTile("tablet-mobile-tile", TabletConnectivity.CarrierName, airplane ? "飞行模式" : (TabletConnectivity.MobileOn ? TabletConnectivity.CarrierNetwork + " · 已连接" : "已关闭"),
                TabletConnectivity.MobileOn, TabletIconKind.Cloud, () =>
                {
                    if (!TabletConnectivity.Airplane) TabletConnectivity.SetMobile(!TabletConnectivity.MobileSwitch);
                    Render();
                });
            mobileBtn.AddToClassList("shell-keep");
            topPillsRow.Add(mobileBtn);

            // ---- 2. 播控卡 + 亮度 / 音量两条竖滑条 ----
            var centerRow = new VisualElement();
            centerRow.name = "tablet-controls-center-row";
            centerRow.style.flexDirection = FlexDirection.Row;
            centerRow.style.marginBottom = 10;
            centerRow.style.height = 176;
            scroll.Add(centerRow);

            centerRow.Add(BuildMediaCard());

            // 亮度是真的：整屏压暗，下限 20%
            float bright = TabletDisplayPreferences.Brightness;
            float brightFill = Mathf.InverseLerp(TabletDisplayPreferences.MinBrightness, TabletDisplayPreferences.MaxBrightness, bright);
            var brightSlider = CreateVerticalSlider("tablet-brightness-slider", TabletIconKind.Sun, brightFill,
                () => TabletDisplayPreferences.SetBrightness(TabletDisplayPreferences.Brightness > 0.99f ? 0.6f : 1f),
                fill => TabletDisplayPreferences.SetBrightness(Mathf.Lerp(TabletDisplayPreferences.MinBrightness, TabletDisplayPreferences.MaxBrightness, fill)),
                fill => TabletDisplayPreferences.SetBrightness(Mathf.Lerp(TabletDisplayPreferences.MinBrightness, TabletDisplayPreferences.MaxBrightness, fill)));
            centerRow.Add(brightSlider);

            bool isMute = GameData.MasterVolume <= 0.001f;
            var volSlider = CreateVerticalSlider("tablet-audio-mute-toggle", isMute ? TabletIconKind.SpeakerMuted : TabletIconKind.Speaker,
                Mathf.Clamp01(GameData.MasterVolume),
                () =>
                {
                    if (GameData.MasterVolume > 0.001f)
                    {
                        s_preMuteVolume = GameData.MasterVolume;
                        GameData.MasterVolume = 0f;
                    }
                    else
                    {
                        GameData.MasterVolume = s_preMuteVolume > 0.05f ? s_preMuteVolume : 1f;
                    }
                    Render();
                },
                fill =>
                {
                    GameData.MasterVolume = fill;
                    if (fill > 0.05f) s_preMuteVolume = fill;
                    Render();
                },
                fill =>
                {
                    GameData.MasterVolume = fill;   // 拖动中就实时生效，不等松手
                    if (fill > 0.05f) s_preMuteVolume = fill;
                });
            centerRow.Add(volSlider);

            // ---- 3. 圆形快捷开关（4 列）：只放真正有效的开关，可在“编辑”里增减 ----
            var grid = new VisualElement { name = "tablet-controls-toggle-grid" };
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.marginTop = 8;
            foreach (var id in LoadToggleOrder())
            {
                var t = MakeToggle(id);
                grid.Add(ToggleCell(BuildToggleCircle(t, 58f), null));
            }
            scroll.Add(grid);

            // ---- 4. 编辑 ----
            var editRow = new VisualElement();
            editRow.style.alignItems = Align.Center;
            editRow.style.marginBottom = 12;
            var editBtn = CreatePillButton("编辑", 84, 38, () => { _ctrlEditing = true; Render(); });
            editBtn.name = "tablet-controls-edit";
            editBtn.AddToClassList("shell-keep");
            editBtn.style.backgroundColor = new Color(0f, 0f, 0f, 0.28f);
            editBtn.style.borderTopWidth = editBtn.style.borderBottomWidth = editBtn.style.borderLeftWidth = editBtn.style.borderRightWidth = 0;
            editBtn.style.borderTopLeftRadius = editBtn.style.borderTopRightRadius = 19;
            editBtn.style.borderBottomLeftRadius = editBtn.style.borderBottomRightRadius = 19;
            editBtn.style.fontSize = 14;
            editRow.Add(editBtn);
            scroll.Add(editRow);
        }

        private VisualElement StatusGlyph(TabletIconKind kind)
        {
            var g = new TabletIconElement(kind, 18, new Color(1f, 1f, 1f, 0.92f)) { pickingMode = PickingMode.Ignore };
            g.style.marginLeft = 10;
            return g;
        }

        /// <summary>“编辑与排序”：上半是已启用的开关（红色减号移除），下半是可添加的开关（蓝色加号）。</summary>
        private void RenderControlsEdit(ScrollView scroll)
        {
            var order = LoadToggleOrder();

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row;
            head.style.justifyContent = Justify.SpaceBetween;
            head.style.alignItems = Align.Center;
            head.style.marginBottom = 18;
            head.style.paddingLeft = 6;
            var title = CreateLabel("编辑与排序", 26, false);
            title.style.marginTop = 0; title.style.marginBottom = 0;
            head.Add(title);
            var done = new Button(() => { _ctrlEditing = false; Render(); }) { name = "tablet-controls-edit-done", tooltip = "完成" };
            done.AddToClassList("shell-keep");
            done.style.width = 92; done.style.height = 44;
            done.style.marginTop = 0; done.style.marginBottom = 0; done.style.marginLeft = 0; done.style.marginRight = 0;
            done.style.paddingTop = done.style.paddingBottom = done.style.paddingLeft = done.style.paddingRight = 0;
            done.style.backgroundColor = new Color(0f, 0f, 0f, 0.28f);
            done.style.borderTopWidth = done.style.borderBottomWidth = done.style.borderLeftWidth = done.style.borderRightWidth = 0;
            done.style.borderTopLeftRadius = done.style.borderTopRightRadius = 22;
            done.style.borderBottomLeftRadius = done.style.borderBottomRightRadius = 22;
            done.style.alignItems = Align.Center; done.style.justifyContent = Justify.Center;
            done.Add(new TabletIconElement(TabletIconKind.Check, 22, Color.white) { pickingMode = PickingMode.Ignore });
            head.Add(done);
            scroll.Add(head);

            var grid = new VisualElement { name = "tablet-controls-edit-grid" };
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            foreach (var id in order)
            {
                var t = MakeToggle(id);
                string captured = id;
                var circle = BuildToggleCircle(t, 58f);
                circle.clicked -= t.Click;
                circle.clicked += () => { var o = LoadToggleOrder(); o.Remove(captured); SaveToggleOrder(o); Render(); };
                circle.name = "tablet-edit-remove-" + id;
                circle.Add(EditBadge(false));
                grid.Add(ToggleCell(circle, t.Label));
            }
            scroll.Add(grid);

            var rest = new System.Collections.Generic.List<string>();
            foreach (var id in AllToggleIds) if (!order.Contains(id)) rest.Add(id);

            var divider = new VisualElement { pickingMode = PickingMode.Ignore };
            divider.style.height = 1; divider.style.backgroundColor = new Color(1f, 1f, 1f, 0.16f);
            divider.style.marginTop = 6; divider.style.marginBottom = 18;
            scroll.Add(divider);

            var sec = CreateLabel("更多开关", 16, false);
            sec.style.color = new Color(1f, 1f, 1f, 0.78f);
            sec.style.marginLeft = 6; sec.style.marginBottom = 14; sec.style.marginTop = 0;
            scroll.Add(sec);

            if (rest.Count == 0)
            {
                var none = CreateLabel("所有开关都已在控制中心里", 13, false);
                none.style.color = new Color(1f, 1f, 1f, 0.6f);
                none.style.marginLeft = 6;
                scroll.Add(none);
                return;
            }
            var restGrid = new VisualElement { name = "tablet-controls-edit-more" };
            restGrid.style.flexDirection = FlexDirection.Row;
            restGrid.style.flexWrap = Wrap.Wrap;
            foreach (var id in rest)
            {
                var t = MakeToggle(id);
                string captured = id;
                var circle = BuildToggleCircle(t, 58f);
                circle.clicked -= t.Click;
                circle.clicked += () => { var o = LoadToggleOrder(); if (!o.Contains(captured)) o.Add(captured); SaveToggleOrder(o); Render(); };
                circle.name = "tablet-edit-add-" + id;
                circle.Add(EditBadge(true));
                restGrid.Add(ToggleCell(circle, t.Label));
            }
            scroll.Add(restGrid);
        }

        /// <summary>圆开关右上角的红色减号 / 蓝色加号角标。</summary>
        private static VisualElement EditBadge(bool add)
        {
            var badge = new VisualElement { pickingMode = PickingMode.Ignore };
            badge.style.position = Position.Absolute;
            badge.style.right = -2; badge.style.top = -2;
            badge.style.width = 26; badge.style.height = 26;
            badge.style.borderTopLeftRadius = badge.style.borderTopRightRadius = 13;
            badge.style.borderBottomLeftRadius = badge.style.borderBottomRightRadius = 13;
            badge.style.backgroundColor = add ? new Color(0.25f, 0.46f, 0.96f, 1f) : new Color(0.92f, 0.27f, 0.25f, 1f);
            badge.style.alignItems = Align.Center; badge.style.justifyContent = Justify.Center;
            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.style.position = Position.Absolute; bar.style.width = 12; bar.style.height = 2.5f;
            bar.style.backgroundColor = Color.white;
            badge.Add(bar);
            if (add)
            {
                var v = new VisualElement { pickingMode = PickingMode.Ignore };
                v.style.position = Position.Absolute; v.style.width = 2.5f; v.style.height = 12;
                v.style.backgroundColor = Color.white;
                badge.Add(v);
            }
            return badge;
        }

        private VisualElement BuildMediaCard()
        {
            var mediaCard = new VisualElement { name = "tablet-media-card" };
            mediaCard.style.flexGrow = 1;
            mediaCard.style.flexShrink = 1;
            mediaCard.style.height = 176;
            mediaCard.style.borderTopLeftRadius = mediaCard.style.borderTopRightRadius = 28;
            mediaCard.style.borderBottomLeftRadius = mediaCard.style.borderBottomRightRadius = 28;
            mediaCard.style.backgroundColor = _highContrast ? Color.black : TabletTheme.GlassTile;
            mediaCard.style.paddingLeft = 14; mediaCard.style.paddingRight = 14;
            mediaCard.style.paddingTop = 14; mediaCard.style.paddingBottom = 12;
            mediaCard.style.justifyContent = Justify.SpaceBetween;

            bool playing = RailwayRenaissance.UI.Tablet.Apps.TabletMusicAppUI.tabletIsPlaying;
            string track = RailwayRenaissance.UI.Tablet.Apps.TabletMusicAppUI.tabletCurrentTrackTitle;
            bool hasTrack = !string.IsNullOrEmpty(track);

            // 中部：曲名（没有在播就是“未在播放”）
            var center = new VisualElement { pickingMode = PickingMode.Ignore };
            center.style.flexGrow = 1;
            center.style.alignItems = Align.Center; center.style.justifyContent = Justify.Center;
            var songLbl = CreateLabel(hasTrack ? track : "未在播放", hasTrack ? 14 : 16, hasTrack);
            songLbl.pickingMode = PickingMode.Ignore;
            songLbl.style.overflow = Overflow.Hidden;
            songLbl.style.textOverflow = TextOverflow.Ellipsis;
            songLbl.style.whiteSpace = WhiteSpace.NoWrap;
            songLbl.style.maxWidth = Length.Percent(100);
            songLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            songLbl.style.color = hasTrack ? Color.white : new Color(1f, 1f, 1f, 0.72f);
            center.Add(songLbl);
            if (hasTrack)
            {
                var artLbl = CreateLabel("铁道随身听", 11, false);
                artLbl.pickingMode = PickingMode.Ignore;
                artLbl.style.color = TabletTheme.TextSecondary;
                artLbl.style.marginTop = 2;
                center.Add(artLbl);
            }
            mediaCard.Add(center);

            // 底部：上一首 / 播放暂停 / 下一首，横向铺开
            var transport = new VisualElement();
            transport.style.flexDirection = FlexDirection.Row;
            transport.style.justifyContent = Justify.SpaceAround;
            transport.style.alignItems = Align.Center;
            transport.Add(TransportBtn("tablet-media-prev", TabletTransportKind.Prev, 22, () =>
            {
                RailwayRenaissance.UI.Tablet.Apps.TabletMusicAppUI.PlayPrevTabletTrack();
                Render();
            }));
            var playBtn = TransportBtn("tablet-media-play-toggle", playing ? TabletTransportKind.Pause : TabletTransportKind.Play, 28, () =>
            {
                RailwayRenaissance.UI.Tablet.Apps.TabletMusicAppUI.ToggleTabletMusicPlayPause();
                Render();
            });
            transport.Add(playBtn);
            transport.Add(TransportBtn("tablet-media-next", TabletTransportKind.Next, 22, () =>
            {
                RailwayRenaissance.UI.Tablet.Apps.TabletMusicAppUI.PlayNextTabletTrack();
                Render();
            }));
            mediaCard.Add(transport);
            return mediaCard;
        }

        private Button TransportBtn(string name, TabletTransportKind kind, float iconSize, Action onClick)
        {
            var btn = new Button(onClick) { name = name };
            btn.AddToClassList("shell-keep");
            btn.style.width = 52; btn.style.height = 40;
            btn.style.marginTop = 0; btn.style.marginBottom = 0; btn.style.marginLeft = 0; btn.style.marginRight = 0;
            btn.style.paddingTop = btn.style.paddingBottom = btn.style.paddingLeft = btn.style.paddingRight = 0;
            btn.style.backgroundColor = Color.clear;
            btn.style.borderTopWidth = btn.style.borderBottomWidth = btn.style.borderLeftWidth = btn.style.borderRightWidth = 0;
            btn.style.alignItems = Align.Center; btn.style.justifyContent = Justify.Center;
            btn.Add(new TabletTransportIcon(kind, iconSize, Color.white));
            btn.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("scale") };
            btn.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(TabletTheme.MotionMs(110), TimeUnit.Millisecond) };
            btn.RegisterCallback<PointerDownEvent>(_ => btn.style.scale = new Scale(new Vector3(0.86f, 0.86f, 1f)));
            btn.RegisterCallback<PointerUpEvent>(_ => btn.style.scale = new Scale(Vector3.one));
            btn.RegisterCallback<PointerLeaveEvent>(_ => btn.style.scale = new Scale(Vector3.one));
            return btn;
        }

        /// <summary>
        /// 竖向胶囊滑条：点击切换，纵向拖动调节；拖动过程只改填充高度，松手时一次性提交，避免重建界面打断拖动。
        /// </summary>
        private Button CreateVerticalSlider(string name, TabletIconKind icon, float initialFill, Action onToggle, Action<float> onCommit, Action<float> onLive = null)
        {
            float fillValue = Mathf.Clamp01(initialFill);
            bool down = false, dragged = false;
            float startY = 0f;

            var btn = new Button(() =>
            {
                if (dragged) return;
                onToggle?.Invoke();
            }) { name = name };
            btn.AddToClassList("shell-keep");
            btn.style.width = 74;
            btn.style.height = 176;
            btn.style.flexShrink = 0;
            btn.style.marginLeft = 10;
            btn.style.paddingLeft = btn.style.paddingRight = btn.style.paddingTop = btn.style.paddingBottom = 0;
            btn.style.backgroundColor = _highContrast ? Color.black : TabletTheme.GlassTile;
            btn.style.borderTopWidth = btn.style.borderBottomWidth = btn.style.borderLeftWidth = btn.style.borderRightWidth = 0;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius = 28;
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 28;
            btn.style.overflow = Overflow.Hidden;

            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.style.position = Position.Absolute;
            fill.style.left = 0; fill.style.right = 0; fill.style.bottom = 0;
            fill.style.height = Length.Percent(fillValue * 100f);
            fill.style.backgroundColor = TabletTheme.GlassTileActive;
            btn.Add(fill);

            var iconLabel = new TabletIconElement(icon, 26f, SliderIconColor(icon, fillValue));
            iconLabel.style.position = Position.Absolute;
            iconLabel.style.left = Length.Percent(50);
            iconLabel.style.translate = new Translate(Length.Percent(-50), 0);
            iconLabel.style.bottom = 16;
            btn.Add(iconLabel);

            // 指针处理：按下就独占指针（外层滚动区、翻页手势都抢不走）；任何一种结束方式（抬起、取消、被抢走）
            // 都走同一个收尾，状态不会卡在“按着”；位置取屏幕坐标，不受缩放和悬停动画影响
            int activeId = -1;

            void SetFromY(float y)
            {
                Rect wb = btn.worldBound;
                float h = Mathf.Max(1f, wb.height);
                fillValue = Mathf.Clamp01(1f - (y - wb.yMin) / h);
                fill.style.height = Length.Percent(fillValue * 100f);
                iconLabel.IconColor = SliderIconColor(icon, fillValue);
                onLive?.Invoke(fillValue);
            }

            void Finish(bool commit)
            {
                if (activeId == -1) return;
                int id = activeId;
                activeId = -1;
                down = false;
                if (btn.HasPointerCapture(id)) btn.ReleasePointer(id);
                bool wasDragged = dragged;
                if (wasDragged && commit) onCommit?.Invoke(fillValue);
                _sliderDragging = false;
                if (_renderPending) { _renderPending = false; Render(); }
                // 点击回调在抬起之后才触发，保留 dragged 到那之后再清
                if (wasDragged) btn.schedule.Execute(() => dragged = false).ExecuteLater(0);
            }

            btn.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                if (activeId != -1) Finish(false);
                activeId = e.pointerId;
                down = true; dragged = false; startY = e.position.y;
                btn.CapturePointer(e.pointerId);
                e.StopPropagation();
            });
            btn.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (e.pointerId != activeId) return;
                if (!dragged && Mathf.Abs(e.position.y - startY) > 6f)
                {
                    dragged = true;
                    _sliderDragging = true;
                }
                if (!dragged) return;
                SetFromY(e.position.y);
                e.StopPropagation();
            });
            btn.RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.pointerId != activeId) return;
                Finish(true);
                e.StopPropagation();
            });
            btn.RegisterCallback<PointerCancelEvent>(e => { if (e.pointerId == activeId) Finish(true); });
            btn.RegisterCallback<PointerCaptureOutEvent>(e => { if (e.pointerId == activeId) Finish(true); });
            btn.RegisterCallback<DetachFromPanelEvent>(_ => { if (activeId != -1) Finish(false); });
            return btn;
        }

        private static void AttachLauncherIcon(Button btn, TabletIconKind kind)
        {
            btn.style.paddingLeft = 26;
            var icon = new TabletIconElement(kind, 15f, Color.white);
            icon.style.position = Position.Absolute;
            icon.style.left = 10;
            icon.style.top = Length.Percent(50);
            icon.style.translate = new Translate(0, Length.Percent(-50));
            btn.Add(icon);
        }

        // 大胶囊磁贴：图标 + 标题/状态两行文字（用两个 Label 控制行距，不用按钮自带的多行文本）
        private Button BuildBigTile(string name, string title, string status, bool active, TabletIconKind icon, Action onClick)
        {
            var btn = CreateTileButton(string.Empty, name, active, onClick);
            StyleBigTile(btn);
            btn.style.backgroundColor = active ? TabletTheme.GlassTileActive : (_highContrast ? Color.black : TabletTheme.GlassTile);
            btn.style.flexDirection = FlexDirection.Row;
            btn.style.alignItems = Align.Center;
            var column = new VisualElement { pickingMode = PickingMode.Ignore };
            var titleLabel = CreateLabel(title, 14, true);
            titleLabel.pickingMode = PickingMode.Ignore;
            titleLabel.AddToClassList("shell-dark");
            titleLabel.style.color = active ? TabletTheme.InkOnGlass : Color.white;
            var statusLabel = CreateLabel(status, 11, false);
            statusLabel.pickingMode = PickingMode.Ignore;
            statusLabel.AddToClassList("shell-dark");
            statusLabel.style.color = active ? TabletTheme.InkOnGlassSoft : TabletTheme.TextSecondary;
            statusLabel.style.marginTop = 3;
            column.Add(titleLabel);
            column.Add(statusLabel);
            btn.Add(column);
            AttachTileIcon(btn, icon, active, false);
            return btn;
        }

        // 开启态的磁贴图标带颜色（对照参考图：蓝牙蓝、静音红、手电白底深色），其余保持深色
        private static Color ActiveIconColor(TabletIconKind kind)
        {
            switch (kind)
            {
                case TabletIconKind.Moon: return new Color(0.40f, 0.36f, 0.95f, 1f);
                case TabletIconKind.Bolt: return new Color(0.98f, 0.60f, 0.10f, 1f);
                case TabletIconKind.Palette: return new Color(0.88f, 0.30f, 0.55f, 1f);
                case TabletIconKind.Wifi: return new Color(0.16f, 0.48f, 0.95f, 1f);
                case TabletIconKind.Cloud: return new Color(0.16f, 0.48f, 0.95f, 1f);
                case TabletIconKind.Contrast: return new Color(0.16f, 0.48f, 0.95f, 1f);
                default: return TabletTheme.InkOnGlass;
            }
        }

        // 竖向滑条图标：填充处于白色时使用彩色图标（太阳橙、喇叭蓝），空条时用白色
        private static Color SliderIconColor(TabletIconKind kind, float fill)
        {
            if (fill <= 0.14f) return Color.white;
            switch (kind)
            {
                case TabletIconKind.Sun: return new Color(0.98f, 0.62f, 0.08f, 1f);
                case TabletIconKind.Speaker: return new Color(0.16f, 0.48f, 0.95f, 1f);
                default: return TabletTheme.InkOnGlass;
            }
        }

        private static void AttachTileIcon(Button btn, TabletIconKind kind, bool active, bool round)
        {
            var icon = new TabletIconElement(kind, round ? 28f : 28f, active ? ActiveIconColor(kind) : Color.white);
            icon.style.position = Position.Absolute;
            if (round)
            {
                icon.style.top = Length.Percent(50);
                icon.style.left = Length.Percent(50);
                icon.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            }
            else
            {
                icon.style.left = 18;
                icon.style.top = Length.Percent(50);
                icon.style.translate = new Translate(0, Length.Percent(-50));
            }
            btn.Add(icon);
        }

        private static void StyleBigTile(Button btn)
        {
            btn.style.width = Length.Percent(48.5f);
            btn.style.height = 76;
            btn.style.minHeight = 76;
            btn.style.fontSize = 13;
            btn.style.whiteSpace = WhiteSpace.Normal;
            btn.style.unityTextAlign = TextAnchor.MiddleLeft;
            btn.style.paddingLeft = 56;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius = 30;
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 30;
        }

        private static void StyleRoundTile(Button btn)
        {
            btn.style.width = 68;
            btn.style.height = 68;
            btn.style.minWidth = 68;
            btn.style.minHeight = 68;
            btn.style.fontSize = 11;
            btn.style.whiteSpace = WhiteSpace.Normal;
            btn.style.unityTextAlign = TextAnchor.LowerCenter;
            btn.style.paddingBottom = 12;
            btn.style.paddingLeft = btn.style.paddingRight = 2;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius = 34;
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 34;
        }

        private Button CreateTileButton(string text, string name, bool active, Action onClick)
        {
            var btn = CreatePillButton(text, 120, 44, onClick);
            btn.name = name;
            btn.style.fontSize = 12;
            if (active)
            {
                btn.style.backgroundColor = TabletTheme.ActiveFill;
            }
            return btn;
        }

        private Button CreatePillButton(string text, float minWidth, float minHeight, Action onClick)
        {
            var btn = new Button(onClick) { text = text };
            btn.style.minWidth = minWidth;
            btn.style.minHeight = minHeight;
            btn.style.backgroundColor = TabletTheme.OverlayFaint;
            btn.style.color = Color.white;
            btn.style.borderTopWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1;
            var bc = TabletTheme.OverlaySoft;
            btn.style.borderTopColor = bc;
            btn.style.borderRightColor = bc;
            btn.style.borderBottomColor = bc;
            btn.style.borderLeftColor = bc;
            btn.style.borderTopLeftRadius = 16;
            btn.style.borderTopRightRadius = 16;
            btn.style.borderBottomLeftRadius = 16;
            btn.style.borderBottomRightRadius = 16;
            ApplyFont(btn);
            return btn;
        }

        private Label CreateLabel(string text, int fontSize, bool bold)
        {
            var label = new Label(text);
            label.style.fontSize = fontSize;
            label.style.color = Color.white;
            if (bold)
            {
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            ApplyFont(label);
            return label;
        }

        private Button CreateButton(string text, float minWidth, float minHeight, Action onClick)
        {
            var btn = new Button(onClick) { text = text };
            btn.style.minWidth = minWidth;
            btn.style.minHeight = minHeight;
            btn.style.backgroundColor = TabletTheme.OverlayFaint;
            btn.style.color = Color.white;
            btn.style.borderTopWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1;
            var bc = TabletTheme.OverlaySoft;
            btn.style.borderTopColor = bc;
            btn.style.borderRightColor = bc;
            btn.style.borderBottomColor = bc;
            btn.style.borderLeftColor = bc;
            btn.style.borderTopLeftRadius = 12;
            btn.style.borderTopRightRadius = 12;
            btn.style.borderBottomLeftRadius = 12;
            btn.style.borderBottomRightRadius = 12;
            ApplyFont(btn);
            return btn;
        }

        private void ApplyFont(VisualElement element)
        {
            if (_font == null) return;
            element.style.unityFont = _font;
            element.style.unityFontDefinition = FontDefinition.FromFont(_font);
        }
    }
}
