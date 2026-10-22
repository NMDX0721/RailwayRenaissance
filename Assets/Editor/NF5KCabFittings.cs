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
        Material Finish(string name,Color color,float metal,float smooth)
        {
            string path="Assets/Resources/Cab/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
        }
        enamel=Finish("Cab_Service_Satin_Enamel",new Color(.43f,.50f,.47f),.12f,.24f);
        var paper=Finish("Cab_Service_Paper",new Color(.81f,.78f,.65f),0,.08f);
        var safety=Finish("Cab_Safety_Red_Enamel",new Color(.57f,.07f,.055f),.18f,.27f);
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
        void Label(string name,string text,Vector3 p,float size,Quaternion rotation)
        {
            var g=new GameObject(name);g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localRotation=rotation;g.layer=30;
            var t=g.AddComponent<TextMesh>();t.text=text;t.font=Resources.Load<Font>("Fonts/SarasaMonoSC-Regular")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.color=new Color(.88f,.86f,.73f);
            string path="Assets/Resources/Cab/Cab_World_Label.mat";
            var ink=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(ink==null){ink=new Material(Shader.Find("Railway/CabWorldLabel"));AssetDatabase.CreateAsset(ink,path);}
            g.GetComponent<MeshRenderer>().sharedMaterial=ink;
            g.AddComponent<CabWorldLabel>();
        }
        // Compact selector beside the driver's seat, outside the central aisle and door sweep.
        Box("Parking_Selector_Pedestal",new Vector3(.99f,.34f,.75f),new Vector3(.16f,.68f,.24f),enamel);
        Box("Parking_Selector_Floor_Flange",new Vector3(.99f,.012f,.75f),new Vector3(.20f,.024f,.28f),steel);
        foreach(float x in new[]{.91f,1.07f})foreach(float z in new[]{.64f,.86f})
            Part("Pedestal_Floor_Bolt",PrimitiveType.Cylinder,new Vector3(x,.027f,z),new Vector3(.014f,.004f,.014f),steel);
        Box("Selector_Service_Cover",new Vector3(.905f,.35f,.75f),new Vector3(.009f,.42f,.18f),black);
        Box("Selector_Service_Inset",new Vector3(.899f,.35f,.75f),new Vector3(.005f,.405f,.165f),enamel);
        Box("Parking_Selector_Bezel",new Vector3(.99f,.695f,.75f),new Vector3(.18f,.025f,.26f),black);
        Part("Selector_Shaft_Collar",PrimitiveType.Cylinder,new Vector3(.99f,.715f,.75f),new Vector3(.055f,.012f,.055f),steel);
        foreach(float x in new[]{.92f,1.06f})foreach(float z in new[]{.65f,.85f})
            Part("Selector_Plate_Fastener",PrimitiveType.Cylinder,new Vector3(x,.710f,z),new Vector3(.012f,.002f,.012f),steel);
        foreach(float z in new[]{.655f,.845f})Box("Selector_End_Mark",new Vector3(.99f,.710f,z),new Vector3(.025f,.003f,.008f),enamel);
        var lever=Part("Parking_Brake_Selector",PrimitiveType.Cube,new Vector3(.99f,.735f,.75f),new Vector3(.028f,.036f,.105f),red);
        var collider=lever.AddComponent<BoxCollider>();
        root.SetParent(state.transform,true);
        var target=lever.AddComponent<CabDrivingTarget>();target.Configure(state,CabDrivingAction.ParkingBrake,Vector3.up,65);target.ApplyVisual();
        Label("Parking_Plate_Legend","PARK",new Vector3(.99f,.711f,.66f),.003f,Quaternion.Euler(90,0,0));
        Label("Parking_Release_Legend","REL",new Vector3(1.047f,.711f,.79f),.002f,Quaternion.Euler(90,0,0));
        Label("Parking_Apply_Legend","SET",new Vector3(.936f,.711f,.79f),.002f,Quaternion.Euler(90,0,0));
        // Right-hand auxiliary station in the driver's facing direction, not a second driving desk.
        Box("Assistant_Work_Shelf",new Vector3(-.86f,.80f,-.09f),new Vector3(.65f,.045f,.36f),enamel);
        foreach(float x in new[]{-1.10f,-.62f})
        {
            Rod("Shelf_Wall_Bracket",new Vector3(x,.77f,-.04f),new Vector3(x,.55f,-.40f),.014f,steel);
            Box("Shelf_Mounting_Plate",new Vector3(x,.67f,-.40f),new Vector3(.075f,.25f,.018f),enamel);
            foreach(float y in new[]{.58f,.76f})Part("Shelf_Mount_Fastener",PrimitiveType.Sphere,new Vector3(x,y,-.388f),new Vector3(.014f,.014f,.008f),steel);
        }
        Box("Shelf_Rear_Retaining_Lip",new Vector3(-.86f,.845f,-.265f),new Vector3(.65f,.065f,.015f),enamel);
        Box("Radio_Receiver_Visual_Only",new Vector3(-.87f,.89f,-.17f),new Vector3(.29f,.13f,.16f),black);
        Box("Radio_Faceplate",new Vector3(-.87f,.89f,-.082f),new Vector3(.26f,.105f,.012f),enamel);
        foreach(float x in new[]{-.985f,-.755f})foreach(float y in new[]{.85f,.93f})
            Part("Radio_Face_Screw",PrimitiveType.Sphere,new Vector3(x,y,-.074f),new Vector3(.008f,.008f,.005f),steel);
        for(int i=0;i<7;i++)Box("Radio_Speaker_Slot",new Vector3(-.94f+i*.012f,.89f,-.074f),new Vector3(.004f,.065f,.004f),black);
        for(int i=0;i<2;i++)
        {
            var knob=Part("Radio_Control_Knob",PrimitiveType.Cylinder,new Vector3(-.82f+i*.055f,.885f,-.067f),new Vector3(.025f,.010f,.025f),black);knob.transform.localRotation=Quaternion.Euler(90,0,0);
            Box("Radio_Knob_Index",new Vector3(-.82f+i*.055f,.893f,-.055f),new Vector3(.003f,.006f,.003f),paper);
        }
        Label("Radio_Engraved_Legend","RADIO",new Vector3(-.81f,.925f,-.073f),.002f,Quaternion.Euler(0,180,0));
        Box("Handset_Cradle",new Vector3(-.60f,.86f,-.13f),new Vector3(.07f,.045f,.19f),steel);
        Rod("Handset_Grip",new Vector3(-.60f,.91f,-.20f),new Vector3(-.60f,.91f,-.06f),.018f,black);
        foreach(float z in new[]{-.20f,-.06f})Part("Handset_Earpiece",PrimitiveType.Sphere,new Vector3(-.60f,.895f,z),new Vector3(.06f,.045f,.05f),black);
        Rod("Handset_Cord",new Vector3(-.60f,.885f,-.06f),new Vector3(-.53f,.70f,-.06f),.004f,black);
        Rod("Handset_Cord_Return",new Vector3(-.53f,.70f,-.06f),new Vector3(-.72f,.86f,-.17f),.004f,black);
        Box("Timetable_Clipboard",new Vector3(-1.05f,.829f,.005f),new Vector3(.17f,.008f,.23f),black);
        Box("Timetable_Blank_Insert",new Vector3(-1.05f,.835f,.005f),new Vector3(.15f,.003f,.21f),paper);
        Box("Timetable_Clip",new Vector3(-1.05f,.84f,-.087f),new Vector3(.06f,.008f,.015f),steel);
        for(int i=0;i<8;i++)Box("Timetable_Print_Rule",new Vector3(-1.05f,.837f,-.06f+i*.018f),new Vector3(.12f,.0008f,.001f),enamel);
        // Retaining lip and divided pockets keep small items off the floor.
        Box("Assistant_Map_Pocket_Back",new Vector3(-1.32f,.57f,.15f),new Vector3(.035f,.27f,.40f),enamel);
        Box("Assistant_Map_Pocket_Front",new Vector3(-1.24f,.53f,.15f),new Vector3(.025f,.19f,.40f),enamel);
        foreach(float z in new[]{-.05f,.15f,.35f})Box("Map_Pocket_Divider",new Vector3(-1.28f,.53f,z),new Vector3(.10f,.19f,.015f),steel);
        Box("Map_Pocket_Base",new Vector3(-1.28f,.435f,.15f),new Vector3(.10f,.015f,.40f),enamel);
        // Next batch: restrained safety fittings on solid partition / vestibule returns.
        foreach(float z in new[]{2.13f,17.15f})
        {
            float x=z<3?1.21f:-1.21f;
            float face=z<3?1:-1, mountZ=z-.075f-face*.075f;
            Box("Extinguisher_Back_Mount",new Vector3(x,.46f,mountZ),new Vector3(.12f,.58f,.025f),enamel);
            foreach(float y in new[]{.23f,.68f})
            {
                Rod("Extinguisher_Wall_Anchor",new Vector3(x,y,mountZ),new Vector3(x,y,z<3?1.96f:17.30f),.012f,steel);
                Part("Extinguisher_Mount_Bolt",PrimitiveType.Sphere,new Vector3(x,y,mountZ+face*.02f),new Vector3(.016f,.016f,.01f),steel);
            }
            foreach(float y in new[]{.32f,.59f})
            {
                Box("Extinguisher_Fixed_Strap",new Vector3(x,y,mountZ),new Vector3(.15f,.023f,.016f),black);
                Box("Extinguisher_Strap_Buckle",new Vector3(x+.08f,y,z-.04f),new Vector3(.026f,.036f,.018f),steel);
            }
            Box("Extinguisher_Base_Cup",new Vector3(x,.22f,z-.075f),new Vector3(.16f,.025f,.17f),steel);
            int first=root.childCount;
            Part("Extinguisher_Cylinder",PrimitiveType.Capsule,new Vector3(x,.46f,z-.075f),new Vector3(.13f,.23f,.13f),safety);
            Part("Extinguisher_Neck",PrimitiveType.Cylinder,new Vector3(x,.71f,z-.075f),new Vector3(.035f,.025f,.035f),steel);
            Rod("Extinguisher_Lever",new Vector3(x-.045f,.75f,z-.075f),new Vector3(x+.045f,.75f,z-.075f),.010f,black);
            Rod("Extinguisher_Safety_Pin",new Vector3(x-.03f,.735f,z-.08f),new Vector3(x-.06f,.735f,z-.08f),.004f,steel);
            Rod("Extinguisher_Hose",new Vector3(x+.04f,.72f,z-.075f),new Vector3(x+.075f,.35f,z-.075f),.008f,black);
            Box("Extinguisher_Instruction_Label",new Vector3(x,.48f,z-.075f+face*.067f),new Vector3(.075f,.14f,.004f),paper);
            for(int i=0;i<5;i++)Box("Extinguisher_Label_Rule",new Vector3(x,.52f-i*.02f,z-.075f+face*.070f),new Vector3(.055f,.002f,.001f),black);
            var parts=new Transform[root.childCount-first];for(int i=0;i<parts.Length;i++)parts[i]=root.GetChild(first+i);
            var item=new GameObject("Extinguisher_Removable_Assembly").transform;item.SetParent(root,false);item.localPosition=new Vector3(x,.46f,z-.075f);
            foreach(var part in parts)part.SetParent(item,true);
            var mount=new GameObject("Extinguisher_Interaction_Dock");mount.transform.SetParent(root,false);mount.transform.localPosition=new Vector3(x,.46f,z-.075f);mount.layer=30;
            var hit=mount.AddComponent<BoxCollider>();hit.size=new Vector3(.17f,.60f,.19f);
            mount.AddComponent<CabSafetyEquipment>().Configure(item,true);
            Box("Extinguisher_Identification_Plate",new Vector3(x,.88f,z-.015f),new Vector3(.20f,.12f,.012f),safety);
            Label("Extinguisher_Nameplate","灭火器\nFIRE",new Vector3(x,.88f,z-.015f+face*.008f),.003f,Quaternion.Euler(0,face>0?180:0,0));
        }
        foreach(float x in new[]{-.51f,.51f})
        {
            Rod("Front_Passage_Grab_Rail",new Vector3(x,.78f,2.01f),new Vector3(x,1.64f,2.01f),.016f,steel);
            foreach(float y in new[]{.78f,1.64f})Rod("Passage_Grab_Wall_StandOff",new Vector3(x,y,2.01f),new Vector3(x,y,1.96f),.012f,steel);
        }
        Box("Rear_Vestibule_Safety_Panel",new Vector3(-1.34f,1.35f,17.12f),new Vector3(.025f,.30f,.24f),enamel);
        int hammerStart=root.childCount;
        Rod("Emergency_Hammer_Grip_Visual_Only",new Vector3(-1.31f,1.25f,17.12f),new Vector3(-1.31f,1.43f,17.12f),.013f,safety);
        Rod("Emergency_Hammer_Head",new Vector3(-1.31f,1.43f,17.07f),new Vector3(-1.31f,1.43f,17.17f),.020f,steel);
        var hammerParts=new[]{root.GetChild(hammerStart),root.GetChild(hammerStart+1)};
        var hammer=new GameObject("Emergency_Hammer_Removable_Assembly").transform;hammer.SetParent(root,false);hammer.localPosition=new Vector3(-1.31f,1.35f,17.12f);
        foreach(var part in hammerParts)part.SetParent(hammer,true);
        var hammerDock=new GameObject("Emergency_Hammer_Dock");hammerDock.transform.SetParent(root,false);hammerDock.transform.localPosition=hammer.localPosition;hammerDock.layer=30;
        hammerDock.AddComponent<BoxCollider>().size=new Vector3(.09f,.26f,.15f);hammerDock.AddComponent<CabSafetyEquipment>().Configure(hammer,false);
        Label("Emergency_Hammer_Plate","安全锤",new Vector3(-1.322f,1.56f,17.12f),.003f,Quaternion.Euler(0,-90,0));
        NF5KIndustrialFinish.FinishRunningGear(root);
        UnityEngine.Object.DestroyImmediate(collider);
        lever.AddComponent<MeshCollider>().sharedMesh=lever.GetComponent<MeshFilter>().sharedMesh;
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            if(renderer.name!="Parking_Selector_Pedestal"&&renderer.name!="Assistant_Work_Shelf"&&renderer.name!="Extinguisher_Cylinder")continue;
            foreach(var seat in body.GetComponentsInChildren<BoxCollider>())
                if((seat.name.EndsWith("Seat_Cushion")||seat.name.EndsWith("Seat_Back")||seat.name.StartsWith("Passenger_Seat_"))&&renderer.bounds.Intersects(seat.bounds))
                    throw new InvalidOperationException(renderer.name+" intersects seat envelope "+seat.name);
        }
        Debug.Log("NF5K_CAB_FITTINGS_CLEARANCE_OK: selector/shelf/extinguisher bounds clear of selected seat collider envelopes");
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
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/assistant_station_safety_round.png");
            camera.transform.position=new Vector3(.25f,1.18f,1.05f);camera.transform.LookAt(new Vector3(.98f,.71f,.75f));
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/parking_selector_safety_round.png");
            camera.transform.position=new Vector3(.82f,1.03f,2.70f);camera.transform.LookAt(new Vector3(1.21f,.60f,2.12f));camera.fieldOfView=58;
            fill.transform.position=new Vector3(0,1.6f,2.7f);
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/passenger_safety_interactive_delivery.png");
            EditorSceneManager.OpenScene("Assets/Scenes/CabRailMotionReview.unity");NF5KSimulationPlayReview.Begin();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
