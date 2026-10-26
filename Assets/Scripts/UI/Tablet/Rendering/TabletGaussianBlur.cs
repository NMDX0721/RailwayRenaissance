using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UI.Tablet.Rendering
{
    /// <summary>
    /// Self-contained two-pass separable 9-tap Gaussian blur service for UI render textures.
    /// Operates without compute shaders or camera grabs; compatible with URP Windows/Android.
    /// Allocates and manages its own ping-pong render targets; never overwrites or releases caller source.
    /// </summary>
    public sealed class TabletGaussianBlur : IDisposable
    {
        private const string ShaderPath = "UI/TabletGaussianBlur";
        private const int MaxRadius = 4;
        private const int TapCount = 9;

        private Material _material;
        private Shader _shader;
        private RenderTexture _pingRT;
        private RenderTexture _pongRT;
        private RenderTextureDescriptor _currentDescriptor;
        private bool _isDisposed;

        // Cached shader property IDs
        private static readonly int BlurDirId = Shader.PropertyToID("_BlurDir");
        private static readonly int Weights0Id = Shader.PropertyToID("_Weights0");
        private static readonly int Weight4Id = Shader.PropertyToID("_Weight4");

        // Runtime normalized weights: tap 0 (center), taps 1..4 (symmetric pairs)
        private readonly float[] _weights = new float[5];
        private float _cachedSigma = -1f;

        public bool IsSupported { get; private set; }
        public string LastError { get; private set; } = string.Empty;

        public int Downsample { get; set; } = 2; // 1 = full, 2 = half, 4 = quarter
        public float Sigma { get; set; } = 2.5f;

        /// <summary>
        /// Returns the most recently computed blur result. Null if not executed or disposed.
        /// </summary>
        public RenderTexture CurrentResult => _pingRT;

        public TabletGaussianBlur()
        {
            InitializeShader();
        }

        private void InitializeShader()
        {
            _shader = Resources.Load<Shader>(ShaderPath);
            if (_shader == null)
            {
                IsSupported = false;
                LastError = $"Shader not found at Resources path: {ShaderPath}. Ensure shader asset is in Resources/UI/.";
                return;
            }

            if (!_shader.isSupported)
            {
                IsSupported = false;
                LastError = $"Shader '{ShaderPath}' is not supported on this graphics device.";
                return;
            }

            _material = new Material(_shader) { hideFlags = HideFlags.DontSave };
            IsSupported = true;
            LastError = string.Empty;
        }

        /// <summary>
        /// Evaluates a 1D discrete Gaussian distribution and normalizes weights across the 9 taps:
        /// W[0] + 2 * (W[1] + W[2] + W[3] + W[4]) == 1.0.
        /// </summary>
        private void UpdateWeights(float sigma)
        {
            if (Mathf.Approximately(sigma, _cachedSigma))
            {
                return;
            }

            float safeSigma = Mathf.Clamp(sigma, 0.05f, 10.0f);
            float twoSigmaSq = 2.0f * safeSigma * safeSigma;

            float sum = 0f;
            for (int i = 0; i <= MaxRadius; i++)
            {
                float w = Mathf.Exp(-(i * i) / twoSigmaSq);
                if (float.IsNaN(w) || float.IsInfinity(w))
                {
                    w = (i == 0) ? 1f : 0f;
                }
                _weights[i] = w;
                sum += (i == 0) ? w : 2.0f * w;
            }

            if (sum > 0.00001f)
            {
                for (int i = 0; i <= MaxRadius; i++)
                {
                    _weights[i] /= sum;
                }
            }
            else
            {
                _weights[0] = 1f;
                for (int i = 1; i <= MaxRadius; i++) _weights[i] = 0f;
            }

            _cachedSigma = safeSigma;
        }

        private void EnsureResources(RenderTexture source)
        {
            int down = Mathf.Clamp(Downsample, 1, 4);
            int targetWidth = Mathf.Max(16, source.width / down);
            int targetHeight = Mathf.Max(16, source.height / down);

            bool needsRealloc = _pingRT == null || _pongRT == null ||
                                _currentDescriptor.width != targetWidth ||
                                _currentDescriptor.height != targetHeight ||
                                _currentDescriptor.graphicsFormat != source.graphicsFormat;

            if (needsRealloc)
            {
                ReleaseRenderTextures();

                _currentDescriptor = new RenderTextureDescriptor(targetWidth, targetHeight, source.graphicsFormat, 0)
                {
                    msaaSamples = 1,
                    useMipMap = false,
                    autoGenerateMips = false,
                    sRGB = source.sRGB
                };

                _pingRT = new RenderTexture(_currentDescriptor)
                {
                    name = "TabletBlur_PingRT",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave
                };
                _pingRT.Create();

                _pongRT = new RenderTexture(_currentDescriptor)
                {
                    name = "TabletBlur_PongRT",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave
                };
                _pongRT.Create();
            }
        }

        /// <summary>
        /// Executes separable Gaussian blur on source. Returns owned blurred RenderTexture.
        /// Caller source is never modified or released.
        /// </summary>
        public RenderTexture Process(RenderTexture source)
        {
            if (_isDisposed)
            {
                LastError = "Attempted to process blur on a disposed TabletGaussianBlur instance.";
                return source;
            }

            if (source == null)
            {
                LastError = "Source RenderTexture is null.";
                return null;
            }

            if (!source.IsCreated())
            {
                source.Create();
            }

            if (!IsSupported || _material == null)
            {
                if (_material == null) InitializeShader();
                if (!IsSupported) return source;
            }

            EnsureResources(source);
            UpdateWeights(Sigma);

            _material.SetVector(Weights0Id, new Vector4(_weights[0], _weights[1], _weights[2], _weights[3]));
            _material.SetFloat(Weight4Id, _weights[4]);

            // Unity Issue Tracker workaround: Graphics.Blit can alter RenderTexture.active unexpectedly
            RenderTexture prevActive = RenderTexture.active;
            try
            {
                // Pass 0: Horizontal Blur (Source -> Pong)
                Vector2 texelSize = new Vector2(1.0f / _currentDescriptor.width, 1.0f / _currentDescriptor.height);
                _material.SetVector(BlurDirId, new Vector4(texelSize.x, 0.0f, 0.0f, 0.0f));
                Graphics.Blit(source, _pongRT, _material, 0);

                // Pass 0: Vertical Blur (Pong -> Ping)
                _material.SetVector(BlurDirId, new Vector4(0.0f, texelSize.y, 0.0f, 0.0f));
                Graphics.Blit(_pongRT, _pingRT, _material, 0);
            }
            catch (Exception ex)
            {
                LastError = $"Exception during blur blit execution: {ex.Message}";
                return source;
            }
            finally
            {
                RenderTexture.active = prevActive;
            }

            return _pingRT;
        }

        private void ReleaseRenderTextures()
        {
            if (_pingRT != null)
            {
                _pingRT.Release();
                UnityEngine.Object.DestroyImmediate(_pingRT);
                _pingRT = null;
            }

            if (_pongRT != null)
            {
                _pongRT.Release();
                UnityEngine.Object.DestroyImmediate(_pongRT);
                _pongRT = null;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            ReleaseRenderTextures();

            if (_material != null)
            {
                UnityEngine.Object.DestroyImmediate(_material);
                _material = null;
            }

            _shader = null;
            IsSupported = false;
        }
    }
}
