using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RailwayRenaissance.Cab;

public static class NF5KVigilanceSetup
{
    public static void Apply(CabEngineState state)
    {
        var body=state.GetComponentInParent<CabInteriorLighting>().transform;
        foreach(var t in body.GetComponentsInChildren<Transform>().Where(t=>t.name=="Vigilance_Pedal_Reserved"||t.name=="Driver_Footrest"||t.name=="NF5K_Vigilance_Assembly").ToArray())
            if(t!=null)UnityEngine.Object.DestroyImmediate(t.gameObject);
        var root=new GameObject("NF5K_Vigilance_Assembly").transform;root.SetParent(state.transform,false);
        root.SetPositionAndRotation(body.TransformPoint(new Vector3(.65f,0,.49f)),body.rotation);root.localScale=Vector3.one/state.transform.lossyScale.x;
        Material M(string n)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/"+n+".mat");
        var paint=NF5KPedalFinish.Material("Cab_Pedal_Painted_Steel_V5",new Color(.21f,.26f,.25f),.32f,.20f,false);
        var steel=NF5KPedalFinish.Material("Cab_Pedal_Satin_Axle_V2",new Color(.42f,.45f,.44f),.8f,.32f,false);
        var rubber=NF5KPedalFinish.Material("Cab_Pedal_Rubber_V5",new Color(.14f,.15f,.14f),0,.12f,true);
        Transform Part(string name,Vector3 p,Vector3 size,Material mat,bool collider=false)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.layer=30;g.transform.SetParent(root,false);g.transform.localPosition=p;
            g.GetComponent<MeshFilter>().sharedMesh=NF5KPedalFinish.Rounded(size,name=="Vigilance_Rubber_Tread"?.003f:.0025f);g.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collider)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());else g.GetComponent<BoxCollider>().size=size;return g.transform;
        }
        Part("Footrest_Base",new Vector3(.02f,.003f,0),new Vector3(.40f,.006f,.25f),paint);
        var shell=NF5KParkingConsoleMount.MeshPart(root,"Footrest_Continuous_Wedge",NF5KPedalFinish.Wedge(),paint,true);
        var deck=new GameObject("Footrest_Inclined_Frame").transform;deck.SetParent(root,false);deck.localPosition=new Vector3(0,.055f,0);deck.localRotation=Quaternion.Euler(15,0,0);
        Transform Surface(string name,Vector3 position,Vector3 size,Material finish,bool hit=false)
        {var part=Part(name,Vector3.zero,size,finish,hit);part.SetParent(deck,false);part.localPosition=position;return part;}
        Surface("Fixed_Foot_Support",new Vector3(-.065f,.002f,0),new Vector3(.20f,.004f,.235f),steel);
        for(int i=0;i<10;i++)Surface("Fixed_Footrest_Rib",new Vector3(-.065f,.0047f,-.10f+i*.022f),new Vector3(.18f,.0015f,.005f),paint);
        var pivot=new GameObject("Vigilance_Heel_Hinge").transform;pivot.SetParent(deck,false);pivot.localPosition=new Vector3(.10f,.006f,.085f);
        var tread=Surface("Vigilance_Rubber_Tread",new Vector3(.10f,.010f,0),new Vector3(.105f,.008f,.17f),rubber,true);
        tread.SetParent(pivot,true);
        var v=root.gameObject.AddComponent<CabVigilance>();v.Configure(state,tread,pivot);
        for(int i=0;i<7;i++)
        {
            var rib=Surface("Tread_Rib",new Vector3(.10f,.0145f,-.066f+i*.022f),new Vector3(.09f,.0015f,.006f),rubber);
            rib.SetParent(tread,true);
        }
        Surface("Pedal_Switch_Body",new Vector3(.10f,-.012f,0),new Vector3(.08f,.024f,.13f),paint);
        foreach(float x in new[]{-.17f,.21f})foreach(float z in new[]{-.108f,.108f})
        {
            var fastener=NF5KParkingConsoleMount.MeshPart(deck,"Footrest_Service_Screw",AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/Cab/Cab_Fan_Recessed_Slotted_Head_V6.asset"),steel);
            fastener.transform.localPosition=new Vector3(x,.0004f,z);fastener.transform.localScale=new Vector3(.008f,.0015f,.008f);
        }
        var hinge=GameObject.CreatePrimitive(PrimitiveType.Cylinder);hinge.name="Hinge_Barrel";hinge.transform.SetParent(deck,false);hinge.transform.localPosition=pivot.localPosition;hinge.transform.localRotation=Quaternion.Euler(0,0,90);hinge.transform.localScale=new Vector3(.008f,.064f,.008f);
        hinge.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/Cab/Cab_Circulator_Smooth_Cylinder_V3.asset");hinge.GetComponent<Renderer>().sharedMaterial=steel;UnityEngine.Object.DestroyImmediate(hinge.GetComponent<Collider>());
        foreach(float x in new[]{.043f,.157f})
            Surface("Hinge_Bearing_Block",new Vector3(x,.004f,.085f),new Vector3(.008f,.008f,.018f),paint);
        foreach(float x in new[]{.037f,.163f})
            Surface("Pedal_Aperture_Edge",new Vector3(x,.0006f,0),new Vector3(.002f,.0012f,.19f),rubber);
        foreach(float x in new[]{-.158f,.198f})foreach(float z in new[]{-.10f,.10f})
        {
            var screw=NF5KParkingConsoleMount.MeshPart(root,"Floor_Captive_Screw",AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/Cab/Cab_Fan_Recessed_Slotted_Head_V6.asset"),steel);
            screw.transform.localPosition=new Vector3(x,.006f,z);screw.transform.localScale=new Vector3(.009f,.002f,.009f);
        }
    }
    public static void Batch()
    {
        try
        {
            foreach(string n in new[]{"CabInteriorBlockout","CabDaylightReview","CabSimulationReview","CabRailMotionReview"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+n+".unity");var state=UnityEngine.Object.FindAnyObjectByType<CabEngineState>();
                Apply(state);Apply(state);NF5KVigilanceReview.Geometry(state);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            UnityEngine.Object.FindAnyObjectByType<CabDaylightEnvironment>().Apply(CabDaylightEnvironment.DayPreset.Overcast);NF5KBogieDetailReview.ConfigureInspectionLighting();
            var fill=new GameObject("Footwell_Inspection_Only").AddComponent<Light>();fill.type=LightType.Point;fill.transform.position=new Vector3(.9f,.48f,.65f);fill.intensity=.35f;fill.range=2;
            var cam=Camera.main;cam.transform.position=new Vector3(1.10f,.36f,.73f);cam.transform.LookAt(new Vector3(.70f,.06f,.46f));cam.fieldOfView=65;
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/vigilance_footwell_v7.png");
            EditorSceneManager.OpenScene("Assets/Scenes/CabRailMotionReview.unity");NF5KSimulationPlayReview.Begin();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
