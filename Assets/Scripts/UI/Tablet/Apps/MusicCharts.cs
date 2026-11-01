using System;
using System.Collections.Generic;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // NetEase's public charts and hot searches, read anonymously (no login needed) and kept for a few minutes, so the home page,
    // the chart page and the search box share one request.
    public static class MusicCharts
    {
        const float Lifetime = 600f;
        static NeteaseLocalBridge.RemotePlaylist[] charts;
        static string[] hots;
        static float chartsTime = -1e9f, hotsTime = -1e9f;
        static bool chartsLoading, hotsLoading, chartsFailed, hotsFailed;
        public static event Action Changed;

        public static NeteaseLocalBridge.RemotePlaylist[] Charts => charts;
        public static string[] HotSearches => hots;
        public static bool ChartsLoading => chartsLoading;
        public static bool ChartsFailed => chartsFailed;

        // Ask for fresh data when the cache is old; callers redraw on Changed.
        public static void Ensure()
        {
            if (!chartsLoading && Time.realtimeSinceStartup - chartsTime > Lifetime && !(chartsFailed && Time.realtimeSinceStartup - chartsTime < 60f))
            {
                chartsLoading = true;
                NeteaseLocalBridge.Instance.Send("toplists", null, response => {
                    chartsLoading = false; chartsTime = Time.realtimeSinceStartup;
                    chartsFailed = !response.ok || response.result?.playlists == null;
                    if (!chartsFailed) charts = response.result.playlists;
                    Changed?.Invoke();
                });
            }
            if (!hotsLoading && Time.realtimeSinceStartup - hotsTime > Lifetime && !(hotsFailed && Time.realtimeSinceStartup - hotsTime < 60f))
            {
                hotsLoading = true;
                NeteaseLocalBridge.Instance.Send("hot_search", null, response => {
                    hotsLoading = false; hotsTime = Time.realtimeSinceStartup;
                    hotsFailed = !response.ok || response.result?.hots == null;
                    if (!hotsFailed) hots = response.result.hots;
                    Changed?.Invoke();
                });
            }
        }
    }
}
