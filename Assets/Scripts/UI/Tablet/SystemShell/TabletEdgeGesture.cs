using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    public enum TabletGestureAction { None, Notifications, Controls, Back, Home, Recents, Consumed }

    /// <summary>Tracks one edge touch; callers supply panel coordinates and unscaled time.</summary>
    public sealed class TabletEdgeGesture
    {
        private enum Kind { None, Notifications, Controls, Bottom, Back }
        private const float VerticalDistance = 72f;
        private const float BackDistance = 82f;
        private const float HoldSeconds = 0.28f;
        private const float HoldMovementTolerance = 10f;

        private int _pointerId = -1;
        private Kind _kind;
        private Vector2 _start;
        private Vector2 _holdAnchor;
        private float _holdSince = -1f;
        private bool _recentsOpened;
        private bool _backFromLeft;

        public bool Begin(int pointerId, Vector2 point, float width, float height, float bottomHeight, bool blockSideBack)
        {
            if (_pointerId != -1 || width <= 0 || height <= 0) return false;
            Kind kind = Kind.None;
            if (point.y <= 24f)
                kind = point.x < width * 0.5f ? Kind.Notifications : Kind.Controls;
            else if (point.y >= height - bottomHeight)
                kind = Kind.Bottom;
            else if ((point.x <= 18f || point.x >= width - 18f) && !blockSideBack)
                kind = Kind.Back;
            if (kind == Kind.None) return false;

            _pointerId = pointerId;
            _kind = kind;
            _start = point;
            _backFromLeft = point.x <= 18f;
            _holdSince = -1f;
            _recentsOpened = false;
            return true;
        }

        public void Move(int pointerId, Vector2 point, float now)
        {
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
            else if (_kind == Kind.Back && (_backFromLeft ? delta.x >= BackDistance : delta.x <= -BackDistance) &&
                     Mathf.Abs(delta.x) > Mathf.Abs(delta.y) * 1.25f)
                action = TabletGestureAction.Back;
            Reset();
            return action;
        }

        public void Cancel(int pointerId)
        {
            if (pointerId == _pointerId) Reset();
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
                   Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * 1.25f;
        }
    }
}
