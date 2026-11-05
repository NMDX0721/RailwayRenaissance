using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

// Reads the saved candidate without rebuilding or saving scene assets.
public static class NF5KStructureReview
{
    const string Output = "tools/cab_reference/structure_review/";
    [Serializable] sealed class Part
    {
        public string path;
        public Vector3 localMin, localMax;
        public bool hasDoorController;
    }
    [Serializable] sealed class Report
    {
        public string scene, scope = "Saved geometry in body-local metres; no engineering certification or dynamic clearance claim";
        public int seats, sideDoorControllers;
        public float armrestAisleM, partitionOpeningM;
        public Part[] parts;
    }
    public static void Batch()
    {
        try
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene("Assets/Scenes/CabInteriorBlockout.unity");
            EditorApplication.delayCall += Review;
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void Review()
    {
        try
        {
            var body = GameObject.Find("CabInterior_Blockout");
            if (body == null) throw new InvalidOperationException("Saved body missing");
            var renderers = body.GetComponentsInChildren<Renderer>(true);
            Bounds LocalBounds(Renderer r)
            {
                var b = r.localBounds;
                var result = new Bounds(body.transform.InverseTransformPoint(r.transform.TransformPoint(b.min)), Vector3.zero);
                for (int i = 0; i < 8; i++)
                    result.Encapsulate(body.transform.InverseTransformPoint(r.transform.TransformPoint(new Vector3(
                        (i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y,
                        (i & 4) == 0 ? b.min.z : b.max.z))));
                return result;
            }
            var names = new[] { "Passenger_Armrest", "Rear_Passenger_Partition", "Passenger_Passage_Door_Lower",
                "Rear_Gangway_Door", "Underfloor_Equipment_Cabinet", "Continuous_Underframe",
                "Passenger_Seat_Cushion", "Rear_Entry_Threshold", "Floor" };
            var selected = renderers.Where(r => names.Any(n => r.name == n || r.name.EndsWith(n))).ToArray();
            string PathOf(Transform t) => t == body.transform ? t.name : PathOf(t.parent) + "/" + t.name;
            var arms = renderers.Where(r => r.name == "Passenger_Armrest").Select(LocalBounds).ToArray();
            var partitions = renderers.Where(r => r.name == "Rear_Passenger_Partition").Select(LocalBounds).ToArray();
            if (arms.Length == 0 || partitions.Length != 2) throw new InvalidOperationException("Layout measurement inputs missing");
            var report = new Report {
                scene = EditorSceneManager.GetActiveScene().path,
                seats = renderers.Count(r => r.name == "Passenger_Seat_Cushion"),
                sideDoorControllers = body.GetComponentsInChildren<CabSideDoor>(true).Length,
                armrestAisleM = arms.Where(b => b.center.x > 0).Min(b => b.min.x) - arms.Where(b => b.center.x < 0).Max(b => b.max.x),
                partitionOpeningM = partitions.Single(b => b.center.x > 0).min.x - partitions.Single(b => b.center.x < 0).max.x,
                parts = selected.Select(r => { var b = LocalBounds(r); return new Part { path = PathOf(r.transform),
                    localMin = b.min, localMax = b.max, hasDoorController = r.GetComponentInParent<CabSideDoor>() != null }; }).ToArray()
            };
            File.WriteAllText(Output + "dimensions.json", JsonUtility.ToJson(report, true));
            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("Review camera missing");
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>())
                if (!r.transform.IsChildOf(body.transform)) r.enabled = false;
            camera.orthographic = true; camera.nearClipPlane = .02f; camera.farClipPlane = 100;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.35f,.38f,.40f);
            var center = body.transform.TransformPoint(new Vector3(0,.55f,8.425f));
            var directions = new[] { Vector3.back, Vector3.forward, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
            var labels = new[] { "front", "rear", "side_negative_x", "side_positive_x", "top", "bottom" };
            for (int i = 0; i < directions.Length; i++)
            {
                var direction = body.transform.TransformDirection(directions[i]);
                camera.transform.position = center + direction * 30;
                camera.transform.rotation = Quaternion.LookRotation(-direction, i >= 4 ? body.transform.forward : body.transform.up);
                camera.orthographicSize = i < 2 ? 2.5f : i >= 4 ? 10.5f : 6.5f;
                Capture(camera, Output + labels[i] + ".png");
            }
            Debug.Log("NF5K_STRUCTURE_REVIEW_OK: saved-scene bounds and six orthographic views; no scene saved; seats=" + report.seats + "; aisle=" + report.armrestAisleM + "; partition=" + report.partitionOpeningM);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void Capture(Camera camera, string path)
    {
        var target = RenderTexture.GetTemporary(1600,1000,24);
        var previous = RenderTexture.active; var previousTarget = camera.targetTexture;
        var pixels = new Texture2D(1600,1000,TextureFormat.RGB24,false);
        try {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0,0,1600,1000),0,0); pixels.Apply();
            File.WriteAllBytes(path,pixels.EncodeToPNG());
        }
        finally { camera.targetTexture = previousTarget; RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(pixels); }
    }
}
