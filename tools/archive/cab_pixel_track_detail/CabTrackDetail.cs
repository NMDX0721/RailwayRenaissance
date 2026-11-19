using System.Collections.Generic;
using UnityEngine;

namespace RailwayRenaissance.Cab
{
    /// <summary>
    /// 窗外轨道细节：把枕木、垫板、扣件、道砟碎石、分层钢轨合并成少量网格，
    /// 每个枕木节段 3 次绘制；并提供车速驱动的道砟振动与枕木受载下沉。
    /// </summary>
    internal static class CabTrackDetail
    {
        public const int StoneVariants = 6;

        private static readonly Vector3[] s_N = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.up, Vector3.down };
        private static readonly Vector3[] s_U = { Vector3.up, Vector3.right, Vector3.forward, Vector3.up, Vector3.right, Vector3.forward };
        private static readonly Vector3[] s_V = { Vector3.right, Vector3.up, Vector3.up, Vector3.forward, Vector3.forward, Vector3.right };

        private sealed class Builder
        {
            private readonly List<Vector3> m_Verts = new List<Vector3>();
            private readonly List<int>[] m_Tris;

            public Builder(int subMeshes)
            {
                m_Tris = new List<int>[subMeshes];
                for (int i = 0; i < subMeshes; i++) m_Tris[i] = new List<int>();
            }

            public void Box(int sub, Vector3 center, Vector3 size, Quaternion rot)
            {
                for (int f = 0; f < 6; f++)
                {
                    Vector3 n = Vector3.Scale(s_N[f], size) * 0.5f;
                    Vector3 u = Vector3.Scale(s_U[f], size) * 0.5f;
                    Vector3 v = Vector3.Scale(s_V[f], size) * 0.5f;
                    int b = m_Verts.Count;
                    m_Verts.Add(center + rot * (n - u - v));
                    m_Verts.Add(center + rot * (n - u + v));
                    m_Verts.Add(center + rot * (n + u + v));
                    m_Verts.Add(center + rot * (n + u - v));
                    var t = m_Tris[sub];
                    t.Add(b); t.Add(b + 1); t.Add(b + 2);
                    t.Add(b); t.Add(b + 2); t.Add(b + 3);
                }
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(m_Verts);
                mesh.subMeshCount = m_Tris.Length;
                for (int i = 0; i < m_Tris.Length; i++) mesh.SetTriangles(m_Tris[i], i);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        public static Material[] BodyMaterials()
        {
            return new[]
            {
                CabPalette.GetMaterial(CabPalette.ColorSleeper),
                CabPalette.GetMaterial(CabPalette.HexColor("#3A2A1D")),   // 枕木顶面磨亮
                CabPalette.GetMaterial(CabPalette.HexColor("#2B2E31")),   // 垫板 / 扣件座
                CabPalette.GetMaterial(CabPalette.HexColor("#6B4A32")),   // 弹条锈色
            };
        }

        public static Material[] StoneMaterials()
        {
            return new[]
            {
                CabPalette.GetMaterial(CabPalette.ColorBallast),
                CabPalette.GetMaterial(CabPalette.HexColor("#5A5349")),
                CabPalette.GetMaterial(CabPalette.HexColor("#322E2A")),
            };
        }

        public static Material[] RailMaterials()
        {
            return new[]
            {
                CabPalette.GetMaterial(CabPalette.ColorSteelRail),
                CabPalette.GetMaterial(CabPalette.HexColor("#C7D2DC")),   // 轨顶踏面光亮
                CabPalette.GetMaterial(CabPalette.HexColor("#5B4636")),   // 轨腰锈蚀
            };
        }

        /// <summary>枕木 + 垫板 + 弹条扣件（每股钢轨两侧各一对）。</summary>
        public static Mesh BuildBody(float halfGauge)
        {
            var b = new Builder(4);
            b.Box(0, new Vector3(0f, 0.04f, 0f), new Vector3(2.4f, 0.11f, 0.22f), Quaternion.identity);
            b.Box(1, new Vector3(0f, 0.0975f, 0f), new Vector3(2.3f, 0.012f, 0.16f), Quaternion.identity);
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * halfGauge;
                b.Box(2, new Vector3(x, 0.109f, 0f), new Vector3(0.20f, 0.012f, 0.19f), Quaternion.identity);
                for (int side = -1; side <= 1; side += 2)
                {
                    float cx = x + side * 0.058f;
                    b.Box(3, new Vector3(cx, 0.128f, 0f), new Vector3(0.036f, 0.026f, 0.05f), Quaternion.identity);
                    b.Box(2, new Vector3(x + side * 0.09f, 0.122f, 0f), new Vector3(0.028f, 0.02f, 0.07f), Quaternion.identity);
                }
            }
            return b.ToMesh("CabTrackBody");
        }

