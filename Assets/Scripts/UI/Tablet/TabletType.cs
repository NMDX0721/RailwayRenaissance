namespace RailwayRenaissance.UI.Tablet
{
    /// <summary>平板统一字号阶梯。状态栏、通知、超级岛、应用顶栏都从这里取，不再各写各的数字。</summary>
    public static class TabletType
    {
        /// <summary>状态栏：时间、日期、电量数字共用同一字号。</summary>
        public const int StatusText = 16;
        public const int Caption = 11;
        public const int Small = 12;
        public const int Body = 13;
        public const int Title = 15;
        public const int AppTitle = 20;
        public const int Large = 22;
        public const int Display = 44;
    }
}
