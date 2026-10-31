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

    public static List<MusicTimedWord> Words(MusicTimedLine line, double nextStart)
    {
        var result = new List<MusicTimedWord>();
        string text = line.text;
        if (string.IsNullOrWhiteSpace(text)) return result;
        double end = !double.IsInfinity(line.end) && line.end > line.start ? line.end : nextStart;
        if (double.IsInfinity(end) || end <= line.start) end = line.start + 4;
        end = Math.Min(end, line.start + 12);
        double span = (end - line.start) * 0.9;
        if (span < 0.4) return result;

        var tokens = new List<string>(); var word = new StringBuilder();
        void Flush() { if (word.Length > 0) { tokens.Add(word.ToString()); word.Clear(); } }
        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (word.Length > 0) { word.Append(c); Flush(); }
                else if (tokens.Count > 0) tokens[tokens.Count - 1] += c;
                continue;
            }
            if (c < 0x250 && (char.IsLetterOrDigit(c) || c == '\'')) { word.Append(c); continue; }
            if (char.IsPunctuation(c) || char.IsSymbol(c))
            {
                if (word.Length > 0) { word.Append(c); Flush(); }
                else if (tokens.Count > 0) tokens[tokens.Count - 1] += c;
                else tokens.Add(c.ToString());
                continue;
            }
            Flush(); tokens.Add(c.ToString());
        }
        Flush();
        if (tokens.Count == 0) return result;

        var weights = new double[tokens.Count]; double total = 0;
        for (int i = 0; i < tokens.Count; i++)
        {
            double weight = 0;
            foreach (char c in tokens[i]) weight += char.IsWhiteSpace(c) || char.IsPunctuation(c) ? 0.15 : c < 0x250 ? 0.5 : 1.0;
            weights[i] = Math.Max(0.3, weight); total += weights[i];
        }
        double at = line.start;
        for (int i = 0; i < tokens.Count; i++)
        {
            double length = span * weights[i] / total;
            result.Add(new MusicTimedWord { start = at, end = at + length, text = tokens[i] });
            at += length;
        }
        return result;
    }
}
}
