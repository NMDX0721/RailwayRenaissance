using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    public interface ITabletTaskLifecycle
    {
        void OnViewCreated(VisualElement view, bool restored);
        void OnForeground(VisualElement view);
        void OnBackground(VisualElement view);
        void OnViewReclaimed(VisualElement view);
        void OnClosed(VisualElement view);
    }

    internal sealed class EmptyTabletTaskLifecycle : ITabletTaskLifecycle
    {
        internal static readonly EmptyTabletTaskLifecycle Instance = new EmptyTabletTaskLifecycle();
        public void OnViewCreated(VisualElement view, bool restored) { }
        public void OnForeground(VisualElement view) { }
        public void OnBackground(VisualElement view) { }
        public void OnViewReclaimed(VisualElement view) { }
        public void OnClosed(VisualElement view) { }
    }

    public sealed class TabletTaskSession
    {
        public string AppId { get; }
        public string Title { get; }
        public VisualElement Root { get; internal set; }
        public Focusable LastFocusedElement { get; internal set; }
        public bool IsSuspended => Root == null;
        internal TabletViewStateSnapshot Snapshot { get; set; }
        internal object AppState { get; set; }
        internal ITabletTaskLifecycle Lifecycle { get; set; } = EmptyTabletTaskLifecycle.Instance;
        internal long LastActivated { get; set; }
        internal readonly List<TabletPageEntry> Pages = new List<TabletPageEntry>();
        internal List<Action> SuspendedPageBuilders { get; set; }

        public TabletTaskSession(string appId, string title)
        {
            AppId = appId ?? throw new ArgumentNullException(nameof(appId));
            Title = string.IsNullOrWhiteSpace(title) ? appId : title;
        }
    }

    internal sealed class TabletPageEntry
    {
        internal string Title;
        internal VisualElement View;
        internal Action Rebuild;
        internal Focusable PreviousFocus;
    }

    internal sealed class TabletViewStateSnapshot
    {
        private sealed class Entry
        {
            public int[] Path;
            public Vector2 ScrollOffset;
            public string TextValue;
            public bool HasScroll;
            public bool HasText;
        }

        private readonly List<Entry> _entries = new List<Entry>();

        public static TabletViewStateSnapshot Capture(VisualElement root)
        {
            var snapshot = new TabletViewStateSnapshot();
            if (root != null) snapshot.CaptureElement(root, new List<int>());
            return snapshot;
        }

        private void CaptureElement(VisualElement element, List<int> path)
        {
            var entry = new Entry { Path = path.ToArray() };
            if (element is ScrollView scroll)
            {
                entry.ScrollOffset = scroll.scrollOffset;
                entry.HasScroll = true;
            }
            if (element is TextField text)
            {
                entry.TextValue = text.value;
                entry.HasText = true;
            }
            if (entry.HasScroll || entry.HasText) _entries.Add(entry);

            for (int i = 0; i < element.hierarchy.childCount; i++)
            {
                path.Add(i);
                CaptureElement(element.hierarchy[i], path);
                path.RemoveAt(path.Count - 1);
            }
        }

        public void Restore(VisualElement root)
        {
            if (root == null) return;
            var targets = new List<KeyValuePair<Entry, VisualElement>>();
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                VisualElement element = root;
                bool found = true;
                for (int p = 0; p < entry.Path.Length; p++)
                {
                    int childIndex = entry.Path[p];
                    if (childIndex < 0 || childIndex >= element.hierarchy.childCount)
                    {
                        found = false;
                        break;
                    }
                    element = element.hierarchy[childIndex];
                }
                if (!found) continue;
                targets.Add(new KeyValuePair<Entry, VisualElement>(entry, element));
            }
            foreach (KeyValuePair<Entry, VisualElement> target in targets)
            {
                Entry entry = target.Key;
                VisualElement element = target.Value;
                if (element != root && !root.Contains(element)) continue;
                if (entry.HasText && element is TextField text)
                    text.value = entry.TextValue ?? string.Empty;
                if (entry.HasScroll && element is ScrollView scroll)
                    RestoreScrollWhenReady(scroll, entry.ScrollOffset);
            }
        }

        private static void RestoreScrollWhenReady(ScrollView scroll, Vector2 offset)
        {
            bool finished = false;
            EventCallback<GeometryChangedEvent> geometry = null;
            Action apply = () =>
            {
                if (finished) return;
                finished = true;
                scroll.contentViewport.UnregisterCallback(geometry);
                scroll.contentContainer.UnregisterCallback(geometry);
                scroll.scrollOffset = offset;
            };
            Action tryApply = () =>
            {
                if (finished || scroll.panel == null) return;
                Rect viewport = scroll.contentViewport.layout;
                if (float.IsNaN(viewport.width) || float.IsNaN(viewport.height) ||
                    viewport.width <= 0 || viewport.height <= 0) return;
                float horizontalMax = scroll.horizontalScroller.highValue;
                float verticalMax = scroll.verticalScroller.highValue;
                if (float.IsNaN(horizontalMax) || float.IsNaN(verticalMax) ||
                    horizontalMax + 0.5f < offset.x || verticalMax + 0.5f < offset.y) return;
                apply();
            };
            geometry = _ => scroll.schedule.Execute(tryApply).ExecuteLater(0);
            scroll.contentViewport.RegisterCallback(geometry);
            scroll.contentContainer.RegisterCallback(geometry);
            scroll.schedule.Execute(tryApply).ExecuteLater(0);
            // If the rebuilt content is shorter, clamp to its available range instead of waiting forever.
            scroll.schedule.Execute(apply).StartingIn(1000);
        }
    }
}
