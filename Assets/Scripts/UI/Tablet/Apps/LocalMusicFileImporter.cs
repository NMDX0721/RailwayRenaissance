using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 本地音频文件导入与资源管理器桥接器
    /// 支持 Windows 原生多选文件对话框呼出，自动将选中的音频拷贝至 CustomMusic 目录
    /// </summary>
    public static class LocalMusicFileImporter
    {
#if UNITY_EDITOR
        public static string ReviewDirectory;
#endif
        public static string GetCustomMusicDirectory()
        {
            string dir = Path.Combine(Application.persistentDataPath, "CustomMusic");
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(ReviewDirectory)) dir = ReviewDirectory;
#endif
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        public static void OpenMusicFolderInExplorer()
        {
            string dir = GetCustomMusicDirectory();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", dir.Replace('/', '\\'));
                return;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LocalMusicImporter] 打开文件夹失败: " + ex.Message);
            }
#endif
            Application.OpenURL("file://" + dir);
        }

        public static void OpenCustomMusicFolder() => OpenMusicFolderInExplorer();

        public class LocalMusicTrack
        {
            public string id;
            public string title;
            public string filePath;
        }

        public static List<LocalMusicTrack> GetLocalMusicTracks()
        {
            var list = new List<LocalMusicTrack>();
            try
            {
                string dir = GetCustomMusicDirectory();
                if (!Directory.Exists(dir)) return list;

                var files = Directory.GetFiles(dir);
                foreach (var f in files)
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext == ".mp3" || ext == ".wav" || ext == ".ogg")
                    {
                        string fname = Path.GetFileNameWithoutExtension(f);
                        list.Add(new LocalMusicTrack
                        {
                            id = "local_" + fname,
                            title = fname,
                            filePath = f
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LocalMusicImporter] 获取本地音频列表失败: " + ex.Message);
            }
            return list;
        }

        public static void LoadAudioClipAsync(string filePath, Action<AudioClip> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                onError?.Invoke("本地音频文件不存在: " + filePath);
                return;
            }

            AudioType audioType = AudioType.UNKNOWN;
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".mp3") audioType = AudioType.MPEG;
            else if (ext == ".wav") audioType = AudioType.WAV;
            else if (ext == ".ogg") audioType = AudioType.OGGVORBIS;

            string uri = new Uri(Path.GetFullPath(filePath)).AbsoluteUri;
            var req = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
            var op = req.SendWebRequest();
            op.completed += _ =>
            {
                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(req);
                    if (clip != null)
                    {
                        onSuccess?.Invoke(clip);
                    }
                    else
                    {
                        onError?.Invoke("音频解码失败");
                    }
                }
                else
                {
                    onError?.Invoke("加载音频失败: " + req.error);
                }
                req.Dispose();
            };
        }

        /// <summary>
        /// 呼出系统原生文件选择对话框并自动拷贝文件到对应目录
        /// </summary>
        /// <param name="onImportFinished">参数为成功导入的文件总数</param>
        public static void PromptImportAudioFiles(Action<int> onImportFinished)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                var selectedFiles = ShowWin32OpenFileDialog();
                if (selectedFiles == null || selectedFiles.Count == 0)
                {
                    onImportFinished?.Invoke(0);
                    return;
                }

                onImportFinished?.Invoke(ImportAudioFiles(selectedFiles));
            }
            catch (Exception ex)
            {
                Debug.LogError("[LocalMusicImporter] 对话框唤出异常: " + ex);
                onImportFinished?.Invoke(0);
            }
#else
            OpenMusicFolderInExplorer();
            onImportFinished?.Invoke(0);
#endif
        }

        public static void PromptImportLyrics(Action<string> selected)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                var files = ShowWin32OpenFileDialog(true);
                if (files.Count > 0) selected?.Invoke(files[0]);
            }
            catch (Exception ex) { TabletMusicAppUI.ShowToast("无法打开字幕选择窗口：" + ex.Message); }
#else
            TabletMusicAppUI.ShowToast("此版本的字幕文件选择仅支持 Windows");
