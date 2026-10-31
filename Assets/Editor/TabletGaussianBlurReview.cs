using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UI.Tablet.Rendering;

namespace UI.Tablet.Editor
{
    /// <summary>
    /// Standalone Play Mode & batch review entry point for the Tablet Gaussian Blur module.
    /// Executes isolated normalization checks, pixel contrast reduction, ownership guarantees,
    /// dynamic resizing, overlay exclusion, and event routing without saving game scenes.
    /// </summary>
    [InitializeOnLoad]
    public static class TabletGaussianBlurReview
    {
        private const string PendingKey = "TabletBlurReview.Pending";
        private const string StageKey = "TabletBlurReview.Stage";
        private const string OutputDir = "tools/compile/tablet_blur_review";
        private const float TimeoutSeconds = 20.0f;

        private static double s_deadline;
        private static int s_frame;
        private static GameObject s_reviewHost;
        private static TabletBlurPreview s_preview;
        private static Color s_preOverlayColorSample;

        static TabletGaussianBlurReview()
        {
            if (SessionState.GetBool(PendingKey, false))
            {
                StartPolling();
            }
        }

        [MenuItem("Tools/Tablet Blur/Run Review Batch")]
        public static void Batch()
        {
            EnsureOutputDirectory();
            Debug.Log("[TabletBlurReview] Starting Standalone Batch Review...");

            try
            {
                // Run direct service algorithmic assertions first (fail-fast)
                RunServiceAlgorithmicChecks();
            }
            catch (Exception ex)
            {
                WriteMetricsFailure("Algorithmic checks failed: " + ex.Message);
                FailAndFinish("Algorithmic check exception: " + ex.Message);
                return;
            }

            // Set up domain-reload-safe Play Mode harness for UI capture and interaction
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(StageKey, 1);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            StartPolling();
            EditorApplication.isPlaying = true;
        }

        private static void EnsureOutputDirectory()
        {
            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
            }
        }

        private static void StartPolling()
        {
            s_deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void RunServiceAlgorithmicChecks()
        {
            using (var service = new TabletGaussianBlur { Downsample = 1, Sigma = 2.5f })
            {
                if (!service.IsSupported)
                {
                    throw new InvalidOperationException("TabletGaussianBlur is not supported: " + service.LastError);
                }

                // 1. Normalization & constant color preservation
                int w = 64, h = 64;
                var flatRT = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                Color testColor = new Color(0.35f, 0.55f, 0.75f, 1.0f);
                Graphics.Blit(Texture2D.whiteTexture, flatRT);

                // Fill with constant color
                var flatFillTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        flatFillTex.SetPixel(x, y, testColor);
                    }
                }
                flatFillTex.Apply();
                Graphics.Blit(flatFillTex, flatRT);

                var flatOut = service.Process(flatRT);
                var flatRead = ReadBackTexture(flatOut);
                Color midColor = flatRead.GetPixel(w / 2, h / 2);

                float diffR = Mathf.Abs(midColor.r - testColor.r);
                float diffG = Mathf.Abs(midColor.g - testColor.g);
                float diffB = Mathf.Abs(midColor.b - testColor.b);
                float maxDelta = Mathf.Max(diffR, Mathf.Max(diffG, diffB));

                RenderTexture.ReleaseTemporary(flatRT);
                UnityEngine.Object.DestroyImmediate(flatFillTex);
                UnityEngine.Object.DestroyImmediate(flatRead);

                if (maxDelta > 0.03f)
                {
                    throw new Exception($"Normalization failed. Constant color delta {maxDelta:F4} exceeded 0.03.");
                }

                // 2. High-Frequency contrast reduction on Checkerboard
                service.Sigma = 3.0f;
                int cw = 128, ch = 128;
                var checkerTex = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
                for (int y = 0; y < ch; y++)
                {
                    for (int x = 0; x < cw; x++)
                    {
                        bool isWhite = ((x / 8) + (y / 8)) % 2 == 0;
                        checkerTex.SetPixel(x, y, isWhite ? Color.white : Color.black);
                    }
                }
                checkerTex.Apply();

                var checkerRT = RenderTexture.GetTemporary(cw, ch, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(checkerTex, checkerRT);

                var srcTex = ReadBackTexture(checkerRT);
                float srcStdDev = ComputeStdDev(srcTex);
                SavePng(srcTex, Path.Combine(OutputDir, "checker_source.png"));

                var blurredRT = service.Process(checkerRT);
                var blurTex = ReadBackTexture(blurredRT);
                float blurStdDev = ComputeStdDev(blurTex);
                SavePng(blurTex, Path.Combine(OutputDir, "checker_blurred.png"));

                float contrastReduction = (srcStdDev - blurStdDev) / Mathf.Max(0.0001f, srcStdDev);

                RenderTexture.ReleaseTemporary(checkerRT);
                UnityEngine.Object.DestroyImmediate(checkerTex);
                UnityEngine.Object.DestroyImmediate(srcTex);
                UnityEngine.Object.DestroyImmediate(blurTex);

                if (contrastReduction < 0.40f)
                {
                    throw new Exception($"High-frequency contrast reduction insufficient: {contrastReduction:P1} < 40%.");
                }

                // 3. Distinct source / output ownership
                var ownSrc = RenderTexture.GetTemporary(64, 64, 0, RenderTextureFormat.ARGB32);
                var ownOut = service.Process(ownSrc);
                if (ownOut == ownSrc)
                {
                    RenderTexture.ReleaseTemporary(ownSrc);
                    throw new Exception("Blur service mutated or returned source texture directly.");
                }
                RenderTexture.ReleaseTemporary(ownSrc);

                // 4. Resize reallocation / reuse
                service.Downsample = 2;
                var resA = RenderTexture.GetTemporary(100, 100, 0, RenderTextureFormat.ARGB32);
                var outA = service.Process(resA);
                int outAW = outA.width;
                int outAH = outA.height;
                RenderTexture.ReleaseTemporary(resA);

                var resB = RenderTexture.GetTemporary(200, 300, 0, RenderTextureFormat.ARGB32);
                var outB = service.Process(resB);
                int outBW = outB.width;
                int outBH = outB.height;
                RenderTexture.ReleaseTemporary(resB);

                if (outAW != 50 || outAH != 50 || outBW != 100 || outBH != 150)
                {
                    throw new Exception($"Reallocation dimension check failed. Got A=({outAW}x{outAH}), B=({outBW}x{outBH}).");
                }
            }

            // 5. Resource disposal integrity
            var dispService = new TabletGaussianBlur();
            var dispSrc = RenderTexture.GetTemporary(64, 64, 0, RenderTextureFormat.ARGB32);
            dispService.Process(dispSrc);
            dispService.Dispose();
            if (dispService.CurrentResult != null || dispService.IsSupported)
            {
                RenderTexture.ReleaseTemporary(dispSrc);
                throw new Exception("Disposal integrity check failed: internal resources still held.");
            }
            RenderTexture.ReleaseTemporary(dispSrc);

            Debug.Log("[TabletBlurReview] Algorithmic verification passed.");
        }

