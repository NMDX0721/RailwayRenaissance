using System;

namespace RailwayRenaissance.UI.Tablet
{
    /// <summary>一天的气象读数。所有平板界面（锁屏、桌面小组件、天气 App）共用同一份，不再各写各的字面量。</summary>
    public sealed class TabletWeatherReading
    {
        public DateTime Date;
        public int Now, High, Low;
        public string Condition;
        public TabletIconKind Icon;
        public bool Wet;
        public int WindLevel;
        public float WindMs;
        public string WindDirection;
        public float PressureKPa;
        public int Humidity;
        public float VisibilityKm;
        public int Aqi;
        public string Advice;

        public string Comfort => Now < 0 ? "寒冷" : Now < 10 ? "偏凉" : Now < 25 ? "体感舒适" : Now < 31 ? "偏热" : "炎热";
        public string HumidityLabel => Humidity < 40 ? "偏干" : Humidity <= 75 ? "正常" : "偏湿";
        public string VisibilityLabel => VisibilityKm >= 8f ? "良好" : VisibilityKm >= 5f ? "一般" : "较差";
        public string AqiLabel => Aqi <= 50 ? "优" : Aqi <= 100 ? "良" : "轻度污染";
        /// <summary>轮轨粘着系数：雨雪天明显下降。</summary>
        public float Adhesion => Wet ? 0.24f : 0.38f;
    }

    /// <summary>
    /// 软编码的气象模型：由系统日期决定（同一天各界面读到的数值一致），随季节、日期伪随机变化，当前温度随一天内的钟点起伏。
    /// 游戏里没有真实气象数据源，这里只是数据驱动的仿真；季节均温是可调表，不写进界面代码。
    /// </summary>
    public static class TabletWeather
    {
        /// <summary>各月平均气温（°C），温带半岛气候。</summary>
        private static readonly int[] MonthlyMean = { -3, 0, 6, 12, 18, 22, 25, 25, 20, 14, 6, -1 };

        private struct Kind
        {
            public string Name; public TabletIconKind Icon; public int Delta; public int Humidity; public float Visibility; public bool Wet; public string Advice;
            public Kind(string n, TabletIconKind i, int d, int h, float v, bool w, string a) { Name = n; Icon = i; Delta = d; Humidity = h; Visibility = v; Wet = w; Advice = a; }
        }

        private static readonly Kind[] Kinds =
        {
            new Kind("晴", TabletIconKind.Sun, 2, 45, 12f, false, "适合巡检"),
            new Kind("多云", TabletIconKind.PartlyCloudy, 0, 58, 9.5f, false, "线路畅通"),
            new Kind("阴", TabletIconKind.Cloud, -1, 68, 7f, false, "线路畅通"),
            new Kind("薄雾", TabletIconKind.Cloud, -1, 82, 3.5f, false, "提早开启机车大灯"),
            new Kind("小雨", TabletIconKind.Rain, -3, 88, 5f, true, "轨面湿滑，注意制动"),
        };

        private static readonly string[] WindDirections = { "北", "东北", "东", "东南", "南", "西南", "西", "西北" };

        private static uint Hash(DateTime d)
        {
            uint h = (uint)(d.Year * 10000 + d.Month * 100 + d.Day);
            h ^= h << 13; h ^= h >> 17; h ^= h << 5;
            h *= 2654435761u;
            h ^= h >> 15;
            return h;
        }

        public static TabletWeatherReading For(DateTime when)
        {
            DateTime day = when.Date;
            uint h = Hash(day);
            int mean = MonthlyMean[day.Month - 1];

            int roll = (int)(h % 100);
            int idx = roll < 22 ? 0 : roll < 55 ? 1 : roll < 72 ? 2 : roll < 82 ? 3 : 4;
            Kind kind = Kinds[idx];

            int daily = (int)((h >> 12) % 7) - 3;
            int center = mean + daily + kind.Delta;
            int high = center + 5;
            int low = center - 5;

            string condition = kind.Name;
            if (kind.Wet && center <= 1) condition = "小雪";

            // 当前温度：下午三点最高，凌晨三点最低
            double hour = when.Hour + when.Minute / 60.0;
            double diurnal = 0.5 + 0.5 * Math.Cos((hour - 15.0) / 24.0 * Math.PI * 2.0);
            int now = (int)Math.Round(low + (high - low) * diurnal);

            int wind = 1 + (int)((h >> 8) % 4) + (kind.Wet ? 1 : 0);
            return new TabletWeatherReading
            {
                Date = day,
                Now = now, High = high, Low = low,
                Condition = condition, Icon = kind.Icon, Wet = kind.Wet,
                WindLevel = wind,
                WindMs = (float)Math.Round(wind * 1.6 + ((h >> 4) % 10) / 10.0, 1),
                WindDirection = WindDirections[(int)((h >> 16) % 8)],
                PressureKPa = (float)Math.Round(100.2 + ((h >> 20) % 30) / 10.0 - (kind.Wet ? 0.8 : 0.0), 1),
                Humidity = Math.Min(95, kind.Humidity + (int)((h >> 6) % 9) - 4),
                VisibilityKm = (float)Math.Round(kind.Visibility + ((h >> 3) % 5) / 10.0, 1),
                Aqi = Math.Max(8, 15 + (int)((h >> 9) % 40) - (kind.Wet ? 8 : 0)),
                Advice = kind.Advice,
            };
        }

        public static TabletWeatherReading Now => For(TabletDateTime.Now);
    }
}
