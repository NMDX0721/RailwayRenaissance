using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    /// <summary>
    /// 桌面图标拖动：长按图标拿起（半透明副本跟着手指），拖到别的格子、文件夹或屏幕边缘（翻页）后松手放下。
    /// 只负责手势与副本；放到哪里、怎么改排布由使用方通过回调决定。手指在拿起前移动超过 10 像素就当作翻页，不触发拿起。
    /// </summary>
    public sealed class TabletDesktopDrag
    {
        public enum SlotKind { Page0, Page1, Folder, FolderPanel }

        public struct Slot
        {
            public Rect rect;
            public SlotKind kind;
            public int index;
        }

        // 防误触：长按更久才拿起，手指轻微抖动不算移动；文件夹只认中间一块；贴边翻页要停更久；拖出文件夹要明显离开面板
        private const float HoldMs = 480f;
        private const float MoveCancel = 18f;
        private const float EdgeZone = 40f;
        private const float EdgeHoldSeconds = 0.9f;
        private const float FolderCoreScale = 0.5f;
        private const float PanelExitMargin = 36f;

        private static float s_justDraggedUntil;
        /// <summary>刚放下图标的一小段时间内，图标上的点击不当作“打开应用”。</summary>
        public static bool JustDragged => Time.unscaledTime < s_justDraggedUntil;

        private readonly VisualElement _host;
        private readonly Func<string, VisualElement> _makeGhost;
        private readonly Func<bool, List<Slot>> _slots;
        private readonly Action<string, Slot?, bool> _onDrop;
        private readonly Action<int> _onEdge;
        private readonly Action<bool> _onActive;

        private VisualElement _ghost, _source;
        private string _app;
        private bool _fromFolder;
        private int _pointerId = -1;
        private float _edgeSince = -1f;
        private float _lastEdgeFlip;

        public bool Active { get; private set; }

        /// <param name="slots">按“是否从文件夹里拖”返回当前可放下的区域（世界坐标）。</param>
        /// <param name="onDrop">放下：应用名、命中的区域（没命中为 null）、是否从文件夹里拖出来。</param>
        public TabletDesktopDrag(VisualElement host, Func<string, VisualElement> makeGhost, Func<bool, List<Slot>> slots,
            Action<string, Slot?, bool> onDrop, Action<int> onEdge, Action<bool> onActive)
        {
            _host = host; _makeGhost = makeGhost; _slots = slots; _onDrop = onDrop; _onEdge = onEdge; _onActive = onActive;
        }

        public void Attach(VisualElement cell, string app, bool fromFolder = false)
        {
            int pid = -1;
            Vector2 down = Vector2.zero;
            IVisualElementScheduledItem hold = null;

            cell.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0 || Active) return;
                pid = e.pointerId; down = e.position;
                hold?.Pause();
                hold = cell.schedule.Execute(() => { if (pid != -1) Begin(cell, app, pid, down, fromFolder); }).StartingIn((long)HoldMs);
            });
            cell.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (e.pointerId != pid) return;
                if (!Active)
                {
                    if (((Vector2)e.position - down).sqrMagnitude > MoveCancel * MoveCancel) { hold?.Pause(); pid = -1; }
                }
                else if (_source == cell) Move(e.position);
            });
            cell.RegisterCallback<PointerUpEvent>(e =>
            {
                hold?.Pause();
                if (Active && _source == cell && e.pointerId == pid) End(e.position, true);
                pid = -1;
            });
            cell.RegisterCallback<PointerCancelEvent>(e => { hold?.Pause(); if (Active && _source == cell) End(e.position, false); pid = -1; });
            cell.RegisterCallback<PointerCaptureOutEvent>(e => { if (Active && _source == cell) End(Vector2.zero, false); });
        }

        private void Begin(VisualElement cell, string app, int pointerId, Vector2 pos, bool fromFolder)
        {
            if (Active) return;
            Active = true; _source = cell; _app = app; _fromFolder = fromFolder; _pointerId = pointerId;
            cell.CapturePointer(pointerId);
            cell.style.opacity = 0.25f;
            _ghost = _makeGhost(app);
            if (_ghost != null)
            {
                _ghost.pickingMode = PickingMode.Ignore;
                _ghost.style.position = Position.Absolute;
                _ghost.style.scale = new Scale(new Vector3(1.18f, 1.18f, 1f));
                _ghost.style.opacity = 0.92f;
                _host.Add(_ghost);
                _ghost.BringToFront();
                PlaceGhost(pos);
            }
            _edgeSince = -1f;
            _onActive?.Invoke(true);
        }

        private void PlaceGhost(Vector2 world)
        {
            if (_ghost == null) return;
            Vector2 origin = _host.worldBound.position;
            var size = new Vector2(_ghost.resolvedStyle.width, _ghost.resolvedStyle.height);
            if (float.IsNaN(size.x) || size.x < 1f) size = new Vector2(60f, 60f);
            _ghost.style.left = world.x - origin.x - size.x * 0.5f;
            _ghost.style.top = world.y - origin.y - size.y * 0.5f;
        }

        private Slot? Hit(Vector2 world)
        {
            foreach (var s in _slots(_fromFolder))
            {
                Rect r = s.rect;
                if (s.kind == SlotKind.Folder)
                {
                    float w = r.width * FolderCoreScale, h = r.height * FolderCoreScale;
                    r = new Rect(r.center.x - w * 0.5f, r.center.y - h * 0.5f, w, h);
                }
                else if (s.kind == SlotKind.FolderPanel)
                    r = new Rect(r.xMin - PanelExitMargin, r.yMin - PanelExitMargin, r.width + PanelExitMargin * 2f, r.height + PanelExitMargin * 2f);
                if (r.Contains(world)) return s;
            }
            return null;
        }

        private void Move(Vector2 world)
        {
            PlaceGhost(world);
            var hit = Hit(world);
            bool overFolder = hit.HasValue && hit.Value.kind == SlotKind.Folder;
            if (_ghost != null) _ghost.style.scale = new Scale(overFolder ? new Vector3(0.8f, 0.8f, 1f) : new Vector3(1.18f, 1.18f, 1f));

            // 贴着屏幕左右边缘停一会儿：翻页，方便把图标拖到另一页
            Rect hostRect = _host.worldBound;
            int dir = world.x < hostRect.xMin + EdgeZone ? -1 : world.x > hostRect.xMax - EdgeZone ? 1 : 0;
            if (dir == 0 || _fromFolder) { _edgeSince = -1f; return; }
            if (_edgeSince < 0f) _edgeSince = Time.unscaledTime;
            if (Time.unscaledTime - _edgeSince >= EdgeHoldSeconds && Time.unscaledTime - _lastEdgeFlip > 0.9f)
            {
                _lastEdgeFlip = Time.unscaledTime; _edgeSince = -1f;
                _onEdge?.Invoke(dir);
            }
        }

        private void End(Vector2 world, bool drop)
        {
            if (!Active) return;
            Active = false;
            s_justDraggedUntil = Time.unscaledTime + 0.3f;
            if (_source != null)
            {
                if (_source.HasPointerCapture(_pointerId)) _source.ReleasePointer(_pointerId);
                _source.style.opacity = 1f;
            }
            if (_ghost != null) { _ghost.RemoveFromHierarchy(); _ghost = null; }
            string app = _app; bool fromFolder = _fromFolder;
            _source = null; _pointerId = -1; _edgeSince = -1f;
            _onActive?.Invoke(false);
            if (drop) _onDrop?.Invoke(app, Hit(world), fromFolder);
        }
    }
}
