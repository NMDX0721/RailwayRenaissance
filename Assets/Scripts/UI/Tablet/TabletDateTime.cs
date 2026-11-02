using System;

namespace RailwayRenaissance.UI.Tablet
{
    /// <summary>
    /// 平板上显示的“剧情日期”：钟点取系统时间，月日取系统日期，年份固定为剧情年份 2076。
    /// 星期由 2076 年的日历推算，所以星期与公历年份一致。主体历 = 公历 − 1971（2076 年即主体历 105 年，与剧情设定一致）。
    /// 需要给玩家看的日期都走这里，不要直接用 DateTime.Now。
    /// </summary>
    public static class TabletDateTime
    {
        public const int StoryYear = 2076;
        public const int EraOffset = 1971;

        public static DateTime Now => ToStory(DateTime.Now);

        public static DateTime ToStory(DateTime real)
        {
            int day = Math.Min(real.Day, DateTime.DaysInMonth(StoryYear, real.Month));
            return new DateTime(StoryYear, real.Month, day, real.Hour, real.Minute, real.Second);
        }

        public static int EraYear => StoryYear - EraOffset;

        private static readonly string[] Weekdays = { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
        public static string WeekdayShort(DateTime t) => Weekdays[(int)t.DayOfWeek];
        public static string WeekdayLong(DateTime t) => "星期" + "日一二三四五六"[(int)t.DayOfWeek];
    }
}
