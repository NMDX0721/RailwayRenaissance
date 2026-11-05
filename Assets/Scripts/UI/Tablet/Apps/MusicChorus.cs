using System.Collections.Generic;
using System.Text;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // Finds the chorus of a lyric document from how often its lines come back (the idea of Folia's chorus detection; the code is our own).
    // A line counts as chorus when it is sung at least twice and sits in a run of two or more such lines, or when it is sung three times or more.
    // Blank lines split runs, and interjections (one character, or a short Latin word like "oh" or "yeah") never count.
    public static class MusicChorus
    {
        public static bool[] Detect(MusicTimedDocument document)
        {
            int count = document?.lines.Count ?? 0;
            var result = new bool[count];
            if (count == 0) return result;
            var keys = new string[count]; var times = new Dictionary<string, int>();
            for (int i = 0; i < count; i++)
            {
                keys[i] = Normalize(document.lines[i].text);
                if (keys[i].Length < 2 || (keys[i].Length <= 5 && IsAscii(keys[i]))) { keys[i] = null; continue; }
                times.TryGetValue(keys[i], out int seen); times[keys[i]] = seen + 1;
            }
            var repeated = new bool[count];
            for (int i = 0; i < count; i++) repeated[i] = keys[i] != null && times[keys[i]] >= 2;
            int start = 0;
            while (start < count)
            {
                if (!repeated[start]) { start++; continue; }
                int end = start; while (end + 1 < count && repeated[end + 1]) end++;
                for (int i = start; i <= end; i++) result[i] = end > start || times[keys[i]] >= 3;
                start = end + 1;
            }
            return result;
        }

        static bool IsAscii(string text) { foreach (char c in text) if (c > 127) return false; return true; }

        static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var builder = new StringBuilder(text.Length);
            foreach (char c in text) if (char.IsLetterOrDigit(c)) builder.Append(char.ToLowerInvariant(c));
            return builder.ToString();
        }
    }
}
