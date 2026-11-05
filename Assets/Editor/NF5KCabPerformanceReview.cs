using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using RailwayRenaissance.Cab;

[InitializeOnLoad]
public static class NF5KCabPerformanceReview
{
    const string Pending="NF5KCabPerformanceReview.Pending";
    static NF5KCabPerformanceReview(){EditorApplication.playModeStateChanged+=Changed;}
    public static void Batch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/CabRailMotionReview.unity");
        SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void Assert(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    static void Changed(PlayModeStateChange change)
    {
        if(change!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Pending,false))return;
        SessionState.SetBool(Pending,false);
        try
        {
            var rail=UnityEngine.Object.FindAnyObjectByType<CabRailMotion>();
            rail.ApplyBackend(true);
            int hoses=0;
            foreach(var hose in rail.Vehicle.GetComponentsInChildren<CabFlexibleHose>())
            {
                var mesh=hose.GetComponent<MeshFilter>().sharedMesh;
                hose.UpdateGeometry(mesh);int before=hose.GeometryUpdateCount;
                for(int i=0;i<120;i++)hose.UpdateGeometry(mesh);
                Assert(hose.GeometryUpdateCount==before,"Stationary hose rebuilt");
                var field=typeof(CabFlexibleHose).GetField("to",BindingFlags.NonPublic|BindingFlags.Instance);
                var endpoint=(Transform)field.GetValue(hose);var position=endpoint.localPosition;
                endpoint.localPosition+=new Vector3(.012f,.003f,0);hose.UpdateGeometry(mesh);
                Assert(hose.GeometryUpdateCount==before+1&&hose.EndpointError()<.001f,"Deformed hose not updated or detached");
                endpoint.localPosition=position;hose.UpdateGeometry(mesh);hoses++;
            }
            Assert(hoses>0,"No actual hoses tested");
            Debug.Log("CAB_PERF_HOSE_OK: actual hoses="+hoses+", 120 unchanged calls each produce zero rebuilds; endpoint movement updates within 1mm");
            CabPhysicsQueries.Prepare();int syncs=CabPhysicsQueries.SynchronizationCount;
            for(int i=0;i<120;i++)CabPhysicsQueries.Prepare();
            Assert(CabPhysicsQueries.SynchronizationCount==syncs,"Clean query triggered synchronization");
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.transform.position=new Vector3(0,100,5);
            CabPhysicsQueries.MarkDirty();CabPhysicsQueries.Prepare();
            cube.transform.position=new Vector3(0,100,2);CabPhysicsQueries.MarkDirty();
            int dirtyCount=CabPhysicsQueries.SynchronizationCount;CabPhysicsQueries.Prepare();CabPhysicsQueries.Prepare();
            Assert(CabPhysicsQueries.SynchronizationCount==dirtyCount+1,"Dirty query did not coalesce synchronization");
            Assert(Physics.Raycast(new Vector3(0,100,0),Vector3.forward,out var hit,3)&&hit.collider.gameObject==cube,"Moved collider raycast stale");
            UnityEngine.Object.DestroyImmediate(cube);
            Debug.Log("CAB_PERF_SYNC_OK: clean queries skip sync, dirty batch syncs once; moved collider hit correctly");
            var ui=UnityEngine.Object.FindAnyObjectByType<CabContextInteraction>();
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            void Set(string name,object value)=>typeof(CabContextInteraction).GetField(name,flags).SetValue(ui,value);
            var camera=Camera.main;camera.transform.SetPositionAndRotation(new Vector3(0,100,0),Quaternion.identity);
            Set("settings",false);Set("cursorAim",false);Set("focused",null);Set("actionRect",new Rect());Set("inputAfter",float.PositiveInfinity);
            var mouse=Mouse.current??InputSystem.AddDevice<Mouse>();
            InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(Screen.width*.5f,Screen.height*.5f)});InputSystem.Update();
            var candidates=(List<Collider>)typeof(CabContextInteraction).GetField("candidates",flags).GetValue(ui);candidates.Clear();
            for(int i=0;i<40;i++)
            {
                var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.transform.position=new Vector3((i%5)*.004f,100,.8f+(i/5)*.004f);obj.transform.localScale=Vector3.one*.03f;
                obj.AddComponent<CabPassageDoor>();
                candidates.Add(obj.GetComponent<Collider>());
            }
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=new Vector3(0,100,.5f);blocker.transform.localScale=Vector3.one*.3f;
            CabPhysicsQueries.MarkDirty();
            typeof(CabContextInteraction).GetMethod("Update",flags).Invoke(ui,null);
            Assert(ui.LastQueryCount==5,"Expected one aim + four nearby rays, got "+ui.LastQueryCount);
            Debug.Log("CAB_PERF_RAY_BUDGET_OK: 40 visible nearby colliders, exactly 5 queries");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
