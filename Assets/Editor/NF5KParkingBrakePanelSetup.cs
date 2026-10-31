using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KParkingBrakePanelSetup
{
    public static void Apply(CabEngineState state)
    {
        var command=state.GetComponentsInChildren<CabDrivingTarget>().Single(t=>t.Action==CabDrivingAction.ParkingBrake);
        var root=command.transform.parent;
        foreach(var t in command.GetComponentsInChildren<Transform>().Where(t=>t!=command.transform).ToArray())
            if(t!=null)UnityEngine.Object.DestroyImmediate(t.gameObject);
        if(command.GetComponent<Renderer>()!=null)command.GetComponent<Renderer>().enabled=false;
        foreach(var c in command.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(c);
        command.Configure(state,CabDrivingAction.ParkingBrake,Vector3.up,0);
        foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>t.name=="NF5K_Parking_Button_Panel"||t.name=="Parking_Integrated_Scale"||t.name=="Parking_Plate_Seating_Gasket"||t.name=="Parking_Dust_Seal"||t.name=="Parking_Fixed_Stop"||t.name=="Selector_End_Mark"||t.name=="Selector_Shaft_Collar"||t.name=="Parking_Plate_Legend"||t.name=="Parking_Apply_Legend"||t.name=="Parking_Release_Legend"||t.name=="Parking_Selector_Pedestal"||t.name=="Parking_Selector_Floor_Flange"||t.name=="Pedestal_Floor_Bolt"||t.name=="Selector_Service_Cover"||t.name=="Selector_Service_Inset"||t.name=="Parking_Selector_Bezel"||t.name=="Selector_Plate_Fastener").ToArray())
            if(t!=null)UnityEngine.Object.DestroyImmediate(t.gameObject);
        var panel=new GameObject("NF5K_Parking_Button_Panel").transform;panel.SetParent(root,false);
        var frame=state.GetComponentInParent<CabInteriorLighting>().transform;
        panel.SetPositionAndRotation(frame.TransformPoint(NF5KParkingConsoleMount.Anchor),frame.rotation*NF5KParkingConsoleMount.Tilt);
        command.transform.position=panel.position;
        Material M(string n)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/"+n+".mat");
        var dark=M("Cab_Fan_Selector_Graphite_V5");var steel=M("Cab_Fan_Satin_Steel");
        var cylinder=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/Cab/Cab_Circulator_Smooth_Cylinder_V3.asset");
        GameObject Part(string name,Transform parent,Vector3 position,Vector3 scale,Material mat,PrimitiveType type)
        {
            var g=GameObject.CreatePrimitive(type);g.layer=30;g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;
            if(type==PrimitiveType.Cylinder)g.GetComponent<MeshFilter>().sharedMesh=cylinder;return g;
        }
        var deck=frame.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="Contoured_Control_Deck");
        var shell=frame.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Continuous_Folded_Console_Shell");
        NF5KParkingConsoleMount.MeshPart(panel,"Parking_Panel_Flush_Bezel",NF5KParkingConsoleMount.PerforatedPlate("Cab_Parking_Flush_Bezel_V7",0,.003f),M("Cab_Annunciator_Frame"),true);
        NF5KParkingConsoleMount.MeshPart(panel,"Parking_Panel_Seating_Gasket",NF5KParkingConsoleMount.PerforatedPlate("Cab_Parking_Panel_Gasket_V7",-.003f,.0015f),dark);
        NF5KParkingConsoleMount.Housing(panel,deck,shell.sharedMaterial,dark);
        void Print(string key,Vector3 p,Vector2 size)
        {
            string name="Parking_Button_Print_"+key;
            NF5KEquipmentPrint.Add(panel,name,"Cab_Parking_"+key+"_V6",p,size);
            var t=panel.Find(name);t.localRotation=Quaternion.Euler(-90,0,0);
            var ink=t.GetComponent<Renderer>().sharedMaterial;
            NF5KLightingKeysSetup.ConfigureInk(ink);
            ink.SetFloat("_Blend",0);ink.SetFloat("_BlendModePreserveSpecular",0);
            ink.DisableKeyword("_ALPHAPREMULTIPLY_ON");ink.DisableKeyword("_ALPHAMODULATE_ON");EditorUtility.SetDirty(ink);
            t.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        Print("title",new Vector3(0,.0002f,-.032f),new Vector2(.108f,.018f));
        foreach(bool releasing in new[]{false,true})
        {
            float x=releasing?-NF5KParkingConsoleMount.ButtonX:NF5KParkingConsoleMount.ButtonX;
            var target=new GameObject(releasing?"Parking_Release_Button":"Parking_Apply_Button").transform;
            target.gameObject.layer=30;target.SetParent(panel,false);target.localPosition=new Vector3(x,.0007f,-.004f);
            Part("Parking_Button_Seal",target,Vector3.zero,new Vector3(.036f,.0007f,.036f),dark,PrimitiveType.Cylinder);
            Part("Parking_Button_Threaded_Collar",target,Vector3.up*.0025f,new Vector3(.034f,.0018f,.034f),steel,PrimitiveType.Cylinder);
            var cap=Part("Parking_Button_Moving_Cap",target,Vector3.up*.005f,new Vector3(.027f,.009f,.027f),Finish(releasing?"Cab_Parking_Release_Green":"Cab_Parking_Apply_Red",releasing?new Color(.055f,.25f,.115f):new Color(.42f,.055f,.035f)),PrimitiveType.Sphere);
            Part("Parking_Button_Rear_Body",target,Vector3.down*.014f,new Vector3(.020f,.013f,.020f),dark,PrimitiveType.Cylinder);
            cap.AddComponent<MeshCollider>().sharedMesh=cap.GetComponent<MeshFilter>().sharedMesh;
            target.gameObject.AddComponent<CabParkingBrakeButton>().Configure(state,command,releasing,cap.transform);
            Print(releasing?"release":"apply",new Vector3(x,.0002f,.029f),new Vector2(.052f,.016f));
        }
        foreach(float x in new[]{-.053f,.053f})foreach(float z in new[]{-.034f,.034f})
        {
            var screw=NF5KParkingConsoleMount.MeshPart(panel,"Parking_Panel_Captive_Screw",MESH("Cab_Fan_Recessed_Slotted_Head_V6"),steel);
            screw.transform.localPosition=new Vector3(x,.0003f,z);screw.transform.localScale=new Vector3(.006f,.001f,.006f);
            screw.GetComponent<Renderer>().sharedMaterials=new[]{steel,M("Cab_Fan_Fastener_Recess_V4")};
        }
    }
    static Mesh MESH(string name)=>AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/Cab/"+name+".asset");
    static Material Finish(string name,Color color)
    {
        string path="Assets/Resources/Cab/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",.04f);m.SetFloat("_Smoothness",.32f);EditorUtility.SetDirty(m);return m;
    }
    public static void Batch()
    {
        try
        {
            foreach(string n in new[]{"CabInteriorBlockout","CabDaylightReview","CabSimulationReview","CabRailMotionReview"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+n+".unity");var state=UnityEngine.Object.FindAnyObjectByType<CabEngineState>();
                Apply(state);Apply(state);NF5KParkingBrakePanelReview.Geometry(state);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            UnityEngine.Object.FindAnyObjectByType<CabDaylightEnvironment>().Apply(CabDaylightEnvironment.DayPreset.Overcast);NF5KBogieDetailReview.ConfigureInspectionLighting();
            var panel=UnityEngine.Object.FindAnyObjectByType<CabParkingBrakeButton>().transform.parent;var cam=Camera.main;
            cam.fieldOfView=40;cam.transform.position=panel.position+new Vector3(-.20f,.20f,.32f);cam.transform.LookAt(panel.position);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/parking_brake_console_mount_v7.png");
            cam.fieldOfView=75;cam.transform.position=new Vector3(.65f,1.38f,.78f);cam.transform.LookAt(panel.position+Vector3.up*.10f);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/parking_brake_driver_context_v7.png");
            cam.fieldOfView=58;cam.transform.position=new Vector3(.52f,1.30f,1.14f);cam.transform.LookAt(panel.position+new Vector3(-.18f,-.04f,-.02f));
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/parking_brake_removed_pedestal_v7.png");
            Debug.Log("NF5K_PARKING_BUTTONS_BUILD_OK: four scenes, red apply and green release; rotary assembly retired");
            EditorSceneManager.OpenScene("Assets/Scenes/CabRailMotionReview.unity");NF5KSimulationPlayReview.Begin();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
