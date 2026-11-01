using System;
using System.Collections.Generic;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // NetEase's public charts and hot searches, read anonymously (no login needed) and kept for a few minutes, so the home page,
    // the chart page and the search box share one request.
    public static class MusicCharts
    {
        const float Lifetime = 600f, RetryAfter = 20f;   // a good answer is kept ten minutes; a failed one is tried again after twenty seconds
        static NeteaseLocalBridge.RemotePlaylist[] charts;
        static string[] hots;
        static float nextChartsAt, nextHotsAt;   // the earliest time each list may be asked for again
        static bool chartsLoading, hotsLoading, chartsFailed, hotsFailed;
        public static event Action Changed;

        public static NeteaseLocalBridge.RemotePlaylist[] Charts => charts;
        public static string[] HotSearches => hots;
        public static bool ChartsLoading => chartsLoading;
        public static bool ChartsFailed => chartsFailed;

        /// <summary>The "retry" button: ask again now, whatever the wait after the last failure.</summary>
        public static void Retry() { nextChartsAt = 0f; nextHotsAt = 0f; Ensure(); }

        // Ask for fresh data when the cache is old; callers redraw on Changed.
        public static void Ensure()
        {
            if (!chartsLoading && Time.realtimeSinceStartup >= nextChartsAt)
            {
                chartsLoading = true;
                NeteaseLocalBridge.Instance.Send("toplists", null, response => {
                    chartsLoading = false;
                    bool wasFailed = chartsFailed;
                    chartsFailed = !response.ok || response.result?.playlists == null;
                    nextChartsAt = Time.realtimeSinceStartup + (chartsFailed ? RetryAfter : Lifetime);
                    if (!chartsFailed) charts = response.result.playlists;
                    // A page redraws when this fires, so a failed try after a failed try (nothing new to show) stays silent.
                    if (!chartsFailed || !wasFailed) Changed?.Invoke();
                });
            }
            if (!hotsLoading && Time.realtimeSinceStartup >= nextHotsAt)
            {
                hotsLoading = true;
                NeteaseLocalBridge.Instance.Send("hot_search", null, response => {
                    hotsLoading = false;
                    bool wasFailed = hotsFailed;
                    hotsFailed = !response.ok || response.result?.hots == null;
                    nextHotsAt = Time.realtimeSinceStartup + (hotsFailed ? RetryAfter : Lifetime);
                    if (!hotsFailed) hots = response.result.hots;
                    if (!hotsFailed || !wasFailed) Changed?.Invoke();
                });
            }
        }
    }
}
