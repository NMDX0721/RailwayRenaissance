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
        private readonly VisualElement _contentContainer;
        private readonly Label _badge;
        private Focusable _previousFocus;
        private TabletSystemPanel _renderedPanel;
        private Color _accent = new Color(.86f,.65f,.27f);
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
            _badge.style.backgroundColor = new Color(0.92f, 0.22f, 0.22f, 1f);
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
            _scrim.style.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 0.45f);
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
            _panel.style.backgroundColor = new Color(0.13f, 0.14f, 0.16f, 0.98f);
            _panel.style.borderTopWidth = 1;
            _panel.style.borderRightWidth = 1;
            _panel.style.borderBottomWidth = 1;
            _panel.style.borderLeftWidth = 1;
            var borderColor = new Color(0.35f, 0.36f, 0.40f, 1f);
            _panel.style.borderTopColor = borderColor;
            _panel.style.borderRightColor = borderColor;
            _panel.style.borderBottomColor = borderColor;
            _panel.style.borderLeftColor = borderColor;
            _panel.style.borderTopLeftRadius = 8;
            _panel.style.borderTopRightRadius = 8;
            _panel.style.borderBottomLeftRadius = 8;
            _panel.style.borderBottomRightRadius = 8;
            _panel.style.paddingTop = 12;
            _panel.style.paddingRight = 12;
            _panel.style.paddingBottom = 12;
            _panel.style.paddingLeft = 12;
            _panel.pickingMode = PickingMode.Position;
            _panel.focusable = true;
            _panel.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            _panel.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());

            var panelHeader = new VisualElement();
            panelHeader.style.flexDirection = FlexDirection.Row;
            panelHeader.style.justifyContent = Justify.SpaceBetween;
            panelHeader.style.alignItems = Align.Center;
            panelHeader.style.marginBottom = 10;

            _panelTitle = CreateLabel("系统面板", 15, true);
            var btnClose = CreateButton("✕", 44, 44, () => _state.ClosePanel());
            panelHeader.Add(_panelTitle);
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
            _panel.style.backgroundColor = highContrast ? Color.black : GlassControlPanel;
            if (highContrast)
            {
                ClearBlurredBackground();
                _scrim.style.backgroundColor = new Color(0f, 0f, 0f, 0.85f);
            }
            else
            {
                _scrim.style.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 0.45f);
                if (contrastChanged && _state.Panel != TabletSystemPanel.None)
                {
                    OnRequestCapture?.Invoke();
                }
            }
            _rootContainer.Query<Button>().ForEach(button =>
            {
                button.style.color = Color.white;
                bool quietActive = button.name == "tablet-quiet-toggle" && _state.QuietMode;
                bool hcActive = button.name == "tablet-high-contrast-toggle" && (_presentation?.HighContrast ?? false);
                bool rmActive = button.name == "tablet-reduce-motion-toggle" && (_presentation?.ReduceMotion ?? false);
                bool dimActive = button.name == "tablet-wallpaper-dim-toggle" && (_presentation != null && _presentation.WallpaperDim > 0.05f);
                bool muteActive = button.name == "tablet-audio-mute-toggle" && GameData.MasterVolume <= 0.001f;
                bool isActive = quietActive || hcActive || rmActive || dimActive || muteActive;

                if (button.name == "tablet-notifications-open" || button.name == "tablet-controls-open")
                {
                    button.style.backgroundColor = new Color(1f, 1f, 1f, 0.12f);
                    button.style.borderTopColor = button.style.borderBottomColor = button.style.borderLeftColor = button.style.borderRightColor = new Color(1f, 1f, 1f, 0.18f);
                }
                else
                {
                    button.style.backgroundColor = isActive ? new Color(0.24f, 0.38f, 0.28f, 1f) : highContrast ? Color.black : new Color(1f, 1f, 1f, 0.08f);
                    button.style.borderTopColor = button.style.borderBottomColor = button.style.borderLeftColor = button.style.borderRightColor = isActive ? accent : new Color(1f, 1f, 1f, 0.12f);
                }
            });
            _panel.Query<Label>().ForEach(label => label.style.color = Color.white);
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
                _panelTitle.text = "通知中心";
                RenderNotifications();
            }
            else if (_state.Panel == TabletSystemPanel.Controls)
            {
                _panelTitle.text = "控制面板";
                RenderControls();
            }
            ApplyResponsiveLayout(_parent.layout.width, _parent.layout.height);
            if (opening)
            {
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

        private void OnParentGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyResponsiveLayout(evt.newRect.width, evt.newRect.height);
        }

        private void ApplyResponsiveLayout(float width, float height)
        {
            if (_panel == null || width <= 0 || height <= 0) return;

            bool compact = width < 820 || height < 520;
            bool fullSheet = height > width || width < 620;
            bool fromLeft = _state.Panel == TabletSystemPanel.Notifications;

            if (fromLeft)
            {
                _panel.style.left = 0;
                _panel.style.right = StyleKeyword.Auto;
            }
            else
            {
                _panel.style.left = StyleKeyword.Auto;
                _panel.style.right = 0;
            }

            _panel.style.width = fullSheet
                ? Length.Percent(100)
                : new Length(Mathf.Clamp(width * 0.40f, compact ? 340f : 440f, 500f), LengthUnit.Pixel);
            _panel.style.maxWidth = Length.Percent(100);
            _panel.style.top = fullSheet ? 0 : 44;
            _panel.style.bottom = fullSheet ? 0 : 12;
            _panel.style.borderTopLeftRadius = fullSheet ? 0 : 22;
            _panel.style.borderTopRightRadius = fullSheet ? 0 : 22;
            _panel.style.borderBottomLeftRadius = fullSheet ? 0 : 22;
            _panel.style.borderBottomRightRadius = fullSheet ? 0 : 22;
            _panel.style.borderTopWidth = 1; _panel.style.borderTopColor = GlassHighlightBorder;
            _panel.style.borderLeftWidth = 1; _panel.style.borderLeftColor = new Color(1f, 1f, 1f, 0.10f);
            _panel.style.paddingLeft = compact ? 14 : 18;
            _panel.style.paddingRight = compact ? 14 : 18;
            _panel.style.paddingTop = compact ? 14 : 18;
            _panel.style.paddingBottom = compact ? 14 : 18;
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
            toolbar.style.marginBottom = 8;

            var clearBtn = CreateButton("全部清除", 88, 44, () => _state.ClearNotifications());
            clearBtn.name = "tablet-notifications-clear";
            clearBtn.SetEnabled(_state.Notifications.Count > 0);
            toolbar.Add(clearBtn);
            _contentContainer.Add(toolbar);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            _contentContainer.Add(scroll);

            var list = _state.Notifications;
            if (list.Count == 0)
            {
                var empty = CreateLabel("暂无通知", 14, false);
                empty.style.marginTop = 24;
                empty.style.unityTextAlign = TextAnchor.MiddleCenter;
                empty.style.color = new Color(0.65f, 0.65f, 0.70f, 1f);
                scroll.Add(empty);
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var card = new VisualElement();
                card.style.backgroundColor = new Color(0.18f, 0.19f, 0.22f, 1f);
                card.style.borderTopLeftRadius = 6;
                card.style.borderTopRightRadius = 6;
                card.style.borderBottomLeftRadius = 6;
                card.style.borderBottomRightRadius = 6;
                card.style.paddingTop = 8;
                card.style.paddingRight = 8;
                card.style.paddingBottom = 8;
                card.style.paddingLeft = 8;
                card.style.marginBottom = 8;

                var cardHeader = new VisualElement();
                cardHeader.style.flexDirection = FlexDirection.Row;
                cardHeader.style.justifyContent = Justify.SpaceBetween;
                cardHeader.style.alignItems = Align.Center;

                var titleLabel = CreateLabel(item.Title, 13, true);
                titleLabel.style.flexGrow = 1;
                titleLabel.style.flexShrink = 1;
                titleLabel.style.whiteSpace = WhiteSpace.Normal;

                var timeLabel = CreateLabel(item.CreatedAt.ToString("HH:mm:ss"), 11, false);
                timeLabel.style.color = new Color(0.70f, 0.70f, 0.75f, 1f);
                timeLabel.style.marginRight = 6;

                long dismissId = item.Id;
                var dismissBtn = CreateButton("✕", 44, 44, () => _state.Dismiss(dismissId));
                dismissBtn.name = "tablet-notification-dismiss-" + dismissId;

                cardHeader.Add(titleLabel);
                cardHeader.Add(timeLabel);
                cardHeader.Add(dismissBtn);
                card.Add(cardHeader);

                if (!string.IsNullOrEmpty(item.Body))
                {
                    var bodyLabel = CreateLabel(item.Body, 12, false);
                    bodyLabel.style.marginTop = 4;
                    bodyLabel.style.whiteSpace = WhiteSpace.Normal;
                    bodyLabel.style.color = new Color(0.85f, 0.85f, 0.88f, 1f);
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

            // ==================== 1. 顶部状态与设备概要 ====================
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 12;
            scroll.Add(headerRow);

            var devTitle = CreateLabel("NF-5K 列控智能终端", 14, true);
            devTitle.style.color = new Color(0.9f, 0.9f, 0.95f, 1f);
            headerRow.Add(devTitle);

            var quickSettingsBtn = CreatePillButton("⚙ 设置", 54, 28, () =>
            {
                _state.ClosePanel();
                _openSettings?.Invoke();
            });
            quickSettingsBtn.style.fontSize = 11;
            headerRow.Add(quickSettingsBtn);

            // ==================== 2. 双胶囊快速掌控大卡 (Master Pill Cards) ====================
            var topPillsRow = new VisualElement();
            topPillsRow.style.flexDirection = FlexDirection.Row;
            topPillsRow.style.justifyContent = Justify.SpaceBetween;
            topPillsRow.style.marginBottom = 12;
            scroll.Add(topPillsRow);

            // 勿扰模式大胶囊卡片
            string quietText = _state.QuietMode ? "勿扰 · 已开启" : "勿扰 · 已关闭";
            var quietBtn = CreateTileButton(quietText, "tablet-quiet-toggle", _state.QuietMode, () =>
            {
                _state.SetQuietMode(!_state.QuietMode);
            });
            quietBtn.style.width = Length.Percent(48);
            quietBtn.style.minHeight = 58;
            quietBtn.style.borderTopLeftRadius = 18; quietBtn.style.borderTopRightRadius = 18;
            quietBtn.style.borderBottomLeftRadius = 18; quietBtn.style.borderBottomRightRadius = 18;
            topPillsRow.Add(quietBtn);

            // 高对比度大胶囊卡片
            bool hc = _presentation != null && _presentation.HighContrast;
            string hcText = hc ? "高对比 · 已开启" : "高对比 · 已关闭";
            var hcBtn = CreateTileButton(hcText, "tablet-high-contrast-toggle", hc, () =>
            {
                if (_presentation != null) _presentation.SetHighContrast(!_presentation.HighContrast);
            });
            hcBtn.style.width = Length.Percent(48);
            hcBtn.style.minHeight = 58;
            hcBtn.style.borderTopLeftRadius = 18; hcBtn.style.borderTopRightRadius = 18;
            hcBtn.style.borderBottomLeftRadius = 18; hcBtn.style.borderBottomRightRadius = 18;
            topPillsRow.Add(hcBtn);

            // ==================== 3. 双纵向胶囊滑块 + 车载播控中心微件 ====================
            var centerRow = new VisualElement();
            centerRow.name = "tablet-controls-center-row";
            centerRow.style.flexDirection = FlexDirection.Row;
            centerRow.style.justifyContent = Justify.SpaceBetween;
            centerRow.style.marginBottom = 12;
            centerRow.style.height = 130;
            scroll.Add(centerRow);

            // 左侧纵向滑块 1: 防眩光/亮度 (tablet-wallpaper-dim-toggle)
            bool dim = _presentation != null && _presentation.WallpaperDim > 0.05f;
            string dimText = dim ? "防眩光 · 30%" : "防眩光 · 关闭";
            var dimBtn = CreateTileButton(dimText, "tablet-wallpaper-dim-toggle", dim, () =>
            {
                if (_presentation != null)
                {
                    _presentation.SetWallpaperDim(_presentation.WallpaperDim > 0.05f ? 0f : 0.3f);
                }
            });
            dimBtn.style.width = 64;
            dimBtn.style.height = 130;
            dimBtn.style.borderTopLeftRadius = 32; dimBtn.style.borderTopRightRadius = 32;
            dimBtn.style.borderBottomLeftRadius = 32; dimBtn.style.borderBottomRightRadius = 32;
            dimBtn.style.fontSize = 11;
            dimBtn.style.whiteSpace = WhiteSpace.Normal;
            centerRow.Add(dimBtn);

            // 左侧纵向滑块 2: 系统主音量 (tablet-audio-mute-toggle)
            bool isMute = GameData.MasterVolume <= 0.001f;
            string muteText = isMute ? "音频 · 静音" : "音频 · 正常";
            var muteBtn = CreateTileButton(muteText, "tablet-audio-mute-toggle", isMute, () =>
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
            });
            muteBtn.style.width = 64;
            muteBtn.style.height = 130;
            muteBtn.style.borderTopLeftRadius = 32; muteBtn.style.borderTopRightRadius = 32;
            muteBtn.style.borderBottomLeftRadius = 32; muteBtn.style.borderBottomRightRadius = 32;
            muteBtn.style.fontSize = 11;
            muteBtn.style.whiteSpace = WhiteSpace.Normal;
            centerRow.Add(muteBtn);

            // 右侧微件: 车载流体播控中心 (tablet-media-card)
            var mediaCard = new VisualElement { name = "tablet-media-card" };
            mediaCard.style.flexGrow = 1;
            mediaCard.style.marginLeft = 10;
            mediaCard.style.height = 130;
            mediaCard.style.borderTopLeftRadius = 18; mediaCard.style.borderTopRightRadius = 18;
            mediaCard.style.borderBottomLeftRadius = 18; mediaCard.style.borderBottomRightRadius = 18;
            mediaCard.style.backgroundColor = new Color(1f, 1f, 1f, 0.06f);
            mediaCard.style.borderTopWidth = 1; mediaCard.style.borderBottomWidth = 1;
            mediaCard.style.borderLeftWidth = 1; mediaCard.style.borderRightWidth = 1;
            var mcBorder = new Color(1f, 1f, 1f, 0.12f);
            mediaCard.style.borderTopColor = mcBorder; mediaCard.style.borderBottomColor = mcBorder;
            mediaCard.style.borderLeftColor = mcBorder; mediaCard.style.borderRightColor = mcBorder;
            mediaCard.style.paddingLeft = 12; mediaCard.style.paddingRight = 12;
            mediaCard.style.paddingTop = 10; mediaCard.style.paddingBottom = 10;
            mediaCard.style.justifyContent = Justify.SpaceBetween;

            var mediaHead = new VisualElement();
            mediaHead.style.flexDirection = FlexDirection.Row;
            mediaHead.style.justifyContent = Justify.SpaceBetween;
            mediaHead.style.alignItems = Align.Center;
            var mediaTag = CreateLabel("📻 车载广播播控", 11, false);
            mediaTag.style.color = new Color(0.7f, 0.75f, 0.85f, 1f);
            mediaHead.Add(mediaTag);
            mediaCard.Add(mediaHead);

            var trackRow = new VisualElement();
            trackRow.style.flexDirection = FlexDirection.Row;
            trackRow.style.alignItems = Align.Center;
            var vinylThumb = new VisualElement();
            vinylThumb.style.width = 32; vinylThumb.style.height = 32;
            vinylThumb.style.borderTopLeftRadius = 16; vinylThumb.style.borderTopRightRadius = 16;
            vinylThumb.style.borderBottomLeftRadius = 16; vinylThumb.style.borderBottomRightRadius = 16;
            vinylThumb.style.backgroundColor = new Color(0.15f, 0.15f, 0.18f, 1f);
            vinylThumb.style.borderTopWidth = 1; vinylThumb.style.borderBottomWidth = 1;
            vinylThumb.style.borderLeftWidth = 1; vinylThumb.style.borderRightWidth = 1;
            vinylThumb.style.borderTopColor = AccentSystem; vinylThumb.style.borderBottomColor = AccentSystem;
            vinylThumb.style.borderLeftColor = AccentSystem; vinylThumb.style.borderRightColor = AccentSystem;
            vinylThumb.style.marginRight = 8;
            trackRow.Add(vinylThumb);

            var trackInfo = new VisualElement();
            string curSong = !string.IsNullOrEmpty(RailwayRenaissance.UI.Tablet.Apps.TabletMusicAppUI.tabletCurrentTrackTitle)
                ? RailwayRenaissance.UI.Tablet.Apps.TabletMusicAppUI.tabletCurrentTrackTitle
                : "归乡 · 晨曦微光";
            var songLbl = CreateLabel(curSong, 12, true);
            songLbl.style.maxWidth = 140;
            var artLbl = CreateLabel("铁道随身听", 10, false);
            artLbl.style.color = new Color(0.6f, 0.6f, 0.65f, 1f);
            trackInfo.Add(songLbl);
            trackInfo.Add(artLbl);
            trackRow.Add(trackInfo);
            mediaCard.Add(trackRow);

            var mediaActions = new VisualElement();
            mediaActions.style.flexDirection = FlexDirection.Row;
            mediaActions.style.justifyContent = Justify.FlexEnd;
            mediaActions.style.alignItems = Align.Center;

            var playBtn = CreatePillButton(RailwayRenaissance.UI.Tablet.Apps.TabletMusicAppUI.tabletIsPlaying ? "❚❚" : "▶", 40, 26, () =>
            {
                RailwayRenaissance.UI.Tablet.Apps.TabletMusicAppUI.ToggleTabletMusicPlayPause();
                Render();
            });
            playBtn.name = "tablet-media-play-toggle";
            playBtn.style.fontSize = 11;
            playBtn.style.backgroundColor = AccentSystem;
            mediaActions.Add(playBtn);
            mediaCard.Add(mediaActions);

            centerRow.Add(mediaCard);

            // ==================== 4. 次要快捷磁贴矩阵 ====================
            var quickGrid = new VisualElement();
            quickGrid.style.flexDirection = FlexDirection.Row;
            quickGrid.style.justifyContent = Justify.SpaceBetween;
            quickGrid.style.marginBottom = 12;
            scroll.Add(quickGrid);

            // 减少动效
            bool rm = _presentation != null && _presentation.ReduceMotion;
            string rmText = rm ? "动效 · 减弱" : "动效 · 完整";
            var rmBtn = CreateTileButton(rmText, "tablet-reduce-motion-toggle", rm, () =>
            {
                if (_presentation != null) _presentation.SetReduceMotion(!_presentation.ReduceMotion);
            });
            rmBtn.style.width = Length.Percent(48);
            rmBtn.style.height = 42;
            rmBtn.style.borderTopLeftRadius = 14; rmBtn.style.borderTopRightRadius = 14;
            rmBtn.style.borderBottomLeftRadius = 14; rmBtn.style.borderBottomRightRadius = 14;
            quickGrid.Add(rmBtn);

            // 强调色切换
            string accentName = _presentation == null ? "强调色 · 琥珀" :
                _presentation.Accent == TabletAccent.Amber ? "强调色 · 琥珀" :
                _presentation.Accent == TabletAccent.Teal ? "强调色 · 青绿" : "强调色 · 钴蓝";
            var accentBtn = CreateTileButton(accentName, "tablet-accent-cycle", false, () =>
            {
                if (_presentation != null)
                {
                    TabletAccent next = _presentation.Accent == TabletAccent.Amber ? TabletAccent.Teal :
                                        _presentation.Accent == TabletAccent.Teal ? TabletAccent.Blue : TabletAccent.Amber;
                    _presentation.SetAccent(next);
                }
            });
            accentBtn.style.width = Length.Percent(48);
            accentBtn.style.height = 42;
            accentBtn.style.borderTopLeftRadius = 14; accentBtn.style.borderTopRightRadius = 14;
            accentBtn.style.borderBottomLeftRadius = 14; accentBtn.style.borderBottomRightRadius = 14;
            quickGrid.Add(accentBtn);

            // ==================== 5. 硬件沙箱与通信隔离 ====================
            var offlineCard = new VisualElement { name = "settings-hardware-offline-box" };
            offlineCard.style.backgroundColor = new Color(0.12f, 0.13f, 0.16f, 0.95f);
            offlineCard.style.borderTopLeftRadius = 14; offlineCard.style.borderTopRightRadius = 14;
            offlineCard.style.borderBottomLeftRadius = 14; offlineCard.style.borderBottomRightRadius = 14;
            offlineCard.style.borderTopWidth = 1; offlineCard.style.borderBottomWidth = 1;
            offlineCard.style.borderLeftWidth = 1; offlineCard.style.borderRightWidth = 1;
            var boxBorder = new Color(1f, 1f, 1f, 0.08f);
            offlineCard.style.borderTopColor = boxBorder; offlineCard.style.borderBottomColor = boxBorder;
            offlineCard.style.borderLeftColor = boxBorder; offlineCard.style.borderRightColor = boxBorder;
            offlineCard.style.paddingTop = 10; offlineCard.style.paddingBottom = 10;
            offlineCard.style.paddingLeft = 12; offlineCard.style.paddingRight = 12;
            offlineCard.style.marginBottom = 12;

            var boxHead = new VisualElement();
            boxHead.style.flexDirection = FlexDirection.Row;
            boxHead.style.alignItems = Align.Center;
            boxHead.style.marginBottom = 6;
            var shieldIc = CreateLabel("🛡 车载硬件物理隔离沙箱", 12, true);
            shieldIc.style.color = new Color(0.35f, 0.85f, 0.55f, 1f);
            boxHead.Add(shieldIc);
            offlineCard.Add(boxHead);

            var wlanRow = CreateLabel("● 无线网络 (WLAN) · 离线 [沙箱隔离 / NOT_RUN]", 11, false);
            wlanRow.style.color = new Color(0.65f, 0.65f, 0.70f, 1f);
            wlanRow.style.marginBottom = 3;
            offlineCard.Add(wlanRow);

            var btRow = CreateLabel("● 蓝牙无线射频 · 离线 [硬件未搭载 / NOT_RUN]", 11, false);
            btRow.style.color = new Color(0.65f, 0.65f, 0.70f, 1f);
            btRow.style.marginBottom = 3;
            offlineCard.Add(btRow);

            var simRow = CreateLabel("● 蜂窝移动基带 · 离线 [物理硬隔离 / NOT_RUN]", 11, false);
            simRow.style.color = new Color(0.65f, 0.65f, 0.70f, 1f);
            offlineCard.Add(simRow);

            scroll.Add(offlineCard);

            // ==================== 6. 系统入口 ====================
            var settingsBtn = CreatePillButton("打开完整系统设置 ›", 200, 42, () =>
            {
                _state.ClosePanel();
                _openSettings?.Invoke();
            });
            settingsBtn.style.alignSelf = Align.Stretch;
            settingsBtn.name = "tablet-shell-settings";
            settingsBtn.style.fontSize = 13;
            settingsBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            scroll.Add(settingsBtn);
        }

        private Button CreateTileButton(string text, string name, bool active, Action onClick)
        {
            var btn = CreatePillButton(text, 120, 44, onClick);
            btn.name = name;
            btn.style.fontSize = 12;
            if (active)
            {
                btn.style.backgroundColor = new Color(0.24f, 0.38f, 0.28f, 1f);
            }
            return btn;
        }

        private Button CreatePillButton(string text, float minWidth, float minHeight, Action onClick)
        {
            var btn = new Button(onClick) { text = text };
            btn.style.minWidth = minWidth;
            btn.style.minHeight = minHeight;
            btn.style.backgroundColor = new Color(1f, 1f, 1f, 0.08f);
            btn.style.color = Color.white;
            btn.style.borderTopWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1;
            var bc = new Color(1f, 1f, 1f, 0.12f);
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
            btn.style.backgroundColor = new Color(1f, 1f, 1f, 0.08f);
            btn.style.color = Color.white;
            btn.style.borderTopWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1;
            var bc = new Color(1f, 1f, 1f, 0.12f);
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
