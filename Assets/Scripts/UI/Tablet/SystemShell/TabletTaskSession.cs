using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    public sealed class TabletTaskSession
    {
        public string AppId { get; }
        public string Title { get; }
        public VisualElement Root { get; internal set; }
        public Focusable LastFocusedElement { get; internal set; }
        public bool IsSuspended => Root == null;
        internal TabletViewStateSnapshot Snapshot { get; set; }
        internal long LastActivated { get; set; }

        public TabletTaskSession(string appId, string title)
        {
            AppId = appId ?? throw new ArgumentNullException(nameof(appId));
            Title = string.IsNullOrWhiteSpace(title) ? appId : title;
        }
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

                if (entry.HasText && element is TextField text)
                    text.value = entry.TextValue ?? string.Empty;
                if (entry.HasScroll && element is ScrollView scroll)
                    scroll.schedule.Execute(() => scroll.scrollOffset = entry.ScrollOffset).ExecuteLater(0);
            }
        }
    }
}
