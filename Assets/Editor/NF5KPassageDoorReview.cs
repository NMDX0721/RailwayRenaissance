using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

[InitializeOnLoad]
public static class NF5KPassageDoorReview
{
    const string Pending = "NF5KPassageDoorReview.Pending";
    static NF5KPassageDoorReview() { EditorApplication.playModeStateChanged += PlayChanged; }
    const string Output = "tools/cab_reference/passage_door_review/";
    // The ATO research scene has a separately detailed partition assembly; do not migrate it as the baseline door.
    static readonly string[] Scenes = { "CabInteriorBlockout", "CabDaylightReview", "CabSimulationReview", "CabRailMotionReview" };
    public static void Batch()
    {
        try
        {
            Directory.CreateDirectory(Output);
            foreach (var name in Scenes)
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
                var pivot = GameObject.Find("Passenger_Passage_Door_Pivot").transform;
                var door = pivot.GetComponent<CabPassageDoor>();
                if (door == null) door = pivot.gameObject.AddComponent<CabPassageDoor>();
                door.SetOpen(0);
                var children = pivot.Cast<Transform>().ToArray();
                var positions = children.Select(t => t.position).ToArray();
                var body = GameObject.Find("CabInterior_Blockout").transform;
                pivot.position = body.TransformPoint(new Vector3(-.385f,0,1.876f));
                for (int i = 0; i < children.Length; i++) children[i].position = positions[i];
                var glass = pivot.Find("Passenger_Passage_Door_Glass");
                if (glass.GetComponent<BoxCollider>() == null) glass.gameObject.AddComponent<BoxCollider>();
                NF5KPassageDoorFinish.Apply(pivot);
                Validate(door);
                NF5KDoorReview.Validate();
                if (name == "CabSimulationReview") Capture(door);
                door.SetOpen(0);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                // Validate the serialized result, not just the in-memory patch.
                EditorSceneManager.OpenScene(scene.path);
                Validate(UnityEngine.Object.FindAnyObjectByType<CabPassageDoor>());
                Debug.Log("NF5K_PASSAGE_SCENE_OK: " + name);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/CabSimulationReview.unity");
            SessionState.SetBool(Pending,true);
            EditorApplication.isPlaying = true;
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void Validate(CabPassageDoor door)
    {
        if (door == null) throw new InvalidOperationException("Missing passage door");
        var body = GameObject.Find("CabInterior_Blockout");
        var fixedColliders = body.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger && !c.transform.IsChildOf(door.transform)).ToArray();
        try {
            for (int angle = 0; angle <= 90; angle++)
            {
                door.SetOpen(angle / 90f); Physics.SyncTransforms();
                foreach (var moving in door.GetComponentsInChildren<BoxCollider>())
                    foreach (var obstacle in fixedColliders)
                        if (Physics.ComputePenetration(moving,moving.transform.position,moving.transform.rotation,
                            obstacle,obstacle.transform.position,obstacle.transform.rotation,out _,out float depth) && depth > .002f)
                            throw new InvalidOperationException("Passage sweep at " + angle + " intersects " + obstacle.name + " depth=" + depth);
            }
            // Fixed project proxy, not an accessibility standard: 500mm diameter, 1.75m tall.
            var proxy = new GameObject("Passage_Clearance_Proxy");
            var capsule = proxy.AddComponent<CapsuleCollider>(); capsule.radius=.25f; capsule.height=1.75f;
            var root = body.transform;
            try {
                for (int i=0;i<=40;i++)
                {
                    proxy.transform.position=root.TransformPoint(new Vector3(.03f,.88f,1.30f+i*.03f));
                    proxy.transform.rotation=root.rotation;
                    foreach (var c in body.GetComponentsInChildren<Collider>())
                        if (!c.isTrigger && Physics.ComputePenetration(capsule,proxy.transform.position,proxy.transform.rotation,
                            c,c.transform.position,c.transform.rotation,out _,out float depth) && depth>.002f)
                            throw new InvalidOperationException("Open passage proxy blocked by "+c.name+" at sample "+i);
                }
            } finally { UnityEngine.Object.DestroyImmediate(proxy); }
        } finally { door.SetOpen(0); Physics.SyncTransforms(); }
        Debug.Log("NF5K_PASSAGE_SWEEP_OK: 91 angles, body colliders; open-door 500mm x 1750mm capsule, 41 samples; closed restored");
    }
    static void Capture(CabPassageDoor door)
    {
        var camera=Camera.main; var position=camera.transform.position;var rotation=camera.transform.rotation;
        bool asynchronousShaders=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
        bool orthographic=camera.orthographic;float fieldOfView=camera.fieldOfView;
        camera.orthographic=false; camera.fieldOfView=70;
        camera.transform.position=door.transform.root.TransformPoint(new Vector3(.10f,1.30f,.45f));
        camera.transform.LookAt(door.transform.position+Vector3.up*.95f);
        var light=new GameObject("Passage_Review_Fill").AddComponent<Light>(); light.type=LightType.Point;
        light.transform.position=camera.transform.position+Vector3.right*.7f; light.range=5; light.intensity=1.3f;
        foreach (var open in new[]{false,true}) {
            door.SetOpen(open?1:0); var rt=RenderTexture.GetTemporary(1200,1000,24);
            var previous=RenderTexture.active; var target=camera.targetTexture;
            var pixels=new Texture2D(1200,1000,TextureFormat.RGB24,false);
            try { camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1200,1000),0,0);pixels.Apply();File.WriteAllBytes(Output+(open?"open":"closed")+".png",pixels.EncodeToPNG()); }
            finally { camera.targetTexture=target;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(pixels); }
        }
        door.SetOpen(0);
        var body=door.transform.root;
        camera.transform.position=body.TransformPoint(new Vector3(.46f,1.10f,1.30f));
        camera.transform.LookAt(body.TransformPoint(new Vector3(.26f,1.0f,1.91f)));camera.fieldOfView=40;
        light.transform.position=body.TransformPoint(new Vector3(.0f,1.3f,1.3f));
        var detailTarget=RenderTexture.GetTemporary(1200,1000,24);var active=RenderTexture.active;var originalTarget=camera.targetTexture;
        var detailPixels=new Texture2D(1200,1000,TextureFormat.RGB24,false);
        try {camera.targetTexture=detailTarget;camera.Render();camera.Render();RenderTexture.active=detailTarget;detailPixels.ReadPixels(new Rect(0,0,1200,1000),0,0);detailPixels.Apply();File.WriteAllBytes(Output+"handle_detail.png",detailPixels.EncodeToPNG());}
        finally {camera.targetTexture=originalTarget;RenderTexture.active=active;RenderTexture.ReleaseTemporary(detailTarget);UnityEngine.Object.DestroyImmediate(detailPixels);}
        UnityEngine.Object.DestroyImmediate(light.gameObject);
        camera.transform.SetPositionAndRotation(position,rotation);camera.orthographic=orthographic;camera.fieldOfView=fieldOfView;
        ShaderUtil.allowAsyncCompilation=asynchronousShaders;
    }
    static void PlayChanged(PlayModeStateChange change)
    {
        if(change!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Pending,false))return;
        SessionState.SetBool(Pending,false);
        try {
            var door=UnityEngine.Object.FindAnyObjectByType<CabPassageDoor>();
            var ui=UnityEngine.Object.FindAnyObjectByType<CabContextInteraction>();
            if(ui==null)throw new InvalidOperationException("Context UI missing");
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            var glass=door.transform.Find("Passenger_Passage_Door_Glass").GetComponent<Collider>();
            var resolved=typeof(CabContextInteraction).GetMethod("Target",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{glass});
            if(!ReferenceEquals(resolved,door))throw new InvalidOperationException("Door collider does not resolve to interaction target");
            typeof(CabContextInteraction).GetField("focused",flags).SetValue(ui,door);
            typeof(CabContextInteraction).GetMethod("BuildChoices",flags).Invoke(ui,null);
            var choices=(System.Collections.IList)typeof(CabContextInteraction).GetField("choices",flags).GetValue(ui);
            if(choices.Count!=1)throw new InvalidOperationException("Door action not offered");
            typeof(CabContextInteraction).GetMethod("ExecuteSelected",flags).Invoke(ui,null);
            if(door.OpenFraction!=1)throw new InvalidOperationException("Context open failed");
            typeof(CabContextInteraction).GetMethod("BuildChoices",flags).Invoke(ui,null);
            typeof(CabContextInteraction).GetMethod("ExecuteSelected",flags).Invoke(ui,null);
            if(door.OpenFraction!=0)throw new InvalidOperationException("Context close failed");
            Debug.Log("NF5K_PASSAGE_PLAY_OK: context choice dispatch opens and closes serialized door");
            EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
    }
}
