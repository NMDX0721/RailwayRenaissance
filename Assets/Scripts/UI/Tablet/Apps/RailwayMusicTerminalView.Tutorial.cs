using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // The first-use tour: a ring around one part of the app at a time with a short card beside it (after Folia's Ponder lessons, but small).
    // It starts by itself once, on the first visit; "帮助 › 重新观看" in the settings runs it again. Skipping or finishing marks it as seen.
    public sealed partial class RailwayMusicTerminalView
    {
        struct TourStep { public string target, title, text; public TourStep(string a, string b, string c) { target = a; title = b; text = c; } }
        static readonly TourStep[] Tour =
        {
            new TourStep("music-search", "搜索", "输入歌名或歌手，也可以粘贴歌单链接；按 Ctrl+F 随时回到这里。"),
            new TourStep("music-nav-首页", "沿线收听", "首页、榜单、原声、本地音乐、收藏和云端都在这里；私人歌单在下面。"),
            new TourStep("music-player", "播放栏", "点曲名，会在列表里找到正在播放的歌；点封面，打开这张唱片的档案。"),
            new TourStep("music-lyrics", "歌词与舞台", "全屏歌词页，有多种舞台风格；按 L 也能打开。"),
            new TourStep("music-settings", "播放设置", "倍速、音色、睡眠定时和歌词时间校准。"),
            new TourStep("music-nav-settings", "音乐设置", "均衡器、统一响度、主题色和播放栏按钮都在设置里；教学可以随时重看。"),
        };
        VisualElement tourLayer; int tourIndex;

        void MaybeStartTutorial()
        {
            if (Application.isBatchMode || MusicLibrarySession.State.tutorialDone || tourLayer != null) return;
            root.schedule.Execute(() => { if (!MusicLibrarySession.State.tutorialDone && overlay == null && tourLayer == null) StartTutorial(); }).StartingIn(1200);
        }

        void StartTutorial()
        {
            EndTutorial(false);
            tourIndex = 0;
            tourLayer = Box(root, "music-tour"); tourLayer.name = "music-tour";
            tourLayer.focusable = true;
            tourLayer.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Escape) { EndTutorial(true); e.StopPropagation(); } else if (e.keyCode == KeyCode.RightArrow || e.keyCode == KeyCode.Return) { StepTour(1); e.StopPropagation(); } else if (e.keyCode == KeyCode.LeftArrow) { StepTour(-1); e.StopPropagation(); } });
            tourLayer.RegisterCallback<GeometryChangedEvent>(_ => DrawTourStep());
            DrawTourStep(); tourLayer.Focus();
        }

        void EndTutorial(bool done)
        {
            tourLayer?.RemoveFromHierarchy(); tourLayer = null;
            if (done) { MusicLibrarySession.State.tutorialDone = true; MusicLibrarySession.Save(); }
        }

        void StepTour(int delta)
        {
            int next = tourIndex + delta;
            if (next >= Tour.Length) { EndTutorial(true); return; }
            tourIndex = Mathf.Max(0, next); DrawTourStep();
        }

        void DrawTourStep()
        {
            if (tourLayer == null || tourLayer.panel == null) return;
            tourLayer.Clear();
            var step = Tour[tourIndex];
            var target = root.Q(step.target);
            if (target != null && (target.resolvedStyle.display == DisplayStyle.None || target.resolvedStyle.width <= 0)) target = null;
            var area = root.worldBound;
            float left, top, width = 300f;
            if (target != null)
            {
                var at = target.worldBound;
                var ring = Box(tourLayer, "music-tour-ring"); ring.pickingMode = PickingMode.Ignore;
                ring.style.left = at.x - area.x - 6; ring.style.top = at.y - area.y - 6; ring.style.width = at.width + 12; ring.style.height = at.height + 12;
                bool below = at.center.y < area.center.y;
                top = below ? at.yMax - area.y + 16 : at.y - area.y - 16 - 150;
                left = Mathf.Clamp(at.x - area.x, 12, Mathf.Max(12, area.width - width - 12));
            }
            else { left = (area.width - width) / 2; top = area.height / 3; }
            top = Mathf.Clamp(top, 12, Mathf.Max(12, area.height - 170));
            var card = Box(tourLayer, "music-tour-card"); card.style.left = left; card.style.top = top; card.style.width = width;
            Text(card, step.title, "music-tour-title");
            Text(card, step.text, "music-tour-text");
            var bar = Box(card, "music-tour-bar");
            Text(bar, (tourIndex + 1) + " / " + Tour.Length, "music-tour-count");
            Chip(bar, "跳过", () => EndTutorial(true), "music-tour-skip").AddToClassList("music-tour-button");
            if (tourIndex > 0) Chip(bar, "上一步", () => StepTour(-1), "music-tour-prev").AddToClassList("music-tour-button");
            var next = Chip(bar, tourIndex + 1 >= Tour.Length ? "完成" : "下一步", () => StepTour(1), "music-tour-next"); next.AddToClassList("music-tour-button"); next.AddToClassList("music-primary");
        }
    }
}