        /// <summary>两枕之间及道肩的碎石，variant 决定随机种子。</summary>
        public static Mesh BuildStones(int variant, float spacing)
        {
            var rng = new System.Random(7919 * (variant + 1));
            var b = new Builder(3);
            float half = spacing * 0.5f;
            int count = 26;
            for (int i = 0; i < count; i++)
            {
                // 主要落在枕木盒之间，少量爬到道肩
                bool shoulder = i >= 20;
                float x = shoulder
                    ? (rng.Next(2) == 0 ? -1 : 1) * (1.28f + (float)rng.NextDouble() * 0.45f)
                    : ((float)rng.NextDouble() * 2.4f - 1.2f);
                float zRange = half - 0.12f;
                float z = shoulder ? ((float)rng.NextDouble() * 2f - 1f) * half
                                   : (rng.Next(2) == 0 ? -1 : 1) * (0.12f + (float)rng.NextDouble() * zRange);
                float s = 0.05f + (float)rng.NextDouble() * 0.07f;
                Quaternion q = Quaternion.Euler((float)rng.NextDouble() * 50f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 50f);
                float y = shoulder ? 0.01f - (Mathf.Abs(x) - 1.28f) * 0.18f : 0.03f + (float)rng.NextDouble() * 0.025f;
                b.Box(rng.Next(3), new Vector3(x, y, z), new Vector3(s * 1.3f, s * 0.8f, s), q);
            }
            return b.ToMesh("CabTrackStones" + variant);
        }

        /// <summary>分层钢轨：底脚、轨腰、轨头与踏面亮带。长度沿 z。</summary>
        public static Mesh BuildRail(float length)
        {
            var b = new Builder(3);
            b.Box(0, new Vector3(0f, 0.105f, 0f), new Vector3(0.13f, 0.03f, length), Quaternion.identity);
            b.Box(2, new Vector3(0f, 0.158f, 0f), new Vector3(0.035f, 0.08f, length), Quaternion.identity);
            b.Box(0, new Vector3(0f, 0.208f, 0f), new Vector3(0.075f, 0.04f, length), Quaternion.identity);
            b.Box(1, new Vector3(0f, 0.2315f, 0f), new Vector3(0.04f, 0.005f, length), Quaternion.identity);
            return b.ToMesh("CabTrackRail");
        }

        /// <summary>
        /// 车速驱动的振动：道砟随机颤动（近车端最强）与枕木受载下沉。
        /// speedMps 为 0 时严格回到静止姿态。
        /// </summary>
        public static void Animate(IList<Transform> bodies, IList<Transform> stones, IList<Transform> roots, float spacing, float speedMps, float time)
        {
            float a = Mathf.Clamp01(speedMps / 25f);
            for (int i = 0; i < roots.Count; i++)
            {
                float z = roots[i].localPosition.z;
                // 轴载影响沿线前方衰减：下沉与颤动幅度都在车头前最大
                float near = Mathf.Exp(-Mathf.Max(0f, z) / 14f);
                float amp = a * (0.35f + 0.65f * near);

                float p1 = time * 61f + i * 2.17f;
                float p2 = time * 97f + i * 5.31f;
                float jitter = (Mathf.Sin(p1) * 0.6f + Mathf.Sin(p2) * 0.4f);
                float sway = Mathf.Sin(time * 43f + i * 1.3f);

                if (bodies[i] != null)
                {
                    float dip = -0.012f * a * near - 0.0025f * amp * jitter;
                    bodies[i].localPosition = new Vector3(0f, dip, 0f);
                    bodies[i].localRotation = Quaternion.Euler(0f, 0f, 0.35f * amp * sway);
                }
                if (stones[i] != null)
                {
                    float hop = Mathf.Max(0f, jitter) * 0.014f * amp;
                    stones[i].localPosition = new Vector3(0f, hop, 0f);
                    stones[i].localRotation = Quaternion.Euler(1.6f * amp * jitter, 0f, 2.2f * amp * sway);
                }
            }
        }
    }
}