#endif
        }

        public static int ImportAudioFiles(IEnumerable<string> sources)
        {
            int imported = 0;
            if (sources == null) return imported;
            string directory = GetCustomMusicDirectory();
            foreach (string source in sources)
            {
                if (string.IsNullOrEmpty(source) || !File.Exists(source)) continue;
                string extension = Path.GetExtension(source).ToLowerInvariant();
                if (extension != ".mp3" && extension != ".wav" && extension != ".ogg") continue;
                string destination = Path.Combine(directory, Path.GetFileName(source));
                try
                {
                    if (!string.Equals(Path.GetFullPath(source), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
                        File.Copy(source, destination, true);
                    string sourceStem = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(source)), Path.GetFileNameWithoutExtension(source));
                    string destinationStem = Path.Combine(directory, Path.GetFileNameWithoutExtension(source));
                    foreach (string subtitle in new[] { ".lrc", ".vtt", ".srt", ".json", ".t.lrc" })
                    {
                        string companion = sourceStem + subtitle;
                        if (File.Exists(companion) && new FileInfo(companion).Length <= MusicTimedText.MaxBytes &&
                            !string.Equals(Path.GetFullPath(companion), Path.GetFullPath(destinationStem + subtitle), StringComparison.OrdinalIgnoreCase))
                            File.Copy(companion, destinationStem + subtitle, true);
                    }
                    imported++;
                }
                catch (Exception ex) { Debug.LogWarning("[LocalMusicImporter] 文件未导入：" + ex.Message); }
            }
            return imported;
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class OpenFileName
        {
            public int structSize = 0;
            public IntPtr dlgOwner = IntPtr.Zero;
            public IntPtr instance = IntPtr.Zero;
            public string filter = null;
            public string customFilter = null;
            public int maxCustFilter = 0;
            public int filterIndex = 0;
            public IntPtr file = IntPtr.Zero;
            public int maxFile = 0;
            public string fileTitle = null;
            public int maxFileTitle = 0;
            public string initialDir = null;
            public string title = null;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public string defExt = null;
            public IntPtr custData = IntPtr.Zero;
            public IntPtr hook = IntPtr.Zero;
            public string templateName = null;
            public IntPtr reservedPtr = IntPtr.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;
        }

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        private const int OFN_EXPLORER = 0x00080000;
        private const int OFN_FILEMUSTEXIST = 0x00001000;
        private const int OFN_PATHMUSTEXIST = 0x00000800;
        private const int OFN_ALLOWMULTISELECT = 0x00000200;
        private const int OFN_NOCHANGEDIR = 0x00000008;

        private static List<string> ShowWin32OpenFileDialog(bool lyrics = false)
        {
            var results = new List<string>();
            const int bufferSize = 65536; // 64KB 支撑超多选
            IntPtr bufferPtr = Marshal.AllocHGlobal(bufferSize * Marshal.SystemDefaultCharSize);

            try
            {
                // 清空缓冲区内存
                for (int i = 0; i < bufferSize; i++)
                {
                    Marshal.WriteInt16(bufferPtr, i * 2, 0);
                }

                var ofn = new OpenFileName();
                ofn.structSize = Marshal.SizeOf(ofn);
                // 筛选器：格式用 \0 分割，末尾双 \0
                ofn.filter = "支持的音频文件 (*.mp3;*.wav;*.ogg)\0*.mp3;*.wav;*.ogg\0MP3 音乐 (*.mp3)\0*.mp3\0WAV 无损 (*.wav)\0*.wav\0OGG 音频 (*.ogg)\0*.ogg\0所有文件 (*.*)\0*.*\0\0";
                if (lyrics) ofn.filter = "歌词与字幕 (*.lrc;*.srt;*.vtt;*.json)\0*.lrc;*.srt;*.vtt;*.json\0\0";
                ofn.file = bufferPtr;
                ofn.maxFile = bufferSize;
                ofn.fileTitle = new string(new char[64]);
                ofn.maxFileTitle = ofn.fileTitle.Length;
                ofn.initialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                ofn.title = "选择要导入随身听的音频文件（支持按住 Ctrl 多选）";
                ofn.flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_ALLOWMULTISELECT | OFN_NOCHANGEDIR;
                if (lyrics) { ofn.title = "选择歌词或字幕（LRC / SRT / WebVTT / 逐词 JSON）"; ofn.flags &= ~OFN_ALLOWMULTISELECT; }

                if (GetOpenFileName(ofn))
                {
                    // 解析 null-terminated string 数组
                    var strList = new List<string>();
                    IntPtr current = bufferPtr;
                    while (true)
                    {
                        string s = Marshal.PtrToStringAuto(current);
                        if (string.IsNullOrEmpty(s)) break;
                        strList.Add(s);
                        current = (IntPtr)(current.ToInt64() + (s.Length + 1) * Marshal.SystemDefaultCharSize);
                    }

                    if (strList.Count == 1)
                    {
                        // 仅选中单文件
                        results.Add(strList[0]);
                    }
                    else if (strList.Count > 1)
                    {
                        // 多选：第一项是所在目录，后续是文件名
                        string dir = strList[0];
                        for (int i = 1; i < strList.Count; i++)
                        {
                            results.Add(Path.Combine(dir, strList[i]));
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }

            return results;
        }
#endif
    }
}
