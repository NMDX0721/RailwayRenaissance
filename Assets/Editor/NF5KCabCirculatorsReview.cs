using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KCabCirculatorsReview
{
    public static void Validate(CabEngineState state)
    {
        void Assert(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
        var fans=state.GetComponentsInChildren<CabCirculationFan>();Assert(fans.Length==2,"Expected two circulation fans");
        var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;
        var lighting=state.GetComponentInParent<CabInteriorLighting>();bool main=lighting.MainPower,backup=lighting.BatteryPower;
        var ui=state.GetComponent<CabContextInteraction>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var focus=typeof(CabContextInteraction).GetField("focused",flags);var prior=focus.GetValue(ui);
        try
        {
            foreach(var fan in fans)
            {
                foreach(var renderer in fan.GetComponentsInChildren<Renderer>())Assert(renderer.gameObject.layer==30,"Fan part invisible to cab review camera: "+renderer.name);
                var head=fan.Head;var rotor=head.Find("Fan_Three_Blade_Rotor");
                camera.transform.position=fan.Control.position+new Vector3(0,.45f,.35f);camera.transform.LookAt(fan.Control.position);Physics.SyncTransforms();
                Assert(Physics.Raycast(camera.transform.position,(fan.Control.position-camera.transform.position).normalized,out var hit,.9f,1<<30)&&hit.collider.GetComponentInParent<CabCirculationFan>()==fan,"Fan not reachable by real ray");
                Assert(Mathf.Abs(lighting.transform.InverseTransformPoint(fan.Control.position).y-.8405f)<.002f,"Fan control is not at desk height");
                var panelScrews=fan.Control.GetComponentsInChildren<Transform>().Where(t=>t.name=="Fan_Control_Fixing_Screw").ToArray();
                Assert(panelScrews.Length==4&&panelScrews.Select(t=>Mathf.Sign(t.localPosition.x)+","+Mathf.Sign(t.localPosition.z)).Distinct().Count()==4,"Control plate must have four corner fasteners");
                foreach(var screw in panelScrews)Assert(Mathf.Abs(screw.localPosition.y-.0015f)<.00001f,"Control screw not seated on thin metal plate");
                var dial=fan.Control.Find("Fan_Three_Position_Knob");
                float dialTop=dial.GetComponentsInChildren<MeshFilter>().Max(m=>m.sharedMesh.vertices.Max(v=>fan.Control.InverseTransformPoint(m.transform.TransformPoint(v)).y));
                Assert(dialTop-.0015f<.007f,"Fan dial is too tall above the panel");
                Assert(head.GetComponent<Collider>()==null,"Ceiling fan must not expose an unreachable switch target");
                var logo=head.Find("Fan_Manufacturer_Logo_Chuanlan_Wing_Rail");
                Assert(logo!=null&&!logo.IsChildOf(rotor)&&Mathf.Abs(logo.localPosition.z-.07456f)<.00001f,"Manufacturer logo missing or attached to rotating blades");
                var stem=fan.transform.Find("Fan_Rigid_Drop_Stem");
                float stemWidth=stem.GetComponent<MeshFilter>().sharedMesh.vertices.Max(v=>fan.transform.InverseTransformPoint(stem.TransformPoint(v)).x)-stem.GetComponent<MeshFilter>().sharedMesh.vertices.Min(v=>fan.transform.InverseTransformPoint(stem.TransformPoint(v)).x);
                Assert(Mathf.Abs(stemWidth-.012f)<.0001f,"Fan stem diameter regressed");
                var folded=fan.GetComponentsInChildren<Transform>().First(t=>t.name=="Fan_Continuous_Folded_Yoke").GetComponent<Renderer>();
                Assert(folded.bounds.Intersects(stem.GetComponent<Renderer>().bounds),"Folded fan yoke floats below stem");
                Assert(fan.GetComponentsInChildren<Transform>().Count(t=>t.name=="Fan_Flange_Screw")==4,"Roof screw count changed");
                foreach(var screw in fan.GetComponentsInChildren<Transform>().Where(t=>t.name=="Fan_Flange_Screw"))
                {
                    Assert(screw.Find("Captive_Washer")!=null&&screw.Find("Rounded_Slotted_Head")!=null,"Fastener lacks washer or head");
                    Assert(Mathf.Abs(screw.localPosition.y-.158f)<.0001f,"Roof screw is not seated on flange underside");
                }
                var guard=head.Find("Fan_Wire_Guard").GetComponent<MeshFilter>().sharedMesh;
                Assert(guard.name=="Cab_Circulator_Wire_Cage_V3"&&guard.vertexCount>40000,"Old segmented cage resource remains");
                var points=guard.vertices;var normals=guard.normals;
                for(int ring=0;ring<7;ring++)for(int segment=0;segment<384;segment++)
                {
                    int a=ring*384*16+segment*16,b=ring*384*16+((segment+1)%384)*16;
                    Assert(Vector3.Dot(normals[a],normals[b])>.9998f,"Guard ring shading has a normal seam");
                }
                var motor=head.Find("Fan_Motor_Housing").GetComponent<Renderer>();
                var shaft=head.Find("Fan_Motor_Output_Shaft").GetComponent<Renderer>();
                Assert(shaft.bounds.Intersects(motor.bounds)&&shaft.bounds.Intersects(rotor.Find("Fan_Rotor_Hub").GetComponent<Renderer>().bounds),"Motor shaft floats between housing and hub");
                Assert(head.GetComponentsInChildren<Transform>().Count(t=>t.name=="Fan_Rear_Guard_Anchor")==4,"Rear cage anchors missing");
                foreach(var anchor in head.GetComponentsInChildren<Transform>().Where(t=>t.name=="Fan_Rear_Guard_Anchor"))
                    Assert(anchor.GetComponent<Renderer>().bounds.Intersects(motor.bounds),"Guard anchor floats off motor");
                foreach(var axle in fan.GetComponentsInChildren<Transform>().Where(t=>t.name=="Fan_Head_Pivot_Axle"))
                    Assert(axle.GetComponent<Renderer>().bounds.Intersects(motor.bounds),"Tilt pivot does not enter motor housing");
                var desk=lighting.GetComponentsInChildren<Renderer>().First(t=>t.name==(fan.name.StartsWith("Driver")?"Contoured_Control_Deck":"Assistant_Work_Shelf"));
                var plate=fan.Control.Find("Fan_Control_Metal_Plate").GetComponent<MeshFilter>();
                float bottom=plate.sharedMesh.vertices.Min(v=>lighting.transform.InverseTransformPoint(plate.transform.TransformPoint(v)).y);
                float deskTop=desk.GetComponent<MeshFilter>().sharedMesh.vertices.Max(v=>lighting.transform.InverseTransformPoint(desk.transform.TransformPoint(v)).y);
                Assert(Mathf.Abs(bottom-deskTop)<.001f,"Fan control plate floats above worktop");
                focus.SetValue(ui,fan);typeof(CabContextInteraction).GetMethod("BuildChoices",flags).Invoke(ui,null);
                Assert(((System.Collections.ICollection)typeof(CabContextInteraction).GetField("choices",flags).GetValue(ui)).Count==4,"Fan speed/swing contextual actions missing");
                typeof(CabContextInteraction).GetField("selected",flags).SetValue(ui,1);typeof(CabContextInteraction).GetMethod("ExecuteSelected",flags).Invoke(ui,null);
                Assert(fan.Speed==1,"Context low speed failed");
                lighting.SetSupply(false,false);float phase=fan.Phase;fan.Advance(.1f);Assert(!fan.Powered&&fan.Phase==phase,"Fan rotates without supply");
                lighting.SetSupply(true,false);fan.Advance(.1f);Assert(fan.Powered&&fan.Phase!=phase,"Fan does not rotate with supply");
                Assert(fan.SelectSpeed(2)&&!fan.SelectSpeed(2)&&!fan.SelectSpeed(3),"Fan detent bounds failed");
                var knob=fan.Control.Find("Fan_Three_Position_Knob");
                Assert(Vector3.Dot(knob.localRotation*Vector3.forward,new Vector3(Mathf.Sin(Mathf.PI/3),0,Mathf.Cos(Mathf.PI/3)))>.9999f,"High detent pointer misses printed scale");
                Assert(fan.Control.Find("Fan_Control_Indexed_Scale")!=null&&head.Find("Fan_Rear_Manufacturer_Rating_Plate")!=null,"Scale or manufacturer plate missing");
                typeof(CabContextInteraction).GetField("selected",flags).SetValue(ui,3);typeof(CabContextInteraction).GetMethod("ExecuteSelected",flags).Invoke(ui,null);
                Assert(fan.Oscillating,"Context swing command failed");
                var rocker=fan.Control.Find("Fan_Desk_Swing_Selector");
                Assert(Quaternion.Angle(rocker.localRotation,Quaternion.Euler(12,0,0))<.001f,"Sweep rocker does not show selected end");
                float lowYaw=0,highYaw=0;
                var flexible=fan.transform.Find("Fan_Flexible_Power_Lead");var gland=head.Find("Fan_Cable_Motor_Gland");
                for(int sample=0;sample<80;sample++)
                {
                    fan.Advance(.25f);lowYaw=Mathf.Min(lowYaw,fan.YawAngle);highYaw=Mathf.Max(highYaw,fan.YawAngle);
                    Assert(Mathf.Abs(fan.YawAngle)<=25.001f,"Fan swing exceeds stop envelope");
                    Assert(Vector3.Distance(flexible.TransformPoint(Vector3.down),fan.CableMotorEndpoint)<.0001f,"Swing tears cable away from motor gland");
                    for(int vertex=0;vertex<points.Length;vertex+=64)
                    {
                        var p=lighting.transform.InverseTransformPoint(head.TransformPoint(points[vertex]));
                        Assert(Mathf.Abs(p.x)<1.30f&&p.y<2.095f,"Swing guard crosses wall or roof envelope");
                    }
                }
                Assert(lowYaw< -24.9f&&highYaw>24.9f,"Fan did not traverse both swing limits");
                Assert(fan.ToggleOscillation(),"Fix direction command failed");float fixedYaw=fan.YawAngle;fan.Advance(.2f);Assert(fan.YawAngle==fixedYaw,"Fixed direction keeps swinging");
                Assert(Quaternion.Angle(rocker.localRotation,Quaternion.Euler(-12,0,0))<.001f,"Fixed rocker does not show selected end");
                float runningSpeed=fan.AngularSpeed;lighting.SetSupply(false,false);fan.Advance(.1f);
                Assert(fan.AngularSpeed>0&&fan.AngularSpeed<runningSpeed,"Power loss lacks decelerating coast");
                for(int stop=0;stop<30;stop++)fan.Advance(.1f);
                Assert(fan.AngularSpeed==0,"Fan never stops after power loss");lighting.SetSupply(true,false);
                phase=fan.Phase;fan.Advance(float.NaN);fan.Advance(float.PositiveInfinity);Assert(fan.Phase==phase,"Invalid timestep affected fan");
                foreach(var other in fans)if(other!=fan)Assert(other.Speed==0,"Fan switches are coupled");
                for(int i=0;i<12;i++)
                {
                    rotor.localRotation=Quaternion.Euler(0,0,i*30);
                    foreach(var mesh in rotor.GetComponentsInChildren<MeshFilter>())foreach(var vertex in mesh.sharedMesh.vertices)
                    {
                        var p=head.InverseTransformPoint(mesh.transform.TransformPoint(vertex));
                        Assert(new Vector2(p.x,p.y).magnitude<.086f&&p.z>-.015f&&p.z<.025f,"Rotor leaves guard clearance envelope");
                    }
                }
                foreach(var flange in fan.GetComponentsInChildren<Transform>().Where(t=>t.name=="Fan_Roof_Isolator"))
                {
                    float top=flange.GetComponent<MeshFilter>().sharedMesh.vertices.Max(v=>lighting.transform.InverseTransformPoint(flange.TransformPoint(v)).y);
                    Assert(Mathf.Abs(top-2.10f)<.001f,"Fan flange does not meet the roof");
                }
                camera.transform.position=fan.Control.position+Vector3.forward*3;Assert(!fan.SelectSpeed(0),"Remote fan operation accepted");
                fan.enabled=false;Assert(fan.Speed==0&&fan.Phase==0,"Fan disable did not reset");fan.enabled=true;
            }
            Debug.Log("NF5K_CIRCULATORS_PLAY_OK: two real rays, independent 0/1/2 commands, supply gate, rotor envelope, roof contact, distance and disable restoration; visual motor only, no airflow/load solver");
        }
        finally
        {
            foreach(var fan in fans){fan.enabled=false;fan.enabled=true;}
            lighting.SetSupply(main,backup);focus.SetValue(ui,prior);camera.transform.SetPositionAndRotation(position,rotation);
        }
    }
}
