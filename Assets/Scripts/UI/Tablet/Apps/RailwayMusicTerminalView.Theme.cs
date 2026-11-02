using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // The app's accent colour follows the cover of the track that is playing: the colour of the main buttons, the active pill and rail item, the
    // sliders' handles, the playing row and the small headings. It moves to the new colour over about half a second, and returns to brass when
    // nothing is playing. The colour is read from the cover (see MusicReactiveStageView.AccentFor); nothing is stored.
    public sealed partial class RailwayMusicTerminalView
    {
        static readonly Color BrassAccent = new Color(205f / 255f, 176f / 255f, 112f / 255f, 1f);
        static readonly Color BaseBackground = new Color(32f / 255f, 33f / 255f, 31f / 255f, 1f);
        Color accent = BrassAccent, accentTarget = BrassAccent;
        string themeId = "<none>"; float nextAccentPass;

        void UpdateTheme(string id)
        {
            if (themeId != id)
            {
                themeId = id;
                var track = MusicLibrarySession.Find(id);
                accentTarget = track == null ? BrassAccent : MusicReactiveStageView.AccentFor(MusicAlbumArtResolver.GetAlbumCover(id, track.category));
            }
            float distance = Mathf.Abs(accent.r - accentTarget.r) + Mathf.Abs(accent.g - accentTarget.g) + Mathf.Abs(accent.b - accentTarget.b);
            if (distance < .004f)
            {
                // Sheets and menus opened since the last change start with the stylesheet's brass; catch them a few times a second.
                if (Time.unscaledTime > nextAccentPass) { nextAccentPass = Time.unscaledTime + .4f; if (accent != BrassAccent) ApplyAccent(); }
                return;
            }
            accent = distance < .02f || MusicLibrarySession.State.reduceMotion ? accentTarget : Color.Lerp(accent, accentTarget, 1f - Mathf.Exp(-7f * Time.unscaledDeltaTime));
            ApplyAccent();
        }

        // Called after a page is drawn as well, because drawing creates new elements that start with the stylesheet's brass.
        void ApplyAccent()
        {
            if (root == null || root.panel == null) return;
            Color ink = accent.grayscale > .5f ? new Color(.12f, .13f, .1f) : Color.white;
            Color soft = Color.Lerp(accent, Color.white, .35f), dim = Color.Lerp(accent, new Color(.45f, .45f, .4f), .45f);
            void Fill(VisualElement e)
            {
                e.style.backgroundColor = accent; e.style.borderTopColor = accent; e.style.borderBottomColor = accent; e.style.borderLeftColor = accent; e.style.borderRightColor = accent;
                e.style.color = ink;
                foreach (var icon in e.Query(className: "music-vector-icon").ToList()) icon.style.color = ink;
            }
            foreach (var e in root.Query(className: "music-primary").ToList()) Fill(e);
            foreach (var e in root.Query(className: "music-home-chip").Where(x => x.ClassListContains("is-active")).ToList()) Fill(e);
            foreach (var e in root.Query(className: "music-channel-selector").Children<VisualElement>().Where(x => x.ClassListContains("is-active")).ToList()) Fill(e);
            foreach (var e in root.Query(className: "music-big-play").ToList()) Fill(e);
            foreach (var e in root.Query(className: "unity-base-slider__dragger").ToList()) e.style.backgroundColor = accent;
            foreach (var e in root.Query(className: "music-navigation").Descendents<VisualElement>().Where(x => x.ClassListContains("is-active") && x is Button).ToList())
            {
                e.style.backgroundColor = new Color(accent.r, accent.g, accent.b, .2f); e.style.color = soft;
                foreach (var icon in e.Query(className: "music-vector-icon").ToList()) icon.style.color = soft;
            }
            foreach (var row in root.Query(className: "music-track-row").Where(x => x.ClassListContains("is-playing")).ToList())
                foreach (var title in row.Query<Label>(className: "music-track-title").ToList()) title.style.color = soft;
            foreach (var e in root.Query<Label>(className: "music-eyebrow").ToList()) e.style.color = dim;
            foreach (var e in root.Query<Label>(className: "music-suggest-heading").ToList()) e.style.color = dim;
            foreach (var e in root.Query(className: "music-header").ToList()) e.style.borderBottomColor = dim;
            foreach (var e in root.Query(className: "music-listening-hero").ToList()) e.style.borderLeftColor = accent;
            root.style.backgroundColor = Color.Lerp(BaseBackground, new Color(accent.r * .5f, accent.g * .5f, accent.b * .5f, 1f), .08f);
        }
    }
}
