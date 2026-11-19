using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.Apps;

namespace RailwayRenaissance.Editor
{
    /// <summary>媒姬百科截图评审：首页、人物/机车/术语/时间线词条、合集、搜索联想、窄窗信息框。</summary>
    [InitializeOnLoad]
    public static class TabletWikiReview
    {
        private const string PendingKey = "TabletWikiReview.Pending";
        private const string OutputDir = "tools/compile/tablet_wiki_v1";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        private static double deadline, nextCheck;
        private static int stage, frame;
        private static UIDocument document;
        private static VisualElement root;
        private static RenderTexture target;
        private static GameMainUI ui;
        private static VNManager wikiPauseVn;
        private static string pendingCapture;

        static TabletWikiReview()
        {
            if (SessionState.GetBool(PendingKey, false)) StartChecking();
        }

        private static void Assert(bool c, string m)
        {
            if (!c) throw new InvalidOperationException("[TabletWikiReview] " + m);
        }

        private static void Resize(int w, int h)
        {
            if (target != null)
            {
                document.panelSettings.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
            target = new RenderTexture(w, h, 24);
            target.Create();
            document.panelSettings.targetTexture = target;
        }

        private static void Capture(string name)
        {
            Flush();
            pendingCapture = name;
        }

        private static void Flush()
        {
            if (pendingCapture == null) return;
            string name = pendingCapture;
            pendingCapture = null;
            var prev = RenderTexture.active;
            RenderTexture.active = target;
            var img = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            img.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            img.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(OutputDir, name + ".png"), img.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(img);
        }

        [MenuItem("Tools/Tablet/Run Wiki Review")]
        public static void Batch()
        {
            TabletWikiAppUI.Motion = false; // 截图要静态画面，不要截到过渡中间帧
            Directory.CreateDirectory(OutputDir);
            SessionState.SetBool(PendingKey, true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void StartChecking()
        {
            stage = 0;
            frame = Time.frameCount;
            deadline = EditorApplication.timeSinceStartup + 120.0;
            nextCheck = 0;
            EditorApplication.update += Step;
        }

        private static void Next()
        {
            stage++;
            frame = Time.frameCount;
        }

        private static void Step()
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 0.05;
            if (EditorApplication.timeSinceStartup > deadline) { Teardown(false, "Timed out at stage " + stage); return; }
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if ((stage > 0 || ui != null) && Time.frameCount - frame < 25) return;
            Flush();
            try
            {
                switch (stage)
                {
                    case 0:
                        if (ui != null)
                        {
                            GameMainUI.OpenTabletApp("媒姬百科");
                            Next(); break;
                        }
                        GameMainUI.Show();
                        ui = GameMainUI.Instance;
                        Assert(ui != null, "GameMainUI.Show failed");
                        typeof(GameMainUI).GetMethod("EnsureDesktopBuiltSync", Hidden)?.Invoke(ui, null);
                        document = (UIDocument)typeof(GameMainUI).GetField("uiDoc", Hidden).GetValue(ui);
                        root = document.rootVisualElement;
                        var settings = UnityEngine.Object.Instantiate(document.panelSettings);
                        settings.clearColor = true;
                        settings.colorClearValue = new Color(0.06f, 0.07f, 0.06f);
                        document.panelSettings = settings;
                        Resize(2560, 1440);
                        typeof(GameMainUI).GetMethod("Unlock", Hidden)?.Invoke(ui, null);
                        Capture("00_desktop");
                        frame = Time.frameCount; break;
                    case 1:
                        Assert(root.Q("wiki-root") != null, "wiki-root missing");
                        Capture("01_home"); Next(); break;
                    case 2: TabletWikiAppUI.OpenArticle("laochen"); Next(); break;
                    case 3:
                        Assert(root.Q("wiki-article-title") != null, "article title missing");
                        Capture("02_article_person"); Next(); break;
                    case 4: TabletWikiAppUI.OpenArticle("道砟"); Next(); break;
                    case 5: Capture("03_article_term"); Next(); break;
                    case 6: TabletWikiAppUI.OpenArticle("nf5"); Next(); break;
                    case 7: Capture("04_article_loco"); Next(); break;
                    case 8: TabletWikiAppUI.OpenArticle("timeline"); Next(); break;
                    case 9: Capture("05_article_timeline"); Next(); break;
                    case 10: TabletWikiAppUI.OpenArticle("col_music"); Next(); break;
                    case 11: Capture("06_collection"); Next(); break;
                    case 12:
                        var search = root.Q<TextField>("wiki-search");
                        Assert(search != null, "wiki-search missing");
                        search.value = "闭塞"; Next(); break;
                    case 13: Capture("07_search_suggest"); Next(); break;
                    case 14: TabletWikiAppUI.OpenTab("search"); Next(); break;
                    case 15: Capture("08_tab_search"); Next(); break;
                    case 16: TabletWikiAppUI.OpenTab("more"); Next(); break;
                    case 17: Capture("09_tab_more"); Next(); break;
                    case 18: TabletWikiAppUI.OpenTab("history"); Next(); break;
                    case 19: Capture("10_tab_history"); Next(); break;
                    case 20:
                        Resize(1280, 800);
                        TabletWikiAppUI.OpenArticle("laochen"); Next(); break;
                    case 21: Capture("11_narrow_article"); Next(); break;
                    case 22:
                        Resize(2560, 1440);
                        TabletWikiAppUI.OpenFromVN("道砟", () => { });
                        Next(); break;
                    case 23:
                        Assert(root.Q("wiki-back-vn") != null, "back-to-VN pill missing");
                        Assert(root.Q("wiki-nav-back") == null, "fresh VN root should have no history arrow");
                        Capture("12_from_vn"); Next(); break;
                    case 24:
                        Assert(!TabletWikiAppUI.TryGoBack(), "TryGoBack on root must return false so the shell exits to VN");
                        TabletWikiAppUI.OpenArticle("rail_ballast"); Next(); break;
                    case 25:
                        Assert(root.Q("wiki-back-vn") == null, "plain open must clear VN origin");
                        Capture("13_term_enriched"); Next(); break;
                    case 26: TabletWikiAppUI.OpenArticle("entrust_regulation"); Next(); break;
                    case 27:
                        Assert(root.Q("wiki-article-title") != null, "entrust_regulation missing");
                        Capture("14_entrust_regulation"); Next(); break;
                    case 28: TabletWikiAppUI.OpenArticle("timeline"); Next(); break;
                    case 29: Capture("15_timeline_v2"); Next(); break;
                    case 30: TabletWikiAppUI.OpenArticle("suiyue"); Next(); break;
                    case 31: Capture("16_suiyue"); Next(); break;
                    case 32: Resize(2560, 1440); Next(); break;
                    case 33: TabletWikiAppUI.OpenArticle("wufeng_line"); Next(); break;
                    case 34: foreach (var sv in root.Query<ScrollView>().ToList()) sv.scrollOffset = new Vector2(0, 380); Next(); break;
                    case 35: Capture("17_line_chart"); Next(); break;
                    case 36: TabletWikiAppUI.OpenArticle("great_dereg"); Next(); break;
                    case 37: foreach (var sv in root.Query<ScrollView>().ToList()) sv.scrollOffset = new Vector2(0, 700); Next(); break;
                    case 38: Capture("18_dereg_charts"); Next(); break;
                    case 39: TabletWikiAppUI.OpenArticle("laochen"); Next(); break;
                    case 40: foreach (var sv in root.Query<ScrollView>().ToList()) sv.scrollOffset = new Vector2(0, 600); Next(); break;
                    case 41: Capture("19_relations"); Next(); break;
                    case 42: TabletWikiAppUI.OpenArticle("entrust_regulation"); Next(); break;
                    case 43: foreach (var sv in root.Query<ScrollView>().ToList()) sv.scrollOffset = new Vector2(0, 700); Next(); break;
                    case 44: Capture("20_regulation_blocks"); Next(); break;
                    case 45: TabletWikiAppUI.OpenArticle("timeline"); Next(); break;
                    case 46: foreach (var sv in root.Query<ScrollView>().ToList()) sv.scrollOffset = new Vector2(0, 700); Next(); break;
                    case 47: Capture("21_timeline_blocks"); Next(); break;
                    case 48: TabletWikiAppUI.OpenArticle("uset"); Next(); break;
                    case 49: foreach (var sv in root.Query<ScrollView>().ToList()) sv.scrollOffset = new Vector2(0, 600); Next(); break;
                    case 50: Capture("22_uset_split"); Next(); break;
                    case 51: TabletWikiAppUI.OpenArticle("nf5"); Next(); break;
                    case 52: foreach (var sv in root.Query<ScrollView>().ToList()) sv.scrollOffset = new Vector2(0, 600); Next(); break;
                    case 53: Capture("23_nf5_stats"); Next(); break;
                                        case 54: TabletWikiAppUI.OpenArticle("cylinder_scoring"); Next(); break;
                    case 55: foreach (var sv in root.Query<ScrollView>().ToList()) sv.scrollOffset = new Vector2(0, 200); Next(); break;
                    case 56: Capture("24_term_talk"); Next(); break;
                    case 57: TabletWikiAppUI.OpenArticle("rail_ballast"); Next(); break;
                    case 58: foreach (var sv in root.Query<ScrollView>().ToList()) sv.scrollOffset = new Vector2(0, 200); Next(); break;
                    case 59: Capture("25_term_talk2"); Next(); break;
                    case 60: TabletWikiAppUI.OpenArticle("uset"); Next(); break;
                    case 61: foreach (var sv in root.Query<ScrollView>().ToList()) sv.scrollOffset = new Vector2(0, 200); Next(); break;
                    case 62: Capture("26_style_check"); Next(); break;
                                        case 63: TabletWikiAppUI.OpenArticle("rail_ballast"); Next(); break;
                    case 64: foreach (var sv in root.Query<ScrollView>().ToList()) sv.scrollOffset = new Vector2(0, 0); Next(); break;
                    case 65: Capture("27_line_track"); Next(); break;
                    case 66: TabletWikiAppUI.OpenArticle("nf5"); Next(); break;
                    case 67: foreach (var sv in root.Query<ScrollView>().ToList()) sv.scrollOffset = new Vector2(0, 300); Next(); break;
                    case 68: Capture("28_line_loco"); Next(); break;
                    case 69: TabletWikiAppUI.OpenArticle("wufeng_line"); Next(); break;
                    case 70: foreach (var sv in root.Query<ScrollView>().ToList()) sv.scrollOffset = new Vector2(0, 900); Next(); break;
                    case 71: Capture("29_line_siding"); Next(); break;
                    case 72: TabletWikiAppUI.OpenArticle("great_dereg"); Next(); break;
                    case 73: foreach (var sv in root.Query<ScrollView>().ToList()) sv.scrollOffset = new Vector2(0, 300); Next(); break;
                    case 74: Capture("30_line_style"); Next(); break;
                    case 75:
                    {
                        var sheet = new VisualElement { name = "mascot-sheet" };
                        sheet.style.position = Position.Absolute; sheet.style.left = 0; sheet.style.top = 0; sheet.style.right = 0; sheet.style.bottom = 0;
                        sheet.style.backgroundColor = new Color(0.93f, 0.95f, 0.95f, 1f); sheet.style.flexDirection = FlexDirection.Row;
                        sheet.style.alignItems = Align.Center; sheet.style.justifyContent = Justify.SpaceAround;
                        foreach (float sz in new[] { 640f, 220f, 96f, 56f, 38f }) sheet.Add(new RailwayRenaissance.UI.Tablet.TabletMediaMascot(sz));
                        root.Add(sheet); Next(); break;
                    }
                    case 76: Capture("31_mascot"); Next(); break;
                    case 77:
                    {
                        var article = TabletWikiData.Get("laochen");
                        string key = article.UnlockKey;
                        bool hadKey = PlayerPrefs.HasKey(key);
                        int previous = PlayerPrefs.GetInt(key);
                        try
                        {
                            PlayerPrefs.SetInt(key, 0);
                            TabletWikiAppUI.OpenArticle(article.Id);
                            var articleView = root.Q<ScrollView>("wiki-main");
                            Assert(articleView.Query<Label>().ToList().Exists(label => label.text == "此词条尚未收录"),
                                "locked article did not show its unlock notice");
                            GameMainUI.OpenTabletApp("设置");
                            PlayerPrefs.SetInt(key, 1);
                            GameMainUI.OpenTabletApp("媒姬百科");
                            Assert(root.Q<ScrollView>("wiki-main") == articleView && !articleView.Query<Label>().ToList()
                                .Exists(label => label.text == "此词条尚未收录"), "returning to Wiki did not refresh the cached article");
                        }
                        finally
                        {
                            if (hadKey) PlayerPrefs.SetInt(key, previous);
                            else PlayerPrefs.DeleteKey(key);
                        }
                        Next(); break;
                    }
                    case 78:
                    {
                        var track = Array.Find(StoryArchiveCatalog.MusicEntries, item => item.id == "cloud_rail");
                        string key = "ArchiveMusic_" + track.id;
                        bool hadKey = PlayerPrefs.HasKey(key);
                        int previous = PlayerPrefs.GetInt(key);
                        try
                        {
                            PlayerPrefs.SetInt(key, 0);
                            TabletWikiAppUI.OpenArticle("col_music");
                            var articleView = root.Q<ScrollView>("wiki-main");
                            bool HasTrack() => articleView.Query<Label>().ToList().Exists(label => label.text.Contains(track.title));
                            Assert(!HasTrack(), "locked track appeared in the music collection");
                            GameMainUI.OpenTabletApp("设置");
                            PlayerPrefs.SetInt(key, 1);
                            GameMainUI.OpenTabletApp("媒姬百科");
                            Assert(root.Q<ScrollView>("wiki-main") == articleView && HasTrack(),
                                "returning to Wiki did not refresh the cached music collection");
                        }
                        finally
                        {
                            if (hadKey) PlayerPrefs.SetInt(key, previous);
                            else PlayerPrefs.DeleteKey(key);
                        }
                        VerifyGuestWatchMerge();
                        Next(); break;
                    }
                    case 79:
                    {
                        var host = new GameObject("VN Wiki pause review");
                        host.SetActive(false);
                        wikiPauseVn = host.AddComponent<VNManager>();
                        typeof(VNManager).GetField("isAutoPlay", Hidden).SetValue(wikiPauseVn, true);
                        typeof(VNManager).GetField("isScriptRunning", Hidden).SetValue(wikiPauseVn, true);
                        GameMainUI.OpenTipsPage("rail_ballast");
                        Assert(StoryAdvanceBlocked(), "VN input was not blocked by Wiki");
                        var timer = (System.Collections.IEnumerator)typeof(VNManager).GetMethod("AutoPlayTimer", Hidden)
                            .Invoke(wikiPauseVn, null);
                        Assert(timer.MoveNext() && timer.Current is WaitForSeconds, "VN Auto did not start its normal delay");
                        Assert(timer.MoveNext() && timer.Current == null, "VN Auto did not wait while Wiki was open");
                        typeof(GameMainUI).GetMethod("ReturnToTabletHome", Hidden).Invoke(ui, null);
                        Assert(StoryAdvanceBlocked(), "the Wiki return frame leaked through to VN");
                        Next(); break;
                    }
                    case 80:
                        Assert(!StoryAdvanceBlocked(), "VN stayed paused after returning from Wiki");
                        UnityEngine.Object.Destroy(wikiPauseVn.gameObject);
                        Teardown(true, "all stages, unlock refresh, guest watch merge and VN playback pause"); break;
                }
            }
            catch (Exception ex) { Teardown(false, "Exception at stage " + stage + ": " + ex.Message + "\n" + ex.StackTrace); }
        }

        private static bool StoryAdvanceBlocked() =>
            (bool)typeof(VNManager).GetMethod("AdvanceBlockedByPanels", Hidden).Invoke(wikiPauseVn, null);

        private static void VerifyGuestWatchMerge()
        {
            const string guest = "WikiWatch_local";
            string account = "WikiWatch_review_" + Guid.NewGuid().ToString("N");
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var load = typeof(TabletWikiAppUI).GetMethod("LoadList", flags);
            var save = typeof(TabletWikiAppUI).GetMethod("SaveList", flags);
            var merge = typeof(TabletWikiAppUI).GetMethod("MergeLocalWatch", flags);
            var cache = (Dictionary<string, List<string>>)typeof(TabletWikiAppUI).GetField("s_listCache", flags).GetValue(null);
            bool hadGuest = PlayerPrefs.HasKey(guest);
            string previous = PlayerPrefs.GetString(guest);
            List<string> Load(string key) => (List<string>)load.Invoke(null, new object[] { key });
            void Save(string key, List<string> list) => save.Invoke(null, new object[] { key, list });
            try
            {
                Save(guest, new List<string> { "laochen" });
                merge.Invoke(null, new object[] { account });
                Assert(Load(account).Contains("laochen") && Load(guest).Count == 0, "guest watch was not consumed");
                Save(account, new List<string>());
                merge.Invoke(null, new object[] { account });
                Assert(Load(account).Count == 0, "second sign-in resurrected a removed watch");
            }
            finally
            {
                if (hadGuest) PlayerPrefs.SetString(guest, previous); else PlayerPrefs.DeleteKey(guest);
                PlayerPrefs.DeleteKey(account);
                cache.Remove(guest);
                cache.Remove(account);
                PlayerPrefs.Save();
            }
        }

        private static void Teardown(bool ok, string reason)
        {
            if (ok) Flush();
            EditorApplication.update -= Step;
            SessionState.SetBool(PendingKey, false);
            if (target != null)
            {
                if (document != null && document.panelSettings != null) document.panelSettings.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                target = null;
            }
            EditorApplication.isPlaying = false;
            if (ok) { Debug.Log("[TABLET_WIKI_REVIEW_OK] " + reason); EditorApplication.Exit(0); }
            else { Debug.LogError("[TABLET_WIKI_REVIEW_FAIL] " + reason); EditorApplication.Exit(1); }
        }
    }
}
