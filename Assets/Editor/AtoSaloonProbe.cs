using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AtoSaloonProbe
{
    public static void Batch()
    {
        EditorSceneManager.OpenScene(AtoLineClosedLoop.GradedScenePath);
        var stats = new Dictionary<string, int>();
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
        {
            var c = r.bounds.center;
            if (Mathf.Abs(c.x) > 2f || c.z < 3f || c.z > 22f || c.y < -0.5f || c.y > 3f) continue;
            string key = "layer=" + r.gameObject.layer + " shadow=" + r.shadowCastingMode + " recv=" + r.receiveShadows + " mat=" + (r.sharedMaterial != null ? r.sharedMaterial.shader.name : "null");
            stats[key] = stats.TryGetValue(key, out var n) ? n + 1 : 1;
            string nm = r.name.ToLowerInvariant();
            if (nm.Contains("window") || nm.Contains("glass") || nm.Contains("roof") || nm.Contains("ceiling") || nm.Contains("wall"))
                Debug.Log("SALOON " + r.name + " parent=" + (r.transform.parent != null ? r.transform.parent.name : "-") + " " + key + " size=" + r.bounds.size.ToString("F2") + " mat=" + r.sharedMaterial.name);
        }
        foreach (var kv in stats) Debug.Log("SALOON_STAT " + kv.Value + " x " + kv.Key);
        EditorApplication.Exit(0);
    }
}
