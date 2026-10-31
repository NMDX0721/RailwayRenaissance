using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    public sealed class MusicTimedWord
    {
        public double start, end;
        public string text;
    }
    public sealed class MusicTimedLine
    {
        public double start, end = double.PositiveInfinity;
        public string text, translation;
        public readonly List<MusicTimedWord> words = new List<MusicTimedWord>();
    }
    public sealed class MusicTimedDocument
    {
        public readonly List<MusicTimedLine> lines = new List<MusicTimedLine>();
        public string source, error, plainText;
        public int Active(double seconds)
        {
            // AudioSource's float/sample clock can land a fraction of a millisecond before a clicked cue.
            seconds += .001;
            int low = 0, high = lines.Count - 1, found = -1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                if (lines[mid].start <= seconds) { found = mid; low = mid + 1; } else high = mid - 1;
            }
            return found >= 0 && seconds < lines[found].end ? found : -1;
        }
    }
    [Serializable]
    public sealed class MusicLyricPreference
    {
        public string id, fileName, translationFileName, sourceName;
        public int offsetMs;
    }

    // Independent Unity implementation of public timed-text formats. No Folia source is included.
    public static class MusicTimedText
    {
        [Serializable]
        sealed class LyricSheet
        {
            public string id, source, plainText;
        }
        [Serializable]
        sealed class JsonWords { public JsonWord[] words; }
        [Serializable]
        sealed class JsonWord
        {
            public string word;
            public double start = -1, end = -1;
            public int line_index = -1;
        }
        public const int MaxBytes = 1024 * 1024;
        static readonly Regex Stamp = new Regex(@"\[(\d{1,3}:\d{2}(?:[.:]\d{1,3})?)\]", RegexOptions.Compiled);
        static readonly Regex WordStamp = new Regex(@"<(\d{1,3}:\d{2}(?:[.:]\d{1,3})?)>", RegexOptions.Compiled);
        static readonly Regex Offset = new Regex(@"\[offset:([+-]?\d+)\]", RegexOptions.IgnoreCase);
        public static MusicTimedDocument Parse(string text, string format)
        {
            var result = new MusicTimedDocument();
            if (string.IsNullOrWhiteSpace(text)) { result.error = "歌词文件为空"; return result; }
            if (Encoding.UTF8.GetByteCount(text) > MaxBytes) { result.error = "歌词文件不能超过 1 MB"; return result; }
            try
            {
                text = text.TrimStart('\uFEFF').Replace("\r\n", "\n").Replace('\r', '\n');
                if (format == ".json") ParseJsonWords(text, result);
                else if (format == ".srt" || format == ".vtt" || text.StartsWith("WEBVTT") || text.Contains("-->")) ParseCues(text, result);
                else ParseLrc(text, result);
                var ordered = result.lines.OrderBy(l => l.start).ToArray();
                result.lines.Clear(); result.lines.AddRange(ordered);
                if (result.lines.Count > 5000) throw new FormatException("字幕行数过多");
                for (int i = 0; i < result.lines.Count; i++)
                {
                    var line = result.lines[i];
                    // Repeated timestamps form bilingual lines rather than unstable competing active rows.
                    if (i > 0 && Math.Abs(line.start - result.lines[i - 1].start) < .00001)
                    {
                        result.lines[i - 1].translation = string.IsNullOrEmpty(result.lines[i - 1].translation) ? line.text : result.lines[i - 1].translation + "\n" + line.text;
                        result.lines.RemoveAt(i--); continue;
                    }
                }
                for (int i = 0; i < result.lines.Count; i++)
                {
                    var line = result.lines[i];
                    if (i + 1 < result.lines.Count) line.end = Math.Min(line.end, result.lines[i + 1].start);
                    for (int w = 0; w < line.words.Count; w++)
                        line.words[w].end = Math.Min(line.words[w].end, Math.Min(line.end,
                            w + 1 < line.words.Count ? line.words[w + 1].start : double.PositiveInfinity));
                }
                if (result.lines.Count == 0) result.error = "没有找到有效时间标记，请使用 LRC、SRT、WebVTT 或逐词 JSON";
            }
            catch (Exception ex) { result.lines.Clear(); result.error = "无法解析字幕：" + ex.Message; }
            return result;
        }
        static void ParseJsonWords(string text, MusicTimedDocument doc)
        {
            if (!text.TrimStart().StartsWith("[")) throw new FormatException("逐词 JSON 应为数组");
            var data = JsonUtility.FromJson<JsonWords>("{\"words\":" + text + "}");
            if (data?.words == null || data.words.Length == 0 || data.words.Length > 10000)
                throw new FormatException("逐词 JSON 为空或词数过多");
            double previousEnd = -1;
            foreach (var word in data.words)
            {
                if (word == null || string.IsNullOrWhiteSpace(word.word) ||
                    double.IsNaN(word.start) || double.IsNaN(word.end) ||
                    double.IsInfinity(word.start) || double.IsInfinity(word.end) ||
                    word.start < 0 || word.end <= word.start || word.start < previousEnd ||
                    word.line_index < 0 || word.line_index > doc.lines.Count ||
                    word.line_index < doc.lines.Count - 1)
                    throw new FormatException("逐词 JSON 存在缺失字段、错序或无效时间");
                if (word.line_index == doc.lines.Count)
                    doc.lines.Add(new MusicTimedLine { start = word.start, text = "" });
                var line = doc.lines[word.line_index];
                if (line.words.Count > 0)
                {
                    line.words[line.words.Count - 1].text += " ";
                    line.text += " ";
                }
                line.words.Add(new MusicTimedWord { text = word.word, start = word.start, end = word.end });
                line.text += word.word;
                line.end = word.end;
                previousEnd = word.end;
            }
        }
        static double Seconds(string stamp)
        {
            var parts = stamp.Replace(',', '.').Split(':');
            if (parts.Length < 2 || parts.Length > 3) throw new FormatException("时间格式不正确");
            double seconds = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                double value = double.Parse(parts[i], CultureInfo.InvariantCulture);
                if (value < 0 || (i > 0 && value >= 60)) throw new FormatException("时间分量越界");
                seconds = seconds * 60 + value;
            }
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new FormatException("无效时间");
            return seconds;
        }
        static void ParseLrc(string text, MusicTimedDocument doc)
        {
            double offset = 0;
            var tag = Offset.Match(text);
            if (tag.Success && int.TryParse(tag.Groups[1].Value, out int ms)) offset = ms / 1000d;
            foreach (string raw in text.Split('\n'))
            {
                var stamps = Stamp.Matches(raw);
                if (stamps.Count == 0 || stamps[0].Index != 0) continue;
                int end = 0, count = 0;
                foreach (Match stamp in stamps) { if (stamp.Index != end) break; end = stamp.Index + stamp.Length; count++; }
                string body = raw.Substring(end);
                var words = WordStamp.Matches(body);
                string plain = WordStamp.Replace(body, "").Trim();
                // Empty LRC lines are real boundaries (intro/interlude/end), never invented lyrics.
                for (int j = 0; j < count; j++)
                {
                    double start = LrcSeconds(stamps[j].Groups[1].Value) + offset;
                    var line = new MusicTimedLine { start = start, text = plain };
                    double shift = start - LrcSeconds(stamps[0].Groups[1].Value);
                    for (int w = 0; w < words.Count; w++)
                    {
                        int first = words[w].Index + words[w].Length;
                        int last = w + 1 < words.Count ? words[w + 1].Index : body.Length;
                        double at = LrcSeconds(words[w].Groups[1].Value) + shift;
                        double until = w + 1 < words.Count ? LrcSeconds(words[w + 1].Groups[1].Value) + shift : double.PositiveInfinity;
                        string token = body.Substring(first, last - first);
                        if (w == 0 && words[0].Index > 0)
                            line.words.Add(new MusicTimedWord { start = start, end = at, text = body.Substring(0, words[0].Index) });
                        if (token.Length > 0 && at >= start && until > at)
                            line.words.Add(new MusicTimedWord { start = at, end = until, text = token });
                        else if (token.Length == 0 && w == words.Count - 1) line.end = at;
                    }
                    // Disordered/malformed word tags fall back to line timing, without fabricating syllable timing.
                    if (string.Concat(line.words.Select(w => w.text)).Trim() != plain ||
                        line.words.Where((w, i) => i > 0 && w.start < line.words[i - 1].start).Any()) line.words.Clear();
                    doc.lines.Add(line);
                }
            }
        }
        static double LrcSeconds(string text)
        {
            int last = text.LastIndexOf(':');
            if (last != text.IndexOf(':')) text = text.Substring(0, last) + "." + text.Substring(last + 1);
            return Seconds(text);
        }
        static void ParseCues(string text, MusicTimedDocument doc)
        {
            var rows = text.Split('\n');
            for (int i = 0; i < rows.Length; i++)
            {
                if (!rows[i].Contains("-->")) continue;
                var timing = rows[i].Split(new[] { "-->" }, StringSplitOptions.None);
                if (timing.Length != 2) continue;
                double start = Seconds(timing[0].Trim()), end = Seconds(timing[1].Trim().Split(' ')[0]);
                var body = new StringBuilder();
                while (++i < rows.Length && !string.IsNullOrWhiteSpace(rows[i])) { if (body.Length > 0) body.Append('\n'); body.Append(rows[i]); }
                if (end <= start || body.Length == 0) continue;
                string plain = System.Net.WebUtility.HtmlDecode(Regex.Replace(body.ToString(), "<[^>]*>", ""));
                doc.lines.Add(new MusicTimedLine { start = start, end = end, text = plain });
            }
        }
        public static MusicLyricPreference Preference(string id)
        {
            var list = MusicLibrarySession.State.lyrics;
            var found = list.Find(p => p.id == id);
            if (found == null) { found = new MusicLyricPreference { id = id }; list.Add(found); }
            return found;
        }
        public static double? SeekPosition(double lineTime, int offsetMs, double duration)
        {
            double position = lineTime - offsetMs / 1000d;
            if (double.IsNaN(position) || double.IsInfinity(position) || position >= duration) return null;
            return Math.Max(0, position);
        }
        public static MusicTimedDocument Load(MusicLibraryTrack track)
        {
            if (track == null) return new MusicTimedDocument { error = "先选择一首曲目" };
            var pref = Preference(track.id);
            string directory = MusicLibrarySession.LyricsDirectory;
            MusicTimedDocument document = null, translation = null;
            if (!string.IsNullOrEmpty(pref.fileName))
            {
                document = Read(Path.Combine(directory, pref.fileName));
                document.source = string.IsNullOrEmpty(pref.sourceName) ? "已导入字幕" : pref.sourceName;
            }
            if (document == null && track.local)
            {
                string stem = Path.Combine(LocalMusicFileImporter.GetCustomMusicDirectory(), Path.GetFileNameWithoutExtension(track.fileName));
                foreach (string format in new[] { ".lrc", ".vtt", ".srt", ".json" })
                    if (File.Exists(stem + format)) { document = Read(stem + format); break; }
                if (File.Exists(stem + ".t.lrc")) translation = Read(stem + ".t.lrc");
            }
            if (document == null && !track.local)
            {
                var asset = Resources.Load<TextAsset>("Music/Lyrics/" + track.id);
                if (asset != null)
                {
                    document = Parse(asset.text, ""); document.source = "原声字幕";
                    var infoAsset = Resources.Load<TextAsset>("Music/Lyrics/" + track.id + "_info");
                    if (infoAsset != null)
                    {
                        var info = JsonUtility.FromJson<LyricSheet>(infoAsset.text);
                        if (info != null && info.id == track.id && !string.IsNullOrEmpty(info.source)) document.source = info.source;
                    }
                }
                if (document == null)
                {
                    var sheetAsset = Resources.Load<TextAsset>("Music/LyricSheets/" + track.id);
                    if (sheetAsset != null)
                    {
                        var sheet = JsonUtility.FromJson<LyricSheet>(sheetAsset.text);
                        if (sheet != null && sheet.id == track.id && !string.IsNullOrWhiteSpace(sheet.plainText))
                            document = new MusicTimedDocument { source = sheet.source, plainText = sheet.plainText };
                    }
                }
            }
            if (!string.IsNullOrEmpty(pref.translationFileName)) translation = Read(Path.Combine(directory, pref.translationFileName));
            document ??= new MusicTimedDocument { error = "暂无同步歌词 / 字幕\n可导入 LRC、SRT、WebVTT 或逐词 JSON。纯音乐不会自动生成歌词。" };
            if (translation != null)
                foreach (var line in document.lines)
                {
                    var match = translation.lines.FirstOrDefault(t => Math.Abs(t.start - line.start) <= .2);
                    if (match != null) line.translation = match.text;
                }
            return document;
        }
        static MusicTimedDocument Read(string path)
        {
            try
            {
                if (!File.Exists(path)) return new MusicTimedDocument { error = "已选字幕文件缺失，请重新导入" };
                if (new FileInfo(path).Length > MaxBytes) return new MusicTimedDocument { error = "字幕文件不能超过 1 MB" };
                var result = Parse(File.ReadAllText(path, new UTF8Encoding(false, true)), Path.GetExtension(path).ToLowerInvariant());
                result.source = Path.GetFileName(path); return result;
            }
            catch (Exception) { return new MusicTimedDocument { error = "无法读取字幕，请检查文件权限和 UTF-8 编码" }; }
        }
        public static bool Import(string id, string path, bool translation, out string message)
        {
            string extension = Path.GetExtension(path ?? "").ToLowerInvariant();
            if (extension != ".lrc" && extension != ".vtt" && extension != ".srt" && extension != ".json") { message = "支持 LRC、SRT、WebVTT 和逐词 JSON"; return false; }
            var parsed = Read(path);
            if (!string.IsNullOrEmpty(parsed.error)) { message = parsed.error; return false; }
            try
            {
                using var hash = SHA256.Create();
                string key = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(id))).Replace("-", "");
                string name = key + (translation ? ".translation" : "") + extension;
                Directory.CreateDirectory(MusicLibrarySession.LyricsDirectory);
                string destination = Path.Combine(MusicLibrarySession.LyricsDirectory, name);
                if (!string.Equals(Path.GetFullPath(path), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase)) File.Copy(path, destination, true);
                var pref = Preference(id);
                if (translation) pref.translationFileName = name;
                else { pref.fileName = name; pref.sourceName = Path.GetFileName(path); }
                MusicLibrarySession.Save(); message = "已导入 " + parsed.lines.Count + " 行字幕"; return true;
            }
            catch (Exception) { message = "字幕未能保存，请检查目录权限"; return false; }
        }
    }
}
