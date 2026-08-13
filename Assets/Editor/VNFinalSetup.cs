using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class VNFinalSetup
{
    [MenuItem("Tools/Finalize VN_Test Scene")]
    public static void FinalizeScene()
    {
        if (EditorApplication.isPlaying) return;

        var scene = EditorSceneManager.GetActiveScene();

        var existing = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        foreach (var go in existing)
        {
            if (go.name != "Main Camera" && go.name != "Directional Light" && go.name != "EventSystem")
                Object.DestroyImmediate(go);
        }

        var manager = new GameObject("VN_TestManager");
        manager.AddComponent<VNManager>();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("VN_Test scene finalized. Delete Assets/Editor/VNFinalSetup.cs now.");
    }
}