using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Tablet.Rendering
{
    /// <summary>
    /// Self-contained prototype testbed for isolated UI Toolkit capture, blur, and pointer interaction.
    /// Never automatically attached to production scenes.
    /// Manages an offscreen background panel settings and UIDocument rendered to a private RenderTexture,
    /// along with a foreground overlay UIDocument displaying the blurred background.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class TabletBlurPreview : MonoBehaviour, IDisposable
    {
        [Header("Configuration")]
        [SerializeField] private PanelSettings basePanelSettingsAsset;
        [SerializeField] private Vector2Int targetResolution = new Vector2Int(1280, 720);
        [Range(1, 4)] [SerializeField] private int downsample = 2;
        [Range(0.1f, 10f)] [SerializeField] private float blurSigma = 2.5f;

        // Runtime owned objects
        private TabletGaussianBlur _blurService;
        private PanelSettings _clonedBackgroundPanelSettings;
        private UIDocument _overlayDocument;
        private GameObject _backgroundGo;
        private UIDocument _backgroundDocument;
        private RenderTexture _sourceRT;
        private Coroutine _renderLoopCoroutine;

        // UI Visual Elements
        private VisualElement _blurredDisplayElement;
        private VisualElement _shellOverlay;
        private Label _statusLabel;
        private Button _toggleBlurButton;
        private Button _bgTestButton;
        private Button _overlayTestButton;

        // Debug & review properties
        public RenderTexture SourceTexture => _sourceRT;
        public RenderTexture BlurredTexture => _blurService?.CurrentResult;
        public bool IsBlurActive { get; set; } = true;
        public TabletGaussianBlur BlurService => _blurService;
        public int BackgroundClickCount { get; private set; }
        public int OverlayClickCount { get; private set; }
        public bool IsServiceSupported => _blurService != null && _blurService.IsSupported;
        public string ServiceLastError => _blurService?.LastError ?? string.Empty;

        public int Downsample
        {
            get => downsample;
            set => downsample = Mathf.Clamp(value, 1, 4);
        }

        public float BlurSigma
        {
            get => blurSigma;
            set => blurSigma = Mathf.Clamp(value, 0.1f, 10f);
        }

        public Vector2Int TargetResolution
        {
            get => targetResolution;
            set
            {
                if (targetResolution != value)
                {
                    targetResolution = value;
                    if (isActiveAndEnabled)
                    {
                        EnsureSourceTexture();
                    }
                }
            }
        }

        private void Awake()
        {
            _overlayDocument = GetComponent<UIDocument>();
            _blurService = new TabletGaussianBlur();
        }

        private void OnEnable()
        {
            SetupOffscreenBackgroundUI();
            BuildOverlayUI();
            _renderLoopCoroutine = StartCoroutine(RenderBlurLoop());
        }

        private void OnDisable()
        {
            if (_renderLoopCoroutine != null)
            {
                StopCoroutine(_renderLoopCoroutine);
                _renderLoopCoroutine = null;
            }
            ClearOverlayUI();
            TearDownOffscreenBackgroundUI();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void EnsureSourceTexture()
        {
            if (targetResolution.x <= 0 || targetResolution.y <= 0) return;

            if (_sourceRT != null && _sourceRT.width == targetResolution.x && _sourceRT.height == targetResolution.y)
            {
                return;
            }

            if (_sourceRT != null)
            {
                _sourceRT.Release();
                DestroyImmediate(_sourceRT);
                _sourceRT = null;
            }

            _sourceRT = new RenderTexture(targetResolution.x, targetResolution.y, 0, RenderTextureFormat.ARGB32)
            {
                name = "TabletBlur_SourceRT",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            _sourceRT.Create();

            if (_clonedBackgroundPanelSettings != null)
            {
                _clonedBackgroundPanelSettings.targetTexture = _sourceRT;
                _clonedBackgroundPanelSettings.referenceResolution = targetResolution;
            }
        }

        private void SetupOffscreenBackgroundUI()
        {
            EnsureSourceTexture();

            // Clone PanelSettings to avoid modifying shared project assets
            if (basePanelSettingsAsset != null)
            {
                _clonedBackgroundPanelSettings = Instantiate(basePanelSettingsAsset);
            }
            else
            {
                _clonedBackgroundPanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                _clonedBackgroundPanelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                _clonedBackgroundPanelSettings.referenceResolution = targetResolution;
            }
            _clonedBackgroundPanelSettings.targetTexture = _sourceRT;
            _clonedBackgroundPanelSettings.clearColor = true;
            _clonedBackgroundPanelSettings.colorClearValue = new Color(0.08f, 0.09f, 0.11f, 1f);

            // Offscreen coordinate mapping for UI Toolkit picking
            _clonedBackgroundPanelSettings.SetScreenToPanelSpaceFunction((Vector2 screenPos) =>
            {
                float normX = Mathf.Clamp01(screenPos.x / Mathf.Max(1f, Screen.width));
                float normY = Mathf.Clamp01(screenPos.y / Mathf.Max(1f, Screen.height));
                return new Vector2(normX * targetResolution.x, (1.0f - normY) * targetResolution.y);
            });

            // Create background UI Document host
            _backgroundGo = new GameObject("[TabletBlur_BackgroundHost]");
            _backgroundGo.hideFlags = HideFlags.DontSave;
            _backgroundGo.transform.SetParent(transform, false);
            _backgroundDocument = _backgroundGo.AddComponent<UIDocument>();
            _backgroundDocument.panelSettings = _clonedBackgroundPanelSettings;

            BuildBackgroundContent(_backgroundDocument.rootVisualElement);
        }

        private void BuildBackgroundContent(VisualElement root)
        {
            root.Clear();
            root.style.width = new Length(100, LengthUnit.Percent);
            root.style.height = new Length(100, LengthUnit.Percent);
            root.style.flexDirection = FlexDirection.Column;
            root.style.justifyContent = Justify.Center;
            root.style.alignItems = Align.Center;

            // Checkerboard grid simulator container
            var checker = new VisualElement
            {
                name = "MockApp_Checkerboard"
            };
            checker.style.width = 600;
            checker.style.height = 400;
            checker.style.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
            checker.style.borderTopColor = checker.style.borderBottomColor =
                checker.style.borderLeftColor = checker.style.borderRightColor = new Color(0.2f, 0.4f, 0.8f, 1f);
            checker.style.borderTopWidth = checker.style.borderBottomWidth =
                checker.style.borderLeftWidth = checker.style.borderRightWidth = 2f;
            checker.style.justifyContent = Justify.SpaceAround;
            checker.style.alignItems = Align.Center;

            var title = new Label("Underlying Application UI (Offscreen RT)")
            {
                style = { color = Color.white, fontSize = 22, unityFontStyleAndWeight = FontStyle.Bold }
            };
            checker.Add(title);

            _bgTestButton = new Button(() =>
            {
                BackgroundClickCount++;
                UpdateStatus();
            })
            {
                text = "Background App Action Button"
            };
            _bgTestButton.style.width = 280;
            _bgTestButton.style.height = 48;
            _bgTestButton.style.backgroundColor = new Color(0.18f, 0.55f, 0.34f, 1f);
            checker.Add(_bgTestButton);

            root.Add(checker);
        }

        private void BuildOverlayUI()
        {
            if (_overlayDocument == null) return;
            var root = _overlayDocument.rootVisualElement;
            root.Clear();
            root.style.width = new Length(100, LengthUnit.Percent);
            root.style.height = new Length(100, LengthUnit.Percent);

            // Container for displaying the captured/blurred texture
            _blurredDisplayElement = new VisualElement
            {
                name = "Tablet_BlurredDisplay"
            };
            _blurredDisplayElement.style.width = new Length(100, LengthUnit.Percent);
            _blurredDisplayElement.style.height = new Length(100, LengthUnit.Percent);
            _blurredDisplayElement.style.position = Position.Absolute;
            root.Add(_blurredDisplayElement);

            // Scrim Overlay Panel (Must be excluded from blur source)
            _shellOverlay = new VisualElement
            {
                name = "Tablet_ShellOverlayControls"
            };
            _shellOverlay.style.position = Position.Absolute;
            _shellOverlay.style.top = 20;
            _shellOverlay.style.right = 20;
            _shellOverlay.style.width = 360;
            _shellOverlay.style.paddingTop = _shellOverlay.style.paddingBottom =
                _shellOverlay.style.paddingLeft = _shellOverlay.style.paddingRight = 16;
            _shellOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.65f);
            _shellOverlay.style.borderTopLeftRadius = _shellOverlay.style.borderTopRightRadius =
                _shellOverlay.style.borderBottomLeftRadius = _shellOverlay.style.borderBottomRightRadius = 8f;

            _statusLabel = new Label();
            _statusLabel.style.color = Color.white;
            _statusLabel.style.marginBottom = 10;
            _shellOverlay.Add(_statusLabel);

            _toggleBlurButton = new Button(() =>
            {
                IsBlurActive = !IsBlurActive;
                UpdateStatus();
            })
            {
                text = "Toggle Blur"
            };
            _toggleBlurButton.style.height = 36;
            _toggleBlurButton.style.marginBottom = 8;
            _shellOverlay.Add(_toggleBlurButton);

            _overlayTestButton = new Button(() =>
            {
                OverlayClickCount++;
                UpdateStatus();
            })
            {
                text = "Overlay Shell Button"
            };
            _overlayTestButton.style.height = 36;
            _overlayTestButton.style.backgroundColor = new Color(0.7f, 0.2f, 0.2f, 1f);
            _shellOverlay.Add(_overlayTestButton);

            root.Add(_shellOverlay);
            UpdateStatus();
        }

        private void ClearOverlayUI()
        {
            if (_blurredDisplayElement != null)
            {
                _blurredDisplayElement.style.backgroundImage = StyleKeyword.Null;
                _blurredDisplayElement = null;
            }

            if (_overlayDocument != null && _overlayDocument.rootVisualElement != null)
            {
                _overlayDocument.rootVisualElement.Clear();
            }

            _shellOverlay = null;
            _statusLabel = null;
            _toggleBlurButton = null;
            _overlayTestButton = null;
        }

        private void UpdateStatus()
        {
            if (_statusLabel == null) return;
            string supportStr = IsServiceSupported ? "Supported" : $"Fallback ({ServiceLastError})";
            _statusLabel.text = $"[Tablet Blur Module]\n" +
                                $"Status: {supportStr}\n" +
                                $"Blur Active: {IsBlurActive}\n" +
                                $"Downsample: {downsample}x | Sigma: {blurSigma:F1}\n" +
                                $"BG Clicks: {BackgroundClickCount} | Overlay Clicks: {OverlayClickCount}";
        }

        private IEnumerator RenderBlurLoop()
        {
            // Frame scheduling: WaitForEndOfFrame guarantees UI Toolkit offscreen panel rendered into _sourceRT
            var waitFrame = new WaitForEndOfFrame();
            while (true)
            {
                yield return waitFrame;

                if (!isActiveAndEnabled || _blurService == null)
                {
                    continue;
                }

                EnsureSourceTexture();
                if (_sourceRT == null)
                {
                    continue;
                }

                _blurService.Downsample = downsample;
                _blurService.Sigma = blurSigma;

                RenderTexture displayTex = _sourceRT;
                if (IsBlurActive && _blurService.IsSupported)
                {
                    displayTex = _blurService.Process(_sourceRT);
                }

                if (_blurredDisplayElement != null && displayTex != null)
                {
                    _blurredDisplayElement.style.backgroundImage = Background.FromRenderTexture(displayTex);
                }
            }
        }

        private void TearDownOffscreenBackgroundUI()
        {
            if (_backgroundGo != null)
            {
                DestroyImmediate(_backgroundGo);
                _backgroundGo = null;
                _backgroundDocument = null;
            }

            if (_clonedBackgroundPanelSettings != null)
            {
                _clonedBackgroundPanelSettings.targetTexture = null;
                DestroyImmediate(_clonedBackgroundPanelSettings);
                _clonedBackgroundPanelSettings = null;
            }

            if (_sourceRT != null)
            {
                _sourceRT.Release();
                DestroyImmediate(_sourceRT);
                _sourceRT = null;
            }
        }

        public void SetOverlayBackgroundColor(Color color)
        {
            if (_shellOverlay != null)
            {
                _shellOverlay.style.backgroundColor = color;
            }
        }

        public void SimulateBackgroundClick()
        {
            SubmitReviewButton(_bgTestButton);
        }

        public void SimulateOverlayClick()
        {
            SubmitReviewButton(_overlayTestButton);
        }

        static void SubmitReviewButton(Button button)
        {
            if (button == null) return;
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
        }

        public void Dispose()
        {
            if (_renderLoopCoroutine != null)
            {
                StopCoroutine(_renderLoopCoroutine);
                _renderLoopCoroutine = null;
            }
            ClearOverlayUI();
            TearDownOffscreenBackgroundUI();
            if (_blurService != null)
            {
                _blurService.Dispose();
                _blurService = null;
            }
        }
    }
}
