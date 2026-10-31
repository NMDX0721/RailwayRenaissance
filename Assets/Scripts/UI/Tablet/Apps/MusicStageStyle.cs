using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>The per-style tunables Folia's classic style exposes; saved with the music library.</summary>
    public struct StageTuning
    {
        public int intensity;          // 0 calm, 1 normal, 2 chaotic: how wildly words are scattered
        public bool rotation;          // words tilt while waiting / after being sung
        public float breathing;        // 0..2 multiplier on the whole-line float
        public float spacing;          // 0..2 multiplier on the measured gap between words
        public bool legacyLayout;      // use the source words as they are, without semantic grouping

        public static StageTuning FromState(MusicLibraryState state) => new StageTuning
        {
            intensity = Mathf.Clamp(state.stageIntensity, 0, 2), rotation = state.stageRotation,
            breathing = Mathf.Clamp(state.stageBreathing, 0, 2), spacing = Mathf.Clamp(state.stageSpacing, 0, 2),
            legacyLayout = state.stageLegacyLayout
        };
    }

    /// <summary>Everything a stage style needs for one frame. Styles never read the player themselves, so they all follow the same clock.</summary>
    public struct StageContext
    {
        public MusicTimedDocument document;
        public int active;
        public double clock;
        public float dt;
        public bool calm, estimate, showTranslation;
        public float fontScale, energy, low, mid, high;
        public Color accent;
        public StageTuning tuning;
    }

    /// <summary>One lyric stage look (Folia calls these visualizer styles). The host owns the shared subtitle layer around it.</summary>
    public interface IStageStyle
    {
        string Id { get; }
        string Title { get; }
        /// <summary>The element the host places on stage.</summary>
        VisualElement Root { get; }
        void Tick(in StageContext context);
        /// <summary>Drop the built line so the next Tick rebuilds it (settings changed, track changed).</summary>
        void Invalidate();
    }

    public static class StageStyles
    {
        sealed class Entry { public string id, title; public Func<IStageStyle> create; }
        static readonly List<Entry> entries = new List<Entry>
        {
            new Entry { id = "luminous", title = "Luminous", create = () => new MusicStageLuminous() }
        };

        public const string Default = "luminous";
        public static int Count => entries.Count;
        public static string IdAt(int index) => entries[Mathf.Clamp(index, 0, entries.Count - 1)].id;
        public static string TitleOf(string id) { foreach (var e in entries) if (e.id == id) return e.title; return entries[0].title; }
        public static bool Has(string id) { foreach (var e in entries) if (e.id == id) return true; return false; }
        public static IStageStyle Create(string id)
        {
            foreach (var e in entries) if (e.id == id) return e.create();
            return entries[0].create();
        }
    }
}
