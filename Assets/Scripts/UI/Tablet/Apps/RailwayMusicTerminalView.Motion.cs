using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    public sealed partial class RailwayMusicTerminalView
    {
        // Page entrance: after a navigation the page's blocks fade up one after another instead of popping in.
        // Only runs for real navigations (not for refreshes while playing), and not with "减少动效".
        void PlayEnter()
        {
            if (root == null || content == null || root.ClassListContains("music-reduce-motion") || Application.isBatchMode) return;
            var blocks = new List<VisualElement>();
            void Collect(VisualElement parent)
            {
                foreach (var child in parent.Children())
                {
                    if (child.resolvedStyle.position == Position.Absolute) continue;
                    blocks.Add(child);
                }
            }
            Collect(content);
            // A page is usually one scroll view: stagger its sections instead of fading the whole page at once.
            if (blocks.Count == 1 && blocks[0] is ScrollView sv) { blocks.Clear(); Collect(sv.contentContainer); }
            int shown = Mathf.Min(blocks.Count, 9);
            for (int i = 0; i < shown; i++)
            {
                var block = blocks[i];
                block.AddToClassList("music-enter-anim"); block.AddToClassList("music-enter");
                block.schedule.Execute(() => block.RemoveFromClassList("music-enter")).StartingIn(30 + i * 45);
                block.schedule.Execute(() => block.RemoveFromClassList("music-enter-anim")).StartingIn(30 + i * 45 + 420);
            }
        }
    }
}
