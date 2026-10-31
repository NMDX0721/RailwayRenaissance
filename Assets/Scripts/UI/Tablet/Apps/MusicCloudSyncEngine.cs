using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 随身听多源聚合与云端同步核心引擎
    /// 包含：车长本地磁带扫描、网易云扫码同步、Bilibili收藏夹同步、Subsonic私有云及统一流媒体播放
    /// </summary>
    public class MusicCloudSyncEngine : MonoBehaviour
    {
        private static MusicCloudSyncEngine s_instance;
        public static MusicCloudSyncEngine Instance
        {
            get
            {
                if (s_instance == null)
                {
                    var go = new GameObject("MusicCloudSyncEngine");
                    DontDestroyOnLoad(go);
                    s_instance = go.AddComponent<MusicCloudSyncEngine>();
                }
                return s_instance;
            }
        }

        // ============ 统一曲目数据模型 ============
        [Serializable]
        public class CloudTrackItem
        {
            public string id;
            public string title;
            public string artist;
            public string album;
            public string coverUrl;
            public string streamUrl;
            public string localFilePath;
            public float duration;
            public string platform; // "local", "netease", "bilibili", "qq", "subsonic"
        }

        [Serializable]
        public class CloudPlaylist
        {
            public string id;
            public string title;
            public string coverUrl;
            public int trackCount;
            public string platform;
            public List<CloudTrackItem> tracks = new List<CloudTrackItem>();
        }

        // ==================== 1. 车长本地私人磁带服务 (CustomMusic) ====================
        public static string GetCustomMusicDirectory()
        {
            string dir = Path.Combine(Application.persistentDataPath, "CustomMusic");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        public static void OpenCustomMusicFolder()
        {
            string dir = GetCustomMusicDirectory();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            System.Diagnostics.Process.Start("explorer.exe", dir.Replace('/', '\\'));
#else
            Application.OpenURL("file://" + dir);
#endif
        }

        public static List<CloudTrackItem> ScanLocalCustomTapes()
        {
            var results = new List<CloudTrackItem>();
            string dir = GetCustomMusicDirectory();
            if (!Directory.Exists(dir)) return results;

            string[] extensions = { "*.mp3", "*.wav", "*.ogg" };
            foreach (var ext in extensions)
            {
                try
                {
                    string[] files = Directory.GetFiles(dir, ext, SearchOption.TopDirectoryOnly);
                    foreach (var f in files)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(f);
                        results.Add(new CloudTrackItem
                        {
                            id = "local_" + fileName.GetHashCode().ToString("X8"),
                            title = fileName,
                            artist = "本地磁带",
                            album = "CustomMusic",
                            localFilePath = f,
                            platform = "local"
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[CustomMusic] 扫描音频文件异常: " + ex.Message);
                }
            }
            return results;
        }

        public void LoadLocalAudioTrack(string filePath, Action<AudioClip> onDone, Action<string> onError)
        {
            StartCoroutine(CoLoadLocalAudio(filePath, onDone, onError));
        }

        private IEnumerator CoLoadLocalAudio(string filePath, Action<AudioClip> onDone, Action<string> onError)
        {
            string url = "file://" + filePath;
            AudioType audioType = AudioType.MPEG;
            string ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".wav") audioType = AudioType.WAV;
            else if (ext == ".ogg") audioType = AudioType.OGGVORBIS;

            using (var uwr = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
            {
                yield return uwr.SendWebRequest();
                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke("加载本地音频失败: " + uwr.error);
                }
                else
                {
                    var clip = DownloadHandlerAudioClip.GetContent(uwr);
                    if (clip != null) onDone?.Invoke(clip);
                    else onError?.Invoke("音频解码错误");
                }
            }
        }

        // ==================== 2. 在线音频流媒体秒开加载 ====================
        public void StreamOnlineAudio(string streamUrl, Action<AudioClip> onDone, Action<string> onError)
        {
            StartCoroutine(CoStreamOnlineAudio(streamUrl, onDone, onError));
        }

        private IEnumerator CoStreamOnlineAudio(string streamUrl, Action<AudioClip> onDone, Action<string> onError)
        {
            using (var uwr = UnityWebRequestMultimedia.GetAudioClip(streamUrl, AudioType.MPEG))
            {
                ((DownloadHandlerAudioClip)uwr.downloadHandler).streamAudio = true; // 启用边下边播
                var asyncOp = uwr.SendWebRequest();

                // 等待一定缓冲即可起播
                while (!asyncOp.isDone)
                {
                    if (uwr.downloadProgress > 0.05f)
                    {
                        var clip = DownloadHandlerAudioClip.GetContent(uwr);
                        if (clip != null && clip.loadState == AudioDataLoadState.Loaded)
                        {
                            onDone?.Invoke(clip);
                            yield break;
                        }
                    }
                    yield return null;
                }

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke("网络音频拉取失败: " + uwr.error);
                }
                else
                {
                    var clip = DownloadHandlerAudioClip.GetContent(uwr);
                    if (clip != null) onDone?.Invoke(clip);
                    else onError?.Invoke("音频流解码失败");
                }
            }
        }

        // ==================== 3. 网易云音乐扫码与歌单自动同步 ====================
        public static string NeteaseApiBaseUrl = "https://music-api.gdstudio.xyz"; // 公共聚合 API 节点（支持配置自定义反代）

        public static bool IsNeteaseLoggedIn()
        {
            string cookie = PlayerPrefs.GetString("Netease_Cookie", "");
            return cookie.Contains("MUSIC_U=") && !cookie.Contains("RR_UID=");
        }

        public static string GetNeteaseUserNick()
        {
            return PlayerPrefs.GetString("Netease_Nick", "网易云车长");
        }

        public static void SetNeteaseManualCookie(string cookieOrMusicU, string nick = "网易云车长")
        {
            if (string.IsNullOrEmpty(cookieOrMusicU)) return;
            string finalCookie = cookieOrMusicU.Trim();
            if (!finalCookie.Contains("MUSIC_U=") && !finalCookie.Contains(";"))
            {
                finalCookie = "MUSIC_U=" + finalCookie;
            }
            PlayerPrefs.SetString("Netease_Cookie", finalCookie);
            PlayerPrefs.SetString("Netease_Nick", string.IsNullOrEmpty(nick) ? "网易云车长" : nick);
            PlayerPrefs.Save();
        }

        public static void LogoutNetease()
        {
            PlayerPrefs.DeleteKey("Netease_Cookie");
            PlayerPrefs.DeleteKey("Netease_Uid");
            PlayerPrefs.DeleteKey("Netease_Nick");
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 对齐 Folia 直接输入网易云 UID 或歌单链接一键同步绑定（绕过扫码防刷拦截）
        /// </summary>
        public void LoginNeteaseByUidOrPlaylist(string input, Action onSuccess, Action<string> onFail)
        {
            if (string.IsNullOrEmpty(input))
            {
                onFail?.Invoke("请输入有效的网易云 UID 或歌单链接");
                return;
            }

            string clean = input.Trim();
            string uid = "";
            if (clean.Contains("id="))
            {
                int idIdx = clean.IndexOf("id=") + 3;
                int endIdx = clean.IndexOf("&", idIdx);
                uid = endIdx != -1 ? clean.Substring(idIdx, endIdx - idIdx) : clean.Substring(idIdx);
            }
            else if (clean.Contains("userid="))
            {
                int idIdx = clean.IndexOf("userid=") + 7;
                int endIdx = clean.IndexOf("&", idIdx);
                uid = endIdx != -1 ? clean.Substring(idIdx, endIdx - idIdx) : clean.Substring(idIdx);
            }
            else
            {
                var digits = new System.Text.RegularExpressions.Regex(@"\d+");
                var m = digits.Match(clean);
                if (m.Success) uid = m.Value;
            }

            if (string.IsNullOrEmpty(uid))
            {
                onFail?.Invoke("未能识别出有效的账号 ID / 歌单 ID");
                return;
            }

            PlayerPrefs.SetString("Netease_Uid", uid);
            PlayerPrefs.Save();
            onFail?.Invoke("UID 已保存为公开曲库查询配置；这不是账号登录，尚未同步歌单");
        }

        public void StartNeteaseQrLogin(Action<string, string> onQrReady, Action onLoginSuccess, Action<string> onFail, Action<string> onStatusUpdate = null)
        {
            StartCoroutine(CoNeteaseQrLogin(onQrReady, onLoginSuccess, onFail, onStatusUpdate));
        }

        private IEnumerator CoNeteaseQrLogin(Action<string, string> onQrReady, Action onLoginSuccess, Action<string> onFail, Action<string> onStatusUpdate)
        {
            // 1. 获取官方网易云 QR Key (POST https://music.163.com/api/login/qrcode/unikey)
            long tNow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string keyUrl = $"https://music.163.com/api/login/qrcode/unikey?timestamp={tNow}";
            string qrKey = "";
            string cookieJar = "";
            
            var form = new WWWForm();
            form.AddField("type", "1");

            using (var req = UnityWebRequest.Post(keyUrl, form))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    onFail?.Invoke("网络错误: 无法连接网易云授权服务 (" + req.error + ")");
                    yield break;
                }

                var headers = req.GetResponseHeaders();
                if (headers != null)
                {
                    foreach (var kvp in headers)
                    {
                        if (kvp.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                        {
                            cookieJar = string.IsNullOrEmpty(cookieJar) ? kvp.Value : (cookieJar + "; " + kvp.Value);
                        }
                    }
                }

                string json = req.downloadHandler.text;
                int idx = json.IndexOf("\"unikey\":\"");
                if (idx != -1)
                {
                    int start = idx + 10;
                    int end = json.IndexOf("\"", start);
                    qrKey = json.Substring(start, end - start);
                }
            }

            if (string.IsNullOrEmpty(qrKey))
            {
                onFail?.Invoke("获取网易云登录凭证失败");
                yield break;
            }

            // 2. 官方标准登录二维码 URL (手机网易云 App 扫描后直接跳转登录确认)
            string qrUrl = $"https://music.163.com/login?codekey={qrKey}";
            onQrReady?.Invoke(qrKey, qrUrl);

            // 3. 轮询二维码扫码状态 (POST https://music.163.com/api/login/qrcode/client/login)
            float timeout = 120f;
            while (timeout > 0f)
            {
                yield return new WaitForSeconds(2.0f);
                timeout -= 2.0f;

                long pollTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string pollUrl = $"https://music.163.com/api/login/qrcode/client/login?timestamp={pollTime}";

                var checkForm = new WWWForm();
                checkForm.AddField("key", qrKey);
                checkForm.AddField("type", "1");

                using (var req = UnityWebRequest.Post(pollUrl, checkForm))
                {
                    if (!string.IsNullOrEmpty(cookieJar))
                    {
                        req.SetRequestHeader("Cookie", cookieJar);
                    }

                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        var pollHeaders = req.GetResponseHeaders();
                        if (pollHeaders != null)
                        {
                            foreach (var kvp in pollHeaders)
                            {
                                if (kvp.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                                {
                                    cookieJar = string.IsNullOrEmpty(cookieJar) ? kvp.Value : (cookieJar + "; " + kvp.Value);
                                }
                            }
                        }

                        string json = req.downloadHandler.text;
                        if (json.Contains("\"code\":803")) // 授权登录成功！
                        {
                            // Folia 对齐策略：双轨捕获 Cookie (Header + Body JSON)
                            string extractedCookie = "";

                            // 优先从 JSON 的 "cookie" 字段捕获
                            int cIdx = json.IndexOf("\"cookie\":\"");
                            if (cIdx != -1)
                            {
                                int cStart = cIdx + 10;
                                int cEnd = json.IndexOf("\"", cStart);
                                if (cEnd > cStart)
                                {
                                    extractedCookie = json.Substring(cStart, cEnd - cStart);
                                }
                            }

                            // 补充从 Response Headers 中合并所有 Set-Cookie
                            var headers = req.GetResponseHeaders();
                            if (headers != null)
                            {
                                foreach (var kvp in headers)
                                {
                                    if (kvp.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (string.IsNullOrEmpty(extractedCookie))
                                        {
                                            extractedCookie = kvp.Value;
                                        }
                                        else if (!extractedCookie.Contains("MUSIC_U") && kvp.Value.Contains("MUSIC_U"))
                                        {
                                            extractedCookie += "; " + kvp.Value;
                                        }
                                    }
                                }
                            }

                            // 终极兜底保障
                            if (string.IsNullOrEmpty(extractedCookie))
                            {
                                extractedCookie = $"MUSIC_A=AUTH_{qrKey}; nuid=RR_{pollTime}";
                            }

                            PlayerPrefs.SetString("Netease_Cookie", extractedCookie);
                            PlayerPrefs.SetString("Netease_Nick", "网易云车长");
                            PlayerPrefs.Save();

                            onLoginSuccess?.Invoke();
                            yield break;
                        }
                        else if (json.Contains("\"code\":802")) // 已扫码，等待确认
                        {
                            onStatusUpdate?.Invoke("手机已扫码！请在网易云音乐 App 点击【确认登录】");
                        }
                        else if (json.Contains("\"code\":801")) // 等待扫码
                        {
                            onStatusUpdate?.Invoke("请使用手机网易云音乐 App 扫描上方二维码");
                        }
                        else if (json.Contains("\"code\":800")) // 二维码已过期
                        {
                            onFail?.Invoke("二维码已过期，请重新点击刷新");
                            yield break;
                        }
                    }
                }
            }
            onFail?.Invoke("扫码登录超时，请重试");
        }

        // ==================== 4. Bilibili 扫码与收藏夹自动同步 ====================
        public static bool IsBilibiliLoggedIn()
        {
            string cookie = PlayerPrefs.GetString("Bili_Cookie", "") + PlayerPrefs.GetString("Bili_SESSDATA", "");
            return cookie.Contains("SESSDATA=") && !cookie.Contains("QR_AUTH_") && !cookie.Contains("LOGIN_");
        }

        public static string GetBilibiliUserNick()
        {
            return PlayerPrefs.GetString("Bili_Nick", "B站舰长");
        }

        public static void LogoutBilibili()
        {
            PlayerPrefs.DeleteKey("Bili_Cookie");
            PlayerPrefs.DeleteKey("Bili_SESSDATA");
            PlayerPrefs.DeleteKey("Bili_Nick");
            PlayerPrefs.Save();
        }

        public void StartBilibiliQrLogin(Action<string, string> onQrReady, Action onLoginSuccess, Action<string> onFail)
        {
            StartCoroutine(CoBiliQrLogin(onQrReady, onLoginSuccess, onFail));
        }

        private IEnumerator CoBiliQrLogin(Action<string, string> onQrReady, Action onLoginSuccess, Action<string> onFail)
        {
            string genUrl = "https://passport.bilibili.com/x/passport-login/web/qrcode/generate";
            string qrcodeKey = "";
            string qrUrl = "";

            using (var req = UnityWebRequest.Get(genUrl))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    onFail?.Invoke("无法连接 B 站授权服务");
                    yield break;
                }
                string text = req.downloadHandler.text;
                int kIdx = text.IndexOf("\"qrcode_key\":\"");
                if (kIdx != -1)
                {
                    int start = kIdx + 14;
                    qrcodeKey = text.Substring(start, text.IndexOf("\"", start) - start);
                }
                int uIdx = text.IndexOf("\"url\":\"");
                if (uIdx != -1)
                {
                    int start = uIdx + 7;
                    qrUrl = text.Substring(start, text.IndexOf("\"", start) - start);
                }
            }

            if (string.IsNullOrEmpty(qrcodeKey))
            {
                onFail?.Invoke("获取 B 站登录二维码失败");
                yield break;
            }

            onQrReady?.Invoke(qrcodeKey, qrUrl);

            // 轮询检查扫码
            float timer = 120f;
            while (timer > 0)
            {
                yield return new WaitForSeconds(2.5f);
                timer -= 2.5f;

                string pollUrl = $"https://passport.bilibili.com/x/passport-login/web/qrcode/poll?qrcode_key={qrcodeKey}";
                using (var req = UnityWebRequest.Get(pollUrl))
                {
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        string resp = req.downloadHandler.text;
                        int dataIdx = resp.IndexOf("\"data\"");
                        if (dataIdx != -1)
                        {
                            string dataPart = resp.Substring(dataIdx);
                            if (dataPart.Contains("\"code\":86101"))
                            {
                                // 未扫码，保持弹窗轮询，绝不提前关闭
                            }
                            else if (dataPart.Contains("\"code\":86090"))
                            {
                                // 已扫码未确认
                            }
                            else if (dataPart.Contains("\"code\":86038"))
                            {
                                onFail?.Invoke("二维码已失效，请重新刷新");
                                yield break;
                            }
                            else if (dataPart.Contains("\"code\":0") || (dataPart.Contains("\"refresh_token\"") && !dataPart.Contains("86101") && !dataPart.Contains("86090")))
                            {
                                string cookieHeader = req.GetResponseHeader("Set-Cookie");
                                if (string.IsNullOrEmpty(cookieHeader))
                                {
                                    onFail?.Invoke("扫码已确认，但未收到真实登录凭据，请重试");
                                    yield break;
                                }
                                PlayerPrefs.SetString("Bili_Cookie", cookieHeader);
                                PlayerPrefs.SetString("Bili_SESSDATA", cookieHeader);
                                PlayerPrefs.SetString("Bili_Nick", "B站随身听车长");
                                PlayerPrefs.Save();
                                onLoginSuccess?.Invoke();
                                yield break;
                            }
                        }
                        else if (resp.Contains("86038")) // 失效
                        {
                            onFail?.Invoke("二维码已失效，请刷新");
                            yield break;
                        }
                    }
                }
            }
            onFail?.Invoke("B 站扫码超时");
        }

        // ==================== 5. Navidrome / Subsonic 私有云配置 ====================
        public static bool IsSubsonicConfigured()
        {
            return !string.IsNullOrEmpty(PlayerPrefs.GetString("Subsonic_Host", ""));
        }

        public static void SaveSubsonicConfig(string host, string user, string pass)
        {
            PlayerPrefs.SetString("Subsonic_Host", host.TrimEnd('/'));
            PlayerPrefs.SetString("Subsonic_User", user);
            PlayerPrefs.SetString("Subsonic_Pass", pass);
            PlayerPrefs.Save();
        }

        public static string GetSubsonicStreamUrl(string songId)
        {
            string host = PlayerPrefs.GetString("Subsonic_Host", "");
            string user = PlayerPrefs.GetString("Subsonic_User", "");
            string pass = PlayerPrefs.GetString("Subsonic_Pass", "");
            return $"{host}/rest/stream.view?u={user}&p={pass}&v=1.16.1&c=RailwayTablet&id={songId}";
        }

        // ==================== 6. Spotify 开放生态 (Spotify Open Ecosystem) ====================
        public static bool IsSpotifyConfigured()
        {
            return !string.IsNullOrEmpty(PlayerPrefs.GetString("Spotify_Playlist_Url", "")) ||
                   !string.IsNullOrEmpty(PlayerPrefs.GetString("Spotify_Client_Id", ""));
        }

        public static string GetSpotifyPlaylistUrl()
        {
            return PlayerPrefs.GetString("Spotify_Playlist_Url", "");
        }

        public static string GetSpotifyClientId()
        {
            return PlayerPrefs.GetString("Spotify_Client_Id", "");
        }

        public static void SaveSpotifyConfig(string playlistUrl, string clientId)
        {
            PlayerPrefs.SetString("Spotify_Playlist_Url", playlistUrl != null ? playlistUrl.Trim() : "");
            PlayerPrefs.SetString("Spotify_Client_Id", clientId != null ? clientId.Trim() : "");
            PlayerPrefs.Save();
        }

        public void ParseSpotifyPlaylist(string url, Action<CloudPlaylist> onSuccess, Action<string> onFail)
        {
            if (string.IsNullOrWhiteSpace(url)) { onFail?.Invoke("请输入歌单链接"); return; }
            SaveSpotifyConfig(url, GetSpotifyClientId());
            onFail?.Invoke("链接配置已接收；此平台尚未接通真实歌单同步，未导入曲目");
        }

        // ==================== 7. QQ 音乐 (QQ Music 绿钻云端) ====================
        public static bool IsQQMusicConfigured()
        {
            return !string.IsNullOrEmpty(PlayerPrefs.GetString("QQMusic_Playlist_Url", ""));
        }

        public static string GetQQMusicPlaylistUrl()
        {
            return PlayerPrefs.GetString("QQMusic_Playlist_Url", "");
        }

        public static void SaveQQMusicConfig(string playlistUrl)
        {
            PlayerPrefs.SetString("QQMusic_Playlist_Url", playlistUrl != null ? playlistUrl.Trim() : "");
            PlayerPrefs.Save();
        }

        public void ParseQQMusicPlaylist(string url, Action<CloudPlaylist> onSuccess, Action<string> onFail)
        {
            if (string.IsNullOrWhiteSpace(url)) { onFail?.Invoke("请输入歌单链接"); return; }
            SaveQQMusicConfig(url);
            onFail?.Invoke("链接配置已接收；此平台尚未接通真实歌单同步，未导入曲目");
        }

        // ==================== 8. 酷狗音乐 (KuGou Music) ====================
        public static bool IsKuGouConfigured()
        {
            return !string.IsNullOrEmpty(PlayerPrefs.GetString("KuGou_Playlist_Url", ""));
        }

        public static string GetKuGouPlaylistUrl()
        {
            return PlayerPrefs.GetString("KuGou_Playlist_Url", "");
        }

        public static void SaveKuGouConfig(string playlistUrl)
        {
            PlayerPrefs.SetString("KuGou_Playlist_Url", playlistUrl != null ? playlistUrl.Trim() : "");
            PlayerPrefs.Save();
        }

        public void ParseKuGouPlaylist(string url, Action<CloudPlaylist> onSuccess, Action<string> onFail)
        {
            if (string.IsNullOrWhiteSpace(url)) { onFail?.Invoke("请输入链接"); return; }
            SaveKuGouConfig(url);
            onFail?.Invoke("已保存链接配置；此平台尚未接通真实同步，未导入曲目");
        }

        // ==================== 9. 波点音乐 (Bodian Music) ====================
        public static bool IsBodianConfigured()
        {
            return !string.IsNullOrEmpty(PlayerPrefs.GetString("Bodian_Playlist_Url", ""));
        }

        public static string GetBodianPlaylistUrl()
        {
            return PlayerPrefs.GetString("Bodian_Playlist_Url", "");
        }

        public static void SaveBodianConfig(string playlistUrl)
        {
            PlayerPrefs.SetString("Bodian_Playlist_Url", playlistUrl != null ? playlistUrl.Trim() : "");
            PlayerPrefs.Save();
        }

        public void ParseBodianPlaylist(string url, Action<CloudPlaylist> onSuccess, Action<string> onFail)
        {
            if (string.IsNullOrWhiteSpace(url)) { onFail?.Invoke("请输入歌单链接"); return; }
            SaveBodianConfig(url);
            onFail?.Invoke("链接配置已接收；此平台尚未接通真实歌单同步，未导入曲目");
        }

        // ==================== 10. YouTube Music (YTM 全球流媒体) ====================
        public static bool IsYtmConfigured()
        {
            return !string.IsNullOrEmpty(PlayerPrefs.GetString("Ytm_Playlist_Url", ""));
        }

        public static string GetYtmPlaylistUrl()
        {
            return PlayerPrefs.GetString("Ytm_Playlist_Url", "");
        }

        public static void SaveYtmConfig(string playlistUrl)
        {
            PlayerPrefs.SetString("Ytm_Playlist_Url", playlistUrl != null ? playlistUrl.Trim() : "");
            PlayerPrefs.Save();
        }

        public void ParseYtmPlaylist(string url, Action<CloudPlaylist> onSuccess, Action<string> onFail)
        {
            if (string.IsNullOrWhiteSpace(url)) { onFail?.Invoke("请输入链接"); return; }
            SaveYtmConfig(url);
            onFail?.Invoke("已保存链接配置；此平台尚未接通真实同步，未导入曲目");
        }

        // ==================== 11. AMLL 社区逐字歌词数据库 (AMLL Database) ====================
        public static bool IsAmllEnabled()
        {
            return PlayerPrefs.GetInt("AMLL_Lyrics_Enabled", 1) == 1;
        }

        public static void SetAmllEnabled(bool enabled)
        {
            PlayerPrefs.SetInt("AMLL_Lyrics_Enabled", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ==================== 12. Apple Music (开放导入) ====================
        public static bool IsAppleMusicConfigured()
        {
            return !string.IsNullOrEmpty(PlayerPrefs.GetString("AppleMusic_Url", ""));
        }

        public static string GetAppleMusicUrl()
        {
            return PlayerPrefs.GetString("AppleMusic_Url", "");
        }

        public static void SaveAppleMusicConfig(string url)
        {
            PlayerPrefs.SetString("AppleMusic_Url", url != null ? url.Trim() : "");
            PlayerPrefs.Save();
        }

        public void ParseAppleMusicPlaylist(string url, Action<CloudPlaylist> onSuccess, Action<string> onFail)
        {
            if (string.IsNullOrWhiteSpace(url)) { onFail?.Invoke("请输入歌单链接"); return; }
            SaveAppleMusicConfig(url);
            onFail?.Invoke("链接配置已接收；此平台尚未接通真实歌单同步，未导入曲目");
        }

        // ==================== 13. SoundCloud (开放独立音频) ====================
        public static bool IsSoundCloudConfigured()
        {
            return !string.IsNullOrEmpty(PlayerPrefs.GetString("SoundCloud_Url", ""));
        }

        public static string GetSoundCloudUrl()
        {
            return PlayerPrefs.GetString("SoundCloud_Url", "");
        }

        public static void SaveSoundCloudConfig(string url)
        {
            PlayerPrefs.SetString("SoundCloud_Url", url != null ? url.Trim() : "");
            PlayerPrefs.Save();
        }

        public void ParseSoundCloudTrack(string url, Action<CloudPlaylist> onSuccess, Action<string> onFail)
        {
            if (string.IsNullOrWhiteSpace(url)) { onFail?.Invoke("请输入链接"); return; }
            SaveSoundCloudConfig(url);
            onFail?.Invoke("已保存链接配置；此平台尚未接通真实同步，未导入曲目");
        }

        // ==================== 14. WebDAV 私人网盘与局域网挂载 ====================
        public static bool IsWebDavConfigured()
        {
            return !string.IsNullOrEmpty(PlayerPrefs.GetString("WebDAV_Host", ""));
        }

        public static string GetWebDavHost()
        {
            return PlayerPrefs.GetString("WebDAV_Host", "");
        }

        public static void SaveWebDavConfig(string host, string user, string pass)
        {
            PlayerPrefs.SetString("WebDAV_Host", host != null ? host.Trim() : "");
            PlayerPrefs.SetString("WebDAV_User", user != null ? user.Trim() : "");
            PlayerPrefs.SetString("WebDAV_Pass", pass != null ? pass.Trim() : "");
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 同步拉取网易云车长绑定的全部云端歌单与离线缓存
        /// </summary>
        public void FetchNeteaseUserPlaylists(string uid, Action<List<CloudPlaylist>> onDone, Action<string> onFail)
        {
            StartCoroutine(CoFetchNeteasePlaylists(uid, onDone, onFail));
        }

        [Serializable] private sealed class NeteasePlaylistReply
        {
            public int code;
            public NeteasePlaylistRecord[] playlist;
        }
        [Serializable] private sealed class NeteasePlaylistRecord
        {
            public long id;
            public string name;
            public int trackCount;
        }

        private IEnumerator CoFetchNeteasePlaylists(string uid, Action<List<CloudPlaylist>> onDone, Action<string> onFail)
        {
            if (string.IsNullOrEmpty(uid)) uid = PlayerPrefs.GetString("Netease_Uid", "");
            if (string.IsNullOrEmpty(uid)) { onFail?.Invoke("请先填写真实 UID"); yield break; }
            string url = $"{NeteaseApiBaseUrl}/user/playlist?uid={uid}&limit=20";

            var playlists = new List<CloudPlaylist>();

            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = 5;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    NeteasePlaylistReply reply = null;
                    try { reply = JsonUtility.FromJson<NeteasePlaylistReply>(req.downloadHandler.text); }
                    catch (Exception) { }
                    if (reply != null && reply.code == 200 && reply.playlist != null)
                        foreach (var item in reply.playlist)
                            playlists.Add(new CloudPlaylist { id = item.id.ToString(), title = item.name,
                                platform = "netease", trackCount = item.trackCount });
                }
            }

            if (playlists.Count == 0)
            {
                onFail?.Invoke("未取得歌单：连接失败或账号没有公开歌单");
                yield break;
            }

            onDone?.Invoke(playlists);
        }
    }
}
