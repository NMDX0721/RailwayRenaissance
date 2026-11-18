using System.Collections.Generic;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    public enum FixtureKind
    {
        Door,      // 靠近自动开、离开自动关（closeness 驱动）
        Gate,      // 由游戏状态驱动（道口栏杆、库房卷帘）
        Light,     // 灯：开/关
        Signal,    // 信号机：红 → 绿
        Ambient,   // 循环环境动画（炊烟、水面、树摇）
        Sign,      // 随政治压力档（0–4）取帧的告示/封条
        Decal      // 贴花：单帧透明贴图，按沙能渗透度出现
    }

    /// <summary>
    /// 室外整图上的「动画补丁」：在原画上盖一小块带帧的贴图。
    /// 约定（给美术）：补丁贴图是横向帧条 fx_&lt;id&gt;.png，每帧尺寸 = 原图矩形 (w×h)，
    /// paintedFrame 那一帧必须与原画该矩形逐像素一致，这样无补丁/素材缺失时画面不变。
    /// 素材缺失时，门用程序占位（半透明深色门洞）。
    /// </summary>
    public sealed class Fixture
    {
        public string id, label;
        public FixtureKind kind;
        public bool east;                 // false = 站区整图 1620×971；true = 东段 1024×1536
        public int x, y, w, h;            // 原图像素，左上为原点
        public int frames = 2;
        public int paintedFrame;          // 与原画一致的那一帧（门=0，库房卷帘=最后一帧）
        public float fps = 4f;            // Ambient 播放速度
        public float speed = 5f;          // 非 Ambient：每秒变化量（1/秒 = 1 秒走完全程）
        public Vector2Int trigger = new Vector2Int(-1, -1);   // Door：触发格
        public float radius = 1.8f;                           // Door：触发半径（格）
        public float minPen;                                  // Decal：沙能渗透度达到该值才显示
        public RectInt foot;                                  // Decal：占用的世界格（显示时不可通行）；w/h=0 表示不占格
        public float t;                   // 当前进度 0..1
        public float target;              // 目标进度
        public string asset => "fx_" + id;

        public int Frame
        {
            get
            {
                if (kind == FixtureKind.Ambient) return Mathf.FloorToInt(Time.unscaledTime * fps) % Mathf.Max(1, frames);
                return Mathf.Clamp(Mathf.RoundToInt(t * (frames - 1)), 0, frames - 1);
            }
        }
    }

    /// <summary>
    /// 沙能飞行器掠过的地面影子（世界观：铁路已被取代）。约每 70–180 秒一架（渗透度越高越频繁），
    /// 沿水平方向飞过，贴图 fx_flyer_shadow_a/b/c 约定「机头朝右」，向左飞时镜像；缺贴图时用程序影子。
    /// </summary>
    public static class ExploreFlyers
    {
        public struct Flyer { public Vector2 pos, vel; public Vector2 size; public int type; }
        public static readonly List<Flyer> Active = new List<Flyer>();
        private static float nextAt = 25f;
        private static float clock;

        public static void Update(float dt, float worldW, float worldH, float penetration)
        {
            clock += dt;
            if (clock >= nextAt)
            {
                Spawn(worldW, worldH);
                nextAt = clock + Mathf.Lerp(180f, 70f, Mathf.Clamp01(penetration)) * Random.Range(0.8f, 1.2f);
            }
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var f = Active[i];
                f.pos += f.vel * dt;
                if (f.pos.x < -400f || f.pos.x > worldW + 400f) Active.RemoveAt(i); else Active[i] = f;
            }
        }

        public static void Spawn(float worldW, float worldH)
        {
            int type = Random.Range(0, 3);
            Vector2 size = type == 0 ? new Vector2(160f, 80f) : type == 1 ? new Vector2(120f, 60f) : new Vector2(200f, 100f);
            bool rightward = Random.value < 0.5f;
            float speed = Random.Range(110f, 170f);
            float y = Random.Range(worldH * 0.15f, worldH * 0.85f);
            Active.Add(new Flyer
            {
                type = type, size = size,
                pos = new Vector2(rightward ? -300f : worldW + 300f, y),
                vel = new Vector2(rightward ? speed : -speed, Random.Range(-10f, 10f))
            });
        }

        public static void Clear() { Active.Clear(); clock = 0f; nextAt = 25f; }
    }

    public static class ExploreFixtures
    {
        public const int StationW = 1620, StationH = 971, EastW = 1024, EastH = 1536;
        public static readonly List<Fixture> All = new List<Fixture>();
        private static readonly Dictionary<string, Fixture> byId = new Dictionary<string, Fixture>();

        static ExploreFixtures() { Build(); foreach (var f in All) ApplySidecar(f); }

        [System.Serializable]
        private class Sidecar { public int x = -1, y = -1, w = -1, h = -1, frames = -1, paintedFrame = -1; public float fps = -1f; }

        /// <summary>
        /// 美术可在 Fixtures/ 下放 fx_&lt;id&gt;.json 覆盖矩形/帧数/原画帧/帧率（只写要改的字段），
        /// 这样补丁尺寸微调不需要改代码。例：{"x":370,"y":248,"w":78,"h":102,"frames":6}
        /// </summary>
        private static void ApplySidecar(Fixture f)
        {
            var ta = Resources.Load<TextAsset>("Art/Explore/Fixtures/" + f.asset);
            if (ta == null) return;
            var s = JsonUtility.FromJson<Sidecar>(ta.text);
            if (s == null) return;
            if (s.x >= 0) f.x = s.x;
            if (s.y >= 0) f.y = s.y;
            if (s.w > 0) f.w = s.w;
            if (s.h > 0) f.h = s.h;
            if (s.frames > 0) f.frames = s.frames;
            if (s.paintedFrame >= 0) f.paintedFrame = s.paintedFrame;
            if (s.fps > 0f) f.fps = s.fps;
            f.t = f.target = f.paintedFrame == 0 ? 0f : 1f;
        }

        private static Fixture Add(Fixture f)
        {
            All.Add(f); byId[f.id] = f;
            f.t = f.target = f.paintedFrame == 0 ? 0f : 1f;
            return f;
        }

        private static void Build()
        {
            // ---- 门：frame 0 = 关（原画），最后一帧 = 完全打开 ----
            Add(new Fixture { id = "door_hall", label = "站务大厅 双扇门", kind = FixtureKind.Door, x = 372, y = 250, w = 74, h = 98, frames = 5, trigger = new Vector2Int(10, 8), radius = 2.2f });
            Add(new Fixture { id = "door_dispatch", label = "调度室 门", kind = FixtureKind.Door, x = 822, y = 262, w = 42, h = 78, frames = 4, trigger = new Vector2Int(20, 7), radius = 1.8f });
            Add(new Fixture { id = "door_waiting", label = "候车室 门", kind = FixtureKind.Door, x = 1160, y = 262, w = 38, h = 78, frames = 4, trigger = new Vector2Int(29, 7), radius = 1.8f });
            Add(new Fixture { id = "door_shop", label = "铁路商店 门", kind = FixtureKind.Door, x = 755, y = 715, w = 38, h = 82, frames = 4, trigger = new Vector2Int(19, 19), radius = 1.8f });
            // 库房卷帘：原画是开着的，最后一帧 = 原画；帧 0 = 完全关闭
            Add(new Fixture { id = "door_garage", label = "车库 卷帘门", kind = FixtureKind.Gate, x = 1040, y = 715, w = 132, h = 120, frames = 6, paintedFrame = 5, speed = 1.2f });
            Add(new Fixture { id = "door_shed", label = "东段货棚 推拉门", kind = FixtureKind.Gate, east = true, x = 688, y = 228, w = 108, h = 68, frames = 4, paintedFrame = 3, speed = 1.2f });

            // ---- 道口与信号 ----
            // 栏杆：frame 0 = 放下（原画），最后一帧 = 抬起
            // 矩形必须覆盖抬杆摆动的全部范围（含被杆遮住的背景，由美术补画）
            Add(new Fixture { id = "crossing_bar_n", label = "道口栏杆（北，支点在左）", kind = FixtureKind.Gate, east = true, x = 566, y = 478, w = 168, h = 126, frames = 5, speed = 0.6f });
            Add(new Fixture { id = "crossing_bar_s", label = "道口栏杆（南，支点在右）", kind = FixtureKind.Gate, east = true, x = 630, y = 722, w = 162, h = 122, frames = 5, speed = 0.6f });
            Add(new Fixture { id = "crossing_light", label = "道口红灯（交替闪）", kind = FixtureKind.Ambient, east = true, x = 778, y = 483, w = 42, h = 120, frames = 2, fps = 2f });
            // 臂板信号机：0 = 红（原画），1 = 过渡，2 = 绿
            Add(new Fixture { id = "signal_station", label = "站场臂板信号机", kind = FixtureKind.Signal, x = 1437, y = 100, w = 84, h = 252, frames = 3, speed = 1.5f });

            // ---- 灯：frame 0 = 灭，frame 1 = 亮（原画是亮的）----
            Add(new Fixture { id = "lamp_station", label = "站台路灯（只含灯头）", kind = FixtureKind.Light, x = 650, y = 85, w = 58, h = 64, frames = 2, paintedFrame = 1, speed = 4f });
            Add(new Fixture { id = "lamp_shed", label = "货棚檐灯", kind = FixtureKind.Light, east = true, x = 645, y = 212, w = 30, h = 35, frames = 2, paintedFrame = 1, speed = 4f });

            // ---- 环境循环（frame 0 = 原画）----
            // ---- 政治压力告示（0 常态=原画；1 警示通报 2 约谈整改 3 挂牌督办 4 停产停业整顿）----
            Add(new Fixture { id = "sign_hall", label = "站务大厅 告示/封条", kind = FixtureKind.Sign, x = 368, y = 220, w = 84, h = 30, frames = 5 });
            Add(new Fixture { id = "sign_dispatch", label = "调度室 告示/封条", kind = FixtureKind.Sign, x = 744, y = 282, w = 66, h = 50, frames = 5 });

            // ---- 沙能贴花（渗透度越高越多；单帧透明）----
            // foot = 占用的逻辑格（x,y,w,h，世界格），出现时变成不可通行
            Add(new Fixture { id = "decal_usert_stand", label = "沙能免费试乘摊", kind = FixtureKind.Decal, x = 1296, y = 548, w = 124, h = 108, frames = 1, minPen = 0.30f, foot = new RectInt(32, 14, 3, 2) });
            Add(new Fixture { id = "decal_usert_billboard", label = "沙能广告牌", kind = FixtureKind.Decal, x = 1180, y = 28, w = 110, h = 100, frames = 1, minPen = 0.45f, foot = new RectInt(29, 1, 3, 2) });
            Add(new Fixture { id = "decal_usert_supply", label = "沙能补给站小亭", kind = FixtureKind.Decal, east = true, x = 760, y = 860, w = 150, h = 100, frames = 1, minPen = 0.60f, foot = new RectInt(52, 13, 2, 2) });

            Add(new Fixture { id = "amb_chimney", label = "站房烟囱炊烟", kind = FixtureKind.Ambient, x = 262, y = 20, w = 70, h = 110, frames = 6, fps = 5f });
            Add(new Fixture { id = "amb_pond", label = "水塘波光与荷叶", kind = FixtureKind.Ambient, x = 1310, y = 730, w = 230, h = 180, frames = 4, fps = 2.5f });
            Add(new Fixture { id = "amb_stream", label = "东段溪流", kind = FixtureKind.Ambient, east = true, x = 850, y = 870, w = 174, h = 530, frames = 4, fps = 2.5f });
            Add(new Fixture { id = "amb_blossom_nw", label = "左上樱树摇曳", kind = FixtureKind.Ambient, x = 0, y = 0, w = 340, h = 220, frames = 4, fps = 1.6f });
            Add(new Fixture { id = "amb_blossom_ne", label = "右上樱树摇曳", kind = FixtureKind.Ambient, x = 1310, y = 0, w = 190, h = 200, frames = 4, fps = 1.6f });
        }

        public static Fixture Get(string id) => byId.TryGetValue(id, out var f) ? f : null;

        /// <summary>游戏逻辑设目标：0..1（门/栏杆/灯/信号机）。</summary>
        public static void Set(string id, float target) { var f = Get(id); if (f != null) f.target = Mathf.Clamp01(target); }

        /// <summary>每帧推进；玩家位置用于门的靠近触发（只在室外调用）。</summary>
        public static void Tick(float dt, Vector2 playerWorld, float tilePx)
        {
            Vector2 pt = playerWorld / tilePx;
            foreach (var f in All)
            {
                if (f.kind == FixtureKind.Door && f.trigger.x >= 0)
                {
                    float d = Vector2.Distance(pt, new Vector2(f.trigger.x + 0.5f, f.trigger.y + 0.5f));
                    f.target = d <= f.radius ? 1f : 0f;
                }
                if (f.kind == FixtureKind.Sign) f.t = f.target = Mathf.Clamp(GameData.PoliticalTier, 0, f.frames - 1) / (float)Mathf.Max(1, f.frames - 1);
                else if (f.kind != FixtureKind.Ambient && f.kind != FixtureKind.Decal)
                    f.t = Mathf.MoveTowards(f.t, f.target, f.speed * dt);
            }
        }

        /// <summary>该世界格是否被已显示的贴花占用（室外）。</summary>
        public static bool DecalSolid(int tx, int ty)
        {
            foreach (var f in All)
                if (f.kind == FixtureKind.Decal && f.foot.width > 0 && f.foot.Contains(new Vector2Int(tx, ty)) && NeedsPatch(f)) return true;
            return false;
        }

        /// <summary>评审/新档：恢复到原画状态。</summary>
        public static void ResetAll()
        {
            foreach (var f in All) f.t = f.target = f.paintedFrame == 0 ? 0f : 1f;
        }

        /// <summary>该补丁是否需要绘制（与原画一致的帧不画）。</summary>
        public static bool NeedsPatch(Fixture f)
        {
            if (f.kind == FixtureKind.Decal) return SandRivalManager.GetPenetration("wufeng") >= f.minPen;
            if (f.id == "crossing_light") { var bar = Get("crossing_bar_n"); if (bar != null && bar.t > 0.3f) return false; }   // 栏杆抬起时红灯不闪
            return f.kind == FixtureKind.Ambient ? f.Frame != 0 : f.Frame != f.paintedFrame;
        }

        /// <summary>原图像素矩形 → 世界像素矩形（站区 40×24 格，东段 16×24 格，每格 32）。</summary>
        public static Rect ToWorld(Fixture f, float tilePx, int stationTiles, int heightTiles)
        {
            float sw = f.east ? EastW : StationW, sh = f.east ? EastH : StationH;
            float tilesW = f.east ? 16f : stationTiles;
            float sx = tilesW * tilePx / sw, sy = heightTiles * tilePx / sh;
            float ox = f.east ? stationTiles * tilePx : 0f;
            return new Rect(ox + f.x * sx, f.y * sy, f.w * sx, f.h * sy);
        }
    }
}
