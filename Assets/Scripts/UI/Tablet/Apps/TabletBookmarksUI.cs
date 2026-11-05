using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    public static class TabletBookmarksUI
    {
        public static void Build(VisualElement win, Action back, Action onExit = null, Action<string> toast = null)
        {
            win.Clear();
            var body = BuildAppBody(win, "阅读书签", "自动保存阅读位置，也可以从剧情菜单手动收藏");
            body.name = "tablet-bookmarks";
            var backButton = new Button(back) { text = "返回剧情", name = "bookmarks-back" };
            StyleAction(backButton); backButton.style.alignSelf = Align.FlexStart; body.Add(backButton);
            var scroll = new ScrollView(); scroll.style.flexGrow = 1; body.Add(scroll);
            Group("自动书签", BookmarkManager.GetAllAuto());
            Group("手动书签", BookmarkManager.GetAllManual());

            void Group(string title, System.Collections.Generic.List<BookmarkManager.Bookmark> list)
            {
                var heading = new Label(title); heading.style.marginTop = 14; heading.style.fontSize = 18; heading.style.unityFontDefinition = Fd(); scroll.Add(heading);
                if (list.Count == 0) { scroll.Add(new Label(title == "手动书签" ? "在剧情 Menu → 书签中添加" : "暂无自动书签")); return; }
                foreach (var bookmark in list)
                {
                    var bm = bookmark;
                    var card = BuildAppCard(); card.name = "bookmark-" + bm.id;
                    card.style.marginTop = 8; card.style.flexShrink = 0; scroll.Add(card);
                    string cardHeading = "第" + bm.episodeNum + "话 · " + bm.episodeTitle;
                    var name = new Label(bm.name != null && bm.name.StartsWith("第" + bm.episodeNum + "话") ? cardHeading : bm.name);
                    name.style.whiteSpace = WhiteSpace.Normal; name.style.fontSize = 16; name.style.unityFontDefinition = Fd(); card.Add(name);
                    var preview = new Label(bm.previewText); preview.style.whiteSpace = WhiteSpace.Normal; preview.style.color = InkSecondary; preview.style.marginTop = 6; card.Add(preview);
                    var actions = new VisualElement(); actions.style.flexDirection = FlexDirection.Row; actions.style.marginTop = 10; card.Add(actions);
                    var jump = new Button(() =>
                    {
                        if (!BookmarkManager.JumpToBookmark(bm)) { toast?.Invoke("此书签位置已失效"); return; }
                        PlayerPrefs.SetInt("VN_FromGameMain", 1); PlayerPrefs.Save();
                        onExit?.Invoke(); GameMainUI.Instance?.Hide(); SceneManager.LoadScene("VN_Test");
                    }) { text = "继续阅读", name = "bookmark-jump-" + bm.id };
                    jump.SetEnabled(BookmarkManager.IsPlayable(bm)); StyleAction(jump, true); actions.Add(jump);
                    if (bm.isAuto) continue;
                    var remove = new Button(() =>
                    {
                        if (card.Q("bookmark-delete-confirm") != null) return;
                        var confirmation = new VisualElement { name = "bookmark-delete-confirm" }; confirmation.style.marginTop = 10; card.Add(confirmation);
                        confirmation.Add(new Label("删除这个书签？存档与其他书签会保留。"));
                        var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; confirmation.Add(row);
                        var cancel = new Button(() => confirmation.RemoveFromHierarchy()) { text = "取消" }; StyleAction(cancel); row.Add(cancel);
                        var confirm = new Button(() => { BookmarkManager.RemoveManual(bm.id); Build(win, back, onExit, toast); }) { text = "确认删除" }; StyleAction(confirm, false, true); row.Add(confirm);
                    }) { text = "删除", name = "bookmark-delete-" + bm.id }; StyleAction(remove, false, true); actions.Add(remove);
                }
            }
        }
        public static void StyleAction(Button button, bool primary = false, bool danger = false)
        {
            button.style.minHeight = 48; button.style.minWidth = 112; button.style.marginRight = 10;
            button.style.paddingLeft = 18; button.style.paddingRight = 18;
            button.style.borderTopLeftRadius = 14; button.style.borderTopRightRadius = 14;
            button.style.borderBottomLeftRadius = 14; button.style.borderBottomRightRadius = 14;
            button.style.borderTopWidth = 0; button.style.borderBottomWidth = 0;
            button.style.borderLeftWidth = 0; button.style.borderRightWidth = 0;
            button.style.backgroundColor = primary ? AccentSystem : danger ? new Color(0.78f,0.2f,0.16f,0.1f) : SurfaceHover;
            button.style.color = primary ? Color.white : danger ? new Color(0.7f,0.17f,0.14f) : InkPrimary;
            button.style.fontSize = 16; button.style.unityFontDefinition = Fd();
        }
    }
}
