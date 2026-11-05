using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public static class CabPhysicsQueries
    {
        static bool dirty;
        public static int SynchronizationCount { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { dirty = false; SynchronizationCount = 0; }
        public static void MarkDirty() => dirty = true;
        public static void Prepare()
        {
            if (!dirty) return;
            Physics.SyncTransforms(); SynchronizationCount++;
            dirty = false;
        }
    }
}
