using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    public sealed class TabletPresentationSettingsView : IDisposable
    {
        private readonly VisualElement _root;
        private readonly TabletPresentationPreferences _prefs;
        private readonly Toggle _reduceMotionToggle;
        private readonly Toggle _highContrastToggle;
        private readonly Slider _wallpaperDimSlider;
        private readonly Button _amberBtn, _tealBtn, _blueBtn;
        private readonly Button _resetBtn, _confirmResetBtn, _cancelResetBtn;
        private readonly VisualElement _resetConfirmRow;
        private readonly Label _errorLabel;
        private bool _isUpdating;
        private bool _disposed;

        public TabletPresentationSettingsView(VisualElement parent, TabletPresentationPreferences prefs, Font font)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            _prefs = prefs ?? throw new ArgumentNullException(nameof(prefs));
            _root = new VisualElement { name = "presentation-settings-root" };
            ApplyFontStyle(_root, font);

            _reduceMotionToggle = CreateToggle("减少动态效果", "toggle-reduce-motion", v => _prefs.SetReduceMotion(v));
            _highContrastToggle = CreateToggle("系统面板高对比度", "toggle-high-contrast", v => _prefs.SetHighContrast(v));

            var accentBox = new VisualElement { name = "accent-box", style = { flexDirection = FlexDirection.Row, minHeight = 44, alignItems = Align.Center } };
            accentBox.Add(new Label("主题强调色") { style = { minWidth = 100 } });
            _amberBtn = CreateButton("琥珀", () => _prefs.SetAccent(TabletAccent.Amber));
            _tealBtn = CreateButton("青绿", () => _prefs.SetAccent(TabletAccent.Teal));
            _blueBtn = CreateButton("蓝色", () => _prefs.SetAccent(TabletAccent.Blue));
            accentBox.Add(_amberBtn); accentBox.Add(_tealBtn); accentBox.Add(_blueBtn);

            var sliderBox = new VisualElement { name = "wallpaper-dim-box" };
            _wallpaperDimSlider = new Slider("壁纸压暗", 0f, 0.4f) { name = "slider-wallpaper-dim", style = { minHeight = 44 } };
            _wallpaperDimSlider.RegisterValueChangedCallback(evt => { if (!_isUpdating) _prefs.SetWallpaperDim(evt.newValue); });
            var noteLabel = new Label("仅调整平板壁纸，不影响场景光照") { name = "label-dim-note", style = { fontSize = 11, opacity = 0.7f } };
            sliderBox.Add(_wallpaperDimSlider); sliderBox.Add(noteLabel);

            var resetContainer = new VisualElement { name = "reset-container", style = { marginTop = 8 } };
            _resetBtn = CreateButton("恢复默认设置", () => SetResetPrompt(true), "btn-reset-defaults");
            _resetConfirmRow = new VisualElement { style = { flexDirection = FlexDirection.Row, display = DisplayStyle.None } };
            _confirmResetBtn = CreateButton("确认恢复", () => { _prefs.ResetToDefaults(); SetResetPrompt(false); }, "btn-confirm-reset");
            _cancelResetBtn = CreateButton("取消", () => SetResetPrompt(false), "btn-cancel-reset");
            _resetConfirmRow.Add(_confirmResetBtn); _resetConfirmRow.Add(_cancelResetBtn);
            resetContainer.Add(_resetBtn); resetContainer.Add(_resetConfirmRow);

            _errorLabel = new Label { name = "label-error", style = { color = Color.red, minHeight = 20, display = DisplayStyle.None } };

            _root.Add(_reduceMotionToggle); _root.Add(_highContrastToggle); _root.Add(accentBox);
            _root.Add(sliderBox); _root.Add(resetContainer); _root.Add(_errorLabel);
            parent.Add(_root);

            _prefs.Changed += Refresh;
            _root.RegisterCallback<DetachFromPanelEvent>(_ => Dispose());
            Refresh();
        }

        private Toggle CreateToggle(string text, string name, Action<bool> onChange)
        {
            var toggle = new Toggle(text) { name = name, style = { minHeight = 44 } };
            toggle.RegisterValueChangedCallback(evt => { if (!_isUpdating) onChange(evt.newValue); });
            return toggle;
        }

        private Button CreateButton(string text, Action onClick, string name = null)
        {
            var btn = new Button(onClick) { text = text, name = name ?? $"btn-{text}", style = { minHeight = 44, minWidth = 44 } };
            return btn;
        }

        private void SetResetPrompt(bool asking)
        {
            _resetBtn.style.display = asking ? DisplayStyle.None : DisplayStyle.Flex;
            _resetConfirmRow.style.display = asking ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Refresh()
        {
            _isUpdating = true;
            try
            {
                _reduceMotionToggle.SetValueWithoutNotify(_prefs.ReduceMotion);
                _highContrastToggle.SetValueWithoutNotify(_prefs.HighContrast);
                _wallpaperDimSlider.SetValueWithoutNotify(_prefs.WallpaperDim);

                UpdateAccentButtons(_prefs.Accent);
                bool contrast = _prefs.HighContrast;
                _root.style.backgroundColor = contrast ? Color.black : new Color(.96f,.95f,.92f);
                _root.style.color = contrast ? Color.white : new Color(.12f,.14f,.13f);
                _root.style.paddingLeft = _root.style.paddingRight = 12;
                _root.style.paddingTop = _root.style.paddingBottom = 12;
                _root.Query<Label>().ForEach(label => label.style.color = contrast ? Color.white : new Color(.12f,.14f,.13f));

                bool ro = _prefs.IsReadOnly;
                _reduceMotionToggle.SetEnabled(!ro);
                _highContrastToggle.SetEnabled(!ro);
                _wallpaperDimSlider.SetEnabled(!ro);
                _amberBtn.SetEnabled(!ro); _tealBtn.SetEnabled(!ro); _blueBtn.SetEnabled(!ro);
                _resetBtn.SetEnabled(!ro); _confirmResetBtn.SetEnabled(!ro);

                bool hasError = !string.IsNullOrEmpty(_prefs.LastError);
                _errorLabel.text = _prefs.LastError ?? string.Empty;
                _errorLabel.style.display = hasError ? DisplayStyle.Flex : DisplayStyle.None;
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void UpdateAccentButtons(TabletAccent active)
        {
            HighlightButton(_amberBtn, active == TabletAccent.Amber);
            HighlightButton(_tealBtn, active == TabletAccent.Teal);
            HighlightButton(_blueBtn, active == TabletAccent.Blue);
        }

        private static void HighlightButton(Button btn, bool active)
        {
            btn.style.unityFontStyleAndWeight = active ? FontStyle.Bold : FontStyle.Normal;
            btn.style.borderBottomWidth = btn.style.borderTopWidth = btn.style.borderLeftWidth = btn.style.borderRightWidth = active ? 2 : 1;
        }

        private static void ApplyFontStyle(VisualElement element, Font font)
        {
            if (font != null) element.style.unityFont = font;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            SetResetPrompt(false);
            _prefs.Changed -= Refresh;
            _root.RemoveFromHierarchy();
        }
    }
}
