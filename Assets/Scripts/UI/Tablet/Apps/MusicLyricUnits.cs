using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>One displayed character (or letter) with the time it is sung.</summary>
    public struct LyricGlyph
    {
        public string text;
        public double start, end;
        public bool spaceAfter;
    }

    /// <summary>A group of glyphs that appears as one word on stage, e.g. "手を" or "unbelievable".</summary>
    public sealed class LyricUnit
    {
        public readonly List<LyricGlyph> glyphs = new List<LyricGlyph>();
        public bool closed;
        public double start => glyphs.Count == 0 ? 0 : glyphs[0].start;
        public double end => glyphs.Count == 0 ? 0 : glyphs[glyphs.Count - 1].end;
    }

    /// <summary>
    /// Word timing and grouping for lyric lines, following Folia's pipeline: lines without word times get weighted word times
    /// (parserCore.buildTimedWords), then characters are grouped into layout units so a noun and its particle appear together.
    /// Estimated times only drive the display; they are never stored as lyric data.
    /// </summary>
    public static class MusicLyricUnits
    {
        enum Kind { Han, Hira, Kata, Hangul, Latin, Punct, Other }
        const string Particles = "はがをにでとものへやかねよ";

        static Kind Classify(char c)
        {
            if (c >= 0x4E00 && c <= 0x9FFF || c >= 0x3400 && c <= 0x4DBF) return Kind.Han;
            if (c >= 0x3040 && c <= 0x309F) return Kind.Hira;
            if (c >= 0x30A0 && c <= 0x30FF || c >= 0xFF66 && c <= 0xFF9F) return Kind.Kata;
            if (c >= 0xAC00 && c <= 0xD7AF) return Kind.Hangul;
            if (c < 0x250 && char.IsLetterOrDigit(c) || c == '\'' || c == '’') return Kind.Latin;
            if (char.IsPunctuation(c) || char.IsSymbol(c)) return Kind.Punct;
            return Kind.Other;
        }
        static bool IsCjk(char c) { var kind = Classify(c); return kind == Kind.Han || kind == Kind.Hira || kind == Kind.Kata || kind == Kind.Hangul; }

        /// <summary>Folia's buildTimedWords: whitespace tokens, one word per CJK character, weights, 90% of the (capped) line time.</summary>
        public static List<MusicTimedWord> EstimatedWords(MusicTimedLine line, double nextStart)
        {
            var words = new List<MusicTimedWord>();
            string text = line.text;
            if (string.IsNullOrWhiteSpace(text)) return words;
            double end = !double.IsInfinity(line.end) && line.end > line.start ? line.end : nextStart;
            double reading = text.Length * 0.5;
            if (double.IsInfinity(end) || end <= line.start) end = line.start + Math.Max(3, reading);
            double duration = Math.Max(end - line.start, 0.1);
            // A long gap before the next line is mostly silence: do not stretch the words across it.
            if (duration > reading + 2 && duration > 5) duration = reading + 2;

            var pieces = new List<KeyValuePair<string, double>>();
            double total = 0;
            foreach (string token in text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            {
                bool cjk = false;
                foreach (char c in token) if (IsCjk(c)) { cjk = true; break; }
                if (cjk)
                {
                    foreach (char c in token)
                    {
                        double weight = Classify(c) == Kind.Punct ? 0 : 1;
                        pieces.Add(new KeyValuePair<string, double>(c.ToString(), weight)); total += weight;
                    }
                }
                else { double weight = 1 + token.Length * 0.15; pieces.Add(new KeyValuePair<string, double>(token + " ", weight)); total += weight; }
            }
            if (total <= 0) total = 1;
            double perWeight = duration * 0.9 / total, cursor = line.start;
            foreach (var piece in pieces)
            {
                double length = Math.Max(piece.Value * perWeight, 0.05);
                words.Add(new MusicTimedWord { text = piece.Key, start = cursor, end = cursor + length });
                if (piece.Value > 0) cursor += length;
            }
            // The last latin token should not end with a space.
            if (words.Count > 0) words[words.Count - 1].text = words[words.Count - 1].text.TrimEnd();
            return words;
        }

        static void AddEven(List<LyricGlyph> glyphs, string text, double start, double end)
        {
            if (string.IsNullOrEmpty(text)) return;
            bool space = text.Length > 0 && char.IsWhiteSpace(text[text.Length - 1]);
            string body = text.TrimEnd();
            if (body.Length == 0) { if (glyphs.Count > 0) { var last = glyphs[glyphs.Count - 1]; last.spaceAfter = true; last.text += " "; glyphs[glyphs.Count - 1] = last; } return; }
            var elements = new List<string>();
            var enumerator = StringInfo.GetTextElementEnumerator(body);
            while (enumerator.MoveNext()) elements.Add((string)enumerator.Current);
            if (double.IsInfinity(end) || end < start) end = start + 0.1;
            double step = (end - start) / elements.Count;
            for (int i = 0; i < elements.Count; i++)
            {
                bool lastElement = i == elements.Count - 1;
                glyphs.Add(new LyricGlyph { text = elements[i] + (lastElement && space ? " " : ""), start = start + step * i, end = lastElement ? end : start + step * (i + 1), spaceAfter = lastElement && space });
            }
        }

        /// <summary>The glyphs of a line with their times: the source's own word times when present, otherwise estimated ones.</summary>
        public static List<LyricGlyph> Timeline(MusicTimedLine line, double nextStart, bool estimate, out bool estimated)
        {
            estimated = false;
            var glyphs = new List<LyricGlyph>();
            if (line.words.Count > 0)
            {
                foreach (var word in line.words) AddEven(glyphs, word.text, word.start, double.IsInfinity(word.end) ? word.start + 0.5 : word.end);
                return glyphs;
            }
            if (!estimate) return glyphs;
            foreach (var word in EstimatedWords(line, nextStart)) AddEven(glyphs, word.text, word.start, word.end);
            estimated = glyphs.Count > 0;
            return glyphs;
        }

        /// <summary>
        /// Groups glyphs into display units. Japanese: a content word keeps its okurigana and takes the particle that follows it
        /// ("手を", "思い出の"); Chinese: two-character chunks; Korean and Latin: space-separated words; punctuation sticks to the word before.
        /// </summary>
        public static List<LyricUnit> Group(List<LyricGlyph> glyphs)
        {
            var units = new List<LyricUnit>();
            if (glyphs.Count == 0) return units;
            bool kana = false, han = false;
            foreach (var glyph in glyphs)
            {
                var kind = Classify(glyph.text[0]);
                if (kind == Kind.Hira || kind == Kind.Kata) kana = true; else if (kind == Kind.Han) han = true;
            }
            bool chinese = han && !kana;

            LyricUnit current = null; int hanInUnit = 0;
            for (int i = 0; i < glyphs.Count; i++)
            {
                var glyph = glyphs[i]; char c = glyph.text[0]; var kind = Classify(c);
                bool start = current == null || current.closed;
                if (!start)
                {
                    var last = current.glyphs[current.glyphs.Count - 1]; var lastKind = Classify(last.text[0]);
                    bool lastParticle = lastKind == Kind.Hira && Particles.IndexOf(last.text[0]) >= 0;
                    switch (kind)
                    {
                        case Kind.Punct: break;
                        case Kind.Han:
                            if (chinese)
                            {
                                int remaining = 0; while (i + remaining < glyphs.Count && Classify(glyphs[i + remaining].text[0]) == Kind.Han) remaining++;
                                start = hanInUnit >= 2 && remaining >= 2;
                            }
                            else start = !(lastKind == Kind.Han || lastKind == Kind.Hira && !lastParticle && current.glyphs.Count <= 4 && HasKind(current, Kind.Han));
                            break;
                        case Kind.Hira:
                            start = lastKind == Kind.Punct || current.glyphs.Count >= 6;
                            break;
                        case Kind.Kata: start = lastKind != Kind.Kata; break;
                        case Kind.Latin: start = lastKind != Kind.Latin; break;
                        case Kind.Hangul: start = current.glyphs.Count >= 6; break;
                        default: start = true; break;
                    }
                }
                if (start) { current = new LyricUnit(); units.Add(current); hanInUnit = 0; }
                current.glyphs.Add(glyph);
                if (kind == Kind.Han) hanInUnit++;
                bool particle = kind == Kind.Hira && Particles.IndexOf(c) >= 0;
                if (glyph.spaceAfter || kind == Kind.Punct) current.closed = true;
                else if (particle && !chinese)
                {
                    // A particle ends its word when it follows a content word (kanji, katakana, latin) or a few kana.
                    int before = current.glyphs.Count - 1;
                    var previousKind = before > 0 ? Classify(current.glyphs[before - 1].text[0]) : Kind.Other;
                    if (previousKind == Kind.Han || previousKind == Kind.Kata || previousKind == Kind.Latin || before >= 2) current.closed = true;
                }
            }
            return units;
        }

        static bool HasKind(LyricUnit unit, Kind kind)
        {
            foreach (var glyph in unit.glyphs) if (Classify(glyph.text[0]) == kind) return true;
            return false;
        }
    }
}
