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
        private readonly VisualElement _panel;
        private readonly Label _panelTitle;
        private readonly Label _panelSub;
        private readonly VisualElement _contentContainer;
        private readonly Label _badge;
        private Focusable _previousFocus;
        private TabletSystemPanel _renderedPanel;
        private Color _accent = TabletTheme.Accent;
        private bool _highContrast;
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

            var btnNotif = CreatePillButton("🔔 通知", 62, 32, () => _state.TogglePanel(TabletSystemPanel.Notifications));
            var btnCtrl = CreatePillButton("⚙ 控制", 62, 32, () => _state.TogglePanel(TabletSystemPanel.Controls));
            btnNotif.name = "tablet-notifications-open";
            btnCtrl.name = "tablet-controls-open";
            btnNotif.style.marginRight = 6;

            _badge = new Label { name = "tablet-notifications-badge" };
            _badge.style.position = Position.Absolute;
            _badge.style.top = -2;
            _badge.style.right = -2;
            _badge.style.minWidth = 16;
            _badge.style.height = 16;
            _badge.style.borderTopLeftRadius = 8;
            _badge.style.borderTopRightRadius = 8;
            _badge.style.borderBottomLeftRadius = 8;
            _badge.style.borderBottomRightRadius = 8;
            _badge.style.backgroundColor = TabletTheme.Badge;
            _badge.style.color = Color.white;
            _badge.style.fontSize = 10;
            _badge.style.unityFontStyleAndWeight = FontStyle.Bold;
            _badge.style.unityTextAlign = TextAnchor.MiddleCenter;
            _badge.style.paddingLeft = 3;
            _badge.style.paddingRight = 3;
            _badge.pickingMode = PickingMode.Ignore;
            _badge.style.display = DisplayStyle.None;
            ApplyFont(_badge);
            btnNotif.Add(_badge);

            _launcher.Add(btnNotif);
            _launcher.Add(btnCtrl);

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
            _panel.style.top = 52;
            _panel.style.bottom = 12;
            _panel.style.left = 0;
            _panel.style.right = StyleKeyword.Auto;
            _panel.style.width = 380;
            _panel.style.maxWidth = Length.Percent(100);
            _panel.style.backgroundColor = Color.clear;   // 内容直接浮在整屏模糊背景上，由卡片承载材质
            _panel.style.borderTopLeftRadius = 8;
            _panel.style.borderTopRightRadius = 8;
            _panel.style.borderBottomLeftRadius = 8;
            _panel.style.borderBottomRightRadius = 8;
            _panel.style.paddingTop = 12;
            _panel.style.paddingRight = 12;
            _panel.style.paddingBottom = 12;
            _panel.style.paddingLeft = 12;
            _panel.pickingMode = PickingMode.Ignore;      // 列内空白处点击落到遮罩上关闭面板
            _panel.focusable = true;
            _panel.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            _panel.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());

            var panelHeader = new VisualElement();
            panelHeader.style.flexDirection = FlexDirection.Row;
            panelHeader.style.justifyContent = Justify.SpaceBetween;
            panelHeader.style.alignItems = Align.Center;
            panelHeader.style.marginBottom = 10;

            var titleBox = new VisualElement();
            _panelTitle = CreateLabel("系统面板", 15, true);
            _panelSub = CreateLabel(string.Empty, 13, false);
            _panelSub.style.color = TabletTheme.TextStrong;
            titleBox.Add(_panelTitle);
            titleBox.Add(_panelSub);
            var btnClose = CreateButton("✕", 44, 44, () => _state.ClosePanel());
            btnClose.name = "tablet-shell-close";
            btnClose.AddToClassList("shell-keep");
            btnClose.style.width = 44; btnClose.style.height = 44;
            btnClose.style.backgroundColor = TabletTheme.GlassTileActive;
            btnClose.style.color = TabletTheme.InkOnGlass;
            btnClose.style.borderTopWidth = 0; btnClose.style.borderBottomWidth = 0;
            btnClose.style.borderLeftWidth = 0; btnClose.style.borderRightWidth = 0;
            btnClose.style.borderTopLeftRadius = 22; btnClose.style.borderTopRightRadius = 22;
            btnClose.style.borderBottomLeftRadius = 22; btnClose.style.borderBottomRightRadius = 22;
            panelHeader.Add(titleBox);
            panelHeader.Add(btnClose);
            _panel.Add(panelHeader);

            _contentContainer = new VisualElement();
            _contentContainer.style.flexGrow = 1;
            _panel.Add(_contentContainer);

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
                    _blurBackground.style.backgroundImage = Background.FromRenderTexture(blurredTexture);
                    _blurBackground.style.display = DisplayStyle.Flex;
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
                    button.style.color = Color.white;
                    button.style.backgroundColor = TabletTheme.OverlaySoft;
                    button.style.borderTopColor = button.style.borderBottomColor = button.style.borderLeftColor = button.style.borderRightColor = TabletTheme.OverlayMedium;
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

        private void Render()
        {
            UpdateBadge();

            if (_state.Panel == TabletSystemPanel.None)
            {
                _overlay.style.display = DisplayStyle.None;
                _overlay.style.opacity = 0f;
                ClearBlurredBackground();
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
            if (opening) _previousFocus = _parent.panel?.focusController.focusedElement;
            _renderedPanel = _state.Panel;
            _overlay.style.display = DisplayStyle.Flex;
            _contentContainer.Clear();

            if (_state.Panel == TabletSystemPanel.Notifications)
            {
                _panelTitle.text = DateTime.Now.ToString("HH:mm");
                _panelTitle.style.fontSize = 44;
                _panelSub.text = DateTime.Now.ToString("M月d日 dddd");
                RenderNotifications();
            }
            else if (_state.Panel == TabletSystemPanel.Controls)
            {
                _panelTitle.text = "控制中心";
                _panelTitle.style.fontSize = 20;
                _panelSub.text = DateTime.Now.ToString("M月d日 dddd");
                RenderControls();
            }
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

        /// <summary>面板从状态栏方向轻轻落下并淡入；开启“减少动效”时直接到位。</summary>
        private void PlayPanelOpenAnimation()
        {
            int ms = TabletTheme.MotionMs(240);
            _panel.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("translate"), new StylePropertyName("opacity") };
            _panel.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(ms, TimeUnit.Millisecond) };
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
            bool notifications = _state.Panel == TabletSystemPanel.Notifications;

            // 通知页：整屏模糊上居中一列（约 47% 宽）；控制中心：靠右一列（约 31% 宽）。
            float columnWidth = notifications
                ? Mathf.Clamp(width * 0.47f, compact ? 340f : 420f, 640f)
                : Mathf.Clamp(width * 0.31f, compact ? 340f : 380f, 480f);
            if (fullSheet) columnWidth = width - 24f;
            columnWidth = Mathf.Min(columnWidth, width);

            _panel.style.width = columnWidth;
            _panel.style.maxWidth = Length.Percent(100);
            if (notifications || fullSheet)
            {
                _panel.style.left = Mathf.Max(0f, (width - columnWidth) * 0.5f);
                _panel.style.right = StyleKeyword.Auto;
            }
            else
            {
                _panel.style.left = StyleKeyword.Auto;
                _panel.style.right = 16;
            }
            _panel.style.top = 52;
            _panel.style.bottom = 12;
            _panel.style.borderTopLeftRadius = 22;
            _panel.style.borderTopRightRadius = 22;
            _panel.style.borderBottomLeftRadius = 22;
            _panel.style.borderBottomRightRadius = 22;
            _panel.style.borderTopWidth = 0; _panel.style.borderLeftWidth = 0;
            _panel.style.borderRightWidth = 0; _panel.style.borderBottomWidth = 0;
            _panel.style.paddingLeft = 0;
            _panel.style.paddingRight = 0;
            _panel.style.paddingTop = 4;
            _panel.style.paddingBottom = 0;
        }

        private void UpdateBadge()
        {
            if (_badge == null) return;
            int unread = _state.UnreadCount;
            var btnNotif = _launcher?.Q<Button>("tablet-notifications-open");
            if (unread > 0)
            {
                _badge.style.display = DisplayStyle.Flex;
                _badge.text = unread > 99 ? "99+" : unread.ToString();
                _badge.tooltip = $"{unread}条未读通知";
                if (btnNotif != null) btnNotif.tooltip = $"通知 ({_badge.text} 未读)";
            }
            else
            {
                _badge.style.display = DisplayStyle.None;
                _badge.text = string.Empty;
                _badge.tooltip = string.Empty;
                if (btnNotif != null) btnNotif.tooltip = "通知";
            }
        }

        private void RenderNotifications()
        {
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.justifyContent = Justify.FlexEnd;
            toolbar.style.marginBottom = 10;

            var clearBtn = CreateButton("全部清除", 88, 44, () => _state.ClearNotifications());
            clearBtn.name = "tablet-notifications-clear";
            clearBtn.AddToClassList("shell-keep");
            clearBtn.style.backgroundColor = TabletTheme.GlassCardLight;
            clearBtn.style.color = TabletTheme.InkOnGlass;
            clearBtn.style.borderTopWidth = clearBtn.style.borderBottomWidth = clearBtn.style.borderLeftWidth = clearBtn.style.borderRightWidth = 0;
            clearBtn.style.borderTopLeftRadius = clearBtn.style.borderTopRightRadius = clearBtn.style.borderBottomLeftRadius = clearBtn.style.borderBottomRightRadius = 22;
            clearBtn.SetEnabled(_state.Notifications.Count > 0);
            toolbar.Add(clearBtn);
            _contentContainer.Add(toolbar);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _contentContainer.Add(scroll);

            var list = _state.Notifications;
            if (list.Count == 0)
            {
                var empty = CreateLabel("暂无通知", 14, false);
                empty.style.marginTop = 24;
                empty.style.unityTextAlign = TextAnchor.MiddleCenter;
                empty.style.color = TabletTheme.TextStrong;
                empty.AddToClassList("shell-dark");
                scroll.Add(empty);
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var card = new VisualElement();
                card.style.backgroundColor = _highContrast ? Color.white : TabletTheme.GlassCardLight;
                card.style.borderTopLeftRadius = card.style.borderTopRightRadius = 22;
                card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 22;
                card.style.paddingTop = 12;
                card.style.paddingRight = 6;
                card.style.paddingBottom = 12;
                card.style.paddingLeft = 16;
                card.style.marginBottom = 10;

                var cardHeader = new VisualElement();
                cardHeader.style.flexDirection = FlexDirection.Row;
                cardHeader.style.justifyContent = Justify.SpaceBetween;
                cardHeader.style.alignItems = Align.Center;

                var titleLabel = CreateLabel(item.Title, 14, true);
                titleLabel.AddToClassList("shell-dark");
                titleLabel.style.color = TabletTheme.InkOnGlass;
                titleLabel.style.flexGrow = 1;
                titleLabel.style.flexShrink = 1;
                titleLabel.style.whiteSpace = WhiteSpace.Normal;

                var timeLabel = CreateLabel(item.CreatedAt.ToString("HH:mm"), 11, false);
                timeLabel.AddToClassList("shell-dark");
                timeLabel.style.color = TabletTheme.InkOnGlassSoft;
                timeLabel.style.marginRight = 2;

                long dismissId = item.Id;
                var dismissBtn = CreateButton("✕", 44, 44, () => _state.Dismiss(dismissId));
                dismissBtn.name = "tablet-notification-dismiss-" + dismissId;
                dismissBtn.AddToClassList("shell-keep");
                dismissBtn.style.backgroundColor = Color.clear;
                dismissBtn.style.color = TabletTheme.InkOnGlassSoft;
                dismissBtn.style.borderTopWidth = dismissBtn.style.borderBottomWidth = dismissBtn.style.borderLeftWidth = dismissBtn.style.borderRightWidth = 0;

                cardHeader.Add(titleLabel);
                cardHeader.Add(timeLabel);
                cardHeader.Add(dismissBtn);
                card.Add(cardHeader);

                if (!string.IsNullOrEmpty(item.Body))
                {
                    var bodyLabel = CreateLabel(item.Body, 12, false);
                    bodyLabel.AddToClassList("shell-dark");
                    bodyLabel.style.color = TabletTheme.InkOnGlassSoft;
                    bodyLabel.style.marginRight = 10;
                    bodyLabel.style.whiteSpace = WhiteSpace.Normal;
                    card.Add(bodyLabel);
                }

                scroll.Add(card);
            }
        }

        private void RenderControls()
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _contentContainer.Add(scroll);

            // ---- 1. 两张大胶囊：勿扰 / 高对比度 ----
            var topPillsRow = new VisualElement();
            topPillsRow.style.flexDirection = FlexDirection.Row;
            topPillsRow.style.justifyContent = Justify.SpaceBetween;
            topPillsRow.style.marginBottom = 10;
            scroll.Add(topPillsRow);

            var quietBtn = CreateTileButton(_state.QuietMode ? "勿扰\n已开启" : "勿扰\n已关闭", "tablet-quiet-toggle", _state.QuietMode, () =>
            {
                _state.SetQuietMode(!_state.QuietMode);
            });
            StyleBigTile(quietBtn);
            AttachTileIcon(quietBtn, TabletIconKind.Moon, _state.QuietMode, false);
            topPillsRow.Add(quietBtn);

            bool hc = _presentation != null && _presentation.HighContrast;
            var hcBtn = CreateTileButton(hc ? "高对比\n已开启" : "高对比\n已关闭", "tablet-high-contrast-toggle", hc, () =>
            {
                if (_presentation != null) _presentation.SetHighContrast(!_presentation.HighContrast);
            });
            StyleBigTile(hcBtn);
            AttachTileIcon(hcBtn, TabletIconKind.Contrast, hc, false);
            topPillsRow.Add(hcBtn);

            // ---- 2. 播控卡 + 两条竖向滑条（亮度 / 音量） ----
            var centerRow = new VisualElement();
            centerRow.name = "tablet-controls-center-row";
            centerRow.style.flexDirection = FlexDirection.Row;
            centerRow.style.marginBottom = 10;
            centerRow.style.height = 164;
            scroll.Add(centerRow);

            var mediaCard = BuildMediaCard();
            centerRow.Add(mediaCard);

            float dimValue = _presentation != null ? _presentation.WallpaperDim : 0f;
            var dimSlider = CreateVerticalSlider("tablet-wallpaper-dim-toggle", TabletIconKind.Sun,
                1f - Mathf.Clamp01(dimValue / 0.4f),
                () =>
                {
                    if (_presentation != null)
                        _presentation.SetWallpaperDim(_presentation.WallpaperDim > 0.05f ? 0f : 0.3f);
                },
                fill =>
                {
                    if (_presentation != null) _presentation.SetWallpaperDim((1f - fill) * 0.4f);
                });
            centerRow.Add(dimSlider);

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
                });
            centerRow.Add(volSlider);

            // ---- 3. 圆形快捷开关：动效 / 强调色 / 设置 ----
            var roundRow = new VisualElement();
            roundRow.style.flexDirection = FlexDirection.Row;
            roundRow.style.justifyContent = Justify.SpaceBetween;
            roundRow.style.marginBottom = 10;
            scroll.Add(roundRow);

            bool rm = _presentation != null && _presentation.ReduceMotion;
            var rmBtn = CreateTileButton(rm ? "动效减弱" : "动效完整", "tablet-reduce-motion-toggle", rm, () =>
            {
                if (_presentation != null) _presentation.SetReduceMotion(!_presentation.ReduceMotion);
            });
            StyleRoundTile(rmBtn);
            AttachTileIcon(rmBtn, TabletIconKind.Bolt, rm, true);
            roundRow.Add(rmBtn);

            string accentName = _presentation == null ? "琥珀" :
                _presentation.Accent == TabletAccent.Amber ? "琥珀" :
                _presentation.Accent == TabletAccent.Teal ? "青绿" : "钴蓝";
            var accentBtn = CreateTileButton(accentName, "tablet-accent-cycle", false, () =>
            {
                if (_presentation != null)
                {
                    TabletAccent next = _presentation.Accent == TabletAccent.Amber ? TabletAccent.Teal :
                                        _presentation.Accent == TabletAccent.Teal ? TabletAccent.Blue : TabletAccent.Amber;
                    _presentation.SetAccent(next);
                }
            });
            StyleRoundTile(accentBtn);
            AttachTileIcon(accentBtn, TabletIconKind.Palette, false, true);
            roundRow.Add(accentBtn);

            var quickSettingsBtn = CreateTileButton("设置", "tablet-controls-settings", false, () =>
            {
                _state.ClosePanel();
                _openSettings?.Invoke();
            });
            StyleRoundTile(quickSettingsBtn);
            AttachTileIcon(quickSettingsBtn, TabletIconKind.Gear, false, true);
            roundRow.Add(quickSettingsBtn);

            // ---- 4. 本机网络状态（保持事实：内网沙箱，无公共网络） ----
            var offlineCard = new VisualElement { name = "settings-hardware-offline-box" };
            offlineCard.style.backgroundColor = _highContrast ? Color.black : TabletTheme.GlassTile;
            offlineCard.style.borderTopLeftRadius = offlineCard.style.borderTopRightRadius = 20;
            offlineCard.style.borderBottomLeftRadius = offlineCard.style.borderBottomRightRadius = 20;
            offlineCard.style.paddingTop = 12; offlineCard.style.paddingBottom = 12;
            offlineCard.style.paddingLeft = 16; offlineCard.style.paddingRight = 16;
            offlineCard.style.marginBottom = 10;

            var boxHead = CreateLabel("车载内网 · 离线沙箱", 13, true);
            boxHead.style.color = TabletTheme.SignalGreen;
            boxHead.style.marginBottom = 4;
            offlineCard.Add(boxHead);
            var boxBody = CreateLabel("本终端运行在车载内网，没有公共 Wi-Fi、蓝牙与蜂窝模块。", 11, false);
            boxBody.style.whiteSpace = WhiteSpace.Normal;
            boxBody.style.color = TabletTheme.TextSecondary;
            offlineCard.Add(boxBody);
            scroll.Add(offlineCard);

            // ---- 5. 系统入口 ----
            var settingsBtn = CreatePillButton("打开完整系统设置 ›", 200, 48, () =>
            {
                _state.ClosePanel();
                _openSettings?.Invoke();
            });
            settingsBtn.style.alignSelf = Align.Stretch;
            settingsBtn.name = "tablet-shell-settings";
            settingsBtn.style.fontSize = 13;
            settingsBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            settingsBtn.style.borderTopLeftRadius = settingsBtn.style.borderTopRightRadius = 24;
            settingsBtn.style.borderBottomLeftRadius = settingsBtn.style.borderBottomRightRadius = 24;
            scroll.Add(settingsBtn);
        }

        private VisualElement BuildMediaCard()
        {
            var mediaCard = new VisualElement { name = "tablet-media-card" };
            mediaCard.style.flexGrow = 1;
            mediaCard.style.flexShrink = 1;
            mediaCard.style.height = 164;
            mediaCard.style.borderTopLeftRadius = mediaCard.style.borderTopRightRadius = 24;
            mediaCard.style.borderBottomLeftRadius = mediaCard.style.borderBottomRightRadius = 24;
            mediaCard.style.backgroundColor = _highContrast ? Color.black : TabletTheme.GlassTile;
            mediaCard.style.paddingLeft = 14; mediaCard.style.paddingRight = 14;
            mediaCard.style.paddingTop = 12; mediaCard.style.paddingBottom = 12;
            mediaCard.style.justifyContent = Justify.SpaceBetween;

            var vinylThumb = new VisualElement();
            vinylThumb.style.width = 44; vinylThumb.style.height = 44;
            vinylThumb.style.borderTopLeftRadius = vinylThumb.style.borderTopRightRadius = 12;
            vinylThumb.style.borderBottomLeftRadius = vinylThumb.style.borderBottomRightRadius = 12;
            vinylThumb.style.backgroundColor = TabletTheme.VinylFill;
            vinylThumb.style.borderTopWidth = vinylThumb.style.borderBottomWidth = vinylThumb.style.borderLeftWidth = vinylThumb.style.borderRightWidth = 2;
            vinylThumb.style.borderTopColor = vinylThumb.style.borderBottomColor = vinylThumb.style.borderLeftColor = vinylThumb.style.borderRightColor = _accent;
            mediaCard.Add(vinylThumb);

            var trackInfo = new VisualElement();
            string curSong = !string.IsNullOrEmpty(RailwayRenaissance.UI.Tablet.Apps.TabletMusicAppUI.tabletCurrentTrackTitle)
                ? RailwayRenaissance.UI.Tablet.Apps.TabletMusicAppUI.tabletCurrentTrackTitle
                : "归乡 · 晨曦微光";
            var songLbl = CreateLabel(curSong, 13, true);
            songLbl.style.overflow = Overflow.Hidden;
            songLbl.style.textOverflow = TextOverflow.Ellipsis;
            songLbl.style.whiteSpace = WhiteSpace.NoWrap;
            var artLbl = CreateLabel("铁道随身听", 11, false);
            artLbl.style.color = TabletTheme.TextSecondary;
            trackInfo.Add(songLbl);
            trackInfo.Add(artLbl);
            mediaCard.Add(trackInfo);

            var mediaActions = new VisualElement();
            mediaActions.style.flexDirection = FlexDirection.Row;
            mediaActions.style.justifyContent = Justify.FlexStart;
            var playBtn = CreatePillButton(RailwayRenaissance.UI.Tablet.Apps.TabletMusicAppUI.tabletIsPlaying ? "❚❚" : "▶", 56, 36, () =>
            {
                RailwayRenaissance.UI.Tablet.Apps.TabletMusicAppUI.ToggleTabletMusicPlayPause();
                Render();
            });
            playBtn.name = "tablet-media-play-toggle";
            playBtn.AddToClassList("shell-keep");
            playBtn.style.fontSize = 12;
            playBtn.style.backgroundColor = _accent;
            playBtn.style.color = TabletTheme.InkOnGlass;
            playBtn.style.borderTopWidth = playBtn.style.borderBottomWidth = playBtn.style.borderLeftWidth = playBtn.style.borderRightWidth = 0;
            playBtn.style.borderTopLeftRadius = playBtn.style.borderTopRightRadius = 18;
            playBtn.style.borderBottomLeftRadius = playBtn.style.borderBottomRightRadius = 18;
            mediaActions.Add(playBtn);
            mediaCard.Add(mediaActions);
            return mediaCard;
        }

        /// <summary>
        /// 竖向胶囊滑条：点击切换，纵向拖动调节；拖动过程只改填充高度，松手时一次性提交，避免重建界面打断拖动。
        /// </summary>
        private Button CreateVerticalSlider(string name, TabletIconKind icon, float initialFill, Action onToggle, Action<float> onCommit)
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
            btn.style.width = 62;
            btn.style.height = 164;
            btn.style.flexShrink = 0;
            btn.style.marginLeft = 10;
            btn.style.paddingLeft = btn.style.paddingRight = btn.style.paddingTop = btn.style.paddingBottom = 0;
            btn.style.backgroundColor = _highContrast ? Color.black : TabletTheme.GlassTile;
            btn.style.borderTopWidth = btn.style.borderBottomWidth = btn.style.borderLeftWidth = btn.style.borderRightWidth = 0;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius = 31;
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 31;
            btn.style.overflow = Overflow.Hidden;

            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.style.position = Position.Absolute;
            fill.style.left = 0; fill.style.right = 0; fill.style.bottom = 0;
            fill.style.height = Length.Percent(fillValue * 100f);
            fill.style.backgroundColor = TabletTheme.GlassTileActive;
            btn.Add(fill);

            var iconLabel = new TabletIconElement(icon, 24f, fillValue > 0.14f ? TabletTheme.InkOnGlass : Color.white);
            iconLabel.style.position = Position.Absolute;
            iconLabel.style.left = Length.Percent(50);
            iconLabel.style.translate = new Translate(Length.Percent(-50), 0);
            iconLabel.style.bottom = 16;
            btn.Add(iconLabel);

            btn.RegisterCallback<PointerDownEvent>(e => { down = true; dragged = false; startY = e.position.y; });
            btn.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!down) return;
                if (!dragged && Mathf.Abs(e.position.y - startY) > 6f)
                {
                    dragged = true;
                    btn.CapturePointer(e.pointerId);
                }
                if (dragged)
                {
                    float h = Mathf.Max(1f, btn.resolvedStyle.height);
                    fillValue = Mathf.Clamp01(1f - e.localPosition.y / h);
                    fill.style.height = Length.Percent(fillValue * 100f);
                    iconLabel.IconColor = fillValue > 0.14f ? TabletTheme.InkOnGlass : Color.white;
                }
            });
            btn.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!down) return;
                down = false;
                if (!dragged) return;
                if (btn.HasPointerCapture(e.pointerId)) btn.ReleasePointer(e.pointerId);
                onCommit?.Invoke(fillValue);
            });
            return btn;
        }

        private static void AttachTileIcon(Button btn, TabletIconKind kind, bool active, bool round)
        {
            var icon = new TabletIconElement(kind, round ? 24f : 26f, active ? TabletTheme.InkOnGlass : Color.white);
            icon.style.position = Position.Absolute;
            if (round)
            {
                icon.style.top = 14;
                icon.style.left = Length.Percent(50);
                icon.style.translate = new Translate(Length.Percent(-50), 0);
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
            btn.style.height = 64;
            btn.style.minHeight = 64;
            btn.style.fontSize = 13;
            btn.style.whiteSpace = WhiteSpace.Normal;
            btn.style.unityTextAlign = TextAnchor.MiddleLeft;
            btn.style.paddingLeft = 56;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius = 24;
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 24;
        }

        private static void StyleRoundTile(Button btn)
        {
            btn.style.width = 76;
            btn.style.height = 76;
            btn.style.minWidth = 76;
            btn.style.minHeight = 76;
            btn.style.fontSize = 11;
            btn.style.whiteSpace = WhiteSpace.Normal;
            btn.style.unityTextAlign = TextAnchor.LowerCenter;
            btn.style.paddingBottom = 12;
            btn.style.paddingLeft = btn.style.paddingRight = 2;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius = 38;
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 38;
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
