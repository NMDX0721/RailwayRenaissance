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
        /// <summary>True when the style draws its own previous / next line and translation, so the host hides its shared subtitle layer.</summary>
        bool OwnsSubtitles { get; }
        /// <summary>The element the host places on stage.</summary>
        VisualElement Root { get; }
        void Tick(in StageContext context);
        /// <summary>Drop the built line so the next Tick rebuilds it (settings changed, track changed).</summary>
        void Invalidate();
    }

    /// <summary>
    /// Lyric text uses the project's pixel font, set once on the lyric area. The pixel font has no Hangul, so a label that contains Korean takes
    /// the Sarasa font instead, as the lyric list does.
    /// </summary>
    public static class StageFonts
    {
        static Font hangul;

        public static Font Pixel() => RailwayRenaissance.UI.Tablet.TabletFonts.Pixel();

        public static bool HasHangul(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            foreach (char c in value) if ((c >= '가' && c <= '힯') || (c >= 'ᄀ' && c <= 'ᇿ') || (c >= '㄰' && c <= '㆏')) return true;
            return false;
        }

        public static void Apply(Label label)
        {
            if (!HasHangul(label.text)) { label.style.unityFontDefinition = StyleKeyword.Null; return; }
            if (hangul == null) hangul = Resources.Load<Font>("Fonts/SarasaMonoSC-Regular");
            if (hangul != null) label.style.unityFontDefinition = new FontDefinition { font = hangul };
        }
    }

    public static class StageStyles
    {
        sealed class Entry { public string id, title; public Func<IStageStyle> create; }
        static readonly List<Entry> entries = new List<Entry>
        {
            new Entry { id = "luminous", title = "Luminous", create = () => new MusicStageLuminous() },
            new Entry { id = "partita", title = "云阶", create = () => new MusicStagePartita() },
            new Entry { id = "tilt", title = "Tilt", create = () => new MusicStageTilt() },
            new Entry { id = "claddagh", title = "回环", create = () => new MusicStageCladdagh() },
            new Entry { id = "cadenza", title = "心象", create = () => new MusicStageCadenza() },
            new Entry { id = "still", title = "Still", create = () => new MusicStageStill() }
        };

        public const string Default = "luminous";
        public static int Count => entries.Count;
        public static string IdAt(int index) => entries[Mathf.Clamp(index, 0, entries.Count - 1)].id;
        public static string Next(string id) { for (int i = 0; i < entries.Count; i++) if (entries[i].id == id) return entries[(i + 1) % entries.Count].id; return entries[0].id; }
        public static string TitleOf(string id) { foreach (var e in entries) if (e.id == id) return e.title; return entries[0].title; }
        public static bool Has(string id) { foreach (var e in entries) if (e.id == id) return true; return false; }
        public static IStageStyle Create(string id)
        {
            foreach (var e in entries) if (e.id == id) return e.create();
            return entries[0].create();
        }
    }
}
