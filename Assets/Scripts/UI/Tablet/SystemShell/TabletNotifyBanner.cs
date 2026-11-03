using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    /// <summary>
    /// 顶部横幅（heads-up）与轻提示（toast）。
    /// 横幅：从屏幕上方落下，停留数秒自动收起，上划收起，点击打开对应应用；多条排队，同 Key 原地更新。
    /// 勿扰、静默渠道、系统面板打开时不弹横幅，通知仍进入通知中心。
    /// </summary>
    public sealed class TabletNotifyBanner : IDisposable
    {
        private readonly VisualElement _parent;
        private readonly TabletShellState _state;
        private readonly VisualElement _host;
        private readonly VisualElement _toast;
        private readonly Label _toastLabel;
        private readonly Queue<TabletNotification> _queue = new Queue<TabletNotification>();
        private TabletNotification _current;
        private VisualElement _card;
        private IVisualElementScheduledItem _hideTimer;
        private IVisualElementScheduledItem _toastTimer;
        private bool _held;

        public VisualElement Root => _host;
        public bool IsShowing => _current != null;
        public TabletNotification Current => _current;

        public TabletNotifyBanner(VisualElement parent, TabletShellState state)
        {
            _parent = parent; _state = state;

            _host = new VisualElement { name = "tablet-notify-banner", pickingMode = PickingMode.Ignore };
            _host.style.position = Position.Absolute;
            _host.style.left = 0; _host.style.right = 0; _host.style.top = 0;
            _host.style.alignItems = Align.Center;
            _parent.Add(_host);

            _toast = new VisualElement { name = "tablet-toast", pickingMode = PickingMode.Ignore };
            _toast.style.position = Position.Absolute;
            _toast.style.left = 0; _toast.style.right = 0; _toast.style.bottom = 96;
            _toast.style.alignItems = Align.Center;
            _toast.style.display = DisplayStyle.None;
            var pill = new VisualElement { pickingMode = PickingMode.Ignore };
            pill.style.backgroundColor = new Color(0.08f, 0.08f, 0.10f, 0.88f);
            pill.style.paddingLeft = 20; pill.style.paddingRight = 20; pill.style.height = 40;
            pill.style.justifyContent = Justify.Center;
            pill.style.borderTopLeftRadius = pill.style.borderTopRightRadius = 20;
            pill.style.borderBottomLeftRadius = pill.style.borderBottomRightRadius = 20;
            _toastLabel = new Label { pickingMode = PickingMode.Ignore };
            _toastLabel.style.fontSize = TabletType.Body;
            _toastLabel.style.color = Color.white;
            _toastLabel.style.unityFontDefinition = Fd();
            _toastLabel.style.marginTop = 0; _toastLabel.style.marginBottom = 0;
            pill.Add(_toastLabel);
            _toast.Add(pill);
            _parent.Add(_toast);

            _state.NotificationPosted += OnPosted;
            _state.Changed += OnStateChanged;
        }

        public void Dispose()
        {
            _state.NotificationPosted -= OnPosted;
            _state.Changed -= OnStateChanged;
            _host.RemoveFromHierarchy();
            _toast.RemoveFromHierarchy();
        }

        // ---------- 轻提示 ----------
        public void ShowToast(string message)
        {
            _toastLabel.text = message;
            _toast.style.display = DisplayStyle.Flex;
            _toast.BringToFront();
            _toast.style.opacity = 0f;
            int ms = TabletTheme.MotionMs(180);
            _toast.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity") };
            _toast.style.transitionDuration = new List<TimeValue> { new TimeValue(ms, TimeUnit.Millisecond) };
            _toast.schedule.Execute(() => _toast.style.opacity = 1f).StartingIn(16);
            _toastTimer?.Pause();
            _toastTimer = _toast.schedule.Execute(() =>
            {
                _toast.style.opacity = 0f;
                _toast.schedule.Execute(() => { if (_toast.style.opacity.value <= 0.01f) _toast.style.display = DisplayStyle.None; }).StartingIn(ms + 30);
            });
            _toastTimer.ExecuteLater(1800);
        }

        // ---------- 横幅 ----------
        private bool ShouldBanner(TabletNotification n)
        {
            var ch = TabletNotify.GetChannel(n.ChannelId);
            var imp = n.Request?.Importance ?? ch.Importance;
            if (imp == TabletNotifyImportance.Silent) return false;
            if (_state.QuietMode) return false;
            if (_state.Panel != TabletSystemPanel.None) return false;
            return true;
        }

        private void OnPosted(TabletNotification n, bool isUpdate)
        {
            if (!ShouldBanner(n)) return;
            if (isUpdate && _current != null && _current.Key == n.Key && !string.IsNullOrEmpty(n.Key))
            {
                _current = n;
                RebuildCard(n, animate: false);
                RestartTimer(n);
                return;
            }
            if (isUpdate)
            {
                var kept = new Queue<TabletNotification>();
                foreach (var q in _queue) if (q.Key != n.Key) kept.Enqueue(q);
                _queue.Clear();
                foreach (var q in kept) _queue.Enqueue(q);
            }
            _queue.Enqueue(n);
            while (_queue.Count > 3) _queue.Dequeue();
            if (_current == null) ShowNext();
        }

        private void OnStateChanged()
        {
            if (_state.Panel != TabletSystemPanel.None && (_current != null || _queue.Count > 0))
            {
                _queue.Clear();
                HideCurrent(immediate: true);
            }
            else if (_current != null)
            {
                // 通知被清除/已被划掉时，横幅也随之消失
                bool alive = false;
                foreach (var x in _state.Notifications) if (x.Id == _current.Id) { alive = true; break; }
                if (!alive) HideCurrent(immediate: false);
            }
        }

        private void ShowNext()
        {
            if (_queue.Count == 0) return;
            _current = _queue.Dequeue();
            RebuildCard(_current, animate: true);
            RestartTimer(_current);
        }

        private void RestartTimer(TabletNotification n)
        {
            var ch = TabletNotify.GetChannel(n.ChannelId);
            var imp = n.Request?.Importance ?? ch.Importance;
            int ms = imp == TabletNotifyImportance.High ? 6500 : 4200;
            if (n.Request != null && n.Request.Actions.Count > 0) ms += 2000;
            _hideTimer?.Pause();
            _hideTimer = _host.schedule.Execute(() => { if (_held) { RestartTimer(n); return; } HideCurrent(false); });
            _hideTimer.ExecuteLater(ms);
        }

        private void RebuildCard(TabletNotification n, bool animate)
        {
            if (_card != null) _card.RemoveFromHierarchy();
            _card = new VisualElement { name = "tablet-notify-banner-wrap" };
            _card.style.width = 440;
            _card.style.maxWidth = Length.Percent(92);
            _card.style.marginTop = 10 + TabletTheme.SafeInsetTop;
            _card.pickingMode = PickingMode.Position;
            var inner = TabletNotifyCard.Build(n, TabletNotifyCardMode.Banner, TabletTheme.HighContrast, null, () => HideCurrent(false));
            _card.Add(inner);
            _host.Add(_card);
            _host.BringToFront();

            int ms = TabletTheme.MotionMs(320);
            _card.style.transformOrigin = new TransformOrigin(Length.Percent(50), 0);
            _card.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("translate"), new StylePropertyName("opacity"), new StylePropertyName("scale") };
            _card.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseOutBack), new EasingFunction(EasingMode.EaseOut), new EasingFunction(EasingMode.EaseOutBack) };
            if (animate && ms > 0)
            {
                _card.style.transitionDuration = new List<TimeValue> { new TimeValue(ms, TimeUnit.Millisecond), new TimeValue(ms, TimeUnit.Millisecond), new TimeValue(ms, TimeUnit.Millisecond) };
                _card.style.translate = new Translate(0, -90);
                _card.style.scale = new Scale(new Vector3(0.92f, 0.92f, 1f));
                _card.style.opacity = 0f;
                var c = _card;
                c.schedule.Execute(() =>
                {
                    c.style.translate = new Translate(0, 0);
                    c.style.scale = new Scale(Vector3.one);
                    c.style.opacity = 1f;
                }).StartingIn(16);
            }
            else
            {
                _card.style.transitionDuration = new List<TimeValue> { new TimeValue(0, TimeUnit.Millisecond), new TimeValue(0, TimeUnit.Millisecond), new TimeValue(0, TimeUnit.Millisecond) };
            }
            AttachGestures(_card, n);
        }

        private void AttachGestures(VisualElement card, TabletNotification n)
        {
            bool down = false, dragged = false;
            Vector2 start = default;
            card.RegisterCallback<PointerEnterEvent>(_ => _held = true);
            card.RegisterCallback<PointerLeaveEvent>(_ => { if (!down) _held = false; });
            card.RegisterCallback<PointerDownEvent>(e => { down = true; dragged = false; _held = true; start = e.position; });
            card.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!down) return;
                float dy = e.position.y - start.y;
                if (!dragged && Mathf.Abs(dy) > 8f) { dragged = true; card.CapturePointer(e.pointerId); }
                if (dragged)
                {
                    card.style.transitionDuration = new List<TimeValue> { new TimeValue(0, TimeUnit.Millisecond), new TimeValue(0, TimeUnit.Millisecond), new TimeValue(0, TimeUnit.Millisecond) };
                    card.style.translate = new Translate(0, Mathf.Min(0f, dy));
                }
            });
            card.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!down) return;
                down = false; _held = false;
                if (dragged)
                {
                    if (card.HasPointerCapture(e.pointerId)) card.ReleasePointer(e.pointerId);
                    if (e.position.y - start.y < -36f) HideCurrent(false);
                    else
                    {
                        int ms = TabletTheme.MotionMs(200);
                        card.style.transitionDuration = new List<TimeValue> { new TimeValue(ms, TimeUnit.Millisecond), new TimeValue(ms, TimeUnit.Millisecond), new TimeValue(ms, TimeUnit.Millisecond) };
                        card.style.translate = new Translate(0, 0);
                    }
                    return;
                }
            });
            card.RegisterCallback<ClickEvent>(e =>
            {
                if (dragged) { dragged = false; return; }
                if (e.target is Button) return; // 操作按钮自己处理
                HideCurrent(false);
                _state.Dismiss(n.Id);
                TabletNotify.Open(n);
            });
        }

        private void HideCurrent(bool immediate)
        {
            _hideTimer?.Pause();
            var card = _card;
            _current = null; _card = null; _held = false;
            if (card == null) { ShowNextSoon(0); return; }
            int ms = immediate ? 0 : TabletTheme.MotionMs(220);
            if (ms <= 0)
            {
                card.RemoveFromHierarchy();
                ShowNextSoon(0);
                return;
            }
            card.style.transitionDuration = new List<TimeValue> { new TimeValue(ms, TimeUnit.Millisecond), new TimeValue(ms, TimeUnit.Millisecond), new TimeValue(ms, TimeUnit.Millisecond) };
            card.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseIn), new EasingFunction(EasingMode.EaseIn), new EasingFunction(EasingMode.EaseIn) };
            card.style.translate = new Translate(0, -90);
            card.style.opacity = 0f;
            card.schedule.Execute(() => card.RemoveFromHierarchy()).StartingIn(ms + 20);
            ShowNextSoon(ms + 60);
        }

        private void ShowNextSoon(int delayMs)
        {
            if (_queue.Count == 0) return;
            if (delayMs <= 0) { ShowNext(); return; }
            _host.schedule.Execute(() => { if (_current == null) ShowNext(); }).StartingIn(delayMs);
        }
    }
}
