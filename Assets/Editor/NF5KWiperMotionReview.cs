using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.SceneManagement;
using RailwayRenaissance.Cab;

public static class NF5KWiperMotionReview
{
    public static void ValidateMotion()
    {
        var type=Type.GetType("RailwayRenaissance.Cab.CabWiperMotion, Assembly-CSharp");
        if(type==null)throw new InvalidOperationException("Wiper motion implementation missing (expected initial red)");
        var motion=Activator.CreateInstance(type);
        void Assert(bool value,string why){if(!value)throw new InvalidOperationException(why);}
        bool Select(int mode)=>(bool)type.GetMethod("SelectMode").Invoke(motion,new object[]{mode});
        void Advance(float dt,bool powered)=>type.GetMethod("Advance").Invoke(motion,new object[]{dt,powered});
        double Phase()=>Convert.ToDouble(type.GetProperty("Phase").GetValue(motion));
        bool Parked()=>(bool)type.GetProperty("Parked").GetValue(motion);
        Assert(Parked(),"Initial wiper not parked");
        Assert(!Select(-1)&&!Select(3)&&Select(1),"Wiper detents not bounded");
        Advance(.25f,true);double slow=Phase();Assert(slow>.14&&slow<.16,"Low speed is not 36 cycles/min");
        Advance(float.NaN,true);Advance(float.PositiveInfinity,true);Advance(-1,true);Assert(Phase()==slow,"Invalid dt moved wipers");
        Advance(.25f,false);Assert(Phase()==slow&&!Parked(),"Power cut teleports or moves wiper");
        Assert(Select(0),"Off command rejected");Advance(.25f,false);Assert(Phase()==slow,"Unpowered parking moved wiper");
        for(int i=0;i<20;i++)Advance(.1f,true);Assert(Parked()&&Phase()==0,"Off does not self-park with restored power");
        Assert(Select(2),"High mode rejected");Advance(.25f,true);Assert(Math.Abs(Phase()-.25)<1e-6,"High speed is not 60 cycles/min");
        Assert(Select(1),"High-to-low transition rejected");double prior=Phase();Advance(.1f,true);Assert(Phase()>prior,"Changing speed resets phase");
        Debug.Log("NF5K_WIPER_MOTION_OK: low/high, bounded commands, phase continuity, powered self-park, powerless freeze and invalid-dt guards");
    }
    public static void Batch()
    {
        try{ValidateMotion();EditorApplication.Exit(0);}
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    public static void ValidateGeometry(CabEngineState state)
    {
        void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        var system=state.GetComponentInChildren<CabWiperSystem>();
        Assert(system!=null,"Wiper assembled implementation missing (expected assembly red)");
        var supply=state.GetComponentInParent<CabInteriorLighting>();bool oldMain=supply.MainPower,oldBattery=supply.BatteryPower;
        try
        {
            system.Configure(system.Assemblies,system.Control,system.Control.Find("Wiper_Three_Position_Selector"),null);
            supply.SetSupply(true,false);
            Assert(system.Powered,"Wiper does not resolve supply installed after its generation");
        }
        finally{supply.SetSupply(oldMain,oldBattery);}
        Assert(system.Assemblies.Length==2,"Expected two linked wipers");
        foreach(var a in system.Assemblies)
        {
            var wire=a.plane.Find("Wiper_Retained_Wiring").GetComponent<Renderer>().bounds;
            var turn=a.plane.Find("Wiper_Wiring_Return").GetComponent<Renderer>().bounds;
            var grommet=a.plane.Find("Wiper_Harness_Entry_Grommet").GetComponent<Renderer>().bounds;
            var flange=a.plane.Find("Wiper_Harness_Bulkhead_Flange").GetComponent<Renderer>().bounds;
            Assert(wire.Intersects(turn)&&turn.Intersects(grommet)&&grommet.Intersects(flange),"Wiper harness has a floating free end");
        }
        Bounds InSystem(MeshRenderer renderer)
        {
            var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
            var points=mesh.vertices.Select(v=>system.transform.InverseTransformPoint(renderer.transform.TransformPoint(v))).ToArray();
            var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);return bounds;
        }
        var surfaces=system.Control.GetComponentsInChildren<MeshRenderer>();var board=InSystem(surfaces[0]);foreach(var surface in surfaces.Skip(1))board.Encapsulate(InSystem(surface));
        foreach(var other in state.GetComponentsInChildren<MeshRenderer>())
        {
            if(other.transform.IsChildOf(system.transform)||!other.enabled)continue;
            if(!other.GetComponentsInParent<Transform>().Any(t=>t.name=="MainConsoleStudy"))continue;
            var otherMesh=other.GetComponent<MeshFilter>();
            if(otherMesh==null||otherMesh.sharedMesh==null)continue; // Use Unity's null check for missing native components.
            var bounds=InSystem(other);
            if(bounds.max.y<=board.min.y+.0005f)continue;
            Assert(!board.Intersects(bounds),"Wiper control overlaps existing equipment: "+other.name);
        }
        system.Motion.Reset();system.Motion.SelectMode(1);
        for(int sample=0;sample<200;sample++)
        {
            system.Advance(1f/120,true);
            foreach(var a in system.Assemblies)
            {
                Assert(Mathf.Abs(a.arm.localScale.y-.460f)<.00001f&&Mathf.Abs(a.parallel.localScale.y-.460f)<.00001f,"Link length changes during sweep");
                Assert(Quaternion.Angle(a.carrier.localRotation,Quaternion.identity)<.001f,"Pantograph rotates blade with arm");
                var mesh=a.rubber.GetComponent<MeshFilter>().sharedMesh;
                float contact=float.NegativeInfinity;
                foreach(var vertex in mesh.vertices)
                {
                    var p=a.glass.InverseTransformPoint(a.rubber.TransformPoint(vertex));contact=Mathf.Max(contact,p.z);
                    Assert(Mathf.Abs(p.x)<.485f&&Mathf.Abs(p.y)<.330f,"Wiper rubber leaves safe glass envelope");
                    Assert(p.z<=.0101f,"Rubber passes through glass");
                }
                Assert(Mathf.Abs(contact-.010f)<.0001f,"Rubber floats away from glass");
            }
        }
        system.Motion.Reset();system.Apply();
        Debug.Log("NF5K_WIPER_GEOMETRY_OK: two fixed-length parallelograms, constant blade attitude, rubber/glass contact and full-cycle aperture clearance");
    }
    public static void AssemblyBatch()
    {
        try{EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");ValidateGeometry(UnityEngine.Object.FindAnyObjectByType<CabEngineState>());EditorApplication.Exit(0);}
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    public static void ValidateInteraction(CabEngineState state)
    {
        void Assert(bool value,string why){if(!value)throw new InvalidOperationException(why);}
        var system=state.GetComponentInChildren<CabWiperSystem>();var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;
        var lighting=state.GetComponentInParent<CabInteriorLighting>();bool main=lighting.MainPower,battery=lighting.BatteryPower;
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var ui=state.GetComponent<CabContextInteraction>();var focus=typeof(CabContextInteraction).GetField("focused",flags);var prior=focus.GetValue(ui);
        var temporary=new GameObject("Temporary_Wiper_Legacy_Binding_Review");temporary.SetActive(false);
        try
        {
            camera.transform.position=system.Control.position+new Vector3(0,.40f,.30f);camera.transform.LookAt(system.Control.position);Physics.SyncTransforms();
            Assert(Physics.Raycast(camera.transform.position,(system.Control.position-camera.transform.position).normalized,out var hit,.7f,1<<30)&&hit.collider.GetComponentInParent<CabWiperSystem>()==system,"Real ray cannot reach wiper control");
            focus.SetValue(ui,system);typeof(CabContextInteraction).GetMethod("BuildChoices",flags).Invoke(ui,null);
            Assert(((System.Collections.ICollection)typeof(CabContextInteraction).GetField("choices",flags).GetValue(ui)).Count==3,"Wiper contextual choices missing");
            lighting.SetSupply(false,false);Assert(system.SelectMode(1),"Unpowered wiper preselection failed");system.Advance(.2f,system.Powered);Assert(system.Motion.Parked,"Wiper runs without supply");
            lighting.SetSupply(true,false);typeof(CabContextInteraction).GetField("selected",flags).SetValue(ui,2);typeof(CabContextInteraction).GetMethod("ExecuteSelected",flags).Invoke(ui,null);
            system.Advance(.25f,system.Powered);Assert(system.Motion.Mode==2&&system.Motion.Phase>.24,"Physical high command not applied");
            double phase=system.Motion.Phase;lighting.SetSupply(false,false);system.Advance(.25f,system.Powered);Assert(system.Motion.Phase==phase,"Power cut moves or parks brush");
            Assert(system.SelectMode(0),"Off rejected");lighting.SetSupply(true,false);for(int i=0;i<20;i++)system.Advance(.1f,system.Powered);Assert(system.Motion.Parked,"Powered off does not return to park");
            var legacy=temporary.AddComponent<Cab3DSceneController>();system.BindLegacy(legacy);legacy.ForceWeather(true);legacy.SetWipers(true);
            Assert(system.Motion.Mode==1,"Legacy on does not select low");Assert(system.SelectMode(2)&&legacy.WipersOn&&system.Motion.Mode==2,"Legacy synchronization overwrites high");
            legacy.ToggleWipers();Assert(system.Motion.Mode==0&&legacy.IsRaining,"Legacy wiper off changes weather or misses physical system");
            system.BindLegacy(null);camera.transform.position=system.Control.position+Vector3.forward*4;Assert(!system.SelectMode(1),"Remote wiper control accepted");
            system.enabled=false;Assert(system.Motion.Parked&&system.Motion.Mode==0,"Disable leaves moving wipers");system.enabled=true;
            Debug.Log("NF5K_WIPER_INTERACTION_OK: real ray/context, three detents, supply gate, power-loss freeze/recovery, self-park, explicit legacy sync, weather separation, distance and disable restoration");
        }
        finally
        {
            system.BindLegacy(null);system.Motion.Reset();system.Apply();UnityEngine.Object.DestroyImmediate(temporary);
            lighting.SetSupply(main,battery);camera.transform.SetPositionAndRotation(position,rotation);focus.SetValue(ui,prior);
        }
    }
    public static void CaptureBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            UnityEngine.Object.FindAnyObjectByType<CabDaylightEnvironment>().Apply(CabDaylightEnvironment.DayPreset.Overcast);
            NF5KBogieDetailReview.ConfigureInspectionLighting();
            var system=UnityEngine.Object.FindAnyObjectByType<CabWiperSystem>();var camera=Camera.main;
            var fill=new GameObject("Temporary_Wiper_Inspection_Fill").AddComponent<Light>();fill.type=LightType.Directional;fill.shadows=LightShadows.None;fill.intensity=.30f;fill.transform.rotation=Quaternion.Euler(35,90,0);
            camera.orthographic=false;camera.fieldOfView=66;camera.transform.position=new Vector3(0,1.53f,-2.40f);camera.transform.LookAt(new Vector3(0,1.48f,-.60f));
            system.Motion.Reset();system.Apply();NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/wipers_front_park_v3.png");
            var plane=system.Assemblies[1].plane;
            camera.transform.position=plane.TransformPoint(new Vector3(0,-.04f,-.90f));camera.transform.LookAt(plane.TransformPoint(new Vector3(0,-.06f,0)));camera.fieldOfView=67;
            system.Motion.SelectMode(2);system.Advance(.25f,true);NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/wiper_midstroke_v3.png");
            system.Advance(.25f,true);NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/wiper_endstroke_v3.png");
            system.Motion.Reset();system.Apply();
            camera.transform.position=plane.TransformPoint(new Vector3(-.025f,-.29f,.52f));camera.transform.LookAt(plane.TransformPoint(new Vector3(-.025f,-.425f,.11f)));camera.fieldOfView=40;
            fill.transform.rotation=Quaternion.Euler(25,150,0);NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/wiper_inner_drive_v3.png");
            camera.transform.position=system.Control.position+new Vector3(.07f,.18f,.16f);camera.transform.LookAt(system.Control.position);camera.fieldOfView=32;
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/wiper_control_v3.png");
            Debug.Log("NF5K_WIPER_CAPTURE_OK: actual parked/mid/end geometry, inside drive and physical control; temporary lighting never saved");EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
