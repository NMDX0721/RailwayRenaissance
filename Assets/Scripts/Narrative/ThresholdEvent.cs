namespace Narrative
{
    /// <summary>阈值穿越方向（N1.6）。</summary>
    public enum TrendDirection { Rising, Falling, Stable }

    /// <summary>阈值事件（N1.6）——趋势线越过阈值时产生，按方向与严重度分级。</summary>
    [System.Serializable]
    public class ThresholdEvent
    {
        public string trendName; // trust/fiscal/sand/political/decay
        public float thresholdValue;
        public TrendDirection direction;
        public string severity; // warning/severe/irreversible
    }
}