using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class NF5KCabWorkstationReview
{
    [Serializable] sealed class Dimensions
    {
        public float mainWidth,mainCenterX,assistantWidth,assistantDepth,worktopHeight,assistantSeatX,clearSeatGap,frontDeckGap;
        public int sideDoorSamples=176,passageDoorSamples=44;
        public float passageLeafWidth,passageLeafHeight,passageHeightWidthRatio,passageWindowWidth,passageWindowHeight,passageLowerPanelHeight,passageHandleHeight;
    }
    public static void Validate(Transform body)
    {
        void Assert(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        Bounds BoundsInBody(Renderer renderer)
        {
            var points=renderer.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>body.InverseTransformPoint(renderer.transform.TransformPoint(v))).ToArray();
            var b=new Bounds(points[0],Vector3.zero);foreach(var point in points)b.Encapsulate(point);return b;
        }
        var renderers=body.GetComponentsInChildren<Renderer>();
        Renderer Find(string name)=>renderers.First(r=>r.name==name);
        var main=BoundsInBody(Find("Contoured_Control_Deck"));var shelf=BoundsInBody(Find("Assistant_Work_Shelf"));
        var leftSeat=BoundsInBody(Find("Assistant_Seat_Cushion"));var rightSeat=BoundsInBody(Find("Driver_Seat_Cushion"));
        var bridge=BoundsInBody(Find("Aux_Main_Deck_Bridge"));
        var d=new Dimensions {mainWidth=main.size.x,mainCenterX=main.center.x,assistantWidth=shelf.size.x,assistantDepth=shelf.size.z,
            worktopHeight=shelf.max.y,assistantSeatX=body.Find("Assistant_Seat_Pivot").localPosition.x,clearSeatGap=rightSeat.min.x-leftSeat.max.x,frontDeckGap=main.min.x-shelf.max.x};
        Assert(Mathf.Abs(d.mainWidth-1.55f)<.001f&&Mathf.Abs(d.mainCenterX-.55f)<.001f,"Main console moved or resized");
        Assert(Mathf.Abs(d.assistantWidth-.95f)<.001f&&Mathf.Abs(d.assistantDepth-.58f)<.001f,"Auxiliary desk dimensions");
        Assert(Mathf.Abs(main.max.y-shelf.max.y)<.001f&&Mathf.Abs(bridge.max.y-main.max.y)<.001f,"Worktop/bridge height mismatch");
        Assert(bridge.min.x<shelf.max.x&&bridge.max.x>main.min.x,"Front filler is not supported by both desks");
        Assert(Mathf.Abs(d.assistantSeatX+.65f)<.001f&&d.clearSeatGap>.80f,"Assistant seat alignment or seat gap");
        var fixedBoxes=body.GetComponentsInChildren<BoxCollider>().Where(b=>b.name=="Assistant_Work_Shelf"||b.name.StartsWith("Aux_")).ToArray();
        var seats=body.GetComponentsInChildren<BoxCollider>().Where(b=>b.name.EndsWith("Seat_Cushion")||b.name.EndsWith("Seat_Back")).ToArray();
        Physics.SyncTransforms();
        foreach(var fixedBox in fixedBoxes)foreach(var seat in seats)
            Assert(!Physics.ComputePenetration(fixedBox,fixedBox.transform.position,fixedBox.transform.rotation,seat,seat.transform.position,seat.transform.rotation,out _,out var depth)||depth<=.001f,"Desk intersects seat: "+fixedBox.name+" / "+seat.name);
        var route=new GameObject("Temporary_Cab_Centre_Clearance");route.transform.SetParent(body,false);route.transform.localPosition=new Vector3(0,1.1f,1.08f);
        var volume=route.AddComponent<BoxCollider>();volume.size=new Vector3(.62f,1.8f,1.10f);
        try
        {
            foreach(var c in body.GetComponentsInChildren<Collider>())
            {
                if(c==volume||c.isTrigger||!c.enabled)continue;
                Assert(!Physics.ComputePenetration(volume,volume.transform.position,volume.transform.rotation,c,c.transform.position,c.transform.rotation,out _,out var depth)||depth<=.001f,"Rear centre passage obstructed by "+c.name);
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(route);}
        NF5KDoorReview.Validate();
        var passage=body.GetComponentsInChildren<Transform>().First(t=>t.name=="Passenger_Passage_Door_Pivot");
        var panel=BoundsInBody(Find("Passenger_Passage_Door_Lower"));
        var push=BoundsInBody(Find("Passage_Detail_Push_Instruction"));
        Assert(Mathf.Abs(push.size.x-.044f)<.001f&&Mathf.Abs(push.size.y-.045f)<.001f,"Push instruction retained obsolete printed mesh dimensions");
        Assert(Mathf.Abs(push.center.x-NF5KPassageDoorDetail.HandleX)<.001f&&Mathf.Abs(push.center.y-.9475f)<.001f,"Push instruction must sit on the upper handle backplate");
        var handle=BoundsInBody(Find("Passenger_Door_Handle"));
        var windowSill=BoundsInBody(Find("Passage_Detail_Leaf_Window_Sill"));
        var handlePlate=BoundsInBody(Find("Passage_Detail_Latch_Escutcheon"));
        Assert(Mathf.Abs(handle.center.y-NF5KPassageDoorDetail.HandleHeight)<.001f,"Handle and latch height mismatch");
        Assert(handlePlate.max.y<=NF5KPassageDoorDetail.HandlePlateTop+.001f&&windowSill.min.y-handlePlate.max.y>=.019f,"Handle plate must remain below the window sill");
        var stile=BoundsInBody(Find("Passage_Detail_Leaf_Stile"));
        var glazing=BoundsInBody(Find("Passenger_Passage_Door_Glass"));
        d.passageLeafWidth=panel.size.x;d.passageLeafHeight=stile.size.y;d.passageHeightWidthRatio=d.passageLeafHeight/d.passageLeafWidth;
        d.passageWindowWidth=glazing.size.x;d.passageWindowHeight=glazing.size.y;d.passageLowerPanelHeight=panel.size.y;d.passageHandleHeight=handle.center.y;
        Assert(Mathf.Abs(d.passageLeafHeight-NF5KPassageDoorDetail.LeafHeight)<.001f&&d.passageHeightWidthRatio<2.2f,"Door became elongated or vertically scaled");
        Assert(Mathf.Abs(d.passageWindowHeight-.75f)<.001f&&Mathf.Abs(d.passageLowerPanelHeight-.98f)<.001f,"Window/lower-panel proportions drifted");
        Assert(Mathf.Abs(panel.size.x-NF5KPassageDoorDetail.LeafWidth)<.001f,"Passage leaf width mismatch");
        Assert(Mathf.Abs(body.InverseTransformPoint(passage.position).x-NF5KPassageDoorDetail.AxisX)<.001f,"Passage hinge width alignment mismatch");
        var original=passage.localRotation;
        var moving=passage.GetComponentsInChildren<BoxCollider>();
        var obstacles=body.GetComponentsInChildren<BoxCollider>().Where(b=>!b.transform.IsChildOf(passage)&&
            (b.name.EndsWith("Seat_Cushion")||b.name.EndsWith("Seat_Back")||b.name=="Rear_Passenger_Partition"||b.name.StartsWith("Aux_")||b.name=="Assistant_Work_Shelf")).ToArray();
        try
        {
            for(int step=0;step<=43;step++)
            {
                passage.localRotation=original*Quaternion.Euler(0,85f*step/43,0);
                foreach(var leaf in moving)foreach(var obstacle in obstacles)
                    Assert(!Physics.ComputePenetration(leaf,leaf.transform.position,leaf.transform.rotation,obstacle,obstacle.transform.position,obstacle.transform.rotation,out _,out var depth)||depth<=.002f,"Rear passage "+leaf.name+" at sample "+step+" intersects "+obstacle.name+" by "+depth);
            }
        }
        finally{passage.localRotation=original;Physics.SyncTransforms();}
        string name=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        File.WriteAllText("tools/compile/cab_workstation_"+name+".json",JsonUtility.ToJson(d,true));
        Debug.Log("NF5K_CAB_WORKSTATION_OK: "+JsonUtility.ToJson(d)+"; selected seat envelopes, 0.62m rear route, four side door sweeps and offline rear door sweep; not a full anthropometric certification");
    }
}
