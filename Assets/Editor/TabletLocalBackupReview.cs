using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.Apps;

/// <summary>
/// 平板设置「备份与恢复」数据范围、Schema v2、降级合并与零副作用自动化验收脚本。
/// 严格使用纯内存 Fake 存储，绝不读取、修改或清理真实 PlayerPrefs、经济存档或音乐文件。
/// </summary>
[InitializeOnLoad]
public static class TabletLocalBackupReview
{
    private const string PendingKey = "TabletLocalBackupReview.Pending";
    private const string OutputDir = "Library/TabletLocalBackupReview";

    private static double deadline;
    private static double nextCheck;
    private static int stage;
    private static int frame;
    private static UIDocument document;
    private static VisualElement root;
    private static RenderTexture target;
    private static FakeMemoryStorage reviewFakeStorage;

    public sealed class FakeMemoryStorage : IKeyValueStorage
    {
        public readonly Dictionary<string, string> Data = new Dictionary<string, string>();
        public int SetStringCount = 0;
        public int DeleteKeyCount = 0;
        public int SaveCount = 0;

        public int TotalWrites => SetStringCount + DeleteKeyCount;

        public void ResetCounters()
        {
            SetStringCount = 0;
            DeleteKeyCount = 0;
            SaveCount = 0;
        }

        public bool HasKey(string key) => Data.ContainsKey(key);
        public string GetString(string key, string defaultValue = "") => Data.TryGetValue(key, out var val) ? val : defaultValue;
        public void SetString(string key, string value)
        {
            Data[key] = value;
            SetStringCount++;
        }
        public void DeleteKey(string key)
        {
            Data.Remove(key);
            DeleteKeyCount++;
        }
        public void Save()
        {
            SaveCount++;
        }
    }

    static TabletLocalBackupReview()
    {
        if (SessionState.GetBool(PendingKey, false))
        {
            StartChecking();
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("[TabletLocalBackupReview Assert Failed] " + message);
        }
    }

    private static void StartChecking()
    {
        deadline = EditorApplication.timeSinceStartup + 60.0;
        EditorApplication.update -= Check;
        EditorApplication.update += Check;
    }

