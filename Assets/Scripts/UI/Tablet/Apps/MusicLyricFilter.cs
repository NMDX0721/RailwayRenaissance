using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// Takes the credit and copyright lines that platforms put into lyric text ("作词：…", "Produced by …", "未经许可，不得翻唱") out of a
    /// document, so they are not shown as sung lines and do not take a turn on the stage. Applies to every source (online, imported, local).
    /// A credit label counts anywhere in the song; the wider copyright wording only counts near the start or the end, where it is printed.
    /// Independent implementation; the idea of a user-editable lyric filter follows Folia's, the rules here are our own.
    /// </summary>
    public static class MusicLyricFilter
    {
        const int EdgeLines = 8;

        // "label : value" credit lines, anywhere in the lyrics.
        static readonly Regex Credit = new Regex(
            @"^\s*[\[\(（【]?\s*(作词|作曲|编曲|词曲|词|曲|制作人|制作|监制|出品人?|出品|发行|企划|统筹|演唱|歌手|主唱|配唱|和声|合声|伴奏|吉他|贝斯|贝司|鼓|键盘|弦乐|钢琴|混音|母带|录音|录制|音乐总监|OP|SP|封面|插画|文案|歌词翻译|翻译|翻唱|原唱|原曲|原作|Lyrics?\s*by|Lyricist|Words\s*by|Written\s*by|Music\s*by|Composer|Composed\s*by|Arranger|Arranged\s*by|Arrangement|Producer|Produced\s*by|Mix(ed)?(\s*by)?|Master(ed)?(\s*by)?|Recorded\s*(by|at)|Vocals?|Guitar|Bass|Drums|Keys|Piano|Strings|Chorus|Cover\s*by|Published\s*by|Label)\s*[\]\)）】]?\s*[:：]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // English credits are often written without a colon ("Produced by Foo"); like the notices, only near the start or the end.
        static readonly Regex ByCredit = new Regex(@"^\s*(Lyrics?|Words|Written|Music|Composed|Arranged|Produced|Mixed|Mastered|Recorded|Engineered|Cover|Published|Translated)\s+by\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Copyright and notice wording, only at the start or the end.
        static readonly Regex Notice = new Regex(
            @"(版权所有|版权归|享有.{0,12}版权|未经(许可|授权|著作权人)|不得(翻唱|转载|使用|商用)|纯音乐，?请欣赏|此歌曲为没有填词的纯音乐|暂无歌词|Copyright|All rights reserved|Unauthorized|TME|℗|©|仅供(试听|学习))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>How many lines the filter took out of <paramref name="document"/>. Does nothing when <paramref name="enabled"/> is false.</summary>
        public static int Apply(MusicTimedDocument document, bool enabled, string customWords)
        {
            if (!enabled || document == null || document.lines.Count == 0) return 0;
            var words = new List<string>();
            foreach (string part in (customWords ?? "").Split(new[] { ',', '，', ';', '；', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string word = part.Trim(); if (word.Length > 0) words.Add(word);
            }
            int total = document.lines.Count, before = total;
            for (int i = document.lines.Count - 1; i >= 0; i--)
            {
                var line = document.lines[i];
                string text = line?.text ?? "";
                bool edge = i < EdgeLines || i >= total - EdgeLines;
                bool remove = Credit.IsMatch(text) || edge && (Notice.IsMatch(text) || ByCredit.IsMatch(text));
                if (!remove) foreach (string word in words) if (text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) { remove = true; break; }
                if (remove) document.lines.RemoveAt(i);
            }
            return before - document.lines.Count;
        }
    }
}
