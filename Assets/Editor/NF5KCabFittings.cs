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
        // Preserve the command facade; the actual buttons are mounted on the console later.
        var lever=new GameObject("Parking_Brake_Command");lever.transform.SetParent(root,false);
        root.SetParent(state.transform,true);
        lever.AddComponent<CabDrivingTarget>().Configure(state,CabDrivingAction.ParkingBrake,Vector3.up,0);
        NF5KCabWorkstationLayout.Build(root,body,enamel,steel,black,paper);
        // Next batch: restrained safety fittings on solid partition / vestibule returns.
        foreach(float z in new[]{2.13f,17.15f})
        {
            float x=z<3?1.21f:-1.21f;
            float face=z<3?1:-1, mountZ=z-.075f-face*.075f;
            Box("Extinguisher_Back_Mount",new Vector3(x,.44f,mountZ),new Vector3(.12f,.42f,.018f),enamel);
            foreach(float y in new[]{.26f,.61f})
            {
                Rod("Extinguisher_Wall_Anchor",new Vector3(x,y,mountZ),new Vector3(x,y,z<3?1.96f:17.30f),.012f,steel);
                Part("Extinguisher_Mount_Bolt",PrimitiveType.Cube,new Vector3(x,y,mountZ+face*.011f),new Vector3(.016f,.016f,.004f),steel);
            }
            Box("Extinguisher_Base_Cup",new Vector3(x,.2325f,z-.075f),new Vector3(.16f,.025f,.17f),steel);
            var item=new GameObject("Extinguisher_Removable_Assembly").transform;item.SetParent(root,false);item.localPosition=new Vector3(x,.245f,z-.075f);
            item.localRotation=Quaternion.Euler(0,face>0?0:180,0);
            NF5KExtinguisherModel.Build(item,z<3?5f:-7f);
            foreach(float side in new[]{-1f,1f})
                Box("Extinguisher_Band_Wall_Web",new Vector3(x+side*.058f,.49f,z-.075f-face*.0375f),new Vector3(.012f,.034f,.075f),steel);
            Rod("Extinguisher_Fixed_Hinge_Barrel",new Vector3(x-face*.058f,.472f,z-.075f),new Vector3(x-face*.058f,.508f,z-.075f),.006f,steel);
            var band=NF5KExtinguisherModel.BuildBand(root,new Vector3(x,.49f,z-.075f),face);
            var mount=new GameObject("Extinguisher_Interaction_Dock");mount.transform.SetParent(root,false);mount.transform.localPosition=new Vector3(x,.46f,z-.075f);mount.layer=30;
            var hit=mount.AddComponent<BoxCollider>();hit.size=new Vector3(.17f,.60f,.19f);
            mount.AddComponent<CabSafetyEquipment>().Configure(item,true,new[]{band});
            Box("Extinguisher_Identification_Plate",new Vector3(x,.74f,z-.015f),new Vector3(.20f,.12f,.012f),safety);
            Label("Extinguisher_Nameplate",z<3?"灭火器 FX-01\nFIRE EXTINGUISHER":"灭火器 FX-02\nFIRE EXTINGUISHER",new Vector3(x,.74f,z-.015f+face*.008f),.003f,Quaternion.Euler(0,face>0?180:0,0));
        }
        foreach(float x in new[]{-.56f,.56f})
        {
            Rod("Front_Passage_Grab_Rail",new Vector3(x,.78f,2.01f),new Vector3(x,1.64f,2.01f),.016f,steel);
            foreach(float y in new[]{.78f,1.64f})
            {
                Rod("Passage_Grab_Wall_StandOff",new Vector3(x,y,2.01f),new Vector3(x,y,1.96f),.012f,steel);
                Part("Passage_Grab_Isolation_Pad",PrimitiveType.Cylinder,new Vector3(x,y,1.951f),new Vector3(.052f,.001f,.052f),black).transform.localRotation=Quaternion.Euler(90,0,0);
                Part("Passage_Grab_Wall_Flange",PrimitiveType.Cylinder,new Vector3(x,y,1.956f),new Vector3(.050f,.004f,.050f),steel).transform.localRotation=Quaternion.Euler(90,0,0);
                foreach(float dy in new[]{-.017f,.017f})
                    Part("Passage_Grab_Flange_Bolt",PrimitiveType.Cylinder,new Vector3(x,y+dy,1.961f),new Vector3(.006f,.001f,.006f),steel).transform.localRotation=Quaternion.Euler(90,0,0);
                Part("Passage_Grab_Smooth_Return",PrimitiveType.Sphere,new Vector3(x,y,2.01f),Vector3.one*.032f,steel);
            }
            var railHit=new GameObject("Passage_Grab_Fixed_Collider");railHit.layer=30;railHit.transform.SetParent(root,false);railHit.transform.localPosition=new Vector3(x,1.21f,2.01f);
            railHit.AddComponent<BoxCollider>().size=new Vector3(.032f,.86f,.032f);
        }
        NF5KExtinguisherModel.BuildManual(root);
        NF5KWallEquipment.Build(root,body);
        NF5KCabVisors.Build(root,body);
        NF5KCabCirculators.Build(root,body);
        NF5KWiperSetup.Build(root,body);
        NF5KConsoleRecessSetup.Apply(body);
        NF5KLightingKeysSetup.Build(root,body);
        NF5KExteriorLightControlSetup.Build(root,body);
        NF5KAnnunciatorSetup.Build(root,body);
        NF5KBrakeRefinement.Apply(body);
        NF5KServiceControlSetup.Build(root,body);
        NF5KParkingSelectorDetail.Apply(state);
        NF5KVigilanceSetup.Apply(state);
        NF5KVigilanceIndicatorSetup.Apply(state);
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

        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            if(renderer.name!="Parking_Selector_Pedestal"&&renderer.name!="Assistant_Work_Shelf"&&renderer.name!="Extinguisher_Cylinder")continue;
            foreach(var seat in body.GetComponentsInChildren<BoxCollider>())
                if((seat.name.EndsWith("Seat_Cushion")||seat.name.EndsWith("Seat_Back")||seat.name.StartsWith("Passenger_Seat_"))&&renderer.bounds.Intersects(seat.bounds))
                    throw new InvalidOperationException(renderer.name+" intersects seat envelope "+seat.name);
        }
        NF5KCabWorkstationReview.Validate(body);
        Debug.Log("NF5K_CAB_FITTINGS_CLEARANCE_OK: selector/shelf/extinguisher bounds clear of selected seat collider envelopes");
        Debug.Log("NF5K_CAB_FITTINGS_OK: console-mounted parking buttons; auxiliary radio/handset, shelf and map pockets are visual only");
    }
    public static void CapturePassageProportions()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            UnityEngine.Object.FindFirstObjectByType<CabDaylightEnvironment>().Apply(CabDaylightEnvironment.DayPreset.Overcast);
            NF5KBogieDetailReview.ConfigureInspectionLighting();
            var fill=new GameObject("Temporary_Passage_Proportion_Fill").AddComponent<Light>();
            fill.type=LightType.Point;fill.range=4;fill.intensity=.20f;fill.transform.position=new Vector3(.8f,1.8f,3.0f);
            var camera=Camera.main;camera.orthographic=true;camera.orthographicSize=1.13f;
            // Place the inspection camera ahead of passenger seats so the leaf is unobstructed.
            camera.transform.position=new Vector3(0,1.0f,2.65f);camera.transform.LookAt(new Vector3(0,1.0f,1.91f));
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/passage_orthographic_clear_v16.png");
            Debug.Log("NF5K_PASSAGE_PROPORTION_CAPTURE_OK: saved daylight scene, front orthographic view ahead of seats; scene not modified on disk");
            EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static void CaptureCirculator(Camera camera,Light fill)
    {
        var fan=UnityEngine.Object.FindObjectsByType<CabCirculationFan>(FindObjectsSortMode.None)[0];
        var head=fan.Head;camera.orthographic=false;camera.fieldOfView=37;
        camera.transform.position=head.TransformPoint(new Vector3(0,0,.48f));camera.transform.LookAt(head.position);
        fill.type=LightType.Directional;fill.shadows=LightShadows.None;fill.transform.rotation=Quaternion.LookRotation(-head.forward+Vector3.down*.15f);fill.intensity=.45f;
        NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/cab_circulator_close_v33.png");
        camera.transform.position=head.TransformPoint(new Vector3(.31f,.09f,.34f));camera.transform.LookAt(head.TransformPoint(new Vector3(0,.02f,-.015f)));camera.fieldOfView=40;
        NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/cab_circulator_connections_v33.png");
        camera.transform.position=head.TransformPoint(new Vector3(0,0,-.22f));camera.transform.LookAt(head.TransformPoint(new Vector3(0,0,-.081f)));camera.fieldOfView=27;
        fill.transform.rotation=Quaternion.LookRotation(head.forward+Vector3.down*.1f);fill.intensity=.3f;
        NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/cab_fan_rating_plate_v33.png");
        fill.transform.rotation=Quaternion.LookRotation(-head.forward+Vector3.down*.15f);fill.intensity=.3f;
        var flange=fan.transform.Find("Fan_Roof_Flange");
        camera.transform.position=flange.position+new Vector3(.10f,-.10f,.12f);camera.transform.LookAt(flange.position+Vector3.down*.025f);camera.fieldOfView=45;
        NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/cab_fan_mount_hardware_v33.png");
        camera.transform.position=fan.Control.position+new Vector3(.07f,.18f,.16f);camera.transform.LookAt(fan.Control.position);camera.fieldOfView=32;
        NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/cab_fan_desk_control_v33.png");
        var screw=fan.Control.GetComponentsInChildren<Transform>();
        foreach(var part in screw)if(part.name=="Fan_Control_Fixing_Screw")
        {
            camera.transform.position=part.position+new Vector3(.012f,.025f,.021f);camera.transform.LookAt(part.position);camera.fieldOfView=25;
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/cab_fan_fastener_macro_v33.png");break;
        }
        camera.transform.position=new Vector3(0,1.55f,1.5f);camera.transform.LookAt(new Vector3(0,1.89f,.30f));camera.fieldOfView=82;
        fill.transform.rotation=Quaternion.Euler(20,180,0);fill.intensity=.25f;
        NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/cab_circulators_layout_v33.png");
    }
    public static void CaptureCirculators()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            UnityEngine.Object.FindFirstObjectByType<CabDaylightEnvironment>().Apply(CabDaylightEnvironment.DayPreset.Overcast);
            NF5KBogieDetailReview.ConfigureInspectionLighting();
            var fill=new GameObject("Temporary_Fan_Review_Fill").AddComponent<Light>();fill.type=LightType.Point;fill.range=4;
            CaptureCirculator(Camera.main,fill);EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
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
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/assistant_station_safety_round_passage_circulators_v17.png");
            camera.transform.position=new Vector3(0,1.76f,1.72f);camera.transform.LookAt(new Vector3(0,1.05f,-.13f));camera.fieldOfView=75;
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/cab_communication_overall_v17.png");
            camera.transform.position=new Vector3(-.58f,1.16f,.40f);camera.transform.LookAt(new Vector3(-.58f,.850f,.12f));camera.fieldOfView=47;
            fill.intensity=.15f;NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/crew_record_close_v17.png");
            camera.transform.position=new Vector3(-.67f,.48f,.53f);camera.transform.LookAt(new Vector3(-.85f,.55f,-.10f));camera.fieldOfView=65;
            fill.transform.position=new Vector3(-.6f,.5f,.5f);fill.intensity=.75f;NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/aux_underdesk_support_v17.png");
            var passageDoor=UnityEngine.Object.FindFirstObjectByType<CabPassageDoor>();
            camera.transform.position=new Vector3(0,1.05f,.35f);camera.transform.LookAt(new Vector3(0,1.05f,1.91f));camera.fieldOfView=65;
            fill.transform.position=new Vector3(-.8f,1.8f,1.0f);fill.intensity=.25f;NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/passage_closed_circulators_v17.png");
            var passagePose=passageDoor.transform.localRotation;passageDoor.transform.localRotation=passagePose*Quaternion.Euler(0,85,0);
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/passage_open_circulators_v17.png");passageDoor.transform.localRotation=passagePose;
            camera.transform.position=new Vector3(0,1.05f,3.40f);camera.transform.LookAt(new Vector3(0,1.05f,1.91f));camera.fieldOfView=68;
            fill.transform.position=new Vector3(.8f,1.8f,3.0f);fill.intensity=.20f;NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/passage_passenger_circulators_v17.png");
            camera.transform.position=new Vector3(0,1.45f,4.20f);camera.transform.LookAt(new Vector3(0,.98f,1.91f));camera.fieldOfView=54;
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/passage_full_circulators_v17.png");
            camera.orthographic=true;camera.orthographicSize=1.13f;camera.transform.position=new Vector3(0,1.0f,2.65f);camera.transform.LookAt(new Vector3(0,1.0f,1.91f));
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/passage_orthographic_circulators_v17.png");camera.orthographic=false;
            camera.transform.position=new Vector3(.25f,1.18f,1.05f);camera.transform.LookAt(new Vector3(.98f,.71f,.75f));
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/parking_selector_safety_round_passage_circulators_v17.png");
            camera.transform.position=new Vector3(.86f,.82f,2.65f);camera.transform.LookAt(new Vector3(1.21f,.48f,2.055f));camera.fieldOfView=50;
            fill.transform.position=new Vector3(0,1.6f,2.7f);
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/extinguisher_bilingual_mounted_print_delivery_passage_circulators_v17.png");
            camera.transform.position=new Vector3(1.21f,.46f,2.48f);camera.transform.LookAt(new Vector3(1.21f,.435f,2.055f));camera.fieldOfView=58;
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/extinguisher_bilingual_front_print_delivery_passage_circulators_v17.png");
            camera.transform.position=new Vector3(1.21f,.575f,2.30f);camera.transform.LookAt(new Vector3(1.21f,.572f,2.09f));camera.fieldOfView=30;
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/extinguisher_gauge_valve_print_delivery_passage_circulators_v17.png");
            camera.transform.position=new Vector3(.95f,.84f,2.45f);camera.transform.LookAt(new Vector3(.95f,.84f,2.02f));camera.fieldOfView=45;
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/baiyue_fire_manual_print_delivery_passage_circulators_v17.png");
            var aid=UnityEngine.Object.FindFirstObjectByType<CabStorageCabinet>();fill.intensity=.85f;
            camera.transform.position=new Vector3(-.67f,1.45f,2.58f);camera.transform.LookAt(aid.transform.position);camera.fieldOfView=44;
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/first_aid_cabinet_closed_print_delivery_passage_circulators_v17.png");
            aid.Toggle();camera.transform.position=new Vector3(-.48f,1.47f,2.62f);camera.transform.LookAt(aid.transform.position);
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/first_aid_cabinet_open_print_delivery_passage_circulators_v17.png");
            camera.transform.position=new Vector3(-.90f,1.468f,2.27f);camera.transform.LookAt(new Vector3(-.90f,1.468f,2.027f));camera.fieldOfView=35;
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/first_aid_pack_print_closeup_passage_circulators_v17.png");aid.Toggle();
            camera.transform.position=new Vector3(.85f,1.48f,1.13f);camera.transform.LookAt(new Vector3(.85f,1.48f,1.82f));camera.fieldOfView=48;
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/rear_electrical_cover_print_delivery_passage_circulators_v17.png");
            var visors=UnityEngine.Object.FindObjectsByType<CabSunVisor>(FindObjectsSortMode.None);
            fill.transform.position=new Vector3(0,1.3f,1.5f);fill.intensity=.6f;
            camera.transform.position=new Vector3(0,1.63f,1.13f);camera.transform.LookAt(new Vector3(0,1.77f,-.40f));camera.fieldOfView=67;
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/cab_visors_stowed_delivery_passage_circulators_v17.png");
            foreach(var visor in visors){visor.Adjust(1);visor.Adjust(1);}
            NF5KBogieDetailReview.Capture(camera,"tools/cab_reference/interior_review/cab_visors_lowered_delivery_passage_circulators_v17.png");
            CaptureCirculator(camera,fill);
            EditorSceneManager.OpenScene("Assets/Scenes/CabRailMotionReview.unity");NF5KSimulationPlayReview.Begin();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
