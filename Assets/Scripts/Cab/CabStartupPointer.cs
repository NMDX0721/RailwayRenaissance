using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RailwayRenaissance.Cab
{
    public sealed class CabStartupPointer : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] LayerMask interactionMask = ~0;
        public void Configure(Camera camera, int mask) { viewCamera = camera; interactionMask = mask; }
        void OnEnable() { if (viewCamera == null) viewCamera = Camera.main; }
        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || viewCamera == null) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            bool primary = mouse.leftButton.wasPressedThisFrame;
            bool secondary = mouse.rightButton.wasPressedThisFrame;
            if (!primary && !secondary) return;
            ActivateRay(viewCamera.ScreenPointToRay(mouse.position.ReadValue()), secondary && !primary);
        }
        public bool ActivateRay(Ray ray, bool secondary = false)
        {
            if (!Physics.Raycast(ray, out var hit, 4f, interactionMask, QueryTriggerInteraction.Ignore)) return false;
            var target = hit.collider.GetComponent<CabStartupClickTarget>();
            return target != null && target.Activate(secondary);
        }
    }
}
