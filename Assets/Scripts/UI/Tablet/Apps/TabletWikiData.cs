using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>媒姬百科的一个词条小节。</summary>
    public sealed class WikiSection
    {
        public string Heading;
        public string Text;
    }

    /// <summary>媒姬百科词条。正文里用 [[id|显示文字]] 写站内链接，'''文字''' 写粗体。</summary>
    public sealed class WikiArticle
    {
        public string Id;
        public string Title;
        public string Category;
        public string Group;             // 分类下的子组（铁路术语的工务/机务…）
        public string Subtitle;
        public string Lead;
        public string Portrait;          // Resources 路径，可空
        public string UnlockKey;         // PlayerPrefs 键；空 = 始终收录
        public string UnlockHint;
        public string[] Aliases = new string[0];
        public string[] See = new string[0];
        public string SortKey = "~";
        public bool IsTerm;
        public string ChapterScript;     // 剧情章节才有
        public readonly List<KeyValuePair<string, string>> Infobox = new List<KeyValuePair<string, string>>();
        public List<WikiSection> Sections = new List<WikiSection>();
        public Func<List<WikiSection>> DynamicSections;   // 资料库合集：每次打开时按解锁进度生成

        public bool Unlocked => string.IsNullOrEmpty(UnlockKey) || PlayerPrefs.GetInt(UnlockKey, 0) == 1;
        public List<WikiSection> GetSections() => DynamicSections != null ? DynamicSections() : Sections;
    }

    /// <summary>词条数据层：合并手写词条（Resources/Wiki）、术语表（Terms.json）、VN 图鉴（角色/列车/章节/场景/CG/音乐）。</summary>
    public static class TabletWikiData
    {
        public const string CatPeople = "人物";
        public const string CatVehicle = "机车与车辆";
        public const string CatPlace = "线路与车站";
        public const string CatWorld = "历史与世界观";
        public const string CatTerm = "铁路术语";
        public const string CatStory = "剧情章节";
        public const string CatArchive = "资料库";

        public static readonly string[] Categories = { CatPeople, CatVehicle, CatPlace, CatWorld, CatTerm, CatStory, CatArchive };

        [Serializable] private class JKv { public string k; public string v; }
        [Serializable] private class JSec { public string h; public string t; }
        [Serializable]
        private class JArticle
        {
            public string id, title, category, subtitle, unlock, hint, portrait, lead;
            public string[] aliases, see;
            public JKv[] infobox;
            public JSec[] sections;
        }
        [Serializable] private class JRoot { public string[] dyk; public JArticle[] articles; }

        private static List<WikiArticle> s_all;
        private static Dictionary<string, WikiArticle> s_byId;
        private static Dictionary<string, string> s_byTitle;     // 标题/别名 → id
        private static string[] s_dyk = new string[0];

        public static IReadOnlyList<WikiArticle> All { get { Ensure(); return s_all; } }
        public static string[] DidYouKnow { get { Ensure(); return s_dyk; } }

        public static WikiArticle Get(string idOrTitle)
        {
            Ensure();
            if (string.IsNullOrEmpty(idOrTitle)) return null;
            if (s_byId.TryGetValue(idOrTitle, out var a)) return a;
            return s_byTitle.TryGetValue(idOrTitle, out var id) ? s_byId[id] : null;
        }

        public static List<WikiArticle> InCategory(string category)
        {
            Ensure();
            var list = s_all.FindAll(a => a.Category == category);
            list.Sort((x, y) =>
            {
                int c = string.CompareOrdinal(x.Group ?? "", y.Group ?? "");
                if (c != 0) return c;
                c = string.CompareOrdinal(x.SortKey, y.SortKey);
                return c != 0 ? c : string.CompareOrdinal(x.Title, y.Title);
            });
            return list;
        }

        /// <summary>已收录词条数 / 总数。</summary>
        public static void Progress(out int unlocked, out int total)
        {
            Ensure();
            unlocked = 0; total = 0;
            foreach (var a in s_all)
            {
                if (a.Category == CatArchive) continue;
                total++;
                if (a.Unlocked) unlocked++;
            }
        }

        public static List<WikiArticle> Search(string keyword, int max = 40)
        {
            Ensure();
            var res = new List<(int score, WikiArticle a)>();
            string k = (keyword ?? "").Trim().ToLowerInvariant();
            if (k.Length == 0) return new List<WikiArticle>();
            foreach (var a in s_all)
            {
                int score = 0;
                string t = a.Title.ToLowerInvariant();
                if (t == k) score = 100;
                else if (t.StartsWith(k)) score = 80;
                else if (t.Contains(k)) score = 60;
                else
                {
                    foreach (var al in a.Aliases)
                        if (al.ToLowerInvariant().Contains(k)) { score = 50; break; }
                    if (score == 0 && a.Unlocked)
                    {
                        if ((a.Subtitle ?? "").ToLowerInvariant().Contains(k)) score = 30;
                        else if ((a.Lead ?? "").ToLowerInvariant().Contains(k)) score = 20;
                    }
                }
                if (score > 0) res.Add((score, a));
            }
            res.Sort((x, y) => y.score != x.score ? y.score.CompareTo(x.score) : string.CompareOrdinal(x.a.Title, y.a.Title));
            var outList = new List<WikiArticle>();
            for (int i = 0; i < res.Count && i < max; i++) outList.Add(res[i].a);
            return outList;
        }

        // ---------------- 标记 → 富文本 ----------------

        public const string LinkColor = "#126A5C";
        public const string RedLinkColor = "#B8382B";
        private static readonly Regex LinkRx = new Regex(@"\[\[([^\]\|]+)\|([^\]]+)\]\]", RegexOptions.Compiled);
        private static readonly Regex BoldRx = new Regex(@"'''(.+?)'''", RegexOptions.Compiled);

        /// <summary>把词条标记转成 UITK 富文本（站内链接用 &lt;link&gt;，未收录的词条显示红链）。</summary>
        public static string ToRich(string text, string selfId = null, bool autoLinkTerms = false)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            Ensure();
            string s = text;
            if (autoLinkTerms) s = AutoLinkTerms(s, selfId);
            s = LinkRx.Replace(s, m =>
            {
                string id = m.Groups[1].Value, label = m.Groups[2].Value;
                var target = Get(id);
                if (target == null) return label;
                string color = target.Unlocked ? LinkColor : RedLinkColor;
                return "<link=\"" + target.Id + "\"><color=" + color + ">" + label + "</color></link>";
            });
            s = BoldRx.Replace(s, "<b>$1</b>");
            return s;
        }

        private static string AutoLinkTerms(string text, string selfId)
        {
            var occ = TermHighlightSystem.FindOccurrences(text);
            if (occ.Count == 0) return text;
            var sb = new StringBuilder(text.Length + 48);
            int last = 0;
            foreach (var (start, len, id) in occ)
            {
                if (start < last || id == selfId || !s_byId.ContainsKey(id)) continue;
                sb.Append(text, last, start - last);
                sb.Append("[[").Append(id).Append('|').Append(text, start, len).Append("]]");
                last = start + len;
            }
            sb.Append(text, last, text.Length - last);
            return sb.ToString();
        }

        // ---------------- 构建 ----------------

        public static void Reload() { s_all = null; }

        private static void Ensure()
        {
            if (s_all != null) return;
            s_all = new List<WikiArticle>();
            s_byId = new Dictionary<string, WikiArticle>();
            s_byTitle = new Dictionary<string, string>();

            LoadAuthored();
            LoadTerms();
            LinkTermNeighbors();
            LoadChapters();
            LoadCollections();

            foreach (var a in s_all)
            {
                s_byId[a.Id] = a;
            }
            foreach (var a in s_all)
            {
                s_byTitle[a.Title] = a.Id;
                foreach (var al in a.Aliases) if (!string.IsNullOrEmpty(al) && !s_byTitle.ContainsKey(al)) s_byTitle[al] = a.Id;
            }
        }

        private static void Add(WikiArticle a)
        {
            if (a == null || string.IsNullOrEmpty(a.Id)) return;
            if (s_all.Exists(x => x.Id == a.Id)) return;
            s_all.Add(a);
        }

        private static void LoadAuthored()
        {
            var asset = Resources.Load<TextAsset>("Wiki/wiki_articles");
            if (asset == null) { Debug.LogWarning("[Wiki] Resources/Wiki/wiki_articles.json missing"); return; }
            JRoot root = null;
            try { root = JsonUtility.FromJson<JRoot>(asset.text); }
            catch (Exception ex) { Debug.LogWarning("[Wiki] parse failed: " + ex.Message); }
            if (root == null) return;
            s_dyk = root.dyk ?? new string[0];
            if (root.articles == null) return;
            foreach (var j in root.articles)
            {
                if (j == null || string.IsNullOrEmpty(j.id)) continue;
                var a = new WikiArticle
                {
                    Id = j.id, Title = j.title, Category = j.category, Subtitle = j.subtitle, Lead = j.lead,
                    Portrait = j.portrait, UnlockKey = j.unlock, UnlockHint = j.hint,
                    Aliases = j.aliases ?? new string[0], See = j.see ?? new string[0],
                    SortKey = j.title
                };
                if (j.infobox != null) foreach (var kv in j.infobox) a.Infobox.Add(new KeyValuePair<string, string>(kv.k, kv.v));
                if (j.sections != null) foreach (var sec in j.sections) a.Sections.Add(new WikiSection { Heading = sec.h, Text = sec.t });
                Add(a);
            }
        }

        private static readonly Dictionary<string, string> TermGroups = BuildTermGroups();

        private static Dictionary<string, string> BuildTermGroups()
        {
            var d = new Dictionary<string, string>();
            void G(string group, params string[] ids) { foreach (var i in ids) d[i] = group; }
            G("工务与线路", "rail_ballast", "steel_rail", "rail_joint", "track_patrol", "sleeper", "track_rule", "subgrade", "curve",
                "tunnel", "track_gauge", "superelevation", "rail_spike", "mud_pumping", "milepost", "gradient", "curve_radius",
                "transition_curve", "clearance", "anti_creep", "rail_flaw_detection", "fishplate", "track_jack", "tamping");
            G("信号与闭塞", "semaphore", "signal", "block", "train_token", "token_instrument", "semaphore_signal", "home_signal");
            G("行车组织", "train_meeting", "railway_section", "marshalling", "shunting", "anti_roll", "timetable", "turnout", "siding");
            G("柴油机与燃油", "fuel_pump_high", "fuel_pump", "fuel_injector", "diesel_engine", "diesel_locomotive", "needle_valve",
                "cylinder", "cylinder_liner", "piston", "cylinder_scoring", "engine_seizure", "high_pressure_pipe",
                "plunger_pump_high", "plunger_pump", "fuel_injector_assy");
            G("制动与走行部", "brake_handle", "braking", "brake_shoe", "wheelset", "wheel_tread", "wheel_flange", "bogie", "axle_box");
            G("事故", "derailment", "derailment_minor");
            G("量具", "dial_indicator", "micrometer");
            return d;
        }

        private static void LoadTerms()
        {
            var asset = Resources.Load<TextAsset>("Terms");
            if (asset == null) return;
            TermHighlightSystem.TermJsonData data = null;
            try { data = JsonUtility.FromJson<TermHighlightSystem.TermJsonData>(asset.text); }
            catch (Exception ex) { Debug.LogWarning("[Wiki] Terms parse failed: " + ex.Message); }
            if (data == null || data.terms == null) return;
            foreach (var t in data.terms)
            {
                if (t == null || string.IsNullOrEmpty(t.id) || string.IsNullOrEmpty(t.name)) continue;
                TermExplanation lead = null;
                var rest = new List<TermExplanation>();
                if (t.explanations != null)
                {
                    foreach (var e in t.explanations)
                    {
                        if (e == null || string.IsNullOrEmpty(e.text)) continue;
                        var lite = new TermExplanation { speaker = e.speaker, text = e.text };
                        if (lead == null && e.speaker != "老陈") lead = lite; else rest.Add(lite);
                    }
                }
                if (lead == null && rest.Count > 0) { lead = rest[0]; rest.RemoveAt(0); }
                TermGroups.TryGetValue(t.id, out string group);
                var a = new WikiArticle
                {
                    Id = t.id, Title = t.name, Category = CatTerm, Group = group ?? "其他", IsTerm = true,
                    Subtitle = (group ?? "铁路") + "术语",
                    Lead = lead == null ? "'''" + t.name + "'''。"
                         : lead.text.StartsWith(t.name) ? "'''" + t.name + "'''" + lead.text.Substring(t.name.Length)
                         : "'''" + t.name + "'''：" + lead.text,
                    SortKey = string.IsNullOrEmpty(t.py) ? "~" : t.py
                };
                a.Infobox.Add(new KeyValuePair<string, string>("类别", a.Group));
                if (lead != null && !string.IsNullOrEmpty(lead.speaker)) a.Infobox.Add(new KeyValuePair<string, string>("讲解人", lead.speaker));
                string sketch = t.id == "rail_ballast" || t.id == "sleeper" || t.id == "track_gauge" ? "track" : t.id == "train_meeting" ? "siding" : t.id == "diesel_locomotive" || t.id == "bogie" ? "loco" : null;
                if (sketch != null) a.Sections.Add(new WikiSection { Heading = "示意图", Text = "@draw :: " + sketch });
                if (rest.Count > 0)
                {
                    var sb = new StringBuilder("@talk :: ");
                    for (int k = 0; k < rest.Count; k++)
                    {
                        if (k > 0) sb.Append('|');
                        sb.Append(string.IsNullOrEmpty(rest[k].speaker) ? "旁白" : rest[k].speaker).Append('=')
                          .Append(rest[k].text.Replace("|", "｜").Replace("\n", " "));
                    }
                    a.Sections.Add(new WikiSection { Heading = "其他说法", Text = sb.ToString() });
                }
                Add(a);
            }
        }

        /// <summary>术语词条自动「参见」同组的其他术语（每条最多 6 个，取字母序相邻者），读完一条能顺着往下翻。</summary>
        private static void LinkTermNeighbors()
        {
            var byGroup = new Dictionary<string, List<WikiArticle>>();
            foreach (var a in s_all)
            {
                if (!a.IsTerm) continue;
                if (!byGroup.TryGetValue(a.Group, out var l)) byGroup[a.Group] = l = new List<WikiArticle>();
                l.Add(a);
            }
            foreach (var kv in byGroup)
            {
                var list = kv.Value;
                list.Sort((x, y) => string.CompareOrdinal(x.SortKey, y.SortKey));
                for (int i = 0; i < list.Count; i++)
                {
                    var see = new List<string>();
                    for (int d = 1; see.Count < 6 && d < list.Count; d++)
                    {
                        int up = i + d, dn = i - d;
                        if (up < list.Count && see.Count < 6) see.Add(list[up].Id);
                        if (dn >= 0 && see.Count < 6) see.Add(list[dn].Id);
                    }
                    if (list[i].See.Length == 0) list[i].See = see.ToArray();
                    list[i].Infobox.Add(new KeyValuePair<string, string>("同组术语", list.Count + " 条"));
                }
            }
        }

        private static void LoadChapters()
        {
            foreach (var c in StoryArchiveCatalog.ChapterEntries)
            {
                var a = new WikiArticle
                {
                    Id = "ch_" + c.id, Title = c.title, Category = CatStory, Subtitle = "序章剧情",
                    Lead = "'''" + c.title + "'''：" + c.summary,
                    UnlockKey = "ArchiveStory_" + c.id, UnlockHint = c.condition,
                    ChapterScript = c.scriptName, SortKey = c.id
                };
                a.Infobox.Add(new KeyValuePair<string, string>("脚本", c.scriptName));
                a.Infobox.Add(new KeyValuePair<string, string>("解锁条件", c.condition));
                Add(a);
            }
        }

        private static void LoadCollections()
        {
            Add(new WikiArticle
            {
                Id = "col_scenes", Title = "场景图鉴", Category = CatArchive, Subtitle = "剧情中出现过的场景",
                Lead = "'''场景图鉴'''收录剧情中出现过的背景场景。完整的画面请在「相册」应用中查看。",
                DynamicSections = () => Collect("场景", StoryArchiveCatalog.SceneEntries.Length,
                    i => (StoryArchiveCatalog.SceneEntries[i].title, "ArchiveScene_" + StoryArchiveCatalog.SceneEntries[i].id, StoryArchiveCatalog.SceneEntries[i].condition))
            });
            Add(new WikiArticle
            {
                Id = "col_cgs", Title = "事件 CG", Category = CatArchive, Subtitle = "剧情中的关键画面",
                Lead = "'''事件 CG'''收录剧情中的关键画面。完整的画面请在「相册」应用中查看。",
                DynamicSections = () => Collect("CG", StoryArchiveCatalog.Cgs.Length,
                    i => (StoryArchiveCatalog.Cgs[i].title, "ArchiveCG_" + StoryArchiveCatalog.Cgs[i].id, StoryArchiveCatalog.Cgs[i].condition))
            });
            Add(new WikiArticle
            {
                Id = "col_music", Title = "背景音乐", Category = CatArchive, Subtitle = "剧情中的曲目",
                Lead = "'''背景音乐'''收录剧情中出现过的 BGM 与歌曲。解锁后可在「音乐」应用中收听。",
                DynamicSections = () =>
                {
                    var secs = new List<WikiSection>();
                    foreach (string cat in new[] { "BGM", "歌曲" })
                    {
                        var sb = new StringBuilder();
                        int have = 0, all = 0;
                        foreach (var m in StoryArchiveCatalog.MusicEntries)
                        {
                            if (m.category != cat) continue;
                            all++;
                            bool ok = PlayerPrefs.GetInt("ArchiveMusic_" + m.id, 0) == 1;
                            if (ok) have++;
                            if (sb.Length > 0) sb.Append('\n');
                            sb.Append("· ").Append(ok ? m.title : "？？？").Append(" —— ").Append(ok ? "已收录" : "未收录（" + m.condition + "）");
                        }
                        secs.Add(new WikiSection { Heading = cat + "（" + have + "/" + all + "）", Text = sb.ToString() });
                    }
                    return secs;
                }
            });
        }

        private static List<WikiSection> Collect(string kind, int count, Func<int, (string title, string key, string condition)> item)
        {
            var sb = new StringBuilder();
            int have = 0;
            for (int i = 0; i < count; i++)
            {
                var (title, key, condition) = item(i);
                bool ok = PlayerPrefs.GetInt(key, 0) == 1;
                if (ok) have++;
                if (sb.Length > 0) sb.Append('\n');
                sb.Append("· ").Append(ok ? title : "？？？").Append(" —— ").Append(ok ? "已收录" : "未收录（" + condition + "）");
            }
            return new List<WikiSection> { new WikiSection { Heading = kind + "收录（" + have + "/" + count + "）", Text = sb.ToString() } };
        }
    }
}