    [MenuItem("Railway/Review/Tablet Local Backup Review")]
    public static void Batch()
    {
        Debug.Log("==================================================");
        Debug.Log("[TabletLocalBackupReview] Starting automated validation batch...");

        // Record real PlayerPrefs state into SessionState to ensure zero modification
        bool realHadKey = PlayerPrefs.HasKey(TabletLocalBackupService.BackupJsonKey);
        string realVal = realHadKey ? PlayerPrefs.GetString(TabletLocalBackupService.BackupJsonKey, "") : "";
        SessionState.SetBool("TabletLocalBackupReview.RealHadKey", realHadKey);
        SessionState.SetString("TabletLocalBackupReview.RealVal", realVal);

        // Run isolated in-memory unit tests (Tests 1 - 5)
        RunMemoryUnitTests();

        Directory.CreateDirectory(OutputDir);
        SessionState.SetBool(PendingKey, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        StartChecking();
        EditorApplication.isPlaying = true;
    }

    private static void RunMemoryUnitTests()
    {
        Test1_V2RoundtripSpecialCharsAndExistence();
        Test2_SnapshotRestoreAccuracyAndCleanup();
        Test3_EmptySnapshotRestore();
        Test4_V1JsonAndLegacyTextPartialMerge();
        Test5_FailureIsolationZeroWrites();
        Debug.Log("[TabletLocalBackupReview] In-memory unit tests 1-5 passed successfully.");
    }

    private static void Test1_V2RoundtripSpecialCharsAndExistence()
    {
        var mem = new FakeMemoryStorage();
        string chineseText = "【第一章】归乡：晨雾与铁道，含各种标点：“”，《》！@#￥%……&*（）——+{}|：“”《》？~`";
        string multilineText = "line1\nline2\r\n\"escaped quotes\" and \\slashes\\ and \t tabs";
        string jsonText = "{\"title\":\"成就图鉴\",\"unlocked\":[\"first_ride\",\"engineer_lv2\"],\"rate\":0.95}";
        string emptyVal = "";

        mem.SetString("VN_Save_0", chineseText);
        mem.SetString("SaveSlot_0", multilineText);
        mem.SetString("SaveSlot_1", emptyVal); // exists with empty string
        mem.SetString("Achievements_Data", jsonText);
        mem.SetString("Bookmark_Auto_0", "auto_bm_0");
        mem.SetString("Bookmark_ManualIndex", "bm_20261026000000001,bm_20261026000000002");
        mem.SetString("Bookmark_Manual_bm_20261026000000001", "{\"speaker\":\"李铁男\",\"dialogue\":\"出发！\"}");
        mem.SetString("Bookmark_Manual_bm_20261026000000002", "{\"speaker\":\"张工程\",\"dialogue\":\"收到！\"}");
        // Omitted fixed keys: VN_Save_1 and SaveSlot_2 (non-existent)

        string payload = TabletLocalBackupService.CreateBackupPayload(mem);
        Assert(!string.IsNullOrEmpty(payload), "Payload must not be empty");

        bool valid = TabletLocalBackupService.TryValidateBackup(payload, out var format, out var parsedObj, out var err);
        Assert(valid, "Validation failed: " + err);
        Assert(format == TabletLocalBackupService.BackupFormat.V2Json, "Format must be V2Json");

        var v2 = (TabletLocalBackupService.BackupPayloadV2)parsedObj;
        Assert(v2.schemaVersion == 2, "Schema version must be 2");

        var fixedMap = new Dictionary<string, TabletLocalBackupService.FixedItemV2>();
        foreach (var item in v2.fixedItems) fixedMap[item.key] = item;

        Assert(fixedMap["VN_Save_0"].exists && fixedMap["VN_Save_0"].val == chineseText, "Chinese text must match exactly");
        Assert(fixedMap["SaveSlot_0"].exists && fixedMap["SaveSlot_0"].val == multilineText, "Multiline and escaped text must match exactly");
        Assert(fixedMap["SaveSlot_1"].exists && fixedMap["SaveSlot_1"].val == "", "Empty string must have exists=true, val=\"\"");
        Assert(!fixedMap["VN_Save_1"].exists, "Omitted key VN_Save_1 must have exists=false");
        Assert(fixedMap["Achievements_Data"].exists && fixedMap["Achievements_Data"].val == jsonText, "Achievements JSON must match exactly");

        Assert(v2.manualIndex == "bm_20261026000000001,bm_20261026000000002", "Manual index must match");
        Assert(v2.manualBookmarks.Count == 2, "Manual bookmarks count must be 2");
        Assert(v2.manualBookmarks[0].id == "bm_20261026000000001" && v2.manualBookmarks[0].val.Contains("李铁男"), "Bookmark 1 must match");
        Assert(v2.manualBookmarks[1].id == "bm_20261026000000002" && v2.manualBookmarks[1].val.Contains("张工程"), "Bookmark 2 must match");

        // Restore into clean storage
        var memRestore = new FakeMemoryStorage();
        bool restored = TabletLocalBackupService.RestoreBackup(memRestore, payload, out var msg, out var count);
        Assert(restored, "Restore failed: " + msg);
        Assert(memRestore.HasKey("SaveSlot_1") && memRestore.GetString("SaveSlot_1") == "", "Restored storage must have empty string key");
        Assert(!memRestore.HasKey("VN_Save_1"), "Restored storage must NOT have non-existent key");
        Assert(memRestore.GetString("VN_Save_0") == chineseText, "Restored Chinese text must match");
        Assert(memRestore.GetString("SaveSlot_0") == multilineText, "Restored multiline text must match");
    }

    private static void Test2_SnapshotRestoreAccuracyAndCleanup()
    {
        var mem = new FakeMemoryStorage();
        mem.SetString("VN_Save_0", "old_save_0");
        mem.SetString("VN_Save_5", "old_save_5_to_delete");
        mem.SetString("Bookmark_ManualIndex", "bm_20261026000000001,bm_20261026000000003");
        mem.SetString("Bookmark_Manual_bm_20261026000000001", "keep_bm_1");
        mem.SetString("Bookmark_Manual_bm_20261026000000003", "delete_bm_3");
        mem.SetString("Bookmark_Manual_bm_20261026999999999", "unindexed_orphan_keep");
        mem.SetString("GameData_Money", "99999");
        mem.SetString("Player_Settings_Volume", "80");

        var v2 = new TabletLocalBackupService.BackupPayloadV2
        {
            schemaVersion = 2,
            createdAt = "2026/10/26 12:00:00",
            manualIndex = "bm_20261026000000001"
        };
        v2.fixedItems.Add(new TabletLocalBackupService.FixedItemV2 { key = "VN_Save_0", exists = true, val = "new_save_0" });
        v2.fixedItems.Add(new TabletLocalBackupService.FixedItemV2 { key = "VN_Save_5", exists = false, val = "" });
        v2.manualBookmarks.Add(new TabletLocalBackupService.ManualBookmarkV2 { id = "bm_20261026000000001", val = "new_bm_1" });
        string rawJson = JsonUtility.ToJson(v2);

        bool success = TabletLocalBackupService.RestoreBackup(mem, rawJson, out var msg, out var count);
        Assert(success, "Restore failed: " + msg);

        Assert(mem.GetString("VN_Save_0") == "new_save_0", "VN_Save_0 must be updated");
        Assert(!mem.HasKey("VN_Save_5"), "VN_Save_5 must be deleted from storage");
        Assert(mem.GetString("Bookmark_ManualIndex") == "bm_20261026000000001", "Bookmark_ManualIndex must be updated");
        Assert(mem.GetString("Bookmark_Manual_bm_20261026000000001") == "new_bm_1", "Bookmark 1 must be updated");
        Assert(!mem.HasKey("Bookmark_Manual_bm_20261026000000003"), "Extra indexed manual bookmark 3 must be deleted");
        Assert(mem.GetString("Bookmark_Manual_bm_20261026999999999") == "unindexed_orphan_keep", "Unindexed orphan manual bookmark must be untouched");
        Assert(mem.GetString("GameData_Money") == "99999", "GameData_Money outside whitelist must be untouched");
        Assert(mem.GetString("Player_Settings_Volume") == "80", "Player_Settings_Volume outside whitelist must be untouched");
    }

    private static void Test3_EmptySnapshotRestore()
    {
        var mem = new FakeMemoryStorage();
        mem.SetString("VN_Save_0", "save_0");
        mem.SetString("SaveSlot_0", "slot_0");
        mem.SetString("Achievements_Data", "ach_data");
        mem.SetString("Bookmark_Auto_0", "auto_0");
        mem.SetString("Bookmark_ManualIndex", "bm_20261026000000001");
        mem.SetString("Bookmark_Manual_bm_20261026000000001", "bm_1");
        mem.SetString("OtherKey_Unrelated", "preserve_me");

        var emptyV2 = new TabletLocalBackupService.BackupPayloadV2
        {
            schemaVersion = 2,
            createdAt = "2026/10/26 12:00:00",
            manualIndex = ""
        };
        string rawJson = JsonUtility.ToJson(emptyV2);

        bool success = TabletLocalBackupService.RestoreBackup(mem, rawJson, out var msg, out var count);
        Assert(success, "Empty snapshot restore must succeed: " + msg);

        Assert(!mem.HasKey("VN_Save_0"), "VN_Save_0 must be cleared");
        Assert(!mem.HasKey("SaveSlot_0"), "SaveSlot_0 must be cleared");
        Assert(!mem.HasKey("Achievements_Data"), "Achievements_Data must be cleared");
        Assert(!mem.HasKey("Bookmark_Auto_0"), "Bookmark_Auto_0 must be cleared");
        Assert(!mem.HasKey("Bookmark_ManualIndex"), "Bookmark_ManualIndex must be cleared");
        Assert(!mem.HasKey("Bookmark_Manual_bm_20261026000000001"), "Bookmark_Manual_bm_... must be cleared");
        Assert(mem.GetString("OtherKey_Unrelated") == "preserve_me", "OtherKey_Unrelated outside whitelist must be preserved");
    }

    private static void Test4_V1JsonAndLegacyTextPartialMerge()
    {
        var mem = new FakeMemoryStorage();
        mem.SetString("VN_Save_0", "orig_save_0");
        mem.SetString("VN_Save_1", "orig_save_1_should_remain");
        mem.SetString("Unrelated_Key", "unrelated_remain");

        // V1 JSON containing VN_Save_0 and transient key VN_AutoLoad
        string v1Json = "{\"version\":1,\"createdAt\":\"2026/10/25 10:00:00\",\"items\":[{\"key\":\"VN_Save_0\",\"val\":\"v1_updated_0\"},{\"key\":\"VN_AutoLoad\",\"val\":\"123\"}]}";
        bool successV1 = TabletLocalBackupService.RestoreBackup(mem, v1Json, out var msgV1, out var countV1);
        Assert(successV1, "V1 restore must succeed: " + msgV1);

        Assert(mem.GetString("VN_Save_0") == "v1_updated_0", "VN_Save_0 must be updated by V1");
        Assert(mem.GetString("VN_Save_1") == "orig_save_1_should_remain", "VN_Save_1 not in V1 must be preserved (partial merge)");
        Assert(!mem.HasKey("VN_AutoLoad"), "Transient VN_AutoLoad must NOT be written as string");
        Assert(mem.GetString("Unrelated_Key") == "unrelated_remain", "Unrelated key must remain");

        // Legacy text format
        string legacyText = "VN_Save_1=legacy_save_1\nVN_AutoLoad=999\nInvalidKey=bad_val";
        bool successLegacy = TabletLocalBackupService.RestoreBackup(mem, legacyText, out var msgLegacy, out var countLegacy);
        Assert(successLegacy, "Legacy text restore must succeed: " + msgLegacy);

        Assert(mem.GetString("VN_Save_1") == "legacy_save_1", "VN_Save_1 must be updated by legacy text");
        Assert(mem.GetString("VN_Save_0") == "v1_updated_0", "VN_Save_0 not in legacy text must be preserved");
        Assert(!mem.HasKey("VN_AutoLoad"), "Transient VN_AutoLoad must not be written from legacy text");
        Assert(!mem.HasKey("InvalidKey"), "InvalidKey must not be written from legacy text");
    }

    private static void Test5_FailureIsolationZeroWrites()
    {
        string[] invalidPayloads = new[]
        {
            "", // empty
            "   ", // whitespace
            "{\"schemaVersion\": 0}", // invalid schema version <= 0
            "{\"version\": 0}", // invalid v1 version <= 0
            "{\"schemaVersion\": 999}", // future unsupported schema
            "{\"invalid json without close brace", // corrupt JSON
            "{\"schemaVersion\": 2, \"fixedItems\": [{\"key\": \"VN_Save_0\", \"exists\": true}, {\"key\": \"VN_Save_0\", \"exists\": true}]}", // duplicate fixed key
            "{\"schemaVersion\": 2, \"fixedItems\": [{\"key\": \"GameData_Money\", \"exists\": true}]}", // non-whitelist key in fixedItems
            "{\"schemaVersion\": 2, \"manualBookmarks\": [{\"id\": \"invalid_id_format\", \"val\": \"foo\"}]}", // invalid manual bookmark ID
            "{\"schemaVersion\": 2, \"manualBookmarks\": [{\"id\": \"bm_20261026000000001\", \"val\": \"foo\"}, {\"id\": \"bm_20261026000000001\", \"val\": \"bar\"}]}", // duplicate manual ID
            "{\"schemaVersion\": 2, \"manualIndex\": \"bm_invalid_id\"}", // invalid manual index ID
            "Just random non-key-value text with no equals sign" // invalid legacy text
        };

        for (int i = 0; i < invalidPayloads.Length; i++)
        {
            var mem = new FakeMemoryStorage();
            mem.SetString("VN_Save_0", "safe_val");
            mem.ResetCounters();

            bool ok = TabletLocalBackupService.RestoreBackup(mem, invalidPayloads[i], out var err, out var count);
            Assert(!ok, $"Payload {i} must fail validation, but succeeded");
            Assert(mem.TotalWrites == 0, $"Payload {i} failed validation but made {mem.TotalWrites} writes to storage! (Zero writes required)");
            Assert(mem.GetString("VN_Save_0") == "safe_val", "Pre-existing storage content must remain unaltered");
        }
    }

    private static void Resize(int width, int height)
    {
        if (target != null)
        {
            if (document != null && document.panelSettings != null) document.panelSettings.targetTexture = null;
            target.Release();
            UnityEngine.Object.Destroy(target);
        }
        target = new RenderTexture(width, height, 24);
        target.Create();
        document.panelSettings.targetTexture = target;
    }

    private static void Capture(string filename)
    {
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        image.Apply();
        RenderTexture.active = previous;
        string fullPath = Path.Combine(OutputDir, filename);
        File.WriteAllBytes(fullPath, image.EncodeToPNG());
        UnityEngine.Object.Destroy(image);
        Debug.Log("[TabletLocalBackupReview] Captured screenshot: " + fullPath);
    }

    private static void Click(Button b)
    {
        Assert(b != null, "Button must not be null");
        var clickedField = typeof(Clickable).GetField("clicked", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        var action = clickedField?.GetValue(b.clickable) as Action;
        if (action != null)
        {
            action.Invoke();
        }
        else
        {
            using var evt = ClickEvent.GetPooled();
            evt.target = b;
            b.SendEvent(evt);
        }
    }

    private static void Check()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
            {
                throw new TimeoutException("Tablet local backup review timed out");
            }
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (stage > 0 && Time.frameCount - frame < 15) return;
            if (EditorApplication.timeSinceStartup < nextCheck) return;

            switch (stage)
            {
                case 0:
                    // Setup UIDocument and PanelSettings
                    var hostGo = new GameObject("BackupReviewHost");
                    document = hostGo.AddComponent<UIDocument>();
                    var panelSettings = Resources.Load<PanelSettings>("UI/TitleScreenPanelSettings");
                    Assert(panelSettings != null, "TitleScreenPanelSettings asset missing in Resources/UI");
                    var clonedSettings = UnityEngine.Object.Instantiate(panelSettings);
                    clonedSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
                    clonedSettings.scale = 1;
                    clonedSettings.clearColor = true;
                    clonedSettings.colorClearValue = new Color(0.08f, 0.09f, 0.12f, 1f);
                    document.panelSettings = clonedSettings;
                    Resize(1440, 1024);

                    root = document.rootVisualElement;
                    root.Clear();
                    root.style.width = 1440;
                    root.style.height = 1024;

                    // Inject fake memory storage to TabletSettingsAppUI
                    reviewFakeStorage = new FakeMemoryStorage();
                    TabletSettingsAppUI.StorageAdapter = reviewFakeStorage;

                    // Pre-populate fake storage with existing state and a valid V2 backup
                    reviewFakeStorage.SetString("VN_Save_0", "运行时剧目进度0");
                    reviewFakeStorage.SetString("SaveSlot_0", "运行时故事槽位0");
                    reviewFakeStorage.SetString("Bookmark_ManualIndex", "bm_20261026000000001");
                    reviewFakeStorage.SetString("Bookmark_Manual_bm_20261026000000001", "运行时待删除书签");
                    reviewFakeStorage.SetString("Unrelated_Player_Gold", "9999");

                    // Create backup payload with distinct data
                    var backupSource = new FakeMemoryStorage();
                    backupSource.SetString("VN_Save_0", "快照备份剧目进度0");
                    backupSource.SetString("SaveSlot_0", "快照备份故事槽位0");
                    backupSource.SetString("Achievements_Data", "{\"achievements\":[\"restored_badge\"]}");
                    backupSource.SetString("Bookmark_Auto_0", "快照自动书签0");
                    backupSource.SetString("Bookmark_ManualIndex", "bm_20261026000000002");
                    backupSource.SetString("Bookmark_Manual_bm_20261026000000002", "快照恢复书签2");
                    string testBackupJson = TabletLocalBackupService.CreateBackupPayload(backupSource);
                    reviewFakeStorage.SetString(TabletLocalBackupService.BackupJsonKey, testBackupJson);
                    reviewFakeStorage.SetString(TabletLocalBackupService.BackupTimeKey, "2026/10/26 15:30");
                    reviewFakeStorage.ResetCounters();

                    // 1. Verify Category 23 Entry Row Text
                    var catField = typeof(TabletSettingsAppUI).GetField("currentSettingsCategory", BindingFlags.Static | BindingFlags.NonPublic);
                    int origCat = (int)catField.GetValue(null);
                    catField.SetValue(null, 23);
                    var settingsWin = new VisualElement { name = "settings-win" };
                    settingsWin.style.width = 1440; settingsWin.style.height = 1024;
                    root.Add(settingsWin);
                    TabletSettingsAppUI.BuildSettingsPage(settingsWin);

                    var backupTitleLabel = settingsWin.Query<Label>().Where(l => l.text != null && l.text.Contains("本地数据备份与恢复")).First();
                    Assert(backupTitleLabel != null, "Category 23 must contain '本地数据备份与恢复' row title");
                    var category23Labels = settingsWin.Query<Label>().ToList();
                    foreach (var lbl in category23Labels)
                    {
                        Assert(!lbl.text.Contains("快照经营数据"), "Category 23 must NOT contain '快照经营数据'");
                    }
                    var backupDescLabel = settingsWin.Query<Label>().Where(l => l.text != null && l.text.Contains("快照 VN 剧目存档、成就与书签记录（不含经营运行态）")).First();
                    Assert(backupDescLabel != null, "Category 23 row subtitle must match '快照 VN 剧目存档、成就与书签记录（不含经营运行态）'");

                    catField.SetValue(null, origCat);
                    root.Clear();

                    // 2. Render Backup & Restore Page directly into root
                    TabletSettingsAppUI.ShowBackupRestorePage(root);

                    // Verify labels in Backup & Restore page
                    var tipLabel = root.Q<Label>("backup-tip-label");
                    Assert(tipLabel != null, "backup-tip-label must exist");
                    Assert(tipLabel.text == "本机档案备份系统：将主线剧目阅读进度、剧目存档、成就与书签快照备份至本机", "backup-tip-label text mismatch: " + tipLabel.text);

                    var scopeLabel = root.Q<Label>("backup-scope-label");
                    Assert(scopeLabel != null, "backup-scope-label must exist");
                    Assert(scopeLabel.text.Contains("支持范围：主线剧目进度") && scopeLabel.text.Contains("不含金钱/天数等经营运行态"), "backup-scope-label text mismatch: " + scopeLabel.text);

                    var statusLabel = root.Q<Label>("backup-status-label");
                    Assert(statusLabel != null, "backup-status-label must exist");
                    Assert(statusLabel.text.Contains("上次备份：2026/10/26 15:30"), "backup-status-label text mismatch: " + statusLabel.text);

                    var allPageLabels = root.Query<Label>().ToList();
                    foreach (var lbl in allPageLabels)
                    {
                        Assert(!lbl.text.Contains("经营数据"), "Backup page must NOT contain '经营数据' in any label text, found in: " + lbl.text);
                    }
                    break;

                case 1:
                    // Capture 1440x1024 backup page
                    Capture("01-backup-page-1440.png");

                    // Click "恢复上次备份" -> confirm box should show
                    var btnRestore = root.Q<Button>("btn-backup-restore");
                    Assert(btnRestore != null, "btn-backup-restore missing");
                    Click(btnRestore);

                    var confirmBox = root.Q<VisualElement>("backup-confirm-box");
                    Assert(confirmBox != null, "backup-confirm-box missing");
                    Assert(confirmBox.resolvedStyle.display == DisplayStyle.Flex || confirmBox.style.display == DisplayStyle.Flex, "confirmBox must be visible after clicking restore");

                    var confirmDesc = root.Q<Label>("backup-confirm-desc");
                    Assert(confirmDesc != null, "backup-confirm-desc missing");
                    Assert(confirmDesc.text.Contains("按快照精确还原主线剧目存档") && confirmDesc.text.Contains("不影响金钱与经营运行态"), "confirmDesc text mismatch: " + confirmDesc.text);
                    Assert(!confirmDesc.text.Contains("覆盖当前主线剧目与经营数据进度"), "confirmDesc must not mention covering economic data");

                    Capture("02-backup-confirm-1440.png");

                    // Test Cancel button -> zero writes, confirmBox hidden
                    int writesBeforeCancel = reviewFakeStorage.TotalWrites;
                    var btnCancel = root.Q<Button>("btn-backup-cancel");
                    Assert(btnCancel != null, "btn-backup-cancel missing");
                    Click(btnCancel);

                    Assert(confirmBox.style.display == DisplayStyle.None, "confirmBox must be hidden after cancel");
                    Assert(reviewFakeStorage.TotalWrites == writesBeforeCancel, "Cancel button must cause ZERO writes on fake storage");
                    break;

                case 2:
                    // Click "恢复上次备份" again, then Confirm
                    var btnRestore2 = root.Q<Button>("btn-backup-restore");
                    Click(btnRestore2);
                    var confirmBox2 = root.Q<VisualElement>("backup-confirm-box");
                    Assert(confirmBox2.style.display == DisplayStyle.Flex, "confirmBox must be visible again");

                    var btnConfirm = root.Q<Button>("btn-backup-confirm");
                    Assert(btnConfirm != null, "btn-backup-confirm missing");
                    Click(btnConfirm);

                    Assert(confirmBox2.style.display == DisplayStyle.None, "confirmBox must be hidden after confirm");

                    // Verify restore results in fake storage
                    Assert(reviewFakeStorage.GetString("VN_Save_0") == "快照备份剧目进度0", "VN_Save_0 must be restored");
                    Assert(reviewFakeStorage.GetString("SaveSlot_0") == "快照备份故事槽位0", "SaveSlot_0 must be restored");
                    Assert(reviewFakeStorage.GetString("Achievements_Data") == "{\"achievements\":[\"restored_badge\"]}", "Achievements_Data must be restored");
                    Assert(reviewFakeStorage.GetString("Bookmark_Auto_0") == "快照自动书签0", "Bookmark_Auto_0 must be restored");
                    Assert(reviewFakeStorage.GetString("Bookmark_ManualIndex") == "bm_20261026000000002", "Bookmark_ManualIndex must be updated");
                    Assert(reviewFakeStorage.GetString("Bookmark_Manual_bm_20261026000000002") == "快照恢复书签2", "Bookmark 2 must be restored");
                    Assert(!reviewFakeStorage.HasKey("Bookmark_Manual_bm_20261026000000001"), "bm 1 absent from snapshot must be deleted");
                    Assert(reviewFakeStorage.GetString("Unrelated_Player_Gold") == "9999", "Unrelated_Player_Gold outside whitelist must be untouched");

                    // Test "立即备份" button
                    var btnBackup = root.Q<Button>("btn-backup-now");
                    Assert(btnBackup != null, "btn-backup-now missing");
                    Click(btnBackup);

                    string savedBackup = reviewFakeStorage.GetString(TabletLocalBackupService.BackupJsonKey, "");
                    Assert(!string.IsNullOrEmpty(savedBackup), "BackupJsonKey must be saved");
                    bool savedValid = TabletLocalBackupService.TryValidateBackup(savedBackup, out var fmt, out _, out _);
                    Assert(savedValid && fmt == TabletLocalBackupService.BackupFormat.V2Json, "Saved backup must be valid V2 JSON");
                    break;

                case 3:
                    // Resize to 1024x768
                    Resize(1024, 768);
                    root.style.width = 1024;
                    root.style.height = 768;
                    break;

                case 4:
                    Capture("03-backup-page-1024.png");

                    var btnBackup1024 = root.Q<Button>("btn-backup-now");
                    var btnRestore1024 = root.Q<Button>("btn-backup-restore");
                    Assert(btnBackup1024 != null && btnRestore1024 != null, "Buttons must exist at 1024x768");

                    // Verify buttons are within bounds and pickable
                    var page = root.Q("backup-restore-page");
                    Assert(page != null, "backup-restore-page container missing");
                    Assert(page.worldBound.xMin >= -0.1f && page.worldBound.xMax <= 1024.1f, "Page container must not overflow 1024 width: " + page.worldBound);

                    // Verify Real PlayerPrefs was NEVER touched
                    bool initialRealPlayerPrefsHadKey = SessionState.GetBool("TabletLocalBackupReview.RealHadKey", false);
                    string initialRealPlayerPrefsBackup = SessionState.GetString("TabletLocalBackupReview.RealVal", "");
                    bool realPlayerPrefsNowHasKey = PlayerPrefs.HasKey(TabletLocalBackupService.BackupJsonKey);
                    string realPlayerPrefsNowVal = realPlayerPrefsNowHasKey ? PlayerPrefs.GetString(TabletLocalBackupService.BackupJsonKey) : "";
                    Assert(realPlayerPrefsNowHasKey == initialRealPlayerPrefsHadKey, "Real PlayerPrefs key existence changed! Real PlayerPrefs must NOT be touched");
                    Assert(realPlayerPrefsNowVal == initialRealPlayerPrefsBackup, "Real PlayerPrefs value changed! Real PlayerPrefs must NOT be touched");

                    Debug.Log("==================================================");
                    Debug.Log("TABLET_LOCAL_BACKUP_REVIEW_OK: v2 roundtrip, exact snapshot deletion, empty snapshot, v1/legacy partial merge, failure zero-writes, UI text/picking at 1440x1024 and 1024x768, and real PlayerPrefs protection verified.");
                    Debug.Log("==================================================");
                    Finish(0);
                    return;
            }

            stage++;
            frame = Time.frameCount;
            nextCheck = EditorApplication.timeSinceStartup + 0.4;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Finish(1);
        }
    }

    private static void Finish(int code)
    {
        SessionState.SetBool(PendingKey, false);
        EditorApplication.update -= Check;
        TabletSettingsAppUI.StorageAdapter = new PlayerPrefsStorage();
        if (target != null)
        {
            if (document != null && document.panelSettings != null) document.panelSettings.targetTexture = null;
            target.Release();
            target = null;
        }
        EditorApplication.Exit(code);
    }
}
