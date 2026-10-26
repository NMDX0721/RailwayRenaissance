using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    public sealed class TabletNotificationStorage
    {
        [Serializable]
        public sealed class StorageModel
        {
            public int schemaVersion = 1;
            public bool quietMode;
            public List<ItemModel> notifications = new List<ItemModel>();
        }

        [Serializable]
        public sealed class ItemModel
        {
            public long id;
            public string title;
            public string body;
            public string createdAt;
            public bool isRead;
        }

        public string FilePath { get; }
        public bool IsReadOnly { get; private set; }
        public string LastError { get; private set; }

        public TabletNotificationStorage(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("通知配置文件路径不能为空。", nameof(filePath));
            }
            FilePath = filePath;
        }

        public bool Load(out bool quietMode, out List<TabletNotification> notifications, out long nextId)
        {
            quietMode = false;
            notifications = new List<TabletNotification>();
            nextId = 1;
            LastError = null;
            IsReadOnly = false;

            if (!File.Exists(FilePath))
            {
                return true;
            }

            string rawJson;
            try
            {
                rawJson = File.ReadAllText(FilePath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LastError = $"读取通知存储失败，使用默认配置（原文件保持不变）：{ex.Message}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                LastError = "通知存储内容为空，已回退为默认配置（原文件保持不变）。";
                return false;
            }

            string trimmed = rawJson.Trim();
            if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}"))
            {
                LastError = "通知存储非有效 JSON 对象，已回退为默认配置（原文件保持不变）。";
                return false;
            }

            StorageModel data;
            try
            {
                data = JsonUtility.FromJson<StorageModel>(rawJson);
            }
            catch (Exception ex)
            {
                LastError = $"解析通知存储失败，已回退为默认配置（原文件保持不变）：{ex.Message}";
                return false;
            }

            if (data == null || !rawJson.Contains("\"schemaVersion\"") || data.schemaVersion <= 0)
            {
                LastError = "通知存储版本号缺失或无效，已回退为默认配置（原文件保持不变）。";
                return false;
            }

            if (data.schemaVersion > 1)
            {
                IsReadOnly = true;
                LastError = $"通知存储版本更高（v{data.schemaVersion}），当前客户端仅支持只读，已禁止覆盖写入。";
                quietMode = data.quietMode;
                ParseItems(data.notifications, notifications, ref nextId);
                return true;
            }

            quietMode = data.quietMode;
            ParseItems(data.notifications, notifications, ref nextId);
            return true;
        }

        private static void ParseItems(List<ItemModel> rawItems, List<TabletNotification> result, ref long nextId)
        {
            if (rawItems == null) return;
            long maxId = 0;
            int count = 0;
            for (int i = 0; i < rawItems.Count; i++)
            {
                if (count >= TabletShellState.MaxCapacity) break;
                var item = rawItems[i];
                if (item == null) continue;
                if (item.id > maxId) maxId = item.id;
                DateTime dt;
                if (!DateTime.TryParse(item.createdAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out dt))
                {
                    dt = DateTime.Now;
                }
                var notif = new TabletNotification(item.id, item.title, item.body, dt, item.isRead);
                result.Add(notif);
                count++;
            }
            nextId = Math.Max(nextId, maxId + 1);
        }

        public bool Save(bool quietMode, IReadOnlyList<TabletNotification> notifications)
        {
            if (IsReadOnly)
            {
                LastError = "当前通知存储为只读模式，无法覆盖写入。";
                return false;
            }

            var model = new StorageModel
            {
                schemaVersion = 1,
                quietMode = quietMode
            };

            if (notifications != null)
            {
                int limit = Math.Min(notifications.Count, TabletShellState.MaxCapacity);
                for (int i = 0; i < limit; i++)
                {
                    var n = notifications[i];
                    model.notifications.Add(new ItemModel
                    {
                        id = n.Id,
                        title = n.Title,
                        body = n.Body,
                        createdAt = n.CreatedAt.ToString("o"),
                        isRead = n.IsRead
                    });
                }
            }

            string tempPath = null;
            try
            {
                string dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string fileName = Path.GetFileName(FilePath);
                tempPath = Path.Combine(string.IsNullOrEmpty(dir) ? "." : dir, $"{fileName}.{Guid.NewGuid():N}.tmp");

                string json = JsonUtility.ToJson(model, true);
                File.WriteAllText(tempPath, json, Encoding.UTF8);

                if (File.Exists(FilePath))
                {
                    File.Replace(tempPath, FilePath, null);
                }
                else
                {
                    File.Move(tempPath, FilePath);
                }

                LastError = null;
                return true;
            }
            catch (Exception ex)
            {
                // Diagnostic warning WITHOUT logging notification content (protect user privacy)
                LastError = $"通知持久化保存失败（已保留原文件）：{ex.Message}";
                Debug.LogWarning($"[TabletNotificationStorage] Failed to save notifications: {ex.Message}");
                return false;
            }
            finally
            {
                if (tempPath != null && File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }
    }
}
