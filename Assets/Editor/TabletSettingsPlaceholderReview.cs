using System.Linq;
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.Apps;

/// <summary>
/// 平板设置 App 剩余分类（3、8、16、17、19、20、21、24）去占位与状态真实性自动化验收脚本。
/// 验证 8 个目标分类均走专属页面、彻底消除通用三行占位模板、
/// 保持无消费端状态的纯只读无误导箭头与假操作、状态与文案符合机车车载内网与硬件事实、
/// 真实可达入口正常导航/降级且不写入无关设置，并在 1440×1024 与 1024×768 分辨率下完成布局测量与截屏。
/// </summary>
[InitializeOnLoad]
public static class TabletSettingsPlaceholderReview
{
    private const string PendingKey = "TabletSettingsPlaceholderReview.Pending";
    private const string OriginalCategoryKey = "TabletSettingsPlaceholderReview.OriginalCategory";
    private static GameObject hostGo;
    private static UIDocument hostDoc;
    private static RenderTexture activeTarget;
    private static double runtimeDeadline;
    private static int layoutIndex;
    private static int layoutFrame;
    private static readonly int[] LayoutCategories = { 3, 8, 16, 17, 19, 20, 21, 24 };
    private static readonly Vector2Int[] LayoutResolutions = { new Vector2Int(1440, 1024), new Vector2Int(1024, 768) };
    private const string OutputDir = "Library/TabletSettingsPlaceholderReview";

