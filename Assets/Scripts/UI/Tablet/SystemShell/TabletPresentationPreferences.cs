using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    public enum TabletAccent
    {
        Amber,
        Teal,
        Blue
    }

    public sealed class TabletPresentationPreferences
    {
        [Serializable]
        private sealed class StorageModel
        {
            public int schemaVersion;
            public bool reduceMotion;
            public bool highContrast;
            public TabletAccent accent;
            public float wallpaperDim;
        }

        private readonly string _filePath;
        private bool _hasValidSavedFile;

        public bool ReduceMotion { get; private set; }
        public bool HighContrast { get; private set; }
        public bool IsReadOnly { get; private set; }
        public TabletAccent Accent { get; private set; }
        public float WallpaperDim { get; private set; }
        public string LastError { get; private set; }

        public event Action Changed;

        public TabletPresentationPreferences(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("配置文件路径不能为空。", nameof(filePath));
            }

            _filePath = filePath;
            ReduceMotion = false;
            HighContrast = false;
            Accent = TabletAccent.Amber;
            WallpaperDim = 0f;
            IsReadOnly = false;
            LastError = null;
            _hasValidSavedFile = false;

            if (!File.Exists(_filePath))
            {
                return;
            }

            string rawJson;
            try
            {
                rawJson = File.ReadAllText(_filePath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LastError = $"读取配置文件失败，使用默认配置（原文件保持不变）：{ex.Message}";
                return;
            }

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                LastError = "配置文件内容为空，已回退为默认配置（原文件保持不变）。";
                return;
            }

            string trimmed = rawJson.Trim();
            if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}"))
            {
                LastError = "配置文件非有效 JSON 对象，已回退为默认配置（原文件保持不变）。";
                return;
            }

            StorageModel data;
            try
            {
                data = JsonUtility.FromJson<StorageModel>(rawJson);
            }
            catch (Exception ex)
            {
                LastError = $"解析配置文件失败，已回退为默认配置（原文件保持不变）：{ex.Message}";
                return;
            }

            if (data == null || !rawJson.Contains("\"schemaVersion\"") || data.schemaVersion <= 0)
            {
                LastError = "配置文件版本号缺失或无效，已回退为默认配置（原文件保持不变）。";
                return;
            }

            if (data.schemaVersion > 1)
            {
                IsReadOnly = true;
                LastError = $"配置版本更高（v{data.schemaVersion}），当前客户端仅支持只读，已禁止覆盖写入。";
                ReduceMotion = data.reduceMotion;
                HighContrast = data.highContrast;
                if (Enum.IsDefined(typeof(TabletAccent), data.accent))
                {
                    Accent = data.accent;
                }
                if (!float.IsNaN(data.wallpaperDim) && !float.IsInfinity(data.wallpaperDim))
                {
                    WallpaperDim = data.wallpaperDim < 0f ? 0f : (data.wallpaperDim > 0.4f ? 0.4f : data.wallpaperDim);
                }
                _hasValidSavedFile = true;
                return;
            }

            ReduceMotion = data.reduceMotion;
            HighContrast = data.highContrast;

            bool hasFieldWarning = false;
            if (Enum.IsDefined(typeof(TabletAccent), data.accent))
            {
                Accent = data.accent;
            }
            else
            {
                Accent = TabletAccent.Amber;
                hasFieldWarning = true;
            }

            if (!float.IsNaN(data.wallpaperDim) && !float.IsInfinity(data.wallpaperDim) && data.wallpaperDim >= 0f && data.wallpaperDim <= 0.4f)
            {
                WallpaperDim = data.wallpaperDim;
            }
            else
            {
                WallpaperDim = 0f;
                hasFieldWarning = true;
            }

            if (hasFieldWarning)
            {
                LastError = "配置文件中部分字段取值无效，已恢复为默认值；原文件保留至下次保存。";
                _hasValidSavedFile = false;
            }
            else
            {
                _hasValidSavedFile = true;
            }
        }

        public void SetReduceMotion(bool value)
        {
            if (IsReadOnly)
            {
                LastError = "当前配置为只读模式，无法修改。";
                Changed?.Invoke();
                return;
            }
            if (ReduceMotion == value)
            {
                return;
            }
            ReduceMotion = value;
            TrySave();
        }

        public void SetHighContrast(bool value)
        {
            if (IsReadOnly)
            {
                LastError = "当前配置为只读模式，无法修改。";
                Changed?.Invoke();
                return;
            }
            if (HighContrast == value)
            {
                return;
            }
            HighContrast = value;
            TrySave();
        }

        public void SetAccent(TabletAccent value)
        {
            if (IsReadOnly)
            {
                LastError = "当前配置为只读模式，无法修改。";
                Changed?.Invoke();
                return;
            }
            if (!Enum.IsDefined(typeof(TabletAccent), value))
            {
                LastError = "无效的强调色选项。";
                Changed?.Invoke();
                return;
            }
            if (Accent == value)
            {
                return;
            }
            Accent = value;
            TrySave();
        }

        public void SetWallpaperDim(float value)
        {
            if (IsReadOnly)
            {
                LastError = "当前配置为只读模式，无法修改。";
                Changed?.Invoke();
                return;
            }
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                LastError = "壁纸暗角比例不能为 NaN 或无穷大。";
                Changed?.Invoke();
                return;
            }
            float clamped = value < 0f ? 0f : (value > 0.4f ? 0.4f : value);
            if (Math.Abs(WallpaperDim - clamped) < 0.0001f)
            {
                return;
            }
            WallpaperDim = clamped;
            TrySave();
        }

        public void ResetToDefaults()
        {
            if (IsReadOnly)
            {
                LastError = "当前配置为只读模式，无法重置。";
                Changed?.Invoke();
                return;
            }

            bool isAlreadyDefault = !ReduceMotion && !HighContrast && Accent == TabletAccent.Amber && Math.Abs(WallpaperDim) < 0.0001f;
            if (isAlreadyDefault && _hasValidSavedFile && string.IsNullOrEmpty(LastError))
            {
                return;
            }

            ReduceMotion = false;
            HighContrast = false;
            Accent = TabletAccent.Amber;
            WallpaperDim = 0f;
            TrySave();
        }

        private void TrySave()
        {
            var model = new StorageModel
            {
                schemaVersion = 1,
                reduceMotion = ReduceMotion,
                highContrast = HighContrast,
                accent = Accent,
                wallpaperDim = WallpaperDim
            };

            string tempPath = null;
            try
            {
                string dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string fileName = Path.GetFileName(_filePath);
                tempPath = Path.Combine(string.IsNullOrEmpty(dir) ? "." : dir, $"{fileName}.{Guid.NewGuid():N}.tmp");

                string json = JsonUtility.ToJson(model, true);
                File.WriteAllText(tempPath, json, Encoding.UTF8);

                if (File.Exists(_filePath))
                {
                    File.Replace(tempPath, _filePath, null);
                }
                else
                {
                    File.Move(tempPath, _filePath);
                }

                _hasValidSavedFile = true;
                LastError = null;
            }
            catch (Exception ex)
            {
                _hasValidSavedFile = false;
                LastError = $"设置仅在当前会话生效，持久化保存失败（已保留原文件）：{ex.Message}";
            }
            finally
            {
                if (tempPath != null && File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
                Changed?.Invoke();
            }
        }
    }
}