        private static void OnUpdate()
        {
            if (EditorApplication.timeSinceStartup > s_deadline)
            {
                WriteMetricsFailure("Timeout reached during review harness execution.");
                FailAndFinish("Review harness timed out.");
                return;
            }

            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;

            int stage = SessionState.GetInt(StageKey, 0);
            if (stage == 0) return;

            try
            {
                switch (stage)
                {
                    case 1:
                        // Spawn testbed host GameObject
                        s_reviewHost = new GameObject("[TabletBlur_ReviewHarness]");
                        s_reviewHost.hideFlags = HideFlags.DontSave;
                        s_reviewHost.AddComponent<UIDocument>();
                        s_preview = s_reviewHost.AddComponent<TabletBlurPreview>();

                        s_frame = Time.frameCount;
                        SessionState.SetInt(StageKey, 2);
                        break;

                    case 2:
                        // Wait 5 frames for offscreen background to render via WaitForEndOfFrame loop
                        if (Time.frameCount - s_frame < 5) return;

                        if (s_preview == null || s_preview.SourceTexture == null)
                        {
                            throw new Exception("Preview failed to initialize SourceTexture in offscreen panel.");
                        }

                        // Sample pixel before changing overlay color
                        var preTex = ReadBackTexture(s_preview.SourceTexture);
                        s_preOverlayColorSample = preTex.GetPixel(s_preview.SourceTexture.width / 2, s_preview.SourceTexture.height / 2);
                        UnityEngine.Object.DestroyImmediate(preTex);

                        // Change overlay background color to bright magenta
                        s_preview.SetOverlayBackgroundColor(Color.magenta);

                        s_frame = Time.frameCount;
                        SessionState.SetInt(StageKey, 3);
                        break;

                    case 3:
                        // Wait 3 frames to ensure overlay rendered
                        if (Time.frameCount - s_frame < 3) return;

                        // Check Overlay Exclusion: SourceTexture must NOT have picked up magenta from overlay
                        var postTex = ReadBackTexture(s_preview.SourceTexture);
                        Color postOverlayColorSample = postTex.GetPixel(s_preview.SourceTexture.width / 2, s_preview.SourceTexture.height / 2);
                        SavePng(postTex, Path.Combine(OutputDir, "preview_source.png"));
                        UnityEngine.Object.DestroyImmediate(postTex);

                        float sampleDiff = Vector4.Distance(s_preOverlayColorSample, postOverlayColorSample);
                        bool overlayExcluded = sampleDiff < 0.05f;

                        if (!overlayExcluded)
                        {
                            throw new Exception($"Overlay exclusion failed: source texture modified by overlay (delta={sampleDiff:F4}).");
                        }

                        // Save blurred result screenshot
                        if (s_preview.BlurredTexture != null)
                        {
                            var blurCapture = ReadBackTexture(s_preview.BlurredTexture);
                            SavePng(blurCapture, Path.Combine(OutputDir, "preview_blurred.png"));
                            UnityEngine.Object.DestroyImmediate(blurCapture);
                        }

                        // Exercise UI event paths
                        s_preview.SimulateBackgroundClick();
                        s_preview.SimulateOverlayClick();

                        bool eventsPassed = (s_preview.BackgroundClickCount == 1 && s_preview.OverlayClickCount == 1);
                        if (!eventsPassed)
                        {
                            throw new Exception($"UI simulated click events failed. BG={s_preview.BackgroundClickCount}, Overlay={s_preview.OverlayClickCount}");
                        }

                        // Write full metrics JSON
                        WriteMetricsSuccess();
                        FinishAndFinish(true, "All blur and preview checks passed.");
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteMetricsFailure(ex.ToString());
                FailAndFinish("Exception during review execution: " + ex.Message);
            }
        }

        private static float ComputeStdDev(Texture2D tex)
        {
            Color[] pixels = tex.GetPixels();
            if (pixels.Length == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < pixels.Length; i++)
            {
                sum += pixels[i].grayscale;
            }
            float mean = sum / pixels.Length;
            float sumSqDiff = 0f;
            for (int i = 0; i < pixels.Length; i++)
            {
                float diff = pixels[i].grayscale - mean;
                sumSqDiff += diff * diff;
            }
            return Mathf.Sqrt(sumSqDiff / pixels.Length);
        }

        private static Texture2D ReadBackTexture(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            return tex;
        }

        private static void SavePng(Texture2D tex, string path)
        {
            EnsureOutputDirectory();
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }

        private static void WriteMetricsSuccess()
        {
            EnsureOutputDirectory();
            string json = "{\n" +
                          "  \"passed\": true,\n" +
                          "  \"constantPreserved\": true,\n" +
                          "  \"contrastReduced\": true,\n" +
                          "  \"ownershipDistinct\": true,\n" +
                          "  \"reallocationVerified\": true,\n" +
                          "  \"disposalClean\": true,\n" +
                          "  \"overlayExclusionVerified\": true,\n" +
                          "  \"eventHandlingPassed\": true,\n" +
                          "  \"manualTouchQARequired\": [\n" +
                          "    \"Android multi-touch gestures\",\n" +
                          "    \"Stylus pressure and hover\",\n" +
                          "    \"Physical device thermal throttling on sustained blur\"\n" +
                          "  ]\n" +
                          "}";
            File.WriteAllText(Path.Combine(OutputDir, "metrics.json"), json);
        }

        private static void WriteMetricsFailure(string reason)
        {
            EnsureOutputDirectory();
            string escaped = reason.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");
            string json = "{\n" +
                          "  \"passed\": false,\n" +
                          $"  \"error\": \"{escaped}\"\n" +
                          "}";
            File.WriteAllText(Path.Combine(OutputDir, "metrics.json"), json);
        }

        private static void FailAndFinish(string reason)
        {
            FinishAndFinish(false, reason);
        }

        private static void FinishAndFinish(bool passed, string message)
        {
            SessionState.SetBool(PendingKey, false);
            SessionState.SetInt(StageKey, 0);
            EditorApplication.update -= OnUpdate;

            if (s_reviewHost != null)
            {
                UnityEngine.Object.DestroyImmediate(s_reviewHost);
                s_reviewHost = null;
                s_preview = null;
            }

            Debug.Log(passed ? $"[TabletBlurReview] SUCCESS: {message}" : $"[TabletBlurReview] FAILED: {message}");
            EditorApplication.isPlaying = false;

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(passed ? 0 : 1);
            }
        }
    }
}
