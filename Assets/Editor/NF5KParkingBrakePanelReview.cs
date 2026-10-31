using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KParkingBrakePanelReview
{
    static void Assert(bool pass,string why){if(!pass)throw new InvalidOperationException(why);}
    static Bounds InFrame(Transform frame,MeshFilter mesh)
    {
        var points=mesh.sharedMesh.vertices.Select(v=>frame.InverseTransformPoint(mesh.transform.TransformPoint(v))).ToArray();
        var result=new Bounds(points[0],Vector3.zero);foreach(var p in points)result.Encapsulate(p);return result;
    }
    static float Distance(Bounds a,Bounds b)=>Vector3.Max(Vector3.zero,Vector3.Max(a.min-b.max,b.min-a.max)).magnitude;
    [Serializable] sealed class Placement
    {
        public Vector3 anchor,panelMin,panelMax;public float minSeatCandidateGap,minKneeCandidateGap,minBrakeGap;
        public int seatPoseSamples;public string scope="Actual mesh bounds, actual deck contact, authored seat/knee candidates; not a full anthropometric certification";
    }
    public static void Geometry(CabEngineState state)
    {
        var buttons=state.GetComponentsInChildren<CabParkingBrakeButton>();Assert(buttons.Length==2,"Parking requires two independent buttons");
        Assert(buttons.Count(b=>b.ReleaseButton)==1,"Parking button functions duplicated");
        var panel=buttons[0].transform.parent;var root=panel.parent;var frame=state.GetComponentInParent<CabInteriorLighting>().transform;
        var all=frame.GetComponentsInChildren<MeshFilter>();
        Bounds B(Transform t)=>InFrame(panel,t.GetComponent<MeshFilter>());
        var plate=panel.Find("Parking_Panel_Flush_Bezel");var plateB=B(plate);var gasket=B(panel.Find("Parking_Panel_Seating_Gasket"));
        Assert(Mathf.Abs(plateB.min.y-gasket.max.y)<.0002f&&Mathf.Abs(plateB.size.y-.003f)<.0002f,"Parking panel/gasket thickness or support mismatch");
        foreach(var button in buttons)
        {
            var seal=B(button.transform.Find("Parking_Button_Seal"));var collar=B(button.transform.Find("Parking_Button_Threaded_Collar"));var cap=B(button.Cap);
            Assert(Mathf.Abs(seal.min.y-plateB.max.y)<.0002f&&Mathf.Abs(collar.min.y-seal.max.y)<.0002f,"Parking collar/seal not seated");
            Assert(cap.Intersects(collar)&&button.GetComponentsInChildren<MeshCollider>().Length==1,"Parking cap disconnected or lacks real mesh hit");
            Assert(cap.min.x>plateB.min.x&&cap.max.x<plateB.max.x&&cap.min.z>plateB.min.z&&cap.max.z<plateB.max.z,"Parking button overhangs plate");
            var origin=button.transform.position+panel.up*.08f;
            Assert(!plate.GetComponent<MeshCollider>().Raycast(new Ray(origin,-panel.up),out _,.12f),"Parking switch aperture is blocked by face plate");
        }
        foreach(string n in new[]{"Parking_Selector_Pedestal","Parking_Selector_Floor_Flange","Pedestal_Floor_Bolt","Selector_Service_Cover","Parking_Selector_Bezel","Parking_Integrated_Scale","Selector_Shaft_Collar"})
            Assert(!root.GetComponentsInChildren<Transform>().Any(t=>t.name==n),"Retired floor stand/rotary part remains: "+n);
        var owner=state.GetComponentsInChildren<CabDrivingTarget>().Single(t=>t.Action==CabDrivingAction.ParkingBrake);
        Assert(!owner.GetComponentsInChildren<Renderer>().Any(r=>r.enabled)&&owner.GetComponentsInChildren<Collider>().Length==0,"Old selector still visible/clickable");
        var deck=all.Single(f=>f.name=="Contoured_Control_Deck");var contact=panel.Find("Parking_Console_Contact_Gasket").GetComponent<MeshFilter>();
        foreach(var v in contact.sharedMesh.vertices)
        {var point=contact.transform.TransformPoint(v);Assert(Vector3.Distance(point,NF5KParkingConsoleMount.Nearest(deck,point))<.0012f,"Console mount has unsupported contact perimeter");}
        var parts=panel.GetComponentsInChildren<MeshFilter>();var total=InFrame(frame,parts[0]);foreach(var f in parts)total.Encapsulate(InFrame(frame,f));
        var d=new Placement{anchor=frame.InverseTransformPoint(panel.position),panelMin=total.min,panelMax=total.max,minBrakeGap=100,minSeatCandidateGap=100,minKneeCandidateGap=100};
        foreach(var f in all.Where(f=>(f.name.StartsWith("Short_")||f.name.StartsWith("Tall_")||f.name=="Service_Control_Seated_Plate")&&f.GetComponent<Renderer>()!=null&&f.GetComponent<Renderer>().enabled))
        {var b=InFrame(frame,f);d.minBrakeGap=Mathf.Min(d.minBrakeGap,Distance(total,b));var expanded=total;expanded.Expand(.030f);Assert(!expanded.Intersects(b),"Parking panel too close to existing brake/service hardware: "+f.name);}
        var seatParts=all.Where(f=>f.name.StartsWith("Driver_Seat_")).Select(f=>InFrame(frame,f)).ToArray();
        var seat=all.Single(f=>f.name=="Driver_Seat_Cushion");float seatX=InFrame(frame,seat).center.x;
        foreach(float slide in new[]{-.12f,-.06f,0f,.06f,.12f})foreach(float height in new[]{-.05f,0f,.05f})
        {
            d.seatPoseSamples++;var delta=new Vector3(0,height,slide);
            foreach(var b0 in seatParts){var b=b0;b.center+=delta;float gap=Distance(total,b);d.minSeatCandidateGap=Mathf.Min(d.minSeatCandidateGap,gap);Assert(gap>.03f,"Parking console mount intersects authored seat travel candidate");}
            var knee=new Bounds(new Vector3(seatX,.64f+height,.48f+slide),new Vector3(.42f,.20f,.38f));
            float kgap=Distance(total,knee);d.minKneeCandidateGap=Mathf.Min(d.minKneeCandidateGap,kgap);Assert(kgap>.03f,"Parking console mount intersects authored knee candidate");
        }
        System.IO.File.WriteAllText("tools/compile/parking_mount_"+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+".json",JsonUtility.ToJson(d,true));
        Debug.Log("NF5K_PARKING_BUTTON_GEOMETRY_OK: perforated 3 mm face, rear switch bodies, real console contact, removed floor stand; 15 mm brake margin and authored seat/knee candidates clear");
    }
    public static void Validate(CabEngineState state)
    {
        Geometry(state);
        var buttons=state.GetComponentsInChildren<CabParkingBrakeButton>();var driver=state.GetComponent<CabSimulationDriver>();
        var cam=Camera.main;var position=cam.transform.position;var rotation=cam.transform.rotation;string snapshot=driver.CaptureCheckpoint();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var ui=state.GetComponent<CabContextInteraction>();
        var focus=typeof(CabContextInteraction).GetField("focused",flags);var prior=focus.GetValue(ui);bool english=CabLightingKey.English;
        try
        {
            Assert(driver.Input.Master&&driver.Model.State.Running,"Parking review requires started control power");
            foreach(var button in buttons)
            {
                var body=state.GetComponentInParent<CabInteriorLighting>().transform;
                var eye=body.TransformPoint(new Vector3(.65f,1.38f,.78f));
                Physics.SyncTransforms();
                Assert(Physics.Raycast(eye,(button.Cap.position-eye).normalized,out var seatHit,Vector3.Distance(eye,button.Cap.position)+.02f,1<<30)&&seatHit.collider.GetComponentInParent<CabParkingBrakeButton>()==button,"Seated-eye parking cap ray blocked");
                driver.ParkingBrake=button.ReleaseButton;driver.Advance(.02);
                cam.transform.position=button.Cap.position+button.transform.up*.25f;cam.transform.LookAt(button.Cap.position,button.transform.forward);Physics.SyncTransforms();
                var ray=new Ray(cam.transform.position,-button.transform.up);
                Assert(Physics.Raycast(ray,out var hit,.30f,1<<30)&&hit.collider.GetComponentInParent<CabParkingBrakeButton>()==button,"Parking cap ray blocked");
                Assert(typeof(CabContextInteraction).GetMethod("Target",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{hit.collider})==button,"Parking cap resolves to wrong contextual target");
                focus.SetValue(ui,button);typeof(CabContextInteraction).GetMethod("BuildChoices",flags).Invoke(ui,null);
                var choices=(System.Collections.IList)typeof(CabContextInteraction).GetField("choices",flags).GetValue(ui);
                Assert(choices.Count==1&&(bool)choices[0].GetType().GetField("deliberate").GetValue(choices[0]),"Parking action lacks single long-hold choice");
                var rest=button.Cap.localPosition;
                typeof(CabContextInteraction).GetField("selected",flags).SetValue(ui,0);typeof(CabContextInteraction).GetMethod("ExecuteSelected",flags).Invoke(ui,null);
                Assert(driver.ParkingBrake==!button.ReleaseButton&&Mathf.Abs(button.Cap.localPosition.y-rest.y+.0018f)<.00001f,"Parking context command or cap stroke failed");
                driver.Advance(.02);Assert(driver.Input.ParkingBrake==!button.ReleaseButton,"Parking command did not reach simulation input");
                Assert(!button.Press(),"Repeated already-selected parking command accepted");
                button.enabled=false;Assert(button.Cap.localPosition==rest&&!button.Press(),"Disabled parking button did not restore/guard");button.enabled=true;
                driver.ParkingBrake=button.ReleaseButton;
                Assert(state.GetComponent<CabStartupPointer>().ActivateRay(ray)&&driver.ParkingBrake==!button.ReleaseButton,"Real parking ray did not route to fixed button action");
                driver.ParkingBrake=button.ReleaseButton;
                bool power=driver.Input.Master;driver.Input.Master=false;button.Refresh();
                Assert(button.Cap.localPosition==rest&&!button.Press()&&driver.ParkingBrake==button.ReleaseButton,"Power loss changed parking command or left cap pressed");driver.Input.Master=power;
                cam.transform.position+=Vector3.up*4;Assert(!button.Press(),"Remote parking action accepted");cam.transform.position=button.Cap.position+button.transform.up*.25f;
                float timeScale=Time.timeScale;try{Time.timeScale=0;Assert(!button.Press(),"Paused parking accepted");}finally{Time.timeScale=timeScale;}
                driver.Model.State.SpeedMps=1;driver.Advance(0);Assert(!button.Press(),"Moving parking accepted");driver.Model.State.SpeedMps=0;driver.Advance(0);
                CabLightingKey.SetLanguage("en");Assert(button.Hint.Contains(button.ReleaseButton?"RELEASE":"APPLY"),"English parking label missing");CabLightingKey.SetLanguage("zh");
            }
            Debug.Log("NF5K_PARKING_BUTTON_PLAY_OK: red apply/green release actual mesh rays, one deliberate context action per cap, backend input, spring return, power/distance/motion/pause/disable guards and bilingual hints");
        }
        finally
        {
            foreach(var button in buttons){button.enabled=false;button.enabled=true;}
            driver.RestoreCheckpoint(snapshot);cam.transform.SetPositionAndRotation(position,rotation);focus.SetValue(ui,prior);CabLightingKey.SetLanguage(english?"en":"zh");
        }
    }
}
