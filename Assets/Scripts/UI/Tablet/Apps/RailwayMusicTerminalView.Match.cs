using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // A local file has only its file name. This sheet looks the song up on NetEase (no login needed), and on a pick takes that song's
    // lyrics (with word timing where there is any, and the translation) and its cover for the local track. The pick is remembered with the
    // track and can be cleared again. The audio file itself is never touched.
    public sealed partial class RailwayMusicTerminalView
    {
        int matchGeneration;

        /// <summary>Opens the matching sheet for a local track by id (used by the review).</summary>
        public static void OpenMatchFor(string id, string term = null) { if (current != null) { current.matchTermOverride = term; current.OpenMatch(MusicLibrarySession.Find(id)); } }
        string matchTermOverride;

        static string GuessSearchTerm(MusicLibraryTrack track)
        {
            string stem = Path.GetFileNameWithoutExtension(track.fileName ?? track.title ?? "");
            string term = !string.IsNullOrWhiteSpace(track.artist) ? track.title + " " + track.artist : (string.IsNullOrWhiteSpace(stem) ? track.title : stem);
            return (term ?? "").Replace('_', ' ').Replace('-', ' ').Trim();
        }

        void OpenMatch(MusicLibraryTrack track)
        {
            if (track == null) return;
            var panel = Modal("匹配在线歌词和封面"); panel.AddToClassList("music-replace-sheet");
            Text(panel, track.title, "music-detail-title");
            var pref = MusicTimedText.Preference(track.id);
            var bar = Box(panel, "music-cloud-search-bar");
            var field = new TextField { value = matchTermOverride ?? GuessSearchTerm(track), name = "music-match-term" }; bar.Add(field); matchTermOverride = null;
            var status = Text(panel, "输入曲名和歌手，搜索网易云。", "music-muted");
            var scroll = new ScrollView { verticalScrollerVisibility = ScrollerVisibility.Auto }; scroll.style.flexShrink = 1; scroll.style.minHeight = 120; panel.Add(scroll);
            var actions = Box(panel, "music-list-actions");
            if (!string.IsNullOrEmpty(pref.matchRemoteId))
            {
                status.text = "当前已匹配：" + pref.matchTitle + "（网易云）";
                Button(actions, "清除匹配", () => { ClearMatch(track); CloseModal(); }, "music-match-clear");
            }
            void Run()
            {
                string term = (field.value ?? "").Trim(); if (term.Length == 0) return;
                int generation = ++matchGeneration; status.text = "正在搜索…"; scroll.Clear();
                MusicCatalogSearch.Search("netease", term, response => {
                    if (generation != matchGeneration || scroll.panel == null) return;
                    var tracks = response.ok ? response.result?.tracks : null;
                    if (tracks == null || tracks.Length == 0) { status.text = response.ok ? "没有找到，换个写法再试。" : "搜索失败，请检查网络。"; return; }
                    status.text = "选一首，用它的歌词和封面";
                    foreach (var remote in tracks.Take(12))
                    {
                        var item = remote;
                        var row = Box(scroll.contentContainer, "music-replace-row");
                        var text = Box(row, "music-replace-text");
                        Text(text, item.title, "music-replace-title");
                        string length = track.duration > 0 && item.durationMs > 0 ? (Mathf.Abs(track.duration - item.durationMs / 1000f) <= 3f ? " · 时长相近" : " · 时长差 " + Mathf.RoundToInt(Mathf.Abs(track.duration - item.durationMs / 1000f)) + " 秒") : "";
                        Text(text, string.Join(" · ", new[] { item.artist, item.album }.Where(part => !string.IsNullOrWhiteSpace(part))) + length, "music-replace-sub");
                        Button(row, "使用", () => ApplyMatch(track, item, status), "music-match-use-" + item.id).AddToClassList("music-primary");
                    }
                });
            }
            var go = Button(bar, "搜索", Run, "music-match-search"); go.style.marginLeft = 8;
            field.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { Run(); e.StopPropagation(); } });
            Run();
        }

        void ApplyMatch(MusicLibraryTrack track, NeteaseLocalBridge.RemoteTrack remote, Label status)
        {
            status.text = "正在读取歌词…";
            NeteaseLocalBridge.Instance.Send("lyric", new NeteaseLocalBridge.Parameters { id = remote.id }, result => {
                if (!result.ok || result.result == null) { status.text = "歌词读取失败，可稍后重试。"; return; }
                string message = "";
                string lyric = !string.IsNullOrWhiteSpace(result.result.wordLyric) ? result.result.wordLyric : result.result.lyric;
                bool imported = false;
                if (!string.IsNullOrWhiteSpace(lyric))
                {
                    imported = ImportMatched(track.id, lyric, false, out message);
                    if (imported && !string.IsNullOrWhiteSpace(result.result.translation)) ImportMatched(track.id, result.result.translation, true, out _);
                }
                var pref = MusicTimedText.Preference(track.id);
                pref.matchSource = "netease"; pref.matchRemoteId = remote.id; pref.matchTitle = remote.title; pref.matchCover = remote.cover ?? "";
                MusicAlbumArtResolver.InvalidateRemote(track.id);
                MusicLibrarySession.Save();
                CloseModal();
                ShowToast((imported ? "已匹配歌词" : "这首没有可用歌词") + (string.IsNullOrEmpty(remote.cover) ? "" : "和封面") + "：" + remote.title);
                Refresh();
            });
        }

        // The lyric text goes through the same import as a file the player picks, so it is validated and stored the same way.
        static bool ImportMatched(string id, string text, bool translation, out string message)
        {
            string temp = null;
            try
            {
                Directory.CreateDirectory(MusicLibrarySession.LyricsDirectory);
                temp = Path.Combine(MusicLibrarySession.LyricsDirectory, "match_" + Guid.NewGuid().ToString("N") + ".lrc");
                File.WriteAllText(temp, text, new System.Text.UTF8Encoding(false));
                return MusicTimedText.Import(id, temp, translation, out message);
            }
            catch (Exception) { message = "歌词未能保存"; return false; }
            finally { try { if (temp != null && File.Exists(temp)) File.Delete(temp); } catch (Exception) { } }
        }

        internal static bool ImportMatchedForReview(string id, string text, out string message) => ImportMatched(id, text, false, out message);

        void ClearMatch(MusicLibraryTrack track)
        {
            var pref = MusicTimedText.Preference(track.id);
            pref.matchSource = null; pref.matchRemoteId = null; pref.matchTitle = null; pref.matchCover = null;
            MusicAlbumArtResolver.InvalidateRemote(track.id);
            MusicLibrarySession.Save(); ShowToast("已清除匹配（已导入的歌词保留，可在歌词页重新导入）"); Refresh();
        }
    }

    /// <summary>Lets the review reach the lyric import the matching sheet uses.</summary>
    public static class RailwayMusicTerminalViewReviewAccess
    {
        public static bool ImportMatched(string id, string text, out string message) => RailwayMusicTerminalView.ImportMatchedForReview(id, text, out message);
    }
}
