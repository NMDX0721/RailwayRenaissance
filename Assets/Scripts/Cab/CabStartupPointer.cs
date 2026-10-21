using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RailwayRenaissance.Cab
{
    public sealed class CabStartupPointer : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] LayerMask interactionMask = ~0;
        string drivingHint;
        Font hintFont;
        public void Configure(Camera camera, int mask) { viewCamera = camera; interactionMask = mask; }
        void OnEnable() { if (viewCamera == null) viewCamera = Camera.main; }
        void Update()
        {
            var mouse = Mouse.current;
            drivingHint = null;
            if (mouse == null || viewCamera == null) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            var simulation=GetComponent<CabSimulationDriver>();
            if(simulation!=null&&simulation.isActiveAndEnabled&&simulation.BlocksPointer(mouse.position.ReadValue()))return;
            var ray = viewCamera.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out var hover, 4f, interactionMask, QueryTriggerInteraction.Ignore))
            {
                var driving = hover.collider.GetComponentInParent<CabDrivingTarget>();
                if (driving != null) drivingHint = driving.Hint;
            }
            bool primary = mouse.leftButton.wasPressedThisFrame;
            bool secondary = mouse.rightButton.wasPressedThisFrame;
            if (!primary && !secondary) return;
            ActivateRay(ray, secondary && !primary);
        }
        void OnGUI()
        {
            if (drivingHint == null) return;
            if (hintFont == null) hintFont = Resources.Load<Font>("Fonts/SarasaMonoSC-Regular");
            var old = GUI.skin.font;
            if (hintFont != null) GUI.skin.font = hintFont;
            GUI.Box(new Rect(Mathf.Max(0, (Screen.width-520)/2), 12, 520, 48),
                drivingHint + "\n左键增加／施加 · 右键减少／缓解 · F2收起测试面板");
            GUI.skin.font = old;
        }
        public bool ActivateRay(Ray ray, bool secondary = false)
        {
            if (!Physics.Raycast(ray, out var hit, 4f, interactionMask, QueryTriggerInteraction.Ignore)) return false;
            var target = hit.collider.GetComponent<CabStartupClickTarget>();
            if (target != null) return target.Activate(secondary);
            var driving = hit.collider.GetComponentInParent<CabDrivingTarget>();
            return driving != null && driving.Activate(secondary);
        }
    }
}
