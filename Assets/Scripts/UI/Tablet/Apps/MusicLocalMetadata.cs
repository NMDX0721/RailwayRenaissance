using System;
using System.IO;
using System.Text;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // Conservative ID3v2.3/v2.4 text reader; unsupported or malformed tags fall back to filename.
    public static class MusicLocalMetadata
    {
        public sealed class Tags { public string title, artist; public byte[] cover; }
        static int SyncSafe(byte[] b, int p) => (b[p] << 21) | (b[p + 1] << 14) | (b[p + 2] << 7) | b[p + 3];
        static string TextFrame(byte[] data, int offset, int length)
        {
            if (length < 2) return null;
            Encoding encoding;
            switch (data[offset])
            {
                case 0: encoding = Encoding.GetEncoding(28591); break;
                case 1:
                    if (length < 4) return null;
                    if (data[offset + 1] == 0xff && data[offset + 2] == 0xfe) { encoding = Encoding.Unicode; offset += 2; length -= 2; }
                    else if (data[offset + 1] == 0xfe && data[offset + 2] == 0xff) { encoding = Encoding.BigEndianUnicode; offset += 2; length -= 2; }
                    else return null;
                    break;
                case 2: encoding = Encoding.BigEndianUnicode; break;
                case 3: encoding = Encoding.UTF8; break;
                default: return null;
            }
            return encoding.GetString(data, offset + 1, length - 1).Trim('\0', ' ', '\r', '\n');
        }
        static byte[] PictureFrame(byte[] data, int offset, int length)
        {
            int end = offset + length;
            if (length < 5 || data[offset] > 3) return null;
            int encoding = data[offset], p = offset + 1, mimeStart = p;
            while (p < end && data[p] != 0) p++;
            if (p >= end) return null;
            string mime = Encoding.ASCII.GetString(data, mimeStart, p - mimeStart).ToLowerInvariant();
            if (mime != "image/jpeg" && mime != "image/png") return null;
            p += 2; // MIME terminator and picture type.
            if (p >= end) return null;
            if (encoding == 1 || encoding == 2)
            {
                while (p + 1 < end && (data[p] != 0 || data[p + 1] != 0)) p += 2;
                p += 2;
            }
            else { while (p < end && data[p] != 0) p++; p++; }
            if (p >= end || end - p > 2 * 1024 * 1024) return null;
            var picture = new byte[end - p]; Buffer.BlockCopy(data, p, picture, 0, picture.Length); return picture;
        }
        public static Tags Read(string path)
        {
            var result = new Tags();
            if (!string.Equals(Path.GetExtension(path), ".mp3", StringComparison.OrdinalIgnoreCase)) return result;
            try
            {
                using var stream = File.OpenRead(path);
                var header = new byte[10];
                if (stream.Read(header, 0, header.Length) != header.Length || header[0] != 'I' || header[1] != 'D' || header[2] != '3' ||
                    (header[3] != 3 && header[3] != 4) || header[5] != 0 || (header[6] | header[7] | header[8] | header[9]) >= 128) return result;
                int size = SyncSafe(header, 6);
                if (size <= 0 || size > 2 * 1024 * 1024 || size > stream.Length - 10) return result;
                var tag = new byte[size]; int read = 0;
                while (read < size) { int n = stream.Read(tag, read, size - read); if (n == 0) return result; read += n; }
                for (int p = 0; p + 10 <= size;)
                {
                    if (tag[p] == 0) break;
                    string id = Encoding.ASCII.GetString(tag, p, 4);
                    int length = header[3] == 4 ? SyncSafe(tag, p + 4) : (tag[p + 4] << 24) | (tag[p + 5] << 16) | (tag[p + 6] << 8) | tag[p + 7];
                    if (length <= 0 || length > size - p - 10) break;
                    if (tag[p + 8] == 0 && tag[p + 9] == 0)
                    {
                        if (id == "TIT2") result.title = TextFrame(tag, p + 10, length);
                        else if (id == "TPE1") result.artist = TextFrame(tag, p + 10, length);
                        else if (id == "APIC" && result.cover == null) result.cover = PictureFrame(tag, p + 10, length);
                    }
                    p += 10 + length;
                }
            }
            catch (Exception) { /* A broken tag must not prevent importing or playing audio. */ }
            return result;
        }
    }
}
