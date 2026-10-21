using System;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KDrivingInteractionSetup
{
    public static void Configure(CabEngineState state)
    {
        Bind(state, "Handwheel_Pivot", CabDrivingAction.Throttle, 240);
        Bind(state, "Tall_Spindle_Pivot", CabDrivingAction.AutomaticBrake, 100);
        Bind(state, "Short_Spindle_Pivot", CabDrivingAction.IndependentBrake, 65);
        Bind(state, "Reverser_Handle", CabDrivingAction.Reverser, 25);
    }
    static void Bind(CabEngineState state, string name, CabDrivingAction action, float travel)
    {
        Transform pivot = null;
        foreach (var t in state.GetComponentsInChildren<Transform>(true))
            if (t.name == name) { pivot = t; break; }
        if (pivot == null) throw new InvalidOperationException("Missing driving pivot: " + name);
        var target = pivot.gameObject.AddComponent<CabDrivingTarget>();
        Vector3 axis = Vector3.right;
        if (action != CabDrivingAction.Reverser)
        {
            string prefix = action == CabDrivingAction.Throttle ? "Controller" : "Brake";
            Transform origin = null, tip = null;
            foreach (var t in state.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == prefix + "_Calibration_Origin") origin = t;
                if (t.name == prefix + "_Calibration_AxisZ") tip = t;
            }
            if (origin == null || tip == null) throw new InvalidOperationException("Missing shaft calibration: " + name);
            axis = pivot.InverseTransformDirection(tip.position - origin.position).normalized;
        }
        target.Configure(state, action, axis, travel);
        int count = 0;
        foreach (var mesh in pivot.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mesh.sharedMesh == null) continue;
            // Exact mesh hits avoid invisible boxes across handwheel holes and between handles.
            var collider = mesh.GetComponent<MeshCollider>();
            if (collider == null) collider = mesh.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh.sharedMesh; count++;
        }
        if (count == 0) throw new InvalidOperationException("No clickable moving meshes: " + name);
        target.ApplyVisual();
        Debug.Log("NF5K_DRIVING_BINDING: " + action + " meshes=" + count);
    }
}