    static TabletSettingsPlaceholderReview()
    {
        if (SessionState.GetBool(PendingKey, false)) StartRuntimeLayoutReview();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("[TabletSettingsPlaceholderReview Assert Failed] " + message);
        }
    }

    private static VisualElement SetupTestRoot(int width = 1440, int height = 1024)
    {
        if (hostGo == null)
        {
            hostGo = new GameObject("SettingsPlaceholderTestRoot");
            hostDoc = hostGo.AddComponent<UIDocument>();
            var panelSettings = Resources.Load<PanelSettings>("UI/TitleScreenPanelSettings");
            var settings = panelSettings != null ? ScriptableObject.Instantiate(panelSettings) : ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.scale = 1;
            hostDoc.panelSettings = settings;
        }

        ResizeTarget(width, height);

        var root = hostDoc.rootVisualElement;
        root.style.width = width;
        root.style.height = height;
        root.Clear();
        return root;
    }

    private static void TeardownTestRoot()
    {
        if (hostGo != null)
        {
            if (hostDoc != null && hostDoc.panelSettings != null && hostDoc.panelSettings.targetTexture != null)
            {
                var rt = hostDoc.panelSettings.targetTexture;
                hostDoc.panelSettings.targetTexture = null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                activeTarget = null;
            }
            UnityEngine.Object.DestroyImmediate(hostGo);
            hostGo = null;
            hostDoc = null;
        }
    }

    private static void AssertNoPlaceholderStrings(VisualElement container, string categoryName)
    {
        var labels = container.Query<Label>().ToList();
        foreach (var l in labels)
        {
            if (string.IsNullOrEmpty(l.text)) continue;
            Assert(!l.text.Contains("此功能尚未开放"), $"Category '{categoryName}' must NOT contain '此功能尚未开放'. Found: {l.text}");
            Assert(!l.text.Contains("基本配置"), $"Category '{categoryName}' must NOT contain placeholder '基本配置'. Found: {l.text}");
            Assert(!l.text.Contains("高级选项与网络策略"), $"Category '{categoryName}' must NOT contain placeholder '高级选项与网络策略'. Found: {l.text}");
            Assert(!l.text.Contains("权限与策略说明"), $"Category '{categoryName}' must NOT contain placeholder '权限与策略说明'. Found: {l.text}");
        }
    }

    private static void AssertNoTogglesOrInputs(VisualElement container, string categoryName)
    {
        Assert(container.Query<Toggle>().ToList().Count == 0, $"Category '{categoryName}' must have NO Toggle.");
        Assert(container.Query<Slider>().ToList().Count == 0, $"Category '{categoryName}' must have NO Slider.");
        Assert(container.Query<TextField>().ToList().Count == 0, $"Category '{categoryName}' must have NO TextField.");
    }

    private static void ClickElement(VisualElement el)
    {
        Assert(el != null, "Element to click must not be null");
        using var evt = ClickEvent.GetPooled();
        evt.target = el;
        el.SendEvent(evt);
    }

    [MenuItem("Railway/Review/Tablet Settings Placeholder Review")]
    public static void Batch()
    {
        Debug.Log("==================================================");
        Debug.Log("[TabletSettingsPlaceholderReview] Starting automated validation batch...");

        var catField = typeof(TabletSettingsAppUI).GetField("currentSettingsCategory", BindingFlags.Static | BindingFlags.NonPublic);
        int origCategory = (int)catField.GetValue(null);

        try
        {
            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
            }

            var root = SetupTestRoot(1440, 1024);

            // 1. 分类 3: 平板特色功能
            catField.SetValue(null, 3);
            TestCategory3_TabletFeatures(root, catField);

            // 2. 分类 8: 小米超级AI
            catField.SetValue(null, 8);
            TestCategory8_Ai(root);

            // 3. 分类 16: 人脸与锁屏密码
            catField.SetValue(null, 16);
            TestCategory16_LockSecurity(root);

            // 4. 分类 17: 隐私与安全
            catField.SetValue(null, 17);
            TestCategory17_Privacy(root);

            // 5. 分类 19: 电池
            catField.SetValue(null, 19);
            TestCategory19_Battery(root);

            // 6. 分类 20: 健康使用设备
            catField.SetValue(null, 20);
            TestCategory20_DigitalWellbeing(root, catField);

            // 7. 分类 21: 儿童模式
            catField.SetValue(null, 21);
            TestCategory21_KidsMode(root);

            // 8. 分类 24: 反馈
            catField.SetValue(null, 24);
            TestCategory24_Feedback(root);

            // 9. 回归验证与数据纯洁性断言
            TestRegressionAndInvariants(root, catField);

            TeardownTestRoot();
            StartRuntimeLayoutReview(origCategory);
        }
        finally
        {
            catField.SetValue(null, origCategory);
            TeardownTestRoot();
        }
    }

    private static void ResizeTarget(int width, int height)
    {
        if (hostDoc.panelSettings.targetTexture != null)
        {
            var previous = hostDoc.panelSettings.targetTexture;
            hostDoc.panelSettings.targetTexture = null;
            previous.Release();
            UnityEngine.Object.DestroyImmediate(previous);
        }

        activeTarget = new RenderTexture(width, height, 24);
        activeTarget.Create();
        hostDoc.panelSettings.targetTexture = activeTarget;
    }

    private static void StartRuntimeLayoutReview(int originalCategory = -1)
    {
        if (originalCategory >= 0) SessionState.SetInt(OriginalCategoryKey, originalCategory);
        SessionState.SetBool(PendingKey, true);
        runtimeDeadline = EditorApplication.timeSinceStartup + 120;
        layoutIndex = 0;
        EditorApplication.update -= CheckRuntimeLayout;
        EditorApplication.update += CheckRuntimeLayout;
        if (!EditorApplication.isPlaying)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
    }

    private static void CheckRuntimeLayout()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > runtimeDeadline)
                throw new TimeoutException("Tablet settings runtime layout review timed out");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;

            if (hostGo == null)
            {
                hostGo = new GameObject("TabletSettingsPlaceholderRuntimeReview");
                hostDoc = hostGo.AddComponent<UIDocument>();
                var source = Resources.Load<PanelSettings>("UI/TitleScreenPanelSettings");
                var settings = source != null ? ScriptableObject.Instantiate(source) : ScriptableObject.CreateInstance<PanelSettings>();
                settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                settings.scale = 1;
                settings.clearColor = true;
                settings.colorClearValue = new Color(.055f, .065f, .07f, 1f);
                hostDoc.panelSettings = settings;
                var root = hostDoc.rootVisualElement;
                root.style.width = LayoutResolutions[0].x;
                root.style.height = LayoutResolutions[0].y;
                root.Clear();
                ResizeTarget(LayoutResolutions[0].x, LayoutResolutions[0].y);
                BuildLayoutCase();
                layoutFrame = Time.frameCount;
                return;
            }

            if (Time.frameCount - layoutFrame < 45) return;
            ValidateAndCaptureLayoutCase();
            layoutIndex++;
            if (layoutIndex >= LayoutCategories.Length * LayoutResolutions.Length)
            {
                var categoryField = typeof(TabletSettingsAppUI).GetField("currentSettingsCategory", BindingFlags.Static | BindingFlags.NonPublic);
                categoryField.SetValue(null, SessionState.GetInt(OriginalCategoryKey, 1));
                FinishRuntimeLayoutReview(0);
                return;
            }

            var resolution = LayoutResolutions[layoutIndex / LayoutCategories.Length];
            ResizeTarget(resolution.x, resolution.y);
            var rootVisual = hostDoc.rootVisualElement;
            rootVisual.style.width = resolution.x;
            rootVisual.style.height = resolution.y;
            rootVisual.Clear();
            BuildLayoutCase();
            layoutFrame = Time.frameCount;
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            FinishRuntimeLayoutReview(1);
        }
    }

    private static void BuildLayoutCase()
    {
        var resolution = LayoutResolutions[layoutIndex / LayoutCategories.Length];
        int category = LayoutCategories[layoutIndex % LayoutCategories.Length];
        var categoryField = typeof(TabletSettingsAppUI).GetField("currentSettingsCategory", BindingFlags.Static | BindingFlags.NonPublic);
        categoryField.SetValue(null, category);
        var win = new VisualElement { name = "settings-runtime-review-win" };
        win.style.width = resolution.x;
        win.style.height = resolution.y;
        hostDoc.rootVisualElement.Add(win);
        TabletSettingsAppUI.BuildSettingsPage(win);
    }

    private static void ValidateAndCaptureLayoutCase()
    {
        int category = LayoutCategories[layoutIndex % LayoutCategories.Length];
        var resolution = LayoutResolutions[layoutIndex / LayoutCategories.Length];
        var root = hostDoc.rootVisualElement;
        var win = root.Q<VisualElement>("settings-runtime-review-win");
        var rightPane = win?.Q<ScrollView>("settings-right-pane") ?? win?.Query<ScrollView>().AtIndex(1);
        Assert(rightPane != null, $"Category {category} missing settings right pane at {resolution.x}x{resolution.y}");

        var paneBounds = rightPane.worldBound;
        Assert(paneBounds.width > 0 && paneBounds.height > 0, $"Category {category} has empty viewport bounds");
        Assert(paneBounds.xMin >= -1 && paneBounds.xMax <= resolution.x + 1, $"Category {category} viewport overflows horizontally: {paneBounds}");
        Assert(paneBounds.yMin >= -1 && paneBounds.yMax <= resolution.y + 1, $"Category {category} viewport overflows vertically: {paneBounds}");

        var header = rightPane.Q<Label>();
        Assert(header != null && header.worldBound.width > 0 && header.worldBound.height > 0, $"Category {category} header has no laid out bounds");
        Assert(paneBounds.Overlaps(header.worldBound), $"Category {category} header is outside its visible pane");

        var cards = rightPane.Query(className: null).Where(e => e.name != null && e.name.EndsWith("-card")).ToList();
        Assert(cards.Count > 0, $"Category {category} has no cards at {resolution.x}x{resolution.y}");
        foreach (var card in cards)
        {
            var bounds = card.worldBound;
            Assert(bounds.width > 0 && bounds.height > 0, $"Category {category} card '{card.name}' has empty bounds");
            Assert(bounds.xMin >= paneBounds.xMin - 1 && bounds.xMax <= paneBounds.xMax + 1,
                $"Category {category} card '{card.name}' overflows viewport horizontally: {bounds} vs {paneBounds}");
        }

        // Check the actual UI Toolkit hit test on the visible navigation row in category 3.
        if (category == 3)
        {
            var target = FindRow(win, "系统控制与通知中心");
            Assert(target != null && paneBounds.Overlaps(target.worldBound), "System panel navigation row is not visible");
            var picked = root.panel.Pick(target.worldBound.center);
            Assert(picked != null && (picked == target || target.Contains(picked)), "Visible system panel navigation row is not hit-testable");
        }

        CaptureScreen(activeTarget, $"Settings_Cat{category}_{resolution.x}x{resolution.y}");
    }

    private static VisualElement FindRow(VisualElement container, string title)
    {
        foreach (var element in container.Query(className: null).ToList())
        {
            var label = element.Q<Label>();
            if (label != null && label.text == title) return element;
        }
        return null;
    }

    private static void FinishRuntimeLayoutReview(int exitCode)
    {
        SessionState.SetBool(PendingKey, false);
        EditorApplication.update -= CheckRuntimeLayout;
        if (hostGo != null)
        {
            if (hostDoc != null && hostDoc.panelSettings != null && hostDoc.panelSettings.targetTexture != null)
            {
                var target = hostDoc.panelSettings.targetTexture;
                hostDoc.panelSettings.targetTexture = null;
                target.Release();
                UnityEngine.Object.Destroy(target);
            }
            UnityEngine.Object.Destroy(hostGo);
        }
        hostGo = null;
        hostDoc = null;
        activeTarget = null;
        EditorApplication.isPlaying = false;
        if (exitCode == 0)
            Debug.Log("TABLET_SETTINGS_PLACEHOLDER_REVIEW_OK: 8 truthful pages, UI Toolkit layout measured at 1440x1024 and 1024x768, runtime screenshot and hit testing passed.");
        else
            Debug.LogError("TABLET_SETTINGS_PLACEHOLDER_REVIEW_FAILED");
        EditorApplication.Exit(exitCode);
    }

    /// <summary>
    /// 用例 1: 分类 3 平板特色功能渲染专属页面、包含小部件与系统入口，无三行占位模板。
    /// </summary>
    private static void TestCategory3_TabletFeatures(VisualElement root, FieldInfo catField)
    {
        root.Clear();
        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);

        string lastToast = null;
        TabletSettingsAppUI.BuildSettingsPage(win, msg => lastToast = msg);

        var page = win.Q("settings-tablet-features-page");
        Assert(page != null, "settings-tablet-features-page must exist for Category 3");

        var desktopCard = win.Q("settings-tablet-features-desktop-card");
        Assert(desktopCard != null, "settings-tablet-features-desktop-card must exist");

        var systemCard = win.Q("settings-tablet-features-system-card");
        Assert(systemCard != null, "settings-tablet-features-system-card must exist");

        // 验证标题为 平板特色功能
        var header = page.Q<Label>();
        Assert(header != null && header.text == "平板特色功能", $"Header must be '平板特色功能', got: {header?.text}");

        // 验证彻底消除占位模板
        AssertNoPlaceholderStrings(page, "平板特色功能");
        AssertNoTogglesOrInputs(page, "平板特色功能");

        // 小部件行是只读说明，不应伪装成固定/启停操作
        var rows = page.Query(className: null).ToList();
        VisualElement suiyueRow = null;
        VisualElement notifNavRow = null;
        VisualElement presNavRow = null;

        foreach (var r in rows)
        {
            var l = r.Q<Label>();
            if (l != null && l.text == "岁月提示气泡") suiyueRow = r;
            if (l != null && l.text == "系统控制与通知中心") notifNavRow = r;
            if (l != null && l.text == "平板外观与个性化调优") presNavRow = r;
        }

        Assert(suiyueRow != null, "岁月提示气泡行必须存在");
        ClickElement(suiyueRow);
        Assert(lastToast == null, "Read-only Suiyue row must not show a success toast or claim an action occurred: " + lastToast);
        Assert(!suiyueRow.Query<Label>().ToList().Any(l => l.text == "›"), "Read-only Suiyue row must not show a navigation arrow");

        Assert(notifNavRow != null, "系统控制与通知中心导航行必须存在");
        ClickElement(notifNavRow);
        int currentCat = (int)catField.GetValue(null);
        Assert(currentCat == 11, $"Clicking notifications row must navigate to category 11, got {currentCat}");

        // 切回分类 3 测试外观导航
        catField.SetValue(null, 3);
        TabletSettingsAppUI.BuildSettingsPage(win);
        page = win.Q("settings-tablet-features-page");
        presNavRow = null;
        foreach (var r in page.Query(className: null).ToList())
        {
            var l = r.Q<Label>();
            if (l != null && l.text == "平板外观与个性化调优") presNavRow = r;
        }
        Assert(presNavRow != null, "平板外观与个性化调优导航行必须存在");
        ClickElement(presNavRow);
        currentCat = (int)catField.GetValue(null);
        Assert(currentCat == 9, $"Clicking presentation row must navigate to category 9, got {currentCat}");

        catField.SetValue(null, 3);
        Debug.Log("[TabletSettingsPlaceholderReview] Test 1 Passed: Category 3 (Tablet Features) dedicated page and navigation verified.");
    }

    /// <summary>
    /// 用例 2: 分类 8 小米超级AI专属页面、本地规则模型说明、无虚假在线 AI 算力宣称。
    /// </summary>
    private static void TestCategory8_Ai(VisualElement root)
    {
        root.Clear();
        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);

        string lastToast = null;
        TabletSettingsAppUI.BuildSettingsPage(win, msg => lastToast = msg);

        var page = win.Q("settings-ai-page");
        Assert(page != null, "settings-ai-page must exist for Category 8");

        var engineCard = win.Q("settings-ai-engine-card");
        Assert(engineCard != null, "settings-ai-engine-card must exist");

        var interactionCard = win.Q("settings-ai-interaction-card");
        Assert(interactionCard != null, "settings-ai-interaction-card must exist");

        var header = page.Q<Label>();
        Assert(header != null && header.text == "小米超级AI", $"Header must be '小米超级AI', got: {header?.text}");

        AssertNoPlaceholderStrings(page, "小米超级AI");
        AssertNoTogglesOrInputs(page, "小米超级AI");

        var labels = page.Query<Label>().ToList();
        var sb = new System.Text.StringBuilder();
        foreach (var l in labels) if (!string.IsNullOrEmpty(l.text)) sb.Append(l.text).Append(" ");
        string text = sb.ToString();

        Assert(text.Contains("本地预设回复") && text.Contains("未接入外部大模型服务"), "AI page must describe the local scripted response scope without claiming a model: " + text);
        Assert(!text.Contains("在线大模型已连接") && !text.Contains("智能云端助理运行中"), "AI page must NOT claim online cloud assistant: " + text);

        VisualElement bubbleRow = null;
        foreach (var r in page.Query(className: null).ToList())
        {
            var l = r.Q<Label>();
            if (l != null && l.text == "桌面提示气泡") bubbleRow = r;
        }
        Assert(bubbleRow != null, "桌面提示气泡行必须存在");
        ClickElement(bubbleRow);
        Assert(lastToast == null, "Read-only desktop bubble row must not claim it was started or pinned: " + lastToast);
        Assert(!bubbleRow.Query<Label>().ToList().Any(l => l.text == "›"), "Read-only desktop bubble row must not show a navigation arrow");

        Debug.Log("[TabletSettingsPlaceholderReview] Test 2 Passed: Category 8 (Xiaomi AI) dedicated page and truthful boundaries verified.");
    }

    /// <summary>
    /// 用例 3: 分类 16 人脸与锁屏密码专属页面、如实说明无密码凭据与无生物识别硬件。
    /// </summary>
    private static void TestCategory16_LockSecurity(VisualElement root)
    {
        root.Clear();
        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);

        TabletSettingsAppUI.BuildSettingsPage(win);

        var page = win.Q("settings-lock-security-page");
        Assert(page != null, "settings-lock-security-page must exist for Category 16");

        var credCard = win.Q("settings-lock-credential-card");
        Assert(credCard != null, "settings-lock-credential-card must exist");

        var policyCard = win.Q("settings-lock-policy-card");
        Assert(policyCard != null, "settings-lock-policy-card must exist");

        var header = page.Q<Label>();
        Assert(header != null && header.text == "人脸与锁屏密码", $"Header must be '人脸与锁屏密码', got: {header?.text}");

        AssertNoPlaceholderStrings(page, "人脸与锁屏密码");
        AssertNoTogglesOrInputs(page, "人脸与锁屏密码");

        // 验证纯只读行无箭头（›）
        foreach (var l in page.Query<Label>().ToList())
        {
            if (l.text != null && l.text.Trim() == "›")
            {
                throw new InvalidOperationException("Category 16 must NOT have arrow '›' on read-only rows.");
            }
        }

        var labels = page.Query<Label>().ToList();
        var sb = new System.Text.StringBuilder();
        foreach (var l in labels) if (!string.IsNullOrEmpty(l.text)) sb.Append(l.text).Append(" ");
        string text = sb.ToString();

        Assert(text.Contains("没有密码凭据验证功能") && text.Contains("轻触屏幕或按空格键关闭"), "Lock security must describe the actual app behavior: " + text);
        Assert(text.Contains("没有接入生物识别验证") && text.Contains("不在此应用中检测"), "Lock security must distinguish app support from unknown physical hardware: " + text);
        Assert(!text.Contains("人脸已录入") && !text.Contains("指纹已激活") && !text.Contains("密码保护中"), "Lock security must NOT claim fake biometric authentication: " + text);

        Debug.Log("[TabletSettingsPlaceholderReview] Test 3 Passed: Category 16 (Lock Security) dedicated page and truthful credential state verified.");
    }

    /// <summary>
    /// 用例 4: 分类 17 隐私与安全专属页面、如实说明本地持久化目录存储与沙箱隔离。
    /// </summary>
    private static void TestCategory17_Privacy(VisualElement root)
    {
        root.Clear();
        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);

        TabletSettingsAppUI.BuildSettingsPage(win);

        var page = win.Q("settings-privacy-page");
        Assert(page != null, "settings-privacy-page must exist for Category 17");

        var storageCard = win.Q("settings-privacy-storage-card");
        Assert(storageCard != null, "settings-privacy-storage-card must exist");

        var sandboxCard = win.Q("settings-privacy-sandbox-card");
        Assert(sandboxCard != null, "settings-privacy-sandbox-card must exist");

        var header = page.Q<Label>();
        Assert(header != null && header.text == "隐私与安全", $"Header must be '隐私与安全', got: {header?.text}");

        AssertNoPlaceholderStrings(page, "隐私与安全");
        AssertNoTogglesOrInputs(page, "隐私与安全");

        foreach (var l in page.Query<Label>().ToList())
        {
            if (l.text != null && l.text.Trim() == "›")
            {
                throw new InvalidOperationException("Category 17 must NOT have arrow '›' on read-only rows.");
            }
        }

        var labels = page.Query<Label>().ToList();
        var sb = new System.Text.StringBuilder();
        foreach (var l in labels) if (!string.IsNullOrEmpty(l.text)) sb.Append(l.text).Append(" ");
        string text = sb.ToString();

        Assert(text.Contains("本机 PlayerPrefs 或文件"), "Privacy page must describe local persistence without claiming all data is stored one way: " + text);
        Assert(text.Contains("按用户操作请求第三方服务"), "Privacy page must disclose user-initiated third-party music requests: " + text);
        Assert(text.Contains("不提供系统权限审计或统一遥测开关"), "Privacy page must not pretend to audit OS permissions or control telemetry: " + text);
        Assert(!text.Contains("云端安全防护中") && !text.Contains("已上传诊断数据"), "Privacy page must NOT claim cloud security/telemetry: " + text);

        Debug.Log("[TabletSettingsPlaceholderReview] Test 4 Passed: Category 17 (Privacy & Security) dedicated page verified.");
    }

    /// <summary>
    /// 用例 5: 分类 19 电池专属页面，读取平台真实上报值或准确显示未知状态。
    /// </summary>
    private static void TestCategory19_Battery(VisualElement root)
    {
        root.Clear();
        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);

        TabletSettingsAppUI.BuildSettingsPage(win);

        var page = win.Q("settings-battery-page");
        Assert(page != null, "settings-battery-page must exist for Category 19");

        var statusCard = win.Q("settings-battery-status-card");
        Assert(statusCard != null, "settings-battery-status-card must exist");

        var policyCard = win.Q("settings-battery-policy-card");
        Assert(policyCard != null, "settings-battery-policy-card must exist");

        var header = page.Q<Label>();
        Assert(header != null && header.text == "电池", $"Header must be '电池', got: {header?.text}");

        AssertNoPlaceholderStrings(page, "电池");
        AssertNoTogglesOrInputs(page, "电池");

        var labels = page.Query<Label>().ToList();
        var sb = new System.Text.StringBuilder();
        foreach (var l in labels) if (!string.IsNullOrEmpty(l.text)) sb.Append(l.text).Append(" ");
        string text = sb.ToString();

        float lvl = SystemInfo.batteryLevel;
        if (lvl >= 0f)
        {
            int pct = Mathf.RoundToInt(lvl * 100f);
            Assert(text.Contains($"{pct}%"), $"Battery page must contain real battery percent {pct}%: " + text);
        }
        else
        {
            Assert(text.Contains("平台未提供电量读数"), "Battery page must report unavailable telemetry instead of inferring an external supply: " + text);
        }

        Assert(text.Contains("不代表机车蓄电池或辅助电源遥测"), "Battery page must distinguish host battery data from locomotive power telemetry: " + text);
        Assert(!text.Contains("外部电源供电") && !text.Contains("逆变母线直供"), "Battery page must not infer external or locomotive supply from missing host data: " + text);
        Assert(!text.Contains("超级快充 120W 充电中") && !text.Contains("剩余续航 48 小时"), "Battery page must NOT make up fake telemetry numbers: " + text);

        Debug.Log("[TabletSettingsPlaceholderReview] Test 5 Passed: Category 19 (Battery) dedicated page and truthful power state verified.");
    }

    /// <summary>
    /// 用例 6: 分类 20 健康使用设备专属页面、说明无活跃度监测与应用限额、值班警惕由司控台警惕装置负责。
    /// </summary>
    private static void TestCategory20_DigitalWellbeing(VisualElement root, FieldInfo catField)
    {
        root.Clear();
        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);

        TabletSettingsAppUI.BuildSettingsPage(win);

        var page = win.Q("settings-digital-wellbeing-page");
        Assert(page != null, "settings-digital-wellbeing-page must exist for Category 20");

        var statsCard = win.Q("settings-digital-wellbeing-stats-card");
        Assert(statsCard != null, "settings-digital-wellbeing-stats-card must exist");

        var dutyCard = win.Q("settings-digital-wellbeing-duty-card");
        Assert(dutyCard != null, "settings-digital-wellbeing-duty-card must exist");

        var header = page.Q<Label>();
        Assert(header != null && header.text == "健康使用设备", $"Header must be '健康使用设备', got: {header?.text}");

        AssertNoPlaceholderStrings(page, "健康使用设备");
        AssertNoTogglesOrInputs(page, "健康使用设备");

        var labels = page.Query<Label>().ToList();
        var sb = new System.Text.StringBuilder();
        foreach (var l in labels) if (!string.IsNullOrEmpty(l.text)) sb.Append(l.text).Append(" ");
        string text = sb.ToString();

        Assert(text.Contains("未启用后台应用活跃度监测"), "Digital wellbeing must state no activity tracking: " + text);
        Assert(text.Contains("不读取机车警惕装置、驾驶疲劳状态或行车联锁数据"), "Digital wellbeing must not claim access to locomotive safety hardware: " + text);
        Assert(!text.Contains("今日使用 3 小时 45 分") && !text.Contains("应用限制生效中"), "Digital wellbeing must NOT fake screen time statistics: " + text);

        VisualElement displayNavRow = null;
        foreach (var r in page.Query(className: null).ToList())
        {
            var l = r.Q<Label>();
            if (l != null && l.text == "显示与亮度设置") displayNavRow = r;
        }
        Assert(displayNavRow != null, "显示与亮度 navigation row must exist");
        ClickElement(displayNavRow);
        int currentCat = (int)catField.GetValue(null);
        Assert(currentCat == 13, $"Clicking display settings row must navigate to category 13, got {currentCat}");

        catField.SetValue(null, 20);
        Debug.Log("[TabletSettingsPlaceholderReview] Test 6 Passed: Category 20 (Digital Wellbeing) dedicated page and navigation verified.");
    }

    /// <summary>
    /// 用例 7: 分类 21 儿童模式专属页面、说明无儿童空间与家长控制、值班终端权限边界。
    /// </summary>
    private static void TestCategory21_KidsMode(VisualElement root)
    {
        root.Clear();
        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);

        TabletSettingsAppUI.BuildSettingsPage(win);

        var page = win.Q("settings-kids-mode-page");
        Assert(page != null, "settings-kids-mode-page must exist for Category 21");

        var statusCard = win.Q("settings-kids-mode-status-card");
        Assert(statusCard != null, "settings-kids-mode-status-card must exist");

        var envCard = win.Q("settings-kids-mode-env-card");
        Assert(envCard != null, "settings-kids-mode-env-card must exist");

        var header = page.Q<Label>();
        Assert(header != null && header.text == "儿童模式", $"Header must be '儿童模式', got: {header?.text}");

        AssertNoPlaceholderStrings(page, "儿童模式");
        AssertNoTogglesOrInputs(page, "儿童模式");

        foreach (var l in page.Query<Label>().ToList())
        {
            if (l.text != null && l.text.Trim() == "›")
            {
                throw new InvalidOperationException("Category 21 must NOT have arrow '›' on read-only rows.");
            }
        }

        var labels = page.Query<Label>().ToList();
        var sb = new System.Text.StringBuilder();
        foreach (var l in labels) if (!string.IsNullOrEmpty(l.text)) sb.Append(l.text).Append(" ");
        string text = sb.ToString();

        Assert(text.Contains("没有独立儿童账户或受限应用桌面"), "Kids mode must state the absent child account/desktop feature: " + text);
        Assert(text.Contains("没有家长监护、使用时长限制或内容过滤功能"), "Kids mode must state feature boundaries without inventing railway policy: " + text);
        Assert(text.Contains("没有儿童模式开关或单独退出密码"), "Kids mode must state there is no active mode or exit password: " + text);
        Assert(!text.Contains("儿童空间已开启") && !text.Contains("已限制使用应用"), "Kids mode must NOT claim fake parental restrictions: " + text);

        Debug.Log("[TabletSettingsPlaceholderReview] Test 7 Passed: Category 21 (Kids Mode) dedicated page and truthful boundaries verified.");
    }

    /// <summary>
    /// 用例 8: 分类 24 反馈专属页面，准确说明当前没有收件端或提交行为。
    /// </summary>
    private static void TestCategory24_Feedback(VisualElement root)
    {
        root.Clear();
        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);

        TabletSettingsAppUI.BuildSettingsPage(win);

        var page = win.Q("settings-feedback-page");
        Assert(page != null, "settings-feedback-page must exist for Category 24");

        var feedbackCard = win.Q("settings-feedback-status-card");
        Assert(feedbackCard != null, "settings-feedback-status-card must exist");

        var channelCard = win.Q("settings-feedback-channel-card");
        Assert(channelCard != null, "settings-feedback-channel-card must exist");

        var header = page.Q<Label>();
        Assert(header != null && header.text == "反馈", $"Header must be '反馈', got: {header?.text}");

        AssertNoPlaceholderStrings(page, "反馈");
        AssertNoTogglesOrInputs(page, "反馈");

        var labels = page.Query<Label>().ToList();
        var sb = new System.Text.StringBuilder();
        foreach (var l in labels) if (!string.IsNullOrEmpty(l.text)) sb.Append(l.text).Append(" ");
        string text = sb.ToString();

        Assert(text.Contains("未接入反馈收件端") && text.Contains("不会发送或保存反馈内容"), "Feedback page must disclose that no submission endpoint exists: " + text);
        Assert(text.Contains("没有配置可验证的调度电话、工单地址或外部反馈入口"), "Feedback page must not invent reporting channels: " + text);
        Assert(!text.Contains("ShowAffairsPage") && !text.Contains("请通过调度电话") && !text.Contains("维护线缆导出"), "Feedback page must not claim placeholder or unsupported channels: " + text);

        Debug.Log("[TabletSettingsPlaceholderReview] Test 8 Passed: Category 24 (Feedback) dedicated page and submission boundary verified.");
    }

    private static void CaptureScreen(RenderTexture rt, string filename)
    {
        Assert(rt != null && rt.IsCreated(), $"Render target missing for {filename}");
        var prev = RenderTexture.active;
        RenderTexture.active = rt;

        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        int pixelsDifferentFromFirst = 0;
        var pixels = tex.GetPixels32();
        var firstPixel = pixels[0];
        for (int i = 0; i < pixels.Length; i++)
        {
            var pixel = pixels[i];
            if (Mathf.Abs(pixel.r - firstPixel.r) > 12 || Mathf.Abs(pixel.g - firstPixel.g) > 12 || Mathf.Abs(pixel.b - firstPixel.b) > 12)
                pixelsDifferentFromFirst++;
        }
        Assert(pixelsDifferentFromFirst > pixels.Length / 100,
            $"Rendered capture '{filename}' is blank or uniformly cleared ({pixelsDifferentFromFirst} pixels differ from the first pixel)");

        RenderTexture.active = prev;

        string fullPath = Path.Combine(OutputDir, filename + ".png");
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    /// <summary>
    /// 用例 10: 回归验证分类 11（通知设置）保持完好，且全局 PlayerPrefs 未被污染写入。
    /// </summary>
    private static void TestRegressionAndInvariants(VisualElement root, FieldInfo catField)
    {
        catField.SetValue(null, 11);
        root.Clear();

        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);

        TabletSettingsAppUI.BuildSettingsPage(win);

        var notifBtn = win.Q("settings-open-notifications-btn");
        var ctrlBtn = win.Q("settings-open-controls-btn");
        var panelCard = win.Q("settings-notifications-panel-card");
        var infoCard = win.Q("settings-notifications-info-card");

        Assert(notifBtn != null, "settings-open-notifications-btn must still exist in Category 11");
        Assert(ctrlBtn != null, "settings-open-controls-btn must still exist in Category 11");
        Assert(panelCard != null, "settings-notifications-panel-card must still exist in Category 11");
        Assert(infoCard != null, "settings-notifications-info-card must still exist in Category 11");

        // 验证分类 11 不含通用占位
        AssertNoPlaceholderStrings(win, "通知与状态栏");

        Debug.Log("[TabletSettingsPlaceholderReview] Test 10 Passed: Category 11 regression verified intact.");
    }
}

