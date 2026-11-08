using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.Account;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>Where the in-game Xiaomi account sign-in plugs into the music app: set OneTapLogin to start the real flow.</summary>
    public static class MusicAccountHooks
    {
        public static Action OneTapLogin;
    }

    public sealed partial class RailwayMusicTerminalView
    {
        bool accountBound;

        void BindAccount()
        {
            if (accountBound) return; accountBound = true;
            Action changed = () => { if (root?.panel != null && (page == "我的" || page == "首页")) RenderContent(); };
            AccountService.Changed += changed;
            root.RegisterCallback<DetachFromPanelEvent>(_ => { AccountService.Changed -= changed; accountBound = false; });
        }

        // The signed-in name, or the station name while nobody is signed in.
        static string AccountName() => AccountService.IsRegistered ? AccountService.DisplayName : "雾峰站";

        void RenderMine()
        {
            var scroll = new ScrollView { name = "music-track-list" }; scroll.AddToClassList("music-track-list"); content.Add(scroll);
            var body = scroll.contentContainer; body.AddToClassList("music-home-content");
            Text(body, "沿线声音档案  /  MINE", "music-eyebrow");
            Text(body, "我的", "music-section-title");
            var card = Box(body, "music-mine-card");
            var profile = AccountService.Profile;
            var avatar = Box(card, "music-mine-avatar");
            TabletAvatar.Decorate(avatar, profile, 64f);
            var who = Box(card, "music-mine-who");
            Text(who, AccountName(), "music-mine-name").name = "music-mine-name";
            if (AccountService.IsRegistered)
            {
                Text(who, "小米账号 · " + AccountService.MaskedPhone(profile), "music-mine-sub");
                if (TabletXiaomiSignIn.IsSignedIn("music"))
                    Button(card, "退出登录", () => { TabletXiaomiSignIn.SignOut("music"); RenderContent(); }, "music-mine-logout");
                else
                    Button(card, "小米账号登录", () => TabletXiaomiSignIn.Request(card, "music", "音乐", new Color(0.19f, 0.72f, 0.42f, 1f), ok => { if (ok) RenderContent(); }), "music-mine-login").AddToClassList("music-primary");
            }
            else
            {
                Text(who, "还没有登录小米账号。登录后，音乐记录和生涯会跟着账号走。", "music-mine-sub");
                var login = Button(card, "小米账号一键登录", () =>
                {
                    if (MusicAccountHooks.OneTapLogin != null) MusicAccountHooks.OneTapLogin();
                    else TabletXiaomiSignIn.Request(card, "music", "音乐", new Color(0.19f, 0.72f, 0.42f, 1f), ok => { if (ok) RenderContent(); });
                }, "music-mine-login"); login.AddToClassList("music-primary");
            }
            var stats = MusicLibrarySession.PlayStats();
            var cards = Box(body, "music-career-cards");
            void Stat(string value, string label, string name)
            {
                var c = Box(cards, "music-career-card"); c.name = name;
                Text(c, value, "music-career-value"); Text(c, label, "music-career-label");
            }
            Stat(MusicLibrarySession.ListenText(), "累计时长", "music-mine-time");
            Stat(stats.Sum(x => x.count).ToString(), "累计播放（次）", "music-mine-plays");
            Stat(stats.Count.ToString(), "听过的曲目（首）", "music-mine-songs");
            Stat(MusicLibrarySession.State.favorites.Count.ToString(), "收藏（首）", "music-mine-favorites");
            Stat(MusicLibrarySession.State.playlists.Count.ToString(), "私人歌单（个）", "music-mine-lists");
            // Private playlists live here, not in the sidebar.
            var listBar = Box(body, "music-section-bar"); Text(listBar, "我的歌单", "music-section-title music-section-small");
            Button(Box(listBar, "music-section-actions"), "新建歌单", () => EditName(null), "music-new-playlist");
            var lists = MusicLibrarySession.State.playlists;
            if (lists.Count == 0) Text(body, "还没有私人歌单。点右上角“新建歌单”，再从曲目的“更多”菜单把歌放进去。", "music-muted");
            else
            {
                var grid = Box(body, "music-card-grid");
                foreach (var list in lists)
                {
                    var captured = list;
                    var artTrack = list.tracks.Select(MusicLibrarySession.Find).FirstOrDefault(t => t != null);
                    CollectionCard(grid, list.title, list.tracks.Count + " 首 · 本机", () => Navigate("歌单", captured), artTrack, "local-list:" + list.id, null);
                }
            }
            var actions = Box(body, "music-list-actions");
            Button(actions, "查看生涯", () => Navigate("生涯"), "music-mine-career");
        }
    }
}
