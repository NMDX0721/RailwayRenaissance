using UnityEngine;

namespace RailwayRenaissance.Cab
{
    // Interior partition door: deliberately separate from the four exterior side doors.
    [DisallowMultipleComponent]
    public sealed class CabPassageDoor : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] float openFraction;
        public float OpenFraction => openFraction;
        public string Hint => "客舱隔门 · " + (openFraction > .5f ? "已打开" : "已关闭");
        void Awake() => SetOpen(openFraction);
        void OnValidate() => SetOpen(openFraction);
        public void SetOpen(float fraction)
        {
            openFraction = Mathf.Clamp01(fraction);
            transform.localRotation = Quaternion.Euler(0, 90 * openFraction, 0);
            CabPhysicsQueries.MarkDirty();
        }
        public bool Toggle() { SetOpen(openFraction > .5f ? 0 : 1); return true; }
    }
}
