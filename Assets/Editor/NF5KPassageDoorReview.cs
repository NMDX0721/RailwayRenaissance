using System;
using System.Reflection;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KPassageDoorReview
{
    public static void Validate(CabEngineState state)
    {
        void Assert(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        var body=state.GetComponentInParent<CabInteriorLighting>();var door=body.GetComponentInChildren<CabPassageDoor>();
        Assert(door!=null,"Passage door interaction missing");
        var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;
        var closed=door.transform.localRotation;var ui=state.GetComponent<CabContextInteraction>();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var focus=typeof(CabContextInteraction).GetField("focused",flags);var prior=focus.GetValue(ui);
        var build=typeof(CabContextInteraction).GetMethod("BuildChoices",flags);var execute=typeof(CabContextInteraction).GetMethod("ExecuteSelected",flags);
        GameObject obstacle=null;
        try
        {
            camera.transform.position=body.transform.TransformPoint(new Vector3(0,1.25f,2.50f));Physics.SyncTransforms();
            Assert(Physics.Raycast(camera.transform.position,body.transform.TransformDirection(Vector3.back),out var hit,1,1<<30)&&hit.collider.GetComponentInParent<CabPassageDoor>()==door,"Rear passage door ray unreachable");
            focus.SetValue(ui,door);build.Invoke(ui,null);typeof(CabContextInteraction).GetField("selected",flags).SetValue(ui,0);execute.Invoke(ui,null);
            Assert(door.Moving&&!door.Toggle(),"Door primary action or repeated-motion guard failed");
            for(int i=0;i<30;i++)door.Advance(.02f);
            Assert(door.IsOpen,"Passage door did not reach open stop: "+door.Angle+" "+door.LastWarning);
            Assert(door.Toggle(),"Passage door close rejected");for(int i=0;i<30;i++)door.Advance(.02f);
            Assert(door.Angle==0,"Passage door did not close: "+door.LastWarning);
            camera.transform.position=body.transform.TransformPoint(new Vector3(0,1.25f,5));Assert(!door.Toggle(),"Remote passage door action accepted");
            camera.transform.position=body.transform.TransformPoint(new Vector3(0,1.25f,2.50f));
            obstacle=new GameObject("Temporary_Passage_Obstruction");obstacle.transform.SetParent(body.transform,false);obstacle.transform.localPosition=new Vector3(.04f,.50f,1.68f);
            obstacle.AddComponent<BoxCollider>().size=new Vector3(.12f,.70f,.12f);
            // Refresh the obstacle bindings exactly as a new runtime scene would.
            typeof(CabPassageDoor).GetMethod("Cache",flags).Invoke(door,null);
            Assert(door.Toggle(),"Obstruction check could not start");for(int i=0;i<30;i++)door.Advance(.02f);
            Assert(!door.IsOpen&&!door.Moving&&!string.IsNullOrEmpty(door.LastWarning),"Door did not stop before fixed obstruction");
            Assert(door.Toggle()&&door.NextActionCloses==false,"Blocked door did not allow reverse operation");
            UnityEngine.Object.DestroyImmediate(obstacle);obstacle=null;
            door.enabled=false;Assert(door.Angle==0&&Quaternion.Angle(door.transform.localRotation,closed)<.01f,"Passage door disable restoration failed");door.enabled=true;
            camera.transform.position=body.transform.TransformPoint(new Vector3(.04f,.50f,1.65f));
            Assert(door.Toggle(),"Camera clearance check could not start");for(int i=0;i<30;i++)door.Advance(.02f);
            Assert(!door.IsOpen&&!door.Moving&&!string.IsNullOrEmpty(door.LastWarning),"Door did not stop before camera clearance envelope");
            Debug.Log("NF5K_PASSAGE_DOOR_PLAY_OK: real rear ray/context, bounded swing, repeated-motion and distance guards, fixed obstruction stop/reverse, disable restoration; camera clearance is not a full player capsule");
        }
        finally
        {
            if(obstacle!=null)UnityEngine.Object.DestroyImmediate(obstacle);
            door.enabled=false;door.enabled=true;focus.SetValue(ui,prior);camera.transform.SetPositionAndRotation(position,rotation);
        }
    }
}
