using System;
using System.Linq;
using UnityEngine;
using RailwayRenaissance.Cab;
using RailwayRenaissance.Cab.Simulation;

public static class NF5KVigilanceReview
{
    static void Assert(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
    public static void Geometry(CabEngineState state)
    {
        var v=state.GetComponentsInChildren<CabVigilance>();Assert(v.Length==1,"Vigilance assembly duplicated");
        var frame=state.GetComponentInParent<CabInteriorLighting>().transform;
        Assert(!frame.GetComponentsInChildren<Transform>().Any(t=>t.name=="Vigilance_Pedal_Reserved"||t.name=="Driver_Footrest"),"Old footwell placeholders remain");
        var p=v[0].transform.InverseTransformPoint(v[0].Tread.position);Assert(p.y>.05f&&p.y<.09f&&Math.Abs(p.z)<.01f,"Pedal displaced from floor candidate");
        var inclined=v[0].transform.Find("Footrest_Inclined_Frame");Assert(inclined!=null&&Math.Abs(Quaternion.Angle(Quaternion.identity,inclined.localRotation)-15)<.01f,"Footrest slope is not 15 degrees");
        Assert(v[0].Hinge!=null,"Pedal lacks heel hinge");
        var hinge=v[0].Hinge;var rotation=hinge.localRotation;var anchor=hinge.position;
        var treadBounds=v[0].Tread.GetComponent<MeshFilter>().sharedMesh.bounds;
        try
        {
            for(int step=0;step<=8;step++)
            {
                hinge.localRotation=Quaternion.Euler(-step*.25f,0,0);
                foreach(float x in new[]{treadBounds.min.x,treadBounds.max.x})foreach(float z in new[]{treadBounds.min.z,treadBounds.max.z})
                {
                    var corner=inclined.InverseTransformPoint(v[0].Tread.TransformPoint(new Vector3(x,treadBounds.min.y,z)));
                    Assert(corner.y>=-.0003f,"Moving tread penetrates switch housing");
                    Assert(corner.x>.04f&&corner.x<.16f&&Math.Abs(corner.z)<.095f,"Moving tread strikes aperture rim");
                }
                Assert(Vector3.Distance(anchor,hinge.position)<.00001f,"Heel hinge anchor moved during stroke");
            }
        }
        finally{hinge.localRotation=rotation;}
        var basePart=v[0].transform.Find("Footrest_Base");var bounds=basePart.GetComponent<MeshFilter>().sharedMesh.bounds;Assert(Math.Abs(basePart.localPosition.y+bounds.min.y)<.002f,"Footrest not floor-supported");
        Assert(v[0].Tread.GetComponent<Collider>()!=null,"Pedal has no actual hit surface");
        Debug.Log("NF5K_VIGILANCE_GEOMETRY_OK: single floor-supported footrest/pedal, real hit surface and removed placeholders");
    }
    public static void Validate(CabEngineState state)
    {
        var v=state.GetComponentInChildren<CabVigilance>();if(v==null)return;
        Geometry(state);var d=state.GetComponent<CabSimulationDriver>();var checkpoint=d.CaptureCheckpoint();
        var cam=Camera.main;var pos=cam.transform.position;var rot=cam.transform.rotation;bool paused=d.SimulationPaused;
        var ui=state.GetComponent<CabContextInteraction>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var focused=typeof(CabContextInteraction).GetField("focused",flags);var priorFocus=focused.GetValue(ui);
        try
        {
            v.Restore(null);var input=new SimulationInput{Master=true,Throttle=4};var model=new SimulationState{SpeedMps=5};
            for(int i=0;i<5751;i++)v.Step(input,model);
            Assert(v.Phase==1,"Vigilance visual prewarning boundary failed");
            for(int i=0;i<250;i++)v.Step(input,model);
            Assert(v.Phase==2&&!v.Penalty,"Vigilance audible boundary failed");
            for(int i=0;i<401;i++)v.Step(input,model);
            Assert(v.Penalty&&input.Throttle==0&&input.Brake==4,"Vigilance did not impose braking/traction cut");
            input.Master=false;model.SpeedMps=0;v.Step(input,model);Assert(v.Penalty,"Power loss cleared penalty");
            var saved=v.Capture();v.Restore(null);v.Restore(saved);Assert(v.Penalty,"Vigilance latch restore failed");
            v.Restore(null);cam.transform.position=v.Tread.position+Vector3.up*.30f;cam.transform.LookAt(v.Tread);
            Physics.SyncTransforms();Assert(Physics.Raycast(cam.transform.position,Vector3.down,out var hit,.4f,1<<30)&&hit.collider.GetComponentInParent<CabVigilance>()==v,"Pedal actual ray blocked");
            var target=typeof(CabContextInteraction).GetMethod("Target",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{hit.collider});
            Assert(target==v,"Pedal ray resolves to wrong target");focused.SetValue(ui,v);
            typeof(CabContextInteraction).GetMethod("BuildChoices",flags).Invoke(ui,null);
            var choices=(System.Collections.IList)typeof(CabContextInteraction).GetField("choices",flags).GetValue(ui);
            Assert(choices.Count==1,"Vigilance context requires one acknowledgement action");
            typeof(CabContextInteraction).GetField("selected",flags).SetValue(ui,0);
            typeof(CabContextInteraction).GetMethod("ExecuteSelected",flags).Invoke(ui,null);
            Assert(Quaternion.Angle(v.Hinge.localRotation,Quaternion.identity)>1.99f,"Pedal hinge stroke absent");
            d.SimulationPaused=true;Assert(!v.Acknowledge(),"Paused pedal accepts action");d.SimulationPaused=false;
            cam.transform.position+=Vector3.up*4;Assert(!v.Acknowledge(),"Distant pedal accepts action");
            cam.transform.position=pos;v.Restore("{\"version\":1,\"elapsed\":128,\"penalty\":true}");
            var moving=SimulationCodec.Decode<SimulationState>(SimulationCodec.Encode(d.Model.State));moving.SpeedMps=5;d.Model.Restore(moving);
            d.Advance(1);Assert(d.Input.Throttle==0&&d.Input.Brake==4&&d.Model.State.SpeedMps<5,"Real driver tick bypassed vigilance braking");
            cam.transform.position=v.Tread.position+Vector3.up*.3f;
            Assert(!v.Acknowledge(),"Moving penalty accepted reset");
            d.Advance(60);Assert(Math.Abs(d.Model.State.SpeedMps)<.028&&v.Penalty,"Vigilance failed to stop or retain penalty");
            string full=d.CaptureCheckpoint();v.Restore(null);d.RestoreCheckpoint(full);Assert(v.Penalty,"Driver checkpoint lost vigilance latch");
            bool master=d.Input.Master;d.Input.Master=false;
            typeof(CabVigilance).GetMethod("Refresh",flags).Invoke(v,null);
            Assert(v.GetComponent<AudioSource>()!=null&&v.GetComponent<AudioSource>().mute&&v.Penalty,"Power loss did not mute buzzer or cleared latch");d.Input.Master=master;
            state.SetThrottle(0);state.SetBrakeNotch(BrakeNotch.Emergency);d.Advance(0);
            Assert(v.Acknowledge()&&!v.Penalty&&state.ThrottleNotch==0&&state.BrakePos==BrakeNotch.Emergency,"Stopped vigilance reset auto-released brake or failed");
            Debug.Log("NF5K_VIGILANCE_PLAY_OK: fixed-tick visual/audio/penalty stages, actual pedal ray/stroke, pause/distance guards, power-loss latch, driver brake override and checkpoint");
        }
        finally{focused.SetValue(ui,priorFocus);d.SimulationPaused=paused;d.RestoreCheckpoint(checkpoint);cam.transform.SetPositionAndRotation(pos,rot);}
    }
}
