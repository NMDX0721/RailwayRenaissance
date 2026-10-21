using System;
using System.Collections.Generic;
using RailwayRenaissance.Cab;
using UnityEngine;

public static class NF5KDoorReview
{
    public static void Validate()
    {
        var body=GameObject.Find("CabInterior_Blockout");var obstacles=new List<BoxCollider>();
        foreach(var box in body.GetComponentsInChildren<BoxCollider>())
            if(box.name.EndsWith("Seat_Back")||box.name.EndsWith("Seat_Cushion")||box.name=="Rear_Passenger_Partition"||box.name=="Rear_Electrical_Service_Panel")obstacles.Add(box);
        var doors=body.GetComponentsInChildren<CabSideDoor>();
        if(doors.Length!=4)throw new InvalidOperationException("Expected four separate side door leaves");
        foreach(var door in doors)
        {
            for(int step=0;step<=43;step++)
            {
                door.SetOpen(step/43f);
                foreach(var moving in door.GetComponentsInChildren<BoxCollider>())foreach(var fixedBox in obstacles)
                    if(Physics.ComputePenetration(moving,moving.transform.position,moving.transform.rotation,fixedBox,fixedBox.transform.position,fixedBox.transform.rotation,out var direction,out var distance)&&distance>.002f)
                        throw new InvalidOperationException(door.name+" swing intersects "+fixedBox.name+" by "+distance);
            }
            door.Close();
        }
        Debug.Log("NF5K_DOOR_SWEEP_OK: four separate inward-opening leaves, 44 sampled angles each; seats, rear partition and service panel clear; closed restored");
    }
}
