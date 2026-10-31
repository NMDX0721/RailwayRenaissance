using System;
using UnityEngine;
using UnityEngine.UIElements;

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

        public VisualElement BlurBackgroundElement => _blurBackground;
        public bool IsBlurActive => _blurBackground != null && _blurBackground.style.display == DisplayStyle.Flex && _blurBackground.style.backgroundImage.value.renderTexture != null;
        public Action OnRequestCapture { get; set; }

        public TabletShellView(VisualElement parent, TabletShellState state, Font font, Action openSettings)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _font = font;
            _openSettings = openSettings;

            _rootContainer = new VisualElement { name = "tablet-shell-root" };
            _rootContainer.style.position = Position.Absolute;
            _rootContainer.style.left = 0;
            _rootContainer.style.top = 0;
            _rootContainer.style.right = 0;
            _rootContainer.style.bottom = 0;
            _rootContainer.pickingMode = PickingMode.Ignore;

            _launcher = new VisualElement { name = "tablet-shell-launcher" };
            _launcher.style.position = Position.Absolute;
            _launcher.style.top = 2;
            _launcher.style.right = 210;
            _launcher.style.height = 44;
            _launcher.style.flexDirection = FlexDirection.Row;
            _launcher.style.alignItems = Align.Center;
            _launcher.pickingMode = PickingMode.Position;
            _launcher.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            var btnNotif = CreateButton("通知", 52, 44, () => _state.TogglePanel(TabletSystemPanel.Notifications));
            var btnCtrl = CreateButton("控制", 52, 44, () => _state.TogglePanel(TabletSystemPanel.Controls));
            btnNotif.name = "tablet-notifications-open";
            btnCtrl.name = "tablet-controls-open";
            btnNotif.style.marginRight = 4;

            _badge = new Label { name = "tablet-notifications-badge" };
            _badge.style.position = Position.Absolute;
            _badge.style.top = 2;
            _badge.style.right = 2;
            _badge.style.minWidth = 16;
            _badge.style.height = 16;
            _badge.style.borderTopLeftRadius = 8;
            _badge.style.borderTopRightRadius = 8;
            _badge.style.borderBottomLeftRadius = 8;
            _badge.style.borderBottomRightRadius = 8;
            _badge.style.backgroundColor = new Color(0.85f, 0.22f, 0.22f, 1f);
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
            _panel.style.right = 0;
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

            _state.Changed += Render;
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
            _panel.style.backgroundColor = highContrast ? Color.black : new Color(.13f,.14f,.16f,.98f);
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
                button.style.backgroundColor = highContrast ? Color.black : new Color(.22f,.24f,.28f);
                button.style.borderTopColor = button.style.borderBottomColor = button.style.borderLeftColor = button.style.borderRightColor = accent;
            });
            _panel.Query<Label>().ForEach(label => label.style.color = Color.white);
        }

        public void Dispose()
        {
            _state.Changed -= Render;
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
            var quietContainer = new VisualElement();
            quietContainer.style.marginBottom = 16;

            string quietStatusText = _state.QuietMode ? "勿扰通知 (已开启)" : "勿扰通知 (已关闭)";
            var quietBtn = CreateButton(quietStatusText, 200, 48, () =>
            {
                _state.SetQuietMode(!_state.QuietMode);
            });
            quietBtn.style.alignSelf = Align.Stretch;
            quietBtn.name = "tablet-quiet-toggle";
            if (_state.QuietMode)
            {
                quietBtn.style.backgroundColor = new Color(0.24f, 0.38f, 0.28f, 1f);
            }
            quietContainer.Add(quietBtn);
            _contentContainer.Add(quietContainer);
            var quietHint = CreateLabel("通知仍会收存，开启后不弹出提示。", 12, false);
            quietHint.style.whiteSpace = WhiteSpace.Normal;
            quietHint.style.marginBottom = 16;
            _contentContainer.Add(quietHint);

            var settingsContainer = new VisualElement();
            var settingsBtn = CreateButton("设置", 200, 48, () =>
            {
                _state.ClosePanel();
                _openSettings?.Invoke();
            });
            settingsBtn.style.alignSelf = Align.Stretch;
            settingsBtn.name = "tablet-shell-settings";
            settingsContainer.Add(settingsBtn);
            _contentContainer.Add(settingsContainer);
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
            btn.style.backgroundColor = new Color(0.22f, 0.24f, 0.28f, 1f);
            btn.style.color = Color.white;
            btn.style.borderTopWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1;
            var bc = new Color(0.38f, 0.40f, 0.45f, 1f);
            btn.style.borderTopColor = bc;
            btn.style.borderRightColor = bc;
            btn.style.borderBottomColor = bc;
            btn.style.borderLeftColor = bc;
            btn.style.borderTopLeftRadius = 4;
            btn.style.borderTopRightRadius = 4;
            btn.style.borderBottomLeftRadius = 4;
            btn.style.borderBottomRightRadius = 4;
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
