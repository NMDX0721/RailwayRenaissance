using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Looks for audio files in the usual places on a background thread. It only lists files: nothing is read,
/// copied or sent anywhere until the player asks to import what was found. It can be stopped at any time.
/// </summary>
public sealed class LocalMusicScanner
{
    public static LocalMusicScanner Current;
    readonly List<string> found = new List<string>();
    readonly object gate = new object();
    volatile bool cancelled, running = true;
    int folders;

    public bool Running => running;
    public bool Cancelled => cancelled;
    public int Folders => Volatile.Read(ref folders);
    public int FoundCount { get { lock (gate) return found.Count; } }
    public List<string> Snapshot() { lock (gate) return new List<string>(found); }
    public void Cancel() => cancelled = true;

    static readonly HashSet<string> SkipNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "windows", "program files", "program files (x86)", "programdata", "$recycle.bin", "system volume information", "appdata", "node_modules", "library", "steamapps", "temp", "cache", "caches", "samples" };

    public static List<string> DefaultRoots()
    {
        var roots = new List<string>();
        void Add(string path) { if (!string.IsNullOrEmpty(path) && Directory.Exists(path) && !roots.Contains(path)) roots.Add(path); }
        Add(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(profile)) { Add(Path.Combine(profile, "Downloads")); Add(Path.Combine(profile, "Desktop")); }
        Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Fixed || !drive.IsReady) continue;
                foreach (string name in new[] { "Music", "音乐", "CloudMusic", "KuGou", "Downloads" }) Add(Path.Combine(drive.RootDirectory.FullName, name));
            }
        }
        catch (Exception) { /* drives are optional */ }
        return roots;
    }

    /// <summary>Starts a scan. Must be called on the main thread (the library folder is read here).</summary>
    public static LocalMusicScanner Begin(List<string> roots, string libraryDirectory)
    {
        var known = new HashSet<string>();
        try
        {
            if (Directory.Exists(libraryDirectory))
                foreach (string file in Directory.EnumerateFiles(libraryDirectory)) known.Add(Path.GetFileName(file).ToLowerInvariant() + "|" + new FileInfo(file).Length);
        }
        catch (Exception) { /* nothing to exclude */ }
        var scanner = new LocalMusicScanner();
        Current = scanner;
        Task.Run(() => scanner.Run(roots, libraryDirectory, known));
        return scanner;
    }

    void Run(List<string> roots, string libraryDirectory, HashSet<string> known)
    {
        try
        {
            var stack = new Stack<KeyValuePair<string, int>>();
            foreach (string root in roots) stack.Push(new KeyValuePair<string, int>(root, 0));
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string skip = libraryDirectory == null ? null : libraryDirectory.TrimEnd('\\', '/');
            while (stack.Count > 0 && !cancelled)
            {
                var entry = stack.Pop();
                string folder;
                try { folder = Path.GetFullPath(entry.Key).TrimEnd('\\', '/'); } catch (Exception) { continue; }
                if (!seen.Add(folder) || (skip != null && string.Equals(folder, skip, StringComparison.OrdinalIgnoreCase))) continue;
                Interlocked.Increment(ref folders);
                try
                {
                    foreach (string file in Directory.EnumerateFiles(folder))
                    {
                        if (cancelled) break;
                        string extension = Path.GetExtension(file).ToLowerInvariant();
                        if (extension != ".mp3" && extension != ".wav" && extension != ".ogg") continue;
                        long size;
                        try { size = new FileInfo(file).Length; } catch (Exception) { continue; }
                        // Short files are interface sounds and clips, not music.
                        if (size < 256 * 1024 || size > 1024L * 1024 * 1024) continue;
                        if (known.Contains(Path.GetFileName(file).ToLowerInvariant() + "|" + size)) continue;
                        lock (gate) { if (found.Count < 5000) found.Add(file); }
                    }
                    if (entry.Value >= 8) continue;
                    foreach (string sub in Directory.EnumerateDirectories(folder))
                    {
                        if (cancelled) break;
                        string name = Path.GetFileName(sub);
                        if (name.StartsWith(".") || SkipNames.Contains(name)) continue;
                        try { if ((File.GetAttributes(sub) & (FileAttributes.ReparsePoint | FileAttributes.System | FileAttributes.Hidden)) != 0) continue; }
                        catch (Exception) { continue; }
                        stack.Push(new KeyValuePair<string, int>(sub, entry.Value + 1));
                    }
                }
                catch (Exception) { /* unreadable folder */ }
                Thread.Sleep(0);
            }
        }
        finally { running = false; }
    }
}
