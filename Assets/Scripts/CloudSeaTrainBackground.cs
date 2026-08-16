using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(Renderer))]
public class CloudSeaTrainBackground : MonoBehaviour
{
    [Header("Video")]
    public VideoClip videoClip;
    public bool loop = true;

    private VideoPlayer _videoPlayer;
    private Renderer _renderer;
    private RenderTexture _rt;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();

        _rt = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        _rt.filterMode = FilterMode.Bilinear;
        _rt.Create();

        if (_renderer.sharedMaterial != null)
        {
            _renderer.material = _renderer.sharedMaterial;
        }
        else
        {
            var mat = Resources.Load<Material>("Materials/VideoBackground");
            if (mat != null) _renderer.material = mat;
        }
        _renderer.material.mainTexture = _rt;

        _videoPlayer = gameObject.AddComponent<VideoPlayer>();
        _videoPlayer.playOnAwake = true;
        _videoPlayer.isLooping = loop;
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _videoPlayer.targetTexture = _rt;
        _videoPlayer.aspectRatio = VideoAspectRatio.Stretch;
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        _videoPlayer.skipOnDrop = false;

        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, "cloud_sea_bg.mp4");
        if (System.IO.File.Exists(videoPath))
        {
            _videoPlayer.url = videoPath;
            _videoPlayer.source = VideoSource.Url;
        }
        else
        {
            if (videoClip == null)
                videoClip = Resources.Load<VideoClip>("Videos/cloud_sea_bg");
            if (videoClip != null)
            {
                _videoPlayer.clip = videoClip;
                _videoPlayer.source = VideoSource.VideoClip;
            }
        }
    }

    void OnDestroy()
    {
        if (_rt != null) _rt.Release();
    }
}
