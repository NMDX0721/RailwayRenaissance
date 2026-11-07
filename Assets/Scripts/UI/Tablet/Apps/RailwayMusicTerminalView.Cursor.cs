using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // The game draws its own pixel cursor: over anything that can be pressed or dragged it becomes the hand, over a text field the I-beam.
    public sealed partial class RailwayMusicTerminalView
    {
        enum CursorKind { Arrow, Hand, IBeam }
        static Texture2D handCursor, ibeamCursor;
        CursorKind shownCursor = CursorKind.Arrow;

        void BindCursor()
        {
            root.RegisterCallback<PointerOverEvent>(e => SetCursor(KindOf(e.target as VisualElement)));
            root.RegisterCallback<PointerLeaveEvent>(_ => SetCursor(CursorKind.Arrow));
            root.RegisterCallback<DetachFromPanelEvent>(_ => SetCursor(CursorKind.Arrow));
            root.schedule.Execute(PlaceImeWindow).Every(40);
        }

        // The pixel-art cursor scales the panel itself, so the IME candidate window would land far from the field: keep it right under the field being typed in.
        void PlaceImeWindow()
        {
            if (root?.panel == null) return;
            var focused = root.panel.focusController?.focusedElement as VisualElement;
            var field = focused as TextField ?? focused?.GetFirstAncestorOfType<TextField>();
            if (field == null) return;
            var a = RuntimePanelUtils.ScreenToPanel(root.panel, Vector2.zero); var b = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(100, 100));
            Vector2 scale = (b - a) / 100f;
            if (Mathf.Abs(scale.x) < 1e-4f || Mathf.Abs(scale.y) < 1e-4f) return;
            Vector2 panelPoint = new Vector2(field.worldBound.xMin + 12f, field.worldBound.yMax);
            Vector2 screenTopLeft = new Vector2((panelPoint.x - a.x) / scale.x, (panelPoint.y - a.y) / scale.y);
            LegacyInputAdapter.SetImeCursor(new Vector2(screenTopLeft.x, Screen.height - screenTopLeft.y));
        }

        static CursorKind KindOf(VisualElement element)
        {
            for (var e = element; e != null; e = e.parent)
            {
                if (e is TextField || e.ClassListContains("unity-base-text-field__input")) return CursorKind.IBeam;
                if (e is Button || e is Slider || e is Toggle || e is Foldout || e is DropdownField || e.ClassListContains("music-track-row") || e.ClassListContains("music-cover-tile") || e.ClassListContains("unity-base-slider__dragger") || e.ClassListContains("unity-foldout__toggle"))
                    return e.enabledInHierarchy ? CursorKind.Hand : CursorKind.Arrow;
                if (e.ClassListContains("music-modal") || e.name == "music-overlay") break;
            }
            return CursorKind.Arrow;
        }

        void SetCursor(CursorKind kind)
        {
            if (kind == shownCursor) return;
            shownCursor = kind;
            if (kind == CursorKind.Hand)
            {
                if (handCursor == null) handCursor = LoginManager.LoadCursorTexture("Cursors/cursor_hand", 3);
                UnityEngine.Cursor.SetCursor(handCursor, Vector2.zero, CursorMode.ForceSoftware);
            }
            else if (kind == CursorKind.IBeam)
            {
                if (ibeamCursor == null) ibeamCursor = LoginManager.LoadCursorTexture("Cursors/cursor_ibeam", 3);
                UnityEngine.Cursor.SetCursor(ibeamCursor, new Vector2(ibeamCursor.width / 2, ibeamCursor.height / 2), CursorMode.ForceSoftware);
            }
            else UnityEngine.Cursor.SetCursor(LoginManager.GetArrowTexture(), Vector2.zero, CursorMode.ForceSoftware);
        }
    }
}
