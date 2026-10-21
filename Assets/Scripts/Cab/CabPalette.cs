using System.Collections.Generic;
using UnityEngine;

namespace RailwayRenaissance.Cab
{
    /// <summary>
    /// 驾驶台 3D 场景调色板与程序化材质管理器
    /// 严格对齐美术文档调色板（深棕、暖棕、琥珀、米白、暗红、金、深蓝）
    /// 统一使用 URP Unlit / 纯色平涂材质，无高光，纯色离散色阶
    /// </summary>
    public static class CabPalette
    {
        // 核心项目调色板色值
        public static readonly Color ColorDarkBrown   = HexColor("#3A2010"); // 主色：深棕
        public static readonly Color ColorWarmBrown   = HexColor("#6B4423"); // 主色：暖棕
        public static readonly Color ColorAmber       = HexColor("#B8843A"); // 主色：琥珀
        public static readonly Color ColorOffWhite    = HexColor("#E8D8C0"); // 辅色：米白
        public static readonly Color ColorGreyBrown   = HexColor("#7A6B5A"); // 辅色：灰棕
        public static readonly Color ColorDarkRed     = HexColor("#8B3A2A"); // 辅色：暗红
        public static readonly Color ColorGold        = HexColor("#D4A843"); // 点缀：金
        public static readonly Color ColorDeepBlue    = HexColor("#1A2A3A"); // 点缀：深蓝

        // UI 与控制台常用别名
        public static readonly Color DarkBronze       = ColorDarkBrown;
        public static readonly Color WarmBronze       = ColorWarmBrown;
        public static readonly Color AmberGlow        = ColorAmber;
        public static readonly Color RicePaper        = ColorOffWhite;
        public static readonly Color CrimsonRed       = HexColor("#B22222"); // 暗红警戒
        public static readonly Color GoldTrim         = ColorGold;
        // 场景特定色值（铁道、植被、信号）
        public static readonly Color RailSteel        = HexColor("#8C9EAF"); // 钢轨银蓝
        public static readonly Color IronBlack        = HexColor("#1A1412"); // 铁黑底色
        public static readonly Color ColorSteelRail   = HexColor("#8C9EAF"); // 钢轨银蓝
        public static readonly Color ColorBallast     = HexColor("#47413B"); // 道砟中灰
        public static readonly Color ColorSleeper     = HexColor("#281C13"); // 枕木深褐
        public static readonly Color ColorForestGreen = HexColor("#294720"); // 树木松绿
        public static readonly Color ColorFoliageLight= HexColor("#436628"); // 灌木浅绿
        public static readonly Color ColorMountainFar = HexColor("#324554"); // 远山灰蓝
        public static readonly Color ColorMountainMid = HexColor("#3A4F36"); // 中山苔绿
        public static readonly Color ColorSignalRed   = HexColor("#D42626"); // 信号机红灯
        public static readonly Color ColorSignalGreen = HexColor("#24BD52"); // 信号机绿灯
        public static readonly Color ColorPoleWood    = HexColor("#544030"); // 电线杆深木色
        public static readonly Color ColorStationWall = HexColor("#D9CEB8"); // 站房米灰墙面
        public static readonly Color ColorStationRoof = HexColor("#8B3322"); // 站房红陶瓦屋顶

        // 驾驶台重工业专用色彩
        public static readonly Color CabGreen         = HexColor("#233D2D"); // 经典内燃机车工业墨绿
        public static readonly Color CabConsoleDark   = HexColor("#161413"); // 铸铁防锈操纵台深色基座
        public static readonly Color CabPanelTrim     = HexColor("#38322B"); // 操纵台深褐耐磨包边
        public static readonly Color GaugeFaceDark    = HexColor("#0F1012"); // 亚光黑仪表盘底色
        public static readonly Color GaugeBezelMetal  = HexColor("#4E4942"); // 仪表金属外圈压圈
        public static readonly Color NeedleRed        = HexColor("#E53935"); // 仪表鲜红指示针
        public static readonly Color NeedleWhite      = HexColor("#F5F3E8"); // 仪表米白/夜光指示针
        public static readonly Color NeedleAmber      = HexColor("#FFB300"); // 速度表金黄细针
        public static readonly Color IvoryKeyCap      = HexColor("#F5EFE0"); // 象牙白琴键开关
        public static readonly Color IvoryKeyDark     = HexColor("#A89E8C"); // 琴键开关未激活阴影
        public static readonly Color BrassPlaqueGold  = HexColor("#C6933A"); // 纯铜/黄铜铸造铭牌
        public static readonly Color SignalLensGreen  = HexColor("#00E676"); // 信号机绿透镜亮灯
        public static readonly Color SignalLensYellow = HexColor("#FFD600"); // 信号机黄透镜亮灯
        public static readonly Color SignalLensRed    = HexColor("#FF1744"); // 信号机红透镜亮灯
        public static readonly Color SignalLensDark   = HexColor("#2A2C30"); // 信号灯未点亮暗透镜
        public static readonly Color BoltSilver       = HexColor("#8E8880"); // 六角固定螺栓银灰

        private static Shader s_CachedShader;
        private static readonly Dictionary<Color, Material> s_MaterialCache = new Dictionary<Color, Material>();

        private static Shader GetUnlitShader()
        {
            if (s_CachedShader != null) return s_CachedShader;

            s_CachedShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (s_CachedShader == null) s_CachedShader = Shader.Find("Unlit/Color");
            if (s_CachedShader == null) s_CachedShader = Shader.Find("Sprites/Default");
            if (s_CachedShader == null) s_CachedShader = Shader.Find("Hidden/Internal-Colored");
            return s_CachedShader;
        }

        public static Material GetMaterial(Color color)
        {
            if (s_MaterialCache.TryGetValue(color, out Material mat) && mat != null)
            {
                return mat;
            }

            Shader shader = GetUnlitShader();
            mat = new Material(shader)
            {
                color = color,
                name = $"CabMat_{ColorUtility.ToHtmlStringRGBA(color)}"
            };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            s_MaterialCache[color] = mat;
            return mat;
        }

        public static Color HexColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color c)) return c;
            return Color.magenta;
        }

        public static void Clear()
        {
            foreach (var kv in s_MaterialCache)
            {
                if (kv.Value != null) Object.Destroy(kv.Value);
            }
            s_MaterialCache.Clear();
        }
    }
}
