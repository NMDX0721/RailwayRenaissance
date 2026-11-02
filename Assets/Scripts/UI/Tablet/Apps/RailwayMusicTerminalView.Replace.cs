using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // When an online track cannot be played (taken down, no rights, the source is gone), look for the same song on the other sources and swap it
    // in everywhere it was kept: favourites, playlists, the queue and the recent list. Matches are ranked by title, then by how close the length is.
    public sealed partial class RailwayMusicTerminalView
    {
        sealed class Candidate
        {
            public string source; public NeteaseLocalBridge.RemoteTrack remote; public int score; public float gap;
        }

        int replaceGeneration;

        /// <summary>Opens the replacement sheet for a track by id (used by the review).</summary>
        public static void OpenReplacementFor(string id) { if (current != null) current.OpenReplacement(MusicLibrarySession.Find(id)); }

        static string Plain(string text)
        {
            var kept = new System.Text.StringBuilder();
            foreach (char ch in (text ?? "").ToLowerInvariant()) if (char.IsLetterOrDigit(ch)) kept.Append(ch);
            return kept.ToString();
        }

        static int TitleScore(string wanted, string found)
        {
            string a = Plain(wanted), b = Plain(found);
            if (a.Length == 0 || b.Length == 0) return 0;
            if (a == b) return 3;
            if (b.StartsWith(a) || a.StartsWith(b)) return 2;
            return b.Contains(a) || a.Contains(b) ? 1 : 0;
        }

        void OpenReplacement(MusicLibraryTrack track)
        {
            if (track == null) return;
            var panel = Modal("替换来源"); panel.AddToClassList("music-replace-sheet");
            int generation = ++replaceGeneration;
            Text(panel, "为「" + track.title + "」寻找其他来源", "music-detail-title");
            var status = Text(panel, "正在其他来源里查找同名曲…", "music-muted");
            var scroll = new ScrollView { verticalScrollerVisibility = ScrollerVisibility.Auto }; scroll.style.flexShrink = 1; scroll.style.minHeight = 120; panel.Add(scroll);
            string term = (track.title + " " + (track.artist ?? "")).Trim();
            var sources = new List<string> { "netease" };
            foreach (string source in MusicCatalogSearch.Sources) if (source != "netease" && NeteaseLocalBridge.HasSession(source)) sources.Add(source);
            var found = new List<Candidate>(); int waiting = sources.Count;
            foreach (string source in sources)
            {
                string captured = source;
                MusicCatalogSearch.Search(captured, term, response => {
                    if (generation != replaceGeneration || scroll.panel == null) return;
                    waiting--;
                    if (response.ok && response.result?.tracks != null)
                        foreach (var remote in response.result.tracks.Take(8))
                        {
                            if (MusicSourceId.Online(captured, remote.id) == track.id) continue;
                            // A candidate must match the title at all; the artist only ranks the ones that do (another song by the same artist is not a replacement).
                            int titleScore = TitleScore(track.title, remote.title);
                            if (titleScore == 0) continue;
                            int score = titleScore * 10 + (!string.IsNullOrWhiteSpace(track.artist) && Plain(remote.artist).Contains(Plain(track.artist.Split('/')[0])) ? 12 : 0);
                            float gap = track.duration > 0 && remote.durationMs > 0 ? Mathf.Abs(track.duration - remote.durationMs / 1000f) : -1f;
                            if (score < 10) continue;
                            found.Add(new Candidate { source = captured, remote = remote, score = score, gap = gap });
                        }
                    ShowCandidates(track, scroll, status, found, waiting);
                });
            }
        }

        void ShowCandidates(MusicLibraryTrack track, ScrollView scroll, Label status, List<Candidate> found, int waiting)
        {
            scroll.Clear();
            found.Sort((x, y) => y.score != x.score ? y.score.CompareTo(x.score) : (x.gap < 0 ? 999f : x.gap).CompareTo(y.gap < 0 ? 999f : y.gap));
            if (found.Count == 0) { status.text = waiting > 0 ? "正在其他来源里查找同名曲…" : "没有找到同名曲。可以换个来源登录后再试。"; return; }
            status.text = (waiting > 0 ? "已找到 " + found.Count + " 个，仍在查找…" : "找到 " + found.Count + " 个候选") + " · 选一个替换，收藏、歌单和队列里的这首都会换成它";
            foreach (var candidate in found.Take(12))
            {
                var item = candidate;
                var row = Box(scroll.contentContainer, "music-replace-row");
                var text = Box(row, "music-replace-text");
                Text(text, item.remote.title, "music-replace-title");
                string length = item.gap < 0 ? "" : item.gap <= 3f ? " · 时长相近" : " · 时长差 " + Mathf.RoundToInt(item.gap) + " 秒";
                Text(text, (string.IsNullOrEmpty(item.remote.artist) ? "" : item.remote.artist + " · ") + MusicCatalogSearch.Label(item.source) + length, "music-replace-sub");
                var pick = Button(row, "替换并播放", () => Replace(track, item), "music-replace-" + item.source + "-" + item.remote.id);
                pick.AddToClassList("music-primary");
            }
        }

        void Replace(MusicLibraryTrack track, Candidate candidate)
        {
            var added = MusicLibrarySession.RegisterRemote(candidate.remote, candidate.source);
            if (added == null) { ShowToast("这首曲目无法加入"); return; }
            int places = MusicLibrarySession.ReplaceTrack(track.id, added.id);
            CloseModal();
            ShowToast("已替换为 " + MusicCatalogSearch.Label(candidate.source) + "，共 " + places + " 处");
            MusicLibrarySession.Play(added.id);
        }
    }
}
