using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    public enum TabletGestureAction { None, Notifications, Controls, Back, Home, Recents, Consumed, RevealNav }

    /// <summary>Tracks one edge touch; callers supply panel coordinates and unscaled time.</summary>
    public sealed class TabletEdgeGesture
    {
        private enum Kind { None, Notifications, Controls, Bottom, Back }
        private const float VerticalDistance = 56f;
        private const float SideEdgeWidth = 32f;
        private const float StaleSeconds = 2f;
        /// <summary>底边手势热区高度；全面屏手势下导航栏不占布局。</summary>
        public const float BottomEdgeZone = 48f;
        private const float RevealDistance = 14f;
        private const float BackDistance = 64f;
        private const float HoldSeconds = 0.28f;
        private const float HoldMovementTolerance = 10f;

        private int _pointerId = -1;
        private Kind _kind;
        private Vector2 _start;
        private Vector2 _holdAnchor;
        private float _holdSince = -1f;
        private bool _recentsOpened;
        private bool _backFromLeft;

        private Vector2 _last;
        private float _beganAt;

        /// <summary>当前这次手势从按下到现在的位移，供界面做跟手反馈。</summary>
        public Vector2 Delta => _pointerId == -1 ? Vector2.zero : _last - _start;
        /// <summary>是否正在拖底边手势线（上滑回桌面 / 停住进多任务）。</summary>
        public bool IsBottomDrag => _pointerId != -1 && _kind == Kind.Bottom && !_recentsOpened;

        public int ActivePointerId => _pointerId;
        public bool HasActiveGesture => _pointerId != -1;

        public bool Begin(int pointerId, Vector2 point, float width, float height, float bottomHeight, bool blockDirect, int activeTouchCount = 1)
        {
            // 上一次手势的抬起事件丢了（被别的控件截走、窗口失焦）会一直占着，之后所有手势都“不触发”；超过两秒就当作过期
            if (_pointerId != -1 && Time.unscaledTime - _beganAt > StaleSeconds) Reset();
            if (_pointerId != -1 || width <= 0 || height <= 0) return false;
            if (activeTouchCount > 1) return false;
            if (blockDirect) return false;

            Kind kind = Kind.None;
            if (point.y <= 48f)
                kind = point.x < width * 0.5f ? Kind.Notifications : Kind.Controls;
            else if (point.y >= height - bottomHeight)
                kind = Kind.Bottom;
            else if (point.x <= SideEdgeWidth || point.x >= width - SideEdgeWidth)
                kind = Kind.Back;
            if (kind == Kind.None) return false;

            _pointerId = pointerId;
            _kind = kind;
            _start = point;
            _last = point;
            _beganAt = Time.unscaledTime;
            _backFromLeft = point.x <= SideEdgeWidth;
            _holdSince = -1f;
            _recentsOpened = false;
            return true;
        }

        public void Move(int pointerId, Vector2 point, float now)
        {
            if (pointerId == _pointerId) _last = point;
            if (pointerId != _pointerId || _kind != Kind.Bottom || _recentsOpened) return;
            Vector2 delta = point - _start;
            if (!IsVertical(delta, false))
            {
                _holdSince = -1f;
                return;
            }

            if (_holdSince < 0f || (point - _holdAnchor).sqrMagnitude > HoldMovementTolerance * HoldMovementTolerance)
            {
                _holdAnchor = point;
                _holdSince = now;
            }
        }

        public TabletGestureAction Tick(float now)
        {
            if (_kind != Kind.Bottom || _recentsOpened || _holdSince < 0f || now - _holdSince < HoldSeconds)
                return TabletGestureAction.None;
            _recentsOpened = true;
            return TabletGestureAction.Recents;
        }

        public TabletGestureAction End(int pointerId, Vector2 point)
        {
            if (pointerId != _pointerId || _kind == Kind.None) return TabletGestureAction.None;
            Vector2 delta = point - _start;
            TabletGestureAction action = TabletGestureAction.None;
            if (_recentsOpened)
                action = TabletGestureAction.Consumed;
            else if ((_kind == Kind.Notifications || _kind == Kind.Controls) && IsVertical(delta, true))
                action = _kind == Kind.Notifications ? TabletGestureAction.Notifications : TabletGestureAction.Controls;
            else if (_kind == Kind.Bottom && IsVertical(delta, false))
                action = TabletGestureAction.Home;
            else if (_kind == Kind.Bottom && delta.y <= -RevealDistance && Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * 1.25f)
                action = TabletGestureAction.RevealNav;
            else if (_kind == Kind.Back && (_backFromLeft ? delta.x >= BackDistance : delta.x <= -BackDistance) &&
                     Mathf.Abs(delta.x) > Mathf.Abs(delta.y) * 1.25f)
                action = TabletGestureAction.Back;
            Reset();
            return action;
        }

        public void Cancel(int pointerId)
        {
            if (pointerId == _pointerId || pointerId < 0) Reset();
        }

        public void Reset()
        {
            _pointerId = -1;
            _kind = Kind.None;
            _holdSince = -1f;
            _recentsOpened = false;
        }

        private static bool IsVertical(Vector2 delta, bool down)
        {
            return (down ? delta.y >= VerticalDistance : delta.y <= -VerticalDistance) &&
                   Mathf.Abs(delta.y) > Mathf.Abs(delta.x);
        }
    }
}
