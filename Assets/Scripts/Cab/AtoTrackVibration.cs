using System.Collections.Generic;
using UnityEngine;

namespace RailwayRenaissance.Cab
{
    // Visual-only ballast response to the passing train. The track kit splits the modelled stones into chunks of two sleeper bays
    // ("Ballast_Stones_NN"); chunks under and just behind the vehicle hop and rock with an amplitude that follows speed.
    // Nothing here touches colliders, the route or the simulation: it is presentation, like the suspension visuals in CabRailMotion.
    public sealed class AtoTrackVibration : MonoBehaviour
    {
        [SerializeField] float fullSpeedMps = 20f;
        [SerializeField] float maxHopMetres = 0.008f;
        [SerializeField] float maxTiltDegrees = 1.2f;
        const float UnderVehicle = 9f, WakeAhead = 6f, WakeBehind = 14f;

        struct Chunk
        {
            public Transform transform;
            public Vector3 basePosition, pivot;
            public float station, phase;
            public bool shaken;
        }

        CabRailMotion motion;
        readonly List<Chunk> chunks = new List<Chunk>();
        double lastStation;
        float speed;
        bool haveLast;

        void Start() => Collect();

        public void Collect()
        {
            chunks.Clear();
            motion = FindAnyObjectByType<CabRailMotion>();
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                if (!r.name.StartsWith("Ballast_Stones_", System.StringComparison.Ordinal)) continue;
                var filter = r.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                var t = r.transform;
                // Route distance of the chunk centre: world z = 8.6 - s (see AtoTestLineBuilder).
                float s = 8.6f - r.bounds.center.z;
                chunks.Add(new Chunk
                {
                    transform = t,
                    basePosition = t.localPosition,
                    pivot = filter.sharedMesh.bounds.center,
                    station = s,
                    phase = Mathf.Abs(Mathf.Sin((t.name.GetHashCode() & 0xffff) * 12.9898f + s * 3.1f)) * 50f,
                });
            }
        }

        void LateUpdate()
        {
            if (motion == null || chunks.Count == 0) return;
            float dt = Time.deltaTime;
            double station = motion.Station;
            if (haveLast && dt > 1e-5f) speed = Mathf.Lerp(speed, Mathf.Abs((float)((station - lastStation) / dt)), Mathf.Clamp01(dt * 8f));
            lastStation = station; haveLast = true;
            float level = Mathf.Clamp01(speed / fullSpeedMps);
            float time = Time.time;

            for (int i = 0; i < chunks.Count; i++)
            {
                var c = chunks[i];
                float d = c.station - (float)station;                    // positive: ahead of the vehicle centre
                float w = d > UnderVehicle ? Mathf.Exp(-(d - UnderVehicle) / WakeAhead * 2f)
                        : d < -UnderVehicle ? Mathf.Exp(-(-d - UnderVehicle) / WakeBehind * 2f) : 1f;
                float amp = level * w;
                if (amp < 0.01f)
                {
                    if (c.shaken) { c.transform.localPosition = c.basePosition; c.transform.localRotation = Quaternion.identity; c.shaken = false; chunks[i] = c; }
                    continue;
                }
                float p1 = time * 17f + c.phase, p2 = time * 29f + c.phase * 1.7f, p3 = time * 13f + c.phase * 0.6f;
                float jitter = Mathf.Sin(p1) * 0.6f + Mathf.Sin(p2) * 0.4f;
                var rot = Quaternion.Euler(maxTiltDegrees * amp * jitter, 0f, maxTiltDegrees * amp * Mathf.Sin(p3));
                float hop = Mathf.Max(0f, jitter) * maxHopMetres * amp;
                c.transform.localRotation = rot;
                c.transform.localPosition = c.basePosition + (c.pivot - rot * c.pivot) + new Vector3(0f, hop, 0f);
                c.shaken = true; chunks[i] = c;
            }
        }
    }
}
