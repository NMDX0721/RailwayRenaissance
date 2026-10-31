using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using WorldGen;

public static class NewGameSeedPresetReview
{
    private const string ReviewLogHeader = "[NewGameSeedPresetReview]";

    [MenuItem("Railway/Review/New Game Seed Preset Review")]
    public static void RunFromMenu()
    {
        RunAllTests(isBatch: false);
    }

    public static void Batch()
    {
        RunAllTests(isBatch: true);
    }

    private static (GameObject go, UIDocument doc) SetupTestRoot()
    {
        var go = new GameObject("Review_NewGameSetup_Root");
        var doc = go.AddComponent<UIDocument>();
        var settings = Resources.Load<PanelSettings>("UI/TitleScreenPanelSettings");
        if (settings != null)
        {
            doc.panelSettings = settings;
        }
        return (go, doc);
    }

    public static void RunAllTests(bool isBatch)
    {
        Debug.Log($"{ReviewLogHeader} Starting comprehensive seed preset and procedural generation review...");

        // Snapshot PlayerPrefs to guarantee zero side effects
        string originalConfigJson = PlayerPrefs.GetString("RailGameConfig", null);
        bool hadConfig = PlayerPrefs.HasKey("RailGameConfig");

        int passedCount = 0;
        int failedCount = 0;

        void Assert(bool condition, string testName, string detail = "")
        {
            if (condition)
            {
                passedCount++;
                Debug.Log($"{ReviewLogHeader} [PASS] {testName} {(string.IsNullOrEmpty(detail) ? "" : "- " + detail)}");
            }
            else
            {
                failedCount++;
                Debug.LogError($"{ReviewLogHeader} [FAIL] {testName} {(string.IsNullOrEmpty(detail) ? "" : "- " + detail)}");
            }
        }

        try
        {
            // ─────────────────────────────────────────────────────────────
            // Test 1: 随机码算法有效性与 20-bit 范围覆盖
            // ─────────────────────────────────────────────────────────────
            bool allRandomValid = true;
            var generatedCodes = new HashSet<string>();
            var rng = new System.Random(12345);

            for (int i = 0; i < 50; i++)
            {
                string code = GameConfig.GenerateRandomSeedCode(rng);
                bool valid = GameConfig.IsValidSeedCode(code);
                int seedVal = WorldGenerator.DecodeSeed(code);
                if (!valid || seedVal < 0 || seedVal > 0xFFFFF)
                {
                    allRandomValid = false;
                    break;
                }
                generatedCodes.Add(code);
            }

            Assert(allRandomValid && generatedCodes.Count > 40, "Test 1: Random Seed Validity & Range",
                $"50 random seeds generated, distinct count: {generatedCodes.Count}, all non-negative and <= 0xFFFFF.");

            // ─────────────────────────────────────────────────────────────
            // Test 2: 预设列表与编码对应性
            // ─────────────────────────────────────────────────────────────
            bool presetsAllValid = true;
            foreach (var preset in GameConfig.Presets)
            {
                bool valid = GameConfig.IsValidSeedCode(preset.code);
                int decoded = WorldGenerator.DecodeSeed(preset.code);
                string reEncoded = WorldGenerator.EncodeSeed(preset.seed);
                if (!valid || decoded != preset.seed || reEncoded != preset.code)
                {
                    presetsAllValid = false;
                    Debug.LogError($"{ReviewLogHeader} Preset mismatch: seed={preset.seed}, code={preset.code}, decoded={decoded}, reEncoded={reEncoded}");
                }
            }

            Assert(presetsAllValid && GameConfig.Presets.Length == 5, "Test 2: Preset Definitions & Decode",
                $"5 presets verified: {string.Join(", ", GameConfig.Presets.Select(p => p.code))}");

            // ─────────────────────────────────────────────────────────────
            // Test 3: 错误格式与校验和拒绝
            // ─────────────────────────────────────────────────────────────
            string[] invalidCandidates = new string[]
            {
                null,
                "",
                "12345",
                "RR-7A3F-B2C9",      // 早期历史无效码
                "RR-042-D9E1",       // 位数不足且校验和错误
                "RR-0002A-FFFFF",     // 校验和被篡改
                "RR-00001-00004",     // 校验和偏离 1
                "ZZ-00001-00003",     // 前缀错误
                "RR-123456-00000",    // 长度溢出
                "RR-ABCDE-FGHIJ"      // 非十六进制
            };

            bool allRejected = true;
            foreach (var inv in invalidCandidates)
            {
                if (GameConfig.IsValidSeedCode(inv) || WorldGenerator.DecodeSeed(inv) >= 0)
                {
                    allRejected = false;
                    Debug.LogError($"{ReviewLogHeader} False positive on invalid code: {inv}");
                }
            }

            Assert(allRejected, "Test 3: Invalid Seed Rejection",
                "All 10 invalid formats and corrupted checksums correctly rejected.");

            // ─────────────────────────────────────────────────────────────
            // Test 4: UI 设置流程与确认值不变性
            // ─────────────────────────────────────────────────────────────
            var (rootGo, uiDoc) = SetupTestRoot();
            try
            {
                var setupUI = rootGo.AddComponent<NewGameSetupUI>();
                bool confirmedCalled = false;
                setupUI.Init(uiDoc, () => { confirmedCalled = true; });

                // 检查初始模式为 Random，并有有效随机码
                Assert(setupUI.CurrentSeedModeIndex == 0, "Test 4.1: UI Default Mode is Random", "Default mode is SeedMode.Random.");
                string initialRandomCode = setupUI.CurrentSeedCode;
                Assert(GameConfig.IsValidSeedCode(initialRandomCode), "Test 4.2: Initial Seed Code Valid", $"Random code: {initialRandomCode}");

                // 切换到预设模式，选择预设 1 (seed 42)
                setupUI.SelectPresetForReview(1);
                Assert(setupUI.CurrentSeedCode == "RR-0002A-0007E", "Test 4.3: Preset Selection", "Preset 1 set code to RR-0002A-0007E.");

                // 切换到输入模式，输入合法码 RR-00071-00153 (seed 113)
                setupUI.SetInputSeedForReview("RR-00071-00153");
                Assert(setupUI.CurrentSeedCode == "RR-00071-00153" && setupUI.CanConfirm, "Test 4.4: Valid Input Mode", "Valid input code accepted.");

                // 输入非法码，验证阻断确认
                setupUI.SetInputSeedForReview("RR-INVALID-00000");
                Assert(!setupUI.CanConfirm, "Test 4.5: Invalid Input Blocks Confirmation", "Invalid code disables confirmation.");

                // 切回随机模式，验证保留初次生成的随机码不变（跨页/切模式不变性）
                setupUI.SetSeedModeForReview(0);
                Assert(setupUI.CurrentSeedCode == initialRandomCode, "Test 4.6: Invariance of Initial Random Seed",
                    $"Restored random code {initialRandomCode} without unintended re-roll.");

                // 重新随机测试
                setupUI.ReRollRandomSeedForReview();
                string newRandomCode = setupUI.CurrentSeedCode;
                Assert(GameConfig.IsValidSeedCode(newRandomCode), "Test 4.7: Explicit Re-Roll", $"New random seed {newRandomCode} valid.");

                // 模拟确认：验证保存的代码与当前 UI 代码完全一致
                setupUI.ConfirmForReview();
                var savedConfig = GameConfig.Load();
                Assert(confirmedCalled && savedConfig.seedCode == newRandomCode, "Test 4.8: Confirmation Persistence Matches UI",
                    $"Saved seedCode '{savedConfig.seedCode}' exactly matches UI '{newRandomCode}'.");
            }
            finally
            {
                if (rootGo != null) UnityEngine.Object.DestroyImmediate(rootGo);
            }

            // ─────────────────────────────────────────────────────────────
            // Test 5: 确定性世界生成一致性 (同码两次生成规范化比较)
            // ─────────────────────────────────────────────────────────────
            string testSeedCode = "RR-0002A-0007E"; // seed 42
            WorldSeedData world1 = WorldGenerator.GenerateFromSeedCode(testSeedCode);
            WorldSeedData world2 = WorldGenerator.GenerateFromSeedCode(testSeedCode);

            bool worldIdentical = (world1 != null && world2 != null)
                && world1.seedId == world2.seedId
                && world1.seedCode == world2.seedCode
                && world1.politicalTendency == world2.politicalTendency
                && world1.politicalCycleLength == world2.politicalCycleLength
                && Mathf.Approximately(world1.politicalFavor, world2.politicalFavor)
                && Mathf.Approximately(world1.stability, world2.stability)
                && world1.resourceDistribution.pattern == world2.resourceDistribution.pattern
                && Mathf.Approximately(world1.initialTrends.trust, world2.initialTrends.trust)
                && Mathf.Approximately(world1.initialTrends.fiscalPressure, world2.initialTrends.fiscalPressure)
                && Mathf.Approximately(world1.initialTrends.sandPenetration, world2.initialTrends.sandPenetration)
                && Mathf.Approximately(world1.initialTrends.politicalPressure, world2.initialTrends.politicalPressure)
                && Mathf.Approximately(world1.initialTrends.infrastructureDecay, world2.initialTrends.infrastructureDecay)
                && world1.cities.Count == world2.cities.Count
                && world1.railEdges.Count == world2.railEdges.Count;

            // 深度城市与边属性比较
            if (worldIdentical)
            {
                foreach (var kvp in world1.cities)
                {
                    if (!world2.cities.TryGetValue(kvp.Key, out var c2) ||
                        kvp.Value.population != c2.population ||
                        kvp.Value.coordinates != c2.coordinates ||
                        !kvp.Value.industries.SequenceEqual(c2.industries) ||
                        !kvp.Value.dependencies.imports.SequenceEqual(c2.dependencies.imports) ||
                        !kvp.Value.dependencies.exports.SequenceEqual(c2.dependencies.exports))
                    {
                        worldIdentical = false;
                        break;
                    }
                }
                for (int i = 0; i < world1.railEdges.Count; i++)
                {
                    var e1 = world1.railEdges[i];
                    var e2 = world2.railEdges[i];
                    if (e1.fromCity != e2.fromCity || e1.toCity != e2.toCity ||
                        !Mathf.Approximately(e1.travelTime, e2.travelTime) || e1.capacity != e2.capacity)
                    {
                        worldIdentical = false;
                        break;
                    }
                }
            }

            Assert(worldIdentical, "Test 5: Deterministic World Invariance",
                "World A and World B for seed RR-0002A-0007E are 100% identical across all dimensions.");

            // ─────────────────────────────────────────────────────────────
            // Test 6: 不同种子具备合理差异性
            // ─────────────────────────────────────────────────────────────
            WorldSeedData worldOther = WorldGenerator.GenerateFromSeedCode("RR-003E7-00BB5"); // seed 999
            bool worldDiffers = world1 != null && worldOther != null &&
                (world1.initialTrends.trust != worldOther.initialTrends.trust ||
                 world1.initialTrends.politicalPressure != worldOther.initialTrends.politicalPressure ||
                 world1.cities.Values.Sum(c => c.population) != worldOther.cities.Values.Sum(c => c.population));

            Assert(worldDiffers, "Test 6: Seed Diversity",
                $"Seeds 42 and 999 differ in trends and population (Seed 42 trust={world1.initialTrends.trust:F3}, Seed 999 trust={worldOther.initialTrends.trust:F3}).");

            // ─────────────────────────────────────────────────────────────
            // Test 7: 运行时子系统接线与 CurrentSeed 注入
            // ─────────────────────────────────────────────────────────────
            var testConfig = new GameConfig
            {
                seedCode = "RR-0004D-000E7", // seed 77
                seedId = "seed_001"
            };
            testConfig.Save();

            GameData.InitializeSubsystems();
            bool seedWired = GameData.CurrentSeed != null
                && GameData.CurrentSeed.seedCode == "RR-0004D-000E7"
                && GameData.CurrentSeed.seedId == "seed_00077";

            Assert(seedWired, "Test 7: Runtime Subsystem Wiring",
                $"GameData.CurrentSeed correctly generated from seedCode: id={GameData.CurrentSeed?.seedId}, code={GameData.CurrentSeed?.seedCode}");

            // ─────────────────────────────────────────────────────────────
            // Test 8: 旧配置与无效码兼容安全回退
            // ─────────────────────────────────────────────────────────────
            var legacyFallbackConfig = new GameConfig
            {
                seedCode = "RR-7A3F-B2C9", // legacy invalid code
                seedId = "seed_001"
            };
            legacyFallbackConfig.Save();

            GameData.InitializeSubsystems();
            bool fallbackSuccess = GameData.CurrentSeed != null && GameData.CurrentSeed.seedId == "seed_001";
            Assert(fallbackSuccess, "Test 8.1: Legacy Invalid Code Fallback",
                $"Invalid seedCode fell back safely to legacy resource: {GameData.CurrentSeed?.seedId}");

            var emptyCodeConfig = new GameConfig
            {
                seedCode = "",
                seedId = "seed_001"
            };
            emptyCodeConfig.Save();

            GameData.InitializeSubsystems();
            bool emptyCodeFallback = GameData.CurrentSeed != null && GameData.CurrentSeed.seedId == "seed_001";
            Assert(emptyCodeFallback, "Test 8.2: Empty SeedCode Fallback",
                $"Empty seedCode fell back safely to legacy resource: {GameData.CurrentSeed?.seedId}");
        }
        catch (Exception ex)
        {
            failedCount++;
            Debug.LogError($"{ReviewLogHeader} Unexpected Exception during review: {ex}");
        }
        finally
        {
            // Restore original PlayerPrefs state
            if (hadConfig)
            {
                PlayerPrefs.SetString("RailGameConfig", originalConfigJson);
            }
            else
            {
                PlayerPrefs.DeleteKey("RailGameConfig");
            }
            PlayerPrefs.Save();
            Debug.Log($"{ReviewLogHeader} Restored original PlayerPrefs 'RailGameConfig'.");
        }

        Debug.Log($"{ReviewLogHeader} Review Completed. Passed: {passedCount}, Failed: {failedCount}.");

        if (failedCount == 0 && passedCount > 0)
        {
            Debug.Log("NEW_GAME_SEED_PRESET_REVIEW_OK: all automated acceptance criteria passed successfully.");
            if (isBatch)
            {
                EditorApplication.Exit(0);
            }
        }
        else
        {
            Debug.LogError($"NEW_GAME_SEED_PRESET_REVIEW_FAILED: {failedCount} tests failed.");
            if (isBatch)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
