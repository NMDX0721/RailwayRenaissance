using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RailwayRenaissance.UI.Tablet.Apps
{

/// <summary>
/// Presentation helpers for line-timed lyrics. Estimated word times are only used to draw the fill and are always
/// labelled as estimates; they are never written into the library or shown as measured data.
/// </summary>
public static class MusicLyricEstimate
{
    static readonly Regex Credit = new Regex(
        @"^\s*(作词|作曲|编曲|制作人|制作|演唱|歌手|混音|母带|录音|监制|配唱|和声|词|曲|Lyrics?|Lyricist|Composer|Arranger|Arrangement|Producer|Mix(ed)?|Master(ed)?|Vocals?|Written by|Words|Music)\s*[:：]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Removes credit lines such as "作词：…" that platforms put in the lyric text.</summary>
    public static void StripCredits(MusicTimedDocument document)
    {
        if (document != null) document.lines.RemoveAll(line => line != null && Credit.IsMatch(line.text ?? ""));
    }

    public static List<MusicTimedWord> Words(MusicTimedLine line, double nextStart) => MusicLyricUnits.EstimatedWords(line, nextStart);
}
}
