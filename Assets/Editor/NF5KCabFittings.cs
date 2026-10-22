using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KCabFittings
{
    public static void Apply(Transform body)
    {
        var state=body.GetComponentInChildren<CabEngineState>();
        foreach(var t in body.GetComponentsInChildren<Transform>())
            if(t!=null&&t.name=="Cab_Service_Fittings")UnityEngine.Object.DestroyImmediate(t.gameObject);
        var root=new GameObject("Cab_Service_Fittings").transform;root.SetParent(body,false);
        Material M(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/"+name+".mat");
        var enamel=M("Air_Reservoir_Warm_Grey");var steel=M("Service_Fitting_Steel");var black=M("Service_Hose_Rubber");var red=M("Service_Valve_Red");
        if(enamel==null||steel==null||black==null||red==null)throw new InvalidOperationException("Service materials missing");
        GameObject Part(string name,PrimitiveType type,Vector3 p,Vector3 size,Material mat,Transform parent=null)
        {
            var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=size;
            g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
            if(parent!=null)g.transform.SetParent(parent,true);g.layer=30;return g;
        }
        void Box(string name,Vector3 p,Vector3 size,Material mat)=>Part(name,PrimitiveType.Cube,p,size,mat);
        void Rod(string name,Vector3 a,Vector3 b,float r,Material mat)
        {
            var g=Part(name,PrimitiveType.Cylinder,(a+b)*.5f,new Vector3(r*2,Vector3.Distance(a,b)*.5f,r*2),mat);
            g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
        }
        // Compact selector beside the driver's seat, outside the central aisle and door sweep.
        Box("Parking_Selector_Pedestal",new Vector3(.99f,.34f,.75f),new Vector3(.16f,.68f,.24f),enamel);
        Box("Parking_Selector_Bezel",new Vector3(.99f,.695f,.75f),new Vector3(.18f,.025f,.26f),black);
        Part("Selector_Shaft_Collar",PrimitiveType.Cylinder,new Vector3(.99f,.715f,.75f),new Vector3(.055f,.012f,.055f),steel);
        foreach(float x in new[]{.92f,1.06f})foreach(float z in new[]{.65f,.85f})
            Part("Selector_Plate_Fastener",PrimitiveType.Cylinder,new Vector3(x,.710f,z),new Vector3(.012f,.002f,.012f),steel);
        foreach(float z in new[]{.655f,.845f})Box("Selector_End_Mark",new Vector3(.99f,.710f,z),new Vector3(.025f,.003f,.008f),enamel);
        var lever=Part("Parking_Brake_Selector",PrimitiveType.Cube,new Vector3(.99f,.735f,.75f),new Vector3(.035f,.045f,.14f),red);
        var collider=lever.AddComponent<BoxCollider>();
        root.SetParent(state.transform,true);
        var target=lever.AddComponent<CabDrivingTarget>();target.Configure(state,CabDrivingAction.ParkingBrake,Vector3.up,65);target.ApplyVisual();
        // Right-hand auxiliary station in the driver's facing direction, not a second driving desk.
        Box("Assistant_Work_Shelf",new Vector3(-.86f,.80f,-.09f),new Vector3(.65f,.045f,.36f),enamel);
        foreach(float x in new[]{-1.10f,-.62f})
            Rod("Shelf_Wall_Bracket",new Vector3(x,.77f,-.24f),new Vector3(x,.55f,-.36f),.014f,steel);
        Box("Radio_Receiver_Visual_Only",new Vector3(-.87f,.89f,-.17f),new Vector3(.29f,.13f,.16f),black);
        Box("Radio_Faceplate",new Vector3(-.87f,.89f,-.082f),new Vector3(.26f,.105f,.012f),enamel);
        foreach(float x in new[]{-.985f,-.755f})foreach(float y in new[]{.85f,.93f})
            Part("Radio_Face_Screw",PrimitiveType.Sphere,new Vector3(x,y,-.074f),new Vector3(.008f,.008f,.005f),steel);
        for(int i=0;i<7;i++)Box("Radio_Speaker_Slot",new Vector3(-.94f+i*.012f,.89f,-.074f),new Vector3(.004f,.065f,.004f),black);
        for(int i=0;i<2;i++)Part("Radio_Control_Knob",PrimitiveType.Sphere,new Vector3(-.82f+i*.055f,.885f,-.067f),new Vector3(.025f,.025f,.018f),black);
        Box("Handset_Cradle",new Vector3(-.60f,.86f,-.13f),new Vector3(.07f,.045f,.19f),steel);
        Rod("Handset_Grip",new Vector3(-.60f,.91f,-.20f),new Vector3(-.60f,.91f,-.06f),.018f,black);
        foreach(float z in new[]{-.20f,-.06f})Part("Handset_Earpiece",PrimitiveType.Sphere,new Vector3(-.60f,.895f,z),new Vector3(.06f,.045f,.05f),black);
        Rod("Handset_Cord",new Vector3(-.60f,.885f,-.06f),new Vector3(-.53f,.70f,-.06f),.004f,black);
        Rod("Handset_Cord_Return",new Vector3(-.53f,.70f,-.06f),new Vector3(-.72f,.86f,-.17f),.004f,black);
        Box("Timetable_Clipboard",new Vector3(-1.05f,.829f,.005f),new Vector3(.18f,.008f,.15f),black);
        Box("Timetable_Blank_Insert",new Vector3(-1.05f,.835f,.005f),new Vector3(.16f,.003f,.13f),enamel);
        Box("Timetable_Clip",new Vector3(-1.05f,.84f,-.055f),new Vector3(.06f,.008f,.015f),steel);
        // Retaining lip and divided pockets keep small items off the floor.
        Box("Assistant_Map_Pocket_Back",new Vector3(-1.32f,.57f,.15f),new Vector3(.035f,.27f,.40f),enamel);
        Box("Assistant_Map_Pocket_Front",new Vector3(-1.24f,.53f,.15f),new Vector3(.025f,.19f,.40f),enamel);
        foreach(float z in new[]{-.05f,.15f,.35f})Box("Map_Pocket_Divider",new Vector3(-1.28f,.53f,z),new Vector3(.10f,.19f,.015f),steel);
        Box("Map_Pocket_Base",new Vector3(-1.28f,.435f,.15f),new Vector3(.10f,.015f,.40f),enamel);
        NF5KIndustrialFinish.FinishRunningGear(root);
        Debug.Log("NF5K_CAB_FITTINGS_OK: physical parking selector; auxiliary radio/handset, shelf and map pockets are visual only");
    }
    public static void BatchBuild()
    {
        try
        {
            foreach(string name in new[]{"CabInteriorBlockout","CabDaylightReview","CabSimulationReview","CabRailMotionReview"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");Apply(GameObject.Find("CabInterior_Blockout").transform);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            UnityEngine.Object.FindFirstObjectByType<CabDaylightEnvironment>().Apply(CabDaylightEnvironment.DayPreset.Overcast);
            NF5KBogieDetailReview.ConfigureInspectionLighting();
            var lights=UnityEngine.Object.FindFirstObjectByType<CabInteriorLighting>();lights.SetAssistantTask(true);
            var camera=Camera.main;camera.orthographic=false;camera.fieldOfView=65;
            camera.transform.position=new Vector3(-.40f,1.31f,.65f);camera.transform.LookAt(new Vector3(-.87f,.85f,-.10f));
            var fill=new GameObject("Temporary_Cab_Fittings_Fill").AddComponent<Light>();fill.type=LightType.Point;fill.range=4;fill.intensity=1.5f;fill.transform.position=new Vector3(0,1.7f,.65f);
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/assistant_station_fittings_final.png");
            camera.transform.position=new Vector3(.25f,1.18f,1.05f);camera.transform.LookAt(new Vector3(.98f,.71f,.75f));
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/parking_selector_fittings_final.png");
            EditorSceneManager.OpenScene("Assets/Scenes/CabRailMotionReview.unity");NF5KSimulationPlayReview.Begin();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
