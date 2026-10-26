using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    public interface IKeyValueStorage
    {
        bool HasKey(string key);
        string GetString(string key, string defaultValue = "");
        void SetString(string key, string value);
        void DeleteKey(string key);
        void Save();
    }

    public sealed class PlayerPrefsStorage : IKeyValueStorage
    {
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
        public void Save() => PlayerPrefs.Save();
    }

    public static class TabletLocalBackupService
    {
        public const int CurrentSchemaVersion = 2;
        public const string BackupJsonKey = "SUIYUE_Backup_Json";
        public const string BackupTimeKey = "SUIYUE_Backup_Time";
        public const string ManualIndexKey = "Bookmark_ManualIndex";
        public const string ManualKeyPrefix = "Bookmark_Manual_";

        private static readonly Regex ManualIdRegex = new Regex(@"^bm_\d{17}$", RegexOptions.Compiled);

        public enum BackupFormat
        {
            Unknown,
            V2Json,
            V1Json,
            LegacyText
        }

        [Serializable]
        public sealed class BackupPayloadV2
        {
            public int schemaVersion = CurrentSchemaVersion;
            public string createdAt = "";
            public List<FixedItemV2> fixedItems = new List<FixedItemV2>();
            public string manualIndex = "";
            public List<ManualBookmarkV2> manualBookmarks = new List<ManualBookmarkV2>();
        }

        [Serializable]
        public sealed class FixedItemV2
        {
            public string key;
            public bool exists;
            public string val;
        }

        [Serializable]
        public sealed class ManualBookmarkV2
        {
            public string id;
            public string val;
        }

        [Serializable]
        public sealed class BackupPayloadV1
        {
            public int version = 1;
            public string createdAt = "";
            public List<V1Item> items = new List<V1Item>();
        }

        [Serializable]
        public sealed class V1Item
        {
            public string key;
            public string val;
        }

        [Serializable]
        private sealed class SchemaVersionProbe
        {
            public int schemaVersion;
            public int version;
        }

        public static readonly string[] FixedWhitelistKeys = BuildFixedWhitelistKeys();
        public static readonly HashSet<string> FixedWhitelistSet = new HashSet<string>(FixedWhitelistKeys);

        private static string[] BuildFixedWhitelistKeys()
        {
            var keys = new List<string>();
            for (int i = 0; i <= 60; i++) keys.Add("VN_Save_" + i);
            for (int i = 0; i <= 5; i++) keys.Add("SaveSlot_" + i);
            keys.Add("Achievements_Data");
            for (int i = 0; i < 5; i++) keys.Add("Bookmark_Auto_" + i);
            return keys.ToArray();
        }

        public static bool IsValidManualBookmarkId(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return ManualIdRegex.IsMatch(id);
        }

        public static bool IsSupportedKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (FixedWhitelistSet.Contains(key)) return true;
            if (key == ManualIndexKey) return true;
            if (key.StartsWith(ManualKeyPrefix))
            {
                string id = key.Substring(ManualKeyPrefix.Length);
                return IsValidManualBookmarkId(id);
            }
            return false;
        }

        public static string CreateBackupPayload(IKeyValueStorage storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            var payload = new BackupPayloadV2
            {
                schemaVersion = CurrentSchemaVersion,
                createdAt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
            };

            // 1. Fixed archive items
            foreach (var key in FixedWhitelistKeys)
            {
                bool exists = storage.HasKey(key);
                string val = exists ? storage.GetString(key, "") : "";
                payload.fixedItems.Add(new FixedItemV2
                {
                    key = key,
                    exists = exists,
                    val = val
                });
            }

            // 2. Dynamic manual bookmarks
            string rawIndex = storage.GetString(ManualIndexKey, "");
            var validIds = new List<string>();
            if (!string.IsNullOrEmpty(rawIndex))
            {
                var split = rawIndex.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var seen = new HashSet<string>();
                foreach (var s in split)
                {
                    string id = s.Trim();
                    if (IsValidManualBookmarkId(id) && seen.Add(id))
                    {
                        validIds.Add(id);
                        string k = ManualKeyPrefix + id;
                        if (storage.HasKey(k))
                        {
                            payload.manualBookmarks.Add(new ManualBookmarkV2
                            {
                                id = id,
                                val = storage.GetString(k, "")
                            });
                        }
                    }
                }
            }
            payload.manualIndex = string.Join(",", validIds);

            return JsonUtility.ToJson(payload, true);
        }

        public static bool TryValidateBackup(string rawData, out BackupFormat format, out object parsedPayload, out string error)
        {
            format = BackupFormat.Unknown;
            parsedPayload = null;
            error = null;

            if (string.IsNullOrWhiteSpace(rawData))
            {
                error = "备份数据为空";
                return false;
            }

            string trimmed = rawData.Trim();
            if (trimmed.StartsWith("{"))
            {
                if (!trimmed.EndsWith("}"))
                {
                    error = "JSON 格式未闭合或损坏";
                    return false;
                }

                SchemaVersionProbe probe;
                try
                {
                    probe = JsonUtility.FromJson<SchemaVersionProbe>(trimmed);
                }
                catch (Exception ex)
                {
                    error = "JSON 解析失败: " + ex.Message;
                    return false;
                }

                if (probe == null)
                {
                    error = "JSON 解析为空对象";
                    return false;
                }

                int effectiveVersion = probe.schemaVersion > 0 ? probe.schemaVersion : probe.version;

                if (effectiveVersion <= 0)
                {
                    error = "备份数据版本号缺失或无效 (schemaVersion <= 0)";
                    return false;
                }

                if (effectiveVersion > CurrentSchemaVersion)
                {
                    error = $"不支持的更高备份版本 (v{effectiveVersion})，当前支持最高版本为 v{CurrentSchemaVersion}";
                    return false;
                }

                if (effectiveVersion == 2)
                {
                    BackupPayloadV2 v2;
                    try
                    {
                        v2 = JsonUtility.FromJson<BackupPayloadV2>(trimmed);
                    }
                    catch (Exception ex)
                    {
                        error = "V2 备份结构反序列化失败: " + ex.Message;
                        return false;
                    }

                    if (v2 == null)
                    {
                        error = "V2 备份反序列化为空对象";
                        return false;
                    }

                    // Validate fixedItems
                    var seenFixed = new HashSet<string>();
                    if (v2.fixedItems != null)
                    {
                        foreach (var item in v2.fixedItems)
                        {
                            if (item == null || string.IsNullOrEmpty(item.key))
                            {
                                error = "固定档案项包含空键名";
                                return false;
                            }
                            if (!FixedWhitelistSet.Contains(item.key))
                            {
                                error = $"固定档案项包含不支持的非白名单键: {item.key}";
                                return false;
                            }
                            if (!seenFixed.Add(item.key))
                            {
                                error = $"固定档案项包含重复键: {item.key}";
                                return false;
                            }
                        }
                    }

                    // Validate manualBookmarks
                    var seenManual = new HashSet<string>();
                    if (v2.manualBookmarks != null)
                    {
                        foreach (var mb in v2.manualBookmarks)
                        {
                            if (mb == null || string.IsNullOrEmpty(mb.id))
                            {
                                error = "手动书签项包含空 ID";
                                return false;
                            }
                            if (!IsValidManualBookmarkId(mb.id))
                            {
                                error = $"手动书签 ID 格式不符合规范: {mb.id}";
                                return false;
                            }
                            if (!seenManual.Add(mb.id))
                            {
                                error = $"手动书签项包含重复 ID: {mb.id}";
                                return false;
                            }
                        }
                    }

                    // Validate manualIndex
                    if (!string.IsNullOrEmpty(v2.manualIndex))
                    {
                        var indexTokens = v2.manualIndex.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        var seenIndexIds = new HashSet<string>();
                        foreach (var token in indexTokens)
                        {
                            string id = token.Trim();
                            if (!IsValidManualBookmarkId(id))
                            {
                                error = $"手动书签索引包含无效 ID: {id}";
                                return false;
                            }
                            if (!seenIndexIds.Add(id))
                            {
                                error = $"手动书签索引包含重复 ID: {id}";
                                return false;
                            }
                        }
                    }

                    format = BackupFormat.V2Json;
                    parsedPayload = v2;
                    return true;
                }

                if (effectiveVersion == 1)
                {
                    BackupPayloadV1 v1;
                    try
                    {
                        v1 = JsonUtility.FromJson<BackupPayloadV1>(trimmed);
                    }
                    catch (Exception ex)
                    {
                        error = "V1 备份结构反序列化失败: " + ex.Message;
                        return false;
                    }

                    if (v1 == null || v1.items == null)
                    {
                        error = "V1 备份反序列化为空对象或项列表为空";
                        return false;
                    }

                    var seenV1 = new HashSet<string>();
                    bool hasSupported = false;
                    foreach (var it in v1.items)
                    {
                        if (it == null || string.IsNullOrEmpty(it.key)) continue;
                        if (!seenV1.Add(it.key))
                        {
                            error = $"V1 备份项包含重复键: {it.key}";
                            return false;
                        }
                        if (IsSupportedKey(it.key))
                        {
                            hasSupported = true;
                        }
                    }

                    if (!hasSupported)
                    {
                        error = "V1 备份中未包含任何受支持的 VN 剧目档案键";
                        return false;
                    }

                    format = BackupFormat.V1Json;
                    parsedPayload = v1;
                    return true;
                }
            }

            // Legacy key=value text format
            try
            {
                var lines = trimmed.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var seenTextKeys = new HashSet<string>();
                var pairs = new Dictionary<string, string>();
                bool hasSupported = false;

                foreach (var line in lines)
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                    {
                        error = "历史文本备份包含无效键值对行: " + line;
                        return false;
                    }
                    string k = line.Substring(0, eq).Trim();
                    string v = line.Substring(eq + 1);

                    if (!seenTextKeys.Add(k))
                    {
                        error = "历史文本备份包含重复键: " + k;
                        return false;
                    }

                    if (IsSupportedKey(k))
                    {
                        pairs[k] = v;
                        hasSupported = true;
                    }
                }

                if (!hasSupported)
                {
                    error = "历史文本备份中未包含任何受支持的 VN 剧目档案键";
                    return false;
                }

                format = BackupFormat.LegacyText;
                parsedPayload = pairs;
                return true;
            }
            catch (Exception ex)
            {
                error = "历史格式备份读取异常: " + ex.Message;
                return false;
            }
        }

        public static bool RestoreBackup(IKeyValueStorage storage, string rawData, out string message, out int restoredCount)
        {
            restoredCount = 0;
            message = null;

            if (storage == null) throw new ArgumentNullException(nameof(storage));

            if (!TryValidateBackup(rawData, out var format, out var payload, out var err))
            {
                message = err;
                return false;
            }

            if (format == BackupFormat.V2Json)
            {
                var v2 = (BackupPayloadV2)payload;

                // 1. Restore fixed items with exact existence mapping
                var fixedMap = new Dictionary<string, FixedItemV2>();
                if (v2.fixedItems != null)
                {
                    foreach (var item in v2.fixedItems)
                    {
                        if (item != null && !string.IsNullOrEmpty(item.key))
                        {
                            fixedMap[item.key] = item;
                        }
                    }
                }

                foreach (var key in FixedWhitelistKeys)
                {
                    if (fixedMap.TryGetValue(key, out var item))
                    {
                        if (item.exists)
                        {
                            storage.SetString(key, item.val ?? "");
                            restoredCount++;
                        }
                        else
                        {
                            if (storage.HasKey(key))
                            {
                                storage.DeleteKey(key);
                            }
                        }
                    }
                    else
                    {
                        // Unmentioned fixed key in v2 snapshot is treated as non-existent
                        if (storage.HasKey(key))
                        {
                            storage.DeleteKey(key);
                        }
                    }
                }

                // 2. Restore dynamic manual bookmarks
                string currentRawIndex = storage.GetString(ManualIndexKey, "");
                var currentIndexedIds = new HashSet<string>();
                if (!string.IsNullOrEmpty(currentRawIndex))
                {
                    var tokens = currentRawIndex.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var t in tokens)
                    {
                        string id = t.Trim();
                        if (IsValidManualBookmarkId(id))
                        {
                            currentIndexedIds.Add(id);
                        }
                    }
                }

                var snapshotIds = new HashSet<string>();
                if (v2.manualBookmarks != null)
                {
                    foreach (var mb in v2.manualBookmarks)
                    {
                        if (mb != null && IsValidManualBookmarkId(mb.id))
                        {
                            snapshotIds.Add(mb.id);
                            storage.SetString(ManualKeyPrefix + mb.id, mb.val ?? "");
                            restoredCount++;
                        }
                    }
                }

                // Delete current manual bookmarks that are in current index but absent from snapshot
                foreach (var currId in currentIndexedIds)
                {
                    if (!snapshotIds.Contains(currId))
                    {
                        string k = ManualKeyPrefix + currId;
                        if (storage.HasKey(k))
                        {
                            storage.DeleteKey(k);
                        }
                    }
                }

                // Update manual index
                if (!string.IsNullOrEmpty(v2.manualIndex))
                {
                    storage.SetString(ManualIndexKey, v2.manualIndex);
                    restoredCount++;
                }
                else
                {
                    if (storage.HasKey(ManualIndexKey))
                    {
                        storage.DeleteKey(ManualIndexKey);
                    }
                }

                storage.Save();
                message = $"恢复完成：已按 V2 精确快照还原 {restoredCount} 项 VN 剧目档案与书签（未包含的档案槽位与多余手动书签已清理；下次进入剧目时生效）";
                return true;
            }

            if (format == BackupFormat.V1Json)
            {
                var v1 = (BackupPayloadV1)payload;
                foreach (var it in v1.items)
                {
                    if (it == null || string.IsNullOrEmpty(it.key)) continue;
                    if (IsSupportedKey(it.key))
                    {
                        storage.SetString(it.key, it.val ?? "");
                        restoredCount++;
                    }
                }

                storage.Save();
                message = $"恢复完成：已按旧版 V1 格式部分合并还原 {restoredCount} 项受支持的 VN 剧目档案（旧格式不含完整书签快照，未修改未列出档案；下次进入剧目时生效）";
                return true;
            }

            if (format == BackupFormat.LegacyText)
            {
                var pairs = (Dictionary<string, string>)payload;
                foreach (var kv in pairs)
                {
                    if (IsSupportedKey(kv.Key))
                    {
                        storage.SetString(kv.Key, kv.Value ?? "");
                        restoredCount++;
                    }
                }

                storage.Save();
                message = $"恢复完成：已按历史纯文本格式部分合并还原 {restoredCount} 项受支持的 VN 剧目档案（旧格式范围有限，未修改未列出档案；下次进入剧目时生效）";
                return true;
            }

            message = "未知备份格式";
            return false;
        }

        public static bool TryParseLegacyPairs(string rawData, out Dictionary<string, string> parsedPairs, out string error)
        {
            parsedPairs = new Dictionary<string, string>();
            error = null;

            if (!TryValidateBackup(rawData, out var format, out var payload, out error))
            {
                return false;
            }

            if (format == BackupFormat.V2Json)
            {
                var v2 = (BackupPayloadV2)payload;
                if (v2.fixedItems != null)
                {
                    foreach (var fi in v2.fixedItems)
                    {
                        if (fi != null && fi.exists) parsedPairs[fi.key] = fi.val ?? "";
                    }
                }
                if (!string.IsNullOrEmpty(v2.manualIndex)) parsedPairs[ManualIndexKey] = v2.manualIndex;
                if (v2.manualBookmarks != null)
                {
                    foreach (var mb in v2.manualBookmarks)
                    {
                        if (mb != null) parsedPairs[ManualKeyPrefix + mb.id] = mb.val ?? "";
                    }
                }
                return true;
            }

            if (format == BackupFormat.V1Json)
            {
                var v1 = (BackupPayloadV1)payload;
                foreach (var it in v1.items)
                {
                    if (it != null && IsSupportedKey(it.key)) parsedPairs[it.key] = it.val ?? "";
                }
                return true;
            }

            if (format == BackupFormat.LegacyText)
            {
                parsedPairs = (Dictionary<string, string>)payload;
                return true;
            }

            return false;
        }
    }
}
