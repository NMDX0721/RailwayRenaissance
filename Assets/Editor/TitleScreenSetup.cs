#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class TitleScreenSetup
{
    private const string ScenePath = "Assets/Scenes/TitleScreen.unity";

    [MenuItem("RailRevival/Setup TitleScreen")]
    public static void Setup()
    {
        if (Application.isPlaying) return;

        Scene scene;
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ScenePath) != null)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        else
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = GameObject.FindWithTag("MainCamera");
        if (camGo == null)
        {
            camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            SceneManager.MoveGameObjectToScene(camGo, scene);
        }
        camGo.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camGo.GetComponent<Camera>().backgroundColor = Color.black;
        camGo.GetComponent<Camera>().orthographic = true;
        camGo.GetComponent<Camera>().orthographicSize = 5f;
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            SceneManager.MoveGameObjectToScene(es, scene);
        }

        var titleManager = FindOrCreate(scene, "TitleManager");
        var uiDoc = titleManager.GetComponent<UIDocument>();
        if (uiDoc == null) uiDoc = titleManager.AddComponent<UIDocument>();

        var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Resources/UI/TitleScreenPanelSettings.asset");
        if (ps == null)
        {
            ps = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(ps, "Assets/Resources/UI/TitleScreenPanelSettings.asset");
        }
        uiDoc.panelSettings = ps;

        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Resources/UI/TitleScreen.uxml");
        if (uxml != null) uiDoc.visualTreeAsset = uxml;

        if (titleManager.GetComponent<TitleScreen>() == null)
            titleManager.AddComponent<TitleScreen>();

        var videoBg = FindOrCreate(scene, "VideoBackground");
        var mf = videoBg.GetComponent<MeshFilter>();
        if (mf == null) mf = videoBg.AddComponent<MeshFilter>();
        mf.sharedMesh = MakeQuad();
        var mr = videoBg.GetComponent<MeshRenderer>();
        if (mr == null) mr = videoBg.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        var existingMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/VideoBackground.mat");
        if (existingMat != null) mr.sharedMaterial = existingMat;
        videoBg.transform.localScale = new Vector3(22f, 12.5f, 1f);
        videoBg.transform.position = new Vector3(0f, 1f, 0f);

        if (videoBg.GetComponent<CloudSeaTrainBackground>() == null)
            videoBg.AddComponent<CloudSeaTrainBackground>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("TitleScreenSetup: 完成！");
    }

    [MenuItem("RailRevival/Fix Build Shader")]
    public static void FixBuildShader()
    {
        SettingsService.OpenProjectSettings("Project/Graphics");
        Debug.Log("请在 Always Included Shaders 列表中点击 + 搜索并添加 Unlit/Texture");
    }

    private static Mesh MakeQuad()
    {
        var m = new Mesh { name = "VideoQuad" };
        m.vertices = new[] { new Vector3(-0.5f,-0.5f,0), new Vector3(0.5f,-0.5f,0), new Vector3(0.5f,0.5f,0), new Vector3(-0.5f,0.5f,0) };
        m.uv = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1) };
        m.triangles = new[] { 0,2,1, 0,3,2 };
        m.RecalculateNormals();
        return m;
    }

    private static GameObject FindOrCreate(Scene scene, string name)
    {
        foreach (var r in scene.GetRootGameObjects())
            if (r.name == name) return r;
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go;
    }
}
#endif
