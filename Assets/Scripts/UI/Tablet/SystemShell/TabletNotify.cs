using System;
using System.Collections.Generic;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    public enum TabletNotifyImportance
    {
        /// <summary>只进通知中心，不弹横幅。</summary>
        Silent = 0,
        /// <summary>弹横幅（勿扰时只进通知中心）。</summary>
        Normal = 1,
        /// <summary>弹横幅，停留更久。</summary>
        High = 2
    }

    /// <summary>通知渠道：应用在这里登记自己能发哪些通知，用户可按渠道开关。</summary>
    public sealed class TabletNotifyChannel
    {
        public string Id;
        /// <summary>对应 LaunchApp 的应用名；为空表示系统通知。</summary>
        public string AppId;
        public string AppName = "系统";
        public string Name = "通知";
        public TabletIconKind Icon = TabletIconKind.Bell;
        /// <summary>角标底色；不填则用当前强调色。</summary>
        public Color? Accent;
        public TabletNotifyImportance Importance = TabletNotifyImportance.Normal;

        public Color AccentColor => Accent ?? TabletTheme.Accent;
        public bool Enabled
        {
            get => TabletNotify.IsChannelEnabled(Id);
            set => TabletNotify.SetChannelEnabled(Id, value);
        }
    }

    public sealed class TabletNotifyAction
    {
        public string Label;
        public Action OnClick;
        public bool Primary;
    }

    /// <summary>一条待发送的通知。链式写法：new TabletNotifyRequest("sms","验证码","123456").WithAction("复制", ...)。</summary>
    public sealed class TabletNotifyRequest
    {
        public string ChannelId = TabletNotify.SystemChannel;
        public string Title;
        public string Body;
        /// <summary>相同 Key 的通知互相替换（进度、计时类通知用）。</summary>
        public string Key;
        /// <summary>0..1 显示进度条；负数不显示。</summary>
        public float Progress = -1f;
        public Action OnClick;
        public TabletNotifyImportance? Importance;
        public readonly List<TabletNotifyAction> Actions = new List<TabletNotifyAction>();

        public TabletNotifyRequest(string channelId, string title, string body = null)
        {
            ChannelId = string.IsNullOrEmpty(channelId) ? TabletNotify.SystemChannel : channelId;
            Title = title; Body = body;
        }

        public TabletNotifyRequest WithKey(string key) { Key = key; return this; }
        public TabletNotifyRequest WithProgress(float p) { Progress = p; return this; }
        public TabletNotifyRequest OnTap(Action a) { OnClick = a; return this; }
        public TabletNotifyRequest Silent() { Importance = TabletNotifyImportance.Silent; return this; }
        public TabletNotifyRequest WithAction(string label, Action cb, bool primary = false)
        {
            if (Actions.Count < 2) Actions.Add(new TabletNotifyAction { Label = label, OnClick = cb, Primary = primary });
            return this;
        }
    }

    /// <summary>
    /// 平板通知的统一入口。应用只需：RegisterChannel（一次）+ Post（随时）。
    /// 横幅、通知中心、勿扰、渠道开关都由系统统一处理。
    /// </summary>
    public static class TabletNotify
    {
        public const string SystemChannel = "system";
        public const string AccountChannel = "account";
        public const string SmsChannel = "sms";
        public const string StoryChannel = "story";

        private static readonly Dictionary<string, TabletNotifyChannel> s_channels = new Dictionary<string, TabletNotifyChannel>();
        private static readonly List<TabletNotifyChannel> s_order = new List<TabletNotifyChannel>();
        private static TabletShellState s_state;
        private static Action<string> s_launch;
        private static Action<string> s_toast;

        public static event Action ChannelsChanged;

        static TabletNotify()
        {
            RegisterChannel(new TabletNotifyChannel { Id = SystemChannel, AppName = "系统", Name = "系统提醒", Icon = TabletIconKind.Bell });
            RegisterChannel(new TabletNotifyChannel { Id = AccountChannel, AppId = "设置", AppName = "设置", Name = "账号与安全", Icon = TabletIconKind.Person });
            RegisterChannel(new TabletNotifyChannel { Id = SmsChannel, AppName = "信息", Name = "短信", Icon = TabletIconKind.Chat, Accent = new Color(0.20f, 0.72f, 0.40f, 1f), Importance = TabletNotifyImportance.High });
            RegisterChannel(new TabletNotifyChannel { Id = StoryChannel, AppId = "RDA", AppName = "RDA", Name = "剧情与调度", Icon = TabletIconKind.Train });
        }

        public static void Attach(TabletShellState state, Action<string> launchApp, Action<string> toast)
        {
            s_state = state; s_launch = launchApp; s_toast = toast;
        }

        public static TabletShellState State => s_state;
        public static IReadOnlyList<TabletNotifyChannel> Channels => s_order;

        public static void RegisterChannel(TabletNotifyChannel channel)
        {
            if (channel == null || string.IsNullOrEmpty(channel.Id)) return;
            if (s_channels.TryGetValue(channel.Id, out var old)) s_order[s_order.IndexOf(old)] = channel;
            else s_order.Add(channel);
            s_channels[channel.Id] = channel;
            ChannelsChanged?.Invoke();
        }

        public static TabletNotifyChannel GetChannel(string id)
        {
            if (!string.IsNullOrEmpty(id) && s_channels.TryGetValue(id, out var c)) return c;
            return s_channels[SystemChannel];
        }

        private static string PrefKey(string id) => "tablet.notify.off." + id;

        public static bool IsChannelEnabled(string id)
        {
            try { return PlayerPrefs.GetInt(PrefKey(id), 0) == 0; } catch { return true; }
        }

        public static void SetChannelEnabled(string id, bool enabled)
        {
            try { PlayerPrefs.SetInt(PrefKey(id), enabled ? 0 : 1); } catch { }
            ChannelsChanged?.Invoke();
        }

        public static long Post(TabletNotifyRequest request)
        {
            if (request == null || s_state == null) return -1;
            var channel = GetChannel(request.ChannelId);
            if (!channel.Enabled) return -1;
            return s_state.Post(request, channel);
        }

        public static long Post(string channelId, string title, string body = null, Action onClick = null)
            => Post(new TabletNotifyRequest(channelId, title, body) { OnClick = onClick });

        /// <summary>系统/账号类提示的统一出口：“短信”走短信渠道并附带“复制验证码”，其余走账号渠道。</summary>
        public static long PostNotice(string title, string body)
        {
            if (title == "短信")
            {
                var req = new TabletNotifyRequest(SmsChannel, title, body);
                string code = ExtractCode(body);
                if (code != null) req.WithAction("复制验证码", () => { GUIUtility.systemCopyBuffer = code; Toast("验证码已复制"); }, true);
                return Post(req);
            }
            return Post(AccountChannel, title, body);
        }

        private static string ExtractCode(string body)
        {
            if (string.IsNullOrEmpty(body)) return null;
            int i = 0;
            while (i < body.Length)
            {
                if (!char.IsDigit(body[i])) { i++; continue; }
                int j = i;
                while (j < body.Length && char.IsDigit(body[j])) j++;
                if (j - i >= 4 && j - i <= 8) return body.Substring(i, j - i);
                i = j;
            }
            return null;
        }

        public static bool Cancel(string key) => s_state != null && s_state.DismissByKey(key);

        /// <summary>轻提示：不入通知中心的短暂小条（“已复制”之类的即时反馈）。</summary>
        public static void Toast(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            s_toast?.Invoke(message);
        }

        /// <summary>点击通知后的统一去向：打开渠道对应的应用。</summary>
        public static void Open(TabletNotification n)
        {
            if (n == null) return;
            if (n.Request?.OnClick != null) { n.Request.OnClick(); return; }
            var app = GetChannel(n.ChannelId).AppId;
            if (!string.IsNullOrEmpty(app)) s_launch?.Invoke(app);
        }

        public static string RelativeTime(DateTime created)
        {
            var span = DateTime.Now - created;
            if (span.TotalSeconds < 60) return "现在";
            if (span.TotalMinutes < 60) return (int)span.TotalMinutes + "分钟前";
            if (created.Date == DateTime.Now.Date) return created.ToString("HH:mm");
            return created.ToString("M月d日");
        }
    }
}
