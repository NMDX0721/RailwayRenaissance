using UnityEngine;
using UnityEngine.Video;
using UnityEditor;
using UnityEditor.SceneManagement;

public class CloudSeaSetup : Editor
{
    [MenuItem("RailRevival/设置云海列车背景")]
    static void SetupBackground()
    {
        // 创建全屏Quad
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "CloudSeaBackground";
        quad.transform.position = new Vector3(0, 0, 5);
        quad.transform.localScale = new Vector3(20, 11.25f, 1);

        // 创建着色器材质
        var shader = Shader.Find("RailRevival/CloudSeaTrain");
        if (shader == null)
        {
            Debug.LogError("找不到着色器 RailRevival/CloudSeaTrain");
            return;
        }

        var mat = new Material(shader);

        // 加载噪声纹理
        Texture2D noiseTex = null;
        
        // 方法1: 从Resources加载
        noiseTex = Resources.Load<Texture2D>("Textures/NoiseTexture");
        
        // 方法2: 从Assets直接加载
        if (noiseTex == null)
        {
            var guids = AssetDatabase.FindAssets("NoiseTexture t:Texture2D");
            foreach (var guid in guids)
            {
                noiseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
                if (noiseTex != null) break;
            }
        }
        
        // 方法3: 使用默认纹理
        if (noiseTex == null)
        {
            noiseTex = Texture2D.whiteTexture;
            Debug.LogWarning("噪声纹理未找到！使用白色纹理替代。");
        }
        else
        {
            Debug.Log($"噪声纹理加载结果: {(noiseTex != null ? noiseTex.name + " " + noiseTex.width + "x" + noiseTex.height : "NULL")}");
        Debug.Log($"材质噪声强度: {mat.GetFloat("_NoiseStrength")}");
        }

        mat.SetTexture("_NoiseTex", noiseTex);
        mat.SetVector("_NoiseTexSize", new Vector4(noiseTex.width, noiseTex.height, 0, 0));
        mat.SetFloat("_TimeScale", 4f);
        mat.SetFloat("_NoiseStrength", 1.9f);

        quad.GetComponent<Renderer>().material = mat;

        quad.AddComponent<CloudSeaTrainBackground>();

        quad.GetComponent<Renderer>().sortingOrder = -100;

        // 相机
        if (Camera.main == null)
        {
            var camObj = new GameObject("MainCamera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.35f, 0.43f, 1f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5.625f;
            cam.transform.position = new Vector3(0, 0, -10);
        }

        Selection.activeGameObject = quad;
        Debug.Log("云海列车背景已设置！");
    }

    [MenuItem("RailRevival/创建标题画面场景")]
    static void CreateTitleScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "TitleScreen";

        // 相机
        var camObj = new GameObject("MainCamera");
        camObj.tag = "MainCamera";
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0, 0, -10);

        // 全屏Quad
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "TitleBackground";
        quad.transform.position = Vector3.zero;
        quad.transform.localScale = new Vector3(22, 12.5f, 1);

        // 添加VideoPlayer
        var vp = quad.AddComponent<VideoPlayer>();
        var videoClip = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Videos/cloud_sea_bg.mp4");
        if (videoClip != null)
        {
            var renderer = quad.GetComponent<Renderer>();
            var rt = new RenderTexture(1920, 1080, 0);
            rt.Create();

            vp.playOnAwake = false;
            vp.isLooping = true;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = rt;
            vp.clip = videoClip;
            vp.audioOutputMode = VideoAudioOutputMode.None;

            var mat = new Material(Shader.Find("Unlit/Texture"));
            mat.mainTexture = rt;
            renderer.material = mat;

            vp.Play();
            Debug.Log($"视频背景已设置: {videoClip.name} ({videoClip.width}x{videoClip.height})");
        }
        else
        {
            Debug.LogWarning("未找到视频文件 Assets/Videos/cloud_sea_bg.mp4");
        }

        // 标题文字Canvas
        var canvasObj = new GameObject("TitleCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var titleObj = new GameObject("GameTitle");
        titleObj.transform.SetParent(canvasObj.transform, false);
        var titleText = titleObj.AddComponent<UnityEngine.UI.Text>();
        titleText.text = "🚂 铁路复兴：沙能冲击";
        titleText.fontSize = 48;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.94f, 0.82f, 0.38f, 1f);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.7f);
        titleRect.anchorMax = new Vector2(0.5f, 0.7f);
        titleRect.sizeDelta = new Vector2(800, 80);
        titleRect.anchoredPosition = Vector2.zero;

        var subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(canvasObj.transform, false);
        var subText = subObj.AddComponent<UnityEngine.UI.Text>();
        subText.text = "Railway Renaissance: Sand Energy Impact";
        subText.fontSize = 18;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = new Color(1f, 1f, 1f, 0.6f);
        var subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 0.6f);
        subRect.anchorMax = new Vector2(0.5f, 0.6f);
        subRect.sizeDelta = new Vector2(600, 40);
        subRect.anchoredPosition = Vector2.zero;

        // BGM按钮
        var bgmBtnObj = new GameObject("BGMButton");
        bgmBtnObj.transform.SetParent(canvasObj.transform, false);
        var bgmBtn = bgmBtnObj.AddComponent<UnityEngine.UI.Button>();
        var bgmText = bgmBtnObj.AddComponent<UnityEngine.UI.Text>();
        bgmText.text = "🔇";
        bgmText.fontSize = 24;
        bgmText.alignment = TextAnchor.MiddleCenter;
        var bgmRect = bgmBtnObj.GetComponent<RectTransform>();
        bgmRect.anchorMin = new Vector2(1, 1);
        bgmRect.anchorMax = new Vector2(1, 1);
        bgmRect.sizeDelta = new Vector2(50, 50);
        bgmRect.anchoredPosition = new Vector2(-40, -40);

        // 移除Quad上的Collider（防止阻挡鼠标事件）
        var collider = quad.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);

        // EventSystem（先创建）
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/TitleScreen.unity");

        var scenes = new EditorBuildSettingsScene[2];
        scenes[0] = new EditorBuildSettingsScene("Assets/Scenes/TitleScreen.unity", true);
        scenes[1] = new EditorBuildSettingsScene("Assets/Scenes/StationSlice_V1.unity", true);
        EditorBuildSettings.scenes = scenes;

        Debug.Log("标题画面场景已创建：Assets/Scenes/TitleScreen.unity（视频背景）");
    }
}
