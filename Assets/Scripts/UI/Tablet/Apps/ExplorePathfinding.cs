using System;
using System.Collections.Generic;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 探索场景的网格寻路：二叉堆 A*（8 向、不切角）。与视图解耦，只依赖「格子是否可走」。
    /// 之前 open 表是线性扫描 + List.Contains，大地图/多区域会变慢。
    /// </summary>
    public static class ExplorePathfinding
    {
        private static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        /// <summary>格子序列（含起点与终点）；不可达返回 null。</summary>
        public static List<Vector2Int> FindPath(int w, int h, Func<int, int, bool> walk, Vector2Int start, Vector2Int goal)
        {
            if (!walk(goal.x, goal.y) || !walk(start.x, start.y)) return null;
            int n = w * h;
            var g = new float[n];
            var from = new int[n];
            var state = new byte[n];          // 0 未见 1 在 open 2 已关闭
            for (int i = 0; i < n; i++) { g[i] = float.MaxValue; from[i] = -1; }
            int si = start.y * w + start.x, gi = goal.y * w + goal.x;
            g[si] = 0f;

            var heap = new Heap(n);
            heap.Push(si, Heuristic(start.x, start.y, goal));
            state[si] = 1;
            while (heap.Count > 0)
            {
                int cur = heap.Pop();
                if (state[cur] == 2) continue;      // 惰性删除的旧条目
                state[cur] = 2;
                if (cur == gi) break;
                int cx = cur % w, cy = cur / w;
                foreach (var d in Dirs)
                {
                    int nx = cx + d.x, ny = cy + d.y;
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h || !walk(nx, ny)) continue;
                    if (d.x != 0 && d.y != 0 && (!walk(cx + d.x, cy) || !walk(cx, cy + d.y))) continue;   // 不切角
                    int ni = ny * w + nx;
                    if (state[ni] == 2) continue;
                    float ng = g[cur] + (d.x != 0 && d.y != 0 ? 1.414f : 1f);
                    if (ng < g[ni])
                    {
                        g[ni] = ng; from[ni] = cur; state[ni] = 1;
                        heap.Push(ni, ng + Heuristic(nx, ny, goal));
                    }
                }
            }
            if (from[gi] < 0 && gi != si) return null;
            var path = new List<Vector2Int>();
            for (int c = gi; c != -1; c = from[c]) path.Add(new Vector2Int(c % w, c / w));
            path.Reverse();
            return path;
        }

        /// <summary>从 t 向外按切比雪夫环找最近的可走格。</summary>
        public static bool NearestWalkable(Func<int, int, bool> walk, Vector2Int t, int maxRadius, out Vector2Int result)
        {
            for (int r = 0; r <= maxRadius; r++)
            {
                bool found = false; float best = float.MaxValue; Vector2Int pick = t;
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                        if (!walk(t.x + dx, t.y + dy)) continue;
                        float dist = dx * dx + dy * dy;      // 同一环内取欧氏距离最近，避免偏向某个方向
                        if (dist < best) { best = dist; pick = new Vector2Int(t.x + dx, t.y + dy); found = true; }
                    }
                if (found) { result = pick; return true; }
            }
            result = t;
            return false;
        }

        private static float Heuristic(int x, int y, Vector2Int goal)
        {
            int dx = Mathf.Abs(x - goal.x), dy = Mathf.Abs(y - goal.y);
            return Mathf.Max(dx, dy) + 0.414f * Mathf.Min(dx, dy);
        }

        /// <summary>最小堆（按 f 值），允许重复入堆，弹出时用 state 过滤旧条目。</summary>
        private sealed class Heap
        {
            private readonly List<(float f, int id)> a;
            public Heap(int capacity) { a = new List<(float, int)>(Mathf.Min(capacity, 256)); }
            public int Count => a.Count;

            public void Push(int id, float f)
            {
                a.Add((f, id));
                int i = a.Count - 1;
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (a[p].f <= a[i].f) break;
                    (a[p], a[i]) = (a[i], a[p]);
                    i = p;
                }
            }

            public int Pop()
            {
                int top = a[0].id;
                int last = a.Count - 1;
                a[0] = a[last]; a.RemoveAt(last);
                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1, r = l + 1, m = i;
                    if (l < a.Count && a[l].f < a[m].f) m = l;
                    if (r < a.Count && a[r].f < a[m].f) m = r;
                    if (m == i) break;
                    (a[m], a[i]) = (a[i], a[m]);
                    i = m;
                }
                return top;
            }
        }
    }
}
