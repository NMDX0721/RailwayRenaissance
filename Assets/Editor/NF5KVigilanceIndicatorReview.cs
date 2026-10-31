using System;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KVigilanceIndicatorReview
{
    static void Assert(bool pass,string why){if(!pass)throw new InvalidOperationException(why);}
    public static void Geometry(CabEngineState state)
    {
        var lamps=state.GetComponentsInChildren<CabVigilanceIndicator>();Assert(lamps.Length==1,"Expected one vigilance indicator");
        var root=lamps[0].transform;Assert(root.Find("Vigilance_Optical_Face")!=null&&root.Find("Vigilance_Indicator_Legend")!=null,"Indicator optical layers missing");
        Assert(Math.Abs(root.localPosition.x+.1775f)<.0001f&&Math.Abs(root.localPosition.y-.020f)<.0001f,"Indicator not aligned to upper row");
        Debug.Log("NF5K_VIGILANCE_INDICATOR_GEOMETRY_OK: independent optical face and bilingual legend");
    }
    public static void Validate(CabEngineState state)
    {
        var lamp=state.GetComponentInChildren<CabVigilanceIndicator>();if(lamp==null)return;
        var v=state.GetComponentInChildren<CabVigilance>();var panel=state.GetComponentInChildren<CabAnnunciatorPanel>();var driver=state.GetComponent<CabSimulationDriver>();
        string saved=v.Capture();bool master=driver.Input.Master;
        var cam=Camera.main;var camPosition=cam.transform.position;var camRotation=cam.transform.rotation;var camFov=cam.fieldOfView;
        try
        {
            panel.CancelLampTest();v.Restore(null);lamp.Refresh();Assert(!lamp.Lit,"Indicator lit while normal");
            v.Restore("{\"version\":1,\"elapsed\":116,\"penalty\":false}");lamp.Refresh();Assert(lamp.Lit,"Warning indication missing");
            v.Restore("{\"version\":1,\"elapsed\":128,\"penalty\":true}");lamp.Refresh();Assert(lamp.Lit,"Penalty indication missing");
            driver.Input.Master=false;lamp.Refresh();Assert(!lamp.Lit,"Indicator remains lit without supply");driver.Input.Master=master;
            v.Restore(null);Assert(panel.RequestLampTest(),"Lamp test unavailable");panel.Refresh();lamp.Refresh();Assert(lamp.Lit,"Lamp test misses vigilance indicator");
            var center=lamp.transform.parent.TransformPoint(new Vector3(-.0275f,0,0));
            cam.fieldOfView=45;cam.transform.position=center+lamp.transform.forward*.74f+lamp.transform.up*.06f;cam.transform.LookAt(center,lamp.transform.up);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/vigilance_indicator_v5_test.png");
            panel.CancelLampTest();panel.Refresh();lamp.Refresh();Assert(!lamp.Lit,"Lamp test did not restore real indication");
            NF5KDriverLightAudit.CaptureRuntime(state);
            Debug.Log("NF5K_VIGILANCE_INDICATOR_PLAY_OK: warning, penalty, supply and lamp test");
        }
        finally{cam.transform.SetPositionAndRotation(camPosition,camRotation);cam.fieldOfView=camFov;driver.Input.Master=master;panel.CancelLampTest();v.Restore(saved);lamp.Refresh();}
    }
}
