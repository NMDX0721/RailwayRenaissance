using UnityEngine;
using UnityEngine.Video;
using System.IO;

[RequireComponent(typeof(VideoPlayer))]
public class TitleScreenVideoBg : MonoBehaviour
{
    [Header("视频设置")]
    public VideoClip videoClip;
    public string videoPath = "";

    [Header("渲染设置")]
    public RenderTexture renderTexture;
    public int renderWidth = 1920;
    public int renderHeight = 1080;

    private VideoPlayer _player;
    private Renderer _renderer;

    void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;

        _player = GetComponent<VideoPlayer>();
        _renderer = GetComponent<Renderer>();

        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(renderWidth, renderHeight, 0);
            renderTexture.Create();
        }

        _player.playOnAwake = false;
        _player.isLooping = true;
        _player.renderMode = VideoRenderMode.RenderTexture;
        _player.targetTexture = renderTexture;
        _player.audioOutputMode = VideoAudioOutputMode.None;

        if (videoClip != null)
        {
            _player.clip = videoClip;
        }
        else if (!string.IsNullOrEmpty(videoPath))
        {
            _player.url = videoPath;
        }

        if (_renderer != null)
        {
            Shader shader = Shader.Find("Unlit/Texture");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit");
            var mat = new Material(shader);
            mat.mainTexture = renderTexture;
            _renderer.material = mat;
        }

        if (string.IsNullOrEmpty(_player.url) && _player.clip == null)
        {
            string streamingPath = Application.streamingAssetsPath + "/cloud_sea_bg.mp4";
            if (File.Exists(streamingPath))
                _player.url = streamingPath;
        }

        _player.Play();
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}
