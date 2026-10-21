using UnityEngine;

namespace RailwayRenaissance.Cab
{
    [DisallowMultipleComponent]
    public sealed class CabSideDoor : MonoBehaviour
    {
        [SerializeField] float openAngle;
        [SerializeField, Range(0,1)] float openFraction;
        [SerializeField] Transform latch;
        [SerializeField] Vector3 latchClosedPosition;
        public float OpenFraction => openFraction;
        void Awake() => SetOpen(openFraction);
        void OnValidate() => SetOpen(openFraction);
        public void Configure(float angle,Transform lockBolt=null)
        {
            openAngle=angle;latch=lockBolt;
            if(latch!=null)latchClosedPosition=latch.localPosition;
            SetOpen(0);
        }
        public void SetOpen(float fraction)
        {
            openFraction=Mathf.Clamp01(fraction);
            if(latch!=null)latch.localPosition=latchClosedPosition+(openFraction>.001f?Vector3.forward*.035f:Vector3.zero);
            transform.localRotation=Quaternion.Euler(0,openAngle*openFraction,0);
        }
        [ContextMenu("Open door inward")] public void Open() => SetOpen(1);
        [ContextMenu("Close door")] public void Close() => SetOpen(0);
    }
}
